-- ============================================================
-- AI 开发助理模块 Chunk 1 — RS_MAIDEVUPG 模块注册（升级管理）
-- 包含: tss_moudle / tss_moudlepath / tss_moudleapi / tss_func / tss_funcpoint
-- 模块指向 VSS_AIDEV_UPGRADE (升级记录主表)
-- 四路径: QRY/QQRY/MAIN/SEL 全部指向 vss_aidev_upgrade_001
-- 标准 CRUD API: A01查询/A02打开/A03保存/A04删除
-- 父菜单: 系统管理(3e3c83ce2b3c475b82902478c89c27c0)
-- ============================================================

-- ============================================================
-- 1. tss_moudle — 模块注册
-- ============================================================
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_maidevupg_module_001', 'RS_MAIDEVUPG', '升级管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE = 'RS_MAIDEVUPG');

-- ============================================================
-- 2. tss_moudlepath — QRY/QQRY/MAIN/SEL 四路径
-- ============================================================
SET @module_id = (SELECT ID FROM tss_moudle WHERE MODULECODE = 'RS_MAIDEVUPG');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidevupg_qry', @module_id, 'QRY', 'vss_aidev_upgrade_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidevupg_qry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidevupg_qqry', @module_id, 'QQRY', 'vss_aidev_upgrade_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidevupg_qqry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidevupg_sel', @module_id, 'SEL', 'vss_aidev_upgrade_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidevupg_sel');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidevupg_main', @module_id, 'MAIN', 'vss_aidev_upgrade_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidevupg_main');

-- ============================================================
-- 3. tss_moudleapi — A01查询/A02打开/A03保存/A04删除
-- ============================================================
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidevupg_a01', @module_id, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidevupg_a01');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidevupg_a02', @module_id, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidevupg_a02');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidevupg_a03', @module_id, 'A03', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidevupg_a03');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidevupg_a04', @module_id, 'A04', 'delete', 'delete', '', NULL, '删除', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidevupg_a04');

-- ============================================================
-- 4. tss_func — 菜单（系统管理 > 升级管理）
-- ============================================================
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, FUNCICON, ISOUTERURL, SORTCODE)
SELECT UUID(), 'RS_MAIDEVUPG', '升级管理', 's01/mAIDevUPG', '3e3c83ce2b3c475b82902478c89c27c0', NULL, NULL, 180
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE = 'RS_MAIDEVUPG');

-- ============================================================
-- 5. tss_funcpoint — 功能点权限
-- ============================================================
SET @func_id = (SELECT ID FROM tss_func WHERE FUNCCODE = 'RS_MAIDEVUPG' LIMIT 1);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A01', '查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A01' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A03', '新增编辑'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A03' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A04', '删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A04' AND FUNCID = @func_id);
