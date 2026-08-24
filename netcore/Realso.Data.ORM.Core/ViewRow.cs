using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class ViewRow : Dictionary<string, object>
  {
    public Dictionary<string, ViewColumnValue> viewColumValues = new Dictionary<string, ViewColumnValue>();
    public ViewRow(DataView View)
    {
      View.Columns.ForEach((ViewColumn column) =>
      {
        ViewColumnValue cValue = new ViewColumnValue(this, column);
        this.Add(column.Name, null);
        viewColumValues.Add(column.Name, cValue);
      });
      this.Status = ViewRowStatus.Null;
    }

    public ViewRowStatus Status { get; set; }

    private ViewColumnValue getViewColumnValue(string columnName)
    {
      ViewColumnValue cValue = viewColumValues[columnName];
      if (cValue == null)
      {
        throw new Exception(columnName + ":不存在！");
      }
      return cValue;
    }

    public new object this[string columnName]
    {
      get
      {
        return getViewColumnValue(columnName).Value;
      }
      set
      {
        if (base.ContainsKey(columnName))
        {
          getViewColumnValue(columnName).Value = value;
          base[columnName] = getViewColumnValue(columnName).Value;
        }
      }
    }

    public object GetValue(string columnName)
    {
      return this[columnName];
    }
    public object GetOldValue(string columnName)
    {
      return getViewColumnValue(columnName).OldValue;
    }

    public string GetString(string columnName)
    {
      return this[columnName] + string.Empty;
    }

    public string GetOldString(string columnName)
    {
      return getViewColumnValue(columnName).OldValue + string.Empty;
    }

    public bool IsChange()
    {
      return this.viewColumValues.Values.ToList().FindAll((ViewColumnValue ColumnValue) => { return ColumnValue.IsChange(); }).Count > 0;
    }
  }
}
