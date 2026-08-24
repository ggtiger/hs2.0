'use strict';

const getFields = function(that = window) {
  const fields = [
    {
      type: 'itemLayout',
      title: '布局',
      cell: 1,
      width: '100%',
      height: '50px',
      size: 12,
      align: 'left',
      weight: false
    }
  ];

  return fields;
};

module.exports = getFields;
