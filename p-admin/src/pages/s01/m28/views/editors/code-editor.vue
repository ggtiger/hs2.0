<template>
  <div class="st-ed-code" v-if="moduleCode">
    <div class="st-ed-code-body">
      <!-- 左侧: 文件列表（复用 code-editor-popup 逻辑） -->
      <div class="ca-popup-filelist">
        <div class="ca-group" v-for="g in groups" :key="g.kind">
          <div class="ca-group-header">
            <span class="ca-kind" :class="g.kind">{{ g.label }} ({{ g.items.length }})</span>
            <span class="ca-group-actions">
              <i v-if="g.kind !== 'js' && g.kind !== 'vue'" class="h-icon-link ca-act" title="选入已有资产" @click="openSelector(g.kind)"></i>
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
                  :content="'删除文件 ' + f.code + '？'"
                  transfer
                  @confirm="doDelete(g.kind, f)"
                >
                  <i class="h-icon-trash ca-op danger" title="删除文件"></i>
                </Poptip>
                <Poptip
                  v-else
                  :content="'移除 ' + f.code + ' 与当前模块的接口关联？'"
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

      <!-- 右侧: 工具栏 + 编辑器 + 状态栏 -->
      <div class="ca-popup-main" v-if="!selectorOpen">
        <div class="ca-popup-header">
          <span class="ca-popup-header-title">{{ kindLabel }}</span>
          <span class="ca-flex"></span>
          <Button size="s" @click="openAi">AI 助手</Button>
          <Button size="s" :disabled="!assetId" @click="openHistory">版本</Button>
          <Button size="s" v-if="kind !== 'js' && kind !== 'vue'" :color="testOpen ? 'primary' : null" @click="testOpen = !testOpen">测试</Button>
          <Button size="s" :loading="checking" @click="handleCheck">编译</Button>
          <save-actions :loading="saving" @save="onQuickSave" @commit="onCommitSave" />
        </div>
        <div class="ca-popup-toolbar">
          <input v-model="CODE" placeholder="编码" class="ca-input ca-code" />
          <input v-model="NAME" placeholder="名称" class="ca-input ca-name" />
          <input v-model="MODULEPATH" :placeholder="pathPlaceholder" class="ca-input ca-path" @change="onPathChange" />
          <input v-model="REMARK" placeholder="备注(可选)" class="ca-input ca-remark" />
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

      <!-- 选入面板 -->
      <div class="ca-popup-main" v-else>
        <div class="ca-sel-head">
          <span class="ca-kind" :class="selectorKind">{{ ASSET_META[selectorKind].kindBadge }}</span>
          <span class="ca-sel-title">选入到 {{ moduleCode }}（可多选）</span>
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
            {{ selSearch ? '无匹配资产' : '没有可选入的资产' }}
          </div>
        </div>
        <div class="ca-sel-foot">
          <span class="ca-sel-count">已选 {{ selectorChecked.length }} 项</span>
          <span class="ca-flex"></span>
          <Button size="s" @click="selectorOpen = false">取消</Button>
          <Button size="s" color="primary" :loading="selLinking" :disabled="selectorChecked.length === 0" @click="doLinkSelected">选入关联</Button>
        </div>
      </div>

      <!-- 接口测试面板 -->
      <div v-if="testOpen && kind !== 'js' && kind !== 'vue'" class="ca-test-side">
        <code-test-panel :kind="kind" :source="SOURCECODE" :code="CODE" />
      </div>
    </div>

    <!-- 版本历史弹窗 -->
    <version-history-popup ref="verHistory" @rollback="onVersionRollback" />
  </div>
</template>

<script>
import sfcCodeEditor from '@/pages/s01/m17/components/sfc-code-editor.vue';
import saveActions from '@/components/generic-module/save-actions.vue';
import versionHistoryPopup from '@/components/generic-module/version-history-popup.vue';
import codeTestPanel from '@/components/generic-module/code-test-panel.vue';
import { Constants as CEP, mapGetters as cepMapGetters } from '@/components/generic-module/code-editor-store';
import { mapDateTable as m17MapDateTable, getStoreResult as m17GetStoreResult } from '@/pages/s01/m17/store';
import {
  ASSET_META, STORE_NS, openAsset, addAsset, checkAsset, saveAsset,
  loadModuleCodes, deriveAssetDir, deriveScriptPath, deriveTplCode,
  defaultCsharpTemplate, defaultSqlTemplate, defaultJsTemplate, defaultVueTemplate,
} from '@/pages/s01/m17/code-asset';
import { invalidateCacheByPrefix } from '@/sfc-loader';

