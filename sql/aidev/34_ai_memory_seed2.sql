-- ============================================================
-- AI 记忆中枢 — 种子数据第二批(踩坑/术语/调试经验/关键 ID)
-- 内容: 从 MEMORY.md/CLAUDE.md 二次提炼, 补充 33_ai_memory_seed.sql 未覆盖项
--       覆盖 OnlyOffice/前端调试/SFC 平台/低代码通用模块/@ui 过滤器/关键 ID/业务对象 等
-- 来源: SOURCE='auto_seed' (后续可由用户编辑/删除)
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、关键 ID / 业务对象(glossary, PRIORITY=3)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_key_ids_modules', 'glossary', 'general',
'核心模块 MODULEID 与父菜单',
'LI_M02 MODULEID=84c09562-13e6-11ea-9e8d-00163e067045, 父菜单 UPFUNCID=735b6a1a-13e6-11ea-9e8d-00163e067045。LI_M00 父菜单 UPFUNCID=1e38586dacef48559ddd565f9f79de59。R02_M07 MODULEID=r02_m07_module_001。RS_M17(SFC 在线开发) MODULEID=rs_m17_module_001, 父菜单 UPFUNCID=3e3c83ce2b3c475b82902478c89c27c0(系统管理)。RS_M18(模块配置) MODULEID=rs_m18_module_001。RS_M00(全局) MODULEID=8a4059b5fb5348cda1128cd68410d16f。',
'moduleid,LI_M02,RS_M17,RS_M00,菜单,UPFUNCID',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_key_ids_modules');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_key_ids_resources', 'glossary', 'general',
'核心 DATAVIEW RESOURCEID',
'VCK_ORECORD(检测记录视图)=d3eb0413e7bc4a7ebda595e7d6dd2214。VCK_ACCEPT(受理视图)=01fa46b049a94481a359c04a920431c8。VCK_LOGISTICS(物流)=vck_logistics_001, VCK_LOGISTICS_NODE=vck_logistics_node_001。VCK_PROJECT_FEE(费用)=vck_project_fee_001。VCK_CALIBRATION_RULES(校准规则)=vck_calib_rules_001。VCK_SFC_TEMPLATE(SFC 模板)=tbs_sfc_tpl_001。VCK_MODULE_PAGE=vck_module_page_001, VCK_MODULE_BUTTON=vck_module_button_001。VSS_RESUIPC=b45edaad63494430be9e7731b7aed951, TBS_RESUIPC=ec18e3a20a314b8fb912a2991b6b5205。VRP_PROJECT_FEE_SUM=vrp_fee_sum_001(RESOURCETYPE=SQL, FILTERSQL=SS0020, LI_M031 A20)。LI_M031 MODULEID=471464500d3c409ab9cb72d6df581675。',
'RESOURCEID,VCK_ORECORD,VCK_ACCEPT,资源ID',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_key_ids_resources');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_business_objects', 'glossary', 'general',
'业务对象(计量检测行业)',
'ORECORD=检测记录(主业务对象, 表 tck_orecord); ACCEPT=受理; CERT=证书; CUST=客户(tbs_cust, CUSTCODE/CUSTNAME); EMP=员工(tbs_emp, EMPNAME); DEPT=部门(tbs_dept); REGUITEM=检定项目(tbs_reguitem, ITEMNAME); LOGISTICS=物流(tss_logistics); PROJECT_FEE=项目费用(tss_project_fee); CALIBRATION_RULES=校准规则(tbs_calibration_rules)。华溯计量管理系统面向计量检测/校准行业 LIMS。',
'业务对象,ORECORD,ACCEPT,CERT,计量,LIMS',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_business_objects');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_billstate_codes', 'glossary', 'general',
'单据状态码(BillState)',
'1=待提交 / 2=待审核(复核) / 5=待审批 / 6=已审批 / 10=已签发 / 12=已驳回。对应 APICODE: A17(提交)/A12(复核)/A14(审批)/A16(驳回)/A13(撤销复核)/A15(撤销审批)。内置单据状态流转引擎 BillState+BillFlow, 支持驳回和撤销。',
'单据状态,BillState,BillFlow,审核,审批,APICODE',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_billstate_codes');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_response_format', 'glossary', 'general',
'统一响应格式',
'JSON 格式: { Code: 200, Data: {...}, Message: "" }。Code=200 成功, Code=501 登录超时, Code=500 内部错误。C# JSON 序列化: 不使用驼峰(DefaultContractResolver), 忽略 Null 值, 忽略循环引用。',
'响应,Code,JSON,序列化',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_response_format');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_dict_codes', 'glossary', 'general',
'数据字典编码段(D06xx/D07xx)',
'D06xx 系列=aidev 状态字典。D07xx 系列=低代码/AI 集成字典: D0701 版本对象类型 / D0702 版本操作类型 / D0703 场景传输 / D0704 AI 工具集 / D0705 上下文源 / D0706 执行类型 / D0707 业务分类 / D0708 字段占宽。tss_resuipc.SELECTDATA 写 DICTNAME 而非 DICTCODE。',
'字典,D06xx,D07xx,DICTNAME,SELECTDATA',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_dict_codes');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_modulesystem_paths', 'glossary', 'general',
'tss_moudlepath PATHNAME 约定',
'QRY=列表数据源 / QQRY=高级查询数据源 / SEL=选择器数据源 / MAIN=主表数据源 / DTSA-DTSF=子表数据源(主子表关系)。tss_moudlepathrel 配主子表关系(PATHNAMEA=MAIN, PATHNAMEB=DTSx, RFIELDSA/B 为外键字段)。',
'PATHNAME,QRY,QQRY,MAIN,DTS,主子表',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_modulesystem_paths');

