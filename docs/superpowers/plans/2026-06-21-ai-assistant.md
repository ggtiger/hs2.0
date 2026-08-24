# 华溯 LIMS 智能助理 实现计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为华溯 LIMS 构建一个全功能智能助理，用户用自然语言查询数据、分析、找模块、跳转、新增/修改/删除/审批单据，并通过图表/HTML/表单交付富结果。

**Architecture:** ReAct 多步循环 Agent（DeepSeek function calling），后端经现有 `DataController.Call` 入口的抽取服务 `DataCallService` 操作系统、继承用户权限；SSE 流式推送统一消息块；前端右侧全局浮动抽屉渲染；元数据驱动（运行时读 `tss_func`/`tss_moudle`/`tss_resfield`/`tss_resuipc`），零硬编码模块。

**Tech Stack:** 后端 .NET Core 2.2 + 自研元数据 ORM；前端 Vue 2.5 + HeyUI + Vuex + Jest；LLM DeepSeek（OpenAI 兼容）；MySQL 5.7。

**关联 spec:** `docs/superpowers/specs/2026-06-21-ai-assistant-design.md`（已评审通过）

> **⚠️ 实现状态（2026-06-22 更新）**
> - **M1 完成、M2 完成。** M3-M6 未开始。
> - **传输层 SSE → SignalR**（见 spec 调整说明）。Chunk 1 Task 1.9 的 `AssistantController /send`(SSE) 已被 `AssistantHub.Ask`(SignalR ReAct 循环) 取代；Task 1.11 前端 `api/assistant.js` 用 `@aspnet/signalr` 而非 fetch。新增 `Hubs/AssistantHub.cs`、`Services/AssistantToolExecutor.cs`、`Services/DataCallService.cs`。
> - **M2 关键实现**：`DataCallService.QueryCore` 为共享查询核心，`DataController.doQuery` 非导出路径已委托；`get_module_schema` 结构化返回接口/过滤器参数/字段；4 个只读工具 + ReAct 循环 + 前端 `ToolCallBlock` 均完成。
> - 测试：`Realso.Assistant.Test` xUnit 19 通过；前端 Jest（需 `testURL` 修复后）。
> - 后续 M3-M6 需各自展开为完整计划再实现。

---

## 范围与计划结构说明

本项目跨 6 个里程碑、后端+前端+数据库，体量大。本计划文档采用如下结构：

- **Chunk 1（M1 地基）**：完整可执行的 TDD 微步骤计划，是所有后续里程碑的基础，单独交付可用软件（发消息→流式回复→用量入库）。
- **Chunk 2（M2–M6 路线图）**：每个里程碑给出文件清单 + 关键任务 + 测试要点的大纲。每完成一个里程碑后展开为独立完整计划。

**计划级排序调整（相对 spec）：** spec 把 `DataCallService` 抽取列在 M1，但 M1 验证标准不需要它。本计划将 `DataCallService` 抽取移到 **M2 第一个任务**（首次需要时），让 M1 聚焦"管道"。

**测试策略：**
- 后端新建 **`Realso.Assistant.Test`** xUnit 项目（见 Task 1.2）。现有 `Realso.Test` 是 Exe 控制台应用、无 xunit/TestSdk、不引用 WebAPI，不能直接用。
- 前端用 **Jest**（`test/unit` 已配置）。
- 集成点（真实 SSE flush、真实 LLM 调用、真实 DB）用手动/端到端验证。

**关键代码约定（已核对源码，实现时遵循）：**
- 用户身份：`BaseControl.userInfo` 来自表单字段 `_userInfo_`（经 `HashtableBinder` 用 `JsonConvert.DeserializeObject<Hashtable>` 解析）。键名是 **`ID`** 和 **`NICKNAME`**（不是 NAME）。
- 前端 token 在 `store.state.user.access_token`，userInfo 在 `store.state.user.userInfo`（db.js 行 122/126）。
- Vuex 全局模块在 `store/index.js` 的 `modules:{app, user}` **静态注册**（非 `createStore.getStore`，后者用于依赖 RS_M00 的页面模块）。
- DB 访问：`DBHelper helper = DB.GetDBHelper();`（参考 `WordTemplateController`）；具体查询/执行方法名以 `Realso.Data.DBAccess` 实际 API 为准，实现时对照 `WordTemplateController` 用法确认。
- 全局 JSON 用 `DefaultContractResolver`（PascalCase）；SSE 消息块**单独用 camelCase** 序列化（见 SseWriter）。

**VCS 说明：** 当前工作目录**未初始化 git**。"Commit" 步骤作为**逻辑检查点**。如需真实版本控制先 `git init`；否则跳过命令，仅以检查点划分进度。

---

## File Structure（全项目文件清单与职责）

### 后端新增（`netcore/`）

| 文件 | 职责 | 里程碑 |
|---|---|---|
| `Realso.Assistant.Test/`（新项目） | 助理后端 xUnit 测试 | M1 |
| `Realso.WebAPI/Utils/AesHelper.cs` | AES 对称加解密 | M1 |
| `Realso.WebAPI/Services/LlmConfigService.cs` | 读启用的 LLM 配置 + AES 解密 + Key 脱敏 | M1 |
| `Realso.WebAPI/Services/SseWriter.cs` | SSE 消息块序列化（camelCase） | M1 |
| `Realso.WebAPI/Services/UsageLogger.cs` | LLM 用量记录 + 费用计算 | M1 |
| `Realso.WebAPI/Services/DeepSeekClient.cs` | DeepSeek API 调用（流式 + usage 解析） | M1 |
| `Realso.WebAPI/Services/SessionStore.cs` | 会话上下文（内存缓存 + DB 持久） | M1 |
| `Realso.WebAPI/Controllers/AssistantController.cs` | 助理主 Controller（`/send` SSE、`/confirm`、`/form-submit`、ReAct 循环） | M1-M5 |
| `Realso.WebAPI/Services/DataCallService.cs` | 从 DataController 抽取的统一数据操作 | M2 |
| `Realso.Core` 或 `Realso.WebAPI/Models/UserContext.cs` | 当前用户上下文 | M2 |
| `Realso.WebAPI/Services/AssistantToolExecutor.cs` | 8 工具实现与分发 | M2-M5 |
| `Realso.WebAPI/Services/ConfirmGate.cs` | 确认门暂停/恢复 | M4 |
| `Realso.WebAPI/Services/AuditLogger.cs` | 助理写操作审计 | M4 |
| `Realso.WebAPI/Controllers/LLMConfigController.cs` | LLM 配置 CRUD（加密/脱敏） | M6 |

### 后端修改

| 文件 | 修改 | 里程碑 |
|---|---|---|
| `Realso.WebAPI/appsettings.json` | 加 `Assistant.AesKey`（在现有 JSON 对象内新增键） | M1 |
| `Realso.sln` | 加入 `Realso.Assistant.Test` 项目 | M1 |
| `Realso.WebAPI/Startup.cs` | 注册 AssistantController 路由 + 注入服务 | M1 |
| `Realso.WebAPI/Controllers/DataController.cs` | doXxx 核心抽到 DataCallService（行为不变） | M2 |

### 前端新增（`p-admin/src/`）

