<template>
  <div class="sfc-ide-page" ref="idePage">
    <!-- 顶部工具栏 -->
    <div class="sfc-ide-toolbar">
      <div class="sfc-ide-toolbar-left">
        <span class="sfc-ide-logo">代码在线开发</span>
      </div>
      <div class="sfc-ide-toolbar-center">
        <div class="sfc-ide-file-info" v-if="currentPath">
          <select v-model="fileType" class="sfc-input sfc-input-type" :disabled="!!templateId">
            <option value="VUE">VUE</option>
            <option value="JS">JS</option>
            <option value="CSHARP">C#</option>
            <option value="SQL">SQL</option>
          </select>
          <input v-model="templateCode" placeholder="编码" class="sfc-input sfc-input-code" />
          <input v-model="templateName" placeholder="名称" class="sfc-input sfc-input-name" />
          <!-- 路径（全部类型统一: vue/js=@/pages|@/modules, csharp/sql=@/scripts） -->
          <input v-model="modulePath" :placeholder="pathPlaceholder" class="sfc-input sfc-input-path" />
          <Select
            v-if="fileKind === 'sfc'"
            v-model="dbModuleCode"
            :datas="moduleList"
            keyName="MODULECODE"
            titleName="MODULENAME"
            :filterable="true"
            placeholder="选择模块"
            class="sfc-input sfc-input-module"
          ></Select>
          <span v-else class="sfc-kind-badge">{{ fileKind === 'csharp' ? 'API 脚本' : 'SQL 模板' }}</span>
        </div>
      </div>
      <div class="sfc-ide-toolbar-right">
        <DropdownMenu
          :datas="tplList"
          trigger="click"
          :toggleIcon="false"
          @click="onTplSelect"
        >
          <Button size="s">插入模板 <i class="h-icon-down"></i></Button>
        </DropdownMenu>
        <Button size="s" @click="togglePageFullscreen">
          {{ isPageFullscreen ? '退出全屏' : '整页全屏' }}
        </Button>
        <Button size="s" :disabled="!templateId" @click="openVersions">版本</Button>
        <Button size="s" v-if="fileKind === 'csharp' || fileKind === 'sql'" :color="activeTab === 'test' ? 'primary' : null" @click="activeTab = activeTab === 'test' ? 'preview' : 'test'">测试</Button>
        <Button size="s" :loading="compiling" @click="handleCompile">编译</Button>
        <save-actions :loading="saving" @save="onQuickSave" @commit="onCommitSave" />
      </div>
    </div>

    <!-- 插入模板 Modal -->
    <Modal v-model="tplModalVisible" :title="tplModalTitle" class-name="sfc-tpl-modal" :closeOnMask="false" :width="560">
      <div class="sfc-tpl-modal-body">
        <!-- 模板类型说明 -->
        <div class="sfc-tpl-desc" v-if="tplCurrent">
          <i class="h-icon-info"></i>
          <span>{{ tplCurrent.desc }}</span>
        </div>

        <!-- 参数表单 -->
        <div class="sfc-tpl-form">
          <div class="sfc-tpl-row">
            <label>模块编码</label>
            <div class="sfc-tpl-field">
              <Select
                v-model="tplForm.moduleCode"
                :datas="moduleList"
                keyName="MODULECODE"
                titleName="MODULENAME"
                :filterable="true"
                placeholder="搜索选择模块 (如 RS_M16)"
                @input="onModuleChange"
                class="sfc-tpl-select"
              ></Select>
            </div>
          </div>
          <div class="sfc-tpl-row">
            <label>store命名空间</label>
            <div class="sfc-tpl-field">
              <input v-model="tplForm.storeName" class="sfc-tpl-input" placeholder="如 s01/m16 (自动推导, 可改)" />
            </div>
          </div>
          <div class="sfc-tpl-row">
            <label>页面标题</label>
            <div class="sfc-tpl-field">
              <input v-model="tplForm.title" class="sfc-tpl-input" placeholder="如 委托单位" />
            </div>
          </div>
          <div class="sfc-tpl-row" v-if="tplCurrent && tplCurrent.key === 'master-detail'">
            <label>子表名</label>
            <div class="sfc-tpl-field">
              <input v-model="tplForm.dtsName" class="sfc-tpl-input" placeholder="如 DTS (默认)" />
            </div>
          </div>
        </div>

        <!-- 文件生成区 -->
        <div class="sfc-tpl-files" v-if="tplCurrent">
          <p class="sfc-tpl-files-title">
            <i class="h-icon-document"></i>
            选择要生成的文件 (代码填入编辑器, modulePath 自动推导):
          </p>
          <div class="sfc-tpl-file-btns">
            <Button
              v-for="f in tplCurrent.files"
              :key="f.key"
              size="s"
              @click="genFile(f)"
            >{{ f.label }}</Button>
          </div>
        </div>
      </div>
      <div slot="footer" class="sfc-tpl-modal-footer">
        <Button @click="tplModalVisible = false">关闭</Button>
      </div>
    </Modal>

    <!-- 主体: 三栏布局 -->
    <div class="sfc-ide-body">
      <!-- 左侧: 文件树 -->
      <div class="sfc-ide-sidebar">
        <file-tree
          :selectedPath="currentPath"
          @select="onFileSelect"
          @new-file="onNewFile"
          @delete-file="onDeleteFile"
        ></file-tree>
      </div>

      <!-- 中间: 编辑器 -->
      <div class="sfc-ide-editor" ref="editorSection">
        <div class="sfc-ide-section-title" v-if="currentPath">
          <span class="sfc-ide-section-label">
            {{ currentPath }}
            <span class="sfc-ide-section-badge" v-if="dirty">未保存</span>
          </span>
          <span class="sfc-ide-section-actions">
            <span class="sfc-ide-fs-btn" @click="toggleEditorFullscreen" :title="isEditorFullscreen ? '退出全屏' : '编辑器全屏'">
              {{ isEditorFullscreen ? '⤓' : '⤢' }}
            </span>
          </span>
        </div>
        <div class="sfc-ide-section-title" v-else>请从左侧选择文件</div>
        <sfc-code-editor
          v-if="currentPath"
          ref="editor"
          v-model="sourceCode"
          :fileType="fileType"
          @change="onCodeChange"
        ></sfc-code-editor>
        <div v-else class="sfc-ide-empty">
          <div class="sfc-ide-empty-icon">&#128221;</div>
          <p>选择一个文件开始编辑，或点击 + 新建文件</p>
        </div>
      </div>

      <!-- 右侧: 预览 / AI 助手 (Tab 切换) -->
      <div class="sfc-ide-preview" ref="previewSection">
        <div class="sfc-ide-section-title sfc-ide-tab-bar">
          <div class="sfc-ide-tabs">
            <span :class="['sfc-ide-tab', { active: activeTab === 'preview' }]" @click="activeTab = 'preview'">实时预览</span>
            <span :class="['sfc-ide-tab', { active: activeTab === 'ai' }]" @click="switchToAi">AI 助手</span>
            <span v-if="fileKind === 'csharp' || fileKind === 'sql'" :class="['sfc-ide-tab', { active: activeTab === 'test' }]" @click="activeTab = 'test'">接口测试</span>
          </div>
          <span class="sfc-ide-section-actions">
            <span class="sfc-ide-fs-btn" @click="togglePreviewFullscreen" :title="isPreviewFullscreen ? '退出全屏' : '预览全屏'">
              {{ isPreviewFullscreen ? '⤓' : '⤢' }}
            </span>
          </span>
        </div>
        <!-- 实时预览 -->
        <sfc-preview
          v-if="activeTab === 'preview' && currentPath"
          :source="sourceCode"
          :modulePath="modulePath"
        ></sfc-preview>
        <div v-if="activeTab === 'preview' && !currentPath" class="sfc-ide-empty">
          <div class="sfc-ide-empty-icon">&#128065;</div>
          <p>选择文件后显示预览</p>
        </div>
        <!-- AI 助手 -->
        <ai-chat-panel
          v-if="activeTab === 'ai'"
          ref="aiPanel"
          :currentFile="aiCurrentFile"
          :siblingFiles="aiSiblingFiles"
          :moduleCode="dbModuleCode"
          @apply-code="onApplyCode"
        ></ai-chat-panel>
        <!-- 接口测试（csharp/sql；参数自动识别，支持源码试运行/接口执行） -->
        <code-test-panel
          v-if="activeTab === 'test' && (fileKind === 'csharp' || fileKind === 'sql')"
          :kind="fileKind"
          :source="sourceCode"
          :code="templateCode"
        />
      </div>
    </div>

    <!-- 底部状态栏 -->
    <div class="sfc-ide-statusbar">
      <span v-if="statusMsg" class="sfc-status-msg" :class="{ 'sfc-status-error': statusIsError }">{{ statusMsg }}</span>
      <span v-else class="sfc-status-info">就绪</span>
      <span class="sfc-status-deps" v-if="deps.length > 0">依赖: {{ deps.join(', ') }}</span>
    </div>

    <!-- 通用版本历史弹窗 -->
    <version-history-popup ref="verHistory" @rollback="onVersionRollback" />
  </div>