-- -----------------------------------------------------------
-- 二、ORM 高级规则(rule, PRIORITY=4-5)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_schema_no_cache', 'rule', 'metadata',
'SchemaManage.GetResource 每次查数据库(无缓存)',
'ORM 元数据每次调用都查数据库, 无内存缓存。频繁查询时考虑业务层缓存(Resource.UisetFields 等)。BuildSQL01 根据 Resource+ResourceFilter+ResourceField 动态构建 SQL, BuildQuery 自动 LEFT JOIN 引用字段, NVelocity 解析过滤器, MySQL LIMIT 分页。',
'SchemaManage,缓存,GetResource,BuildSQL',
NULL, '1,2', 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_schema_no_cache');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_dataview_datatableresourceid', 'rule', 'metadata',
'DATAVIEW 必须配 TABLERESOURCEID 指向物理表',
'tss_resource 中 DATAVIEW(VCK/VSS) 资源的 TABLERESOURCEID 必须指向对应的物理表(TBS)资源 ID。这样 ORM 才能正确建立视图与物理表的字段关联, REFFIELDID 才能正确解析。漏配会导致 doOpen/doSave 数据无法回写物理表。',
'DATAVIEW,TABLERESOURCEID,物理表,视图',
NULL, '1', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_dataview_datatableresourceid');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_iskey_guid', 'rule', 'metadata',
'ISKEY=1 的字段必须配 KEYGENTYPE=GUID',
'tss_resfield 中主键字段(ISKEY=1)必须配置 KEYGENTYPE=GUID, ORM save 时自动生成 GUID 写入。漏配会导致主键为空, save 失败。',
'ISKEY,KEYGENTYPE,GUID,主键',
NULL, '1', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_iskey_guid');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_resuipc_editsort_null', 'pitfall', 'metadata',
'tss_resuipc EDITSORT 大量为 NULL, 不能用 WHERE 过滤',
'LI_M02 VCK_ORECORD 所有字段 EDITSORT=NULL, 按字段过滤 EDITSORT>0 会全部丢失。查询字段配置时必须 LEFT JOIN 而非 WHERE 过滤。LISTSORT/QUERYSORT/EDITSORT NULL 值普遍存在, 过滤需谨慎。',
'select f.* from tss_resuipc f where f.EDITSORT>0', '改用 LEFT JOIN tss_resuipc 并在 SELECT 层 IFNULL 处理, 不在 WHERE 过滤',
'EDITSORT,resuipc,LISTSORT,NULL,LEFT JOIN',
NULL, '2,4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_resuipc_editsort_null');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_extendjs_sfcslot', 'rule', 'sfc',
'页面扩展机制(EXTENDJS + SLOTS)',
'PAGECONFIG JSON 两个扩展点: ① EXTENDJS - 动态 mixin 扩展, loadExtendMixin() 优先从 PAGECONFIG.EXTENDJS 加载 SFC JS, 否则从约定路径 @/modules/{moduleCode}/{pageCode}.js, 合并 methods/computed 到组件实例。② SLOTS - 对象 key=slot 名 value=SFC 路径, 每个 slot 配一个 .vue 文件, loadSlotComponents() 加载, SFC 完全替换 slot。列表页支持 header-action/footer-action/table-action(传 row)/simple-query/body-query; 表单页支持 form-top/form-bottom/field:字段名(传 value/@input)。',
'EXTENDJS,SLOTS,mixin,扩展,slot,loadExtendMixin',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_extendjs_sfcslot');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_generic_store_extend', 'rule', 'sfc',
'模块级 store 扩展(applyStoreExtend)',
'generic-store.js 的 applyStoreExtend(moduleCode) 异步加载 @/modules/{moduleCode}/store.js, 合并 actions/mutations 到已注册的 Vuex 模块。用于在不修改 generic-store 框架代码的前提下, 为特定模块添加自定义 action。',
'store,扩展,applyStoreExtend,generic-store,Vuex',
NULL, '4', 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_generic_store_extend');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_dev_version_storage', 'rule', 'metadata',
'版本管理(tss_dev_version + cfg)',
'版本管理双表: tss_dev_version(版本快照) + tss_dev_version_cfg(配置)。DevVersionService 在 _doSave/doDelete/doUpdate 三个入口统一拦截, 自动生成版本快照。版本中心页面 s01/m22。回滚走 RDevVersionController。版本链语义: v(n).BEFORE = v(n-1).AFTER(快速保存的改动折叠进下次提交)。',
'版本,tss_dev_version,DevVersionService,回滚,版本链',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_dev_version_storage');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_script_flow', 'rule', 'csharp',
'编排接口(APITYPE=script, 多步骤组合)',
'编排接口用于复杂业务流程, DataController case "script" + doScriptFlowApi 方法。APIPARAM 存步骤 JSON 数组, 步骤类型: sql/query/if/update/return。StepContext 共享变量, @_STEP_key_ 格式注入 NVelocity。轻量条件求值器(EvalSimpleExpr)支持 &&/||/!/>/</>=/<=/==/!=。单事务执行。m18 config.vue 提供"编排接口"按钮编辑器(左右分栏+AI 对话面板)。RS_M18 A12=保存步骤JSON, A13=新建空编排接口。',
'编排接口,script,APITYPE,doScriptFlowApi,StepContext,EvalSimpleExpr',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_script_flow');

