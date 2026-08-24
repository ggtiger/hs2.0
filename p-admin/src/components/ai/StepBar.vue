<template>
  <div class="ai-step-bar" v-if="steps && steps.length > 0">
    <div
      v-for="(s, i) in steps"
      :key="s.key || i"
      class="ai-step"
      :class="['ai-step-' + (s.status || 'pending'), { 'ai-step-active': currentStep === i || s.status === 'start' }]"
    >
      <span class="ai-step-icon">
        <template v-if="s.status === 'done'">✓</template>
        <template v-else-if="s.status === 'skipped'">○</template>
        <template v-else-if="s.status === 'start'">●</template>
        <template v-else>{{ i + 1 }}</template>
      </span>
      <span class="ai-step-label">{{ s.label }}</span>
      <span v-if="i < steps.length - 1" class="ai-step-line"></span>
    </div>
  </div>
</template>

<script>
// 步骤条：从 workspace.vue / module-wizard.vue 提取
// steps: [{ key, label, status }]  status: pending/start/done/skipped
// currentStep: 当前步骤索引（可选，用于高亮）
export default {
  name: 'StepBar',
  props: {
    steps: { type: Array, default: function() { return [] } },
    currentStep: { type: Number, default: -1 }
  }
};
</script>

<style lang="less" scoped>
.ai-step-bar {
  display: flex;
  align-items: flex-start;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #eee;
}
.ai-step {
  display: flex;
  align-items: center;
  gap: 6px;
  position: relative;
  padding-right: 24px;
  font-size: 12px;
  color: #909399;
}
.ai-step-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #e4e7ed;
  color: #909399;
  font-size: 12px;
  font-weight: 600;
  flex-shrink: 0;
}
.ai-step-label {
  white-space: nowrap;
}
.ai-step-line {
  position: absolute;
  right: 0;
  top: 11px;
  width: 18px;
  height: 1px;
  background: #dcdfe6;
}
.ai-step-pending {
  color: #909399;
}
.ai-step-start {
  color: #409eff;
  .ai-step-icon {
    background: #409eff;
    color: #fff;
  }
}
.ai-step-done {
  color: #19be6b;
  .ai-step-icon {
    background: #19be6b;
    color: #fff;
  }
  .ai-step-line {
    background: #19be6b;
  }
}
.ai-step-skipped {
  color: #c0c4cc;
}
.ai-step-active {
  font-weight: 600;
}
</style>
