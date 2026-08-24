<template>
  <div class="st-ed-resource" v-if="moduleCode">
    <!-- 左：资源列表 -->
    <div class="st-ed-list">
      <section-list
        ref="sectionList"
        section="resource"
        :selected-module="selectedModule"
        :active-item-id="activeItemId"
        :section-defs="sectionDefs"
        @select="onSelectItem"
        @count="onCount"
      />
    </div>
    <!-- 右：资源编辑（内嵌 m01 add.vue） -->
    <div class="st-ed-form" v-if="selectedItem">
      <res-add-adapter
        :key="selectedItem.ID"
        :did="selectedItem.RESOURCENAME"
        @saved="onSaved"
        @ask-ai="onAskAI"
        @related="onRelated"
      />
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-link"></i>
      <p>从左侧列表选择资源，在此处编辑</p>
    </div>
  </div>
</template>

<script>
import SectionList from '../components/section-list.vue';
import ResAddAdapter from './res-add-adapter.vue';
import { RESOURCE_DEF as _RESOURCE_DEF } from '@/constants';

/* eslint-disable */
var RESOURCE_DEF = Object.assign({}, _RESOURCE_DEF, {
  transform: function(rows) {
    var seen = {}
    return rows.filter(function(r) { return r && r.RESOURCEID && !seen[r.RESOURCEID] && (seen[r.RESOURCEID] = true) })
      .map(function(r) { return Object.assign({}, r, { ID: r.RESOURCEID, _sourceType: 'path' }) })
  }
});
/* eslint-enable */

var SECTION_DEFS = { resource: RESOURCE_DEF };

export default {
  name: 'ResourceEditor',
  components: {
    SectionList: SectionList,
    ResAddAdapter: ResAddAdapter
  },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      selectedItem: null,
      selectedModule: null,
      sectionDefs: SECTION_DEFS
    };
  },
  computed: {
    activeItemId() {
      return (this.selectedItem && this.selectedItem.ID) || '';
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
    }
  },
  methods: {
    onSelectItem(item) {
      this.selectedItem = item;
    },
    onCount(payload) {
      this.$emit('count', payload);
    },
    onSaved() {
      if (this.$refs.sectionList) {
        this.$refs.sectionList.loadList();
      }
      this.$emit('saved', { section: 'resource' });
    },
    onAskAI(focus) {
      this.$emit('ask-ai', focus);
    },
    onRelated(payload) {
      var list = this.$refs.sectionList;
      if (!list || !list.items) return;
      var items = list.items;
      var seenIds = {};
      items.forEach(function(r) { seenIds[r.ID] = true; });
      var added = false;
      payload.items.forEach(function(ri) {
        if (!seenIds[ri.ID]) {
          items.push(ri);
          seenIds[ri.ID] = true;
          added = true;
        }
      });
      if (added) {
        list.items = items;
        this.$emit('count', { key: 'resource', n: items.length });
      }
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-resource {
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
  overflow: auto;
  & > :first-child {
    // res-add-adapter 内的 view-dialog 去掉 panel 边框
  }
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
