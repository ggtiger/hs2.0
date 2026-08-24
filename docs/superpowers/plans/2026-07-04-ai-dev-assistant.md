# AI 开发助理 实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 AI 开发助理系统，能按华溯 ORM 元数据规范自动编排"新增/修改功能"所需的全部资源（表/视图/字段/UI/字典/过滤器/模块/API/菜单/权限/审批流），产出经人工确认的变更包，导出为升级脚本，生产环境通过升级模块导入执行并留档。

**Architecture:** 三层模型 —— 会话(DevSession)聚合一次开发过程，变更包(ChangeSet)承载结构化变更项（DRAFT→CONFIRMED→导出），升级包(UpgradePack)在生产环境导入执行。AI 通过 LLM + Function Calling 调用一组"开发工具"产出变更项草稿，用户逐项确认后才生成最终脚本。所有写操作以 `tss_aidev_*` 系列表留档，遵循 ORM 元数据铁律（字段大写无下划线、VSS_ 系统视图前缀、REFFIELDID 链向 TBS、过滤器三条铁律等）。

**Tech Stack:** 后端 .NET Core 2.2 + Dapper + 自研ORM + DeepSeek LLM (Function Calling)；前端 Vue 2.5 + HeyUI + Vuex 3；数据库 MySQL 5.7；复用 orm-metadata-generator skill 的全部铁律作为校验规则。

**设计基线（用户决策）**：
- 交互形态：对话式 + 实时预览面板
- 执行边界：生成草稿 + 人工确认执行
- 修改功能覆盖：加字段 / 改界面 / 加 API / 加审批流（四类全覆盖，需逆向解析）
- 技术路线：LLM + Function Calling 工具链
- 表前缀：`tss_`（系统功能域）；视图前缀：`VSS_`；字段命名：大写无下划线全连写
- 时序铁律：AI 产出 DRAFT 变更项 → 用户逐项 Accept → CONFIRMED 项汇总成可导出脚本

---

## 文件结构总览

### 后端 (netcore/)

```
Realso.WebAPI/
  Controllers/
    RMAIDevController.cs            # RS_MAIDEV 自定义接口 (A05-A12)
    RMAIDevUpgController.cs         # RS_MAIDEVUPG 自定义接口 (A05-A08)
  Services/
    AiDev/
      ChangeSetEngine.cs            # 聚合工具调用 → ChangeSet
      ChangeSetValidator.cs         # 套用 orm 铁律做静态校验
      ChangeSetExporter.cs          # ChangeSet → .aidev.sql 导出
      MetadataReader.cs             # 逆向读取现有元数据
      Llm/
        ILlmClient.cs               # LLM 抽象
        DeepSeekLlmClient.cs        # DeepSeek Function Calling
        ToolSchemaMapper.cs         # IDevTool → function schema
      Tools/
        IDevTool.cs                 # 工具基类
        TableTools.cs               # A 组: 表与字段
        UiConfigTools.cs            # B 组: 界面配置
        FilterSqlTools.cs           # C 组: 过滤器与SQL
        ModuleApiTools.cs           # D 组: 模块与API
        MenuPermTools.cs            # E 组: 菜单与权限
        BillFlowTools.cs            # F 组: 审批流
  Models/
    AiDev/
      ChangeSet.cs / ChangeItem.cs
      DevSession.cs
      UpgradePack.cs / UpgradeLog.cs
      ValidationReport.cs
```

### 前端 (p-admin/)

```
src/pages/s01/mAIDev/               # RS_MAIDEV 开发环境
  index.js / router.js / store.js
  views/
    main.vue                        # 会话列表
    workspace.vue                   # 对话+预览分栏(核心)
    detail.vue                      # 会话详情+导出
    components/
      ChatPanel.vue                 # 对话区
      ChangeItemTree.vue            # 变更项树(含Accept/Reject)
      ScriptPreview.vue             # 已确认脚本区
      PagePreview.vue               # 模拟页面渲染
      DictQuickPicker.vue           # 快速选字典/选择器

src/pages/s01/mAIDevUPG/            # RS_MAIDEVUPG 升级管理
  index.js / router.js / store.js
  views/
    main.vue                        # 升级记录列表
    import.vue                      # 导入页(上传→预览→执行)
    detail.vue                      # 详情(明细+快照diff+回滚)

src/api/
  aidev.js                          # AI开发助理 API 封装
```

