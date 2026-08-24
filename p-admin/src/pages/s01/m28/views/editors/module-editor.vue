<template>
  <div class="st-ed-mod">
    <!-- 左：模块列表 -->
    <div class="st-ed-list">
      <div class="st-ed-list-toolbar">
        <input class="st-ed-list-search" v-model="keyword" placeholder="搜索模块..." />
        <Button size="xs" icon="h-icon-refresh" @click="loadModules" />
      </div>
      <div v-if="loading" class="st-ed-list-state">
        <i class="h-icon-loading"></i>
      </div>
      <div v-else-if="filteredList.length === 0" class="st-ed-list-empty">
        暂无模块
      </div>
      <div v-else class="st-ed-list-items">
        <div
          v-for="m in filteredList"
          :key="m.MODULECODE"
          :class="['st-ed-list-item', { active: selectedModuleCode === m.MODULECODE }]"
          @click="onSelect(m)"
        >
          <i class="h-icon-cube"></i>
          <div class="st-ed-list-text">
            <span class="st-ed-list-code">{{ m.MODULECODE }}</span>
            <span class="st-ed-list-desc" v-if="m.MODULENAME">{{ m.MODULENAME }}</span>
          </div>
        </div>
      </div>
      <div class="st-ed-list-footer">
        <Button size="xs" icon="h-icon-plus" color="primary" block @click="$emit('new-module')">新建模块</Button>
      </div>
    </div>
    <!-- 右：模块编辑（内嵌 m02 add.vue） -->
    <div class="st-ed-form" v-if="selectedModule">
      <div class="st-ed-mod-bar">
        <span class="st-ed-mod-bar-title">{{ selectedModule.MODULECODE }}</span>
        <span class="st-ed-mod-bar-name" v-if="selectedModule.MODULENAME">{{ selectedModule.MODULENAME }}</span>
        <span class="st-ed-mod-bar-flex"></span>
        <Button size="s" @click="openModuleConfig"><i class="h-icon-setting"></i> 模块配置</Button>
        <Button size="s" color="primary" @click="handleSave">保存</Button>
      </div>
      <div class="st-ed-mod-body">
        <mod-add-adapter
          ref="modAdd"
          :key="selectedModuleCode"
          :did="selectedModuleCode"
          @saved="onSaved"
        />
      </div>
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-cube"></i>
      <p>从左侧列表选择模块，或新建模块</p>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import ModAddAdapter from './mod-add-adapter.vue';

