# rs-meta-form / rs-meta-field 元数据驱动表单组件

## 概述

- **rs-meta-form** - 多字段表单组件，包装 `rs-form-edit`，传数据+字段配置即可渲染完整表单
- **rs-meta-field** - 单字段组件，和 rs-meta-form 相同的数据源/字段加载机制，只渲染 `field-name` 指定的单个字段

两个组件共享相同的 props 格式，不依赖 generic-module 框架，可在任何 `.vue` 文件中直接使用。

## 文件位置

- `rs-meta-form.vue` - 多字段表单
- `rs-meta-field.vue` - 单字段
- 全局注册：`src/components/index.js`

---

## Props

### 数据源（三选一）

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `path` | `[Object, String]` | `null` | DataTable 对象（如 `$MAIN`）或路径名字符串（如 `'MAIN'`） |
| `value` | `Object` | `null` | v-model 普通对象模式（rs-meta-form）/ 字段值（rs-meta-field） |
| `storeName` | `String` | `''` | `path` 为字符串时的 Vuex 命名空间，默认从 `inject.aiFormStoreName` 取 |

### 字段配置（优先级从高到低）

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `fields` | `Array` | `null` | 直接传入字段配置数组（`Gen.getFormFields` 格式） |
| `resourceName` | `String` | `''` | 按资源名从 `store.state.app.scms` 加载 |
| `moduleCode` | `String` | `''` | 按模块编码推导 resourceName（从 MODPATH.MAIN 查找） |

### 字段覆盖

| Prop | rs-meta-form | rs-meta-field | 说明 |
|------|-------------|---------------|------|
| `overrides` | `{ CUSTNAME: { readonly: true } }` | - | 按字段名做 key 的覆盖对象 |
| `override` | - | `{ readonly: true }` | 直接针对当前字段的覆盖 |
| `fieldName` | - | `String` | 要渲染的字段名（在 fields 数组中按 `props.key` 匹配） |
| `field` | - | `Object` | 直接传单个 field 配置对象（优先于 fields 数组查找） |

### 透传 rs-form-edit 布局（仅 rs-meta-form）

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `mode` | `String` | `'twocolumn'` | 布局模式：`single` / `twocolumn` / `threecolumn` |
| `labelWidth` | `Number` | `80` | 标签宽度 |
| `labelPosition` | `String` | `'right'` | 标签位置：`left` / `right` |
| `disabled` | `Boolean` | `false` | 整体禁用 |
| `showErrorTip` | `Boolean` | `false` | 显示错误提示 |
| `validOnChange` | `Boolean` | `true` | 变更时校验 |
| `defaultValues` | `Object` | `{}` | 默认值 |

### Form 包裹（仅 rs-meta-field）

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `wrapForm` | `Boolean` | `true` | 是否自动包裹 `<Form>`，放在已有 Form 内时设为 `false` |
| `mode` | `String` | `'twocolumn'` | Form 布局模式 |
| `labelWidth` | `Number` | `80` | 标签宽度 |
| `labelPosition` | `String` | `'right'` | 标签位置 |
| `disabled` | `Boolean` | `false` | 禁用 |

---

## override / overrides 属性清单

### 快捷属性（直接写在 override 里）

