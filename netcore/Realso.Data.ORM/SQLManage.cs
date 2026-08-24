using System.Collections;
using System.Net.Http.Headers;
using System;
using Realso.Data.DBAccess;
using System.Collections.Generic;
using System.Linq;
using Realso.Data.ORM.Core;

namespace Realso.Data.ORM
{
  public class SQLManage
  {
    protected static IViewOperate operate01 = new ViewOperate01();
    //获取数据（统一代码资产表优先：tss_sql 已并入 tss_code_asset；历史表兜底）
    public static string GetSQL(string SQLCODE, string SQLTYPE = "mysql")
    {
      using (var helper = DB.GetDBHelper())
      {
        string txt = helper.QueryFirstOrDefault<string>(
          "SELECT SOURCECODE FROM tss_code_asset WHERE ASSETTYPE='sql' AND CODE=@c AND ISDELETED=0 LIMIT 1",
          new { c = SQLCODE });
        if (!string.IsNullOrEmpty(txt)) return txt;
      }
      // 历史 tss_sql 兜底（未迁移的遗留数据）
      DataView view = new DataView(operate01.GetResource("VSS_sQL"));
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterParams["SQLCODE"] = SQLCODE;
      queryInfo.FilterParams["SQLTYPE"] = SQLTYPE;
      queryInfo.FilterCode = "F01";
      operate01.Open(view, queryInfo);
      if (view.Count > 0)
      {
        return view[0].GetString("SQLTXT");
      }
      return "";
    }

    public static string ParseSQL(string SQLTXT, Hashtable param)
    {
      Realso.Utils.VelocityHelper velocity = new Realso.Utils.VelocityHelper();
      string SQL = velocity.ExecuteMergeString(SQLTXT, param);
      return SQL;
    }
  }
}
