<template>
  <div class="icon-sel">
    <Select
      :datas="selectOptions"
      v-model="selectValue"
      :readonly="true"
      :filterable="false"
      :placeholder="value ? '' : '请选择图标'"
      @click.native="open"
    >
      <template slot="show" slot-scope="{value: obj}">
        <i :class="obj.key"></i>
      </template>
    </Select>
    <Modal v-model="visible" :hasCloseIcon="true" middle>
      <view-dialog title="选择图标">
        <template slot="body">
          <Search v-model="keyword" placeholder="搜索图标" style="width:100%;margin-bottom:10px;" />
          <div class="icon-sel-grid">
            <div
              v-for="icon in filteredIcons"
              :key="icon"
              class="icon-sel-item"
              :class="{ 'icon-sel-active': value === icon }"
              :title="icon"
              @click="select(icon)"
            >
              <i :class="icon"></i>
              <div class="icon-sel-name">{{ icon.replace('h-icon-', '') }}</div>
            </div>
          </div>
        </template>
      </view-dialog>
    </Modal>
  </div>
</template>

<script>
const ICONS = [
  'h-icon-home', 'h-icon-task', 'h-icon-menu', 'h-icon-inbox', 'h-icon-outbox',
  'h-icon-user', 'h-icon-users', 'h-icon-bell', 'h-icon-message', 'h-icon-link',
  'h-icon-setting', 'h-icon-edit', 'h-icon-search', 'h-icon-refresh', 'h-icon-complete',
  'h-icon-completed', 'h-icon-check', 'h-icon-plus', 'h-icon-minus', 'h-icon-trash',
  'h-icon-upload', 'h-icon-download', 'h-icon-calendar', 'h-icon-location', 'h-icon-lock',
  'h-icon-star', 'h-icon-star-on', 'h-icon-fullscreen', 'h-icon-github', 'h-icon-help',
  'h-icon-help-solid', 'h-icon-info', 'h-icon-warn', 'h-icon-success', 'h-icon-error',
  'h-icon-close', 'h-icon-close-min', 'h-icon-top', 'h-icon-down', 'h-icon-left',
  'h-icon-right', 'h-icon-angle-top', 'h-icon-angle-down', 'h-icon-angle-left', 'h-icon-angle-right',
  'h-icon-loading', 'h-icon-spinner'
];

export default {
  name: 'IconSel',
  props: {
    value: { type: String, default: '' }
  },
  data() {
    return {
      visible: false,
      keyword: '',
      icons: ICONS
    };
  },
  computed: {
    selectOptions() {
      return this.value ? [{ key: this.value, title: this.value }] : [];
    },
    selectValue() {
      return this.value || '';
    },
    filteredIcons() {
      if (!this.keyword) return this.icons;
      return this.icons.filter(icon => icon.indexOf(this.keyword.toLowerCase()) > -1);
    }
  },
  methods: {
    open() {
      this.visible = true;
      this.keyword = '';
    },
    select(icon) {
      this.$emit('input', icon);
      this.visible = false;
    }
  }
};
</script>

<style scoped>
.icon-sel {
  width: 100%;
}
.icon-sel >>> .h-select-show {
  cursor: pointer;
}
.icon-sel-placeholder {
  color: #b0b0b0;
}
.icon-sel-grid {
  display: flex;
  flex-wrap: wrap;
  max-height: 320px;
  overflow-y: auto;
  margin-top: 10px;
}
.icon-sel-item {
  width: 72px;
  height: 68px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  border: 1px solid transparent;
  border-radius: 4px;
  margin: 2px;
  transition: all 0.2s;
}
.icon-sel-item:hover {
  background-color: var(--hover-bg, #f0f5ff);
  border-color: var(--hover-border, #d9e5ff);
}
.icon-sel-item > i {
  font-size: 22px;
  color: #555;
}
.icon-sel-item.icon-sel-active {
  background-color: var(--hover-bg, #e8f0ff);
  border-color: var(--active-border, #5b9bd5);
}
.icon-sel-item.icon-sel-active > i {
  color: var(--active-border, #5b9bd5);
}
.icon-sel-name {
  font-size: 11px;
  color: #999;
  margin-top: 4px;
  word-break: break-all;
  text-align: center;
  line-height: 1.2;
}
</style>
