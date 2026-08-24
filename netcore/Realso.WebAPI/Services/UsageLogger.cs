using System;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// LLM 调用用量记录 + 费用计算。每次 DeepSeek 调用后记录 token 消耗与估算费用。
  /// </summary>
  public class UsageLogger
  {
    /// <summary>费用 = 输入token/1000 × 输入单价 + 输出token/1000 × 输出单价</summary>
    public static decimal ComputeCost(int prompt, int completion, decimal priceIn, decimal priceOut)
    {
      return (prompt / 1000m) * priceIn + (completion / 1000m) * priceOut;
    }

    /// <summary>记录一次 LLM 调用到 TBS_LLM_USAGE（兼容旧调用，OPERATIONTYPE 默认 'chat'）</summary>
    public void Log(string userId, string userName, string conversationId,
      int promptTokens, int completionTokens, decimal priceIn, decimal priceOut,
      int durationMs, bool success, string errorMsg)
    {
      Log(userId, userName, conversationId, promptTokens, completionTokens,
        priceIn, priceOut, durationMs, success, errorMsg, "chat");
    }

    /// <summary>记录一次 LLM 调用到 TBS_LLM_USAGE，带场景标识。
    /// operationType: chat/form/aidev/wizard/sfc/optimize/vision（替代硬编码 'chat'）。
    /// DbUsageRecorder 调此重载。
    /// </summary>
    public void Log(string userId, string userName, string conversationId,
      int promptTokens, int completionTokens, decimal priceIn, decimal priceOut,
      int durationMs, bool success, string errorMsg, string operationType,
      string moduleCode = null, string toolName = null)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var id = Guid.NewGuid().ToString("N");
        int total = promptTokens + completionTokens;
        decimal cost = ComputeCost(promptTokens, completionTokens, priceIn, priceOut);
        helper.Execute(
          @"INSERT INTO TBS_LLM_USAGE (ID,USERID,USERNAME,CONVERSATIONID,OPERATIONTYPE,MODULECODE,TOOLNAME,
             PROMPTTOKENS,COMPLETIONTOKENS,TOTALTOKENS,COST,DURATIONMS,ISSUCCESS,ERRORMSG,REQUESTTIME,ISDELETED)
             VALUES (@ID,@UID,@UN,@CID,@OPT,@MC,@TN,@PT,@CT,@TT,@COST,@DUR,@OK,@ERR,NOW(),0)",
          new
          {
            ID = id,
            UID = userId,
            UN = userName,
            CID = conversationId,
            OPT = string.IsNullOrEmpty(operationType) ? "chat" : operationType,
            MC = moduleCode,
            TN = toolName,
            PT = promptTokens,
            CT = completionTokens,
            TT = total,
            COST = cost,
            DUR = durationMs,
            OK = success ? 1 : 0,
            ERR = errorMsg
          });
      }
    }
  }
}
