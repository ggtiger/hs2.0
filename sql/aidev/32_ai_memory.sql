-- ============================================================
-- AI 统一记忆中枢 — 数据库升级
-- 内容: ① tss_ai_memory(统一记忆表: rule/example/pitfall/glossary)
--       ② tss_ai_feedback(反馈回流表: thumbs_up/down/edited/adopted)
--       ③ ORM 元数据注册 (TBS+VSS 两张)
--       ④ RS_M26 记忆管理模块(通用模块自举)
--       ⑤ 3 个声明式 AI 工具(search_memory/recall_examples/list_pitfalls)
--       ⑥ 纳入版本管理
-- 机制: MemoryService 按 ASSETTYPE+SCENE+WIZARD_STEP 三维检索;
--       反馈表自动回流为 example; HITCOUNT 记录命中频次;
--       5-10 人团队用 LIKE+标签检索不上向量库
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 1. 建表: tss_ai_memory(统一记忆表)
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_ai_memory (
  ID             VARCHAR(36) NOT NULL COMMENT '主键',
  MEMORYTYPE     VARCHAR(20) NOT NULL COMMENT 'rule(规则)/example(示例)/pitfall(反模式)/glossary(术语)',
  ASSETTYPE      VARCHAR(20) NOT NULL DEFAULT 'general' COMMENT 'sfc/sql/csharp/metadata/wizard/general',
  TITLE          VARCHAR(200) NOT NULL COMMENT '标题(简短描述)',
  CONTENT        TEXT NOT NULL COMMENT '主体内容(规则说明/正确代码/术语定义)',
  WRONG_CONTENT  TEXT DEFAULT NULL COMMENT '仅 pitfall: 错误示例',
  FIX_STRATEGY   TEXT DEFAULT NULL COMMENT '仅 pitfall: 修正方案',
  TAGS           VARCHAR(500) DEFAULT NULL COMMENT '关键词标签(逗号分隔, 用于 LIKE 检索)',
  SCENE_CODES    VARCHAR(500) DEFAULT NULL COMMENT '关联场景编码(逗号分隔, NULL=全局)',
  WIZARD_STEPS   VARCHAR(100) DEFAULT NULL COMMENT '关联向导步骤(逗号分隔 0-5, NULL=不限)',
  PRIORITY       INT DEFAULT 3 COMMENT '优先级 1-5(影响注入顺序, 5 最优先)',
  QUALITY_SCORE  INT DEFAULT 0 COMMENT 'example 评分 0-5(用户反馈累计)',
  HITCOUNT       INT DEFAULT 0 COMMENT '被命中次数(越用越知哪条常用)',
  SOURCE         VARCHAR(50) DEFAULT 'manual' COMMENT 'manual/auto_seed/feedback',
  ISDELETED      TINYINT DEFAULT 0 COMMENT '逻辑删除',
  CREATEID       VARCHAR(64) DEFAULT NULL COMMENT '创建人ID',
  CREATER        VARCHAR(64) DEFAULT NULL COMMENT '创建人姓名',
  MODIFYID       VARCHAR(64) DEFAULT NULL COMMENT '修改人ID',
  MODIFER        VARCHAR(64) DEFAULT NULL COMMENT '修改人姓名',
  CREATETIME     DATETIME DEFAULT NULL COMMENT '创建时间',
  MODIFYTIME     DATETIME DEFAULT NULL COMMENT '修改时间',
  PRIMARY KEY (ID),
  KEY idx_type_asset (MEMORYTYPE, ASSETTYPE, ISDELETED),
  KEY idx_scene (SCENE_CODES(64)),
  KEY idx_tags (TAGS(100)),
  KEY idx_priority (PRIORITY, HITCOUNT)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI 统一记忆中枢(rule/example/pitfall/glossary)';

-- -----------------------------------------------------------
-- 2. 建表: tss_ai_feedback(反馈回流表)
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS tss_ai_feedback (
  ID              VARCHAR(36) NOT NULL COMMENT '主键',
  SESSIONID       VARCHAR(36) DEFAULT NULL COMMENT '关联会话ID',
  SCENE_CODE      VARCHAR(32) DEFAULT NULL COMMENT '场景编码(assistant/aidev/wizard/sfc/...)',
  ASSETTYPE       VARCHAR(20) DEFAULT NULL COMMENT '资产类型(同 tss_ai_memory)',
  USERID          VARCHAR(64) DEFAULT NULL COMMENT '用户ID',
  USERNAME        VARCHAR(64) DEFAULT NULL COMMENT '用户姓名',
  FEEDBACK_TYPE   VARCHAR(20) NOT NULL COMMENT 'thumbs_up/thumbs_down/edited/adopted',
  USER_REQUEST    TEXT DEFAULT NULL COMMENT '原始用户请求(prompt 摘要)',
  ORIGINAL_OUTPUT TEXT DEFAULT NULL COMMENT 'AI 原版输出',
  FINAL_OUTPUT    TEXT DEFAULT NULL COMMENT '用户最终采用版本',
  DIFF_TEXT       TEXT DEFAULT NULL COMMENT '自动计算的差异',
  ISSUE_TAGS      VARCHAR(500) DEFAULT NULL COMMENT '问题标签(naming/syntax/logic/missing_field/permission/...)',
  QUALITY_SCORE   INT DEFAULT NULL COMMENT '用户打分 1-5',
  COMMENT         TEXT DEFAULT NULL COMMENT '用户备注',
  PROMOTED        TINYINT DEFAULT 0 COMMENT '是否已提升为 example(0=未提升 1=已提升)',
  CREATETIME      DATETIME DEFAULT NULL COMMENT '反馈时间',
  PRIMARY KEY (ID),
  KEY idx_session (SESSIONID),
  KEY idx_scene_type (SCENE_CODE, FEEDBACK_TYPE),
  KEY idx_promoted (PROMOTED, CREATETIME)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='AI 反馈回流表(用户采纳/修正/打分)';

-- -----------------------------------------------------------
-- 3. ORM 资源注册: TBS_AI_MEMORY + VSS_AI_MEMORY
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_ai_memory_001', 'TBS_AI_MEMORY', 'tss_ai_memory', 'TABLE', NULL, 'TSS_AI_MEMORY', 'AI 统一记忆表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_ai_memory_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_ai_memory_001', 'VSS_AI_MEMORY', 'tss_ai_memory', 'DATAVIEW', 'tbs_ai_memory_001', 'A', 'AI 统一记忆视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_ai_memory_001');

-- 3.1 TBS_AI_MEMORY 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_id', 'tbs_ai_memory_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_type', 'tbs_ai_memory_001', NULL, 'MEMORYTYPE', 'varchar', 0, 0, 20, '记忆类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_type');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_asset', 'tbs_ai_memory_001', NULL, 'ASSETTYPE', 'varchar', 0, 0, 20, '资产类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_asset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_title', 'tbs_ai_memory_001', NULL, 'TITLE', 'varchar', 0, 0, 200, '标题'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_title');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_content', 'tbs_ai_memory_001', NULL, 'CONTENT', 'text', 0, 1, '主体内容'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_content');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_wrong', 'tbs_ai_memory_001', NULL, 'WRONG_CONTENT', 'text', 0, 1, '错误示例(pitfall)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_wrong');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_fix', 'tbs_ai_memory_001', NULL, 'FIX_STRATEGY', 'text', 0, 1, '修正方案(pitfall)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_fix');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_tags', 'tbs_ai_memory_001', NULL, 'TAGS', 'varchar', 0, 1, 500, '关键词标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_tags');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_scenes', 'tbs_ai_memory_001', NULL, 'SCENE_CODES', 'varchar', 0, 1, 500, '关联场景'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_scenes');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_steps', 'tbs_ai_memory_001', NULL, 'WIZARD_STEPS', 'varchar', 0, 1, 100, '关联向导步骤'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_steps');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_priority', 'tbs_ai_memory_001', NULL, 'PRIORITY', 'int', 0, 1, '优先级'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_priority');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_score', 'tbs_ai_memory_001', NULL, 'QUALITY_SCORE', 'int', 0, 1, '评分'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_score');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_hit', 'tbs_ai_memory_001', NULL, 'HITCOUNT', 'int', 0, 1, '命中次数'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_hit');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tam_source', 'tbs_ai_memory_001', NULL, 'SOURCE', 'varchar', 0, 1, 50, '来源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_source');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_isdeleted', 'tbs_ai_memory_001', NULL, 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_isdeleted');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_ctime', 'tbs_ai_memory_001', NULL, 'CREATETIME', 'datetime', 0, 1, '创建时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_ctime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tam_mtime', 'tbs_ai_memory_001', NULL, 'MODIFYTIME', 'datetime', 0, 1, '修改时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tam_mtime');

