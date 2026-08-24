<template>
  <!-- itemLabel：纯文本/已赋值的字段文本 -->
  <view v-if="node.type === 'itemLabel'" class="tn-label" :style="boxStyle">
    <text :style="textStyle">{{ node.label || node.value || '' }}</text>
  </view>

  <!-- itemLayout：栅格布局行
       PC 端多列栅格（cell=2, cols={0:14,1:10}），移动端强制单列，所有子节点垂直排列
  -->
  <view v-else-if="node.type === 'itemLayout'" class="tn-layout" :style="boxStyle">
    <view class="tn-layout__col tn-layout__col--full">
      <template v-for="(ci) in layoutCellCount" :key="ci">
        <tpl-node
          v-for="(child, ki) in cellChildren(ci - 1)"
          :key="ci + '-' + ki"
          :node="child"
        />
      </template>
    </view>
  </view>

  <!-- itemField：标签 + 字段值（移动端只读，固定 100% 宽度，不继承 PC 端宽度）
       PC 端 itemField 内嵌 <itemLabel v-bind="labelProps">，标签文本在 labelProps.label
  -->
  <view v-else-if="node.type === 'itemField'" class="tn-field">
    <view class="tn-field__inner">
      <text v-if="fieldLabel" class="tn-field__label" :style="labelTextStyle">{{ fieldLabel }}</text>
      <text class="tn-field__value" :style="fieldTextStyle">{{ node.value || '' }}</text>
    </view>
  </view>

  <!-- itemCheckBox -->
  <view v-else-if="node.type === 'itemCheckBox'" class="tn-checkbox" :style="boxStyle">
    <text :style="textStyle">{{ node.label || '' }} {{ node.value ? '☑' : '☐' }}</text>
  </view>

  <!-- itemTable：子表数据表格（独立区域，支持水平滚动，列标题用中文名）
       PC 端用原生 <table>，移动端 H5 也用 <table> 保证表头与数据列宽一致
  -->
  <view v-else-if="node.type === 'itemTable'" class="tn-table">
    <scroll-view v-if="tableRows.length" scroll-x class="tn-table__scroll">
      <!-- #ifdef H5 -->
      <view class="tn-table__html" v-html="tableHtml"></view>
      <!-- #endif -->
      <!-- #ifndef H5 -->
      <view class="tn-table__html">
        <rich-text :nodes="tableHtml"></rich-text>
      </view>
      <!-- #endif -->
    </scroll-view>
    <view v-else class="tn-table__empty">
      <text class="text-xs text-gray-400">暂无数据</text>
    </view>
  </view>

  <!-- itemEditor：HTML 表格（PC 端用 UEditor/WangEditor，移动端只读渲染替换后的 HTML，支持水平滚动） -->
  <view v-else-if="node.type === 'itemEditor'" class="tn-editor">
    <scroll-view v-if="editorHtml" scroll-x class="tn-editor__scroll">
      <view class="tn-editor__inner">
        <!-- #ifdef H5 -->
        <view class="tn-editor__html" v-html="editorHtml"></view>
        <!-- #endif -->
        <!-- #ifndef H5 -->
        <uv-parse :content="editorHtml"></uv-parse>
        <!-- #endif -->
      </view>
    </scroll-view>
  </view>
</template>

<script setup>
/**
 * 模板递归渲染节点
 * 完整复刻 PC 端 rs-edit-item 六种组件类型：
 * itemLayout → 栅格行（cell=列数, cols={0:12,1:12} → 百分比宽度）
 * itemLabel  → 文本（label 或 value，dealTreeData 已将字段值写入 label）
 * itemField  → 标签+值（label 来自 labelProps.label，对齐 PC 端 <itemLabel v-bind="labelProps">）
 * itemTable  → 原生 <table> 渲染（表头数据行列宽一致），sourceName → C00/A02 获取中文名
 * itemEditor → 富文本（scroll-view 水平滚动，max-width 2048rpx）
 * itemCheckBox → 复选框
 */
import { computed, ref, onMounted } from 'vue'
import { postData } from '@/utils/request'

const props = defineProps({
  node: { type: Object, default: () => ({}) }
})

// 通用盒模型样式（移动端不设 height，让内容自动撑高，避免压扁重叠）
const boxStyle = computed(() => {
  const s = {}
  if (props.node.width && props.node.width !== 'auto') s.width = props.node.width
  return s
})

// 文本样式（字号/对齐/加粗，对齐 PC 端 rr-text-* / rr-f* / rr-weight）
const textStyle = computed(() => {
  const s = {}
  const sz = props.node.size || 12
  s.fontSize = sz * 2 + 'rpx'
  if (props.node.align) s.textAlign = props.node.align
  if (props.node.weight) s.fontWeight = 'bold'
  return s
})

// ─── itemField ───
// PC 端 itemField 内嵌 <itemLabel v-bind="labelProps">
// labelProps = { label: '标签文本', size, weight, width, align }
// 标签文本优先取 labelProps.label，其次取 node.label
const fieldLabel = computed(() => {
  return props.node.labelProps?.label || props.node.label || ''
})

