-- ============================================================
-- 演示: AI 生成在线开发模块 RS_FEE(项目费用管理)
-- 严格遵循记忆库铁律:
--   - 字段全大写无下划线
--   - 新表 collation utf8mb4_general_ci
--   - tss_ 系统表无 ISDELETED(只有 tbs_module_page/button 例外, 自带 ISDELETED)
--   - DATAVIEW 字段 REFFIELDID 指向 TBS 字段
--   - F00=A.ID=@ID
--   - ORDERBY 无表别名
--   - 下拉走字典 DICTNAME
--   - F02 用 @ui:adv 自动
-- 幂等: 清理旧半成品 + NOT EXISTS 防重复
-- ============================================================

-- -----------------------------------------------------------
-- 0. 清理旧半成品(只清本次范围)
-- -----------------------------------------------------------
DELETE FROM tss_resfield    WHERE RESOURCEID IN ('tbs_project_fee_001','vck_project_fee_001');
DELETE FROM tss_resfilter   WHERE RESOURCEID = 'vck_project_fee_001';
DELETE FROM tss_resuipc     WHERE RESOURCEID = 'vck_project_fee_001';
DELETE FROM tss_resource    WHERE ID IN ('tbs_project_fee_001','vck_project_fee_001');
DROP TABLE IF EXISTS tbs_project_fee;
DELETE FROM tss_module_page   WHERE MODULECODE = 'RS_FEE';
DELETE FROM tss_module_button WHERE MODULECODE = 'RS_FEE';
DELETE FROM tss_funcpoint     WHERE FUNCPOINTCODE LIKE 'RS_FEE/%';
DELETE FROM tss_func          WHERE FUNCCODE = 'LI_M_FEE';
DELETE FROM tss_dictitem      WHERE DICTID = 'd_dict_d0710';
DELETE FROM tss_dict          WHERE DICTCODE = 'D0710';

