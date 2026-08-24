-- ============================================================
-- 声明式 AI 工具(tss_ai_tool) — 数据库升级
-- 内容: tss_ai_tool(声明式只读工具注册表) + ORM 元数据注册
--       + RS_M24 工具管理模块(通用模块自举) + 2 个种子工具
-- 日期: 2026-07-17
-- 机制: DeclarativeSqlToolExecutor 读表生成 OpenAI 工具定义;
--       仅允许 SELECT(模板原文+注参后双重校验); MAXROWS 截断;
--       同名内置工具优先(C# 定义不可被 DB 覆盖)
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_ai_tool
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_ai_tool (
  ID           VARCHAR(36) NOT NULL COMMENT '主键',
  TOOLNAME     VARCHAR(64) NOT NULL COMMENT '工具名(全局唯一,小写下划线)',
  TOOLSET      VARCHAR(32) NOT NULL COMMENT '所属工具集(assistant/formfill/dev/sfc)',
  DESCRIPTION  VARCHAR(1000) NOT NULL COMMENT '给 LLM 看的工具描述',
  PARAMS       TEXT COMMENT 'JSON Schema(parameters 对象原文)',
  EXECUTORTYPE VARCHAR(16) NOT NULL COMMENT 'sql(声明式只读)/builtin(代码内置,占位)',
  SQLCODE      VARCHAR(64) DEFAULT NULL COMMENT 'EXECUTORTYPE=sql 时指向 tss_sql.SQLCODE(仅 SELECT)',
  MAXROWS      INT DEFAULT 200 COMMENT '结果行数上限(防 token 爆炸)',
  ENABLED      TINYINT DEFAULT 1,
  REMARK       VARCHAR(200) DEFAULT NULL,
  ISDELETED    TINYINT DEFAULT 0,
  PRIMARY KEY (ID),
  UNIQUE KEY uk_toolname (TOOLNAME)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='声明式 AI 工具注册表';

-- -----------------------------------------------------------
-- 2. ORM 资源注册: TBS_AI_TOOL + VSS_AI_TOOL
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_ai_tool_001', 'TBS_AI_TOOL', 'tss_ai_tool', 'TABLE', NULL, 'TSS_AI_TOOL', '声明式 AI 工具表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_ai_tool_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_ai_tool_001', 'VSS_AI_TOOL', 'tss_ai_tool', 'DATAVIEW', 'tbs_ai_tool_001', 'A', '声明式 AI 工具视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_ai_tool_001');

-- TBS 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_id', 'tbs_ai_tool_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_name', 'tbs_ai_tool_001', NULL, 'TOOLNAME', 'varchar', 0, 0, 64, '工具名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_set', 'tbs_ai_tool_001', NULL, 'TOOLSET', 'varchar', 0, 0, 32, '工具集'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_set');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_desc', 'tbs_ai_tool_001', NULL, 'DESCRIPTION', 'varchar', 0, 0, 1000, '工具描述'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_desc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tat_params', 'tbs_ai_tool_001', NULL, 'PARAMS', 'text', 0, 1, '参数Schema'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_params');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_exectype', 'tbs_ai_tool_001', NULL, 'EXECUTORTYPE', 'varchar', 0, 0, 16, '执行类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_exectype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_sqlcode', 'tbs_ai_tool_001', NULL, 'SQLCODE', 'varchar', 0, 1, 64, 'SQL模板编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_sqlcode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tat_maxrows', 'tbs_ai_tool_001', NULL, 'MAXROWS', 'int', 0, 1, '行数上限'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_maxrows');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tat_enabled', 'tbs_ai_tool_001', NULL, 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tat_remark', 'tbs_ai_tool_001', NULL, 'REMARK', 'varchar', 0, 1, 200, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tat_isdeleted', 'tbs_ai_tool_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tat_isdeleted');

