-- 30_release.sql
-- 内容: 发布中心 — tss_release 建表 + ORM 元数据注册 + RS_M22 扩展接口
-- 1. CREATE TABLE tss_release
-- 2. TBS/VSS 资源注册 + resfield + resuipc + 过滤器
-- 3. RS_M22 扩展：moudleapi 加 A08/A09/A10/A11
-- 4. tss_dev_version 加 idx_tag 索引
-- 5. D0701 字典补 release 类型项

-- ============================================================
-- 1. 建表
-- ============================================================
CREATE TABLE IF NOT EXISTS tss_release (
  ID            VARCHAR(36) PRIMARY KEY,
  RELEASECODE   VARCHAR(64) NOT NULL COMMENT '发布编码(如 REL_20260719_01)',
  RELEASENAME   VARCHAR(128) NOT NULL COMMENT '发布名称',
  TAG           VARCHAR(64) NOT NULL COMMENT '关联的版本 TAG',
  OBJCOUNT      INT DEFAULT 0 COMMENT '包含对象数',
  STATUS        VARCHAR(16) DEFAULT 'draft' COMMENT 'draft/published/deployed',
  SCRIPTCONTENT LONGTEXT COMMENT '.aidev.sql 格式发布脚本',
  SCRIPTHASH    VARCHAR(64) COMMENT '脚本哈希(防篡改)',
  REMARK        VARCHAR(500),
  CREATEID      VARCHAR(64),
  CREATER       VARCHAR(64),
  CREATETIME    DATETIME,
  MODIFYID      VARCHAR(64),
  MODIFER       VARCHAR(64),
  MODIFYTIME    DATETIME,
  ISDELETED     TINYINT DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- 唯一键: uk_releasecode
ALTER TABLE tss_release ADD UNIQUE INDEX uk_releasecode (RELEASECODE);
ALTER TABLE tss_release ADD INDEX idx_tag (TAG);
ALTER TABLE tss_release ADD INDEX idx_status (STATUS);

-- ============================================================
-- 2. ORM 资源注册
-- ============================================================

-- TBS_RELEASE
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, RESOURCEANAME, COMMENTS)
SELECT 'tbs_release_001', 'TBS_RELEASE', 'tss_release', 'TABLE', 'A', '发布包物理表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_release_001');

-- VSS_RELEASE
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_release_001', 'VSS_RELEASE', 'tss_release', 'DATAVIEW', 'tbs_release_001', 'A', '发布包视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_release_001');

-- TBS resfield
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, KEYGENTYPE, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_id', 'tbs_release_001', 'ID', 'varchar', 36, 1, 'GUID', NULL, 'ID' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_id');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_code', 'tbs_release_001', 'RELEASECODE', 'varchar', 64, 0, NULL, '发布编码' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_code');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_name', 'tbs_release_001', 'RELEASENAME', 'varchar', 128, 0, NULL, '发布名称' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_name');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_tag', 'tbs_release_001', 'TAG', 'varchar', 64, 0, NULL, 'TAG' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_tag');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_objcount', 'tbs_release_001', 'OBJCOUNT', 'int', 11, 0, '0', '对象数' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_objcount');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_status', 'tbs_release_001', 'STATUS', 'varchar', 16, 0, 'draft', '状态' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_status');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_script', 'tbs_release_001', 'SCRIPTCONTENT', 'longtext', 0, NULL, '发布脚本' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_script');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_hash', 'tbs_release_001', 'SCRIPTHASH', 'varchar', 64, 0, NULL, '脚本哈希' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_hash');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_remark', 'tbs_release_001', 'REMARK', 'varchar', 500, 0, NULL, '备注' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_createid', 'tbs_release_001', 'CREATEID', 'varchar', 64, 0, NULL, '创建人ID' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_createid');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_creater', 'tbs_release_001', 'CREATER', 'varchar', 64, 0, NULL, '创建人' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_creater');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_createtime', 'tbs_release_001', 'CREATETIME', 'datetime', 0, NULL, '创建时间' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_tr_isdeleted', 'tbs_release_001', 'ISDELETED', 'tinyint', 4, 0, '0', '逻辑删除' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tr_isdeleted');

