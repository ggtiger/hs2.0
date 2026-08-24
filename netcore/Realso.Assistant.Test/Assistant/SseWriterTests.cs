using Realso.WebAPI.Services;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class SseWriterTests
  {
    [Fact]
    public void Frame_CamelCaseDataLineWithDoubleNewline()
    {
      var sse = SseWriter.Frame(new { type = "text", text = "hello" });
      Assert.Equal("data: {\"type\":\"text\",\"text\":\"hello\"}\n\n", sse);
    }

    [Fact]
    public void FrameDone()
    {
      Assert.Equal("data: {\"type\":\"done\"}\n\n", SseWriter.FrameDone());
    }

    [Fact]
    public void FrameHeartbeat()
    {
      Assert.Contains("\"type\":\"heartbeat\"", SseWriter.FrameHeartbeat());
    }
  }
}
