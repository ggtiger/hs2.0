# LIMS系统升级改造实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 睿谱希管理系统 11 项升级需求全部实施，包括 UI 重构、审核分屏、异常检测、电子证书、物流管理等。

**Architecture:** 在现有 Vue 2 + HeyUI + Webpack 3 前端和 .NET Core 2.2 + Dapper + 自研ORM 后端上改造。遵循现有元数据驱动架构和模块化页面结构（router.js + store.js + views/）。新增功能通过 DataController.doMyApi 扩展点和标准 CRUD 模式实现。

**Tech Stack:** Vue 2.5 / HeyUI 1.25 / Vuex 3 / Axios / .NET Core 2.2 / Dapper / MySQL 5.7 / PDF.js / Less

**Spec:** `docs/superpowers/specs/2026-05-29-lims-upgrade-design.md`

---

## File Structure

### 新建文件

| 文件 | 职责 |
|------|------|
| `p-admin/src/theme/modern.less` | 现代主题覆盖 HeyUI 默认变量 |
| `p-admin/src/pages/r01/m025/router.js` | 委托审核路由 |
| `p-admin/src/pages/r01/m025/store.js` | 委托审核 Store |
| `p-admin/src/pages/r01/m025/index.js` | 委托审核入口 |
| `p-admin/src/pages/r01/m025/views/main.vue` | 委托审核入口页 |
| `p-admin/src/pages/r01/m025/views/review.vue` | 审核分屏页 |
| `p-admin/src/pages/r01/m026/router.js` | 委托审批路由 |
| `p-admin/src/pages/r01/m026/store.js` | 委托审批 Store |
| `p-admin/src/pages/r01/m026/index.js` | 委托审批入口 |
| `p-admin/src/pages/r01/m026/views/main.vue` | 委托审批入口页 |
| `p-admin/src/pages/r01/m026/views/review.vue` | 审批分屏页 |
| `p-admin/src/pages/r02/m07/` | 物流管理模块（router+store+index+views） |
| `p-admin/src/pages/out/logistics/` | 客户物流查询页 |
| `p-admin/src/pages/out/ecert/` | 客户电子证书下载页 |
| `p-admin/src/components/scroll-notice/` | 公告滚动组件 |

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `p-admin/src/pages/r01/m01/views/add.vue:70` | 删除按钮添加 Poptip 确认框 |
| `p-admin/src/pages/r01/m02/views/add.vue:53-62` | 审批记录隐藏 + Drawer 组件 |
| `p-admin/src/pages/main/wodezhuye.vue:54-67` | 公告区改为滚动组件 |
| `p-admin/src/components/main.vue` | router-view 包裹 keep-alive |
| `p-admin/src/components/printPdf/index.vue` | PDF.js canvas 渲染替换 iframe |
| `p-admin/src/assets/style.css` | 全局样式调整 |
| `netcore/Realso.WebAPI/Controllers/RM11Controller.cs` | 新增 doCheckAnomaly / doFieldModify / doECertSign |
| `netcore/Realso.WebAPI/Controllers/FileController.cs:128-157` | PDF 缓存检查优化 |

### 数据库变更

| 表 | 操作 |
|----|------|
| `TSS_CALIBRATION_RULES` | 新建：校准规则配置 |
| `TSS_PROJECT_FEE` | 新建：项目费用标准 |
| `TSS_LOGISTICS` | 新建：物流主表 |
| `TSS_LOGISTICS_NODE` | 新建：物流节点表 |

---

## Chunk 1: 基础修复 + 界面重构（需求1/3/6）

### Task 1.1: 修复 m01 删除按钮缺少确认弹窗

**Files:**
- Modify: `p-admin/src/pages/r01/m01/views/add.vue:70`

- [ ] **Step 1: 给删除按钮添加 Poptip 确认框**

在 `add.vue` 第70行，将：

```html
<Button class="ml5" v-if="ISSHOWDELETE" color="red" @click.native="del">删除</Button>
```

替换为：

```html
<Poptip content="确定删除？" v-if="ISSHOWDELETE" @confirm="del">
  <Button class="ml5" color="red">删除</Button>
</Poptip>
```

- [ ] **Step 2: 验证**

运行: `cd p-admin && npm run dev`
预期: 打开项目管理页面，编辑一条记录，删除按钮显示红色，点击后弹出确认框

- [ ] **Step 3: Commit**

```bash
git add p-admin/src/pages/r01/m01/views/add.vue
git commit -m "fix: 给项目管理删除按钮添加确认弹窗，防止误删"
```

---

### Task 1.2: 后端增加原始记录唯一性校验（防重复）

**Files:**
- Modify: `netcore/Realso.WebAPI/Controllers/RM11Controller.cs`

- [ ] **Step 1: 在 doSave/doBatchSubmit 中增加重复校验**

在 `RM11Controller.cs` 的 `doSave` 方法开头，加载数据后增加唯一性校验：

