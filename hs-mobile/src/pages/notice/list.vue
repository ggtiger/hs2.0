<template>
  <view class="page">
    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="loadList(false)"
    >
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="notice-card" @click="goDetail(item.ID)">
          <view class="notice-card__title">{{ item.NOTITLE || '—' }}</view>
          <view class="notice-card__footer">
            <text class="notice-card__date">{{ formatDate(item.BILLDATE) }}</text>
            <text class="notice-card__arrow">查看详情 ›</text>
          </view>
        </view>
        <empty-state v-if="!loading && !list.length" icon="📢" text="暂无公告" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getNotices } from '@/api/home'
import { formatDate } from '@/utils/format'

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
    const res = await getNotices({ pageIndex: pageIndex.value, pageSize })
    const newList = res?.list || res || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {} finally { loading.value = false }
}
function onRefresh() { refreshing.value = true; loadList(true).finally(() => (refreshing.value = false)) }
function goDetail(id) { uni.navigateTo({ url: `/pages/notice/detail?id=${id}` }) }
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }
.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 24rpx; }
.notice-card {
  background-color: #fff; border-radius: 16rpx; padding: 28rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__title { font-size: 30rpx; font-weight: 600; color: #1a1a1a; line-height: 1.5; }
  &__footer { display: flex; align-items: center; justify-content: space-between; margin-top: 20rpx; padding-top: 20rpx; border-top: 1rpx solid #f5f5f5; }
  &__date { font-size: 24rpx; color: #c0c4cc; }
  &__arrow { font-size: 24rpx; color: #2f7df6; }
}
.loading-tip { text-align: center; padding: 24rpx; font-size: 24rpx; color: #c0c4cc; }
</style>
