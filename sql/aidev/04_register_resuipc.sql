-- ============================================================
-- AI 开发助理模块 Chunk 1 — UI 配置 (tss_resuipc)
-- 为 6 张 VSS 视图配置列表/查询/编辑字段
-- 配置规则:
--   每张 VSS 配: 行号列(LISTSORT=0) + 关键名称列(LISTSORT=1) + 编码列(LISTSORT=2)
--                 + 状态列(LISTSORT=3, select) + 操作列(LISTSORT=99, action)
--   RS_MAIDEV 操作列: 编辑:edit,RS_MAIDEV/A03;删除:del,RS_MAIDEV/A04
--   RS_MAIDEVUPG 操作列: 编辑:edit,RS_MAIDEVUPG/A03;删除:del,RS_MAIDEVUPG/A04
--   session QUERYSORT: SESSIONNAME(input) + SESSIONCODE(input)
--   upgrade QUERYSORT: UPGRADECODE(input) + STATUS(select)
--   状态列 SELECTDATA 用 k:v 内联格式(k 是实际存储值)
-- ============================================================

-- ============================================================
-- 1. VSS_AIDEV_SESSION UI 配置
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_idx', 'vss_aidev_session_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_idx');

-- 会话编码
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_code', 'vss_aidev_session_001', 'rf_vaisess_code', '会话编码', 2, 2, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_code');

-- 会话名称
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_name', 'vss_aidev_session_001', 'rf_vaisess_name', '会话名称', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_name');

-- 会话类型
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_type', 'vss_aidev_session_001', 'rf_vaisess_type', '会话类型', NULL, NULL, 3, 'select', 'NEW:新增,MODIFY:修改'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_type');

-- 目标模块
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_tmod', 'vss_aidev_session_001', 'rf_vaisess_tmod', '目标模块', 4, NULL, 4, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_tmod');

-- 开发意图
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_intent', 'vss_aidev_session_001', 'rf_vaisess_intent', '开发意图', NULL, NULL, 5, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_intent');

-- 状态
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_status', 'vss_aidev_session_001', 'rf_vaisess_status', '状态', 3, 3, 6, 'select', 'DRAFT:草稿,GENERATING:生成中,REVIEWING:审核中,EXPORTED:已导出,ARCHIVED:已归档'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_status');

-- 创建人姓名
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aisess_cbyname', 'vss_aidev_session_001', 'rf_vaisess_cbyname', '创建人', 5, NULL, NULL, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_cbyname');

-- 创建时间
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aisess_ctime', 'vss_aidev_session_001', 'rf_vaisess_ctime', '创建时间', 6, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_ctime');

-- 关闭日期
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_close', 'vss_aidev_session_001', 'rf_vaisess_close', '关闭日期', NULL, NULL, 7, 'datepicker', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_close');

-- 备注
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aisess_remark', 'vss_aidev_session_001', 'rf_vaisess_remark', '备注', NULL, NULL, 8, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_remark');

-- 操作列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, SHOWLENGTH, ACTIONCODE)
SELECT 'uipc_aisess_action', 'vss_aidev_session_001', NULL, '操作', 99, NULL, NULL, 'action', NULL, '150', '编辑:edit,RS_MAIDEV/A03;删除:del,RS_MAIDEV/A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aisess_action');

-- ============================================================
-- 2. VSS_AIDEV_CHANGESET UI 配置
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_idx', 'vss_aidev_changeset_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_idx');

-- 变更包编码
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_code', 'vss_aidev_changeset_001', 'rf_vaics_code', '变更包编码', 2, 1, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_code');

-- 标题
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_title', 'vss_aidev_changeset_001', 'rf_vaics_title', '标题', 1, 2, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_title');

-- 会话ID
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_sid', 'vss_aidev_changeset_001', 'rf_vaics_sid', '会话ID', 3, NULL, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_sid');

-- 来源
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_src', 'vss_aidev_changeset_001', 'rf_vaics_src', '来源', 4, NULL, 4, 'select', 'NEW:新增,MODIFY:修改'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_src');

-- 意图
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_intent', 'vss_aidev_changeset_001', 'rf_vaics_intent', '意图', NULL, NULL, 5, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_intent');

-- 校验通过
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_vp', 'vss_aidev_changeset_001', 'rf_vaics_vp', '校验通过', 5, NULL, 6, 'checkbox', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_vp');

-- 校验报告
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aics_vr', 'vss_aidev_changeset_001', 'rf_vaics_vr', '校验报告', NULL, NULL, 7, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_vr');

-- 项数量
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aics_cnt', 'vss_aidev_changeset_001', 'rf_vaics_cnt', '项数量', 6, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_cnt');

