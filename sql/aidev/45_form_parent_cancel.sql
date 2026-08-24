-- ============================================================
-- 表单按钮去取消 + form 页挂 list 子页面(PARENTID)
-- 背景: 用户两条新规则:
--   1. 表单 footer 不需要「取消」按钮(rs-modal 自带 X 关闭, 功能重复)
--   2. form 页必须是 list 页的下级子页面(tss_module_page.PARENTID=list页ID)
-- 日期: 2026-07-20
-- ============================================================

-- 1. 删除取消按钮(RS_FEE + RS_M26)
DELETE FROM tss_module_button WHERE ID IN ('mb_fee_cancel','mb_rs_m26_form_cancel');

-- 2. form 页补 PARENTID(RS_FEE + RS_M23 + RS_M24 + RS_M26)
UPDATE tss_module_page SET PARENTID='mp_fee_list'   WHERE ID='mp_fee_form';
UPDATE tss_module_page SET PARENTID='mp_rs_m23_main' WHERE ID='mp_rs_m23_form';
UPDATE tss_module_page SET PARENTID='mp_rs_m24_main' WHERE ID='mp_rs_m24_form';
UPDATE tss_module_page SET PARENTID='mp_rs_m26_main' WHERE ID='mp_rs_m26_form';

-- 3. pitfall: PARENTID 规则
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_form_parentid', 'pitfall', 'wizard',
'form 页必须配 PARENTID 指向 list 页 ID(form 是 list 的下级子页面)',
'【症状】form 页 PARENTID=NULL → formPageConfig 找不到 list 关联的 form, 双击行/添加按钮打不开表单或打开错页面。
【根因】generic-module.vue formPageConfig 查找优先级: 按钮指定 formPageCode > defaultFormPageCode > PARENTID=list页ID的form > 任意form。PARENTID 是声明 list→form 父子关系的正规机制, 多 form 页时必须靠 PARENTID 归属。
【铁律】tss_module_page: PAGETYPE=form 的行必须 PARENTID=list页ID(如 mp_xxx_form.PARENTID=mp_xxx_list); PAGETYPE=list 的行 PARENTID=NULL。
【配套】main 页 PAGECONFIG.defaultFormPageCode=form页PAGECODE 也要配, 双保险。
【反例】RS_FEE/RS_M23/RS_M24/RS_M26 的 form 页 PARENTID 全 NULL。
【正例】LIB_M05: 8b16(add,form).PARENTID=fb68(main,list); LIB_M07: lib_m07_page_form.PARENTID=lib_m07_page_main。',
'PARENTID,子页面,form,list,父子关系,module_page,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_form_parentid');

-- 4. 更新 form footer 按钮记忆(去取消)
UPDATE tss_ai_memory SET TITLE='表单页 footer 只配「保存+删除」按钮, 不配「取消」按钮(rs-modal 自带关闭)',
CONTENT='【症状】表单弹窗 footer 多出「取消」按钮, 与 rs-modal 右上角 X 关闭功能重复, 界面冗余。
【铁律】PAGETYPE=form 的 tss_module_page 在 BTNAREA=footer 只配两个按钮:
  1. 「保存」: BTNCODE=save, INTERACTTYPE=direct, APICODE=A04save, PERMCODE={MODULECODE}/A04, ICON=h-icon-save, COLOR=primary
  2. 「删除」: BTNCODE=delete, INTERACTTYPE=poptip, POPTIPTEXT=确定删除?, SHOWCOND=row.ID && state.__mode__!==''add''(仅编辑模式显示)
【为什么不配取消】generic-module 的表单是内嵌 rs-modal 弹窗, 弹窗自带 X 关闭按钮; 再配「取消」按钮功能重复。
【反例】RS_FEE/RS_M26 最初配了保存+删除+取消三按钮, 取消与 rs-modal X 重复。
【正例】footer 只有保存(primary)+删除(red, 编辑模式显示)。',
MODIFYTIME=NOW()
WHERE ID='am_pitfall_form_delete_button';

-- 完成
