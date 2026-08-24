# rs-meta-query-panel / rs-meta-query-panel-field 查询面板组件

## 概述

- **rs-meta-query-panel** - 查询面板容器，基于 `rs-query-panel/index.vue` 模式，`<Row><Cell>` 网格布局，本地 `queryValues` 缓存，查询时同步到 DataTable
- **rs-meta-query-panel-field** - 查询单字段，`rr-flex-row` 布局（label + 控件横排），支持放在 panel 内或独立使用

与 `rs-meta-form` / `rs-meta-field` 的区别：

| 特性 | rs-meta-form / rs-meta-field | rs-meta-query-panel / rs-meta-query-panel-field |
|------|------|------|
| 布局 | `<Form>` + `<FormItem>` | `<Row><Cell>` + `rr-flex-row` |
| 数据绑定 | 实时双向绑定 DataTable | 本地缓存，查询时同步 |
| 字段过滤 | `EDITSORT > 0` | `QUERYSORT > 0` |
| 类型推导 | `EDITTYPE` | `QUERYTYPE` 优先，回退 `EDITTYPE` |
| 匹配方式 | 无 | `QUERYMODE`（like/eq/in/range） |
| 校验 | 支持必填 | 不校验 |
| 适用场景 | 新增/编辑表单 | 查询条件面板 |

## 文件位置

- `rs-query-panel/rs-meta-query-panel.vue` - 查询面板容器
- `rs-query-panel/rs-meta-query-panel-field.vue` - 查询单字段
- 全局注册：`src/components/index.js`

---

## rs-meta-query-panel

### Props

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `path` | `[Object, String, Array]` | `null` | QQRY DataTable 对象 / 数组(mapDateTable getter) / 路径名字符串 |
| `storeName` | `String` | `''` | `path` 为字符串时的 Vuex 命名空间 |
| `fieldsConfig` | `Array` | `null` | 直接传入 scm 原始字段数组（优先于 scm 读取） |
| `resourceName` | `String` | `''` | 按资源名从 `store.state.app.scms` 加载 |
| `moduleCode` | `String` | `''` | 按模块编码推导 resourceName（从 MODPATH.QQRY 查找） |
| `overrides` | `Object` | `{}` | 字段级覆盖 `{ BUSTYPEID: { type: 'select', dict: 'D0701' } }` |
| `showButtons` | `Boolean` | `true` | 是否显示搜索/重置按钮 |
| `cellWidth` | `Number` | `6` | 每个 Cell 的宽度（24 栅格制） |

### 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| `query` | `Object` (queryValues 副本) | 点击查询按钮，已同步到 DataTable |
| `reset` | - | 点击重置按钮，已恢复默认值 |

### 用法

```html
<!-- 1. 自动渲染所有查询字段 + 搜索/重置按钮 -->
<rs-meta-query-panel
  :path="qqryDt"
  module-code="LI_M00"
  @query="onQuery"
  @reset="onReset"
/>

<!-- 2. 字段级覆盖 -->
<rs-meta-query-panel
  :path="qqryDt"
  module-code="LI_M00"
  :overrides="{
    BUSTYPEID: { type: 'select', dict: 'D0701' },
    BILLDATE: { type: 'daterange' }
  }"
  @query="onQuery"
/>

<!-- 3. 手动指定字段（关闭自动按钮） -->
<rs-meta-query-panel :path="qqryDt" module-code="LI_M00" :show-buttons="false">
  <rs-meta-query-panel-field field-name="BUSTYPEID" />
  <rs-meta-query-panel-field field-name="STATE" :override="{ type: 'select', dict: 'D0701' }" />
  <Button color="primary" @click="$parent.onQuery">查询</Button>
</rs-meta-query-panel>

<!-- 4. 直接传字段配置 -->
<rs-meta-query-panel
  :path="qqryDt"
  :fields-config="customFields"
  @query="onQuery"
/>
```

### 暴露方法

| 方法 | 说明 |
|------|------|
| `onQuery()` | 触发查询（同步缓存到 DataTable + emit query） |
| `onReset()` | 重置查询条件 |

---

## rs-meta-query-panel-field

### 两种模式

组件自动检测是否在 `rs-meta-query-panel` 内：
- **panel 模式**：通过 `inject` 注入父 panel 的读写能力，从 `queryValues` 读写
- **独立模式**：自己管理值，从 `path`(DataTable/数组) 或 `v-model` 读写

### Props

| Prop | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `fieldName` | `String` | `''` | 字段名（按 RESFIELDNAME/FIELDNAME 匹配） |
| `fieldConfig` | `Object` | `null` | 直接传 scm 字段配置对象（优先于 fieldName 查找） |
| `path` | `[Object, String, Array]` | `null` | 独立模式：DataTable/数组/路径名 |
| `value` | `Any` | `''` | 独立模式：v-model 绑定值 |
| `storeName` | `String` | `''` | 独立模式：path 为字符串时的 Vuex 命名空间 |
| `fields` | `Array` | `null` | 独立模式：字段配置数组 |
| `resourceName` | `String` | `''` | 独立模式：按资源名加载 |
| `moduleCode` | `String` | `''` | 独立模式：按模块编码推导（从 MODPATH.QQRY 查找） |
| `override` | `Object` | `{}` | 字段级覆盖 |
| `labelWidth` | `Number` | `60` | 标签宽度 |

