# 华溯低代码开发体系 — 架构分析与演进规划

> 生成日期: 2026-07-09

---

## 〇、模块加载运行机制

> 这是理解整个低代码体系的基础。模块是元数据驱动的运行时配置单元。

### 完整链路

```
用户点击菜单(key="r02/m07", FUNCCODE="R02_M07")
  │
  ├─ main.vue.select()
  │   ├─ dispatch app/initModule("R02_M07")
  │   │   └─ POST /api/outer/call/RS_M00/A03 {MODULECODE:"R02_M07"}
  │   │       → 后端 MOUDLE.Open() 查4张表:
  │   │         VSS_MOUDLE → 模块基本信息(含FLOWCODE)
  │   │         VSS_MOUDLEPATH → 数据源(QRY/MAIN/DTSA...)
  │   │         VSS_MOUDLEPATHREL → 主子表关系
  │   │         VSS_MOUDLEAPI → 接口(A01query/A02open/A04save...)
  │   │       → 存入 store.state.app.modules["R02_M07"] (懒加载,首次点击时加载,缓存)
  │   │
  │   └─ router.push({name:"r02/m07"})
  │
  ├─ 路由匹配 → 懒加载 chunk → 执行 store.js
  │   └─ createStore.getStore({moduleCode:"R02_M07", storeName:"r02/m07"})
  │       ├─ new Store03 → ensureModule() → 从 app.modules 读取(已加载)
  │       ├─ moudle.getPaths() → {QRY:"VCK_XX", MAIN:"VCK_XX", DTSA:"VCK_XX_DTS"}
  │       └─ 为每个 path 创建 DataTable(path, RESOURCENAME)
  │
  └─ main.vue 渲染
      ├─ <list-t01 :store="store">
      │   ├─ mounted → dispatch app/initScms(["VCK_XX"]) → 查 tss_resuipc (懒加载,缓存)
      │   ├─ setColumns() → 从 scm 生成表格列
      │   └─ query() → POST /api/data/call/R02_M07/A01/ → 显示数据
      └─ clickRow → <rs-form-edit> → 从 scm 生成表单字段
```

### 关键认知

1. **模块 = 运行时配置包**：包含 MODPATH(数据源映射) + MODAPI(接口定义) + MODPATHREF(主子表关系)。前端不硬编码API路径，全部从模块配置动态获取。
2. **initModule 是懒加载**：模块配置不在登录时全量加载，而是用户首次点击对应菜单时按需加载，存入 `app.modules[moduleCode]` 缓存。
3. **initScms 也是懒加载**：UI字段配置(tss_resuipc)在 rs-table-list/rs-form-edit 的 mounted/created 时按需加载，存入 `app.scms[RESOURCENAME]` 缓存。
4. **一个模块可对应多个页面**：LI_M02 有5个路由(列表/审核/审批/签发/查询下载)共享同一个 store，区别仅在于用不同接口查询(A01/A011/A012/A013/A014)。
5. **模块与页面的关系是隐式的**：靠 FUNCCODE=MODULECODE 的命名约定关联，没有元数据描述"一个模块有哪些页面"。
6. **按钮完全硬编码**：list-t01/rs-form-edit 不渲染底部按钮，全部由业务 Vue 文件手写。

### 核心文件

| 文件 | 职责 |
|------|------|
| `p-admin/src/store/modules/app.js` | app Vuex模块：菜单/字典/scm/模块配置的加载与存储 |
| `p-admin/src/store/createStore.js` | 工厂函数：创建命名空间Vuex模块并注册 |
| `p-admin/src/store/Store03.js` | 核心Store类：Moudle封装、ensureModule、CRUD actions |
| `p-admin/src/router/index.js` | 路由：require.context自动加载、beforeEach模块加载 |
| `p-admin/src/components/main.vue` | 主布局：菜单渲染、select→initModule→router.push |
| `p-admin/src/mixins/add01.js` | 审批流按钮显隐核心(ISSHOW* computed) |
| `netcore/Realso.WebAPI/Models/MOUDLE.cs` | 后端模块模型：Open加载MODPATH/MODPATHREF/MODAPI |
| `netcore/Realso.WebAPI/Controllers/DataController.cs` | 数据控制器：Call→switch(APITYPE)→doQuery/doOpen/doSave等 |

---

## 一、现有六层架构全景

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 6: 前端业务页面 (b01/m01, r01/m02, ...)              │
│  用户最终看到的页面                                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ 极低复杂度│  │ 低复杂度  │  │ 中复杂度  │  │ 高复杂度  │   │
│  │ 纯配置驱动│  │ 配置+少量 │  │ 批量/图表 │  │ 完全自定义│   │
│  │ list-t01 │  │ 自定义方法│  │ 跨模块联动│  │ SFC/手写  │   │
│  │ +add01   │  │          │  │          │  │          │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
│       40%          25%          20%           15%            │
└─────────────────────────────────────────────────────────────┘
        ▲ 由谁生成？
        │
┌───────┴─────────────────────────────────────────────────────┐
│  Layer 5: SFC在线开发 (m17)                                  │
│  在线编写/编译/部署 Vue 组件，覆盖高复杂度场景                 │
└─────────────────────────────────────────────────────────────┘
        ▲ 注册入口
        │
┌───────┴─────────────────────────────────────────────────────┐
│  Layer 4: 功能管理 (m03) — 菜单 + 权限                       │
└─────────────────────────────────────────────────────────────┘
        ▲ 组装逻辑
        │
┌───────┴─────────────────────────────────────────────────────┐
│  Layer 3: 模块管理 (m02) — 数据源 + 接口 + 关系              │
│           + SQL配置 (m13) — 复杂查询模板                      │
└─────────────────────────────────────────────────────────────┘
        ▲ 定义数据
        │
┌───────┴─────────────────────────────────────────────────────┐
│  Layer 2: 资源管理 (m01) — 表/视图/字段/过滤器/UI配置         │
└─────────────────────────────────────────────────────────────┘
        ▲ 物理存储
        │
┌───────┴─────────────────────────────────────────────────────┐
│  Layer 1: 数据库 — MySQL 物理表                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 二、各层模块关系与数据流

### 2.1 配置阶段（管理员操作）

```
资源管理(m01) → 定义表/视图/字段/过滤器/UI配置
SQL配置(m13)  → 定义复杂查询模板
模块管理(m02) → 组装资源+SQL+接口 → 生成可调用模块
功能管理(m03) → 注册菜单+权限 → 用户可见可操作
SFC开发(m17)  → 编写自定义页面 → 补充标准组件覆盖不到的场景
```

### 2.2 运行阶段（用户操作）

```
用户点击菜单 → FUNCCODE匹配MODULECODE → 加载模块配置
→ 前端store.initModule → 读取moudlepath/moudleapi
→ RsTableList/RsFormEdit 根据resuipc渲染列表/表单
→ api/Data/call/{Module}/{ApiCode} → ORM动态构建SQL执行
→ 或: sfc-loader加载SFC组件 → 自定义页面渲染
```

### 2.3 各层定位与覆盖率

| 层 | 模块 | 解决什么问题 | 覆盖率 |
|---|------|------------|--------|
| **数据层** | 资源管理(m01) | 数据从哪来、长什么样 | 100% — 所有CRUD都依赖 |
| **逻辑层** | 模块管理(m02) + SQL配置(m13) | 业务逻辑怎么执行 | ~80% — 标准CRUD覆盖，复杂逻辑需自定义Controller |
| **表现层** | UI设置(m01子功能) + SFC(m17) | 界面怎么展示 | ~60% — 标准列表/表单覆盖，复杂交互需SFC |
| **入口层** | 功能管理(m03) | 用户怎么访问 | 100% — 所有菜单都依赖 |

---

## 三、前端页面复杂度谱系分析

### 3.1 统计范围

共分析 4 个业务目录、22 个模块、34 个 Vue 页面文件：
- `b01/` 基础数据（12个模块）
- `r01/` 报告/检验（8个模块）
- `r02/` 记录/报表（4个模块）
- `cgdd/` 采购订单（Demo代码，非生产）

### 3.2 复杂度分级

#### 极低复杂度 (~40%) — 纯配置驱动

