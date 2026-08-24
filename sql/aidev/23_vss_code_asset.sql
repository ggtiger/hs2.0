-- ============================================================
-- VSS_CODE_ASSET 统一视图 + 历史视图还原 — 数据库升级
-- 内容: ① 新建 VSS_CODE_ASSET 统一视图(统一字段名 CODE/NAME/SOURCECODE)
--       ② 还原 VSS_API_SCRIPT/VSS_SQL/VCK_SFC_TEMPLATE 到历史表(撤销重接)
--       ③ RS_M21/RS_M13/RS_M17 模块路径改指 VSS_CODE_ASSET + 类型过滤器
--       ④ 版本纳管切换到 VSS_CODE_ASSET
-- 原则: 历史视图与老表(tss_api_script/tss_sql/tbs_sfc_template)原样保留,
--       后续所有新使用一律走 VSS_CODE_ASSET
-- 日期: 2026-07-17
-- ============================================================

-- -----------------------------------------------------------
-- 1. 新建 VSS_CODE_ASSET 统一视图
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_code_asset_001', 'VSS_CODE_ASSET', 'tss_code_asset', 'DATAVIEW', 'tbs_code_asset_001', 'A', '统一代码资产视图(C#/SQL/JS/VUE)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_code_asset_001');

-- 字段注册(统一字段名, REFFIELDID→tbs_code_asset_001 同名TBS字段)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_id', 'vss_code_asset_001', 'rf_ca_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_type', 'vss_code_asset_001', 'rf_ca_type', 'ASSETTYPE', 'varchar', 0, 0, 16, '资产类型(csharp/sql/js/vue)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_type');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_code', 'vss_code_asset_001', 'rf_ca_code', 'CODE', 'varchar', 0, 0, 200, '编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_name', 'vss_code_asset_001', 'rf_ca_name', 'NAME', 'varchar', 0, 1, 200, '名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_path', 'vss_code_asset_001', 'rf_ca_path', 'MODULEPATH', 'varchar', 0, 1, 200, 'SFC路径'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_path');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_ftype', 'vss_code_asset_001', 'rf_ca_ftype', 'FILETYPE', 'varchar', 0, 1, 16, 'JS/VUE'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_ftype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_source', 'vss_code_asset_001', 'rf_ca_source', 'SOURCECODE', 'text', 0, 1, '源码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_source');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_compiled', 'vss_code_asset_001', 'rf_ca_compiled', 'COMPILEDCODE', 'text', 0, 1, '编译产物'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_compiled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_deps', 'vss_code_asset_001', 'rf_ca_deps', 'DEPS', 'varchar', 0, 1, 2000, '依赖'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_deps');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_sqltype', 'vss_code_asset_001', 'rf_ca_sqltype', 'SQLTYPE', 'varchar', 0, 1, 16, 'SQL类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_sqltype');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_version', 'vss_code_asset_001', 'rf_ca_version', 'VERSION', 'int', 0, 1, '版本号'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_version');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_remark', 'vss_code_asset_001', 'rf_ca_remark', 'REMARK', 'varchar', 0, 1, 500, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_createid', 'vss_code_asset_001', 'rf_ca_createid', 'CREATEID', 'varchar', 0, 1, 64, '创建人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_createid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_creater', 'vss_code_asset_001', 'rf_ca_creater', 'CREATER', 'varchar', 0, 1, 64, '创建人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_creater');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_modifyid', 'vss_code_asset_001', 'rf_ca_modifyid', 'MODIFYID', 'varchar', 0, 1, 64, '修改人ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_modifyid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vca_modifer', 'vss_code_asset_001', 'rf_ca_modifer', 'MODIFER', 'varchar', 0, 1, 64, '修改人姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_modifer');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_isdeleted', 'vss_code_asset_001', 'rf_ca_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_isdeleted');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_ctime', 'vss_code_asset_001', 'rf_ca_ctime', 'CREATETIME', 'datetime', 0, 1, '创建时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_ctime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vca_mtime', 'vss_code_asset_001', 'rf_ca_utime', 'MODIFYTIME', 'datetime', 0, 1, '修改时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vca_mtime');

