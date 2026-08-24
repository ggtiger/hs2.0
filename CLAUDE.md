# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

华溯计量管理系统 (hs2.0) — 面向计量检测/校准行业的 LIMS 系统，包含两个独立子项目：

- `netcore/` — 后端 API 服务（.NET Core 2.2 + Dapper + 自研ORM）
- `p-admin/` — 前端管理界面（Vue 2 + HeyUI + Webpack 3）

## 常用命令

### 后端 (netcore/)

```bash
# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行主 API（端口 5001）
dotnet run --project Realso.WebAPI

# 运行认证服务（端口 5000/5003）
dotnet run --project Realso.Auth

# 运行测试
dotnet test
```

### 前端 (p-admin/)

```bash
# 安装依赖
npm install

# 开发服务器（端口 8089，自动打开浏览器）
npm run dev

# 生产构建（输出到 dist/）
npm run build

# ESLint 检查
npm run lint

# 单元测试
npm run unit
```

## 后端架构

### 元数据驱动架构

核心设计：数据模型不通过代码实体类定义，而是存储在数据库元数据表中。系统运行时从这些表读取元数据，动态构建 SQL 和执行数据操作。新增业务功能只需配置元数据 + 重写少量代码。

#### 元数据表及其职责

| 表 | 职责 | 配置内容 |
|---|------|---------|
| TSS_RESOURCE | 资源/表定义 | RESOURCENAME, TABLENAME, RESOURCETYPE(TABLE/DATAVIEW/SQL) |
| TSS_RESFIELD | 字段定义 | FIELDNAME, FIELDTYPE, ISKEY, REFRESOURCEID(引用), UPFIELDID(上级), VFORMAT(单据号格式) |
| TSS_RESFILTER | 过滤器定义 | FILTERCODE, FILTERSQL(NVelocity模板), ORDERBY |
| TSS_RESUIPC | UI页面配置 | LABELNAME, EDITTYPE, LISTSORT, QUERYSORT, EDITSORT, SELECTDATA |
| TSS_MOUDLE | 模块定义 | MODULECODE, MODULENAME |
| TSS_MOUDLEPATH | 模块数据源 | PATHNAME(QRY/QQRY/MAIN/DTS), RESOURCEID |
| TSS_MOUDLEPATHREL | 数据源主外键关系 | 主表字段→子表字段映射 |
| TSS_MOUDLEAPI | 模块接口 | APICODE, APITYPE, PATHNAME, FILTERCODE, APIPARAM, BEFOREAPICODE, AFTERAPICODE |
| TSS_SQL | SQL模板 | SQLCODE, SQLTXT(NVelocity模板) |
| TSS_FUNC | 菜单 | FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID |
| TSS_FUNCPOINT | 功能点(权限) | FUNCPOINTCODE, FUNCPOINTNAME |

#### 资源命名规则

- TBS_xxx — 物理表定义
- VCK_xxx — 业务视图(DATAVIEW)，前端列表/表单使用
- VBS_xxx — 基础数据视图(选择器)
- VRP_xxx — 报表视图(SQL类型)
- VSS_xxx — 系统管理视图

#### ORM 数据流（完整链路）

```
前端 DataTable.getXML()
  → POST api/Data/call/{ModuleName}/{ApiCode}
  → DataController.Call
  → MOUDLE.Open(ModuleName) 加载模块配置
  → MD.GetAPI(ApiCode) 获取接口配置
  → switch(APITYPE) 路由到 doQuery/doOpen/doSave/doDelete/...
  → SchemaManage.GetResource(resourceName) 加载元数据
  → BuildSQL01.BuildQuery/BuildInsert/BuildUpdate/BuildDelete 构建SQL
  → ViewOperate01 执行SQL (事务)
  → 返回 QueryResult/DataView/void
```

#### 字段定义（tss_resfield）注册规范

新增业务模块时，字段定义必须遵循以下规则：

