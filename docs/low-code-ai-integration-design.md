# 华溯低代码平台 × AI 开发集成 — 详细设计

> 生成日期: 2026-07-17
> 前置文档: `docs/low-code-system-design.md`（低代码体系架构分析与演进规划）
> 本文档定位: 在已落地的低代码体系（GenericModule/SFC/模块向导）与已落地的 AI 体系（AgentEngine/变更包/三智能体）之上，完成两者的深度融合设计。**原则：能封装的封装，能配置的配置，平台大部分接口走元数据配置。**

---

## 〇、现状盘点（为什么是这个方案）

### 0.1 已落地能力清单

| 域 | 能力 | 载体 |
|---|------|------|
| 低代码运行时 | GenericModule（list/form/select/review/sfc 五种 PAGETYPE）+ 动态路由 `/g/{MODULECODE}/{PAGECODE}` | `tss_module_page` / `tss_module_button` + `generic-module.vue` / `generic-form.vue` |
| 低代码扩展 | EXTENDJS 动态 mixin / SLOTS 插槽 SFC / 模块级 store 扩展 | PAGECONFIG JSON + sfc-loader |
| SFC 在线开发 | 在线编写/编译/运行时加载 Vue 组件 | `tbs_sfc_template` + RS_M17 + `src/sfc-loader/` |
| 模块配置 | s01/m18 可视化配置（页面树/按钮/子页面/发布菜单） | RS_M18 + `config.vue` |
| AI 框架 | 4 场景统一 ReAct 循环（助理/填报/AiDev/向导/SFC） | `Services/Agent/AgentEngine.cs` + 子类 |
| AI 变更管控 | 会话→变更包→变更项(DRAFT/CONFIRMED)→校验→导出→执行→回滚 | `tss_aidev_*` 6 表 + ChangeSetEngine/Validator/Exporter/UpgradeExecutor |
| AI 开发工具 | 16 个开发工具（建表/字段/视图/过滤器/UI/字典/模块/API/菜单/权限/SFC） | `AssistantToolExecutor.GetDevToolDefinitions()` |
| AI 模块向导 | 6 步分步生成 + 一键全量生成，共享 changesetId | `WizardStepOrchestrator` + `module-wizard.vue` |
| AI 前端 | AiClient（SignalR/SSE 按场景）+ aiAgentProxy（19 个前端工具）+ 三智能体 store | `src/utils/ai/` + `src/store/modules/assistant.js` |
| 已配置化 | LLM 提供商/密钥（TBS_LLM_CONFIG）、提示词（TBS_ASSISTANT_PROMPT）、用量（TBS_LLM_USAGE） | 数据库表 + MemoryCache |

### 0.2 差距总览（本文档要解决的问题）

| # | 差距 | 原则 | 优先级 | 章节 |
|---|------|------|--------|------|
| G1 | **AI 生成不了页面/按钮**（tss_module_page/tss_module_button 无工具，AI 路径模块不闭环；手动路径 generateManual 已有 RS_M18 页面+按钮生成，AI 路径反而缺失） | 配置 | **P0** | 三 |
| G2 | **自定义业务接口必须写 C# Controller**（doMyApi 硬编码 + 重新编译发布、不热更新，AI 无法生成，"接口走元数据"未达成。APITYPE=sql 覆盖 SQL 类逻辑，APITYPE=csharp 在线脚本覆盖复杂逻辑） | 接口元数据化 | **P0** | 四 |
| G3 | AI 场景（scene）注册三处硬编码镜像（AiClient.js / aiAgentProxy / ToolRegistry+Startup） | 配置 | P1 | 五 |
| G4 | AI 工具定义 C# 硬编码（16+8+6 个），只读查询类工具本质是"一条 SQL" | 配置 | P1 | 六 |
| G5 | EDITTYPE 已 21 种但 AI 工具只暴露 8 种；PAGETYPE=report 是占位 | 封装暴露 | P2 | 七 |
| G6 | 无业务模板市场（.aidev.sql 导出/导入管道已存在，缺产品化） | 封装 | P2 | 八 |
| G7 | 无可视化表单设计器（uiSetFull 已完成 80%，缺画布化） | 体验 | P3 | 九 |
| G8 | **在线开发无版本概念**（SFC/脚本/元数据/页面配置直改库，改错无法回滚、无法对比；仅 AI 升级包路径有 DDL 快照） | 封装 | **P0** | 十一 |

> **实施状态（2026-07-17）：G1-G8 已全部落地**，详见文末"实施状态"章节（含交付文件清单、迁移 SQL、设计偏差说明）。

### 0.3 关键代码事实（设计依据）

以下事实经代码核实，后续设计均以此为据：

1. **DataController.Call 的 APITYPE switch**（`DataController.cs:51-122`）：22 个标准 APITYPE + default 走 `doMyApi`（`:132-135`，默认报"接口类型不存在"）。新增 APITYPE 只需在基类加 case，**所有子 Controller 自动继承**。
2. **tss_moudleapi 已有 SQLID 列**（exec 类型专用，BEFOREAPICODE/AFTERAPICODE 经 `SQLManage.GetSQL(SQLID)` 取模板执行），SQL 接口化无需改表结构。
3. **ViewOperate01.Save(ArrayList) 中 string 项不在事务内**（`ViewOperate01.cs:75-132`，`helper.Execute(v+"")` 无 trans 参数）——脚本接口**不能**复用 string 通道，须像 UpgradeExecutor 一样自行 `BeginTransaction()`。
4. **TryBuildChangeItem 管道**（`AiDevOrchestrator.cs:460-499`）：工具结果反射取 `sql/metadata/warnings` → `MapToolToCategory`（`:586-620`）→ ChangeItem。新增工具只需：工具定义 + 执行 case + 映射表项 + （可选）校验规则。
5. **ChangeSetValidator 按 CATEGORY switch**（`ChangeSetValidator.cs:80-117`），新增 CATEGORY 需加常量（`ChangeSet.cs:85-96`）+ case + 规则方法。
6. **前端 21 种 EDITTYPE 已全部实现**（`rs-form-cell.vue` 渲染分支 + `uiSetFull.vue:1084-1106` EDIT_TYPE_OPTIONS）：text/number/select/textarea/datepicker/autocomplete/multiautocomplete/treepicker/checkbox/fileupload/imageupload/code/image/action/multiselect/singleselect/toolbar/tableblock/pageaction/index/slot。**设计文档 Phase 1 的"EDITTYPE 扩展"实际已完成**，缺的只是 AI 工具未暴露。
7. **列表操作列已实现两条路**：scm 级 `EDITTYPE=action`（gen.js:252-267 解析 `"标签:code,per|..."`）+ generic-module 的 `tss_module_button BTNAREA=row`。
8. **wizard 手动兜底路径已生成页面+按钮**（`module-wizard.vue:658-702` generateManual 依次 RS_M02/A04、RS_M18/A04、S01_M03/A04），AI 路径缺对应工具——G1 缺口的确凿证据。
9. **AiClient scene 硬编码**（`AiClient.js:139-141` isSignalRScene：assistant/form/optimize→SignalR；`aiAgentProxy.js:1314-1332` registerForScene：assistant=19 个/form=12 个/其他=0）。
10. **UpgradeExecutor 已有完整能力**：ParseMeta/ParseItems/SplitSqlStatements/单事务执行/快照回滚/HASH 防篡改（`UpgradeExecutor.cs`）——模板市场的安装引擎直接复用。

