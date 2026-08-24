using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// IFrontendToolHandler 默认实现。封装 AssistantHub 里前端工具调用逻辑：
  ///   1) per-connection 的前端工具定义注册（前端连接时 RegisterFrontendTools 注册）
  ///   2) ExecuteFrontendTool：生成 callId → 注册 TaskCompletionSource → 推 frontend_tool_call → 等 30s 回传
  ///   3) HandleResult：HTTP/SignalR 回传结果时调 FrontendToolCallStore.TrySetResult
  ///
  /// SignalR 的 Clients.Caller（IClientProxy）每次调用时由 Hub 传入（Hub 拿不到 Caller 的单例），
  /// 因此 Handler 用一个可设置的 CallerProvider 委托，Hub 在每个 Hub 方法入口设置当前连接的 caller。
  /// </summary>
  public class FrontendToolHandler : IFrontendToolHandler
  {
    // per-connection 的前端工具定义（前端连接时注册）
    private static readonly ConcurrentDictionary<string, List<object>> _frontendToolDefs = new ConcurrentDictionary<string, List<object>>();

    // 当前连接的 Caller 代理 + ConnectionId（Hub 方法入口设置，per-request）。
    // 必须用 AsyncLocal（不能加 [ThreadStatic]）：RunLoopAsync 内多次 await 会切换线程，
    // AsyncLocal.Value 跨 await 流动；[ThreadStatic] 会让每线程各有一个 AsyncLocal 实例，
    // await 切换线程后 Current 丢失 -> Caller null -> SignalR 静默不推 -> 前端"输出一半断了"。
    private static readonly AsyncLocal<AgentHubCallerContext> _current = new AsyncLocal<AgentHubCallerContext>();

    /// <summary>当前连接上下文（ConnectionId + IClientProxy）</summary>
    public static AgentHubCallerContext Current
    {
      get => _current.Value;
      set => _current.Value = value;
    }

    /// <summary>当前连接注册的前端工具定义</summary>
    public List<object> GetRegisteredDefinitions()
    {
      var ctx = Current;
      if (ctx == null || ctx.ConnectionId == null) return new List<object>();
      return _frontendToolDefs.TryGetValue(ctx.ConnectionId, out var defs) ? defs : new List<object>();
    }

    /// <summary>前端连接时注册工具定义（Hub.RegisterFrontendTools 调用）</summary>
    public static void RegisterDefinitions(string connectionId, List<object> defs)
    {
      if (defs != null && defs.Count > 0 && !string.IsNullOrEmpty(connectionId))
        _frontendToolDefs[connectionId] = defs;
    }

    /// <summary>连接断开时清理（Hub.OnDisconnectedAsync 调用）</summary>
    public static void ClearForConnection(string connectionId)
    {
      _frontendToolDefs.TryRemove(connectionId, out _);
      FrontendToolCallStore.CancelForConnection(connectionId);
    }

    /// <summary>
    /// 执行前端工具：推送 frontend_tool_call 给前端代理层，等待 FrontendToolResult 回传。
    /// 超时保护：30 秒未回传则返回失败。
    /// </summary>
    public async Task<string> ExecuteFrontendTool(string toolName, JObject args)
    {
      var ctx = Current;
      if (ctx == null || ctx.Caller == null)
        return "{\"success\":false,\"error\":\"无 SignalR 连接上下文，无法执行前端工具\"}";

      string callId = ctx.ConnectionId + ":" + Guid.NewGuid().ToString("N");
      var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
      FrontendToolCallStore.Register(callId, tcs);

      string argsJson = args.ToString();
      Console.WriteLine($"[FrontendTool] 推送 callId={callId} tool={toolName} conn={ctx.ConnectionId}");
      await ctx.Caller.SendAsync("frontend_tool_call", callId, toolName, argsJson);

      var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(30000));
      FrontendToolCallStore.Remove(callId);

      if (completedTask == tcs.Task)
      {
        Console.WriteLine($"[FrontendTool] 收到回传 callId={callId} tool={toolName}");
        return tcs.Task.Result;
      }
      Console.WriteLine($"[FrontendTool] 超时 callId={callId} tool={toolName}（前端未回传或callId不匹配）");
      return "{\"success\":false,\"error\":\"前端工具执行超时(" + toolName + ")\"}";
    }

    /// <summary>前端回传结果（Hub.FrontendToolResult / Controller.ToolResult 都调此方法）</summary>
    public void HandleResult(string callId, string resultJson)
    {
      FrontendToolCallStore.TrySetResult(callId, resultJson);
    }
  }

  /// <summary>
  /// Hub 方法入口设置的当前连接上下文（ConnectionId + Clients.Caller）。
  /// 命名为 AgentHubCallerContext 避免与 SignalR 自带的 HubCallerContext 冲突。
  /// 通过 using scope 保证请求结束自动清理。
  /// </summary>
  public class AgentHubCallerContext
  {
    public string ConnectionId;
    public IClientProxy Caller;
  }
}
