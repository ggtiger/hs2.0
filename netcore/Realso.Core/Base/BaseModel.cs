using System;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;

namespace Realso.Core.Base
{
  public class BaseModel
  {
    //属性
    protected IViewOperate operate;
    //表
    private DataView view;

    public BaseModel(IViewOperate operate, string resouceName)
    {
      this.operate = operate;
      this.view = new DataView(operate.GetResource(resouceName));
    }
    public BaseModel(IViewOperate operate, Resource resouce)
    {
      this.operate = operate;
      this.view = new DataView(resouce);
    }

    public BaseModel(IViewOperate operate, DataView view)
    {
      this.operate = operate;
      this.view = view;
    }
    //方法
    public void InitData(string strXML)
    {
      operate.FillData(view, strXML);
    }
    //取值
    public string GetValue(string fieldName, int rowIndex = 0)
    {
      return this.view[rowIndex].GetString(fieldName);
    }
    //获取历史值
    public string GetOldValue(string fieldName, int rowIndex = 0)
    {
      return this.view[rowIndex].GetOldString(fieldName);
    }
    //设置值
    public void SetValue(string fieldName, object value, int rowIndex = 0)
    {
      this.view[rowIndex][fieldName] = value;
    }
    //设置主键
    public void FillKey()
    {
      operate.FillKey(view);
    }

    public bool HasColumn(string columnName)
    {
      int idx = view.Columns.FindIndex((ViewColumn c) =>
      {
        return c.Name == columnName;
      });
      return idx != -1;
    }

    public void FillEntryNum(string EntryNumField = "ENTRYNUM")
    {
      if (this.HasColumn(EntryNumField))
      {
        for (int i = 0; i < this.view.Count; i++)
        {
          this.SetValue(EntryNumField, i + 1, i);
        }
      }
    }

    public DataView GetView()
    {
      return this.view;
    }
    public void SetValues(string name, string value, bool isnull = false)
    {
      for (int i = 0; i < this.view.Count; i++)
      {
        if (isnull == true)
        {
          if (this.GetValue(name, i) == "")
          {
            this.SetValue(name, value, i);
          }
        }
        else
        {
          this.SetValue(name, value, i);
        }
      }
    }
    public string GetValues(string name)
    {
      var idx = "-99999999";
      for (int i = 0; i < this.view.Count; i++)
      {
        idx += "," + this.GetValue(name, i);
      }
      return idx;
    }

    public void Open(QueryInfo queryInfo)
    {
      operate.Open(view, queryInfo);
    }

    public QueryResult Query(QueryInfo queryInfo)
    {
      return operate.Open(view.Resource, queryInfo);
    }

    public void OpenByID(string ID)
    {
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = "F00";
      queryInfo.FilterParams["ID"] = ID;
      operate.Open(view, queryInfo);
    }

  }
}
