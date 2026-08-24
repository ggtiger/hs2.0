<template>
  <view class="page">
    <search-bar v-model="keyword" placeholder="搜索受理单号 / 客户" @search="loadList(true)" />

    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="loadList(false)"
    >
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="item-card" @click="goDetail(item)">
          <view class="item-card__header">
            <text class="item-card__no">{{ item.BILLCODE || item.ID }}</text>
            <state-tag :state="item.STATE" />
          </view>
          <view class="item-card__row"><text class="item-card__label">客户</text><text class="item-card__value">{{ item.CUSTNAME || '—' }}</text></view>
          <view class="item-card__row"><text class="item-card__label">设备</text><text class="item-card__value">{{ item.MNAME || '—' }}</text></view>
          <view class="item-card__row"><text class="item-card__label">样品数</text><text class="item-card__value">{{ item.CNT || 0 }} 件</text></view>
          <view class="item-card__row"><text class="item-card__label">受理日期</text><text class="item-card__value">{{ formatDate(item.BILLDATE) }}</text></view>
          <view class="item-card__actions" @click.stop>
            <view class="action-btn" @click="goAddLogistics(item)">📮 新增物流</view>
          </view>
        </view>

        <empty-state v-if="!loading && !list.length" text="暂无受理单" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { queryAccept } from '@/api/query'
import { formatDate } from '@/utils/format'

const keyword = ref('')
const list = ref([])
const loading = ref(false)
const refreshing = ref(false)
const pageIndex = ref(1)
const pageSize = 20
const hasMore = ref(true)

onShow(() => loadList(true))

async function loadList(reset = false) {
  if (loading.value) return
  if (reset) { pageIndex.value = 1; hasMore.value = true; list.value = [] }
  if (!hasMore.value) return
  loading.value = true
  try {
    const filter = keyword.value ? { KEYWORD: keyword.value } : {}
    const res = await queryAccept(filter, { pageIndex: pageIndex.value, pageSize })
    const newList = res.list || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {} finally { loading.value = false }
}
function onRefresh() { refreshing.value = true; loadList(true).finally(() => (refreshing.value = false)) }
function goDetail(item) {
  uni.navigateTo({ url: `/pages/approve/detail?id=${item.ID}&type=view&module=LI_M00` })
}
function goAddLogistics(item) {
  uni.navigateTo({ url: `/pages/logistics/add?refId=${item.ID}&refCode=${encodeURIComponent(item.BILLCODE || '')}` })
}
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }
.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 20rpx 24rpx; }
.item-card {
  background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16rpx; }
  &__no { font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__row { display: flex; font-size: 26rpx; line-height: 1.8; }
  &__label { width: 140rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
  &__actions { display: flex; justify-content: flex-end; margin-top: 16rpx; padding-top: 16rpx; border-top: 1rpx solid #f0f0f0; }
}
.action-btn {
  padding: 8rpx 24rpx;
  border-radius: 20rpx;
  font-size: 24rpx;
  color: #2F54EB;
  background-color: #F0F5FF;
}
.loading-tip { text-align: center; padding: 24rpx; font-size: 24rpx; color: #c0c4cc; }
</style>
