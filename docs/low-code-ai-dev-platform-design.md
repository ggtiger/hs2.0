# 低代码开发平台与 AI 开发中心 — 总体设计文档

> 本文档基于对 hs2.0 仓库实际代码（netcore 后端 / p-admin 前端 / sql 迁移脚本）的全量梳理生成，覆盖：**低代码开发体系、在线开发（代码 IDE）、SFC 在线开发、模块开发中心（RS_M28）、AI 开发中心（AI 开发助理 + AI 配置中心）** 的架构设计与全部表结构设计。
>
> - 后端主线：`netcore/Realso.WebAPI`（ASP.NET Core 2.2 + Dapper + MySQL + SignalR）
> - 前端主线：`p-admin`（Vue 2.5 + Vuex 3 + HeyUI 1.25 + CodeMirror 5）
> - 数据库：MySQL 5.7+（新表 InnoDB/utf8mb4，遗留系统表 MyISAM/utf8）
> - 文档生成日期：2026-07-24；依据 `sql/aidev/` 01–54 号迁移脚本的最终形态
> - **2026-07-25 增量更新**：新增第十章——元数据表单/查询面板组件（rs-meta-*）、字段/列覆盖（overrides）机制、字典子集端到端能力、SFC 扩展模板规范、存量业务模块迁移 GenericModule 计划

---

## 目录