-- -----------------------------------------------------------
-- 三、@ui 过滤器自动生成(rule, PRIORITY=5)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_ui_filter_placeholder', 'rule', 'sql',
'@ui 过滤器自动生成(FILTERSQL 占位符)',
'tss_resfilter.FILTERSQL 支持 @ui / @ui:adv / @ui:adv:RESOURCEID 占位符, 自动生成过滤条件替代手写 NVelocity。@ui=F01 模糊搜索, @ui:adv=F02 高级查询。@ui 可在 FILTERSQL 任意位置(前/后/中间), 用正则替换后统一走 ParseSQL。',
'@ui,过滤器,FILTERSQL,F01,F02,自动生成',
NULL, '2,3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_ui_filter_placeholder');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_querymode_priority', 'rule', 'metadata',
'QUERYMODE 推导优先级',
'tss_resuipc 新增 QUERYMODE(varchar) 字段, 值 like/eq/in/range。推导优先级: QUERYMODE 显式 > EDITTYPE/QUERYTYPE 推导 > ISKEY/REFRESOURCEID→eq > FIELDTYPE 推导 > 默认 eq。varchar 默认 eq(非 like), 因多数是 ID/CODE 编码字段。F01 模式用 LISTSORT>0 列表字段中 DeriveQueryMode=like 的字段进 INPUT OR 块。',
'QUERYMODE,like,eq,in,range,FIELDTYPE,varchar',
NULL, '2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_querymode_priority');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_uisetfields_cache', 'rule', 'metadata',
'UisetFields 缓存到 Resource',
'GetResource 时一次性加载到 Resource.UisetFields, BuildFilterFromUI 直接使用, 避免额外查询。UisetField 模型字段: FIELDNAME/LABELNAME/EDITTYPE/QUERYTYPE/QUERYMODE/QUERYSORT/FIELDTYPE/REFRESOURCEANAME/REFFIELDNAME/REFFIELDID。',
'UisetFields,缓存,Resource,BuildFilterFromUI',
NULL, '2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_uisetfields_cache');

