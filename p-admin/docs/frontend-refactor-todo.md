# 前端违规清单（按 P0/P1/P2 分批迁移）

> 本清单由 grep `import db` / `db.postData|db.call|db.open|db.openTables|db.getNewID` / `this.$store.dispatch` 自动汇总。
> 数据采集日期：2026-07-19。
> **当前状态：全项目规范类违规（`db.postData/call/open` + `$store.dispatch` 业务调用）= 0。**

## 全部批次完成清单（2026-07-19）

### 第一批：核心模块（12 个 .vue）

| 模块 | 文件 | 备注 |
|---|---|---|
| s01/m25 | main.vue + install-modal.vue | loadPreviewScript/deleteTemplate/installTemplate 进 store |
| s01/mAIDev | workspace.vue | loadSessionDetail/saveSessionAsTemplate 进 store |
| r02/m07 | add.vue | loadAcceptRefs 进 store；onShow 改 $callAction |
| r01/m05 | main.vue + add.vue | checkLogisticsExists/loadProjectFee 进 baseStore；7 处 dispatch 改 $callAction |
| r01/m025 | main.vue + review.vue | 4 个新 action；9 处动态 dispatch 改 $callAction |
| r01/m026 | main.vue + review.vue | loadWtList/detectAnomalies 进 store |
| out/ecert | index.vue | 新建 store.js（函数导出模式） |
| out/logistics | index.vue | 新建 store.js（函数导出模式） |

### 第二批：P0 基础设施层

| 文件 | 备注 |
|---|---|
| list-t02.vue | 删死代码 import db + import heyui |
| add-t01.vue | 2 处 dispatch → $callAction |
| view-add-t01.vue | 2 处 dispatch → $callAction |
| rs-table-cell.vue | 删 unused titleName |
| generic-form.vue | 删 computed side effect + unused mc |

### 第三批：s01/m01-m12（24 个 .vue）

| 模块 | 文件数 | dispatch 转换数 |
|---|---|---|
| s01/m01 | 6 | 20 |
| s01/m02 | 2 | 4 |
| s01/m03 | 3 | 5 |
| s01/m04 | 2 | 3 |
| s01/m05 | 2 | 4 |
| s01/m07 | 2 | 3 |
| s01/m08 | 1 | 3 |
| s01/m09 | 1 | 3 |
| s01/m10 | 5 | 38 |
| s01/m12 | 2 | 0（仅 import-db disable） |

### 第四批：b01/m02-m06（6 个 .vue）

13 处 dispatch → $callAction。

### 第五批：r01/m01/m03/m06/m031

5 处 dispatch → $callAction + 1 处 import-db disable。

### 第六批：r01/m02 系列（15 个 .vue）

56 处 dispatch → $callAction + 6 处 import-db disable（getUrl 豁免）。

### 第七批：收尾

- `main/gonggaoDetail.vue`: 1 处 dispatch → $callAction
- `r01/m05/main1.vue`: import db 加 disable（getUrl 豁免）
- `r01/m05/logistics-add.vue`: 删死代码 import db
- `out/m01/main.vue + show.vue`: import db 加 disable（getUrl 豁免）

---

## 当前豁免场景汇总

### import db 保留（getUrl 纯函数豁免，已加 eslint-disable）

| 文件 | 用途 |
|---|---|
| out/m01/main.vue + show.vue | `db.getUrl('pdfsy')` PDF 预览 |
| r01/m025/review.vue | `db.getUrl('upload')` 文件下载 |
| r01/m02/main*.vue (5 个) | `db.getUrl('pdf')` / `db.getUrl('upload')` |
| r01/m02/attach-flow-panel.vue | `db.getUrl('upload')` 附件 URL |
| r01/m05/main.vue + main1.vue | `db.getUrl('pdf')` / `db.getUrl('upload')` |
| r01/m031/main.vue | `db.getUrl('pdf')` |
| s01/m07/excel-editor.vue | `db.getUrl('url')` Excel 编辑器 |
| s01/m12/main.vue + add.vue | `db.getUrl('url')` Word 模板 |
| s01/m10/main.vue | `db.getUrl('pdf')` |

### $store.dispatch 保留（框架级 action，已加 eslint-disable）

| action | 使用场景 |
|---|---|
| `app/initScms` | scm 元数据异步加载 |
| `app/initModule` | 模块配置异步加载 |
| `assistant/*` | AI 助手面板 |
| `formContext/*` | 表单上下文 |

### 白名单组件（ESLint overrides 豁免）

`rs-uploader/*`、`rs-uploader-template/*`、`rs-onlyoffice-preview/*`、`edit/ueditor/*`、`rs-word-template-editor/*`

