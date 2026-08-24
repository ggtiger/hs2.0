<template>
  <view class="page">
    <search-bar v-model="keyword" :placeholder="'搜索单据号 / 设备 / 客户'" @search="onSearch" />

    <!-- 状态筛选 -->
    <scroll-view scroll-x class="filter-bar" show-scrollbar="false">
      <view
        v-for="item in stateFilters"
        :key="item.value"
        class="filter-item"
        :class="{ 'filter-item--active': activeState === item.value }"
        @click="onFilter(item.value)"
      >
        {{ item.label }}
      </view>
    </scroll-view>

    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="onLoadMore"
    >
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="item-card" @click="goDetail(item)">
          <view class="item-card__header">
            <text class="item-card__no">{{ item.BILLCODE || item.ID }}</text>
            <state-tag :state="item.STATE" />
          </view>
          <view class="item-card__row"><text class="item-card__label">客户</text><text class="item-card__value">{{ item.CUSTNAME || '—' }}</text></view>
          <view class="item-card__row"><text class="item-card__label">联系人</text><text class="item-card__value">{{ item.LINKER || '—' }}</text></view>
          <view class="item-card__row"><text class="item-card__label">委托日期</text><text class="item-card__value">{{ formatDate(item.BILLDATE) }}</text></view>
        </view>

        <empty-state v-if="!loading && !list.length" text="暂无委托单" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { queryDelegate } from '@/api/query'
import { formatDate } from '@/utils/format'

const keyword = ref('')
const activeState = ref('')
const list = ref([])
const loading = ref(false)
const refreshing = ref(false)
const pageIndex = ref(1)
const pageSize = 20
const hasMore = ref(true)

const stateFilters = [
  { label: '全部', value: '' },
  { label: '待提交', value: '1' },
  { label: '待审核', value: '2' },
  { label: '待审批', value: '5' },
  { label: '已审批', value: '6' },
  { label: '已签发', value: '10' }
]

onLoad((options) => {
  if (options.keyword) keyword.value = decodeURIComponent(options.keyword)
})

onShow(() => loadList(true))

function onSearch() {
  loadList(true)
}
function onFilter(val) {
  activeState.value = val
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
    const filter = {}
    if (keyword.value) filter.KEYWORD = keyword.value
    if (activeState.value) filter.STATE = activeState.value
    const res = await queryDelegate(filter, { pageIndex: pageIndex.value, pageSize })
    const newList = res.list || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {} finally {
    loading.value = false
  }
}

function onRefresh() {
  refreshing.value = true
  loadList(true).finally(() => (refreshing.value = false))
}
function onLoadMore() {
  if (hasMore.value && !loading.value) loadList(false)
}
function goDetail(item) {
  uni.navigateTo({ url: `/pages/approve/detail?id=${item.ID}&type=view&module=LI_M06` })
}
</script>

<style lang="scss" scoped>
.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
}

.filter-bar {
  white-space: nowrap;
  background-color: #fff;
  padding: 16rpx 24rpx;
}

.filter-item {
  display: inline-block;
  padding: 10rpx 28rpx;
  margin-right: 16rpx;
  border-radius: 32rpx;
  background-color: #f5f7fa;
  font-size: 26rpx;
  color: #666;

  &--active {
    background-color: #e8f1fe;
    color: #2f7df6;
  }
}

.list-scroll {
  flex: 1;
  overflow: hidden;
}

.list {
  padding: 20rpx 24rpx;
}

.item-card {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0, 0, 0, 0.05);

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16rpx;
  }

  &__no {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
  }

  &__row {
    display: flex;
    font-size: 26rpx;
    line-height: 1.8;
  }

  &__label {
    width: 80rpx;
    color: #909399;
  }

  &__value {
    flex: 1;
    color: #333;
  }
}

.loading-tip {
  text-align: center;
  padding: 24rpx;
  font-size: 24rpx;
  color: #c0c4cc;
}
</style>