export default {
  name: 'ModuleEditor',
  components: { ModAddAdapter: ModAddAdapter },
  props: {
    moduleCode: { type: String, default: '' },
    sections: { type: Array, default: () => [] },
    counts: { type: Object, default: () => ({}) }
  },
  data() {
    return {
      modules: [],
      selectedModuleCode: '',
      loading: false,
      keyword: ''
    };
  },
  computed: {
    selectedModule() {
      if (!this.selectedModuleCode) return null;
      for (var i = 0; i < this.modules.length; i++) {
        if (this.modules[i].MODULECODE === this.selectedModuleCode) return this.modules[i];
      }
      return null;
    },
    filteredList() {
      if (!this.keyword) return this.modules;
      var kw = this.keyword.toUpperCase();
      return this.modules.filter(function(m) {
        return (m.MODULECODE || '').toUpperCase().indexOf(kw) >= 0 ||
               (m.MODULENAME || '').toUpperCase().indexOf(kw) >= 0;
      });
    }
  },
  watch: {
    moduleCode(v) {
      this.selectedModuleCode = v;
    },
    selectedModuleCode(v) {
      this.$emit('module-selected', v, this.selectedModule);
    }
  },
  created() {
    this.M02_STORE = getGenericStore('RS_M02');
    this.selectedModuleCode = this.moduleCode;
    this.loadModules();
  },
  methods: {
    async loadModules() {
      this.loading = true;
      try {
        var QQRY = this.M02_STORE.storeHelper.getTable('QQRY');
        if (QQRY) {
          QQRY.setValue('PageSize', 500);
          QQRY.setValue('PageIndex', 1);
          try { QQRY.setValue('INPUT', '') } catch (e) { /* */ }
        }
        await this.$callAction({ action: 'RS_M02/query' });
        var st = this.$store.state['RS_M02'];
        this.modules = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        // 模块列表加载完后，如果已有选中模块，重新 emit（computed selectedModule 现在能找到了）
        if (this.selectedModuleCode && this.selectedModule) {
          this.$emit('module-selected', this.selectedModuleCode, this.selectedModule);
        }
      } catch (e) {
        this.modules = [];
      } finally {
        this.loading = false;
      }
    },
    onSelect(m) {
      this.selectedModuleCode = m.MODULECODE;
      this.$emit('module-selected', m.MODULECODE, m);
    },
    openModuleConfig() {
      this.$emit('open-config', this.selectedModuleCode);
    },
    handleSave() {
      if (this.$refs.modAdd && this.$refs.modAdd.$refs.modAdd && typeof this.$refs.modAdd.$refs.modAdd.save === 'function') {
        this.$refs.modAdd.$refs.modAdd.save();
      }
    },
    onSaved() {
      this.loadModules();
      this.$emit('saved');
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-mod {
  display: flex;
  flex: 1;
  min-height: 0;
  background: @st-bg-white;
}

.st-ed-list {
  width: 280px;
  flex-shrink: 0;
  border-right: 1px solid @st-border-light;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.st-ed-list-toolbar {
  display: flex;
  gap: @st-space-sm;
  padding: @st-space-sm;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-ed-list-search {
  flex: 1;
  height: 28px;
  padding: 0 @st-space-sm;
  border: 1px solid @st-border;
  border-radius: @st-radius;
  font-size: 11px;
  outline: none;
  &:focus { border-color: @st-primary; }
}

.st-ed-list-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-primary;
}

.st-ed-list-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-text-disabled;
  font-size: 12px;
}

.st-ed-list-items {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
}

.st-ed-list-item {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  padding: 8px @st-space-md;
  cursor: pointer;
  white-space: nowrap;
  &:hover { background: @st-primary-pale; }
  &.active {
    background: @st-primary-light;
    .st-ed-list-code { color: @st-primary; font-weight: 600; }
    i { color: @st-primary; }
  }
  i { font-size: 16px; color: @st-text-hint; }
}

.st-ed-list-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.st-ed-list-code {
  font-size: 12px;
  color: @st-text;
  overflow: hidden;
  text-overflow: ellipsis;
  font-family: @st-mono;
}

.st-ed-list-desc {
  font-size: 11px;
  color: @st-text-hint;
  overflow: hidden;
  text-overflow: ellipsis;
}

.st-ed-list-footer {
  padding: @st-space-sm;
  border-top: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-ed-form {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.st-ed-mod-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 12px;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-ed-mod-bar-title {
  font-size: 13px;
  font-weight: 700;
  color: @st-text;
  font-family: @st-mono;
}

.st-ed-mod-bar-name {
  font-size: 12px;
  color: @st-text-sec;
}

.st-ed-mod-bar-flex { flex: 1; }

.st-ed-mod-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  & /deep/ .h-panel {
    border: none;
    box-shadow: none;
  }
  & /deep/ .h-panel-bar {
    display: none;
  }
  & /deep/ .h-panel-footer {
    display: none;
  }
  & /deep/ .maxModalH {
    max-height: none;
    overflow: visible;
  }
}

.st-ed-mod-sections {
  border-top: 1px solid @st-border-light;
  padding: 10px 12px;
  flex-shrink: 0;
}

.st-ed-mod-section-title {
  font-size: 11px;
  font-weight: 600;
  color: @st-text-hint;
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.st-ed-mod-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 6px;
}

.st-ed-mod-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 8px 4px;
  border: 1px solid @st-border-light;
  border-radius: @st-radius;
  cursor: pointer;
  position: relative;
  &:hover { border-color: @st-primary; background: @st-primary-pale; }
  i { font-size: 16px; color: @st-primary; }
}

.st-ed-mod-card-name {
  font-size: 10px;
  font-weight: 600;
  color: @st-text;
}

.st-ed-mod-card-count {
  position: absolute;
  top: 2px;
  right: 2px;
  background: @st-primary-pale;
  color: @st-primary;
  border-radius: 6px;
  padding: 0 4px;
  font-size: 9px;
  line-height: 12px;
  min-width: 12px;
  text-align: center;
}

.st-ed-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: @st-text-hint;
  i { font-size: 40px; color: #d6e4ff; }
  p { margin: 0; font-size: 12px; }
}
</style>
