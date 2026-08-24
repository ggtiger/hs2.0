using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Realso.Data.ORM.Core;
namespace Realso.Data.ORM
{
  public class BuildSQL01 : IBuildSQL
  {
    public string BuildQuery(Resource resource, QueryInfo queryInfo)
    {
      string sql = "";
      if (resource.RESOURCETYPE == "SQL")
      {
        return this.BuildQueryBySQLID(resource, queryInfo);
      }
      List<ResourceField> fields = resource.Fields;
      List<ResourceFilter> filters = resource.Filters;
      ArrayList afields = new ArrayList();
      ArrayList aJoins = new ArrayList();
      ResourceFilter filter = filters.FirstOrDefault((ResourceFilter f) =>
      {
        return f.FILTERCODE == queryInfo.FilterCode;
      });
      aJoins.Add($" {resource.TABLENAME} {resource.RESOURCEANAME} ");
      fields.ForEach((ResourceField field) =>
      {
        //引用类型
        if ("" != field.REFRESOURCEID + "")
        {
          Resource refResouce = SchemaManage.GetResource(field.REFRESOURCEID);
          if ("TABLE" == refResouce.RESOURCETYPE || "VIEW" == refResouce.RESOURCETYPE)
          {
            aJoins.Add($" LEFT JOIN {refResouce.TABLENAME} {field.REFRESOURCEANAME} ON {field.REFRELATION} ");
          }
          if ("SQL" == refResouce.RESOURCETYPE)
          {
            aJoins.Add($" LEFT JOIN ({refResouce.SQLCODE}) {field.REFRESOURCEANAME} ON {field.REFRELATION} ");
          }
          if ("DATAVIEW" == refResouce.RESOURCETYPE)
          {
            //TODO暂不支持多层
          }
        }
        if (field.REFFIELDID + "" != "")
        {
          //引用子类型
          if (field.UPFIELDID + "" != "")
          {
            ResourceField upField = fields.First((ResourceField f) =>
                  {
                    return f.ID == field.UPFIELDID;
                  });
            if ("datetime" == field.FIELDTYPE)
            {
              afields.Add($" date_format({upField.REFRESOURCEANAME}.{field.REFFIELDNAME},'%Y-%m-%d %H:%i:%s') AS {field.FIELDNAME} ");
            }
            else if ("date" == field.FIELDTYPE)
            {
              afields.Add($" date_format({upField.REFRESOURCEANAME}.{field.REFFIELDNAME},'%Y-%m-%d') AS {field.FIELDNAME} ");
            }
            else
            {
              afields.Add($" {upField.REFRESOURCEANAME}.{field.REFFIELDNAME} AS {field.FIELDNAME} ");
            }
          }
          else
          {
            if ("datetime" == field.FIELDTYPE)
            {
              afields.Add($" date_format({resource.RESOURCEANAME}.{field.REFFIELDNAME},'%Y-%m-%d %H:%i:%s') AS {field.FIELDNAME} ");
            }
            else if ("date" == field.FIELDTYPE)
            {
              afields.Add($" date_format({resource.RESOURCEANAME}.{field.REFFIELDNAME},'%Y-%m-%d') AS {field.FIELDNAME} ");
            }
            else
            {
              afields.Add($" {resource.RESOURCEANAME}.{field.REFFIELDNAME} AS {field.FIELDNAME} ");
            }
          }
        }
        //其他
        else
        {
          afields.Add(" NULL AS " + field.FIELDNAME);
        }
      });

      string filedString = string.Join(",", afields.ToArray());
      string joinString = string.Join(" ", aJoins.ToArray());

      // @ui 过滤器自动生成：FILTERSQL 中 @ui / @ui:adv 作为占位符，替换为自动生成的条件
      // 支持与手写 SQL 混合：如 "A.STATE IN (2) AND @ui:adv AND A.DEPTID = @_DEPTID_"
      string filterString;
      if (filter != null && filter.FILTERSQL != null && filter.FILTERSQL.Contains("@ui"))
      {
        filterString = BuildFilterFromUI(resource, filter.FILTERSQL, queryInfo.FilterParams);
      }
      else
      {
        filterString = filter != null ? SQLManage.ParseSQL(filter.FILTERSQL, queryInfo.FilterParams) : "";
      }

      int startPage = queryInfo.PageSize * (queryInfo.PageIndex - 1);
      int endPage = queryInfo.PageSize;//queryInfo.PageIndex * queryInfo.PageSize;
      if (queryInfo.OtherWhere != "")
      {
        filterString = filterString != "" ? queryInfo.OtherWhere + " AND " + filterString : queryInfo.OtherWhere;
      }
      filterString = filterString != "" ? " WHERE " + filterString : "";

      string orderBy = queryInfo.OrderBy + "" == "" && filter != null ? filter.ORDERBY + "" : queryInfo.OrderBy;

      //不分页
      if (queryInfo.PageSize == 1)
      {
        sql = " SELECT {0} FROM {1} {2} ";
        if (orderBy != "")
        {
          sql = " SELECT * FROM (SELECT {0} FROM {1} {2}) T ORDER BY " + orderBy;
        }
        sql = string.Format(sql, filedString, joinString, filterString);
      }
      else
      {
        string oSum = "";
        if (queryInfo.SumFields != "")
        {
          string[] fs = queryInfo.SumFields.Split(',');
          for (int i = 0; i < fs.Length; i++)
          {
            oSum += $" ,Sum({fs[i]}) {fs[i]} ";
          }
        }
        sql = @" SELECT {0} FROM {1} {2} LIMIT {3},{4};
                      SELECT count(1) C" + oSum + " FROM {1} {2} ";
        if (orderBy != "")
        {
          sql = " SELECT * FROM (SELECT {0} FROM {1} {2}) T ORDER BY " + orderBy + @" LIMIT {3},{4};
                    SELECT count(1) C" + oSum + " FROM {1} {2} ";
        }
        sql = string.Format(sql, filedString, joinString, filterString, startPage, endPage);
      }
      return sql;
    }

