/**
 * 审批/待办相关接口
 *
 * 原始记录 LI_M02 的审批操作，对应桌面端 Store03.flowSave
 * 待办过滤通过 FilterParams 传 STATE + CHECKID/VERIFYID（当前用户 ID）
 *
 * 注意：CHECKID/VERIFYID/CREATEID 字段名以后端 F01 过滤模板为准，联调时确认
 */
import { query, open, flowSave, flowBatch } from './db'
import { APPROVE_ACTION } from '@/utils/state'

/**
 * 待办列表（后端 F011/F012/F013 用 @_USERID_ 自动按当前用户过滤，前端无需传 CHECKID/VERIFYID）
 * - A34/F011 待审核（CHECKID=我, STATE=2）
 * - A36/F012 待审批（VERIFYID=我, STATE=5）
 * - A40/F013 待签发（STATE=6）
 * - A01/F01 待提交/驳回（STATE IN 1,12）
 */
export function listToCheck(page) {
  return query('LI_M02', {}, page, { apiCode: 'A34' })
}
export function listToVerify(page) {
  return query('LI_M02', {}, page, { apiCode: 'A36' })
}
export function listToSign(page) {
  return query('LI_M02', {}, page, { apiCode: 'A40' })
}
export function listToSubmit(page) {
  return query('LI_M02', {}, page, { apiCode: 'A01' })
}

/** 记录详情（返回 { MAIN, DTSA, ... }），module 默认 LI_M02 */
export function getRecordDetail(id, module) {
  return open(module || 'LI_M02', id)
}

/** 复核通过（需选择下一审批人） */
export function doCheck(params) {
  return flowSave('LI_M02', APPROVE_ACTION.CHECK, params)
}
/** 撤销复核 */
export function undoCheck(params) {
  return flowSave('LI_M02', APPROVE_ACTION.RE_CHECK, params)
}
/** 审批通过 */
export function doVerify(params) {
  return flowSave('LI_M02', APPROVE_ACTION.VERIFY, params)
}
/** 撤销审批 */
export function undoVerify(params) {
  return flowSave('LI_M02', APPROVE_ACTION.RE_VERIFY, params)
}
/** 驳回 */
export function doReject(params) {
  return flowSave('LI_M02', APPROVE_ACTION.REJECT, params)
}
/** 批量审批 */
export function batchVerify(params) {
  return flowBatch('LI_M02', APPROVE_ACTION.BATCH_VERIFY, params)
}
/** 批量驳回 */
export function batchReject(params) {
  return flowBatch('LI_M02', APPROVE_ACTION.BATCH_REJECT_VERIFY, params)
}
