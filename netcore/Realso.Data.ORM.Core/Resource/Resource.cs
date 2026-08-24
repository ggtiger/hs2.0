using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class Resource
  {
    public string ID { get; set; }
    public string RESOURCENAME { get; set; }
    public string RESOURCEANAME { get; set; }
    public string TABLENAME { get; set; }
    public string RESOURCETYPE { get; set; }
    public string SQLCODE { get; set; }
    public int ISFORBID { get; set; }
    public string ISCREATE { get; set; }
    public string COMMENTS { get; set; }
    public List<ResourceField> Fields { get; set; }
    public List<ResourceFilter> Filters { get; set; }
    /// <summary>
    /// resuipc 全字段配置（FIELDNAME → UisetField），GetResource 时一次性加载，@ui 过滤器使用
    /// </summary>
    public Dictionary<string, UisetField> UisetFields { get; set; }
  }
}