### 数据库

6 张 `tss_` 物理表 + 6 个 `VSS_` 视图 + 2 个 ORM 模块元数据。

---

## Chunk 1: 数据库与元数据基座

**目标**：建好 6 张物理表、6 个 VSS 视图、2 个 ORM 模块（RS_MAIDEV / RS_MAIDEVUPG）的全部元数据注册。完成后两个模块的标准 CRUD（A01-A04）即可通过 ORM 跑通。

### Task 1.1: 创建 6 张物理表

**Files:**
- Create: `netcore/docs/sql/aidev/01_create_tables.sql`

- [ ] **Step 1: 编写建表 SQL**

```sql
-- 会话表
CREATE TABLE IF NOT EXISTS tss_aidev_session (
  ID varchar(50) NOT NULL,
  SESSIONCODE varchar(50) DEFAULT NULL,
  SESSIONNAME varchar(200) DEFAULT NULL,
  SESSIONTYPE varchar(16) DEFAULT NULL COMMENT 'NEW/MODIFY',
  TARGETMODULE varchar(64) DEFAULT NULL,
  INTENT text DEFAULT NULL,
  STATUS varchar(16) DEFAULT 'DRAFT' COMMENT 'DRAFT/GENERATING/REVIEWING/EXPORTED/ARCHIVED',
  CREATEDBY varchar(50) DEFAULT NULL,
  CREATEDATE datetime DEFAULT NULL,
  CLOSEDATE datetime DEFAULT NULL,
  CHANGESETID varchar(50) DEFAULT NULL,
  REMARK varchar(500) DEFAULT NULL,
  ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 变更包表
CREATE TABLE IF NOT EXISTS tss_aidev_changeset (
  ID varchar(50) NOT NULL,
  SESSIONID varchar(50) DEFAULT NULL,
  CHANGESETCODE varchar(50) DEFAULT NULL,
  TITLE varchar(200) DEFAULT NULL,
  SOURCE varchar(16) DEFAULT NULL COMMENT 'NEW/MODIFY',
  INTENT text DEFAULT NULL,
  VALIDATIONPASSED tinyint DEFAULT 0,
  VALIDATIONREPORT text DEFAULT NULL,
  ITEMCOUNT int DEFAULT 0,
  CREATEDATE datetime DEFAULT NULL,
  ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 变更项表
CREATE TABLE IF NOT EXISTS tss_aidev_changeitem (
  ID varchar(50) NOT NULL,
  CHANGESETID varchar(50) DEFAULT NULL,
  ITEMSEQ int DEFAULT NULL,
  CATEGORY varchar(32) DEFAULT NULL COMMENT 'physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/billflow',
  ACTION varchar(16) DEFAULT NULL COMMENT 'create/alter/update/delete',
  TOOL varchar(64) DEFAULT NULL,
  TARGET varchar(128) DEFAULT NULL,
  SQLCONTENT longtext DEFAULT NULL,
  METADATA text DEFAULT NULL,
  RATIONALE text DEFAULT NULL,
  WARNINGS text DEFAULT NULL,
  DEPENDSON varchar(500) DEFAULT NULL COMMENT '依赖的ITEMID列表,逗号分隔',
  ITEMSTATUS varchar(16) DEFAULT 'DRAFT' COMMENT 'DRAFT/CONFIRMED/REJECTED',
  CONFIRMEDBY varchar(50) DEFAULT NULL,
  CONFIRMEDDATE datetime DEFAULT NULL,
  CONFIRMORDER int DEFAULT NULL,
  ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 升级记录表
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade (
  ID varchar(50) NOT NULL,
  UPGRADECODE varchar(50) DEFAULT NULL,
  SESSIONCODE varchar(50) DEFAULT NULL,
  SESSIONNAME varchar(200) DEFAULT NULL,
  SESSIONTYPE varchar(16) DEFAULT NULL,
  TARGETMODULE varchar(64) DEFAULT NULL,
  INTENT text DEFAULT NULL,
  SCRIPTCONTENT longtext DEFAULT NULL,
  SCRIPTHASH varchar(64) DEFAULT NULL,
  ITEMCOUNT int DEFAULT 0,
  STATUS varchar(16) DEFAULT 'PENDING' COMMENT 'PENDING/RUNNING/SUCCESS/FAILED/ROLLEDBACK',
  EXECUTEDBY varchar(50) DEFAULT NULL,
  EXECUTEDATE datetime DEFAULT NULL,
  DURATIONMS int DEFAULT NULL,
  ERRORMSG text DEFAULT NULL,
  ROLLBACKSCRIPT longtext DEFAULT NULL,
  ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 升级日志表
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade_log (
  ID varchar(50) NOT NULL,
  UPGRADEID varchar(50) DEFAULT NULL,
  ITEMID varchar(50) DEFAULT NULL,
  ITEMCATEGORY varchar(32) DEFAULT NULL,
  ITEMACTION varchar(16) DEFAULT NULL,
  ITEMTARGET varchar(128) DEFAULT NULL,
  SQLSNIPPET text DEFAULT NULL,
  STATUS varchar(16) DEFAULT NULL,
  ERRORMSG text DEFAULT NULL,
  ROWSAFFECTED int DEFAULT NULL,
  EXECUTEDATE datetime DEFAULT NULL,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 升级快照表(可选,支持回滚)
CREATE TABLE IF NOT EXISTS tss_aidev_upgrade_snapshot (
  ID varchar(50) NOT NULL,
  UPGRADEID varchar(50) DEFAULT NULL,
  OBJECTTYPE varchar(32) DEFAULT NULL COMMENT 'TABLE/RESOURCE/RESFIELD/FUNC',
  OBJECTNAME varchar(128) DEFAULT NULL,
  SNAPSHOTBEFORE longtext DEFAULT NULL,
  SNAPSHOTAFTER longtext DEFAULT NULL,
  PRIMARY KEY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

- [ ] **Step 2: 执行建表**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 < netcore/docs/sql/aidev/01_create_tables.sql`
Expected: 6 张表创建成功

