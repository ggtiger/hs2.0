<template>
  <view class="state-tag" :style="{ color: info.color, backgroundColor: bgColor }">
    {{ info.label }}
  </view>
</template>

<script setup>
import { computed } from 'vue'
import { getStateInfo } from '@/utils/state'

const props = defineProps({
  state: {
    type: [Number, String],
    default: ''
  }
})

const info = computed(() => getStateInfo(props.state))

// 背景色 = 文字色 + 低透明度
const bgColor = computed(() => {
  const hex = info.value.color
  // hex → rgba(20%)
  if (hex && hex.startsWith('#')) {
    const r = parseInt(hex.slice(1, 3), 16)
    const g = parseInt(hex.slice(3, 5), 16)
    const b = parseInt(hex.slice(5, 7), 16)
    return `rgba(${r}, ${g}, ${b}, 0.12)`
  }
  return 'rgba(144, 147, 153, 0.12)'
})
</script>

<style lang="scss" scoped>
.state-tag {
  display: inline-flex;
  align-items: center;
  padding: 2rpx 16rpx;
  border-radius: 8rpx;
  font-size: 22rpx;
  line-height: 1.6;
  white-space: nowrap;
}
</style>