1. **字段名不能有下划线**：数据库字段名使用大写无下划线格式（如 `FORMULANAME`、`FORMULACODE`、`ISDELETED`），与系统现有表（如 `tbs_cust` 的 `CUSTCODE`、`CUSTNAME`）保持一致
2. **物理表（TBS）必须注册字段**：在 `tss_resfield` 中为物理表资源插入所有字段定义
3. **DATAVIEW（VCK/VSS）必须注册字段**：在 `tss_resfield` 中为 DATAVIEW 资源插入所有字段定义
4. **DATAVIEW 字段必须通过 REFFIELDID 关联物理表字段**：DATAVIEW 的每个字段的 `REFFIELDID` 必须指向物理表对应字段的 ID，不能为 NULL

示例（ID 字段）：
```sql
-- 物理表字段
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, ...)
SELECT 'rf_fmp_id', 'tbs_xxx_001', NULL, 'ID', ...  -- 物理表字段 REFFIELDID 为 NULL
FROM DUAL WHERE NOT EXISTS (...);

-- DATAVIEW 字段（REFFIELDID 指向物理表字段 ID）
INSERT INTO tss_resfield (ID, RESOURCEID, REFFIELDID, FIELDNAME, ...)
SELECT 'rf_fm_id', 'vck_xxx_001', 'rf_fmp_id', 'ID', ...  -- REFFIELDID = 物理表字段ID
FROM DUAL WHERE NOT EXISTS (...);
```

#### 引用字段（REFRESOURCEID）的 JOIN 机制

字段定义中 REFRESOURCEID 指向另一个资源，ORM 自动构建 LEFT JOIN：
- TABLE/DATAVIEW 类型：`LEFT JOIN {引用表名} {别名} ON {REFRELATION}`
- SQL 类型：`LEFT JOIN ({SQLCODE}) {别名} ON {REFRELATION}`
- 有 UPFIELDID 的字段是子引用（外键通过父字段间接关联）

#### 过滤器 FILTERSQL 模板语法（NVelocity）

```sql
-- 基本条件
A.STATE IN(2)

-- 模板条件 (#if 判断，空值不生效)
#if("$!{CUSTNAME}"!="")
AND A.CUSTNAME LIKE CONCAT('%',@CUSTNAME,'%')
#end

-- 系统变量 (自动注入)
@_USERID_  — 当前登录用户ID
@_EMPID_   — 当前员工ID
@_DEPTID_  — 当前部门ID

-- 日期范围
#if("$!{BILLDATE_start}"!="")
AND A.BILLDATE>=str_to_date(@BILLDATE_start,'%Y-%m-%d')
#end
```

#### FILTERCODE 编号规范

| 编码 | 用途 | 说明 |
|-----|------|------|
| F00 | 单条查询 | `A.ID = @ID` |
| F01 | 列表查询(模糊搜索) | INPUT参数多字段模糊匹配 + 数据权限 |
| F02 | 高级查询(多条件) | NVelocity模板条件组合 |
| F03+ | 专用/批量操作 | `A.ID IN @ID AND A.STATE IN (...)` |
| F011/F012 | 审核/审批列表查询 | 加 CHECKID/VERIFYID=当前用户 |
| F021/F022 | 审核/审批高级查询 | 加 CHECKID/VERIFYID=当前用户 |

#### 前后端数据传输格式（XML）

前端 `DataTable.getXML()` 生成 XML，后端 `ViewOperate01.FillData()` 解析：
```xml
<表名 l="u" c="字段列表" t="类型列表">
  <a>  <!-- 新增行 -->
    <r c0="值" c1="值" .../>
  </a>
  <m>  <!-- 修改行（含旧值 oc0, oc1...） -->
    <r c0="新值" oc0="旧值" .../>
  </m>
  <d>  <!-- 删除行（仅旧值） -->
    <r oc0="旧值" oc1="旧值" .../>
  </d>
</表名>
```

#### 新增业务模块的开发模式

开发一个新业务模块（如物流管理 r02/m07）需要以下步骤：

