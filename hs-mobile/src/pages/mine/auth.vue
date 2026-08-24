<template>
  <view class="page">
    <view class="list">
      <view v-for="item in list" :key="item.ID" class="auth-card">
        <view class="auth-card__header">
          <text class="auth-card__icon">🛡️</text>
          <text class="auth-card__name">{{ item.AUTHNAME || item.SCOPE || '—' }}</text>
          <text class="auth-card__tag">{{ item.AUTHTYPE || '授权' }}</text>
        </view>
        <view class="auth-card__row"><text class="auth-card__label">授权范围</text><text class="auth-card__value">{{ item.RANGEDESC || item.AUTHRANGE || '—' }}</text></view>
        <view class="auth-card__row"><text class="auth-card__label">授权日期</text><text class="auth-card__value">{{ formatDate(item.AUTHDATE || item.ISSUEDATE) }}</text></view>
        <view class="auth-card__row"><text class="auth-card__label">有效期至</text><text class="auth-card__value">{{ formatDate(item.VALIDDATE) || '长期' }}</text></view>
      </view>
      <empty-state v-if="!loading && !list.length" icon="🛡️" text="暂无授权记录" />
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
    // 查询当前人员授权（LI_M08）
    const res = await call('LI_M08', 'A01', {
      MODULECODE: 'LI_M08',
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
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding-bottom: 40rpx; }
.list { padding: 24rpx; }
.auth-card {
  background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__header { display: flex; align-items: center; margin-bottom: 16rpx; }
  &__icon { font-size: 36rpx; margin-right: 16rpx; }
  &__name { flex: 1; font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__tag { font-size: 22rpx; color: #2f7df6; padding: 4rpx 16rpx; border-radius: 8rpx; background-color: #e8f1fe; }
  &__row { display: flex; padding: 8rpx 0; font-size: 26rpx; }
  &__label { width: 140rpx; color: #909399; }
  &__value { flex: 1; color: #333; }
}
</style>
