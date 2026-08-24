using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Realso.Data.ORM.Core;

namespace Realso.Data.ORM
{
  public interface IViewOperate
  {
    void Save(ArrayList saveList);
    void FillData(DataView dataView, string str);
    Resource GetResource(string resourceName);
    void Open(DataView dataView, QueryInfo queryInfo);
    void Opens(IList<QuerysInfo> querysInfos);
    QueryResult Open(Resource resource, QueryInfo queryInfo);
    IList<string> GetNewID(Resource resource, int inc = 0);
    void FillKey(DataView dataView);
    IEnumerable<dynamic> Query(string sql, object param = null);
  }
}
