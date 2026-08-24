using System;
using System.Collections.Generic;

namespace Realso.Utils
{
  /// <summary>
  /// SQL 脚本工具：多语句拆分 / 注释判定 / DDL 黑名单检查。
  /// 供 UpgradeExecutor（升级包执行）与 DataController.doSqlApi（APITYPE=sql 脚本接口）共用，
  /// 避免两处各写一套拆分逻辑（原 SplitSqlStatements 从 UpgradeExecutor 抽取至此）。
  /// </summary>
  public static class SqlScriptHelper
  {
    /// <summary>
    /// 把 SQL 文本按分号拆成多条语句。
    /// 拆分前先剥掉注释（-- 行注释与 /* 块注释）：注释里的分号（如"-- 铁律: xxx; yyy"）
    /// 不会把语句切断，注释里的 @参数/DDL 关键字也不会干扰执行与检查。
    /// 简化处理：不解析字符串内的分号（SQL 模板禁止单引号，无字符串字面量场景）。
    /// </summary>
    public static List<string> SplitSqlStatements(string sql)
    {
      var result = new List<string>();
      if (string.IsNullOrEmpty(sql)) return result;
      // 先剥注释再拆分（注释中的分号不参与切分）
      string cleaned = StripComments(sql);
      var parts = cleaned.Split(';');
      foreach (var p in parts)
      {
        string s = p.Trim();
        if (string.IsNullOrEmpty(s)) continue;
        result.Add(s);
      }
      return result;
    }

    /// <summary>
    /// 剥掉 SQL 注释：-- 到行尾、/* 到 */（逐行处理，模板禁单引号故无需考虑字符串内命中）
    /// </summary>
    public static string StripComments(string sql)
    {
      if (string.IsNullOrEmpty(sql)) return sql;
      string normalized = sql.Replace("\r\n", "\n").Replace("\r", "\n");
      var lines = normalized.Split('\n');
      var sb = new System.Text.StringBuilder();
      bool inBlock = false;
      foreach (var rawLine in lines)
      {
        var line = rawLine;
        var out_ = new System.Text.StringBuilder();
        int i = 0;
        while (i < line.Length)
        {
          if (inBlock)
          {
            int end = line.IndexOf("*/", i, StringComparison.Ordinal);
            if (end < 0) { i = line.Length; continue; }
            inBlock = false;
            i = end + 2;
            continue;
          }
          // -- 行注释：到行尾全部丢弃
          if (i + 1 < line.Length && line[i] == '-' && line[i + 1] == '-') break;
          // /* 块注释起点
          if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
          {
            inBlock = true;
            i += 2;
            continue;
          }
          out_.Append(line[i]);
          i++;
        }
        sb.Append(out_.ToString());
        sb.Append('\n');
      }
      return sb.ToString();
    }

    /// <summary>
    /// 判断一段文本是否全是注释行（每行都以 -- 开头）。
    /// </summary>
    public static bool IsAllComment(string text)
    {
      var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
      foreach (var l in lines)
      {
        string t = l.Trim();
        if (string.IsNullOrEmpty(t)) continue;
        if (!t.StartsWith("--")) return false;
      }
      return true;
    }

    /// <summary>
    /// 判断是否幂等检查语句（SELECT @exec_count / SET @skip_script）。
    /// 这类语句在执行时跳过（不报错但不计入业务执行）。
    /// </summary>
    public static bool IsIdempotentCheck(string stmt)
    {
      string upper = stmt.ToUpperInvariant();
      return upper.Contains("@EXEC_COUNT") || upper.Contains("@SKIP_SCRIPT");
    }

    /// <summary>
    /// 判断语句是否 SELECT 查询（含 WITH 开头的 CTE）。
    /// </summary>
    public static bool IsSelect(string stmt)
    {
      string t = stmt.TrimStart();
      return t.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
          || t.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
          || t.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// DDL 黑名单关键字（脚本接口禁止执行）。
    /// 只拦首关键字级别，不做完整 SQL 解析（解析成本与误判率都不划算），
    /// 注入防控依赖执行层的参数化，不靠本检查。
    /// </summary>
    private static readonly string[] DDL_KEYWORDS = { "DROP", "ALTER", "TRUNCATE", "CREATE", "GRANT", "REVOKE", "RENAME" };

    /// <summary>
    /// 检查语句是否命中 DDL 黑名单，命中返回关键字，未命中返回 null。
    /// </summary>
    public static string MatchDdlKeyword(string stmt)
    {
      string t = stmt.TrimStart();
      foreach (var kw in DDL_KEYWORDS)
      {
        // 首单词匹配（DROP TABLE / ALTER TABLE ...），避免误伤 UPDATE ... SET NAME='CREATE' 之类
        if (t.StartsWith(kw + " ", StringComparison.OrdinalIgnoreCase)
         || t.StartsWith(kw + "\n", StringComparison.OrdinalIgnoreCase)
         || t.StartsWith(kw + "\t", StringComparison.OrdinalIgnoreCase))
        {
          return kw;
        }
      }
      return null;
    }

    /// <summary>
    /// 截断字符串到指定长度（防止数据库字段超长）。
    /// </summary>
    public static string Truncate(string s, int max)
    {
      if (string.IsNullOrEmpty(s)) return s;
      return s.Length <= max ? s : s.Substring(0, max);
    }
  }
}