export default {
  name: 'CodeEditor',
  components: { sfcCodeEditor, saveActions, versionHistoryPopup, codeTestPanel },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      kind: 'csharp',
      dirty: false,
      editorReady: false,
      saving: false,
      checking: false,
      statusMsg: '',
      statusIsError: false,
      moduleCodes: [],
      assetDir: '',
      selectorOpen: false,
      selectorKind: 'csharp',
      selSearch: '',
      selectorItems: [],
      selLinking: false,
      testOpen: false,
    };
  },
  computed: {
    ...cepMapGetters([
      'groupCsharp', 'groupSql', 'groupJs', 'groupVue',
      'selectorItemsCsharp', 'selectorItemsSql', 'selectorItemsJs', 'selectorItemsVue',
    ]),
    ...m17MapDateTable('MAIN', [
      'ID', 'CODE', 'NAME', 'SOURCECODE', 'REMARK',
      'MODULEPATH', 'VERSION', 'ASSETTYPE',
    ]),
    assetId() { return this.ID },
    code() { return this.CODE },
    name() { return this.NAME },
    remark() { return this.REMARK },
    source() { return this.SOURCECODE },
    path() { return this.MODULEPATH },
    groups() {
      return [
        { kind: 'csharp', label: 'API 脚本', items: this.groupCsharp },
        { kind: 'sql', label: 'SQL 模板', items: this.groupSql },
        { kind: 'js', label: 'JS 模块', items: this.groupJs },
        { kind: 'vue', label: 'Vue 组件', items: this.groupVue },
      ];
    },
    ASSET_META() { return ASSET_META },
    meta() { return ASSET_META[this.kind] },
    selectorChecked() {
      return this.selectorItems.filter(function(f) { return f._checked });
    },
    pathPlaceholder() {
      if (this.kind === 'js') return '路径(@/modules/模块/xxx.js)';
      if (this.kind === 'vue') return '路径(@/pages/模块/xxx.vue)';
      if (this.kind === 'csharp') return '路径(@/scripts/模块/编码.cs)';
      return '路径(@/scripts/模块/编码.sql)';
    },
    kindLabel() {
      var g = this.groups.find(function(g) { return g.kind === this.kind }.bind(this));
      return g ? g.label : '代码编辑';
    },
    selectorFilteredItems() {
      var kw = (this.selSearch || '').toLowerCase();
      if (!kw) return this.selectorItems;
      return this.selectorItems.filter(function(f) {
        return (f.code || '').toLowerCase().indexOf(kw) >= 0 || (f.name || '').toLowerCase().indexOf(kw) >= 0;
      });
    },
  },
  watch: {
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
    'item.ID'(v) {
      // 从 section-list 选中代码项时，自动定位
      if (v) this.locateItem(v);
    },
  },
  created() {
    m17GetStoreResult();
  },
  mounted() {
    this.init();
  },
  methods: {
    async init() {
      this.moduleCodes = await loadModuleCodes();
      await this.loadGroups();
      // 注册 sfcContext
      this.$store.commit('sfcContext/SET', {
        editorRef: this,
        moduleCode: this.moduleCode,
        editTarget: this.kind,
        siblingFiles: [],
        active: true,
      });
      // 选中 section-list 传入的 item
      if (this.item && this.item.ID) {
        this.locateItem(this.item.ID);
      }
    },
    async locateItem(id) {
      // 在列表中找到对应的资产并打开
      var allGroups = this.groups;
      for (var gi = 0; gi < allGroups.length; gi++) {
        var items = allGroups[gi].items;
        for (var fi = 0; fi < items.length; fi++) {
          if (items[fi].rid === id) {
            await this.loadAsset(allGroups[gi].kind, id);
            return;
          }
        }
      }
    },
    async loadGroups() {
      try {
        if (this.moduleCode) {
          await this.$callAction({
            action: CEP.STORE_NAME + '/loadModuleAssets',
            param: { moduleCode: this.moduleCode },
            isBusy: false,
          });
          return;
        }
        await this.$callAction({
          action: CEP.STORE_NAME + '/loadAllAssets',
          param: {},
          isBusy: false,
        });
      } catch (e) {
        // eslint-disable-next-line no-console
        console.error('[CodeEditor] 加载文件列表失败:', e);
      }
    },
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
        this.kind = kind;
        if (kind !== 'js' && kind !== 'vue' && !dt.getValue('MODULEPATH')) {
          dt.setValue('MODULEPATH', await deriveScriptPath(kind, dt.getValue('CODE'), this.moduleCodes));
        }
        if (kind !== 'csharp') dt.setValue('VERSION', 1);
        this.assetDir = deriveAssetDir(dt.getValue('CODE'), kind, this.moduleCodes);
        this.dirty = false;
        this.editorReady = true;
        this.statusMsg = '';
        this.$nextTick(function() {
          if (this.$refs.editor) this.$refs.editor.setValue(this.SOURCECODE);
        }.bind(this));
      } catch (e) {
        this.statusMsg = '加载失败: ' + (e.message || e);
        this.statusIsError = true;
      }
    },
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
      this.VERSION = 1;
      this.REMARK = '';
      if (kind === 'js') {
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
      this.$nextTick(function() {
        if (this.$refs.editor) this.$refs.editor.setValue(this.source);
      }.bind(this));
    },
    onCodeChange() { this.dirty = true },
    onPathChange() {
      if (this.kind === 'js' && this.MODULEPATH) {
        this.CODE = deriveTplCode(this.MODULEPATH);
      }
      if (this.kind === 'vue' && this.MODULEPATH) {
        this.CODE = deriveTplCode(this.MODULEPATH);
      }
      this.dirty = true;
    },
    fileLabel(kind, f) {
      if (kind === 'csharp') return (f.code || '') + '.cs';
      if (kind === 'sql') return (f.code || '') + '.sql';
      var p = f.path || '';
      if (p) return p.substring(p.lastIndexOf('/') + 1);
      return f.code || '';
    },
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
    // 选入
    async openSelector(kind) {
      if (!this.moduleCode) {
        this.$Message.warning('选入需要在模块上下文打开');
        return;
      }
      this.selectorKind = kind;
      this.selSearch = '';
      this.selectorOpen = true;
      this.selectorItems = [];
      try {
        await this.$callAction({
          action: CEP.STORE_NAME + '/loadSelectorAssets',
          param: { kind: kind },
          isBusy: false,
        });
        var derived = kind === 'csharp' ? this.selectorItemsCsharp :
          kind === 'sql' ? this.selectorItemsSql : this.selectorItemsJs;
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
      try {
        for (var i = 0; i < checked.length; i++) {
          try {
            var link = await this.$callAction({
              action: CEP.STORE_NAME + '/linkAsset',
              param: { moduleCode: this.moduleCode, kind: this.selectorKind, code: checked[i].code, apiName: checked[i].name },
              isBusy: false,
            });
            if (link && link.apiCode) okCount++;
          } catch (e) { /* skip */ }
        }
        this.$Message.success('已选入 ' + okCount + ' 项');
        this.selectorOpen = false;
        await this.loadGroups();
      } finally {
        this.selLinking = false;
      }
    },
    // 移除关联
    async doUnlink(kind, f) {
      try {
        await this.$callAction({
          action: CEP.STORE_NAME + '/unlinkAsset',
          param: { moduleCode: this.moduleCode, kind: kind, code: f.code },
          isBusy: false,
        });
        this.$Message.success('已移除');
        if (this.kind === kind && this.assetId === f.rid) {
          await this.clearEditing();
        }
        await this.loadGroups();
      } catch (e) {
        this.$Message.error('移除失败: ' + (e.message || e));
      }
    },
    async clearEditing() {
      this.editorReady = false;
      this.dirty = false;
      await addAsset('csharp');
    },
    // 删除
    async doDelete(kind, f) {
      try {
        if (kind !== 'js' && kind !== 'vue') {
          await this.$callAction({
            action: CEP.STORE_NAME + '/unlinkAsset',
            param: { moduleCode: this.moduleCode, kind: kind, code: f.code },
            isBusy: false,
          }).catch(function() {});
        }
        var dt = await openAsset(kind, f.rid);
        dt.setValue('ISDELETED', '1');
        await this.$callAction({
          action: STORE_NS + '/save',
          param: { CHANGENOTE: '删除文件' },
          isBusy: false,
        });
        this.$Message.success('已删除');
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
    openAi() {
      this.$emit('ask-ai', {
        key: 'code_' + this.kind + '_' + (this.CODE || ''),
        label: (this.kindLabel || '代码') + ': ' + (this.CODE || ''),
        icon: 'h-icon-code'
      });
    },
    openHistory() {
      if (!this.assetId) return;
      this.$refs.verHistory.show({
        objType: 'code',
        objId: this.assetId,
        objCode: this.CODE,
      });
    },
    async onVersionRollback() {
      if (this.assetId) {
        await this.loadAsset(this.kind, this.assetId);
        await this.loadGroups();
      }
    },
    onQuickSave() { this.handleSave('', true) },
    onCommitSave(note) { this.handleSave(note, false) },
    async handleSave(note, skipVersion) {
      if (!(this.CODE || '').trim()) { this.$Message.error('请输入编码'); return }
      if (!(this.NAME || '').trim()) { this.$Message.error('请输入名称'); return }
      this.saving = true;
      try {
        var ret = await saveAsset(this.kind, {
          code: this.CODE,
          name: this.NAME,
          source: this.SOURCECODE,
          remark: this.REMARK,
          path: this.MODULEPATH,
          version: skipVersion ? (this.VERSION || 1) : (this.VERSION || 1) + 1,
          changeNote: note || '',
          skipVersion: !!skipVersion,
        });
        this.statusIsError = !ret.passed;
        this.statusMsg = ret.message;
        if (ret.passed) {
          this.dirty = false;
          this.statusMsg = skipVersion ? '保存成功' : '提交成功';
          if (this.moduleCode && this.kind !== 'js' && this.kind !== 'vue') {
            try {
              var link = await this.$callAction({
                action: CEP.STORE_NAME + '/linkAsset',
                param: { moduleCode: this.moduleCode, kind: this.kind, code: this.CODE, apiName: this.NAME },
                isBusy: false,
              });
              if (link && link.message) this.statusMsg += ' · ' + link.message;
            } catch (le) { /* skip */ }
          }
          this.$Message.success(this.statusMsg);
          this.$emit('saved', { section: 'code', id: this.ID });
          if (this.MODULEPATH) {
            var dir = this.MODULEPATH.substring(0, this.MODULEPATH.lastIndexOf('/') + 1);
            invalidateCacheByPrefix(dir);
            invalidateCacheByPrefix(this.MODULEPATH);
          }
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
  beforeDestroy() {
    if (this.$store.state.sfcContext.editorRef === this) {
      this.$store.commit('sfcContext/CLEAR');
    }
  },
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-code {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  background: @st-bg-white;
  overflow: hidden;
}

.st-ed-code-body {
  flex: 1;
  display: flex;
  min-height: 0;
  overflow: hidden;
  gap: 0;
}

/* 左侧文件列表 — 复用 code-editor-popup 样式 */
.ca-popup-filelist {
  width: 220px;
  flex-shrink: 0;
  border-right: 1px solid #e8eaec;
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
  display: flex;
  flex-direction: column;
  padding: 5px 10px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  position: relative;
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
  &:hover .ca-file-ops { display: block; }
}
.ca-file-empty {
  padding: 8px 10px;
  color: #c0c4cc;
  font-size: 12px;
}

/* 右侧主区域 */
.ca-popup-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.ca-popup-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border-bottom: 1px solid #e8eaec;
  flex-shrink: 0;
  background: #fff;
}
.ca-popup-header-title {
  font-size: 13px;
  font-weight: 600;
  color: #333;
}
.ca-popup-toolbar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border-bottom: 1px solid #e8eaec;
  flex-shrink: 0;
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
.ca-remark { width: 300px; }
.ca-flex { flex: 1; }
.ca-popup-editor {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 6px 10px 0;
}
.ca-popup-section-title {
  display: flex;
  gap: 8px;
  align-items: center;
  color: #515a6e;
  font-size: 12px;
  padding-bottom: 4px;
}
.ca-badge { color: #fa8c16; font-size: 11px; }
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
  padding: 4px 10px;
  font-size: 12px;
  color: #52c41a;
  flex-shrink: 0;
  .ca-status-error { color: #ed4014; }
}

/* 选入面板 */
.ca-sel-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-bottom: 1px solid #e8eaec;
}
.ca-sel-title { color: #515a6e; font-size: 12px; }
.ca-sel-search { width: 200px; }
.ca-sel-list { flex: 1; overflow-y: auto; padding: 6px 0; }
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
  .ca-sel-code { font-weight: 600; color: #17233d; }
  .ca-sel-name { color: #9ea7b4; font-size: 12px; }
}
.ca-sel-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-top: 1px solid #e8eaec;
}
.ca-sel-count { color: #9ea7b4; font-size: 12px; }

/* 接口测试面板 */
.ca-test-side {
  width: 400px;
  flex-shrink: 0;
  border-left: 1px solid #e8eaec;
  overflow: auto;
  display: flex;
  flex-direction: column;
}
</style>
