using Realso.WebAPI.Services;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class UsageLoggerTests
  {
    [Fact]
    public void ComputeCost_InputPlusOutputPer1kTokens()
    {
      // PRICEINPUT/PRICEOUTPUT 单位：元/千token
      // 1000 输入@0.002 + 500 输出@0.008 = 0.002 + 0.004 = 0.006
      var cost = UsageLogger.ComputeCost(prompt: 1000, completion: 500, priceIn: 0.002m, priceOut: 0.008m);
      Assert.Equal(0.006m, cost);
    }

    [Fact]
    public void ComputeCost_ZeroTokens()
    {
      Assert.Equal(0m, UsageLogger.ComputeCost(0, 0, 0.002m, 0.008m));
    }
  }
}
