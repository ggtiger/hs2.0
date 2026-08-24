-- ============================================================
-- 下拉选项数据字典化 — 数据库升级
-- 内容: 7 个数据字典(tss_dict + tss_dictitem) + resuipc SELECTDATA 改字典名
-- 日期: 2026-07-17
-- 机制: SET_DICTS 按 DICTNAME 聚合(state.dicts[DICTNAME][value]=label),
--       HeyUI addDict 全局注册, resuipc SELECTDATA 写字典名(DICTNAME)即可
-- ============================================================

-- -----------------------------------------------------------
-- 1. 字典头
-- -----------------------------------------------------------
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0701', 'D0701', '版本对象类型', 1, '版本中心对象类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0701');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0702', 'D0702', '版本操作类型', 1, '版本快照操作类型'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0702');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0703', 'D0703', 'AI场景传输方式', 1, 'signalr/sse'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0703');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0704', 'D0704', 'AI工具集', 1, 'assistant/formfill/dev/sfc'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0704');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0705', 'D0705', 'AI场景上下文源', 1, 'none/formContext/sfcContext'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0705');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0706', 'D0706', 'AI工具执行类型', 1, 'sql/builtin'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0706');
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0707', 'D0707', '业务分类', 1, 'b01/r01/r02/s01'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0707');

-- -----------------------------------------------------------
-- 2. 字典项: 版本对象类型 (D0701)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_01', 'dict_d0701', 'SFC页面', 'sfc', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_02', 'dict_d0701', 'C#脚本', 'api_script', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_02');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_03', 'dict_d0701', 'SQL模板', 'sql', 3 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_03');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_04', 'dict_d0701', '页面配置', 'page', 4 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_04');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_05', 'dict_d0701', '按钮配置', 'button', 5 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_05');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_06', 'dict_d0701', '模块', 'module', 6 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_06');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_07', 'dict_d0701', '资源', 'resource', 7 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_07');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_08', 'dict_d0701', '字段', 'field', 8 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_08');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_09', 'dict_d0701', '过滤器', 'filter', 9 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_09');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_10', 'dict_d0701', 'UI配置', 'ui', 10 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_10');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_11', 'dict_d0701', '接口', 'api', 11 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_11');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_12', 'dict_d0701', 'AI场景', 'scene', 12 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_12');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_13', 'dict_d0701', 'AI工具', 'aitool', 13 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_13');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_14', 'dict_d0701', '业务模板', 'template', 14 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_14');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0701_15', 'dict_d0701', '代码资产', 'code', 15 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0701_15');

-- -----------------------------------------------------------
-- 3. 字典项: 版本操作类型 (D0702)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0702_01', 'dict_d0702', '新增', 'insert', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0702_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0702_02', 'dict_d0702', '修改', 'update', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0702_02');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0702_03', 'dict_d0702', '删除', 'delete', 3 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0702_03');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0702_04', 'dict_d0702', '回滚', 'rollback', 4 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0702_04');

-- -----------------------------------------------------------
-- 4. 字典项: AI场景传输方式 (D0703)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0703_01', 'dict_d0703', 'SignalR', 'signalr', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0703_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0703_02', 'dict_d0703', 'SSE', 'sse', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0703_02');

-- -----------------------------------------------------------
-- 5. 字典项: AI工具集 (D0704)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0704_01', 'dict_d0704', 'assistant(通用助理)', 'assistant', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0704_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0704_02', 'dict_d0704', 'formfill(表单填报)', 'formfill', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0704_02');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0704_03', 'dict_d0704', 'dev(AI开发)', 'dev', 3 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0704_03');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0704_04', 'dict_d0704', 'sfc(SFC助手)', 'sfc', 4 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0704_04');

-- -----------------------------------------------------------
-- 6. 字典项: AI场景上下文源 (D0705)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0705_01', 'dict_d0705', '无', 'none', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0705_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0705_02', 'dict_d0705', '表单上下文', 'formContext', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0705_02');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0705_03', 'dict_d0705', 'SFC上下文', 'sfcContext', 3 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0705_03');

-- -----------------------------------------------------------
-- 7. 字典项: AI工具执行类型 (D0706)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0706_01', 'dict_d0706', 'SQL查询(只读)', 'sql', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0706_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0706_02', 'dict_d0706', '代码内置', 'builtin', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0706_02');

-- -----------------------------------------------------------
-- 8. 字典项: 业务分类 (D0707)
-- -----------------------------------------------------------
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0707_01', 'dict_d0707', '基础数据', 'b01', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0707_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0707_02', 'dict_d0707', '报告/检验', 'r01', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0707_02');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0707_03', 'dict_d0707', '记录/报表', 'r02', 3 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0707_03');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0707_04', 'dict_d0707', '系统管理', 's01', 4 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0707_04');

-- -----------------------------------------------------------
-- 9. resuipc SELECTDATA: k:v 内联 → 字典名(DICTNAME)
-- -----------------------------------------------------------
UPDATE tss_resuipc SET SELECTDATA='版本对象类型' WHERE ID='uipc_dv_objtype';
UPDATE tss_resuipc SET SELECTDATA='版本操作类型' WHERE ID='uipc_dv_optype';
UPDATE tss_resuipc SET SELECTDATA='AI场景传输方式' WHERE ID='uipc_sc_transport';
UPDATE tss_resuipc SET SELECTDATA='AI工具集' WHERE ID='uipc_sc_toolset';
UPDATE tss_resuipc SET SELECTDATA='AI场景上下文源' WHERE ID='uipc_sc_ctxsrc';
UPDATE tss_resuipc SET SELECTDATA='AI工具集' WHERE ID='uipc_at_set';
UPDATE tss_resuipc SET SELECTDATA='AI工具执行类型' WHERE ID='uipc_at_exectype';
UPDATE tss_resuipc SET SELECTDATA='业务分类' WHERE ID='uipc_mt_category';

-- -----------------------------------------------------------
-- 10. 补充: 字段占宽 (D0708, uiSetFull 占宽下拉)
-- -----------------------------------------------------------
INSERT INTO tss_dict (ID, DICTCODE, DICTNAME, ISUSE, REMARK)
SELECT 'dict_d0708', 'D0708', '字段占宽', 1, 'resuipc COLSPAN'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dict WHERE DICTCODE='D0708');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0708_01', 'dict_d0708', '按列宽(与相邻字段同行)', '1', 1 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0708_01');
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM) SELECT 'di_d0708_02', 'dict_d0708', '整行(独占一行)', '2', 2 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_dictitem WHERE ID='di_d0708_02');
