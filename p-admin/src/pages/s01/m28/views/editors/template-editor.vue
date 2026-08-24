<template>
  <div class="st-ed-template" v-if="moduleCode">
    <!-- 左：模板列表 -->
    <div class="st-ed-list">
      <section-list
        ref="sectionList"
        section="template"
        :selected-module="selectedModule"
        :active-item-id="activeItemId"
        :section-defs="sectionDefs"
        @select="onSelectItem"
        @count="onCount"
      />
    </div>
    <!-- 右：模板预览 -->
    <div class="st-ed-form" v-if="selectedItem">
      <div class="tpl-inline">
        <!-- 工具栏 -->
        <div class="tpl-bar">
          <span class="tpl-bar-code">{{ selectedItem.TEMPLATECODE }}</span>
          <span v-if="selectedItem.CATEGORY" :class="['tpl-tag', 'cat-' + selectedItem.CATEGORY]">{{ selectedItem.CATEGORY }}</span>
          <span v-if="selectedItem.VERSION" class="tpl-bar-version">v{{ selectedItem.VERSION }}</span>
          <span v-if="selectedItem.ENABLED == 0" class="tpl-tag disabled">禁用</span>
          <span class="tpl-bar-meta" v-if="selectedItem.CREATER">{{ selectedItem.CREATER }} · {{ selectedItem.CREATETIME }}</span>
          <span class="tpl-bar-flex"></span>
          <Button size="s" @click="openMarket"><i class="h-icon-folder"></i> 模板市场</Button>
          <Button size="s" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
        </div>
        <!-- 模板名 + 来源 + 描述 -->
        <div class="tpl-meta" v-if="selectedItem.TEMPLATENAME">
          <span v-if="selectedItem.TEMPLATENAME"><i class="h-icon-document"></i> {{ selectedItem.TEMPLATENAME }}</span>
          <span v-if="selectedItem.SOURCEINFO" class="tpl-meta-src"><i class="h-icon-link"></i> {{ selectedItem.SOURCEINFO }}</span>
        </div>
        <div class="tpl-desc" v-if="selectedItem.DESCRIPTION">{{ selectedItem.DESCRIPTION }}</div>
        <!-- 预览脚本 -->
        <div class="tpl-body">
          <pre class="tpl-script">{{ previewScript || (loadingPreview ? '加载中...' : '（无脚本内容）') }}</pre>
        </div>
      </div>
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-task"></i>
      <p>从左侧列表选择模板，在此处查看预览</p>
    </div>
  </div>
</template>

<script>
import SectionList from '../components/section-list.vue';
import { Constants as TplConstants, getStoreResult as getTplStore } from '@/pages/s01/m25/store';
import { TEMPLATE_DEF } from '@/constants';

var SECTION_DEFS = { template: TEMPLATE_DEF };

export default {
  name: 'TemplateEditor',
  components: { SectionList: SectionList },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      selectedItem: null,
      selectedModule: null,
      sectionDefs: SECTION_DEFS,
      previewScript: '',
      loadingPreview: false
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
    },
    selectedItem(v) {
      if (v) this.loadPreview(v);
    }
  },
  methods: {
    onSelectItem(item) {
      this.selectedItem = item;
    },
    onCount(payload) {
      this.$emit('count', payload);
    },
    async loadPreview(row) {
      this.previewScript = '';
      this.loadingPreview = true;
      try {
        getTplStore(); // 确保 s01/m25 store 已注册
        this.previewScript = await this.$callAction({
          action: TplConstants.STORE_NAME + '/loadPreviewScript',
          param: { id: row.ID },
          isBusy: false
        });
      } catch (e) {
        this.previewScript = '（脚本加载失败）';
      } finally {
        this.loadingPreview = false;
      }
    },
    openMarket() {
      this.$emit('open-editor', { type: 'template-market' });
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'tpl_' + (this.selectedItem && this.selectedItem.ID),
        label: '模板 ' + (this.selectedItem && this.selectedItem.TEMPLATECODE),
        icon: 'h-icon-folder'
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-template {
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

.tpl-inline {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}

.tpl-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 12px;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.tpl-bar-code {
  font-size: 13px;
  font-weight: 700;
  color: @st-text;
  font-family: @st-mono;
}

.tpl-bar-version {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  background: #f9f0ff;
  color: #722ed1;
  font-weight: 600;
}

.tpl-bar-meta {
  font-size: 11px;
  color: @st-text-hint;
}

.tpl-bar-flex { flex: 1; }

.tpl-tag {
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 10px;
  font-weight: 600;
  background: #e6f7ff;
  color: #1890ff;
  &.disabled { background: #f5f5f5; color: #999; }
}

.tpl-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 4px 12px;
  font-size: 12px;
  color: @st-text-sec;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
  i { font-size: 12px; color: @st-text-hint; }
}

.tpl-desc {
  padding: 6px 12px;
  font-size: 12px;
  color: #515a6e;
  line-height: 1.6;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.tpl-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 8px;
}

.tpl-script {
  margin: 0;
  padding: 10px 12px;
  background: #f8f8f9;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
  color: #333;
  max-height: none;
}
</style>
