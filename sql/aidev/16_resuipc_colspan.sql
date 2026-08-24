-- ============================================================
-- tss_resuipc 加 COLSPAN 列(字段占宽) — 数据库升级
-- 内容: ALTER 加列 + ORM resfield 注册(TBS/VSS)
-- 日期: 2026-07-17
-- 机制: COLSPAN>=2 → gen.js 置 FormItem single=true(独占整行);
--       默认 1 = 按列宽(与相邻字段同行, 历史行为不变)
-- ============================================================

-- 1. ALTER 加列(information_schema 守卫, MySQL 5.7 无 IF NOT EXISTS)
SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='tss_resuipc' AND COLUMN_NAME='COLSPAN');
SET @ddl := IF(@col_exists=0,
  'ALTER TABLE tss_resuipc ADD COLUMN COLSPAN TINYINT DEFAULT 1 COMMENT ''字段占宽: 1=按列宽, 2=整行''',
  'SELECT 1');
PREPARE stmt FROM @ddl; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- 2. ORM resfield 注册
-- TBS_RESUIPC (RESOURCEID=ec18e3a20a314b8fb912a2991b6b5205)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tu_colspan', 'ec18e3a20a314b8fb912a2991b6b5205', NULL, 'COLSPAN', 'int', 0, 1, '字段占宽(1=按列宽,2=整行)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tu_colspan');

-- VSS_RESUIPC (RESOURCEID=b45edaad63494430be9e7731b7aed951, REFFIELDID→TBS 字段)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vu_colspan', 'b45edaad63494430be9e7731b7aed951', 'rf_tu_colspan', 'COLSPAN', 'int', 0, 1, '字段占宽(1=按列宽,2=整行)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vu_colspan');
