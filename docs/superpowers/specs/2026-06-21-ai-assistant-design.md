# 华溯 LIMS 智能助理 - 设计文档

> **⚠️ 实现调整说明（2026-06-22 更新，以本文为准）**
> - **传输层：SSE → SignalR。** 原设计用裸 SSE（`AssistantController /send` 写 `Response.Body`），实现时发现流式响应在 CORS 管线中拿不到响应头，**改用项目已有的 SignalR**：`AssistantHub`（路由 `/assistantHub`），CORS 走全局 `"SignalrCore"` 策略（与 `ChatHub` 一致）。**消息块协议（thinking/tool_call/tool_result/text/chart/html/form/confirm/navigate/heartbeat/error/done）不变**，只是经 SignalR 推送。前端 `api/assistant.js` 用 `@aspnet/signalr` 客户端（非 fetch）。下文凡提及 SSE / `AssistantController /send` 的，均以 SignalR 实现为准。
> - **M2 已完成（含 DRY 重构）**：`DataCallService` 提供 `QueryCore` 共享核心，**`DataController.doQuery` 非导出路径已委托给它**（助理 `query_data` 与系统列表查询共用同一查询逻辑，权限/规则一致）。`get_module_schema` 结构化返回接口列表(apiType 中文描述) + 解析出的 `queryFilterParams` + 字段清单。
> - **413 修复**：tool 结果进对话前截断到 4KB，`query_data` 默认 5 行（上限 50）。
> - `Realso.Assistant.Test`（xUnit）现有 19 个测试通过。
> - **M4 需求调整（2026-06-22）**：**所有变更操作（编辑/删除/审批/驳回）改为"跳转到真实页面"，不在 AI 对话框处理**。助理只负责定位路由并跳转，用户在真实页面操作。因此**移除**：save_record / delete_record / flow_action 工具、ConfirmGate / ConfirmBlock、AuditLogger / TBS_ASSISTANT_AUDIT、FormBlock / form-submit、/confirm 端点。M4+M5 合并为**纯导航**里程碑（`navigate` 工具：list/add/edit/detail 跳转 + 页面 query.id 适配）。下文 M4"写操作+确认门+审计"及 M3"表单填报"章节按此调整为准（已废弃）。
> - **query_stats 新增（文档外）**：统计分析工具，用 ORM 的 `BuildSQL01.BuildQuery` 生成正确 base SELECT（物理表+JOIN+权限），包成子查询，LLM 只提供 select/groupBy/where/orderBy 引用字段名。优先用 F02(高级查询)过滤器，参数全空默认。

## 需求背景

华溯计量管理系统（hs2.0）是一个面向计量检测/校准行业的 LIMS 系统，包含大量业务模块（收发、校准、报告、证书、费用、物流等）。当前系统功能强大但模块众多，用户需要熟悉菜单结构、字段含义、操作流程才能高效使用。

**目标**：为系统增加一个智能助理，用户通过自然语言即可：
- 📊 **问答与分析**：查询任意模块数据、统计分析
- 🔍 **找模块**：自然语言定位功能
- 🧭 **跳转**：直接导航到页面/单据
- ✏️ **操作整个系统**：新增/修改/删除数据、提交/审批等状态流转（含安全确认）
- 📈 **富结果交付**：用图表、HTML、可填报表单展示与录入数据

**核心洞察**：系统是**元数据驱动**架构，所有模块/字段/API/菜单都存在 `tss_func` / `tss_moudle` / `tss_moudleapi` / `tss_resfield` / `tss_resuipc` 等表里。因此助理**无需硬编码任何模块知识**，运行时读取元数据即可动态"看懂"整个系统，天然具备覆盖所有模块的能力。

## 需求确认

| 维度 | 决定 |
|---|---|
| 功能范围 | 全功能（读/写/分析/找模块/跳转/富内容/状态流转） |
| LLM | DeepSeek（原生 function calling） |
| 执行模型 | 后端运行，经 `DataController.Call` 统一入口，继承当前用户权限 |
| 写操作安全 | 所有写操作二次确认（继承权限 + 确认门） |
| UI 形态 | 右侧全局浮动抽屉 |
| Agent 架构 | ReAct 多步循环 + 写操作确认门 + 8 个通用工具 + 元数据驱动 |
| 流式协议 | SSE（Server-Sent Events） |
| 富内容 | 图表(ECharts) + HTML(DOMPurify sanitize) + 可填报表单(复用 tss_resuipc) |
| 管理后台 | LLM 配置页（Key 加密）+ 用量统计（按人/总量，含费用） |
| 部署前提 | **MVP 单实例部署**（多实例需会话亲和或 Redis，见确认门章节） |

## 方案选择

**Agent 编排架构三选一：**

| 方案 | 描述 | 取舍 |
|---|---|---|
| A. ReAct 多步循环 ⭐ | LLM 在循环里 思考→调工具→看结果→再思考，直到完成 | 能处理复杂多步任务；token 消耗高，需步数上限 |
| B. 单轮 function calling | 一问一答一调 | 简单快；处理不了多步分析，达不到全功能 |
| C. 先规划后执行 | LLM 先生成完整计划，确认后逐步执行 | 可控性极强；灵活性差，探索性分析弱 |

