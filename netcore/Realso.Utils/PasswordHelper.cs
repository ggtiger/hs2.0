using System;
using System.Security.Cryptography;
using System.Text;

namespace Realso.Utils
{
  /// <summary>
  /// 密码哈希与临时Token生成工具
  /// </summary>
  public class PasswordHelper
  {
    // HMAC密钥，用于生成访问Token
    private static readonly string _hmacKey = ConfigHelper.GetConfig("ECert:TokenKey") ?? "HS2_ECERT_TOKEN_2026_SECURE_KEY";

    /// <summary>
    /// 对密码进行哈希，返回 salt$SHA256(salt+password) 格式
    /// </summary>
    public static string HashPassword(string password)
    {
      if (string.IsNullOrEmpty(password)) return null;
      byte[] saltBytes = new byte[16];
      using (var rng = RandomNumberGenerator.Create())
      {
        rng.GetBytes(saltBytes);
      }
      string salt = Convert.ToBase64String(saltBytes);
      string hash = ComputeSha256(salt + password);
      return salt + "$" + hash;
    }

    /// <summary>
    /// 验证密码是否匹配
    /// </summary>
    public static bool VerifyPassword(string password, string storedPassword)
    {
      if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword)) return false;
      int sepIndex = storedPassword.IndexOf('$');
      if (sepIndex < 0) return false;
      string salt = storedPassword.Substring(0, sepIndex);
      string storedHash = storedPassword.Substring(sepIndex + 1);
      string computedHash = ComputeSha256(salt + password);
      return computedHash == storedHash;
    }

    /// <summary>
    /// 生成临时访问Token（基于FILEID+小时时间戳+HMAC）
    /// Token在1小时内有效
    /// </summary>
    public static string GenerateAccessToken(string fileId)
    {
      if (string.IsNullOrEmpty(fileId)) return null;
      long hourTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600;
      string raw = fileId + ":" + hourTimestamp;
      string hmac = ComputeHmac(raw);
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw + ":" + hmac));
    }

    /// <summary>
    /// 验证访问Token是否有效（1小时内）
    /// </summary>
    public static bool VerifyAccessToken(string fileId, string token)
    {
      if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(token)) return false;
      try
      {
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        string[] parts = decoded.Split(':');
        if (parts.Length != 3) return false;
        string rawFileId = parts[0];
        long hourTimestamp = long.Parse(parts[1]);
        string hmac = parts[2];
        // 校验fileId匹配
        if (rawFileId != fileId) return false;
        // 校验时间（1小时内有效）
        long currentHour = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600;
        if (Math.Abs(currentHour - hourTimestamp) > 1) return false;
        // 校验HMAC
        string expectedHmac = ComputeHmac(rawFileId + ":" + hourTimestamp);
        return hmac == expectedHmac;
      }
      catch
      {
        return false;
      }
    }

    private static string ComputeSha256(string input)
    {
      using (var sha256 = SHA256.Create())
      {
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
          builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
      }
    }

    private static string ComputeHmac(string input)
    {
      using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey)))
      {
        byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
          builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
      }
    }
  }
}