// itemField 标签样式（来自 labelProps，对齐 PC 端 itemLabel 的 props）
const labelTextStyle = computed(() => {
  const lp = props.node.labelProps || {}
  const s = {}
  s.fontSize = (lp.size || props.node.size || 12) * 2 + 'rpx'
  if (lp.weight) s.fontWeight = 'bold'
  // 移动端标签强制右对齐，不继承 PC 端 labelProps.align
  return s
})

// itemField 值样式（来自 fieldProps，对齐 PC 端 itemField 的输入框样式）
const fieldTextStyle = computed(() => {
  const fp = props.node.fieldProps || {}
  const s = {}
  s.fontSize = (fp.size || props.node.size || 12) * 2 + 'rpx'
  if (fp.weight) s.fontWeight = 'bold'
  // 移动端值区域强制左对齐，不继承 PC 端 fieldProps.align
  return s
})

// ─── itemLayout ───
// 移动端强制单列：忽略 PC 端多列栅格，所有子节点垂直排列
const layoutCellCount = computed(() => props.node.cell || 1)

/**
 * 获取 cell 槽位的子节点
 * PC 端逻辑（rs-edit-item）：
 * - cell=1：所有 children 都在同一个槽位
 * - cell>1：children[idx] 对应槽位 idx（children[idx] 本身可能是个 layout 节点）
 */
function cellChildren(cellIndex) {
  const children = props.node.children || []
  const cell = props.node.cell || 1
  if (cell === 1) {
    return children
  }
  const child = children[cellIndex]
  if (!child) return []
  if (child.children && child.children.length > 0) {
    return child.children
  }
  return [child]
}

// ─── itemTable ───
// PC 端用原生 <table> 渲染（table/index.vue），表头数据行在同一个 table 中列宽自然对齐
// 移动端同样用原生 <table> HTML，通过 v-html / rich-text 渲染

/** 字段元数据缓存（sourceName → [{RESFIELDNAME, LABELNAME, ...}]），跨组件实例共享 */
const scmsCache = {}

// itemTable 列定义（优先用后端元数据的中文名，降级用数据行 key）
const tableCols = ref([])

// itemTable 数据行
const tableRows = computed(() => {
  const v = props.node.value
  if (Array.isArray(v)) return v
  return []
})

/**
 * 将表格渲染为原生 <table> HTML（对齐 PC 端 table/index.vue）
 * 好处：表头和数据行在同一个 <table> 中，列宽自然对齐
 */
const tableHtml = computed(() => {
  const cols = tableCols.value
  const rows = tableRows.value
  if (!cols.length || !rows.length) return ''

  const colCount = cols.length
  const colWidth = (100 / colCount).toFixed(2) + '%'

  let html = '<table style="width:100%;border-top:1px solid #333;border-left:1px solid #333;border-spacing:0;">'

  // 表头行
  html += '<tr style="background-color:#f5f7fa;">'
  cols.forEach((col) => {
    html += `<td style="width:${colWidth};padding:5px 8px;border-bottom:1px solid #333;border-right:1px solid #333;font-size:12px;font-weight:600;color:#333;text-align:center;white-space:nowrap;">${col.title || col.key}</td>`
  })
  html += '</tr>'

  // 数据行
  rows.forEach((row) => {
    html += '<tr>'
    cols.forEach((col) => {
      const val = row[col.key] != null ? row[col.key] : ''
      html += `<td style="width:${colWidth};padding:5px 8px;border-bottom:1px solid #333;border-right:1px solid #333;font-size:12px;color:#333;text-align:center;word-break:break-all;">${val}</td>`
    })
    html += '</tr>'
  })

  // 底部字段名行（对齐 PC 端 table/index.vue 第 33-41 行：inLayout 时显示 column.key）
  html += '<tr>'
  cols.forEach((col) => {
    html += `<td style="width:${colWidth};padding:5px 8px;border-bottom:1px solid #333;border-right:1px solid #333;font-size:10px;color:#999;text-align:center;">${col.key}</td>`
  })
  html += '</tr>'

  html += '</table>'
  return html
})

/**
 * 加载 sourceName 对应的字段元数据，构建列定义
 * 对齐 PC 端 initScms + Gen.getTableColumns 逻辑
 */
async function loadTableColumns() {
  const sourceName = props.node.sourceName
  if (!sourceName || !tableRows.value.length) {
    tableCols.value = buildColsFromRows()
    return
  }

  if (scmsCache[sourceName]) {
    tableCols.value = buildColsFromScms(scmsCache[sourceName])
    return
  }

  try {
    const ret = await postData({
      api: '/api/outer/call/C00/A02/',
      params: {
        PageSize: 200,
        PageIndex: 1,
        FilterParams: { RESOURCENAMES: [sourceName] },
        OrderBy: 'RESOURCEID,ENTRYNUM'
      }
    }, 'url')

    if (ret && ret.Items && ret.Items.length > 0) {
      const items = ret.Items
      let mname = ''
      const titems = []
      items.forEach((item) => {
        if (item.RESOURCENAME !== mname) {
          if (titems.length > 0) { scmsCache[mname] = titems.splice(0) }
          mname = item.RESOURCENAME
        }
        titems.push(item)
      })
      if (titems.length > 0) { scmsCache[mname] = titems }

      if (scmsCache[sourceName]) {
        tableCols.value = buildColsFromScms(scmsCache[sourceName])
        return
      }
    }
    tableCols.value = buildColsFromRows()
  } catch (e) {
    console.warn('loadTableColumns failed:', e)
    tableCols.value = buildColsFromRows()
  }
}