---

## 一、总体集成架构

```
┌──────────────────────── AI 开发入口层 ────────────────────────┐
│ mAIDev 工作区 │ m18 向导 │ m17 SFC面板 │ 模板市场 │ 全局抽屉  │
└──────────────────────────────┬───────────────────────────────┘
                               │ 统一 AgentEngine + ChangeSet 管道
┌──────────────────────────────┴───────────────────────────────┐
│                    AI 能力层（新增配置化）                     │
│  tss_ai_scene(场景注册)  tss_ai_tool(声明式工具)              │
│  LlmClient │ PromptService │ UsageLogger（已配置化，不变）     │
└──────────────────────────────┬───────────────────────────────┘
                               │ 产出 = 元数据 SQL（DRAFT→CONFIRMED→执行）
┌──────────────────────────────┴───────────────────────────────┐
│                    元数据层（全部 tss_* 配置）                 │
│  resource/resfield/resfilter/resuipc/moudle/moudleapi(含SQLID)│
│  module_page/module_button/func/funcpoint/sql/sfc_template    │
│  + define_page/define_button/define_sql_api 等新工具(G1/G2)   │
└──────────────────────────────┬───────────────────────────────┘
                               │ 运行时消费，零硬编码
┌──────────────────────────────┴───────────────────────────────┐
│  GenericModule │ sfc-loader │ DataController                  │
│  + APITYPE=sql 脚本接口引擎(G2) │ + report-t01 接入(G5)       │
└──────────────────────────────────────────────────────────────┘
```

**元数据即产品**：本文档新增的 3 张表（tss_ai_scene/tss_ai_tool/tss_module_template）自身的管理界面，也用 GenericModule 零代码生成（注册资源+模块+页面即可），平台自举。

---

## 二、P0-1：页面/按钮 AI 生成闭环（define_page / define_button）

### 2.1 问题

向导 6 步（基本信息/数据模型/视图与查询/接口配置/UI配置/菜单注册）的 STEP_TOOL_MAP（`WizardStepOrchestrator.cs:42-50`）不含页面/按钮工具；16 个开发工具（`AssistantToolExecutor.cs:82-173`）同样没有。AI 生成完模块/接口/菜单后，还需人工到 s01/m18 补页面配置，菜单点击才有路由目标。

### 2.2 目标

AI 一句话生成的模块 = 元数据 + 页面 + 按钮 + 菜单，执行后**无需任何手工补配**，直接可用。

### 2.3 工具定义（GetDevToolDefinitions 新增 2 个）

**工具 1：define_page**

```
name: define_page
description: 定义模块页面(tss_module_page)。一个模块至少两个页面：
  列表页(PAGETYPE=list) + 表单页(PAGETYPE=form)。
  【约定】列表页 PAGECODE=main(菜单 /g/{MODULECODE}/main 指向它)；
  表单页 PAGECODE=form(ROUTEPATH 为空,由列表页弹窗打开,
  列表页 PAGECONFIG.defaultFormPageCode='form')。
  COMPONENTTYPE 默认 standard(通用模板渲染)；自定义页面用 sfc + SFCMODULEPATH。
params:
  moduleCode    string 必填  模块编码
  pageCode      string 必填  页面编码(main/form/review...)
  pageName      string 必填  页面名称
  pageType      string 必填  list/form/select/review/report
  routePath     string 可选  本地路由路径(通用模板页可空)
  componentType string 可选  standard(默认)/sfc
  sfcModulePath string 可选  componentType=sfc 时必填
  queryApiCode  string 可选  list 页查询接口(默认 A01)
  openApiCode   string 可选  form 页打开接口(默认 A02)
  saveApiCode   string 可选  form 页保存接口(默认 A04)
  pageConfig    string 可选  PAGECONFIG JSON(如 defaultFormPageCode/MAINPATH/SLOTS/EXTENDJS)
  sortNo        int    可选  排序(默认 0)
产出: tss_module_page 幂等 INSERT(SELECT...FROM DUAL WHERE NOT EXISTS)
ID 规则: 'mp_' + lower(moduleCode) + '_' + lower(pageCode)  （确定性 ID 利于幂等+去重）
```

**工具 2：define_button**

```
name: define_button
description: 定义页面按钮(tss_module_button)。
  标准 CRUD 约定：列表页 header 加"添加"(BTNCODE=add, PERMCODE={MODULECODE}/A04)；
  表单页 footer 加"保存"(BTNCODE=save)+"取消"(BTNCODE=cancel)。
  审批流按钮(提交/审核/审批/撤销)不需要配——模块有 FLOWCODE 时前端自动生成。
  BTNCODE 预设: add/edit/select/delete/save/export/submit/reSubmit/check/
  reCheck/verify/reVerify/subAdd/subRemove/subUp/subDown/custom。
  ACTIONTYPE: api(默认,调模块接口)/openForm/openSelector。
params:
  moduleCode   string 必填  模块编码(冗余字段,用于 A03 加载关联)
  pageCode     string 必填  目标页面(工具内部转 PAGEID='mp_'+...)
  btnName      string 必填  按钮名
  btnArea      string 必填  header/footer/row/子表路径(DTSA...)
  btnCode      string 可选  预设编码(默认 custom)
  apiCode      string 可选  ACTIONTYPE=api 时关联接口
  interactType string 可选  direct(默认)/poptip
  showCond     string 可选  显隐表达式(系统变量 _USERID_/_EMPID_/_DEPTID_/_checks_)
  permCode     string 可选  权限编码(如 R02_M07/A17)
  color/icon   string 可选
  extParam     string 可选  EXTPARAM JSON(openMode/formPageCode/extraParams/beforeAction/afterAction)
  sortNo       int    可选
产出: tss_module_button 幂等 INSERT
ID 规则: 'mb_' + lower(moduleCode) + '_' + lower(pageCode) + '_' + seq
```

### 2.4 变更包管道接入（4 处改动）

| 位置 | 改动 |
|------|------|
| `ChangeSet.cs:85-96` | CATEGORY 常量新增 `page` / `button` |
| `AiDevOrchestrator.MapToolToCategory:586-620` | + `define_page → (page, create, pageCode)`、`define_button → (button, create, btnName)` |
| `AiDevOrchestrator.BuildRationale:505-581` | + 中文说明（"定义列表页 main(通用模板)" / "定义按钮 添加(header)"） |
| `AiDevOrchestrator.MapToolToStep:665-688` | + `define_page/define_button → create_page`（前端步骤条显示） |
| `AssistantToolExecutor.Execute` | + 两个 case：组装幂等 INSERT SQL + metadata 返回 |

### 2.5 校验规则（ChangeSetValidator 新增）

