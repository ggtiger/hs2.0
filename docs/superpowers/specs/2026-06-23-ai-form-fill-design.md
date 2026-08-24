# AI 对话自动填报表单 - 设计文档

## 需求

在真实表单页（add.vue / RsFormEdit）上提供「AI 填报」按钮，用户点击后在右侧弹出专属侧栏，AI 通过对话（自由描述 + 不足追问）针对性收集信息，自动填写当前表单字段。AI **只填不提交**，用户复核后正常提交。

与 M4「变更跳转真实页面」一致：AI 辅助录入，提交权在用户。

## 架构

- **AI 按钮注入 RsFormEdit**（全局，所有表单自动获得）。
- **FormAssistantPanel**：右侧独立侧栏，专属当前表单的会话，不复用全局抽屉。
- **复用 M1-M3 基建**：SignalR 连接、DeepSeek ReAct 循环、get_module_schema/query_data 工具。新增 `fill_form` 工具 + `AskForm` hub 方法。
- **事件隔离**：全局抽屉用 `block` 事件，表单面板用 `formblock` 事件（同一连接，互不干扰）。

## 数据流

```
用户在 add.vue 表单 → 点「✨ AI 填报」
  → FormAssistantPanel 右侧弹出（带 moduleCode + datatable）
  → 共享 SignalR 连接，invoke AskForm(moduleCode, msg, userInfo)
  → 后端 form-fill ReAct 循环：
      get_module_schema 了解字段+refFields
      → 用户自由描述
      → query_data 解析引用字段(名称→ID+名称)
      → fill_form({字段:值}) → hub 推 formblock{type:fill,fields} → panel emit fill
      → RsFormEdit.applyFill → path.setValue → 表单实时刷新
      → 缺必填字段 → 追问 → 再 fill_form
  → 用户复核表单 + 点「确定」正常提交（AI 不碰提交）
```

## 后端

### fill_form 工具（AssistantToolExecutor）
- 定义：`fill_form(fields:object)`，fields={FIELDNAME:值}。
- Execute 返回 `FillResult{fields, filled}`。
- hub 工具循环检测 FillResult → 推 `formblock{type:"fill", fields}`。

### form-fill 工具集
`GetFormFillToolDefinitions()`：get_module_schema、query_data、search_menu、fill_form（不含 query_stats/open_record/navigate）。

### AskForm hub 方法（AssistantHub）
- 签名：`AskForm(moduleCode, message, userInfoJson)`。
- per-connection 内存会话（`ConcurrentDictionary<connectionId, List>`，不落库——form-fill 临时）。
- 系统提示词：了解字段→收集→query_data 解析引用→fill_form 填充→缺必填追问→**绝不提交**。
- 推送用 `formblock` 事件（区别于 Ask 的 `block`）。
- 复用 ReAct 循环（抽成共享 RunAgentLoop）。
- OnDisconnectedAsync 清理该连接的会话。

## 前端

### rs-form-edit.vue（注入）
- 右上角「✨ AI 填报」按钮 → showAiPanel。
- 挂载 `<FormAssistantPanel :moduleCode @fill="applyFill" @close>`。
- `applyFill(fields)`：遍历调 `this.path.setValue(field, value)`（同一 datatable，实时刷新）。
- moduleCode：从绑定的模块 store 读取（createStore config.moduleCode）。

### FormAssistantPanel.vue（右侧侧栏）
- 固定右侧滑入（~380px），独立会话状态。
- 复用共享 SignalR 连接（api/assistant.js 的 ensureConnected），监听 `formblock`，invoke `AskForm`。
- 渲染 text/tool_call/tool_result（复用 TextBlock/ToolCallBlock）；fill → emit('fill', fields)。

### api/assistant.js
- 导出 `ensureConnected`（共享连接）。
- `sendFormMessage(moduleCode, message, onBlock)`：conn.on('formblock', onBlock) + invoke AskForm。

## 引用字段解析 + 边界
- AI 看 refFields 知道引用关系；query_data 查名称→ID+名称，填两字段（autocomplete 一致）。多匹配列出让用户选。
- 字段名只能来自 schema（臆造时返回真实字段纠正）。
- AI 只填不提交；必填缺失追问；用户可手动改。

## 文件清单
- 后端：AssistantToolExecutor（fill_form + FillResult + GetFormFillToolDefinitions）、AssistantHub（AskForm + RunAgentLoop 重构 + formblock + OnDisconnected）。
- 前端：rs-form-edit.vue（按钮+applyFill）、FormAssistantPanel.vue（新增）、api/assistant.js（导出+sendFormMessage）。
