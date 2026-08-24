-- ============================================================
-- RS_FEE 添加按钮按 LIB_M05 标准修复 + pitfall 沉淀
-- 背景: 用户反馈"添加还是配置不对, 参考部门管理 LIB_M05"。
--   LIB_M05 添加按钮: EXTPARAM={"action":"openForm","openMode":"add","formPageCode":"add"}
--   RS_FEE 添加按钮:  EXTPARAM={"formPageCode":"add"}  ← 缺 action+openMode
--   且 main 页 PAGECONFIG 缺 defaultFormPageCode(双击行打不开表单)
--   generic-module 的表单打开是内嵌 rs-modal+generic-form, 不是路由跳转
-- 日期: 2026-07-20
-- ============================================================

-- 1. 添加按钮 EXTPARAM 三要素补齐
UPDATE tss_module_button SET EXTPARAM='{"action":"openForm","openMode":"add","formPageCode":"add"}'
WHERE ID='mb_fee_add';

-- 2. main 页 PAGECONFIG 补 defaultFormPageCode
UPDATE tss_module_page
SET PAGECONFIG='{"dynamicQuery":true,"EXTENDJS":"@/modules/RS_FEE/main.js","defaultFormPageCode":"add"}'
WHERE ID='mp_fee_list';

-- 3. pitfall 记忆(已入库则跳过)
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_add_button_extparam', 'pitfall', 'wizard',
'添加/编辑按钮 EXTPARAM 三要素: action=openForm + openMode + formPageCode; main页必须配 defaultFormPageCode',
'【症状】列表页点添加按钮没反应/表单不弹出/双击行打不开表单。
【根因】generic-module 的表单打开机制是内嵌 rs-modal + generic-form($refs.madd.show()), 不是路由跳转。handleBtnAction 按 ext.action 分发, doOpenForm 依赖 formPageCode 定位表单页配置; 双击行(clickRow) 走 defaultFormPageCode 找默认表单页。
【添加按钮标准配置】(对照 LIB_M05 部门管理):
  BTNTYPE=custom, BTNCODE=add, BTNAREA=header, INTERACTTYPE=direct,
  PERMCODE={MODULECODE}/A04, ICON=h-icon-plus, COLOR=primary,
  EXTPARAM={"action":"openForm","openMode":"add","formPageCode":"add"}
【编辑按钮标准配置】(row 区, 需要时才配):
  BTNCODE=edit, BTNAREA=row, INTERACTTYPE=direct,
  EXTPARAM={"action":"openForm","openMode":"edit","formPageCode":"add"}
【main 页 PAGECONFIG 必配】defaultFormPageCode=add(指向 form 页的 PAGECODE), 双击行才能打开表单。
【EXTPARAM 三要素缺一不可】
  action=openForm      — 明确走打开表单分支
  openMode=add/edit    — 新增清空 currentId / 编辑带行 ID
  formPageCode=add     — 目标 form 页的 PAGECODE(必须与 tss_module_page.PAGECODE 一致)
【反例】RS_FEE 最初 EXTPARAM={"formPageCode":"add"} 缺 action+openMode, main 页缺 defaultFormPageCode。
【正例】LIB_M05 添加按钮 + main 页 defaultFormPageCode=add。',
'添加按钮,EXTPARAM,openForm,openMode,formPageCode,defaultFormPageCode,rs-modal,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_add_button_extparam');

-- 完成: 2 行 UPDATE + 1 条 pitfall
