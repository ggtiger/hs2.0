using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Realso.Core.Base;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Realso.WebAPI.Services;
using Realso.WebAPI.Services.Agent;
using Realso.WebAPI.Services.AiDev;
using Realso.WebAPI.Services.AiMemory;
using Realso.WebAPI.Models;
using Realso.WebAPI.Models.AiDev;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// AI 开发助理 Controller。
  /// 继承 DataController 走统一入口 api/data/call/RM_AIDEV/{ApiCode}，
  /// 在 doMyApi 里 switch APICODE 分发到开发助理相关操作。
  ///
  /// 接口清单：
  /// - A05 generate  : 触发一次 LLM 编排生成（异步）
  /// - A06 validate  : 校验变更包
  /// - A07 export    : 导出 .aidev.sql 脚本（冻结会话）
  /// - A09 confirm   : 确认变更项
  /// - A10 reject    : 拒绝变更项
  /// - A11 unconfirm : 撤销确认
  /// - A12 getScript : 获取已确认脚本（预览，不冻结）
  /// </summary>
  [Route("api/[controller]")]
  [Authorize]
  public class RMAIDevController : DataController
  {
    private readonly AiDevOrchestrator _orchestrator;
    private readonly WizardStepOrchestrator _wizardOrchestrator;
    private readonly ChangeSetEngine _changeSetEngine;
    private readonly ChangeSetExporter _exporter;
    private readonly LlmConfigService _llmCfg;

    public RMAIDevController(AiDevOrchestrator orchestrator, WizardStepOrchestrator wizardOrchestrator, ChangeSetEngine changeSetEngine, ChangeSetExporter exporter, LlmConfigService llmCfg)
    {
      _orchestrator = orchestrator;
      _wizardOrchestrator = wizardOrchestrator;
      _changeSetEngine = changeSetEngine;
      _exporter = exporter;
      _llmCfg = llmCfg;
    }

    /// <summary>
    /// 重写 doMyApi：按 APICODE 分发。
    /// APICODE 从 row 取（与 RM11Controller 一致），也兼容从 Params 取。
    /// </summary>
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row != null ? row.GetString("APICODE") : (Params["APICODE"] as string);
      if (string.IsNullOrEmpty(APICODE) && Params["APICODE"] != null)
        APICODE = Params["APICODE"].ToString();

      // 用户ID：优先 Params["__USERID__"]，其次 userInfo.ID
      string userId = "anonymous";
      if (Params != null && Params["__USERID__"] != null)
        userId = Params["__USERID__"].ToString();
      else if (this.userInfo != null && this.userInfo["ID"] != null)
        userId = this.userInfo["ID"].ToString();

      switch (APICODE)
      {
        case "A05":  // generate
          doGenerate(MD, row, Params, userId);
          break;
        case "A06":  // validate
          doValidate(MD, row, Params);
          break;
        case "A07":  // export
          doExport(MD, row, Params);
          break;
        case "A09":  // confirm
          doConfirm(MD, row, Params, userId);
          break;
        case "A10":  // reject
          doReject(MD, row, Params);
          break;
        case "A11":  // unconfirm
          doUnconfirm(MD, row, Params);
          break;
        case "A12":  // getScript
          doGetScript(MD, row, Params);
          break;
        case "A13":  // archive 归档会话
          doArchive(MD, row, Params);
          break;
        case "A14":  // dedup 去重清理变更项
          doDedup(MD, row, Params);
          break;
        case "A15":  // merge 合并所有 DRAFT 为一条统一变更项
          doMerge(MD, row, Params);
          break;
        case "A16":  // listItems 查变更项列表（按 changesetId）
          doListItems(MD, row, Params);
          break;
        case "A17":  // executeConfirmed 执行已确认脚本（开发环境直接落库）
          doExecuteConfirmed(MD, row, Params);
          break;
        case "A18":  // getConversation 加载历史对话
          doGetConversation(MD, row, Params);
          break;
        case "A19":  // openWizardSession 创建模块向导会话（session + changeset，6 步共享）
          doOpenWizardSession(MD, row, Params, userId);
          break;
        default:
          responseModel.SetError("接口编码:" + APICODE + " 不存在！");
          break;
      }
    }

    /// <summary>
    /// A13 archive：归档会话（EXPORTED → ARCHIVED）。
    /// 入参：{sessionId}
    /// </summary>
    private void doArchive(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string sessionId = Params["sessionId"] as string;
      if (string.IsNullOrEmpty(sessionId))
      {
        responseModel.SetError("sessionId 不能为空");
        return;
      }
      try
      {
        _changeSetEngine.ArchiveSession(sessionId);
        responseModel.SetData(new { success = true, status = "ARCHIVED" });
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A14 dedup：去重清理变更项（同 changeset 内 CATEGORY+ACTION+TARGET+SQL 相同的只保留最早一条）。
    /// 入参：{sessionId} 或 {changesetId}。
    /// </summary>
    private void doDedup(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      if (string.IsNullOrEmpty(changesetId))
      {
        string sessionId = Params["sessionId"] as string;
        if (!string.IsNullOrEmpty(sessionId))
        {
          DBHelper h = DB.GetDBHelper();
          using (h)
          {
            changesetId = h.QueryFirstOrDefault<string>(
              "SELECT CHANGESETID FROM tss_aidev_session WHERE ID=@sid AND ISDELETED=0",
              new { sid = sessionId });
          }
        }
      }
      if (string.IsNullOrEmpty(changesetId))
      {
        responseModel.SetError("changesetId 不能为空（传 changesetId 或 sessionId）");
        return;
      }
      try
      {
        int deleted = _changeSetEngine.DedupItems(changesetId);
        responseModel.SetData(new { success = true, deleted = deleted });
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A15 merge：合并所有 DRAFT 变更项为一条统一变更项（按会话合并为一条）。
    /// 入参：{sessionId} 或 {changesetId}。返回 {mergedId}。
    /// 合并后原 DRAFT 项标记为 MERGED，导出/执行只取合并后的 CONFIRMED 项。
    /// </summary>
    private void doMerge(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      if (string.IsNullOrEmpty(changesetId))
      {
        string sessionId = Params["sessionId"] as string;
        if (!string.IsNullOrEmpty(sessionId))
        {
          DBHelper h = DB.GetDBHelper();
          using (h)
          {
            changesetId = h.QueryFirstOrDefault<string>(
              "SELECT CHANGESETID FROM tss_aidev_session WHERE ID=@sid AND ISDELETED=0",
              new { sid = sessionId });
          }
        }
      }
      if (string.IsNullOrEmpty(changesetId))
      {
        responseModel.SetError("changesetId 不能为空（传 changesetId 或 sessionId）");
        return;
      }
      try
      {
        string userId = HttpContext?.User?.Identity?.Name ?? "system";
        string mergedId = _changeSetEngine.MergeItems(changesetId, userId);
        responseModel.SetData(new { success = true, mergedId = mergedId });
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A16 listItems：按 changesetId 查变更项列表（含 DRAFT/CONFIRMED/MERGED/REJECTED 全部，前端按状态展示）。
    /// 入参：{changesetId}。返回 {Items:[...], TotalCount}，字段大写与前端一致。
    /// 不走标准 A01（VSS_AIDEV_CHANGEITEM 未挂模块路径），用直接 SQL 查。
    /// </summary>
    private void doListItems(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      if (string.IsNullOrEmpty(changesetId))
      {
        responseModel.SetError("changesetId 不能为空");
        return;
      }
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var items = helper.Query(
            @"SELECT ID, CHANGESETID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET,
                     SQLCONTENT, METADATA, RATIONALE, WARNINGS, DEPENDSON,
                     ITEMSTATUS, CONFIRMEDBY, CONFIRMEDTIME, CONFIRMORDER, ISDELETED
              FROM tss_aidev_changeitem
              WHERE CHANGESETID=@csid AND ISDELETED=0
              ORDER BY ITEMSEQ",
            new { csid = changesetId }).ToList();
          // 转 List<object> 便于 JSON 序列化（Dapper dynamic -> 匿名对象，字段名小写问题用别名修正）
          var list = new List<object>();
          foreach (dynamic d in items)
          {
            list.Add(new
            {
              ID = (string)d.ID,
              CHANGESETID = (string)d.CHANGESETID,
              ITEMSEQ = (int)d.ITEMSEQ,
              CATEGORY = (string)d.CATEGORY,
              ACTION = (string)d.ACTION,
              TOOL = (string)d.TOOL ?? "",
              TARGET = (string)d.TARGET ?? "",
              SQLCONTENT = (string)d.SQLCONTENT ?? "",
              METADATA = (string)d.METADATA ?? "",
              RATIONALE = (string)d.RATIONALE ?? "",
              WARNINGS = (string)d.WARNINGS ?? "",
              DEPENDSON = d.DEPENDSON == null ? "" : (string)d.DEPENDSON,
              ITEMSTATUS = (string)d.ITEMSTATUS,
              CONFIRMEDBY = d.CONFIRMEDBY == null ? "" : (string)d.CONFIRMEDBY,
              CONFIRMEDTIME = d.CONFIRMEDTIME == null ? null : (object)d.CONFIRMEDTIME,
              CONFIRMORDER = d.CONFIRMORDER == null ? (int?)null : (int)d.CONFIRMORDER,
              ISDELETED = (int)d.ISDELETED
            });
          }
          responseModel.SetData(new { Items = list, TotalCount = list.Count });
        }
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A17 executeConfirmed：执行已确认脚本（开发环境直接落库，调试用）。
    /// 入参：{sessionId} 或 {changesetId}。把所有 CONFIRMED 项 SQL 按顺序在单事务里执行。
    /// </summary>
    private void doExecuteConfirmed(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      if (string.IsNullOrEmpty(changesetId))
      {
        string sessionId = Params["sessionId"] as string;
        if (!string.IsNullOrEmpty(sessionId))
        {
          DBHelper h = DB.GetDBHelper();
          using (h)
          {
            changesetId = h.QueryFirstOrDefault<string>(
              "SELECT CHANGESETID FROM tss_aidev_session WHERE ID=@sid AND ISDELETED=0",
              new { sid = sessionId });
          }
        }
      }
      if (string.IsNullOrEmpty(changesetId))
      {
        responseModel.SetError("changesetId 不能为空（传 changesetId 或 sessionId）");
        return;
      }
      string userId = Params["executedBy"] as string;
      try
      {
        var result = _changeSetEngine.ExecuteConfirmed(changesetId, userId);
        responseModel.SetData(result);
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A18 getConversation：加载历史对话（重新打开工作区时显示之前的对话）。
    /// 入参：{sessionId}。返回 CONVERSATION JSON 字符串（[{role,content,ts}]）。
    /// </summary>
    private void doGetConversation(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string sessionId = Params["sessionId"] as string;
      if (string.IsNullOrEmpty(sessionId))
      {
        responseModel.SetError("sessionId 不能为空");
        return;
      }
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          string json = helper.QueryFirstOrDefault<string>(
            "SELECT CONVERSATION FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0",
            new { id = sessionId }) ?? "";
          responseModel.SetData(new { conversation = json });
        }
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// A19 openWizardSession：创建模块向导专用会话 + 变更包。
    /// 向导 6 步共享同一个 sessionId/changesetId，跨步骤时序由 AssistantToolExecutor 的 DRAFT 兜底修复。
    /// 返回 {sessionId, changesetId, sessionCode}。
    /// </summary>
    private void doOpenWizardSession(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          string sessionId = Guid.NewGuid().ToString("N");
          string sessionCode = "WZ" + DateTime.Now.ToString("yyyyMMddHHmmss");
          string changesetId = "cs_" + sessionCode + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
          string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

          // 1. 创建会话（SESSIONTYPE=WIZARD_NEW 区分于整模块生成的 NEW/MODIFY）
          helper.Execute(
            @"INSERT INTO tss_aidev_session (ID, SESSIONCODE, SESSIONNAME, SESSIONTYPE, TARGETMODULE, INTENT, STATUS, CREATEDBY, CREATEDTIME, CHANGESETID, ISDELETED)
              VALUES (@id, @code, @name, 'WIZARD_NEW', '', '模块向导分步创建', 'DRAFT', @uid, @tm, @csid, 0)",
            new { id = sessionId, code = sessionCode, name = "模块向导-" + sessionCode, uid = userId, tm = now, csid = changesetId });

          // 2. 创建变更包（6 步的 DRAFT 变更项都挂这里）
          helper.Execute(
            @"INSERT INTO tss_aidev_changeset (ID, SESSIONID, CHANGESETCODE, TITLE, SOURCE, INTENT, VALIDATIONPASSED, VALIDATIONREPORT, ITEMCOUNT, CREATEDTIME, ISDELETED)
              VALUES (@id, @sid, @code, @title, 'wizard', '模块向导分步创建', 0, NULL, 0, @tm, 0)",
            new { id = changesetId, sid = sessionId, code = changesetId, title = "模块向导变更包-" + sessionCode, tm = now });

          responseModel.SetData(new { sessionId, changesetId, sessionCode });
        }
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
      }
    }

    /// <summary>
    /// 流式生成接口（SSE）。
    /// 不走 doMyApi（Call 框架不适合 SSE），独立 action。
    /// 路由：api/RMAIDev/generate-stream（Controller 已有 [Route("api/[controller]")]）。
    /// 风格对齐 AssistantController.Send：writeLock 串行化 + 15s 心跳 + finally 写 done。
    /// 事件类型：text/tool_call/tool_result/item/validate/error/done。
    /// </summary>
    [HttpPost("generate-stream")]
    public async Task GenerateStream([FromForm] string sessionId, [FromForm] string message)
    {
      // userInfo 由 _userInfo_ 表单字段经 HashtableBinder 绑定（键 ID / NICKNAME）
      string userId = this.userInfo != null ? (this.userInfo["ID"] + "") : "anonymous";

      Response.ContentType = "text/event-stream";
      Response.Headers["Cache-Control"] = "no-cache";
      Response.Headers["X-Accel-Buffering"] = "no";
      // 流式响应：CORS 头必须在写 body 前显式设置。
      var origin = Request.Headers["Origin"].ToString();
      if (!string.IsNullOrEmpty(origin))
      {
        Response.Headers["Access-Control-Allow-Origin"] = origin;
        Response.Headers["Access-Control-Allow-Credentials"] = "true";
      }

      // 并发安全：所有 Response 写入串行化（心跳 vs 内容）
      var writeLock = new SemaphoreSlim(1, 1);
      async Task Write(object block)
      {
        await writeLock.WaitAsync();
        try
        {
          await Response.WriteAsync(SseWriter.Frame(block));
          await Response.Body.FlushAsync();
        }
        finally
        {
          writeLock.Release();
        }
      }

      // 心跳保活（15s）
      var cts = new CancellationTokenSource();
      var heartbeat = Task.Run(async () =>
      {
        while (!cts.Token.IsCancellationRequested)
        {
          try { await Task.Delay(15000, cts.Token); } catch { break; }
          if (!cts.Token.IsCancellationRequested)
          {
            await Write(new { type = "heartbeat" });
          }
        }
      });

      var sw = Stopwatch.StartNew();
      bool doneWritten = false;
      try
      {
        // 场景配额检查
        var aidevSceneCfg = SceneConfigService.GetScene("aidev");
        var quotaErr = SceneConfigService.CheckDailyQuota(aidevSceneCfg, "aidev");
        if (quotaErr != null)
        {
          await Write(new { type = "error", text = quotaErr });
          return;
        }

        if (string.IsNullOrEmpty(sessionId))
        {
          await Write(new { type = "error", text = "sessionId 不能为空" });
          return;
        }
        if (string.IsNullOrEmpty(message))
        {
          await Write(new { type = "error", text = "message 不能为空" });
          return;
        }

        // 推送流程步骤模板（前端初始化 todoList）
        string sessionType = "NEW";
        try
        {
          DBHelper helper = DB.GetDBHelper();
          using (helper)
          {
            var st = helper.QueryFirstOrDefault<string>(
              "SELECT SESSIONTYPE FROM tss_aidev_session WHERE ID=@id", new { id = sessionId });
            if (!string.IsNullOrEmpty(st)) sessionType = st;
          }
        }
        catch { /* 取不到默认 NEW */ }
        var steps = BuildStepTemplate(sessionType);
        await Write(new { type = "steps", steps });

        var result = await _orchestrator.GenerateAsync(
          sessionId, message, userId,
          onContent: async c => { await Write(new { type = "text", text = c }); },
          onToolCall: async (toolName, argsJson) =>
          {
            await Write(new { type = "tool_call", tool = toolName, args = argsJson });
          },
          onToolResult: async (toolName, resultSummary) =>
          {
            await Write(new { type = "tool_result", tool = toolName, summary = resultSummary });
          },
          onItem: async (item) =>
          {
            await Write(new
            {
              type = "item",
              item = new
              {
                id = item.ID,
                changesetId = item.CHANGESETID,
                seq = item.ITEMSEQ,
                category = item.CATEGORY,
                action = item.ACTION,
                tool = item.TOOL,
                target = item.TARGET,
                sqlContent = item.SQLCONTENT,
                metadata = item.METADATA,
                rationale = item.RATIONALE,
                warnings = item.WARNINGS,
                dependsOn = item.DEPENDSON,
                status = item.ITEMSTATUS
              }
            });
          },
          onValidate: async (report) =>
          {
            await Write(new { type = "validate", report });
          },
          onError: async (errorMsg) =>
          {
            await Write(new { type = "error", text = errorMsg });
          },
          onStep: async (stepKey, status, toolName) =>
          {
            await Write(new { type = "step", step = stepKey, status = status, tool = toolName });
          });

        // 最终写 done（携带 changesetId + newItemCount）
        await Write(new
        {
          type = "done",
          sessionId = result.SessionId,
          sessionCode = result.SessionCode,
          changeSetId = result.ChangeSetId,
          newItemCount = result.NewItems != null ? result.NewItems.Count : 0,
          warnings = result.Warnings,
          elapsedMs = sw.ElapsedMilliseconds
        });
        doneWritten = true;
      }
      catch (Exception ex)
      {
        await Write(new { type = "error", text = "生成失败：" + ex.Message });
      }
      finally
      {
        cts.Cancel();
        try { await heartbeat; } catch { }
        // 仅在未写过 done 时补写（异常路径或提前退出）
        if (!doneWritten) await Write(new { type = "done" });
      }
    }

    /// <summary>
    /// 模块向导一键生成全部 6 步（SSE）。
    /// 用户描述一次需求，后端按 step0->5 连续生成，事件带 step 字段标识当前步。
    /// 路由：api/RMAIDev/generate-all-stream
    /// </summary>
    [HttpPost("generate-all-stream")]
    public async Task GenerateAllStream([FromForm] string sessionId, [FromForm] string wizardContext, [FromForm] string message)
    {
      string userId = this.userInfo != null ? (this.userInfo["ID"] + "") : "anonymous";

      Response.ContentType = "text/event-stream";
      Response.Headers["Cache-Control"] = "no-cache";
      Response.Headers["X-Accel-Buffering"] = "no";
      var origin = Request.Headers["Origin"].ToString();
      if (!string.IsNullOrEmpty(origin))
      {
        Response.Headers["Access-Control-Allow-Origin"] = origin;
        Response.Headers["Access-Control-Allow-Credentials"] = "true";
      }

      var writeLock = new SemaphoreSlim(1, 1);
      async Task Write(object block)
      {
        await writeLock.WaitAsync();
        try
        {
          await Response.WriteAsync(SseWriter.Frame(block));
          await Response.Body.FlushAsync();
        }
        finally
        {
          writeLock.Release();
        }
      }

      var cts = new CancellationTokenSource();
      var heartbeat = Task.Run(async () =>
      {
        while (!cts.Token.IsCancellationRequested)
        {
          try { await Task.Delay(15000, cts.Token); } catch { break; }
          if (!cts.Token.IsCancellationRequested) await Write(new { type = "heartbeat" });
        }
      });

      var sw = Stopwatch.StartNew();
      bool doneWritten = false;
      try
      {
        if (string.IsNullOrEmpty(sessionId)) { await Write(new { type = "error", text = "sessionId 不能为空" }); return; }
        if (string.IsNullOrEmpty(message)) { await Write(new { type = "error", text = "message 不能为空" }); return; }

        var result = await _wizardOrchestrator.GenerateAllAsync(
          sessionId, wizardContext ?? "", message, userId,
          onStepStart: async step => { await Write(new { type = "step_start", step = step, label = stepLabel(step) }); },
          onContent: async (step, c) => { await Write(new { type = "text", step = step, text = c }); },
          onToolCall: async (step, toolName, argsJson) => { await Write(new { type = "tool_call", step = step, tool = toolName, args = argsJson }); },
          onToolResult: async (step, toolName, resultSummary) => { await Write(new { type = "tool_result", step = step, tool = toolName, summary = resultSummary }); },
          onItem: async (item) =>
          {
            await Write(new
            {
              type = "item",
              item = new
              {
                id = item.ID, changesetId = item.CHANGESETID, seq = item.ITEMSEQ,
                category = item.CATEGORY, action = item.ACTION, tool = item.TOOL, target = item.TARGET,
                sqlContent = item.SQLCONTENT, metadata = item.METADATA, rationale = item.RATIONALE,
                warnings = item.WARNINGS, dependsOn = item.DEPENDSON, status = item.ITEMSTATUS
              }
            });
          },
          onValidate: async (report) => { await Write(new { type = "validate", report }); },
          onError: async (errorMsg) => { await Write(new { type = "error", text = errorMsg }); });

        await Write(new
        {
          type = "done",
          sessionId = result.SessionId, sessionCode = result.SessionCode, changeSetId = result.ChangeSetId,
          newItemCount = result.NewItems != null ? result.NewItems.Count : 0,
          warnings = result.Warnings, elapsedMs = sw.ElapsedMilliseconds
        });
        doneWritten = true;
      }
      catch (Exception ex)
      {
        await Write(new { type = "error", text = "生成失败：" + ex.Message });
      }
      finally
      {
        cts.Cancel();
        try { await heartbeat; } catch { }
        if (!doneWritten) await Write(new { type = "done" });
      }
    }

    private static string stepLabel(int step)
    {
      var labels = new[] { "基本信息", "数据模型", "视图与查询", "接口配置", "UI配置", "菜单注册" };
      return step >= 0 && step < labels.Length ? labels[step] : ("第" + (step + 1) + "步");
    }

    /// <summary>
    /// 模块向导分步生成（SSE）。
    /// 复用 generate-stream 的 SSE 框架（writeLock 串行化 + 15s 心跳 + finally done），
    /// 但按向导当前步骤(step)只生成该步相关工具的变更项。
    /// 事件类型与 generate-stream 一致（不推 steps 模板，向导有自己的步骤条）。
    /// 路由：api/RMAIDev/generate-step-stream
    /// </summary>
    [HttpPost("generate-step-stream")]
    public async Task GenerateStepStream([FromForm] string sessionId, [FromForm] int step,
      [FromForm] string wizardContext, [FromForm] string message)
    {
      string userId = this.userInfo != null ? (this.userInfo["ID"] + "") : "anonymous";

      Response.ContentType = "text/event-stream";
      Response.Headers["Cache-Control"] = "no-cache";
      Response.Headers["X-Accel-Buffering"] = "no";
      var origin = Request.Headers["Origin"].ToString();
      if (!string.IsNullOrEmpty(origin))
      {
        Response.Headers["Access-Control-Allow-Origin"] = origin;
        Response.Headers["Access-Control-Allow-Credentials"] = "true";
      }

      var writeLock = new SemaphoreSlim(1, 1);
      async Task Write(object block)
      {
        await writeLock.WaitAsync();
        try
        {
          await Response.WriteAsync(SseWriter.Frame(block));
          await Response.Body.FlushAsync();
        }
        finally
        {
          writeLock.Release();
        }
      }

      var cts = new CancellationTokenSource();
      var heartbeat = Task.Run(async () =>
      {
        while (!cts.Token.IsCancellationRequested)
        {
          try { await Task.Delay(15000, cts.Token); } catch { break; }
          if (!cts.Token.IsCancellationRequested)
          {
            await Write(new { type = "heartbeat" });
          }
        }
      });

      var sw = Stopwatch.StartNew();
      bool doneWritten = false;
      try
      {
        // 场景配额检查
        var wizardSceneCfg = SceneConfigService.GetScene("wizard");
        var quotaErr = SceneConfigService.CheckDailyQuota(wizardSceneCfg, "wizard");
        if (quotaErr != null)
        {
          await Write(new { type = "error", text = quotaErr });
          return;
        }

        if (string.IsNullOrEmpty(sessionId))
        {
          await Write(new { type = "error", text = "sessionId 不能为空" });
          return;
        }
        if (string.IsNullOrEmpty(message))
        {
          await Write(new { type = "error", text = "message 不能为空" });
          return;
        }

        var result = await _wizardOrchestrator.GenerateStepAsync(
          sessionId, step, wizardContext ?? "", message, userId,
          onContent: async c => { await Write(new { type = "text", text = c }); },
          onToolCall: async (toolName, argsJson) =>
          {
            await Write(new { type = "tool_call", tool = toolName, args = argsJson });
          },
          onToolResult: async (toolName, resultSummary) =>
          {
            await Write(new { type = "tool_result", tool = toolName, summary = resultSummary });
          },
          onItem: async (item) =>
          {
            await Write(new
            {
              type = "item",
              item = new
              {
                id = item.ID,
                changesetId = item.CHANGESETID,
                seq = item.ITEMSEQ,
                category = item.CATEGORY,
                action = item.ACTION,
                tool = item.TOOL,
                target = item.TARGET,
                sqlContent = item.SQLCONTENT,
                metadata = item.METADATA,
                rationale = item.RATIONALE,
                warnings = item.WARNINGS,
                dependsOn = item.DEPENDSON,
                status = item.ITEMSTATUS
              }
            });
          },
          onValidate: async (report) =>
          {
            await Write(new { type = "validate", report });
          },
          onError: async (errorMsg) =>
          {
            await Write(new { type = "error", text = errorMsg });
          });

        await Write(new
        {
          type = "done",
          sessionId = result.SessionId,
          sessionCode = result.SessionCode,
          changeSetId = result.ChangeSetId,
          step = step,
          newItemCount = result.NewItems != null ? result.NewItems.Count : 0,
          warnings = result.Warnings,
          elapsedMs = sw.ElapsedMilliseconds
        });
        doneWritten = true;
      }
      catch (Exception ex)
      {
        await Write(new { type = "error", text = "生成失败：" + ex.Message });
      }
      finally
      {
        cts.Cancel();
        try { await heartbeat; } catch { }
        if (!doneWritten) await Write(new { type = "done" });
      }
    }

    /// <summary>
    /// 构建 todoList 步骤模板（NEW/MODIFY 两套）。
    /// 每个 step: { key, label, status }，初始 status=pending。
    /// key 与 Orchestrator.MapToolToStep 返回值对应，前端按 key 匹配 step 事件更新状态。
    /// </summary>
    private static List<object> BuildStepTemplate(string sessionType)
    {
      var steps = new List<object>();
      if (sessionType == "MODIFY")
      {
        steps.Add(new { key = "read_schema", label = "读模块/表结构", status = "pending" });
        steps.Add(new { key = "add_field", label = "加字段/改结构", status = "pending" });
        steps.Add(new { key = "create_view", label = "扩展视图字段", status = "pending" });
        steps.Add(new { key = "config_ui", label = "配置界面", status = "pending" });
        steps.Add(new { key = "create_dict", label = "创建字典", status = "pending" });
        steps.Add(new { key = "create_filter", label = "定义过滤器", status = "pending" });
        steps.Add(new { key = "create_api", label = "定义 API/按钮", status = "pending" });
        steps.Add(new { key = "create_funcpoints", label = "创建权限点", status = "pending" });
      }
      else
      {
        steps.Add(new { key = "check_resource", label = "检查资源复用", status = "pending" });
        steps.Add(new { key = "read_schema", label = "读表结构", status = "pending" });
        steps.Add(new { key = "create_table", label = "创建物理表", status = "pending" });
        steps.Add(new { key = "create_view", label = "定义视图", status = "pending" });
        steps.Add(new { key = "config_ui", label = "配置界面", status = "pending" });
        steps.Add(new { key = "create_dict", label = "创建字典", status = "pending" });
        steps.Add(new { key = "create_filter", label = "定义过滤器", status = "pending" });
        steps.Add(new { key = "register_module", label = "注册模块", status = "pending" });
        steps.Add(new { key = "create_api", label = "定义 API", status = "pending" });
        steps.Add(new { key = "create_menu", label = "创建菜单", status = "pending" });
        steps.Add(new { key = "create_funcpoints", label = "创建权限点", status = "pending" });
      }
      // 通用收尾步骤
      steps.Add(new { key = "validate", label = "校验变更包", status = "pending" });
      steps.Add(new { key = "confirm", label = "确认变更项", status = "pending" });
      steps.Add(new { key = "export", label = "导出脚本", status = "pending" });
      return steps;
    }

    /// <summary>
    /// A05 generate：触发一次 LLM 编排生成。
    /// 入参：{sessionId, message}
    /// 异步方法在同步 doMyApi 里用 GetAwaiter().GetResult() 阻塞等待。
    /// </summary>
    private void doGenerate(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string sessionId = Params["sessionId"] as string;
      string message = Params["message"] as string;
      if (string.IsNullOrEmpty(sessionId))
      {
        responseModel.SetError("sessionId 不能为空");
        return;
      }
      if (string.IsNullOrEmpty(message))
      {
        responseModel.SetError("message 不能为空");
        return;
      }

      try
      {
        var result = _orchestrator.GenerateAsync(sessionId, message, userId).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(result.Error))
        {
          responseModel.SetError(result.Error);
          return;
        }
        responseModel.SetData(new
        {
          sessionId = result.SessionId,
          sessionCode = result.SessionCode,
          changeSetId = result.ChangeSetId,
          conversation = result.Conversation,
          newItems = result.NewItems,
          newItemCount = result.NewItems != null ? result.NewItems.Count : 0,
          validationReport = result.ValidationReport,
          warnings = result.Warnings
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("生成失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A06 validate：校验变更包。
    /// 入参：{changesetId}
    /// </summary>
    private void doValidate(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      if (string.IsNullOrEmpty(changesetId))
      {
        responseModel.SetError("changesetId 不能为空");
        return;
      }
      try
      {
        var report = _changeSetEngine.ValidateChangeSet(changesetId);
        responseModel.SetData(new
        {
          passed = report.Passed,
          checkCount = report.Checks != null ? report.Checks.Count : 0,
          failCount = report.Checks != null ? report.Checks.FindAll(c => c.Status == ValidationCheck.STATUS_FAIL).Count : 0,
          warnCount = report.Checks != null ? report.Checks.FindAll(c => c.Status == ValidationCheck.STATUS_WARN).Count : 0,
          checks = report.Checks
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("校验失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A07 export：导出 .aidev.sql 脚本（冻结会话状态为 EXPORTED）。
    /// 入参：{sessionId}
    /// 返回：{script, sessionCode, frozen:true}
    /// </summary>
    private void doExport(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string sessionId = Params["sessionId"] as string;
      if (string.IsNullOrEmpty(sessionId))
      {
        responseModel.SetError("sessionId 不能为空");
        return;
      }
      try
      {
        string script = _exporter.Export(sessionId);
        responseModel.SetData(new
        {
          script,
          scriptLength = script.Length,
          frozen = true,
          hint = "会话状态已冻结为 EXPORTED，导出脚本可由 DBA 执行"
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("导出失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A09 confirm：确认变更项（DRAFT→CONFIRMED）。
    /// 入参：{itemId, userId?}
    /// </summary>
    private void doConfirm(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string itemId = Params["itemId"] as string;
      if (string.IsNullOrEmpty(itemId))
      {
        responseModel.SetError("itemId 不能为空");
        return;
      }
      try
      {
        _changeSetEngine.ConfirmItem(itemId, userId);
        responseModel.SetData(new { itemId, status = "CONFIRMED", confirmedBy = userId });
      }
      catch (Exception ex)
      {
        responseModel.SetError("确认失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A10 reject：拒绝变更项（DRAFT→REJECTED）。
    /// 入参：{itemId}
    /// </summary>
    private void doReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string itemId = Params["itemId"] as string;
      if (string.IsNullOrEmpty(itemId))
      {
        responseModel.SetError("itemId 不能为空");
        return;
      }
      try
      {
        _changeSetEngine.RejectItem(itemId);
        responseModel.SetData(new { itemId, status = "REJECTED" });
      }
      catch (Exception ex)
      {
        responseModel.SetError("拒绝失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A11 unconfirm：撤销确认（CONFIRMED→DRAFT）。
    /// 入参：{itemId}
    /// </summary>
    private void doUnconfirm(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string itemId = Params["itemId"] as string;
      if (string.IsNullOrEmpty(itemId))
      {
        responseModel.SetError("itemId 不能为空");
        return;
      }
      try
      {
        _changeSetEngine.UnconfirmItem(itemId);
        responseModel.SetData(new { itemId, status = "DRAFT" });
      }
      catch (Exception ex)
      {
        responseModel.SetError("撤销确认失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A12 getScript：获取已确认脚本（预览，不冻结会话）。
    /// 入参：{changesetId} 或 {sessionId}
    /// </summary>
    private void doGetScript(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string changesetId = Params["changesetId"] as string;
      string sessionId = Params["sessionId"] as string;

      try
      {
        string script;
        if (!string.IsNullOrEmpty(changesetId))
        {
          script = _changeSetEngine.GetConfirmedScript(changesetId);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
          script = _exporter.PreviewScript(sessionId);
        }
        else
        {
          responseModel.SetError("changesetId 或 sessionId 不能同时为空");
          return;
        }
        responseModel.SetData(new { script, scriptLength = script.Length });
      }
      catch (Exception ex)
      {
        responseModel.SetError("获取脚本失败：" + ex.Message);
      }
    }

    // ============================================================
    // AI 记忆中枢 - 反馈回流端点(2026-07-19)
    // 不走 doMyApi, 直接 JSON 交互(非 ORM 资源, 简化前端调用)
    // 用 ContentResult + JsonConvert 手动序列化(DataController 基类无 Json 方法)
    // ============================================================

    private IActionResult JsonOk(object data, int code = 200)
    {
      return new ContentResult
      {
        Content = JsonConvert.SerializeObject(data),
        ContentType = "application/json; charset=utf-8",
        StatusCode = code
      };
    }

    /// <summary>
    /// POST /api/RMAIDev/feedback
    /// 记录用户对 AI 输出的反馈(👍/👎/修正后/采纳)。
    /// 入参 JSON: {sessionId, sceneCode, assetType, feedbackType, userRequest, originalOutput, finalOutput, diffText, issueTags, qualityScore, comment}
    /// </summary>
    [HttpPost("feedback")]
    public IActionResult RecordFeedback([FromBody] FeedbackRequest req)
    {
      if (req == null || string.IsNullOrEmpty(req.feedbackType))
        return JsonOk(new { Code = 400, Message = "feedbackType 不能为空" }, 400);
      string userId = this.userInfo != null && this.userInfo["ID"] != null ? this.userInfo["ID"].ToString() : "";
      string userName = this.userInfo != null && this.userInfo["EMPNAME"] != null ? this.userInfo["EMPNAME"].ToString() : "";
      bool ok = MemoryService.RecordFeedback(new MemoryService.FeedbackRecord
      {
        SESSIONID = req.sessionId,
        SCENE_CODE = req.sceneCode,
        ASSETTYPE = req.assetType,
        USERID = userId,
        USERNAME = userName,
        FEEDBACK_TYPE = req.feedbackType,
        USER_REQUEST = req.userRequest,
        ORIGINAL_OUTPUT = req.originalOutput,
        FINAL_OUTPUT = req.finalOutput,
        DIFF_TEXT = req.diffText,
        ISSUE_TAGS = req.issueTags,
        QUALITY_SCORE = req.qualityScore,
        COMMENT = req.comment
      });
      return JsonOk(new { Code = ok ? 200 : 500, Message = ok ? "反馈已记录, 谢谢!" : "记录失败(可能表未迁移)" });
    }

    /// <summary>
    /// POST /api/RMAIDev/promote-example
    /// 把指定反馈提升为 example(写入 tss_ai_memory 供后续 LLM 检索)。
    /// 入参: {feedbackId}
    /// </summary>
    [HttpPost("promote-example")]
    public IActionResult PromoteExample([FromBody] PromoteRequest req)
    {
      if (req == null || string.IsNullOrEmpty(req.feedbackId))
        return JsonOk(new { Code = 400, Message = "feedbackId 不能为空" }, 400);
      bool ok = MemoryService.AdoptAsExample(req.feedbackId);
      return JsonOk(new { Code = ok ? 200 : 500, Message = ok ? "已提升为示例, 后续 AI 调用会优先采用" : "提升失败" });
    }

    /// <summary>手动失效 MemoryService 缓存(管理页保存后调用)</summary>
    [HttpPost("invalidate-memory")]
    public IActionResult InvalidateMemory()
    {
      MemoryService.Invalidate();
      return JsonOk(new { Code = 200, Message = "记忆缓存已刷新" });
    }

    /// <summary>手动失效工具定义缓存(配置中心工具页保存后调用)</summary>
    [HttpPost("invalidate-tool-cache")]
    public IActionResult InvalidateToolCache()
    {
      DeclarativeToolProvider.Invalidate();
      SceneConfigService.Invalidate();
      return JsonOk(new { Code = 200, Message = "工具缓存已刷新" });
    }

    /// <summary>测试 LLM 连接(AI 配置中心用): 用启用配置发一条 ping, 返回耗时/模型/结果</summary>
    [HttpPost("test-llm")]
    public async Task<IActionResult> TestLlm()
    {
      var cfg = _llmCfg.GetEnabled();
      if (cfg == null)
        return JsonOk(new { Code = 500, Message = "没有启用的 LLM 配置, 请先保存并启用" });
      var sw = System.Diagnostics.Stopwatch.StartNew();
      try
      {
        var payload = new
        {
          model = cfg.ModelName,
          messages = new object[] { new { role = "user", content = "ping, 请只回复: ok" } },
          stream = false,
          max_tokens = 8
        };
        var req = new HttpRequestMessage(HttpMethod.Post, cfg.BaseUrl.TrimEnd('/') + "/chat/completions");
        req.Headers.Add("Authorization", "Bearer " + cfg.ApiKeyPlain);
        req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        using (var http = new HttpClient())
        {
          http.Timeout = TimeSpan.FromSeconds(30);
          var resp = await http.SendAsync(req);
          var json = await resp.Content.ReadAsStringAsync();
          sw.Stop();
          if (!resp.IsSuccessStatusCode)
            return JsonOk(new { Code = 500, Message = "API 返回 " + (int)resp.StatusCode + "(" + sw.ElapsedMilliseconds + "ms): " + json });
          var jo = JObject.Parse(json);
          string reply = jo["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
          return JsonOk(new { Code = 200, Message = "连接成功", Data = new { provider = cfg.Provider, model = cfg.ModelName, ms = sw.ElapsedMilliseconds, reply } });
        }
      }
      catch (Exception ex)
      {
        sw.Stop();
        return JsonOk(new { Code = 500, Message = "连接失败(" + sw.ElapsedMilliseconds + "ms): " + ex.Message });
      }
    }

    public class FeedbackRequest
    {
      public string sessionId { get; set; }
      public string sceneCode { get; set; }
      public string assetType { get; set; }
      public string feedbackType { get; set; }
      public string userRequest { get; set; }
      public string originalOutput { get; set; }
      public string finalOutput { get; set; }
      public string diffText { get; set; }
      public string issueTags { get; set; }
      public int? qualityScore { get; set; }
      public string comment { get; set; }
    }
    public class PromoteRequest
    {
      public string feedbackId { get; set; }
    }
  }
}