| 文件 | 职责 | 里程碑 |
|---|---|---|
| `store/modules/assistant.js` | Vuex 模块（namespaced，静态注册） | M1 |
| `api/assistant.js` | 助理 SSE API 封装 | M1 |
| `components/assistant/AssistantDrawer.vue` | 抽屉容器 + 浮动按钮 | M1 |
| `components/assistant/AssistantMessageList.vue` | 消息流按 block.type 分发 | M1 |
| `components/assistant/AssistantInput.vue` | 输入框 + 发送 | M1 |
| `components/assistant/blocks/TextBlock.vue` | Markdown 文字 | M1 |
| `components/assistant/blocks/ThinkingBlock.vue` | 思考过程（可折叠） | M1 |
| `components/assistant/blocks/ToolCallBlock.vue` | 工具调用卡片 | M2 |
| `components/assistant/blocks/{Chart,Html,Form}Block.vue` | 富内容 | M3 |
| `components/assistant/blocks/ConfirmBlock.vue` | 写操作确认门 | M4 |
| `pages/s01/m14/*`（LLM 配置）、`pages/s01/m15/*`（用量统计） | 管理页（标准模块结构） | M6 |

### 前端修改

| 文件 | 修改 | 里程碑 |
|---|---|---|
| `App.vue` | 挂载 AssistantDrawer（**保留现有 SignalR 代码**） | M1 |
| `store/index.js` | `modules: { app, user, assistant }` 加 assistant | M1 |
| `package.json` | 新增 `marked`、`dompurify` | M1 |
| 各业务 `main.vue`/`add.vue` | 支持 `query.id` 自动打开单据 | M5 |

### 数据库（MySQL D0001）

| 表/视图 | 里程碑 |
|---|---|
| `TBS_ASSISTANT_CONVERSATION`、`TBS_ASSISTANT_MESSAGE`、`TBS_LLM_CONFIG`、`TBS_LLM_USAGE` | M1 |
| `TBS_ASSISTANT_AUDIT` | M4 |
| `VRP_LLM_USAGE_BY_USER` | M6 |

---

## Chunk 1: M1 地基（完整可执行）

> **M1 目标**：发消息、DeepSeek 流式回复（暂无工具）、用量入库。
> **M1 验证标准**：前端抽屉发"你好"→ 后端调 DeepSeek → SSE 流式逐字返回 → 回复显示（真实用户名入库，非 anonymous）→ `TBS_LLM_USAGE` 有一条 token/费用记录。

### Task 1.1: 数据库表 + 种子配置 + appsettings

**Files:**
- Create: `docs/sql/assistant_m1.sql`
- Modify: `netcore/Realso.WebAPI/appsettings.json`（在现有 JSON 内新增 `Assistant` 键，勿整体替换）

字段规范：大写无下划线、`ISDELETED` tinyint 默认 0、ID 用 char(36)。

- [ ] **Step 1: 写建表 SQL**

```sql
CREATE TABLE IF NOT EXISTS TBS_ASSISTANT_CONVERSATION (
  ID char(36) NOT NULL, USERID varchar(64) DEFAULT NULL, USERNAME varchar(64) DEFAULT NULL,
  TITLE varchar(200) DEFAULT NULL, CREATETIME datetime DEFAULT NULL, UPDATETIME datetime DEFAULT NULL,
  ISDELETED tinyint DEFAULT 0, PRIMARY KEY (ID));
CREATE TABLE IF NOT EXISTS TBS_ASSISTANT_MESSAGE (
  ID char(36) NOT NULL, CONVERSATIONID char(36) NOT NULL, ROLE varchar(20) DEFAULT NULL,
  CONTENT text, BLOCKSJSON text, CREATETIME datetime DEFAULT NULL, ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID), KEY IDX_CONV (CONVERSATIONID, CREATETIME));
CREATE TABLE IF NOT EXISTS TBS_LLM_CONFIG (
  ID char(36) NOT NULL, PROVIDER varchar(32) DEFAULT NULL, APIKEY varchar(512) DEFAULT NULL,
  MODELNAME varchar(64) DEFAULT NULL, BASEURL varchar(255) DEFAULT NULL,
  PRICEINPUT decimal(10,6) DEFAULT 0, PRICEOUTPUT decimal(10,6) DEFAULT 0, PARAMS text,
  ENABLED tinyint DEFAULT 0, ISDELETED tinyint DEFAULT 0, PRIMARY KEY (ID));
CREATE TABLE IF NOT EXISTS TBS_LLM_USAGE (
  ID char(36) NOT NULL, USERID varchar(64) DEFAULT NULL, USERNAME varchar(64) DEFAULT NULL,
  CONVERSATIONID char(36) DEFAULT NULL, MODULECODE varchar(64) DEFAULT NULL, TOOLNAME varchar(64) DEFAULT NULL,
  OPERATIONTYPE varchar(32) DEFAULT NULL, PROMPTTOKENS int DEFAULT 0, COMPLETIONTOKENS int DEFAULT 0,
  TOTALTOKENS int DEFAULT 0, COST decimal(10,4) DEFAULT 0, DURATIONMS int DEFAULT 0,
  ISSUCCESS tinyint DEFAULT 0, ERRORMSG text, REQUESTTIME datetime DEFAULT NULL, ISDELETED tinyint DEFAULT 0,
  PRIMARY KEY (ID), KEY IDX_USER_TIME (USERID, REQUESTTIME));
```

- [ ] **Step 2: 元数据注册推迟到 M6（本步仅占位说明，不阻塞 M1）**

M1 用 Dapper 直查这四张表，不走元数据驱动 ORM，故元数据（tss_resource/tss_resfield）注册随 M6 管理页一起做。在此记录决策，无需 SQL。

- [ ] **Step 3: 写种子配置 SQL**（APIKEY 用 Task 1.3 的 AesHelper 生成密文后替换 `<CIPHER>`）

```sql
INSERT INTO TBS_LLM_CONFIG (ID, PROVIDER, APIKEY, MODELNAME, BASEURL, PRICEINPUT, PRICEOUTPUT, PARAMS, ENABLED, ISDELETED)
VALUES ('cfg_deepseek_001','DeepSeek','<CIPHER>','deepseek-chat','https://api.deepseek.com/v1',
        0.002,0.008,'{"temperature":0.3,"max_tokens":4096}',1,0);
```

- [ ] **Step 4: 执行建表 SQL**

Run: `docker exec -i labone-mysql mysql -ulabone -plabone123 D0001 < docs/sql/assistant_m1.sql`
Expected: 无报错；`docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SHOW TABLES LIKE 'TBS_ASSISTANT%'"` 返回 2 行（CONVERSATION/MESSAGE）+ `SHOW TABLES LIKE 'TBS_LLM_%'` 返回 2 行。

- [ ] **Step 5: 在 appsettings.json 现有 JSON 内加 Assistant 段**

```json
"Assistant": { "AesKey": "0123456789abcdef0123456789abcdef" }
```
（32 字节；生产换强随机值，单独管理不入库。）

- [ ] **Step 6: Commit / 检查点** — "M1: 建表 + appsettings"

---

### Task 1.2: 测试项目 Realso.Assistant.Test 搭建

> 现有 `Realso.Test` 是 Exe 控制台应用（无 xunit/TestSdk，不引用 WebAPI），不能用于本项目的 TDD。新建独立测试项目。

**Files:**
- Create: `netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj`
- Modify: `netcore/Realso.sln`

- [ ] **Step 1: 用模板创建 xUnit 测试项目**

