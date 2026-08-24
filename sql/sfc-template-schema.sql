-- ============================================================
-- SFC 在线开发平台 — 数据库元数据初始化脚本
-- 模块: RS_M17  菜单: 系统管理 > SFC在线开发
-- ============================================================

-- ============================================================
-- 1. 物理表
-- ============================================================
CREATE TABLE IF NOT EXISTS tbs_sfc_template (
  ID            VARCHAR(36)  NOT NULL COMMENT '主键',
  TEMPLATECODE  VARCHAR(100) NOT NULL COMMENT '模板编码(唯一)',
  TEMPLATENAME  VARCHAR(200) NOT NULL COMMENT '模板名称',
  MODULEPATH    VARCHAR(500) NOT NULL COMMENT '模块路径 如 @/pages/r02/m07/views/main.vue',
  FILETYPE      VARCHAR(10)  NOT NULL COMMENT 'VUE / JS',
  SOURCECODE    LONGTEXT     COMMENT '原始源码(编辑器内容)',
  COMPILEDCODE  LONGTEXT     COMMENT '编译后代码(Babel转CJS+render函数)',
  DEPS          TEXT         COMMENT '依赖路径JSON数组',
  DESCRIPTION   VARCHAR(500) COMMENT '描述',
  ISDELETED     TINYINT      NOT NULL DEFAULT 0 COMMENT '逻辑删除 0未删除 1已删除',
  CREATEDBY     VARCHAR(36)  COMMENT '创建人ID',
  CREATEDTIME   DATETIME     COMMENT '创建时间',
  UPDATEDBY     VARCHAR(36)  COMMENT '更新人ID',
  UPDATEDTIME   DATETIME     COMMENT '更新时间',
  PRIMARY KEY (ID),
  UNIQUE KEY uk_sfc_code (TEMPLATECODE),
  KEY idx_sfc_path (MODULEPATH(100))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='SFC在线模板';

-- ============================================================
-- 2. tss_resource — TBS 物理表资源 + VCK 视图资源
-- ============================================================
INSERT INTO tss_resource (ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID)
SELECT 'tbs_sfc_tpl_001', 'TBS_SFC_TEMPLATE', 'A', 'tbs_sfc_template', 'TABLE', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID = 'tbs_sfc_tpl_001');

INSERT INTO tss_resource (ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID)
SELECT 'vck_sfc_tpl_001', 'VCK_SFC_TEMPLATE', 'A', 'tbs_sfc_template', 'DATAVIEW', 'tbs_sfc_tpl_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID = 'vck_sfc_tpl_001');

-- ============================================================
-- 3. tss_resfield — TBS 物理表字段
-- ============================================================
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_id', 'tbs_sfc_tpl_001', NULL, 'ID', '主键', 'varchar', NULL, 0, 36, '主键', 1, 'GUID', NULL, 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_id');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_code', 'tbs_sfc_tpl_001', NULL, 'TEMPLATECODE', '模板编码', 'varchar', NULL, 0, 100, '模板编码', 0, NULL, NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_code');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_name', 'tbs_sfc_tpl_001', NULL, 'TEMPLATENAME', '模板名称', 'varchar', NULL, 0, 200, '模板名称', 0, NULL, NULL, 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_name');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_path', 'tbs_sfc_tpl_001', NULL, 'MODULEPATH', '模块路径', 'varchar', NULL, 0, 500, '模块路径', 0, NULL, NULL, 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_path');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_type', 'tbs_sfc_tpl_001', NULL, 'FILETYPE', '文件类型', 'varchar', NULL, 0, 10, '文件类型', 0, NULL, NULL, 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_type');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_src', 'tbs_sfc_tpl_001', NULL, 'SOURCECODE', '原始源码', 'varchar', NULL, 1, 0, '原始源码', 0, NULL, NULL, 6
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_src');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_comp', 'tbs_sfc_tpl_001', NULL, 'COMPILEDCODE', '编译后代码', 'varchar', NULL, 1, 0, '编译后代码', 0, NULL, NULL, 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_comp');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_deps', 'tbs_sfc_tpl_001', NULL, 'DEPS', '依赖列表', 'varchar', NULL, 1, 1000, '依赖列表JSON', 0, NULL, NULL, 8
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_deps');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_desc', 'tbs_sfc_tpl_001', NULL, 'DESCRIPTION', '描述', 'varchar', NULL, 1, 500, '描述', 0, NULL, NULL, 9
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_desc');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_isdel', 'tbs_sfc_tpl_001', NULL, 'ISDELETED', '是否删除', 'int', NULL, 0, 1, '是否删除', 0, NULL, '0', 10
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_isdel');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_cby', 'tbs_sfc_tpl_001', NULL, 'CREATEDBY', '创建人', 'varchar', NULL, 1, 36, '创建人ID', 0, NULL, NULL, 11
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_cby');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_ctime', 'tbs_sfc_tpl_001', NULL, 'CREATEDTIME', '创建时间', 'datetime', NULL, 1, 0, '创建时间', 0, NULL, NULL, 12
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_ctime');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_uby', 'tbs_sfc_tpl_001', NULL, 'UPDATEDBY', '更新人', 'varchar', NULL, 1, 36, '更新人ID', 0, NULL, NULL, 13
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_uby');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_sfct_utime', 'tbs_sfc_tpl_001', NULL, 'UPDATEDTIME', '更新时间', 'datetime', NULL, 1, 0, '更新时间', 0, NULL, NULL, 14
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_sfct_utime');

