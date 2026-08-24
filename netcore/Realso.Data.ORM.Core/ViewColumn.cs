using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class ViewColumn : ResourceField
  {
    public ViewColumn(string name, string type)
    {
      this.Name = name;
      this.Type = type;
    }
    public string Name { get; set; }

    public string Type { get; set; }
  }
}
