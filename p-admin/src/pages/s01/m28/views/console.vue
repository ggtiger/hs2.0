<template>
  <div class="st-console">
    <!-- Header -->
    <div class="st-header">
      <div class="st-header-left">
        <i class="h-icon-cube"></i>
        <span class="st-header-title">模块开发中心</span>
        <template v-if="selectedModule">
          <span class="st-header-sep">/</span>
          <span class="st-header-mod">{{ selectedModule.MODULECODE }}</span>
          <span class="st-header-modname" v-if="selectedModule.MODULENAME">{{ selectedModule.MODULENAME }}</span>
        </template>
      </div>
      <div class="st-header-right">
        <Button size="s" @click="openCodeEditor"><i class="h-icon-github"></i> 在线开发</Button>
        <Button size="s" color="primary" @click="openWizard">
          <i class="h-icon-magic"></i> AI 向导
        </Button>
        <Button size="s" @click="aiVisible = !aiVisible">
          <i class="h-icon-bubble"></i>
        </Button>
      </div>
    </div>

    <div class="st-body">
      <!-- 左导航 -->
      <studio-nav
        :sections="SECTIONS"
        :active="activeSection"
        :counts="counts"
        :collapsed="navCollapsed"
        :has-module="!!moduleCode"
        @select="onSectionChange"
        @new-module="onNewModule"
        @toggle-collapse="navCollapsed = !navCollapsed"
      />

      <!-- 主内容区 -->
      <div class="st-main">
        <!-- 模块选择 -->
        <template v-if="activeSection === 'module'">
          <module-editor
            :module-code="moduleCode"
            :sections="SECTION_CARDS"
            :counts="counts"
            @module-selected="onModuleSelected"
            @goto-section="onGotoSection"
            @open-config="openModuleConfigByCode"
            @open-edit="openModuleEditByCode"
            @new-module="onNewModule"
          />
        </template>
        <!-- 其他分类：全高编辑器 -->
        <template v-else>
          <component
            :is="currentEditor"
            v-if="moduleCode"
            :key="activeSection + '_' + moduleCode"
            :item="selectedItem"
            :module-code="moduleCode"
            @saved="onEditorSaved"
            @open-editor="onOpenEditor"
            @ask-ai="onAskAI"
            @count="onCount"
          />
          <div v-else class="st-placeholder">
            <i class="h-icon-cube"></i>
            <p class="st-placeholder-title">请先选择模块</p>
            <p class="st-placeholder-hint">在左侧"模块"分类中选择或新建模块</p>
          </div>
        </template>
      </div>

      <!-- AI 侧滑 -->
      <transition name="slide-right">
        <ai-slide
          v-if="aiVisible"
          ref="aiSlide"
          :selected-module="selectedModule"
          @close="aiVisible = false"
          @open-wizard="openWizard"
        />
      </transition>
    </div>

    <!-- 全屏 Modal: 模块配置 -->
    <Modal v-model="cfgModalVisible" :title="cfgModalTitle" fullScreen hasCloseIcon>
      <mod-config v-if="cfgModalVisible" ref="modConfig" :moduleCodeProp="cfgModalModuleCode"
        @close="cfgModalVisible = false" @saved="onConfigSaved" @save-error="cfgModalSaving = false"></mod-config>
      <div slot="footer">
        <Button @click="cfgModalVisible = false">关闭</Button>
        <Button color="primary" class="ml5" :loading="cfgModalSaving" @click="saveConfig">保存</Button>
      </div>
    </Modal>

    <!-- 全屏 Modal: AI 向导 -->
    <Modal v-model="wizardModalVisible" title="AI 模块开发向导" fullScreen hasCloseIcon>
      <module-wizard v-if="wizardModalVisible" ref="modWizard" @done="onWizardDone"></module-wizard>
      <div slot="footer">
        <Button @click="wizardModalVisible = false">关闭</Button>
      </div>
    </Modal>

    <!-- 全屏 Modal: 代码在线开发 -->
    <Modal v-model="codeEditorVisible" title="代码在线开发" fullScreen hasCloseIcon>
      <div class="st-code-editor-frame" v-if="codeEditorVisible">
        <sfc-edit ref="sfcEdit" />
      </div>
    </Modal>

    <!-- rs-modal: 模块编辑（与 m02 main.vue 一致，add.vue 的 closeW 依赖 $parent.setvalue） -->
    <rs-modal :title="modEditTitle" ref="modEditModal" :width="900" @input="onModEditModalChange">
      <rs-mod-add v-if="modEditOpened"></rs-mod-add>
    </rs-modal>
  </div>
