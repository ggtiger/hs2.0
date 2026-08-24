<template>
  <div class="st-ed-scene" v-if="moduleCode">
    <!-- 左：场景列表 -->
    <div class="st-ed-list">
      <section-list
        ref="sectionList"
        section="scene"
        :selected-module="selectedModule"
        :active-item-id="activeItemId"
        :section-defs="sectionDefs"
        @select="onSelectItem"
        @count="onCount"
      />
    </div>
    <!-- 右：场景编辑 -->
    <div class="st-ed-form" v-if="selectedItem">
      <!-- 加载中 -->
      <div v-if="loading" class="st-ed-state">
        <i class="h-icon-loading"></i> 加载场景配置...
      </div>
      <div v-else class="st-ed-body">
        <!-- 基本属性表单 -->
        <section class="st-ed-section">
          <header class="st-ed-section-head">
            <span class="st-ed-section-title"><i class="h-icon-info"></i> 基本属性</span>
            <div class="st-ed-section-actions">
              <h-switch :value="form.ENABLED == 1" @input="form.ENABLED = $event ? 1 : 0" small>
                {{ form.ENABLED == 1 ? '启用' : '停用' }}
              </h-switch>
              <Button size="xs" color="primary" @click="handleSave" :disabled="saving || !dirty">
                <i :class="saving ? 'h-icon-loading' : 'h-icon-save'"></i> 保存
              </Button>
              <Button size="xs" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
            </div>
          </header>
          <div class="st-ed-form-grid">
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">场景编码</label>
              <input class="st-ed-input" v-model="form.SCENECODE" disabled />
            </div>
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">场景名称</label>
              <input class="st-ed-input" v-model="form.SCENENAME" placeholder="场景名称" />
            </div>
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">模型 ID</label>
              <input class="st-ed-input mono" v-model="form.MODELID" placeholder="LLM 模型 ID" />
            </div>
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">降级模型</label>
              <input class="st-ed-input mono" v-model="form.FALLBACKID" placeholder="降级模型 ID（可选）" />
            </div>
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">传输方式</label>
              <input class="st-ed-input" v-model="form.TRANSPORT" placeholder="如 chat/completion" />
            </div>
            <div class="st-ed-form-item">
              <label class="st-ed-form-label">工具集</label>
              <input class="st-ed-input" v-model="form.TOOLSET" placeholder="如 builtin/aidev" />
            </div>
            <div class="st-ed-form-item st-ed-form-item-full">
              <label class="st-ed-form-label">上下文源</label>
              <input class="st-ed-input mono" v-model="form.CONTEXTSOURCE" placeholder="上下文源标识" />
            </div>
          </div>
        </section>
        <!-- 模型参数 JSON -->
        <section class="st-ed-section st-ed-section-params">
          <header class="st-ed-section-head">
            <span class="st-ed-section-title"><i class="h-icon-setting"></i> 模型参数 (JSON)</span>
            <Button size="xs" @click="formatParams" title="格式化 JSON">格式化</Button>
          </header>
          <div class="st-ed-json-editor">
            <sfc-code-editor
              ref="paramsEditor"
              v-model="paramsJson"
              file-type="JS"
            />
          </div>
        </section>
        <!-- 前端工具 -->
        <section class="st-ed-section">
          <header class="st-ed-section-head">
            <span class="st-ed-section-title"><i class="h-icon-component"></i> 前端工具</span>
          </header>
          <div class="st-ed-tools-area">
            <div class="st-ed-tools-tags">
              <span v-for="(t, i) in frontendTools" :key="i" class="st-ed-tool-tag">
                {{ t }}
                <button class="st-ed-tool-remove" @click="removeTool(i)">✕</button>
              </span>
              <input
                class="st-ed-tool-input"
                v-model="newTool"
                placeholder="输入工具名后回车添加"
                @keydown.enter.prevent="addTool"
              />
            </div>
          </div>
        </section>
      </div>
    </div>
    <div v-else class="st-ed-empty">
      <i class="h-icon-star"></i>
      <p>从左侧列表选择场景，在此处编辑</p>
    </div>
  </div>
</template>

<script>
import SectionList from '../components/section-list.vue';
import SfcCodeEditor from '@/pages/s01/m17/components/sfc-code-editor.vue';
import { getGenericStore } from '@/components/generic-module/generic-store';
import { SCENE_DEF } from '@/constants';

