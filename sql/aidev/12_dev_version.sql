-- ============================================================
-- 在线开发版本管理 — 数据库升级
-- 内容: tss_dev_version(版本快照) + tss_dev_version_cfg(纳管资源配置)
--       + ORM 元数据注册 + RS_M22 版本中心模块 + 首批纳管资源种子
-- 日期: 2026-07-17
-- 机制: DataController._doSave/doDelete 统一拦截, 对 cfg 内资源的
--       DataView 自动抓前后镜像写入版本行(与业务保存同事务)
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_dev_version (版本快照)
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_dev_version (
  ID            VARCHAR(36) NOT NULL COMMENT '主键',
  OBJTYPE       VARCHAR(32) NOT NULL COMMENT '对象类型(sfc/api_script/sql/page/button/module/resource/...)',
  OBJID         VARCHAR(64) NOT NULL COMMENT '对象主键(源表.ID)',
  OBJCODE       VARCHAR(200) DEFAULT NULL COMMENT '对象编码(冗余便于检索)',
  OBJNAME       VARCHAR(200) DEFAULT NULL COMMENT '对象名称(冗余)',
  VERSION       INT NOT NULL COMMENT '对象内递增版本号',
  OPTYPE        VARCHAR(8) NOT NULL COMMENT 'insert/update/delete/rollback',
  BEFORECONTENT LONGTEXT COMMENT '变更前快照(文本或JSON), insert 为 NULL',
  AFTERCONTENT  LONGTEXT COMMENT '变更后快照, delete 为 NULL',
  CHANGENOTE    VARCHAR(500) DEFAULT NULL COMMENT '变更说明',
  TAG           VARCHAR(64) DEFAULT NULL COMMENT '发布点标签(不被清理)',
  PINNED        TINYINT DEFAULT 0 COMMENT '置顶保留(不被清理)',
  CREATEID      VARCHAR(64) DEFAULT NULL COMMENT '操作人ID',
  CREATER       VARCHAR(64) DEFAULT NULL COMMENT '操作人姓名',
  CREATETIME   DATETIME DEFAULT NULL COMMENT '操作时间',
  SRCTABLE      VARCHAR(64) DEFAULT NULL COMMENT '来源物理表(回滚定位用)',
  ISDELETED     TINYINT DEFAULT 0 COMMENT '逻辑删除',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_obj_ver (OBJTYPE, OBJID, VERSION),
  KEY idx_code (OBJTYPE, OBJCODE),
  KEY idx_time (CREATETIME)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='在线开发版本快照';

-- -----------------------------------------------------------
-- 2. 建表: tss_dev_version_cfg (纳管资源配置)
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_dev_version_cfg (
  ID           VARCHAR(36) NOT NULL COMMENT '主键',
  RESOURCENAME VARCHAR(64) NOT NULL COMMENT '纳入版本管理的资源名(saveList 中 DataView 的资源名)',
  OBJTYPE      VARCHAR(32) NOT NULL COMMENT '版本对象类型',
  CODEEXPR     VARCHAR(200) DEFAULT NULL COMMENT 'OBJCODE 取值字段(逗号分隔多个字段,用 / 连接)',
  NAMEEXPR     VARCHAR(200) DEFAULT NULL COMMENT 'OBJNAME 取值字段(同上)',
  MAXVERSIONS  INT DEFAULT 50 COMMENT '每对象保留版本数(超出清理最旧非PINNED/无TAG)',
  ENABLED      TINYINT DEFAULT 1,
  ISDELETED    TINYINT DEFAULT 0,
  PRIMARY KEY (ID),
  UNIQUE KEY uk_res (RESOURCENAME)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='版本管理资源配置';

-- -----------------------------------------------------------
-- 3. 首批纳管资源种子
-- -----------------------------------------------------------
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_sfc', 'VCK_SFC_TEMPLATE', 'sfc', 'MODULEPATH', 'TEMPLATENAME', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VCK_SFC_TEMPLATE');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_sfc2', 'TBS_SFC_TEMPLATE', 'sfc', 'MODULEPATH', 'TEMPLATENAME', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='TBS_SFC_TEMPLATE');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_apiscript', 'VSS_API_SCRIPT', 'api_script', 'SCRIPTCODE', 'SCRIPTNAME', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_API_SCRIPT');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_modpage', 'VCK_MODULE_PAGE', 'page', 'MODULECODE,PAGECODE', 'PAGENAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VCK_MODULE_PAGE');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_modbutton', 'VCK_MODULE_BUTTON', 'button', 'MODULECODE,BTNNAME', 'BTNNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VCK_MODULE_BUTTON');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_moudle', 'VSS_MOUDLE', 'module', 'MODULECODE', 'MODULENAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_MOUDLE');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_resource', 'VSS_RESOURCE', 'resource', 'RESOURCENAME', 'COMMENTS', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_RESOURCE');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_resfield', 'VSS_RESFIELD', 'field', 'FIELDNAME', 'COMMENTS', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_RESFIELD');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_resfilter', 'VSS_RESFILTER', 'filter', 'FILTERCODE', 'REMARK', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_RESFILTER');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_resuipc', 'VSS_RESUIPC', 'ui', 'LABELNAME', 'LABELNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_RESUIPC');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_moudleapi', 'VSS_MOUDLEAPI', 'api', 'APICODE', 'APINAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_MOUDLEAPI');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_sql', 'VSS_sQL', 'sql', 'SQLCODE', 'REMARK', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_sQL');

