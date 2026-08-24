using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Realso.Core.Base;
using Realso.WebAPI.Services;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// 智能助理主 Controller。M1：/send 接收消息 → 调 DeepSeek 流式 → SSE 推 text → 记用量。
  /// 继承 BaseControl 以拿 this.userInfo（来自 _userInfo_ 表单字段，键 ID/NICKNAME）。
  /// 所有 Response 写入经 SemaphoreSlim 串行化（心跳与内容并发写会抛异常）。
  /// </summary>
  [Route("api/assistant")]
  [Authorize]
  public class AssistantController : BaseControl
  {
    private readonly LlmConfigService _cfg;
    private readonly LlmClient _llm;

    public AssistantController(LlmConfigService cfg, LlmClient llm)
    {
      _cfg = cfg;
      _llm = llm;
    }

    /// <summary>
    /// 前端工具结果回传（HTTP 方式，绕过 SignalR 单向半开问题）。
    /// 前端代理层执行完工具后，POST {callId, resultJson} 到此端点。
    /// 后端查找 FrontendToolCallStore 里的 callId，tcs.SetResult 唤醒 RunAgentLoop。
    /// </summary>
    [HttpPost("tool-result")]
    public IActionResult ToolResult([FromBody] ToolResultRequest req)
    {
      if (req == null || string.IsNullOrEmpty(req.CallId))
      {
        return Ok(new { Code = 200, Data = new { success = false, error = "callId不能为空" } });
      }
      bool found = FrontendToolCallStore.TrySetResult(req.CallId, req.ResultJson);
      Console.WriteLine($"[FrontendTool] (HTTP)tool-result callId={req.CallId} found={found}");
      return Ok(new { Code = 200, Data = new { success = found } });
    }

    public class ToolResultRequest
    {
      public string CallId { get; set; }
      public string ResultJson { get; set; }
    }

    /// <summary>
    /// AI 场景配置（tss_ai_scene）：前端 AiClient/aiAgentProxy 启动时拉取，
    /// 替代 isSignalRScene/registerForScene 的硬编码。60s 缓存 + 内置默认值回落。
    /// </summary>
    [HttpPost("scene-config")]
    public IActionResult SceneConfig()
    {
      return Ok(new { Code = 200, Data = SceneConfigService.GetAll() });
    }

    /// <summary>
    /// AI识别图片（HTTP方式，避免SignalR 32KB消息限制导致连接断开）。
    /// 前端粘贴图片后POST {base64Image, mimeType}，后端调视觉LLM识别返回文字。
    /// </summary>
    [HttpPost("analyze-image")]
    public async Task<IActionResult> AnalyzeImage([FromBody] AnalyzeImageRequest req)
    {
      if (req == null || string.IsNullOrEmpty(req.Base64Image))
      {
        return Ok(new { Code = 200, Data = new { success = false, error = "图片数据为空" } });
      }
      var visionCfg = _cfg.GetVision();
      if (visionCfg == null)
      {
        return Ok(new { Code = 200, Data = new { success = false, error = "未配置视觉LLM，请在LLM配置页加一个ISVISION=1的模型" } });
      }
      try
      {
        string text = await _llm.AnalyzeImageAsync(visionCfg, req.Base64Image, req.MimeType);
        bool ok = !text.StartsWith("⚠️");
        return Ok(new { Code = 200, Data = new { success = ok, text } });
      }
      catch (System.Exception e)
      {
        return Ok(new { Code = 200, Data = new { success = false, error = "识别失败：" + e.Message } });
      }
    }

    public class AnalyzeImageRequest
    {
      public string Base64Image { get; set; }
      public string MimeType { get; set; }
    }
  }
}
