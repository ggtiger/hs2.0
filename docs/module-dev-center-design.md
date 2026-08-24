# 模块开发中心 (RS_M28) — 实施计划

> 日期：2026-07-20
> 状态：设计草案，待评审
> 作者：Creative Intelligence 会话产出
> 架构方案：**A 聚合视图型**（零数据迁移，复用现有模块接口）
> 导航主体：**模块对象树**（左侧 tss_moudle 树锚定）
> AI 集成：**右侧常驻助理**（类 Cursor AI Panel）

---

## 1. 目标与范围

### 1.1 用户痛点
当前开发一个新业务模块（如 r03/m01）需要在 **7+ 菜单**来回切换：
- s01/m01 资源管理（配 tss_resource / resfield / resfilter / resuipc）
- s01/m02 模块管理 + s01/m18 模块配置（页面/按钮/数据源/编排接口）
- s01/m03 功能管理（菜单 + 功能点）
- s01/m06 字典管理
- s01/m13 SQL 配置 / s01/m17 代码在线开发（脚本资产）
- s01/m22 版本中心 / s01/m25 模板市场
- s01/m27 AI 配置中心（场景 / 工具 / 提示词）

**没有"模块全景视图"**——一个模块的所有相关信息无法在一屏内看到，跨模块数据关联弱（靠 MODULEPATH 前缀约定，不可靠）。

### 1.2 设计目标
- 一站式：左侧选定模块后，右侧所有 Tab 自动过滤到该模块
- 不重写：复用 RS_M01/M02/M03/M06/M17/M18/M22/M25 现有接口，**零数据迁移**
- AI 锚定：右侧常驻 AI 面板，上下文随"当前选中模块/资源/页面"自动构建
- 不阻塞：保留各原有菜单，模块开发中心仅作为聚合壳

### 1.3 非目标
- 不合并/废弃现有 RS_M17/M18/M22/M25 等模块
- 不重架 tss_code_asset / tss_resource 等核心表
- 不替代 RS_M18 的复杂配置（页面/按钮精细编辑仍跳转 m18/config.vue 全屏 Modal）

---

## 2. 信息架构

### 2.1 整体布局（参考 m27，但导航主体从"分区"换为"模块对象"）

```
┌──────────────────────────────────────────────────────────────────┐
│ dc-header  模块开发中心  [+ 新建模块(向导)]  [刷新]  [Cmd+K]      │
├──────────────────┬───────────────────────────────┬───────────────┤
│ dc-module-tree   │ dc-content                    │ dc-ai-panel   │
│ 240px            │ (Tab 容器)                    │ 360px 可折叠   │
│                  │                               │               │
| 🔍 搜索框         │ ┌─────────────────────────┐  │ AI 助理        │
│                  │ │ dc-overview  模块概览    │  │ [对话/向导 切换]│
│ 📁 b01 基础数据   │ │ dc-resource 资源/字段    │  │               │
│  └ LI_M02 收发记录│ │ dc-page     页面/按钮    │  │ <消息列表>     │
│  └ B01_M05 客户   │ │ dc-code     代码资产     │  │               │
│ 📁 r01 报告       │ │ dc-menu     菜单/功能点   │  │ <输入框 + 工具>│
│ 📁 r02 记录       │ │ dc-version  版本时间线   │  │               │
│  └ R02_M07 物流   │ │ dc-template 模板/发布    │  │ 当前上下文:    │
│ 📁 s01 系统管理   │ │ dc-dict     字典(跨模块)  │  │ • 模块: R02_M07│
│  └ RS_M18 ...    │ │                         │  │ • 焦点: VCK_.. │
│ 📁 cgdd 采购订单  │ └─────────────────────────┘  │               │
└──────────────────┴───────────────────────────────┴───────────────┘
```

### 2.2 与 m27 的关键差异

