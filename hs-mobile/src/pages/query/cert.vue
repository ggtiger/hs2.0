<template>
  <view class="page">
    <search-bar v-model="keyword" placeholder="搜索证书编号 / 设备 / 客户" @search="loadList(true)" />

    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="loadList(false)"
    >
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="cert-card" @click="previewCert(item)">
          <view class="cert-card__header">
            <text class="cert-card__icon">📜</text>
            <view class="cert-card__main">
              <text class="cert-card__no">{{ item.CERTCODE || item.REFBILLCODE || item.ID }}</text>
              <text class="cert-card__device">{{ item.MNAME || '—' }}</text>
            </view>
          </view>
          <view class="cert-card__footer">
            <text class="cert-card__cust">{{ item.CUSTNAME || '—' }}</text>
            <text class="cert-card__date">签发：{{ formatDate(item.ECERTSIGNDATE || item.VERIFYTIME) }}</text>
          </view>
        </view>

        <empty-state v-if="!loading && !list.length" text="暂无证书" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { queryCert } from '@/api/query'
import { formatDate } from '@/utils/format'
import { previewPdf, pickFileId } from '@/utils/pdf'

const keyword = ref('')
const list = ref([])
const loading = ref(false)
const refreshing = ref(false)
const pageIndex = ref(1)
const pageSize = 20
const hasMore = ref(true)

onLoad((options) => {
  if (options.keyword) keyword.value = decodeURIComponent(options.keyword)
})
onShow(() => loadList(true))

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
    const filter = keyword.value ? { KEYWORD: keyword.value } : {}
    const res = await queryCert(filter, { pageIndex: pageIndex.value, pageSize })
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
function previewCert(item) {
  const fileId = pickFileId(item)
  if (!fileId) {
    uni.showToast({ title: '证书文件未生成', icon: 'none' })
    return
  }
  previewPdf(fileId)
}
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }
.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 20rpx 24rpx; }
.cert-card {
  background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__header { display: flex; align-items: center; margin-bottom: 16rpx; }
  &__icon { font-size: 44rpx; margin-right: 16rpx; }
  &__main { flex: 1; }
  &__no { display: block; font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__device { font-size: 26rpx; color: #666; margin-top: 4rpx; }
  &__footer { display: flex; justify-content: space-between; padding-top: 16rpx; border-top: 1rpx solid #f5f5f5; }
  &__cust { font-size: 24rpx; color: #909399; }
  &__date { font-size: 24rpx; color: #c0c4cc; }
}
.loading-tip { text-align: center; padding: 24rpx; font-size: 24rpx; color: #c0c4cc; }
</style>
