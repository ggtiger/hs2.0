<template>
  <view class="page">
    <search-bar v-model="keyword" placeholder="搜索单据号 / 设备名称" @search="onSearch" />

    <!-- 查询类型入口 -->
    <view class="section">
      <view class="section__title">业务查询</view>
      <view class="grid">
        <view
          v-for="item in menus"
          :key="item.path"
          class="grid-item"
          @click="goPage(item.path)"
        >
          <view class="grid-item__icon" :style="{ background: item.bg }">{{ item.icon }}</view>
          <text class="grid-item__name">{{ item.name }}</text>
        </view>
      </view>
    </view>

    <!-- 搜索结果（跨模块快捷搜索） -->
    <view v-if="searched" class="section">
      <view class="section__title">搜索结果</view>
      <view class="search-tips">
        请选择上方查询类型，按「{{ keyword }}」精确检索：
      </view>
      <view class="search-jump">
        <view v-for="item in menus" :key="item.path" class="search-jump__item" @click="goPage(item.path + '?keyword=' + encodeURIComponent(keyword))">
          <text>在「{{ item.name }}」中搜索</text>
          <text class="search-jump__arrow">›</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'

const keyword = ref('')
const searched = ref(false)

const menus = [
  { name: '委托单', icon: '📋', bg: '#e8f1fe', path: '/pages/query/delegate' },
  { name: '受理单', icon: '📥', bg: '#e6f7ec', path: '/pages/query/accept' },
  { name: '原始记录', icon: '📄', bg: '#fff3e0', path: '/pages/query/record' },
  { name: '证书', icon: '📜', bg: '#fde8ef', path: '/pages/query/cert' },
  { name: '费用', icon: '💰', bg: '#f0e8fd', path: '/pages/query/fee' },
  { name: '物流', icon: '📦', bg: '#e0f2f1', path: '/pages/query/logistics' }
]

function onSearch(val) {
  if (!val.trim()) {
    searched.value = false
    return
  }
  searched.value = true
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

.section {
  margin: 24rpx;
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;

  &__title {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
    margin-bottom: 24rpx;
  }
}

.grid {
  display: flex;
  flex-wrap: wrap;
}

.grid-item {
  width: 33.33%;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 20rpx 0;

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
    font-size: 26rpx;
    color: #333;
  }
}

.search-tips {
  font-size: 26rpx;
  color: #909399;
  line-height: 1.8;
}

.search-jump {
  margin-top: 16rpx;

  &__item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 24rpx 0;
    font-size: 28rpx;
    color: #2f7df6;
    border-bottom: 1rpx solid #f5f5f5;
  }

  &__arrow {
    color: #c0c4cc;
    font-size: 32rpx;
  }
}
</style>
