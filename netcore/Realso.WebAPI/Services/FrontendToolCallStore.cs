using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 前端工具调用的共享存储（跨 Hub 和 Controller 访问）。
  /// 后端 ExecuteFrontendTool 推送 frontend_tool_call 后，在此注册 callId→tcs 等待结果。
  /// 前端可通过两种方式回传结果：
  ///   1) SignalR: AssistantHub.FrontendToolResult（备用，受连接稳定性影响）
  ///   2) HTTP: AssistantController.ToolResult（主用，独立请求更可靠，绕过 SignalR 单向半开问题）
  /// </summary>
  public static class FrontendToolCallStore
  {
    public static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> PendingCalls
      = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

    /// <summary>注册一个待处理的前端工具调用</summary>
    public static void Register(string callId, TaskCompletionSource<string> tcs)
    {
      PendingCalls[callId] = tcs;
    }

    /// <summary>回传结果（HTTP/SignalR 都调此方法）。返回是否匹配到 callId。</summary>
    public static bool TrySetResult(string callId, string resultJson)
    {
      if (string.IsNullOrEmpty(callId)) return false;
      if (PendingCalls.TryRemove(callId, out var tcs))
      {
        tcs.TrySetResult(resultJson ?? "{}");
        return true;
      }
      return false;
    }

    /// <summary>移除并取消一个调用（超时清理）</summary>
    public static void Remove(string callId)
    {
      PendingCalls.TryRemove(callId, out _);
    }

    /// <summary>连接断开时，取消该连接的所有 pending 调用（置失败）</summary>
    public static void CancelForConnection(string connectionId)
    {
      if (string.IsNullOrEmpty(connectionId)) return;
      var keysToRemove = new List<string>();
      var prefix = connectionId + ":";
      foreach (var kv in PendingCalls)
      {
        if (kv.Key.StartsWith(prefix))
        {
          kv.Value.TrySetResult("{\"success\":false,\"error\":\"连接已断开\"}");
          keysToRemove.Add(kv.Key);
        }
      }
      foreach (var k in keysToRemove) PendingCalls.TryRemove(k, out _);
    }
  }
}