Run: `cd netcore && dotnet new xunit -o Realso.Assistant.Test -f netcoreapp2.2`
Expected: 生成 `Realso.Assistant.Test.csproj`（含 `Microsoft.NET.Test.Sdk` + `xunit` + `xunit.runner.visualstudio`）+ `UnitTest1.cs`。

- [ ] **Step 2: 加 Realso.WebAPI 项目引用**

Run: `cd netcore/Realso.Assistant.Test && dotnet add reference ../Realso.WebAPI/Realso.WebAPI.csproj`
Expected: csproj 出现 `<ProjectReference Include="..\Realso.WebAPI\...">`。

- [ ] **Step 3: 加入解决方案**

Run: `cd netcore && dotnet sln Realso.sln add Realso.Assistant.Test/Realso.Assistant.Test.csproj`
Expected: `dotnet sln Realso.sln list` 含 Realso.Assistant.Test。

- [ ] **Step 4: 删除模板默认 UnitTest1.cs，建测试目录**

Run: `rm netcore/Realso.Assistant.Test/UnitTest1.cs && mkdir -p netcore/Realso.Assistant.Test/Assistant`

- [ ] **Step 5: 冒烟——写一个空通过测试确认测试基建可用**

Create: `netcore/Realso.Assistant.Test/Assistant/SanityTests.cs`
```csharp
using Xunit;
namespace Realso.Assistant.Test.Assistant { public class SanityTests { [Fact] public void Ok() { Assert.True(true); } } }
```
Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj`
Expected: PASS（1 个）。

- [ ] **Step 6: Commit / 检查点** — "M1: 测试项目 Realso.Assistant.Test"

---

### Task 1.3: AES 加密助手（TDD）

**Files:**
- Create: `netcore/Realso.WebAPI/Utils/AesHelper.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/AesHelperTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using Realso.WebAPI.Utils;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class AesHelperTests
    {
        const string Key = "0123456789abcdef0123456789abcdef";
        [Fact]
        public void Encrypt_Decrypt_RoundTrip()
        {
            var plain = "sk-deepseek-abcdef123456";
            var cipher = AesHelper.Encrypt(plain, Key);
            Assert.NotEqual(plain, cipher);
            Assert.Equal(plain, AesHelper.Decrypt(cipher, Key));
        }
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj --filter AesHelper`
Expected: FAIL（AesHelper 未定义）

- [ ] **Step 3: 实现 AesHelper**（AES-256-CBC，IV 前置，Base64）

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace Realso.WebAPI.Utils
{
    public static class AesHelper
    {
        public static string Encrypt(string plain, string key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.GenerateIV();
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        var b = Encoding.UTF8.GetBytes(plain);
                        cs.Write(b, 0, b.Length); cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        public static string Decrypt(string cipherBase64, string key)
        {
            var all = Convert.FromBase64String(cipherBase64);
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                var iv = new byte[16]; Array.Copy(all, 0, iv, 0, 16); aes.IV = iv;
                using (var cs = new CryptoStream(new MemoryStream(all, 16, all.Length - 16), aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8)) { return sr.ReadToEnd(); }
            }
        }
    }
}
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj --filter AesHelper`
Expected: PASS

- [ ] **Step 5: 用真实密钥生成种子密文**

Run（临时）：在 Sanity 测试里临时调 `AesHelper.Encrypt("sk-你的真实key", Key)` 打印密文，填入 Task 1.1 Step 3 的 `<CIPHER>` 并执行 INSERT。
Expected: DB 中 `TBS_LLM_CONFIG` 有一条 ENABLED=1 记录。

- [ ] **Step 6: Commit / 检查点** — "M1: AesHelper + 种子密文"

---

### Task 1.4: LLM 配置读取 + Key 脱敏（TDD）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/LlmConfigService.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/LlmConfigServiceTests.cs`

- [ ] **Step 1: 写失败测试**（脱敏纯函数；GetEnabled 留 Step 5 手动集成验证）

```csharp
using Realso.WebAPI.Services;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class LlmConfigServiceTests
    {
        [Theory]
        [InlineData("sk-deepseek-abcdef123456", "sk-****...3456")]
        [InlineData("sk-ab", "sk-****")]
        public void Mask(string input, string expected) => Assert.Equal(expected, LlmConfigService.Mask(input));
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj --filter LlmConfig`
Expected: FAIL

- [ ] **Step 3: 实现 LlmConfigService**（DBHelper 用法对照 WordTemplateController；方法名以 DBAccess 实际 API 为准）

```csharp
using System;
using Realso.Data.DBAccess;   // DB / DBHelper
using Realso.WebAPI.Utils;
namespace Realso.WebAPI.Services
{
    public class LlmConfig
    {
        public string Provider, ApiKeyPlain, ModelName, BaseUrl, Params;
        public decimal PriceInput, PriceOutput;
    }
    // 与 TBS_LLM_CONFIG 列一一对应
    public class LlmConfigRow { public string ID, PROVIDER, APIKEY, MODELNAME, BASEURL, PARAMS; public decimal PRICEINPUT, PRICEOUTPUT; public int ENABLED; }

    public class LlmConfigService
    {
        private readonly string _aesKey;
        public LlmConfigService(string aesKey) { _aesKey = aesKey; }

        public static string Mask(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var prefix = key.Length >= 2 ? key.Substring(0, 2) : key;
            if (key.Length <= 6) return prefix + "****";
            return prefix + "****..." + key.Substring(key.Length - 4);
        }

        public LlmConfig GetEnabled()
        {
            DBHelper helper = DB.GetDBHelper();   // 模式同 WordTemplateController
            // 查询方法名以 Realso.Data.DBAccess 实际 API 为准（如 helper.Query<LlmConfigRow>(sql).FirstOrDefault()）
            var rows = helper.Query<LlmConfigRow>(
                "SELECT * FROM TBS_LLM_CONFIG WHERE ENABLED=1 AND ISDELETED=0 LIMIT 1");
            var row = rows != null ? (rows as IEnumerable<LlmConfigRow>) : null;
            // ↑ 若 DBAccess 返回 IList，直接 FirstOrDefault。实现时按实际签名调整。
            return null; // 见 Step 4 完整实现
        }
    }
}
```

- [ ] **Step 4: 补全 GetEnabled 实现**

按 DBAccess 实际查询方法签名补全：取第一条启用记录 → `AesHelper.Decrypt(row.APIKEY, _aesKey)` 得 ApiKeyPlain → 映射其余字段返回 LlmConfig。若 DBAccess 的查询返回类型与示例不符，对照 `WordTemplateController.cs:565` 的 `helper.Query<T>` 用法对齐。

- [ ] **Step 5: 运行脱敏测试 + 手动验证 GetEnabled**

Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj --filter LlmConfig`
Expected: PASS（2 个）

手动验证：临时测试调 `new LlmConfigService(Key).GetEnabled()`，断言 `ApiKeyPlain` == Task 1.3 种子的明文。

- [ ] **Step 6: Commit / 检查点** — "M1: LlmConfigService + tests"

---

### Task 1.5: 用量记录 + 费用计算（TDD）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/UsageLogger.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/UsageLoggerTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using Realso.WebAPI.Services;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class UsageLoggerTests
    {
        [Fact]
        public void ComputeCost_InputPlusOutputPer1k()
        {
            // 1000@0.002/千 + 500@0.008/千 = 0.002 + 0.004
            Assert.Equal(0.006m, UsageLogger.ComputeCost(1000, 500, 0.002m, 0.008m));
        }
    }
}
```

- [ ] **Step 2: 运行确认失败** — `dotnet test ... --filter UsageLogger` → FAIL

- [ ] **Step 3: 实现 UsageLogger**（DBHelper 模式同上）

```csharp
using System;
namespace Realso.WebAPI.Services
{
    public class UsageLogger
    {
        public static decimal ComputeCost(int prompt, int completion, decimal priceIn, decimal priceOut)
            => (prompt / 1000m) * priceIn + (completion / 1000m) * priceOut;

        public void Log(string userId, string userName, string conversationId,
            int promptTokens, int completionTokens, decimal priceIn, decimal priceOut,
            int durationMs, bool success, string errorMsg)
        {
            DBHelper helper = DB.GetDBHelper();
            var id = Guid.NewGuid().ToString("N");
            var total = promptTokens + completionTokens;
            var cost = ComputeCost(promptTokens, completionTokens, priceIn, priceOut);
            helper.Execute(
                @"INSERT INTO TBS_LLM_USAGE (ID,USERID,USERNAME,CONVERSATIONID,OPERATIONTYPE,
                   PROMPTTOKENS,COMPLETIONTOKENS,TOTALTOKENS,COST,DURATIONMS,ISSUCCESS,ERRORMSG,REQUESTTIME,ISDELETED)
                   VALUES (@ID,@UID,@UN,@CID,'chat',@PT,@CT,@TT,@COST,@DUR,@OK,@ERR,NOW(),0)",
                new { ID=id, UID=userId, UN=userName, CID=conversationId, PT=promptTokens,
                      CT=completionTokens, TT=total, COST=cost, DUR=durationMs, OK=success?1:0, ERR=errorMsg });
        }
    }
}
```

- [ ] **Step 4: 运行确认通过** — `dotnet test ... --filter UsageLogger` → PASS

- [ ] **Step 5: Commit / 检查点** — "M1: UsageLogger + tests"

---

### Task 1.6: SSE 消息块协议与序列化（TDD）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/SseWriter.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/SseWriterTests.cs`

> 全局 JSON 是 PascalCase，SSE 块**单独用 camelCase** 序列化，前端才能识别 `type`/`text`/`tool`/`args`。

- [ ] **Step 1: 写失败测试**

```csharp
using Realso.WebAPI.Services;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class SseWriterTests
    {
        [Fact] public void Frame_CamelCase() => Assert.Equal("data: {\"type\":\"text\",\"text\":\"hello\"}\n\n", SseWriter.Frame(new { type="text", text="hello" }));
        [Fact] public void FrameDone() => Assert.Equal("data: {\"type\":\"done\"}\n\n", SseWriter.FrameDone());
        [Fact] public void FrameHeartbeat() => Assert.Contains("\"type\":\"heartbeat\"", SseWriter.FrameHeartbeat());
    }
}
```

- [ ] **Step 2: 运行确认失败** — `dotnet test ... --filter SseWriter` → FAIL

- [ ] **Step 3: 实现 SseWriter**

```csharp
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace Realso.WebAPI.Services
{
    public static class SseWriter
    {
        static readonly JsonSerializerSettings Camel = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };
        public static string Frame(object block) => "data: " + JsonConvert.SerializeObject(block, Camel) + "\n\n";
        public static string FrameDone() => Frame(new { type = "done" });
        public static string FrameHeartbeat() => Frame(new { type = "heartbeat" });
    }
}
```

> 实际写出 + 加锁在 AssistantController 用 `SemaphoreSlim` 包裹（见 Task 1.9），避免心跳与内容并发写 Response 流。

- [ ] **Step 4: 运行确认通过** — `dotnet test ... --filter SseWriter` → PASS（3 个）

- [ ] **Step 5: Commit / 检查点** — "M1: SseWriter + tests"

---

### Task 1.7: DeepSeek 客户端（流式 + usage 解析）（TDD with mock）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/DeepSeekClient.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/DeepSeekClientTests.cs`

DeepSeek 是 OpenAI 兼容：`POST {BaseUrl}/chat/completions`，`stream:true` + `stream_options:{include_usage:true}`，响应 SSE，每行 `data: {choices:[{delta:{content}}], usage:{...}}`，末尾 `data: [DONE]`。

- [ ] **Step 1: 写失败测试**（HttpMessageHandler mock 返回固定 SSE 流）

```csharp
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Realso.WebAPI.Services;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class DeepSeekClientTests
    {
        const string Body =
            "data: {\"choices\":[{\"delta\":{\"content\":\"你\"}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"content\":\"好\"}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{}}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":2}}\n\n" +
            "data: [DONE]\n\n";

        [Fact]
        public async Task Stream_AccumulatesContentAndExtractsUsage()
        {
            var client = new DeepSeekClient(new HttpClient(new Stub(Body)));
            var got = new List<string>();
            var u = await client.StreamChatAsync("http://x", "k", "m", new object[0], null, s => got.Add(s));
            Assert.Equal(new[] { "你", "好" }, got);
            Assert.Equal(10, u.PromptTokens);
            Assert.Equal(2, u.CompletionTokens);
        }
        class Stub : HttpMessageHandler
        {
            readonly string _b; public Stub(string b) { _b = b; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_b, Encoding.UTF8, "text/event-stream") });
        }
    }
}
```

- [ ] **Step 2: 运行确认失败** — `dotnet test ... --filter DeepSeekClient` → FAIL

- [ ] **Step 3: 实现 DeepSeekClient**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Realso.WebAPI.Services
{
    public class LlmUsage { public int PromptTokens; public int CompletionTokens; public List<object> ToolCalls = new List<object>(); public bool HasToolCalls => ToolCalls.Count > 0; }

    public class DeepSeekClient
    {
        private readonly HttpClient _http;
        public DeepSeekClient(HttpClient http) { _http = http; }

        public async Task<LlmUsage> StreamChatAsync(string baseUrl, string apiKey, string model,
            object messages, object tools, Action<string> onContent, Action<List<object>> onToolCalls = null)
        {
            var payload = new { model, messages, stream = true, stream_options = new { include_usage = true }, tools };
            var req = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/chat/completions");
            req.Headers.Add("Authorization", "Bearer " + apiKey);
            req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            var usage = new LlmUsage();
            using (var stream = await resp.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!line.StartsWith("data:")) continue;   // 跳过空行 / keep-alive 注释
                    var data = line.Substring(5).Trim();
                    if (data == "[DONE]") break;
                    var chunk = JObject.Parse(data);
                    var ut = chunk["usage"];
                    if (ut != null) { usage.PromptTokens = (int)(ut["prompt_tokens"] ?? 0); usage.CompletionTokens = (int)(ut["completion_tokens"] ?? 0); }
                    var delta = chunk["choices"]?[0]?["delta"];
                    if (delta == null) continue;
                    var c = delta["content"]?.ToString();
                    if (!string.IsNullOrEmpty(c)) onContent(c);
                    // tool_calls 累积在 M2 补（M1 传 tools=null）
                }
            }
            return usage;
        }
    }
}
```

- [ ] **Step 4: 运行确认通过** — `dotnet test ... --filter DeepSeekClient` → PASS

- [ ] **Step 5: Commit / 检查点** — "M1: DeepSeekClient + tests"

---

### Task 1.8: 会话存储 SessionStore（内存 + DB 持久）（TDD）

**Files:**
- Create: `netcore/Realso.WebAPI/Services/SessionStore.cs`
- Test: `netcore/Realso.Assistant.Test/Assistant/SessionStoreTests.cs`

> M1 范围：内存层 + 用户/助理消息落库。**崩溃后从 DB 重建消息历史推迟到 M2**（M1 会话在同请求/内存内完成；此处显式声明，非埋雷 TODO）。

- [ ] **Step 1: 写失败测试**（内存层，DB 持久方法在 Step 5 手动验证）

```csharp
using Realso.WebAPI.Services;
using Xunit;
namespace Realso.Assistant.Test.Assistant
{
    public class SessionStoreTests
    {
        [Fact]
        public void Create_GetsId_LoadsEmpty()
        {
            var store = new SessionStore();
            // Create 会写 DB（需测试库）；本测试聚焦内存：用内部方法或跳过 DB
            // 见 Step 3：将 DB 写入抽成可注入接口以便单测
            Assert.True(true); // 占位，实际断言见 Step 3 重写
        }
    }
}
```

- [ ] **Step 2: 运行确认** — 占位通过（Step 3 重写真实测试）

- [ ] **Step 3: 把 DB 写入抽成可注入接口，写真实单测**

为可单测，定义 `IConversationRepo`（Create/AppendMessage 等 DB 操作），`SessionStore` 依赖它。测试用内存实现。重写测试断言：Create→Load 内存消息为空；AddUser→Load 含 1 条。

- [ ] **Step 4: 实现 SessionStore**（内存 ConcurrentDictionary + IConversationRepo 持久）

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
namespace Realso.WebAPI.Services
{
    public class AssistantMessage { public string Role; public string Content; }
    public class AssistantSession { public string ConversationId; public List<AssistantMessage> Messages = new List<AssistantMessage>(); }

    public interface IConversationRepo
    {
        string Create(string userId, string userName);
        void AppendMessage(string conversationId, string role, string content, string blocksJson);
        // List<AssistantMessage> LoadFromDb(string conversationId);  // M2 实现（崩溃重建）
    }

    public class SessionStore
    {
        private static readonly ConcurrentDictionary<string, AssistantSession> _cache = new ConcurrentDictionary<string, AssistantSession>();
        private readonly IConversationRepo _repo;
        public SessionStore(IConversationRepo repo = null) { _repo = repo; }  // 测试可传内存 repo

        public string Create(string userId, string userName)
        {
            var id = (_repo != null) ? _repo.Create(userId, userName) : Guid.NewGuid().ToString("N");
            _cache[id] = new AssistantSession { ConversationId = id };
            return id;
        }
        public AssistantSession Load(string conversationId)
            => _cache.GetOrAdd(conversationId, k => new AssistantSession { ConversationId = k });
        public void AddUser(string conversationId, string content)
        {
            Load(conversationId).Messages.Add(new AssistantMessage { Role = "user", Content = content });
            _repo?.AppendMessage(conversationId, "user", content, null);
        }
        public void AddAssistant(string conversationId, string content)
        {
            Load(conversationId).Messages.Add(new AssistantMessage { Role = "assistant", Content = content });
            _repo?.AppendMessage(conversationId, "assistant", content, null);
        }
    }
}
```

