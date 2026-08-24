/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Realso.Data.ORM
{
  /// <summary>
  /// 查询实体
  /// </summary>
  public class QueryResult
  {
    public QueryResult(string TotalCount, List<dynamic> Items)
    {
      this.TotalCount = TotalCount;
      this.Items = Items;
      this.SumInfo = new Hashtable();
    }

    public QueryResult()
    {
      this.SumInfo = new Hashtable();
    }

    public string TotalCount { get; set; }

    public List<dynamic> Items { get; set; }

    public Hashtable SumInfo { get; set; }

  }
}
