# rs-form-edit / list-t01 overrides 字段覆盖说明

## 概述

`rs-form-edit` 和 `list-t01` 都支持通过 `overrides` prop 动态覆盖字段/列属性，无需修改 scm 元数据。

---

## rs-form-edit

### Props

| Prop | 类型 | 说明 |
|------|------|------|
| `fields` | `Array` | 直接传入字段配置数组（优先于 path.scm） |
| `overrides` | `Object` | 字段级覆盖 `{ 字段名: { 属性: 值 } }` |

### override 属性

| 属性 | 作用 | 示例 |
|------|------|------|
| `type` | 控件类型 | `'text'` / `'select'` / `'datepicker'` / `'number'` / `'autocomplete'` |
| `label` | 标签文本 | `'客户名称'` |
| `readonly` | 只读 | `true` |
| `required` | 必填 | `true` |
| `placeholder` | 占位 | `'请输入'` |
| `single` | 独占整行 | `true` |
| `dict` | 字典名 | `'D0701'` |
| `items` | 字典筛选项（配合 dict） | `['1', '2']` |
| `visibleIf` | 显隐条件 | `'ISSHOWADMIN'` |
| `updateFields` | 联动字段映射 | `'CUSTID,ID;CUSTNAME,CUSTNAME'` |
| `selType` | 选择器预设名 | `'cust'` / `'emp'` / `'dept'` |
| `apiCode` | 接口编码（覆盖预设） | `'A08'` |
| `module` | 模块编码（覆盖预设） | `'RS_M00'` |
| `keyName` | 值字段名 | `'ID'` |
| `titleName` | 显示字段名 | `'CUSTNAME'` |
| `paramMappings` | 联动参数映射 | `'DEPTID,DEPTID'` |
| `defaultParams` | 默认过滤参数 | `{ STATUS: '1' }` |
| `cellProps` | 任意 cellProps 子属性 | `{ precision: 2, min: 0 }` |
| `formItemProps` | 任意 formItemProps 子属性 | `{ rules: [...] }` |

### 用法

```html
<!-- 直接传 fields -->
<rs-form-edit :path="$MAIN" :fields="customFields" />

<!-- 传 overrides 覆盖 scm 字段属性 -->
<rs-form-edit
  :path="$MAIN"
  :overrides="{
    CUSTNAME: { readonly: true, label: '客户名称' },
    AMOUNT: { type: 'number', cellProps: { precision: 2, min: 0 } },
    STATE: { type: 'select', dict: 'D0701', items: ['1', '2'] },
    CUSTID: { type: 'autocomplete', selType: 'cust', titleName: 'CUSTNAME' }
  }"
/>

<!-- fields + overrides 同时使用 -->
<rs-form-edit
  :path="$MAIN"
  :fields="fields"
  :overrides="{ STATE: { type: 'select', dict: 'D0701' } }"
/>
```

### 暴露方法

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `valid()` | `{ result, ... }` | 表单校验 |
| `applyFill(fields)` | - | AI 填充 |
| `getModel()` | `Object` | 当前编辑行数据（`path.data[0]`） |

---

## list-t01

### Props

| Prop | 类型 | 说明 |
|------|------|------|
| `columnConfig` | `Array` | 直接传入列配置数组（优先于 path.scm 生成） |
| `columnOverrides` | `Object` | 列级覆盖 `{ 字段名: { 属性: 值 } }` |

### columnOverride 属性

| 属性 | 作用 | 示例 |
|------|------|------|
| `title` | 列标题 | `'客户名称'` |
| `width` | 列宽 | `200` |
| `minWidth` | 最小宽 | `100` |
| `maxWidth` | 最大宽 | `300` |
| `type` | 列类型 | `'text'` / `'index'` / `'pageaction'` |
| `dict` | 字典名 | `'D0701'` |
| `datas` | 选项数组 | `[{key:'1',title:'启用'}]` |
| `align` | 对齐 | `'left'` / `'center'` / `'right'` |
| `fixed` | 固定列 | `'left'` / `'right'` |
| `visibleIf` | 显隐条件 | `'ISSHOWADMIN'` |
| `perCode` | 权限码 | `'M01_EXPORT'` |
| `updateFields` | 联动字段 | `'CUSTID,ID'` |
| `selectData` | 选择器配置 | `'{"selType":"cust"}'` |
| 其他属性 | 任意透传 | `sort: true` |

### 用法

```html
<!-- 传 columnOverrides 覆盖 scm 列属性 -->
<list-t01
  :path="$QRY"
  :column-overrides="{
    CUSTNAME: { title: '客户', width: 200 },
    STATE: { dict: 'D0701', width: 100, align: 'center' },
    AMOUNT: { title: '金额', align: 'right' },
    ACTION: { fixed: 'right', width: 120 }
  }"
/>

<!-- 直接传 columnConfig -->
<list-t01
  :path="$QRY"
  :column-config="customColumns"
/>

<!-- columnConfig + columnOverrides -->
<list-t01
  :path="$QRY"
  :column-config="columns"
  :column-overrides="{ STATE: { dict: 'D0701' } }"
/>
```

### 暴露方法（通过 $refs.table）

| 方法 | 说明 |
|------|------|
| `$refs.table.columns` | 当前列配置数组 |
| `$refs.table.getColumns()` | 获取列（如有） |
| `$refs.table.query(1)` | 刷新列表 |

---

## 对比

| 特性 | rs-form-edit overrides | list-t01 columnOverrides |
|------|----------------------|------------------------|
| 覆盖目标 | 表单字段 (Gen.getFormFields) | 列表列 (Gen.getTableColumns) |
| 覆盖属性 | type/label/readonly/required/dict/selType 等 | title/width/dict/align/fixed 等 |
| 选择器快捷属性 | 支持 (selType/apiCode/module 等) | 不支持（列表不用选择器） |
| dict+items | 支持 | 用 datas 代替 |
| cellProps/formItemProps | 支持 | 不适用 |
| 触发时机 | created + watch.fields/overrides | computed 实时 |

---

## 在 SFC 扩展中使用

### 表单页扩展JS中设置 overrides

```javascript
// form.js 扩展
export default {
  mounted() {
    // 动态设置 rs-form-edit 的 overrides
    if (this.$refs.form) {
      this.$refs.form.overrides = {
        CUSTNAME: { readonly: this.STATE !== '1' },
        AMOUNT: { type: 'number', cellProps: { precision: 2 } },
      };
    }
  },
};
```

### 列表页通过 SFC slot 传 overrides

在 generic-module 中，list-t01 的 props 由 generic-module 透传。可通过 PAGECONFIG 配置或扩展JS 设置：

```javascript
// main.js 扩展
export default {
  computed: {
    // 动态计算列覆盖
    columnOverrides() {
      return {
        STATE: { dict: 'D0701', width: 100, align: 'center' },
        CUSTNAME: { title: this.keyword ? '客户(搜索)' : '客户' },
      };
    },
  },
};
```
