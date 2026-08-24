<template>
  <view class="page">
    <scroll-view scroll-y class="content">
      <!-- 关联受理单 -->
      <view class="card">
        <view class="card__title">📋 关联受理单</view>
        <view v-if="acceptList.length" class="ref-list">
          <view v-for="(item, idx) in acceptList" :key="idx" class="ref-tag">
            <text class="ref-tag__code">{{ item.ACCEPTCODE }}</text>
          </view>
        </view>
        <view v-else class="ref-info">
          <text class="ref-info__hint">未关联受理单</text>
        </view>
      </view>

      <!-- 物流类型 -->
      <view class="card">
        <view class="card__title">📦 物流类型</view>
        <view class="type-tabs">
          <view
            v-for="t in typeOptions"
            :key="t.value"
            class="type-tab"
            :class="{ 'type-tab--active': form.REFTYPE === t.value }"
            @click="form.REFTYPE = t.value"
          >
            <text>{{ t.label }}</text>
          </view>
        </view>
      </view>

      <!-- 物流信息 -->
      <view class="card">
        <view class="card__title">🚚 物流信息</view>

        <!-- 快递公司 -->
        <view class="form-item">
          <text class="form-item__label">快递公司</text>
          <view class="form-item__input-wrap">
            <input v-model="form.EXPCOMPANY" class="form-item__input" placeholder="请输入快递公司" />
          </view>
        </view>
        <!-- 常用快递快捷选择 -->
        <view class="quick-tags">
          <view
            v-for="c in commonCarriers"
            :key="c"
            class="quick-tag"
            @click="form.EXPCOMPANY = c"
          >
            <text>{{ c }}</text>
          </view>
        </view>

        <!-- 物流单号 -->
        <view class="form-item">
          <text class="form-item__label">物流单号</text>
          <view class="form-item__input-wrap">
            <input v-model="form.LOGISTICSNO" class="form-item__input" placeholder="请输入或扫码" />
            <view class="scan-btn" @click="scanLogisticsNo">
              <uv-icon name="scan" size="20" color="#2F54EB"></uv-icon>
            </view>
          </view>
        </view>

        <!-- 寄出日期 -->
        <view class="form-item">
          <text class="form-item__label">寄出日期</text>
          <picker mode="date" :value="form.SENDDATE" @change="onDateChange">
            <view class="form-item__input-wrap">
              <text class="form-item__input form-item__input--picker">{{ form.SENDDATE || '请选择日期' }}</text>
              <uv-icon name="arrow-right" size="16" color="#ccc"></uv-icon>
            </view>
          </picker>
        </view>
      </view>

      <!-- 收件信息 -->
      <view class="card">
        <view class="card__title">📬 收件信息</view>

        <view class="form-item">
          <text class="form-item__label">收件人</text>
          <view class="form-item__input-wrap">
            <input v-model="form.RECEIVENAME" class="form-item__input" placeholder="请输入收件人" />
          </view>
        </view>

        <view class="form-item">
          <text class="form-item__label">联系电话</text>
          <view class="form-item__input-wrap">
            <input v-model="form.RECEIVEPHONE" class="form-item__input" placeholder="请输入电话" type="number" />
          </view>
        </view>

        <view class="form-item">
          <text class="form-item__label">收件地址</text>
          <view class="form-item__input-wrap">
            <textarea v-model="form.RECEIVEADDR" class="form-item__textarea" placeholder="请输入收件地址" :auto-height="true" />
          </view>
        </view>
      </view>

      <!-- 拍照记录 -->
      <view class="card">
        <view class="card__title">📷 拍照记录</view>
        <view class="photo-grid">
          <view v-for="(photo, idx) in photoList" :key="idx" class="photo-item">
            <image :src="getPhotoUrl(photo)" class="photo-item__img" mode="aspectFill" @click="previewPhoto(idx)" />
            <view v-if="photo.uploading" class="photo-item__loading">
              <uv-loading-icon size="20" color="#fff"></uv-loading-icon>
            </view>
            <view class="photo-item__del" @click="removePhoto(idx)">✕</view>
          </view>
          <view v-if="photoList.length < 9" class="photo-item photo-item--add" @click="takePhoto">
            <text class="photo-item__icon">＋</text>
            <text class="photo-item__text">拍照</text>
          </view>
        </view>
      </view>

      <!-- 备注 -->
      <view class="card">
        <view class="card__title">📝 备注</view>
        <textarea v-model="form.REMARK" class="remark" placeholder="请输入备注（选填）" maxlength="500" />
      </view>

      <view style="height: 160rpx"></view>
    </scroll-view>

    <!-- 底部保存按钮 -->
    <view class="footer">
      <uv-button type="primary" text="保存" :loading="saving" @click="onSave"></uv-button>
    </view>
  </view>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { saveLogistics } from '@/api/logistics'
