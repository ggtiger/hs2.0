# r01/ 模块迁移到 generic-module + SFC 总览

**所有模块必须迁移**，代码资产通过 m17 在线开发创建，存入 tss_code_asset。

| 模块 | 名称 | moduleCode | 方案 | 难度 |
|------|------|-----------|------|------|
| m01 | 项目管理 | LI_M01 | 标准配置 + EXTENDJS | 中等 |
| m02 | 原始记录 | LI_M02 | 整页 SFC 重写 | 极复杂 |
| m025 | 委托审核 | LI_M02 | 整页 SFC（审核工作台） | 极复杂 |
| m026 | 委托审批 | LI_M02 | 整页 SFC（审批工作台） | 极复杂 |
| m03 | 费用管理 | LI_M03 | 标准配置 + EXTENDJS | 简单 |
| m031 | 费用汇总 | LI_M031 | 列表整页 SFC + 表单配置 | 复杂 |
| m05 | 受理单 | LI_M00 | 标准配置 + EXTENDJS | 复杂 |
| m06 | 委托管理 | LI_M06 | 标准配置 + EXTENDJS | 中等 |

## 迁移策略

### 简单模块（标准配置）
m03/m01/m06/m05 — m18 配置页面+按钮 + m17 创建 EXTENDJS/Store扩展

### 复杂模块（整页 SFC）
m02/m025/m026/m031 — 整页用 SFC 组件替换，PAGECONFIG 配 `SFCMODULEPATH` 指向整页 SFC，Store扩展保留全部业务逻辑

### 详细方案
各子目录下有独立的 `migrate-to-generic.md`
