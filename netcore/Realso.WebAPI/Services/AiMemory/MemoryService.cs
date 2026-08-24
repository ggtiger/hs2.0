using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Realso.Data.DBAccess;
using Realso.Utils;

namespace Realso.WebAPI.Services.AiMemory
{
  /// <summary>
  /// AI 统一记忆中枢服务(tss_ai_memory)。
  /// 职责：
  ///   1. 检索：按场景/向导步骤/资产类型/用户 prompt 关键词三级检索 rule/example/pitfall。
  ///   2. 注入：BuildMemoryPrompt 一站式构建 system prompt 增量段，由 AgentEngine/WizardStepOrchestrator 调用。
  ///   3. 回流：RecordFeedback 记录用户反馈到 tss_ai_feedback；AdoptAsExample 把反馈提升为 example。
  ///   4. 反馈统计：IncrementHitCount 批量增加命中次数(用于"越用越知哪条常用")。
  /// 缓存：60s 内 rule/glossary/pitfall 列表缓存(变更不频繁)。
  /// 容错：表不存在/查询失败返回空，不抛异常(避免拖垮 Agent 主流程)。
  /// </summary>
  public static class MemoryService
  {
    // ============== 数据结构 ==============

    public class MemoryItem
    {
      public string ID;
      public string MEMORYTYPE;   // rule / example / pitfall / glossary
      public string ASSETTYPE;    // sfc / sql / csharp / metadata / wizard / general
      public string TITLE;
      public string CONTENT;
      public string WRONG_CONTENT;
      public string FIX_STRATEGY;
      public string TAGS;
      public string SCENE_CODES;
      public string WIZARD_STEPS;
      public int PRIORITY;
      public int QUALITY_SCORE;
      public int HITCOUNT;
    }

    public class FeedbackRecord
    {
      public string SESSIONID;
      public string SCENE_CODE;
      public string ASSETTYPE;
      public string USERID;
      public string USERNAME;
      public string FEEDBACK_TYPE;   // thumbs_up / thumbs_down / edited / adopted
      public string USER_REQUEST;
      public string ORIGINAL_OUTPUT;
      public string FINAL_OUTPUT;
      public string DIFF_TEXT;
      public string ISSUE_TAGS;
      public int? QUALITY_SCORE;
      public string COMMENT;
    }

    // ============== 缓存 ==============
    private static List<MemoryItem> _allCache;
    private static DateTime _loadedAt = DateTime.MinValue;
    private static readonly object _lock = new object();
    private const int CACHE_SECONDS = 60;

