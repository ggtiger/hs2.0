/**
 * 认证接口
 * 复用桌面端 IdentityServer4 认证（Realso.Auth，端口 5000）
 *
 * 对齐桌面端 p-admin/src/api/user.js：
 * - login:   postData({ api:'/api/user/login', USERNAME, PASSWORD }, 'user')
 * - loginout: postData({ api:'/api/user/loginout' }, 'user')
 *
 * 后端 UserController.Login([FromForm] USERNAME, [FromForm] PASSWORD)，
 * 值经 JsonConvert.DeserializeObject<String> 反解（即 tran 编码的带引号字符串）
 */
import { postData } from '@/utils/request'

/**
 * 登录
 * @param {string} username
 * @param {string} password
 * @returns {Promise<{userInfo:Object, token:{access_token:string,...}}>}
 *   登录成功：userInfo.status === 2
 */
export function login(username, password) {
  return postData(
    {
      api: '/api/user/login',
      USERNAME: username,
      PASSWORD: password
    },
    'user',
    { noAuth: true }
  )
}

/** 退出登录 */
export function logout() {
  return postData({ api: '/api/user/loginout' }, 'user').catch(() => {})
}

/**
 * 加载当前用户功能权限点（对应桌面端 C00/A06）
 * @param {string} userId
 * @returns {Promise<Array>} 权限点列表
 */
export function loadPermissions(userId) {
  return postData(
    {
      api: `/api/data/call/C00/A06/`,
      params: { FilterParams: { USERID: userId } }
    },
    'url',
    { silent: true }
  ).then((res) => (Array.isArray(res) ? res : res?.Items || []))
}
