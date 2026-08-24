using System.Diagnostics;
using System.Collections;
using System;
using Realso.Data.DBAccess;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Realso.Data.ORM.Core;
using System.Data;

namespace Realso.Data.ORM
{
  public class ViewOperate01 : IViewOperate
  {

    private DBHelper helper = DB.GetDBHelper();
    private IBuildSQL buildSQL = new BuildSQL01();
    public void Open(Realso.Data.ORM.Core.DataView dataView, QueryInfo queryInfo)
    {
      //获取查询SQL
      //分页dataView存储分页意义不大
      QueryResult ret = this.Open(dataView.Resource, queryInfo);
      dataView.FillData(ret.Items);
    }

    public void Opens(IList<QuerysInfo> querysInfos)
    {
      for (int i = 0; i < querysInfos.Count; i++)
      {
        QueryInfo queryInfo = querysInfos[i].QueryInfo;
        Realso.Data.ORM.Core.DataView dataView = querysInfos[i].DataView;
        QueryResult ret = this.Open(dataView.Resource, queryInfo);
        dataView.FillData(ret.Items);
      }
    }

    public QueryResult Open(Resource resource, QueryInfo queryInfo)
    {
      QueryResult ret = new QueryResult();
      string sql = buildSQL.BuildQuery(resource, queryInfo);
      string paramJson = Newtonsoft.Json.JsonConvert.SerializeObject(queryInfo.FilterParams);
      Realso.Utils.Logger.Info("Open:" + sql + " | Params:" + paramJson + " | PageSize:" + queryInfo.PageSize + " | PageIndex:" + queryInfo.PageIndex + " | FilterCode:" + queryInfo.FilterCode);
      var list = new List<dynamic>();
      // 每次查询使用独立连接，避免嵌套DataReader冲突
      DBHelper queryHelper = DB.GetDBHelper();
      using (queryHelper)
      {
        //不支持一条分页
        if (queryInfo.PageSize > 1)
        {
          var multi = queryHelper.QueryMultiple(sql, DBHelper.getParameters(queryInfo.FilterParams));
          list = multi.Read().ToList();
          var clist = multi.Read();
          ret.TotalCount = (clist.ToList()[0] as IDictionary<string, object>)["C"] + "";
          if (queryInfo.SumFields != "")
          {
            string[] fs = queryInfo.SumFields.Split(',');
            for (int i = 0; i < fs.Length; i++)
            {
              ret.SumInfo[fs[i]] = (clist.ToList()[0] as IDictionary<string, object>)[fs[i]] + "";
            }
          }
        }
        else
        {
          Realso.Utils.Logger.Info("Open:走 Query<dynamic> 分支 (PageSize<=1)");
          list = queryHelper.Query<dynamic>(sql, DBHelper.getParameters(queryInfo.FilterParams)).ToList();
          Realso.Utils.Logger.Info("Open:查询返回记录数=" + list.Count);
        }
      }
      ret.Items = list;
      return ret;
    }

    public void Save(ArrayList saveList)
    {
      using (helper.Connection)
      {
        helper.Connection.Open();
        IDbTransaction trans = helper.BeginTransaction();
        try
        {
          foreach (var v in saveList)
          {
            if (v is Realso.Data.ORM.Core.DataView)
            {
              Realso.Data.ORM.Core.DataView view = v as Realso.Data.ORM.Core.DataView;
              if (view.Inserted.Count > 0)
              {
                string insertSQL = buildSQL.BuildBatchInsert(view);
                Realso.Utils.Logger.Info("insertSQL:" + insertSQL);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                helper.Execute(insertSQL,null,trans);
                stopwatch.Stop();
                Realso.Utils.Logger.Info($"数据插入临时表end：{stopwatch.ElapsedMilliseconds}");
              }
              if (view.Updated.Count > 0)
              {
                string updateSQL = buildSQL.BuildUpdate(view, null);
                Realso.Utils.Logger.Info("updateSQL:" + updateSQL);
                helper.Execute(updateSQL, view.Updated[0], trans);
              }
              if (view.Deleted.Count > 0)
              {
                string deleteSQL = buildSQL.BuildDelete(view, null);
                Realso.Utils.Logger.Info("deleteSQL:" + deleteSQL);
                helper.Execute(deleteSQL, null, trans);
              }
            }
            else if (v is string)
            {
              helper.Execute(v + "");
            }
            else if (v is ExecInfo)
            {
              ExecInfo dv = v as ExecInfo;
              helper.Execute(dv.SQL.Replace("\r\n", " ").Replace('\n', ' ').Replace('\t', ' '), dv.GetParameters(), trans, null, dv.commandType);
              dv.SetParameters();
            }
          }
        }
        catch (Exception ex)
        {
          helper = DB.GetDBHelper();
          throw ex;
        }
        trans.Commit();
        helper.Connection.Close();
      }
      helper = DB.GetDBHelper();
    }

