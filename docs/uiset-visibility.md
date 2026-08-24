# UI 配置显隐条件（visibleIf）

通过页面配置（uiSetFull）驱动字段、按钮、列的显示/隐藏，业务页只需定义一个 computed/method 即可控制，无需修改组件代码。

## 核心约定

> **未配置 `visibleIf` 时，默认查找 `ISSHOW${key}`**（`ISSHOW` + 字段/按钮/列的 key）。

即：业务页定义了 `ISSHOWCUSTTYPE`，那么 CUSTTYPE 字段/列/查询项自动受它控制，配置里留空即可。

## 标准化上下文 ctx

显隐条件以 **method** 形式定义时，框架调用它并传入统一上下文：

```js
{ row, key, path }
```

| 字段 | 含义 | 适用场景 |
|---|---|---|
| `row` | 当前数据行 | 表单字段（= 表单 model）；按钮/列场景为 undefined |
| `key` | 字段/按钮/列的 key | 全部 |
| `path` | DataTable 路径名 | 全部，用于字段名重复时消歧 |

`path` 取值：

| 组件 | path |
|---|---|
| rs-form-edit（表单字段） | `MAIN` |
| rs-table-list（列表列/按钮） | `QRY` |
| rs-table-edit（子表编辑列，如 table-block） | `DTSA` / `DTSB` … |

## 两种形态（任选其一）

### 1) computed —— 无参，读业务页自身状态

适合只依赖页面级状态（选中行、权限等）的判断：

```js
// b01/m01/views/main.vue
computed: {
  // 列表选中行 > 0 才显示删除按钮（按钮 code='delete' → ISSHOWdelete）
  ISSHOWdelete() {
    return this.checks.length > 0;
  },
}
```

### 2) method —— 接收 ctx，按行数据判断

适合依赖当前行数据的判断：

```js
methods: {
  // STATE=1 时才显示 CUSTTYPE 字段
  ISSHOWCUSTTYPE({ row, key, path }) {
    if (!row) return true;
    return row.STATE === 1;
  },
}
```

## 求值规则（evalVisibility）

```
host（业务页）未定义该方法/computed  →  恒显（true）
值为 function                        →  调用 method(ctx)，取真值
否则（computed）                     →  取其值真值
```

- `host` 即 `visibilityHost`，由 `list01`/`add01` mixin 注入（= 业务页组件本身）。
- 未定义恒显，保证老页面/未配置字段不受影响。

## 显式配置（可选）

uiset 的 `VISIBLEIF` 字段可显式指定条件名，突破 `ISSHOW${key}` 默认命名：

- 留空 → `ISSHOW${key}`
- 填 `ISADMIN` → 查 `ISADMIN`（不限于 ISSHOW 前缀）

## 适用场景

| 场景 | 组件 | 默认条件名 | ctx |
|---|---|---|---|
| 表单字段 | rs-form-edit `isFieldVisible` | `ISSHOW${字段名}` | `{ row, key, path }` |
| 列表按钮 / 行按钮 | rs-table-list `isActionVisible` | `ISSHOW${按钮code}` | `{ key, path }` |
| 列表列 | rs-table-edit `isColumnVisible` | `ISSHOW${列key}` | `{ key, path }` |

## 完整示例

### 列表页：按钮 + 列 + 查询字段联动（b01/m01）

```js
// main.vue
import { list01 } from '@/mixins/list01';

export default {
  mixins: [list01],
  computed: {
    // 按钮：选中行才显示（code='delete' / 'export'）
    ISSHOWdelete() { return this.checks.length > 0; },
    ISSHOWexport() { return this.checks.length > 0; },
  },
  methods: {
    // 查询字段：行业 = 制造业时才显示县区
    ISSHOWCOUNTYNAME({ row, path }) {
      // QQRY 上读 CUSTTYPE（这里 row 为空，从 QQRY DataTable 取）
      const qqry = this.$store.state[Constants.STORE_NAME].dt.QQRY;
      return qqry && qqry.getValue('CUSTTYPE') === '制造业';
    },
  },
}
```

### 表单页：字段间联动（r02/m07）

```js
// add.vue
methods: {
  // 类型=证书(2) 时才显示证书编号字段（key=CERTNO）
  ISSHOWCERTNO({ row }) {
    return row && row.REFTYPE === '2';
  },
}
```

### 主表/子表同名字段消歧

主表和子表都有 STATE，用 `path` 区分：

```js
methods: {
  ISSHOWSTATE({ row, path }) {
    if (path === 'MAIN') return row.STATE >= 1;
    if (path === 'DTSA') return row.STATE === 2;
    return true;
  },
}
```

## 实现位置

| 文件 | 职责 |
|---|---|
| `src/utils/visibility.js` | `evalVisibility(host, visIf, ctx)` 统一求值（单一源） |
| `src/components/rs-form/rs-form-edit.vue` | `isFieldVisible` → 表单字段，传 `{ row: model, key, path }` |
| `src/components/rs-table/rs-table-list.vue` | `isActionVisible` → 按钮，传 `{ key, path }` |
| `src/components/rs-table/rs-table-edit.vue` | `isColumnVisible` → 列，传 `{ key, path }` |
| `src/utils/gen.js` | 从 scm 读 `VISIBLEIF` → 写入 `field.props.visibleIf` |

## 可见性配置流转

```
uiset 配 VISIBLEIF（可空）
  → tss_resuipc.VISIBLEIF
  → scm → gen.js getFormFields/getTableColumns → field.props.visibleIf
  → 组件 isFieldVisible/isActionVisible/isColumnVisible
  → 空则默认 'ISSHOW'+key
  → evalVisibility(host, visIf, ctx)
  → host 上的 computed/method 决定显隐
```