| 模块 | 说明 |
|------|------|
| b01/m05 (add) | 最典型标准add页面：view-dialog + rs-form-edit + Add01+Sel01 mixin，methods为空 |
| b01/m07 | 资质证书管理，标准list+add |
| b01/m08 | 人员授权管理，标准list+add |
| b01/m010 | 能力确认，标准list+add |
| b01/m012 | 人员监督，标准list+add |

特征：main.vue 约13-16行template / 30行script；add.vue 约16行template / 11行script；0-2个自定义methods。

#### 低复杂度 (~25%) — 标准页面+少量增强

| 模块 | 增强点 |
|------|--------|
| b01/m01 | 高级查询按钮(showQuery) |
| b01/m02 | add.vue有AutoComplete选择器+RsUploader |
| b01/m04 | add.vue有TreePicker(部门树) |
| b01/m09 | add.vue有子表DTSA+addDts/removeDts |
| b01/m011 | add.vue有省市区三级联动AutoComplete |
| r01/m01 | 标准list+add，add有AutoComplete |

特征 |

特征：1-4个自定义methods；少量自定义组件import（AutoComplete/RsUploader/TreePicker）；无特殊API调用。

#### 中等复杂度 (~20%) — 批量操作/图表/跨模块

| 模块 | 核心复杂度来源 |
|------|---------------|
| b01/m06 (add) | 6个Tab页（基本信息+5个子表），多个AutoComplete+RsUploader |
| b01/m03 (add) | 多个AutoComplete（部门/员工/标准器）+文件上传 |
| r01/m03 (main) | checkbox选择+批量收费/折扣/撤销操作 |
| r01/m06 (main) | 同上，委托管理的批量操作 |
| r02/m01,m02,m03 | report-t01报表模板+ECharts图表+自定义initData数据加工 |
| r02/m07 | ensureModuleLoaded跨模块初始化+loadAcceptRefs直调A02 API |

特征：5-10个自定义methods；有批量操作按钮；或ECharts图表；或跨模块store初始化。

#### 高/极高复杂度 (~15%) — 完全自定义页面

| 模块 | 核心复杂度来源 |
|------|---------------|
| r01/m02 (main) | checkbox+15+个footer按钮（提交/撤销/审核/审批/证书生成/打印/下载），rs-print-pdf，List01 mixin |
| r01/m02 (add) | **最复杂add页面**：左侧受理单列表+右侧rs-edit-item动态模板渲染（非rs-form-edit），ardSel+tmpSel+attachFlowPanel，dealConfigSelect递归处理模板配置树，inputObj/tableObj/editorObj三层状态管理，自定义save/submit，完整审核审批流程 |
| r01/m031 (main) | **完全自定义费用管理面板**：自定义Table（非list-t01），父子节点checkbox级联选中逻辑，Tabs明细/汇总双视图，自定义分页，calcTableHeight动态高度，getTreeData树形数据递归构建，sumTotalAMT汇总计算 |
| r01/m05 (main) | **最复杂main页面**：checkbox+14+个footer操作按钮，9个ISSHOW计算属性按状态/部门/创建人判断可见性，addLogistics跨模块调用（initModule R02_M07），rsLogisticsAdd跨模块组件引入 |
| r01/m05 (add) | AutoComplete联动（选择客户自动填充联系人/电话/地址），选择模板后从TSS_PROJECT_FEE查费用，费用自动计算watch(4个)，getBillCode2单据号生成 |
| r01/m025 (main) | 完全自定义面板（非list-t01），双表格（委托单列表+明细列表），db.postData直调A53 API，动态import review.vue审批弹窗 |
| r01/m026 (main) | 同m025，委托审批版本 |

特征：15+个自定义methods；完全脱离list-t01标准组件；DOM级操作；模板引擎渲染；跨模块组件/API联动。

### 3.3 复杂度谱系可视化

```
极低 ←————————————————————————————————————————————→ 极高

b01/m07  b01/m08  b01/m010  b01/m012  b01/m05(add)
  │
  ├─ b01/m01  b01/m02  b01/m04  b01/m09  r01/m01
  │   b01/m011(main)  r01/m03(add)  r01/m031(add)
  │
  │   ├─ b01/m06(add)  b01/m03(add)  b01/m011(add)
  │   │  r01/m03(main)  r01/m06(main)
  │   │  r02/m01  r02/m02  r02/m03  r02/m07
  │   │
  │   │   ├─ r01/m02(main)  r01/m05(add)
  │   │   │  r01/m06(add)  r01/m025(main)  r01/m026(main)
  │   │   │
  │   │   │   ├─ r01/m02(add) ★最复杂add
  │   │   │   │  r01/m031(main) ★最复杂main(自定义)
  │   │   │   │  r01/m05(main) ★最复杂main(标准骨架)
```

### 3.4 复杂度的三大驱动因素

1. **审批流程按钮**：每增加一组审核/审批/驳回按钮，methods数量翻倍
2. **跨模块联动**：addLogistics(R01→R02)、loadProjectFee(查TSS_PROJECT_FEE)等打破模块边界
3. **自定义模板渲染**：r01/m02 add.vue使用rs-edit-item替代rs-form-edit，引入inputObj/tableObj/editorObj三层状态管理

---

## 四、核心矛盾与痛点

### 4.1 配置碎片化 — 五个模块割裂操作

开发一个新业务模块需要依次操作5个不同页面，且彼此之间没有联动：
- m01建资源 → 手动记下RESOURCEID → m02建模块时粘贴
- m02建接口 → 手动记下APICODE → m03建功能点时粘贴
- m01设UI → 不知道模块会怎么用这些字段
- m17写SFC → 不知道资源字段有哪些、接口怎么调

### 4.2 标准页面与SFC之间断层

- RsTableList/RsFormEdit能自动渲染，但能力有限（复杂表单、联动、图表做不到）
- SFC能做任何事，但要从零写起，无法复用标准组件的配置
- 没有中间态：从标准页面"升级"到SFC时，UI配置(resuipc)就废了

### 4.3 缺少"业务模板"概念

- 现有SFC模板是代码模板（列表页/新增页的Vue代码骨架）
- 但没有"业务场景模板"（如：一个完整的"校准记录管理"包含哪些资源/模块/接口/菜单）
- 每次新业务都是手工配置，重复劳动多

### 4.4 前端页面与配置体系脱节

- 极低复杂度页面已经纯配置驱动，但低/中复杂度页面仍需手写大量重复代码
- AutoComplete/TreePicker/RsUploader等组件在UI配置中无法声明
- 子表Tab分组、批量操作按钮、ECharts报表等无法配置化
- "配置不出来"时直接跳到"完全手写"，缺少渐进增强路径

---

## 五、按钮配置深度分析

按钮是低代码体系中最难配置化的部分，因为按钮涉及**可见性逻辑**（谁能看到）、**交互模式**（点击后做什么）、**权限控制**（谁能操作）三个维度。以下是对现有页面按钮的完整梳理。

### 5.1 列表页按钮模式

#### 5.1.1 顶部工具栏按钮 (header-action)

| 按钮 | 出现频率 | 权限控制 | 交互模式 | 能否配置化 |
|------|---------|---------|---------|-----------|
| 高级查询 | 高(6个模块) | 无 | toggle showQuery | **已配置化** — list-t01的showQuery属性 |
| 添加 | 高(所有模块) | v-per A04 | 打开新增弹窗 | **已配置化** — list-t01的addper属性 |
| 导出 | 中 | v-per A09 | 触发导出 | **已配置化** — list-t01的expper属性 |

**结论**：header-action区域的按钮已经基本配置化，无需额外设计。

#### 5.1.2 底部批量操作按钮 (footer-action) — 配置化的核心难点

现有8种按钮模式，按复杂度递增：

**模式A：标准审批流按钮组**

```
提交(A17) → 撤销提交(A18) → 审核(A12) → 撤销审核(A13) → 审批(A14) → 撤销审批(A15)
```

- 可见性：由 add01/list01 mixin 的 ISSHOW* 计算属性控制，基于 STATE 字段 + 选中行状态
- 权限：每个按钮对应一个 v-per APICODE
- 交互：部分用 Poptip 确认，部分直接调用
- **配置化方案**：模块配置 FLOWCODE 后，list-t01 自动渲染对应状态的按钮组

**模式B：自定义业务流转按钮组**

```
提交(A08) → 撤销提交(A23) → 受理(A14) → 撤销受理(A15) → 完成(A23) → 撤销完成(A24)
```

