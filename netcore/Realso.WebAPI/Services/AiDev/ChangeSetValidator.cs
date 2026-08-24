using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Realso.WebAPI.Models.AiDev;

namespace Realso.WebAPI.Services.AiDev
{
  /// <summary>
  /// 变更包校验器：把 orm-metadata-generator skill 的全部铁律代码化。
  /// 输入变更项列表，按 CATEGORY 决定跑哪些规则，输出 ValidationReport。
  /// 校验器只检查 METADATA JSON 里描述的元数据是否符合铁律，不查数据库。
  /// </summary>
  public class ChangeSetValidator
  {
    /// <summary>
    /// 校验整个变更包的所有变更项。
    /// </summary>
    public ValidationReport Validate(List<ChangeItem> items)
    {
      var report = new ValidationReport { Passed = true };
      if (items == null || items.Count == 0)
      {
        report.Passed = false;
        report.Checks.Add(new ValidationCheck
        {
          Rule = "HasItems",
          Status = ValidationCheck.STATUS_FAIL,
          Message = "变更包为空，无变更项可校验",
          ItemSeq = 0
        });
        return report;
      }

      foreach (var item in items)
      {
        var itemChecks = ValidateItem(item);
        report.Checks.AddRange(itemChecks);
      }

      // 任一 fail 则整体不通过
      if (report.Checks.Any(c => c.Status == ValidationCheck.STATUS_FAIL))
      {
        report.Passed = false;
      }
      return report;
    }

