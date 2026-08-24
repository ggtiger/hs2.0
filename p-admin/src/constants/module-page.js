/**
 * 页面模板 + 按钮配置
 * 来源: s01/m18/views/config.vue
 */

export var PAGE_TPL_DEFAULTS = {
  list: {
    PAGETYPE: 'list',
    COMPONENTTYPE: 'standard',
    QUERYAPICODE: 'A01',
    ADVQUERYAPICODE: '',
    OPENAPICODE: '',
    SAVEAPICODE: '',
    PAGECONFIG: '{"QRYPATH":"QRY","QQRYSPATH":"QQRY","defaultFormPageCode":"add"}',
    buttons: [
      { BTNNAME: '添加', BTNTYPE: 'custom', BTNCODE: 'add', BTNAREA: 'header', APICODE: '', INTERACTTYPE: 'direct', ICON: 'h-icon-plus', COLOR: 'primary', EXTPARAM: '{"action":"openForm","openMode":"add","formPageCode":"add"}' }
    ]
  },
  form: {
    PAGETYPE: 'form',
    COMPONENTTYPE: 'standard',
    OPENAPICODE: '',
    SAVEAPICODE: 'A04',
    PAGECONFIG: '{"MAINPATH":"MAIN"}',
    buttons: [
      { BTNNAME: '保存', BTNTYPE: 'crud', BTNCODE: 'save', BTNAREA: 'footer', APICODE: 'A04', INTERACTTYPE: 'direct', ICON: 'h-icon-save', COLOR: 'primary', EXTPARAM: '{"action":"api"}' },
      { BTNNAME: '删除', BTNTYPE: 'crud', BTNCODE: 'delete', BTNAREA: 'footer', APICODE: 'A07', INTERACTTYPE: 'poptip', SHOWCOND: 'ID!=null', COLOR: 'red', EXTPARAM: '{"action":"api"}' }
    ]
  },
  select: {
    PAGETYPE: 'select',
    COMPONENTTYPE: 'standard',
    QUERYAPICODE: 'A01',
    ADVQUERYAPICODE: '',
    PAGECONFIG: '{"QRYPATH":"QRY","QQRYSPATH":"QQRY","SELECTMODE":"single"}',
    buttons: []
  }
};

