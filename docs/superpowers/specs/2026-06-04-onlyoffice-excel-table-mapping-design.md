# OnlyOffice Excel 编辑表格映射 - 设计文档

## 需求背景

s01/m07 模板编辑功能中，`itemEditor`（富文本框）的表格映射配置极其复杂：需要手动在富文本中绘制表格、插入 `${字段名}` 占位符、逐个配置字段属性和计算公式。整个过程容易出错，用户体验差。

**目标**：通过 OnlyOffice Excel 的能力，让用户在 Excel 中完成表格布局 + 字段占位符 + 公式编辑，一键回写到富文本表格中。

## 需求确认

| 项目 | 选择 |
|-----|------|
| 使用场景 | 完整替代（Excel 中完成布局+字段+公式，一键导入） |
| 字段标记 | `${字段名}` 占位符（与现有语法一致） |
| 公式语法 | Excel 原生公式（自动转换为模板公式） |
| OnlyOffice | 已部署 Document Server |
| 交互流程 | 弹窗式编辑 |
| 回写范围 | 选中已有 itemEditor 控件编辑回写 |

## 方案选择

**方案 A（采用）：纯前端解析**

前端使用 SheetJS 库完成 HTML ↔ Excel 双向转换，OnlyOffice 作为编辑器弹窗嵌入。后端仅提供文件临时存储和 OnlyOffice 回调接口。

选择理由：
1. 表格数据量通常不大，前端解析性能足够
2. OnlyOffice 已部署，只需配置回调 URL
3. 不需要后端核心逻辑改动，开发周期短
4. SheetJS 库成熟，HTML ↔ Excel 转换有现成 API

## 架构设计

### 整体交互流程

```
选中 itemEditor 控件 → 点击"Excel编辑"按钮
  → 前端解析 HTML 表格 + fields → 生成 .xlsx（SheetJS）
  → 上传到后端临时存储 → OnlyOffice 弹窗打开编辑
  → 用户编辑（调整布局、添加 ${字段名}、写公式）
  → 点击保存 → OnlyOffice 回调后端 → 前端获取 .xlsx
  → SheetJS 解析 Excel → 转换为 HTML + fields
  → 回写 itemEditor.value 和 itemEditor.fields
```

### 文件变更清单

#### 新增文件

| 文件 | 位置 | 职责 |
|-----|------|------|
| `excel-editor.vue` | `s01/m07/views/components/` | OnlyOffice 弹窗编辑器组件 |
| `excelConverter.js` | `s01/m07/views/components/` | Excel ↔ HTML/fields 转换工具（核心） |

#### 修改文件

| 文件 | 修改内容 |
|-----|---------|
| `rs-set-attr.vue` | itemEditor 属性面板增加"Excel编辑"按钮 |
| `ueditor/index2.vue` | inLayout 编辑模式下增加"Excel编辑"快捷入口 |

#### 后端新增

| 文件 | 职责 |
|-----|------|
| `ExcelEditorController.cs` | 临时文件上传、OnlyOffice 回调、文件下载 |

## 核心模块设计

### 1. excelConverter.js — 双向转换引擎

#### 1.1 导出：itemEditor → Excel

```javascript
/**
 * 将 itemEditor 的 HTML 表格和字段定义转换为 Excel 工作簿
 * @param {string} htmlValue - 富文本 HTML（含 <table>）
 * @param {Array} fields - 字段定义数组
 * @returns {ArrayBuffer} - .xlsx 文件数据
 */
function exportToExcel(htmlValue, fields) {
  // 1. 解析 HTML，提取 <table> 结构
  // 2. 识别每个单元格中的 ${字段名} 占位符
  // 3. 查找字段 formula，将 ${FIELD} 转为 Excel 单元格引用
  // 4. 处理合并单元格 (colspan/rowspan)
  // 5. 使用 SheetJS 生成 .xlsx
}
```

**公式导出转换**：
- 模板公式 `${A}+${B}` → Excel 公式 `=C2+D2`（C2/D2 是字段 A/B 所在单元格）
- 模板专用函数 `$avg([${A},${B}])` → Excel `=AVERAGE(C2,D2)`
- 计量专用函数（`$indError` 等）→ 在 Excel 单元格中以文本形式保留，如 `=$indError(C2,D2,2)`

#### 1.2 导入：Excel → itemEditor

