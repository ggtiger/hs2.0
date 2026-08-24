<template>
  <div class="st-slist">
    <div class="st-slist-toolbar">
      <input
        class="st-slist-search"
        v-model="keyword"
        :placeholder="'搜索' + sectionName + '...'"
      />
      <Button size="xs" icon="h-icon-refresh" @click="loadList" />
    </div>
    <div v-if="loading" class="st-slist-state">
      <i class="h-icon-loading"></i>
    </div>
    <div v-else-if="filteredList.length === 0" class="st-slist-empty">
      暂无{{ sectionName }}
    </div>
    <div v-else class="st-slist-list">
      <template v-for="item in filteredList">
        <div
          v-if="item.isGroupHeader"
          :key="'gh_' + item.groupKey"
          class="st-slist-group"
        >
          <span class="st-slist-group-label">{{ item.groupLabel }}</span>
          <span class="st-slist-group-count">{{ item.groupCount }}</span>
        </div>
        <div
          v-else
          :key="item.ID"
          :class="['st-slist-item', { active: activeItemId === item.ID }]"
          @click="onSelect(item)"
        >
          <span class="st-slist-code">{{ item[displayField] }}</span>
          <span class="st-slist-type" v-if="item.RESOURCETYPE">{{ item.RESOURCETYPE === 'TABLE' ? '表' : item.RESOURCETYPE === 'DATAVIEW' ? '视图' : item.RESOURCETYPE === 'SQL' ? 'SQL' : '' }}</span>
          <span class="st-slist-desc" v-if="descField && item[descField]">{{ item[descField] }}</span>
        </div>
      </template>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';

// 默认配置（向后兼容，优先使用 sectionDefs prop）
var DEFAULT_DEFS = {};

export default {
  name: 'SectionList',
  props: {
    section: { type: String, default: '' },
    selectedModule: { type: Object, default: null },
    activeItemId: { type: String, default: '' },
    sectionDefs: { type: Object, default: null }
  },
  data() {
    return {
      keyword: '',
      loading: false,
      items: []
    };
  },
  computed: {
    defs() {
      return this.sectionDefs || DEFAULT_DEFS;
    },
    config() {
      return this.defs[this.section] || null;
    },
    sectionName() {
      return this.config ? this.config.name : '';
    },
    displayField() {
      return this.config ? this.config.display : '';
    },
    descField() {
      return this.config ? this.config.desc : '';
    },
    filteredList() {
      if (!this.keyword || !this.items) return this.items;
      var kw = this.keyword.toUpperCase();
      var df = this.displayField;
      var sf = this.descField;
      return this.items.filter(function(item) {
        if (item.isGroupHeader) return true;
        var code = (item[df] || '').toUpperCase();
        var name = sf ? (item[sf] || '').toUpperCase() : '';
        return code.indexOf(kw) >= 0 || name.indexOf(kw) >= 0;
      });
    },
    moduleCode() {
      return (this.selectedModule && this.selectedModule.MODULECODE) || '';
    }
  },
  watch: {
    section() {
      this.items = [];
      this.keyword = '';
      if (this.moduleCode) this.loadList();
    },
    moduleCode(v) {
      if (v) {
        this.loadList();
        this.loadAllCounts();
      } else {
        this.items = [];
      }
    }
  },
  created() {
    if (this.moduleCode) this.loadList();
  },
  methods: {
    async loadList() {
      var cfg = this.config;
      var mc = this.moduleCode;
      if (!cfg || !mc) return;
      this.loading = true;
      try {
        getGenericStore(cfg.store);
        var ret = await this.$callAction({
          action: cfg.store + '/call',
          param: {
            APICODE: cfg.api,
            params: {
              FilterParams: cfg.filterParams(mc),
              PageSize: 500,
              PageIndex: 1
            }
          },
          isBusy: false
        });
        var rows = cfg.extract(ret);
        this.items = cfg.transform(rows, mc);
        this.$emit('count', { key: this.section, n: this.items.filter(function(i) { return !i.isGroupHeader }).length });
        this.$emit('loaded');
      } catch (e) {
        this.items = [];
      } finally {
        this.loading = false;
      }
    },
    async loadAllCounts() {
      var mc = this.moduleCode;
      if (!mc) return;
      var self = this;
      var keys = Object.keys(this.defs);
      for (var i = 0; i < keys.length; i++) {
        var key = keys[i];
        if (key === this.section) continue;
        var cfg = this.defs[key];
        try {
          getGenericStore(cfg.store);
          var ret = await self.$callAction({
            action: cfg.store + '/call',
            param: {
              APICODE: cfg.api,
              params: {
                FilterParams: cfg.filterParams(mc),
                PageSize: 500,
                PageIndex: 1
              }
            },
            isBusy: false
          });
          var rows = cfg.extract(ret);
          var transformed = cfg.transform(rows, mc);
          self.$emit('count', { key: key, n: transformed.filter(function(item) { return !item.isGroupHeader }).length });
        } catch (e) {
          self.$emit('count', { key: key, n: 0 });
        }
      }
    },
    onSelect(item) {
      if (item.isGroupHeader) return;
      this.$emit('select', item);
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-slist {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: @st-bg-white;
}

.st-slist-toolbar {
  display: flex;
  gap: @st-space-sm;
  padding: @st-space-sm;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-slist-search {
  flex: 1;
  height: 28px;
  padding: 0 @st-space-sm;
  border: 1px solid @st-border;
  border-radius: @st-radius;
  font-size: 11px;
  outline: none;
  &:focus { border-color: @st-primary; }
}

.st-slist-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-primary;
}

.st-slist-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-text-disabled;
  font-size: 12px;
}

.st-slist-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
}

.st-slist-item {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  padding: 6px @st-space-md;
  cursor: pointer;
  white-space: nowrap;
  &:hover { background: @st-primary-pale; }
  &.active {
    background: @st-primary-light;
    .st-slist-code { color: @st-primary; font-weight: 600; }
  }
  .st-slist-code {
    font-size: 12px;
    color: @st-text;
    overflow: hidden;
    text-overflow: ellipsis;
    font-family: @st-mono;
  }
  .st-slist-type {
    font-size: 9px;
    padding: 0 4px;
    border-radius: 3px;
    line-height: 14px;
    font-weight: 600;
    flex-shrink: 0;
    background: #e6f7ff;
    color: #1890ff;
  }
  .st-slist-desc {
    font-size: 11px;
    color: @st-text-hint;
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
  }
}

.st-slist-group {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  padding: 4px @st-space-md;
  font-size: 10px;
  font-weight: 600;
  color: @st-text-hint;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  user-select: none;
  .st-slist-group-label { flex: 1; }
  .st-slist-group-count {
    background: @st-bg-gray;
    border-radius: 8px;
    padding: 0 5px;
    font-size: 9px;
    line-height: 14px;
    min-width: 14px;
    text-align: center;
  }
}
</style>
