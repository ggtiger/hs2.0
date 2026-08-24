using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.Utils;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 在线开发版本管理服务：在 DataController.doSave/doDelete 的 operate01.Save 前统一拦截，
  /// 对 tss_dev_version_cfg 纳管资源的 DataView 自动抓前后镜像生成版本行，
  /// 追加进 saveList 与业务保存同事务提交（原子：业务失败版本也不留）。
  /// 设计文档: docs/low-code-ai-integration-design.md 第九章。
  /// </summary>
  public static class DevVersionService
  {
    // ===== 纳管资源配置（60s 缓存；表不存在时优雅降级为空 = 不拦截，兼容未迁移环境）=====
    public class VersionCfg
    {
      public string RESOURCENAME;
      public string OBJTYPE;
      public string CODEEXPR;
      public string NAMEEXPR;
      public int MAXVERSIONS;
    }

    /// <summary>本次保存触及的对象（用于事后清理过期版本）</summary>
    public class TouchedObj
    {
      public string ObjType;
      public string ObjId;
      public int MaxVersions;
    }

    private static List<VersionCfg> _cfgCache;
    private static DateTime _cfgLoadedAt = DateTime.MinValue;
    private static readonly object _cfgLock = new object();

    private static List<VersionCfg> GetCfg()
    {
      if (_cfgCache != null && (DateTime.Now - _cfgLoadedAt).TotalSeconds < 60) return _cfgCache;
      lock (_cfgLock)
      {
        if (_cfgCache != null && (DateTime.Now - _cfgLoadedAt).TotalSeconds < 60) return _cfgCache;
        List<VersionCfg> list = new List<VersionCfg>();
        try
        {
          using (var helper = DB.GetDBHelper())
          {
            var rows = helper.Query<dynamic>(
              "SELECT RESOURCENAME, OBJTYPE, CODEEXPR, NAMEEXPR, MAXVERSIONS FROM tss_dev_version_cfg WHERE ENABLED=1 AND ISDELETED=0");
            foreach (var r in rows)
            {
              list.Add(new VersionCfg
              {
                RESOURCENAME = (string)r.RESOURCENAME,
                OBJTYPE = (string)r.OBJTYPE,
                CODEEXPR = (string)r.CODEEXPR,
                NAMEEXPR = (string)r.NAMEEXPR,
                MAXVERSIONS = r.MAXVERSIONS == null ? 50 : (int)r.MAXVERSIONS
              });
            }
          }
        }
        catch (Exception ex)
        {
          // 表不存在（未迁移环境）等场景：降级为不拦截，绝不影响业务保存
          Logger.Warn("DevVersionService.GetCfg 读取失败（降级不拦截）: " + ex.Message);
          list = new List<VersionCfg>();
        }
        _cfgCache = list;
        _cfgLoadedAt = DateTime.Now;
        return _cfgCache;
      }
    }

    /// <summary>手动失效配置缓存（cfg 管理页保存后调用）</summary>
    public static void InvalidateCfg()
    {
      lock (_cfgLock) { _cfgCache = null; }
    }

    // ===== 直接 SQL 通道（AI 变更集执行等）的事后捕获 =====

    /// <summary>待捕获对象引用</summary>
    public class ObjRef
    {
      public string ResourceName;
      public string ObjId;
      public string OpType; // insert/update/delete
    }

    /// <summary>
    /// 批量事后捕获（直接 SQL 通道：不经过 doSave 的写入，如 AI 变更集执行）。
    /// 逐对象快照：insert→BEFORE=null/AFTER=当前行；update→BEFORE=上一版本AFTER(链式)/AFTER=当前行；
    /// delete→BEFORE=上一版本AFTER或当前行/AFTER=null。
    /// insert 规范化：该对象已有历史版本时按 update 记录（幂等脚本重复执行不产生假 insert）。
    /// 未纳管资源静默跳过；任何单对象异常只记日志不影响其余。
    /// </summary>
    public static void CaptureObjects(List<ObjRef> objs, string creater, string changeNote)
    {
      if (objs == null || objs.Count == 0) return;
      try
      {
        var cfgs = GetCfg();
        if (cfgs.Count == 0) return;
        using (var helper = DB.GetDBHelper())
        {
          var touched = new List<TouchedObj>();
          var verSeq = new Dictionary<string, int>();
          foreach (var o in objs)
          {
            try
            {
              CaptureOneObject(helper, cfgs, o, creater, changeNote, verSeq, touched);
            }
            catch (Exception ex)
            {
              Logger.Warn("DevVersionService.CaptureObjects 单对象捕获失败(" + o.ResourceName + "/" + o.ObjId + "): " + ex.Message);
            }
          }
          CleanupExpired(touched);
        }
      }
      catch (Exception ex)
      {
        Logger.Error("DevVersionService.CaptureObjects 异常（已跳过）: " + ex.Message, ex);
      }
    }

    private static void CaptureOneObject(DBHelper helper, List<VersionCfg> cfgs, ObjRef o,
      string creater, string changeNote, Dictionary<string, int> verSeq, List<TouchedObj> touched)
    {
      var cfg = cfgs.Find(c => c.RESOURCENAME == o.ResourceName);
      if (cfg == null || string.IsNullOrEmpty(o.ObjId)) return;
      // 表名: 资源的底层表（tss_ 元数据表主键统一 ID）
      string table = helper.ExecuteScalar(
        "SELECT TABLENAME FROM tss_resource WHERE RESOURCENAME=@rn LIMIT 1", new { rn = cfg.RESOURCENAME }) + "";
      if (string.IsNullOrEmpty(table)) return;

      string opType = string.IsNullOrEmpty(o.OpType) ? "update" : o.OpType;
      // insert 规范化: 已有历史版本 → update（幂等脚本重跑不产生假 insert）
      if (opType == "insert" && QueryLastVersionAfter(helper, cfg.OBJTYPE, o.ObjId) != null) opType = "update";

      string beforeContent = null, afterContent = null;
      if (opType != "insert")
      {
        beforeContent = QueryLastVersionAfter(helper, cfg.OBJTYPE, o.ObjId);
      }
      var curJson = QueryRowJson(helper, table, "ID", o.ObjId);
      if (opType != "delete")
      {
        afterContent = curJson;
        if (afterContent == null) return; // 对象不存在（如执行失败），跳过
        if (opType != "insert" && beforeContent == null) beforeContent = afterContent; // 无历史: 首版本差异为空, 后续起链式
      }
      else
      {
        if (beforeContent == null) beforeContent = curJson; // 无历史则取当前行作前镜像
        if (beforeContent == null) return;
      }

      // OBJCODE/OBJNAME 从当前行(或前镜像)按表达式提取
      var rowDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(afterContent ?? beforeContent);
      int ver = NextVersion(helper, cfg.OBJTYPE, o.ObjId, verSeq);
      helper.Execute(
        @"INSERT INTO tss_dev_version
          (ID, OBJTYPE, OBJID, OBJCODE, OBJNAME, VERSION, OPTYPE, BEFORECONTENT, AFTERCONTENT,
           CHANGENOTE, CREATEID, CREATER, CREATETIME, SRCTABLE, ISDELETED)
          VALUES (@ID, @OBJTYPE, @OBJID, @OBJCODE, @OBJNAME, @VERSION, @OPTYPE, @BEFOREC, @AFTERC,
           @NOTE, @CBY, @CBYN, @CT, @SRCTABLE, 0)",
        new
        {
          ID = Guid.NewGuid().ToString("N"),
          OBJTYPE = cfg.OBJTYPE,
          OBJID = o.ObjId,
          OBJCODE = BuildExprFromDict(cfg.CODEEXPR, rowDict),
          OBJNAME = BuildExprFromDict(cfg.NAMEEXPR, rowDict),
          VERSION = ver,
          OPTYPE = opType,
          BEFOREC = beforeContent,
          AFTERC = afterContent,
          NOTE = changeNote,
          CBY = "",
          CBYN = creater ?? "",
          CT = DateTime.Now,
          SRCTABLE = table
        });
      if (touched.Find(t => t.ObjType == cfg.OBJTYPE && t.ObjId == o.ObjId) == null)
      {
        touched.Add(new TouchedObj { ObjType = cfg.OBJTYPE, ObjId = o.ObjId, MaxVersions = cfg.MAXVERSIONS });
      }
    }

    /// <summary>按 cfg 表达式从字典取 OBJCODE/OBJNAME（逗号分隔多字段，用 / 连接）</summary>
    private static string BuildExprFromDict(string expr, Dictionary<string, object> row)
    {
      if (string.IsNullOrEmpty(expr) || row == null) return null;
      var parts = new List<string>();
      foreach (var f in expr.Split(','))
      {
        object v;
        row.TryGetValue(f.Trim(), out v);
        if (v != null && (v + "") != "") parts.Add(v + "");
      }
      return parts.Count > 0 ? string.Join("/", parts) : null;
    }

    /// <summary>
    /// 在 operate01.Save(saveList) 之前调用：为纳管 DataView 生成版本行并追加进 saveList（同事务）。
    /// 返回触及对象列表（Save 成功后传 CleanupExpired 清理过期版本）。
    /// 任何异常只记日志不影响业务保存（版本是安全网，不能成为单点故障）。
    /// changeNote: 前端保存时填写的变更说明（可选），写入每条版本行的 CHANGENOTE。
    /// </summary>
    public static List<TouchedObj> Capture(IViewOperate operate, ArrayList saveList, Hashtable userInfo, string changeNote = null)
    {
      var touched = new List<TouchedObj>();
      try
      {
        var cfgs = GetCfg();
        if (cfgs.Count == 0) return touched;

        DataView verView = null;
        DBHelper helper = null;
        var verSeq = new Dictionary<string, int>();  // 同批同对象递增（防 MAX+1 冲突）
        try
        {
          foreach (var item in saveList)
          {
            if (!(item is DataView)) continue;
            var dv = (DataView)item;
            var res = dv.Resource;
            if (res == null || string.IsNullOrEmpty(res.RESOURCENAME)) continue;
            // 双保险：版本表自身永不纳入（防递归）
            if (res.RESOURCENAME == "TBS_DEV_VERSION" || res.RESOURCENAME == "VSS_DEV_VERSION") continue;
            var cfg = cfgs.Find(c => c.RESOURCENAME == res.RESOURCENAME);
            if (cfg == null) continue;

            string table = res.TABLENAME;
            string keyField = GetKeyField(res);
            if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(keyField)) continue;
            if (helper == null) helper = DB.GetDBHelper();

            foreach (var row in dv.Inserted)
              AppendVersionRow(operate, ref verView, helper, cfg, dv, table, keyField, row, "insert", userInfo, verSeq, touched, changeNote);
            foreach (var row in dv.Updated)
              AppendVersionRow(operate, ref verView, helper, cfg, dv, table, keyField, row, "update", userInfo, verSeq, touched, changeNote);
            foreach (var row in dv.Deleted)
              AppendVersionRow(operate, ref verView, helper, cfg, dv, table, keyField, row, "delete", userInfo, verSeq, touched, changeNote);
          }
          if (verView != null && verView.Inserted.Count > 0)
          {
            saveList.Add(verView);
          }
        }
        finally
        {
          helper?.Dispose();
        }
      }
      catch (Exception ex)
      {
        Logger.Error("DevVersionService.Capture 异常（已跳过版本捕获，业务保存继续）: " + ex.Message, ex);
      }
      return touched;
    }

    /// <summary>
    /// 清理过期版本：每对象保留 MAXVERSIONS 个最新版本（PINNED=1 或有 TAG 的永久保留）。
    /// 在 operate01.Save 成功后调用（非事务，家政清理失败无碍）。
    /// </summary>
    public static void CleanupExpired(List<TouchedObj> touched)
    {
      if (touched == null || touched.Count == 0) return;
      try
      {
        using (var helper = DB.GetDBHelper())
        {
          foreach (var t in touched)
          {
            if (t.MaxVersions <= 0) continue;
            // MySQL 同表 DELETE+子查询需双层嵌套；LIMIT 用内联 int（来源 cfg，安全）
            helper.Execute(
              @"DELETE FROM tss_dev_version
                WHERE OBJTYPE=@t AND OBJID=@id AND PINNED=0 AND (TAG IS NULL OR TAG='')
                  AND VERSION NOT IN (
                    SELECT VERSION FROM (
                      SELECT VERSION FROM tss_dev_version WHERE OBJTYPE=@t2 AND OBJID=@id2
                      ORDER BY VERSION DESC LIMIT " + t.MaxVersions + @"
                    ) keep_
                  )",
              new { t = t.ObjType, id = t.ObjId, t2 = t.ObjType, id2 = t.ObjId });
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("DevVersionService.CleanupExpired 异常: " + ex.Message);
      }
    }

    // ===== 内部实现 =====

    /// <summary>取资源主键字段名（ISKEY=1，默认 ID）</summary>
    private static string GetKeyField(Resource res)
    {
      var key = res.Fields.Find(f => f.ISKEY + "" == "1");
      return key != null ? key.FIELDNAME : "ID";
    }

    /// <summary>生成一条版本行（before/after 镜像 + 递增版本号）</summary>
    private static void AppendVersionRow(IViewOperate operate, ref DataView verView, DBHelper helper,
      VersionCfg cfg, DataView dv, string table, string keyField, ViewRow row, string opType,
      Hashtable userInfo, Dictionary<string, int> verSeq, List<TouchedObj> touched, string changeNote = null)
    {
      var res = dv.Resource;
      string objId = row[keyField] + "";
      if (string.IsNullOrEmpty(objId)) return;

      // 镜像：insert 只有 after，delete 只有 before，update 双镜像
      // 前镜像优先取上一版本的 AFTERCONTENT（版本链连续: v(n).BEFORE == v(n-1).AFTER），
      // 快速保存(SKIPVERSION)不留版本, 其中间改动折叠进下次提交的差异；
      // 无历史版本时（首次纳入/外部渠道改动）回退为查当前 DB 行
      string beforeContent = null, afterContent = null;
      if (opType != "insert")
      {
        beforeContent = QueryLastVersionAfter(helper, cfg.OBJTYPE, objId);
        if (beforeContent == null)
        {
          beforeContent = QueryRowJson(helper, table, keyField, objId);
          if (beforeContent == null) return; // 行已不存在（如重复删除），跳过
        }
      }
      if (opType != "delete")
      {
        afterContent = RowToJson(dv, row);
      }
      // 逻辑删除识别: update 且 ISDELETED 由 0→1 → 按 delete 语义记录
      // (历史显示"删除"而非"修改"; 回滚时行在则写回 BEFORE 镜像即恢复)
      if (opType == "update" && IsLogicalDelete(beforeContent, afterContent))
      {
        opType = "delete";
      }

      int ver = NextVersion(helper, cfg.OBJTYPE, objId, verSeq);
      if (verView == null) verView = new DataView(operate.GetResource("TBS_DEV_VERSION"));
      var vrow = verView.GetAddRow();
      vrow["ID"] = Guid.NewGuid().ToString("N");
      vrow["OBJTYPE"] = cfg.OBJTYPE;
      vrow["OBJID"] = objId;
      vrow["OBJCODE"] = BuildExpr(cfg.CODEEXPR, row);
      vrow["OBJNAME"] = BuildExpr(cfg.NAMEEXPR, row);
      vrow["VERSION"] = ver;
      vrow["OPTYPE"] = opType;
      vrow["BEFORECONTENT"] = beforeContent;
      vrow["AFTERCONTENT"] = afterContent;
      vrow["CHANGENOTE"] = string.IsNullOrEmpty(changeNote) ? null : changeNote;
      vrow["CREATEID"] = userInfo != null ? userInfo["ID"] + "" : "";
      vrow["CREATER"] = userInfo != null ? userInfo["NICKNAME"] + "" : "";
      vrow["CREATETIME"] = DateTime.Now;
      vrow["SRCTABLE"] = table;
      vrow["ISDELETED"] = 0;
      verView.AddRow(vrow);

      if (touched.Find(t => t.ObjType == cfg.OBJTYPE && t.ObjId == objId) == null)
      {
        touched.Add(new TouchedObj { ObjType = cfg.OBJTYPE, ObjId = objId, MaxVersions = cfg.MAXVERSIONS });
      }
    }

    /// <summary>版本号：MAX(VERSION)+1（同批同对象在 verSeq 里继续递增，防唯一键冲突）</summary>
    private static int NextVersion(DBHelper helper, string objType, string objId, Dictionary<string, int> verSeq)
    {
      string key = objType + "|" + objId;
      int next;
      if (verSeq.TryGetValue(key, out next))
      {
        verSeq[key] = next + 1;
        return next + 1;
      }
      int max = Convert.ToInt32(helper.ExecuteScalar(
        "SELECT IFNULL(MAX(VERSION),0) FROM tss_dev_version WHERE OBJTYPE=@t AND OBJID=@id",
        new { t = objType, id = objId }));
      verSeq[key] = max + 1;
      return max + 1;
    }

    /// <summary>判断是否逻辑删除: 前镜像 ISDELETED≠1 且后镜像 ISDELETED=1（快照缺字段/解析失败按否）</summary>
    private static bool IsLogicalDelete(string beforeJson, string afterJson)
    {
      if (string.IsNullOrEmpty(beforeJson) || string.IsNullOrEmpty(afterJson)) return false;
      try
      {
        var before = JsonConvert.DeserializeObject<Dictionary<string, object>>(beforeJson);
        var after = JsonConvert.DeserializeObject<Dictionary<string, object>>(afterJson);
        object bv, av;
        after.TryGetValue("ISDELETED", out av);
        before.TryGetValue("ISDELETED", out bv);
        return (av + "" == "1") && (bv + "" != "1");
      }
      catch
      {
        return false;
      }
    }

    /// <summary>取该对象上一版本的 AFTERCONTENT（版本链前镜像；无历史版本返回 null）</summary>
    private static string QueryLastVersionAfter(DBHelper helper, string objType, string objId)
    {
      var v = helper.ExecuteScalar(
        "SELECT AFTERCONTENT FROM tss_dev_version WHERE OBJTYPE=@t AND OBJID=@id AND AFTERCONTENT IS NOT NULL ORDER BY VERSION DESC LIMIT 1",
        new { t = objType, id = objId });
      return v == null ? null : v + "";
    }

    /// <summary>SELECT 当前行 → JSON（update/delete 的变更前镜像）</summary>
    private static string QueryRowJson(DBHelper helper, string table, string keyField, string objId)
    {
      // table/keyField 来自 ORM 资源元数据（系统内部可信源），objId 参数化
      var row = helper.QueryFirstOrDefault(
        "SELECT * FROM `" + table + "` WHERE `" + keyField + "`=@id LIMIT 1", new { id = objId });
      if (row == null) return null;
      var dict = ((IDictionary<string, object>)row).ToDictionary(kv => kv.Key, kv => kv.Value);
      return JsonConvert.SerializeObject(dict);
    }

    /// <summary>ViewRow → JSON（insert/update 的变更后镜像，只取视图列）</summary>
    private static string RowToJson(DataView dv, ViewRow row)
    {
      var dict = new Dictionary<string, object>();
      foreach (var col in dv.Columns)
      {
        dict[col.Name] = row[col.Name];
      }
      return JsonConvert.SerializeObject(dict);
    }

    /// <summary>按 cfg 表达式从行取 OBJCODE/OBJNAME（逗号分隔多字段，用 / 连接）</summary>
    private static string BuildExpr(string expr, ViewRow row)
    {
      if (string.IsNullOrEmpty(expr)) return null;
      var parts = new List<string>();
      foreach (var f in expr.Split(','))
      {
        string v = row[f.Trim()] + "";
        if (!string.IsNullOrEmpty(v)) parts.Add(v);
      }
      return parts.Count > 0 ? string.Join("/", parts) : null;
    }
  }
}