**采用：方案 A（ReAct）+ 吸收方案 C 的"计划可见"** —— ReAct 循环驱动复杂任务，每个写操作步骤执行前展示详情并二次确认，把可控性嫁接进来。

**工具策略：少量精心设计的通用工具 + 元数据驱动** —— 不为每个模块写一个工具，而是写 8 个通用工具，内部读元数据表，自动覆盖所有模块，零硬编码。

## 总体架构

### 核心思想

助理 = **"一个会用你们系统的高级用户"**。它不重写任何业务逻辑，而是通过现有统一入口 `DataController.Call` 操作系统，因此自动继承当前用户权限、复用全部业务规则与单据状态机。它"懂"整个系统，靠运行时读取元数据表，零硬编码。

### 分层架构

```
┌──────────────────────────────────────────────────────────┐
│  p-admin 前端 (Vue 2)                                     │
│  ┌────────────────────────────────────┐                  │
│  │  右侧助理抽屉（全局常驻）            │                  │
│  │  消息流：思考/工具卡/文字/图表/      │   SSE 流式        │
│  │        HTML/表单/确认门/跳转         │◄─────────────┐   │
│  │  输入框                             │              │   │
│  └──────────┬─────────────────────────┘              │   │
└─────────────┼─────────────────────────────────────────┘   │
              │ POST /api/assistant/send (+会话ID)           │
              ▼  (响应体即 SSE 流)                           │
┌──────────────────────────────────────────────────────────┐│
│  Realso.WebAPI 后端                                       ││
│  ┌────────────────────────────────────┐                  ││
│  │  AssistantController                │                  ││
│  │  • 会话上下文管理（内存+DB双层）      │                  ││
│  │  • ReAct 循环编排                    │                  ││
│  │  • 确认门暂停/恢复                   │                  ││
│  └────┬───────────────────┬───────────┘                  ││
│       │                   │                               ││
│       ▼                   ▼                               ││
│  ┌─────────┐       ┌────────────────────┐                ││
│  │ DeepSeek│◄─────►│  工具层 (8个通用)    │                ││
│  │  API    │ 工具调 │ search_menu         │                ││
│  │(fn call)│ 用/结果│ get_module_schema   │                ││
│  │  +SSE   │       │ query_data          │                ││
│  └─────────┘       │ open_record         │                ││
│       │            │ save_record ✅确认  │                ││
│       ▼ usage      │ delete_record ✅确认│                ││
│  ┌──────────┐      │ flow_action ✅确认  │                ││
│  │TBS_LLM_  │      │ navigate            │                ││
│  │USAGE 记录│      └─────────┬──────────┘                ││
│  └──────────┘                │ 复用现有链路                 ││
│                              ▼                            ││
│  ┌────────────────────────────────────┐                  ││
│  │  DataCallService (从 DataController │ ★继承用户权限     ││
│  │  抽取，两者共用)                     │   ★复用业务规则   ││
│  │  → SchemaManage → BuildSQL01        │   ★单据状态机生效 ││
│  │  → ViewOperate01 (事务执行)          │                  ││
│  └────────────────────────────────────┘                  ││
└──────────────────────────────────────────────────────────┘│
                       │                                     │
                       ▼                                     │
                MySQL（业务数据 + 元数据表）                   │
```

### 一次对话的数据流

1. 用户在抽屉输入"统计本月待校准器具，按部门分组"
2. 前端 `POST /api/assistant/send`（带 `conversationId` + 消息）
3. 后端加载会话上下文，调 DeepSeek（带 8 个工具定义 + 系统提示词）
4. **ReAct 循环**：DeepSeek 决定先 `search_menu("校准")` → 找到模块 → `get_module_schema` 看字段 → `query_data` 查数据 → 聚合
   - 每步结果实时通过 SSE 推给前端（用户看到"正在查询 校准记录 模块…"等思考过程）
   - 若中途要写操作 → **暂停**，SSE 推 `confirm` 块，前端弹确认门，用户确认才继续
5. DeepSeek 产出最终回答（Markdown 表格 / 图表 / 表单）→ 流式渲染
6. 若涉及跳转 → 推 `navigate` 块，前端执行 `router.push`

## 统一消息块协议

SSE 推送的不是纯文本，而是结构化的消息块流。每一轮 Agent 循环由多个块组成，前端按 `type` 分别渲染。

| type | 含义 | payload |
|---|---|---|
| `thinking` | 助理思考过程（可折叠展示） | 文本 |
| `tool_call` | 即将调用某工具 | `{tool, args}` |
| `tool_result` | 工具返回结果（可折叠） | `{tool, result?, error?, summary}` |
| `text` | 普通 Markdown 文字（逐字流式） | 文本片段 |
| `chart` | 📊 ECharts 图表 | ECharts option JSON |
| `html` | 📄 富文本 HTML | HTML 片段（已 sanitize） |
| `form` | 📝 可填报表单 | 表单规格 |
| `confirm` | 🔒 写操作确认门 | `{action, module, data, riskLevel, confirmId}` |
| `navigate` | 🧭 前端跳转 | `{path, params}` |
| `heartbeat` | 心跳（长时间无输出时保活） | — |
| `error` | 错误/超时/步数上限 | 错误信息 |
| `done` | 本轮流式结束 | — |