- [ ] **Step 3: 验证**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SHOW TABLES LIKE 'tss_aidev_%';"`
Expected: 6 行结果

- [ ] **Step 4: Commit**

```bash
git add netcore/docs/sql/aidev/01_create_tables.sql
git commit -m "feat(aidev): 创建 AI 开发助理 6 张物理表"
```

### Task 1.2: 注册 TBS 资源 + resfield

**Files:**
- Create: `netcore/docs/sql/aidev/02_register_tbs_resource.sql`

按 orm-metadata-generator skill 规范，为 6 张物理表注册 TBS 资源 + 全字段 resfield（RESOURCEANAME='A'，ID 字段 ISKEY=1 KEYGENTYPE=GUID）。

- [ ] **Step 1: 为每张表写 INSERT INTO tss_resource + tss_resfield**

参考已有 `TBS_SFC_TEMPLATE` 资源注册格式。每张表一个 TBS 资源 ID（如 `tss_aidev_session_001`），所有字段补全 FIELDANAME/FIELDTYPE/NULLABLE/FIELDLENGTH/COMMENTS/ISKEY/KEYGENTYPE/DEFAULTVALUE/ENTRYNUM。

- [ ] **Step 2: 执行 + 验证 RESOURCEANAME='A' 且 ID 字段 ISKEY=1**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT ID,RESOURCENAME,RESOURCEANAME FROM tss_resource WHERE RESOURCENAME LIKE 'TSS_AIDEV_%';"`
Expected: 6 行，RESOURCEANAME 全为 'A'

- [ ] **Step 3: Commit**

### Task 1.3: 注册 VSS 视图 + resfield（REFFIELDID 链向 TBS）

**Files:**
- Create: `netcore/docs/sql/aidev/03_register_vss_view.sql`

为 6 张表各注册一个 VSS_ DATAVIEW（TABLERESOURCEID 指向对应 TBS）。每个 VSS 字段 REFFIELDID 指向对应 TBS 字段。引用字段（CREATEDBY/EXECUTEDBY/CONFIRMEDBY）REFRESOURCEID 指向 TBS_EMP（`770072c4-0750-11ea-9e8d-00163e067045`），REFFIELDID 指向 TBS_EMP.EMPNAME（`936de29c-0750-11ea-9e8d-00163e067045`），UPFIELDID 指向本地 ID 字段。

