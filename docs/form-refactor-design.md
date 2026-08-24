# Form 页重构：按钮统一 BTNAREA + 表单分组 Tab + 子表全走 tableblock

> 日期：2026-07-14
> 涉及组件：generic-form / rs-form-edit / gen.js / s01/m18 配置页

## 一、背景与问题

重构前 generic-form 表单页存在三个问题：

1. **子表渲染机制重叠**：同时存在两套
   - `rs-form-edit` 的 tableblock 字段（form 内嵌表格区块，按钮来自 `SELECTDATA.tableBlockConfig.buttons`）
   - generic-form 的独立子表 tab（`subTables` + `rs-table-edit`，按钮写死新增/删除）
   - 同一子表可能被两种机制重复渲染，按钮集还不一致。

2. **子表 tab 按钮写死**：新增/删除硬编码，无法配置权限/显隐/交互类型。

3. **表单不分组**：字段平铺，字段多时体验差。

## 二、重构目标（三个需求）

1. **所有按钮统一走 `tss_module_button` + `BTNAREA`**（含 tableblock 按钮，一刀切去掉 `SELECTDATA.buttons`）。
2. **表单按 uiset 表单分组（EDITGROUP）分 tab**，每个分组一个 tab。
3. **子表全部走 tableblock**（配在分组内），取消独立子表 tab。

## 三、BTNAREA 区域定义

| BTNAREA | 位置 | 典型按钮 |
|---|---|---|
| `header` | 列表页顶部 | 新增、导出、自定义 |
| `footer` | 列表底部 / form 底部 | 批量操作、审批流、保存/删除 |
| `row` | 表格行操作列 | 编辑、删除 |
| `subtable` | 子表 tableblock 工具栏 | 增、删、上移、下移、自定义 |

subtable 按钮额外用 `EXTPARAM.subtable` 指明归属哪个子表（如 `DTSA`）。

## 四、核心设计

### 1. 按钮统一 BTNAREA

- **subtable 按钮**：`tss_module_button` 里 `BTNAREA='subtable'`，`EXTPARAM={"subtable":"DTSA"}`。
- **generic-form** 新增 `subTableButtonsMap` computed：把 `BTNAREA='subtable'` 的按钮按 `EXTPARAM.subtable` 分组成 `{DTSA:[btn...], DTSB:[btn...]}`，通过 `provide` 注入 rs-form-edit。
- **rs-form-edit** 的 tableblock 按钮改从 `inject subTableButtonsMap[subtable]` 读；取不到时默认增删移兜底。
- **gen.js** tableblock 字段不再解析 `SELECTDATA.buttons/showButtons`（一刀切）。
- generic-form `visibleButtons` 排除 `BTNAREA='subtable'`，避免子表按钮漏到 form 底部。

### 2. 表单分组 Tab（EDITGROUP）

- `tss_resuipc` 加 `EDITGROUP` 字段（**数据层待加**），相同 EDITGROUP 值的字段归一个 tab，空值归"基本信息"。
- generic-form：
  - `mainScm`：MAIN 资源的原始 scm 字段。
  - `formGroups`：按 EDITGROUP 分组，每组用 `Gen.getFormFields(items)` 生成 rs-form-edit 的 fields。
  - `hasGroups`：分组数 > 1 才显示 Tabs，否则单 rs-form-edit（兜底）。
  - Tabs 每个分组一个 `rs-form-edit :fields="group.fields"`（rs-form-edit 已支持外部传 fields）。
  - `watch formGroups`：scm 异步加载后设默认选中首个分组。

### 3. 子表全走 tableblock

- 删除 generic-form 独立子表 tab 相关：`subTables` / `addDtsRow` / `removeDtsRow` / `getSubDt` / `getSubData` / `initSubTables`，以及 beforeCreate 里对子表的 `mapDateTable`（子表改由 store `dt[subtable]` 提供，tableblock 直接读）。
- 子表作为 `EDITTYPE='tableblock'` 字段配在 MAIN 资源的 uiset 里，排到某 EDITGROUP 下，随该分组 tab 显示。

## 五、代码改动清单

### `p-admin/src/utils/gen.js`
- tableblock 字段去掉 `buttons` / `showButtons` 解析（不再从 SELECTDATA 读按钮）。

### `p-admin/src/components/rs-form/rs-form-edit.vue`
- `inject` 新增 `subTableButtonsMap: { default: () => ({}) }`。
- `tableBlockButtons(field)`：优先用 `subTableButtonsMap[cfg.subtable]`，无配置时返回默认增删移。
- `onTableBlockBtn` 自定义分支：从 `subTableButtonsMap` 查按钮配置。

