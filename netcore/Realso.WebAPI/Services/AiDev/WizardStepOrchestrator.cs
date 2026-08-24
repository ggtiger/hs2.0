using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.WebAPI.Models.AiDev;
using Realso.WebAPI.Services.Agent;
using Realso.WebAPI.Services.AiMemory;
using Realso.WebAPI.Services;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// 模块向导分步生成编排器：把 LLM + 工具 + ChangeSetEngine 串起来，但按向导步骤分步调用。
  ///
  /// 与 AiDevOrchestrator 的区别：
  /// - AiDevOrchestrator 是"整模块一次性生成"（20 轮跑完建表→视图→UI→模块→API→菜单全流程）
  /// - WizardStepOrchestrator 是"按当前向导步骤生成"：每步只允许该步相关工具子集（stepToolMap），
  ///   每步独立 system prompt（含向导 form 上下文），6 步共享同一 sessionId/changesetId。
  ///
  /// 复用 AiDevOrchestrator 的静态辅助方法（LoadOrCreateSession/TryBuildChangeItem/MapToolToStep 等）
  /// 和 Function Calling 循环模式，避免代码重复。
  /// 跨步骤时序裂缝（Step2 define_dataview 读 Step1 未落库的 TBS 资源）由 AssistantToolExecutor
  /// 的 LookupDraftResourceId 兜底修复，6 步共享 changesetId 即可。
  /// </summary>
  public class WizardStepOrchestrator
  {
    private readonly DevAgentEngine _engine;
    private readonly LlmConfigService _cfg;
    private readonly ChangeSetEngine _changeSetEngine;
    private readonly PromptService _prompts;
    private readonly UsageLogger _usage;

    // 每步工具循环最大轮数（每步工具少，2-4 个，典型 2-3 轮完成，10 轮足够防死循环）
    private const int MAX_TOOL_ROUNDS = 100;

    // 步骤标签（中文，与前端 module-wizard.vue 的 steps 数组一致）
    private static readonly string[] STEP_LABELS = { "基本信息", "数据模型", "视图与查询", "接口与页面", "UI配置", "菜单注册" };

    // 每步允许的工具子集（权威定义，前端 stepToolMap 是镜像用于 UI 提示）
    // LLM 只能看到该步工具定义，自然不会调该步之外的工具
    private static readonly Dictionary<int, string[]> STEP_TOOL_MAP = new Dictionary<int, string[]>
    {
      { 0, new[] { "register_module", "search_existing_resource", "search_module_template", "read_module_template" } },
      { 1, new[] { "create_physical_table", "search_existing_resource", "read_table_schema", "configure_resource_field" } },
      { 2, new[] { "define_dataview", "configure_resource_field", "define_filter" } },
      { 3, new[] { "define_api", "define_sql_api", "define_script_api", "define_script_flow_api", "update_script_flow_api", "read_script_flow_api", "define_filter", "define_page", "define_button" } },
      { 4, new[] { "configure_ui_field", "search_dict", "create_dict" } },
      { 5, new[] { "create_menu", "create_funcpoints" } }
    };

    public WizardStepOrchestrator(DevAgentEngine engine, LlmConfigService cfg, ChangeSetEngine changeSetEngine, PromptService prompts = null, UsageLogger usage = null)
    {
      _engine = engine;
      _cfg = cfg;
      _changeSetEngine = changeSetEngine;
      _prompts = prompts;
      _usage = usage;
    }

    /// <summary>
    /// 一键生成全部 6 步：一次 SSE 连接内部循环 step 0->5 连续生成。
    /// 每步产出后自动进下一步，6 步共享 changesetId 保证跨步依赖（Step3 视图能读 Step2 的 DRAFT 表）。
    /// 用户只需描述一次需求，AI 自动按步骤顺序生成全部配置。
    /// </summary>
    /// <param name="userMessage">用户的一次需求描述（如"开发LIMS样品管理模块"）</param>
    /// <param name="onStepStart">每步开始时回调(step)，前端据此推进步骤条</param>
    public async Task<OrchestratorResult> GenerateAllAsync(string sessionId, string wizardContext,
      string userMessage, string userId,
      Func<int, string, Task> onContent = null,          // (step, text)
      Func<int, string, string, Task> onToolCall = null, // (step, toolName, argsJson)
      Func<int, string, string, Task> onToolResult = null,
      Func<ChangeItem, Task> onItem = null,
      Func<ValidationReport, Task> onValidate = null,
      Func<string, Task> onError = null,
      Func<int, Task> onStepStart = null)
    {
      var result = new OrchestratorResult();
      var allItems = new List<ChangeItem>();
      string changesetId = null;
      string sessionCode = null;

      for (int step = 0; step < STEP_LABELS.Length; step++)
      {
        if (onStepStart != null) await onStepStart(step);
        // 每步的 userMessage：第一步用用户原始需求，后续步骤基于需求继续生成该步
        string stepMsg = step == 0 ? userMessage : "基于上述需求，继续生成「" + STEP_LABELS[step] + "」步骤的配置（第" + (step + 1) + "步）";
        Console.WriteLine("[WizardAll] step=" + step + " start");

        // 中间步骤不推送校验报告（依赖未全，校验无意义），最后一步才推送
        bool isLastStep = step == STEP_LABELS.Length - 1;
        var stepResult = await GenerateStepAsync(sessionId, step, wizardContext, stepMsg, userId,
          onContent: async c => { if (onContent != null) await onContent(step, c); },
          onToolCall: async (tn, aj) => { if (onToolCall != null) await onToolCall(step, tn, aj); },
          onToolResult: async (tn, rs) => { if (onToolResult != null) await onToolResult(step, tn, rs); },
          onItem: async it => { allItems.Add(it); if (onItem != null) await onItem(it); },
          onValidate: isLastStep ? (Func<ValidationReport, Task>)(async rep => { if (onValidate != null) await onValidate(rep); }) : null,
          onError: async err => { if (onError != null) await onError("第" + (step + 1) + "步: " + err); },
          enforcePreviousExecuted: false);

        if (!string.IsNullOrEmpty(stepResult.Error))
        {
          // session 级错误（会话不存在/LLM 未配置）直接中断
          result.Error = stepResult.Error;
          return result;
        }
        if (stepResult.NewItems != null) changesetId = stepResult.ChangeSetId;
        sessionCode = stepResult.SessionCode;
        Console.WriteLine("[WizardAll] step=" + step + " done, items=" + (stepResult.NewItems != null ? stepResult.NewItems.Count : 0));
      }

      result.SessionId = sessionId;
      result.SessionCode = sessionCode;
      result.ChangeSetId = changesetId;
      result.NewItems = allItems;
      return result;
    }

    /// <summary>
    /// 触发一次向导分步生成。6 步共享同一 sessionId/changesetId。
    /// 回调签名与 AiDevOrchestrator.GenerateAsync 一致，前端 SSE 处理可复用。
    /// </summary>
    /// <param name="sessionId">向导会话 ID（6 步共享）</param>
    /// <param name="step">0-5 步骤号</param>
    /// <param name="wizardContext">向导 form 上下文 JSON（moduleCode/tableName/fields 等）</param>
    /// <param name="userMessage">用户本轮消息</param>
    public async Task<OrchestratorResult> GenerateStepAsync(string sessionId, int step, string wizardContext,
      string userMessage, string userId,
      Func<string, Task> onContent = null,
      Func<string, string, Task> onToolCall = null,
      Func<string, string, Task> onToolResult = null,
      Func<ChangeItem, Task> onItem = null,
      Func<ValidationReport, Task> onValidate = null,
      Func<string, Task> onError = null,
      bool enforcePreviousExecuted = true)
    {
      var result = new OrchestratorResult();

      // 1. 加载会话 + changeset（复用 AiDevOrchestrator 的静态方法）
      var sessionInfo = AiDevOrchestrator.LoadOrCreateSession(sessionId, userMessage, userId);
      if (sessionInfo == null)
      {
        result.Error = "向导会话不存在: " + sessionId;
        if (onError != null) await onError(result.Error);
        return result;
      }
      string changesetId = sessionInfo.Value.changesetId;
      string sessionCode = sessionInfo.Value.sessionCode;

      // 1.5 步骤强制：第 N 步(N>0)开始前，changeset 里不允许有未执行的变更项(DRAFT/CONFIRMED)。
      // 上一步的 SQL 没确认执行，本步工具(DB 查询)就找不到上一步的资源/字段/页面 → 生成质量塌方。
      // 一键生成模式(GenerateAllAsync)传 enforcePreviousExecuted=false 跳过（它连续跑 6 步最后统一确认执行）。
      if (step > 0 && enforcePreviousExecuted)
      {
        DBHelper helper0 = DB.GetDBHelper();
        using (helper0)
        {
          int pending = helper0.QueryFirstOrDefault<int>(
            @"SELECT COUNT(*) FROM tss_aidev_changeitem
              WHERE CHANGESETID=@csid AND ISDELETED=0 AND ITEMSTATUS IN ('DRAFT','CONFIRMED')",
            new { csid = changesetId });
          if (pending > 0)
          {
            result.Error = "上一步还有 " + pending + " 条变更项未执行。请先在变更清单中「确认并执行」上一步的产出，再进入本步——否则本步找不到上一步创建的资源（表/字段/视图/接口都还没落库）。";
            if (onError != null) await onError(result.Error);
            return result;
          }
        }
      }

      // 2. 取 LLM 配置（场景级模型路由）
      var sceneCfg = SceneConfigService.GetScene("wizard");
      var llmCfg = _cfg.GetByScene(sceneCfg);
      if (llmCfg == null)
      {
        result.Error = "未配置 LLM，请先在管理后台配置 DeepSeek API Key";
        if (onError != null) await onError(result.Error);
        return result;
      }

      // 3. 校验步骤号 + 取该步工具子集
      if (step < 0 || step >= STEP_LABELS.Length)
      {
        result.Error = "无效的向导步骤号: " + step;
        if (onError != null) await onError(result.Error);
        return result;
      }
      var tools = GetStepTools(step);
      if (tools.Count == 0)
      {
        result.Error = "第 " + (step + 1) + " 步没有可用的生成工具";
        if (onError != null) await onError(result.Error);
        return result;
      }

      // 4. 构造 messages（按步骤 system prompt + 历史对话 + 已产出变更项 + 当前用户消息）
      var messages = BuildStepMessages(sessionId, step, wizardContext, userMessage, changesetId);

      // 5. Function Calling 循环（复用 AiDevOrchestrator 的模式，工具数组已按步过滤）
      var executor = new AssistantToolExecutor(AiDevOrchestrator.BuildUserInfo(userId));
      var userInfo = AiDevOrchestrator.BuildUserInfo(userId);

      // 收集 result 字段（DevAgentEngine 通过 sink 推事件，这里包装回调收集 + 透传给调用方）
      var newItems = new List<ChangeItem>();
      string conversation = "";
      ValidationReport validationReport = null;
      var warnings = new List<string>();

      Func<string, Task> wrapContent = async c => { conversation += c; if (onContent != null) await onContent(c); };
      Func<ChangeItem, Task> wrapItem = async item => { newItems.Add(item); if (onItem != null) await onItem(item); };
      Func<ValidationReport, Task> wrapValidate = async r => { validationReport = r; if (onValidate != null) await onValidate(r); };
      Func<string, Task> wrapError = async msg => { warnings.Add(msg); if (onError != null) await onError(msg); };

      // Wizard 无工具级 onStep（步骤条由 GenerateAllAsync 外层 onStepStart 推进），onStep 传 null、ToolToStepMapper=null
      var sink = new AiDevCallbackSink(
        wrapContent,
        onToolCall,
        onToolResult,
        item => wrapItem((ChangeItem)item),
        r => wrapValidate((ValidationReport)r),
        wrapError,
        null);

      var req = new AgentRunRequest
      {
        Messages = messages,
        Tools = tools,
        Cfg = llmCfg,
        UserId = userId,
        UserName = "",
        ConversationId = sessionId,
        OperationType = "wizard",
        ToolContext = new ToolContext { UserInfo = userInfo, ChangeSetId = changesetId },
        ChangeSetEngine = _changeSetEngine,
        ToolToStepMapper = null,
        Options = new AgentOptions { MaxSteps = MAX_TOOL_ROUNDS, MaxToolResultChars = 4000, SummaryTruncateChars = 200 }
      };
      req.Options.ApplySceneParams(sceneCfg?.PARAMS);

      await _engine.RunLoopAsync(req, sink, executor, _usage != null ? new DbUsageLogger(_usage) : (IUsageRecorder)new NullUsageRecorder());
      if (warnings.Count > 0) result.Warnings = warnings;

      // 6. 校验变更包由 DevAgentEngine.OnLoopDoneAsync 完成（推 onValidate），此处保留分界注释。

      result.SessionId = sessionId;
      result.SessionCode = sessionCode;
      result.ChangeSetId = changesetId;
      result.Conversation = conversation;
      result.NewItems = newItems;
      result.ValidationReport = validationReport;

      // 7. 持久化本轮对话到 CONVERSATION（追加，6 步连续保留上下文）
      try
      {
        DBHelper h2 = DB.GetDBHelper();
        using (h2)
        {
          string oldJson = h2.QueryFirstOrDefault<string>(
            "SELECT CONVERSATION FROM tss_aidev_session WHERE ID=@id", new { id = sessionId }) ?? "";
          var history = string.IsNullOrEmpty(oldJson)
            ? new List<HistoryMessage>()
            : (JsonConvert.DeserializeObject<List<HistoryMessage>>(oldJson) ?? new List<HistoryMessage>());
          string aiReply = conversation;
          string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
          if (!string.IsNullOrEmpty(userMessage))
            history.Add(new HistoryMessage { role = "user", content = userMessage, ts = now });
          if (!string.IsNullOrEmpty(aiReply))
            history.Add(new HistoryMessage { role = "assistant", content = aiReply, ts = now });
          string newJson = JsonConvert.SerializeObject(history);
          h2.Execute("UPDATE tss_aidev_session SET CONVERSATION=@json WHERE ID=@id",
            new { json = newJson, id = sessionId });
        }
      }
      catch { /* 持久化失败不阻塞返回 */ }

      return result;
    }

    /// <summary>
    /// 从 GetDevToolDefinitions() 过滤出该步允许的工具子集。
    /// 工具定义是匿名对象 {type:"function", function:{name,...}}，序列化后按 function.name 过滤。
    /// </summary>
    public static List<object> GetStepTools(int step)
    {
      string[] allowed = null;
      if (!STEP_TOOL_MAP.TryGetValue(step, out allowed) || allowed == null) return new List<object>();
      var allowedSet = new HashSet<string>(allowed);
      var allTools = AssistantToolExecutor.GetDevToolDefinitions();
      var result = new List<object>();
      foreach (var t in allTools)
      {
        try
        {
          var jo = JObject.Parse(JsonConvert.SerializeObject(t));
          string name = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(name) && allowedSet.Contains(name)) result.Add(t);
        }
        catch { /* 解析失败跳过 */ }
      }
      return result;
    }

    /// <summary>
    /// 构造分步 messages：该步 system prompt + 历史对话 + 已产出变更项摘要 + 当前用户消息。
    /// 已产出变更项摘要让 LLM 跨步骤感知（如 Step2 知道 Step1 建了哪些字段）。
    /// </summary>
    private List<object> BuildStepMessages(string sessionId, int step, string wizardContext, string userMessage, string changesetId)
    {
      string historyJson = "";
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var session = helper.QueryFirstOrDefault<dynamic>(
          "SELECT CONVERSATION FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0", new { id = sessionId });
        if (session != null) historyJson = (string)session.CONVERSATION ?? "";

        var list = new List<object>
        {
          new { role = "system", content = BuildStepSystemPrompt(step, wizardContext, userMessage) }
        };

        // 历史对话（6 步连续保留，让 LLM 记住之前步骤的产出与用户意图）
        if (!string.IsNullOrEmpty(historyJson))
        {
          try
          {
            var history = JsonConvert.DeserializeObject<List<HistoryMessage>>(historyJson);
            if (history != null)
            {
              foreach (var h in history)
              {
                if (!string.IsNullOrEmpty(h.content))
                  list.Add(new { role = h.role, content = h.content });
              }
            }
          }
          catch { /* 历史解析失败不阻塞 */ }
        }

        // 已产出变更项摘要（跨步骤感知：让 LLM 知道前序步骤产出过什么，避免重复/冲突）
        if (!string.IsNullOrEmpty(changesetId))
        {
          var items = helper.Query<ChangeItem>(
            "SELECT ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET, RATIONALE FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ISDELETED=0 ORDER BY ITEMSEQ",
            new { csid = changesetId }).ToList();
          if (items.Count > 0)
          {
            var sb = new StringBuilder();
            sb.Append("前序步骤已产出的变更项（本步可引用，避免重复产出）：\n");
            foreach (var it in items)
            {
              sb.Append("- #" + it.ITEMSEQ + " " + it.CATEGORY + "/" + it.ACTION + " " + (it.TARGET ?? "") + " via " + (it.TOOL ?? "") + "\n");
            }
            list.Add(new { role = "system", content = sb.ToString() });
          }
        }

        list.Add(new { role = "user", content = userMessage });
        return list;
      }
    }

    /// <summary>
    /// 按向导步骤构造 system prompt。每步聚焦该步任务 + 相关命名规范铁律 + 向导 form 上下文。
    /// userMessage 用于按关键词检索 tss_ai_memory 中的 example/pitfall(2026-07-19 接入记忆中枢)。
    /// stepGoal/commonRules 走 PromptService(wizard_step_N/wizard_common_rules, 配置中心可在线改), 读不到回落 AiDevPrompts 常量。
    /// </summary>
    private string BuildStepSystemPrompt(int step, string wizardContext, string userMessage = "")
    {
      // 解析向导 form 上下文，提取关键字段供 prompt 引用
      string moduleCode = "", moduleName = "", tableName = "", tableComment = "";
      string listFields = "", queryFields = "", editFields = "";
      string flowCode = "", menuName = "", parentFuncId = "", bizCategory = "";
      if (!string.IsNullOrEmpty(wizardContext))
      {
        try
        {
          var ctx = JObject.Parse(wizardContext);
          moduleCode = ctx["moduleCode"]?.ToString() ?? "";
          moduleName = ctx["moduleName"]?.ToString() ?? "";
          tableName = ctx["tableName"]?.ToString() ?? "";
          tableComment = ctx["tableComment"]?.ToString() ?? "";
          listFields = ctx["listFields"]?.ToString() ?? "";
          queryFields = ctx["queryFields"]?.ToString() ?? "";
          editFields = ctx["editFields"]?.ToString() ?? "";
          flowCode = ctx["flowCode"]?.ToString() ?? "";
          menuName = ctx["menuName"]?.ToString() ?? "";
          parentFuncId = ctx["parentFuncId"]?.ToString() ?? "";
          bizCategory = ctx["bizCategory"]?.ToString() ?? "";
        }
        catch { /* 解析失败用空值 */ }
      }

      string ctxSummary = "【当前向导上下文】模块编码=" + (moduleCode + "") + "，模块名称=" + (moduleName + "") +
        (string.IsNullOrEmpty(tableName) ? "" : "，表名=" + tableName) +
        (string.IsNullOrEmpty(tableComment) ? "" : "，表注释=" + tableComment) +
        (string.IsNullOrEmpty(listFields) ? "" : "，列表字段=" + listFields) +
        (string.IsNullOrEmpty(editFields) ? "" : "，编辑字段=" + editFields);

      // commonRules / stepGoal 走 PromptService 配置化(wizard_common_rules / wizard_step_N, 配置中心可在线改),
      // 读不到回落 AiDevPrompts 代码常量(2026-07-20 起硬编码块已迁移到 AiDevPrompts.cs)
      string commonRules = (_prompts != null ? _prompts.Get("wizard_common_rules") : null) ?? AiDevPrompts.WizardCommonRules;
      var stepGoalFallbacks = new[]
      {
        AiDevPrompts.WizardStep0, AiDevPrompts.WizardStep1, AiDevPrompts.WizardStep2,
        AiDevPrompts.WizardStep3, AiDevPrompts.WizardStep4, AiDevPrompts.WizardStep5
      };
      string stepGoal = (_prompts != null ? _prompts.Get("wizard_step_" + step) : null)
        ?? (step >= 0 && step < stepGoalFallbacks.Length ? stepGoalFallbacks[step] : "");

      string stepLabel = step >= 0 && step < STEP_LABELS.Length ? STEP_LABELS[step] : ("第" + (step + 1) + "步");
      // 注入记忆中枢(2026-07-19): 检索 tss_ai_memory 中 rule/example/pitfall,
      // 按 step + assetType=wizard 过滤 + userMessage 关键词召回, 失败/无命中返回空字符串。
      string memoryPrompt = MemoryService.BuildMemoryPrompt("wizard", step, "wizard", userMessage);
      return "你是华溯 LIMS 系统的模块创建向导助手，当前负责「" + stepLabel + "」步骤。\n" +
        ctxSummary + "\n\n" + stepGoal + "\n" + commonRules + memoryPrompt;
    }
  }
}
