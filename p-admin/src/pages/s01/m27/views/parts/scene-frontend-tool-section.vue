<template>
  <div class="scene-frontend-tool-section sp-section">
    <div class="sfs-header" @click="toggleSection">
      <i :class="sectionExpanded ? 'h-icon-down' : 'h-icon-right'"></i>
      <span class="sfs-title">前端工具</span>
      <span class="sfs-count" v-if="selectedTools.length">{{ selectedTools.length }}/{{ allTools.length }}</span>
    </div>
    <div class="sfs-body" v-if="sectionExpanded">
      <div class="sfs-tags">
        <div class="sfs-tag" v-for="t in allTools" :key="t.name"
          :class="{ active: selectedTools.indexOf(t.name) >= 0 }"
          @click="toggleTool(t.name)" :title="t.desc">
          {{ t.name }}
        </div>
        <div class="sfs-tag special" :class="{ active: value === 'all' }" @click="$emit('input', 'all')">all (全部)</div>
        <div class="sfs-tag special" :class="{ active: value === 'none' }" @click="$emit('input', 'none')">none (无)</div>
      </div>
    </div>
  </div>
</template>

<script>
import { FRONTEND_TOOL_LIST } from './scenePromptMap';

export default {
  name: 'SceneFrontendToolSection',
  props: {
    value: { type: String, default: '' }
  },
  data() {
    return {
      sectionExpanded: true
    };
  },
  computed: {
    allTools() {
      return FRONTEND_TOOL_LIST;
    },
    selectedTools() {
      var v = this.value || '';
      if (v === 'all' || v === 'none' || !v) return [];
      return v.split(',').map(function(s) { return s.trim(); }).filter(Boolean);
    }
  },
  methods: {
    toggleSection() {
      this.sectionExpanded = !this.sectionExpanded;
    },
    toggleTool(name) {
      var sel = this.selectedTools.slice();
      var idx = sel.indexOf(name);
      if (idx >= 0) sel.splice(idx, 1);
      else sel.push(name);
      this.$emit('input', sel.length > 0 ? sel.join(',') : 'none');
    }
  }
};
</script>

<style lang="less" scoped>
.scene-frontend-tool-section {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.sfs-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  flex-shrink: 0;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.sfs-title { font-size: 13px; font-weight: 600; }
.sfs-count {
  font-size: 11px; background: #f6ffed; color: #52c41a;
  padding: 0 6px; border-radius: 8px;
}
.sfs-body { padding: 10px 12px; }
.sfs-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
.sfs-tag {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 3px;
  border: 1px solid #d9d9d9;
  cursor: pointer;
  font-family: Consolas, monospace;
  color: #666;
  &:hover { border-color: #2F54EB; color: #2F54EB; }
  &.active { background: #e6f7ff; border-color: #91d5ff; color: #1890ff; }
  &.special { font-family: inherit; font-style: italic; }
}
</style>