### `p-admin/src/components/generic-module/generic-form.vue`
- **删除**：`subTables` data、`initSubTables` / `addDtsRow` / `removeDtsRow` / `getSubDt` / `getSubData` 方法、beforeCreate 的子表 mapDateTable 块、template 的子表 tab。
- **新增 computed**：`mainScm`、`formGroups`（按 EDITGROUP 分组）、`hasGroups`、`subTableButtonsMap`（BTNAREA=subtable 按 EXTPARAM.subtable 分组）。
- `provide` 加 `subTableButtonsMap`。
- `visibleButtons` 过滤 `BTNAREA !== 'subtable'`。
- `watch.formGroups`：设默认 activeTab。
- template：`hasGroups` 时按分组 Tabs，每分组一个 `rs-form-edit :fields`；否则单 `rs-form-edit`。

### `p-admin/src/pages/s01/m18/views/config.vue`
- `btnAreaOptions` / `btnAreas`：`dts` → `subtable`。
- `btnForm` data 加 `SUBTABLE` 字段。
- `buildExtparam`：`BTNAREA==='subtable' && SUBTABLE` 时 `ext.subtable = SUBTABLE`。
- `parseExtparamToForm`：反序列化 `SUBTABLE`。
- template：`BTNAREA==='subtable'` 时显示"子表"选择 FormItem（`subPaths`）。
- `subPaths` computed：从 `MODPATHREF` 取子表 PATHNAMEB（返回 `[{key,title}]`）。
- `subButtonGroups` method：subtable 按钮按子表（`sub.key`）再分组，多子表各自显示。
- `openAddBtnModal(preset)`：支持预设 BTNAREA/SUBTABLE；子表分组标题旁"新增"按钮调 `openAddBtnModal({BTNAREA:'subtable', SUBTABLE:g.sub})`。

### `p-admin/src/pages/s01/m18/views/components/page-list.vue`
- BTNAREA 下拉选项 `subtable` + 子表列（`btnSubtable` / `syncBtnSubtable`）。
- `addButton`：原来用不存在的 `ADD_BUTTON` mutation → 改用 Store03 通用 `ADD`（补 path/ID/BTNNAME）。

## 六、数据层配置（让效果生效）

### 1. tss_resuipc 加 EDITGROUP 字段（待执行）
```sql
ALTER TABLE tss_resuipc ADD COLUMN EDITGROUP varchar(50) NULL;
-- 并在 ORM 元数据 tss_resfield 注册（VCK_RESUIPC 字段 REFFIELDID 指向物理字段）
-- 给字段填 EDITGROUP 值，相同值 = 同一个 tab
```

### 2. 子表配 tableblock 字段
在 MAIN 资源的 uiset 里加字段：
- `EDITTYPE = 'tableblock'`
- `SELECTDATA = {"subtable":"DTS"}`（指向子表路径名）
- `EDITSORT` 排到某个 EDITGROUP 的字段之后（随该分组 tab 显示）

### 3. subtable 按钮配置
`tss_module_button`：
- `BTNAREA = 'subtable'`
- `EXTPARAM = '{"subtable":"DTS"}'`
- `BTNCODE`：`add` / `remove` / `up` / `down`（标准，rs-form-edit 识别）或自定义 code
- `BTNNAME` / `ICON` / `COLOR` / `PERMCODE` / `SHOWCOND` / `INTERACTTYPE` 同普通按钮

## 七、数据流

```
按钮配置（s01/m18 / tss_module_button）
  BTNAREA='subtable' + EXTPARAM={"subtable":"DTS"}
  ↓
generic-form.subTableButtonsMap computed
  { DTS: [btn, btn...], DTSA: [...] }
  ↓ provide
rs-form-edit inject subTableButtonsMap
  ↓
tableblock 工具栏渲染（tableBlockButtons 取 subTableButtonsMap[subtable]）
```

```
表单分组（tss_resuipc.EDITGROUP）
  ↓
generic-form.mainScm → formGroups（按 EDITGROUP 分组 + Gen.getFormFields）
  ↓
Tabs 每分组一个 rs-form-edit :fields
  ↓
tableblock 字段随所在分组 tab 显示
```

## 八、顺带修复的 Bug

1. **page-list addButton 用了不存在的 `ADD_BUTTON` mutation** → 改用 Store03 通用 `ADD`（`{path:'MODBUTTON', item:{ID,...}}`）。
2. **btnAreas 仍是旧名 `dts`** → 改 `subtable`（否则 subtable 按钮添加后不显示，看着像"添加不成功"）。
3. **subButtonGroups 把 subPaths 的 `{key,title}` 对象当字符串比较** → 改用 `sub.key`。

## 九、待办

- [ ] 数据层：`tss_resuipc` 加 `EDITGROUP` 字段 + ORM 元数据注册（`ALTER TABLE` + `tss_resfield`）。
- [ ] s01/m18 uiset 配置：EDITTYPE=tableblock 字段的 SELECTDATA.subtable 编辑 UI。
- [ ] 验证 subtable 按钮按子表分组显示（subButtonGroups 用 sub.key 后）。
