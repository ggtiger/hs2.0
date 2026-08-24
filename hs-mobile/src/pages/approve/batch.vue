<template>
  <view class="page">
    <!-- 类型切换 -->
    <view class="type-bar">
      <view class="type-bar__item" :class="{ active: type === 'verify' }" @click="switchType('verify')">待审批</view>
      <view class="type-bar__item" :class="{ active: type === 'check' }" @click="switchType('check')">待复核</view>
    </view>

    <!-- 全选栏 -->
    <view v-if="list.length" class="select-bar">
      <view class="select-bar__check" :class="{ 'select-bar__check--on': allChecked }" @click="toggleAll">
        <text>{{ allChecked ? '☑' : '☐' }}</text>
      </view>
      <text class="select-bar__text">已选 {{ selectedIds.length }}/{{ list.length }}</text>
    </view>

    <scroll-view scroll-y class="list-scroll">
      <view class="list">
        <view v-for="item in list" :key="item.ID" class="batch-card" @click="toggle(item.ID)">
          <view class="batch-card__check" :class="{ 'batch-card__check--on': selectedIds.includes(item.ID) }">
            <text>{{ selectedIds.includes(item.ID) ? '☑' : '☐' }}</text>
          </view>
          <view class="batch-card__body">
            <view class="batch-card__header">
              <text class="batch-card__no">{{ item.BILLNO || item.ID }}</text>
              <state-tag :state="item.STATE" />
            </view>
            <text class="batch-card__device">{{ item.EQUIPNAME || '—' }}</text>
          </view>
        </view>
        <empty-state v-if="!loading && !list.length" text="暂无可批量操作的记录" />
      </view>
    </scroll-view>

    <!-- 批量意见 + 操作 -->
    <view v-if="list.length" class="footer safe-bottom">
      <textarea v-model="remark" class="footer__opinion" placeholder="批量意见（选填，驳回时建议填写）" maxlength="200" />
      <view class="footer__btns">
        <view class="footer__btn footer__btn--reject" @click="onBatchReject">批量驳回</view>
        <view class="footer__btn footer__btn--approve" @click="onBatchApprove">批量通过</view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { listToCheck, listToVerify, batchVerify, batchReject } from '@/api/approve'

const type = ref('verify')
const list = ref([])
const loading = ref(false)
const selectedIds = ref([])
const remark = ref('')

const allChecked = computed(() => list.value.length > 0 && selectedIds.value.length === list.value.length)

onShow(() => loadList())

function switchType(t) {
  type.value = t
  loadList()
}

async function loadList() {
  loading.value = true
  selectedIds.value = []
  try {
    const fetcher = type.value === 'check' ? listToCheck : listToVerify
    const res = await fetcher({ pageIndex: 1, pageSize: 50 })
    list.value = res.list || []
  } catch (e) {} finally {
    loading.value = false
  }
}

function toggle(id) {
  const idx = selectedIds.value.indexOf(id)
  if (idx >= 0) selectedIds.value.splice(idx, 1)
  else selectedIds.value.push(id)
}

function toggleAll() {
  selectedIds.value = allChecked.value ? [] : list.value.map((i) => i.ID)
}

function onBatchApprove() {
  doBatch(true)
}
function onBatchReject() {
  doBatch(false)
}

function doBatch(approve) {
  if (!selectedIds.value.length) {
    return uni.showToast({ title: '请至少选择一条记录', icon: 'none' })
  }
  const action = approve ? batchVerify : batchReject
  uni.showModal({
    title: '确认操作',
    content: `确定对选中的 ${selectedIds.value.length} 条记录「批量${approve ? '通过' : '驳回'}」？`,
    success: async (res) => {
      if (!res.confirm) return
      try {
        await action({ IDS: selectedIds.value, REMARK: remark.value })
        loadList()
      } catch (e) {}
    }
  })
}
</script>

<style lang="scss" scoped>
.page { display: flex; flex-direction: column; height: 100vh; background-color: #f5f7fa; }

.type-bar { display: flex; background-color: #fff; }
.type-bar__item { flex: 1; text-align: center; height: 84rpx; line-height: 84rpx; font-size: 28rpx; color: #666;
  &.active { color: #2f7df6; font-weight: 600; border-bottom: 4rpx solid #2f7df6; } }

.select-bar { display: flex; align-items: center; padding: 20rpx 24rpx; background-color: #fff; border-top: 1rpx solid #f5f5f5; }
.select-bar__check { font-size: 36rpx; color: #c0c4cc; margin-right: 16rpx;
  &--on { color: #2f7df6; } }
.select-bar__text { font-size: 26rpx; color: #666; }

.list-scroll { flex: 1; overflow: hidden; }
.list { padding: 20rpx 24rpx; }

.batch-card { display: flex; align-items: flex-start; background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-bottom: 20rpx; box-shadow: 0 2rpx 12rpx 0 rgba(0,0,0,0.05);
  &__check { font-size: 36rpx; color: #c0c4cc; margin-right: 20rpx; margin-top: 4rpx;
    &--on { color: #2f7df6; } }
  &__body { flex: 1; }
  &__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8rpx; }
  &__no { font-size: 30rpx; font-weight: 600; color: #1a1a1a; }
  &__device { font-size: 26rpx; color: #666; } }

.footer { background-color: #fff; padding: 20rpx 24rpx; box-shadow: 0 -2rpx 12rpx 0 rgba(0,0,0,0.06); }
.footer__opinion { width: 100%; height: 120rpx; padding: 16rpx; background-color: #f5f7fa; border-radius: 12rpx; font-size: 28rpx; box-sizing: border-box; margin-bottom: 16rpx; }
.footer__btns { display: flex; gap: 24rpx; }
.footer__btn { flex: 1; height: 88rpx; line-height: 88rpx; text-align: center; border-radius: 44rpx; font-size: 30rpx;
  &--reject { background-color: #fff; color: #f5222d; border: 1rpx solid #f5222d; }
  &--approve { background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; } }
</style>
