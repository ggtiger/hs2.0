<template>
  <view class="todo-card" @click="$emit('click', item)">
    <view class="todo-card__header">
      <text class="todo-card__no">{{ item.REFBILLCODE || item.WTCODE || item.BILLNO || item.ID }}</text>
      <state-tag :state="item.STATE" />
    </view>
    <view class="todo-card__body">
      <view class="todo-card__row">
        <text class="todo-card__label">设备名称</text>
        <text class="todo-card__value">{{ item.MNAME || item.EQUIPNAME || item.DEVICENAME || '—' }}</text>
      </view>
      <view class="todo-card__row">
        <text class="todo-card__label">委托单位</text>
        <text class="todo-card__value">{{ item.CUSTNAME || '—' }}</text>
      </view>
    </view>
    <view class="todo-card__footer">
      <text class="todo-card__time">{{ formatRelative(item.SUMBMITTIME || item.SUBMITTIME || item.CREATETIME || item.CREATEDATE) }}</text>
      <text class="todo-card__arrow">›</text>
    </view>
  </view>
</template>

<script setup>
import { formatRelative } from '@/utils/format'

defineProps({
  item: {
    type: Object,
    default: () => ({})
  }
})
defineEmits(['click'])
</script>

<style lang="scss" scoped>
.todo-card {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 20rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0, 0, 0, 0.05);

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16rpx;
  }

  &__no {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
  }

  &__row {
    display: flex;
    font-size: 26rpx;
    line-height: 1.8;
  }

  &__label {
    width: 140rpx;
    color: #909399;
    flex-shrink: 0;
  }

  &__value {
    flex: 1;
    color: #333;
  }

  &__footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-top: 12rpx;
    padding-top: 12rpx;
    border-top: 1rpx solid #f0f0f0;
  }

  &__time {
    font-size: 24rpx;
    color: #c0c4cc;
  }

  &__arrow {
    font-size: 32rpx;
    color: #c0c4cc;
  }
}
</style>
