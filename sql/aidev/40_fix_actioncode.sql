-- ============================================================
-- 修复 RS_FEE 的 ACTIONCODE 空值 + 永久 pitfall
-- 背景: RS_FEE demo 的 5 个 moudleapi 行 ACTIONCODE 全是 NULL。
--       ACTIONCODE 是前端识别接口用途的关键字段(store.js 的
--       getApiRow/getApi 按 ACTIONCODE 匹配 query/open/save/delete),
--       空值导致前端按类型找不到对应接口, 按钮点了没反应。
-- 日期: 2026-07-19
-- ============================================================

-- --------------------------------------------------
-- 1. 修正 RS_FEE 的 5 个 moudleapi ACTIONCODE
-- --------------------------------------------------
UPDATE tss_moudleapi SET ACTIONCODE='query'  WHERE ID='ma_fee_a01';
UPDATE tss_moudleapi SET ACTIONCODE='open'   WHERE ID='ma_fee_a02';
UPDATE tss_moudleapi SET ACTIONCODE='save'   WHERE ID='ma_fee_a04';
UPDATE tss_moudleapi SET ACTIONCODE='delete' WHERE ID='ma_fee_a07';
UPDATE tss_moudleapi SET ACTIONCODE='query'  WHERE ID='ma_fee_a09';

-- --------------------------------------------------
-- 2. 永久 pitfall: ACTIONCODE 必须按 APITYPE 标准值填充
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_actioncode_required', 'pitfall', 'metadata',
'moudleapi 的 ACTIONCODE 必须按 APITYPE 对应的标准值填充, 不能为 NULL',
'【症状】AI 生成的 moudleapi 行 ACTIONCODE 全是 NULL → 前端 store.js getApiRow/getApi 按 ACTIONCODE 匹配 query/open/save/delete 找不到接口 → 按钮点了没反应/列表加载不出来/表单保存失败。
【根因】前端 BaseStore/Store03 不是按 APICODE(A01/A02/A04) 识别接口, 而是按 ACTIONCODE 匹配。APICODE 只是编码, ACTIONCODE 才是用途标识。
【标准映射】(tss_moudleapi 全库 44+42+31+6 条主流通用值):
  APITYPE=query     → ACTIONCODE=''query''       (列表查询, A01/A09)
  APITYPE=query     → ACTIONCODE=''advQuery''    (高级查询, A03 列表)
  APITYPE=open      → ACTIONCODE=''open''        (打开单条, A02)
  APITYPE=save      → ACTIONCODE=''save''        (保存, A04)
  APITYPE=delete    → ACTIONCODE=''delete''      (删除, A07)
  APITYPE=submit    → ACTIONCODE=''submit''      (提交, A17)
  APITYPE=reSubmit  → ACTIONCODE=''reSubmit''    (撤销提交)
  APITYPE=check     → ACTIONCODE=''check''       (审核, A12)
  APITYPE=reCheck   → ACTIONCODE=''reCheck''     (撤销审核, A13)
  APITYPE=verify    → ACTIONCODE=''verify''      (审批, A14)
  APITYPE=reVerify  → ACTIONCODE=''reVerify''    (撤销审批, A15)
  APITYPE=sql/csharp/script → ACTIONCODE=自定义动作码(如 ''checkScript''/''testTool''), 用于按钮 APICODE 关联
  APITYPE=NULL 且 APICODE=A00 → ACTIONCODE=''add'' (新建单)
【铁律】INSERT tss_moudleapi 必须同时填 APITYPE + ACTIONCODE, 都不许 NULL。ACTIONCODE 值必须与 APITYPE 匹配上表。
【反例】RS_FEE demo 5 个接口 ACTIONCODE 全 NULL → 前端列表加载失败。
【正例】全库统计: A01/query→''query'' 44 条, A02/open→''open'' 42 条, A04/save→''save'' 31 条, A07/delete→''delete'' 6 条。',
'moudleapi,ACTIONCODE,APITYPE,query,open,save,delete,标准值,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_actioncode_required');

-- 完成: 5 行 UPDATE + 1 条 pitfall