| 属性 | 作用 | 映射到 | 适用类型 |
|------|------|--------|---------|
| `type` | 控件类型 | `props.type` | 全部 |
| `label` | 标签文本 | `formItemProps.label` | 全部 |
| `readonly` | 只读 | `cellProps.disabled` | 全部 |
| `required` | 必填 | `formItemProps.required` + `props.nullable` | 全部 |
| `placeholder` | 占位提示 | `cellProps.placeholder` | text/textarea/number/select/datepicker/code |
| `dict` | 字典名 | `props.dict` + `cellProps.dict` | select |
| `items` | 字典筛选项 `['key1','key2']` | 配合 dict 使用，从字典中只显示指定 key 的选项，转为 `cellProps.datas` | select |
| `single` | 独占整行 | `formItemProps.single` | 全部 |
| `visibleIf` | 显隐条件 | `props.visibleIf` | 全部 |
| `updateFields` | 联动字段映射 `"本地,远程;本地,远程"` | `props.updateFields` | autocomplete/treepicker/fileupload/imageupload |
| `selType` | 选择器预设名（如 `'dept'`） | `cellProps.selConfig.selType` | autocomplete/treepicker/multiautocomplete |
| `apiCode` | 接口编码（覆盖预设） | `cellProps.selConfig.apiCode` | autocomplete/treepicker/multiautocomplete |
| `module` | 模块编码（覆盖预设） | `cellProps.selConfig.module` | autocomplete/treepicker/multiautocomplete |
| `keyName` | 值字段名 | `cellProps.selConfig.keyName` | autocomplete/treepicker/multiautocomplete |
| `titleName` | 显示字段名 | `cellProps.selConfig.titleName` | autocomplete/treepicker/multiautocomplete |
| `paramMappings` | 联动参数映射 `"本地,远程;..."` | `cellProps.selConfig.paramMappings` | autocomplete/treepicker/multiautocomplete |
| `defaultParams` | 默认过滤参数 `{}` | `cellProps.selConfig.defaultParams` | autocomplete/treepicker/multiautocomplete |

### cellProps 子属性（按控件类型）

```
text / textarea
  ├─ disabled        只读
  ├─ placeholder     占位
  ├─ maxlength       最大长度
  └─ readonly        只读(HTML原生)

number
  ├─ disabled        只读
  ├─ placeholder     占位
  ├─ precision       小数精度
  ├─ min             最小值
  ├─ max             最大值
  └─ step            步长

select
  ├─ disabled        只读
  ├─ dict            字典名(如 D0701)
  ├─ datas           选项数组 [{key,title}] 或 {key:title}
  ├─ items           字典筛选项 ['key1','key2']（配合 dict，只显示指定 key 的选项）
  ├─ placeholder     占位
  ├─ multiple        多选
  └─ filterable      可搜索

datepicker
  ├─ disabled        只读
  ├─ placeholder     占位
  ├─ format          日期格式(如 YYYY-MM-DD)
  ├─ noInput         不可手动输入
  ├─ start           开始日期限制
  ├─ end             结束日期限制
  └─ placement       弹出位置

checkbox
  ├─ disabled        只读
  ├─ trueValue       选中值(默认1)
  └─ falseValue      未选中值(默认0)

editor
  ├─ disabled        只读
  ├─ height          编辑器高度
  └─ toolbar         工具栏配置

autocomplete / treepicker / multiautocomplete
  ├─ option          选项配置(含loadData函数，通常由rs-form-edit注入)
  ├─ disabled        只读
  ├─ placeholder     占位(multiautocomplete)
  ├─ selConfig       选择器配置JSON
  ├─ titleName       显示字段名
  └─ keyName         值字段名

fileupload / imageupload
  ├─ disabled           只读
  ├─ uploaderOptions    上传配置 {multifile, mode, subtable, subMappings}
  └─ uploadSubtableConfig 子表绑定配置

fileuploadtpl
  ├─ disabled              只读
  └─ uploaderTplConfig     模板配置 {templateType, moduleCode, maxFileSize, showSelect}

code
  ├─ disabled        只读
  ├─ placeholder     占位
  └─ language        语言(sql/javascript/clike)

slot
  └─ (内容由父级插槽提供)
```

### 选择器快捷属性

autocomplete/treepicker/multiautocomplete 的选择器配置可以直接在 override 中写快捷属性，无需手写完整 JSON：

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `selType` | 预设名（如 `'dept'`、`'emp'`、`'cust'`） | - |
| `apiCode` | 接口编码（覆盖预设） | 预设的 apiCode |
| `module` | 模块编码（覆盖预设） | 预设的 module |
| `keyName` | 值字段名 | 预设的 keyName |
| `titleName` | 显示字段名 | 预设的 titleName |
| `parentName` | 父级字段名（treepicker 用） | 预设的 parentName |
| `paramMappings` | 联动参数映射 `"本地,远程;..."` | `""` |
| `defaultParams` | 默认过滤参数 `{}` | `null` |

