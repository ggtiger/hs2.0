<template>
  <rs-modal ref="modal" v-model="modalVisible" :width="900">
    <view-dialog :title="title" class="d-width">
      <template slot="body">
        <div class="sfc-popup-body" :class="{ 'sfc-popup-body-multi': editTarget === 'sfc' }">
      <!-- 文件列表侧边栏（仅 SFC 组件编辑） -->
      <div class="sfc-popup-filelist" v-if="editTarget === 'sfc'">
        <div class="sfc-popup-filelist-header">
          <span>文件 ({{ fileList.length }})</span>
          <Button size="s" icon="h-icon-plus" @click="newFile" title="新建"></Button>
        </div>
        <div class="sfc-popup-filelist-items">
          <div
            v-for="f in fileList"
            :key="f.ID"
            :class="['sfc-popup-file-item', { active: f.MODULEPATH === modulePath }]"
            @click="switchFile(f.MODULEPATH)"
            :title="f.MODULEPATH"
          >
            <i :class="f.FILETYPE === 'VUE' ? 'h-icon-edit' : 'h-icon-code'"></i>
            <span class="sfc-popup-file-name">{{ f.MODULEPATH.replace(dirPrefix, '') }}</span>
            <span v-if="f.MODULEPATH === mainModulePath" class="sfc-popup-file-main" title="主组件">★</span>
          </div>
          <div v-if="fileList.length === 0 && !fileListLoading" class="sfc-popup-file-empty">暂无文件</div>
          <div v-if="fileListLoading" class="sfc-popup-file-empty">加载中...</div>
        </div>
      </div>
      <!-- 主区域（工具栏 + 内容 + 状态栏） -->
      <div class="sfc-popup-main">
      <!-- 工具栏 -->
      <div class="sfc-popup-toolbar">
        <div class="sfc-popup-toolbar-left">
          <select v-model="fileType" class="sfc-popup-input sfc-popup-input-type">
            <option value="VUE">VUE</option>
            <option value="JS">JS</option>
          </select>
          <input v-model="templateCode" placeholder="编码" class="sfc-popup-input sfc-popup-input-code" />
          <input v-model="templateName" placeholder="名称" class="sfc-popup-input sfc-popup-input-name" />
          <input v-model="modulePath" :placeholder="fileType === 'VUE' ? '@/pages/xxx/xxx.vue' : '@/modules/xxx/xxx.js'" class="sfc-popup-input sfc-popup-input-path" />
        </div>
        <div class="sfc-popup-toolbar-right">
          <Button size="s" :loading="compiling" @click="handleCompile">编译</Button>
          <Button size="s" color="primary" :loading="saving" @click="handleSave">保存</Button>
        </div>
      </div>
      <!-- 编辑器 + 预览 / AI 助手 (Tab 切换) -->
      <div class="sfc-popup-content">
        <!-- Tab 栏 -->
        <div class="sfc-popup-tab-bar">
          <span :class="['sfc-popup-tab', { active: popupTab === 'editor' }]" @click="popupTab = 'editor'">代码编辑</span>
          <span :class="['sfc-popup-tab', { active: popupTab === 'ai' }]" @click="popupTab = 'ai'">AI 助手</span>
        </div>
        <!-- 代码编辑 Tab: 编辑器 + 预览 横向布局 -->
        <div class="sfc-popup-editor-row" v-show="popupTab === 'editor'">
          <div class="sfc-popup-editor">
            <div class="sfc-popup-section-title">
              <span>{{ modulePath || '代码编辑' }}</span>
              <span v-if="dirty" class="sfc-popup-badge">未保存</span>
            </div>
            <sfc-code-editor
              ref="editor"
              v-model="sourceCode"
              :fileType="fileType"
              @change="onCodeChange"
            ></sfc-code-editor>
          </div>
          <div class="sfc-popup-preview" v-if="fileType === 'VUE'">
            <div class="sfc-popup-section-title">预览</div>
            <sfc-preview
              :source="sourceCode"
              :modulePath="modulePath"
            ></sfc-preview>
          </div>
        </div>
        <!-- AI 助手 Tab -->
        <div class="sfc-popup-ai" v-show="popupTab === 'ai'">
          <ai-chat-panel
            ref="aiPanel"
            :currentFile="aiCurrentFile"
            :moduleCode="moduleCode"
            :editTarget="editTarget"
            :pageCode="pageCode"
            @apply-code="onApplyCode"
          ></ai-chat-panel>
        </div>
      </div>
      <!-- 状态栏 -->
      <div class="sfc-popup-statusbar">
        <span v-if="statusMsg" :class="{ 'sfc-status-error': statusIsError }">{{ statusMsg }}</span>
        <span v-else>就绪</span>
        <span v-if="deps.length > 0">依赖: {{ deps.join(', ') }}</span>
      </div>
      </div><!-- /.sfc-popup-main -->
    </div>
      </template>
    </view-dialog>
  </rs-modal>
