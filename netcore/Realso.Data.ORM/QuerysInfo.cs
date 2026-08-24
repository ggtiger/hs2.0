/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/
using System.Collections;
using Realso.Data.ORM.Core;
namespace Realso.Data.ORM
{
  /// <summary>
  /// 查询实体
  /// </summary>
  public class QuerysInfo
  {

    public QuerysInfo(DataView dataView, QueryInfo queryInfo)
    {
      this.DataView = dataView;
      this.QueryInfo = queryInfo;
    }
    public DataView DataView { get; set; }

    public QueryInfo QueryInfo { get; set; }
  }
}