### 假阳性（不算违规）

- `s01/m17/edit.vue:1284` — `defaultJsTemplate` 字符串模板内的示例代码
- `r02/m01/m02/m03` — `import db` 已注释掉

---

## 未处理

- **login/login.vue + login2.vue** — 登录页特殊流程，用户明确不处理

---

## pre-existing style errors（不在整改范围）

全项目仍有若干 pre-existing style error（eqeqeq、no-redeclare、no-unused-vars、citem 重复键等），已用 `--fix` 自动修复可修复部分，剩余需逐文件人工处理。


## 违规类型图例

| 类型 | 简写 | 说明 |
|---|---|---|
| `import db` | **I** | `.vue` 直接 `import db from '@/api/db'` |
| `db.xxx` | **D** | `.vue` 直接调用 `db.postData / db.call / db.open / db.openTables / db.getNewID` |
| `$store.dispatch` | **S** | `.vue` 跨过 `$callAction` 直接 `this.$store.dispatch(...)` |

## 严重度 / 批次 / 工作量

| 标记 | 含义 | 工作量 |
|---|---|---|
| **P0** | 基础设施，影响所有业务页，须优先迁移 | L |
| **P1** | 高密度业务页 / 关键新模块，迁移收益高 | M |
| **P2** | 低密度单页，迁移收益小 | S |

工作量：S = 单文件单一调用点；M = 多 action 需迁移；L = 跨组件耦合，需谨慎设计。

---

## 一、P0：基础设施层（优先迁移，影响面广）

| 文件 | 类型 | 备注 |
|---|---|---|
| `src/components/rs-template/list-t01.vue` | I + S + D | 列表模板基座，被所有列表页使用 |
| `src/components/rs-template/list-t02.vue` | I | 列表模板变体 |
| `src/components/rs-template/add-t01.vue` | S | 表单模板基座 |
| `src/components/rs-template/report-t01.vue` | I | 报表模板 |
| `src/components/rs-table/rs-table-cell.vue` | I | 表格单元格（评估是否豁免） |
| `src/components/rs-table/rs-table-edit.vue` | S | 表格编辑态 |
| `src/components/rs-table/rs-table-list.vue` | S | 表格列表态 |
| `src/components/rs-form/rs-form-edit.vue` | S | 表单编辑组件 |
| `src/components/rs-form/rs-editor.vue` | I | 富文本框（评估是否豁免） |
| `src/components/views/view-add-t01.vue` | S | 视图模式表单基座 |
| `src/components/generic-module/generic-module.vue` | I + S | 通用模块（GenericModule） |
| `src/components/generic-module/generic-form.vue` | S | 通用表单 |
| `src/store/createStore.js` | —（豁免） | Store 框架自身 |
| `src/store/Store03.js` | —（豁免） | Store 框架自身 |
| `src/utils/extends.js` | —（豁免） | `$callAction` 实现 |

---

## 二、P1：高密度业务页 / 新开发模块

### s01/m17 代码在线开发（最新开发，违反严重）

| 文件 | 类型 | 行号（db.xxx） |
|---|---|---|
| `src/pages/s01/m17/views/edit.vue` | I + S + D | 643, 690, 871, 1261 |
| `src/pages/s01/m17/components/file-tree.vue` | I + D | 80 |

### s01/m18 模块配置（最新开发）

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/s01/m18/views/config.vue` | I + S + D | 1016, 2015 |
| `src/pages/s01/m18/views/components/module-wizard.vue` | I + S + D | 382, 398, 417, 724, 747, 755 |
| `src/pages/s01/m18/views/components/page-preview.vue` | S | — |

### s01/m22 版本中心

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/s01/m22/views/main.vue` | I + D | 205, 218, 241, 267, 283, 302 |
| `src/pages/s01/m22/views/components/version-diff-modal.vue` | I + D | 91, 113 |

### s01/m25 模板市场 / s01/mAIDev 工作台

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/s01/m25/views/main.vue` | I + S + D | 66 |
| `src/pages/s01/m25/views/components/install-modal.vue` | I + D | 71 |
| `src/pages/s01/mAIDev/views/workspace.vue` | I + D | 282, 651 |

### components/generic-module（最近新写的弹窗组件）

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/components/generic-module/code-editor-popup.vue` | I + S + D | 289, 307, 370, 577, 581, 658, 684, 704, 728, 822 |
| `src/components/generic-module/code-test-panel.vue` | I + D | 130, 160, 180（**示范 B 已迁移**） |
| `src/components/generic-module/sfc-editor-popup.vue` | I + S + D | 241(注释), 250(注释), 642, 678 |
| `src/components/generic-module/version-history-popup.vue` | I + D | 141, 163, 180, 196, 211 |
| `src/components/generic-module/script-flow-editor.vue` | I + S + D | 294, 340, 369, 414 |