    /// <summary>
    /// 根据 resuipc 配置自动生成过滤器 SQL 条件
    /// FILTERSQL 中 @ui / @ui:adv 作为占位符，被替换为自动生成的条件
    /// 支持与手写 SQL 混合：如 "A.STATE IN (2) AND @ui:adv AND A.DEPTID = @_DEPTID_"
    ///
    /// F01 @ui: INPUT LIKE OR 块仅包含 QUERYSORT>0 且推导为 like 的字段（手动指定，避免全表扫描）
    /// F02 @ui:adv: 资源所有字段都生成查询条件，按 resuipc QUERYMODE + FIELDTYPE 自动推导
    /// </summary>
    private string BuildFilterFromUI(Resource resource, string filterSQL, Hashtable filterParams)
    {
      // 检查资源是否有 ISDELETED 字段（tss_ 系统表没有）
      bool hasIsDeleted = resource.Fields != null && resource.Fields.Any(f => f.FIELDNAME == "ISDELETED");
      string alias = resource.RESOURCEANAME;

      // 匹配 @ui 占位符：@ui / @ui:adv / @ui:adv:RESOURCEID
      var match = System.Text.RegularExpressions.Regex.Match(filterSQL, @"@ui(:adv)?(:[A-Za-z0-9_]+)?");
      if (!match.Success) return SQLManage.ParseSQL(filterSQL, filterParams);

      string uiTag = match.Value;
      bool isAdvMode = uiTag.Contains(":adv");
      string targetResourceId = ParseUiResourceId(uiTag, resource);

      // 获取 uiset 配置：优先用 resource.UisetFields（GetResource 时已加载）
      Dictionary<string, UisetField> uisetDict = resource.UisetFields;
      // 如果指定了不同的目标资源ID，需要单独加载
      if (targetResourceId != resource.ID || uisetDict == null)
      {
        uisetDict = SchemaManage.GetUisetAllFields(targetResourceId);
      }

      // 生成 @ui 占位符对应的 SQL 条件
      string uiCondition;
      if (isAdvMode)
      {
        uiCondition = BuildAdvFilterCore(resource, uisetDict, alias, hasIsDeleted);
      }
      else
      {
        // F01 模式：DISPLAYINLIST=1 的字段中，推导为 like 的进 INPUT OR 块
        List<UisetField> listFields = uisetDict != null
          ? uisetDict.Values.Where(u => u.DISPLAYINLIST.HasValue && u.DISPLAYINLIST.Value == 1).ToList()
          : new List<UisetField>();
        if (listFields.Count == 0)
        {
          uiCondition = hasIsDeleted ? alias + ".ISDELETED = 0" : "1=1";
        }
        else
        {
          uiCondition = BuildSimpleFilterCore(listFields, alias, hasIsDeleted, uisetDict);
        }
      }

      // 将 @ui 占位符替换为生成的条件，其余手写部分保留，统一 NVelocity 解析
      string combined = filterSQL.Substring(0, match.Index) + uiCondition + filterSQL.Substring(match.Index + match.Length);

      // DEBUG: 打印 @ui 生成的模板
      System.Console.WriteLine("=== @ui DEBUG ===");
      System.Console.WriteLine("FILTERSQL原文: " + filterSQL);
      System.Console.WriteLine("uisetDict count: " + (uisetDict != null ? uisetDict.Count : -1));
      if (uisetDict != null)
      {
        foreach (var kv in uisetDict.Values.Take(5))
        {
          System.Console.WriteLine("  field=" + kv.FIELDNAME + " DISPLAYINLIST=" + kv.DISPLAYINLIST + " QUERYSORT=" + kv.QUERYSORT + " EDITTYPE=" + kv.EDITTYPE);
        }
      }
      System.Console.WriteLine("uiCondition: " + uiCondition);
      System.Console.WriteLine("combined模板: " + combined);
      System.Console.WriteLine("================");

      return SQLManage.ParseSQL(combined, filterParams);
    }

