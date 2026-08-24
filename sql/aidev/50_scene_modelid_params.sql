-- ============================================================
-- AI 场景增强 — 场景级模型路由 + AgentOptions 场景化
-- 内容: tss_ai_scene 增加 MODELID(varchar(64)) + PARAMS(text) 两个字段
--       + ORM resfield 注册 + resuipc UI 配置
--       + LlmConfigService.GetById + SceneConfigService 读取新字段
--       + 各编排器/Hub/Controller 从场景配置取模型和参数
-- 日期: 2026-07-20
-- ============================================================

-- -----------------------------------------------------------
-- 1. ALTER TABLE: 增加 MODELID + PARAMS
-- -----------------------------------------------------------
ALTER TABLE tss_ai_scene
  ADD COLUMN MODELID VARCHAR(64) DEFAULT NULL COMMENT '指定LLM模型ID(TBS_LLM_CONFIG.ID,NULL=用全局默认)' AFTER PROMPTKEY,
  ADD COLUMN PARAMS TEXT DEFAULT NULL COMMENT 'Agent参数JSON(maxSteps/timeoutMs/heartbeatIntervalMs/temperature等,NULL=用代码默认值)' AFTER MODELID;

-- -----------------------------------------------------------
-- 2. TBS resfield 注册
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_modelid', 'tbs_ai_scene_001', NULL, 'MODELID', 'varchar', 0, 1, 64, '指定LLM模型ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_modelid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tsc_params', 'tbs_ai_scene_001', NULL, 'PARAMS', 'text', 0, 1, 'Agent参数JSON'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_params');

-- -----------------------------------------------------------
-- 3. VSS resfield 注册 (REFFIELDID→TBS)
-- -----------------------------------------------------------
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_modelid', 'vss_ai_scene_001', 'rf_tsc_modelid', 'MODELID', 'varchar', 0, 1, 64, '指定LLM模型ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_modelid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vsc_params', 'vss_ai_scene_001', 'rf_tsc_params', 'PARAMS', 'text', 0, 1, 'Agent参数JSON'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_params');

-- -----------------------------------------------------------
-- 4. UI 配置: MODELID(下拉选模型) + PARAMS(textarea)
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_modelid', 'vss_ai_scene_001', 'rf_vsc_modelid', '指定模型', NULL, NULL, 7, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_modelid');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_params', 'vss_ai_scene_001', 'rf_vsc_params', 'Agent参数', NULL, NULL, 8, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_params');

-- 调整已有字段的 EDITSORT，为新字段腾位
UPDATE tss_resuipc SET EDITSORT=9 WHERE ID='uipc_sc_ftools';
UPDATE tss_resuipc SET EDITSORT=10 WHERE ID='uipc_sc_ctxsrc';
UPDATE tss_resuipc SET EDITSORT=11 WHERE ID='uipc_sc_enabled';
UPDATE tss_resuipc SET EDITSORT=12 WHERE ID='uipc_sc_sortno';
UPDATE tss_resuipc SET EDITSORT=13 WHERE ID='uipc_sc_remark';