-- 3.2 VSS_AI_MEMORY 字段(REFFIELDID 指向 TBS)
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_id', 'vss_ai_memory_001', 'rf_tam_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_type', 'vss_ai_memory_001', 'rf_tam_type', 'MEMORYTYPE', 'varchar', 0, 0, 20, '记忆类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_type');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_asset', 'vss_ai_memory_001', 'rf_tam_asset', 'ASSETTYPE', 'varchar', 0, 0, 20, '资产类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_asset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_title', 'vss_ai_memory_001', 'rf_tam_title', 'TITLE', 'varchar', 0, 0, 200, '标题'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_title');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_content', 'vss_ai_memory_001', 'rf_tam_content', 'CONTENT', 'text', 0, 1, '主体内容'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_content');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_wrong', 'vss_ai_memory_001', 'rf_tam_wrong', 'WRONG_CONTENT', 'text', 0, 1, '错误示例'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_wrong');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_fix', 'vss_ai_memory_001', 'rf_tam_fix', 'FIX_STRATEGY', 'text', 0, 1, '修正方案'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_fix');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_tags', 'vss_ai_memory_001', 'rf_tam_tags', 'TAGS', 'varchar', 0, 1, 500, '关键词标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_tags');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_scenes', 'vss_ai_memory_001', 'rf_tam_scenes', 'SCENE_CODES', 'varchar', 0, 1, 500, '关联场景'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_scenes');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_steps', 'vss_ai_memory_001', 'rf_tam_steps', 'WIZARD_STEPS', 'varchar', 0, 1, 100, '关联向导步骤'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_steps');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_priority', 'vss_ai_memory_001', 'rf_tam_priority', 'PRIORITY', 'int', 0, 1, '优先级'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_priority');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_score', 'vss_ai_memory_001', 'rf_tam_score', 'QUALITY_SCORE', 'int', 0, 1, '评分'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_score');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_hit', 'vss_ai_memory_001', 'rf_tam_hit', 'HITCOUNT', 'int', 0, 1, '命中次数'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_hit');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vam_source', 'vss_ai_memory_001', 'rf_tam_source', 'SOURCE', 'varchar', 0, 1, 50, '来源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_source');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_isdeleted', 'vss_ai_memory_001', 'rf_tam_isdeleted', 'ISDELETED', 'int', 0, 1, '逻辑删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_isdeleted');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_ctime', 'vss_ai_memory_001', 'rf_tam_ctime', 'CREATETIME', 'datetime', 0, 1, '创建时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_ctime');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vam_mtime', 'vss_ai_memory_001', 'rf_tam_mtime', 'MODIFYTIME', 'datetime', 0, 1, '修改时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vam_mtime');