-- 创建时间
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aics_ctime', 'vss_aidev_changeset_001', 'rf_vaics_ctime', '创建时间', 7, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aics_ctime');

-- ============================================================
-- 3. VSS_AIDEV_CHANGEITEM UI 配置
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_idx', 'vss_aidev_changeitem_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_idx');

-- 项序号
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aici_seq', 'vss_aidev_changeitem_001', 'rf_vaici_seq', '序号', 1, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_seq');

-- 类别
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_cat', 'vss_aidev_changeitem_001', 'rf_vaici_cat', '类别', 2, 1, 2, 'select', 'physical_table:物理表,dataview:视图,field:字段,ui:UI配置,dict:字典,filter:过滤器,module:模块,api:接口,menu:菜单,permission:权限,billflow:审批流'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_cat');

-- 操作
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_act', 'vss_aidev_changeitem_001', 'rf_vaici_act', '操作类型', 3, 2, 3, 'select', 'create:新增,alter:修改结构,update:更新数据,delete:删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_act');

-- 目标对象
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_tgt', 'vss_aidev_changeitem_001', 'rf_vaici_tgt', '目标对象', 4, 3, 4, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_tgt');

-- 工具
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_tool', 'vss_aidev_changeitem_001', 'rf_vaici_tool', '生成工具', 5, NULL, 5, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_tool');

-- SQL内容
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_sql', 'vss_aidev_changeitem_001', 'rf_vaici_sql', 'SQL内容', NULL, NULL, 6, 'code', '{language:"sql"}'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_sql');

-- 元数据
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_md', 'vss_aidev_changeitem_001', 'rf_vaici_md', '元数据', NULL, NULL, 7, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_md');

-- 理由
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_rat', 'vss_aidev_changeitem_001', 'rf_vaici_rat', '生成理由', NULL, NULL, 8, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_rat');

-- 警告
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_warn', 'vss_aidev_changeitem_001', 'rf_vaici_warn', '警告信息', NULL, NULL, 9, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_warn');

-- 依赖项
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_dep', 'vss_aidev_changeitem_001', 'rf_vaici_dep', '依赖项', NULL, NULL, 10, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_dep');

-- 项状态
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aici_status', 'vss_aidev_changeitem_001', 'rf_vaici_status', '状态', 6, 4, 11, 'select', 'DRAFT:草稿,CONFIRMED:已确认,REJECTED:已拒绝'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_status');

-- 确认人姓名
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aici_cbname', 'vss_aidev_changeitem_001', 'rf_vaici_cbname', '确认人', 7, NULL, NULL, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_cbname');

-- 确认时间
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aici_ctime', 'vss_aidev_changeitem_001', 'rf_vaici_ctime', '确认时间', 8, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_ctime');

-- 确认顺序
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aici_corder', 'vss_aidev_changeitem_001', 'rf_vaici_corder', '确认顺序', 9, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aici_corder');

-- ============================================================
-- 4. VSS_AIDEV_UPGRADE UI 配置 (RS_MAIDEVUPG 模块)
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_idx', 'vss_aidev_upgrade_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_idx');

-- 升级编码
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_code', 'vss_aidev_upgrade_001', 'rf_vaiupg_code', '升级编码', 1, 1, 1, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_code');

-- 会话编码
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_scode', 'vss_aidev_upgrade_001', 'rf_vaiupg_scode', '会话编码', 2, NULL, 2, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_scode');

-- 会话名称
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_sname', 'vss_aidev_upgrade_001', 'rf_vaiupg_sname', '会话名称', 3, 2, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_sname');

-- 会话类型
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_stype', 'vss_aidev_upgrade_001', 'rf_vaiupg_stype', '会话类型', 4, NULL, 4, 'select', 'NEW:新增,MODIFY:修改'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_stype');

-- 目标模块
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_tmod', 'vss_aidev_upgrade_001', 'rf_vaiupg_tmod', '目标模块', 5, NULL, 5, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_tmod');

-- 意图
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_intent', 'vss_aidev_upgrade_001', 'rf_vaiupg_intent', '意图', NULL, NULL, 6, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_intent');

-- 脚本内容
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_script', 'vss_aidev_upgrade_001', 'rf_vaiupg_script', '脚本内容', NULL, NULL, 7, 'code', '{language:"sql"}'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_script');

-- 脚本哈希
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupg_hash', 'vss_aidev_upgrade_001', 'rf_vaiupg_hash', '脚本哈希', 6, NULL, NULL, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_hash');

-- 项数量
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupg_cnt', 'vss_aidev_upgrade_001', 'rf_vaiupg_cnt', '项数量', 7, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_cnt');