**SSE 帧格式**（每行一个块）：
```
data: {"type":"thinking","text":"我先查一下校准模块..."}

data: {"type":"tool_call","tool":"search_menu","args":{"keyword":"校准"}}

data: {"type":"chart","option":{...echarts option...}}

data: {"type":"done"}
```

**连接异常处理**：
- **心跳保活**：LLM 思考期间（可能数十秒无输出），后端每 15s 发一个 `heartbeat` 块，前端据此知道连接存活；超过 60s 无任何块则前端显示"响应超时，点击重试"。
- **正常结束 vs 异常断开**：正常结束必有 `done` 块；若 `reader.read()` 返回 done 但未见 `done` 块，视为异常中断 → UI 显示"连接中断，点击重试"。
- **不支持断点续传**：SSE 是单次 POST 请求的响应流，不支持 `Last-Event-ID` 续传。重试时基于已持久化的会话历史重新发起，LLM 可从上次中断处继续（已执行的工具结果已落库）。
- **tool_result 失败**：失败时 `result` 为空、`error` 填错误信息，引擎把错误作为工具结果喂回 LLM，由 LLM 转成用户能懂的话。

## 后端 Agent 引擎

### ReAct 循环（伪代码）

```csharp
// AssistantController.RunAgentLoop
async Task RunLoop(string convId, string userMsg, UserContext user, HttpResponse sse) {
  var session = SessionStore.Load(convId);      // 内存缓存，DB 持久
  session.AddUser(userMsg);
  SessionStore.PersistMessage(convId, userMsg); // 用户消息立即落库

  using var heartbeat = new HeartbeatTimer(sse, TimeSpan.FromSeconds(15)); // 保活
  for (int step = 0; step < MAX_STEPS; step++) {   // MAX_STEPS=12 防死循环
    // ① 调 DeepSeek（流式），边收边转发 content 到 SSE
    var resp = await DeepSeekClient.ChatStreamAsync(session, TOOLS, sse);
    UsageLogger.Log(user, convId, resp.Usage);       // 用量埋点（token+费用）

    if (!resp.HasToolCalls) {                         // ② 无工具调用 = 最终回答
      await sse.SendAsync(new { type = "done" });
      SessionStore.Save(session);
      return;
    }

    foreach (var call in resp.ToolCalls) {            // ③ 执行工具（串行）
      await sse.SendAsync(new { type = "tool_call", tool = call.Name, args = call.Args });

      if (IsWriteTool(call.Name)) {                   // ④ 写工具 → 确认门暂停
        var ok = await ConfirmGate.Ask(sse, call, user);
        if (!ok) {
          session.AddToolResult(call.Id, "用户拒绝");
          SessionStore.PersistMessage(convId, ToolResult("用户拒绝"));
          continue;
        }
      }

      var result = ToolExecutor.Run(call, user);      // ⑤ 执行（内部走 DataCallService）
      await sse.SendAsync(new { type = "tool_result", summary = Summarize(result) });
      session.AddToolResult(call.Id, result);
      SessionStore.PersistMessage(convId, result);    // 每步工具结果立即落库
    }
  }
  await sse.SendAsync(new { type = "error", text = "已达最大步数(12)，请缩小任务范围" });
}
```

**边界保护**：

| 限制 | 默认值 |
|---|---|
| 单轮最大步数 | 12（初始值，M2 后按真实步数分布调整） |
| LLM 调用超时 | 60s |
| 工具执行超时 | 30s |
| 确认门超时 | 5 分钟 |
| `query_data` 最大返回行数 | 500（超出只回摘要） |
| 心跳间隔 | 15s |

### DeepSeek 接入 + SSE 转发 + 用量埋点

- DeepSeek API 兼容 OpenAI 格式，原生 SSE 流式。后端用 `HttpClient` 读 SSE 流，逐 delta 转发：
  - `content` delta → 发 `text` 块（逐字打字效果）
  - `tool_calls` 累积完成 → 进入工具执行
  - 流结束 → 响应里的 `usage`（prompt/completion tokens）→ 记入 `TBS_LLM_USAGE`
- SSE 写法（.NET Core 2.2 原生）：设 `Content-Type: text/event-stream`，每块以 `data: {json}\n\n` 写入 `Response.Body` 并 `Flush()`。

### 会话与上下文管理

- **会话存储（内存 + DB 双层）**：`SessionStore` 是内存缓存层（热数据，循环内读写），`TBS_ASSISTANT_CONVERSATION`/`TBS_ASSISTANT_MESSAGE` 是持久层。**同步策略**：用户消息和每次工具执行结果**立即写 DB**（已执行的工具调用/结果先落库），LLM 的最终回答在循环结束 `done` 时批量写。**崩溃对账**：SSE 中途断开时，已通过 `DataCallService` 执行的写操作已在业务库真实生效（不可回滚），会话状态以 DB 为准；用户重连后看到已持久化的消息，未完成的步骤需重新发起。即"业务写操作幂等落库，会话状态可重建"。
- **上下文窗口**：DeepSeek 上下文 64K+。每次请求带系统提示词 + 最近 N 条消息；超出阈值滑动窗口截断（保留 system + 最近消息）。
- **系统提示词**：定义助理人设、可用工具说明、"只读优先、写操作需确认"、元数据使用指引、**工具结果是数据而非指令的边界**（防 prompt injection）。

