using System;
using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.DBAccess;
using Realso.Data.ORM.Core;
using Realso.WebAPI.Models;
using Realso.WebAPI.Services;
using Realso.WebAPI.Services.AiDev;

namespace Realso.WebAPI.Controllers
{
  /// <summary>
  /// 业务模板市场 Controller（RS_M25）。
  /// 继承 DataController 走统一入口，自定义接口在 doMyApi 按 APICODE 分发：
  ///   A05 exportModule — 把已存在模块的全部关联元数据导出为模板（tss_module_template）
  ///   A06 install      — 变量替换 + UpgradeExecutor.Import 注册升级（PENDING）
  ///                      后续执行/回滚复用 RMAIDevUpgController 的 A06/A07/A08
  /// 标准 A01 查询 / A02 打开 / A04 保存 / A07 删除走基类。
  /// </summary>
  [Route("api/[controller]")]
  [Authorize]
  public class RModuleTplController : DataController
  {
    private readonly UpgradeExecutor _upgradeExecutor;
    private readonly ChangeSetExporter _changeSetExporter;

    public RModuleTplController(UpgradeExecutor upgradeExecutor, ChangeSetExporter changeSetExporter)
    {
      _upgradeExecutor = upgradeExecutor;
      _changeSetExporter = changeSetExporter;
    }

    protected override void doMyApi(MOUDLE MD, ViewRow row, string APITYPE, Hashtable Params)
    {
      string apiCode = row != null ? row.GetString("APICODE") : "";
      string userId = this.userInfo != null && this.userInfo["ID"] != null ? this.userInfo["ID"] + "" : "anonymous";
      string userName = this.userInfo != null && this.userInfo["NICKNAME"] != null ? this.userInfo["NICKNAME"] + "" : userId;
      switch (apiCode)
      {
        case "A05":
          doExportModule(MD, row, Params, userId, userName);
          break;
        case "A06":
          doInstall(MD, row, Params, userId);
          break;
        case "A08":
          doSaveSessionAsTemplate(MD, row, Params, userId, userName);
          break;
        default:
          base.doMyApi(MD, row, APITYPE, Params);
          break;
      }
    }

    /// <summary>
    /// A05 导出模块为模板。
    /// 入参: {moduleCode, templateCode, templateName, category, description}
    /// 返回: {id, templateCode, itemCount}
    /// </summary>
    protected virtual void doExportModule(MOUDLE MD, ViewRow row, Hashtable Params, string userId, string userName)
    {
      string moduleCode = Params["moduleCode"] + "";
      string templateCode = Params["templateCode"] + "";
      string templateName = Params["templateName"] + "";
      string category = Params["category"] + "";
      string description = Params["description"] + "";
      if (string.IsNullOrEmpty(moduleCode)) { responseModel.SetError("moduleCode 不能为空"); return; }
      if (string.IsNullOrEmpty(templateCode)) { responseModel.SetError("templateCode 不能为空"); return; }
      if (string.IsNullOrEmpty(templateName)) { responseModel.SetError("templateName 不能为空"); return; }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // TEMPLATECODE 唯一性
        var exist = helper.QueryFirstOrDefault(
          "SELECT ID FROM tss_module_template WHERE TEMPLATECODE=@tc AND ISDELETED=0 LIMIT 1",
          new { tc = templateCode });
        if (exist != null)
        {
          responseModel.SetError("模板编码 " + templateCode + " 已存在，请更换");
          return;
        }
        try
        {
          var result = TemplateExporter.Export(helper, moduleCode, templateCode, templateName, userName);
          string id = Guid.NewGuid().ToString("N");
          helper.Execute(
            @"INSERT INTO tss_module_template
              (ID, TEMPLATECODE, TEMPLATENAME, CATEGORY, DESCRIPTION, VARIABLES, SCRIPT, SOURCEINFO, VERSION, ENABLED, CREATEID, CREATER, CREATETIME, ISDELETED)
              VALUES (@ID, @TC, @TN, @CAT, @DES, @VARS, @SCRIPT, @SRC, '1.0.0', 1, @CB, @CBN, @CT, 0)",
            new
            {
              ID = id,
              TC = templateCode,
              TN = templateName,
              CAT = category,
              DES = description,
              VARS = result.Variables,
              SCRIPT = result.Script,
              SRC = moduleCode,
              CB = userId,
              CBN = userName,
              CT = DateTime.Now
            });
          responseModel.SetData(new { id, templateCode, itemCount = result.ItemCount, message = "导出成功，共 " + result.ItemCount + " 条元数据" });
        }
        catch (Exception ex)
        {
          responseModel.SetError("导出失败: " + ex.Message);
        }
      }
    }

