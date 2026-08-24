<template>
  <view class="page">
    <search-bar v-model="keyword" placeholder="搜索委托单号 / 客户" @search="loadList(true)" />

    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="loadList(false)"
    >
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="fee-card" @click="goDetail(item)">
          <view class="fee-card__header">
            <text class="fee-card__no">{{ item.BILLCODE || item.ID }}</text>
            <text class="fee-card__state" :class="{ 'fee-card__state--paid': !!item.CHARGETIME }">
              {{ item.CHARGETIME ? '已收费' : '未收费' }}
            </text>
          </view>
          <view class="fee-card__row"><text class="fee-card__label">客户</text><text class="fee-card__value">{{ item.CUSTNAME || '—' }}</text></view>
          <view class="fee-card__row"><text class="fee-card__label">设备</text><text class="fee-card__value">{{ item.MNAME || '—' }}</text></view>
          <view class="fee-card__row"><text class="fee-card__label">数量</text><text class="fee-card__value">{{ item.CNT || 0 }} 件</text></view>
          <view class="fee-card__amount">
            <text class="fee-card__amount-label">应收金额</text>
            <text class="fee-card__amount-value">¥ {{ formatMoney(item.CAMT) }}</text>
          </view>
        </view>

        <empty-state v-if="!loading && !list.length" text="暂无费用记录" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { queryFee } from '@/api/query'
import { formatMoney } from '@/utils/format'

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
    const res = await queryFee(filter, { pageIndex: pageIndex.value, pageSize })
    const newList = res.list || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {} finally { loading.value = false }
}
function onRefresh() { refreshing.value = true; loadList(true).finally(() => (refreshing.value = false)) }
function goDetail(item) {
  uni.navigateTo({ url: `/pages/approve/detail?id=${item.ID}&type=view&module=LI_M03` })
}
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }
.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 20rpx 24rpx; }
.fee-card {
  background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16rpx; }
  &__no { font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__state { font-size: 24rpx; color: #f5222d; padding: 2rpx 16rpx; border-radius: 8rpx; background-color: rgba(245,34,45,0.1);
    &--paid { color: #07c160; background-color: rgba(7,193,96,0.1); } }
  &__row { display: flex; font-size: 26rpx; line-height: 1.8; }
  &__label { width: 140rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
  &__amount { display: flex; align-items: center; justify-content: space-between; margin-top: 16rpx; padding-top: 16rpx; border-top: 1rpx solid #f5f5f5; }
  &__amount-label { font-size: 26rpx; color: #909399; }
  &__amount-value { font-size: 36rpx; font-weight: 700; color: #f5222d; }
}
.loading-tip { text-align: center; padding: 24rpx; font-size: 24rpx; color: #c0c4cc; }
</style>
