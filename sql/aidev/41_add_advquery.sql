-- ============================================================
-- 补 RS_FEE 缺失的 A03 advQuery 高级查询接口 + pitfall 补全
-- 背景: 标准业务模块接口五件套是 A01/A02/A03/A04/A07,
--       A03 = query + QRY + F02 + advQuery (高级查询),
--       RS_FEE 只配了 A01/A02/A04/A07/A09 缺 A03,
--       前端 rs-query-panel 展开"高级查询"时找不到接口。
-- 日期: 2026-07-20
-- ============================================================

-- --------------------------------------------------
-- 1. 补 A03 advQuery 接口
-- --------------------------------------------------
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, PATHNAME, FILTERCODE, ACTIONCODE, APIPARAM)
SELECT 'ma_fee_a03', (SELECT ID FROM tss_moudle WHERE MODULECODE='RS_FEE'),
'A03', 'query', 'QRY', 'F02', 'advQuery', NULL
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudleapi WHERE ID='ma_fee_a03');

-- --------------------------------------------------
-- 2. 更新 pitfall 记忆: ACTIONCODE 映射表补 advQuery + 五件套说明
-- --------------------------------------------------
UPDATE tss_ai_memory SET CONTENT =
'【症状】AI 生成的 moudleapi 行 ACTIONCODE 全是 NULL → 前端 store.js getApiRow/getApi 按 ACTIONCODE 匹配 query/open/save/delete/advQuery 找不到接口 → 按钮点了没反应/列表加载不出来/高级查询面板打不开/表单保存失败。
【根因】前端 BaseStore/Store03 不是按 APICODE(A01/A02/A04) 识别接口, 而是按 ACTIONCODE 匹配。APICODE 只是编码, ACTIONCODE 才是用途标识。
【标准业务模块接口五件套】(必配):
  A01 query QRY  F01  ACTIONCODE=query     (列表模糊查询)
  A02 open  MAIN F00  ACTIONCODE=open      (按 ID 打开单条)
  A03 query QRY  F02  ACTIONCODE=advQuery  (高级查询面板, 多条件组合)
  A04 save  MAIN      ACTIONCODE=save      (保存)
  A07 delete MAIN     ACTIONCODE=delete    (删除)
【ACTIONCODE 完整映射】(tss_moudleapi 全库主流值):
  APITYPE=query + FILTERCODE=F01 → ACTIONCODE=''query''      (列表模糊搜索)
  APITYPE=query + FILTERCODE=F02 → ACTIONCODE=''advQuery''   (高级查询)
  APITYPE=open                   → ACTIONCODE=''open''
  APITYPE=save                   → ACTIONCODE=''save''
  APITYPE=delete                 → ACTIONCODE=''delete''
  APITYPE=submit                 → ACTIONCODE=''submit''     (提交, A17)
  APITYPE=reSubmit               → ACTIONCODE=''reSubmit''   (撤销提交)
  APITYPE=check                  → ACTIONCODE=''check''      (审核, A12)
  APITYPE=reCheck                → ACTIONCODE=''reCheck''    (撤销审核, A13)
  APITYPE=verify                 → ACTIONCODE=''verify''     (审批, A14)
  APITYPE=reVerify               → ACTIONCODE=''reVerify''   (撤销审批, A15)
  APITYPE=sql/csharp/script      → ACTIONCODE=自定义动作码(如 ''checkScript''/''testTool''), 按钮 APICODE 关联用
【铁律】INSERT tss_moudleapi 必须同时填 APITYPE + ACTIONCODE, 都不许 NULL。ACTIONCODE 值必须与 APITYPE+FILTERCODE 匹配上表。
【反例】RS_FEE demo 最初 5 个接口 ACTIONCODE 全 NULL + 缺 A03 → 列表/高级查询/保存全废。
【正例】R02_M07 标准: A01 query/query, A02 open/open, A03 query/advQuery, A04 save/save, A05 delete/delete。',
MODIFYTIME=NOW()
WHERE ID='am_pitfall_actioncode_required';

-- 完成: 1 行 INSERT + 1 条记忆 UPDATE
