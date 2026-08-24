<template>
  <view class="page">
    <view class="scan-hero">
      <view class="scan-hero__ring" @click="doScan()">
        <text class="scan-hero__icon">⌖</text>
        <text class="scan-hero__text">点击扫码</text>
      </view>
    </view>

    <!-- 扫码类型 -->
    <view class="section">
      <view class="section__title">选择扫码类型</view>
      <view class="type-grid">
        <view
          v-for="t in types"
          :key="t.value"
          class="type-item"
          :class="{ 'type-item--active': activeType === t.value }"
          @click="selectType(t.value)"
        >
          <text class="type-item__icon">{{ t.icon }}</text>
          <text class="type-item__name">{{ t.name }}</text>
        </view>
      </view>
    </view>

    <!-- 手动输入 -->
    <view class="section">
      <view class="section__title">{{ activeTypeObj.name }}编号查询</view>
      <view class="manual">
        <input
          v-model="manualCode"
          class="manual__input"
          :placeholder="activeTypeObj.placeholder"
          confirm-type="search"
          @confirm="doManual"
        />
        <view class="manual__btn" @click="doManual">查询</view>
      </view>
    </view>

    <!-- 扫码历史 -->
    <view class="section">
      <view class="section__head">
        <text class="section__title">最近记录</text>
        <text v-if="history.length" class="section__clear" @click="clearHistory">清空</text>
      </view>
      <view class="history">
        <view
          v-for="(item, idx) in history"
          :key="idx"
          class="history-item"
          @click="handleResult(item.code)"
        >
          <text class="history-item__icon">{{ typeMap[item.type]?.icon || '🔎' }}</text>
          <view class="history-item__main">
            <text class="history-item__code">{{ item.code }}</text>
            <text class="history-item__time">{{ item.time }}</text>
          </view>
        </view>
        <empty-state v-if="!history.length" icon="🕐" text="暂无扫码记录" />
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { scanCode } from '@/utils/scan'
import { formatDateTime } from '@/utils/format'

const types = [
  { value: 'sample', name: '查样品', icon: '🔬', placeholder: '请输入样品条码/委托单号' },
  { value: 'cert', name: '查证书', icon: '📜', placeholder: '请输入证书编号' },
  { value: 'logistics', name: '查物流', icon: '📦', placeholder: '请输入物流单号' },
  { value: 'addLogistics', name: '新增物流', icon: '📮', placeholder: '请扫描受理单条码' }
]
const typeMap = types.reduce((m, t) => ((m[t.value] = t), m), {})

const activeType = ref('cert')
const manualCode = ref('')
const history = ref([])

const activeTypeObj = computed(() => typeMap[activeType.value])

onShow(() => {
  // 读取本地历史（注意：沙箱环境无 Date，此处时间由调用方写入）
  try {
    const raw = uni.getStorageSync('hs_scan_history')
    history.value = raw ? JSON.parse(raw) : []
  } catch (e) {
    history.value = []
  }
})

function selectType(value) {
  activeType.value = value
  manualCode.value = ''
}

async function doScan() {
  try {
    const res = await scanCode()
    if (res.manual) {
      // H5 端走手动输入
      return
    }
    if (res.result) {
      handleResult(res.result)
    }
  } catch (e) {
    if (e.message !== 'cancel') {
      uni.showToast({ title: '扫码取消或失败', icon: 'none' })
    }
  }
}

function doManual() {
  if (!manualCode.value.trim()) {
    uni.showToast({ title: '请输入编号', icon: 'none' })
    return
  }
  handleResult(manualCode.value.trim())
}

function handleResult(code) {
  // 记录历史
  saveHistory(code)
  // 根据类型跳转
  const routes = {
    sample: `/pages/query/delegate?keyword=${encodeURIComponent(code)}`,
    cert: `/pages/query/cert?keyword=${encodeURIComponent(code)}`,
    logistics: `/pages/query/logistics?keyword=${encodeURIComponent(code)}`,
    addLogistics: `/pages/logistics/search?keyword=${encodeURIComponent(code)}`
  }
  uni.navigateTo({ url: routes[activeType.value] })
}

function saveHistory(code) {
  const item = {
    type: activeType.value,
    code,
    time: formatDateTime(Date.now(), 'MM-DD HH:mm')
  }
  history.value.unshift(item)
  history.value = history.value.slice(0, 10)
  uni.setStorageSync('hs_scan_history', JSON.stringify(history.value))
}

function clearHistory() {
  history.value = []
  uni.removeStorageSync('hs_scan_history')
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background-color: #f5f7fa;
  padding-bottom: 40rpx;
}

.scan-hero {
  display: flex;
  justify-content: center;
  padding: 60rpx 0;
  background: linear-gradient(135deg, #2f7df6, #1a66d9);

  &__ring {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    width: 240rpx;
    height: 240rpx;
    border-radius: 50%;
    background-color: rgba(255, 255, 255, 0.15);
    border: 4rpx solid rgba(255, 255, 255, 0.6);
    color: #fff;
  }

  &__icon {
    font-size: 80rpx;
  }

  &__text {
    margin-top: 12rpx;
    font-size: 28rpx;
  }
}

.section {
  margin: 24rpx;
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;

  &__title {
    font-size: 28rpx;
    font-weight: 600;
    color: #1a1a1a;
    margin-bottom: 24rpx;
  }

  &__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16rpx;
  }

  &__clear {
    font-size: 24rpx;
    color: #f5222d;
  }
}

.type-grid {
  display: flex;
  gap: 20rpx;
}

.type-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 24rpx 0;
  border-radius: 12rpx;
  background-color: #f5f7fa;

  &--active {
    background-color: #e8f1fe;
    border: 2rpx solid #2f7df6;
  }

  &__icon {
    font-size: 44rpx;
  }

  &__name {
    margin-top: 8rpx;
    font-size: 26rpx;
    color: #333;
  }
}

.manual {
  display: flex;
  gap: 16rpx;

  &__input {
    flex: 1;
    height: 76rpx;
    padding: 0 24rpx;
    background-color: #f5f7fa;
    border-radius: 12rpx;
    font-size: 28rpx;
  }

  &__btn {
    padding: 0 40rpx;
    height: 76rpx;
    line-height: 76rpx;
    background: linear-gradient(135deg, #2f7df6, #1a66d9);
    color: #fff;
    border-radius: 12rpx;
    font-size: 28rpx;
  }
}

.history-item {
  display: flex;
  align-items: center;
  padding: 20rpx 0;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__icon {
    font-size: 36rpx;
    margin-right: 20rpx;
  }

  &__main {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  &__code {
    font-size: 28rpx;
    color: #333;
  }

  &__time {
    font-size: 24rpx;
    color: #c0c4cc;
  }
}
</style>
