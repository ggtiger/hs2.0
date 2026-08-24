<template>
  <div class="st-ed-dict" v-if="moduleCode">
    <!-- 左：字典列表 -->
    <div class="st-ed-list">
      <section-list
        ref="sectionList"
        section="dict"
        :selected-module="selectedModule"
        :active-item-id="activeItemId"
        :section-defs="sectionDefs"
        @select="onSelectItem"
        @count="onCount"
      />
    </div>
    <!-- 右：字典编辑（内嵌 m06 add.vue） -->
    <div class="st-ed-form" v-if="selectedItem">
      <dict-add-adapter
        :key="selectedItem.ID"
        :item-id="selectedItem.ID"
        @saved="onSaved"
        @ask-ai="onAskAI"
      />
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-inbox"></i>
      <p>从左侧列表选择字典，在此处编辑</p>
    </div>
  </div>
</template>

<script>
import SectionList from '../components/section-list.vue';
import DictAddAdapter from './dict-add-adapter.vue';
import { DICT_DEF } from '@/constants';

var SECTION_DEFS = { dict: DICT_DEF };

export default {
  name: 'DictEditor',
  components: {
    SectionList: SectionList,
    DictAddAdapter: DictAddAdapter
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
      this.$emit('saved', { section: 'dict' });
    },
    onAskAI(focus) {
      this.$emit('ask-ai', focus);
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-dict {
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
