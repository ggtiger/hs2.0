<template>
  <view class="page">
    <!-- 查询表单 -->
    <view v-if="!cert" class="form-wrap">
      <view class="logo">📜</view>
      <view class="title">电子证书验证</view>
      <view class="subtitle">输入证书编号或扫描二维码验证真伪</view>

      <view class="form">
        <input v-model="certNo" class="form__input" placeholder="请输入证书编号" confirm-type="search" @confirm="onCheck" />
        <view class="form__scan" @click="onScan">扫码验证</view>
        <button class="form__btn" :loading="loading" :disabled="loading" @click="onCheck">立即验证</button>
      </view>
    </view>

    <!-- 密码输入（需要密码时） -->
    <view v-else-if="needPwd && !verified" class="form-wrap">
      <view class="logo">🔐</view>
      <view class="title">请输入查看密码</view>
      <view class="subtitle">证书 {{ certNo }} 需要密码验证</view>
      <view class="form">
        <input v-model="pwd" class="form__input" :password="true" placeholder="请输入查看密码" @confirm="onVerify" />
        <button class="form__btn" :loading="loading" :disabled="loading" @click="onVerify">确认</button>
        <view class="form__back" @click="reset">返回重新输入</view>
      </view>
    </view>

    <!-- 验证结果 -->
    <view v-else-if="cert" class="result">
      <view class="result__banner">
        <text class="result__icon">✓</text>
        <text class="result__text">验证通过</text>
      </view>
      <view class="card">
        <view class="field"><text class="field__label">证书编号</text><text class="field__value">{{ cert.CERTNO || certNo }}</text></view>
        <view class="field"><text class="field__label">委托单位</text><text class="field__value">{{ cert.CUSTNAME || '—' }}</text></view>
        <view class="field"><text class="field__label">设备名称</text><text class="field__value">{{ cert.EQUIPNAME || '—' }}</text></view>
        <view class="field"><text class="field__label">规格型号</text><text class="field__value">{{ cert.SPEC || '—' }}</text></view>
        <view class="field"><text class="field__label">出厂编号</text><text class="field__value">{{ cert.FACTORYNO || '—' }}</text></view>
        <view class="field"><text class="field__label">签发日期</text><text class="field__value">{{ formatDate(cert.SIGNDATE) }}</text></view>
        <view class="field"><text class="field__label">有效期至</text><text class="field__value">{{ formatDate(cert.VALIDDATE) }}</text></view>
      </view>

      <view v-if="cert.PDFURL" class="pdf-actions">
        <button class="form__btn" @click="viewPdf">查看证书 PDF</button>
      </view>
      <view class="form__back" @click="reset">验证其他证书</view>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { checkEcert, viewEcert } from '@/api/outer'
import { scanCode } from '@/utils/scan'
import { formatDate } from '@/utils/format'

const certNo = ref('')
const pwd = ref('')
const cert = ref(null)
const needPwd = ref(false)
const verified = ref(false)
const loading = ref(false)

onLoad((options) => {
  if (options.ID) certNo.value = options.ID
  if (options.certNo) certNo.value = options.certNo
})

async function onCheck() {
  if (!certNo.value.trim()) {
    return uni.showToast({ title: '请输入证书编号', icon: 'none' })
  }
  loading.value = true
  try {
    // 检查证书是否存在及是否需要密码（LI_ECERT/A02）
    const res = await checkEcert({ CERTNO: certNo.value.trim() })
    needPwd.value = res?.NEED_PWD === 1
    cert.value = needPwd.value ? null : await viewEcert({ CERTNO: certNo.value.trim() })
    verified.value = !needPwd.value
  } catch (e) {} finally {
    loading.value = false
  }
}

async function onVerify() {
  if (!pwd.value) return uni.showToast({ title: '请输入密码', icon: 'none' })
  loading.value = true
  try {
    cert.value = await viewEcert({ CERTNO: certNo.value.trim(), ECERTPWD: pwd.value })
    verified.value = true
  } catch (e) {} finally {
    loading.value = false
  }
}

async function onScan() {
  try {
    const res = await scanCode()
    if (res.result) {
      certNo.value = res.result
      onCheck()
    }
  } catch (e) {}
}

function viewPdf() {
  // TODO: 接入 PDF 预览
  // #ifdef H5
  window.open(cert.value.PDFURL)
  // #endif
  // #ifndef H5
  uni.showToast({ title: 'PDF 预览待接入', icon: 'none' })
  // #endif
}

function reset() {
  certNo.value = ''
  pwd.value = ''
  cert.value = null
  needPwd.value = false
  verified.value = false
}
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #f5f7fa; padding: 0 48rpx; }
.form-wrap { display: flex; flex-direction: column; align-items: center; padding-top: 120rpx; }
.logo { width: 140rpx; height: 140rpx; border-radius: 36rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; font-size: 64rpx; display: flex; align-items: center; justify-content: center; }
.title { margin-top: 32rpx; font-size: 40rpx; font-weight: 700; color: #1a1a1a; }
.subtitle { margin-top: 12rpx; font-size: 26rpx; color: #909399; text-align: center; }
.form { width: 100%; margin-top: 60rpx; }
.form__input { width: 100%; height: 96rpx; padding: 0 32rpx; background-color: #fff; border-radius: 16rpx; font-size: 30rpx; box-sizing: border-box; }
.form__scan { text-align: center; margin: 24rpx 0; font-size: 28rpx; color: #2f7df6; }
.form__btn { margin-top: 24rpx; width: 100%; height: 96rpx; line-height: 96rpx; background: linear-gradient(135deg, #2f7df6, #1a66d9); color: #fff; border-radius: 48rpx; font-size: 32rpx; border: none;
  &::after { border: none; } }
.form__back { text-align: center; margin-top: 40rpx; font-size: 26rpx; color: #909399; }

.result { padding-top: 40rpx; }
.result__banner { display: flex; flex-direction: column; align-items: center; padding: 40rpx 0; }
.result__icon { width: 96rpx; height: 96rpx; line-height: 96rpx; text-align: center; border-radius: 50%; background-color: #07c160; color: #fff; font-size: 56rpx; }
.result__text { margin-top: 16rpx; font-size: 32rpx; font-weight: 600; color: #07c160; }
.card { background-color: #fff; border-radius: 16rpx; padding: 24rpx; margin-top: 24rpx; }
.field { display: flex; padding: 12rpx 0; font-size: 28rpx; }
.field__label { width: 160rpx; color: #909399; }
.field__value { flex: 1; color: #333; }
.pdf-actions { margin-top: 32rpx; }
</style>
