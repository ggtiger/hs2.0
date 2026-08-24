<template>
  <div class="st-ed-version" v-if="moduleCode">
    <!-- 左：版本列表 -->
    <div class="st-ed-list">
      <section-list
        ref="sectionList"
        section="version"
        :selected-module="selectedModule"
        :active-item-id="activeItemId"
        :section-defs="sectionDefs"
        @select="onSelectItem"
        @count="onCount"
      />
    </div>
    <!-- 右：版本对比 -->
    <div class="st-ed-form" v-if="selectedItem">
      <div class="vdiff-inline">
        <!-- 工具栏 -->
        <div class="vdiff-bar">
          <span class="vdiff-bar-code">{{ selectedItem.OBJCODE || '-' }}</span>
          <span :class="['vdiff-tag', 'op-' + (selectedItem.OPTYPE || '')]">{{ opLabel }}</span>
          <span class="vdiff-bar-version" v-if="selectedItem.VERSION">v{{ selectedItem.VERSION }}</span>
          <span class="vdiff-bar-meta" v-if="selectedItem.CREATER">{{ selectedItem.CREATER }} · {{ selectedItem.CREATETIME }}</span>
          <span class="vdiff-bar-flex"></span>
          <Button size="s" :color="mode === 'change' ? 'primary' : null" @click="setMode('change')">该版本变化</Button>
          <Button size="s" :color="mode === 'current' ? 'primary' : null" :loading="loadingCurrent" @click="setMode('current')">与现在对比</Button>
          <Button size="s" @click="openVersionCenter"><i class="h-icon-history"></i> 版本中心</Button>
          <Button size="s" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
        </div>
        <!-- 变更说明 -->
        <div class="vdiff-note" v-if="selectedItem.CHANGENOTE">{{ selectedItem.CHANGENOTE }}</div>
        <!-- 对比提示 -->
        <div class="vdiff-tip" v-if="mode === 'current'">
          {{ currentExists ? '左=该版本快照(v' + selectedItem.VERSION + ' 保存后)，右=当前实时状态' : '对象当前已不存在（可能被删除）' }}
        </div>
        <!-- 对比内容 -->
        <div class="vdiff-body" v-if="verDetail">
          <version-diff-view
            :beforeContent="diffBefore"
            :afterContent="diffAfter"
            :fill="true"
          />
        </div>
        <div v-else class="vdiff-loading">
          <i class="h-icon-loading"></i>
        </div>
      </div>
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-clock"></i>
      <p>从左侧列表选择版本，在此处查看对比</p>
    </div>
  </div>
</template>

<script>
import SectionList from '../components/section-list.vue';
import versionDiffView from '@/components/generic-module/version-diff-view.vue';
import { Constants as VHP, mapState as vhpMapState } from '@/components/generic-module/version-history-store';
import { VERSION_DEF } from '@/constants';

var SECTION_DEFS = { version: VERSION_DEF };

export default {
  name: 'VersionEditor',
  components: { SectionList: SectionList, versionDiffView: versionDiffView },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      selectedItem: null,
      selectedModule: null,
      sectionDefs: SECTION_DEFS,
      mode: 'change',
      loadingCurrent: false
    };
  },
  computed: {
    ...vhpMapState(['currentDetail', 'currentContent', 'currentExists']),
    activeItemId() {
      return (this.selectedItem && this.selectedItem.ID) || '';
    },
    verDetail() { return this.currentDetail; },
    opLabel() {
      if (!this.selectedItem) return '';
      var d = (this.$store.state.app && this.$store.state.app.dicts['版本操作类型']) || {};
      return d[this.selectedItem.OPTYPE] || this.selectedItem.OPTYPE;
    },
    diffBefore() {
      if (!this.verDetail) return null;
      return this.mode === 'change' ? this.verDetail.BEFORECONTENT : this.verDetail.AFTERCONTENT;
    },
    diffAfter() {
      if (!this.verDetail) return null;
      return this.mode === 'change' ? this.verDetail.AFTERCONTENT : this.currentContent;
    }
  },
  watch: {
    moduleCode: {
      handler(v) {
        if (v) {
          this.selectedModule = { MODULECODE: v };
        } else {
          this.selectedModule = null;
          this.selectedItem = null;
        }
      },
      immediate: true
    },
    selectedItem(v) {
      if (v) this.loadDetail(v);
    }
  },
  methods: {
    onSelectItem(item) {
      this.selectedItem = item;
    },
    onCount(payload) {
      this.$emit('count', payload);
    },
    async loadDetail(row) {
      this.mode = 'change';
      try {
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadCurrentState',
          param: { id: '' },
          isBusy: false
        }).catch(function() {});
        await this.$callAction({
          action: VHP.STORE_NAME + '/loadDetail',
          param: { id: row.ID, fallback: null },
          isBusy: false
        });
      } catch (e) {
        // loadDetail 失败时已弹错误提示
      }
    },
    async setMode(m) {
      this.mode = m;
      if (m === 'current' && !this.currentContent && !this.currentExists) {
        this.loadingCurrent = true;
        try {
          await this.$callAction({
            action: VHP.STORE_NAME + '/loadCurrentState',
            param: { id: this.selectedItem.ID },
            isBusy: false
          });
        } catch (e) {
          this.mode = 'change';
        } finally {
          this.loadingCurrent = false;
        }
      }
    },
    openVersionCenter() {
      this.$emit('open-editor', { type: 'version-center' });
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'ver_' + (this.selectedItem && this.selectedItem.ID),
        label: '版本 ' + (this.selectedItem && this.selectedItem.OBJCODE) + ' v' + (this.selectedItem && this.selectedItem.VERSION),
        icon: 'h-icon-clock'
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-version {
  display: flex;
  flex: 1;
  min-height: 0;
  background: @st-bg-white;
}

.st-ed-list {
  width: 240px;
  flex-shrink: 0;
  border-right: 1px solid @st-border-light;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.st-ed-form {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
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

.vdiff-inline {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}

.vdiff-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 12px;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.vdiff-bar-code {
  font-size: 13px;
  font-weight: 700;
  color: @st-text;
  font-family: @st-mono;
}

.vdiff-bar-version {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  background: #f9f0ff;
  color: #722ed1;
  font-weight: 600;
}

.vdiff-bar-meta {
  font-size: 11px;
  color: @st-text-hint;
}

.vdiff-bar-flex { flex: 1; }

.vdiff-tag {
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 10px;
  font-weight: 600;
  background: #e8eaec;
  color: #515a6e;
  &.op-insert, &.op-create { background: #f6ffed; color: #52c41a; }
  &.op-update { background: #e6f7ff; color: #1890ff; }
  &.op-delete { background: #fff1f0; color: #ff4d4f; }
  &.op-rollback { background: #f9f0ff; color: #722ed1; }
}

.vdiff-note {
  padding: 6px 12px;
  font-size: 11px;
  color: #515a6e;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.vdiff-tip {
  padding: 6px 12px;
  font-size: 11px;
  color: @st-text-hint;
  flex-shrink: 0;
}

.vdiff-body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.vdiff-loading {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-primary;
}
</style>
