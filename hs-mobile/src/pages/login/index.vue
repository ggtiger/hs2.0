<template>
  <view class="page" :style="{ paddingTop: statusBarHeight + 'px' }">
    <view class="brand">
      <view class="brand__logo">📐</view>
      <view class="brand__title">睿谱希</view>
      <view class="brand__subtitle">LIMS 移动版</view>
    </view>

    <view class="form">
      <view class="form__item">
        <text class="form__icon">👤</text>
        <input
          v-model="form.username"
          class="form__input"
          placeholder="用户名 / 手机号"
          placeholder-class="form__placeholder"
        />
      </view>
      <view class="form__item">
        <text class="form__icon">🔒</text>
        <input
          v-model="form.password"
          class="form__input"
          :password="!showPwd"
          placeholder="密码"
          placeholder-class="form__placeholder"
        />
        <text class="form__toggle" @click="showPwd = !showPwd">{{ showPwd ? '🙈' : '👁️' }}</text>
      </view>

      <view class="form__extra">
        <view class="form__remember" @click="remember = !remember">
          <text class="form__checkbox" :class="{ 'form__checkbox--on': remember }">{{ remember ? '☑' : '☐' }}</text>
          <text class="form__remember-text">记住密码</text>
        </view>
      </view>

      <button class="form__btn" :loading="loading" :disabled="loading" @click="onLogin">
        {{ loading ? '登录中...' : '登 录' }}
      </button>
    </view>

    <!-- 客户入口（免登录） -->
    <view class="guest">
      <view class="guest__line"><text>客户自助入口</text></view>
      <view class="guest__btns">
        <view class="guest__btn" @click="go('/pages/out/verify')">📜 证书验证</view>
        <view class="guest__btn" @click="go('/pages/out/progress')">📊 进度查询</view>
        <view class="guest__btn" @click="go('/pages/out/logistics')">📦 物流查询</view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { useUserStore } from '@/store'

const userStore = useUserStore()

const statusBarHeight = ref(20)
const showPwd = ref(false)
const loading = ref(false)
const remember = ref(false)
const form = reactive({ username: '', password: '' })

onLoad(() => {
  try {
    statusBarHeight.value = uni.getSystemInfoSync().statusBarHeight || 20
  } catch (e) {}
  // 恢复记住的账号
  const saved = uni.getStorageSync('hs_login_remember')
  if (saved) {
    try {
      const obj = JSON.parse(saved)
      form.username = obj.username || ''
      form.password = obj.password || ''
      remember.value = true
    } catch (e) {}
  }
})

async function onLogin() {
  if (!form.username.trim()) {
    return uni.showToast({ title: '请输入用户名', icon: 'none' })
  }
  if (!form.password) {
    return uni.showToast({ title: '请输入密码', icon: 'none' })
  }

  loading.value = true
  try {
    await userStore.login(form.username.trim(), form.password)
    // 记住密码
    if (remember.value) {
      uni.setStorageSync('hs_login_remember', JSON.stringify({
        username: form.username,
        password: form.password
      }))
    } else {
      uni.removeStorageSync('hs_login_remember')
    }
    uni.showToast({ title: '登录成功', icon: 'success' })
    setTimeout(() => {
      uni.switchTab({ url: '/pages/index/index' })
    }, 600)
  } catch (e) {
    // 登录失败（密码错误/停用等）由 store 抛出，此处展示消息
    if (e && e.message) {
      uni.showToast({ title: e.message, icon: 'none' })
    }
  } finally {
    loading.value = false
  }
}

function go(url) {
  uni.navigateTo({ url })
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: linear-gradient(180deg, #2f7df6 0%, #4a8df8 40%, #f5f7fa 100%);
  padding: 0 48rpx;
}

.brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 100rpx 0 60rpx;
  color: #fff;

  &__logo {
    font-size: 100rpx;
    width: 160rpx;
    height: 160rpx;
    border-radius: 40rpx;
    background-color: rgba(255, 255, 255, 0.2);
    display: flex;
    align-items: center;
    justify-content: center;
  }

  &__title {
    margin-top: 24rpx;
    font-size: 44rpx;
    font-weight: 700;
  }

  &__subtitle {
    margin-top: 8rpx;
    font-size: 26rpx;
    opacity: 0.9;
  }
}

.form {
  background-color: #fff;
  border-radius: 24rpx;
  padding: 48rpx 40rpx;
  box-shadow: 0 8rpx 40rpx rgba(0, 0, 0, 0.08);

  &__item {
    display: flex;
    align-items: center;
    height: 96rpx;
    border-bottom: 1rpx solid #f0f0f0;
  }

  &__icon {
    font-size: 36rpx;
    margin-right: 20rpx;
  }

  &__input {
    flex: 1;
    height: 96rpx;
    font-size: 30rpx;
    color: #333;
  }

  &__placeholder {
    color: #c0c4cc;
  }

  &__toggle {
    font-size: 32rpx;
    padding: 0 8rpx;
  }

  &__extra {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin: 32rpx 0;
  }

  &__remember {
    display: flex;
    align-items: center;
  }

  &__checkbox {
    font-size: 32rpx;
    color: #c0c4cc;

    &--on {
      color: #2f7df6;
    }
  }

  &__remember-text {
    margin-left: 12rpx;
    font-size: 26rpx;
    color: #666;
  }

  &__btn {
    margin-top: 16rpx;
    width: 100%;
    height: 96rpx;
    line-height: 96rpx;
    background: linear-gradient(135deg, #2f7df6, #1a66d9);
    color: #fff;
    border-radius: 48rpx;
    font-size: 32rpx;
    border: none;

    &::after {
      border: none;
    }

    &[disabled] {
      opacity: 0.7;
    }
  }
}

.guest {
  margin-top: 60rpx;

  &__line {
    display: flex;
    align-items: center;
    justify-content: center;
    color: #909399;
    font-size: 24rpx;
    margin-bottom: 32rpx;

    &::before,
    &::after {
      content: '';
      width: 80rpx;
      height: 1rpx;
      background-color: #dcdfe6;
      margin: 0 16rpx;
    }
  }

  &__btns {
    display: flex;
    gap: 20rpx;
  }

  &__btn {
    flex: 1;
    height: 80rpx;
    line-height: 80rpx;
    text-align: center;
    background-color: #fff;
    border-radius: 16rpx;
    font-size: 26rpx;
    color: #2f7df6;
    box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.04);
  }
}
</style>
