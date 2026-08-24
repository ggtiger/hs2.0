-- ============================================================
-- AI 记忆中枢 — 模式澄清(在线开发 vs 传统四件套)
-- 背景: 用户实际 90% 场景是"在线开发"(GenericModule + SFC),
--       传统四件套(router.js/store.js/main.vue/add.vue) 仅作历史参考
-- 本迁移:
--   1. 新增"在线开发模式总纲"高优先级规则(PRIORITY=10, 必注入)
--   2. 给"四件套专属"条目改 ASSETTYPE='frontend'(与 sfc=在线开发区分)
--   3. 在线开发适用机制(Store03/mapDateTable/Add01/Sel01/$callAction) 保持 sfc 不变
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、在线开发模式总纲(PRIORITY=10, 全场景必注入)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_online_dev_master', 'rule', 'wizard',
'在线开发模式总纲(GenericModule + SFC, 不需要四件套)',
'hs2.0 项目 90% 场景是"在线开发", 走低代码模式, 与传统四件套模式完全不同。
【在线开发的核心】
1. 模块/页面/按钮 通过 tss_module_page + tss_module_button 元数据配置, 不写 router.js/store.js/main.vue/add.vue 四件套
2. 通用组件 src/components/generic-module/(generic-module.vue 列表/generic-form.vue 表单), 由 PAGECONFIG JSON 驱动渲染
3. 通用 Store: getGenericStore(moduleCode) 等价于 createStore.getStore(), 但已缓存到 storeCache
4. 路由自动注册: registerGenericRoute(moduleCode, moduleData) 在 app.js initModule 中自动调用, 不需要手写 require.ensure
5. 菜单跳转: tss_func.OUTERURL 配 /g/{MODULECODE}/{PAGECODE} 即走 generic 路由
【不需要的东西(传统四件套专属)】
- ❌ 不需要 router.js(用 registerGenericRoute)
- ❌ 不需要手写 store.js(用 getGenericStore)
- ❌ 不需要 main.vue/add.vue(用 generic-module.vue/generic-form.vue)
- ❌ 不需要 require.ensure 懒加载(SFC 用 loadCompiledSFC 动态加载)
- ❌ 不需要 SelStore(generic-store 内部自动注入)
- ❌ 不需要 list-t01/add-t01 模板(generic-module 替代)
【仍然适用的机制(generic-store 内部就是 createStore)】
- ✅ Store03 标准 actions(query/open/save/delete/call/batch/flowSave)
- ✅ mapDateTable 双向绑定(通过 BaseStore.mixState 在 dt.{path} 创建)
- ✅ Add01/Sel01 mixin(generic-form 已内置)
- ✅ $callAction 调用(action 走 RS_M17 或对应模块 store)
- ✅ DataTable.getXML() 数据格式
【区分信号】
- ASSETTYPE=''sfc'' = 在线开发(SFC 组件 + GenericModule)
- ASSETTYPE=''frontend'' = 传统四件套模式(仅历史参考, AI 在线开发场景跳过)
【AI 判定】
若用户说"创建模块/新增页面/配置页面"且未明确指定"手写 router" → 默认在线开发模式, 用 generic-module + 元数据配置, 而非产出 router.js/store.js/main.vue/add.vue 四件套。',
'在线开发,GenericModule,tss_module_page,SFC,loadCompiledSFC,generic-store,registerGenericRoute,四件套,router',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_online_dev_master');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_generic_module_data_driven', 'rule', 'wizard',
'GenericModule 元数据驱动渲染(PAGECONFIG + 按钮 + 查询)',
'在线开发的页面由元数据驱动, 关键配置:
【tss_module_page 字段】MODULECODE/PAGECODE/PAGENAME/PAGETYPE(list|form)/ROUTEPATH/COMPONENTTYPE/SFCMODULEPATH/QUERYAPICODE/OPENAPICODE/SAVEAPICODE + PAGECONFIG(JSON)
【PAGECONFIG JSON】扩展点:
- SLOTS: { "header-action": "@/modules/{MC}/{slotName}.vue", "table-action": "..." } 每个插槽配一个 SFC 文件
- EXTENDJS: "@/modules/{MC}/{pageCode}.js" 动态 mixin 扩展(methods/computed 合并到 generic-module 组件实例)
- COLSPAN: 字段占宽(span 数, gen.js→single 容器)
- REPORT: report-t01 接入配置
【tss_module_button 字段】ID/PAGEID/MODULECODE/APICODE/BTNNAME/BTNTYPE/BTNAREA(header|footer|row)/INTERACTTYPE/SHOWCOND/PERMCODE/ICON/COLOR/EXTPARAM(JSON)
【按钮渲染】BTNAREA=header→header-action slot; BTNAREA=footer→footer-action slot; BTNAREA=row→table-action slot(传 row 对象)
【SHOWCOND】JS 表达式字符串, 传入 row 上下文, 返回 boolean 控制按钮显隐
【表单页】PAGETYPE=form 直接渲染 generic-form(div.generic-form-page 包裹), 不用 rs-modal 弹窗
来源: p-admin/src/components/generic-module/generic-module.vue + generic-form.vue + generic-store.js。',
'GenericModule,tss_module_page,PAGECONFIG,SLOTS,EXTENDJS,tss_module_button,BTNAREA,SHOWCOND',
'assistant,aidev,wizard', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_generic_module_data_driven');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_generic_store_usage', 'rule', 'wizard',
'在线开发 Store 用法(getGenericStore + applyStoreExtend)',
'在线开发用 getGenericStore 替代 createStore.getStore:
【getGenericStore(moduleCode)】等价于 createStore.getStore(), 但缓存到 storeCache(同模块第二次调用直接命中缓存)。位置: src/components/generic-module/generic-store.js。
【模块级 Store 扩展】applyStoreExtend(moduleCode) 异步加载 @/modules/{moduleCode}/store.js, 合并 actions/mutations 到已注册的 Vuex 模块。注意: JS 模块走 SFC new Function 执行无闭包, 不能引用外部变量, 模块编码用 state.MODULECODE。
【列表页 generic-module.vue】自动加载 PAGECONFIG + 按钮 + 查询字段, 用 rs-table-list 渲染, 按钮区域 BTNAREA 分发。
【表单页 generic-form.vue】用 rs-form-edit 渲染字段, mode=single/twocolumn, Add01 mixin 已内置(save/del/submit/check/verify)。
【扩展点加载】loadExtendMixin() 优先从 PAGECONFIG.EXTENDJS 加载 SFC JS, 否则从约定路径 @/modules/{moduleCode}/{pageCode}.js; loadSlotComponents() 加载 SLOTS 配置的 SFC。
来源: p-admin/src/components/generic-module/generic-store.js + generic-module.vue + generic-form.vue。',
'getGenericStore,applyStoreExtend,generic-store,generic-module,generic-form,loadExtendMixin,loadSlotComponents',
'assistant,aidev,wizard', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_generic_store_usage');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_dev_pattern', 'rule', 'sfc',
'SFC 在线开发模式(loadCompiledSFC + remote-route)',
'在线开发写 SFC Vue 组件, 不写 router/store:
【运行时加载】loadCompiledSFC(modulePath) 从 DB 拉取 COMPILEDCODE 字段, 用 new Function() 执行返回 Vue 组件 options。位置: p-admin/src/sfc-loader/index.js。
【远程路由】<remote-route :modulePath="xxx"></remote-route> 动态渲染在线 SFC, props.modulePath 必填。位置: p-admin/src/sfc-loader/remote-route.vue。
【桥梁白名单】SFC 内可 import 的模块: vue/heyui/vuex/axios/@/api/db/@/store/@/store/createStore/@/store/Store03/@/store/BaseStore/@/mixins/add01/@/components/generic-module/generic-store/rs-vcore/utils/Date(完整清单见 module-bridge.js)。非白名单的 npm 包禁止直接 import(需先声明 DEPS 预加载)。
【保存】SFC 资产保存用 store 的 add action(INIT+ADD), 不手拼 XML。保存路径: setValue 各字段 → store.dispatch(''RS_M17/save'')。SFC 编辑器双按钮: 保存=SKIPVERSION=1 快速保存不留版本, 提交=CHANGENOTE+生成版本。
【编译流水线】compileSFC(sourceCode, modulePath, fileType): vue-template-compiler.parseComponent → compiler.compile(模板→render) → @babel/standalone(import→require) → less(style+scoped) → extractDeps(import 路径预加载) → new Function 执行。
来源: p-admin/src/sfc-loader/ + src/components/generic-module/。',
'SFC,loadCompiledSFC,remote-route,module-bridge,compileSFC,桥梁白名单,saveAsset,RS_M17',
'assistant,aidev,wizard,sfc', '0,1,2,3,4,5', 8, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_dev_pattern');

