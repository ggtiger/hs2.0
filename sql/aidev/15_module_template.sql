-- ============================================================
-- 业务模板市场(tss_module_template) — 数据库升级
-- 内容: tss_module_template 建表 + ORM 元数据注册 + RS_M25 模块
--       + 通用模块页面配置(自举) + 版本管理纳入 + 声明式 AI 工具种子
-- 日期: 2026-07-17
-- 机制: TemplateExporter 遍历模块关联元数据生成幂等脚本;
--       安装 = 变量替换 → UpgradeExecutor.Import/Execute(单事务+快照回滚)
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_module_template
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_module_template (
  ID           VARCHAR(36) NOT NULL COMMENT '主键',
  TEMPLATECODE VARCHAR(64) NOT NULL COMMENT '模板编码(唯一)',
  TEMPLATENAME VARCHAR(128) NOT NULL COMMENT '模板名称',
  CATEGORY     VARCHAR(32) DEFAULT NULL COMMENT '业务分类(b01/r01/r02/s01)',
  DESCRIPTION  VARCHAR(500) DEFAULT NULL COMMENT '描述',
  VARIABLES    TEXT COMMENT '安装变量定义 JSON [{name,label,default,required}]',
  SCRIPT       LONGTEXT COMMENT '元数据脚本(与 .aidev.sql 同构, 含 ${VAR} 占位)',
  SOURCEINFO   VARCHAR(200) DEFAULT NULL COMMENT '来源(模块编码或会话编码)',
  VERSION      VARCHAR(16) DEFAULT '1.0.0',
  ENABLED      TINYINT DEFAULT 1,
  CREATEID     VARCHAR(64) DEFAULT NULL COMMENT '创建人ID',
  CREATER      VARCHAR(64) DEFAULT NULL COMMENT '创建人姓名',
  MODIFYID     VARCHAR(64) DEFAULT NULL COMMENT '修改人ID',
  MODIFER      VARCHAR(64) DEFAULT NULL COMMENT '修改人姓名',
  MODIFYTIME   DATETIME DEFAULT NULL COMMENT '修改时间',
  CREATETIME   DATETIME DEFAULT NULL COMMENT '创建时间',
  ISDELETED    TINYINT DEFAULT 0,
  PRIMARY KEY (ID),
  UNIQUE KEY uk_templatecode (TEMPLATECODE)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='业务模块模板';

-- -----------------------------------------------------------
-- 2. ORM 资源注册: TBS_MODULE_TEMPLATE + VSS_MODULE_TEMPLATE
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_module_tpl_001', 'TBS_MODULE_TEMPLATE', 'tss_module_template', 'TABLE', NULL, 'TSS_MODULE_TEMPLATE', '业务模块模板表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_module_tpl_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_module_tpl_001', 'VSS_MODULE_TEMPLATE', 'tss_module_template', 'DATAVIEW', 'tbs_module_tpl_001', 'A', '业务模块模板视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_module_tpl_001');

-- TBS 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_id', 'tbs_module_tpl_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_code', 'tbs_module_tpl_001', NULL, 'TEMPLATECODE', 'varchar', 0, 0, 64, '模板编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_name', 'tbs_module_tpl_001', NULL, 'TEMPLATENAME', 'varchar', 0, 0, 128, '模板名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_category', 'tbs_module_tpl_001', NULL, 'CATEGORY', 'varchar', 0, 1, 32, '业务分类'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_category');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_desc', 'tbs_module_tpl_001', NULL, 'DESCRIPTION', 'varchar', 0, 1, 500, '描述'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_desc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_vars', 'tbs_module_tpl_001', NULL, 'VARIABLES', 'text', 0, 1, '安装变量'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_vars');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_script', 'tbs_module_tpl_001', NULL, 'SCRIPT', 'text', 0, 1, '模板脚本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_script');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_src', 'tbs_module_tpl_001', NULL, 'SOURCEINFO', 'varchar', 0, 1, 200, '来源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_src');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_version', 'tbs_module_tpl_001', NULL, 'VERSION', 'varchar', 0, 1, 16, '版本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_enabled', 'tbs_module_tpl_001', NULL, 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_createid', 'tbs_module_tpl_001', NULL, 'CREATEID', 'varchar', 0, 1, 64, '创建人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_createby');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_createtime', 'tbs_module_tpl_001', NULL, 'CREATETIME', 'datetime', 0, 1, '创建时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_isdeleted', 'tbs_module_tpl_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_isdeleted');

