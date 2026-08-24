-- ============================================================
-- AI 开发助理 — 状态字段字典化 (Chunk 6 修正)
-- 把 VSS_AIDEV_* 里写死的内联 k:v SELECTDATA 改为引用字典
-- 7 个字典: D0604 会话状态 / D0605 变更项状态 / D0606 升级状态 / D0607 会话类型 / D0608 变更项类别 / D0609 变更项操作类型 / D0610 快照对象类型
-- 注意: tss_dict 用 ISUSE(1启用), tss_dictitem 无删除标志
-- 幂等写法，可重复执行
-- ============================================================

-- ============================================================
-- 1. 创建字典 (tss_dict: ID, DICTCODE, DICTNAME, ISUSE)
-- ============================================================
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_session_status', 'D0604', 'AI会话状态', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0604');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_item_status', 'D0605', 'AI变更项状态', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0605');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_upgrade_status', 'D0606', 'AI升级状态', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0606');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_session_type', 'D0607', 'AI会话类型', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0607');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_item_category', 'D0608', 'AI变更项类别', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0608');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_item_action', 'D0609', 'AI变更项操作类型', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0609');

INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE)
SELECT 'dict_aidev_snapshot_type', 'D0610', 'AI快照对象类型', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0610');

-- ============================================================
-- 2. 创建字典项 (tss_dictitem: ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
-- ============================================================
-- D0604 AI会话状态
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiss_1', 'dict_aidev_session_status', 'DRAFT', '草稿', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_status' AND ITEMVALUE='DRAFT');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiss_2', 'dict_aidev_session_status', 'GENERATING', '生成中', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_status' AND ITEMVALUE='GENERATING');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiss_3', 'dict_aidev_session_status', 'REVIEWING', '审核中', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_status' AND ITEMVALUE='REVIEWING');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiss_4', 'dict_aidev_session_status', 'EXPORTED', '已导出', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_status' AND ITEMVALUE='EXPORTED');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiss_5', 'dict_aidev_session_status', 'ARCHIVED', '已归档', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_status' AND ITEMVALUE='ARCHIVED');

-- D0605 AI变更项状态
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aist_1', 'dict_aidev_item_status', 'DRAFT', '草稿', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_status' AND ITEMVALUE='DRAFT');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aist_2', 'dict_aidev_item_status', 'CONFIRMED', '已确认', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_status' AND ITEMVALUE='CONFIRMED');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aist_3', 'dict_aidev_item_status', 'REJECTED', '已拒绝', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_status' AND ITEMVALUE='REJECTED');

-- D0606 AI升级状态
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aius_1', 'dict_aidev_upgrade_status', 'PENDING', '待执行', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_upgrade_status' AND ITEMVALUE='PENDING');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aius_2', 'dict_aidev_upgrade_status', 'RUNNING', '执行中', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_upgrade_status' AND ITEMVALUE='RUNNING');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aius_3', 'dict_aidev_upgrade_status', 'SUCCESS', '成功', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_upgrade_status' AND ITEMVALUE='SUCCESS');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aius_4', 'dict_aidev_upgrade_status', 'FAILED', '失败', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_upgrade_status' AND ITEMVALUE='FAILED');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aius_5', 'dict_aidev_upgrade_status', 'ROLLEDBACK', '已回滚', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_upgrade_status' AND ITEMVALUE='ROLLEDBACK');

-- D0607 AI会话类型
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aistype_1', 'dict_aidev_session_type', 'NEW', '新增', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_type' AND ITEMVALUE='NEW');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aistype_2', 'dict_aidev_session_type', 'MODIFY', '修改', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_session_type' AND ITEMVALUE='MODIFY');

