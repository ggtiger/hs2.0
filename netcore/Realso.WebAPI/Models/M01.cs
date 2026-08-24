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
  // 目前只支持三级审批
  public class BillState
  {
    public static readonly int 待提交 = 1;
    public static readonly int 待审核 = 2;
    public static readonly int 已审核 = 3;
    public static readonly int 已作废 = 4;
    public static readonly int 待审批 = 5;
    public static readonly int 已审批 = 6;
    public static readonly int 待接收 = 7;
    public static readonly int 已驳回 = 12;
    public static readonly int 已制证 = 10;
    public static readonly int 已打印 = 11;
    public static readonly int 待发布 = 19;
    public static readonly int 已发布 = 20;

  }

  public class BillFlow
  {
    public static readonly int 提交_审核 = 1;
    public static readonly int 提交_审核_审批 = 2;
    public static readonly int 提交_审核_审批_作废 = 3;
    public static readonly int 提交_检验 = 4;
    public static readonly int 提交_审核_发布 = 5;
  }
  public class M01 : BaseModel
  {
    public M01(IViewOperate operate, string mname) : base(operate, mname)
    {
    }
    public M01(IViewOperate operate, DataView view) : base(operate, view)
    {
    }

    public virtual void setMakeInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("CREATID", userInfo["ID"], i);
        this.SetValue("CREATER", userInfo["NICKNAME"], i);
        this.SetValue("CREATETIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setModifyInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("MODIFYID", userInfo["ID"], i);
        this.SetValue("MODIFIER", userInfo["NICKNAME"], i);
        this.SetValue("MODIFYTIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setSubmitInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("SUBMITID", userInfo["ID"], i);
        this.SetValue("SUBMITER", userInfo["NICKNAME"], i);
        this.SetValue("SUMBMITTIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setReSubmitInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("CHECKID", "", i);
        this.SetValue("CHECKER", "", i);
        this.SetValue("SUBMITID", "", i);
        this.SetValue("SUBMITER", "", i);
        this.SetValue("SUMBMITTIME", "", i);
      }
    }

    public virtual void setCompleteInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("COMPLETEID", userInfo["ID"], i);
        this.SetValue("COMPLETER", userInfo["NICKNAME"], i);
        this.SetValue("COMPLETETIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setReCompleteInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("COMPLETEID", "", i);
        this.SetValue("COMPLETER", "", i);
        this.SetValue("COMPLETETIME", "", i);
      }
    }

    public virtual void setCheckInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("CHECKID", userInfo["ID"], i);
        this.SetValue("CHECKER", userInfo["NICKNAME"], i);
        this.SetValue("CHECKTIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setReCheckInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("VERIFYID", "", i);
        this.SetValue("VERIFIER", "", i);
        //this.SetValue("CHECKID", "", i);
        //this.SetValue("CHECKER", "", i);
        this.SetValue("CHECKTIME", "", i);
      }
    }

    public virtual void setVerifyInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("VERIFYID", userInfo["ID"], i);
        this.SetValue("VERIFIER", userInfo["NICKNAME"], i);
        this.SetValue("VERIFYTIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setReVerifyInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        //this.SetValue("VERIFYID", "", i);
        //this.SetValue("VERIFIER", "", i);
        this.SetValue("VERIFYTIME", "", i);
      }
    }

    public virtual void setInvalidInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("INVALIDID", userInfo["NICKNAME"], i);
        this.SetValue("INVALIDER", userInfo["NICKNAME"], i);
        this.SetValue("INVALIDTIME", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), i);
      }
    }
    public virtual void setReInvalidInfo(Hashtable userInfo)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        this.SetValue("INVALIDER", "", i);
        this.SetValue("INVALIDTIME", "", i);
      }
    }


    public virtual void setState(string flowCode)
    {
      for (int i = 0; i < this.GetView().Count; i++)
      {
        if (flowCode == BillFlow.提交_审核 + "")
        {
          if (this.GetValue("SUMBMITTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待提交, i);
          }
          else if (this.GetValue("CHECKTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审核, i);
          }
          else
          {
            this.SetValue("STATE", BillState.已审核, i);
          }
        }
        if (flowCode == BillFlow.提交_审核_审批 + "")
        {
          if (this.GetValue("SUMBMITTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待提交, i);
          }
          else if (this.GetValue("CHECKTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审核, i);
          }
          else if (this.GetValue("VERIFYTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审批, i);
          }
          else
          {
            this.SetValue("STATE", BillState.已审批, i);
          }
        }
        if (flowCode == BillFlow.提交_审核_发布 + "")
        {
          if (this.GetValue("SUMBMITTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待提交, i);
          }
          else if (this.GetValue("CHECKTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审核, i);
          }
          else if (this.GetValue("VERIFYTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待发布, i);
          }
          else
          {
            this.SetValue("STATE", BillState.已发布, i);
          }
        }
        if (flowCode == BillFlow.提交_审核_审批_作废 + "")
        {
          if (this.GetValue("SUMBMITTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待提交, i);
          }
          else if (this.GetValue("CHECKTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审核, i);
          }
          else if (this.GetValue("VERIFYTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待审批, i);
          }
          else
          {
            this.SetValue("STATE", BillState.已审批, i);
          }
          if (this.GetValue("INVALIDTIME", i) + "" != "")
          {
            this.SetValue("STATE", BillState.已作废, i);
          }
        }
        if (flowCode == BillFlow.提交_检验 + "")
        {
          if (this.GetValue("SUMBMITTIME", i) + "" == "")
          {
            this.SetValue("STATE", BillState.待提交, i);
          }
        }
      }
    }

    public virtual void setBillCode(string TCODE, string codeField = "BILLCODE", int rowIndex = 0)
    {
      Dictionary<string, object> Params = new Dictionary<string, object>();
      Params["TCODE"] = TCODE;
      Params["OCODE"] = new ParamInfo("", System.Data.DbType.String, System.Data.ParameterDirection.Output);
      ArrayList list = new ArrayList();
      list.Add(new ExecInfo("PSS_GENCODE", Params));
      this.operate.Save(list);
      this.SetValue(codeField, (Params["OCODE"] as ParamInfo).Value + "", rowIndex);
    }
  }
}
