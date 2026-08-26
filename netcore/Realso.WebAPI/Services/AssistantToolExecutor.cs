using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.Data.ORM;
using Realso.Data.ORM.Core;
using Realso.Utils;
using Realso.WebAPI.Services.Agent;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 助理工具执行器：持有用户上下文，按工具名分发执行，返回结果对象（序列化后喂回 LLM）。
  /// 工具实现复用 DBHelper 直查元数据表；数据查询类工具(M2.5/2.6)用 DataCallService。
  /// 实现 IToolExecutor 接口，工具集按场景分组（assistant/formfill/dev），
  /// IsFrontendTool 替代 AssistantHub 里硬编码的 FRONTEND_TOOLS HashSet。
  /// </summary>
  public class AssistantToolExecutor : IToolExecutor
  {
    // 前端工具集合：这些工具由前端代理层执行（操作UI/store/router），后端只转发调用并等待结果。
    // 从 AssistantHub.FRONTEND_TOOLS 迁移过来（阶段 2a），替代 Hub 里硬编码的 HashSet。
    private static readonly HashSet<string> FRONTEND_TOOLS = new HashSet<string>
    {
      "fill_form", "fill_subtable", "get_form_data", "get_current_page",
      "get_user_info", "get_menus", "show_message", "close_dialog",
      "open_form", "set_form_field", "get_form_field", "save_form",
      "add_subtable_row", "delete_subtable_row", "update_subtable_row",
      "clear_subtable", "get_subtable_data", "list_subtables"
    };

    private readonly Hashtable _userInfo;
    private readonly DataCallService _dataCall;

    public AssistantToolExecutor(Hashtable userInfo)
    {
      _userInfo = userInfo;
      _dataCall = new DataCallService(new ViewOperate01());
    }

    /// <summary>发给 DeepSeek 的 tools 定义（function calling）</summary>
    public static List<object> GetToolDefinitions()
    {
      var defs = new List<object>
      {
        Tool("search_menu", "按关键词搜索系统模块。返回匹配的模块列表(moduleCode/moduleName/remark)。先调用此工具找到用户想操作的模块的 moduleCode。",
          P("keyword", "string", "搜索关键词，如模块名或编号", true)),
        // 以下工具 M2 后续逐步实现，先注册让 LLM 知晓
        Tool("get_module_schema", "获取指定模块的字段/过滤器/API定义。在 query_data 前调用，了解该模块接受哪些过滤参数。",
          P("moduleCode", "string", "模块编号(moduleCode)", true)),
        Tool("query_data", "查询指定模块的数据列表。filter 的 key 必须来自 get_module_schema 返回的过滤器参数名。",
          P("moduleCode", "string", "模块编号", true),
          P("filter", "object", "过滤条件，key 为过滤器参数名", false),
          P("pageSize", "integer", "每页条数，默认10，上限500", false)),
        Tool("query_stats", "统计分析：在模块数据上做聚合统计(COUNT/SUM/AVG/GROUP BY)。底层用 ORM 自动构建正确查询(表/JOIN/数据权限)，你只需提供 select/groupBy 等，引用字段名(来自 get_module_schema 的 fields[].name)，无需关心表名/JOIN。统计需求优先用此工具。查子表数据时传path(子表路径名如DTSA，字段在get_module_schema的subTables[].fields)，不传path查主表。",
          P("moduleCode", "string", "模块编号", true),
          P("select", "string", "SELECT 列表，引用字段名，可含聚合。如 'DEPTNAME, COUNT(*) AS cnt'", true),
          P("path", "string", "子表路径名(可选，如DTSA)。传则查子表，字段来自get_module_schema的subTables[].fields", false),
          P("groupBy", "string", "GROUP BY 字段名，如 'DEPTNAME'", false),
          P("where", "string", "过滤(子查询层)，引用字段名，如 'ISDELETED=0'", false),
          P("orderBy", "string", "排序，如 'cnt DESC'", false),
          P("having", "string", "HAVING 条件", false)),
        Tool("open_record", "按ID打开单据详情(主表+子表)。",
          P("moduleCode", "string", "模块编号", true),
          P("id", "string", "单据ID", true)),
        Tool("navigate", "跳转到模块的真实页面（新增/编辑/删除/审批等变更操作都在真实页面做，不在对话框处理）。返回列表页路由并触发前端跳转。用户想新增/修改/删除/审批某模块时调用此工具。",
          P("moduleCode", "string", "模块编号", true),
          P("id", "string", "若针对某条具体记录(编辑/查看)，传其ID，页面会自动打开", false)),
        // 以下两个只读工具供 AI 开发助理（变更包生成）使用：复用旧资源 + 读物理表结构
        Tool("search_existing_resource", "检查目标表是否已有资源注册（落实'复用旧资源'原则）。生成新模块 SQL 前必须调用，避免同一表出现两套资源冲突。返回 ID/RESOURCENAME/RESOURCETYPE/TABLENAME。",
          P("tableName", "string", "物理表名(如 tbs_emp)", true)),
        Tool("read_table_schema", "读物理表结构(SHOW COLUMNS FROM)+ 已注册的 resfield，返回字段列表。用于了解现有表结构，避免重复建表或字段。",
          P("tableName", "string", "物理表名(如 tbs_emp)", true))
      };
      return MergeDeclarative(defs, "assistant");
    }

    /// <summary>
    /// AI 开发助理专用工具集：产出变更项结构化数据（sql + metadata），
    /// 由 Orchestrator 转成 ChangeItem 写入变更包。工具不直接写库。
    /// </summary>
    public static List<object> GetDevToolDefinitions()
    {
      var defs = new List<object>
      {
        // 只读工具（开发助理也要用，用于先复用旧资源）
        Tool("search_existing_resource", "检查目标表是否已有资源注册（落实'复用旧资源'原则）。生成新模块 SQL 前必须调用，避免同一表出现两套资源冲突。返回 ID/RESOURCENAME/RESOURCETYPE/TABLENAME。",
          P("tableName", "string", "物理表名(如 tbs_emp)", true)),
        Tool("read_table_schema", "读物理表结构(SHOW COLUMNS FROM)+ 已注册的 resfield，返回字段列表。用于了解现有表结构，避免重复建表或字段。",
          P("tableName", "string", "物理表名(如 tbs_emp)", true)),
        Tool("read_sfc_template", "读取已有 SFC 模板的标准源码（学习结构）。**产出 SFC 模块前必须先读 SFC_TEST 三件套**（SFC_TEST_STORE/SFC_TEST_MAIN/SFC_TEST_ADD），照着标准结构改业务部分（模块名/字段/TableItem），不要自己发明结构。返回 SOURCECODE/MODULEPATH/FILETYPE/DEPS。",
          P("templateCode", "string", "标准模板编码(SFC_TEST_STORE/SFC_TEST_MAIN/SFC_TEST_ADD)", true)),
        Tool("read_api_script", "读取已有 C# 脚本接口的源码（学习写法/改写参考）。返回 SCRIPTCODE/SCRIPTNAME/SOURCECODE/VERSION。",
          P("scriptCode", "string", "脚本编码(如 SC_SCRIPT_CHECK/SC_SAMPLE_DELETE)", true)),
        Tool("read_module_template", "读取业务模板市场里的模板详情（学习完整模块的元数据组织方式：资源/字段/过滤器/UI/接口/页面/按钮/菜单）。先用 search_module_template 找到 TEMPLATECODE。返回模板信息+元数据脚本(超长截断)。",
          P("templateCode", "string", "模板编码(如 TPL_LOGISTICS)", true)),
        Tool("search_module_template", "按关键词搜索业务模板市场里的模板。返回匹配的模板列表(TEMPLATECODE/TEMPLATENAME/CATEGORY/DESCRIPTION)。选中后再用 read_module_template 读取详情。",
          P("keyword", "string", "搜索关键词(如 物流/样品/设备)", true)),
        // ---- A 组：物理表 + 字段 ----
        Tool("create_physical_table", "创建物理表 + 注册 TBS resource + 注册 resfield 元数据。fields 每项 {name,type,length,nullable,comment,isKey}。产出 CREATE TABLE SQL + 元数据，不直接写库。字段名必须大写无下划线。",
          P("tableName", "string", "物理表名(如 tbs_logistics)", true),
          P("fields", "array", "字段数组，每项 {name,type,length,nullable,comment,isKey}。name 大写无下划线；type 如 VARCHAR/INT/DATETIME；isKey=true 的字段自动 KEYGENTYPE=GUID", true)),
        // add_field_to_table 已废弃：合并到 configure_resource_field(physicalColumnExists=false)
        // Tool("add_field_to_table", ...(已移除，保留 Execute case 仅为向后兼容引导))
        Tool("configure_resource_field", "操作资源配置表(tss_resfield)：给资源新增/修改字段定义。**一个工具搞定所有加字段场景**。**TABLE 资源加字段自动检测物理列**（information_schema）：物理列不存在自动产出 ALTER TABLE ADD COLUMN，已存在只补 resfield——AI 无需判断、无需传 physicalColumnExists。**refFieldId 规则**：TABLE 资源字段不需要 refFieldId（REFFIELDID 必须为 NULL）；DATAVIEW 资源字段 refFieldId 由工具自动校验/填充（指向对应 TBS 字段，AI 无需传）。**refTableName（引用字段对）规则**：仅用于 DATAVIEW(VSS/VCK) 视图（通过 JOIN 取关联表字段，如 VSS_DEPT 加 MANAGERID+MANAGERNAME 关联员工）；**TABLE 资源是单表，禁止用 refTableName**（TABLE 字段直接对应物理列，加 MANAGERID 就用普通 VARCHAR(64)，不需要关联）。**时序校验**：给 DATAVIEW 加字段时，TBS 必须已有该字段（DB 已存在 或 同会话前序 DRAFT 项里有 都算）。",
          P("action", "string", "操作类型：add(新增)/update(修改)，默认 add", false),
          P("resourceId", "string", "目标资源 ID（tss_resource.ID）", true),
          P("fieldName", "string", "字段名（大写无下划线）", true),
          P("fieldAname", "string", "字段中文名", false),
          P("fieldType", "string", "字段类型 VARCHAR/INT/DATETIME/TEXT 等", false),
          P("fieldLength", "integer", "字段长度", false),
          P("nullable", "boolean", "是否可空", false),
          P("isKey", "boolean", "是否主键（仅 add 有效，自动 KEYGENTYPE=GUID）", false),
          P("physicalColumnExists", "boolean", "物理列是否已存在。true=已存在只补 resfield（默认）；false=不存在需 ALTER TABLE 加列（仅 TABLE 资源有效，DATAVIEW 强制 true）", false),
          P("refFieldId", "string", "VSS(DATAVIEW) 字段必填：链向的 TBS 字段 ID（铁律）", false),
          P("refTableName", "string", "【引用字段模式】被引用的物理表名(如 tbs_dept)。传此参数表示创建引用字段对(自动产出 ID+NAME 两个字段，参考 VBS_EMP.DEPTID/DEPTNAME)", false),
          P("relation", "string", "【引用字段模式】JOIN 关系(如 A.DEPTID=B.ID)", false),
          P("nameFieldName", "string", "【引用字段模式】名称字段名(如 DEPTNAME)，自动建子引用字段+UPFIELDID", false)),
        // ---- B 组：视图 + 引用字段 ----
        Tool("define_dataview", "定义 DATAVIEW 资源 + resfield（REFFIELDID 链向 TBS 物理表字段）。**视图命名铁律：物理表名首字母 T 换成 V**——tbs_xxx→VBS_XXX，tck_xxx→VCK_XXX（如 tbs_project_fee→VBS_PROJECT_FEE）。fields 每项 {name,refFieldName(对应TBS字段名)}。产出元数据，无 SQL。",
          P("vckName", "string", "视图资源名，按 T→V 规则命名：tbs_ 表→VBS_XXX，tck_ 表→VCK_XXX", true),
          P("tableName", "string", "物理表名(TBS 资源的 TABLENAME)", true),
          P("fields", "array", "字段数组，每项 {name,refFieldName}。name=视图字段名，refFieldName=对应TBS物理表字段名（用于建 REFFIELDID 关联）", true)),
        // define_reference_field 已废弃（产出空 SQL，无实际变更），改用 configure_resource_field(refTableName=...) 的引用字段对模式
        // Tool("define_reference_field", ...(已移除，保留 Execute case 仅为向后兼容))

        // ---- C 组：UI 配置 ----
        Tool("configure_ui_field", "配置字段的 UI（一个字段一条 resuipc 记录，同时含列表列+表单控件配置）。**合并了原 configure_list_column + configure_form_field**——一个字段只调一次本工具，列表+表单一次配全，避免产出两条记录。DB 已有该字段 resuipc 则 UPDATE，无则 INSERT。editType 全量支持：text/textarea/number/datepicker/select/multiselect/autocomplete/multiautocomplete/treepicker/checkbox/fileupload/imageupload/code/editor。selectData 约定：select/multiselect=字典名或 k:v 内联(如 1:是,0:否)；autocomplete/treepicker=选择器配置 JSON(如 {\"module\":\"B01_M01\",\"apiCode\":\"A01\",\"keyName\":\"ID\",\"titleName\":\"DEPTNAME\",\"paramMappings\":{}})；multiautocomplete 需加 mode(subtable|field)+subMappings；fileupload/imageupload={\"multifile\":true} 或 subtable 模式；code={\"language\":\"sql|csharp|javascript\"}。",
          P("fieldId", "string", "resfield ID", true),
          P("labelName", "string", "列标题/字段中文名（列表+表单共用）", false),
          P("listSort", "integer", "列表列排序(从1开始，0=不在列表显示)", false),
          P("showLength", "integer", "列表列宽(字符数)", false),
          P("editType", "string", "表单控件类型(text/textarea/number/datepicker/select/multiselect/autocomplete/multiautocomplete/treepicker/checkbox/fileupload/imageupload/code/editor)", false),
          P("selectData", "string", "select 类型时字典名或 k:v 内联(如 '1:是,0:否')；autocomplete/treepicker 时选择器配置 JSON", false),
          P("editSort", "integer", "表单排序(0=不在表单显示)", false),
          P("updateFields", "string", "联动更新字段(逗号分隔)", false)),
        // ---- D 组：字典（先查现有，找不到用 create_dict 创建）----
        Tool("search_dict", "按关键词搜索已有字典。需字典的字段(EDITTYPE=select)先调此工具查，找到就在 SELECTDATA 引用字典名；找不到再调 create_dict 创建。",
          P("keyword", "string", "字典名关键词(如 单据状态/物流方式)", true)),
        Tool("create_dict", "创建字典 + 字典项（仅在 search_dict 找不到匹配字典时使用）。items 每项 {value,name}。产出 tss_dict + tss_dictitem INSERT SQL。",
          P("dictName", "string", "字典名(如 物流方式)", true),
          P("items", "array", "字典项数组，每项 {value,name}", true)),
        // ---- E 组：过滤器 ----
        Tool("define_filter", "定义过滤器：校验三条铁律(1=1开头/@INPUT/ORDERBY无别名)，产出 tss_resfilter INSERT SQL。FILTERSQL 用 NVelocity 模板，参数用 @VAR，禁止单引号。",
          P("resourceId", "string", "资源 ID", true),
          P("filterCode", "string", "过滤器编码(F00单条/F01列表/F02高级查询)", true),
          P("filterSql", "string", "NVelocity 模板 SQL，必须以 1=1 开头，F01/F02 必须用 @INPUT", true),
          P("orderBy", "string", "排序(无表别名前缀，如 ID DESC)", false)),
        // ---- F 组：模块 + API ----
        Tool("register_module", "注册模块：产出 tss_moudle INSERT SQL。",
          P("moduleCode", "string", "模块编码(如 R02_M07)", true),
          P("moduleName", "string", "模块名(如 物流管理)", true)),
        Tool("define_api", "定义模块接口：产出 tss_moudleapi INSERT SQL。ACTIONCODE 不能为空（前端 getApi 靠它找接口）。",
          P("moduleCode", "string", "模块编码", true),
          P("apiCode", "string", "接口编码(如 A01query/A02open/A04save)", true),
          P("apiType", "string", "接口类型(query/open/save/delete/submit/check/verify/batchSubmit等)", true),
          P("pathName", "string", "数据源路径名(QRY/QQRY/MAIN/SEL/DTSA等)", true),
          P("filterCode", "string", "过滤器编码(如 F01)", false),
          P("actionCode", "string", "动作编码(query/open/save/delete/advQuery等)，不能为空", true),
          P("apiParam", "string", "接口参数(JSON 字符串)", false)),
        Tool("define_sql_api", "定义 SQL 脚本接口（tss_sql + tss_moudleapi APITYPE=sql）：自定义业务操作（状态流转/批量更新/计算回写/多语句事务）不用写 C# Controller，运行时由 DataController 单事务执行。**铁律**：sqlTxt 禁止单引号（NVelocity 限制）；禁止 DDL（DROP/ALTER/TRUNCATE/CREATE/GRANT/REVOKE/RENAME）；参数用 @VAR；系统变量 @_USERID_/@_EMPID_/@_DEPTID_ 自动注入；IN 列表写 IN (@IDS)；多语句分号分隔，任一失败整体回滚；查询型接口最后一条 SELECT 的结果集返回给前端。产出两条 INSERT，不直接写库。",
          P("moduleCode", "string", "模块编码", true),
          P("apiCode", "string", "接口编码(如 A23)", true),
          P("apiName", "string", "接口名称(如 批量完成)", true),
          P("actionCode", "string", "动作编码(如 batchDone)，不能为空", true),
          P("sqlCode", "string", "SQL模板编码(tss_sql.SQLCODE，如 SS_ACCEPT_DONE)", true),
          P("sqlTxt", "string", "NVelocity SQL 模板（禁单引号，参数 @VAR，可多语句分号分隔）", true),
          P("remark", "string", "备注", false)),
        Tool("define_script_api", "定义 C# 脚本接口（tss_api_script + tss_moudleapi APITYPE=csharp）：复杂自定义逻辑（循环/条件/多步/事务）在线编写、保存即生效（Roslyn 运行时编译），不用写 C# Controller。**脚本上下文（直接可用）**：P(\"参数名\")取参数；UserId/UserInfo 当前用户；Db(\"SELECT ...\", new {参数}) 查询；DbFirst 首行；DbScalar 标量；DbExec(\"UPDATE/INSERT/DELETE ...\", new {参数}) 执行；Sql(\"SQLCODE\", hashtable) 调 tss_sql 模板；using (var t = Trans()) { DbExec(...); t.Commit(); } 事务；MD 模块配置；Operate ORM 操作；Log(\"msg\") 日志；Response.SetData(obj) 返回数据；Response.SetError(\"msg\") 返回错误。**铁律**：SQL 必须 @参数化（防注入）；禁 DDL；return 提前返回可用。**源码会做 Roslyn 编译检查，失败返回错误让你修正后重调**。示例：var id=P(\"ID\"); using(var t=Trans()){ DbExec(\"UPDATE tbs_x SET STATE=2 WHERE ID=@id\", new { id }); t.Commit(); } Response.SetData(new { affected = 1 });",
          P("moduleCode", "string", "模块编码", true),
          P("apiCode", "string", "接口编码(如 A51)", true),
          P("apiName", "string", "接口名称(如 退样)", true),
          P("actionCode", "string", "动作编码(如 backSample)，不能为空", true),
          P("scriptCode", "string", "脚本编码(tss_api_script.SCRIPTCODE，如 SC_ACCEPT_BACK)", true),
          P("scriptName", "string", "脚本名称", true),
          P("sourceCode", "string", "完整 C# 脚本源码", true),
          P("remark", "string", "备注", false)),
        Tool("define_script_flow_api", "定义声明式多步骤编排接口（tss_moudleapi APITYPE=script, APIPARAM=步骤JSON）：将多个 SQL/查询步骤串起来，步骤间共享变量，支持条件跳转。适用场景：先查后更新、多步事务、条件分支等。步骤类型：sql(执行SQL模板,sqlCode必填)、query(调模块查询,apiCode必填)、if(条件跳转,cond+goto必填)、update(同sql，语义区分)、return(指定返回数据,data选填)。条件语法：变量.属性(如 result1.affected)、比较(>/</>=/<=/==/!=)、逻辑(&&/||/!)。产出 tss_moudleapi INSERT（APITYPE=script, APIPARAM=steps JSON），不直接写库。",
          P("moduleCode", "string", "模块编码", true),
          P("apiCode", "string", "接口编码(如 A52)", true),
          P("apiName", "string", "接口名称(如 审核并回写)", true),
          P("actionCode", "string", "动作编码(如 checkAndWriteback)，不能为空", true),
          P("steps", "array", "步骤数组JSON。每项 {type,sqlCode?,apiCode?,output?,cond?,goto?,data?}。示例: [{type:sql,sqlCode:SS_XXX,output:r1},{type:if,cond:r1.affected>0,goto:3},{type:update,sqlCode:SS_YYY},{type:return,data:r1}]", true),
          P("remark", "string", "备注", false)),
        Tool("update_script_flow_api", "修改已有编排接口的步骤配置（tss_moudleapi APITYPE=script, UPDATE APIPARAM=新步骤JSON）。用于调整已有编排接口的步骤，不新建接口。先调 read_script_flow_api 读取当前步骤，再调本工具修改。产出 UPDATE SQL，不直接写库。",
          P("apiId", "string", "tss_moudleapi.ID（从 read_script_flow_api 获取）", true),
          P("steps", "array", "新的步骤数组JSON（完整替换，非增量）。每项 {type,sqlCode?,apiCode?,output?,cond?,goto?,data?}", true),
          P("apiName", "string", "新接口名称(可选，不传则不更新)", false),
          P("actionCode", "string", "新动作编码(可选，不传则不更新)", false)),
        Tool("read_script_flow_api", "读取已有编排接口的步骤配置。返回 apiId/apiCode/apiName/actionCode/steps。修改编排接口前先调此工具读取当前状态。",
          P("moduleCode", "string", "模块编码", true),
          P("apiCode", "string", "接口编码", true)),
        Tool("create_menu", "创建菜单：产出 tss_func INSERT SQL。**OUTERURL 规则**：通用模板模块必须写 /g/{MODULECODE}/main（如 /g/R02_M07/main）；本地手写页面写路由名（如 r02/m07）；SFC 在线页面写 {业务}/{模块}/online/{view名}。",
          P("funcCode", "string", "菜单编码(如 R02_M07)", true),
          P("funcName", "string", "菜单名(如 物流管理)", true),
          P("outerUrl", "string", "前端路由路径(通用模板模块写 /g/{MODULECODE}/main)", true),
          P("upFuncId", "string", "上级菜单 ID", true)),
        Tool("create_funcpoints", "创建功能点权限：产出 tss_funcpoint INSERT SQL。points 如 ['A01','A03','A04']。",
          P("funcCode", "string", "所属菜单编码", true),
          P("points", "array", "功能点编码数组(如 ['A01','A03','A04'])", true)),
        // ---- I 组：页面 + 按钮（tss_module_page/tss_module_button，GenericModule 页面清单，低代码闭环最后一环）----
        Tool("define_page", "定义模块页面(tss_module_page)，通用模板(GenericModule)按此配置自动渲染页面并注册动态路由(/g/{MODULECODE}/{PAGECODE})。**一个标准模块至少两个页面**：①列表页 pageCode=main, pageType=list（菜单 OUTERURL=/g/{MODULECODE}/main 指向它，pageConfig 配 {\"defaultFormPageCode\":\"form\"}）②表单页 pageCode=form, pageType=form（列表页内嵌 rs-modal 弹窗打开，routePath 留空，**PARENTID 自动挂到同模块 list 页下**，也可显式传 parentId）。componentType 只允许 standard（通用模板渲染，默认）或 sfc（需 sfcModulePath，先用 create_sfc_module 产出 SFC 文件）——填 generic-module/generic-form 会导致路由不注册白屏。模块有 FLOWCODE 时审批按钮前端自动生成，页面只需配 CRUD 按钮。产出 tss_module_page INSERT，不直接写库。",
          P("moduleCode", "string", "模块编码", true),
          P("pageCode", "string", "页面编码(模块内唯一, 列表页用 main, 表单页用 form)", true),
          P("pageName", "string", "页面名称(如 列表页/编辑页)", true),
          P("pageType", "string", "页面类型: list/form/select/review/report", true),
          P("routePath", "string", "本地路由路径(通用模板页通常留空)", false),
          P("componentType", "string", "standard(默认)/sfc", false),
          P("sfcModulePath", "string", "SFC组件路径(componentType=sfc 时必填)", false),
          P("queryApiCode", "string", "list页查询接口编码(默认 A01)", false),
          P("openApiCode", "string", "form页打开接口编码(默认 A02)", false),
          P("saveApiCode", "string", "form页保存接口编码(默认 A04)", false),
          P("pageConfig", "string", "PAGECONFIG JSON 字符串。列表页必配 {\"defaultFormPageCode\":\"form\"}；可选 QRYPATH/QQRYSPATH/MAINPATH/SELECTMODE/SLOTS/EXTENDJS", false),
          P("sortNo", "integer", "排序号(默认0, 列表页应排在表单页前)", false)),
        Tool("define_button", "定义页面按钮(tss_module_button)。**标准CRUD约定**：列表页 header 加\"添加\"(btnCode=add, apiCode=A04, permCode={MODULECODE}/A04)；表单页 footer 加\"保存\"(btnCode=save, apiCode=A04)+\"取消\"(btnCode=cancel, 无需 apiCode)。**审批流按钮(提交/审核/审批/撤销)不要配**——模块有 FLOWCODE 时前端自动生成。btnCode 预设: add/edit/select/delete/save/export/submit/reSubmit/check/reCheck/verify/reVerify/subAdd/subRemove/subUp/subDown/cancel/custom。actionType: api(调模块接口,默认)/openForm/openSelector(后两者需 extParam 配 formPageCode/selectPageCode)。产出 tss_module_button INSERT，不直接写库。",
          P("moduleCode", "string", "模块编码", true),
          P("pageCode", "string", "目标页面编码(如 main/form)", true),
          P("btnName", "string", "按钮显示名称", true),
          P("btnArea", "string", "按钮区域: header(列表顶部)/footer(底部)/row(行操作列)/DTSA等(子表工具栏)", true),
          P("btnCode", "string", "预设按钮编码(默认 custom)", false),
          P("apiCode", "string", "关联接口编码(btnCode=custom 或审批流类时必填)", false),
          P("interactType", "string", "交互类型: direct(默认)/poptip(二次确认)", false),
          P("poptipText", "string", "poptip 确认提示文字", false),
          P("showCond", "string", "显隐条件表达式(如 STATE===1&&CREATEID==_USERID_；列表页可用 _checks_.every(r=>r.STATE===1))", false),
          P("permCode", "string", "权限编码(如 R02_M07/A04)", false),
          P("color", "string", "颜色: primary/red/blue", false),
          P("icon", "string", "图标", false),
          P("actionType", "string", "api(默认)/openForm/openSelector", false),
          P("extParam", "string", "扩展参数 JSON 字符串(openMode/formPageCode/selectPageCode/extraParams/beforeAction/afterAction)", false),
          P("sortNo", "integer", "排序号", false)),
        // ---- H 组：SFC 在线模块（前端 Vue 页面，不走传统元数据建表）----
        Tool("create_sfc_module", "创建 SFC 在线模块文件（存入 tbs_sfc_template，运行时由 SFC loader 加载）。**适用于纯前端展示页/自定义复杂 UI/非标准 CRUD 的页面**。不走传统元数据建表（无需 tss_resource/tss_resfield）。\n**标准三件套（必须全部产出）**：① store.js（Vuex store，存数据/动作）② main.vue（列表页，依赖 ['./add.vue','../store']）③ add.vue（编辑表单页，依赖 ['../store','@/mixins/add01']）。参考 SFC_TEST_STORE/SFC_TEST_MAIN/SFC_TEST_ADD。\n**MODULEPATH 规则**：@/pages/{业务}/{模块}/views/{view}.vue（如 @/pages/r02/m07/views/main.vue），store.js 在 @/pages/{业务}/{模块}/store.js。\n**菜单 OUTERURL 规则（create_menu 用）**：{业务}/{模块}/online/{view名}（如 r02/m07/online/main）—— 不是 modulePath 直接转换，是路由约定（s01/m16/online/main → @/pages/s01/m16/views/main.vue）。store.js 无需菜单。\nCOMPILEDCODE 留空，前端 loader 运行时编译 SOURCECODE。",
          P("templateCode", "string", "模板编码(唯一，如 SFC_DEVICE_MAIN/SFC_DEVICE_ADD/SFC_DEVICE_STORE)", true),
          P("templateName", "string", "模板名称(如 设备管理-列表页)", true),
          P("modulePath", "string", "模块路径 main.vue/add.vue: @/pages/{业务}/{模块}/views/{view}.vue；store.js: @/pages/{业务}/{模块}/store.js", true),
          P("fileType", "string", "文件类型 VUE 或 JS(store 用 JS，默认 VUE)", false),
          P("sourceCode", "string", "完整源码。VUE: <template>...</template><script>...</script><style>...</style>；JS(store): export default createStore(...)", true),
          P("deps", "string", "依赖路径 JSON 数组。main.vue: [\"./add.vue\",\"../store\"]；add.vue: [\"../store\",\"@/mixins/add01\"]；store.js: [\"@/store/createStore\"]", false),
          P("description", "string", "描述(可选)", false)),
        // ---- V 组：校验工具(2026-07-19 接入记忆中枢 self-correction loop) ----
        // 让 AI 产出代码后主动调用 verify_* 看问题, 在对话里自我修正, 形成"生成→校验→修正→确认"闭环。
        Tool("verify_sfc", "校验 SFC 源码(.vue)常见错误。检查项: ①<template>/<script>/<style>结构完整 ②字段名/属性名禁下划线 ③import 路径合法性 ④DataTable/store 调用模式。产出 SFC 代码后必须调用一次, 看到 issues 必须修正后再退出。",
          P("sourceCode", "string", "完整 SFC 源码", true)),
        Tool("verify_sql", "校验 SQL 模板(NVelocity)常见错误。检查项: ①禁单引号(NVelocity 解析失败) ②LIKE 用 CONCAT ③参数语法(@VAR/Dapper 或 $!{VAR}/NVelocity 一致) ④DDL 黑名单 ⑤注释分号断裂。产出 SQL 模板后必须调用一次。",
          P("sqlText", "string", "SQL 模板原文", true)),
        Tool("verify_metadata", "校验 ORM 元数据 SQL/JSON 配置常见错误。检查项: ①ORDERBY 一律不带表别名 ②字段名大写无下划线 ③tss_ 系统表禁 ISDELETED 列 ④F00 必须是 A.ID=@ID ⑤FIELDTYPE 用 MySQL 原生类型。生成建表/资源/过滤器 SQL 后必须调用。",
          P("content", "string", "待校验的 SQL 或 JSON 配置", true),
          P("kind", "string", "类型: sql_resource(资源/字段/过滤器 SQL) / moudleapi / pageconfig_json, 默认自动识别", false))
      };
      return MergeDeclarative(defs, "dev");
    }

    /// <summary>表单填报专用工具集（get_module_schema + query_data + search_menu + fill_form + fill_subtable）</summary>
    public static List<object> GetFormFillToolDefinitions()
    {
      var defs = new List<object>
      {
        Tool("get_module_schema", "获取指定模块的字段定义（含 refFields 引用关系）。填报前必调，了解有哪些字段、哪些是引用字段。",
          P("moduleCode", "string", "模块编号", true)),
        Tool("query_data", "查询模块数据。用于把用户说的名称解析成ID（如客户名→CUSTID+CUSTNAME），填引用字段时用。",
          P("moduleCode", "string", "模块编号", true),
          P("filter", "object", "过滤条件", false),
          P("pageSize", "integer", "每页条数", false)),
        Tool("search_menu", "按关键词搜索模块（找引用字段对应的模块时用）。",
          P("keyword", "string", "关键词", true)),
        Tool("fill_form", "把收集/解析到的值填入当前表单主表。fields 的 key 必须是 get_module_schema 返回的大写字段名(name)，原样使用、区分大小写（如 CUSTCODE 不是 custcode）。不同类型字段的填值规则：text/textarea/editor/code 直接填字符串；number 填数字；datepicker 填 YYYY-MM-DD；checkbox 填 1(是)或0(否)；select 填字典key（get_module_schema 返回的 selectOptions 里有可选值列表，直接匹配名称→key，不需要 query_data）；autocomplete/treepicker 必须同时填 ID 字段和显示名字段（如 CUSTID='xxx', CUSTNAME='ABC公司'），通过 query_data 查出ID。可多次调用增量填充。",
          P("fields", "object", "字段键值对，key 必须大写，如 {CUSTID:'...', CUSTNAME:'XX', BILLDATE:'2026-06-23', ISACTIVE:1}", true)),
        Tool("fill_subtable", "把子表数据填入当前表单的子表（明细行）。path 是子表路径名（如 DTSA），rows 是行数组，每行是 {字段名:值}。子表字段定义在 get_module_schema 返回的 subTables[] 里。添加子表行时，先调 fill_subtable({path:'DTSA', rows:[{字段:值}]})。",
          P("path", "string", "子表路径名，如 DTSA/DTSB/DTSC", true),
          P("rows", "array", "子表行数组，每行是 {字段名:值}，如 [{ITEMNAME:'万用表', QTY:1}, {ITEMNAME:'示波器', QTY:2}]", true)),
        Tool("query_stats", "统计分析：在模块数据上做聚合统计(COUNT/SUM/AVG/GROUP BY)。底层用 ORM 自动构建正确查询(表/JOIN/数据权限)，你只需提供 select/groupBy 等，引用字段名(来自 get_module_schema 的 fields[].name)，无需关心表名/JOIN。查子表数据时传path(子表路径名如DTSA)，不传path查主表。",
          P("moduleCode", "string", "模块编号", true),
          P("select", "string", "SELECT 列表，引用字段名，可含聚合。如 'DEPTNAME, COUNT(*) AS cnt'", true),
          P("path", "string", "子表路径名(可选，如DTSA)。传则查子表", false),
          P("groupBy", "string", "GROUP BY 字段名，如 'DEPTNAME'", false),
          P("where", "string", "过滤(子查询层)，引用字段名，如 'ISDELETED=0'", false),
          P("orderBy", "string", "排序，如 'cnt DESC'", false),
          P("having", "string", "HAVING 条件", false))
      };
      return MergeDeclarative(defs, "formfill");
    }

    /// <summary>
    /// 合并声明式工具定义（tss_ai_tool，同名内置优先）。
    /// 三个静态 GetXxxToolDefinitions 出口统一调用，覆盖 Hub/向导/AiDev/SFC 全部调用方。
    /// 同时用 DB 中 builtin 工具的 DESCRIPTION/PARAMS 覆盖 C# 代码定义（配置中心可在线修改）。
    /// skipOverrides=true 时跳过 DB 覆盖（BuiltinToolSync 同步时用，避免启动时循环）。
    /// </summary>
    private static List<object> MergeDeclarative(List<object> defs, string setName, bool skipOverrides = false)
    {
      // 用 DB 中的 builtin 覆盖值替换 C# 定义的描述和参数
      if (!skipOverrides)
      {
        var overrides = DeclarativeToolProvider.GetBuiltinOverrides();
        if (overrides.Count > 0)
        {
          for (int i = 0; i < defs.Count; i++)
          {
            try
            {
              var jo = defs[i] is JObject j ? j : JObject.FromObject(defs[i]);
              var fn = jo["function"];
              if (fn == null) continue;
              string name = fn["name"]?.ToString();
              if (name != null && overrides.TryGetValue(name, out var ov))
              {
                fn["description"] = ov.Description;
                fn["parameters"] = ov.Parameters;
                defs[i] = jo;
              }
            }
            catch { }
          }
        }
      }
      // 追加声明式 SQL 工具（同名不覆盖内置）
      var names = new HashSet<string>();
      foreach (var d in defs) ExtractName(d, names);
      foreach (var d in Realso.WebAPI.Services.Agent.DeclarativeToolProvider.GetDefinitions(setName))
      {
        var tmp = new HashSet<string>();
        ExtractName(d, tmp);
        foreach (var n in tmp)
        {
          if (names.Add(n)) defs.Add(d);
        }
      }
      return defs;
    }

    // ============ IToolExecutor 实现 ============

    /// <summary>该执行器负责的所有工具名（三个 set 合并去重）</summary>
    public IEnumerable<string> GetToolNames()
    {
      var names = new HashSet<string>();
      foreach (var def in GetToolDefinitions())
        ExtractName(def, names);
      foreach (var def in GetDevToolDefinitions())
        ExtractName(def, names);
      foreach (var def in GetFormFillToolDefinitions())
        ExtractName(def, names);
      return names;
    }

    private static void ExtractName(object def, HashSet<string> names)
    {
      try
      {
        var jo = def is JObject j ? j : JObject.FromObject(def);
        var name = jo["function"]?["name"]?.ToString();
        if (!string.IsNullOrEmpty(name)) names.Add(name);
      }
      catch { }
    }

    /// <summary>按 set 取工具定义：assistant(通用)/formfill(填报)/dev(开发)。null 返回通用集。</summary>
    public List<object> GetDefinitionsBySet(string setName, ToolKind? filter = null)
    {
      // filter 现阶段不细分（工具定义未标 Kind），按 set 返回
      if (string.IsNullOrEmpty(setName) || setName == "assistant") return GetToolDefinitions();
      if (setName == "formfill") return GetFormFillToolDefinitions();
      if (setName == "dev") return GetDevToolDefinitions();
      return GetToolDefinitions();
    }

    /// <summary>默认返回通用工具集</summary>
    public List<object> GetDefinitions(ToolKind? filter = null) => GetToolDefinitions();

    /// <summary>判断是否前端工具（替代 AssistantHub.FRONTEND_TOOLS HashSet）</summary>
    public bool IsFrontendTool(string toolName)
    {
      return !string.IsNullOrEmpty(toolName) && FRONTEND_TOOLS.Contains(toolName);
    }

    /// <summary>
    /// IToolExecutor.Execute：包装现有 Execute(toolName, args, changesetId)，
    /// 把 object 结果包装成 ToolResult。特殊结果类型（NavigateResult/FillResult/SubTableResult）
    /// 原样放入 Data，AgentEngine 的 OnToolResultAsync 钩子识别后走对应 sink 回调。
    /// </summary>
    public Task<ToolResult> Execute(string toolName, JObject args, ToolContext ctx)
    {
      string changesetId = ctx?.ChangeSetId;
      object result = Execute(toolName, args, changesetId);
      var tr = new ToolResult { Data = result };
      // 开发场景产出类工具：标记为变更项候选（DevAgentEngine 据此调 TryBuildChangeItem）
      if (result != null)
      {
        var jo = result as JObject;
        if (jo == null)
        {
          try { jo = JObject.FromObject(result); } catch { }
        }
        if (jo != null)
        {
          var sql = jo["sql"]?.ToString();
          if (!string.IsNullOrEmpty(sql)) { tr.Sql = sql; tr.IsChangeItem = true; }
          tr.Metadata = jo["metadata"]?.ToString();
        }
      }
      return Task.FromResult(tr);
    }

    /// <summary>执行工具，返回结果对象（喂回 LLM）。
    /// changesetId 用于工具校验时序：同会话前序工具产出的 DRAFT 变更项还没写库，
    /// 但通过 changesetId 可在 tss_aidev_changeitem.METADATA 里查到（虚拟 DB 状态），
    /// 用于跨工具时序校验（如 configure_resource_field(DATAVIEW) 校验 TBS 字段是否已加）。
    /// </summary>
    public object Execute(string toolName, JObject args, string changesetId = null)
    {
      try
      {
        // 声明式 SQL 工具（tss_ai_tool, EXECUTORTYPE=sql）按名分发
        // builtin 工具虽也同步到 tss_ai_tool，但 HasTool 已排除 builtin，不会误路由
        if (Realso.WebAPI.Services.Agent.DeclarativeToolProvider.HasTool(toolName))
        {
          var dtr = Realso.WebAPI.Services.Agent.DeclarativeToolProvider.Execute(
            toolName, args, new Realso.WebAPI.Services.Agent.ToolContext { UserInfo = _userInfo, ChangeSetId = changesetId }).GetAwaiter().GetResult();
          return dtr.Data;
        }
        switch (toolName)
        {
          case "search_menu": return SearchMenu(args["keyword"]?.ToString());
          case "get_module_schema": return GetModuleSchema(args["moduleCode"]?.ToString());
          case "query_data": return QueryData(args);
          case "query_stats": return QueryStats(args);
          case "navigate": return Navigate(args["moduleCode"]?.ToString(), args["id"]?.ToString());
          case "open_record": return OpenRecord(args["moduleCode"]?.ToString(), args["id"]?.ToString());
          case "fill_form": return FillForm(args["fields"] as JObject);
          case "fill_subtable": return FillSubTable(args["path"]?.ToString(), args["rows"] as JArray);
          // AI 开发助理只读工具：复用旧资源检查 + 读物理表结构
          case "search_existing_resource": return SearchExistingResource(args["tableName"]?.ToString());
          case "read_table_schema": return ReadTableSchema(args["tableName"]?.ToString());
          case "read_sfc_template": return ReadSfcTemplate(args);
          case "read_api_script": return ReadApiScript(args["scriptCode"]?.ToString());
          case "read_module_template": return ReadModuleTemplate(args["templateCode"]?.ToString());
          case "search_module_template": return SearchModuleTemplate(args["keyword"]?.ToString());
          // AI 开发助理产出类工具（不直接写库，返回 {sql,metadata} 由 Orchestrator 转 ChangeItem）
          case "create_physical_table": return CreatePhysicalTable(args);
          case "add_field_to_table": return new { error = "add_field_to_table 已合并到 configure_resource_field(physicalColumnExists=false)，请改用 configure_resource_field" };
          case "configure_resource_field": return ConfigureResourceField(args, changesetId);
          case "define_dataview": return DefineDataview(args, changesetId);
          case "define_reference_field": return new { error = "工具 define_reference_field 已废弃，请改用 configure_resource_field(refTableName=..., relation=..., nameFieldName=...) 创建引用字段对" };
          case "configure_ui_field": return ConfigureUiField(args, changesetId);
          case "configure_list_column": return new { error = "configure_list_column 已合并到 configure_ui_field，请改用 configure_ui_field（一次配全列表+表单）" };
          case "configure_form_field": return new { error = "configure_form_field 已合并到 configure_ui_field，请改用 configure_ui_field（一次配全列表+表单）" };
          case "search_dict": return SearchDict(args["keyword"]?.ToString());
          case "create_dict": return CreateDict(args);
          case "define_filter": return DefineFilter(args);
          case "register_module": return RegisterModule(args);
          case "define_api": return DefineApi(args, changesetId);
          case "define_sql_api": return DefineSqlApi(args, changesetId);
          case "define_script_api": return DefineScriptApi(args, changesetId);
          case "define_script_flow_api": return DefineScriptFlowApi(args, changesetId);
          case "update_script_flow_api": return UpdateScriptFlowApi(args, changesetId);
          case "read_script_flow_api": return ReadScriptFlowApi(args);
          case "create_menu": return CreateMenu(args);
          case "create_funcpoints": return CreateFuncpoints(args);
          case "define_page": return DefinePage(args, changesetId);
          case "define_button": return DefineButton(args, changesetId);
          case "create_sfc_module": return CreateSfcModule(args);
          // 校验工具(self-correction loop): AI 产出后主动校验, 看到 issues 必须修正
          case "verify_sfc": return VerifySfc(args["sourceCode"]?.ToString());
          case "verify_sql": return VerifySql(args["sqlText"]?.ToString());
          case "verify_metadata": return VerifyMetadata(args["content"]?.ToString(), args["kind"]?.ToString());
          default: return new { error = "未知工具: " + toolName };
        }
      }
      catch (System.Exception ex)
      {
        return new { error = ex.Message };
      }
    }

    // search_existing_resource：查 tss_resource 是否已有该表的资源注册。
    // 落实"复用旧资源"原则——同一表两套资源会导致冲突，已有则复用旧 ID。
    private object SearchExistingResource(string tableName)
    {
      if (string.IsNullOrEmpty(tableName)) return new { error = "tableName 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var rows = helper.Query<ResourceRow>(
          @"SELECT ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID
            FROM tss_resource
            WHERE TABLENAME=@tn
            ORDER BY RESOURCETYPE",
          new { tn = tableName });
        var list = new List<object>();
        foreach (var r in rows)
        {
          list.Add(new
          {
            id = r.ID,
            resourceName = r.RESOURCENAME,
            resourceAname = r.RESOURCEANAME,
            tableName = r.TABLENAME,
            resourceType = r.RESOURCETYPE,
            tableResourceId = r.TABLERESOURCEID
          });
        }
        return new
        {
          tableName,
          count = list.Count,
          resources = list,
          hint = list.Count > 0
            ? "已存在资源注册，生成新模块时必须复用上述 ID，不要新建同名资源（否则同一表两套资源会冲突）。旧资源有问题用 UPDATE 修复，不要 DELETE+INSERT。"
            : "无已有资源注册，可新建 TBS/VCK 资源。"
        };
      }
    }

    // read_table_schema：读物理表结构(SHOW COLUMNS FROM) + 已注册的 resfield。
    // read_sfc_template：读 SFC 标准模板源码（只读，AI 学习结构用；统一代码资产表，原 tbs_sfc_template 已并入）
    private object ReadSfcTemplate(JObject args)
    {
      string templateCode = args["templateCode"]?.ToString();
      if (string.IsNullOrEmpty(templateCode)) return new { error = "templateCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var t = helper.QueryFirstOrDefault<dynamic>(
          "SELECT CODE, NAME, MODULEPATH, FILETYPE, SOURCECODE, DEPS, REMARK FROM tss_code_asset WHERE ASSETTYPE IN ('js','vue') AND CODE=@tc AND ISDELETED=0",
          new { tc = templateCode });
        if (t == null) return new { error = "SFC 模板 " + templateCode + " 不存在" };
        return new
        {
          templateCode = (string)t.CODE,
          templateName = (string)t.NAME,
          modulePath = (string)t.MODULEPATH,
          fileType = (string)t.FILETYPE,
          sourceCode = (string)t.SOURCECODE,
          deps = (string)t.DEPS,
          description = (string)t.REMARK
        };
      }
    }

    // 用于了解现有表结构和字段定义，避免重复建表或字段。
    private object ReadTableSchema(string tableName)
    {
      if (string.IsNullOrEmpty(tableName)) return new { error = "tableName 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 1. SHOW COLUMNS FROM 物理表
        var columns = new List<object>();
        try
        {
          var colRows = helper.Query<ColumnRow>(
            "SHOW COLUMNS FROM " + tableName);
          foreach (var c in colRows)
          {
            columns.Add(new
            {
              field = c.Field,
              type = c.Type,
              nullable = c.Null == "YES",
              key = c.Key,
              defaultValue = c.Default,
              extra = c.Extra
            });
          }
        }
        catch (System.Exception ex)
        {
          return new { error = "读取表结构失败: " + ex.Message, tableName };
        }

        // 2. 已注册的 resfield（可能有多个资源指向同一表）
        var resFields = new List<object>();
        var resRows = helper.Query<ResourceRow>(
          @"SELECT ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE
            FROM tss_resource WHERE TABLENAME=@tn",
          new { tn = tableName });
        foreach (var res in resRows)
        {
          var frs = helper.Query<FieldRow>(
            @"SELECT FIELDNAME, FIELDANAME, FIELDTYPE, NULLABLE, FIELDLENGTH,
                     ISKEY, KEYGENTYPE, DEFAULTVALUE, REFRESOURCEID, REFRESOURCEANAME,
                     REFRELATION, REFFIELDID, UPFIELDID, ENTRYNUM
              FROM tss_resfield
              WHERE RESOURCEID=@rid
              ORDER BY ENTRYNUM",
            new { rid = res.ID });
          var fieldList = new List<object>();
          foreach (var fr in frs)
          {
            fieldList.Add(new
            {
              fieldName = fr.FIELDNAME,
              label = fr.LABEL,
              fieldType = fr.EDITTYPE,
              nullable = fr.NULLABLE,
              isKey = fr.ISKEY,
              keyGenType = fr.KEYGENTYPE,
              refResourceId = fr.REFRESOURCEID,
              refResourceAname = fr.REFRESOURCEANAME,
              refRelation = fr.REFRELATION,
              refFieldId = fr.REFFIELDID,
              upFieldId = fr.UPFIELDID,
              entryNum = fr.ENTRYNUM
            });
          }
          resFields.Add(new
          {
            resourceId = res.ID,
            resourceName = res.RESOURCENAME,
            resourceType = res.RESOURCETYPE,
            fields = fieldList
          });
        }

        return new
        {
          tableName,
          columns,
          registeredResources = resFields,
          hint = "columns 是物理表实际结构；registeredResources 是 ORM 元数据注册的字段定义。两者应一致，不一致说明元数据缺失。"
        };
      }
    }

    // search_menu：搜 tss_moudle
    private object SearchMenu(string keyword)
    {
      if (string.IsNullOrEmpty(keyword)) return new { error = "keyword 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var kw = "%" + keyword + "%";
        var rows = helper.Query<MoudleRow>(
          @"SELECT MODULECODE, MODULENAME, REMARK FROM tss_moudle
            WHERE MODULENAME LIKE @kw OR MODULECODE LIKE @kw
            ORDER BY MODULECODE LIMIT 10",
          new { kw });
        var list = new List<object>();
        foreach (var r in rows) list.Add(new { moduleCode = r.MODULECODE, moduleName = r.MODULENAME, remark = r.REMARK });
        return new { count = list.Count, modules = list };
      }
    }

    // get_module_schema：委托到 SfcModuleSchemaService，供 SFC AI 和助理工具共用
    private object GetModuleSchema(string moduleCode)
    {
      var schema = SfcModuleSchemaService.GetModuleSchema(moduleCode);
      // 用反射安全取字段（不用 dynamic，避免跨程序集匿名类型 RuntimeBinderException）
      if (schema == null) return new { error = "schema 为空" };
      var t = schema.GetType();
      var errProp = t.GetProperty("error");
      if (errProp != null)
      {
        var errVal = errProp.GetValue(schema, null);
        if (errVal != null) return schema;  // SfcModuleSchemaService 返回的 error 对象，原样返回
      }
      // 正常 schema：补充 hint 后返回（用反射取字段，避免 dynamic）
      var jo = new JObject();
      foreach (var p in t.GetProperties())
      {
        var val = p.GetValue(schema, null);
        if (val == null) jo[p.Name] = null;
        else jo[p.Name] = JToken.FromObject(val);
      }
      string tblName = jo["tableName"]?.ToString();
      jo["hint"] = "query_data 的 filter key 必须从 queryFilterParams 选；统计分析用 query_stats 直接写 SELECT（FROM " + (string.IsNullOrEmpty(tblName) ? "<tableName>" : tblName) + "），refFields 列出可 JOIN 的引用表。";
      return new { schema = jo };
    }

    // 获取资源的字段定义 — 委托到 SfcModuleSchemaService
    private (List<object> fields, List<object> refFields) GetResourceFields(DBHelper helper, string resourceId)
    {
      return SfcModuleSchemaService.GetResourceFields(helper, resourceId);
    }

    private static string ApiTypeDesc(string t)
    {
      switch (t)
      {
        case "query": return "列表查询";
        case "open": return "打开详情";
        case "save": return "保存(新增/修改)";
        case "delete": return "删除";
        case "submit": return "提交";
        case "reSubmit": return "重新提交";
        case "check": return "复核/审核";
        case "reCheck": return "撤销审核";
        case "verify": return "审批";
        case "reVerify": return "撤销审批";
        default: return t;
      }
    }

    // query_data：filter(JObject) → Hashtable → DataCallService.Query
    private object QueryData(JObject args)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };

      var filterParams = new Hashtable();
      var filter = args["filter"] as JObject;
      if (filter != null)
      {
        foreach (var prop in filter)
        {
          filterParams[prop.Key] = (prop.Value is JValue jv) ? jv.Value : prop.Value?.ToString();
        }
      }

      int pageSize = 10;
      if (args["pageSize"] != null) int.TryParse(args["pageSize"].ToString(), out pageSize);
      if (pageSize <= 0) pageSize = 10;
      if (pageSize > 500) pageSize = 500;  // 上限 500，防单次查询过大；进对话前还会 4KB 截断防 413

      var result = _dataCall.Query(moduleCode, filterParams, _userInfo, 1, pageSize);
      return new { total = result.TotalCount, count = result.Items.Count, rows = result.Items };
    }

    // open_record：DataCallService.Open（DataView 由 Newtonsoft 序列化，与 API 返前端一致）
    private object OpenRecord(string moduleCode, string id)
    {
      if (string.IsNullOrEmpty(moduleCode) || string.IsNullOrEmpty(id))
        return new { error = "moduleCode 和 id 不能为空" };
      var ht = _dataCall.Open(moduleCode, id, _userInfo);
      return new { moduleCode, id, data = ht };
    }

    // navigate：解析模块菜单的 OUTERURL → 列表页路由。所有变更(增删改审批)都跳转到真实页面操作。
    // 返回 NavigateResult（hub 据此推 navigate 块给前端 router.push）。
    private object Navigate(string moduleCode, string id)
    {
      if (string.IsNullOrEmpty(moduleCode))
        return new NavigateResult { navigated = false, error = "moduleCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var row = helper.QueryFirstOrDefault<dynamic>(
          "SELECT FUNCNAME, OUTERURL FROM tss_func WHERE FUNCCODE=@mc AND ISHIDE=0 LIMIT 1", new { mc = moduleCode });
        if (row == null || string.IsNullOrEmpty((string)row.OUTERURL))
          return new NavigateResult { navigated = false, error = "模块 " + moduleCode + " 无菜单入口，无法跳转" };
        // 与菜单 select() 跳转规则一致：g/ 或 /g/ 开头视为完整路径（通用模块，仅补前导斜杠），
        // 否则视为本地页面路由名（如 b01/m05），补 /main 列表页
        string outerUrl = ((string)row.OUTERURL).Trim();
        string path;
        if (outerUrl.StartsWith("g/") || outerUrl.StartsWith("/g/"))
        {
          path = outerUrl.StartsWith("/") ? outerUrl : "/" + outerUrl;
        }
        else
        {
          path = "/" + outerUrl.TrimStart('/') + "/main";
        }
        return new NavigateResult
        {
          navigated = true,
          path = path,
          id = id,
          moduleCode = moduleCode,
          moduleName = (string)row.FUNCNAME
        };
      }
    }

    public class NavigateResult
    {
      public bool navigated;
      public string path;
      public string id;
      public string moduleCode;
      public string moduleName;
      public string error;
    }

    // fill_form：把字段值填入当前表单。返回 FillResult（hub 据此推 fill 块给前端 setValue）。
    private object FillForm(JObject fields)
    {
      var dict = new Dictionary<string, object>();
      if (fields != null)
      {
        foreach (var p in fields)
        {
          dict[p.Key] = (p.Value is JValue jv) ? jv.Value : p.Value?.ToString();
        }
      }
      if (dict.Count == 0) return new { error = "fields 不能为空" };
      return new FillResult { fields = dict };
    }

    // fill_subtable：把子表数据填入当前表单。返回 SubTableResult（hub 据此推 subtable 块给前端）。
    private object FillSubTable(string path, JArray rows)
    {
      if (string.IsNullOrEmpty(path)) return new { error = "path 不能为空" };
      if (rows == null || rows.Count == 0) return new { error = "rows 不能为空" };
      var rowList = new List<Dictionary<string, object>>();
      foreach (var row in rows)
      {
        var dict = new Dictionary<string, object>();
        if (row is JObject jo)
        {
          foreach (var p in jo)
          {
            dict[p.Key] = (p.Value is JValue jv) ? jv.Value : p.Value?.ToString();
          }
        }
        rowList.Add(dict);
      }
      return new SubTableResult { path = path, rows = rowList };
    }

    public class FillResult
    {
      public Dictionary<string, object> fields;
    }

    public class SubTableResult
    {
      public string path;
      public List<Dictionary<string, object>> rows;
    }

    // query_stats：统计分析——用 ORM 的 BuildQuery 生成正确的 base SELECT（物理表+JOIN+权限），
    // 包成子查询，LLM 只在其上写聚合（GROUP BY/COUNT/SUM），引用字段名即可，杜绝臆造表名/JOIN。
    private object QueryStats(JObject args)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string path = args["path"]?.ToString();
      string select = args["select"]?.ToString();
      string where = args["where"]?.ToString();
      string groupBy = args["groupBy"]?.ToString();
      string having = args["having"]?.ToString();
      string orderBy = args["orderBy"]?.ToString();

      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrWhiteSpace(select)) return new { error = "select 不能为空（如 DEPTNAME, COUNT(*) AS cnt）" };

      // 校验 LLM 片段安全
      foreach (var frag in new[] { select, where, groupBy, having, orderBy })
      {
        if (!string.IsNullOrEmpty(frag) && !SafeFragment(frag, out string r))
          return new { error = "片段被拒绝(" + r + "): " + frag };
      }

      // 解析资源：传 path 查子表，否则查主表 query 资源
      string resourceId;
      string filterCode;
      if (!string.IsNullOrEmpty(path))
      {
        resourceId = ResolveSubTableResource(moduleCode, path);
        filterCode = null;  // 子表无独立过滤器，直接查物理表
      }
      else
      {
        var (rid, fc) = ResolveQueryResource(moduleCode);
        resourceId = rid;
        filterCode = fc;
      }
      if (resourceId == null) return new { error = "模块 " + moduleCode + (string.IsNullOrEmpty(path) ? " 无 query 资源" : " 无子表 " + path) };

      // ORM 构建 base SELECT
      string baseSql;
      List<string> fieldNames;
      try
      {
        Resource res = SchemaManage.GetResource(resourceId);
        fieldNames = new List<string>();
        foreach (var f in res.Fields) fieldNames.Add(f.FIELDNAME);
        QueryInfo qi = new QueryInfo { FilterCode = filterCode ?? "", PageSize = 1 };
        if (_userInfo != null)
        {
          qi.FilterParams["_USERID_"] = _userInfo["ID"];
          qi.FilterParams["_EMPID_"] = _userInfo["EMPID"];
          qi.FilterParams["_DEPTID_"] = _userInfo["DEPTID"];
        }
        baseSql = new BuildSQL01().BuildQuery(res, qi);
      }
      catch (System.Exception ex)
      {
        Realso.Utils.Logger.Error("[query_stats] 构建 base 查询失败: " + ex.Message + "\n" + ex.StackTrace);
        return new { error = "构建 base 查询失败: " + ex.Message, resourceId, filterCode, stack = ex.StackTrace };
      }

      // 包成子查询做聚合
      string full = "SELECT " + select + " FROM (" + baseSql + ") sub";
      if (!string.IsNullOrEmpty(where)) full += " WHERE " + where;
      if (!string.IsNullOrEmpty(groupBy)) full += " GROUP BY " + groupBy;
      if (!string.IsNullOrEmpty(having)) full += " HAVING " + having;
      if (!string.IsNullOrEmpty(orderBy)) full += " ORDER BY " + orderBy;
      full += " LIMIT 1000";

      try
      {
        // 提取 SQL 中所有 @参数，补默认值（系统变量取用户上下文，其余空串），防 "Parameter must be defined"
        var paramDict = new Dictionary<string, object>();
        foreach (System.Text.RegularExpressions.Match mm in System.Text.RegularExpressions.Regex.Matches(full, @"@([A-Za-z_][A-Za-z0-9_]*)"))
        {
          string v = mm.Groups[1].Value;
          if (paramDict.ContainsKey(v)) continue;
          if (v == "_USERID_" && _userInfo != null) paramDict[v] = _userInfo["ID"];
          else if (v == "_EMPID_" && _userInfo != null) paramDict[v] = _userInfo["EMPID"];
          else if (v == "_DEPTID_" && _userInfo != null) paramDict[v] = _userInfo["DEPTID"];
          else paramDict[v] = "";
        }
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var rows = helper.Query<dynamic>(full, paramDict);
          var list = new List<object>();
          foreach (var rr in rows) list.Add(rr);
          return new { count = list.Count, rows = list, sql = full };
        }
      }
      catch (System.Exception ex)
      {
        Realso.Utils.Logger.Error("[query_stats] SQL 执行失败: " + ex.Message + "\nSQL: " + full + "\n" + ex.StackTrace);
        return new
        {
          error = "SQL 执行失败: " + ex.Message,
          sql = full,
          availableFields = fieldNames,
          hint = "select/groupBy 里引用的字段名必须来自 availableFields（这是 ORM 子查询输出的列名）。"
        };
      }
    }

    // 解析模块的 query 资源ID + 过滤器编码。
    // 从模块接口定义找：先 ACTIONCODE='advQuery'(高级查询)，没有找 ACTIONCODE='query'(列表查询)，
    // 用该接口的 PATHNAME 找数据源 RESOURCEID，用该接口的 FILTERCODE 找过滤器。
    // 接口未定义 FILTERCODE 时，才从资源过滤器选 F02(高级)/F01(列表) 兜底。
    private (string resourceId, string filterCode) ResolveQueryResource(string moduleCode)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var mod = helper.QueryFirstOrDefault<dynamic>("SELECT ID FROM tss_moudle WHERE MODULECODE=@mc", new { mc = moduleCode });
        if (mod == null) return (null, null);
        string mid = (string)mod.ID;
        // 先找 advQuery 接口，没有找 query 接口
        var apiRow = helper.QueryFirstOrDefault<dynamic>(
          "SELECT PATHNAME, FILTERCODE FROM tss_moudleapi WHERE MODULEID=@mid AND ACTIONCODE='advQuery' LIMIT 1", new { mid });
        if (apiRow == null || string.IsNullOrEmpty((string)apiRow.PATHNAME))
        {
          apiRow = helper.QueryFirstOrDefault<dynamic>(
            "SELECT PATHNAME, FILTERCODE FROM tss_moudleapi WHERE MODULEID=@mid AND ACTIONCODE='query' LIMIT 1", new { mid });
        }
        if (apiRow == null || string.IsNullOrEmpty((string)apiRow.PATHNAME)) return (null, null);
        string pathname = (string)apiRow.PATHNAME;
        string filterCode = (string)apiRow.FILTERCODE;
        var pathRow = helper.QueryFirstOrDefault<dynamic>(
          "SELECT RESOURCEID FROM tss_moudlepath WHERE MODULEID=@mid AND PATHNAME=@p LIMIT 1", new { mid, p = pathname });
        if (pathRow == null || pathRow.RESOURCEID == null) return (null, null);
        string rid = (string)pathRow.RESOURCEID;
        // 接口未定义过滤器时，从资源过滤器选 F02/F01 兜底
        if (string.IsNullOrEmpty(filterCode))
        {
          var filters = helper.Query<string>("SELECT FILTERCODE FROM tss_resfilter WHERE RESOURCEID=@rid", new { rid });
          var fset = new HashSet<string>();
          foreach (var f in filters) fset.Add(f);
          filterCode = fset.Contains("F02") ? "F02" : (fset.Contains("F01") ? "F01" : null);
        }
        return (rid, filterCode);
      }
    }

    // 解析模块子表的资源ID（按 path，如 DTSA）。子表无独立过滤器，直接查物理表。
    private string ResolveSubTableResource(string moduleCode, string path)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var mod = helper.QueryFirstOrDefault<dynamic>("SELECT ID FROM tss_moudle WHERE MODULECODE=@mc", new { mc = moduleCode });
        if (mod == null) return null;
        string mid = (string)mod.ID;
        var pathRow = helper.QueryFirstOrDefault<dynamic>(
          "SELECT RESOURCEID FROM tss_moudlepath WHERE MODULEID=@mid AND PATHNAME=@p LIMIT 1", new { mid, p = path });
        if (pathRow == null || pathRow.RESOURCEID == null) return null;
        return (string)pathRow.RESOURCEID;
      }
    }

    // LLM 提供的 SQL 片段安全校验：禁分号/DML/DDL/子查询
    private static bool SafeFragment(string frag, out string reason)
    {
      reason = null;
      string s = frag.Trim();
      if (s.Contains(";")) { reason = "含分号"; return false; }
      if (System.Text.RegularExpressions.Regex.IsMatch(s,
        @"\b(SELECT|INTO|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|GRANT|REVOKE|EXEC|MERGE|REPLACE|LOAD_FILE|OUTFILE|DUMPFILE|CALL)\b",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
      {
        reason = "含禁止关键字"; return false;
      }
      return true;
    }

    // 私有方法：校验标识符（字段名/字段类型等）只允许大写字母数字下划线
    // 用于防 SQL 注入：LLM 传的 FIELDNAME/FIELDTYPE 在拼到 SQL 前先用此方法过滤
    // 字符串值（comment/labelName 等含中文/标点）继续用 .Replace("'","")，不走此方法
    private static string SafeIdentifier(string s)
    {
      if (string.IsNullOrEmpty(s)) return s;
      // 只允许 A-Z 0-9 _，其他全去掉
      var sb = new System.Text.StringBuilder();
      foreach (char c in s)
      {
        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_') sb.Append(c);
      }
      return sb.ToString();
    }

    // ============================================================
    // AI 开发助理产出类工具：每个工具产出 {sql, metadata} 结构化数据，
    // 由 Orchestrator 转成 ChangeItem 写入变更包（DRAFT 状态）。
    // 工具不直接写库，只查实际 ID 后组装 SQL + 元数据 JSON。
    // ============================================================

    // A1. create_physical_table：产出 CREATE TABLE + TBS resource + resfield 元数据
    private object CreatePhysicalTable(JObject args)
    {
      string tableName = args["tableName"]?.ToString();
      var fields = args["fields"] as JArray;
      if (string.IsNullOrEmpty(tableName)) return new { error = "tableName 不能为空" };
      if (fields == null || fields.Count == 0) return new { error = "fields 不能为空" };

      // 1. 组装 CREATE TABLE SQL（含 ISDELETED 逻辑删除字段）
      var colSql = new List<string>();
      var resfields = new List<object>();
      int entryNum = 0;
      string tbsResourceId = "tbs_" + tableName.ToLower().Replace("tbs_", "") + "_001";
      bool hasIsDeleted = false;  // LLM 可能已传 ISDELETED，避免重复追加
      foreach (JObject f in fields)
      {
        string name = f["name"]?.ToString();
        string type = f["type"]?.ToString()?.ToUpper();
        int length = f["length"]?.Type == JTokenType.Integer ? (int)f["length"] : 0;
        bool nullable = f["nullable"]?.Type == JTokenType.Boolean ? (bool)f["nullable"] : true;
        string comment = f["comment"]?.ToString();
        bool isKey = f["isKey"]?.Type == JTokenType.Boolean ? (bool)f["isKey"] : false;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type)) continue;

        // 字段名规则：大写无下划线
        if (name.Contains("_") || name != name.ToUpperInvariant())
          return new { error = "字段名 '" + name + "' 必须大写无下划线" };
        // 防注入：字段名/类型只允许大写字母数字下划线（白名单过滤）
        name = SafeIdentifier(name);
        type = SafeIdentifier(type);
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
          return new { error = "字段名/类型含非法字符（仅允许 A-Z 0-9 _）" };
        // ISDELETED 由工具统一规范追加，LLM 传的跳过避免重复列
        if (name == "ISDELETED") { hasIsDeleted = true; }

        string colDef = name + " " + type;
        if (type == "VARCHAR" && length > 0) colDef += "(" + length + ")";
        if (type == "DECIMAL" && length > 0) colDef += "(" + length + ",4)";
        if (isKey) colDef += " NOT NULL";
        else if (!nullable) colDef += " NOT NULL";
        if (!string.IsNullOrEmpty(comment)) colDef += " COMMENT '" + comment.Replace("'", "") + "'";
        colSql.Add(colDef);

        entryNum++;
        string fieldId = "rf_" + tableName.ToLower().Replace("tbs_", "") + "_" + name.ToLower();
        resfields.Add(new JObject
        {
          ["ID"] = fieldId,
          ["RESOURCEID"] = tbsResourceId,
          ["REFFIELDID"] = null,
          ["FIELDNAME"] = name,
          ["FIELDANAME"] = comment ?? name,
          ["FIELDTYPE"] = type,
          ["FIELDLENGTH"] = length,
          ["NULLABLE"] = nullable ? 1 : 0,
          ["ISKEY"] = isKey ? 1 : 0,
          ["KEYGENTYPE"] = isKey ? "GUID" : null,
          ["ENTRYNUM"] = entryNum,
          ["REFRESOURCEID"] = null,
          ["REFRESOURCEANAME"] = null,
          ["REFRELATION"] = null,
          ["UPFIELDID"] = null
        });
      }
      // 自动加审计六件套（全库标准, 与 ISDELETED 同机制）：
      // CREATEID/MODIFYID varchar(64), CREATER/MODIFER varchar(16), CREATETIME/MODIFYTIME datetime
      // LLM 已传的跳过(以 LLM 为准), 没传的按标准补齐, 保证每张业务表审计字段齐全
      var auditFields = new[]
      {
        new { Name = "CREATEID",   Type = "VARCHAR",  Len = 64, Comment = "创建人ID" },
        new { Name = "CREATER",    Type = "VARCHAR",  Len = 16, Comment = "创建人" },
        new { Name = "CREATETIME", Type = "DATETIME", Len = 0,  Comment = "创建时间" },
        new { Name = "MODIFYID",   Type = "VARCHAR",  Len = 64, Comment = "修改人ID" },
        new { Name = "MODIFER",    Type = "VARCHAR",  Len = 16, Comment = "修改人" },
        new { Name = "MODIFYTIME", Type = "DATETIME", Len = 0,  Comment = "修改时间" }
      };
      var existingNames = new HashSet<string>(fields.Select(f => (f["name"]?.ToString() ?? "").ToUpperInvariant()));
      foreach (var af in auditFields)
      {
        if (existingNames.Contains(af.Name)) continue;
        string colDef = af.Name + " " + af.Type + (af.Len > 0 ? "(" + af.Len + ")" : "") + " COMMENT '" + af.Comment + "'";
        colSql.Add(colDef);
        entryNum++;
        string afId = "rf_" + tableName.ToLower().Replace("tbs_", "") + "_" + af.Name.ToLower();
        resfields.Add(new JObject
        {
          ["ID"] = afId,
          ["RESOURCEID"] = tbsResourceId,
          ["REFFIELDID"] = null,
          ["FIELDNAME"] = af.Name,
          ["FIELDANAME"] = af.Comment,
          ["FIELDTYPE"] = af.Type,
          ["FIELDLENGTH"] = af.Len,
          ["NULLABLE"] = 1,
          ["ISKEY"] = 0,
          ["KEYGENTYPE"] = null,
          ["ENTRYNUM"] = entryNum,
          ["REFRESOURCEID"] = null,
          ["REFRESOURCEANAME"] = null,
          ["REFRELATION"] = null,
          ["UPFIELDID"] = null
        });
      }
      // 自动加 ISDELETED 逻辑删除字段（统一规范）：物理列 + resfield 元数据（避免 ORM 查询缺字段）
      // 仅当 LLM 未传 ISDELETED 时追加，避免 CREATE TABLE 重复列名报错
      if (!hasIsDeleted)
      {
        colSql.Add("ISDELETED TINYINT DEFAULT 0 COMMENT '逻辑删除'");
        entryNum++;
        string isDeletedFieldId = "rf_" + tableName.ToLower().Replace("tbs_", "") + "_isdeleted";
        resfields.Add(new JObject
        {
          ["ID"] = isDeletedFieldId,
          ["RESOURCEID"] = tbsResourceId,
          ["REFFIELDID"] = null,
          ["FIELDNAME"] = "ISDELETED",
          ["FIELDANAME"] = "逻辑删除",
          ["FIELDTYPE"] = "TINYINT",
          ["FIELDLENGTH"] = 1,
          ["NULLABLE"] = 0,
          ["ISKEY"] = 0,
          ["KEYGENTYPE"] = null,
          ["ENTRYNUM"] = entryNum,
          ["REFRESOURCEID"] = null,
          ["REFRESOURCEANAME"] = null,
          ["REFRELATION"] = null,
          ["UPFIELDID"] = null
        });
      }
      string sql = "CREATE TABLE " + tableName + " (\n  " + string.Join(",\n  ", colSql) + "\n);";

      // 2. TBS resource 元数据
      string resName = tableName.ToUpper();
      var resource = new JObject
      {
        ["ID"] = tbsResourceId,
        ["RESOURCENAME"] = resName,
        ["RESOURCEANAME"] = "A",
        ["TABLENAME"] = tableName,
        ["RESOURCETYPE"] = "TABLE",
        ["TABLERESOURCEID"] = null
      };

      // 3. 拼接 tss_resource + tss_resfield INSERT 到 sql（确保用户确认时同时执行 CREATE TABLE + 元数据注册，缺一不可）
      // 注：tss_resource / tss_resfield 是系统元数据表，无 ISDELETED 列，INSERT 不能带 ISDELETED
      string resourceInsert = "INSERT INTO tss_resource (ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID) VALUES ('" +
        tbsResourceId.Replace("'", "") + "', '" + resName.Replace("'", "") + "', 'A', '" + tableName.Replace("'", "") + "', 'TABLE', NULL);";
      var resfieldSb = new System.Text.StringBuilder();
      foreach (JObject rf in resfields)
      {
        string rfId = (string)rf["ID"];
        string fname = (string)rf["FIELDNAME"];
        string faname = (string)(rf["FIELDANAME"] ?? fname);
        string ftype = (string)rf["FIELDTYPE"];
        int flen = (int)rf["FIELDLENGTH"];
        int nul = (int)rf["NULLABLE"];
        int isk = (int)rf["ISKEY"];
        string keygen = rf["KEYGENTYPE"]?.ToString();
        int entry = (int)rf["ENTRYNUM"];
        resfieldSb.AppendLine("INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, UPFIELDID) VALUES ('" +
          rfId.Replace("'", "") + "', '" + tbsResourceId.Replace("'", "") + "', NULL, '" + fname.Replace("'", "") + "', '" + faname.Replace("'", "") + "', '" + ftype.Replace("'", "") + "', " + flen + ", " + nul + ", " + isk + ", " + (string.IsNullOrEmpty(keygen) ? "NULL" : "'" + keygen.Replace("'", "") + "'") + ", " + entry + ", NULL, NULL);");
      }
      sql = sql + "\n" + resourceInsert + "\n" + resfieldSb.ToString();

      var metadata = new JObject
      {
        ["resource"] = resource,
        ["resfields"] = JArray.FromObject(resfields)
      };
      return new { sql, metadata };
    }

    // ============================================================
    // 虚拟 DB 查询（跨工具时序裂缝修复）
    // 同会话前序工具产出的 resfield/module 还没写库（DRAFT 状态），
    // 但已存为 tss_aidev_changeitem.METADATA。校验时序时先用 DB 查，
    // DB 找不到再从 DRAFT 变更项的 metadata 里查（虚拟 DB 状态）。
    // ============================================================

    /// 查同 changeset 内 DRAFT 变更项里，指定资源 + 字段名的 resfield ID（虚拟 DB 状态）。
    /// 用于工具校验时序：前序工具产出的 resfield 还没写库，但从 DRAFT 变更项的 metadata 里能拿到。
    /// metadata 可能是 {resfield:{...}} 或 {resfields:[...]}（引用字段对/批量）
    private string LookupDraftResfieldId(DBHelper helper, string changesetId, string resourceId, string fieldName)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(fieldName)) return null;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='field'
            AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + fieldName + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          // metadata 形如 {resfield:{ID,RESOURCEID,FIELDNAME,...}}
          var rf = jo["resfield"];
          if (rf != null && (string)rf["RESOURCEID"] == resourceId && (string)rf["FIELDNAME"] == fieldName)
            return (string)rf["ID"];
          // metadata 形如 {resfields:[{...},{...}]}（引用字段对/批量场景）
          var rfs = jo["resfields"] as JArray;
          if (rfs != null)
          {
            foreach (var r in rfs)
            {
              if ((string)r["RESOURCEID"] == resourceId && (string)r["FIELDNAME"] == fieldName)
                return (string)r["ID"];
            }
          }
        }
        catch { }
      }
      return null;
    }

    /// 查同 changeset 内 DRAFT 变更项的 module ID（虚拟 DB 状态，用于 define_api 兜底）。
    /// metadata 形如 {module:{ID,MODULECODE,MODULENAME}}
    private string LookupDraftModuleId(DBHelper helper, string changesetId, string moduleCode)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(moduleCode)) return null;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='module' AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + moduleCode + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          var m = jo["module"];
          if (m != null && (string)m["MODULECODE"] == moduleCode) return (string)m["ID"];
        }
        catch { }
      }
      return null;
    }

    /// 查同 changeset 内 DRAFT 变更项的 TBS resource ID（虚拟 DB 状态，用于 define_dataview 兜底）。
    /// 跨步骤时序修复：向导 Step1 create_physical_table 产出 TBS 资源 DRAFT 项未写库，
    /// Step2 define_dataview 查 tss_resource 表查不到，从 DRAFT 变更项的 metadata.resource 里查。
    /// metadata 形如 {resource:{ID,RESOURCENAME,TABLENAME,RESOURCETYPE,...}, resfields:[...]}
    private string LookupDraftResourceId(DBHelper helper, string changesetId, string tableName)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(tableName)) return null;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='physical_table' AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + tableName + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          var r = jo["resource"];
          if (r != null && (string)r["TABLENAME"] == tableName && (string)r["RESOURCETYPE"] == "TABLE")
            return (string)r["ID"];
        }
        catch { }
      }
      return null;
    }

    // A2. add_field_to_table：产出 ALTER TABLE ADD COLUMN + resfield
    private object AddFieldToTable(JObject args)
    {
      string tableName = args["tableName"]?.ToString();
      var field = args["field"] as JObject;
      bool syncToView = args["syncToView"]?.Type == JTokenType.Boolean ? (bool)args["syncToView"] : true;
      if (string.IsNullOrEmpty(tableName)) return new { error = "tableName 不能为空" };
      if (field == null) return new { error = "field 不能为空" };

      string name = field["name"]?.ToString();
      string type = field["type"]?.ToString()?.ToUpper();
      int length = field["length"]?.Type == JTokenType.Integer ? (int)field["length"] : 0;
      bool nullable = field["nullable"]?.Type == JTokenType.Boolean ? (bool)field["nullable"] : true;
      string comment = field["comment"]?.ToString();
      bool isKey = field["isKey"]?.Type == JTokenType.Boolean ? (bool)field["isKey"] : false;
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
        return new { error = "field.name 和 field.type 不能为空" };
      if (name.Contains("_") || name != name.ToUpperInvariant())
        return new { error = "字段名 '" + name + "' 必须大写无下划线" };
      // 防注入：字段名/类型只允许大写字母数字下划线（白名单过滤）
      name = SafeIdentifier(name);
      type = SafeIdentifier(type);
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
        return new { error = "字段名/类型含非法字符（仅允许 A-Z 0-9 _）" };

      // 1. ALTER TABLE SQL
      string colDef = name + " " + type;
      if (type == "VARCHAR" && length > 0) colDef += "(" + length + ")";
      if (type == "DECIMAL" && length > 0) colDef += "(" + length + ",4)";
      if (isKey) colDef += " NOT NULL";
      else if (!nullable) colDef += " NOT NULL";
      if (!string.IsNullOrEmpty(comment)) colDef += " COMMENT '" + comment.Replace("'", "") + "'";
      string sql = "ALTER TABLE " + tableName + " ADD COLUMN " + colDef + ";";

      // 2. 查 TBS resource ID + ENTRYNUM
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var tbsRes = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_resource WHERE TABLENAME=@tn AND RESOURCETYPE='TABLE' LIMIT 1",
          new { tn = tableName });
        if (tbsRes == null) return new { error = "表 " + tableName + " 未注册 TBS 资源，请先 create_physical_table" };
        string resourceId = (string)tbsRes.ID;
        int entryNum = helper.QueryFirstOrDefault<int>(
          "SELECT COALESCE(MAX(ENTRYNUM),0)+1 FROM tss_resfield WHERE RESOURCEID=@rid",
          new { rid = resourceId });

        string fieldId = "rf_" + tableName.ToLower().Replace("tbs_", "") + "_" + name.ToLower() + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var resfield = new JObject
        {
          ["ID"] = fieldId,
          ["RESOURCEID"] = resourceId,
          ["REFFIELDID"] = null,
          ["FIELDNAME"] = name,
          ["FIELDANAME"] = comment ?? name,
          ["FIELDTYPE"] = type,
          ["FIELDLENGTH"] = length,
          ["NULLABLE"] = nullable ? 1 : 0,
          ["ISKEY"] = isKey ? 1 : 0,
          ["KEYGENTYPE"] = isKey ? "GUID" : null,
          ["ENTRYNUM"] = entryNum,
          ["REFRESOURCEID"] = null,
          ["UPFIELDID"] = null
        };
        // 拼接 resfield INSERT 到 sql（确保确认时同时执行 ALTER + 注册 TBS resfield，缺一不可）
        string safeComment = (comment ?? name).Replace("'", "");
        string resfieldInsert = "INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, UPFIELDID) VALUES (" +
          "'" + fieldId + "', '" + resourceId + "', NULL, '" + name + "', '" + safeComment + "', '" + type + "', " + length + ", " + (nullable ? 1 : 0) + ", " + (isKey ? 1 : 0) + ", " + (isKey ? "'GUID'" : "NULL") + ", " + entryNum + ", NULL, NULL);";
        sql = sql + "\n" + resfieldInsert;
        var metadata = new JObject { ["resfield"] = resfield };
        return new { sql, metadata };
      }
    }

    // A3. configure_resource_field：直接操作 tss_resfield（新增/修改字段定义）。
    /// 合并 add_field_to_table 后：通过 physicalColumnExists 区分是否需 ALTER TABLE 加物理列。
    /// changesetId 用于 DATAVIEW 字段校验 TBS 字段时序：DB 找不到时兜底查同会话 DRAFT 变更项。
    private object ConfigureResourceField(JObject args, string changesetId = null)
    {
      string action = args["action"]?.ToString();
      if (string.IsNullOrEmpty(action)) action = "add";
      string resourceId = args["resourceId"]?.ToString();
      string fieldName = args["fieldName"]?.ToString();
      string fieldAname = args["fieldAname"]?.ToString();
      string fieldType = args["fieldType"]?.ToString();
      int fieldLength = args["fieldLength"]?.Type == JTokenType.Integer ? (int)args["fieldLength"] : 0;
      bool hasNullable = args["nullable"]?.Type == JTokenType.Boolean;
      bool nullable = hasNullable ? (bool)args["nullable"] : true;
      bool isKey = args["isKey"]?.Type == JTokenType.Boolean ? (bool)args["isKey"] : false;
      string refFieldId = args["refFieldId"]?.ToString();
      // 物理列是否已存在：true=已存在只补 resfield（默认）；false=不存在需 ALTER TABLE 加列（仅 TABLE 资源有效）
      bool physicalColumnExists = args["physicalColumnExists"]?.Type == JTokenType.Boolean ? (bool)args["physicalColumnExists"] : true;

      if (string.IsNullOrEmpty(resourceId)) return new { error = "resourceId 不能为空" };
      if (string.IsNullOrEmpty(fieldName)) return new { error = "fieldName 不能为空" };
      if (fieldName.Contains("_") || fieldName != fieldName.ToUpperInvariant())
        return new { error = "字段名 '" + fieldName + "' 必须大写无下划线" };
      // 防注入：字段名/类型只允许大写字母数字下划线（白名单过滤）
      fieldName = SafeIdentifier(fieldName);
      if (!string.IsNullOrEmpty(fieldType)) fieldType = SafeIdentifier(fieldType);
      if (string.IsNullOrEmpty(fieldName))
        return new { error = "fieldName 含非法字符（仅允许 A-Z 0-9 _）" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 校验资源存在
        var res = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, RESOURCENAME, RESOURCETYPE FROM tss_resource WHERE ID=@rid LIMIT 1",
          new { rid = resourceId });
        if (res == null) return new { error = "资源 " + resourceId + " 不存在" };
        string resName = (string)res.RESOURCENAME;
        string resType = (string)res.RESOURCETYPE;

        if (action == "add")
        {
          // ALTER 前缀（仅 TABLE 资源 + physicalColumnExists=false 时填，最终拼到 sql 前面）
          string alterSqlPrefix = null;

          // 引用字段对模式：传了 refTableName 时，产出 ID + NAME 两个字段（参考 VBS_EMP.DEPTID/DEPTNAME）
          string refTableName = args["refTableName"]?.ToString();
          if (!string.IsNullOrEmpty(refTableName))
          {
            return ConfigureReferenceFieldPair(helper, resourceId, res, fieldName, fieldAname,
              refTableName, args["relation"]?.ToString(), args["nameFieldName"]?.ToString(), changesetId);
          }

          // 铁律：DATAVIEW 资源新增字段，REFFIELDID 必须链向对应 TBS 的同名字段
          // 强制顺序：TBS 必须先加该字段，再给 DATAVIEW 加（DB 已存在 或 同会话 DRAFT 项里有 都算）
          // 时序裂缝修复：TBS 字段可能由前序工具产出、还未写库（DRAFT），DB 查不到时兜底查 DRAFT metadata
          if (resType == "DATAVIEW")
          {
            // DATAVIEW 不能 ALTER 物理列（视图字段不对应真实物理列，物理列在 TBS 端处理）
            if (!physicalColumnExists)
              return new { error = "DATAVIEW 资源不能 ALTER 物理列，physicalColumnExists 必须为 true（DATAVIEW 字段只补 resfield 元数据，物理列由对应 TBS 资源负责）" };

            var tbsRes = helper.QueryFirstOrDefault<dynamic>(
              "SELECT TABLERESOURCEID FROM tss_resource WHERE ID=@rid",
              new { rid = resourceId });
            string tbsId = tbsRes != null ? (string)tbsRes.TABLERESOURCEID : null;
            if (string.IsNullOrEmpty(tbsId))
              return new { error = "DATAVIEW 资源 " + resName + " 未关联 TBS（TABLERESOURCEID 为空）" };
            // 先查 DB（TBS 字段已写库的场景）
            var tbsField = helper.QueryFirstOrDefault<dynamic>(
              "SELECT ID FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn LIMIT 1",
              new { rid = tbsId, fn = fieldName });
            string tbsFieldId = tbsField != null ? (string)tbsField.ID : null;
            // DB 找不到 → 兜底查同会话 DRAFT 变更项（前序工具产出未写库的场景）
            if (string.IsNullOrEmpty(tbsFieldId))
              tbsFieldId = LookupDraftResfieldId(helper, changesetId, tbsId, fieldName);
            if (string.IsNullOrEmpty(tbsFieldId))
              return new { error = "请先给 TBS 资源加字段 " + fieldName + "，再给 DATAVIEW 加（铁律：DATAVIEW 字段 REFFIELDID 必须链向已存在的 TBS 字段，强制先 TBS 后 DATAVIEW；DB 或同会话 DRAFT 项里有都算）" };
            // 自动取 TBS 字段 ID 作为 REFFIELDID（AI 无需手动传 refFieldId，传了也校验一致）
            if (!string.IsNullOrEmpty(refFieldId) && refFieldId != tbsFieldId)
              return new { error = "refFieldId 与 TBS 字段 ID 不一致，DATAVIEW 字段必须 REFFIELDID 指向对应 TBS 字段（正确值: " + tbsFieldId + "）" };
            refFieldId = tbsFieldId;
          }
          else if (resType == "TABLE")
          {
            // TABLE 资源字段直接对应物理列，不需要 REFFIELDID 关联（与物理表一致）
            // 强制 REFFIELDID=NULL，LLM 误传 refFieldId 时报错引导
            if (!string.IsNullOrEmpty(refFieldId))
              return new { error = "TABLE 资源字段不需要 refFieldId（TBS 字段直接对应物理列，无需关联其他字段，REFFIELDID 必须为 NULL）" };
            refFieldId = null;

            // 物理表（业务表）是基础，资源依赖物理表：必须先确保物理列存在
            // 自动检测物理列（information_schema），不存在自动产出 ALTER，已存在只补 resfield
            // 不再依赖 LLM 传 physicalColumnExists（LLM 容易传错/忘传）
            var physRow = helper.QueryFirstOrDefault<dynamic>(
              "SELECT TABLENAME FROM tss_resource WHERE ID=@rid LIMIT 1",
              new { rid = resourceId });
            if (physRow == null || string.IsNullOrEmpty((string)physRow.TABLENAME))
              return new { error = "TABLE 资源 " + resName + " 未关联物理表（TABLENAME 为空），无法检测物理列" };
            string physTableName = (string)physRow.TABLENAME;
            int colExists = helper.QueryFirstOrDefault<int>(
              @"SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tbl AND COLUMN_NAME=@col",
              new { tbl = physTableName, col = fieldName });
            if (colExists == 0)
            {
              // 物理列不存在 → 自动产出 ALTER TABLE ADD COLUMN（资源描述的列物理表必须先有）
              string alterType = string.IsNullOrEmpty(fieldType) ? "VARCHAR" : fieldType;
              string colDef = fieldName + " " + alterType;
              if (alterType == "VARCHAR" && fieldLength > 0) colDef += "(" + fieldLength + ")";
              if (alterType == "DECIMAL" && fieldLength > 0) colDef += "(" + fieldLength + ",4)";
              if (isKey) colDef += " NOT NULL";
              else if (!nullable) colDef += " NOT NULL";
              if (!string.IsNullOrEmpty(fieldAname)) colDef += " COMMENT '" + fieldAname.Replace("'", "") + "'";
              alterSqlPrefix = "ALTER TABLE " + physTableName + " ADD COLUMN " + colDef + ";\n";
            }
            // 物理列已存在 → 只补 resfield，不 ALTER
          }

          // 校验字段不重复
          int dupCnt = helper.QueryFirstOrDefault<int>(
            "SELECT COUNT(1) FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn",
            new { rid = resourceId, fn = fieldName });
          if (dupCnt > 0) return new { error = "字段 " + fieldName + " 在资源 " + resName + " 中已存在" };

          // ENTRYNUM = MAX+1
          int entryNum = helper.QueryFirstOrDefault<int>(
            "SELECT COALESCE(MAX(ENTRYNUM),0)+1 FROM tss_resfield WHERE RESOURCEID=@rid",
            new { rid = resourceId });

          // fieldId 命名：rf_ + resName小写去前缀(vck_/vss_/tbs_) + _ + fieldName小写 + _ + 8位GUID
          string resNameLower = resName.ToLowerInvariant();
          if (resNameLower.StartsWith("vck_") || resNameLower.StartsWith("vss_") || resNameLower.StartsWith("tbs_"))
            resNameLower = resNameLower.Substring(4);
          string fieldId = "rf_" + resNameLower + "_" + fieldName.ToLowerInvariant() + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);

          // 拼字段中文名（防注入）
          string safeAname = (fieldAname ?? fieldName).Replace("'", "");
          string safeType = (fieldType ?? "VARCHAR").Replace("'", "");
          string safeRefFieldId = string.IsNullOrEmpty(refFieldId) ? "NULL" : "'" + refFieldId.Replace("'", "") + "'";
          string keyGenType = isKey ? "'GUID'" : "NULL";

          // 产出 INSERT INTO tss_resfield
          string sql = "INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, UPFIELDID) VALUES (" +
            "'" + fieldId + "', '" + resourceId + "', " + safeRefFieldId + ", '" + fieldName + "', '" + safeAname + "', '" + safeType + "', " + fieldLength + ", " + (nullable ? 1 : 0) + ", " + (isKey ? 1 : 0) + ", " + keyGenType + ", " + entryNum + ", NULL, NULL);";
          // 拼上 ALTER TABLE（仅 TABLE 资源 + physicalColumnExists=false 时有值）
          if (!string.IsNullOrEmpty(alterSqlPrefix))
            sql = alterSqlPrefix + sql;

          var resfield = new JObject
          {
            ["ID"] = fieldId,
            ["RESOURCEID"] = resourceId,
            ["REFFIELDID"] = string.IsNullOrEmpty(refFieldId) ? null : refFieldId,
            ["FIELDNAME"] = fieldName,
            ["FIELDANAME"] = fieldAname ?? fieldName,
            ["FIELDTYPE"] = fieldType ?? "VARCHAR",
            ["FIELDLENGTH"] = fieldLength,
            ["NULLABLE"] = nullable ? 1 : 0,
            ["ISKEY"] = isKey ? 1 : 0,
            ["KEYGENTYPE"] = isKey ? "GUID" : null,
            ["ENTRYNUM"] = entryNum,
            ["REFRESOURCEID"] = null,
            ["UPFIELDID"] = null
          };
          var metadata = new JObject
          {
            ["resfield"] = resfield,
            ["action"] = "add",
            ["resourceName"] = resName
          };
          return new { sql, metadata };
        }
        else if (action == "update")
        {
          // 查现有字段 ID
          var existing = helper.QueryFirstOrDefault<dynamic>(
            "SELECT ID FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn LIMIT 1",
            new { rid = resourceId, fn = fieldName });
          if (existing == null) return new { error = "字段 " + fieldName + " 在资源 " + resName + " 中不存在，无法 update" };
          string fieldId = (string)existing.ID;

          var setClauses = new List<string>();
          if (!string.IsNullOrEmpty(fieldAname))
            setClauses.Add("FIELDANAME='" + fieldAname.Replace("'", "") + "'");
          if (!string.IsNullOrEmpty(fieldType))
            setClauses.Add("FIELDTYPE='" + fieldType.Replace("'", "") + "'");
          if (args["fieldLength"]?.Type == JTokenType.Integer)
            setClauses.Add("FIELDLENGTH=" + fieldLength);
          if (hasNullable)
            setClauses.Add("NULLABLE=" + (nullable ? 1 : 0));
          if (!string.IsNullOrEmpty(refFieldId))
            setClauses.Add("REFFIELDID='" + refFieldId.Replace("'", "") + "'");

          if (setClauses.Count == 0)
            return new { error = "未提供任何可更新属性（fieldAname/fieldType/fieldLength/nullable/refFieldId）" };

          string sql = "UPDATE tss_resfield SET " + string.Join(", ", setClauses) + " WHERE ID='" + fieldId + "';";

          var metadata = new JObject
          {
            ["fieldId"] = fieldId,
            ["action"] = "update",
            ["resourceName"] = resName,
            ["sets"] = new JArray(setClauses)
          };
          return new { sql, metadata };
        }
        else
        {
          return new { error = "action 必须是 add 或 update" };
        }
      }
    }

    // 引用字段对：产出 DEPTID（主引用）+ DEPTNAME（子引用）两条 resfield
    // 参考 VBS_EMP.DEPTID/DEPTNAME 的真实配置
    private object ConfigureReferenceFieldPair(DBHelper helper, string resourceId, dynamic res,
      string idFieldName, string idFieldAname, string refTableName, string relation, string nameFieldName, string changesetId = null)
    {
      string resName = (string)res.RESOURCENAME;
      string resType = (string)res.RESOURCETYPE;

      // 铁律：TABLE 资源是单表，字段直接对应物理列，不需要引用关联
      // 引用字段对（refTableName 模式）只能用于 DATAVIEW(VSS/VCK) 视图（通过 JOIN 取关联表字段）
      if (resType == "TABLE")
        return new { error = "TABLE 资源(" + resName + ")是单表，字段直接对应物理列，不需要引用关联。给 TABLE 加字段请去掉 refTableName 参数，用普通字段模式（如 MANAGERID VARCHAR(64)）。引用字段对(refTableName)仅用于 DATAVIEW(VSS/VCK) 视图——视图通过 JOIN 才需要关联到员工表/部门表等。" };

      if (string.IsNullOrEmpty(idFieldName)) return new { error = "fieldName 不能为空（主引用字段，如 DEPTID）" };
      if (idFieldName.Contains("_") || idFieldName != idFieldName.ToUpperInvariant())
        return new { error = "字段名 '" + idFieldName + "' 必须大写无下划线" };
      if (string.IsNullOrEmpty(relation)) return new { error = "引用字段必须传 relation（如 A.DEPTID=B.ID）" };
      if (string.IsNullOrEmpty(nameFieldName)) return new { error = "引用字段必须传 nameFieldName（如 DEPTNAME）" };

      // 铁律1：查被引用 TBS 资源（必须是 TABLE 类型）
      var refRes = helper.QueryFirstOrDefault<dynamic>(
        "SELECT ID, RESOURCEANAME FROM tss_resource WHERE TABLENAME=@tn AND RESOURCETYPE='TABLE' LIMIT 1",
        new { tn = refTableName });
      if (refRes == null) return new { error = "被引用表 " + refTableName + " 未注册 TBS 资源（REFRESOURCEID 必须指向 TABLE 类型）" };
      string refResourceId = (string)refRes.ID;
      string refResourceAname = (string)refRes.RESOURCEANAME ?? "B";

      // 铁律2：查被引用 TBS 的名称字段 ID（如 TBS_DEPT.DEPTNAME）
      var refNameField = helper.QueryFirstOrDefault<dynamic>(
        "SELECT ID FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn LIMIT 1",
        new { rid = refResourceId, fn = nameFieldName });
      if (refNameField == null) return new { error = "被引用表 " + refTableName + " 无字段 " + nameFieldName + "（子字段 REFFIELDID 必须指向被引用 TBS 的名称字段）" };
      string refNameFieldId = (string)refNameField.ID;

      // 校验主字段、子字段不重复
      int dupMain = helper.QueryFirstOrDefault<int>(
        "SELECT COUNT(1) FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn",
        new { rid = resourceId, fn = idFieldName });
      if (dupMain > 0) return new { error = "字段 " + idFieldName + " 在资源 " + resName + " 中已存在" };
      int dupSub = helper.QueryFirstOrDefault<int>(
        "SELECT COUNT(1) FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn",
        new { rid = resourceId, fn = nameFieldName });
      if (dupSub > 0) return new { error = "字段 " + nameFieldName + " 在资源 " + resName + " 中已存在" };

      // ENTRYNUM
      int entryNumMain = helper.QueryFirstOrDefault<int>(
        "SELECT COALESCE(MAX(ENTRYNUM),0)+1 FROM tss_resfield WHERE RESOURCEID=@rid",
        new { rid = resourceId });
      int entryNumSub = entryNumMain + 1;

      // fieldId 命名
      string resNameLower = resName.ToLowerInvariant();
      if (resNameLower.StartsWith("vck_") || resNameLower.StartsWith("vss_") || resNameLower.StartsWith("tbs_"))
        resNameLower = resNameLower.Substring(4);
      string mainFieldId = "rf_" + resNameLower + "_" + idFieldName.ToLowerInvariant() + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
      string subFieldId = "rf_" + resNameLower + "_" + nameFieldName.ToLowerInvariant() + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);

      // DATAVIEW 资源：主字段 REFFIELDID 必须链向本地 TBS 的 idFieldName 字段（铁律：DATAVIEW 字段必须 REFFIELDID 链向 TBS）
      // 强制校验：TBS 必须先加该字段，否则报错（强制 AI 先 TBS 后 DATAVIEW）
      // TBS 资源：主字段 REFFIELDID = NULL
      string mainRefFieldIdSql = "NULL";
      string mainRefFieldIdVal = null;
      if (resType == "DATAVIEW")
      {
        var resFull = helper.QueryFirstOrDefault<dynamic>(
          "SELECT TABLERESOURCEID FROM tss_resource WHERE ID=@rid",
          new { rid = resourceId });
        string tableResourceId = resFull != null ? (string)resFull.TABLERESOURCEID : null;
        if (string.IsNullOrEmpty(tableResourceId))
          return new { error = "DATAVIEW 资源 " + resName + " 未关联 TBS（TABLERESOURCEID 为空），无法加引用字段" };
        var localField = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn LIMIT 1",
          new { rid = tableResourceId, fn = idFieldName });
        string localFieldId = localField != null ? (string)localField.ID : null;
        // 同会话 DRAFT 项豁免：DB 没有但同会话前序 DRAFT 项里有该 TBS 字段也算（合并执行时同事务会先 INSERT TBS resfield）
        if (localFieldId == null)
        {
          localFieldId = LookupDraftResfieldId(helper, changesetId, tableResourceId, idFieldName);
        }
        if (localFieldId == null)
          return new { error = "TBS 资源(" + resName + " 对应的 TABLE)还没有字段 " + idFieldName + "。引用字段对的 ID 字段(如 MANAGERID)在 TBS 上是普通物理列(存员工ID，VARCHAR 36)，需先用 configure_resource_field(resourceId=TABLE资源ID, fieldName=" + idFieldName + ", fieldType=VARCHAR, fieldLength=36) 给 TBS 加该字段（会自动 ALTER 物理表 + 注册 TBS resfield），然后再到 DATAVIEW 上加引用字段对。参考 VBS_DEPT.UPDEPTID：TBS_DEPT 有 UPDEPTID 普通列，VBS_DEPT 才配成引用字段。" };
        mainRefFieldIdSql = "'" + localFieldId + "'";
        mainRefFieldIdVal = localFieldId;
      }

      string safeIdAname = (idFieldAname ?? idFieldName).Replace("'", "");
      string safeSubAname = ((idFieldAname ?? idFieldName) + "名称").Replace("'", "");
      string safeRelation = relation.Replace("'", "");

      // 主字段 INSERT（DEPTID）—— 含 REFRESOURCEID/REFRESOURCEANAME/REFRELATION
      string mainSql = "INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, REFRESOURCEANAME, REFRELATION, UPFIELDID) VALUES (" +
        "'" + mainFieldId + "', '" + resourceId + "', " + mainRefFieldIdSql + ", '" + idFieldName + "', '" + safeIdAname + "', 'VARCHAR', 36, 1, 0, NULL, " + entryNumMain + ", '" + refResourceId + "', '" + refResourceAname + "', '" + safeRelation + "', NULL);";

      // 子字段 INSERT（DEPTNAME）—— UPFIELDID 指向主字段，REFFIELDID 指向被引用 TBS 名称字段
      string subSql = "INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, REFRESOURCEANAME, REFRELATION, UPFIELDID) VALUES (" +
        "'" + subFieldId + "', '" + resourceId + "', '" + refNameFieldId + "', '" + nameFieldName + "', '" + safeSubAname + "', 'VARCHAR', 200, 1, 0, NULL, " + entryNumSub + ", NULL, NULL, NULL, '" + mainFieldId + "');";

      string sql = mainSql + "\n" + subSql;

      var mainResfield = new JObject
      {
        ["ID"] = mainFieldId,
        ["FIELDNAME"] = idFieldName,
        ["FIELDANAME"] = idFieldAname ?? idFieldName,
        ["FIELDTYPE"] = "VARCHAR",
        ["FIELDLENGTH"] = 36,
        ["REFRESOURCEID"] = refResourceId,
        ["REFRESOURCEANAME"] = refResourceAname,
        ["REFRELATION"] = relation,
        ["REFFIELDID"] = mainRefFieldIdVal,
        ["UPFIELDID"] = null
      };
      var subResfield = new JObject
      {
        ["ID"] = subFieldId,
        ["FIELDNAME"] = nameFieldName,
        ["FIELDANAME"] = (idFieldAname ?? idFieldName) + "名称",
        ["FIELDTYPE"] = "VARCHAR",
        ["FIELDLENGTH"] = 200,
        ["REFRESOURCEID"] = null,
        ["REFRESOURCEANAME"] = null,
        ["REFRELATION"] = null,
        ["REFFIELDID"] = refNameFieldId,
        ["UPFIELDID"] = mainFieldId
      };
      var metadata = new JObject
      {
        ["resfields"] = new JArray { mainResfield, subResfield },
        ["action"] = "add",
        ["isReference"] = true,
        ["resourceName"] = resName,
        ["refTableName"] = refTableName
      };
      return new { sql, metadata };
    }

    // B1. define_dataview：产出 VCK resource + resfield（REFFIELDID 链向 TBS）。
    /// changesetId 用于时序裂缝修复：TBS 字段 DB 找不到时，兜底查同会话 DRAFT 变更项。
    private object DefineDataview(JObject args, string changesetId = null)
    {
      string vckName = args["vckName"]?.ToString();
      string tableName = args["tableName"]?.ToString();
      var fields = args["fields"] as JArray;
      if (string.IsNullOrEmpty(vckName)) return new { error = "vckName 不能为空" };
      if (string.IsNullOrEmpty(tableName)) return new { error = "tableName 不能为空" };
      if (fields == null || fields.Count == 0) return new { error = "fields 不能为空" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 查 TBS resource ID
        var tbsRes = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_resource WHERE TABLENAME=@tn AND RESOURCETYPE='TABLE' LIMIT 1",
          new { tn = tableName });
        string tbsResourceId = tbsRes != null ? (string)tbsRes.ID : null;
        // DB 找不到 → 兜底查同会话 DRAFT 变更项（向导分步场景：前序 create_physical_table 未写库）
        if (string.IsNullOrEmpty(tbsResourceId))
          tbsResourceId = LookupDraftResourceId(helper, changesetId, tableName);
        if (string.IsNullOrEmpty(tbsResourceId))
          return new { error = "表 " + tableName + " 未注册 TBS 资源，请先 create_physical_table（DB 或同会话 DRAFT 项里都没有）" };

        // 查 TBS 各字段的 ID（建 REFFIELDID 关联）。DB 已写库的字段先入 map。
        var tbsFieldMap = new Dictionary<string, string>();
        var tbsFields = helper.Query<dynamic>(
          "SELECT ID, FIELDNAME FROM tss_resfield WHERE RESOURCEID=@rid",
          new { rid = tbsResourceId });
        foreach (var tf in tbsFields) tbsFieldMap[(string)tf.FIELDNAME] = (string)tf.ID;

        string vckResourceId = "vck_" + tableName.ToLower().Replace("tbs_", "") + "_001";
        var resfields = new List<object>();
        int entryNum = 0;
        foreach (JObject f in fields)
        {
          string name = f["name"]?.ToString();
          string refFieldName = f["refFieldName"]?.ToString();
          if (string.IsNullOrEmpty(name)) continue;
          if (name.Contains("_") || name != name.ToUpperInvariant())
            return new { error = "字段名 '" + name + "' 必须大写无下划线" };

          string refFieldId = null;
          if (!string.IsNullOrEmpty(refFieldName))
          {
            // 先查 DB（map 里已有）
            if (tbsFieldMap.ContainsKey(refFieldName))
              refFieldId = tbsFieldMap[refFieldName];
            else
            {
              // DB 找不到 → 兜底查同会话 DRAFT 变更项（前序工具产出未写库的场景）
              string draftId = LookupDraftResfieldId(helper, changesetId, tbsResourceId, refFieldName);
              if (!string.IsNullOrEmpty(draftId))
              {
                refFieldId = draftId;
                tbsFieldMap[refFieldName] = draftId;  // 入 map，后续字段复用
              }
              else
                return new { error = "TBS 字段 " + refFieldName + " 不存在，无法建 REFFIELDID 关联（DB 或同会话 DRAFT 项里都没有；若该字段尚未产出，请先给 TBS 资源加该字段）" };
            }
          }

          entryNum++;
          string fieldId = "rf_vck_" + tableName.ToLower().Replace("tbs_", "") + "_" + name.ToLower() + "_" + entryNum;
          bool isKeyId = name == "ID";
          resfields.Add(new JObject
          {
            ["ID"] = fieldId,
            ["RESOURCEID"] = vckResourceId,
            ["REFFIELDID"] = refFieldId,
            ["FIELDNAME"] = name,
            ["FIELDANAME"] = name,
            ["FIELDTYPE"] = "VARCHAR",
            ["FIELDLENGTH"] = 200,
            ["NULLABLE"] = 1,
            ["ISKEY"] = isKeyId ? 1 : 0,
            ["KEYGENTYPE"] = isKeyId ? "GUID" : null,
            ["ENTRYNUM"] = entryNum,
            ["REFRESOURCEID"] = null,
            ["UPFIELDID"] = null
          });
        }

        string vckNameUpper = vckName.ToUpper();
        var resource = new JObject
        {
          ["ID"] = vckResourceId,
          ["RESOURCENAME"] = vckNameUpper,
          ["RESOURCEANAME"] = "A",
          ["TABLENAME"] = tableName,
          ["RESOURCETYPE"] = "DATAVIEW",
          ["TABLERESOURCEID"] = tbsResourceId
        };

        // 拼接 tss_resource + tss_resfield INSERT 到 sql（确保用户确认时同时执行 VCK 资源注册 + 字段注册，缺一不可）
        // 注：tss_resource / tss_resfield 是系统元数据表，无 ISDELETED 列，INSERT 不能带 ISDELETED
        string resourceInsert = "INSERT INTO tss_resource (ID, RESOURCENAME, RESOURCEANAME, TABLENAME, RESOURCETYPE, TABLERESOURCEID) VALUES ('" +
          vckResourceId.Replace("'", "") + "', '" + vckNameUpper.Replace("'", "") + "', 'A', '" + tableName.Replace("'", "") + "', 'DATAVIEW', '" + tbsResourceId.Replace("'", "") + "');";
        var resfieldSb = new System.Text.StringBuilder();
        foreach (JObject rf in resfields)
        {
          string rfId = (string)rf["ID"];
          string fname = (string)rf["FIELDNAME"];
          string faname = (string)(rf["FIELDANAME"] ?? fname);
          string ftype = (string)rf["FIELDTYPE"];
          int flen = (int)rf["FIELDLENGTH"];
          int nul = (int)rf["NULLABLE"];
          int isk = (int)rf["ISKEY"];
          string keygen = rf["KEYGENTYPE"]?.ToString();
          int entry = (int)rf["ENTRYNUM"];
          string refFieldIdVal = (string)rf["REFFIELDID"];
          string refFieldIdSql = string.IsNullOrEmpty(refFieldIdVal) ? "NULL" : "'" + refFieldIdVal.Replace("'", "") + "'";
          resfieldSb.AppendLine("INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, FIELDANAME, FIELDTYPE, FIELDLENGTH, NULLABLE, ISKEY, KEYGENTYPE, ENTRYNUM, REFRESOURCEID, UPFIELDID) VALUES ('" +
            rfId.Replace("'", "") + "', '" + vckResourceId.Replace("'", "") + "', " + refFieldIdSql + ", '" + fname.Replace("'", "") + "', '" + faname.Replace("'", "") + "', '" + ftype.Replace("'", "") + "', " + flen + ", " + nul + ", " + isk + ", " + (string.IsNullOrEmpty(keygen) ? "NULL" : "'" + keygen.Replace("'", "") + "'") + ", " + entry + ", NULL, NULL);");
        }
        string sql = resourceInsert + "\n" + resfieldSb.ToString();

        var metadata = new JObject
        {
          ["resource"] = resource,
          ["resfields"] = JArray.FromObject(resfields)
        };
        return new { sql, metadata };
      }
    }

    // B2. define_reference_field：产出引用字段配置（REFRESOURCEID/REFRELATION/UPFIELDID）
    // 自动套用两条铁律：REFRESOURCEID 指向 TBS、REFFIELDID 指向被引用 TBS 字段
    private object DefineReferenceField(JObject args)
    {
      string fieldName = args["fieldName"]?.ToString();
      string refTableName = args["refTableName"]?.ToString();
      string relation = args["relation"]?.ToString();
      string nameField = args["nameField"]?.ToString();
      if (string.IsNullOrEmpty(fieldName)) return new { error = "fieldName 不能为空" };
      if (string.IsNullOrEmpty(refTableName)) return new { error = "refTableName 不能为空" };
      if (string.IsNullOrEmpty(relation)) return new { error = "relation 不能为空" };
      if (string.IsNullOrEmpty(nameField)) return new { error = "nameField 不能为空" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 铁律1：REFRESOURCEID 必须指向 TBS(TABLE)，不能指向 VBS(DATAVIEW)
        var refRes = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, RESOURCENAME, RESOURCEANAME FROM tss_resource WHERE TABLENAME=@tn AND RESOURCETYPE='TABLE' LIMIT 1",
          new { tn = refTableName });
        if (refRes == null) return new { error = "引用表 " + refTableName + " 未注册 TBS 资源，REFRESOURCEID 必须指向 TABLE 类型" };
        string refResourceId = (string)refRes.ID;
        string refResourceAname = (string)refRes.RESOURCEANAME ?? "B";

        // 铁律2：REFFIELDID 必须指向被引用 TBS 表的字段（如 TBS_EMP.EMPNAME）
        var refNameField = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_resfield WHERE RESOURCEID=@rid AND FIELDNAME=@fn LIMIT 1",
          new { rid = refResourceId, fn = nameField });
        if (refNameField == null) return new { error = "被引用表 " + refTableName + " 无字段 " + nameField + "，REFFIELDID 必须指向被引用 TBS 字段" };
        string refFieldId = (string)refNameField.ID;

        // 主引用字段（ID 字段，如 EMPID）
        var mainField = new JObject
        {
          ["FIELDNAME"] = fieldName,
          ["REFRESOURCEID"] = refResourceId,
          ["REFRESOURCEANAME"] = refResourceAname,
          ["REFRELATION"] = relation,
          ["REFFIELDID"] = refFieldId,
          ["UPFIELDID"] = null,
          ["REFRESOURCE_TYPE"] = "TABLE"
        };
        // 子引用字段（名称字段，如 EMPNAME，UPFIELDID 指向主引用字段）
        var subField = new JObject
        {
          ["FIELDNAME"] = nameField,
          ["REFRESOURCEID"] = refResourceId,
          ["REFRESOURCEANAME"] = refResourceAname,
          ["REFRELATION"] = relation,
          ["REFFIELDID"] = refFieldId,
          ["UPFIELDID"] = "__MAIN_FIELD__",  // 占位，Orchestrator 填实际字段ID
          ["REFRESOURCE_TYPE"] = "TABLE"
        };
        var metadata = new JObject
        {
          ["resfields"] = new JArray { mainField, subField }
        };
        return new { sql = "", metadata };
      }
    }

    // C1. configure_list_column：产出 resuipc UPDATE（LISTSORT/LABELNAME/SHOWLENGTH）
    // C1. configure_ui_field：合并 list_column + form_field，一个字段一条 resuipc（列表+表单一次配全）
    private object ConfigureUiField(JObject args, string changesetId = null)
    {
      string fieldId = args["fieldId"]?.ToString();
      string labelName = args["labelName"]?.ToString();
      int listSort = args["listSort"]?.Type == JTokenType.Integer ? (int)args["listSort"] : 0;
      int showLength = args["showLength"]?.Type == JTokenType.Integer ? (int)args["showLength"] : 100;
      string editType = args["editType"]?.ToString();
      string selectData = args["selectData"]?.ToString();
      int editSort = args["editSort"]?.Type == JTokenType.Integer ? (int)args["editSort"] : 0;
      string updateFields = args["updateFields"]?.ToString();
      if (string.IsNullOrEmpty(fieldId)) return new { error = "fieldId 不能为空" };
      if (string.IsNullOrEmpty(labelName) && string.IsNullOrEmpty(editType))
        return new { error = "至少传 labelName(配列表) 或 editType(配表单) 之一" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var resuipc = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_resuipc WHERE RESFIELDID=@fid LIMIT 1",
          new { fid = fieldId });
        string sql;
        string resourceIdStr = null;  // DRAFT 兜底填充, 供 metadata 回写
        string safeFieldId = fieldId.Replace("'", "");
        string safeLabel = (labelName ?? "").Replace("'", "");
        string safeEditType = SafeIdentifier(editType ?? "");
        string safeSelectData = selectData == null ? "" : selectData.Replace("'", "");
        string safeUpdateFields = updateFields == null ? "" : updateFields.Replace("'", "");

        if (resuipc == null)
        {
          // 不存在则 INSERT（列表+表单字段一次入库）
          string uipcId = "uipc_" + safeFieldId.ToLower().Replace("-", "").Substring(0, Math.Min(safeFieldId.Length, 12)) + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
          // DRAFT 兜底: 前序步骤的 create_physical_table / define_dataview 可能未执行,
          // tss_resfield 表查不到该 fieldId → INSERT 时 SELECT ... FROM tss_resfield 返回空,
          // 导致 resuipc.RESOURCEID=NULL(行废掉)。从 DRAFT 变更项的 metadata.resfield 里取 RESOURCEID 兜底。
          var dbResfield = helper.QueryFirstOrDefault<dynamic>(
            "SELECT RESOURCEID FROM tss_resfield WHERE ID=@fid",
            new { fid = fieldId });
          resourceIdStr = dbResfield?.RESOURCEID as string;
          if (string.IsNullOrEmpty(resourceIdStr) && !string.IsNullOrEmpty(changesetId))
          {
            resourceIdStr = LookupDraftResfieldResourceId(helper, changesetId, fieldId);
          }
          if (string.IsNullOrEmpty(resourceIdStr))
          {
            return new { error = "fieldId 在 tss_resfield 表和同 changeset 的 DRAFT 变更项中都查不到, 请确认 fieldId 正确, 或先执行前序步骤(第 2 步)把 resfield 落库后再来配 UI" };
          }
          string safeResourceId = resourceIdStr.Replace("'", "");
          sql = "INSERT INTO tss_resuipc (ID, RESOURCEID, RESFIELDID, LABELNAME, LISTSORT, SHOWLENGTH, EDITTYPE, SELECTDATA, EDITSORT, UPDATEFIELDS) " +
                "VALUES ('" + uipcId + "', '" + safeResourceId + "', '" + safeFieldId + "', " +
                (string.IsNullOrEmpty(safeLabel) ? "NULL" : "'" + safeLabel + "'") + ", " + listSort + ", " + showLength + ", " +
                (string.IsNullOrEmpty(safeEditType) ? "NULL" : "'" + safeEditType + "'") + ", " +
                (string.IsNullOrEmpty(safeSelectData) ? "NULL" : "'" + safeSelectData + "'") + ", " + editSort + ", " +
                (string.IsNullOrEmpty(safeUpdateFields) ? "NULL" : "'" + safeUpdateFields + "'") + ");";
        }
        else
        {
          // 已存在则 UPDATE（只更新非空字段，避免覆盖已配好的列表/表单配置）
          var sets = new List<string>();
          if (!string.IsNullOrEmpty(safeLabel)) sets.Add("LABELNAME='" + safeLabel + "'");
          if (args["listSort"]?.Type == JTokenType.Integer) sets.Add("LISTSORT=" + listSort);
          if (args["showLength"]?.Type == JTokenType.Integer) sets.Add("SHOWLENGTH=" + showLength);
          if (!string.IsNullOrEmpty(safeEditType)) sets.Add("EDITTYPE='" + safeEditType + "'");
          if (!string.IsNullOrEmpty(safeSelectData)) sets.Add("SELECTDATA='" + safeSelectData + "'");
          if (args["editSort"]?.Type == JTokenType.Integer) sets.Add("EDITSORT=" + editSort);
          if (!string.IsNullOrEmpty(safeUpdateFields)) sets.Add("UPDATEFIELDS='" + safeUpdateFields + "'");
          if (sets.Count == 0) return new { error = "未提供任何要更新的字段" };
          string uipcId = (string)resuipc.ID;
          sql = "UPDATE tss_resuipc SET " + string.Join(", ", sets) + " WHERE ID='" + uipcId.Replace("'", "") + "';";
        }
        var metadata = new JObject
        {
          ["resuipc"] = new JObject
          {
            ["RESFIELDID"] = fieldId,
            ["RESOURCEID"] = resourceIdStr ?? "",
            ["LABELNAME"] = labelName,
            ["LISTSORT"] = listSort,
            ["SHOWLENGTH"] = showLength,
            ["EDITTYPE"] = editType,
            ["SELECTDATA"] = selectData,
            ["EDITSORT"] = editSort,
            ["UPDATEFIELDS"] = updateFields
          }
        };
        return new { sql, metadata };
      }
    }

    /// <summary>
    /// 查同 changeset 内 DRAFT 变更项的 resfield.RESOURCEID（虚拟 DB 状态兜底）。
    /// 第 4 步 configure_ui_field 在第 2 步 create_physical_table 未执行时,
    /// tss_resfield 表里查不到 fieldId → 从 DRAFT 变更项的 metadata.resfield.RESOURCEID 兜底。
    /// </summary>
    private string LookupDraftResfieldResourceId(DBHelper helper, string changesetId, string fieldId)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(fieldId)) return null;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='field'
            AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + fieldId + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          var rf = jo["resfield"];
          if (rf != null && (string)rf["ID"] == fieldId)
          {
            return (string)rf["RESOURCEID"];
          }
        }
        catch { }
      }
      return null;
    }

    /// <summary>
    /// 查同 changeset 内 DRAFT 变更项的 module_page.ID（虚拟 DB 状态兜底）。
    /// 第 3 步 define_button 在同步骤 define_page 未执行时, tss_module_page 表查不到 pageId → 从 DRAFT 兜底。
    /// </summary>
    private string LookupDraftPageId(DBHelper helper, string changesetId, string pageId)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(pageId)) return null;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='page'
            AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + pageId + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          var pg = jo["module_page"];
          if (pg != null && (string)pg["ID"] == pageId)
          {
            return (string)pg["ID"];
          }
        }
        catch { }
      }
      return null;
    }

    // 已废弃：configure_list_column / configure_form_field 合并到 configure_ui_field
    private object ConfigureListColumn(JObject args)
    {
      return new { error = "configure_list_column 已合并到 configure_ui_field" };
    }

    // 已废弃：configure_form_field 合并到 configure_ui_field
    private object ConfigureFormField(JObject args)
    {
      return new { error = "configure_form_field 已合并到 configure_ui_field" };
    }

    // D. search_dict：搜索已有字典（只读）。字典在字典管理模块 RS_M06 维护，AI 不自建。
    private object SearchDict(string keyword)
    {
      if (string.IsNullOrEmpty(keyword)) return new { error = "keyword 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var kw = "%" + keyword + "%";
        // 模糊匹配字典名，返回字典 + 字典项
        var dicts = helper.Query<dynamic>(
          @"SELECT ID, DICTNAME FROM tss_dict WHERE DICTNAME LIKE @kw ORDER BY DICTNAME LIMIT 10",
          new { kw });
        var list = new List<object>();
        foreach (var d in dicts)
        {
          string dictId = (string)d.ID;
          string dictName = (string)d.DICTNAME;
          var items = helper.Query<dynamic>(
            @"SELECT ITEMVALUE, ITEMNAME, ENTRYNUM FROM tss_dictitem
              WHERE DICTID=@did ORDER BY ENTRYNUM",
            new { did = dictId });
          var itemList = new List<object>();
          foreach (var it in items)
          {
            itemList.Add(new { value = (string)it.ITEMVALUE, name = (string)it.ITEMNAME });
          }
          list.Add(new { dictName, items = itemList });
        }
        return new
        {
          count = list.Count,
          dicts = list,
          hint = list.Count == 0
            ? "未找到匹配字典。可调用 create_dict 创建新字典（产出 tss_dict/tss_dictitem INSERT），创建后在 SELECTDATA 引用该字典名。"
            : "在 configure_form_field / configure_list_column 的 SELECTDATA 中直接引用上述字典名(dictName)。"
        };
      }
    }

    // D2. create_dict：产出 tss_dict + tss_dictitem INSERT SQL（仅在 search_dict 找不到时用）
    private object CreateDict(JObject args)
    {
      string dictName = args["dictName"]?.ToString();
      var items = args["items"] as JArray;
      if (string.IsNullOrEmpty(dictName)) return new { error = "dictName 不能为空" };
      if (items == null || items.Count == 0) return new { error = "items 不能为空" };

      // 先检查是否已存在同名字典，避免重复创建
      DBHelper helper0 = DB.GetDBHelper();
      using (helper0)
      {
        int existCnt = helper0.QueryFirstOrDefault<int>(
          "SELECT COUNT(1) FROM tss_dict WHERE DICTNAME=@name",
          new { name = dictName });
        if (existCnt > 0)
          return new { error = "字典[" + dictName + "]已存在，请直接在 SELECTDATA 引用该字典名，不要重复创建" };
      }

      string dictId = "dict_" + Guid.NewGuid().ToString("N").Substring(0, 12);
      var sqlList = new List<string>();
      sqlList.Add("INSERT INTO tss_dict (ID, DICTNAME) VALUES ('" + dictId + "', '" + dictName.Replace("'", "") + "');");

      int entryNum = 0;
      var itemList = new List<object>();
      foreach (JObject it in items)
      {
        string value = it["value"]?.ToString();
        string name = it["name"]?.ToString();
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(name)) continue;
        entryNum++;
        string itemId = dictId + "_item_" + entryNum;
        sqlList.Add("INSERT INTO tss_dictitem (ID, DICTID, ITEMVALUE, ITEMNAME, ENTRYNUM) VALUES ('" + itemId + "', '" + dictId + "', '" + value.Replace("'", "") + "', '" + name.Replace("'", "") + "', " + entryNum + ");");
        itemList.Add(new { id = itemId, value, name, entryNum });
      }
      string sql = string.Join("\n", sqlList);
      var metadata = new JObject
      {
        ["dict"] = new JObject { ["ID"] = dictId, ["DICTNAME"] = dictName, ["items"] = JArray.FromObject(itemList) }
      };
      return new { sql, metadata, warnings = "字典[" + dictName + "]将随升级脚本创建，创建后在 SELECTDATA 引用字典名: " + dictName };
    }

    // E. define_filter：校验三条铁律 + 产出 tss_resfilter INSERT
    private object DefineFilter(JObject args)
    {
      string resourceId = args["resourceId"]?.ToString();
      string filterCode = args["filterCode"]?.ToString();
      string filterSql = args["filterSql"]?.ToString();
      string orderBy = args["orderBy"]?.ToString();
      if (string.IsNullOrEmpty(resourceId)) return new { error = "resourceId 不能为空" };
      if (string.IsNullOrEmpty(filterCode)) return new { error = "filterCode 不能为空" };
      if (string.IsNullOrEmpty(filterSql)) return new { error = "filterSql 不能为空" };

      // 铁律校验
      var warnings = new List<string>();
      // NVelocity 不能处理单引号
      if (filterSql.Contains("'"))
        return new { error = "FILTERSQL 含单引号，NVelocity 解析会失败，必须移除" };
      // 铁律1：F01/F02 必须以 1=1 开头
      if (filterCode == "F01" || filterCode == "F02")
      {
        if (!filterSql.TrimStart().StartsWith("1=1"))
          return new { error = "F01/F02 过滤器必须以 '1=1' 开头" };
        // 铁律2：F01 必须用 @INPUT
        if (filterCode == "F01" && !filterSql.Contains("@INPUT"))
          warnings.Add("F01 列表查询建议用 @INPUT 参数接收前端 QQRY 字段");
      }
      // 铁律3：ORDERBY 不能带表别名前缀
      if (!string.IsNullOrEmpty(orderBy) && orderBy.Contains("."))
        warnings.Add("ORDERBY '" + orderBy + "' 带表别名前缀，ORM 包子查询后外层别名是 T");

      // 注意：filterSql 是 LLM 写的 NVelocity 模板，含 #if/@VAR 等模板语法，
      // 无法做严格白名单过滤（不同于字段名/类型）。当前仅拦单引号 + 上面的关键字校验，
      // 残留注入风险（--、/* */、反斜杠等可能通过）。变更包审核阶段需人工复核 FILTERSQL。
      string filterId = "filt_" + filterCode.ToLower() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
      string sql = "INSERT INTO tss_resfilter (ID, RESOURCEID, FILTERCODE, FILTERSQL, ORDERBY) VALUES ('" + filterId + "', '" + resourceId + "', '" + filterCode + "', '" + filterSql.Replace("'", "") + "', " + (string.IsNullOrEmpty(orderBy) ? "NULL" : "'" + orderBy.Replace("'", "") + "'") + ");";

      var metadata = new JObject
      {
        ["filter"] = new JObject
        {
          ["ID"] = filterId,
          ["RESOURCEID"] = resourceId,
          ["FILTERCODE"] = filterCode,
          ["FILTERSQL"] = filterSql,
          ["ORDERBY"] = orderBy
        }
      };
      return new { sql, metadata, warnings = warnings.Count > 0 ? warnings.ToArray() : null };
    }

    // F1. register_module：产出 tss_moudle INSERT
    private object RegisterModule(JObject args)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string moduleName = args["moduleName"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(moduleName)) return new { error = "moduleName 不能为空" };

      // 模块编码唯一性(数据库有 uk_moudle_code 唯一索引, 重复 INSERT 直接报错):
      // 工具层先查重, 给 LLM 明确的改名指引(加序号/换前缀), 避免执行阶段才炸
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var exist = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, MODULENAME FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1",
          new { mc = moduleCode });
        if (exist != null)
        {
          return new { error = "模块编码 " + moduleCode + " 已被占用(现有模块: " + (string)exist.MODULENAME + ")。模块编码不能重复, 请换一个: ①同前缀递增序号(如 R02_M07→R02_M08) ②或换业务前缀。确认新编码后重新调 register_module" };
        }
      }

      string moduleId = "mod_" + moduleCode.ToLower() + "_001";
      string sql = "INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME) VALUES ('" + moduleId + "', '" + moduleCode + "', '" + moduleName.Replace("'", "") + "');";
      var metadata = new JObject
      {
        ["module"] = new JObject { ["ID"] = moduleId, ["MODULECODE"] = moduleCode, ["MODULENAME"] = moduleName }
      };
      return new { sql, metadata };
    }

    // F2. define_api：产出 tss_moudleapi INSERT。
    /// changesetId 用于时序裂缝修复：模块 DB 找不到时，兜底查同会话 DRAFT 变更项。
    private object DefineApi(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      string apiType = args["apiType"]?.ToString();
      string pathName = args["pathName"]?.ToString();
      string filterCode = args["filterCode"]?.ToString();
      string actionCode = args["actionCode"]?.ToString();
      string apiParam = args["apiParam"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(apiCode)) return new { error = "apiCode 不能为空" };
      if (string.IsNullOrEmpty(apiType)) return new { error = "apiType 不能为空" };
      if (string.IsNullOrEmpty(actionCode)) return new { error = "actionCode 不能为空（前端 getApi 靠它找接口）" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 先查 DB
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        string moduleId = mod != null ? (string)mod.ID : null;
        // DB 找不到 → 兜底查同会话 DRAFT 变更项（前序 register_module 未写库的场景）
        if (string.IsNullOrEmpty(moduleId))
          moduleId = LookupDraftModuleId(helper, changesetId, moduleCode);
        if (string.IsNullOrEmpty(moduleId))
          return new { error = "模块 " + moduleCode + " 未注册，请先 register_module（DB 或同会话 DRAFT 项里都没有）" };

        string apiId = "api_" + moduleCode.ToLower() + "_" + apiCode.ToLower();
        string sql = "INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, PATHNAME, FILTERCODE, ACTIONCODE, APIPARAM) VALUES ('" +
          apiId + "', '" + moduleId + "', '" + apiCode + "', '" + apiType + "', '" + pathName + "', " +
          (string.IsNullOrEmpty(filterCode) ? "NULL" : "'" + filterCode + "'") + ", '" + actionCode + "', " +
          (string.IsNullOrEmpty(apiParam) ? "NULL" : "'" + apiParam.Replace("'", "") + "'") + ");";
        var metadata = new JObject
        {
          ["moudleapi"] = new JObject
          {
            ["ID"] = apiId,
            ["MODULEID"] = moduleId,
            ["APICODE"] = apiCode,
            ["APITYPE"] = apiType,
            ["PATHNAME"] = pathName,
            ["FILTERCODE"] = filterCode,
            ["ACTIONCODE"] = actionCode,
            ["APIPARAM"] = apiParam
          }
        };
        return new { sql, metadata };
      }
    }

    // F3. define_sql_api：产出 tss_sql INSERT + tss_moudleapi(APITYPE=sql) INSERT（自定义接口元数据化）。
    /// SQLTXT 用 0x 十六进制字面量写入（多语句模板含分号，若按字符串字面量写入会被
    /// UpgradeExecutor 的分号拆分误判；HEX 同时避开一切转义问题）。
    private object DefineSqlApi(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      string apiName = args["apiName"]?.ToString();
      string actionCode = args["actionCode"]?.ToString();
      string sqlCode = args["sqlCode"]?.ToString();
      string sqlTxt = args["sqlTxt"]?.ToString();
      string remark = args["remark"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(apiCode)) return new { error = "apiCode 不能为空" };
      if (string.IsNullOrEmpty(apiName)) return new { error = "apiName 不能为空" };
      if (string.IsNullOrEmpty(actionCode)) return new { error = "actionCode 不能为空（前端 getApi 靠它找接口）" };
      if (string.IsNullOrEmpty(sqlCode)) return new { error = "sqlCode 不能为空" };
      if (sqlCode.Length > 16) return new { error = "sqlCode 长度不能超过 16 字符（tss_moudleapi.SQLID 列是 varchar(16)，如 SS_ACCEPT_DONE）" };
      if (string.IsNullOrEmpty(sqlTxt)) return new { error = "sqlTxt 不能为空" };

      // 铁律1：禁单引号（NVelocity 解析会失败）
      if (sqlTxt.Contains("'"))
        return new { error = "sqlTxt 含单引号，NVelocity 解析会失败。请改用 @参数传值，或 CHAR(39) 拼接" };
      // 铁律2：禁 DDL（首关键字预检）
      foreach (var s in SqlScriptHelper.SplitSqlStatements(sqlTxt))
      {
        string ddl = SqlScriptHelper.MatchDdlKeyword(s);
        if (ddl != null)
          return new { error = "sqlTxt 含 DDL 关键字 " + ddl + "，脚本接口禁止 DDL。结构变更请用 create_physical_table / configure_resource_field" };
      }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 模块必须存在（DB 或同会话 DRAFT 项）
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        string moduleId = mod != null ? (string)mod.ID : null;
        if (string.IsNullOrEmpty(moduleId))
          moduleId = LookupDraftModuleId(helper, changesetId, moduleCode);
        if (string.IsNullOrEmpty(moduleId))
          return new { error = "模块 " + moduleCode + " 未注册，请先 register_module（DB 或同会话 DRAFT 项里都没有）" };

        // SQLCODE 唯一性（复用旧资源原则：已存在不重复建，提示改 UPDATE 思路；统一代码资产表）
        var existSql = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_code_asset WHERE ASSETTYPE='sql' AND CODE=@sc LIMIT 1", new { sc = sqlCode });
        if (existSql != null)
          return new { error = "SQLCODE " + sqlCode + " 已存在（ID=" + (string)existSql.ID + "），请换一个编码，或说明修改点产出 UPDATE tss_code_asset" };

        // tss_code_asset INSERT（SOURCECODE 走 HEX 字面量）
        string sqlRowId = "sql_" + sqlCode.ToLower();
        string sqlInsert = "INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, SOURCECODE, SQLTYPE, REMARK, ISDELETED) VALUES ('" +
          sqlRowId + "', 'sql', '" + sqlCode + "', " + SqlStr(remark) + ", 0x" + ToHexString(sqlTxt) + ", 'mysql', " + SqlStr(remark) + ", 0);";

        // tss_moudleapi INSERT（APITYPE=sql + SQLID=SQLCODE，PATHNAME 约定 MAIN）
        string apiId = "api_" + moduleCode.ToLower() + "_" + apiCode.ToLower();
        string apiInsert = "INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, SQLID) VALUES ('" +
          apiId + "', '" + moduleId + "', '" + apiCode + "', 'sql', " + SqlStr(apiName) + ", '" + actionCode + "', 'MAIN', '" + sqlCode + "');";

        string sql = sqlInsert + "\n" + apiInsert;
        var metadata = new JObject
        {
          ["sql"] = new JObject
          {
            ["SQLID"] = sqlRowId,
            ["SQLCODE"] = sqlCode,
            ["SQLTXT"] = sqlTxt,
            ["REMARK"] = remark
          },
          ["moudleapi"] = new JObject
          {
            ["ID"] = apiId,
            ["MODULEID"] = moduleId,
            ["APICODE"] = apiCode,
            ["APITYPE"] = "sql",
            ["APINAME"] = apiName,
            ["ACTIONCODE"] = actionCode,
            ["PATHNAME"] = "MAIN",
            ["SQLID"] = sqlCode
          }
        };
        return new { sql, metadata };
      }
    }

    /// 字符串 → UTF8 十六进制（MySQL 0x 字面量写入用）
    private static string ToHexString(string s)
    {
      var bytes = System.Text.Encoding.UTF8.GetBytes(s ?? "");
      var sb = new System.Text.StringBuilder(bytes.Length * 2);
      foreach (var b in bytes) sb.Append(b.ToString("x2"));
      return sb.ToString();
    }

    // F4. read_api_script：读已有 C# 脚本源码（只读，AI 学习写法用；统一代码资产表）
    private object ReadApiScript(string scriptCode)
    {
      if (string.IsNullOrEmpty(scriptCode)) return new { error = "scriptCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var t = helper.QueryFirstOrDefault<dynamic>(
          "SELECT CODE, NAME, SOURCECODE, VERSION, REMARK FROM tss_code_asset WHERE ASSETTYPE='csharp' AND CODE=@sc AND ISDELETED=0 LIMIT 1",
          new { sc = scriptCode });
        if (t == null) return new { error = "脚本 " + scriptCode + " 不存在" };
        return new
        {
          scriptCode = (string)t.CODE,
          scriptName = (string)t.NAME,
          sourceCode = (string)t.SOURCECODE,
          version = t.VERSION == null ? 1 : (int)t.VERSION,
          remark = (string)t.REMARK
        };
      }
    }

    // F4b. read_module_template：读业务模板详情（只读，AI 学习完整模块的元数据组织方式）
    // search_module_template：按关键词搜索业务模板市场
    private object SearchModuleTemplate(string keyword)
    {
      if (string.IsNullOrEmpty(keyword)) return new { error = "keyword 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var rows = helper.Query<dynamic>(
          @"SELECT TEMPLATECODE, TEMPLATENAME, CATEGORY, DESCRIPTION
            FROM tss_module_template
            WHERE ISDELETED=0 AND (TEMPLATENAME LIKE @kw OR TEMPLATECODE LIKE @kw OR CATEGORY LIKE @kw OR DESCRIPTION LIKE @kw)
            ORDER BY TEMPLATENAME
            LIMIT 20",
          new { kw = "%" + keyword + "%" });
        var list = new List<object>();
        foreach (var r in rows)
        {
          list.Add(new
          {
            templateCode = (string)r.TEMPLATECODE,
            templateName = (string)r.TEMPLATENAME,
            category = (string)r.CATEGORY,
            description = (string)r.DESCRIPTION
          });
        }
        if (list.Count == 0) return new { found = false, message = "未找到匹配模板，请换关键词或直接从零创建" };
        return new { found = true, templates = list };
      }
    }

    private object ReadModuleTemplate(string templateCode)
    {
      if (string.IsNullOrEmpty(templateCode)) return new { error = "templateCode 不能为空" };
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var t = helper.QueryFirstOrDefault<dynamic>(
          "SELECT TEMPLATECODE, TEMPLATENAME, CATEGORY, DESCRIPTION, VARIABLES, SCRIPT, SOURCEINFO, VERSION FROM tss_module_template WHERE TEMPLATECODE=@tc AND ISDELETED=0 LIMIT 1",
          new { tc = templateCode });
        if (t == null) return new { error = "模板 " + templateCode + " 不存在" };
        string script = (string)t.SCRIPT ?? "";
        int scriptLen = script.Length;
        // 脚本可能很大（几十KB），截断返回（AI 学习结构足够）
        if (scriptLen > 20000) script = script.Substring(0, 20000) + "\n-- ...(截断, 共 " + scriptLen + " 字符)";
        return new
        {
          templateCode = (string)t.TEMPLATECODE,
          templateName = (string)t.TEMPLATENAME,
          category = (string)t.CATEGORY,
          description = (string)t.DESCRIPTION,
          variables = (string)t.VARIABLES,
          script,
          scriptLen,
          sourceInfo = (string)t.SOURCEINFO,
          version = (string)t.VERSION
        };
      }
    }

    // F5. define_script_api：产出 tss_api_script INSERT + tss_moudleapi(APITYPE=csharp) INSERT。
    /// SOURCECODE 先经 Roslyn 编译检查（失败即返回错误让 AI 修正），再用 0x HEX 写入
    /// （C# 源码含大量引号/分号，HEX 同时避开转义与升级执行器的分号拆分误判）。
    private object DefineScriptApi(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      string apiName = args["apiName"]?.ToString();
      string actionCode = args["actionCode"]?.ToString();
      string scriptCode = args["scriptCode"]?.ToString();
      string scriptName = args["scriptName"]?.ToString();
      string sourceCode = args["sourceCode"]?.ToString();
      string remark = args["remark"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(apiCode)) return new { error = "apiCode 不能为空" };
      if (string.IsNullOrEmpty(apiName)) return new { error = "apiName 不能为空" };
      if (string.IsNullOrEmpty(actionCode)) return new { error = "actionCode 不能为空（前端 getApi 靠它找接口）" };
      if (string.IsNullOrEmpty(scriptCode)) return new { error = "scriptCode 不能为空" };
      if (string.IsNullOrEmpty(scriptName)) return new { error = "scriptName 不能为空" };
      if (string.IsNullOrEmpty(sourceCode)) return new { error = "sourceCode 不能为空" };

      // Roslyn 编译预检（提前拦截语法错误，避免写入后运行时才暴露）
      var compileErrors = Realso.WebAPI.Services.Scripting.CSharpScriptEngine.CheckSyntax(sourceCode);
      if (compileErrors.Count > 0)
      {
        return new { error = "源码编译失败，请修正后重新调用： " + string.Join("；", compileErrors.Take(3)) };
      }
      // DDL 预检（脚本内 SQL 字符串禁 DDL）
      var ddlMatch = System.Text.RegularExpressions.Regex.Match(sourceCode, @"\b(DROP|ALTER|TRUNCATE)\s+(TABLE|VIEW)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
      if (ddlMatch.Success)
        return new { error = "源码含 DDL（" + ddlMatch.Value + "），脚本接口禁止结构变更，请用 create_physical_table / configure_resource_field" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 模块必须存在（DB 或同会话 DRAFT 项）
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        string moduleId = mod != null ? (string)mod.ID : null;
        if (string.IsNullOrEmpty(moduleId))
          moduleId = LookupDraftModuleId(helper, changesetId, moduleCode);
        if (string.IsNullOrEmpty(moduleId))
          return new { error = "模块 " + moduleCode + " 未注册，请先 register_module（DB 或同会话 DRAFT 项里都没有）" };

        // SCRIPTCODE 唯一性（已存在 → 提示产出 UPDATE 变更；统一代码资产表）
        var existScript = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_code_asset WHERE ASSETTYPE='csharp' AND CODE=@sc AND ISDELETED=0 LIMIT 1", new { sc = scriptCode });
        if (existScript != null)
          return new { error = "脚本 " + scriptCode + " 已存在（ID=" + (string)existScript.ID + "），如需修改请产出 UPDATE tss_code_asset SET SOURCECODE=...（说明修改点）" };

        // tss_code_asset INSERT（SOURCECODE 走 HEX 字面量）
        string scriptRowId = "as_" + scriptCode.ToLower();
        string scriptInsert = "INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, SOURCECODE, VERSION, REMARK, ISDELETED) VALUES ('" +
          scriptRowId + "', 'csharp', '" + scriptCode + "', " + SqlStr(scriptName) + ", 0x" + ToHexString(sourceCode) + ", 1, " + SqlStr(remark) + ", 0);";

        // tss_moudleapi INSERT（APITYPE=csharp + SCRIPTCODE 指向脚本，PATHNAME 约定 MAIN）
        string apiId = "api_" + moduleCode.ToLower() + "_" + apiCode.ToLower();
        string apiInsert = "INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, SCRIPTCODE) VALUES ('" +
          apiId + "', '" + moduleId + "', '" + apiCode + "', 'csharp', " + SqlStr(apiName) + ", '" + actionCode + "', 'MAIN', '" + scriptCode + "');";

        string sql = scriptInsert + "\n" + apiInsert;
        var metadata = new JObject
        {
          ["script"] = new JObject
          {
            ["ID"] = scriptRowId,
            ["SCRIPTCODE"] = scriptCode,
            ["SCRIPTNAME"] = scriptName,
            ["SOURCECODE_LEN"] = sourceCode.Length,
            ["REMARK"] = remark
          },
          ["moudleapi"] = new JObject
          {
            ["ID"] = apiId,
            ["MODULEID"] = moduleId,
            ["APICODE"] = apiCode,
            ["APITYPE"] = "csharp",
            ["APINAME"] = apiName,
            ["ACTIONCODE"] = actionCode,
            ["PATHNAME"] = "MAIN",
            ["SCRIPTCODE"] = scriptCode
          }
        };
        return new { sql, metadata };
      }
    }

    // F6. define_script_flow_api：产出 tss_moudleapi INSERT（APITYPE=script, APIPARAM=steps JSON）
    private object DefineScriptFlowApi(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      string apiName = args["apiName"]?.ToString();
      string actionCode = args["actionCode"]?.ToString();
      string stepsJson = args["steps"]?.ToString();
      string remark = args["remark"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(apiCode)) return new { error = "apiCode 不能为空" };
      if (string.IsNullOrEmpty(apiName)) return new { error = "apiName 不能为空" };
      if (string.IsNullOrEmpty(actionCode)) return new { error = "actionCode 不能为空（前端 getApi 靠它找接口）" };
      if (string.IsNullOrEmpty(stepsJson)) return new { error = "steps 不能为空" };

      // 解析并校验步骤 JSON
      JArray steps;
      try
      {
        var parsed = JToken.Parse(stepsJson);
        if (parsed is JArray) steps = (JArray)parsed;
        else return new { error = "steps 不是合法的 JSON 数组" };
      }
      catch (Exception ex)
      {
        return new { error = "steps JSON 解析失败: " + ex.Message };
      }
      if (steps.Count == 0) return new { error = "steps 数组不能为空" };

      var validTypes = new HashSet<string> { "sql", "query", "if", "update", "return" };
      for (int i = 0; i < steps.Count; i++)
      {
        var step = steps[i] as JObject;
        if (step == null) return new { error = "步骤 " + i + " 不是合法的 JSON 对象" };
        string type = step["type"]?.ToString()?.ToLower() ?? "";
        if (!validTypes.Contains(type)) return new { error = "步骤 " + i + " 未知类型: " + type + "，合法值: sql/query/if/update/return" };
        if ((type == "sql" || type == "update") && string.IsNullOrEmpty(step["sqlCode"]?.ToString()))
          return new { error = "步骤 " + i + "(type=" + type + ") 缺少 sqlCode" };
        if (type == "query" && string.IsNullOrEmpty(step["apiCode"]?.ToString()))
          return new { error = "步骤 " + i + "(type=query) 缺少 apiCode" };
        if (type == "if")
        {
          if (string.IsNullOrEmpty(step["cond"]?.ToString())) return new { error = "步骤 " + i + "(type=if) 缺少 cond" };
          if (step["goto"] == null) return new { error = "步骤 " + i + "(type=if) 缺少 goto" };
          int gotoVal = step["goto"].Value<int>();
          if (gotoVal < 0 || gotoVal >= steps.Count) return new { error = "步骤 " + i + " goto=" + gotoVal + " 超出范围(0-" + (steps.Count - 1) + ")" };
        }
      }

      // 序列化步骤 JSON（确保格式化）
      string apiParam = JsonConvert.SerializeObject(steps, Formatting.None);

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 模块必须存在
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        string moduleId = mod != null ? (string)mod.ID : null;
        if (string.IsNullOrEmpty(moduleId))
          moduleId = LookupDraftModuleId(helper, changesetId, moduleCode);
        if (string.IsNullOrEmpty(moduleId))
          return new { error = "模块 " + moduleCode + " 未注册，请先 register_module" };

        // APICODE 唯一性
        var existApi = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudleapi WHERE MODULEID=@mid AND APICODE=@ac LIMIT 1",
          new { mid = moduleId, ac = apiCode });
        if (existApi != null)
          return new { error = "接口 " + apiCode + " 已存在（ID=" + (string)existApi.ID + "），如需修改请产出 UPDATE" };

        // tss_moudleapi INSERT（APITYPE=script, APIPARAM=steps JSON）
        string apiId = "api_" + moduleCode.ToLower() + "_" + apiCode.ToLower();
        string apiInsert = "INSERT INTO tss_moudleapi (ID, MODULEID, APICODE, APITYPE, APINAME, ACTIONCODE, PATHNAME, APIPARAM) VALUES ('" +
          apiId + "', '" + moduleId + "', '" + apiCode + "', 'script', " + SqlStr(apiName) + ", '" + actionCode + "', 'MAIN', " + SqlStr(apiParam) + ");";

        var metadata = new JObject
        {
          ["moudleapi"] = new JObject
          {
            ["ID"] = apiId,
            ["MODULEID"] = moduleId,
            ["APICODE"] = apiCode,
            ["APITYPE"] = "script",
            ["APINAME"] = apiName,
            ["ACTIONCODE"] = actionCode,
            ["PATHNAME"] = "MAIN",
            ["APIPARAM"] = apiParam,
            ["REMARK"] = remark
          }
        };
        return new { sql = apiInsert, metadata };
      }
    }

    // F7. update_script_flow_api：修改已有编排接口步骤
    private object UpdateScriptFlowApi(JObject args, string changesetId = null)
    {
      string apiId = args["apiId"]?.ToString();
      string stepsJson = args["steps"]?.ToString();
      string apiName = args["apiName"]?.ToString();
      string actionCode = args["actionCode"]?.ToString();
      if (string.IsNullOrEmpty(apiId)) return new { error = "apiId 不能为空" };
      if (string.IsNullOrEmpty(stepsJson)) return new { error = "steps 不能为空" };

      // 解析并校验步骤 JSON
      JArray steps;
      try
      {
        var parsed = JToken.Parse(stepsJson);
        if (parsed is JArray) steps = (JArray)parsed;
        else return new { error = "steps 不是合法的 JSON 数组" };
      }
      catch (Exception ex)
      {
        return new { error = "steps JSON 解析失败: " + ex.Message };
      }
      if (steps.Count == 0) return new { error = "steps 数组不能为空" };

      var validTypes = new HashSet<string> { "sql", "query", "if", "update", "return" };
      for (int i = 0; i < steps.Count; i++)
      {
        var step = steps[i] as JObject;
        if (step == null) return new { error = "步骤 " + i + " 不是合法的 JSON 对象" };
        string type = step["type"]?.ToString()?.ToLower() ?? "";
        if (!validTypes.Contains(type)) return new { error = "步骤 " + i + " 未知类型: " + type + "，合法值: sql/query/if/update/return" };
        if ((type == "sql" || type == "update") && string.IsNullOrEmpty(step["sqlCode"]?.ToString()))
          return new { error = "步骤 " + i + "(type=" + type + ") 缺少 sqlCode" };
        if (type == "query" && string.IsNullOrEmpty(step["apiCode"]?.ToString()))
          return new { error = "步骤 " + i + "(type=query) 缺少 apiCode" };
        if (type == "if")
        {
          if (string.IsNullOrEmpty(step["cond"]?.ToString())) return new { error = "步骤 " + i + "(type=if) 缺少 cond" };
          if (step["goto"] == null) return new { error = "步骤 " + i + "(type=if) 缺少 goto" };
          int gotoVal = step["goto"].Value<int>();
          if (gotoVal < 0 || gotoVal >= steps.Count) return new { error = "步骤 " + i + " goto=" + gotoVal + " 超出范围(0-" + (steps.Count - 1) + ")" };
        }
      }

      // 序列化步骤 JSON（确保格式化）
      string apiParam = JsonConvert.SerializeObject(steps, Formatting.None);

      // 构建 UPDATE SQL
      string setClauses = "APIPARAM=" + SqlStr(apiParam);
      if (!string.IsNullOrEmpty(apiName)) setClauses += ", APINAME=" + SqlStr(apiName);
      if (!string.IsNullOrEmpty(actionCode)) setClauses += ", ACTIONCODE=" + SqlStr(actionCode);
      string sql = "UPDATE tss_moudleapi SET " + setClauses + " WHERE ID='" + apiId.Replace("'", "") + "';";

      var metadata = new JObject
      {
        ["moudleapi"] = new JObject
        {
          ["ID"] = apiId,
          ["APITYPE"] = "script",
          ["APIPARAM"] = apiParam,
          ["APINAME"] = apiName ?? "",
          ["ACTIONCODE"] = actionCode ?? ""
        }
      };
      return new { sql, metadata };
    }

    // F8. read_script_flow_api：读取已有编排接口配置（只读工具）
    private object ReadScriptFlowApi(JObject args)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(apiCode)) return new { error = "apiCode 不能为空" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        if (mod == null) return new { error = "模块 " + moduleCode + " 不存在" };
        string moduleId = (string)mod.ID;

        var api = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID, APICODE, APINAME, ACTIONCODE, APIPARAM FROM tss_moudleapi WHERE MODULEID=@mid AND APICODE=@ac LIMIT 1",
          new { mid = moduleId, ac = apiCode });
        if (api == null) return new { error = "接口 " + apiCode + " 不存在于模块 " + moduleCode };

        string apiParam = (string)api.APIPARAM ?? "[]";
        JArray steps;
        try
        {
          var parsed = JToken.Parse(apiParam);
          steps = parsed is JArray ? (JArray)parsed : new JArray();
        }
        catch
        {
          steps = new JArray();
        }

        return new
        {
          apiId = (string)api.ID,
          apiCode = (string)api.APICODE,
          apiName = (string)api.APINAME ?? "",
          actionCode = (string)api.ACTIONCODE ?? "",
          steps
        };
      }
    }

    // G1. create_menu：产出 tss_func INSERT
    private object CreateMenu(JObject args)
    {
      string funcCode = args["funcCode"]?.ToString();
      string funcName = args["funcName"]?.ToString();
      string outerUrl = args["outerUrl"]?.ToString();
      string upFuncId = args["upFuncId"]?.ToString();
      if (string.IsNullOrEmpty(funcCode)) return new { error = "funcCode 不能为空" };
      if (string.IsNullOrEmpty(funcName)) return new { error = "funcName 不能为空" };
      if (string.IsNullOrEmpty(outerUrl)) return new { error = "outerUrl 不能为空" };
      if (string.IsNullOrEmpty(upFuncId)) return new { error = "upFuncId 不能为空" };

      string funcId = "func_" + funcCode.ToLower() + "_001";
      string sql = "INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, ISHIDE) VALUES ('" +
        funcId + "', '" + funcCode + "', '" + funcName.Replace("'", "") + "', '" + outerUrl + "', '" + upFuncId + "', 0);";
      var metadata = new JObject
      {
        ["func"] = new JObject { ["ID"] = funcId, ["FUNCCODE"] = funcCode, ["FUNCNAME"] = funcName, ["OUTERURL"] = outerUrl, ["UPFUNCID"] = upFuncId }
      };
      return new { sql, metadata };
    }

    // G2. create_funcpoints：产出 tss_funcpoint INSERT
    private object CreateFuncpoints(JObject args)
    {
      string funcCode = args["funcCode"]?.ToString();
      var points = args["points"] as JArray;
      if (string.IsNullOrEmpty(funcCode)) return new { error = "funcCode 不能为空" };
      if (points == null || points.Count == 0) return new { error = "points 不能为空" };

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var func = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_func WHERE FUNCCODE=@fc LIMIT 1", new { fc = funcCode });
        if (func == null) return new { error = "菜单 " + funcCode + " 未注册，请先 create_menu" };
        string funcId = (string)func.ID;

        var sqlList = new List<string>();
        var pointList = new List<object>();
        int idx = 0;
        foreach (var p in points)
        {
          string pointCode = p.ToString();
          if (string.IsNullOrEmpty(pointCode)) continue;
          idx++;
          string pointId = "fp_" + funcCode.ToLower() + "_" + pointCode.ToLower();
          // 真实表结构: FUNCID 指向 tss_func.ID（无 FUNCCODE 列）
          sqlList.Add("INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME) VALUES ('" +
            pointId + "', '" + funcId + "', '" + pointCode + "', '" + pointCode + "');");
          pointList.Add(new { id = pointId, funcId, pointCode });
        }
        string sql = string.Join("\n", sqlList);
        var metadata = new JObject { ["funcpoints"] = JArray.FromObject(pointList) };
        return new { sql, metadata };
      }
    }

    // I1. define_page：产出 tss_module_page INSERT（GenericModule 页面清单）
    /// 页面 ID 用确定性规则 mp_{moduleCode}_{pageCode}（幂等 + define_button 可直接推导）。
    /// 模块必须已注册（DB 或同会话 DRAFT 项）；同模块同 pageCode 已存在则拒绝重复定义。
    private object DefinePage(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string pageCode = args["pageCode"]?.ToString();
      string pageName = args["pageName"]?.ToString();
      string pageType = args["pageType"]?.ToString();
      string routePath = args["routePath"]?.ToString();
      string componentType = args["componentType"]?.ToString();
      string sfcModulePath = args["sfcModulePath"]?.ToString();
      string queryApiCode = args["queryApiCode"]?.ToString();
      string openApiCode = args["openApiCode"]?.ToString();
      string saveApiCode = args["saveApiCode"]?.ToString();
      string pageConfig = args["pageConfig"]?.ToString();
      string parentId = args["parentId"]?.ToString();
      int sortNo = args["sortNo"]?.Type == JTokenType.Integer ? (int)args["sortNo"] : 0;
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(pageCode)) return new { error = "pageCode 不能为空" };
      if (string.IsNullOrEmpty(pageName)) return new { error = "pageName 不能为空" };
      if (string.IsNullOrEmpty(pageType)) return new { error = "pageType 不能为空" };

      // 枚举/格式校验（提前拦截，避免写入后运行时才暴露）
      var pageTypes = new HashSet<string> { "list", "form", "select", "review", "report" };
      if (!pageTypes.Contains(pageType)) return new { error = "pageType 必须是 list/form/select/review/report 之一" };
      if (string.IsNullOrEmpty(componentType)) componentType = "standard";
      if (componentType != "standard" && componentType != "sfc") return new { error = "componentType 必须是 standard 或 sfc" };
      if (componentType == "sfc" && string.IsNullOrEmpty(sfcModulePath)) return new { error = "componentType=sfc 时 sfcModulePath 必填" };
      if (!string.IsNullOrEmpty(pageConfig))
      {
        try { JObject.Parse(pageConfig); }
        catch { return new { error = "pageConfig 不是合法 JSON（前端运行时会 JSON.parse）" }; }
      }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 模块必须存在（DB 或同会话 DRAFT 项，跨步时序修复同 define_api）
        var mod = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_moudle WHERE MODULECODE=@mc LIMIT 1", new { mc = moduleCode });
        if (mod == null && string.IsNullOrEmpty(LookupDraftModuleId(helper, changesetId, moduleCode)))
          return new { error = "模块 " + moduleCode + " 未注册，请先 register_module（DB 或同会话 DRAFT 项里都没有）" };

        // form 页必须挂为 list 页的子页面（PARENTID=list页ID）：未传 parentId 时自动找同模块 list 页挂接。
        // generic-module formPageConfig 按 PARENTID 找 list 关联 form，缺了双击行/添加按钮打不开表单。
        JObject listPageRow = null;
        if (pageType == "form" && string.IsNullOrEmpty(parentId))
        {
          var listPage = helper.QueryFirstOrDefault<dynamic>(
            "SELECT ID, PAGECONFIG FROM tss_module_page WHERE MODULECODE=@mc AND PAGETYPE='list' AND ISDELETED=0 ORDER BY SORTNO LIMIT 1",
            new { mc = moduleCode });
          if (listPage != null)
          {
            parentId = (string)listPage.ID;
            listPageRow = new JObject { ["ID"] = parentId, ["PAGECONFIG"] = (string)listPage.PAGECONFIG };
          }
        }
        else if (pageType == "form" && !string.IsNullOrEmpty(parentId))
        {
          var listPage = helper.QueryFirstOrDefault<dynamic>(
            "SELECT ID, PAGECONFIG FROM tss_module_page WHERE ID=@pid AND ISDELETED=0 LIMIT 1",
            new { pid = parentId });
          if (listPage != null) listPageRow = new JObject { ["ID"] = parentId, ["PAGECONFIG"] = (string)listPage.PAGECONFIG };
        }

        // 同模块同 pageCode 已存在 → 拒绝重复定义（复用旧资源原则，修改走 UPDATE）
        var existPage = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_module_page WHERE MODULECODE=@mc AND PAGECODE=@pc AND ISDELETED=0 LIMIT 1",
          new { mc = moduleCode, pc = pageCode });
        if (existPage != null)
          return new { error = "页面 " + moduleCode + "/" + pageCode + " 已存在（ID=" + (string)existPage.ID + "），如需修改请产出 UPDATE 语句而不是重复 define_page" };

        string pageId = "mp_" + moduleCode.ToLower() + "_" + pageCode.ToLower();
        var cols = new List<string> { "ID", "MODULECODE", "PAGECODE", "PAGENAME", "PAGETYPE" };
        var vals = new List<string> { SqlStr(pageId), SqlStr(moduleCode), SqlStr(pageCode), SqlStr(pageName), SqlStr(pageType) };
        AddCol(cols, vals, "PARENTID", parentId);
        AddCol(cols, vals, "ROUTEPATH", routePath);
        AddCol(cols, vals, "COMPONENTTYPE", componentType);
        AddCol(cols, vals, "SFCMODULEPATH", sfcModulePath);
        AddCol(cols, vals, "QUERYAPICODE", queryApiCode);
        AddCol(cols, vals, "OPENAPICODE", openApiCode);
        AddCol(cols, vals, "SAVEAPICODE", saveApiCode);
        AddCol(cols, vals, "PAGECONFIG", pageConfig);
        cols.Add("SORTNO"); vals.Add(sortNo.ToString());
        cols.Add("ISDELETED"); vals.Add("0");
        string sql = "INSERT INTO tss_module_page (" + string.Join(", ", cols) + ") VALUES (" + string.Join(", ", vals) + ");";

        // form 页双写配套: 同步把子页面声明合并进 list 页 PAGECONFIG.SUBPAGES
        // (m18 配置界面顶层只显示 !PARENTID 页面, 子页面靠父页 SUBPAGES 展示; 缺了 m18 看不到 form 页)
        if (pageType == "form" && listPageRow != null)
        {
          try
          {
            var cfgStr = (string)listPageRow["PAGECONFIG"];
            JObject cfg = string.IsNullOrEmpty(cfgStr) ? new JObject() : JObject.Parse(cfgStr);
            var subPages = cfg["SUBPAGES"] as JArray ?? new JArray();
            bool exists = subPages.Any(sp => (string)sp["PAGEID"] == pageId);
            if (!exists)
            {
              subPages.Add(new JObject
              {
                ["PAGEID"] = pageId,
                ["PAGENAME"] = pageName,
                ["PAGETYPE"] = "form",
                ["COMPONENTTYPE"] = componentType,
                ["SFCMODULEPATH"] = "",
                ["REFMODULECODE"] = "",
                ["REFPAGECODE"] = "",
                ["MODALWIDTH"] = null,
                ["MODALFULLSCREEN"] = false
              });
              cfg["SUBPAGES"] = subPages;
              string listPageId = (string)listPageRow["ID"];
              sql += "\nUPDATE tss_module_page SET PAGECONFIG='" + cfg.ToString(Formatting.None).Replace("'", "''") + "' WHERE ID='" + listPageId.Replace("'", "") + "';";
            }
          }
          catch { /* SUBPAGES 合并失败不阻塞主 INSERT */ }
        }

        var metadata = new JObject
        {
          ["page"] = new JObject
          {
            ["ID"] = pageId,
            ["MODULECODE"] = moduleCode,
            ["PAGECODE"] = pageCode,
            ["PAGENAME"] = pageName,
            ["PAGETYPE"] = pageType,
            ["PARENTID"] = parentId,
            ["ROUTEPATH"] = routePath,
            ["COMPONENTTYPE"] = componentType,
            ["SFCMODULEPATH"] = sfcModulePath,
            ["QUERYAPICODE"] = queryApiCode,
            ["OPENAPICODE"] = openApiCode,
            ["SAVEAPICODE"] = saveApiCode,
            ["PAGECONFIG"] = pageConfig,
            ["SORTNO"] = sortNo
          }
        };
        return new { sql, metadata };
      }
    }

    // I2. define_button：产出 tss_module_button INSERT（页面按钮配置）
    /// 按钮 ID 用确定性规则 mb_{moduleCode}_{pageCode}_{hash8}（hash 取 btnArea|btnName|apiCode|sortNo）。
    /// PAGEID 优先取 DB 真实页面 ID（兼容手工配置的页面），否则用约定 ID mp_{moduleCode}_{pageCode}。
    private object DefineButton(JObject args, string changesetId = null)
    {
      string moduleCode = args["moduleCode"]?.ToString();
      string pageCode = args["pageCode"]?.ToString();
      string btnName = args["btnName"]?.ToString();
      string btnArea = args["btnArea"]?.ToString();
      string btnCode = args["btnCode"]?.ToString();
      string apiCode = args["apiCode"]?.ToString();
      string interactType = args["interactType"]?.ToString();
      string poptipText = args["poptipText"]?.ToString();
      string showCond = args["showCond"]?.ToString();
      string permCode = args["permCode"]?.ToString();
      string color = args["color"]?.ToString();
      string icon = args["icon"]?.ToString();
      string actionType = args["actionType"]?.ToString();
      string extParam = args["extParam"]?.ToString();
      int sortNo = args["sortNo"]?.Type == JTokenType.Integer ? (int)args["sortNo"] : 0;
      if (string.IsNullOrEmpty(moduleCode)) return new { error = "moduleCode 不能为空" };
      if (string.IsNullOrEmpty(pageCode)) return new { error = "pageCode 不能为空" };
      if (string.IsNullOrEmpty(btnName)) return new { error = "btnName 不能为空" };
      if (string.IsNullOrEmpty(btnArea)) return new { error = "btnArea 不能为空" };

      // 枚举校验
      if (btnArea != "header" && btnArea != "footer" && btnArea != "row" && !btnArea.StartsWith("DTS"))
        return new { error = "btnArea 必须是 header/footer/row 或 DTS 开头的子表路径" };
      if (string.IsNullOrEmpty(btnCode)) btnCode = "custom";
      var btnCodes = new HashSet<string> { "add", "edit", "select", "delete", "save", "export", "submit", "reSubmit", "check", "reCheck", "verify", "reVerify", "subAdd", "subRemove", "subUp", "subDown", "cancel", "custom" };
      if (!btnCodes.Contains(btnCode)) return new { error = "btnCode 必须是预设编码之一(add/edit/select/delete/save/export/submit/reSubmit/check/reCheck/verify/reVerify/subAdd/subRemove/subUp/subDown/cancel/custom)" };
      if (string.IsNullOrEmpty(interactType)) interactType = "direct";
      if (interactType != "direct" && interactType != "poptip") return new { error = "interactType 必须是 direct 或 poptip" };
      if (string.IsNullOrEmpty(actionType)) actionType = "api";
      if (actionType != "api" && actionType != "openForm" && actionType != "openSelector") return new { error = "actionType 必须是 api/openForm/openSelector 之一" };
      // 需要调接口的按钮编码必须配 apiCode（内置行为编码 add/edit/select/delete/save/export/cancel/sub* 除外）
      var needApiCodes = new HashSet<string> { "custom", "submit", "reSubmit", "check", "reCheck", "verify", "reVerify" };
      if (actionType == "api" && needApiCodes.Contains(btnCode) && string.IsNullOrEmpty(apiCode))
        return new { error = "btnCode=" + btnCode + " 的按钮必须配 apiCode（调哪个模块接口）" };
      if (!string.IsNullOrEmpty(extParam))
      {
        try { JObject.Parse(extParam); }
        catch { return new { error = "extParam 不是合法 JSON" }; }
      }

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        // 页面存在性：DB 查（兼容手工配置的页面，取真实 PAGEID）；查不到查同会话 DRAFT page 项
        string pageId = "mp_" + moduleCode.ToLower() + "_" + pageCode.ToLower();
        var pageRow = helper.QueryFirstOrDefault<dynamic>(
          "SELECT ID FROM tss_module_page WHERE MODULECODE=@mc AND PAGECODE=@pc AND ISDELETED=0 LIMIT 1",
          new { mc = moduleCode, pc = pageCode });
        if (pageRow != null)
        {
          pageId = (string)pageRow.ID;
        }
        else if (!DraftPageExists(helper, changesetId, moduleCode, pageCode))
        {
          return new { error = "页面 " + moduleCode + "/" + pageCode + " 不存在（DB 或同会话 DRAFT 项里都没有），请先 define_page" };
        }

        // BTNTYPE 推导：内置 CRUD 编码→crud；审批流编码→flow；其余→custom
        string btnType = "custom";
        var crudCodes = new HashSet<string> { "add", "edit", "select", "delete", "save", "export", "cancel", "subAdd", "subRemove", "subUp", "subDown" };
        var flowCodes = new HashSet<string> { "submit", "reSubmit", "check", "reCheck", "verify", "reVerify" };
        if (crudCodes.Contains(btnCode)) btnType = "crud";
        else if (flowCodes.Contains(btnCode)) btnType = "flow";

        string btnId = "mb_" + moduleCode.ToLower() + "_" + pageCode.ToLower() + "_" + ShortHash(btnArea + "|" + btnName + "|" + (apiCode ?? "") + "|" + sortNo);
        var cols = new List<string> { "ID", "PAGEID", "MODULECODE", "BTNNAME", "BTNTYPE", "BTNCODE", "BTNAREA", "INTERACTTYPE" };
        var vals = new List<string> { SqlStr(btnId), SqlStr(pageId), SqlStr(moduleCode), SqlStr(btnName), SqlStr(btnType), SqlStr(btnCode), SqlStr(btnArea), SqlStr(interactType) };
        AddCol(cols, vals, "APICODE", apiCode);
        AddCol(cols, vals, "POPTIPTEXT", poptipText);
        AddCol(cols, vals, "SHOWCOND", showCond);
        AddCol(cols, vals, "PERMCODE", permCode);
        AddCol(cols, vals, "ICON", icon);
        AddCol(cols, vals, "COLOR", color);
        AddCol(cols, vals, "EXTPARAM", extParam);
        cols.Add("SORTNO"); vals.Add(sortNo.ToString());
        cols.Add("ISDELETED"); vals.Add("0");
        string sql = "INSERT INTO tss_module_button (" + string.Join(", ", cols) + ") VALUES (" + string.Join(", ", vals) + ");";

        var metadata = new JObject
        {
          ["button"] = new JObject
          {
            ["ID"] = btnId,
            ["PAGEID"] = pageId,
            ["MODULECODE"] = moduleCode,
            ["PAGECODE"] = pageCode,
            ["APICODE"] = apiCode,
            ["BTNNAME"] = btnName,
            ["BTNTYPE"] = btnType,
            ["BTNCODE"] = btnCode,
            ["BTNAREA"] = btnArea,
            ["INTERACTTYPE"] = interactType,
            ["SHOWCOND"] = showCond,
            ["PERMCODE"] = permCode,
            ["ACTIONTYPE"] = actionType,
            ["EXTPARAM"] = extParam,
            ["SORTNO"] = sortNo
          }
        };
        return new { sql, metadata };
      }
    }

    /// 查同 changeset 内 DRAFT 变更项里是否已有 page 定义（跨步时序修复：Step3 define_button 引用的页面可能还是 DRAFT 未写库）。
    private bool DraftPageExists(DBHelper helper, string changesetId, string moduleCode, string pageCode)
    {
      if (string.IsNullOrEmpty(changesetId) || string.IsNullOrEmpty(moduleCode) || string.IsNullOrEmpty(pageCode)) return false;
      var rows = helper.Query<string>(
        @"SELECT METADATA FROM tss_aidev_changeitem
          WHERE CHANGESETID=@csid AND ISDELETED=0 AND CATEGORY='page' AND METADATA LIKE @pat",
        new { csid = changesetId, pat = "%" + pageCode + "%" });
      foreach (var meta in rows)
      {
        try
        {
          var jo = JObject.Parse(meta);
          var p = jo["page"];
          if (p != null && (string)p["MODULECODE"] == moduleCode && (string)p["PAGECODE"] == pageCode) return true;
        }
        catch { }
      }
      return false;
    }

    /// SQL 字符串字面量（单引号转义；空→NULL）
    private static string SqlStr(string v)
    {
      return string.IsNullOrEmpty(v) ? "NULL" : "'" + v.Replace("'", "''") + "'";
    }

    /// 可选列：值非空才加入 INSERT 列清单
    private static void AddCol(List<string> cols, List<string> vals, string col, string val)
    {
      if (string.IsNullOrEmpty(val)) return;
      cols.Add(col);
      vals.Add(SqlStr(val));
    }

    /// 8 位短 hash（确定性 ID 用，MD5 前 4 字节）
    private static string ShortHash(string s)
    {
      using (var md5 = System.Security.Cryptography.MD5.Create())
      {
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s ?? ""));
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 4; i++) sb.Append(bytes[i].ToString("x2"));
        return sb.ToString();
      }
    }

    // H1. create_sfc_module：产出 tbs_sfc_template INSERT（SFC 在线 Vue 模块）
    private object CreateSfcModule(JObject args)
    {
      string templateCode = args["templateCode"]?.ToString();
      string templateName = args["templateName"]?.ToString();
      string modulePath = args["modulePath"]?.ToString();
      string fileType = args["fileType"]?.ToString();
      if (string.IsNullOrEmpty(fileType)) fileType = "VUE";
      string sourceCode = args["sourceCode"]?.ToString();
      string deps = args["deps"]?.ToString();
      string description = args["description"]?.ToString();
      if (string.IsNullOrEmpty(templateCode)) return new { error = "templateCode 不能为空" };
      if (string.IsNullOrEmpty(templateName)) return new { error = "templateName 不能为空" };
      if (string.IsNullOrEmpty(modulePath)) return new { error = "modulePath 不能为空（如 @/pages/r02/m07/views/main.vue）" };
      if (string.IsNullOrEmpty(sourceCode)) return new { error = "sourceCode 不能为空（完整 Vue SFC 源码）" };

      // 重名校验：CODE 唯一（统一代码资产表，js/vue 域）
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        int exists = helper.QueryFirstOrDefault<int>(
          "SELECT COUNT(1) FROM tss_code_asset WHERE ASSETTYPE IN ('js','vue') AND CODE=@tc AND ISDELETED=0", new { tc = templateCode });
        if (exists > 0) return new { error = "SFC 模板编码 " + templateCode + " 已存在，请换一个 TEMPLATECODE" };
      }

      // 拼 INSERT（统一代码资产表；SOURCECODE 走 0x HEX 字面量——JS 源码含大量分号，
      // 字符串字面量会被 UpgradeExecutor 的分号拆分误判，HEX 一并解决转义问题）
      string id = Guid.NewGuid().ToString("N");
      string fileTypeLower = fileType.ToLower();
      string fileTypeUpper = fileType.ToUpper();
      string sql = "INSERT INTO tss_code_asset (ID, ASSETTYPE, CODE, NAME, MODULEPATH, FILETYPE, SOURCECODE, COMPILEDCODE, DEPS, REMARK, ISDELETED) VALUES ('" +
        id + "', '" + fileTypeLower + "', " + SqlStr(templateCode) + ", " + SqlStr(templateName) + ", " + SqlStr(modulePath) + ", '" + fileTypeUpper + "', 0x" + ToHexString(sourceCode) + ", NULL, " +
        (string.IsNullOrEmpty(deps) ? "NULL" : SqlStr(deps)) + ", " +
        (string.IsNullOrEmpty(description) ? "NULL" : SqlStr(description)) + ", 0);";

      var metadata = new JObject
      {
        ["sfc"] = new JObject
        {
          ["ID"] = id,
          ["TEMPLATECODE"] = templateCode,
          ["TEMPLATENAME"] = templateName,
          ["MODULEPATH"] = modulePath,
          ["FILETYPE"] = fileTypeUpper,
          ["DEPS"] = deps,
          ["DESCRIPTION"] = description,
          ["SOURCECODE_LEN"] = sourceCode.Length
        }
      };
      return new { sql, metadata };
    }

    // ---- 工具定义辅助 ----
    private static object Tool(string name, string desc, params object[] props)
    {
      var properties = new Dictionary<string, object>();
      var required = new List<string>();
      foreach (Dictionary<string, object> p in props)
      {
        properties[(string)p["name"]] = new { type = p["type"], description = p["desc"] };
        if (p.ContainsKey("required") && (bool)p["required"]) required.Add((string)p["name"]);
      }
      return new
      {
        type = "function",
        function = new
        {
          name,
          description = desc,
          parameters = new { type = "object", properties, required }
        }
      };
    }
    private static Dictionary<string, object> P(string name, string type, string desc, bool required)
    {
      return new Dictionary<string, object> { { "name", name }, { "type", type }, { "desc", desc }, { "required", required } };
    }

    // ============== 校验工具(self-correction loop, 2026-07-19) ==============
    // 设计原则: 每个工具返回 {passed, issues:[{severity, message, location}]},
    // AI 看到 severity=error 必须修正后再退出对话, 形成"生成→校验→修正"闭环。

    /// <summary>
    /// verify_sfc: 校验 SFC(.vue) 源码结构 + 命名规范 + import 模式。
    /// 检查项：
    ///   error: 必含 <template> + <script>; 字段名带下划线(SFC_MODULEPATH 这类非法); require.ensure 缺失(router 文件特例)。
    ///   warn: 缺 <style>(可接受); import @/api/db(违反 Store 规范); this.$store.dispatch(违反 $callAction 规范)。
    /// </summary>
    private static object VerifySfc(string sourceCode)
    {
      var issues = new List<object>();
      if (string.IsNullOrEmpty(sourceCode))
      {
        return new { passed = false, issues = new[] { new { severity = "error", message = "sourceCode 为空", location = "" } } };
      }
      // 结构检查
      if (!sourceCode.Contains("<template"))
        issues.Add(new { severity = "error", message = "SFC 缺 <template> 标签", location = "" });
      if (!sourceCode.Contains("<script"))
        issues.Add(new { severity = "error", message = "SFC 缺 <script> 标签", location = "" });
      // 命名规范: 属性名/字段名不应含下划线(SFC_MODULEPATH 这类)
      var badNameMatches = System.Text.RegularExpressions.Regex.Matches(sourceCode, @"\b[A-Z]+_[A-Z]+(?:_[A-Z]+)+\b");
      var badNames = badNameMatches.Cast<System.Text.RegularExpressions.Match>()
                                   .Select(m => m.Value).Distinct().Where(n => !IsAllowedUnderScoreName(n)).ToList();
      foreach (var n in badNames)
        issues.Add(new { severity = "warn", message = "发现疑似含下划线的大写标识符(" + n + "), 项目规范字段名应无下划线(如 SFCMODULEPATH 非 SFC_MODULEPATH)", location = n });
      // 前端规范
      if (sourceCode.Contains("import db") && sourceCode.Contains("@/api/db"))
        issues.Add(new { severity = "warn", message = ".vue 直接 import @/api/db 违反 Store 规范, 应通过 Vuex action + $callAction", location = "" });
      if (sourceCode.Contains("this.$store.dispatch"))
        issues.Add(new { severity = "warn", message = "禁止 this.$store.dispatch(...), 应使用 this.$callAction({action, param})", location = "" });
      if (sourceCode.Contains("SelStore") && !sourceCode.Contains("// "))
        issues.Add(new { severity = "warn", message = "store.js 使用 SelStore 时, 确保 app store 已加载(RS_M00), 否则 chunk 早执行会报错", location = "" });
      // Router 文件特殊检查
      if (sourceCode.Contains("require.ensure") || sourceCode.Contains("router"))
      {
        if (!sourceCode.Contains("require.ensure") && sourceCode.Contains("component:"))
          issues.Add(new { severity = "error", message = "router.js 必须用 require.ensure 懒加载, 禁同步 import", location = "" });
      }
      bool passed = !issues.Any(i => ((dynamic)i).severity == "error");
      return new { passed, issues };
    }

    /// <summary>
    /// verify_sql: 校验 SQL 模板(NVelocity) 常见错误。
    /// 检查项:
    ///   error: 含单引号(NVelocity 会截断); DDL 关键词(DROP/ALTER/TRUNCATE/CREATE); FILTERCODE 编号异常。
    ///   warn: LIKE 用字面量('%x%'应改 CONCAT); 注释含分号(旧 SplitSqlStatements 会误切)。
    /// </summary>
    private static object VerifySql(string sqlText)
    {
      var issues = new List<object>();
      if (string.IsNullOrEmpty(sqlText))
      {
        return new { passed = false, issues = new[] { new { severity = "error", message = "sqlText 为空", location = "" } } };
      }
      // 单引号(NVelocity 解析失败)
      if (sqlText.Contains("'"))
        issues.Add(new { severity = "error", message = "SQL 模板含单引号, NVelocity 会解析失败导致 SQL 截断。LIKE 用 CONCAT('%',@x,'%') 等价写法", location = "single quote" });
      // DDL 黑名单
      var ddlKws = new[] { "DROP ", "ALTER ", "TRUNCATE ", "CREATE TABLE", "GRANT ", "REVOKE ", "RENAME " };
      foreach (var kw in ddlKws)
      {
        if (sqlText.ToUpper().Contains(kw))
          issues.Add(new { severity = "error", message = "SQL 含 DDL 关键词(" + kw.Trim() + "), DataController.doSqlApi 禁止执行 DDL", location = kw.Trim() });
      }
      // LIKE 字面量(应改 CONCAT)
      if (System.Text.RegularExpressions.Regex.IsMatch(sqlText, @"LIKE\s+'[^@]*%"))
        issues.Add(new { severity = "warn", message = "LIKE 用字面量字符串, 应改用 LIKE CONCAT('%',@VAR,'%') 避免 NVelocity 单引号问题", location = "LIKE literal" });
      // 参数语法混用
      bool hasAt = sqlText.Contains("@");
      bool hasDollar = sqlText.Contains("$!");
      if (hasAt && hasDollar)
        issues.Add(new { severity = "info", message = "模板同时含 @VAR(Dapper) 和 $!{VAR}(NVelocity), 确认两种参数语义使用场景正确", location = "" });
      // 注释里含分号(SqlScriptHelper 已剥注释, 但提醒)
      if (System.Text.RegularExpressions.Regex.IsMatch(sqlText, @"--.*;|/\*.*;.*\*/"))
        issues.Add(new { severity = "info", message = "注释里含分号, 旧 SplitSqlStatements 会误切(新版已剥注释, 安全)", location = "" });
      bool passed = !issues.Any(i => ((dynamic)i).severity == "error");
      return new { passed, issues };
    }

    /// <summary>
    /// verify_metadata: 校验 ORM 元数据 SQL/JSON 配置。
    /// 检查项:
    ///   error: ORDERBY 带表别名(A.x); tss_ 表 SQL 含 ISDELETED 列; F00 非 A.ID=@ID; FIELDTYPE 用 .NET 类型。
    ///   warn: 字段名含下划线(大写); RESOURCEANAME 非 A(视图)。
    /// </summary>
    private static object VerifyMetadata(string content, string kind)
    {
      var issues = new List<object>();
      if (string.IsNullOrEmpty(content))
      {
        return new { passed = false, issues = new[] { new { severity = "error", message = "content 为空", location = "" } } };
      }
      string upper = content.ToUpper();
      // ORDERBY 带表别名
      var orderByMatches = System.Text.RegularExpressions.Regex.Matches(content, @"ORDERBY\s*=\s*'?A\.[A-Z]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
      foreach (System.Text.RegularExpressions.Match m in orderByMatches)
        issues.Add(new { severity = "error", message = "ORDERBY 带表别名(A.x), BuildSQL 包子查询后排序会报 Unknown column. 改为无别名: SORTNO, PAGECODE", location = m.Value });
      // tss_ 表 SQL 含 ISDELETED 列
      if (upper.Contains("TSS_RESOURCE") || upper.Contains("TSS_RESFIELD") || upper.Contains("TSS_MOUDLE") || upper.Contains("TSS_FUNC"))
      {
        if (upper.Contains("ISDELETED"))
          issues.Add(new { severity = "error", message = "tss_ 系统元数据表无 ISDELETED 字段, INSERT/UPDATE 不能带此列", location = "ISDELETED" });
      }
      // F00 过滤器非 A.ID=@ID
      if (upper.Contains("'F00'") || upper.Contains("\"F00\""))
      {
        if (!System.Text.RegularExpressions.Regex.IsMatch(content, @"F00['""].*A\.ID\s*=\s*@ID", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
          issues.Add(new { severity = "error", message = "F00 过滤器必须是 'A.ID=@ID'(doOpen 走 FilterParams.ID). 按业务字段过滤会导致表单打开空白", location = "F00" });
      }
      // FIELDTYPE 用 .NET 类型
      var dotnetTypes = new[] { "FIELDTYPE", "VARCHAR", "INT", "DATETIME", "TEXT", "DECIMAL" };
      if (upper.Contains("FIELDTYPE"))
      {
        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"FIELDTYPE[^,]*'(String|Int32|Int64|DateTime|Boolean|Decimal)'"))
          issues.Add(new { severity = "error", message = "FIELDTYPE 必须用 MySQL 原生类型(varchar/int/datetime/text), 不能用 .NET 类型(String/Int32 等)", location = "FIELDTYPE" });
      }
      // 字段名大写下划线
      var badFieldMatches = System.Text.RegularExpressions.Regex.Matches(content, @"FIELDNAME\s*[,=]\s*'([A-Z]+_[A-Z]+(?:_[A-Z]+)+)'");
      var badFields = badFieldMatches.Cast<System.Text.RegularExpressions.Match>()
                                     .Select(m => m.Groups[1].Value).Distinct().ToList();
      foreach (var f in badFields)
        issues.Add(new { severity = "warn", message = "字段名含下划线(" + f + "), 项目规范应大写无下划线", location = f });
      // RESOURCEANAME 非 A(视图场景)
      if (upper.Contains("DATAVIEW") && System.Text.RegularExpressions.Regex.IsMatch(content, @"RESOURCEANAME\s*[,=]\s*'[^A]'"))
        issues.Add(new { severity = "warn", message = "DATAVIEW 视图 RESOURCEANAME 必须为 A(BuildQuery 用 A 作主表别名)", location = "RESOURCEANAME" });
      bool passed = !issues.Any(i => ((dynamic)i).severity == "error");
      return new { passed, issues };
    }

    /// <summary>VerifySfc 白名单: 部分大写下划线标识符是允许的(SQL 操作码 LIKE_REGEXP 等)</summary>
    private static bool IsAllowedUnderScoreName(string name)
    {
      var allowList = new[] { "SQL_TEMPLATE", "API_CODE", "LEFT_JOIN", "PRIMARY_KEY" };
      return allowList.Contains(name);
    }

    public class MoudleRow
    {
      public string MODULECODE;
      public string MODULENAME;
      public string REMARK;
    }

    public class ApiRow
    {
      public string APICODE;
      public string APITYPE;
      public string APINAME;
      public string FILTERCODE;
      public string PATHNAME;
    }

    public class FilterRow
    {
      public string FILTERSQL;
    }

    public class FieldRow
    {
      public string FIELDNAME;
      public string LABEL;
      public string EDITTYPE;
      public string SELECTDATA;
      public string UPDATEFIELDS;
      public string REFRESOURCEID;
      public string REFRESOURCEANAME;
      public string REFRELATION;
      public string REFFIELDID;
      // resfield 表字段（read_table_schema 用）
      public string FIELDANAME;
      public string FIELDTYPE;
      public int? NULLABLE;
      public int? FIELDLENGTH;
      public int? ISKEY;
      public string KEYGENTYPE;
      public string DEFAULTVALUE;
      public string UPFIELDID;
      public int? ENTRYNUM;
    }

    public class RefResourceRow
    {
      public string ID;
      public string TABLENAME;
    }

    // 资源行（search_existing_resource / read_table_schema 用）
    public class ResourceRow
    {
      public string ID;
      public string RESOURCENAME;
      public string RESOURCEANAME;
      public string TABLENAME;
      public string RESOURCETYPE;
      public string TABLERESOURCEID;
    }

    // SHOW COLUMNS FROM 返回行（read_table_schema 用）
    public class ColumnRow
    {
      public string Field;
      public string Type;
      public string Null;
      public string Key;
      public string Default;
      public string Extra;
    }
  }
}
