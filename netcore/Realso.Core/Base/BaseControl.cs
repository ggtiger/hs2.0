using System.Collections;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Realso.Core.Models;
using System.Collections.Generic;
using Realso.Data.ORM;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Realso.Data.ORM.Core;

namespace Realso.Core.Base
{
  //路由设计
  //控制器/数据模型/操作
  public class BaseControl : ControllerBase
  {
    //属性：
    //请求实体
    protected IViewOperate operate01 = new ViewOperate01();
    private RequestModel requestModel = new RequestModel();
    //返回实体
    protected ResponseModel responseModel = new ResponseModel();

    [BindProperty(Name = "_userInfo_")]
    public Hashtable userInfo { get; set; }
    protected HttpContextAccessor context = new HttpContextAccessor();

    //方法：
    //_处理请求
    private void doRequest()
    {
    }
    //_处理返回
    protected IActionResult doResponse()
    {
      return new JsonResult(this.responseModel);
    }

    protected QueryInfo GetQueryInfo(Hashtable Params)
    {
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = Params["FILTERCODE"] + "";
      queryInfo.FilterParams = Params["FilterParams"] as Hashtable;
      if (Params["PageSize"] + "" != "")
      {
        queryInfo.PageSize = int.Parse(Params["PageSize"] + "");
      }
      if (Params["PageIndex"] + "" != "")
      {
        queryInfo.PageIndex = int.Parse(Params["PageIndex"] + "");
      }
      queryInfo.OrderBy = Params["OrderBy"] + "";
      queryInfo.SumFields = Params["sumFields"] + "";
      return queryInfo;
    }

    public virtual BaseModel GetModel(string Path, string RESOURCEID)
    {
      return new BaseModel(this.operate01, RESOURCEID);
    }

    public virtual void setSaveInfo(DataView view)
    {
      if (view.Resource.Fields.Find((ResourceField f) =>
      {
        return f.FIELDNAME == "CREATER";
      }) == null)
      {
        return;
      }
      ViewRow row = view[0];
      if (row.GetString("CREATETIME") == "")
      {
        view[0]["CREATEID"] = this.userInfo["ID"] + "";
        view[0]["CREATER"] = this.userInfo["NICKNAME"] + "";
        view[0]["CREATETIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      }
      else
      {
        view[0]["MODIFYID"] = this.userInfo["ID"] + "";
        view[0]["MODIFER"] = this.userInfo["NICKNAME"] + "";
        view[0]["MODIFYTIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      }

    }

    public virtual void Save(string MODULENAME, string APINAME, ArrayList saveList)
    {
      try
      {
        if (APINAME.IndexOf(':') != -1)
        {
          MODULENAME = APINAME.Split(':')[0];
          APINAME = APINAME.Split(':')[1];
        }
        operate01.Save(saveList);
        this.addLog(MODULENAME, APINAME + "【成功】", saveList, "");
      }
      catch (Exception ex)
      {
        this.addLog(MODULENAME, APINAME + "【失败】", saveList, ex.Message + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void addLog(string FUNCNAME, string FUNCPOINT, ArrayList saveList, string errorMessage)
    {
      if (FUNCPOINT.IndexOf(':') != -1)
      {
        FUNCNAME = FUNCPOINT.Split(':')[0];
        FUNCPOINT = FUNCPOINT.Split(':')[1];
      }
      //DataView view = model.GetView();
      DataView logView = new DataView(this.operate01.GetResource("VSS_LOG"));
      ViewRow row = new ViewRow(logView);
      row["LOGIP"] = context.HttpContext?.Connection.RemoteIpAddress.ToString();
      row["LOGTYPE"] = 1;
      row["CREATER"] = this.userInfo["NICKNAME"] + "";
      row["CREATEID"] = this.userInfo["ID"] + "";
      row["CREATEDATE"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      row["FUNCNAME"] = FUNCNAME;
      row["FUNCPOINT"] = FUNCPOINT;
      //row["FUNCNAME"] = resouce.COMMENTS;
      string logData = errorMessage;
      logView.AddRow(row);
      foreach (var tobj in saveList)
      {
        DataView view = null;
        if (tobj is DataView)
        {
          view = tobj as DataView;
        }
        else
        {
          continue;
        }
        Resource resouce = view.Resource;
        view.Inserted.ForEach((ViewRow r) =>
        {
          logData += " [新增] ";
          resouce.Fields.ForEach((ResourceField f) =>
                {
                  logData += f.COMMENTS + ":" + r[f.FIELDNAME] + ";";
                });
        });
        view.Updated.ForEach((ViewRow r) =>
        {
          logData += " [修改] ";
          resouce.Fields.ForEach((ResourceField f) =>
                {
                  logData += f.COMMENTS + ":(" + r[f.FIELDNAME]+"->"+r.GetOldString(f.FIELDNAME) + ");";
                });
        });
        view.Deleted.ForEach((ViewRow r) =>
        {
          logData += " [删除] ";
          resouce.Fields.ForEach((ResourceField f) =>
                {
                  logData += f.COMMENTS + ":" + r[f.FIELDNAME] + ";";
                });
        });
      }
      row["LOGDATA"] = logData;
      this.operate01.FillKey(logView);
      ArrayList saveList2 = new ArrayList();
      saveList2.Add(logView);
      this.operate01.Save(saveList2);
    }
  }
}