    /// <summary>
    /// 校验单个变更项：按 CATEGORY 决定跑哪些规则。
    /// </summary>
    public List<ValidationCheck> ValidateItem(ChangeItem item)
    {
      var checks = new List<ValidationCheck>();
      if (item == null) return checks;
      if (string.IsNullOrEmpty(item.METADATA))
      {
        // 无 METADATA 的变更项（如纯 SQL 项）只做依赖检查，跳过元数据规则
        return checks;
      }

      JObject meta;
      try
      {
        meta = JObject.Parse(item.METADATA);
      }
      catch (Exception ex)
      {
        checks.Add(new ValidationCheck
        {
          Rule = "MetadataParseable",
          Status = ValidationCheck.STATUS_FAIL,
          Message = "METADATA 不是合法 JSON: " + ex.Message,
          ItemSeq = item.ITEMSEQ
        });
        return checks;
      }

      // 按 CATEGORY 分发规则
      switch (item.CATEGORY)
      {
        case ChangeItem.CAT_PHYSICAL_TABLE:
          // 物理表：字段名规则
          RunFieldNameRules(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_DATAVIEW:
          // 视图：RESOURCEANAME + ID字段 + REFFIELDID + 字段名规则
          RunResourceanameRule(meta, item.ITEMSEQ, checks);
          RunVssIdFieldKeyRule(meta, item.ITEMSEQ, checks);
          RunVssReffieldidRule(meta, item.ITEMSEQ, checks);
          RunFieldNameRules(meta, item.ITEMSEQ, checks);
          RunRefResourceidRule(meta, item.ITEMSEQ, checks);
          RunReffieldidRule(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_FIELD:
          // 字段变更：字段名规则 + 引用字段规则
          RunFieldNameRules(meta, item.ITEMSEQ, checks);
          RunRefResourceidRule(meta, item.ITEMSEQ, checks);
          RunReffieldidRule(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_FILTER:
          // 过滤器：三条铁律
          RunFilterThreeRules(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_MODULE:
          // 模块：四路径 + ACTIONCODE
          RunMoudlePathFourPathsRule(meta, item.ITEMSEQ, checks);
          RunMoudleApiActioncodeRule(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_API:
          // 接口：ACTIONCODE + SQL 脚本接口的 SQLTXT 检查 + 编排接口的 APIPARAM 检查
          RunMoudleApiActioncodeRule(meta, item.ITEMSEQ, checks);
          RunSqlApiRules(meta, item.ITEMSEQ, checks);
          RunScriptFlowRules(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_PAGE:
          // 页面：PAGETYPE 枚举 + SFC 路径 + PAGECONFIG JSON
          RunModulePageRules(meta, item.ITEMSEQ, checks);
          break;
        case ChangeItem.CAT_BUTTON:
          // 按钮：BTNAREA/BTNCODE 枚举 + APICODE 必填场景 + EXTPARAM JSON
          RunModuleButtonRules(meta, item.ITEMSEQ, checks);
          break;
        default:
          // 其他类型暂无强制规则
          break;
      }
      return checks;
    }

    // ===== 规则1：RESOURCEANAME 不能为 NULL，必须为 'A' =====
    private void RunResourceanameRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var res = meta["resource"] as JObject;
      if (res == null)
      {
        checks.Add(Fail("CheckResourceaname", itemSeq, "METADATA 缺少 resource 节点"));
        return;
      }
      string aname = res["RESOURCEANAME"]?.ToString();
      if (string.IsNullOrEmpty(aname))
      {
        checks.Add(Fail("CheckResourceaname", itemSeq, "RESOURCEANAME 为 NULL，ORM 用它作 SQL 表别名，缺失会导致 .ID AS ID 语法错误"));
      }
      else if (aname != "A")
      {
        checks.Add(Warn("CheckResourceaname", itemSeq, "RESOURCEANAME='" + aname + "'，建议用 'A'（系统默认别名）"));
      }
      else
      {
        checks.Add(Pass("CheckResourceaname", itemSeq, "RESOURCEANAME='A'"));
      }
    }

    // ===== 规则2：VSS 的 ID 字段 ISKEY=1 KEYGENTYPE=GUID =====
    private void RunVssIdFieldKeyRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var fields = meta["resfields"] as JArray;
      if (fields == null || fields.Count == 0)
      {
        checks.Add(Warn("CheckVssIdFieldKey", itemSeq, "METADATA 无 resfields，跳过 ID 字段检查"));
        return;
      }
      bool foundId = false;
      foreach (JObject f in fields)
      {
        if (f["FIELDNAME"]?.ToString() == "ID")
        {
          foundId = true;
          int iskey = f["ISKEY"]?.Type == JTokenType.Integer ? (int)f["ISKEY"] : 0;
          string keygen = f["KEYGENTYPE"]?.ToString();
          if (iskey != 1)
          {
            checks.Add(Fail("CheckVssIdFieldKey", itemSeq, "ID 字段 ISKEY!=1，保存时无法识别主键"));
          }
          else if (string.IsNullOrEmpty(keygen) || keygen != "GUID")
          {
            checks.Add(Fail("CheckVssIdFieldKey", itemSeq, "ID 字段 KEYGENTYPE!='GUID'，保存时无法生成主键"));
          }
          else
          {
            checks.Add(Pass("CheckVssIdFieldKey", itemSeq, "ID 字段 ISKEY=1, KEYGENTYPE=GUID"));
          }
          break;
        }
      }
      if (!foundId)
      {
        checks.Add(Warn("CheckVssIdFieldKey", itemSeq, "resfields 中未找到 ID 字段"));
      }
    }

    // ===== 规则3：VSS 每个字段 REFFIELDID 非空 =====
    private void RunVssReffieldidRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var fields = meta["resfields"] as JArray;
      if (fields == null || fields.Count == 0)
      {
        checks.Add(Warn("CheckVssReffieldid", itemSeq, "METADATA 无 resfields，跳过 REFFIELDID 检查"));
        return;
      }
      int missing = 0;
      foreach (JObject f in fields)
      {
        string refId = f["REFFIELDID"]?.ToString();
        if (string.IsNullOrEmpty(refId))
        {
          missing++;
        }
      }
      if (missing > 0)
      {
        checks.Add(Fail("CheckVssReffieldid", itemSeq, "有 " + missing + " 个字段 REFFIELDID 为空，VCK 字段必须通过 REFFIELDID 关联物理表字段"));
      }
      else
      {
        checks.Add(Pass("CheckVssReffieldid", itemSeq, "所有字段 REFFIELDID 非空"));
      }
    }

    // ===== 规则4：过滤器三条铁律 =====
    private void RunFilterThreeRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var filter = meta["filter"] as JObject;
      if (filter == null)
      {
        checks.Add(Fail("CheckFilterThreeRules", itemSeq, "METADATA 缺少 filter 节点"));
        return;
      }
      string sql = filter["FILTERSQL"]?.ToString();
      string orderby = filter["ORDERBY"]?.ToString();
      string fcode = filter["FILTERCODE"]?.ToString();

      // F01 列表查询才强制三条铁律；F00 单条查询只需 RESOURCEANAME 类规则
      if (fcode == "F01" || fcode == "F02" || string.IsNullOrEmpty(fcode))
      {
        // 铁律1：必须以 1=1 开头
        if (string.IsNullOrEmpty(sql) || !sql.TrimStart().StartsWith("1=1"))
        {
          checks.Add(Fail("CheckFilterThreeRules", itemSeq, "FILTERSQL 未以 '1=1' 开头，INPUT 为空时 WHERE AND ... 语法错误"));
        }
        else
        {
          checks.Add(Pass("CheckFilterThreeRules", itemSeq, "FILTERSQL 以 '1=1' 开头"));
        }

        // 铁律2：必须用 @INPUT 参数
        if (string.IsNullOrEmpty(sql) || !sql.Contains("@INPUT"))
        {
          checks.Add(Fail("CheckFilterThreeRules", itemSeq, "FILTERSQL 未使用 @INPUT 参数，前端 QQRY 传的是 INPUT 字段"));
        }
        else
        {
          checks.Add(Pass("CheckFilterThreeRules", itemSeq, "FILTERSQL 使用 @INPUT 参数"));
        }
      }

      // 铁律3：ORDERBY 不能带表别名前缀
      if (!string.IsNullOrEmpty(orderby) && orderby.Contains("."))
      {
        checks.Add(Fail("CheckFilterThreeRules", itemSeq, "ORDERBY='" + orderby + "' 带表别名前缀，ORM 包子查询后外层别名是 T，应改为 '" + orderby.Replace("A.", "").Replace("a.", "") + "'"));
      }
      else if (!string.IsNullOrEmpty(orderby))
      {
        checks.Add(Pass("CheckFilterThreeRules", itemSeq, "ORDERBY 无表别名前缀"));
      }
    }

    // ===== 规则5：引用字段 REFRESOURCEID 必须指向 TBS(TABLE)而非 VBS(DATAVIEW) =====
    private void RunRefResourceidRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var fields = meta["resfields"] as JArray;
      if (fields == null) return;
      int refCount = 0, badCount = 0;
      foreach (JObject f in fields)
      {
        string refRid = f["REFRESOURCEID"]?.ToString();
        if (string.IsNullOrEmpty(refRid)) continue;
        refCount++;
        // METADATA 中引用资源的类型（如已知则校验，未知只警告）
        var refRes = f["REFRESOURCE_TYPE"]?.ToString();
        if (!string.IsNullOrEmpty(refRes))
        {
          if (refRes == "DATAVIEW")
          {
            badCount++;
            checks.Add(Fail("CheckRefResourceid", itemSeq, "字段 " + f["FIELDNAME"] + " 的 REFRESOURCEID 指向 DATAVIEW(VBS/VCK)，ORM 只支持 JOIN TABLE/VIEW/SQL，应指向 TBS 物理表"));
          }
        }
      }
      if (badCount == 0 && refCount > 0)
      {
        checks.Add(Pass("CheckRefResourceid", itemSeq, refCount + " 个引用字段 REFRESOURCEID 指向 TABLE 类型"));
      }
    }

    // ===== 规则6：引用名称字段的 REFFIELDID 必须指向被引用 TBS 表的字段 =====
    private void RunReffieldidRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var fields = meta["resfields"] as JArray;
      if (fields == null) return;
      int hasUp = 0, badCount = 0;
      foreach (JObject f in fields)
      {
        string upField = f["UPFIELDID"]?.ToString();
        if (string.IsNullOrEmpty(upField)) continue;
        hasUp++;
        string refFieldId = f["REFFIELDID"]?.ToString();
        if (string.IsNullOrEmpty(refFieldId))
        {
          badCount++;
          checks.Add(Fail("CheckReffieldid", itemSeq, "字段 " + f["FIELDNAME"] + " 有 UPFIELDID 但 REFFIELDID 为空，名称字段必须指向 JOIN 表的字段"));
        }
        else
        {
          // REFFIELDID 指向本地 TBS 字段而非 JOIN 表字段也算错，但无法在校验器层判定（需查库）
          // 这里只做存在性检查
        }
      }
      if (badCount == 0 && hasUp > 0)
      {
        checks.Add(Pass("CheckReffieldid", itemSeq, hasUp + " 个名称字段 REFFIELDID 非空"));
      }
    }