**第一步：数据库元数据配置**
1. `CREATE TABLE` 创建物理表（如 TSS_LOGISTICS）
2. `tss_resource` 插入物理表定义（TBS_xxx, RESOURCETYPE=TABLE）
3. `tss_resource` 插入视图定义（VCK_xxx, RESOURCETYPE=DATAVIEW, TABLERESOURCEID 指向物理表）
4. `tss_resfield` 插入字段定义（ISKEY=1 的字段设 KEYGENTYPE=GUID）
5. `tss_resfilter` 插入过滤器（F00单条/F01列表/F02高级查询）
6. `tss_resuipc` 插入 UI 配置（LISTSORT列显示/QUERYSORT查询/EDITSORT编辑）
7. `tss_moudle` 注册模块（MODULECODE）
8. `tss_moudlepath` 配置数据源（QRY/QQRY/SEL/MAIN/DTS）
9. `tss_moudleapi` 配置 API（A01query/A02open/A04save/A07delete）
10. `tss_func` 新增菜单项
11. `tss_funcpoint` 配置功能点权限

**第二步：前端页面开发**
1. 创建 `src/pages/{业务代码}/{模块编号}/` 目录
2. `router.js` — 路由定义，必须使用 `require.ensure` 懒加载，禁止同步 import
3. `store.js` — 通过 `createStore.getStore({ config, storeName, actions })` 注册
4. `views/main.vue` — 列表页面，使用 `RsTableList` 组件渲染
5. `views/add.vue` — 新增/编辑页面

**第三步：后端扩展（仅自定义操作需要）**
1. 创建 `Controllers/RMxxController.cs` 继承 `DataController`
2. 重写 `doMyApi`，switch(APICODE) 处理自定义接口
3. 在 `Startup.cs` 注册路由

#### 前端 Store 初始化依赖顺序

```
1. app store initModule → 加载 RS_M00 模块配置（用户/菜单/字典）
2. 页面路由 → initModule → 加载业务模块配置（如 LI_M02）
3. router.js 懒加载 → chunk 执行 → store.js 运行
4. createStore.getStore → 从 app store 读模块配置 → 注册 Vuex 模块
```

**重要**：store.js 中不能使用 `SelStore`，因为 SelStore 构造函数依赖 `Store.state['app'].modules['RS_M00']`，若 chunk 在 app store 加载前执行会报 `Cannot read properties of undefined (reading 'MODPATH')`。路由的同步 import 也会导致同样问题。

#### 审批流程

内置单据状态流转引擎（BillState + BillFlow），支持：待提交 → 待审核 → 待审批 → 已审批，以及驳回和撤销操作。

单据状态码：
| 状态码 | 含义 |
|-------|------|
| 1 | 待提交 |
| 2 | 待审核(复核) |
| 5 | 待审批 |
| 6 | 已审批 |
| 10 | 已签发 |
| 12 | 已驳回 |

对应 APICODE：A17(提交), A12(复核), A14(审批), A16(驳回), A13(撤销复核), A15(撤销审批)

### 项目分层

```
Realso.Auth          — IdentityServer4 认证服务（JWT Bearer Token）
Realso.WebAPI        — 应用层（Controllers + Models），主入口
Realso.Core          — 框架基类（BaseControl, BaseModel, BaseService）
Realso.Data.ORM      — 自研ORM实现（SchemaManage 读元数据, BuildSQL01 构建SQL, ViewOperate01 执行操作）
Realso.Data.ORM.Core — ORM核心定义（Resource, DataView, ViewRow）
Realso.Data.DBAccess — Dapper 封装（DBHelper, DB 连接工厂）
Realso.Utils         — 工具类
```

### 核心数据流

前端所有数据操作通过统一入口 `POST api/Data/call/{ModuleName}/{ApiCode}`，DataController.Call 根据 API 配置的 APITYPE 路由到对应方法：