</template>

<script>
import StudioNav from './components/studio-nav.vue';
import AiSlide from './components/ai-slide.vue';
import ModuleEditor from './editors/module-editor.vue';
import ResourceEditor from './editors/resource-editor.vue';
import CodeEditor from './editors/code-editor.vue';
import PageEditor from './editors/page-editor.vue';
import DictEditor from './editors/dict-editor.vue';
import SceneEditor from './editors/scene-editor.vue';
import MenuEditor from './editors/menu-editor.vue';
import VersionEditor from './editors/version-editor.vue';
import TemplateEditor from './editors/template-editor.vue';
import RsModAdd from '@/pages/s01/m02/views/add.vue';
import ModConfig from '@/pages/s01/m18/views/config.vue';
import ModuleWizard from '@/pages/s01/m18/views/components/module-wizard.vue';
import SfcEdit from '@/pages/s01/m17/views/edit.vue';
import { getGenericStore } from '@/components/generic-module/generic-store';
import { SECTIONS, SECTION_CARDS, SECTION_DEFS } from '@/constants';

var EDITOR_MAP = {
  ResourceEditor: ResourceEditor,
  CodeEditor: CodeEditor,
  PageEditor: PageEditor,
  DictEditor: DictEditor,
  SceneEditor: SceneEditor,
  MenuEditor: MenuEditor,
  VersionEditor: VersionEditor,
  TemplateEditor: TemplateEditor
};

export default {
  name: 's01-m28-main',
  components: {
    StudioNav: StudioNav,
    AiSlide: AiSlide,
    ModuleEditor: ModuleEditor,
    ResourceEditor: ResourceEditor,
    CodeEditor: CodeEditor,
    PageEditor: PageEditor,
    DictEditor: DictEditor,
    SceneEditor: SceneEditor,
    MenuEditor: MenuEditor,
    VersionEditor: VersionEditor,
    TemplateEditor: TemplateEditor,
    RsModAdd: RsModAdd,
    ModConfig: ModConfig,
    ModuleWizard: ModuleWizard,
    SfcEdit: SfcEdit
  },
  data() {
    return {
      SECTIONS: SECTIONS,
      SECTION_CARDS: SECTION_CARDS,
      SECTION_DEFS: SECTION_DEFS,
      selectedModuleCode: '',
      selectedModuleObj: null,
      activeSection: 'module',
      selectedItem: null,
      counts: { module: 0, resource: 0, page: 0, code: 0, menu: 0, version: 0, template: 0, dict: 0, scene: 0 },
      aiVisible: false,
      navCollapsed: false,
      cfgModalVisible: false,
      cfgModalModuleCode: '',
      cfgModalSaving: false,
      modEditOpened: false,
      wizardModalVisible: false,
      codeEditorVisible: false
    };
  },
  computed: {
    moduleCode() {
      return this.selectedModuleCode || '';
    },
    selectedModule() {
      return this.selectedModuleObj;
    },
    currentEditor() {
      var def = SECTION_DEFS[this.activeSection];
      return def && def.editor ? EDITOR_MAP[def.editor] : ResourceEditor;
    },
    cfgModalTitle() {
      return this.cfgModalModuleCode ? '模块配置 - ' + this.cfgModalModuleCode : '模块配置';
    },
    modEditTitle() {
      return this.selectedModuleCode ? '模块编辑 - ' + this.selectedModuleCode : '模块编辑';
    }
  },
  watch: {
    selectedModuleCode(v) {
      this.selectedItem = null;
      if (v) this.loadAllCounts();
    }
  },
  created() {
    this.M02_STORE = getGenericStore('RS_M02');
  },
  methods: {
    onModuleSelected(code, mod) {
      this.selectedModuleCode = code;
      this.selectedModuleObj = mod || null;
    },
    onGotoSection(key) {
      if (!this.moduleCode) return;
      this.activeSection = key;
      this.selectedItem = null;
    },
    async loadAllCounts() {
      var mc = this.moduleCode;
      if (!mc) return;
      var self = this;
      var keys = Object.keys(SECTION_DEFS);
      for (var i = 0; i < keys.length; i++) {
        var key = keys[i];
        var cfg = SECTION_DEFS[key];
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
          self.$set(self.counts, key, transformed.filter(function(item) { return !item.isGroupHeader }).length);
        } catch (e) {
          self.$set(self.counts, key, 0);
        }
      }
    },
    onSectionChange(key) {
      if (key !== 'module' && !this.moduleCode) return;
      this.activeSection = key;
      this.selectedItem = null;
    },
    onCount(payload) {
      if (payload && payload.key !== undefined) {
        this.$set(this.counts, payload.key, payload.n);
      }
    },
    onEditorSaved() {},
    onOpenEditor(payload) {
      if (!payload || !payload.type) return;
      switch (payload.type) {
        case 'module-config':
          this.openModuleConfigByCode(this.moduleCode);
          break;
        case 'code':
          var path = '/s01/m17/edit';
          if (payload.code) path += '?code=' + encodeURIComponent(payload.code);
          this.$router.push(path);
          break;
        case 'sfc':
          if (payload.modulePath) {
            this.$router.push('/s01/m17/edit?modulePath=' + encodeURIComponent(payload.modulePath));
          }
          break;
        case 'version-center':
          this.$router.push('/s01/m22/main');
          break;
        case 'template-market':
          this.$router.push('/s01/m25/main');
          break;
        default:
          // eslint-disable-next-line no-console
          console.warn('[DevCenter] Unknown editor type:', payload.type);
      }
    },
    onAskAI(focus) {
      this.aiVisible = true;
      if (focus && focus.key) {
        var self = this;
        this.$nextTick(function() {
          if (self.$refs.aiSlide) {
            self.$refs.aiSlide.addFocus(focus);
          }
        });
      }
    },
    openModuleConfigByCode(code) {
      if (!code) return;
      this.cfgModalModuleCode = code;
      this.cfgModalSaving = false;
      this.cfgModalVisible = true;
    },
    saveConfig() {
      if (this.$refs.modConfig && typeof this.$refs.modConfig.handleSave === 'function') {
        this.cfgModalSaving = true;
        try { this.$refs.modConfig.handleSave() } catch (e) { this.cfgModalSaving = false }
      }
    },
    onConfigSaved() {
      this.cfgModalSaving = false;
      this.cfgModalVisible = false;
    },
    openModuleEditByCode(code) {
      if (!code) return;
      this.$callAction({
        action: 's01/m02/open',
        param: { DID: code },
        isBusy: false
      }).then(function() {
        this.modEditOpened = true;
        this.$nextTick(function() {
          this.$refs.modEditModal.show();
        }.bind(this));
      }.bind(this)).catch(function(e) {
        this.$Message.error('加载模块失败: ' + (e.message || e));
      }.bind(this));
    },
    onModEditModalChange(val) {
      if (!val) {
        this.modEditOpened = false;
      }
    },
    openWizard() { this.wizardModalVisible = true },
    onWizardDone() {},
    onNewModule() { this.wizardModalVisible = true },
    openCodeEditor() { this.codeEditorVisible = true },
    onCodeEditorSaved() {
      // 代码保存后刷新代码分类计数
      if (this.moduleCode) this.loadAllCounts();
    }
  }
};
</script>