- 可见性：自定义 ISSHOW* 计算属性，逻辑因模块而异
- 权限：每个按钮对应 v-per APICODE
- 交互：部分用 Poptip 确认，部分直接调用
- **配置化方案**：需要"流转状态配置"表，定义 STATE→按钮映射

**模式C：费用操作按钮组**

```
收费 → 撤销收费 → 折扣(弹窗输入)
```

- 可见性：基于 CHARGEID 字段判断（已收费/未收费）
- 权限：部分无权限控制
- 交互：折扣按钮需弹窗输入折扣值
- **配置化方案**：需要"字段状态按钮"配置，定义字段值→按钮映射

**模式D：打印/下载按钮组**

```
证书打印(A16) → 证书下载(A20) → 受理打印(A16) → 便签打印(A16) → 记录打印(A38)
```

- 可见性：基于 STATE + 选中行数量 + 业务条件（同客户/同日期等）
- 权限：v-per APICODE
- 交互：直接调用打印/下载方法
- **配置化方案**：需要"打印配置"表，定义打印类型+条件

**模式E：跨模块操作按钮**

```
添加物流(R02_M07/A04) → 退样(A51) → 更新模版(A45)
```

- 可见性：基于选中行状态
- 权限：v-per 跨模块 APICODE
- 交互：部分需先 initModule 跨模块，再打开弹窗
- **配置化方案**：需要"跨模块按钮"配置，定义目标模块+APICODE

**模式F：证书操作按钮组**

```
证书生成(A21) → 证书预览(A49) → 作废(A22)
```

- 可见性：基于 STATE + 选中行数量
- 权限：v-per APICODE
- 交互：证书生成需批量处理，预览需打开PDF
- **配置化方案**：同模式A，属于审批流末端操作

**模式G：审核审批专用按钮**

```
委托审核(A12) → 委托审批(A14)
```

- 可见性：:disabled 而非 v-if，按钮始终可见但不可点击
- 权限：v-per APICODE
- 交互：打开全屏审核弹窗(review.vue)
- **配置化方案**：需要"审核页面"配置，定义审核弹窗组件

**模式H：查询/重置按钮**

```
查询 → 重置
```

- 可见性：始终可见
- 权限：无
- 交互：触发查询/重置
- **配置化方案**：已配置化 — list-t01内置

#### 5.1.3 列表操作列按钮 (table-action)

| 按钮 | 出现频率 | 交互模式 | 能否配置化 |
|------|---------|---------|-----------|
| 启用/停用 | 低(1个模块，且已v-if=false隐藏) | 切换ISUSE字段 | **可配置化** — resuipc增加ACTIONTYPE |
| 编辑/删除 | 低(当前通过行点击编辑) | 打开编辑弹窗/确认删除 | **可配置化** — resuipc增加ACTIONTYPE |

**结论**：操作列按钮使用极少，配置化优先级低。

### 5.2 表单页按钮模式

#### 5.2.1 底部操作按钮 (footer)

**模式A：简单CRUD按钮组** — b01/m01-m012 多数模块

```
取消 → 删除(Poptip确认, v-if="ID") → 确定
```

- 删除按钮仅编辑时显示(v-if="ID")
- 权限：删除 v-per A07，确定 v-per A04
- **配置化方案**：已基本配置化 — Add01 mixin 自动渲染

**模式B：标准审批流按钮组** — b01/m04, r01/m01, r01/m06

```
暂存(A04) → 删除(A07) → 提交(A08) → 撤销提交(A09) → 审核(A10) → 撤销审核(A11)
```

- 可见性：ISSHOW* 计算属性，基于 STATE + 创建人校验
- 权限：每个按钮对应 v-per APICODE
- 表单编辑区域也受 ISSHOWSAVE 控制(:disabled)
- **配置化方案**：模块配置 FLOWCODE 后，rs-form-edit 自动渲染按钮组 + disabled 状态

**模式C：Tooltip弹窗提交** — r01/m02

```
暂存(A04) → 删除(A07) → 提交(A17, Tooltip选审核人) → 提交并继续(A17) → 撤销提交(A18)
```

- 提交按钮使用 Tooltip 弹窗选择审核人
- "提交并继续"是特殊变体：提交后不关闭弹窗
- **配置化方案**：需要"提交模式"配置 — direct/tooltip/tooltip-continue

**模式D：自定义业务按钮** — r01/m03/m031 费用

```
修改(v-if="!CHARGEID") → 收费
```

- 修改按钮仅在未收费时显示
- **配置化方案**：需要"字段条件按钮"配置

**模式E：最简表单** — r02/m07 物流

```
保存 → 取消
```

- 无权限控制、无审批
- **配置化方案**：已配置化

#### 5.2.2 子表操作按钮

| 位置 | 按钮组 | 出现频率 | 能否配置化 |
|------|--------|---------|-----------|
| ToolBar slot="right" | 选入 + 移除 | 中 | **可配置化** — 子表配置操作类型 |
| ToolBar slot="right" | 新增 + 删除 + 上移 + 下移 | 低 | **可配置化** — 子表配置操作类型 |
| ToolBar slot="right" | 导入(文件) + 新增 + 移除 | 低 | **需脚本注入** — 文件导入是自定义逻辑 |
| 行内按钮 | 受理检验 + 检验 + 撤销 | 极低(1个) | **需SFC** — 行内按钮逻辑复杂 |

### 5.3 按钮可见性控制逻辑汇总

#### 5.3.1 add01 mixin 标准计算属性（表单页）

| 计算属性 | 条件 | 控制的按钮 |
|---------|------|-----------|
| ISSHOWSAVE | `(!STATE \|\| STATE===1) && 创建人校验` | 暂存/确定 |
| ISSHOWDELETE | `ID && (!STATE \|\| STATE===1) && 创建人校验` | 删除 |
| ISSHOWSUBMIT | `(!STATE \|\| STATE===1) && 创建人校验` | 提交 |
| ISSHOWRESUBMIT | `ID && STATE===2 && 创建人校验` | 撤销提交 |
| ISSHOWCHECK | `ID && STATE===2` | 审核 |
| ISSHOWRECHECK | `ID && (STATE===3 \|\| STATE===5 \|\| STATE===19)` | 撤销审核 |
| ISSHOWVERIFY | `ID && (STATE===3 \|\| STATE===5 \|\| STATE===19)` | 审批 |
| ISSHOWREVERIFY | `ID && (STATE===6 \|\| STATE===20)` | 撤销审批 |
| ISSHOWINVALID | `ID && STATE===6` | 作废 |

**创建人校验逻辑**: `isModifyBySelf===false || CREATEID=='' || (isModifyBySelf && CREATEID==userInfo.ID)`

#### 5.3.2 list01 mixin 标准计算属性（列表页）

| 计算属性 | 条件 (基于 checks 数组) | 控制的按钮 |
|---------|----------------------|-----------|
| ISSHOWSUBMIT | 所有选中项 STATE===1 | 批量提交 |
| ISSHOWRESUBMIT | 所有选中项 STATE===2 | 批量撤销提交 |
| ISSHOWCHECK | 所有选中项 STATE===2 | 批量审核 |
| ISSHOWRECHECK | 所有选中项 STATE===5/19 | 批量撤销审核 |
| ISSHOWVERIFY | 所有选中项 STATE===5/19 | 批量审批 |
| ISSHOWREVERIFY | 所有选中项 STATE===6/20 | 批量撤销审批 |

#### 5.3.3 组件自定义覆写的 ISSHOW*

| 模块 | 属性 | 覆写逻辑 | 原因 |
|------|------|---------|---------|------|
| r01/m02 add | ISSHOWSAVE | `(!STATE && PTEMPLATEID) \|\| STATE===1 \|\| STATE===12) && CREATEID==userInfo.ID` | 原始记录需先选模板 |
| r01/m02 add | ISSHOWDELETE | `ID && (!STATE \|\| STATE===1 \|\| STATE===12) && CREATEID==userInfo.ID` | 驳回后仍可删除 |
| r01/m02 main | ISSHOWSUBMIT | STATE=1/12 且同部门且创建人是当前用户 | 增加部门校验 |
| r01/m05 main | ISSHOWRESUBMIT | `STATE===8 \|\| STATE===7` | 自定义流转状态 |
| r01/m05 main | ISSHOWREACCEPT | `STATE===8 且当前用户=受理人` | 受理人校验 |
| r01/m03 main | ISFEE | 所有选中 !CHARGEID | 字段值判断 |
| r01/m031 main | ISSHOWAPRINT | 所有选中同 CUSTNAME | 业务条件判断 |

