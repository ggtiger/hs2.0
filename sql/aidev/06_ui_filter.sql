-- ============================================================
-- @ui 过滤器自动生成功能 - 数据库升级脚本
-- 1. tss_resuipc 新增 QUERYMODE 字段 (like/eq/in/range)
-- 2. ORM 元数据注册 QUERYMODE 字段 (TBS_RESUIPC + VSS_RESUIPC)
-- 3. tss_resfilter.FILTERSQL 支持 @ui / @ui:adv 特殊值
-- ============================================================

-- 1. tss_resuipc 表新增 QUERYMODE 列
-- QUERYMODE: 查询匹配方式，覆盖 EDITTYPE 默认推导
--   like  → 模糊搜索 (A.FIELD LIKE CONCAT('%',@FIELD,'%'))
--   eq    → 精确匹配 (A.FIELD = @FIELD)
--   in    → 多值匹配 (A.FIELD IN (@FIELD))
--   range → 范围查询 (A.FIELD >= @FIELD_start AND A.FIELD <= @FIELD_end)
--   NULL  → 按 EDITTYPE/QUERYTYPE 自动推导（默认行为）
ALTER TABLE tss_resuipc ADD COLUMN QUERYMODE varchar(20) DEFAULT NULL
  COMMENT '查询匹配方式: like/eq/in/range, NULL=按EDITTYPE推导';

-- 2. TBS_RESUIPC (物理表资源) 注册 QUERYMODE 字段
-- TBS_RESUIPC RESOURCEID = ec18e3a20a314b8fb912a2991b6b5205
INSERT INTO tss_resfield (ID, RESOURCEID, FIELDNAME, FIELDTYPE, ISKEY, ENTRYNUM)
SELECT 'rf_tbs_resuipc_querymode', 'ec18e3a20a314b8fb912a2991b6b5205', 'QUERYMODE', 'varchar', 0, 19
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM tss_resfield WHERE ID = 'rf_tbs_resuipc_querymode'
);

-- 3. VSS_RESUIPC (DATAVIEW 资源) 注册 QUERYMODE 字段
-- VSS_RESUIPC RESOURCEID = b45edaad63494430be9e7731b7aed951
-- REFFIELDID 指向 TBS 字段 ID
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, ENTRYNUM)
SELECT 'rf_vss_resuipc_querymode', 'b45edaad63494430be9e7731b7aed951', 'rf_tbs_resuipc_querymode', 'QUERYMODE', 'varchar', 0, 27
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM tss_resfield WHERE ID = 'rf_vss_resuipc_querymode'
);

-- 4. VSS_RESUIPC 的 resuipc 也注册 QUERYMODE（UI 配置，使其在配置页面可编辑）
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, EDITTYPE, LISTSORT, QUERYSORT, EDITSORT, SELECTDATA, ENTRYNUM)
SELECT 'uipc_vss_querymode', 'b45edaad63494430be9e7731b7aed951', 'rf_vss_resuipc_querymode', '查询方式', 'select', 0, 0, 0,
  '[{key:"like",title:"模糊搜索"},{key:"eq",title:"精确匹配"},{key:"in",title:"多值匹配"},{key:"range",title:"范围查询"}]',
  27
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_vss_querymode'
);

-- ============================================================
-- 使用示例
-- ============================================================

-- 示例1: 将物流模块 F02 过滤器改为 @ui:adv 自动生成
-- UPDATE tss_resfilter SET FILTERSQL = '@ui:adv' WHERE ID = 'flt_log_f02';

-- 示例2: 配置 resuipc 查询字段
-- UPDATE tss_resuipc SET QUERYSORT=1, QUERYMODE='like' WHERE FIELDNAME='EXPCOMPANY' AND RESOURCEID='vck_logistics_001';
-- UPDATE tss_resuipc SET QUERYSORT=2, QUERYMODE='like' WHERE FIELDNAME='LOGISTICSNO' AND RESOURCEID='vck_logistics_001';
-- UPDATE tss_resuipc SET QUERYSORT=3, QUERYMODE='eq'  WHERE FIELDNAME='REFTYPE'   AND RESOURCEID='vck_logistics_001';
-- UPDATE tss_resuipc SET QUERYSORT=4, QUERYMODE='in'  WHERE FIELDNAME='STATUS'     AND RESOURCEID='vck_logistics_001';
-- UPDATE tss_resuipc SET QUERYSORT=5, QUERYTYPE='daterange' WHERE FIELDNAME='SENDDATE' AND RESOURCEID='vck_logistics_001';
-- UPDATE tss_resuipc SET QUERYSORT=6, QUERYMODE='like' WHERE FIELDNAME='RECEIVENAME' AND RESOURCEID='vck_logistics_001';