```
case "page":   RunModulePageRules
  - PAGETYPE ∈ {list,form,select,review,report}
  - COMPONENTTYPE=sfc ⇒ SFCMODULEPATH 非空
  - 同 MODULECODE 下 PAGECODE 唯一（会话内 DRAFT 项检查，不查库）
case "button": RunModuleButtonRules
  - BTNAREA ∈ {header,footer,row} 或 DTS 开头
  - BTNCODE ∈ 预设 16 值 或 custom
  - BTNCODE=custom 且 ACTIONTYPE=api ⇒ APICODE 非空
  - 引用的 pageCode 在同会话存在 page 项（时序校验，仿 configure_resource_field 的 DRAFT 感知）
```

### 2.6 向导接入（STEP_TOOL_MAP 扩展，保持 6 步不新增）

页面/按钮依赖接口（QUERYAPICODE/OPENAPICODE/SAVEAPICODE 指向 APICODE），归并进 Step 3：

```csharp
// WizardStepOrchestrator.cs
STEP_LABELS[3]: "接口配置" → "接口与页面"
STEP_TOOL_MAP[3] = { "define_api", "define_filter", "define_page", "define_button" }
```

前端 `module-wizard.vue` 同步（3 处）：

1. `steps` 数组第 4 项文案改为"接口与页面"（L214）
2. `stepToolMap[3]` 镜像加两个工具（L280-290）
3. `buildStepPrompt(3)` 补充：默认生成 main 列表页 + form 表单页 + 添加/保存/取消按钮；有 flowCode 时提示"审批按钮自动生成无需配置"（L413-434）

菜单工具规则更新：`create_menu` 的 description 补充——通用模板模块的 `outerUrl` 必须为 `/g/{MODULECODE}/main`（与 s01/m18 publish 的约定一致）。

### 2.7 实现步骤

1. AssistantToolExecutor：2 个工具定义 + 2 个 Execute case（产出幂等 SQL）
2. AiDevOrchestrator：映射表 3 处
3. ChangeSet.cs 常量 + ChangeSetValidator 2 个 case
4. WizardStepOrchestrator STEP_TOOL_MAP/STEP_LABELS
5. module-wizard.vue 3 处文案/镜像
6. 端到端验证：AI 生成"样品管理模块" → 执行变更包 → 菜单点击直接打开 GenericModule 列表页

**风险**：低。纯增量，不动既有工具；生成失败时仍可人工在 s01/m18 补配（现有路径不受影响）。

---

## 三、P0-2：APITYPE=sql 脚本接口（自定义接口元数据化）

### 3.1 问题

自定义业务接口（如"批量收费""退样""生成证书"）当前必须：写 C# Controller 子类 → 重写 doMyApi → tss_moudleapi 配 APITYPE=NULL → 重新编译发布。这是"平台大部分接口走元数据配置"原则下最大的硬编码残留。分析 RM11/RM13/RM15/SM110 的自定义接口，**约 70% 是"一条或多条 SQL + 状态更新"模式**，完全可以声明式表达。

### 3.2 设计：DataController 新增 case "sql"

```csharp
// DataController.cs Call 的 switch 新增（基类，所有模块可用）
case "sql": doSqlApi(MD, row, Params); break;
```

**doSqlApi 处理流程**：

```
1. SQLID = row.GetString("SQLID")                // tss_moudleapi.SQLID（列已存在，无需改表）
2. sqlTxt = SQLManage.GetSQL(SQLID)              // 从 tss_sql 取 NVelocity 模板
3. 参数组装:
   params = Params["FilterParams"] as Hashtable ?? Params
   注入系统变量 _USERID_/_EMPID_/_DEPTID_
   Params 顶层键(ID/IDS 等)一并并入（方便前端直传）
4. sql = SQLManage.ParseSQL(sqlTxt, params)      // NVelocity 注参
5. 安全检查(可配置黑名单): DROP/ALTER/TRUNCATE/CREATE/GRANT/RENAME
   → 命中则 SetError("脚本接口禁止 DDL")
6. 自行开事务执行（不用 operate01.Save 的 string 通道——该通道不在事务内）:
   using conn → BeginTransaction
   statements = SplitSqlStatements(sql)           // 提取 UpgradeExecutor 的拆分逻辑为公共工具
   foreach stmt:
     SELECT 开头 → Query，收集结果集
     其余 → Execute(trans)，累加 affected
   Commit（任一失败 Rollback + SetError）
7. 返回 ResponseModel:
   有结果集 → Data = 最后一个结果集(或结果集列表)
   无结果集 → Data = { affected: n }
8. addOperateLogs 复用现有日志
```

**配套公共化改动**：把 `UpgradeExecutor.SplitSqlStatements` 抽到 `Realso.Utils`（如 `SqlScriptHelper`），UpgradeExecutor 与 doSqlApi 共用。

### 3.3 tss_moudleapi 配置示例（"批量完成"接口）

```sql
-- 1) SQL 模板（注意 NVelocity 铁律：禁止单引号，空串用 CHAR(39) 或参数化）
INSERT INTO tss_sql (SQLID, SQLCODE, SQLTYPE, SQLTXT, REMARK)
SELECT 'sql_accept_done_001', 'SS_ACCEPT_DONE', 'mysql',
'UPDATE tbs_accept SET STATE=15, DONETIME=NOW(), DONEID=@_USERID_
 WHERE ID IN (@IDS) AND STATE=7', '受理批量完成'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_sql WHERE SQLCODE='SS_ACCEPT_DONE');

-- 2) 接口注册（APITYPE=sql，ACTIONCODE 必填——前端 getApi 依赖）
INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, SQLID, APIPARAM, ENTRYNUM)
SELECT 'ma_accept_a23', @mid, 'A23', 'sql', '批量完成', 'batchDone', 'MAIN', 'SS_ACCEPT_DONE', NULL, 23
FROM DUAL WHERE NOT EXISTS (...);
```

前端调用零改动——`store.call({APICODE:'A23', params:{FilterParams:{IDS}}})` 走统一 `/api/data/call/{module}/{apicode}` 入口，天然继承认证/日志/响应格式。

### 3.4 AI 工具：define_sql_api

```
name: define_sql_api
description: 定义 SQL 脚本接口(tss_sql + tss_moudleapi APITYPE=sql)。
  适用于自定义业务操作(状态流转/批量更新/计算回写),替代写 C# Controller。
  【铁律】SQLTXT 禁止单引号(NVelocity 限制)；参数用 @VAR；
  系统变量 @_USERID_/@_EMPID_/@_DEPTID_ 自动注入；
  禁止 DDL(DROP/ALTER/TRUNCATE/CREATE)；IN 列表写 IN (@IDS)。
params:
  moduleCode  string 必填
  apiCode     string 必填  如 A51
  apiName     string 必填
  actionCode  string 必填  前端识别码
  sqlCode     string 必填  如 SS_ACCEPT_DONE
  sqlTxt      string 必填  NVelocity 模板
  remark      string 可选
产出: 单变更项，SQLCONTENT = tss_sql INSERT + tss_moudleapi INSERT 两段
CATEGORY: api（复用现有 api 校验 RunMoudleApiActioncodeRule）
```

工具校验逻辑（Execute case 内）：正则预检单引号 → 有则返回 error 让 AI 重写；正则预检 DDL 关键字 → 同上。变更包导出/执行管道零改动（SQLCONTENT 透传 + UpgradeExecutor 单事务）。

