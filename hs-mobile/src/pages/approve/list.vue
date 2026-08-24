<template>
  <view class="page">
    <search-bar v-model="keyword" placeholder="搜索单据号 / 设备" @search="loadList(true)" />

    <scroll-view
      scroll-y
      class="list-scroll"
      :refresher-enabled="true"
      :refresher-triggered="refreshing"
      @refresherrefresh="onRefresh"
      @scrolltolower="loadList(false)"
    >
      <view class="list">
        <todo-card v-for="item in list" :key="item.ID" :item="item" @click="goDetail(item)" />
        <empty-state v-if="!loading && !list.length" text="暂无审批记录" />
        <view v-if="loading" class="loading-tip">加载中...</view>
        <view v-else-if="!hasMore && list.length" class="loading-tip">没有更多了</view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { queryRecord } from '@/api/query'

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
    // 查询我处理过的记录（已审核以上，且处理人/审批人为当前用户）
    // TODO: 后端按 CHECKID/VERIFYID 过滤，前端标记 _history 即可
    const filter = { _history: true }
    if (keyword.value) filter.KEYWORD = keyword.value
    const res = await queryRecord(filter, { pageIndex: pageIndex.value, pageSize })
    const newList = res.list || []
    list.value = reset ? newList : [...list.value, ...newList]
    hasMore.value = newList.length >= pageSize
    if (hasMore.value) pageIndex.value++
  } catch (e) {} finally { loading.value = false }
}
function onRefresh() { refreshing.value = true; loadList(true).finally(() => (refreshing.value = false)) }
function goDetail(item) {
  uni.navigateTo({ url: `/pages/approve/detail?id=${item.ID}&type=view` })
}
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }
.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 20rpx 24rpx; }
.loading-tip { text-align: center; padding: 24rpx; font-size: 24rpx; color: #c0c4cc; }
</style>
