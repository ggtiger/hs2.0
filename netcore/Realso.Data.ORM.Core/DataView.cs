using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class DataView : List<ViewRow>
  {
    private Resource _resouce;

    public Resource Resource
    {
      get
      {
        return this._resouce;
      }
      set
      {
        this._resouce = value;
        this._resouce.Fields.ForEach((ResourceField filed) =>
        {
          this.Columns.Add(new ViewColumn(filed.FIELDNAME, filed.FIELDTYPE));
        });
      }
    }

    public DataView()
    {
      this.Columns = new List<ViewColumn>();
      this.Inserted = new List<ViewRow>();
      this.Deleted = new List<ViewRow>();
      this.KeyRow = new Dictionary<string, ViewRow>();
    }

    public DataView(Resource resouce)
    {
      this.Columns = new List<ViewColumn>();
      this.Inserted = new List<ViewRow>();
      this.Deleted = new List<ViewRow>();
      this.KeyRow = new Dictionary<string, ViewRow>();
      this.Resource = resouce;
    }

    public List<ViewColumn> Columns { get; set; }

    public List<ViewRow> Inserted { get; set; }

    public List<ViewRow> Updated
    {
      get
      {
        return this.FindAll((ViewRow Row) =>
        {
          return Row.IsChange();
        });
      }
    }

    public List<ViewRow> Deleted { get; set; }

    public Dictionary<string, ViewRow> KeyRow { get; set; }

    public ViewRow GetAddRow()
    {
      return new ViewRow(this);
    }

    public void AddRow(ViewRow Row)
    {
      if (this.Contains(Row))
      {
        throw new Exception("真是伤脑筋，重复插入了！");
      }
      this.Add(Row);
      if (Row.Status != ViewRowStatus.Filling)
      {
        Row.Status = ViewRowStatus.Add;
        this.Inserted.Add(Row);
      }
      else
      {
        //重复赋值不然无法记录历史值
        this.Columns.ForEach((ViewColumn column) =>
        {
          Row[column.Name] = Row[column.Name];
        });
      }
    }

    public void DeleteRow(ViewRow Row)
    {
      this.Remove(Row);
      if (Row.Status != ViewRowStatus.Add)
      {
        this.Deleted.Add(Row);
      }
      else
      {
        this.Inserted.Remove(Row);
      }
    }

    public void FillData(List<dynamic> list, Boolean IsClear = true)
    {
      this.Inserted.Clear();
      this.Deleted.Clear();
      if (IsClear)
        this.Clear();
      list.ForEach((dynamic obj) =>
      {
        ViewRow row = this.GetAddRow();
        row.Status = ViewRowStatus.Filling;
        this.Columns.ForEach((ViewColumn column) =>
        {
          row[column.Name] = (obj as IDictionary<string, object>)[column.Name];
        });
        this.AddRow(row);
        row.Status = ViewRowStatus.Filled;
      });
    }
  }
}