</template>
<script>
import { compileSFC, invalidateCacheByPrefix } from '@/sfc-loader';
import { queryModules } from '@/utils/selRegistry';
import { mapDateTable, Constants } from '../store';
import { TEMPLATES, derivePaths, deriveStoreName } from '../templates';
import sfcCodeEditor from '../components/sfc-code-editor.vue';
import sfcPreview from '../components/sfc-preview.vue';
import fileTree from '../components/file-tree.vue';
import aiChatPanel from '../components/ai-chat-panel.vue';
import saveActions from '@/components/generic-module/save-actions.vue';
import versionHistoryPopup from '@/components/generic-module/version-history-popup.vue';
import codeTestPanel from '@/components/generic-module/code-test-panel.vue';
import { openAsset, addAsset, checkAsset, saveAsset, applyAiFileOps, parseAiFileBlocks, defaultCsharpTemplate, defaultSqlTemplate, deriveScriptPath } from '../code-asset';

export default {
  name: 's01-m17-edit',
  components: { sfcCodeEditor, sfcPreview, fileTree, aiChatPanel, saveActions, versionHistoryPopup, codeTestPanel },
  computed: {
    // 编辑字段绑 MAIN DataTable（mapDateTable 生成 get/set 自动读写 dt）
    ...mapDateTable('MAIN', ['ID', 'CODE', 'NAME', 'SOURCECODE', 'REMARK', 'MODULEPATH', 'VERSION']),
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
    description: {
      get() { return this.REMARK || '' },
      set(v) { this.REMARK = v },
    },
    scriptVersion: {
      get() { return +(this.VERSION || 1) },
      set(v) { this.VERSION = v },
    },
    tplList() {
      // 给 DropdownMenu 的 :datas, 每项 { title, key }
      return TEMPLATES.map(function(t) {
        return { key: t.key, title: t.name };
      });
    },
    moduleCode() {
      // 从 modulePath 推导模块编码
      // '@/pages/s01/m16/views/main.vue' → 's01/m16'
      var p = this.modulePath || '';
      if (!p) return '';
      var parts = p.split('/');
      // 找到 pages 后的两段
      var idx = -1;
      for (var i = 0; i < parts.length; i++) {
        if (parts[i] === 'pages') { idx = i; break }
      }
      if (idx >= 0 && idx + 2 < parts.length) {
        return parts[idx + 1] + '/' + parts[idx + 2];
      }
      return '';
    },
    // 路径占位提示（按文件类型）
    pathPlaceholder() {
      if (this.fileType === 'CSHARP') return '@/scripts/模块/编码.cs';
      if (this.fileType === 'SQL') return '@/scripts/模块/编码.sql';
      return this.fileType === 'VUE' ? '@/pages/xxx/xxx.vue' : '@/pages/xxx/xxx.js';
    },
  },
  data() {
    return {
      // 代码资产类型: 'sfc'(SFC组件) | 'csharp'(API脚本) | 'sql'(SQL模板)
      fileKind: 'sfc',
      // 编辑字段(templateId/templateCode/templateName/modulePath/sourceCode/description/scriptVersion)
      // 已迁移为 computed 别名，读写 s01/m17 MAIN DataTable（见上方 mapDateTable）
      currentPath: '',
      fileType: 'VUE',
      deps: [],
      saving: false,
      compiling: false,
      statusMsg: '',
      statusIsError: false,
      dirty: false,
      // 全屏状态 (3 个独立: 整页 / 编辑器 / 预览)
      isPageFullscreen: false,
      isEditorFullscreen: false,
      isPreviewFullscreen: false,
      // 插入模板
      tplDropdownVisible: false,
      tplModalVisible: false,
      tplModalTitle: '',
      tplCurrent: null, // 当前选中的模板对象 (TEMPLATES 项)
      tplCurrentFile: null, // 当前选中的文件子项
      tplForm: {
        moduleCode: '',
        storeName: '',
        title: '',
        dtsName: 'DTS',
      },
      moduleList: [], // 数据库所有模块 (tss_moudle), 供模块编码下拉
      moduleMap: {}, // MODULECODE → MODULENAME, 选中后自动填标题
      // 数据库模块编码 (AI 助手工具调用时使用)
      dbModuleCode: '', // 工具栏选择的数据库 MODULECODE (如 LIB_M07)
      // AI 助手 Tab
      activeTab: 'preview', // 'preview' | 'ai'
      aiCurrentFile: null, // {path, type, content} 当前编辑文件
      aiSiblingFiles: [], // [{path, type, content}] 同级文件
      siblingFilesCache: null, // 缓存: { dirPrefix, files, loadedAt }
    };
  },
  async created() {
    var id = this.$route.query.id;
    if (id) {
      await this.loadTemplate(id);
    }
    // 路由直达: ?scriptCode=SC_XXX(API 脚本) / ?sqlCode=SS_XXX(SQL 模板) / ?newKind=csharp|sql&module=R02_M07(新建)
    if (this.$route.query.scriptCode) {
      await this.openApiScriptByCode(this.$route.query.scriptCode);
    }
    if (this.$route.query.sqlCode) {
      await this.openSqlByCode(this.$route.query.sqlCode);
    }
    if (this.$route.query.newKind) {
      this.onNewFile(this.$route.query.newKind, this.$route.query.module || '');
    }
    // URL 参数预填数据库模块编码
    if (this.$route.query.moduleCode) {
      this.dbModuleCode = this.$route.query.moduleCode;
    }
    // 预加载模块列表 (供工具栏 MODULECODE 下拉使用)
    this.loadModuleList();
    // 监听 fullscreenchange 同步状态 (用户按 Esc 退出时也要更新按钮文案)
    window.addEventListener('resize', this._handleFsChange);
    document.addEventListener('fullscreenchange', this._handleFsChange);
    document.addEventListener('webkitfullscreenchange', this._handleFsChange);
    // Ctrl+Shift+I 切换到 AI 助手
    document.addEventListener('keydown', this._handleAiShortcut);
    // 注册当前 SFC 编辑器到全局 sfcContext，供全局抽屉的开发 agent 使用
    // editTarget: sfc=''/csharp/sql —— 后端按它选专业提示词（切文件类型时 watch 同步）
    this.$store.commit('sfcContext/SET', {
      editorRef: this,
      moduleCode: this.dbModuleCode,
      siblingFiles: this.aiSiblingFiles,
      editTarget: this.fileKind === 'sfc' ? '' : this.fileKind,
      active: true
    });
    // eslint-disable-next-line no-restricted-syntax
    this.$store.dispatch('assistant/setAgent', 'sfc');
  },
  beforeDestroy() {
    document.removeEventListener('fullscreenchange', this._handleFsChange);
    document.removeEventListener('webkitfullscreenchange', this._handleFsChange);
    window.removeEventListener('resize', this._handleFsChange);
    document.removeEventListener('keydown', this._handleAiShortcut);
    this.$store.commit('sfcContext/CLEAR');
  },
  watch: {
    dbModuleCode(newVal) {
      this.$store.commit('sfcContext/UPDATE', { moduleCode: newVal });
    },
    aiSiblingFiles(newVal) {
      this.$store.commit('sfcContext/UPDATE', { siblingFiles: newVal });
    },
    fileKind(newVal) {
      this.$store.commit('sfcContext/UPDATE', { editTarget: newVal === 'sfc' ? '' : newVal });
      // 切走脚本类型时退出测试 tab
      if (this.activeTab === 'test' && newVal !== 'csharp' && newVal !== 'sql') {
        this.activeTab = 'preview';
      }
    },
    fileType(newVal) {
      // 仅对新建文件(无 templateId)切换默认模板 + 资产类型
      if (!this.templateId && this.dirty) {
        var tplMap = { VUE: defaultVueTemplate, JS: defaultJsTemplate, CSHARP: defaultCsharpTemplate, SQL: defaultSqlTemplate };
        var kindMap = { VUE: 'sfc', JS: 'sfc', CSHARP: 'csharp', SQL: 'sql' };
        // 当前源码仍是任一默认模板时才切换（用户已写内容不覆盖）
        var isDefault = [defaultVueTemplate, defaultJsTemplate, defaultCsharpTemplate, defaultSqlTemplate].indexOf(this.sourceCode) >= 0;
        if (isDefault && tplMap[newVal]) {
          this.sourceCode = tplMap[newVal];
          this.fileKind = kindMap[newVal];
          // 路径随类型切换约定: JS 模块 → @/modules/, VUE → @/pages/
          if (newVal === 'JS' && this.modulePath && this.modulePath.indexOf('@/modules/') !== 0) {
            this.modulePath = '@/modules/' + (this.dbModuleCode || 'DEMO') + '/新模块.js';
          } else if (newVal === 'VUE' && this.modulePath && this.modulePath.indexOf('@/modules/') === 0) {
            this.modulePath = '@/pages/新模板.vue';
          }
          this.currentPath = this.modulePath;
          if (this.$refs.editor) this.$refs.editor.setValue(this.sourceCode);
        }
      }
    },
    // 文件切换时更新 AI 上下文 + 失效同级文件缓存
    currentPath() {
      this.siblingFilesCache = null;
      // 立即更新当前文件上下文 (不依赖 activeTab，保证切换到 AI tab 时已有正确数据)
      this.aiCurrentFile = this.getAiCurrentFile();
      if (this.activeTab === 'ai') {
        this.loadSiblingFiles();
      }
    },
    // 代码变更时实时更新 AI 上下文
    sourceCode() {
      this.aiCurrentFile = this.getAiCurrentFile();
    },
  },
  methods: {
    // ====== 全屏 (浏览器原生 Fullscreen API, 接管整个屏幕) ======
    _handleFsChange() {
      var fsEl = document.fullscreenElement || document.webkitFullscreenElement;
      this.isPageFullscreen = fsEl === this.$refs.idePage;
      this.isEditorFullscreen = fsEl === this.$refs.editorSection;
      this.isPreviewFullscreen = fsEl === this.$refs.previewSection;
      // 切换全屏后, 编辑器(CodeMirror)需要 refresh 才能正确计算宽度
      this.$nextTick(() => {
        if (this.$refs.editor && this.$refs.editor.refresh) {
          this.$refs.editor.refresh();
        }
      });
    },
    _requestFs(el) {
      if (!el) return Promise.resolve();
      if (el.requestFullscreen) return el.requestFullscreen();
      if (el.webkitRequestFullscreen) return el.webkitRequestFullscreen();
      return Promise.resolve();
    },
    _exitFs() {
      if (document.exitFullscreen) return document.exitFullscreen();
      if (document.webkitExitFullscreen) return document.webkitExitFullscreen();
    },
    _toggleFs(el) {
      var fsEl = document.fullscreenElement || document.webkitFullscreenElement;
      if (fsEl) {
        return this._exitFs();
      }
      return this._requestFs(el);
    },
    togglePageFullscreen() {
      this._toggleFs(this.$refs.idePage);
    },
    // 打开当前文件的版本历史弹窗（查询/当时对比/与现在对比/回滚/标记）
    openVersions() {
      if (!this.templateId) {
        this.$error('当前文件尚未保存，无版本记录');
        return;
      }
      this.$refs.verHistory.show({
        objType: 'code',
        objId: this.templateId,
        objCode: this.templateCode || this.modulePath,
      });
    },
    // 历史弹窗回滚后：重新加载当前文件内容
    async onVersionRollback() {
      if (this.templateId) {
        if (this.fileKind === 'csharp') await this.openApiScript({ scriptId: this.templateId });
        else if (this.fileKind === 'sql') await this.openSqlTemplate({ sqlId: this.templateId });
        else await this.loadTemplate(this.templateId);
        this.$root.$emit('sfc-tree-refresh');
      }
    },
    toggleEditorFullscreen() {
      this._toggleFs(this.$refs.editorSection);
    },
    togglePreviewFullscreen() {
      this._toggleFs(this.$refs.previewSection);
    },
    // ====== 插入模板 ======
    async onTplSelect(code) {
      // Dropdown 选中模板类型 → 打开参数 Modal
      var tpl = null;
      TEMPLATES.forEach(function(t) {
        if (!tpl && t.key === code) tpl = t;
      });
      if (!tpl) return;
      this.tplCurrent = tpl;
      this.tplCurrentFile = null;
      this.tplModalTitle = '插入模板: ' + tpl.name;
      // 懒加载模块列表 (仅首次)
      if (this.moduleList.length === 0) {
        await this.loadModuleList();
      }
      this.tplModalVisible = true;
    },
    async loadModuleList() {
      // 复用 selRegistry.queryModules 从 tss_moudle 拉所有模块, 供模块编码下拉
      try {
        var items = await queryModules();
        this.moduleList = items || [];
        var map = {};
        this.moduleList.forEach(function(m) {
          if (m.MODULECODE) map[m.MODULECODE] = m.MODULENAME || '';
        });
        this.moduleMap = map;
      } catch (e) {
        console.warn('[SfcEdit] 加载模块列表失败:', e);
      }
    },
    onModuleChange() {
      // 选中模块后: 自动推导 storeName, 用模块名填标题 (若标题为空)
      var code = this.tplForm.moduleCode;
      if (!code) return;
      this.tplForm.storeName = deriveStoreName(code);
      if (!this.tplForm.title || !this.tplForm.title.trim()) {
        this.tplForm.title = this.moduleMap[code] || '';
      }
    },
    genFile(file) {
      if (!this.tplForm.moduleCode || !this.tplForm.moduleCode.trim()) {
        this.$error('请填写模块编码');
        return;
      }
      if (!this.tplForm.title || !this.tplForm.title.trim()) {
        this.$error('请填写页面标题');
        return;
      }
      var tpl = this.tplCurrent;
      // 找到当前模板里对应的 file 定义 (tpl.files 来自 TEMPLATES, gen 已绑定)
      var fileDef = null;
      tpl.files.forEach(function(f) {
        if (!fileDef && f.key === file.key) fileDef = f;
      });
      if (!fileDef) return;
      // 若当前编辑器有未保存修改, 提示
      if (this.dirty) {
        var ok = confirm('当前文件有未保存的修改, 插入模板会覆盖, 继续?');
        if (!ok) return;
      }
      var code = fileDef.gen({
        moduleCode: this.tplForm.moduleCode.trim().toUpperCase(),
        storeName: this.tplForm.storeName.trim(),
        title: this.tplForm.title.trim(),
        dtsName: this.tplForm.dtsName.trim() || 'DTS',
      });
      // 推导当前文件的 modulePath
      var paths = derivePaths(this.tplForm.moduleCode.trim().toUpperCase());
      var pathMap = { main: paths.main, add: paths.add, store: paths.store };
      // 纯展示模板只有 main, 且不依赖 store
      this.sourceCode = code;
      this.modulePath = pathMap[file.key] || paths.main;
      this.fileType = fileDef.fileType;
      this.templateCode = this.tplForm.moduleCode.trim().toUpperCase() + '_' + file.key;
      this.templateName = this.tplForm.title.trim() + ' (' + fileDef.label + ')';
      this.currentPath = this.modulePath;
      this.deps = [];
      this.dirty = true;
      this.tplCurrentFile = fileDef;
      this.statusMsg = '已生成 ' + fileDef.label + ', modulePath=' + this.modulePath + ', 请检查后保存';
      this.statusIsError = false;
      var self = this;
      this.$nextTick(function() {
        if (self.$refs.editor) {
          self.$refs.editor.setValue(self.sourceCode);
        }
      });
    },
    async onDeleteFile(node) {
      if (!node || !node.templateId) {
        this.$error('无法删除: 缺少文件 ID');
        return;
      }
      // 删除前确认
      var ok = confirm('确定删除文件 "' + (node.templateName || node.name) + '" ?\n路径: ' + node.path + '\n此操作不可恢复');
      if (!ok) return;
      // 删除的是当前编辑的文件 → 检查未保存(四类资产统一按 templateId 判断)
      if (this.templateId && this.templateId === node.templateId && this.dirty) {
        var ok2 = confirm('当前文件有未保存的修改, 仍然删除?');
        if (!ok2) return;
      }
      try {
        // 逻辑删除(四类资产统一): open 载入 MAIN → ISDELETED=1 → save(走 doSave, 版本记录为 delete 可回滚)
        // 不物理删除: 版本兜底+行还在双保险; 生成列唯一键 uk_livepath 保证删除后同路径可重建
        await this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { ID: node.templateId }, isBusy: false });
        this.$MAIN.setValue('ISDELETED', '1');
        await this.$callAction({ action: `${Constants.STORE_NAME}/save`, param: { CHANGENOTE: '删除文件' }, isBusy: false });
        this.$alert('删除成功（如需恢复，可到版本中心回滚）');
        // 如果删的是当前打开的文件, 清空编辑区(按 templateId 判断)
        if (this.templateId && this.templateId === node.templateId) {
          this.templateId = '';
          this.currentPath = '';
          this.modulePath = '@/pages/';
          this.sourceCode = '';
          this.deps = [];
          this.dirty = false;
          this.statusMsg = '';
        }
        // 刷新文件树
        this.$root.$emit('sfc-tree-refresh');
      } catch (e) {
        this.$error('删除失败: ' + (e.message || e));
      }
    },
    async onFileSelect(node) {
      if (this.dirty) {
        var ok = confirm('当前文件未保存，是否丢弃修改？');
        if (!ok) return;
      }
      // 按代码资产类型分流: API 脚本(C#) / SQL 模板 / SFC 组件
      if (node.fileKind === 'csharp') {
        await this.openApiScript(node);
        return;
      }
      if (node.fileKind === 'sql') {
        await this.openSqlTemplate(node);
        return;
      }
      this.fileKind = 'sfc';
      this.currentPath = node.path;
      this.modulePath = node.path;
      this.templateId = node.templateId;
      this.templateName = node.templateName || node.name;
      this.templateCode = node.name;
      await this.loadTemplate(node.templateId);
    },
    // ====== API 脚本 (C#) ======
    async openApiScript(node) {
      try {
        // 经数据源打开（code-asset 共用层：RS_M21 Store03 open → DataTable）
        var dt = await openAsset('csharp', node.scriptId);
        if (!dt || !dt.data || dt.data.length === 0) {
          this.$error('未找到脚本数据');
          return;
        }
        this.fileKind = 'csharp';
        this.templateId = dt.getValue('ID');
        this.templateCode = dt.getValue('CODE') || '';
        this.templateName = dt.getValue('NAME') || '';
        this.modulePath = dt.getValue('MODULEPATH') || (await deriveScriptPath('csharp', dt.getValue('CODE') || ''));
        this.currentPath = this.modulePath;
        this.fileType = 'CSHARP';
        this.sourceCode = dt.getValue('SOURCECODE') || '';
        this.description = dt.getValue('REMARK') || '';
        this.scriptVersion = +(dt.getValue('VERSION') || 1);
        this.deps = [];
        this.dirty = false;
        this.statusMsg = '已加载: ' + this.templateName + ' (v' + this.scriptVersion + ')';
        this.statusIsError = false;
        var self = this;
        this.$nextTick(function() {
          if (self.$refs.editor) self.$refs.editor.setValue(self.sourceCode);
        });
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    async openApiScriptByCode(scriptCode) {
      try {
        var ret = await this.$callAction({
          action: `${Constants.STORE_NAME}/listAssets`,
          param: { assetType: 'csharp' },
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        var found = items.find(function(s) { return s.CODE === scriptCode });
        if (found) {
          await this.openApiScript({ scriptId: found.ID });
        } else {
          this.$error('脚本不存在: ' + scriptCode);
        }
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    // ====== SQL 模板 ======
    async openSqlTemplate(node) {
      try {
        // 经数据源打开（code-asset 共用层：RS_M13 Store03 open → DataTable）
        var dt = await openAsset('sql', node.sqlId);
        if (!dt || !dt.data || dt.data.length === 0) {
          this.$error('未找到 SQL 模板数据');
          return;
        }
        this.fileKind = 'sql';
        this.templateId = dt.getValue('ID');
        this.templateCode = dt.getValue('CODE') || '';
        this.templateName = dt.getValue('NAME') || '';
        this.modulePath = dt.getValue('MODULEPATH') || (await deriveScriptPath('sql', dt.getValue('CODE') || ''));
        this.currentPath = this.modulePath;
        this.fileType = 'SQL';
        this.sourceCode = dt.getValue('SOURCECODE') || '';
        this.description = dt.getValue('REMARK') || '';
        this.deps = [];
        this.dirty = false;
        this.statusMsg = '已加载: ' + (this.templateName || this.templateCode);
        this.statusIsError = false;
        var self = this;
        this.$nextTick(function() {
          if (self.$refs.editor) self.$refs.editor.setValue(self.sourceCode);
        });
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    async openSqlByCode(sqlCode) {
      try {
        var ret = await this.$callAction({
          action: `${Constants.STORE_NAME}/listAssets`,
          param: { assetType: 'sql' },
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        var found = items.find(function(q) { return q.CODE === sqlCode });
        if (found) {
          await this.openSqlTemplate({ sqlId: found.ID });
        } else {
          this.$error('SQL 模板不存在: ' + sqlCode);
        }
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    async onNewFile(kind, modulePrefix) {
      if (this.dirty) {
        var ok = confirm('当前文件未保存，是否丢弃修改？');
        if (!ok) return;
      }
      // 新建代码资产: 按类型初始化(SFC 组件 / API 脚本 / SQL 模板)
      kind = kind || (this.fileType === 'CSHARP' ? 'csharp' : (this.fileType === 'SQL' ? 'sql' : 'sfc'));
      var prefix = modulePrefix ? modulePrefix + '_' : '';
      // 各资产走自己的数据源 add（code-asset 共用层：INIT+ADD 新行，DataTable 承载）
      if (kind === 'csharp') {
        await addAsset('csharp');
      } else if (kind === 'sql') {
        await addAsset('sql');
      } else {
        await this.$callAction({ action: `${Constants.STORE_NAME}/add`, isBusy: false });
      }
      this.templateId = '';
      this.scriptVersion = 1;
      this.deps = [];
      this.dirty = true;
      this.statusIsError = false;
      if (kind === 'csharp') {
        this.fileKind = 'csharp';
        this.fileType = 'CSHARP';
        this.templateCode = 'SC_' + prefix + 'NEW';
        this.templateName = '新脚本';
        this.modulePath = await deriveScriptPath('csharp', this.templateCode);
        this.currentPath = this.modulePath;
        this.sourceCode = defaultCsharpTemplate;
        this.description = '';
        this.statusMsg = '新建 API 脚本 (C#)，请修改编码/名称后保存';
      } else if (kind === 'sql') {
        this.fileKind = 'sql';
        this.fileType = 'SQL';
        this.templateCode = 'SS_' + prefix + 'NEW';
        this.templateName = '新模板';
        this.modulePath = await deriveScriptPath('sql', this.templateCode);
        this.currentPath = this.modulePath;
        this.sourceCode = defaultSqlTemplate;
        this.description = '';
        this.statusMsg = '新建 SQL 模板，请修改编码/名称后保存';
      } else {
        this.fileKind = 'sfc';
        this.fileType = 'VUE';
        this.templateCode = 'new_' + Date.now();
        this.templateName = '新模板';
        // 默认路径按类型: VUE → @/pages/, JS 模块 → @/modules/{当前模块或示例}/
        if (this.fileType === 'JS') {
          var modDir = this.dbModuleCode || (this.moduleList && this.moduleList.length > 0 ? this.moduleList[0].MODULECODE : 'DEMO');
          this.modulePath = '@/modules/' + modDir + '/新模块.js';
          this.currentPath = this.modulePath;
          this.sourceCode = defaultJsTemplate;
        } else {
          this.modulePath = '@/pages/';
          this.currentPath = '@/pages/新模板.vue';
          this.sourceCode = defaultVueTemplate;
        }
        this.statusMsg = '新建文件，请修改编码/名称/路径后保存';
      }
      this.$nextTick(function() {
        if (this.$refs.editor) {
          this.$refs.editor.setValue(this.sourceCode);
        }
      }.bind(this));
    },
    async loadTemplate(id) {
      if (!id) {
        if (this.currentPath) {
          this.statusMsg = '新文件';
        }
        return;
      }
      try {
        // 调 Store03 open 加载到 DataTable
        await this.$callAction({ action: `${Constants.STORE_NAME}/open`, param: { ID: id }, isBusy: false });
        var dt = this.$MAIN;
        if (!dt || !dt.data || dt.data.length === 0) {
          this.$error('未找到模板数据');
          return;
        }
        this.templateId = dt.getValue('ID');
        this.templateCode = dt.getValue('CODE') || '';
        this.templateName = dt.getValue('NAME') || '';
        this.modulePath = dt.getValue('MODULEPATH') || '';
        this.currentPath = this.modulePath;
        this.fileType = dt.getValue('FILETYPE') || 'VUE';
        this.sourceCode = dt.getValue('SOURCECODE') || '';
        this.description = dt.getValue('REMARK') || '';
        try {
          this.deps = dt.getValue('DEPS') ? JSON.parse(dt.getValue('DEPS')) : [];
        } catch (e) {
          this.deps = [];
        }
        this.dirty = false;
        this.statusMsg = '已加载: ' + this.templateName;
        this.statusIsError = false;
        this.$nextTick(function() {
          if (this.$refs.editor) {
            this.$refs.editor.setValue(this.sourceCode);
          }
        }.bind(this));
      } catch (e) {
        this.$error('加载失败: ' + (e.message || e));
      }
    },
    onCodeChange() {
      this.dirty = true;
      this.statusMsg = '';
    },
    // ====== AI 助手 ======
    _handleAiShortcut(e) {
      if (e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'i')) {
        e.preventDefault();
        this.switchToAi();
      }
    },
    switchToAi() {
      this.activeTab = 'ai';
      this.$nextTick(function() {
        if (this.$refs.aiPanel) {
          this.$refs.aiPanel.focusInput();
        }
      }.bind(this));
      // 加载同级文件上下文 (如果缓存有效则秒回)
      this.loadSiblingFiles();
    },
    /**
     * 获取当前文件的 AI 上下文信息
     */
    getAiCurrentFile() {
      if (!this.currentPath) return null;
      return {
        path: this.modulePath,
        type: this.fileType,
        content: this.sourceCode,
      };
    },
    /**
     * 加载同级文件 (同模块目录下的所有文件)
     * 在切换到 AI Tab 或首次发消息时加载
     */
    async loadSiblingFiles() {
      if (!this.currentPath) return;

      // 推导模块目录前缀
      // '@/pages/s01/m16/views/main.vue' → '@/pages/s01/m16/'
      var path = this.modulePath || this.currentPath;
      var parts = path.split('/');
      var pagesIdx = -1;
      for (var i = 0; i < parts.length; i++) {
        if (parts[i] === 'pages') { pagesIdx = i; break }
      }
      if (pagesIdx < 0 || pagesIdx + 2 >= parts.length) return;
      var dirPrefix = parts.slice(0, pagesIdx + 3).join('/'); // '@/pages/s01/m16'

      // 检查缓存是否有效
      if (this.siblingFilesCache &&
          this.siblingFilesCache.dirPrefix === dirPrefix &&
          this.siblingFilesCache.files.length > 0) {
        this.aiSiblingFiles = this.siblingFilesCache.files;
        this.aiCurrentFile = this.getAiCurrentFile();
        return;
      }

      try {
        // 查询所有文件 (空 FilterParams), 客户端按模块目录前缀过滤
        var ret = await this.$callAction({
          action: `${Constants.STORE_NAME}/listAssets`,
          param: {},
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        var files = [];
        for (var j = 0; j < items.length; j++) {
          var item = items[j];
          var itemPath = item.MODULEPATH || '';
          // 确保是同级文件 (前缀匹配)
          if (itemPath.indexOf(dirPrefix) === 0) {
            files.push({
              path: itemPath,
              type: item.FILETYPE || 'VUE',
              content: item.SOURCECODE || '',
            });
          }
        }
        this.siblingFilesCache = {
          dirPrefix: dirPrefix,
          files: files,
          loadedAt: Date.now(),
        };
        this.aiSiblingFiles = files;
        this.aiCurrentFile = this.getAiCurrentFile();
      } catch (e) {
        console.warn('[SfcEdit] 加载同级文件失败:', e);
        // 失败时至少提供当前文件
        this.aiSiblingFiles = [];
        this.aiCurrentFile = this.getAiCurrentFile();
      }
    },
    /**
     * 应用 AI 生成的代码到编辑器
     * @param {object} payload - {code, mode, searchReplace?}
     *   mode: 'replace'(替换全部) / 'search-replace'(精准替换) / 'insert'(插入光标) / 'newfile'(新建文件)
     */
    onApplyCode(payload) {
      // 多文件联动: 含 ###FILE: 段 → 逐文件落库(接口自动关联模块)
      var ops = parseAiFileBlocks(payload.code || '');
      if (ops) return this.applyAiFiles(ops);
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
          this.$error('所有 SEARCH 块均未匹配到原文，请检查代码是否已变更');
        }
      } else if (mode === 'replace') {
        if (this.dirty) {
          var ok = confirm('当前文件有未保存的修改, 替换全部会覆盖, 继续?');
          if (!ok) return;
        }
        // SEARCH/REPLACE 模式下"替换全部": 用所有 replace 部分拼接
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
      } else if (mode === 'newfile') {
        if (this.dirty) {
          var ok2 = confirm('当前文件有未保存的修改, 新建文件会丢弃修改, 继续?');
          if (!ok2) return;
        }
        this.$callAction({ action: Constants.STORE_NAME + '/add', isBusy: false });
        this.templateId = '';
        this.fileType = code.indexOf('<template>') >= 0 ? 'VUE' : 'JS';
        this.templateCode = 'ai_' + Date.now();
        this.templateName = 'AI 生成';
        this.modulePath = this.fileType === 'VUE' ? '@/pages/新模板.vue' : '@/pages/新模板.js';
        this.currentPath = this.modulePath;
        this.sourceCode = code;
        this.deps = [];
        this.dirty = true;
        this.statusMsg = 'AI 代码已创建为新文件';
        this.statusIsError = false;
        var self = this;
        this.$nextTick(function() {
          if (self.$refs.editor) {
            self.$refs.editor.setValue(self.sourceCode);
          }
        });
        this.$alert('已创建新文件');
      }
    },
    // 多文件联动落库：###FILE 段逐个打开/新建保存（脚本类自动关联模块接口），当前文件直接替换
    async applyAiFiles(ops) {
      var self = this;
      this.statusMsg = 'AI 多文件落库中 (' + ops.length + ' 个文件)...';
      var ret = await applyAiFileOps(ops, {
        moduleCode: this.dbModuleCode,
        currentPath: this.modulePath,
        applyCurrent: function(path, code) {
          self.sourceCode = code;
          if (self.$refs.editor) self.$refs.editor.setValue(code);
          self.dirty = true;
        },
      });
      var parts = [];
      if (ret.saved.length) parts.push('已保存 ' + ret.saved.length + ' 个');
      if (ret.linked.length) parts.push('关联接口: ' + ret.linked.join(', '));
      if (ret.skipped.length) parts.push('跳过: ' + ret.skipped.join(', '));
      if (ret.errors.length) parts.push('失败: ' + ret.errors.join('；'));
      this.statusMsg = parts.join(' · ');
      this.statusIsError = ret.errors.length > 0;
      if (this.statusIsError) this.$error(this.statusMsg);
      else this.$alert(this.statusMsg);
      this.$root.$emit('sfc-tree-refresh');
    },
    async handleCompile() {
      // 编译分流: csharp → Roslyn 编译检查; sql → 前端规则校验; sfc → 前端编译
      if (this.fileKind === 'csharp') return this.compileApiScript();
      if (this.fileKind === 'sql') return this.compileSqlTemplate();
      this.compiling = true;
      this.statusMsg = '';
      try {
        var result = await compileSFC(this.sourceCode, this.modulePath, this.fileType);
        this.deps = result.deps;
        this.statusMsg = '编译成功, 依赖: ' + result.deps.join(', ');
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
    // csharp 脚本: Roslyn 编译检查（code-asset 共用层）
    async compileApiScript() {
      this.compiling = true;
      this.statusMsg = '';
      try {
        var ret = await checkAsset('csharp', this.sourceCode);
        this.statusMsg = ret.message;
        this.statusIsError = !ret.passed;
        if (ret.passed) this.$alert(ret.message);
        else this.$error(ret.message);
      } finally {
        this.compiling = false;
      }
    },
    // SQL 模板: 前端规则校验（code-asset 共用层）
    async compileSqlTemplate() {
      var ret = await checkAsset('sql', this.sourceCode);
      this.statusMsg = ret.message;
      this.statusIsError = !ret.passed;
      if (ret.passed) this.$alert(ret.message);
      else this.$error(ret.message);
    },
    // 保存(快速, 不留版本) / 提交(留版本+变更说明)
    onQuickSave() {
      this.handleSave('', true);
    },
    onCommitSave(note) {
      this.handleSave(note, false);
    },
    async handleSave(note, skipVersion) {
      // 保存分流: csharp → RS_M21 A04; sql → RS_M13 A04; sfc → 原流程
      if (this.fileKind === 'csharp') return this.saveApiScript(note, skipVersion);
      if (this.fileKind === 'sql') return this.saveSqlTemplate(note, skipVersion);
      if (!this.templateCode.trim()) {
        this.$error('请输入模板编码');
        return;
      }
      if (!this.templateName.trim()) {
        this.$error('请输入模板名称');
        return;
      }
      if (!this.modulePath.trim()) {
        this.$error('请输入模块路径');
        return;
      }
      this.saving = true;
      this.statusMsg = '';
      try {
        var result = await compileSFC(this.sourceCode, this.modulePath, this.fileType);
        this.deps = result.deps;

        var now = new Date().toISOString().replace('T', ' ').substring(0, 19);
        var dt = this.$MAIN;
        if (!dt) {
          this.$error('DataTable 未初始化');
          return;
        }
        // 同步编辑器字段到 DataTable (DataTable 自动追踪 modify, getXML 自动生成正确 oc)
        dt.setValue('CODE', this.templateCode);
        dt.setValue('NAME', this.templateName);
        dt.setValue('MODULEPATH', this.modulePath);
        dt.setValue('FILETYPE', this.fileType);
        dt.setValue('SOURCECODE', this.sourceCode);
        dt.setValue('COMPILEDCODE', result.compiledCode);
        dt.setValue('DEPS', JSON.stringify(result.deps));
        dt.setValue('REMARK', this.description || '');
        dt.setValue('ISDELETED', '0');
        // 统一代码资产表类型标记(js/vue 按文件类型)
        dt.setValue('ASSETTYPE', this.fileType === 'JS' ? 'js' : 'vue');
        dt.setValue('MODIFYTIME', now);
        if (!this.templateId) {
          dt.setValue('CREATETIME', now);
        }

        // 调 Store03 save: 内部 getXML + POST A04 (CHANGENOTE 写入版本记录; SKIPVERSION=1 快速保存不留版本)
        await this.$callAction({
          action: `${Constants.STORE_NAME}/save`,
          param: { CHANGENOTE: note || '', SKIPVERSION: skipVersion ? '1' : null },
          isBusy: false,
        });

        // 保存成功后后端回写 ID 到 DataTable
        this.templateId = dt.getValue('ID');
        this.dirty = false;
        this.currentPath = this.modulePath;
        this.statusMsg = '保存成功';
        this.statusIsError = false;
        this.$alert('保存成功');
        // 失效 moduleCache 中此模块前缀下的所有条目, 让下次加载 (预览 / 已部署页面) 都拉最新代码
        // 推导前缀: '@/pages/s01/m16/views/main.vue' → '@/pages/s01/m16/'
        var pathPrefix = this.modulePath.substring(0, this.modulePath.lastIndexOf('/') + 1);
        var moduleDirPrefix = pathPrefix.substring(0, pathPrefix.length - 1); // 去掉尾部 /
        moduleDirPrefix = moduleDirPrefix.substring(0, moduleDirPrefix.lastIndexOf('/') + 1);
        invalidateCacheByPrefix(moduleDirPrefix);
        // 刷新文件树
        this.$root.$emit('sfc-tree-refresh');
      } catch (e) {
        this.statusMsg = '保存失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$error('保存失败: ' + (e.message || e));
      } finally {
        this.saving = false;
      }
    },
    // 保存 API 脚本 (C#): code-asset 共用层（先编译检查, DataTable setValue → Store03 save）
    async saveApiScript(changeNote, skipVersion) {
      if (!this.templateCode.trim()) {
        this.$error('请输入脚本编码');
        return;
      }
      if (!this.templateName.trim()) {
        this.$error('请输入脚本名称');
        return;
      }
      this.saving = true;
      this.statusMsg = '';
      try {
        var ret = await saveAsset('csharp', {
          code: this.templateCode,
          name: this.templateName,
          source: this.sourceCode,
          remark: this.description || '',
          path: this.modulePath,
          // 快速保存(不留版本)不递增资产 VERSION；提交才递增
          version: skipVersion ? (this.scriptVersion || 1) : (this.scriptVersion || 1) + 1,
          changeNote: changeNote || '',
          skipVersion: !!skipVersion,
        });
        if (!ret.passed) {
          this.statusMsg = ret.message;
          this.statusIsError = true;
          this.$error(ret.message);
          return;
        }
        this.templateId = ret.id;
        this.scriptVersion = ret.version;
        this.dirty = false;
        this.currentPath = this.modulePath;
        this.statusMsg = skipVersion ? '保存成功（未生成版本）' : '提交成功 (v' + this.scriptVersion + ')';
        this.statusIsError = false;
        this.$alert(skipVersion ? '保存成功' : '提交成功');
        this.$root.$emit('sfc-tree-refresh');
      } catch (e) {
        this.statusMsg = '保存失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$error(this.statusMsg);
      } finally {
        this.saving = false;
      }
    },
    // 保存 SQL 模板: code-asset 共用层（先规则校验）
    async saveSqlTemplate(changeNote, skipVersion) {
      if (!this.templateCode.trim()) {
        this.$error('请输入模板编码');
        return;
      }
      if (!this.templateName.trim()) {
        this.$error('请输入模板名称(备注)');
        return;
      }
      this.saving = true;
      this.statusMsg = '';
      try {
        var ret = await saveAsset('sql', {
          code: this.templateCode,
          name: this.templateName,
          source: this.sourceCode,
          path: this.modulePath,
          changeNote: changeNote || '',
          skipVersion: !!skipVersion,
        });
        if (!ret.passed) {
          this.statusMsg = ret.message;
          this.statusIsError = true;
          this.$error(ret.message);
          return;
        }
        this.templateId = ret.id;
        this.dirty = false;
        this.currentPath = this.modulePath;
        this.statusMsg = '保存成功';
        this.statusIsError = false;
        this.$alert('保存成功');
        this.$root.$emit('sfc-tree-refresh');
      } catch (e) {
        this.statusMsg = '保存失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$error(this.statusMsg);
      } finally {
        this.saving = false;
      }
    },
  },
};

var defaultVueTemplate = `<template>
  <div class="demo-page">
    <h2>{{ title }}</h2>
    <p>{{ message }}</p>
    <Button color="primary" @click="handleClick">点击我</Button>
    <p v-if="clicked">你点击了按钮!</p>
  </div>
</template>
<script>
export default {
  data() {
    return {
      title: '在线 SFC 示例',
      message: '这是一个在浏览器中编写的 Vue 组件',
      clicked: false,
    };
  },
  methods: {
    handleClick() {
      this.clicked = true;
      this.$alert('按钮被点击了');
    },
  },
};
<\/script>
<style scoped>
.demo-page {
  padding: 20px;
}
.demo-page h2 {
  color: #2d8cf0;
}
</style>
`;

var defaultJsTemplate = `// JS 模块示例
import db from '@/api/db';

export default {
  async getData() {
    var ret = await db.postData({
      api: '/api/data/call/RS_M17/A01/',
      params: { FilterParams: {}, PageSize: 20, PageIndex: 1 },
    });
    return ret;
  },
};
`;
</script>
<style lang="less" scoped>
.sfc-ide-page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #1e1e1e;
}
.sfc-ide-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 16px;
  background: #2d2d2d;
  border-bottom: 1px solid #3c3c3c;
  flex-shrink: 0;
  height: 44px;
}
.sfc-ide-toolbar-left {
  display: flex;
  align-items: center;
}
.sfc-ide-logo {
  color: #fff;
  font-size: 14px;
  font-weight: bold;
}
.sfc-ide-toolbar-center {
  flex: 1;
  display: flex;
  justify-content: center;
  padding: 0 20px;
}
.sfc-ide-file-info {
  display: flex;
  gap: 6px;
}
.sfc-kind-badge {
  color: #9b59b6;
  font-size: 12px;
  font-weight: bold;
  padding: 2px 8px;
  border: 1px solid #9b59b6;
  border-radius: 3px;
  white-space: nowrap;
  align-self: center;
}
.sfc-input {
  background: #3c3c3c;
  border: 1px solid #3c3c3c;
  border-radius: 3px;
  padding: 4px 8px;
  color: #ddd;
  font-size: 12px;
  outline: none;
  &:focus {
    border-color: #0a84ff;
  }
}
.sfc-input-type {
  width: 60px;
  cursor: pointer;
}
.sfc-input-code {
  width: 100px;
}
.sfc-input-name {
  width: 140px;
}
.sfc-input-path {
  width: 250px;
}
.sfc-input-module {
  width: 160px;
}
.sfc-ide-toolbar-right {
  display: flex;
  gap: 6px;
}
.sfc-ide-body {
  display: flex;
  flex: 1;
  overflow: hidden;
}
.sfc-ide-sidebar {
  width: 240px;
  flex-shrink: 0;
  border-right: 1px solid #3c3c3c;
  overflow: hidden;
}
.sfc-ide-editor {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-right: 1px solid #3c3c3c;
}
.sfc-ide-preview {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sfc-ide-section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 12px;
  background: #252526;
  border-bottom: 1px solid #3c3c3c;
  font-size: 12px;
  color: #888;
  flex-shrink: 0;
}
.sfc-ide-section-label {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
}
.sfc-ide-section-actions {
  flex-shrink: 0;
  margin-left: 8px;
  display: inline-flex;
  align-items: center;
}
/* AI 助手 Tab 栏 */
.sfc-ide-tab-bar {
  padding: 0 8px 0 0;
}
.sfc-ide-tabs {
  display: flex;
  align-items: center;
  flex: 1;
  height: 100%;
}
.sfc-ide-tab {
  padding: 4px 16px;
  cursor: pointer;
  color: #888;
  font-size: 12px;
  height: 100%;
  display: flex;
  align-items: center;
  border-bottom: 2px solid transparent;
  user-select: none;
  &:hover {
    color: #ccc;
  }
  &.active {
    color: #fff;
    border-bottom-color: #0a84ff;
  }
}
.sfc-ide-fs-btn {
  cursor: pointer;
  color: #888;
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 14px;
  line-height: 1;
  user-select: none;
  &:hover {
    color: #fff;
    background: #3c3c3c;
  }
}
.sfc-ide-section-badge {
  display: inline-block;
  margin-left: 8px;
  padding: 1px 6px;
  background: #e6a23c;
  color: #fff;
  border-radius: 3px;
  font-size: 10px;
}
/* 全屏时: 让编辑器/预览分栏脱离三栏 flex, 占满屏幕 */
.sfc-ide-editor:fullscreen,
.sfc-ide-preview:fullscreen,
.sfc-ide-editor:-webkit-full-screen,
.sfc-ide-preview:-webkit-full-screen {
  width: 100vw;
  height: 100vh;
  background: #1e1e1e;
}
.sfc-ide-page:fullscreen,
.sfc-ide-page:-webkit-full-screen {
  width: 100vw;
  height: 100vh;
  background: #1e1e1e;
}
.sfc-ide-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #555;
  p {
    margin-top: 12px;
    font-size: 13px;
  }
}
.sfc-ide-empty-icon {
  font-size: 48px;
}
.sfc-ide-statusbar {
  display: flex;
  justify-content: space-between;
  padding: 3px 16px;
  background: #007acc;
  color: #fff;
  font-size: 12px;
  flex-shrink: 0;
  height: 22px;
  align-items: center;
}
.sfc-status-msg.sfc-status-error {
  color: #f48771;
}
.sfc-status-deps {
  opacity: 0.8;
}
/* 插入模板 Modal */
.sfc-tpl-modal-body {
  padding: 4px 8px;
}
.sfc-tpl-desc {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #f0f7ff;
  border: 1px solid #d6e8ff;
  border-radius: 6px;
  padding: 10px 14px;
  margin-bottom: 18px;
  color: #1d6fdc;
  font-size: 12px;
  line-height: 1.5;
  i {
    color: #0a84ff;
    font-size: 16px;
    flex-shrink: 0;
  }
}
.sfc-tpl-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.sfc-tpl-row {
  display: flex;
  align-items: center;
  label {
    width: 100px;
    text-align: right;
    padding-right: 14px;
    color: #515a6e;
    font-size: 13px;
    flex-shrink: 0;
  }
}
.sfc-tpl-field {
  flex: 1;
}
.sfc-tpl-input,
.sfc-tpl-select {
  width: 100%;
  border: 1px solid #dddee1;
  border-radius: 4px;
  padding: 6px 10px;
  font-size: 13px;
  outline: none;
  box-sizing: border-box;
  &:focus {
    border-color: #0a84ff;
  }
}
.sfc-tpl-files {
  margin-top: 20px;
  padding-top: 18px;
  border-top: 1px dashed #e0e0e0;
}
.sfc-tpl-files-title {
  margin: 0 0 12px 0;
  color: #515a6e;
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
  i {
    color: #0a84ff;
  }
}
.sfc-tpl-file-btns {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}
.sfc-tpl-modal-footer {
  text-align: right;
}
</style>
