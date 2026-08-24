# r02/ 模块迁移到 generic-module + SFC 总览

**所有模块必须迁移**，代码资产通过 m17 在线开发创建，存入 tss_code_asset。

| 模块 | 名称 | moduleCode | 方案 | 难度 |
|------|------|-----------|------|------|
| m01 | 检测统计 | LIR_M01 | PAGETYPE=report | 复杂 |
| m02 | 人员效能 | LIR_M02 | PAGETYPE=report | 复杂 |
| m03 | 客户统计 | LIR_M03 | PAGETYPE=report + EXTENDJS | 复杂 |
| m05 | (废弃) | - | 删除 | - |
| m07 | 物流管理 | R02_M07 | 标准配置 + EXTENDJS | 中等 |

## 迁移策略

### 报表模块
m01/m02/m03 — PAGETYPE=report + PAGECONFIG.REPORT 配置图表 + EXTENDJS 处理 rowspan/SHOWNUM

### CRUD 模块
m07 — 标准配置 + Store扩展（多path INIT + loadAcceptRefs）

### 详细方案
各子目录下有独立的 `migrate-to-generic.md`
