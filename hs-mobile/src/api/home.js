/**
 * 首页/工作台相关接口
 *
 * 复用桌面端首页 store（namespace: c02）的接口：
 * - C02/A01 公告列表
 * - C02/A02 常用功能
 * - C02/A03 待办提醒统计
 * - LIR_M01/A01 检测统计
 * - LIR_M02/A01 效能统计
 */
import { call, query } from './db'

/** 提醒列表（标准器过期/应溯源设备等，来自 C02/A03） */
export function getTodoStats() {
  return call('C02', 'A03', { FilterParams: {} }, { silent: true })
    .then((res) => Array.isArray(res) ? res : (res?.Items || []))
}

/** 审批待办数量（待审核 A34 / 待审批 A36 / 待签发 A40，后端按当前用户过滤）
 *  注意 PageSize 不能为 1（后端 PageSize==1 时不返回 TotalCount），用 2 */
export function getApproveCounts() {
  return Promise.all([
    query('LI_M02', {}, { pageSize: 2, pageIndex: 1 }, { apiCode: 'A34', silent: true }),
    query('LI_M02', {}, { pageSize: 2, pageIndex: 1 }, { apiCode: 'A36', silent: true }),
    query('LI_M02', {}, { pageSize: 2, pageIndex: 1 }, { apiCode: 'A40', silent: true })
  ]).then(([c, v, s]) => ({
    check: c.total || 0,
    verify: v.total || 0,
    sign: s.total || 0
  }))
}

/** 常用功能入口（C02/A02） */
export function getCommonFuncs() {
  return call('C02', 'A02', { FilterParams: {} })
    .then((res) => Array.isArray(res) ? res : (res?.Items || []))
}

/** 系统公告列表（C02/A01） */
export function getNotices(page = { pageIndex: 1, pageSize: 5 }) {
  return query('C02', {}, page, { apiCode: 'A01' })
}

/** 公告详情（对应 RS_M08/A02） */
export function getNoticeDetail(id) {
  return call('RS_M08', 'A02', { FilterParams: { ID: id } })
}

/** 检测统计（对应 LIR_M01/A01） */
export function getCheckStats() {
  return call('LIR_M01', 'A01', { FilterParams: {} }, { silent: true })
}

/** 效能统计（对应 LIR_M02/A01） */
export function getEfficiencyStats() {
  return call('LIR_M02', 'A01', { FilterParams: {} }, { silent: true })
}
