<template>
  <view class="page">
    <view class="form-wrap">
      <view class="logo">📦</view>
      <view class="title">物流查询</view>
      <view class="subtitle">输入物流单号查询寄送轨迹</view>

      <view class="form">
        <input v-model="logisticsNo" class="form__input" placeholder="请输入物流单号" confirm-type="search" @confirm="onSearch" />
        <view class="form__scan" @click="onScan">扫码查询</view>
        <button class="form__btn" :loading="loading" :disabled="loading" @click="onSearch">查询物流</button>
      </view>
    </view>

    <!-- 物流信息 -->
    <view v-if="info" class="card">
      <view class="card__row"><text class="card__label">快递公司</text><text class="card__value">{{ info.EXPCOMPANY || '—' }}</text></view>
      <view class="card__row"><text class="card__label">物流单号</text><text class="card__value">{{ info.LOGISTICSNO || '—' }}</text></view>
      <view class="card__row"><text class="card__label">当前状态</text><text class="card__state" :style="{ color: stateColor }">{{ stateText }}</text></view>
    </view>

    <!-- 轨迹 -->
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

    <empty-state v-if="searched && !tracks.length && !info" icon="📦" text="未查询到物流信息" action-text="重新查询" @action="reset" />
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { queryLogisticsTrack } from '@/api/outer'
import { scanCode } from '@/utils/scan'
import { formatDateTime } from '@/utils/format'

const logisticsNo = ref('')
const info = ref(null)
const tracks = ref([])
const searched = ref(false)
const loading = ref(false)

const stateMap = { 0: { t: '待寄送', c: '#909399' }, 1: { t: '已寄送', c: '#2f7df6' }, 2: { t: '运输中', c: '#ff9900' }, 3: { t: '已签收', c: '#07c160' } }
const stateText = computed(() => stateMap[info.value?.STATE]?.t || '—')
const stateColor = computed(() => stateMap[info.value?.STATE]?.c || '#909399')

async function onSearch() {
  if (!logisticsNo.value.trim()) return uni.showToast({ title: '请输入物流单号', icon: 'none' })
  loading.value = true
  uni.showLoading({ title: '查询中' })
  try {
    const res = await queryLogisticsTrack(logisticsNo.value.trim())
    info.value = res?.main || res?.info || (Array.isArray(res) ? null : res) || null
    tracks.value = res?.nodes || res?.tracks || res?.list || (Array.isArray(res) ? res : [])
    searched.value = true
  } catch (e) {} finally {
    loading.value = false
    uni.hideLoading()
  }
}

async function onScan() {
  try {
    const res = await scanCode()
    if (res.result) { logisticsNo.value = res.result; onSearch() }
  } catch (e) {}
}

function previewPhoto(url) { uni.previewImage({ urls: [url] }) }
function reset() { logisticsNo.value = ''; info.value = null; tracks.value = []; searched.value = false }
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding: 0 48rpx 40rpx; }
.form-wrap { display: flex; flex-direction: column; align-items: center; padding-top: 80rpx; }
.logo { width: 140rpx; height: 140rpx; border-radius: 36rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; font-size: 64rpx; display: flex; align-items: center; justify-content: center; }
.title { margin-top: 32rpx; font-size: 40rpx; font-weight: 700; color: #1a1a1a; }
.subtitle { margin-top: 12rpx; font-size: 26rpx; color: #909399; text-align: center; }
.form { width: 100%; margin-top: 48rpx; }
.form__input { width: 100%; height: 96rpx; padding: 0 32rpx; background-color: #fff; border-radius: 16rpx; font-size: 30rpx; box-sizing: border-box; }
.form__scan { text-align: center; margin: 20rpx 0; font-size: 28rpx; color: #2f7df6; }
.form__btn { width: 100%; height: 96rpx; line-height: 96rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; border-radius: 48rpx; font-size: 32rpx; border: none;
  &::after { border: none; } }
.card { background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-top: 24rpx;
  &__row { display: flex; padding: 12rpx 0; font-size: 28rpx; }
  &__label { width: 160rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
  &__state { font-weight: 600; } }
.timeline { margin-top: 24rpx; background-color: #fff; border-radius: 16rpx; padding: 32rpx 24rpx; }
.track { display: flex;
  &__line-wrap { display: flex; flex-direction: column; align-items: center; margin-right: 24rpx; }
  &__dot { width: 20rpx; height: 20rpx; border-radius: 50%; background-color: #dcdfe6; margin-top: 8rpx;
    &--active { background-color: #07c160; box-shadow: 0 0 0 8rpx rgba(7,193,96,0.15); } }
  &__line { flex: 1; width: 2rpx; background-color: #ebedf0; margin: 8rpx 0; }
  &__content { flex: 1; padding-bottom: 36rpx; }
  &__desc { display: block; font-size: 28rpx; color: #333; }
  &__time { display: block; margin-top: 8rpx; font-size: 24rpx; color: #c0c4cc; }
  &__photo { width: 120rpx; height: 120rpx; border-radius: 8rpx; margin-top: 16rpx; } }
</style>
