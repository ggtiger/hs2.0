<template>
  <view class="page">
    <scroll-view scroll-y class="content">
      <view class="content-inner">
      <!-- 状态横幅 -->
      <view class="banner" :style="{ background: stateInfo.bg }">
        <view class="banner__state">{{ stateInfo.label }}</view>
        <view class="banner__no">{{ record.REFBILLCODE || record.BILLCODE || record.DOCCODE || record.ID || '—' }}</view>
      </view>

      <!-- 原始记录模板（按模板布局顺序逐节点渲染） -->
      <view v-if="tplNodes.length">
        <record-template :nodes="tplNodes" />
      </view>

      <!-- 非模板模块的基本信息（委托/受理/费用等，无 TPMDATA） -->
      <view v-if="!tplNodes.length" class="card">
        <view class="card__title">📋 基本信息</view>
        <view v-if="record.DOCTITLE" class="field field--bold"><text class="field__value">{{ record.DOCTITLE }}</text></view>
        <view class="field"><text class="field__label">委托单位</text><text class="field__value">{{ record.CUSTNAME || '—' }}</text></view>
        <view class="field"><text class="field__label">联系人</text><text class="field__value">{{ record.LINKER || '—' }}</text></view>
        <view class="field"><text class="field__label">设备名称</text><text class="field__value">{{ record.MNAME || '—' }}</text></view>
        <view class="field"><text class="field__label">规格型号</text><text class="field__value">{{ record.SIZETYPE || '—' }}</text></view>
        <view class="field"><text class="field__label">出厂编号</text><text class="field__value">{{ record.OPCODE || '—' }}</text></view>
        <view class="field"><text class="field__label">生产厂家</text><text class="field__value">{{ record.MANUFACTURER || '—' }}</text></view>
        <view class="field"><text class="field__label">检定规程</text><text class="field__value">{{ record.REGUITEMNAME || '—' }}</text></view>
        <view class="field"><text class="field__label">检校日期</text><text class="field__value">{{ formatDate(record.BILLDATE) }}</text></view>
      </view>

      <!-- 附件（DTSD） -->
      <view v-if="attachments.length" class="card">
        <view class="card__title">📎 附件</view>
        <!-- 图片/视频缩略图，一行三个 -->
        <view v-if="mediaAtts.length" class="att-grid">
          <view v-for="(att, idx) in mediaAtts" :key="att._idx" class="att-grid__item" @click="previewMedia(att._idx)">
            <image v-if="isImg(att.FILENAME)" :src="att._url" class="att-grid__thumb" mode="aspectFill" />
            <view v-else-if="isVideo(att.FILENAME)" class="att-grid__video">
              <text class="att-grid__play">▶</text>
            </view>
          </view>
        </view>
        <!-- 非图片视频，列表显示 -->
        <view v-if="fileAtts.length">
          <view v-for="(att, idx) in fileAtts" :key="idx" class="att-item" @click="previewPdf(att.FILEID)">
            <text class="att-item__icon">{{ attIcon(att.FILENAME) }}</text>
            <text class="att-item__name">{{ att.FILENAME || ('附件' + (idx + 1)) }}</text>
            <text class="att-item__arrow">›</text>
          </view>
        </view>
      </view>

      <!-- 物流信息（R02_M07，REF_ID 关联当前记录） -->
      <view v-if="logisticsList.length" class="card">
        <view class="card__title">🚚 物流信息</view>
        <view v-for="(log, idx) in logisticsList" :key="idx" class="log-item">
          <view class="log-item__head">
            <text class="log-item__company">{{ log.LOGISTICS_COMPANY || '—' }}</text>
            <text class="log-item__state" :class="logisticsStatusStyle(log.STATUS)">{{ logisticsStatusLabel(log.STATUS) }}</text>
          </view>
          <view class="log-item__row">
            <text class="log-item__label">物流单号</text>
            <text class="log-item__value">{{ log.LOGISTICS_NO || '—' }}</text>
          </view>
          <view class="log-item__row">
            <text class="log-item__label">类型</text>
            <text class="log-item__value">{{ log.REF_TYPE === '1' || log.REF_TYPE === 1 ? '样品' : '证书' }}</text>
          </view>
          <view v-if="log.SEND_DATE" class="log-item__row">
            <text class="log-item__label">寄出日期</text>
            <text class="log-item__value">{{ formatDate(log.SEND_DATE) }}</text>
          </view>
          <view v-if="log.RECEIVE_NAME" class="log-item__row">
            <text class="log-item__label">收件人</text>
            <text class="log-item__value">{{ log.RECEIVE_NAME }}{{ log.RECEIVE_PHONE ? ' ' + log.RECEIVE_PHONE : '' }}</text>
          </view>
          <!-- 物流节点照片 -->
          <view v-if="log.photos && log.photos.length" class="log-photos">
            <image
              v-for="(url, pi) in log.photos"
              :key="pi"
              :src="url"
              class="log-photos__img"
              mode="aspectFill"
              @click="previewLogPhoto(log.photos, pi)"
            />
          </view>
        </view>
      </view>

      <!-- 审批记录（DTSC，对齐 PC 端 attach-flow-panel 时间轴） -->
      <view v-if="flowLogs.length" class="card">
        <view class="card__title">📋 审批记录</view>
        <view class="flow">
          <view v-for="(log, idx) in flowLogs" :key="idx" class="flow__item">
            <view class="flow__dot" :class="flowDotClass(log.STATE)"></view>
            <view v-if="idx < flowLogs.length - 1" class="flow__line"></view>
            <view class="flow__body">
              <view class="flow__head">
                <text class="flow__user">{{ log.OPLOGER }}</text>
                <text class="flow__state" :class="flowStateClass(log.STATE)">{{ log.STATE }}</text>
              </view>
              <text class="flow__time">{{ log.OPLOGDATE }}</text>
              <view v-if="log.REMARK" class="flow__remark">
                <text>{{ log.REMARK }}</text>
              </view>
            </view>
          </view>
        </view>
      </view>

      <!-- 审批意见 -->
      <view v-if="!isReadonly" class="card">
        <view class="card__title">✍️ {{ opinionLabel }}</view>
        <textarea
          v-model="remark"
          class="opinion"
          :placeholder="opinionPlaceholder"
          maxlength="500"
        />
        <view class="opinion__count">{{ remark.length }}/500</view>
      </view>

      <!-- 复核通过：选择下一审批人 -->
      <view v-if="needNextApprover && !isReadonly" class="card">
        <view class="card__title">👥 选择下一审批人</view>
        <view class="approver">
          <input
            v-model="nextApprover"
            class="approver__input"
            placeholder="点击选择下一审批人"
            disabled
            @click="showPicker = true"
          />
        </view>
      </view>

      <view style="height: 200rpx"></view>
      </view>
    </scroll-view>

    <!-- 审批人选择器 -->
    <approver-picker
      :show="showPicker"
      @close="showPicker = false"
      @pick="onPickApprover"
    />

    <!-- 底部操作栏 -->
    <approve-bar
      v-if="!isReadonly"
      :approve-text="approveText"
      :reject-text="rejectText"
      @approve="onApprove"
      @reject="onReject"
    />
  </view>