-- 3.3 过滤器: F00/F01/F02
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, REMARK)
SELECT 'rf_am_f00', 'vss_ai_memory_001', 'F00', 'A.ID=@ID', '按ID查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_am_f00');
-- F01 列表查询(支持 MEMORYTYPE/ASSETTYPE 简单过滤)
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_am_f01', 'vss_ai_memory_001', 'F01',
'1=1 AND A.ISDELETED=0
#if("$!{MEMORYTYPE}"!="")
AND A.MEMORYTYPE=@MEMORYTYPE
#end
#if("$!{ASSETTYPE}"!="")
AND A.ASSETTYPE=@ASSETTYPE
#end
#if("$!{KEYWORD}"!="")
AND (A.TITLE LIKE CONCAT(''%'',@KEYWORD,''%'') OR A.TAGS LIKE CONCAT(''%'',@KEYWORD,''%'') OR A.CONTENT LIKE CONCAT(''%'',@KEYWORD,''%''))
#end',
'PRIORITY DESC, HITCOUNT DESC, CREATETIME DESC',
'记忆列表(按类型/资产/关键词过滤)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_am_f01');

-- 3.4 UI 配置
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_idx', 'vss_ai_memory_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_idx');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_type', 'vss_ai_memory_001', 'rf_vam_type', '类型', 1, 1, 1, 'select', 'rule:规则,example:示例,pitfall:反模式,glossary:术语'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_type');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_asset', 'vss_ai_memory_001', 'rf_vam_asset', '资产', 2, 2, 2, 'select', 'sfc:SFC组件,sql:SQL模板,csharp:C#脚本,metadata:元数据,wizard:向导,general:通用'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_asset');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_title', 'vss_ai_memory_001', 'rf_vam_title', '标题', 3, 3, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_title');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_content', 'vss_ai_memory_001', 'rf_vam_content', '内容', 4, NULL, 4, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_content');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_wrong', 'vss_ai_memory_001', 'rf_vam_wrong', '错误示例', NULL, NULL, 5, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_wrong');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_fix', 'vss_ai_memory_001', 'rf_vam_fix', '修正方案', NULL, NULL, 6, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_fix');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_tags', 'vss_ai_memory_001', 'rf_vam_tags', '标签', 5, NULL, 7, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_tags');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_scenes', 'vss_ai_memory_001', 'rf_vam_scenes', '关联场景', NULL, NULL, 8, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_scenes');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_steps', 'vss_ai_memory_001', 'rf_vam_steps', '向导步骤', NULL, NULL, 9, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_steps');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_priority', 'vss_ai_memory_001', 'rf_vam_priority', '优先级', 6, NULL, 10, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_priority');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_score', 'vss_ai_memory_001', 'rf_vam_score', '评分', 7, NULL, 11, 'number', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_score');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_hit', 'vss_ai_memory_001', 'rf_vam_hit', '命中次数', 8, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_hit');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_source', 'vss_ai_memory_001', 'rf_vam_source', '来源', 9, NULL, 12, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_source');
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_am_ctime', 'vss_ai_memory_001', 'rf_vam_ctime', '创建时间', 10, NULL, NULL, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID='uipc_am_ctime');

