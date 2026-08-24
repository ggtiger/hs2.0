-- ============================================================
-- csharp/sql 资产回填 MODULEPATH — 数据库升级
-- 内容: 按 SC_/SS_+模块编码 前缀推导目录, 回填路径
--       约定: @/scripts/{模块}/{编码}.{ext}; 无模块前缀 → @/scripts/{编码}.{ext}
-- 日期: 2026-07-18
-- ============================================================

-- csharp: 最长模块编码前缀匹配
UPDATE tss_code_asset a
JOIN (
  SELECT a2.ID, (
    SELECT CONCAT('@/scripts/', m2.MODULECODE, '/', a2.CODE, '.cs')
    FROM tss_moudle m2
    WHERE a2.CODE LIKE CONCAT('SC_', m2.MODULECODE, '_%')
    ORDER BY LENGTH(m2.MODULECODE) DESC LIMIT 1
  ) AS P FROM tss_code_asset a2 WHERE a2.ASSETTYPE='csharp'
) x ON x.ID = a.ID
SET a.MODULEPATH = IFNULL(x.P, CONCAT('@/scripts/', a.CODE, '.cs'))
WHERE a.ASSETTYPE='csharp' AND (a.MODULEPATH IS NULL OR a.MODULEPATH='');

-- sql: 同规则(.sql 后缀)
UPDATE tss_code_asset a
JOIN (
  SELECT a2.ID, (
    SELECT CONCAT('@/scripts/', m2.MODULECODE, '/', a2.CODE, '.sql')
    FROM tss_moudle m2
    WHERE a2.CODE LIKE CONCAT('SS_', m2.MODULECODE, '_%')
    ORDER BY LENGTH(m2.MODULECODE) DESC LIMIT 1
  ) AS P FROM tss_code_asset a2 WHERE a2.ASSETTYPE='sql'
) x ON x.ID = a.ID
SET a.MODULEPATH = IFNULL(x.P, CONCAT('@/scripts/', a.CODE, '.sql'))
WHERE a.ASSETTYPE='sql' AND (a.MODULEPATH IS NULL OR a.MODULEPATH='');
