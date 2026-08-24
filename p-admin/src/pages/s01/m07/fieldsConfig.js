'use strict';

const getFields = function(that = window) {
  const fields = [
    {
      id: 'layout1',
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      width: '100%',
      height: '50px',
      size: 12,
      align: 'left',
      weight: false,
      children: [
        {
          id: 'layout11',
          type: 'itemLabel',
          title: '文本',
          label: 'QR-JT33（1）',
          width: '100%',
          height: '33px',
          size: 18,
          align: 'right',
          weight: false,
          field: 'NAME',
          content: '说明',
          parent: 'layout1'
        }
      ]
    },
    {
      id: 'layout2',
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      width: '100%',
      height: '33px',
      align: 'center',
      children: [
        {
          id: 'layout21',
          type: 'itemLabel',
          title: '文本',
          label: '安 徽 省 计 量 科 学 研 究 院',
          size: 21,
          height: '33px',
          align: 'center',
          weight: false,
          field: 'NAME',
          content: '说明',
          parent: 'layout2'
        }
      ]
    },
    {
      id: 'layout3',
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      height: '33px',
      align: 'center',
      width: '100%',
      children: [
        {
          id: 'layout31',
          type: 'itemLabel',
          title: '文本',
          label: '机动车前照灯检测仪校准记录',
          size: 29,
          height: '33px',
          field: 'NAME',
          content: '说明',
          parent: 'layout3'
        }
      ]
    },
    {
      id: 'layout4',
      type: 'itemLayout',
      title: '布局',
      height: '20px',
      children: []
    },
    {
      id: 'layout5',
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      align: 'right',
      width: '100%',
      height: '33px',
      children: [
        {
          id: 'layout51',
          type: 'itemLabel',
          title: '文本',
          label: '证书编号：678456S',
          size: 14,
          align: 'right',
          height: '33px',
          field: 'NAME',
          content: '说明',
          parent: 'layout5'
        }
      ]
    },
    {
      id: 'layout6',
      type: 'itemLayout',
      title: '布局',
      height: '33px'
    },
    {
      id: 'layout7',
      type: 'itemLayout',
      title: '布局',
      cell: 2,
      height: '33px',
      children: [
        {
          id: 'field1',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '设备名称：',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        },
        {
          id: 'field2',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '送校单位：',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        }
      ]
    },
    {
      id: 'layout8',
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      width: '100%',
      height: '33px',
      children: [
        {
          id: 'field3',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '委托方地址：',
            align: 'center',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        }
      ]
    },
    {
      id: 'layout9',
      type: 'itemLayout',
      title: '布局',
      cell: 2,
      height: '33px',
      children: [
        {
          id: 'field4',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '型号规格：',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        },
        {
          id: 'field5',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '出厂编号：',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        }
      ]
    },
    {
      id: 'layout10',
      type: 'itemLayout',
      title: '布局',
      cell: 2,
      height: '33px',
      children: [
        {
          children: [
            {
              id: 'field6',
              type: 'itemField',
              title: '输入框',
              labelProps: {
                size: 14,
                label: '生产厂家：',
                height: '14px'
              },
              fieldType: 'text',
              fieldProps: {
                size: 14,
                width: '100%',
                height: '14px'
              },
              field: 'NAME',
              content: '说明'
            }
          ]
        },
        {
          children: [
            {
              id: 'label5',
              type: 'itemLabel',
              title: '文本',
              label: '校准条件：',
              size: 14,
              align: 'right',
              height: '14px',
              width: '70px'
            },
            {
              id: 'field7',
              type: 'itemField',
              title: '输入框',
              labelProps: {
                size: 14,
                label: '温度：',
                height: '14px'
              },
              fieldType: 'text',
              fieldProps: {
                size: 14,
                width: '100%',
                height: '14px'
              },
              field: 'NAME',
              content: '说明'
            },
            {
              id: 'label6',
              type: 'itemLabel',
              title: '文本',
              label: '℃',
              size: 14,
              width: 'auto',
              height: '14px'
            },
            {
              id: 'field8',
              type: 'itemField',
              title: '输入框',
              labelProps: {
                size: 14,
                label: '相对湿度：',
                height: '14px'
              },
              fieldType: 'text',
              fieldProps: {
                size: 14,
                width: '100%',
                height: '14px'
              },
              field: 'NAME',
              content: '说明'
            },
            {
              id: 'label7',
              type: 'itemLabel',
              title: '文本',
              label: '%',
              size: 14,
              width: 'auto',
              height: '14px'
            }
          ]
        }
      ]
    },
    {
      id: 'layout11',
      type: 'itemLayout',
      title: '布局',
      height: '20px'
    },
    {
      id: 'layout12',
      type: 'itemLayout',
      title: '布局',
      height: '20px'
    },
    {
      title: '富文本',
      type: 'ueditor',
      content: ''
    },
    {
      id: 'layout13',
      type: 'itemLayout',
      title: '布局',
      height: '20px'
    },
    {
      id: 'layout14',
      type: 'itemLayout',
      title: '布局',
      cell: 3,
      height: '33px',
      children: [
        {
          id: 'field9',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '校准员：',
            height: '14px'
          },
          fieldType: 'text',
          data: '1111',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        },
        {
          id: 'field10',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '核验员：',
            height: '14px'
          },
          fieldType: 'text',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        },
        {
          id: 'field11',
          type: 'itemField',
          title: '输入框',
          labelProps: {
            size: 14,
            label: '校准时间：',
            height: '14px'
          },
          fieldType: 'date',
          fieldProps: {
            size: 14,
            width: '100%',
            height: '14px'
          },
          field: 'NAME',
          content: '说明'
        }
      ]
    }
  ];

  return fields;
};

module.exports = getFields;
