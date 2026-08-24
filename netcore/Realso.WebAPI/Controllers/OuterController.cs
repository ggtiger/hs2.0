using System.Reflection.Emit;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.ORM;
using Realso.WebAPI.Models;
using System.Web.Http;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using Microsoft.AspNetCore.Authorization;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Realso.Utils;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  //[ApiController]
  public class OuterController : BaseControl
  {
    //初始化模块
    //参数：模块ID
    [HttpPost("init")]
    public IActionResult InitData()
    {
      return Ok();
    }

    //打开数据
    //参数：模块ID、接口编码、参数
    [HttpPost("call/{modulename}/{apicode}")]
    [EnableCors("AllowHeaders")]
    public IActionResult Call(string ModuleName, string ApiCode, [FromForm] string Api, [FromForm] Hashtable Params)
    {
      try
      {
        // 不需要MOUDLE加载的接口直接路由（支持未登录访问）
        if (ApiCode == "A02" && ModuleName == "LI_ECERT")
        {
          try
          {
            this.doEcertCheckPwd(null, null, Params ?? new Hashtable());
          }
          catch (Exception ex)
          {
            responseModel.SetError("A02异常:" + ex.Message + "|" + ex.StackTrace);
          }
          return this.doResponse();
        }
        if (ApiCode == "A03" && ModuleName == "LI_ECERT")
        {
          try
          {
            this.doEcertViewWithPwd(null, null, Params ?? new Hashtable());
          }
          catch (Exception ex)
          {
            responseModel.SetError("A03异常:" + ex.StackTrace);
          }
          return this.doResponse();
        }

        MOUDLE MD = new MOUDLE(this.operate01);
        MD.Open(ModuleName);
        ViewRow row = MD.GetAPI(ApiCode);
        if (row == null)
        {
          responseModel.SetError("接口编码不存在！");
          return this.doResponse();
        }
        string APITYPE = row.GetString("APITYPE");
        switch (APITYPE)
        {
          case "query":
            this.doQuery(MD, row, Params);
            break;
          case "open":
            this.doOpen(MD, row, Params);
            break;
          case "getBillCode":
            this.doGetBillCode(MD, row, Params);
            break;
          default:
            this.doMyApi(MD, row, APITYPE, Params);
            break;
        }
        return this.doResponse();
      }
      catch (Exception ex)
      {
        responseModel.SetError(ex.Message);
        return this.doResponse();
      }
    }

    protected virtual void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      switch (APITYPE)
      {
        case "ecertCheckPwd":
          this.doEcertCheckPwd(MD, row, Params);
          break;
        case "ecertViewWithPwd":
          this.doEcertViewWithPwd(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    /// <summary>
    /// 检查电子证书是否需要密码（A02）
    /// 使用直接SQL查询，不依赖ORM和userInfo，支持未登录访问
    /// </summary>
    protected virtual void doEcertCheckPwd(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string ID = Params["ID"] + "";
      string CERTNO = Params["CERTNO"] + "";

      if (string.IsNullOrEmpty(ID) && string.IsNullOrEmpty(CERTNO))
      {
        responseModel.SetError("请提供ID或证书编号");
        return;
      }

      try
      {
        var dbHelper = DB.GetDBHelper();
        string sql = "SELECT ID, CERTCODE, ECERTSIGN, ECERTPWD FROM tck_orecord WHERE ECERTSIGN=1";
        object param = null;

        if (!string.IsNullOrEmpty(ID))
        {
          sql += " AND ID=@ID";
          param = new { ID };
        }
        else
        {
          sql += " AND CERTCODE=@CERTCODE";
          param = new { CERTCODE = CERTNO };
        }
        sql += " LIMIT 1";

        var result = dbHelper.QueryFirstOrDefault(sql, param);
        if (result == null)
        {
          responseModel.SetError("证书不存在或尚未电子签发");
          return;
        }

        Hashtable data = new Hashtable();
        data["ID"] = ((IDictionary<string, object>)result)["ID"] + "";
        data["CERTCODE"] = ((IDictionary<string, object>)result)["CERTCODE"] + "";
        string ecertPwd = ((IDictionary<string, object>)result)["ECERTPWD"] + "";
        data["NEED_PWD"] = string.IsNullOrEmpty(ecertPwd) ? 0 : 1;
        responseModel.SetData(data);
      }
      catch (Exception ex)
      {
        Logger.Info($"doEcertCheckPwd 异常: {ex.Message}");
        responseModel.SetError("查询失败");
      }
    }

    /// <summary>
    /// 验证密码后返回证书信息+FILEID+ACCESS_TOKEN（A03）
    /// 使用直接SQL查询，不依赖ORM和userInfo，支持未登录访问
    /// </summary>
    protected virtual void doEcertViewWithPwd(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string ID = Params["ID"] + "";
      string PWD = Params["PWD"] + "";

      if (string.IsNullOrEmpty(ID))
      {
        responseModel.SetError("参数不完整");
        return;
      }

      try
      {
        var dbHelper = DB.GetDBHelper();
        string sql = "SELECT ID, CERTCODE, MNAME, SIZETYPE, OPCODE, CUSTNAME, SIGNDATE, EXPDATE, MANUFACTURER, ECERTSIGN, ECERTPWD, CERTID FROM tck_orecord WHERE ID=@ID AND ECERTSIGN=1 LIMIT 1";
        var result = dbHelper.QueryFirstOrDefault(sql, new { ID });

        if (result == null)
        {
          responseModel.SetError("证书不存在或尚未电子签发");
          return;
        }

        var row2 = (IDictionary<string, object>)result;
        string ecertPwd = row2["ECERTPWD"] + "";

        // 有密码则需验证
        if (!string.IsNullOrEmpty(ecertPwd))
        {
          if (string.IsNullOrEmpty(PWD) || !PasswordHelper.VerifyPassword(PWD, ecertPwd))
          {
            responseModel.SetError("密码错误");
            return;
          }
        }

        // 获取证书PDF文件ID
        string FILEID = row2["CERTID"] + "";
        // 生成临时访问Token
        string accessToken = PasswordHelper.GenerateAccessToken(FILEID);

        Hashtable data = new Hashtable();
        data["ID"] = row2["ID"] + "";
        data["CERTCODE"] = row2["CERTCODE"] + "";
        data["MNAME"] = row2["MNAME"] + "";
        data["SIZETYPE"] = row2["SIZETYPE"] + "";
        data["OPCODE"] = row2["OPCODE"] + "";
        data["CUSTNAME"] = row2["CUSTNAME"] + "";
        data["SIGNDATE"] = row2["SIGNDATE"] + "";
        data["EXPDATE"] = row2["EXPDATE"] + "";
        data["MANUFACTURER"] = row2["MANUFACTURER"] + "";
        data["FILEID"] = FILEID;
        data["ACCESS_TOKEN"] = accessToken;
        responseModel.SetData(data);
      }
      catch (Exception ex)
      {
        Logger.Info($"doEcertViewWithPwd 异常: {ex.Message}");
        responseModel.SetError("查询失败");
      }
    }

    protected virtual void doQuery(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      Params["FILTERCODE"] = row["FILTERCODE"];
      string RESOURCEID = MAINRESOURCEID;
      if (MAINPATH != "")
      {
        ViewRow pathRow = MD.GetPath(MAINPATH);
        RESOURCEID = pathRow.GetString("RESOURCEID");
      }
      BaseModel MAIN = GetModel(MAINPATH, RESOURCEID);

      Hashtable FilterParams = Params["FilterParams"] as Hashtable;
      if (FilterParams != null){
        FilterParams["_USERID_"] = this.userInfo["ID"];
        FilterParams["_EMPID_"] = this.userInfo["EMPID"];
        FilterParams["_DEPTID_"] = this.userInfo["DEPTID"];
      }
      string isExport = Params["isExport"] + "";
      QueryInfo queryInfo = GetQueryInfo(Params);

      if (isExport == "1")
      {
        queryInfo.PageIndex = 1;
        queryInfo.PageSize = 1000;
        QueryResult result = MAIN.Query(queryInfo);
        MAIN.GetView().FillData(result.Items);
        ArrayList columns = Params["columns"] as ArrayList;
        while (MAIN.GetView().Count < int.Parse(result.TotalCount))
        {
          queryInfo.PageIndex++;
          result = MAIN.Query(queryInfo);
          MAIN.GetView().FillData(result.Items, false);
        }
        List<DataView> tables = new List<DataView>();
        List<ArrayList> lcolums = new List<ArrayList>();
        tables.Add(MAIN.GetView());
        lcolums.Add(columns);
        FILE file = new FILE(this.operate01);
        Hashtable saveFile = new Hashtable();
        string expPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string fileName = MD.GetView()[0].GetString("MODULENAME") + "导出" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
        saveFile["UPLOADFILEPATH"] = expPath + fileName;
        saveFile["UPLOADTYPE"] = "导出";
        saveFile["FILENAME"] = fileName;
        ExcelHelper.CreateExcel(expPath + fileName, tables.ToArray(), null, lcolums.ToArray());
        file.SaveFile(saveFile);
        ArrayList saveList = new ArrayList();
        saveList.Add(file.GetView());
        this.operate01.Save(saveList);
        responseModel.SetData(file.GetValue("ID"));
      }
      else
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", new ArrayList(), "");
        responseModel.SetData(MAIN.Query(queryInfo));
      }

    }

    protected virtual void doOpen(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      Params["FILTERCODE"] = row["FILTERCODE"];
      Hashtable ht = new Hashtable();
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      MAIN.Open(GetQueryInfo(Params));
      ht[MAINPATH] = MAIN.GetView();
      IList<ViewRow> list = MD.GetPathRel(MAINPATH);
      foreach (var trow in list)
      {
        String tpath = trow.GetString("PATHNAMEB");
        BaseModel DTS = GetModel(tpath, MD.GetPath(tpath).GetString("RESOURCEID"));
        QueryInfo tqueryInfo = new QueryInfo();
        tqueryInfo.OtherWhere = $"{ MAIN.GetView().Resource.RESOURCEANAME}.{trow.GetString("RFIELDSB")} IN @{trow.GetString("RFIELDSA")}";
        tqueryInfo.FilterParams[$"{trow.GetString("RFIELDSA")}"] = MAIN.GetValues(trow.GetString("RFIELDSA")).Split(',');
        if (DTS.HasColumn("ENTRYNUM"))
        {
          tqueryInfo.OrderBy = "ENTRYNUM";
        }
        DTS.Open(tqueryInfo);
        ht[tpath] = DTS.GetView();
      }
      responseModel.SetData(ht);
    }

    protected virtual void doGetId(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      int CNT = int.Parse(Params["CNT"] + "");
      string RESOURCEID = Params["RESOURCEID"] + "";
      Resource tresource = null;
      if (RESOURCEID != "")
      {
        operate01.GetResource(RESOURCEID);
      }
      responseModel.SetData(operate01.GetNewID(tresource, CNT));
    }

    protected virtual void doGetBillCode(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string TCODE =  Params["TCODE"] + "";
       Dictionary<string, object> oParams = new Dictionary<string, object>();
      oParams["TCODE"] = TCODE;
      oParams["OCODE"] = new ParamInfo("", System.Data.DbType.String, System.Data.ParameterDirection.Output);
      ArrayList list = new ArrayList();
      list.Add(new ExecInfo("PSS_GENCODE", oParams));
      operate01.Save(list);
      responseModel.SetData((oParams["OCODE"] as ParamInfo).Value + "");
    }




    protected IDictionary<string, DataView> _doSave(MOUDLE MD, ViewRow row, Hashtable Params, ArrayList saveList)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string BEFOREAPI = row.GetString("BEFOREAPICODE");
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (BEFOREAPI != "")
      {
        ViewRow beforeAPI = MD.GetAPI(BEFOREAPI);
        if ("exec" == beforeAPI.GetString("APITYPE"))
        {
          Dictionary<string, object> dicPrams = new Dictionary<string, object>();
          foreach (string key in Params.Keys)
          {
            dicPrams[key] = Params[key];
          }
          saveList.Add(new ExecInfo(SQLManage.GetSQL(beforeAPI.GetString("SQLID")), dicPrams));
        }
      }
      IDictionary<string, DataView> viewList = new Dictionary<string, DataView>();
      foreach (DictionaryEntry d in Params)
      {

        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        view.InitData(d.Value + "");
        view.FillKey();
        //view.FillEntryNum();
        if ((d.Key + "") == MAINPATH && MAINPATH != "")
        {
          this.setSaveInfo(view.GetView());
        }
        saveList.Add(view.GetView());
        viewList.Add(d.Key + "", view.GetView());
      }
      //单据号处理
      if (MAINPATH != "")
      {
        DataView MAIN = viewList[MAINPATH];
        M01 MMAIN = new M01(this.operate01, MAIN);
        ResourceField field = MAIN.Resource.Fields.Find((ResourceField f) =>
        {
          return f.FIELDNAME == "BILLCODE";
        });
        if (field != null && (MMAIN.GetValue("BILLCODE") + "" == ""))
        {
          MMAIN.setBillCode(field.VFORMAT);
        }
      }
      //主外键处理
      foreach (var item in MD.PATHREL.GetView())
      {
        var PATHA = item["PATHNAMEA"] + "";
        var PATHB = item["PATHNAMEB"] + "";
        var FIELDA = item["RFIELDSA"] + "";
        var FIELDB = item["RFIELDSB"] + "";
        if (viewList.ContainsKey(PATHB))
        {
          if (!viewList.ContainsKey(PATHA))
          {
            continue;
          }
          var v = viewList[PATHA][0][FIELDA];
          viewList[PATHB].ForEach((ViewRow tr) =>
          {
            tr[FIELDB] = v;
          });
        }
      }
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = null;
          if (ePath != "")
          {
            eView = viewList[ePath];
          }
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }
      return viewList;
    }

    protected ExecInfo GetExecInfo(ViewRow apiRow, DataView eView)
    {
      Dictionary<string, object> dicPrams = new Dictionary<string, object>();
      if (eView != null)
      {
        string eApiParam = apiRow.GetString("APIPARAM");
        string[] aa = eApiParam.Split(',');
        foreach (var a in aa)
        {
          if (eView.Count == 1)
          {
            dicPrams[a] = eView[0][a];
          }
          else if (eView.Deleted.Count == 1)
          {
            dicPrams[a] = eView.Deleted[0][a];
          }
        }
      }
      return new ExecInfo(SQLManage.GetSQL(apiRow.GetString("SQLID")), dicPrams);
    }

    protected virtual void doSave(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      try
      {
        string MAINPATH = row.GetString("PATHNAME");
        IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
        if (viewList.ContainsKey(MAINPATH))
        {
          M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
          MAIN.setReSubmitInfo(this.userInfo);
          MAIN.setReCheckInfo(this.userInfo);
          MAIN.setReVerifyInfo(this.userInfo);
          string flowCode = MD.GetValue("FLOWCODE");
          MAIN.setState(flowCode);
        }
        operate01.Save(saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
        responseModel.SetData(viewList);
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
    }

    protected virtual void doDelete(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      ArrayList saveList = new ArrayList();
      Dictionary<string, DataView> viewList = new Dictionary<string, DataView>();
      foreach (DictionaryEntry d in Params)
      {
        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        view.InitData(d.Value + "");
        saveList.Add(view.GetView());
        viewList[d.Key + ""] = view.GetView();
      }
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = null;
          if (ePath != "")
          {
            eView = viewList[ePath];
          }
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }
      try
      {
        operate01.Save(saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
      responseModel.SetData(saveList);
    }
    protected virtual void doDelete2(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      responseModel.SetData(true);
    }

    protected virtual void doUpdate(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string APIPARAM = row.GetString("APIPARAM");
      string FILTERCODE = row.GetString("FILTERCODE");
      ArrayList saveList = new ArrayList();
      foreach (DictionaryEntry d in Params)
      {
        ViewRow pathRow = MD.GetPath(d.Key + "");
        BaseModel view = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        BaseModel qview = this.GetModel(d.Key + "", pathRow.GetString("RESOURCEID"));
        QueryInfo queryInfo = new QueryInfo();
        view.InitData(d.Value + "");
        queryInfo.FilterCode = FILTERCODE;
        string[] a = APIPARAM.Split(',');
        for (int i = 0; i < a.Length; i++)
        {
          queryInfo.FilterParams[a[i]] = view.GetValue(a[i]);
        }
        qview.Open(queryInfo);
        for (int i = 0; i < a.Length; i++)
        {
          qview.SetValue(a[i], view.GetValue(a[i]));
        }
        try
        {
          saveList.Add(qview.GetView());
          operate01.Save(saveList);
          this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
          qview.Open(queryInfo);
        }
        catch (Exception ex)
        {
          this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
          throw ex;
        }
        responseModel.SetData(qview.GetView());
      }
    }
    public override BaseModel GetModel(string Path, string RESOURCEID)
    {
      return new M01(this.operate01, RESOURCEID);
    }

    protected M01 getMainPath(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string flowCode = MD.GetValue("FLOWCODE");
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      M01 MAIN = (M01)GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      MAIN.OpenByID(Params["ID"] + "");
      return MAIN;
    }

    protected void doFlowCodeSave(M01 MAIN, MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      MAIN.setState(flowCode);
      try
      {
        saveList.Add(MAIN.GetView());
        operate01.Save(saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
    }

    protected virtual void doSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string MAINPATH = row.GetString("PATHNAME");
      IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
      M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
      MAIN.setSubmitInfo(this.userInfo);
      string flowCode = MD.GetValue("FLOWCODE");
      MAIN.setState(flowCode);
      try
      {
        operate01.Save(saveList);
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME") + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
      responseModel.SetData(viewList);
    }
    protected virtual void doReSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);

      MAIN.setReSubmitInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      for (int i = 0; i < MAIN.GetView().Count; i++)
      {
        MAIN.SetValue("VERIFYID", Params["NEXTAPRID"], i);
        MAIN.SetValue("VERIFIER", Params["NEXTAPRER"], i);
      }
      MAIN.setCheckInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doReCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReCheckInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setVerifyInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }
    protected virtual void doReVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReVerifyInfo(this.userInfo);
      this.doFlowCodeSave(MAIN, MD, row, Params);
      responseModel.SetData(MAIN.GetView());
    }

    protected virtual void doBatchCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "已审核"));
           row1["STATE"] = BillState.待审批;
           row1["CHECKREMARK"] = Params["REMARK"];
           row1["VERIFYID"] = Params["NEXTAPRID"];
           row1["VERIFIER"] = Params["NEXTAPRER"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);

        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchCheckReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "审核驳回"));
           row1["STATE"] = BillState.已驳回;
           row1["CHECKREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", "", "撤销审核"));
           row1["STATE"] = BillState.待审核;
           row1["CHECKREMARK"] = "";
           row1["VERIFYID"] = "";
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReCheckInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "已审批"));
           row1["STATE"] = BillState.已审批;
           row1["VERIFYREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setVerifyInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchVerifyReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           saveList.Add(this.addOperateLogs(row1["ID"] + "", Params["REMARK"] + "", "审批驳回"));
           row1["STATE"] = BillState.已驳回;
           row1["VERIFYREMARK"] = Params["REMARK"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCheckInfo(this.userInfo);

        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
          {
            saveList.Add(this.addOperateLogs(row1["ID"] + "", "", "撤销审批"));
            row1["STATE"] = BillState.待审批;
            row1["VERIFYREMARK"] = "";
          });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReVerifyInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual BaseModel doGetBatchMain(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string APIPARAM = row.GetString("APIPARAM");
      string FILTERCODE = row.GetString("FILTERCODE");
      Params["FILTERCODE"] = row["FILTERCODE"];
      ArrayList saveList = new ArrayList();
      Hashtable ht = new Hashtable();
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      QueryInfo queryInfo = GetQueryInfo(Params);
      queryInfo.FilterParams["AEMPID"] = this.userInfo["EMPID"];
      MAIN.Open(GetQueryInfo(Params));
      if (MAIN.GetView().Count != (Params["ID"] + "").Split(',').Length)
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return null;
      }
      return MAIN;
    }

    protected virtual DataView addOperateLogs(string ID, string REMARK, string STATE)
    {
      DataView logView = new DataView(this.operate01.GetResource("VSS_OPLOGS"));
      ViewRow row = new ViewRow(logView);
      row["REFID"] = ID;
      row["STATE"] = STATE;
      row["REMARK"] = REMARK;
      row["OPLOGER"] = this.userInfo["NICKNAME"] + "";
      row["OPLOGDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      logView.AddRow(row);
      this.operate01.FillKey(logView);
      return logView;
    }
  }
}