```csharp
// 在 _doSave 方法中，view.InitData 之后、saveList.Add 之前
string ARDCODE = view.GetValue("ARDCODE") + "";  // 设备编号
string REFTPMID = view.GetValue("REFTPMID") + "";  // 模板ID
string WTID = view.GetValue("WTID") + "";  // 委托单ID
string currentID = view.GetValue("ID") + "";

if (!string.IsNullOrEmpty(ARDCODE) && !string.IsNullOrEmpty(REFTPMID))
{
    QueryInfo dupCheck = new QueryInfo();
    dupCheck.OtherWhere = "ARDCODE=@ARDCODE AND REFTPMID=@REFTPMID AND WTID=@WTID AND STATE!=4";
    dupCheck.FilterParams["ARDCODE"] = ARDCODE;
    dupCheck.FilterParams["REFTPMID"] = REFTPMID;
    dupCheck.FilterParams["WTID"] = WTID;
    if (!string.IsNullOrEmpty(currentID))
    {
        dupCheck.OtherWhere += " AND ID!=@ID";
        dupCheck.FilterParams["ID"] = currentID;
    }
    BaseModel dupModel = GetModel(MAINPATH, RESOURCEID);
    QueryResult dupResult = dupModel.Query(dupCheck);
    if (int.Parse(dupResult.TotalCount) > 0)
    {
        responseModel.SetError("存在重复记录：相同委托单下该设备已使用相同模板创建记录！");
        return;
    }
}
```

注意：字段名（ARDCODE/REFTPMID/WTID）需根据实际元数据表字段确认。具体字段名从 `TSS_RESFIELD` 表中查询对应资源(RESOURCEID)的字段定义。

- [ ] **Step 2: 验证**

运行: `cd netcore && dotnet build`
预期: 编译通过无错误

- [ ] **Step 3: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/RM11Controller.cs
git commit -m "fix: 后端增加原始记录唯一性校验，防止同一委托单下重复创建记录"
```

---

### Task 1.3: 创建现代主题文件

**Files:**
- Create: `p-admin/src/theme/modern.less`

- [ ] **Step 1: 创建主题文件**

创建 `p-admin/src/theme/modern.less`，覆盖 HeyUI 默认变量：

```less
// 现代主题 - 覆盖 HeyUI 默认变量
@primary-color: #2B7A78;        // 青蓝色主色
@primary-color-light: #3AAFA9;
@primary-color-dark: #1A535C;
@link-color: @primary-color;

// 圆角
@border-radius-base: 6px;
@border-radius-small: 4px;

// 阴影
@shadow-base: 0 2px 8px rgba(0, 0, 0, 0.08);
@shadow-card: 0 1px 4px rgba(0, 0, 0, 0.06);
@shadow-hover: 0 4px 16px rgba(0, 0, 0, 0.12);

// 字体
@font-size-base: 14px;
@font-size-small: 12px;
@font-size-large: 16px;

// 间距
@padding-base: 12px;
@padding-large: 20px;

// 表格优化
@table-border-color: #e8e8e8;
@table-header-bg: #f5f7fa;
@table-row-hover-bg: #e6f7ff;

// 按钮优化
@btn-border-radius: @border-radius-base;
@btn-padding-base: 5px 16px;

// 卡片
@card-bg: #ffffff;
@card-border-radius: 8px;
@card-padding: 16px;
```

- [ ] **Step 2: 在 main.js 中引入主题**

在 `p-admin/src/main.js` 中，在现有样式 import 之后添加：

```javascript
import './theme/modern.less'
```

- [ ] **Step 3: 验证**

运行: `cd p-admin && npm run dev`
预期: 页面主色调变为青蓝色，按钮圆角增大，表格有淡色 hover 效果

- [ ] **Step 4: Commit**

```bash
git add p-admin/src/theme/modern.less p-admin/src/main.js
git commit -m "feat: 新增现代主题文件，统一视觉风格"
```

---

### Task 1.4: PDF缓存优化

**Files:**
- Modify: `netcore/Realso.WebAPI/Controllers/FileController.cs:128-157`

- [ ] **Step 1: 优化 DownLoadPdf 方法，优先返回已缓存PDF**

将 `DownLoadPdf` 方法改为：

```csharp
[HttpGet("pdf/{id}")]
[EnableCors("AllowHeaders")]
public IActionResult DownLoadPdf(string Id)
{
  Hashtable Params = new Hashtable();
  Params["FILTERCODE"] = "F00";
  Hashtable FilterParams = new Hashtable();
  FilterParams["ID"] = Id;
  Params["FilterParams"] = FilterParams;
  BaseModel MAIN = GetModel("", "VSS_FILES");
  MAIN.Open(GetQueryInfo(Params));
  if (MAIN.GetView().Count > 0)
  {
    ViewRow row = MAIN.GetView()[0];
    string FILENAME = row.GetString("FILENAME");
    string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
    string FilePath = rootPath + row.GetString("FILEPATH");

    string pdfPath = FilePath.Replace(".docx", ".pdf");

    // 优先使用已缓存的PDF
    if (System.IO.File.Exists(pdfPath))
    {
      return new PhysicalFileResult(pdfPath, "application/pdf");
    }

    // PDF不存在，触发转换
    if (System.IO.File.Exists(FilePath) && Path.GetExtension(FILENAME) == ".docx")
    {
      Realso.Utils.MySocket.Send("127.0.0.1", 5555, FilePath);
      // 等待PDF生成（最多5秒）
      for (int i = 0; i < 50; i++)
      {
        System.Threading.Thread.Sleep(100);
        if (System.IO.File.Exists(pdfPath)) break;
      }
    }

    if (System.IO.File.Exists(pdfPath))
    {
      return new PhysicalFileResult(pdfPath, "application/pdf");
    }

    return NotFound(new { Message = "PDF文件生成中，请稍后重试" });
  }
  return NotFound();
}
```

- [ ] **Step 2: 验证**

运行: `cd netcore && dotnet build`
预期: 编译通过。首次请求触发转换，后续请求直接返回缓存PDF

- [ ] **Step 3: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/FileController.cs
git commit -m "perf: PDF预览优先返回缓存文件，避免重复转换"
```

