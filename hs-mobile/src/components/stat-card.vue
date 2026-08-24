<template>
  <view class="stat-card" :style="{ background: bg }" @click="$emit('click')">
    <view class="stat-card__value">{{ value }}</view>
    <view class="stat-card__label">{{ label }}</view>
  </view>
</template>

<script setup>
defineProps({
  value: {
    type: [Number, String],
    default: 0
  },
  label: {
    type: String,
    default: ''
  },
  bg: {
    type: String,
    default: 'linear-gradient(135deg, #2f7df6, #1a66d9)'
  }
})
defineEmits(['click'])
</script>

<style lang="scss" scoped>
.stat-card {
  flex: 1;
  padding: 28rpx 20rpx;
  border-radius: 16rpx;
  color: #fff;
  text-align: center;

  &__value {
    font-size: 52rpx;
    font-weight: 700;
    line-height: 1.2;
  }

  &__label {
    margin-top: 8rpx;
    font-size: 24rpx;
    opacity: 0.9;
  }
}
</style>
