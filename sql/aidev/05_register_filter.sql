-- ============================================================
-- AI 开发助理模块 Chunk 1 — 过滤器 (tss_resfilter)
-- 三条铁律:
--   1. F00 单条: FILTERSQL = 'A.ID = @ID'
--   2. F01 列表: 必须以 '1=1' 开头, 用 #if("$!{INPUT}"!="") 判断, AND A.ISDELETED = 0
--   3. ORDERBY 不能带表别名前缀 (用 CREATEDTIME DESC)
-- F03 专用:
--   - changeset: A.SESSIONID=@SESSIONID
--   - changeitem: A.CHANGESETID=@CHANGESETID
--   - upgrade_log/snapshot: A.UPGRADEID=@UPGRADEID
-- 模糊搜索字段:
--   - session: SESSIONNAME/SESSIONCODE
--   - upgrade: UPGRADECODE/SESSIONNAME
--   - changeitem: TARGET
--   - changeset: TITLE/CHANGESETCODE
-- 注意: NVelocity 不能处理单引号, LIKE 用 CONCAT('%',@INPUT,'%')
-- ============================================================

-- ============================================================
-- 1. VSS_AIDEV_SESSION 过滤器
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aisess_f00', 'vss_aidev_session_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aisess_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aisess_f01', 'vss_aidev_session_001', 'F01', CONCAT('1=1', CHAR(10),
'#if("$!{INPUT}"!="")', CHAR(10),
'AND (A.SESSIONNAME LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'OR A.SESSIONCODE LIKE CONCAT(''%'',@INPUT,''%''))', CHAR(10),
'#end', CHAR(10),
'AND A.ISDELETED = 0'), 'CREATEDTIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aisess_f01');

-- ============================================================
-- 2. VSS_AIDEV_CHANGESET 过滤器
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aics_f00', 'vss_aidev_changeset_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aics_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aics_f01', 'vss_aidev_changeset_001', 'F01', CONCAT('1=1', CHAR(10),
'#if("$!{INPUT}"!="")', CHAR(10),
'AND (A.TITLE LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'OR A.CHANGESETCODE LIKE CONCAT(''%'',@INPUT,''%''))', CHAR(10),
'#end', CHAR(10),
'AND A.ISDELETED = 0'), 'CREATEDTIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aics_f01');

-- F03: 按会话ID加载变更包（子表查询）
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aics_f03', 'vss_aidev_changeset_001', 'F03', CONCAT('1=1', CHAR(10),
'AND A.SESSIONID = @SESSIONID', CHAR(10),
'AND A.ISDELETED = 0'), NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aics_f03');

-- ============================================================
-- 3. VSS_AIDEV_CHANGEITEM 过滤器
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aici_f00', 'vss_aidev_changeitem_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aici_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aici_f01', 'vss_aidev_changeitem_001', 'F01', CONCAT('1=1', CHAR(10),
'#if("$!{INPUT}"!="")', CHAR(10),
'AND A.TARGET LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'#end', CHAR(10),
'AND A.ISDELETED = 0'), 'CREATEDTIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aici_f01');

-- F03: 按变更包ID加载变更项（子表查询）
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aici_f03', 'vss_aidev_changeitem_001', 'F03', CONCAT('1=1', CHAR(10),
'AND A.CHANGESETID = @CHANGESETID', CHAR(10),
'AND A.ISDELETED = 0'), NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aici_f03');

-- ============================================================
-- 4. VSS_AIDEV_UPGRADE 过滤器
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupg_f00', 'vss_aidev_upgrade_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupg_f00');

INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupg_f01', 'vss_aidev_upgrade_001', 'F01', CONCAT('1=1', CHAR(10),
'#if("$!{INPUT}"!="")', CHAR(10),
'AND (A.UPGRADECODE LIKE CONCAT(''%'',@INPUT,''%'')', CHAR(10),
'OR A.SESSIONNAME LIKE CONCAT(''%'',@INPUT,''%''))', CHAR(10),
'#end', CHAR(10),
'AND A.ISDELETED = 0'), 'CREATEDTIME DESC'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupg_f01');

-- ============================================================
-- 5. VSS_AIDEV_UPGRADE_LOG 过滤器 (无 F01, 只 F00 + F03)
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupgl_f00', 'vss_aidev_upgrade_log_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupgl_f00');

-- F03: 按升级ID加载日志（子表查询）
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupgl_f03', 'vss_aidev_upgrade_log_001', 'F03', CONCAT('1=1', CHAR(10),
'AND A.UPGRADEID = @UPGRADEID'), NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupgl_f03');

-- ============================================================
-- 6. VSS_AIDEV_UPGRADE_SNAPSHOT 过滤器 (无 F01, 只 F00 + F03)
-- ============================================================
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupgs_f00', 'vss_aidev_upgrade_snapshot_001', 'F00', 'A.ID = @ID', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupgs_f00');

-- F03: 按升级ID加载快照（子表查询）
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY)
SELECT 'flt_aiupgs_f03', 'vss_aidev_upgrade_snapshot_001', 'F03', CONCAT('1=1', CHAR(10),
'AND A.UPGRADEID = @UPGRADEID'), NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID = 'flt_aiupgs_f03');