</template>

<script setup>
import { ref, computed } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getRecordDetail, doCheck, doVerify, doReject } from '@/api/approve'
import { getStateInfo } from '@/utils/state'
import { formatDate } from '@/utils/format'
import { previewPdf } from '@/utils/pdf'
import { DATA_BASE_URL } from '@/utils/config'
import { query, call } from '@/api/db'

const recordId = ref('')
const type = ref('check') // check | verify | sign
const moduleCode = ref('LI_M02')
const record = ref({})
const attachments = ref([])
const flowLogs = ref([])
const logisticsList = ref([])
const tplNodes = ref([])
const remark = ref('')
const nextApprover = ref('')
const nextApproverId = ref('')
const showPicker = ref(false)

// 附件分类：图片/视频 vs 其他文件
const IMG_EXTS = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp']
const VIDEO_EXTS = ['mp4', 'webm', 'ogg', 'mov', 'avi']

function fileExt(name) {
  if (!name) return ''
  const parts = name.split('.')
  return parts.length > 1 ? parts.pop().toLowerCase() : ''
}

function isImg(name) { return IMG_EXTS.includes(fileExt(name)) }
function isVideo(name) { return VIDEO_EXTS.includes(fileExt(name)) }
function isMedia(name) { return isImg(name) || isVideo(name) }