- [一、概述](#一概述)
- [二、总体架构](#二总体架构)
- [三、数据库设计（表结构全集）](#三数据库设计表结构全集)
- [四、后端设计](#四后端设计)
- [五、前端设计](#五前端设计)
- [六、核心工作流](#六核心工作流)
- [七、设计规约与铁律](#七设计规约与铁律)
- [八、演进历程（迁移脚本清单）](#八演进历程迁移脚本清单)
- [九、附录](#九附录)
- [十、增量补充（2026-07-25）](#十增量补充2026-07-25)

---

## 一、概述

### 1.1 平台定位

本平台是一个**元数据驱动的低代码开发平台**，并在此基础上构建了 **AI 开发中心**。其核心理念：

- **模块 = 运行时配置包**：一个业务模块 = 物理表 + 资源/字段/过滤器/UI 元数据 + 模块数据源/接口定义 + 页面/按钮配置 + 菜单/权限，全部存于数据库，运行时由统一引擎解释执行。
- **配置能搞定的不写脚本，脚本能搞定的不写 SFC，SFC 是最后手段**：三级低代码体系——Level 1 纯配置（约 80% 页面）、Level 2 配置 + 脚本注入（约 15%）、Level 3 纯 SFC（约 5%）。
- **平台自举（dogfooding）**：平台自身的管理界面（资源管理、模块配置、AI 配置等）也全部由这套元数据驱动渲染。
- **AI 不直接写库**：AI 调用开发工具产出 DRAFT 变更项 → 用户逐项确认 → 汇总导出幂等升级脚本 → 生产环境导入执行，全程可校验、可回滚、可追溯。

### 1.2 范围说明

| 主题 | 对应系统/模块 | 本文档章节 |
|---|---|---|
| 低代码开发（元数据体系、通用模块引擎） | RS_M00~M06、tss_* 元数据表、GenericModule | 三、四、五 |
| 在线开发（代码在线开发 IDE） | RS_M17（s01/m17）、tss_code_asset | 三、五 |
| SFC 在线开发 | sfc-loader、sfc-editor、RS_M17、tss_module_page.COMPONENTTYPE=sfc | 四、五 |
| 模块开发中心 | RS_M28（s01/m28） | 三、五 |
| AI 开发中心 | RS_MAIDEV / RS_MAIDEVUPG（AI 开发助理/升级管理）、RS_M27（AI 配置中心）、Agent 引擎 | 三、四、五 |

### 1.3 术语与编号约定

- **模块编码**：`RS_MXX`（系统管理域，如 RS_M17 代码在线开发）、`RS_MAIDEV`（AI 开发助理）；业务域另有 `LI_MXX`、`LIB_MXX` 等。
- **前端页面目录**：`p-admin/src/pages/s01/mXX` 对应系统管理域定制页面（s01=系统管理业务域）；通用模板模块走约定路由 `/g/{MODULECODE}/{PAGECODE}`。
- **资源命名**：物理表资源 `TBS_XXX` / `TCK_XXX` / `TSS_XXX`；数据视图资源名为物理表名首字母 T→V（`TBS_XXX`→`VBS_XXX`）。
- **接口编码 APICODE**：模块内接口统一编号——A01 查询 / A02 打开 / A03 高级查询 / A04 保存 / A07 删除 / A05+ 自定义；**APICODE 是能力，按钮是入口，二者多对多**。
- **过滤器 FILTERCODE**：F00=按 ID 单条、F01=列表、F02=高级查询、F03+=主子关联等专用。
- **菜单与权限**：`tss_func.FUNCCODE` 必须等于 `MODULECODE`；权限点 key = `FUNCCODE/FUNCPOINTCODE`（如 `RS_M28/A01`），按钮 `PERMCODE={MODULECODE}/{APICODE}` 与之匹配才显示。

---

## 二、总体架构

### 2.1 技术栈

| 层 | 技术 |
|---|---|
| 前端 | Vue 2.5 + Vuex 3 + vue-router 3 + HeyUI 1.25（webpack 构建）；CodeMirror 5 代码编辑器；浏览器端 `vue-template-compiler` + `@babel/standalone` + `less` 在线编译 SFC |
| AI 传输 | SignalR（`@aspnet/signalr`，assistant/form/optimize 场景）+ 原生 fetch SSE（aidev/wizard/sfc 场景） |
| 后端 | ASP.NET Core 2.2 MVC + SignalR + Dapper + MySQL；Roslyn（C# 脚本运行时编译）；NVelocity（SQL 模板） |
| LLM | OpenAI 兼容 `/chat/completions` 流式接口（DeepSeek 等供应商可配） |
| 数据库 | MySQL 5.7+ |
| 并行实现 | `hs2-java`（Java + magic-api 移植版，单快照、略滞后于 netcore，见 §4.8） |

### 2.2 后端分层

```
前端 (p-admin / hs-mobile)
  │  HTTP(form) / SSE / SignalR
  ▼
Realso.WebAPI            唯一 API 宿主：Controllers + Hubs + Services + Models
  ├─ Realso.Core           BaseControl(userInfo/responseModel)、BaseModel、ResponseModel
  ├─ Realso.Data.ORM       元数据驱动 ORM：MOUDLE、ViewOperate01、SQLManage(NVelocity)、SchemaManage
  ├─ Realso.Data.ORM.Core  DataView/ViewRow/ViewColumn/Resource 元数据对象模型
  ├─ Realso.Data.DBAccess  DB 静态工厂 + DBHelper（Dapper 封装）
  └─ Realso.Utils          Logger、SqlScriptHelper、VelocityHelper、AesHelper、Word/Excel
Realso.Auth              独立认证服务（JWT Bearer）
```

**核心机制：元数据驱动统一入口**。`DataController` 暴露 `POST /api/{controller}/call/{modulename}/{apicode}`，加载 `tss_moudle` / `tss_moudleapi` 元数据后按 `APITYPE` 分发：query / open / save / delete / submit / check / verify / batch* / sql / csharp / script；未知类型落 `doMyApi` 虚方法由子类控制器按 APICODE 二次分发（`netcore/Realso.WebAPI/Controllers/DataController.cs:144`）。

### 2.3 六层低代码体系

```
L1  MySQL 物理表（tbs_/tck_/tss_）
L2  资源层：tss_resource + tss_resfield + tss_resfilter + tss_resuipc   （s01/m01 资源管理）
L3  模块层：tss_moudle + tss_moudlepath(+rel) + tss_moudleapi + tss_sql （s01/m02 模块管理、m13 SQL配置）
L4  页面层：tss_module_page + tss_module_button                         （s01/m18 模块配置）
L5  代码层：tss_code_asset（csharp/sql/js/vue 统一代码资产）             （s01/m17 代码在线开发）
L6  运行层：GenericModule 通用引擎 / 在线 SFC 动态路由 / 定制页面
菜单权限层：tss_func + tss_funcpoint 横切（s01/m03 功能管理、m04 角色管理）
```

### 2.4 运行时页面渲染的三种路径

| 路径 | 触发条件 | 说明 |
|---|---|---|
| 定制页面 | `tss_func.OUTERURL = s01/mXX/...` | webpack 静态打包的传统 Vue 页面（如 m17 IDE） |
| 通用模块 | `OUTERURL = /g/{MODULECODE}/{PAGECODE}` 或页面 ROUTEPATH | `GenericModule` 按 MODPAGE/MODBUTTON/scm 元数据渲染，`registerGenericRoute` 动态注册路由（`p-admin/src/router/index.js:161`） |
| 在线 SFC | 页面 `COMPONENTTYPE=sfc` + `SFCMODULEPATH`，或路由含 `/online/` | 运行时从 `VSS_CODE_ASSET` 取编译产物动态挂载，`registerOnlineRoute`（`router/index.js:108`） |

### 2.5 AI 开发中心总体结构

```
┌─────────────────────────── 前端 ───────────────────────────┐
│ s01/m28 模块开发中心（聚合工作台，含 AI 侧滑面板）            │
│ s01/m17 代码在线开发 IDE（内嵌 AI 助手）                     │
│ s01/m18 模块配置（AI 模块创建向导 module-wizard）            │
│ s01/m27 AI 配置中心（场景/工具/记忆/提示词/模型/用量）        │
│ components/ai/* 消息块渲染 + utils/ai/AiClient.js 传输层     │
└────────────── SignalR / SSE / HTTP ────────────────────────┘
┌─────────────────────────── 后端 ───────────────────────────┐
│ AssistantHub（SignalR：Ask/AskForm/OptimizePrompt/前端工具） │
│ RMAIDevController（SSE：generate-stream / 向导 3 端点）      │
│ RMSfcAiController（SSE：generate-code + schema + 元数据SQL） │
│ ─ Agent 引擎层：AgentEngine ReAct 循环（Dev/Assistant 子类） │
│ ─ 工具层：内置工具(C#) / 声明式SQL工具(tss_ai_tool) / 前端工具│
│ ─ 编排层：AiDevOrchestrator → ChangeSetEngine（变更包管道）  │
│ ─ 配置层：SceneConfigService / PromptService / MemoryService │
│ ─ 保障层：ChangeSetValidator / UpgradeExecutor / 版本捕获    │
└────────────────────────────────────────────────────────────┘
```

---

## 三、数据库设计（表结构全集）

### 3.0 全局约定

- **命名规范**：物理表前缀 `tbs_`（基础/业务）、`tck_`（流程/记录）、`tss_`（系统/元数据）；字段名**全大写无下划线连写**（`CREATETIME` 而非 `CREATED_TIME`）。
- **审计字段六件套**（新表标准）：`CREATEID varchar(64)`、`CREATER varchar(16)`、`CREATETIME datetime`、`MODIFYID varchar(64)`、`MODIFER varchar(16)`、`MODIFYTIME datetime`，另加 `ISDELETED tinyint default 0` 逻辑删除。后端 `doSave` 按字段名自动填充。
- **主键**：`varchar(36/64)`，ISKEY=1 + KEYGENTYPE=`GUID` 时 FillKey 自动填 32 位无连字符 GUID。
- **ORM 自描述**：所有物理表/视图注册在 `tss_resource`，字段注册在 `tss_resfield`，过滤器在 `tss_resfilter`，UI 在 `tss_resuipc`，模块/数据源/接口在 `tss_moudle` 系列——平台功能全部"自举"。
- **存储引擎现状**：新表统一 InnoDB + utf8mb4 + utf8mb4_general_ci；遗留系统表（`tss_resource` 等 13 张）为 MyISAM + utf8 且有 latin1 列。
- **索引命名**：`uk_xxx` 唯一索引、`idx_xxx` 普通索引；迁移脚本统一用 information_schema 守卫保证幂等（30_release.sql 除外，见 §9.3）。

### 3.1 低代码核心元数据表

#### 3.1.1 tss_resource — 资源表（ORM 资源注册中心）

每张物理表（TABLE）和数据视图（DATAVIEW）一行。

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 资源 ID（惯例小写，如 `tbs_code_asset_001`） |
| RESOURCENAME | varchar(32) | | 资源名（`TBS_XXX`/`VSS_XXX`/`VCK_XXX`） |
| RESOURCEANAME | varchar(64) | | 资源别名（SQL 中的表别名，惯用 `A`） |
| TABLERESOURCEID | varchar(64) | →tss_resource.ID | DATAVIEW 指向其 TABLE 资源 |
| TABLENAME | varchar(32) | | 物理表名 |
| RESOURCETYPE | varchar(16) | | TABLE / DATAVIEW / SQL |
| SQLCODE | varchar(16) | →tss_sql.SQLCODE | RESOURCETYPE=SQL 时的 SQL 模板编码 |
| ISFORBID | int | | 禁止编辑否 |
| ISCREATE | int | | 建表否 |
| COMMENTS | varchar(32) | | 说明 |

#### 3.1.2 tss_resfield — 资源字段表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键（惯例 `rf_xxx`） |
| RESOURCEID | varchar(64) | →tss_resource.ID | 所属资源 |
| REFFIELDID | varchar(64) | →tss_resfield.ID | DATAVIEW 字段指向物理表字段（TBS 字段为 NULL） |
| FIELDNAME | varchar(32) | | 字段名（大写无下划线） |
| FIELDANAME | varchar(32) | | 字段别名（中文名） |
| FIELDTYPE | varchar(16) | | varchar/int/text/datetime/decimal… |
| PREC | int | | 精度 |
| NULLABLE | int | | 1 可空 / 0 必填 |
| FIELDLENGTH | int | | 长度 |
| REFRESOURCEID | varchar(64) | →tss_resource.ID | 引用资源（人员字段指向 TBS_EMP） |
| REFRESOURCEANAME | varchar(8) | | 引用资源别名（如 `B`） |
| REFRELATION | varchar(128) | | 引用关系（`A.CREATEID=B.ID`） |
| UPFIELDID | varchar(64) | →tss_resfield.ID | 引用名称字段回指本地 ID 字段（虚拟列机制） |
| VFORMAT | varchar(128) | | 格式化模板（单据号 `ORD{yyyy}` 等，配合 PSS_GENCODE） |
| ISVIRTUAL | int | | 虚拟字段否 |
| ISVO / ISDO | int | | 视图对象否 / 数据存储对象否 |
| DEFAULTVALUE | varchar(32) | | 默认值 |
| ISKEY | int | | 主键标记 |
| KEYGENTYPE | varchar(8) | | 主键生成方式（`GUID`） |
| INDEXGROUP | varchar(32) | | 索引组 |
| ENTRYNUM | int | | 分录号（决定 SELECT/INSERT 字段顺序） |
| EDITTYPE | varchar(16) | | 编辑方式 |
| COMMENTS | varchar(32) | | 说明 |

#### 3.1.3 tss_resfilter — 资源过滤器表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| RESOURCEID | varchar(64) | NOT NULL，→tss_resource.ID | 所属资源 |
| FILTERCODE | varchar(8) | NOT NULL | F00 单条 / F01 列表 / F02 高级查询 / F03+ 专用 |
| FILTERSQL | text | NOT NULL | NVelocity 模板（`#if("$!{X}"!="")`，**禁单引号**，LIKE 用 `CONCAT(CHAR(37),@X,CHAR(37))`；特殊值 `@ui`/`@ui:adv` 按 resuipc 查询配置自动生成） |
| ORDERBY | varchar(64) | | 排序（不允许带表别名前缀） |
| REMARK | varchar(128) | | 说明 |

#### 3.1.4 tss_resuipc — 资源页面描述表（UI 配置）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| RESOURCEID | varchar(64) | →tss_resource.ID | 所属资源 |
| RESFIELDID | varchar(64) | →tss_resfield.ID | 资源字段 |
| FIELDNAME | varchar(32) | | UI 层数据 key，**必须与 resfield.FIELDNAME 一致** |
| LABELNAME | varchar(32) | | 列头/表单 label |
| EDITTYPE | varchar(16) | | text/textarea/select/checkbox/number/datepicker/code/index/action… |
| MAXLENGTH | int | | 长度 |
| SHOWLENGTH | varchar(32) | | 显示长度 |
| LISTSORT / QUERYSORT / EDITSORT | int | | 列表/查询/编辑排序（NULL=不显示） |
| EDITGROUP | varchar(16) | | 编辑组（表单按组分 Tab） |
| QUERYTYPE | varchar(16) | | 查询控件类型（input/daterange…） |
| QUERYMODE | varchar(20) | | 查询匹配方式 like/eq/in/range，NULL 按 EDITTYPE 推导（06 号迁移新增） |
| COLSPAN | tinyint | 默认 1 | 字段占宽：1=按列宽，2=独占整行（16 号迁移新增） |
| PLACEHOLDER | varchar(16) | | 空显示 |
| NULLABLE / EDITABLE | int | | 可空否 / 禁编辑否 |
| SELECTDATA | varchar(128) | | 下拉数据：**写字典名 DICTNAME**（如"费用类型"）或 k:v 内联 |
| ACTIONCODE | varchar(128) | | 事件码 |
| UPDATEFIELDS | varchar(512) | | 更新字段 |
| ENTRYNUM | int | | 分录号 |

#### 3.1.5 tss_moudle — 模块表（表名为历史拼写 moudle）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| MODULECODE | varchar(16) | | 模块编码（RS_M17、RS_MAIDEV…，逻辑唯一） |
| MODULENAME | varchar(32) | | 模块名称 |
| FLOWCODE | varchar(13) | | 流程编码（审批流；决定默认流程按钮） |
| REMARK | varchar(128) | | 说明 |

#### 3.1.6 tss_moudlepath — 模块数据源表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| MODULEID | varchar(64) | →tss_moudle.ID | 模块 ID |
| PATHNAME | varchar(16) | | 数据源名：QRY 查询 / QQRY 高级查询 / SEL 选择器 / MAIN 主表 / DTSA 等子表 / MODPAGE / MODBUTTON… |
| RESOURCEID | varchar(64) | →tss_resource.ID | 数据源资源 |
| ENTRYNUM | int | | 分录号 |
| REMARK | varchar(64) | | 说明 |

#### 3.1.7 tss_moudlepathrel — 模块数据源关系表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| MODULEID | varchar(64) | →tss_moudle.ID | 模块 ID |
| PATHNAMEA / PATHNAMEB | varchar(16) | | 主/子数据源名 |
| RFIELDSA / RFIELDSB | varchar(32) | | 关系字段（如 `MAIN.MODULECODE=MODPAGE.MODULECODE`） |
| ENTRYNUM / REMARK | — | | 分录号 / 说明 |

#### 3.1.8 tss_moudleapi — 模块接口定义表（核心）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| MODULEID | varchar(64) | →tss_moudle.ID | 模块 ID |
| APICODE | varchar(16) | | 接口编码（模块内唯一） |
| ACTIONCODE | varchar(32) | | 事件码（query/open/advQuery/save/delete/submit/check/verify…；前端按它识别接口用途，**必填**；自定义接口置 NULL 走 doMyApi） |
| APINAME | varchar(64) | | 接口名称 |
| APITYPE | varchar(32) | | query/open/save/delete/submit/check/verify/sql/csharp/script；NULL=自定义 |
| PATHNAME | varchar(16) | →tss_moudlepath.PATHNAME | 数据源名 |
| RESOURCEID | varchar(64) | →tss_resource.ID | 资源 ID（直指定时使用） |
| FILTERCODE | varchar(16) | →tss_resfilter.FILTERCODE | 过滤器编码 |
| SQLID | varchar(16) | →tss_sql.SQLCODE | APITYPE=sql 时的 SQL 模板编码 |
| SCRIPTCODE | varchar(64) | →tss_code_asset.CODE | APITYPE=csharp 时的脚本编码（11 号迁移新增） |
| APIPARAM | **TEXT** | | 接口参数：query/advQuery=`QQRY`，save/delete=`MAIN` 或 `MAIN,DTSA`；APITYPE=script 时存编排步骤 JSON（29 号迁移由 varchar(128) 扩为 TEXT） |
| BEFOREAPICODE / AFTERAPICODE | varchar(64) | | 前置/后置接口编码 |
| BEFOREAPIID / AFTERAPIID | varchar(64/255) | | 前置/后置接口 ID |
| FILEID | varchar(64) | | 文件 ID |
| ENTRYNUM / REMARK | — | | 分录号 / 说明 |

#### 3.1.9 tss_func — 功能（菜单）表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| UPFUNCID | varchar(64) | →tss_func.ID | 上级菜单 |
| FUNCTYPE | int(1) | | 1 目录 / 2 菜单 |
| FUNCCODE | varchar(32) | | **必须 = MODULECODE**（权限 key 前缀） |
| FUNCNAME | varchar(32) | | 功能名称 |
| FUNCICON | varchar(16) | | 图标 |
| ISOUTERURL | int(1) | | 外部 URL 否 |
| OUTERURL | varchar(128) | | 路由（`/g/{MODULECODE}/{PAGECODE}` 走通用模块；`s01/mXX` 走定制页） |
| ISHIDE | int(1) | 默认 0 | 隐藏否（49 号迁移用它归并旧 AI 菜单） |
| ISUSE | int(1) | 默认 0 | 使用否 |
| LEVEL / NOSUB | int(1) | | 层级 / 无下级 |
| RULECODE | varchar(256) | | 规则码 |
| SORTCODE | int | 默认 0 | 排序码（菜单排序=FUNCCODE+SORTCODE） |
| REMARK | varchar(256) | | 备注 |

#### 3.1.10 tss_funcpoint — 功能点（权限点）表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(64) | PK | 主键 |
| FUNCID | varchar(64) | →tss_func.ID | 所属菜单 |
| FUNCPOINTCODE | varchar(32) | | 权限点编码（**只写纯 APICODE**：A01/A04/A07…） |
| FUNCPOINTNAME | varchar(32) | | 名称 |
| APICODE | varchar(16) | ↔tss_moudleapi.APICODE | 关联接口编码 |
| ENTRYNUM | int | | 分录号 |

#### 3.1.11 tss_dict / tss_dictitem — 字典表

- **tss_dict**：ID varchar(64) PK；DICTCODE varchar(16)（D0604…D0710）；DICTNAME varchar(32)（resuipc.SELECTDATA 引用它）；ISUSE int(1)；REMARK varchar(255)（32 号迁移扩列）。
- **tss_dictitem**：ID PK；DICTID→tss_dict.ID；ITEMNAME varchar(255) 显示名；ITEMVALUE varchar(255) 存储值；REMARK；ENTRYNUM。
- 已建字典：D0604 会话状态、D0605 变更项状态、D0606 升级状态、D0607 会话类型、D0608 变更项类别、D0609 变更项操作、D0610 快照对象类型；D0701 版本对象类型、D0702 版本操作类型、D0703 AI 场景传输方式、D0704 AI 工具集、D0705 上下文源、D0706 工具执行类型、D0707 业务分类、D0708 字段占宽。

#### 3.1.12 tss_sql — SQL 模板表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| SQLID | varchar(64) | PK | 主键 |
| SQLCODE | varchar(64) | | SQL 编码（SS_XXX，被 moudleapi.SQLID / tss_ai_tool.SQLCODE 引用） |
| SQLTYPE | varchar(16) | | SQL 类型（mysql） |
| SQLTXT | text | | NVelocity 模板（禁单引号） |
| REMARK | varchar(128) | | 备注 |

重要模板：`SS_MOD_CODEFILES`（按模块查关联 C#/SQL/JS/VUE 资产）、`SS_AI_MODPAGES`、`SS_AI_TOOLLIST`、`SS_AI_MODTPL`、`SS_AI_MEM_SEARCH`。

#### 3.1.13 tss_module_page — 模块页面配置表（低代码页面体系核心）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键（惯例 `mp_{module}_{page}`） |
| MODULECODE | varchar(50) | NOT NULL，↔tss_moudle.MODULECODE | 所属模块 |
| PAGECODE | varchar(50) | NOT NULL | 页面编码（模块内唯一，如 main/form/add） |
| PAGENAME | varchar(100) | | 页面名称 |
| PAGETYPE | varchar(20) | NOT NULL | list / form / review / report / select |
| PARENTID | varchar(36) | →本表.ID | 上级页面（**form 页必须指向 list 页**） |
| ROUTEPATH | varchar(100) | | 路由路径（弹窗方式可空） |
| COMPONENTTYPE | varchar(20) | 默认 'standard' | **只能 standard / sfc**（路由白名单）；sfc 需配 SFCMODULEPATH |
| SFCMODULEPATH | varchar(100) | ↔tss_code_asset.MODULEPATH | SFC 组件路径 |
| QUERYAPICODE / ADVQUERYAPICODE / OPENAPICODE / SAVEAPICODE | varchar(20) | ↔tss_moudleapi.APICODE | 列表查询/高级查询/打开/保存接口（后补列） |
| PAGECONFIG | varchar(2000) | | 页面配置 JSON（`defaultFormPageCode`/`EXTENDJS`/`SLOTS`/`FORMLAYOUT`/`REPORT` 等扩展点，后补列） |
| SORTNO | int | 默认 0 | 排序号 |
| ISDELETED | tinyint | 默认 0 | 逻辑删除 |

#### 3.1.14 tss_module_button — 页面按钮配置表

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键（惯例 `mb_{module}_{page}_{seq}`） |
| PAGEID | varchar(36) | NOT NULL，→tss_module_page.ID | 所属页面 |
| MODULECODE | varchar(50) | | 模块编码（冗余） |
| APICODE | varchar(20) | NOT NULL，↔tss_moudleapi.APICODE | 关联接口 |
| BTNNAME | varchar(50) | | 按钮显示名 |
| BTNTYPE | varchar(20) | 默认 'custom' | crud / flow / custom |
| BTNCODE | varchar(30) | | add/edit/save/delete/submit/check/verify/export/custom（**行为由 BTNCODE+EXTPARAM 决定**） |
| BTNAREA | varchar(20) | 默认 'footer' | header / footer / row / 子表路径 |
| INTERACTTYPE | varchar(20) | 默认 'direct' | **只允许 direct / poptip**（后端强校验） |
| POPTIPTEXT | varchar(100) | | poptip 确认提示文字 |
| SHOWCOND | varchar(200) | | 显隐条件表达式（如 `row.ID && state.__mode__ !== 'add'`） |
| PERMCODE | varchar(100) | | 权限编码（`{MODULECODE}/{APICODE}`） |
| ICON / COLOR | varchar(50/20) | | 图标 / 颜色 |
| SORTNO | int | | 排序 |
| EXTPARAM | varchar(500) | | 扩展参数 JSON（如 `{"action":"openForm","openMode":"add","formPageCode":"add"}`） |
| BTNCONFIG | varchar(2000) | | 按钮扩展配置 JSON |
| ISDELETED | tinyint | 默认 0 | 逻辑删除 |

### 3.2 代码资产与 SFC 表

#### 3.2.1 tss_code_asset — 统一代码资产表（22 号迁移创建）

四类代码资产合一：**csharp**（C# 脚本）/ **sql**（SQL 模板）/ **js**（JS 模块）/ **vue**（SFC 组件）。历史数据自 `tss_api_script`、`tss_sql`、`tbs_sfc_template` 迁移并保留原 ID；老表保留作历史归档。

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键（迁移保留原表 ID） |
| ASSETTYPE | varchar(16) | NOT NULL | 资产类型 csharp/sql/js/vue |
| CODE | varchar(200) | NOT NULL | 编码（SCRIPTCODE/SQLCODE/TEMPLATECODE 统一） |
| NAME | varchar(200) | | 名称 |
| MODULEPATH | varchar(200) | | 路径（SFC `@/modules/...`；csharp/sql 为 `@/scripts/{模块}/{编码}.{cs/sql}`） |
| FILETYPE | varchar(16) | | JS / VUE（仅 js/vue） |
| SOURCECODE | longtext | | 源码 |
| COMPILEDCODE | longtext | | SFC 编译产物（仅 js/vue） |
| DEPS | varchar(2000) | | SFC 依赖（仅 js/vue） |
| SQLTYPE | varchar(16) | | SQL 类型（仅 sql） |
| VERSION | int | 默认 1 | 版本号（展示用；热更新按源码哈希检测） |
| REMARK | varchar(500) | | 备注 |
| CREATEID / CREATER / MODIFYID / MODIFER | varchar(64) | | 人员四列 |
| CREATETIME / MODIFYTIME | datetime | | 创建/修改时间 |
| ISDELETED | tinyint | 默认 0 | 逻辑删除 |
| LIVEPATH | varchar(200) | 生成列 VIRTUAL | `IF(ISDELETED=0,MODULEPATH,NULL)`（26 号迁移新增） |

索引：`uk_livepath(LIVEPATH)` 唯一（逻辑删除行自动让出路径，回滚写回 ISDELETED=0 即恢复）；`idx_type_code(ASSETTYPE, CODE)`。

关联：`tss_moudleapi.SCRIPTCODE`/`SQLID` → CODE；`tss_module_page.SFCMODULEPATH`、`PAGECONFIG.EXTENDJS`/`SLOTS` → MODULEPATH；版本纳管视图 `VSS_CODE_ASSET`。

#### 3.2.2 tbs_sfc_template — SFC 在线模板（历史表）

ID PK；TEMPLATECODE varchar(100) 唯一；TEMPLATENAME；MODULEPATH varchar(500)（如 `@/pages/r02/m07/views/main.vue`）；FILETYPE（VUE/JS）；SOURCECODE/COMPILEDCODE longtext；DEPS text（JSON 数组）；DESCRIPTION；ISDELETED；CREATEDBY/CREATEDTIME/UPDATEDBY/UPDATEDTIME（旧式审计列）。新使用一律走 `VSS_CODE_ASSET`。

#### 3.2.3 tss_api_script — API C# 脚本（历史表，11 号迁移创建）

ID PK；SCRIPTCODE varchar(64) 唯一；SCRIPTNAME；SOURCECODE longtext（迁移里以 0x HEX 写入）；VERSION；REMARK；ISDELETED。种子脚本：SC_SCRIPT_CHECK（语法检查）、SC_SAMPLE_DELETE、SC_TEST_AI_TOOL、SC_M18_LINK_API / SC_M18_UNLINK_API / SC_M18_SAVE_SCRIPTFLOW / SC_M18_CREATE_SCRIPTFLOW。

### 3.3 AI 开发助理表（01 号迁移建 6 表）

模块：RS_MAIDEV（AI 开发助理）/ RS_MAIDEVUPG（升级管理）。

#### 3.3.1 tss_aidev_session — AI 开发会话

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| SESSIONCODE | varchar(50) | NOT NULL，唯一 uk_aisess_code | 会话编码 |
| SESSIONNAME | varchar(200) | NOT NULL | 会话名称 |
| SESSIONTYPE | varchar(16) | | NEW / MODIFY |
| TARGETMODULE | varchar(64) | ↔tss_moudle.MODULECODE | 目标模块编码 |
| INTENT | text | | 开发意图描述 |
| STATUS | varchar(16) | 默认 'DRAFT' | DRAFT/GENERATING/REVIEWING/EXPORTED/ARCHIVED |
| CREATEDBY | varchar(36) | | 创建人 ID |
| CREATEDTIME | datetime | | 创建时间 |
| CLOSEDATE | datetime | | 关闭日期 |
| CHANGESETID | varchar(36) | →tss_aidev_changeset.ID | 关联变更包 |
| CONVERSATION | — | | 对话历史 JSON（代码侧维护） |
| REMARK | varchar(500) | | 备注 |
| ISDELETED | tinyint | 默认 0 | |

#### 3.3.2 tss_aidev_changeset — 变更包

ID PK；SESSIONID varchar(36) NOT NULL →session（idx_aics_session）；CHANGESETCODE varchar(50) 唯一 uk_aics_code；TITLE varchar(200)；SOURCE varchar(16)（NEW/MODIFY/ai）；INTENT text；VALIDATIONPASSED tinyint 默认 0；VALIDATIONREPORT text；ITEMCOUNT int 默认 0；CREATEDTIME；ISDELETED。

#### 3.3.3 tss_aidev_changeitem — 变更项

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| CHANGESETID | varchar(36) | NOT NULL →changeset | 所属变更包 |
| ITEMSEQ | int | | 项序号 |
| CATEGORY | varchar(32) | | 类别：physical_table/dataview/field/ui/dict/filter/module/api/menu/permission/billflow/page/button/sfc… |
| ACTION | varchar(16) | | create / alter / update / delete |
| TOOL | varchar(64) | | 生成工具 |
| TARGET | varchar(128) | | 目标对象 |
| SQLCONTENT | longtext | | SQL 内容 |
| METADATA | text | | 元数据 JSON（校验与版本捕获的依据） |
| RATIONALE | text | | 设计理由（人类可读） |
| WARNINGS | text | | 警告 |
| DEPENDSON | varchar(500) | | 依赖项 ID 逗号分隔（自关联） |
| ITEMSTATUS | varchar(16) | 默认 'DRAFT' | DRAFT/CONFIRMED/EXECUTED/REJECTED/MERGED |
| CONFIRMEDBY / CONFIRMEDTIME / CONFIRMORDER | — | | 确认人/时间/顺序 |
| ISDELETED | tinyint | 默认 0 | |

#### 3.3.4 tss_aidev_upgrade — 升级记录

ID PK；UPGRADECODE varchar(50) 唯一 uk_aiupg_code；SESSIONCODE/SESSIONNAME/SESSIONTYPE/TARGETMODULE/INTENT 冗余自会话；SCRIPTCONTENT longtext（.aidev.sql 脚本）；SCRIPTHASH varchar(64)（SHA256 防篡改）；ITEMCOUNT int；STATUS varchar(16) 默认 'PENDING'（PENDING/RUNNING/SUCCESS/FAILED/ROLLEDBACK）；EXECUTEDBY/EXECUTEDTIME；DURATIONMS int；ERRORMSG text；ROLLBACKSCRIPT longtext；ISDELETED。

#### 3.3.5 tss_aidev_upgrade_log — 升级日志

ID PK；UPGRADEID→upgrade；ITEMID→changeitem；ITEMCATEGORY/ITEMACTION/ITEMTARGET 冗余；SQLSNIPPET text；STATUS；ERRORMSG；ROWSAFFECTED int；EXECUTEDTIME。纯日志，无 ISDELETED。

#### 3.3.6 tss_aidev_upgrade_snapshot — 升级快照（回滚依据）

ID PK；UPGRADEID→upgrade；OBJECTTYPE varchar(32)（TABLE/RESOURCE/RESFIELD/FUNC）；OBJECTNAME varchar(128)；SNAPSHOTBEFORE/SNAPSHOTAFTER longtext（变更前后 JSON）。

### 3.4 版本与发布表

#### 3.4.1 tss_dev_version — 在线开发版本快照（12 号迁移创建）

机制：`DataController._doSave/doDelete` 统一拦截，对 cfg 内资源的 DataView 自动抓前后镜像写版本行（与业务保存同事务）。

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| OBJTYPE | varchar(32) | NOT NULL | 对象类型（sfc/api_script/sql/page/button/module/resource/field/filter/ui/api/scene/aitool/template/code/dict/menu/permission/script/release/aimemory…，D0701 字典） |
| OBJID | varchar(64) | NOT NULL | 对象主键（源表.ID） |
| OBJCODE / OBJNAME | varchar(200) | | 对象编码/名称（冗余检索） |
| VERSION | int | NOT NULL | 对象内递增版本号 |
| OPTYPE | varchar(8) | NOT NULL | insert / update / delete / rollback |
| BEFORECONTENT | longtext | | 变更前快照（insert 为 NULL） |
| AFTERCONTENT | longtext | | 变更后快照（delete 为 NULL） |
| CHANGENOTE | varchar(500) | | 变更说明（提交保存时填写） |
| TAG | varchar(64) | idx_tag | 发布点标签（不被清理） |
| PINNED | tinyint | 默认 0 | 置顶保留（不被清理） |
| CREATEID / CREATER | varchar(64) | | 操作人 |
| CREATETIME | datetime | | 操作时间 |
| SRCTABLE | varchar(64) | | 来源物理表（回滚定位） |
| ISDELETED | tinyint | 默认 0 | |

索引：`uk_obj_ver(OBJTYPE, OBJID, VERSION)` 唯一；idx_code、idx_time、idx_tag。

#### 3.4.2 tss_dev_version_cfg — 版本纳管资源配置

ID PK；RESOURCENAME varchar(64) 唯一 uk_res（saveList 中 DataView 的资源名）；OBJTYPE varchar(32)；CODEEXPR/NAMEEXPR varchar(200)（OBJCODE/OBJNAME 取值字段，多字段逗号分隔用 `/` 连接）；MAXVERSIONS int 默认 50（超出清理最旧非 PINNED 无 TAG 版本）；ENABLED tinyint 默认 1；ISDELETED。

纳管范围演进：12 号首批 12 项（SFC/API 脚本/页面/按钮/模块/资源/字段/过滤器/UI/接口/SQL）；后续增补 scene/aitool/template/aimemory/dict/menu/permission/release/code（VSS_CODE_ASSET）。

#### 3.4.3 tss_release — 发布包（30 号迁移创建）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| RELEASECODE | varchar(64) | 唯一 uk_releasecode | 发布编码（`REL_20260719_01`） |
| RELEASENAME | varchar(128) | NOT NULL | 发布名称 |
| TAG | varchar(64) | idx_tag | 关联 tss_dev_version.TAG |
| OBJCOUNT | int | 默认 0 | 包含对象数 |
| STATUS | varchar(16) | 默认 'draft' | draft / published / deployed |
| SCRIPTCONTENT | longtext | | .aidev.sql 格式发布脚本（幂等 INSERT） |
| SCRIPTHASH | varchar(64) | | 脚本哈希 |
| 审计六列 + ISDELETED | — | | |

### 3.5 AI 场景 / 工具 / 记忆 / 反馈表

#### 3.5.1 tss_ai_scene — AI 场景注册表（13 号迁移创建，50 号加列）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| SCENECODE | varchar(32) | 唯一 uk_scenecode | 场景编码（assistant/form/optimize/aidev/wizard/sfc/自定义） |
| SCENENAME | varchar(64) | | 场景名称 |
| TRANSPORT | varchar(16) | NOT NULL | signalr / sse |
| ENDPOINT | varchar(128) | | SSE：完整路由；signalr：Hub 方法名（Ask/AskForm/OptimizePrompt） |
| TOOLSET | varchar(32) | | 后端工具集 assistant/formfill/dev/sfc |
| PROMPTKEY | varchar(64) | →TBS_ASSISTANT_PROMPT.PROMPTKEY | 提示词 key |
| MODELID | varchar(64) | →TBS_LLM_CONFIG.ID，NULL=全局默认 | 场景级模型路由（50 号新增） |
| PARAMS | text | | Agent 参数 JSON（maxSteps/timeoutMs/temperature…，50 号新增） |
| DAILYQUOTA | int | | 每日 Token 上限，0=不限（代码侧保障列） |
| FRONTENDTOOLS | varchar(512) | | 前端工具 all/none 或逗号分隔工具名 |
| CONTEXTSOURCE | varchar(32) | | 上下文源 none/formContext/sfcContext |
| ENABLED / SORTNO / REMARK / ISDELETED | — | | |

种子 6 场景：assistant(SignalR Ask) / form(AskForm+12 个表单工具) / optimize / aidev(SSE) / wizard(SSE 分步) / sfc(SSE)。管理模块 RS_M23。

#### 3.5.2 tss_ai_tool — 声明式 AI 工具注册表（14 号迁移创建）

ID PK；TOOLNAME varchar(64) 唯一 uk_toolname（小写下划线）；TOOLSET varchar(32)；DESCRIPTION varchar(1000)（给 LLM 看）；PARAMS text（JSON Schema）；EXECUTORTYPE varchar(16)（**sql**=声明式只读 / **csharp**=脚本 / **builtin**=内置定义覆盖 / **frontend**=前端工具清单）；SQLCODE varchar(64)→tss_sql.SQLCODE（仅 SELECT，模板原文+注参后双重校验）；MAXROWS int 默认 200（防 token 爆炸）；ENABLED/REMARK/ISDELETED。管理模块 RS_M24。

种子工具：search_module_pages、list_ai_tools、search_module_template、search_memory、recall_examples、list_pitfalls。

#### 3.5.3 tss_ai_memory — AI 统一记忆中枢（32 号迁移创建）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| MEMORYTYPE | varchar(20) | NOT NULL | rule 规则 / example 示例 / pitfall 反模式 / glossary 术语 |
| ASSETTYPE | varchar(20) | 默认 'general' | sfc/sql/csharp/metadata/wizard/frontend/general |
| TITLE | varchar(200) | NOT NULL | 标题 |
| CONTENT | text | NOT NULL | 主体内容 |
| WRONG_CONTENT | text | | 仅 pitfall：错误示例 |
| FIX_STRATEGY | text | | 仅 pitfall：修正方案 |
| TAGS | varchar(500) | | 关键词标签（LIKE 检索） |
| SCENE_CODES | varchar(500) | ↔tss_ai_scene.SCENECODE | 关联场景，NULL=全局 |
| WIZARD_STEPS | varchar(100) | | 关联向导步骤 0-5，NULL=不限 |
| PRIORITY | int | 默认 3 | 优先级 1-5（≥4 硬规则必注入） |
| QUALITY_SCORE | int | 默认 0 | example 评分 0-5 |
| HITCOUNT | int | 默认 0 | 命中次数 |
| SOURCE | varchar(50) | 默认 'manual' | manual / auto_seed / feedback |
| 审计六列 + ISDELETED | — | | |

索引：idx_type_asset(MEMORYTYPE, ASSETTYPE, ISDELETED)；idx_scene(SCENE_CODES 前缀）；idx_tags(TAGS 前缀）；idx_priority(PRIORITY, HITCOUNT)。33–38/40–48 号迁移持续灌入种子（ORM 铁律、SFC/SQL/C# 示例、踩坑、术语，SOURCE='auto_seed'）。管理模块 RS_M26。

#### 3.5.4 tss_ai_feedback — AI 反馈回流表（32 号迁移创建）

ID PK；SESSIONID→tss_aidev_session.ID；SCENE_CODE varchar(32)；ASSETTYPE；USERID/USERNAME；FEEDBACK_TYPE varchar(20)（thumbs_up/thumbs_down/edited/adopted）；USER_REQUEST/ORIGINAL_OUTPUT/FINAL_OUTPUT/DIFF_TEXT text；ISSUE_TAGS varchar(500)（naming/syntax/logic/missing_field/permission…）；QUALITY_SCORE int 1-5；COMMENT text；PROMOTED tinyint 默认 0（已提升为 tss_ai_memory example）；CREATETIME。索引 idx_scene_type、idx_promoted。

### 3.6 AI 控制台 / LLM 配置 / 用量 / 助理会话表

#### 3.6.1 TBS_ASSISTANT_CONVERSATION — 助理会话

ID char(36) PK；USERID/USERNAME varchar(64)；TITLE varchar(200)；CREATETIME/UPDATETIME；ISDELETED。

#### 3.6.2 TBS_ASSISTANT_MESSAGE — 会话消息（含工具调用记录）

ID char(36) PK；CONVERSATIONID→会话（IDX_CONV(CONVERSATIONID, CREATETIME)）；ROLE varchar(20)（user/assistant/tool）；CONTENT text；BLOCKSJSON text（结构化块：文本/工具调用/反馈块等）；CREATETIME；ISDELETED。

#### 3.6.3 TBS_LLM_CONFIG — LLM 模型配置（管理模块 RS_M14）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | char(36) | PK | 主键（←tss_ai_scene.MODELID 引用） |
| PROVIDER | varchar(32) | | 供应商（DeepSeek…） |
| APIKEY | varchar(512) | | API 密钥（AES 密文） |
| MODELNAME | varchar(64) | | 模型名 |
| BASEURL | varchar(255) | | API 地址 |
| PRICEINPUT / PRICEOUTPUT | decimal(10,6) | 默认 0 | 输入/输出单价（元/千 token） |
| PARAMS | text | | 参数 JSON |
| FALLBACKID | varchar | →本表.ID | 降级模型（不可用沿链最多降 3 级；代码侧保障列） |
| ISVISION | int | | 视觉模型标记（代码侧保障列） |
| ENABLED | tinyint | 默认 0 | 启用（唯一启用行=全局默认） |
| ISDELETED | tinyint | 默认 0 | |

#### 3.6.4 TBS_LLM_USAGE — LLM 调用用量（管理模块 RS_M15，只读）

ID char(36) PK；USERID/USERNAME；CONVERSATIONID；**MODULECODE varchar(64)** 关联模块、**TOOLNAME varchar(64)** 工具名（51 号迁移补注册）；OPERATIONTYPE varchar(32)（=场景编码，配额按它统计）；PROMPTTOKENS/COMPLETIONTOKENS/TOTALTOKENS int；COST decimal(10,4)；DURATIONMS int；ISSUCCESS tinyint；ERRORMSG text；REQUESTTIME datetime；ISDELETED。索引 IDX_USER_TIME(USERID, REQUESTTIME)。

#### 3.6.5 TBS_ASSISTANT_PROMPT — 提示词（管理模块 RS_M16）

ID PK；PROMPTKEY（←tss_ai_scene.PROMPTKEY 引用）；CONTENT；DESCRIPTION；UPDATEDBY/UPDATETIME；VERSION/WEIGHT（A/B 测试，唯一索引 UK_PROMPTKEY_VER(PROMPTKEY, VERSION)）；ISDELETED。

### 3.7 模板市场表

#### tss_module_template — 业务模块模板（15 号迁移创建，管理模块 RS_M25）

| 字段 | 类型 | 约束 | 含义 |
|---|---|---|---|
| ID | varchar(36) | PK | 主键 |
| TEMPLATECODE | varchar(64) | 唯一 uk_templatecode | 模板编码 |
| TEMPLATENAME | varchar(128) | NOT NULL | 模板名称 |
| CATEGORY | varchar(32) | | 业务分类 b01/r01/r02/s01（D0707） |
| DESCRIPTION | varchar(500) | | 描述 |
| VARIABLES | text | | 安装变量定义 JSON `[{name,label,default,required}]` |
| SCRIPT | longtext | | 元数据脚本（与 .aidev.sql 同构，含 `${VAR}` 占位） |
| SOURCEINFO | varchar(200) | | 来源（模块编码或会话编码） |
| VERSION | varchar(16) | 默认 '1.0.0' | 版本 |
| ENABLED | tinyint | 默认 1 | |
| 审计六列 + ISDELETED | — | | |

机制：TemplateExporter 遍历模块元数据生成幂等脚本；安装 = 变量替换 → UpgradeExecutor.Import/Execute（单事务 + 快照回滚）。

### 3.8 全局关系图（逻辑外键汇总）

```
元数据主干：
  tss_moudle 1─n tss_moudlepath → tss_resource
  tss_moudle 1─n tss_moudleapi（经 PATHNAME/FILTERCODE/SQLID/SCRIPTCODE
                 分别关联 moudlepath / resfilter / tss_sql / 脚本资产）
  tss_moudlepathrel 描述主子数据源关系
资源层：
  tss_resource 1─n tss_resfield / tss_resfilter / tss_resuipc
  DATAVIEW.TABLERESOURCEID → TABLE 资源
  resfield.REFFIELDID → TBS 字段；REFRESOURCEID/UPFIELDID 做引用名称虚拟列
页面层：
  tss_module_page（MODULECODE↔moudle，PARENTID 自关联，*APICODE↔moudleapi）
    1─n tss_module_button（PAGEID；PERMCODE = FUNCCODE/FUNCPOINTCODE）
菜单权限：
  tss_func（UPFUNCID 自关联）1─n tss_funcpoint（FUNCID，APICODE↔moudleapi）
字典：
  tss_dict 1─n tss_dictitem；resuipc.SELECTDATA = tss_dict.DICTNAME
AI 开发链：
  tss_aidev_session 1─n tss_aidev_changeset 1─n tss_aidev_changeitem
  tss_aidev_upgrade 1─n tss_aidev_upgrade_log（ITEMID→changeitem）
                   1─n tss_aidev_upgrade_snapshot
版本发布：
  tss_dev_version_cfg（RESOURCENAME↔resource）驱动 tss_dev_version（OBJID→源表行）
  tss_release.TAG ↔ tss_dev_version.TAG
代码资产：
  tss_code_asset（CODE ← moudleapi.SCRIPTCODE/SQLID；
                 MODULEPATH ← module_page.SFCMODULEPATH/PAGECONFIG）
AI 运行链：
  tss_ai_scene（MODELID→TBS_LLM_CONFIG.ID 及 FALLBACKID 降级链；
               PROMPTKEY→TBS_ASSISTANT_PROMPT）
  tss_ai_tool.SQLCODE → tss_sql
  TBS_LLM_USAGE（OPERATIONTYPE=SCENECODE 供配额统计；TOOLNAME→tss_ai_tool）
  tss_ai_feedback（SESSIONID→aidev_session；PROMOTED→tss_ai_memory example）
```

### 3.9 平台自注册模块清单（元数据模块地图）

| 模块编码 | 名称 | 前端页面 | 说明 |
|---|---|---|---|
| RS_M01 | 资源管理 | s01/m01 | tss_resource/resfield/resfilter/resuipc 维护 |
| RS_M02 | 模块管理 | s01/m02 | tss_moudle 系列维护 |
| RS_M03 | 功能管理 | s01/m03 | 菜单/权限点 |
| RS_M06 | 字典管理 | s01/m06 | 字典 |
| RS_M13 | SQL 配置 | s01/m13 | tss_sql 模板（历史，待评估下线） |
| RS_M14/M15/M16 | LLM 配置 / LLM 用量 / 提示词 | s01/m14-16（已归并入 m27） | AI 基础配置 |
| RS_M17 | 代码在线开发 | s01/m17 | 统一代码资产 IDE |
| RS_M18 | 模块配置 | s01/m18 | 页面/按钮可视化配置 + AI 向导 |
| RS_M21 | API 脚本管理 | 通用页 | C# 脚本（历史入口） |
| RS_M22 | 开发版本中心 | s01/m22 | 版本快照/回滚/标记/发布 |
| RS_M23/M24/M26 | AI 场景 / AI 工具 / AI 记忆 | 通用页（已归并入 m27） | AI 运行配置 |
| RS_M25 | 模板市场 | s01/m25 | 模块模板导出/安装 |
| RS_M27 | AI 配置中心 | s01/m27 | AI 一站式配置（聚合 M14-M16/M23/M24/M26） |
| RS_M28 | 模块开发中心 | s01/m28 | 一站式聚合工作台（零数据迁移复用各模块接口） |
| RS_MAIDEV | AI 开发助理 | s01/mAIDev | 会话/变更包/变更项 |
| RS_MAIDEVUPG | 升级管理 | s01/mAIDevUPG | 升级脚本导入/执行/回滚 |

---

## 四、后端设计

### 4.1 运行时骨架（Startup）

`netcore/Realso.WebAPI/Startup.cs`：

- **DI 注册**：`LlmClient` / `AgentEngine` / `DevAgentEngine` / `AssistantAgentEngine` / `IToolRegistry` 为 Singleton；`ChangeSetEngine` / `ChangeSetExporter` / `AiDevOrchestrator` / `WizardStepOrchestrator` / `UpgradeExecutor` / `PromptService` / `UsageLogger` / `SessionStore` / `LlmConfigService` 为 Scoped（`Startup.cs:86-111`）。
- **启动副作用**：`PromptDefaults.Register()`（提示词默认值注册 + 同步 DB）；`BuiltinToolSync.SyncAll()`（内置工具清单同步 `tss_ai_tool`）（`Startup.cs:99-101`）。
- **SignalR Hub**：`/chatHub`、`/assistantHub`。

### 4.2 控制器与 API 端点清单

#### 4.2.1 统一入口（基类）

| 控制器 | 路由 | 说明 |
|---|---|---|
| DataController（基类） | `POST /api/data/init`、`POST /api/{ctrl}/call/{modulename}/{apicode}` | 全部低代码模块的统一运行时：按 `tss_moudleapi.APITYPE` 分发 query/open/save/delete/submit/check/verify/sql/csharp/script，未知类型 → `doMyApi` 虚方法 |

三类代码资产运行时接口（DataController 内）：
- **APITYPE=sql**：SQL 模板接口（NVelocity + Dapper，多语句单事务 + DDL 黑名单）—— `doSqlApi`（`DataController.cs:156`）
- **APITYPE=csharp**：C# 脚本接口（Roslyn 运行时编译，源码 MD5 哈希热更新）—— `doScriptApi`（`DataController.cs:263`）+ `Services/Scripting/CSharpScriptEngine.cs:19`
- **APITYPE=script**：声明式编排接口（APIPARAM = 步骤 JSON：sql/query/if/update/return）—— `doScriptFlowApi`（`DataController.cs:322`）

#### 4.2.2 AI 开发相关控制器

| 控制器 | 路由前缀 | 端点 | 用途 |
|---|---|---|---|
| RMAIDevController | `api/RMAIDev` | `call/...` A05–A19 | A05 generate / A06 validate / A07 export / A09 confirm / A10 reject / A11 unconfirm / A12 getScript / A13 archive / A14 dedup / A15 merge / A16 listItems / A17 executeConfirmed / A18 getConversation / A19 openWizardSession |
| | | POST `generate-stream` | SSE 整模块生成（NEW/MODIFY），事件 steps/text/tool_call/tool_result/item/validate/step/error/done |
| | | POST `generate-all-stream` / `generate-step-stream` | SSE 向导一键 6 步连续生成 / 分步生成 |
| | | POST `feedback` / `promote-example` / `invalidate-memory` / `invalidate-tool-cache` / `test-llm` | 反馈回流、提升记忆示例、缓存失效、LLM 连通性测试 |
| RMSfcAiController | `api/RMSfcAi` | POST `generate-code` | SSE SFC 代码助手（editTarget 分提示词），事件 text/tool_call/tool_result/heartbeat/error/done |
| | | POST `get-module-schema` / `execute-metadata-sql` | 取模块元数据 schema / 执行用户确认过的元数据 SQL（禁 DROP/ALTER/TRUNCATE/CREATE） |
| RMAIDevUpgController | `api/RMAIDevUpg` | `call/...` A05–A08 | A05 import（.aidev.sql 入库 PENDING）/ A06 execute（单事务）/ A07 rollback / A08 preview |
| RDevVersionController（RS_M22） | `api/RDevVersion` | `call/...` A05–A11 | A05 rollback / A06 current / A07 mark / A08 batchMark / A09 createRelease / A10 deployRelease（SHA256 校验）/ A11 listReleases |
| RCodeAssetController（RS_M17） | `api/RCodeAsset` | `call/...` A07–A09 | A07 testSql（NVelocity 注参试运行，仅 SELECT/WITH/SHOW，LIMIT 200）/ A08 testScript（Roslyn 试运行）/ A09 assetApis |
| RModuleTplController（RS_M25） | `api/RModuleTpl` | `call/...` A05/A06/A08 | 导出模块为模板 / 安装模板 / AI 会话存为模板 |
| AssistantController | `api/assistant` | POST `tool-result` / `scene-config` / `analyze-image` | 前端工具结果回传 / 场景配置下发 / 视觉识别 |
| AssistantHub（SignalR） | `/assistantHub` | `Ask` / `AskForm` / `OptimizePrompt` / `RegisterFrontendTools` / `FrontendToolResult` | 通用助理（落库会话）与表单填报（内存会话）的 ReAct 循环 |
| S01M01Controller | `api/S01M01` | `open/compare/sync/refresh/unregistered/batchCreate` | 模块元数据管理与同步 |

### 4.3 Agent 引擎与工具执行机制

#### 4.3.1 统一 ReAct 骨架

`Services/Agent/AgentEngine.cs:42` `RunLoopAsync`（从 4 套历史循环提取）：

```
for step < MaxSteps:
  ① LlmClient.StreamChatAsync（OpenAI 兼容流式）→ sink.OnContent
  ② IUsageRecorder.Record（token 用量）
  ③ 无 tool_calls → 终答 break
  ④ assistant tool_calls 追加 messages
  ⑤ 逐 tool_call：前端工具 → ToolContext.FrontendHandler（推 frontend_tool_call，等 30s 回传）
     后端工具 → sink.OnToolCall → executor.Execute → sink.OnToolResult（摘要截断）→ 结果喂回
  ⑥ OnToolsExecutedAsync 钩子（DevAgentEngine 转 ChangeItem）
达上限 sink.OnError；OnLoopDoneAsync → sink.OnDone
```

差异化注入（`AgentRunRequest`）：`IAgentEventSink`（SignalREventSink=Hub 推送 / AiDevCallbackSink=SSE 回调）、`IToolExecutor`、`IUsageRecorder`、`ToolToStepMapper`、`AgentOptions`（MaxSteps/截断/心跳，可被场景 PARAMS JSON 覆盖 `ApplySceneParams`）。子类：`DevAgentEngine`（变更项钩子 + 校验 + 步骤事件）、`AssistantAgentEngine`（navigate/fill/subtable 特殊结果）。

#### 4.3.2 工具三层体系

1. **内置工具（C# 实现）**——`AssistantToolExecutor`（约 3400 行）三套静态定义：
   - assistant 8 个：search_menu / get_module_schema / query_data / query_stats / open_record / navigate / search_existing_resource / read_table_schema
   - formfill 表单填报工具组
   - **dev 开发 20+ 个**：create_physical_table / configure_resource_field / define_dataview / configure_ui_field / search_dict / create_dict / define_filter / register_module / define_api / define_sql_api / define_script_api / define_script_flow_api / update_script_flow_api / read_script_flow_api / create_menu / create_funcpoints / define_page / define_button / create_sfc_module + 只读类 read_sfc_template / read_api_script / search_module_template 等。**开发工具只产出 {sql, metadata} 不直接写库**，由编排层转 ChangeItem。
   - `SfcAiToolExecutor` 6 个只读工具：get_module_schema / get_module_pages / get_uiset / get_sql_list / get_module_files / read_code_asset。
   - 前端工具 18 个（fill_form / navigate 等，`FrontendToolHandler` 经 SignalR 转发等回传，超时 30s）。
2. **声明式 SQL 工具**——`DeclarativeSqlToolExecutor`：`tss_ai_tool` 表里配 工具名/描述/参数 JSON Schema/SQLCODE 即注册给 LLM，**不用写 C#**。安全边界：模板原文与 NVelocity 注参后**双重 SELECT 校验**、MAXROWS 截断（默认 200）、60s 缓存、表不存在降级为空。
3. **定义覆盖合并**——`AssistantToolExecutor.MergeDeclarative`：先以 DB 中 builtin 行覆盖 C# 工具的描述/参数（配置中心在线改），再追加声明式工具（同名内置优先）；`BuiltinToolSync.SyncAll` 启动时把 C# 工具定义同步进 `tss_ai_tool`（只读展示）。

#### 4.3.3 用量记录

`IUsageRecorder` 三实现：`DbUsageLogger`（每轮一条）、`AggregateUsageReporter`（循环累计 done 时汇总一条，SFC 场景）、`NullUsageRecorder`。底层 `UsageLogger.Log` 写 `TBS_LLM_USAGE`（OPERATIONTYPE=场景编码、token 数、按单价估算 COST、耗时）。场景日配额由 `SceneConfigService.CheckDailyQuota` 按 OPERATIONTYPE 汇总当日 TOTALTOKENS 校验。

### 4.4 AI 开发编排工作流（AiDevOrchestrator）

核心文件：`Services/AiDev/AiDevOrchestrator.cs`（约 650 行）。`GenerateAsync` 完整流程：

1. **加载/创建会话 + 变更包**：查 `tss_aidev_session`；无 CHANGESETID 则插 `tss_aidev_changeset` 并回写，会话状态置 GENERATING。
2. **取 LLM 配置**：`SceneConfigService.GetScene("aidev")` → `LlmConfigService.GetByScene`（场景 MODELID → FALLBACKID 降级链 ≤3 级 → 全局默认）。
3. **构造 messages**：system prompt 按 SESSIONTYPE 分支（`aidev_system_new` / `aidev_system_modify`，占位符 `{TARGET_MODULE}/{ADD_FIELD_GUIDE}/{NAMING_RULES}/{IRON_RULES}` 代码内替换）+ 历史对话 + **已产出变更项摘要**（防重复）+ 当前用户消息。
4. **工具集**：`GetDevToolDefinitions()`（20+ 开发工具）。
5. **ReAct 循环**：委托 `DevAgentEngine.RunLoopAsync`（MAX_TOOL_ROUNDS=100），事件经 `AiDevCallbackSink` 透传 SSE。
6. **工具结果→变更项**：每轮工具执行后 `OnToolsExecutedAsync` 调 `TryBuildChangeItem`——反射取工具结果的 `sql/metadata/warnings`，只读工具显式跳过；`MapToolToCategory` 映射 CATEGORY/ACTION/TARGET；`BuildRationale` 生成人类可读变更说明。
7. **落库 DRAFT**：`ChangeSetEngine.AppendItem`——事务内分配 ITEMSEQ，**语义去重**（同 CATEGORY+ACTION+TARGET 下 `NormalizeSql` 去 GUID 后缀/时间戳后比对），重复则回滚并改写 tool result 为 `{skipped:true}` 告知 LLM 勿重复产出。
8. **校验**：循环结束 → `ChangeSetValidator.Validate`（13 组铁律，只查 METADATA JSON 不查库），报告写回 `tss_aidev_changeset.VALIDATIONPASSED/VALIDATIONREPORT`。
9. **对话持久化**：追加 user/assistant 消息到 `CONVERSATION`。

#### 变更项生命周期

```
DRAFT ──confirm(A09)──▶ CONFIRMED ──ExecuteConfirmed(A17)──▶ EXECUTED
  │                      │  ▲
  ├──reject(A10)──▶ REJECTED
  ├──merge(A15)──▶ MERGED（生成一条 CONFIRMED 汇总项）
  └── dedup(A14) 清理重复
CONFIRMED ──unconfirm(A11)──▶ DRAFT（有已确认依赖项时拒绝）
```

- 确认时校验 DEPENDSON 依赖已确认，分配 CONFIRMORDER。
- **导出**：`ChangeSetExporter.Export` 按 CONFIRMORDER 拼 `.aidev.sql`——@META 头 + 幂等检查段（tss_aidev_upgrade 查 SESSIONCODE SUCCESS）+ @ITEM 分节 + 升级登记 INSERT；导出即冻结会话 STATUS=EXPORTED；A13 归档 EXPORTED→ARCHIVED。
- **执行**：`ExecuteConfirmed`——CONFIRMED 项按序在**单事务**逐语句执行，全成功 commit 并置 EXECUTED，任一失败 rollback；成功后**版本捕获**：按 CATEGORY+METADATA 提取触及的元数据对象交 `DevVersionService.CaptureObjects` 快照。

#### 向导模式（WizardStepOrchestrator）

6 步：基本信息 / 数据模型 / 视图与查询 / 接口与页面 / UI 配置 / 菜单注册。`STEP_TOOL_MAP` 限定每步工具子集；每步独立 system prompt（`wizard_step_N` + `wizard_common_rules` + 表单上下文 + **MemoryService.BuildMemoryPrompt 记忆注入**）；6 步共享 sessionId/changesetId；分步模式强制上一步变更项全部 EXECUTED 才能进下一步；跨步骤时序裂缝由 `LookupDraftResourceId`（从 DRAFT 项 METADATA 找前序资源 ID）兜底。

#### 升级链路

`UpgradeExecutor`（673 行）解析 `.aidev.sql` @META/@ITEM → Import 入 `tss_aidev_upgrade`(PENDING) → Execute 单事务执行（失败回滚）→ Rollback 执行 ROLLBACKSCRIPT。发布包链路 `doCreateRelease` 按 TAG 收集版本对象生成幂等 INSERT IGNORE 脚本入 `tss_release`，部署时 SHA256 校验后复用 UpgradeExecutor.Import。

### 4.5 SFC AI 辅助开发流程

`RMSfcAiController.GenerateCode`（`RMSfcAiController.cs:43`）：

1. SSE 通道建立（writeLock 串行化 + 15s 心跳）。
2. `SceneConfigService.GetScene("sfc")` 配额检查 → `LlmConfigService.GetByScene` 场景级模型路由。
3. `ParseContext` 解析前端 context（currentFile/siblingFiles/moduleCode/**editTarget**/pageCode）。
4. `BuildSystemPrompt` 按 editTarget 选提示词：csharp→`script_ai_cs_prompt`、sql→`script_ai_sql_prompt`、js→`script_ai_js_prompt`、其余（extendjs/store/sfc）→`sfc_ai_system_prompt`；PromptService 读不到回落代码常量。
5. `BuildUserMessage` 拼接当前文件 + 同级文件 + 模块编码，**元数据不预注入**，由 AI 通过工具自取。
6. `AgentEngine.RunLoopAsync`（MaxSteps=15，MaxToolResultChars=4000）+ `SfcAiToolExecutor`（6 只读工具）+ `AggregateUsageReporter`；done 事件带 usage。

工具实现都在 `SfcModuleSchemaService`（556 行）：直查 `tss_moudle` / `tss_moudleapi` / `tss_resfilter`（正则提取 @VAR 参数名）/ `tss_moudlepath`+`tss_resource` / `tss_resfield` / `tss_resuipc` / `tss_module_page` / `tss_module_button` / `tss_sql` / `tss_code_asset`。

### 4.6 提示词体系（四层）

- **存储**：`TBS_ASSISTANT_PROMPT`（PROMPTKEY/CONTENT/VERSION/WEIGHT），同 key 多版本 + 权重随机（A/B 测试）。
- **服务**：`PromptService`——30s MemoryCache → DB 多版本加权选择 → 代码默认值兜底；`RegisterDefault` / `RegisterDefaultForce`（强制覆盖，用于代码 prompt 升级）；`SyncDefaultsToDb` 启动同步（不覆盖用户在线修改）。
- **默认值注册**：`PromptDefaults.Register()` 集中注册全部 key——助理（`system_general`/`system_form`/`tool:*`）、SFC（`sfc_ai_system_prompt` 约 957 行 Vue2+HeyUI 知识库、`script_ai_cs/sql/js_prompt`）、AI 开发（`aidev_system_new/modify`）、向导（`wizard_common_rules` + `wizard_step_0~5`）、其他（`meta_optimize_prompt`/`vision_default_prompt`）。
- **场景路由**：`SceneConfigService` 读 `tss_ai_scene`（TRANSPORT/ENDPOINT/TOOLSET/PROMPTKEY/MODELID/PARAMS/DAILYQUOTA/FRONTENDTOOLS/CONTEXTSOURCE），60s 缓存 + 6 个内置默认场景，实现"场景 → 传输+工具集+提示词+模型+参数+配额"统一配置；前端经 `api/assistant/scene-config` 拉取。

### 4.7 记忆中枢（MemoryService）

`Services/AiMemory/MemoryService.cs` 读 `tss_ai_memory`（rule/example/pitfall/glossary），三级检索：**硬规则 PRIORITY≥4 必注入** / 关键词计分召回示例 / 反模式 TAGS 命中强制注入 → `BuildMemoryPrompt` 拼 system prompt 增量段（已接入向导）。回流：`tss_ai_feedback` 记录反馈 → `promote-example` 提升为示例；HITCOUNT 统计"越用越知哪条常用"。

### 4.8 hs2-java 与 netcore 的关系

**netcore 是活跃主线，hs2-java 是进行中的 Java 移植版（单快照、略滞后）。** `AIDevService.java:33-43` 头注释明确对应 .NET 端 RMAIDevController + AiDevOrchestrator + ChangeSetEngine + ChangeSetExporter + LlmConfigService；Java 端用 magic-api 脚本承载普通 CRUD 接口，SSE 保留为 Java Controller（`SseController.java` 暴露等价端点）。移植常量有差异（如 MAX_TOOL_ROUNDS：Java=200 vs netcore=100；向导 STEP_TOOL_MAP 缺后加工具），说明 Java 快照早于 netcore 近期迭代。

---

## 五、前端设计

### 5.1 通用模块引擎 generic-module（元数据驱动渲染）

目录 `p-admin/src/components/generic-module/`。

#### 5.1.1 元数据来源与 store

- 模块元数据存于 `state.app.modules[moduleCode]`，含 `MODPAGE`（页面）、`MODBUTTON`（按钮）、`MODPATH`（数据路径→RESOURCENAME）；字段级元数据（scm）经 `app/initScms` 加载到 `state.app.scms[RESOURCENAME]`。
- `generic-store.js:38` `getGenericStore(moduleCode)` 按模块编码动态创建 Vuex 模块（Store03 系），缓存于 storeCache；`applyStoreExtend()` 尝试加载约定路径 `@/modules/{moduleCode}/store.js` 的 SFC store 扩展（actions/mutations 运行时合并进已注册模块，支持热更新）。

#### 5.1.2 generic-module.vue（约 1600 行）—— 页面级渲染

- props：`moduleCode` + `pageCode`（默认 'main'）；`pageConfig` 从 `moduleData.MODPAGE` 按 PAGECODE 取页。
- 按 `PAGETYPE` 分发渲染：
  - **list** → 查询区 + 表格 + 分页，新增/编辑弹 rs-modal 内嵌 generic-form
  - **form** → 整页 generic-form
  - **select** → 选择器风格列表（单/多选，可递归渲染另一个 generic-module）
  - **review** → 审核页
  - **report** → 报表页（`PAGECONFIG.REPORT` 驱动 report-t01 + ECharts）
  - `COMPONENTTYPE==='sfc'` 且有 `SFCMODULEPATH` → `loadCompiledSFC` 在线组件
- **按钮渲染**：MODBUTTON 按 BTNAREA（header/footer/row/子表路径）分区 + SORTNO 排序 + SHOWCOND 表达式/扩展方法控制显隐；INTERACTTYPE==='poptip' 自动包确认气泡；`v-per="btn.PERMCODE"` 权限。
- **按钮动作分发** `handleBtnAction`：`EXTPARAM.action` = `openForm`（弹表单）/ `openSelector`（选入弹窗）/ 默认 `doCallApi`（按 BTNCODE 走 save/export/submit/check 等标准 action）；`beforeAction`/`afterAction` 钩子调扩展 JS 方法。
- **三级扩展机制**：
  1. 扩展 JS mixin：`PAGECONFIG.EXTENDJS` 或约定 `@/modules/{moduleCode}/{pageCode}.js`，methods/computed/data/init/mounted 合并进组件实例
  2. SFC slot 组件：`simple-query`/`body-query`/`header-action`/`footer-action`/`table-action` 等命名 slot 可用在线 SFC 替换
  3. 标准接口暴露给扩展 JS：`openPage()` / `openSelector()` / `closePage()`
- QQRY 查询字段通过 `Object.defineProperty` 映射到 `this.字段名`，扩展 JS 可直接读写。

#### 5.1.3 generic-form.vue（约 700 行）—— 表单级渲染

- 基于 `rs-form-edit`；主表 scm 字段按 `EDITGROUP` 分组（多分组渲染 Tabs）、`EDITSORT` 排序；`PAGECONFIG.FORMLAYOUT` 控制一/两/三列。
- 子表按钮按 BTNAREA=子表路径分组经 provide 下发；footer 渲染按钮（同 SHOWCOND/EXTPARAM 机制）。
- 扩展 slot：`form-top`/`form-bottom` + 字段级 slot；provide `visibilityHost` 让 `ISSHOWxxx` 方法在扩展上查找。

#### 5.1.4 其他通用件

- `sfc-editor-popup.vue`（842 行）：SFC/扩展 JS 弹窗编辑器（文件列表 + CodeMirror + 预览 + AI Tab）。
- `code-editor-popup.vue`（1127 行）：模块脚本弹窗（csharp/sql/js/vue 四组文件列表、选入关联、测试、版本、AI），与 m17 IDE 共用 `code-asset.js` 访问层。
- `script-flow-editor.vue`：编排接口编辑器（多接口串行脚本流）。
- `version-history-popup.vue` + `version-diff-view.vue`：通用版本历史/对比。
- `code-test-panel.vue`：接口测试面板；`save-actions.vue`：快速保存/提交保存双按钮；`generic-selector.vue`：通用选入弹窗。

### 5.2 SFC 在线开发机制（`src/sfc-loader/`）

#### 5.2.1 编译（保存时/预览时）—— sfc-compiler.js

`compileSFC(sourceCode, modulePath, fileType)` 五步：

1. `vue-template-compiler.parseComponent` 拆 template/script/styles
2. template → render 函数串（`compiler.compile`）
3. script → `@babel/standalone` 转 ES Module 为 CJS，**剥掉 `"use strict"`**（render 用 `with(this)`，严格模式会炸）
4. styles → less 编译 + 简化版 scoped（选择器加 `[d-xxx]` 属性）
5. `extractDeps` 静态提取 import 路径为 DEPS，组装成 `(function(module, exports, require, Vue){...})` 字符串；render 以 `_renderStr` JSON 字符串注入，样式注入包进 beforeMount/mounted 钩子

`executeCompiled` 用**间接 eval `(0,eval)()`** 在全局非严格作用域执行工厂函数，再把 `_renderStr` eval 成真函数。

#### 5.2.2 模块解析（运行时）—— module-resolver.js

自定义 `__sfc_require__(modulePath, callerPath)`，四类来源：

- **类型 A**：`@/` webpack 桥梁模块（`module-bridge.js` 启动时把 db/Store/Store03/mixins 等挂到 `window.__SFC_MODULES__`，含 `__esModule` 标记防 Babel 双包）
- **类型 A2**：`require.context('@/', ...)` 预扫描全 src，桥梁未注册的 `@/components/xxx` 按需 webpack require
- **类型 B**：数据库模块——`POST /api/data/call/RS_M17/A06/` 按 MODULEPATH 取 COMPILEDCODE+DEPS，先 `preloadDeps` 递归预载依赖再同步 executeCompiled；模块缓存 + resolvingQueue 防循环依赖
- **类型 C**：全局库 vue/heyui/vuex/axios

缓存治理：`invalidateCacheByPrefix` 按路径前缀精准失效（编辑器保存后/预览前用），全局库不清。

#### 5.2.3 挂载入口

- **运行时页面**：`remote-route.vue`——路由 name 含 `/online/` 时由 `registerOnlineRoute()` 动态注册路由（`@/pages/{s01/m16}/views/{main}.vue` 约定推导），Wrapper 组件 name 对齐 keep-alive 缓存 key。
- **通用模块页面**：`generic-module.vue loadSfcModule()`（COMPONENTTYPE=sfc）。
- **预览**：m17 `sfc-preview.vue`（800ms 防抖 → compileSFC → preloadDeps → executeCompiled → `Vue.extend` 离线 `$mount()`）。

### 5.3 代码在线开发 IDE —— s01/m17（RS_M17）

- 路由 `/s01/m17`（重定向 `/s01/m17/edit`），菜单隐藏；主视图 `views/edit.vue`（1599 行）三栏 IDE：左文件树 / 中编辑器 / 右预览+AI+测试 Tab。
- store：vuex 命名空间 `s01/m17`，数据源 `RS_M17`（统一视图 `VSS_CODE_ASSET`）。
- **统一访问层 `code-asset.js`**：四类代码资产的打开/校验/保存全部经 DataTable + Store03：
  - `checkAsset()`：csharp 走后端 Roslyn（`RS_M21/A05`）；js/vue 走前端 compileSFC；sql 走前端铁律校验（禁单引号、禁 DDL、必须 SELECT/INSERT 等开头）
  - `saveAsset()`：先校验再 dt.setValue + dispatch save，js/vue 保存时重算 COMPILEDCODE/DEPS
  - AI 多文件联动：`parseAiFileBlocks()`（解析 `###FILE: 路径` 段）、`applyAiFileOps()`（脚本类落库后自动调 `RS_M18/A07` 关联模块接口、分配接口码 A51+）
  - 目录推导：`SC_/SS_ + 模块编码` 前缀 → 模块目录；js/vue 以 MODULEPATH 为身份
- 组件：`file-tree.vue`（统一 `RS_M17/A01` 取全类型资产按模块目录分组）、`sfc-code-editor.vue`（CodeMirror 封装，fileType→mode 映射，提供 applySearchReplace/insertAtCursor/setValue）、`sfc-preview.vue`（实时预览）、`ai-chat-panel.vue`（内嵌 AI 助手）。
- `templates/index.js`：4 套代码生成模板——单表 CRUD / 主子表 CRUD / 审批流单据（各含 main.vue+add.vue+store.js 生成器）。
- 关键交互：`onApplyCode` 支持 search-replace 精准替换 / replace 全部 / insert 光标处 / newfile 四种应用模式；`handleSave` 分 quick save（不留版本）与 commit save（写版本说明）；支持 `?scriptCode=?sqlCode=?newKind=` 路由直达。

### 5.4 模块配置器 —— s01/m18（RS_M18）

- 路由 `/s01/m18/config`；主视图 `views/config.vue`（3138 行）。
- 布局：顶部工具栏（新建模块向导、Store 扩展、版本历史、模块脚本、编排接口、导出模板、发布）；左侧页面列表（两级子页面，支持"自定义子页面"+"引用其他模块"）；下方页面属性表单（页面编码/类型/路由/组件类型/SFC 路径/四个 APICODE 等）；右侧按钮配置区（按 header/footer/row/子表分区，预设按钮、流程按钮、EXTPARAM 构建器、字段映射、钩子片段生成）。
- store paths 直配 `MAIN=VSS_MOUDLE / MODPAGE=VCK_MODULE_PAGE / MODBUTTON=VCK_MODULE_BUTTON / FUNC=VSS_FUNC / FUNCPOINT=VSS_FUNCPOINT`，保存即写模块元数据。
- 子组件：
  - `module-wizard.vue`（1038 行）：**AI 模块创建向导**，6 步（基本信息→数据模型→视图查询→页面→按钮→菜单）+ "AI 一键生成全部"，右侧对话精修
  - `page-list.vue`：独立页面列表面板；`page-preview.vue`：内嵌 GenericModule 实时预览；`config-sel-popup.vue`：跨模块复制配置
- 与 m17 联动：`openCodeFiles()` 打开 code-editor-popup；`openSfcEditor()` 打开 sfc-editor-popup。

### 5.5 AI 配置中心 —— s01/m27（RS_M27）

- 路由 `/s01/m27/main`；主视图 `views/console.vue`：顶部配置引导 checklist + 左侧导航 + 右侧 keep-alive 动态组件，6 个分区：
  - **场景 scenes**（默认）：场景列表 + 详情面板，"提示词+工具+模型一体化"——基础配置（SCENECODE/TRANSPORT/ENDPOINT/TOOLSET/CONTEXTSOURCE/ENABLED）、模型配置（指定/降级模型、Agent 参数 JSON、每日配额），内嵌提示词段、后端工具段（builtin/sql 分组开关）、前端工具段、`scene-test-chat`（按场景配置发测试对话）
  - **AI 设置 setting**：LLM 服务商/Key/模型
  - **记忆 memory**：规则/坑/示例知识库
  - **调用记录 usage**：tokens/成本/成功率统计卡 + 明细表（走 `RS_M15` 通用 store）
  - **提示词管理 prompts**、**工具管理 tools**：高级独立管理
- 49 号迁移把 LLM 配置/用量/提示词/场景/工具/记忆六个旧菜单 ISHIDE=1 归并至此。

### 5.6 模块开发中心 —— s01/m28（RS_M28）

- 路由 `/s01/m28/main`，**菜单可见**；主视图 `views/console.vue`（461 行）。
- 布局：头部（当前模块 + 在线开发 / AI 向导 / AI 侧滑按钮）+ 左 `studio-nav` 分类导航 + 主区编辑器 + 右 AI 侧滑（对话/向导双 Tab，带"上下文焦点"chip 自动注入当前模块）。
- **9 大分类**（`src/constants/section-defs.js` SECTIONS）：模块 / 资源 / 页面 / 代码 / 菜单 / 版本 / 模板 / 字典 / 场景，每类对应 `views/editors/` 下一个编辑器（module-editor、resource-editor、page-editor、code-editor、menu-editor、version-editor、template-editor、dict-editor、scene-editor + 各自 add-adapter）。
- **复用聚合**：全屏 Modal 内嵌 m18 的 `config.vue`（模块配置）、m18 的 `module-wizard.vue`（AI 向导）、m17 的 `edit.vue`（代码在线开发）、m02 的 `add.vue`（模块编辑）——m28 是把 m17/m18/m02 能力聚合的工作台。
- 后端侧：53 号迁移注册 RS_M28（只读聚合壳，不注册 moudlepath/moudleapi/resfield/resfilter，零数据迁移，权限点仅 A01）。

### 5.7 AI 前端体系

#### 5.7.1 传输层 `src/utils/ai/AiClient.js`（307 行）

- 统一客户端，**只负责传输与事件分发，不维护 messages**；按 scene 选传输：`assistant/form/optimize` → SignalR `/assistantHub`（共享单连接），`aidev/wizard/sfc` → SSE。
- SignalR 事件：`block`（assistant）、`formblock`（form）、`frontend_tool_call`（后端反向调前端工具 → aiAgentProxy.execute → 结果 HTTP POST `/api/assistant/tool-result` 回传）；连接后按场景注册前端工具子集。
- `handleBlock` 统一分发 block 类型：text/thinking/tool_call/tool_result/item/validate/step/navigate/fill/subtable/error/done/conversation/heartbeat。
- `sceneConfig.js`：场景配置从 `/api/assistant/scene-config` 动态拉取，失败回落内置默认。
- `streamSSE.js`：统一 SSE 解析（`\n\n` 分帧 + tail 保留 + 多行 `data:` 拼接）。
- `api/sfc-ai.js`：`POST /api/RMSfcAi/generate-code`（FormData + Bearer，SSE）、`get-module-schema`、`execute-metadata-sql`。

#### 5.7.2 全局 Vuex `store/modules/assistant.js`

- 一个入口三个智能体：`currentAgent = assistant | form | sfc`，消息数组各自独立，各持独立 AiClient。
- `onFormBlock` 把 fill/subtable block 直接应用到当前表单；sfc done 时提取 SEARCH/REPLACE 为 `search_replace` block。
- `store/modules/sfcContext.js`：编辑器上下文（editorRef/moduleCode/siblingFiles/editTarget），m17 edit.vue mounted 时 SET；全局抽屉 `AssistantDrawer.vue` 据此启用"开发"agent。

#### 5.7.3 对话面板组件（`src/components/ai/`）

- `AiMessageList.vue`：消息 = `[{role, blocks:[...]}]` 模型，按 block.type 分发：text→RichTextBlock（marked+dompurify）、thinking→ThinkingBlock、tool_call→ToolCallBlock、navigate→NavigateBlock、code→CodeBlock、metadata_sql→MetadataSqlBlock（确认执行按钮）、fill/subtable 内联卡片、search_replace 渲染 diff（红删绿增）+ "应用修改/替换全部"按钮；支持 light/dark 主题。
- `blocks/` 9 种：ChartBlock、CodeBlock、FeedbackBlock、HtmlBlock、MetadataSqlBlock、NavigateBlock、RichTextBlock、ThinkingBlock、ToolCallBlock。
- `AiInput.vue`（Enter 发送/图片粘贴）、`StepBar.vue`（向导步骤条）、`ChangeItemCard.vue`（变更项卡片）。
- m17 `ai-chat-panel.vue`：IDE 内嵌面板（dark 主题），context 含 currentFile/siblingFiles/moduleCode/editTarget；`onConfirmSql` 执行元数据 SQL 后把结果作为 user 消息自动续发 AI。

---

## 六、核心工作流

### 6.1 手工低代码开发一个模块

```
s01/m01 资源管理：注册 TBS 物理表 → 注册字段 → 建 VSS/VBS 视图 → 配过滤器(F00/F01/F02) → 配 UI(resuipc)
  → s01/m02 模块管理：注册模块 + 数据源路径(QRY/QQRY/MAIN/SEL) + 接口(A01/A02/A03/A04/A07)
  → s01/m18 模块配置：页面(main list + add form) + 按钮(按 FLOWCODE 自动生成流程按钮)
  → s01/m03 功能管理：菜单(FUNCCODE=MODULECODE, OUTERURL=/g/{MC}/main) + 权限点
  → 运行时：/g/{MODULECODE}/main 由 GenericModule 渲染
需要扩展时：PAGECONFIG.EXTENDJS 扩展 JS / SLOTS 插槽 SFC / m17 编写四类代码资产
```

### 6.2 AI 开发助理工作流（RS_MAIDEV）

**开发环境 6 步**：

```
新建会话(NEW/MODIFY) → 工作区对话(AI 调开发工具产出 DRAFT 变更项)
  → 逐项确认 CONFIRMED（校验依赖）→ 变更集校验(13 组 ORM 铁律)
  → 导出 {SESSIONCODE}_升级包.aidev.sql（会话冻结 EXPORTED）→ 归档 ARCHIVED
```

**生产环境 3 步**：上传导入(PENDING) → 预览 → 单事务执行（SUCCESS/FAILED）。
**安全保障**：幂等检查 + SHA256 防篡改 + 单事务 + 快照回滚（ROLLBACKSCRIPT）。

### 6.3 AI 向导建模块（wizard 场景）

m18/m28 的 module-wizard 或 m28 AI 侧滑 → `generate-all-stream`（一键 6 步）或 `generate-step-stream`（分步）：基本信息 → 数据模型 → 视图与查询 → 接口与页面 → UI 配置 → 菜单注册。每步工具子集受限（STEP_TOOL_MAP），记忆中枢逐步注入；分步模式强制上一步全部 EXECUTED。产物即 `demo_rs_fee.sql` 样式的完整模块元数据。

### 6.4 SFC 在线开发闭环

```
m17 IDE / sfc-editor-popup 编写 vue 源码(SOURCECODE)
  → compileSFC 浏览器端编译(render+CJS+less) → 保存 VSS_CODE_ASSET(COMPILEDCODE+DEPS)
  → 版本快照(tss_dev_version, OBJTYPE=code) → 缓存按前缀失效
  → 运行时 loadCompiledSFC 按 MODULEPATH 加载执行(module-resolver 四类 require)
AI 辅助：ai-chat-panel → RMSfcAi/generate-code(editTarget 分提示词 + 6 只读工具)
  → SEARCH/REPLACE diff → 应用修改 → 保存落库
```

### 6.5 版本与发布

- **版本捕获**：`doSave/doDelete` 统一拦截 cfg 内资源 DataView 前后镜像（同事务）+ 变更集执行后按 CATEGORY+METADATA 捕获 + 直接 SQL 通道（DevVersionService）。
- **版本中心（s01/m22）**：版本列表/对比/回滚（镜像回滚 + 生成 rollback 版本）/ 标记 TAG/置顶 / 批量打标。
- **发布**：按 TAG 创建发布包（幂等 INSERT 脚本入 tss_release）→ 部署（SHA256 校验 + UpgradeExecutor.Import）。
- **保存双态**：快速保存（不留版本）/ 提交保存（写 CHANGENOTE，版本链连续）。

### 6.6 模板市场（RS_M25）

导出：TemplateExporter 按 14 类元数据依赖序遍历模块 → 生成含 `${VAR}` 占位的幂等脚本入 tss_module_template。安装：变量替换 → UpgradeExecutor.Import/Execute（单事务 + 快照回滚）。AI 会话也可一键存为模板（A08）。

---

## 七、设计规约与铁律

以下铁律由 `ChangeSetValidator` 强校验 / AI 记忆中枢必注入（PRIORITY≥4）/ 历次踩坑沉淀：

**ORM 与命名**
1. 字段名全大写无下划线连写（`CREATETIME` 非 `CREATED_TIME`）。
2. 视图命名：物理表名首字母 T→V（`TBS_XXX`→`VBS_XXX`）。
3. RESOURCEANAME 惯用 `A`；VSS 视图 ID 字段 ISKEY=1 + KEYGENTYPE=GUID；VSS 字段 REFFIELDID 非空。
4. 引用名称虚拟列：REFRESOURCEID 必须指 TBS 资源，UPFIELDID 回指本地 ID 字段。
5. 审计字段标准六列 CREATEID(64)/CREATER(16)/CREATETIME/MODIFYID/MODIFER/MODIFYTIME + ISDELETED。

**SQL 与过滤器**
6. NVelocity 模板**禁单引号**；LIKE 用 `CONCAT(CHAR(37),@X,CHAR(37))`。
7. FILTERSQL 以 `1=1` 开头；ORDERBY 不带表别名前缀。
8. SQL 接口/声明式工具仅 SELECT（脚本接口禁 DDL）；元数据 SQL 执行禁 DROP/ALTER/TRUNCATE/CREATE。

**模块与接口**
9. moudleapi.ACTIONCODE 必填（前端按它识别接口用途）；自定义接口 APITYPE/ACTIONCODE 置 NULL 走 doMyApi。
10. 标准接口五件套：A01 query / A02 open / A03 advQuery / A04 save / A07 delete；APIPARAM：query/advQuery=`QQRY`，save/delete=`MAIN`。
11. 模块四路径：QRY / QQRY / MAIN / SEL。
12. 元数据修改必须走标准保存接口才有版本快照。

**页面与按钮**
13. COMPONENTTYPE 只能 `standard`/`sfc`（错填导致路由不注册白屏）。
14. form 页 PARENTID 必须指向 list 页；list 页 PAGECONFIG 必须含 `defaultFormPageCode`。
15. 添加按钮 EXTPARAM 三要素：`action=openForm` + `openMode` + `formPageCode`，缺一不可。
16. 表单页 footer 只配保存+删除不配取消（rs-modal 自带 X）；删除按钮 poptip + `SHOWCOND=ID!=null`。
17. INTERACTTYPE 只允许 `direct`/`poptip`。
18. 通用模板模块菜单 `OUTERURL=/g/{MODULECODE}/main`，列表页 PAGECODE=main。

**菜单权限**
19. tss_func.FUNCCODE 必须 = MODULECODE；tss_funcpoint 用 FUNCID 关联，FUNCPOINTCODE 只写纯 APICODE。
20. 按钮 PERMCODE=`{MODULECODE}/{APICODE}` 与权限点匹配才显示。

**UI 配置**
21. resuipc.FIELDNAME 必须与 resfield.FIELDNAME 一致（否则列表空白）。
22. SELECTDATA 写字典名 DICTNAME，不写内联 k:v（新规范）。
23. LISTSORT/QUERYSORT/EDITSORT 为 NULL 即不显示；长文本列表隐藏。

**代码资产**
24. csharp 源码以 0x HEX 写入（迁移脚本）；热更新按源码 MD5 哈希检测。
25. 代码资产四类统一 tss_code_asset，逻辑删除让位 MODULEPATH（uk_livepath 生成列）。

---

## 八、演进历程（迁移脚本清单）

`sql/aidev/` 54 个迁移脚本记录了平台从零到一的完整演进，可作为实施编年史：

| 文件 | 里程碑 |
|---|---|
| 01–05 | AI 开发助理 6 表 + 全套 ORM 注册（资源/视图/UI/过滤器） |
| 06–08 | RS_MAIDEV / RS_MAIDEVUPG 模块注册；`@ui` 自动生成过滤器（resuipc.QUERYMODE） |
| 09 | D0604–D0610 状态字典，SELECTDATA 从内联改字典名 |
| 10–11 | 自定义接口注册（doMyApi 通道）；tss_api_script + moudleapi.SCRIPTCODE（APITYPE=csharp） |
| 12 | **版本体系**：tss_dev_version(+cfg) + RS_M22 版本中心 |
| 13–15 | **AI 场景化**：tss_ai_scene(RS_M23) / tss_ai_tool 声明式工具(RS_M24) / tss_module_template 模板市场(RS_M25) |
| 16–17 | resuipc.COLSPAN 占宽；csharp 接口自举（RS_M24 A05 测试调用） |
| 18 | D0701–D0708 平台字典 |
| 19–21 | SS_MOD_CODEFILES 模块代码文件清单；M18 资产关联/解关联接口脚本 |
| 22–24 | **统一代码资产**：tss_code_asset 四类型合一 + 历史数据迁移 + VSS_CODE_ASSET + MODULEPATH 回填 |
| 25–27 | 版本中心增强（当前快照/标记）；uk_livepath 逻辑删除改造；dict/menu/permission 纳入版本 |
| 28–29 | 资产试运行接口（A07/A08）；**编排接口**：APIPARAM 扩 TEXT（APITYPE=script） |
| 30–31 | **发布体系**：tss_release + RS_M22 A08–A11；脚本流编辑器（M18 A12/A13） |
| 32–38 | **AI 记忆中枢**：tss_ai_memory + tss_ai_feedback + RS_M26；铁律/示例/反模式/术语种子三批+澄清 |
| 39–48 | RS_FEE 演示模块踩坑修复系列（ACTIONCODE/APIPARAM/权限/按钮/PARENTID/COMPONENTTYPE/视图命名/记忆冲突清理）——教训全部沉淀进记忆库 |
| 49–52 | **AI 配置中心**：RS_M27 聚合六个 AI 菜单；场景级模型路由(MODELID+PARAMS)；用量 MODULECODE/TOOLNAME；Phase4 resuipc 补齐 |
| 53–54 | **模块开发中心**：RS_M28 注册；SS_MOD_CODEFILES 补 VUE 资产 |

演示样板：`demo_rs_fee.sql`（RS_FEE 项目费用管理）是向导生成物的参照标准。

---

## 九、附录

### 9.1 关键源码索引

**编排/变更集**
- `netcore/Realso.WebAPI/Services/AiDev/AiDevOrchestrator.cs:63` GenerateAsync / `:324` TryBuildChangeItem
- `netcore/Realso.WebAPI/Services/AiDev/ChangeSetEngine.cs:28` AppendItem / `:346` ExecuteConfirmed / `:737` NormalizeSql
- `netcore/Realso.WebAPI/Services/AiDev/ChangeSetValidator.cs:52` ValidateItem（13 组铁律）
- `netcore/Realso.WebAPI/Services/AiDev/ChangeSetExporter.cs:32` Export / `UpgradeExecutor.cs:25` Import
- `netcore/Realso.WebAPI/Services/AiDev/WizardStepOrchestrator.cs:46` STEP_TOOL_MAP

**Agent 框架**
- `netcore/Realso.WebAPI/Services/Agent/AgentEngine.cs:42` RunLoopAsync
- `netcore/Realso.WebAPI/Services/Agent/DevAgentEngine.cs:35` OnToolsExecutedAsync
- `netcore/Realso.WebAPI/Services/Agent/DeclarativeSqlToolExecutor.cs:22` 声明式工具
- `netcore/Realso.WebAPI/Services/AssistantToolExecutor.cs:45/85/257` 三套工具定义
- `netcore/Realso.WebAPI/Services/SfcAiToolExecutor.cs:16` / `SfcModuleSchemaService.cs:18`

**配置/提示词/记忆**
- `netcore/Realso.WebAPI/Services/SceneConfigService.cs:96` CheckDailyQuota / `:116` 内置场景
- `netcore/Realso.WebAPI/Services/PromptService.cs:151` / `PromptDefaults.cs:10` / `AiDevPrompts.cs:13`
- `netcore/Realso.WebAPI/Services/AiMemory/MemoryService.cs:198` BuildMemoryPrompt
- `netcore/Realso.WebAPI/Services/LlmConfigService.cs:92` GetByScene 降级链
- `netcore/Realso.WebAPI/Services/DevVersionService.cs:19` 版本捕获

**前端**
- IDE：`p-admin/src/pages/s01/m17/views/edit.vue`、`code-asset.js:195`（AI 多文件落库）
- 引擎：`components/generic-module/generic-module.vue:437`、`generic-form.vue:86`、`generic-store.js:38`
- SFC loader：`sfc-loader/sfc-compiler.js:177`、`module-resolver.js:280`、`remote-route.vue:42`、`router/index.js:108/161`
- AI：`utils/ai/AiClient.js:114`、`store/modules/assistant.js:140`、`components/ai/AiMessageList.vue:63`、`api/sfc-ai.js:16`
- 常量：`constants/code-asset.js:9`、`module-page.js:6`、`sfc-templates.js:6`、`section-defs.js:6`

### 9.2 相关已有文档（docs/）

| 文档 | 定位 | 时效 |
|---|---|---|
| low-code-system-design.md（1290 行） | 低代码体系母文档：现状盘点 + 痛点 + 三级体系规划 | 2026-07-09；"目标方案"大部分已是现实，痛点分析正是 m28 存在的理由 |
| low-code-ai-integration-design.md（724 行） | 低代码×AI 集成设计，G1–G8 标注"全部落地"，含 as-built 偏差 | 2026-07-17/18，**最权威的实施状态文档** |
| module-dev-center-design.md（469 行） | RS_M28 实施计划草案 | 2026-07-20；已实现但 as-built 为内嵌编辑器（推翻了"跳转 Modal"决策） |
| ai-dev-assistant-guide.md | RS_MAIDEV/MAIDEVUPG 用户操作手册 | 早期描述，仍有效 |
| module-page-template.md | 页面配置 as-built 事实模板（含 8 条铁律） | 2026-07-22，最新 |
| form-refactor-design.md / flow-button-defaults.md / uiset-visibility.md | 表单重构 / 流程按钮默认值 / 显隐机制 | 均已实施 |

### 9.3 已知不一致点与注意事项

1. `tss_module_page` 的 CREATE 语句（init_module_page_button.sql）不含 PAGECONFIG/ADVQUERYAPICODE 两列，为后补物理列，§3.1.13 已按最终形态列出。
2. FALLBACKID/ISVISION（TBS_LLM_CONFIG）、DAILYQUOTA（tss_ai_scene）、VERSION/WEIGHT（TBS_ASSISTANT_PROMPT）为"代码侧保障列"——迁移文件中无对应 ALTER，由后端服务直接 SELECT 保障。
3. 30_release.sql 的 ALTER 加索引语句非幂等写法（重复执行报 1061），与其余迁移的 information_schema 守卫风格不同。
4. RS_M22 的 30 号 A08–A11 接口注册把 APITYPE 写成动作名，与"APITYPE=NULL 走 doMyApi"惯例不一致，属历史遗留写法。
5. RM16 控制器是业务模块（受理单批量打印），与 RS_M16"提示词管理"仅编号巧合，勿混淆。
6. 按钮扩展参数 JSON 在 module-page-template.md 中写作 BTNCONFIG、其余文档用 EXTPARAM——以 `tss_module_button` 实际列名（两者皆有，EXTPARAM 为行为参数、BTNCONFIG 为扩展配置）为准。

### 9.4 尚未实现的边界（避免过度承诺）

- APITYPE=script 编排接口已有运行时（doScriptFlowApi），AI 侧工具链（define_script_flow_api 等）为后续增强项
- 向导 Step0"从模板开始"未实现；Step3 未纳入脚本接口工具
- RS_M13（SQL 配置）下线评估未做；hs2-java 同步滞后（见 §4.8）
- 表单设计器仅做 COLSPAN 渐进增强，全自由画布未做
- m28 v2 规划（模块关联图/批量操作/AI Code Review/跨环境同步/插件化 Tab）未实施

---

## 十、增量补充（2026-07-25）

> 本章记录 2026-07-25 新增的功能与规划。来源：p-admin 提交 `5d93aaa`（元数据表单与查询面板组件）、`869577b`（页面组件名统一），以及一批未提交的工作区改动（rs-form-edit/list-t01 覆盖机制、三份模板规范文档、14 份模块迁移计划）。未提交内容标注为"进行中"。

### 10.1 元数据驱动表单组件 rs-meta-form / rs-meta-field（已落地）

**定位**：轻量级元数据驱动表单组件，包装 `rs-form-edit`，传数据 + 字段配置即可渲染完整表单。**关键特点是不依赖 generic-module 框架**，可在任何 `.vue` 文件（SFC slot、弹窗、独立页面）直接使用。全局注册于 `p-admin/src/components/index.js:9-12`。

- `p-admin/src/components/rs-form/rs-meta-form.vue`（389 行）—— 多字段表单
- `p-admin/src/components/rs-form/rs-meta-field.vue`（392 行）—— 单字段（按 fieldName 匹配渲染，`wrapForm` 控制是否自动包裹 `<Form>`）
- `p-admin/src/components/rs-form/rs-meta-form.md`（756 行）—— 参考文档，实质是整个表单体系 override 属性、选择器配置、上传配置的"属性全集手册"

**三种数据源模式**（`rs-meta-form.vue:98-124`）：

1. `path` = DataTable 对象（如 `$MAIN`）或数组（内部 `_wrapArrayPath` 包装、`_findDtByData` 反查真实 DataTable）
2. `path` = 路径名字符串 + `storeName`（Vuex 命名空间，缺省从 `inject.aiFormStoreName` 取）
3. `value` = v-model 普通对象，内部 `_createAdapter` 创建 DataTable 适配器（`rs-meta-form.vue:193-229`）

**字段配置加载优先级**：`fields` prop > `resourceName`（initScms + Gen.getFormFields）> `moduleCode`（从 modules[mc].MODPATH 找 PATHNAME=MAIN 的 RESOURCENAME）> `path.scm` 兜底。

**暴露方法**：`valid()` / `getModel()` / `getDataTable()` / `applyFill(fields)`（AI 填充接口）；provide `visibilityHost`、`aiFormModuleCode`、`aiFormStoreName`。

### 10.2 查询面板组件 rs-meta-query-panel / rs-meta-query-panel-field（已落地）

元数据驱动的**查询条件面板**，与 rs-meta-form 同源但面向查询场景（`p-admin/src/components/rs-query-panel/`，panel 274 行 + field 406 行 + md 252 行）。与 rs-meta-form 的差异：

| 特性 | rs-meta-form | rs-meta-query-panel |
|---|---|---|
| 布局 | `<Form>` + `<FormItem>` | `<Row><Cell>` + rr-flex-row |
| 数据绑定 | 实时双向绑定 DataTable | 本地 queryValues 缓存，点查询时同步 |
| 字段过滤 | EDITSORT > 0 | QUERYSORT > 0 |
| 类型推导 | EDITTYPE | QUERYTYPE 优先，回退 EDITTYPE |
| 匹配方式 | 无 | QUERYMODE（like/eq/in/range） |
| 校验 | 必填校验 | 不校验 |

- panel：`path` 指向 **QQRY** DataTable；`moduleCode` 从 MODPATH.QQRY 推导资源名；支持 `overrides`、`cellWidth`（24 栅格）、`showButtons`；事件 query/reset。
- field：**双模式自动检测**——panel 内通过 inject 读写 queryValues；独立使用时可绑 path 或 v-model。支持 daterange、`mode:'in'` 多选、选择器快捷属性。
- 典型用法：替代 generic-module `simple-query` 插槽中手写的查询条件。

### 10.3 字段/列覆盖（overrides）机制（进行中，未提交）

**不改 scm 元数据也能定制字段/列**的兜底手段，覆盖逻辑下沉到基础组件：

- **rs-form-edit**（+86 行，`p-admin/src/components/rs-form/rs-form-edit.vue`）：新增 `fields` / `overrides` props。`_applyOverrides` 深拷贝防污染 scm 缓存，支持快捷属性（label/readonly/required/type/dict/placeholder/single/visibleIf/updateFields）+ 选择器快捷属性（selType/apiCode/module/keyName/titleName/paramMappings/defaultParams，合并进 `cellProps.selConfig` JSON）+ `cellProps`/`formItemProps` 任意子属性透传；watch fields/overrides 变化重新应用。
- **list-t01**（+58 行，`p-admin/src/components/rs-template/list-t01.vue`）：新增 `columnConfig`（直接传入列配置，优先于 path.scm）/ `columnOverrides`（列级覆盖：title/width/minWidth/maxWidth/dict/datas/visibleIf/perCode/type/align/fixed/updateFields/selectData 及任意其他属性）。
- rs-meta-form 内置同名 `_applyOverrides`（`rs-meta-form.vue:272-359`），机制一致。
- 专项文档：`p-admin/src/components/rs-form/rs-form-edit-overrides.md`（192 行，含 SFC 扩展 JS 动态设置示例）。

### 10.4 字典子集（dict + items）端到端能力（已落地）

SELECTDATA 新增 JSON 格式 `{"dict":"D0701","items":["1","2"]}`，实现"同一字典在不同字段显示不同子集"，三处形成闭环：

1. **配置端**：s01/m01 `uiSetFull.vue`（+178 行）——选字典后出现"数据范围"多选，勾选子集存为上述 JSON，留空回退纯字典名；自定义数据改为弹窗按行编辑 key/title（兼容解析旧 `k:v` 文本与 JSON 两种格式）。
2. **渲染端**：`src/utils/gen.js`（+17 行）——getFormFields 解析 `{dict, items}` 时用 `heyui.getDict` 取字典并按 items 过滤生成 `cellProps.datas`；items 为空回退 `cellProps.dict`。
3. **覆盖端**：rs-meta-form / rs-form-edit 的 overrides 写 `{ dict: 'D0701', items: [...] }` 时从 Vuex 字典过滤生成 datas（`rs-meta-form.vue:311-323`）。

### 10.5 模板与规范文档（进行中，未提交）

三份文档是**面向 AI 代码生成 + 开发者**的知识库，与 `constants/sfc-templates.js`（编辑器内置起手代码常量）用途不同、内容呼应：

| 文档 | 行数 | 内容 |
|---|---|---|
| `components/generic-module/standard-templates.md` | 1164 | 总规范：**模式决策树**（A 标准 CRUD / B 审批流 CRUD / C 自定义 Controller / D 报表页 / E 选择页 / F 整页 SFC / A+ 主子表 / G 弹窗内嵌表单）+ 各模式 m18 配置与 main.js/add.js/store.js 全文模板 + 11 类常用代码块库 + **AI 提示词模板** + EDITTYPE/PAGETYPE/SFC Slot 清单 + 调试检查清单 |
| `components/generic-module/sfc-extend-templates.md` | 871 | SFC 扩展编码模板库：扩展 JS 骨架（含完整 this 上下文清单）、Store 扩展、查询面板 SFC Slot（以 rs-meta-query-panel 为标准写法）、表单字段 Slot、按钮钩子、常用场景（openPage/openSelector/子表增删行）、PAGECONFIG |
| `components/rs-form/rs-form-edit-overrides.md` | 192 | overrides / columnOverrides 专项说明（§10.3 的文档化） |

### 10.6 存量业务模块迁移 GenericModule 计划（规划中，未提交）

14 份 `migrate-to-generic.md`（r01/r02 各模块目录 + 两份域总览），目标：把存量"四件套"（router.js/store.js/main.vue/add.vue）业务模块迁移到元数据引擎。**总原则**：代码资产经 m17 在线开发创建、存 `tss_code_asset`（路径 `@/modules/{moduleCode}/`）；页面/按钮/字段经 m18 可视化配置；菜单 OUTERURL 改为 `/g/{moduleCode}/{pageCode}`。

**r01 域**（`r01/migrate-to-generic.md`）：

| 模块 | moduleCode | 方案 | 难度 |
|---|---|---|---|
| m01 项目管理 | LI_M01 | 标准配置 + EXTENDJS（字段全走 m18 uiSetFull，~280 行→~120 行） | 中等 |
| m02 原始记录 | LI_M02 | 整页 SFC 重写（系统最核心模块，5484 行 → 8 个 SFC 资产 + 30+ actions） | 极复杂 |
| m025 委托审核 | LI_M02 | 整页 SFC（审核工作台：三栏分屏 + 14 项 AI 检查清单 + OnlyOffice 预览） | 极复杂 |
| m026 委托审批 | LI_M02 | 整页 SFC（**复用 m025 的 review.vue**，传 mode='verify'） | 极复杂 |
| m03 费用管理 | LI_M03 | 标准配置 + EXTENDJS（用 list-t01 columnOverrides 做金额格式化） | 简单 |
| m031 费用汇总 | LI_M031 | 列表整页 SFC（树形级联不强行配置化）+ 表单配置化 | 复杂 |
| m05 受理单 | LI_M00 | 标准配置 + EXTENDJS + SFC Slot（**直接用 rs-meta-query-panel 替代手写查询面板**） | 复杂 |
| m06 委托管理 | LI_M06 | 标准配置 + EXTENDJS + 3 类 SFC Slot（Excel 导入依赖 xlsx 为风险点） | 中等 |

**r02 域**（`r02/migrate-to-generic.md`）：m01 检测统计 / m02 人员效能 / m03 客户统计 → `PAGETYPE=report`（列由 scm LISTSORT 生成、图表由 PAGECONFIG.REPORT.CHART 描述）；m07 物流管理 → 标准配置 + EXTENDJS（multiautocomplete 子表绑定配置化）；**m05 废弃删除**。

### 10.7 杂项

- 页面组件 `name` 属性统一为 `模块前缀-页面标识-main` 格式（提交 869577b，影响 keep-alive 缓存 key 约定）；s01/m18 路由移除多余 notCache 元信息。

### 10.8 增量主线小结

这批新增是一套组合拳，服务于同一目标——**把存量业务模块迁移到 generic-module 元数据引擎，让"配置优先、覆盖兜底、SFC 兜底复杂场景"成为标准路径**：standard-templates.md 是总规范；migrate-to-generic.md 系列（14 份）是规范在 12 个模块上的应用评估；rs-meta-form/query-panel 四组件是迁移配套基础设施（多份方案直接引用）；overrides/columnOverrides 是"不改元数据也能定制"的兜底；字典子集能力打通配置端/渲染端/覆盖端。后续可将 rs-meta-* 组件与 overrides 规范沉淀进 AI 记忆中枢（tss_ai_memory）与 standard-templates.md 的 AI 提示词模板，供 SFC/向导场景复用。
