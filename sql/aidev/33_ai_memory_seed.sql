-- ============================================================
-- AI 记忆中枢 — 种子数据(rule/pitfall/example)
-- 内容: 从 MEMORY.md/CLAUDE.md 提炼的核心铁律、踩坑、示例
--       覆盖 ORM 元数据/SFC/SQL/C# 脚本/向导/版本管理 6 大类
-- 来源: SOURCE='auto_seed' (后续可由用户编辑/删除)
-- 机制: MemoryService 启动时按场景/步骤加载, 优先级 PRIORITY=5 必注入
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、ORM 元数据铁律(PRIORITY=5, 必注入)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_fieldname_no_underscore', 'rule', 'metadata',
'字段名不能有下划线',
'数据库字段名一律大写无下划线格式: 如 FORMULANAME/FORMULACODE/ISDELETED。与系统现有表(如 tbs_cust 的 CUSTCODE/CUSTNAME)保持一致。禁止: SFC_MODULEPATH/QUERY_APICODE/CREATED_TIME 这类下划线写法。正确: SFCMODULEPATH/QUERYAPICODE/CREATETIME。',
'字段名,命名,下划线,ORM,resfield',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_fieldname_no_underscore');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_dataview_reffieldid', 'rule', 'metadata',
'DATAVIEW 字段必须通过 REFFIELDID 关联物理表字段',
'tss_resfield 中 DATAVIEW(VCK/VSS) 资源的每个字段, REFFIELDID 必须指向物理表对应字段的 ID, 不能为 NULL。物理表(TBS)字段的 REFFIELDID 才为 NULL。如果漏注册 VSS 字段, 前端查询会报 "The given key X was not present in the dictionary"。',
'REFFIELDID,DATAVIEW,视图,VCK,VSS,resfield',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_dataview_reffieldid');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_refresource_dataview', 'pitfall', 'metadata',
'REFRESOURCEID 必须指向 TBS(TABLE)而非 VBS(DATAVIEW)',
'ORM BuildSQL01 JOIN 时只处理 TABLE/VIEW/SQL 类型, DATAVIEW 类型不支持 JOIN。REFRESOURCEID 必须指向物理表资源(TBS_EMP/TBS_DEPT 等)。VBS_EMP(933b4dfe91ed4cbb9a4785a949fdea6e)是 DATAVIEW 不能用作 REFRESOURCEID, 必须用 TBS_EMP(770072c4-0750-11ea-9e8d-00163e067045)。',
'REFRESOURCEID="VBS_EMP"', '改用 REFRESOURCEID="TBS_EMP"(770072c4-0750-11ea-9e8d-00163e067045)',
'REFRESOURCEID,JOIN,VBS,TBS,DATAVIEW',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_refresource_dataview');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_reffieldid_to_tbs', 'rule', 'metadata',
'REFFIELDID 必须指向 TBS 字段而非 VBS 字段',
'ORM 通过 LEFT JOIN TSS_RESFIELD B ON A.REFFIELDID=B.ID 获取 REFFIELDNAME(B.FIELDNAME), REFFIELDNAME 决定 SELECT 的列名。REFFIELDID 必须指向被引用 TBS 表的字段, 这样 REFFIELDNAME 才是物理表实际存在的列名。TBS_EMP.EMPNAME 字段 ID=936de29c-0750-11ea-9e8d-00163e067045, TBS_REGUITEM.ITEMNAME 字段 ID=eba58e05-1751-11ea-9e8d-00163e067045。',
'REFFIELDID,REFFIELDNAME,JOIN,字段引用',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_reffieldid_to_tbs');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_fieldtype_mysql_native', 'rule', 'metadata',
'FIELDTYPE 必须用 MySQL 原生类型',
'tss_resfield.FIELDTYPE 只识别 MySQL 原生类型: varchar/text/datetime/date/int/tinyint/decimal 等。不识别 .NET 类型(String/Int32/DateTime), 不识别 Java 类型。如果 FIELDTYPE 写成 String, INSERT SQL 值不加引号导致 "Unknown column" 错误。',
'FIELDTYPE,类型,MySQL,varchar,String',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_fieldtype_mysql_native');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_resource_aname_a', 'rule', 'metadata',
'VCK/VSS 视图 RESOURCEANAME 必须为 A',
'ORM BuildQuery 的 LEFT JOIN 用 RESOURCEANAME 作为表别名, DATAVIEW 视图的 RESOURCEANAME 必须为 A(主表别名)。配置错误会导致 SQL 生成失败。',
'RESOURCEANAME,别名,DATAVIEW,VCK,VSS',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_resource_aname_a');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_new_table_collation', 'rule', 'metadata',
'新表 collation 必须 utf8mb4_general_ci',
'MySQL 8 默认 utf8mb4_0900_ai_ci, 与全库(utf8mb4_general_ci)JOIN 时报 "Illegal mix of collations"。CREATE TABLE 须显式指定 COLLATE=utf8mb4_general_ci。已踩坑的表(tbs_sfc_template 等 7 张)已 CONVERT 修复。',
'collation,字符集,utf8mb4,JOIN,MySQL8',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_new_table_collation');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_add_field_three_steps', 'rule', 'metadata',
'加表列必须三步走(漏了必炸)',
'ALTER TABLE 加列后必须: ① 注册 TBS resfield ② 注册 VSS resfield(REFFIELDID 指向 TBS 字段) ③ 涉及列表/表单再配 resuipc。漏②时 Model.GetString("列名") 报 "The given key X was not present in the dictionary"(ViewRow viewColumValues 由资源字段构建)。',
'加列,resfield,resuipc,三步走,ALTER TABLE',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_add_field_three_steps');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_tss_no_isdeleted', 'rule', 'metadata',
'tss_ 系统元数据表无 ISDELETED 字段',
'tss_ 系统元数据表(如 tss_resource/tss_resfield/tss_moudleapi)全部无 ISDELETED 字段, 生成 SQL 不能带 ISDELETED 列。只有 tbs_ 业务表和 tss_aidev_*/tss_ai_*/tss_dev_*/tss_release/tss_module_* 等业务系统表才有 ISDELETED。',
'ISDELETED,tss_,元数据,系统表',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_tss_no_isdeleted');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_f00_filter_id', 'rule', 'metadata',
'F00 过滤器必须 A.ID=@ID',
'doOpen/Store03.open 走 FilterParams.ID 路径, F00 过滤器必须是 "A.ID=@ID"。F00 按业务编码过滤(如 A.SCRIPTCODE=@SCRIPTCODE)会导致表单打开永远空白。只有"按编码打开"的专用打开接口(如 RS_M18 按 MODULECODE 开配置)才能用业务字段。RS_M21/RS_M23 曾因此踩坑已修。',
'F00,过滤器,doOpen,FilterParams,ID',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_f00_filter_id');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_orderby_no_alias', 'pitfall', 'metadata',
'ORDERBY 一律不带表别名',
'BuildSQL01.cs:121/140 只要 orderBy 非空就包 "SELECT * FROM (...) T ORDER BY {orderBy}" 子查询(分页/不分页都包)。排序列带别名前缀(A.x)时, 若排序列不在 SELECT 输出列就报 "Unknown column A.x in order clause"。无前缀是唯一安全写法。',
'ORDERBY A.SORTNO, A.PAGECODE', '改用 ORDERBY= SORTNO, PAGECODE(无 A. 前缀)',
'ORDERBY,排序,别名,BuildSQL,子查询',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_orderby_no_alias');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_filtercode_convention', 'rule', 'metadata',
'FILTERCODE 编号规范(F00/F01/F02/F011/F021...)',
'F00=单条查询(A.ID=@ID) / F01=列表查询(模糊搜索+数据权限) / F02=高级查询(NVelocity 多条件) / F03+=专用批量 / F011/F012=审核列表(加 CHECKID=当前用户) / F021/F022=审批列表(加 VERIFYID=当前用户) / F021/F022 高级查询变体。新过滤器按此规范编号。',
'FILTERCODE,F00,F01,F02,F011,F021,过滤器',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_filtercode_convention');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_resource_prefix', 'rule', 'metadata',
'资源命名规则(前缀)',
'TBS_xxx=物理表定义 / VCK_xxx=业务视图(前端列表/表单用) / VBS_xxx=基础数据视图(选择器) / VRP_xxx=报表视图(SQL 类型) / VSS_xxx=系统管理视图。tss_resource.RESOURCENAME 按此前缀分类, 决定 ORM 的处理方式。',
'TBS,VCK,VBS,VRP,VSS,资源命名,前缀',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_resource_prefix');

