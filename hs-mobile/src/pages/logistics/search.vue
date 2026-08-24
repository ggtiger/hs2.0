<template>
  <view class="page">
    <!-- 搜索栏 -->
    <view class="search-bar">
      <uv-search
        v-model="keyword"
        placeholder="输入受理单号搜索"
        :show-action="true"
        action-text="搜索"
        @search="doSearch"
        @custom="doSearch"
        @clear="onClear"
      ></uv-search>
    </view>

    <!-- 搜索结果 -->
    <scroll-view scroll-y class="list">
      <view v-if="loading" class="empty">
        <uv-loading-icon></uv-loading-icon>
        <text class="empty__text">搜索中...</text>
      </view>
      <view v-else-if="!searched" class="empty">
        <text class="empty__text">请输入受理单号搜索</text>
      </view>
      <view v-else-if="!list.length" class="empty">
        <text class="empty__text">未找到受理单</text>
      </view>
      <template v-else>
        <view v-for="item in list" :key="item.ID" class="card" @click="onSelect(item)">
          <view class="card__head">
            <text class="card__code">{{ item.BILLCODE }}</text>
            <text class="card__state" :class="stateClass(item.STATE)">{{ item.STATE }}</text>
          </view>
          <view class="card__row">
            <text class="card__label">委托单位</text>
            <text class="card__value">{{ item.CUSTNAME || '—' }}</text>
          </view>
          <view class="card__row">
            <text class="card__label">设备名称</text>
            <text class="card__value">{{ item.MNAME || '—' }}</text>
          </view>
          <view class="card__row">
            <text class="card__label">规格型号</text>
            <text class="card__value">{{ item.SIZETYPE || '—' }}</text>
          </view>
          <view class="card__footer">
            <text class="card__date">{{ item.BILLDATE || '' }}</text>
            <uv-button type="primary" size="mini" text="新增物流" @click.stop="onSelect(item)"></uv-button>
          </view>
        </view>
      </template>
    </scroll-view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { searchAcceptBill } from '@/api/logistics'

const keyword = ref('')
const list = ref([])
const loading = ref(false)
const searched = ref(false)

onLoad((options) => {
  if (options.keyword) {
    keyword.value = options.keyword
    doSearch()
  }
})

async function doSearch() {
  const kw = keyword.value.trim()
  if (!kw) {
    return uni.showToast({ title: '请输入受理单号', icon: 'none' })
  }
  loading.value = true
  searched.value = true
  try {
    const res = await searchAcceptBill(kw)
    list.value = res.list
    // 只有一条结果时自动跳转，减少操作步骤
    if (res.list.length === 1) {
      onSelect(res.list[0])
      return
    }
  } catch (e) {
    list.value = []
  } finally {
    loading.value = false
  }
}

function onClear() {
  list.value = []
  searched.value = false
}

function onSelect(item) {
  uni.navigateTo({
    url: `/pages/logistics/add?refId=${item.ID}&refCode=${encodeURIComponent(item.BILLCODE || '')}`
  })
}

function stateClass(state) {
  if (!state) return ''
  if (state.includes('审批') || state.includes('完成')) return 'st-green'
  if (state.includes('驳回')) return 'st-red'
  if (state.includes('审核')) return 'st-blue'
  return 'st-gray'
}
</script>

<style lang="scss" scoped>
.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
}

.search-bar {
  padding: 16rpx 24rpx;
  background-color: #fff;
}

.list {
  flex: 1;
  padding: 16rpx 24rpx;
}

.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 120rpx;

  &__text {
    margin-top: 16rpx;
    font-size: 28rpx;
    color: #999;
  }
}

.card {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 16rpx;

  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16rpx;
  }

  &__code {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
  }

  &__state {
    font-size: 22rpx;
    padding: 4rpx 16rpx;
    border-radius: 20rpx;
    font-weight: 500;

    &.st-green { color: #52C41A; background-color: #F6FFED; }
    &.st-red { color: #F5222D; background-color: #FFF1F0; }
    &.st-blue { color: #2F54EB; background-color: #F0F5FF; }
    &.st-gray { color: #8C8C8C; background-color: #FAFAFA; }
  }

  &__row {
    display: flex;
    padding: 6rpx 0;
    font-size: 26rpx;
  }

  &__label {
    width: 140rpx;
    color: #999;
    flex-shrink: 0;
    text-align: right;
    margin-right: 12rpx;
  }

  &__value {
    flex: 1;
    color: #333;
  }

  &__footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 16rpx;
    padding-top: 16rpx;
    border-top: 1rpx solid #f0f0f0;
  }

  &__date {
    font-size: 24rpx;
    color: #bbb;
  }
}
</style>
