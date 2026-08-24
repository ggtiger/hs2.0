using System.Collections;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.ORM;
using Realso.WebAPI.Models;
using Realso.Core.Base;
using Microsoft.AspNetCore.Hosting;
using Realso.Utils;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using Realso.Data.ORM.Core;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SM110Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public SM110Controller(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row.GetString("APICODE");
      switch (APICODE)
      {
        case "A10"://复核
          this.doCheck(MD, row, Params);
          break;
        case "A11"://撤销复核
          this.doReCheck(MD, row, Params);
          break;
        case "A12"://审批
          this.doVerify(MD, row, Params);
          break;
        case "A13"://撤销审批
          this.doReVerify(MD, row, Params);
          break;
        case "A15"://驳回
          this.doReject(MD, row, Params);
          break;
        case "A27"://打印
          this.doPrint(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    protected override DataView addOperateLogs(string ID, string REMARK, string STATE)
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

    protected virtual void doReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setCheckInfo(this.userInfo);
      ArrayList saveList = new ArrayList();

      saveList.Add(MAIN.GetView());
      if (MAIN.GetValue("STATE") + "" == BillState.待审核 + "")
      {
        MAIN.SetValue("CHECKREMARK", Params["REMARK"]);
        saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "审核驳回"));
      }
      else if (MAIN.GetValue("STATE") + "" == BillState.待审批 + "")
      {
        MAIN.SetValue("VERIFYREMARK", Params["REMARK"]);
        saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "审批驳回"));
      }
      else
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.SetValue("STATE", BillState.已驳回);
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }
    protected override void doCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setCheckInfo(this.userInfo);
      for (int i = 0; i < MAIN.GetView().Count; i++)
      {
        MAIN.SetValue("VERIFYID", Params["NEXTAPRID"], i);
        MAIN.SetValue("VERIFIER", Params["NEXTAPRER"], i);
      }
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      if (MAIN.GetValue("STATE") + "" != BillState.待审核 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      saveList.Add(this.addOperateLogs(Params["ID"] + "", Params["REMARK"] + "", "已审核"));
      MAIN.setState(flowCode);
      MAIN.SetValue("CHECKREMARK", Params["REMARK"]);
      saveList.Add(MAIN.GetView());
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }
    protected override void doReCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReCheckInfo(this.userInfo);
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      saveList.Add(MAIN.GetView());
      if (MAIN.GetValue("STATE") + "" != BillState.待审批 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.setState(flowCode);
      MAIN.SetValue("CHECKREMARK", "");
      saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "撤销审批"));
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }

    protected override void doVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setVerifyInfo(this.userInfo);
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      saveList.Add(MAIN.GetView());
      string BEFOREAPI = row.GetString("BEFOREAPICODE");

      if (MAIN.GetValue("STATE") + "" != BillState.待审批 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.setState(flowCode);
      MAIN.SetValue("VERIFYREMARK", Params["REMARK"]);
      saveList.Add(this.addOperateLogs(Params["ID"] + "", Params["REMARK"] + "", "已审批"));
      //审批后
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = MAIN.GetView();
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }
    protected override void doReVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setReVerifyInfo(this.userInfo);
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      saveList.Add(MAIN.GetView());
      if (MAIN.GetValue("STATE") + "" != BillState.已审批 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.setState(flowCode);
      MAIN.SetValue("VERIFYREMARK", "");
      saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "撤销审批"));
      //审批后
      string AFTERAPI = row.GetString("AFTERAPICODE");
      if (AFTERAPI != "")
      {
        ViewRow afterAPI = MD.GetAPI(AFTERAPI);
        if ("exec" == afterAPI.GetString("APITYPE"))
        {
          string ePath = afterAPI.GetString("PATHNAME");
          DataView eView = MAIN.GetView();
          saveList.Add(this.GetExecInfo(afterAPI, eView));
        }
      }

      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }

    protected virtual void doPrint(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      //1.查询生成证书所需数据
      string MAINPATH = "MAIN";
      string DTSAPATH = "DTSA";
      ViewRow pathRow1 = MD.GetPath(MAINPATH);
      ViewRow pathRow2 = MD.GetPath(DTSAPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow1.GetString("RESOURCEID"));
      BaseModel DTSA = GetModel(DTSAPATH, pathRow2.GetString("RESOURCEID"));
      QueryInfo queryInfo1 = new QueryInfo();
      queryInfo1.FilterCode = "F00";
      queryInfo1.FilterParams["ID"] = Params["ID"];
      MAIN.OpenByID(Params["ID"] + "");
      DTSA.Open(queryInfo1);
      Dictionary<string, object> list = new Dictionary<string, object>();
      string[] fileds = { "DOCNAME", "DOCCODE", "DOCSORT", "DEPTNAME", "BILLDATE", "CREATER","SUBMITER","MODIFER","CHECKER","VERIFIER" };
      List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
      List<ViewColumn> columns = MAIN.GetView().Columns;
        columns.ForEach(column =>
        {
          list[column.Name] = MAIN.GetValue(column.Name);
        });

        DTSA.GetView().ForEach(viewRow =>
        {
          if (!list.ContainsKey(viewRow.GetString("FIELDNAME")))
            list[viewRow.GetString("FIELDNAME")] = viewRow.GetValue("FIELDVALUE");
        });

      string EXPTEMP = MAIN.GetValue("EXPTEMP");
      FILE file = new FILE(this.operate01);
      string filePath = file.GetFilePath(EXPTEMP);
      if (filePath == "")
      {
        return;
      }
      string copyDoc2 = filePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
      Realso.Utils.Logger.Info("文档地址==>" + filePath);
      WordHelper.ReplaceFromTemplate(WordHelper.CopyWord(filePath, copyDoc2), list);
      Realso.Utils.Logger.Info("制作完成==>" + filePath);
      Hashtable saveFile2 = new Hashtable();
      saveFile2["UPLOADFILEPATH"] = copyDoc2;
      saveFile2["UPLOADTYPE"] = "其他";
      saveFile2["FILENAME"] = MAIN.GetValue("DOCCODE") + "文档.docx";
      file.SaveFile(saveFile2);
      saveList.Add(file.GetView());
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(file.GetValue("ID"));
    }
  }
}
