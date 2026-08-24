-- ============================================================
-- APITYPE=csharp 在线 C# 脚本接口 — 数据库升级
-- 内容: tss_api_script 建表 + tss_moudleapi 加 SCRIPTCODE 列
--       + ORM 元数据注册 + RS_M21 脚本管理模块(通用模块自举)
--       + SC_SCRIPT_CHECK 编译检查种子脚本
-- 日期: 2026-07-17
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_api_script
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_api_script (
  ID          VARCHAR(36) NOT NULL COMMENT '主键',
  SCRIPTCODE  VARCHAR(64) NOT NULL COMMENT '脚本编码(唯一)',
  SCRIPTNAME  VARCHAR(128) DEFAULT NULL COMMENT '脚本名称',
  SOURCECODE  LONGTEXT COMMENT 'C# 脚本源码',
  VERSION     INT DEFAULT 1 COMMENT '版本号(展示用;热更新按源码哈希检测)',
  REMARK      VARCHAR(500) DEFAULT NULL COMMENT '备注',
  ISDELETED   TINYINT DEFAULT 0 COMMENT '逻辑删除',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_scriptcode (SCRIPTCODE)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='API C# 脚本(在线编辑热更新)';

-- -----------------------------------------------------------
-- 2. tss_moudleapi 加 SCRIPTCODE 列(MySQL 5.7 无 IF NOT EXISTS,用 information_schema 守卫)
-- -----------------------------------------------------------
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='tss_moudleapi' AND COLUMN_NAME='SCRIPTCODE');
SET @ddl := IF(@col_exists=0,
  'ALTER TABLE tss_moudleapi ADD COLUMN SCRIPTCODE VARCHAR(64) NULL COMMENT ''APITYPE=csharp 时指向 tss_api_script.SCRIPTCODE''',
  'SELECT 1');
PREPARE stmt FROM @ddl; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------
-- 3. ORM 资源注册: TBS_API_SCRIPT + VSS_API_SCRIPT
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_api_script_001', 'TBS_API_SCRIPT', 'tss_api_script', 'TABLE', NULL, 'TSS_API_SCRIPT', 'API C# 脚本表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_api_script_001');

INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_api_script_001', 'VSS_API_SCRIPT', 'tss_api_script', 'DATAVIEW', 'tbs_api_script_001', 'A', 'API C# 脚本视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_api_script_001');

-- -----------------------------------------------------------
-- 4. tss_resfield: TBS_API_SCRIPT 字段
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tas_id', 'tbs_api_script_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tas_code', 'tbs_api_script_001', NULL, 'SCRIPTCODE', 'varchar', 0, 0, 64, '脚本编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tas_name', 'tbs_api_script_001', NULL, 'SCRIPTNAME', 'varchar', 0, 1, 128, '脚本名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tas_source', 'tbs_api_script_001', NULL, 'SOURCECODE', 'text', 0, 1, NULL, 'C# 源码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_source');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tas_version', 'tbs_api_script_001', NULL, 'VERSION', 'int', 0, 1, '版本号'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tas_remark', 'tbs_api_script_001', NULL, 'REMARK', 'varchar', 0, 1, 500, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tas_isdeleted', 'tbs_api_script_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tas_isdeleted');

-- -----------------------------------------------------------
-- 5. tss_resfield: VSS_API_SCRIPT 字段 (REFFIELDID→TBS)
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vas_id', 'vss_api_script_001', 'rf_tas_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vas_code', 'vss_api_script_001', 'rf_tas_code', 'SCRIPTCODE', 'varchar', 0, 0, 64, '脚本编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vas_name', 'vss_api_script_001', 'rf_tas_name', 'SCRIPTNAME', 'varchar', 0, 1, 128, '脚本名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vas_source', 'vss_api_script_001', 'rf_tas_source', 'SOURCECODE', 'text', 0, 1, NULL, 'C# 源码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_source');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vas_version', 'vss_api_script_001', 'rf_tas_version', 'VERSION', 'int', 0, 1, '版本号'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vas_remark', 'vss_api_script_001', 'rf_tas_remark', 'REMARK', 'varchar', 0, 1, 500, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vas_isdeleted', 'vss_api_script_001', 'rf_tas_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vas_isdeleted');

