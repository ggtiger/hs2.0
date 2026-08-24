using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Realso.Core.Base;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.WebAPI.Models;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 从 DataController 抽取的统一数据操作服务。核心查询逻辑(QueryCore)单点实现，
  /// 助理工具与 DataController.doQuery 共用，确保权限/业务规则一致。
  /// operate01 由调用方传入：助理传 new ViewOperate01()，DataController 传 this.operate01（共享连接上下文）。
  /// </summary>
  public class DataCallService
  {
    private readonly IViewOperate _operate01;

    public DataCallService(IViewOperate operate)
    {
      _operate01 = operate;
    }

    /// <summary>
    /// 共享核心：在已构建的 MAIN 上注入数据权限参数(_USERID_/_EMPID_/_DEPTID_)并执行查询。
    /// 助理 Query 与 DataController.doQuery(非导出)均调用此方法。
    /// </summary>
    public QueryResult QueryCore(BaseModel MAIN, string filterCode, Hashtable filterParams, Hashtable userInfo,
      string orderBy, string sumFields, int pageIndex, int pageSize)
    {
      Hashtable fp = filterParams != null ? (Hashtable)filterParams.Clone() : new Hashtable();
      if (userInfo != null)
      {
        fp["_USERID_"] = userInfo["ID"];
        fp["_EMPID_"] = userInfo["EMPID"];
        fp["_DEPTID_"] = userInfo["DEPTID"];
      }
      // 补全 FILTERSQL 的所有 @参数（空串兜底），防 Dapper "Parameter must be defined"
      // AI 调用 query_data 时只传 filter 里的参数，未传的 @参数会导致 Dapper 报错
      // 注意：FILTERCODE 不是全局唯一的（F01/F02 各资源都有），必须加 RESOURCEID 精确定位
      if (!string.IsNullOrEmpty(filterCode))
      {
        try
        {
          string mainResourceId = MAIN.GetView().Resource.ID;
          DBHelper h = DB.GetDBHelper();
          using (h)
          {
            var frow = h.QueryFirstOrDefault<dynamic>(
              "SELECT FILTERSQL, RESOURCEID FROM tss_resfilter WHERE FILTERCODE=@fc AND RESOURCEID=@rid LIMIT 1",
              new { fc = filterCode, rid = mainResourceId });
            if (frow != null && !string.IsNullOrEmpty((string)frow.FILTERSQL))
            {
              string filterSql = (string)frow.FILTERSQL;
              if (filterSql.Contains("@ui"))
              {
                // 用正则匹配 @ui 占位符，提取 adv 模式和目标资源
                var uiMatch = System.Text.RegularExpressions.Regex.Match(filterSql, @"@ui(:adv)?(:[A-Za-z0-9_]+)?");
                bool isAdvMode = uiMatch.Success && uiMatch.Value.Contains(":adv");
                string resourceId = frow.RESOURCEID;
                // 解析 @ui:adv:RESOURCEID 中的目标资源ID
                if (uiMatch.Success)
                {
                  string[] parts = uiMatch.Value.Split(':');
                  if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                  {
                    resourceId = parts[2];
                  }
                }

                if (isAdvMode)
                {
                  // F02 高级查询：为资源的所有字段补全参数
                  // 从 resfield 获取所有字段（不再只取 QUERYSORT>0）
                  var allFields = h.Query<dynamic>(
                    "SELECT FIELDNAME, FIELDTYPE, ISKEY, REFRESOURCEID FROM tss_resfield WHERE RESOURCEID=@rid",
                    new { rid = resourceId });
                  // 从 resuipc 获取 QUERYMODE/QUERYTYPE 配置
                  var uisetRows = h.Query<dynamic>(
                    "SELECT FIELDNAME, QUERYTYPE, EDITTYPE, QUERYMODE FROM tss_resuipc WHERE RESOURCEID=@rid",
                    new { rid = resourceId });
                  // 构建 FIELDNAME → QUERYMODE 映射
                  var uisetMap = new Dictionary<string, string>();
                  foreach (var u in uisetRows)
                  {
                    string fn = (string)u.FIELDNAME;
                    string qm = u.QUERYMODE + "";
                    if (string.IsNullOrEmpty(qm))
                    {
                      string ct = (u.QUERYTYPE ?? u.EDITTYPE ?? "") + "";
                      switch (ct)
                      {
                        case "input": case "text": case "textarea": qm = "like"; break;
                        case "select": case "datepicker": case "autocomplete": case "number": qm = "eq"; break;
                        case "daterange": qm = "range"; break;
                        default: qm = ""; break; // 留空，后面按 FIELDTYPE 推导
                      }
                    }
                    if (!string.IsNullOrEmpty(fn)) uisetMap[fn] = qm;
                  }
                  foreach (var rf in allFields)
                  {
                    string fname = (string)rf.FIELDNAME;
                    string ftype = (rf.FIELDTYPE + "").ToLower();
                    string iskey = rf.ISKEY + "";
                    string refResId = rf.REFRESOURCEID + "";
                    // 推导匹配方式（与 BuildSQL01.DeriveQueryMode 保持一致）
                    string qm;
                    if (uisetMap.TryGetValue(fname, out string uqm) && !string.IsNullOrEmpty(uqm))
                    {
                      qm = uqm;
                    }
                    else if (iskey == "1" || !string.IsNullOrEmpty(refResId))
                    {
                      qm = "eq";
                    }
                    else
                    {
                      // 无 resuipc 配置时所有类型默认 eq（varchar 多数是 ID/CODE 编码）
                      qm = "eq";
                    }
                    if (qm == "range")
                    {
                      if (!fp.ContainsKey(fname + "_start")) fp[fname + "_start"] = "";
                      if (!fp.ContainsKey(fname + "_end")) fp[fname + "_end"] = "";
                    }
                    else
                    {
                      if (!fp.ContainsKey(fname)) fp[fname] = "";
                    }
                  }
                }
                else
                {
                  // F01 模糊搜索：LISTSORT>0 且推导为 like 的字段进 INPUT OR 块，只需补全 INPUT
                  // F01 不生成 eq/in/range 独立条件，无需补全其他参数
                  if (!fp.ContainsKey("INPUT")) fp["INPUT"] = "";
                }
              }
              else
              {
                // 传统模式：正则提取 @参数
                foreach (Match m in Regex.Matches(filterSql, @"@([A-Za-z][A-Za-z0-9_]*)"))
                {
                  string n = m.Groups[1].Value;
                  if (!fp.ContainsKey(n)) fp[n] = "";
                }
              }
            }
          }
        }
        catch { }
      }
      QueryInfo qi = new QueryInfo
      {
        FilterCode = filterCode ?? "",
        FilterParams = fp,
        PageIndex = pageIndex,
        PageSize = pageSize,
        OrderBy = orderBy ?? "",
        SumFields = sumFields ?? ""
      };
      return MAIN.Query(qi);
    }

    /// <summary>
    /// 助理入口：按 moduleCode 打开模块、构建 MAIN、调用 QueryCore。
    /// </summary>
    public QueryResult Query(string moduleCode, Hashtable filterParams, Hashtable userInfo,
      int pageIndex = 1, int pageSize = 20)
    {
      MOUDLE MD = new MOUDLE(_operate01);
      MD.Open(moduleCode);

      // 找 query API：优先 A01，否则按 APITYPE=query
      ViewRow row = MD.GetAPI("A01");
      if (row == null)
      {
        foreach (ViewRow r in MD.API.GetView())
        {
          if (r.GetString("APITYPE") == "query") { row = r; break; }
        }
      }
      if (row == null) throw new System.Exception("模块 " + moduleCode + " 无 query 类型 API");

      string MAINPATH = row.GetString("PATHNAME");
      string RESOURCEID = row.GetString("RESOURCEID");
      if (MAINPATH != "")
      {
        ViewRow pathRow = MD.GetPath(MAINPATH);
        if (pathRow != null) RESOURCEID = pathRow.GetString("RESOURCEID");
      }
      BaseModel MAIN = new BaseModel(_operate01, RESOURCEID);

      return QueryCore(MAIN, row.GetString("FILTERCODE"), filterParams, userInfo, null, null, pageIndex, pageSize);
    }

    /// <summary>
    /// 打开单据详情（主表+子表，镜像 DataController.doOpen）。
    /// </summary>
    public Hashtable Open(string moduleCode, string id, Hashtable userInfo)
    {
      MOUDLE MD = new MOUDLE(_operate01);
      MD.Open(moduleCode);

      ViewRow row = MD.GetAPI("A02");
      if (row == null)
      {
        foreach (ViewRow r in MD.API.GetView())
        {
          if (r.GetString("APITYPE") == "open") { row = r; break; }
        }
      }
      if (row == null) throw new System.Exception("模块 " + moduleCode + " 无 open 类型 API");

      string MAINPATH = row.GetString("PATHNAME");
      string FILTERCODE = row.GetString("FILTERCODE");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = new BaseModel(_operate01, pathRow.GetString("RESOURCEID"));

      Hashtable fp = new Hashtable();
      fp["ID"] = id;
      QueryInfo qi = new QueryInfo { FilterCode = FILTERCODE, FilterParams = fp };
      MAIN.Open(qi);
      Hashtable result = new Hashtable();
      result[MAINPATH] = MAIN.GetView();

      IList<ViewRow> rels = MD.GetPathRel(MAINPATH);
      foreach (ViewRow trow in rels)
      {
        string tpath = trow.GetString("PATHNAMEB");
        BaseModel DTS = new BaseModel(_operate01, MD.GetPath(tpath).GetString("RESOURCEID"));
        QueryInfo tqi = new QueryInfo();
        tqi.OtherWhere = $"{MAIN.GetView().Resource.RESOURCEANAME}.{trow.GetString("RFIELDSB")} IN @{trow.GetString("RFIELDSA")}";
        tqi.FilterParams[trow.GetString("RFIELDSA")] = MAIN.GetValues(trow.GetString("RFIELDSA")).Split(',');
        if (DTS.HasColumn("ENTRYNUM")) tqi.OrderBy = "ENTRYNUM";
        DTS.Open(tqi);
        result[tpath] = DTS.GetView();
      }
      return result;
    }
  }
}