- **query** — 列表查询（支持分页、导出）
- **open** — 打开单条数据（含关联子表）
- **save** — 保存（新增/修改，含单据号生成、主外键处理）
- **delete** — 删除
- **submit/reSubmit** — 提交/重新提交
- **check/reCheck** — 审核/撤销审核
- **verify/reVerify** — 审批/撤销审批
- **batchXxx** — 批量操作
- **自定义** — 子类重写 `doMyApi` 扩展

### 审批流程

内置单据状态流转引擎（BillState + BillFlow），支持：待提交 → 待审核 → 待审批 → 已审批，以及驳回和撤销操作。

### 数据层

- 不使用 Entity Framework，使用 Dapper 1.50.5 + 自研 ORM
- DataView 类似 DataSet，包含 Inserted/Updated/Deleted 集合，支持行状态追踪
- SchemaManage.GetResource(resourceName) 读取元数据（每次调用都查数据库，无缓存）
- BuildSQL01 根据 Resource + ResourceFilter + ResourceField 动态构建 SQL
  - BuildQuery：自动 LEFT JOIN 引用字段，NVelocity 解析过滤器，MySQL LIMIT 分页
  - BuildInsert/BuildBatchInsert/BuildUpdate/BuildDelete：参数化或拼接式 SQL
- ViewOperate01 执行数据操作（事务），FillData 解析前端 XML
- SQLManage 从 `VSS_sQL` 表获取预定义 SQL，支持 NVelocity 模板参数化
- 数据库：MySQL 5.7

### 响应格式

```json
{ "Code": 200, "Data": {...}, "Message": "" }
```

Code=200 成功，Code=501 登录超时，Code=500 内部错误。

## 前端架构

### 技术栈

Vue 2.5 + Vue Router 3 + Vuex 3 + HeyUI 1.25 + Axios 0.18 + Webpack 3 + Less + ECharts 4

### 前端数据流强制规范（必读）

> 详细规范、模板代码、白名单见 `p-admin/docs/frontend-store-convention.md`。
> 完整违规清单见 `p-admin/docs/frontend-refactor-todo.md`（按 P0/P1/P2 分批迁移）。

`p-admin/` 前端代码必须遵守四条强制规则：

1. **接口调用必须通过 Store action**。禁止在 `.vue` 中 `import db from '@/api/db'` 后调 `db.postData/call/open/openTables/getNewID`。`@/api/db` 是 Store 层私有依赖。
2. **业务数据存放在 DataTable**（由 `BaseStore.mixState()` 在 `dt.{path}` 创建），通过 `setValue/getValue/getXML` 读写。
3. **模板双向绑定用 `mapDateTable`**：`...mapDateTable('MAIN', ['F1','F2'])`，由 `BaseStore.mapGetters` 生成 get/set。
4. **页面调 Store action 用 `this.$callAction({action, param, ...})`**（见 `src/utils/extends.js:72`），禁止 `this.$store.dispatch(...)`。

**已配置 ESLint 规则**（`.eslintrc.js`，先 `warn` 评估）：
- `no-restricted-imports`：禁止 `.vue` import `@/api/db`
- `no-restricted-syntax`：禁止 `this.$store.dispatch(...)`

**规范参考实现**：
- `$callAction` 定义：`p-admin/src/utils/extends.js:72`
- `createStore.getStore`：`p-admin/src/store/createStore.js`
- `Store03.mixActions`：`p-admin/src/store/Store03.js`
- 规范列表页：`p-admin/src/pages/b01/m01/views/main.vue`
- 规范表单页：`p-admin/src/pages/b01/m01/views/add.vue`
- 规范 store.js：`p-admin/src/pages/b01/m01/store.js`

**豁免白名单**（专用 endpoint 组件 + Store 框架自身）：`rs-uploader/*`、`rs-uploader-template/*`、`rs-onlyoffice-preview/*`、`edit/ueditor/*`、`rs-word-template-editor/*`、`src/store/*`、`src/api/*`、`src/utils/extends.js`。

### 模块化页面结构

每个业务模块遵循标准结构：

