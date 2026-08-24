-- ============================================================
-- AI 调用记录增强 — MODULECODE/TOOLNAME 字段 ORM 元数据注册
-- 内容: TBS + VCK resfield 注册 MODULECODE/TOOLNAME
--       + resuipc UI 配置
--       + UsageRecord + UsageLogger INSERT 补上这两列（代码已改）
--       + AiDev/Wizard 场景启用 DbUsageLogger（代码已改）
-- 日期: 2026-07-20
-- ============================================================

-- -----------------------------------------------------------
-- 1. TBS resfield 注册
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tlu_modulecode', 'tbs_llm_usage_001', NULL, 'MODULECODE', 'varchar', 0, 1, 64, '关联模块编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tlu_modulecode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tlu_toolname', 'tbs_llm_usage_001', NULL, 'TOOLNAME', 'varchar', 0, 1, 64, '工具名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tlu_toolname');

-- -----------------------------------------------------------
-- 2. VSS resfield 注册 (REFFIELDID→TBS)
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vlu_modulecode', 'vck_llm_usage_001', 'rf_tlu_modulecode', 'MODULECODE', 'varchar', 0, 1, 64, '关联模块编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vlu_modulecode');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vlu_toolname', 'vck_llm_usage_001', 'rf_tlu_toolname', 'TOOLNAME', 'varchar', 0, 1, 64, '工具名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vlu_toolname');

-- -----------------------------------------------------------
-- 3. UI 配置: MODULECODE(列表+查询) + TOOLNAME(列表+查询)
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_lu_modulecode', 'vck_llm_usage_001', 'rf_vlu_modulecode', '模块', 6, 3, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_lu_modulecode');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_lu_toolname', 'vck_llm_usage_001', 'rf_vlu_toolname', '工具', 7, 4, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_lu_toolname');
