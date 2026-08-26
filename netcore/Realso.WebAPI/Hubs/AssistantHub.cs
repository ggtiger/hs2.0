using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.WebAPI.Services;
using Realso.WebAPI.Services.Agent;

namespace Realso.WebAPI.Hubs
{
  /// <summary>
  /// 智能助理 SignalR Hub。
  /// Ask：通用助理（全局抽屉，block 事件，会话落库）。
  /// AskForm：表单填报（FormAssistantPanel，formblock 事件，per-connection 内存会话）。
  /// 共用 AgentEngine.RunLoopAsync ReAct 循环（阶段 2a 重构：替代原 RunAgentLoop）。
  /// 工具层委托 IToolRegistry + IToolExecutor，前端工具委托 FrontendToolHandler。
  /// CORS 由全局 "SignalrCore" 策略处理。
  /// [Authorize]：必须登录（JWT 经 query string access_token 传递，见 Startup JwtBearerEvents）。
  /// </summary>
  [Authorize]
  public class AssistantHub : Hub
  {
    private readonly LlmConfigService _cfg;
    private readonly SessionStore _sessions;
    private readonly UsageLogger _usage;
    private readonly PromptService _prompts;
    private readonly AssistantAgentEngine _engine;
    private readonly IToolRegistry _toolRegistry;

    // 表单填报的 per-connection 内存会话（不落库，断开即清）。含 moduleCode，切换模块时重置。
    private static readonly ConcurrentDictionary<string, FormChatSession> _formChats = new ConcurrentDictionary<string, FormChatSession>();

    // 表单填报会话：记录当前模块 + 消息历史。切换模块（moduleCode 变）时清空 Messages 重建 system prompt，
    // 避免旧模块上下文污染（如部门管理切到其他模块，AI 还用部门管理的 schema）。
    private class FormChatSession
    {
      public string ModuleCode;
      public List<object> Messages = new List<object>();
    }

    public AssistantHub(LlmConfigService cfg, SessionStore sessions, UsageLogger usage, PromptService prompts,
      AssistantAgentEngine engine, IToolRegistry toolRegistry)
    {
      _cfg = cfg;
      _sessions = sessions;
      _usage = usage;
      _prompts = prompts;
      _engine = engine;
      _toolRegistry = toolRegistry;
    }

    /// <summary>
    /// 设置当前请求的 SignalR 连接上下文（Caller + ConnectionId），
    /// 供 SignalREventSink 推送 block、FrontendToolHandler 推送 frontend_tool_call 用。
    /// 在每个 Hub 方法入口调用，用 using scope 保证请求结束自动清理。
    /// </summary>
    private CallerScope SetCaller()
    {
      FrontendToolHandler.Current = new AgentHubCallerContext
      {
        ConnectionId = Context.ConnectionId,
        Caller = Clients.Caller
      };
      return new CallerScope();
    }

    /// <summary>请求结束时清理 Caller 上下文（防止泄漏到下次请求）</summary>
    private struct CallerScope : IDisposable
    {
      public void Dispose() => FrontendToolHandler.Current = null;
    }