---

### Task 1.5: 前端 keep-alive 缓存

**Files:**
- Modify: `p-admin/src/components/main.vue`

- [ ] **Step 1: 在 router-view 外包裹 keep-alive**

找到 `main.vue` 中的 `<router-view>` 标签，改为：

```html
<keep-alive :include="cachedViews">
  <router-view></router-view>
</keep-alive>
```

在组件 data 中添加：

```javascript
data() {
  return {
    cachedViews: ['r01-m021-main1', 'r01-m022-main2', 'r01-m025-main', 'r01-m026-main'],
    // ... 其他现有数据
  }
}
```

注意：keep-alive 的 include 匹配的是组件的 name 属性，需确认各业务页面组件的 name 是否一致。

- [ ] **Step 2: 验证**

运行: `cd p-admin && npm run dev`
预期: 在不同Tab间切换时，之前打开的页面不重新加载数据

- [ ] **Step 3: Commit**

```bash
git add p-admin/src/components/main.vue
git commit -m "perf: 为业务页面添加keep-alive缓存，提升页面切换速度"
```

---

## Chunk 2: 核心流程改造（需求2/4/5/7/8）

### Task 2.1: 首页公告滚动播放

**Files:**
- Modify: `p-admin/src/pages/main/wodezhuye.vue:54-67`

- [ ] **Step 1: 创建公告滚动组件**

创建 `p-admin/src/components/scroll-notice/index.vue`：

```vue
<template>
  <div class="scroll-notice" @mouseenter="paused=true" @mouseleave="paused=false">
    <div class="scroll-notice-slogan" v-if="slogan">
      <i class="h-icon-bell"></i> {{slogan}}
    </div>
    <div class="scroll-notice-wrapper" :style="{height: height}">
      <ul class="scroll-notice-list" :class="{'is-paused': paused}">
        <li v-for="(item, index) in list" :key="index" class="scroll-notice-item" @click="$emit('click', item)">
          <span class="scroll-notice-badge">{{index + 1}}</span>
          <span class="scroll-notice-title">{{item.NOTITLE}}</span>
          <span class="scroll-notice-date">{{item.BILLDATE}}</span>
        </li>
        <!-- 复制一份用于无缝滚动 -->
        <li v-for="(item, index) in list" :key="'dup-'+index" class="scroll-notice-item" @click="$emit('click', item)">
          <span class="scroll-notice-badge">{{index + 1}}</span>
          <span class="scroll-notice-title">{{item.NOTITLE}}</span>
          <span class="scroll-notice-date">{{item.BILLDATE}}</span>
        </li>
      </ul>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    list: { type: Array, default: () => [] },
    height: { type: String, default: '280px' },
    slogan: { type: String, default: '' }
  },
  data() {
    return { paused: false };
  }
};
</script>

<style lang="less" scoped>
.scroll-notice {
  &-slogan {
    text-align: center;
    padding: 8px 0;
    font-weight: bold;
    color: #2B7A78;
    font-size: 15px;
    border-bottom: 1px solid #f0f0f0;
    margin-bottom: 8px;
  }
  &-wrapper {
    overflow: hidden;
  }
  &-list {
    animation: scrollUp 20s linear infinite;
    &.is-paused {
      animation-play-state: paused;
    }
  }
  &-item {
    display: flex;
    align-items: center;
    padding: 8px 0;
    cursor: pointer;
    &:hover .scroll-notice-title {
      color: #2B7A78;
    }
  }
  &-badge {
    width: 22px; height: 22px;
    border-radius: 3px;
    line-height: 22px;
    text-align: center;
    color: #fff;
    background: #2B7A78;
    margin-right: 10px;
    flex-shrink: 0;
    font-size: 12px;
  }
  &-title {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #666;
  }
  &-date {
    color: #999;
    font-size: 12px;
    margin-left: 10px;
    flex-shrink: 0;
  }
}
@keyframes scrollUp {
  0% { transform: translateY(0); }
  100% { transform: translateY(-50%); }
}
</style>
```

- [ ] **Step 2: 修改首页使用滚动组件**

在 `wodezhuye.vue` 中：

1. import 组件：
```javascript
import ScrollNotice from '@/components/scroll-notice';
```

2. 注册组件：
```javascript
components: { chart, gonggaoDetail, setMenu, gonggaoList, ScrollNotice },
```

3. 将公告区的 `<ul class="rr-gonggao">` 替换为：
```html
<scroll-notice :list="QRY1" slogan="质量第一 精准计量" @click="getDetail" />
```

4. 移除 panel-body 的固定高度 `style="height:300px"`

- [ ] **Step 3: 验证**

运行: `cd p-admin && npm run dev`
预期: 首页公告区自动垂直滚动，鼠标悬停暂停，点击公告弹出详情

- [ ] **Step 4: Commit**

```bash
git add p-admin/src/components/scroll-notice/ p-admin/src/pages/main/wodezhuye.vue
git commit -m "feat: 首页公告改为滚动播放，增加slogan条幅"
```

---

### Task 2.2: 流程日志改为按键查看（Drawer抽屉）

**Files:**
- Modify: `p-admin/src/pages/r01/m02/views/add.vue:53-62`

- [ ] **Step 1: 隐藏审批记录表格，添加按钮和Drawer**

将审批记录区域（第52-62行）：