-- VSS 字段 (REFFIELDID→TBS)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_id', 'vss_ai_tool_001', 'rf_tat_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_name', 'vss_ai_tool_001', 'rf_tat_name', 'TOOLNAME', 'varchar', 0, 0, 64, '工具名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_set', 'vss_ai_tool_001', 'rf_tat_set', 'TOOLSET', 'varchar', 0, 0, 32, '工具集'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_set');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_desc', 'vss_ai_tool_001', 'rf_tat_desc', 'DESCRIPTION', 'varchar', 0, 0, 1000, '工具描述'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_desc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vat_params', 'vss_ai_tool_001', 'rf_tat_params', 'PARAMS', 'text', 0, 1, '参数Schema'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_params');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_exectype', 'vss_ai_tool_001', 'rf_tat_exectype', 'EXECUTORTYPE', 'varchar', 0, 0, 16, '执行类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_exectype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_sqlcode', 'vss_ai_tool_001', 'rf_tat_sqlcode', 'SQLCODE', 'varchar', 0, 1, 64, 'SQL模板编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_sqlcode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vat_maxrows', 'vss_ai_tool_001', 'rf_tat_maxrows', 'MAXROWS', 'int', 0, 1, '行数上限'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_maxrows');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vat_enabled', 'vss_ai_tool_001', 'rf_tat_enabled', 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vat_remark', 'vss_ai_tool_001', 'rf_tat_remark', 'REMARK', 'varchar', 0, 1, 200, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vat_isdeleted', 'vss_ai_tool_001', 'rf_tat_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vat_isdeleted');

-- -----------------------------------------------------------
-- 3. 过滤器: F00(按ID) / F01(列表)
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_at_f00', 'vss_ai_tool_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_at_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_at_f01', 'vss_ai_tool_001', 'F01', '1=1 AND A.ISDELETED=0', 'TOOLSET, TOOLNAME', '工具列表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_at_f01');

-- -----------------------------------------------------------
-- 4. UI 配置: VSS_AI_TOOL
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_idx', 'vss_ai_tool_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_name', 'vss_ai_tool_001', 'rf_vat_name', '工具名', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_name');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_set', 'vss_ai_tool_001', 'rf_vat_set', '工具集', 2, NULL, 2, 'select', 'assistant:assistant,formfill:formfill,dev:dev,sfc:sfc'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_set');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_desc', 'vss_ai_tool_001', 'rf_vat_desc', '工具描述(给LLM看)', 3, NULL, 3, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_desc');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_params', 'vss_ai_tool_001', 'rf_vat_params', '参数Schema(JSON)', NULL, NULL, 4, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_params');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_exectype', 'vss_ai_tool_001', 'rf_vat_exectype', '执行类型', 4, NULL, 5, 'select', 'sql:SQL查询(只读),builtin:代码内置'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_exectype');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_sqlcode', 'vss_ai_tool_001', 'rf_vat_sqlcode', 'SQL模板编码', 5, NULL, 6, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_sqlcode');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_maxrows', 'vss_ai_tool_001', 'rf_vat_maxrows', '行数上限', NULL, NULL, 7, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_maxrows');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_enabled', 'vss_ai_tool_001', 'rf_vat_enabled', '启用', 6, NULL, 8, 'checkbox', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_enabled');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_at_remark', 'vss_ai_tool_001', 'rf_vat_remark', '备注', 7, NULL, 9, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_at_remark');

-- -----------------------------------------------------------
-- 5. RS_M24 模块注册 (AI 工具管理, 通用模块自举)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m24_module_001', 'RS_M24', 'AI工具管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M24');

SET @m24 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M24');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m24_qry', @m24, 'QRY', 'vss_ai_tool_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m24_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m24_qqry', @m24, 'QQRY', 'vss_ai_tool_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m24_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m24_sel', @m24, 'SEL', 'vss_ai_tool_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m24_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m24_main', @m24, 'MAIN', 'vss_ai_tool_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m24_main');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m24_a01', @m24, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m24_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m24_a02', @m24, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m24_a02');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m24_a04', @m24, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m24_a04');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m24_a07', @m24, 'A07', 'delete', 'delete', '', NULL, '删除', 'MAIN', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m24_a07');

-- -----------------------------------------------------------
-- 6. 菜单 + 功能点 (通用模块路由 /g/RS_M24/main)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m24_001', 'RS_M24', 'AI工具管理', '/g/RS_M24/main', '3e3c83ce2b3c475b82902478c89c27c0', 240, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M24');

SET @f24 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M24');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m24_a01', @f24, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m24_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m24_a04', @f24, 'A04', '编辑', 'A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m24_a04');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m24_a07', @f24, 'A07', '删除', 'A07'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m24_a07');