    /// <summary>
    /// 前端注册工具定义：前端连接后调用，把代理层的工具定义注册给后端。
    /// 后端在 AgentEngine 循环时合并到 LLM 的工具列表，让 LLM 知道并能调用这些前端工具。
    /// </summary>
    public Task RegisterFrontendTools(string toolDefsJson)
    {
      try
      {
        var defs = JsonConvert.DeserializeObject<List<object>>(toolDefsJson);
        if (defs != null && defs.Count > 0)
        {
          FrontendToolHandler.RegisterDefinitions(Context.ConnectionId, defs);
          Console.WriteLine($"[AssistantHub] 连接 {Context.ConnectionId} 注册了 {defs.Count} 个前端工具");
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("[AssistantHub] 注册前端工具失败: " + e.Message);
      }
      return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
      _formChats.TryRemove(Context.ConnectionId, out _);
      FrontendToolHandler.ClearForConnection(Context.ConnectionId);
      return base.OnDisconnectedAsync(exception);
    }

    /// <summary>前端工具结果回传（SignalR 通道，备用）。主用 HTTP /api/assistant/tool-result</summary>
    public Task FrontendToolResult(string callId, string resultJson)
    {
      FrontendToolHandler handler = new FrontendToolHandler();
      handler.HandleResult(callId, resultJson);
      return Task.CompletedTask;
    }

    /// <summary>
    /// AI优化提示词：前端管理页面调，传入当前提示词内容，返回LLM优化后的文本。
    /// 用 meta_optimize_prompt（从 TBS_ASSISTANT_PROMPT 读）让 LLM 优化，不走 ReAct 循环，不调工具。
    /// </summary>
    public async Task<string> OptimizePrompt(string content)
    {
      if (string.IsNullOrWhiteSpace(content)) return content;
      var cfg = _cfg.GetEnabled();
      if (cfg == null) return "⚠️ 未配置 LLM，请先在管理后台配置 DeepSeek API Key";

      string metaPrompt = _prompts.Get("meta_optimize_prompt") ?? "";
      var messages = new List<object>
      {
        new { role = "user", content = metaPrompt + content }
      };
      string result = "";
      try
      {
        var llm = new LlmClient(new System.Net.Http.HttpClient());
        var usage = await llm.StreamChatAsync(cfg, messages, null,
          onContent: c => { result += c; return Task.CompletedTask; });
        return result;
      }
      catch (System.Exception e)
      {
        return "⚠️ 优化失败：" + e.Message;
      }
    }

    /// <summary>
    /// AI识别图片：前端粘贴图片后调用，调视觉LLM识别图片内容，返回文字描述。
    /// 前端把返回的文字填入输入框，用户发送后由 deepseek-chat 根据表单schema解析填表。
    /// </summary>
    public async Task<string> AnalyzeImage(string base64Image, string mimeType)
    {
      var visionCfg = _cfg.GetVision();
      if (visionCfg == null) return "⚠️ 未配置视觉LLM，请在LLM配置页加一个ISVISION=1的模型（如GLM-4V/通义千问VL）";
      try
      {
        string prompt = _prompts.Get("vision_default_prompt");
        var llm = new LlmClient(new System.Net.Http.HttpClient());
        return await llm.AnalyzeImageAsync(visionCfg, base64Image, mimeType, prompt);
      }
      catch (System.Exception e)
      {
        return "⚠️ 图片识别失败：" + e.Message;
      }
    }

    // ============ 按场景测试对话（AI 配置中心用） ============
    /// <summary>
    /// 按 scene 编号加载场景配置，用场景指定的模型/提示词/工具集/参数运行对话。
    /// 复用 Ask 的 ReAct 循环，区别在于场景由前端动态指定而非硬编码。
    /// 事件通道: block（与 Ask 一致，前端 AiClient 注册 assistant blockCallback 接收）。
    /// </summary>
    public async Task AskScene(string scene, string conversationId, string message, string userInfoJson)
    {
      using (SetCaller())
      {
        try
        {
          if (string.IsNullOrWhiteSpace(scene)) scene = "assistant";
          Hashtable userInfo = GetUserInfo(); // 从 JWT 取身份，忽略前端自报 userInfoJson
          if (userInfo == null) { await SendBlock("block", "error", "无法识别登录用户，请重新登录"); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }
          string userId = userInfo != null ? (userInfo["ID"] + "") : "anonymous";
          string userName = userInfo != null ? (userInfo["NICKNAME"] + "") : "";

          // 按场景编码加载配置
          var sceneCfg = SceneConfigService.GetScene(scene);
          if (sceneCfg == null) { await SendBlock("block", "error", "场景 " + scene + " 未配置"); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }

          // 会话管理: 用 scene 前缀隔离不同场景的会话
          string convId;
          if (string.IsNullOrEmpty(conversationId))
          {
            convId = _sessions.Create(userId, userName);
            await Clients.Caller.SendAsync("block", new { type = "conversation", conversationId = convId });
          }
          else
          {
            convId = conversationId;
          }
          _sessions.AddUser(convId, message);

          // 模型路由：场景指定模型 > 全局默认
          var cfg = _cfg.GetByScene(sceneCfg);
          if (cfg == null) { await SendBlock("block", "error", "未配置 LLM，请先在管理后台配置 DeepSeek API Key"); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }

          // 场景配额检查
          var quotaErr = SceneConfigService.CheckDailyQuota(sceneCfg, "scene_test");
          if (quotaErr != null) { await SendBlock("block", "error", quotaErr); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }

          var executor = new AssistantToolExecutor(userInfo);
          // system prompt 按场景 PROMPTKEY 选取
          string systemPrompt = _prompts.Get(sceneCfg.PROMPTKEY ?? "system_general");
          var messages = new List<object>
          {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = message }
          };

          // 工具集从场景配置解析，TOOLSET 为空表示无工具
          var tools = string.IsNullOrEmpty(sceneCfg.TOOLSET)
            ? new List<object>()
            : MergeWithFrontendTools(executor.GetDefinitionsBySet(sceneCfg.TOOLSET));

          var options = new AgentOptions { MaxSteps = 50, MaxToolResultChars = 4000, SummaryTruncateChars = 200 };
          options.ApplySceneParams(sceneCfg.PARAMS);

          var req = new AgentRunRequest
          {
            Messages = messages,
            Tools = tools,
            Cfg = cfg,
            UserId = userId,
            UserName = userName,
            ConversationId = convId,
            OperationType = "scene_test",
            ToolContext = new ToolContext
            {
              UserInfo = userInfo,
              ConnectionId = Context.ConnectionId,
              FrontendHandler = new FrontendToolHandler()
            },
            Options = options
          };

          var sink = new SignalREventSink("block");
          var usageRecorder = new DbUsageLogger(_usage);
          await _engine.RunLoopAsync(req, sink, executor, usageRecorder);
        }
        catch (Exception ex)
        {
          Console.WriteLine("[AskScene] 异常: " + ex.Message + "\n" + ex.StackTrace);
          try { await SendBlock("block", "error", "AskScene 异常: " + ex.Message); } catch { }
          try { await Clients.Caller.SendAsync("block", new { type = "done" }); } catch { }
        }
      }
    }