-- VSS 字段 (REFFIELDID→TBS)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_id', 'vss_module_tpl_001', 'rf_tmt_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_code', 'vss_module_tpl_001', 'rf_tmt_code', 'TEMPLATECODE', 'varchar', 0, 0, 64, '模板编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_name', 'vss_module_tpl_001', 'rf_tmt_name', 'TEMPLATENAME', 'varchar', 0, 0, 128, '模板名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_category', 'vss_module_tpl_001', 'rf_tmt_category', 'CATEGORY', 'varchar', 0, 1, 32, '业务分类'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_category');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_desc', 'vss_module_tpl_001', 'rf_tmt_desc', 'DESCRIPTION', 'varchar', 0, 1, 500, '描述'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_desc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_vars', 'vss_module_tpl_001', 'rf_tmt_vars', 'VARIABLES', 'text', 0, 1, '安装变量'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_vars');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_script', 'vss_module_tpl_001', 'rf_tmt_script', 'SCRIPT', 'text', 0, 1, '模板脚本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_script');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_src', 'vss_module_tpl_001', 'rf_tmt_src', 'SOURCEINFO', 'varchar', 0, 1, 200, '来源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_src');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_version', 'vss_module_tpl_001', 'rf_tmt_version', 'VERSION', 'varchar', 0, 1, 16, '版本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_enabled', 'vss_module_tpl_001', 'rf_tmt_enabled', 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_createid', 'vss_module_tpl_001', 'rf_tmt_createid', 'CREATEID', 'varchar', 0, 1, 64, '创建人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_createby');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_createtime', 'vss_module_tpl_001', 'rf_tmt_createtime', 'CREATETIME', 'datetime', 0, 1, '创建时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_creater', 'tbs_module_tpl_001', NULL, 'CREATER', 'varchar', 0, 1, 64, '创建人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_creater');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_creater', 'vss_module_tpl_001', 'rf_tmt_creater', 'CREATER', 'varchar', 0, 1, 64, '创建人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_creater');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_modifyid', 'tbs_module_tpl_001', NULL, 'MODIFYID', 'varchar', 0, 1, 64, '修改人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_modifyid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_modifyid', 'vss_module_tpl_001', 'rf_tmt_modifyid', 'MODIFYID', 'varchar', 0, 1, 64, '修改人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_modifyid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tmt_modifer', 'tbs_module_tpl_001', NULL, 'MODIFER', 'varchar', 0, 1, 64, '修改人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_modifer');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vmt_modifer', 'vss_module_tpl_001', 'rf_tmt_modifer', 'MODIFER', 'varchar', 0, 1, 64, '修改人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_modifer');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tmt_modifytime', 'tbs_module_tpl_001', NULL, 'MODIFYTIME', 'datetime', 0, 1, '修改时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tmt_modifytime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_modifytime', 'vss_module_tpl_001', 'rf_tmt_modifytime', 'MODIFYTIME', 'datetime', 0, 1, '修改时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_modifytime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vmt_isdeleted', 'vss_module_tpl_001', 'rf_tmt_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vmt_isdeleted');

-- -----------------------------------------------------------
-- 3. 过滤器: F00(按ID) / F01(列表)
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_mt_f00', 'vss_module_tpl_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_mt_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_mt_f01', 'vss_module_tpl_001', 'F01', '1=1 AND A.ISDELETED=0 AND A.ENABLED=1
#if("$!{TEMPLATECODE}"!="")
AND A.TEMPLATECODE LIKE CONCAT(CHAR(37),@TEMPLATECODE,CHAR(37))
#end
#if("$!{TEMPLATENAME}"!="")
AND A.TEMPLATENAME LIKE CONCAT(CHAR(37),@TEMPLATENAME,CHAR(37))
#end
#if("$!{CATEGORY}"!="")
AND A.CATEGORY=@CATEGORY
#end', 'CREATETIME DESC, ID DESC', '模板列表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_mt_f01');

-- -----------------------------------------------------------
-- 4. UI 配置: VSS_MODULE_TEMPLATE
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_idx', 'vss_module_tpl_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_code', 'vss_module_tpl_001', 'rf_vmt_code', '模板编码', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_code');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_name', 'vss_module_tpl_001', 'rf_vmt_name', '模板名称', 2, 2, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_name');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_category', 'vss_module_tpl_001', 'rf_vmt_category', '分类', 3, 3, 3, 'select', 'b01:基础数据,r01:报告/检验,r02:记录/报表,s01:系统管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_category');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_desc', 'vss_module_tpl_001', 'rf_vmt_desc', '描述', 4, NULL, 4, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_desc');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_src', 'vss_module_tpl_001', 'rf_vmt_src', '来源模块', 5, NULL, 5, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_src');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_version', 'vss_module_tpl_001', 'rf_vmt_version', '版本', 6, NULL, 6, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_version');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_creater', 'vss_module_tpl_001', 'rf_vmt_creater', '创建人', 7, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_createby');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_mt_createtime', 'vss_module_tpl_001', 'rf_vmt_createtime', '创建时间', 8, NULL, NULL, 'datepicker', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_mt_createtime');

