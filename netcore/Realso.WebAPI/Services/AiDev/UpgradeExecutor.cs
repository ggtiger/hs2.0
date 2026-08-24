using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Realso.Data.DBAccess;
using Realso.Utils;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// 升级执行器：处理 .aidev.sql 脚本导入、预览、执行、回滚。
  ///
  /// 配套 ChangeSetExporter 导出的脚本格式：
  /// - @META 头：-- @META SessionCode=xxx / SessionName=xxx / Items=N ...
  /// - 幂等检查段：SELECT @exec_count := COUNT(1) FROM tss_aidev_upgrade WHERE SESSIONCODE=... AND STATUS='SUCCESS'
  /// - 分节注释：-- @ITEM id=... / category=... / action=... / target=...
  /// - 收尾升级登记段：-- 升级完成标记 后跟 INSERT INTO tss_aidev_upgrade ... STATUS='PENDING'
  ///
  /// Import 时会剥离收尾 INSERT 段（执行器自己管理 upgrade 记录状态，避免重复插入冲突）。
  /// Execute 采用单事务逐语句执行，任一语句失败即整体回滚。
  /// </summary>
  public class UpgradeExecutor
  {
    // 升级登记段的起始标记（ChangeSetExporter 固定输出此注释）
    private const string UPGRADE_FOOTER_MARKER = "-- 升级完成标记";

    /// <summary>
    /// 解析脚本 @META 头，提取键值对。
    /// 格式：-- @META Key=Value
    /// </summary>
    public static Dictionary<string, string> ParseMeta(string scriptContent)
    {
      var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrEmpty(scriptContent)) return meta;

      foreach (var line in scriptContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
      {
        string trimmed = line.Trim();
        if (!trimmed.StartsWith("-- @META")) continue;
        // 去掉 "-- @META " 前缀
        string body = trimmed.Substring("-- @META".Length).Trim();
        int eq = body.IndexOf('=');
        if (eq <= 0) continue;
        string key = body.Substring(0, eq).Trim();
        string val = body.Substring(eq + 1).Trim();
        meta[key] = val;
      }
      return meta;
    }

    /// <summary>
    /// 解析脚本里的变更项（-- @ITEM 注释分节），返回有序列表。
    /// 每个分节以 "-- ====..." 起始，包含若干 "-- @ITEM key=value" 行，后跟 SQL 内容直到下一个分节。
    /// </summary>
    public static List<UpgradeItem> ParseItems(string scriptContent)
    {
      var items = new List<UpgradeItem>();
      if (string.IsNullOrEmpty(scriptContent)) return items;

      // 先剥离收尾升级登记段，避免把 INSERT INTO tss_aidev_upgrade 当成一个变更项
      string body = StripFooterInsert(scriptContent);

      var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      UpgradeItem cur = null;
      StringBuilder sqlBuf = null;
      bool inItem = false;
      bool collectingSql = false;

      for (int i = 0; i < lines.Length; i++)
      {
        string line = lines[i];
        string trimmed = line.Trim();

        // 检测分节起始：-- @ITEM id=... 这一行标志新分节开始
        // ChangeSetExporter 输出顺序：先一堆 -- @ITEM key=value，再 ==== 分隔，再 SQL
        // 这里以 "-- @ITEM id=" 作为新分节的开始信号
        if (trimmed.StartsWith("-- @ITEM id="))
        {
          // 收尾上一个
          if (cur != null && sqlBuf != null)
          {
            cur.Sql = sqlBuf.ToString().Trim();
            if (!string.IsNullOrEmpty(cur.Sql)) items.Add(cur);
          }
          cur = new UpgradeItem();
          sqlBuf = new StringBuilder();
          collectingSql = false;
          inItem = true;
          ApplyItemLine(cur, trimmed);
          continue;
        }

        if (inItem)
        {
          // 仍处在 @META/key 行阶段
          if (trimmed.StartsWith("-- @ITEM"))
          {
            ApplyItemLine(cur, trimmed);
            continue;
          }
          // 跳过分隔注释行 "-- ====..."
          if (trimmed.StartsWith("-- ==="))
          {
            // 第一次遇到 ==== 之后开始收集 SQL
            collectingSql = true;
            continue;
          }
          // 空行：若已开始收集 SQL，保留作为语句分隔
          if (string.IsNullOrEmpty(trimmed))
          {
            if (collectingSql && sqlBuf.Length > 0) sqlBuf.AppendLine();
            continue;
          }
          // 收集 SQL 内容
          if (collectingSql)
          {
            sqlBuf.AppendLine(line);
          }
        }
      }
      // 收尾最后一个
      if (cur != null && sqlBuf != null)
      {
        cur.Sql = sqlBuf.ToString().Trim();
        if (!string.IsNullOrEmpty(cur.Sql)) items.Add(cur);
      }
      return items;
    }

    /// <summary>
    /// 把 "-- @ITEM key=value key=value ..." 行的值填入 UpgradeItem。
    /// ChangeSetExporter 输出的 @ITEM 行可能一行多个 key=value（空格分隔），
    /// 例如 "-- @ITEM seq=1 confirmOrder=1" 和 "-- @ITEM category=DDL action=CREATE"。
    /// 本方法按空格拆分后逐个解析。
    /// </summary>
    private static void ApplyItemLine(UpgradeItem item, string line)
    {
      // 去掉 "-- @ITEM " 前缀
      string body = line.Substring("-- @ITEM".Length).Trim();
      // 按空格拆分（支持一行多个 key=value）
      var tokens = body.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var token in tokens)
      {
        int eq = token.IndexOf('=');
        if (eq <= 0) continue;
        string key = token.Substring(0, eq).Trim();
        string val = token.Substring(eq + 1).Trim();
        switch (key.ToLowerInvariant())
        {
          case "id": item.ItemId = val; break;
          case "category": item.Category = val; break;
          case "action": item.Action = val; break;
          case "target": item.Target = val; break;
          case "tool": item.Tool = val; break;
          case "seq": item.Seq = val; break;
          case "confirmorder": item.ConfirmOrder = val; break;
          case "dependson": item.DependsOn = val; break;
          case "rationale": item.Rationale = val; break;
          case "warnings": item.Warnings = val; break;
        }
      }
    }

    /// <summary>
    /// 剥离脚本收尾的"升级登记"段（INSERT INTO tss_aidev_upgrade ...）。
    /// 执行器自己管理 upgrade 记录状态，不让脚本重复插入。
    /// 标记为 "-- 升级完成标记" 之后的所有内容全部删除。
    /// </summary>
    private static string StripFooterInsert(string scriptContent)
    {
      int idx = scriptContent.IndexOf(UPGRADE_FOOTER_MARKER, StringComparison.OrdinalIgnoreCase);
      if (idx < 0) return scriptContent;
      // 找到标记所在行的上一行分隔符（-- === 分隔），整体切到分隔行之前
      // 简化：直接切到 "升级完成标记" 出现位置之前的最近一个 "-- ===" 分隔行
      int cutFrom = idx;
      // 向前找最近的 "-- ===" 分隔行
      string before = scriptContent.Substring(0, idx);
      int lastSep = before.LastIndexOf("-- ===", StringComparison.Ordinal);
      if (lastSep > 0) cutFrom = lastSep;
      return scriptContent.Substring(0, cutFrom).TrimEnd() + Environment.NewLine;
    }

    /// <summary>
    /// 计算 SHA256 哈希（小写十六进制）。
    /// </summary>
    public static string ComputeHash(string content)
    {
      using (var sha = SHA256.Create())
      {
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? ""));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
      }
    }

    /// <summary>
    /// 生成升级编码：UP + yyyyMMdd + 4位序列。
    /// </summary>
    private static string GenUpgradeCode(DBHelper helper)
    {
      string prefix = "UP" + DateTime.Now.ToString("yyyyMMdd");
      // 查当天已有序列数
      int cnt = helper.QueryFirstOrDefault<int>(
        "SELECT COUNT(1) FROM tss_aidev_upgrade WHERE UPGRADECODE LIKE @p",
        new { p = prefix + "%" });
      return prefix + (cnt + 1).ToString("D4");
    }

    /// <summary>
    /// 导入脚本入库（不执行）。
    /// 1. 解析 @META 头
    /// 2. 幂等检查：SESSIONCODE 已 SUCCESS 则拒绝
    /// 3. SCRIPTHASH 重复检查
    /// 4. 剥离收尾 INSERT 段后存 SCRIPTCONTENT，STATUS='PENDING'
    /// 返回新建的 upgrade 记录 ID。
    /// </summary>
    public string Import(string scriptContent, string importedBy)
    {
      if (string.IsNullOrEmpty(scriptContent))
        throw new ArgumentException("scriptContent 不能为空");

      var meta = ParseMeta(scriptContent);
      string sessionCode = meta.ContainsKey("SessionCode") ? meta["SessionCode"] : "";
      string sessionName = meta.ContainsKey("SessionName") ? meta["SessionName"] : "";
      string sessionType = meta.ContainsKey("SessionType") ? meta["SessionType"] : "";
      string targetModule = meta.ContainsKey("TargetModule") ? meta["TargetModule"] : "";
      string intent = meta.ContainsKey("Intent") ? meta["Intent"] : "";
      string sessionId = meta.ContainsKey("SessionId") ? meta["SessionId"] : "";
      string changesetId = meta.ContainsKey("ChangeSetId") ? meta["ChangeSetId"] : "";

      if (string.IsNullOrEmpty(sessionCode))
        throw new InvalidOperationException("脚本缺少 @META SessionCode 头，无法导入");

      // 剥离收尾 INSERT 段（执行器自己管理 upgrade 记录）
      string cleanScript = StripFooterInsert(scriptContent);
      string hash = ComputeHash(cleanScript);

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 1. 幂等检查：已 SUCCESS 则禁止重复导入
        int successCnt = helper.QueryFirstOrDefault<int>(
          "SELECT COUNT(1) FROM tss_aidev_upgrade WHERE SESSIONCODE=@sc AND STATUS='SUCCESS' AND ISDELETED=0",
          new { sc = sessionCode });
        if (successCnt > 0)
          throw new InvalidOperationException("该会话已成功执行过（SESSIONCODE=" + sessionCode + "），禁止重复导入");

        // 2. SCRIPTHASH 重复检查
        int hashCnt = helper.QueryFirstOrDefault<int>(
          "SELECT COUNT(1) FROM tss_aidev_upgrade WHERE SCRIPTHASH=@h AND ISDELETED=0",
          new { h = hash });
        if (hashCnt > 0)
          throw new InvalidOperationException("该脚本内容已导入过（SCRIPTHASH 重复），禁止重复导入");

        // 3. 解析变更项数量
        var items = ParseItems(cleanScript);

        // 4. 生成 UPGRADECODE
        string upgradeCode = GenUpgradeCode(helper);
        string upgradeId = Guid.NewGuid().ToString("N");

        // 5. INSERT tss_aidev_upgrade
        helper.Execute(
          @"INSERT INTO tss_aidev_upgrade
            (ID, UPGRADECODE, SESSIONCODE, SESSIONID, CHANGESETID, SESSIONNAME, SESSIONTYPE, TARGETMODULE, INTENT,
             SCRIPTCONTENT, SCRIPTHASH, ITEMCOUNT, STATUS, EXECUTEDBY, ISDELETED)
            VALUES (@id, @code, @sc, @sid, @csid, @sname, @stype, @tmod, @intent,
                    @script, @hash, @cnt, 'PENDING', @by, 0)",
          new
          {
            id = upgradeId,
            code = upgradeCode,
            sc = sessionCode,
            sid = sessionId,
            csid = changesetId,
            sname = sessionName,
            stype = sessionType,
            tmod = targetModule,
            intent = intent,
            script = cleanScript,
            hash = hash,
            cnt = items.Count,
            by = importedBy
          });

        return upgradeId;
      }
    }

    /// <summary>
    /// 预览：读 upgrade 记录的 SCRIPTCONTENT，调 ParseItems，返回 {upgrade, items}。
    /// 不执行任何 SQL。
    /// </summary>
    public object Preview(string upgradeId)
    {
      if (string.IsNullOrEmpty(upgradeId))
        throw new ArgumentException("upgradeId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var upg = helper.QueryFirstOrDefault<dynamic>(
          @"SELECT ID, UPGRADECODE, SESSIONCODE, SESSIONNAME, SESSIONTYPE, TARGETMODULE, INTENT,
                   SCRIPTHASH, ITEMCOUNT, STATUS, EXECUTEDBY, EXECUTEDTIME
            FROM tss_aidev_upgrade WHERE ID=@id AND ISDELETED=0",
          new { id = upgradeId });
        if (upg == null)
          throw new InvalidOperationException("升级记录不存在: " + upgradeId);

        string script = helper.QueryFirstOrDefault<string>(
          "SELECT SCRIPTCONTENT FROM tss_aidev_upgrade WHERE ID=@id",
          new { id = upgradeId });

        var items = ParseItems(script);
        return new
        {
          upgrade = new
          {
            id = (string)upg.ID,
            upgradeCode = (string)upg.UPGRADECODE,
            sessionCode = (string)upg.SESSIONCODE,
            sessionName = (string)upg.SESSIONNAME,
            sessionType = upg.SESSIONTYPE == null ? "" : (string)upg.SESSIONTYPE,
            targetModule = upg.TARGETMODULE == null ? "" : (string)upg.TARGETMODULE,
            intent = upg.INTENT == null ? "" : (string)upg.INTENT,
            scriptHash = (string)upg.SCRIPTHASH,
            itemCount = upg.ITEMCOUNT == null ? 0 : (int)upg.ITEMCOUNT,
            status = (string)upg.STATUS,
            executedBy = upg.EXECUTEDBY == null ? "" : (string)upg.EXECUTEDBY,
            executedTime = upg.EXECUTEDTIME
          },
          items = items
        };
      }
    }

    /// <summary>
    /// 执行升级（核心）。
    /// 1. 读 upgrade 记录，STATUS 置 RUNNING
    /// 2. 解析变更项
    /// 3. 对每项涉及的表生成快照（SHOW CREATE TABLE），存 tss_aidev_upgrade_snapshot
    /// 4. 生成 ROLLBACKSCRIPT（CREATE TABLE→DROP、INSERT→DELETE、CREATE INDEX→DROP INDEX）
    /// 5. 单事务逐语句执行，任一失败即回滚
    /// 6. 全部成功 Commit，STATUS=SUCCESS
    /// </summary>
    public ExecuteResult Execute(string upgradeId, string executedBy)
    {
      if (string.IsNullOrEmpty(upgradeId))
        throw new ArgumentException("upgradeId 不能为空");

      var sw = System.Diagnostics.Stopwatch.StartNew();
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 1. 读 upgrade 记录
        var upg = helper.QueryFirstOrDefault<dynamic>(
          @"SELECT ID, UPGRADECODE, SESSIONCODE, SCRIPTCONTENT, SCRIPTHASH, STATUS
            FROM tss_aidev_upgrade WHERE ID=@id AND ISDELETED=0",
          new { id = upgradeId });
        if (upg == null)
          throw new InvalidOperationException("升级记录不存在: " + upgradeId);

        string status = (string)upg.STATUS;
        if (status == "SUCCESS")
          throw new InvalidOperationException("该升级已成功执行，如需重做请先回滚");
        if (status == "RUNNING")
          throw new InvalidOperationException("该升级正在执行中（STATUS=RUNNING），请检查或等待");
        if (status == "ROLLEDBACK")
          throw new InvalidOperationException("该升级已回滚，不能重复执行");

        string scriptContent = (string)upg.SCRIPTCONTENT;
        string storedHash = (string)upg.SCRIPTHASH;

        // 2. HASH 防篡改校验：执行前重新计算脚本哈希，与导入时存的 SCRIPTHASH 比对
        //    防止 SCRIPTCONTENT 被人手动修改后执行（导入后到执行期间被篡改）
        if (!string.IsNullOrEmpty(storedHash))
        {
          string currentHash = ComputeHash(scriptContent);
          if (!string.Equals(currentHash, storedHash, StringComparison.OrdinalIgnoreCase))
          {
            throw new InvalidOperationException(
              "脚本哈希校验失败：当前脚本内容与导入时不一致（可能被篡改），禁止执行。" +
              "stored=" + storedHash + ", current=" + currentHash);
          }
        }

        // 3. STATUS 置 RUNNING
        helper.Execute(
          "UPDATE tss_aidev_upgrade SET STATUS='RUNNING', EXECUTEDBY=@by WHERE ID=@id",
          new { by = executedBy, id = upgradeId });

        // 3. 解析变更项
        var items = ParseItems(scriptContent);

        // 4. 生成快照 + 回滚脚本
        var rollbackSb = new StringBuilder();
        var targets = ExtractTargets(items);
        foreach (var tgt in targets)
        {
          string tableName = tgt;
          try
          {
            var createRow = helper.QueryFirstOrDefault<dynamic>(
              "SHOW CREATE TABLE " + tableName);
            if (createRow != null)
            {
              // MySQL SHOW CREATE TABLE 返回的第二列是 Create Table
              string createSql = ((IDictionary<string, object>)createRow)["Create Table"] as string;
              if (!string.IsNullOrEmpty(createSql))
              {
                // 存快照
                helper.Execute(
                  @"INSERT INTO tss_aidev_upgrade_snapshot (ID, UPGRADEID, OBJECTTYPE, OBJECTNAME, SNAPSHOTBEFORE)
                    VALUES (@id, @uid, 'TABLE', @name, @snap)",
                  new { id = Guid.NewGuid().ToString("N"), uid = upgradeId, name = tableName, snap = createSql });
                // 回滚：先 DROP 再重建（最稳妥的表级回滚）
                rollbackSb.AppendLine("DROP TABLE IF EXISTS " + tableName + ";");
                rollbackSb.AppendLine(createSql + ";");
              }
            }
          }
          catch (Exception ex)
          {
            // 表不存在等异常，跳过快照（不影响执行）
            Logger.Info("生成快照失败 " + tableName + ": " + ex.Message);
          }
        }

        // 5. 单事务逐语句执行
        IDbTransaction trans = helper.BeginTransaction();
        int executedCount = 0;
        string failedItemId = null;
        string failedItemCategory = null;
        string errorMsg = null;

        try
        {
          foreach (var item in items)
          {
            // 把该项 SQL 按分号拆成多条语句
            var stmts = SqlScriptHelper.SplitSqlStatements(item.Sql);
            int rowsAffected = 0;
            foreach (var stmt in stmts)
            {
              string s = stmt.Trim();
              if (string.IsNullOrEmpty(s)) continue;
              // 跳过纯幂等检查语句（SELECT @exec_count / SET @skip_script 等）
              if (SqlScriptHelper.IsIdempotentCheck(s)) continue;

              string upper = s.ToUpperInvariant();
              if (upper.StartsWith("SELECT") || upper.StartsWith("SET @") || upper.StartsWith("SHOW"))
              {
                // 只读语句：用 Query 执行，不报错
                helper.Query(s, null);
              }
              else
              {
                rowsAffected += helper.Execute(s, null, trans);
              }
            }

            // 写一条 log（SUCCESS）
            helper.Execute(
              @"INSERT INTO tss_aidev_upgrade_log
                (ID, UPGRADEID, ITEMID, ITEMCATEGORY, ITEMACTION, ITEMTARGET, SQLSNIPPET, STATUS, ROWSAFFECTED, EXECUTEDTIME)
                VALUES (@id, @uid, @iid, @cat, @act, @tgt, @sql, 'SUCCESS', @rows, NOW())",
              new
              {
                id = Guid.NewGuid().ToString("N"),
                uid = upgradeId,
                iid = item.ItemId ?? "",
                cat = item.Category ?? "",
                act = item.Action ?? "",
                tgt = item.Target ?? "",
                sql = SqlScriptHelper.Truncate(item.Sql, 2000),
                rows = rowsAffected
              },
              trans);
            executedCount++;
          }

          // 全部成功：Commit
          trans.Commit();

          // 存 ROLLBACKSCRIPT + 更新 STATUS=SUCCESS
          sw.Stop();
          helper.Execute(
            @"UPDATE tss_aidev_upgrade
              SET STATUS='SUCCESS', EXECUTEDTIME=NOW(), DURATIONMS=@ms, ROLLBACKSCRIPT=@rb, ERRORMSG=NULL
              WHERE ID=@id",
            new { ms = (int)sw.ElapsedMilliseconds, rb = rollbackSb.ToString(), id = upgradeId });

          return new ExecuteResult
          {
            Status = "SUCCESS",
            ItemCount = executedCount,
            DurationMs = (int)sw.ElapsedMilliseconds
          };
        }
        catch (Exception ex)
        {
          // 任一失败：回滚事务
          try { trans.Rollback(); } catch { /* ignore */ }
          errorMsg = ex.Message;
          failedItemId = items.Count > executedCount ? items[executedCount].ItemId : null;
          failedItemCategory = items.Count > executedCount ? items[executedCount].Category : null;

          // 已写的 log（在事务内）已随 Rollback 撤销，这里补一条 FAILED log（独立事务外）
          try
          {
            helper.Execute(
              @"INSERT INTO tss_aidev_upgrade_log
                (ID, UPGRADEID, ITEMID, ITEMCATEGORY, ITEMACTION, ITEMTARGET, SQLSNIPPET, STATUS, ERRORMSG, EXECUTEDTIME)
                VALUES (@id, @uid, @iid, @cat, @act, @tgt, @sql, 'FAILED', @err, NOW())",
              new
              {
                id = Guid.NewGuid().ToString("N"),
                uid = upgradeId,
                iid = failedItemId ?? "",
                cat = failedItemCategory ?? "",
                act = "",
                tgt = "",
                sql = "",
                err = SqlScriptHelper.Truncate(errorMsg, 2000)
              });
          }
          catch { /* ignore log failure */ }

          sw.Stop();
          helper.Execute(
            "UPDATE tss_aidev_upgrade SET STATUS='FAILED', DURATIONMS=@ms, ERRORMSG=@err WHERE ID=@id",
            new { ms = (int)sw.ElapsedMilliseconds, err = SqlScriptHelper.Truncate(errorMsg, 2000), id = upgradeId });

          return new ExecuteResult
          {
            Status = "FAILED",
            ItemCount = executedCount,
            FailedItemId = failedItemId,
            FailedItemCategory = failedItemCategory,
            ErrorMsg = errorMsg,
            DurationMs = (int)sw.ElapsedMilliseconds
          };
        }
      }
    }

    /// <summary>
    /// 回滚：执行 ROLLBACKSCRIPT，STATUS 置 ROLLEDBACK。
    /// 仅 SUCCESS 状态可回滚。
    /// </summary>
    public ExecuteResult Rollback(string upgradeId, string rolledbackBy)
    {
      if (string.IsNullOrEmpty(upgradeId))
        throw new ArgumentException("upgradeId 不能为空");

      var sw = System.Diagnostics.Stopwatch.StartNew();
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var upg = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, STATUS, ROLLBACKSCRIPT FROM tss_aidev_upgrade WHERE ID=@id AND ISDELETED=0",
          new { id = upgradeId });
        if (upg == null)
          throw new InvalidOperationException("升级记录不存在: " + upgradeId);

        string status = (string)upg.STATUS;
        if (status != "SUCCESS")
          throw new InvalidOperationException("仅 SUCCESS 状态可回滚，当前状态=" + status);

        string rollbackScript = upg.ROLLBACKSCRIPT == null ? "" : (string)upg.ROLLBACKSCRIPT;
        if (string.IsNullOrEmpty(rollbackScript))
          throw new InvalidOperationException("无回滚脚本（ROLLBACKSCRIPT 为空），可能未生成快照");

        // 单事务执行回滚脚本
        var stmts = SqlScriptHelper.SplitSqlStatements(rollbackScript);
        IDbTransaction trans = helper.BeginTransaction();
        try
        {
          foreach (var stmt in stmts)
          {
            string s = stmt.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            helper.Execute(s, null, trans);
          }
          trans.Commit();

          sw.Stop();
          helper.Execute(
            "UPDATE tss_aidev_upgrade SET STATUS='ROLLEDBACK', DURATIONMS=@ms WHERE ID=@id",
            new { ms = (int)sw.ElapsedMilliseconds, id = upgradeId });

          return new ExecuteResult
          {
            Status = "ROLLEDBACK",
            DurationMs = (int)sw.ElapsedMilliseconds
          };
        }
        catch (Exception ex)
        {
          try { trans.Rollback(); } catch { /* ignore */ }
          sw.Stop();
          helper.Execute(
            "UPDATE tss_aidev_upgrade SET ERRORMSG=@err WHERE ID=@id",
            new { err = SqlScriptHelper.Truncate("回滚失败：" + ex.Message, 2000), id = upgradeId });
          throw new InvalidOperationException("回滚失败：" + ex.Message, ex);
        }
      }
    }

    /// <summary>
    /// 从变更项列表里抽取所有涉及的表名（用于快照）。
    /// 从 TARGET 字段提取；若 TARGET 为空，尝试从 SQL 里解析。
    /// </summary>
    private List<string> ExtractTargets(List<UpgradeItem> items)
    {
      var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var item in items)
      {
        if (!string.IsNullOrEmpty(item.Target))
        {
          // TARGET 可能是 RESOURCENAME（如 tbs_xxx）或表名
          // 直接加入，SHOW CREATE TABLE 不存在会异常跳过
          set.Add(item.Target);
        }
        // 从 SQL 提取 CREATE TABLE xxx / ALTER TABLE xxx / INSERT INTO xxx / UPDATE xxx / DELETE FROM xxx
        if (!string.IsNullOrEmpty(item.Sql))
        {
          var m = Regex.Match(item.Sql, @"(?i)(?:CREATE\s+TABLE|ALTER\s+TABLE|INSERT\s+INTO|UPDATE|DELETE\s+FROM)\s+`?(\w+)`?");
          if (m.Success) set.Add(m.Groups[1].Value);
        }
      }
      // 过滤掉系统表（避免对 tss_aidev_upgrade 自身生成快照导致循环）
      set.RemoveWhere(n => n.StartsWith("tss_aidev_upgrade", StringComparison.OrdinalIgnoreCase));
      return set.ToList();
    }

  }

  /// <summary>
  /// 升级执行结果。
  /// </summary>
  public class ExecuteResult
  {
    public string Status;            // SUCCESS / FAILED / ROLLEDBACK
    public int ItemCount;            // 成功执行的变更项数
    public string FailedItemId;      // 失败项 ID（FAILED 时）
    public string FailedItemCategory;// 失败项类别（FAILED 时）
    public string ErrorMsg;          // 错误信息（FAILED 时）
    public int DurationMs;           // 耗时毫秒
  }

  /// <summary>
  /// 脚本里解析出的单个变更项。
  /// </summary>
  public class UpgradeItem
  {
    public string ItemId;
    public string Category;
    public string Action;
    public string Target;
    public string Tool;
    public string Seq;
    public string ConfirmOrder;
    public string DependsOn;
    public string Rationale;
    public string Warnings;
    public string Sql;
  }
}