-- -----------------------------------------------------------
-- 2. VSS_CODE_ASSET 过滤器
--    F00 按ID / F01 通用列表(参数化类型) / F03 按路径(SFC加载)
--    FC1 csharp列表(RS_M21 A01) / FS1 sql列表(RS_M13 A01) / FJ1 js+vue列表(RS_M17 A01)
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_vca_f00', 'vss_code_asset_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_vca_f01', 'vss_code_asset_001', 'F01', '1=1 AND A.ISDELETED=0
#if("$!{ASSETTYPE}"!="")
AND A.ASSETTYPE=@ASSETTYPE
#end
#if("$!{INPUT}"!="")
AND (A.CODE LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.NAME LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.MODULEPATH LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.REMARK LIKE CONCAT(CHAR(37),@INPUT,CHAR(37)))
#end
#if("$!{CODE}"!="")
AND A.CODE LIKE CONCAT(CHAR(37),@CODE,CHAR(37))
#end
#if("$!{NAME}"!="")
AND A.NAME LIKE CONCAT(CHAR(37),@NAME,CHAR(37))
#end
#if("$!{CREATER}"!="")
AND A.CREATER LIKE CONCAT(CHAR(37),@CREATER,CHAR(37))
#end', 'MODIFYTIME DESC, ID DESC', '通用列表(INPUT模糊+参数化类型/编码/名称/修改人)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_f01');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_vca_f03', 'vss_code_asset_001', 'F03', 'A.MODULEPATH=@MODULEPATH', '按SFC路径查询(sfc-loader)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_f03');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_vca_fc1', 'vss_code_asset_001', 'FC1', '1=1 AND A.ASSETTYPE=CHAR(99,115,104,97,114,112) AND A.ISDELETED=0', 'CODE', 'csharp 列表(RS_M21)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_fc1');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_vca_fs1', 'vss_code_asset_001', 'FS1', '1=1 AND A.ASSETTYPE=CHAR(115,113,108)
#if("$!{INPUT}"!="")
AND (A.CODE LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.NAME LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.REMARK LIKE CONCAT(CHAR(37),@INPUT,CHAR(37)))
#end', 'CODE', 'sql 列表(RS_M13)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_fs1');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_vca_fj1', 'vss_code_asset_001', 'FJ1', '1=1 AND A.ASSETTYPE IN (CHAR(106,115),CHAR(118,117,101)) AND A.ISDELETED=0', 'MODULEPATH', 'js+vue 列表(RS_M17)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_vca_fj1');

-- 已部署环境纠正(幂等): ORDERBY 不允许表别名前缀
-- (ORM 有排序时包装 SELECT * FROM (...) T ORDER BY, 内层列带 AS 别名后 A.列名 无法解析)
-- F01 同时补 INPUT 模糊搜索条件(CODE/NAME/MODULEPATH/REMARK)
UPDATE tss_resfilter SET FILTERSQL='1=1 AND A.ISDELETED=0
#if("$!{ASSETTYPE}"!="")
AND A.ASSETTYPE=@ASSETTYPE
#end
#if("$!{INPUT}"!="")
AND (A.CODE LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.NAME LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.MODULEPATH LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.REMARK LIKE CONCAT(CHAR(37),@INPUT,CHAR(37)))
#end
#if("$!{CODE}"!="")
AND A.CODE LIKE CONCAT(CHAR(37),@CODE,CHAR(37))
#end
#if("$!{NAME}"!="")
AND A.NAME LIKE CONCAT(CHAR(37),@NAME,CHAR(37))
#end
#if("$!{CREATER}"!="")
AND A.CREATER LIKE CONCAT(CHAR(37),@CREATER,CHAR(37))
#end', ORDERBY='MODIFYTIME DESC, ID DESC' WHERE ID='rf_vca_f01';
UPDATE tss_resfilter SET ORDERBY='CODE' WHERE ID='rf_vca_fc1';
UPDATE tss_resfilter SET ORDERBY='CODE' WHERE ID='rf_vca_fs1';
UPDATE tss_resfilter SET ORDERBY='MODULEPATH' WHERE ID='rf_vca_fj1';

-- -----------------------------------------------------------
-- 3. 还原历史视图(撤销重接, 指向原历史表)
-- -----------------------------------------------------------
-- 3.1 VSS_API_SCRIPT → tss_api_script
UPDATE tss_resource SET TABLERESOURCEID='tbs_api_script_001', TABLENAME='tss_api_script' WHERE ID='vss_api_script_001';
UPDATE tss_resfield v JOIN tss_resfield t ON t.RESOURCEID='tbs_api_script_001' AND t.FIELDNAME=v.FIELDNAME
SET v.REFFIELDID=t.ID
WHERE v.RESOURCEID='vss_api_script_001' AND v.REFFIELDID IS NOT NULL AND v.ID NOT LIKE 'rf_vas_%type';
-- 删除本次新增的非历史字段(ASSETTYPE/人员/时间)
DELETE FROM tss_resfield WHERE RESOURCEID='vss_api_script_001'
  AND ID IN ('rf_vas_type','rf_vas_createid','rf_vas_creater','rf_vas_modifyid','rf_vas_modifer','rf_vas_mtime','rf_vas_ctime');
-- F01 还原
UPDATE tss_resfilter SET FILTERSQL='1=1 AND A.ISDELETED=0' WHERE ID='rf_as_f01';