```javascript
/**
 * 将 Excel 工作簿转换为 itemEditor 的 HTML 和字段定义
 * @param {ArrayBuffer} xlsxData - .xlsx 文件数据
 * @param {Array} existingFields - 已有字段定义（用于属性合并）
 * @returns {{ value: string, fields: Array }} - HTML + 字段定义
 */
function importFromExcel(xlsxData, existingFields) {
  // 1. SheetJS 解析 .xlsx
  // 2. 遍历每个单元格
  // 3. 识别 ${字段名} → 创建/合并 field 定义
  // 4. 识别 Excel 公式 → 转换为模板公式
  // 5. 处理合并单元格 → colspan/rowspan
  // 6. 生成 HTML 表格
  // 7. 合并已有字段属性（保留 name/minv/maxv/helpInfo 等）
}
```

**公式导入转换**：
- Excel `=SUM(C2:C6)` → 模板 `$t([${F1},${F2},${F3},${F4},${F5}])`
- Excel `=AVERAGE(C2:C6)` → 模板 `$avg([${F1},...])`
- Excel `=ABS(C2-D2)` → 模板 `$abAbs(${F1},${F2})`
- Excel `=C2+D2` → 模板 `${F1}+${F2}`
- Excel `=$indError(C2,D2,2)` → 模板 `$indError(${F1},${F2},2)`（直接保留计量函数）

### 2. Excel 函数 ↔ 模板公式映射表

#### 2.1 有 Excel 直接对应的函数（双向自动转换）

| Excel 函数 | 模板函数 | 说明 |
|-----------|---------|------|
| `SUM(range)` | `$t([fields],dp)` | 求和 |
| `AVERAGE(range)` | `$avg([fields],dp,dfh)` | 平均值 |
| `ABS(val)` | `$abs(val)` | 绝对值 |
| `SQRT(val)` | `$sqrt(val)` | 平方根 |
| `LN(val)` | `$log(val)` | 自然对数 |
| `ROUND(val,n)` | `$fixed(val,n,dfh)` | 保留小数位 |
| `STDEV.S(range)` | `$stdev([fields],dp,dfh)` | 样本标准差 |
| `MAX(range)-MIN(range)` | `$maxmin([fields],dp,dfh)` | 极差 |
| `SQRT(SUMSQ(range))` | `$sqrtpow([fields],dp,dfh)` | 平方和的平方根 |
| `ABS(a-b)` | `$abAbs(${a},${b},dp,dfh)` | AB差值绝对值 |
| `val^2` | `$pow2(val)` | 平方 |

#### 2.2 计量专用函数（无 Excel 原生对应，用 `$` 前缀在 Excel 中标记）

| 标记方式 | 模板函数 | 说明 |
|---------|---------|------|
| `=$indError(a,b,dp,dfh)` | `$indError(${a},${b},dp,dfh)` | 示值误差 |
| `=$std(range,snum,dp,dfh)` | `$std([fields],snum,dp,dfh)` | 标准方差 |
| `=$maxStd(range,snum,dp,dfh)` | `$maxStd([fields],snum,dp,dfh)` | 最大偏差 |
| `=$maxAbs(range,dp,iaabs)` | `$maxAbs([fields],dp,iaabs)` | 绝对值最大 |
| `=$minAbs(range,dp,iaabs)` | `$minAbs([fields],dp,iaabs)` | 绝对值最小 |
| `=$avgStd(range,num,dp,dfh)` | `$avgStd([fields],num,dp,dfh)` | 平均值-标准值 |
| `=$maxminStd(range,std,dp,dfh)` | `$maxminStd([fields],std,dp,dfh)` | 极差/标准 |

在 Excel 中，这些 `$` 前缀函数会被 OnlyOffice 识别为文本（不是标准 Excel 函数），不影响编辑。导入时系统识别 `$` 前缀，直接转换为模板公式。

#### 2.3 单元格引用 → 字段名映射

```javascript
// 导入时：建立单元格坐标 → 字段名的映射
// 例如 Excel 中：
//   A1: "温度",  B1: "压力"
//   A2: ${TEMP},  B2: ${PRES}
//   C2: =A2+B2
// 转换为：
//   C2 的公式 =A2+B2 → ${TEMP}+${PRES}

// 导出时：反向映射
//   字段 TEMP 在 HTML 表格的第2列第2行 → Excel C2
//   公式 ${TEMP}+${PRES} → =C2+D2
```

### 3. 字段属性合并策略

从 Excel 回写时，**合并**而非替换字段定义：

