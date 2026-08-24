<template>
  <div class="asst-toolcall">
    <div class="asst-toolcall-head" @click="open = !open">
      <span class="asst-tool-icon">{{ statusIcon }}</span>
      <span class="asst-tool-name">{{ tool }}</span>
      <span v-if="statusText" class="asst-tool-status" :class="statusClass">{{ statusText }}</span>
      <span class="asst-tool-toggle">{{ open ? '▾' : '▸' }}</span>
    </div>
    <div v-show="open" class="asst-toolcall-body">
      <div v-if="args" class="asst-tool-args">参数: {{ prettyArgs }}</div>
      <div v-if="summary" class="asst-tool-result">结果: {{ summary }}</div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'ToolCallBlock',
  props: {
    tool: { type: String, default: '' },
    args: { type: String, default: '' },
    summary: { type: String, default: '' }
  },
  data() {
    return { open: false };
  },
  computed: {
    prettyArgs() {
      if (!this.args) return '';
      try {
        return JSON.stringify(JSON.parse(this.args));
      } catch (e) {
        return this.args;
      }
    },
    // 状态：根据 summary 内容判断（前端工具用 ✅/❌ 前缀，后端工具用 JSON）
    statusIcon() {
      if (!this.summary) return '🔧';
      if (this.summary === '执行中…') return '⏳';
      if (this.summary.indexOf('✅') === 0) return '✅';
      if (this.summary.indexOf('❌') === 0) return '❌';
      return '🔧';
    },
    statusText() {
      if (!this.summary) return '';
      if (this.summary === '执行中…') return '执行中';
      if (this.summary.indexOf('✅') === 0) return '成功';
      if (this.summary.indexOf('❌') === 0) return '失败';
      return '已完成';
    },
    statusClass() {
      if (this.summary === '执行中…') return 'asst-status-running';
      if (this.summary.indexOf('✅') === 0) return 'asst-status-success';
      if (this.summary.indexOf('❌') === 0) return 'asst-status-fail';
      return '';
    }
  }
};
</script>

<style scoped>
.asst-toolcall {
  margin: 4px 0;
  font-size: 12px;
  border-left: 3px solid #2d8cf0;
  background: #f6f8fa;
  border-radius: 0 4px 4px 0;
  padding: 4px 8px;
}
.asst-toolcall-head {
  cursor: pointer;
  user-select: none;
  color: #2d8cf0;
  display: flex;
  align-items: center;
  gap: 4px;
}
.asst-tool-name {
  font-weight: 600;
}
.asst-tool-status {
  font-size: 11px;
  padding: 1px 6px;
  border-radius: 8px;
  color: #fff;
}
.asst-status-running {
  background: #faad14;
}
.asst-status-success {
  background: #19be6b;
}
.asst-status-fail {
  background: #ed4014;
}
.asst-tool-toggle {
  margin-left: auto;
  color: #999;
}
.asst-toolcall-body {
  margin-top: 4px;
  color: #555;
  word-break: break-all;
}
.asst-tool-result {
  margin-top: 2px;
  color: #19be6b;
}
</style>
