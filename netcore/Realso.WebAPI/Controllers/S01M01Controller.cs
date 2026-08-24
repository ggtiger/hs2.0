using System.Reflection.Emit;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.ORM;
using Realso.WebAPI.Models;
using System.Web.Http;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Newtonsoft.Json;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class S01M01Controller : BaseControl
  {
    //打开数据
    //参数：模块ID、接口编码、参数
    [HttpPost("open/{modulename}/{apicode}")]
    [EnableCors("AllowHeaders")]
    public IActionResult Call(string ModuleName, string ApiCode, [FromForm] string Api, [FromForm] Hashtable Params)
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

      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      if ("query" == APITYPE)
      {
        Params["FILTERCODE"] = row["FILTERCODE"];
        string RESOURCEID = MAINRESOURCEID;
        if (MAINPATH != "")
        {
          ViewRow pathRow = MD.GetPath(MAINPATH);
          RESOURCEID = pathRow.GetString("RESOURCEID");
        }
        BaseModel MAIN = GetModel(MAINPATH, RESOURCEID);
        responseModel.SetData(MAIN.Query(GetQueryInfo(Params)));
        return this.doResponse();
      }
      if ("open" == APITYPE)
      {
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
          tqueryInfo.OtherWhere = $"{ MAIN.GetView().Resource.RESOURCEANAME}.{trow.GetString("RFIELDSB")}=@{trow.GetString("RFIELDSA")}";
          tqueryInfo.FilterParams[$"{trow.GetString("RFIELDSA")}"] = MAIN.GetValue(trow.GetString("RFIELDSA"));
          if (DTS.HasColumn("ENTRYNUM"))
          {
            tqueryInfo.OrderBy = "ENTRYNUM";
          }
          DTS.Open(tqueryInfo);
          ht[tpath] = DTS.GetView();
        }
        responseModel.SetData(ht);
        return this.doResponse();
      }

      if ("getid" == APITYPE)
      {
        int CNT = int.Parse(Params["CNT"] + "");
        string RESOURCEID = Params["RESOURCEID"] + "";
        Resource tresource = null;
        if (RESOURCEID != "")
        {
          operate01.GetResource(RESOURCEID);
        }
        responseModel.SetData(operate01.GetNewID(tresource, CNT));
        return this.doResponse();
      }

      if ("save" == APITYPE)
      {
        ArrayList saveList = new ArrayList();
        IDictionary<string, DataView> viewList = new Dictionary<string, DataView>();
        foreach (DictionaryEntry d in Params)
        {
          ViewRow pathRow = MD.GetPath(d.Key + "");
          BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
          view.InitData(d.Value + "");
          view.FillKey();
          //view.FillEntryNum();
          saveList.Add(view.GetView());
          viewList.Add(d.Key + "", view.GetView());
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
            var v = viewList[PATHA][0][FIELDA];
            viewList[PATHB].ForEach((ViewRow tr) =>
            {
              tr[FIELDB] = v;
            });
          }
        }
        operate01.Save(saveList);
        // 同步物理表字段：新增字段自动ALTER TABLE ADD COLUMN
        SyncTableColumns(viewList);
        responseModel.SetData(viewList);
        return this.doResponse();
      }

      if ("delete" == APITYPE)
      {
        ArrayList saveList = new ArrayList();
        foreach (DictionaryEntry d in Params)
        {
          ViewRow pathRow = MD.GetPath(d.Key + "");
          BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
          view.InitData(d.Value + "");
          saveList.Add(view.GetView());
        }
        operate01.Save(saveList);
        responseModel.SetData(saveList);
        return this.doResponse();
      }
      return new JsonResult(row);
    }

    /// <summary>
    /// 保存TABLE类型资源时，同步新增字段到物理表
    /// </summary>
    private void SyncTableColumns(IDictionary<string, DataView> viewList)
    {
      // 检查是否为 TABLE 类型资源
      if (!viewList.ContainsKey("MAIN")) return;
      DataView mainView = viewList["MAIN"];
      if (mainView.Count == 0) return;

      string resourceType = mainView[0].GetString("RESOURCETYPE");
      string tableName = mainView[0].GetString("TABLENAME");
      if (resourceType != "TABLE" || string.IsNullOrEmpty(tableName)) return;

      // 检查是否有新增字段
      if (!viewList.ContainsKey("DTSA")) return;
      DataView fieldView = viewList["DTSA"];
      if (fieldView.Inserted.Count == 0) return;

      // 查询物理表已有字段
      List<string> existingColumns;
      DBHelper syncHelper = DB.GetDBHelper();
      using (syncHelper)
      {
        existingColumns = syncHelper.Query<string>(
          "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
          new { TABLENAME = tableName }
        ).ToList();
      }

      // 遍历新增字段，同步到物理表
      foreach (ViewRow row in fieldView.Inserted)
      {
        string fieldName = row.GetString("FIELDNAME");
        string upFieldId = row.GetString("UPFIELDID");
        string isVirtual = row.GetString("ISVIRTUAL");

        // 跳过子引用字段和虚拟字段
        if (!string.IsNullOrEmpty(upFieldId) || isVirtual == "1") continue;
        // 跳过已存在的字段
        if (existingColumns.Contains(fieldName)) continue;

        string fieldType = row.GetString("FIELDTYPE");
        int fieldLength = int.Parse(row.GetString("FIELDLENGTH") == "" ? "0" : row.GetString("FIELDLENGTH"));
        int prec = int.Parse(row.GetString("PREC") == "" ? "0" : row.GetString("PREC"));
        int nullable = int.Parse(row.GetString("NULLABLE") == "" ? "1" : row.GetString("NULLABLE"));
        string defaultValue = row.GetString("DEFAULTVALUE");
        string comments = row.GetString("COMMENTS");

        // 构建列定义
        string columnDef = GetColumnDefinition(fieldType, fieldLength, prec);
        string nullDef = nullable == 0 ? "NOT NULL" : "DEFAULT NULL";
        string defaultDef = !string.IsNullOrEmpty(defaultValue) ? $"DEFAULT '{defaultValue}'" : "";
        string commentDef = !string.IsNullOrEmpty(comments) ? $"COMMENT '{comments.Replace("'", "\\'")}'" : "";

        string alterSQL = $"ALTER TABLE `{tableName}` ADD COLUMN `{fieldName}` {columnDef} {nullDef} {defaultDef} {commentDef}";
        try
        {
          Realso.Utils.Logger.Info("SyncTableColumn:" + alterSQL);
          DBHelper alterHelper = DB.GetDBHelper();
          using (alterHelper)
          {
            alterHelper.Execute(alterSQL);
          }
        }
        catch (Exception ex)
        {
          Realso.Utils.Logger.Error("SyncTableColumn Error:" + ex.Message, ex);
        }
      }
    }

    /// <summary>
    /// 根据FIELDTYPE映射MySQL列类型定义
    /// </summary>
    private string GetColumnDefinition(string fieldType, int fieldLength, int prec)
    {
      switch (fieldType)
      {
        case "varchar": return fieldLength > 0 ? $"varchar({fieldLength})" : "varchar(255)";
        case "text": return "text";
        case "int": return "int";
        case "bigint": return "bigint";
        case "float": return "float";
        case "decimal": return prec > 0 ? $"decimal({fieldLength},{prec})" : "decimal(18,2)";
        case "datetime": return "datetime";
        case "date": return "date";
        case "tinyint": return "tinyint";
        default: return fieldLength > 0 ? $"varchar({fieldLength})" : "varchar(255)";
      }
    }

    /// <summary>
    /// 对比物理表结构与元数据字段差异
    /// </summary>
    [HttpPost("compare")]
    [EnableCors("AllowHeaders")]
    public IActionResult Compare([FromForm] Hashtable Params)
    {
      try
      {
        string TABLENAME = Params["TABLENAME"] + "";
        string RESOURCEID = Params["RESOURCEID"] + "";
        if (string.IsNullOrEmpty(TABLENAME) || string.IsNullOrEmpty(RESOURCEID))
        {
          responseModel.SetError("TABLENAME和RESOURCEID不能为空");
          return this.doResponse();
        }

        bool tableExists = false;
        List<Dictionary<string, object>> physicalColumns = new List<Dictionary<string, object>>();
        List<Dictionary<string, object>> metaFields = new List<Dictionary<string, object>>();

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 1. 检查物理表是否存在
          var tableResult = helper.QueryFirstOrDefault(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
            new { TABLENAME });
          tableExists = tableResult != null;

          // 2. 获取物理表列信息
          if (tableExists)
          {
            var columns = helper.Query(
              @"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE,
                       IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME
                ORDER BY ORDINAL_POSITION",
              new { TABLENAME }).ToList();

            foreach (var col in columns)
            {
              var dict = new Dictionary<string, object>();
              dict["COLUMN_NAME"] = col.COLUMN_NAME;
              dict["DATA_TYPE"] = col.DATA_TYPE;
              dict["CHARACTER_MAXIMUM_LENGTH"] = col.CHARACTER_MAXIMUM_LENGTH;
              dict["NUMERIC_PRECISION"] = col.NUMERIC_PRECISION;
              dict["NUMERIC_SCALE"] = col.NUMERIC_SCALE;
              dict["IS_NULLABLE"] = col.IS_NULLABLE;
              dict["COLUMN_DEFAULT"] = col.COLUMN_DEFAULT;
              dict["COLUMN_COMMENT"] = col.COLUMN_COMMENT;
              physicalColumns.Add(dict);
            }
          }

          // 3. 获取元数据字段（跳过子引用和虚拟字段）
          var fields = helper.Query(
            @"SELECT FIELDNAME, FIELDTYPE, FIELDLENGTH, PREC, NULLABLE, DEFAULTVALUE, COMMENTS, ISKEY, KEYGENTYPE
              FROM TSS_RESFIELD
              WHERE RESOURCEID=@RESOURCEID AND (UPFIELDID IS NULL OR UPFIELDID='') AND (ISVIRTUAL IS NULL OR ISVIRTUAL!='1')
              ORDER BY ENTRYNUM",
            new { RESOURCEID }).ToList();

          foreach (var f in fields)
          {
            var dict = new Dictionary<string, object>();
            dict["FIELDNAME"] = f.FIELDNAME;
            dict["FIELDTYPE"] = f.FIELDTYPE ?? "";
            dict["FIELDLENGTH"] = f.FIELDLENGTH ?? "0";
            dict["PREC"] = f.PREC ?? "0";
            dict["NULLABLE"] = f.NULLABLE ?? "1";
            dict["DEFAULTVALUE"] = f.DEFAULTVALUE ?? "";
            dict["COMMENTS"] = f.COMMENTS ?? "";
            dict["ISKEY"] = f.ISKEY ?? "0";
            metaFields.Add(dict);
          }
        }

        // 4. 合并对比
        var physicalNames = new HashSet<string>(physicalColumns.Select(c => c["COLUMN_NAME"].ToString()));
        var metaNames = new HashSet<string>(metaFields.Select(f => f["FIELDNAME"].ToString()));
        var allNames = physicalNames.Union(metaNames).ToList();

        var result = new List<Dictionary<string, object>>();
        foreach (var name in allNames)
        {
          var item = new Dictionary<string, object>();
          item["FIELDNAME"] = name;
          bool inPhysical = physicalNames.Contains(name);
          bool inMeta = metaNames.Contains(name);
          item["IN_PHYSICAL"] = inPhysical;
          item["IN_META"] = inMeta;

          if (inMeta)
          {
            var metaField = metaFields.First(f => f["FIELDNAME"].ToString() == name);
            item["META_FIELDTYPE"] = metaField["FIELDTYPE"];
            item["META_FIELDLENGTH"] = metaField["FIELDLENGTH"];
            item["META_PREC"] = metaField["PREC"];
            item["META_NULLABLE"] = metaField["NULLABLE"];
            item["META_DEFAULTVALUE"] = metaField["DEFAULTVALUE"];
            item["META_COMMENTS"] = metaField["COMMENTS"];
            item["META_ISKEY"] = metaField["ISKEY"];
          }
          else
          {
            item["META_FIELDTYPE"] = "";
            item["META_FIELDLENGTH"] = "0";
            item["META_PREC"] = "0";
            item["META_NULLABLE"] = "1";
            item["META_DEFAULTVALUE"] = "";
            item["META_COMMENTS"] = "";
            item["META_ISKEY"] = "0";
          }

          if (inPhysical)
          {
            var physCol = physicalColumns.First(c => c["COLUMN_NAME"].ToString() == name);
            item["PHYSICAL_DATA_TYPE"] = physCol["DATA_TYPE"];
            item["PHYSICAL_MAX_LENGTH"] = physCol["CHARACTER_MAXIMUM_LENGTH"];
            item["PHYSICAL_PRECISION"] = physCol["NUMERIC_PRECISION"];
            item["PHYSICAL_SCALE"] = physCol["NUMERIC_SCALE"];
            item["PHYSICAL_IS_NULLABLE"] = physCol["IS_NULLABLE"];
            item["PHYSICAL_DEFAULT"] = physCol["COLUMN_DEFAULT"];
            item["PHYSICAL_COMMENT"] = physCol["COLUMN_COMMENT"];
          }
          else
          {
            item["PHYSICAL_DATA_TYPE"] = "";
            item["PHYSICAL_MAX_LENGTH"] = null;
            item["PHYSICAL_PRECISION"] = null;
            item["PHYSICAL_SCALE"] = null;
            item["PHYSICAL_IS_NULLABLE"] = "";
            item["PHYSICAL_DEFAULT"] = null;
            item["PHYSICAL_COMMENT"] = "";
          }

          if (inPhysical && inMeta) item["STATUS"] = "matched";
          else if (inMeta && !inPhysical) item["STATUS"] = "meta_only";
          else item["STATUS"] = "physical_only";

          result.Add(item);
        }

        responseModel.SetData(new
        {
          tableExists,
          columns = result
        });
      }
      catch (Exception ex)
      {
        Realso.Utils.Logger.Error("Compare Error:" + ex.Message, ex);
        responseModel.SetError("对比失败：" + ex.Message);
      }
      return this.doResponse();
    }

    /// <summary>
    /// 同步元数据字段到物理表（CREATE TABLE / ALTER TABLE ADD COLUMN）
    /// </summary>
    [HttpPost("sync")]
    [EnableCors("AllowHeaders")]
    public IActionResult Sync([FromForm] Hashtable Params)
    {
      try
      {
        string TABLENAME = Params["TABLENAME"] + "";
        string FIELDS = Params["FIELDS"] + "";
        if (string.IsNullOrEmpty(TABLENAME))
        {
          responseModel.SetError("TABLENAME不能为空");
          return this.doResponse();
        }

        var fieldList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(FIELDS);
        if (fieldList == null || fieldList.Count == 0)
        {
          responseModel.SetError("同步字段列表不能为空");
          return this.doResponse();
        }

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 1. 检查物理表是否存在
          var tableResult = helper.QueryFirstOrDefault(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
            new { TABLENAME });
          bool tableExists = tableResult != null;

          if (!tableExists)
          {
            // 2. 表不存在，CREATE TABLE
            var columnDefs = new List<string>();
            var pkFields = new List<string>();

            foreach (var field in fieldList)
            {
              string fieldName = field.ContainsKey("FIELDNAME") ? field["FIELDNAME"] : "";
              if (string.IsNullOrEmpty(fieldName)) continue;

              string fieldType = field.ContainsKey("FIELDTYPE") ? field["FIELDTYPE"] : "varchar";
              int fieldLength = 0;
              int.TryParse(field.ContainsKey("FIELDLENGTH") ? field["FIELDLENGTH"] : "0", out fieldLength);
              int prec = 0;
              int.TryParse(field.ContainsKey("PREC") ? field["PREC"] : "0", out prec);
              int nullable = 1;
              int.TryParse(field.ContainsKey("NULLABLE") ? field["NULLABLE"] : "1", out nullable);
              string defaultValue = field.ContainsKey("DEFAULTVALUE") ? field["DEFAULTVALUE"] : "";
              string comments = field.ContainsKey("COMMENTS") ? field["COMMENTS"] : "";
              string isKey = field.ContainsKey("ISKEY") ? field["ISKEY"] : "0";

              string columnDef = GetColumnDefinition(fieldType, fieldLength, prec);
              string nullDef = nullable == 0 ? "NOT NULL" : "DEFAULT NULL";
              string defaultDef = !string.IsNullOrEmpty(defaultValue) ? $"DEFAULT '{defaultValue.Replace("'", "\\'")}'" : "";
              string commentDef = !string.IsNullOrEmpty(comments) ? $"COMMENT '{comments.Replace("'", "\\'")}'" : "";

              // NOT NULL 和 DEFAULT 不能同时出现（DEFAULT NULL 除外）
              string colSQL = $"`{fieldName}` {columnDef}";
              if (nullable == 0 && !string.IsNullOrEmpty(defaultValue))
              {
                colSQL += $" NOT NULL {defaultDef}";
              }
              else if (nullable == 0)
              {
                colSQL += " NOT NULL";
              }
              else if (!string.IsNullOrEmpty(defaultValue))
              {
                colSQL += $" {defaultDef}";
              }
              else
              {
                colSQL += " DEFAULT NULL";
              }
              colSQL += $" {commentDef}";
              columnDefs.Add(colSQL);

              if (isKey == "1") pkFields.Add($"`{fieldName}`");
            }

            // 添加 is_deleted 标准字段
            columnDefs.Add("`is_deleted` tinyint NOT NULL DEFAULT 0 COMMENT '逻辑删除'");

            string pkSQL = pkFields.Count > 0 ? $", PRIMARY KEY ({string.Join(", ", pkFields)})" : "";
            string createSQL = $"CREATE TABLE `{TABLENAME}` ({string.Join(", ", columnDefs)}{pkSQL}) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";

            Realso.Utils.Logger.Info("Sync CreateTable:" + createSQL);
            helper.Execute(createSQL);

            responseModel.SetData(new { action = "create_table", tableName = TABLENAME });
          }
          else
          {
            // 3. 表已存在，ALTER TABLE ADD COLUMN
            var existingColumns = helper.Query<string>(
              "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
              new { TABLENAME }).ToList();

            var existingSet = new HashSet<string>(existingColumns);
            int addedCount = 0;

            foreach (var field in fieldList)
            {
              string fieldName = field.ContainsKey("FIELDNAME") ? field["FIELDNAME"] : "";
              if (string.IsNullOrEmpty(fieldName)) continue;
              if (existingSet.Contains(fieldName)) continue;

              string fieldType = field.ContainsKey("FIELDTYPE") ? field["FIELDTYPE"] : "varchar";
              int fieldLength = 0;
              int.TryParse(field.ContainsKey("FIELDLENGTH") ? field["FIELDLENGTH"] : "0", out fieldLength);
              int prec = 0;
              int.TryParse(field.ContainsKey("PREC") ? field["PREC"] : "0", out prec);
              int nullable = 1;
              int.TryParse(field.ContainsKey("NULLABLE") ? field["NULLABLE"] : "1", out nullable);
              string defaultValue = field.ContainsKey("DEFAULTVALUE") ? field["DEFAULTVALUE"] : "";
              string comments = field.ContainsKey("COMMENTS") ? field["COMMENTS"] : "";

              string columnDef = GetColumnDefinition(fieldType, fieldLength, prec);
              string nullDef = nullable == 0 ? "NOT NULL" : "DEFAULT NULL";
              string defaultDef = !string.IsNullOrEmpty(defaultValue) ? $"DEFAULT '{defaultValue.Replace("'", "\\'")}'" : "";
              string commentDef = !string.IsNullOrEmpty(comments) ? $"COMMENT '{comments.Replace("'", "\\'")}'" : "";

              string colSQL;
              if (nullable == 0 && !string.IsNullOrEmpty(defaultValue))
              {
                colSQL = $"ALTER TABLE `{TABLENAME}` ADD COLUMN `{fieldName}` {columnDef} NOT NULL {defaultDef} {commentDef}";
              }
              else if (nullable == 0)
              {
                colSQL = $"ALTER TABLE `{TABLENAME}` ADD COLUMN `{fieldName}` {columnDef} NOT NULL {commentDef}";
              }
              else if (!string.IsNullOrEmpty(defaultValue))
              {
                colSQL = $"ALTER TABLE `{TABLENAME}` ADD COLUMN `{fieldName}` {columnDef} {defaultDef} {commentDef}";
              }
              else
              {
                colSQL = $"ALTER TABLE `{TABLENAME}` ADD COLUMN `{fieldName}` {columnDef} DEFAULT NULL {commentDef}";
              }

              Realso.Utils.Logger.Info("Sync AddColumn:" + colSQL);
              helper.Execute(colSQL);
              addedCount++;
            }

            responseModel.SetData(new { action = "add_columns", tableName = TABLENAME, addedCount });
          }
        }
      }
      catch (Exception ex)
      {
        Realso.Utils.Logger.Error("Sync Error:" + ex.Message, ex);
        responseModel.SetError("同步失败：" + ex.Message);
      }
      return this.doResponse();
    }

    /// <summary>
    /// 刷新元数据：根据物理表列更新tss_resfield（新增缺失字段、更新已有字段类型）
    /// </summary>
    [HttpPost("refresh")]
    [EnableCors("AllowHeaders")]
    public IActionResult Refresh([FromForm] Hashtable Params)
    {
      try
      {
        string TABLENAME = Params["TABLENAME"] + "";
        string RESOURCEID = Params["RESOURCEID"] + "";
        if (string.IsNullOrEmpty(TABLENAME) || string.IsNullOrEmpty(RESOURCEID))
        {
          responseModel.SetError("TABLENAME和RESOURCEID不能为空");
          return this.doResponse();
        }

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 1. 获取物理表列信息
          var columns = helper.Query(
            @"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE,
                     IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT, COLUMN_KEY
              FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME
              ORDER BY ORDINAL_POSITION",
            new { TABLENAME }).ToList();

          if (columns.Count == 0)
          {
            responseModel.SetError("物理表不存在或没有字段");
            return this.doResponse();
          }

          // 2. 获取当前元数据字段
          var metaFields = helper.Query(
            "SELECT FIELDNAME, FIELDTYPE, FIELDLENGTH, PREC, NULLABLE, DEFAULTVALUE, COMMENTS, ISKEY FROM TSS_RESFIELD WHERE RESOURCEID=@RESOURCEID AND (UPFIELDID IS NULL OR UPFIELDID='')",
            new { RESOURCEID }).ToList();
          var metaNames = new HashSet<string>(metaFields.Select(f => (string)(f.FIELDNAME + "").ToUpper()));

          int addedCount = 0;
          int updatedCount = 0;
          int entryNum = 0;

          foreach (var col in columns)
          {
            entryNum++;
            string colName = col.COLUMN_NAME + "";
            string dataType = col.DATA_TYPE + "";
            long? maxLength = col.CHARACTER_MAXIMUM_LENGTH;
            long? numericPrecision = col.NUMERIC_PRECISION;
            long? numericScale = col.NUMERIC_SCALE;
            string isNullable = col.IS_NULLABLE + "";
            string columnDefault = col.COLUMN_DEFAULT + "";
            string columnComment = col.COLUMN_COMMENT + "";
            string columnKey = col.COLUMN_KEY + "";

            // 映射 MySQL DATA_TYPE -> FIELDTYPE
            string fieldType = MapMySqlTypeToFieldType(dataType);
            int fieldLength = 0;
            int prec = 0;
            if (dataType == "varchar" || dataType == "char")
              fieldLength = maxLength.HasValue ? (int)maxLength.Value : 255;
            else if (dataType == "decimal" || dataType == "numeric")
            {
              fieldLength = numericPrecision.HasValue ? (int)numericPrecision.Value : 18;
              prec = numericScale.HasValue ? (int)numericScale.Value : 2;
            }
            int nullable = isNullable == "YES" ? 1 : 0;
            int isKey = columnKey == "PRI" ? 1 : 0;

            if (metaNames.Contains(colName.ToUpper()))
            {
              // 已存在：更新类型/长度/精度等属性
              string setSql = "UPDATE TSS_RESFIELD SET FIELDTYPE=@FIELDTYPE, FIELDLENGTH=@FIELDLENGTH, PREC=@PREC, NULLABLE=@NULLABLE, ISKEY=@ISKEY";
              if (!string.IsNullOrEmpty(columnComment))
                setSql += ", COMMENTS=@COMMENTS";
              if (!string.IsNullOrEmpty(columnDefault))
                setSql += ", DEFAULTVALUE=@DEFAULTVALUE";
              setSql += " WHERE RESOURCEID=@RESOURCEID AND FIELDNAME=@FIELDNAME AND (UPFIELDID IS NULL OR UPFIELDID='')";

              helper.Execute(setSql, new
              {
                FIELDTYPE = fieldType,
                FIELDLENGTH = fieldLength,
                PREC = prec,
                NULLABLE = nullable,
                ISKEY = isKey,
                COMMENTS = string.IsNullOrEmpty(columnComment) ? null : columnComment,
                DEFAULTVALUE = string.IsNullOrEmpty(columnDefault) ? null : columnDefault,
                RESOURCEID,
                FIELDNAME = colName
              });
              updatedCount++;
            }
            else
            {
              // 不存在：新增
              string newId = helper.QueryFirstOrDefault<string>("SELECT REPLACE(UUID(),'-','')");
              string insertSql = @"INSERT INTO TSS_RESFIELD (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, PREC, NULLABLE, DEFAULTVALUE, COMMENTS, ISKEY, KEYGENTYPE, ISVO, ISDO, ENTRYNUM, ISVIRTUAL)
                                   VALUES (@ID, @RESOURCEID, @FIELDNAME, @FIELDTYPE, @FIELDLENGTH, @PREC, @NULLABLE, @DEFAULTVALUE, @COMMENTS, @ISKEY, @KEYGENTYPE, @ISVO, @ISDO, @ENTRYNUM, 0)";
              helper.Execute(insertSql, new
              {
                ID = newId,
                RESOURCEID,
                FIELDNAME = colName,
                FIELDTYPE = fieldType,
                FIELDLENGTH = fieldLength,
                PREC = prec,
                NULLABLE = nullable,
                DEFAULTVALUE = string.IsNullOrEmpty(columnDefault) ? "" : columnDefault,
                COMMENTS = string.IsNullOrEmpty(columnComment) ? colName : columnComment,
                ISKEY = isKey,
                KEYGENTYPE = isKey == 1 ? "GUID" : "",
                ISVO = 1,
                ISDO = 1,
                ENTRYNUM = entryNum
              });
              addedCount++;
            }
          }

          responseModel.SetData(new { addedCount, updatedCount, tableName = TABLENAME });
        }
      }
      catch (Exception ex)
      {
        Realso.Utils.Logger.Error("Refresh Error:" + ex.Message, ex);
        responseModel.SetError("刷新失败：" + ex.Message);
      }
      return this.doResponse();
    }

    /// <summary>
    /// MySQL DATA_TYPE 映射到元数据 FIELDTYPE
    /// </summary>
    private string MapMySqlTypeToFieldType(string mySqlType)
    {
      switch (mySqlType)
      {
        case "varchar":
        case "char": return "varchar";
        case "text":
        case "longtext":
        case "mediumtext":
        case "tinytext": return "text";
        case "int":
        case "integer": return "int";
        case "bigint": return "bigint";
        case "tinyint": return "tinyint";
        case "smallint": return "int";
        case "float":
        case "double": return "float";
        case "decimal":
        case "numeric": return "decimal";
        case "datetime":
        case "timestamp": return "datetime";
        case "date": return "date";
        default: return "varchar";
      }
    }

    /// <summary>
    /// 查询数据库中未注册为TABLE资源的物理表
    /// </summary>
    [HttpPost("unregistered")]
    [EnableCors("AllowHeaders")]
    public IActionResult Unregistered([FromForm] Hashtable Params)
    {
      try
      {
        string INPUT = Params["INPUT"] + "";
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 获取所有已注册的TABLE资源对应的物理表名
          var registeredTables = helper.Query<string>(
            "SELECT TABLENAME FROM TSS_RESOURCE WHERE RESOURCETYPE='TABLE' AND TABLENAME IS NOT NULL AND TABLENAME!=''").ToList();
          var registeredSet = new HashSet<string>(registeredTables.Select(t => t.ToUpper()));

          // 获取数据库中的物理表（排除系统表）
          var tables = helper.Query(
            @"SELECT TABLE_NAME, TABLE_COMMENT
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'
              ORDER BY TABLE_NAME",
            new {}).ToList();

          var result = new List<Dictionary<string, object>>();
          foreach (var t in tables)
          {
            string tableName = t.TABLE_NAME + "";
            string tableComment = t.TABLE_COMMENT + "";

            // 排除系统元数据表
            if (tableName.StartsWith("TSS_") || tableName.StartsWith("tss_")) continue;

            // 排除已注册的
            if (registeredSet.Contains(tableName.ToUpper())) continue;

            // 模糊搜索
            if (!string.IsNullOrEmpty(INPUT) && !tableName.ToUpper().Contains(INPUT.ToUpper())) continue;

            // 获取表字段数
            var colCount = helper.QueryFirstOrDefault<int>(
              "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
              new { TABLENAME = tableName });

            result.Add(new Dictionary<string, object>
            {
              { "TABLENAME", tableName },
              { "COMMENTS", tableComment },
              { "COLUMN_COUNT", colCount }
            });
          }

          responseModel.SetData(result);
        }
      }
      catch (Exception ex)
      {
        Realso.Utils.Logger.Error("Unregistered Error:" + ex.Message, ex);
        responseModel.SetError("查询失败：" + ex.Message);
      }
      return this.doResponse();
    }

    /// <summary>
    /// 批量生成TABLE类型资源（含字段定义）
    /// </summary>
    [HttpPost("batchCreate")]
    [EnableCors("AllowHeaders")]
    public IActionResult BatchCreate([FromForm] Hashtable Params)
    {
      try
      {
        string TABLES = Params["TABLES"] + "";
        if (string.IsNullOrEmpty(TABLES))
        {
          responseModel.SetError("TABLES不能为空");
          return this.doResponse();
        }

        var tableList = JsonConvert.DeserializeObject<List<string>>(TABLES);
        if (tableList == null || tableList.Count == 0)
        {
          responseModel.SetError("请选择要注册的表");
          return this.doResponse();
        }

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          int createdCount = 0;
          foreach (var tableName in tableList)
          {
            // 1. 创建TSS_RESOURCE记录（RESOURCETYPE=TABLE，RESOURCENAME=表名）
            string resourceId = helper.QueryFirstOrDefault<string>("SELECT REPLACE(UUID(),'-','')");

            // 获取表注释
            string tableComment = helper.QueryFirstOrDefault<string>(
              "SELECT TABLE_COMMENT FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME",
              new { TABLENAME = tableName }) ?? tableName;

            helper.Execute(
              @"INSERT INTO TSS_RESOURCE (ID, RESOURCENAME, TABLENAME, RESOURCETYPE, COMMENTS, ISFORBID, ISCREATE)
                VALUES (@ID, @RESOURCENAME, @TABLENAME, 'TABLE', @COMMENTS, 0, 1)",
              new { ID = resourceId, RESOURCENAME = tableName, TABLENAME = tableName, COMMENTS = tableComment });

            // 2. 获取物理表列，创建TSS_RESFIELD记录
            var columns = helper.Query(
              @"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE,
                       IS_NULLABLE, COLUMN_DEFAULT, COLUMN_COMMENT, COLUMN_KEY, ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@TABLENAME
                ORDER BY ORDINAL_POSITION",
              new { TABLENAME = tableName }).ToList();

            foreach (var col in columns)
            {
              string colName = col.COLUMN_NAME + "";
              string dataType = col.DATA_TYPE + "";
              string fieldType = MapMySqlTypeToFieldType(dataType);
              int fieldLength = 0;
              int prec = 0;
              if (dataType == "varchar" || dataType == "char")
                fieldLength = (col.CHARACTER_MAXIMUM_LENGTH ?? 255);
              else if (dataType == "decimal" || dataType == "numeric")
              {
                fieldLength = (col.NUMERIC_PRECISION ?? 18);
                prec = (col.NUMERIC_SCALE ?? 2);
              }
              int nullable = (col.IS_NULLABLE + "") == "YES" ? 1 : 0;
              int isKey = (col.COLUMN_KEY + "") == "PRI" ? 1 : 0;
              string columnComment = col.COLUMN_COMMENT + "";
              string columnDefault = col.COLUMN_DEFAULT + "";
              int entryNum = col.ORDINAL_POSITION;

              string fieldId = helper.QueryFirstOrDefault<string>("SELECT REPLACE(UUID(),'-','')");

              helper.Execute(
                @"INSERT INTO TSS_RESFIELD (ID, RESOURCEID, FIELDNAME, FIELDTYPE, FIELDLENGTH, PREC, NULLABLE,
                    DEFAULTVALUE, COMMENTS, ISKEY, KEYGENTYPE, ISVO, ISDO, ENTRYNUM, ISVIRTUAL)
                  VALUES (@ID, @RESOURCEID, @FIELDNAME, @FIELDTYPE, @FIELDLENGTH, @PREC, @NULLABLE,
                    @DEFAULTVALUE, @COMMENTS, @ISKEY, @KEYGENTYPE, @ISVO, @ISDO, @ENTRYNUM, 0)",
                new
                {
                  ID = fieldId,
                  RESOURCEID = resourceId,
                  FIELDNAME = colName,
                  FIELDTYPE = fieldType,
                  FIELDLENGTH = fieldLength,
                  PREC = prec,
                  NULLABLE = nullable,
                  DEFAULTVALUE = string.IsNullOrEmpty(columnDefault) ? "" : columnDefault,
                  COMMENTS = string.IsNullOrEmpty(columnComment) ? colName : columnComment,
                  ISKEY = isKey,
                  KEYGENTYPE = isKey == 1 ? "GUID" : "",
                  ISVO = 1,
                  ISDO = 1,
                  ENTRYNUM = entryNum
                });
            }

            createdCount++;
          }

          responseModel.SetData(new { createdCount });
        }
      }
      catch (Exception ex)
      {
        Realso.Utils.Logger.Error("BatchCreate Error:" + ex.Message, ex);
        responseModel.SetError("批量创建失败：" + ex.Message);
      }
      return this.doResponse();
    }

  }
}