-- -----------------------------------------------------------
-- 6. 过滤器: F00(按编码) / F01(列表)
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_as_f00', 'vss_api_script_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_as_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_as_f01', 'vss_api_script_001', 'F01', '1=1 AND A.ISDELETED=0', 'SCRIPTCODE', '脚本列表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_as_f01');

-- -----------------------------------------------------------
-- 7. UI 配置: VSS_API_SCRIPT (列表 + 表单, SOURCECODE 用 code 编辑器)
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_idx', 'vss_api_script_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_code', 'vss_api_script_001', 'rf_vas_code', '脚本编码', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_code');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_name', 'vss_api_script_001', 'rf_vas_name', '脚本名称', 2, 2, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_name');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_source', 'vss_api_script_001', 'rf_vas_source', 'C# 源码', NULL, NULL, 3, 'code', '{"language":"csharp"}'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_source');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_version', 'vss_api_script_001', 'rf_vas_version', '版本', 3, NULL, NULL, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_version');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_as_remark', 'vss_api_script_001', 'rf_vas_remark', '备注', 4, NULL, 4, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_as_remark');

-- -----------------------------------------------------------
-- 8. RS_M21 模块注册 (API 脚本管理, 通用模块自举)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m21_module_001', 'RS_M21', 'API脚本管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M21');

SET @m21 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M21');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m21_qry', @m21, 'QRY', 'vss_api_script_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m21_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m21_qqry', @m21, 'QQRY', 'vss_api_script_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m21_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m21_sel', @m21, 'SEL', 'vss_api_script_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m21_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m21_main', @m21, 'MAIN', 'vss_api_script_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m21_main');

-- 标准 CRUD 接口
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m21_a01', @m21, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m21_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m21_a02', @m21, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m21_a02');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m21_a04', @m21, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m21_a04');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m21_a07', @m21, 'A07', 'delete', 'delete', '', NULL, '删除', 'MAIN', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m21_a07');
-- A05 编译检查(APITYPE=csharp, 指向种子脚本)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, APINAME, SCRIPTCODE, ENTRYNUM)
SELECT 'ma_m21_a05', @m21, 'A05', 'checkScript', 'csharp', 'MAIN', '编译检查', 'SC_SCRIPT_CHECK', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m21_a05');

-- -----------------------------------------------------------
-- 9. 菜单 + 功能点 (通用模块路由 /g/RS_M21/main)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m21_001', 'RS_M21', 'API脚本管理', '/g/RS_M21/main', '3e3c83ce2b3c475b82902478c89c27c0', 210, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M21');

SET @f21 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M21');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m21_a01', @f21, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m21_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m21_a04', @f21, 'A04', '编辑(脚本写权限,高危)', 'A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m21_a04');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m21_a07', @f21, 'A07', '删除', 'A07'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m21_a07');

-- -----------------------------------------------------------
-- 10. 通用模块页面 + 按钮配置 (自举: 管理界面零代码)
-- -----------------------------------------------------------
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, QUERYAPICODE, SORTNO, PAGECONFIG, ISDELETED)
SELECT 'mp_rs_m21_main', 'RS_M21', 'main', '脚本列表', 'list', 'standard', 'A01', 1, '{"defaultFormPageCode":"form"}', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m21_main');
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, OPENAPICODE, SAVEAPICODE, SORTNO, ISDELETED)
SELECT 'mp_rs_m21_form', 'RS_M21', 'form', '脚本编辑', 'form', 'standard', 'A02', 'A04', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m21_form');

INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m21_main_add', 'mp_rs_m21_main', 'RS_M21', 'A04', '添加', 'crud', 'add', 'header', 'direct', 'RS_M21/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m21_main_add');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m21_form_save', 'mp_rs_m21_form', 'RS_M21', 'A04', '保存', 'crud', 'save', 'footer', 'direct', 'RS_M21/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m21_form_save');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, SORTNO, ISDELETED)
SELECT 'mb_rs_m21_form_cancel', 'mp_rs_m21_form', 'RS_M21', NULL, '取消', 'crud', 'cancel', 'footer', 'direct', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m21_form_cancel');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, COLOR, SHOWCOND, SORTNO, ISDELETED)
SELECT 'mb_rs_m21_form_check', 'mp_rs_m21_form', 'RS_M21', 'A05', '编译检查', 'custom', 'custom', 'footer', 'direct', 'primary', 'ID!=null', 3, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m21_form_check');