export var BTN_PRESETS = [
  { BTNCODE: 'add', BTNNAME: '新增', BTNTYPE: 'custom', BTNAREA: 'header', INTERACTTYPE: 'direct', ICON: 'h-icon-plus', COLOR: 'primary', ACTIONTYPE: 'openForm', OPENMODE: 'add' },
  { BTNCODE: 'edit', BTNNAME: '编辑', BTNTYPE: 'custom', BTNAREA: 'row', INTERACTTYPE: 'direct', ICON: 'h-icon-edit', COLOR: '', ACTIONTYPE: 'openForm', OPENMODE: 'edit' },
  { BTNCODE: 'select', BTNNAME: '选入', BTNTYPE: 'custom', BTNAREA: 'header', INTERACTTYPE: 'direct', ICON: 'h-icon-plus', COLOR: 'primary', ACTIONTYPE: 'openSelector', SELECTMODE: 'multiple', SELECTPAGECODE: '', SELECTTARGET: '' },
  { BTNCODE: 'delete', BTNNAME: '删除', BTNTYPE: 'crud', BTNAREA: 'row', INTERACTTYPE: 'poptip', ICON: 'h-icon-trash', COLOR: 'red', POPTIPTEXT: '确定删除？', ACTIONTYPE: 'api' },
  { BTNCODE: 'save', BTNNAME: '保存', BTNTYPE: 'crud', BTNAREA: 'header', INTERACTTYPE: 'direct', ICON: 'h-icon-save', COLOR: 'primary', ACTIONTYPE: 'api' },
  { BTNCODE: 'export', BTNNAME: '导出', BTNTYPE: 'crud', BTNAREA: 'header', INTERACTTYPE: 'direct', ICON: 'h-icon-download', COLOR: '', ACTIONTYPE: 'api' },
  { BTNCODE: 'submit', BTNNAME: '提交', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'direct', ICON: 'h-icon-complete', COLOR: 'primary', ACTIONTYPE: 'api' },
  { BTNCODE: 'reSubmit', BTNNAME: '撤销提交', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', POPTIPTEXT: '确定撤销提交？', ACTIONTYPE: 'api' },
  { BTNCODE: 'check', BTNNAME: '审核', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api' },
  { BTNCODE: 'reCheck', BTNNAME: '撤销审核', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', POPTIPTEXT: '确定撤销审核？', ACTIONTYPE: 'api' },
  { BTNCODE: 'verify', BTNNAME: '审批', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'direct', ICON: 'h-icon-check', COLOR: 'primary', ACTIONTYPE: 'api' },
  { BTNCODE: 'reVerify', BTNNAME: '撤销审批', BTNTYPE: 'flow', BTNAREA: 'footer', INTERACTTYPE: 'poptip', ICON: 'h-icon-undo', COLOR: '', POPTIPTEXT: '确定撤销审批？', ACTIONTYPE: 'api' },
  // 子表行级操作(无 ACTIONTYPE, 由 rs-form-edit 内置处理: subAdd→ADD提交, subRemove→DEL, subUp/subDown→upItem/downItem)
  { BTNCODE: 'subAdd', BTNNAME: '新增行', BTNTYPE: 'custom', BTNAREA: '', INTERACTTYPE: 'direct', ICON: 'h-icon-plus', COLOR: 'primary', ACTIONTYPE: '' },
  { BTNCODE: 'subRemove', BTNNAME: '移除行', BTNTYPE: 'custom', BTNAREA: '', INTERACTTYPE: 'poptip', ICON: 'h-icon-minus', COLOR: 'red', POPTIPTEXT: '确定移除？', ACTIONTYPE: '' },
  { BTNCODE: 'subUp', BTNNAME: '上移行', BTNTYPE: 'custom', BTNAREA: '', INTERACTTYPE: 'direct', ICON: 'h-icon-top', COLOR: '', ACTIONTYPE: '' },
  { BTNCODE: 'subDown', BTNNAME: '下移行', BTNTYPE: 'custom', BTNAREA: '', INTERACTTYPE: 'direct', ICON: 'h-icon-down', COLOR: '', ACTIONTYPE: '' }
];

export var BTN_ICON_QUICK = [
  { key: 'h-icon-plus', title: '新增' },
  { key: 'h-icon-edit', title: '编辑' },
  { key: 'h-icon-trash', title: '删除' },
  { key: 'h-icon-save', title: '保存' },
  { key: 'h-icon-search', title: '搜索' },
  { key: 'h-icon-download', title: '下载' },
  { key: 'h-icon-complete', title: '完成' },
  { key: 'h-icon-check', title: '勾选' },
  { key: 'h-icon-undo', title: '撤销' },
  { key: 'h-icon-printer', title: '打印' },
];

export var BTN_POPTIP_OPTIONS = [
  { key: '确定删除？', title: '确定删除？' },
  { key: '确定执行？', title: '确定执行？' },
  { key: '确定撤销提交？', title: '确定撤销提交？' },
  { key: '确定撤销审核？', title: '确定撤销审核？' },
  { key: '确定撤销审批？', title: '确定撤销审批？' },
  { key: '确定提交？', title: '确定提交？' },
  { key: '确定作废？', title: '确定作废？' },
];

export var BTN_SHOWCOND_OPTIONS = [
  { key: '', title: '始终显示' },
  { key: 'ID!=null', title: '仅编辑' },
  { key: 'STATE===1', title: '待提交' },
  { key: 'STATE in [1,12]', title: '待提交/驳回' },
  { key: 'STATE===2', title: '待审核' },
  { key: 'STATE in [5,19]', title: '已审核' },
  { key: 'STATE===6', title: '已审批' },
  { key: '_checks_.length>0', title: '有选中行' },
  { key: '_checks_.length===1', title: '仅1行' },
  { key: '_checks_.every(r=>r.STATE===1)', title: '全待提交' },
  { key: '_checks_.every(r=>r.STATE===2)', title: '全待审核' },
  { key: 'STATE===1&&CREATEID==_USERID_', title: '本人待提交' },
  { key: 'STATE in [1]&&CREATEID==_USERID_', title: '本人待提交+' },
  { key: 'STATE===2&&CREATEID==_USERID_', title: '本人待审核' },
];

export var BTN_EXTPARAM_OPTIONS = [
  { key: '', title: '无' },
  { key: '{"submitMode":"select_checker"}', title: '选审核人' },
  { key: '{"submitMode":"select_checker_continue"}', title: '选审核人+' },
  { key: '{"printType":"cert"}', title: '证书打印' },
  { key: '{"printType":"accept"}', title: '受理打印' },
];

export var BTN_FORM_DEFAULTS = {
  BTNNAME: '',
  BTNCODE: 'custom',
  BTNTYPE: 'custom',
  BTNAREA: 'header',
  INTERACTTYPE: 'direct',
  APICODE: '',
  PERMCODE: '',
  COLOR: '',
  ICON: '',
  POPTIPTEXT: '',
  SHOWCOND: '',
  EXTPARAM: '',
  // 动作配置
  ACTIONTYPE: 'api',
  OPENMODE: 'add',
  FORMPAGECODE: '',
  SELECTMODE: 'single',
  SELECTMODULE: '',
  SELECTPAGECODE: '',
  SELECTTARGET: '',
  FIELDMAP: '',
  SELECTWIDTH: 900,
  BEFOREACTION: '',
  AFTERACTION: '',
  EXTRAPARAMS: '',
  MODALWIDTH: '',
  MODALFULLSCREEN: false,
  SORTNO: 0
};

export var SUB_PAGE_FORM_DEFAULTS = {
  PAGEID: '',
  PAGECODE: '',
  PAGENAME: '',
  PAGETYPE: 'form',
  COMPONENTTYPE: 'standard',
  SFCMODULEPATH: '',
  REFMODULECODE: '',
  REFPAGECODE: '',
  MODALWIDTH: null,
  MODALFULLSCREEN: false
};

export var ACTION_TYPE_OPTIONS = [
  { key: 'api', title: '调用API(默认)' },
  { key: 'openForm', title: '打开表单' },
  { key: 'openSelector', title: '选入列表' }
];
