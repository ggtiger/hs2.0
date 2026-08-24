# AI 开发助理 使用指南

华溯计量管理系统 (hs2.0) 的 AI 开发助理，能按 ORM 元数据规范自动编排"新增/修改功能"所需的全部资源（表/视图/字段/UI/字典/过滤器/模块/API/菜单/权限/审批流），产出经人工确认的变更包，导出为升级脚本，生产环境通过升级模块导入执行并留档。

## 核心理念

AI 不直接写库，而是调用"开发工具"产出 DRAFT 变更项 → 用户逐项确认 CONFIRMED → 汇总成可导出脚本。所有写操作以 `tss_aidev_*` 表留档，遵循 ORM 元数据铁律。

## 两个模块

| 模块 | 用途 | 使用环境 |
|---|---|---|
| RS_MAIDEV（AI开发助理） | 会话管理 + 对话生成 + 变更包 + 导出 | 开发环境 |
| RS_MAIDEVUPG（升级管理） | 上传脚本 + 预览 + 执行 + 回滚 | 生产环境 |

## 开发环境工作流

### 1. 新建开发会话
菜单：系统管理 → AI开发助理 → 新建

- **会话类型 NEW**：新增功能（建新表/新模块）
- **会话类型 MODIFY**：修改功能（加字段/改界面/加API/加审批流），需填目标模块编码

### 2. 进入工作区对话
点击会话列表的"进入工作区"，左右分栏：
- **左侧对话区**：用自然语言描述需求，如"新增一个设备校准记录模块，含设备名、校准日期、校准人、结果"
- **右侧变更项区**：AI 实时产出 DRAFT 变更项（含 SQL + 元数据 + 理由 + 警告）

### 3. 逐项确认
对每个 DRAFT 变更项点"确认"（→CONFIRMED）或"拒绝"（→REJECTED）：
- 确认后进入"已确认脚本区"
- 有依赖关系的项，被依赖项需先确认
- 高风险项（建表/删字段）建议单条确认

### 4. 校验
点"校验"按钮，跑 ORM 全部铁律（RESOURCEANAME 非空、ISKEY/KEYGENTYPE、过滤器三条铁律、引用字段两条铁律、ACTIONCODE 非空、四路径齐全、字段名无下划线大写）。校验不过的项有红色提示。

### 5. 导出升级包
点"导出升级包"，下载 `{SESSIONCODE}_升级包.aidev.sql`。导出后会话状态冻结为 EXPORTED，变更项不可再改。

### 6. 归档（可选）
导出后可归档（EXPORTED → ARCHIVED），会话进入只读历史档。

## 生产环境工作流

### 1. 导入升级包
菜单：系统管理 → 升级管理 → 导入升级包

三步向导：
1. **上传脚本**：选择 .aidev.sql 文件或粘贴内容
2. **预览变更项**：系统解析 @META 头 + @ITEM 分节，展示会话信息和变更项列表
3. **执行**：确认后点"确认执行"

### 2. 安全保障
- **幂等检查**：同一 SESSIONCODE 已成功执行过，禁止重复导入
- **HASH 防篡改**：执行前重新计算脚本 SHA256，与导入时存的不一致则拒绝执行
- **事务执行**：单事务包裹所有变更，任一项失败全部回滚
- **快照 + 回滚脚本**：执行前对受影响表生成 SHOW CREATE TABLE 快照和反向脚本（CREATE→DROP、INSERT→DELETE）

### 3. 查看详情
升级记录列表 → 点"详情"：
- 元信息面板（会话/类型/执行人/耗时/错误）
- 执行明细日志表（每个变更项的状态/影响行数/错误）

### 4. 回滚（如需）
SUCCESS 状态的升级可点"回滚"，执行反向脚本，状态置 ROLLEDBACK。

## 修改功能四类场景

MODIFY 类型会话，AI 会先调 `get_module_schema` + `read_table_schema` 读现状，再按场景产出差异变更项：

| 场景 | AI 产出 |
|---|---|
| 给已有表加字段 | ALTER TABLE ADD COLUMN + resfield + (可选)视图字段 + UI 配置 |

**字典使用规范**：字典在系统管理 > 字典管理（RS_M06）统一维护，**AI 不自建字典**。需要字典的字段（EDITTYPE=select），AI 会先调 `search_dict` 查已有字典，找到的引用字典名；找不到的在 RATIONALE 提示用户先去字典管理模块创建。
| 改界面配置 | UPDATE tss_resuipc（改控件类型/选择器/列宽/排序） |
| 加自定义 API | INSERT tss_moudleapi（自定义 APICODE）+ 操作列按钮 |
| 加审批流 | STATE 字段 + A12/A14/A16 等状态流转 API + 权限点 |

## 数据库表

| 表 | 作用 |
|---|---|
| tss_aidev_session | 开发会话 |
| tss_aidev_changeset | 变更包（1:1 挂会话） |
| tss_aidev_changeitem | 变更项明细（DRAFT/CONFIRMED/REJECTED） |
| tss_aidev_upgrade | 升级记录 |
| tss_aidev_upgrade_log | 升级执行明细 |
| tss_aidev_upgrade_snapshot | 升级快照（支持回滚） |

## 权限点

**RS_MAIDEV**：A01 查询 / A03 新增编辑 / A04 删除 / A05 AI生成 / A07 导出脚本 / A08 开发环境执行 / A09 确认 / A10 拒绝 / A13 归档

**RS_MAIDEVUPG**：A01 查询 / A03 新增编辑 / A04 删除 / A05 导入 / A06 执行 / A07 回滚 / A08 预览

角色绑定在生产环境由系统管理员在权限管理界面配置。

## 配置要求

- **DeepSeek API Key**：开发环境需在管理后台配置（LlmConfigService），AI 生成功能依赖此配置
- **数据库**：MySQL 5.7+，需执行 `sql/aidev/01-08` 全部 SQL 脚本初始化元数据

## 文件清单

**SQL（sql/aidev/）**：01_create_tables / 02_register_tbs_resource / 03_register_vss_view / 04_register_resuipc / 05_register_filter / 06_register_module_maidev / 07_register_module_maidevupg / 08_register_funcpoints

**后端（netcore/）**：
- Controllers/RMAIDevController.cs / RMAIDevUpgController.cs
- Services/AiDev/：ChangeSetEngine / ChangeSetValidator / ChangeSetExporter / AiDevOrchestrator / UpgradeExecutor
- Services/AssistantToolExecutor.cs（扩展 14 个开发工具）
- Models/AiDev/ChangeSet.cs

**前端（p-admin/）**：
- src/pages/s01/mAIDev/：main.vue / add.vue / workspace.vue
- src/pages/s01/mAIDevUPG/：main.vue / import.vue / detail.vue
- src/api/aidev.js / aidev-upg.js
