using System.Threading.Tasks;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// IAgentEventSink 的回调适配实现（阶段 2b 过渡用）。
  /// 把 AiDevOrchestrator.GenerateAsync / WizardStepOrchestrator.GenerateStepAsync 的
  /// 7 个 Func 回调（onContent/onToolCall/onToolResult/onItem/onValidate/onError/onStep）
  /// 适配为 IAgentEventSink，让迁移期 RMAIDevController 的 SSE 端点仍能工作。
  ///
  /// 阶段 2b 完成 SSE→Hub 迁移后，AiDev/Sfc/Wizard 全部用 SignalREventSink，本类阶段 4 删除。
  /// </summary>
  public class AiDevCallbackSink : IAgentEventSink
  {
    private readonly System.Func<string, Task> _onContent;
    private readonly System.Func<string, string, Task> _onToolCall;
    private readonly System.Func<string, string, Task> _onToolResult;
    private readonly System.Func<object, Task> _onItem;
    private readonly System.Func<object, Task> _onValidate;
    private readonly System.Func<string, Task> _onError;
    private readonly System.Func<string, string, string, Task> _onStep;
    private readonly System.Func<string, Task> _onStepStart;

    public AiDevCallbackSink(
      System.Func<string, Task> onContent = null,
      System.Func<string, string, Task> onToolCall = null,
      System.Func<string, string, Task> onToolResult = null,
      System.Func<object, Task> onItem = null,
      System.Func<object, Task> onValidate = null,
      System.Func<string, Task> onError = null,
      System.Func<string, string, string, Task> onStep = null,
      System.Func<string, Task> onStepStart = null)
    {
      _onContent = onContent;
      _onToolCall = onToolCall;
      _onToolResult = onToolResult;
      _onItem = onItem;
      _onValidate = onValidate;
      _onError = onError;
      _onStep = onStep;
      _onStepStart = onStepStart;
    }

    public Task OnContent(string delta) => _onContent?.Invoke(delta) ?? Task.CompletedTask;
    public Task OnToolCall(string toolName, string argsJson) => _onToolCall?.Invoke(toolName, argsJson) ?? Task.CompletedTask;
    public Task OnToolResult(string toolName, string summary) => _onToolResult?.Invoke(toolName, summary) ?? Task.CompletedTask;
    public Task OnNavigate(string path, object query, string moduleCode, string moduleName) => Task.CompletedTask;
    public Task OnFill(object fields) => Task.CompletedTask;
    public Task OnSubTable(string path, object rows) => Task.CompletedTask;
    public Task OnItem(object changeItem) => _onItem?.Invoke(changeItem) ?? Task.CompletedTask;
    public Task OnValidate(object validationReport) => _onValidate?.Invoke(validationReport) ?? Task.CompletedTask;
    public Task OnStep(string stepKey, string status, string toolName) => _onStep?.Invoke(stepKey, status, toolName) ?? Task.CompletedTask;
    public Task OnStepStart(string stepKey) => _onStepStart?.Invoke(stepKey) ?? Task.CompletedTask;
    public Task OnError(string message) => _onError?.Invoke(message) ?? Task.CompletedTask;
    public Task OnDone(object usage = null) => Task.CompletedTask;
    public Task OnHeartbeat() => Task.CompletedTask;
  }
}
