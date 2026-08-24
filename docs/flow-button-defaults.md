# 按钮流程默认配置方案

## 概述

模块配置页面 (s01/m18) 新增页面时，根据模块的 FLOWCODE 自动生成对应的审批流按钮，避免用户手动逐个添加。

FLOWCODE 存储在 `tss_moudle.FLOWCODE`，前端通过 `modules[moduleCode].MOD.FLOWCODE` 读取。

## 流程类型与按钮配置

### FLOWCODE = 空（无审批流）

#### 列表页 (list)

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 添加 | header | custom | A04 | direct | - | h-icon-plus | primary | openForm | OPENMODE=add |
| 编辑 | row | custom | A02 | direct | - | h-icon-edit | - | openForm | OPENMODE=edit |
| 删除 | row | crud | A07 | poptip | - | h-icon-trash | red | api | POPTIPTEXT=确定删除？ |
| 导出 | header | crud | - | direct | - | h-icon-download | - | api | - |

#### 表单页 (form)

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 保存 | header | crud | A04 | direct | - | h-icon-save | primary | api | - |

---

### FLOWCODE = 1（提交→审核）

状态流转：待提交(1) → 待审核(2) → 已审核(3)

#### 列表页 (list)

基础按钮同无审批流，追加 footer 审批流按钮：

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 提交 | footer | flow | A17 | direct | `_checks_.every(r=>r.STATE===1)` | h-icon-complete | primary | api | - |
| 撤销提交 | footer | flow | A18 | poptip | `_checks_.every(r=>r.STATE===2)` | h-icon-undo | - | api | POPTIPTEXT=确定撤销提交？ |
| 审核 | footer | flow | A12 | direct | `_checks_.every(r=>r.STATE===2)` | h-icon-check | primary | api | - |
| 撤销审核 | footer | flow | A13 | poptip | `_checks_.every(r=>r.STATE===3)` | h-icon-undo | - | api | POPTIPTEXT=确定撤销审核？ |

#### 表单页 (form)

有审批流时，按钮统一放 footer（不再用 header）：

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 暂存 | footer | flow | A04 | direct | `STATE in [1]&&CREATEID==_USERID_` | h-icon-save | primary | api | - |
| 删除 | footer | flow | A07 | poptip | `STATE in [1]&&CREATEID==_USERID_&&ID!=null` | h-icon-trash | red | api | POPTIPTEXT=确定删除？ |
| 提交 | footer | flow | A17 | direct | `STATE in [1]&&CREATEID==_USERID_` | h-icon-complete | primary | api | - |
| 撤销提交 | footer | flow | A18 | poptip | `STATE===2&&CREATEID==_USERID_` | h-icon-undo | - | api | POPTIPTEXT=确定撤销提交？ |
| 审核 | footer | flow | A12 | direct | `STATE===2` | h-icon-check | primary | api | - |
| 撤销审核 | footer | flow | A13 | poptip | `STATE===3` | h-icon-undo | - | api | POPTIPTEXT=确定撤销审核？ |

---

### FLOWCODE = 2（提交→审核→审批）

状态流转：待提交(1) → 待审核(2) → 待审批(5) → 已审批(6)

在 FLOWCODE=1 基础上追加：

#### 列表页额外按钮

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 审批 | footer | flow | A14 | direct | `_checks_.every(r=>r.STATE in [5,19])` | h-icon-check | primary | api | - |
| 撤销审批 | footer | flow | A15 | poptip | `_checks_.every(r=>r.STATE in [6,20])` | h-icon-undo | - | api | POPTIPTEXT=确定撤销审批？ |

#### 表单页额外按钮

| 按钮 | BTNAREA | BTNTYPE | APICODE | INTERACTTYPE | SHOWCOND | ICON | COLOR | ACTIONTYPE | 其他 |
|------|---------|---------|---------|-------------|----------|------|-------|-----------|------|
| 审批 | footer | flow | A14 | direct | `STATE in [5,19]` | h-icon-check | primary | api | - |
| 撤销审批 | footer | flow | A15 | poptip | `STATE in [6,20]` | h-icon-undo | - | api | POPTIPTEXT=确定撤销审批？ |

---

## 与现有 generateFlowButtons 的差异

| 项目 | 现有 generateFlowButtons | 本方案（配置时写入） |
|------|------------------------|-------------------|
| 撤销审核 SHOWCOND(列表) | `STATE in [5,19]` | `STATE===3` |
| 撤销审核 SHOWCOND(表单) | `STATE in [5,19]` | `STATE===3` |
| 表单页按钮区域 | footer | footer（一致） |
| 保存按钮名称 | 暂存 | 暂存（一致） |
| 可见性 | 运行时自动合并，配置界面不可见 | 配置时写入 MODBUTTON，用户可编辑/删除/排序 |
| 去重 | 按 APICODE+BTNAREA 去重 | 同 |

**说明**：撤销审核的 SHOWCOND 区分 FLOWCODE：FLOWCODE=1 时为 `STATE===3`（已审核），FLOWCODE=2 时为 `STATE in [5,19]`（待审批/待发布）。因为 FLOWCODE=1 的终态是已审核(3)，撤销审核应从已审核回退到待审核；FLOWCODE=2 的审核后进入待审批(5)，撤销审核应从待审批回退到待审核。

## 实现要点

### 1. config.vue 新增 flowCode computed

```js
flowCode() {
  var appState = this.$store.state.app;
  var modData = appState && appState.modules && appState.modules[this.moduleCode];
  if (!modData || !modData.MOD) return '';
  return modData.MOD.FLOWCODE || '';
},
```

### 2. config.vue 新增 getFlowDefaultButtons(pageType, flowCode) 方法

根据 pageType 和 flowCode 返回按钮配置数组，逻辑与上述表格一致。

### 3. 修改 _addPageWithTpl

在模板按钮基础上，根据 `this.flowCode` 追加审批流按钮。追加时检查是否已存在同 APICODE+BTNAREA 的按钮，避免重复。

### 4. 新增"按流程生成按钮"功能

对已有页面，提供一键按钮：根据 FLOWCODE 补充缺失的审批流按钮（不覆盖已有按钮）。

### 5. 保留 generateFlowButtons 兜底

运行时 `generateFlowButtons` 仍保留，作为老数据（没有配置审批流按钮的页面）的兜底机制。当 MODBUTTON 中已有同 APICODE+BTNAREA 的 flow 按钮时，不再自动生成。

## 同步修改 generateFlowButtons

为保持一致性，`generic-module.vue` 的 `generateFlowButtons` 中撤销审核的 SHOWCOND 也改为 `STATE===3`（列表页和表单页）。