### 5.4 按钮与APICODE对应关系

| APICODE | 含义 | 使用模块 |
|---------|------|---------|
| A03 | 保存(项目管理) | LI_M01 |
| A04 | 保存/添加 | LIB_M01-M012, LI_M00, LI_M02, LI_M03, LI_M06, R02_M07 |
| A05 | 删除(m07-m012)或提交(m06) | LIB_M07-M012, LI_M06 |
| A07 | 删除(m01-m06) | LIB_M01-M06, LI_M00, LI_M02 |
| A08 | 提交 | LIB_M04, LI_M00, LI_M01, LI_M06 |
| A09 | 撤销提交 | LIB_M04, LI_M00, LI_M01, LI_M06 |
| A10 | 审核 | LIB_M04, LI_M01 |
| A11 | 撤销审核 | LIB_M04, LI_M01 |
| A12 | 审核(原始记录) | LI_M02 |
| A13 | 撤销审核(原始记录) | LI_M02, LI_M031(收费) |
| A14 | 审批(原始记录) | LI_M02, LI_M00(受理) |
| A15 | 撤销审批(原始记录) | LI_M00 |
| A16 | 打印 | LI_M00 |
| A17 | 提交(原始记录) | LI_M02 |
| A18 | 撤销提交(原始记录) | LI_M02 |
| A20 | 证书下载 | LI_M00 |
| A21 | 证书生成 | LI_M02 |
| A22 | 作废 | LI_M02 |
| A23 | 撤销提交/完成 | LI_M00 |
| A24 | 撤销完成 | LI_M00 |
| A38 | 记录打印(已注释) | LI_M02 |
| A39 | 记录下载(已注释) | LI_M02 |
| A45 | 更新模版 | LI_M02 |
| A49 | 证书预览 | LI_M02 |
| A51 | 退样 | LI_M00 |

### 5.5 按钮配置归属分析

#### 5.5.1 核心认知：接口 ≠ 按钮

**接口(APICODE)是"能力"，按钮是"入口"，是多对多关系：**

```
A04(保存) ─┬─ 表单页"确定"按钮
            ├─ 表单页"暂存"按钮
            └─ 脚本自动调用(如提交前自动暂存)

A16(打印) ─┬─ 列表页"证书打印"按钮
            ├─ 表单页"打印"按钮
            └─ 其他页面跨模块调用

A17(提交) ─┬─ 列表页"批量提交"按钮(多行选中)
            ├─ 表单页"提交"按钮(单条)
            └─ 表单页"提交并继续"按钮(单条+不关闭)

A23(撤销提交/完成) ─┬─ 列表页"撤销提交"按钮
                     └─ 列表页"完成"按钮(同一个APICODE，不同SHOWCOND)
```

因此，**按钮配置不能挂在 tss_moudleapi 上**，否则：
- 一个 APICODE 对应多个按钮时产生数据冗余
- 接口配置被按钮UI信息(名称/图标/颜色/位置)污染
- 纯后端调用的接口(如 BEFOREAPICODE/AFTERAPICODE)不需要按钮配置

#### 5.5.2 三个候选归属方案对比

| 方案 | 优点 | 缺点 |
|------|------|------|
| **A: 扩展 tss_moudleapi** | 按钮与接口同表，天然关联 | 接口≠按钮，1个APICODE可能对应多个按钮；接口配置被按钮UI信息污染；纯后端调用的接口不需要按钮配置 |
| **B: 新建 tss_module_button 独立表** | 按钮与接口解耦，职责清晰；一个APICODE可对应多个按钮；按钮可跨页面复用 | 新增一张元数据表，需注册到ORM；模块管理页面需增加按钮配置Tab |
| **C: 扩展 tss_funcpoint** | 权限与按钮天然关联 | funcpoint是全局权限配置，不关心按钮位置/样式/显隐条件；一个功能点在不同页面是不同按钮；funcpoint与模块无关，但按钮是按模块按页面配置的 |

**结论：方案B最优** — 新建 `tss_module_button` 独立表，通过 MODULECODE + APICODE 关联到 tss_moudleapi，通过 FUNCCODE/FUNCPOINTCODE 关联到 tss_funcpoint。

#### 5.5.3 推荐方案：页面清单 + 按钮配置

**核心思路**：模块管理从"数据源+接口"扩展为"数据源+接口+**页面清单**"。页面清单支持分组上下级管理，每个页面配置按钮。

**设计原则**：
1. 按钮挂在页面上，不是挂在模块上 — 同一个APICODE(A17)在列表页和表单页是不同按钮
2. 页面通过关联APICODE来确定行为 — 过滤器/数据源等已由接口定义，页面只指定用哪个接口
3. 列表页和表单页统一管理 — 通过 PAGETYPE 区分
4. 审批流按钮由 FLOWCODE 自动生成 — 不需要逐个配置
5. 页面支持分组/层级 — PARENTID 实现树形结构

**两张新表**：

##### tss_module_page (模块页面)

```sql
CREATE TABLE tss_module_page (
  ID            VARCHAR(36) PRIMARY KEY,
  MODULECODE    VARCHAR(50) NOT NULL COMMENT '所属模块编码(外键→tss_moudle.MODULECODE)',
  PAGECODE      VARCHAR(50) NOT NULL COMMENT '页面编码(模块内唯一, 如 list/form/review)',
  PAGENAME      VARCHAR(100) COMMENT '页面名称(如 列表页/审核列表/编辑页)',
  PAGETYPE      VARCHAR(20) NOT NULL COMMENT '页面类型: list/form/review/report',
  PARENTID      VARCHAR(36) COMMENT '上级页面ID(分组/层级, 空表示顶层)',
  ROUTEPATH     VARCHAR(100) COMMENT '路由路径(如 r02/m07/main), 弹窗方式可为空',
  COMPONENTTYPE VARCHAR(20) DEFAULT 'standard' COMMENT '组件类型: standard(标准)/sfc(在线开发)',
  SFC_MODULEPATH VARCHAR(100) COMMENT 'SFC组件路径(仅COMPONENTTYPE=sfc时)',
  QUERY_APICODE VARCHAR(20) COMMENT '列表查询接口编码(如 A01/A011), 接口里已绑FILTERCODE和PATHNAME',
  OPEN_APICODE  VARCHAR(20) COMMENT '表单打开接口编码(如 A02)',
  SAVE_APICODE  VARCHAR(20) COMMENT '表单保存接口编码(如 A04)',
  SORTNO        INT DEFAULT 0,
  ISDELETED     TINYINT DEFAULT 0
);
```

**PAGETYPE 说明**：

| 值 | 含义 | 默认组件 | 典型场景 |
|----|------|---------|---------|
| list | 列表页 | list-t01 | 主列表、审核列表、审批列表 |
| form | 表单页 | rs-form-edit | 新增/编辑弹窗、审批表单 |
| review | 审核页 | review.vue | 审核审批专用页面 |
| report | 报表页 | report-t01 | ECharts报表 |

**PAGECODE+ROUTEPATH 的关系**：
- 列表页有独立路由：PAGECODE=list, ROUTEPATH=r02/m07/main
- 表单弹窗无独立路由：PAGECODE=form, ROUTEPATH为空（由列表页打开弹窗）
- 编辑页有独立路由：PAGECODE=form, ROUTEPATH=r02/m07/add（Tab方式而非弹窗）

**QUERY_APICODE 的关键作用**：
同一个模块的多个列表页（如 LI_M02 的5个列表），区别仅在于用不同接口查询：
- 列表页 → QUERY_APICODE=A01（接口绑了FILTERCODE=F01，查所有记录）
- 审核列表 → QUERY_APICODE=A011（接口绑了FILTERCODE=F011，自动按 CHECKID=当前用户 过滤）
- 审批列表 → QUERY_APICODE=A012（接口绑了FILTERCODE=F012，自动按 VERIFYID=当前用户 过滤）

**过滤器、数据源、排序规则等全部由接口(tss_moudleapi)定义**，页面只指定用哪个接口，不重复配置。

##### tss_module_button (页面按钮)