-- -----------------------------------------------------------
-- 四、前端调试经验(pitfall, 高价值踩坑)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_keepalive_resize', 'pitfall', 'sfc',
'keep-alive Tab 切换后表格滚动条错乱',
'keep-alive 缓存的页面切走后调整窗口大小再切回来, HeyUI Table 表格滚动条错乱。优先复用已有机制: 直接触发 window.dispatchEvent(new Event("resize")) 让组件自己响应, 比手动调 resize()、Vue.mixin 注入 activated 钩子等间接方案更简单可靠。修复方式: main.vue 的 $route watcher 中延迟触发 window.resize 事件。',
'手动调 component.resize()', '改用: window.dispatchEvent(new Event("resize"))',
'keep-alive,resize,HeyUI,Table,Tab,滚动条',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_keepalive_resize');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_vue_mixin_global', 'pitfall', 'sfc',
'Vue.mixin 全局副作用陷阱',
'Vue.mixin 会注入到所有组件, activated 钩子会在每个被 keep-alive 缓存的组件上触发, 产生大量无效调用, 应避免全局 mixin。如非必要, 不要用 Vue.mixin 全局注册生命周期钩子。',
'Vue.mixin({ activated: ... })', '改用: 在需要的组件内单独定义 activated, 或用 $route watcher 替代',
'Vue.mixin,mixin,activated,全局,副作用',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_vue_mixin_global');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_computed_shared_ref', 'pitfall', 'sfc',
'Vue 2 $options.computed 共享引用陷阱',
'this.$options.computed 在 Vue 2 中是组件定义级别的共享对象引用(mergeOptions 的 defaultStrat 返回 parentVal)。在 beforeCreate 中 this.$options.computed[key]=xxx 会污染所有实例。generic-form.vue 曾因此导致第一个表单的 $MAIN 被所有后续实例继承(if(!computed[key]) 守卫只能阻止覆盖, 不能阻止读取)。',
'this.$options.computed[key] = xxx', '改用: this.$options.computed = Object.assign({}, this.$options.computed) 创建实例级副本',
'Vue2,computed,$options,共享引用,beforeCreate,pollute',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_computed_shared_ref');

