/**
 * 应用状态管理
 * 管理菜单、权限点（对应桌面端 app store 的 initMenu / fpoints）
 */
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { loadPermissions as apiLoadPermissions } from '@/api/auth'

export const useAppStore = defineStore('app', () => {
  // 菜单树（FUNCTYPE=1 目录，FUNCTYPE=2 页面）
  const menus = ref([])
  // 权限点字典：{ 'FUNCCODE/FUNCPOINTCODE': true }
  const fpoints = ref({})
  // 字典缓存
  const dicts = ref({})

  /** 设置菜单数据 */
  function setMenus(list) {
    menus.value = list || []
  }

  /**
   * 加载当前用户功能权限点（对应桌面端 C00/A06，登录后调用）
   * @param {string} userId
   */
  async function loadPermissions(userId) {
    try {
      const items = await apiLoadPermissions(userId)
      setFpoints(items)
    } catch (e) {
      console.warn('权限点加载失败', e)
    }
    return fpoints.value
  }

  /** 设置权限点（对应桌面端 v-per 指令依赖的 fpoints） */
  function setFpoints(items) {
    const map = {}
    ;(items || []).forEach((item) => {
      const key = item.FUNCCODE && item.FUNCPOINTCODE
        ? `${item.FUNCCODE}/${item.FUNCPOINTCODE}`
        : item.CODE || ''
      if (key) map[key] = true
    })
    fpoints.value = map
  }

  /**
   * 权限点检查（对应桌面端 v-per 指令逻辑）
   * @param {string} code 'LI_M02/A12' 格式
   * @returns {boolean}
   */
  function hasPerm(code) {
    if (!code) return true
    return !!fpoints.value[code]
  }

  /** 设置字典 */
  function setDict(code, items) {
    dicts.value[code] = items
  }

  /** 重置（退出登录时） */
  function reset() {
    menus.value = []
    fpoints.value = {}
    dicts.value = {}
  }

  return {
    menus,
    fpoints,
    dicts,
    setMenus,
    loadPermissions,
    setFpoints,
    hasPerm,
    setDict,
    reset
  }
})