- [ ] **Step 1: 编写 VSS 资源 + resfield INSERT**

遵循两条铁律：REFRESOURCEID 必须指向 TBS（TABLE 类型）；REFFIELDID 必须指向被引用 TBS 表的字段。

- [ ] **Step 2: 执行 + 验证 REFFIELDID 非空、ID 字段 ISKEY=1 KEYGENTYPE=GUID**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT FIELDNAME,REFFIELDID,ISKEY,KEYGENTYPE FROM tss_resfield WHERE RESOURCEID LIKE 'vss_aidev_%' ORDER BY RESOURCEID,ENTRYNUM;"`
Expected: 所有字段 REFFIELDID 非空，ID 字段 ISKEY=1

- [ ] **Step 3: Commit**

### Task 1.4: 注册 resuipc（UI 配置）

**Files:**
- Create: `netcore/docs/sql/aidev/04_register_resuipc.sql`

为 6 个 VSS 视图配置列表/查询/表单显示。状态字段 SELECTDATA 用字典"单据状态"或内联 `1:有效,0:无效`（仅简单标志）。CREATEDBY/EXECUTEDBY 用 autocomplete 选员工（`{"selType":"emp"}`）。

- [ ] **Step 1: 编写 resuipc INSERT**

每张表配：行号列(index) + 主字段列 + 状态列 + 操作列(action)。LISTSORT/QUERYSORT/EDITSORT 按需赋值。

- [ ] **Step 2: 执行 + 验证**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT LABELNAME,LISTSORT,QUERYSORT,EDITSORT,EDITTYPE,SELECTDATA FROM tss_resuipc WHERE RESOURCEID LIKE 'vss_aidev_%' ORDER BY RESOURCEID,EDITSORT,LISTSORT;"`
Expected: 关键字段都有排序值

- [ ] **Step 3: Commit**

### Task 1.5: 注册过滤器（三条铁律）

**Files:**
- Create: `netcore/docs/sql/aidev/05_register_filter.sql`

为每个 VSS 配 F00（单条 `A.ID = @ID`）+ F01（列表模糊搜索，必须 `1=1` 开头 + `@INPUT` 参数 + ORDERBY 不带表别名）。

- [ ] **Step 1: 编写 filter INSERT**

F01 模板：`1=1\n#if("$!{INPUT}"!="")\nAND (A.SESSIONNAME LIKE CONCAT('%',@INPUT,'%') OR A.SESSIONCODE LIKE CONCAT('%',@INPUT,'%'))\n#end\nAND A.ISDELETED = 0`，ORDERBY=`CREATEDATE DESC`。

- [ ] **Step 2: 执行 + 验证三条铁律**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT FILTERCODE,FILTERSQL,ORDERBY FROM tss_resfilter WHERE RESOURCEID LIKE 'vss_aidev_%';"`
Expected: 所有 F01 以 `1=1` 开头、含 `@INPUT`、ORDERBY 无 `A.` 前缀

- [ ] **Step 3: Commit**

### Task 1.6: 注册 RS_MAIDEV 模块（moudle + moudlepath + moudleapi + func + funcpoint）

**Files:**
- Create: `netcore/docs/sql/aidev/06_register_module_maidev.sql`

MODULECODE=`RS_MAIDEV`，UPFUNCID=`3e3c83ce2b3c475b82902478c89c27c0`（系统管理父菜单，与 RS_M17 同级）。moudlepath 配 QRY/QQRY/MAIN/SEL 四路径（指向 VSS_AIDEV_SESSION）。moudleapi 配 A01-A04 标准 CRUD（ACTIONCODE 非空，A01 的 PATHNAME=QRY APIPARAM=QQRY）。func 菜单 FUNCCODE=`RS_MAIDEV` OUTERURL=`s01/mAIDev`。funcpoint 配 A01/A03/A04。

- [ ] **Step 1: 编写模块元数据 INSERT**

- [ ] **Step 2: 执行 + 验证四路径 + ACTIONCODE 非空**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT PATHNAME FROM tss_moudlepath WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_MAIDEV'); SELECT APICODE,ACTIONCODE,APITYPE,PATHNAME,APIPARAM FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE='RS_MAIDEV');"`
Expected: QRY/QQRY/MAIN/SEL 四行；A01-A04 ACTIONCODE 非空

