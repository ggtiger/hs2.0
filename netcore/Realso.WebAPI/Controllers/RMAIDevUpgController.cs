using System;
using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Realso.Core.Base;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.WebAPI.Models;
using Realso.WebAPI.Services.AiDev;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// AI 开发助理 - 升级管理 Controller。
  /// 继承 DataController 走统一入口 api/data/call/RM_AIDEV_UPG/{ApiCode}，
  /// 在 doMyApi 里 switch APICODE 分发到升级执行器相关操作。
  ///
  /// 接口清单：
  /// - A05 import  : 导入 .aidev.sql 脚本入库（STATUS=PENDING）
  /// - A06 execute : 执行升级（单事务，任一失败回滚）
  /// - A07 rollback : 回滚升级（执行 ROLLBACKSCRIPT）
  /// - A08 preview : 预览升级内容（解析 SCRIPTCONTENT 为变更项列表）
  /// </summary>
  [Route("api/[controller]")]
  [Authorize]
  public class RMAIDevUpgController : DataController
  {
    private readonly UpgradeExecutor _executor;

    public RMAIDevUpgController(UpgradeExecutor executor)
    {
      _executor = executor;
    }

    /// <summary>
    /// 重写 doMyApi：按 APICODE 分发。
    /// APICODE 从 row 取（与 RMAIDevController 一致），也兼容从 Params 取。
    /// </summary>
    protected override void doMyApi(MOUDLE MD, ViewRow row, String APITYPE, Hashtable Params)
    {
      string APICODE = row != null ? row.GetString("APICODE") : (Params["APICODE"] as string);
      if (string.IsNullOrEmpty(APICODE) && Params["APICODE"] != null)
        APICODE = Params["APICODE"].ToString();

      // 用户ID：优先 Params["__USERID__"]，其次 userInfo.ID
      string userId = "anonymous";
      if (Params != null && Params["__USERID__"] != null)
        userId = Params["__USERID__"].ToString();
      else if (this.userInfo != null && this.userInfo["ID"] != null)
        userId = this.userInfo["ID"].ToString();

      switch (APICODE)
      {
        case "A05":  // import
          doImport(MD, row, Params, userId);
          break;
        case "A06":  // execute
          doExecute(MD, row, Params, userId);
          break;
        case "A07":  // rollback
          doRollback(MD, row, Params, userId);
          break;
        case "A08":  // preview
          doPreview(MD, row, Params);
          break;
        default:
          responseModel.SetError("接口编码:" + APICODE + " 不存在！");
          break;
      }
    }

    /// <summary>
    /// A05 import：导入 .aidev.sql 脚本入库。
    /// 入参：{scriptContent}
    /// 返回：{upgradeId, upgradeCode, sessionCode, status:'PENDING'}
    /// 注意：scriptContent 可能很大（LONGTEXT），通过 [FromForm] Hashtable Params 传。
    /// </summary>
    private void doImport(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string scriptContent = Params["scriptContent"] as string;
      if (string.IsNullOrEmpty(scriptContent))
      {
        responseModel.SetError("scriptContent 不能为空");
        return;
      }
      try
      {
        string upgradeId = _executor.Import(scriptContent, userId);
        responseModel.SetData(new
        {
          upgradeId,
          status = "PENDING",
          importedBy = userId,
          hint = "导入成功，可调用 A06 execute 执行升级"
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("导入失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A06 execute：执行升级（单事务，任一失败回滚）。
    /// 入参：{upgradeId}
    /// 返回：{status, itemCount, failedItemId, errorMsg, durationMs}
    /// </summary>
    private void doExecute(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string upgradeId = Params["upgradeId"] as string;
      if (string.IsNullOrEmpty(upgradeId))
      {
        responseModel.SetError("upgradeId 不能为空");
        return;
      }
      try
      {
        var result = _executor.Execute(upgradeId, userId);
        responseModel.SetData(new
        {
          status = result.Status,
          itemCount = result.ItemCount,
          failedItemId = result.FailedItemId ?? "",
          failedItemCategory = result.FailedItemCategory ?? "",
          errorMsg = result.ErrorMsg ?? "",
          durationMs = result.DurationMs,
          executedBy = userId
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("执行失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A07 rollback：回滚升级（执行 ROLLBACKSCRIPT）。
    /// 入参：{upgradeId}
    /// 返回：{status:'ROLLEDBACK', durationMs}
    /// </summary>
    private void doRollback(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string upgradeId = Params["upgradeId"] as string;
      if (string.IsNullOrEmpty(upgradeId))
      {
        responseModel.SetError("upgradeId 不能为空");
        return;
      }
      try
      {
        var result = _executor.Rollback(upgradeId, userId);
        responseModel.SetData(new
        {
          status = result.Status,
          durationMs = result.DurationMs,
          rolledbackBy = userId
        });
      }
      catch (Exception ex)
      {
        responseModel.SetError("回滚失败：" + ex.Message);
      }
    }

    /// <summary>
    /// A08 preview：预览升级内容（解析 SCRIPTCONTENT 为变更项列表）。
    /// 入参：{upgradeId}
    /// 返回：{upgrade, items[]}
    /// </summary>
    private void doPreview(MOUDLE MD, ViewRow row, Hashtable Params)
    {
      string upgradeId = Params["upgradeId"] as string;
      if (string.IsNullOrEmpty(upgradeId))
      {
        responseModel.SetError("upgradeId 不能为空");
        return;
      }
      try
      {
        var result = _executor.Preview(upgradeId);
        responseModel.SetData(result);
      }
      catch (Exception ex)
      {
        responseModel.SetError("预览失败：" + ex.Message);
      }
    }
  }
}