    // ============ 通用助理（全局抽屉） ============
    public async Task Ask(string conversationId, string message, string userInfoJson)
    {
      using (SetCaller())
      {
        Hashtable userInfo = GetUserInfo(); // 从 JWT 取身份，忽略前端自报 userInfoJson
        if (userInfo == null) { await SendBlock("block", "error", "无法识别登录用户，请重新登录"); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }
        string userId = userInfo != null ? (userInfo["ID"] + "") : "anonymous";
        string userName = userInfo != null ? (userInfo["NICKNAME"] + "") : "";

        if (string.IsNullOrEmpty(conversationId))
        {
          conversationId = _sessions.Create(userId, userName);
          await Clients.Caller.SendAsync("block", new { type = "conversation", conversationId });
        }
        _sessions.AddUser(conversationId, message);

        // 场景配置：驱动模型路由、工具集、Agent参数
        var sceneCfg = SceneConfigService.GetScene("assistant");
        var cfg = _cfg.GetByScene(sceneCfg);
        if (cfg == null) { await SendBlock("block", "error", "未配置 LLM，请先在管理后台配置 DeepSeek API Key"); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }

        // 场景配额检查
        var quotaErr = SceneConfigService.CheckDailyQuota(sceneCfg, "chat");
        if (quotaErr != null) { await SendBlock("block", "error", quotaErr); await Clients.Caller.SendAsync("block", new { type = "done" }); return; }

        var executor = new AssistantToolExecutor(userInfo);
        var messages = BuildLlmMessages(_sessions.Load(conversationId), message);
        // 后端工具定义 + 前端注册的工具定义合并，工具描述用 tool:<name> 覆盖
        // 工具集从场景配置解析（tss_ai_scene.TOOLSET，缺省回落 assistant）
        var tools = MergeWithFrontendTools(executor.GetDefinitionsBySet(sceneCfg?.TOOLSET ?? "assistant"));

        var options = new AgentOptions { MaxSteps = 50, MaxToolResultChars = 4000, SummaryTruncateChars = 200 };
        options.ApplySceneParams(sceneCfg?.PARAMS);

        var req = new AgentRunRequest
        {
          Messages = messages,
          Tools = tools,
          Cfg = cfg,
          UserId = userId,
          UserName = userName,
          ConversationId = conversationId,
          OperationType = "chat",
          ToolContext = new ToolContext
          {
            UserInfo = userInfo,
            ConnectionId = Context.ConnectionId,
            FrontendHandler = new FrontendToolHandler()
          },
          Options = options
        };

        var sink = new SignalREventSink("block");
        var usageRecorder = new DbUsageLogger(_usage);
        var result = await _engine.RunLoopAsync(req, sink, executor, usageRecorder);
        if (result.FinalText != null) _sessions.AddAssistant(conversationId, result.FinalText);
        // 保存完整LLM消息历史（含tool_calls/tool results），供下次对话恢复上下文
        _sessions.SaveFullMessages(conversationId, messages);
      }
    }