### 3.5 可选扩展（P2，本阶段不做）：APITYPE=script 接口编排

对于"查询→判断→更新→再调子接口"的多步逻辑，预留 `APITYPE=script`：APIPARAM 存 JSON 步骤数组 `[{type:"sql",sqlCode},{type:"query",apiCode},{type:"if",cond,goto}]`。复用 doSqlApi 的执行器加步骤循环。**仅在 sql 类型证明覆盖不足后启动**，避免过度设计。

### 3.6 实现步骤

1. `Realso.Utils` 新增 SqlScriptHelper（SplitSqlStatements 迁入，UpgradeExecutor 改引用）
2. `DataController.cs`：case "sql" + doSqlApi（约 80 行）
3. 黑名单常量（可放 PromptService 同款配置表，默认 DROP/ALTER/TRUNCATE/CREATE/GRANT/RENAME）
4. AssistantToolExecutor：define_sql_api 工具定义 + Execute case + 预检
5. AiDevOrchestrator 映射 + ChangeSetValidator（api 类追加 SQLTXT 单引号/DDL 检查）
6. 验证：把 LI_M00 的 A51(退样) 改造为 sql 接口做对比测试

**风险**：中。SQL 注入面——必须仅靠 NVelocity `#if` + @参数（执行层 Dapper 参数化保留），禁止 `$!{var}` 直接拼值（工具 description 与校验双重把关）；黑名单只做 DDL 兜底，不做完整 SQL 解析。

---

## 四、P1-1：AI 场景配置化（tss_ai_scene）

### 4.1 问题

scene 定义三处硬编码镜像：`AiClient.js:139-141`（传输）、`aiAgentProxy.js:1314-1332`（前端工具）、后端 ToolRegistry setName + Startup DI。新增 AI 场景（如"AI 报表助手""AI 数据清洗"）要改 4 个文件两端代码。

### 4.2 表设计

```sql
CREATE TABLE tss_ai_scene (
  ID            VARCHAR(36) PRIMARY KEY,
  SCENECODE     VARCHAR(32) NOT NULL COMMENT '场景编码(assistant/form/optimize/aidev/wizard/sfc/自定义)',
  SCENENAME     VARCHAR(64) COMMENT '场景名称',
  TRANSPORT     VARCHAR(16) NOT NULL COMMENT 'signalr/sse',
  ENDPOINT      VARCHAR(128) COMMENT 'SSE: 完整路由(如 /api/RMAIDev/generate-stream); signalr: Hub方法名(Ask/AskForm)',
  TOOLSET       VARCHAR(32) COMMENT '后端工具集(assistant/formfill/dev/sfc)',
  PROMPTKEY     VARCHAR(64) COMMENT '提示词 key(TBS_ASSISTANT_PROMPT)',
  FRONTENDTOOLS VARCHAR(512) COMMENT '前端工具: all/none 或逗号分隔工具名',
  CONTEXTSOURCE VARCHAR(32) COMMENT '上下文源: none/formContext/sfcContext',
  ENABLED       TINYINT DEFAULT 1,
  SORTNO        INT DEFAULT 0,
  REMARK        VARCHAR(200),
  ISDELETED     TINYINT DEFAULT 0
);
-- 初始化: 现有 6 个场景种子数据(与现行硬编码行为逐一对应)
```

### 4.3 后端改动

1. `SceneConfigService`（Scoped + 60s MemoryCache）：读表返回场景配置；**表无数据时回落到现行硬编码默认值**（保证升级平滑）。
2. `ToolRegistry` 增 `GetToolsForScene(sceneCode)`：scene→TOOLSET→现有 `GetTools(setName)`，一层薄封装。
3. AssistantHub / 各 SSE action：从 SceneConfigService 取 PROMPTKEY/TOOLSET，替代字面量。
4. 新增轻量端点 `GET /api/assistant/scene-config`：返回所有 ENABLED 场景（前端消费），响应进 MemoryCache。

### 4.4 前端改动

1. `AiClient`：`ensureSceneConfig()`（首次使用时拉取 scene-config 缓存到模块级），`isSignalRScene(scene)` 改为读配置的 TRANSPORT；SSE 场景的 url 由 ENDPOINT 注入各 send 方法。
2. `aiAgentProxy.registerForScene(scene)`：FRONTENDTOOLS 解析（all→全部 / none→[] / 逗号名单→过滤），替代 if-else。
3. `assistant.js`：3 个 AiClient 实例创建逻辑保留（消息存储仍按 agent 分离），但构造参数从场景配置取。

### 4.5 管理界面（平台自举）

tss_ai_scene 走标准 ORM 注册（TBS 资源+resfield+VSS 视图+resuipc+F00/F01 过滤器+模块 RS_M20 + GenericModule 页面配置），**管理页零代码**：菜单配 `/g/RS_M20/main`。

**风险**：低。回落默认值保证不改表也能运行；改动集中在读取侧。

---

## 五、P1-2：声明式 AI 工具（tss_ai_tool）

### 5.1 问题

30 个工具全部硬编码在 `AssistantToolExecutor.cs`。其中只读查询类（search_dict/read_table_schema/get_menus/search_menu…）本质 = 一条 SELECT + 参数描述，每次加工具都要改 C# + 发布。而"让 AI 能查更多业务元数据"是运营期持续需求。

### 5.2 表设计

```sql
CREATE TABLE tss_ai_tool (
  ID           VARCHAR(36) PRIMARY KEY,
  TOOLNAME     VARCHAR(64) NOT NULL COMMENT '工具名(全局唯一,小写下划线)',
  TOOLSET      VARCHAR(32) NOT NULL COMMENT '所属工具集(assistant/formfill/dev/sfc)',
  DESCRIPTION  VARCHAR(1000) NOT NULL COMMENT '给 LLM 看的工具描述',
  PARAMS       TEXT COMMENT 'JSON Schema(parameters 对象原文)',
  EXECUTORTYPE VARCHAR(16) NOT NULL COMMENT 'sql(声明式只读)/builtin(代码内置)',
  SQLCODE      VARCHAR(64) COMMENT 'EXECUTORTYPE=sql 时指向 tss_sql.SQLCODE(仅 SELECT)',
  MAXROWS      INT DEFAULT 200 COMMENT '结果行数上限(防 token 爆炸)',
  ENABLED      TINYINT DEFAULT 1,
  ISDELETED    TINYINT DEFAULT 0
);
```

### 5.3 执行器：DeclarativeSqlToolExecutor

```csharp
class DeclarativeSqlToolExecutor : IToolExecutor {
  GetToolNames()        → 查 tss_ai_tool (ENABLED=1, EXECUTORTYPE=sql)
  GetDefinitions(set)   → 按 TOOLSET 过滤，直接用 DESCRIPTION/PARAMS 拼 OpenAI 工具格式
  Execute(name, args)   → SQLManage.GetSQL(SQLCODE) → ParseSQL(args 注参)
                        → 仅允许 SELECT 开头(正则) → Query + Take(MAXROWS)
                        → 结果 JSON 序列化(受 AgentOptions.MaxToolResultChars 截断)
}
```

