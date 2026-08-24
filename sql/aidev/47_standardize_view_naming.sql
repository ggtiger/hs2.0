-- ============================================================
-- RS_FEE 审计字段标准化 + 视图命名修正(VCK→VBS) + 三条记忆
-- 背景: 用户澄清三个规范:
--   1. 审计字段标准化: CREATEID/MODIFYID varchar(64), CREATER/MODIFER varchar(16),
--      CREATETIME/MODIFYTIME datetime(全库 tbs_* 主流, m18 显示 192/48 是字节数=字符数×3)
--   2. 数据视图命名 = 物理表首字母 T→V: TBS_XXX→VBS_XXX, TCK_XXX→VCK_XXX
--      (实证: TBS_DEPT→VBS_DEPT, TBS_CUST→VBS_CUST, TCK_ORECORD→VCK_ORECORD)
--      RS_FEE 物理表 tbs_project_fee 的视图必须叫 VBS_PROJECT_FEE 不是 VCK
--   3. 向导每步必须等上一步确认执行(代码侧已强制, 见 ChangeSetEngine/WizardStepOrchestrator)
-- 日期: 2026-07-20
-- ============================================================

-- --------------------------------------------------
-- 1. tbs_project_fee 审计字段标准化
-- --------------------------------------------------
ALTER TABLE tbs_project_fee MODIFY COLUMN ID varchar(64) NOT NULL;
ALTER TABLE tbs_project_fee MODIFY COLUMN CREATEID varchar(64);
ALTER TABLE tbs_project_fee MODIFY COLUMN CREATER varchar(16);
ALTER TABLE tbs_project_fee MODIFY COLUMN MODIFYID varchar(64);
ALTER TABLE tbs_project_fee MODIFY COLUMN MODIFER varchar(16);

-- --------------------------------------------------
-- 2. 视图改名 VCK_PROJECT_FEE → VBS_PROJECT_FEE(级联 5 张表)
-- --------------------------------------------------
UPDATE tss_resource   SET ID='vbs_project_fee_001', RESOURCENAME='VBS_PROJECT_FEE' WHERE ID='vck_project_fee_001';
UPDATE tss_resfield   SET RESOURCEID='vbs_project_fee_001' WHERE RESOURCEID='vck_project_fee_001';
UPDATE tss_resfilter  SET RESOURCEID='vbs_project_fee_001' WHERE RESOURCEID='vck_project_fee_001';
UPDATE tss_resuipc    SET RESOURCEID='vbs_project_fee_001' WHERE RESOURCEID='vck_project_fee_001';
UPDATE tss_moudlepath SET RESOURCEID='vbs_project_fee_001' WHERE RESOURCEID='vck_project_fee_001';

-- --------------------------------------------------
-- 3. 记忆: 审计字段标准化
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_audit_fields_standard', 'rule', 'metadata',
'业务表审计字段标准化六件套: CREATEID/MODIFYID varchar(64), CREATER/MODIFER varchar(16), CREATETIME/MODIFYTIME datetime',
'【标准】业务表(tbs_/tck_)必须含以下审计字段, 类型/长度固定:
  ID         varchar(64)  主键(KEYGENTYPE=GUID)
  CREATEID   varchar(64)  创建人ID
  CREATER    varchar(16)  创建人姓名(R 后缀, 不是 CREATERNAME)
  CREATETIME datetime     创建时间
  MODIFYID   varchar(64)  修改人ID
  MODIFER    varchar(16)  修改人姓名(R 后缀)
  MODIFYTIME datetime     修改时间
  ISDELETED  tinyint      逻辑删除(默认 0)
【注意】m18 界面显示长度 192/48 是字节数(utf8mb4 字符×3), 对应 varchar(64)/varchar(16), 不要按 192/48 建列。
【命名禁忌】禁用 CREATEDBY/CREATEDBYNAME/CREATEDTIME/UPDATEBY/UPDATETIME(全大写无下划线, R 后缀表姓名)。
【setSaveInfo 自动填充】后端 doSave 时 CREATE*/MODIFY* 由 ORM 按字段名自动写当前用户/时间, 前端不用传。
【反例】tbs_project_fee 最初 CREATEID varchar(32)/CREATER varchar(64) → 与全库标准不符。
【正例】tbs_dept/tbs_cust/tbs_ard: CREATEID varchar(64), CREATER varchar(16)。',
'审计字段,CREATEID,CREATER,CREATETIME,MODIFYID,MODIFER,MODIFYTIME,varchar64,varchar16,标准化,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_audit_fields_standard');