-- -----------------------------------------------------------
-- 7. 通用模块页面 + 按钮配置 (自举)
-- -----------------------------------------------------------
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, QUERYAPICODE, SORTNO, PAGECONFIG, ISDELETED)
SELECT 'mp_rs_m24_main', 'RS_M24', 'main', '工具列表', 'list', 'standard', 'A01', 1, '{"defaultFormPageCode":"form"}', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m24_main');
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, OPENAPICODE, SAVEAPICODE, SORTNO, ISDELETED)
SELECT 'mp_rs_m24_form', 'RS_M24', 'form', '工具编辑', 'form', 'standard', 'A02', 'A04', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m24_form');

INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m24_main_add', 'mp_rs_m24_main', 'RS_M24', 'A04', '添加', 'crud', 'add', 'header', 'direct', 'RS_M24/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m24_main_add');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m24_form_save', 'mp_rs_m24_form', 'RS_M24', 'A04', '保存', 'crud', 'save', 'footer', 'direct', 'RS_M24/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m24_form_save');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, SORTNO, ISDELETED)
SELECT 'mb_rs_m24_form_cancel', 'mp_rs_m24_form', 'RS_M24', NULL, '取消', 'crud', 'cancel', 'footer', 'direct', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m24_form_cancel');

-- -----------------------------------------------------------
-- 8. 纳入版本管理
-- -----------------------------------------------------------
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_aitool', 'VSS_AI_TOOL', 'aitool', 'TOOLNAME', 'TOOLNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_AI_TOOL');

-- -----------------------------------------------------------
-- 9. 种子: 2 个声明式工具示例
-- -----------------------------------------------------------
-- 9.1 SQL 模板: 按模块查页面配置(dev 工具集用)
INSERT INTO tss_sql (SQLID, SQLCODE, SQLTYPE, SQLTXT, REMARK)
SELECT 'sql_ai_modpages', 'SS_AI_MODPAGES', 'mysql', 'SELECT MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, QUERYAPICODE, OPENAPICODE, SAVEAPICODE, SORTNO FROM tss_module_page WHERE ISDELETED=0
#if("$!{MODULECODE}"!="")
AND MODULECODE=@MODULECODE
#end
ORDER BY MODULECODE, SORTNO', 'AI工具: 查模块页面配置'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_sql WHERE SQLCODE='SS_AI_MODPAGES');

-- 9.2 SQL 模板: 查 AI 工具注册表(自举示例)
INSERT INTO tss_sql (SQLID, SQLCODE, SQLTYPE, SQLTXT, REMARK)
SELECT 'sql_ai_toollist', 'SS_AI_TOOLLIST', 'mysql', 'SELECT TOOLNAME, TOOLSET, DESCRIPTION, SQLCODE, MAXROWS, ENABLED FROM tss_ai_tool WHERE ISDELETED=0
#if("$!{TOOLSET}"!="")
AND TOOLSET=@TOOLSET
#end
ORDER BY TOOLSET, TOOLNAME', 'AI工具: 查声明式工具注册表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_sql WHERE SQLCODE='SS_AI_TOOLLIST');

-- 9.3 工具: search_module_pages(dev 集)
INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_modpages', 'search_module_pages', 'dev', '查询模块的页面配置(tss_module_page)：PAGECODE/PAGENAME/PAGETYPE/COMPONENTTYPE/QUERYAPICODE/OPENAPICODE/SAVEAPICODE/SORTNO。定义页面或按钮前调用，了解已有页面避免重复。', '{"type":"object","properties":{"moduleCode":{"type":"string","description":"模块编码，不传查全部"}}}', 'sql', 'SS_AI_MODPAGES', 100, 1, '种子: 查页面配置', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='search_module_pages');

-- 9.4 工具: list_ai_tools(dev 集, 自举示例)
INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_toollist', 'list_ai_tools', 'dev', '查询已注册的声明式 AI 工具(tss_ai_tool)：TOOLNAME/TOOLSET/DESCRIPTION/SQLCODE/MAXROWS/ENABLED。新增声明式工具前调用，避免重名。', '{"type":"object","properties":{"toolset":{"type":"string","description":"工具集(assistant/formfill/dev/sfc)，不传查全部"}}}', 'sql', 'SS_AI_TOOLLIST', 100, 1, '种子: 查工具注册表', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='list_ai_tools');
