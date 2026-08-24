-- ============================================================
-- 代码资产逻辑删除 + 删除版本管理 — 数据库升级
-- 内容: tss_code_asset 路径唯一键改造: uk_path(MODULEPATH) → uk_livepath(生成列)
--       生成列 LIVEPATH = IF(ISDELETED=0, MODULEPATH, NULL)
--       逻辑删除行自动让出路径(多个 NULL 不冲突), 活动行路径仍唯一
--       → 逻辑删除后同路径可重新新建, 回滚写回 ISDELETED=0 即恢复原路径
-- 配套: 后端 DevVersionService 识别 ISDELETED 0→1 的 update 为 delete 版本,
--       回滚 delete 行在则 UPDATE 写回/行不在才 INSERT; 前端四类型统一逻辑删除
-- 日期: 2026-07-18
-- ============================================================

-- 幂等: 仅当 uk_path 仍存在时执行改造
SET @c1 := (SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='tss_code_asset' AND INDEX_NAME='uk_path');
SET @sql1 := IF(@c1>0,
  'ALTER TABLE tss_code_asset DROP INDEX uk_path',
  'SELECT 1');
PREPARE st1 FROM @sql1; EXECUTE st1; DEALLOCATE PREPARE st1;

SET @c2 := (SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='tss_code_asset' AND COLUMN_NAME='LIVEPATH');
SET @sql2 := IF(@c2=0,
  'ALTER TABLE tss_code_asset ADD COLUMN LIVEPATH VARCHAR(200) GENERATED ALWAYS AS (IF(ISDELETED=0, MODULEPATH, NULL)) VIRTUAL COMMENT ''活动路径(ISDELETED=0时=MODULEPATH, 否则NULL; 唯一键 uk_livepath)''',
  'SELECT 1');
PREPARE st2 FROM @sql2; EXECUTE st2; DEALLOCATE PREPARE st2;

SET @c3 := (SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='tss_code_asset' AND INDEX_NAME='uk_livepath');
SET @sql3 := IF(@c3=0,
  'ALTER TABLE tss_code_asset ADD UNIQUE KEY uk_livepath (LIVEPATH)',
  'SELECT 1');
PREPARE st3 FROM @sql3; EXECUTE st3; DEALLOCATE PREPARE st3;