-- --------------------------------------------------
-- 4. 记忆: 视图命名 T→V 规则(修正之前 VCK 一律化的错误认知)
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_view_naming_t2v', 'rule', 'metadata',
'数据视图命名 = 物理表名首字母 T 换成 V: TBS_XXX→VBS_XXX, TCK_XXX→VCK_XXX',
'【铁律】数据视图(DATAVIEW)名称跟物理表走, 只把首字母 T 换成 V:
  tbs_project_fee → VBS_PROJECT_FEE(不是 VCK!)
  tbs_dept        → VBS_DEPT
  tck_orecord     → VCK_ORECORD
【实证】全库 DATAVIEW 资源: TBS_DEPT→VBS_DEPT, TBS_CUST→VBS_CUST, TBS_EMP→VBS_EMP, TBS_REGUITEM→VBS_REGUITEM, TCK_ORECORD→VCK_ORECORD, TCK_ACCEPT→VCK_ACCEPT。
【前缀语义】tbs_=基础/业务表, tck_=流程/记录表; vbs_/vck_ 是对应的数据视图。不是 VCK=业务视图/VBS=选择器(旧认知错误)。
【特殊后缀】同表多视图加后缀: VCK_ACCEPT_SEL(选择器)/VCK_ACCEPT_FEE(费用)/VCK_ACCEPT_OUTER(外部分享), 基础名仍按 T→V。
【资源 ID】视图资源 ID 用小写同名: vbs_project_fee_001, 与 RESOURCENAME 对应。
【反例】RS_FEE 最初视图叫 VCK_PROJECT_FEE(表是 tbs_) → 违反 T→V 规则, 已改 VBS_PROJECT_FEE。
【影响面】改名需级联 5 张表: tss_resource(ID+RESOURCENAME) / tss_resfield / tss_resfilter / tss_resuipc / tss_moudlepath 的 RESOURCEID。',
'视图命名,VBS,VCK,TBS,TCK,T换V,DATAVIEW,命名规则,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_view_naming_t2v');

-- --------------------------------------------------
-- 5. 记忆: 向导步骤强制确认执行
-- --------------------------------------------------
INSERT INTO tss_ai_memory (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, SOURCE, ISDELETED, CREATETIME)
SELECT 'am_rule_wizard_step_enforce', 'rule', 'wizard',
'向导每步开始前必须确认上一步变更项已执行(EXECUTED), 否则本步找不到上一步资源',
'【规则】向导分步生成时, 第 N 步(N>0)开始前检查 changeset:
  存在 ITEMSTATUS IN (DRAFT, CONFIRMED) 的变更项 → 拒绝生成, 提示用户先在变更清单「确认并执行」。
【原因】每步工具产出的是 DRAFT 变更项(SQL 未真正执行)。第 N+1 步的工具(create_physical_table/define_dataview/configure_ui_field 等)要查 DB 拿上一步的资源 ID/字段 ID, 上一步没执行就查不到 → 生成质量塌方。
【状态机】ChangeItem: DRAFT(产出) → CONFIRMED(用户确认) → EXECUTED(ExecuteConfirmed 跑完 SQL 自动转入) / REJECTED / MERGED。
【一键生成例外】GenerateAllAsync(6 步连跑)跳过此检查(内部共享 changeset, 靠 LookupDraft* 系列兜底跨步查找), 最后统一确认执行。
【前端配合】向导 UI 应在每步完成后提示「请先执行本步变更项再进入下一步」, 执行按钮调 ExecuteConfirmed。
【代码位置】WizardStepOrchestrator.GenerateStepAsync 的 enforcePreviousExecuted 检查; ChangeSetEngine.ExecuteConfirmed 成功后 CONFIRMED→EXECUTED。',
'向导,步骤强制,确认执行,EXECUTED,changeset,DRAFT,铁律',
'assistant,aidev,wizard', '0,1,2,3,4,5', 10, 'auto_seed', 0, NOW()
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_ai_memory WHERE ID='am_rule_wizard_step_enforce');

-- 完成: 5 ALTER + 5 级联 UPDATE + 3 条记忆
