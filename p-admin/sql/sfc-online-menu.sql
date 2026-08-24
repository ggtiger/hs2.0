-- 在线 SFC 模块菜单测试入口
--
-- 原理 (约定优于配置):
--   菜单 OUTERURL = 路由 name = `s01/m16/online/main`
--   路由层 (router/index.js) 看到 name 含 `/online/` 段, 自动推导 MODULEPATH:
--     s01/m16/online/main → @/pages/s01/m16/views/main.vue
--   并动态 addRoutes 注册 RemoteRoute 组件加载该在线 SFC
--
--   菜单点击流程 (main.vue select):
--     initModule(name) → omenus.find(OUTERURL=name) → 取 FUNCCODE=S01_M16
--                     → dispatch('app/initModule', 'S01_M16') 加载模块配置(MODPATH/scm)
--     registerOnlineRoute(name) → addRoutes 注册在线路由
--     $router.push({name}) → 命中 RemoteRoute → loadCompiledSFC → 渲染
--
-- 使用前置条件:
--   1. 数据库已有 tbs_sfc_template 记录 MODULEPATH=@/pages/s01/m16/views/main.vue
--      (通过 SFC 编辑器编译保存, 或运行 gen-sfc-test-data.js)
--   2. tss_moudle 已注册 S01_M16 模块 (通常已存在, 因为 RS_M16 提示词管理就是它)
--
-- 清理: DELETE FROM tss_func WHERE FUNCCODE = 'S01_M16_ONLINE';

INSERT INTO tss_func (
  ID, UPFUNCID, FUNCTYPE, FUNCCODE, FUNCNAME, FUNCICON,
  ISOUTERURL, OUTERURL, REMARK, ISHIDE, ISUSE, LEVEL, SORTCODE
) VALUES (
  REPLACE(UUID(), '-', ''),
  '3e3c83ce2b3c475b82902478c89c27c0',  -- UPFUNCID: 系统管理目录
  1,                                     -- FUNCTYPE: 1=菜单
  'RS_M16',                              -- FUNCCODE: 必须对应 tss_moudle.MODULECODE (用于 initModule 加载模块配置)
  '提示词管理(在线版)',                  -- FUNCNAME: 菜单显示名
  'md-document',
  0,                                     -- ISOUTERURL: 0=内部路由 (走 $router.push)
  's01/m16/online/main',                 -- OUTERURL: 路由 name (含 /online/ 触发在线加载)
  'SFC 在线开发平台 - 测试入口 (从数据库加载 s01/m16 的 Vue 组件)',
  0,                                     -- ISHIDE: 0=显示
  1,                                     -- ISUSE: 1=启用
  2,                                     -- LEVEL: 菜单层级
  180                                    -- SORTCODE: 排序
);

-- 验证
SELECT FUNCCODE, FUNCNAME, OUTERURL FROM tss_func WHERE OUTERURL = 's01/m16/online/main';
