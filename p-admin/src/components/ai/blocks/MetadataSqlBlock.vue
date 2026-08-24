<template>
  <div class="ai-meta-sql">
    <div class="ams-header">
      <span class="ams-icon">⚠️</span>
      <span class="ams-label">元数据变更 SQL</span>
      <span class="ams-status" :class="'ams-' + execStatus">{{ statusText }}</span>
    </div>
    <pre class="ams-code">{{ code }}</pre>
    <div class="ams-actions" v-if="execStatus === 'pending'">
      <button class="ams-btn ams-btn-primary" @click="onConfirm">确认执行</button>
      <button class="ams-btn" @click="ignore">忽略</button>
    </div>
    <div class="ams-result" v-if="execResult">{{ execResult }}</div>
  </div>
</template>

<script>
// 元数据 SQL 可交互卡片
// execStatus 状态机：pending -> running -> success/cancelled/failed
// 确认执行时 emit confirm(code)，调用方执行 SQL 后通过 setStatus 更新状态
export default {
  name: 'MetadataSqlBlock',
  props: {
    code: { type: String, default: '' },
    // 初始状态，默认 pending
    initialStatus: { type: String, default: 'pending' }
  },
  data() {
    return {
      execStatus: this.initialStatus,
      execResult: ''
    };
  },
  computed: {
    statusText() {
      var map = {
        pending: '待确认',
        running: '执行中...',
        success: '已执行',
        cancelled: '已忽略',
        failed: '失败'
      };
      return map[this.execStatus] || this.execStatus;
    }
  },
  methods: {
    onConfirm() {
      this.execStatus = 'running';
      this.$emit('confirm', this.code);
    },
    ignore() {
      this.execStatus = 'cancelled';
    },
    // 调用方执行完成后调用此方法更新状态
    setStatus(status, result) {
      this.execStatus = status;
      if (result !== undefined) this.execResult = result;
    }
  }
};
</script>

<style lang="less" scoped>
.ai-meta-sql {
  margin: 6px 0;
  border: 1px solid #e6a23c;
  border-radius: 6px;
  background: #fffdf5;
  overflow: hidden;
}
.ams-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  background: #fdf6ec;
  border-bottom: 1px solid #f5dab1;
  font-size: 12px;
}
.ams-icon {
  font-size: 14px;
}
.ams-label {
  color: #e6a23c;
  font-weight: 600;
}
.ams-status {
  margin-left: auto;
  font-size: 11px;
}
.ams-pending {
  color: #e6a23c;
}
.ams-running {
  color: #409eff;
}
.ams-success {
  color: #67c23a;
}
.ams-failed {
  color: #f56c6c;
}
.ams-cancelled {
  color: #909399;
}
.ams-code {
  margin: 0;
  padding: 8px 10px;
  background: #1e1e1e;
  color: #d4d4d4;
  font-family: 'Monaco', 'Consolas', monospace;
  font-size: 12px;
  line-height: 1.5;
  max-height: 240px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
}
.ams-actions {
  display: flex;
  gap: 8px;
  padding: 6px 10px;
  background: #fdfdf6;
  border-top: 1px solid #f5dab1;
}
.ams-btn {
  padding: 4px 14px;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  background: #fff;
  color: #606266;
  font-size: 12px;
  cursor: pointer;
  &:hover {
    border-color: #409eff;
    color: #409eff;
  }
}
.ams-btn-primary {
  background: #409eff;
  border-color: #409eff;
  color: #fff;
  &:hover {
    background: #66b1ff;
    border-color: #66b1ff;
    color: #fff;
  }
}
.ams-result {
  padding: 6px 10px;
  background: #f0f9eb;
  color: #67c23a;
  font-size: 12px;
  border-top: 1px solid #e1f3d8;
}
</style>