### r01/m02 记录管理（高密度违规）

| 文件 | 类型 |
|---|---|
| `src/pages/r01/m02/views/main.vue` | I + S |
| `src/pages/r01/m02/views/main1.vue` | I + S |
| `src/pages/r01/m02/views/main2.vue` | I |
| `src/pages/r01/m02/views/main3.vue` | I |
| `src/pages/r01/m02/views/main4.vue` | I |
| `src/pages/r01/m02/views/add.vue` | S |
| `src/pages/r01/m02/views/add1.vue` | S |
| `src/pages/r01/m02/views/add2.vue` | S |
| `src/pages/r01/m02/views/add3.vue` | S |
| `src/pages/r01/m02/views/add4.vue` | S |
| `src/pages/r01/m02/views/ardSel.vue` | S |
| `src/pages/r01/m02/views/logList.vue` | S |
| `src/pages/r01/m02/views/tmpSel.vue` | S |
| `src/pages/r01/m02/views/tmpSel2.vue` | S |
| `src/pages/r01/m02/views/attach-flow-panel.vue` | I |

### s01/m01 资源管理

| 文件 | 类型 |
|---|---|
| `src/pages/s01/m01/views/main.vue` | I + S |
| `src/pages/s01/m01/views/add.vue` | S |
| `src/pages/s01/m01/views/fieldSel.vue` | S |
| `src/pages/s01/m01/views/refSet.vue` | S |
| `src/pages/s01/m01/views/uiSet.vue` | S |
| `src/pages/s01/m01/views/uiSetFull.vue` | S |

### s01/m02 字段管理

| 文件 | 类型 |
|---|---|
| `src/pages/s01/m02/views/main.vue` | I + S |
| `src/pages/s01/m02/views/add.vue` | S |

### s01/m03 过滤器 / s01/m04 模块 / s01/m10

| 文件 | 类型 |
|---|---|
| `src/pages/s01/m03/views/main.vue` | I + S |
| `src/pages/s01/m03/views/add.vue` | S |
| `src/pages/s01/m03/views/apiSel.vue` | S |
| `src/pages/s01/m04/views/add.vue` | S |
| `src/pages/s01/m04/views/powerSet.vue` | S |
| `src/pages/s01/m10/views/main1.vue` | S |
| `src/pages/s01/m10/views/add.vue` | S |
| `src/pages/s01/m10/views/add1.vue` | S |
| `src/pages/s01/m10/views/add2.vue` | S |
| `src/pages/s01/m10/views/ardSel.vue` | S |

### s01/m05 员工管理（示范 A 已重构 store.js）

| 文件 | 类型 |
|---|---|
| `src/pages/s01/m05/views/deptSet.vue` | S |
| `src/pages/s01/m05/views/roleSet.vue` | S |

### r01/m025 / r01/m026 记录

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/r01/m025/views/main.vue` | I + S + D | 251 |
| `src/pages/r01/m025/views/review.vue` | I + D | 698, 1098, 1328 |
| `src/pages/r01/m026/views/main.vue` | I + D | 238 |
| `src/pages/r01/m026/views/review.vue` | I + D | 498 |

### r01/m05 / r01/m031 / r02 系列

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/r01/m05/views/main.vue` | I + S + D | 478 |
| `src/pages/r01/m05/views/main1.vue` | I | — |
| `src/pages/r01/m05/views/add.vue` | I + S + D | 247 |
| `src/pages/r01/m05/views/add1.vue` | S | — |
| `src/pages/r01/m05/views/logistics-add.vue` | I | — |
| `src/pages/r01/m031/views/main.vue` | I + S | — |
| `src/pages/r02/m01/views/main.vue` | I | — |
| `src/pages/r02/m02/views/main.vue` | I | — |
| `src/pages/r02/m03/views/main.vue` | I | — |
| `src/pages/r02/m07/views/main.vue` | S | — |
| `src/pages/r02/m07/views/add.vue` | I + S + D | 93 |

---

## 三、P2：低密度单页