/** 从后端元数据构建列定义（对齐 PC 端 Gen.getTableColumns） */
function buildColsFromScms(items) {
  const ordered = items.slice().sort((a, b) => {
    const la = +a.LISTSORT || 0
    const lb = +b.LISTSORT || 0
    if (la && lb) return la - lb
    if (la) return -1
    if (lb) return 1
    return (+a.ENTRYNUM || 0) - (+b.ENTRYNUM || 0)
  })
  return ordered.map((item) => ({
    key: item.RESFIELDNAME,
    title: item.LABELNAME || item.RESFIELDNAME
  }))
}

/** 降级：从数据行 key 推断列定义 */
function buildColsFromRows() {
  if (tableRows.value.length && tableRows.value[0]) {
    return Object.keys(tableRows.value[0]).map((k) => ({ key: k, title: k }))
  }
  return []
}

onMounted(() => {
  if (props.node.type === 'itemTable') {
    loadTableColumns()
  }
})

// ─── itemEditor ───
/**
 * itemEditor HTML 替换
 * PC 端逻辑（ueditor/index2.vue watch.value 第 275-288 行）：
 *   将 ${field} 占位符替换为 <input value="xxx"> 控件
 * 移动端简化：直接替换为文字值（只读），保留表格结构
 */
const editorHtml = computed(() => {
  let html = props.node.value || ''
  if (!html) return ''
  const fields = props.node.fields || []
  fields.forEach((f) => {
    const val = f.value || f.dvalue || ''
    const replacement = `<span style="display:inline-block;min-width:20px;text-align:center;${f.weight ? 'font-weight:bold;' : ''}${!val && f.isnotnull ? 'color:red;' : ''}">${val}</span>`
    html = html.replace('${' + f.field + '}', replacement)
  })
  html = html.replace(/\$\{[^}]+\}/g, '<span style="color:#ccc;">—</span>')
  return html
})
</script>

<style lang="scss" scoped>
.tn-label {
  padding: 8rpx 12rpx;
  overflow: hidden;
  word-break: break-all;
  line-height: 1.8;
}

.tn-layout {
  width: 100%;

  &__col {
    min-width: 0;
    overflow: hidden;
    padding: 6rpx 8rpx;
    display: flex;
    flex-direction: column;

    &--full {
      width: 100%;
    }
  }
}

.tn-field {
  padding: 8rpx 12rpx;
  width: 100%;

  &__inner {
    display: flex;
    align-items: baseline;
    line-height: 1.8;
  }

  &__label {
    width: 160rpx;
    flex-shrink: 0;
    margin-right: 8rpx;
    color: #666;
    word-break: break-all;
    text-align: right;
  }

  &__value {
    flex: 1;
    min-width: 0;
    word-break: break-all;
  }
}

.tn-checkbox {
  padding: 8rpx 12rpx;
}

.tn-table {
  margin: 24rpx 0;
  padding: 16rpx;
  background-color: #fff;
  border-radius: 12rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

  &__scroll {
    width: 100%;
    white-space: nowrap;
  }

  &__html {
    display: inline-block;
    min-width: 100%;
    max-width: 2048rpx;
    white-space: normal;

    :deep(table) {
      border-top: 1rpx solid #333;
      border-left: 1rpx solid #333;
      border-spacing: 0;
    }

    :deep(table td) {
      border-bottom: 1rpx solid #333;
      border-right: 1rpx solid #333;
      padding: 10rpx 16rpx;
      word-break: break-all;
    }
  }

  &__empty {
    padding: 16rpx;
    text-align: center;
  }
}

.tn-editor {
  margin: 24rpx 0;
  padding: 16rpx;
  background-color: #fff;
  border-radius: 12rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

  &__scroll {
    width: 100%;
    white-space: nowrap;
  }

  &__inner {
    display: inline-block;
    min-width: 100%;
    max-width: 2048rpx;
  }

  &__html {
    white-space: normal;

    :deep(table) {
      border-top: 1rpx solid #333;
      border-left: 1rpx solid #333;
      border-spacing: 0;
    }

    :deep(table td) {
      border-bottom: 1rpx solid #333;
      border-right: 1rpx solid #333;
      padding: 6rpx 8rpx;
      word-break: break-all;
      font-size: 24rpx;
    }

    :deep(table td p) {
      margin: 2rpx 0;
      font-size: 24rpx;
    }

    :deep(table td input) {
      border: none;
      background: none;
      width: 100%;
      font-size: 24rpx;
      padding: 0;
    }
  }
}
</style>
