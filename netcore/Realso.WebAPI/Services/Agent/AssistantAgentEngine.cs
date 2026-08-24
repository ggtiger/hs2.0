using System.Threading.Tasks;
using Realso.WebAPI.Services;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 通用助理/表单填报场景的 AgentEngine 扩展。
  /// override OnToolResultAsync：识别 AssistantToolExecutor 的特殊结果类型
  /// （NavigateResult/FillResult/SubTableResult），走对应 sink 回调（navigate/fill/subtable 块）。
  /// 开发/SFC 场景不产这些结果，用基类 AgentEngine 即可。
  /// </summary>
  public class AssistantAgentEngine : AgentEngine
  {
    public AssistantAgentEngine(LlmClient llm) : base(llm) { }

    protected override Task OnToolResultAsync(string toolName, ToolResult result, IAgentEventSink sink)
    {
      var data = result?.Data;
      if (data == null) return Task.CompletedTask;

      // navigate：推跳转块（后端查路由表，前端执行跳转）
      if (data is AssistantToolExecutor.NavigateResult nr && nr.navigated)
      {
        return sink.OnNavigate(nr.path, nr.id != null ? new { id = nr.id } : null, nr.moduleCode, nr.moduleName);
      }
      // fill_form/fill_subtable 走前端代理层时不进这里；后端 FillResult（保留兜底）
      if (data is AssistantToolExecutor.FillResult fr)
      {
        return sink.OnFill(fr.fields);
      }
      if (data is AssistantToolExecutor.SubTableResult sr)
      {
        return sink.OnSubTable(sr.path, sr.rows);
      }
      return Task.CompletedTask;
    }
  }
}