    /// <summary>
    /// 解析 @ui 中的目标资源ID
    /// 格式: @ui / @ui:adv / @ui:adv:RESOURCEID
    /// </summary>
    private string ParseUiResourceId(string filterSQL, Resource currentResource)
    {
      string[] parts = filterSQL.Split(':');
      // @ui:adv:RESOURCEID → parts = ["@ui", "adv", "RESOURCEID"]
      if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
      {
        return parts[2];
      }
      return currentResource.ID;
    }

    /// <summary>
    /// 推导字段的查询匹配方式
    /// 优先级: QUERYMODE(显式) > QUERYTYPE/EDITTYPE 推导 > FIELDTYPE+ISKEY+REFRESOURCEID 推导
    ///
    /// 推导策略：
    /// - 主键(ISKEY=1) → eq
    /// - 外键(REFRESOURCEID非空) → eq
    /// - 有 resuipc 的 QUERYTYPE/EDITTYPE → 按控件类型推导（input→like, select→eq 等）
    /// - 无 resuipc 时按 FIELDTYPE 推导：
    ///   - varchar/text → eq（多数 varchar 是 ID/CODE 编码，不适合 like；需要 like 的在 resuipc 配 QUERYMODE=like）
    ///   - int/decimal/datetime → eq
    /// </summary>
    private string DeriveQueryMode(UisetField uiset, ResourceField resField)
    {
      // 1. 显式设置 QUERYMODE 优先
      if (uiset != null && !string.IsNullOrEmpty(uiset.QUERYMODE))
      {
        return uiset.QUERYMODE;
      }

      // 2. 有 resuipc 配置时，按 QUERYTYPE（优先）或 EDITTYPE 推导
      if (uiset != null)
      {
        string controlType = uiset.QUERYTYPE ?? uiset.EDITTYPE ?? "";
        switch (controlType)
        {
          case "input":
          case "text":
          case "textarea":
            return "like";
          case "select": return "eq";
          case "datepicker": return "eq";
          case "daterange": return "range";
          case "autocomplete": return "eq";
          case "number": return "eq";
        }
      }

      // 3. 无 resuipc 配置或 EDITTYPE 无法推导时，按 FIELDTYPE + ISKEY + REFRESOURCEID 推导
      if (resField != null)
      {
        // 主键 → 精确匹配
        if (resField.ISKEY + "" == "1")
        {
          return "eq";
        }
        // 有引用资源（外键） → 精确匹配
        if (!string.IsNullOrEmpty(resField.REFRESOURCEID))
        {
          return "eq";
        }
        // 按字段类型推导：无 resuipc 时 varchar 默认 eq（多数是 ID/CODE 编码）
        string fieldType = (resField.FIELDTYPE ?? "").ToLower();
        switch (fieldType)
        {
          case "varchar":
          case "text":
            return "eq";
          case "int":
          case "tinyint":
          case "bigint":
          case "decimal":
          case "float":
          case "double":
          case "datetime":
          case "date":
            return "eq";
          default:
            return "eq";
        }
      }

      // 4. 都没有，默认精确匹配
      return "eq";
    }