// 构建附件的完整 URL
function attUrl(fileId) {
  // #ifdef H5
  return `/api/file/${fileId}`
  // #endif
  // #ifndef H5
  return `${DATA_BASE_URL}/api/file/${fileId}`
  // #endif
}

// 图片/视频附件列表（带 _idx 原始索引和 _url）
const mediaAtts = computed(() =>
  attachments.value
    .filter(a => isMedia(a.FILENAME))
    .map(a => ({ ...a, _idx: attachments.value.indexOf(a), _url: attUrl(a.FILEID) }))
)

// 非图片视频附件列表
const fileAtts = computed(() =>
  attachments.value.filter(a => !isMedia(a.FILENAME))
)

// 图片 URL 列表（给 uni.previewImage 用）
const imgUrls = computed(() => mediaAtts.value.filter(a => isImg(a.FILENAME)).map(a => a._url))

// 点击图片/视频预览
function previewMedia(originalIdx) {
  const att = attachments.value[originalIdx]
  if (isVideo(att.FILENAME)) {
    // 视频：单独打开
    const url = attUrl(att.FILEID)
    // #ifdef H5
    window.open(url, '_blank')
    return
    // #endif
    // #ifndef H5
    uni.showLoading({ title: '加载中', mask: true })
    uni.downloadFile({
      url,
      success: (res) => {
        uni.hideLoading()
        if (res.statusCode === 200) {
          uni.openDocument({ filePath: res.tempFilePath, showMenu: true })
        }
      },
      fail: () => { uni.hideLoading(); uni.showToast({ title: '打开失败', icon: 'none' }) }
    })
    return
    // #endif
  }
  // 图片：用 uni.previewImage 左右切换
  const pos = imgUrls.value.indexOf(attUrl(att.FILEID))
  uni.previewImage({
    urls: imgUrls.value,
    current: pos >= 0 ? imgUrls.value[pos] : imgUrls.value[0]
  })
}

const stateInfo = computed(() => {
  const info = getStateInfo(record.value.STATE)
  return { ...info, bg: info.color }
})

// 只读模式：查询入口跳转时（type=view）隐藏审批操作
const isReadonly = computed(() => type.value === 'view')

const needNextApprover = computed(() => type.value === 'check')

const approveText = computed(() => ({
  check: '复核通过',
  verify: '审批通过',
  sign: '签发'
}[type.value] || '通过'))

const rejectText = computed(() => ({
  check: '复核驳回',
  verify: '审批驳回'
}[type.value] || '驳回'))

const opinionLabel = computed(() => type.value === 'check' ? '复核意见' : '审批意见')
const opinionPlaceholder = computed(() => `请输入${opinionLabel.value}（驳回时必填）`)

onLoad((options) => {
  recordId.value = options.id
  type.value = options.type || 'check'
  moduleCode.value = options.module || 'LI_M02'
  // 动态导航栏标题：查看时按模块，审批时按动作
  const titleMap = { LI_M02: '原始记录详情', LI_M06: '委托详情', LI_M00: '受理详情', LI_M03: '费用详情' }
  const title = type.value === 'view'
    ? (titleMap[moduleCode.value] || '记录详情')
    : ({ check: '待审核', verify: '待审批', sign: '待签发' }[type.value] || '审批详情')
  uni.setNavigationBarTitle({ title })
  loadDetail()
})

