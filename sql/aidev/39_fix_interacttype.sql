-- ============================================================
-- 修正已落库的 INTERACTTYPE 错误值 + demo SQL 同步
-- 背景: 38 批迁移和 demo_rs_fee 早期版本误用 INTERACTTYPE='navigate'/'confirm',
--       实际后端 DefineButton 强校验只允许 'direct'/'poptip',
--       前端 generic-module.vue 也只识别 'poptip'。
--       按钮行为由 BTNCODE + EXTPARAM 决定, 不是 INTERACTTYPE。
-- 日期: 2026-07-19
-- ============================================================

-- --------------------------------------------------
-- 1. 修正 RS_M26 的 5 个按钮
-- --------------------------------------------------
UPDATE tss_module_button SET BTNCODE='add',    INTERACTTYPE='direct', EXTPARAM='{"formPageCode":"form"}'
WHERE ID='mb_rs_m26_main_add';

UPDATE tss_module_button SET BTNCODE='edit',   INTERACTTYPE='direct', POPTIPTEXT=NULL,
  EXTPARAM='{"formPageCode":"form","openMode":"edit"}'
WHERE ID='mb_rs_m26_main_edit';

UPDATE tss_module_button SET BTNCODE='delete', INTERACTTYPE='poptip', POPTIPTEXT='确认删除该条记忆?',
  APICODE=NULL, EXTPARAM=NULL
WHERE ID='mb_rs_m26_main_delete';

UPDATE tss_module_button SET BTNCODE='delete', INTERACTTYPE='poptip', POPTIPTEXT='确认删除该条记忆?',
  APICODE=NULL, EXTPARAM=NULL,
  SHOWCOND='row.ID && state.__mode__ !== ''add'''
WHERE ID='mb_rs_m26_form_delete';

UPDATE tss_module_button SET BTNCODE='save',   INTERACTTYPE='direct'
WHERE ID='mb_rs_m26_form_save';

UPDATE tss_module_button SET BTNCODE='cancel', INTERACTTYPE='direct'
WHERE ID='mb_rs_m26_form_cancel';

-- --------------------------------------------------
-- 2. 修正 RS_FEE demo 模块(若已入库)
-- --------------------------------------------------
UPDATE tss_module_button SET BTNCODE='delete', INTERACTTYPE='poptip', POPTIPTEXT='确定删除该费用?'
WHERE ID='mb_fee_delete';

UPDATE tss_module_button SET BTNCODE='add',  INTERACTTYPE='direct',
  EXTPARAM='{"formPageCode":"add"}'
WHERE ID='mb_fee_add';

UPDATE tss_module_button SET BTNCODE='edit', INTERACTTYPE='direct',
  EXTPARAM='{"formPageCode":"add","openMode":"edit"}'
WHERE ID='mb_fee_edit';

UPDATE tss_module_button SET BTNCODE='save',   INTERACTTYPE='direct'
WHERE ID='mb_fee_save';

UPDATE tss_module_button SET BTNCODE='cancel', INTERACTTYPE='direct'
WHERE ID='mb_fee_cancel';

-- 完成
-- 本批 0 条新增, 11 行 UPDATE(RS_M26 6 个按钮 + RS_FEE 5 个按钮)