生产用 `DbConversationRepo : IConversationRepo`（DBHelper 模式同前）在 Startup 注册。

- [ ] **Step 5: 运行确认通过 + 手动验证落库** — `dotnet test ... --filter SessionStore` → PASS；手动：真实 repo 调 AddUser 后 `TBS_ASSISTANT_MESSAGE` 有记录。

- [ ] **Step 6: Commit / 检查点** — "M1: SessionStore + tests"

---

### Task 1.9: AssistantController + SSE `/send`（ReAct 单步，无工具，并发安全）

**Files:**
- Create: `netcore/Realso.WebAPI/Controllers/AssistantController.cs`
- Modify: `netcore/Realso.WebAPI/Startup.cs`

> 要点：① 继承 `BaseControl` 以拿 `this.userInfo`（来自 `_userInfo_` 表单字段，键 `ID`/`NICKNAME`）；② **SemaphoreSlim 序列化所有 Response 写入**（心跳与内容并发写会抛 InvalidOperationException）；③ 心跳保活。

- [ ] **Step 1: 实现 AssistantController**

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Core.Base;
using Realso.WebAPI.Services;

namespace Realso.WebAPI.Controllers
{
    [Route("api/assistant")]
    public class AssistantController : BaseControl
    {
        private readonly LlmConfigService _cfg;
        private readonly DeepSeekClient _llm;
        private readonly SessionStore _sessions;
        private readonly UsageLogger _usage;
        public AssistantController(LlmConfigService cfg, DeepSeekClient llm, SessionStore sessions, UsageLogger usage)
        { _cfg = cfg; _llm = llm; _sessions = sessions; _usage = usage; }