-- -----------------------------------------------------------
-- 二、把"传统四件套专属"条目 ASSETTYPE 从 sfc 改成 frontend
--    (与 sfc=在线开发区分, AI 在 wizard/aidev 在线开发场景跳过这些)
-- -----------------------------------------------------------

UPDATE tss_ai_memory SET ASSETTYPE='frontend'
WHERE ID IN (
  'am_example_router_js',
  'am_example_store_js',
  'am_example_main_vue',
  'am_example_add_vue',
  'am_rule_frontend_router_ensure',
  'am_rule_frontend_store_action'
) AND ASSETTYPE='sfc';

-- 给这些条目的 TAGS 补"传统四件套"标记
UPDATE tss_ai_memory SET TAGS = CONCAT(TAGS, ',传统四件套,router.js,store.js')
WHERE ID='am_example_router_js' AND TAGS NOT LIKE '%传统四件套%';
UPDATE tss_ai_memory SET TAGS = CONCAT(TAGS, ',传统四件套,store.js')
WHERE ID='am_example_store_js' AND TAGS NOT LIKE '%传统四件套%';
UPDATE tss_ai_memory SET TAGS = CONCAT(TAGS, ',传统四件套,main.vue')
WHERE ID='am_example_main_vue' AND TAGS NOT LIKE '%传统四件套%';
UPDATE tss_ai_memory SET TAGS = CONCAT(TAGS, ',传统四件套,add.vue')
WHERE ID='am_example_add_vue' AND TAGS NOT LIKE '%传统四件套%';

-- -----------------------------------------------------------
-- 三、把这些机制类条目(Store03/Add01/Sel01/$callAction/ensureModule/createStore)
--    调整 TAGS 注明"在线开发 generic-store 内部仍用"
--    (不降权, 因为 generic-store.js 内部就是 createStore + Store03 + Add01 + Sel01)
-- -----------------------------------------------------------

UPDATE tss_ai_memory
SET TAGS = CONCAT(TAGS, ',在线开发复用,generic-store')
WHERE ID IN (
  'am_rule_store03_actions',
  'am_rule_add01_mixin',
  'am_rule_sel01_mixin',
  'am_rule_callaction_usage',
  'am_rule_ensure_module',
  'am_rule_createstore_unregister'
) AND TAGS NOT LIKE '%在线开发复用%';

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 本批 4 条新增 + 12 条更新(6 改 ASSETTYPE=frontend, 6 补"在线开发复用"标记)
-- 配合 33/34/35 = 共 124 条种子
-- 区分:
--   ASSETTYPE='sfc'     → 在线开发 SFC + GenericModule
--   ASSETTYPE='frontend'→ 传统四件套模式(仅历史参考)
--   ASSETTYPE='wizard'  → 在线开发流程总纲