-- -----------------------------------------------------------
-- 二、NVelocity / SQL 模板铁律(PRIORITY=5)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_nvelocity_single_quote', 'pitfall', 'sql',
'NVelocity 不能处理单引号(SQL 模板中禁用单引号)',
'SQL 模板(tss_sql)经 NVelocity 解析, 模板中任何单引号(如 "未指定"、空字符串 %%、LIKE %like%)都会导致 NVelocity 解析失败, SQL 被截断。SQL 模板必须避免单引号。LIKE 用 CONCAT 函数而非字符串字面量。',
'SELECT * FROM t WHERE NAME LIKE ''%xx%''', '改用: AND NAME LIKE CONCAT(CONCAT(''%'',@NAME),''%'') 或 MySQL 函数处理',
'NVelocity,单引号,SQL模板,LIKE,CONCAT,tss_sql',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_nvelocity_single_quote');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_filtersql_dollar_syntax', 'rule', 'sql',
'tss_resfilter 和 tss_sql 参数语法不同',
'tss_resfilter.FILTERSQL 用 @VAR(Dapper 参数化); tss_sql.SQLTXT 用 $!{VAR}(NVelocity 模板变量)。但 SQL 模板中 @VAR 也能被 Dapper 处理(NVelocity 不修改 @ 符号)。系统变量自动注入: @_USERID_(当前用户ID)/@_EMPID_(当前员工ID)/@_DEPTID_(当前部门ID)。',
'FILTERSQL,SQLTXT,@VAR,NVelocity,参数,系统变量',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_filtersql_dollar_syntax');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_filtersql_nvelocity_template', 'rule', 'sql',
'FILTERSQL NVelocity 模板语法',
'基本条件: A.STATE IN(2)。模板条件(#if 判断, 空值不生效): #if("$!{CUSTNAME}"!="") AND A.CUSTNAME LIKE CONCAT(''%'',@CUSTNAME,''%'') #end。日期范围: #if("$!{BILLDATE_start}"!="") AND A.BILLDATE>=str_to_date(@BILLDATE_start,''%Y-%m-%d'') #end',
'FILTERSQL,NVelocity,#if,模板,日期范围',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_filtersql_nvelocity_template');

