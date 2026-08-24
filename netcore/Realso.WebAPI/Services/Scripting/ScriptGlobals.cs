using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Realso.Core.Models;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.WebAPI.Models;

namespace Realso.WebAPI.Services.Scripting
{
  /// <summary>
  /// C# 脚本接口（APITYPE=csharp）的全局上下文：脚本内直接可用的属性与方法。
  /// 数据访问统一走 Db/DbExec/Sql，事务用 using (var t = Trans()) { ... t.Commit(); }。
  /// 返回给前端：Response.SetData(obj) / Response.SetError(msg)。
  /// </summary>
  public class ScriptGlobals
  {
    /// <summary>接口参数（FilterParams 并入 + Params 顶层键 + 系统变量 _USERID_/_EMPID_/_DEPTID_）</summary>
    public Hashtable Params { get; set; }
    /// <summary>当前登录用户（ID/NICKNAME/EMPID/DEPTID）</summary>
    public Hashtable UserInfo { get; set; }
    /// <summary>模块配置（GetPath/GetAPI/GetValue 等）</summary>
    public MOUDLE MD { get; set; }
    /// <summary>响应对象：脚本用它设置返回</summary>
    public ResponseModel Response { get; set; }
    /// <summary>当前模块编码</summary>
    public string ModuleCode { get; set; }
    /// <summary>当前接口编码</summary>
    public string ApiCode { get; set; }
    /// <summary>ORM 操作对象（GetResource/GetNewID/Open 等高级用法）</summary>
    public IViewOperate Operate { get; set; }

    private DBHelper _helper;
    private IDbTransaction _trans;

    private DBHelper Helper
    {
      get
      {
        if (_helper == null) _helper = DB.GetDBHelper();
        if (_helper.Connection.State != ConnectionState.Open) _helper.Connection.Open();
        return _helper;
      }
    }

    /// <summary>查询：Db("SELECT * FROM tbs_x WHERE ID=@id", new { id })</summary>
    public List<dynamic> Db(string sql, object param = null)
    {
      return Helper.Connection.Query(sql, param, _trans).ToList();
    }

    /// <summary>查询首行（无行返回 null）</summary>
    public dynamic DbFirst(string sql, object param = null)
    {
      return Helper.Connection.QueryFirstOrDefault(sql, param, _trans);
    }

    /// <summary>执行 DML：DbExec("UPDATE tbs_x SET STATE=2 WHERE ID=@id", new { id })。有事务时自动并入</summary>
    public int DbExec(string sql, object param = null)
    {
      return Helper.Execute(sql, param, _trans);
    }

    /// <summary>执行标量：DbScalar("SELECT COUNT(1) FROM tbs_x")</summary>
    public object DbScalar(string sql, object param = null)
    {
      return Helper.Connection.ExecuteScalar(sql, param, _trans);
    }

    /// <summary>调用 tss_sql 里的 NVelocity 模板查询：Sql("SS_XXX", new Hashtable{{"ID","1"}})</summary>
    public List<dynamic> Sql(string sqlCode, Hashtable p = null)
    {
      string txt = SQLManage.GetSQL(sqlCode);
      if (string.IsNullOrEmpty(txt)) throw new Exception("SQL模板 " + sqlCode + " 不存在");
      string merged = SQLManage.ParseSQL(txt, p ?? new Hashtable());
      return Db(merged);
    }

    /// <summary>取参数值的便捷方法（转字符串，null→""）</summary>
    public string P(string name)
    {
      return Params != null && Params[name] != null ? Params[name] + "" : "";
    }

    /// <summary>当前用户 ID</summary>
    public string UserId { get { return UserInfo != null && UserInfo["ID"] != null ? UserInfo["ID"] + "" : ""; } }

    /// <summary>开启事务：using (var t = Trans()) { DbExec(...); t.Commit(); }（忘 Commit 自动回滚）</summary>
    public ScriptTransaction Trans()
    {
      if (_trans != null) throw new Exception("已有活动事务，不支持嵌套 Trans()");
      _trans = Helper.BeginTransaction();
      return new ScriptTransaction(this);
    }

    internal void EndTrans(bool commit)
    {
      if (_trans == null) return;
      try
      {
        if (commit) _trans.Commit();
        else _trans.Rollback();
      }
      finally
      {
        _trans.Dispose();
        _trans = null;
      }
    }

    /// <summary>写日志（log4net，排错用）</summary>
    public void Log(string msg)
    {
      Realso.Utils.Logger.Info("[脚本接口 " + (ModuleCode ?? "") + "/" + (ApiCode ?? "") + "] " + msg);
    }

    /// <summary>释放连接（由 doScriptApi 在 finally 调用，脚本无需关心）</summary>
    internal void Cleanup()
    {
      try { if (_trans != null) EndTrans(false); } catch { }
      try { _helper?.Dispose(); } catch { }
      _helper = null;
    }
  }

  /// <summary>脚本事务对象：Commit/Rollback，Dispose 时未提交则回滚</summary>
  public class ScriptTransaction : IDisposable
  {
    private readonly ScriptGlobals _g;
    private bool _done;
    internal ScriptTransaction(ScriptGlobals g) { _g = g; }
    public void Commit() { if (!_done) { _g.EndTrans(true); _done = true; } }
    public void Rollback() { if (!_done) { _g.EndTrans(false); _done = true; } }
    public void Dispose() { if (!_done) Rollback(); }
  }
}