import { scanCode } from '@/utils/scan'
import { getToken } from '@/utils/auth'
import { DATA_BASE_URL } from '@/utils/config'

const refId = ref('')
const refCode = ref('')
const saving = ref(false)
// 照片列表：每项 { tempPath: 本地临时路径, fileId: 上传后的文件ID, uploading: 是否上传中 }
const photoList = ref([])

// 获取照片显示 URL：上传成功后用后端文件 URL，否则用本地临时路径
function getPhotoUrl(photo) {
  if (photo.fileId) {
    // #ifdef H5
    return `/api/file/${photo.fileId}`
    // #endif
    // #ifndef H5
    return `${DATA_BASE_URL}/api/file/${photo.fileId}`
    // #endif
  }
  return photo.tempPath
}

const typeOptions = [
  { value: '1', label: '样品' },
  { value: '2', label: '证书' }
]

const commonCarriers = ['顺丰', '中通', '圆通', '韵达', 'EMS', '申通', '百世', '京东']

const form = reactive({
  REFTYPE: '1',
  EXPCOMPANY: '',
  LOGISTICSNO: '',
  SENDDATE: '',
  RECEIVENAME: '',
  RECEIVEPHONE: '',
  RECEIVEADDR: '',
  REMARK: '',
  FILES: ''
})

// 关联受理单列表（支持多个）
const acceptList = ref([])

onLoad((options) => {
  // 支持单个受理单（旧模式）或多个（新模式）
  if (options.refId) {
    acceptList.value.push({
      ACCEPTID: options.refId,
      ACCEPTCODE: decodeURIComponent(options.refCode || '')
    })
  }
  // 支持多选传入：refIds=id1,id2&refCodes=code1,code2
  if (options.refIds) {
    const ids = options.refIds.split(',')
    const codes = (options.refCodes || '').split(',').map(c => decodeURIComponent(c))
    acceptList.value = ids.map((id, i) => ({ ACCEPTID: id, ACCEPTCODE: codes[i] || '' }))
  }
  form.REFTYPE = options.refType || '1'

  // 默认寄出日期为今天
  const now = new Date()
  form.SENDDATE = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
})

function onDateChange(e) {
  form.SENDDATE = e.detail.value
}

async function scanLogisticsNo() {
  try {
    const res = await scanCode({ scanType: ['barCode'] })
    if (res && res.result) {
      form.LOGISTICS_NO = res.result
    } else if (res && res.manual) {
      // H5 降级手动输入，聚焦到物流单号输入框
      uni.showToast({ title: '请手动输入物流单号', icon: 'none' })
    }
  } catch (e) {
    // 用户取消扫码
  }
}

/**
 * 拍照并上传
 * 对齐 PC 端 RsUploader：POST /api/file/，multipart/form-data，
 * 带 Authorization header 和 _userInfo_，返回 { id, key }
 *
 * 关键：通过 photoList.value[idx] 更新属性，确保 Vue 3 响应式追踪
 */
function takePhoto() {
  uni.chooseImage({
    count: 9 - photoList.value.length,
    sizeType: ['compressed'],
    sourceType: ['camera'],
    success: (res) => {
      res.tempFilePaths.forEach((path) => {
        const idx = photoList.value.length
        photoList.value.push({ tempPath: path, fileId: '', uploading: true })
        uploadFile(idx)
      })
    },
    fail: () => {
      // camera 失败时降级为相册+相机
      uni.chooseImage({
        count: 9 - photoList.value.length,
        sizeType: ['compressed'],
        sourceType: ['album', 'camera'],
        success: (res) => {
          res.tempFilePaths.forEach((path) => {
            const idx = photoList.value.length
            photoList.value.push({ tempPath: path, fileId: '', uploading: true })
            uploadFile(idx)
          })
        }
      })
    }
  })
}

/**
 * 上传单个文件到后端 /api/file/
 * @param {number} idx 照片在 photoList 中的索引，通过索引更新确保响应式
 */