-- -----------------------------------------------------------
-- 4. ORM 资源注册: TBS_AI_FEEDBACK + VSS_AI_FEEDBACK
-- -----------------------------------------------------------
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'tbs_ai_fb_001', 'TBS_AI_FEEDBACK', 'tss_ai_feedback', 'TABLE', NULL, 'TSS_AI_FEEDBACK', 'AI 反馈回流表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='tbs_ai_fb_001');
INSERT INTO tss_resource (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID, RESOURCEANAME, COMMENTS)
SELECT 'vss_ai_fb_001', 'VSS_AI_FEEDBACK', 'tss_ai_feedback', 'DATAVIEW', 'tbs_ai_fb_001', 'A', 'AI 反馈回流视图'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resource WHERE ID='vss_ai_fb_001');

-- 4.1 TBS_AI_FEEDBACK 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_id', 'tbs_ai_fb_001', NULL, 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_session', 'tbs_ai_fb_001', NULL, 'SESSIONID', 'varchar', 0, 1, 36, '关联会话'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_session');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_scene', 'tbs_ai_fb_001', NULL, 'SCENE_CODE', 'varchar', 0, 1, 32, '场景编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_scene');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_asset', 'tbs_ai_fb_001', NULL, 'ASSETTYPE', 'varchar', 0, 1, 20, '资产类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_asset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_uid', 'tbs_ai_fb_001', NULL, 'USERID', 'varchar', 0, 1, 64, '用户ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_uid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_uname', 'tbs_ai_fb_001', NULL, 'USERNAME', 'varchar', 0, 1, 64, '用户姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_uname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_type', 'tbs_ai_fb_001', NULL, 'FEEDBACK_TYPE', 'varchar', 0, 0, 20, '反馈类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_type');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_req', 'tbs_ai_fb_001', NULL, 'USER_REQUEST', 'text', 0, 1, '用户请求'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_req');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_orig', 'tbs_ai_fb_001', NULL, 'ORIGINAL_OUTPUT', 'text', 0, 1, 'AI 原版'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_orig');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_final', 'tbs_ai_fb_001', NULL, 'FINAL_OUTPUT', 'text', 0, 1, '最终采用版'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_final');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_diff', 'tbs_ai_fb_001', NULL, 'DIFF_TEXT', 'text', 0, 1, '差异文本'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_diff');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_tfb_tags', 'tbs_ai_fb_001', NULL, 'ISSUE_TAGS', 'varchar', 0, 1, 500, '问题标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_tags');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_score', 'tbs_ai_fb_001', NULL, 'QUALITY_SCORE', 'int', 0, 1, '评分'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_score');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_comment', 'tbs_ai_fb_001', NULL, 'COMMENT', 'text', 0, 1, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_comment');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_promoted', 'tbs_ai_fb_001', NULL, 'PROMOTED', 'int', 0, 1, '已提升为示例'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_promoted');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_tfb_ctime', 'tbs_ai_fb_001', NULL, 'CREATETIME', 'datetime', 0, 1, '反馈时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_tfb_ctime');

