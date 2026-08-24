using System;
using System.Linq;
using System.Text;
using Realso.Data.DBAccess;
using Realso.WebAPI.Models.AiDev;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// 变更包导出器：把会话关联的 changeset 中所有 CONFIRMED 变更项，
  /// 按 CONFIRMORDER 拼接成可执行的 .aidev.sql 脚本。
  ///
  /// 导出脚本结构：
  /// 1. 元数据头（@META SESSIONCODE/SESSIONNAME/SESSIONTYPE/TARGETMODULE/INTENT/ITEMS）
  /// 2. 前置幂等检查（SELECT COUNT FROM tss_aidev_upgrade WHERE SESSIONCODE=... AND STATUS='SUCCESS'）
  /// 3. 分节注释 + 每条变更项的 SQLCONTENT
  /// 4. 导出后会话状态冻结为 EXPORTED
  /// </summary>
  public class ChangeSetExporter
  {
    private readonly ChangeSetEngine _changeSetEngine;

    public ChangeSetExporter(ChangeSetEngine changeSetEngine)
    {
      _changeSetEngine = changeSetEngine;
    }

    /// <summary>
    /// 导出会话的变更包为 .aidev.sql 脚本字符串。
    /// 同时把会话 STATUS 置为 EXPORTED（冻结）。
    /// </summary>
    public string Export(string sessionId)
    {
      if (string.IsNullOrEmpty(sessionId))
        throw new ArgumentException("sessionId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 1. 查会话 + changeset
        var session = helper.QueryFirstOrDefault<dynamic>(
          @"SELECT s.ID, s.SESSIONCODE, s.SESSIONNAME, s.SESSIONTYPE, s.TARGETMODULE, s.INTENT,
                   s.CHANGESETID, s.STATUS
            FROM tss_aidev_session s
            WHERE s.ID=@id AND s.ISDELETED=0",
          new { id = sessionId });
        if (session == null)
          throw new InvalidOperationException("会话不存在: " + sessionId);

        string changesetId = (string)session.CHANGESETID;
        if (string.IsNullOrEmpty(changesetId))
          throw new InvalidOperationException("会话 " + sessionId + " 无关联变更包");

        // 2. 查所有 CONFIRMED 变更项（按 CONFIRMORDER 排序）
        var items = helper.Query<ChangeItem>(
          @"SELECT ID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET,
                   SQLCONTENT, METADATA, RATIONALE, WARNINGS, DEPENDSON,
                   CONFIRMORDER
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0
            ORDER BY CONFIRMORDER, ITEMSEQ",
          new { csid = changesetId }).ToList();

        // 3. 拼接脚本
        var sb = new StringBuilder();
        string sessionCode = (string)session.SESSIONCODE ?? "";
        string sessionName = (string)session.SESSIONNAME ?? "";
        string sessionType = (string)session.SESSIONTYPE ?? "";
        string targetModule = (string)session.TARGETMODULE ?? "";
        string intent = (string)session.INTENT ?? "";

        // 3.1 元数据头
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- AI 开发变更包导出脚本 (.aidev.sql)");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- @META SessionId=" + sessionId);
        sb.AppendLine("-- @META SessionCode=" + sessionCode);
        sb.AppendLine("-- @META SessionName=" + sessionName);
        sb.AppendLine("-- @META SessionType=" + sessionType);
        sb.AppendLine("-- @META TargetModule=" + targetModule);
        sb.AppendLine("-- @META Intent=" + intent);
        sb.AppendLine("-- @META ChangeSetId=" + changesetId);
        sb.AppendLine("-- @META Items=" + items.Count);
        sb.AppendLine("-- @META ExportedAt=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("-- @META ExportedBy=ai-dev-assistant");
        sb.AppendLine();

        // 3.2 前置幂等检查：若此会话已成功执行过则跳过
        sb.AppendLine("-- ------------------------------------------------------------");
        sb.AppendLine("-- 幂等检查：若此会话已成功执行过则跳过整个脚本");
        sb.AppendLine("-- ------------------------------------------------------------");
        sb.AppendLine("SELECT @exec_count := COUNT(1) FROM tss_aidev_upgrade WHERE SESSIONCODE='" + sessionCode + "' AND STATUS='SUCCESS';");
        sb.AppendLine("SET @skip_script = IF(@exec_count > 0, 1, 0);");
        sb.AppendLine("-- 若 @skip_script=1 则后续 SQL 全部不执行（执行前由升级程序判断）");
        sb.AppendLine();

        // 3.3 分节注释 + 每条变更项 SQL
        int idx = 0;
        foreach (var item in items)
        {
          idx++;
          sb.AppendLine("-- ============================================================");
          sb.AppendLine("-- @ITEM " + idx + "/" + items.Count);
          sb.AppendLine("-- @ITEM id=" + item.ID);
          sb.AppendLine("-- @ITEM seq=" + item.ITEMSEQ + " confirmOrder=" + item.CONFIRMORDER);
          sb.AppendLine("-- @ITEM category=" + item.CATEGORY + " action=" + item.ACTION);
          sb.AppendLine("-- @ITEM tool=" + (item.TOOL ?? ""));
          sb.AppendLine("-- @ITEM target=" + (item.TARGET ?? ""));
          if (!string.IsNullOrEmpty(item.DEPENDSON))
            sb.AppendLine("-- @ITEM dependsOn=" + item.DEPENDSON);
          if (!string.IsNullOrEmpty(item.RATIONALE))
            sb.AppendLine("-- @ITEM rationale=" + item.RATIONALE);
          if (!string.IsNullOrEmpty(item.WARNINGS))
            sb.AppendLine("-- @ITEM warnings=" + item.WARNINGS);
          sb.AppendLine("-- ============================================================");
          if (!string.IsNullOrEmpty(item.SQLCONTENT))
          {
            sb.AppendLine(item.SQLCONTENT);
            sb.AppendLine();
          }
          else
          {
            sb.AppendLine("-- (无 SQLCONTENT，纯元数据变更，跳过)");
            sb.AppendLine();
          }
        }

        // 3.4 收尾：记录本次升级状态
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("-- 升级完成标记");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("INSERT INTO tss_aidev_upgrade (ID, SESSIONCODE, SESSIONID, CHANGESETID, STATUS, EXPORTEDAT, EXECUTEDAT, ISDELETED)");
        sb.AppendLine("VALUES ('" + Guid.NewGuid().ToString("N") + "', '" + sessionCode + "', '" + sessionId + "', '" + changesetId + "', 'PENDING', NOW(), NULL, 0);");

        // 4. 冻结会话状态为 EXPORTED
        helper.Execute(
          "UPDATE tss_aidev_session SET STATUS='EXPORTED' WHERE ID=@id",
          new { id = sessionId });

        return sb.ToString();
      }
    }

    /// <summary>
    /// 仅获取已确认脚本内容（不冻结会话状态）。用于预览。
    /// </summary>
    public string PreviewScript(string sessionId)
    {
      if (string.IsNullOrEmpty(sessionId))
        throw new ArgumentException("sessionId 不能为空");

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var session = helper.QueryFirstOrDefault<dynamic>(
          "SELECT CHANGESETID, SESSIONCODE, SESSIONNAME, SESSIONTYPE, TARGETMODULE, INTENT FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0",
          new { id = sessionId });
        if (session == null)
          throw new InvalidOperationException("会话不存在: " + sessionId);

        string changesetId = (string)session.CHANGESETID;
        if (string.IsNullOrEmpty(changesetId))
          return "-- 无关联变更包";

        var items = helper.Query<ChangeItem>(
          @"SELECT ID, ITEMSEQ, CATEGORY, ACTION, TOOL, TARGET, SQLCONTENT, DEPENDSON, CONFIRMORDER
            FROM tss_aidev_changeitem
            WHERE CHANGESETID=@csid AND ITEMSTATUS='CONFIRMED' AND ISDELETED=0
            ORDER BY CONFIRMORDER, ITEMSEQ",
          new { csid = changesetId }).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("-- 预览（不冻结会话）");
        sb.AppendLine("-- @META SessionCode=" + (string)session.SESSIONCODE);
        sb.AppendLine("-- @META Items=" + items.Count);
        sb.AppendLine();
        int idx = 0;
        foreach (var item in items)
        {
          idx++;
          sb.AppendLine("-- [" + idx + "/" + items.Count + "] " + item.CATEGORY + "/" + item.ACTION + " " + (item.TARGET ?? ""));
          if (!string.IsNullOrEmpty(item.SQLCONTENT))
          {
            sb.AppendLine(item.SQLCONTENT);
            sb.AppendLine();
          }
        }
        return sb.ToString();
      }
    }
  }
}