```sql
CREATE TABLE tss_module_button (
  ID            VARCHAR(36) PRIMARY KEY,
  PAGEID        VARCHAR(36) NOT NULL COMMENT '所属页面(外键→tss_module_page.ID)',
  APICODE       VARCHAR(20) NOT NULL COMMENT '关联接口编码(外键→tss_moudleapi.APICODE)',
  BTNNAME       VARCHAR(50) COMMENT '按钮显示名称',
  BTNTYPE       VARCHAR(20) COMMENT '按钮类型: crud/flow/custom',
  BTNAREA       VARCHAR(20) COMMENT '按钮区域: header/footer/row/dts',
  INTERACTTYPE  VARCHAR(20) DEFAULT 'direct' COMMENT '交互类型: direct/poptip/tooltip/disabled',
  SHOWCOND      VARCHAR(200) COMMENT '显隐条件表达式(空=始终显示)',
  PERMCODE      VARCHAR(100) COMMENT '权限编码(如LI_M02/A17, 空=无需权限)',
  ICON          VARCHAR(50) COMMENT '图标',
  COLOR         VARCHAR(20) COMMENT '颜色: primary/red/blue',
  SORTNO        INT DEFAULT 0,
  EXTPARAM      VARCHAR(500) COMMENT '扩展参数(JSON, 如提交模式/打印类型等)',
  ISDELETED     TINYINT DEFAULT 0
);
```

**BTNAREA 说明**：

| 值 | 含义 | 适用PAGETYPE |
|----|------|-------------|
| header | 顶部工具栏 | list |
| footer | 底部操作栏 | list/form |
| row | 行操作列 | list |
| dts | 子表工具栏 | form |

**BTNAREA 替代了原方案的 PAGEAREA** — 之前用 PAGEAREA 同时区分页面类型和按钮位置(list-header/form-footer)，现在页面类型由 PAGEID→PAGETYPE 决定，BTNAREA 只负责按钮在页面内的位置。

**BTNTYPE 说明**：

| 值 | 含义 | 按钮来源 |
|----|------|---------|
| crud | 标准CRUD按钮 | 系统内置(添加/导出/保存/删除/取消)，ISSHOW由组件内部处理 |
| flow | 审批流按钮 | 由 FLOWCODE 自动生成，ISSHOW由 mixin 根据 STATE 计算 |
| custom | 自定义业务按钮 | 手动配置，ISSHOW由 SHOWCOND 表达式计算 |

**SHOWCOND 表达式语法**（运行时解析）：

```
STATE===1                          // 单状态判断
STATE in [1,12]                    // 多状态判断
STATE===1&&CREATEID==_USERID_      // 状态+创建人
!CHARGEID                          // 字段值判断
_checks_.length===1                // 选中行数量(仅列表页)
_checks_.every(r=>r.STATE===1)    // 所有选中行状态(仅列表页)
ID!=null                           // 仅编辑时(仅表单页)
```

系统变量：`_USERID_`(当前用户ID)、`_EMPID_`(当前员工ID)、`_DEPTID_`(当前部门ID)、`_checks_`(选中行数组，仅列表页)

**EXTPARAM JSON 示例**：

```json
{ "submitMode": "select_checker" }           // Tooltip弹窗选审核人
{ "submitMode": "select_checker_continue" }  // 选审核人+提交后不关闭
{ "printType": "cert" }                      // 证书打印
{ "printType": "accept" }                    // 受理打印
{ "targetModule": "R02_M07", "targetAction": "addLogistics" }  // 跨模块操作
```

#### 5.5.4 数据示例

**R02_M07 (物流管理) — 简单模块，1个列表+1个表单**：

tss_module_page:

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | COMPONENTTYPE | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|-------------|-------------|
| list | 物流列表 | list | r02/m07/main | standard | A01 | | |
| form | 物流编辑 | form | | standard | | A02 | A04 |

tss_module_button:

| PAGEID→PAGECODE | BTNAREA | APICODE | BTNNAME | BTNTYPE | INTERACTTYPE | SHOWCOND | PERMCODE | SORTNO |
|-----------------|---------|---------|---------|---------|-------------|----------|---------|--------|
| list | header | A04 | 添加 | crud | direct | | R02_M07/A04 | 1 |
| form | footer | A04 | 保存 | crud | direct | | | 1 |
| form | footer | | 取消 | crud | direct | | | 2 |

**LI_M02 (原始记录) — 复杂模块，5个列表+1个表单**：

tss_module_page:

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | PARENTID |
|----------|----------|----------|-----------|--------------|----------|
| list | 记录列表 | list | r01/m02/main | A01 | |
| list_check | 审核列表 | list | r01/m021/main | A011 | |
| list_verify | 审批列表 | list | r01/m022/main | A012 | |
| list_sign | 签发列表 | list | r01/m023/main | A013 | |
| list_query | 查询下载 | list | r01/m024/main | A014 | |
| form | 记录编辑 | form | | | |

注意：5个列表页的区别仅在于 QUERY_APICODE 不同（A01/A011/A012/A013/A014），每个接口在 tss_moudleapi 中已经绑定了不同的 FILTERCODE（F01/F011/F012/F013/F014）。

tss_module_button (列表页 list):

| BTNAREA | APICODE | BTNNAME | BTNTYPE | INTERACTTYPE | SHOWCOND | PERMCODE | SORTNO |
|---------|---------|---------|---------|-------------|----------|---------|--------|
| header | A04 | 添加 | crud | direct | | LI_M02/A04 | 1 |
| footer | A17 | 提交 | flow | tooltip | STATE in [1,12]&&CREATEID==_USERID_ | LI_M02/A17 | 2 |
| footer | A18 | 撤销提交 | flow | poptip | STATE===2&&CREATEID==_USERID_ | LI_M02/A18 | 3 |
| footer | A45 | 更新模版 | custom | poptip | STATE in [1,2,12] | LI_M02/A45 | 4 |
| footer | A49 | 证书预览 | custom | direct | _checks_.length===1 | LI_M02/A49 | 5 |

tss_module_button (表单页 form):

| BTNAREA | APICODE | BTNNAME | BTNTYPE | INTERACTTYPE | SHOWCOND | PERMCODE | SORTNO |
|---------|---------|---------|---------|-------------|----------|---------|--------|
| footer | A04 | 暂存 | crud | direct | STATE in [1,12]&&CREATEID==_USERID_ | LI_M02/A04 | 1 |
| footer | A07 | 删除 | crud | poptip | STATE in [1,12]&&CREATEID==_USERID_&&ID!=null | LI_M02/A07 | 2 |
| footer | A17 | 提交 | flow | tooltip | STATE in [1,12]&&CREATEID==_USERID_ | LI_M02/A17 | 3 |
| footer | A17 | 提交并继续 | flow | tooltip | STATE in [1,12]&&CREATEID==_USERID_ | LI_M02/A17 | 4 |

注意：A17 在表单页出现两次（"提交"和"提交并继续"），通过 EXTPARAM 的 submitMode 区分。这与原方案用 PAGEAREA 区分不同 — 现在它们属于同一个 PAGEID，通过 EXTPARAM 区分变体。

**LI_M00 (受理单) — 自定义流转，按钮多**：

tss_module_page:

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE |
|----------|----------|----------|-----------|--------------|
| list | 受理单列表 | list | r01/m05/main | A01 |
| form | 受理单编辑 | form | | |

tss_module_button (列表页 list):

| BTNAREA | APICODE | BTNNAME | BTNTYPE | INTERACTTYPE | SHOWCOND | PERMCODE | SORTNO |
|---------|---------|---------|---------|-------------|----------|---------|--------|
| footer | A08 | 提交 | custom | direct | _checks_.every(r=>r.STATE===1) | LI_M00/A08 | 1 |
| footer | A23 | 撤销提交 | custom | poptip | _checks_.every(r=>r.STATE in [7,8]) | LI_M00/A23 | 2 |
| footer | A14 | 受理 | custom | direct | _checks_.every(r=>r.STATE===7) | LI_M00/A14 | 3 |
| footer | A15 | 撤销受理 | custom | poptip | _checks_.every(r=>r.STATE===8&&r.AEMPID==_EMPID_) | LI_M00/A15 | 4 |
| footer | A23 | 完成 | custom | direct | _checks_.every(r=>r.STATE===7) | LI_M00/A23 | 5 |
| footer | A24 | 撤销完成 | custom | poptip | _checks_.every(r=>r.STATE===15) | LI_M00/A24 | 6 |
| footer | A16 | 受理打印 | custom | direct | | LI_M00/A16 | 7 |
| footer | A16 | 证书打印 | custom | direct | _checks_.every(r=>r.STATE in [10,11,14])&&_checks_.length===1 | LI_M00/A16 | 8 |
| footer | A20 | 证书下载 | custom | direct | _checks_.every(r=>r.STATE in [10,11,14]) | LI_M00/A20 | 9 |
| footer | A51 | 退样 | custom | poptip | _checks_.every(r=>r.STATE===1) | LI_M00/A51 | 10 |

