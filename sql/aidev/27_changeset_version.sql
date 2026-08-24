-- ============================================================
-- AI 变更集通道版本捕获 — 数据库升级
-- 内容: ① tss_dev_version_cfg 增补 VSS_DICT/VSS_FUNC/VSS_FUNCPOINT 纳管
--       ② D0701 字典补 dict/menu/permission 类型项
-- 说明: ChangeSetEngine.ExecuteConfirmed 提交后按 CATEGORY+METADATA 快照
--       触及对象(资源/字段/UI/过滤器/模块/接口/页面/按钮/字典/菜单/权限)
-- 日期: 2026-07-18
-- ============================================================

-- ① 纳管配置增补
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_dict', 'VSS_DICT', 'dict', 'DICTNAME', 'DICTNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE ID='dvc_dict');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_func', 'VSS_FUNC', 'menu', 'FUNCCODE', 'FUNCNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE ID='dvc_func');
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_funcpoint', 'VSS_FUNCPOINT', 'permission', 'FUNCPOINTCODE', 'FUNCPOINTNAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE ID='dvc_funcpoint');

-- ② D0701 字典补类型项
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_16', 'dict_d0701', '数据字典', 'dict', 16 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_16');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_17', 'dict_d0701', '菜单', 'menu', 17 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_17');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_18', 'dict_d0701', '权限点', 'permission', 18 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_18');
