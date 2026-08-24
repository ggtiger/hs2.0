namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 一次 LLM 调用的用量记录。替代 UsageLogger.Log 的散乱参数，
  /// 新增 OperationType 区分场景（chat/form/aidev/wizard/sfc/optimize/vision）。
  /// 当前 UsageLogger.Log 硬编码 OPERATIONTYPE='chat'，迁移后按场景传值。
  /// </summary>
  public class UsageRecord
  {
    public string UserId;
    public string UserName;
    public string ConversationId;
    /// <summary>场景：chat/form/aidev/wizard/sfc/optimize/vision</summary>
    public string OperationType = "chat";
    /// <summary>关联模块编码（form=aidev/wizard 场景的模块）</summary>
    public string ModuleCode;
    /// <summary>工具名（AgentEngine 每轮调用的工具名，可用于统计哪些工具最常用）</summary>
    public string ToolName;
    public int PromptTokens;
    public int CompletionTokens;
    public decimal PriceInput;
    public decimal PriceOutput;
    public int DurationMs;
    public bool Success;
    public string ErrorMsg;
  }

  /// <summary>
  /// 用量记录器抽象。统一 4 套循环各异的 usage 处理：
  ///   - DbUsageLogger：每轮 LLM 调用写一条 TBS_LLM_USAGE（通用助理/表单/优化/视觉）
  ///   - AggregateUsageReporter：累计 prompt/completion tokens，OnDone 时写一条汇总（SFC 场景，替代 RMSfcAiController 手动累加）
  ///   - NullUsageRecorder：不记录（AiDev/Wizard 迁移期保持，后续可改 Db）
  /// </summary>
  public interface IUsageRecorder
  {
    /// <summary>记录一次 LLM 调用（AgentEngine 每轮调一次）</summary>
    void Record(UsageRecord record);

    /// <summary>获取累计用量（AggregateUsageReporter 用，Db/Null 返回 0）</summary>
    (int promptTokens, int completionTokens) GetTotal();
  }
}
