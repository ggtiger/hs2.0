-- ============================================================
-- RS_FEE demo 五项配置修复 + 对应 pitfall 沉淀
-- 背景: 用户实测发现 5 个向导生成质量问题:
--   1. FEETYPE 下拉 SELECTDATA 写成 DICTCODE(D0710), 应写 DICTNAME(费用类型)
--   2. main 页 ADVQUERYAPICODE 未绑定高级查询接口 A03
--   3. PAGECONFIG.EXTENDJS 指向的扩展 JS 资产未生成(main.js/add.js/store.js)
--   4. 列表页不需要 row 编辑/删除按钮(行点击即打开表单)
--   5. 添加按钮不显示: 菜单 FUNCCODE=LI_M_FEE 与权限点 FUNCPOINTCODE=RS_FEE/A04 双重错误,
--      正确是 FUNCCODE=MODULECODE(RS_FEE) + FUNCPOINTCODE=纯APICODE(A04),
--      fpoints key=FUNCCODE/FUNCPOINTCODE 才能匹配按钮 PERMCODE
-- 日期: 2026-07-20
-- ============================================================

-- --------------------------------------------------
-- 1. FEETYPE SELECTDATA: DICTCODE → DICTNAME
-- --------------------------------------------------
UPDATE tss_resuipc SET SELECTDATA='费用类型'
WHERE RESOURCEID='vck_project_fee_001' AND FIELDNAME='FEETYPE';

-- --------------------------------------------------
-- 2. main 页绑定高级查询接口
-- --------------------------------------------------
UPDATE tss_module_page SET ADVQUERYAPICODE='A03' WHERE ID='mp_fee_list';

-- --------------------------------------------------
-- 3. 生成扩展 JS 骨架(store.js + main.js + add.js)
-- --------------------------------------------------
INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, MODULEPATH, FILETYPE, SOURCECODE, VERSION, REMARK, ISDELETED, CREATETIME)
SELECT 'ca_rs_fee_store', 'js', 'RS_FEE_store', 'store.js', '@/modules/RS_FEE/store.js', 'JS',
'// 模块级 store 扩展 — RS_FEE
// 由 generic-store.js applyStoreExtend 加载, 合并 actions/mutations 到 Vuex 模块
// 注意: 不能 import, 通过 context 入参访问 store
export default function (context) {
  const { state, commit, dispatch, rootState, getters } = context;
  const ns = state.MODULECODE;  // 不能硬编码模块编码

  return {
    actions: {
      // 示例: 自定义 action(走 call 通用通道)
      // async myAction({ dispatch }, { id }) {
      //   const ret = await dispatch("call", { APICODE: "A51", params: { id } });
      //   return ret;
      // },
    },
    mutations: {
      // 示例: 自定义 mutation
      // SET_XXX(state, { path, value }) { ... },
    },
  };
}',
1, 'RS_FEE 模块 store 扩展骨架(向导自动生成)', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_code_asset WHERE MODULEPATH='@/modules/RS_FEE/store.js');

INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, MODULEPATH, FILETYPE, SOURCECODE, VERSION, REMARK, ISDELETED, CREATETIME)
SELECT 'ca_rs_fee_main', 'js', 'RS_FEE_main', 'main.js', '@/modules/RS_FEE/main.js', 'JS',
'// 页面扩展 JS — main 页(列表)
// 由 generic-module.vue loadExtendMixin 加载, 合并 methods/computed 到组件实例
export default {
  methods: {
    // ISSHOW 方法 — 框架自动传入 { row, key, path }
    // 示例: 按行状态控制按钮显隐
    // ISSHOWDELETE({ row, key, path }) {
    //   return row && row.STATE === 1;
    // },
  },
  computed: {
    // 无参显隐: 返回 true 显示 / false 隐藏
    // ISSHOWADD() { return true; },
  },
  // init 在 created 阶段调用(早于 mounted)
  init() {},
  // mounted 在组件 mounted 后调用
  mounted() {},
};',
1, 'RS_FEE 列表页扩展骨架(向导自动生成)', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_code_asset WHERE MODULEPATH='@/modules/RS_FEE/main.js');

INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, MODULEPATH, FILETYPE, SOURCECODE, VERSION, REMARK, ISDELETED, CREATETIME)
SELECT 'ca_rs_fee_add', 'js', 'RS_FEE_add', 'add.js', '@/modules/RS_FEE/add.js', 'JS',
'// 页面扩展 JS — add 页(表单编辑)
// 由 generic-form.vue loadExtendMixin 加载, 合并 methods/computed 到组件实例
export default {
  methods: {
    // ISSHOW 方法 — 框架自动传入 { row, key, path }
    // 示例: 新增时(无ID)隐藏某字段, 编辑时显示
    // ISSHOWCREATER({ row, key, path }) {
    //   if (!row) return false;
    //   return !!row.ID;
    // },
    // 表单回调: 保存前校验(返回 false 阻止保存)
    // validateBeforeSave() { return true; },
    // 保存后回调
    // afterSave() {},
  },
  computed: {},
  init() {},
  mounted() {},
};',
1, 'RS_FEE 表单页扩展骨架(向导自动生成)', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_code_asset WHERE MODULEPATH='@/modules/RS_FEE/add.js');

-- --------------------------------------------------
-- 4. 删除 row 编辑/删除按钮(行点击即打开表单, 不需要 row 按钮)
-- --------------------------------------------------
DELETE FROM tss_module_button WHERE ID IN ('mb_fee_edit', 'mb_fee_delete');

-- --------------------------------------------------
-- 5. 修复菜单 FUNCCODE + 权限点 FUNCPOINTCODE
--    fpoints key = FUNCCODE/FUNCPOINTCODE, 按钮 PERMCODE=RS_FEE/A04 才能匹配
-- --------------------------------------------------
UPDATE tss_func SET FUNCCODE='RS_FEE' WHERE ID='fn_fee_001';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A01' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A01';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A02' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A02';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A03' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A03';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A04' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A04';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A07' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A07';
UPDATE tss_funcpoint SET FUNCPOINTCODE='A09' WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='RS_FEE/A09';
-- 补 A03 权限点(高级查询)
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT REPLACE(UUID(),'-',''), 'fn_fee_001', 'A03', '高级查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID='fn_fee_001' AND FUNCPOINTCODE='A03');

-- --------------------------------------------------
-- 6. pitfall 沉淀(5 条)
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_selectdata_dictname', 'pitfall', 'metadata',
'下拉字段 SELECTDATA 必须写 DICTNAME(字典名称), 不能写 DICTCODE(字典编码)',
'【症状】select 字段下拉打开空白或显示原始值, 前端把 SELECTDATA 当自定义内联数据处理。
【根因】前端 Select 从 store.state.app.dicts[SELECTDATA] 读选项, dicts 的 key 是 DICTNAME(中文名称, 启动时 initDict 全量加载 + heyui.addDict 注册)。写 DICTCODE(D0710) 在 dicts 里查不到 → 下拉空白。
【铁律】resuipc.SELECTDATA = tss_dict.DICTNAME(如 ''费用类型''), 不是 DICTCODE(如 ''D0710''), 更不是 k:v 内联字符串。
【反例】RS_FEE FEETYPE SELECTDATA=D0710 → 下拉空白。
【正例】SELECTDATA=''费用类型'' + tss_dict(DICTNAME=费用类型) + tss_dictitem(travel/差旅费, material/材料费, service/服务费, other/其他)。',
'SELECTDATA,DICTNAME,DICTCODE,下拉,字典,select,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_selectdata_dictname');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_advquery_apicode_binding', 'pitfall', 'wizard',
'列表页必须绑定高级查询接口: tss_module_page.ADVQUERYAPICODE=A03',
'【症状】列表页高级查询面板点查询无反应/接口不存在。
【铁律】PAGETYPE=list 的 tss_module_page 三个接口字段必须齐:
  QUERYAPICODE=''A01''     (列表模糊查询)
  ADVQUERYAPICODE=''A03''  (高级查询, 对应 ACTIONCODE=advQuery 的接口)
  OPENAPICODE=''A02''      (打开单条)
  SAVEAPICODE=''A04''      (保存, form 页也配)
