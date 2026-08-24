using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.Utils;
using Realso.WebAPI.Models.AiDev;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// 变更包引擎：管理变更项的追加/确认/拒绝/导出。
  /// AI 产出的变更项先以 DRAFT 状态入库，用户确认后转 CONFIRMED，
  /// 最终按 CONFIRMORDER 拼接成可执行 SQL 脚本导出。
  /// 导出后会话状态冻结为 EXPORTED。
  /// </summary>
  public class ChangeSetEngine
  {
    private readonly ChangeSetValidator _validator = new ChangeSetValidator();

    /// <summary>
    /// 追加一条 AI 产出的 DRAFT 变更项。
    /// 同步更新 changeset 的 ITEMCOUNT。
    /// </summary>
    public bool AppendItem(string changesetId, ChangeItem item)
    {
      if (string.IsNullOrEmpty(changesetId))
        throw new ArgumentException("changesetId 不能为空");
      if (item == null)
        throw new ArgumentException("item 不能为空");

      // 自动生成 ID（ITEMSEQ 在事务内分配，避免并发重复）
      if (string.IsNullOrEmpty(item.ID))
      {
        item.ID = Guid.NewGuid().ToString("N");
      }
      item.CHANGESETID = changesetId;
      item.ITEMSTATUS = ChangeItem.STATUS_DRAFT;
      item.ISDELETED = 0;

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // ⚠ DB.GetDBHelper() 创建的 MySqlConnection 默认 closed，BeginTransaction 不会自动 open（Dapper Query/Execute 才会）
        // 必须显式 Open，否则 "Connection is not open"（参考 ViewOperate01 line 79）
        if (helper.Connection.State != System.Data.ConnectionState.Open) helper.Connection.Open();
        using (var tran = helper.BeginTransaction())
        {
        // 事务内分配 ITEMSEQ：读-改-写在事务里，避免并发产出相同 ITEMSEQ
        int max = helper.Connection.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(ITEMSEQ),0) FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ISDELETED=0",
          new { csid = changesetId }, tran);
        if (item.ITEMSEQ <= 0)
        {
          item.ITEMSEQ = max + 1;
        }

        // 去重兜底：同一 changeset 内，CATEGORY+ACTION+TARGET 相同的现有 DRAFT 项，
        // 用标准化 SQL（NormalizeSql 去除 GUID 后缀/时间戳等动态噪声）比较，任一相同则跳过。
        // 不再用 SQLCONTENT 完全相同判断（LLM 每次产出 fieldId GUID 后缀、ENTRYNUM、时间戳必然不同，会绕过去重）。
        var existingSqls = helper.Connection.Query<string>(
          @"SELECT SQLCONTENT FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND CATEGORY=@cat AND ACTION=@act
              AND ((TARGET IS NULL AND @tgt IS NULL) OR (TARGET=@tgt))
              AND ISDELETED=0",
          new { csid = changesetId, cat = item.CATEGORY, act = item.ACTION, tgt = item.TARGET }, tran);
        string normalizedNew = NormalizeSql(item.SQLCONTENT);
        foreach (var es in existingSqls)
        {
          if (NormalizeSql(es) == normalizedNew)
          {
            tran.Rollback();
            return false;  // 已存在语义相同的变更项，跳过本次追加（返回 false 让 orchestrator 不触发 onItem，避免前端幽灵项）
          }
        }

        helper.Execute(
          @"INSERT INTO tss_aidev_changeitem
            (ID, CHANGESETID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET,
             SQLCONTENT, METADATA, RATIONALE, WARNINGS, DEPENDSON,
             ITEMSTATUS, CONFIRMEDBY, CONFIRMEDTIME, CONFIRMORDER, ISDELETED)
            VALUES
            (@ID, @CHANGESETID, @ITEMSEQ, @CATEGORY, @ACTION, @TOOL, @TARGET,
             @SQLCONTENT, @METADATA, @RATIONALE, @WARNINGS, @DEPENDSON,
             @ITEMSTATUS, NULL, NULL, NULL, 0)",
          new
          {
            item.ID,
            item.CHANGESETID,
            item.ITEMSEQ,
            item.CATEGORY,
            item.ACTION,
            item.TOOL,
            item.TARGET,
            item.SQLCONTENT,
            item.METADATA,
            item.RATIONALE,
            item.WARNINGS,
            item.DEPENDSON,
            item.ITEMSTATUS
          }, tran);

        // 同步 changeset 的 ITEMCOUNT
        helper.Execute(
          @"UPDATE tss_aidev_changeset SET ITEMCOUNT = (
              SELECT COUNT(1) FROM tss_aidev_changeitem
              WHERE CHANGESETID=@csid AND ISDELETED=0
            ) WHERE ID=@csid",
          new { csid = changesetId }, tran);

        tran.Commit();
        }
      }
      return true;
    }

    /// <summary>
    /// 去重清理：同一 changeset 内，CATEGORY+ACTION+TARGET+SQLCONTENT 完全相同的项只保留最早一条（ITEMSEQ 最小），其余逻辑删除。
    /// 返回删除的项数。用于清理 LLM 历史重复产出（如多次重复的 ALTER TABLE、空 SQL 的 define_reference_field 产出等）。
    /// </summary>
    public int DedupItems(string changesetId)
    {
      if (string.IsNullOrEmpty(changesetId)) return 0;
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 查所有项（按重复 key 排序，ITEMSEQ 升序），每组保留第一条
        var rows = helper.Query<dynamic>(
          @"SELECT ID, CATEGORY, ACTION, COALESCE(TARGET,'') AS TARGET, COALESCE(SQLCONTENT,'') AS SQLCONTENT
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ISDELETED=0
            ORDER BY CATEGORY, ACTION, TARGET, SQLCONTENT, ITEMSEQ",
          new { csid = changesetId }).ToList();

        if (rows.Count == 0) return 0;

        var keepIds = new HashSet<string>();
        string lastKey = null;
        foreach (var r in rows)
        {
          // 分组 key 用 NormalizeSql 去除动态噪声（GUID 后缀/时间戳/VALUES 内 GUID 字面量），
          // 让同一字段的多次产出归为同一组（避免 LLM 工具每次带不同噪声导致去重失效）
          string key = (string)r.CATEGORY + "|" + (string)r.ACTION + "|" + (string)r.TARGET + "|" + NormalizeSql((string)r.SQLCONTENT);
          if (key != lastKey)
          {
            keepIds.Add((string)r.ID);  // 每组第一条（ITEMSEQ 最小）保留
            lastKey = key;
          }
        }

        if (keepIds.Count == rows.Count) return 0;  // 无重复

        // 逻辑删除非保留项（Dapper 展开 IN 子句）
        int deleted = helper.Execute(
          @"UPDATE tss_aidev_changeitem SET ISDELETED=1
            WHERE CHANGESETID=@csid AND ISDELETED=0
            AND ID NOT IN @keepIds",
          new { csid = changesetId, keepIds = keepIds });

        // 同步 changeset 的 ITEMCOUNT
        helper.Execute(
          @"UPDATE tss_aidev_changeset SET ITEMCOUNT = (
              SELECT COUNT(1) FROM tss_aidev_changeitem
              WHERE CHANGESETID=@csid AND ISDELETED=0
            ) WHERE ID=@csid",
          new { csid = changesetId });

        return deleted;
      }
    }

    /// <summary>
    /// 确认变更项：DRAFT→CONFIRMED，写 CONFIRMEDBY/CONFIRMEDTIME/CONFIRMORDER。
    /// 校验 DEPENDSON 指向的依赖项是否已 CONFIRMED，未确认则拒绝确认。
    /// </summary>
    public void ConfirmItem(string itemId, string confirmedBy)
    {
      if (string.IsNullOrEmpty(itemId))
        throw new ArgumentException("itemId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var item = helper.QueryFirstOrDefault<ChangeItem>(
          "SELECT ID, CHANGESETID, ITEMSTATUS, DEPENDSON FROM tss_aidev_changeitem WHERE ID=@id AND ISDELETED=0",
          new { id = itemId });
        if (item == null)
          throw new InvalidOperationException("变更项不存在: " + itemId);
        if (item.ITEMSTATUS != ChangeItem.STATUS_DRAFT)
          throw new InvalidOperationException("变更项状态为 " + item.ITEMSTATUS + "，只有 DRAFT 可确认");

        // 校验依赖项是否已确认
        if (!string.IsNullOrEmpty(item.DEPENDSON))
        {
          var depIds = item.DEPENDSON.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
          foreach (var depId in depIds)
          {
            var depStatus = helper.QueryFirstOrDefault<string>(
              "SELECT ITEMSTATUS FROM tss_aidev_changeitem WHERE ID=@id AND ISDELETED=0",
              new { id = depId });
            if (string.IsNullOrEmpty(depStatus))
              throw new InvalidOperationException("依赖项 " + depId + " 不存在");
            if (depStatus != ChangeItem.STATUS_CONFIRMED)
              throw new InvalidOperationException("依赖项 " + depId + " 未确认，无法确认当前项");
          }
        }

        // CONFIRMORDER 自增
        int nextOrder = helper.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(CONFIRMORDER),0)+1 FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED'",
          new { csid = item.CHANGESETID });

        helper.Execute(
          @"UPDATE tss_aidev_changeitem
            SET ITEMSTATUS='CONFIRMED', CONFIRMEDBY=@by, CONFIRMEDTIME=@tm, CONFIRMORDER=@ord
            WHERE ID=@id",
          new { by = confirmedBy ?? "", tm = DateTime.Now, ord = nextOrder, id = itemId });
      }
    }

    /// <summary>
    /// 拒绝变更项：DRAFT→REJECTED。
    /// </summary>
    public void RejectItem(string itemId)
    {
      if (string.IsNullOrEmpty(itemId))
        throw new ArgumentException("itemId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var status = helper.QueryFirstOrDefault<string>(
          "SELECT ITEMSTATUS FROM tss_aidev_changeitem WHERE ID=@id AND ISDELETED=0",
          new { id = itemId });
        if (string.IsNullOrEmpty(status))
          throw new InvalidOperationException("变更项不存在: " + itemId);
        if (status != ChangeItem.STATUS_DRAFT)
          throw new InvalidOperationException("变更项状态为 " + status + "，只有 DRAFT 可拒绝");

        helper.Execute(
          "UPDATE tss_aidev_changeitem SET ITEMSTATUS='REJECTED' WHERE ID=@id",
          new { id = itemId });
      }
    }

    /// <summary>
    /// 撤销确认：CONFIRMED→DRAFT（导出前可用）。
    /// 释放 CONFIRMORDER，后续项需重新确认重排。
    /// </summary>
    public void UnconfirmItem(string itemId)
    {
      if (string.IsNullOrEmpty(itemId))
        throw new ArgumentException("itemId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var status = helper.QueryFirstOrDefault<string>(
          "SELECT ITEMSTATUS FROM tss_aidev_changeitem WHERE ID=@id AND ISDELETED=0",
          new { id = itemId });
        if (string.IsNullOrEmpty(status))
          throw new InvalidOperationException("变更项不存在: " + itemId);
        if (status != ChangeItem.STATUS_CONFIRMED)
          throw new InvalidOperationException("变更项状态为 " + status + "，只有 CONFIRMED 可撤销");

        // 检查是否有其他 CONFIRMED 项依赖此项，有则不允许撤销
        var csid = helper.QueryFirstOrDefault<string>(
          "SELECT CHANGESETID FROM tss_aidev_changeitem WHERE ID=@id",
          new { id = itemId });
        var dependents = helper.Query<string>(
          "SELECT ID FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND DEPENDSON LIKE @pat",
          new { csid, pat = "%" + itemId + "%" });
        if (dependents.Any())
        {
          throw new InvalidOperationException("有已确认项依赖此项: " + string.Join(",", dependents) + "，请先撤销它们的确认");
        }

        helper.Execute(
          "UPDATE tss_aidev_changeitem SET ITEMSTATUS='DRAFT', CONFIRMEDBY=NULL, CONFIRMEDTIME=NULL, CONFIRMORDER=NULL WHERE ID=@id",
          new { id = itemId });
      }
    }

    /// <summary>
    /// 获取已确认脚本：查所有 CONFIRMED 项，按 CONFIRMORDER 拼接 SQLCONTENT。
    /// DEPENDSON 用于校验依赖已满足（确认时已检查），CONFIRMORDER 即执行顺序。
    /// </summary>
    public string GetConfirmedScript(string changesetId)
    {
      if (string.IsNullOrEmpty(changesetId))
        throw new ArgumentException("changesetId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var items = helper.Query<ChangeItem>(
          @"SELECT ID, ITEMSEQ, CATEGORY, ACTION, TARGET, SQLCONTENT, DEPENDSON, CONFIRMORDER
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0
            ORDER BY CONFIRMORDER, ITEMSEQ",
          new { csid = changesetId }).ToList();

        if (items.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- AI 开发变更包导出脚本");
        sb.AppendLine("-- ChangeSetId: " + changesetId);
        sb.AppendLine("-- 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("-- 变更项数: " + items.Count);
        sb.AppendLine();

        int idx = 0;
        foreach (var item in items)
        {
          idx++;
          sb.AppendLine("-- ============================================================");
          sb.AppendLine("-- [" + idx + "/" + items.Count + "] " + item.CATEGORY + "/" + item.ACTION + " " + (item.TARGET ?? ""));
          sb.AppendLine("-- ItemSeq=" + item.ITEMSEQ + " ConfirmOrder=" + item.CONFIRMORDER);
          if (!string.IsNullOrEmpty(item.DEPENDSON))
            sb.AppendLine("-- DependsOn: " + item.DEPENDSON);
          sb.AppendLine("-- ============================================================");
          if (!string.IsNullOrEmpty(item.SQLCONTENT))
          {
            sb.AppendLine(item.SQLCONTENT);
            sb.AppendLine();
          }
          else
          {
            sb.AppendLine("-- (无 SQLCONTENT，跳过)");
            sb.AppendLine();
          }
        }
        return sb.ToString();
      }
    }

    /// <summary>
    /// 执行已确认脚本（开发环境直接落库，调试用）。
    /// 把所有 CONFIRMED 项的 SQLCONTENT 按顺序（CONFIRMORDER, ITEMSEQ）在单事务里执行。
    /// 全部成功 commit，任一失败 rollback 并抛异常。返回执行结果。
    /// </summary>
    public object ExecuteConfirmed(string changesetId, string executedBy)
    {
      if (string.IsNullOrEmpty(changesetId))
        throw new ArgumentException("changesetId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var items = helper.Query<dynamic>(
          @"SELECT ID, ITEMSEQ, CATEGORY, ACTION, TARGET, SQLCONTENT, CONFIRMORDER
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0
            ORDER BY CONFIRMORDER, ITEMSEQ",
          new { csid = changesetId }).ToList();

        if (items.Count == 0)
          return new { success = false, itemCount = 0, errorMsg = "没有已确认的变更项，请先确认" };

        int total = 0;      // 实际执行的 SQL 语句数
        int itemDone = 0;   // 完成的变更项数
        var executedItems = new List<object>();

        // ⚠ 同 AppendItem: DB.GetDBHelper() 的连接默认 closed, BeginTransaction 不会自动 open,
        // 必须显式 Open, 否则 "Connection is not open"
        if (helper.Connection.State != System.Data.ConnectionState.Open) helper.Connection.Open();
        var tran = helper.BeginTransaction();
        try
        {
          foreach (var it in items)
          {
            string sqlContent = (string)it.SQLCONTENT;
            string itemId = (string)it.ID;
            if (string.IsNullOrWhiteSpace(sqlContent))
            {
              itemDone++;
              executedItems.Add(new { id = itemId, seq = it.ITEMSEQ, target = it.TARGET, status = "SKIPPED", reason = "无 SQL" });
              continue;
            }
            // 按分号拆分多条语句（过滤空行/纯注释行）
            var stmts = sqlContent.Split(new[] { ';' }, StringSplitOptions.None);
            int stmtExecuted = 0;
            foreach (var raw in stmts)
            {
              string stmt = raw.Trim();
              if (string.IsNullOrEmpty(stmt)) continue;
              // 跳过纯注释行（以 -- 开头的整段）
              var lines = stmt.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("--"));
              string clean = string.Join("\n", lines);
              if (string.IsNullOrEmpty(clean)) continue;
              helper.Execute(clean + ";", transaction: tran);
              stmtExecuted++;
              total++;
            }
            itemDone++;
            executedItems.Add(new { id = itemId, seq = it.ITEMSEQ, target = it.TARGET, status = "SUCCESS", stmts = stmtExecuted });
          }
          // 执行成功：CONFIRMED → EXECUTED（同事务），向导步骤强制依赖此状态判定"上一步已执行"
          helper.Connection.Execute(
            @"UPDATE tss_aidev_changeitem SET ITEMSTATUS='EXECUTED'
              WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0",
            new { csid = changesetId }, tran);
          tran.Commit();
          // 版本捕获（直接 SQL 通道）：按变更项 CATEGORY+METADATA 快照触及的元数据对象
          // 失败只记日志，不影响执行结果（版本是安全网）
          try
          {
            CaptureExecutedItems(helper, changesetId, executedBy);
          }
          catch (Exception exCap)
          {
            Logger.Warn("ChangeSetEngine 版本捕获异常（已跳过）: " + exCap.Message);
          }
          return new { success = true, itemCount = itemDone, totalStatements = total, executedItems };
        }
        catch (Exception ex)
        {
          tran.Rollback();
          return new { success = false, itemCount = itemDone, totalStatements = total, errorMsg = ex.Message, executedItems };
        }
      }
    }

    /// <summary>
    /// 执行成功后的版本捕获：扫描本变更集 CONFIRMED(直接执行)+MERGED(被合并执行) 的变更项，
    /// 按 CATEGORY+METADATA 提取触及的元数据对象，交 DevVersionService 逐对象快照。
    /// </summary>
    private void CaptureExecutedItems(DBHelper helper, string changesetId, string executedBy)
    {
      var items = helper.Query<dynamic>(
        @"SELECT CATEGORY, ACTION, METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ITEMSTATUS IN ('CONFIRMED','MERGED') AND ISDELETED=0",
        new { csid = changesetId }).ToList();
      if (items.Count == 0) return;

      var objs = new List<Realso.WebAPI.Services.DevVersionService.ObjRef>();
      var seen = new HashSet<string>();
      foreach (var it in items)
      {
        string category = it.CATEGORY + "";
        string action = it.ACTION + "";
        string meta = it.METADATA + "";
        if (string.IsNullOrEmpty(meta)) continue;
        JObject jo;
        try { jo = JObject.Parse(meta); } catch { continue; }
        string opType = action == ChangeItem.ACTION_CREATE ? "insert"
          : action == ChangeItem.ACTION_DELETE ? "delete" : "update";
        foreach (var pair in ExtractObjects(category, jo, opType))
        {
          string key = pair.ResourceName + "|" + pair.ObjId;
          if (seen.Add(key)) objs.Add(pair);
        }
      }
      if (objs.Count > 0)
      {
        Realso.WebAPI.Services.DevVersionService.CaptureObjects(objs, executedBy, "AI 变更集执行 " + changesetId);
      }
    }

    /// <summary>按变更项类别从 METADATA 提取 (资源名, 对象ID) 列表</summary>
    private List<Realso.WebAPI.Services.DevVersionService.ObjRef> ExtractObjects(string category, JObject meta, string opType)
    {
      var list = new List<Realso.WebAPI.Services.DevVersionService.ObjRef>();
      Action<string, string> add = (metaKey, resourceName) =>
      {
        foreach (var id in ExtractIds(meta, metaKey))
        {
          list.Add(new Realso.WebAPI.Services.DevVersionService.ObjRef { ResourceName = resourceName, ObjId = id, OpType = opType });
        }
      };
      switch (category)
      {
        case ChangeItem.CAT_PHYSICAL_TABLE:
        case ChangeItem.CAT_DATAVIEW:
          add("resource", "VSS_RESOURCE");
          add("resfields", "VSS_RESFIELD");
          add("resfield", "VSS_RESFIELD");
          break;
        case ChangeItem.CAT_FIELD:
          add("resfield", "VSS_RESFIELD");
          add("resfields", "VSS_RESFIELD");
          break;
        case ChangeItem.CAT_UI:
          add("resuipc", "VSS_RESUIPC");
          add("uipc", "VSS_RESUIPC");
          break;
        case ChangeItem.CAT_DICT:
          add("dict", "VSS_DICT");
          break;
        case ChangeItem.CAT_FILTER:
          add("filter", "VSS_RESFILTER");
          break;
        case ChangeItem.CAT_MODULE:
          add("module", "VSS_MOUDLE");
          break;
        case ChangeItem.CAT_API:
          add("moudleapi", "VSS_MOUDLEAPI");
          add("api", "VSS_MOUDLEAPI");
          add("sql", "VSS_CODE_ASSET");
          break;
        case ChangeItem.CAT_MENU:
          add("func", "VSS_FUNC");
          break;
        case ChangeItem.CAT_PERMISSION:
          add("funcpoints", "VSS_FUNCPOINT");
          add("funcpoint", "VSS_FUNCPOINT");
          break;
        case ChangeItem.CAT_PAGE:
          add("page", "VCK_MODULE_PAGE");
          break;
        case ChangeItem.CAT_BUTTON:
          add("button", "VCK_MODULE_BUTTON");
          break;
      }
      return list;
    }

    /// <summary>从 METADATA 指定键提取 ID 列表（值可能是单对象 {ID} 或数组 [{ID},...]）</summary>
    private List<string> ExtractIds(JObject meta, string key)
    {
      var ids = new List<string>();
      var token = meta[key];
      if (token == null) return ids;
      if (token is JArray)
      {
        foreach (var t in (JArray)token)
        {
          var id = t is JObject ? ((JObject)t)["ID"] + "" : t + "";
          if (!string.IsNullOrEmpty(id)) ids.Add(id);
        }
      }
      else if (token is JObject)
      {
        var id = ((JObject)token)["ID"] + "";
        if (!string.IsNullOrEmpty(id)) ids.Add(id);
      }
      return ids;
    }

    /// <summary>
    /// 合并变更项：把所有 DRAFT 项的 SQLCONTENT 按依赖顺序拼接成一条整体 SQL，
    /// 创建一条 CAT_MERGED 的 CONFIRMED 变更项（含整段脚本），原 DRAFT 项标记为 STATUS_MERGED。
    /// 这样分析阶段可逐条审核，确认时合并为一条统一变更项（用户选择"按会话合并为一条"）。
    /// 幂等：已存在 MERGED 项则返回其 ID，不重复合并。
    /// </summary>
    public string MergeItems(string changesetId, string confirmedBy)
    {
      if (string.IsNullOrEmpty(changesetId))
        throw new ArgumentException("changesetId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 幂等：已有 MERGED 项则直接返回
        string existingMerged = helper.QueryFirstOrDefault<string>(
          "SELECT ID FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND CATEGORY=@cat AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0 LIMIT 1",
          new { csid = changesetId, cat = ChangeItem.CAT_MERGED });
        if (!string.IsNullOrEmpty(existingMerged)) return existingMerged;

        // 查所有 DRAFT 项，按 ITEMSEQ（产出顺序）拼接
        var drafts = helper.Query<ChangeItem>(
          @"SELECT ID, ITEMSEQ, CATEGORY, ACTION, TARGET, SQLCONTENT, DEPENDSON
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ITEMSTATUS='DRAFT' AND ISDELETED=0
            ORDER BY ITEMSEQ",
          new { csid = changesetId }).ToList();

        if (drafts.Count == 0)
          throw new InvalidOperationException("无 DRAFT 变更项可合并");

        // 拼接整段 SQL（带分节注释，便于审核）
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- AI 开发合并变更脚本（" + drafts.Count + " 个变更项合并）");
        sb.AppendLine("-- ChangeSetId: " + changesetId);
        sb.AppendLine("-- 合并时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("-- ============================================================");
        sb.AppendLine();
        int idx = 0;
        var draftIds = new List<string>();
        foreach (var d in drafts)
        {
          draftIds.Add(d.ID);
          idx++;
          sb.AppendLine("-- ---------- [" + idx + "/" + drafts.Count + "] " + d.CATEGORY + "/" + d.ACTION + " " + (d.TARGET ?? "") + " ----------");
          if (!string.IsNullOrEmpty(d.SQLCONTENT))
          {
            sb.AppendLine(d.SQLCONTENT);
            sb.AppendLine();
          }
        }

        string mergedSql = sb.ToString();
        string mergedId = Guid.NewGuid().ToString("N");
        int entryNum = helper.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(ITEMSEQ),0)+1 FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ISDELETED=0",
          new { csid = changesetId });
        int confirmOrder = helper.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(CONFIRMORDER),0)+1 FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED'",
          new { csid = changesetId });

        // 新建合并项（CONFIRMED 状态，含整段脚本）
        helper.Execute(
          @"INSERT INTO tss_aidev_changeitem
            (ID, CHANGESETID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET,
             SQLCONTENT, METADATA, RATIONALE, WARNINGS, DEPENDSON,
             ITEMSTATUS, CONFIRMEDBY, CONFIRMEDTIME, CONFIRMORDER, ISDELETED)
            VALUES
            (@ID, @CHANGESETID, @ITEMSEQ, @CATEGORY, @ACTION, @TOOL, @TARGET,
             @SQLCONTENT, @METADATA, @RATIONALE, @WARNINGS, @DEPENDSON,
             @ITEMSTATUS, @CONFIRMEDBY, @CONFIRMEDTIME, @CONFIRMORDER, 0)",
          new
          {
            ID = mergedId,
            CHANGESETID = changesetId,
            ITEMSEQ = entryNum,
            CATEGORY = ChangeItem.CAT_MERGED,
            ACTION = ChangeItem.ACTION_CREATE,
            TOOL = "merge",
            TARGET = "merged_script",
            SQLCONTENT = mergedSql,
            METADATA = "{}",
            RATIONALE = "合并 " + drafts.Count + " 个 DRAFT 变更项为一条统一脚本",
            WARNINGS = "",
            DEPENDSON = string.Join(",", draftIds),
            ITEMSTATUS = ChangeItem.STATUS_CONFIRMED,
            CONFIRMEDBY = confirmedBy ?? "",
            CONFIRMEDTIME = DateTime.Now,
            CONFIRMORDER = confirmOrder
          });

        // 原 DRAFT 项标记为 MERGED（保留记录，不删除，便于追溯；导出/执行只取 CONFIRMED）
        helper.Execute(
          "UPDATE tss_aidev_changeitem SET ITEMSTATUS=@st WHERE ID IN @ids",
          new { st = ChangeItem.STATUS_MERGED, ids = draftIds });

        // 同步 changeset ITEMCOUNT
        helper.Execute(
          @"UPDATE tss_aidev_changeset SET ITEMCOUNT = (
              SELECT COUNT(1) FROM tss_aidev_changeitem
              WHERE CHANGESETID=@csid AND ISDELETED=0
            ) WHERE ID=@csid",
          new { csid = changesetId });

        return mergedId;
      }
    }

    /// <summary>
    /// 校验整个变更包：跑 ChangeSetValidator，结果写回 changeset 的 VALIDATIONPASSED/VALIDATIONREPORT。
    /// </summary>
    public ValidationReport ValidateChangeSet(string changesetId)
    {
      if (string.IsNullOrEmpty(changesetId))
        throw new ArgumentException("changesetId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // ORDER BY 与 GetConfirmedScript 保持一致（CONFIRMORDER, ITEMSEQ），
        // 让校验顺序与导出/执行顺序对齐，便于调试和定位序号相关问题。
        // ItemSeq 字段仍保留在 SELECT 列表，供前端展示与 Validator 报告引用。
        var items = helper.Query<ChangeItem>(
          @"SELECT ID, CHANGESETID, ITEMSEQ, CATEGORY, ACTION, METADATA, DEPENDSON, CONFIRMORDER
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ISDELETED=0
            ORDER BY CONFIRMORDER, ITEMSEQ",
          new { csid = changesetId }).ToList();

        var report = _validator.Validate(items);

        // 写回 changeset
        helper.Execute(
          @"UPDATE tss_aidev_changeset
            SET VALIDATIONPASSED=@passed, VALIDATIONREPORT=@report
            WHERE ID=@csid",
          new
          {
            passed = report.Passed ? 1 : 0,
            report = JsonConvert.SerializeObject(report),
            csid = changesetId
          });

        return report;
      }
    }

    /// <summary>
    /// 检查会话状态是否 EXPORTED（导出后冻结，不可再追加/确认变更项）。
    /// </summary>
    public bool IsExported(string changesetId)
    {
      if (string.IsNullOrEmpty(changesetId)) return false;
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var status = helper.QueryFirstOrDefault<string>(
          "SELECT s.STATUS FROM tss_aidev_changeset cs JOIN tss_aidev_session s ON cs.SESSIONID = s.ID WHERE cs.ID=@csid AND cs.ISDELETED=0",
          new { csid = changesetId });
        return status == DevSession.STATUS_EXPORTED;
      }
    }

    /// <summary>
    /// 归档会话：EXPORTED → ARCHIVED。
    /// 归档后会话进入只读历史档，变更项不再可编辑（EXPORTED 已冻结，ARCHIVED 进一步标记为历史）。
    /// 只有已导出的会话才能归档。
    /// </summary>
    public void ArchiveSession(string sessionId)
    {
      if (string.IsNullOrEmpty(sessionId)) throw new InvalidOperationException("sessionId 不能为空");
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var status = helper.QueryFirstOrDefault<string>(
          "SELECT STATUS FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0",
          new { id = sessionId });
        if (status == null) throw new InvalidOperationException("会话不存在: " + sessionId);
        if (status != DevSession.STATUS_EXPORTED)
          throw new InvalidOperationException("只有已导出(EXPORTED)的会话才能归档，当前状态: " + status);
        if (status == DevSession.STATUS_ARCHIVED) return; // 已归档，幂等
        helper.Execute(
          "UPDATE tss_aidev_session SET STATUS=@st, CLOSEDATE=NOW() WHERE ID=@id",
          new { st = DevSession.STATUS_ARCHIVED, id = sessionId });
      }
    }

    /// <summary>
    /// 标准化 SQL：去除动态噪声（GUID 后缀、时间戳、VALUES 子句内的 GUID 字面量），
    /// 只保留语义结构。用于 DRAFT 去重比较，避免 LLM 工具产出因每次带不同的 fieldId GUID 后缀、
    /// ENTRYNUM 自增、时间戳等噪声导致 SQL 字符串必然不同从而绕过去重。
    /// </summary>
    private static string NormalizeSql(string sql)
    {
      if (string.IsNullOrEmpty(sql)) return "";
      string s = sql;
      // 1. 去除 8 位 hex 后缀（rf_dept_managerid_a1b2c3d4 → rf_dept_managerid_）
      s = System.Text.RegularExpressions.Regex.Replace(s, @"_[0-9a-f]{8}\b", "_");
      // 2. 去除时间戳（yyyyMMddHHmmss，14 位数字，以 20 开头）→ TS
      s = System.Text.RegularExpressions.Regex.Replace(s, @"\b20\d{12}\b", "TS");
      // 3. 去除 VALUES 子句里引号内的 32 位 hex GUID（'xxxxxxxx...32位hex' → 'GUID'）
      s = System.Text.RegularExpressions.Regex.Replace(s, @"'[0-9a-f]{32}'", "'GUID'");
      // 注意：不进行激进数字替换，避免误伤字段长度（VARCHAR(64) 等）
      return s.Trim();
    }

    /// <summary>
    /// 获取下一个 ITEMSEQ（已存在的最大 ITEMSEQ + 1）。
    /// </summary>
    private int NextItemSeq(string changesetId)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        int max = helper.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(ITEMSEQ),0) FROM tss_aidev_changeitem WHERE CHANGESETID=@csid AND ISDELETED=0",
          new { csid = changesetId });
        return max + 1;
      }
    }

    /// <summary>
    /// 查询变更包的所有变更项（按 ITEMSEQ）。
    /// </summary>
    public List<ChangeItem> ListItems(string changesetId)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        return helper.Query<ChangeItem>(
          @"SELECT ID, CHANGESETID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET,
                   SQLCONTENT, METADATA, RATIONALE, WARNINGS, DEPENDSON,
                   ITEMSTATUS, CONFIRMEDBY, CONFIRMEDTIME, CONFIRMORDER, ISDELETED
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ISDELETED=0
            ORDER BY ITEMSEQ",
          new { csid = changesetId }).ToList();
      }
    }
  }
}