    /// <summary>
    /// A06 安装模板：变量替换 → UpgradeExecutor.Import 注册升级（STATUS=PENDING）。
    /// 入参: {templateId, variables(JSON 字符串, 如 {"MODULECODE":"R02_M08","MODULENAME":"样品管理","PARENTFUNCID":"xxx"})}
    /// 返回: {upgradeId, upgradeCode, status:'PENDING'}（后续用 RMAIDevUpg 的 A08 预览 / A06 执行 / A07 回滚）
    /// </summary>
    protected virtual void doInstall(MOUDLE MD, ViewRow row, Hashtable Params, string userId)
    {
      string templateId = Params["templateId"] + "";
      string variablesJson = Params["variables"] + "";
      if (string.IsNullOrEmpty(templateId)) { responseModel.SetError("templateId 不能为空"); return; }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var tpl = helper.QueryFirstOrDefault(
          "SELECT * FROM tss_module_template WHERE ID=@id AND ISDELETED=0 LIMIT 1", new { id = templateId });
        if (tpl == null) { responseModel.SetError("模板不存在"); return; }
        string script = (string)tpl.SCRIPT;

        string error = TemplateExporter.Substitute(script, variablesJson, out string finalScript);
        if (error != null)
        {
          responseModel.SetError(error);
          return;
        }
        try
        {
          string upgradeId = _upgradeExecutor.Import(finalScript, userId);
          responseModel.SetData(new
          {
            upgradeId,
            status = "PENDING",
            message = "已注册升级（PENDING），确认后执行"
          });
        }
        catch (Exception ex)
        {
          responseModel.SetError("注册升级失败: " + ex.Message);
        }
      }
    }
    /// <summary>
    /// A08 把 AI 开发会话的已确认变更存为模板（模板来源 A：AI 会话导出）。
    /// 取 ChangeSetExporter.PreviewScript（已确认项拼接，不冻结会话），
    /// 加模板头（SessionCode=TPL_xxx，供 Import 幂等）+ 目标模块编码/名称替换为变量占位。
    /// 入参: {sessionId, templateCode, templateName, category, description}
    /// 返回: {id, templateCode, itemCount}
    /// </summary>
    protected virtual void doSaveSessionAsTemplate(MOUDLE MD, ViewRow row, Hashtable Params, string userId, string userName)
    {
      string sessionId = Params["sessionId"] + "";
      string templateCode = Params["templateCode"] + "";
      string templateName = Params["templateName"] + "";
      string category = Params["category"] + "";
      string description = Params["description"] + "";
      if (string.IsNullOrEmpty(sessionId)) { responseModel.SetError("sessionId 不能为空"); return; }
      if (string.IsNullOrEmpty(templateCode)) { responseModel.SetError("templateCode 不能为空"); return; }
      if (string.IsNullOrEmpty(templateName)) { responseModel.SetError("templateName 不能为空"); return; }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var session = helper.QueryFirstOrDefault(
          "SELECT * FROM tss_aidev_session WHERE ID=@id AND ISDELETED=0 LIMIT 1", new { id = sessionId });
        if (session == null) { responseModel.SetError("会话不存在"); return; }
        string targetModule = session.TARGETMODULE + "";
        string sessionName = session.SESSIONNAME + "";

        var exist = helper.QueryFirstOrDefault(
          "SELECT ID FROM tss_module_template WHERE TEMPLATECODE=@tc AND ISDELETED=0 LIMIT 1",
          new { tc = templateCode });
        if (exist != null)
        {
          responseModel.SetError("模板编码 " + templateCode + " 已存在，请更换");
          return;
        }

        try
        {
          // 已确认项拼接脚本（不冻结会话、不带头/幂等段）
          string body = _changeSetExporter.PreviewScript(sessionId);
          if (string.IsNullOrEmpty(body) || !body.Contains("INSERT") && !body.Contains("CREATE") && !body.Contains("ALTER") && !body.Contains("UPDATE"))
          {
            responseModel.SetError("会话内没有已确认的变更项，请先在变更项列表确认");
            return;
          }
          // 变量占位（与 TemplateExporter 同规则：带引号精确替换）
          if (!string.IsNullOrEmpty(targetModule))
          {
            body = body.Replace("'" + targetModule + "'", "'${MODULECODE}'");
          }
          // 模板头（Import 解析 SessionCode 必填）
          string parentFuncId = "";
          var header = new System.Text.StringBuilder();
          header.AppendLine("-- ============================================================");
          header.AppendLine("-- 业务模板脚本（来源: AI 开发会话 " + sessionName + "）");
          header.AppendLine("-- ============================================================");
          header.AppendLine("-- @META SessionCode=TPL_" + templateCode);
          header.AppendLine("-- @META SessionName=" + templateName);
          header.AppendLine("-- @META SessionType=template");
          header.AppendLine("-- @META TargetModule=" + targetModule);
          header.AppendLine("-- @META Intent=AI 会话存为模板(" + sessionId + ")");
          header.AppendLine("-- @META TemplateCode=" + templateCode);
          header.AppendLine("-- @META SourceModule=" + targetModule);
          header.AppendLine("-- @META Variables=${MODULECODE},${MODULENAME},${PARENTFUNCID}");
          header.AppendLine();
          string script = header.ToString() + body;

          string variables = Newtonsoft.Json.JsonConvert.SerializeObject(new object[]
          {
            new { name = "MODULECODE", label = "模块编码", @default = targetModule, required = true },
            new { name = "MODULENAME", label = "模块名称", @default = "", required = true },
            new { name = "PARENTFUNCID", label = "父菜单ID", @default = parentFuncId, required = true }
          });
          string id = Guid.NewGuid().ToString("N");
          helper.Execute(
            @"INSERT INTO tss_module_template
              (ID, TEMPLATECODE, TEMPLATENAME, CATEGORY, DESCRIPTION, VARIABLES, SCRIPT, SOURCEINFO, VERSION, ENABLED, CREATEID, CREATER, CREATETIME, ISDELETED)
              VALUES (@ID, @TC, @TN, @CAT, @DES, @VARS, @SCRIPT, @SRC, '1.0.0', 1, @CB, @CBN, @CT, 0)",
            new
            {
              ID = id,
              TC = templateCode,
              TN = templateName,
              CAT = category,
              DES = description,
              VARS = variables,
              SCRIPT = script,
              SRC = "AI会话:" + sessionName,
              CB = userId,
              CBN = userName,
              CT = DateTime.Now
            });
          responseModel.SetData(new { id, templateCode, message = "已存为模板，可到模板市场预览/安装" });
        }
        catch (Exception ex)
        {
          responseModel.SetError("存为模板失败: " + ex.Message);
        }
      }
    }
  }
}