-- 3.2 VSS_SQL → tss_sql (RESOURCEID=13aacfef78e64ffc9a571330ba774168, 原 TABLERESOURCEID=1)
UPDATE tss_resource SET TABLERESOURCEID='1', TABLENAME='tss_sql' WHERE ID='13aacfef78e64ffc9a571330ba774168';
UPDATE tss_resfield v JOIN tss_resfield t ON t.RESOURCEID='1' AND t.FIELDNAME=v.FIELDNAME
SET v.REFFIELDID=t.ID
WHERE v.RESOURCEID='13aacfef78e64ffc9a571330ba774168' AND v.REFFIELDID IS NOT NULL;
DELETE FROM tss_resfield WHERE RESOURCEID='13aacfef78e64ffc9a571330ba774168'
  AND ID IN ('rf_vsql_type','rf_vsql_createid','rf_vsql_creater','rf_vsql_modifyid','rf_vsql_modifer','rf_vsql_mtime','rf_vsql_ctime');
-- F01/F04/F05 还原原文
UPDATE tss_resfilter SET FILTERSQL='A.SQLCODE=@SQLCODE AND A.SQLTYPE=@SQLTYPE' WHERE RESOURCEID='13aacfef78e64ffc9a571330ba774168' AND FILTERCODE='F01';
UPDATE tss_resfilter SET FILTERSQL='1=1
#if("$!{INPUT}"!="")
AND (A.SQLCODE LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.SQLTYPE LIKE CONCAT(CHAR(37),@INPUT,CHAR(37))
OR A.REMARK LIKE CONCAT(CHAR(37),@INPUT,CHAR(37)))
#end' WHERE RESOURCEID='13aacfef78e64ffc9a571330ba774168' AND FILTERCODE='F04';
UPDATE tss_resfilter SET FILTERSQL='1=1
#if("$!{SQLCODE}"!="")
AND A.SQLCODE LIKE CONCAT(CHAR(37),@SQLCODE,CHAR(37))
#end
#if("$!{SQLTYPE}"!="")
AND A.SQLTYPE = @SQLTYPE
#end
#if("$!{REMARK}"!="")
AND A.REMARK LIKE CONCAT(CHAR(37),@REMARK,CHAR(37))
#end' WHERE RESOURCEID='13aacfef78e64ffc9a571330ba774168' AND FILTERCODE='F05';

-- 3.3 VCK_SFC_TEMPLATE → tbs_sfc_template
UPDATE tss_resource SET TABLERESOURCEID='tbs_sfc_tpl_001', TABLENAME='tbs_sfc_template' WHERE ID='vck_sfc_tpl_001';
UPDATE tss_resfield v JOIN tss_resfield t ON t.RESOURCEID='tbs_sfc_tpl_001' AND t.FIELDNAME=v.FIELDNAME
SET v.REFFIELDID=t.ID
WHERE v.RESOURCEID='vck_sfc_tpl_001' AND v.REFFIELDID IS NOT NULL;
DELETE FROM tss_resfield WHERE RESOURCEID='vck_sfc_tpl_001'
  AND ID IN ('rf_vsfc_type','rf_vsfc_createid','rf_vsfc_creater','rf_vsfc_modifyid','rf_vsfc_modifer');
-- F01 还原
UPDATE tss_resfilter SET FILTERSQL='1=1 AND A.ISDELETED=0' WHERE RESOURCEID='vck_sfc_tpl_001' AND FILTERCODE='F01';

-- -----------------------------------------------------------
-- 4. 三个模块的路径改指 VSS_CODE_ASSET + A01 用类型过滤器
-- -----------------------------------------------------------
UPDATE tss_moudlepath SET RESOURCEID='vss_code_asset_001'
WHERE MODULEID IN (SELECT ID FROM tss_moudle WHERE MODULECODE IN ('RS_M21','RS_M13','RS_M17'));
-- A01 类型过滤器
UPDATE tss_moudleapi SET FILTERCODE='FC1' WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M21') AND APICODE='A01';
UPDATE tss_moudleapi SET FILTERCODE='FS1' WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M13') AND APICODE IN ('A01','A03');
UPDATE tss_moudleapi SET FILTERCODE='FJ1' WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M17') AND APICODE='A01';

-- -----------------------------------------------------------
-- 5. 版本纳管切换到 VSS_CODE_ASSET(历史视图的纳管停用)
-- -----------------------------------------------------------
UPDATE tss_dev_version_cfg SET ENABLED=0 WHERE ID IN ('dvc_apiscript','dvc_sql','dvc_sfc','dvc_sfc2');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_codeasset', 'VSS_CODE_ASSET', 'code', 'CODE', 'NAME', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_CODE_ASSET');