-- -----------------------------------------------------------
-- 11. 种子脚本: SC_SCRIPT_CHECK (编译检查, SOURCECODE 用 0x HEX 写入)
-- -----------------------------------------------------------
INSERT INTO tss_api_script (ID, SCRIPTCODE, SCRIPTNAME, SOURCECODE, VERSION, REMARK, ISDELETED)
SELECT 'as_sc_script_check', 'SC_SCRIPT_CHECK', '编译检查', 0x2f2f20e7bc96e8af91e6a380e69fa5e8849ae69cacefbc8852535f4d32312041303520e68ea5e58fa3efbc89efbc9ae6a380e69fa5e8849ae69cace79a8420432320e8afade6b3950a2f2f20e58f82e695b0efbc9a534f55524345434f4445efbc88e58fafe98089efbc8ce79bb4e4bca0e6ba90e7a081efbc89efbc9b4944efbc88e58fafe98089efbc8ce68c8920494420e4bb8ee7bb9fe4b880e4bba3e7a081e8b584e4baa7e8a1a8e8afbbe58f96e5b7b2e4bf9de5ad98e8849ae69cacefbc890a76617220737263203d20502822534f55524345434f444522293b0a69662028737263203d3d202222202626205028224944222920213d20222229207b0a20207661722072203d2044624669727374282253454c45435420534f55524345434f44452046524f4d207473735f636f64655f61737365742057484552452049443d406964222c206e6577207b206964203d2050282249442229207d293b0a2020696620287220213d206e756c6c2920737263203d2028737472696e6729722e534f55524345434f44453b0a7d0a766172206572726f7273203d20435368617270536372697074456e67696e652e436865636b53796e74617828737263293b0a526573706f6e73652e53657444617461286e6577207b20706173736564203d206572726f72732e436f756e74203d3d20302c206572726f7273207d293b0a, 1, 'RS_M21 A05 接口: 检查 C# 脚本语法', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_api_script WHERE SCRIPTCODE='SC_SCRIPT_CHECK');

-- 示例脚本: SC_SAMPLE_DELETE (演示 Db/DbExec/Trans/Response 用法)
INSERT INTO tss_api_script (ID, SCRIPTCODE, SCRIPTNAME, SOURCECODE, VERSION, REMARK, ISDELETED)
SELECT 'as_sc_sample_delete', 'SC_SAMPLE_DELETE', '示例-逻辑删除', 0x2f2f20e7a4bae4be8be8849ae69cacefbc9ae68c8920494420e980bbe8be91e588a0e999a4e5bd93e5898de8849ae69cace8aeb0e5bd95efbc88e6bc94e7a4ba2044622f4462457865632f5472616e732f526573706f6e736520e794a8e6b395efbc890a766172206964203d20502822494422293b0a696620286964203d3d20222229207b20526573706f6e73652e5365744572726f722822494420e4b88de883bde4b8bae7a9ba22293b2072657475726e3b207d0a7573696e6720287661722074203d205472616e73282929207b0a20204462457865632822555044415445207473735f6170695f7363726970742053455420495344454c455445443d312057484552452049443d406964222c206e6577207b206964207d293b0a2020742e436f6d6d697428293b0a7d0a526573706f6e73652e53657444617461286e6577207b206166666563746564203d20312c206d657373616765203d2022e5b7b2e588a0e999a422207d293b0a, 1, '脚本写法示例, 可删除', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_api_script WHERE SCRIPTCODE='SC_SAMPLE_DELETE');

-- -----------------------------------------------------------
-- 12. 补充: SCRIPTCODE 资源字段注册(2026-07-17 修复)
-- tss_moudleapi 加列后漏注册 resfield, 导致 MD.GetAPI 行缺列,
-- row.GetString("SCRIPTCODE") 报 "The given key was not present in the dictionary"
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tma_scriptcode', '11', NULL, 'SCRIPTCODE', 'varchar', 0, 1, 64, 'C# 脚本编码(APITYPE=csharp)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tma_scriptcode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vma_scriptcode', '15', 'rf_tma_scriptcode', 'SCRIPTCODE', 'varchar', 0, 1, 64, 'C# 脚本编码(APITYPE=csharp)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vma_scriptcode');