```
src/pages/{业务代码}/{模块编号}/
  index.js      — 模块入口
  router.js     — 路由定义
  store.js      — Vuex Store 定义
  views/
    main.vue    — 列表主页面
    add.vue     — 新增/编辑页面
```

业务代码：`b01`(基础数据)、`s01`(系统管理)、`r01`/`r02`(报告/记录)、`cgdd`(采购订单)

### 路由自动加载

使用 webpack `require.context` 自动扫描 `src/pages/*/router.js`，无需手动注册路由。

### Vuex 动态模块注册

- 全局模块：`app`（菜单/字典/权限）、`user`（登录/token），持久化到 sessionStorage
- 页面模块：通过 `createStore.getStore()` 动态注册，命名空间如 `b01/m01`
- Store 继承链：BaseStore → Store03（CRUD 操作）→ 各模块 Store
- SelStore 提供通用下拉选择器数据（部门/员工/客户/标准等）

**Store03 核心 Actions**：
- `query` — 列表查询，从 API 配置取 APICODE/PATHNAME/APIPARAM/FILTERCODE
- `open` — 按 ID 查询单条 + 子表
- `save` — DataTable.getXML() → POST 后端，回写结果
- `call` — 通用调用，POST `{apiPath}/{moduleCode}/{APICODE}/`
- `batch` — 批量操作，传 ID 列表，回写 updateFields
- `flowSave` — 审批流操作（ID + ACTIONCODE）

**DataTable 核心方法**：
- `getXML()` — 将增/改/删行序列化为 XML 字符串（与后端 FillData 解析格式对应）
- `setValue/getValue` — 按字段名读写行数据
- `getFields` — 返回字段列表

### API 层 (src/api/db.js)

- `getUrl(type)` — 获取 API 基础地址（type: 'url'|'user'|'upload'|'pdf'|'pdfsy'|'socket'）
- `postData(param, type)` — 核心请求方法，自动注入 token 和 userInfo
- `open(params)` — 通用查询
- `openTables(paths)` — 批量多表查询
- `call(para)` — 通用调用（CRUD 操作）
- `getNewID(scmName, inc)` — 获取新 ID

**注意**：API 地址在 `db.js` 的 `getUrl()` 中硬编码，切换环境需修改此函数。

### 后端 Controller 扩展模式

子控制器继承 DataController，重写 `doMyApi` 处理自定义 APITYPE：

```csharp
// 例: Realso.WebAPI/Controllers/RM11Controller.cs
public class RM11Controller : DataController {
  public override IActionResult doMyApi(string APICODE, ...) {
    switch (APICODE) {
      case "A51": return doPreviewCert(...);  // 证书预览
      case "A55": return doSignECert(...);     // 电子签发
      case "A56": return doFieldModify(...);  // 字段修改
      case "A57": return doAnomalyCheck(...);  // 异常检测
      default: return base.doMyApi(APICODE, ...);
    }
  }
}
```

路由注册在 `Startup.cs`：
```csharp
services.AddMvc().AddControllersAsServices();
```

### 全局组件 (rs- 前缀)

RsTableList/RsTableEdit（表格）、RsFormEdit（表单）、RsModal（弹窗）、ListT01/ListT02（列表模板）、AddT01（新增模板）、ReportT01（报表模板）

### Vue 原型方法 (utils/extends.js)

- `this.$callAction()` — 调用 Vuex action
- `this.$alert()` / `this.$error()` / `this.$confirm()` — 消息提示
- `this.$busy()` / `this.$free()` — 加载状态
- `v-per` 指令 — 权限控制
- `v-loadmore` 指令 — 虚拟滚动

## 代码风格

### 前端

- ESLint: standard 规则 + vue/essential
- 分号必填，单引号，2 空格缩进，函数括号前无空格
- 字符串使用单引号

### 后端

- C# 2 空格缩进
- JSON 序列化：不使用驼峰（DefaultContractResolver），忽略 Null 值，忽略循环引用
