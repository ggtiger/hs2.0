-- ============================================================
-- AI 场景配置化 — 数据库升级
-- 内容: tss_ai_scene(AI 场景注册表) + 6 条现有场景种子
--       + ORM 元数据注册 + RS_M23 场景管理模块(通用模块自举)
-- 日期: 2026-07-17
-- 机制: 前端 AiClient/aiAgentProxy 启动时拉取场景配置替代硬编码;
--       表为空/未迁移时回落代码内置默认值,行为与现状一致
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_ai_scene
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_ai_scene (
  ID            VARCHAR(36) NOT NULL COMMENT '主键',
  SCENECODE     VARCHAR(32) NOT NULL COMMENT '场景编码(assistant/form/optimize/aidev/wizard/sfc/自定义)',
  SCENENAME     VARCHAR(64) DEFAULT NULL COMMENT '场景名称',
  TRANSPORT     VARCHAR(16) NOT NULL COMMENT '传输方式: signalr/sse',
  ENDPOINT      VARCHAR(128) DEFAULT NULL COMMENT 'SSE: 完整路由; signalr: Hub方法名(Ask/AskForm/OptimizePrompt)',
  TOOLSET       VARCHAR(32) DEFAULT NULL COMMENT '后端工具集(assistant/formfill/dev/sfc)',
  PROMPTKEY     VARCHAR(64) DEFAULT NULL COMMENT '提示词 key(TBS_ASSISTANT_PROMPT)',
  FRONTENDTOOLS VARCHAR(512) DEFAULT NULL COMMENT '前端工具: all/none 或逗号分隔工具名',
  CONTEXTSOURCE VARCHAR(32) DEFAULT NULL COMMENT '上下文源: none/formContext/sfcContext',
  ENABLED       TINYINT DEFAULT 1,
  SORTNO        INT DEFAULT 0,
  REMARK        VARCHAR(200) DEFAULT NULL,
  ISDELETED     TINYINT DEFAULT 0,
  PRIMARY KEY (ID),
  UNIQUE KEY uk_scenecode (SCENECODE)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI 场景注册表';

-- -----------------------------------------------------------
-- 2. 现有 6 个场景种子(与代码内置行为一一对应)
-- -----------------------------------------------------------
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_assistant', 'assistant', '通用助理', 'signalr', 'Ask', 'assistant', 'all', 'none', 1, 1, '全局抽屉通用助理(SignalR Ask)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='assistant');
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_form', 'form', '表单填报', 'signalr', 'AskForm', 'formfill', 'fill_form,fill_subtable,get_form_data,get_form_field,set_form_field,save_form,add_subtable_row,delete_subtable_row,update_subtable_row,clear_subtable,get_subtable_data,list_subtables', 'formContext', 1, 2, '表单内 AI 填报(SignalR AskForm)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='form');
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_optimize', 'optimize', '提示词优化', 'signalr', 'OptimizePrompt', NULL, 'none', 'none', 1, 3, 'm16 提示词优化(RPC 无 block)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='optimize');
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_aidev', 'aidev', 'AI开发助理', 'sse', '/api/RMAIDev/generate-stream', 'dev', 'none', 'none', 1, 4, 'mAIDev 工作区(SSE)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='aidev');
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_wizard', 'wizard', '模块向导', 'sse', '/api/RMAIDev/generate-step-stream', 'dev', 'none', 'none', 1, 5, 'm18 模块创建向导(SSE 分步)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='wizard');
INSERT INTO tss_ai_scene (ID, SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, FRONTENDTOOLS, CONTEXTSOURCE, ENABLED, SORTNO, REMARK, ISDELETED)
SELECT 'sc_sfc', 'sfc', 'SFC代码助手', 'sse', '/api/RMSfcAi/generate-code', 'sfc', 'none', 'sfcContext', 1, 6, 'm17 SFC 编辑器 AI 面板(SSE)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_scene WHERE SCENECODE='sfc');

-- PROMPTKEY 补充(与后端默认行为一致, 后续可配置化调整)
UPDATE tss_ai_scene SET PROMPTKEY='system_general' WHERE SCENECODE='assistant';
UPDATE tss_ai_scene SET PROMPTKEY='system_form' WHERE SCENECODE='form';
UPDATE tss_ai_scene SET PROMPTKEY='meta_optimize_prompt' WHERE SCENECODE='optimize';