    public Resource GetResource(string resourceName)
    {
      return SchemaManage.GetResource(resourceName);
    }

    public void FillData(Realso.Data.ORM.Core.DataView dataView, string strXML)
    {
      String aaa = "";
      try
      {
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(strXML);
        // 从XML中读取表名，字段名，字段类型，数据库位置
        if (dataView.Resource == null)
        {
          String tableName = xml.FirstChild.NextSibling.Name;
          dataView.Resource = this.GetResource(tableName);
        }
        String colNameStr = xml.FirstChild.NextSibling.Attributes["c"].Value;
        String[] colNames = colNameStr.Split(',');
        String colTypeStr = xml.FirstChild.NextSibling.Attributes["t"].Value;
        String[] colTypes = colTypeStr.Split(',');
        String dbLocation = xml.FirstChild.NextSibling.Attributes["l"].Value;
        for (int k = 0; k < 3; k++)
        {
          List<ViewRow> op = new List<ViewRow>();
          // 遍历所有当前操作行
          for (Int32 i = 0; i < xml.ChildNodes[1].ChildNodes[k].ChildNodes.Count; i++)
          {
            ViewRow ViewRow = dataView.GetAddRow();
            op.Add(ViewRow);
            string mark = "c";
            if (k == 2 || k == 1)
              mark = "oc";
            for (int j = 0; j < colNames.Length; j++)
            {
              String strParam = mark + j.ToString();
              aaa = strParam;
              string strValue = System.Web.HttpUtility.UrlDecode(xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value, System.Text.Encoding.UTF8);
              //处理Null
              if ((xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.ToLower() == "undefined")
                  || (xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.Length == 0)
                  || (xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.ToLower() == "null"))
              {
                strValue = "";
              }
              ViewRow[colNames[j]] = strValue;
            }
            //新增时直接插入新增记录
            if (k == 0)
            {
              dataView.AddRow(ViewRow);
            }
            else//存入表
            {
              ViewRow.Status = ViewRowStatus.Filling;
              dataView.AddRow(ViewRow);
              ViewRow.Status = ViewRowStatus.Filled;
            }
            // 更新，对之前保存的记录进行跟新操作
            if (k == 1)
            {
              for (int j = 0; j < colNames.Length; j++)
              {
                String strParam = "c" + j.ToString();
                string strValue = System.Web.HttpUtility.UrlDecode(xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value, System.Text.Encoding.UTF8);
                string strOld = System.Web.HttpUtility.UrlDecode(xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes["oc" + j].Value, System.Text.Encoding.UTF8);
                //处理Null
                if ((xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.ToLower() == "undefined")
                    || (xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.Length == 0)
                    || (xml.ChildNodes[1].ChildNodes[k].ChildNodes[i].Attributes[strParam].Value.ToLower() == "null"))
                {
                  strValue = "";
                }
                if (strOld != strValue)
                {
                  ViewRow[colNames[j]] = strValue;
                }
              }
            }
          }
          if (k == 2)
          {
            //删除需要删除的记录
            for (int i = 0; i < op.Count; i++)
            {
              dataView.DeleteRow(op[i]);
            }
          }
        }
      }
      catch (System.Exception ex)
      {
        throw new Exception("解析出错");
      }
    }

    public IList<string> GetNewID(Resource resource, int inc = 1)
    {
      IList<string> list = new List<string>();
      for (int i = 0; i < inc; i++)
      {
        list.Add(Guid.NewGuid().ToString().Replace("-", ""));
      }
      return list;
    }

    public void FillKey(Realso.Data.ORM.Core.DataView dataView)
    {
      Realso.Data.ORM.Core.ResourceField field = dataView.Resource.Fields.Find((Realso.Data.ORM.Core.ResourceField f) =>
      {
        return f.ISKEY == "1";
      });
      foreach (var row in dataView)
      {
        if (row.GetString(field.FIELDNAME) == "")
        {
          row[field.FIELDNAME] = Guid.NewGuid().ToString().Replace("-", "");
        }
        // 填充 DEFAULTVALUE：新增行中空值字段使用元数据默认值
        foreach (var f in dataView.Resource.Fields)
        {
          if (!string.IsNullOrEmpty(f.DEFAULTVALUE) && f.ISKEY != "1")
          {
            var val = row[f.FIELDNAME];
            if (val == null || (val is string s && string.IsNullOrEmpty(s)))
            {
              row[f.FIELDNAME] = f.DEFAULTVALUE;
            }
          }
        }
      }
    }

    public IEnumerable<dynamic> Query(string sql, object param = null)
    {
      DBHelper queryHelper = DB.GetDBHelper();
      using (queryHelper)
      {
        return queryHelper.Query(sql, param).ToList();
      }
    }
  }
}