    // ============ 表单填报（FormAssistantPanel） ============
    public async Task AskForm(string moduleCode, string message, string userInfoJson, string formDataJson)
    {
      using (SetCaller())
      {
        Hashtable userInfo = GetUserInfo(); // 从 JWT 取身份，忽略前端自报 userInfoJson
        if (userInfo == null) { await SendBlock("formblock", "error", "无法识别登录用户，请重新登录"); await Clients.Caller.SendAsync("formblock", new { type = "done" }); return; }
        string userId = userInfo != null ? (userInfo["ID"] + "") : "anonymous";
        string userName = userInfo != null ? (userInfo["NICKNAME"] + "") : "";

        // 场景配置（tss_ai_scene）：PROMPTKEY 驱动提示词选择，MODELID 驱动模型路由
        var formSceneCfg = SceneConfigService.GetScene("form");
        var cfg = _cfg.GetByScene(formSceneCfg);
        if (cfg == null) { await SendBlock("formblock", "error", "未配置 LLM"); await Clients.Caller.SendAsync("formblock", new { type = "done" }); return; }

        // 场景配额检查
        var formQuotaErr = SceneConfigService.CheckDailyQuota(formSceneCfg, "form");
        if (formQuotaErr != null) { await SendBlock("formblock", "error", formQuotaErr); await Clients.Caller.SendAsync("formblock", new { type = "done" }); return; }

        // 解析当前表单数据
        string currentDataPrompt = BuildCurrentDataPrompt(formDataJson);

        // per-connection 内存会话：切换模块（moduleCode 变）时重置 Messages，重建 system prompt
        var session = _formChats.GetOrAdd(Context.ConnectionId, k => new FormChatSession());
        if (session.ModuleCode != moduleCode)
        {
          session.Messages.Clear();
          session.ModuleCode = moduleCode;
        }
        var messages = session.Messages;
        if (messages.Count == 0)
        {
          // system prompt 从 TBS_ASSISTANT_PROMPT 读（key 由场景配置 PROMPTKEY 决定），替换占位符
          string formPromptTemplate = _prompts.Get(formSceneCfg?.PROMPTKEY ?? "system_form") ?? "";
          string formPrompt = formPromptTemplate
            .Replace("{moduleCode}", moduleCode)
            .Replace("{currentDataPrompt}", currentDataPrompt);
          messages.Add(new { role = "system", content = formPrompt });
        }
        messages.Add(new { role = "user", content = message });

        var executor = new AssistantToolExecutor(userInfo);
        var tools = MergeWithFrontendTools(executor.GetDefinitionsBySet(formSceneCfg?.TOOLSET ?? "formfill"));

        var formOptions = new AgentOptions { MaxSteps = 50, MaxToolResultChars = 4000, SummaryTruncateChars = 200 };
        formOptions.ApplySceneParams(formSceneCfg?.PARAMS);

        var req = new AgentRunRequest
        {
          Messages = messages,
          Tools = tools,
          Cfg = cfg,
          UserId = userId,
          UserName = userName,
          ConversationId = "FORMFILL_" + moduleCode,
          OperationType = "form",
          ModuleCode = moduleCode,
          ToolContext = new ToolContext
          {
            UserInfo = userInfo,
            ConnectionId = Context.ConnectionId,
            FrontendHandler = new FrontendToolHandler()
          },
          Options = formOptions
        };

        var sink = new SignalREventSink("formblock");
        var usageRecorder = new DbUsageLogger(_usage);
        await _engine.RunLoopAsync(req, sink, executor, usageRecorder);
      }
    }