注意：A23 出现两次（"撤销提交"和"完成"），A16 也出现两次（"受理打印"和"证书打印"），通过 SHOWCOND 和 EXTPARAM 区分。

#### 5.5.5 审批流按钮自动生成规则

当模块配置了 FLOWCODE 后，系统为每个 PAGETYPE=list/form 的页面自动生成 BTNTYPE=flow 的按钮（无需手动配置）：

**FLOWCODE=1 (提交→审核)**：

列表页(list)自动生成：

| BTNAREA | APICODE | BTNNAME | SHOWCOND |
|---------|---------|---------|----------|
| footer | A17 | 提交 | _checks_.every(r=>r.STATE===1) |
| footer | A18 | 撤销提交 | _checks_.every(r=>r.STATE===2) |
| footer | A12 | 审核 | _checks_.every(r=>r.STATE===2) |
| footer | A13 | 撤销审核 | _checks_.every(r=>r.STATE in [5,19]) |

表单页(form)自动生成：

| BTNAREA | APICODE | BTNNAME | SHOWCOND |
|---------|---------|---------|----------|
| footer | A04 | 暂存 | STATE in [1]&&CREATEID==_USERID_ |
| footer | A07 | 删除 | STATE in [1]&&CREATEID==_USERID_&&ID!=null |
| footer | A17 | 提交 | STATE in [1]&&CREATEID==_USERID_ |
| footer | A18 | 撤销提交 | STATE===2&&CREATEID==_USERID_ |
| footer | A12 | 审核 | STATE===2 |
| footer | A13 | 撤销审核 | STATE in [5,19] |

**FLOWCODE=2 (提交→审核→审批)**：在 FLOWCODE=1 基础上增加审批按钮组。

**自定义流转**（如受理单的提交→受理→完成）：不设 FLOWCODE，全部用 BTNTYPE=custom 手动配置。

#### 5.5.6 前端渲染流程

```
1. initModule → 加载模块配置(moudlepath + moudleapi + module_page + module_button)
2. 路由跳转 → 根据 ROUTEPATH 匹配到 module_page 记录
3. list-t01 mounted:
   a. 读取当前 PAGEID 对应的 module_button
   b. BTNTYPE=crud → 内置渲染(添加/导出/高级查询)
   c. BTNTYPE=flow → 根据 FLOWCODE 自动生成，SHOWCOND 由 mixin ISSHOW* 计算
   d. BTNTYPE=custom → 解析 SHOWCOND 表达式，动态生成 computed + 渲染按钮
   e. INTERACTTYPE 决定渲染方式: Button / Poptip+Button / Tooltip+Button
   f. QUERY_APICODE 决定用哪个接口查询(替代硬编码的 A01)
4. rs-form-edit mounted:
   a. 读取当前 PAGEID 对应的 module_button
   b. 同上渲染逻辑
   c. BTNTYPE=crud 的 save 按钮 → 同时控制表单 :disabled
   d. OPEN_APICODE 决定用哪个接口打开(替代硬编码的 A02)
   e. SAVE_APICODE 决定用哪个接口保存(替代硬编码的 A04)
```

#### 5.5.7 配置化覆盖率预估

| 按钮模式 | 占比 | 能否纯配置 | 需要什么 |
|---------|------|-----------|---------|
| 标准CRUD(添加/导出/高级查询) | 30% | **已配置化** | 无 |
| 标准审批流(提交/审核/审批/撤销) | 25% | **FLOWCODE自动生成** | tss_module_page + tss_module_button |
| 打印/下载 | 10% | **可配置化** | SHOWCOND + EXTPARAM(printType) |
| 自定义流转(受理/完成/退样) | 15% | **可配置化** | SHOWCOND + BTNTYPE=custom |
| 费用操作(收费/折扣) | 5% | **可配置化** | SHOWCOND + INTERACTTYPE=tooltip |
| 跨模块操作(添加物流) | 5% | **需脚本注入** | Level 2 脚本片段 |
| Tooltip弹窗提交(选审核人) | 5% | **可配置化** | EXTPARAM(submitMode) |
| 行内操作(受理检验) | 5% | **需SFC** | Level 3 |

**结论**：约90%的按钮可以通过配置化覆盖，只有跨模块操作和行内复杂操作需要脚本注入或SFC。

### 5.6 通用模板 — 消灭四件套

#### 5.6.1 问题：每新增一个模块都要写4个文件

现在新增一个简单业务模块（如"资质证书管理"）需要创建：

```
src/pages/b01/m07/
  ├── router.js    — 路由定义（结构完全一致，只换路径和标题）
  ├── store.js     — Vuex Store（只换 moduleCode + storeName）
  └── views/
      ├── main.vue — 列表页（list-t01 + 弹窗，几乎零自定义）
      └── add.vue  — 编辑页（view-dialog + rs-form-edit，零自定义）
```

**4个文件，约200行代码，但90%是重复模板**。

#### 5.6.2 现有页面的可替代率统计

对38个业务模块的分析结果：

| 文件 | 可完全替代 | 配置覆盖后可替代 | 不可替代 | 可替代率 |
|------|-----------|----------------|---------|---------|
| **router.js** | 28(74%) | 10(26%) | 0 | **100%** |
| **store.js** | 13(34%) | 14(37%) | 12(32%) | **71%** |
| **main.vue** | 16(42%) | 15(39%) | 8(21%) | **82%** |
| **add.vue** | 6(18%) | 6(18%) | 23(65%) | **35%** |

**router.js 替代率最高** — 38个模块的路由结构几乎完全一致，差异仅在于路径和标题。
**add.vue 替代率最低** — 表单页面的业务逻辑差异最大（子表数量/审批流/联动/自定义组件）。

#### 5.6.3 通用模板方案：GenericModule

**核心思路**：用1个通用组件 + 元数据配置，替代4个文件。已有的 `registerOnlineRoute` + `sfc-loader` 动态路由机制证明这条路可行。

```
现在：每新增模块 → 写4个文件 → 发布前端
目标：每新增模块 → 配置元数据 → 即时可用（无需写文件、无需发布）
```

**通用模板组件 `GenericModule`**：

```vue
<!-- p-admin/src/components/generic-module/index.vue -->
<template>
  <!-- 根据 PAGEPAGE.PAGETYPE 渲染不同布局 -->
  <list-t01 v-if="pageType==='list'" :store="store" ...>
    <!-- 按钮由 tss_module_button 配置驱动渲染 -->
    <template slot="header-action">
      <generic-buttons :buttons="headerButtons" @click="onBtnClick" />
    </template>
    <template slot="footer-action">
      <generic-buttons :buttons="footerButtons" @click="onBtnClick" />
    </template>
    <!-- 弹窗编辑页 -->
    <rs-modal ref="madd">
      <generic-form :pageData="formPageData" :store="store" />
    </rs-modal>
  </list-t01>
</template>
```

**通用模板的加载流程**：

```
1. 菜单点击 → ROUTEPATH = "b01/m07"
2. 路由守卫检测到未匹配路由
3. 查询 tss_module_page WHERE ROUTEPATH='b01/m07'
   → 找到: MODULECODE=LIB_M07, PAGECODE=list, PAGETYPE=list
4. 动态注册路由:
   router.addRoutes([{
     path: '/b01/m07',
     component: () => import('@/components/main'),
     children: [{
       path: 'main',
       component: GenericModule,  // 通用组件，不是业务页面
       props: { moduleCode: 'LIB_M07', pageCode: 'list' }
     }]
   }])
5. GenericModule mounted:
   a. dispatch app/initModule('LIB_M07') — 加载模块配置
   b. createStore.getStore({moduleCode:'LIB_M07', storeName:'b01/m07'}) — 自动创建store
   c. 读取 tss_module_page + tss_module_button — 获取页面和按钮配置
   d. 渲染 list-t01 + rs-form-edit — 元数据驱动
```

