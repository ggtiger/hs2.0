<template>
  <view class="empty-state">
    <view class="empty-state__icon">{{ icon }}</view>
    <view class="empty-state__text">{{ text }}</view>
    <view v-if="$slots.action || actionText" class="empty-state__action" @click="$emit('action')">
      <slot name="action">{{ actionText }}</slot>
    </view>
  </view>
</template>

<script setup>
defineProps({
  icon: {
    type: String,
    default: '📭'
  },
  text: {
    type: String,
    default: '暂无数据'
  },
  actionText: {
    type: String,
    default: ''
  }
})
defineEmits(['action'])
</script>

<style lang="scss" scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 120rpx 0;

  &__icon {
    font-size: 96rpx;
    opacity: 0.6;
  }

  &__text {
    margin-top: 24rpx;
    font-size: 28rpx;
    color: #909399;
  }

  &__action {
    margin-top: 24rpx;
    padding: 12rpx 40rpx;
    color: #2f7df6;
    font-size: 28rpx;
    border: 1rpx solid #2f7df6;
    border-radius: 32rpx;
  }
}
</style>
