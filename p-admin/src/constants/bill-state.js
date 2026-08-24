/**
 * 单据状态映射
 * 来源: r01/m025/m026 审批页面合并（取最全版本）
 */

export var BILL_STATE_MAP = {
  1: '待提交',
  2: '待审核',
  3: '已审核',
  4: '已作废',
  5: '待审批',
  6: '已审批',
  7: '待接收',
  8: '待检验',
  10: '已签发',
  12: '已驳回',
  19: '待审批',
  20: '已审批'
};

export var BILL_STATE_COLOR = function(state) {
  if (state == null) return '';
  if (state === 6 || state === 20) return '#19be6b';
  if (state === 12) return '#ed4014';
  if (state === 2 || state === 5) return '#ff9900';
  return '';
};
