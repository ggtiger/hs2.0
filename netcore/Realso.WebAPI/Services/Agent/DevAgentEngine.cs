using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Realso.WebAPI.Models.AiDev;
using Realso.WebAPI.Services.AiDev;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 开发场景 Agent 引擎（继承 AgentEngine，扩展 ChangeItem 钩子 + onStep + ValidateChangeSet）。
  /// 用于 AiDev（NEW/MODIFY）和 Wizard（6 步分步）场景。
  ///
  /// - OnToolsExecutedAsync：每轮工具执行后调 TryBuildChangeItem -> ChangeSetEngine.AppendItem ->
  ///   去重改写 messages 里 tool result content -> eventSink.OnItem
  /// - OnToolStepStart/End：推 onStep start/done/skipped（按 req.ToolToStepMapper 映射步骤 key）
  /// - OnLoopDoneAsync：循环结束调 ValidateChangeSet -> eventSink.OnValidate
  ///
  /// 逻辑从 AiDevOrchestrator L188-215 / WizardStepOrchestrator L242-264 提取，消除两份复制粘贴。
  /// </summary>
  public class DevAgentEngine : AgentEngine
  {
    public DevAgentEngine(LlmClient llm) : base(llm) { }

    // 只读工具（无变更产出）：onStep 推 skipped 而非 done
    private static readonly HashSet<string> READONLY_TOOLS = new HashSet<string>
    {
      "search_existing_resource", "read_table_schema", "get_module_schema",
      "search_menu", "search_dict", "read_sfc_template"
    };

    /// <summary>
    /// 工具执行后：把产出类工具结果转成 ChangeItem 追加到 changeset，去重时改写 tool result 告知 LLM。
    /// </summary>
    protected override async Task OnToolsExecutedAsync(List<ToolCallResult> toolCallResults, AgentRunRequest req, IAgentEventSink sink)
    {
      var changeSet = req.ChangeSetEngine;
      if (changeSet == null) return;
      string changesetId = req.ToolContext?.ChangeSetId;
      if (string.IsNullOrEmpty(changesetId)) return;

      foreach (var tcr in toolCallResults)
      {
        if (tcr.IsFrontend) continue;            // 前端工具无变更项
        if (tcr.ToolMessageIndex < 0) continue;  // 没写入 messages，无法去重改写

        var item = AiDevOrchestrator.TryBuildChangeItem(tcr.ToolName, tcr.Args, tcr.OriginalResult, changesetId);
        if (item == null) continue;  // 只读工具/无 sql/有 error

        try
        {
          bool appended = changeSet.AppendItem(changesetId, item);
          if (appended)
          {
            await sink.OnItem(item);
          }
          else
          {
            // 去重跳过：改写 messages 里该 tool result 的 content，告知 LLM 已跳过
            var skipped = new { skipped = true, reason = "该变更项已存在（CATEGORY+ACTION+TARGET+SQL 与已有项重复），已跳过，请勿重复产出同一字段/SQL" };
            var toolMsg = req.Messages[tcr.ToolMessageIndex] as Dictionary<string, object>;
            if (toolMsg != null) toolMsg["content"] = JsonConvert.SerializeObject(skipped);
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine("[DevAgentEngine] AppendItem 异常: " + ex.Message);
        }
      }
    }

    /// <summary>循环结束：跑 ValidateChangeSet，推 onValidate。</summary>
    protected override async Task OnLoopDoneAsync(AgentRunRequest req, IAgentEventSink sink)
    {
      var changeSet = req.ChangeSetEngine;
      string changesetId = req.ToolContext?.ChangeSetId;
      if (changeSet == null || string.IsNullOrEmpty(changesetId)) return;
      try
      {
        var report = changeSet.ValidateChangeSet(changesetId);
        if (report != null) await sink.OnValidate(report);
        await sink.OnStep("validate", "done", "validate");  // 校验完成 step 信号（AiDev/Wizard 前端步骤条）
      }
      catch (Exception ex)
      {
        Console.WriteLine("[DevAgentEngine] ValidateChangeSet 异常: " + ex.Message);
      }
    }

    protected override async Task OnToolStepStart(string toolName, AgentRunRequest req, IAgentEventSink sink)
    {
      // 工具级 start：推 onStep(stepKey,"start",toolName)，与 AiDev 原循环 onStep("start") 一致。
      // 步骤级 step_start（Wizard 一键生成）由 GenerateAllAsync 外层直接推 sink.OnStepStart，不进 DevAgentEngine。
      string stepKey = req.ToolToStepMapper?.Invoke(toolName);
      if (!string.IsNullOrEmpty(stepKey)) await sink.OnStep(stepKey, "start", toolName);
    }

    protected override async Task OnToolStepEnd(string toolName, ToolResult result, AgentRunRequest req, IAgentEventSink sink)
    {
      string stepKey = req.ToolToStepMapper?.Invoke(toolName);
      if (string.IsNullOrEmpty(stepKey)) return;
      bool isReadOnly = READONLY_TOOLS.Contains(toolName);
      await sink.OnStep(stepKey, isReadOnly ? "skipped" : "done", toolName);
    }
  }
}
