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
  public class RM13Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public RM13Controller(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row.GetString("APICODE");
      switch (APICODE)
      {
        case "A12":
          doBatchDiscount(MD, row, Params);
          break;
        case "A13":
          doBatchFee(MD, row, Params);
          break;
        case "A14":
          doBatchReFee(MD, row, Params);
          break;
        case "A16":
          doAPrint(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    protected virtual void doBatchDiscount(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      string AFTERAPI = row.GetString("AFTERAPICODE");
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["DISCOUNT"] = Params["DISCOUNT"];
         });
        saveList.Add(MAIN.GetView());
        if (AFTERAPI != "")
        {
          ViewRow afterAPI = MD.GetAPI(AFTERAPI);
          if ("exec" == afterAPI.GetString("APITYPE"))
          {
            Dictionary<string, object> dicPrams = new Dictionary<string, object>();
            dicPrams["ID"] = Params["ID"];
            ExecInfo execInfo = new ExecInfo(SQLManage.GetSQL(afterAPI.GetString("SQLID")), dicPrams);
            saveList.Add(execInfo);
          }
        }
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        MAIN = this.doGetBatchMain(MD, row, Params);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchFee(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["CHARGEID"] = this.userInfo["ID"];
           row1["CHARGER"] = this.userInfo["NICKNAME"];
           row1["CHARGETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
           row1["RAMT"] = row1["AMT"];
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    private void doAPrint(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      double AMT = 0D;
      double CAMT = 0D;
      double BAMT = 0D;
      double CNT = 0D;
      Dictionary<string, object> list = new Dictionary<string, object>();
      string[] mfileds = {  "CUSTNAME","WTCODE","BILLDATE","CREATER","CREATETIME","CHARGER","CHARGETIME" };
      string[] tfileds = { "IDX", "BILLCODE","BILLDATE","BUSTYPEID", "MNAME", "SIZETYPE", "OPCODE", "CNT", "ISFFREE","AMT","CAMT","BAMT","OREMARK","DISCOUNT","OAMT","RAMT","REMARK","REMARK1","REMARK2" };

      if (MAIN.GetView().Count > 0)
      {
        ViewRow viewRow = MAIN.GetView()[0];
        foreach (var f in mfileds)
        {
          list[f] = viewRow[f];
        }

        List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
        int i = 1;
        MAIN.GetView().ForEach(ViewRow =>
        {
          Dictionary<string, object> tdic = new Dictionary<string, object>();
          foreach (var f in tfileds)
          {

            if (f == "IDX")
            {
              tdic["T" + f] = i++;
            }
            else if (f == "ISFFREE")
            {
              if (ViewRow[f] + "" == "1")
              {
                tdic["TISFFREE1"] = "√";
              }
              else
              {
                tdic["TISFFREE2"] = "√";
              }
            }
            else if (f == "BUSTYPEID")
            {
              if (ViewRow[f] + "" == "1")
              {
                tdic["TBUSTYPENAME"] = "委外";
              }
              if (ViewRow[f] + "" == "2")
              {
                tdic["TBUSTYPENAME"] = "自检";
              }
            }
            else
            {
              tdic["T" + f] = ViewRow[f];
            }
            if (f=="CAMT"||f=="OAMT"||f=="BAMT")
            {
                double numDouble3;
                if (!double.TryParse(ViewRow[f]+"", out numDouble3))
                {
                      numDouble3 = 0D;
                }
                CAMT +=numDouble3;
            }
             if (f=="AMT")
            {
                double numDouble3;
                if (!double.TryParse(ViewRow[f]+"", out numDouble3))
                {
                      numDouble3 = 0D;
                }
                AMT +=numDouble3;
            }
             if (f=="CNT")
            {
                double numDouble3;
                if (!double.TryParse(ViewRow[f]+"", out numDouble3))
                {
                      numDouble3 = 0D;
                }
                CNT +=numDouble3;
            }

          }
          tlist.Add(tdic);
        });
        foreach (var f in tfileds)
        {
          if (f == "ISFFREE")
          {
            list["TISFFREE1"] = tlist;
            list["TISFFREE2"] = tlist;
          }
          else
            list["T" + f] = tlist;
        }
        list["SUMAMT"] = AMT;
        list["SUMCAMT"] = CAMT;
        list["SUMCNT"] = CNT;
        list["FEEDATE"] =DateTime.Now.ToString("yyyy年MM月dd日");;

        FILE file = new FILE(this.operate01);
        string filePath = file.GetFilePath(row.GetString("FILEID"));
        if (filePath == "")
        {
          this.responseModel.SetError("未找到打印模版");
          return;
        }
        string copyDoc = filePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
        Realso.Utils.Logger.Info("证书地址==>" + filePath);
        WordHelper.ReplaceFromTemplate(WordHelper.CopyWord(filePath, copyDoc), list);
        Hashtable saveFile = new Hashtable();
        saveFile["UPLOADFILEPATH"] = copyDoc;
        saveFile["UPLOADTYPE"] = "其他";
        saveFile["FILENAME"] = MAIN.GetValue("CUSTNAME") + "-" + MAIN.GetValue("SENDDATE") + ".docx";
        file.SaveFile(saveFile);
        saveList.Add(file.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(file.GetValue("ID"));
      }
    }

    protected ExecInfo GetExecInfo2(ViewRow apiRow, DataView eView)
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

    protected virtual void doBatchReFee(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["CHARGEID"] = "";
           row1["CHARGER"] = "";
           row1["CHARGETIME"] = "";
           row1["RAMT"] = "";
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
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
      //queryInfo.FilterParams["AEMPID"] = this.userInfo["EMPID"];
      MAIN.Open(GetQueryInfo(Params));
      if (MAIN.GetView().Count != (Params["ID"] + "").Split(',').Length)
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return null;
      }
      return MAIN;
    }
  }
}