</template>
<script>
import { compileSFC, invalidateCacheByPrefix } from '@/sfc-loader';
import { Constants as CEP } from './code-editor-store';
import { mapDateTable as sepMapDateTable, getStoreResult as sepGetStoreResult, Constants as SEP } from './sfc-editor-store';
import sfcCodeEditor from '@/pages/s01/m17/components/sfc-code-editor.vue';
import sfcPreview from '@/pages/s01/m17/components/sfc-preview.vue';
import aiChatPanel from '@/pages/s01/m17/components/ai-chat-panel.vue';
import viewDialog from '@/components/views/view-dialog.vue';
import { DEFAULT_PAGE_JS_TEMPLATE, DEFAULT_STORE_JS_TEMPLATE, DEFAULT_VUE_TEMPLATE, SLOT_BUTTON_TEMPLATE, SLOT_TABLE_ACTION_TEMPLATE, SLOT_QUERY_TEMPLATE, SLOT_FORM_AREA_TEMPLATE, SLOT_FIELD_TEMPLATE } from '@/constants';

var defaultPageJsTemplate = DEFAULT_PAGE_JS_TEMPLATE;
var defaultStoreJsTemplate = DEFAULT_STORE_JS_TEMPLATE;
var defaultVueTemplate = DEFAULT_VUE_TEMPLATE;
var slotButtonTemplate = SLOT_BUTTON_TEMPLATE;
var slotTableActionTemplate = SLOT_TABLE_ACTION_TEMPLATE;
var slotQueryTemplate = SLOT_QUERY_TEMPLATE;
var slotFormAreaTemplate = SLOT_FORM_AREA_TEMPLATE;
var slotFieldTemplate = SLOT_FIELD_TEMPLATE;

// 根据 slot 名称获取对应模板
function getSlotTemplate(slotName) {
  if (!slotName) return defaultVueTemplate;
  if (slotName === 'header-action' || slotName === 'footer-action') {
    return slotButtonTemplate;
  }
  if (slotName === 'table-action') {
    return slotTableActionTemplate;
  }
  if (slotName === 'simple-query' || slotName === 'body-query') {
    return slotQueryTemplate;
  }
  if (slotName === 'form-top' || slotName === 'form-bottom') {
    return slotFormAreaTemplate;
  }
  if (slotName.indexOf('field:') === 0) {
    return slotFieldTemplate;
  }
  return defaultVueTemplate;
}

