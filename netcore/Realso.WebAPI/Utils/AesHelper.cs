using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Realso.WebAPI.Utils
{
  /// <summary>
  /// AES-256-CBC 对称加解密，IV 前置拼接后 Base64 输出。用于 LLM API Key 等敏感配置的存储。
  /// </summary>
  public static class AesHelper
  {
    public static string Encrypt(string plain, string key)
    {
      using (var aes = Aes.Create())
      {
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.GenerateIV();
        using (var ms = new MemoryStream())
        {
          ms.Write(aes.IV, 0, aes.IV.Length);
          using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
          {
            var bytes = Encoding.UTF8.GetBytes(plain);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
          }
          return Convert.ToBase64String(ms.ToArray());
        }
      }
    }

    public static string Decrypt(string cipherBase64, string key)
    {
      var all = Convert.FromBase64String(cipherBase64);
      using (var aes = Aes.Create())
      {
        aes.Key = Encoding.UTF8.GetBytes(key);
        var iv = new byte[16];
        Array.Copy(all, 0, iv, 0, 16);
        aes.IV = iv;
        using (var cs = new CryptoStream(new MemoryStream(all, 16, all.Length - 16),
                   aes.CreateDecryptor(), CryptoStreamMode.Read))
        using (var sr = new StreamReader(cs, Encoding.UTF8))
        {
          return sr.ReadToEnd();
        }
      }
    }
  }
}
