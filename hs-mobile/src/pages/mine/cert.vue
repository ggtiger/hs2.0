<template>
  <view class="page">
    <view class="list">
      <view v-for="item in list" :key="item.ID" class="cert-card">
        <view class="cert-card__header">
          <text class="cert-card__icon">🏅</text>
          <view class="cert-card__main">
            <text class="cert-card__name">{{ item.CERTNAME || '—' }}</text>
            <text class="cert-card__no">证书编号：{{ item.CERTNO || '—' }}</text>
          </view>
          <text class="cert-card__state" :class="stateClass(item)">
            {{ stateText(item) }}
          </text>
        </view>
        <view class="cert-card__row"><text class="cert-card__label">授权范围</text><text class="cert-card__value">{{ item.SCOPE || item.AUTHRANGE || '—' }}</text></view>
        <view class="cert-card__row"><text class="cert-card__label">发证日期</text><text class="cert-card__value">{{ formatDate(item.ISSUEDATE) }}</text></view>
        <view class="cert-card__row"><text class="cert-card__label">有效期至</text><text class="cert-card__value">{{ formatDate(item.VALIDDATE) }}</text></view>
      </view>
      <empty-state v-if="!loading && !list.length" icon="🏅" text="暂无资质证书" />
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { call } from '@/api/db'
import { useUserStore } from '@/store'
import { formatDate } from '@/utils/format'

const userStore = useUserStore()
const list = ref([])
const loading = ref(false)

onShow(() => loadList())

async function loadList() {
  loading.value = true
  try {
    // 查询当前人员资质证书（LI_M07）
    const res = await call('LI_M07', 'A01', {
      MODULECODE: 'LI_M07',
      APICODE: 'A01',
      INPUT: { page: 1, pagesize: 50, EMPID: userStore.empId }
    })
    list.value = res?.list || res?.rows || (Array.isArray(res) ? res : [])
  } catch (e) {
    list.value = []
  } finally {
    loading.value = false
  }
}

function stateText(item) {
  if (!item.VALIDDATE) return '—'
  // 简单判断是否过期（运行时有 Date）
  return new Date(item.VALIDDATE) > new Date() ? '有效' : '已过期'
}
function stateClass(item) {
  return stateText(item) === '有效' ? 'cert-card__state--valid' : 'cert-card__state--expired'
}
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding-bottom: 40rpx; }
.list { padding: 24rpx; }
.cert-card {
  background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__header { display: flex; align-items: flex-start; margin-bottom: 16rpx; }
  &__icon { font-size: 44rpx; margin-right: 16rpx; }
  &__main { flex: 1; }
  &__name { display: block; font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__no { display: block; margin-top: 4rpx; font-size: 24rpx; color: #909399; }
  &__state { font-size: 24rpx; padding: 4rpx 16rpx; border-radius: 8rpx; flex-shrink: 0;
    &--valid { color: #07c160; background-color: rgba(7,193,96,0.1); }
    &--expired { color: #f5222d; background-color: rgba(245,34,45,0.1); } }
  &__row { display: flex; padding: 8rpx 0; font-size: 26rpx; }
  &__label { width: 140rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
}
</style>