-- -----------------------------------------------------------
-- 1. 物理表(collation utf8mb4_general_ci, 字段全大写无下划线)
-- -----------------------------------------------------------
CREATE TABLE tbs_project_fee (
  ID varchar(32) NOT NULL,
  FEECODE varchar(64) DEFAULT NULL COMMENT '费用编码',
  FEENAME varchar(200) DEFAULT NULL COMMENT '费用名称',
  FEETYPE varchar(16) DEFAULT NULL COMMENT '费用类型',
  AMOUNT decimal(18,2) DEFAULT NULL COMMENT '金额',
  FEEDATE datetime DEFAULT NULL COMMENT '费用日期',
  REMARK varchar(500) DEFAULT NULL COMMENT '备注',
  CREATEID varchar(32) DEFAULT NULL COMMENT '创建人ID',
  CREATER varchar(64) DEFAULT NULL COMMENT '创建人',
  CREATETIME datetime DEFAULT NULL COMMENT '创建时间',
  MODIFYID varchar(32) DEFAULT NULL COMMENT '修改人ID',
  MODIFER varchar(64) DEFAULT NULL COMMENT '修改人',
  MODIFYTIME datetime DEFAULT NULL COMMENT '修改时间',
  ISDELETED tinyint DEFAULT 0 COMMENT '逻辑删除',
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='项目费用';

-- -----------------------------------------------------------
-- 2. 资源(TBS + VCK, RESOURCEANAME=A)
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
VALUES ('tbs_project_fee_001', 'TBS_PROJECT_FEE', 'tbs_project_fee', 'TABLE', NULL, 'A', '项目费用物理表')
ON DUPLICATE KEY UPDATE COMMENTS=VALUES(COMMENTS);

INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
VALUES ('vck_project_fee_001', 'VCK_PROJECT_FEE', 'vck_project_fee', 'DATAVIEW', 'tbs_project_fee_001', 'A', '项目费用业务视图')
ON DUPLICATE KEY UPDATE COMMENTS=VALUES(COMMENTS);

-- -----------------------------------------------------------
-- 3. 物理表字段(TBS, REFFIELDID=NULL, ISKEY=1 主键 KEYGENTYPE=GUID)
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, COMMENTS) VALUES
('rf_tpf_id',      'tbs_project_fee_001', 'ID',          'varchar',  32,  0, 1, 'GUID',  1,  '主键'),
('rf_tpf_feecode', 'tbs_project_fee_001', 'FEECODE',     'varchar',  64,  1, 0, NULL,    2,  '费用编码'),
('rf_tpf_feename', 'tbs_project_fee_001', 'FEENAME',     'varchar',  200, 1, 0, NULL,    3,  '费用名称'),
('rf_tpf_feetype', 'tbs_project_fee_001', 'FEETYPE',     'varchar',  16,  1, 0, NULL,    4,  '费用类型'),
('rf_tpf_amount',  'tbs_project_fee_001', 'AMOUNT',      'decimal',  18,  1, 0, NULL,    5,  '金额'),
('rf_tpf_feedate', 'tbs_project_fee_001', 'FEEDATE',     'datetime', 0,   1, 0, NULL,    6,  '费用日期'),
('rf_tpf_remark',  'tbs_project_fee_001', 'REMARK',      'varchar',  500, 1, 0, NULL,    7,  '备注'),
('rf_tpf_createid',   'tbs_project_fee_001', 'CREATEID',   'varchar',  32,  1, 0, NULL, 91, '创建人ID'),
('rf_tpf_creater',    'tbs_project_fee_001', 'CREATER',    'varchar',  64,  1, 0, NULL, 92, '创建人'),
('rf_tpf_createtime', 'tbs_project_fee_001', 'CREATETIME', 'datetime', 0,   1, 0, NULL, 93, '创建时间'),
('rf_tpf_modifyid',   'tbs_project_fee_001', 'MODIFYID',   'varchar',  32,  1, 0, NULL, 94, '修改人ID'),
('rf_tpf_modifer',    'tbs_project_fee_001', 'MODIFER',    'varchar',  64,  1, 0, NULL, 95, '修改人'),
('rf_tpf_modifytime', 'tbs_project_fee_001', 'MODIFYTIME', 'datetime', 0,   1, 0, NULL, 96, '修改时间'),
('rf_tpf_deleted',    'tbs_project_fee_001', 'ISDELETED',  'tinyint',  1,   1, 0, NULL, 97, '逻辑删除');

-- -----------------------------------------------------------
-- 4. 业务视图字段(VCK, REFFIELDID 指向 TBS 字段 ID)
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, COMMENTS) VALUES
('rf_vpf_id',         'vck_project_fee_001', 'rf_tpf_id',          'ID',          'varchar',  32,  0, 1, 'GUID',  1,  '主键'),
('rf_vpf_feecode',    'vck_project_fee_001', 'rf_tpf_feecode',     'FEECODE',     'varchar',  64,  1, 0, NULL,    2,  '费用编码'),
('rf_vpf_feename',    'vck_project_fee_001', 'rf_tpf_feename',     'FEENAME',     'varchar',  200, 1, 0, NULL,    3,  '费用名称'),
('rf_vpf_feetype',    'vck_project_fee_001', 'rf_tpf_feetype',     'FEETYPE',     'varchar',  16,  1, 0, NULL,    4,  '费用类型'),
('rf_vpf_amount',     'vck_project_fee_001', 'rf_tpf_amount',      'AMOUNT',      'decimal',  18,  1, 0, NULL,    5,  '金额'),
('rf_vpf_feedate',    'vck_project_fee_001', 'rf_tpf_feedate',     'FEEDATE',     'datetime', 0,   1, 0, NULL,    6,  '费用日期'),
('rf_vpf_remark',     'vck_project_fee_001', 'rf_tpf_remark',      'REMARK',      'varchar',  500, 1, 0, NULL,    7,  '备注'),
('rf_vpf_createid',   'vck_project_fee_001', 'rf_tpf_createid',    'CREATEID',    'varchar',  32,  1, 0, NULL, 91, '创建人ID'),
('rf_vpf_creater',    'vck_project_fee_001', 'rf_tpf_creater',     'CREATER',     'varchar',  64,  1, 0, NULL, 92, '创建人'),
('rf_vpf_createtime', 'vck_project_fee_001', 'rf_tpf_createtime',  'CREATETIME',  'datetime', 0,   1, 0, NULL, 93, '创建时间'),
('rf_vpf_modifyid',   'vck_project_fee_001', 'rf_tpf_modifyid',    'MODIFYID',    'varchar',  32,  1, 0, NULL, 94, '修改人ID'),
('rf_vpf_modifer',    'vck_project_fee_001', 'rf_tpf_modifer',     'MODIFER',     'varchar',  64,  1, 0, NULL, 95, '修改人'),
('rf_vpf_modifytime', 'vck_project_fee_001', 'rf_tpf_modifytime',  'MODIFYTIME',  'datetime', 0,   1, 0, NULL, 96, '修改时间'),
('rf_vpf_deleted',    'vck_project_fee_001', 'rf_tpf_deleted',     'ISDELETED',   'tinyint',  1,   1, 0, NULL, 97, '逻辑删除');

