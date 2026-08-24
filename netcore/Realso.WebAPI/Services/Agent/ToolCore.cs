using System.Collections;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 工具分类。替代 AssistantHub 里硬编码的 FRONTEND_TOOLS HashSet。
  /// - Backend：后端直接执行（查数据/查 schema/navigate/开发类工具）
  /// - Frontend：前端代理层执行（操作 UI/store/router），后端只转发并等回传
  /// - ReadOnly：纯只读查询工具（SFC 的 get_module_schema 等），无副作用
  /// </summary>
  public enum ToolKind
  {
    Backend,
    Frontend,
    ReadOnly
  }

  /// <summary>
  /// 工具执行上下文。统一传递 UserInfo/ChangeSetId/ConnectionId/前端工具处理器，
  /// 替代各执行器各自定义的散乱参数（AssistantToolExecutor 的 changesetId、SfcAiToolExecutor 的无参）。
  /// </summary>
  public class ToolContext
  {
    /// <summary>当前用户信息（Hashtable，含 ID/NICKNAME/EMPID/DEPTID 等，来自前端 userInfoJson）</summary>
    public Hashtable UserInfo;

    /// <summary>AI 开发/向导场景的变更包 ID（用于 TryBuildChangeItem 时序校验）</summary>
    public string ChangeSetId;

    /// <summary>SignalR 连接 ID（用于前端工具调用的 callId 生成与断连清理）</summary>
    public string ConnectionId;

    /// <summary>前端工具处理器（仅通用助理/表单场景需要，开发/SFC 场景为 null）</summary>
    public IFrontendToolHandler FrontendHandler;
  }

  /// <summary>
  /// 工具执行结果。统一后端工具与前端工具的返回结构。
  /// - Data：原始结果对象（序列化后喂回 LLM，截断到 MaxToolResultChars）
  /// - Summary：给前端展示的摘要（截断到 SummaryTruncateChars）
  /// - IsChangeItem：是否为开发场景的变更项（DevAgentEngine 据此走 ChangeSetEngine.AppendItem）
  /// </summary>
  public class ToolResult
  {
    public bool Success = true;
    public string Error;
    public object Data;
    public string Summary;

    /// <summary>开发场景：是否为变更项（CREATE TABLE/configure_resource_field 等产出类工具返回 true）</summary>
    public bool IsChangeItem;

    /// <summary>开发场景：变更项的 SQL 内容（TryBuildChangeItem 从 Data 里提取）</summary>
    public string Sql;

    /// <summary>开发场景：变更项的元数据 JSON（TryBuildChangeItem 从 Data 里提取）</summary>
    public string Metadata;

    public static ToolResult Ok(object data, string summary = null)
    {
      return new ToolResult { Data = data, Summary = summary };
    }

    public static ToolResult Fail(string error)
    {
      return new ToolResult { Success = false, Error = error, Data = new { error } };
    }
  }

  /// <summary>
  /// 一次工具调用的完整上下文（名称+参数+原始结果+在 messages 中的位置）。
  /// AgentEngine.ExecuteToolCallsAsync 收集后传给 OnToolsExecutedAsync 钩子，
  /// DevAgentEngine 据此调 TryBuildChangeItem（需要原始 result 对象，反射取 sql/metadata）。
  /// ToolMessageIndex 是该工具结果写入 req.Messages 的索引（tool 角色），钩子可据此改写 content（去重改写）。
  /// </summary>
  public class ToolCallResult
  {
    public string ToolCallId;
    public string ToolName;
    public Newtonsoft.Json.Linq.JObject Args;
    /// <summary>原始工具结果对象（ToolResult.Data，未截断未序列化）</summary>
    public object OriginalResult;
    /// <summary>序列化后的 resultJson（未截断，用于 TryBuildChangeItem 取 sql/metadata）</summary>
    public string ResultJson;
    /// <summary>该工具结果在 req.Messages 中的索引（tool 角色消息），钩子可改写其 content</summary>
    public int ToolMessageIndex = -1;
    /// <summary>是否前端工具（前端工具无 OriginalResult，跳过 ChangeItem 逻辑）</summary>
    public bool IsFrontend;
  }
}
