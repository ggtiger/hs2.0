using Realso.WebAPI.Services;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// IUsageRecorder 的数据库实现。每轮 LLM 调用写一条 TBS_LLM_USAGE。
  /// 封装现有 UsageLogger.Log，新增 operationType 参数（现有硬编码 'chat'）。
  /// 用于通用助理/表单填报/提示词优化/图片识别场景。
  /// </summary>
  public class DbUsageLogger : IUsageRecorder
  {
    private readonly UsageLogger _inner;
    public DbUsageLogger(UsageLogger inner) { _inner = inner; }

    public void Record(UsageRecord r)
    {
      _inner.Log(r.UserId, r.UserName, r.ConversationId,
        r.PromptTokens, r.CompletionTokens, r.PriceInput, r.PriceOutput,
        r.DurationMs, r.Success, r.ErrorMsg, r.OperationType,
        r.ModuleCode, r.ToolName);
    }

    public (int, int) GetTotal() => (0, 0);
  }

  /// <summary>
  /// 累计用量实现。AgentEngine 每轮累加 prompt/completion tokens，
  /// OnDone 时调用 Record 写一条汇总。替代 RMSfcAiController 手动累加 + done 事件返回。
  /// 用于 SFC 代码生成场景。
  /// </summary>
  public class AggregateUsageReporter : IUsageRecorder
  {
    private readonly UsageLogger _inner;
    private int _prompt;
    private int _completion;
    private UsageRecord _template;

    public AggregateUsageReporter(UsageLogger inner) { _inner = inner; }

    public void Record(UsageRecord r)
    {
      _template = r;  // 保留第一条作为模板（UserId/UserName/ConvId/OperationType/Price）
      _prompt += r.PromptTokens;
      _completion += r.CompletionTokens;
    }

    public (int, int) GetTotal() => (_prompt, _completion);

    /// <summary>循环结束时调，写一条汇总记录</summary>
    public void FlushAndLog()
    {
      if (_template == null) return;
      _template.PromptTokens = _prompt;
      _template.CompletionTokens = _completion;
      _inner.Log(_template.UserId, _template.UserName, _template.ConversationId,
        _prompt, _completion, _template.PriceInput, _template.PriceOutput,
        0, true, null, _template.OperationType,
        _template.ModuleCode, _template.ToolName);
    }
  }

  /// <summary>
  /// 空实现。AiDev/Wizard 场景当前不记 usage，迁移期保持。
  /// 后续可改为 DbUsageLogger 统一记录开发场景用量。
  /// </summary>
  public class NullUsageRecorder : IUsageRecorder
  {
    public void Record(UsageRecord r) { }
    public (int, int) GetTotal() => (0, 0);
  }
}