```html
审批记录
<div>
  <table>
    <tr v-for="(item,index) in DTSC" :key="index">
      <td width="100px;">{{item.OPLOGER}}</td>
      <td width="200px;">{{item.OPLOGDATE}}</td>
      <td width="100px;">{{item.STATE}}</td>
      <td>{{item.REMARK}}</td>
    </tr>
  </table>
</div>
```

替换为：

```html
<Button size="s" @click="showLogs=true" v-if="DTSC.length>0">
  <i class="h-icon-clock"></i> 查看审批记录
</Button>
<Drawer v-model="showLogs" :width="350">
  <div class="rr-drawer-header" slot="header">审批记录</div>
  <div class="rr-timeline">
    <div class="rr-timeline-item" v-for="(item, index) in DTSC" :key="index">
      <div class="rr-timeline-dot" :class="getLogDotClass(item.STATE)"></div>
      <div class="rr-timeline-content">
        <div class="rr-timeline-header">
          <span class="rr-timeline-user">{{item.OPLOGER}}</span>
          <span class="rr-timeline-state">{{item.STATE}}</span>
        </div>
        <div class="rr-timeline-time">{{item.OPLOGDATE}}</div>
        <div class="rr-timeline-remark" v-if="item.REMARK">{{item.REMARK}}</div>
      </div>
    </div>
  </div>
</Drawer>
```

- [ ] **Step 2: 在 data 中添加状态变量和方法**

在 `data()` 返回对象中添加：
```javascript
showLogs: false,
```

在 `methods` 中添加：
```javascript
getLogDotClass(state) {
  const map = {
    '已提交': 'dot-blue',
    '已审核': 'dot-green',
    '已审批': 'dot-green',
    '已驳回': 'dot-red',
    '已签发': 'dot-primary',
    '已作废': 'dot-gray',
  };
  return map[state] || 'dot-blue';
},
```

- [ ] **Step 3: 添加时间轴样式**

在 `<style>` 中添加：

```less
.rr-timeline {
  padding: 10px 0;
  &-item {
    display: flex;
    padding-bottom: 20px;
    position: relative;
  }
  &-dot {
    width: 10px; height: 10px;
    border-radius: 50%;
    margin: 5px 12px 0 0;
    flex-shrink: 0;
    &.dot-blue { background: #2B7A78; }
    &.dot-green { background: #52c41a; }
    &.dot-red { background: #f5222d; }
    &.dot-primary { background: #1890ff; }
    &.dot-gray { background: #999; }
  }
  &-content { flex: 1; }
  &-header { display: flex; justify-content: space-between; }
  &-user { font-weight: bold; }
  &-state { color: #2B7A78; font-size: 12px; }
  &-time { color: #999; font-size: 12px; margin-top: 4px; }
  &-remark { color: #666; margin-top: 4px; font-size: 13px; background: #f5f5f5; padding: 6px 8px; border-radius: 4px; }
}
```

- [ ] **Step 4: 验证**

运行: `cd p-admin && npm run dev`
预期: 原始记录编辑页面不再直接显示审批记录，点击"查看审批记录"按钮打开右侧抽屉，以时间轴形式展示

- [ ] **Step 5: Commit**

```bash
git add p-admin/src/pages/r01/m02/views/add.vue
git commit -m "feat: 审批记录改为按键查看，Drawer抽屉+时间轴展示"
```

---

### Task 2.3: 后端增加字段修改API（需求4）

**Files:**
- Modify: `netcore/Realso.WebAPI/Controllers/RM11Controller.cs`

- [ ] **Step 1: 在 doMyApi 中增加 A51 路由**

在 `RM11Controller.cs` 的 `doMyApi` switch 中，`default` 之前添加：

```csharp
case "A51":
  this.doFieldModify(MD, row, Params);
  break;
```

- [ ] **Step 2: 实现 doFieldModify 方法**

```csharp
protected virtual void doFieldModify(MOUDLE MD, ViewRow row, Hashtable Params)
{
  string MAINPATH = row.GetString("PATHNAME");
  string ID = Params["ID"] + "";
  string FIELD_NAME = Params["FIELD_NAME"] + "";
  string FIELD_VALUE = Params["FIELD_VALUE"] + "";

  if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(FIELD_NAME))
  {
    responseModel.SetError("参数不完整");
    return;
  }

  // 允许修改的字段白名单
  string[] allowedFields = { "CERTCODE", "CALIBDATE", "CHECKTIME", "VERIFYTIME" };
  if (Array.IndexOf(allowedFields, FIELD_NAME) == -1)
  {
    responseModel.SetError("不允许修改该字段");
    return;
  }

  ViewRow pathRow = MD.GetPath(MAINPATH);
  M01 MAIN = new M01(this.operate01, pathRow.GetString("RESOURCEID"));
  MAIN.OpenByID(ID);

  if (MAIN.GetView().Count == 0)
  {
    responseModel.SetError("记录不存在");
    return;
  }

  string oldValue = MAIN.GetValue(FIELD_NAME) + "";
  MAIN.SetValue(FIELD_NAME, FIELD_VALUE);

  // 记录变更日志（复用现有 addLog）
  ArrayList saveList = new ArrayList();
  saveList.Add(MAIN.GetView());
  try
  {
    operate01.Save(saveList);
    this.addLog(
      MD.GetView()[0].GetString("MODULENAME"),
      $"字段修改：{FIELD_NAME} 由 [{oldValue}] 改为 [{FIELD_VALUE}]",
      saveList, ""
    );
    responseModel.SetData(MAIN.GetView());
  }
  catch (Exception ex)
  {
    this.addLog(MD.GetView()[0].GetString("MODULENAME"), "字段修改【失败】", saveList, ex.Message);
    throw;
  }
}
```

