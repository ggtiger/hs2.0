/**
 * 待办状态管理
 * 缓存待办统计，供工作台徽标与待办列表共享
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getApproveCounts } from '@/api/home'

export const useTodoStore = defineStore('todo', () => {
  // 审批待办数量：待审核/待审批/待签发
  const stats = ref({ check: 0, verify: 0, sign: 0 })
  // 工作台点击待办卡片时要激活的 tab（跨页传递，因 switchTab 不能带参数）
  const activeTab = ref('check')

  // 待办总数（铃铛徽标）
  const totalCount = computed(() => stats.value.check + stats.value.verify + stats.value.sign)

  /** 拉取审批待办数量（A34/A36/A40） */
  async function fetchStats() {
    try {
      const res = await getApproveCounts()
      stats.value = { check: res.check, verify: res.verify, sign: res.sign }
    } catch (e) {
      console.warn('待办统计拉取失败', e)
    }
    return stats.value
  }

  /** 按类型获取数量：check/verify/sign */
  function getCountByType(type) {
    return stats.value[type] || 0
  }

  /** 设置待办中心要激活的 tab */
  function setActiveTab(type) {
    activeTab.value = type
  }

  return {
    stats,
    activeTab,
    totalCount,
    fetchStats,
    getCountByType,
    setActiveTab
  }
})
