<template>
  <rs-modal ref="modal" v-model="visible" auto-width>
    <view-dialog :title="title" style="width: 1100px;">
      <template slot="header">
        <Button size="s" @click="openAi">AI 助手</Button>
        <Button size="s" :disabled="!assetId" @click="openHistory">版本</Button>
        <Button size="s" v-if="kind !== 'js' && kind !== 'vue'" :color="testOpen ? 'primary' : null" @click="testOpen = !testOpen">测试</Button>
        <Button size="s" :loading="checking" @click="handleCheck">编译</Button>
        <save-actions :loading="saving" @save="onQuickSave" @commit="onCommitSave" />
      </template>
      <template slot="body">
        <div class="ca-popup-body">
          <!-- 左侧: 文件列表（API 脚本 / SQL 模板 两组） -->
          <div class="ca-popup-filelist">
            <div class="ca-group" v-for="g in groups" :key="g.kind">
              <div class="ca-group-header">
                <span class="ca-kind" :class="g.kind">{{ g.label }} ({{ g.items.length }})</span>
                <span class="ca-group-actions">
                  <i v-if="g.kind !== 'js' && g.kind !== 'vue'" class="h-icon-link ca-act" title="选入已有资产(关联到当前模块)" @click="openSelector(g.kind)"></i>
                  <i class="h-icon-plus ca-act" title="新建" @click="newFile(g.kind, moduleCode)"></i>
                </span>
              </div>
              <div class="ca-group-items">
                <div
                  v-for="f in g.items"
                  :key="f.rid"
                  :class="['ca-file-item', { active: kind === g.kind && assetId === f.rid }]"
                  :title="(f.path || f.code) + (f.name ? ' · ' + f.name : '')"
                  @click="switchFile(g.kind, f)"
                >
                  <span class="ca-file-code">
                    {{ fileLabel(g.kind, f) }}
                    <span v-if="f.apiCode" class="ca-file-api">{{ f.apiCode }}</span>
                    <span v-if="!isOwn(g.kind, f)" class="ca-file-ext">外链</span>
                  </span>
                  <span class="ca-file-name">{{ f.name }}</span>
                  <span class="ca-file-ops" @click.stop>
                    <Poptip
                      v-if="isOwn(g.kind, f)"
                      :content="'删除文件 ' + f.code + '？（同时移除本模块接口关联，不可恢复）'"
                      transfer
                      @confirm="doDelete(g.kind, f)"
                    >
                      <i class="h-icon-trash ca-op danger" title="删除文件"></i>
                    </Poptip>
                    <Poptip
                      v-else
                      :content="'移除 ' + f.code + ' 与当前模块的接口关联？（文件本身保留）'"
                      transfer
                      @confirm="doUnlink(g.kind, f)"
                    >
                      <i class="h-icon-close ca-op" title="移除接口关联"></i>
                    </Poptip>
                  </span>
                </div>
                <div v-if="g.items.length === 0" class="ca-file-empty">暂无</div>
              </div>
            </div>
          </div>

          <!-- 右侧: 工具栏 + 编辑器 + 状态栏（选入模式时切换为选择器） -->
          <div class="ca-popup-main" v-if="!selectorOpen">
            <div class="ca-popup-toolbar">
              <input v-model="CODE" placeholder="编码" class="ca-input ca-code" />
              <input v-model="NAME" placeholder="名称" class="ca-input ca-name" />
              <input v-model="MODULEPATH" :placeholder="pathPlaceholder" class="ca-input ca-path" @change="onPathChange" />
              <input v-model="REMARK" placeholder="备注(可选)" class="ca-input ca-remark" />
              <span class="ca-flex"></span>
            </div>
            <div class="ca-popup-editor">
              <div class="ca-popup-section-title">
                <span>{{ MODULEPATH || CODE || '代码编辑' }}</span>
                <span v-if="dirty" class="ca-badge">未保存</span>
              </div>
              <sfc-code-editor
                v-if="editorReady"
                ref="editor"
                v-model="SOURCECODE"
                :fileType="meta.fileType"
                @change="onCodeChange"
              ></sfc-code-editor>
              <div v-else class="ca-editor-empty">从左侧选择文件，或点 + 新建</div>
            </div>
            <div class="ca-popup-status">
              <span :class="{ 'ca-status-error': statusIsError }">{{ statusMsg || '就绪' }}</span>
            </div>
          </div>
          <!-- 选入面板：把其他模块的已有资产关联到当前模块 -->
          <div class="ca-popup-main" v-else>
            <div class="ca-sel-head">
              <span class="ca-kind" :class="selectorKind">{{ ASSET_META[selectorKind].kindBadge }}</span>
              <span class="ca-sel-title">选入到 {{ moduleCode }}（可多选，保存后自动分配接口编码）</span>
              <span class="ca-flex"></span>
              <input v-model="selSearch" placeholder="搜索编码/名称..." class="ca-input ca-sel-search" />
            </div>
            <div class="ca-sel-list">
              <div
                v-for="f in selectorFilteredItems"
                :key="f.rid"
                :class="['ca-sel-item', { active: f._checked }]"
                @click="f._checked = !f._checked"
              >
                <i :class="f._checked ? 'h-icon-check' : 'h-icon-plus'"></i>
                <span class="ca-sel-code">{{ fileLabel(selectorKind, f) }}</span>
                <span class="ca-sel-name">{{ f.name }}</span>
              </div>
              <div v-if="selectorFilteredItems.length === 0" class="ca-file-empty">
                {{ selSearch ? '无匹配资产' : '没有可选入的资产（已全部在当前模块）' }}
              </div>
            </div>
            <div class="ca-sel-foot">
              <span class="ca-sel-count">已选 {{ selectorChecked.length }} 项</span>
              <span class="ca-flex"></span>
              <Button size="s" @click="selectorOpen = false">取消</Button>
              <Button size="s" color="primary" :loading="selLinking" :disabled="selectorChecked.length === 0" @click="doLinkSelected">选入关联</Button>
            </div>
          </div>

          <!-- 右侧: 接口测试面板（csharp/sql；参数自动识别，支持源码试运行/接口执行） -->
          <div v-if="testOpen && kind !== 'js' && kind !== 'vue'" class="ca-test-side">
            <code-test-panel :kind="kind" :source="SOURCECODE" :code="CODE" />
          </div>
        </div>
      </template>
    </view-dialog>
    <!-- 通用版本历史弹窗 -->
    <version-history-popup ref="verHistory" @rollback="onVersionRollback" />
  </rs-modal>