-- VSS resfield (REFFIELDID → TBS 字段)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, KEYGENTYPE, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_id', 'vss_release_001', 'rf_tr_id', 'ID', 'varchar', 36, 1, 'GUID', NULL, 'ID' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_code', 'vss_release_001', 'rf_tr_code', 'RELEASECODE', 'varchar', 64, 0, NULL, '发布编码' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_name', 'vss_release_001', 'rf_tr_name', 'RELEASENAME', 'varchar', 128, 0, NULL, '发布名称' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_tag', 'vss_release_001', 'rf_tr_tag', 'TAG', 'varchar', 64, 0, NULL, 'TAG' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_tag');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_objcount', 'vss_release_001', 'rf_tr_objcount', 'OBJCOUNT', 'int', 11, 0, '0', '对象数' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_objcount');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_status', 'vss_release_001', 'rf_tr_status', 'STATUS', 'varchar', 16, 0, 'draft', '状态' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_status');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_script', 'vss_release_001', 'rf_tr_script', 'SCRIPTCONTENT', 'longtext', 0, NULL, '发布脚本' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_script');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_hash', 'vss_release_001', 'rf_tr_hash', 'SCRIPTHASH', 'varchar', 64, 0, NULL, '脚本哈希' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_hash');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_remark', 'vss_release_001', 'rf_tr_remark', 'REMARK', 'varchar', 500, 0, NULL, '备注' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_createid', 'vss_release_001', 'rf_tr_createid', 'CREATEID', 'varchar', 64, 0, NULL, '创建人ID' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_createid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_creater', 'vss_release_001', 'rf_tr_creater', 'CREATER', 'varchar', 64, 0, NULL, '创建人' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_creater');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_createtime', 'vss_release_001', 'rf_tr_createtime', 'CREATETIME', 'datetime', 0, NULL, '创建时间' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_createtime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, ISKEY, DEFAULTVALUE, REMARK)
SELECT 'rf_vr_isdeleted', 'vss_release_001', 'rf_tr_isdeleted', 'ISDELETED', 'tinyint', 4, 0, '0', '逻辑删除' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vr_isdeleted');

-- VSS UI 配置 (列表显示)
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_code', 'vss_release_001', 'rf_vr_code', 'RELEASECODE', '发布编码', 1, 1, 1, 'text', 180 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_code');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_name', 'vss_release_001', 'rf_vr_name', 'RELEASENAME', '发布名称', 2, 0, 2, 'text', 200 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_name');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_tag', 'vss_release_001', 'rf_vr_tag', 'TAG', 'TAG', 3, 2, 3, 'text', 120 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_tag');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_objcount', 'vss_release_001', 'rf_vr_objcount', 'OBJCOUNT', '对象数', 4, 0, 0, 'numberinput', 80 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_objcount');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_status', 'vss_release_001', 'rf_vr_status', 'STATUS', '状态', 5, 3, 0, 'text', 100 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_status');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_creater', 'vss_release_001', 'rf_vr_creater', 'CREATER', '创建人', 6, 0, 0, 'text', 100 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_creater');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_createtime', 'vss_release_001', 'rf_vr_createtime', 'CREATETIME', '创建时间', 7, 0, 0, 'datepicker', 160 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_createtime');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SHOWLENGTH)
SELECT 'uipc_vr_remark', 'vss_release_001', 'rf_vr_remark', 'REMARK', '备注', 8, 0, 4, 'textarea', 200 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_vr_remark');

-- VSS 过滤器
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'rf_vr_f00', 'vss_release_001', 'F00', '1=1 AND A.ID=@ID', 'A.CREATETIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vr_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'rf_vr_f01', 'vss_release_001', 'F01', '1=1 #if("$!{INPUT}"!="") AND (A.RELEASECODE LIKE CONCAT("%",@INPUT,"%") OR A.RELEASENAME LIKE CONCAT("%",@INPUT,"%") OR A.TAG LIKE CONCAT("%",@INPUT,"%")) #end', 'A.CREATETIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vr_f01');

-- ============================================================
-- 3. RS_M22 扩展接口
-- ============================================================
SET @m22 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M22');

-- A08 batchMark
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME)
SELECT 'api_m22_a08', @m22, 'A08', 'batchMark', '批量打标', 'batchMark', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=@m22 AND APICODE='A08');

-- A09 createRelease
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME)
SELECT 'api_m22_a09', @m22, 'A09', 'createRelease', '创建发布包', 'createRelease', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=@m22 AND APICODE='A09');

-- A10 deployRelease
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME)
SELECT 'api_m22_a10', @m22, 'A10', 'deployRelease', '部署发布包', 'deployRelease', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=@m22 AND APICODE='A10');

-- A11 listReleases
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME)
SELECT 'api_m22_a11', @m22, 'A11', 'listReleases', '查询发布包列表', 'listReleases', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=@m22 AND APICODE='A11');

-- ============================================================
-- 4. tss_dev_version 加索引
-- ============================================================
ALTER TABLE tss_dev_version ADD INDEX idx_tag (TAG);

-- ============================================================
-- 5. D0701 字典补 release 类型项
-- ============================================================
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM)
SELECT 'di_d0701_release', 'dict_d0701', '发布包', 'release', 9
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM tss_dictitem WHERE DICTID='dict_d0701' AND ITEMVALUE='release'
);

-- ============================================================
-- 6. 版本纳管配置
-- ============================================================
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_release', 'VSS_RELEASE', 'release', 'RELEASECODE', 'RELEASENAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_RELEASE');
