using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.WebAPI.Models.AiDev;
using Realso.WebAPI.Services.Agent;
using Realso.WebAPI.Services;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// AI 开发助理编排器：把 LLM + 工具 + ChangeSetEngine 串起来。
  ///
  /// 核心流程（Function Calling 循环）：
  /// 1. 加载会话 + changeset（没有则创建）
  /// 2. 构造 system prompt（说明这是开发助理，可用工具产出变更项，必须先 search_existing_resource 检查复用）
  /// 3. 构造 messages（system + 历史对话 + 当前用户消息）
  /// 4. 调 DeepSeekClient.StreamChatAsync，传 tools=开发工具 schema
  /// 5. Function Calling 循环：LLM 返回 tool_call → 执行 AssistantToolExecutor.Execute →
  ///    把结果作为 tool 角色消息喂回 → 继续调 LLM → 直到 LLM 不再返回 tool_call
  /// 6. 每次工具调用产出的 {sql, metadata} 转成 ChangeItem，调 ChangeSetEngine.AppendItem 写入(DRAFT 状态)
  /// 7. 跑 ChangeSetEngine.ValidateChangeSet
  /// 8. 返回 {conversation, newItems, validationReport}
  /// </summary>
  public class AiDevOrchestrator
  {
    private readonly DevAgentEngine _engine;
    private readonly LlmConfigService _cfg;
    private readonly ChangeSetEngine _changeSetEngine;
    private readonly PromptService _prompts;
    private readonly UsageLogger _usage;

    // 工具循环最大轮数（防死循环）。典型业务 8-10 个工具调用，20 轮足够；过大会烧 token
    private const int MAX_TOOL_ROUNDS = 100;

    public AiDevOrchestrator(DevAgentEngine engine, LlmConfigService cfg, ChangeSetEngine changeSetEngine, PromptService prompts = null, UsageLogger usage = null)
    {
      _engine = engine;
      _cfg = cfg;
      _changeSetEngine = changeSetEngine;
      _prompts = prompts;
      _usage = usage;
    }

    /// <summary>
    /// 触发一次开发对话生成。
    /// 返回结构：{conversation, newItems, validationReport, error?}
    /// 各回调用于流式推送事件（可传 null）：
    /// - onContent：LLM 文本片段
    /// - onToolCall：(toolName, argsJson) 工具调用前
    /// - onToolResult：(resultSummary) 工具执行完
    /// - onItem：(item) 变更项产生
    /// - onValidate：(report) 校验完成
    /// - onError：(errorMsg) 异常
    /// - onStep：(stepKey, status, toolName) 流程步骤状态变更(start/done/skipped)
    /// </summary>
    public async Task<OrchestratorResult> GenerateAsync(string sessionId, string userMessage, string userId,
      Func<string, Task> onContent = null,
      Func<string, string, Task> onToolCall = null,
      Func<string, string, Task> onToolResult = null,
      Func<ChangeItem, Task> onItem = null,
      Func<ValidationReport, Task> onValidate = null,
      Func<string, Task> onError = null,
      Func<string, string, string, Task> onStep = null)
    {
      var result = new OrchestratorResult();

      // 1. 加载/创建会话 + changeset
      var sessionInfo = LoadOrCreateSession(sessionId, userMessage, userId);
      if (sessionInfo == null)
      {
        result.Error = "会话不存在: " + sessionId;
        if (onError != null) await onError(result.Error);
        return result;
      }
      string changesetId = sessionInfo.Value.changesetId;
      string sessionCode = sessionInfo.Value.sessionCode;

      // 2. 取 LLM 配置（场景级模型路由）
      var sceneCfg = SceneConfigService.GetScene("aidev");
      var llmCfg = _cfg.GetByScene(sceneCfg);
      if (llmCfg == null)
      {
        result.Error = "未配置 LLM，请先在管理后台配置 DeepSeek API Key";
        if (onError != null) await onError(result.Error);
        return result;
      }

      // 3. 构造 LLM 消息（system + 历史对话 + 当前用户消息）
      var messages = BuildMessages(sessionId, userMessage);

      // 4. 取开发工具 schema
      var tools = AssistantToolExecutor.GetDevToolDefinitions();

      // 5. Function Calling 循环
      //    每轮：调 LLM → 若有 tool_call 则执行 → 把结果作为 tool 消息喂回 → 继续调
      //    直到 LLM 不再返回 tool_call 或达到最大轮数
      var executor = new AssistantToolExecutor(BuildUserInfo(userId));
      var userInfo = BuildUserInfo(userId);

      // 收集 result 字段（DevAgentEngine 通过 sink 推事件，这里包装回调收集 + 透传给调用方）
      var newItems = new List<ChangeItem>();
      string conversation = "";
      ValidationReport validationReport = null;
      var warnings = new List<string>();

      Func<string, Task> wrapContent = async c => { conversation += c; if (onContent != null) await onContent(c); };
      Func<ChangeItem, Task> wrapItem = async item => { newItems.Add(item); if (onItem != null) await onItem(item); };
      Func<ValidationReport, Task> wrapValidate = async r => { validationReport = r; if (onValidate != null) await onValidate(r); };
      Func<string, Task> wrapError = async msg => { warnings.Add(msg); if (onError != null) await onError(msg); };

      var sink = new AiDevCallbackSink(
        wrapContent,
        onToolCall,
        onToolResult,
        item => wrapItem((ChangeItem)item),
        r => wrapValidate((ValidationReport)r),
        wrapError,
        onStep);

      var req = new AgentRunRequest
      {
        Messages = messages,
        Tools = tools,
        Cfg = llmCfg,
        UserId = userId,
        UserName = "",
        ConversationId = sessionId,
        OperationType = "aidev",
        ToolContext = new ToolContext { UserInfo = userInfo, ChangeSetId = changesetId },
        ChangeSetEngine = _changeSetEngine,
        ToolToStepMapper = MapToolToStep,
        Options = new AgentOptions { MaxSteps = MAX_TOOL_ROUNDS, MaxToolResultChars = 4000, SummaryTruncateChars = 200 }
      };
      req.Options.ApplySceneParams(sceneCfg?.PARAMS);

      await _engine.RunLoopAsync(req, sink, executor, new DbUsageLogger(_usage));
      if (warnings.Count > 0) result.Warnings = warnings;

      // 8. 校验变更包（由 DevAgentEngine.OnLoopDoneAsync 完成：推 onValidate + onStep validate=done），
      //    达上限 warning 由 wrapError 收集到 result.Warnings。此处保留分界注释供回溯。

      result.SessionId = sessionId;
      result.SessionCode = sessionCode;
      result.ChangeSetId = changesetId;
      result.Conversation = conversation;
      result.NewItems = newItems;
      result.ValidationReport = validationReport;

      // 持久化本轮对话到 CONVERSATION（追加，让重新打开工作区能看到历史）
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
    /// 加载会话，返回 {changesetId, sessionCode}。会话不存在返回 null。
    /// 会话没有 changeset 则自动创建。
    /// </summary>
    internal static (string changesetId, string sessionCode)? LoadOrCreateSession(string sessionId, string userMessage, string userId)
    {
      if (string.IsNullOrEmpty(sessionId)) return null;
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var session = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, SESSIONCODE, SESSIONNAME, CHANGESETID, STATUS FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0",
          new { id = sessionId });
        if (session == null) return null;

        string changesetId = (string)session.CHANGESETID;
        string sessionCode = (string)session.SESSIONCODE;

        // 没有 changeset 则创建
        if (string.IsNullOrEmpty(changesetId))
        {
          changesetId = "cs_" + sessionCode + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
          string title = string.IsNullOrEmpty((string)session.SESSIONNAME) ? ("AI 开发变更包-" + sessionCode) : (string)session.SESSIONNAME;
          helper.Execute(
            @"INSERT INTO tss_aidev_changeset (ID, SESSIONID, CHANGESETCODE, TITLE, SOURCE, INTENT, VALIDATIONPASSED, VALIDATIONREPORT, ITEMCOUNT, CREATEDTIME, ISDELETED)
              VALUES (@id, @sid, @code, @title, 'ai', @intent, 0, NULL, 0, @tm, 0)",
            new
            {
              id = changesetId,
              sid = sessionId,
              code = changesetId,
              title,
              intent = userMessage,
              tm = DateTime.Now
            });
          // 关联回 session
          helper.Execute(
            "UPDATE tss_aidev_session SET CHANGESETID=@csid, STATUS='GENERATING' WHERE ID=@sid",
            new { csid = changesetId, sid = sessionId });
        }
        return (changesetId, sessionCode);
      }
    }

    /// <summary>
    /// 构造 LLM messages：system prompt + 历史对话 + 当前用户消息。
    /// 历史对话暂用 changeset 的 INTENT 简化（M1 不存完整对话历史，后续可扩展）。
    /// 按 SESSIONTYPE(NEW/MODIFY) 切换 system prompt 分支。
    /// </summary>
    private List<object> BuildMessages(string sessionId, string userMessage)
    {
      // 取会话类型 + 目标模块 + changesetId + 历史对话，决定用哪套 prompt
      string sessionType = "NEW";
      string targetModule = "";
      string changesetId = "";
      string historyJson = "";
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var session = helper.QueryFirstOrDefault<dynamic>(
          "SELECT CHANGESETID, SESSIONTYPE, TARGETMODULE, CONVERSATION FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0", new { id = sessionId });
        if (session != null)
        {
          sessionType = string.IsNullOrEmpty((string)session.SESSIONTYPE) ? "NEW" : (string)session.SESSIONTYPE;
          targetModule = (string)session.TARGETMODULE ?? "";
          changesetId = (string)session.CHANGESETID ?? "";
          historyJson = (string)session.CONVERSATION ?? "";
        }

        var list = new List<object>
        {
          new
          {
            role = "system",
            content = BuildSystemPrompt(sessionType, targetModule)
          }
        };

        // 历史对话（让 LLM 记住之前用户问过什么、AI 答过什么，保持上下文连贯）
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
          catch { /* 历史解析失败不阻塞，跳过 */ }
        }

        // 历史变更项（让 LLM 知道之前产出过什么，避免重复）
        if (!string.IsNullOrEmpty(changesetId))
        {
          var items = helper.Query<ChangeItem>(
            "SELECT ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET, RATIONALE FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ISDELETED=0 ORDER BY ITEMSEQ",
            new { csid = changesetId }).ToList();
          if (items.Count > 0)
          {
            var sb = new StringBuilder();
            sb.Append("已产出的变更项（避免重复产出）：\n");
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
    /// 构造开发助理 system prompt。按 SESSIONTYPE 分支：NEW 走新建全流程，MODIFY 走修改流程。
    /// 主模板走 PromptService(aidev_system_new/aidev_system_modify, 配置中心可在线改),
    /// 读不到时回落 AiDevPrompts 代码常量；占位符 {TARGET_MODULE}/{ADD_FIELD_GUIDE}/{NAMING_RULES}/{IRON_RULES} 在代码里替换。
    /// </summary>
    private string BuildSystemPrompt(string sessionType, string targetModule)
    {
      string key = sessionType == "MODIFY" ? "aidev_system_modify" : "aidev_system_new";
      string fallback = sessionType == "MODIFY" ? AiDevPrompts.AidevModify : AiDevPrompts.AidevNew;
      string tpl = (_prompts != null ? _prompts.Get(key) : null) ?? fallback;
      return tpl
        .Replace("{TARGET_MODULE}", string.IsNullOrEmpty(targetModule) ? "" : "（目标模块: " + targetModule + "）")
        .Replace("{ADD_FIELD_GUIDE}", AiDevPrompts.AddFieldGuide)
        .Replace("{NAMING_RULES}", AiDevPrompts.NamingRules)
        .Replace("{IRON_RULES}", AiDevPrompts.IronRules);
    }


    /// <summary>
    /// 把工具结果转成 ChangeItem。
    /// 只有产出类工具（有 sql/metadata 字段）才转，只读工具（search_existing_resource/read_table_schema）返回 null。
    /// </summary>
    internal static ChangeItem TryBuildChangeItem(string toolName, JObject args, object toolResult, string changesetId)
    {
      if (string.IsNullOrEmpty(toolName) || toolResult == null) return null;
      // 只读工具不产出变更项（get_module_schema/search_menu/query_data 等已在 AssistantToolExecutor 返回不含 sql/metadata 的结构，反射取不到 sql 字段会返回空，但显式跳过更安全）
      if (toolName == "search_existing_resource" || toolName == "read_table_schema" || toolName == "search_dict" || toolName == "get_module_schema" || toolName == "search_menu" || toolName == "read_sfc_template" || toolName == "read_api_script" || toolName == "read_module_template" || toolName == "search_module_template" || toolName == "read_script_flow_api") return null;

      // 用反射取 sql/metadata/warnings 字段（toolResult 是匿名对象）
      var t = toolResult.GetType();
      string sql = t.GetProperty("sql")?.GetValue(toolResult, null)?.ToString() ?? "";
      object metadataObj = t.GetProperty("metadata")?.GetValue(toolResult, null);
      string metadataJson = metadataObj == null ? "" : JsonConvert.SerializeObject(metadataObj);
      object warningsObj = t.GetProperty("warnings")?.GetValue(toolResult, null);
      string warnings = warningsObj == null ? "" : JsonConvert.SerializeObject(warningsObj);

      // 检查是否有 error 字段（工具执行失败则不建变更项）
      object errorObj = t.GetProperty("error")?.GetValue(toolResult, null);
      if (errorObj != null) return null;

      // 映射 toolName → CATEGORY/ACTION
      var (category, action, target) = MapToolToCategory(toolName, args);
      if (category == null) return null;

      return new ChangeItem
      {
        ID = Guid.NewGuid().ToString("N"),
        CHANGESETID = changesetId,
        ITEMSEQ = 0,  // ChangeSetEngine.AppendItem 会自动分配
        CATEGORY = category,
        ACTION = action,
        TOOL = toolName,
        TARGET = target,
        SQLCONTENT = sql,
        METADATA = metadataJson,
        RATIONALE = BuildRationale(toolName, args, target),
        WARNINGS = warnings,
        DEPENDSON = null,
        ITEMSTATUS = ChangeItem.STATUS_DRAFT,
        ISDELETED = 0
      };
    }

    /// <summary>
    /// 根据工具名 + 参数生成人类可读的变更说明（替代笼统的"由 xxx 产出"）。
    /// 让用户一眼看出这条变更项具体做了什么。
    /// </summary>
    internal static string BuildRationale(string toolName, JObject args, string target)
    {
      try
      {
        string action = args["action"]?.ToString();
        string fieldName = args["fieldName"]?.ToString();
        string fieldAname = args["fieldAname"]?.ToString();
        string fieldType = args["fieldType"]?.ToString();
        int fieldLength = args["fieldLength"]?.Type == JTokenType.Integer ? (int)args["fieldLength"] : 0;
        string refTableName = args["refTableName"]?.ToString();
        string nameFieldName = args["nameFieldName"]?.ToString();
        string tableName = args["tableName"]?.ToString();
        string vckName = args["vckName"]?.ToString();
        string resourceId = args["resourceId"]?.ToString();
        string moduleCode = args["moduleCode"]?.ToString();
        string apiCode = args["apiCode"]?.ToString();
        string filterCode = args["filterCode"]?.ToString();
        string dictName = args["dictName"]?.ToString();
        string funcCode = args["funcCode"]?.ToString();
        string funcName = args["funcName"]?.ToString();

        // 资源名：configure_resource_field 的 target 是 fieldName（不是资源名），需查 resourceId → RESOURCENAME
        string resName = target;
        if (!string.IsNullOrEmpty(resourceId))
        {
          try
          {
            DBHelper h = DB.GetDBHelper();
            using (h)
            {
              string rn = h.QueryFirstOrDefault<string>(
                "SELECT RESOURCENAME FROM tss_resource WHERE ID=@rid LIMIT 1",
                new { rid = resourceId });
              if (!string.IsNullOrEmpty(rn)) resName = rn;
            }
          }
          catch { /* 查不到用 target 兜底 */ }
        }

        switch (toolName)
        {
          case "create_physical_table":
            return "创建物理表 " + tableName + " + 注册 TBS 资源 + resfield 元数据";
          case "configure_resource_field":
            if (!string.IsNullOrEmpty(refTableName))
              return "给 " + resName + " 加引用字段对 " + fieldName + "+" + nameFieldName + "（关联 " + refTableName + "，" + (fieldAname ?? fieldName) + "）";
            string typeInfo = fieldType ?? "VARCHAR";
            if (fieldLength > 0) typeInfo += "(" + fieldLength + ")";
            string aname = !string.IsNullOrEmpty(fieldAname) ? "（" + fieldAname + "）" : "";
            return "给 " + resName + " " + (action == "update" ? "修改" : "新增") + "字段 " + fieldName + " " + typeInfo + aname;
          case "define_dataview":
            return "定义 DATAVIEW " + vckName + " + resfield（REFFIELDID 链向 " + tableName + "）";
          case "configure_ui_field":
            return "配置字段 " + target + " 的 UI（列表列+表单控件一次配全）";
          case "create_dict":
            return "创建字典 " + dictName + " + 字典项";
          case "define_filter":
            return "定义过滤器 " + filterCode + "（" + target + "）";
          case "register_module":
            return "注册模块 " + moduleCode + "（" + target + "）";
          case "define_api":
            return "定义接口 " + apiCode + "（模块 " + moduleCode + "）";
          case "define_sql_api":
            return "定义 SQL 脚本接口 " + apiCode + "（模块 " + moduleCode + "，SQL模板 " + args["sqlCode"] + "）";
          case "define_script_api":
            return "定义 C# 脚本接口 " + apiCode + "（模块 " + moduleCode + "，脚本 " + args["scriptCode"] + "，在线热更新）";
          case "define_script_flow_api":
            return "定义编排接口 " + apiCode + "（模块 " + moduleCode + "，" + (args["steps"]?.ToString()?.Length > 50 ? args["steps"].ToString().Substring(0, 50) + "..." : args["steps"]?.ToString()) + "）";
          case "update_script_flow_api":
            return "修改编排接口 " + target + "（更新步骤配置）";
          case "create_menu":
            return "创建菜单 " + funcCode + "（" + funcName + "）";
          case "create_funcpoints":
            return "创建功能点权限（菜单 " + funcCode + "）";
          case "define_page":
            return "定义页面 " + args["moduleCode"] + "/" + args["pageCode"] + "（" + args["pageName"] + "，" + args["pageType"] + (args["componentType"]?.ToString() == "sfc" ? "，SFC 组件" : "，通用模板") + "）";
          case "define_button":
            return "定义按钮 " + args["btnName"] + "（" + args["moduleCode"] + "/" + args["pageCode"] + " 的 " + args["btnArea"] + " 区域，btnCode=" + (args["btnCode"] ?? "custom") + "）";
          case "create_sfc_module":
            return "创建 SFC 在线模块 " + target + "（前端 Vue 页面，存 tbs_sfc_template）";
          default:
            return "由 " + toolName + " 产出（" + target + "）";
        }
      }
      catch
      {
        return "由 " + toolName + " 产出";
      }
    }

    /// <summary>
    /// 工具名 → (CATEGORY, ACTION, TARGET) 映射。
    /// </summary>
    internal static (string category, string action, string target) MapToolToCategory(string toolName, JObject args)
    {
      string target = ExtractTarget(toolName, args);
      switch (toolName)
      {
        case "create_physical_table":
          return (ChangeItem.CAT_PHYSICAL_TABLE, ChangeItem.ACTION_CREATE, target);
        case "add_field_to_table":
          return (ChangeItem.CAT_FIELD, ChangeItem.ACTION_ALTER, target);
        case "define_dataview":
          return (ChangeItem.CAT_DATAVIEW, ChangeItem.ACTION_CREATE, target);
        case "define_reference_field":
          return (ChangeItem.CAT_FIELD, ChangeItem.ACTION_CREATE, target);
        case "configure_resource_field":
          return (ChangeItem.CAT_FIELD, args["action"]?.ToString() == "update" ? ChangeItem.ACTION_UPDATE : ChangeItem.ACTION_CREATE, target);
        case "configure_ui_field":
          return (ChangeItem.CAT_UI, ChangeItem.ACTION_UPDATE, target);
        case "create_dict":
          return (ChangeItem.CAT_DICT, ChangeItem.ACTION_CREATE, target);
        case "define_filter":
          return (ChangeItem.CAT_FILTER, ChangeItem.ACTION_CREATE, target);
        case "register_module":
          return (ChangeItem.CAT_MODULE, ChangeItem.ACTION_CREATE, target);
        case "define_api":
          return (ChangeItem.CAT_API, ChangeItem.ACTION_CREATE, target);
        case "define_sql_api":
          return (ChangeItem.CAT_API, ChangeItem.ACTION_CREATE, target);
        case "define_script_api":
          return (ChangeItem.CAT_API, ChangeItem.ACTION_CREATE, target);
        case "define_script_flow_api":
          return (ChangeItem.CAT_API, ChangeItem.ACTION_CREATE, target);
        case "update_script_flow_api":
          return (ChangeItem.CAT_API, ChangeItem.ACTION_UPDATE, target);
        case "create_menu":
          return (ChangeItem.CAT_MENU, ChangeItem.ACTION_CREATE, target);
        case "create_funcpoints":
          return (ChangeItem.CAT_PERMISSION, ChangeItem.ACTION_CREATE, target);
        case "define_page":
          return (ChangeItem.CAT_PAGE, ChangeItem.ACTION_CREATE, target);
        case "define_button":
          return (ChangeItem.CAT_BUTTON, ChangeItem.ACTION_CREATE, target);
        case "create_sfc_module":
          return (ChangeItem.CAT_MODULE, ChangeItem.ACTION_CREATE, target);
        default:
          return (null, null, null);
      }
    }

    /// <summary>
    /// 从工具参数提取 TARGET（用于变更项描述）。
    /// </summary>
    internal static string ExtractTarget(string toolName, JObject args)
    {
      if (args == null) return "";
      switch (toolName)
      {
        case "create_physical_table":
        case "add_field_to_table":
          return args["tableName"]?.ToString() ?? "";
        case "define_dataview":
          return args["vckName"]?.ToString() ?? "";
        case "define_reference_field":
          return args["fieldName"]?.ToString() ?? "";
        case "configure_resource_field":
          return args["fieldName"]?.ToString() ?? (args["resourceId"]?.ToString() ?? "");
        case "configure_ui_field":
          return args["fieldId"]?.ToString() ?? "";
        case "create_dict":
          return args["dictName"]?.ToString() ?? "";
        case "define_filter":
          return args["filterCode"]?.ToString() ?? "";
        case "register_module":
          return args["moduleCode"]?.ToString() ?? "";
        case "define_api":
          return args["apiCode"]?.ToString() ?? "";
        case "define_sql_api":
          return (args["moduleCode"]?.ToString() ?? "") + "/" + (args["apiCode"]?.ToString() ?? "");
        case "define_script_api":
          return (args["moduleCode"]?.ToString() ?? "") + "/" + (args["apiCode"]?.ToString() ?? "");
        case "define_script_flow_api":
          return (args["moduleCode"]?.ToString() ?? "") + "/" + (args["apiCode"]?.ToString() ?? "");
        case "update_script_flow_api":
          return args["apiId"]?.ToString() ?? "";
        case "read_script_flow_api":
          return (args["moduleCode"]?.ToString() ?? "") + "/" + (args["apiCode"]?.ToString() ?? "");
        case "create_menu":
          return args["funcCode"]?.ToString() ?? "";
        case "create_funcpoints":
          return args["funcCode"]?.ToString() ?? "";
        case "define_page":
          return (args["moduleCode"]?.ToString() ?? "") + "/" + (args["pageCode"]?.ToString() ?? "");
        case "define_button":
          return args["btnName"]?.ToString() ?? "";
        case "create_sfc_module":
          return args["templateCode"]?.ToString() ?? "";
        default:
          return "";
      }
    }

    /// <summary>
    /// 构造工具结果摘要（用于 SSE 推送，避免把完整 toolResult 序列化出去）。
    /// 工具名 → 流程步骤 key 映射（前端 todoList 按此 key 匹配步骤）。
    /// 返回 null 表示该工具不对应流程步骤（如 search_menu 等辅助工具）。
    /// </summary>
    internal static string MapToolToStep(string toolName)
    {
      if (string.IsNullOrEmpty(toolName)) return null;
      switch (toolName)
      {
        case "search_existing_resource": return "check_resource";
        case "read_table_schema": return "read_schema";
        case "get_module_schema": return "read_schema";      // MODIFY 场景读模块 schema
        case "create_physical_table": return "create_table";
        case "add_field_to_table": return "add_field";        // MODIFY 场景加字段
        case "configure_resource_field": return "add_field";   // 直接改 resfield 归到加字段步骤
        case "define_dataview": return "create_view";
        case "define_reference_field": return "create_view";  // 引用字段归到视图步骤
        case "configure_ui_field": return "config_ui";
        case "create_dict": return "create_dict";
        case "define_filter": return "create_filter";
        case "register_module": return "register_module";
        case "define_api": return "create_api";
        case "define_sql_api": return "create_api";  // SQL 脚本接口归到接口步骤
        case "define_script_api": return "create_api";  // C# 脚本接口归到接口步骤
        case "define_script_flow_api": return "create_api";  // 编排接口归到接口步骤
        case "update_script_flow_api": return "create_api";  // 修改编排接口归到接口步骤
        case "read_script_flow_api": return "read_schema";   // 读取编排接口归到读schema步骤
        case "create_menu": return "create_menu";
        case "create_funcpoints": return "create_funcpoints";
        case "define_page": return "create_page";
        case "define_button": return "create_page";  // 按钮归到页面配置步骤
        case "create_sfc_module": return "register_module";  // SFC 模块归到模块步骤
        default: return null;
      }
    }

    /// <summary>
    /// 有 error 返回 error 内容，有 sql 返回 sql 截断，否则返回 JSON 截断。
    /// </summary>
    internal static string BuildToolResultSummary(object toolResult)
    {
      if (toolResult == null) return "";
      var t = toolResult.GetType();
      object errorObj = t.GetProperty("error")?.GetValue(toolResult, null);
      if (errorObj != null) return "error: " + errorObj;
      string sql = t.GetProperty("sql")?.GetValue(toolResult, null)?.ToString() ?? "";
      if (!string.IsNullOrEmpty(sql))
      {
        return "sql: " + (sql.Length > 200 ? sql.Substring(0, 200) + "..." : sql);
      }
      string json = JsonConvert.SerializeObject(toolResult);
      return json.Length > 200 ? json.Substring(0, 200) + "..." : json;
    }

    /// <summary>
    /// 用 userId 构造一个最小 userInfo Hashtable（工具执行可能需要）。
    /// </summary>
    internal static Hashtable BuildUserInfo(string userId)
    {
      var ht = new Hashtable();
      ht["ID"] = userId ?? "anonymous";
      return ht;
    }
  }

  /// <summary>
  /// 编排器返回结果。
  /// </summary>
  public class OrchestratorResult
  {
    public string SessionId;
    public string SessionCode;
    public string ChangeSetId;
    public string Conversation;
    public List<ChangeItem> NewItems;
    public ValidationReport ValidationReport;
    public List<string> Warnings;
    public string Error;
  }

  /// <summary>历史对话消息（持久化到 tss_aidev_session.CONVERSATION）</summary>
  public class HistoryMessage
  {
    public string role;     // user / assistant
    public string content;  // 文本内容
    public string ts;       // 时间戳 yyyy-MM-dd HH:mm:ss
  }
}
