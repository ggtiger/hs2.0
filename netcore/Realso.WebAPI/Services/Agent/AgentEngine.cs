using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// Agent ReAct 循环骨架（4 套循环共性的统一出口）。
  ///
  /// 提取自 AssistantHub.RunAgentLoop / AiDevOrchestrator.GenerateAsync /
  /// WizardStepOrchestrator.GenerateStepAsync / RMSfcAiController.GenerateCode 的共性：
  ///   for step in MaxSteps:
  ///     ① 流式调 LLM（onContent 推 text block）
  ///     ② 记录 usage（IUsageRecorder）
  ///     ③ 无 tool_calls → 最终回答，break
  ///     ④ assistant tool_calls 加入 messages
  ///     ⑤ 遍历 tool_calls：前端工具走 FrontendHandler，后端工具走 ToolExecutor；
  ///        推 tool_call/tool_result 块；tool result 截断后加入 messages（tool 角色）
  ///     ⑥ DevAgentEngine 在 ⑤ 后插 ChangeItem 钩子
  ///   达上限推 error，最后推 done
  ///
  /// 差异通过依赖注入参数化：
  ///   - IAgentEventSink：事件推送（SignalR/回调适配）
  ///   - IToolExecutor：工具执行（AssistantToolExecutor/SfcAiToolExecutor）
  ///   - IUsageRecorder：用量记录（Db/Aggregate/Null）
  ///   - ToolContext.FrontendHandler：前端工具（仅助理/表单场景）
  ///   - AgentOptions：MaxSteps/截断/心跳
  /// </summary>
  public class AgentEngine
  {
    private readonly LlmClient _llm;

    public AgentEngine(LlmClient llm) { _llm = llm; }

    /// <summary>
    /// 执行 ReAct 循环。
    /// </summary>
    public virtual async Task<AgentRunResult> RunLoopAsync(
      AgentRunRequest req,
      IAgentEventSink sink,
      IToolExecutor executor,
      IUsageRecorder usageRecorder)
    {
      var opts = req.Options ?? new AgentOptions();
      var sw = Stopwatch.StartNew();
      bool gotAnswer = false;
      string finalText = null;
      int totalPrompt = 0, totalCompletion = 0;

      // 心跳 Task（SFC 场景）
      CancellationTokenSource heartbeatCts = null;
      if (opts.EnableHeartbeat)
      {
        heartbeatCts = new CancellationTokenSource();
        _ = HeartbeatLoopAsync(sink, opts.HeartbeatInterval, heartbeatCts.Token);
      }

      try
      {
        for (int step = 0; step < opts.MaxSteps; step++)
        {
          string text = "";
          var usage = await _llm.StreamChatAsync(req.Cfg, req.Messages, req.Tools,
            onContent: async c =>
            {
              text += c;
              await sink.OnContent(c);
            });

          totalPrompt += usage.PromptTokens;
          totalCompletion += usage.CompletionTokens;

          // 记录用量
          if (usageRecorder != null)
          {
            usageRecorder.Record(new UsageRecord
            {
              UserId = req.UserId,
              UserName = req.UserName,
              ConversationId = req.ConversationId,
              OperationType = req.OperationType,
              ModuleCode = req.ModuleCode,
              PromptTokens = usage.PromptTokens,
              CompletionTokens = usage.CompletionTokens,
              PriceInput = req.Cfg.PriceInput,
              PriceOutput = req.Cfg.PriceOutput,
              DurationMs = (int)sw.ElapsedMilliseconds,
              Success = true
            });
            sw.Restart();
          }

          if (!usage.HasToolCalls)
          {
            gotAnswer = true;
            finalText = text;
            break;
          }

          // assistant tool_calls 加入 messages
          var toolCallsForMsg = new List<object>();
          foreach (var tc in usage.ToolCalls)
          {
            var jtc = (JObject)tc;
            string tcId = jtc["id"]?.ToString();
            string fnName = jtc["function"]?["name"]?.ToString();
            string fnArgs = jtc["function"]?["arguments"]?.ToString() ?? "{}";
            toolCallsForMsg.Add(new { id = tcId, type = "function", function = new { name = fnName, arguments = fnArgs } });
          }
          req.Messages.Add(new { role = "assistant", content = string.IsNullOrEmpty(text) ? null : text, tool_calls = toolCallsForMsg });

          // 遍历执行 tool_calls，收集结果供 OnToolsExecutedAsync 钩子用
          var toolCallResults = await ExecuteToolCallsAsync(usage.ToolCalls, req, sink, executor);

          // DevAgentEngine 的 ChangeItem 钩子（子类 override 调用）
          await OnToolsExecutedAsync(toolCallResults, req, sink);
        }

        if (!gotAnswer)
        {
          await sink.OnError(string.Format(opts.MaxStepsMessage, opts.MaxSteps));
        }

        // AggregateUsageReporter 在 done 时 flush
        if (usageRecorder is AggregateUsageReporter agg)
        {
          agg.FlushAndLog();
        }

        await OnLoopDoneAsync(req, sink);
        await sink.OnDone(opts.EnableHeartbeat ? new { promptTokens = totalPrompt, completionTokens = totalCompletion } : null);
        return new AgentRunResult { GotAnswer = gotAnswer, FinalText = finalText, Usage = (totalPrompt, totalCompletion) };
      }
      finally
      {
        heartbeatCts?.Cancel();
      }
    }

    /// <summary>
    /// 执行 tool_calls 列表。前端工具走 FrontendHandler，后端工具走 ToolExecutor。
    /// 推 tool_call/tool_result 块，特殊结果（navigate/fill/subtable）走对应 sink 回调。
    /// tool result 截断到 MaxToolResultChars 加入 messages（tool 角色）。
    /// 返回每个工具调用的完整上下文（名称+参数+原始结果+messages 索引），供 OnToolsExecutedAsync 钩子用。
    /// </summary>
    protected virtual async Task<List<ToolCallResult>> ExecuteToolCallsAsync(List<object> toolCalls, AgentRunRequest req, IAgentEventSink sink, IToolExecutor executor)
    {
      var opts = req.Options ?? new AgentOptions();
      var ctx = req.ToolContext;
      var results = new List<ToolCallResult>();

      foreach (var tc in toolCalls)
      {
        var jtc = (JObject)tc;
        string tcId = jtc["id"]?.ToString();
        string fnName = jtc["function"]?["name"]?.ToString();
        JObject args;
        try { args = JObject.Parse(jtc["function"]?["arguments"]?.ToString() ?? "{}"); }
        catch { args = new JObject(); }

        var tcr = new ToolCallResult { ToolCallId = tcId, ToolName = fnName, Args = args };
        bool isFrontend = executor != null && executor.IsFrontendTool(fnName);
        tcr.IsFrontend = isFrontend;

        string resultJson;
        if (isFrontend && ctx?.FrontendHandler != null)
        {
          // 前端工具：只推 frontend_tool_call 给前端代理层执行，等回传
          // 不推 tool_call/tool_result block —— 前端工具的 UI 状态由前端自己管理
          resultJson = await ctx.FrontendHandler.ExecuteFrontendTool(fnName, args);
          tcr.ResultJson = resultJson;
        }
        else
        {
          // 后端工具：推 tool_call 让前端展示，执行后推 tool_result
          await OnToolStepStart(fnName, req, sink);  // onStep start（AiDev/Wizard 步骤条，先于 tool_call）
          await sink.OnToolCall(fnName, args.ToString());

          var result = await executor.Execute(fnName, args, ctx);
          tcr.OriginalResult = result?.Data;
          resultJson = result?.Data != null ? JsonConvert.SerializeObject(result.Data) : "{}";
          tcr.ResultJson = resultJson;

          // 特殊结果类型走对应 sink 回调（通用助理场景）
          await OnToolResultAsync(fnName, result, sink);

          string summary = result?.Summary;
          if (string.IsNullOrEmpty(summary))
            summary = resultJson.Length > opts.SummaryTruncateChars
              ? resultJson.Substring(0, opts.SummaryTruncateChars) + "..."
              : resultJson;
          await sink.OnToolResult(fnName, summary);
          await OnToolStepEnd(fnName, result, req, sink);  // onStep done/skipped
        }

        // tool result 截断后加入 messages（tool 角色），喂回 LLM 下一轮
        // 用 Dictionary 而非匿名对象：DevAgentEngine 去重改写时需修改 content（匿名对象属性只读）
        string contentForLlm = opts.MaxToolResultChars > 0 && resultJson.Length > opts.MaxToolResultChars
          ? resultJson.Substring(0, opts.MaxToolResultChars) + "\n...(结果过长已截断)"
          : resultJson;
        var toolMsg = new Dictionary<string, object>
        {
          { "role", "tool" },
          { "tool_call_id", tcId },
          { "content", contentForLlm }
        };
        req.Messages.Add(toolMsg);
        tcr.ToolMessageIndex = req.Messages.Count - 1;
        results.Add(tcr);
      }
      return results;
    }

    /// <summary>
    /// 特殊工具结果的后处理钩子（通用助理的 navigate/fill/subtable）。
    /// 默认空实现，AssistantHub 场景由子类或调用方注入。
    /// DevAgentEngine 不需要此钩子（开发工具不产 navigate/fill）。
    /// </summary>
    protected virtual Task OnToolResultAsync(string toolName, ToolResult result, IAgentEventSink sink)
    {
      return Task.CompletedTask;
    }

    /// <summary>
    /// 工具执行后的钩子。DevAgentEngine override 在此调 TryBuildChangeItem→ChangeSetEngine.AppendItem→去重改写。
    /// 默认空实现。
    /// </summary>
    protected virtual Task OnToolsExecutedAsync(List<ToolCallResult> toolCallResults, AgentRunRequest req, IAgentEventSink sink)
    {
      return Task.CompletedTask;
    }

    /// <summary>工具执行前钩子（AiDev/Wizard 推 onStep start，先于 tool_call）。默认空。</summary>
    protected virtual Task OnToolStepStart(string toolName, AgentRunRequest req, IAgentEventSink sink) => Task.CompletedTask;

    /// <summary>工具执行后钩子（AiDev/Wizard 推 onStep done/skipped）。默认空。</summary>
    protected virtual Task OnToolStepEnd(string toolName, ToolResult result, AgentRunRequest req, IAgentEventSink sink) => Task.CompletedTask;

    /// <summary>循环结束钩子（DevAgentEngine 调 ValidateChangeSet 推 onValidate）。在 OnDone 前调用。默认空。</summary>
    protected virtual Task OnLoopDoneAsync(AgentRunRequest req, IAgentEventSink sink) => Task.CompletedTask;

    /// <summary>判断工具是否为前端工具（遍历 ToolContext.FrontendHandler 能处理的工具集）</summary>
    private bool IsFrontendToolByRegistry(string toolName, IToolExecutor executor, ToolContext ctx)
    {
      // 前端工具由前端 aiAgentProxy 注册，后端 ToolExecutor.IsFrontendTool 已覆盖
      return executor != null && executor.IsFrontendTool(toolName);
    }

    private async Task HeartbeatLoopAsync(IAgentEventSink sink, TimeSpan interval, CancellationToken token)
    {
      try
      {
        while (!token.IsCancellationRequested)
        {
          await Task.Delay(interval, token);
          if (!token.IsCancellationRequested)
            await sink.OnHeartbeat();
        }
      }
      catch (TaskCanceledException) { }
    }
  }
}
