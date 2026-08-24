using Realso.WebAPI.Utils;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class AesHelperTests
  {
    private const string Key = "0123456789abcdef0123456789abcdef"; // 32 字节

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginal()
    {
      var plain = "sk-deepseek-abcdef123456";
      var cipher = AesHelper.Encrypt(plain, Key);
      Assert.NotEqual(plain, cipher);
      Assert.Equal(plain, AesHelper.Decrypt(cipher, Key));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
      var cipher = AesHelper.Encrypt("secret", Key);
      Assert.ThrowsAny<System.Exception>(() => AesHelper.Decrypt(cipher, Key + "x"));
    }
  }
}