-- D0608 AI变更项类别
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_1', 'dict_aidev_item_category', 'physical_table', '物理表', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='physical_table');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_2', 'dict_aidev_item_category', 'dataview', '视图', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='dataview');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_3', 'dict_aidev_item_category', 'field', '字段', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='field');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_4', 'dict_aidev_item_category', 'ui', 'UI配置', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='ui');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_5', 'dict_aidev_item_category', 'dict', '字典', 5
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='dict');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_6', 'dict_aidev_item_category', 'filter', '过滤器', 6
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='filter');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_7', 'dict_aidev_item_category', 'module', '模块', 7
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='module');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_8', 'dict_aidev_item_category', 'api', '接口', 8
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='api');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_9', 'dict_aidev_item_category', 'menu', '菜单', 9
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='menu');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_10', 'dict_aidev_item_category', 'permission', '权限', 10
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='permission');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aicat_11', 'dict_aidev_item_category', 'billflow', '审批流', 11
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_category' AND ITEMVALUE='billflow');

-- D0609 AI变更项操作类型
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiact_1', 'dict_aidev_item_action', 'create', '新增', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_action' AND ITEMVALUE='create');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiact_2', 'dict_aidev_item_action', 'alter', '修改结构', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_action' AND ITEMVALUE='alter');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiact_3', 'dict_aidev_item_action', 'update', '更新数据', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_action' AND ITEMVALUE='update');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aiact_4', 'dict_aidev_item_action', 'delete', '删除', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_item_action' AND ITEMVALUE='delete');

-- D0610 AI快照对象类型
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aisnap_1', 'dict_aidev_snapshot_type', 'TABLE', '表', 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_snapshot_type' AND ITEMVALUE='TABLE');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aisnap_2', 'dict_aidev_snapshot_type', 'RESOURCE', '资源', 2
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_snapshot_type' AND ITEMVALUE='RESOURCE');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aisnap_3', 'dict_aidev_snapshot_type', 'RESFIELD', '字段', 3
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_snapshot_type' AND ITEMVALUE='RESFIELD');
INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM)
SELECT 'di_aisnap_4', 'dict_aidev_snapshot_type', 'FUNC', '菜单', 4
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE DICTID='dict_aidev_snapshot_type' AND ITEMVALUE='FUNC');

-- ============================================================
-- 3. 更新 resuipc SELECTDATA 引用字典名
-- ============================================================
-- VSS_AIDEV_SESSION 会话类型/状态
UPDATE tss_resuipc SET SELECTDATA='AI会话类型' WHERE RESOURCEID='vss_aidev_session_001' AND LABELNAME='会话类型';
UPDATE tss_resuipc SET SELECTDATA='AI会话状态' WHERE RESOURCEID='vss_aidev_session_001' AND LABELNAME='状态';

-- VSS_AIDEV_CHANGESET 来源
UPDATE tss_resuipc SET SELECTDATA='AI会话类型' WHERE RESOURCEID='vss_aidev_changeset_001' AND LABELNAME='来源';

-- VSS_AIDEV_CHANGEITEM 类别/操作类型/状态
UPDATE tss_resuipc SET SELECTDATA='AI变更项类别' WHERE RESOURCEID='vss_aidev_changeitem_001' AND LABELNAME='类别';
UPDATE tss_resuipc SET SELECTDATA='AI变更项操作类型' WHERE RESOURCEID='vss_aidev_changeitem_001' AND LABELNAME='操作类型';
UPDATE tss_resuipc SET SELECTDATA='AI变更项状态' WHERE RESOURCEID='vss_aidev_changeitem_001' AND LABELNAME='状态';

-- VSS_AIDEV_UPGRADE 会话类型/状态
UPDATE tss_resuipc SET SELECTDATA='AI会话类型' WHERE RESOURCEID='vss_aidev_upgrade_001' AND LABELNAME='会话类型';
UPDATE tss_resuipc SET SELECTDATA='AI升级状态' WHERE RESOURCEID='vss_aidev_upgrade_001' AND LABELNAME='状态';

-- VSS_AIDEV_UPGRADE_LOG 状态
UPDATE tss_resuipc SET SELECTDATA='AI升级状态' WHERE RESOURCEID='vss_aidev_upgrade_log_001' AND LABELNAME='状态';

-- VSS_AIDEV_UPGRADE_SNAPSHOT 对象类型
UPDATE tss_resuipc SET SELECTDATA='AI快照对象类型' WHERE RESOURCEID='vss_aidev_upgrade_snapshot_001' AND LABELNAME='对象类型';
