/**
 * 核心请求封装
 *
 * 完全对齐桌面端 p-admin/src/api/db.js 的请求机制：
 * - 表单编码：每个字段值 JSON.stringify 后整体 urlencode（tran 函数）
 * - Content-Type: application/x-www-form-urlencoded
 * - 自动注入 _userInfo_（登录后由 store 调 setUserInfoCache 填充）
 * - 自动注入 Authorization: Bearer {token}
 * - 响应格式 { Code:200, Data, Message }，Code=501 跳登录
 *
 * 用法（对齐桌面端 postData(param, type)）：
 *   postData({ api: '/api/data/call/LI_M02/A14/', params: {...} })          // 业务数据 5001
 *   postData({ api: '/api/user/login', USERNAME, PASSWORD }, 'user')        // 登录 5000
 */
import { getToken, removeToken } from './auth'
import { DATA_BASE_URL, AUTH_BASE_URL } from './config'

// _userInfo_ 缓存：登录成功后由 user store 写入，每个请求带上（对齐桌面端 store.state.user.userInfo）
let _userInfoCache = {}

export function setUserInfoCache(info) {
  _userInfoCache = info || {}
}

export function getUserInfoCache() {
  return _userInfoCache
}

/**
 * tran 编码：key=JSON.stringify(value)&key2=...（与桌面端 db.js 完全一致）
 * 后端 .NET 用 JsonConvert.DeserializeObject<String/Object> 反解
 */
function encodeBody(obj) {
  let ret = ''
  for (let key in obj) {
    if (obj[key] === undefined) continue
    ret += encodeURIComponent(key) + '=' + encodeURIComponent(JSON.stringify(obj[key])) + '&'
  }
  return ret.replace(/&$/, '')
}

// 是否正在跳转登录（避免重复跳转）
let isRedirecting = false
function redirectToLogin() {
  if (isRedirecting) return
  isRedirecting = true
  removeToken()
  _userInfoCache = {}
  uni.showToast({ title: '登录已过期，请重新登录', icon: 'none' })
  setTimeout(() => {
    uni.reLaunch({ url: '/pages/login/index' })
    isRedirecting = false
  }, 800)
}

/**
 * 核心请求（对齐桌面端 postData）
 * @param {Object} param 必须含 api 字段；业务参数放 param.params 或直接平铺（如 USERNAME/PASSWORD）
 * @param {string} [type='url'] 'url'=业务数据(5001) | 'user'=认证(5000)
 * @param {Object} [opts] { noAuth, silent }
 * @returns {Promise<any>} resolve(Data)
 */
export function postData(param, type = 'url', opts = {}) {
  const { noAuth = false, silent = false } = opts

  // 注入 _userInfo_（对齐桌面端 tpara['_userInfo_'] = store.state.user.userInfo）
  const tpara = { ...param }
  tpara._userInfo_ = _userInfoCache

  const base = type === 'user' ? AUTH_BASE_URL : DATA_BASE_URL
  const url = base + param.api
  const body = encodeBody(tpara)

  const header = {
    'Content-Type': 'application/x-www-form-urlencoded'
  }
  if (!noAuth) {
    const token = getToken()
    if (token) {
      header['Authorization'] = `Bearer ${token}`
    }
  }

  return new Promise((resolve, reject) => {
    uni.request({
      url,
      method: 'POST',
      data: body,
      header,
      timeout: 30000,
      success: (res) => {
        if (res.statusCode < 200 || res.statusCode >= 300) {
          if (res.statusCode === 401) redirectToLogin()
          if (!silent) uni.showToast({ title: `网络错误(${res.statusCode})`, icon: 'none' })
          return reject(new Error(`网络错误(${res.statusCode})`))
        }
        const data = res.data || {}
        // 兼容后端 { Code, Data, Message } 格式
        if (typeof data === 'object' && 'Code' in data) {
          if (data.Code === 200) return resolve(data.Data)
          if (data.Code === 501) {
            redirectToLogin()
            return reject(new Error(data.Message || '登录已过期'))
          }
          if (!silent) uni.showToast({ title: data.Message || '操作失败', icon: 'none' })
          return reject(new Error(data.Message || `请求失败(${data.Code})`))
        }
        resolve(data)
      },
      fail: (err) => {
        if (!silent) uni.showToast({ title: '网络连接失败', icon: 'none' })
        reject(new Error(err.errMsg || '网络连接失败'))
      }
    })
  })
}