async function loadDetail() {
  try {
    // A02 返回 { MAIN:[主记录], DTSA:[标准器], DTSD:[附件], DTSC:[日志], DTSB:[测量数据] }
    const res = await getRecordDetail(recordId.value, moduleCode.value)
    const main = (res?.MAIN && res.MAIN[0]) || (res?.main && res.main[0]) || {}
    record.value = main
    attachments.value = res?.DTSD || []
    flowLogs.value = res?.DTSC || []

    // 查询关联物流信息（R02_M07，REF_ID = 当前记录ID）
    loadLogistics()

    // 原始记录模板渲染（完整复刻 PC 端 dealTreeData + DTSB/DTSA 数据绑定）
    if (moduleCode.value === 'LI_M02' && main.TPMDATA) {
      try {
        const tpmTree = typeof main.TPMDATA === 'string' ? JSON.parse(main.TPMDATA) : main.TPMDATA
        const dtsb = res?.DTSB || []
        const dtsa = res?.DTSA || []

        // Step 1: dealTreeData — 递归遍历模板树，将 MAIN 字段值写入节点的 value/label
        // PC 端逻辑：if(n.field) { n.value = MAIN.getValue(n.field) || n.dvalue; n.label = n.value || n.label; }
        const mainFields = Object.keys(main)
        function dealTreeData(nodes) {
          if (!Array.isArray(nodes)) return
          nodes.forEach((n) => {
            if (n.field) {
              // 优先从 MAIN 取值，否则用 dvalue 兜底
              const mainVal = mainFields.indexOf(n.field) !== -1 ? main[n.field] : undefined
              n.value = mainVal !== undefined && mainVal !== null ? mainVal : (n.dvalue || '')
              // PC 端 itemLabel 显示逻辑：n.label = n.value || n.label
              if (n.type === 'itemLabel') {
                n.label = n.value || n.label
              }
            }
            if (n.children && n.children.length > 0) {
              dealTreeData(n.children)
            }
          })
        }
        dealTreeData(tpmTree)

        // Step 2: 构建 inputObj/tableObj/editorObj 索引（同 PC 端 dealConfigSelect）
        const inputObj = {}
        const tableObj = {}
        const editorObj = []
        function dealConfigSelect(nodes) {
          if (!Array.isArray(nodes)) return
          nodes.forEach((n) => {
            if (n.path) { n.fieldProps = n.fieldProps || {} }
            if (n.field) { inputObj[n.field] = n }
            if (n.sourceName) { tableObj[n.sourceName] = n; n.value = n.value || [] }
            if (n.type === 'itemEditor') {
              // 初始化 fields 数组（PC 端 dealConfigSelect 逻辑）
              if (!n.fields) n.fields = []
              editorObj.push(n)
            }
            // 递归处理子节点（PC 端用 path，fields 嵌套在 children 中）
            if (n.children && n.children.length > 0) { dealConfigSelect(n.children) }
            // itemEditor 的 fields 中可能有嵌套的 field 定义
            if (n.fields && Array.isArray(n.fields)) {
              n.fields.forEach((f) => {
                if (f.field) { inputObj[f.field] = f }
              })
            }
          })
        }
        dealConfigSelect(tpmTree)

        // Step 3: 补充 DTSB 数据到 inputObj（非 MAIN 字段从 DTSB 取值）
        // PC 端逻辑：if(fields.indexOf(k)===-1 && DTSB) { tt=DTSB.find(d=>d.FIELDNAME===k); inputObj[k].value=tt.FIELDVALUE }
        Object.keys(inputObj).forEach((k) => {
          if (mainFields.indexOf(k) === -1) {
            const tt = dtsb.find((d) => d.FIELDNAME === k)
            if (tt) {
              inputObj[k].value = tt.FIELDVALUE
              inputObj[k].name = tt.FIELDREMARK
              inputObj[k].field = tt.FIELDNAME
              // itemLabel 类型也更新 label
              if (inputObj[k].type === 'itemLabel') {
                inputObj[k].label = tt.FIELDVALUE || inputObj[k].label
              }
            }
          }
        })

        // Step 4: 补充 DTSB 数据到 editorObj 的 fields
        editorObj.forEach((p) => {
          (p.fields || []).forEach((f) => {
            const tt = dtsb.find((d) => d.FIELDNAME === f.field)
            if (tt) {
              f.value = tt.FIELDVALUE
              f.name = tt.FIELDREMARK
              f.field = tt.FIELDNAME
            }
          })
        })

        // Step 5: 补充 DTSA 数据到 tableObj（标准器子表行）
        if (dtsa.length > 0) {
          Object.values(tableObj).forEach((t) => {
            if (!Array.isArray(t.value)) t.value = []
            dtsa.forEach((ditem) => {
              t.value.push({
                ID: ditem.ARDID, ARDNAME: ditem.ARDNAME,
                SIZETYPE: ditem.SIZETYPE, OMCODE: ditem.OMCODE,
                DEGREE: ditem.DEGREE, CERTCODE: ditem.CERTCODE,
                EXPDATE: ditem.EXPDATE, CORGNAME: ditem.CORGNAME
              })
            })
          })
        }

        tplNodes.value = tpmTree
      } catch (e) {
        console.warn('TPMDATA 模板渲染失败', e)
        tplNodes.value = []
      }
    } else {
      tplNodes.value = []
    }
  } catch (e) {
    // 请求层已提示
  }
}

