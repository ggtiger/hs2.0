using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 统一 LLM 客户端（合并 DeepSeekClient + VisionClient）。
  /// 两者都是 OpenAI 兼容的 /chat/completions 端点，只是调用模式不同：
  ///   - StreamChatAsync：流式（stream=true），逐 content delta 回调，支持 function calling。文本场景。
  ///   - AnalyzeImageAsync：非流式（stream=false），多模态（text+image_url）。视觉场景。
  /// 共享单例 HttpClient（替代原来两个各 new HttpClient）。
  /// 现有 DeepSeekClient/VisionClient 阶段 4 删除，过渡期保留。
  /// </summary>

  /// <summary>一次 LLM 调用的 token 用量（LLM 响应 usage 字段）</summary>
  public class LlmUsage
  {
    public int PromptTokens;
    public int CompletionTokens;
    public List<object> ToolCalls = new List<object>();
    public bool HasToolCalls => ToolCalls.Count > 0;
  }

  public class LlmClient
  {
    private readonly HttpClient _http;
    public LlmClient(HttpClient http) { _http = http; }

    /// <summary>
    /// 流式调用 chat/completions。逐 content delta 回调 onContent，流结束提取 usage + tool_calls。
    /// 替代 DeepSeekClient.StreamChatAsync(baseUrl,apiKey,model,...)，签名改为直接接收 LlmConfig。
    /// </summary>
    public async Task<LlmUsage> StreamChatAsync(LlmConfig cfg, object messages, object tools,
      Func<string, Task> onContent, Action<List<object>> onToolCalls = null)
    {
      var payload = new
      {
        model = cfg.ModelName,
        messages,
        stream = true,
        stream_options = new { include_usage = true },
        tools
      };
      var req = new HttpRequestMessage(HttpMethod.Post, cfg.BaseUrl.TrimEnd('/') + "/chat/completions");
      req.Headers.Add("Authorization", "Bearer " + cfg.ApiKeyPlain);
      req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

      var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
      resp.EnsureSuccessStatusCode();

      var usage = new LlmUsage();
      // tool_calls 按 index 累积，key=index，value=累积的 JObject
      var toolAccum = new Dictionary<int, JObject>();

      using (var stream = await resp.Content.ReadAsStreamAsync())
      using (var reader = new StreamReader(stream))
      {
        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
          if (!line.StartsWith("data:")) continue;   // 跳过空行 / keep-alive 注释行
          var data = line.Substring(5).Trim();
          if (data == "[DONE]") break;
          var chunk = JObject.Parse(data);

          // usage（通常在末尾 chunk；可能为 JSON null）
          var usageTok = chunk["usage"];
          if (usageTok != null && usageTok.Type == JTokenType.Object)
          {
            usage.PromptTokens = (int)(usageTok["prompt_tokens"] ?? 0);
            usage.CompletionTokens = (int)(usageTok["completion_tokens"] ?? 0);
          }

          // choices/delta（某些 chunk 的 choices 为 JSON null 或空数组）
          var choices = chunk["choices"];
          if (choices == null || choices.Type != JTokenType.Array) continue;
          var choicesArr = (JArray)choices;
          if (choicesArr.Count == 0) continue;
          var delta = choicesArr[0]["delta"];
          if (delta == null || delta.Type != JTokenType.Object) continue;

          var content = delta["content"]?.ToString();
          if (!string.IsNullOrEmpty(content)) await onContent(content);

          // tool_calls 按 index 累积：id/type/name 取首片，arguments 分片拼接
          var tcs = delta["tool_calls"];
          if (tcs is JArray)
          {
            foreach (var tc in (JArray)tcs)
            {
              var idx = (int)(tc["index"] ?? 0);
              if (!toolAccum.ContainsKey(idx))
              {
                var o = new JObject();
                if (tc["id"] != null) o["id"] = tc["id"];
                // type 缺失时补默认 "function"，否则下一轮 LLM 调用 messages 格式错误
                if (tc["type"] != null) o["type"] = tc["type"];
                else o["type"] = "function";
                var fn = new JObject();
                if (tc["function"]?["name"] != null) fn["name"] = tc["function"]["name"];
                fn["arguments"] = "";
                o["function"] = fn;
                toolAccum[idx] = o;
              }
              var argFrag = tc["function"]?["arguments"]?.ToString();
              if (!string.IsNullOrEmpty(argFrag))
              {
                toolAccum[idx]["function"]["arguments"] = (string)toolAccum[idx]["function"]["arguments"] + argFrag;
              }
            }
          }
        }
      }

      if (toolAccum.Count > 0)
      {
        for (int i = 0; i < toolAccum.Count; i++)
          if (toolAccum.ContainsKey(i)) usage.ToolCalls.Add(toolAccum[i]);
        onToolCalls?.Invoke(usage.ToolCalls);
      }
      return usage;
    }

    /// <summary>
    /// 视觉识别：调多模态 LLM 识别图片内容，返回文字描述。非流式。
    /// 替代 VisionClient.AnalyzeAsync。
    /// </summary>
    /// <param name="cfg">视觉LLM配置（ISVISION=1）</param>
    /// <param name="base64Image">图片base64（不含data:前缀）</param>
    /// <param name="mimeType">图片MIME类型，如 image/png</param>
    /// <param name="prompt">识别指令（null 时用默认）</param>
    public async Task<string> AnalyzeImageAsync(LlmConfig cfg, string base64Image, string mimeType, string prompt = null)
    {
      if (cfg == null) return "⚠️ 未配置视觉LLM，请先在LLM配置页加一个ISVISION=1的模型";
      if (string.IsNullOrEmpty(base64Image)) return "⚠️ 图片数据为空";

      string dataUrl = "data:" + (mimeType ?? "image/png") + ";base64," + base64Image;
      var payload = new
      {
        model = cfg.ModelName,
        messages = new object[]
        {
          new
          {
            role = "user",
            content = new object[]
            {
              new { type = "text", text = prompt ?? "请识别图片中的所有文字信息，按字段名:值的格式列出（如 客户名称:xxx，日期:xxx）。只返回识别到的内容，不要解释。" },
              new { type = "image_url", image_url = new { url = dataUrl } }
            }
          }
        },
        stream = false
      };

      var req = new HttpRequestMessage(HttpMethod.Post, cfg.BaseUrl.TrimEnd('/') + "/chat/completions");
      req.Headers.Add("Authorization", "Bearer " + cfg.ApiKeyPlain);
      req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

      var resp = await _http.SendAsync(req);
      var json = await resp.Content.ReadAsStringAsync();
      if (!resp.IsSuccessStatusCode)
      {
        return "⚠️ 视觉模型API返回 " + (int)resp.StatusCode + " " + resp.StatusCode + "：" + json;
      }
      var jo = JObject.Parse(json);
      return jo["choices"]?[0]?["message"]?["content"]?.ToString() ?? "（识别结果为空）";
    }
  }
}
