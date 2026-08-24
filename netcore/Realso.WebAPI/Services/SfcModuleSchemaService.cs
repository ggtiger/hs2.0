using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// SFC 模块元数据查询服务 — 从 tss_moudle/tss_resuipc/tss_moudlepath 等元数据表
  /// 读取模块的 API/字段/子表关系/过滤器参数，供 SFC AI 助手和助理工具共用。
  /// </summary>
  public static class SfcModuleSchemaService
  {
    /// <summary>
    /// 获取模块完整元数据 schema (API 列表/字段/子表/过滤器参数)
    /// </summary>
    public static object GetModuleSchema(string moduleCode)
    {
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, MODULECODE, MODULENAME, REMARK FROM tss_moudle WHERE MODULECODE=@mc", new { mc = moduleCode });
        if (mod == null) return new { error = "模块不存在: " + moduleCode };
        string moduleId = (string)mod.ID;

        var apis = helper.Query<ApiRow>(
          "SELECT APICODE, APITYPE, APINAME, FILTERCODE, PATHNAME FROM tss_moudleapi WHERE MODULEID=@mid ORDER BY APICODE",
          new { mid = moduleId });

        var apiList = new List<object>();
        string queryFilterCode = null;
        string queryPath = null;
        foreach (var a in apis)
        {
          apiList.Add(new { apiCode = a.APICODE, apiType = a.APITYPE, apiTypeDesc = ApiTypeDesc(a.APITYPE), apiName = a.APINAME });
          if (a.APITYPE == "query" && queryFilterCode == null)
          {
            queryFilterCode = a.FILTERCODE;
            queryPath = a.PATHNAME;
          }
        }

        // 解析查询过滤器的参数名（从 FILTERSQL 提取 @VAR，排除 @_ 系统变量）
        var filterParams = new List<object>();
        if (!string.IsNullOrEmpty(queryFilterCode))
        {
          var f = helper.QueryFirstOrDefault<FilterRow>(
            "SELECT FILTERSQL FROM tss_resfilter WHERE FILTERCODE=@fc LIMIT 1", new { fc = queryFilterCode });
          if (f != null && !string.IsNullOrEmpty(f.FILTERSQL))
          {
            var seen = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(f.FILTERSQL, @"@([A-Za-z][A-Za-z0-9_]*)"))
            {
              string n = mm.Groups[1].Value;
              if (n.StartsWith("_")) continue;   // 排除 _USERID_/_EMPID_/_DEPTID_
              if (seen.Add(n))
              {
                string hint = null;
                if (n.EndsWith("_start")) hint = "日期范围起(YYYY-MM-DD)";
                else if (n.EndsWith("_end")) hint = "日期范围止(YYYY-MM-DD)";
                filterParams.Add(new { name = n, hint });
              }
            }
          }
        }

        // 字段清单 + 物理表名 + 字段引用关系
        var fields = new List<object>();
        string tableName = null;
        var refFields = new List<object>();
        var subTables = new List<object>();
        if (!string.IsNullOrEmpty(queryPath))
        {
          var pathRow = helper.QueryFirstOrDefault<dynamic>(
            "SELECT RESOURCEID FROM tss_moudlepath WHERE MODULEID=@mid AND PATHNAME=@p LIMIT 1",
            new { mid = moduleId, p = queryPath });
          if (pathRow != null && pathRow.RESOURCEID != null)
          {
            string rid = (string)pathRow.RESOURCEID;
            // 物理表/视图名
            var res = helper.QueryFirstOrDefault<dynamic>(
              "SELECT TABLENAME, RESOURCETYPE FROM tss_resource WHERE ID=@rid LIMIT 1", new { rid });
            tableName = res != null ? (string)res.TABLENAME : null;

            // 主表字段
            var mainResult = GetResourceFields(helper, rid);
            fields = mainResult.fields;
            refFields = mainResult.refFields;

            // 子表字段：从 tss_moudlepathrel 数据源关系查
            var subPathRows = helper.Query<dynamic>(
              "SELECT PATHNAMEB, RFIELDSA, RFIELDSB FROM tss_moudlepathrel WHERE MODULEID=@mid AND PATHNAMEA='MAIN'",
              new { mid = moduleId });
            foreach (var spr in subPathRows)
            {
              string pathName = (string)spr.PATHNAMEB;
              if (string.IsNullOrEmpty(pathName)) continue;
              var subPathRes = helper.QueryFirstOrDefault<dynamic>(
                "SELECT RESOURCEID FROM tss_moudlepath WHERE MODULEID=@mid AND PATHNAME=@p LIMIT 1",
                new { mid = moduleId, p = pathName });
              if (subPathRes != null && subPathRes.RESOURCEID != null)
              {
                string subRid = (string)subPathRes.RESOURCEID;
                var subRes = helper.QueryFirstOrDefault<dynamic>(
                  "SELECT TABLENAME, RESOURCETYPE FROM tss_resource WHERE ID=@rid LIMIT 1", new { rid = subRid });
                var subResult = GetResourceFields(helper, subRid);
                subTables.Add(new
                {
                  pathName = pathName,
                  tableName = subRes != null ? (string)subRes.TABLENAME : null,
                  relation = new { mainField = (string)spr.RFIELDSA, subField = (string)spr.RFIELDSB },
                  fields = subResult.fields,
                  refFields = subResult.refFields
                });
              }
            }
          }
        }

        return new
        {
          moduleCode = (string)mod.MODULECODE,
          moduleName = (string)mod.MODULENAME,
          remark = (string)mod.REMARK,
          tableName,
          apis = apiList,
          queryFilterParams = filterParams,
          fields,
          refFields,
          subTables,
        };
      }
    }

    /// <summary>
    /// 获取资源的字段定义（主表/子表共用）
    /// </summary>
    public static (List<object> fields, List<object> refFields) GetResourceFields(DBHelper helper, string resourceId)
    {
      var fields = new List<object>();
      var refFields = new List<object>();
      var frs = helper.Query<FieldRow>(
        @"SELECT u.FIELDNAME,
                 COALESCE(NULLIF(u.LABELNAME,''), f.FIELDANAME) AS LABEL,
                 u.EDITTYPE,
                 u.SELECTDATA,
                 u.UPDATEFIELDS,
                 f.REFRESOURCEID, f.REFRESOURCEANAME, f.REFRELATION, f.REFFIELDID
          FROM tss_resuipc u
          LEFT JOIN tss_resfield f ON u.RESFIELDID = f.ID
          WHERE u.RESOURCEID=@rid
          ORDER BY IFNULL(u.LISTSORT,99999), u.FIELDNAME
          LIMIT 40",
        new { rid = resourceId });
      // 收集引用字段，稍后批量解析引用表名 + 引用字段名
      var refRids = new HashSet<string>();
      var refFieldIds = new HashSet<string>();
      foreach (var fr in frs)
      {
        // 解析 select 字段的字典选项
        var selectOptions = new List<object>();
        if (fr.EDITTYPE == "select" && !string.IsNullOrEmpty(fr.SELECTDATA))
        {
          try
          {
            var arr = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(fr.SELECTDATA);
            if (arr != null)
            {
              foreach (var d in arr)
              {
                selectOptions.Add(new { key = d.ContainsKey("key") ? d["key"] : null, title = d.ContainsKey("title") ? d["title"] : null });
              }
            }
          }
          catch
          {
            // 不是 JSON 数组，是字典名，查 TSS_DICTITEM 获取选项
            try
            {
              var dictItems = helper.Query<dynamic>(
                "SELECT ITEMVALUE, ITEMNAME FROM TSS_DICTITEM WHERE DICTID IN (SELECT ID FROM TSS_DICT WHERE DICTCODE=@dc OR DICTNAME=@dc) ORDER BY ENTRYNUM",
                new { dc = fr.SELECTDATA });
              foreach (var di in dictItems)
              {
                selectOptions.Add(new { key = (string)di.ITEMVALUE, title = (string)di.ITEMNAME });
              }
            }
            catch { }
          }
        }
        fields.Add(new { name = fr.FIELDNAME, label = fr.LABEL, editType = fr.EDITTYPE, selectData = fr.SELECTDATA, updateFields = fr.UPDATEFIELDS, selectOptions });
        if (!string.IsNullOrEmpty(fr.REFRESOURCEID)) refRids.Add(fr.REFRESOURCEID);
        if (!string.IsNullOrEmpty(fr.REFFIELDID)) refFieldIds.Add(fr.REFFIELDID);
      }
      // 解析引用资源->表名、引用字段->字段名
      if (refRids.Count > 0)
      {
        var refTableMap = new Dictionary<string, string>();
        foreach (var rr in helper.Query<RefResourceRow>(
          "SELECT ID, TABLENAME FROM tss_resource WHERE ID IN @ids", new { ids = refRids }))
        {
          refTableMap[rr.ID] = rr.TABLENAME;
        }
        var refFieldMap = new Dictionary<string, string>();
        if (refFieldIds.Count > 0)
        {
          foreach (var rf in helper.Query<RefResourceRow>(
            "SELECT ID, FIELDNAME AS TABLENAME FROM tss_resfield WHERE ID IN @ids", new { ids = refFieldIds }))
          {
            refFieldMap[rf.ID] = rf.TABLENAME;
          }
        }
        foreach (var fr in frs)
        {
          if (string.IsNullOrEmpty(fr.REFRESOURCEID) || !refTableMap.ContainsKey(fr.REFRESOURCEID)) continue;
          string refField = fr.REFFIELDID != null && refFieldMap.ContainsKey(fr.REFFIELDID) ? refFieldMap[fr.REFFIELDID] : null;
          refFields.Add(new
          {
            field = fr.FIELDNAME,
            label = fr.LABEL,
            refTable = refTableMap[fr.REFRESOURCEID],
            alias = fr.REFRESOURCEANAME,
            on = fr.REFRELATION,
            refField,
            usage = "LEFT JOIN " + refTableMap[fr.REFRESOURCEID] + " " + (fr.REFRESOURCEANAME ?? "") + " ON " + (fr.REFRELATION ?? "")
                    + (refField != null ? "，取 " + (fr.REFRESOURCEANAME ?? "") + "." + refField : "")
          });
        }
      }
      return (fields, refFields);
    }

    /// <summary>
    /// 获取模块页面配置 + 按钮配置 (tss_module_page + tss_module_button)
    /// </summary>
    public static object GetModulePages(string moduleCode)
    {
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var pages = helper.Query<ModulePageRow>(
          @"SELECT ID, MODULECODE, PAGECODE, PAGENAME, PAGETYPE, ROUTEPATH,
                   COMPONENTTYPE, SFCMODULEPATH, PAGECONFIG,
                   QUERYAPICODE, OPENAPICODE, SAVEAPICODE
            FROM tss_module_page
            WHERE MODULECODE=@mc AND ISDELETED=0
            ORDER BY PAGETYPE", new { mc = moduleCode });
        if (pages == null || !pages.Any()) return new { moduleCode, pages = new List<object>() };

        var result = new List<object>();
        foreach (var p in pages)
        {
          var btns = helper.Query<ModuleButtonRow>(
            @"SELECT BTNNAME, BTNTYPE, BTNAREA, INTERACTTYPE, SHOWCOND, PERMCODE,
                     ICON, COLOR, EXTPARAM, APICODE
              FROM tss_module_button
              WHERE PAGEID=@pid AND ISDELETED=0
              ORDER BY BTNAREA, ID", new { pid = p.ID });
          var btnList = new List<object>();
          if (btns != null)
          {
            foreach (var b in btns)
            {
              btnList.Add(new
              {
                btnName = b.BTNNAME,
                btnType = b.BTNTYPE,
                btnArea = b.BTNAREA,
                interactType = b.INTERACTTYPE,
                showCond = b.SHOWCOND,
                permCode = b.PERMCODE,
                icon = b.ICON,
                color = b.COLOR,
                extParam = b.EXTPARAM,
                apiCode = b.APICODE
              });
            }
          }
          result.Add(new
          {
            pageCode = p.PAGECODE,
            pageName = p.PAGENAME,
            pageType = p.PAGETYPE,
            routePath = p.ROUTEPATH,
            componentType = p.COMPONENTTYPE,
            sfcModulePath = p.SFCMODULEPATH,
            extendJs = ParseExtendJs(p.PAGECONFIG),
            queryApiCode = p.QUERYAPICODE,
            openApiCode = p.OPENAPICODE,
            saveApiCode = p.SAVEAPICODE,
            buttons = btnList
          });
        }
        return new { moduleCode, pages = result };
      }
    }

    /// <summary>
    /// 获取模块字段完整 UI 配置 (tss_resuipc) — 比 GetResourceFields 更多字段(REQUIRED/READONLY/DEFAULTVALUE/排序等)
    /// </summary>
    public static object GetModuleUiset(string moduleCode)
    {
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var rows = helper.Query<UisetRow>(
          @"SELECT u.FIELDNAME, u.LABELNAME, u.EDITTYPE, u.SELECTDATA,
                   u.LISTSORT, u.QUERYSORT, u.EDITSORT,
                   u.NULLABLE, u.EDITABLE, u.DEFAULTVALUE
            FROM tss_resuipc u
            LEFT JOIN tss_moudlepath mp ON mp.RESOURCEID = u.RESOURCEID
            WHERE mp.MODULEID = (SELECT ID FROM tss_moudle WHERE MODULECODE=@mc)
              AND mp.PATHNAME IN ('QRY','MAIN')
            ORDER BY mp.PATHNAME, IFNULL(u.EDITSORT,99999), u.FIELDNAME",
          new { mc = moduleCode });
        var fields = new List<object>();
        if (rows != null)
        {
          foreach (var r in rows)
          {
            fields.Add(new
            {
              fieldName = r.FIELDNAME,
              labelName = r.LABELNAME,
              editType = r.EDITTYPE,
              selectData = r.SELECTDATA,
              listSort = r.LISTSORT,
              querySort = r.QUERYSORT,
              editSort = r.EDITSORT,
              required = (r.NULLABLE ?? 1) == 0,
              readonlyField = (r.EDITABLE ?? 1) == 0,
              defaultValue = r.DEFAULTVALUE
            });
          }
        }
        return new { moduleCode, fields };
      }
    }

    /// <summary>从 tss_module_page.PAGECONFIG JSON 解析 EXTENDJS 路径（EXTENDJS 不是独立列，存在 PAGECONFIG JSON）</summary>
    private static string ParseExtendJs(string pageConfigJson)
    {
      if (string.IsNullOrEmpty(pageConfigJson)) return null;
      try
      {
        var jo = JsonConvert.DeserializeObject<dynamic>(pageConfigJson);
        return jo != null && jo.EXTENDJS != null ? (string)jo.EXTENDJS : null;
      }
      catch { return null; }
    }

    /// <summary>
    /// 搜索统一代码资产表中的 SQL 模板（原 tss_sql 已并入 tss_code_asset），供 AI 查找可用接口
    /// </summary>
    public static object GetSqlList(string keyword)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        string sql;
        object param;
        if (!string.IsNullOrEmpty(keyword))
        {
          sql = @"SELECT CODE AS SQLCODE, SOURCECODE AS SQLTXT, REMARK FROM tss_code_asset
                  WHERE ASSETTYPE='sql' AND ISDELETED=0 AND (CODE LIKE @kw OR REMARK LIKE @kw)
                  ORDER BY CODE LIMIT 30";
          param = new { kw = "%" + keyword + "%" };
        }
        else
        {
          sql = @"SELECT CODE AS SQLCODE, SOURCECODE AS SQLTXT, REMARK FROM tss_code_asset
                  WHERE ASSETTYPE='sql' AND ISDELETED=0 ORDER BY CODE LIMIT 30";
          param = null;
        }
        var rows = helper.Query<SqlRow>(sql, param);
        var list = new List<object>();
        foreach (var r in rows)
        {
          // SQLTXT 截断防止过长（AI 只需知道是否存在 + 大概用途）
          string txt = r.SQLTXT;
          if (!string.IsNullOrEmpty(txt) && txt.Length > 300)
            txt = txt.Substring(0, 300) + "...";
          list.Add(new { sqlCode = r.SQLCODE, sqlTxt = txt, remark = r.REMARK });
        }
        return new { count = list.Count, items = list };
      }
    }

    /// <summary>
    /// 模块代码文件清单（AI 多文件联动开发用）：模块相关的 C# 脚本 / SQL 模板 / JS 模块 / VUE 组件。
    /// 只返回标识信息（不含源码，防超长）；读内容用 ReadCodeAsset。
    /// 模块归属规则: csharp/sql 按 SC_/SS_+模块编码前缀 或 moudleapi 关联；js/vue 按 @/modules/{MC}/ 路径
    /// </summary>
    public static object GetModuleFiles(string moduleCode)
    {
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var rows = helper.Query<dynamic>(
          @"SELECT A.ASSETTYPE AS kind, A.CODE AS code, A.NAME AS name, A.MODULEPATH AS path,
                   (SELECT GROUP_CONCAT(B.APICODE) FROM tss_moudleapi B
                     WHERE B.MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE=@mc)
                       AND (B.SCRIPTCODE=A.CODE OR B.SQLID=A.CODE)) AS apiCodes
            FROM tss_code_asset A
            WHERE A.ISDELETED=0 AND (
              (A.ASSETTYPE='csharp' AND (A.CODE LIKE CONCAT('SC_', @mc, '\\_%') OR A.CODE IN
                (SELECT SCRIPTCODE FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE=@mc) AND SCRIPTCODE IS NOT NULL)))
              OR (A.ASSETTYPE='sql' AND (A.CODE LIKE CONCAT('SS_', @mc, '\\_%') OR A.CODE IN
                (SELECT SQLID FROM tss_moudleapi WHERE MODULEID=(SELECT ID FROM tss_moudle WHERE MODULECODE=@mc) AND SQLID IS NOT NULL)))
              OR (A.ASSETTYPE IN ('js','vue') AND A.MODULEPATH LIKE CONCAT('@/modules/', @mc, '/%')))
            ORDER BY A.ASSETTYPE, A.CODE
            LIMIT 100",
          new { mc = moduleCode });
        var list = new List<object>();
        foreach (var r in rows)
        {
          list.Add(new { kind = (string)r.kind, code = (string)r.code, name = (string)r.name, path = (string)r.path, apiCodes = (string)r.apiCodes });
        }
        return new { count = list.Count, items = list };
      }
    }

    /// <summary>
    /// 读取单个代码资产完整内容（AI 改文件前必读）。
    /// csharp/sql 按 code 精确匹配；js/vue 按 path(MODULEPATH) 精确匹配。
    /// </summary>
    public static object ReadCodeAsset(string code, string path)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<dynamic>(
          @"SELECT ASSETTYPE AS kind, CODE AS code, NAME AS name, MODULEPATH AS path, SOURCECODE AS source
            FROM tss_code_asset
            WHERE ISDELETED=0 AND ((@code <> '' AND CODE=@code) OR (@path <> '' AND MODULEPATH=@path))
            LIMIT 1",
          new { code = code ?? "", path = path ?? "" });
        if (row == null) return new { error = "资产不存在: " + (string.IsNullOrEmpty(code) ? path : code) };
        return new { kind = (string)row.kind, code = (string)row.code, name = (string)row.name, path = (string)row.path, source = (string)row.source };
      }
    }

    public static string ApiTypeDesc(string t)
    {
      switch (t)
      {
        case "query": return "列表查询";
        case "open": return "打开详情";
        case "save": return "保存(新增/修改)";
        case "delete": return "删除";
        case "submit": return "提交";
        case "reSubmit": return "重新提交";
        case "check": return "复核/审核";
        case "reCheck": return "撤销审核";
        case "verify": return "审批";
        case "reVerify": return "撤销审批";
        default: return t;
      }
    }

    // =================== 辅助行类型 ===================
    // 与 AssistantToolExecutor 中的同名类保持一致，供 DBHelper.Query<T> 映射

    public class ApiRow
    {
      public string APICODE;
      public string APITYPE;
      public string APINAME;
      public string FILTERCODE;
      public string PATHNAME;
    }

    public class FilterRow
    {
      public string FILTERSQL;
    }

    public class FieldRow
    {
      public string FIELDNAME;
      public string LABEL;
      public string EDITTYPE;
      public string SELECTDATA;
      public string UPDATEFIELDS;
      public string REFRESOURCEID;
      public string REFRESOURCEANAME;
      public string REFRELATION;
      public string REFFIELDID;
    }

    public class RefResourceRow
    {
      public string ID;
      public string TABLENAME;
    }

    public class ModulePageRow
    {
      public string ID;
      public string MODULECODE;
      public string PAGECODE;
      public string PAGENAME;
      public string PAGETYPE;
      public string ROUTEPATH;
      public string COMPONENTTYPE;
      public string SFCMODULEPATH;
      public string PAGECONFIG;
      public string QUERYAPICODE;
      public string OPENAPICODE;
      public string SAVEAPICODE;
    }

    public class ModuleButtonRow
    {
      public string BTNNAME;
      public string BTNTYPE;
      public string BTNAREA;
      public string INTERACTTYPE;
      public string SHOWCOND;
      public string PERMCODE;
      public string ICON;
      public string COLOR;
      public string EXTPARAM;
      public string APICODE;
    }

    public class SqlRow
    {
      public string SQLCODE;
      public string SQLTXT;
      public string REMARK;
    }

    public class UisetRow
    {
      public string FIELDNAME;
      public string LABELNAME;
      public string EDITTYPE;
      public string SELECTDATA;
      public string QUERYTYPE;
      public string QUERYMODE;
      public int? LISTSORT;
      public int? QUERYSORT;
      public int? EDITSORT;
      public int? NULLABLE;
      public int? EDITABLE;
      public string DEFAULTVALUE;
    }
  }
}
