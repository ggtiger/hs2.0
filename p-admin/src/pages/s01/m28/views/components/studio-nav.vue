<template>
  <div :class="['st-nav', { collapsed: collapsed }]">
    <div
      v-for="s in sections"
      :key="s.key"
      :class="['st-nav-item', { active: active === s.key, disabled: s.key !== 'module' && !hasModule }]"
      :title="collapsed ? s.name : ''"
      @click="onSelect(s)"
    >
      <i :class="s.icon"></i>
      <div class="st-nav-text" v-if="!collapsed">
        <div class="st-nav-name">{{ s.name }}</div>
      </div>
      <span class="st-nav-count" v-if="counts[s.key] !== undefined && counts[s.key] > 0">{{ counts[s.key] }}</span>
    </div>
    <div class="st-nav-divider"></div>
    <div class="st-nav-item st-nav-new" :title="collapsed ? '新建模块' : ''" @click="$emit('new-module')">
      <i class="h-icon-plus"></i>
      <span class="st-nav-name" v-if="!collapsed">新建模块</span>
    </div>
    <!-- 底部折叠按钮 -->
    <div class="st-nav-toggle" @click="$emit('toggle-collapse')">
      <i :class="collapsed ? 'h-icon-arrow-right' : 'h-icon-arrow-left'"></i>
    </div>
  </div>
</template>

<script>
export default {
  name: 'StudioNav',
  props: {
    sections: { type: Array, default: () => [] },
    active: { type: String, default: '' },
    counts: { type: Object, default: () => ({}) },
    collapsed: { type: Boolean, default: false },
    hasModule: { type: Boolean, default: false }
  },
  methods: {
    onSelect(s) {
      if (s.key !== 'module' && !this.hasModule) return;
      this.$emit('select', s.key);
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

@st-nav-collapsed-w: 44px;

.st-nav {
  width: @st-nav-w;
  background: @st-bg-white;
  border-right: 1px solid @st-border;
  padding: @st-space-sm 0;
  flex-shrink: 0;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  transition: width 0.2s ease;

  &.collapsed {
    width: @st-nav-collapsed-w;

    .st-nav-item {
      justify-content: center;
      padding: 10px 0;
      border-left-width: 2px;
      i { font-size: 16px; margin: 0; }
    }
    .st-nav-count {
      position: absolute;
      top: 2px;
      right: 2px;
      min-width: 14px;
      padding: 0 3px;
      font-size: 9px;
      line-height: 14px;
      border-radius: 7px;
    }
    .st-nav-item { position: relative; }
    .st-nav-divider { margin: @st-space-sm 6px; }
    .st-nav-new {
      justify-content: center;
      padding: 10px 0;
      i { font-size: 16px; }
    }
    .st-nav-toggle {
      padding: 8px 0;
      i { font-size: 12px; }
    }
  }
}

.st-nav-item {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 10px 10px;
  cursor: pointer;
  border-left: 3px solid transparent;
  transition: background 0.15s, border-color 0.15s;
  i { font-size: 16px; color: @st-text-hint; }
  .st-nav-name { font-size: 13px; font-weight: 600; color: @st-text; width: 26px;}
  .st-nav-count {
    margin-left: auto;
    background: @st-primary-pale;
    color: @st-primary;
    border-radius: 8px;
    padding: 0 6px;
    font-size: 11px;
    line-height: 18px;
    min-width: 18px;
    text-align: center;
  }
  &:hover { background: #f8f9fa; }
  &.active {
    background: @st-primary-light;
    border-left-color: @st-primary;
    i { color: @st-primary; }
    .st-nav-name { color: @st-primary; }
    .st-nav-count { background: @st-primary; color: #fff; }
  }
  &.disabled {
    opacity: 0.4;
    cursor: not-allowed;
    &:hover { background: transparent; }
  }
}

.st-nav-divider {
  height: 1px;
  background: @st-border-light;
  margin: @st-space-sm 14px;
}

.st-nav-new {
  i { color: @st-success; }
  .st-nav-name { color: @st-success; }
  &:hover { background: @st-success-bg; }
}

.st-nav-toggle {
  margin-top: auto;
  padding: 10px;
  text-align: center;
  cursor: pointer;
  border-top: 1px solid @st-border-light;
  i { font-size: 16px; color: @st-text-hint; }
  &:hover { background: #f8f9fa; i { color: @st-primary; } }
}
</style>
