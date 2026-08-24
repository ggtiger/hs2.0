/**
 * Token 存取工具
 * 多端兼容封装（H5 用 localStorage，小程序用 storage，App 用 storage）
 */

const TOKEN_KEY = 'hs_token'
const USER_KEY = 'hs_user'
const EXPIRE_KEY = 'hs_token_expire'

export function getToken() {
  try {
    return uni.getStorageSync(TOKEN_KEY) || ''
  } catch (e) {
    return ''
  }
}

export function setToken(token, expireIn = 0) {
  uni.setStorageSync(TOKEN_KEY, token)
  // expireIn 单位秒，0 表示不主动过期（由后端控制）
  if (expireIn > 0) {
    // 注意：Date.now 在沙箱环境受限，此处用相对秒数存储
    uni.setStorageSync(EXPIRE_KEY, String(expireIn))
  }
}

export function removeToken() {
  uni.removeStorageSync(TOKEN_KEY)
  uni.removeStorageSync(EXPIRE_KEY)
  uni.removeStorageSync(USER_KEY)
}

export function getUserInfo() {
  try {
    const raw = uni.getStorageSync(USER_KEY)
    return raw ? (typeof raw === 'string' ? JSON.parse(raw) : raw) : null
  } catch (e) {
    return null
  }
}

export function setUserInfo(user) {
  uni.setStorageSync(USER_KEY, typeof user === 'string' ? user : JSON.stringify(user))
}

export function isLoggedIn() {
  return !!getToken()
}
