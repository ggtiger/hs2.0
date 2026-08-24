<template>
  <div class="asst-thinking">
    <div class="asst-thinking-head" @click="open = !open">
      💭 {{ open ? '收起' : '展开' }}思考过程
    </div>
    <div v-show="open" class="asst-thinking-body">{{ text }}</div>
  </div>
</template>

<script>
export default {
  name: 'ThinkingBlock',
  props: {
    text: { type: String, default: '' }
  },
  data() {
    return { open: false };
  }
};
</script>

<style scoped>
.asst-thinking {
  margin: 4px 0;
  font-size: 12px;
  color: #888;
}
.asst-thinking-head {
  cursor: pointer;
  user-select: none;
}
.asst-thinking-body {
  margin-top: 4px;
  padding: 6px 8px;
  background: #f6f8fa;
  border-radius: 4px;
  white-space: pre-wrap;
}
</style>
