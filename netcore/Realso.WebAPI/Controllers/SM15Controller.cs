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
  public class SM15Controller : DataController
  {
    protected readonly IHostingEnvironment _hostingEnvironment;

    public SM15Controller(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row.GetString("APICODE");
      switch (APICODE)
      {
        case "A12"://重置密码
          this.doResetPass(MD, row, Params);
          break;
        case "A13"://修改密码
          this.doSetPass(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口类型:" + APITYPE + "不存在！");
          break;
      }
    }

    private void doResetPass(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 m01 = new M01(this.operate01, "VSS_USER");
      m01.OpenByID(Params["ID"] + "");
      m01.SetValue("PASSWORD", PasswordHelper.HashPassword("888888"));
      ArrayList saveList = new ArrayList();
      saveList.Add(m01.GetView());
      this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
      responseModel.SetData(true);
    }

    private void doSetPass(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      M01 m01 = new M01(this.operate01, "VSS_USER");
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = "F02";
      queryInfo.FilterParams["USERNAME"] = Params["USERNAME"];
      m01.Open(queryInfo);
      bool pwdOk = false;
      if (m01.GetView().Count > 0)
      {
        // 哈希存储走 VerifyPassword；存量明文兼容比对
        string stored = m01.GetValue("PASSWORD");
        string opwd = Params["OPASSWORD"] + "";
        pwdOk = !string.IsNullOrEmpty(stored) && stored.Contains("$")
          ? PasswordHelper.VerifyPassword(opwd, stored)
          : stored == opwd;
      }
      if (!pwdOk)
      {
        responseModel.SetError("原密码不正确!");
      }
      else
      {
        m01.SetValue("PASSWORD", PasswordHelper.HashPassword(Params["PASSWORD"] + ""));
        ArrayList saveList = new ArrayList();
        saveList.Add(m01.GetView());
        this.Save(MD.GetView()[0].GetString("MODULENAME"), row.GetString("APINAME"), saveList);
        responseModel.SetData(true);
      }
    }
  }
}
