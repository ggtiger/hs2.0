using System.Reflection.Metadata;
using System.Globalization;
using System.Net.Http;
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
using Newtonsoft.Json;
using Realso.WebAPI.Common;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class RM16Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public RM16Controller(IHostingEnvironment hostingEnvironment)
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
         case "A10":
          doBatchPrint(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }
    protected  void doBatchPrint(MOUDLE MD, ViewRow row, Hashtable Params){
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      Dictionary<string, object> list = new Dictionary<string, object>();
      ArrayList saveList = new ArrayList();
      string[] tfileds = { "CUSTNAME","MNAME", "SIZETYPE", "OPCODE"};
       if (MAIN != null)
      {
        ViewRow viewRow = MAIN.GetView()[0];
        List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
        int i = 1;
        MAIN.GetView().ForEach(ViewRow =>
        {
          Dictionary<string, object> tdic = new Dictionary<string, object>();
          BaseModel DTS = new BaseModel(this.operate01,"VCK_DELEGATE_ITEM");
          QueryInfo queryInfo =new QueryInfo();
          Hashtable filterParams = new Hashtable();
          queryInfo.FilterCode = "F01";
          queryInfo.FilterParams =filterParams;
          filterParams["ID"] = ViewRow["ID"];
          DTS.Open(queryInfo);
          DTS.GetView().ForEach(DTSViewRow =>{
            foreach (var f in tfileds)
            {
              tdic["T" + f] = DTSViewRow[f];
            }
            tdic["TCUSTNAME"] = ViewRow["CUSTNAME"];
            tdic["TCREATER"] = userInfo["NICKNAME"];
            tdic["TCREATEDATE"] = DateTime.Now.ToString("yyyy-MM-dd");
            tlist.Add(tdic);
          });
        });

        foreach (var f in tfileds)
        {
          list["T" + f] = tlist;
        }
        list["TCUSTNAME"] = tlist;
        list["TCREATER"] = tlist;
        list["TCREATEDATE"] = tlist;

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
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setSubmitInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
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

    protected override void doCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList saveList = new ArrayList();
      string MAINPATH = row.GetString("PATHNAME");
      IDictionary<string, DataView> viewList = this._doSave(MD, row, Params, saveList);
      M01 MAIN = new M01(this.operate01, viewList[MAINPATH]);
      MAIN.setCheckInfo(this.userInfo);
      string flowCode = MD.GetValue("FLOWCODE");
      MAIN.setState(flowCode);
      //生成受理单
      M01 ACCEPT = new M01(this.operate01,"VCK_ACCEPT");
      DataView aview = ACCEPT.GetView();
      M01 DTS = new M01(this.operate01, viewList["DTS"]);

      Hashtable queryParams = new Hashtable();
      queryParams["FILTERCODE"] = "F25";
      M01 queryModel = new M01(this.operate01,"VCK_ACCEPT");
      QueryInfo queryInfo = GetQueryInfo(queryParams);
      queryInfo.FilterParams = new Hashtable();
      queryInfo.FilterParams["BILLID"] = MAIN.GetValue("ID");
      queryModel.Open(queryInfo);


      ResourceField field = aview.Resource.Fields.Find((ResourceField f) =>
        {
          return f.FIELDNAME == "BILLCODE";
        });
      DTS.GetView().ForEach((ViewRow vr)=>{
        ViewRow tvr= queryModel.GetView().FindLast((ViewRow vvr)=>{
          return vr.GetString("REFENTRYID") == vvr.GetString("ID");
        });
        if(tvr!=null){
          return;
        }
        ViewRow arow = new ViewRow(aview);
        string[] cvfields = {"ID","MNAME","SIZETYPE","CNT","OPCODE","MANUFACTURER"
        ,"ADEPTID","PTEMPLATEID","ADEPTID","ISFFREE","CAMT","OAMT","BUSTYPEID","SLINKER","SENDNAME","SENDDATE","WCUSTNAME"};
        foreach (var f in cvfields){
          if("SENDDATE" == f){
            arow[f] =  (vr[f]+"").Split(' ')[0];
          }else{
            arow[f] = vr[f];
            if(f=="CAMT"||f=="OAMT"){
              arow[f] = 0;
            }
          }
        }
        string[] cvmfields = {"CUSTID","LINKER","MOBILE","ADDR","EMAIL"};
        foreach (var f in cvmfields){
          if(MAIN.GetValue(f)+""!=""){
           arow[f] = MAIN.GetValue(f);
          }
        }
        arow["STATE"] = "1";
        arow["VER"] = "1";
        arow["WTCODE"] = MAIN.GetValue("BILLCODE");
        ACCEPT.setMakeInfo(this.userInfo);
        arow["BILLDATE"] = DateTime.Now.ToString("yyyy-MM-dd");
        aview.AddRow(arow);
        ACCEPT.setBillCode(field.VFORMAT,"BILLCODE",aview.Count-1);
        vr["REFENTRYID"] = arow["ID"];
        saveList.Add(this.addOperateLogs(arow["ID"] + "","委托", "已审核"));
      });
      viewList.Add("ACCEPT",aview);
      saveList.Add(aview);
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);

      Hashtable msgParams = new Hashtable();
       string[] cvmfields2 = {"ID","BILLCODE","CUSTID","LINKER","MOBILE","ADDR","EMAIL"};
      foreach (var f in cvmfields2){
           msgParams[f] = MAIN.GetValue(f);
        }

      WxUserMessage.addMessage(MAIN.GetValue("MOBILE"),"","委托确认",msgParams);
      responseModel.SetData(viewList);
    }

  }
}
