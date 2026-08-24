-- ============================================================
-- AI 开发助理模块 Chunk 1 — RS_MAIDEV 模块注册
-- 包含: tss_moudle / tss_moudlepath / tss_moudleapi / tss_func / tss_funcpoint
-- 模块指向 VSS_AIDEV_SESSION (开发会话主表)
-- 四路径: QRY/QQRY/MAIN/SEL 全部指向 vss_aidev_session_001
-- 标准 CRUD API: A01查询/A02打开/A03保存/A04删除
-- 父菜单: 系统管理(3e3c83ce2b3c475b82902478c89c27c0)
-- ============================================================

-- ============================================================
-- 1. tss_moudle — 模块注册
-- ============================================================
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_maidev_module_001', 'RS_MAIDEV', 'AI开发助理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE = 'RS_MAIDEV');

-- ============================================================
-- 2. tss_moudlepath — QRY/QQRY/MAIN/SEL 四路径
-- ============================================================
SET @module_id = (SELECT ID FROM tss_moudle WHERE MODULECODE = 'RS_MAIDEV');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidev_qry', @module_id, 'QRY', 'vss_aidev_session_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidev_qry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidev_qqry', @module_id, 'QQRY', 'vss_aidev_session_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidev_qqry');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidev_sel', @module_id, 'SEL', 'vss_aidev_session_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidev_sel');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID)
SELECT 'mp_maidev_main', @module_id, 'MAIN', 'vss_aidev_session_001'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID = 'mp_maidev_main');

-- ============================================================
-- 3. tss_moudleapi — A01查询/A02打开/A03保存/A04删除
-- ============================================================
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidev_a01', @module_id, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidev_a01');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidev_a02', @module_id, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidev_a02');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidev_a03', @module_id, 'A03', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidev_a03');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM)
SELECT 'ma_maidev_a04', @module_id, 'A04', 'delete', 'delete', '', NULL, '删除', 'MAIN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID = 'ma_maidev_a04');

-- ============================================================
-- 4. tss_func — 菜单（系统管理 > AI开发助理）
-- ============================================================
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, FUNCICON, ISOUTERURL, SORTCODE)
SELECT UUID(), 'RS_MAIDEV', 'AI开发助理', 's01/mAIDev', '3e3c83ce2b3c475b82902478c89c27c0', NULL, NULL, 170
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE = 'RS_MAIDEV');

-- ============================================================
-- 5. tss_funcpoint — 功能点权限
-- ============================================================
SET @func_id = (SELECT ID FROM tss_func WHERE FUNCCODE = 'RS_MAIDEV' LIMIT 1);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A01', '查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A01' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A03', '新增编辑'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A03' AND FUNCID = @func_id);

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_id, 'A04', '删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCPOINTCODE = 'A04' AND FUNCID = @func_id);