export default {
  name: 'sfc-editor-popup',
  components: { sfcCodeEditor, sfcPreview, aiChatPanel, viewDialog },
  props: {
    title: { type: String, default: 'SFC 编辑器' },
  },
  data() {
    return {
      modalVisible: false,
      // 编辑字段(templateId/templateCode/templateName/modulePath/sourceCode/fileType)
      // 已迁移为 computed 别名，读写弹窗专用 MAIN DataTable（sfc-editor-store，见下方 mapDateTable）
      deps: [],
      saving: false,
      compiling: false,
      statusMsg: '',
      statusIsError: false,
      dirty: false,
      popupTab: 'editor', // 'editor' | 'ai'
      moduleCode: '', // 模块编码 (AI 工具调用用)
      editTarget: '', // 编辑目标: extendjs/store/sfc/sfcmodulepath
      pageCode: '', // 页面编码 (extendjs 时有效)
      slotName: '', // 当前编辑的 slot 名称 (slot 扩展时有效，用于生成模板)
      // 多文件管理（仅 editTarget==='sfc'）
      fileList: [], // 当前目录下的文件列表
      dirPrefix: '', // @/modules/{moduleCode}/，文件列表过滤前缀
      mainModulePath: '', // 主组件路径（列表里标星）
      fileListLoading: false
    };
  },
  created() {
    // 强制注册弹窗专用 store（含 MAIN DataTable），保证 mapDateTable 立即可用
    sepGetStoreResult();
  },
  computed: {
    // 编辑字段绑弹窗专用 MAIN DataTable（mapDateTable 生成 get/set 自动读写 dt）
    ...sepMapDateTable('MAIN', ['ID', 'CODE', 'NAME', 'SOURCECODE', 'MODULEPATH', 'FILETYPE']),
    // 语义化双向别名（保留原模板/方法里的字段名，实际读写 MAIN DataTable）
    templateId: {
      get() { return this.ID || '' },
      set(v) { this.ID = v },
    },
    templateCode: {
      get() { return this.CODE || '' },
      set(v) { this.CODE = v },
    },
    templateName: {
      get() { return this.NAME || '' },
      set(v) { this.NAME = v },
    },
    modulePath: {
      get() { return this.MODULEPATH || '' },
      set(v) { this.MODULEPATH = v },
    },
    sourceCode: {
      get() { return this.SOURCECODE || '' },
      set(v) { this.SOURCECODE = v },
    },
    fileType: {
      get() { return this.FILETYPE || 'JS' },
      set(v) { this.FILETYPE = v },
    },
    aiCurrentFile() {
      if (!this.modulePath) return null;
      return {
        path: this.modulePath,
        type: this.fileType,
        content: this.sourceCode,
      };
    },
  },
  watch: {
    // 弹窗关闭时清空 sfcContext（全局抽屉的开发 agent 据此禁用/切回助理）
    modalVisible(v) {
      if (!v) this.$store.commit('sfcContext/CLEAR');
    }
  },
  methods: {
    // 供全局抽屉的开发 agent 取当前文件（和 s01/m17 edit.vue 接口一致）
    getAiCurrentFile() {
      return this.aiCurrentFile;
    },
    /**
     * 打开弹窗，加载或新建
     * @param {string} path - MODULEPATH
     * @param {string} fType - 'JS' | 'VUE'
     * @param {object} [context] - { moduleCode, editTarget, pageCode }
     */
    async show(path, fType, context) {
      // 重置 MAIN DataTable 为全新 ADD 行（编辑字段别名直接写入该行；找到已有记录后 open 会替换整行）
      await this.$callAction({ action: SEP.STORE_NAME + '/add', isBusy: false });
      this.modulePath = path || '';
      this.fileType = fType || 'JS';
      this.deps = [];
      this.fileList = [];
      this.dirty = false;
      this.statusMsg = '';
      this.statusIsError = false;
      this.popupTab = 'editor';
      // 从 context 接收编辑目标信息
      if (context) {
        this.moduleCode = context.moduleCode || '';
        this.editTarget = context.editTarget || '';
        this.pageCode = context.pageCode || '';
        this.mainModulePath = context.mainModulePath || '';
        this.slotName = context.slotName || '';
      } else {
        this.moduleCode = '';
        this.editTarget = '';
        this.pageCode = '';
        this.mainModulePath = '';
        this.slotName = '';
      }

      // SFC 组件编辑: 推导目录前缀，加载同目录文件列表
      if (this.editTarget === 'sfc' && path) {
        this.dirPrefix = path.substring(0, path.lastIndexOf('/') + 1);
        this.loadFileList();
      } else {
        this.dirPrefix = '';
      }

      // 尝试从数据库查找已有模板
      if (path) {
        await this.loadByModulePath(path);
      }

      // 如果没找到，初始化新模板
      if (!this.templateId) {
        if (this.fileType === 'VUE') {
          // 有 slotName 时使用对应 slot 模板，否则用通用 Vue 模板
          this.sourceCode = this.slotName ? getSlotTemplate(this.slotName) : defaultVueTemplate;
        } else if (path && path.endsWith('/store.js')) {
          this.sourceCode = defaultStoreJsTemplate;
        } else {
          this.sourceCode = defaultPageJsTemplate;
        }
        this.templateCode = path ? (this.moduleCode ? this.moduleCode + '_' : '') + path.split('/').pop().replace(/\.\w+$/, '') : 'new_' + Date.now();
        this.templateName = path ? path.split('/').pop() : '新模板';
        this.dirty = true;
      }

      this.$refs.modal.show();
      this.modalVisible = true;
      // 注册到全局 sfcContext，供全局抽屉的开发 agent 使用
      this.$store.commit('sfcContext/SET', {
        editorRef: this,
        moduleCode: this.moduleCode,
        siblingFiles: [],
        active: true
      });
      // eslint-disable-next-line no-restricted-syntax
      this.$store.dispatch('assistant/setAgent', 'sfc');

      this.$nextTick(function() {
        if (this.$refs.editor) {
          this.$refs.editor.setValue(this.sourceCode);
          // 自动合并插入钩子方法骨架（复用编辑器 SEARCH/REPLACE 合并能力）
          if (context && context.insertMethod) {
            this.insertMethodSkeleton(context.insertMethod);
          }
        }
      }.bind(this));

      // CodeMirror 在容器尺寸变化后（modal 动画结束、flex 布局就绪）需 refresh
      // 否则代码内容显示不全、不出现滚动条
      [200, 400, 800].forEach(function(delay) {
        setTimeout(function() {
          if (this.$refs.editor && this.$refs.editor.refresh) {
            this.$refs.editor.refresh();
          }
        }.bind(this), delay);
      }.bind(this));
    },
    // 利用编辑器的 SEARCH/REPLACE 合并能力，把方法骨架插入到 methods/computed 块
    // 复用 sfc-code-editor.applySearchReplace（与 AI 生成代码的合并机制一致）
    insertMethodSkeleton(spec) {
      if (!this.$refs.editor || !spec || !spec.name) return;
      var src = this.$refs.editor.getValue();
      // 幂等：方法已定义则跳过
      var defRe = new RegExp('\\b' + spec.name + '\\s*\\(');
      if (defRe.test(src)) {
        this.$Message('方法 ' + spec.name + ' 已存在，未重复插入');
        return;
      }
      // 候选锚点（块声明行），覆盖常见空格变体
      var block = spec.block || 'methods';
      var anchors = [
        '  ' + block + ': {',
        block + ': {',
        '  ' + block + ':{',
        block + ':{'
      ];
      var anchor = null;
      for (var i = 0; i < anchors.length; i++) {
        if (src.indexOf(anchors[i]) !== -1) { anchor = anchors[i]; break }
      }
      if (!anchor) {
        this.$Message('未找到 ' + block + ' 块，无法自动插入，请手动添加');
        return;
      }
      // 从锚点行提取缩进，方法缩进 = 块缩进 + 2 空格
      var indentMatch = anchor.match(/^(\s*)/);
      var methodIndent = (indentMatch ? indentMatch[1] : '') + '  ';
      var lines = (spec.snippet || '').split('\n');
      var indented = lines.map(function(l) {
        return l === '' ? '' : methodIndent + l;
      }).join('\n');
      var ret = this.$refs.editor.applySearchReplace([{ search: anchor, replace: anchor + '\n' + indented }]);
      if (ret && ret.applied > 0) {
        this.sourceCode = this.$refs.editor.getValue();
        this.dirty = true;
        this.$Message('已插入方法 ' + spec.name);
      } else {
        this.$Message('插入失败：未匹配到锚点');
      }
    },
    async loadByModulePath(path) {
      try {
        var ret = await this.$callAction({
          action: CEP.STORE_NAME + '/findAssetsByPath',
          param: { modulePath: path },
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        if (items.length > 0) {
          var record = items[0];
          // open 加载完整行到 MAIN DataTable（别名字段自动同步；行状态=modify，保存走 UPDATE）
          await this.$callAction({ action: SEP.STORE_NAME + '/open', param: { ID: record.ID }, isBusy: false });
          var dt = this.$MAIN;
          try {
            this.deps = dt.getValue('DEPS') ? JSON.parse(dt.getValue('DEPS')) : [];
          } catch (e) {
            this.deps = [];
          }
          this.dirty = false;
          this.statusMsg = '已加载: ' + this.templateName;
          this.statusIsError = false;
        }
      } catch (e) {
        console.warn('[SfcPopup] 查询模板失败:', e);
      }
    },
    // 加载同目录下的文件列表（仅 sfc 多文件模式）
    async loadFileList() {
      if (!this.dirPrefix) return;
      var self = this;
      this.fileListLoading = true;
      try {
        var ret = await this.$callAction({
          action: CEP.STORE_NAME + '/listAllAssets',
          param: {},
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        this.fileList = items.filter(function(it) {
          return (it.MODULEPATH || '').indexOf(self.dirPrefix) === 0;
        });
      } catch (e) {
        console.warn('[SfcPopup] 加载文件列表失败:', e);
      } finally {
        this.fileListLoading = false;
      }
    },
    // 切换到另一个文件
    async switchFile(path) {
      if (!path || path === this.modulePath) return;
      var self = this;
      if (this.dirty) {
        try {
          await this.$Confirm('当前文件未保存，是否切换？', '提示');
        } catch (e) {
          return; // 取消
        }
      }
      // 重置 MAIN DataTable 为全新 ADD 行（避免别名字段写入污染上一个文件的行）
      await this.$callAction({ action: SEP.STORE_NAME + '/add', isBusy: false });
      this.modulePath = path;
      this.deps = [];
      this.dirty = false;
      this.statusMsg = '';
      await this.loadByModulePath(path);
      // 未找到记录时按扩展名给默认模板
      if (!this.templateId) {
        if (path.endsWith('.vue')) {
          this.sourceCode = defaultVueTemplate;
        } else if (path.endsWith('/store.js')) {
          this.sourceCode = defaultStoreJsTemplate;
        } else {
          this.sourceCode = defaultPageJsTemplate;
        }
        this.fileType = path.endsWith('.vue') ? 'VUE' : 'JS';
        this.templateCode = (this.moduleCode ? this.moduleCode + '_' : '') + path.split('/').pop().replace(/\.\w+$/, '');
        this.templateName = path.split('/').pop();
        this.dirty = true;
      }
      this.$nextTick(function() {
        if (self.$refs.editor) self.$refs.editor.setValue(self.sourceCode);
      });
    },
    // 新建文件
    newFile() {
      var self = this;
      var proceed = async function() {
        var newName = 'new_' + Date.now() + '.vue';
        // 重置 MAIN DataTable 为全新 ADD 行
        await self.$callAction({ action: SEP.STORE_NAME + '/add', isBusy: false });
        self.modulePath = (self.dirPrefix || '@/modules/') + newName;
        self.fileType = 'VUE';
        self.templateCode = newName.replace(/\.\w+$/, '');
        self.templateName = newName;
        self.sourceCode = defaultVueTemplate;
        self.deps = [];
        self.dirty = true;
        self.statusMsg = '新建文件: ' + newName;
        self.statusIsError = false;
        self.popupTab = 'editor';
        self.$nextTick(function() {
          if (self.$refs.editor) self.$refs.editor.setValue(self.sourceCode);
        });
      };
      if (this.dirty) {
        this.$Confirm('当前文件未保存，是否新建？', '提示').then(proceed).catch(function() {});
      } else {
        proceed();
      }
    },
    onCodeChange() {
      this.dirty = true;
      this.statusMsg = '';
    },
    async handleCompile() {
      this.compiling = true;
      this.statusMsg = '';
      try {
        var result = await compileSFC(this.sourceCode, this.modulePath, this.fileType);
        this.deps = result.deps;
        this.statusMsg = '编译成功';
        this.statusIsError = false;
        this.$alert('编译成功');
      } catch (e) {
        this.statusMsg = '编译失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$error('编译失败: ' + (e.message || e));
      } finally {
        this.compiling = false;
      }
    },
    async handleSave() {
      if (!this.modulePath.trim()) {
        this.$error('请输入模块路径');
        return;
      }
      this.saving = true;
      this.statusMsg = '';
      try {
        // 编译
        var result = await compileSFC(this.sourceCode, this.modulePath, this.fileType);
        this.deps = result.deps;

        // 确保 RS_M17 模块配置已加载到 app store
        if (!this.$store.state.app.modules['RS_M17']) {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch('app/initModule', 'RS_M17');
        }

        // 编辑字段已通过别名写入弹窗专用 MAIN DataTable（sfc-editor-store）：
        // 新增 = show/newFile 时的 ADD 行；修改 = loadByModulePath open 的行；此处只补编译产物
        var dt = this.$MAIN;
        if (!dt || !dt.data || dt.data.length === 0) {
          this.$error('DataTable 未初始化');
          return;
        }

        var now = new Date().toISOString().replace('T', ' ').substring(0, 19);
        // 编码/名称兜底（别名字段为空时按路径推导）
        if (!this.CODE) dt.setValue('CODE', (this.moduleCode ? this.moduleCode + '_' : '') + this.modulePath.split('/').pop().replace(/\.\w+$/, ''));
        if (!this.NAME) dt.setValue('NAME', this.modulePath.split('/').pop());
        dt.setValue('FILETYPE', this.fileType);
        dt.setValue('COMPILEDCODE', result.compiledCode);
        dt.setValue('DEPS', JSON.stringify(result.deps));
        dt.setValue('ISDELETED', '0');
        dt.setValue('ASSETTYPE', 'vue');
        dt.setValue('MODIFYTIME', now);
        if (!this.templateId) {
          dt.setValue('CREATETIME', now);
        }

        await this.$callAction({ action: SEP.STORE_NAME + '/save' });

        // 保存成功后 ID 已由后端回写到 MAIN DataTable（templateId 别名自动同步）
        this.dirty = false;
        this.statusMsg = '保存成功';
        this.statusIsError = false;

        // 失效缓存: 清除该文件所在目录的所有缓存（确保所有相关变体被清除）
        var dir = this.modulePath.substring(0, this.modulePath.lastIndexOf('/') + 1);
        invalidateCacheByPrefix(dir);
        invalidateCacheByPrefix(this.modulePath);

        // SFC 多文件模式: 刷新文件列表（新文件入库、主组件星标）
        if (this.editTarget === 'sfc') {
          this.loadFileList();
        }

        this.$alert('保存成功');
        // 通知父组件
        this.$emit('saved', this.modulePath);
      } catch (e) {
        this.statusMsg = '保存失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$error('保存失败: ' + (e.message || e));
      } finally {
        this.saving = false;
      }
    },
    /**
     * 应用 AI 生成的代码到编辑器
     */
    onApplyCode(payload) {
      if (!this.$refs.editor) {
        this.$error('编辑器未就绪');
        return;
      }
      var mode = payload.mode;
      var code = payload.code || '';
      var searchReplace = payload.searchReplace || [];

      if (mode === 'search-replace') {
        var result = this.$refs.editor.applySearchReplace(searchReplace);
        if (result.applied > 0) {
          this.sourceCode = this.$refs.editor.getValue();
          this.dirty = true;
          this.statusMsg = 'AI 修改已应用: ' + result.applied + ' 处替换';
          this.statusIsError = false;
          this.$alert('已应用 ' + result.applied + ' 处修改' +
            (result.failed.length > 0 ? '，' + result.failed.length + ' 处匹配失败' : ''));
        } else {
          this.statusMsg = 'AI 修改未匹配到任何内容';
          this.statusIsError = true;
          this.$error('所有 SEARCH 块均未匹配到原文');
        }
      } else if (mode === 'replace') {
        if (this.dirty) {
          var ok = confirm('当前文件有未保存的修改, 替换全部会覆盖, 继续?');
          if (!ok) return;
        }
        if (searchReplace.length > 0) {
          code = searchReplace.map(function(b) { return b.replace }).join('\n\n');
        }
        this.sourceCode = code;
        this.$refs.editor.setValue(code);
        this.dirty = true;
        this.statusMsg = 'AI 代码已替换全部内容';
        this.statusIsError = false;
        this.$alert('已替换全部代码');
      } else if (mode === 'insert') {
        this.$refs.editor.insertAtCursor(code);
        this.sourceCode = this.$refs.editor.getValue();
        this.dirty = true;
        this.statusMsg = 'AI 代码已插入到光标位置';
        this.statusIsError = false;
        this.$alert('已插入到光标位置');
      }
      // 应用代码后切回编辑 Tab，并刷新 CodeMirror（隐藏容器中 setValue 后需 refresh 才能渲染）
      this.popupTab = 'editor';
      this.$nextTick(function() {
        if (this.$refs.editor) this.$refs.editor.refresh();
      });
    },
  },
};
</script>
<style lang="less" scoped>
/* 覆盖 view-dialog 的 .maxModalH overflow:auto，避免外层出现滚动条
   内部 sfc-code-editor 的 CodeMirror 已自带滚动，无需外层滚动 */
/deep/ .maxModalH {
  overflow: hidden !important;
   padding: 0 !important;;
}
/deep/ .rs-modal-footer {
  display: none !important;
}
.sfc-popup-body {
  display: flex;
  flex-direction: column;
  /* HeyUI Modal 默认 top:100px + panel-bar/footer ~80px + 边距，
     200px 缓冲确保整弹窗在视口内，不触发外层滚动 */
  height: calc(100vh - 200px);
  min-height: 400px;
}
/* SFC 多文件模式: 横向布局，左侧文件列表 + 右侧主区域 */
.sfc-popup-body-multi {
  flex-direction: row;
}
.sfc-popup-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.sfc-popup-filelist {
  width: 180px;
  flex-shrink: 0;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sfc-popup-filelist-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 8px;
  font-size: 12px;
  color: #666;
  border-bottom: 1px solid #f0f0f0;
  background: #fafafa;
  flex-shrink: 0;
}
.sfc-popup-filelist-items {
  flex: 1;
  overflow: auto;
}
.sfc-popup-file-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 8px;
  cursor: pointer;
  font-size: 12px;
  border-left: 2px solid transparent;
  color: #303133;
  &:hover { background: #f5f5f5; }
}
.sfc-popup-file-item.active {
  background: #F0F5FF;
  color: #2F54EB;
  border-left-color: #2F54EB;
}
.sfc-popup-file-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sfc-popup-file-main {
  color: #faad14;
  flex-shrink: 0;
}
.sfc-popup-file-empty {
  padding: 16px 8px;
  color: #bbb;
  font-size: 12px;
  text-align: center;
}
.sfc-popup-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 12px;
  background: #f7f7f7;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.sfc-popup-toolbar-left {
  display: flex;
  gap: 6px;
  flex: 1;
}
.sfc-popup-toolbar-right {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
  margin-left: 12px;
}
.sfc-popup-input {
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  padding: 3px 8px;
  font-size: 12px;
  outline: none;
  &:focus {
    border-color: #0a84ff;
  }
}
.sfc-popup-input-type {
  width: 55px;
  cursor: pointer;
}
.sfc-popup-input-code {
  width: 100px;
}
.sfc-popup-input-name {
  width: 120px;
}
.sfc-popup-input-path {
  flex: 1;
  min-width: 150px;
}
.sfc-popup-content {
  display: flex;
  flex: 1;
  min-height: 0;
  overflow: hidden;
  flex-direction: column;
}
.sfc-popup-tab-bar {
  display: flex;
  align-items: center;
  background: #f0f0f0;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.sfc-popup-tab {
  padding: 4px 16px;
  cursor: pointer;
  color: #888;
  font-size: 12px;
  border-bottom: 2px solid transparent;
  user-select: none;
  &:hover { color: #555; }
  &.active {
    color: #0a84ff;
    border-bottom-color: #0a84ff;
  }
}
.sfc-popup-ai {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.sfc-popup-editor-row {
  flex: 1;
  min-height: 0;
  display: flex;
  overflow: hidden;
}
.sfc-popup-editor {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-right: 1px solid #e8e8e8;
}
.sfc-popup-preview {
  width: 40%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sfc-popup-section-title {
  display: flex;
  align-items: center;
  padding: 4px 10px;
  background: #fafafa;
  border-bottom: 1px solid #e8e8e8;
  font-size: 12px;
  color: #666;
  flex-shrink: 0;
  gap: 6px;
}
.sfc-popup-badge {
  display: inline-block;
  padding: 1px 6px;
  background: #e6a23c;
  color: #fff;
  border-radius: 3px;
  font-size: 10px;
}
.sfc-popup-statusbar {
  display: flex;
  justify-content: space-between;
  padding: 3px 12px;
  background: #007acc;
  color: #fff;
  font-size: 12px;
  flex-shrink: 0;
  height: 22px;
  align-items: center;
}
.sfc-status-error {
  color: #f48771;
}
</style>
<!-- 全局样式: 直接给代码编辑器容器一个基于视口的明确像素高度，
     彻底绕过 flex 链路，CodeMirror 拿到有界高度后自动出内部滚动条 -->
<style lang="less">
.sfc-popup-editor .sfc-code-editor-wrap {
  height: calc(100vh - 315px) !important;
  min-height: 300px !important;
  display: flex !important;
  flex-direction: column !important;
}
.sfc-popup-editor .sfc-code-editor-body {
  flex: 1 !important;
  min-height: 0 !important;
}
.sfc-popup-editor .CodeMirror {
  height: 100% !important;
}
</style>