`ToolRegistry.GetTools(setName)` 合并：内置定义（C#）+ 声明式定义（DB），**同名内置优先**（DB 不可覆盖 builtin，防提示词注入篡改核心工具）。

### 5.4 自举与安全

- AI 开发助理可通过 define_sql_api 类似的工具**为自己创造只读工具**（如"查某模块字段配置"），运营期能力随元数据生长——这是"平台大部分能力走配置"的终极形态。
- 安全边界：声明式工具仅 SELECT；builtin 变更类工具维持 C#（走 ChangeItem 管道）；frontend 工具维持 aiAgentProxy 注册。

**风险**：低-中。DESCRIPTION 质量直接影响 LLM 调用准确率，管理界面需配"测试调用"按钮（输入 args 预览结果）。

---

## 六、P2-1：EDITTYPE 暴露 / 报表页接入

### 6.1 EDITTYPE 全量暴露（小改动）

事实：前端 21 种 EDITTYPE 已实现（见 0.3-6），但 `configure_ui_field` 工具的 editType 枚举描述只有 8 种（`AssistantToolExecutor.cs:127`）。

**改动（仅工具描述）**：editType 枚举扩为 `text/textarea/number/datepicker/select/multiselect/autocomplete/multiautocomplete/treepicker/checkbox/fileupload/imageupload/code/editor`，并补充 selectData 约定（autocomplete/treepicker=`{module,apiCode,keyName,titleName,paramMappings}` JSON；multiautocomplete 需 mode/subMappings；fileupload/imageupload=`{multifile}` 或 subtable 模式）。无代码逻辑改动。

### 6.2 PAGETYPE=report 接入 report-t01

现状：`generic-module.vue:390-395` 为占位提示；`report-t01.vue` 是受控组件（datas/columns/options/initOption 全靠父传）。

**设计（PAGECONFIG 驱动）**：

```json
// tss_module_page.PAGECONFIG
{ "REPORT": {
    "APICODE": "A01",
    "TABLE": true,
    "CHART": { "type": "bar", "xField": "DEPTNAME", "yFields": ["CNT","AMT"], "initOption": {} }
} }
```

generic-module 的 report 分支改为：

```
1. 渲染 <report-t01 :datas :columns :options :initOption @query="onReportQuery">
2. columns: 从 scm LISTSORT 自动生成(复用 list 逻辑)
3. query: dispatch store.call(REPORT.APICODE) → rows 填 datas
4. options computed: 按 CHART 配置把 rows 映射为 echarts option
   (xField→xAxis.data, yFields→series[], type→series.type)
5. 查询区: simple-query slot 支持(日期范围等,沿用 rs-query-panel)
```

报表数据源惯例：`RESOURCETYPE=SQL` 的 VRP_xxx 资源 + FILTERSQL 存 SQLCODE（已有完整链路，含分页两段式）。

**AI 侧**：define_page 的 PAGECONFIG 直接支持 REPORT key，无需新工具。

**风险**：低。占位替换，不影响存量 SFC 报表页。

---

## 七、P2-2：业务模板市场（tss_module_template）

### 7.1 设计要点

模板 = 一组关联元数据的幂等 SQL 快照（与 .aidev.sql 同构）+ 变量占位。安装 = 变量替换 + UpgradeExecutor 执行（天然获得幂等/单事务/快照回滚/HASH 防篡改）。

```sql
CREATE TABLE tss_module_template (
  ID           VARCHAR(36) PRIMARY KEY,
  TEMPLATECODE VARCHAR(64) NOT NULL,
  TEMPLATENAME VARCHAR(128) NOT NULL,
  CATEGORY     VARCHAR(32) COMMENT '业务分类(b01/r01/r02/s01)',
  DESCRIPTION  VARCHAR(500),
  VARIABLES    TEXT COMMENT '变量定义 JSON [{name,label,default,required}]',
  SCRIPT       LONGTEXT COMMENT '.aidev.sql 格式脚本(含 ${VAR} 占位)',
  SOURCEINFO   VARCHAR(200) COMMENT '来源(模块编码或会话编码)',
  VERSION      VARCHAR(16) DEFAULT '1.0.0',
  ENABLED      TINYINT DEFAULT 1,
  ISDELETED    TINYINT DEFAULT 0
);
```

### 7.2 导出（两个来源）

**来源 A：AI 会话导出**——mAIDev 工作区加"存为模板"按钮：取已 CONFIRMED 项 → ChangeSetExporter.Export 同格式 → 模块编码等替换为 `${MODULECODE}` 占位 → 入模板表。

**来源 B：存量模块导出**（s01/m18 加"导出为模板"）：后端 `TemplateExporter` 按依赖序遍历：

```
tss_moudle(MODULECODE) → moudlepath/pathrel → moudleapi
  → 各 path 的 resource → resfield → resfilter → resuipc
  → tss_sql(被 SQLID/FILTERSQL 引用) → module_page/module_button
  → func/funcpoint → 引用的 dict
逐项生成幂等 INSERT(WHERE NOT EXISTS)，ID 用确定性规则(便于重装幂等)
模块编码/业务码替换为 ${MODULECODE}/${BIZCODE}
```

### 7.3 安装

```
模板市场页(新 GenericModule + SFC 安装弹窗):
  列表(卡片: 名称/分类/描述/版本) → [预览] 显示脚本分节与变量
  → [安装] 弹出变量表单(VARIABLES 驱动) → 提交
后端 POST /api/RMAIDevUpg/call/... A09 installTemplate:
  变量替换(${VAR}→值, 校验必填/编码格式) → UpgradeExecutor.Import
  → Preview 返回影响项 → 用户确认 → Execute(单事务+快照)
失败可 Rollback——模板安装天然带后悔药。
```

### 7.4 AI 集成

- 新只读工具 `search_module_template`（声明式 sql 工具即可，验证第五章机制）
- 新内置工具 `read_module_template`：读模板 SCRIPT 让 AI 学习结构（read_sfc_template 模式的成功复用）
- 向导 Step0 增加"从模板开始"选项：选中模板后 AI 基于模板 SCRIPT 做增量修改而非从零生成

**风险**：低。核心引擎（Exporter/UpgradeExecutor）已生产验证；工作量在遍历导出与变量替换两处。

---

## 八、P3-1：可视化表单设计器（渐进增强 uiSetFull）

### 8.1 判断

`uiSetFull.vue` 已具备：字段列表（勾选/拖拽排序）+ 属性面板（21 种 EDITTYPE 全配置）+ **rs-form-edit 实时预览**。距离"画布式设计器"只差布局维度。**建议不做全自由画布**（投入高、与现有 Form 布局体系冲突），做渐进增强。

### 8.2 增量设计

| 增强 | 元数据 | 渲染改动 |
|------|--------|---------|
| 字段占宽（一行几列） | tss_resuipc 加 `COLSPAN`(1/2，默认 1) | generic-form 的 Form 渲染按 COLSPAN 设 item 宽度类 |
| 字段行内排序即画布排序 | 复用 EDITSORT（已有拖拽） | 无 |
| 预览即画布 | 无 | uiSetFull 预览区字段支持拖拽换位（回写 EDITSORT）+ 宽度手柄（回写 COLSPAN） |
| EDITGROUP 可视化管理 | 已有 EDITGROUP 列 | uiSetFull 增加分组 Tab 管理（重命名/排序） |