-- -----------------------------------------------------------
-- 5. RS_M25 模块注册 (模板市场)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m25_module_001', 'RS_M25', '模板市场'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M25');

SET @m25 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M25');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m25_qry', @m25, 'QRY', 'vss_module_tpl_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m25_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m25_qqry', @m25, 'QQRY', 'vss_module_tpl_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m25_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m25_sel', @m25, 'SEL', 'vss_module_tpl_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m25_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m25_main', @m25, 'MAIN', 'vss_module_tpl_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m25_main');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m25_a01', @m25, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m25_a02', @m25, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a02');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m25_a04', @m25, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a04');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m25_a07', @m25, 'A07', 'delete', 'delete', '', NULL, '删除', 'MAIN', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a07');
-- A05 导出模块为模板(自定义, APITYPE/ACTIONCODE 置 NULL → RModuleTplController.doMyApi)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, ENTRYNUM)
SELECT 'ma_m25_a05', @m25, 'A05', NULL, '导出模块为模板', NULL, 'MAIN', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a05');
-- A06 安装模板(自定义)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, ENTRYNUM)
SELECT 'ma_m25_a06', @m25, 'A06', NULL, '安装模板', NULL, 'MAIN', 6
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a06');

-- -----------------------------------------------------------
-- 6. 菜单 + 功能点 (本地路由 s01/m25)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m25_001', 'RS_M25', '模板市场', 's01/m25', '3e3c83ce2b3c475b82902478c89c27c0', 250, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M25');

SET @f25 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M25');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m25_a01', @f25, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m25_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m25_a05', @f25, 'A05', '导出模板', 'A05'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m25_a05');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m25_a06', @f25, 'A06', '安装模板(高危)', 'A06'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m25_a06');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m25_a07', @f25, 'A07', '删除模板', 'A07'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m25_a07');

-- -----------------------------------------------------------
-- 7. 纳入版本管理 + 声明式 AI 工具种子
-- -----------------------------------------------------------
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_modtpl', 'VSS_MODULE_TEMPLATE', 'template', 'TEMPLATECODE', 'TEMPLATENAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_MODULE_TEMPLATE');

-- SQL 模板: 搜索业务模板
INSERT INTO tss_sql (SQLID, SQLCODE, SQLTYPE, SQLTXT, REMARK)
SELECT 'sql_ai_modtpl', 'SS_AI_MODTPL', 'mysql', 'SELECT TEMPLATECODE, TEMPLATENAME, CATEGORY, DESCRIPTION, SOURCEINFO, VERSION, CREATETIME FROM tss_module_template WHERE ISDELETED=0 AND ENABLED=1
#if("$!{KEYWORD}"!="")
AND (TEMPLATECODE LIKE CONCAT(CHAR(37),@KEYWORD,CHAR(37)) OR TEMPLATENAME LIKE CONCAT(CHAR(37),@KEYWORD,CHAR(37)))
#end
ORDER BY ID DESC', 'AI工具: 搜索业务模板'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_sql WHERE SQLCODE='SS_AI_MODTPL');

INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_modtpl', 'search_module_template', 'dev', '搜索业务模板市场(tss_module_template)：TEMPLATECODE/TEMPLATENAME/CATEGORY/DESCRIPTION/SOURCEINFO/VERSION。用户要从已有模板创建模块时先搜模板，找到后用 read_module_template 读详情。', '{"type":"object","properties":{"keyword":{"type":"string","description":"编码或名称关键词，不传返回全部"}}}', 'sql', 'SS_AI_MODTPL', 50, 1, '种子: 搜索业务模板', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='search_module_template');

-- -----------------------------------------------------------
-- 9. A08 AI 会话存为模板接口(2026-07-17 追加)
-- -----------------------------------------------------------
SET @m25 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M25');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, ENTRYNUM)
SELECT 'ma_m25_a08', @m25, 'A08', NULL, 'AI会话存为模板', NULL, 'MAIN', 8
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m25_a08');

SET @f25 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M25');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m25_a08', @f25, 'A08', 'AI会话存为模板', 'A08'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m25_a08');
