-- ============================================================
-- 代码资产接口测试 — 数据库升级
-- 内容: RS_M17 注册 A07(SQL模板试运行)/A08(C#脚本试运行) 自定义接口
--       路由: /api/RCodeAsset/call/RS_M17/A07|A08 (RCodeAssetController.doMyApi)
-- 日期: 2026-07-18
-- ============================================================

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APINAME, APITYPE, PATHNAME)
SELECT 'ma_m17_a07', ID, 'A07', 'SQL模板试运行', NULL, 'MAIN' FROM tss_moudle WHERE MODULECODE='RS_M17'
AND NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M17') AND APICODE='A07');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APINAME, APITYPE, PATHNAME)
SELECT 'ma_m17_a08', ID, 'A08', 'C#脚本试运行', NULL, 'MAIN' FROM tss_moudle WHERE MODULECODE='RS_M17'
AND NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M17') AND APICODE='A08');