ALTER TABLE 一条 + uiSetFull 交互增强 + generic-form 一个 CSS 类逻辑，即可完成"够用"的可视化设计。**全自由画布留作远期**，届时输出物仍是 resuipc+COLSPAN，元数据兼容。

---

## 九、数据库变更汇总

| 变更 | 类型 | 所属章节 |
|------|------|---------|
| tss_ai_scene 建表 + 6 条种子数据 | 新表 | 四 |
| tss_ai_tool 建表 | 新表 | 五 |
| tss_module_template 建表 | 新表 | 七 |
| tss_resuipc 加 COLSPAN 列 | ALTER | 八 |
| tss_moudleapi / tss_module_page / tss_module_button | **零改动**（SQLID 列已存在，页面/按钮表已就绪） | — |
| 3 张新表的 ORM 注册（TBS+VSS+resfield+resuipc+filter+模块+页面） | 元数据 SQL | 四/五/七（自举，管理界面零代码） |

## 十、实施路线

| 序 | 事项 | 投入 | 依赖 | 验证标准 |
|----|------|------|------|---------|
| 1 | G1 define_page/define_button + 向导接入 | 低 | 无 | AI 生成模块执行后菜单直接可用 |
| 2 | G2 APITYPE=sql + define_sql_api | 中 | SqlScriptHelper 抽取 | LI_M00 A51 改配置化对比通过 |
| 3 | G3 tss_ai_scene | 低 | 无 | 新场景纯配置接入 |
| 4 | G4 tss_ai_tool | 中 | 无 | search_dict 改声明式对照通过 |
| 5 | G5 EDITTYPE 暴露 + report 接入 | 中 | 无 | AI 配出 treepicker 字段；VRP 报表零 SFC |
| 6 | G6 模板市场 | 中 | G1（模板含页面/按钮） | 物流模块导出→重装→可用 |
| 7 | G7 表单设计器增量 | 低 | 无 | 拖拽排序+占宽实时预览 |

1→2 是价值主峰（AI 生成完整业务模块闭环）；3→4 让 AI 体系自身也符合"能配置的配置"；5→7 按业务需要拉动。

---

## 附录 A：新增工具一览（累计）

| 工具 | 类型 | 章节 |
|------|------|------|
| define_page | 变更类(builtin→ChangeItem) | 二 |
| define_button | 变更类 | 二 |
| define_sql_api | 变更类 | 三 |
| define_script_api | 变更类（Roslyn 预检 + HEX 写入） | 三 |
| read_api_script | 只读(builtin) | 三 |
| search_module_pages | 只读(声明式 sql, tss_ai_tool 种子) | 五 |
| list_ai_tools | 只读(声明式 sql, 种子) | 五 |
| search_module_template | 只读(声明式 sql, 种子) | 七 |
| read_module_template | 只读(builtin) | 七 |

## 附录 B：关键铁律（AI 工具 description 与校验器双重落实）

1. 字段名大写无下划线；VCK 字段 REFFIELDID 必须指向 TBS 字段；REFRESOURCEID 必须指向 TABLE 资源
2. FIELDTYPE 用 MySQL 原生类型（varchar/int/datetime…）
3. NVelocity 模板（FILTERSQL/SQLTXT）禁止单引号
4. FILTERSQL 以 1=1 开头、F01/F02 含 @INPUT、ORDERBY 无表别名
5. moudleapi.ACTIONCODE 非空；自定义 sql 接口 APITYPE=sql + SQLID 指向 tss_sql
6. 通用模板模块菜单 OUTERURL=/g/{MODULECODE}/main；列表页 PAGECODE=main
7. 声明式 AI 工具仅 SELECT；脚本接口禁止 DDL
8. csharp 脚本 SOURCECODE 一律以 MySQL 0x 十六进制字面量写入（同时避开单引号转义与 UpgradeExecutor 分号拆分误判）；脚本编辑页配独立功能点权限
9. 元数据修改必须走标准保存接口（doSave/doDelete/doUpdate 通道才有版本快照）；脚本中直 UPDATE tss_* 不产生版本，校验器应告警
10. tss_funcpoint 用 FUNCID（指向 tss_func.ID），不是 FUNCCODE（既有 create_funcpoints 工具生成的是 FUNCCODE，执行会报错——已知 bug，待修）

---

## 十一、实施状态（2026-07-17，G1-G8 全部落地）

### 11.1 交付总览

| 项 | 状态 | 核心交付 | 迁移 SQL |
|----|------|---------|---------|
| G1 define_page/define_button | ✅ | 2 工具 + 变更包管道 + 向导 Step3 扩为"接口与页面" | 无（表已存在） |
| G2a APITYPE=sql | ✅ | SqlScriptHelper 公共化 + doSqlApi（自开事务+DDL黑名单）+ define_sql_api | 无（SQLID 列已存在） |
| G2b APITYPE=csharp | ✅ | Roslyn 引擎（源码哈希热更新）+ ScriptGlobals + doScriptApi + RS_M21 管理页 + 2 工具 | `sql/aidev/11_api_script.sql` |
| G8 版本管理 | ✅ | _doSave/doDelete/doUpdate 统一拦截 + RDevVersionController 回滚 + 版本中心页(s01/m22) + m17/m18 入口 | `sql/aidev/12_dev_version.sql` |
| G3 tss_ai_scene | ✅ | SceneConfigService(60s缓存+内置回落) + scene-config 端点 + 前端 sceneConfig.js + RS_M23 | `sql/aidev/13_ai_scene.sql` |
| G4 tss_ai_tool | ✅ | DeclarativeSqlToolExecutor(双重SELECT校验+MAXROWS) + 声明式挂接(静态合并,同名内置优先) + RS_M24 | `sql/aidev/14_ai_tool.sql` |
| G5 EDITTYPE/report | ✅ | configure_ui_field 全量枚举 + PAGETYPE=report 接 report-t01(PAGECONFIG.REPORT 驱动) | 无 |
| G6 模板市场 | ✅ | TemplateExporter(依赖序遍历+HEX+变量占位) + RModuleTplController + 模板市场页(s01/m25) + m18 导出按钮 | `sql/aidev/15_module_template.sql` |
| G7 表单设计器增量 | ✅ | tss_resuipc.COLSPAN(≥2→single整行) + generic-form FORMLAYOUT + uiSetFull 占宽配置 | `sql/aidev/16_resuipc_colspan.sql` |

### 11.2 关键后端文件（新增/改动）

**新增**：
- `Realso.Utils/SqlScriptHelper.cs` — SQL 拆分/注释判定/DDL 黑名单（UpgradeExecutor 已改共用）
- `Services/Scripting/CSharpScriptEngine.cs` + `ScriptGlobals.cs` — Roslyn 脚本引擎（源码哈希热更新，冒烟测试 5/5 通过）
- `Services/DevVersionService.cs` — 版本捕获（cfg 60s 缓存，同事务，异常降级不影响业务保存）
- `Controllers/RDevVersionController.cs` — A05 回滚（OPTYPE 逆操作，回滚本身生成新版本）
- `Services/SceneConfigService.cs` — 场景配置（表空回落内置默认值）
- `Services/Agent/DeclarativeSqlToolExecutor.cs` + `DeclarativeToolProvider` — 声明式工具
- `Services/TemplateExporter.cs` — 模板导出（14 类元数据依赖序遍历）
- `Controllers/RModuleTplController.cs` — A05 导出 / A06 安装（→UpgradeExecutor.Import）