-- -----------------------------------------------------------
-- 4. ORM 资源注册: TBS_DEV_VERSION + VSS_DEV_VERSION
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_dev_version_001', 'TBS_DEV_VERSION', 'tss_dev_version', 'TABLE', NULL, 'TSS_DEV_VERSION', '在线开发版本快照表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_dev_version_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_dev_version_001', 'VSS_DEV_VERSION', 'tss_dev_version', 'DATAVIEW', 'tbs_dev_version_001', 'A', '在线开发版本快照视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_dev_version_001');

-- TBS 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_id', 'tbs_dev_version_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_objtype', 'tbs_dev_version_001', NULL, 'OBJTYPE', 'varchar', 0, 0, 32, '对象类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_objtype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_objid', 'tbs_dev_version_001', NULL, 'OBJID', 'varchar', 0, 0, 64, '对象ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_objid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_objcode', 'tbs_dev_version_001', NULL, 'OBJCODE', 'varchar', 0, 1, 200, '对象编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_objcode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_objname', 'tbs_dev_version_001', NULL, 'OBJNAME', 'varchar', 0, 1, 200, '对象名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_objname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_version', 'tbs_dev_version_001', NULL, 'VERSION', 'int', 0, 0, '版本号'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_optype', 'tbs_dev_version_001', NULL, 'OPTYPE', 'varchar', 0, 0, 8, '操作类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_optype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_before', 'tbs_dev_version_001', NULL, 'BEFORECONTENT', 'text', 0, 1, '变更前快照'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_before');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_after', 'tbs_dev_version_001', NULL, 'AFTERCONTENT', 'text', 0, 1, '变更后快照'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_after');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_note', 'tbs_dev_version_001', NULL, 'CHANGENOTE', 'varchar', 0, 1, 500, '变更说明'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_note');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_tag', 'tbs_dev_version_001', NULL, 'TAG', 'varchar', 0, 1, 64, '发布点标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_tag');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_pinned', 'tbs_dev_version_001', NULL, 'PINNED', 'int', 0, 1, '置顶保留'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_pinned');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_createid', 'tbs_dev_version_001', NULL, 'CREATEID', 'varchar', 0, 1, 64, '操作人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_createby');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_creater', 'tbs_dev_version_001', NULL, 'CREATER', 'varchar', 0, 1, 64, '操作人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_createbyname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_createtime', 'tbs_dev_version_001', NULL, 'CREATETIME', 'datetime', 0, 1, '操作时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tdv_srctable', 'tbs_dev_version_001', NULL, 'SRCTABLE', 'varchar', 0, 1, 64, '来源物理表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_srctable');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tdv_isdeleted', 'tbs_dev_version_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tdv_isdeleted');

-- VSS 字段 (REFFIELDID→TBS)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_id', 'vss_dev_version_001', 'rf_tdv_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_objtype', 'vss_dev_version_001', 'rf_tdv_objtype', 'OBJTYPE', 'varchar', 0, 0, 32, '对象类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_objtype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_objid', 'vss_dev_version_001', 'rf_tdv_objid', 'OBJID', 'varchar', 0, 0, 64, '对象ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_objid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_objcode', 'vss_dev_version_001', 'rf_tdv_objcode', 'OBJCODE', 'varchar', 0, 1, 200, '对象编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_objcode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_objname', 'vss_dev_version_001', 'rf_tdv_objname', 'OBJNAME', 'varchar', 0, 1, 200, '对象名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_objname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_version', 'vss_dev_version_001', 'rf_tdv_version', 'VERSION', 'int', 0, 0, '版本号'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_optype', 'vss_dev_version_001', 'rf_tdv_optype', 'OPTYPE', 'varchar', 0, 0, 8, '操作类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_optype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_before', 'vss_dev_version_001', 'rf_tdv_before', 'BEFORECONTENT', 'text', 0, 1, '变更前快照'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_before');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_after', 'vss_dev_version_001', 'rf_tdv_after', 'AFTERCONTENT', 'text', 0, 1, '变更后快照'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_after');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_note', 'vss_dev_version_001', 'rf_tdv_note', 'CHANGENOTE', 'varchar', 0, 1, 500, '变更说明'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_note');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_tag', 'vss_dev_version_001', 'rf_tdv_tag', 'TAG', 'varchar', 0, 1, 64, '发布点标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_tag');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_pinned', 'vss_dev_version_001', 'rf_tdv_pinned', 'PINNED', 'int', 0, 1, '置顶保留'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_pinned');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_createid', 'vss_dev_version_001', 'rf_tdv_createid', 'CREATEID', 'varchar', 0, 1, 64, '操作人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_createby');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_creater', 'vss_dev_version_001', 'rf_tdv_creater', 'CREATER', 'varchar', 0, 1, 64, '操作人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_createbyname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_createtime', 'vss_dev_version_001', 'rf_tdv_createtime', 'CREATETIME', 'datetime', 0, 1, '操作时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vdv_srctable', 'vss_dev_version_001', 'rf_tdv_srctable', 'SRCTABLE', 'varchar', 0, 1, 64, '来源物理表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_srctable');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vdv_isdeleted', 'vss_dev_version_001', 'rf_tdv_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vdv_isdeleted');

