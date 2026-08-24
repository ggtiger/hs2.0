'use strict';

const getFieldsTem = function(that = window) {
  const fields = {
    itemLayout: {
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      cols: [24],
      width: '100%',
      height: '33px',
      size: 14,
      align: 'left',
      weight: false,
      parent: ''
    },
    itemLabel: {
      type: 'itemLabel',
      title: '文本',
      label: '文本',
      width: 'auto',
      height: '18px',
      size: 14,
      align: 'left',
      weight: false,
      field: 'NAME',
      content: '说明',
      parent: ''
    },
    itemField: {
      type: 'itemField',
      title: '输入框',
      width: '100%',
      height: '18px',
      labelProps: {
        size: 14,
        label: '文本',
        height: '18px'
      },
      fieldType: 'text',
      textMore: false,
      fieldProps: {
        size: 14,
        width: '100%',
        height: '18px'
      },
      field: 'NAME',
      content: '说明',
      parent: ''
    },
    itemCheckBox: {
      type: 'itemCheckBox',
      title: '勾选框',
      fieldType: 'checkBox',
      width: '100%',
      height: '18px',
      size: 14,
      align: 'left',
      weight: false,
      datas: [{ title: '选择1', key: 0 }],
      field: 'ISTEXT',
      content: '是否是多选',
      parent: ''
    },
    itemTable: {
      type: 'itemTable',
      title: '表格',
      width: '100%',
      height: '20px',
      row: 5
    },
    itemEditor: {
      type: 'itemEditor',
      title: '富文本',
      cell: 1,
      width: '100%',
      height: '50px',
      value: '请输入'
    }
  };

  return fields;
};

module.exports = getFieldsTem;
