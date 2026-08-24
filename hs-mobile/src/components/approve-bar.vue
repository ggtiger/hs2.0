<template>
  <view class="approve-bar safe-bottom">
    <view v-if="showReject" class="approve-bar__btn approve-bar__btn--reject" @click="$emit('reject')">
      {{ rejectText }}
    </view>
    <view class="approve-bar__btn approve-bar__btn--primary" @click="$emit('approve')">
      {{ approveText }}
    </view>
  </view>
</template>

<script setup>
defineProps({
  approveText: {
    type: String,
    default: '通过'
  },
  rejectText: {
    type: String,
    default: '驳回'
  },
  showReject: {
    type: Boolean,
    default: true
  }
})
defineEmits(['approve', 'reject'])
</script>

<style lang="scss" scoped>
.approve-bar {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  gap: 24rpx;
  padding: 20rpx 24rpx;
  padding-bottom: calc(20rpx + constant(safe-area-inset-bottom));
  padding-bottom: calc(20rpx + env(safe-area-inset-bottom));
  background-color: #fff;
  box-shadow: 0 -2rpx 12rpx 0 rgba(0, 0, 0, 0.06);
  z-index: 100;

  &__btn {
    flex: 1;
    height: 88rpx;
    line-height: 88rpx;
    text-align: center;
    border-radius: 44rpx;
    font-size: 30rpx;
    font-weight: 500;

    &--reject {
      background-color: #fff;
      color: #f5222d;
      border: 1rpx solid #f5222d;
    }

    &--primary {
      background: linear-gradient(135deg, #2f7df6, #1a66d9);
      color: #fff;
    }
  }
}
</style>