    /// <summary>
    /// 获取字段在 SQL 中的列名（考虑引用字段）
    /// 外键引用字段（REFRESOURCEANAME 非空）在主表中存的是外键 ID，WHERE 条件必须用 REFFIELDNAME（物理列名）
    /// 例如 PROVINCENAME 引用了 TBS_REGION，主表实际列名是 PROVINCEID
    /// 注意：REFFIELDID 在 DATAVIEW 中只是指向物理表字段的映射，不是外键引用
    /// </summary>
    private string GetFieldColumnName(UisetField field, string alias)
    {
      // 外键引用字段：主表中存的是外键，用 REFFIELDNAME（如 PROVINCEID）
      if (!string.IsNullOrEmpty(field.REFRESOURCEANAME) && !string.IsNullOrEmpty(field.REFFIELDNAME))
      {
        return alias + "." + field.REFFIELDNAME;
      }
      return alias + "." + field.FIELDNAME;
    }

    /// <summary>
    /// F01 模式：模糊搜索（生成 NVelocity 模板，由调用方统一 ParseSQL）
    /// <summary>
    /// F01 模式：模糊搜索（生成 NVelocity 模板，由调用方统一 ParseSQL）
    /// DISPLAYINLIST=1 且 DeriveQueryMode=like 的字段合并到一个 INPUT LIKE OR 块
    /// 列名生成逻辑与 BuildQuery SELECT 一致：
    /// - 有 UPFIELDID 的子引用字段：用父字段的 REFRESOURCEANAME.本字段REFFIELDNAME（如 B.REGION_NAME）
    /// - 有 REFFIELDID 无 UPFIELDID 的 DATAVIEW 映射字段：用 alias.本字段REFFIELDNAME（如 A.CUSTCODE）
    /// - 无 REFFIELDID 的普通字段：用 alias.FIELDNAME
    /// </summary>
    private string BuildSimpleFilterCore(List<UisetField> listFields, string alias, bool hasIsDeleted, Dictionary<string, UisetField> uisetDict)
    {
      List<string> likeFields = new List<string>();

      foreach (var field in listFields)
      {
        string mode = DeriveQueryMode(field, null);
        System.Console.WriteLine("  field=" + field.FIELDNAME + " EDITTYPE=" + field.EDITTYPE + " QUERYTYPE=" + field.QUERYTYPE + " mode=" + mode + " UPFIELDID=" + field.UPFIELDID + " REFFIELDID=" + field.REFFIELDID + " REFFIELDNAME=" + field.REFFIELDNAME + " REFRESOURCEANAME=" + field.REFRESOURCEANAME);
        if (mode == "like")
        {
          string colName = GetUisetColumnName(field, alias, uisetDict);
          likeFields.Add(colName + " LIKE CONCAT('%',@INPUT,'%')");
        }
      }

      // 组装 FILTERSQL
      string result = "1=1";

      // INPUT LIKE OR 块
      if (likeFields.Count > 0)
      {
        result += "\n#if(\"$!{INPUT}\"!=\"\")\nAND (" + string.Join("\nOR ", likeFields) + ")\n#end";
      }

      // 逻辑删除过滤（仅业务表有 ISDELETED 字段）
      if (hasIsDeleted)
      {
        result += "\nAND " + alias + ".ISDELETED = 0";
      }

      return result;
    }

    /// <summary>
    /// 获取 uiset 字段在 WHERE 条件中的列名，与 BuildQuery SELECT 逻辑一致
    /// - 子引用字段（有 UPFIELDID）：用父字段的 REFRESOURCEANAME.本字段REFFIELDNAME
    /// - DATAVIEW 映射字段（有 REFFIELDID 无 UPFIELDID）：用 alias.本字段REFFIELDNAME
    /// - 普通字段（无 REFFIELDID）：用 alias.FIELDNAME
    /// </summary>
    private string GetUisetColumnName(UisetField field, string alias, Dictionary<string, UisetField> uisetDict)
    {
      // 子引用字段：用父字段的 JOIN 别名（与 BuildQuery 中 f.ID == field.UPFIELDID 逻辑一致）
      if (!string.IsNullOrEmpty(field.UPFIELDID))
      {
        UisetField parentField = uisetDict.Values.FirstOrDefault(f => f.RESFIELDID == field.UPFIELDID);
        if (parentField != null && !string.IsNullOrEmpty(parentField.REFRESOURCEANAME) && !string.IsNullOrEmpty(field.REFFIELDNAME))
        {
          return parentField.REFRESOURCEANAME + "." + field.REFFIELDNAME;
        }
      }
      // DATAVIEW 映射字段：用主表别名 + 物理列名（REFFIELDNAME）
      if (!string.IsNullOrEmpty(field.REFFIELDID) && !string.IsNullOrEmpty(field.REFFIELDNAME))
      {
        return alias + "." + field.REFFIELDNAME;
      }
      // 普通字段
      return alias + "." + field.FIELDNAME;
    }