    /// <summary>
    /// 合并后端工具定义和前端注册的工具定义（按工具名去重，后端定义优先保留详细描述）。
    /// 工具描述用 TBS_ASSISTANT_PROMPT 表里的 tool:<name> 覆盖（页面可配置）。
    /// </summary>
    private List<object> MergeWithFrontendTools(List<object> backendTools)
    {
      var merged = new List<object>();
      var names = new HashSet<string>();
      // 后端工具：用表里的 tool:<name> 覆盖 description
      foreach (var t in backendTools)
      {
        try
        {
          var jo = JObject.FromObject(t);
          var name = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(name))
          {
            names.Add(name);
            string dbDesc = _prompts.Get("tool:" + name);
            if (!string.IsNullOrEmpty(dbDesc))
            {
              jo["function"]["description"] = dbDesc;
            }
            merged.Add(jo);
          }
        }
        catch { merged.Add(t); }
      }
      // 前端工具：用表里的 tool:<name> 覆盖 description
      var feHandler = new FrontendToolHandler();
      foreach (var fd in feHandler.GetRegisteredDefinitions() ?? new List<object>())
      {
        try
        {
          var jo = fd is JObject j ? j : JObject.FromObject(fd);
          var name = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(name) && names.Add(name))
          {
            string dbDesc = _prompts.Get("tool:" + name);
            if (!string.IsNullOrEmpty(dbDesc))
            {
              jo["function"]["description"] = dbDesc;
            }
            merged.Add(jo);
          }
        }
        catch { }
      }
      return merged;
    }

    private async Task SendBlock(string eventName, string type, string text)
    {
      await Clients.Caller.SendAsync(eventName, new { type, text });
    }

    /// <summary>
    /// 从 JWT claims(sub=USERNAME) 查询用户信息，不信任前端自报的 userInfoJson。
    /// 返回与 VSS_USER 行一致的键：ID/USERNAME/NICKNAME/USERTYPE/EMPID/EMPCODE/EMPNAME/DEPTID。
    /// </summary>
    private Hashtable GetUserInfo()
    {
      // .NET 2.2 JwtBearer 会把 sub 映射为 nameidentifier，三种取法兜底
      string username = Context.User?.FindFirst("sub")?.Value
        ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.Identity?.Name;
      if (string.IsNullOrEmpty(username))
      {
        var claims = Context.User?.Claims == null ? "(no claims)"
          : string.Join(", ", System.Linq.Enumerable.Select(Context.User.Claims, c => c.Type + "=" + c.Value));
        Realso.Utils.Logger.Error("[AssistantHub] 无法从 token 获取用户名, claims: " + claims);
        return null;
      }
      var row = DB.GetDBHelper().QueryFirstOrDefault(
        "SELECT U.ID, U.USERNAME, U.NICKNAME, U.USERTYPE, U.EMPID, E.EMPCODE, E.EMPNAME, E.DEPTID" +
        " FROM TSS_USER U LEFT JOIN TBS_EMP E ON U.EMPID = E.ID WHERE U.USERNAME = @USERNAME",
        new { USERNAME = username });
      if (row == null)
      {
        Realso.Utils.Logger.Error("[AssistantHub] TSS_USER 查不到用户: " + username);
        return null;
      }
      Hashtable ht = new Hashtable();
      ht["ID"] = row.ID;
      ht["USERNAME"] = row.USERNAME;
      ht["NICKNAME"] = row.NICKNAME;
      ht["USERTYPE"] = row.USERTYPE;
      ht["EMPID"] = row.EMPID;
      ht["EMPCODE"] = row.EMPCODE;
      ht["EMPNAME"] = row.EMPNAME;
      ht["DEPTID"] = row.DEPTID;
      return ht;
    }

    /// <summary>解析 formDataJson 生成"当前表单已有数据"提示</summary>
    private static string BuildCurrentDataPrompt(string formDataJson)
    {
      if (string.IsNullOrEmpty(formDataJson)) return "";
      try
      {
        var formData = JsonConvert.DeserializeObject<Hashtable>(formDataJson);
        if (formData == null || formData.Count == 0) return "";
        var mainPairs = new List<string>();
        var subTables = new List<string>();
        foreach (DictionaryEntry entry in formData)
        {
          string key = entry.Key + "";
          if (key.StartsWith("__subtable_"))
          {
            string subPath = key.Substring("__subtable_".Length);
            var rows = entry.Value as JArray;
            if (rows != null && rows.Count > 0)
            {
              subTables.Add("子表 " + subPath + "：" + rows.Count + " 行数据");
            }
            continue;
          }
          string val = entry.Value + "";
          if (string.IsNullOrEmpty(val) || val == "0" || val.ToLower() == "false") continue;
          mainPairs.Add(key + "=" + val);
        }
        if (mainPairs.Count == 0 && subTables.Count == 0) return "";
        string prompt = "当前表单已有数据：\n";
        if (mainPairs.Count > 0) prompt += string.Join("\n", mainPairs) + "\n";
        if (subTables.Count > 0) prompt += string.Join("\n", subTables) + "\n";
        prompt += "\n";
        return prompt;
      }
      catch { return ""; }
    }

    private List<object> BuildLlmMessages(AssistantSession session, string currentUserMessage)
    {
      // system prompt 从 TBS_ASSISTANT_PROMPT 读（key 由场景配置 PROMPTKEY 决定，缺省 system_general），表里没数据用代码默认值兜底
      string systemPrompt = _prompts.Get(SceneConfigService.GetScene("assistant")?.PROMPTKEY ?? "system_general");
      var list = new List<object>
      {
        new { role = "system", content = systemPrompt }
      };
      // 优先用完整历史（含tool_calls/tool results，上次SaveFullMessages存的）
      if (session.FullMessages != null && session.FullMessages.Count > 0)
      {
        list.AddRange(session.FullMessages);
      }
      else
      {
        // 首次：用text历史（排除本次user，后面单独加）
        foreach (var m in session.Messages)
        {
          if (m.Role == "user" && m.Content == currentUserMessage) continue;
          list.Add(new { role = m.Role, content = m.Content });
        }
      }
      // 本次user消息
      list.Add(new { role = "user", content = currentUserMessage });
      return list;
    }
  }
}