- [ ] **Step 3: Commit**

### Task 1.7: 注册 RS_MAIDEVUPG 模块

**Files:**
- Create: `netcore/docs/sql/aidev/07_register_module_maidevupg.sql`

MODULECODE=`RS_MAIDEVUPG`，UPFUNCID 同上。moudlepath 指向 VSS_AIDEV_UPGRADE。moudleapi 配 A01-A04。菜单 OUTERURL=`s01/mAIDevUPG`。

- [ ] **Step 1-3: 同 Task 1.6 流程，Commit**

### Task 1.8: 前端骨架（两个模块的 router/store/main.vue）

**Files:**
- Create: `src/pages/s01/mAIDev/index.js` `router.js` `store.js` `views/main.vue`
- Create: `src/pages/s01/mAIDevUPG/index.js` `router.js` `store.js` `views/main.vue`

router.js 用 `require.ensure` 懒加载，store.js 用 `createStore.getStore`，不能用 SelStore（参考 m025/m026 教训）。main.vue 用 RsTableList 渲染列表。

- [ ] **Step 1: 创建两个模块的目录骨架（参考 s01/m17 结构）**

- [ ] **Step 2: 启动前端验证列表能加载**

Run: `cd p-admin && npm run dev`，访问两个菜单
Expected: 列表页能打开、查询能返回数据（即使为空）

- [ ] **Step 3: Commit**

---

## Chunk 2: ChangeSet 引擎 + 校验器

**目标**：实现 ChangeSet 的生成、聚合、校验、确认、脚本汇总。完成后能在后端代码里调工具产出变更项、跑校验、按确认状态汇总脚本。本 Chunk 不接 LLM，先用硬编码工具调用验证引擎。

### Task 2.1: ChangeSet / ChangeItem 模型

**Files:**
- Create: `netcore/Realso.WebAPI/Models/AiDev/ChangeSet.cs`
- Create: `netcore/Realso.WebAPI/Models/AiDev/ChangeItem.cs`
- Create: `netcore/Realso.WebAPI/Models/AiDev/ValidationReport.cs`

- [ ] **Step 1: 编写 POCO 模型**

ChangeSet：SessionId、Title、Source、Intent、Items 列表、ValidationReport。
ChangeItem：Id、ChangeSetId、ItemSeq、Category、Action、Tool、Target、SqlContent、Metadata、Rationale、Warnings、DependsOn、ItemStatus、ConfirmedBy、ConfirmedDate、ConfirmOrder。
ValidationReport：Passed、Checks 列表（Rule/Status/Message）。

- [ ] **Step 2: Commit**

### Task 2.2: ChangeSetValidator（orm 铁律代码化）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/ChangeSetValidator.cs`

把 orm-metadata-generator skill 的全部铁律做成校验规则：RESOURCEANAME 非空、VCK/VSS ID 字段 ISKEY=1 KEYGENTYPE=GUID、过滤器三条铁律（1=1 开头/@INPUT/ORDERBY 无别名）、引用字段两条铁律（REFRESOURCEID 指向 TBS、REFFIELDID 指向被引用 TBS 字段）、moudleapi ACTIONCODE 非空、moudlepath 四路径齐全、字段名无下划线、字段名大写。

- [ ] **Step 1: 编写 IChangeSetValidator + 实现**

输入 ChangeSet，输出 ValidationReport。每条规则一个方法。

- [ ] **Step 2: 写单元测试覆盖每条铁律的正反例**

- [ ] **Step 3: Commit**

### Task 2.3: IDevTool 工具基类 + ToolSchemaMapper

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/Tools/IDevTool.cs`
- Create: `netcore/Realso.WebAPI/Services/AiDev/Llm/ToolSchemaMapper.cs`

IDevTool 接口：Name、Description、InputSchema（JSON Schema）、Execute(params)→ChangeItem。ToolSchemaMapper：把 IDevTool 列表转成 LLM 的 function 数组。

- [ ] **Step 1: 定义接口 + mapper**

- [ ] **Step 2: Commit**

### Task 2.4: A 组工具实现（TableTools）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/Tools/TableTools.cs`

