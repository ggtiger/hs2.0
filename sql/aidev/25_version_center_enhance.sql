-- ============================================================
-- 版本中心增强 — 数据库升级
-- 内容: ① RS_M22 注册 A06(查对象当前快照)/A07(版本标记TAG/置顶) 自定义接口
--       ② D0701 字典补 code(代码资产) 项(18_dicts.sql 已同步, 此处幂等兜底)
-- 说明: CHANGENOTE 变更说明由后端 DataController 透传(保留参数键), 无表结构变更
-- 日期: 2026-07-18
-- ============================================================

-- ① RS_M22 A06/A07 (APITYPE=NULL 走 RDevVersionController.doMyApi)
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APINAME, APITYPE, PATHNAME)
SELECT 'ma_m22_a06', ID, 'A06', '查对象当前快照', NULL, 'MAIN' FROM tss_moudle WHERE MODULECODE='RS_M22'
AND NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M22') AND APICODE='A06');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APINAME, APITYPE, PATHNAME)
SELECT 'ma_m22_a07', ID, 'A07', '版本标记(TAG/置顶)', NULL, 'MAIN' FROM tss_moudle WHERE MODULECODE='RS_M22'
AND NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M22') AND APICODE='A07');

-- ② D0701 字典补 code(代码资产)
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_15', 'dict_d0701', '代码资产', 'code', 15 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_15');