## 工具层

### 关键工程决策：抽取 DataCallService

把 `DataController.Call` 的核心逻辑（权限校验 → `MOUDLE.Open` → `MD.GetAPI` → `SchemaManage` → `BuildSQL01` → `ViewOperate01`）抽成内部服务。**原 Controller 和工具层都调它**，确保助理走和真实用户完全一致的权限与业务规则链路。

```csharp
// 当前用户上下文 —— 从 HttpContext.User claims 构造，与现有认证一致
public class UserContext {
  public string UserId;    // @_USERID_
  public string EmpId;     // @_EMPID_
  public string DeptId;    // @_DEPTID_
  public string[] RoleCodes;
  public string Token;     // 原始 token，透传给既有权限校验
}

public class DataCallService {
  // 从 DataController.Call 抽取，两个调用方共用
  public QueryResult  Query(string module, string apiCode, object input, UserContext u);
  public DataView     Open(string module, string id, UserContext u);
  public SaveResult   Save(string module, string xml, UserContext u);     // A04save，返回 before/after
  public void         Delete(string module, string[] ids, UserContext u); // A07delete
  public FlowResult   Flow(string module, string[] ids, string actionCode, UserContext u); // A17/A12/A14/A16...
}

public class SaveResult { public string Id; public bool Success; public object Before; public object After; }
```

- **覆盖的 APITYPE**：Query/Open/Save/Delete/Flow（submit/check/verify/reCheck/reVerify）。其余 `batchXxx`、子控制器自定义 `doMyApi` 接口（如 RM11 的 A51 证书预览）**不在助理工具范围**，后续按需扩展。
- **`Save` 返回变更前后数据**：`SaveResult` 含 `before/after`，工具层直接用于审计 `TBS_ASSISTANT_AUDIT`，**避免 save 前后各 open 一次的双查开销**（DataCallService.Save 内部在事务里读取 before、执行 update、读取 after）。
- `DataController.Call` 改为调用 `DataCallService`（保持现有行为不变），工具层也调它。权限校验与真实用户操作完全一致。

### 8 个工具签名与实现

| 工具 | 签名 | 实现 | 写? |
|---|---|---|---|
| `search_menu` | `(keyword)` | 查 `tss_func` 模糊匹配 `FUNCNAME`，返回 `{funcCode, funcName, path, moduleCode}` | ❌ |
| `get_module_schema` | `(moduleCode)` | 见下方返回结构 | ❌ |
| `query_data` | `(moduleCode, filter, fields?, page?, pageSize?)` | `DataCallService.Query(A01query)`，filter 按 schema 过滤器参数构造 | ❌ |
| `open_record` | `(moduleCode, id)` | `DataCallService.Open(A02open)`，返回主表+子表 | ❌ |
| `save_record` | `(moduleCode, data)` | data→FillData XML → `DataCallService.Save(A04save)` | ✅确认 |
| `delete_record` | `(moduleCode, ids[])` | `DataCallService.Delete(A07delete)` | ✅确认 |
| `flow_action` | `(moduleCode, ids[], actionCode)` | `DataCallService.Flow`（提交/复核/审批/驳回等状态流转） | ✅确认 |
| `navigate` | `(path, params?)` | 特殊：不调后端，引擎发 `navigate` 块，前端 `router.push` | ❌ |

**`get_module_schema` 返回结构**（同时喂给 LLM 和前端表单，一源两用）：
```json
{
  "moduleCode": "LI_M02",
  "moduleName": "收发记录",
  "filters": [
    {"filterCode":"F01", "params":[{"name":"CUSTNAME","type":"text","label":"客户"},{"name":"STATE","type":"number"}]}
  ],
  "fields": [
    {"name":"CUSTID","label":"客户","editType":"REF","required":true,"options":{...}},
    {"name":"BILLDATE","label":"单据日期","editType":"DATE"}
  ],
  "apis": {"query":"A01","open":"A02","save":"A04","delete":"A07","submit":"A17","check":"A12","verify":"A14"}
}
```
- `filters` 来自 `tss_resfilter.FILTERSQL` 的 `@XXX` 参数清单，`query_data` 的 `filter` 必须使用这些参数名（如 `{CUSTNAME:"某客户", STATE:2}`）。
- `fields` 按 `EDITSORT/QUERYSORT` 排序，大模块（如 LI_M02 字段上百个）**取前 N 个核心字段**，防 token 爆炸。

**`save_record` 的 `data` 结构**（按 FIELDNAME，区分主表/子表，对应 `tss_moudlepath` 的 MAIN/DTSA/DTSB/...）：
```json
{
  "main": {"CUSTID":"...","BILLDATE":"2026-06-21","REMARK":"..."},
  "subTables": {
    "DTSA": [{"ITEMNAME":"XX器具","QTY":2}],
    "DTSB": []
  }
}
```
格式转换层据此组装成后端 `FillData` 解析的 XML（`<表名 l="u" c="字段列表"><a><r c0=".." c1=".."/></a></表名>`）。

