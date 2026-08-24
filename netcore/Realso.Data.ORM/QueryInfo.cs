/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/
using System.Collections;

namespace Realso.Data.ORM
{
  /// <summary>
  /// 查询实体
  /// </summary>
  public class QueryInfo
  {

    public QueryInfo()
    {
      FilterCode = "00";
      FilterParams = new Hashtable();
      PageSize = 1;
      PageIndex = 1;
      OrderBy = "";
      OtherWhere = "";
      SumFields = "";
    }

    public string FilterCode { get; set; }

    public Hashtable FilterParams { get; set; }

    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public string SQL { get; set; }

    public string OrderBy { get; set; }
    public string OtherWhere { get; set; }
    public string SumFields { get; set; }
  }
}
