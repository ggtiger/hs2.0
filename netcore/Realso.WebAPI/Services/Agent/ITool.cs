using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 单个工具抽象。每个工具自声明 Kind（Backend/Frontend/ReadOnly），
  /// 替代 AssistantHub 里硬编码的 FRONTEND_TOOLS HashSet 判断。
  /// 现阶段 AssistantToolExecutor/SfcAiToolExecutor 内部仍是 switch 分发，
  /// 阶段 4 逐步把每个工具拆成独立 ITool 实现类。
  /// </summary>
  public interface ITool
  {
    /// <summary>工具名（function calling 的 function.name）</summary>
    string Name { get; }

    /// <summary>工具分类</summary>
    ToolKind Kind { get; }

    /// <summary>OpenAI function calling 格式的工具定义 {type:"function", function:{name,description,parameters}}</summary>
    object Definition { get; }

    /// <summary>执行工具</summary>
    Task<ToolResult> Execute(JObject args, ToolContext ctx);
  }

  /// <summary>
  /// 工具执行器抽象（批量分发）。现阶段由 AssistantToolExecutor/SfcAiToolExecutor 实现，
  /// 内部 switch 分发，后续可逐步拆成 ITool 列表。
  /// 统一签名 Execute(toolName, args, ctx) 替代：
  ///   - AssistantToolExecutor.Execute(toolName, args, changesetId=null)
  ///   - SfcAiToolExecutor.Execute(toolName, args)
  /// </summary>
  public interface IToolExecutor
  {
    /// <summary>该执行器负责的工具名集合（所有 set 合并）</summary>
    IEnumerable<string> GetToolNames();

    /// <summary>
    /// 按工具集名取定义。executor 自己决定如何按 set 过滤：
    ///   - AssistantToolExecutor: "assistant"(通用7个)/"formfill"(填报5个)/"dev"(开发16个)
    ///   - SfcAiToolExecutor: "sfc"(4个只读)
    /// setName=null 时返回全部。
    /// ToolKind filter 进一步按 Kind 过滤（现阶段工具定义未标 Kind，filter=null 返回全部）。
    /// </summary>
    List<object> GetDefinitionsBySet(string setName, ToolKind? filter = null);

    /// <summary>默认实现：返回全部定义（不按 set 过滤）</summary>
    List<object> GetDefinitions(ToolKind? filter = null);

    /// <summary>判断工具是否为前端工具（由 ToolKind==Frontend 决定，替代 FRONTEND_TOOLS HashSet）</summary>
    bool IsFrontendTool(string toolName);

    /// <summary>执行工具</summary>
    Task<ToolResult> Execute(string toolName, JObject args, ToolContext ctx);
  }

  /// <summary>
  /// 工具注册中心（单例）。按场景聚合多个 IToolExecutor，
  /// 替代 AssistantHub 里 MergeWithFrontendTools + 三个静态 GetXxxToolDefinitions 方法。
  /// 调用方按 scene 取定义：assistant 取 Backend+Frontend，form 取 FormFill+Frontend，
  /// aidev/wizard 取 Dev，sfc 取 ReadOnly。
  /// </summary>
  public interface IToolRegistry
  {
    /// <summary>注册一个执行器，命名一个工具集（如 "assistant"/"formfill"/"dev"/"sfc"）</summary>
    void Register(string setName, IToolExecutor executor);

    /// <summary>取某工具集的定义（按 Kind 过滤）</summary>
    List<object> GetDefinitions(string setName, ToolKind? filter = null);

    /// <summary>合并多个工具集的定义（按工具名去重，后者不覆盖前者）</summary>
    List<object> GetMergedDefinitions(string[] setNames, ToolKind? filter = null);

    /// <summary>判断工具是否为前端工具（遍历所有已注册执行器）</summary>
    bool IsFrontendTool(string toolName);

    /// <summary>取某工具的执行器（AgentEngine 执行时按工具名找执行器）</summary>
    IToolExecutor GetExecutor(string toolName);
  }
}
