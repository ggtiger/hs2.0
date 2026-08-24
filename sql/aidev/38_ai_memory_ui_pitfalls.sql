-- ============================================================
-- AI 记忆 UI 规范 + RS_M26 修复
-- 背景: AI 记忆管理页(RS_M26)暴露三类问题, 同时这些也是
--       AI 向导生成模块时的通用坑, 必须作为永久 pitfall 注入:
--   1. 列表字段未绑定 FIELDNAME → 整列空白(只配 RESFIELDID 不够)
--   2. 列表页缺 row 区域「编辑/删除」按钮 → 无法操作行
--   3. 表单页缺「删除」按钮 → 编辑模式下不能删
--   4. 编辑应跳子页面(navigate)而非弹窗(direct), 否则长内容/复杂表单体验差
--   5. LISTSORT 应从 1 起递增, 不是任意大数
--   6. 长文本字段(CONTENT/REMARK/错误示例)在列表应隐藏或限宽, 不直接全量展示
-- 日期: 2026-07-19
-- ============================================================

-- -----------------------------------------------------------
-- 一、永久记忆: UI 配置 6 类铁律(pitfall, PRIORITY=10, 向导必注入)
-- -----------------------------------------------------------

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_resuipc_fieldname', 'pitfall', 'metadata',
'列表/表单字段必须同时配 FIELDNAME + RESFIELDID(只配 RESFIELDID 渲染空白)',
'【症状】generic-module 列表整列空白、表单字段不渲染、查询条件失效。
【根因】tss_resuipc 行只配了 RESFIELDID(指向 tss_resfield.ID)是不够的。generic-module 渲染时优先读 FIELDNAME 列作为数据 key(对应 DataTable 列名 / SELECT 输出列名), FIELDNAME=NULL 时 cell 找不到数据 → 空白。
【正确做法】INSERT tss_resuipc 时三件套必齐:
  FIELDNAME = ''FEECODE''        -- 大写字段名, 与 tss_resfield.FIELDNAME 一致
  RESFIELDID = ''rf_xxx_feecode'' -- 指向 tss_resfield.ID
  LABELNAME = ''费用编码''        -- 中文标签
【对照】RS_FEE 正确示例: vck_project_fee_001 所有 resuipc 行 FIELDNAME+RESFIELDID+LABELNAME 三件套齐全。RS_M26 反例: resuipc.FIELDNAME 全 NULL(只配 RESFIELDID) → 列表全空白。
【关联】DATAVIEW 资源必须在 tss_resfield 注册字段(REFFIELDID→TBS), 这是元数据层; resuipc FIELDNAME 是 UI 层, 两层缺一不可。',
'resuipc,FIELDNAME,列表空白,RESFIELDID,generic-module,UI配置,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_resuipc_fieldname');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_list_row_buttons', 'pitfall', 'wizard',
'列表页 tss_module_button 必须配 row 区域「编辑+删除」按钮',
'【症状】列表页表格行没有任何操作按钮, 用户无法编辑/删除已有记录。
【铁律】PAGETYPE=list 的 tss_module_page 必须配至少 2 个 BTNAREA=''row'' 按钮:
  1. 「编辑」按钮: BTNAREA=''row'', BTNCODE=''edit'', INTERACTTYPE=''direct'', EXTPARAM=''{"formPageCode":"form","openMode":"edit"}''
     → 点击跳 form 子页面编辑当前行
  2. 「删除」按钮: BTNAREA=''row'', BTNCODE=''delete'', INTERACTTYPE=''poptip'', POPTIPTEXT=''确定删除?''
     → 点击弹 Poptip 确认框, 走 store delete action
【INTERACTTYPE 只允许 direct 或 poptip】(后端 DefineButton 强校验, 其他值被拒; 前端 generic-module.vue 也只识别 poptip)
  - direct: 点击直接执行(默认)
  - poptip: Poptip 组件弹确认框 + POPTIPTEXT 文案