-- -----------------------------------------------------------
-- 3. ORM 资源注册: TBS_AI_SCENE + VSS_AI_SCENE
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_ai_scene_001', 'TBS_AI_SCENE', 'tss_ai_scene', 'TABLE', NULL, 'TSS_AI_SCENE', 'AI 场景注册表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_ai_scene_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_ai_scene_001', 'VSS_AI_SCENE', 'tss_ai_scene', 'DATAVIEW', 'tbs_ai_scene_001', 'A', 'AI 场景视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_ai_scene_001');

-- TBS 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_id', 'tbs_ai_scene_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_code', 'tbs_ai_scene_001', NULL, 'SCENECODE', 'varchar', 0, 0, 32, '场景编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_name', 'tbs_ai_scene_001', NULL, 'SCENENAME', 'varchar', 0, 1, 64, '场景名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_transport', 'tbs_ai_scene_001', NULL, 'TRANSPORT', 'varchar', 0, 0, 16, '传输方式'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_transport');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_endpoint', 'tbs_ai_scene_001', NULL, 'ENDPOINT', 'varchar', 0, 1, 128, '端点'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_endpoint');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_toolset', 'tbs_ai_scene_001', NULL, 'TOOLSET', 'varchar', 0, 1, 32, '后端工具集'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_toolset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_promptkey', 'tbs_ai_scene_001', NULL, 'PROMPTKEY', 'varchar', 0, 1, 64, '提示词key'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_promptkey');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_ftools', 'tbs_ai_scene_001', NULL, 'FRONTENDTOOLS', 'varchar', 0, 1, 512, '前端工具'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_ftools');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_ctxsrc', 'tbs_ai_scene_001', NULL, 'CONTEXTSOURCE', 'varchar', 0, 1, 32, '上下文源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_ctxsrc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tsc_enabled', 'tbs_ai_scene_001', NULL, 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tsc_sortno', 'tbs_ai_scene_001', NULL, 'SORTNO', 'int', 0, 1, '排序'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_sortno');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tsc_remark', 'tbs_ai_scene_001', NULL, 'REMARK', 'varchar', 0, 1, 200, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tsc_isdeleted', 'tbs_ai_scene_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tsc_isdeleted');

-- VSS 字段 (REFFIELDID→TBS)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_id', 'vss_ai_scene_001', 'rf_tsc_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_code', 'vss_ai_scene_001', 'rf_tsc_code', 'SCENECODE', 'varchar', 0, 0, 32, '场景编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_code');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_name', 'vss_ai_scene_001', 'rf_tsc_name', 'SCENENAME', 'varchar', 0, 1, 64, '场景名称'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_name');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_transport', 'vss_ai_scene_001', 'rf_tsc_transport', 'TRANSPORT', 'varchar', 0, 0, 16, '传输方式'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_transport');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_endpoint', 'vss_ai_scene_001', 'rf_tsc_endpoint', 'ENDPOINT', 'varchar', 0, 1, 128, '端点'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_endpoint');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_toolset', 'vss_ai_scene_001', 'rf_tsc_toolset', 'TOOLSET', 'varchar', 0, 1, 32, '后端工具集'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_toolset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_promptkey', 'vss_ai_scene_001', 'rf_tsc_promptkey', 'PROMPTKEY', 'varchar', 0, 1, 64, '提示词key'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_promptkey');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_ftools', 'vss_ai_scene_001', 'rf_tsc_ftools', 'FRONTENDTOOLS', 'varchar', 0, 1, 512, '前端工具'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_ftools');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_ctxsrc', 'vss_ai_scene_001', 'rf_tsc_ctxsrc', 'CONTEXTSOURCE', 'varchar', 0, 1, 32, '上下文源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_ctxsrc');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vsc_enabled', 'vss_ai_scene_001', 'rf_tsc_enabled', 'ENABLED', 'int', 0, 1, '启用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_enabled');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vsc_sortno', 'vss_ai_scene_001', 'rf_tsc_sortno', 'SORTNO', 'int', 0, 1, '排序'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_sortno');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vsc_remark', 'vss_ai_scene_001', 'rf_tsc_remark', 'REMARK', 'varchar', 0, 1, 200, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_remark');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vsc_isdeleted', 'vss_ai_scene_001', 'rf_tsc_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vsc_isdeleted');

-- -----------------------------------------------------------
-- 4. 过滤器: F00(按编码) / F01(列表)
-- -----------------------------------------------------------
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_sc_f00', 'vss_ai_scene_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_sc_f00');
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_sc_f01', 'vss_ai_scene_001', 'F01', '1=1 AND A.ISDELETED=0', 'SORTNO, SCENECODE', '场景列表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_sc_f01');

