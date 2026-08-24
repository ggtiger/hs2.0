/**
 * 业务查询接口
 * 委托单/受理单/原始记录/证书/费用/物流
 */
import { query, open } from './db'

/** 委托单查询（LI_M06） */
export function queryDelegate(filter, page) {
  return query('LI_M06', filter, page, { apiCode: 'A01' })
}
export function openDelegate(id) {
  return open('LI_M06', id)
}

/** 受理单查询（LI_M00） */
export function queryAccept(filter, page) {
  return query('LI_M00', filter, page, { apiCode: 'A01' })
}
export function openAccept(id) {
  return open('LI_M00', id)
}

/** 原始记录查询（LI_M02） */
export function queryRecord(filter, page) {
  return query('LI_M02', filter, page, { apiCode: 'A01' })
}
export function openRecord(id) {
  return open('LI_M02', id)
}

/** 证书查询（基于 LI_M02 已签发记录 STATE=10） */
export function queryCert(filter, page) {
  return query('LI_M02', { STATE: 10, ...filter }, page, { apiCode: 'A01' })
}

/** 费用查询（LI_M03，F01 过滤器需 BUSTYPEID 参数） */
export function queryFee(filter, page) {
  return query('LI_M03', { BUSTYPEID: '', ...filter }, page, { apiCode: 'A01' })
}
export function openFee(id) {
  return open('LI_M03', id)
}

/** 物流查询（R02_M07） */
export function queryLogistics(filter, page) {
  return query('R02_M07', filter, page, { apiCode: 'A01' })
}