### 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| `input` | `Any` | 独立 v-model 模式：值变更 |
| `change` | `{ field, value }` | 值变更（所有模式） |

### override 属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `type` | 控件类型 | `'text'` / `'select'` / `'datepicker'` / `'daterange'` / `'number'` / `'autocomplete'` / `'textarea'` |
| `mode` | 匹配方式 | `'like'` / `'eq'` / `'in'` / `'range'` |
| `dict` | 字典名 | `'D0701'` |
| `items` | 字典筛选项（配合 dict，只显示指定 key 的选项） | `['1', '2']` |
| `datas` | 选项数组 | `[{key:'1',title:'启用'}]` |
| `label` | 标签文本 | `'客户名称'` |
| `placeholder` | 占位 | `'请输入'` |
| `selType` | 选择器预设名 | `'dept'` / `'emp'` / `'cust'` |
| `apiCode` | 接口编码（覆盖预设） | `'A08'` |
| `module` | 模块编码（覆盖预设） | `'RS_M00'` |
| `keyName` | 值字段名 | `'ID'` |
| `titleName` | 显示字段名 | `'CUSTNAME'` |
| `paramMappings` | 联动参数映射 | `'DEPTID,DEPTID'` |
| `defaultParams` | 默认过滤参数 | `{ STATUS: '1' }` |

### 用法

```html
<!-- 1. 放在 panel 内（panel 模式） -->
<rs-meta-query-panel :path="qqryDt" module-code="LI_M00" :show-buttons="false">
  <rs-meta-query-panel-field field-name="BUSTYPEID" />
  <rs-meta-query-panel-field field-name="STATE" :override="{ type: 'select', dict: 'D0701' }" />
</rs-meta-query-panel>

<!-- 2. 独立使用 - 从 DataTable 读写 -->
<rs-meta-query-panel-field :path="qqryDt" field-name="BUSTYPEID" module-code="LI_M00" />

<!-- 3. 独立使用 - v-model 模式 -->
<rs-meta-query-panel-field
  v-model="keyword"
  field-name="KEYWORD"
  :field-config="{ LABELNAME: '关键词', QUERYTYPE: 'text' }"
/>

<!-- 4. 日期范围 -->
<rs-meta-query-panel-field
  :path="qqryDt"
  field-name="BILLDATE"
  :override="{ type: 'daterange' }"
/>

<!-- 5. 多选下拉（QUERYMODE=in） -->
<rs-meta-query-panel-field
  :path="qqryDt"
  field-name="STATUS"
  :override="{ type: 'select', mode: 'in', dict: 'D0701' }"
/>

<!-- 6. 字典+筛选项（只显示部分选项） -->
<rs-meta-query-panel-field
  :path="qqryDt"
  field-name="STATUS"
  :override="{ type: 'select', dict: 'D0701', items: ['1', '2'] }"
/>

<!-- 7. 选择器（autocomplete） -->
<rs-meta-query-panel-field
  :path="qqryDt"
  field-name="CUSTID"
  :override="{ type: 'autocomplete', selType: 'cust', titleName: 'CUSTNAME' }"
/>
```

---

## 支持的控件类型

| type | 控件 | 说明 |
|------|------|------|
| `text` / `input` | `<input>` | 文本输入（默认） |
| `textarea` | `<textarea>` | 多行文本 |
| `number` | `<NumberInput>` | 数字输入 |
| `datepicker` | `<DatePicker>` | 日期选择 |
| `daterange` | `<DateRangePicker>` | 日期范围 |
| `select` | `<Select>` | 下拉选择（支持 `mode: 'in'` 多选） |
| `autocomplete` | `<AutoComplete>` | 自动完成（选择器） |
| `slot` | 具名插槽 | 自定义内容 |

## QUERYMODE 匹配方式

| mode | 说明 | 渲染效果 |
|------|------|---------|
| `like` | 模糊搜索 | 普通输入框 |
| `eq` | 精确匹配 | 普通输入框/select |
| `in` | 多值匹配 | 多选 Select |
| `range` | 范围匹配 | min/max 双输入框（非日期）或 DateRangePicker（日期） |

## SFC Slot 中使用

在 `simple-query` 插槽中用 `rs-meta-query-panel-field` 替代手写查询条件：

```html
<template>
  <div slot="simple-query">
    <Row :space="9" v-if="qqryDt">
      <Cell width="2"></Cell>
      <Cell width="11">
        <rs-meta-query-panel-field :path="qqryDt" field-name="BUSTYPEID" module-code="LI_M00" />
      </Cell>
      <Cell width="11">
        <rs-meta-query-panel-field :path="qqryDt" field-name="STATE" module-code="LI_M00" />
      </Cell>
    </Row>
  </div>
</template>
<script>
export default {
  props: { host: { type: Object, required: true } },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj || !this.host.storeObj.storeHelper) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    }
  },
  methods: {
    doSearch() {
      if (!this.qqryDt) return;
      this.host.$refs.list.query(1);
    }
  }
};
</script>
```