**与现有 registerOnlineRoute 的关系**：

```
现有 registerOnlineRoute (SFC模式):
  路由 → 加载 SFC 源码 → 编译 → new Function() 执行 → 自定义组件

新增 registerGenericRoute (通用模板模式):
  路由 → 查元数据 → GenericModule + 配置 → 标准组件

两者共存:
  /s01/m17/online/main  → SFC模式（自定义页面）
  /b01/m07/main         → 通用模板模式（标准页面）
  /r01/m02/main         → 本地路由（手写复杂页面，保留）
```

#### 5.6.4 三个层级的路由策略

| 层级 | 判断条件 | 路由注册方式 | 组件来源 |
|------|---------|------------|---------|
| **通用模板页** | tss_module_page.ROUTEPATH 有值 且 COMPONENTTYPE=standard | registerGenericRoute → GenericModule | 元数据自动渲染 list-t01/rs-form-edit |
| **SFC自定义页** | tss_module_page.ROUTEPATH 有值 且 COMPONENTTYPE=sfc | registerOnlineRoute → RemoteRoute | SFC在线编写/编译/加载 |
| **本地手写页** | 本地 router.js 有定义 | require.context 静态注册 | 业务 Vue 文件 |

**路由匹配优先级**：本地路由 > 通用模板路由 > SFC路由

#### 5.6.5 Store 自动生成

现在27个模块的 store.js 可以自动生成（极简型13个 + 标准型14个），模板如下：

```javascript
// 通用 Store 生成函数
function createGenericStore({ moduleCode, storeName, options = {} }) {
  let config = { moduleCode };
  // 如果有选择器需求，注入 SelStore paths
  if (options.selStore) {
    let oSelStore = new SelStore();
    config.paths = oSelStore.mixPaths();
  }
  let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
    config,
    storeName,
    mutations: options.mutations || {},
    actions: {
      // 标准 add action
      add({ commit }) {
        let paths = options.addPaths || ['MAIN'];
        commit('INIT', { paths });
        let defaultItem = options.defaultItem || {};
        commit('ADD', { path: 'MAIN', item: defaultItem });
      },
      // 可选: endisable
      ...(options.endisable ? {
        async endisable({ commit, dispatch }, { item }) {
          commit('SET_ENDISABLE', { item });
          let ret = await dispatch('call', {
            APICODE: options.endisableApi || 'A07',
            params: { 'UPDATE': storeHelper.getTable('UPDATE').getXML() }
          });
          if (ret.length > 0) {
            for (let a in ret[0]) item[a] = ret[0][a];
          }
        }
      } : {}),
      // 可选: SelStore actions
      ...(options.selStore ? oSelStore.mixActions() : {}),
      ...options.actions
    }
  });
  return { mapState, mapGetters, mapDateTable, Constants };
}
```

**对应关系**：

| 现在 store.js 的代码 | 自动生成 |
|---------------------|---------|
| `moduleCode: 'LIB_M07'` | 从 tss_module_page 读取 MODULECODE |
| `storeName: 'b01/m07'` | 从 ROUTEPATH 推导 |
| `oSelStore = new SelStore()` | tss_module_page 的 options.selStore=true |
| `SET_ENDISABLE mutation` | tss_module_page 的 options.endisable=true |
| `add action` | 标准内置 |
| `endisable action` | options.endisable=true 时自动生成 |

#### 5.6.6 新增模块的未来流程

**现在**（5步，需写代码）：
1. 数据库配元数据（资源/模块/接口/菜单）
2. 写 router.js
3. 写 store.js
4. 写 main.vue
5. 写 add.vue
6. npm run build → 发布

**目标**（1步，零代码）：
1. 通过模块向导配置元数据 → 通用模板自动渲染 → 即时可用

**渐进增强路径**：
- 配置不够用 → 在 tss_module_page 中切换 COMPONENTTYPE=sfc → 在线编写自定义页面
- SFC也不够 → 写本地 Vue 文件 → 本地 router.js 注册 → 传统开发模式

#### 5.6.7 实施步骤

| 步骤 | 内容 | 产出 |
|------|------|------|
| 1 | 实现 GenericModule 组件 | 支持 list/form 两种 PAGETYPE 的标准渲染 |
| 2 | 实现 registerGenericRoute | 动态路由注册，替代 router.js |
| 3 | 实现 createGenericStore | 自动生成 store，替代 store.js |
| 4 | 扩展 initModule 加载 module_page/module_button | 前端拿到页面+按钮配置 |
| 5 | 将1个简单模块(b01/m07)迁移到通用模板 | 验证端到端流程 |
| 6 | 逐步迁移其余15个零自定义模块 | 扩大覆盖面 |

---

## 六、演进方案：三级低代码体系

### 核心原则

**配置能搞定的不写脚本，脚本能搞定的不写SFC，SFC是最后手段。**

### Level 1: 纯配置页面 (目标覆盖 80%+ 场景)

基于资源管理→模块管理→UI设置→功能管理的配置链，自动生成 list-t01 + rs-form-edit 页面。

**需要增强的配置能力：**

| 增强项 | 现状 | 目标 | 改动点 |
|--------|------|------|--------|
| EDITTYPE扩展 | text/select/date/number/textarea | +autocomplete/treepicker/uploader/cascader/richtext | resuipc.EDITTYPE枚举 + rs-form-edit渲染分支 |
| Tab分组 | 无，子表硬编码在add.vue | resuipc增加GROUPNAME字段，rs-form-edit按GROUPNAME分Tab | tss_resuipc加GROUPNAME列 + rs-form-edit Tab渲染逻辑 |
| 页面清单配置 | 模块不管页面 | tss_module_page独立表，模块管理增加页面配置Tab | 新增tss_module_page表 + 模块管理页面Tab |
| 列表/表单页按钮 | 硬编码 | tss_module_button独立表，挂在页面上 | 新增tss_module_button表 + 组件自动渲染 |
| 审批流按钮 | 硬编码 | FLOWCODE自动生成审批按钮组 | list-t01/rs-form-edit读取FLOWCODE自动生成 |
| 按钮可见性 | 硬编码ISSHOW* | SHOWCOND表达式配置 | 表达式解析引擎 + 系统变量注入 |
| 通用模板组件 | 每模块写4个文件 | GenericModule替代router/store/main/add | 新增GenericModule + createGenericStore |
| 动态路由注册 | router.js手写 | registerGenericRoute从元数据生成路由 | 扩展router/index.js路由守卫 |
| 列表操作列 | 硬编码 | resuipc增加ACTIONTYPE列配置(启用/禁用/删除) | tss_resuipc加ACTIONTYPE列 + list-t01操作列渲染 |

**生成方式：** 向导一键生成 → 零代码 → 即时可用

### Level 2: 配置 + 脚本增强 (覆盖 15% 场景)

基于Level 1的标准页面，注入自定义脚本片段，实现局部增强。

**脚本注入点：**

| 注入点 | 用途 | 示例 |
|--------|------|------|
| header-action插槽 | 自定义工具栏按钮 | 批量收费/批量审批按钮 |
| column-slot插槽 | 自定义列渲染 | 状态颜色/操作按钮 |
| beforeSave/afterSave钩子 | 保存前后自定义逻辑 | 费用计算/单据号生成 |
| onRowClick钩子 | 行点击自定义行为 | 双表格联动 |
| computed注入 | 自定义计算属性 | ISSHOW按状态控制按钮可见性 |
| methods注入 | 自定义方法 | 批量操作/跨模块调用 |

**实现方式：**

1. 模块管理(m02)增加"页面脚本"配置 — 新增PATHNAME=SCRIPT的数据源，关联tbs_sfc_template
2. list-t01/rs-form-edit增加脚本加载逻辑 — mounted时检查模块是否有SCRIPT配置，有则加载并合并
3. 脚本片段格式 — 导出Vue options的部分属性(methods/computed/watch)，不是完整SFC

```javascript
// 脚本片段示例 — 存储在 tbs_sfc_template
// MODULEPATH = "R01_M03/main-script"
export default {
  methods: {
    async batchFee(rows) {
      // 自定义批量收费逻辑
    },
    async batchReFee(rows) {
      // 自定义批量撤销收费
    }
  },
  computed: {
    ISSHOW_batchFee() {
      return this.selectedRows.length > 0
    }
  }
}
```

