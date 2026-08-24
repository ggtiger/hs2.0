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
  public class RM15Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public RM15Controller(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row.GetString("APICODE");
      switch (APICODE)
      {
        case "A12":
          doBatchSubmit(MD, row, Params);
          break;
        case "A13":
          doBatchReSubmit(MD, row, Params);
          break;
        case "A14":
          doBatchAccept(MD, row, Params);
          break;
        case "A15":
          doBatchReAccept(MD, row, Params);
          break;
        case "A17":
          doPrint(MD, row, Params);
          break;
        case "A20":
          doDownload(MD, row, Params);
          break;
        case "A21":
          doAPrint(MD, row, Params);
          break;
        case "A22":
          doPPrint(MD, row, Params);
          break;
        case "A23":
          doBatchComplete(MD, row, Params);
          break;
        case "A51":
          doBatchReturn(MD, row, Params);
          break;
        case "A24":
          doBatchReComplete(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    private void doPPrint(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      Dictionary<string, object> list = new Dictionary<string, object>();
      string[] mfileds = { "CUSTCODE", "CUSTNAME", "SENDNAME", "SENDDATE", "ADDR", "LINKER", "MOBILE", "AEMPNAME","WTCODE" };
      string[] tfileds = { "CUSTNAME","ADEPTNAME","BILLCODE", "MNAME", "SIZETYPE", "OPCODE", "BILLDATE", "GETDATE","REMARK","REMARK1","REMARK2"};
      if (MAIN.GetView().Count > 0)
      {
        ViewRow viewRow = MAIN.GetView()[0];
        List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
        int i = 1;
        MAIN.GetView().ForEach(ViewRow =>
        {
          Dictionary<string, object> tdic = new Dictionary<string, object>();
          foreach (var f in tfileds)
          {
            tdic["T" + f] = ViewRow[f];
          }
          tlist.Add(tdic);
        });
        foreach (var f in tfileds)
        {
          list["T" + f] = tlist;
        }

        FILE file = new FILE(this.operate01);
        string filePath = file.GetFilePath(row.GetString("FILEID"));
        if (filePath == "")
        {
          this.responseModel.SetError("未找到打印模版");
          return;
        }
        string imgURL =  getQRFilePath(2,"进度二维码","&WTCODE="+MAIN.GetValue("WTCODE")+"&SLINKER="+MAIN.GetValue("SLINKER"));
        list["JDQR"] =  imgURL;
        list["JDQR_IMG2"] = imgURL;
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


    private void doAPrint(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      Dictionary<string, object> list = new Dictionary<string, object>();
      string[] mfileds = { "CUSTCODE", "CUSTNAME", "SENDNAME", "SENDDATE", "ADDR", "LINKER", "MOBILE","WCUSTNAME","SLINKER", "AEMPNAME","WTCODE","BILLDATE" ,"SUBMITER","PRINTDATE","CHECKNAME","EMAIL","CREATER","LANDLINE"};
      string[] tfileds = { "IDX", "BILLCODE", "MNAME", "SIZETYPE", "OPCODE", "CNT", "ISFFREE", "AGREEDATE","REMARK","REMARK1","REMARK2","CUSTNAME"};
      if (MAIN.GetView().Count > 0)
      {
        ViewRow viewRow = MAIN.GetView()[0];
         viewRow["PRINTDATE"] = DateTime.Now.ToString("yyyy-MM-dd");
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
            else
            {
              tdic["T" + f] = ViewRow[f];
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

        FILE file = new FILE(this.operate01);
        string filePath = file.GetFilePath(row.GetString("FILEID"));
        if (filePath == "")
        {
          this.responseModel.SetError("未找到打印模版");
          return;
        }
        string imgURL =  getQRFilePath(2,"进度二维码","&WTCODE="+MAIN.GetValue("WTCODE")+"&SLINKER="+MAIN.GetValue("SLINKER"));
        list["JDQR"] =  imgURL;
        list["JDQR_IMG2"] = imgURL;
        Realso.Utils.Logger.Info($"图片地址 {list["JDQR_IMG2"]}");
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
     private string getQRFilePath(int size,string pathType,string urlParm){
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Url:{pathType}");
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      string filePath =  Realso.Utils.ConfigHelper.GetConfig($"Upload:临时:Path");
      string fileName= "二维码"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".png" ;
      DirectoryInfo di = new DirectoryInfo(rootPath + filePath);
      if (!di.Exists)
      {
        di.Create();
      }
      QRHelper.SaveQR(rootPath+filePath+fileName, size, rPath+urlParm);
      return rootPath+filePath+fileName;
    }

    protected virtual void doPrint(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      ArrayList saveList = new ArrayList();
      saveList.Add(MAIN.GetView());
      if (MAIN.GetValue("STATE") + "" != BillState.已制证 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.SetValue("STATE", BillState.已打印);
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }

    protected virtual void doDownload(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      FILE file = new FILE(this.operate01);
      IList<Realso.Utils.FileInfo> fileInfos = new List<Realso.Utils.FileInfo>();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["STATE"] = 14;
           fileInfos.Add(new Realso.Utils.FileInfo($"{row1["BILLDATE"]}-{row1["BILLCODE"]}-{row1["CUSTNAME"]}-{row1["MNAME"]}-{row1["SIZETYPE"]}-{row1["OPCODE"]}.pdf", file.GetFilePath(row1.GetString("CERTID")).Replace("docx", "pdf")));
         });
        Hashtable saveFile = new Hashtable();
        string expPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string fileName = "批量下载" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
        saveFile["UPLOADFILEPATH"] = expPath + fileName;
        saveFile["UPLOADTYPE"] = "";
        saveFile["FILENAME"] = fileName;
        FileHelper.Zip(fileInfos, expPath + fileName);
        file.SaveFile(saveFile);
        saveList.Add(MAIN.GetView());
        saveList.Add(file.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(new { ID = file.GetValue("ID"), Items = MAIN.GetView() });
      }
    }
    protected virtual void doBatchAccept(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["AEMPID"] = this.userInfo["EMPID"];
           row1["ADEPTID"] = this.userInfo["DEPTID"];
           row1["STATE"] = 8;
         });
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReAccept(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["AEMPID"] = "";
           row1["ADEPTID"] = "";
           row1["PTEMPLATEID"] = "";
           row1["CAMT"] = 0;
           row1["AMT"] = 0;
           row1["STATE"] = 7;
         });
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected override void doBatchSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           if (row1.GetString("AEMPID") != "")
           {
             row1["STATE"] = 8;
           }
           else
           {
             row1["STATE"] = 7;
           }
          saveList.Add(this.addOperateLogs(row1["ID"] + "", "受理", "已接收"));
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setSubmitInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected override void doAfterSubmit(MOUDLE MD, ViewRow row, Hashtable Params,ArrayList saveList)
    {

    }

    protected override void doBatchReSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["STATE"] = 1;
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReSubmitInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchComplete(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
             row1["STATE"] = 15;
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCompleteInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected virtual void doBatchReturn(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
             row1["STATE"] = 21;
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setCompleteInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }



    protected virtual void doBatchReComplete(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           row1["STATE"] = 7;
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReCompleteInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected override void doSubmit(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string MAINPATH = row.GetString("PATHNAME");
      IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
      M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
      MAIN.setSubmitInfo(this.userInfo);
      if (MAIN.GetValue("AEMPID") == "")
      {
        MAIN.SetValue("STATE", 7);
      }
      else
      {
        MAIN.SetValue("STATE", 8);
      }
      saveList.Add(this.addOperateLogs(MAIN.GetValue("ID") + "", "受理", "已接收"));
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(viewList);
    }
  }
}