- [ ] **Step 3: 验证**

运行: `cd netcore && dotnet build`
预期: 编译通过

- [ ] **Step 4: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/RM11Controller.cs
git commit -m "feat: 后端增加字段修改API，支持证书编号/日期手动修改并记录日志"
```

---

### Task 2.4: 后端增加异常检测API（需求7）

**Files:**
- Modify: `netcore/Realso.WebAPI/Controllers/RM11Controller.cs`

- [ ] **Step 1: 在 doMyApi 中增加 A52 路由**

```csharp
case "A52":
  this.doCheckAnomaly(MD, row, Params);
  break;
```

- [ ] **Step 2: 实现 doCheckAnomaly 方法**

```csharp
protected virtual void doCheckAnomaly(MOUDLE MD, ViewRow row, Hashtable Params)
{
  ArrayList anomalies = new ArrayList();
  string ID = Params["ID"] + "";

  // 1. 标准器查重：同一标准器在同一时间段被多条记录使用
  string sql1 = SQLManage.GetSQL("CHECK_ARD_CONFLICT");
  if (!string.IsNullOrEmpty(sql1))
  {
    Hashtable p1 = new Hashtable();
    p1["ID"] = ID;
    var ardConflicts = this.operate01.Query(sql1, p1);
    foreach (var item in ardConflicts)
    {
      anomalies.Add(new {
        type = "ard_conflict",
        level = "warning",
        message = $"标准器 {item["ARDNAME"]} 在 {item["START_DATE"]}~{item["END_DATE"]} 被多人使用"
      });
    }
  }

  // 2. 人员查重：同一校准人员同一时间段并行记录
  string sql2 = SQLManage.GetSQL("CHECK_EMP_CONFLICT");
  if (!string.IsNullOrEmpty(sql2))
  {
    Hashtable p2 = new Hashtable();
    p2["ID"] = ID;
    var empConflicts = this.operate01.Query(sql2, p2);
    foreach (var item in empConflicts)
    {
      anomalies.Add(new {
        type = "emp_conflict",
        level = "warning",
        message = $"校准人员 {item["EMPNAME"]} 在 {item["CALIBDATE"]} 有 {item["CNT"]} 条并行记录"
      });
    }
  }

  // 3. 委托超期检测
  string sql3 = SQLManage.GetSQL("CHECK_WT_TIMEOUT");
  if (!string.IsNullOrEmpty(sql3))
  {
    Hashtable p3 = new Hashtable();
    p3["ID"] = ID;
    var timeoutItems = this.operate01.Query(sql3, p3);
    foreach (var item in timeoutItems)
    {
      anomalies.Add(new {
        type = "wt_timeout",
        level = "error",
        message = $"委托单 {item["WTCODE"]} 已超期 {item["DAYS"]} 天"
      });
    }
  }

  responseModel.SetData(anomalies);
}
```

注意：SQL 模板（CHECK_ARD_CONFLICT 等）需在 `VSS_sQL` 表中预先配置。

- [ ] **Step 3: 验证**

运行: `cd netcore && dotnet build`
预期: 编译通过

- [ ] **Step 4: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/RM11Controller.cs
git commit -m "feat: 后端增加异常检测API，检测标准器冲突、人员冲突、委托超期"
```

---

### Task 2.5: 新建委托审核模块（需求8.1）

**Files:**
- Create: `p-admin/src/pages/r01/m025/router.js`
- Create: `p-admin/src/pages/r01/m025/store.js`
- Create: `p-admin/src/pages/r01/m025/index.js`
- Create: `p-admin/src/pages/r01/m025/views/main.vue`
- Create: `p-admin/src/pages/r01/m025/views/review.vue`

- [ ] **Step 1: 创建 router.js**

```javascript
export default [
  {
    path: 'm025',
    name: '/r01/m025',
    component: resolve => require(['./index'], resolve),
    children: [
      {
        path: '',
        name: '/r01/m025/main',
        component: resolve => require(['./views/main.vue'], resolve)
      },
      {
        path: 'review',
        name: '/r01/m025/review',
        component: resolve => require(['./views/review.vue'], resolve)
      }
    ]
  }
];
```

- [ ] **Step 2: 创建 store.js**

参照现有 `r01/m021/store1.js` 模式，创建 store：

```javascript
import Store03 from '@/store/Store03';
import createStore from '@/store/createStore';
import BaseStore from './baseStore';  // 复用 r01/m02/baseStore.js

let storeName = 'r01/m025';
export default createStore.getStore({
  storeName,
  mixins: [BaseStore],
  getters: {},
  mutations: {},
  actions: {
    // 委托单维度查询
    queryWT({ state, commit, rootState }, param) {
      param = param || {};
      param.api = '/api/rm11/call/LI_M02/A53/';  // 新增API
      param.tp = 'call';
      param.ISCHECKREPEAT = false;
      return db.call(param).then(ret => {
        commit('SET_DATA', { key: 'WT_LIST', data: ret.Items || [] });
      });
    },
    // 委托单下明细查询
    queryWTDetail({ state, commit, rootState }, param) {
      param = param || {};
      param.api = '/api/rm11/call/LI_M02/A54/';  // 新增API
      param.tp = 'call';
      param.ISCHECKREPEAT = false;
      return db.call(param).then(ret => {
        commit('SET_DATA', { key: 'WT_DETAIL', data: ret.Items || [] });
      });
    },
  }
});
export const Constants = { STORE_NAME: storeName };
```

- [ ] **Step 3: 创建 index.js**

