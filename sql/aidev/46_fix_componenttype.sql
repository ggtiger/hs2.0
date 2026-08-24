-- ============================================================
-- RS_FEE COMPONENTTYPE 修复(白屏根因) + pitfall 沉淀
-- 背景: 用户反馈"进去都看不到表单页面"。根因: RS_FEE 两页
--   COMPONENTTYPE=generic-module/generic-form(手工 demo SQL 写错),
--   registerGenericRoute(router/index.js:167) 白名单只认 standard/sfc,
--   页面被过滤 → 路由未注册 → 白屏。LIB_M01/LIB_M05 全是 standard。
-- 日期: 2026-07-20
-- ============================================================

UPDATE tss_module_page SET COMPONENTTYPE='standard' WHERE ID IN ('mp_fee_form','mp_fee_list');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_componenttype_whitelist', 'pitfall', 'wizard',
'tss_module_page.COMPONENTTYPE 只能填 standard 或 sfc, 不能填 generic-module/generic-form',
'【症状】模块页面路由没注册, 访问 /g/{MC}/{pageCode} 空白/404, 整个模块进不去。
【根因】registerGenericRoute(router/index.js:167) 过滤页面: COMPONENTTYPE 白名单只有 standard 和 sfc(sfc 还需 SFCMODULEPATH 非空)。填 generic-module/generic-form 会被过滤 → 页面不注册路由 → 白屏。
【铁律】
  COMPONENTTYPE=standard — 标准页面(列表 list / 表单 form / 报表 report), 由 generic-module/generic-form 组件按 PAGETYPE 自动渲染
  COMPONENTTYPE=sfc      — 在线 SFC 页面, 必须同时配 SFCMODULEPATH
【区分】PAGETYPE(list/form/select/report) 决定用哪个组件渲染; COMPONENTTYPE(standard/sfc) 决定代码来源(标准组件库 vs 在线 SFC)。generic-module/generic-form 是组件名不是 COMPONENTTYPE 值。
【反例】RS_FEE 两页 COMPONENTTYPE=generic-module/generic-form → 路由全没注册, 模块白屏。
【正例】LIB_M01/LIB_M05 全部 COMPONENTTYPE=standard。',
'COMPONENTTYPE,standard,sfc,路由白名单,registerGenericRoute,白屏,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_componenttype_whitelist');

-- 完成