实现 create_physical_table / add_field_to_table / modify_field / define_dataview / define_reference_field。每个工具产出 ChangeItem（含 SQLCONTENT + METADATA + RATIONALE + DEPENDSON）。define_reference_field 自动套用两条铁律生成正确的 REFRESOURCEID/REFRELATION/UPFIELDID。

- [ ] **Step 1: 实现 5 个工具**

- [ ] **Step 2: 写测试：create_physical_table 产出的 ChangeItem 通过 Validator**

- [ ] **Step 3: Commit**

### Task 2.5: B-G 组工具实现

**Files:**
- Create: `UiConfigTools.cs` `FilterSqlTools.cs` `ModuleApiTools.cs` `MenuPermTools.cs` `BillFlowTools.cs`

实现剩余工具组。FilterSqlTools 的 define_filter 自动校验三条铁律；ModuleApiTools 的 register_module 自动配齐四路径；MenuPermTools 的 create_funcpoints 只写 A01 等编码（前端自动拼 FUNCCODE）。

- [ ] **Step 1: 逐组实现 + 测试**

- [ ] **Step 2: Commit**

### Task 2.6: MetadataReader（逆向读取工具）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/MetadataReader.cs`

实现 read_module_metadata / read_table_schema / search_existing_resource。用 `DB.GetDBHelper()` 直接 SQL（参考 Word 模板编辑器教训，避免 ORM Query 接口无认证上下文返回空）。

- [ ] **Step 1: 实现三个读取方法**

- [ ] **Step 2: 测试读取 RS_M11 模块能返回完整元数据全景**

- [ ] **Step 3: Commit**

### Task 2.7: ChangeSetEngine（聚合 + 确认 + 脚本汇总）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/ChangeSetEngine.cs`

方法：AppendItem（AI 产出 DRAFT 项）、ConfirmItem（DRAFT→CONFIRMED，写 CONFIRMEDBY/DATE/ORDER）、RejectItem、UnconfirmItem、GetConfirmedScript（按 CONFIRMORDER+DEPENDSON 拓扑排序汇总 SQLCONTENT）。

- [ ] **Step 1: 实现引擎**

- [ ] **Step 2: 测试：append 3 项 → confirm 2 项 → GetConfirmedScript 只返回 2 项且按依赖排序**

- [ ] **Step 3: Commit**

---

## Chunk 3: LLM 接入 + RMAIDevController

**目标**：接 DeepSeek Function Calling，实现 RS_MAIDEV 的自定义接口（A05 generate / A06 validate / A07 export / A08 apply / A09 confirm / A10 reject / A11 unconfirm / A12 getScript）。完成后用户能在前端发起对话、AI 产出变更项、用户确认、导出脚本。

### Task 3.1: ILlmClient + DeepSeekLlmClient

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/Llm/ILlmClient.cs`
- Create: `netcore/Realso.WebAPI/Services/AiDev/Llm/DeepSeekLlmClient.cs`

ILlmClient：Chat(messages, tools)→Response（含 tool_calls）。DeepSeekLlmClient 用 HttpClient 调 DeepSeek API，支持 Function Calling 循环（LLM 返回 tool_call → 执行工具 → 把结果喂回 → 继续直到 LLM 返回 final）。

- [ ] **Step 1: 实现客户端 + Function Calling 循环**

- [ ] **Step 2: 用一个简单工具（如 get_current_time）测试循环能跑通**

- [ ] **Step 3: Commit**

### Task 3.2: RMAIDevController（A05-A12）

**Files:**
- Create: `netcore/Realso.WebAPI/Controllers/RMAIDevController.cs`

继承 DataController，重写 doMyApi，switch APICODE 处理 8 个自定义接口。A05 generate 内部：读会话上下文 → 调 LLM + 工具链 → 调 ChangeSetEngine.AppendItem → 跑 Validator → 返回变更项。A07 export 调 ChangeSetExporter。

- [ ] **Step 1: 实现 Controller 骨架 + 8 个 case**

- [ ] **Step 2: Startup.cs 注册 Controller**

- [ ] **Step 3: 用 Postman 测 A05 能产出 DRAFT 变更项入库**

- [ ] **Step 4: Commit**

### Task 3.3: ChangeSetExporter（导出 .aidev.sql）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/ChangeSetExporter.cs`