| 维度 | m27 AI 配置中心 | m28 模块开发中心 |
|---|---|---|
| 导航锚点 | 业务分类（6 个分区） | 模块对象（tss_moudle 树） |
| 数据来源 | 6 个 AI 基础模块 | **11+ 个模块开发相关模块** |
| AI 集成 | 场景配置页内嵌测试对话 | **右侧常驻助理面板** |
| 权限粒度 | 整页权限 | 模块维度过滤 + 标准页权限 |
| 是否有 store.js | 无（纯委托） | 有（管理当前选中模块 + AI 对话状态） |

---

## 3. 左侧模块树设计

### 3.1 数据源
- 主表：`tss_moudle` (MODULECODE / MODULENAME)
- 分组依据：`UPFUNCID` 关联到 `tss_func`，按业务域分组（b01 / r01 / r02 / s01 / cgdd / lib）
- 树结构：
  ```
  业务域（func 顶层）
  └─ 模块（moudle，按 UPFUNCID 归属）
  ```
- 加载接口：复用 `RS_M02/A01` (query) 或新增轻量接口 `RS_M28/A01` 一次性返回树 JSON（避免多次往返）

### 3.2 树节点交互
- 点击模块节点 → 右侧所有 Tab 切换数据源到该模块
- 右键菜单：
  - 新建子模块（跳转向导 Step0）
  - 导出为模板（RS_M25/A05）
  - 删除模块（确认对话框 + 级联检查）
  - 复制 MODULECODE（粘贴到别处用）
- 拖拽：暂不支持（v1）
- 搜索：前端模糊匹配 MODULECODE / MODULENAME，自动展开父节点

### 3.3 树顶部工具栏
- 🔍 搜索框
- ➕ 新建模块（打开 m18/views/components/module-wizard.vue）
- 🔄 刷新
- ⚙️ 显示设置（隐藏系统模块 RS_*/LI_M00 等）

---

## 4. 右侧 Tab 容器设计

### 4.1 Tab 列表与数据来源

| Tab | 标题 | 数据来源 | 操作能力 | 是否锚定模块 |
|---|---|---|---|---|
| **overview** | 模块概览 | 聚合：tss_moudle + 关联资源 + 关联页面 + 关联代码资产计数 | 只读 + 跳转链接 | ✅ |
| **resource** | 资源/字段 | `RS_M01` (VSS_RESOURCE / VSS_RESFIELD / VSS_RESFILTER) 按 MODULECODE 过滤 | 查看 + 跳转 m01/add.vue 编辑 | ✅ |
| **page** | 页面/按钮 | `RS_M18` (VCK_MODULE_PAGE / VCK_MODULE_BUTTON) | 查看 + 跳转 m18/config.vue 全屏编辑 | ✅ |
| **code** | 代码资产 | `RS_M17` (VSS_CODE_ASSET) 按 MODULEPATH LIKE 前缀匹配 | 查看 + 跳转 m17/edit.vue | ✅ |
| **menu** | 菜单/功能点 | `RS_M03` (VSS_FUNC / VSS_FUNCPOINT) 按 OUTERURL LIKE %MODULECODE% | 查看 + 启停 | ✅ |
| **version** | 版本时间线 | `RS_M22` (VSS_DEV_VERSION) 按 OBJCODE 过滤 | 查看 + 对比 + 回滚 | ✅ |
| **template** | 模板/发布 | `RS_M25` (VSS_MODULE_TEMPLATE) 按 SOURCEINFO 模糊匹配 | 导出模板 + 安装模板入口 | ✅ |
| **dict** | 字典(跨模块) | `RS_M06` (tss_dict) | 完整 CRUD（字典无模块归属） | ❌ 跨模块 |
| **scene** | AI 场景 | `RS_M23` (tss_ai_scene) 按 CONTEXTSOURCE 含 MODULECODE 过滤 | 查看 + 启停 + 测试 | ✅ |