【BTNCODE 决定按钮行为】add/edit/select/delete/save/cancel/export/submit/reSubmit/check/reCheck/verify/reVerify/subAdd/subRemove/subUp/subDown/custom
【反例】RS_M26 记忆列表 main 页只有 header 的「添加」按钮, row 区域空 → 用户无法编辑/删除任一条记忆。
【正例】RS_FEE demo: mb_rs_fee_main_edit(row, edit, direct, {"formPageCode":"form","openMode":"edit"}) + mb_rs_fee_main_delete(row, delete, poptip, "确定删除?")。',
'tss_module_button,BTNAREA,row,编辑,删除,INTERACTTYPE,navigate,confirm,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_list_row_buttons');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_form_delete_button', 'pitfall', 'wizard',
'表单页 footer 必须配「删除」按钮(编辑模式可见, 新增模式隐藏)',
'【症状】表单页打开已有记录编辑时, 没有「删除」按钮, 用户必须返回列表才能删 → 体验割裂。
【铁律】PAGETYPE=form 的 tss_module_page 必须在 BTNAREA=''footer'' 配三个按钮:
  1. 「保存」: BTNCODE=''save'', BTNAREA=''footer'', INTERACTTYPE=''direct'', APICODE=''A04save''
  2. 「删除」: BTNCODE=''delete'', BTNAREA=''footer'', INTERACTTYPE=''poptip'', POPTIPTEXT=''确定删除?'',
     SHOWCOND=''row.ID && state.__mode__ !== ''''add'''' ''
     → 仅编辑模式显示(有 ID 且非新增); 走 store delete action 不需 APICODE
  3. 「取消」: BTNCODE=''cancel'', BTNAREA=''footer'', INTERACTTYPE=''direct'', 走 closeTabAndBack 返回列表
【SHOWCOND 上下文】传入 {row, key, path, state}, row 是当前 form 数据, state.__mode__ 是 ''''add'''' / ''''edit''''。
【INTERACTTYPE 只允许 direct 或 poptip】不要写成 confirm/navigate/slot 等不存在的值。
【反例】RS_M26 记忆编辑 form 页只有「保存」「取消」两个按钮, 缺删除 → 编辑模式无法删。
【正例】RS_FEE demo form 页: 保存/删除/取消 三按钮齐全。',
'表单页,删除按钮,BTNAREA,footer,SHOWCOND,编辑模式,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_form_delete_button');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_form_subpage_navigate', 'pitfall', 'wizard',
'复杂表单(长内容/多字段/富文本)走子页面 navigate, 不要弹窗 direct',
'【症状】长内容表单(CONTENT/源代码/JSON 配置)用 modal 弹窗 → 内容被裁剪、滚动条混乱、保存按钮够不着。
【铁律】判定走子页面还是弹窗:
  - 字段数 ≤ 6 且无 textarea/富文本 → 可用 modal 弹窗 (header 添加按钮 BTNCODE=''add'' + EXTPARAM.openMode=''modal'')
  - 字段数 > 6 或含 textarea/代码编辑器/富文本 → 必须走子页面 (BTNCODE=''add''/''edit'' + EXTPARAM.formPageCode=''form'')
