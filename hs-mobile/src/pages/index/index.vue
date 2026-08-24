<template>
  <view class="page">
    <!-- 自定义导航栏（渐变背景延伸到状态栏） -->
    <view class="header" :style="{ paddingTop: statusBarHeight + 'px' }">
      <view class="header__content">
        <view>
          <view class="header__welcome">你好，{{ userName || '请登录' }}</view>
          <view class="header__dept">{{ deptName }}</view>
        </view>
      </view>
    </view>

    <!-- 待办统计（待审核/待审批/待签发） -->
    <view class="stats-row">
      <stat-card
        :value="todoStore.getCountByType('check')"
        label="待审核"
        bg="linear-gradient(135deg, #ff9a4d, #ff7a00)"
        @click="goTodo('check')"
      />
      <stat-card
        :value="todoStore.getCountByType('verify')"
        label="待审批"
        bg="linear-gradient(135deg, #2f7df6, #1a66d9)"
        @click="goTodo('verify')"
      />
      <stat-card
        :value="todoStore.getCountByType('sign')"
        label="待签发"
        bg="linear-gradient(135deg, #36d1a6, #07c160)"
        @click="goTodo('sign')"
      />
    </view>

    <!-- 提醒（标准器过期/应溯源设备等） -->
    <view v-if="reminds.length" class="section">
      <view class="section__title">提醒</view>
      <view class="remind-list">
        <view v-for="item in reminds" :key="item.TITLE" class="remind-item" @click="goRemind(item)">
          <text class="remind-item__icon">⚠️</text>
          <text class="remind-item__title">{{ item.TITLE }}</text>
          <text class="remind-item__cnt">{{ item.CNT }}</text>
          <text class="remind-item__arrow">›</text>
        </view>
      </view>
    </view>

    <!-- 常用功能 -->
    <view class="section">
      <view class="section__title">常用功能</view>
      <view class="func-grid">
        <view
          v-for="func in commonFuncs"
          :key="func.path"
          class="func-item"
          @click="goPage(func.path)"
        >
          <view class="func-item__icon" :style="{ background: func.bg }">{{ func.icon }}</view>
          <text class="func-item__name">{{ func.name }}</text>
        </view>
      </view>
    </view>

    <!-- 系统公告 -->
    <view class="section">
      <view class="section__head">
        <text class="section__title">系统公告</text>
        <text class="section__more" @click="goNotice">更多 ›</text>
      </view>
      <view class="notice-list">
        <view
          v-for="notice in notices"
          :key="notice.ID"
          class="notice-item"
          @click="goNoticeDetail(notice.ID)"
        >
          <view class="notice-item__dot"></view>
          <view class="notice-item__main">
            <text class="notice-item__title">{{ notice.NOTITLE || '—' }}</text>
            <text class="notice-item__date">{{ formatDate(notice.BILLDATE) }}</text>
          </view>
        </view>
        <empty-state v-if="!notices.length" icon="📢" text="暂无公告" />
      </view>
    </view>

    <!-- 效能概览（从全员效能列表聚合：总完成量 + 平均效能系数） -->
    <view class="section">
      <view class="section__title">效能概览</view>
      <view class="efficiency">
        <view class="efficiency__item">
          <text class="efficiency__value">{{ efficiency.count }}</text>
          <text class="efficiency__label">总完成量</text>
        </view>
        <view class="efficiency__divider"></view>
        <view class="efficiency__item">
          <text class="efficiency__value">{{ efficiency.score }}</text>
          <text class="efficiency__label">平均效能</text>
        </view>
        <view class="efficiency__divider"></view>
        <view class="efficiency__item">
          <text class="efficiency__value">{{ efficiency.people }}</text>
          <text class="efficiency__label">在岗人数</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { onShow, onPullDownRefresh } from '@dcloudio/uni-app'
import { useUserStore, useTodoStore } from '@/store'
import { getNotices, getEfficiencyStats, getTodoStats } from '@/api/home'
import { formatDate } from '@/utils/format'

const userStore = useUserStore()
const todoStore = useTodoStore()

const statusBarHeight = ref(20)
const userName = ref(userStore.userName)
const deptName = ref(userStore.deptName)

const notices = ref([])
const reminds = ref([])
const efficiency = reactive({ count: 0, score: '0.0', people: 0 })

// 常用功能（本地配置 + 后端常用功能合并）
const commonFuncs = ref([
  { name: '委托查询', icon: '📋', bg: '#e8f1fe', path: '/pages/query/delegate' },
  { name: '记录查询', icon: '📄', bg: '#e6f7ec', path: '/pages/query/record' },
  { name: '证书查询', icon: '📜', bg: '#fff3e0', path: '/pages/query/cert' },
  { name: '物流查询', icon: '📦', bg: '#fde8ef', path: '/pages/query/logistics' },
  { name: '批量审批', icon: '✅', bg: '#e8f1fe', path: '/pages/approve/batch' },
  { name: '我的资质', icon: '🏅', bg: '#f0e8fd', path: '/pages/mine/cert' }
])

onShow(() => {
  // 获取状态栏高度
  try {
    statusBarHeight.value = uni.getSystemInfoSync().statusBarHeight || 20
  } catch (e) {}
  userName.value = userStore.userName
  deptName.value = userStore.deptName
  loadData()
})