    /// <summary>
    /// F01 兼容方法：生成条件并直接 ParseSQL
    /// </summary>
    private string BuildSimpleFilter(List<UisetField> listFields, string alias, Hashtable filterParams, bool hasIsDeleted, Dictionary<string, UisetField> uisetDict)
    {
      return SQLManage.ParseSQL(BuildSimpleFilterCore(listFields, alias, hasIsDeleted, uisetDict), filterParams);
    }

    /// <summary>
    /// F02 模式：高级查询（生成 NVelocity 模板，由调用方统一 ParseSQL）
    /// 资源所有字段都生成查询条件，按 QUERYMODE + FIELDTYPE 自动推导匹配方式
    /// 有 UPFIELDID 的子引用字段不生成独立条件（它依赖父字段的 JOIN）
    /// </summary>
    private string BuildAdvFilterCore(Resource resource, Dictionary<string, UisetField> uisetDict, string alias, bool hasIsDeleted)
    {
      List<string> conditions = new List<string>();

      foreach (var resField in resource.Fields)
      {
        // 跳过虚拟字段
        if (resField.ISVIRTUAL == "1")
        {
          continue;
        }

        // 查找 resuipc 配置（可能没有）
        UisetField uiset = null;
        uisetDict.TryGetValue(resField.FIELDNAME, out uiset);

        // 推导匹配方式
        string mode = DeriveQueryMode(uiset, resField);
        string fieldName = resField.FIELDNAME;

        // 获取 SQL 列名：与 BuildQuery SELECT 逻辑一致
        // - 子引用字段（有 UPFIELDID）：用父字段的 REFRESOURCEANAME.本字段REFFIELDNAME（如 B.REGION_NAME）
        // - 外键引用字段（有 REFRESOURCEID）：用 alias.本字段REFFIELDNAME（如 A.PROVINCEID）
        // - DATAVIEW 映射字段（有 REFFIELDID 无 UPFIELDID 无 REFRESOURCEID）：用 alias.REFFIELDNAME
        // - 普通字段：用 alias.FIELDNAME
        string colName;
        if (!string.IsNullOrEmpty(resField.UPFIELDID))
        {
          // 子引用字段：找父字段，用父字段的 JOIN 别名
          ResourceField upField = resource.Fields.FirstOrDefault(f => f.ID == resField.UPFIELDID);
          if (upField != null && !string.IsNullOrEmpty(upField.REFRESOURCEANAME) && !string.IsNullOrEmpty(resField.REFFIELDNAME))
          {
            colName = upField.REFRESOURCEANAME + "." + resField.REFFIELDNAME;
          }
          else
          {
            // 找不到父字段，用主表别名
            colName = alias + "." + (resField.REFFIELDNAME ?? fieldName);
          }
        }
        else if (!string.IsNullOrEmpty(resField.REFFIELDID) && !string.IsNullOrEmpty(resField.REFFIELDNAME))
        {
          colName = alias + "." + resField.REFFIELDNAME;
        }
        else
        {
          colName = alias + "." + fieldName;
        }

        if (mode == "like")
        {
          conditions.Add("#if(\"$!{" + fieldName + "}\"!=\"\")\nAND " + colName + " LIKE CONCAT('%',@" + fieldName + ",'%')\n#end");
        }
        else if (mode == "eq")
        {
          string fieldType = (resField.FIELDTYPE ?? "").ToLower();
          if (fieldType == "datetime")
          {
            conditions.Add("#if(\"$!{" + fieldName + "}\"!=\"\")\nAND " + colName + " = str_to_date(@" + fieldName + ",'%Y-%m-%d %H:%i:%s')\n#end");
          }
          else if (fieldType == "date")
          {
            conditions.Add("#if(\"$!{" + fieldName + "}\"!=\"\")\nAND " + colName + " = str_to_date(@" + fieldName + ",'%Y-%m-%d')\n#end");
          }
          else
          {
            conditions.Add("#if(\"$!{" + fieldName + "}\"!=\"\")\nAND " + colName + " = @" + fieldName + "\n#end");
          }
        }
        else if (mode == "in")
        {
          conditions.Add("#if(\"$!{" + fieldName + "}\"!=\"\")\nAND " + colName + " IN (@" + fieldName + ")\n#end");
        }
        else if (mode == "range")
        {
          conditions.Add("#if(\"$!{" + fieldName + "_start}\"!=\"\")\nAND " + colName + " >= str_to_date(@" + fieldName + "_start,'%Y-%m-%d')\n#end");
          conditions.Add("#if(\"$!{" + fieldName + "_end}\"!=\"\")\nAND " + colName + " <= str_to_date(@" + fieldName + "_end,'%Y-%m-%d')\n#end");
        }
      }

      // 组装 FILTERSQL
      string result = "1=1";
      foreach (var cond in conditions)
      {
        result += "\n" + cond;
      }

      // 逻辑删除过滤（仅业务表有 ISDELETED 字段）
      if (hasIsDeleted)
      {
        result += "\nAND " + alias + ".ISDELETED = 0";
      }

      return result;
    }