【子页面配置】
  - 列表页 main PAGECONFIG 加: {"defaultFormPageCode":"form"}
  - 列表 header「添加」按钮: BTNCODE=''add'', INTERACTTYPE=''direct'', EXTPARAM={''{''"formPageCode":"form"{'}'}'}
  - 列表 row「编辑」按钮: BTNCODE=''edit'', INTERACTTYPE=''direct'', EXTPARAM={''{''"formPageCode":"form","openMode":"edit"{'}'}'}
  - form 页 ROUTEPATH 不用配, registerGenericRoute 自动注册 /g/{MC}/form
  - form 页保存/取消后调 closeTabAndBack() 关闭 Tab 返回列表
【关键】跳子页面靠 BTNCODE+EXTPARAM.formPageCode, 不是 INTERACTTYPE=''navigate'' (该值不存在)
【反例】RS_M26 记忆编辑 form 页 CONTENT 是 textarea(长文本), 但 main 页「添加」按钮缺 formPageCode 跳转 → 体验差。
【正例】RS_FEE demo: main 添加/row 编辑都走 BTNCODE+formPageCode 跳 form 子页面。',
'表单页,子页面,navigate,弹窗,modal,PAGECONFIG,defaultFormPageCode,长内容,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_form_subpage_navigate');

INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_pitfall_listsort_start_from_1', 'pitfall', 'metadata',
'LISTSORT/EDITSORT/QUERYSORT 必须从 1 起连续递增, 不要从 11/100 起步',
'【症状】LISTSORT 从 11 起步 → 列表前 10 列空白; EDITSORT 跳号 → 表单字段顺序错乱。
【铁律】
  - LISTSORT: 列表显示顺序, 从 1 起连续递增(1,2,3,4,5...), 不显示的字段设 NULL
  - QUERYSORT: 查询条件顺序, 从 1 起连续递增, 不查询的字段设 NULL
  - EDITSORT: 表单编辑顺序, 从 1 起连续递增, 不编辑的字段设 NULL
  - 三套 SORT 独立编号, 互不影响
【长文本字段在列表】
  - textarea/text 字段(CONTENT/REMARK/错误示例) 在列表 LISTSORT 设 NULL(不显示) 或只显示前 50 字截断
  - 列表只显示编码/名称/类型/状态/时间 这类短字段
  - 详情走子页面 form 查看
【反例】RS_M26 resuipc LISTSORT 从 11 起步(11~25), 把 CONTENT/错误示例/修正方案 这些 textarea 也排进列表 → 列表极宽且空白。
【正例】RS_FEE resuipc LISTSORT 1~6 (编码/名称/类型/金额/日期/备注), 备注虽 textarea 但限宽显示; REMARK 在列表设 LISTSORT=6 短显示, 编辑页才全量。',
'LISTSORT,EDITSORT,QUERYSORT,顺序,长文本,textarea,列表宽度,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 9, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_pitfall_listsort_start_from_1');

-- -----------------------------------------------------------
-- 二、立即修复 RS_M26 模块(把 pitfall 应用到自身)
-- -----------------------------------------------------------

-- 2.1 修复 resuipc: 补 FIELDNAME + 重排 LISTSORT(长文本字段不进列表)
UPDATE tss_resuipc SET FIELDNAME='MEMORYTYPE' WHERE ID='uipc_am_type';
UPDATE tss_resuipc SET FIELDNAME='ASSETTYPE'  WHERE ID='uipc_am_asset';
UPDATE tss_resuipc SET FIELDNAME='TITLE'      WHERE ID='uipc_am_title';
UPDATE tss_resuipc SET FIELDNAME='CONTENT'    WHERE ID='uipc_am_content';
UPDATE tss_resuipc SET FIELDNAME='WRONG_CONTENT'  WHERE ID='uipc_am_wrong';
UPDATE tss_resuipc SET FIELDNAME='FIX_STRATEGY'   WHERE ID='uipc_am_fix';
UPDATE tss_resuipc SET FIELDNAME='TAGS'           WHERE ID='uipc_am_tags';
UPDATE tss_resuipc SET FIELDNAME='SCENE_CODES'    WHERE ID='uipc_am_scenes';
UPDATE tss_resuipc SET FIELDNAME='WIZARD_STEPS'   WHERE ID='uipc_am_steps';
UPDATE tss_resuipc SET FIELDNAME='PRIORITY'       WHERE ID='uipc_am_priority';
UPDATE tss_resuipc SET FIELDNAME='QUALITY_SCORE'  WHERE ID='uipc_am_score';
UPDATE tss_resuipc SET FIELDNAME='HITCOUNT'       WHERE ID='uipc_am_hit';
UPDATE tss_resuipc SET FIELDNAME='SOURCE'         WHERE ID='uipc_am_source';
UPDATE tss_resuipc SET FIELDNAME='CREATETIME'     WHERE ID='uipc_am_ctime';

-- 重排 LISTSORT: 类型/资产/标题/优先级/评分/命中/来源/创建时间 进列表(8 列),
-- 长文本(CONTENT/WRONG/FIX/TAGS/SCENES/STEPS)不进列表(走表单页查看)
UPDATE tss_resuipc SET LISTSORT=1 WHERE ID='uipc_am_type';
UPDATE tss_resuipc SET LISTSORT=2 WHERE ID='uipc_am_asset';
UPDATE tss_resuipc SET LISTSORT=3 WHERE ID='uipc_am_title';
UPDATE tss_resuipc SET LISTSORT=4 WHERE ID='uipc_am_priority';
UPDATE tss_resuipc SET LISTSORT=5 WHERE ID='uipc_am_score';
UPDATE tss_resuipc SET LISTSORT=6 WHERE ID='uipc_am_hit';
UPDATE tss_resuipc SET LISTSORT=7 WHERE ID='uipc_am_source';
UPDATE tss_resuipc SET LISTSORT=8 WHERE ID='uipc_am_ctime';
UPDATE tss_resuipc SET LISTSORT=NULL WHERE ID IN ('uipc_am_content','uipc_am_wrong','uipc_am_fix','uipc_am_tags','uipc_am_scenes','uipc_am_steps');

-- 重排 EDITSORT 从 1 起
UPDATE tss_resuipc SET EDITSORT=1  WHERE ID='uipc_am_type';
UPDATE tss_resuipc SET EDITSORT=2  WHERE ID='uipc_am_asset';
UPDATE tss_resuipc SET EDITSORT=3  WHERE ID='uipc_am_title';
UPDATE tss_resuipc SET EDITSORT=4  WHERE ID='uipc_am_content';
UPDATE tss_resuipc SET EDITSORT=5  WHERE ID='uipc_am_wrong';
UPDATE tss_resuipc SET EDITSORT=6  WHERE ID='uipc_am_fix';
UPDATE tss_resuipc SET EDITSORT=7  WHERE ID='uipc_am_tags';
UPDATE tss_resuipc SET EDITSORT=8  WHERE ID='uipc_am_scenes';
UPDATE tss_resuipc SET EDITSORT=9  WHERE ID='uipc_am_steps';
UPDATE tss_resuipc SET EDITSORT=10 WHERE ID='uipc_am_priority';
UPDATE tss_resuipc SET EDITSORT=11 WHERE ID='uipc_am_score';
UPDATE tss_resuipc SET EDITSORT=12 WHERE ID='uipc_am_source';

-- 2.2 main 页 PAGECONFIG 加 defaultFormPageCode, 让「编辑」走子页面
UPDATE tss_module_page
SET PAGECONFIG = '{"defaultFormPageCode":"form"}'
WHERE ID='mp_rs_m26_main';

-- main 页「添加」按钮改为跳子页面（BTNCODE='add' + formPageCode）
UPDATE tss_module_button
SET BTNCODE='add',
    INTERACTTYPE='direct',
    EXTPARAM='{"formPageCode":"form"}'
WHERE ID='mb_rs_m26_main_add';

-- 2.3 新增 row 区域「编辑」「删除」按钮(列表行操作)
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, EXTPARAM, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_main_edit', 'mp_rs_m26_main', 'RS_M26', NULL, '编辑', 'crud', 'edit', 'row', 'direct', NULL, NULL, NULL, 'icon-edit', '#0080ff',
'{"formPageCode":"form","openMode":"edit"}',
1, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_main_edit');

INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, EXTPARAM, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_main_delete', 'mp_rs_m26_main', 'RS_M26', NULL, '删除', 'crud', 'delete', 'row', 'poptip', '确认删除该条记忆?', NULL, NULL, 'icon-delete', '#e02020', NULL,
2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_main_delete');

-- 2.4 form 页新增「删除」按钮(footer, 仅编辑模式显示)
INSERT INTO tss_module_button (ID, PAGEID, MODULECODE, APICODE, BTNNAME, BTNTYPE, BTNCODE, BTNAREA, INTERACTTYPE, POPTIPTEXT, SHOWCOND, PERMCODE, ICON, COLOR, EXTPARAM, SORTNO, ISDELETED)
SELECT 'mb_rs_m26_form_delete', 'mp_rs_m26_form', 'RS_M26', NULL, '删除', 'crud', 'delete', 'footer', 'poptip', '确认删除该条记忆?',
'row.ID && state.__mode__ !== ''add''',
NULL, 'icon-delete', '#e02020', NULL,
2, 0
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_module_button WHERE ID='mb_rs_m26_form_delete');

-- 重新排序 form footer: 保存(1) 删除(2) 取消(3)
UPDATE tss_module_button SET SORTNO=1 WHERE ID='mb_rs_m26_form_save';
UPDATE tss_module_button SET SORTNO=3 WHERE ID='mb_rs_m26_form_cancel';

-- -----------------------------------------------------------
-- 三、把新增的 5 条 pitfall 加入向导 Step1/Step3 引导
-- -----------------------------------------------------------
-- MemoryService 在向导场景会按 PRIORITY>=5 自动注入, 这些 pitfall PRIORITY=9/10
-- 必然被注入, 无需额外配置 SCENE_CODES(已配 assistant,aidev,wizard 全场景)

-- -----------------------------------------------------------
-- 完成
-- -----------------------------------------------------------
-- 本批 5 条新增 pitfall(UI 铁律) + RS_M26 模块修复:
--   - resuipc FIELDNAME 补齐 14 行
--   - LISTSORT 重排(8 列进列表, 6 个长文本字段移出列表)
--   - EDITSORT 重排(1~12)
--   - main 页 PAGECONFIG + 添加按钮 navigate
--   - 新增 row 编辑/删除 2 按钮
--   - 新增 form 删除 1 按钮
-- 配合 32-37 = 共 139 条种子 + RS_M26 模块自愈