function onPickApprover({ id, name }) {
  nextApproverId.value = id
  nextApprover.value = name
}

// 附件图标（对齐 PC 端 getIcon）
function attIcon(name) {
  if (!name) return '📄'
  const ext = name.split('.').pop().toLowerCase()
  const map = { pdf: '📕', doc: '📘', docx: '📘', xls: '📗', xlsx: '📗', ppt: '📙', pptx: '📙', zip: '📦', rar: '📦', jpg: '🖼️', jpeg: '🖼️', png: '🖼️', gif: '🖼️' }
  return map[ext] || '📄'
}

// 审批记录圆点样式（对齐 PC 端 getDotClass）
function flowDotClass(state) {
  const m = { '已提交': 'dot-blue', '已审核': 'dot-green', '已审批': 'dot-green', '已驳回': 'dot-red', '已签发': 'dot-primary', '已作废': 'dot-gray' }
  return m[state] || 'dot-blue'
}

// 审批状态标签样式（对齐 PC 端 getStateClass）
function flowStateClass(state) {
  const m = { '已提交': 'st-blue', '已审核': 'st-green', '已审批': 'st-green', '已驳回': 'st-red', '已签发': 'st-primary', '已作废': 'st-gray' }
  return m[state] || 'st-blue'
}

// ─── 物流信息查询 ───
async function loadLogistics() {
  if (!recordId.value) return
  try {
    // 先查物流列表（F01 用 INPUT 模糊搜索，这里用 REF_ID 精确匹配）
    const res = await query('R02_M07', { REF_ID: recordId.value }, { pageSize: 50 })
    const list = res.list || []

    // 对每条物流查 A02 获取子表（物流节点，含 NODE_IMAGE 照片）
    const withNodes = await Promise.all(list.map(async (log) => {
      try {
        const detail = await call('R02_M07', 'A02', { FilterParams: { ID: log.ID } })
        const nodes = detail?.DTS || []
        // 从节点中提取有图片的记录
        const photos = nodes
          .filter(n => n.NODE_IMAGE)
          .map(n => fileUrl(n.NODE_IMAGE))
        return { ...log, photos, nodes }
      } catch (e) {
        return { ...log, photos: [], nodes: [] }
      }
    }))
    logisticsList.value = withNodes
  } catch (e) {
    logisticsList.value = []
  }
}

// 构造文件访问 URL（对齐 PC 端 db.getUrl('upload') + fileId）
function fileUrl(fileId) {
  if (!fileId) return ''
  // #ifdef H5
  return `/api/file/${fileId}`
  // #endif
  // #ifndef H5
  return `${DATA_BASE_URL}/api/file/${fileId}`
  // #endif
}