var MC = 'RS_M23';

var SECTION_DEFS = { scene: SCENE_DEF };

export default {
  name: 'SceneEditor',
  components: { SectionList: SectionList, SfcCodeEditor: SfcCodeEditor },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      selectedModule: null,
      selectedItem: null,
      sectionDefs: SECTION_DEFS,
      loading: false,
      saving: false,
      form: {
        SCENECODE: '',
        SCENENAME: '',
        MODELID: '',
        FALLBACKID: '',
        TRANSPORT: '',
        TOOLSET: '',
        CONTEXTSOURCE: '',
        ENABLED: 1
      },
      paramsJson: '',
      frontendTools: [],
      newTool: ''
    };
  },
  computed: {
    activeItemId() {
      return (this.selectedItem && this.selectedItem.ID) || '';
    },
    dirty() {
      if (!this.selectedItem) return false;
      var i = this.selectedItem;
      return this.form.SCENENAME !== (i.SCENENAME || '') ||
             this.form.MODELID !== (i.MODELID || '') ||
             this.form.FALLBACKID !== (i.FALLBACKID || '') ||
             this.form.TRANSPORT !== (i.TRANSPORT || '') ||
             this.form.TOOLSET !== (i.TOOLSET || '') ||
             this.form.CONTEXTSOURCE !== (i.CONTEXTSOURCE || '') ||
             (this.form.ENABLED !== 0) !== (i.ENABLED !== 0);
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
          this.resetForm();
        }
      },
      immediate: true
    },
    'selectedItem.ID'(v) {
      if (v) this.loadScene();
      else this.resetForm();
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
  },
  methods: {
    onSelectItem(item) {
      this.selectedItem = item;
    },
    onCount(payload) {
      this.$emit('count', payload);
    },
    loadScene() {
      if (!this.selectedItem) return;
      var i = this.selectedItem;
      this.form.SCENECODE = i.SCENECODE || '';
      this.form.SCENENAME = i.SCENENAME || '';
      this.form.MODELID = i.MODELID || '';
      this.form.FALLBACKID = i.FALLBACKID || '';
      this.form.TRANSPORT = i.TRANSPORT || '';
      this.form.TOOLSET = i.TOOLSET || '';
      this.form.CONTEXTSOURCE = i.CONTEXTSOURCE || '';
      this.form.ENABLED = i.ENABLED != null ? i.ENABLED : 1;
      this.paramsJson = this.formatJsonString(i.PARAMS);
      this.frontendTools = this.parseTools(i.FRONTENDTOOLLIST);
    },
    resetForm() {
      this.form = {
        SCENECODE: '',
        SCENENAME: '',
        MODELID: '',
        FALLBACKID: '',
        TRANSPORT: '',
        TOOLSET: '',
        CONTEXTSOURCE: '',
        ENABLED: 1
      };
      this.paramsJson = '';
      this.frontendTools = [];
    },
    parseTools(v) {
      if (!v) return [];
      if (Array.isArray(v)) return v.slice();
      try {
        var p = JSON.parse(v);
        return Array.isArray(p) ? p : [String(v)];
      } catch (e) {
        return String(v).split(',').map(function(s) { return s.trim(); }).filter(Boolean);
      }
    },
    formatJsonString(v) {
      if (!v) return '';
      if (typeof v === 'string') {
        try { return JSON.stringify(JSON.parse(v), null, 2); } catch (e) { return v; }
      }
      try { return JSON.stringify(v, null, 2); } catch (e) { return String(v); }
    },
    formatParams() {
      try {
        var obj = JSON.parse(this.paramsJson);
        this.paramsJson = JSON.stringify(obj, null, 2);
        this.$Message.success('JSON 已格式化');
      } catch (e) {
        this.$Message.error('JSON 格式错误: ' + (e.message || e));
      }
    },
    addTool() {
      var t = (this.newTool || '').trim();
      if (!t) return;
      if (this.frontendTools.indexOf(t) >= 0) {
        this.$Message.error('工具已存在');
        return;
      }
      this.frontendTools.push(t);
      this.newTool = '';
    },
    removeTool(idx) {
      this.frontendTools.splice(idx, 1);
    },
    async handleSave() {
      if (this.saving || !this.dirty) return;
      if (this.paramsJson && this.paramsJson.trim()) {
        try { JSON.parse(this.paramsJson); } catch (e) {
          this.$Message.error('PARAMS JSON 格式错误: ' + (e.message || e));
          return;
        }
      }
      this.saving = true;
      try {
        await this.$callAction({
          action: MC + '/call',
          param: { APICODE: 'A02', params: { FilterParams: { ID: this.selectedItem.ID } } },
          isBusy: false
        });
        var dt = this.storeObj.storeHelper.getTable('MAIN');
        if (!dt) { this.$Message.error('DataTable 未初始化'); return; }
        try { dt.setValue('SCENENAME', this.form.SCENENAME); } catch (e) { /* ignore */ }
        try { dt.setValue('MODELID', this.form.MODELID); } catch (e) { /* ignore */ }
        try { dt.setValue('FALLBACKID', this.form.FALLBACKID); } catch (e) { /* ignore */ }
        try { dt.setValue('TRANSPORT', this.form.TRANSPORT); } catch (e) { /* ignore */ }
        try { dt.setValue('TOOLSET', this.form.TOOLSET); } catch (e) { /* ignore */ }
        try { dt.setValue('CONTEXTSOURCE', this.form.CONTEXTSOURCE); } catch (e) { /* ignore */ }
        try { dt.setValue('ENABLED', this.form.ENABLED); } catch (e) { /* ignore */ }
        try { dt.setValue('PARAMS', this.paramsJson); } catch (e) { /* ignore */ }
        try { dt.setValue('FRONTENDTOOLLIST', JSON.stringify(this.frontendTools)); } catch (e) { /* ignore */ }
        await this.$callAction({ action: MC + '/save', isBusy: false });
        this.$Message.success('保存成功');
        this.$emit('saved', { section: 'scene' });
      } catch (e) {
        this.$Message.error('保存失败: ' + (e.message || e));
      } finally {
        this.saving = false;
      }
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'scene_' + (this.selectedItem.SCENECODE || ''),
        label: '场景: ' + (this.selectedItem.SCENENAME || this.selectedItem.SCENECODE),
        icon: 'h-icon-star',
        type: 'scene',
        name: this.selectedItem.SCENECODE
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-scene {
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

.st-ed-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  color: @st-text-hint;
  font-size: 12px;
}

.st-ed-body {
  flex: 1;
  overflow-y: auto;
  padding: @st-space-md;
  display: flex;
  flex-direction: column;
  gap: @st-space-md;
}

.st-ed-section {
  background: @st-bg-white;
  border: 1px solid @st-border-light;
  border-radius: @st-radius;
  overflow: hidden;
}

.st-ed-section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: @st-space-sm;
  padding: 6px @st-space-md;
  background: @st-bg-gray;
  border-bottom: 1px solid @st-border-light;
  font-size: 12px;
  font-weight: 600;
  color: @st-text;
  .st-ed-section-title {
    display: flex;
    align-items: center;
    gap: 4px;
    i { color: @st-primary; font-size: 12px; }
  }
}

.st-ed-section-actions {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
}

.st-ed-form-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: @st-space-sm @st-space-lg;
  padding: @st-space-md;
}

.st-ed-form-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  &.st-ed-form-item-full { grid-column: 1 / -1; }
  .st-ed-form-label {
    font-size: 10px;
    color: @st-text-hint;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
}

.st-ed-input {
  .st-input();
  &.mono { font-family: @st-mono; }
  &:disabled { background: @st-bg-gray; color: @st-text-hint; }
}

.st-ed-section-params {
  display: flex;
  flex-direction: column;
}

.st-ed-json-editor {
  height: 220px;
  overflow: hidden;
  & > :first-child { height: 100%; }
}

.st-ed-tools-area {
  padding: 10px @st-space-md;
}

.st-ed-tools-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.st-ed-tool-tag {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  background: @st-primary-pale;
  color: @st-primary;
  border: 1px solid #d6e4ff;
  border-radius: @st-radius-pill;
  font-size: 11px;
  font-weight: 600;
  .st-ed-tool-remove {
    border: none;
    background: transparent;
    cursor: pointer;
    color: @st-text-hint;
    font-size: 10px;
    padding: 0;
    &:hover { color: @st-error; }
  }
}

.st-ed-tool-input {
  .st-input();
  height: 28px;
  flex: 1;
  min-width: 160px;
  border-style: dashed;
  &:focus { border-style: solid; }
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