-- -----------------------------------------------------------
-- 五、OnlyOffice 字段插入(pitfall)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_onlyoffice_no_connector', 'pitfall', 'csharp',
'OnlyOffice DS 9.4.0 CE 没有 createConnector',
'OnlyOffice Document Server 9.4.0 Community Edition 没有 docEditor.createConnector() 方法。DocsAPI.DocEditor 只有 destroyEditor/serviceCommand/insertImage 等基础方法。不要在代码中调用 createConnector。',
'OnlyOffice,createConnector,DocsAPI,CE,9.4.0',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_onlyoffice_no_connector');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_onlyoffice_plugin_deploy', 'rule', 'csharp',
'OnlyOffice 字段插入插件部署流程',
'Docker onlyoffice-ds(port 8088, v9.4.0 CE) 内插件路径 /var/www/onlyoffice/documentserver/sdkjs-plugins/fieldinserter/。修改 index.js 后必须: rm -f *.gz && gzip -k -9 index.js && supervisorctl restart all。还要改 config.json version + index.html ?v=xxx cache-buster。浏览器端 Ctrl+Shift+R 强刷 + 注销 Service Worker 解决缓存。',
'OnlyOffice,插件,部署,gzip,supervisorctl,缓存',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_onlyoffice_plugin_deploy');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_onlyoffice_highlight', 'rule', 'csharp',
'OnlyOffice 内容控件高亮方案(双通道)',
'插件用 Asc.plugin.callCommand(() => Api.GetDocument().GetCurrentContentControl().GetTag()) 每 600ms 检测, 通过 window.top.postMessage + POST /current-selection 双通道上报。前端监听 message 事件 + 轮询后端获取 tag, 调用 highlightByTag 高亮字段。Content Control 类型: Inline=2(文本), Block=1(表格/html), Picture=单独方法 AddContentControlPicture。',
'OnlyOffice,高亮,ContentControl,callCommand,postMessage',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_onlyoffice_highlight');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_onlyoffice_orm_dropdown', 'pitfall', 'csharp',
'OnlyOffice 后端避免 ORM Query 取下拉数据',
'WordTemplateController(POST /field-queue 入队, GET /field-queue 插件 500ms 拉取)无用户认证上下文, 调 ORM Query 接口会返回空。改用 DB.GetDBHelper() 直接 SQL 查询下拉数据。',
'ORM Query 接口获取下拉数据', '改用: DB.GetDBHelper().GetDataTable(sql) 直接 SQL',
'OnlyOffice,下拉,ORM,认证,DBHelper',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_onlyoffice_orm_dropdown');

-- -----------------------------------------------------------
-- 六、SFC 在线开发平台(rule)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_db_load', 'rule', 'sfc',
'SFC 运行时从 DB 加载(loadCompiledSFC)',
'loadCompiledSFC(modulePath) 运行时从 DB 加载 COMPILEDCODE 字段, 用 new Function() 执行返回 Vue 组件 options。SFC 加载器位于 src/sfc-loader/(sfc-compiler.js/module-resolver.js/module-bridge.js/remote-route.vue/index.js)。前端通过 db.postData({api:"/api/data/call/RS_M17/A06/", params:{FilterParams:{MODULEPATH:xxx}}}) 调用 A06=按 MODULEPATH 运行时加载(F03 过滤器)。',
'SFC,loadCompiledSFC,COMPILEDCODE,RS_M17,A06,sfc-loader',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_db_load');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_codeasset_unified_table', 'rule', 'metadata',
'统一代码资产表 tss_code_asset(四类型合一)',
'四代码(csharp/sql/js/vue)合并到一张表 tss_code_asset(系统表 tss_ 前缀), 字段全按标准: ID/ASSETTYPE/CODE/NAME/MODULEPATH/FILETYPE/SOURCECODE/COMPILEDCODE/DEPS/SQLTYPE/VERSION/REMARK + 标准审计字段。三个 VSS 视图(VSS_API_SCRIPT/VSS_SQL/VCK_SFC_TEMPLATE) TABLERESOURCEID 指向 tbs_code_asset_001, resfield REFFIELDNAME 别名保 FIELDNAME 不变(SCRIPTCODE→CODE 等), 前端/DataTable/版本捕获零改动。',
'tss_code_asset,代码资产,VSS_API_SCRIPT,VSS_SQL,VCK_SFC_TEMPLATE',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_codeasset_unified_table');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_vss_code_asset_final', 'rule', 'metadata',
'VSS_CODE_ASSET 是四代码唯一入口(最终架构)',
'四代码(csharp/sql/js/vue)唯一使用入口 = VSS_CODE_ASSET(vss_code_asset_001)。历史三视图(VSS_API_SCRIPT/VSS_SQL/VCK_SFC_TEMPLATE)已还原到原历史表, 不再使用。过滤器: F00 按 ID / F01 参数化通用 / FC1=RS_M21 csharp / FS1=RS_M13 sql / FJ1=RS_M17 js+vue。store 通道统一 RS_M17(code-asset ASSET_META 三个 kind 全部 storeCode=RS_M17, open/add/save/delete 单通道)。前端字段名已全量统一为 CODE/NAME/SOURCECODE/REMARK/CREATETIME/MODIFYTIME。版本纳管切到 VSS_CODE_ASSET(dvc_codeasset)。',
'VSS_CODE_ASSET,代码资产,RS_M17,store 通道,FJ1,FC1,FS1',
NULL, NULL, 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_vss_code_asset_final');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_save_buttons', 'rule', 'sfc',
'SFC 双按钮(保存=快速保存不留版本, 提交=留版本)',
'SFC 编辑器双按钮语义: 保存=SKIPVERSION=1 快速保存不留版本, 提交=CHANGENOTE+生成版本。版本链 v(n).BEFORE=v(n-1).AFTER(快速保存改动折叠进下次提交)。删除版本管理(2026-07-18): 四类资产统一逻辑删除(ISDELETED=1 走 doSave), 禁物理删除。tss_code_asset 用生成列唯一键 uk_livepath(IF(ISDELETED=0,MODULEPATH,NULL)) 替代 uk_path, 已删行让出路径可重建。',
'SFC,保存,提交,SKIPVERSION,版本,uk_livepath',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_save_buttons');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_funcpoints_bug', 'pitfall', 'wizard',
'create_funcpoints 工具生成 FUNCCODE 列(已知 bug)',
'create_funcpoints 工具执行报 "Unknown column FUNCCODE", 因工具生成的 SQL 写入 FUNCCODE 列, 但真实表 tss_funcpoint 的列是 FUNCID(不是 FUNCCODE)。这是已知 bug 未修。AI 生成功能点权限时应直接用 FUNCID 字段名。',
'INSERT INTO tss_funcpoint (FUNCCODE, FUNCPOINTCODE, ...) VALUES (...)', '改用: INSERT INTO tss_funcpoint (FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, FUNCID_FK, ...) VALUES (...) - 字段名 FUNCID',
'create_funcpoints,FUNCCODE,FUNCID,tss_funcpoint,已知 bug',
'wizard', '5', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_funcpoints_bug');