-- -----------------------------------------------------------
-- 5. 过滤器(F00 A.ID=@ID / F01 INPUT 模糊 / F02 @ui:adv 自动)
-- ORDERBY 无表别名前缀
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK) VALUES
('flt_vpf_f00', 'vck_project_fee_001', 'F00', 'A.ID = @ID', NULL, '单条查询');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK) VALUES
('flt_vpf_f01', 'vck_project_fee_001', 'F01',
'1=1
#if("$!{INPUT}"!="")
AND (A.FEECODE LIKE CONCAT(''%'',@INPUT,''%'')
  OR A.FEENAME LIKE CONCAT(''%'',@INPUT,''%''))
#end
AND A.ISDELETED = 0',
'CREATETIME DESC', '列表模糊搜索');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK) VALUES
('flt_vpf_f02', 'vck_project_fee_001', 'F02', '@ui:adv', 'CREATETIME DESC', '高级查询(自动)');

-- -----------------------------------------------------------
-- 6. UI 配置(tss_resuipc)
-- 列表(LISTSORT) + 查询(QUERYSORT/QUERYMODE) + 编辑(EDITSORT)
-- FEETYPE 走数据字典 DICTNAME=D0710
-- AMOUNT/FEEDATE 配 QUERYMODE 支持范围查询
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, FIELDNAME, LABELNAME, EDITTYPE, LISTSORT, QUERYSORT, EDITSORT, QUERYTYPE, QUERYMODE, SHOWLENGTH, NULLABLE, DISPLAYINLIST, SELECTDATA, COLSPAN, ENTRYNUM) VALUES
('up_vpf_feecode', 'vck_project_fee_001', 'rf_vpf_feecode', 'FEECODE',  '费用编码', 'text',       1, 1, 1, 'input',    'like',  '120', 1, 1, NULL,                       1, 1),
('up_vpf_feename', 'vck_project_fee_001', 'rf_vpf_feename', 'FEENAME',  '费用名称', 'text',       2, 2, 2, 'input',    'like',  NULL,  1, 1, NULL,                       2, 2),
('up_vpf_feetype', 'vck_project_fee_001', 'rf_vpf_feetype', 'FEETYPE',  '费用类型', 'select',     3, 3, 3, 'select',   'eq',    NULL,  1, 1, 'D0710',                     1, 3),
('up_vpf_amount',  'vck_project_fee_001', 'rf_vpf_amount',  'AMOUNT',   '金额',     'number',     4, 4, 4, 'number',   'range', '120', 1, 1, NULL,                       1, 4),
('up_vpf_feedate', 'vck_project_fee_001', 'rf_vpf_feedate', 'FEEDATE',  '费用日期', 'datepicker', 5, 5, 5, 'daterange','range', '120', 1, 1, NULL,                       1, 5),
('up_vpf_remark',  'vck_project_fee_001', 'rf_vpf_remark',  'REMARK',   '备注',     'textarea',   6, NULL, 6, NULL, NULL,   NULL, 1, 0, NULL,                       2, 6);