// 物流状态映射
const logisticsStatusMap = { 0: '待寄送', 1: '已寄送', 2: '运输中', 3: '已签收' }
const logisticsStatusColor = { 0: 'st-gray', 1: 'st-blue', 2: 'st-primary', 3: 'st-green' }

function logisticsStatusLabel(status) {
  return logisticsStatusMap[status] || '未知'
}

function logisticsStatusStyle(status) {
  return logisticsStatusColor[status] || 'st-gray'
}

// 预览物流节点照片
function previewLogPhoto(photos, idx) {
  uni.previewImage({ urls: photos, current: photos[idx] })
}

async function onApprove() {
  if (needNextApprover.value && !nextApprover.value) {
    return uni.showToast({ title: '请选择下一审批人', icon: 'none' })
  }
  const params = {
    ID: recordId.value,
    REMARK: remark.value,
    NEXTAPRID: nextApproverId.value,
    NEXTAPRER: nextApprover.value
  }
  const action = type.value === 'check' ? doCheck : doVerify
  uni.showModal({
    title: '确认操作',
    content: `确定${approveText.value}？`,
    success: async (res) => {
      if (!res.confirm) return
      try {
        await action(params)
        setTimeout(() => uni.navigateBack(), 800)
      } catch (e) {}
    }
  })
}

async function onReject() {
  if (!remark.value.trim()) {
    return uni.showToast({ title: '驳回请填写意见', icon: 'none' })
  }
  uni.showModal({
    title: '确认驳回',
    content: `确定驳回此记录？`,
    success: async (res) => {
      if (!res.confirm) return
      try {
        await doReject({ ID: recordId.value, REMARK: remark.value })
        setTimeout(() => uni.navigateBack(), 800)
      } catch (e) {}
    }
  })
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
  overflow: hidden;
}

.content-inner {
  padding: 24rpx;
}

.banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 32rpx;
  border-radius: 16rpx;
  color: #fff;
  margin-bottom: 24rpx;

  &__state {
    font-size: 32rpx;
    font-weight: 600;
  }

  &__no {
    font-size: 26rpx;
    opacity: 0.95;
  }
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

  &__empty {
    text-align: center;
    padding: 24rpx;
    font-size: 26rpx;
    color: #c0c4cc;
  }

  &--link {
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
}

.att-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
  margin-bottom: 16rpx;

  &__item {
    width: calc((100% - 24rpx) / 3);
    aspect-ratio: 1;
    border-radius: 12rpx;
    overflow: hidden;
    background-color: #f5f5f5;
  }

  &__thumb {
    width: 100%;
    height: 100%;
  }

  &__video {
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #e8e8e8;
  }

  &__play {
    font-size: 48rpx;
    color: rgba(255, 255, 255, 0.9);
    text-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.3);
  }
}

.att-item {
  display: flex;
  align-items: center;
  padding: 20rpx 0;
  border-bottom: 1rpx solid #f5f5f5;

  &:last-child {
    border-bottom: none;
  }

  &__icon {
    font-size: 32rpx;
    margin-right: 16rpx;
  }

  &__name {
    flex: 1;
    font-size: 28rpx;
    color: #2f7df6;
  }

  &__arrow {
    font-size: 32rpx;
    color: #c0c4cc;
  }
}

.field {
  display: flex;
  padding: 12rpx 0;
  font-size: 28rpx;

  &--bold {
    padding-bottom: 20rpx;
    border-bottom: 1rpx solid #f0f0f0;
    margin-bottom: 8rpx;

    .field__value {
      font-size: 32rpx;
      font-weight: 600;
      color: #1a1a1a;
    }
  }

  &__label {
    width: 160rpx;
    color: #909399;
    flex-shrink: 0;
  }

  &__value {
    flex: 1;
    color: #333;
  }
}

