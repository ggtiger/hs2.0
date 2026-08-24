using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// IAgentEventSink 的 SignalR 实现。封装 Clients.Caller.SendAsync(eventName, block)。
  /// eventName 参数化：block(通用助理)/formblock(表单)/devblock(AI开发)/wizardblock(向导)/sfcblock(SFC)。
  /// Caller 由 FrontendToolHandler.Current 提供（Hub 方法入口设置），保证每请求取当前连接。
  ///
  /// 收拢 AssistantHub.RunAgentLoop 里所有 Clients.Caller.SendAsync 调用，
  /// 消息块结构与现有完全一致（type=text/tool_call/tool_result/navigate/fill/subtable/error/done）。
  /// </summary>
  public class SignalREventSink : IAgentEventSink
  {
    private readonly string _eventName;

    public SignalREventSink(string eventName)
    {
      _eventName = eventName;
    }

    /// <summary>当前连接的 Caller（由 FrontendToolHandler.Current 透传）</summary>
    private IClientProxy Caller => FrontendToolHandler.Current?.Caller;

    private Task Send(object block)
    {
      var caller = Caller;
      if (caller == null) return Task.CompletedTask;
      return caller.SendAsync(_eventName, block);
    }

    public Task OnContent(string delta)
      => Send(new { type = "text", text = delta });

    public Task OnToolCall(string toolName, string argsJson)
      => Send(new { type = "tool_call", tool = toolName, args = argsJson });

    public Task OnToolResult(string toolName, string summary)
      => Send(new { type = "tool_result", tool = toolName, summary });

    public Task OnNavigate(string path, object query, string moduleCode, string moduleName)
      => Send(new { type = "navigate", path, query, moduleCode, moduleName });

    public Task OnFill(object fields)
      => Send(new { type = "fill", fields });

    public Task OnSubTable(string path, object rows)
      => Send(new { type = "subtable", path, rows });

    public Task OnItem(object changeItem)
      => Send(new { type = "item", item = changeItem });

    public Task OnValidate(object validationReport)
      => Send(new { type = "validate", report = validationReport });

    public Task OnStep(string stepKey, string status, string toolName)
      => Send(new { type = "step", step = stepKey, status, tool = toolName });

    public Task OnStepStart(string stepKey)
      => Send(new { type = "step_start", step = stepKey });

    public Task OnError(string message)
      => Send(new { type = "error", text = message });

    public Task OnDone(object usage = null)
      => Send(usage == null ? (object)new { type = "done" } : new { type = "done", usage });

    public Task OnHeartbeat()
      => Send(new { type = "heartbeat" });
  }
}