        [HttpPost("send")]
        [EnableCors("AllowHeaders")]
        public async Task Send([FromForm] string conversationId, [FromForm] string message)
        {
            // userInfo 由 _userInfo_ 表单字段经 HashtableBinder 绑定（键 ID / NICKNAME）
            var userId = this.userInfo != null ? (this.userInfo["ID"] + "") : "anonymous";
            var userName = this.userInfo != null ? (this.userInfo["NICKNAME"] + "") : "";

            if (string.IsNullOrEmpty(conversationId)) conversationId = _sessions.Create(userId, userName);
            _sessions.AddUser(conversationId, message);

            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            // 并发安全：所有 Response 写入串行化（心跳 vs 内容）
            var writeLock = new SemaphoreSlim(1, 1);
            async Task Write(object block)
            {
                await writeLock.WaitAsync();
                try { await Response.WriteAsync(SseWriter.Frame(block)); await Response.Body.FlushAsync(); }
                finally { writeLock.Release(); }
            }

            var cfg = _cfg.GetEnabled();
            if (cfg == null) { await Write(new { type = "error", text = "未配置 LLM，请先在管理后台配置" }); return; }

            // 心跳保活（15s）
            var cts = new CancellationTokenSource();
            var heartbeat = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try { await Task.Delay(15000, cts.Token); } catch { break; }
                    if (!cts.Token.IsCancellationRequested) await Write(new { type = "heartbeat" });
                }
            });

            var llmMessages = BuildLlmMessages(_sessions.Load(conversationId));
            var sw = Stopwatch.StartNew();
            string text = "";
            try
            {
                var usage = await _llm.StreamChatAsync(cfg.BaseUrl, cfg.ApiKeyPlain, cfg.ModelName,
                    messages: llmMessages, tools: null,
                    onContent: async c => { text += c; await Write(new { type = "text", text = c }); });
                _sessions.AddAssistant(conversationId, text);
                _usage.Log(userId, userName, conversationId, usage.PromptTokens, usage.CompletionTokens,
                    cfg.PriceInput, cfg.PriceOutput, (int)sw.ElapsedMilliseconds, true, null);
            }
            catch (Exception ex)
            {
                _usage.Log(userId, userName, conversationId, 0, 0, cfg.PriceInput, cfg.PriceOutput,
                    (int)sw.ElapsedMilliseconds, false, ex.Message);
                await Write(new { type = "error", text = "调用失败：" + ex.Message });
            }
            finally
            {
                cts.Cancel();
                try { await heartbeat; } catch { }
                await Write(new { type = "done" });
            }
        }

        private List<object> BuildLlmMessages(AssistantSession s)
        {
            var list = new List<object> { new { role = "system", content = "你是华溯 LIMS 智能助理。当前为 M1 版本（仅对话，暂无工具）。用中文简洁回答。" } };
            foreach (var m in s.Messages) list.Add(new { role = m.Role, content = m.Content });
            return list;
        }
    }
}
```
> 继承 BaseControl + 构造注入：参考现有子控制器（如 RM11Controller）如何同时继承 BaseControl 并用构造注入服务。若 BaseControl 构造与注入冲突，按 RM11Controller 模式调整。`[EnableCors]` 的 using 见 Startup。

- [ ] **Step 2: Startup.cs 注册服务**

`ConfigureServices` 加：
```csharp
services.AddSingleton(new DeepSeekClient(new HttpClient()));
services.AddScoped(sp => new LlmConfigService(Configuration["Assistant:AesKey"]));
services.AddScoped<SessionStore>();
services.AddScoped<UsageLogger>();
```
（SessionStore 生产用 DbConversationRepo：`services.AddScoped<IConversationRepo, DbConversationRepo>(); services.AddScoped<SessionStore>();`）

- [ ] **Step 3: 构建确认**

Run: `dotnet build netcore/Realso.sln`
Expected: 成功（警告可接受）

- [ ] **Step 4: 启动 + curl 手动验证 SSE**（带 _userInfo_ 与 token）

```bash
# 终端1
dotnet run --project netcore/Realso.WebAPI --urls http://127.0.0.1:5001
# 终端2（TOKEN 用真实登录 token；_userInfo_ 为 JSON 字符串）
curl -N -X POST http://127.0.0.1:5001/api/assistant/send \
  -H "Authorization: Bearer <TOKEN>" \
  -F "conversationId=" -F "message=你好" \
  -F '_userInfo_={"ID":"u1","NICKNAME":"测试员"}'
