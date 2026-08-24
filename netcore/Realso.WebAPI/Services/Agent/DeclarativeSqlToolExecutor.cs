using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.Utils;
using Realso.WebAPI.Services.Scripting;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// 声明式工具定义+执行提供者（tss_ai_tool，EXECUTORTYPE=sql/builtin）。
  /// 只读查询类工具无需写 C#：表里配 工具名/描述/参数Schema/SQLCODE 即可注册给 LLM。
  /// builtin 工具的定义也从 DB 读取（BuiltinToolSync 启动时同步到 DB，配置中心可在线修改描述/参数）。
  /// 安全边界：仅允许 SELECT（模板原文与注参后结果双重校验）；MAXROWS 截断防 token 爆炸；
  /// 同名内置工具优先（ToolRegistry 注册顺序），DB 不可覆盖 builtin。
  /// 60s 缓存；表不存在时降级为空（未迁移环境不影响系统）。
  /// </summary>
  public class DeclarativeSqlToolExecutor : IToolExecutor
  {
    private class ToolRow
    {
      public string TOOLNAME;
      public string TOOLSET;
      public string DESCRIPTION;
      public string PARAMS;
      public string SQLCODE;
      public string EXECUTORTYPE;
      public int MAXROWS;
    }

    private List<ToolRow> _cache;
    private DateTime _loadedAt = DateTime.MinValue;
    private readonly object _lock = new object();

    private List<ToolRow> GetTools()
    {
      if (_cache != null && (DateTime.Now - _loadedAt).TotalSeconds < 60) return _cache;
      lock (_lock)
      {
        if (_cache != null && (DateTime.Now - _loadedAt).TotalSeconds < 60) return _cache;
        List<ToolRow> list = new List<ToolRow>();
        try
        {
          using (var helper = DB.GetDBHelper())
          {
            var rows = helper.Query<dynamic>(
              "SELECT TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, SQLCODE, EXECUTORTYPE, MAXROWS FROM tss_ai_tool WHERE ENABLED=1 AND ISDELETED=0 AND EXECUTORTYPE IN ('sql','builtin','csharp')");
            foreach (var r in rows)
            {
              list.Add(new ToolRow
              {
                TOOLNAME = (string)r.TOOLNAME,
                TOOLSET = (string)r.TOOLSET,
                DESCRIPTION = (string)r.DESCRIPTION,
                PARAMS = (string)r.PARAMS,
                SQLCODE = r.SQLCODE == null ? null : (string)r.SQLCODE,
                EXECUTORTYPE = r.EXECUTORTYPE == null ? "sql" : (string)r.EXECUTORTYPE,
                MAXROWS = r.MAXROWS == null ? 200 : (int)r.MAXROWS
              });
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Warn("DeclarativeSqlToolExecutor 读取 tss_ai_tool 失败（降级为空）: " + ex.Message);
          list = new List<ToolRow>();
        }
        _cache = list;
        _loadedAt = DateTime.Now;
        return _cache;
      }
    }

    /// <summary>手动失效缓存（工具管理页保存后调用）</summary>
    public void Invalidate()
    {
      lock (_lock) { _cache = null; }
    }

    public IEnumerable<string> GetToolNames()
    {
      // 只返回 EXECUTORTYPE=sql 的工具名；builtin 工具由 C# AssistantToolExecutor 执行，不走声明式路径
      return GetTools().Where(t => t.EXECUTORTYPE != "builtin").Select(t => t.TOOLNAME).ToList();
    }

    public List<object> GetDefinitionsBySet(string setName, ToolKind? filter = null)
    {
      var defs = new List<object>();
      foreach (var t in GetTools())
      {
        // builtin 工具没有 set 过滤（TOOLSET='builtin'），不在此处返回，由 MergeDeclarativeOverrides 处理
        if (t.EXECUTORTYPE == "builtin") continue;
        if (!string.IsNullOrEmpty(setName) && t.TOOLSET != setName) continue;
        defs.Add(BuildDef(t));
      }
      return defs;
    }

    /// <summary>
    /// 取所有 builtin 工具的 DB 覆盖定义（描述/参数可由配置中心在线修改）。
    /// 返回 Dictionary&lt;toolName, {description, parameters}&gt; 供 MergeDeclarative 覆盖 C# 代码定义。
    /// </summary>
    public Dictionary<string, BuiltinOverride> GetBuiltinOverrides()
    {
      var dict = new Dictionary<string, BuiltinOverride>();
      foreach (var t in GetTools())
      {
        if (t.EXECUTORTYPE != "builtin") continue;
        JObject parameters;
        try
        {
          parameters = string.IsNullOrEmpty(t.PARAMS)
            ? new JObject { ["type"] = "object", ["properties"] = new JObject() }
            : JObject.Parse(t.PARAMS);
        }
        catch
        {
          parameters = new JObject { ["type"] = "object", ["properties"] = new JObject() };
        }
        dict[t.TOOLNAME] = new BuiltinOverride
        {
          Description = t.DESCRIPTION ?? "",
          Parameters = parameters
        };
      }
      return dict;
    }

    /// <summary>builtin 工具的 DB 覆盖定义（描述+参数）</summary>
    public class BuiltinOverride
    {
      public string Description;
      public JObject Parameters;
    }

    public List<object> GetDefinitions(ToolKind? filter = null)
    {
      return GetDefinitionsBySet(null, filter);
    }

    public bool IsFrontendTool(string toolName)
    {
      return false;
    }

    public Task<ToolResult> Execute(string toolName, JObject args, ToolContext ctx)
    {
      var tool = GetTools().Find(t => t.TOOLNAME == toolName);
      if (tool == null) return Task.FromResult(ToolResult.Fail("声明式工具不存在或未启用: " + toolName));
      // builtin 工具由 C# AssistantToolExecutor 执行，不走声明式 SQL 路径
      if (tool.EXECUTORTYPE == "builtin") return Task.FromResult(ToolResult.Fail("builtin 工具 " + toolName + " 应由 C# 执行器处理"));

      // csharp 脚本执行：SQLCODE 存脚本编码，调 CSharpScriptEngine
      if (tool.EXECUTORTYPE == "csharp")
      {
        return ExecuteCSharp(tool, args, ctx);
      }

      // sql 执行
      if (string.IsNullOrEmpty(tool.SQLCODE)) return Task.FromResult(ToolResult.Fail("工具 " + toolName + " 未配置 SQLCODE"));
      try
      {
        string txt = SQLManage.GetSQL(tool.SQLCODE);
        if (string.IsNullOrEmpty(txt)) return Task.FromResult(ToolResult.Fail("SQL模板 " + tool.SQLCODE + " 不存在（tss_sql 查无 SQLCODE）"));
        // 第一重校验：模板原文必须 SELECT 开头
        if (!IsSelectSql(txt)) return Task.FromResult(ToolResult.Fail("声明式工具仅允许 SELECT 查询（模板原文校验失败）"));

        // 参数组装：args → Hashtable + 系统变量
        var ht = new Hashtable();
        if (args != null)
        {
          foreach (var p in args)
          {
            ht[p.Key] = p.Value == null || p.Value.Type == JTokenType.Null ? "" : p.Value.ToString();
          }
        }
        if (ctx != null && ctx.UserInfo != null)
        {
          ht["_USERID_"] = ctx.UserInfo["ID"];
          ht["_EMPID_"] = ctx.UserInfo["EMPID"];
          ht["_DEPTID_"] = ctx.UserInfo["DEPTID"];
        }
        string sql = SQLManage.ParseSQL(txt, ht);
        // 第二重校验：注参后仍须 SELECT（防 NVelocity 分支注入 DML）
        if (!IsSelectSql(sql)) return Task.FromResult(ToolResult.Fail("声明式工具仅允许 SELECT 查询（注参后校验失败）"));

        using (var helper = DB.GetDBHelper())
        {
          var rows = helper.Query(sql).Take(tool.MAXROWS > 0 ? tool.MAXROWS : 200).ToList();
          return Task.FromResult(ToolResult.Ok(
            new { count = rows.Count, rows },
            toolName + " 返回 " + rows.Count + " 行"));
        }
      }
      catch (Exception ex)
      {
        return Task.FromResult(ToolResult.Fail("工具 " + toolName + " 执行失败: " + ex.Message));
      }
    }

    /// <summary>执行 csharp 类型工具：SQLCODE 存脚本编码，调 CSharpScriptEngine</summary>
    private Task<ToolResult> ExecuteCSharp(ToolRow tool, JObject args, ToolContext ctx)
    {
      if (string.IsNullOrEmpty(tool.SQLCODE)) return Task.FromResult(ToolResult.Fail("工具 " + tool.TOOLNAME + " 未配置脚本编码(SQLCODE)"));
      try
      {
        // 构建脚本全局变量
        var globals = new ScriptGlobals
        {
          Params = new Hashtable(),
          Response = new Realso.Core.Models.ResponseModel()
        };
        if (ctx != null && ctx.UserInfo != null)
        {
          globals.UserInfo = ctx.UserInfo;
        }
        // 把 args 注入为脚本可用的参数
        if (args != null)
        {
          foreach (var p in args)
          {
            globals.Params[p.Key] = p.Value == null || p.Value.Type == JTokenType.Null ? "" : p.Value.ToString();
          }
        }
        // 注入系统变量
        if (ctx != null && ctx.UserInfo != null)
        {
          globals.Params["_USERID_"] = ctx.UserInfo["ID"];
          globals.Params["_EMPID_"] = ctx.UserInfo["EMPID"];
          globals.Params["_DEPTID_"] = ctx.UserInfo["DEPTID"];
        }
        string error;
        bool ok = CSharpScriptEngine.Execute(tool.SQLCODE, globals, out error);
        if (!ok) return Task.FromResult(ToolResult.Fail("工具 " + tool.TOOLNAME + " 执行失败: " + error));
        // 脚本通过 Response.SetData() 返回结果
        var result = globals.Response.Data;
        string summary = tool.TOOLNAME + " 执行成功";
        return Task.FromResult(ToolResult.Ok(result, summary));
      }
      catch (Exception ex)
      {
        return Task.FromResult(ToolResult.Fail("工具 " + tool.TOOLNAME + " 执行异常: " + ex.Message));
      }
    }

    private static bool IsSelectSql(string sql)
    {
      if (string.IsNullOrEmpty(sql)) return false;
      string t = sql.TrimStart();
      return t.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
          || t.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
          || t.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase);
    }

    private static object BuildDef(ToolRow t)
    {
      JObject parameters;
      try
      {
        parameters = string.IsNullOrEmpty(t.PARAMS)
          ? new JObject { ["type"] = "object", ["properties"] = new JObject() }
          : JObject.Parse(t.PARAMS);
      }
      catch
      {
        parameters = new JObject { ["type"] = "object", ["properties"] = new JObject() };
      }
      return new
      {
        type = "function",
        function = new
        {
          name = t.TOOLNAME,
          description = t.DESCRIPTION,
          parameters
        }
      };
    }
  }

  /// <summary>
  /// 声明式工具的共享访问点（进程级单例）。
  /// 供 AssistantToolExecutor / SfcAiToolExecutor 在静态工具定义与同步 Execute 里挂接，
  /// 覆盖所有调用方（Hub/向导/AiDev/SFC 控制器）而无需逐个改线。
  /// </summary>
  public static class DeclarativeToolProvider
  {
    private static readonly DeclarativeSqlToolExecutor _shared = new DeclarativeSqlToolExecutor();

    /// <summary>按 set 取声明式工具定义（setName=null 返回全部）</summary>
    public static List<object> GetDefinitions(string setName)
    {
      return _shared.GetDefinitionsBySet(setName);
    }

    /// <summary>工具名是否为声明式工具</summary>
    public static bool HasTool(string toolName)
    {
      return !string.IsNullOrEmpty(toolName) && _shared.GetToolNames().Contains(toolName);
    }

    /// <summary>执行声明式工具</summary>
    public static Task<ToolResult> Execute(string toolName, JObject args, ToolContext ctx)
    {
      return _shared.Execute(toolName, args, ctx);
    }

    /// <summary>缓存失效（工具管理页保存后调用）</summary>
    public static void Invalidate()
    {
      _shared.Invalidate();
    }

    /// <summary>取 builtin 工具的 DB 覆盖定义（描述/参数，配置中心可在线修改）</summary>
    public static Dictionary<string, DeclarativeSqlToolExecutor.BuiltinOverride> GetBuiltinOverrides()
    {
      return _shared.GetBuiltinOverrides();
    }
  }
}