-- -----------------------------------------------------------
-- 七、低代码通用模块 GenericModule(rule)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_generic_module_design', 'rule', 'metadata',
'低代码通用模块(GenericModule)核心设计',
'核心设计: tss_module_page(页面配置) + tss_module_button(按钮配置) 替代四件套(router.js/store.js/main.vue/add.vue)。数据库表字段: tss_module_page(ID/MODULECODE/PAGECODE/PAGENAME/PAGETYPE/ROUTEPATH/COMPONENTTYPE/SFC_MODULEPATH/QUERY_APICODE/OPEN_APICODE/SAVE_APICODE), tss_module_button(ID/PAGEID/MODULECODE/APICODE/BTNNAME/BTNTYPE/BTNAREA/INTERACTTYPE/SHOWCOND/PERMCODE/ICON/COLOR/EXTPARAM)。前端组件 src/components/generic-module/(generic-module.vue/generic-form.vue/generic-store.js/index.js)。动态路由 registerGenericRoute(moduleCode, moduleData) 在 app.js initModule 中自动调用。',
'GenericModule,通用模块,tss_module_page,tss_module_button,动态路由,generic-module',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_generic_module_design');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_generic_menu_jump', 'rule', 'metadata',
'菜单跳转(tss_func.OUTERURL)规则',
'tss_func.OUTERURL select() 菜单: ① /g/ 或 g/ 开头按 path 跳转(自动补前导/, 如 g/LIB_M05/main), 不再自动检测 MODPAGE 跳 generic; ② 其他情况按 name 跳(如 b01/m05)。想走 generic 在 OUTERURL 配 /g/{MODULECODE}/{PAGECODE}。registerGenericRoute 返回所有有效 names, pages 按 SORTNO 排序。beforeEach /g/ 直接访问用 added.length>0 判断。F00 ORDERBY=A.SORTNO,A.PAGECODE。',
'菜单,OUTERURL,/g/,generic,跳转,tss_func,SORTNO',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_generic_menu_jump');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_button_areas', 'rule', 'sfc',
'按钮区域渲染(header/footer/row)',
'generic-module.vue 支持 3 种按钮区域: BTNAREA=header(header-action slot) / BTNAREA=footer(footer-action slot) / BTNAREA=row(table-action slot, 传 row)。表单页 PAGETYPE=form 时直接渲染 generic-form(div.generic-form-page 包裹), 不用 rs-modal 弹窗。',
'BTNAREA,header,footer,row,按钮区域,generic-module',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_button_areas');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_codeasset_save_iron', 'rule', 'sfc',
'铁律: 不手拼 XML(走数据源)',
'数据读写一律经数据源(getGenericStore("RS_XX") → storeHelper.getTable("MAIN") → setValue → Store.dispatch("RS_XX/save")), Store03 getXML 自动处理转义/oc/回写 ID。新建用 store 的 add action(INIT+ADD)。严禁手拼 XML 字符串(易遗漏转义/oc/回写 ID)。',
'XML,getXML,数据源,Store03,add action,INIT,转义',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_codeasset_save_iron');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_module_link_api', 'rule', 'csharp',
'新建脚本自动关联模块(RS_M18 A07)',
'code-editor-popup 带 moduleCode 上下文时, 保存成功自动调 RS_M18/A07(SC_M18_LINK_API): 幂等查重(SQLID/SCRIPTCODE 已存在直接返回) + 分配下一个 APICODE(GREATEST(MAX(数字后缀),50)+1, 自定义永远 A51 起) + 建 moudleapi 行(APITYPE=csharp/sql, SCRIPTCODE/SQLID 指向资产)。迁移 20_link_api.sql。RS_M18 A08=SC_M18_UNLINK_API 解除关联。',
'RS_M18,A07,SC_M18_LINK_API,APICODE,A51,moudleapi,关联',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_module_link_api');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_rs_code_editor_modes', 'rule', 'sfc',
'rs-code-editor 语言模式已装',
'rs-code-editor 只装了 sql/javascript/clike 三种 CodeMirror mode。配置写 language:csharp 经 normalizeMode 映射到 text/x-csharp。新语言需先 import codemirror mode 才能使用。',
'rs-code-editor,CodeMirror,mode,sql,javascript,clike,csharp',
NULL, '4', 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_rs_code_editor_modes');