<style lang="less" scoped>
@import './studio-common.less';

.st-console {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: @st-bg;
}

.st-header {
  background: @st-bg-white;
  border-bottom: 1px solid @st-border;
  padding: 0 @st-space-lg;
  height: @st-header-h;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.st-header-left {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
  > i { font-size: 18px; color: @st-primary; }
}

.st-header-title {
  font-size: 15px;
  font-weight: 600;
  color: @st-text;
}

.st-header-sep {
  color: @st-text-hint;
  font-size: 12px;
}

.st-header-mod {
  font-size: 14px;
  font-weight: 700;
  color: @st-primary;
  font-family: @st-mono;
}

.st-header-modname {
  font-size: 12px;
  color: @st-text-sec;
}

.st-header-right {
  display: flex;
  align-items: center;
  gap: @st-space-sm;
}

.st-body {
  flex: 1;
  display: flex;
  min-height: 0;
}

.st-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  background: @st-bg-white;
}

.st-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 20px;
  text-align: center;
  color: @st-text-hint;
  i { font-size: 40px; color: #d6e4ff; }
  .st-placeholder-title {
    margin: 8px 0 4px;
    font-size: 14px;
    font-weight: 600;
    color: @st-text-sec;
  }
  .st-placeholder-hint {
    margin: 0;
    font-size: 11px;
    color: @st-text-disabled;
  }
}

/* AI 侧滑过渡 */
.slide-right-enter-active,
.slide-right-leave-active {
  transition: transform 0.25s ease, opacity 0.25s ease;
}
.slide-right-enter,
.slide-right-leave-to {
  transform: translateX(100%);
  opacity: 0;
}

.st-code-editor-frame {
  height: calc(100vh);
  overflow: hidden;
}
</style>