.std-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16rpx 0;
  border-bottom: 1rpx solid #f5f5f5;
  font-size: 28rpx;

  &:last-child {
    border-bottom: none;
  }

  &__name {
    color: #333;
  }

  &__date {
    color: #909399;
    font-size: 24rpx;
  }
}

.opinion {
  width: 100%;
  height: 160rpx;
  padding: 16rpx;
  background-color: #f5f7fa;
  border-radius: 12rpx;
  font-size: 28rpx;
  box-sizing: border-box;

  &__count {
    text-align: right;
    font-size: 22rpx;
    color: #c0c4cc;
    margin-top: 8rpx;
  }
}

.approver__input {
  height: 80rpx;
  padding: 0 24rpx;
  background-color: #f5f7fa;
  border-radius: 12rpx;
  font-size: 28rpx;
}

// 审批记录时间轴（对齐 PC 端 attach-flow-panel）
.flow {
  padding: 8rpx 0;

  &__item {
    position: relative;
    padding-left: 40rpx;
    padding-bottom: 32rpx;

    &:last-child {
      padding-bottom: 0;
    }
  }

  &__dot {
    position: absolute;
    left: 0;
    top: 8rpx;
    width: 20rpx;
    height: 20rpx;
    border-radius: 50%;
    z-index: 1;

    &.dot-blue { background-color: #2F54EB; }
    &.dot-green { background-color: #52C41A; }
    &.dot-red { background-color: #F5222D; }
    &.dot-primary { background-color: #597EF7; }
    &.dot-gray { background-color: #BFBFBF; }
  }

  &__line {
    position: absolute;
    left: 9rpx;
    top: 32rpx;
    bottom: 0;
    width: 2rpx;
    background-color: #f0f0f0;
  }

  &__body {
    min-width: 0;
  }

  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  &__user {
    font-weight: 600;
    color: #1a1a1a;
    font-size: 28rpx;
  }

  &__state {
    font-size: 22rpx;
    font-weight: 500;
    padding: 2rpx 16rpx;
    border-radius: 20rpx;

    &.st-blue { color: #2F54EB; background-color: #F0F5FF; }
    &.st-green { color: #52C41A; background-color: #F6FFED; }
    &.st-red { color: #F5222D; background-color: #FFF1F0; }
    &.st-primary { color: #597EF7; background-color: #F0F5FF; }
    &.st-gray { color: #8C8C8C; background-color: #FAFAFA; }
  }

  &__time {
    color: #8c8c8c;
    font-size: 24rpx;
    margin-top: 4rpx;
  }

  &__remark {
    color: #434343;
    margin-top: 12rpx;
    font-size: 26rpx;
    background-color: #f5f5f5;
    padding: 12rpx 16rpx;
    border-radius: 12rpx;
    border-left: 6rpx solid #2F54EB;
  }
}

.log-item {
  padding: 16rpx 0;
  border-bottom: 1rpx solid #f0f0f0;

  &:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }

  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8rpx;
  }

  &__company {
    font-size: 28rpx;
    font-weight: 600;
    color: #1a1a1a;
  }

  &__state {
    font-size: 22rpx;
    font-weight: 500;
    padding: 2rpx 16rpx;
    border-radius: 20rpx;

    &.st-green { color: #52C41A; background-color: #F6FFED; }
    &.st-blue { color: #2F54EB; background-color: #F0F5FF; }
    &.st-primary { color: #597EF7; background-color: #F0F5FF; }
    &.st-gray { color: #8C8C8C; background-color: #FAFAFA; }
  }

  &__row {
    display: flex;
    font-size: 26rpx;
    padding: 4rpx 0;
  }

  &__label {
    width: 140rpx;
    color: #999;
    text-align: right;
    margin-right: 12rpx;
  }

  &__value {
    flex: 1;
    color: #333;
  }
}

.log-photos {
  display: flex;
  flex-wrap: wrap;
  gap: 12rpx;
  margin-top: 8rpx;

  &__img {
    width: 120rpx;
    height: 120rpx;
    border-radius: 8rpx;
  }
}
</style>