| 文件 | 类型 | 行号 |
|---|---|---|
| `src/pages/b01/m02/views/add.vue` | S | — |
| `src/pages/b01/m03/views/add.vue` | S | — |
| `src/pages/b01/m04/views/add.vue` | S | — |
| `src/pages/b01/m04/views/ardSel.vue` | S | — |
| `src/pages/b01/m05/views/add.vue` | S | — |
| `src/pages/b01/m06/views/add.vue` | S | — |
| `src/pages/s01/m07/views/add.vue` | S | — |
| `src/pages/s01/m07/views/editTemplate.vue` | S | — |
| `src/pages/s01/m07/views/components/excel-editor.vue` | I | — |
| `src/pages/s01/m08/views/add.vue` | S | — |
| `src/pages/s01/m09/views/add.vue` | S | — |
| `src/pages/s01/m12/views/main.vue` | I | — |
| `src/pages/s01/m12/views/add.vue` | I | — |
| `src/pages/r01/m01/views/add.vue` | S | — |
| `src/pages/r01/m06/views/add.vue` | S | — |
| `src/pages/login/login.vue` | S（已注释） | — |
| `src/pages/login/login2.vue` | S（已注释） | — |
| `src/pages/main/views/gonggaoDetail.vue` | S | — |
| `src/pages/out/m01/views/main.vue` | I | — |
| `src/pages/out/m01/views/show.vue` | I + S | — |
| `src/pages/out/logistics/index.vue` | I + D | 73 |
| `src/pages/out/ecert/index.vue` | I + D | 156, 180 |
| `src/components/ai/blocks/NavigateBlock.vue` | S | — |
| `src/components/assistant/AssistantDrawer.vue` | S | — |
| `src/components/edit/table/index.vue` | S | — |

---

## 四、白名单（豁免，不计违规）

以下目录 / 文件**不**走 `api/data/call` 通用通道，专用 endpoint，本轮豁免：

| 目录 / 文件 | 说明 |
|---|---|
| `src/components/rs-uploader/*` | 文件上传 `/api/upload` |
| `src/components/rs-uploader-template/*` | 模板上传 |
| `src/components/rs-onlyoffice-preview/*` | OnlyOffice `/field-queue` |
| `src/components/edit/ueditor/*` | UEditor 富文本 |
| `src/components/rs-word-template-editor/*` | Word 模板 |
| `src/store/createStore.js`、`Store03.js`、`BaseStore.js`、`SelStore.js` | Store 框架自身 |
| `src/utils/extends.js` | `$callAction` 实现 |
| `src/api/db.js` | 网络层 |

> `src/components/rs-form/rs-editor.vue`、`src/components/rs-table/rs-table-cell.vue` 评估后定夺。

---

## 五、批次推进建议

### 批次 1（已落地）
- 规范文档 `docs/frontend-store-convention.md`
- ESLint 规则 `.eslintrc.js`
- CLAUDE.md 增量
- 示范 A：`src/pages/s01/m05/store.js`（轻度，换注册入口）
- 示范 B：`src/components/generic-module/code-test-panel.vue`（重度，抽 store + $callAction）

### 批次 2（P0 基础设施）
- `list-t01.vue` / `list-t02.vue` / `add-t01.vue` / `view-add-t01.vue`
- `rs-table-list.vue` / `rs-table-edit.vue` / `rs-form-edit.vue`
- `generic-module.vue` / `generic-form.vue`

### 批次 3（P1 新开发模块）
- `s01/m17` 系列（edit.vue + file-tree.vue + code-editor-popup.vue + sfc-editor-popup.vue + version-history-popup.vue + script-flow-editor.vue）
- `s01/m18` 系列（config.vue + module-wizard.vue + page-preview.vue）
- `s01/m22` 系列（main.vue + version-diff-modal.vue）
- `s01/m25` + `s01/mAIDev`

### 批次 4（P1 业务页）
- `r01/m02` 全系列
- `s01/m01` + `s01/m02` + `s01/m03` + `s01/m04` + `s01/m10`
- `r01/m025` + `r01/m026` + `r01/m05` + `r01/m031`
- `r02/m01~m07`

### 批次 5（P2 单页）
- 各 `*/add.vue` 的 `$store.dispatch` 改 `$callAction`
- 外部页 `out/*`

每批：改代码 → `npm run lint` → `npm run dev` 手测 → 提交。

---

## 附录：违规统计（采集日期 2026-07-19）

| 类型 | 文件数 |
|---|---|
| `.vue` 中 `import db` | 52 |
| `.vue` 中 `db.xxx(...)` 调用点 | 约 60+（分布在 30+ 文件） |
| `.vue` 中 `this.$store.dispatch(...)` | 约 70 文件 |

去重合并后约 80+ 文件需要迁移。建议优先完成 P0（约 10 个文件）后整体回归测试，再批量推进 P1/P2。