-- 4.2 VSS_AI_FEEDBACK 字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, KEYGENTYPE, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_id', 'vss_ai_fb_001', 'rf_tfb_id', 'ID', 'varchar', 1, 'GUID', 0, 36, '主键'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_id');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_session', 'vss_ai_fb_001', 'rf_tfb_session', 'SESSIONID', 'varchar', 0, 1, 36, '关联会话'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_session');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_scene', 'vss_ai_fb_001', 'rf_tfb_scene', 'SCENE_CODE', 'varchar', 0, 1, 32, '场景编码'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_scene');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_asset', 'vss_ai_fb_001', 'rf_tfb_asset', 'ASSETTYPE', 'varchar', 0, 1, 20, '资产类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_asset');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_uid', 'vss_ai_fb_001', 'rf_tfb_uid', 'USERID', 'varchar', 0, 1, 64, '用户ID'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_uid');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_uname', 'vss_ai_fb_001', 'rf_tfb_uname', 'USERNAME', 'varchar', 0, 1, 64, '用户姓名'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_uname');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_type', 'vss_ai_fb_001', 'rf_tfb_type', 'FEEDBACK_TYPE', 'varchar', 0, 0, 20, '反馈类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_type');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_orig', 'vss_ai_fb_001', 'rf_tfb_orig', 'ORIGINAL_OUTPUT', 'text', 0, 1, 'AI 原版'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_orig');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_final', 'vss_ai_fb_001', 'rf_tfb_final', 'FINAL_OUTPUT', 'text', 0, 1, '最终版'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_final');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_diff', 'vss_ai_fb_001', 'rf_tfb_diff', 'DIFF_TEXT', 'text', 0, 1, '差异'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_diff');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, FIELDLENGTH, COMMENTS)
SELECT 'rf_vfb_tags', 'vss_ai_fb_001', 'rf_tfb_tags', 'ISSUE_TAGS', 'varchar', 0, 1, 500, '问题标签'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_tags');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_score', 'vss_ai_fb_001', 'rf_tfb_score', 'QUALITY_SCORE', 'int', 0, 1, '评分'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_score');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_comment', 'vss_ai_fb_001', 'rf_tfb_comment', 'COMMENT', 'text', 0, 1, '备注'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_comment');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_promoted', 'vss_ai_fb_001', 'rf_tfb_promoted', 'PROMOTED', 'int', 0, 1, '已提升'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_promoted');
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDTYPE, ISKEY, NULLABLE, COMMENTS)
SELECT 'rf_vfb_ctime', 'vss_ai_fb_001', 'rf_tfb_ctime', 'CREATETIME', 'datetime', 0, 1, '反馈时间'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfield WHERE ID='rf_vfb_ctime');

-- 4.3 反馈表过滤器/UI(精简, 主要由后端 API 写入)
INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY, REMARK)
SELECT 'rf_fb_f01', 'vss_ai_fb_001', 'F01',
'1=1
#if("$!{SCENE_CODE}"!="")
AND A.SCENE_CODE=@SCENE_CODE
#end
#if("$!{FEEDBACK_TYPE}"!="")
AND A.FEEDBACK_TYPE=@FEEDBACK_TYPE
#end
#if("$!{PROMOTED}"!="")
AND A.PROMOTED=@PROMOTED
#end',
'CREATETIME DESC',
'反馈列表'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resfilter WHERE ID='rf_fb_f01');

-- -----------------------------------------------------------
-- 5. RS_M26 模块注册 (AI 记忆管理, 通用模块自举)
-- -----------------------------------------------------------
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m26_module_001', 'RS_M26', 'AI记忆管理'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M26');

