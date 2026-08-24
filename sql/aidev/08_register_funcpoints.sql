-- ============================================================
-- AI 开发助理 — 自定义接口权限点补充 (Chunk 6.2)
-- RS_MAIDEV: A05 生成 / A07 导出 / A08 执行(开发环境)
-- RS_MAIDEVUPG: A05 导入 / A06 执行 / A07 回滚 / A08 预览
-- 幂等写法，可重复执行
-- ============================================================

SET @func_maidev = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_MAIDEV' LIMIT 1);
SET @func_maidevupg = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_MAIDEVUPG' LIMIT 1);

-- RS_MAIDEV 权限点
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidev, 'A05', 'AI生成'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidev AND FUNCPOINTCODE='A05');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidev, 'A07', '导出脚本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidev AND FUNCPOINTCODE='A07');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidev, 'A08', '开发环境执行'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidev AND FUNCPOINTCODE='A08');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidev, 'A09', '确认变更项'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidev AND FUNCPOINTCODE='A09');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidev, 'A10', '拒绝变更项'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidev AND FUNCPOINTCODE='A10');

-- RS_MAIDEVUPG 权限点
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidevupg, 'A05', '导入脚本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidevupg AND FUNCPOINTCODE='A05');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidevupg, 'A06', '执行升级'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidevupg AND FUNCPOINTCODE='A06');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidevupg, 'A07', '回滚升级'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidevupg AND FUNCPOINTCODE='A07');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT UUID(), @func_maidevupg, 'A08', '预览变更项'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID=@func_maidevupg AND FUNCPOINTCODE='A08');