-- ============================================================
-- 4. tss_resfield — VCK 视图字段 (REFFIELDID 指向 TBS 字段)
-- ============================================================
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_id', 'vck_sfc_tpl_001', 'rf_sfct_id', 'ID', '主键', 'varchar', NULL, 0, 36, '主键', 1, 'GUID', NULL, 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_id');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_code', 'vck_sfc_tpl_001', 'rf_sfct_code', 'TEMPLATECODE', '模板编码', 'varchar', NULL, 0, 100, '模板编码', 0, NULL, NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_code');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_name', 'vck_sfc_tpl_001', 'rf_sfct_name', 'TEMPLATENAME', '模板名称', 'varchar', NULL, 0, 200, '模板名称', 0, NULL, NULL, 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_name');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_path', 'vck_sfc_tpl_001', 'rf_sfct_path', 'MODULEPATH', '模块路径', 'varchar', NULL, 0, 500, '模块路径', 0, NULL, NULL, 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_path');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_type', 'vck_sfc_tpl_001', 'rf_sfct_type', 'FILETYPE', '文件类型', 'varchar', NULL, 0, 10, '文件类型', 0, NULL, NULL, 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_type');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_src', 'vck_sfc_tpl_001', 'rf_sfct_src', 'SOURCECODE', '原始源码', 'varchar', NULL, 1, 0, '原始源码', 0, NULL, NULL, 6
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_src');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_comp', 'vck_sfc_tpl_001', 'rf_sfct_comp', 'COMPILEDCODE', '编译后代码', 'varchar', NULL, 1, 0, '编译后代码', 0, NULL, NULL, 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_comp');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_deps', 'vck_sfc_tpl_001', 'rf_sfct_deps', 'DEPS', '依赖列表', 'varchar', NULL, 1, 1000, '依赖列表JSON', 0, NULL, NULL, 8
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_deps');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_desc', 'vck_sfc_tpl_001', 'rf_sfct_desc', 'DESCRIPTION', '描述', 'varchar', NULL, 1, 500, '描述', 0, NULL, NULL, 9
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_desc');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_isdel', 'vck_sfc_tpl_001', 'rf_sfct_isdel', 'ISDELETED', '是否删除', 'int', NULL, 0, 1, '是否删除', 0, NULL, '0', 10
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_isdel');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_cby', 'vck_sfc_tpl_001', 'rf_sfct_cby', 'CREATEDBY', '创建人', 'varchar', NULL, 1, 36, '创建人ID', 0, NULL, NULL, 11
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_cby');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_ctime', 'vck_sfc_tpl_001', 'rf_sfct_ctime', 'CREATEDTIME', '创建时间', 'datetime', NULL, 1, 0, '创建时间', 0, NULL, NULL, 12
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_ctime');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_uby', 'vck_sfc_tpl_001', 'rf_sfct_uby', 'UPDATEDBY', '更新人', 'varchar', NULL, 1, 36, '更新人ID', 0, NULL, NULL, 13
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_uby');

INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, PREC, NULLABLE, FIELDLENGTH, COMMENTS, ISKEY, KEYGENTYPE, DEFAULTVALUE, ENTRYNUM)
SELECT 'rf_vsfc_utime', 'vck_sfc_tpl_001', 'rf_sfct_utime', 'UPDATEDTIME', '更新时间', 'datetime', NULL, 1, 0, '更新时间', 0, NULL, NULL, 14
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID = 'rf_vsfc_utime');

-- ============================================================
-- 5. tss_resfilter — F00 单条 / F01 列表搜索 / F03 按路径加载
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_sfc_f00', 'vck_sfc_tpl_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_sfc_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_sfc_f01', 'vck_sfc_tpl_001', 'F01', CONCAT('1=1', CHAR(10),
'#if("$!{INPUT}"!="")', CHAR(10),
'AND (A.TEMPLATECODE LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'OR A.TEMPLATENAME LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'OR A.MODULEPATH LIKE CONCAT(''%'',@INPUT,''%''))', CHAR(10),
'#end', CHAR(10),
'AND A.ISDELETED = 0'), 'UPDATEDTIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_sfc_f01');

