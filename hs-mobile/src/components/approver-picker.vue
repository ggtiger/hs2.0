<template>
  <view v-if="show" class="picker-mask" @click="close">
    <view class="picker" @click.stop>
      <view class="picker__header">
        <text class="picker__title">选择下一审批人</text>
        <text class="picker__close" @click="close">✕</text>
      </view>
      <view class="picker__search">
        <input
          v-model="keyword"
          class="picker__input"
          placeholder="输入姓名搜索"
          confirm-type="search"
          @confirm="search"
        />
      </view>
      <scroll-view scroll-y class="picker__list">
        <view
          v-for="emp in list"
          :key="emp.ID"
          class="picker__item"
          @click="onPick(emp)"
        >
          <view class="picker__avatar">{{ (emp.EMPNAME || '?').charAt(0) }}</view>
          <view class="picker__main">
            <text class="picker__name">{{ emp.EMPNAME }}</text>
            <text class="picker__dept">{{ emp.DEPTNAME || '' }}</text>
          </view>
          <text class="picker__arrow">›</text>
        </view>
        <empty-state v-if="!loading && searched && !list.length" icon="👤" text="暂无匹配人员" />
        <view v-if="loading" class="picker__tip">搜索中...</view>
      </scroll-view>
    </view>
  </view>
</template>

<script setup>
import { ref, watch } from 'vue'
import { query } from '@/api/db'

const props = defineProps({
  show: { type: Boolean, default: false }
})
const emit = defineEmits(['close', 'pick'])

const keyword = ref('')
const list = ref([])
const loading = ref(false)
const searched = ref(false)

// 弹窗打开时加载初始列表
watch(
  () => props.show,
  (val) => {
    if (val) {
      keyword.value = ''
      list.value = []
      searched.value = false
      search()
    }
  }
)

async function search() {
  loading.value = true
  searched.value = true
  try {
    // 查询员工（LIB_M06），按姓名模糊匹配
    const res = await query(
      'LIB_M06',
      keyword.value ? { EMPNAME: keyword.value } : {},
      { pageIndex: 1, pageSize: 30 },
      { silent: true }
    )
    list.value = res.list || []
  } catch (e) {
    list.value = []
  } finally {
    loading.value = false
  }
}

function onPick(emp) {
  emit('pick', { id: emp.ID, name: emp.EMPNAME })
  emit('close')
}

function close() {
  emit('close')
}
</script>

<style lang="scss" scoped>
.picker-mask {
  position: fixed;
  left: 0;
  right: 0;
  top: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.45);
  z-index: 999;
  display: flex;
  align-items: flex-end;
}

.picker {
  width: 100%;
  max-height: 80vh;
  background-color: #fff;
  border-radius: 24rpx 24rpx 0 0;
  display: flex;
  flex-direction: column;
  padding-bottom: env(safe-area-inset-bottom);

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 28rpx 32rpx;
    border-bottom: 1rpx solid #f0f0f0;
  }

  &__title {
    font-size: 32rpx;
    font-weight: 600;
    color: #1a1a1a;
  }

  &__close {
    font-size: 32rpx;
    color: #909399;
    padding: 0 8rpx;
  }

  &__search {
    padding: 20rpx 32rpx;
  }

  &__input {
    height: 72rpx;
    padding: 0 24rpx;
    background-color: #f5f7fa;
    border-radius: 36rpx;
    font-size: 28rpx;
  }

  &__list {
    max-height: 60vh;
  }

  &__item {
    display: flex;
    align-items: center;
    padding: 24rpx 32rpx;
    border-bottom: 1rpx solid #f5f5f5;
  }

  &__avatar {
    width: 72rpx;
    height: 72rpx;
    border-radius: 50%;
    background-color: #e8f1fe;
    color: #2f7df6;
    font-size: 30rpx;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 20rpx;
  }

  &__main {
    flex: 1;
  }

  &__name {
    display: block;
    font-size: 30rpx;
    color: #1a1a1a;
  }

  &__dept {
    display: block;
    margin-top: 4rpx;
    font-size: 24rpx;
    color: #909399;
  }

  &__arrow {
    font-size: 36rpx;
    color: #c0c4cc;
  }

  &__tip {
    text-align: center;
    padding: 32rpx;
    font-size: 26rpx;
    color: #909399;
  }
}
</style>