【反例】RS_FEE main 页只配 QUERYAPICODE=A01, ADVQUERYAPICODE=NULL → 高级查询废。
【正例】QUERYAPICODE=A01 + ADVQUERYAPICODE=A03 + OPENAPICODE=A02 + SAVEAPICODE=A04。',
'ADVQUERYAPICODE,QUERYAPICODE,高级查询,A03,module_page,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_advquery_apicode_binding');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_extendjs_must_generate', 'pitfall', 'wizard',
'PAGECONFIG.EXTENDJS 指向的扩展 JS 资产必须同步生成到 tss_code_asset',
'【症状】PAGECONFIG 配了 EXTENDJS=@/modules/{MC}/main.js 但 tss_code_asset 无此 MODULEPATH 记录 → 扩展加载静默失败(catch 忽略), 用户以为有扩展实际没生效。
【铁律】向导配 EXTENDJS 时必须同时在 tss_code_asset 生成骨架资产:
  ASSETTYPE=''js'', FILETYPE=''JS'', MODULEPATH=''@/modules/{MC}/{pageCode}.js''
  骨架包含: methods(ISSHOW 方法/回调) + computed(ISSHOW 无参) + init() + mounted()
  模块级 store 扩展: MODULEPATH=''@/modules/{MC}/store.js'', export default function(context) 返回 {actions, mutations}
【三个层级】
  1. store 扩展: @/modules/{MC}/store.js (applyStoreExtend 加载, function(context) 形式)
  2. 页面扩展: @/modules/{MC}/{pageCode}.js (loadExtendMixin 加载, 对象形式)
  3. SFC slot: @/modules/{MC}/{slotName}.vue (loadSlotComponents 加载)
【反例】RS_FEE PAGECONFIG.EXTENDJS 配了但资产没生成。
【正例】RS_FEE 修复后: store.js + main.js + add.js 三个骨架资产齐全。',
'EXTENDJS,扩展JS,tss_code_asset,骨架,store.js,loadExtendMixin,applyStoreExtend,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_extendjs_must_generate');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_row_buttons_optional', 'rule', 'wizard',
'row 编辑/删除按钮按需配置: 行点击打开表单时可不配 row 按钮',
'【规则】generic-module 列表行点击(clickRow)默认打开表单页编辑, 因此:
  - 简单 CRUD 模块: row 按钮可不配(点击行进表单, 表单 footer 有保存/删除/取消)
  - 需要行内快捷操作才配 row 按钮(如行内删除/行内状态切换)
【判断标准】用户没明确要求行内按钮 → 不配 row 按钮, 保持列表简洁。
【反例】RS_FEE 最初配了 row 编辑+删除, 用户反馈不需要(行点击即可编辑)。
【注意】与 am_pitfall_list_row_buttons 不冲突: 那条针对"用户需要行操作但没配"的场景, 本条针对"默认不必配"的简约原则。默认行为=行点击打开表单。',
'row按钮,行点击,clickRow,简约,按需配置',
'assistant,aidev,wizard', '0,1,2,3,4,5', 7, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_row_buttons_optional');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_funcpoint_code_format', 'pitfall', 'metadata',
'权限点 FUNCPOINTCODE 只写纯 APICODE(A01/A04), FUNCCODE 必须与 MODULECODE 一致',
'【症状】按钮配了 PERMCODE 但页面上不显示(v-per 指令 display:none)。
【根因】前端 fpoints 权限表 key = FUNCCODE + ''/'' + FUNCPOINTCODE(app.js:82)。按钮 PERMCODE 按 ''{MODULECODE}/{APICODE}'' 匹配(如 RS_FEE/A04)。因此:
  - FUNCCODE 必须 = MODULECODE(如 RS_FEE), 不能另起编码(如 LI_M_FEE)
  - FUNCPOINTCODE 只写纯 APICODE(如 A04), 不能带模块前缀(如 RS_FEE/A04 会变成 key=RS_FEE/RS_FEE/A04)
【铁律】
  tss_func.FUNCCODE = MODULECODE
  tss_funcpoint.FUNCPOINTCODE = APICODE(A01/A02/A03/A04/A07...)
  按钮 PERMCODE = MODULECODE/APICODE
【反例】RS_FEE: FUNCCODE=LI_M_FEE + FUNCPOINTCODE=RS_FEE/A04 → key=LI_M_FEE/RS_FEE/A04, 按钮 PERMCODE=RS_FEE/A04 永不匹配 → 添加按钮不显示。
【正例】LIB_M07: FUNCCODE=LIB_M07 + FUNCPOINTCODE=A04 → key=LIB_M07/A04 与按钮 PERMCODE 一致。',
'FUNCPOINTCODE,FUNCCODE,PERMCODE,v-per,权限点,按钮不显示,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_funcpoint_code_format');

-- 完成: 5 项修复 + 5 条 pitfall
