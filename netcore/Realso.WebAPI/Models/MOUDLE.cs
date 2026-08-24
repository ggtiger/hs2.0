using System.Collections;
using System;
using Realso.Data.ORM;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Realso.Core.Base;
using Realso.Data.ORM.Core;

namespace Realso.WebAPI.Models
{
  public class MOUDLE : BaseModel
  {
    public BaseModel PATH;
    public BaseModel PATHREL;
    public BaseModel API;
    public MOUDLE(IViewOperate operate) : base(operate, "VSS_MOUDLE")
    {
      PATH = new BaseModel(operate, "VSS_MOUDLEPATH");
      PATHREL = new BaseModel(operate, "VSS_MOUDLEPATHREL");
      API = new BaseModel(operate, "VSS_MOUDLEAPI");
    }

    public void Open(string ID)
    {
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = "F00";
      queryInfo.FilterParams["MODULECODE"] = ID;
      base.Open(queryInfo);
      if (this.GetView().Count == 0)
      {
        throw new Exception("模块编号不存在！");
      }
      string MODULEID = this.GetValue("ID");
      QueryInfo queryInfo2 = new QueryInfo();
      queryInfo2.FilterCode = "F00";
      queryInfo2.FilterParams["MODULEID"] = MODULEID;
      this.PATH.Open(queryInfo2);
      this.PATHREL.Open(queryInfo2);
      this.API.Open(queryInfo2);
    }

    public ViewRow GetPath(String Path)
    {
      return this.PATH.GetView().Find((ViewRow row) =>
      {
        return row["PATHNAME"] + "" == Path;
      });
    }

    public IList<ViewRow> GetPathRel(String Path)
    {
      return this.PATHREL.GetView().FindAll((ViewRow row) =>
      {
        return row["PATHNAMEA"] + "" == Path;
      });
    }

    public ViewRow GetPathByRel(String Path)
    {
      return this.PATHREL.GetView().Find((ViewRow row) =>
      {
        return row["PATHNAMEB"] + "" == Path;
      });
    }

    public ViewRow GetAPI(String APICODE)
    {
      return this.API.GetView().Find((ViewRow row) =>
      {
        return row["APICODE"] + "" == APICODE;
      });
    }

  }
}