    /// <summary>
    /// F02 兼容方法：生成条件并直接 ParseSQL
    /// </summary>
    private string BuildAdvFilter(Resource resource, Dictionary<string, UisetField> uisetDict, string alias, Hashtable filterParams, bool hasIsDeleted)
    {
      return SQLManage.ParseSQL(BuildAdvFilterCore(resource, uisetDict, alias, hasIsDeleted), filterParams);
    }

    private string BuildQueryBySQLID(Resource resource, QueryInfo queryInfo)
    {
      string sql = "";
      List<ResourceFilter> filters = resource.Filters;
      ResourceFilter filter = filters.FirstOrDefault((ResourceFilter f) =>
     {
       return f.FILTERCODE == queryInfo.FilterCode;
     });
      string sqlId = filter.FILTERSQL;
      string tsql = SQLManage.ParseSQL(SQLManage.GetSQL(sqlId), queryInfo.FilterParams);
      List<ResourceField> fields = resource.Fields;
      ArrayList afields = new ArrayList();
      fields.ForEach((ResourceField field) =>
      {
        if ("datetime" == field.FIELDTYPE)
        {
          afields.Add($" date_format({field.FIELDNAME},'%Y-%m-%d %H:%i:%s') AS {field.FIELDNAME} ");
        }
        else if ("date" == field.FIELDTYPE)
        {
          afields.Add($" date_format({field.FIELDNAME},'%Y-%m-%d') AS {field.FIELDNAME} ");
        }
        else
        {
          afields.Add($" {field.FIELDNAME} AS {field.FIELDNAME} ");
        }
      });

      string orderBy = queryInfo.OrderBy + "" == "" && filter != null ? filter.ORDERBY + "" : queryInfo.OrderBy;
      string filedString = string.Join(",", afields.ToArray());
      int startPage = queryInfo.PageSize * (queryInfo.PageIndex - 1);
      int endPage = queryInfo.PageSize;//queryInfo.PageIndex * queryInfo.PageSize;
      //不分页
      if (queryInfo.PageSize == 1)
      {
        sql = " SELECT {0} FROM ({1}) T ";
        if (orderBy != "")
        {
          sql = " SELECT {0} FROM ({1}) T ORDER BY " + orderBy;
        }
        sql = string.Format(sql, filedString, tsql);
      }
      else
      {
        string oSum = "";
        if (queryInfo.SumFields != "")
        {
          string[] fs = queryInfo.SumFields.Split(',');
          for (int i = 0; i < fs.Length; i++)
          {
            oSum += $" ,Sum({fs[i]}) {fs[i]} ";
          }
        }
        sql = @" SELECT {0} FROM  ({1}) T LIMIT {2},{3};
                      SELECT count(1) C" + oSum + " FROM  ({1}) T ";
        if (orderBy != "")
        {
          sql = " SELECT {0} FROM  ({1}) T ORDER BY " + orderBy + @" LIMIT {2},{3};
                    SELECT count(1) C" + oSum + " FROM  ({1}) T ";
        }
        sql = string.Format(sql, filedString, tsql, startPage, endPage);
      }
      return sql;
    }

    public string BuildSave(DataView view)
    {
      string sql = "";
      return sql;
    }