把 CONFIRMED 状态的变更项按依赖拓扑排序，输出带元数据头（@META）、前置幂等检查、分节 SQL、后置校验的 .aidev.sql 文件。所有 DDL 用 IF NOT EXISTS，所有 INSERT 用 WHERE NOT EXISTS。

- [ ] **Step 1: 实现导出器**

- [ ] **Step 2: 测试导出 RS_M11 风格的变更包能被 mysql 直接执行**

- [ ] **Step 3: Commit**

### Task 3.4: 前端 workspace.vue（对话+预览分栏）

**Files:**
- Create: `src/pages/s01/mAIDev/views/workspace.vue`
- Create: `src/pages/s01/mAIDev/views/components/ChatPanel.vue`
- Create: `src/pages/s01/mAIDev/views/components/ChangeItemTree.vue`
- Create: `src/pages/s01/mAIDev/views/components/ScriptPreview.vue`

ChatPanel 发起对话调 A05；ChangeItemTree 渲染变更项树（每项 checkbox + Accept/Reject 按钮，调 A09/A10）；ScriptPreview 实时调 A12 getScript 渲染已确认脚本区 + 导出按钮（调 A07 下载）。

- [ ] **Step 1: 实现四组件 + store actions**

- [ ] **Step 2: 端到端验证：对话→AI产出DRAFT项→Accept→脚本区更新→导出**

- [ ] **Step 3: Commit**

---

## Chunk 4: 升级管理模块（RS_MAIDEVUPG）

**目标**：实现生产环境的脚本导入、预览、执行、回滚、留档。完成后开发环境导出的 .aidev.sql 能在生产环境完整跑通升级闭环。

### Task 4.1: RMAIDevUpgController（A05-A08）

**Files:**
- Create: `netcore/Realso.WebAPI/Controllers/RMAIDevUpgController.cs`

A05 import：上传脚本 + 预解析元数据头 + 幂等检查（查 tss_aidev_upgrade 是否已有该 SESSIONCODE 且 SUCCESS）+ 入库 PENDING。A08 preview：解析变更项返回前端预览。A06 execute：生成快照 → 事务执行 → 写 log → 状态置 SUCCESS/FAILED。A07 rollback：走 ROLLBACKSCRIPT。

- [ ] **Step 1: 实现 Controller**

- [ ] **Step 2: Startup.cs 注册**

- [ ] **Step 3: Commit**

### Task 4.2: UpgradeExecutor（事务+快照+日志）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/AiDev/UpgradeExecutor.cs`

执行流程：① 读脚本元数据头 ② 幂等检查 ③ 对受影响对象 SHOW CREATE TABLE + SELECT 元数据行存 snapshot ④ 生成 ROLLBACKSCRIPT（CREATE→DROP、INSERT→DELETE、ALTER ADD→DROP COLUMN） ⑤ 单事务执行 ⑥ 逐项写 upgrade_log ⑦ 状态置位。

- [ ] **Step 1: 实现执行器**

- [ ] **Step 2: 测试：导入一个建表脚本 → 执行成功 → 升级记录 SUCCESS + log 有明细**

- [ ] **Step 3: 测试：制造失败（语法错误SQL）→ 事务回滚 → 状态 FAILED + ERRORMSG**

- [ ] **Step 4: 测试：SUCCESS 后回滚 → 走 ROLLBACKSCRIPT → 表被 DROP**

- [ ] **Step 5: Commit**

### Task 4.3: 前端 import.vue + detail.vue

**Files:**
- Create: `src/pages/s01/mAIDevUPG/views/import.vue`
- Create: `src/pages/s01/mAIDevUPG/views/detail.vue`

import.vue：上传按钮 → 调 A05 → 预览变更项（调 A08）→ 执行按钮（调 A06）。detail.vue：展示 upgrade_log 子表 + 快照 diff（SNAPSHOTBEFORE/AFTER 用代码高亮组件）+ 回滚按钮（调 A07）。

- [ ] **Step 1: 实现两页面**

- [ ] **Step 2: 端到端：开发环境导出 → 拷贝到生产环境 → 导入 → 预览 → 执行 → 详情查看**

- [ ] **Step 3: Commit**

---

## Chunk 5: 修改功能逆向解析（四类场景）