只写要改的属性，其余从原配置或预设继承：

```javascript
// 只改 apiCode
{ selType: 'cust', apiCode: 'A08' }

// 改显示字段
{ selType: 'emp', titleName: 'EMPNAME' }

// 加联动参数 + 默认过滤
{ selType: 'emp', paramMappings: 'DEPTID,DEPTID', defaultParams: { STATUS: '1' } }

// 完全自定义接口
{ module: 'RS_M00', apiCode: 'A05', keyName: 'ID', titleName: 'DEPTNAME' }
```

---

## 选择器配置详解

### autocomplete（自动完成）/ treepicker（树选择器）

选择器配置写在 `tss_resuipc.SELECTDATA` 字段中，支持三种写法：

#### 写法1：预设名（字符串）

直接用 `selRegistry.js` 中注册的预设名：

```json
"dept"
```

可用预设：

| 预设名 | 说明 | 模块 | 接口 | 值字段 | 显示字段 |
|--------|------|------|------|--------|---------|
| `dept` | 部门 | RS_M00 | A05 | ID | DEPTNAME |
| `updept` | 部门(含上级) | RS_M00 | A04 | ID | DEPTNAME |
| `emp` | 员工 | RS_M00 | A06 | ID | EMPNAME |
| `emp-user` | 员工(按部门/功能点) | RS_M00 | A13 | ID | EMPNAME |
| `tstdd` | 测量标准 | RS_M00 | A07 | ID | STDDNAME |
| `cust` | 客户 | RS_M00 | A08 | ID | CUSTNAME |
| `ptmp` | 原始记录模板 | RS_M00 | A09 | ID | PTMPNAME |
| `accept` | 委托单 | RS_M00 | A10 | ID | ACCEPTNAME |
| `ard` | 标准器 | RS_M00 | A11 | ID | ARDNAME |
| `reguitem` | 规程制度 | RS_M00 | A12 | ID | REGUITEMNAME |
| `reg` | 行政区划 | RS_M00 | A15 | REGION_CODE | REGION_NAME |
| `dept-tree` | 部门树(treepicker专用) | RS_M00 | A04 | ID | DEPTNAME |

#### 写法2：预设 + 覆盖字段（JSON）

```json
{ "selType": "dept", "keyName": "ID", "titleName": "DEPTNAME" }
```

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `selType` | 预设名 | - |
| `keyName` | 值字段名（覆盖预设） | 预设的 keyName |
| `titleName` | 显示字段名（覆盖预设） | 预设的 titleName |
| `parentName` | 父级字段名（treepicker 树形结构） | 预设的 parentName |
| `paramMappings` | 联动参数映射，见下方 | `""` |
| `defaultParams` | 默认过滤参数（静态固定条件） | `null` |

#### 写法3：完全自定义（JSON）

```json
{
  "module": "RS_M00",
  "apiCode": "A05",
  "keyName": "ID",
  "titleName": "DEPTNAME"
}
```

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `module` | 模块编码 | - |
| `apiCode` | 接口编码（列表查询接口） | - |
| `keyName` | 值字段名 | `ID` |
| `titleName` | 显示字段名 | - |
| `parentName` | 父级字段名（treepicker 用） | `""` |
| `paramMappings` | 联动参数映射 | `""` |
| `defaultParams` | 默认过滤参数 | `null` |

#### 联动参数映射（paramMappings）

让选择器的查询接口接收当前表单其他字段的值作为过滤条件：

```json
{
  "selType": "emp",
  "paramMappings": "DEPTID,DEPTID;STATUS,STATUS"
}
```

格式：`"本地字段,远程参数名;本地字段,远程参数名"`

- `DEPTID,DEPTID` = 表单 DEPTID 字段值 -> 接口 FilterParams.DEPTID
- 用户在表单里选了部门后，员工选择器只显示该部门的员工

#### 默认过滤参数（defaultParams）

静态固定过滤条件，优先级低于 paramMappings 的动态值：

