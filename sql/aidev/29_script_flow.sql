-- 29_script_flow.sql
-- 内容: APITYPE=script 接口编排支持
-- 1. ALTER TABLE tss_moudleapi MODIFY APIPARAM TEXT（varchar(128)→TEXT，步骤 JSON 可能很长）
-- 2. D0701 字典补 script 类型项

-- 1. 扩宽 APIPARAM 字段（步骤 JSON 可能很长，varchar(128) 不够）
ALTER TABLE tss_moudleapi MODIFY APIPARAM TEXT;

-- 2. D0701 版本对象类型字典补 script 类型项
INSERT INTO tss_dictitem (ID, DICTID, ITEMNAME, ITEMVALUE, ENTRYNUM)
SELECT 'di_d0701_script', 'dict_d0701', '编排接口', 'script', 8
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM tss_dictitem WHERE DICTID='dict_d0701' AND ITEMVALUE='script'
);
