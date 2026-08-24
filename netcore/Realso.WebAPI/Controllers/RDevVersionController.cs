using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.Data.ORM.Core;
using Realso.WebAPI.Models;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// 开发版本中心控制器（RS_M22）。
  /// 自定义接口（前端必须走 /api/RDevVersion/call/ 路由）：
  ///   A05 rollback — 回滚到指定版本
  ///   A06 current  — 取版本对应对象的当前快照
  ///   A07 mark     — 设置版本 TAG/PINNED
  ///   A08 batchMark — 批量打标（按 OBJTYPE+OBJCODE 范围）
  ///   A09 createRelease — 按 TAG 创建发布包
  ///   A10 deployRelease — 部署发布包
  ///   A11 listReleases  — 查询发布包列表
  /// 标准 A01 查询 / A02 打开走基类 DataController。
  /// </summary>
  public class RDevVersionController : DataController
  {
    protected override void doMyApi(MOUDLE MD, ViewRow row, string APITYPE, Hashtable Params)
    {
      string apiCode = row.GetString("APICODE");
      switch (apiCode)
      {
        case "A05":
          doRollback(MD, row, Params);
          break;
        case "A06":
          doCurrent(MD, row, Params);
          break;
        case "A07":
          doMark(MD, row, Params);
          break;
        case "A08":
          doBatchMark(MD, row, Params);
          break;
        case "A09":
          doCreateRelease(MD, row, Params);
          break;
        case "A10":
          doDeployRelease(MD, row, Params);
          break;
        case "A11":
          doListReleases(MD, row, Params);
          break;
        default:
          base.doMyApi(MD, row, APITYPE, Params);
          break;
      }
    }

    /// <summary>取版本对应对象的当前行快照 JSON（对象已不存在时 exists=false）</summary>
    protected virtual void doCurrent(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string versionId = Params != null ? Params["ID"] + "" : "";
      if (string.IsNullOrEmpty(versionId))
      {
        responseModel.SetError("ID 不能为空");
        return;
      }
      using (var helper = DB.GetDBHelper())
      {
        var v = helper.QueryFirstOrDefault(
          "SELECT OBJID, SRCTABLE FROM tss_dev_version WHERE ID=@id AND ISDELETED=0 LIMIT 1", new { id = versionId });
        if (v == null)
        {
          responseModel.SetError("版本记录不存在");
          return;
        }
        string table = v.SRCTABLE;
        string objId = v.OBJID;
        // 表名来自版本记录（系统内部写入），仍做标识符白名单防篡改
        if (string.IsNullOrEmpty(table) || !Regex.IsMatch(table, "^[a-zA-Z0-9_]+$"))
        {
          responseModel.SetError("版本记录缺少合法的来源表信息（SRCTABLE）");
          return;
        }
        var cur = helper.QueryFirstOrDefault(
          "SELECT * FROM `" + table + "` WHERE ID=@id LIMIT 1", new { id = objId });
        if (cur == null)
        {
          responseModel.SetData(new { exists = false, current = (string)null });
          return;
        }
        var dict = ((IDictionary<string, object>)cur).ToDictionary(kv => kv.Key, kv => kv.Value);
        responseModel.SetData(new { exists = true, current = JsonConvert.SerializeObject(dict) });
      }
    }

    /// <summary>设置版本 TAG（发布标记）/PINNED（置顶）；二者任一的版本永不被过期清理</summary>
    protected virtual void doMark(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string versionId = Params != null ? Params["ID"] + "" : "";
      if (string.IsNullOrEmpty(versionId))
      {
        responseModel.SetError("ID 不能为空");
        return;
      }
      string tag = Params["TAG"] + "";
      int pinned = (Params["PINNED"] + "") == "1" ? 1 : 0;
      using (var helper = DB.GetDBHelper())
      {
        int n = helper.Execute(
          "UPDATE tss_dev_version SET TAG=@tag, PINNED=@pinned WHERE ID=@id",
          new { tag = string.IsNullOrEmpty(tag) ? null : tag, pinned, id = versionId });
        if (n == 0)
        {
          responseModel.SetError("版本记录不存在");
          return;
        }
        responseModel.SetData(new { message = "已标记" });
      }
    }

    /// <summary>
    /// 回滚到指定版本：
    ///   insert → 删除该行；delete → 用 BEFORE 镜像重新插入；update → 用 BEFORE 镜像写回。
    /// 回滚本身写入 OPTYPE=rollback 新版本（镜像对调），支持"回滚的回滚"。
    /// </summary>
    protected virtual void doRollback(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string versionId = Params != null ? Params["ID"] + "" : "";
      if (string.IsNullOrEmpty(versionId))
      {
        responseModel.SetError("ID 不能为空");
        return;
      }
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var v = helper.QueryFirstOrDefault(
          "SELECT * FROM tss_dev_version WHERE ID=@id AND ISDELETED=0 LIMIT 1", new { id = versionId });
        if (v == null)
        {
          responseModel.SetError("版本记录不存在");
          return;
        }
        string objType = v.OBJTYPE;
        string objId = v.OBJID;
        string objCode = v.OBJCODE;
        string objName = v.OBJNAME;
        int ver = (int)v.VERSION;
        string opType = v.OPTYPE;
        string table = v.SRCTABLE;
        string beforeC = v.BEFORECONTENT;
        string afterC = v.AFTERCONTENT;

        if (opType == "rollback")
        {
          responseModel.SetError("该版本本身是回滚记录，请选择原始目标版本回滚");
          return;
        }
        // 表名来自版本记录（系统内部写入），仍做标识符白名单防篡改
        if (string.IsNullOrEmpty(table) || !Regex.IsMatch(table, "^[a-zA-Z0-9_]+$"))
        {
          responseModel.SetError("版本记录缺少合法的来源表信息（SRCTABLE），无法回滚");
          return;
        }
        if (opType != "insert" && string.IsNullOrEmpty(beforeC))
        {
          responseModel.SetError("该版本无变更前快照，无法回滚");
          return;
        }

        helper.Connection.Open();
        using (var trans = helper.BeginTransaction())
        {
          try
          {
            if (opType == "insert")
            {
              // 回滚新增 = 删除该行
              helper.Execute("DELETE FROM `" + table + "` WHERE ID=@id", new { id = objId }, trans);
            }
            else
            {
              var before = JsonConvert.DeserializeObject<Dictionary<string, object>>(beforeC);
              if (opType == "delete")
              {
                // 回滚删除: 行在则按 BEFORE 镜像写回(逻辑删除场景, 恢复 ISDELETED=0 与原内容)，
                //           行不在则重新插入(物理删除场景)
                var cnt = helper.Connection.ExecuteScalar("SELECT COUNT(1) FROM `" + table + "` WHERE ID=@id", new { id = objId }, trans);
                if (Convert.ToInt32(cnt) > 0)
                {
                  var setCols = before.Keys.Where(k => k != "ID").ToList();
                  string updateSql = "UPDATE `" + table + "` SET " +
                    string.Join(",", setCols.Select(c => "`" + c + "`=@" + c)) + " WHERE ID=@ID";
                  helper.Execute(updateSql, before, trans);
                }
                else
                {
                  var cols = before.Keys.ToList();
                  string insertSql = "INSERT INTO `" + table + "` (" +
                    string.Join(",", cols.Select(c => "`" + c + "`")) + ") VALUES (" +
                    string.Join(",", cols.Select(c => "@" + c)) + ")";
                  helper.Execute(insertSql, before, trans);
                }
              }
              else
              {
                // 回滚修改 = 用 BEFORE 镜像写回（主键除外）
                var setCols = before.Keys.Where(k => k != "ID").ToList();
                string updateSql = "UPDATE `" + table + "` SET " +
                  string.Join(",", setCols.Select(c => "`" + c + "`=@" + c)) + " WHERE ID=@ID";
                helper.Execute(updateSql, before, trans);
              }
            }

            // 回滚本身生成新版本（镜像对调：当前状态→回滚后状态）
            int newVer = Convert.ToInt32(helper.Connection.ExecuteScalar(
              "SELECT IFNULL(MAX(VERSION),0)+1 FROM tss_dev_version WHERE OBJTYPE=@t AND OBJID=@id",
              new { t = objType, id = objId }, trans));
            helper.Execute(
              @"INSERT INTO tss_dev_version
                (ID, OBJTYPE, OBJID, OBJCODE, OBJNAME, VERSION, OPTYPE, BEFORECONTENT, AFTERCONTENT,
                 CHANGENOTE, CREATEID, CREATER, CREATETIME, SRCTABLE, ISDELETED)
                VALUES (@ID, @OBJTYPE, @OBJID, @OBJCODE, @OBJNAME, @VERSION, 'rollback', @BEFOREC, @AFTERC,
                 @NOTE, @CBY, @CBYN, @CT, @SRCTABLE, 0)",
              new
              {
                ID = Guid.NewGuid().ToString("N"),
                OBJTYPE = objType,
                OBJID = objId,
                OBJCODE = objCode,
                OBJNAME = objName,
                VERSION = newVer,
                BEFOREC = afterC,
                AFTERC = beforeC,
                NOTE = "回滚到 v" + ver,
                CBY = userInfo != null ? userInfo["ID"] + "" : "",
                CBYN = userInfo != null ? userInfo["NICKNAME"] + "" : "",
                CT = DateTime.Now,
                SRCTABLE = table
              }, trans);
            trans.Commit();
            responseModel.SetData(new { version = newVer, message = "已回滚到 v" + ver + "（生成新版本 v" + newVer + "）" });
          }
          catch (Exception ex)
          {
            try { trans.Rollback(); } catch { }
            responseModel.SetError("回滚失败（已还原）: " + ex.Message);
          }
        }
      }
    }

    /// <summary>A08 批量打标：按 OBJTYPE+OBJCODE 范围批量设置 TAG/PINNED</summary>
    protected virtual void doBatchMark(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string objType = Params != null ? Params["OBJTYPE"] + "" : "";
      string objCode = Params != null ? Params["OBJCODE"] + "" : "";
      string tag = Params != null ? Params["TAG"] + "" : "";
      int pinned = (Params["PINNED"] + "") == "1" ? 1 : 0;
      if (string.IsNullOrEmpty(objType) && string.IsNullOrEmpty(objCode) && string.IsNullOrEmpty(tag) && pinned == 0)
      {
        responseModel.SetError("至少提供一个筛选条件或标记值");
        return;
      }
      using (var helper = DB.GetDBHelper())
      {
        string sql = "UPDATE tss_dev_version SET ";
        var sets = new List<string>();
        var dbParams = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(tag)) { sets.Add("TAG=@tag"); dbParams["tag"] = tag; }
        if (pinned > 0) { sets.Add("PINNED=@pinned"); dbParams["pinned"] = pinned; }
        if (sets.Count == 0) { responseModel.SetError("TAG 或 PINNED 至少提供一个"); return; }
        sql += string.Join(",", sets);
        sql += " WHERE ISDELETED=0";
        if (!string.IsNullOrEmpty(objType)) { sql += " AND OBJTYPE=@objType"; dbParams["objType"] = objType; }
        if (!string.IsNullOrEmpty(objCode)) { sql += " AND OBJCODE LIKE @objCode"; dbParams["objCode"] = objCode + "%"; }
        int n = helper.Execute(sql, dbParams);
        responseModel.SetData(new { affected = n });
      }
    }

    /// <summary>A09 按 TAG 创建发布包：收集 TAG 对应的版本对象 → 生成 .aidev.sql 格式脚本 → 存入 tss_release</summary>
    protected virtual void doCreateRelease(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string tag = Params != null ? Params["TAG"] + "" : "";
      string releaseCode = Params != null ? Params["RELEASECODE"] + "" : "";
      string releaseName = Params != null ? Params["RELEASENAME"] + "" : "";
      string remark = Params != null ? Params["REMARK"] + "" : "";
      if (string.IsNullOrEmpty(tag)) { responseModel.SetError("TAG 不能为空"); return; }
      if (string.IsNullOrEmpty(releaseCode)) { responseModel.SetError("RELEASECODE 不能为空"); return; }
      if (string.IsNullOrEmpty(releaseName)) { responseModel.SetError("RELEASENAME 不能为空"); return; }

      using (var helper = DB.GetDBHelper())
      {
        // 1. 按 TAG 收集版本记录中 OBJTYPE+OBJID+SRCTABLE 去重列表
        var versions = helper.Query(
          "SELECT DISTINCT OBJTYPE, OBJID, OBJCODE, OBJNAME, SRCTABLE FROM tss_dev_version WHERE TAG=@tag AND ISDELETED=0",
          new { tag }).ToList();
        if (versions.Count == 0)
        {
          responseModel.SetError("TAG=" + tag + " 下无版本记录");
          return;
        }

        // 2. 按 SRCTABLE 分组收集对象，按依赖序生成幂等 INSERT
        var sb = new StringBuilder();
        sb.AppendLine("-- @META version=1 tag=" + tag + " generated=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("-- 发布包: " + releaseCode + " " + releaseName);
        sb.AppendLine();

        // 依赖序遍历：tss_resource → tss_resfield → tss_resfilter → tss_resuipc → tss_moudle → tss_moudlepath → tss_moudlepathrel → tss_moudleapi → tss_module_page → tss_module_button → tss_func → tss_funcpoint → tss_code_asset → tss_dict → tss_dictitem
        string[] tableOrder = {
          "tss_resource", "tss_resfield", "tss_resfilter", "tss_resuipc",
          "tss_moudle", "tss_moudlepath", "tss_moudlepathrel", "tss_moudleapi",
          "tss_module_page", "tss_module_button", "tss_func", "tss_funcpoint",
          "tss_code_asset", "tss_dict", "tss_dictitem"
        };

        // 按版本记录的 SRCTABLE 分组
        var byTable = new Dictionary<string, List<dynamic>>();
        foreach (var v in versions)
        {
          string table = v.SRCTABLE;
          if (string.IsNullOrEmpty(table)) continue;
          if (!byTable.ContainsKey(table)) byTable[table] = new List<dynamic>();
          byTable[table].Add(v);
        }

        int itemCount = 0;
        foreach (string table in tableOrder)
        {
          if (!byTable.ContainsKey(table)) continue;
          var objs = byTable[table];
          sb.AppendLine("-- === " + table + " (" + objs.Count + " 条) ===");
          foreach (var obj in objs)
          {
            string objId = obj.OBJID;
            if (string.IsNullOrEmpty(objId)) continue;
            // 白名单校验
            if (!Regex.IsMatch(table, "^[a-zA-Z0-9_]+$")) continue;
            var cur = helper.QueryFirstOrDefault("SELECT * FROM `" + table + "` WHERE ID=@id LIMIT 1", new { id = objId });
            if (cur == null) continue;
            var dict = ((IDictionary<string, object>)cur).ToDictionary(kv => kv.Key, kv => kv.Value);
            // 生成幂等 INSERT（INSERT IGNORE 避免主键冲突）
            var cols = dict.Keys.ToList();
            var insertSql = "INSERT IGNORE INTO `" + table + "` (" +
              string.Join(",", cols.Select(c => "`" + c + "`")) + ") VALUES (" +
              string.Join(",", cols.Select(c => FormatValue(dict[c]))) + ");";
            sb.AppendLine(insertSql);
            itemCount++;
          }
          sb.AppendLine();
        }

        // 3. 计算 SCRIPTHASH
        string scriptContent = sb.ToString();
        string scriptHash;
        using (var sha = SHA256.Create())
        {
          var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(scriptContent));
          scriptHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // 4. INSERT tss_release
        string releaseId = Guid.NewGuid().ToString("N");
        string userId = userInfo != null ? userInfo["ID"] + "" : "";
        string userName = userInfo != null ? userInfo["NICKNAME"] + "" : "";
        string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        helper.Execute(
          @"INSERT INTO tss_release (ID, RELEASECODE, RELEASENAME, TAG, OBJCOUNT, STATUS, SCRIPTCONTENT, SCRIPTHASH, REMARK, CREATEID, CREATER, CREATETIME, ISDELETED)
            VALUES (@ID, @RC, @RN, @TAG, @OC, 'draft', @SC, @SH, @RM, @CI, @CN, @CT, 0)",
          new { ID = releaseId, RC = releaseCode, RN = releaseName, TAG = tag, OC = itemCount, SC = scriptContent, SH = scriptHash, RM = remark, CI = userId, CN = userName, CT = now });

        responseModel.SetData(new { releaseId, objCount = itemCount, scriptLen = scriptContent.Length });
      }
    }

    /// <summary>格式化值为 SQL 字面量</summary>
    private static string FormatValue(object val)
    {
      if (val == null || val == DBNull.Value) return "NULL";
      if (val is bool b) return b ? "1" : "0";
      if (val is int || val is long || val is decimal || val is double || val is float) return val.ToString();
      string s = val.ToString();
      if (string.IsNullOrEmpty(s)) return "NULL";
      // 含单引号/分号/过长 → 0x HEX
      if (s.Contains("'") || s.Contains(";") || s.Length > 500)
        return "0x" + BitConverter.ToString(Encoding.UTF8.GetBytes(s)).Replace("-", "");
      return "'" + s.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
    }

    /// <summary>A10 部署发布包：读 SCRIPTCONTENT → UpgradeExecutor.Import</summary>
    protected virtual void doDeployRelease(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string releaseId = Params != null ? Params["RELEASEID"] + "" : "";
      if (string.IsNullOrEmpty(releaseId)) { responseModel.SetError("RELEASEID 不能为空"); return; }

      using (var helper = DB.GetDBHelper())
      {
        var rel = helper.QueryFirstOrDefault(
          "SELECT ID, RELEASECODE, RELEASENAME, STATUS, SCRIPTCONTENT, SCRIPTHASH FROM tss_release WHERE ID=@id AND ISDELETED=0 LIMIT 1",
          new { id = releaseId });
        if (rel == null) { responseModel.SetError("发布包不存在"); return; }

        string scriptContent = rel.SCRIPTCONTENT;
        string storedHash = rel.SCRIPTHASH;
        // 完整性校验
        string currentHash;
        using (var sha = SHA256.Create())
        {
          var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(scriptContent));
          currentHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        if (currentHash != storedHash)
        {
          responseModel.SetError("发布包脚本哈希校验失败，内容可能被篡改");
          return;
        }

        // 复用 UpgradeExecutor.Import
        try
        {
          var executor = new Realso.WebAPI.Services.AiDev.UpgradeExecutor();
          string upgradeId = executor.Import(scriptContent, userInfo?["ID"] + "");
          // 更新状态
          helper.Execute("UPDATE tss_release SET STATUS='published' WHERE ID=@id", new { id = releaseId });
          responseModel.SetData(new { upgradeId, releaseCode = rel.RELEASECODE + "", message = "导入成功，请到升级中心执行部署" });
        }
        catch (Exception ex)
        {
          responseModel.SetError("导入失败: " + ex.Message);
        }
      }
    }

    /// <summary>A11 查询发布包列表</summary>
    protected virtual void doListReleases(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string status = Params != null ? Params["STATUS"] + "" : "";
      string tag = Params != null ? Params["TAG"] + "" : "";
      string input = Params != null ? Params["INPUT"] + "" : "";
      int pageSize = 50;
      if (Params != null && Params["PageSize"] != null) int.TryParse(Params["PageSize"] + "", out pageSize);
      if (pageSize <= 0 || pageSize > 200) pageSize = 50;

      using (var helper = DB.GetDBHelper())
      {
        string sql = "SELECT ID, RELEASECODE, RELEASENAME, TAG, OBJCOUNT, STATUS, CREATER, CREATETIME FROM tss_release WHERE ISDELETED=0";
        var dbParams = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(status)) { sql += " AND STATUS=@status"; dbParams["status"] = status; }
        if (!string.IsNullOrEmpty(tag)) { sql += " AND TAG=@tag"; dbParams["tag"] = tag; }
        if (!string.IsNullOrEmpty(input)) { sql += " AND (RELEASECODE LIKE @input OR RELEASENAME LIKE @input)"; dbParams["input"] = "%" + input + "%"; }
        sql += " ORDER BY CREATETIME DESC LIMIT " + pageSize;
        var list = helper.Query(sql, dbParams).ToList();
        responseModel.SetData(list);
      }
    }
  }
}