```json
{
  "selType": "emp",
  "defaultParams": { "STATUS": "1", "TYPE": "2" }
}
```

#### updateFields（联动写入多字段）

选中后把选中记录的多个字段值写入表单，配置在 `tss_resuipc.UPDATEFIELDS`：

```
DEPTID,ID;DEPTNAME,DEPTNAME
```

格式：`"本地字段,远程字段;本地字段,远程字段"`

- 选中员工后，`ID` 写入表单 `DEPTID` 字段，`DEPTNAME` 写入表单 `DEPTNAME` 字段
- override 中用 `updateFields` 属性覆盖

#### override 示例

```javascript
// 改用客户选择器
{ updateFields: 'CUSTID,ID;CUSTNAME,CUSTNAME' }

// 改用自定义接口
{
  cellProps: {
    selConfig: JSON.stringify({
      module: 'RS_M00',
      apiCode: 'A08',
      keyName: 'ID',
      titleName: 'CUSTNAME',
      paramMappings: 'TYPE,TYPE',
      defaultParams: { STATUS: '1' }
    })
  },
  updateFields: 'CUSTID,ID;CUSTNAME,CUSTNAME'
}
```

---

### multiautocomplete（多选自动完成）

在 autocomplete 基础上增加 `mode` 配置，支持两种模式：

#### 模式1：subtable 子表模式（默认）

选中项映射成子表行：

```json
{
  "selType": "accept",
  "mode": "subtable",
  "subtable": "DTSA",
  "subMappings": "ACCEPTID,ID;ACCEPTCODE,BILLCODE"
}
```

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `mode` | `subtable` | `subtable` |
| `subtable` | 子表路径名 | 字段名 |
| `subMappings` | 子表字段映射 `"子表字段,远程字段;..."` | - |

`subMappings` 格式：`"子表字段,远程字段;子表字段,远程字段"`

- 选中委托单后，`ID` 写入子表 DTSA 的 `ACCEPTID` 列，`BILLCODE` 写入 `ACCEPTCODE` 列

#### 模式2：field 字段模式

选中项的 key 拼成逗号分隔的 id 存入单字段：

```json
{
  "selType": "emp",
  "mode": "field",
  "field": "EMPIDS"
}
```

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `mode` | `field` | - |
| `field` | 存储字段名 | 字段名 |

存储结果如 `"id1,id2,id3"`

---

### fileupload / imageupload（文件/图片上传）

#### 模式1：单文件 + 联动（默认）

```json
{ "multifile": false }
```

选中文件后通过 `updateFields` 联动写入文件 ID 和文件名：

```
FILEID,id;FILENAME,name
```

#### 模式2：多文件逗号 id（multifile）

```json
{ "multifile": true }
```

多个文件的 id 拼成逗号分隔存入单字段：`"id1,id2,id3"`

#### 模式3：子表模式

每文件 = 子表一行：

```json
{
  "mode": "subtable",
  "subtable": "DTSA",
  "subMappings": "FILEID,id;FILENAME,name"
}
```

| 属性 | 说明 |
|------|------|
| `mode` | `subtable` |
| `subtable` | 子表路径名 |
| `subMappings` | `"子表字段,远程字段;..."` |

#### override 示例

```javascript
// 多文件模式
{ cellProps: { uploaderOptions: { multifile: true } } }

// 子表模式
{
  cellProps: {
    uploaderOptions: {
      mode: 'subtable',
      subtable: 'DTSA',
      subMappings: 'FILEID,id;FILENAME,name'
    }
  }
}
```

---

### fileuploadtpl（文件上传+模板选择）

```json
{
  "templateType": "YSJL",
  "moduleCode": "R01_M01",
  "maxFileSize": "10mb",
  "showSelect": true
}
```

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `templateType` | 模板类型编码 | - |
| `moduleCode` | 模块编码 | - |
| `maxFileSize` | 最大文件大小 | - |
| `showSelect` | 是否显示模板选择下拉 | `true` |

#### override 示例

```javascript
{
  cellProps: {
    uploaderTplConfig: {
      templateType: 'YSJL',
      moduleCode: 'R01_M01',
      showSelect: true
    }
  }
}
```

