<template>
  <view class="page">
    <view class="form-wrap">
      <view class="logo">📊</view>
      <view class="title">检测进度查询</view>
      <view class="subtitle">输入委托单号或送检手机号查询进度</view>

      <view class="form">
        <input v-model="form.BILLNO" class="form__input" placeholder="委托单号" />
        <input v-model="form.PHONE" class="form__input" placeholder="送检人手机号（选填）" type="number" />
        <button class="form__btn" :loading="loading" :disabled="loading" @click="onSearch">查询进度</button>
      </view>
    </view>

    <!-- 结果列表 -->
    <view v-if="searched" class="result">
      <view v-if="list.length" class="result__list">
        <view v-for="item in list" :key="item.ID" class="progress-card">
          <view class="progress-card__header">
            <text class="progress-card__device">{{ item.EQUIPNAME || '—' }}</text>
            <text class="progress-card__state" :style="{ color: stateColor(item.STATE) }">{{ stateText(item.STATE) }}</text>
          </view>
          <view class="progress-card__row"><text class="progress-card__label">规格型号</text><text>{{ item.SPEC || '—' }}</text></view>
          <view class="progress-card__row"><text class="progress-card__label">出厂编号</text><text>{{ item.FACTORYNO || '—' }}</text></view>
          <view class="progress-card__actions">
            <view v-if="item.CERTNO || item.PDFURL" class="progress-card__btn" @click="viewCert(item)">查看证书</view>
            <view v-else class="progress-card__btn progress-card__btn--disabled">证书未生成</view>
          </view>
        </view>
      </view>
      <empty-state v-else icon="🔍" text="未查询到相关进度" action-text="重新查询" @action="searched = false" />
    </view>
  </view>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { queryProgress } from '@/api/outer'
import { getStateInfo } from '@/utils/state'

const form = reactive({ BILLNO: '', PHONE: '' })
const list = ref([])
const searched = ref(false)
const loading = ref(false)

function stateText(state) { return getStateInfo(state).label }
function stateColor(state) { return getStateInfo(state).color }

async function onSearch() {
  if (!form.BILLNO.trim() && !form.PHONE.trim()) {
    return uni.showToast({ title: '请至少输入一项', icon: 'none' })
  }
  loading.value = true
  uni.showLoading({ title: '查询中' })
  try {
    const res = await queryProgress({ BILLNO: form.BILLNO.trim(), PHONE: form.PHONE.trim() })
    list.value = res?.list || res?.rows || (Array.isArray(res) ? res : [])
    searched.value = true
  } catch (e) {} finally {
    loading.value = false
    uni.hideLoading()
  }
}

function viewCert(item) {
  // 跳转证书验证页
  if (item.CERTNO) {
    uni.navigateTo({ url: `/pages/out/verify?certNo=${encodeURIComponent(item.CERTNO)}` })
  }
}
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding: 0 48rpx; }
.form-wrap { display: flex; flex-direction: column; align-items: center; padding-top: 100rpx; }
.logo { width: 140rpx; height: 140rpx; border-radius: 36rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; font-size: 64rpx; display: flex; align-items: center; justify-content: center; }
.title { margin-top: 32rpx; font-size: 40rpx; font-weight: 700; color: #1a1a1a; }
.subtitle { margin-top: 12rpx; font-size: 26rpx; color: #909399; text-align: center; }
.form { width: 100%; margin-top: 48rpx; }
.form__input { width: 100%; height: 96rpx; padding: 0 32rpx; background-color: #fff; border-radius: 16rpx; font-size: 30rpx; margin-bottom: 20rpx; box-sizing: border-box; }
.form__btn { width: 100%; height: 96rpx; line-height: 96rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; border-radius: 48rpx; font-size: 32rpx; border: none;
  &::after { border: none; } }
.result { padding-top: 32rpx; }
.progress-card { background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  &__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16rpx; }
  &__device { font-size: 30rpx; font-weight: 600; color: #1a1a1a; flex: 1; }
  &__state { font-size: 26rpx; }
  &__row { display: flex; padding: 8rpx 0; font-size: 26rpx; }
  &__label { width: 140rpx; color: #909399; }
  &__actions { margin-top: 16rpx; padding-top: 16rpx; border-top: 1rpx solid #f5f5f5; }
  &__btn { text-align: center; height: 76rpx; line-height: 76rpx; border-radius: 38rpx; background-color: #e8f1fe; color: #2f7df6; font-size: 28rpx;
    &--disabled { background-color: #f5f5f5; color: #c0c4cc; } } }
</style>