**改动**：
- `DataController.cs` — +case "sql"/"csharp" + doSqlApi/doScriptApi + doSave/doDelete/doUpdate 三处版本拦截
- `AssistantToolExecutor.cs` — +define_page/define_button/define_sql_api/define_script_api/read_api_script/read_module_template + 声明式合并与分发 + configure_ui_field 全量枚举
- `AiDevOrchestrator.cs` — 映射表（MapToolToCategory/ExtractTarget/BuildRationale/MapToolToStep）+ 只读工具跳过清单
- `ChangeSet.cs` — +CAT_PAGE/CAT_BUTTON 常量；`ChangeSetValidator.cs` — +page/button/sqlapi 3 组规则
- `WizardStepOrchestrator.cs` — STEP_TOOL_MAP[3] + STEP_LABELS[3]="接口与页面"
- `ToolRegistry.cs` — 重写支持同 set 多执行器（同名先注册优先）
- `UpgradeExecutor.cs` — SplitSqlStatements 等抽取到 SqlScriptHelper（行为不变）

### 11.3 关键前端文件（新增/改动）

**新增**：
- `src/pages/s01/m22/`（版本中心：筛选+list-t01+对比弹窗+回滚）+ `src/utils/simpleDiff.js`（无依赖 LCS 行级 diff + JSON 字段 diff）
- `src/pages/s01/m25/`（模板市场：预览+安装变量表单→升级详情页执行）
- `src/utils/ai/sceneConfig.js` — 场景配置加载器（失败回落内置默认值）

**改动**：
- `module-wizard.vue` — steps[3]="接口与页面" + stepToolMap/labels/buildStepPrompt
- `workspace.vue` / `ChangeItemCard.vue` — +page/button 分类与摘要
- `AiClient.js` — isSignalRScene 读场景配置 + ensureConnected 先加载配置；`aiAgentProxy.js` — registerForScene 读 FRONTENDTOOLS
- `generic-module.vue` — report 分支接 report-t01（PAGECONFIG.REPORT）；`generic-form.vue` — FORMLAYOUT→formMode
- `gen.js` — COLSPAN≥2→single；`uiSetFull.vue` — 占宽配置
- `m17/edit.vue` — 版本按钮；`m18/config.vue` — 版本历史 + 导出模板按钮/弹窗

### 11.4 设计偏差说明（as-built）

1. **csharp 热更新检测**：设计用 VERSION 列人工递增，实现改为**源码 MD5 哈希**（任何保存路径都生效，不依赖 VERSION 维护纪律）。
2. **声明式工具挂接**：设计走 ToolRegistry 套装配，实现为**静态合并**（DeclarativeToolProvider 挂进 3 个静态定义方法+同步 Execute 分发），因为真实调用链是"静态定义+每请求 executor"，不经过 ToolRegistry。ToolRegistry 已重写支持多执行器（备用）。
3. **funcpoint 列名**：真实表是 FUNCID（非 FUNCCODE），迁移 SQL 按 FUNCID 写；`create_funcpoints` AI 工具仍生成 FUNCCODE 写法，**是既有 bug**（执行时报 Unknown column），待修。
4. **模板安装执行**：设计自带 preview/execute，实现复用 mAIDevUPG 详情页（安装注册 PENDING 后直接路由过去），零重复代码。
5. **报表分页**：PAGEMAX 默认 500（走标准分页两段式）。
6. **G7 画布拖拽**：本期交付 COLSPAN 元数据+渲染+属性面板；预览区画布拖拽换位留作后续（EDITSORT 字段列表拖拽已存在）。
7. **场景后端绑定（2026-07-17 补）**：AssistantHub 的 TOOLSET/PROMPTKEY 字面量已改读 SceneConfigService（assistant→system_general, form→system_form, optimize→meta_optimize_prompt，种子已配）；ToolRegistry.GetToolsForScene 未实现（真实链路不经 registry，场景解析直接由 SceneConfigService 完成）。
8. **测试调用（2026-07-17 补）**：RS_M24"测试调用"按钮以 **csharp 脚本自举**实现（SC_TEST_AI_TOOL：按行 ID 调 DeclarativeToolProvider），验证过端到端链路（csharp 脚本→声明式工具→tss_sql→结果回传）。
9. **冒烟测试修正（重要）**：.NET 程序集懒加载导致 Roslyn 引用缺失（CS0234）+ **Microsoft.CSharp 动态绑定缺失**（row.FIELD 取值报 CSharpArgumentInfo.Create），引擎已加强制加载两类程序集——所有含动态取值的脚本（如 SC_SCRIPT_CHECK）依赖此修复。

### 11.5 待办（下期）

- ~~修复 create_funcpoints 工具的 FUNCCODE→FUNCID bug~~（2026-07-18 已修）
- ~~版本中心：CHANGENOTE 保存时填写 + TAG 发布点打标 UI~~（2026-07-18 已落地，见 11.6）
- 向导 Step0 "从模板开始"：选中模板后 AI 基于模板 SCRIPT 增量修改（7.4 设计，未实施）
- 向导 Step3 工具集尚未纳入 define_sql_api/define_script_api（生成模块时不能顺带生成脚本接口，需事后在模块脚本补）
- APITYPE=script 接口编排（3.5 设计，明确预留）
- 3.7（csharp 详细设计）与 G8（版本管理详细设计）未单独成章，as-built 以 11.2/11.3/11.4/11.6 为准
- 发布中心：TAG 批量打标 + 按 TAG 出发布包（顺序前提"资源/模块定义版本管理"已于 2026-07-18 完成）
- UpgradeExecutor（.aidev.sql 部署通道）不做行级版本捕获（边界：脚本自身即版本工件）
- RS_M13（SQL 配置老页面）与统一代码资产体系重叠，下线评估
- hs2-java 侧同步评估（magic-api 已覆盖部分能力）

### 11.6 增量实施（2026-07-18）

