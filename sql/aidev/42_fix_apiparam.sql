-- ============================================================
-- 修复 RS_FEE 的 APIPARAM 空值 + pitfall 补全
-- 背景: 接口的 APIPARAM(接口参数)决定前端把数据放在哪个 DataTable 路径传输。
--       query/advQuery 接口 APIPARAM=QQRY(查询条件路径),
--       save/delete 接口 APIPARAM=MAIN(主子表路径, 有子表则 MAIN,DTSA,...)。
--       RS_FEE 全部 APIPARAM=NULL → 前端 query 时查询条件没地方放。
-- 日期: 2026-07-20
-- ============================================================

-- --------------------------------------------------
-- 1. 修正 RS_FEE 的 APIPARAM
-- --------------------------------------------------
UPDATE tss_moudleapi SET APIPARAM='QQRY' WHERE ID='ma_fee_a01';
UPDATE tss_moudleapi SET APIPARAM='QQRY' WHERE ID='ma_fee_a03';
UPDATE tss_moudleapi SET APIPARAM='MAIN' WHERE ID='ma_fee_a04';
UPDATE tss_moudleapi SET APIPARAM='MAIN' WHERE ID='ma_fee_a07';

-- --------------------------------------------------
-- 2. pitfall 记忆: APIPARAM 必须按接口类型填充
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_apiparam_required', 'pitfall', 'metadata',
'moudleapi 的 APIPARAM(接口参数)必须按接口类型填充: query/advQuery=QQRY, save/delete=MAIN',
'【症状】AI 生成的 moudleapi 行 APIPARAM 全是 NULL → 前端查询时查询条件不知道放哪个 DataTable 路径, 高级查询面板取不到值, 保存时不知道提交哪些路径的数据。
【根因】前端 Store03 的 query/save action 从 moudleapi.APIPARAM 读取路径名, 按路径从 DataTable 取数据打包 XML 提交。APIPARAM=NULL 时取不到路径 → 查询条件/保存数据为空。
【标准映射】(全库主流值):
  ACTIONCODE=query     → APIPARAM=''QQRY''           (列表模糊查询, 查询条件在 QQRY 路径)
  ACTIONCODE=advQuery  → APIPARAM=''QQRY''           (高级查询, 查询条件也在 QQRY 路径)
  ACTIONCODE=open      → APIPARAM=NULL               (打开单条, 只传 ID 不需要路径)
  ACTIONCODE=save      → APIPARAM=''MAIN''           (单表保存) 或 ''MAIN,DTSA,DTSB''(主子表保存, 按实际子表)
  ACTIONCODE=delete    → APIPARAM=''MAIN''           (单表删除) 或 ''MAIN,DTSA''(主子表级联删除)
  ACTIONCODE=submit/check/verify → APIPARAM=''MAIN'' (审批流操作主表)
【与 PATHNAME 的区别】PATHNAME 是查询时从哪个数据源读(QRY=查询视图, MAIN=主表), APIPARAM 是前端传参放在哪个 DataTable 路径。query 类接口 PATHNAME=QRY + APIPARAM=QQRY 是固定搭配。
【反例】RS_FEE demo 最初 APIPARAM 全 NULL → 前端查询面板取不到查询条件。
【正例】R02_M07: A01/A03 APIPARAM=QQRY, A04 APIPARAM=MAIN,DTSA,DTS, A05 APIPARAM=MAIN,DTSA,DTS。',
'moudleapi,APIPARAM,QQRY,MAIN,接口参数,查询路径,保存路径,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_apiparam_required');

-- 完成: 4 行 UPDATE + 1 条 pitfall