```
Expected: 多行 `data: {"type":"text","text":"..."}` + 末尾 `data: {"type":"done"}`。

- [ ] **Step 5: 验证用量入库（真实用户名）**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT USERNAME,TOTALTOKENS,COST,ISSUCCESS FROM TBS_LLM_USAGE ORDER BY REQUESTTIME DESC LIMIT 1;"`
Expected: USERNAME="测试员"（非 anonymous/空），TOTALTOKENS>0，ISSUCCESS=1。

- [ ] **Step 6: Commit / 检查点** — "M1: AssistantController SSE /send（并发安全 + 真实用户）"

---

### Task 1.10: 前端依赖 + Webpack3 冒烟

**Files:**
- Modify: `p-admin/package.json`

- [ ] **Step 1: 安装（锁兼容版本）**

Run: `cd p-admin && npm install --save marked@4 dompurify@2`
（Webpack3 + Babel6 环境；若构建报错降级 `marked@1`（纯同步 API）、`dompurify@2.2.x`（UMD 构建））

- [ ] **Step 2: 冒烟构建**

Run: `cd p-admin && npm run build`
Expected: 成功，无模块找不到错误。若 `dompurify` 报 ES2017+ 语法，改引其 UMD/dist 路径或降 2.2.x。

- [ ] **Step 3: Commit / 检查点** — "M1: 前端依赖 marked + dompurify"

---

### Task 1.11: 前端 SSE 客户端 + assistant store

**Files:**
- Create: `p-admin/src/api/assistant.js`
- Create: `p-admin/src/store/modules/assistant.js`（**普通 namespaced 模块**，静态注册，不用 createStore）
- Test: `p-admin/test/unit/specs/assistant.spec.js`

> 关键修正：token 从 `store.state.user.access_token` 取，`_userInfo_` 取 `store.state.user.userInfo` 并 **JSON.stringify**（HashtableBinder 按 JSON 解析）。

- [ ] **Step 1: api/assistant.js**

```js
import store from '@/store'
import { getUrl } from '@/api/db'

// 发送消息，流式读取 SSE；onBlock 收每个消息块
export async function sendMessage(conversationId, message, onBlock) {
  const fd = new FormData()
  fd.append('conversationId', conversationId || '')
  fd.append('message', message)
  // _userInfo_ 必须是 JSON 字符串（后端 HashtableBinder 用 JsonConvert 反序列化）
  fd.append('_userInfo_', JSON.stringify(store.state.user.userInfo || {}))
  const resp = await fetch(getUrl('url') + '/api/assistant/send', {
    method: 'POST',
    headers: { Authorization: 'Bearer ' + (store.state.user.access_token || '') },
    body: fd
  })
  const reader = resp.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let sawDone = false
  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })
    const parts = buffer.split('\n\n')
    buffer = parts.pop()
    for (const part of parts) {
      const json = part.replace(/^data: /, '')
      if (!json) continue
      const block = JSON.parse(json)
      if (block.type === 'done') sawDone = true
      if (block.type === 'heartbeat') continue
      onBlock(block)
    }
  }
  return { ok: sawDone }
}
```

- [ ] **Step 2: store/modules/assistant.js（namespaced，静态注册）**

```js
import { sendMessage } from '@/api/assistant'

export default {
  namespaced: true,
  state: { visible: false, conversationId: '', messages: [] },
  mutations: {
    SET_VISIBLE(s, v) { s.visible = v },
    PUSH_MESSAGE(s, m) { s.messages.push(m) },
    APPEND_BLOCK(s, block) {
      const last = s.messages[s.messages.length - 1]
      if (!last) return
      if (block.type === 'text' && last.blocks.length && last.blocks[last.blocks.length - 1].type === 'text') {
        last.blocks[last.blocks.length - 1].text += block.text   // 打字效果累加
      } else {
        last.blocks.push(block)
      }
    }
  },
  actions: {
    toggle({ commit, state }) { commit('SET_VISIBLE', !state.visible) },
    async send({ commit, state }, text) {
      commit('PUSH_MESSAGE', { role: 'user', blocks: [{ type: 'text', text }] })
      commit('PUSH_MESSAGE', { role: 'assistant', blocks: [] })
      try {
        const res = await sendMessage(state.conversationId, text, b => commit('APPEND_BLOCK', b))
        if (!res.ok) commit('APPEND_BLOCK', { type: 'text', text: '⚠️ 连接中断，请重试' })
      } catch (e) {
        commit('APPEND_BLOCK', { type: 'text', text: '⚠️ 网络异常，请重试' })
      }
    }
  }
}
```

- [ ] **Step 3: 在 store/index.js 静态注册**

Modify `p-admin/src/store/index.js`：
```js
import assistant from './modules/assistant'
// ...
modules: { app, user, assistant },
```
（不加到 `createPersistedState` 的 paths，会话无需持久化到 sessionStorage）

- [ ] **Step 4: Jest 单测 APPEND_BLOCK 累加**

