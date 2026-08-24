-- ============================================================
-- Phase 4: AI能力前台化 - 数据库迁移
-- 4.1 模型降级路由 (FALLBACKID)
-- 4.2 提示词 A/B 测试 (VERSION/WEIGHT)
-- 4.3 每日配额 (DAILYQUOTA)
-- + 补齐 m27 相关资源 resuipc FIELDNAME（NULL→实际字段名）
-- ============================================================

-- ============ 4.1 FALLBACKID ============
-- 物理列已在 50_scene_modelid_params.sql 创建
-- resfield 已在 50_scene_modelid_params.sql 注册
-- resuipc FIELDNAME 补齐

UPDATE tss_resuipc SET FIELDNAME='FALLBACKID' WHERE ID='uipc_lc_fallbackid';
UPDATE tss_resuipc SET FIELDNAME='ISVISION' WHERE ID='uipc_lcfg_isvision';

-- ============ 4.2 VERSION/WEIGHT ============
-- 物理列已存在
-- resfield 已注册
-- 唯一索引已改为 UK_PROMPTKEY_VER(PROMPTKEY, VERSION)
-- resuipc FIELDNAME 补齐

UPDATE tss_resuipc SET FIELDNAME='VERSION' WHERE ID='uipc_ap_version';
UPDATE tss_resuipc SET FIELDNAME='WEIGHT' WHERE ID='uipc_ap_weight';

-- ============ 4.3 DAILYQUOTA ============
-- 物理列已在 50_scene_modelid_params.sql 创建
-- resfield 已注册
-- resuipc FIELDNAME 补齐

UPDATE tss_resuipc SET FIELDNAME='DAILYQUOTA' WHERE ID='uipc_sc_dailyquota';

-- ============ 补齐 m27 相关资源所有 resuipc FIELDNAME ============
-- VCK_LLM_CONFIG
UPDATE tss_resuipc SET FIELDNAME='APIKEY' WHERE ID='uipc_lcfg_apikey';
UPDATE tss_resuipc SET FIELDNAME='BASEURL' WHERE ID='uipc_lcfg_baseurl';
UPDATE tss_resuipc SET FIELDNAME='ENABLED' WHERE ID='uipc_lcfg_enabled';
UPDATE tss_resuipc SET FIELDNAME='MODELNAME' WHERE ID='uipc_lcfg_model';
UPDATE tss_resuipc SET FIELDNAME='PARAMS' WHERE ID='uipc_lcfg_params';
UPDATE tss_resuipc SET FIELDNAME='PRICEINPUT' WHERE ID='uipc_lcfg_pi';
UPDATE tss_resuipc SET FIELDNAME='PRICEOUTPUT' WHERE ID='uipc_lcfg_po';
UPDATE tss_resuipc SET FIELDNAME='PROVIDER' WHERE ID='uipc_lcfg_provider';

-- VCK_LLM_USAGE
UPDATE tss_resuipc SET FIELDNAME='COST' WHERE ID='uipc_luse_cost';
UPDATE tss_resuipc SET FIELDNAME='ISSUCCESS' WHERE ID='uipc_luse_ok';
UPDATE tss_resuipc SET FIELDNAME='OPERATIONTYPE' WHERE ID='uipc_luse_optype';
UPDATE tss_resuipc SET FIELDNAME='REQUESTTIME' WHERE ID='uipc_luse_time';
UPDATE tss_resuipc SET FIELDNAME='TOTALTOKENS' WHERE ID='uipc_luse_tt';
UPDATE tss_resuipc SET FIELDNAME='USERNAME' WHERE ID='uipc_luse_uname';
UPDATE tss_resuipc SET FIELDNAME='MODULECODE' WHERE ID='uipc_lu_modulecode';
UPDATE tss_resuipc SET FIELDNAME='TOOLNAME' WHERE ID='uipc_lu_toolname';

-- VSS_AI_SCENE
UPDATE tss_resuipc SET FIELDNAME='SCENECODE' WHERE ID='uipc_sc_code';
UPDATE tss_resuipc SET FIELDNAME='CONTEXTSOURCE' WHERE ID='uipc_sc_ctxsrc';
UPDATE tss_resuipc SET FIELDNAME='ENABLED' WHERE ID='uipc_sc_enabled';
UPDATE tss_resuipc SET FIELDNAME='ENDPOINT' WHERE ID='uipc_sc_endpoint';
UPDATE tss_resuipc SET FIELDNAME='FRONTENDTOOLS' WHERE ID='uipc_sc_ftools';
UPDATE tss_resuipc SET FIELDNAME='ID' WHERE ID='uipc_sc_idx';
UPDATE tss_resuipc SET FIELDNAME='MODELID' WHERE ID='uipc_sc_modelid';
UPDATE tss_resuipc SET FIELDNAME='SCENENAME' WHERE ID='uipc_sc_name';
UPDATE tss_resuipc SET FIELDNAME='PARAMS' WHERE ID='uipc_sc_params';
UPDATE tss_resuipc SET FIELDNAME='PROMPTKEY' WHERE ID='uipc_sc_promptkey';
UPDATE tss_resuipc SET FIELDNAME='REMARK' WHERE ID='uipc_sc_remark';
UPDATE tss_resuipc SET FIELDNAME='SORTNO' WHERE ID='uipc_sc_sortno';
UPDATE tss_resuipc SET FIELDNAME='TOOLSET' WHERE ID='uipc_sc_toolset';
UPDATE tss_resuipc SET FIELDNAME='TRANSPORT' WHERE ID='uipc_sc_transport';