-- -----------------------------------------------------------
-- 三、C# 脚本铁律
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_csharp_script_globals', 'rule', 'csharp',
'C# 脚本 ScriptGlobals 可用 API',
'脚本运行时 globals 对象: P(参数对象, 字段通过 row.X 动态访问)/DbFirst(sql)/DbScalar(sql)/DbExec(sql,param)/Sql(sqlId,params)/Trans(action)/MD(模块上下文)/Log(msg)/Response(响应构建)。注意: DbExec 只有(sql,param)两参, Trans() 自动接管事务, 脚本里别再传第三参。',
'C#脚本,ScriptGlobals,DbExec,Trans,Roslyn',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_csharp_script_globals');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_csharp_dynamic_row', 'rule', 'csharp',
'C# 脚本动态取值需 Microsoft.CSharp RuntimeBinder',
'动态取值(row.X 这种 dynamic 字段访问)依赖 Microsoft.CSharp.RuntimeBinder, .NET 默认懒加载。脚本引擎启动时必须 ForceLoadAssemblies 触碰核心类型(DBHelper/SQLManage/Dapper/CSharpArgumentInfo 等 11 个)强制加载, 否则运行时报找不到类型。',
'C#脚本,RuntimeBinder,dynamic,Roslyn,懒加载',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_csharp_dynamic_row');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_csharp_sourcecode_hex', 'rule', 'csharp',
'C# 脚本源码在 SQL 迁移文件中用 0x 十六进制字面量',
'迁移 SQL 中 C# 脚本源码必须转成 0x 十六进制字面量写入 SOURCECODE 列(避免引号转义冲突), 不能直接用引号包裹的字符串。读取时由 CSharpScriptEngine 自动按文本解码。脚本编译结果按 SourceHash 缓存, 源码变才重编。',
'C#脚本,SOURCECODE,十六进制,0x,迁移,SourceHash',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_csharp_sourcecode_hex');

