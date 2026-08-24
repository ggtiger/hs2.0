using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.WebAPI.Services;
using Realso.WebAPI.Services.Agent;

namespace Realso.WebAPI.Controllers
{
    /// <summary>
    /// SFC 在线开发 AI 代码助手 — SSE 流式生成代码
    /// 路由: api/RMSfcAi/generate-code
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class RMSfcAiController : DataController
    {
        private readonly LlmConfigService _llmConfig;
        private readonly PromptService _promptService;
        private readonly AgentEngine _engine;
        private readonly UsageLogger _usage;

        public RMSfcAiController(LlmConfigService llmConfig, PromptService promptService, AgentEngine engine, UsageLogger usage)
        {
            _llmConfig = llmConfig;
            _promptService = promptService;
            _engine = engine;
            _usage = usage;
        }

        /// <summary>
        /// AI 流式生成/修改 SFC 代码
        /// 入参: message(用户消息), context(JSON: {currentFile, siblingFiles, moduleCode})
        /// SSE 事件: text(流式文本) / error / done
        /// </summary>
        [HttpPost("generate-code")]
        public async Task GenerateCode([FromForm] string message, [FromForm] string context)
        {
            // ---------- SSE 响应头 ----------
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            var origin = Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin))
            {
                Response.Headers["Access-Control-Allow-Origin"] = origin;
                Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            // ---------- 并发写入锁 ----------
            var writeLock = new SemaphoreSlim(1, 1);
            async Task Write(object block)
            {
                await writeLock.WaitAsync();
                try
                {
                    await Response.WriteAsync(SseWriter.Frame(block));
                    await Response.Body.FlushAsync();
                }
                finally { writeLock.Release(); }
            }

            // ---------- 心跳 ----------
            var cts = new CancellationTokenSource();
            var heartbeat = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try { await Task.Delay(15000, cts.Token); } catch { break; }
                    if (!cts.Token.IsCancellationRequested)
                        await Write(new { type = "heartbeat" });
                }
            });

            bool doneWritten = false;
            try
            {
                // 场景配额检查
                var sfcSceneCfg = SceneConfigService.GetScene("sfc");
                var quotaErr = SceneConfigService.CheckDailyQuota(sfcSceneCfg, "sfc");
                if (quotaErr != null)
                {
                    await Write(new { type = "error", text = quotaErr });
                    return;
                }

                // ---------- 获取 LLM 配置（场景级模型路由） ----------
                var llm = _llmConfig.GetByScene(sfcSceneCfg);
                if (llm == null)
                {
                    await Write(new { type = "error", text = "未找到启用的 LLM 配置，请先在系统管理中配置 DeepSeek API" });
                    return;
                }

                // ---------- 解析上下文 ----------
                SfcAiContext ctx = ParseContext(context);

                // ---------- 构建 system prompt ----------
                string systemPrompt = BuildSystemPrompt(ctx);

                // ---------- 构建 messages ----------
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = BuildUserMessage(message, ctx) }
                };

                // ---------- tool calling 循环（迁移到 AgentEngine 统一 ReAct 循环）----------
                var tools = SfcAiToolExecutor.GetToolDefinitions();
                var executor = new SfcAiToolExecutor();

                // AiDevCallbackSink 适配 SSE 回调（AgentEngine 通过 sink 推 text/tool_call/tool_result/error 事件）
                var sink = new AiDevCallbackSink(
                    onContent: c => Write(new { type = "text", text = c }),
                    onToolCall: (tn, aj) => Write(new { type = "tool_call", tool = tn, args = aj }),
                    onToolResult: (tn, rs) => Write(new { type = "tool_result", tool = tn, summary = rs }),
                    onError: e => Write(new { type = "error", text = e })
                );

                var req = new AgentRunRequest
                {
                    Messages = messages,
                    Tools = tools,
                    Cfg = llm,
                    UserId = "",
                    UserName = "",
                    ConversationId = "SFC",
                    OperationType = "sfc",
                    Options = new AgentOptions
                    {
                        MaxSteps = 15,
                        MaxToolResultChars = 4000,
                        SummaryTruncateChars = 500,
                        EnableHeartbeat = false,
                        MaxStepsMessage = "已达最大工具调用步数({0})，请缩小任务范围或直接描述需求"
                    }
                };
                req.Options.ApplySceneParams(sfcSceneCfg?.PARAMS);

                // usage 汇总：循环结束 FlushAndLog 写一条汇总记录（替代原手动累加）
                var usageReporter = new AggregateUsageReporter(_usage);
                var runResult = await _engine.RunLoopAsync(req, sink, executor, usageReporter);

                // ---------- 完成（done 带 usage；AgentEngine 的 sink.OnDone 是空操作不冲突）----------
                await Write(new { type = "done", usage = new { promptTokens = runResult.Usage.Item1, completionTokens = runResult.Usage.Item2 } });
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

        // =================== 内部方法 ===================

        /// <summary>
        /// 获取模块元数据 schema — 供前端 AI 助手加载当前模块的字段/API/子表关系
        /// POST /api/RMSfcAi/get-module-schema
        /// </summary>
        [HttpPost("get-module-schema")]
        public IActionResult GetModuleSchema([FromForm] string moduleCode)
        {
            if (string.IsNullOrEmpty(moduleCode))
            {
                return Ok(new { Code = 400, Message = "moduleCode 不能为空" });
            }
            try
            {
                var schema = SfcModuleSchemaService.GetModuleSchema(moduleCode);
                return Ok(new { Code = 200, Data = schema, Message = "" });
            }
            catch (Exception ex)
            {
                return Ok(new { Code = 500, Message = "获取模块元数据失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 执行用户确认的元数据 SQL（仅允许 INSERT/UPDATE/DELETE，禁止 DROP/ALTER/TRUNCATE/CREATE）
        /// POST /api/RMSfcAi/execute-metadata-sql
        /// </summary>
        [HttpPost("execute-metadata-sql")]
        public IActionResult ExecuteMetadataSql([FromForm] string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return Ok(new { Code = 400, Message = "SQL 不能为空" });

            // 安全校验：禁止 DROP/ALTER/TRUNCATE/CREATE
            string upper = sql.Trim().ToUpper();
            if (upper.Contains("DROP ") || upper.Contains("ALTER ") || upper.Contains("TRUNCATE ") || upper.Contains("CREATE "))
                return Ok(new { Code = 400, Message = "安全限制：不允许执行 DROP/ALTER/TRUNCATE/CREATE 语句" });

            try
            {
                DBHelper helper = DB.GetDBHelper();
                int affected = helper.Execute(sql);
                return Ok(new { Code = 200, Data = new { affectedRows = affected }, Message = "执行成功" });
            }
            catch (Exception ex)
            {
                return Ok(new { Code = 500, Message = "SQL 执行失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 解析前端传入的 context JSON
        /// </summary>
        private SfcAiContext ParseContext(string contextJson)
        {
            if (string.IsNullOrEmpty(contextJson)) return new SfcAiContext();
            try
            {
                return JsonConvert.DeserializeObject<SfcAiContext>(contextJson)
                       ?? new SfcAiContext();
            }
            catch
            {
                return new SfcAiContext();
            }
        }

        /// <summary>
        /// 构建 System Prompt — 按 editTarget 选择（可扩展映射，新资产类型加一行即可）：
        ///   csharp → script_ai_cs_prompt / sql → script_ai_sql_prompt / js → script_ai_js_prompt
        ///   其余(sfc/extendjs/store 等) → sfc_ai_system_prompt
        /// 均可通过 RS_M16 提示词管理在线编辑，读不到时回落代码内置常量。
        /// </summary>
        private string BuildSystemPrompt(SfcAiContext ctx)
        {
          string target = (ctx != null ? ctx.editTarget : null) ?? "";
          string promptKey;
          string fallback;
          switch (target)
          {
            case "csharp":
              promptKey = "script_ai_cs_prompt"; fallback = ScriptAiPrompt.CSharp; break;
            case "sql":
              promptKey = "script_ai_sql_prompt"; fallback = ScriptAiPrompt.Sql; break;
            case "js":
              promptKey = "script_ai_js_prompt"; fallback = ScriptAiPrompt.Js; break;
            default:
              promptKey = "sfc_ai_system_prompt"; fallback = SfcAiPrompt.Content; break;
          }
          return _promptService.Get(promptKey) ?? fallback;
        }

        /// <summary>
        /// 构建用户消息（附带上下文信息）。
        /// 模块元数据不再在此注入，AI 通过工具调用(get_module_schema/get_module_pages/get_uiset)自行获取。
        /// editTarget 决定代码输出类型：extendjs(页面扩展mixin) / store(模块Store扩展) / sfc(独立SFC组件)。
        /// </summary>
        private string BuildUserMessage(string message, SfcAiContext ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine();
            // 编辑目标类型提示（让 AI 针对性生成代码）
            if (!string.IsNullOrEmpty(ctx.editTarget))
            {
                sb.AppendLine("## 编辑目标: " + ctx.editTarget);
                switch (ctx.editTarget)
                {
                    case "csharp":
                        sb.AppendLine("当前在编辑**API 脚本 (C#)**（Roslyn 运行时编译，保存即生效）。");
                        sb.AppendLine("纯顶层语句脚本，不写 namespace/class/Main；上下文用 P/Db/DbFirst/DbExec/Trans/Sql/Response。");
                        break;
                    case "sql":
                        sb.AppendLine("当前在编辑**SQL 模板**（NVelocity 引擎，Dapper 参数化执行，MySQL 语法）。");
                        sb.AppendLine("严禁单引号（用 @参数/CHAR(39)）；LIKE 用 CONCAT(CHAR(37),@P,CHAR(37))；禁 DDL。");
                        break;
                    case "js":
                        sb.AppendLine("当前在编辑**JS 模块**（扩展 JS / Store 扩展，纯 JS 文件，无 template/style）。");
                        sb.AppendLine("扩展 JS 导出 { methods, computed, data, init, mounted }；Store 扩展导出 { actions, mutations }。");
                        break;
                    case "extendjs":
                        sb.AppendLine("当前在编辑**页面扩展 JS**（动态 mixin，合并到 generic-module/generic-form 组件实例）。");
                        sb.AppendLine("代码必须 export default 一个对象，包含 methods/computed/init/mounted 等字段。");
                        sb.AppendLine("不能用 `<template>`/`<style>`，纯 JS 文件。");
                        if (!string.IsNullOrEmpty(ctx.pageCode))
                            sb.AppendLine("目标页面: " + ctx.pageCode);
                        break;
                    case "store":
                        sb.AppendLine("当前在编辑**模块 Store 扩展**（合并到模块的 Vuex 模块）。");
                        sb.AppendLine("代码必须 export default 一个对象，包含 actions/mutations 字段。");
                        sb.AppendLine("不能用 `<template>`/`<style>`，纯 JS 文件。");
                        sb.AppendLine("Store03 默认 actions(query/open/add/save/delete/submit/check/verify/batch/call) 已存在，只需写新增的。");
                        break;
                    case "sfc":
                    case "sfcmodulepath":
                        sb.AppendLine("当前在编辑**SFC 组件**（独立 Vue 单文件组件）。");
                        sb.AppendLine("代码必须是标准 SFC 格式: `<template>...</template><script>...</script><style>...</style>`。");
                        break;
                }
                sb.AppendLine();
            }
            if (ctx.currentFile != null)
            {
                sb.AppendLine("## 当前编辑文件");
                sb.AppendLine("路径: " + (ctx.currentFile.path ?? ""));
                sb.AppendLine("类型: " + (ctx.currentFile.type ?? "VUE"));
                sb.AppendLine("```" + GetLangCode(ctx.currentFile.type));
                sb.AppendLine(ctx.currentFile.content ?? "");
                sb.AppendLine("```");
                sb.AppendLine();
            }
            if (ctx.siblingFiles != null && ctx.siblingFiles.Count > 0)
            {
                sb.AppendLine("## 同模块同级文件");
                foreach (var f in ctx.siblingFiles)
                {
                    if (f.content == ctx.currentFile?.content) continue; // 跳过当前文件
                    sb.AppendLine("### " + (f.path ?? ""));
                    sb.AppendLine("```" + GetLangCode(f.type));
                    sb.AppendLine(f.content ?? "");
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }
            if (!string.IsNullOrEmpty(ctx.moduleCode))
            {
                sb.AppendLine("## 模块编码: " + ctx.moduleCode);
                sb.AppendLine("提示: 可用 get_module_schema / get_module_pages / get_uiset 工具获取此模块的真实元数据，基于工具返回的数据生成代码。");
            }
            sb.AppendLine();
            sb.AppendLine("请根据以上上下文，生成或修改代码。");
            return sb.ToString();
        }

        private string GetLangCode(string fileType)
        {
            if (string.IsNullOrEmpty(fileType)) return "vue";
            var ft = fileType.ToLower();
            if (ft == "js") return "javascript";
            if (ft == "csharp") return "csharp";
            if (ft == "sql") return "sql";
            return "vue";
        }

        // =================== 内部类型 ===================

        private class SfcAiContext
        {
            public SfcFileInfo currentFile { get; set; }
            public List<SfcFileInfo> siblingFiles { get; set; }
            public string moduleCode { get; set; }
            public string editTarget { get; set; } // extendjs/store/sfc/sfcmodulepath
            public string pageCode { get; set; }
        }

        private class SfcFileInfo
        {
            public string path { get; set; }
            public string type { get; set; }
            public string content { get; set; }
        }
    }
}