function uploadFile(idx) {
  // #ifdef H5
  uploadFileH5(idx)
  // #endif
  // #ifndef H5
  uploadFileNative(idx)
  // #endif
}

// H5 端用原生 XMLHttpRequest
function uploadFileH5(idx) {
  const photo = photoList.value[idx]
  if (!photo) return

  const xhr = new XMLHttpRequest()
  xhr.open('POST', '/api/file/')

  const fd = new FormData()
  fd.append('chunks', '1')
  fd.append('chunk', '0')
  fd.append('fileName', `logistics_${Date.now()}.jpg`)

  const userInfo = uni.getStorageSync('hs_user_info') || {}
  fd.append('_userInfo_', JSON.stringify(userInfo))

  // blob URL → fetch blob → append to FormData
  fetch(photo.tempPath)
    .then(r => r.blob())
    .then(blob => {
      fd.append('file', blob, `logistics_${Date.now()}.jpg`)

      xhr.setRequestHeader('Authorization', `Bearer ${getToken()}`)

      xhr.onload = () => {
        // 通过索引更新，触发 Vue 3 响应式
        if (photoList.value[idx]) {
          photoList.value[idx].uploading = false
        }
        try {
          const data = JSON.parse(xhr.responseText)
          if (data.id) {
            const ids = data.id.split(',').filter(Boolean)
            const fileId = ids[ids.length - 1]
            if (photoList.value[idx]) {
              photoList.value[idx].fileId = fileId
            }
          }
        } catch (e) {
          console.warn('上传响应解析失败:', e)
        }
      }
      xhr.onerror = () => {
        if (photoList.value[idx]) {
          photoList.value[idx].uploading = false
        }
        console.warn('上传失败')
      }
      xhr.send(fd)
    })
    .catch(() => {
      if (photoList.value[idx]) {
        photoList.value[idx].uploading = false
      }
    })
}

// 小程序/App 端用 uni.uploadFile
function uploadFileNative(idx) {
  const photo = photoList.value[idx]
  if (!photo) return

  const url = `${DATA_BASE_URL}/api/file/`
  const userInfo = uni.getStorageSync('hs_user_info') || {}

  uni.uploadFile({
    url,
    filePath: photo.tempPath,
    name: 'file',
    header: {
      Authorization: `Bearer ${getToken()}`
    },
    formData: {
      chunks: '1',
      chunk: '0',
      fileName: `logistics_${Date.now()}.jpg`,
      _userInfo_: JSON.stringify(userInfo)
    },
    success: (res) => {
      // 通过索引更新，触发 Vue 3 响应式
      if (photoList.value[idx]) {
        photoList.value[idx].uploading = false
      }
      try {
        const data = JSON.parse(res.data)
        if (data.id) {
          const ids = data.id.split(',').filter(Boolean)
          const fileId = ids[ids.length - 1]
          if (photoList.value[idx]) {
            photoList.value[idx].fileId = fileId
          }
        }
      } catch (e) {
        console.warn('上传响应解析失败:', e)
      }
    },
    fail: () => {
      if (photoList.value[idx]) {
        photoList.value[idx].uploading = false
      }
    }
  })
}

function removePhoto(idx) {
  photoList.value.splice(idx, 1)
}

function previewPhoto(idx) {
  uni.previewImage({
    urls: photoList.value.map(p => getPhotoUrl(p)),
    current: getPhotoUrl(photoList.value[idx])
  })
}