-- -----------------------------------------------------------
-- 四、SFC 组件铁律
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_compile_pipeline', 'rule', 'sfc',
'SFC 编译流水线(vue-template-compiler + babel + less)',
'compileSFC(sourceCode, modulePath, fileType) 完整流程: ① vue-template-compiler.parseComponent 拆 template/script/style ② compiler.compile 编译 template 为 render 函数(错误抛"模板编译错误") ③ @babel/standalone 转 ES6 import 为 CJS require ④ less 编译 style + scoped 处理 ⑤ extractDeps 静态分析 import 路径预加载 DEPS ⑥ executeCompiled 用 new Function() 执行编译后代码。保存前必须 compileSFC 通过。',
'SFC,vue-template-compiler,babel,less,compileSFC,render',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_compile_pipeline');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_bridge_modules', 'rule', 'sfc',
'SFC 桥梁模块(运行时可用 import)',
'SFC 运行时通过 module-bridge.js 暴露全局桥梁: @/api/db(@/store/vue/heyui/@/utils/extends 等)预注册到 window.__SFC_MODULES__。编译后的 SFC 用 CJS require 引用这些桥梁名, 不能直接 import npm 包(除非 DEPS 已声明预加载)。运行时加载走 loadCompiledSFC(modulePath) 从 DB 取 COMPILEDCODE。',
'SFC,module-bridge,桥梁,window.__SFC_MODULES__,require',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_bridge_modules');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sfc_save_xml', 'rule', 'sfc',
'SFC 保存 XML 用 buildDataTableXML 不手拼',
'SFC 资产保存的 XML 数据格式必须用 buildDataTableXML() 函数生成(与 DataTable.getXML() 一致), 不能手拼 XML。手拼易遗漏转义/oc(旧值)/回写 ID。新建用 store 的 add action(INIT+ADD)。saveAsset 流程: setValue 各字段 → store.dispatch(''RS_M17/save'')。',
'SFC,XML,buildDataTableXML,DataTable,saveAsset',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sfc_save_xml');

-- -----------------------------------------------------------
-- 五、前端 Store 铁律(P0 强制规则)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_frontend_store_action', 'rule', 'sfc',
'前端接口调用必须通过 Store action(禁直接 import @/api/db)',
'禁止在 .vue 中 import db from @/api/db 后调 db.postData/call/open/openTables/getNewID。@/api/db 是 Store 层私有依赖。必须通过 Vuex action, 模板双向绑定用 mapDateTable, 页面调 Store action 用 this.$callAction({action,param,...})(见 src/utils/extends.js:72), 禁止 this.$store.dispatch(...)。',
'前端,Store,action,$callAction,mapDateTable,@/api/db',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_frontend_store_action');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_frontend_router_ensure', 'rule', 'sfc',
'前端路由必须用 require.ensure 懒加载',
'router.js 路由定义必须用 require.ensure 懒加载, 禁止同步 import。同步 import 会导致 chunk 在 app store 加载前执行, store.js 中不能使用 SelStore(会因 RS_M00 未加载导致运行时报 "Cannot read properties of undefined (reading MODPATH)")。',
'前端,路由,require.ensure,懒加载,chunk,SelStore',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_frontend_router_ensure');

