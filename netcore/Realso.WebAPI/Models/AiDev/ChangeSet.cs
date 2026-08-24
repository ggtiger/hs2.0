using System;
using System.Collections.Generic;

namespace Realso.WebAPI.Models.AiDev
{
  /// <summary>
  /// AI 开发会话（对应 tss_aidev_session）。
  /// 一次开发对话对应一个会话，关联一个变更包，可导出为 SQL 脚本。
  /// </summary>
  public class DevSession
  {
    public string ID;
    public string SESSIONCODE;
    public string SESSIONNAME;
    public string SESSIONTYPE;       // chat/dev
    public string TARGETMODULE;      // 目标模块编码
    public string INTENT;            // 用户意图
    public string STATUS;            // DRAFT/GENERATING/REVIEWING/EXPORTED/ARCHIVED
    public string CREATEDBY;
    public DateTime? CREATEDTIME;
    public string CHANGESETID;       // 关联变更包ID
    public int ISDELETED;

    // 状态常量
    public const string STATUS_DRAFT = "DRAFT";
    public const string STATUS_GENERATING = "GENERATING";
    public const string STATUS_REVIEWING = "REVIEWING";
    public const string STATUS_EXPORTED = "EXPORTED";
    public const string STATUS_ARCHIVED = "ARCHIVED";
  }

  /// <summary>
  /// 变更包（对应 tss_aidev_changeset）。
  /// 一个会话对应一个变更包，包含若干变更项，可整体校验/导出。
  /// </summary>
  public class ChangeSet
  {
    public string ID;
    public string SESSIONID;
    public string CHANGESETCODE;
    public string TITLE;
    public string SOURCE;            // ai/manual
    public string INTENT;
    public int? VALIDATIONPASSED;    // 1=通过 0=未通过 NULL=未校验
    public string VALIDATIONREPORT;  // JSON 校验报告
    public int ITEMCOUNT;
    public DateTime? CREATEDTIME;
    public int ISDELETED;

    public const string SOURCE_AI = "ai";
    public const string SOURCE_MANUAL = "manual";
  }

  /// <summary>
  /// 变更项（对应 tss_aidev_changeitem）。
  /// AI 产出的每一条 DDL/DML/元数据变更，需用户确认后才会进入导出脚本。
  /// </summary>
  public class ChangeItem
  {
    public string ID;
    public string CHANGESETID;
    public int ITEMSEQ;              // AI 产出顺序
    public string CATEGORY;          // physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/billflow
    public string ACTION;            // create/alter/update/delete
    public string TOOL;              // 产出该项的工具名
    public string TARGET;            // 变更目标（如 RESOURCENAME/FILTERCODE）
    public string SQLCONTENT;        // 可执行 SQL（确认后拼接进导出脚本）
    public string METADATA;          // JSON 元数据描述（供校验器检查）
    public string RATIONALE;         // AI 给出的变更理由
    public string WARNINGS;          // 警告信息
    public string DEPENDSON;         // 依赖的变更项ID（逗号分隔）
    public string ITEMSTATUS;        // DRAFT/CONFIRMED/REJECTED
    public string CONFIRMEDBY;
    public DateTime? CONFIRMEDTIME;
    public int? CONFIRMORDER;        // 确认顺序（导出排序用）
    public int ISDELETED;

    // 状态常量
    public const string STATUS_DRAFT = "DRAFT";
    public const string STATUS_CONFIRMED = "CONFIRMED";
    public const string STATUS_REJECTED = "REJECTED";
    public const string STATUS_MERGED = "MERGED";  // 已被合并到统一变更项（分析阶段细粒度，确认时合并为一条）
    public const string STATUS_EXECUTED = "EXECUTED";  // 已执行（ExecuteConfirmed 成功后由 CONFIRMED 转入，向导步骤强制的判定依据）

    // CATEGORY 常量
    public const string CAT_PHYSICAL_TABLE = "physical_table";
    public const string CAT_DATAVIEW = "dataview";
    public const string CAT_FIELD = "field";
    public const string CAT_UI = "ui";
    public const string CAT_DICT = "dict";
    public const string CAT_FILTER = "filter";
    public const string CAT_MODULE = "module";
    public const string CAT_API = "api";
    public const string CAT_MENU = "menu";
    public const string CAT_PERMISSION = "permission";
    public const string CAT_BILLFLOW = "billflow";
    public const string CAT_MERGED = "merged";  // 合并后的统一变更项（含整段脚本）
    public const string CAT_PAGE = "page";      // 模块页面（tss_module_page，GenericModule 页面清单）
    public const string CAT_BUTTON = "button";  // 页面按钮（tss_module_button）

    // ACTION 常量
    public const string ACTION_CREATE = "create";
    public const string ACTION_ALTER = "alter";
    public const string ACTION_UPDATE = "update";
    public const string ACTION_DELETE = "delete";
  }

  /// <summary>
  /// 校验报告：变更包整体校验结果。
  /// 序列化后写入 tss_aidev_changeset.VALIDATIONREPORT。
  /// </summary>
  public class ValidationReport
  {
    public bool Passed;
    public List<ValidationCheck> Checks = new List<ValidationCheck>();
  }

  /// <summary>
  /// 单条校验结果。
  /// ItemSeq 关联到具体变更项（0 表示整包级检查）。
  /// </summary>
  public class ValidationCheck
  {
    public string Rule;       // 规则名（如 CheckResourceaname）
    public string Status;     // pass/fail/warn
    public string Message;    // 详细说明
    public int ItemSeq;       // 关联变更项序号（0=整包级）

    public const string STATUS_PASS = "pass";
    public const string STATUS_FAIL = "fail";
    public const string STATUS_WARN = "warn";
  }
}