-- -----------------------------------------------------------
-- 八、Java 后端 hs2-java(rule, PRIORITY=3, 后续迁移参考)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_java_backend_stack', 'rule', 'general',
'Java 后端 hs2-java 技术栈(1:1 重写 .NET)',
'hs2-java/ 目录: Spring Boot 3.2.5 + Java 17 + JdbcTemplate + NamedParameterJdbcTemplate + magic-api 2.1.1。ORM 核心 1:1 重写: SchemaManage→SchemaManage.java, BuildSQL01→BuildSQL.java, ViewOperate01→ViewOperate.java, SQLManage→SQLManage.java。模板引擎: NVelocity→Apache Velocity(语法兼容, $!{var} 安静引用)。认证: IdentityServer4→Spring Security + JWT(JwtTokenProvider/JwtAuthFilter)。Controller→magic-api 脚本: data/call.ms(核心路由), outer/call.ms, auth/login.ms。保留 Java Controller: FileController(MultipartFile 不适合 magic-api)。关键: SchemaManage 用手动映射不用 BeanPropertyRowMapper(DB 列名全大写, 驼峰转换会失败); 查询用 NamedParameterJdbcTemplate 支持 @参数(Dapper 风格命名参数); 保留 XML 数据传输格式(前端 DataTable.getXML() 不变)。',
'hs2-java,Spring Boot,Java 17,magic-api,JdbcTemplate,Velocity,JWT',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_java_backend_stack');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_db_connection', 'glossary', 'general',
'数据库连接(本地开发环境)',
'MySQL 5.7 (Docker: labone-mysql, port 13306, user: labone, pwd: labone123, db: D0001)。Java 后端连接同一库。Java ORM 1:1 重写自 .NET, 业务表/元数据表共用。',
'MySQL,Docker,labone-mysql,13306,D0001,连接',
NULL, NULL, 2, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_db_connection');

-- -----------------------------------------------------------
-- 九、移动端 hs-mobile(rule, PRIORITY=2, 上下文参考)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_hsmobile_stack', 'rule', 'general',
'移动端 hs-mobile 技术栈',
'hs-mobile/: uni-app + Vue3 + Pinia。@dcloudio 包须取 vue3 tag 全包版本对齐(latest 是 Vue2), vite 跟随 peer。联调 API 映射参考 hs-mobile-api-mapping.md(独立文档, 不在本记忆库详列)。',
'hs-mobile,uni-app,Vue3,Pinia,dcloudio,移动端',
NULL, NULL, 2, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_hsmobile_stack');

