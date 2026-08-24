using System;
using Realso.Data.DBAccess;
using Realso.WebAPI.Utils;

namespace Realso.WebAPI.Services
{
  /// <summary>启用的 LLM 配置</summary>
  public class LlmConfig
  {
    public string Provider;
    public string ApiKeyPlain;
    public string ModelName;
    public string BaseUrl;
    public string Params;
    public decimal PriceInput;
    public decimal PriceOutput;
    public int IsVision;
    /// <summary>降级模型ID（本模型不可用时回落到该模型配置）</summary>
    public string FallbackId;
  }
  public class LlmConfigRow
  {
    public Guid ID;
    public string PROVIDER;
    public string APIKEY;
    public string MODELNAME;
    public string BASEURL;
    public string PARAMS;
    public string FALLBACKID;
    public decimal PRICEINPUT;
    public decimal PRICEOUTPUT;
    public int ENABLED;
    public int ISVISION;
  }

  /// <summary>
  /// 读取启用的 LLM 配置。配置页走 ORM 元数据驱动，此处给助理读取启用的配置。
  /// APIKEY 在数据库中以 AES 加密存储（AesHelper + appsettings Assistant:AesKey），
  /// 此处解密返回明文 ApiKeyPlain 供 DeepSeekClient/VisionClient 发请求。
  /// 兼容已有明文数据：解密失败时兜底用明文（历史数据无需迁移）。
  /// </summary>
  public class LlmConfigService
  {
    private readonly string _aesKey;
    public LlmConfigService(string aesKey) { _aesKey = aesKey; }

    /// <summary>取启用的文本 LLM 配置（非视觉）；无则返回 null</summary>
    public LlmConfig GetEnabled()
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<LlmConfigRow>(
          "SELECT * FROM TBS_LLM_CONFIG WHERE ENABLED=1 AND ISDELETED=0 AND ISVISION=0 LIMIT 1");
        if (row == null) return null;
        return RowToConfig(row);
      }
    }

    /// <summary>取启用的视觉 LLM 配置（ISVISION=1，用于图片识别）；无则返回 null</summary>
    public LlmConfig GetVision()
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<LlmConfigRow>(
          "SELECT * FROM TBS_LLM_CONFIG WHERE ENABLED=1 AND ISDELETED=0 AND ISVISION=1 LIMIT 1");
        if (row == null) return null;
        return RowToConfig(row);
      }
    }

    /// <summary>按 ID 取 LLM 配置（场景级模型路由用）；无则返回 null</summary>
    public LlmConfig GetById(string id)
    {
      if (string.IsNullOrEmpty(id)) return null;
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<LlmConfigRow>(
          "SELECT * FROM TBS_LLM_CONFIG WHERE ID=@ID AND ISDELETED=0", new { ID = id });
        if (row == null) return null;
        return RowToConfig(row);
      }
    }

    /// <summary>
    /// 按场景配置取 LLM：场景指定 MODELID → 检查可用性(ENABLED=1,ISDELETED=0)
    /// → 不可用时沿 FALLBACKID 降级链(最多3级) → 全局默认。
    /// 防循环：visited set + 3 级上限。
    /// </summary>
    public LlmConfig GetByScene(SceneConfigService.AiScene scene)
    {
      if (scene != null && !string.IsNullOrEmpty(scene.MODELID))
      {
        var cfg = GetById(scene.MODELID);
        if (cfg != null) return cfg;
        // 模型不可用(不存在/已删/已禁用)，沿 FALLBACKID 降级
        var nextId = scene.MODELID;
        var visited = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
          if (!visited.Add(nextId)) break;  // 循环检测
          var fbId = GetFallbackId(nextId);
          if (string.IsNullOrEmpty(fbId)) break;
          if (!visited.Add(fbId)) break;
          var fallback = GetById(fbId);
          if (fallback != null) return fallback;
          nextId = fbId;
        }
      }
      return GetEnabled();
    }

    /// <summary>取指定模型配置的 FALLBACKID（仅查 FALLBACKID 列，轻量）</summary>
    private string GetFallbackId(string id)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<dynamic>(
          "SELECT FALLBACKID FROM TBS_LLM_CONFIG WHERE ID=@ID", new { ID = id });
        return row != null ? (string)row.FALLBACKID : null;
      }
    }

    /// <summary>加密 API Key（配置页保存时调用，写入 TBS_LLM_CONFIG.APIKEY）</summary>
    public string EncryptApiKey(string plain)
    {
      if (string.IsNullOrEmpty(plain)) return plain;
      if (string.IsNullOrEmpty(_aesKey)) return plain;  // 无密钥时不加密（兼容旧部署）
      try { return AesHelper.Encrypt(plain, _aesKey); }
      catch { return plain; }
    }

    /// <summary>
    /// API Key 脱敏回显（配置页密码框用）：保留前 3 位 + 后 4 位，中间用 **** 替代。
    /// 过短的 key 只保留前缀。静态方法，不依赖实例。
    /// </summary>
    public static string Mask(string apiKey)
    {
      if (string.IsNullOrEmpty(apiKey)) return "";
      if (apiKey.Length <= 7) return apiKey.Substring(0, Math.Min(3, apiKey.Length)) + "****";
      return apiKey.Substring(0, 3) + "****..." + apiKey.Substring(apiKey.Length - 4);
    }

    private LlmConfig RowToConfig(LlmConfigRow row)
    {
      return new LlmConfig
      {
        Provider = row.PROVIDER,
        ApiKeyPlain = DecryptApiKey(row.APIKEY),
        ModelName = row.MODELNAME,
        BaseUrl = row.BASEURL,
        Params = row.PARAMS,
        PriceInput = row.PRICEINPUT,
        PriceOutput = row.PRICEOUTPUT,
        IsVision = row.ISVISION,
        FallbackId = row.FALLBACKID
      };
    }

    /// <summary>
    /// 解密 API Key。解密失败兜底用明文（兼容历史明文数据，不强制迁移）。
    /// </summary>
    private string DecryptApiKey(string stored)
    {
      if (string.IsNullOrEmpty(stored)) return stored;
      if (string.IsNullOrEmpty(_aesKey)) return stored;
      // 明文 API Key（历史数据）解密会抛异常，兜底返回明文
      try { return AesHelper.Decrypt(stored, _aesKey); }
      catch { return stored; }
    }
  }
}