**`navigate` 的 `path`**：前端路由 path（来自 `tss_func.OUTERURL` 或前端路由表），`params` 作为 `query` 透传（如 `{path:"/r02/m07/main", params:{id:"xxx"}}`）。

**元数据懒加载**：不一次性把所有模块定义塞给 LLM，而是 `search_menu` → `get_module_schema` 按需获取。

## 确认门机制（写操作暂停/恢复）

```
引擎执行 save_record / delete_record / flow_action 前：
  1. 发 confirm 块 → { action, module, data(人类可读摘要), riskLevel, confirmId }
  2. await TaskCompletionSource<bool>   ← 引擎在此挂起
前端收到 confirm 块：
  → 弹确认框："将新增 1 条【校准记录】，客户=XX，器具=YY，是否执行？"
  → 用户点确认 → POST /api/assistant/confirm {confirmId, accepted:true}
后端收到确认：
  → resolve TaskCompletionSource → 引擎继续执行
  → 5 分钟未确认 → 自动取消该步骤
```

**confirmId 生命周期与并发模型**：
- **生成与存储**：`confirmId` = GUID，存内存字典 `Dictionary<confirmId, TaskCompletionSource<bool>>`，键值对带 5 分钟过期清理。
- **作用域**：confirmId 绑定到当前 SSE 请求所在的 Agent 循环（同一 HTTP 请求生命周期）。前端 `POST /api/assistant/confirm` 必须命中**同一进程实例**。
- **部署前提（重要）**：MVP 假设**单实例部署**（内存字典即生效）。若多实例/负载均衡，必须满足其一：① 会话亲和（sticky session，按 conversationId 路由到同一实例）；② 把 confirmGate 状态外置到 Redis。当前生产若为单实例可直接用方案①，多实例场景在 M1 后补充 Redis 方案。
- **多 confirm 串行**：一轮里多个写工具调用按 `foreach` 顺序串行，每个确认门独立 confirmId，前一个确认后才执行下一个。
- **超时与关闭浏览器**：5 分钟未确认自动取消该步骤（TaskCompletionSource 设为 false）。用户关闭浏览器再打开看不到"待确认"（SSE 请求已结束），但会话消息已持久化"该操作未在超时内确认"，用户可重新发起。

**风险分级**：
- `low`：单条查询、打开记录
- `medium`：单条新增/修改（`save_record`）
- `high`：删除、批量操作、状态流转（`delete_record` / `flow_action` 的审批/驳回）—— 确认门要求**额外勾选确认**（如勾选"我确认删除 N 条"）

## 前端抽屉 UI

### 组件结构与挂载

助理抽屉是**全局常驻组件**，挂在 `App.vue` 根节点，不随路由切换销毁。管理页面在 `s01` 下按标准模块结构开发。

```
src/components/assistant/              ← 全局组件
  AssistantDrawer.vue                  抽屉容器（右侧滑入 + 浮动按钮）
  AssistantMessageList.vue             消息流（按 block.type 分发渲染）
  AssistantInput.vue                   输入框 + 发送/停止
  blocks/
    ThinkingBlock.vue                  思考过程（可折叠）
    ToolCallBlock.vue                  工具调用卡片（工具名+参数+结果）
    TextBlock.vue                      Markdown 文字（流式累加）
    ChartBlock.vue                     ECharts 图表
    HtmlBlock.vue                      sanitize 后的 HTML
    FormBlock.vue                      可填报表单
    ConfirmBlock.vue                   写操作确认门
src/store/modules/assistant.js         Vuex：会话/消息流/SSE 状态

src/pages/s01/                         ← 管理页面（标准模块结构）
  llm-config/      router.js store.js views/main.vue views/add.vue
  llm-usage/       router.js store.js views/main.vue (+ ECharts 图表)
```

**抽屉交互**：右上角浮动按钮（💬）点击展开/收起；宽度约 420px，从右滑入，主内容区收窄（不遮挡）；顶部标题栏（新会话/历史），中间消息流（滚动），底部输入框。复用项目 `window.resize` 机制处理窗口变化。

### SSE 客户端 + 消息流渲染

发消息是 POST、响应是 SSE 流，用 `fetch` + `response.body.getReader()` 解析 `data:` 行（`EventSource` 只支持 GET，不适用）：

```js
// useAssistant.js 核心
async send(text) {
  this.alive = true
  const resp = await fetch('/api/assistant/send', {
    method: 'POST', body: JSON.stringify({ conversationId, message: text })
  })
  const reader = resp.body.getReader()
  const decoder = new TextDecoder()
  let buffer = '', sawDone = false
  try {
    while (this.alive) {
      const { done, value } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n\n'); buffer = lines.pop()
      for (const line of lines) {
        const block = JSON.parse(line.replace(/^data: /, ''))
        if (block.type === 'done') sawDone = true
        if (block.type === 'heartbeat') continue   // 心跳仅保活，不渲染
        this.dispatchBlock(block)
      }
    }
    if (!sawDone) this.showError('连接中断，点击重试')   // 异常断开
  } catch (e) { this.showError('网络异常，点击重试') }
}
```

