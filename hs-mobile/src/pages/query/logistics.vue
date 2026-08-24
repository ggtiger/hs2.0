<template>
  <view class="page">
    <view class="search">
      <input v-model="logisticsNo" class="search__input" placeholder="请输入物流单号" confirm-type="search" @confirm="onSearch" />
      <view class="search__btn" @click="onSearch">查询</view>
    </view>

    <!-- 物流信息 -->
    <view v-if="info" class="card">
      <view class="card__row"><text class="card__label">快递公司</text><text class="card__value">{{ info.EXPCOMPANY || '—' }}</text></view>
      <view class="card__row"><text class="card__label">物流单号</text><text class="card__value">{{ info.LOGISTICSNO || '—' }}</text></view>
      <view class="card__row"><text class="card__label">当前状态</text>
        <text class="card__state" :style="{ color: stateColor }">{{ stateText }}</text>
      </view>
    </view>

    <!-- 轨迹时间线 -->
    <view v-if="tracks.length" class="timeline">
      <view v-for="(track, idx) in tracks" :key="idx" class="track">
        <view class="track__line-wrap">
          <view class="track__dot" :class="{ 'track__dot--active': idx === 0 }"></view>
          <view v-if="idx < tracks.length - 1" class="track__line"></view>
        </view>
        <view class="track__content">
          <text class="track__desc">{{ track.DESCRIPTION || track.NODEDESC || '—' }}</text>
          <text class="track__time">{{ formatDateTime(track.NODETIME || track.TIME) }}</text>
          <image v-if="track.PHOTO" class="track__photo" :src="track.PHOTO" mode="aspectFill" @click="previewPhoto(track.PHOTO)" />
        </view>
      </view>
    </view>

    <empty-state v-if="searched && !tracks.length" icon="📦" text="未查询到物流轨迹" />
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { queryLogistics as apiQueryLogistics } from '@/api/query'
import { formatDateTime } from '@/utils/format'

const logisticsNo = ref('')
const info = ref(null)
const tracks = ref([])
const searched = ref(false)

const stateMap = {
  0: { text: '待寄送', color: '#909399' },
  1: { text: '已寄送', color: '#2f7df6' },
  2: { text: '运输中', color: '#ff9900' },
  3: { text: '已签收', color: '#07c160' }
}
const stateText = computed(() => stateMap[info.value?.STATE]?.text || '—')
const stateColor = computed(() => stateMap[info.value?.STATE]?.color || '#909399')

onLoad((options) => {
  if (options.keyword) {
    logisticsNo.value = decodeURIComponent(options.keyword)
    onSearch()
  }
})

async function onSearch() {
  if (!logisticsNo.value.trim()) {
    return uni.showToast({ title: '请输入物流单号', icon: 'none' })
  }
  searched.value = true
  uni.showLoading({ title: '查询中' })
  try {
    const res = await apiQueryLogistics({ LOGISTICSNO: logisticsNo.value.trim() })
    info.value = res?.main || res?.info || (Array.isArray(res) ? null : res) || null
    tracks.value = res?.nodes || res?.tracks || res?.list || (Array.isArray(res) ? res : [])
  } catch (e) {} finally {
    uni.hideLoading()
  }
}

function previewPhoto(url) {
  uni.previewImage({ urls: [url] })
}
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding-bottom: 40rpx; }

.search {
  display: flex; gap: 16rpx; padding: 24rpx; background-color: #fff;
  &__input { flex: 1; height: 76rpx; padding: 0 24rpx; background-color: #f5f7fa; border-radius: 12rpx; font-size: 28rpx; }
  &__btn { padding: 0 40rpx; height: 76rpx; line-height: 76rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; border-radius: 12rpx; font-size: 28rpx; }
}

.card {
  margin: 24rpx; background-color: #fff; border-radius: 16rpx; padding: 24rpx;
  &__row { display: flex; padding: 12rpx 0; font-size: 28rpx; }
  &__label { width: 160rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
  &__state { font-weight: 600; }
}

.timeline { margin: 0 24rpx; background-color: #fff; border-radius: 16rpx; padding: 32rpx 24rpx; }
.track {
  display: flex;
  &__line-wrap { display: flex; flex-direction: column; align-items: center; margin-right: 24rpx; }
  &__dot { width: 20rpx; height: 20rpx; border-radius: 50%; background-color: #dcdfe6; margin-top: 8rpx;
    &--active { background-color: #07c160; box-shadow: 0 0 0 8rpx rgba(7,193,96,0.15); } }
  &__line { flex: 1; width: 2rpx; background-color: #ebedf0; margin: 8rpx 0; }
  &__content { flex: 1; padding-bottom: 36rpx; }
  &__desc { display: block; font-size: 28rpx; color: #333; }
  &__time { display: block; margin-top: 8rpx; font-size: 24rpx; color: #c0c4cc; }
  &__photo { width: 120rpx; height: 120rpx; border-radius: 8rpx; margin-top: 16rpx; }
}
</style>
