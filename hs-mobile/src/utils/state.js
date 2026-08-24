/**
 * 单据状态映射
 *
 * 对应桌面端 review.vue 的 stateLabel，统一定义移动端状态标签与颜色
 * STATE 值与后端 TSS_BILLSTATE 一致
 */

export const BILL_STATE = {
  1: { label: '待提交', color: '#909399', type: 'default' },
  2: { label: '待审核', color: '#ff9900', type: 'warning' },
  3: { label: '已审核', color: '#2f7df6', type: 'primary' },
  4: { label: '已作废', color: '#f5222d', type: 'danger' },
  5: { label: '待审批', color: '#ff9900', type: 'warning' },
  6: { label: '已审批', color: '#07c160', type: 'success' },
  10: { label: '已签发', color: '#1a66d9', type: 'primary' },
  12: { label: '已驳回', color: '#f5222d', type: 'danger' },
  19: { label: '待审批', color: '#ff9900', type: 'warning' },
  20: { label: '已审批', color: '#07c160', type: 'success' }
}

/**
 * 获取状态信息
 * @param {number|string} state
 * @returns {{label:string,color:string,type:string}}
 */
export function getStateInfo(state) {
  return BILL_STATE[state] || { label: '未知', color: '#909399', type: 'default' }
}

/**
 * 审批操作码（APICODE）—— 对应桌面端 flowSave / LI_M02
 */
export const APPROVE_ACTION = {
  SUBMIT: 'A17',        // 提交
  RE_SUBMIT: 'A18',     // 撤销提交
  CHECK: 'A12',         // 复核通过
  RE_CHECK: 'A13',      // 撤销复核
  VERIFY: 'A14',        // 审批通过
  RE_VERIFY: 'A15',     // 撤销审批
  REJECT: 'A16',        // 驳回
  BATCH_CHECK: 'A23',   // 批量复核
  BATCH_VERIFY: 'A25',  // 批量审批
  BATCH_REJECT_CHECK: 'A28', // 批量复核驳回
  BATCH_REJECT_VERIFY: 'A29' // 批量审批驳回
}