-- -----------------------------------------------------------
-- 7. 数据字典 D0710(费用类型)
-- tss_dictitem 表结构: ID/DICTID/ITEMNAME/ITEMVALUE/ENTRYNUM
-- -----------------------------------------------------------
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK) VALUES
('d_dict_d0710', 'D0710', '费用类型', 1, '差旅/材料/服务/其他');

INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM, REMARK) VALUES
('d_item_d0710_1', 'd_dict_d0710', '差旅费', 'travel',  1, NULL),
('d_item_d0710_2', 'd_dict_d0710', '材料费', 'material',2, NULL),
('d_item_d0710_3', 'd_dict_d0710', '服务费', 'service', 3, NULL),
('d_item_d0710_4', 'd_dict_d0710', '其他',   'other',   4, NULL);

-- -----------------------------------------------------------
-- 8. 模块 + 模块页 + 按钮(GenericModule 在线开发模式)
-- tss_module_page 字段: QUERYAPICODE(无下划线)/OPENAPICODE/SAVEAPICODE
-- PAGECONFIG 是 JSON 字符串(EXTENDJS 配扩展 JS)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME, REMARK) VALUES
('rs_fee_module_001', 'RS_FEE', '项目费用管理', 'AI 在线开发演示')
ON DUPLICATE KEY UPDATE MODULENAME=VALUES(MODULENAME), REMARK=VALUES(REMARK);

-- 列表页
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, ROUTEPATH, COMPONENTTYPE,
  QUERYAPICODE, OPENAPICODE, SAVEAPICODE, PAGECONFIG, SORTNO, ISDELETED) VALUES
('mp_fee_list', 'RS_FEE', 'main', '费用列表', 'list', '/g/RS_FEE/main', 'generic-module',
  'A01', 'A02', 'A04',
  '{"dynamicQuery":true,"EXTENDJS":"@/modules/RS_FEE/main.js"}',
  1, 0)
ON DUPLICATE KEY UPDATE PAGECONFIG=VALUES(PAGECONFIG), SORTNO=VALUES(SORTNO);

-- 表单页
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, ROUTEPATH, COMPONENTTYPE,
  OPENAPICODE, SAVEAPICODE, PAGECONFIG, SORTNO, ISDELETED) VALUES
('mp_fee_form', 'RS_FEE', 'add', '费用编辑', 'form', '/g/RS_FEE/add', 'generic-form',
  'A02', 'A04',
  '{"mode":"twocolumn","EXTENDJS":"@/modules/RS_FEE/add.js"}',
  2, 0)
ON DUPLICATE KEY UPDATE PAGECONFIG=VALUES(PAGECONFIG), SORTNO=VALUES(SORTNO);

-- 模块数据源(QRY=列表查询 / MAIN=单条打开)
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM) VALUES
('mp_fee_qry',  'rs_fee_module_001', 'QRY',  'vck_project_fee_001', 1),
('mp_fee_qqry', 'rs_fee_module_001', 'QQRY', 'vck_project_fee_001', 2),
('mp_fee_main', 'rs_fee_module_001', 'MAIN', 'vck_project_fee_001', 3)
ON DUPLICATE KEY UPDATE RESOURCEID=VALUES(RESOURCEID);