```
已有字段（field 名相同）→ 保留 name、minv、maxv、helpInfo、dvalue 等属性
新增字段 → 创建默认 field 定义
已删除字段（Excel 中不再有对应 ${字段名}）→ 从 fields 数组中移除
```

Excel 批注（Comment）可选用于标记额外属性：
```json
{"name":"温度","minv":0,"maxv":100,"helpInfo":"环境温度"}
```

### 4. excel-editor.vue — OnlyOffice 弹窗组件

#### 4.1 组件接口

```javascript
props: {
  // 无 props，通过 open() 方法传入数据
}

methods: {
  // 打开编辑器
  open(itemEditorData) {
    // itemEditorData = { value: html, fields: [...], selectNode: nodeRef }
    // 1. 调用 excelConverter.exportToExcel() 生成 .xlsx
    // 2. 上传到后端，获取文件 key
    // 3. 初始化 OnlyOffice Document Editor
    // 4. 显示弹窗
  },

  // 保存并返回
  save() {
    // 1. 触发 OnlyOffice 保存
    // 2. 下载保存后的 .xlsx
    // 3. 调用 excelConverter.importFromExcel() 转换
    // 4. $emit('save', { value, fields })
  }
}

events:
  @save — 编辑完成，返回 { value, fields }
```

#### 4.2 OnlyOffice 配置

```javascript
new DocsAPI.DocEditor('editor-container', {
  documentType: 'cell',           // 电子表格模式
  document: {
    fileType: 'xlsx',
    key: fileKey,                  // 唯一标识（每次编辑需不同）
    title: '表格编辑',
    url: downloadUrl,              // 后端文件下载地址
  },
  editorConfig: {
    mode: 'edit',
    callbackUrl: callbackUrl,      // OnlyOffice 保存回调地址
    lang: 'zh-CN',
    customization: {
      autosave: false,             // 关闭自动保存，手动控制
      forcesave: true,             // 启用手动保存
    },
  },
  events: {
    onDocumentReady: () => { /* 编辑器就绪 */ },
    onRequestSaveAs: () => { /* 另存为 */ },
  },
});
```

### 5. 后端 API 设计

#### ExcelEditorController.cs

```
POST /api/ExcelEditor/Upload
  - 接收: .xlsx 文件 (multipart/form-data)
  - 返回: { key: "uuid", downloadUrl: "/api/ExcelEditor/Download?key=uuid" }
  - 存储: 临时目录，TTL 24小时

GET /api/ExcelEditor/Download?key=xxx
  - 返回: .xlsx 文件流
  - OnlyOffice 回调用此 URL 下载文件

POST /api/ExcelEditor/Callback
  - OnlyOffice 保存回调
  - 接收: { key, status, url }
  - status=2: 文档已关闭，下载最新版本替换临时文件
  - status=6: 正在编辑中的强制保存
```

### 6. rs-set-attr.vue 修改

在 `itemEditor` 属性面板中增加"Excel编辑"按钮：

```html
<div class="list-item" v-if="attr.type==='itemEditor'">
  <!-- 现有的 SFIELDS textarea 和字段列表保持不变 -->
  ...
  <Button color="primary" @click.native="openExcelEditor">
    Excel编辑
  </Button>
  <excel-editor ref="excelEditor" @save="onExcelSave" />
</div>
```

```javascript
methods: {
  openExcelEditor() {
    this.$refs.excelEditor.open({
      value: this.attr.value,
      fields: this.attr.fields,
    });
  },
  onExcelSave({ value, fields }) {
    this.attr.value = value;
    this.attr.fields = fields;
  },
}
```

## 依赖

| 依赖 | 用途 | 安装方式 |
|-----|------|---------|
| `xlsx` (SheetJS) | Excel 文件读写 | `npm install xlsx` |
| OnlyOffice Document Server | Excel 在线编辑 | 已部署 |
| OnlyOffice JS API | 前端集成 | `/web-apps/apps/api/documents/api.js` |

## 风险和注意事项

1. **OnlyOffice 回调机制**：OnlyOffice 保存是通过回调后端 URL 实现的，需要确保后端可被 OnlyOffice 服务器访问
2. **文件 Key 唯一性**：每次打开编辑器必须使用不同的 key，否则 OnlyOffice 会使用缓存
3. **并发编辑**：当前设计为单用户编辑模式，不需要考虑多人协作
4. **公式转换不完整**：Excel 支持的函数远多于模板公式，无法转换的公式应保留原始文本并提示用户
5. **单元格引用复杂度**：跨 Sheet 引用、3D 引用等复杂场景暂不支持
