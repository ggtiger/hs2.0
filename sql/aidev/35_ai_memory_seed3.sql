-- ============================================================
-- AI 记忆中枢 — 种子数据第三批(深度 ORM 规则 + 完整前端模板 + 代码示例)
-- 内容: 三轮代码扫描产出
--   - ORM 内部机制(FillKey/ENTRYNUM/ViewColumnValue/UPFIELDID/ISVIRTUAL/GetValues/-99999999 等)
--   - 前端规范模板(main.vue/add.vue/store.js/router.js + Store03 actions + Add01/Sel01 mixin)
--   - C# 脚本/Controller/SQL 过滤器/编排接口完整示例
-- 来源: SOURCE='auto_seed' (后续可由用户编辑/删除)
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、ORM 内部机制深度规则(rule, PRIORITY=4-5)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_fillkey_guid', 'rule', 'metadata',
'ISKEY=1 主键 FillKey 自动填 GUID',
'ViewOperate01.FillKey (ViewOperate01.cs:241) 遍历 resource.Fields 查找 ISKEY=="1" 的字段, 对值为空的行自动填入 Guid.NewGuid().ToString().Replace("-","") (32 位无连字符小写 GUID)。KEYGENTYPE 字段代码并不读——所有 ISKEY=1 字段一律用 GUID 算法。若希望用 PSS_GENCODE 存储过程生成单据号(如 BILLCODE), 必须配 VFORMAT 字段(含 ORD{yyyy} 等模板), DataController._doSave 单独调 MMAIN.setBillCode(field.VFORMAT), 与 ISKEY 的 GUID 填充是两条独立路径。',
'ISKEY,FillKey,GUID,主键,VFORMAT,PSS_GENCODE,BILLCODE',
NULL, '1,2', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_fillkey_guid');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_entrynum_order', 'rule', 'metadata',
'字段按 ENTRYNUM 决定 SELECT/INSERT 顺序',
'SchemaManage.GetResource 加载字段时按 A.ENTRYNUM 排序(SchemaManage.cs:20 "ORDER BY A.ENTRYNUM")。直接决定: ① BuildQuery SELECT 输出列顺序; ② BuildInsert/BuildBatchInsert 列名与 VALUES 占位顺序; ③ DataView.Columns 集合顺序(影响前端列表渲染顺序)。新增 resfield 必须给 ENTRYNUM 合理值(整数, 按期望显示顺序赋值), 否则新字段可能排到意外位置。AssistantToolExecutor 读结构也按 ENTRYNUM 排序保持口径一致。',
'ENTRYNUM,字段顺序,resfield,排序,SchemaManage',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_entrynum_order');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_defaultvalue_implicit', 'pitfall', 'metadata',
'DEFAULTVALUE 隐式填充新增行空值字段',
'ViewOperate01.FillKey (ViewOperate01.cs:253-264) 在生成主键后, 遍历 resource.Fields 所有字段, 凡 DEFAULTVALUE 非空且 ISKEY!="1" 的字段, 新增行中若为 null 或空字符串, 会被自动写入 DEFAULTVALUE。常被忽略: ① ISDELETED 字段配 DEFAULTVALUE=0 自动写入; ② STATE 字段配 DEFAULTVALUE=1 自动写入(待提交); ③ 若某字段不希望被自动填, 必须把 resfield.DEFAULTVALUE 留空。注意: 此逻辑只在新增行(Inserted)路径触发, 更新路径不触发。',
'给字段配 DEFAULTVALUE=PENDING 期望仅 DB INSERT 缺省生效', 'DEFAULTVALUE 同时被 ORM 和 DB 兜底; 若只想要 DB 缺省, 删除 tss_resfield.DEFAULTVALUE 让 DEFAULT 约束生效',
'DEFAULTVALUE,FillKey,新增行,resfield,隐式填充',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_defaultvalue_implicit');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_viewcolumnvalue_null', 'pitfall', 'metadata',
'ViewColumnValue 非字符串类型空串自动转 null',
'ViewColumnValue.cs:30-36 setter 逻辑: 当 Column.Type 不是 varchar/text 时(即 int/decimal/datetime 等), 若传入值是空字符串或字符串 "null", 自动转为 null。隐式转换陷阱: ① 数字字段 setValue("") 实际存的是 null 而非 0; ② 日期字段 setValue("") 变成 null; ③ varchar/text 字段 setValue("") 保持空字符串(不转 null)。BuildInsert/BuildUpdate 对 null 处理为 "NULL"(无引号), 空字符串处理为 ""(带引号)。GetString 内部用 + string.Empty 转换 null 为 ""。',
'row["AMT"]=="" 判断金额为空', '数字/日期字段判空用 row.GetString(field)=="" || row[field]==null; 或统一用 row.GetString()',
'ViewColumnValue,null,空字符串,类型转换,数字字段',
NULL, '1,2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_viewcolumnvalue_null');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_moudlepathrel_inject', 'rule', 'metadata',
'主外键关系 RFIELDSA/B 自动注入子表查询',
'DataController.doOpen (DataController.cs:751-765) 从 tss_moudlepathrel 读 PATHNAMEA=MAIN 的所有行: PATHNAMEB=DTSx 子表路径, RFIELDSA=主表字段名, RFIELDSB=子表字段名。子表查询条件: OtherWhere="{主表别名}.{RFIELDSB} IN @{RFIELDSA}", FilterParams[RFIELDSA]=主表所有行该字段的值(逗号分隔)。若子表资源 HasColumn("ENTRYNUM") 自动追加 OrderBy="ENTRYNUM"。要点: ① 主子表关系靠元数据驱动无需 JOIN; ② tss_moudlepathrel 配错(RFIELDSA/B 字段名颠倒)会导致子表查询空。',
'RFIELDSA,RFIELDSB,主子表,moudlepathrel,OtherWhere,IN,ENTRYNUM',
NULL, '2,3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_moudlepathrel_inject');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_upfieldid_inherit', 'rule', 'metadata',
'UPFIELDID 子引用字段决定 JOIN 别名继承',
'BuildSQL01.BuildQuery (BuildSQL01.cs:45-66) 对有 REFFIELDID 且有 UPFIELDID 的"子引用字段"特殊处理: SELECT 列名用父字段的 REFRESOURCEANAME(而非主表别名 A)拼子字段 REFFIELDNAME, 如 "B.REGION_NAME AS PROVINCENAME"。要点: ① 父字段(无 UPFIELDID, 有 REFRESOURCEID)建立 LEFT JOIN 持有 JOIN 别名(REFRESOURCEANAME=B); ② 子字段(UPFIELDID=父字段ID)依附父字段 JOIN 不单独建; ③ 父子字段 REFRESOURCEID 必须相同(都指向 TBS_REGION), 子字段 REFFIELDID 指向父表字段(如 REGION_NAME)。漏配父字段只配子字段会报 "Unknown column B.REGION_NAME"。',
'UPFIELDID,子引用,JOIN,REFRESOURCEANAME,父子字段',
NULL, '2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_upfieldid_inherit');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_text_field_where', 'pitfall', 'metadata',
'text 字段不参与 UPDATE/DELETE WHERE 条件',
'BuildSQL01.joinWhere (BuildSQL01.cs:711-761) 构造 UPDATE/DELETE 的 WHERE 子句时, 对 varchar/datetime/date 类型正常生成 AND FIELD=val, 但 text 类型 (BuildSQL01.cs:735-738) 代码被注释——text 字段不参与 WHERE 条件构造。后果: ① 用 text 字段做主键的表 UPDATE/DELETE 会丢失定位条件可能误改/误删全表; ② 主键必须是 ISKEY=1 的 varchar/GUID 类型。旧值为空字符串时 datetime/date/数值生成 "AND (FIELD IS NULL)", varchar 生成 "AND (FIELD IS NULL OR FIELD=)"。',
'UPDATE t SET NAME=new WHERE (text 字段无 WHERE 条件, 全表更新)', '主键/业务键必须 varchar 类型, 绝不用 text 类型',
'text字段,joinWhere,UPDATE,DELETE,WHERE,主键类型',
NULL, '1', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_text_field_where');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_buildinsert_escape', 'rule', 'sql',
'BuildBatchInsert 字符串值手动转义引号反斜杠(非参数化)',
'BuildSQL01.getInsertStr (BuildSQL01.cs:619-655) 拼接 INSERT SQL 时, 对 varchar/text 类型手动转义: .Replace("\\","\\\\").Replace("","\\").Replace("\"","\\\""), 再用单引号包裹。注意!这不是参数化查询, 是字符串拼接, 必须转义否则 SQL 注入或语法错误。日期类型用 str_to_date(...,%Y-%m-%d %H:%i:%s) 包裹。数字类型直接拼值(不转义不引号)。null 统一拼 NULL(无引号)。BuildUpdate.joinParam 同款转义; BuildUpdate 另一分支 joinParamPa (BuildSQL01.cs:763-801) 用 @{FIELDNAME} 参数化(Dapper 命名参数), 单行更新走 joinParamPa。',
'BuildInsert,BuildUpdate,转义,引号,SQL注入,str_to_date,参数化',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_buildinsert_escape');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_reserved_param_keys', 'pitfall', 'csharp',
'doSave 跳过 CHANGENOTE/SKIPVERSION 保留参数键',
'DataController._doSave/doDelete/doUpdate 遍历 Params 构造 saveList 时, 使用 IsReservedParamKey (DataController.cs:909-912) 跳过 "CHANGENOTE" 和 "SKIPVERSION" 两个键。否则 MD.GetPath("CHANGENOTE") 返回 null 导致空引用崩溃。CHANGENOTE 是版本变更说明(前端"提交"按钮填写, 写入 tss_dev_version.CHANGENOTE); SKIPVERSION=1 是快速保存不留版本("保存"按钮)。DevVersionService.Capture 在 SKIPVERSION=1 时返回空 TouchedObj 列表跳过版本捕获, 改动折叠进下次非跳过的提交。',
'前端传 {MAIN:..., CHANGENOTE:修复Bug}, doSave 未跳过 CHANGENOTE 调 MD.GetPath(CHANGENOTE) 返回 null 崩溃', '新增类似"控制参数"必须同步加入 IsReservedParamKey 白名单',
'IsReservedParamKey,CHANGENOTE,SKIPVERSION,doSave,保留参数,版本管理',
NULL, '3', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_reserved_param_keys');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_dapper_param_register', 'rule', 'sql',
'Dapper 参数必须显式注册防 "must be defined"',
'DBHelper.getParameters (DBHelper.cs:178-186) 把 Hashtable 转 DynamicParameters, 但 Dapper 默认对 SQL 中 @VAR 要求参数集合存在对应键, 缺失报 "Parameter must be defined"。补全机制: ① DataCallService.QueryCore (DataCallService.cs:40-155) 用正则 @([A-Za-z][A-Za-z0-9_]*) 提取 FILTERSQL 中所有 @参数, 未传则补空串; ② AssistantToolExecutor 同款逻辑(AssistantToolExecutor.cs:851-855)。系统变量 @_USERID_/@_EMPID_/@_DEPTID_ 由调用方(doQuery/DataCallService)从 userInfo 注入。',
'Dapper,DynamicParameters,must be defined,系统变量,FILTERCODE,补全,正则',
NULL, NULL, 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_dapper_param_register');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_filter_resourceid', 'pitfall', 'sql',
'查 tss_resfilter 必须带 RESOURCEID 防多资源冲突',
'DataCallService.QueryCore (DataCallService.cs:42-53) 查 tss_resfilter 时用 "WHERE FILTERCODE=@fc AND RESOURCEID=@rid LIMIT 1", 必须带 RESOURCEID 精确定位。原因: FILTERCODE(F00/F01/F02/F03...)不是全局唯一, 每个资源都有自己的 F00/F01/F02。若只按 FILTERCODE 查, 可能命中其他资源的过滤器导致 SQL 错误执行。AssistantToolExecutor 等多处查询 tss_resfilter 都遵守此约定。tss_resfield/tss_resuipc 同理, 按 RESOURCEID 过滤; tss_moudlepath/tss_moudlepathrel/tss_moudleapi 按 MODULEID 过滤。',
'SELECT * FROM tss_resfilter WHERE FILTERCODE=F01 (多资源同名 F01, LIMIT 1 取到错误资源)', 'SELECT * FROM tss_resfilter WHERE FILTERCODE=F01 AND RESOURCEID=@rid',
'FILTERCODE,RESOURCEID,tss_resfilter,精确定位,多资源冲突',
NULL, NULL, 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_filter_resourceid');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_nested_datareader', 'pitfall', 'csharp',
'嵌套 DataReader 必须独立 DBHelper 连接',
'ViewOperate01.Open (ViewOperate01.cs:45-50) 注释明确: "每次查询使用独立连接, 避免嵌套 DataReader冲突"。Dapper QueryMultiple 在一个 DataReader 上多次 Read, 期间其他查询(如 SchemaManage.GetResource 也开连接)会触发 MySQL "There is already an open DataReader" 异常。代码用 DBHelper queryHelper = DB.GetDBHelper(); using(queryHelper) {...} 独立连接包裹分页查询块。SchemaManage.GetResource 也是 DBHelper helper = DB.GetDBHelper(); using(helper) {...} 独立连接且每次调用都查数据库(无缓存)。',
'在 doQuery 自定义逻辑里复用 this.operate01 的 helper 连接执行查询, 触发 DataReader 冲突', '自定义查询必须 DB.GetDBHelper() 新建独立连接, using 包裹释放',
'DataReader,嵌套,DBHelper,独立连接,Dapper,QueryMultiple',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_nested_datareader');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_vformat_billcode', 'rule', 'metadata',
'VFORMAT 单据号格式走 PSS_GENCODE 存储过程',
'M01.setBillCode (M01.cs:254-263) 单据号生成走存储过程 PSS_GENCODE: 构造 ExecInfo("PSS_GENCODE", Params), Params["TCODE"]=模板编码(如 "ORD"), Params["OCODE"]=ParamInfo Output。operate.Save(list) 执行存储过程, 从 Output 参数取生成的单据号写回 BILLCODE 字段。DataController._doSave (DataController.cs:837-845) 触发条件: ① MAINPATH 不为空; ② 资源有 BILLCODE 字段; ③ 当前 BILLCODE 值为空(首次保存); ④ tss_resfield.VFORMAT 必须配单据号模板编码(如 "ORD{yyyy}")。不是所有 BILLCODE 都自动生成, 仅首次保存且值为空才触发, 再次保存不覆盖。',
'VFORMAT,BILLCODE,PSS_GENCODE,单据号,存储过程,M01',
NULL, '2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_vformat_billcode');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_setsaveinfo_audit', 'rule', 'metadata',
'BaseControl.setSaveInfo 自动维护创建/修改时间',
'BaseControl.setSaveInfo (BaseControl.cs:64-87) 在 doSave 中被调用(DataController.cs:827), 自动维护人员时间字段: ① CREATETIME 为空(新增)→ 写 CREATEID/CREATER/CREATETIME; ② CREATETIME 非空(修改)→ 写 MODIFYID/MODIFER/MODIFYTIME。但此逻辑只作用于 MAIN 路径的第一行(view[0]), 子表 DTS 不维护。判断字段是固定名 "CREATETIME"(BaseControl.cs:74), 所以表必须有 CREATETIME 字段才会触发(HasColumn 隐式判断)。RM11Controller 等 override doSave 时若忘记调 base.doSave 会跳过此维护。',
'setSaveInfo,CREATEID,CREATER,CREATETIME,MODIFYID,MODIFER,MODIFYTIME,人员时间',
NULL, '2', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_setsaveinfo_audit');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_getvalues_magic', 'pitfall', 'csharp',
'BaseModel.GetValues 头部塞 -99999999 防 IN 空集',
'BaseModel.GetValues (BaseModel.cs:97-105) 拼接某字段所有行的值返回逗号分隔字符串时, 头部先加 "-99999999"。代码: "var idx = "-99999999"; for(...) idx += ","+GetValue(name,i);"。防 IN 子句空集兜底: 主表行数为 0 时返回 "-99999999", 子表 IN 查询不会报错(不命中任何行但不抛异常)。doOpen/DataCallService.Open 用 GetValues(RFIELDSA).Split(,) 构造 IN 参数。若不带魔数前缀, 主表为空时 Split 产生 [""] 单元素数组, Dapper 把空串传给数字字段 IN 条件可能抛 "Incorrect integer value"。关键: 业务代码不要直接调 GetValues 解析, 因返回值始终含魔数前缀。',
'var ids = MAIN.GetValues("ID").Split(,); DB.Execute("DELETE FROM t WHERE ID IN @ids", new {ids}); // 第一行 ID=-99999999 误删', '自行遍历 GetView 取值, 或 Skip(1) 跳过魔数前缀',
'GetValues,IN子句,-99999999,空集,魔数,doOpen,主子表',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_getvalues_magic');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_isvirtual_skip', 'rule', 'metadata',
'ISVIRTUAL=1 字段不参与 INSERT/UPDATE/WHERE',
'BuildSQL01 多处对 ISVIRTUAL=="1" 字段特殊处理: ① getFieldsString/getFieldsStringPa (BuildSQL01.cs:594/604) 跳过 ISVIRTUAL=1, 不参与 INSERT 列; ② joinWhere (BuildSQL01.cs:717) 跳过, 不参与 WHERE; ③ BuildAdvFilterCore (BuildSQL01.cs:417) @ui:adv 也跳过, 不生成查询条件。但 BuildQuery SELECT 不跳过——ISVIRTUAL=1 字段会输出为 "NULL AS FIELDNAME"(BuildSQL01.cs:86), 即 SELECT 列存在但值恒为 NULL。用途: 声明 VO 专用字段, 前端模板可访问该列但数据库不存储; 或标记"运行时填充字段"。',
'ISVIRTUAL,虚拟字段,INSERT,WHERE,SELECT,BuildSQL01',
NULL, '2', 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_isvirtual_skip');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_derivequerymode', 'rule', 'metadata',
'DeriveQueryMode 字段查询模式推导优先级',
'BuildSQL01.DeriveQueryMode (BuildSQL01.cs:248-310) 优先级: ① tss_resuipc.QUERYMODE 显式设置(like/eq/in/range)最高; ② 有 resuipc 时按 QUERYTYPE 优先于 EDITTYPE 推导: input/text/textarea→like, select/datepicker/autocomplete/number→eq, daterange→range; ③ 无 resuipc 时按 ResourceField 推导: ISKEY=1 或有 REFRESOURCEID → eq(外键必须精确匹配); ④ FIELDTYPE 推导: varchar/text/int/decimal/datetime 全部默认 eq(因多数 varchar 是 ID/CODE 编码不适合 like)。要某字段支持模糊搜索必须在 resuipc 配 QUERYMODE=like 或 EDITTYPE=input, 否则前端 INPUT 搜索框不会包含该字段。',
'DeriveQueryMode,QUERYMODE,QUERYTYPE,EDITTYPE,模糊搜索,eq,like',
NULL, '2,4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_derivequerymode');