---

### select（下拉选择）

select 的选项来源有三种写法（写在 `tss_resuipc.SELECTDATA` 或 override 的 `dict`/`cellProps.datas` 中）：

#### 写法1：字典名

```javascript
{ dict: 'D0701' }
// 或
{ cellProps: { dict: 'D0701' } }
```

从 `store.state.app.dicts['D0701']` 读取选项（系统启动时全量加载）。

#### 写法2：JSON 数组

```json
[{"key":"1","title":"启用"},{"key":"0","title":"停用"}]
```

```javascript
{ cellProps: { datas: [{ key: '1', title: '启用' }, { key: '0', title: '停用' }] } }
```

#### 写法3：key:title 文本格式

```
1:启用,0:停用
```

存为 SELECTDATA 字符串，`gen.js` 会自动解析。

### formItemProps 子属性

```
formItemProps
  ├─ label           标签文本
  ├─ prop            字段名(校验用)
  ├─ required        是否必填
  ├─ showLabel       是否显示标签
  ├─ single          独占整行
  └─ rules           校验规则
```

### override 用法示例

```javascript
// 改类型+字典
{ type: 'select', dict: 'D0701', placeholder: '请选择' }

// 字典+筛选项（只显示 key 为 '1' 和 '2' 的选项）
{ type: 'select', dict: 'D0701', items: ['1', '2'] }

// 数字精度限制
{ type: 'number', cellProps: { precision: 2, min: 0, max: 9999 } }

// 日期范围
{ type: 'datepicker', cellProps: { format: 'YYYY-MM-DD', noInput: true } }

// 只读+必填+独占整行
{ readonly: true, required: true, single: true }

// 多选下拉
{ type: 'select', cellProps: { multiple: true, filterable: true } }

// 代码编辑器
{ type: 'code', cellProps: { language: 'javascript' }, single: true }
```

---

## 使用示例

### rs-meta-form

```html
<!-- 1. DataTable对象模式（通用模块/传统模块均可） -->
<rs-meta-form
  :path="$MAIN"
  module-code="LIB_M07"
  :overrides="{ CUSTNAME: { readonly: true }, STATE: { type: 'select', dict: 'D0701' } }"
/>

<!-- 2. 路径名+storeName模式 -->
<rs-meta-form
  path="MAIN"
  store-name="b01/m01"
  resource-name="VBS_CUST"
  :overrides="{ CUSTCODE: { readonly: true } }"
/>

<!-- 3. v-model普通对象模式（无Store依赖） -->
<rs-meta-form
  v-model="formData"
  :fields="customFields"
  :overrides="{ STATE: { type: 'select', dict: 'D07xx' } }"
  @change="onFieldChange"
/>

<!-- 4. 直接传fields数组 -->
<rs-meta-form
  :path="$MAIN"
  :fields="fields"
  mode="threecolumn"
  :label-width="100"
/>
```

### rs-meta-field

```html
<!-- 1. DataTable模式，只渲染CUSTNAME字段（自动包裹Form） -->
<rs-meta-field
  :path="$MAIN"
  field-name="CUSTNAME"
  module-code="LIB_M07"
  :override="{ readonly: true, label: '客户名称' }"
/>

<!-- 2. v-model模式，直接绑定字段值 -->
<rs-meta-field
  v-model="name"
  field-name="CUSTNAME"
  :fields="allFields"
  :override="{ required: true }"
/>

<!-- 3. 路径名+store模式 -->
<rs-meta-field
  path="MAIN"
  store-name="b01/m01"
  field-name="CUSTNAME"
  resource-name="VBS_CUST"
/>

<!-- 4. 直接传field配置对象 -->
<rs-meta-field
  v-model="amount"
  :field="{ props: { type: 'number', key: 'AMOUNT', formItemProps: { label: '金额' }, cellProps: { precision: 2 } } }"
  :override="{ cellProps: { min: 0 } }"
/>

<!-- 5. 放在已有Form内（关闭自动包裹） -->
<Form :mode="mode" :label-width="80">
  <rs-meta-field :path="$MAIN" field-name="CUSTNAME" :wrap-form="false" />
  <rs-meta-field :path="$MAIN" field-name="PHONE" :wrap-form="false" />
</Form>
```