Create `p-admin/test/unit/specs/assistant.spec.js`：
```js
import mutations from '@/store/modules/assistant'

describe('assistant mutations', () => {
  it('连续 text 块累加实现打字效果', () => {
    const state = { messages: [{ role: 'assistant', blocks: [] }] }
    mutations.mutations.APPEND_BLOCK(state, { type: 'text', text: '你' })
    mutations.mutations.APPEND_BLOCK(state, { type: 'text', text: '好' })
    expect(state.messages[0].blocks.length).toBe(1)
    expect(state.messages[0].blocks[0].text).toBe('你好')
  })
})
```
（按实际 export 调整：若 `export default { mutations }`，则 `mutations.mutations.APPEND_BLOCK`；若分别 export 则直接 import）

Run: `cd p-admin && npm run unit -- --testPathPattern assistant`
Expected: PASS

- [ ] **Step 5: Commit / 检查点** — "M1: 前端 SSE 客户端 + store（token/userInfo 正确注入）"

---

### Task 1.12: 前端抽屉 UI 组件

**Files:**
- Create: `p-admin/src/components/assistant/{AssistantDrawer,AssistantMessageList,AssistantInput}.vue`
- Create: `p-admin/src/components/assistant/blocks/{TextBlock,ThinkingBlock}.vue`

- [ ] **Step 1: TextBlock.vue**（marked.parse + dompurify）

```vue
<template><div class="asst-text" v-html="html"></div></template>
<script>
import marked from 'marked'
import DOMPurify from 'dompurify'
export default {
  props: { text: { type: String, default: '' } },
  computed: {
    html() { return DOMPurify.sanitize(marked.parse(this.text, { breaks: true })) }
  }
}
</script>
```
> marked v4 用 `marked.parse(...)`（非 `marked(...)`）。若装的是 marked@1，改为 `marked(this.text)`。

- [ ] **Step 2: ThinkingBlock.vue**（可折叠）

```vue
<template>
  <div class="asst-thinking">
    <div class="asst-thinking-head" @click="open = !open">💭 {{ open ? '收起' : '展开' }}思考过程</div>
    <div v-show="open" class="asst-thinking-body">{{ text }}</div>
  </div>
</template>
<script>
export default { props: ['text'], data: () => ({ open: false }) }
</script>
```

- [ ] **Step 3: AssistantMessageList.vue**（按 block.type 分发）

```vue
<template>
  <div class="asst-msg-list">
    <div v-for="(msg, i) in messages" :key="i" :class="['asst-msg', 'asst-msg-' + msg.role]">
      <template v-if="msg.role === 'user'">{{ msg.blocks[0] && msg.blocks[0].text }}</template>
      <template v-else>
        <component v-for="(b, j) in msg.blocks" :key="j" :is="comp(b.type)" v-bind="props(b)"/>
      </template>
    </div>
  </div>
</template>
<script>
import TextBlock from './blocks/TextBlock.vue'
import ThinkingBlock from './blocks/ThinkingBlock.vue'
export default {
  components: { TextBlock, ThinkingBlock },
  computed: { messages() { return this.$store.state.assistant.messages } },
  methods: {
    comp(t) { return { text: 'TextBlock', thinking: 'ThinkingBlock' }[t] || null },
    props(b) { return b.type === 'text' ? { text: b.text } : b }
  }
}
</script>
```

- [ ] **Step 4: AssistantInput.vue**

```vue
<template>
  <div class="asst-input">
    <textarea v-model="text" @keydown.enter.exact.prevent="send" placeholder="问点什么…"></textarea>
    <button @click="send">发送</button>
  </div>
</template>
<script>
export default {
  data: () => ({ text: '' }),
  methods: {
    send() {
      const t = this.text.trim()
      if (!t) return
      this.$store.dispatch('assistant/send', t)
      this.text = ''
    }
  }
}
</script>
```

- [ ] **Step 5: AssistantDrawer.vue**（右侧抽屉 + 浮动按钮）

```vue
<template>
  <div>
    <button class="asst-fab" @click="$store.dispatch('assistant/toggle')">💬</button>
    <transition name="slide">
      <div v-if="$store.state.assistant.visible" class="asst-drawer">
        <div class="asst-header">智能助理 <span @click="$store.dispatch('assistant/toggle')">✕</span></div>
        <AssistantMessageList/>
        <AssistantInput/>
      </div>
    </transition>
  </div>
</template>
<script>
import AssistantMessageList from './AssistantMessageList.vue'
import AssistantInput from './AssistantInput.vue'
export default { components: { AssistantMessageList, AssistantInput } }
</script>
<style scoped>
.asst-fab { position: fixed; right: 24px; top: 80px; z-index: 2000; }
.asst-drawer { position: fixed; right: 0; top: 0; width: 420px; height: 100%; background: #fff;
  box-shadow: -2px 0 8px rgba(0,0,0,.15); z-index: 2000; display: flex; flex-direction: column; }
.asst-header { padding: 12px; border-bottom: 1px solid #eee; }
.slide-enter-active, .slide-leave-active { transition: transform .25s; }
.slide-enter, .slide-leave-to { transform: translateX(100%); }
</style>
```

- [ ] **Step 6: ESLint 通过** — `cd p-admin && npm run lint` → 无 error（标准规则：分号、单引号、2 空格）

- [ ] **Step 7: Commit / 检查点** — "M1: 抽屉 UI 组件"

---

### Task 1.13: App.vue 挂载抽屉（保留 SignalR）

**Files:**
- Modify: `p-admin/src/App.vue`

> ⚠️ 现有 App.vue 顶层有 SignalR 引导代码（`connection.invoke('send','Hello')` 等）。**修改时只加 `<AssistantDrawer/>` 和 components 注册，不要删除 `<script>` 里的 SignalR 行。**

- [ ] **Step 1: 模板加 AssistantDrawer**

```vue
<template>
  <div id="app">
    <router-view v-if="isShow"/>
    <AssistantDrawer/>
  </div>
</template>
```

- [ ] **Step 2: script 注册组件（保留原有 SignalR 代码）**

```vue
<script>
import AssistantDrawer from '@/components/assistant/AssistantDrawer.vue'
export default {
  name: 'App',
  components: { AssistantDrawer },
  data() { return { isShow: true } }
}
// ↓↓↓ 保留原有 SignalR 代码不动 ↓↓↓
document.body.addEventListener('touchstart', function(e) { /* ... */ });
const signalR = require('@aspnet/signalr');
// ... 原有 connection 代码
</script>
```

- [ ] **Step 3: 构建并启动**

Run: `cd p-admin && npm run dev`
Expected: 浏览器打开，右上角见 💬 按钮，无 SignalR 相关报错。

- [ ] **Step 4: Commit / 检查点** — "M1: App.vue 挂载抽屉（保留 SignalR）"

---

### Task 1.14: M1 端到端集成验证（手动）

- [ ] **Step 1: 后端启动** — `dotnet run --project netcore/Realso.WebAPI --urls http://127.0.0.1:5001`
- [ ] **Step 2: 前端启动** — `cd p-admin && npm run dev`
- [ ] **Step 3: 手动验证清单**
  - [ ] 登录系统，右上角见 💬，点击展开抽屉
  - [ ] 输入"你好，你能做什么"，点发送
  - [ ] **逐字流式回复**（打字效果）
  - [ ] Markdown 正常渲染（列表/加粗等）
  - [ ] 关闭抽屉再打开，可继续对话

- [ ] **Step 4: 验证用量入库（真实用户名）**