</template>

<script>
import sfcCodeEditor from '@/pages/s01/m17/components/sfc-code-editor.vue';
import saveActions from './save-actions.vue';
import versionHistoryPopup from './version-history-popup.vue';
import codeTestPanel from './code-test-panel.vue';
import { Constants as CEP, mapGetters as cepMapGetters } from './code-editor-store';
import { mapDateTable as m17MapDateTable, getStoreResult as m17GetStoreResult } from '@/pages/s01/m17/store';
import { ASSET_META, STORE_NS, openAsset, addAsset, checkAsset, saveAsset, applyAiFileOps, parseAiFileBlocks, kindOfPath, defaultCsharpTemplate, defaultSqlTemplate, defaultJsTemplate, defaultVueTemplate, loadModuleCodes, deriveAssetDir, recomposeAssetCode, deriveScriptPath, deriveTplCode } from '@/pages/s01/m17/code-asset';
import { invalidateCacheByPrefix } from '@/sfc-loader';

// 把方法骨架插入到 export default 的指定块（methods/computed）
// 返回 { source, inserted, reason? }: 方法名已存在→不重复插入；无锚点→返回原文
function insertMethodSnippet(source, block, snippet) {
  var nameMatch = snippet.match(/^\s*(?:async\s+)?([A-Za-z_$][\w$]*)\s*\(/m);
  if (nameMatch) {
    var existRe = new RegExp('(?:async\\s+)?' + nameMatch[1] + '\\s*\\(');
    if (existRe.test(source)) return { source: source, inserted: false, reason: 'exists' };
  }
  var indented = snippet.split('\n').map(function(l) { return l ? '    ' + l : l }).join('\n');
  // 锚点1: 已有目标块（methods: { / computed: {）→ 插入到块首
  var blockRe = new RegExp('(\\n[ \\t]*' + block + '\\s*:\\s*\\{[^\\n]*)\\n');
  var m = blockRe.exec(source);
  if (m) {
    var idx = m.index + m[1].length + 1;
    return { source: source.slice(0, idx) + indented + '\n' + source.slice(idx), inserted: true };
  }
  // 锚点2: 没有目标块 → 在 export default { 后新建整个块
  var edRe = /(export\s+default\s*\{[^\n]*)\n/;
  var m2 = edRe.exec(source);
  if (m2) {
    var idx2 = m2.index + m2[1].length + 1;
    var blockCode = '  ' + block + ': {\n' + indented + '\n  },\n';
    return { source: source.slice(0, idx2) + blockCode + source.slice(idx2), inserted: true };
  }
  return { source: source, inserted: false, reason: 'noanchor' };
}

export default {
  name: 'code-editor-popup',
  components: { sfcCodeEditor, saveActions, versionHistoryPopup, codeTestPanel },
  data() {
    return {
      visible: false,
      // 当前编辑资产的类型与 UI 状态（编辑字段通过 mapDateTable 绑 MAIN DataTable）
      kind: 'csharp',
      dirty: false,
      editorReady: false,
      saving: false,
      checking: false,
      statusMsg: '',
      statusIsError: false,
      // 模块上下文（从模块配置打开时传入，限定列表范围+保存后自动关联）
      moduleCode: '',
      // 全部模块编码（目录推导用）+ 当前文件目录
      moduleCodes: [],
      assetDir: '',
      // 选入面板状态
      selectorOpen: false,
      selectorKind: 'csharp',
      selSearch: '',
      selectorItems: [],
      selLinking: false,
      // 接口测试面板
      testOpen: false,
    };
  },
  created() {
    // 强制注册 s01/m17 store（含 MAIN DataTable），保证 mapDateTable 立即可用
    m17GetStoreResult();
  },
  computed: {
    ...cepMapGetters([
      'groupCsharp', 'groupSql', 'groupJs', 'groupVue',
      'selectorItemsCsharp', 'selectorItemsSql', 'selectorItemsJs', 'selectorItemsVue',
    ]),
    // 当前编辑资产的字段直接绑 MAIN DataTable（mapDateTable 生成 get/set 自动读写）
    // 模板 v-model="CODE" / v-model="SOURCECODE" 即双向绑定，不再走组件 data
    ...m17MapDateTable('MAIN', [
      'ID', 'CODE', 'NAME', 'SOURCECODE', 'REMARK',
      'MODULEPATH', 'VERSION', 'ASSETTYPE',
    ]),
    // 语义化别名（与原 .vue 内字段名对齐，模板/方法不动）
    assetId() { return this.ID },
    code() { return this.CODE },
    name() { return this.NAME },
    remark() { return this.REMARK },
    source() { return this.SOURCECODE },
    path() { return this.MODULEPATH },
    version() { return this.VERSION },
    // 左侧文件列表（API 脚本 / SQL 模板 / JS 模块 / Vue 组件 四组）
    // items 由 store getters 派生（filter/map 已收口到 store）
    groups() {
      return [
        { kind: 'csharp', label: 'API 脚本', items: this.groupCsharp },
        { kind: 'sql', label: 'SQL 模板', items: this.groupSql },
        { kind: 'js', label: 'JS 模块', items: this.groupJs },
        { kind: 'vue', label: 'Vue 组件', items: this.groupVue },
      ];
    },
    ASSET_META() {
      return ASSET_META;
    },
    meta() {
      return ASSET_META[this.kind];
    },
    title() {
      return '模块脚本 · ' + this.meta.kindBadge + (this.code ? ' · ' + this.code : '');
    },
    selectorChecked() {
      return this.selectorItems.filter(function(f) { return f._checked });
    },
    // 路径占位提示（按类型）
    pathPlaceholder() {
      if (this.kind === 'js') return '路径(@/modules/模块/xxx.js)';
      if (this.kind === 'vue') return '路径(@/pages/模块/xxx.vue)';
      if (this.kind === 'csharp') return '路径(@/scripts/模块/编码.cs)';
      return '路径(@/scripts/模块/编码.sql)';
    },
    // 选入列表（按搜索词过滤）
    selectorFilteredItems() {
      var kw = (this.selSearch || '').toLowerCase();
      if (!kw) return this.selectorItems;
      return this.selectorItems.filter(function(f) {
        return (f.code || '').toLowerCase().indexOf(kw) >= 0 || (f.name || '').toLowerCase().indexOf(kw) >= 0;
      });
    },
  },
  watch: {
    // 类型/模块变化时同步 sfcContext（仅当当前上下文是自己，避免误改其他编辑器的注册）
    kind(newVal) {
      if (this.$store.state.sfcContext.editorRef === this) {
        this.$store.commit('sfcContext/UPDATE', { editTarget: newVal });
      }
    },
    moduleCode(newVal) {
      if (this.$store.state.sfcContext.editorRef === this) {
        this.$store.commit('sfcContext/UPDATE', { moduleCode: newVal });
      }
    },
    visible(v) {
      if (!v && this.$store.state.sfcContext.editorRef === this) {
        this.$store.commit('sfcContext/CLEAR');
      }
    },
  },
  beforeDestroy() {
    if (this.$store.state.sfcContext.editorRef === this) {
      this.$store.commit('sfcContext/CLEAR');
    }
  },
  methods: {
    // ====== 打开弹窗（moduleCode: 限定只列该模块相关资产；为空则列全部） ======
    async show(moduleCode) {
      this.moduleCode = moduleCode || this.moduleCode || '';
      this.visible = true;
      this.moduleCodes = await loadModuleCodes();
      await this.loadGroups();
      // 注册到全局 sfcContext：智能助理的"开发"TAB 即可对当前脚本生效（与 IDE/SFC弹窗 同一通道）
      this.$store.commit('sfcContext/SET', {
        editorRef: this,
        moduleCode: this.moduleCode,
        editTarget: this.kind,
        siblingFiles: [],
        active: true,
      });
    },
    // ====== 智能助理"开发"TAB 集成（sfcContext 接口：getAiCurrentFile + onApplyCode） ======
    openAi() {
      // eslint-disable-next-line no-restricted-syntax
      if (!this.$store.state.assistant.visible) this.$store.dispatch('assistant/toggle');
      // eslint-disable-next-line no-restricted-syntax
      this.$store.dispatch('assistant/setAgent', 'sfc');
    },
    // 供开发 agent 取当前文件（assistant/send 每次现取，保证最新代码）
    getAiCurrentFile() {
      if (!this.editorReady) return null;
      return { path: this.path || this.code, type: this.meta.fileType, content: this.source };
    },
    // 加载左侧三组列表（实际派生在 store getters，组件只负责选 action）
    async loadGroups() {
      try {
        if (this.moduleCode) {
          // 模块上下文：RS_M18 A06 查"关联+前缀"的模块相关资产，state.moduleMode=true
          await this.$callAction({
            action: CEP.STORE_NAME + '/loadModuleAssets',
            param: { moduleCode: this.moduleCode },
            isBusy: false,
          });
          return;
        }
        // 无模块上下文：RS_M17/A01 全量取数，state.moduleMode=false
        await this.$callAction({
          action: CEP.STORE_NAME + '/loadAllAssets',
          param: {},
          isBusy: false,
        });
      } catch (e) {
        console.error('[code-editor-popup] 加载文件列表失败:', e);
      }
    },
    // ====== 打开已有资产（外部调用入口；moduleCode 限定列表范围+保存后自动关联） ======
    async open(kind, idValue, moduleCode) {
      this.moduleCode = moduleCode || '';
      await this.show(this.moduleCode);
      await this.loadAsset(kind, idValue);
    },
    // ====== 新建（外部调用入口；modulePrefix 即模块编码，作编码前缀+关联目标） ======
    async openNew(kind, modulePrefix) {
      this.moduleCode = modulePrefix || '';
      await this.show(this.moduleCode);
      await this.newFile(kind, modulePrefix);
    },
    // ====== 按路径打开 JS 模块并插入方法骨架（按钮钩子/显隐/动态参数 插入入口） ======
    // insertMethod: { name, block('methods'|'computed'), snippet }
    async openJsInsert(modulePath, moduleCode, insertMethod) {
      await this.openJs(modulePath, moduleCode);
      if (!insertMethod || !insertMethod.snippet) return;
      var r = insertMethodSnippet(this.source, insertMethod.block || 'methods', insertMethod.snippet);
      if (!r.inserted) {
        if (r.reason === 'exists') {
          this.statusMsg = '方法已存在: ' + insertMethod.name + '，可直接编辑';
          this.statusIsError = false;
          this.$Message.warning('方法 ' + insertMethod.name + ' 已存在，未重复插入，可直接编辑');
        } else {
          this.statusMsg = '未找到插入锚点，请手动添加方法';
          this.statusIsError = true;
          this.$Message.error('未找到插入锚点（export default / ' + (insertMethod.block || 'methods') + ' 块），请手动添加方法');
        }
        return;
      }
      this.SOURCECODE = r.source;
      this.dirty = true;
      this.editorReady = true;
      this.statusMsg = '已插入方法骨架: ' + insertMethod.name + '，确认后提交保存';
      this.statusIsError = false;
      this.$Message.success('已插入方法骨架: ' + insertMethod.name + '，确认后提交保存');
      this.$nextTick(() => {
        if (this.$refs.editor) this.$refs.editor.setValue(this.SOURCECODE);
      });
    },
    // ====== 按路径打开 JS 模块（Store扩展/扩展JS 入口；不存在则新建） ======
    async openJs(modulePath, moduleCode) {
      this.moduleCode = moduleCode || '';
      await this.show(this.moduleCode);
      try {
        var ret = await this.$callAction({
          action: CEP.STORE_NAME + '/findAssetsByPath',
          param: { modulePath: modulePath },
          isBusy: false,
        });
        var rows = (ret && ret.Items) || [];
        if (rows.length > 0) {
          await this.loadAsset('js', rows[0].ID);
        } else {
          await this.newFile('js', moduleCode, modulePath);
        }
      } catch (e) {
        this.$Message.error('加载失败: ' + (e.message || e));
      }
    },
    // ====== 列表点击切换 ======
    async switchFile(kind, f) {
      if (this.dirty) {
        var ok = confirm('当前文件未保存，是否丢弃修改？');
        if (!ok) return;
      }
      await this.loadAsset(kind, f.rid);
    },
    async loadAsset(kind, idValue) {
      this.statusMsg = '加载中...';
      this.statusIsError = false;
      try {
        var dt = await openAsset(kind, idValue);
        if (!dt || !dt.data || dt.data.length === 0) {
          this.statusMsg = '未找到数据';
          this.statusIsError = true;
          return;
        }
        // 字段已绑 MAIN DataTable，openAsset 已把数据加载进 dt；
        // 这里只需同步 UI 状态与派生字段
        this.kind = kind;
        // 脚本类若 MODULEPATH 为空（历史数据）按约定补全
        if (kind !== 'js' && kind !== 'vue' && !dt.getValue('MODULEPATH')) {
          dt.setValue('MODULEPATH', await deriveScriptPath(kind, dt.getValue('CODE'), this.moduleCodes));
        }
        // VERSION 对 sql/js 无意义，前端展示统一兜底为 1
        if (kind !== 'csharp') dt.setValue('VERSION', 1);
        this.assetDir = deriveAssetDir(dt.getValue('CODE'), kind, this.moduleCodes);
        this.dirty = false;
        this.editorReady = true;
        this.statusMsg = '';
        this.$nextTick(() => {
          if (this.$refs.editor) this.$refs.editor.setValue(this.SOURCECODE);
        });
      } catch (e) {
        this.statusMsg = '加载失败: ' + (e.message || e);
        this.statusIsError = true;
      }
    },
    // ====== 新建 ======
    async newFile(kind, modulePrefix, fixedPath) {
      if (this.dirty) {
        var ok = confirm('当前文件未保存，是否丢弃修改？');
        if (!ok) return;
      }
      var meta = ASSET_META[kind];
      try {
        await addAsset(kind);
      } catch (e) {
        this.statusMsg = '初始化失败: ' + (e.message || e);
        this.statusIsError = true;
        return;
      }
      this.kind = kind;
      // 新建行：MAIN DataTable 已被 addAsset(INIT+ADD) 清空；以下字段写入 dt，v-model 自动同步
      this.VERSION = 1;
      this.REMARK = '';
      if (kind === 'js') {
        // JS 模块: 编码(从路径推导 {模块编码}_{页面编码})+路径 两个独立字段
        this.MODULEPATH = fixedPath || ('@/modules/' + (modulePrefix || 'DEMO') + '/新模块.js');
        this.CODE = deriveTplCode(this.MODULEPATH);
        this.assetDir = modulePrefix || '公共';
        this.NAME = this.MODULEPATH.substring(this.MODULEPATH.lastIndexOf('/') + 1).replace('.js', '');
        this.SOURCECODE = defaultJsTemplate;
      } else if (kind === 'vue') {
        this.MODULEPATH = fixedPath || ('@/pages/' + (modulePrefix || 'DEMO').toLowerCase() + '/新组件.vue');
        this.CODE = deriveTplCode(this.MODULEPATH);
        this.assetDir = modulePrefix || '公共';
        this.NAME = this.MODULEPATH.substring(this.MODULEPATH.lastIndexOf('/') + 1).replace('.vue', '');
        this.SOURCECODE = defaultVueTemplate;
      } else {
        this.CODE = meta.codePrefix + (modulePrefix ? modulePrefix + '_' : '') + 'NEW';
        this.assetDir = modulePrefix || '公共';
        this.MODULEPATH = await deriveScriptPath(kind, this.CODE, this.moduleCodes);
        this.NAME = '新建' + meta.kindBadge;
        this.SOURCECODE = kind === 'csharp' ? defaultCsharpTemplate : defaultSqlTemplate;
      }
      this.dirty = true;
      this.editorReady = true;
      this.statusMsg = '新建' + meta.kindBadge + '，修改后保存';
      this.statusIsError = false;
      this.$nextTick(() => {
        if (this.$refs.editor) this.$refs.editor.setValue(this.source);
      });
    },
    onCodeChange() {
      this.dirty = true;
    },
    // ====== AI 应用代码（多文件 ###FILE 协议 / search-replace / replace / insert / newfile） ======
    onApplyCode(payload) {
      var code = payload.code || '';
      // 新建文件: 加载为未保存的新文件，用户确认后自己保存/提交
      if (payload.mode === 'newfile') return this.createFileFromAi(code);
      // 多文件联动: 含 ###FILE: 段 → 逐文件落库(接口自动关联模块)
      var ops = parseAiFileBlocks(code);
      if (ops) return this.applyAiFiles(ops);
      if (!this.$refs.editor) {
        this.$Message.error('编辑器未就绪');
        return;
      }
      var mode = payload.mode;
      var searchReplace = payload.searchReplace || [];
      if (mode === 'search-replace') {
        var result = this.$refs.editor.applySearchReplace(searchReplace);
        if (result.applied > 0) {
          this.SOURCECODE = this.$refs.editor.getValue();
          this.dirty = true;
          this.$Message.success('已应用 ' + result.applied + ' 处修改' + (result.failed.length > 0 ? '，' + result.failed.length + ' 处匹配失败' : ''));
        } else {
          this.$Message.error('所有 SEARCH 块均未匹配到原文，请检查代码是否已变更');
        }
      } else if (mode === 'replace') {
        if (this.dirty) {
          var ok = confirm('当前文件有未保存的修改, 替换全部会覆盖, 继续?');
          if (!ok) return;
        }
        if (searchReplace.length > 0) {
          code = searchReplace.map(function(b) { return b.replace }).join('\n\n');
        }
        this.SOURCECODE = code;
        this.$refs.editor.setValue(code);
        this.dirty = true;
        this.$Message.success('已替换全部代码');
      } else if (mode === 'insert') {
        this.$refs.editor.insertAtCursor(code);
        this.SOURCECODE = this.$refs.editor.getValue();
        this.dirty = true;
        this.$Message.success('已插入到光标位置');
      }
    },
    // AI「新建文件」：内容加载为未保存文件（已存在则打开替换内容），用户确认后自己保存/提交
    async createFileFromAi(code) {
      if (this.dirty) {
        var ok = confirm('当前文件有未保存的修改, 新建会丢弃, 继续?');
        if (!ok) return;
      }
      // 解析路径/类型: 有 ###FILE 标记按标记(取第 1 个)，无标记按当前类型推导
      var ops = parseAiFileBlocks(code);
      var kind;
      var path;
      var content;
      var extra = 0;
      if (ops && ops.length > 0) {
        kind = kindOfPath(ops[0].path);
        path = ops[0].path;
        content = ops[0].code;
        extra = ops.length - 1;
      } else {
        kind = this.kind;
        content = code;
        path = kind === 'js' ?
          ('@/modules/' + (this.moduleCode || 'DEMO') + '/新模块.js') :
          kind === 'vue' ?
            ('@/pages/' + (this.moduleCode || 'DEMO').toLowerCase() + '/新组件.vue') :
            await deriveScriptPath(kind, (kind === 'csharp' ? 'SC_' : 'SS_') + (this.moduleCode ? this.moduleCode + '_' : '') + 'NEW', this.moduleCodes);
      }
      if (!kind) {
        this.$Message.error('无法识别内容类型（仅支持 cs/sql/js/vue）');
        return;
      }
      var fileName = path.substring(path.lastIndexOf('/') + 1);
      var base = fileName.replace(/\.(cs|sql|js|vue)$/i, '');
      var codeName = (kind === 'js' || kind === 'vue') ? deriveTplCode(path) : base;
      // 已存在 → 打开并替换内容(未保存)；不存在 → 新建空行
      var existingId = await this.findAssetId(kind, (kind === 'js' || kind === 'vue') ? path : codeName);
      if (existingId) {
        await this.loadAsset(kind, existingId);
        this.statusMsg = '文件已存在，内容已替换（未保存，确认后提交）';
      } else {
        try {
          await addAsset(kind);
        } catch (e) {
          this.statusMsg = '初始化失败: ' + (e.message || e);
          this.statusIsError = true;
          return;
        }
        this.kind = kind;
        this.VERSION = 1;
        this.statusMsg = 'AI 新文件已创建（未保存，确认后提交）';
      }
      // 字段写入 MAIN DataTable，v-model 自动同步
      this.CODE = codeName;
      this.NAME = base;
      this.MODULEPATH = path;
      this.assetDir = this.moduleCode || '公共';
      this.REMARK = '';
      this.SOURCECODE = content;
      this.editorReady = true;
      this.dirty = true;
      this.statusIsError = false;
      if (extra > 0) {
        this.statusMsg += '；该代码块还有 ' + extra + ' 个文件，请用「应用修改」批量落库';
      }
      this.$nextTick(() => {
        if (this.$refs.editor) this.$refs.editor.setValue(this.SOURCECODE);
      });
    },
    // 查资产 ID: js 按路径(A06)，csharp/sql 按编码(A01)；不存在返回 null
    async findAssetId(kind, key) {
      if (kind === 'js' || kind === 'vue') {
        var ret = await this.$callAction({
          action: CEP.STORE_NAME + '/findAssetsByPath',
          param: { modulePath: key },
          isBusy: false,
        });
        var rows = (ret && ret.Items) || [];
        return rows.length > 0 ? rows[0].ID : null;
      }
      var r2 = await this.$callAction({
        action: CEP.STORE_NAME + '/findAssetsByCode',
        param: { assetType: kind, code: key },
        isBusy: false,
      });
      var items = (r2 && r2.Items) || [];
      var found = null;
      items.forEach(function(x) { if (x.CODE === key) found = x; });
      return found ? found.ID : null;
    },
    // 多文件联动落库：###FILE 段逐个打开/新建保存（脚本类自动关联模块接口），当前文件直接替换
    async applyAiFiles(ops) {
      var self = this;
      this.statusMsg = 'AI 多文件落库中 (' + ops.length + ' 个文件)...';
      var ret = await applyAiFileOps(ops, {
        moduleCode: this.moduleCode,
        currentPath: this.path,
        applyCurrent: function(path, code) {
          self.SOURCECODE = code;
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
      if (ret.errors.length > 0) this.$Message.error(this.statusMsg);
      else this.$Message.success(this.statusMsg);
      await this.loadGroups();
    },
    // 目录编辑：重组编码前缀（SC_OLD_MOD_X → SC_NEW_MOD_X），标记未保存
    onDirChange() {
      var newCode = recomposeAssetCode(this.CODE, this.kind, this.assetDir, this.moduleCodes);
      if (newCode !== this.CODE) {
        this.CODE = newCode;
        this.dirty = true;
      }
    },
    // 路径编辑：标记未保存（JS 模块路径即身份，编码随路径重新推导 {模块编码}_{页面编码}）
    onPathChange() {
      if (this.kind === 'js' && this.MODULEPATH) {
        this.CODE = deriveTplCode(this.MODULEPATH);
      }
      if (this.kind === 'vue' && this.MODULEPATH) {
        this.CODE = deriveTplCode(this.MODULEPATH);
      }
      this.dirty = true;
    },
    // 列表显示标签统一（与在线代码 IDE 文件树同款）:
    // csharp=编码.cs / sql=编码.sql / js=路径文件名.js; 完整路径放 title
    fileLabel(kind, f) {
      if (kind === 'csharp') return (f.code || '') + '.cs';
      if (kind === 'sql') return (f.code || '') + '.sql';
      var p = f.path || '';
      if (p) return p.substring(p.lastIndexOf('/') + 1);
      return f.code || '';
    },
    // 是否当前模块自有资产（目录推导；外链资产只能移除关联不能删除；JS 模块按路径归属判断）
    isOwn(kind, f) {
      if (!this.moduleCode) return true;
      if (kind === 'js') {
        return (f.path || f.code || '').indexOf('/modules/' + this.moduleCode + '/') >= 0;
      }
      if (kind === 'vue') {
        return (f.path || f.code || '').indexOf('/' + this.moduleCode.toLowerCase() + '/') >= 0;
      }
      return deriveAssetDir(f.code, kind, this.moduleCodes) === this.moduleCode;
    },
    // ====== 选入已有资产（其他模块的，关联到当前模块） ======
    async openSelector(kind) {
      if (!this.moduleCode) {
        this.$Message.warning('选入需要在模块上下文打开（从模块配置进入）');
        return;
      }
      this.selectorKind = kind;
      this.selSearch = '';
      this.selectorOpen = true;
      // 先清空本地列表，避免显示上一次的内容
      this.selectorItems = [];
      try {
        // 1) store action 取数 + commit 进 state.selectorAssets[kind]
        await this.$callAction({
          action: CEP.STORE_NAME + '/loadSelectorAssets',
          param: { kind: kind },
          isBusy: false,
        });
        // 2) 从 getter 拷贝一份（getter 已完成去重+映射+_checked 初始化）
        //    _checked 在本地翻转，避免修改 store 状态
        var derived = kind === 'csharp' ? this.selectorItemsCsharp :
          kind === 'sql' ? this.selectorItemsSql :
            this.selectorItemsJs;
        this.selectorItems = derived.map(function(f) {
          return { rid: f.rid, code: f.code, name: f.name, path: f.path, _checked: false };
        });
      } catch (e) {
        this.$Message.error('加载资产失败: ' + (e.message || e));
      }
    },
    async doLinkSelected() {
      var checked = this.selectorChecked;
      if (checked.length === 0) return;
      this.selLinking = true;
      var okCount = 0;
      var lastMsg = '';
      try {
        for (var i = 0; i < checked.length; i++) {
          var f = checked[i];
          try {
            var link = await this.$callAction({
              action: CEP.STORE_NAME + '/linkAsset',
              param: { moduleCode: this.moduleCode, kind: this.selectorKind, code: f.code, apiName: f.name },
              isBusy: false,
            });
            if (link && link.apiCode) okCount++;
            lastMsg = (link && link.message) || '';
          } catch (e) {
            lastMsg = f.code + ': ' + (e.message || e);
          }
        }
        this.$Message.success('已选入 ' + okCount + ' 项' + (lastMsg ? '（' + lastMsg + '）' : ''));
        this.selectorOpen = false;
        await this.loadGroups();
      } finally {
        this.selLinking = false;
      }
    },
    // ====== 移除接口关联（外链资产，不删文件；Poptip 已确认） ======
    async doUnlink(kind, f) {
      try {
        var ret = await this.$callAction({
          action: CEP.STORE_NAME + '/unlinkAsset',
          param: { moduleCode: this.moduleCode, kind: kind, code: f.code },
          isBusy: false,
        });
        this.$Message.success((ret && ret.message) || '已移除');
        if (this.kind === kind && this.assetId === f.rid) {
          // 当前正在编辑的被移除了，清空编辑区（清 MAIN DataTable 数据）
          await this.clearEditing();
        }
        await this.loadGroups();
      } catch (e) {
        this.$Message.error('移除失败: ' + (e.message || e));
      }
    },
    // 清空当前编辑区（MAIN DataTable INIT+ADD 空行 + UI 状态重置）
    async clearEditing() {
      this.editorReady = false;
      this.dirty = false;
      await addAsset('csharp'); // INIT + ADD 一行空数据，字段值全部清空
    },
    // ====== 删除自有资产文件（先解本模块关联，再逻辑删除；Poptip 已确认） ======
    async doDelete(kind, f) {
      try {
        // 1) 移除接口关联（JS/Vue 模块不走 moudleapi 关联，跳过）
        if (kind !== 'js' && kind !== 'vue') {
          await this.$callAction({
            action: CEP.STORE_NAME + '/unlinkAsset',
            param: { moduleCode: this.moduleCode, kind: kind, code: f.code },
            isBusy: false,
          }).catch(function() {});
        }
        // 2) 逻辑删除(全类型统一): open → ISDELETED=1 → save(走 doSave, 版本记录为 delete 可回滚)
        //    生成列唯一键 uk_livepath 保证删除后同路径可重建
        var dt = await openAsset(kind, f.rid);
        dt.setValue('ISDELETED', '1');
        await this.$callAction({
          action: STORE_NS + '/save',
          param: { CHANGENOTE: '删除文件' },
          isBusy: false,
        });
        this.$Message.success('已删除（如需恢复，可到版本中心回滚）');
        if (this.kind === kind && this.assetId === f.rid) {
          await this.clearEditing();
        }
        await this.loadGroups();
      } catch (e) {
        this.$Message.error('删除失败: ' + (e.message || e));
      }
    },
    async handleCheck() {
      this.checking = true;
      try {
        var ret = await checkAsset(this.kind, this.SOURCECODE);
        this.statusMsg = ret.message;
        this.statusIsError = !ret.passed;
        if (ret.passed) this.$Message.success(ret.message);
        else this.$Message.error(ret.message);
      } finally {
        this.checking = false;
      }
    },
    // 打开当前资产的版本历史弹窗（查询/当时对比/与现在对比/回滚/标记）
    openHistory() {
      if (!this.assetId) return;
      this.$refs.verHistory.show({
        objType: 'code',
        objId: this.assetId,
        objCode: this.CODE,
      });
    },
    // 历史弹窗回滚后：重新加载当前资产
    async onVersionRollback() {
      if (this.assetId) {
        await this.loadAsset(this.kind, this.assetId);
        await this.loadGroups();
      }
    },
    // 保存(快速, 不留版本) / 提交(留版本+变更说明)
    onQuickSave() {
      this.handleSave('', true);
    },
    onCommitSave(note) {
      this.handleSave(note, false);
    },
    async handleSave(note, skipVersion) {
      if (!(this.CODE || '').trim()) {
        this.$Message.error('请输入编码');
        return;
      }
      if (!(this.NAME || '').trim()) {
        this.$Message.error('请输入名称');
        return;
      }
      this.saving = true;
      try {
        // 字段已绑 MAIN DataTable；saveAsset 内部仍走 dt.setValue（写回同样的值，幂等）
        var ret = await saveAsset(this.kind, {
          code: this.CODE,
          name: this.NAME,
          source: this.SOURCECODE,
          remark: this.REMARK,
          path: this.MODULEPATH,
          // 快速保存(不留版本)不递增资产 VERSION；提交才递增
          version: skipVersion ? (this.VERSION || 1) : (this.VERSION || 1) + 1,
          changeNote: note || '',
          skipVersion: !!skipVersion,
        });
        this.statusIsError = !ret.passed;
        this.statusMsg = ret.message;
        if (ret.passed) {
          // 后端 save 已回写 ID/VERSION 到 MAIN DataTable，mapDateTable 自动同步
          this.dirty = false;
          this.statusMsg = skipVersion ?
            '保存成功（未生成版本）' :
            (this.kind === 'csharp' ? '提交成功 (v' + this.VERSION + ')' : '提交成功');
          // 模块上下文存在时：自动建 moudleapi 关联行（RS_M18 A07，幂等）
          // JS/Vue 模块按路径归属（@/modules/{MC}/ 或 @/pages/{mc}/），不建接口关联
          if (this.moduleCode && this.kind !== 'js' && this.kind !== 'vue') {
            try {
              var link = await this.$callAction({
                action: CEP.STORE_NAME + '/linkAsset',
                param: { moduleCode: this.moduleCode, kind: this.kind, code: this.CODE, apiName: this.NAME },
                isBusy: false,
              });
              if (link && link.apiCode) {
                this.statusMsg += ' · ' + link.message;
              } else if (link && link.message) {
                this.statusMsg += ' · ' + link.message;
              }
            } catch (le) {
              this.statusMsg += ' · 关联失败: ' + (le.message || le);
            }
          }
          this.$Message.success(this.statusMsg);
          this.$emit('saved', { kind: this.kind, code: this.CODE, path: this.MODULEPATH, id: this.ID });
          // 保存后清除 SFC 模块缓存，使下次加载拿到最新代码
          if (this.MODULEPATH) {
            var dir = this.MODULEPATH.substring(0, this.MODULEPATH.lastIndexOf('/') + 1);
            invalidateCacheByPrefix(dir);
            invalidateCacheByPrefix(this.MODULEPATH);
          }
          // 保存后刷新左侧列表
          this.loadGroups();
        } else {
          this.$Message.error(ret.message);
        }
      } catch (e) {
        this.statusMsg = '保存失败: ' + (e.message || e);
        this.statusIsError = true;
        this.$Message.error(this.statusMsg);
      } finally {
        this.saving = false;
      }
    },
  },
};
</script>

<style lang="less" scoped>
.ca-popup-body {
  display: flex;
  height: 72vh;
  gap: 8px;
}
/* 左侧文件列表 */
.ca-popup-filelist {
  width: 220px;
  flex-shrink: 0;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  overflow-y: auto;
  background: #fafafa;
}
.ca-group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 10px;
  background: #f0f1f3;
  border-bottom: 1px solid #e8eaec;
  position: sticky;
  top: 0;
  .ca-add {
    cursor: pointer;
    color: #888;
    &:hover { color: #2d8cf0; }
  }
}
.ca-kind {
  font-size: 12px;
  font-weight: bold;
  padding: 1px 7px;
  border-radius: 3px;
  &.csharp { color: #9b59b6; border: 1px solid #9b59b6; }
  &.sql { color: #16a085; border: 1px solid #16a085; }
  &.js { color: #e67e22; border: 1px solid #e67e22; }
  &.vue { color: #42b983; border: 1px solid #42b983; }
}
.ca-file-item {
  display: flex;
  flex-direction: column;
  padding: 5px 10px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  &:hover { background: #eef4fb; }
  &.active { background: #d6e6f9; }
  .ca-file-code {
    font-size: 13px;
    color: #17233d;
    font-weight: 600;
    word-break: break-all;
  }
  .ca-file-api {
    margin-left: 4px;
    font-size: 10px;
    font-weight: normal;
    color: #2d8cf0;
    border: 1px solid #2d8cf0;
    border-radius: 3px;
    padding: 0 3px;
  }
  .ca-file-name {
    font-size: 11px;
    color: #9ea7b4;
    word-break: break-all;
  }
}
.ca-file-empty {
  padding: 8px 10px;
  color: #c0c4cc;
  font-size: 12px;
}
.ca-group-actions {
  display: flex;
  gap: 8px;
  .ca-act {
    cursor: pointer;
    color: #888;
    font-size: 13px;
    &:hover { color: #2d8cf0; }
  }
}
.ca-file-item {
  position: relative;
}
.ca-file-ext {
  margin-left: 4px;
  font-size: 10px;
  font-weight: normal;
  color: #9ea7b4;
  border: 1px solid #dcdee2;
  border-radius: 3px;
  padding: 0 3px;
}
.ca-file-ops {
  position: absolute;
  right: 6px;
  top: 6px;
  display: none;
  .ca-op {
    cursor: pointer;
    color: #888;
    font-size: 12px;
    &:hover { color: #2d8cf0; }
    &.danger:hover { color: #ed4014; }
  }
}
.ca-file-item:hover .ca-file-ops {
  display: block;
}
/* 选入面板 */
.ca-sel-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e8eaec;
}
.ca-sel-title {
  color: #515a6e;
  font-size: 12px;
}
.ca-sel-search {
  width: 200px;
}
.ca-sel-list {
  flex: 1;
  overflow-y: auto;
  padding: 6px 0;
}
.ca-sel-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  cursor: pointer;
  border-bottom: 1px solid #f5f5f5;
  &:hover { background: #eef4fb; }
  &.active { background: #d6e6f9; }
  i { color: #2d8cf0; font-size: 12px; }
  .ca-sel-code {
    font-weight: 600;
    color: #17233d;
  }
  .ca-sel-name {
    color: #9ea7b4;
    font-size: 12px;
  }
}
.ca-sel-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-top: 8px;
  border-top: 1px solid #e8eaec;
}
.ca-sel-count {
  color: #9ea7b4;
  font-size: 12px;
}
/* 右侧主区域 */
.ca-popup-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.ca-popup-toolbar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-bottom: 8px;
  border-bottom: 1px solid #e8eaec;
}
.ca-input {
  background: #f5f5f5;
  border: 1px solid #dcdee2;
  border-radius: 3px;
  padding: 4px 8px;
  font-size: 13px;
  outline: none;
  &:focus { border-color: #2d8cf0; }
}
.ca-code { width: 120px; }
.ca-path { width: 300px; font-family: Consolas, Monaco, monospace; font-size: 12px; }
.ca-name { width: 110px; }
.ca-dir { width: 110px; color: #16a085; font-weight: 600; }
.ca-remark { width: 300px; }
.ca-flex { flex: 1; }
.ca-popup-editor {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding-top: 6px;
}
/* 右侧接口测试面板 */
.ca-test-side {
  width: 400px;
  flex-shrink: 0;
  border: 1px solid #e8eaec;
  border-radius: 4px;
  overflow: auto;
  display: flex;
  flex-direction: column;
}
.ca-popup-section-title {
  display: flex;
  gap: 8px;
  align-items: center;
  color: #515a6e;
  font-size: 12px;
  padding-bottom: 4px;
}
.ca-badge {
  color: #fa8c16;
  font-size: 11px;
}
.ca-editor-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #c0c4cc;
  font-size: 13px;
  border: 1px dashed #dcdee2;
  border-radius: 4px;
}
.ca-popup-status {
  padding-top: 6px;
  font-size: 12px;
  color: #52c41a;
  .ca-status-error { color: #ed4014; }
}
</style>