async function onSave() {
  if (acceptList.value.length === 0) {
    return uni.showToast({ title: '请关联至少一个受理单', icon: 'none' })
  }
  if (!form.LOGISTICSNO.trim()) {
    return uni.showToast({ title: '请输入物流单号', icon: 'none' })
  }
  if (!form.EXPCOMPANY.trim()) {
    return uni.showToast({ title: '请输入快递公司', icon: 'none' })
  }

  // 等待所有图片上传完成
  const uploading = photoList.value.filter(p => p.uploading)
  if (uploading.length > 0) {
    uni.showToast({ title: '图片上传中，请稍候', icon: 'none' })
    return
  }

  saving.value = true
  try {
    // 构造物流节点数据（对齐 PC 端 DTS TSS_LOGISTICS_NODE）
    const nodes = photoList.value
      .filter(p => p.fileId)
      .map(p => ({
        NODETIME: form.SENDDATE,
        NODEDESC: '物流寄送照片',
        NODEIMAGE: p.fileId
      }))

    // 图片文件ID列表
    const fileIds = photoList.value.filter(p => p.fileId).map(p => p.fileId).join(',')

    await saveLogistics({
      REFTYPE: form.REFTYPE,
      EXPCOMPANY: form.EXPCOMPANY,
      LOGISTICSNO: form.LOGISTICSNO,
      SENDDATE: form.SENDDATE,
      RECEIVENAME: form.RECEIVENAME,
      RECEIVEPHONE: form.RECEIVEPHONE,
      RECEIVEADDR: form.RECEIVEADDR,
      REMARK: form.REMARK,
      FILES: fileIds
    }, acceptList.value, nodes)
    uni.showToast({ title: '保存成功', icon: 'success' })
    setTimeout(() => uni.navigateBack(), 800)
  } catch (e) {
    // 请求层已提示
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f7fa;
}

.content {
  flex: 1;
  padding: 24rpx;
}

.card {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 24rpx;
  margin-bottom: 24rpx;

  &__title {
    font-size: 30rpx;
    font-weight: 600;
    color: #1a1a1a;
    margin-bottom: 20rpx;
  }
}

.ref-info {
  &__code {
    font-size: 32rpx;
    font-weight: 600;
    color: #2F54EB;
    display: block;
    margin-bottom: 8rpx;
  }

  &__hint {
    font-size: 24rpx;
    color: #999;
  }
}

.type-tabs {
  display: flex;
  gap: 16rpx;
}

.type-tab {
  flex: 1;
  text-align: center;
  padding: 16rpx 0;
  border-radius: 12rpx;
  font-size: 28rpx;
  color: #666;
  background-color: #f5f7fa;
  border: 2rpx solid transparent;

  &--active {
    color: #2F54EB;
    background-color: #F0F5FF;
    border-color: #2F54EB;
    font-weight: 600;
  }
}

.form-item {
  display: flex;
  align-items: center;
  padding: 16rpx 0;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__label {
    width: 160rpx;
    font-size: 28rpx;
    color: #666;
    flex-shrink: 0;
    text-align: right;
    margin-right: 16rpx;
  }

  &__input-wrap {
    flex: 1;
    display: flex;
    align-items: center;
    min-width: 0;
  }

  &__input {
    flex: 1;
    font-size: 28rpx;
    color: #333;
    min-width: 0;

    &--picker {
      display: block;
      padding: 8rpx 0;
    }
  }

  &__textarea {
    flex: 1;
    font-size: 28rpx;
    color: #333;
    min-height: 60rpx;
    min-width: 0;
  }
}

.scan-btn {
  flex-shrink: 0;
  margin-left: 12rpx;
  padding: 8rpx;
}

.quick-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
  padding: 8rpx 0 16rpx;
}

.quick-tag {
  padding: 8rpx 20rpx;
  border-radius: 20rpx;
  font-size: 24rpx;
  color: #666;
  background-color: #f5f7fa;

  &:active {
    background-color: #e8e8e8;
  }
}

.photo-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 16rpx;
}

.photo-item {
  position: relative;
  width: calc((100% - 32rpx) / 3);
  aspect-ratio: 1;
  border-radius: 12rpx;
  overflow: hidden;

  &--add {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background-color: #f5f7fa;
    border: 2rpx dashed #d9d9d9;
  }

  &__img {
    width: 100%;
    height: 100%;
  }

  &__icon {
    font-size: 56rpx;
    color: #bbb;
    line-height: 1;
  }

  &__text {
    font-size: 22rpx;
    color: #999;
    margin-top: 4rpx;
  }

  &__del {
    position: absolute;
    top: 0;
    right: 0;
    width: 40rpx;
    height: 40rpx;
    background-color: rgba(0, 0, 0, 0.5);
    color: #fff;
    font-size: 22rpx;
    display: flex;
    align-items: center;
    justify-content: center;
    border-bottom-left-radius: 8rpx;
  }

  &__loading {
    position: absolute;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.3);
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

.remark {
  width: 100%;
  min-height: 120rpx;
  padding: 16rpx;
  background-color: #f5f7fa;
  border-radius: 12rpx;
  font-size: 28rpx;
  box-sizing: border-box;
}

.footer {
  padding: 16rpx 24rpx;
  padding-bottom: calc(16rpx + env(safe-area-inset-bottom));
  background-color: #fff;
  box-shadow: 0 -2rpx 8rpx rgba(0, 0, 0, 0.04);
}
</style>