-- 状态
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_status', 'vss_aidev_upgrade_001', 'rf_vaiupg_status', '状态', 8, 3, 8, 'select', 'PENDING:待执行,RUNNING:执行中,SUCCESS:成功,FAILED:失败,ROLLEDBACK:已回滚'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_status');

-- 执行人姓名
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupg_ebyname', 'vss_aidev_upgrade_001', 'rf_vaiupg_ebyname', '执行人', 9, NULL, NULL, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_ebyname');

-- 执行时间
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupg_etime', 'vss_aidev_upgrade_001', 'rf_vaiupg_etime', '执行时间', 10, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_etime');

-- 耗时
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupg_dur', 'vss_aidev_upgrade_001', 'rf_vaiupg_dur', '耗时(ms)', 11, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_dur');

-- 错误信息
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_err', 'vss_aidev_upgrade_001', 'rf_vaiupg_err', '错误信息', NULL, NULL, 9, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_err');

-- 回滚脚本
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupg_rb', 'vss_aidev_upgrade_001', 'rf_vaiupg_rb', '回滚脚本', NULL, NULL, 10, 'code', '{language:"sql"}'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_rb');

-- 操作列 (RS_MAIDEVUPG)
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, SHOWLENGTH, ACTIONCODE)
SELECT 'uipc_aiupg_action', 'vss_aidev_upgrade_001', NULL, '操作', 99, NULL, NULL, 'action', NULL, '150', '编辑:edit,RS_MAIDEVUPG/A03;删除:del,RS_MAIDEVUPG/A04'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupg_action');

-- ============================================================
-- 5. VSS_AIDEV_UPGRADE_LOG UI 配置 (子表，无操作列)
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_idx', 'vss_aidev_upgrade_log_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_idx');

-- 升级ID
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupgl_uid', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_uid', '升级ID', 1, NULL, 1, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_uid');

-- 变更项ID
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupgl_iid', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_iid', '变更项ID', 2, NULL, 2, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_iid');

-- 项类别
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_icat', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_icat', '项类别', 3, NULL, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_icat');

-- 项操作
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_iact', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_iact', '项操作', 4, NULL, 4, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_iact');

-- 项目标
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_itgt', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_itgt', '项目标', 5, NULL, 5, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_itgt');

-- SQL片段
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_sql', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_sql', 'SQL片段', NULL, NULL, 6, 'code', '{language:"sql"}'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_sql');

-- 状态
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_status', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_status', '状态', 6, NULL, 7, 'select', 'PENDING:待执行,RUNNING:执行中,SUCCESS:成功,FAILED:失败,ROLLEDBACK:已回滚'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_status');

-- 错误信息
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgl_err', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_err', '错误信息', 7, NULL, 8, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_err');

-- 影响行数
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupgl_rows', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_rows', '影响行数', 8, NULL, NULL, 'number', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_rows');

-- 执行时间
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupgl_etime', 'vss_aidev_upgrade_log_001', 'rf_vaiupgl_etime', '执行时间', 9, NULL, NULL, 'datepicker', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgl_etime');

-- ============================================================
-- 6. VSS_AIDEV_UPGRADE_SNAPSHOT UI 配置 (子表，无操作列)
-- ============================================================
-- 行号列
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgs_idx', 'vss_aidev_upgrade_snapshot_001', NULL, '#', 0, NULL, NULL, 'index', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_idx');

-- 升级ID
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA, NULLABLE, EDITABLE)
SELECT 'uipc_aiupgs_uid', 'vss_aidev_upgrade_snapshot_001', 'rf_vaiupgs_uid', '升级ID', 1, NULL, 1, 'text', NULL, 1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_uid');

-- 对象类型
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgs_otype', 'vss_aidev_upgrade_snapshot_001', 'rf_vaiupgs_otype', '对象类型', 2, NULL, 2, 'select', 'TABLE:表,RESOURCE:资源,RESFIELD:字段,FUNC:菜单'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_otype');

-- 对象名称
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgs_oname', 'vss_aidev_upgrade_snapshot_001', 'rf_vaiupgs_oname', '对象名称', 3, NULL, 3, 'text', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_oname');

-- 变更前快照
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgs_before', 'vss_aidev_upgrade_snapshot_001', 'rf_vaiupgs_before', '变更前', NULL, NULL, 4, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_before');

-- 变更后快照
INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, QUERYSORT, EDITSORT, EDITTYPE, SELECTDATA)
SELECT 'uipc_aiupgs_after', 'vss_aidev_upgrade_snapshot_001', 'rf_vaiupgs_after', '变更后', NULL, NULL, 5, 'textarea', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_resuipc WHERE ID = 'uipc_aiupgs_after');
