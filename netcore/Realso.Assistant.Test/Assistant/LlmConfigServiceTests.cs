using Realso.WebAPI.Services;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class LlmConfigServiceTests
  {
    [Theory]
    [InlineData("sk-deepseek-abcdef123456", "sk-****...3456")]
    [InlineData("sk-ab", "sk-****")]       // 过短：保留前缀
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Mask_HidesMiddle(string input, string expected)
    {
      Assert.Equal(expected, LlmConfigService.Mask(input));
    }
  }
}
