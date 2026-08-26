<script>
import { useUserStore } from '@/store/modules/user'
import { setUserInfoCache } from '@/utils/request'
import { getUserInfo } from '@/utils/auth'

export default {
  onLaunch() {
    console.log('睿谱希 App Launch')
    // 先恢复 _userInfo_ 缓存（不依赖 Pinia，确保后续请求带上用户信息）
    const info = getUserInfo()
    if (info) setUserInfoCache(info)
    // 再恢复 store 登录态
    try {
      const userStore = useUserStore()
      userStore.restore()
    } catch (e) {
      console.warn('登录态恢复失败', e)
    }
    // 检查更新（仅 App 端）
    // #ifdef APP-PLUS
    if (uni.getUpdateManager) {
      const updateManager = uni.getUpdateManager()
      updateManager.onCheckForUpdate(() => {})
      updateManager.onUpdateReady(() => {
        uni.showModal({
          title: '更新提示',
          content: '新版本已就绪，是否重启应用？',
          success: (res) => {
            if (res.confirm) updateManager.applyUpdate()
          }
        })
      })
    }
    // #endif
  },
  onShow() {
    console.log('App Show')
  },
  onHide() {
    console.log('App Hide')
  }
}
</script>

<style lang="scss">
/* 注意：App.vue 的 style 不能加 scoped，全局样式在此引入 */
@import '@/styles/tailwind.scss';
@import '@/styles/common.scss';
</style>
