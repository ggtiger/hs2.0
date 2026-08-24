using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 业务模板导出器：把一个已存在模块的全部关联元数据遍历出来，
  /// 生成与 .aidev.sql 同构的幂等 INSERT 脚本（含 ${MODULECODE}/${MODULENAME}/${PARENTFUNCID} 变量占位）。
  /// 安装时变量替换后走 UpgradeExecutor.Import/Execute（天然获得幂等/单事务/快照回滚）。
  /// 遍历范围（依赖序）：
  ///   tss_code_asset(SQL 模板 + API 脚本C#) → tss_resource(TBS→DATAVIEW) → tss_resfield → tss_resfilter → tss_resuipc
  ///   → tss_dict/tss_dictitem → tss_moudle → moudlepath/pathrel → moudleapi
  ///   → tss_code_asset(页面引用的 JS/VUE) → tss_module_page/button → tss_func/funcpoint
  /// 值编码规则：含单引号或分号或长度>500 的文本一律 0x HEX 字面量
  /// （同时避开字符串转义与 UpgradeExecutor 的分号拆分误判）。
  /// </summary>
  public static class TemplateExporter
  {
    /// <summary>导出结果</summary>
    public class ExportResult
    {
      public string Script;        // 含变量占位的脚本
      public string Variables;     // VARIABLES JSON [{name,label,default,required}]
      public int ItemCount;        // 生成的 INSERT 条数
    }

    /// <summary>
    /// 导出模块为模板脚本。
    /// </summary>
    public static ExportResult Export(DBHelper helper, string moduleCode, string templateCode, string templateName, string exportedBy)
    {
      var result = new ExportResult();
      // 1. 模块本体
      var module = helper.QueryFirstOrDefault(
        "SELECT * FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
      if (module == null) throw new Exception("模块 " + moduleCode + " 不存在");
      string moduleId = (string)module.ID;
      string moduleName = (string)module.MODULENAME;

      var sections = new List<KeyValuePair<string, List<string>>>(); // 分节: 标题→INSERT 列表
      int itemCount = 0;

      // 2. 模块数据源/关系/接口
      var paths = helper.Query("SELECT * FROM tss_moudlepath WHERE MODULEID=@mid", new { mid = moduleId }).ToList();
      var pathrels = helper.Query("SELECT * FROM tss_moudlepathrel WHERE MODULEID=@mid", new { mid = moduleId }).ToList();
      var apis = helper.Query("SELECT * FROM tss_moudleapi WHERE MODULEID=@mid", new { mid = moduleId }).ToList();

      // 3. 资源（paths 的 RESOURCEID + DATAVIEW 的 TABLERESOURCEID 链）
      var resourceIds = new HashSet<string>();
      foreach (var p in paths)
      {
        string rid = ((IDictionary<string, object>)p)["RESOURCEID"] + "";
        if (!string.IsNullOrEmpty(rid)) resourceIds.Add(rid);
      }
      if (resourceIds.Count > 0)
      {
        var views = helper.Query("SELECT * FROM tss_resource WHERE ID IN @ids", new { ids = resourceIds.ToArray() }).ToList();
        foreach (var v in views)
        {
          string trid = ((IDictionary<string, object>)v)["TABLERESOURCEID"] + "";
          if (!string.IsNullOrEmpty(trid)) resourceIds.Add(trid);
        }
      }
      var resources = resourceIds.Count > 0
        ? helper.Query("SELECT * FROM tss_resource WHERE ID IN @ids ORDER BY CASE RESOURCETYPE WHEN 'TABLE' THEN 0 WHEN 'SQL' THEN 1 ELSE 2 END, RESOURCENAME", new { ids = resourceIds.ToArray() }).ToList()
        : new List<dynamic>();

      // 4. 字段/过滤器/UI
      var resfields = resourceIds.Count > 0
        ? helper.Query("SELECT * FROM tss_resfield WHERE RESOURCEID IN @ids ORDER BY RESOURCEID, ID", new { ids = resourceIds.ToArray() }).ToList()
        : new List<dynamic>();
      var resfilters = resourceIds.Count > 0
        ? helper.Query("SELECT * FROM tss_resfilter WHERE RESOURCEID IN @ids ORDER BY RESOURCEID, FILTERCODE", new { ids = resourceIds.ToArray() }).ToList()
        : new List<dynamic>();
      var resuipcs = resourceIds.Count > 0
        ? helper.Query("SELECT * FROM tss_resuipc WHERE RESOURCEID IN @ids ORDER BY RESOURCEID, ID", new { ids = resourceIds.ToArray() }).ToList()
        : new List<dynamic>();

      // 5. tss_sql：接口 SQLID + RESOURCETYPE=SQL 资源的过滤器(FILTERSQL 存 SQLCODE)
      var sqlCodes = new HashSet<string>();
      foreach (var a in apis)
      {
        string sid = ((IDictionary<string, object>)a)["SQLID"] + "";
        if (!string.IsNullOrEmpty(sid)) sqlCodes.Add(sid);
      }
      var sqlResources = resources.Where(r => ((IDictionary<string, object>)r)["RESOURCETYPE"] + "" == "SQL").ToList();
      foreach (var sr in sqlResources)
      {
        string rid = ((IDictionary<string, object>)sr)["ID"] + "";
        foreach (var f in resfilters.Where(f => ((IDictionary<string, object>)f)["RESOURCEID"] + "" == rid))
        {
          string fsql = ((IDictionary<string, object>)f)["FILTERSQL"] + "";
          if (!string.IsNullOrEmpty(fsql) && !fsql.Contains(" ")) sqlCodes.Add(fsql);
        }
      }
      var sqls = sqlCodes.Count > 0
        ? helper.Query("SELECT * FROM tss_code_asset WHERE ASSETTYPE='sql' AND CODE IN @codes AND ISDELETED=0", new { codes = sqlCodes.ToArray() }).ToList()
        : new List<dynamic>();

      // 5b. API 脚本 (C#)：接口 SCRIPTCODE 引用的 csharp 资产
      var scriptCodes = new HashSet<string>();
      foreach (var a in apis)
      {
        string sc = ((IDictionary<string, object>)a)["SCRIPTCODE"] + "";
        if (!string.IsNullOrEmpty(sc)) scriptCodes.Add(sc);
      }
      var scripts = scriptCodes.Count > 0
        ? helper.Query("SELECT * FROM tss_code_asset WHERE ASSETTYPE='csharp' AND CODE IN @codes AND ISDELETED=0", new { codes = scriptCodes.ToArray() }).ToList()
        : new List<dynamic>();

      // 6. 字典（resuipc.SELECTDATA 命中的字典名/编码）
      var dictNames = new HashSet<string>();
      foreach (var u in resuipcs)
      {
        string sd = ((IDictionary<string, object>)u)["SELECTDATA"] + "";
        if (!string.IsNullOrEmpty(sd) && !sd.StartsWith("{") && !sd.StartsWith("[") && !sd.Contains(":")) dictNames.Add(sd);
      }
      var dicts = dictNames.Count > 0
        ? helper.Query("SELECT * FROM tss_dict WHERE DICTNAME IN @ns OR DICTCODE IN @ns", new { ns = dictNames.ToArray() }).ToList()
        : new List<dynamic>();
      var dictIds = dicts.Select(d => ((IDictionary<string, object>)d)["ID"] + "").ToArray();
      var dictItems = dictIds.Length > 0
        ? helper.Query("SELECT * FROM tss_dictitem WHERE DICTID IN @ids ORDER BY DICTID, ENTRYNUM", new { ids = dictIds }).ToList()
        : new List<dynamic>();

      // 7. 页面/按钮
      var pages = helper.Query("SELECT * FROM tss_module_page WHERE MODULECODE=@mc ORDER BY SORTNO, PAGECODE", new { mc = moduleCode }).ToList();
      var buttons = helper.Query("SELECT * FROM tss_module_button WHERE MODULECODE=@mc ORDER BY PAGEID, SORTNO", new { mc = moduleCode }).ToList();

      // 8. 页面引用的 SFC 文件（SFCMODULEPATH + PAGECONFIG 里的 EXTENDJS/SLOTS 路径）
      var sfcPaths = new HashSet<string>();
      foreach (var p in pages)
      {
        var pd = (IDictionary<string, object>)p;
        string sfcPath = pd["SFCMODULEPATH"] + "";
        if (!string.IsNullOrEmpty(sfcPath)) sfcPaths.Add(sfcPath);
        string pc = pd["PAGECONFIG"] + "";
        if (!string.IsNullOrEmpty(pc))
        {
          try
          {
            var jo = JObject.Parse(pc);
            string extendJs = jo["EXTENDJS"]?.ToString();
            if (!string.IsNullOrEmpty(extendJs)) sfcPaths.Add(extendJs);
            var slots = jo["SLOTS"] as JObject;
            if (slots != null)
            {
              foreach (var kv in slots)
              {
                string sp = kv.Value?.ToString();
                if (!string.IsNullOrEmpty(sp)) sfcPaths.Add(sp);
              }
            }
          }
          catch { }
        }
      }
      var sfcTemplates = sfcPaths.Count > 0
        ? helper.Query("SELECT * FROM tss_code_asset WHERE ASSETTYPE IN ('js','vue') AND MODULEPATH IN @ps AND ISDELETED=0", new { ps = sfcPaths.ToArray() }).ToList()
        : new List<dynamic>();

      // 9. 菜单/功能点
      var func = helper.QueryFirstOrDefault("SELECT * FROM tss_func WHERE FUNCCODE=@mc LIMIT 1", new { mc = moduleCode });
      var funcpoints = func != null
        ? helper.Query("SELECT * FROM tss_funcpoint WHERE FUNCID=@fid", new { fid = (string)func.ID }).ToList()
        : new List<dynamic>();
      string parentFuncId = func != null ? (string)func.UPFUNCID : "";

      // ===== 生成脚本（依赖序分节）=====
      sections.Add(new KeyValuePair<string, List<string>>("SQL 模板 (tss_code_asset)", sqls.Select(r => BuildInsert("tss_code_asset", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("API 脚本 (tss_code_asset)", scripts.Select(r => BuildInsert("tss_code_asset", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("资源 (tss_resource)", resources.Select(r => BuildInsert("tss_resource", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("字段 (tss_resfield)", resfields.Select(r => BuildInsert("tss_resfield", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("过滤器 (tss_resfilter)", resfilters.Select(r => BuildInsert("tss_resfilter", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("UI 配置 (tss_resuipc)", resuipcs.Select(r => BuildInsert("tss_resuipc", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("字典 (tss_dict + tss_dictitem)",
        dicts.Select(r => BuildInsert("tss_dict", (object)r)).Concat(dictItems.Select(r => BuildInsert("tss_dictitem", (object)r))).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("模块 (tss_moudle)", new List<string> { BuildInsert("tss_moudle", module) }));
      sections.Add(new KeyValuePair<string, List<string>>("数据源 (tss_moudlepath)", paths.Select(r => BuildInsert("tss_moudlepath", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("主子表关系 (tss_moudlepathrel)", pathrels.Select(r => BuildInsert("tss_moudlepathrel", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("接口 (tss_moudleapi)", apis.Select(r => BuildInsert("tss_moudleapi", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("SFC 文件 (tss_code_asset)", sfcTemplates.Select(r => BuildInsert("tss_code_asset", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("页面 (tss_module_page)", pages.Select(r => BuildInsert("tss_module_page", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("按钮 (tss_module_button)", buttons.Select(r => BuildInsert("tss_module_button", (object)r)).ToList()));
      sections.Add(new KeyValuePair<string, List<string>>("菜单 (tss_func)", func != null ? new List<string> { BuildInsert("tss_func", func) } : new List<string>()));
      sections.Add(new KeyValuePair<string, List<string>>("功能点 (tss_funcpoint)", funcpoints.Select(r => BuildInsert("tss_funcpoint", (object)r)).ToList()));

      var sb = new StringBuilder();
      sb.AppendLine("-- ============================================================");
      sb.AppendLine("-- 业务模板脚本 (tss_module_template SCRIPT, 与 .aidev.sql 同构)");
      sb.AppendLine("-- ============================================================");
      sb.AppendLine("-- @META SessionCode=TPL_" + templateCode);
      sb.AppendLine("-- @META SessionName=" + templateName);
      sb.AppendLine("-- @META SessionType=template");
      sb.AppendLine("-- @META TargetModule=" + moduleCode);
      sb.AppendLine("-- @META Intent=业务模板安装(" + templateCode + ")");
      sb.AppendLine("-- @META TemplateCode=" + templateCode);
      sb.AppendLine("-- @META TemplateName=" + templateName);
      sb.AppendLine("-- @META SourceModule=" + moduleCode);
      sb.AppendLine("-- @META ExportedAt=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
      sb.AppendLine("-- @META ExportedBy=" + exportedBy);
      sb.AppendLine("-- @META Variables=${MODULECODE},${MODULENAME},${PARENTFUNCID}");
      sb.AppendLine();
      foreach (var sec in sections)
      {
        if (sec.Value.Count == 0) continue;
        sb.AppendLine("-- ------------------------------------------------------------");
        sb.AppendLine("-- " + sec.Key + " (" + sec.Value.Count + " 条)");
        sb.AppendLine("-- ------------------------------------------------------------");
        foreach (var ins in sec.Value)
        {
          sb.AppendLine(ins);
          itemCount++;
        }
        sb.AppendLine();
      }

      // ===== 变量占位替换（带单引号的精确替换，避免误伤）=====
      string script = sb.ToString();
      script = script.Replace("'" + moduleCode + "'", "'${MODULECODE}'");
      if (!string.IsNullOrEmpty(moduleName))
      {
        script = script.Replace("'" + moduleName + "'", "'${MODULENAME}'");
      }
      if (!string.IsNullOrEmpty(parentFuncId))
      {
        script = script.Replace("'" + parentFuncId + "'", "'${PARENTFUNCID}'");
      }

      result.Script = script;
      result.ItemCount = itemCount;
      result.Variables = Newtonsoft.Json.JsonConvert.SerializeObject(new object[]
      {
        new { name = "MODULECODE", label = "模块编码", @default = moduleCode, required = true },
        new { name = "MODULENAME", label = "模块名称", @default = moduleName, required = true },
        new { name = "PARENTFUNCID", label = "父菜单ID", @default = parentFuncId, required = true }
      });
      return result;
    }

    /// <summary>
    /// 安装替换：校验变量 + 占位符替换。
    /// 返回错误消息（null=成功，script 已替换好）。
    /// </summary>
    public static string Substitute(string script, string variablesJson, out string finalScript)
    {
      finalScript = null;
      JObject vars;
      try
      {
        vars = string.IsNullOrEmpty(variablesJson) ? new JObject() : JObject.Parse(variablesJson);
      }
      catch (Exception ex)
      {
        return "变量 JSON 解析失败: " + ex.Message;
      }
      // MODULECODE 必填 + 格式
      string moduleCode = vars["MODULECODE"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return "变量 MODULECODE（模块编码）必填";
      if (!System.Text.RegularExpressions.Regex.IsMatch(moduleCode, "^[A-Za-z0-9_]+$"))
        return "变量 MODULECODE 只能含字母/数字/下划线";
      finalScript = script;
      foreach (var kv in vars)
      {
        string val = kv.Value?.ToString() ?? "";
        // 防注入：变量值不允许单引号（替换进 SQL 字面量）
        if (val.Contains("'") || val.Contains("\\")) return "变量 " + kv.Key + " 的值不能含单引号或反斜杠";
        finalScript = finalScript.Replace("${" + kv.Key + "}", val);
      }
      // 未替换的占位符检查
      var left = System.Text.RegularExpressions.Regex.Match(finalScript, @"\$\{[A-Z]+\}");
      if (left.Success) return "存在未提供值的变量: " + left.Value;
      return null;
    }

    /// <summary>构造一条 INSERT（全部列；含单引号/分号/超长文本用 0x HEX 字面量）</summary>
    private static string BuildInsert(string table, object rowObj)
    {
      var row = (IDictionary<string, object>)rowObj;
      var cols = new List<string>();
      var vals = new List<string>();
      foreach (var kv in row)
      {
        cols.Add("`" + kv.Key + "`");
        vals.Add(SqlValue(kv.Value));
      }
      return "INSERT INTO `" + table + "` (" + string.Join(", ", cols) + ") VALUES (" + string.Join(", ", vals) + ");";
    }

    /// <summary>SQL 值字面量：null→NULL；数值原样；含单引号/分号/长度>500 的文本→0x HEX；其余单引号字符串</summary>
    private static string SqlValue(object v)
    {
      if (v == null || v == DBNull.Value) return "NULL";
      if (v is bool b) return b ? "1" : "0";
      var t = v.GetType();
      if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
          t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
          t == typeof(float) || t == typeof(double) || t == typeof(decimal))
      {
        return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
      }
      if (v is DateTime dt)
      {
        return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";
      }
      string s = v + "";
      if (s.Length == 0) return "NULL";
      if (s.Contains("'") || s.Contains(";") || s.Length > 500)
      {
        return "0x" + ToHex(s);
      }
      return "'" + s + "'";
    }

    private static string ToHex(string s)
    {
      var bytes = Encoding.UTF8.GetBytes(s ?? "");
      var sb = new StringBuilder(bytes.Length * 2);
      foreach (var b in bytes) sb.Append(b.ToString("x2"));
      return sb.ToString();
    }
  }
}
