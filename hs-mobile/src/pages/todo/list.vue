<template>
  <view class="page">
    <!-- 顶部 Tab 切换 -->
    <view class="tabs">
      <view
        v-for="tab in tabs"
        :key="tab.type"
        class="tabs__item"
        :class="{ 'tabs__item--active': activeType === tab.type }"
        @click="switchTab(tab.type)"
      >
        <text>{{ tab.label }}</text>
        <text v-if="tab.count > 0" class="tabs__count">{{ tab.count }}</text>
      </view>
    </view>

    <!-- 列表 -->
    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="onLoadMore"
    >
      <view class="list">
        <todo-card
          v-for="item in list"
          :key="item.ID"
          :item="item"
          @click="goDetail(item)"
        />
        <empty-state v-if="!loading && !list.length" text="暂无待办" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { useTodoStore } from '@/store'
import { listToCheck, listToVerify, listToSign } from '@/api/approve'

const todoStore = useTodoStore()

const tabs = [
  { type: 'check', label: '待审核' },
  { type: 'verify', label: '待审批' },
  { type: 'sign', label: '待签发' }
]

const activeType = ref('check')
const list = ref([])
const loading = ref(false)
const refreshing = ref(false)
const pageIndex = ref(1)
const pageSize = 20
const hasMore = ref(true)

// 各 Tab 数量徽标（从 todoStore 获取）
const tabCounts = computed(() => ({
  check: todoStore.getCountByType('check'),
  verify: todoStore.getCountByType('verify'),
  sign: todoStore.getCountByType('sign')
}))

onLoad((options) => {
  if (options.type) activeType.value = options.type
})

onShow(() => {
  // 工作台点击待办卡片时通过 store 指定要激活的 tab
  if (todoStore.activeTab && tabs.some((t) => t.type === todoStore.activeTab) && todoStore.activeTab !== activeType.value) {
    activeType.value = todoStore.activeTab
  }
  // 同步徽标数量
  tabs.forEach((t) => {
    t.count = tabCounts.value[t.type] || 0
  })
  loadList(true)
})

function switchTab(type) {
  if (activeType.value === type) return
  activeType.value = type
  loadList(true)
}

async function loadList(reset = false) {
  if (loading.value) return
  if (reset) {
    pageIndex.value = 1
    hasMore.value = true
    list.value = []
  }
  if (!hasMore.value) return

  loading.value = true
  try {
    const fetcher = {
      check: listToCheck,
      verify: listToVerify,
      sign: listToSign
    }[activeType.value]
    const res = await fetcher({ pageIndex: pageIndex.value, pageSize })
    const newList = res.list || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {
    // 静默处理，loading 提示由请求层统一
  } finally {
    loading.value = false
  }
}

function onRefresh() {
  refreshing.value = true
  Promise.all([todoStore.fetchStats(), loadList(true)])
    .finally(() => {
      refreshing.value = false
    })
}

function onLoadMore() {
  if (hasMore.value && !loading.value) loadList(false)
}

function goDetail(item) {
  // 统一跳转审批详情页，带类型参数控制操作按钮
  uni.navigateTo({
    url: `/pages/approve/detail?id=${item.ID}&type=${activeType.value}`
  })
}
</script>

<style lang="scss" scoped>
.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
}

.tabs {
  display: flex;
  background-color: #fff;
  position: relative;
  z-index: 2;

  &__item {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 88rpx;
    font-size: 28rpx;
    color: #666;
    position: relative;

    &--active {
      color: #2f7df6;
      font-weight: 600;

      &::after {
        content: '';
        position: absolute;
        bottom: 0;
        left: 50%;
        transform: translateX(-50%);
        width: 48rpx;
        height: 6rpx;
        border-radius: 3rpx;
        background-color: #2f7df6;
      }
    }
  }

  &__count {
    margin-left: 8rpx;
    min-width: 32rpx;
    height: 32rpx;
    line-height: 32rpx;
    padding: 0 8rpx;
    border-radius: 16rpx;
    background-color: #f5222d;
    color: #fff;
    font-size: 20rpx;
    text-align: center;
  }
}

.list-scroll {
  flex: 1;
  overflow: hidden;
}

.list {
  padding: 20rpx 24rpx;
}

.loading-tip {
  text-align: center;
  padding: 24rpx;
  font-size: 24rpx;
  color: #c0c4cc;
}
</style>