onPullDownRefresh(() => {
  loadData().finally(() => uni.stopPullDownRefresh())
})

async function loadData() {
  // 并行加载，任一失败不影响其他
  await Promise.allSettled([
    todoStore.fetchStats(),
    loadReminds(),
    loadNotices(),
    loadEfficiency()
  ])
}

async function loadReminds() {
  try {
    // C02/A03 返回待办+提醒混合；提醒类 LURL 以 b01 开头（标准器/设备），审批类 r01 已在待办卡片
    const res = await getTodoStats()
    const items = Array.isArray(res) ? res : (res?.list || res?.Items || [])
    reminds.value = items.filter((i) => i.LURL && i.LURL.indexOf('b01') === 0)
  } catch (e) {
    reminds.value = []
  }
}

async function loadNotices() {
  try {
    const res = await getNotices()
    notices.value = res?.list || res || []
  } catch (e) {
    notices.value = []
  }
}

async function loadEfficiency() {
  try {
    // LIR_M02/A01 返回全员效能列表 {Items:[{EMPNAME,F1,XNXS,...}]}
    const res = await getEfficiencyStats()
    const items = res?.Items || (Array.isArray(res) ? res : [])
    const totalF1 = items.reduce((s, i) => s + (Number(i.F1) || 0), 0)
    const totalXnxs = items.reduce((s, i) => s + (Number(i.XNXS) || 0), 0)
    Object.assign(efficiency, {
      count: totalF1,
      score: items.length ? (totalXnxs / items.length).toFixed(1) : '0.0',
      people: items.length
    })
  } catch (e) {}
}

function goTodo(type) {
  // 待办列表是 tabBar 页面；通过 store 传递要激活的 tab
  if (type) todoStore.setActiveTab(type)
  uni.switchTab({ url: '/pages/todo/list' })
}
function goRemind(item) {
  // 提醒（标准器过期/应溯源设备）对应桌面端设备管理，移动端暂无对应页面
  uni.showToast({ title: `${item.TITLE}（${item.CNT}）：请在桌面端处理`, icon: 'none' })
}
function goNotice() {
  uni.navigateTo({ url: '/pages/notice/list' })
}
function goNoticeDetail(id) {
  uni.navigateTo({ url: `/pages/notice/detail?id=${id}` })
}
function goPage(path) {
  uni.navigateTo({ url: path })
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background-color: #f5f7fa;
  padding-bottom: 40rpx;
}

.header {
  background: linear-gradient(135deg, #2f7df6, #1a66d9);
  padding: 0 32rpx 60rpx;
  color: #fff;

  &__content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 24rpx 0;
  }

  &__welcome {
    font-size: 36rpx;
    font-weight: 600;
  }

  &__dept {
    margin-top: 8rpx;
    font-size: 24rpx;
    opacity: 0.9;
  }
}

.stats-row {
  display: flex;
  gap: 20rpx;
  padding: 0 24rpx;
  margin-top: -40rpx;
}

.remind-list {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 0 24rpx;
}

.remind-item {
  display: flex;
  align-items: center;
  padding: 24rpx 0;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__icon {
    font-size: 32rpx;
    margin-right: 16rpx;
  }

  &__title {
    flex: 1;
    font-size: 28rpx;
    color: #333;
  }

  &__cnt {
    min-width: 44rpx;
    height: 36rpx;
    line-height: 36rpx;
    padding: 0 12rpx;
    margin-right: 8rpx;
    border-radius: 18rpx;
    background-color: #fff3e0;
    color: #ff9900;
    font-size: 24rpx;
    text-align: center;
  }

  &__arrow {
    font-size: 32rpx;
    color: #c0c4cc;
  }
}

.section {
  margin: 32rpx 24rpx 0;

  &__title {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
    margin-bottom: 20rpx;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 20rpx;
  }

  &__more {
    font-size: 24rpx;
    color: #909399;
  }
}

.func-grid {
  display: flex;
  flex-wrap: wrap;
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx 0;
}

.func-item {
  width: 25%;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16rpx 0;

  &__icon {
    width: 88rpx;
    height: 88rpx;
    border-radius: 24rpx;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 40rpx;
  }

  &__name {
    margin-top: 12rpx;
    font-size: 24rpx;
    color: #333;
  }
}

.notice-list {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 8rpx 24rpx;
}

.notice-item {
  display: flex;
  align-items: center;
  padding: 24rpx 0;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__dot {
    width: 12rpx;
    height: 12rpx;
    border-radius: 50%;
    background-color: #2f7df6;
    margin-right: 20rpx;
    flex-shrink: 0;
  }

  &__main {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  &__title {
    flex: 1;
    font-size: 28rpx;
    color: #333;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  &__date {
    margin-left: 20rpx;
    font-size: 24rpx;
    color: #c0c4cc;
    flex-shrink: 0;
  }
}

.efficiency {
  display: flex;
  align-items: center;
  background-color: #fff;
  border-radius: 16rpx;
  padding: 32rpx 0;

  &__item {
    flex: 1;
    text-align: center;
  }

  &__value {
    display: block;
    font-size: 48rpx;
    font-weight: 700;
    color: #2f7df6;
  }

  &__label {
    margin-top: 8rpx;
    font-size: 24rpx;
    color: #909399;
  }

  &__divider {
    width: 1rpx;
    height: 60rpx;
    background-color: #f0f0f0;
  }
}
</style>