```javascript
export default require('./views/main.vue');
```

- [ ] **Step 4: 创建 main.vue（委托审核入口页）**

实现PPT Slide 5的布局：上半部分委托单列表（支持选择），下半部分选中委托单的明细记录。底部"开始审批"按钮跳转到 `/r01/m025/review`。

核心布局：
```html
<template>
  <div class="h-panel h-panel-no-border">
    <div class="h-panel-bar">
      <span class="h-panel-title">委托审核</span>
      <div class="h-panel-right">
        <Search v-model="INPUT" placeholder="委托单号/委托单位" @search="query" />
      </div>
    </div>
    <div class="h-panel-body">
      <!-- 委托单列表 -->
      <rs-table-list :datas="WT_LIST" border @click="selectWT">
        <TableItem title="选择" :width="60" align="center">
          <template slot-scope="{data}">
            <input type="radio" name="wt" :value="data.ID" v-model="selectedWTID" @click.stop="selectWT(data)" />
          </template>
        </TableItem>
        <TableItem title="委托单号" prop="WTCODE" :width="140" />
        <TableItem title="委托单位" prop="CUSTNAME" />
        <TableItem title="器具数量" prop="CNT" :width="90" align="center" />
        <TableItem title="委托时间" prop="WTDATE" :width="120" />
        <TableItem title="提交时间" prop="SUBMITTIME" :width="150" sort="auto" />
        <TableItem title="提交部门" prop="DEPTNAME" :width="120" />
        <TableItem title="提交人" prop="SUBMITER" :width="100" />
      </rs-table-list>

      <!-- 委托明细（选中后显示） -->
      <div v-if="selectedWTID" style="margin-top:15px;">
        <ToolBar label="委托明细" :size="14" />
        <rs-table-list :datas="WT_DETAIL" border>
          <TableItem title="序号" :width="60" align="center">
            <template slot-scope="{data,$index}">{{$index+1}}</template>
          </TableItem>
          <TableItem title="证书单位" prop="CUSTNAME" />
          <TableItem title="设备名称" prop="ARDNAME" />
          <TableItem title="设备型号" prop="SIZETYPE" :width="120" />
          <TableItem title="设备编号" prop="OMCODE" :width="120" />
          <TableItem title="审核人" prop="CHECKER" :width="100" />
          <TableItem title="审批状态" prop="STATE_TEXT" :width="100" />
        </rs-table-list>
      </div>

      <!-- 操作按钮 -->
      <div class="footer-action" v-if="selectedWTID && WT_DETAIL.length > 0">
        <Button color="primary" @click="startReview">开始审批</Button>
        <Button @click="startReReview">开始复审</Button>
      </div>
    </div>
  </div>
</template>
```

- [ ] **Step 5: 创建 review.vue（审核分屏页）**

实现PPT Slide 6的三栏分屏布局。这是核心页面，完整实现包含：
- 左侧：记录列表（当前委托单下的待审核记录）
- 中间：证书PDF预览（PDF.js canvas 渲染）
- 右侧：原始记录PDF预览
- 底部：异常检测区 + 审核检查清单 + 操作按钮

关键布局CSS：
```css
.review-container {
  display: flex;
  height: calc(100vh - 160px);
}
.review-left { width: 220px; overflow-y: auto; border-right: 1px solid #e8e8e8; }
.review-center { flex: 1; overflow: hidden; position: relative; }
.review-right { flex: 1; overflow: hidden; position: relative; border-left: 1px solid #e8e8e8; }
.review-anomaly { border-top: 1px solid #e8e8e8; max-height: 200px; overflow-y: auto; }
.review-checklist { border-top: 1px solid #e8e8e8; padding: 10px; }
```

- [ ] **Step 6: Commit**

```bash
git add p-admin/src/pages/r01/m025/
git commit -m "feat: 新建委托审核模块，含入口页和分屏审核页"
```

---

### Task 2.6: 新建委托审批模块（需求8.3三级审批）

**Files:**
- Create: `p-admin/src/pages/r01/m026/` (router.js + store.js + index.js + views/)

- [ ] **Step 1: 复制 m025 模块结构，修改为审批版**