-- -----------------------------------------------------------
-- 5. UI 配置: VSS_AI_SCENE
-- -----------------------------------------------------------
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_idx', 'vss_ai_scene_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_code', 'vss_ai_scene_001', 'rf_vsc_code', '场景编码', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_code');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_name', 'vss_ai_scene_001', 'rf_vsc_name', '场景名称', 2, NULL, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_name');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_transport', 'vss_ai_scene_001', 'rf_vsc_transport', '传输', 3, NULL, 3, 'select', 'signalr:SignalR,sse:SSE'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_transport');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_endpoint', 'vss_ai_scene_001', 'rf_vsc_endpoint', '端点', 4, NULL, 4, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_endpoint');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_toolset', 'vss_ai_scene_001', 'rf_vsc_toolset', '后端工具集', 5, NULL, 5, 'select', 'assistant:assistant,formfill:formfill,dev:dev,sfc:sfc'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_toolset');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_promptkey', 'vss_ai_scene_001', 'rf_vsc_promptkey', '提示词key', NULL, NULL, 6, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_promptkey');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_ftools', 'vss_ai_scene_001', 'rf_vsc_ftools', '前端工具', 6, NULL, 7, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_ftools');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_ctxsrc', 'vss_ai_scene_001', 'rf_vsc_ctxsrc', '上下文源', NULL, NULL, 8, 'select', 'none:无,formContext:表单上下文,sfcContext:SFC上下文'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_ctxsrc');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_enabled', 'vss_ai_scene_001', 'rf_vsc_enabled', '启用', 7, NULL, 9, 'checkbox', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_enabled');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_sortno', 'vss_ai_scene_001', 'rf_vsc_sortno', '排序', NULL, NULL, 10, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_sortno');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_sc_remark', 'vss_ai_scene_001', 'rf_vsc_remark', '备注', 8, NULL, 11, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_sc_remark');

-- -----------------------------------------------------------
-- 6. RS_M23 模块注册 (AI 场景管理, 通用模块自举)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m23_module_001', 'RS_M23', 'AI场景管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M23');

SET @m23 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M23');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m23_qry', @m23, 'QRY', 'vss_ai_scene_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m23_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m23_qqry', @m23, 'QQRY', 'vss_ai_scene_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m23_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m23_sel', @m23, 'SEL', 'vss_ai_scene_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m23_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m23_main', @m23, 'MAIN', 'vss_ai_scene_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m23_main');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m23_a01', @m23, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m23_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m23_a02', @m23, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m23_a02');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m23_a04', @m23, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m23_a04');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m23_a07', @m23, 'A07', 'delete', 'delete', '', NULL, '删除', 'MAIN', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m23_a07');

-- -----------------------------------------------------------
-- 7. 菜单 + 功能点 (通用模块路由 /g/RS_M23/main)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m23_001', 'RS_M23', 'AI场景管理', '/g/RS_M23/main', '3e3c83ce2b3c475b82902478c89c27c0', 230, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M23');

SET @f23 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M23');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m23_a01', @f23, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m23_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m23_a04', @f23, 'A04', '编辑', 'A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m23_a04');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m23_a07', @f23, 'A07', '删除', 'A07'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m23_a07');

-- -----------------------------------------------------------
-- 8. 通用模块页面 + 按钮配置 (自举)
-- -----------------------------------------------------------
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, QUERYAPICODE, SORTNO, PAGECONFIG, ISDELETED)
SELECT 'mp_rs_m23_main', 'RS_M23', 'main', '场景列表', 'list', 'standard', 'A01', 1, '{"defaultFormPageCode":"form"}', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m23_main');
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, OPENAPICODE, SAVEAPICODE, SORTNO, ISDELETED)
SELECT 'mp_rs_m23_form', 'RS_M23', 'form', '场景编辑', 'form', 'standard', 'A02', 'A04', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m23_form');

INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m23_main_add', 'mp_rs_m23_main', 'RS_M23', 'A04', '添加', 'crud', 'add', 'header', 'direct', 'RS_M23/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m23_main_add');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m23_form_save', 'mp_rs_m23_form', 'RS_M23', 'A04', '保存', 'crud', 'save', 'footer', 'direct', 'RS_M23/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m23_form_save');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, SORTNO, ISDELETED)
SELECT 'mb_rs_m23_form_cancel', 'mp_rs_m23_form', 'RS_M23', NULL, '取消', 'crud', 'cancel', 'footer', 'direct', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m23_form_cancel');

-- -----------------------------------------------------------
-- 9. 纳入版本管理 (配置类对象)
-- -----------------------------------------------------------
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_aiscene', 'VSS_AI_SCENE', 'scene', 'SCENECODE', 'SCENENAME', 50, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_AI_SCENE');
