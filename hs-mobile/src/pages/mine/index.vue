<template>
  <view class="page">
    <!-- 自定义导航头部 -->
    <view class="header" :style="{ paddingTop: statusBarHeight + 'px' }">
      <view class="profile">
        <view class="profile__avatar">{{ avatarText }}</view>
        <view class="profile__info">
          <view class="profile__name">{{ userName || '未登录' }}</view>
          <view class="profile__dept">{{ deptName || '—' }}</view>
        </view>
        <view v-if="!isLogged" class="profile__login" @click="goLogin">登录</view>
      </view>
    </view>

    <!-- 个人数据概览 -->
    <view class="overview">
      <view class="overview__item">
        <text class="overview__value">{{ stats.cert }}</text>
        <text class="overview__label">资质证书</text>
      </view>
      <view class="overview__divider"></view>
      <view class="overview__item">
        <text class="overview__value">{{ stats.auth }}</text>
        <text class="overview__label">授权范围</text>
      </view>
      <view class="overview__divider"></view>
      <view class="overview__item">
        <text class="overview__value">{{ stats.todo }}</text>
        <text class="overview__label">待办事项</text>
      </view>
    </view>

    <!-- 功能菜单 -->
    <view class="menu-group">
      <view class="menu-item" @click="goPage('/pages/mine/cert')">
        <text class="menu-item__icon">🏅</text>
        <text class="menu-item__name">我的资质</text>
        <text class="menu-item__arrow">›</text>
      </view>
      <view class="menu-item" @click="goPage('/pages/mine/auth')">
        <text class="menu-item__icon">🛡️</text>
        <text class="menu-item__name">我的授权</text>
        <text class="menu-item__arrow">›</text>
      </view>
    </view>

    <view class="menu-group">
      <view class="menu-item" @click="goNotice">
        <text class="menu-item__icon">📢</text>
        <text class="menu-item__name">系统公告</text>
        <text class="menu-item__arrow">›</text>
      </view>
      <view class="menu-item" @click="toggleNotify">
        <text class="menu-item__icon">🔔</text>
        <text class="menu-item__name">推送通知</text>
        <switch :checked="notifyOn" color="#2f7df6" @change="onNotifyChange" />
      </view>
    </view>

    <view class="menu-group">
      <view class="menu-item" @click="changePwd">
        <text class="menu-item__icon">🔑</text>
        <text class="menu-item__name">修改密码</text>
        <text class="menu-item__arrow">›</text>
      </view>
      <view class="menu-item" @click="showAbout">
        <text class="menu-item__icon">ℹ️</text>
        <text class="menu-item__name">关于</text>
        <text class="menu-item__version">v{{ version }}</text>
      </view>
    </view>

    <view v-if="isLogged" class="logout" @click="onLogout">退出登录</view>
  </view>
</template>

<script setup>
import { ref, computed, reactive } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { useUserStore, useTodoStore } from '@/store'

const userStore = useUserStore()
const todoStore = useTodoStore()

const statusBarHeight = ref(20)
const version = ref('1.0.0')
const notifyOn = ref(true)
const stats = reactive({ cert: 0, auth: 0, todo: 0 })

const isLogged = computed(() => userStore.isLogged)
const userName = computed(() => userStore.userName)
const deptName = computed(() => userStore.deptName)
const avatarText = computed(() => (userName.value || '?').charAt(0))

onShow(() => {
  try {
    statusBarHeight.value = uni.getSystemInfoSync().statusBarHeight || 20
  } catch (e) {}
  notifyOn.value = uni.getStorageSync('hs_notify') !== 'off'
  stats.todo = todoStore.totalCount
})

function goLogin() {
  uni.navigateTo({ url: '/pages/login/index' })
}
function goPage(path) {
  if (!isLogged.value) return goLogin()
  uni.navigateTo({ url: path })
}
function goNotice() {
  uni.navigateTo({ url: '/pages/notice/list' })
}
function toggleNotify() {
  notifyOn.value = !notifyOn.value
  uni.setStorageSync('hs_notify', notifyOn.value ? 'on' : 'off')
}
function onNotifyChange(e) {
  notifyOn.value = e.detail.value
  uni.setStorageSync('hs_notify', notifyOn.value ? 'on' : 'off')
}
function changePwd() {
  uni.showModal({
    title: '修改密码',
    editable: true,
    placeholderText: '请联系管理员或在桌面端修改',
    showCancel: true,
    success: () => {}
  })
}
function showAbout() {
  uni.showModal({
    title: '华溯计量',
    content: '华溯计量管理系统移动端\n版本 v' + version.value + '\n© 2026',
    showCancel: false
  })
}
function onLogout() {
  uni.showModal({
    title: '提示',
    content: '确定要退出登录吗？',
    success: (res) => {
      if (res.confirm) {
        userStore.logout()
        uni.reLaunch({ url: '/pages/login/index' })
      }
    }
  })
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background-color: #f5f7fa;
  padding-bottom: 60rpx;
}

.header {
  background: linear-gradient(135deg, #2f7df6, #1a66d9);
  padding: 0 32rpx 60rpx;
}

.profile {
  display: flex;
  align-items: center;
  padding: 32rpx 0;

  &__avatar {
    width: 112rpx;
    height: 112rpx;
    border-radius: 50%;
    background-color: rgba(255, 255, 255, 0.25);
    color: #fff;
    font-size: 48rpx;
    font-weight: 600;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  &__info {
    flex: 1;
    margin-left: 24rpx;
    color: #fff;
  }

  &__name {
    font-size: 36rpx;
    font-weight: 600;
  }

  &__dept {
    margin-top: 8rpx;
    font-size: 24rpx;
    opacity: 0.9;
  }

  &__login {
    padding: 12rpx 32rpx;
    background-color: rgba(255, 255, 255, 0.2);
    border-radius: 32rpx;
    color: #fff;
    font-size: 26rpx;
  }
}

.overview {
  display: flex;
  align-items: center;
  margin: -32rpx 24rpx 0;
  padding: 32rpx 0;
  background-color: #fff;
  border-radius: 16rpx;
  box-shadow: 0 2rpx 12rpx 0 rgba(0, 0, 0, 0.05);
  position: relative;
  z-index: 2;

  &__item {
    flex: 1;
    text-align: center;
  }

  &__value {
    display: block;
    font-size: 40rpx;
    font-weight: 700;
    color: #2f7df6;
  }

  &__label {
    margin-top: 8rpx;
    font-size: 24rpx;
    color: #909399;
  }

  &__divider {
    width: 1rpx;
    height: 56rpx;
    background-color: #f0f0f0;
  }
}

.menu-group {
  margin: 24rpx;
  background-color: #fff;
  border-radius: 16rpx;
  overflow: hidden;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 28rpx 24rpx;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__icon {
    font-size: 36rpx;
    margin-right: 20rpx;
  }

  &__name {
    flex: 1;
    font-size: 30rpx;
    color: #333;
  }

  &__arrow {
    font-size: 36rpx;
    color: #c0c4cc;
  }

  &__version {
    font-size: 24rpx;
    color: #c0c4cc;
  }
}

.logout {
  margin: 40rpx 24rpx;
  height: 88rpx;
  line-height: 88rpx;
  text-align: center;
  background-color: #fff;
  border-radius: 16rpx;
  color: #f5222d;
  font-size: 30rpx;
}
</style>