    /// <summary>
    /// 全量记忆缓存(60s)。失败返回空列表(不抛异常)。
    /// </summary>
    public static List<MemoryItem> GetAll()
    {
      if (_allCache != null && (DateTime.Now - _loadedAt).TotalSeconds < CACHE_SECONDS) return _allCache;
      lock (_lock)
      {
        if (_allCache != null && (DateTime.Now - _loadedAt).TotalSeconds < CACHE_SECONDS) return _allCache;
        var list = new List<MemoryItem>();
        try
        {
          using (var helper = DB.GetDBHelper())
          {
            var rows = helper.Query<dynamic>(
              @"SELECT ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, WRONG_CONTENT, FIX_STRATEGY,
                       TAGS, SCENE_CODES, WIZARD_STEPS, PRIORITY, QUALITY_SCORE, HITCOUNT
                FROM tss_ai_memory
                WHERE ISDELETED=0
                ORDER BY PRIORITY DESC, HITCOUNT DESC, CREATETIME DESC");
            foreach (var r in rows)
            {
              list.Add(new MemoryItem
              {
                ID = (string)r.ID,
                MEMORYTYPE = (string)r.MEMORYTYPE ?? "",
                ASSETTYPE = (string)r.ASSETTYPE ?? "general",
                TITLE = (string)r.TITLE ?? "",
                CONTENT = (string)r.CONTENT ?? "",
                WRONG_CONTENT = (string)r.WRONG_CONTENT,
                FIX_STRATEGY = (string)r.FIX_STRATEGY,
                TAGS = (string)r.TAGS ?? "",
                SCENE_CODES = (string)r.SCENE_CODES ?? "",
                WIZARD_STEPS = (string)r.WIZARD_STEPS ?? "",
                PRIORITY = r.PRIORITY == null ? 3 : (int)r.PRIORITY,
                QUALITY_SCORE = r.QUALITY_SCORE == null ? 0 : (int)r.QUALITY_SCORE,
                HITCOUNT = r.HITCOUNT == null ? 0 : (int)r.HITCOUNT
              });
            }
          }
        }
        catch (Exception ex)
        {
          // 表未迁移/查询失败：Logger 记录后返回空(不影响主流程)
          Logger.Warn("MemoryService.GetAll 读取 tss_ai_memory 失败(可能未迁移 32_ai_memory.sql): " + ex.Message);
        }
        _allCache = list;
        _loadedAt = DateTime.Now;
        return _allCache;
      }
    }

    /// <summary>手动失效缓存(管理页保存后调用)</summary>
    public static void Invalidate()
    {
      lock (_lock) { _allCache = null; }
    }

    // ============== 检索：三级算法 ==============

    /// <summary>
    /// 第一级：硬规则(必注入)。匹配条件 ASSETTYPE 命中 + SCENE/WIZARD_STEP 不冲突 + PRIORITY>=4。
    /// </summary>
    public static List<MemoryItem> GetRules(string sceneCode, int? wizardStep, string assetType)
    {
      var all = GetAll();
      return all.Where(m => m.MEMORYTYPE == "rule"
                          && m.PRIORITY >= 4
                          && AssetMatch(m.ASSETTYPE, assetType)
                          && SceneMatch(m.SCENE_CODES, sceneCode)
                          && StepMatch(m.WIZARD_STEPS, wizardStep))
                .Take(15)
                .ToList();
    }

    /// <summary>
    /// 第二级：示例/术语(按 prompt 关键词匹配 TAGS/TITLE/CONTENT)。
    /// </summary>
    public static List<MemoryItem> GetByKeyword(string memoryType, string assetType, string userPrompt, int limit)
    {
      var all = GetAll();
      var keywords = Tokenize(userPrompt);
      if (keywords.Count == 0)
      {
        // 无关键词：返回高优先级 + 高评分
        return all.Where(m => m.MEMORYTYPE == memoryType && AssetMatch(m.ASSETTYPE, assetType))
                  .OrderByDescending(m => m.QUALITY_SCORE)
                  .ThenByDescending(m => m.HITCOUNT)
                  .Take(limit)
                  .ToList();
      }
      // 计分：TAGS 命中权重 3, TITLE 命中权重 2, CONTENT 命中权重 1
      var scored = all.Where(m => m.MEMORYTYPE == memoryType && AssetMatch(m.ASSETTYPE, assetType))
                      .Select(m => new
                      {
                        item = m,
                        score = keywords.Sum(k => (m.TAGS.Contains(k) ? 3 : 0)
                                                + (m.TITLE.Contains(k) ? 2 : 0)
                                                + (m.CONTENT.Contains(k) ? 1 : 0))
                      })
                      .Where(x => x.score > 0)
                      .OrderByDescending(x => x.score)
                      .ThenByDescending(x => x.item.HITCOUNT)
                      .Take(limit)
                      .Select(x => x.item)
                      .ToList();
      return scored;
    }

    /// <summary>
    /// 第三级：反模式触发(用户 prompt 命中 TAGS 关键词强制注入)。
    /// </summary>
    public static List<MemoryItem> GetPitfalls(string assetType, string userPrompt)
    {
      var all = GetAll();
      var keywords = Tokenize(userPrompt);
      if (keywords.Count == 0) return new List<MemoryItem>();
      return all.Where(m => m.MEMORYTYPE == "pitfall"
                          && AssetMatch(m.ASSETTYPE, assetType)
                          && keywords.Any(k => m.TAGS.Contains(k)))
                .Take(8)
                .ToList();
    }

    // ============== 注入：一站式构建 prompt 段 ==============

    /// <summary>
    /// 构建 system prompt 的"记忆注入段"。包含三块：
    ///   1. ⚙️ 项目铁律(rule)
    ///   2. 📚 相关示例(example)
    ///   3. ⚠️ 反模式提醒(pitfall)
    /// 返回空字符串表示无注入(调用方判断后再拼)。
    /// </summary>
    public static string BuildMemoryPrompt(string sceneCode, int? wizardStep, string assetType, string userPrompt)
    {
      try
      {
        var rules = GetRules(sceneCode, wizardStep, assetType);
        var examples = GetByKeyword("example", assetType, userPrompt, 3);
        var pitfalls = GetPitfalls(assetType, userPrompt);

        // 没有任何命中就不注入(节省 token)
        if (rules.Count == 0 && examples.Count == 0 && pitfalls.Count == 0) return "";

        // 记录命中(异步, 失败不影响主流程)
        var hitIds = rules.Concat(examples).Concat(pitfalls).Select(m => m.ID).Distinct().ToList();
        IncrementHitCount(hitIds);

        var sb = new StringBuilder();
        sb.Append("\n\n# 📌 项目记忆库(团队沉淀的铁律/示例/反模式, 必须遵守)\n");

        if (rules.Count > 0)
        {
          sb.Append("\n## ⚙️ 铁律(违反会导致运行时错误)\n");
          for (int i = 0; i < rules.Count; i++)
          {
            var m = rules[i];
            sb.Append((i + 1) + ". **" + m.TITLE + "**\n   " + Indent(m.CONTENT, "   ") + "\n");
          }
        }

        if (pitfalls.Count > 0)
        {
          sb.Append("\n## ⚠️ 反模式(本场景相关的已知踩坑, 必须避开)\n");
          foreach (var m in pitfalls)
          {
            sb.Append("- **" + m.TITLE + "**\n");
            sb.Append("  - 错误: " + TruncateOneLine(m.WRONG_CONTENT) + "\n");
            sb.Append("  - 修正: " + TruncateOneLine(m.FIX_STRATEGY) + "\n");
          }
        }

        if (examples.Count > 0)
        {
          sb.Append("\n## 📚 参考示例(同类问题用户验收过的写法)\n");
          foreach (var m in examples)
          {
            sb.Append("### " + m.TITLE + " (评分 " + m.QUALITY_SCORE + "/5)\n");
            sb.Append("```\n" + TruncateCode(m.CONTENT, 800) + "\n```\n");
          }
        }

        return sb.ToString();
      }
      catch (Exception ex)
      {
        Logger.Warn("MemoryService.BuildMemoryPrompt 失败(忽略, 不影响主流程): " + ex.Message);
        return "";
      }
    }

    // ============== 回流：反馈记录 ==============

    /// <summary>
    /// 记录用户反馈到 tss_ai_feedback。失败返回 false(调用方决定是否提示用户)。
    /// </summary>
    public static bool RecordFeedback(FeedbackRecord fb)
    {
      try
      {
        using (var helper = DB.GetDBHelper())
        {
          var id = Guid.NewGuid().ToString("N");
          helper.Execute(
            @"INSERT INTO tss_ai_feedback
              (ID, SESSIONID, SCENE_CODE, ASSETTYPE, USERID, USERNAME, FEEDBACK_TYPE,
               USER_REQUEST, ORIGINAL_OUTPUT, FINAL_OUTPUT, DIFF_TEXT, ISSUE_TAGS,
               QUALITY_SCORE, COMMENT, PROMOTED, CREATETIME)
              VALUES
              (@ID, @SESSIONID, @SCENE_CODE, @ASSETTYPE, @USERID, @USERNAME, @FEEDBACK_TYPE,
               @USER_REQUEST, @ORIGINAL_OUTPUT, @FINAL_OUTPUT, @DIFF_TEXT, @ISSUE_TAGS,
               @QUALITY_SCORE, @COMMENT, 0, NOW())",
            new
            {
              ID = id,
              SESSIONID = fb.SESSIONID ?? "",
              SCENE_CODE = fb.SCENE_CODE ?? "",
              ASSETTYPE = fb.ASSETTYPE ?? "",
              USERID = fb.USERID ?? "",
              USERNAME = fb.USERNAME ?? "",
              FEEDBACK_TYPE = fb.FEEDBACK_TYPE ?? "",
              USER_REQUEST = TruncateForDb(fb.USER_REQUEST, 2000),
              ORIGINAL_OUTPUT = TruncateForDb(fb.ORIGINAL_OUTPUT, 8000),
              FINAL_OUTPUT = TruncateForDb(fb.FINAL_OUTPUT, 8000),
              DIFF_TEXT = TruncateForDb(fb.DIFF_TEXT, 8000),
              ISSUE_TAGS = fb.ISSUE_TAGS ?? "",
              QUALITY_SCORE = fb.QUALITY_SCORE,
              COMMENT = TruncateForDb(fb.COMMENT, 2000)
            });
          return true;
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("MemoryService.RecordFeedback 失败: " + ex.Message);
        return false;
      }
    }

    /// <summary>
    /// 把反馈提升为 example(由用户手动触发或定时任务批量处理)。
    /// 操作：① 复制 FINAL_OUTPUT→tss_ai_memory(MEMORYTYPE=example) ② 标记 PROMOTED=1。
    /// </summary>
    public static bool AdoptAsExample(string feedbackId)
    {
      try
      {
        using (var helper = DB.GetDBHelper())
        {
          var row = helper.Query<dynamic>(
            "SELECT ID, SCENE_CODE, ASSETTYPE, USER_REQUEST, FINAL_OUTPUT, QUALITY_SCORE FROM tss_ai_feedback WHERE ID=@ID",
            new { ID = feedbackId }).FirstOrDefault();
          if (row == null) return false;
          if (string.IsNullOrEmpty((string)row.FINAL_OUTPUT)) return false;

          string newId = Guid.NewGuid().ToString("N");
          helper.Execute(
            @"INSERT INTO tss_ai_memory
              (ID, MEMORYTYPE, ASSETTYPE, TITLE, CONTENT, TAGS, SCENE_CODES, PRIORITY, QUALITY_SCORE, SOURCE, ISDELETED, CREATETIME)
              VALUES
              (@ID, 'example', @ASSETTYPE, @TITLE, @CONTENT, @TAGS, @SCENE_CODES, 3, @SCORE, 'feedback', 0, NOW())",
            new
            {
              ID = newId,
              ASSETTYPE = (string)row.ASSETTYPE ?? "general",
              TITLE = TruncateForDb(row.USER_REQUEST as string, 200) ?? "用户反馈示例",
              CONTENT = TruncateForDb(row.FINAL_OUTPUT as string, 16000),
              TAGS = "用户反馈,example",
              SCENE_CODES = (string)row.SCENE_CODE ?? "",
              SCORE = row.QUALITY_SCORE == null ? 4 : (int)row.QUALITY_SCORE
            });
          helper.Execute("UPDATE tss_ai_feedback SET PROMOTED=1 WHERE ID=@ID", new { ID = feedbackId });
        }
        Invalidate();
        return true;
      }
      catch (Exception ex)
      {
        Logger.Warn("MemoryService.AdoptAsExample 失败: " + ex.Message);
        return false;
      }
    }

    // ============== 内部工具 ==============

    private static bool AssetMatch(string memoAsset, string targetAsset)
    {
      if (string.IsNullOrEmpty(memoAsset) || memoAsset == "general") return true;
      if (string.IsNullOrEmpty(targetAsset)) return true;
      return memoAsset == targetAsset || memoAsset == "general";
    }

    private static bool SceneMatch(string memoScenes, string targetScene)
    {
      if (string.IsNullOrEmpty(memoScenes)) return true; // NULL=全局
      if (string.IsNullOrEmpty(targetScene)) return true;
      var set = memoScenes.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
      return set.Contains(targetScene);
    }

    private static bool StepMatch(string memoSteps, int? targetStep)
    {
      if (string.IsNullOrEmpty(memoSteps)) return true; // NULL=不限步骤
      if (!targetStep.HasValue) return true;
      var set = memoSteps.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
      return set.Contains(targetStep.Value.ToString());
    }

    /// <summary>简单中文分词：按标点/空格切 + 保留 2 字以上的有意义词</summary>
    private static List<string> Tokenize(string text)
    {
      if (string.IsNullOrEmpty(text)) return new List<string>();
      var result = new List<string>();
      // 按非汉字字母数字字符切分
      var parts = Regex.Split(text, @"[^\u4e00-\u9fa5a-zA-Z0-9_]+");
      foreach (var p in parts)
      {
        if (string.IsNullOrEmpty(p)) continue;
        if (p.Length >= 2) result.Add(p);
        // 长中文词再切 2 字滑窗
        if (p.Length >= 4 && Regex.IsMatch(p, @"[\u4e00-\u9fa5]"))
        {
          for (int i = 0; i < p.Length - 1; i++) result.Add(p.Substring(i, 2));
        }
      }
      return result.Distinct().ToList();
    }

    private static string Indent(string text, string pad)
    {
      if (string.IsNullOrEmpty(text)) return "";
      var lines = text.Split('\n');
      return string.Join("\n", lines.Select(l => pad + l.Trim()));
    }

    private static string TruncateOneLine(string s)
    {
      if (string.IsNullOrEmpty(s)) return "";
      var first = s.Split('\n').FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
      if (first.Length > 200) return first.Substring(0, 200) + "...";
      return first;
    }

    private static string TruncateCode(string s, int max)
    {
      if (string.IsNullOrEmpty(s)) return "";
      if (s.Length <= max) return s;
      return s.Substring(0, max) + "\n... (截断, 完整内容见记忆管理页)";
    }

    private static string TruncateForDb(string s, int max)
    {
      if (string.IsNullOrEmpty(s)) return null;
      if (s.Length <= max) return s;
      return s.Substring(0, max);
    }

    /// <summary>批量增加命中次数(异步友好, 失败忽略)</summary>
    private static void IncrementHitCount(List<string> ids)
    {
      if (ids == null || ids.Count == 0) return;
      try
      {
        using (var helper = DB.GetDBHelper())
        {
          var inClause = string.Join(",", ids.Select((id, i) => "@p" + i));
          var param = new Dictionary<string, object>();
          for (int i = 0; i < ids.Count; i++) param["p" + i] = ids[i];
          helper.Execute("UPDATE tss_ai_memory SET HITCOUNT=HITCOUNT+1 WHERE ID IN (" + inClause + ")", param);
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("MemoryService.IncrementHitCount 失败(忽略): " + ex.Message);
      }
    }
  }
}