-- -----------------------------------------------------------
-- 二、前端规范模板(example, PRIORITY=5)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_main_vue', 'example', 'sfc',
'示例: 规范列表页 main.vue 完整模板',
'<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    :dynamicQuery="true"
    addper="LIB_M01/A04"
    ref="list"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID"></rsAdd>
    </rs-modal>
    <template slot="header-action">
      <Button color="primary" v-per="LIB_M01/A04" icon="h-icon-plus" @click="add">添加</Button>
    </template>
    <template slot="table-action">
      <TableItem title="操作" width="200" align="center">
        <template slot-scope="{ data }">
          <Button color="primary" size="s" @click.stop="edit(data)">编辑</Button>
          <Poptip content="确定删除？" @confirm="del(data)">
            <Button color="red" size="s">删除</Button>
          </Poptip>
        </template>
      </TableItem>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from "./add.vue";
import { mapState, mapGetters, mapDateTable, Constants } from "../store";
export default {
  components: { rsAdd },
  data() {
    return {
      CDID: "",
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: "系统管理" }, { title: this.$route.meta.title }],
    };
  },
  methods: {
    add() { this.CDID = ""; this.$refs.madd.show(); },
    edit(row) { this.CDID = row.ID; this.$refs.madd.show(); },
    clickRow(row) { this.CDID = row.ID; this.$refs.madd.show(); },
    del(row) { this.$callAction({ action: `${Constants.STORE_NAME}/delete`, param: { items: [row] }, successText: "删除成功" }); },
  },
};
</script>
来源: p-admin/src/pages/b01/m01/views/main.vue。关键: 通过 :store 透传 mapDateTable/Constants 给 list-t01; slot=header-action 覆盖默认新增按钮; v-per 权限指令。',
'sfc,vue,main,列表页,list-t01,rsAdd,header-action,table-action,v-per',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_main_vue');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_add_vue', 'example', 'sfc',
'示例: 规范表单页 add.vue 完整模板',
'<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <ToolBar label="基本信息" :size="16"></ToolBar>
      <rs-form-edit ref="form" class="maxModalH rs-flex-col" :label-width="80" mode="twocolumn" :path="$MAIN"></rs-form-edit>
    </template>
    <template slot="footer">
      <Button class="ml5" @click.native="closeW">取消</Button>
      <Poptip content="确定删除？" v-per="LIB_M01/A07" v-if="ID" @confirm="del">
        <Button class="ml5" color="red">删除</Button>
      </Poptip>
      <Button class="ml5" v-per="LIB_M01/A04" color="primary" @click.native="save">确定</Button>
    </template>
  </view-dialog>