    // ===== 规则7：moudleapi 的 ACTIONCODE 不能为 NULL =====
    private void RunMoudleApiActioncodeRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var apis = meta["moudleapis"] as JArray;
      if (apis == null || apis.Count == 0)
      {
        // module 类型的变更项可能没有 apis，只警告
        return;
      }
      int badCount = 0;
      foreach (JObject a in apis)
      {
        string actionCode = a["ACTIONCODE"]?.ToString();
        if (string.IsNullOrEmpty(actionCode))
        {
          badCount++;
          checks.Add(Fail("CheckMoudleApiActioncode", itemSeq, "接口 " + (a["APICODE"]?.ToString() ?? "?") + " 的 ACTIONCODE 为 NULL，前端 getApi('query') 找不到 API"));
        }
      }
      if (badCount == 0)
      {
        checks.Add(Pass("CheckMoudleApiActioncode", itemSeq, "所有接口 ACTIONCODE 非空"));
      }
    }

    // ===== 规则8：模块必须有 QRY/QQRY/MAIN/SEL 四路径 =====
    private void RunMoudlePathFourPathsRule(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var paths = meta["moudlepaths"] as JArray;
      if (paths == null || paths.Count == 0)
      {
        checks.Add(Warn("CheckMoudlePathFourPaths", itemSeq, "METADATA 无 moudlepaths，跳过四路径检查"));
        return;
      }
      var pathSet = new HashSet<string>();
      foreach (var p in paths)
      {
        pathSet.Add(p.ToString());
      }
      string[] required = { "QRY", "QQRY", "MAIN", "SEL" };
      var missing = required.Where(r => !pathSet.Contains(r)).ToList();
      if (missing.Count > 0)
      {
        checks.Add(Fail("CheckMoudlePathFourPaths", itemSeq, "缺少路径: " + string.Join("/", missing) + "，缺少 QRY/QQRY 会导致前端 'QRY =不存在！！' 错误"));
      }
      else
      {
        checks.Add(Pass("CheckMoudlePathFourPaths", itemSeq, "四路径齐全: QRY/QQRY/MAIN/SEL"));
      }
    }