**渲染分发**：一轮助理回复 = 一个 `assistantMessage`，内含 `blocks[]` 数组。每收到一个块 push 进去，Vue 响应式渲染对应子组件。`text` 块是片段，累加到同一 TextBlock 实现打字效果。

### 富内容块组件

| 块 | 组件 | 实现 | 依赖 |
|---|---|---|---|
| `text` | TextBlock | Markdown 渲染（流式累加） | 🆕 `marked` |
| `chart` | ChartBlock | `echarts.init(dom).setOption(option)` | ✅ 已有 ECharts 4 |
| `html` | HtmlBlock | `v-html` + **DOMPurify.sanitize** 白名单 | 🆕 `dompurify` |
| `form` | FormBlock | 动态表单（见下） | HeyUI Form |

**FormBlock 动态表单** —— 复用 `tss_resuipc.EDITTYPE` 映射 HeyUI 控件：

```js
// EDITTYPE → HeyUI 控件映射
const CONTROL_MAP = {
  TEXT:   'TextField',
  NUMBER: 'NumberField',
  DATE:   'DatePicker',
  SELECT: 'Select',         // options 来自 tss_resuipc.SELECTDATA
  REF:    'RefPicker',      // 引用选择器（复用现有 VBS 选择器逻辑）
  // ... 与现有 RsFormEdit 保持一致
}
```

表单规格来自 `get_module_schema`（同一份元数据喂给 LLM 和表单）。**填报流程**（独立端点，绕过确认门）：
```
用户填写 FormBlock → 点"提交"
  → POST /api/assistant/form-submit { formId, data }
  → AssistantController 内 action 直接调 DataCallService.Save，绕过 ConfirmGate
    （用户主动填表 = 已确认意图），仍记 TBS_ASSISTANT_AUDIT
  → 返回结果，FormBlock 显示"已创建：单据号 XXX"
```

> 区别：LLM 主动发起的 `save_record` 必须过确认门；用户手填表单走 `/api/assistant/form-submit` 独立端点，视为已确认，直接落库。`/api/assistant/form-submit` 是 `AssistantController` 内的一个 action（非独立 Controller），内部复用 `DataCallService.Save` + 审计。

### 确认门 UI

收到 `confirm` 块 → 用 HeyUI `Modal` 弹确认框，展示人类可读摘要 + 风险等级，确认/拒绝按钮。确认 → `POST /api/assistant/confirm { confirmId, accepted: true }`。

### 导航机制

`navigate` 块 → 抽屉调用 `this.$router.push({ path, query: params })`，页面跳转，**抽屉保持打开**（用户可继续看助理说明）。目标 `main.vue`/`add.vue` 支持通过 `query.id` 自动打开指定单据（各页面轻量适配：`created` 读 `this.$route.query.id`，有则触发 `open(id)`）。

## 管理后台

### LLM 配置（系统管理 s01，管理员可见）

**新增表 `TBS_LLM_CONFIG`**：

| 字段 | 类型 | 说明 |
|---|---|---|
| ID | char(36) | 主键 GUID |
| PROVIDER | varchar | 供应商标识，如 `DeepSeek`（留多 provider 扩展） |
| APIKEY | varchar | **AES 加密存储**，绝不明文 |
| MODELNAME | varchar | 模型名，如 `deepseek-chat` |
| BASEURL | varchar | API 地址 |
| PRICEINPUT | decimal | 输入单价（元/千token） |
| PRICEOUTPUT | decimal | 输出单价（元/千token） |
| PARAMS | text | temperature/max_tokens 等(JSON) |
| ENABLED | tinyint | 是否启用（同时只启用一个） |
| ISDELETED | tinyint | 逻辑删除 |

**配置页面要点**：
- API Key 用密码框输入，列表/详情**脱敏回显**（如 `sk-****...3a7f`），永不返回明文
- 加密密钥放 `appsettings.json`（不入库、不入 git）
- 涉及加密/脱敏，后端用**自定义 Controller**（仿 `RM11Controller` 模式）
- Agent 引擎每次调 LLM 前，从这里读启用的配置取 key/model/价格

### 用量统计

**新增表 `TBS_LLM_USAGE`**（每次 LLM 调用记一条）：

| 字段 | 类型 | 说明 |
|---|---|---|
| ID | char(36) | 主键 |
| USERID / USERNAME | varchar | 谁用的（冗余姓名便于统计） |
| CONVERSATIONID | varchar | 所属会话 |
| MODULECODE | varchar | 涉及模块（可空） |
| TOOLNAME | varchar | 调用的工具（可空） |
| OPERATIONTYPE | varchar | chat/query/save/navigate 等 |
| PROMPTTOKENS | int | 输入 token |
| COMPLETIONTOKENS | int | 输出 token |
| TOTALTOKENS | int | 合计 |
| COST | decimal(10,4) | 输入×PRICEINPUT + 输出×PRICEOUTPUT |
| DURATIONMS | int | 耗时 |
| ISSUCCESS / ERRORMSG | tinyint/text | 成败与错误 |
| REQUESTTIME | datetime | 调用时间 |
| ISDELETED | tinyint | 逻辑删除 |

