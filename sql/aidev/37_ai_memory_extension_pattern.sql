-- ============================================================
-- AI 记忆中枢 — GenericModule 扩展层机制(Store/JS/SFC/回调)
-- 背景: 在线开发虽然不写四件套(router/store/main/add),
--       但需要写 3 种扩展文件 + 回调钩子, 是 GenericModule 的精髓
-- 内容:
--   1. 扩展 Store(@/modules/{MC}/store.js) + applyStoreExtend
--   2. 扩展 JS(@/modules/{MC}/{pageCode}.js) + loadExtendMixin
--   3. 扩展 SFC(@/modules/{MC}/{slotName}.vue) + loadSlotComponents
--   4. ISSHOW 按钮显隐回调 / 字段显隐回调
--   5. 按钮 EXTPARAM 回调 / 子表 BTNCODE 回调
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、扩展 Store(@/modules/{MC}/store.js) — applyStoreExtend
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_extend_store_pattern', 'rule', 'sfc',
'扩展 Store 文件(@/modules/{MC}/store.js)',
'在线开发列表/表单页若需要自定义 action(如 loadAcceptRefs/endisable/importExcel), 必须在 @/modules/{MC}/store.js 写扩展 Store:
【加载机制】generic-store.js 的 applyStoreExtend(moduleCode) 异步加载该文件, 合并 actions/mutations 到已注册的 Vuex 模块(Store03 基础 actions 不会被覆盖, 扩展只新增)。
【铁律】JS 模块走 SFC new Function 执行无闭包:
- ❌ 不能 import 外部模块(db/createStore/Vue/heyui 都不能用 import)
- ❌ 不能引用外部变量(只能用入参 context 的 state/getters/commit/dispatch)
- ✅ 模块编码用 state.MODULECODE, 不能硬编码字面量
【访问 Store 内部】context 提供: state / commit / dispatch / rootState / getters。读写 DataTable 用 context.rootState[ns].dt.{path} 或 dispatch("MAIN/setValue", {field, value})。
【调用 API】用 dispatch("call", {APICODE, params, moduleCode}) 复用通用通道, 不要直接 db.postData。
【保存调用】dispatch("save") 走标准 Store03 save action, 自动处理 XML/版本管理/审批流。',
'扩展Store,applyStoreExtend,@/modules,store.js,无闭包,new Function,MODULECODE,dispatch,call action',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_extend_store_pattern');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_extend_store', 'example', 'sfc',
'示例: 扩展 Store 文件(@/modules/{MC}/store.js)',
'// 文件路径: p-admin/src/modules/LIB_M07/store.js
// 注意: 不能 import, 通过 context 入参访问 store
export default function (context) {
  const { state, commit, dispatch, rootState, getters } = context;
  const ns = state.MODULECODE;  // 不能硬编码模块编码

  return {
    actions: {
      // 自定义 action: 加载关联子表(标准 open 不返回时单独拉)
      async loadAcceptRefs({ commit, dispatch, rootState }, { id }) {
        const ret = await dispatch("call", {
          APICODE: "A21",
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
      // 自定义 action: 状态切换(走 call 通用通道)
      async endisable({ commit, dispatch }, { item }) {
        const ret = await dispatch("call", {
          APICODE: "A07",
          params: { UPDATE: JSON.stringify({ ID: item.ID, ISUSE: item.ISUSE === 1 ? 0 : 1 }) },
        });
        // 回写字段到当前行
        if (ret && ret.length > 0) Object.keys(ret[0]).forEach(k => { item[k] = ret[0][k]; });
      },
    },
    mutations: {
      SET_ENTRYNUM(state, { path }) {
        // 保存前给子表行写 ENTRYNUM(行号), 控制显示顺序
        const dt = state.dt[path];
        if (dt && dt.data) dt.data.forEach((r, i) => dt.setValue("ENTRYNUM", i + 1, i));
      },
    },
  };
}
来源: p-admin/src/modules/LIB_M07/store.js + generic-store.js applyStoreExtend。',
'示例,扩展Store,LIB_M07,applyStoreExtend,call action,SET_ENTRYNUM,MODULECODE',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 8, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_extend_store');

-- -----------------------------------------------------------
-- 二、扩展 JS mixin(@/modules/{MC}/{pageCode}.js) — loadExtendMixin
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_extendjs_pattern', 'rule', 'sfc',
'扩展 JS mixin 文件(@/modules/{MC}/{pageCode}.js)',
'在线开发页面若需要扩展组件 methods/computed/lifecycle, 必须在 @/modules/{MC}/{pageCode}.js 写扩展 JS:
【加载机制】generic-module.vue / generic-form.vue 的 loadExtendMixin() 优先从 PAGECONFIG.EXTENDJS 读路径, 否则从约定路径 @/modules/{MC}/{pageCode}.js 加载。合并到组件实例(等价于 mixin)。
【铁律】同 extend-store, JS 走 SFC new Function 执行无闭包:
- ❌ 不能 import
- ❌ 不能引用外部变量
- ✅ export default 返回 { methods, computed, mounted, created, ... }
【字段访问限制】extendjs 不能用 this.FIELDNAME 直接访问字段值(组件无 mapDateTable 绑定):
- 列表当前行: this.$refs.table.currentRow 或 this.$store.state[ns].dt.QRY.data[index]
- 表单主表行: this.$store.state[ns].dt.MAIN.data[0]
- 写值: this.$store.state[ns].dt.MAIN.setValue("FIELD", value)
【可用 this 上下文】$callAction/$alert/$error/$confirm/$busy/$free/$store/$route/$refs/$emit 全局方法都可用。
【export default 结构】
export default {
  computed: { customComputed() { return ... } },
  methods: { customMethod() { this.$callAction({action:"${mc}/loadXxx", param:{}}) } },
  mounted() { ... },
}
来源: p-admin/src/components/generic-module/generic-module.vue loadExtendMixin + loadSlotComponents。',
'扩展JS,EXTENDJS,loadExtendMixin,mixin,@/modules,pageCode.js,无闭包,new Function,字段访问',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_extendjs_pattern');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_extendjs', 'example', 'sfc',
'示例: 扩展 JS 文件(@/modules/{MC}/{pageCode}.js)',
'// 文件路径: p-admin/src/modules/LIB_M07/main.js
// 注意: 不能 import, 通过 this 访问组件实例
export default {
  computed: {
    // 派生数据(从 store 取)
    activeCount() {
      const ns = this.$store.state.app.currentModuleCode;
      const data = (this.$store.state[ns] && this.$store.state[ns].dt.QRY.data) || [];
      return data.filter(r => r.ISUSE === 1).length;
    },
  },
  methods: {
    // 自定义操作(走扩展 store action)
    async customOp(row) {
      if (!await this.$confirm("确定执行该操作?")) return;
      this.$callAction({
        action: `${this.$store.state.app.currentModuleCode}/endisable`,
        param: { item: row },
        successText: "操作成功",
        successCall: () => this.$callAction({ action: `${this.$store.state.app.currentModuleCode}/query`, timeOut: 0 }),
      });
    },
    // 自定义校验
    validateBeforeSave() {
      const ns = this.$store.state.app.currentModuleCode;
      const row = this.$store.state[ns].dt.MAIN.data[0];
      if (!row.BILLCODE) { this.$error("单据编号不能为空"); return false; }
      return true;
    },
  },
  mounted() {
    console.log("generic-module + extendjs mounted");
  },
}
来源: p-admin/src/components/generic-module/generic-module.vue loadExtendMixin。',
'示例,扩展JS,EXTENDJS,mixin,$callAction,$store,currentModuleCode,validateBeforeSave',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 8, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_extendjs');

-- -----------------------------------------------------------
-- 三、扩展 SFC 槽位(@/modules/{MC}/{slotName}.vue) — loadSlotComponents
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_slots_pattern', 'rule', 'sfc',
'扩展 SFC 槽位(@/modules/{MC}/{slotName}.vue) — loadSlotComponents',
'在线开发页面若需要替换默认插槽内容, 必须在 @/modules/{MC}/{slotName}.vue 写扩展 SFC, 并在 tss_module_page.PAGECONFIG.SLOTS 注册:
【PAGECONFIG.SLOTS JSON 格式】{"header-action":"@/modules/LIB_M07/myHeader.vue","table-action":"@/modules/LIB_M07/myAction.vue"}
【列表页可用 slot】
- header-action: 顶部右侧按钮区(覆盖默认新增按钮)
- footer-action: 底部分页栏左侧(批量操作按钮)
- table-action: 表格操作列(传 { data: row } slot-scope)
- simple-query: 顶部 Search 旁快捷查询区
- body-query: 高级查询面板(dynamicQuery=false 时使用)
【表单页可用 slot】
- form-top: 表单顶部(整体替换)
- form-bottom: 表单底部(整体替换)
- field:字段名: 单字段插槽(传 { value, @input } 可双向绑定)
【SFC 完全权限】扩展 SFC 是完整 Vue 组件:
- ✅ 可以 import 白名单模块(vue/heyui/vuex/axios/@/api/db 等)
- ✅ 可以 mapDateTable 双向绑定(若用 createStore 注册过 store)
- ✅ 可以用 this.$callAction/$emit/$store 等
【加载时机】generic-module/generic-form 的 loadSlotComponents() 在 created 后异步加载, SFC 完全替换 slot 内容。
【AI 判定】用户说"自定义按钮区/操作列/表单某字段/查询面板" → 写 SFC + 配 SLOTS; 说"加自定义 action" → 写 extend-store; 说"加方法/计算属性" → 写 extend-js。',
'扩展SFC,SLOTS,loadSlotComponents,header-action,footer-action,table-action,form-top,field:字段名,白名单 import',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_slots_pattern');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_sfc_slot_table_action', 'example', 'sfc',
'示例: 扩展 SFC - table-action 操作列(@/modules/{MC}/myAction.vue)',
'<template>
  <TableItem title="操作" width="200" align="center">
    <template slot-scope="{ data }">
      <Button color="primary" size="s" @click.stop="edit(data)">编辑</Button>
      <Button color="primary" size="s" v-if="data.STATE === 1" @click.stop="submit(data)">提交</Button>
      <Poptip content="确定删除?" @confirm="del(data)">
        <Button color="red" size="s">删除</Button>
      </Poptip>
    </template>
  </TableItem>
</template>
<script>
// 扩展 SFC 是完整 Vue 组件, 可以 import 白名单模块
// 操作列直接用 this.$callAction 调扩展 store 的 action
export default {
  methods: {
    edit(row) {
      // 跳转表单页或打开弹窗(由 generic-module 配置决定)
      this.$emit("edit", row);
    },
    submit(row) {
      this.$callAction({
        action: `${this.$store.state.app.currentModuleCode}/flowSave`,
        param: { ID: row.ID, ACTIONCODE: "submit" },
        successText: "提交成功",
        successCall: () => this.$emit("refresh"),
      });
    },
    del(row) {
      this.$callAction({
        action: `${this.$store.state.app.currentModuleCode}/delete`,
        param: { items: [row] },
        successText: "删除成功",
        successCall: () => this.$emit("refresh"),
      });
    },
  },
};
</script>
PAGECONFIG.SLOTS 配置: {"table-action":"@/modules/LIB_M07/myAction.vue"}
来源: p-admin/src/components/generic-module/generic-module.vue loadSlotComponents + table-action slot。',
'示例,SFC,table-action,slot-scope,$emit,flowSave,$callAction,PAGECONFIG.SLOTS',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 7, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_sfc_slot_table_action');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_sfc_slot_field', 'example', 'sfc',
'示例: 扩展 SFC - 单字段插槽(@/modules/{MC}/custPicker.vue)',
'<template>
  <TreePicker
    :option="treeOption"
    v-model="innerValue"
    @change="onChange"
  ></TreePicker>
</template>
<script>
import heyui from "heyui";  // 扩展 SFC 可 import 白名单模块
export default {
  props: { value: { type: [String, Number], default: "" } },
  data() {
    return {
      treeOption: {
        keyName: "ID",
        titleName: "DEPTNAME",
        loadData: (id, cb) => {
          // 走扩展 store action 加载树
          this.$callAction({
            action: `${this.$store.state.app.currentModuleCode}/loadDeptTree`,
            param: { parentId: id || "root" },
            isBusy: false,
          }).then(ret => cb(ret || []));
        },
      },
    };
  },
  computed: {
    innerValue: {
      get() { return this.value; },
      set(v) { this.$emit("input", v); },  // 必须用 input 事件回写
    },
  },
  methods: {
    onChange(item) {
      // 联动其他字段: 通过 store mutation 写回主表
      const ns = this.$store.state.app.currentModuleCode;
      this.$store.state[ns].dt.MAIN.setValue("DEPTNAME", item.DEPTNAME);
      this.$store.state[ns].dt.MAIN.setValue("PARENTID", item.PID);
    },
  },
};
</script>
PAGECONFIG.SLOTS 配置: {"field:DEPTID":"@/modules/LIB_M07/custPicker.vue"}
来源: p-admin/src/components/generic-form.vue loadSlotComponents + field:字段名 slot。',
'示例,SFC,field:字段名,TreePicker,$emit input,setValue,联动,白名单 import',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 7, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_sfc_slot_field');

-- -----------------------------------------------------------
-- 四、ISSHOW 回调(按钮显隐 / 字段显隐)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_isshow_callbacks', 'rule', 'sfc',
'ISSHOW 回调(按钮显隐 / 字段显隐)',
'GenericModule 提供两类 ISSHOW 回调钩子, 写在扩展 JS 的 methods 中:
【1. 按钮 SHOWCOND 回调(tss_module_button.SHOWCOND)】
- SHOWCOND 是 JS 表达式字符串(如 "row.STATE===1"), 框架按行上下文求值
- 复杂逻辑用 ISSHOW 方法替代: 方法名约定 ISSHOW{BTNCODE}, 签名 ({ row, key, path }) => boolean
- 例: ISSHOWSUBMITMIT(row) { return row.STATE === 1 && row.AMOUNT > 0 }
【2. 字段显隐回调(resuipc 配合)】
- uiset 字段配 SHOWCOND, 框架按行求值显隐
- 复杂逻辑用 ISSHOW 方法: ISSHOW{FIELDNAME}({ row, key, path }) => boolean
- 例: ISSHOWBILLCODE({ row }) { return row.TYPE === "ORD"; }  // TYPE=ORD 才显示单据号字段
【3. Add01 mixin 内置 ISSHOW(表单按钮, 直接复用)】
- ISSHOWSAVE/DELETE: STATE 空/1
- ISSHOWSUBMIT: STATE 空/1
- ISSHOWRESUBMIT: STATE=2 / ISSHOWCHECK: STATE=2
- ISSHOWRECHECK/VERIFY: STATE=3/5/19 / ISSHOWREVERIFY: STATE=6/20
- ISSHOWINVALID: STATE=6
【方法签名铁律】ISSHOWXXX({ row, key, path }) 必须包含这三个参数, 框架自动传入。row=当前行数据, key=字段名/按钮编码, path=数据源路径(MAIN/QRY/DTSA 等)。
【注意】ISSHOW 是约定前缀, 框架在渲染前会用 reflection 调用同名方法, 不返回 boolean 时默认 true(显示)。',
'ISSHOW,SHOWCOND,按钮显隐,字段显隐,Add01,ISSHOWSAVE,ISSHOWSUBMIT,签名,reflection',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 8, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_isshow_callbacks');

-- -----------------------------------------------------------
-- 五、按钮 EXTPARAM 回调 + 子表 BTNCODE 回调
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_button_callbacks', 'rule', 'sfc',
'按钮 EXTPARAM 回调 + 子表 BTNCODE 回调',
'【1. 按钮 EXTPARAM(tss_module_button.EXTPARAM JSON)】
- 可配置 confirm 提示: {"confirm":"确定执行?"} 框架弹 Poptip 确认后再 onClick
- 可配置 beforeCall/afterCall 回调方法名: {"beforeCall":"validateXxx","afterCall":"refreshList"}
- 自定义 onClick: {"onClick":"customBtnClick"} → 调 extendjs 的 methods.customBtnClick(items)
【2. 按钮 INTERACTTYPE 交互类型】
- none: 直接调 APICODE
- confirm: 弹确认框再调
- form: 弹窗填表再调(params 带表单值)
- select: 弹选择器(员工/部门/客户)再调, EXTPARAM 配 {selectType:"emp"}
【3. 子表 tableblock 按钮 BTNCODE】
- 默认 4 个: subAdd(增) / subRemove(删) / subUp(上移) / subDown(下移)
- 自定义按钮配 tss_module_button(BTNAREA=子表路径如 DTSA), EXTPARAM={"subTable":"DTSA"}
- 子表行操作可用 ISSHOW{BTNCODE} 按行控制显隐
【4. 子表行号 ENTRYNUM】
- 保存前由扩展 store 的 mutation SET_ENTRYNUM 自动写行号(见 am_example_extend_store)
- 子表展示顺序由 ENTRYNUM 控制, 升序排列
- 后端 doOpen 子表查询自动追加 ORDER BY ENTRYNUM(若资源有该字段)',
'EXTPARAM,INTERACTTYPE,BTNCODE,subAdd,subRemove,confirm,beforeCall,afterCall,onClick,ENTRYNUM,tableblock',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 7, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_button_callbacks');

-- -----------------------------------------------------------
-- 六、保存/审批回调钩子
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_save_callbacks', 'rule', 'sfc',
'保存前后/审批回调钩子(beforeSave/afterSave/afterFlow)',
'【1. 保存前校验(extendjs.validateBeforeSave)】
extendjs 写 validateBeforeSave 方法返回 boolean, generic-form 调 save 前先校验:
validateBeforeSave() {
  const ns = this.$store.state.app.currentModuleCode;
  const row = this.$store.state[ns].dt.MAIN.data[0];
  if (!row.BILLCODE) { this.$error("单据编号不能为空"); return false; }
  return true;
}
【2. 保存后回写(afterSave)】
extendjs 写 afterSave(ret) 方法, save 完成后自动调, ret 是后端返回:
afterSave(ret) {
  if (ret && ret.length > 0) {
    // 自动刷新列表
    this.$callAction({ action: `${this.$store.state.app.currentModuleCode}/query`, timeOut: 0 });
  }
}
【3. 审批流回调(afterFlow)】
extendjs 写 afterFlow({ ID, ACTIONCODE, ret }) 方法, flowSave 完成后自动调:
afterFlow({ ID, ACTIONCODE, ret }) {
  this.$callAction({ action: `${this.$store.state.app.currentModuleCode}/open`, param: { ID } });
}
【4. 数据加载完成(onLoaded)】
extendjs 写 onLoaded({ path, data }) 方法, DataTable 数据加载完成后调:
onLoaded({ path, data }) {
  if (path === "MAIN" && data.length > 0) {
    // 主表加载完, 触发子表联动加载
    this.$callAction({ action: `${this.$store.state.app.currentModuleCode}/loadAcceptRefs`, param: { id: data[0].ID } });
  }
}
【5. 列表查询前过滤(onBeforeQuery)】
extendjs 写 onBeforeQuery(params) 方法, query action 发起前调, 可修改过滤参数:
onBeforeQuery(params) {
  params.FilterParams = params.FilterParams || {};
  params.FilterParams.CREATEID = this.$store.state.user.userInfo.ID;  // 仅看本人
}
来源: generic-module.vue + generic-form.vue 的钩子约定。',
'afterSave,afterFlow,onLoaded,onBeforeQuery,validateBeforeSave,回调钩子,extendjs',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 7, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_save_callbacks');

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 本批 8 条新增(2 rule + 2 example 扩展 Store/JS + 3 rule+example SFC + 3 rule 回调)
-- 配合 33-36 = 共 132 条种子
-- 在线开发扩展层四件套:
--   ① @/modules/{MC}/store.js (扩展 actions/mutations)
--   ② @/modules/{MC}/{pageCode}.js (扩展 methods/computed/钩子)
--   ③ @/modules/{MC}/{slotName}.vue (扩展 SFC 槽位)
--   ④ ISSHOW + EXTPARAM + afterSave/afterFlow/onLoaded 回调
