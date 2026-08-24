using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Realso.WebAPI.Services.AiDev;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// Agent 循环选项。统一 4 套循环各异的参数：
  ///   - AssistantHub MAX_STEPS=50 / AiDev MAX_TOOL_ROUNDS=20 / Wizard 每步 10 / SFC MAX_STEPS=15
  ///   - tool result 截断 4000 字符（全部相同）
  ///   - summary 截断 200/200/200/500
  ///   - SFC 有 15s 心跳，其他无
  /// </summary>
  public class AgentOptions
  {
    /// <summary>最大循环步数（默认 50，SFC=15，AiDev=20，Wizard 每步=10）</summary>
    public int MaxSteps = 50;

    /// <summary>喂回 LLM 的 tool result 截断长度（0=不截断，AiDev/Wizard 全量；默认 4000）</summary>
    public int MaxToolResultChars = 4000;

    /// <summary>给前端展示的 summary 截断长度（默认 200，SFC=500）</summary>
    public int SummaryTruncateChars = 200;

    /// <summary>是否启用心跳（SFC=true，其他 false）</summary>
    public bool EnableHeartbeat = false;

    /// <summary>心跳间隔（默认 15s）</summary>
    public TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    /// <summary>达最大步数的错误提示模板</summary>
    public string MaxStepsMessage = "已达最大步数({0})，请缩小任务范围";

    /// <summary>
    /// 从场景 PARAMS JSON 覆盖默认值。PARAMS 格式示例：
    /// {"maxSteps":15, "maxToolResultChars":8000, "summaryTruncateChars":500,
    ///  "enableHeartbeat":true, "heartbeatIntervalMs":15000, "maxStepsMessage":"..."}
    /// 只覆盖 PARAMS 中存在的字段，未出现的保持代码默认值。
    /// </summary>
    public void ApplySceneParams(string paramsJson)
    {
      if (string.IsNullOrEmpty(paramsJson)) return;
      try
      {
        var jo = JObject.Parse(paramsJson);
        if (jo["maxSteps"] != null) MaxSteps = jo["maxSteps"].Value<int>();
        if (jo["maxToolResultChars"] != null) MaxToolResultChars = jo["maxToolResultChars"].Value<int>();
        if (jo["summaryTruncateChars"] != null) SummaryTruncateChars = jo["summaryTruncateChars"].Value<int>();
        if (jo["enableHeartbeat"] != null) EnableHeartbeat = jo["enableHeartbeat"].Value<bool>();
        if (jo["heartbeatIntervalMs"] != null) HeartbeatInterval = TimeSpan.FromMilliseconds(jo["heartbeatIntervalMs"].Value<int>());
        if (jo["maxStepsMessage"] != null) MaxStepsMessage = jo["maxStepsMessage"].Value<string>();
      }
      catch { /* PARAMS 解析失败，保持默认值 */ }
    }
  }

  /// <summary>
  /// 一次 Agent 循环的输入。统一 4 套循环的入参：
  ///   - AssistantHub: messages(从 SessionStore 加载)+tools(MergeWithFrontendTools)+cfg+userId+userName+conversationId
  ///   - AiDev: messages(BuildMessages)+tools(GetDevToolDefinitions)+cfg+sessionId/changesetId
  ///   - SFC: messages(BuildSystemPrompt+BuildUserMessage)+tools(SfcAiToolExecutor)+cfg
  /// </summary>
  public class AgentRunRequest
  {
    /// <summary>LLM 消息列表（含 system + 历史 + 本次 user）</summary>
    public List<object> Messages;

    /// <summary>工具定义（按场景过滤后的子集）</summary>
    public List<object> Tools;

    /// <summary>LLM 配置（文本或视觉）</summary>
    public LlmConfig Cfg;

    public string UserId;
    public string UserName;
    public string ConversationId;
    /// <summary>场景：chat/form/aidev/wizard/sfc/optimize/vision</summary>
    public string OperationType = "chat";

    /// <summary>关联模块编码（form/aidev/wizard 场景传入，写入 usage 记录）</summary>
    public string ModuleCode;

    /// <summary>工具上下文（UserInfo/ChangeSetId/ConnectionId/FrontendHandler）</summary>
    public ToolContext ToolContext;

    /// <summary>开发场景：变更包引擎（AiDev/Wizard 传入，通用助理/SFC 为 null）。
    /// DevAgentEngine 据此调 AppendItem/ValidateChangeSet。Singleton DevAgentEngine 不能构造注入 Scoped ChangeSetEngine，故通过 request 传递。</summary>
    public ChangeSetEngine ChangeSetEngine;

    /// <summary>开发场景：工具名->步骤 key 映射（AiDev 用 MapToolToStep，Wizard 用 STEP_TOOL_MAP 反查）。
    /// DevAgentEngine 据此推 onStep。null 表示该场景无步骤条（通用助理/SFC）。</summary>
    public Func<string, string> ToolToStepMapper;

    public AgentOptions Options = new AgentOptions();
  }

  /// <summary>Agent 循环结果</summary>
  public class AgentRunResult
  {
    public bool GotAnswer;
    public string FinalText;
    public int Steps;
    public (int prompt, int completion) Usage;
  }
}
