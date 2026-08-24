/**
 * AI 场景配置
 * 来源: s01/m27/views/parts/scenePromptMap.js + tools.vue + prompts.vue
 */

// 前端工具清单(与 aiAgentProxy 注册的工具保持一致)
export var FRONTEND_TOOL_LIST = [
  { name: 'navigate', desc: '跳转页面' },
  { name: 'fill_form', desc: '填充表单字段' },
  { name: 'fill_subtable', desc: '添加子表行' },
  { name: 'get_current_page', desc: '获取当前路由/模块信息' },
  { name: 'get_form_data', desc: '获取表单数据' },
  { name: 'get_user_info', desc: '获取当前用户信息' },
  { name: 'get_menus', desc: '获取菜单树' },
  { name: 'show_message', desc: '显示消息提示' },
  { name: 'close_dialog', desc: '关闭 AI 面板' },
  { name: 'open_form', desc: '按 ID 加载记录' },
  { name: 'set_form_field', desc: '设置主表字段值' },
  { name: 'get_form_field', desc: '获取主表字段值' },
  { name: 'save_form', desc: '保存表单' },
  { name: 'add_subtable_row', desc: '新增子表行' },
  { name: 'delete_subtable_row', desc: '删除子表行' },
  { name: 'update_subtable_row', desc: '修改子表行' },
  { name: 'clear_subtable', desc: '清空子表' },
  { name: 'get_subtable_data', desc: '获取子表数据' },
  { name: 'list_subtables', desc: '列出所有子表路径' }
];

// 提示词占位符映射表
export var PROMPT_PLACEHOLDERS = {
  system_form: [
    { name: 'moduleCode', desc: '当前模块编码' },
    { name: 'currentDataPrompt', desc: '当前表单已有数据描述' }
  ],
  aidev_system_new: [
    { name: 'TARGET_MODULE', desc: '目标模块描述(用户输入)' },
    { name: 'ADD_FIELD_GUIDE', desc: '字段添加指南' },
    { name: 'NAMING_RULES', desc: '命名规则(T→V)' },
    { name: 'IRON_RULES', desc: '铁律规则' }
  ],
  aidev_system_modify: [
    { name: 'TARGET_MODULE', desc: '目标模块描述(已有模块)' },
    { name: 'ADD_FIELD_GUIDE', desc: '字段添加指南' },
    { name: 'NAMING_RULES', desc: '命名规则(T→V)' },
    { name: 'IRON_RULES', desc: '铁律规则' }
  ],
  wizard_step_0: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }],
  wizard_step_1: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }],
  wizard_step_2: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }],
  wizard_step_3: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }],
  wizard_step_4: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }],
  wizard_step_5: [{ name: 'wizardContext', desc: '向导上下文(JSON)' }]
};

export var SCENE_PROMPT_MAP = {
  assistant: {
    main: 'system_general',
    sub: [],
    toolDesc: ['tool:search_menu', 'tool:get_module_schema', 'tool:query_data', 'tool:query_stats', 'tool:open_record', 'tool:navigate'],
    forceKeys: []
  },
  form: {
    main: 'system_form',
    sub: [],
    toolDesc: ['tool:search_menu', 'tool:get_module_schema', 'tool:query_data', 'tool:open_record', 'tool:navigate'],
    forceKeys: []
  },
  optimize: {
    main: 'meta_optimize_prompt',
    sub: [],
    toolDesc: [],
    forceKeys: []
  },
  aidev: {
    main: '',
    sub: ['aidev_system_new', 'aidev_system_modify'],
    toolDesc: ['tool:search_menu', 'tool:get_module_schema'],
    forceKeys: []
  },
  wizard: {
    main: 'wizard_common_rules',
    sub: ['wizard_step_0', 'wizard_step_1', 'wizard_step_2', 'wizard_step_3', 'wizard_step_4', 'wizard_step_5'],
    toolDesc: ['tool:search_menu', 'tool:get_module_schema'],
    forceKeys: []
  },
  sfc: {
    main: 'sfc_ai_system_prompt',
    sub: ['script_ai_cs_prompt', 'script_ai_sql_prompt', 'script_ai_js_prompt'],
    toolDesc: [],
    forceKeys: ['sfc_ai_system_prompt']
  }
};

/**
 * 判断 prompt key 是否为 RegisterDefaultForce (启动时强制覆盖)
 */
export function isForceKey(sceneCode, promptKey) {
  var map = SCENE_PROMPT_MAP[sceneCode];
  return map ? (map.forceKeys || []).indexOf(promptKey) >= 0 : false;
}

/**
 * 获取场景关联的所有 prompt key (main + sub)
 */
export function getScenePromptKeys(sceneCode) {
  var map = SCENE_PROMPT_MAP[sceneCode];
  if (!map) return [];
  var keys = [];
  if (map.main) keys.push(map.main);
  if (map.sub && map.sub.length) keys = keys.concat(map.sub);
  return keys;
}

/**
 * 获取场景的工具描述 prompt key
 */
export function getSceneToolDescs(sceneCode) {
  var map = SCENE_PROMPT_MAP[sceneCode];
  return map ? (map.toolDesc || []) : [];
}

// 工具分组定义：key=EXECUTORTYPE, label=显示名, sort=排序
export var GROUP_DEFS = [
  { key: 'builtin', label: '内置工具 (C# 执行器)', sort: 1 },
  { key: 'frontend', label: '前端工具 (JS 执行器)', sort: 2 },
  { key: 'sql', label: 'SQL 查询 (只读)', sort: 3 },
  { key: 'csharp', label: 'API 脚本 (可读写)', sort: 4 },
  { key: 'static', label: '静态合并', sort: 5 }
];

// 执行器类型选项
export var EXEC_TYPE_OPTIONS = [
  { key: 'sql', title: 'sql (SQL 查询, 只读)' },
  { key: 'csharp', title: 'csharp (API 脚本, 可读写)' },
  { key: 'static', title: 'static (静态合并)' }
];
