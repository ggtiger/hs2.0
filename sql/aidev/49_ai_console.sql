-- ============================================================
-- AI 配置中心 RS_M27 模块元数据 + 菜单归并
-- 整合: AI 设置(m14)/提示词(m16)/场景(M23)/工具(M24)/记忆(M26)/调用记录(m15)
-- 旧菜单隐藏(ISHIDE=1), 重复的"提示词管理(在线版)"删除
-- 日期: 2026-07-20
-- ============================================================

-- 1. 模块注册
INSERT INTO tss_moudle (ID, MODULECODE, MODULENAME)
SELECT 'rs_m27_module_001', 'RS_M27', 'AI配置中心'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_moudle WHERE MODULECODE='RS_M27');

-- 2. 菜单(AI 设置父菜单下, 排最前)
INSERT INTO tss_func (ID, FUNCCODE, FUNCNAME, OUTERURL, UPFUNCID, ISHIDE, SORTCODE)
SELECT 'func_rs_m27_001', 'RS_M27', 'AI配置中心', 's01/m27', 'e219156b7e9747f1aa2fe411b6f99a64', 0, 1
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_func WHERE FUNCCODE='RS_M27');

-- 3. 旧菜单隐藏(模块保留, 配置中心内复用其接口)
UPDATE tss_func SET ISHIDE=1 WHERE ID IN (
  '16587a3e6e3311f191e50242ac130002',  -- LLM配置 s01/m14
  '165c4fa16e3311f191e50242ac130002',  -- LLM用量 s01/m15
  'rs_m16_func_001',                   -- 提示词管理 s01/m16
  'func_rs_m23_001',                   -- AI场景管理
  'func_rs_m24_001',                   -- AI工具管理
  'func_rs_m26_001'                    -- AI记忆管理
);

-- 4. 删除重复的"提示词管理(在线版)"菜单
DELETE FROM tss_func WHERE ID='ccb701d577a311f1be820242ac130002';

-- 5. 权限点
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT REPLACE(UUID(),'-',''), 'func_rs_m27_001', 'A01', '查询'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID='func_rs_m27_001' AND FUNCPOINTCODE='A01');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT REPLACE(UUID(),'-',''), 'func_rs_m27_001', 'A04', '保存'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID='func_rs_m27_001' AND FUNCPOINTCODE='A04');
INSERT INTO tss_funcpoint (ID, FUNCID, FUNCPOINTCODE, FUNCPOINTNAME)
SELECT REPLACE(UUID(),'-',''), 'func_rs_m27_001', 'A07', '删除'
FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM tss_funcpoint WHERE FUNCID='func_rs_m27_001' AND FUNCPOINTCODE='A07');

-- 完成
