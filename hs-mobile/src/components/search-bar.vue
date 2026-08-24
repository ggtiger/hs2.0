<template>
  <view class="search-bar">
    <view class="search-bar__input-wrap">
      <text class="search-bar__icon">🔍</text>
      <input
        class="search-bar__input"
        :value="modelValue"
        :placeholder="placeholder"
        confirm-type="search"
        @input="onInput"
        @confirm="onConfirm"
      />
      <text v-if="modelValue" class="search-bar__clear" @click="onClear">✕</text>
    </view>
    <view v-if="showSearchBtn" class="search-bar__btn" @click="onConfirm">搜索</view>
  </view>
</template>

<script setup>
const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  placeholder: {
    type: String,
    default: '请输入关键词搜索'
  },
  showSearchBtn: {
    type: Boolean,
    default: true
  }
})
const emit = defineEmits(['update:modelValue', 'search', 'clear'])

function onInput(e) {
  emit('update:modelValue', e.detail.value)
}
function onConfirm() {
  emit('search', props.modelValue)
}
function onClear() {
  emit('update:modelValue', '')
  emit('clear')
}
</script>

<style lang="scss" scoped>
.search-bar {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 16rpx 24rpx;
  background-color: #fff;

  &__input-wrap {
    flex: 1;
    display: flex;
    align-items: center;
    height: 72rpx;
    padding: 0 20rpx;
    background-color: #f2f3f5;
    border-radius: 36rpx;
  }

  &__icon {
    font-size: 28rpx;
    margin-right: 12rpx;
  }

  &__input {
    flex: 1;
    font-size: 28rpx;
    color: #333;
  }

  &__clear {
    padding: 0 8rpx;
    color: #c0c4cc;
    font-size: 28rpx;
  }

  &__btn {
    color: #2f7df6;
    font-size: 28rpx;
    padding: 0 8rpx;
  }
}
</style>