-- 标准接口(A01 query/A02 open/A04 save/A07 delete/A09 export)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, PATHNAME, FILTERCODE, REMARK) VALUES
('ma_fee_a01', 'rs_fee_module_001', 'A01', 'query', 'QRY',  'F01', '列表查询'),
('ma_fee_a02', 'rs_fee_module_001', 'A02', 'open',  'MAIN', 'F00', '打开单条'),
('ma_fee_a04', 'rs_fee_module_001', 'A04', 'save',  'MAIN', NULL,  '保存'),
('ma_fee_a07', 'rs_fee_module_001', 'A07', 'delete','MAIN', NULL,  '删除'),
('ma_fee_a09', 'rs_fee_module_001', 'A09', 'query', 'QRY',  'F01', '导出')
ON DUPLICATE KEY UPDATE APITYPE=VALUES(APITYPE);

-- 按钮(BTNAREA=header/footer/row, INTERACTTYPE=direct/confirm)
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, SORTNO, EXTPARAM, ISDELETED) VALUES
('mb_fee_add',     'mp_fee_list', 'RS_FEE', NULL,  '添加', 'custom', 'add',     'header', 'direct',  NULL,              NULL,            'RS_FEE/A04', 'h-icon-plus',  'primary', 1, '{"pageCode":"add"}', 0),
('mb_fee_edit',    'mp_fee_list', 'RS_FEE', NULL,  '编辑', 'custom', 'edit',    'row',    'direct',  NULL,              NULL,            'RS_FEE/A02', 'h-icon-edit',  'primary', 1, '{"pageCode":"add"}', 0),
('mb_fee_delete',  'mp_fee_list', 'RS_FEE', 'A07', '删除', 'api',    'delete',  'row',    'confirm', '确定删除该费用?', NULL,            'RS_FEE/A07', 'h-icon-trash', 'red',     2, NULL,                 0),
('mb_fee_save',    'mp_fee_form', 'RS_FEE', 'A04', '保存', 'api',    'save',    'footer', 'direct',  NULL,              NULL,            'RS_FEE/A04', 'h-icon-save',  'primary', 1, NULL,                 0),
('mb_fee_cancel',  'mp_fee_form', 'RS_FEE', NULL,  '取消', 'custom', 'cancel',  'footer', 'direct',  NULL,              NULL,            NULL,          'h-icon-close', NULL,      2, NULL,                 0);

-- -----------------------------------------------------------
-- 9. 菜单 + 权限点
-- tss_func: FUNCCODE(菜单编码) / OUTERURL(/g/RS_FEE/main) / ISOUTERURL=1
-- tss_funcpoint: APICODE 列存接口编码
-- UPFUNCID=1e38586dacef48559ddd565f9f79de59(LI_M00 父菜单, 业务根)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, UPFUNCID, FUNCTYPE, FUNCCODE, FUNCNAME, FUNCICON, ISOUTERURL, OUTERURL, ISHIDE, ISUSE, SORTCODE, REMARK) VALUES
('fn_fee_001', '1e38586dacef48559ddd565f9f79de59', 1, 'LI_M_FEE', '项目费用管理', 'h-icon-money', 1, '/g/RS_FEE/main', 0, 1, 99, 'AI 在线开发演示')
ON DUPLICATE KEY UPDATE FUNCNAME=VALUES(FUNCNAME), OUTERURL=VALUES(OUTERURL);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE, ENTRYNUM) VALUES
('fp_fee_a01', 'fn_fee_001', 'RS_FEE/A01', '查询', 'A01', 1),
('fp_fee_a02', 'fn_fee_001', 'RS_FEE/A02', '打开', 'A02', 2),
('fp_fee_a04', 'fn_fee_001', 'RS_FEE/A04', '保存', 'A04', 3),
('fp_fee_a07', 'fn_fee_001', 'RS_FEE/A07', '删除', 'A07', 4),
('fp_fee_a09', 'fn_fee_001', 'RS_FEE/A09', '导出', 'A09', 5)
ON DUPLICATE KEY UPDATE FUNCPOINTNAME=VALUES(FUNCPOINTNAME);

-- ============================================================
-- 完成 - 演示 SQL 全部入库
-- ============================================================
