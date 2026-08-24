-- ============================================================
-- 模块开发中心 RS_M28 模块元数据 + 菜单注册
-- 一站式聚合: 资源/页面/代码/菜单/版本/字典/AI 助理
-- 复用现有 RS_M01/M02/M03/M06/M17/M18/M22/M23/M25 等模块接口, 零数据迁移
-- 日期: 2026-07-20
-- 设计文档: docs/module-dev-center-design.md
-- ============================================================

-- 1. 模块注册
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m28_module_001', 'RS_M28', '模块开发中心'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M28');

-- 2. 菜单(系统管理父菜单下, 排在版本中心 M22 之后、模板市场 M25 之前)
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, ISHIDE, SORTCODE, FUNCICON)
SELECT 'func_rs_m28_001', 'RS_M28', '模块开发中心', 's01/m28', '3e3c83ce2b3c475b82902478c89c27c0', 0, 230, 'md-cube'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M28');

-- 3. 权限点(模块开发中心主要为只读聚合, 仅需 A01 查询权限)
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT REPLACE(UUID(),'-',''), 'func_rs_m28_001', 'A01', '查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID='func_rs_m28_001' AND FUNCPOINTCODE='A01');

-- 注:
--   - 不注册 tss_moudlepath/tss_moudleapi: RS_M28 是聚合壳, 不做 CRUD
--     模块树数据通过复用 RS_M02/A01 (VSS_MOUDLE) 获取
--   - 不隐藏任何旧菜单: 模块开发中心与现有 RS_M17/M18/M22/M25 等模块并存
--     用户既可从开发中心聚合视图进入, 也可直接访问各专用模块
--   - 不注册 tss_resfield/tss_resfilter: RS_M28 无独立数据视图

-- 完成