**记录时机**：Agent 引擎每次调用 DeepSeek 后，从响应 `usage` 字段取 token 数，结合配置单价算费用，写入。

**统计页面**（元数据驱动 + ECharts）：
- 明细列表：每条调用记录
- 汇总视图 `VRP_LLM_USAGE_BY_USER`：`GROUP BY USERID, USERNAME`，输出 `总调用次数 / SUM(PROMPTTOKENS) / SUM(COMPLETIONTOKENS) / SUM(TOTALTOKENS) / SUM(COST) / MAX(REQUESTTIME) 最后使用时间`，可按时间段过滤
- 趋势图表：按日/月的使用量与费用曲线（ECharts）

> **自洽彩蛋**：用量存在普通表里，助理自己也能查——"这个月谁用我最多？"助理用 `query_data` 查 `TBS_LLM_USAGE` 就能回答并画图。

## 会话记忆

| 层级 | 方案 |
|---|---|
| **短期记忆**（会话内） | 滑动窗口：system 提示词 + 最近 N 条消息，超长截断老的。热数据内存缓存。 |
| **历史回看** | 会话存 `TBS_ASSISTANT_CONVERSATION` + `TBS_ASSISTANT_MESSAGE`，抽屉可分页查看历史会话。 |
| **长期记忆/用户画像** | ❌ YAGNI，不做。不引入向量库/跨会话偏好记忆。 |
| **会话隔离** | 每个 `conversationId` 绑定 `userid`；用户只能看自己的会话；管理员可查全部，且**查看非本人会话记入 `TBS_ASSISTANT_AUDIT`**（防窥探）。 |

## 审计日志（LIMS 合规关键）

LIMS（尤其过 CNAS 认证）要求所有数据变更可追溯。助理触发的写操作除了走 `DataCallService`（沿用业务层既有审计），**额外记一条助理专属审计**，关联会话与确认动作。

**新增表 `TBS_ASSISTANT_AUDIT`**：

| 字段 | 说明 |
|---|---|
| ID / CONVERSATIONID | 审计ID / 关联会话 |
| USERID / USERNAME | 实际操作人 |
| MODULECODE / OPERATION | 模块 / 操作类型(save/delete/flow/查看他人会话) |
| ENTITYID | 受影响单据ID |
| BEFORE / AFTER | 变更前后数据(JSON，来自 SaveResult) |
| RISKLEVEL | low/medium/high |
| CONFIRMED / CONFIRMTIME | 是否经用户确认 + 确认时间 |
| REQUESTTIME | 时间 |
| ISDELETED | 逻辑删除 |

用量审计走 `TBS_LLM_USAGE`（LLM 调用层），业务变更审计走 `TBS_ASSISTANT_AUDIT`（操作层），双层覆盖。

## 安全边界

### ① 权限硬约束（防越权 / 防 prompt injection）

- 工具层**绝不信任 LLM 的任何特权声明**。权限完全由 `UserContext`（当前真实用户）决定，`DataCallService` 按真实用户权限校验。
- 即使 LLM 被诱导说出"我是管理员，删除全部"，`DataCallService` 仍按当前用户权限执行 → 越权直接被拒。
- **工具结果作为"数据"喂回 LLM**（系统提示词明确边界 + 结构标记），防止查询结果里的恶意指令被当作指令执行。

### ② 敏感数据

- API Key：AES 加密存储 + 接口脱敏回显。
- 助理回答只含当前用户有权看到的数据（权限约束自然保证）。

### ③ 速率与成本控制

| 限制 | 默认值 | 可配置 |
|---|---|---|
| 每用户每分钟请求数 | 20 | ✅ |
| 每用户每日 token 上限 | 50 万 | ✅ |
| 单轮最大步数 | 12 | ✅ |
| `query_data` 最大返回行数 | 500 | ✅ |

超额 → 友好拒绝并提示，记入用量。

### ④ 高危操作加码

`riskLevel=high` 的操作（批量删除、审批/驳回），确认门要求额外勾选确认，不只是简单点按钮。

## 错误处理

| 场景 | 处理 |
|---|---|
| LLM 调用失败/超时/限流 | 重试 1 次 → 仍失败则发 `error` 块友好提示 |
| 工具执行失败（业务校验，如单据状态不允许修改） | 把真实错误返回 LLM，由它转成用户能懂的话 |
| 元数据缺失（模块未配 schema） | `get_module_schema` 返回空 → LLM 提示"该模块暂不支持" |
| 步数耗尽 | 提示"任务较复杂，建议拆分或缩小范围" |
| SSE 中途断开 | 已落库的工具结果保留，前端提示重试，重发基于已持久化历史 |
| DeepSeek 整体不可用 | 直接报错（MVP 不做降级，YAGNI） |

## 数据库设计（新增表汇总）