SET @m26 = (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_M26');

INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m26_qry', @m26, 'QRY', 'vss_ai_memory_001', 1, '查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m26_qry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m26_qqry', @m26, 'QQRY', 'vss_ai_memory_001', 2, '高级查询数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m26_qqry');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m26_sel', @m26, 'SEL', 'vss_ai_memory_001', 3, '选择器数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m26_sel');
INSERT INTO tss_moudlepath (ID, MODULEID, PATHNAME, RESOURCEID, ENTRYNUM, REMARK)
SELECT 'mp_m26_main', @m26, 'MAIN', 'vss_ai_memory_001', 4, '主表数据源'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudlepath WHERE ID='mp_m26_main');

INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m26_a01', @m26, 'A01', 'query', 'query', 'QRY', 'F01', '查询', 'QQRY', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m26_a01');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m26_a02', @m26, 'A02', 'open', 'open', 'MAIN', 'F00', '打开', NULL, 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m26_a02');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m26_a04', @m26, 'A04', 'save', 'save', 'MAIN', NULL, '保存', 'MAIN', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m26_a04');
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, ACTIONCODE, APITYPE, PATHNAME, FILTERCODE, APINAME, APIPARAM, ENTRYNUM)
SELECT 'ma_m26_a07', @m26, 'A07', 'delete', 'delete', '', NULL, '删除', 'MAIN', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_m26_a07');

-- -----------------------------------------------------------
-- 6. 菜单 + 功能点 (通用模块路由 /g/RS_M26/main)
-- -----------------------------------------------------------
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, SORTCODE, ISHIDE)
SELECT 'func_rs_m26_001', 'RS_M26', 'AI记忆管理', '/g/RS_M26/main', '3e3c83ce2b3c475b82902478c89c27c0', 260, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M26');

SET @f26 = (SELECT ID FROM tss_func WHERE FUNCCODE='RS_M26');

INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m26_a01', @f26, 'A01', '查询', 'A01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m26_a01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m26_a04', @f26, 'A04', '编辑', 'A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m26_a04');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME, APICODE)
SELECT 'fp_rs_m26_a07', @f26, 'A07', '删除', 'A07'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE ID='fp_rs_m26_a07');

-- -----------------------------------------------------------
-- 7. 通用模块页面 + 按钮配置 (自举)
-- -----------------------------------------------------------
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, QUERYAPICODE, SORTNO, PAGECONFIG, ISDELETED)
SELECT 'mp_rs_m26_main', 'RS_M26', 'main', '记忆列表', 'list', 'standard', 'A01', 1, '{"defaultFormPageCode":"form"}', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m26_main');
INSERT INTO tss_module_page (ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, COMPONENTTYPE, OPENAPICODE, SAVEAPICODE, SORTNO, ISDELETED)
SELECT 'mp_rs_m26_form', 'RS_M26', 'form', '记忆编辑', 'form', 'standard', 'A02', 'A04', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_page WHERE ID='mp_rs_m26_form');

INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_main_add', 'mp_rs_m26_main', 'RS_M26', 'A04', '添加', 'crud', 'add', 'header', 'direct', 'RS_M26/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_main_add');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, PERMCODE, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_form_save', 'mp_rs_m26_form', 'RS_M26', 'A04', '保存', 'crud', 'save', 'footer', 'direct', 'RS_M26/A04', 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_form_save');
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_form_cancel', 'mp_rs_m26_form', 'RS_M26', NULL, '取消', 'crud', 'cancel', 'footer', 'direct', 2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_form_cancel');

-- -----------------------------------------------------------
-- 8. 纳入版本管理 (配置类对象)
-- -----------------------------------------------------------
INSERT INTO tss_dev_version_cfg (ID, RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS, ENABLED, ISDELETED)
SELECT 'dvc_aimemory', 'VSS_AI_MEMORY', 'aimemory', 'TITLE', 'TITLE', 100, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dev_version_cfg WHERE RESOURCENAME='VSS_AI_MEMORY');

