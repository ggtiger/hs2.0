using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 前端工具处理器抽象。封装 AssistantHub.ExecuteFrontendTool + FrontendToolCallStore 逻辑：
  /// 推送 frontend_tool_call 给前端代理层 → 等待 HTTP/SignalR 回传 → 超时保护。
  /// 只有通用助理/表单场景需要（开发/SFC 无前端工具）。AgentEngine 通过 ToolContext.FrontendHandler 调用。
  /// </summary>
  public interface IFrontendToolHandler
  {
    /// <summary>当前连接注册的前端工具定义（前端连接时 RegisterFrontendTools 注册）</summary>
    List<object> GetRegisteredDefinitions();

    /// <summary>执行前端工具：推 frontend_tool_call，等回传，30s 超时</summary>
    Task<string> ExecuteFrontendTool(string toolName, JObject args);

    /// <summary>前端回传结果（SignalR FrontendToolResult 调用）</summary>
    void HandleResult(string callId, string resultJson);
  }
}