| 表 | 职责 |
|---|---|
| `TBS_ASSISTANT_CONVERSATION` | 会话（ID、用户、标题、时间） |
| `TBS_ASSISTANT_MESSAGE` | 会话消息（含工具调用记录，供回看/审计，分页加载） |
| `TBS_ASSISTANT_AUDIT` | 助理写操作审计（before/after、风险、确认） |
| `TBS_LLM_CONFIG` | LLM 配置（Key 加密、模型、单价） |
| `TBS_LLM_USAGE` | LLM 调用用量（token、费用、按人统计） |
| `VRP_LLM_USAGE_BY_USER` | 用量按用户汇总视图（报表） |

字段统一遵循项目规范：大写无下划线、`ISDELETED` 逻辑删除（tinyint 默认 0）。物理表需在 `tss_resource`/`tss_resfield`/`tss_resuipc` 注册元数据（`TBS_` 表 REFFIELDID 为 NULL）。配置页因涉及加密用自定义 Controller，其余统计页走元数据驱动 CRUD。

## 新增依赖

### 前端
- `marked` —— Markdown 渲染
- `dompurify` —— XSS 防护（HTML 白名单）

（ECharts 4 已有；HeyUI Form 已有）

> **构建兼容性**：`marked` / `dompurify` 需先做一次 Webpack 3 + Babel 构建冒烟测试（`dompurify` 新版对构建环境/Node 版本有要求），锁定兼容版本号后再进 M1。

### 后端
- DeepSeek HTTP 客户端（兼容 OpenAI 格式，可用现有 `HttpClient` 或轻量 SDK）
- AES 加密（.NET 内置 `System.Security.Cryptography`，无需额外依赖）

## 构建里程碑（一次性交付，渐进式构建）

| 里程碑 | 内容 | 验证标准 |
|---|---|---|
| **M1 地基** | DataCallService 抽取、DeepSeek 接入+SSE、会话存储、基础抽屉UI、用量埋点 | 能发消息、流式回复、用量入库 |
| **M2 只读能力** | `search_menu`/`get_module_schema`/`query_data`/`open_record` + TextBlock | 能问数据、找模块、Markdown 回答 |
| **M3 富内容** | ChartBlock / HtmlBlock / FormBlock | 图表/HTML/可填报表单 |
| **M4 写操作+确认门** | `save_record`/`delete_record`/`flow_action` + ConfirmBlock + 审计 | 新增/修改/删除/审批单据，全程确认可审计 |
| **M5 导航** | `navigate` + 页面 query.id 适配 | 自然语言跳转并打开单据 |
| **M6 管理后台** | LLM 配置页 + 用量统计页（s01 模块） | 管理员配置 Key、看用量统计 |

## 文件变更清单

### 后端新增

| 文件 | 职责 |
|---|---|
| `Controllers/AssistantController.cs` | 助理主 Controller（ReAct 循环、SSE、确认门、`/send`/`/confirm`/`/form-submit` action） |
| `Services/DataCallService.cs` | 从 DataController 抽取的统一数据操作服务（Query/Open/Save/Delete/Flow） |
| `Services/DeepSeekClient.cs` | DeepSeek API 调用（流式 + function calling） |
| `Services/AssistantToolExecutor.cs` | 8 个工具的实现与分发 |
| `Services/ConfirmGate.cs` | 确认门暂停/恢复（TaskCompletionSource + 过期清理） |
| `Services/UsageLogger.cs` | 用量记录 |
| `Services/AuditLogger.cs` | 助理写操作审计 |
| `Services/SessionStore.cs` | 会话上下文管理（内存缓存 + DB 持久） |
| `Services/HeartbeatTimer.cs` | SSE 心跳保活 |
| `Controllers/LLMConfigController.cs` | LLM 配置（加密/脱敏，自定义） |

### 后端修改

| 文件 | 修改内容 |
|---|---|
| `Controllers/DataController.cs` | 抽取核心逻辑到 DataCallService，Call 方法改为调用它（行为不变） |
| `Startup.cs` | 注册新 Controller 路由、注入服务 |

### 前端新增

| 文件 | 职责 |
|---|---|
| `components/assistant/AssistantDrawer.vue` | 抽屉容器 |
| `components/assistant/AssistantMessageList.vue` | 消息流分发 |
| `components/assistant/AssistantInput.vue` | 输入框 |
| `components/assistant/blocks/*.vue` | 8 个消息块组件 |
| `components/assistant/useAssistant.js` | SSE 客户端 + 状态逻辑（含断线/重试） |
| `store/modules/assistant.js` | Vuex 模块 |
| `pages/s01/llm-config/*` | LLM 配置页（标准模块结构） |
| `pages/s01/llm-usage/*` | 用量统计页（标准模块结构） |

### 前端修改

| 文件 | 修改内容 |
|---|---|
| `App.vue` | 挂载 AssistantDrawer 全局组件 + 浮动按钮 |
| `package.json` | 新增 `marked`、`dompurify`（锁定兼容版本） |
| 各业务 `main.vue`/`add.vue` | 支持 `query.id` 自动打开单据（M5 逐步适配） |
| 菜单元数据 `tss_func` | 仅注册 LLM 配置/用量统计菜单（助理抽屉是全局常驻组件，无需菜单入口） |