    private string getFieldsString(Resource resource)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (field.UPFIELDID + "" == "" && field.ISVIRTUAL != "1")
          sql += "," + field.FIELDNAME;
      });
      return sql.Substring(1);
    }
    private string getFieldsStringPa(Resource resource)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (field.UPFIELDID + "" == "" && field.ISVIRTUAL != "1")
          sql += ",@" + field.FIELDNAME;
      });
      return sql.Substring(1);
    }

    public string BuildInsert(DataView view, ViewRow row)
    {
      Resource resource = view.Resource;
      string sql = $"INSERT INTO {resource.TABLENAME}({getFieldsString(resource)}) VALUES({getFieldsStringPa(resource)});";
      return sql;
    }


    private string getInsertStr(Resource resource, ViewRow row)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (!(field.UPFIELDID + "" == "" && field.ISVIRTUAL != "1"))
          return;
        if (row.GetString(field.FIELDNAME) == "" || row.GetString(field.FIELDNAME) == "null")
        {
          sql += $",NULL ";
        }
        else if ("varchar" == field.FIELDTYPE || "text" == field.FIELDTYPE)
        {
          sql += $",'{row.GetString(field.FIELDNAME).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")}'";
        }
        else if ("datetime" == field.FIELDTYPE)
        {
          // 日期处理：确保 DateTime 对象格式化为 yyyy-MM-dd HH:mm:ss
          var dtVal = row[field.FIELDNAME];
          string dtStr = dtVal is DateTime ? ((DateTime)dtVal).ToString("yyyy-MM-dd HH:mm:ss") : dtVal + "";
          sql += $",str_to_date('{dtStr}','%Y-%m-%d %H:%i:%s') ";
        }
        else if ("date" == field.FIELDTYPE)
        {
          // 日期处理：确保 DateTime 对象格式化为 yyyy-MM-dd
          var dtVal = row[field.FIELDNAME];
          string dtStr = dtVal is DateTime ? ((DateTime)dtVal).ToString("yyyy-MM-dd") : dtVal + "";
          sql += $",str_to_date('{dtStr}','%Y-%m-%d') ";
        }
        else
        {
          sql += $",{row[field.FIELDNAME]} ";
        }
      });
      return $",({sql.Substring(1)})";
    }

    public string BuildBatchInsert(DataView view)
    {
      Resource resource = view.Resource;
      string sql = $"INSERT INTO {resource.TABLENAME}({getFieldsString(resource)}) VALUES";
      string tinsert = "";
      if (view.Inserted.Count > 0)
      {
         view.Inserted.ForEach((row)=>{
             tinsert+=getInsertStr(resource,row);
         }) ;
      }
      return $"{sql}{tinsert.Substring(1)}";
    }


    private string joinParam(Resource resource, ViewRow row)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (field.UPFIELDID + "" != "" || row.GetString(field.FIELDNAME) == row.GetOldString(field.FIELDNAME))
          return;
        if (row.GetString(field.FIELDNAME) == "" || row.GetString(field.FIELDNAME) == "null")
        {
          sql += $", {field.FIELDNAME}=NULL ";
        }
        else if ("varchar" == field.FIELDTYPE || "text" == field.FIELDTYPE)
        {
          sql += $", {field.FIELDNAME}='{row.GetString(field.FIELDNAME).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")}'";
        }
        else if ("datetime" == field.FIELDTYPE)
        {
          //TODO:日期处理
          //str_to_date('2008-08-09 08:9:30', '%Y-%m-%d %h:%i:%s');
          sql += $", {field.FIELDNAME}=str_to_date('{row[field.FIELDNAME]}','%Y-%m-%d %H:%i:%s') ";
        }
        else if ("date" == field.FIELDTYPE)
        {
          //TODO:日期处理
          //str_to_date('2008-08-09 08:9:30', '%Y-%m-%d %h:%i:%s');
          sql += $", {field.FIELDNAME}=str_to_date('{row[field.FIELDNAME]}','%Y-%m-%d') ";
        }
        else
        {
          var val = row[field.FIELDNAME] + "";
          if (string.IsNullOrWhiteSpace(val) || val == "null")
            sql += $", {field.FIELDNAME}=NULL ";
          else
            sql += $", {field.FIELDNAME}={row[field.FIELDNAME]} ";
        }
      });
      return sql.Substring(1);
    }
    private string joinWhere(Resource resource, ViewRow row)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (field.UPFIELDID + "" != "" || field.ISVIRTUAL == "1")
          return;
        if (row.GetOldString(field.FIELDNAME) == "")
        {
          if ("datetime" == field.FIELDTYPE || "date" == field.FIELDTYPE || "float" == field.FIELDTYPE || "int" == field.FIELDTYPE || "decimal" == field.FIELDTYPE || "tinyint" == field.FIELDTYPE)
          {
            sql += $"AND ({field.FIELDNAME} IS NULL ) ";
          }
          else
          {
            sql += $"AND ({field.FIELDNAME} IS NULL OR  {field.FIELDNAME}='') ";
          }

        }
        else if ("varchar" == field.FIELDTYPE)
        {
          sql += $"AND {field.FIELDNAME}='{row.GetOldString(field.FIELDNAME).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")}' ";
        }
        else if ("text" == field.FIELDTYPE)
        {
          // sql += $"AND {field.FIELDNAME}='{row.GetOldString(field.FIELDNAME).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")}' ";
        }
        else if ("date" == field.FIELDTYPE)
        {
          var oldVal = row.GetOldValue(field.FIELDNAME);
          string oldStr = oldVal is DateTime ? ((DateTime)oldVal).ToString("yyyy-MM-dd") : oldVal + "";
          sql += $"AND {field.FIELDNAME}= str_to_date('{oldStr}','%Y-%m-%d') ";
        }
        else if ("datetime" == field.FIELDTYPE)
        {
          var oldVal = row.GetOldValue(field.FIELDNAME);
          string oldStr = oldVal is DateTime ? ((DateTime)oldVal).ToString("yyyy-MM-dd HH:mm:ss") : oldVal + "";
          sql += $"AND {field.FIELDNAME}= str_to_date('{oldStr}','%Y-%m-%d %H:%i:%s') ";
        }
        else
        {
          var oldVal = row.GetOldString(field.FIELDNAME);
          if (string.IsNullOrWhiteSpace(oldVal) || oldVal == "null")
            sql += $"AND ({field.FIELDNAME} IS NULL) ";
          else
            sql += $"AND  {field.FIELDNAME}= {oldVal} ";
        }
      });
      return sql.Substring(3);
    }

    private string joinParamPa(Resource resource, ViewRow row)
    {
      string sql = "";
      List<ResourceField> fields = resource.Fields;
      fields.ForEach((ResourceField field) =>
      {
        if (field.UPFIELDID + "" != "" || row.GetString(field.FIELDNAME) == row.GetOldString(field.FIELDNAME))
          return;
        if (row.GetString(field.FIELDNAME) == "" || row.GetString(field.FIELDNAME) == "null")
        {
          sql += $", {field.FIELDNAME}=NULL ";
        }
        else if ("varchar" == field.FIELDTYPE || "text" == field.FIELDTYPE)
        {
          sql += $", {field.FIELDNAME}=@{field.FIELDNAME}";
        }
        else if ("datetime" == field.FIELDTYPE)
        {
          //TODO:日期处理
          //str_to_date('2008-08-09 08:9:30', '%Y-%m-%d %h:%i:%s');
          sql += $", {field.FIELDNAME}=str_to_date('{row[field.FIELDNAME]}','%Y-%m-%d %H:%i:%s') ";
        }
        else if ("date" == field.FIELDTYPE)
        {
          //TODO:日期处理
          //str_to_date('2008-08-09 08:9:30', '%Y-%m-%d %h:%i:%s');
          sql += $", {field.FIELDNAME}=str_to_date('{row[field.FIELDNAME]}','%Y-%m-%d') ";
        }
        else
        {
          var val = row[field.FIELDNAME] + "";
          if (string.IsNullOrWhiteSpace(val) || val == "null")
            sql += $", {field.FIELDNAME}=NULL ";
          else
            sql += $", {field.FIELDNAME}={row[field.FIELDNAME]} ";
        }
      });
      return sql.Substring(1);
    }

    public string BuildUpdate(DataView view, ViewRow row = null)
    {
      Resource resource = view.Resource;
      string sql = "";
      if (row == null && view.Updated.Count > 1)
      {
        view.Updated.ForEach((ViewRow r) =>
        {
          sql += $" UPDATE {resource.TABLENAME} SET {joinParam(resource, r)}  WHERE {joinWhere(resource, r)} ; ";
        });
      }
      else
      {
        if (row == null)
        {
          row = view.Updated[0];
        }
        sql = $" UPDATE {resource.TABLENAME} SET {joinParamPa(resource, row)}  WHERE {joinWhere(resource, row)} ; ";
      }
      return sql;
    }
    public string BuildDelete(DataView view, ViewRow row = null)
    {
      string sql = "";
      Resource resource = view.Resource;
      if (row == null)
      {
        view.Deleted.ForEach((ViewRow r) =>
        {
          sql += $"DELETE FROM {resource.TABLENAME} WHERE {joinWhere(resource, r)} ; ";
        });
      }
      else
      {
        sql += $"DELETE FROM {resource.TABLENAME} WHERE {joinWhere(resource, row)} ; ";
      }
      return sql;
    }
  }
}
