/**
 * 核心数据接口封装（对齐桌面端 p-admin Store03 + db.js）
 *
 * 后端请求机制：
 * - URL: {5001base}/api/data/call/{Module}/{ApiCode}/
 * - 表单编码：params 整体被 JSON.stringify 后 urlencode（由 request.js postData 处理）
 * - 参数结构遵循桌面端约定（FilterParams/PageSize/PageIndex/ID 等）
 *
 * 与桌面端 Store03 的对应关系：
 * - query    → 列表查询，返回 { list, total }（源自后端 Items/TotalCount）
 * - open     → 详情，返回 { MAIN, DTSA, ... }（按 PATHNAME 分组）
 * - call     → 通用调用，原样返回 Data
 * - flowSave → 审批流操作，参数 { ID, ... }
 * - batch    → 批量操作，参数 { IDS/ID, REMARK, ... }
 */
import { postData } from '@/utils/request'
import { dataApiPath } from '@/utils/config'

/**
 * 通用调用（call 的底层）
 * @param {string} moduleCode 模块编码，如 LI_M02
 * @param {string} apiCode 接口编码，如 A14
 * @param {Object} [params] 业务参数（会作为 FilterParams 或平铺字段，按 APITYPE 决定）
 * @param {Object} [opts] { silent }
 */
export function call(moduleCode, apiCode, params = {}, opts = {}) {
  return postData(
    { api: dataApiPath(moduleCode, apiCode), params },
    'url',
    { silent: opts.silent }
  )
}

/**
 * 列表查询（对应 Store03.query）
 * @param {string} moduleCode
 * @param {Object} [filter] FilterParams 内容（字段名→值，会进入后端 F01 模板）
 * @param {Object} [page] { pageIndex, pageSize }
 * @param {Object} [extra] { apiCode='A01', silent }
 * @returns {Promise<{list:Array,total:number}>}
 */
export function query(moduleCode, filter = {}, page = {}, extra = {}) {
  // 后端 F01 过滤器用 @INPUT 作统一搜索词（LIKE 多个字段），Dapper 要求必须提供
  // 上层传 KEYWORD 自动映射为 INPUT（兼容查询页搜索框）
  const FilterParams = { ...filter }
  if (FilterParams.KEYWORD !== undefined) {
    FilterParams.INPUT = FilterParams.INPUT || FilterParams.KEYWORD
    delete FilterParams.KEYWORD
  }
  if (FilterParams.INPUT === undefined) {
    FilterParams.INPUT = ''
  }
  const params = {
    FilterParams,
    PageSize: page.pageSize || 20,
    PageIndex: page.pageIndex || 1
  }
  return call(moduleCode, extra.apiCode || 'A01', params, { silent: extra.silent }).then((res) => ({
    list: (res && res.Items) || [],
    total: (res && res.TotalCount) || 0
  }))
}

/**
 * 详情查询（对应 Store03.open）
 * @param {string} moduleCode
 * @param {string} id 主键值
 * @param {Object} [opts] { apiCode='A02', apiParam='ID' }
 * @returns {Promise<Object>} { MAIN:[...], DTSA:[...], ... }
 */
export function open(moduleCode, id, opts = {}) {
  const params = { FilterParams: {} }
  params.FilterParams[opts.apiParam || 'ID'] = id
  return call(moduleCode, opts.apiCode || 'A02', params)
}

/**
 * 审批流操作（对应 Store03.flowSave）
 * @param {string} moduleCode
 * @param {string} actionCode 审批动作码，如 A12(复核)/A14(审批)/A16(驳回)
 * @param {Object} [params] { ID, REMARK, NEXTAPRID, NEXTAPRER }
 */
export function flowSave(moduleCode, actionCode, params = {}) {
  return call(moduleCode, actionCode, params).then((res) => {
    uni.showToast({ title: '操作成功', icon: 'success' })
    return res
  })
}

/**
 * 批量操作（对应 Store03.batch）
 * @param {string} moduleCode
 * @param {string} actionCode 如 A23(批量复核)/A25(批量审批)
 * @param {Object} params { IDS:[...], REMARK, ... }
 */
export function flowBatch(moduleCode, actionCode, params = {}) {
  // 对齐桌面端 batch：同时传 FilterParams.ID(数组) 和 ID(逗号串)
  const ids = params.IDS || params.ID || []
  const idList = Array.isArray(ids) ? ids : String(ids).split(',')
  const body = { ...params }
  body.FilterParams = { ...(body.FilterParams || {}), ID: idList }
  body.ID = idList.join(',')
  delete body.IDS
  return call(moduleCode, actionCode, body).then((res) => {
    uni.showToast({ title: '批量操作成功', icon: 'success' })
    return res
  })
}

/**
 * 保存（对应 Store03.save，DataTable XML 保存）
 * 移动端仅做简单新增/编辑场景；复杂主子表保存仍建议在桌面端完成
 * @param {string} moduleCode
 * @param {string} apiCode 如 A04
 * @param {Object} pathData 各 PATHNAME 的数据（{ MAIN: {...}, DTSA: [...] }）
 */
export function save(moduleCode, apiCode, pathData = {}) {
  return call(moduleCode, apiCode, pathData)
}