---

## 暴露方法

### rs-meta-form

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `valid()` | `{ result: Boolean, ... }` | 表单校验 |
| `getModel()` | `Object` | 当前编辑行数据 |
| `getDataTable()` | `DataTable` | DataTable 对象（含变更追踪） |
| `applyFill(fields)` | - | AI 填充接口 |

### rs-meta-field

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `getDataTable()` | `DataTable` | DataTable 对象 |

---

## 事件

### rs-meta-form

| 事件 | 参数 | 说明 |
|------|------|------|
| `input` | `Object` | v-model 同步（普通对象模式） |
| `change` | `{ field, value, source }` | 字段变更（普通对象模式） |

### rs-meta-field

| 事件 | 参数 | 说明 |
|------|------|------|
| `input` | `Any` | 字段值变更 |
| `update-fields` | `Object` | autocomplete/treepicker 联动多字段写入 |

---

## 数据源说明

### 三种模式对比

| 模式 | path | value | storeName | 数据读写方式 |
|------|------|-------|-----------|-------------|
| DataTable对象 | `$MAIN` (Object) | - | - | 直接读写 DataTable |
| 路径名字符串 | `'MAIN'` (String) | - | `'b01/m01'` | 从 `$store.state[storeName].dt[pathName]` 获取 DataTable |
| 普通对象 | - | `{ F1: 'v1' }` | - | 内部创建适配器包装普通对象 |

### 普通对象适配器

v-model 模式下，组件内部创建轻量适配器模拟 DataTable 接口：

```javascript
{
  data: [obj],           // rs-form-edit 读 data[0] 作为 model
  scm: '',               // 空资源名（fields 外部传入）
  _path_: 'MAIN',
  setValue(field, v) { obj[field] = v; emit('input', obj); },
  getValue(field) { return obj[field]; },
}
```

---

## 字段配置格式

字段配置来自 `Gen.getFormFields(scmArray)`，每项结构：

```javascript
{
  props: {
    type,            // 控件类型
    key,             // 字段名
    nullable,        // 是否可空 (0=必填, 1=可空)
    dict,            // 字典名/SELECTDATA原始值
    updateFields,    // 联动字段映射
    visibleIf,       // 显隐条件
    formItemProps: {
      prop,          // 字段名(校验用)
      label,         // 标签文本
      showLabel,     // 是否显示标签
      required,      // 是否必填
      single,        // 独占整行
    },
    cellProps: {
      disabled,      // 只读
      placeholder,   // 占位
      dict,          // 字典名
      datas,         // 选项数组(select)
      precision,     // 精度(number)
      // ... 其他类型特有属性
    },
    cellOn: {},      // 事件绑定
  }
}
```

---

## 内部机制

### 字段加载流程

```
watch resourceName/moduleCode (immediate)
  -> _resolveResourceName() 推导资源名
    -> resourceName prop 优先
    -> moduleCode 推导: store.state.app.modules[moduleCode].MODPATH 找 MAIN 的 RESOURCENAME
    -> path.scm 兜底
  -> $store.dispatch('app/initScms', [resName])
  -> Gen.getFormFields(scm) 转为 fields 数组
  -> loadedFields 存入 data
```

### override 合并逻辑

```
_applyOverride(field)
  -> JSON.parse(JSON.stringify(field)) 深拷贝(避免污染scm缓存)
  -> 快捷属性映射(label/readonly/required/type/dict/placeholder/single/visibleIf/updateFields)
  -> cellProps 子属性透传
  -> formItemProps 子属性透传
```

### provide

```javascript
// rs-meta-form
provide() {
  return {
    visibilityHost: this,      // 字段显隐判断宿主，无 ISSHOW 方法时字段恒显
    subTableButtonsMap: {},
    aiFormModuleCode: this.moduleCode,
    aiFormStoreName: this.storeName,
  };
}
```
