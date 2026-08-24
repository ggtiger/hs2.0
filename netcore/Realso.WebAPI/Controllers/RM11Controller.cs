using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
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
  public class RM11Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public RM11Controller(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }

    /// <summary>
    /// 重写 doSave，在保存前校验原始记录唯一性：同一委托单下相同设备编号+相同模板不允许重复
    /// </summary>
    protected override void doSave(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      string MODULENAME = MD.GetView()[0].GetString("MODULENAME");
      // 只对 LI_M02（原始记录）模块做唯一性校验
      if (MODULENAME == "LI_M02" || Params["ISCHECKREPEAT"] + "" == "true")
      {
        this.checkDuplicateRecord(MD, row, Params);
      }
      base.doSave(MD, row, Params);
    }

    /// <summary>
    /// 校验原始记录唯一性：同一委托单下相同设备编号(MNAME+OPCODE)不允许重复记录
    /// </summary>
    private void checkDuplicateRecord(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));

      // 解析前端传来的 MAIN 数据
      MAIN.InitData(Params[MAINPATH] + "");
      MAIN.FillKey();

      if (MAIN.GetView().Count == 0) return;

      string REFBILLID = MAIN.GetValue("REFBILLID") + "";
      string MNAME = MAIN.GetValue("MNAME") + "";
      string OPCODE = MAIN.GetValue("OPCODE") + "";
      string ID = MAIN.GetValue("ID") + "";

      if (string.IsNullOrEmpty(REFBILLID)) return;

      // 查询同一委托单下是否有相同设备编号的记录（排除自身）
      string sql = "SELECT COUNT(*) AS CNT FROM tck_orecord WHERE REFBILLID=@REFBILLID AND OPCODE=@OPCODE AND IS_DELETED=0 AND ID<>@ID";
      Hashtable sqlParams = new Hashtable();
      sqlParams["REFBILLID"] = REFBILLID;
      sqlParams["OPCODE"] = OPCODE;
      sqlParams["ID"] = ID;

      try
      {
        var result = this.operate01.Query(sql, sqlParams);
        if (result != null && result.Count() > 0)
        {
          foreach (IDictionary<string, object> item in result)
          {
            int cnt = Convert.ToInt32(item["CNT"]);
            if (cnt > 0)
            {
              responseModel.SetError("该委托单下已存在设备编号为【" + OPCODE + "】的原始记录，不允许重复！");
              return;
            }
          }
        }
      }
      catch (Exception) { /* 校验失败不阻塞保存 */ }
    }
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row.GetString("APICODE");
      switch (APICODE)
      {
        case "A11"://接受办理
          this.doBatchAccept(MD, row, Params);
          break;
        case "A30"://撤销办理
          this.doBatchReAccept(MD, row, Params);
          break;
        case "A12"://复核
          this.doCheck(MD, row, Params);
          break;
        case "A13"://撤销复核
          this.doReCheck(MD, row, Params);
          break;
        case "A14"://审批
          this.doVerify(MD, row, Params);
          break;
        case "A15"://撤销审批
          this.doReVerify(MD, row, Params);
          break;
        case "A16"://驳回
          this.doReject(MD, row, Params);
          break;
        case "A21":
          this.doGenCert(MD, row, Params);
          break;
        case "A23"://复核
          this.doBatchCheck(MD, row, Params);
          break;
        case "A28"://复核驳回
          this.doBatchCheckReject(MD, row, Params);
          break;
        case "A24"://撤销复核
          this.doBatchReCheck(MD, row, Params);
          break;
        case "A25"://审批
          this.doBatchVerify(MD, row, Params);
          break;
        case "A29"://审批驳回
          this.doBatchVerifyReject(MD, row, Params);
          break;
        case "A26"://撤销审批
          this.doBatchReVerify(MD, row, Params);
          break;
        case "A27"://生成证书
          this.doBatchGenCert(MD, row, Params);
          break;
        case "A50"://批量撤销证书
          this.doBatchReGenCert(MD, row, Params);
          break;
        case "A22":
          this.doInvalid(MD, row, Params);
          break;
        case "A32":
          this.doBatchSubmit(MD, row, Params);
          break;
        case "A33":
          this.doBatchReSubmit(MD, row, Params);
          break;
        case "A34":
          doPrint(MD, row, Params);
          break;
        case "A39":
          doDownload(MD, row, Params);
          break;
        case "A45":
          doUpdateTemplate(MD, row, Params);
          break;
        case "A49":
          doPrintPreview(MD,row,Params);
          break;
        case "A51":
          this.doFieldModify(MD, row, Params);
          break;
        case "A57":
          this.doCheckAnomaly(MD, row, Params);
          break;
        case "A53":
          this.doQueryWTList(MD, row, Params);
          break;
        case "A54":
          this.doQueryWTDetail(MD, row, Params);
          break;
        case "A55":
          this.doECertSign(MD, row, Params);
          break;
        case "A56":
          this.doECertVerify(MD, row, Params);
          break;
        case "A58":
          this.doUpdateECertPwd(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    private void doPrintPreview(MOUDLE MD, ViewRow row, Hashtable Params)
    {
        ArrayList saveList = new ArrayList();
        //1.查询生成证书所需数据
        string MAINPATH = "MAIN";
        string DTSAPATH = "DTSA";
        string DTSBPATH = "DTSB";
        ViewRow pathRow1 = MD.GetPath(MAINPATH);
        ViewRow pathRow2 = MD.GetPath(DTSAPATH);
        ViewRow pathRow3 = MD.GetPath(DTSBPATH);
        BaseModel MAIN = GetModel(MAINPATH, pathRow1.GetString("RESOURCEID"));
        BaseModel DTSA = GetModel(DTSAPATH, pathRow2.GetString("RESOURCEID"));
        BaseModel DTSB = GetModel(DTSBPATH, pathRow3.GetString("RESOURCEID"));
        QueryInfo queryInfo1 = new QueryInfo();
        queryInfo1.FilterCode = "F00";
        queryInfo1.FilterParams["ID"] = Params["ID"];
        MAIN.OpenByID(Params["ID"] + "");
        DTSA.Open(queryInfo1);
        DTSB.Open(queryInfo1);
        Dictionary<string, object> list = new Dictionary<string, object>();
        List<ViewColumn> columns = MAIN.GetView().Columns;
        columns.ForEach(column =>
        {
          list[column.Name] = MAIN.GetValue(column.Name);
          if(column.Name=="SIGNDATE"&&(list[column.Name]+""=="")){
            list[column.Name] =  DateTime.Now.ToString("yyyy-MM-dd");
          }
        });

        DTSB.GetView().ForEach(viewRow =>
        {
          if (!list.ContainsKey(viewRow.GetString("FIELDNAME")))
            list[viewRow.GetString("FIELDNAME")] = viewRow.GetValue("FIELDVALUE");
        });
        string[] fileds = { "ARDNAME", "SIZETYPE", "OMCODE", "DEGREE", "EXPDATE", "CERTCODE","MANUFACTURER","CORGNAME" };
        List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
        DTSA.GetView().ForEach(ViewRow =>
        {
          Dictionary<string, object> tdic = new Dictionary<string, object>();
          foreach (var f in fileds)
          {
            // Bookmark 模版兼容：key 带 T 前缀（如 TARDNAME）
            tdic["T" + f] = ViewRow[f];
            // SDT 模版兼容：key 不带 T 前缀（如 ARDNAME），与字段 Tag 一致
            tdic[f] = ViewRow[f];
          }
          tlist.Add(tdic);
        });
        foreach (var f in fileds)
        {
          // Bookmark 模版兼容：整体子表数据也用 T 前缀
          list["T" + f] = tlist;
        }
        // 兼容 SDT 模版：子表循环标记使用资源名（如 VCK_ORECORDDTSA_TABLE），
        // 替换引擎按 baseName 查找子表数据
        string dtsaResourceName = DTSA.GetView().Resource.RESOURCENAME;
        if (!string.IsNullOrEmpty(dtsaResourceName) && !list.ContainsKey(dtsaResourceName))
        {
          list[dtsaResourceName] = tlist;
        }

        string CERTEMPID = MAIN.GetValue("CERTEMPID");
        FILE file = new FILE(this.operate01);
        string filePath = file.GetFilePath(CERTEMPID);
        if (filePath == "")
        {
          this.responseModel.SetError("未找到证书模版");
          return;
        }

        // 提交人
        string SUBMITESIGNID = MAIN.GetValue("SUBMITESIGNID");
        string path1 = file.GetFilePath(SUBMITESIGNID);
        //CREATER_IMG
        list["CREATER_IMG"] = path1;

        // 审核人
        string CHECKESIGNID = MAIN.GetValue("CHECKESIGNID");
        string path2 = file.GetFilePath(CHECKESIGNID);
        list["CHECKER_IMG"] = path2;

        // 审批人
        string VERIFYESIGNID = MAIN.GetValue("VERIFYESIGNID");
        string path3 = file.GetFilePath(VERIFYESIGNID);
        list["VERIFIER_IMG"] = path3;

        //导出证书
        string copyDoc = filePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
        Realso.Utils.Logger.Info("证书地址==>" + filePath);
        WordHelper.ReplaceFromTemplate(WordHelper.CopyWord(filePath, copyDoc), list);
        Hashtable saveFile = new Hashtable();
        saveFile["UPLOADFILEPATH"] = copyDoc;
        saveFile["UPLOADTYPE"] = "其他";
        saveFile["FILENAME"] = MAIN.GetValue("CERTCODE") + ".docx";
        // saveFile["SYNC"] ="1";
        file.SaveFile(saveFile);
        saveList.Add(file.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(file.GetValue("ID"));
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
            if(!System.IO.File.Exists(file.GetFilePath(row1.GetString("EXPFILEID")).Replace(".docx", ".pdf"))){
              Realso.Utils.MySocket.Send("127.0.0.1", 5555, file.GetFilePath(row1.GetString("EXPFILEID")));
            }
           fileInfos.Add(new Realso.Utils.FileInfo($"{row1["BILLDATE"]}-{row1["CUSTNAME"]}-{row1["MNAME"]}-{row1["SIZETYPE"]}-{row1["OPCODE"]}.pdf", file.GetFilePath(row1.GetString("EXPFILEID")).Replace("docx", "pdf")));
         });
        Hashtable saveFile = new Hashtable();
        string expPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string fileName = "批量下载" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
        saveFile["UPLOADFILEPATH"] = expPath + fileName;
        saveFile["UPLOADTYPE"] = "";
        saveFile["FILENAME"] = fileName;
        FileHelper.Zip(fileInfos, expPath + fileName);
        file.SaveFile(saveFile);
        //saveList.Add(MAIN.GetView());
        saveList.Add(file.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(new { ID = file.GetValue("ID"), Items = MAIN.GetView() });
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
           row1["STATE"] = BillState.待审核;
           row1["CHECKID"] = Params["NEXTAPRID"];
           row1["CHECKER"] = Params["NEXTAPRER"];
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
           row1["STATE"] = BillState.待提交;
           row1["CHECKID"] = "";
         });
        M01 m01 = new M01(this.operate01, MAIN.GetView());
        m01.setReSubmitInfo(this.userInfo);
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }
    protected virtual void doBatchGenCert(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           Hashtable tParams = new Hashtable();
           tParams["ID"] = row1["ID"];
           tParams["ISBATCH"] = "1";
           try
           {
             this.doGenCert(MD, row, tParams);
             row1["STATE"] = BillState.已制证;
           }
           catch (Exception ex)
           {
             Realso.Utils.Logger.Error("doBatchGenCert:" + row1["ID"], ex);
           }
         });
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }


    protected virtual void doBatchReGenCert(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();
      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
         {
           Hashtable tParams = new Hashtable();
           tParams["ID"] = row1["ID"];
           tParams["ISBATCH"] = "1";
           try
           {
              row1["STATE"] =  BillState.已审批;
              row1["CERTID"] =  "";
              row1["EXPFILEID"] =  "";
              row1["SIGNDATE"] =  "";
              saveList.Add(@"UPDATE tck_accept SET STATE=9 ,CERTID=NULL  WHERE ID = '" + MAIN.GetValue("REFBILLID") + "';");
           }
           catch (Exception ex)
           {
             Realso.Utils.Logger.Error("doBatchReGenCert:" + row1["ID"], ex);
           }
         });
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    protected void doInvalid(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setCheckInfo(this.userInfo);
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      if (MAIN.GetValue("STATE") + "" != BillState.已制证 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      saveList.Add(this.addOperateLogs(Params["ID"] + "", Params["REMARK"] + "", "已作废"));
      //MAIN.
      MAIN.setInvalidInfo(this.userInfo);
      MAIN.SetValue("STATE", BillState.已作废);
      saveList.Add(MAIN.GetView());
      saveList.Add(@"UPDATE tck_accept SET STATE=8,CERTID = NULL  WHERE ID = '" + MAIN.GetValue("REFBILLID") + "';");
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }



    protected virtual void doReject(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      MAIN.setCheckInfo(this.userInfo);
      ArrayList saveList = new ArrayList();

      saveList.Add(MAIN.GetView());
      if (MAIN.GetValue("STATE") + "" == BillState.待审核 + "")
      {
        saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "审核驳回"));
      }
      else if (MAIN.GetValue("STATE") + "" == BillState.待审批 + "")
      {
        saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "审批驳回"));
      }
      else
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      MAIN.SetValue("STATE", BillState.已驳回);
      saveList.Add(@"UPDATE tck_accept SET STATE=13  WHERE ID = '" + MAIN.GetValue("REFBILLID") + "';");
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(MAIN.GetView());
    }
    protected override void doCheck(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 MAIN = this.getMainPath(MD, row, Params);
      for (int i = 0; i < MAIN.GetView().Count; i++)
      {
        MAIN.SetValue("VERIFYID", Params["NEXTAPRID"], i);
        MAIN.SetValue("VERIFIER", Params["NEXTAPRER"], i);
      }
      MAIN.setCheckInfo(this.userInfo);
      ArrayList saveList = new ArrayList();
      string flowCode = MD.GetValue("FLOWCODE");
      if (MAIN.GetValue("STATE") + "" != BillState.待审核 + "")
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      saveList.Add(this.addOperateLogs(Params["ID"] + "", Params["REMARK"] + "", "已审核"));
      MAIN.setState(flowCode);
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
      saveList.Add(this.addOperateLogs(Params["ID"] + "", "", "撤销审核"));
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
           saveList.Add(this.addOperateLogs(MAIN.GetValue("ID") + "","受理", "质检中"));
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

    protected override BaseModel doGetBatchMain(MOUDLE MD, ViewRow row, Hashtable Params)
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

    protected virtual void doUpdateTemplate(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      if (MAIN.GetView().Count != (Params["ID"] + "").Split(',').Length)
      {
        responseModel.SetError("数据发生变更，刷新后再试！");
        return;
      }
      ArrayList saveList = new ArrayList();
      MAIN.GetView().ForEach((ViewRow row1) =>
      {
        saveList.Add(@"UPDATE tck_orecord T SET T.TPMDATA=(SELECT TT.TPMDATA FROM tss_template TT WHERE TT.ID =( SELECT TTT.REFTPMID FROM TCK_PTEMPLATE TTT WHERE TTT.ID= T.PTEMPLATEID )) WHERE T.ID IN('"+row1["ID"]+"');");
      });
      this.operate01.Save(saveList);
      responseModel.SetData(saveList);
    }

    protected virtual void doGenCert(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      //1.查询生成证书所需数据
      string MAINPATH = "MAIN";
      string DTSAPATH = "DTSA";
      string DTSBPATH = "DTSB";
      ViewRow pathRow1 = MD.GetPath(MAINPATH);
      ViewRow pathRow2 = MD.GetPath(DTSAPATH);
      ViewRow pathRow3 = MD.GetPath(DTSBPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow1.GetString("RESOURCEID"));
      BaseModel DTSA = GetModel(DTSAPATH, pathRow2.GetString("RESOURCEID"));
      BaseModel DTSB = GetModel(DTSBPATH, pathRow3.GetString("RESOURCEID"));
      QueryInfo queryInfo1 = new QueryInfo();
      queryInfo1.FilterCode = "F00";
      queryInfo1.FilterParams["ID"] = Params["ID"];
      MAIN.OpenByID(Params["ID"] + "");
      DTSA.Open(queryInfo1);
      DTSB.Open(queryInfo1);
      Dictionary<string, object> list = new Dictionary<string, object>();
      List<ViewColumn> columns = MAIN.GetView().Columns;
      columns.ForEach(column =>
      {
        list[column.Name] = MAIN.GetValue(column.Name);
        if(column.Name=="SIGNDATE"&&(list[column.Name]+""=="")){
           list[column.Name] =  DateTime.Now.ToString("yyyy-MM-dd");
        }
      });

      DTSB.GetView().ForEach(viewRow =>
      {
        if (!list.ContainsKey(viewRow.GetString("FIELDNAME")))
          list[viewRow.GetString("FIELDNAME")] = viewRow.GetValue("FIELDVALUE");
      });
      string[] fileds = { "ARDNAME", "SIZETYPE", "OMCODE", "DEGREE", "EXPDATE", "CERTCODE","MANUFACTURER","CORGNAME" };
      List<Dictionary<string, object>> tlist = new List<Dictionary<string, object>>();
      DTSA.GetView().ForEach(ViewRow =>
      {
        Dictionary<string, object> tdic = new Dictionary<string, object>();
        foreach (var f in fileds)
        {
          // Bookmark 模版兼容：key 带 T 前缀（如 TARDNAME）
          tdic["T" + f] = ViewRow[f];
          // SDT 模版兼容：key 不带 T 前缀（如 ARDNAME），与字段 Tag 一致
          tdic[f] = ViewRow[f];
        }
        tlist.Add(tdic);
      });
      foreach (var f in fileds)
      {
        list["T" + f] = tlist;
      }
      // 兼容 SDT 模版：子表循环标记使用资源名（如 VCK_ORECORDDTSA_TABLE），
      // 替换引擎按 baseName 查找子表数据
      string dtsaResourceName = DTSA.GetView().Resource.RESOURCENAME;
      if (!string.IsNullOrEmpty(dtsaResourceName) && !list.ContainsKey(dtsaResourceName))
      {
        list[dtsaResourceName] = tlist;
      }

      string CERTEMPID = MAIN.GetValue("CERTEMPID");
      FILE file = new FILE(this.operate01);
      string filePath = file.GetFilePath(CERTEMPID);
      if (filePath == "")
      {
        this.responseModel.SetError("未找到证书模版");
        return;
      }

      // 提交人
      string SUBMITESIGNID = MAIN.GetValue("SUBMITESIGNID");
      string path1 = file.GetFilePath(SUBMITESIGNID);
      //CREATER_IMG
      if (path1 != "")
        list["CREATER_IMG"] = path1;
      // 审核人
      string CHECKESIGNID = MAIN.GetValue("CHECKESIGNID");
      string path2 = file.GetFilePath(CHECKESIGNID);
      if (path2 != "")
        list["CHECKER_IMG"] = path2;
      // 审批人
      string VERIFYESIGNID = MAIN.GetValue("VERIFYESIGNID");
      string path3 = file.GetFilePath(VERIFYESIGNID);
      if (path3 != "")
        list["VERIFIER_IMG"] = path3;

      //设置二维码的图片位置
      string url = getQRFilePath(2,"验证二维码","&ID="+Params["ID"]);
      list["CHECKQR"] =  url;
      list["CHECKQR_IMG2"] =  url;
      //导出证书
      string copyDoc = filePath + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
      Realso.Utils.Logger.Info("证书地址==>" + filePath);
      WordHelper.ReplaceFromTemplate(WordHelper.CopyWord(filePath, copyDoc), list);
      Hashtable saveFile = new Hashtable();
      saveFile["UPLOADFILEPATH"] = copyDoc;
      saveFile["UPLOADTYPE"] = "证书";
      saveFile["FILENAME"] = MAIN.GetValue("CERTCODE") + ".docx";
      saveFile["SYNC"] ="1";
      file.SaveFile(saveFile);

      //导出原始记录
      string EXPTEMPID = MAIN.GetValue("EXPTEMPID");
      FILE file2 = new FILE(this.operate01);
      string filePath2 = file2.GetFilePath(EXPTEMPID);
      if (filePath2 != "")
      {
        string copyDoc2 = filePath2 + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
        Realso.Utils.Logger.Info("证书地址==>" + filePath2);
        WordHelper.ReplaceFromTemplate(WordHelper.CopyWord(filePath2, copyDoc2), list);
        Realso.Utils.Logger.Info("制作完成==>" + filePath2);
        Hashtable saveFile2 = new Hashtable();
        saveFile2["UPLOADFILEPATH"] = copyDoc2;
        saveFile2["UPLOADTYPE"] = "证书";
        saveFile2["FILENAME"] = MAIN.GetValue("CERTCODE") + "原始记录.docx";
        saveFile2["SYNC"] ="1";
        file2.SaveFile(saveFile2);
        MAIN.SetValue("EXPFILEID", file2.GetValue("ID"));
      }
      MAIN.SetValue("STATE", BillState.已制证);
      MAIN.SetValue("CERTID", file.GetValue("ID"));

      if(MAIN.GetValue("SIGNDATE")+""==""){
          MAIN.SetValue("SIGNDATE", DateTime.Now.ToString("yyyy-MM-dd"));
      }

      ArrayList saveList = new ArrayList();
      saveList.Add(file.GetView());
      saveList.Add(file2.GetView());
      saveList.Add(MAIN.GetView());
      saveList.Add(@"UPDATE tck_accept SET STATE=10,CERTID='" + file.GetValue("ID") + "'  WHERE ID = '" + MAIN.GetValue("REFBILLID") + "';");
      saveList.Add(this.addOperateLogs(MAIN.GetValue("REFBILLID") + "","受理", "已签发"));
      if (Params["ISBATCH"] + "" == "1")
      {
        this.operate01.Save(saveList);
      }
      else
      {
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      }

    }


    private string getQRFilePath(int size,string pathType,string urlParm){
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Url:验证二维码");
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

    public override void addLog(string FUNCNAME, string FUNCPOINT, ArrayList saveList, string errorMessage)
    {
      base.addLog(FUNCNAME, FUNCPOINT, saveList, errorMessage);
      if (FUNCPOINT == "保存数据【成功】" || FUNCPOINT == "提交【成功】")
      {
        DataView logView = new DataView(this.operate01.GetResource("VSS_LOG"));
        ViewRow row = new ViewRow(logView);
        row["LOGIP"] = context.HttpContext?.Connection.RemoteIpAddress.ToString();
        row["LOGTYPE"] = 2;
        row["CREATER"] = this.userInfo["NICKNAME"] + "";
        row["CREATEID"] = this.userInfo["ID"] + "";
        row["CREATEDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        row["FUNCNAME"] = FUNCNAME;
        row["FUNCPOINT"] = "原始记录变更记录";
        logView.AddRow(row);
        ArrayList logList = new ArrayList();
        string ORECORDID = "";
        foreach (var tobj in saveList)
        {
          DataView view = null;
          if (tobj is DataView)
          {
            view = tobj as DataView;
            if (view.Resource.RESOURCENAME == "VCK_ORECORDDTSB")
            {
              view.Inserted.ForEach((ViewRow r) =>
                      {
                        Hashtable val = new Hashtable();
                        val["类型"] = "新增";
                        val["字段"] = r.GetString("FIELDNAME");
                        val["字段名称"] = r.GetString("FIELDREMARK");
                        val["原始值"] = "";
                        val["当前值"] = r.GetString("FIELDVALUE");
                        ORECORDID = r.GetString("ORECORDID");
                        if (r.GetString("FIELDVALUE") != "")
                          logList.Add(val);
                      });
              view.Updated.ForEach((ViewRow r) =>
              {
                Hashtable val = new Hashtable();
                val["类型"] = "更新";
                val["字段"] = r.GetString("FIELDNAME");
                val["字段名称"] = r.GetString("FIELDREMARK");
                val["原始值"] = r.GetOldString("FIELDVALUE");
                val["当前值"] = r.GetString("FIELDVALUE");
                if (r.GetOldString("FIELDVALUE") != r.GetString("FIELDVALUE"))
                  logList.Add(val);
                ORECORDID = r.GetString("ORECORDID");
              });
            }
          }
        }
        row["REFID"] = ORECORDID;
        row["LOGDATA"] = Newtonsoft.Json.JsonConvert.SerializeObject(logList);
        this.operate01.FillKey(logView);
        ArrayList saveList2 = new ArrayList();
        saveList2.Add(logView);
        this.operate01.Save(saveList2);
      }
    }

    /// <summary>
    /// 字段手动修改（需求4）- 支持证书编号、校准日期、审核日期、审批日期修改，通过addLog记录变更
    /// </summary>
    protected virtual void doFieldModify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string ID = Params["ID"] + "";
      string FIELD_NAME = Params["FIELD_NAME"] + "";
      string FIELD_VALUE = Params["FIELD_VALUE"] + "";

      if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(FIELD_NAME))
      {
        responseModel.SetError("参数不完整");
        return;
      }

      string[] allowedFields = { "CERTCODE", "BILLDATE", "CHECKTIME", "VERIFYTIME" };
      if (Array.IndexOf(allowedFields, FIELD_NAME) == -1)
      {
        responseModel.SetError("不允许修改该字段：" + FIELD_NAME);
        return;
      }

      ViewRow pathRow = MD.GetPath(MAINPATH);
      M01 MAIN = new M01(this.operate01, pathRow.GetString("RESOURCEID"));
      MAIN.OpenByID(ID);

      if (MAIN.GetView().Count == 0)
      {
        responseModel.SetError("记录不存在");
        return;
      }

      string oldValue = MAIN.GetValue(FIELD_NAME) + "";
      MAIN.SetValue(FIELD_NAME, FIELD_VALUE);

      ArrayList saveList = new ArrayList();
      saveList.Add(MAIN.GetView());
      try
      {
        operate01.Save(saveList);
        this.addLog(
          MD.GetView()[0].GetString("MODULENAME"),
          "字段修改：" + FIELD_NAME + " 由 [" + oldValue + "] 改为 [" + FIELD_VALUE + "]",
          saveList, ""
        );
        responseModel.SetData(MAIN.GetView());
      }
      catch (Exception ex)
      {
        this.addLog(MD.GetView()[0].GetString("MODULENAME"), "字段修改【失败】", saveList, ex.Message);
        throw;
      }
    }

    /// <summary>
    /// 异常检测（需求7）- 检测标准器冲突、人员冲突、委托超期、基础信息完整性、环境条件、方法合规性、数据超差
    /// </summary>
    protected virtual void doCheckAnomaly(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      ArrayList anomalies = new ArrayList();
      string ID = Params["ID"] + "";

      // 标准器查重：同一标准器在同一时间段被多条记录使用
      try
      {
        string sql1 = SQLManage.GetSQL("CHECK_ARD_CONFLICT");
        if (!string.IsNullOrEmpty(sql1))
        {
          Hashtable p1 = new Hashtable();
          p1["ID"] = ID;
          var ardConflicts = this.operate01.Query(sql1, p1);
          if (ardConflicts != null)
          {
            foreach (IDictionary<string, object> item in ardConflicts)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "ard_conflict";
              anomaly["level"] = "warning";
              anomaly["message"] = "标准器 " + item["ARDNAME"] + " 在 " + item["CALIBDATE"] + " 被多条记录使用";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { /* SQL未配置则跳过 */ }

      // 人员查重：同一校准人员同一时间段并行记录
      try
      {
        string sql2 = SQLManage.GetSQL("CHECK_EMP_CONFLICT");
        if (!string.IsNullOrEmpty(sql2))
        {
          Hashtable p2 = new Hashtable();
          p2["ID"] = ID;
          var empConflicts = this.operate01.Query(sql2, p2);
          if (empConflicts != null)
          {
            foreach (IDictionary<string, object> item in empConflicts)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "emp_conflict";
              anomaly["level"] = "warning";
              anomaly["message"] = "校准人员 " + item["EMPNAME"] + " 在 " + item["CALIBDATE"] + " 有 " + item["CNT"] + " 条并行记录";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 委托超期检测
      try
      {
        string sql3 = SQLManage.GetSQL("CHECK_WT_TIMEOUT");
        if (!string.IsNullOrEmpty(sql3))
        {
          Hashtable p3 = new Hashtable();
          p3["ID"] = ID;
          var timeoutItems = this.operate01.Query(sql3, p3);
          if (timeoutItems != null)
          {
            foreach (IDictionary<string, object> item in timeoutItems)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "wt_timeout";
              anomaly["level"] = "error";
              anomaly["message"] = "委托单 " + item["WTCODE"] + " 已超期 " + item["DAYS"] + " 天";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 基础信息完整性检测
      try
      {
        string sql4 = SQLManage.GetSQL("CHECK_BASIC_INFO");
        if (!string.IsNullOrEmpty(sql4))
        {
          Hashtable p4 = new Hashtable();
          p4["ID"] = ID;
          var basicInfoIssues = this.operate01.Query(sql4, p4);
          if (basicInfoIssues != null)
          {
            foreach (IDictionary<string, object> item in basicInfoIssues)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "basic_info";
              anomaly["level"] = "warning";
              anomaly["message"] = "基础信息不完整：" + item["FIELDNAME"] + " 未填写";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 环境条件范围检测（温湿度/气压是否在合理范围）
      try
      {
        string sql5 = SQLManage.GetSQL("CHECK_ENV_CONDITION");
        if (!string.IsNullOrEmpty(sql5))
        {
          Hashtable p5 = new Hashtable();
          p5["ID"] = ID;
          var envIssues = this.operate01.Query(sql5, p5);
          if (envIssues != null)
          {
            foreach (IDictionary<string, object> item in envIssues)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "env_condition";
              anomaly["level"] = "warning";
              anomaly["message"] = "环境条件异常：" + item["FIELDNAME"] + " = " + item["VALUE"] + "，超出合理范围";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 方法合规性检测（规程是否现行有效）
      try
      {
        string sql6 = SQLManage.GetSQL("CHECK_METHOD_COMPLIANCE");
        if (!string.IsNullOrEmpty(sql6))
        {
          Hashtable p6 = new Hashtable();
          p6["ID"] = ID;
          var complianceIssues = this.operate01.Query(sql6, p6);
          if (complianceIssues != null)
          {
            foreach (IDictionary<string, object> item in complianceIssues)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "method_compliance";
              anomaly["level"] = "warning";
              anomaly["message"] = "方法合规性：" + item["STANDARDNAME"] + " 可能已过期或无效";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 数据超差检测
      try
      {
        string sql7 = SQLManage.GetSQL("CHECK_DATA_OVERRANGE");
        if (!string.IsNullOrEmpty(sql7))
        {
          Hashtable p7 = new Hashtable();
          p7["ID"] = ID;
          var overrangeItems = this.operate01.Query(sql7, p7);
          if (overrangeItems != null)
          {
            foreach (IDictionary<string, object> item in overrangeItems)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "data_overrange";
              anomaly["level"] = "error";
              anomaly["message"] = "数据超差：" + item["FIELDNAME"] + " = " + item["VALUE"] + "，超出允许范围 " + item["ALLOWRANGE"];
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      // 地域冲突检测（同一标准器/人员同一天在不同地点）
      try
      {
        string sql8 = SQLManage.GetSQL("CHECK_LOCATION_CONFLICT");
        if (!string.IsNullOrEmpty(sql8))
        {
          Hashtable p8 = new Hashtable();
          p8["ID"] = ID;
          var locationConflicts = this.operate01.Query(sql8, p8);
          if (locationConflicts != null)
          {
            foreach (IDictionary<string, object> item in locationConflicts)
            {
              Hashtable anomaly = new Hashtable();
              anomaly["type"] = "location_conflict";
              anomaly["level"] = "warning";
              anomaly["message"] = item["TYPE"] + " " + item["NAME"] + " 在 " + item["CALIBDATE"] + " 存在地域冲突";
              anomalies.Add(anomaly);
            }
          }
        }
      }
      catch (Exception) { }

      responseModel.SetData(anomalies);
    }

    /// <summary>
    /// 委托单维度查询（需求8）
    /// </summary>
    protected virtual void doQueryWTList(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      string MAINRESOURCEID = row.GetString("RESOURCEID");
      Params["FILTERCODE"] = row["FILTERCODE"];
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      QueryInfo queryInfo = GetQueryInfo(Params);
      QueryResult result = MAIN.Query(queryInfo);
      responseModel.SetData(result);
    }

    /// <summary>
    /// 委托单明细查询（需求8）
    /// </summary>
    protected virtual void doQueryWTDetail(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      QueryInfo queryInfo = GetQueryInfo(Params);
      QueryResult result = MAIN.Query(queryInfo);
      responseModel.SetData(result);
    }

    /// <summary>
    /// 电子证书签发（需求10）- 电子章标记 + 防伪二维码 + 更新签发状态
    /// 前端需先调用 A21 生成证书，再调用 A55 做电子签发
    /// 支持单条和批量（ID逗号分隔）
    /// </summary>
    protected virtual void doECertSign(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string ID = Params["ID"] + "";
      if (string.IsNullOrEmpty(ID))
      {
        responseModel.SetError("参数不完整");
        return;
      }

      // 批量模式：ID包含逗号
      if (ID.Contains(","))
      {
        doBatchECertSign(MD, row, Params);
        return;
      }

      // 查询记录信息
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      M01 MAIN = new M01(this.operate01, pathRow.GetString("RESOURCEID"));
      MAIN.OpenByID(ID);

      if (MAIN.GetView().Count == 0)
      {
        responseModel.SetError("记录不存在");
        return;
      }

      // 检查是否已制证（STATE=10）
      if (MAIN.GetValue("STATE") + "" != BillState.已制证 + "")
      {
        responseModel.SetError("请先生成证书后再进行电子签发！");
        return;
      }

      // 检查是否已电子签发
      if (MAIN.GetValue("ECERTSIGN") + "" == "1")
      {
        responseModel.SetError("该证书已电子签发，无需重复操作！");
        return;
      }

      // 1. 生成防伪验证二维码
      string verifyUrl = Realso.Utils.ConfigHelper.GetConfig("Url:电子证书验证") ?? "/out/ecert/";
      string qrImagePath = getQRFilePath(4, "电子证书验证", "&ID=" + ID);

      // 2. 处理查看密码
      string ECERTPWD = Params["ECERTPWD"] + "";
      string hashedPwd = "";
      if (!string.IsNullOrEmpty(ECERTPWD))
      {
        hashedPwd = PasswordHelper.HashPassword(ECERTPWD);
      }

      // 3. 更新电子签发标记
      ArrayList saveList = new ArrayList();
      MAIN.SetValue("ECERTSIGN", 1);
      MAIN.SetValue("ECERTSIGNDATE", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
      MAIN.SetValue("ECERTSIGNER", this.userInfo["NICKNAME"] + "");
      if (!string.IsNullOrEmpty(hashedPwd))
      {
        MAIN.SetValue("ECERTPWD", hashedPwd);
      }
      saveList.Add(MAIN.GetView());

      // 4. 更新受理单电子签发标记
      string REFBILLID = MAIN.GetValue("REFBILLID") + "";
      if (!string.IsNullOrEmpty(REFBILLID))
      {
        saveList.Add(@"UPDATE tck_accept SET ECERTSIGN=1, ECERTSIGNDATE='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE ID = '" + REFBILLID + "';");
      }

      // 5. 记录签发操作日志
      saveList.Add(this.addOperateLogs(ID, "电子签发", "电子证书签发"));

      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);

      // 6. 返回电子证书信息
      Hashtable ecertInfo = new Hashtable();
      ecertInfo["ID"] = ID;
      ecertInfo["CERTCODE"] = MAIN.GetValue("CERTCODE");
      ecertInfo["ECERTSIGN"] = 1;
      ecertInfo["ECERTSIGNDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      ecertInfo["ECERTSIGNER"] = this.userInfo["NICKNAME"] + "";
      ecertInfo["VERIFY_URL"] = verifyUrl + "?ID=" + ID;
      ecertInfo["QR_IMAGE"] = qrImagePath;
      responseModel.SetData(ecertInfo);
    }

    /// <summary>
    /// 批量电子证书签发
    /// </summary>
    protected virtual void doBatchECertSign(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
      ArrayList saveList = new ArrayList();

      // 处理查看密码
      string ECERTPWD = Params["ECERTPWD"] + "";
      string hashedPwd = "";
      if (!string.IsNullOrEmpty(ECERTPWD))
      {
        hashedPwd = PasswordHelper.HashPassword(ECERTPWD);
      }

      if (MAIN != null)
      {
        MAIN.GetView().ForEach((ViewRow row1) =>
        {
          // 检查是否已制证
          if (row1["STATE"] + "" != BillState.已制证 + "")
          {
            return;
          }
          // 检查是否已电子签发
          if (row1["ECERTSIGN"] + "" == "1")
          {
            return;
          }
          row1["ECERTSIGN"] = 1;
          row1["ECERTSIGNDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
          row1["ECERTSIGNER"] = this.userInfo["NICKNAME"] + "";
          if (!string.IsNullOrEmpty(hashedPwd))
          {
            row1["ECERTPWD"] = hashedPwd;
          }

          // 更新受理单电子签发标记
          string REFBILLID = row1["REFBILLID"] + "";
          if (!string.IsNullOrEmpty(REFBILLID))
          {
            saveList.Add(@"UPDATE tck_accept SET ECERTSIGN=1, ECERTSIGNDATE='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' WHERE ID = '" + REFBILLID + "';");
          }

          // 记录操作日志
          saveList.Add(this.addOperateLogs(row1["ID"] + "", "电子签发", "电子证书签发"));
        });
        saveList.Add(MAIN.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(MAIN.GetView());
      }
    }

    /// <summary>
    /// 电子证书验证查询（需求10）- 通过证书ID查询验证信息
    /// </summary>
    protected virtual void doECertVerify(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string ID = Params["ID"] + "";
      if (string.IsNullOrEmpty(ID))
      {
        responseModel.SetError("参数不完整");
        return;
      }

      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      BaseModel MAIN = GetModel(MAINPATH, pathRow.GetString("RESOURCEID"));
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = "F00";
      queryInfo.FilterParams["ID"] = ID;
      MAIN.Open(queryInfo);

      if (MAIN.GetView().Count == 0)
      {
        responseModel.SetError("证书不存在");
        return;
      }

      // 返回验证信息（不含敏感数据）
      Hashtable verifyInfo = new Hashtable();
      verifyInfo["CERTCODE"] = MAIN.GetValue("CERTCODE");
      verifyInfo["MNAME"] = MAIN.GetValue("MNAME");
      verifyInfo["SIZETYPE"] = MAIN.GetValue("SIZETYPE");
      verifyInfo["OPCODE"] = MAIN.GetValue("OPCODE");
      verifyInfo["CUSTNAME"] = MAIN.GetValue("CUSTNAME");
      verifyInfo["SIGNDATE"] = MAIN.GetValue("SIGNDATE");
      verifyInfo["ECERTSIGN"] = MAIN.GetValue("ECERTSIGN");
      verifyInfo["ECERTSIGNDATE"] = MAIN.GetValue("ECERTSIGNDATE");
      verifyInfo["STATE"] = MAIN.GetValue("STATE");
      responseModel.SetData(verifyInfo);
    }

    /// <summary>
    /// 修改/清除电子证书查看密码（A58）
    /// ECERTPWD为空时清除密码
    /// 支持单条和批量（ID逗号分隔）
    /// </summary>
    protected virtual void doUpdateECertPwd(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string ID = Params["ID"] + "";
      if (string.IsNullOrEmpty(ID))
      {
        responseModel.SetError("参数不完整");
        return;
      }

      string ECERTPWD = Params["ECERTPWD"] + "";
      string hashedPwd = "";
      if (!string.IsNullOrEmpty(ECERTPWD))
      {
        hashedPwd = PasswordHelper.HashPassword(ECERTPWD);
      }

      // 批量模式：ID包含逗号
      if (ID.Contains(","))
      {
        BaseModel MAIN = this.doGetBatchMain(MD, row, Params);
        ArrayList saveList = new ArrayList();
        if (MAIN != null)
        {
          MAIN.GetView().ForEach((ViewRow row1) =>
          {
            if (row1["ECERTSIGN"] + "" != "1") return;
            if (!string.IsNullOrEmpty(hashedPwd))
            {
              row1["ECERTPWD"] = hashedPwd;
            }
            else
            {
              row1["ECERTPWD"] = DBNull.Value;
            }
          });
          saveList.Add(MAIN.GetView());
          this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
          responseModel.SetData(MAIN.GetView());
        }
        return;
      }

      // 单条模式
      string MAINPATH = row.GetString("PATHNAME");
      ViewRow pathRow = MD.GetPath(MAINPATH);
      M01 mainRec = new M01(this.operate01, pathRow.GetString("RESOURCEID"));
      mainRec.OpenByID(ID);

      if (mainRec.GetView().Count == 0)
      {
        responseModel.SetError("记录不存在");
        return;
      }

      if (!string.IsNullOrEmpty(hashedPwd))
      {
        mainRec.SetValue("ECERTPWD", hashedPwd);
      }
      else
      {
        mainRec.SetValue("ECERTPWD", DBNull.Value);
      }

      ArrayList saveList2 = new ArrayList();
      saveList2.Add(mainRec.GetView());
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList2);

      Hashtable result = new Hashtable();
      result["ID"] = ID;
      result["ECERTPWD"] = !string.IsNullOrEmpty(ECERTPWD) ? "已设置" : "已清除";
      responseModel.SetData(result);
    }
  }
}