**① 版本体系三大升级（G8 增强）**
- **保存/提交双态**：Store03 save 透传 CHANGENOTE/SKIPVERSION 保留键（DataController 三通道遍历跳过）；前端 save-actions 组件（保存=快速保存不留版本；提交=tooltip 浮层填说明+生成版本），IDE 与模块脚本弹窗统一接入
- **版本链连续**：v(n).BEFORE = v(n-1).AFTER（上一版本 AFTERCONTENT 优先，无历史回退查 DB 当前行），快速保存的中间改动折叠进下次提交差异
- **删除版本管理**：四类代码资产统一逻辑删除（ISDELETED=1 走 doSave，禁物理删除）；tss_code_asset 改生成列唯一键 uk_livepath（IF(ISDELETED=0,MODULEPATH,NULL)，已删行让出路径可重建）；Capture 识别 ISDELETED 0→1 为 OPTYPE=delete；回滚 delete 行在则 UPDATE 写回/行不在才 INSERT
- **AI/SQL 通道覆盖**：ChangeSetEngine.ExecuteConfirmed 提交后按 CATEGORY+METADATA 快照（12 类映射：physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/page/button）；DevVersionService.CaptureObjects 批量事后捕获（insert 幂等规范化：已有历史自动转 update）；cfg 增补 VSS_DICT/VSS_FUNC/VSS_FUNCPOINT。至此三条写入通道（页面保存/AI 变更集/代码资产编辑）全部闭环
- **通用版本历史弹窗**（version-history-popup + version-diff-view 组件化）：查询/该版本变化/与现在对比（A06 取当前快照）/回滚/标记（A07 TAG+PINNED）；入口：IDE 版本按钮、模块脚本弹窗版本按钮、版本中心行"历史"
- **diff 视图修复与优化**：TabPane→HeyUI Tabs(:datas)（原版根本不渲染）；日期格式归一（ISO vs MySQL 假变化消除）；长文本字段始终生成代码 tab（有变差异高亮/无变全量可见，COMPILEDCODE 降噪）

**② 统一代码资产收尾**
- 单接口取数：RS_M17/A01 F01 参数化（ASSETTYPE/CODE/NAME/CREATER/INPUT 模糊），前端按 ASSETTYPE 分组；SS_MOD_CODEFILES 三 union 统一输出 CODE+MODULEPATH
- ORDERBY 去表别名前缀规则落实（ORM 包装 SELECT * FROM (...) T ORDER BY，F01/FC1/FS1/FJ1 全改）
- IDE 文件树 VUE+JS 同组（路径嵌套树），csharp/sql 模块目录两层；树搜索补中文名匹配 + 匹配目录自动展开

**③ 编辑器统一与规范**
- 按钮钩子插入统一走 code-editor-popup（openJsInsert：methods/computed 锚点插入/整块新建/重复拦截/三种结果提醒）；JS 编码规范 {模块编码}_{页面编码}（deriveTplCode，如 LIB_M01_add，路径变更联动重推导）；新建资产按当前模块编码生成编码+路径（弹窗 + 按钮补传 moduleCode）
- SQL 保存校验剥注释（-- 与 /* */ 后再判头部关键字/DDL；单引号仍全文拦截——NVelocity 铁律）

**④ 修复**
- create_funcpoints FUNCCODE→FUNCID（存量 bug，AI 工具执行报 Unknown column）
- TemplateExporter 补 csharp 资产导出（SCRIPTCODE 引用收集，此前导出包漏 C# 脚本）+ sql 节补 ISDELETED=0
- D0701 字典补 code/dict/menu/permission 项；版本弹窗标签字典化

**⑤ AI 功能排查结论（2026-07-18）**
- 场景配置 6 条（aidev/assistant/form/optimize/sfc/wizard）+ AssistantHub 按场景读 TOOLSET/PROMPTKEY ✅
- 声明式工具 3 条 + SQLCODE 资产在位 + SC_TEST_AI_TOOL 自举验证链路 ✅
- 工具分发完整性：定义↔处理器 1:1，废弃工具有明确引导错误 ✅
- 向导 STEP_TOOL_MAP 前后端一致 ✅；LLM 配置（deepseek-chat + glm-4.6v，AES 加密）✅


---

## 十二、统一代码编辑器分层合并规划（2026-07-17，已实施）

### 12.1 背景

低代码扩展点目前有 4 类，编辑入口分散：

| 扩展点 | 载体 | 当前编辑入口 |
|--------|------|-------------|
| 扩展JS (PAGECONFIG.EXTENDJS) | tbs_sfc_template (.js) | sfc-editor-popup |
| Store 扩展 (@/modules/{MC}/store.js) | tbs_sfc_template (.js) | sfc-editor-popup |
| SFC Slot 扩展 (PAGECONFIG.SLOTS) | tbs_sfc_template (.vue) | sfc-editor-popup |
| 按钮钩子 (EXTPARAM.beforeAction/afterAction) | EXTENDJS 文件内方法名 | 无直达入口 |

同时 code-editor-popup 已统一 C# 脚本 + SQL 模板（左列表/目录/编译/保存/关联/选入/删除）。

### 12.2 决策：分层合并（JS 并、VUE 留）

sfc-editor-popup 的重资产 = VUE 预览 + 5 套 Slot 骨架模板 + AI 面板，全并等于重写。
JS 类资产语义与 code-editor-popup 完全一致，并入成本极低。

- **code-editor-popup**：C# 脚本 / SQL 模板 / **JS 模块**（EXTENDJS + Store 扩展 + 通用 JS）
- **sfc-editor-popup**：SLOTS (.vue) / 页面级 SFC（保留预览与模板）
- **入口统一路由**：m18 所有"编辑"按钮按文件类型分流（.js→code-editor-popup，.vue→sfc-editor-popup）

### 12.3 改动清单（已全部落地）

> 实施记录：kind='js' 已入 ASSET_META；saveAsset('js') 保存时重算 COMPILEDCODE+DEPS；SS_MOD_CODEFILES 第三 union(kind=3, FILETYPE=JS)；弹窗标题统一"模块脚本"、三组列表；m18 "接口脚本"按钮更名"模块脚本"，Store扩展/扩展JS 编辑已路由到 code-editor-popup.openJs（不存在则按约定路径新建，扩展JS 保存后自动回填 EXTENDJS 路径）；SLOTS/页面级 VUE 仍走 sfc-editor-popup。

原清单：

1. **ASSET_META 加 kind='js'**：storeCode=RS_M17；idField=ID；codeField=TEMPLATECODE；nameField=TEMPLATENAME；sourceField=SOURCECODE；pathField=MODULEPATH（路径即身份）；deptFields=DEPS/COMPILEDCODE
2. **code-asset.js**：openAsset/addAsset 接 RS_M17 DataTable；checkAsset('js')=compileSFC（返回 DEPS）；saveAsset('js') 保存时重算 COMPILEDCODE+DEPS（逻辑同 m17 edit.vue handleSave）
3. **SS_MOD_CODEFILES 第三 union（kind=3）**：tbs_sfc_template where MODULEPATH LIKE '%/modules/{MC}/%' OR MODULEPATH IN (module_page.SFCMODULEPATH)；弹窗左列表加 "JS 模块" 组，.vue 条目点击转 sfc-editor-popup
4. **目录字段语义**：js 资产显示/编辑 MODULEPATH（与 sfc-editor-popup 的 modulePath 输入一致）
5. **m18 入口**：EXTENDJS 编辑→code-editor-popup(kind='js')；Store扩展→code-editor-popup(kind='js', '@/modules/{MC}/store.js')；SLOTS 编辑按扩展名路由；按钮 EXTPARAM 钩子字段旁加"打开扩展JS"按钮
6. **本期不做**：VUE 预览/Slot 模板/AI 面板不迁移；sfc-editor-popup 不下线
7. **二期候选**：sfc-preview 嵌入 code-editor-popup → 全并下线 sfc-editor-popup
