<template>
  <view class="page">
    <view v-if="notice" class="detail">
      <view class="detail__title">{{ notice.NOTITLE || '—' }}</view>
      <view class="detail__meta">
        <text class="detail__date">发文日期：{{ formatDate(notice.BILLDATE) }}</text>
      </view>
      <view class="detail__divider"></view>

      <!-- 公告正文（Word 粘贴的富文本，含 o:p / mso 等非标准标签） -->
      <!-- #ifdef H5 -->
      <view class="detail__content" v-html="notice.NOCONTENT || '<p>暂无内容</p>'"></view>
      <!-- #endif -->
      <!-- #ifndef H5 -->
      <rich-text class="detail__content" :nodes="notice.NOCONTENT || '<p>暂无内容</p>'"></rich-text>
      <!-- #endif -->

      <!-- 审批信息 -->
      <view v-if="notice.CHECKER || notice.VERIFIER" class="approve-info">
        <view class="approve-info__title">审批信息</view>
        <view v-if="notice.CHECKER" class="approve-info__row">
          <text>审核人：{{ notice.CHECKER }}</text>
          <text v-if="notice.CHECKDATE">{{ formatDate(notice.CHECKDATE) }}</text>
        </view>
        <view v-if="notice.VERIFIER" class="approve-info__row">
          <text>审批人：{{ notice.VERIFIER }}</text>
          <text v-if="notice.VERIFYDATE">{{ formatDate(notice.VERIFYDATE) }}</text>
        </view>
      </view>

      <!-- 附件 -->
      <view v-if="attachments.length" class="attachments">
        <view class="attachments__title">附件</view>
        <view v-for="(att, idx) in attachments" :key="idx" class="attachments__item" @click="openAttachment(att)">
          <text class="attachments__icon">📎</text>
          <text class="attachments__name">{{ att.NAME || att.FILENAME || '附件' + (idx + 1) }}</text>
        </view>
      </view>
    </view>
    <view v-else-if="loading" class="loading-tip">加载中...</view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getNoticeDetail } from '@/api/home'
import { formatDate } from '@/utils/format'

const noticeId = ref('')
const notice = ref(null)
const loading = ref(false)
const attachments = ref([])

onLoad((options) => {
  noticeId.value = options.id
  loadDetail()
})

async function loadDetail() {
  loading.value = true
  try {
    // RS_M08/A02 返回 { MAIN:[公告记录], DTS:[附件] }
    const res = await getNoticeDetail(noticeId.value)
    notice.value = (res?.MAIN && res.MAIN[0]) || (res?.main && res.main[0]) || res || {}
    attachments.value = res?.DTS || res?.dts || []
  } catch (e) {} finally {
    loading.value = false
  }
}

function openAttachment(att) {
  // TODO: 接入附件下载/预览
  uni.showToast({ title: '附件预览待接入', icon: 'none' })
}
</script>

<style lang="scss" scoped>
.page { min-height: 100vh; background-color: #fff; }
.detail { padding: 32rpx;
  &__title { font-size: 40rpx; font-weight: 700; color: #1a1a1a; line-height: 1.4; }
  &__meta { margin-top: 16rpx; }
  &__date { font-size: 24rpx; color: #909399; }
  &__divider { height: 1rpx; background-color: #f0f0f0; margin: 24rpx 0; }
  &__content {
    font-size: 30rpx;
    line-height: 1.8;
    color: #333;
    word-break: break-word;
    overflow-wrap: break-word;
    :deep(p) { margin: 0.6em 0; }
    :deep(img) { max-width: 100%; height: auto; }
    :deep(table) { max-width: 100%; border-collapse: collapse; }
  }
}
.approve-info {
  margin-top: 40rpx; padding: 24rpx; background-color: #f5f7fa; border-radius: 12rpx;
  &__title { font-size: 28rpx; font-weight: 600; color: #333; margin-bottom: 16rpx; }
  &__row { display: flex; justify-content: space-between; font-size: 26rpx; color: #666; padding: 6rpx 0; }
}
.attachments {
  margin-top: 32rpx;
  &__title { font-size: 28rpx; font-weight: 600; color: #333; margin-bottom: 16rpx; }
  &__item { display: flex; align-items: center; padding: 20rpx; background-color: #f5f7fa; border-radius: 12rpx; margin-bottom: 12rpx; }
  &__icon { font-size: 32rpx; margin-right: 16rpx; }
  &__name { font-size: 28rpx; color: #2f7df6; }
}
.loading-tip { text-align: center; padding: 120rpx 0; font-size: 28rpx; color: #909399; }
</style>
