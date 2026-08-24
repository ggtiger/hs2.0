using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 提示词版本（A/B 测试用）
  /// </summary>
  public class PromptVersion
  {
    public string Version;
    public string Content;
    public int Weight;
  }

  /// <summary>
  /// 提示词服务：从 TBS_ASSISTANT_PROMPT 读取提示词，带 30s MemoryCache。
  /// 支持同 PROMPTKEY 多版本 + 权重随机选择（A/B 测试）。
  /// 表里没有该 key 时用代码默认值兜底，保证系统可用。
  /// </summary>
  public class PromptService
  {
    private readonly IMemoryCache _cache;
    private static readonly Random _rng = new Random();
    // 代码默认值兜底（表里没数据时用）
    private static readonly ConcurrentDictionary<string, string> _defaults = new ConcurrentDictionary<string, string>();
    // 强制更新的 key（启动时覆盖数据库中的旧版本，用于代码 prompt 升级）
    private static readonly ConcurrentDictionary<string, string> _forceUpdate = new ConcurrentDictionary<string, string>();

    public PromptService(IMemoryCache cache)
    {
      _cache = cache;
    }

    /// <summary>
    /// 注册代码默认值（启动时从硬编码 prompt 注册，表里没数据时兜底）
    /// </summary>
    public static void RegisterDefault(string key, string content)
    {
      _defaults[key] = content;
    }

    /// <summary>
    /// 注册并强制更新（启动时覆盖数据库中的旧版本，用于代码 prompt 升级后同步）
    /// </summary>
    public static void RegisterDefaultForce(string key, string content)
    {
      _defaults[key] = content;
      _forceUpdate[key] = content;
    }

    /// <summary>
    /// 获取所有代码默认值的key（用于启动同步）
    /// </summary>
    public static IEnumerable<string> GetDefaultKeys()
    {
      return _defaults.Keys;
    }

    /// <summary>
    /// 启动时把代码默认值同步到数据库（数据库没有的key才INSERT，已有的不覆盖用户修改）
    /// 静态方法，在PromptDefaults.Register末尾调用，保证数据库和代码默认值一致
    /// </summary>
    public static void SyncDefaultsToDb()
    {
      foreach (var key in _defaults.Keys)
      {
        try
        {
          var existing = LoadFromDbStatic(key);
          if (string.IsNullOrEmpty(existing))
          {
            InsertToDbStatic(key, _defaults[key]);
          }
          else if (_forceUpdate.ContainsKey(key))
          {
            // 强制更新：代码 prompt 升级后覆盖数据库旧版本
            UpdateToDbStatic(key, _defaults[key]);
          }
        }
        catch { }
      }
      // 清空强制更新标记，只在本次启动生效
      _forceUpdate.Clear();
    }

    private static string LoadFromDbStatic(string key)
    {
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          return helper.QueryFirstOrDefault<string>(
            "SELECT CONTENT FROM TBS_ASSISTANT_PROMPT WHERE PROMPTKEY=@k AND ISDELETED=0 LIMIT 1",
            new { k = key });
        }
      }
      catch { return null; }
    }

    private static void InsertToDbStatic(string key, string content)
    {
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          helper.Execute(
            @"INSERT INTO TBS_ASSISTANT_PROMPT (ID, PROMPTKEY, CONTENT, DESCRIPTION, CREATETIME, ISDELETED)
              VALUES (@ID, @KEY, @C, @DESC, NOW(), 0)",
            new { ID = System.Guid.NewGuid().ToString("N"), KEY = key, C = content, DESC = GetDefaultDescription(key) });
        }
      }
      catch { }
    }

    private static void UpdateToDbStatic(string key, string content)
    {
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          helper.Execute(
            @"UPDATE TBS_ASSISTANT_PROMPT SET CONTENT=@C, DESCRIPTION=@DESC WHERE PROMPTKEY=@KEY AND ISDELETED=0",
            new { KEY = key, C = content, DESC = GetDefaultDescription(key) });
        }
      }
      catch { }
    }

    private static string GetDefaultDescription(string key)
    {
      if (key == "system_general") return "通用助理 system prompt";
      if (key == "system_form") return "表单填报 system prompt（含{moduleCode}/{currentDataPrompt}占位符）";
      if (key == "sfc_ai_system_prompt") return "SFC在线开发 AI代码助手 system prompt（含4种模板代码示例）";
      if (key == "meta_optimize_prompt") return "提示词优化 meta-prompt（AssistantHub.OptimizePrompt 用）";
      if (key == "vision_default_prompt") return "图片识别默认指令（VisionClient.AnalyzeImageAsync 用）";
      if (key.StartsWith("tool:")) return "工具描述：" + key.Substring(5);
      return "";
    }

    /// <summary>
    /// 读取提示词：MemoryCache(30s) → 查表(多版本加权随机) → 代码默认值兜底
    /// </summary>
    public string Get(string key)
    {
      if (string.IsNullOrEmpty(key)) return null;
      return _cache.GetOrCreate(key, entry =>
      {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        var versions = LoadVersionsFromDb(key);
        if (versions != null && versions.Count > 0)
        {
          var selected = WeightedSelect(versions);
          return selected.Content;
        }
        return _defaults.TryGetValue(key, out var d) ? d : null;
      });
    }

    /// <summary>
    /// 读取提示词及版本号（A/B 测试追踪用）
    /// </summary>
    public PromptVersion GetWithVersion(string key)
    {
      if (string.IsNullOrEmpty(key)) return null;
      var versions = LoadVersionsFromDb(key);
      if (versions != null && versions.Count > 0) return WeightedSelect(versions);
      if (_defaults.TryGetValue(key, out var d)) return new PromptVersion { Version = "default", Content = d, Weight = 100 };
      return null;
    }

    /// <summary>
    /// 读取提示词（不查缓存，强制刷新时用）
    /// </summary>
    public string GetFresh(string key)
    {
      var versions = LoadVersionsFromDb(key);
      if (versions != null && versions.Count > 0) return WeightedSelect(versions).Content;
      return _defaults.TryGetValue(key, out var d) ? d : null;
    }

    private List<PromptVersion> LoadVersionsFromDb(string key)
    {
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          return helper.Query<PromptVersion>(
            "SELECT VERSION AS Version, CONTENT AS Content, WEIGHT AS Weight FROM TBS_ASSISTANT_PROMPT WHERE PROMPTKEY=@k AND ISDELETED=0 AND WEIGHT>0",
            new { k = key })?.ToList();
        }
      }
      catch { return null; }
    }

    /// <summary>加权随机选择一个版本</summary>
    private PromptVersion WeightedSelect(List<PromptVersion> versions)
    {
      if (versions.Count == 1) return versions[0];
      int total = 0;
      foreach (var v in versions) total += v.Weight;
      int r = _rng.Next(total);
      int acc = 0;
      foreach (var v in versions)
      {
        acc += v.Weight;
        if (r < acc) return v;
      }
      return versions[versions.Count - 1];
    }

    /// <summary>
    /// 清除指定 key 的缓存（保存后调用，使修改立即生效）
    /// </summary>
    public void ClearCache(string key)
    {
      _cache.Remove(key);
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAll()
    {
      // IMemoryCache 没有清所有的接口，通过缩小过期时间让缓存自然失效
      // 保存提示词后调 ClearCache(具体key) 更精准
      if (_cache is MemoryCache mc && mc != null)
      {
        mc.Compact(100);
      }
    }
  }
}
