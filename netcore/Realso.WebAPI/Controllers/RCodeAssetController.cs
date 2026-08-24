using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Realso.Core.Models;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.WebAPI.Models;
using Realso.WebAPI.Services.Scripting;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// 代码资产测试控制器（RS_M17）。自定义接口（前端走 /api/RCodeAsset/call/ 路由）：
  ///   A07 testSql    — SQL 模板试运行（NVelocity 注参后执行，仅允许 SELECT/WITH/SHOW，LIMIT 200）
  ///   A08 testScript — C# 脚本试运行（源码可未保存，参数注入 ScriptGlobals，返回脚本 Response）
  /// 入参统一：CODE(资产编码,可选) / SOURCE(源码,优先于 CODE) / VALUES(JSON 对象,参数值)
  /// </summary>
  public class RCodeAssetController : DataController
  {
    protected override void doMyApi(MOUDLE MD, ViewRow row, string APITYPE, Hashtable Params)
    {
      string apiCode = row.GetString("APICODE");
      switch (apiCode)
      {
        case "A07":
          doTestSql(MD, row, Params);
          break;
        case "A08":
          doTestScript(MD, row, Params);
          break;
        case "A09":
          doAssetApis(MD, row, Params);
          break;
        default:
          base.doMyApi(MD, row, APITYPE, Params);
          break;
      }
    }

    /// <summary>取源码：SOURCE 优先（编辑器未保存内容），否则按 CODE 从 tss_code_asset 查</summary>
    private string GetSource(Hashtable Params, string assetType, out string error)
    {
      error = null;
      string source = Params != null ? Params["SOURCE"] + "" : "";
      if (!string.IsNullOrWhiteSpace(source)) return source;
      string code = Params != null ? Params["CODE"] + "" : "";
      if (string.IsNullOrEmpty(code))
      {
        error = "SOURCE/CODE 不能都为空";
        return null;
      }
      using (var helper = DB.GetDBHelper())
      {
        source = helper.QueryFirstOrDefault<string>(
          "SELECT SOURCECODE FROM tss_code_asset WHERE ASSETTYPE=@t AND CODE=@c AND ISDELETED=0 LIMIT 1",
          new { t = assetType, c = code });
      }
      if (source == null) error = "资产不存在: " + code;
      return source;
    }

    /// <summary>取参数值表（VALUES JSON 对象）+ 注入系统变量</summary>
    private Hashtable GetValues(Hashtable Params)
    {
      var values = new Hashtable();
      if (Params != null && Params["VALUES"] is JObject jo)
      {
        foreach (var p in jo.Properties()) values[p.Name] = p.Value + "";
      }
      if (Params != null && Params["VALUES"] is Hashtable ht)
      {
        foreach (string k in ht.Keys) values[k] = ht[k];
      }
      if (userInfo != null)
      {
        values["_USERID_"] = userInfo["ID"];
        values["_EMPID_"] = userInfo["EMPID"];
        values["_DEPTID_"] = userInfo["DEPTID"];
      }
      return values;
    }

    /// <summary>A07：SQL 模板试运行（仅查询，防写）</summary>
    protected virtual void doTestSql(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string error;
      string source = GetSource(Params, "sql", out error);
      if (source == null)
      {
        responseModel.SetError(error);
        return;
      }
      // 剥注释后必须 SELECT/WITH/SHOW 开头（测试只允许查询，DML/DDL 拒绝）
      string noComments = Regex.Replace(Regex.Replace(source, @"/\*[\s\S]*?\*/", " "), @"--[^\n]*", " ").Trim();
      if (!Regex.IsMatch(noComments, "^(SELECT|WITH|SHOW)", RegexOptions.IgnoreCase))
      {
        responseModel.SetError("测试仅允许 SELECT/WITH/SHOW 查询语句");
        return;
      }
      var values = GetValues(Params);
      try
      {
        string merged = SQLManage.ParseSQL(source, values).Trim().TrimEnd(';');
        using (var helper = DB.GetDBHelper())
        {
          var rows = helper.Query("SELECT * FROM (" + merged + ") T LIMIT 200",
            DBHelper.getParameters(values)).ToList();
          responseModel.SetData(new
          {
            sql = merged,
            count = rows.Count,
            columns = rows.Count > 0 ? ((IDictionary<string, object>)rows[0]).Keys.ToList() : new List<string>(),
            rows = rows
          });
        }
      }
      catch (Exception ex)
      {
        responseModel.SetError("SQL 执行失败: " + ex.Message);
      }
    }

    /// <summary>A09：查资产已关联的模块接口（测试面板"接口执行"模式下拉用）</summary>
    protected virtual void doAssetApis(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string code = Params != null ? Params["CODE"] + "" : "";
      if (string.IsNullOrEmpty(code))
      {
        responseModel.SetError("CODE 不能为空");
        return;
      }
      using (var helper = DB.GetDBHelper())
      {
        var rows = helper.Query(
          @"SELECT A.APICODE AS apiCode, M.MODULECODE AS moduleCode, A.APINAME AS apiName, A.APITYPE AS apiType
            FROM tss_moudleapi A JOIN tss_moudle M ON M.ID=A.MODULEID
            WHERE A.SCRIPTCODE=@code OR A.SQLID=@code ORDER BY M.MODULECODE, A.APICODE",
          new { code }).ToList();
        responseModel.SetData(new { count = rows.Count, items = rows });
      }
    }

    /// <summary>A08：C# 脚本试运行（返回脚本自己的 Response）</summary>
    protected virtual void doTestScript(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string error;
      string source = GetSource(Params, "csharp", out error);
      if (source == null)
      {
        responseModel.SetError(error);
        return;
      }
      var testResp = new ResponseModel();
      var globals = new ScriptGlobals
      {
        Params = GetValues(Params),
        UserInfo = userInfo,
        MD = MD,
        Response = testResp,
        ModuleCode = MD != null ? MD.GetValue("MODULECODE") : "",
        ApiCode = "TEST",
        Operate = this.operate01
      };
      try
      {
        bool ok = CSharpScriptEngine.ExecuteSource(source, globals, out error);
        if (!ok)
        {
          responseModel.SetError(error);
          return;
        }
        responseModel.SetData(new
        {
          code = testResp.Code,
          message = testResp.Message,
          data = testResp.Data
        });
      }
      finally
      {
        globals.Cleanup();
      }
    }
  }
}
