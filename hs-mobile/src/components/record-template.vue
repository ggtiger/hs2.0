<template>
  <view class="record-tpl">
    <template v-for="(group, gi) in groupedNodes" :key="gi">
      <!-- 信息块：连续的 field/label/layout/checkbox 包裹在白色卡片中 -->
      <view v-if="group.type === 'info'" class="info-block">
        <tpl-node v-for="(node, ni) in group.nodes" :key="ni" :node="node" />
      </view>
      <!-- 独立块：table/editor 单独渲染，自带卡片样式 -->
      <tpl-node v-else v-for="(node, ni) in group.nodes" :key="ni" :node="node" />
    </template>
  </view>
</template>

<script setup>
/**
 * 原始记录模板渲染器
 * 将连续的 itemField/itemLabel/itemLayout/itemCheckBox 归为一个"信息块"（白色卡片），
 * 遇到 itemTable/itemEditor 则断开（它们自带独立卡片样式）。
 */
import { computed } from 'vue'
import TplNode from './tpl-node.vue'

const props = defineProps({
  nodes: { type: Array, default: () => [] }
})

// 信息类节点：连续的这些归到一个白色卡片里
const INFO_TYPES = new Set(['itemField', 'itemLabel', 'itemLayout', 'itemCheckBox'])

// 分组：连续信息类 → info 块；table/editor → independent 块
const groupedNodes = computed(() => {
  const groups = []
  let current = null

  props.nodes.forEach((node) => {
    if (INFO_TYPES.has(node.type)) {
      // 信息类节点，归入当前 info 块
      if (!current || current.type !== 'info') {
        current = { type: 'info', nodes: [] }
        groups.push(current)
      }
      current.nodes.push(node)
    } else {
      // table/editor，断开当前 info 块，独立成块
      current = { type: 'independent', nodes: [node] }
      groups.push(current)
    }
  })

  return groups
})
</script>

<style lang="scss" scoped>
.record-tpl {
  width: 100%;
}

.info-block {
  margin: 24rpx 0;
  padding: 16rpx;
  background-color: #fff;
  border-radius: 12rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);
}
</style>
