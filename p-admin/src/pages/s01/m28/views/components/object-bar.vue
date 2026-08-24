<template>
  <div class="st-obar" v-if="item">
    <div class="st-obar-left">
      <i :class="sectionIcon"></i>
      <span class="st-obar-name">{{ itemDisplayName }}</span>
      <span v-if="itemTypeName" :class="['st-type-badge', typeClass]">{{ itemTypeName }}</span>
      <span v-if="itemSubInfo" class="st-obar-sub">{{ itemSubInfo }}</span>
    </div>
    <div class="st-obar-right">
      <slot name="actions" />
      <Button size="xs" @click="onAskAI" title="问 AI">
        <i class="h-icon-bubble"></i>
      </Button>
    </div>
  </div>
</template>

<script>
var SECTION_META = {
  resource: { icon: 'h-icon-link', nameField: 'RESOURCENAME', typeField: 'RESOURCETYPE', subField: 'TABLENAME' },
  page: { icon: 'h-icon-edit', nameField: 'PAGECODE', typeField: 'PAGETYPE', subField: 'PAGENAME' },
  code: { icon: 'h-icon-code', nameField: 'CODE', typeField: 'ASSETTYPE', subField: 'NAME' },
  menu: { icon: 'h-icon-menu', nameField: 'FUNCCODE', typeField: '', subField: 'FUNCNAME' },
  version: { icon: 'h-icon-clock', nameField: 'OBJCODE', typeField: 'OPTYPE', subField: 'VERSION' },
  template: { icon: 'h-icon-folder', nameField: 'TEMPLATECODE', typeField: 'CATEGORY', subField: 'TEMPLATENAME' },
  dict: { icon: 'h-icon-book', nameField: 'DICTCODE', typeField: '', subField: 'DICTNAME' },
  scene: { icon: 'h-icon-star', nameField: 'SCENECODE', typeField: '', subField: 'SCENENAME' }
};

export default {
  name: 'ObjectBar',
  props: {
    section: { type: String, default: '' },
    item: { type: Object, default: null }
  },
  computed: {
    meta() {
      return SECTION_META[this.section] || {};
    },
    sectionIcon() {
      return this.meta.icon || 'h-icon-info';
    },
    itemDisplayName() {
      var f = this.meta.nameField;
      return (this.item && f && this.item[f]) || '';
    },
    itemTypeName() {
      var f = this.meta.typeField;
      if (!f) return '';
      return (this.item && this.item[f]) || '';
    },
    typeClass() {
      var t = this.itemTypeName;
      if (!t) return '';
      var s = this.section;
      if (s === 'resource') {
        if (t === 'TABLE') return 'type-table';
        if (t === 'DATAVIEW') return 'type-view';
        if (t === 'SQL') return 'type-sql';
        return 'type-other';
      }
      if (s === 'page') {
        if (t === 'list') return 'type-list';
        if (t === 'form') return 'type-form';
        if (t === 'report') return 'type-report';
        if (t === 'custom') return 'type-custom';
        return 'type-other';
      }
      if (s === 'code') {
        var lt = t.toLowerCase();
        if (lt === 'csharp') return 'type-csharp';
        if (lt === 'sql') return 'type-sql';
        if (lt === 'js') return 'type-js';
        if (lt === 'vue') return 'type-vue';
        return 'type-other';
      }
      if (s === 'version') {
        if (t === 'create') return 'type-create';
        if (t === 'update') return 'type-update';
        if (t === 'delete') return 'type-delete';
        return 'type-other';
      }
      return 'type-other';
    },
    itemSubInfo() {
      var f = this.meta.subField;
      if (!f) return '';
      var val = (this.item && this.item[f]) || '';
      if (this.section === 'version' && val) return 'v' + val;
      return val;
    }
  },
  methods: {
    onAskAI() {
      var focusKey = this.section + '_' + ((this.item && this.item.ID) || '');
      var focusLabel = this.section + ' ' + this.itemDisplayName;
      this.$emit('ask-ai', { key: focusKey, label: focusLabel, icon: this.sectionIcon });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-obar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 @st-space-lg;
  height: @st-obar-h;
  background: @st-bg-gray;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-obar-left {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  min-width: 0;
  i { font-size: 16px; color: @st-primary; }
}

.st-obar-name {
  font-family: @st-mono;
  font-size: 13px;
  font-weight: 600;
  color: @st-text;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.st-type-badge {
  .st-type-badge(@st-bg-gray, @st-text-hint);
  &.type-table { .st-type-badge(@st-primary-light, @st-blue); }
  &.type-view { .st-type-badge(@st-success-bg, @st-success); }
  &.type-sql { .st-type-badge(@st-warning-bg, @st-warning); }
  &.type-list { .st-type-badge(@st-primary-light, @st-blue); }
  &.type-form { .st-type-badge(@st-success-bg, @st-success); }
  &.type-report { .st-type-badge(@st-warning-bg, @st-warning); }
  &.type-custom { .st-type-badge(@st-purple-bg, @st-purple); }
  &.type-csharp { .st-type-badge(@st-purple-bg, @st-purple); }
  &.type-js { .st-type-badge(@st-warning-bg, @st-warning); }
  &.type-vue { .st-type-badge(@st-success-bg, @st-success); }
  &.type-create { .st-type-badge(@st-success-bg, @st-success); }
  &.type-update { .st-type-badge(@st-primary-light, @st-blue); }
  &.type-delete { .st-type-badge(@st-error-bg, @st-error); }
  &.type-other { .st-type-badge(@st-bg-gray, @st-text-hint); }
}

.st-obar-sub {
  font-size: 11px;
  color: @st-text-hint;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.st-obar-right {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  flex-shrink: 0;
}
</style>