-- -----------------------------------------------------------
-- 六、向导 / 模块创建铁律
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_wizard_step_tools', 'rule', 'wizard',
'向导每步可用工具(STEP_TOOL_MAP)',
'向导 6 步工具分配: Step0 模板(register_module/search_existing_resource/search_module_template/read_module_template); Step1 物理表(create_physical_table/search_existing_resource/read_table_schema/configure_resource_field); Step2 视图(define_dataview/configure_resource_field/define_filter); Step3 接口与页面(define_api/define_sql_api/define_script_api/define_script_flow_api/update_script_flow_api/read_script_flow_api/define_filter/define_page/define_button); Step4 UI(configure_ui_field/search_dict/create_dict); Step5 菜单(create_menu/create_funcpoints)。',
'向导,STEP_TOOL_MAP,工具,Step,向导步骤',
'wizard', '0,1,2,3,4,5', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_wizard_step_tools');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_wizard_changeset_shared', 'rule', 'wizard',
'向导 6 步共享 changesetId',
'多步 AI 分步生成时, 6 步共享同一个 changesetId(变更包 ID)。WizardStepOrchestrator 按 stepToolMap 过滤工具, 每步只暴露该步工具。生成结果累积写入同一个 changeset, 最后统一导出/应用。',
'向导,changesetId,变更包,分步生成,WizardStepOrchestrator',
'wizard', '0,1,2,3,4,5', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_wizard_changeset_shared');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_metadata_perm_field', 'rule', 'metadata',
'人员/时间字段命名标准(CREATEID/CREATER/CREATETIME)',
'人员时间字段统一命名: CREATEID(创建人ID)/CREATER(创建人姓名,R 后缀)/CREATETIME/MODIFYID/MODIFER/MODIFYTIME。全部大写无下划线。禁用 CREATEDBY/CREATEDBYNAME/CREATEDTIME(仅 tss_aidev_session 是遗留异常, 内部自洽未动)。审计日志类表(tss_dev_version)只配 CREATE 三件套不加 MODIFY; 可编辑记录类表(tss_module_template)CREATE+MODIFY 全配。',
'命名,CREATEID,CREATER,CREATETIME,MODIFYID,MODIFER,人员字段',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_metadata_perm_field');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_dict_not_hardcode', 'rule', 'metadata',
'下拉选项必须走数据字典(禁写死)',
'tss_resuipc.SELECTDATA 写 DICTNAME(非 DICTCODE, 非 k:v 内联)。前端页面 Select 从 store.state.app.dicts[字典名] 读({value:label}映射, 启动时 initDict 全量加载 + heyui.addDict 全局注册)。字典编码 D07xx 系列(D06xx 是 aidev 状态字典)。已建: D0701版本对象类型/D0702版本操作类型/D0703场景传输/D0704AI工具集/D0705上下文源/D0706执行类型/D0707业务分类/D0708字段占宽。',
'字典,SELECTDATA,DICTNAME,D07xx,initDict,下拉选项',
NULL, '2,4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_dict_not_hardcode');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_code_asset_ownership', 'rule', 'csharp',
'代码资产归属语义(命名前缀区分自有/外链)',
'按命名前缀(SC_/SS_+MODULECODE)区分自有/外链: 自有资产可删除(csharp=RS_M21 save ISDELETED=1 逻辑删; sql=RS_M13 delete 物理删; 删前先 A08 解本模块关联); 外链资产(他模块前缀)只能"移除"(RS_M18 A08=SC_M18_UNLINK_API 删 moudleapi 关联行, 不删文件)。选入=RS_M18 A07 逐个关联。',
'代码资产,归属,前缀,SC_,SS_,MODULECODE,删除,逻辑删',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_code_asset_ownership');

-- -----------------------------------------------------------
-- 七、版本管理 / 删除铁律
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_delete_version_rollback', 'rule', 'metadata',
'删除即版本管理(2026-07-18 后)',
'四类资产(csharp/sql/js/vue)统一逻辑删除(ISDELETED=1 走 doSave), 禁物理删除。tss_code_asset 用生成列唯一键 uk_livepath(IF(ISDELETED=0,MODULEPATH,NULL)) 替代 uk_path, 已删行让出路径可重建。DevVersionService 识别 ISDELETED 0→1 的 update 为 OPTYPE=delete; 回滚 delete 行在则 UPDATE 写回, 行不在才 INSERT。',
'删除,版本管理,ISDELETED,逻辑删除,uk_livepath,回滚',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_delete_version_rollback');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_datatable_delete', 'pitfall', 'metadata',
'DataTable 删除/单字段更新必须先 open 加载完整行',
'INIT+ADD+save 的行是 Add 状态, getXML 产 <a> INSERT, 撞 NOT NULL 约束。逻辑删除/单字段更新必须先 open 加载完整行再 setValue(产 <m> 更新只 SET 变更字段)。物理删除用 delete action(INIT+ADD 仅带键值的行 + dispatch delete, <d> 行只带键值无约束问题)。模板/脚本删除都有版本快照兜底。',
'dt.MAIN.add({ID: x, ISDELETED: 1}); dispatch("save")', '改用: 先 await dispatch("open", {ID: x}); 再 dt.MAIN.setValue("ISDELETED", 1); 再 dispatch("save")',
'DataTable,删除,逻辑删除,getXML,Add,INIT,open',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_datatable_delete');