-- -----------------------------------------------------------
-- 十、AI 记忆中枢自描述(glossary, 用于 AI 自我理解)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_ai_memory_hub', 'glossary', 'general',
'AI 记忆中枢三层检索',
'MemoryService.BuildMemoryPrompt 一站式注入, 三层检索: ① rules(铁律) 按 sceneCode+wizardStep+assetType 加载, PRIORITY>=5 必注入; ② examples(示例) 按关键词打分(TAGS 3 分 / TITLE 2 分 / CONTENT 1 分); ③ pitfalls(反模式) 按 TAGS 关键词触发。无需向量库, MySQL LIKE + tags 足够 5-10 人团队 500-2000 条规模。HITCOUNT 字段记录被命中次数, "越用越知哪条常用"。反馈回流: tss_ai_feedback → PROMOTED=1 → AdoptAsExample → tss_ai_memory(MEMORYTYPE=example, SOURCE=feedback)。',
'AI记忆,MemoryService,BuildMemoryPrompt,rules,examples,pitfalls,HITCOUNT,反馈',
'assistant,aidev,wizard', NULL, 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_ai_memory_hub');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_ai_asset_types', 'glossary', 'general',
'ASSETTYPE 维度取值',
'tss_ai_memory.ASSETTYPE 取值: sfc(Vue 组件) / sql(SQL 模板) / csharp(C# 脚本) / metadata(ORM 元数据) / wizard(向导流程) / frontend(前端规范) / general(通用)。一条记忆可覆盖多个 ASSETTYPE(逗号分隔)。SCENE_CODES 取值: assistant(助理对话)/aidev(开发向导)/wizard(模块向导), 同样支持多值。',
'ASSETTYPE,sfc,sql,csharp,metadata,wizard,SCENE_CODES',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_ai_asset_types');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_ai_scene_codes', 'glossary', 'general',
'AI 场景配置(tss_ai_scene)',
'tss_ai_scene 场景配置化, 前端 sceneConfig.js 消费。三个核心场景: assistant(智能助理对话, DeepSeek Agent), aidev(开发向导, 单步生成), wizard(模块向导, 6 步分步生成)。每个场景配 STEP_TOOL_MAP 决定向导每步可用工具, 配 systemPrompt 模板。RS_M23 管理页。',
'ai_scene,场景,assistant,aidev,wizard,RS_M23,STEP_TOOL_MAP',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_ai_scene_codes');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_ai_tools', 'glossary', 'general',
'AI 工具集(声明式只读 + 自定义)',
'tss_ai_tool 声明式只读工具(仅 SELECT, 静态合并挂接非 ToolRegistry), EXECUTORTYPE=sql 走 SQLCODE 直接执行, 返回 JSON。RS_M24 管理页。工具分组: ① 通用查询(search_*/read_*/list_*) ② 元数据操作(create_physical_table/define_dataview/define_api/define_filter/define_page/define_button/configure_resource_field/configure_ui_field/create_menu/create_funcpoints) ③ 脚本接口(define_sql_api/define_script_api/define_script_flow_api/update_script_flow_api/read_script_flow_api) ④ 模板(search_module_template/read_module_template) ⑤ 字典(search_dict/create_dict) ⑥ 校验(verify_sfc/verify_sql/verify_metadata) ⑦ 记忆(search_memory/recall_examples/list_pitfalls)。字典编码 D0704=AI 工具集。',
'ai_tool,tss_ai_tool,RS_M24,EXECUTORTYPE,工具集,D0704',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_ai_tools');

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 本批 43 条新增(20 rule + 7 pitfall + 14 glossary + 2 sfc rule)
-- 配合 33_ai_memory_seed.sql 的 24 条 = 共 67 条种子
-- 后续: 用户反馈通过 tss_ai_feedback 回流, PROMOTED=1 后入 tss_ai_memory