-- -----------------------------------------------------------
-- 9. 声明式 AI 工具(search_memory/recall_examples/list_pitfalls)
--    所有场景的 LLM 都能用这三个工具主动查记忆
-- -----------------------------------------------------------
-- 9.1 SQL 模板: 全文检索记忆(按关键词 + 类型/资产过滤)
INSERT INTO tss_sql (SQLID, SQLCODE, SQLTYPE, SQLTXT, REMARK)
SELECT 'sql_ai_mem_search', 'SS_AI_MEM_SEARCH', 'mysql',
'SELECT ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY, TAGS, PRIORITY, HITCOUNT
FROM tss_ai_memory
WHERE ISDELETED=0
#if("$!{KEYWORD}"!="")
AND (TITLE LIKE CONCAT(''%'',@KEYWORD,''%'') OR TAGS LIKE CONCAT(''%'',@KEYWORD,''%'') OR CONTENT LIKE CONCAT(''%'',@KEYWORD,''%''))
#end
#if("$!{MEMORYTYPE}"!="")
AND MEMORYTYPE=@MEMORYTYPE
#end
#if("$!{ASSETTYPE}"!="")
AND (ASSETTYPE=@ASSETTYPE OR ASSETTYPE=''general'')
#end
ORDER BY PRIORITY DESC, HITCOUNT DESC
LIMIT 10',
'AI工具: 检索项目记忆库(按关键词/类型/资产)'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_sql WHERE SQLCODE='SS_AI_MEM_SEARCH');

-- 9.2 工具: search_memory(通用检索)
INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_mem_search', 'search_memory', 'dev',
'检索项目记忆/规则/示例/反模式(tss_ai_memory): 团队沉淀的铁律、好代码示例、曾经踩过的坑。生成代码前必须调用, 避免重复犯错。支持按关键词(KEYWORD)和类型(MEMORYTYPE=rule/example/pitfall/glossary)过滤。',
'{"type":"object","properties":{"keyword":{"type":"string","description":"关键词(可空, 不传则按类型返回高优先级)"},"memoryType":{"type":"string","description":"rule/example/pitfall/glossary, 可空"},"assetType":{"type":"string","description":"sfc/sql/csharp/metadata/wizard/general, 可空"}}}',
'sql', 'SS_AI_MEM_SEARCH', 10, 1, 'AI记忆检索(主入口)', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='search_memory');

-- 9.3 工具: recall_examples(召回示例代码)
INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_mem_examples', 'recall_examples', 'dev',
'召回同类型高质量代码示例(MEMORYTYPE=example): 用户验收过的代码产物, 按相关度排序。写代码前调用一次, 风格/结构更贴近团队习惯。',
'{"type":"object","properties":{"keyword":{"type":"string","description":"关键词"},"assetType":{"type":"string","description":"sfc/sql/csharp/metadata"}}}',
'sql', 'SS_AI_MEM_SEARCH', 5, 1, '召回示例代码', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='recall_examples');

-- 9.4 工具: list_pitfalls(列出相关反模式)
INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, SQLCODE, MAXROWS, ENABLED, REMARK, ISDELETED)
SELECT 'ait_mem_pitfalls', 'list_pitfalls', 'dev',
'列出当前场景相关的反模式/踩坑(MEMORYTYPE=pitfall): 错误示例 + 修正方案。写代码前调用, 避免重蹈覆辙。',
'{"type":"object","properties":{"keyword":{"type":"string","description":"关键词(如 SQL模板/SFC/字段名/NVelocity)"},"assetType":{"type":"string","description":"资产类型"}}}',
'sql', 'SS_AI_MEM_SEARCH', 10, 1, '列出相关反模式', 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_tool WHERE TOOLNAME='list_pitfalls');

-- 9.5 D0704 字典补三个新工具(search_memory/recall_examples/list_pitfalls)
ALTER TABLE tss_dict MODIFY COLUMN REMARK varchar(255) COLLATE utf8mb4_general_ci DEFAULT NULL COMMENT '说明';
UPDATE tss_dict SET REMARK = CONCAT(IFNULL(REMARK,''), ',search_memory:检索记忆库,recall_examples:召回示例,list_pitfalls:列出反模式')
WHERE DICTCODE='D0704' AND REMARK NOT LIKE '%search_memory%';

-- -----------------------------------------------------------
-- 10. 备注
-- -----------------------------------------------------------
-- 种子数据(铁律/示例/反模式)见 33_ai_memory_seed.sql
-- MemoryService 后端服务:
--   Services/AiMemory/MemoryService.cs (检索/注入/回流)
--   WizardStepOrchestrator.BuildStepSystemPrompt 改造调用 MemoryService
-- 反馈 UI:
--   p-admin/src/components/ai/blocks/FeedbackBlock.vue (👍/👎按钮 + 标签)
--   后端 RMAIDev/A11 记录反馈(POST /api/RMAIDev/feedback)