Run: `docker exec labone-mysql mysql -ulabone -plabone123 D0001 -e "SELECT USERNAME,TOTALTOKENS,COST FROM TBS_LLM_USAGE ORDER BY REQUESTTIME DESC LIMIT 3;"`
Expected: 当前登录用户名（非 anonymous），TOTALTOKENS>0，COST>0。

- [ ] **Step 5: 验证心跳与断线提示**
  - [ ] 停后端 → 发消息 → 显示"⚠️ 网络异常/连接中断，请重试"（fetch 抛错或流被切，有 try/catch 与 sawDone 检测）
  - [ ] 重启后端 → 重试 → 恢复正常

- [ ] **Step 6: 全部单元测试通过**

Run: `dotnet test netcore/Realso.Assistant.Test/Realso.Assistant.Test.csproj` 和 `cd p-admin && npm run unit -- --testPathPattern assistant`
Expected: 全 PASS

- [ ] **Step 7: Commit / 检查点** — "M1: 端到端验证通过 ✅"

---

**🎉 M1 完成。地基就位：助理能对话、流式回复、用量按真实用户可追踪。接下来 M2 加只读工具能力。**

---

## Chunk 2: M2–M6 路线图（后续逐个展开为完整计划）

> 每个里程碑完成后展开为独立完整 TDD 计划（参照 Chunk 1 粒度）。以下为大纲，锁定文件清单与关键测试点。

### M2: 只读能力 + DataCallService 抽取

**验证标准：** 能问数据、找模块、Markdown 回答（含表格）。

**关键任务：**
1. **抽取 `DataCallService`**（首个任务）：从 `DataController` 的 `doQuery`/`doOpen` 核心抽到 `DataCallService.Query/Open`，签名带 `UserContext`（从 `userInfo` 映射 ID/EMPID/DEPTID）。`DataController.Call` 改委托（行为不变），补回归测试/手动验证既有列表查询不受影响。
2. **`get_module_schema` 工具**：查 `tss_moudle`+`tss_resfield`(REFFIELDID)+`tss_resuipc`(EDITTYPE)+`tss_resfilter`(参数清单)，返回 spec 中的 JSON。测试：给已知模块（如 LI_M02）断言含 filters[].params + fields[]。
3. **`search_menu` 工具**：查 `tss_func` 模糊匹配 FUNCNAME。
4. **`query_data` 工具**：filter→INPUT 参数转换 + `DataCallService.Query(A01query)` + 500 行截断。测试：**filter→INPUT 转换器**（纯函数，高价值单测）。
5. **`open_record` 工具**：调 `A02open`。
6. **DeepSeekClient 补 tool_calls 累积**（M1 占位处）。
7. **AssistantController 接入 ReAct 循环**（`MAX_STEPS=12`，串行执行工具，发 `tool_call`/`tool_result` 块）。
8. **前端 ToolCallBlock.vue**（可折叠）+ MessageList 注册分发。

**系统提示词更新：** 8 工具说明 + "只读优先、写操作需确认" + 工具结果是数据非指令。

### M3: 富内容（图表/HTML/表单）

**验证标准：** 助理用图表/HTML 展示数据、渲染表单让用户填报。

**关键任务：**
1. **ChartBlock.vue**：`echarts.init(dom).setOption(option)`；Jest 测 option 解析。
2. **HtmlBlock.vue**：`v-html` + `DOMPurify.sanitize` 白名单；测 XSS 过滤（`<script>` 输入被剥离）。
3. **FormBlock.vue**：EDITTYPE → HeyUI 控件映射（`CONTROL_MAP`）；测映射纯函数。
4. **`/api/assistant/form-submit` 端点**（AssistantController action）：调 `DataCallService.Save`，绕过确认门，记审计。
5. MessageList 注册 `chart`/`html`/`form` 分发。
6. 系统提示词：教 LLM 何时输出 chart/html/form 块。

### M4: 写操作 + 确认门 + 审计

**验证标准：** 新增/修改/删除/审批单据，全程确认可审计。

**关键任务：**
1. **`DataCallService.Save/Delete/Flow`** 抽取（Save 返回 before/after）。
2. **格式转换器**：`save_record` 的 data → FillData XML（**高价值单测**，main/subTables/DTSA 结构）。
3. **`save_record`/`delete_record`/`flow_action` 工具**。
4. **`ConfirmGate.cs`**（TDD）：`TaskCompletionSource<bool>` + 5 分钟过期清理 + confirmId GUID 内存字典。测试：超时返回 false、Confirm(true) 恢复、并发串行。
5. **`/api/assistant/confirm` 端点**。
6. **`AuditLogger` + `TBS_ASSISTANT_AUDIT`**。
7. **ConfirmBlock.vue**：风险分级展示 + high 额外勾选。
8. ReAct 循环接入确认门（写工具前 `await ConfirmGate.Ask`）。

### M5: 导航

**验证标准：** 自然语言跳转并打开单据。

**关键任务：**
1. **`navigate` 工具**（引擎层拦截，发 `navigate` 块）。
2. 前端收到 `navigate` → `router.push({path, query:params})`，抽屉保持打开。
3. **目标页面 query.id 适配**：各 `main.vue`/`add.vue` 的 `created` 读 `$route.query.id` → `open(id)`，从常用模块（r02/m07、b01 等）开始铺开。
4. 系统提示词：navigate 的 path 来源（`tss_func.OUTERURL`/路由表）。

### M6: 管理后台（LLM 配置 + 用量统计）

**验证标准：** 管理员配置 Key、看用量统计（含图表）。

**关键任务：**
1. **元数据注册**：`TBS_LLM_CONFIG`/`TBS_LLM_USAGE`/`VRP_LLM_USAGE_BY_USER` 注册 tss_resource/tss_resfield/tss_resuipc（按 [[orm-metadata]] 规范）。
2. **`VRP_LLM_USAGE_BY_USER` 视图**（SQL 类型，SS0020 模式）。
3. **`LLMConfigController.cs`**（自定义，加密存/脱敏读）。
4. **`pages/s01/m14/`**（LLM 配置页，标准模块结构）。
5. **`pages/s01/m15/`**（用量统计页）：明细 + 按用户汇总 + ECharts 趋势。
6. **`tss_func` 菜单注册**：LLM 配置/用量统计两项。
7. 安全：配置页 `v-per` 管理员；查看非本人会话/用量记审计。

---

## 执行交接

Plan complete and saved to `docs/superpowers/plans/2026-06-21-ai-assistant.md`.

**下一步建议：**
1. 先执行 **Chunk 1（M1）**——独立可交付的地基。
2. M1 完成后再展开 **M2** 为完整 TDD 计划（DataCallService 抽取、filter→INPUT 转换的实际签名要基于 M1 中确认的 DBHelper/operate01 用法定型）。
3. M3–M6 依次展开。

**执行路径：** 若 harness 支持子代理（subagent-driven-development），M1 各 Task 可派发独立子代理实现 + 两阶段评审；否则在当前会话用 executing-plans 批量执行 + 检查点。

**实现注意：**
- 后端测试加在 `Realso.Assistant.Test/Assistant/`，前端在 `test/unit/specs/`。
- DBHelper 查询/执行方法签名、`operate01`、`BaseControl` 构造与注入的共存方式——实现时对照 `WordTemplateController`/`RM11Controller` 实际用法二次确认（本计划基于代码扫描）。
- VCS：当前未 git init，commit 步骤为逻辑检查点。

Ready to execute?
