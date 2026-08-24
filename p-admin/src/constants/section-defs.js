/**
 * 开发中心 SECTION_DEFS
 * 来源: s01/m28/views/console.vue + 6 个 editor
 */

export var SECTIONS = [
  { key: 'module', name: '模块', desc: '选择/新建', icon: 'h-icon-home' },
  { key: 'resource', name: '资源', desc: '表/视图/SQL', icon: 'h-icon-link' },
  { key: 'page', name: '页面', desc: '列表/表单/报表', icon: 'h-icon-edit' },
  { key: 'code', name: '代码', desc: 'C#/SQL/JS/Vue', icon: 'h-icon-github' },
  { key: 'menu', name: '菜单', desc: '导航与权限', icon: 'h-icon-menu' },
  { key: 'version', name: '版本', desc: '变更与发布', icon: 'h-icon-check' },
  { key: 'template', name: '模板', desc: '模块模板', icon: 'h-icon-task' },
  { key: 'dict', name: '字典', desc: '下拉选项', icon: 'h-icon-inbox' },
  { key: 'scene', name: '场景', desc: 'AI 场景', icon: 'h-icon-star' }
];

/* eslint-disable */
export var SECTION_CARDS = SECTIONS.filter(function(s) { return s.key !== 'module'; });
/* eslint-enable */

/* eslint-disable */
export var RESOURCE_DEF = {
  store: 'RS_M02', api: 'A02', path: 'DTSA',
  display: 'RESOURCENAME', desc: 'TABLENAME',
  name: '资源',
  filterParams: function(mc) { return { MODULECODE: mc } },
  extract: function(ret) { return (ret && ret.DTSA) || [] },
  transform: function(rows) {
    var seen = {}
    return rows.filter(function(r) { return r && r.RESOURCEID && !seen[r.RESOURCEID] && (seen[r.RESOURCEID] = true) })
      .map(function(r) { return Object.assign({}, r, { ID: r.RESOURCEID }) })
  }
};

export var PAGE_DEF = {
  store: 'RS_M18', api: 'A02', path: 'MODPAGE',
  display: 'PAGECODE', desc: 'PAGENAME',
  name: '页面',
  filterParams: function(mc) { return { MODULECODE: mc } },
  extract: function(ret) { return (ret && ret.MODPAGE) || [] },
  transform: function(rows) {
    return rows.filter(function(r) { return r && (r.ISDELETED || 0) !== 1 })
  }
};

export var CODE_DEF = {
  store: 'RS_M17', api: 'A01', path: 'Items',
  display: 'CODE', desc: 'NAME',
  name: '代码',
  filterParams: function() { return {} },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows, mc) {
    var filtered = rows.filter(function(c) {
      var code = c.CODE || ''
      var path = c.MODULEPATH || ''
      return code.indexOf('SC_' + mc) === 0 || code.indexOf('SS_' + mc) === 0 || path.indexOf(mc) >= 0
    })
    var groups = { csharp: [], sql: [], js: [], vue: [] }
    filtered.forEach(function(c) {
      var at = (c.ASSETTYPE || '').toLowerCase()
      if (!groups[at]) at = 'js'
      groups[at].push(c)
    })
    var groupDefs = [
      { key: 'csharp', label: 'API 脚本 (C#)' },
      { key: 'sql', label: 'SQL 模板' },
      { key: 'js', label: 'JS 模块' },
      { key: 'vue', label: 'Vue 组件' }
    ]
    var result = []
    groupDefs.forEach(function(g) {
      if (groups[g.key] && groups[g.key].length > 0) {
        result.push({ isGroupHeader: true, groupKey: g.key, groupLabel: g.label, groupCount: groups[g.key].length, ID: 'gh_' + g.key })
        result = result.concat(groups[g.key])
      }
    })
    return result
  }
};

export var MENU_DEF = {
  store: 'RS_M03', api: 'A01', path: 'Items',
  display: 'FUNCCODE', desc: 'FUNCNAME',
  name: '菜单',
  filterParams: function() { return {} },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows, mc) {
    return rows.filter(function(f) { return (f.OUTERURL || '').indexOf(mc) >= 0 })
  }
};

export var VERSION_DEF = {
  store: 'RS_M22', api: 'A01', path: 'Items',
  display: 'OBJCODE', desc: 'VERSION',
  name: '版本',
  filterParams: function(mc) { return { OBJCODE: mc } },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows) {
    return rows.map(function(v) {
      return Object.assign({}, v, { VERSION: v.VERSION || '' })
    })
  }
};

export var TEMPLATE_DEF = {
  store: 'RS_M25', api: 'A01', path: 'Items',
  display: 'TEMPLATECODE', desc: 'TEMPLATENAME',
  name: '模板',
  filterParams: function() { return {} },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows, mc) {
    return rows.filter(function(t) { return (t.SOURCEINFO || '').indexOf(mc) >= 0 || (t.TEMPLATECODE || '').indexOf(mc) >= 0 })
  }
};

export var DICT_DEF = {
  store: 'RS_M06', api: 'A01', path: 'Items',
  display: 'DICTCODE', desc: 'DICTNAME',
  name: '字典',
  filterParams: function() { return {} },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows) { return rows }
};

export var SCENE_DEF = {
  store: 'RS_M23', api: 'A01', path: 'Items',
  display: 'SCENECODE', desc: 'SCENENAME',
  name: 'AI 场景',
  filterParams: function() { return {} },
  extract: function(ret) { return (ret && ret.Items) || [] },
  transform: function(rows, mc) {
    return rows.filter(function(s) { return (s.CONTEXTSOURCE || '').indexOf(mc) >= 0 })
  }
};

export var SECTION_DEFS = {
  resource: Object.assign({}, RESOURCE_DEF, { editor: 'ResourceEditor' }),
  page: Object.assign({}, PAGE_DEF, { editor: 'PageEditor' }),
  code: Object.assign({}, CODE_DEF, { editor: 'CodeEditor' }),
  menu: Object.assign({}, MENU_DEF, { editor: 'MenuEditor' }),
  version: Object.assign({}, VERSION_DEF, { editor: 'VersionEditor' }),
  template: Object.assign({}, TEMPLATE_DEF, { editor: 'TemplateEditor' }),
  dict: Object.assign({}, DICT_DEF, { editor: 'DictEditor' }),
  scene: Object.assign({}, SCENE_DEF, { editor: 'SceneEditor' })
};
/* eslint-enable */
