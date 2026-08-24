using System.Reflection.Emit;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.ORM;
using Realso.WebAPI.Models;
using Realso.WebAPI.Services;
using System.Web.Http;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using Microsoft.AspNetCore.Authorization;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Realso.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dapper;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  //[ApiController]
  [Authorize]
  public class DataController : BaseControl
  {
    //初始化模块
    //参数：模块ID
    [HttpPost("init")]
    public IActionResult InitData()
    {
      return Ok();
    }

    //打开数据
    //参数：模块ID、接口编码、参数
    [HttpPost("call/{modulename}/{apicode}")]
    [EnableCors("AllowHeaders")]
    public IActionResult Call(string ModuleName, string ApiCode, [FromForm] string Api, [FromForm] Hashtable Params)
    {
      try
      {
        MOUDLE MD = new MOUDLE(this.operate01);
        MD.Open(ModuleName);
        ViewRow row = MD.GetAPI(ApiCode);
        if (row == null)
        {
          responseModel.SetError("接口编码不存在！");
          return this.doResponse();
        }
        string APITYPE = row.GetString("APITYPE");
        switch (APITYPE)
        {
          case "query":
            this.doQuery(MD, row, Params);
            break;
          case "open":
            this.doOpen(MD, row, Params);
            break;
          case "getid":
            this.doGetId(MD, row, Params);
            break;
          case "getBillCode":
            this.doGetBillCode(MD, row, Params);
            break;
          case "save":
            this.doSave(MD, row, Params);
            break;
          case "delete":
            this.doDelete(MD, row, Params);
            break;
          case "delete2":
            this.doDelete2(MD, row, Params);
            break;
          case "update":
            this.doUpdate(MD, row, Params);
            break;
          case "submit":
            this.doSubmit(MD, row, Params);
            break;
          case "reSubmit":
            this.doReSubmit(MD, row, Params);
            break;
          case "check":
            this.doCheck(MD, row, Params);
            break;
          case "reCheck":
            this.doReCheck(MD, row, Params);
            break;
          case "verify":
            this.doVerify(MD, row, Params);
            break;
          case "reVerify":
            this.doReVerify(MD, row, Params);
            break;
          case "batchSubmit":
            this.doBatchSubmit(MD, row, Params);
            break;
          case "batchReSubmit":
            this.doBatchReSubmit(MD, row, Params);
            break;
          case "batchCheck":
            this.doBatchCheck(MD, row, Params);
            break;
          case "batchCheckReject":
            this.doBatchCheckReject(MD, row, Params);
            break;
          case "batchReCheck":
            this.doBatchReCheck(MD, row, Params);
            break;
          case "batchVerify":
            this.doBatchVerify(MD, row, Params);
            break;
          case "batchVerifyReject":
            this.doBatchVerifyReject(MD, row, Params);
            break;
          case "batchReVerify":
            this.doBatchReVerify(MD, row, Params);
            break;
          case "sql":
            this.doSqlApi(MD, row, Params);
            break;
          case "csharp":
            this.doScriptApi(MD, row, Params);
            break;
          case "script":
            this.doScriptFlowApi(MD, row, Params);
            break;
          default:
            this.doMyApi(MD, row, APITYPE, Params);
            break;
        }
        return this.doResponse();
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
        return this.doResponse();
      }
    }

    protected virtual void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      responseModel.SetError("接口类型:" + APITYPE + "不存在！");
    }

    /// <summary>
    /// APITYPE=sql：SQL 脚本接口（自定义业务接口元数据化，替代为简单 SQL 逻辑写 C# Controller）。
    /// tss_moudleapi.SQLID 指向 tss_sql.SQLCODE（NVelocity 模板，禁单引号铁律同过滤器）；
    /// 参数：FilterParams 优先 + Params 顶层键并入 + 系统变量 @_USERID_/@_EMPID_/@_DEPTID_；
    /// 多语句自行开事务执行（不能用 operate01.Save 的 string 通道——该通道不在事务内）；
    /// SELECT 语句返回最后结果集，否则返回受影响行数；DDL 黑名单（DROP/ALTER/TRUNCATE/CREATE/GRANT/REVOKE/RENAME）。
    /// </summary>
    protected virtual void doSqlApi(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string sqlId = row.GetString("SQLID");
      if (string.IsNullOrEmpty(sqlId))
      {
        responseModel.SetError("接口 " + row.GetString("APICODE") + " 未配置 SQLID（APITYPE=sql 必须指向 tss_sql.SQLCODE）");
        return;
      }
      string sqlTxt = SQLManage.GetSQL(sqlId);
      if (string.IsNullOrEmpty(sqlTxt))
      {
        responseModel.SetError("SQL模板 " + sqlId + " 不存在（tss_sql 查无 SQLCODE=" + sqlId + "）");
        return;
      }

      // 参数组装：Params 顶层键先入，FilterParams 覆盖；注入系统变量
      Hashtable sqlParams = new Hashtable();
      if (Params != null)
      {
        foreach (string k in Params.Keys)
        {
          if (k == "FilterParams") continue;
          sqlParams[k] = Params[k];
        }
        if (Params["FilterParams"] is Hashtable fp)
        {
          foreach (string k in fp.Keys) sqlParams[k] = fp[k];
        }
      }
      if (userInfo != null)
      {
        sqlParams["_USERID_"] = userInfo["ID"];
        sqlParams["_EMPID_"] = userInfo["EMPID"];
        sqlParams["_DEPTID_"] = userInfo["DEPTID"];
      }

      // NVelocity 注参
      string sql;
      try
      {
        sql = SQLManage.ParseSQL(sqlTxt, sqlParams);
      }
      catch (Exception ex)
      {
        responseModel.SetError("SQL模板 " + sqlId + " 解析失败: " + ex.Message);
        return;
      }

      // 拆分语句 + DDL 黑名单预检
      var stmts = SqlScriptHelper.SplitSqlStatements(sql);
      if (stmts.Count == 0)
      {
        responseModel.SetError("SQL模板 " + sqlId + " 解析后无可执行语句");
        return;
      }
      foreach (var s in stmts)
      {
        string ddl = SqlScriptHelper.MatchDdlKeyword(s);
        if (ddl != null)
        {
          responseModel.SetError("脚本接口禁止 DDL（命中 " + ddl + "），结构变更请走变更包/升级包通道");
          return;
        }
      }

      // 单事务执行（SELECT 也在事务内，保证能看到前序语句的未提交变更）
      // 参数随语句传给 Dapper（@VAR 命名参数，如 SQL 模板里的 @MODULECODE/@IDS 才能生效）
      object dapperParams = DBHelper.getParameters(sqlParams);
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        helper.Connection.Open();
        using (var trans = helper.BeginTransaction())
        {
          try
          {
            int affected = 0;
            IEnumerable<dynamic> lastResult = null;
            foreach (var s in stmts)
            {
              if (SqlScriptHelper.IsSelect(s))
              {
                lastResult = helper.Connection.Query(s, dapperParams, trans).ToList();
              }
              else
              {
                affected += helper.Execute(s, dapperParams, trans);
              }
            }
            trans.Commit();
            responseModel.SetData(lastResult != null ? (object)lastResult : new { affected });
          }
          catch (Exception ex)
          {
            try { trans.Rollback(); } catch { }
            responseModel.SetError("脚本接口执行失败（已回滚）: " + ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// APITYPE=csharp：在线 C# 脚本接口（复杂自定义逻辑的元数据化，doMyApi 的在线等价物）。
    /// tss_moudleapi.SCRIPTCODE 指向 tss_api_script.SCRIPTCODE；Roslyn 运行时编译 + VERSION 热更新；
    /// 脚本上下文 ScriptGlobals（Params/UserInfo/MD/Response/Db/DbExec/Trans/Sql/Log）。
    /// 脚本按"管理员级可信代码"对待：编辑须独立权限点，AI 产出须走变更包确认。
    /// </summary>
    protected virtual void doScriptApi(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string scriptCode = row.GetString("SCRIPTCODE");
      if (string.IsNullOrEmpty(scriptCode))
      {
        responseModel.SetError("接口 " + row.GetString("APICODE") + " 未配置 SCRIPTCODE（APITYPE=csharp 必须指向 tss_api_script.SCRIPTCODE）");
        return;
      }

      // 参数组装（与 doSqlApi 一致：顶层键 + FilterParams 覆盖 + 系统变量）
      Hashtable scriptParams = new Hashtable();
      if (Params != null)
      {
        foreach (string k in Params.Keys)
        {
          if (k == "FilterParams") continue;
          scriptParams[k] = Params[k];
        }
        if (Params["FilterParams"] is Hashtable fp)
        {
          foreach (string k in fp.Keys) scriptParams[k] = fp[k];
        }
      }
      if (userInfo != null)
      {
        scriptParams["_USERID_"] = userInfo["ID"];
        scriptParams["_EMPID_"] = userInfo["EMPID"];
        scriptParams["_DEPTID_"] = userInfo["DEPTID"];
      }

      var globals = new Realso.WebAPI.Services.Scripting.ScriptGlobals
      {
        Params = scriptParams,
        UserInfo = userInfo,
        MD = MD,
        Response = responseModel,
        ModuleCode = MD.GetValue("MODULECODE"),
        ApiCode = row.GetString("APICODE"),
        Operate = this.operate01
      };
      try
      {
        string error;
        bool ok = Realso.WebAPI.Services.Scripting.CSharpScriptEngine.Execute(scriptCode, globals, out error);
        if (!ok) responseModel.SetError(error);
        // 成功时返回值由脚本自行 Response.SetData/SetError 设置
      }
      finally
      {
        globals.Cleanup();
      }
    }

    /// <summary>
    /// APITYPE=script：声明式多步骤接口编排。
    /// APIPARAM 存 JSON 步骤数组，循环执行步骤，步骤间通过 StepContext 共享变量。
    /// 步骤类型：sql(执行SQL模板)/query(模块自身查询)/if(条件跳转)/update(执行DML SQL)/return(指定返回数据)。
    /// 单事务执行，任一步骤失败整体回滚。条件跳转支持简单表达式（&&/||/!/>/</==/!=）。
    /// </summary>
    protected virtual void doScriptFlowApi(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string apiParam = row.GetString("APIPARAM");
      if (string.IsNullOrEmpty(apiParam))
      {
        responseModel.SetError("接口 " + row.GetString("APICODE") + " 未配置 APIPARAM（APITYPE=script 需要步骤 JSON）");
        return;
      }

      // 解析步骤数组
      JArray steps;
      try
      {
        var parsed = JToken.Parse(apiParam);
        if (parsed is JArray) steps = (JArray)parsed;
        else { responseModel.SetError("APIPARAM 不是合法的 JSON 数组"); return; }
      }
      catch (Exception ex)
      {
        responseModel.SetError("APIPARAM JSON 解析失败: " + ex.Message);
        return;
      }
      if (steps.Count == 0)
      {
        responseModel.SetError("APIPARAM 步骤数组为空");
        return;
      }

      // 参数组装（与 doSqlApi 一致）
      Hashtable flowParams = new Hashtable();
      if (Params != null)
      {
        foreach (string k in Params.Keys)
        {
          if (k == "FilterParams") continue;
          flowParams[k] = Params[k];
        }
        if (Params["FilterParams"] is Hashtable fp)
        {
          foreach (string k in fp.Keys) flowParams[k] = fp[k];
        }
      }
      if (userInfo != null)
      {
        flowParams["_USERID_"] = userInfo["ID"];
        flowParams["_EMPID_"] = userInfo["EMPID"];
        flowParams["_DEPTID_"] = userInfo["DEPTID"];
      }

      // StepContext：步骤间共享变量
      var context = new Dictionary<string, object>();
      object lastResult = null;
      string returnKey = null;

      // 单事务执行
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        helper.Connection.Open();
        using (var trans = helper.BeginTransaction())
        {
          try
          {
            int i = 0;
            while (i < steps.Count)
            {
              var step = steps[i] as JObject;
              if (step == null) { i++; continue; }
              string type = step["type"]?.ToString()?.ToLower() ?? "";

              switch (type)
              {
                case "sql":
                case "update":
                  {
                    string sqlCode = step["sqlCode"]?.ToString();
                    if (string.IsNullOrEmpty(sqlCode)) throw new Exception("步骤 " + i + " 缺少 sqlCode");
                    string sqlTxt = SQLManage.GetSQL(sqlCode);
                    if (string.IsNullOrEmpty(sqlTxt)) throw new Exception("SQL模板 " + sqlCode + " 不存在");

                    // NVelocity 注参：原始参数 + StepContext 变量注入（@_STEP_key_ 格式）
                    Hashtable nvParams = new Hashtable();
                    foreach (string k in flowParams.Keys) nvParams[k] = flowParams[k];
                    foreach (var kv in context)
                    {
                      nvParams["_STEP_" + kv.Key + "_"] = kv.Value;
                    }
                    string sql = SQLManage.ParseSQL(sqlTxt, nvParams);

                    var stmts = Realso.Utils.SqlScriptHelper.SplitSqlStatements(sql);
                    foreach (var s in stmts)
                    {
                      string ddl = Realso.Utils.SqlScriptHelper.MatchDdlKeyword(s);
                      if (ddl != null) throw new Exception("步骤 " + i + " 含 DDL(" + ddl + ")，脚本编排禁止 DDL");
                    }

                    object dapperParams = DBHelper.getParameters(nvParams);
                    int affected = 0;
                    IEnumerable<dynamic> queryResult = null;
                    foreach (var s in stmts)
                    {
                      if (Realso.Utils.SqlScriptHelper.IsSelect(s))
                        queryResult = helper.Connection.Query(s, dapperParams, trans).ToList();
                      else
                        affected += helper.Execute(s, dapperParams, trans);
                    }
                    var outputKey = step["output"]?.ToString();
                    var stepResult = queryResult != null ? (object)new { rows = queryResult, count = (queryResult as List<dynamic>)?.Count ?? 0 } : (object)new { affected };
                    if (!string.IsNullOrEmpty(outputKey)) context[outputKey] = stepResult;
                    lastResult = stepResult;
                    break;
                  }
                case "query":
                  {
                    string apiCode = step["apiCode"]?.ToString();
                    if (string.IsNullOrEmpty(apiCode)) throw new Exception("步骤 " + i + " 缺少 apiCode");
                    // 复用模块自身 query 逻辑
                    var apiRow = MD.GetAPI(apiCode);
                    if (apiRow == null) throw new Exception("步骤 " + i + " 接口 " + apiCode + " 不存在");

                    string pathName = apiRow.GetString("PATHNAME");
                    string resourceId = apiRow.GetString("RESOURCEID");
                    if (!string.IsNullOrEmpty(pathName))
                    {
                      ViewRow pathRow = MD.GetPath(pathName);
                      if (pathRow != null) resourceId = pathRow.GetString("RESOURCEID");
                    }
                    BaseModel model = GetModel(pathName, resourceId);
                    Hashtable queryParams = new Hashtable();
                    foreach (string k in flowParams.Keys) queryParams[k] = flowParams[k];
                    if (flowParams["FilterParams"] is Hashtable fp2)
                    {
                      var fpClone = new Hashtable();
                      foreach (string k in fp2.Keys) fpClone[k] = fp2[k];
                      fpClone["_USERID_"] = userInfo?["ID"];
                      fpClone["_EMPID_"] = userInfo?["EMPID"];
                      fpClone["_DEPTID_"] = userInfo?["DEPTID"];
                      queryParams["FilterParams"] = fpClone;
                    }
                    else
                    {
                      var fpNew = new Hashtable();
                      fpNew["_USERID_"] = userInfo?["ID"];
                      fpNew["_EMPID_"] = userInfo?["EMPID"];
                      fpNew["_DEPTID_"] = userInfo?["DEPTID"];
                      queryParams["FilterParams"] = fpNew;
                    }
                    queryParams["FILTERCODE"] = apiRow["FILTERCODE"];
                    QueryInfo qi = GetQueryInfo(queryParams);
                    var queryResult = model.Query(qi);
                    var outputKey = step["output"]?.ToString();
                    var stepResult = (object)new { rows = queryResult?.Items, count = queryResult?.Items?.Count ?? 0, total = queryResult?.TotalCount };
                    if (!string.IsNullOrEmpty(outputKey)) context[outputKey] = stepResult;
                    lastResult = stepResult;
                    break;
                  }
                case "if":
                  {
                    string cond = step["cond"]?.ToString();
                    int? gotoStep = step["goto"]?.Type == JTokenType.Integer ? (int?)step["goto"] : null;
                    if (string.IsNullOrEmpty(cond) || gotoStep == null) break;
                    bool result = EvalSimpleExpr(cond, context);
                    if (result)
                    {
                      if (gotoStep.Value < 0 || gotoStep.Value >= steps.Count)
                        throw new Exception("步骤 " + i + " goto=" + gotoStep.Value + " 超出范围(0-" + (steps.Count - 1) + ")");
                      i = gotoStep.Value;
                      continue; // 跳转，不执行 i++
                    }
                    break;
                  }
                case "return":
                  {
                    string data = step["data"]?.ToString();
                    if (!string.IsNullOrEmpty(data) && context.ContainsKey(data))
                    {
                      returnKey = data;
                    }
                    i = steps.Count; // 结束循环
                    continue;
                  }
                default:
                  throw new Exception("步骤 " + i + " 未知类型: " + type);
              }
              i++;
            }

            trans.Commit();
            // 返回 return 步骤指定数据或最后结果
            if (returnKey != null && context.ContainsKey(returnKey))
              responseModel.SetData(context[returnKey]);
            else
              responseModel.SetData(lastResult ?? new { affected = 0 });
          }
          catch (Exception ex)
          {
            try { trans.Rollback(); } catch { }
            responseModel.SetError("脚本编排执行失败（已回滚）: " + ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// 轻量条件表达式求值器。支持：StepContext 变量引用(result1.affected)、数字比较(&gt;/&lt;/&gt;=/&lt;=)、
    /// 等值比较(==/!=)、逻辑组合(&amp;&amp;/||)、null 检查、取反(!)。
    /// 不引入 Roslyn/NVelocity 等脚本引擎（安全边界：编排接口是声明式的，不应有图灵完备能力）。
    /// </summary>
    private static bool EvalSimpleExpr(string expr, Dictionary<string, object> context)
    {
      if (string.IsNullOrEmpty(expr)) return false;
      expr = expr.Trim();

      // 逻辑 OR（优先级最低）
      int orIdx = FindLogicalOp(expr, "||");
      if (orIdx >= 0)
        return EvalSimpleExpr(expr.Substring(0, orIdx).Trim(), context) || EvalSimpleExpr(expr.Substring(orIdx + 2).Trim(), context);

      // 逻辑 AND
      int andIdx = FindLogicalOp(expr, "&&");
      if (andIdx >= 0)
        return EvalSimpleExpr(expr.Substring(0, andIdx).Trim(), context) && EvalSimpleExpr(expr.Substring(andIdx + 2).Trim(), context);

      // 取反
      if (expr.StartsWith("!"))
        return !EvalSimpleExpr(expr.Substring(1).Trim(), context);

      // 比较运算符
      string[] cmpOps = { ">=", "<=", "!=", "==", ">", "<" };
      foreach (string op in cmpOps)
      {
        int idx = expr.IndexOf(op);
        if (idx > 0)
        {
          string left = expr.Substring(0, idx).Trim();
          string right = expr.Substring(idx + op.Length).Trim();
          object lv = ResolveValue(left, context);
          object rv = ResolveValue(right, context);
          return CompareValues(lv, rv, op);
        }
      }

      // 无运算符：当作布尔值（null/0/空字符串/false 为 false）
      object val = ResolveValue(expr, context);
      if (val == null) return false;
      if (val is bool b) return b;
      if (val is int i) return i != 0;
      if (val is long l) return l != 0;
      if (val is string s) return !string.IsNullOrEmpty(s);
      return true;
    }

    private static int FindLogicalOp(string expr, string op)
    {
      // 从右向左找（左结合），跳过括号内
      int depth = 0;
      for (int i = expr.Length - op.Length; i >= 0; i--)
      {
        if (expr[i] == ')') depth++;
        else if (expr[i] == '(') depth--;
        else if (depth == 0 && i + op.Length <= expr.Length && expr.Substring(i, op.Length) == op)
          return i;
      }
      return -1;
    }

    private static object ResolveValue(string token, Dictionary<string, object> context)
    {
      if (string.IsNullOrEmpty(token)) return null;
      token = token.Trim();
      // null 字面量
      if (token == "null") return null;
      // 布尔字面量
      if (token == "true") return true;
      if (token == "false") return false;
      // 数字字面量
      if (int.TryParse(token, out int intVal)) return intVal;
      if (long.TryParse(token, out long longVal)) return longVal;
      // 字符串字面量（去除引号）
      if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
        return token.Substring(1, token.Length - 2);
      // 点号路径取 StepContext 变量（如 result1.affected）
      if (token.Contains("."))
      {
        string[] parts = token.Split('.');
        if (!context.ContainsKey(parts[0])) return null;
        object current = context[parts[0]];
        for (int i = 1; i < parts.Length && current != null; i++)
        {
          var t = current.GetType();
          var prop = t.GetProperty(parts[i]);
          if (prop != null)
            current = prop.GetValue(current, null);
          else if (current is IDictionary dict)
            current = dict.Contains(parts[i]) ? dict[parts[i]] : null;
          else
            return null;
        }
        return current;
      }
      // 简单变量名
      if (context.ContainsKey(token)) return context[token];
      return token; // 兜底：原样返回（可能是字符串值）
    }

    private static bool CompareValues(object left, object right, string op)
    {
      // null 比较
      if (left == null || right == null)
      {
        switch (op)
        {
          case "==": return left == right;
          case "!=": return left != right;
          default: return false;
        }
      }
      // 数值比较
      if (TryDouble(left, out double ld) && TryDouble(right, out double rd))
      {
        switch (op)
        {
          case ">": return ld > rd;
          case "<": return ld < rd;
          case ">=": return ld >= rd;
          case "<=": return ld <= rd;
          case "==": return ld == rd;
          case "!=": return ld != rd;
        }
      }
      // 字符串比较
      string ls = left.ToString(), rs = right.ToString();
      switch (op)
      {
        case "==": return ls == rs;
        case "!=": return ls != rs;
        case ">": return string.Compare(ls, rs) > 0;
        case "<": return string.Compare(ls, rs) < 0;
        default: return false;
      }
    }

    private static bool TryDouble(object val, out double result)
    {
      result = 0;
      if (val is int i) { result = i; return true; }
      if (val is long l) { result = l; return true; }
      if (val is double d) { result = d; return true; }
      if (val is float f) { result = f; return true; }
      if (val is decimal dec) { result = (double)dec; return true; }
      if (val is string s) return double.TryParse(s, out result);
      return false;
    }

    protected virtual void doQuery(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      Params["FILTERCODE"] = row["FILTERCODE"];
      string RESOURCEID = MAINRESOURCEID;
      if (MAINPATH != "")
      {
        ViewRow pathRow = MD.GetPath(MAINPATH);
        RESOURCEID = pathRow.GetString("RESOURCEID");
      }
      BaseModel MAIN = GetModel(MAINPATH, RESOURCEID);

      Hashtable FilterParams = Params["FilterParams"] as Hashtable;
      if (FilterParams != null){
        FilterParams["_USERID_"] = this.userInfo["ID"];
        FilterParams["_EMPID_"] = this.userInfo["EMPID"];
        FilterParams["_DEPTID_"] = this.userInfo["DEPTID"];
      }
      string isExport = Params["isExport"] + "";
      QueryInfo queryInfo = GetQueryInfo(Params);

      if (isExport == "1")
      {
        queryInfo.PageIndex = 1;
        queryInfo.PageSize = 1000;
        QueryResult result = MAIN.Query(queryInfo);
        MAIN.GetView().FillData(result.Items);
        ArrayList columns = Params["columns"] as ArrayList;
        while (MAIN.GetView().Count < int.Parse(result.TotalCount))
        {
          queryInfo.PageIndex++;
          result = MAIN.Query(queryInfo);
          MAIN.GetView().FillData(result.Items, false);
        }
        List<DataView> tables = new List<DataView>();
        List<ArrayList> lcolums = new List<ArrayList>();
        tables.Add(MAIN.GetView());
        lcolums.Add(columns);
        FILE file = new FILE(this.operate01);
        Hashtable saveFile = new Hashtable();
        string expPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string fileName = MD.GetView()[0].GetString("MODULENAME") + "导出" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
        saveFile["UPLOADFILEPATH"] = expPath + fileName;
        saveFile["UPLOADTYPE"] = "导出";
        saveFile["FILENAME"] = fileName;
        ExcelHelper.CreateExcel(expPath + fileName, tables.ToArray(), null, lcolums.ToArray());
        file.SaveFile(saveFile);
        ArrayList saveList = new ArrayList();
        saveList.Add(file.GetView());
        this.operate01.Save(saveList);
        responseModel.SetData(file.GetValue("ID"));
      }
      else
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", new ArrayList(), "");
        // 委托 DataCallService.QueryCore（与助理 query_data 共用核心查询逻辑，权限/规则一致）
        responseModel.SetData(new DataCallService(this.operate01).QueryCore(
          MAIN, row.GetString("FILTERCODE"), Params["FilterParams"] as Hashtable, this.userInfo,
          queryInfo.OrderBy, queryInfo.SumFields, queryInfo.PageIndex, queryInfo.PageSize));
      }

    }

    protected virtual void doOpen(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      Params["FILTERCODE"] = row["FILTERCODE"];
      Hashtable ht = new Hashtable();
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      MAIN.Open(GetQueryInfo(Params));
      ht[MAINPATH] = MAIN.GetView();
      IList<ViewRow> list = MD.GetPathRel(MAINPATH);
      foreach (var trow in list)
      {
        String tpath = trow.GetString("PATHNAMEB");
        BaseModel DTS = GetModel(tpath, MD.GetPath(tpath).GetString("RESOURCEID"));
        QueryInfo tqueryInfo = new QueryInfo();
        tqueryInfo.OtherWhere = $"{ MAIN.GetView().Resource.RESOURCEANAME}.{trow.GetString("RFIELDSB")} IN @{trow.GetString("RFIELDSA")}";
        tqueryInfo.FilterParams[$"{trow.GetString("RFIELDSA")}"] = MAIN.GetValues(trow.GetString("RFIELDSA")).Split(',');
        if (DTS.HasColumn("ENTRYNUM"))
        {
          tqueryInfo.OrderBy = "ENTRYNUM";
        }
        DTS.Open(tqueryInfo);
        ht[tpath] = DTS.GetView();
      }
      responseModel.SetData(ht);
    }

    protected virtual void doGetId(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      int CNT = int.Parse(Params["CNT"] + "");
      string RESOURCEID = Params["RESOURCEID"] + "";
      Resource tresource = null;
      if (RESOURCEID != "")
      {
        operate01.GetResource(RESOURCEID);
      }
      responseModel.SetData(operate01.GetNewID(tresource, CNT));
    }

    protected virtual void doGetBillCode(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string TCODE =  Params["TCODE"] + "";
       Dictionary<string, object> oParams = new Dictionary<string, object>();
      oParams["TCODE"] = TCODE;
      oParams["OCODE"] = new ParamInfo("", System.Data.DbType.String, System.Data.ParameterDirection.Output);
      ArrayList list = new ArrayList();
      list.Add(new ExecInfo("PSS_GENCODE", oParams));
      operate01.Save(list);
      responseModel.SetData((oParams["OCODE"] as ParamInfo).Value + "");
    }




    protected IDictionary<string, DataView> _doSave(MOUDLE MD, ViewRow row, Hashtable Params, ArrayList saveList)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string BEFOREAPI = row.GetString("BEFOREAPICODE");
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (BEFOREAPI != "")
      {
        ViewRow beforeAPI = MD.GetAPI(BEFOREAPI);
        if ("exec" == beforeAPI.GetString("APITYPE"))
        {
          Dictionary<string, object> dicPrams = new Dictionary<string, object>();
          foreach (string key in Params.Keys)
          {
            dicPrams[key] = Params[key];
          }
          saveList.Add(new ExecInfo(SQLManage.GetSQL(beforeAPI.GetString("SQLID")), dicPrams));
        }
      }
      IDictionary<string, DataView> viewList = new Dictionary<string, DataView>();
      foreach (DictionaryEntry d in Params)
      {
        if (IsReservedParamKey(d.Key + "")) continue;
        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        view.InitData(d.Value + "");
        view.FillKey();
        //view.FillEntryNum();
        if ((d.Key + "") == MAINPATH && MAINPATH != "")
        {
          this.setSaveInfo(view.GetView());
        }
        saveList.Add(view.GetView());
        viewList.Add(d.Key + "", view.GetView());
      }
      //单据号处理
      if (MAINPATH != "")
      {
        DataView MAIN = viewList[MAINPATH];
        M01 MMAIN = new M01(this.operate01, MAIN);
        ResourceField field = MAIN.Resource.Fields.Find((ResourceField f) =>
        {
          return f.FIELDNAME == "BILLCODE";
        });
        if (field != null && (MMAIN.GetValue("BILLCODE") + "" == ""))
        {
          MMAIN.setBillCode(field.VFORMAT);
        }
      }
      //主外键处理
      foreach (var item in MD.PATHREL.GetView())
      {
        var PATHA = item["PATHNAMEA"] + "";
        var PATHB = item["PATHNAMEB"] + "";
        var FIELDA = item["RFIELDSA"] + "";
        var FIELDB = item["RFIELDSB"] + "";
        if (viewList.ContainsKey(PATHB))
        {
          if (!viewList.ContainsKey(PATHA))
          {
            continue;
          }
          var v = viewList[PATHA][0][FIELDA];
          viewList[PATHB].ForEach((ViewRow tr) =>
          {
            tr[FIELDB] = v;
          });
        }
      }
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = null;
          if (ePath != "")
          {
            eView = viewList[ePath];
          }
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }
      return viewList;
    }

    protected ExecInfo GetExecInfo(ViewRow apiRow, DataView eView)
    {
      Dictionary<string, object> dicPrams = new Dictionary<string, object>();
      if (eView != null)
      {
        string eApiParam = apiRow.GetString("APIPARAM");
        string[] aa = eApiParam.Split(',');
        foreach (var a in aa)
        {
          if (eView.Count == 1)
          {
            dicPrams[a] = eView[0][a];
          }
          else if (eView.Deleted.Count == 1)
          {
            dicPrams[a] = eView.Deleted[0][a];
          }
        }
      }
      return new ExecInfo(SQLManage.GetSQL(apiRow.GetString("SQLID")), dicPrams);
    }

    /// <summary>
    /// 请求级保留参数键（不作为数据路径处理）：CHANGENOTE=版本变更说明, SKIPVERSION=1 时跳过版本捕获。
    /// _doSave/doDelete/doUpdate 遍历 Params 时必须跳过，否则 MD.GetPath 空引用。
    /// </summary>
    protected static bool IsReservedParamKey(string key)
    {
      return key == "CHANGENOTE" || key == "SKIPVERSION";
    }

    protected virtual void doSave(MOUDLE MD, ViewRow row, Hashtable Params)
    {      ArrayList saveList = new ArrayList();
      try
      {
        string MAINPATH = row.GetString("PATHNAME");
        IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
        if (viewList.ContainsKey(MAINPATH))
        {
          M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
          MAIN.setReSubmitInfo(this.userInfo);
          MAIN.setReCheckInfo(this.userInfo);
          MAIN.setReVerifyInfo(this.userInfo);
          string flowCode = MD.GetValue("FLOWCODE");
          MAIN.setState(flowCode);
        }
        // 在线开发版本捕获：纳管资源的 DataView 前后镜像生成版本行，与业务保存同事务
        // CHANGENOTE: 前端提交时填写的变更说明；SKIPVERSION=1: 快速保存不留版本
        var skipVer = (Params["SKIPVERSION"] + "") == "1";
        var verTouched = skipVer
          ? new System.Collections.Generic.List<Realso.WebAPI.Services.DevVersionService.TouchedObj>()
          : Realso.WebAPI.Services.DevVersionService.Capture(this.operate01, saveList, this.userInfo, Params["CHANGENOTE"] + "");
        operate01.Save(saveList);
        Realso.WebAPI.Services.DevVersionService.CleanupExpired(verTouched);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
        responseModel.SetData(viewList);
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
    }

    protected virtual void doAfterSave(MOUDLE MD, ViewRow row, Hashtable Params,ArrayList saveList){

    }

    protected virtual void doDelete(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      ArrayList saveList = new ArrayList();
      Dictionary<string, DataView> viewList = new Dictionary<string, DataView>();
      foreach (DictionaryEntry d in Params)
      {
        if (IsReservedParamKey(d.Key + "")) continue;
        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        view.InitData(d.Value + "");
        saveList.Add(view.GetView());
        viewList[d.Key + ""] = view.GetView();
      }
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = null;
          if (ePath != "")
          {
            eView = viewList[ePath];
          }
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }
      try
      {
        // 在线开发版本捕获（doDelete 通道；SKIPVERSION=1 时跳过）
        var skipVerDel = (Params["SKIPVERSION"] + "") == "1";
        var verTouched = skipVerDel
          ? new System.Collections.Generic.List<Realso.WebAPI.Services.DevVersionService.TouchedObj>()
          : Realso.WebAPI.Services.DevVersionService.Capture(this.operate01, saveList, this.userInfo, Params["CHANGENOTE"] + "");
        operate01.Save(saveList);
        Realso.WebAPI.Services.DevVersionService.CleanupExpired(verTouched);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
      responseModel.SetData(saveList);
    }
    protected virtual void doDelete2(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      responseModel.SetData(true);
    }

    protected virtual void doUpdate(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string APIPARAM = row.GetString("APIPARAM");
      string FILTERCODE = row.GetString("FILTERCODE");
      ArrayList saveList = new ArrayList();
      foreach (DictionaryEntry d in Params)
      {
        if (IsReservedParamKey(d.Key + "")) continue;
        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        BaseModel qview = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        QueryInfo queryInfo = new QueryInfo();
        view.InitData(d.Value + "");
        queryInfo.FilterCode = FILTERCODE;
        string[] a = APIPARAM.Split(',');
        for (int i = 0; i < a.Length; i++)
        {
          queryInfo.FilterParams[a[i]] = view.GetValue(a[i]);
        }
        qview.Open(queryInfo);
        for (int i = 0; i < a.Length; i++)
        {
          qview.SetValue(a[i], view.GetValue(a[i]));
        }
        try
        {
          saveList.Add(qview.GetView());
          // 在线开发版本捕获（doUpdate 通道；SKIPVERSION=1 时跳过）
          var skipVerUpd = (Params["SKIPVERSION"] + "") == "1";
          var verTouched = skipVerUpd
            ? new System.Collections.Generic.List<Realso.WebAPI.Services.DevVersionService.TouchedObj>()
            : Realso.WebAPI.Services.DevVersionService.Capture(this.operate01, saveList, this.userInfo, Params["CHANGENOTE"] + "");
          operate01.Save(saveList);
          Realso.WebAPI.Services.DevVersionService.CleanupExpired(verTouched);
          this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
          qview.Open(queryInfo);
        }
        catch (Exception ex)
        {
          this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
          throw ex;
        }
        responseModel.SetData(qview.GetView());
      }
    }
    public override BaseModel GetModel(string Path, string RESOURCEID)
    {
      return new M01(this.operate01, RESOURCEID);
    }

    protected M01 getMainPath(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string flowCode = MD.GetValue("FLOWCODE");
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      M01 MAIN = (M01)GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      MAIN.OpenByID(Params["ID"] + "");
      return MAIN;
    }

    protected void doFlowCodeSave(M01 MAIN, MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      MAIN.setState(flowCode);
      try
      {
        saveList.Add(MAIN.GetView());
        operate01.Save(saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
    }

    protected virtual void doSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string MAINPATH = row.GetString("PATHNAME");
      IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
      M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
      MAIN.setSubmitInfo(this.userInfo);
      string flowCode = MD.GetValue("FLOWCODE");
      MAIN.setState(flowCode);
      try
      {
        operate01.Save(saveList);
        this.doAfterSubmit(MD, row, Params, saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
      responseModel.SetData(viewList);
    }

    protected virtual void doAfterSubmit(MOUDLE MD, ViewRow row, Hashtable Params,ArrayList saveList){

    }

    protected virtual void doReSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);

      MAIN.setReSubmitInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }

    protected virtual void doBatchSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setSubmitInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }



    protected virtual void doBatchReSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
       M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReSubmitInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      for (int i = 0; i < MAIN.GetView().Count; i++)
      {
        MAIN.SetValue("VERIFYID", Params["NEXTAPRID"], i);
        MAIN.SetValue("VERIFIER", Params["NEXTAPRER"], i);
      }
      MAIN.setCheckInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doReCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReCheckInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setVerifyInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doReVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReVerifyInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }

    protected virtual void doBatchCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "已审核"));
           row1["STATE"] = BillState.待审批;
           row1["CHECKREMARK"] = Params["REMARK"];
           row1["VERIFYID"] = Params["NEXTAPRID"];
           row1["VERIFIER"] = Params["NEXTAPRER"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);

        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchCheckReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "审核驳回"));
           row1["STATE"] = BillState.已驳回;
           row1["CHECKREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", "", "撤销审核"));
           row1["STATE"] = BillState.待审核;
           row1["CHECKREMARK"] = "";
           row1["VERIFYID"] = "";
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReCheckInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "已审批"));
           row1["STATE"] = BillState.已审批;
           row1["VERIFYREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setVerifyInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchVerifyReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "审批驳回"));
           row1["STATE"] = BillState.已驳回;
           row1["VERIFYREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);

        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
          {
            saveList.Add(this.addOperateLogs(row1["ID"] + "", "", "撤销审批"));
            row1["STATE"] = BillState.待审批;
            row1["VERIFYREMARK"] = "";
          });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReVerifyInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual BaseModel doGetBatchMain(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string APIPARAM = row.GetString("APIPARAM");
      string FILTERCODE = row.GetString("FILTERCODE");
      Params["FILTERCODE"] = row["FILTERCODE"];
      ArrayList saveList = new ArrayList();
      Hashtable ht = new Hashtable();
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      QueryInfo queryInfo = GetQueryInfo(Params);
      queryInfo.FilterParams["AEMPID"] = this.userInfo["EMPID"];
      MAIN.Open(GetQueryInfo(Params));
      if (MAIN.GetView().Count != (Params["ID"] + "").Split(',').Length)
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return null;
      }
      return MAIN;
    }

    protected virtual DataView addOperateLogs(string ID, string REMARK, string STATE)
    {
      DataView logView = new DataView(this.operate01.GetResource("VSS_OPLOGS"));
      ViewRow row = new ViewRow(logView);
      row["REFID"] = ID;
      row["STATE"] = STATE;
      row["REMARK"] = REMARK;
      row["OPLOGER"] = this.userInfo["NICKNAME"] + "";
      row["OPLOGDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      logView.AddRow(row);
      this.operate01.FillKey(logView);
      string rPathw = Realso.Utils.ConfigHelper.GetConfig($"Url:验证二维码");
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Url:公众号接口");
      try{
        Task.Run(()=>{
             HttpClientHepler.PostResponse(rPath+"wxmp/delegate/updateState",JsonConvert.SerializeObject(row));
        });
      }catch(Exception e){
      }
      return logView;
    }
  }
}
