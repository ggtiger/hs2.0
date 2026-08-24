# 智能体提示词页面配置 + AI 自动优化 设计

## 背景

当前智能助理的提示词硬编码在 `AssistantHub.cs`：
- `BuildLlmMessages` 里的通用助理 system prompt
- `AskForm` 里的表单填报 system prompt
- 工具描述分散在前端 `aiAgentProxy.js`（getDefinition）和后端 `AssistantToolExecutor.cs`（GetToolDefinitions）

需求：提示词可通过管理页面配置，且页面提供"AI 优化"按钮一键优化提示词。

## 目标

1. 提示词存数据库，管理页面可编辑（system prompt + 工具描述文字）
2. 页面"AI 优化"按钮，调 LLM 优化提示词，用户确认后保存
3. 修改后实时生效（带短时缓存）

## 非目标

- 工具名/参数结构不改（保持代码定义，增删工具仍改代码）
- 不写专门 Controller，数据 CRUD 走 ORM 元数据驱动

## 数据模型

### 物理表 `TBS_ASSISTANT_PROMPT`

| 字段 | 类型 | 说明 |
|------|------|------|
| ID | char(36) | 主键，GUID |
| PROMPTKEY | varchar(64) | 提示词键，唯一索引 |
| CONTENT | text | 提示词内容 |
| DESCRIPTION | varchar(255) | 说明（给管理员看） |
| UPDATEDBY | varchar(36) | 修改人 ID |
| UPDATETIME | datetime | 修改时间 |
| ISDELETED | tinyint | 逻辑删除，默认 0 |

PROMPTKEY 约定：
- `system_general` — 通用助理 system prompt
- `system_form` — 表单填报 system prompt
- `tool:<工具名>` — 单个工具的 description（如 `tool:navigate`、`tool:fill_form`）

### 初始数据

从当前硬编码迁移，启动时若表为空或某 key 不存在，用代码里的默认值兜底（保证系统可用）。

## ORM 元数据配置

按项目元数据驱动架构，配置以下元数据，CRUD 走统一入口 `POST api/Data/call/RS_M07/{ApiCode}`：

| 表 | 配置 |
|----|------|
| TSS_RESOURCE | TBS_ASSISTANT_PROMPT（TABLE，RESOURCENAME=`tbs_assistant_prompt`）+ VCK_ASSISTANT_PROMPT（DATAVIEW，TABLERESOURCEID 指向物理表） |
| TSS_RESFIELD | 物理表 + DATAVIEW 字段定义（ID/PROMPTKEY/CONTENT/DESCRIPTION/UPDATEDBY/UPDATETIME/ISDELETED），DATAVIEW 字段 REFFIELDID 关联物理表字段 |
| TSS_RESFILTER | F00（`PROMPTKEY=@PROMPTKEY` 单条）/ F01（列表查询） |
| TSS_RESUIPC | UI 配置（PROMPTKEY/DESCRIPTION 列表显示，CONTENT 编辑 textarea） |
| TSS_MOUDLE | RS_M07 模块 |
| TSS_MOUDLEPATH | MAIN 数据源 → VCK_ASSISTANT_PROMPT |
| TSS_MOUDLEAPI | A01query/A02open/A04save/A07delete |
| TSS_FUNC | 菜单项（系统管理下） |
| TSS_FUNCPOINT | 权限点 |

## 前端页面 `src/pages/s01/m11/`

标准模块结构：
- `router.js` — require.ensure 懒加载
- `store.js` — createStore.getStore 注册 RS_M07
- `views/main.vue` — 列表页（rs-table-list，显示 PROMPTKEY/DESCRIPTION/UPDATETIME）
- `views/add.vue` — 编辑页（rs-form-edit，CONTENT 用 textarea 编辑）
  - 加"✨ AI优化"按钮：调 SignalR `OptimizePrompt(content)`，返回优化结果填入 CONTENT，用户确认后点保存

AI 优化交互：
1. 用户点"AI优化"
2. 前端 `conn.invoke('OptimizePrompt', currentContent)` 
3. 后端调 LLM 返回优化后文本
4. 前端把结果填入 CONTENT textarea（用户可再编辑）
5. 用户点保存（走 ORM A04save）

## 后端改造

### PromptService（新增，带缓存）

`Services/PromptService.cs`：
- `Get(string key)` → MemoryCache 30s + 查 TBS_ASSISTANT_PROMPT（按 PROMPTKEY）
- `GetDefault(string key)` — 表里没有时返回代码默认值（兜底）
- `ClearCache(string key)` / `ClearAll()` — 保存后清缓存

### AssistantHub 改造

1. **`BuildLlmMessages`**（通用助理）：system prompt 改调 `_promptService.Get("system_general")`
2. **`AskForm`**（表单填报）：system prompt 改调 `_promptService.Get("system_form")`，`currentDataPrompt` 仍动态拼
3. **工具描述覆盖**：`MergeWithFrontendTools` 合并工具定义时，对每个工具查 `tool:<name>`，若表里有则覆盖 description
4. **新增 Hub 方法 `OptimizePrompt`**：
   ```csharp
   public async Task<string> OptimizePrompt(string content)
   ```
   - 调 `DeepSeekClient.StreamChatAsync`，用固定 meta-prompt：
     "你是提示词优化专家。优化以下提示词，使其更清晰、准确、有效，保持原意和要点，直接返回优化后的完整提示词，不要解释：\n\n{content}"
   - 流式累积返回最终文本（前端 conn.invoke 的返回值）
   - 不走 ReAct 循环，不调工具

### 前端工具描述

前端 `aiAgentProxy.js` 的工具 description 也从表读：
- `initConnection`（FormAssistantPanel）/插件初始化时，调 ORM API 加载所有 `tool:*` 提示词
- `getDefinition()` 时用表里的 description 覆盖代码默认值
- 表里没有时用代码默认值兜底

## 数据流

### 读取（AssistantHub 构造消息）
```
BuildLlmMessages → PromptService.Get("system_general") → MemoryCache(30s) → 查 TBS_ASSISTANT_PROMPT
MergeWithFrontendTools → 对每个工具 PromptService.Get("tool:<name>") 覆盖 description
```

### 编辑保存
```
前端 s01/m11 add.vue → rs-form-edit save → POST api/Data/call/RS_M07/A04save → ORM save TBS_ASSISTANT_PROMPT
```

### AI 优化
```
add.vue "AI优化"按钮 → conn.invoke('OptimizePrompt', content) → AssistantHub.OptimizePrompt
  → DeepSeekClient(meta-prompt + content) → 返回优化文本 → 前端填入 CONTENT
用户确认 → 保存（A04save）
```

## 错误处理

- 表里没数据/key 不存在：用代码默认值兜底，系统可用
- AI 优化失败（LLM 未配置/网络错）：返回错误提示，CONTENT 保持原值
- 缓存过期自动重查

## 测试

- 页面能列出/编辑/保存提示词
- 修改 system_general 后，下次对话立即用新 prompt
- AI 优化按钮返回优化后文本
- 工具描述修改后，LLM 工具列表用新描述
- 表空时系统正常（代码默认值兜底）

## 实现顺序

1. 建表 + 初始数据（迁移硬编码 prompt）
2. ORM 元数据配置（RS_M07 模块）
3. PromptService（带缓存）
4. AssistantHub 改造（读表 + OptimizePrompt Hub 方法）
5. 前端 s01/m11 页面（列表 + 编辑 + AI 优化按钮）
6. 前端工具描述从表读
7. 测试验证
