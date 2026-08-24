/**
 * 用户状态管理
 * 对齐桌面端 p-admin/src/store/modules/user.js：
 * - token = res.token.access_token（IdentityServer4）
 * - userInfo = res.userInfo
 * - 登录成功标志：userInfo.status === 2
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getToken, setToken, removeToken, getUserInfo, setUserInfo } from '@/utils/auth'
import { setUserInfoCache } from '@/utils/request'
import { login as loginApi, logout as logoutApi, loadPermissions } from '@/api/auth'
import { useAppStore } from './app'
import { useTodoStore } from './todo'

export const useUserStore = defineStore('user', () => {
  const token = ref(getToken())
  const userInfo = ref(getUserInfo())

  // 已登录：有 token 且 userInfo 存在（后端成功返回对象，失败返回 false）
  const isLogged = computed(() => !!token.value && !!userInfo.value)
  const userName = computed(() => userInfo.value?.NICKNAME || userInfo.value?.EMPNAME || userInfo.value?.USERNAME || '')
  const deptName = computed(() => userInfo.value?.DEPTNAME || userInfo.value?.EMPNAME || '')
  const empId = computed(() => userInfo.value?.ID || userInfo.value?.EMPID || '')

  /** 从本地存储恢复登录态（App.onLaunch 调用） */
  function restore() {
    token.value = getToken()
    userInfo.value = getUserInfo()
    // 恢复 _userInfo_ 缓存，保证后续请求带上
    if (userInfo.value) setUserInfoCache(userInfo.value)
  }

  /**
   * 登录
   * @returns {Promise<Object>} res = { userInfo, token }
   */
  async function login(username, password) {
    const res = await loginApi(username, password)

    // 后端约定：失败返回 Data:false（用户不存在/密码错）；成功返回 {userInfo, token}
    if (res === false || res == null || typeof res !== 'object') {
      throw new Error('用户名或密码错误')
    }
    const info = res.userInfo || {}
    const tk = res.token?.access_token || ''
    // 停用检查（ISUSE=0 表示停用）
    if (info.ISUSE === '0' || info.ISUSE === 0) {
      throw new Error('用户已停用')
    }

    if (tk) setToken(tk)
    setUserInfo(info)
    setUserInfoCache(info)
    token.value = tk
    userInfo.value = info

    // 登录成功后初始化权限与待办（对齐桌面端登录后 initMenu）
    if (info.status === 2 || info.STATUS === 2) {
      try {
        const appStore = useAppStore()
        appStore.loadPermissions(info.ID)
        const todoStore = useTodoStore()
        todoStore.fetchStats()
      } catch (e) {
        console.warn('登录后初始化失败', e)
      }
    }
    return res
  }

  /** 退出登录 */
  async function logout() {
    try {
      await logoutApi()
    } catch (e) {}
    removeToken()
    setUserInfoCache({})
    token.value = ''
    userInfo.value = null
    // 清空权限与待办
    try {
      const appStore = useAppStore()
      appStore.reset()
    } catch (e) {}
  }

  return {
    token,
    userInfo,
    isLogged,
    userName,
    deptName,
    empId,
    restore,
    login,
    logout
  }
})
