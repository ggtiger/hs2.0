/**
 * UI 控件类型选项
 * 来源: s01/m01/views/uiSetFull.vue
 */

export var EDIT_TYPE_OPTIONS = [
  { key: 'text', title: '文本' },
  { key: 'number', title: '数字' },
  { key: 'select', title: '下拉' },
  { key: 'textarea', title: '多行文本' },
  { key: 'datepicker', title: '日期' },
  { key: 'autocomplete', title: '自动完成' },
  { key: 'multiautocomplete', title: '多选自动完成' },
  { key: 'treepicker', title: '树形选择' },
  { key: 'checkbox', title: '复选' },
  { key: 'fileupload', title: '文件上传' },
  { key: 'imageupload', title: '图片上传' },
  { key: 'fileuploadtpl', title: '文件上传(模板选择)' },
  { key: 'code', title: '代码' },
  { key: 'image', title: '图片显示' },
  { key: 'action', title: '操作按钮' },
  { key: 'multiselect', title: '多选列' },
  { key: 'singleselect', title: '单选列' },
  { key: 'toolbar', title: '分组标题' },
  { key: 'tableblock', title: '表格区块' },
  { key: 'pageaction', title: '页面按钮' },
  { key: 'index', title: '序号' },
  { key: 'slot', title: '插槽' },
];

export var QUERY_TYPE_OPTIONS = [
  { key: 'input', title: '输入框' },
  { key: 'select', title: '下拉' },
  { key: 'daterange', title: '日期范围' },
];

export var QUERY_MODE_OPTIONS = [
  { key: '', title: '自动（按控件类型推导）' },
  { key: 'like', title: '模糊搜索' },
  { key: 'eq', title: '精确匹配' },
  { key: 'in', title: '多值匹配' },
  { key: 'range', title: '范围查询' },
];
