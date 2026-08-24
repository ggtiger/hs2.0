using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class ViewColumnValue
  {

    public ViewColumnValue(ViewRow Row, ViewColumn Column)
    {
      this.Row = Row;
      this.Column = Column;
    }

    public ViewRow Row { get; set; }

    public ViewColumn Column;

    private object _value { get; set; }
    public object Value
    {
      get
      {
        return this._value;
      }
      set
      {
        if (Column.Type != "varchar" && Column.Type != "text")
        {
          if ((value is string && value + "" == "") || value + "" == "null")
          {
            value = null;
          }
        }
        this._value = value;
        if (this.Row.Status == ViewRowStatus.Filling)
        {
          this.OldValue = value;
        }
      }
    }

    public object OldValue { get; set; }

    public bool IsChange()
    {
      return (this.Row.Status == ViewRowStatus.Filled) && ((this.OldValue + string.Empty) != (this.Value + string.Empty));
    }

    public new string ToString()
    {
      return this.Value + "";
    }
  }
}
