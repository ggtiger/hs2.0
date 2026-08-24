using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.WebAPI.Services;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class AssistantToolExecutorTests
  {
    [Fact]
    public void Execute_UnknownTool_ReturnsError()
    {
      var exec = new AssistantToolExecutor(new Hashtable());
      object result = exec.Execute("nonexistent_tool", new JObject());
      string json = JsonConvert.SerializeObject(result);
      Assert.Contains("未知工具", json);
    }

    [Fact]
    public void GetToolDefinitions_ContainsFourReadTools()
    {
      var defs = AssistantToolExecutor.GetToolDefinitions();
      Assert.True(defs.Count >= 4);
    }
  }
}
