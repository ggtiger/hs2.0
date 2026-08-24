using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Realso.WebAPI.Services.Agent;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// SFC AI 代码助手工具执行器：提供给 DeepSeek 的 tools 定义 + Execute 分发。
  /// 工具实现复用 SfcModuleSchemaService 查询元数据表。
  /// 实现 IToolExecutor 接口（4 个只读工具），供 AgentEngine 统一调度。
  /// </summary>
  public class SfcAiToolExecutor : IToolExecutor
  {
    /// <summary>发给 DeepSeek 的 tools 定义（function calling）</summary>
    public static List<object> GetToolDefinitions()
    {
      var defs = new List<object>
      {
        Tool("get_module_schema",
          "获取模块的字段/API/子表关系/过滤器参数。修改代码前必须先调此工具了解模块结构。返回 moduleCode/moduleName/tableName/apis/fields/refFields/subTables/queryFilterParams。",
          P("moduleCode", "string", "模块编码(如 LIB_M07)", true)),
        Tool("get_module_pages",
          "获取模块的页面配置(tss_module_page)和按钮配置(tss_module_button)。返回 pages[]，每页含 pageCode/pageType/routePath/componentType/queryApiCode/openApiCode/saveApiCode 和 buttons[]。用于了解页面类型和按钮交互模式。",
          P("moduleCode", "string", "模块编码(如 LIB_M07)", true)),
        Tool("get_uiset",
          "获取模块字段完整 UI 配置(tss_resuipc)，含 EDITTYPE/SELECTDATA/LISTSORT/QUERYSORT/EDITSORT/REQUIRED/READONLY/DEFAULTVALUE。用于了解字段控件类型、必填、默认值等细节。",
          P("moduleCode", "string", "模块编码(如 LIB_M07)", true)),
        Tool("get_sql_list",
          "搜索 tss_sql 表中已有的 SQL 模板(SQLCODE/SQLTXT/REMARK)。当需要查找校验逻辑、数据查询SQL、exec接口对应的SQL时先调此工具。传 keyword 按编码或备注模糊搜索，不传则返回全部(最多30条)。",
          P("keyword", "string", "搜索关键字(如部门名DEPT、人员EMP、校验CHECK等)", false)),
        Tool("get_module_files",
          "列出模块全部代码文件(C#脚本/SQL模板/JS模块/VUE组件)：kind/code/name/path/已分配接口码apiCodes。多文件联动开发(接口→store→页面)前必须先调，看清已有文件和接口码分配。",
          P("moduleCode", "string", "模块编码(如 LIB_M07)", true)),
        Tool("read_code_asset",
          "读取单个代码资产完整源码。csharp/sql 传 code(如 SC_R02_M07_BACK)，js/vue 传 path(如 @/modules/R02_M07/store.js)。改动任何已存在文件前必须先读。",
          P("code", "string", "资产编码(csharp/sql)", false),
          P("path", "string", "资产路径(js/vue)", false)),
      };
      // 合并声明式工具（tss_ai_tool，同名内置优先）
      var names = new HashSet<string>();
      foreach (var d in defs)
      {
        try
        {
          var jo = d is JObject j ? j : JObject.FromObject(d);
          var n = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(n)) names.Add(n);
        }
        catch { }
      }
      foreach (var d in DeclarativeToolProvider.GetDefinitions("sfc"))
      {
        try
        {
          var jo = d is JObject j2 ? j2 : JObject.FromObject(d);
          var n = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(n) && names.Add(n)) defs.Add(d);
        }
        catch { }
      }
      return defs;
    }

    /// <summary>执行工具，返回结果对象(序列化后喂回 LLM)</summary>
    public object Execute(string toolName, JObject args)
    {
      try
      {
        // 声明式工具（tss_ai_tool）分发
        if (DeclarativeToolProvider.HasTool(toolName))
        {
          var dtr = DeclarativeToolProvider.Execute(toolName, args, new ToolContext()).GetAwaiter().GetResult();
          return dtr.Data;
        }
        switch (toolName)
        {
          case "get_module_schema":
            return SfcModuleSchemaService.GetModuleSchema(args["moduleCode"]?.ToString());
          case "get_module_pages":
            return SfcModuleSchemaService.GetModulePages(args["moduleCode"]?.ToString());
          case "get_uiset":
            return SfcModuleSchemaService.GetModuleUiset(args["moduleCode"]?.ToString());
          case "get_sql_list":
            return SfcModuleSchemaService.GetSqlList(args["keyword"]?.ToString());
          case "get_module_files":
            return SfcModuleSchemaService.GetModuleFiles(args["moduleCode"]?.ToString());
          case "read_code_asset":
            return SfcModuleSchemaService.ReadCodeAsset(args["code"]?.ToString(), args["path"]?.ToString());
          default:
            return new { error = "未知工具: " + toolName };
        }
      }
      catch (System.Exception ex)
      {
        return new { error = ex.Message };
      }
    }

    // ============ IToolExecutor 实现 ============

    /// <summary>该执行器负责的工具名集合（4 个只读工具）</summary>
    public IEnumerable<string> GetToolNames()
    {
      var names = new HashSet<string>();
      foreach (var def in GetToolDefinitions())
      {
        try
        {
          var jo = def is JObject j ? j : JObject.FromObject(def);
          var name = jo["function"]?["name"]?.ToString();
          if (!string.IsNullOrEmpty(name)) names.Add(name);
        }
        catch { }
      }
      return names;
    }

    /// <summary>按 set 取定义。SFC 只有一个工具集 "sfc"，setName 忽略，返回全部。</summary>
    public List<object> GetDefinitionsBySet(string setName, ToolKind? filter = null)
    {
      return GetToolDefinitions();
    }

    /// <summary>返回全部工具定义（filter 现阶段不细分）</summary>
    public List<object> GetDefinitions(ToolKind? filter = null)
    {
      return GetToolDefinitions();
    }

    /// <summary>SFC 工具全后端只读，无前端工具</summary>
    public bool IsFrontendTool(string toolName)
    {
      return false;
    }

    /// <summary>
    /// IToolExecutor.Execute：包装现有 Execute(toolName, args)，
    /// 把 object 结果包装成 ToolResult.Ok。
    /// </summary>
    public Task<ToolResult> Execute(string toolName, JObject args, ToolContext ctx)
    {
      object result = Execute(toolName, args);
      return Task.FromResult(ToolResult.Ok(result));
    }

    // =================== 辅助方法 ===================

    private static object Tool(string name, string desc, params object[] props)
    {
      var properties = new Dictionary<string, object>();
      var required = new List<string>();
      foreach (Dictionary<string, object> p in props)
      {
        properties[(string)p["name"]] = new { type = p["type"], description = p["desc"] };
        if (p.ContainsKey("required") && (bool)p["required"]) required.Add((string)p["name"]);
      }
      return new
      {
        type = "function",
        function = new
        {
          name,
          description = desc,
          parameters = new { type = "object", properties, required }
        }
      };
    }

    private static Dictionary<string, object> P(string name, string type, string desc, bool required)
    {
      return new Dictionary<string, object> { { "name", name }, { "type", type }, { "desc", desc }, { "required", required } };
    }
  }
}
