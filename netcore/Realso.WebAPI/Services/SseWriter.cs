using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// SSE 消息块序列化。全局 JSON 用 DefaultContractResolver（PascalCase），
  /// 但前端期望块字段 camelCase（type/text/tool/args），故此处单独用 camelCase。
  /// 实际写出 + 并发加锁在 AssistantController 用 SemaphoreSlim 包裹。
  /// </summary>
  public static class SseWriter
  {
    private static readonly JsonSerializerSettings Camel = new JsonSerializerSettings
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver(),
      NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>把块对象序列化为 SSE 帧：data: {json}\n\n</summary>
    public static string Frame(object block)
    {
      var json = JsonConvert.SerializeObject(block, Camel);
      return "data: " + json + "\n\n";
    }

    public static string FrameDone()
    {
      return Frame(new { type = "done" });
    }

    public static string FrameHeartbeat()
    {
      return Frame(new { type = "heartbeat" });
    }
  }
}