-- F03: 运行时按 MODULEPATH 精确加载（供 SFC 加载器调用）
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_sfc_f03', 'vck_sfc_tpl_001', 'F03', CONCAT('1=1', CHAR(10),
'AND A.MODULEPATH = @MODULEPATH', CHAR(10),
'AND A.ISDELETED = 0'), NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_sfc_f03');

-- ============================================================
-- 6. tss_resuipc — UI 配置
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_idx', 'vck_sfc_tpl_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_idx');

-- 模板编码
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_code', 'vck_sfc_tpl_001', 'rf_vsfc_code', '模板编码', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_code');

-- 模板名称
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_name', 'vck_sfc_tpl_001', 'rf_vsfc_name', '模板名称', 2, 2, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_name');

-- 模块路径
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_path', 'vck_sfc_tpl_001', 'rf_vsfc_path', '模块路径', 3, 3, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_path');

-- 文件类型
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_type', 'vck_sfc_tpl_001', 'rf_vsfc_type', '文件类型', 4, NULL, 4, 'select', '1:VUE,0:JS'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_type');

-- 原始源码（不在列表显示，编辑页由自定义编辑器处理）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, SHOWLENGTH)
SELECT 'uipc_sfc_src', 'vck_sfc_tpl_001', 'rf_vsfc_src', '原始源码', NULL, NULL, 5, 'textarea', NULL, '0'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_src');

-- 编译后代码（不在列表/编辑中显示，由系统自动填充）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, SHOWLENGTH)
SELECT 'uipc_sfc_comp', 'vck_sfc_tpl_001', 'rf_vsfc_comp', '编译后代码', NULL, NULL, NULL, 'textarea', NULL, '0'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_comp');

-- 依赖列表（不在列表显示，由系统自动填充）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, SHOWLENGTH)
SELECT 'uipc_sfc_deps', 'vck_sfc_tpl_001', 'rf_vsfc_deps', '依赖列表', NULL, NULL, NULL, 'textarea', NULL, '0'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_deps');

-- 描述
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sfc_desc', 'vck_sfc_tpl_001', 'rf_vsfc_desc', '描述', 5, NULL, 6, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_desc');

-- 创建时间（列表显示，表单只读）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_sfc_ctime', 'vck_sfc_tpl_001', 'rf_vsfc_ctime', '创建时间', 6, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_ctime');

-- 更新时间（列表显示，表单只读）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_sfc_utime', 'vck_sfc_tpl_001', 'rf_vsfc_utime', '更新时间', 7, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_sfc_utime');

-- ============================================================
-- 7. tss_moudle — 模块注册
-- ============================================================
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m17_module_001', 'RS_M17', 'SFC在线开发'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE = 'RS_M17');

-- ============================================================
-- 8. tss_moudlepath — QRY / QQRY / SEL / MAIN
-- ============================================================
SET @module_id = (SELECT ID FROM tss_moudle WHERE MODULECODE = 'RS_M17');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_sfc_qry', @module_id, 'QRY', 'vck_sfc_tpl_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_sfc_qry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_sfc_qqry', @module_id, 'QQRY', 'vck_sfc_tpl_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_sfc_qqry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_sfc_sel', @module_id, 'SEL', 'vck_sfc_tpl_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_sfc_sel');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_sfc_main', @module_id, 'MAIN', 'vck_sfc_tpl_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_sfc_main');

-- ============================================================
-- 9. tss_moudleapi — A01查询 / A02打开 / A04保存 / A05删除 / A06按路径加载
-- ============================================================
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_sfc_a01', @module_id, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_sfc_a01');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_sfc_a02', @module_id, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_sfc_a02');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_sfc_a04', @module_id, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_sfc_a04');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_sfc_a05', @module_id, 'A05', 'delete', 'delete', '', NULL, '删除', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_sfc_a05');

-- A06: 运行时按 MODULEPATH 加载编译后代码（供 SFC 加载器调用）
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_sfc_a06', @module_id, 'A06', 'queryByPath', 'query', 'QRY', 'F03', '按路径加载', 'QQRY'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_sfc_a06');

-- ============================================================
-- 10. tss_func — 菜单（系统管理 > SFC在线开发）
-- ============================================================
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, FUNCICON, ISOUTERURL, SORTCODE)
SELECT UUID(), 'RS_M17', 'SFC在线开发', 's01/m17', '3e3c83ce2b3c475b82902478c89c27c0', NULL, NULL, 170
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE = 'RS_M17');

-- ============================================================
-- 11. tss_funcpoint — 功能点权限
-- ============================================================
SET @func_id = (SELECT ID FROM tss_func WHERE FUNCCODE = 'RS_M17' LIMIT 1);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A01', '查询' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A01' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A03', '新增编辑' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A03' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A04', '删除' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A04' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A06', '运行时加载' FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A06' AND FUNCID = @func_id);