**目标**：覆盖用户要求的四类修改场景：给已有表加字段 / 改界面配置 / 加自定义 API / 加审批流。AI 能读现有元数据 → 理解 → 产出正确的修改类变更项。

### Task 5.1: 加字段场景的 Prompt + 工具编排

**Files:**
- Modify: `RMAIDevController.cs` 的 A05（按 SESSIONTYPE=MODIFY 分支编排）

AI 流程：read_table_schema → search_existing_resource → add_field_to_table（产出 ALTER + resfield + resuipc）→ 问是否同步到 VCK → 若是则 define_dataview 扩展字段。

- [ ] **Step 1: 编写 MODIFY 场景的系统 prompt + 工具编排逻辑**

- [ ] **Step 2: 测试：对话"给 tbs_equip_calib 加一个 CALIBRESULT 字段"→ 产出 ALTER + resfield + resuipc**

- [ ] **Step 3: Commit**

### Task 5.2: 改界面配置场景

**Files:**
- Modify: `RMAIDevController.cs`

AI 流程：read_module_metadata 拿全部 resuipc → LLM 理解当前布局 → 输出 UPDATE tss_resuipc 语句（改 EDITTYPE/SELECTDATA/LISTSORT 等）。

- [ ] **Step 1-3: 同上**

### Task 5.3: 加自定义 API 场景

**Files:**
- Modify: `RMAIDevController.cs`
- Create: `netcore/Realso.WebAPI/Services/AiDev/Tools/ControllerCodeGenerator.cs`

AI 流程：读现有 Controller 看是否已有 RMxxController → 没有则生成新建骨架代码片段（继承 DataController + doMyApi switch）→ 有则生成追加 case 代码片段 → 同时 define_api 配 moudleapi。

- [ ] **Step 1-3: 同上**

### Task 5.4: 加审批流场景

**Files:**
- Modify: `RMAIDevController.cs`

AI 流程：读模块当前是否有 STATE 字段 → 没有则 add_field_to_table 加 STATE → enable_bill_flow 配 BillState/BillFlow + A12/A14/A16 等 API + funcpoint。

- [ ] **Step 1-3: 同上**

---

## Chunk 6: 安全、审计、收尾

**目标**：补全幂等、防篡改、权限、归档等生产级保障。

### Task 6.1: 脚本 HASH 防篡改

**Files:**
- Modify: `UpgradeExecutor.cs`

导入时计算 SCRIPTHASH（SHA256），执行前比对登记值。

- [ ] **Step 1: 实现 + 测试**

### Task 6.2: 权限点精细化

**Files:**
- Modify: `06_register_module_maidev.sql` `07_register_module_maidevupg.sql`

RS_MAIDEV 配 A01 查询/A05 生成/A07 导出/A08 执行；RS_MAIDEVUPG 配 A01 查询/A05 导入/A06 执行/A07 回滚。绑定到运维角色 + 开发管理员角色。

- [ ] **Step 1: 补 funcpoint + 角色绑定 SQL**

### Task 6.3: 会话归档 + 导出锁定

**Files:**
- Modify: `ChangeSetEngine.cs` `RMAIDevController.cs`

导出后会话 STATUS=EXPORTED，changeitem 状态冻结，不能再 confirm/unconfirm。

- [ ] **Step 1: 实现冻结逻辑 + 测试**

### Task 6.4: 文档

**Files:**
- Create: `docs/ai-dev-assistant-guide.md`

写使用文档：如何开会话、如何对话生成、如何确认、如何导出、如何生产导入执行回滚。

- [ ] **Step 1: 写文档**

---

## 执行顺序建议

Chunk 1 → 2 → 3 → 4 → 5 → 6。每个 Chunk 产出可独立验证的成果：
- Chunk 1 完成：两个模块列表页能跑、CRUD 通
- Chunk 2 完成：后端能代码调用工具产出+校验变更项
- Chunk 3 完成：前端对话→生成→确认→导出全链路通
- Chunk 4 完成：开发→生产升级闭环通
- Chunk 5 完成：四类修改场景覆盖
- Chunk 6 完成：生产级安全审计

**最小可用版（MVP）**：Chunk 1+2+3+4。Chunk 5 和 6 可后续迭代。