</template>
<script>
import { mapDateTable, Constants } from "../store";
import Add01 from "@/mixins/add01";
import Sel01 from "@/mixins/sel01";
export default {
  mixins: [Add01, Sel01],
  computed: { ...mapDateTable("MAIN", []) },
};
</script>
来源: p-admin/src/pages/b01/m01/views/add.vue。关键: Add01 mixin 自动处理 ID?open:add 时机, 提供 save/submit/check/verify/del; Sel01 提供通用选择器(empParam/custParam/deptParam); :path=$MAIN 自动绑定 MAIN DataTable($MAIN 由 mapDateTable 生成); closeW/save 来自 Add01 mixin, 自定义时才覆盖。',
'sfc,vue,add,view-dialog,rs-form-edit,Add01,Sel01,$MAIN,mapDateTable',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_add_vue');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_store_js', 'example', 'sfc',
'示例: 规范 store.js 完整模板',
'import db from "@/api/db";
import createStore from "@/store/createStore";
import { SelStore } from "@/store/SelStore";
const oSelStore = new SelStore();
const { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: "LIB_M01", paths: oSelStore.mixPaths() },
  storeName: "b01/m01",
  mutations: {
    SET_ENDISABLE(state, { item }) {
      const UPDATE = storeHelper.getTable("UPDATE");
      UPDATE.setValue("ISUSE", item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue("ID", item.ID);
    },
  },
  actions: {
    add({ commit }) {
      commit("INIT", { paths: ["MAIN", "DTS"] });
      commit("ADD", { path: "MAIN", item: { ISUSE: 1 } });
    },
    async endisable({ commit, dispatch }, { item }) {
      commit("SET_ENDISABLE", { item });
      const ret = await dispatch("call", {
        APICODE: "A07",
        params: { UPDATE: storeHelper.getTable("UPDATE").getXML() },
      });
      if (ret && ret.length > 0) Object.keys(ret[0]).forEach(k => { item[k] = ret[0][k]; });
    },
    ...oSelStore.mixActions(),
  },
});
export { mapState, mapGetters, mapDateTable, Constants };
来源: p-admin/src/pages/b01/m01/store.js。关键: createStore.getStore 自动注册 Vuex 模块, 同名模块先 unregister 再 register; 标准 CRUD 来自 Store03.mixActions; SelStore.mixActions() 注入通用下拉 actions(empSel/deptSel/custSel/regSel); dispatch("call") 是自定义 APICODE 通用通道。',
'sfc,store,createStore,getStore,INIT,ADD,storeHelper,getXML,dispatch,call action',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_store_js');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_router_js', 'example', 'sfc',
'示例: 规范 router.js 模板(require.ensure 懒加载)',
'export default [
  {
    path: "/b01/m01",
    name: "b01/m01",
    redirect: "/b01/m01/main",
    meta: { hideInMenu: true, title: "委托单位", notCache: true, icon: "md-home" },
    component: () => import("@/components/main"),
    children: [
      {
        path: "/b01/m01/main",
        name: "b01/m01/main",
        meta: { hideInMenu: true, title: "委托单位", notCache: true, icon: "md-home" },
        component: r => require.ensure([], () => r(require("@/pages/b01/m01")), "m01"),
      },
    ],
  }];
