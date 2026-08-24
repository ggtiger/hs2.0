<template>
  <div class="st-aislide">
    <div class="st-aislide-header">
      <span class="st-aislide-title"><i class="h-icon-bubble"></i> AI 助理</span>
      <Button size="xs" icon="h-icon-close" @click="$emit('close')" />
    </div>
    <div class="st-aislide-body">
      <ai-panel
        ref="aiPanel"
        :selected-module="selectedModule"
        @collapse="$emit('close')"
        @open-wizard="$emit('open-wizard')"
      />
    </div>
  </div>
</template>

<script>
import AiPanel from './ai-panel.vue';

export default {
  name: 'AiSlide',
  components: { AiPanel },
  props: {
    selectedModule: { type: Object, default: null }
  },
  methods: {
    addFocus(focus) {
      if (this.$refs.aiPanel) {
        this.$refs.aiPanel.addFocus(focus);
      }
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-aislide {
  width: 320px;
  flex-shrink: 0;
  border-left: 1px solid @st-border;
  background: @st-bg-white;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.st-aislide-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 @st-space-md;
  height: @st-obar-h;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
  .st-aislide-title {
    display: flex;
    align-items: center;
    gap: @st-space-sm;
    font-size: 13px;
    font-weight: 600;
    color: @st-text;
    i { color: @st-primary; }
  }
}

.st-aislide-body {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
</style>