4. list-t01自动渲染 — 扫描methods中以`batch`/`custom`开头的方法，自动生成操作按钮

### Level 3: 纯SFC自定义页面 (覆盖 5% 场景)

完全自定义页面，不使用list-t01/rs-form-edit。

**适用场景：**
- 双表格联动面板 (r01/m025, m026)
- 动态模板渲染 (r01/m02 add)
- ECharts报表 (r02/m01-m03)
- 费用管理树形面板 (r01/m031)

**实现方式：** SFC在线开发(m17)编写 → 功能管理(m03)配置路由 → sfc-loader运行时加载

**SFC不应从零写起，应能"继承"标准组件：**
- 从模块配置自动生成SFC骨架代码(已有templates/index.js)
- SFC中可引用RsTableList/RsFormEdit作为子组件
- SFC中可调用store的标准action(query/open/save)

---

## 七、模块向导 — 一站式配置替代五步操作

### 7.1 向导流程

```
┌─────────────────────────────────────────────┐
│           新建业务模块向导                     │
│                                              │
│  Step 1: 基本信息                            │
│    模块编码/名称、业务分类、父菜单             │
│                                              │
│  Step 2: 数据模型                            │
│    新建物理表(表单设计) 或 选择已有表          │
│    → 自动生成 tss_resource + tss_resfield    │
│                                              │
│  Step 3: 视图与查询                          │
│    勾选需要展示的字段 → 自动生成 VCK_ 视图    │
│    配置查询条件 → 自动生成 F01/F02 过滤器     │
│    → 自动生成 tss_resfilter                  │
│                                              │
│  Step 4: 接口配置                              │
│    自动生成 A01query/A02open/A04save/A07delete│
│    可选: 审批流(A12-A17)                     │
│    → 自动生成 tss_moudle + moudlepath + api  │
│                                              │
│  Step 5: UI配置                              │
│    拖拽排序列表字段/查询条件/编辑表单          │
│    → 自动生成 tss_resuipc                    │
│                                              │
│  Step 6: 菜单注册                            │
│    选择父菜单、配置图标                       │
│    → 自动生成 tss_func + tss_funcpoint       │
│                                              │
│  [完成] → 一键生成所有元数据，模块立即可用     │
└─────────────────────────────────────────────┘
```

### 7.2 核心价值

把5个页面的手工操作变成1个向导的流水线，元数据之间的ID引用由系统自动填充。

---

## 八、渐进式页面 — 从配置到代码的平滑升级

### 8.1 三级升级路径

```
标准渲染 (RsTableList)          渐进增强              完全自定义 (SFC)
     │                            │                        │
     ▼                            ▼                        ▼
  纯配置驱动                  配置 + 脚本注入           纯代码驱动
  resuipc定义字段            标准组件 + 脚本片段        SFC在线编写
  自动CRUD                   注入自定义区域            完全控制渲染

  覆盖: 80%场景              覆盖: 15%场景            覆盖: 5%场景
  (简单列表/表单)            (需要局部定制)           (复杂交互/图表)
```

### 8.2 完整工作流

```
                    ┌──────────────────┐
                    │  新建业务模块向导  │
                    └────────┬─────────┘
                             │
                    Step 1-5: 基本信息→数据模型→视图→接口→UI
                             │
                             ▼
                    ┌──────────────────┐
                    │  预览标准页面效果  │
                    └────────┬─────────┘
                             │
                    ┌────────┴────────┐
                    │  效果是否满意？   │
                    └────┬───────┬────┘
                    满意 │       │ 不满意
                         ▼       ▼
              ┌────────────┐  ┌─────────────────┐
              │ Level 1    │  │ 需要增强哪些？    │
              │ 纯配置页面  │  └──┬──────────┬───┘
              │ 即时可用    │     │          │
              └────────────┘  局部增强   整体重做
                              ▼          ▼
                        ┌──────────┐  ┌──────────┐
                        │ Level 2  │  │ Level 3  │
                        │ 配置+脚本│  │ 纯SFC    │
                        │ 注入片段 │  │ 自定义   │
                        └──────────┘  └──────────┘
```

---

## 九、业务模板市场 — 从零配置到一键复用

### 9.1 概念

```
┌──────────────────────────────────────┐
│         业务模板市场                  │
│                                      │
│  📦 校准记录管理                      │
│    包含: 物理表+视图+过滤器+接口+菜单 │
│    预览 → 一键安装 → 改参数即可用     │
│                                      │
│  📦 人员资质管理                      │
│  📦 设备台账管理                      │
│  📦 标准物质管理                      │
│  📦 ...                              │
│                                      │
│  也可以: 从已有模块 → 导出为模板      │
└──────────────────────────────────────┘
```

### 9.2 实现思路

- 模板 = 一组关联的元数据快照（resource + resfield + resfilter + resuipc + moudle + moudlepath + moudleapi + func + funcpoint）
- 安装 = 批量INSERT，自动替换ID引用关系
- 导出 = 从现有模块提取元数据，序列化为JSON

---

## 十、可视化表单设计器 — UI设置从表格到画布

### 10.1 现状

uiSetFull.vue 是三栏拖拽排序，本质还是"字段列表+属性面板"。

### 10.2 目标

```
┌─────────────────────────────────────────────┐
│  可视化表单设计器                             │
│                                              │
│  ┌─────────┐  ┌──────────────────────────┐  │
│  │ 组件面板 │  │     画布区域              │  │
│  │         │  │  ┌──────┐ ┌──────┐       │  │
│  │ 输入框  │  │  │字段1  │ │字段2  │       │  │
│  │ 下拉框  │  │  └──────┘ └──────┘       │  │
│  │ 日期    │  │  ┌────────────────┐      │  │
│  │ 表格    │  │  │   字段3         │      │  │
│  │ Tab页   │  │  └────────────────┘      │  │
│  │ 自定义  │  │                          │  │
│  │ (SFC)  │  │                          │  │
│  └─────────┘  └──────────────────────────┘  │
│                                              │
│  输出: tss_resuipc + 可选生成SFC代码         │
└─────────────────────────────────────────────┘
```

---

## 十一、推荐演进路线

| Phase | 内容 | 投入 | 收益 | 说明 |
|-------|------|------|------|------|
| **Phase 1** | 通用模板 + 页面清单 + 按钮配置 + EDITTYPE扩展 | 高 | 极高 | 消灭42%模块的四件套，配置覆盖率40%→82% |
| **Phase 2** | 模块向导(一键生成元数据) | 中 | 高 | 新模块从5步变1步，消除手工配置 |
| **Phase 3** | 脚本注入机制(Level 2) | 中 | 高 | 标准页面可局部增强，不再非此即彼 |
| **Phase 4** | 业务模板市场 | 低 | 中 | 新业务从模板开始而非从零开始 |
| **Phase 5** | 可视化表单设计器 | 高 | 中 | 体验提升，但功能上不超越Phase1 |

---

## 十二、现有体系 vs 目标体系

| 维度 | 现在 | 目标 |
|------|------|------|
| **极低复杂度页面** | 每模块写4个文件(router/store/main/add) | **通用模板自动渲染，零文件** |
| **低复杂度页面** | 需手写少量代码 | **通用模板+EDITTYPE+按钮配置** |
| **中复杂度页面** | 需手写大量代码 | **通用模板+脚本注入** |
| **高复杂度页面** | 完全手写Vue文件 | **SFC在线开发，可继承标准组件** |
| **配置→代码的过渡** | 断层（要么全配置要么全手写） | **三级渐进：通用模板→脚本注入→SFC** |
| **新模块开发** | 5个页面分别操作+写4个文件+发布 | **向导一键生成 → 即时可用** |
| **路由注册** | 手写router.js | **动态路由：从元数据自动注册** |
| **Store创建** | 手写store.js | **自动生成：从moduleCode推导** |
| **SFC与标准组件** | 互斥（二选一） | **可组合（SFC内用标准组件）** |

**核心洞察**：通用模板(GenericModule)是投入产出比最高的改动 — 仅需实现1个组件+1个动态路由函数+1个Store工厂函数，就能消灭42%模块的四件套(router.js+store.js+main.vue+add.vue)。再配合页面清单和按钮配置，配置覆盖率可从40%提升到82%。
