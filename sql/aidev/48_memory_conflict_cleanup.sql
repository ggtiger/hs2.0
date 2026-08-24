-- ============================================================
-- 记忆库冲突清理(6 组)
-- 1. am_pitfall_list_row_buttons    重写: 与 row_buttons_optional 调和 + 合法值
-- 2. am_pitfall_form_subpage_navigate 删除: navigate 机制不存在, 被 add_button_extparam 取代
-- 3. am_rule_resource_prefix        更新: VCK/VBS 旧认知 → T→V 规则
-- 4. am_example_extend_store        重写: function(context) → 对象形式(实际加载机制)
-- 5. am_rule_extend_store_pattern   重写: 同上
-- 6. am_rule_button_callbacks       重写: INTERACTTYPE 枚举 none/confirm/form/select → direct/poptip
-- 7. am_rule_metadata_perm_field    更新: 补长度标准
-- 8. am_rule_wizard_changeset_shared 更新: 补分步执行强制说明
-- 日期: 2026-07-20
-- ============================================================

-- --------------------------------------------------
-- 1. row 按钮: 默认不配 + 要配就按合法配法(与 optional 调和)
-- --------------------------------------------------
UPDATE tss_ai_memory SET
TITLE='row 按钮默认不配(行点击即编辑); 需要行内操作时的正确配法',
CONTENT='【默认原则】generic-module 行点击(clickRow)默认打开表单编辑, 简单 CRUD 不配 row 按钮(与 am_pitfall_row_buttons_optional 一致)。
【需要行内快捷操作时才配 row 按钮】(如行内删除/行内状态切换), 正确配法:
  「编辑」: BTNAREA=row, BTNCODE=edit, INTERACTTYPE=direct,
    EXTPARAM={\"action\":\"openForm\",\"openMode\":\"edit\",\"formPageCode\":\"add\"}
  「删除」: BTNAREA=row, BTNCODE=delete, INTERACTTYPE=poptip, POPTIPTEXT=确定删除?
    (走 store delete action, 不需 APICODE)
【INTERACTTYPE 只允许 direct/poptip】(后端 DefineButton 强校验, 前端 generic-module 只识别 poptip):
  direct=点击直接执行; poptip=Poptip 弹确认框。不存在 navigate/confirm/form/select 等值。
【反例】RS_M26 最初没有任何行操作入口(当时 row 按钮缺失且行点击未配 defaultFormPageCode); RS_FEE 最初 row 按钮用了 INTERACTTYPE=navigate/confirm 非法值。
【正例】默认: 不配 row 按钮 + defaultFormPageCode 配好(行点击编辑); 需要时: 按上述 direct/poptip 配。',
MODIFYTIME=NOW()
WHERE ID='am_pitfall_list_row_buttons';

-- --------------------------------------------------
-- 2. 删除 navigate 子页面错误记忆(机制不存在, 与 rs-modal 内嵌矛盾)
-- --------------------------------------------------
UPDATE tss_ai_memory SET ISDELETED=1, MODIFYTIME=NOW()
WHERE ID='am_pitfall_form_subpage_navigate';

-- --------------------------------------------------
-- 3. 资源前缀: T→V 规则(用户 2026-07-20 澄清)
-- --------------------------------------------------
UPDATE tss_ai_memory SET
TITLE='资源命名规则: 物理表 tbs_/tck_, 数据视图 = 表名首字母 T 换 V',
CONTENT='TBS_xxx=物理表定义(基础/业务表) / TCK_xxx=物理表定义(流程/记录表) / 数据视图(DATAVIEW) = 物理表名首字母 T 换成 V: TBS_XXX→VBS_XXX, TCK_XXX→VCK_XXX(实证 TBS_DEPT→VBS_DEPT, TCK_ORECORD→VCK_ORECORD) / VRP_xxx=报表视图(SQL 类型) / VSS_xxx=系统管理视图。
【注意】不是 "VCK=业务视图/VBS=选择器" 的旧认知(2026-07-20 用户澄清)。tbs_ 表的视图绝不叫 VCK_。同表多视图加后缀: VCK_ACCEPT_SEL/VCK_ACCEPT_FEE。tss_resource.RESOURCENAME 按此前缀分类, 决定 ORM 处理方式。详见 am_rule_view_naming_t2v。',
MODIFYTIME=NOW()
WHERE ID='am_rule_resource_prefix';

-- --------------------------------------------------
-- 4. 扩展 Store 示例: 对象形式(loadModuleStoreExtend 实际期望)
-- --------------------------------------------------
UPDATE tss_ai_memory SET
TITLE='示例: 扩展 Store 文件(@/modules/{MC}/store.js, 对象形式)',
CONTENT='// 文件路径: @/modules/LIB_M05/store.js
// 加载机制: generic-store.js loadModuleStoreExtend 检查
//   typeof extendObj === ''object'' && (extendObj.actions || extendObj.mutations)
//   → 必须 export default 一个**对象**(含 actions/mutations),
//   → function(context) 形式过不了 typeof 检查, 扩展静默不生效!
export default {
  actions: {
    // 自定义 action: 加载关联子表(标准 open 不返回时单独拉)
    async loadAcceptRefs({ commit, dispatch, rootState, state }, { id }) {
      const ns = state.MODULECODE;  // 模块编码从 state 取, 不能硬编码
      const ret = await dispatch(\"call\", {
        APICODE: \"A21\",
        params: { id },
      });
      // 写回 DTSA DataTable
      const dt = rootState[ns].dt.DTSA;
      if (dt) {
        dt.clear();
        ((ret && ret.DTSA) || []).forEach(r => dt.add({ ACCEPTID: r.ACCEPTID, ACCEPTCODE: r.ACCEPTCODE }));
      }
      return ret;
    },
    // 初始化新增: 先清空数据源, 再创建空行, 再打开
    async initAdd({ commit, dispatch }, params) {
      commit(\"INIT\", { paths: [\"MAIN\", \"DTS\"] });
      await dispatch(\"add\", {});
      await dispatch(\"open\", { ID: \"\" });
      return {};
    },
  },
  mutations: {
    SET_ENTRYNUM(state, { path }) {
      const dt = state.dt[path];
      if (dt && dt.data) dt.data.forEach((r, i) => dt.setValue(\"ENTRYNUM\", i + 1, i));
    },
  },
};
// 【铁律】不能 import 外部模块(db/createStore/Vue/heyui), 不能用闭包变量;
// 只能通过 action 第一个参数 { state, commit, dispatch, rootState, getters } 访问。
// 【实证】LIB_M05 store.js 就是对象形式; function(context) 形式是早期错误示例。',
MODIFYTIME=NOW()
WHERE ID='am_example_extend_store';

-- --------------------------------------------------
-- 5. 扩展 Store 规则: 同步对象形式
-- --------------------------------------------------
UPDATE tss_ai_memory SET
TITLE='扩展 Store 文件(@/modules/{MC}/store.js)必须 export default 对象{actions,mutations}',
CONTENT='在线开发列表/表单页若需要自定义 action(如 loadAcceptRefs/endisable/importExcel), 在 @/modules/{MC}/store.js 写扩展 Store:
【加载机制】generic-store.js applyStoreExtend(moduleCode) → loadModuleStoreExtend 异步加载, 合并 actions/mutations 到已注册 Vuex 模块(Store03 基础 actions 不被覆盖, 扩展只新增)。
【导出形式铁律】必须 export default { actions: {...}, mutations: {...} } 对象!
  loadModuleStoreExtend 检查 typeof extendObj === ''object'' && (extendObj.actions || extendObj.mutations),
  function(context) 形式过不了 typeof === ''object'' 检查 → 返回 null → 扩展静默不生效(无报错难排查)。
【其他铁律】JS 模块走 SFC new Function 执行无闭包:
- 不能 import 外部模块(db/createStore/Vue/heyui 都不能 import)
- 不能引用外部变量, 只能通过 action 第一个参数 { state, commit, dispatch, rootState, getters } 访问
- 模块编码用 state.MODULECODE, 不能硬编码字面量
【读写 DataTable】rootState[ns].dt.{path};【调 API】dispatch(\"call\", { APICODE, params })。
【反例】早期示例 export default function(context){...} → 扩展从不生效。
【正例】LIB_M05 store.js 对象形式(实证可用)。',
MODIFYTIME=NOW()
WHERE ID='am_rule_extend_store_pattern';

-- --------------------------------------------------
-- 6. 按钮回调: INTERACTTYPE 枚举纠正
-- --------------------------------------------------
UPDATE tss_ai_memory SET
TITLE='按钮 EXTPARAM 回调 + INTERACTTYPE(direct/poptip) + 子表 BTNCODE',
CONTENT='【1. INTERACTTYPE 交互类型】(后端 DefineButton 强校验, 前端 generic-module 只识别 poptip)
- direct: 点击直接执行(默认)
- poptip: Poptip 组件弹确认框, POPTIPTEXT 给文案, 点确认才执行
- 不存在 none/confirm/form/select 等值(早期错误记录)
【2. 按钮行为由 BTNCODE + EXTPARAM.action 决定】
- BTNCODE=add:    EXTPARAM={\"action\":\"openForm\",\"openMode\":\"add\",\"formPageCode\":\"add\"}
- BTNCODE=edit:   EXTPARAM={\"action\":\"openForm\",\"openMode\":\"edit\",\"formPageCode\":\"add\"}
- BTNCODE=delete: INTERACTTYPE=poptip, 走 store delete action
- BTNCODE=save:   INTERACTTYPE=direct, APICODE=A04save
- BTNCODE=custom/submit/check...: APICODE 指定接口
【3. EXTPARAM 回调】
- beforeAction/afterAction 回调方法名: {\"beforeAction\":\"beforeXxx\",\"afterAction\":\"afterXxx\"} → 调扩展 JS methods
【4. 子表 tableblock 按钮 BTNCODE】
- 默认 4 个: subAdd(增)/subRemove(删)/subUp(上移)/subDown(下移)
- 自定义按钮配 tss_module_button(BTNAREA=子表路径如 DTSA), EXTPARAM 配 subTable 相关参数',
MODIFYTIME=NOW()
WHERE ID='am_rule_button_callbacks';

-- --------------------------------------------------
-- 7. 人员字段: 补长度标准
-- --------------------------------------------------
UPDATE tss_ai_memory SET
CONTENT='人员时间字段统一命名+长度: CREATEID varchar(64)(创建人ID)/CREATER varchar(16)(创建人姓名,R 后缀)/CREATETIME datetime/MODIFYID varchar(64)/MODIFER varchar(16)/MODIFYTIME datetime。全部大写无下划线。禁用 CREATEDBY/CREATEDBYNAME/CREATEDTIME(仅 tss_aidev_session 是遗留异常, 内部自洽未动)。审计日志类表(tss_dev_version)只配 CREATE 三件套不加 MODIFY; 可编辑记录类表(tss_module_template)CREATE+MODIFY 全配。m18 显示 192/48 是字节数(字符×3)。create_physical_table 工具自动补齐六件套。详见 am_rule_audit_fields_standard。',
MODIFYTIME=NOW()
WHERE ID='am_rule_metadata_perm_field';

-- --------------------------------------------------
-- 8. 共享 changeset: 补分步强制说明
-- --------------------------------------------------
UPDATE tss_ai_memory SET
CONTENT='多步 AI 分步生成时, 6 步共享同一个 changesetId(变更包 ID)。WizardStepOrchestrator 按 stepToolMap 过滤工具, 每步只暴露该步工具。
【两种模式】
- 一键生成(GenerateAllAsync): 6 步连跑共享 changeset, 跨步靠 LookupDraft* 系列从 DRAFT 项 metadata 兜底查找, 最后统一确认执行。
- 分步执行(GenerateStepAsync): 第 N 步开始前强制检查 changeset 无 DRAFT/CONFIRMED 未执行项(enforcePreviousExecuted), 有则拒绝并提示先「确认并执行」上一步——否则本步工具查 DB 找不到上一步资源。执行后 CONFIRMED→EXECUTED(ChangeSetEngine.ExecuteConfirmed)。详见 am_rule_wizard_step_enforce。',
MODIFYTIME=NOW()
WHERE ID='am_rule_wizard_changeset_shared';

-- --------------------------------------------------
-- 验证输出
-- --------------------------------------------------
SELECT ID, TITLE, PRIORITY, ISDELETED FROM tss_ai_memory WHERE ID IN (
'am_pitfall_list_row_buttons','am_pitfall_form_subpage_navigate','am_rule_resource_prefix',
'am_example_extend_store','am_rule_extend_store_pattern','am_rule_button_callbacks',
'am_rule_metadata_perm_field','am_rule_wizard_changeset_shared');