复制 `r01/m025/` 目录为 `r01/m026/`，修改：
- storeName 改为 `'r01/m026'`
- API 调用使用审批相关 APICODE（A40/A42 查询，A25/A29 审批操作）
- 检查清单内容改为审批级别（权限与资质核查、委托与范围核查等）
- 页面标题改为"委托审批"
- 增加权限拦截：检查当前用户是否为该项目的授权签字人

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/r01/m026/
git commit -m "feat: 新建委托审批模块，支持授权签字人合规风控审批"
```

---

## Chunk 3: 业务模块新增（需求9/10）

### Task 3.1: 数据库新建项目费用标准表

- [ ] **Step 1: 执行 SQL**

```sql
CREATE TABLE TSS_PROJECT_FEE (
  ID varchar(50) NOT NULL PRIMARY KEY,
  PROJECT_NAME varchar(200) NOT NULL COMMENT '项目名称',
  TEMPLATE_ID varchar(50) COMMENT '关联模板ID',
  CAMT decimal(10,2) DEFAULT 0 COMMENT '检测费',
  OAMT decimal(10,2) DEFAULT 0 COMMENT '其他费',
  BAMT decimal(10,2) DEFAULT 0 COMMENT '加急费',
  VALID tinyint DEFAULT 1 COMMENT '有效标志',
  CREATEID varchar(50) COMMENT '创建人ID',
  CREATETIME datetime COMMENT '创建时间',
  MODIFYID varchar(50) COMMENT '修改人ID',
  MODIFYTIME datetime COMMENT '修改时间',
  IS_DELETED tinyint DEFAULT 0 COMMENT '逻辑删除'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='项目费用标准表';
```

- [ ] **Step 2: 在 TSS_RESOURCE + TSS_RESFIELD 中注册该表的元数据**

按现有元数据驱动架构，需要在数据库中配置资源定义和字段定义，使 DataController 标准API能操作该表。

- [ ] **Step 3: Commit**

```bash
git add docs/sql/TSS_PROJECT_FEE.sql
git commit -m "feat: 新建项目费用标准表及元数据配置"
```

---

### Task 3.2: 受理单模板选择自动关联项目费用

**Files:**
- Modify: `p-admin/src/pages/r01/m05/views/add.vue`

- [ ] **Step 1: 模板选择后自动查询项目费用**

在 `add.vue` 的模板选择回调中增加费用自动填充逻辑：

```javascript
// 在模板选择的 change 回调中
async onTemplateChange(template) {
  this.PTEMPLATEID = template.ID;
  this.PTEMPLATECAMT = template.CAMT;
  // 查询项目费用
  let fee = await db.call({
    api: '/api/data/call/RS_FEE/A01/',
    tp: 'call',
    TEMPLATE_ID: template.ID
  });
  if (fee && fee.length > 0) {
    this.CAMT = fee[0].CAMT;
    this.OAMT = fee[0].OAMT;
    this.BAMT = fee[0].BAMT;
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/r01/m05/views/add.vue
git commit -m "feat: 受理单选择模板后自动关联项目费用"
```

---

### Task 3.3: 后端电子证书签发API

**Files:**
- Modify: `netcore/Realso.WebAPI/Controllers/RM11Controller.cs`

- [ ] **Step 1: 在 doMyApi 中增加 A55 路由**

```csharp
case "A55":
  this.doECertSign(MD, row, Params);
  break;
```

- [ ] **Step 2: 实现 doECertSign 方法**

在现有 `doGenCert` 基础上扩展，核心逻辑：

```csharp
protected virtual void doECertSign(MOUDLE MD, ViewRow row, Hashtable Params)
{
  // 1. 先调用 doGenCert 生成标准证书PDF
  this.doGenCert(MD, row, Params);

  // 2. 获取生成的PDF文件路径
  string CERTID = responseModel.GetData() + "";  // 需要从 doGenCert 返回中获取

  // 3. 生成防伪二维码（验证URL + 证书ID）
  string verifyUrl = ConfigHelper.GetConfig("Url:证书验证") + "?id=" + Params["ID"];
  string qrPath = QRHelper.CreateQR(verifyUrl, 200, 200);

  // 4. 合成电子公章 + 人员签名 + 二维码到PDF（使用 iTextSharp 或 PdfSharp）
  // 在PDF指定位置叠加图片层
  // 具体实现依赖引入 PDF 操作库

  // 5. 设置PDF密码保护（证书编号后6位）
  // string password = certCode.Substring(certCode.Length - 6);

  // 6. 更新记录状态
  responseModel.SetData(CERTID);
}
```

注意：PDF 操作需要引入 `itext7` 或 `PdfSharpCore` NuGet 包。

- [ ] **Step 3: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/RM11Controller.cs netcore/Realso.WebAPI/Realso.WebAPI.csproj
git commit -m "feat: 后端增加电子证书签发API，支持公章合成和密码保护"
```

---

### Task 3.4: 客户电子证书下载页面

**Files:**
- Create: `p-admin/src/pages/out/ecert/index.vue`
- Create: `p-admin/src/pages/out/ecert/router.js`

- [ ] **Step 1: 创建独立页面**

无需登录的证书下载页。通过URL参数传入证书ID或扫码验证：

```html
<template>
  <div class="ecert-page">
    <div class="ecert-header">
      <h2>睿谱希 - 电子证书验证</h2>
    </div>
    <div class="ecert-body" v-if="certInfo">
      <div class="ecert-valid" v-if="certInfo.isValid">
        <div class="ecert-badge">验证通过</div>
        <div class="ecert-info">
          <p>证书编号：{{certInfo.CERTCODE}}</p>
          <p>委托单位：{{certInfo.CUSTNAME}}</p>
          <p>设备名称：{{certInfo.ARDNAME}}</p>
          <p>签发日期：{{certInfo.SIGNDATE}}</p>
        </div>
        <div class="ecert-actions">
          <a :href="certInfo.pdfUrl" target="_blank">下载电子证书</a>
        </div>
        <div class="ecert-qrcode">
          <p>扫描二维码验证真伪</p>
          <img :src="certInfo.qrUrl" />
        </div>
        <div class="ecert-person">
          <p>校准人员：{{certInfo.SUBMITER}}</p>
          <img :src="certInfo.personQrUrl" title="扫描查看人员资质" />
        </div>
      </div>
      <div class="ecert-invalid" v-else>
        <p>证书验证失败，请核实证书编号。</p>
      </div>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/out/ecert/
git commit -m "feat: 新建客户电子证书下载和验证页面"
```

---

## Chunk 4: 扩展模块（需求11）

### Task 4.1: 数据库新建物流相关表

- [ ] **Step 1: 执行 SQL**

```sql
CREATE TABLE TSS_LOGISTICS (
  ID varchar(50) NOT NULL PRIMARY KEY,
  REF_ID varchar(50) NOT NULL COMMENT '关联委托单/受理单ID',
  REF_TYPE varchar(20) NOT NULL COMMENT '类型：sample/证书',
  LOGISTICS_COMPANY varchar(100) COMMENT '快递公司',
  LOGISTICS_NO varchar(100) COMMENT '物流单号',
  SEND_DATE datetime COMMENT '寄出日期',
  RECEIVE_NAME varchar(50) COMMENT '收件人',
  RECEIVE_PHONE varchar(20) COMMENT '收件人电话',
  RECEIVE_ADDR varchar(500) COMMENT '收件地址',
  STATUS int DEFAULT 0 COMMENT '状态：0待寄送/1已寄送/2运输中/3已签收',
  REMARK varchar(500) COMMENT '备注',
  CREATEID varchar(50),
  CREATETIME datetime,
  MODIFYID varchar(50),
  MODIFYTIME datetime,
  IS_DELETED tinyint DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物流主表';

CREATE TABLE TSS_LOGISTICS_NODE (
  ID varchar(50) NOT NULL PRIMARY KEY,
  LOGISTICS_ID varchar(50) NOT NULL COMMENT '物流主表ID',
  NODE_TIME datetime COMMENT '节点时间',
  NODE_DESC varchar(500) COMMENT '节点描述',
  NODE_IMAGE varchar(200) COMMENT '节点照片文件ID',
  CREATEID varchar(50),
  CREATETIME datetime,
  IS_DELETED tinyint DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物流节点表';
```

- [ ] **Step 2: 在 TSS_RESOURCE + TSS_RESFIELD 中注册元数据**

- [ ] **Step 3: Commit**

```bash
git add docs/sql/TSS_LOGISTICS.sql
git commit -m "feat: 新建物流主表和物流节点表"
```

---

### Task 4.2: 新建物流管理前端模块

**Files:**
- Create: `p-admin/src/pages/r02/m07/` (router.js + store.js + index.js + views/main.vue + views/add.vue)

- [ ] **Step 1: 创建模块文件**

参照现有模块结构（如 `r01/m03`），创建：
- `router.js`: 路由 `/r02/m07`
- `store.js`: Vuex Store，使用 Store03 基类
- `views/main.vue`: 物流列表页，支持按委托单筛选、状态筛选
- `views/add.vue`: 物流信息录入页（快递公司+单号+寄出日期+节点照片上传）

列表页核心功能：
- 筛选条：委托单号、状态（下拉选择）
- 表格列：委托单号、快递公司、物流单号、寄出日期、收件人、状态
- 操作：添加物流、更新状态、查看轨迹
- 底部按钮：添加、批量更新状态

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/r02/m07/
git commit -m "feat: 新建物流管理前端模块"
```

---

### Task 4.3: 客户物流查询页面

**Files:**
- Create: `p-admin/src/pages/out/logistics/index.vue`
- Create: `p-admin/src/pages/out/logistics/router.js`

- [ ] **Step 1: 创建独立查询页**

无需登录，输入物流单号查询：

```html
<template>
  <div class="logistics-query">
    <h2>物流查询</h2>
    <div class="search-box">
      <input v-model="logisticsNo" placeholder="请输入物流单号" />
      <button @click="query">查询</button>
    </div>
    <div class="result" v-if="info">
      <div class="info-card">
        <p>快递公司：{{info.LOGISTICS_COMPANY}}</p>
        <p>物流单号：{{info.LOGISTICS_NO}}</p>
        <p>当前状态：{{statusText(info.STATUS)}}</p>
      </div>
      <div class="timeline">
        <div v-for="node in info.nodes" :key="node.ID" class="timeline-item">
          <div class="dot"></div>
          <div class="content">
            <div class="time">{{node.NODE_TIME}}</div>
            <div class="desc">{{node.NODE_DESC}}</div>
            <img v-if="node.NODE_IMAGE" :src="getFileUrl(node.NODE_IMAGE)" class="node-img" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/out/logistics/
git commit -m "feat: 新建客户物流查询页面"
```

---

## 实施顺序总结

| 顺序 | Task | 需求 | 预计工作量 | 依赖 |
|------|------|------|-----------|------|
| 1 | 1.1 | 需求3 | 小 | 无 |
| 2 | 1.2 | 需求3 | 中 | 无 |
| 3 | 1.3 | 需求1 | 中 | 无 |
| 4 | 1.4 | 需求6 | 中 | 无 |
| 5 | 1.5 | 需求6 | 小 | 无 |
| 6 | 2.1 | 需求2 | 中 | 1.3（主题色） |
| 7 | 2.2 | 需求5 | 中 | 无 |
| 8 | 2.3 | 需求4 | 中 | 无 |
| 9 | 2.4 | 需求7 | 大 | 无 |
| 10 | 2.5 | 需求8 | 大 | 1.4, 2.4, 1.5 |
| 11 | 2.6 | 需求8 | 大 | 2.5 |
| 12 | 3.1 | 需求9 | 中 | 无（DBA） |
| 13 | 3.2 | 需求9 | 中 | 3.1 |
| 14 | 3.3 | 需求10 | 大 | 无 |
| 15 | 3.4 | 需求10 | 中 | 3.3 |
| 16 | 4.1 | 需求11 | 中 | 无（DBA） |
| 17 | 4.2 | 需求11 | 大 | 4.1 |
| 18 | 4.3 | 需求11 | 中 | 4.2 |

**关键路径**: 1.4 → 2.4 → 2.5 → 2.6（PDF优化 → 异常检测 → 审核分屏 → 审批分屏）