-- -----------------------------------------------------------
-- 5. 过滤器: F00(按ID) / F01(列表, 按对象筛选)
-- 注意: FILTERSQL 禁单引号(NVelocity 铁律), LIKE 用 CHAR(37) 代替 '%'
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_dv_f00', 'vss_dev_version_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_dv_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_dv_f01', 'vss_dev_version_001', 'F01', '1=1 AND A.ISDELETED=0
#if("$!{OBJTYPE}"!="")
AND A.OBJTYPE=@OBJTYPE
#end
#if("$!{OBJCODE}"!="")
AND A.OBJCODE LIKE CONCAT(CHAR(37),@OBJCODE,CHAR(37))
#end
#if("$!{OBJID}"!="")
AND A.OBJID=@OBJID
#end
#if("$!{CREATER}"!="")
AND A.CREATER LIKE CONCAT(CHAR(37),@CREATER,CHAR(37))
#end', 'CREATETIME DESC, ID DESC', '版本列表(按对象/操作人筛选)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_dv_f01');
-- 已部署环境的修正(幂等 UPDATE): 旧版 LIKE 含单引号字面量, 统一替换为 CHAR(37) 写法
UPDATE tss_resfilter SET FILTERSQL='1=1 AND A.ISDELETED=0
#if("$!{OBJTYPE}"!="")
AND A.OBJTYPE=@OBJTYPE
#end
#if("$!{OBJCODE}"!="")
AND A.OBJCODE LIKE CONCAT(CHAR(37),@OBJCODE,CHAR(37))
#end
#if("$!{OBJID}"!="")
AND A.OBJID=@OBJID
#end
#if("$!{CREATER}"!="")
AND A.CREATER LIKE CONCAT(CHAR(37),@CREATER,CHAR(37))
#end' WHERE ID='rf_dv_f01';

-- -----------------------------------------------------------
-- 6. UI 配置: VSS_DEV_VERSION (版本中心列表列)
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_idx', 'vss_dev_version_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_objtype', 'vss_dev_version_001', 'rf_vdv_objtype', '类型', 1, 1, NULL, 'select', 'sfc:SFC页面,api_script:C#脚本,sql:SQL模板,page:页面配置,button:按钮配置,module:模块,resource:资源,field:字段,filter:过滤器,ui:UI配置,api:接口'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_objtype');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_objcode', 'vss_dev_version_001', 'rf_vdv_objcode', '对象编码', 2, 2, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_objcode');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_objname', 'vss_dev_version_001', 'rf_vdv_objname', '对象名称', 3, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_objname');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_version', 'vss_dev_version_001', 'rf_vdv_version', '版本', 4, NULL, NULL, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_version');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_optype', 'vss_dev_version_001', 'rf_vdv_optype', '操作', 5, NULL, NULL, 'select', 'insert:新增,update:修改,delete:删除,rollback:回滚'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_optype');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_note', 'vss_dev_version_001', 'rf_vdv_note', '变更说明', 6, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_note');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_tag', 'vss_dev_version_001', 'rf_vdv_tag', '标签', 7, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_tag');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_creater', 'vss_dev_version_001', 'rf_vdv_creater', '操作人', 8, 3, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_createbyname');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_dv_createtime', 'vss_dev_version_001', 'rf_vdv_createtime', '操作时间', 9, NULL, NULL, 'datepicker', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_dv_createtime');

-- -----------------------------------------------------------
-- 7. RS_M22 模块注册 (版本中心)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m22_module_001', 'RS_M22', '开发版本中心'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M22');

SET @m22 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M22');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m22_qry', @m22, 'QRY', 'vss_dev_version_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m22_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m22_qqry', @m22, 'QQRY', 'vss_dev_version_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m22_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m22_sel', @m22, 'SEL', 'vss_dev_version_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m22_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m22_main', @m22, 'MAIN', 'vss_dev_version_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m22_main');

-- 标准接口
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m22_a01', @m22, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m22_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m22_a02', @m22, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m22_a02');
-- A05 回滚(自定义接口, APITYPE/ACTIONCODE 置 NULL → RDevVersionController.doMyApi)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, ENTRYNUM)
SELECT 'ma_m22_a05', @m22, 'A05', NULL, '回滚到该版本', NULL, 'MAIN', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m22_a05');

-- -----------------------------------------------------------
-- 8. 菜单 + 功能点 (本地路由 s01/m22)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m22_001', 'RS_M22', '版本中心', 's01/m22', '3e3c83ce2b3c475b82902478c89c27c0', 220, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M22');

SET @f22 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M22');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m22_a01', @f22, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m22_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m22_a05', @f22, 'A05', '回滚(高危)', 'A05'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m22_a05');