### 4.2 Tab 通用模式
每个 Tab 复用 m27 的 parts/* 模式：
- `created()` 调 `getGenericStore(MC)` 懒注册宿主 store
- `activated()` / watch 当前模块 → `$callAction({ action: MC + '/query', param: { FilterParams: {...} } })`
- 列表读取 `this.$store.state[MC].dt.QRY.data`
- 计数 `$emit('count', { key: 'page', n: list.length })` 上报给 overview

### 4.3 模块锚定的过滤策略

不同模块的关联字段不同，需要差异化过滤：

| 模块组 | 关联字段 | 过滤方式 |
|---|---|---|
| RS_M01 资源 | MODULECODE | `FilterParams: { MODULECODE }` |
| RS_M18 页面 | MODULECODE | `FilterParams: { MODULECODE }` |
| RS_M17 代码 | MODULEPATH LIKE | 调 `RS_M17/A01` 后前端二次过滤 `record.MODULEPATH.includes('/' + mc + '/')` 或 `record.CODE.startsWith('SC_' + mc)` |
| RS_M22 版本 | OBJCODE / OBJID | `FilterParams: { OBJCODE: MODULECODE }` |
| RS_M03 菜单 | OUTERURL LIKE | 前端树过滤 `func.OUTERURL.includes(MODULECODE)` |
| RS_M23 场景 | CONTEXTSOURCE | 调 A01 后前端过滤 `scene.CONTEXTSOURCE.includes(MODULECODE)` |
| RS_M25 模板 | SOURCEINFO | 调 A01 后前端过滤 |

---

## 5. 右侧 AI 助理面板设计

### 5.1 面板结构
```
┌─ dc-ai-panel ────────────────────┐
│ [对话] [向导]    [折叠 ◯]         │
├───────────────────────────────────┤
│ 当前上下文（可编辑，可移除）       │
│ • 模块: R02_M07 (物流管理)        │
│ • 焦点: VCK_LOGISTICS             │
│ • 焦点: list.vue                  │
├───────────────────────────────────┤
│ <消息列表>                        │
│  user: 给物流表加一个"承运单号"    │
│  ai: 好的，我将创建字段...         │
│   [changeset 预览]                │
├───────────────────────────────────┤
│ [可用工具: 12 个] ▼              │
│ ┌─────────────────────────────┐  │
│ │ 输入消息...                  │  │
│ │                  [发送]      │  │
│ └─────────────────────────────┘  │
└───────────────────────────────────┘
```

### 5.2 两种模式

**模式 A：自由对话**
- 复用现有 `AiClient` (utils/ai/AiClient.js)
- `scene: 'aidev'` 或 `'assistant'`，按用户选择
- 用户可手动把"焦点对象"拖入上下文（resource id / page code / script code）
- 工具按当前焦点自动过滤：选中字段时优先 `modify_field` / `add_field`；选中页面时优先 `define_page`

**模式 B：向导模式**
- 复用现有 `WizardStepOrchestrator` 6 步流程
- 但不再跳转独立页面，而是在右侧面板内联
- 步骤进度条在面板顶部
- 每步生成结果展示在消息流，确认按钮在每条消息下方
- "跳过本步" / "重新生成" / "确认并下一步" 三按钮

### 5.3 上下文自动构建策略
```
selectedModule = R02_M07     // 左树选中
selectedResource = VCK_LOGISTICS  // resource Tab 内点击行
selectedPage = list           // page Tab 内点击页面

→ AI 上下文 system prompt 注入:
  "当前操作模块: R02_M07 (物流管理)
   当前焦点资源: VCK_LOGISTICS (vck_logistics_001)
   当前焦点页面: list (PAGETYPE=list)
   当前焦点代码: SS_LOG_001 (SQL 模板)"
```

工具调用时根据焦点自动过滤 `stepToolMap` / `availableTools`，只把相关的 5-10 个工具暴露给 LLM（节省 token）。

### 5.4 AI 助理与 Tab 的联动
- 点击 Tab 内任意行 → 自动加入 AI 上下文（顶栏"当前焦点"显示）
- 用户可点击行内"问 AI"按钮 → 直接预填 prompt "针对这个 XXX 给出优化建议"
- AI 生成的 changeset 在 Tab 内实时高亮（resource Tab 新字段闪烁 2 秒）

---

## 6. 数据来源详细映射

### 6.1 不新增任何数据表
所有数据通过现有 RS_MXX 模块的标准接口（A01 query / A02 open / A04 save）或既有自定义接口获取。

### 6.2 新增模块配置（最小集）

**tss_moudle 注册：**
```sql
INSERT INTO tss_moudle (MODULEID, MODULECODE, MODULENAME) VALUES
  ('rs_m28_module_001', 'RS_M28', '模块开发中心');
```

**tss_moudlepath（RS_M28 自身查询路径）：**
- 仅配置 `QQRY` 路径用于左树加载（一次性返回模块树 JSON）
- 资源：VSS_MOUDLE (复用 m02 已有视图)

**tss_moudleapi（仅 1 个聚合接口）：**
- `A01` APITYPE=query，用于左树加载模块列表（按 UPFUNCID 树化）

**无需 A02/A04 等标准接口**——模块开发中心本身不做 CRUD，所有编辑都委托给底层模块。

### 6.3 前端 store.js（与 m27 不同，m28 需要自己的 store）

```js
// p-admin/src/pages/s01/m28/store.js
import createStore from '@/store/createStore'

var config = {
  storeName: 's01/m28',
  paths: {
    QQRY: 'VSS_MOUDLE'  // 仅用于左树加载
  },
  state: {
    selectedModule: null,      // 当前选中的模块对象
    selectedFocus: [],          // AI 上下文焦点栈
    aiPanelCollapsed: false,
    aiMode: 'chat'              // 'chat' | 'wizard'
  },
  mutations: {
    SET_SELECTED_MODULE(state, m) { state.selectedModule = m },
    PUSH_FOCUS(state, f) { state.selectedFocus.push(f) },
    CLEAR_FOCUS(state) { state.selectedFocus = [] }
  }
}

export default createStore.getStore({ config, storeName: 's01/m28' })
```

---

## 7. 文件清单

### 7.1 新增文件

**前端（p-admin/src/pages/s01/m28/）：**
```
index.js                          # export console.vue
router.js                         # 路由懒加载
store.js                          # 自有 store（仅模块树 + AI 面板状态）
views/
  console.vue                     # 主容器（左树 + Tab + AI 面板三栏布局）
  parts/
    module-tree.vue               # 左侧模块树（基于 Tree component）
    overview.vue                  # Tab: 模块概览（聚合数据卡片）
    resource-tab.vue              # Tab: 资源/字段（RS_M01 数据）
    page-tab.vue                  # Tab: 页面/按钮（RS_M18 数据）
    code-tab.vue                  # Tab: 代码资产（RS_M17 数据）
    menu-tab.vue                  # Tab: 菜单/功能点（RS_M03 数据）
    version-tab.vue               # Tab: 版本时间线（RS_M22 数据）
    template-tab.vue              # Tab: 模板/发布（RS_M25 数据）
    dict-tab.vue                  # Tab: 字典（RS_M06 数据，跨模块）
    scene-tab.vue                 # Tab: AI 场景（RS_M23 数据）
  components/
    ai-panel.vue                  # 右侧 AI 助理面板（对话 + 向导切换）
    focus-bar.vue                 # 上下文焦点栏（显示/移除焦点）
    command-palette.vue           # Cmd+K 全局命令面板（v2 阶段）
```

**后端：**
- 无新增 Controller（零自定义后端代码）
- 仅元数据 SQL（见 7.2）

### 7.2 新增 SQL

**`sql/aidev/53_module_dev_center.sql`：**
- INSERT tss_moudle (RS_M28)
- INSERT tss_moudlepath（QQRY）
- INSERT tss_moudlepathrel
- INSERT tss_moudleapi（A01 query）
- INSERT tss_func（菜单位置：s01 系统管理下，OUTERURL='/s01/m28'）
- INSERT tss_funcpoint（权限点：访问模块开发中心）
- 旧菜单保留（不 ISHIDE）

---

## 8. 实施路径（5 个 Phase）

### Phase 1：骨架 + 左树（1 天）
- 创建 m28 目录结构
- console.vue 三栏布局
- module-tree.vue 加载 tss_moudle 树
- store.js 注册
- 点击树节点暂只 console.log

**验收：** 左树显示所有模块，按业务域分组，支持搜索

### Phase 2：右侧 Tab 容器 + 3 个核心 Tab（2 天）
- Tab：overview / resource / page
- 复用 RS_M01 / RS_M18 接口
- watch selectedModule 自动刷新
- 点击 resource 行跳转 s01/m01/add.vue?id=xxx
- 点击 page 行跳转 m18/config.vue（Modal 方式）

**验收：** 选定模块后，资源/页面 Tab 自动列出该模块的数据，跳转编辑可用

### Phase 3：右侧 AI 面板骨架（1.5 天）
- ai-panel.vue 右侧固定 360px 宽
- focus-bar.vue 显示当前焦点
- 接入 AiClient (scene: 'aidev')
- 支持自由对话（不接入向导）

**验收：** AI 面板能对话，能根据 selectedModule 自动注入上下文

### Phase 4：补全剩余 Tab（2 天）
- code-tab / menu-tab / version-tab / template-tab / dict-tab / scene-tab
- 每个都是 list + 跳转模式（与 Phase 2 模式一致）

**验收：** 所有 9 个 Tab 数据可见，跳转可用

### Phase 5：AI 面板集成向导 + 命令面板（2 天，可选）
- ai-panel.vue 增加"向导"模式切换
- 内嵌 WizardStepOrchestrator 6 步流程
- focus-bar 支持手动添加/移除焦点
- Cmd+K 命令面板（v2 可推迟）

**验收：** 向导能在 AI 面板内完成全流程，不跳转独立页面

**总计：~8.5 天（不含 Phase 5 是 6.5 天）**

---

## 9. 关键技术决策

### 9.1 为什么 m28 需要自己的 store.js（不像 m27）
m27 是纯静态分区切换，无"当前对象"概念。m28 需要维护：
- 当前选中模块（影响所有 Tab 过滤）
- AI 焦点栈（影响 AI 上下文）
- AI 面板折叠状态

这些是跨组件共享状态，必须放 Vuex。

### 9.2 为什么用动态组件 + keep-alive，不用子路由
参考 m27 模式：keep-alive 缓存组件实例，切换 Tab 不丢失状态（滚动位置、列表数据）。子路由每次切换会重建。

### 9.3 为什么不嵌入 m18/config.vue 而是跳转
m18/config.vue 是 3231 行的复杂页面，全屏 Modal 才能放下。Tab 内嵌入会导致二次嵌套布局问题。点击页面 Tab 的"编辑"按钮直接打开 m18 Modal 是最稳妥方案。

### 9.4 为什么左树数据用 tss_func 而不是直接 tss_moudle
tss_moudle 无业务域字段（MODULECODE 如 R02_M07 自带业务域前缀，但 RS_M18 这种系统模块都叫 RS_）。用 tss_func 的树形结构能稳定获得业务域分组（b01 / r01 / s01 / cgdd）。

### 9.5 复用现有代码的边界
- ✅ 复用：`getGenericStore` / `$callAction` / AiClient / `code-asset.js` / `module-wizard.vue` / `code-editor-popup.vue` / `version-history-popup.vue`
- ❌ 不复用：`scenes.vue` 的 5-store 联动逻辑（m28 的 scene-tab 只做查看，不做配置）
- ❌ 不复用：`config.vue` 的精细编辑（走跳转 Modal）

---

## 10. 风险与对策

| 风险 | 等级 | 对策 |
|---|---|---|
| 一次加载 11 个 store 导致首屏卡顿 | 中 | 懒注册 + keep-alive，首次切换 Tab 时才 `getGenericStore(MC)` |
| 模块树过大（全库 100+ 模块） | 低 | 虚拟滚动 + 分组折叠 + 搜索 |
| AI 面板占用屏幕宽度，挤压 Tab 内容 | 中 | 默认折叠（图标展开），用户可固定展开 |
| tss_func.OUTERURL 模糊匹配不准 | 中 | 提供手动"关联到模块"按钮（调用 RS_M03 save 更新 OUTERURL） |
| 权限：用户能看模块但不能改 | 低 | 每个 Tab 内的"编辑"按钮按 RS_MXX/A04 权限点控制 |

---

## 11. 验收标准

### 11.1 功能验收
- [ ] 左树正确显示所有 tss_moudle，按业务域分组
- [ ] 选中模块后，9 个 Tab 数据均自动刷新到该模块范围
- [ ] 各 Tab 内的"跳转编辑"按钮能正确打开对应详情页/Modal
- [ ] AI 面板能对话，上下文正确包含当前模块
- [ ] AI 面板能切换到向导模式，完成 6 步流程
- [ ] Cmd+K（若实现）能快速搜索模块和资源

### 11.2 非功能验收
- [ ] 首屏加载 < 2s（左树 + 空 Tab）
- [ ] 切换 Tab < 300ms
- [ ] 选中模块后所有 Tab 刷新完毕 < 1.5s
- [ ] 不影响现有 RS_M17/M18/M22 等模块的独立访问

---

## 12. 后续演进（v2+）

- **模块关联图**：可视化展示模块 → 资源 → 字段 → 引用关系拓扑（D3.js 力导向图）
- **批量操作**：选中多个模块，批量导出模板 / 批量版本回滚
- **AI Code Review**：选定模块后，AI 自动扫描所有代码资产，给出改进建议
- **跨环境同步**：与 RS_M22 发布中心深度集成，支持 dev/staging/prod 三环境对比
- **插件化**：允许用户自定义 Tab（类似 IDE 的扩展机制）

---

## 附录 A：m27 模式速查（参考实现）

```js
// parts 子组件的标准套路
import { getGenericStore } from '@/components/generic-module/generic-store'
const MC = 'RS_M01'

export default {
  data() { return { list: [] } },
  created() { this.storeObj = getGenericStore(MC) },
  activated() {
    if (this.$store.state['s01/m28'].selectedModule) {
      this.loadList()
    }
  },
  methods: {
    async loadList() {
      const mc = this.$store.state['s01/m28'].selectedModule.MODULECODE
      await this.$callAction({
        action: MC + '/query',
        param: { FilterParams: { MODULECODE: mc } }
      })
      this.list = this.$store.state[MC].dt.QRY.data || []
      this.$emit('count', { key: 'resource', n: this.list.length })
    }
  }
}
```

## 附录 B：路由配置

```js
// p-admin/src/pages/s01/m28/router.js
export default [
  {
    path: '/s01/m28',
    component: () => import('@/components/main'),
    children: [
      {
        path: 'main',
        name: 's01-m28-main',
        component: r => require.ensure([], () => r(require('@/pages/s01/m28')), 'm28'),
        meta: { title: '模块开发中心', icon: 'md-cube', hideInMenu: false }
      }
    ]
  }
]
```

## 附录 C：菜单位置

- 父菜单：s01 系统管理（UPFUNCID = `1e38586d-13e6-11ea-9e8d-00163e067045`）
- 排在 s01/m02 模块管理之后（SORTNO 紧接 m02）
- 图标：`md-cube`
- 权限点：`RS_M28` 模块访问权限