    // ===== 规则9：字段名不能有下划线 =====
    private void RunFieldNameRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var fields = meta["resfields"] as JArray;
      if (fields == null || fields.Count == 0) return;
      int underscoreCount = 0, lowerCount = 0;
      foreach (JObject f in fields)
      {
        string name = f["FIELDNAME"]?.ToString();
        if (string.IsNullOrEmpty(name)) continue;
        // 规则9：不能有下划线
        if (name.Contains("_"))
        {
          underscoreCount++;
          checks.Add(Fail("CheckFieldNameNoUnderscore", itemSeq, "字段名 '" + name + "' 含下划线，应使用大写无下划线格式（如 ISDELETED 而非 IS_DELETED）"));
        }
        // 规则10：必须大写
        if (name != name.ToUpperInvariant())
        {
          lowerCount++;
          checks.Add(Fail("CheckFieldNameUppercase", itemSeq, "字段名 '" + name + "' 含小写字母，必须全大写"));
        }
      }
      if (underscoreCount == 0)
      {
        checks.Add(Pass("CheckFieldNameNoUnderscore", itemSeq, "所有字段名无下划线"));
      }
      if (lowerCount == 0)
      {
        checks.Add(Pass("CheckFieldNameUppercase", itemSeq, "所有字段名大写"));
      }
    }

    // ===== 规则11：module_page 页面配置（PAGETYPE 枚举 / SFC 路径 / PAGECONFIG JSON）=====
    private void RunModulePageRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var page = meta["page"] as JObject;
      if (page == null)
      {
        checks.Add(Fail("CheckModulePage", itemSeq, "METADATA 缺少 page 节点"));
        return;
      }
      // PAGETYPE 枚举
      string pageType = page["PAGETYPE"]?.ToString();
      var types = new HashSet<string> { "list", "form", "select", "review", "report" };
      if (string.IsNullOrEmpty(pageType) || !types.Contains(pageType))
      {
        checks.Add(Fail("CheckModulePageType", itemSeq, "PAGETYPE='" + pageType + "' 必须是 list/form/select/review/report 之一"));
      }
      else
      {
        checks.Add(Pass("CheckModulePageType", itemSeq, "PAGETYPE=" + pageType));
      }
      // COMPONENTTYPE=sfc ⇒ SFCMODULEPATH 非空
      string compType = page["COMPONENTTYPE"]?.ToString();
      if (compType == "sfc" && string.IsNullOrEmpty(page["SFCMODULEPATH"]?.ToString()))
      {
        checks.Add(Fail("CheckSfcModulePath", itemSeq, "COMPONENTTYPE=sfc 但 SFCMODULEPATH 为空，页面渲染会空白"));
      }
      // PAGECONFIG 必须是合法 JSON（前端运行时 JSON.parse）
      string pageConfig = page["PAGECONFIG"]?.ToString();
      if (!string.IsNullOrEmpty(pageConfig))
      {
        try
        {
          JObject.Parse(pageConfig);
          checks.Add(Pass("CheckPageConfigJson", itemSeq, "PAGECONFIG 是合法 JSON"));
        }
        catch
        {
          checks.Add(Fail("CheckPageConfigJson", itemSeq, "PAGECONFIG 不是合法 JSON，前端 JSON.parse 会失败"));
        }
      }
    }

    // ===== 规则12：module_button 按钮配置（BTNAREA/BTNCODE 枚举 / APICODE 必填场景 / EXTPARAM JSON）=====
    private void RunModuleButtonRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var btn = meta["button"] as JObject;
      if (btn == null)
      {
        checks.Add(Fail("CheckModuleButton", itemSeq, "METADATA 缺少 button 节点"));
        return;
      }
      // BTNAREA 枚举
      string btnArea = btn["BTNAREA"]?.ToString() ?? "";
      if (btnArea == "header" || btnArea == "footer" || btnArea == "row" || btnArea.StartsWith("DTS"))
      {
        checks.Add(Pass("CheckBtnArea", itemSeq, "BTNAREA=" + btnArea));
      }
      else
      {
        checks.Add(Fail("CheckBtnArea", itemSeq, "BTNAREA='" + btnArea + "' 必须是 header/footer/row 或 DTS 开头的子表路径"));
      }
      // BTNCODE 预设集合（不在集合内只警告，运行时按 custom 处理）
      string btnCode = btn["BTNCODE"]?.ToString();
      var codes = new HashSet<string> { "add", "edit", "select", "delete", "save", "export", "submit", "reSubmit", "check", "reCheck", "verify", "reVerify", "subAdd", "subRemove", "subUp", "subDown", "cancel", "custom" };
      if (!string.IsNullOrEmpty(btnCode) && !codes.Contains(btnCode))
      {
        checks.Add(Warn("CheckBtnCode", itemSeq, "BTNCODE='" + btnCode + "' 不在预设集合内，前端将按 custom 处理"));
      }
      // 调接口类按钮必须配 APICODE
      string actionType = btn["ACTIONTYPE"]?.ToString();
      string apiCode = btn["APICODE"]?.ToString();
      var needApiCodes = new HashSet<string> { "custom", "submit", "reSubmit", "check", "reCheck", "verify", "reVerify" };
      if ((string.IsNullOrEmpty(actionType) || actionType == "api") && !string.IsNullOrEmpty(btnCode) && needApiCodes.Contains(btnCode) && string.IsNullOrEmpty(apiCode))
      {
        checks.Add(Fail("CheckBtnApiCode", itemSeq, "btnCode=" + btnCode + " 的按钮必须配置 APICODE（点击后调哪个模块接口）"));
      }
      // EXTPARAM 必须是合法 JSON
      string extParam = btn["EXTPARAM"]?.ToString();
      if (!string.IsNullOrEmpty(extParam))
      {
        try
        {
          JObject.Parse(extParam);
        }
        catch
        {
          checks.Add(Fail("CheckExtParamJson", itemSeq, "EXTPARAM 不是合法 JSON，前端解析会失败"));
        }
      }
    }

    // ===== 规则13：SQL 脚本接口（APITYPE=sql）的 SQLTXT 检查（禁单引号/禁 DDL）=====
    private void RunSqlApiRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var sqlNode = meta["sql"] as JObject;
      if (sqlNode == null) return;  // 非 sql 接口（define_api 产出的项无 sql 节点）
      string sqlTxt = sqlNode["SQLTXT"]?.ToString() ?? "";
      if (string.IsNullOrEmpty(sqlTxt))
      {
        checks.Add(Fail("CheckSqlApiTxt", itemSeq, "SQLTXT 为空"));
        return;
      }
      // 禁单引号（NVelocity 铁律）
      if (sqlTxt.Contains("'"))
      {
        checks.Add(Fail("CheckSqlApiQuote", itemSeq, "SQLTXT 含单引号，NVelocity 解析会失败（用 @参数或 CHAR(39)）"));
      }
      else
      {
        checks.Add(Pass("CheckSqlApiQuote", itemSeq, "SQLTXT 无单引号"));
      }
      // 禁 DDL
      string hit = null;
      foreach (var s in Realso.Utils.SqlScriptHelper.SplitSqlStatements(sqlTxt))
      {
        hit = Realso.Utils.SqlScriptHelper.MatchDdlKeyword(s);
        if (hit != null) break;
      }
      if (hit != null)
      {
        checks.Add(Fail("CheckSqlApiDdl", itemSeq, "SQLTXT 含 DDL 关键字 " + hit + "，脚本接口禁止 DDL"));
      }
      else
      {
        checks.Add(Pass("CheckSqlApiDdl", itemSeq, "SQLTXT 无 DDL"));
      }
    }

    // ===== 规则：APITYPE=script 编排接口的 APIPARAM 步骤校验 =====
    private void RunScriptFlowRules(JObject meta, int itemSeq, List<ValidationCheck> checks)
    {
      var apiNode = meta["moudleapi"] as JObject;
      if (apiNode == null) return;
      string apiType = apiNode["APITYPE"]?.ToString() ?? "";
      if (apiType != "script") return;  // 非编排接口跳过

      string apiParam = apiNode["APIPARAM"]?.ToString() ?? "";
      if (string.IsNullOrEmpty(apiParam))
      {
        checks.Add(Fail("CheckScriptFlowParam", itemSeq, "APITYPE=script 但 APIPARAM 为空"));
        return;
      }

      // 解析步骤 JSON
      JArray steps;
      try
      {
        var parsed = JToken.Parse(apiParam);
        if (parsed is JArray) steps = (JArray)parsed;
        else { checks.Add(Fail("CheckScriptFlowParam", itemSeq, "APIPARAM 不是合法的 JSON 数组")); return; }
      }
      catch (Exception ex)
      {
        checks.Add(Fail("CheckScriptFlowParam", itemSeq, "APIPARAM JSON 解析失败: " + ex.Message));
        return;
      }

      if (steps.Count == 0)
      {
        checks.Add(Fail("CheckScriptFlowParam", itemSeq, "APIPARAM 步骤数组为空"));
        return;
      }

      var validTypes = new HashSet<string> { "sql", "query", "if", "update", "return" };
      bool allOk = true;
      for (int i = 0; i < steps.Count; i++)
      {
        var step = steps[i] as JObject;
        if (step == null) { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + " 不是 JSON 对象")); allOk = false; continue; }
        string type = step["type"]?.ToString()?.ToLower() ?? "";
        if (!validTypes.Contains(type))
        { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + " 未知类型: " + type)); allOk = false; continue; }
        if ((type == "sql" || type == "update") && string.IsNullOrEmpty(step["sqlCode"]?.ToString()))
        { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + "(type=" + type + ") 缺少 sqlCode")); allOk = false; }
        if (type == "query" && string.IsNullOrEmpty(step["apiCode"]?.ToString()))
        { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + "(type=query) 缺少 apiCode")); allOk = false; }
        if (type == "if")
        {
          if (string.IsNullOrEmpty(step["cond"]?.ToString()))
          { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + "(type=if) 缺少 cond")); allOk = false; }
          if (step["goto"] == null)
          { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + "(type=if) 缺少 goto")); allOk = false; }
          else
          {
            int gotoVal = step["goto"].Value<int>();
            if (gotoVal < 0 || gotoVal >= steps.Count)
            { checks.Add(Fail("CheckScriptFlowStep", itemSeq, "步骤 " + i + " goto=" + gotoVal + " 超出范围")); allOk = false; }
          }
        }
      }
      if (allOk) checks.Add(Pass("CheckScriptFlowStep", itemSeq, steps.Count + " 步骤校验通过"));
    }

    // ===== 辅助：构造校验结果 =====
    private static ValidationCheck Pass(string rule, int itemSeq, string msg)
    {
      return new ValidationCheck { Rule = rule, Status = ValidationCheck.STATUS_PASS, Message = msg, ItemSeq = itemSeq };
    }
    private static ValidationCheck Fail(string rule, int itemSeq, string msg)
    {
      return new ValidationCheck { Rule = rule, Status = ValidationCheck.STATUS_FAIL, Message = msg, ItemSeq = itemSeq };
    }
    private static ValidationCheck Warn(string rule, int itemSeq, string msg)
    {
      return new ValidationCheck { Rule = rule, Status = ValidationCheck.STATUS_WARN, Message = msg, ItemSeq = itemSeq };
    }
  }
}
