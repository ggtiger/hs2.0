using System.Threading.Tasks;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// Agent 循环事件出口抽象。把 AssistantHub.RunAgentLoop 里的 Clients.Caller.SendAsync、
  /// AiDevOrchestrator 的 7 个 Func 回调、RMSfcAiController 的 SSE Write 统一成一套回调。
  /// 实现类：
  ///   - SignalREventSink：封装 Clients.Caller.SendAsync(eventName, block)，eventName 参数化(block/formblock/devblock/wizardblock/sfcblock)
  ///   - AiDevCallbackSink：适配 AiDevOrchestrator 的 7 个 Func 回调（阶段 2b 过渡用，阶段 4 退役）
  /// 消息块结构与现有保持一致：{type, text?, tool?, args?, summary?, ...}
  /// </summary>
  public interface IAgentEventSink
  {
    /// <summary>流式文本片段（打字效果，逐 delta 推送）</summary>
    Task OnContent(string delta);

    /// <summary>工具调用前（展示工具名+参数给用户）</summary>
    Task OnToolCall(string toolName, string argsJson);

    /// <summary>工具执行完摘要（截断到 SummaryTruncateChars）</summary>
    Task OnToolResult(string toolName, string summary);

    /// <summary>导航跳转（通用助理 navigate 工具）</summary>
    Task OnNavigate(string path, object query, string moduleCode, string moduleName);

    /// <summary>填充表单字段（表单场景 fill_form/fill_subtable 工具结果）</summary>
    Task OnFill(object fields);

    /// <summary>填充子表（表单场景）</summary>
    Task OnSubTable(string path, object rows);

    /// <summary>开发场景：变更项产出（DevAgentEngine 钩子调用）</summary>
    Task OnItem(object changeItem);

    /// <summary>开发场景：变更包校验报告</summary>
    Task OnValidate(object validationReport);

    /// <summary>开发场景：流程步骤状态变更（start/done/skipped）</summary>
    Task OnStep(string stepKey, string status, string toolName);

    /// <summary>开发场景：向导步骤推进（step_start）</summary>
    Task OnStepStart(string stepKey);

    /// <summary>错误</summary>
    Task OnError(string message);

    /// <summary>完成（含可选的累计 usage 汇总）</summary>
    Task OnDone(object usage = null);

    /// <summary>心跳（SFC 场景 15s 间隔，其他场景不调）</summary>
    Task OnHeartbeat();
  }
}
