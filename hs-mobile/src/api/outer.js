/**
 * 外部接口（客户自助，免认证）
 * 对应桌面端 out/ 模块，走 WebAPI 的 OuterController（端口 5001）
 * URL: {5001base}/api/outer/call/{Module}/{ApiCode}/
 */
import { postData } from '@/utils/request'
import { outerApiPath } from '@/utils/config'

/**
 * 外部通用调用（免认证）
 * @param {string} moduleCode
 * @param {string} apiCode
 * @param {Object} [filter] FilterParams 内容
 */
function outerCall(moduleCode, apiCode, filter = {}) {
  return postData(
    { api: outerApiPath(moduleCode, apiCode), params: { FilterParams: filter } },
    'url',
    { noAuth: true }
  )
}

/**
 * 检测进度查询（对应 OUT_M01/A01）
 * @param {Object} filter { BILLNO, PHONE }
 */
export function queryProgress(filter) {
  return outerCall('OUT_M01', 'A01', filter).then((res) => ({
    list: (res && res.Items) || (Array.isArray(res) ? res : []),
    total: (res && res.TotalCount) || 0
  }))
}

/** 证书核验详情（对应 OUT_M01/A02） */
export function verifyDetail(id) {
  return outerCall('OUT_M01', 'A02', { ID: id })
}

/**
 * 电子证书 —— 检查是否存在及是否需要密码（对应 LI_ECERT/A02）
 * @returns {Promise<{NEED_PWD:number, ...}>}
 */
export function checkEcert(filter) {
  return outerCall('LI_ECERT', 'A02', filter)
}

/** 电子证书 —— 查看详情（带密码验证，对应 LI_ECERT/A03） */
export function viewEcert(filter) {
  return outerCall('LI_ECERT', 'A03', filter)
}

/** 物流查询（对应 R02_M07/A10，外部免认证） */
export function queryLogisticsTrack(logisticsNo) {
  return outerCall('R02_M07', 'A10', { LOGISTICSNO: logisticsNo })
}