-- -----------------------------------------------------------
-- 八、通用术语(glossary)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_orm_tables', 'glossary', 'general',
'ORM 核心元数据表及职责',
'TSS_RESOURCE=资源/表定义(RESOURCENAME/TABLENAME/RESOURCETYPE) / TSS_RESFIELD=字段定义(FIELDNAME/FIELDTYPE/ISKEY/REFRESOURCEID/UPFIELDID/VFORMAT) / TSS_RESFILTER=过滤器(FILTERCODE/FILTERSQL NVelocity/ORDERBY) / TSS_RESUIPC=UI 配置(LABELNAME/EDITTYPE/LISTSORT/QUERYSORT/EDITSORT/SELECTDATA) / TSS_MOUDLE=模块 / TSS_MOUDLEPATH=数据源(QRY/QQRY/SEL/MAIN/DTS) / TSS_MOUDLEPATHREL=主外键关系 / TSS_MOUDLEAPI=模块接口 / TSS_SQL=SQL 模板 / TSS_FUNC=菜单 / TSS_FUNCPOINT=功能点权限。',
'ORM,元数据表,TSS_RESOURCE,TSS_RESFIELD,术语',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_orm_tables');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_xml_format', 'glossary', 'general',
'前后端 XML 传输格式(DataTable)',
'前端 DataTable.getXML() 生成 XML, 后端 ViewOperate01.FillData 解析。格式: <表名 l="u" c="字段列表" t="类型列表"><a>(新增行)<r c0="值" c1="值"/></a><m>(修改行, 含旧值 oc0/oc1)<r c0="新值" oc0="旧值"/></m><d>(删除行, 仅旧值)<r oc0="旧值"/></d></表名>。',
'XML,DataTable,getXML,FillData,传输格式',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_xml_format');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_api_types', 'glossary', 'general',
'ORM 标准 APITYPE 类型',
'DataController.Call 根据 API 配置的 APITYPE 路由: query=列表查询(支持分页/导出) / open=打开单条(含关联子表) / save=保存(新增/修改, 含单据号/主外键处理) / delete=删除 / submit/reSubmit=提交/重新提交 / check/reCheck=审核/撤销审核 / verify/reVerify=审批/撤销审批 / batchXxx=批量操作 / sql=SQL 脚本接口(零代码) / csharp=C# 脚本接口(Roslyn 在线) / script=编排接口(多步骤组合) / 自定义=子类重写 doMyApi 扩展。',
'APITYPE,query,open,save,delete,sql,csharp,script,doMyApi',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_api_types');

-- -----------------------------------------------------------
-- 九、示例(example)— 后续由用户反馈自动沉淀, 此处仅占位 1 条
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, QUALITY_SCORE, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_f01_filter', 'example', 'metadata',
'示例: 标准 F01 列表查询过滤器',
'#if("$!{CUSTNAME}"!="")
AND A.CUSTNAME LIKE CONCAT(''%'',@CUSTNAME,''%'')
#end
#if("$!{STATE}"!="")
AND A.STATE=@STATE
#end
#if("$!{BILLDATE_start}"!="")
AND A.BILLDATE>=str_to_date(@BILLDATE_start,''%Y-%m-%d'')
#end
#if("$!{BILLDATE_end}"!="")
AND A.BILLDATE<DATE_ADD(str_to_date(@BILLDATE_end,''%Y-%m-%d''),INTERVAL 1 DAY)
#end
AND A.ISDELETED=0',
'F01,过滤器,模糊搜索,日期范围,示例',
NULL, '3', 3, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_f01_filter');

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 共 24 条种子: 14 rule + 5 pitfall + 3 glossary + 1 example + 1 命名规范
-- 后续: 用户使用过程中产生的反馈自动通过 tss_ai_feedback 回流,
--       PROMOTED=1 的反馈提升为 example(MEMORYTYPE='example', SOURCE='feedback')