来源: p-admin/src/pages/b01/m01/router.js。关键: 业务子路由必须用 require.ensure 懒加载(webpack 3 语法); 禁止同步 import 业务页面(会导致 store.js 在 app store 加载前执行, SelStore 报 Cannot read properties of undefined (reading MODPATH)); index.js 仅一行 export default main(透传 views/main.vue); meta.title 自动作为 Breadcrumb/页签标题; 路由文件由 src/router.js 用 require.context 自动扫描 src/pages/*/router.js。',
'sfc,router,require.ensure,懒加载,code-split,meta,children',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_router_js');

-- -----------------------------------------------------------
-- 三、Store03 + Add01 + $callAction 用法(rule/example)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_store03_actions', 'rule', 'sfc',
'Store03 标准 actions 列表(业务 store 默认就有)',
'Store03.mixActions() 提供以下 action, 业务 store.js 默认就有无需重写: ① query(列表查询, 从 QQRY DataTable 取过滤参数); ② advQuery(高级查询); ③ open(ID, extraFilterParams 含子表自动派生); ④ add(INIT+ADD 空 row 到 MAIN/DTS); ⑤ save(payload 可带 CHANGENOTE/SKIPVERSION 用于版本管理); ⑥ delete; ⑦ call({APICODE, moduleCode, params}) 通用通道; ⑧ batch({APICODE, items, updateFields, params}) 批量回写; ⑨ flowSave({ID, ACTIONCODE}) 单据状态流转; ⑩ submit/reSubmit/check/reCheck/verify/reVerify/invalid/reInvalid; ⑪ batchSubmit/batchCheck/batchVerify/batchCheckReject/batchVerifyReject; ⑫ getBillCode({TCODE})。',
'Store03,actions,query,open,save,delete,call,batch,flowSave,CRUD',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_store03_actions');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_callaction_usage', 'rule', 'sfc',
'$callAction 五种用法',
'$callAction 定义在 src/utils/extends.js:72-124, 统一接管 busy loading/401 跳转登录/失败 $error 弹窗/成功 successText 提示 + successCall 回调 + isSuccessBack 自动关闭 Tab。五种用法: ① fire-and-forget: this.$callAction({action:"b01/m01/save", successText:"保存成功"}); ② 拿返回值: const ret = await this.$callAction({action:"s01/m17/listAssets", param:{assetType:"csharp"}}); ③ 静默(watch 联动用): await this.$callAction({action:"s01/m22/loadReleases", isBusy:false}); ④ 自定义错误: store action 内 try/catch 返回 {ok:false,msg} 信封, .vue 检查 ret.ok; ⑤ isSuccessBack: this.$callAction({action:`${storeName}/save`, successText:"操作成功", isSuccessBack:true, successCall:()=>query}); 仅框架内部 action(app/initScms、app/initModule、assistant/*)允许 $store.dispatch, 需 eslint-disable-next-line 标注。',
'$callAction,异步,Promise,RPC,isSuccessBack,isBusy,successCall',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_callaction_usage');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_ensure_module', 'rule', 'sfc',
'Store03 异步模块加载机制(ensureModule)',
'Store03 构造时 moudle 配置可能还没加载(app store 异步)。所有依赖 this.moudle 的 action 必须先 await this.ensureModule(): ① this._moudle 已就绪 → 同步 Promise.resolve; ② app store 有数据 → 同步 _initMoudle, Promise.resolve; ③ 无数据 → dispatch("app/initModule", moduleCode) 异步加载。get moudle() 兜底: 同步访问时若 app store 有数据则即时 _initMoudle, 否则抛 "模块 XX 配置未加载, 请先 await store.ensureModule()"。_initMoudle 触发 _ensureDataTables: mixState 在构造期间同步执行(dt 为空), paths 加载后用 Vue.set 补建缺失 DataTable 确保响应式。',
'Store03,ensureModule,异步加载,_initMoudle,mixState,Vue.set',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_ensure_module');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_add01_mixin', 'rule', 'sfc',
'Add01 mixin 表单通用方法(0 配置可用)',
'Add01 (src/mixins/add01.js) 提供 props/computed/methods 让表单页 0 配置即可工作。Props: ID/title/storeName/showQuery/citem。Computed(按 STATE 自动显隐按钮): ISSHOWSAVE/DELETE(STATE 空/1)/ISSHOWSUBMIT(空/1)/ISSHOWRESUBMIT(2)/ISSHOWCHECK(2)/ISSHOWRECHECK/VERIFY(3/5/19)/ISSHOWREVERIFY(6/20)/ISSHOWINVALID(6)。Methods(自动调对应 action + 成功后刷新列表): save/submit/reSubmit/check/reCheck/verify/reVerify/invalid/del/addDts/removeDts/moveUp/moveDown。save() 通用实现: this.$refs.form.valid() → $callAction({action:`${storeName}/save`, successText:"操作成功", isSuccessBack:true, successCall:advQuery/query})。onShow() watch $parent.isOpened, true 时 ID?open:add。',
'Add01,mixin,表单,审批流,ISSHOWSAVE,save,onShow',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_add01_mixin');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_sel01_mixin', 'rule', 'sfc',
'Sel01 mixin 通用选择器(empParam/custParam/deptParam/regParam)',
'Sel01 (src/mixins/sel01.js) 提供通用下拉选择器参数, 配合 HeyUI AutoComplete/TreePicker 使用。data 返回: empParam({loadData:this.empSel, keyName:"ID", titleName:"EMPNAME"})/custParam/deptParam(树形)/provinceParam/cityParam(树形联动, 传 PCODE)。这些 action 由 SelStore.mixActions() 注入到 store(store.js 必须 ...oSelStore.mixActions())。表单用法: <AutoComplete v-model="CUSTID" :option="custParam" @change="v => CUSTNAME = v.title" />。关键: loadData(INPUT, callback) 第二参 callback 是异步回填函数, 需在 store action 完成后调用。',
'Sel01,选择器,AutoComplete,TreePicker,empParam,custParam,deptParam,SelStore',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_sel01_mixin');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_edittype_21', 'rule', 'sfc',
'RsFormEdit 21 种 EDITTYPE 字段类型',
'rs-form-edit 字段类型(EDITTYPE): text/textarea/number(基础输入); select(下拉, cellProps.datas JSON 数组 或 cellProps.dict 字典名); datepicker(日期); checkbox(开关, trueValue=1 falseValue=0); editor(富文本 UEditor); image(图片显示); autocomplete(自动完成, cellProps.option 含 loadData); multiautocomplete(多选自动完成, mode=subtable 映射子表行); treepicker(树形选择); fileupload/imageupload(上传, mode=subtable 多文件映射子表); code(代码编辑器, 点击弹出 rs-code-editor, cellProps.language); slot(自定义插槽, slot 名=字段名); toolbar(分组标题, 占整行不参与校验); tableblock(可编辑子表区块, 增删移按钮 + rs-table-edit); pageaction(列表页全局按钮, 不在表单使用); index(序号列)。default-values 应用规则: 普通字段仅当为空时写入主表; multiautocomplete 子表模式仅当子表为空时重建。联动: UPDATEFIELDS 配置 "本地字段,远程字段;本地字段,远程字段"。',
'rs-form-edit,EDITTYPE,text,select,datepicker,checkbox,tableblock,multiautocomplete',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_edittype_21');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_vue_responsive', 'rule', 'sfc',
'Vue 2 响应式陷阱与 $set 预初始化',
'Vue 2 无法检测: ① 给对象新增属性(obj.newKey=v)——必须 Vue.set/this.$set; ② 数组按索引赋值(arr[idx]=v)——必须 Vue.set/splice。rs-form-edit created 钩子预初始化字段: fields.forEach(f => { if (row[f.props.key]===undefined) self.$set(row, f.props.key, ""); }); watch model deep:true 补刀(add action INIT 重建 dt.data 后字段不全)。AI 填报 applyFill 同样问题: Object.keys(converted).forEach(key => { this.$set(model, key, converted[key]); this.path.setValue(key, converted[key]); }); ——让 cell 的 :value 刷新 + 记录变更保证保存。',
'Vue2,响应式,$set,预初始化,rs-form-edit,applyFill',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_vue_responsive');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_createstore_unregister', 'rule', 'sfc',
'createStore.getStore 同名模块先卸载(actions/mutations 是追加)',
'Vuex registerModule 对同名模块的 actions/mutations 是追加而非替换(源码 _actions[type].push(handler))。若不先 unregister: ① SFC 在线编辑器反复执行 store.js → dispatch 时 N 个 handler 全部触发; ② 出现"查询被调用 N 次"、"保存触发多次"等诡异 bug。createStore.getStore 已自动处理: if (Store._modules.root._children[storeName]) Store.unregisterModule(storeName); Store.registerModule(storeName, {...}); Store 扩展时(applyStoreExtend)也走 createStore 通道重新注册, 不要直接 Store.registerModule。',
'createStore,registerModule,actions 累积,unregisterModule,SFC 编辑器',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_createstore_unregister');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_v_per_loadmore', 'rule', 'sfc',
'v-per 权限指令 + v-loadmore 虚拟滚动指令',
'v-per 权限指令: 检查 Store.state.app.fpoints[binding.value], 不存在则 display:none。用法: <Button v-per="LIB_M01/A04" color="primary">添加</Button> 或 <Button v-per="btn.perCode || btn.per">操作</Button>。fpoints 在登录后由 app store initFpoints 加载, 结构 { "LIB_M01/A04": true, ... }。v-loadmore 虚拟滚动指令: 大数据表格只渲染可视区域 + 缓冲行(spillDataNum=5)。用法: <Table v-loadmore="loadRange" :data-size="dataSize">...</Table>。实现: 监听 .ivu-table-body scroll 事件, 计算 topNum, 调 binding.value.call(topNum, topNum+showRowNum+5)。',
'v-per,v-loadmore,权限,虚拟滚动,fpoints,initFpoints',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_v_per_loadmore');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_heyui_dict', 'rule', 'sfc',
'数据字典加载(heyui.addDict + dicts state)',
'字典数据存 store.state.app.dicts(登录后 app/initDict 加载), 结构 { 字典名: {key: title} }。全局 heyui.addDict 注册在 main.vue beforeRouteEnter: Object.keys(store.state["app"].dicts).forEach(key => heyui.addDict(key, store.state["app"].dicts[key])); 字典在 Select 用法: <Select v-model="STATE" :datas="param"></Select> (param 来自 heyui.getDict("字典名") 或直接写字典名 <Select v-model="STATE" dict="STATE_DICT"></Select>)。resuipc SELECTDATA 配置规范: 普通下拉写 DICTNAME(不是 DICTCODE, 不是 k:v 内联); JSON 数组 JSON.stringify([{key,title},...]) gen.js 自动 heyui.addDict("$字段名", ...); 字段名以 $ 开头是内部字典(不进 dict state 仅注册到 heyui)。',
'字典,heyui.addDict,Select,resuipc,SELECTDATA,DICTNAME,$字典',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_heyui_dict');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_global_methods', 'rule', 'sfc',
'Vue 全局方法($alert/$error/$confirm/$busy/$free)',
'Vue.prototype 挂载的全局方法(src/utils/extends.js): this.$alert(msg) 成功提示; this.$error(msg) 错误提示; this.$confirm(content, title) 确认对话框(返回 Promise<boolean>); this.$busy(content, time) 全局 loading 返回 busy 句柄; this.$free(busy) 关闭 loading(传 busy 句柄); this.$callAsync({method, params, timeOut}) 通用异步封装; this.$callAction({...}) 调用 Vuex action; this.$getStoreCall({STORE_NAME}) 返回绑定 STORE_NAME 的 $callAction。$callAction 内部已自动 $busy/$free, 业务代码通常不需要直接调用。计量专用计算函数挂 window: $avg/$t/$abs/$maxAbs/$std/$indError/$round 等, decimalPlaces 控制精度, dfh=true 时正数加 + 前缀。',
'$busy,$alert,$confirm,$error,$avg,$std,计量计算,全局方法',
NULL, NULL, 3, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_global_methods');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_import_db_in_vue', 'pitfall', 'sfc',
'反例: .vue 中 import @/api/db 直调 db.postData',
'违规写法(违反规则 1): <script> import db from "@/api/db"; export default { methods: { async save() { const ret = await db.postData({ api: "/api/data/call/LIB_M01/A04/", params: {...} }); this.items = ret.Items; } } }; </script>。问题: ① 违反"接口调用必须通过 Store action"规则; ② ESLint no-restricted-imports 告警; ③ 数据加工在 .vue 不分层。正确做法: 封装到 store action(actions: { async save({commit}, payload) { const ret = await db.postData({...}); commit("SET_ITEMS", ret.Items); } }), 组件走 this.$callAction({action:"b01/m01/save", param:form, successText:"保存成功"})。豁免白名单: rs-uploader/*, rs-onlyoffice-preview/*, rs-word-template-editor/*, edit/ueditor/*, src/store/*, src/api/*, src/utils/extends.js。',
'vue import db.postData 直调 API', '封装到 store action, 组件走 this.$callAction',
'反例,违规,db.postData,ESLint,no-restricted-imports',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_import_db_in_vue');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_handcraft_xml', 'pitfall', 'sfc',
'反例: 手拼 XML 字符串提交后端',
'违规写法(违反"严禁手拼 XML"铁律): async createModuleBare({commit}, {moduleCode, moduleName}) { const xml = `<VSS_MOUDLE l="u" c="MODULECODE,MODULENAME" t="varchar,varchar"><a><r c0="${moduleCode}" c1="${moduleName}"/></a></VSS_MOUDLE>`; return db.postData({api:"/api/data/call/RS_M02/A04/", params:{VSS_MOUDLE:xml}}); }。问题: ① 转义/CDATA 处理不全, 特殊字符(& < > ")会破坏 XML; ② 缺少 oc 旧值, 无法生成 <m> 修改段; ③ 新增行无法回写后端生成的 ID。正确: commit("INIT", {paths:["MAIN"]}); const MAIN = storeHelper.getTable("MAIN"); MAIN.add({MODULECODE, MODULENAME}); return db.postData({api:"...", params:{VSS_MOUDLE: MAIN.getXML()}}); DataTable.add/setValue/getXML 会自动处理增/改/删段、转义、oc 旧值、回写 ID。',
'手拼 XML 字符串 template literal', '用 DataTable.add + getXML 自动处理转义/oc/ID 回写',
'反例,手拼XML,DataTable,违规,getXML,转义',
NULL, '4', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_handcraft_xml');

-- -----------------------------------------------------------
-- 四、C# 脚本 / Controller 模板(example)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_csharp_script', 'example', 'csharp',
'示例: C# 脚本接口标准模板(ScriptGlobals 全局)',
'// 脚本由 Roslyn 运行时编译, 保存即生效; 脚本体写顶层语句, 不要 namespace/class/Main
// 编码约定: SC_{模块编码}_{功能}, 如 SC_R02_M07_BACK
var id = P("ID");
if (id == "") { Response.SetError("ID 不能为空"); return; }
// 单行查询: dynamic, 字段访问 row.FIELDNAME(大写列名)
var row = DbFirst("SELECT STATE, CUSTNAME FROM tbs_xxx WHERE ID=@id", new { id });
if (row == null) { Response.SetError("记录不存在"); return; }
// 多步写操作必须包 Trans 事务
using (var t = Trans()) {
  DbExec("UPDATE tbs_xxx SET STATE=@s, MODIFYTIME=@now WHERE ID=@id", new { s = 2, now = DateTime.Now, id });
  DbExec("INSERT INTO tss_log(ID, BIZ, ACTIONID, CREATETIME) VALUES(@id,@b,@a,@now)", new { id = Guid.NewGuid().ToString("N"), b = "APPROVE", a = id, now = DateTime.Now });
  t.Commit();  // 忘 Commit 自动回滚
}
// 标量查询: COUNT/SUM
var cnt = DbScalar("SELECT COUNT(1) FROM tbs_xxx WHERE STATE=@s", new { s = 2 });
// 调 tss_sql 里 NVelocity 模板查询
var list = Sql("SS_XXX_LIST", new Hashtable { { "CUSTID", id } });
Response.SetData(new { affected = 1, count = cnt });
来源: netcore/Realso.WebAPI/Services/Scripting/ScriptGlobals.cs + ScriptAiPrompt.cs。',
'csharp,脚本,ScriptGlobals,P(),DbFirst,DbExec,DbScalar,Trans,Response,Sql,Roslyn',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_csharp_script');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_controller_domyapi', 'example', 'csharp',
'示例: doMyApi 自定义 Controller 模板',
'using System; using System.Collections; using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc; using Realso.Data.ORM; using Realso.WebAPI.Models;
namespace Realso.WebAPI.Controllers {
  [Route("api/[controller]")]            // 路由前缀: api/RM11
  [ApiController]
  public class RM11Controller : DataController {  // 继承 DataController 拿到 doQuery/doSave/doOpen 等
    // 可选: 重写 doSave 做保存前校验(base.doSave 仍执行标准保存)
    protected override void doSave(MOUDLE MD, ViewRow row, Hashtable Params) {
      this.checkDuplicateRecord(MD, row, Params); base.doSave(MD, row, Params);
    }
    // 核心: 重写 doMyApi 处理自定义 APITYPE(前端走 /api/RM11/call/{MODULE}/{APICODE})
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params) {
      string APICODE = row.GetString("APICODE");
      switch (APICODE) {
        case "A11": this.doBatchAccept(MD, row, Params); break;
        case "A30": this.doBatchReAccept(MD, row, Params); break;
        case "A12": this.doCheck(MD, row, Params); break;  // 复核
        case "A14": this.doVerify(MD, row, Params); break; // 审批
        case "A16": this.doReject(MD, row, Params); break; // 驳回
        case "A51": this.doPreviewCert(MD, row, Params); break;  // 自定义接口
        default: base.doMyApi(MD, row, APITYPE, Params); break;
      }
    }
    private void checkDuplicateRecord(MOUDLE MD, ViewRow row, Hashtable Params) {
      string sql = "SELECT COUNT(*) AS CNT FROM tck_orecord WHERE REFBILLID=@REFBILLID AND OPCODE=@OPCODE AND ISDELETED=0 AND ID<>@ID";
      // ... 查询 + responseModel.SetError("已存在...") 兜底 ...
    }
  }
}
// 路由注册(Startup.cs): services.AddMvc().AddControllersAsServices();
// 自定义 Controller 的 doMyApi 前端必须走 /api/RM11/call/ 路由, 走 /api/data/call 到不了子类!',
'csharp,controller,doMyApi,DataController,switch APICODE,base.doSave,responseModel.SetError,自定义接口',
NULL, '3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_controller_domyapi');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_csharp_engine', 'example', 'csharp',
'CSharpScriptEngine 缓存与热更新(MD5 + ForceLoadAssemblies)',
'// CSharpScriptEngine: Roslyn 运行时编译, 缓存编译后 Script<object>, 源码哈希变化才重编译
// 与前端 sfc-loader 同一模式(缓存+失效重编译)
private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
// 1. 强制加载核心程序集(.NET 懒加载陷阱: 未触碰的类型所在程序集不在 GetAssemblies 里)
private static void ForceLoadAssemblies() {
  var forceTypes = new[] {
    typeof(DBHelper), typeof(SQLManage), typeof(DataView), typeof(VelocityHelper),
    typeof(ResponseModel), typeof(ScriptGlobals), typeof(Dapper.SqlMapper),
    typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo),  // dynamic row.X 取值
    typeof(MySql.Data.MySqlClient.MySqlConnection),
    typeof(Newtonsoft.Json.JsonConvert), typeof(System.Data.IDbConnection),
  };
  foreach (var t in forceTypes) { try { var _ = t.Assembly.Location; } catch { } }
}
// 2. 编译选项: 引用全部已加载程序集 + 默认 using
private static ScriptOptions BuildOptions() {
  return ScriptOptions.Default.WithReferences(GetReferences())
    .WithImports("System","System.Linq","System.Collections","System.Collections.Generic",
                 "System.Data","System.Text","Realso.Data.DBAccess","Realso.Data.ORM",
                 "Realso.Utils","Realso.WebAPI.Services.Scripting");
}
// 3. 缓存键: code asset CODE(如 SC_R02_M07_BACK); 失效检测: 源码 MD5 不一致即重编译
// 统一代码资产表: SELECT SOURCECODE FROM tss_code_asset WHERE ASSETTYPE=csharp AND CODE=@sc AND ISDELETED=0
// 编辑保存后调 Invalidate(scriptCode) 双保险
来源: netcore/Realso.WebAPI/Services/Scripting/CSharpScriptEngine.cs。',
'csharp,Roslyn,缓存,热更新,MD5,ForceLoadAssemblies,CSharpScript,tss_code_asset',
NULL, NULL, 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_csharp_engine');

-- -----------------------------------------------------------
-- 五、SQL 过滤器骨架 + 编排接口(example)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_filter_skeleton', 'example', 'sql',
'示例: F00/F01/F02/F03 过滤器完整骨架',
'-- NVelocity 模板: $!{PARAM} 安静引用; @PARAM Dapper 参数化; #if("$!{P}"!="") ... #end 条件块
-- 系统变量: @_USERID_ / @_EMPID_ / @_DEPTID_ (自动注入)
-- 铁律: ① 禁单引号 → 用 CHAR(39) 或 @参数; ② LIKE 写 CONCAT(CHAR(37),@P,CHAR(37)) 禁 %xxx%; ③ 禁 DDL; ④ IN 批量 A.ID IN @IDLIST; ⑤ 日期 str_to_date(@D,%Y-%m-%d)

-- F00 单条查询
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
VALUES ("flt_xxx_f00", "vck_xxx_001", "F00", "A.ID = @ID", NULL, "单条查询");

-- F01 列表模糊搜索(INPUT 多字段 OR + 数据权限)
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
VALUES ("flt_xxx_f01", "vck_xxx_001", "F01",
"1=1 #if("$!{INPUT}"!="") AND (A.NAME LIKE CONCAT(''%'',@INPUT,''%'') OR A.CODE LIKE CONCAT(''%'',@INPUT,''%'')) #end AND A.ISDELETED = 0",
"CREATETIME DESC", "列表查询");

-- F02 高级查询(多条件组合, 日期范围)
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
VALUES ("flt_xxx_f02", "vck_xxx_001", "F02",
"1=1 #if("$!{NAME}"!="") AND A.NAME LIKE CONCAT(''%'',@NAME,''%'') #end
#if("$!{TYPE}"!="") AND A.TYPE = @TYPE #end
#if("$!{STATUS}"!="") AND A.STATUS IN (@STATUS) #end
#if("$!{SENDDATE_start}"!="") AND A.SENDDATE >= str_to_date(@SENDDATE_start,''%Y-%m-%d'') #end
#if("$!{SENDDATE_end}"!="") AND A.SENDDATE <= str_to_date(@SENDDATE_end,''%Y-%m-%d'') #end
AND A.ISDELETED = 0",
"CREATETIME DESC", "高级查询");

-- F03 批量操作
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
VALUES ("flt_xxx_f03", "vck_xxx_001", "F03", "A.ID IN @ID AND A.ISDELETED = 0", NULL, "批量操作");',
'sql,nvelocity,FILTERSQL,F00,F01,F02,F03,LIKE CONCAT,IN,日期范围,数据权限',
NULL, '2,3', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_filter_skeleton');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_changeitem', 'example', 'metadata',
'示例: 向导 ChangeItem 结构(TOOL→CATEGORY 映射)',
'// 每条变更项对应一行 tss_aidev_changeitem, 字段见 Realso.WebAPI/Models/AiDev/ChangeSet.cs
new ChangeItem {
  ID = Guid.NewGuid().ToString("N"),
  CHANGESETID = changesetId,    // 6 步共享
  ITEMSEQ = 0,                  // ChangeSetEngine.AppendItem 自动分配
  CATEGORY = ChangeItem.CAT_PHYSICAL_TABLE,
  ACTION = ChangeItem.ACTION_CREATE,
  TOOL = "create_physical_table",
  TARGET = "TBS_PROJECT_FEE",
  SQLCONTENT = "CREATE TABLE tbs_project_fee ...",
  METADATA = "{...}",
  RATIONALE = "为费用管理模块创建主表",
  WARNINGS = "",
  ITEMSTATUS = ChangeItem.STATUS_DRAFT,
}
// CATEGORY 常量: physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/billflow/page/button/merged
// ACTION 常量: create/alter/update/delete
// TOOL→CATEGORY 映射(MapToolToCategory): create_physical_table→physical_table/create; add_field_to_table→field/alter; define_dataview→dataview/create; define_reference_field→field/create; configure_resource_field→field/create|update; configure_ui_field→ui/update; create_dict→dict/create; define_filter→filter/create; register_module→module/create; define_api/define_sql_api/define_script_api/define_script_flow_api→api/create; update_script_flow_api→api/update; create_menu→menu/create; create_funcpoints→permission/create; define_page→page/create; define_button→button/create; create_sfc_module→module/create',
'metadata,ChangeItem,ChangeSet,CATEGORY,ACTION,TOOL,TARGET,SQLCONTENT,METADATA,wizard,stepToolMap',
'wizard', '0,1,2,3,4,5', 5, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_changeitem');

-- -----------------------------------------------------------
-- 六、SFC 桥梁模块 + 三形态对比(glossary/example)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_example_sfc_bridge', 'example', 'sfc',
'SFC 桥梁模块(module-bridge.js 清单)',
'// webpack 启动时把核心基础设施暴露到 window.__SFC_MODULES__, 供 SFC 代码 __sfc_require__ 调用
// 每个条目必须带 __esModule: true 标记, 否则 Babel _interopRequireDefault 会再包一层 .default.default
window.__SFC_MODULES__ = Object.assign(window.__SFC_MODULES__ || {}, {
  // 类型 C: 全局库
  "vue":     { __esModule: true, default: Vue, Vue: Vue },
  "heyui":   { __esModule: true, default: HeyUI },
  "vuex":    { __esModule: true, default: Vuex },
  "axios":   { __esModule: true, default: axios },
  // 类型 A: 项目内部模块
  "@/api/db":                                  { __esModule: true, default: db },
  "@/store":                                   { __esModule: true, default: Store },
  "@/store/createStore":                       { __esModule: true, default: createStore },
  "@/store/Store03":                           { __esModule: true, default: Store03 },
  "@/store/BaseStore":                         { __esModule: true, default: BaseStore },
  "@/mixins/add01":                            { __esModule: true, default: Add01 },
  "@/components/generic-module/generic-store": { __esModule: true, default: { getGenericStore }, getGenericStore },
  // 工具
  "rs-vcore/utils/Date": { __esModule: true, default: rsVcoreDate, dateToString: dateToString },
});
// SFC 内可用 import 清单(白名单): vue/heyui/vuex/axios + @/api/db + @/store + @/store/createStore + @/store/Store03 + @/store/BaseStore + @/mixins/add01 + @/components/generic-module/generic-store + rs-vcore/utils/Date
来源: p-admin/src/sfc-loader/module-bridge.js。',
'sfc,bridge,window,__SFC_MODULES__,__esModule,桥梁,白名单,import',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_example_sfc_bridge');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_gloss_sfc_three_forms', 'glossary', 'sfc',
'SFC 三种代码形态对比(extendjs/store/vue)',
'| 类型 | editTarget | 文件路径约定 | export default 结构 | 合并到 |
|------|-----------|-------------|---------------------|--------|
| 页面扩展 JS | extendjs | @/modules/{MC}/{pageCode}.js | { methods, computed, init, mounted } | generic-module/generic-form 组件实例 |
| Store 扩展 | store | @/modules/{MC}/store.js | { actions, mutations } | Vuex 模块(Store03) |
| SFC Vue 组件 | vue | 自定义路径(如 @/pages/r02/m07/views/main.vue) | Vue SFC 标准结构(options) | 动态路由/RemoteRoute 加载 |
判断规则: 用户说"页面扩展"或 extendjs → 模式 1; 说"store 扩展" → 模式 2; 说"vue 组件/页面" → 模式 3。关键约束: ① 纯 JS 文件(extendjs/store)禁止 import(SFC new Function 执行无闭包), 通过 window.__SFC_MODULES__ 桥接; ② SFC Vue 组件可以 import 白名单内模块; ③ extendjs 不能用 this.FIELDNAME 直接访问字段值(组件无 mapDateTable 绑定), 必须用 this.$store.state[ns].dt.MAIN.data[0] 或 this.$refs.table.currentRow; ④ ISSHOW 方法签名必须包含 { row, key, path } 框架自动传入; ⑤ store 扩展的 action 不能引用外部变量(无闭包), 模块编码用 state.MODULECODE; ⑥ Store03 已有 actions(query/open/save/delete/...)不会被覆盖, 扩展只新增。',
'glossary,sfc,extendjs,store,vue,三种模式,import 白名单,ISSHOW,桥接',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_gloss_sfc_three_forms');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_subtable_pattern', 'rule', 'sfc',
'主子表处理(DTSA/DTSB/DTS + tableblock 区块)',
'主子表通过 tss_moudlepath 配置: MAIN=主表, DTSA/DTSB/DTSC...=子表。tss_moudlepathrel 配置外键关系(PATHNAMEA=MAIN, PATHNAMEB=DTSx, RFIELDSA/B)。方式 1 (推荐): uiset 配 EDITTYPE=tableblock, SELECTDATA: { subtable: "DTS", targetModule: "" }; gen.js 解析为 cellProps.tableBlockConfig; rs-form-edit 渲染 ToolBar + rs-table-edit(默认增删移 4 按钮); 按钮 BTNCODE 映射 subAdd→add/subRemove→remove/subUp→up/subDown→down; 自定义按钮配 tss_module_button(BTNAREA=子表路径)。方式 2: 手动管理(add.vue 直接操作 store.dt.DTSA): DTSA.clear(); ret.DTSA.forEach(r => DTSA.add({ACCEPTID:r.ACCEPTID, ACCEPTCODE:r.ACCEPTCODE})); mutation SET_ENTRYNUM 保存前给子表行写 ENTRYNUM(行号), 子表展示顺序由 ENTRYNUM 控制。',
'子表,DTSA,tableblock,主子表,tss_moudlepathrel,SET_ENTRYNUM,tableBlockConfig',
NULL, '4', 4, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_subtable_pattern');

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 本批 42 条新增(深度 ORM 规则 + 完整前端模板 + C#/SQL 示例)
-- 配合 33(35) + 34(43) = 共 120 条种子记忆
-- 覆盖维度: ORM 元数据 + 前端规范 + SFC + C# 脚本 + SQL 模板 + 向导 + 业务对象 + 调试经验
