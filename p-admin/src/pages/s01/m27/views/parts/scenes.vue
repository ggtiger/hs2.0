<template>
  <div class="scenes-part">
    <!-- 左侧: 场景列表 -->
    <div class="sp-left">
      <div class="sp-filter">
        <input class="sp-search" v-model="keyword" placeholder="搜索场景编码/名称" />
        <Button size="s" color="primary" @click="addNew" style="width:100%">+ 新增场景</Button>
      </div>
      <div class="sp-list" v-loading="loading">
        <div
          v-for="it in filteredList"
          :key="it.ID"
          :class="['sp-item', { active: currentId === it.ID }]"
          @click="selectScene(it)"
        >
          <div class="sp-item-head">
            <span class="sp-item-name">{{ it.SCENENAME || it.SCENECODE }}</span>
            <span :class="['sp-item-enabled', it.ENABLED === 1 ? 'on' : 'off']">{{ it.ENABLED === 1 ? '启用' : '停用' }}</span>
          </div>
          <div class="sp-item-code">{{ it.SCENECODE }}</div>
          <div class="sp-item-meta">
            <span class="sp-tag">{{ it.TRANSPORT }}</span>
            <span class="sp-tag" v-if="it.TOOLSET">{{ it.TOOLSET }}</span>
            <span class="sp-tag ft" v-if="it.FRONTENDTOOLS === 'all'">前端:全部</span>
            <span class="sp-tag ft" v-else-if="it.FRONTENDTOOLS && it.FRONTENDTOOLS !== 'none'">前端:{{ it.FRONTENDTOOLS.split(',').length }}个</span>
          </div>
        </div>
        <div v-if="!loading && filteredList.length === 0" class="sp-empty">暂无场景</div>
      </div>
    </div>

    <!-- 右侧: 场景详情面板 -->
    <div class="sp-right" v-if="currentId">
      <div class="sp-detail" v-loading="detailLoading">
        <!-- 场景头部 -->
        <div class="sp-detail-head">
          <div class="sp-detail-title">
            <span>{{ form.SCENENAME || form.SCENECODE || '新场景' }}</span>
            <span :class="['sp-detail-enabled', form.ENABLED === 1 ? 'on' : 'off']">{{ form.ENABLED === 1 ? '启用' : '停用' }}</span>
          </div>
          <div class="sp-detail-actions">
            <Button size="s" color="primary" @click="save" :loading="saving">保存</Button>
            <Poptip content="确定删除该场景？" @confirm="del" v-if="!isNew">
              <Button size="s" color="red">删除</Button>
            </Poptip>
          </div>
        </div>

        <!-- 可滚动内容区 -->
        <div class="sp-detail-body">
          <!-- 基础配置 -->
          <div class="sp-section">
            <div class="sp-section-header" @click="toggleDetailSection('basic')">
              <i :class="detailSections.basic ? 'h-icon-down' : 'h-icon-right'"></i>
              <span class="sp-section-title">基础配置</span>
            </div>
            <div class="sp-section-body" v-if="detailSections.basic">
              <div class="sp-field">
                <label>场景编码</label>
                <input v-model="form.SCENECODE" :disabled="!isNew" placeholder="如 assistant / aidev / wizard" />
              </div>
              <div class="sp-field">
                <label>场景名称</label>
                <input v-model="form.SCENENAME" placeholder="如 通用助理 / AI 开发 / 模块向导" />
              </div>
              <div class="sp-field">
                <label>传输方式</label>
                <Select v-model="form.TRANSPORT" :datas="transportOptions" />
              </div>
              <div class="sp-field">
                <label>Endpoint</label>
                <input v-model="form.ENDPOINT" placeholder="SSE/WS 端点, 可空" />
              </div>
              <div class="sp-field">
                <label>工具集</label>
                <Select v-model="form.TOOLSET" :datas="toolsetOptions" />
              </div>
              <div class="sp-field">
                <label>上下文源</label>
                <input v-model="form.CONTEXTSOURCE" placeholder="current_page / none" />
              </div>
              <div class="sp-field">
                <label>启用</label>
                <h-switch :value="form.ENABLED===1" @input="form.ENABLED=$event?1:0" />
              </div>
              <div class="sp-field">
                <label>排序</label>
                <NumberInput v-model="form.SORTNO" />
              </div>
              <div class="sp-field full">
                <label>说明</label>
                <textarea v-model="form.REMARK" rows="2"></textarea>
              </div>
            </div>
          </div>

          <!-- 模型配置 -->
          <div class="sp-section">
            <div class="sp-section-header" @click="toggleDetailSection('model')">
              <i :class="detailSections.model ? 'h-icon-down' : 'h-icon-right'"></i>
              <span class="sp-section-title">模型配置</span>
              <span class="sp-section-badge" v-if="form.MODELID">指定模型</span>
              <span class="sp-section-badge quota" v-if="form.DAILYQUOTA > 0">配额:{{ formatQuota(form.DAILYQUOTA) }}</span>
            </div>
            <div class="sp-section-body" v-if="detailSections.model">
              <div class="sp-field">
                <label>指定模型</label>
                <Select v-model="form.MODELID" :datas="modelOptions" placeholder="留空=全局默认模型" />
              </div>
              <div class="sp-field">
                <label>降级模型</label>
                <Select v-model="form.FALLBACKID" :datas="modelOptions" placeholder="留空=无降级" />
              </div>
              <div class="sp-field full">
                <label>Agent 参数</label>
                <textarea v-model="form.PARAMS" rows="3" placeholder='{"maxSteps":15, "enableHeartbeat":true}'></textarea>
                <span class="sp-hint">JSON: maxSteps, maxToolResultChars, summaryTruncateChars, enableHeartbeat, heartbeatIntervalMs</span>
              </div>
              <div class="sp-field">
                <label>每日配额</label>
                <NumberInput v-model="form.DAILYQUOTA" :min="0" />
                <span class="sp-hint">0=不限, 单位tokens</span>
              </div>
            </div>
          </div>

          <!-- 提示词区块 -->
          <ScenePromptSection
            :sceneCode="form.SCENECODE"
            :promptList="promptList"
            @refresh-prompts="loadPrompts"
          />

          <!-- 后端工具区块 -->
          <SceneToolSection
            :toolset="form.TOOLSET"
            :toolList="toolList"
            :sqlOptions="sqlOptions"
            :csharpOptions="csharpOptions"
            @refresh-tools="loadTools"
          />

          <!-- 前端工具区块 -->
          <SceneFrontendToolSection
            v-model="form.FRONTENDTOOLS"
          />

          <!-- 测试对话 -->
          <SceneTestChat
            :sceneCode="form.SCENECODE"
            :form="form"
          />
        </div>
      </div>
    </div>

    <!-- 右侧占位 -->
    <div class="sp-right sp-placeholder" v-else>
      <div class="sp-ph-inner">
        <p>← 从左侧选择场景进行配置</p>
        <p class="sp-ph-tip">场景为中心：提示词、工具、模型配置一体化管理</p>
      </div>
    </div>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import ScenePromptSection from './scene-prompt-section.vue';
import SceneToolSection from './scene-tool-section.vue';
import SceneFrontendToolSection from './scene-frontend-tool-section.vue';
import SceneTestChat from './scene-test-chat.vue';

const MC = 'RS_M23';
const M14 = 'RS_M14';
const M16 = 'RS_M16';
const M24 = 'RS_M24';

export default {
  name: 'ScenesPart',
  components: { ScenePromptSection, SceneToolSection, SceneFrontendToolSection, SceneTestChat },
  data() {
    return {
      loading: false,
      detailLoading: false,
      saving: false,
      keyword: '',
      list: [],
      currentId: '',
      isNew: false,
      form: this.emptyForm(),
      promptList: [],
      toolList: [],
      sqlOptions: [],
      csharpOptions: [],
      detailSections: {
        basic: true,
        model: false
      }
    };
  },
  computed: {
    filteredList() {
      var kw = (this.keyword || '').trim().toLowerCase();
      if (!kw) return this.list;
      return this.list.filter(function(it) {
        return (it.SCENECODE || '').toLowerCase().indexOf(kw) >= 0 ||
          (it.SCENENAME || '').toLowerCase().indexOf(kw) >= 0;
      });
    },
    transportOptions() {
      return [
        { key: 'signalr', title: 'signalr (SignalR 双向)' },
        { key: 'sse', title: 'sse (Server-Sent Events)' },
        { key: 'ws', title: 'ws (WebSocket)' },
        { key: 'http', title: 'http (一次性请求)' }
      ];
    },
    toolsetOptions() {
      return [
        { key: '', title: '(无)' },
        { key: 'assistant', title: 'assistant 通用助理' },
        { key: 'formfill', title: 'formfill 表单填报' },
        { key: 'readonly', title: 'readonly 只读查询' },
        { key: 'dev', title: 'dev 开发工具集' },
        { key: 'sfc', title: 'sfc SFC代码助手' },
        { key: 'wizard', title: 'wizard 向导工具集' }
      ];
    },
    modelOptions() {
      var st = this.$store.state[M14];
      var models = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
      var opts = [{ key: '', title: '(全局默认)' }];
      models.forEach(function(m) {
        if (m.ISDELETED === 1) return;
        var label = (m.PROVIDER || '') + '/' + (m.MODELNAME || '') + (m.ISVISION === 1 ? ' [视觉]' : '') + (m.ENABLED === 1 ? ' ✓' : '');
        opts.push({ key: m.ID, title: label });
      });
      return opts;
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
    this.m14Store = getGenericStore(M14);
    this.m16Store = getGenericStore(M16);
    this.m24Store = getGenericStore(M24);
    this.m17Store = getGenericStore('RS_M17');
  },
  mounted() {
    this.loadList();
    this.loadModels();
    this.loadCodeOptions();
  },
  methods: {
    emptyForm() {
      return {
        ID: '',
        SCENECODE: '',
        SCENENAME: '',
        TRANSPORT: 'sse',
        ENDPOINT: '',
        TOOLSET: '',
        PROMPTKEY: '',
        MODELID: '',
        FALLBACKID: '',
        PARAMS: '',
        DAILYQUOTA: 0,
        FRONTENDTOOLS: '',
        CONTEXTSOURCE: '',
        ENABLED: 1,
        SORTNO: 0,
        REMARK: ''
      };
    },
    toggleDetailSection(key) {
      this.$set(this.detailSections, key, !this.detailSections[key]);
    },
    formatQuota(n) {
      if (!n || n <= 0) return '';
      if (n >= 1000000) return (n / 1000000).toFixed(1) + 'M';
      if (n >= 1000) return (n / 1000).toFixed(0) + 'K';
      return n + '';
    },
    async loadCodeOptions() {
      // 代码资产列表（RS_M17/VSS_CODE_ASSET），按 ASSETTYPE 分组
      try {
        var QQRY = this.m17Store.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 500); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: 'RS_M17/query' });
        var st = this.$store.state['RS_M17'];
        var rows = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        this.sqlOptions = rows.filter(function(it) { return it.ASSETTYPE === 'sql'; }).map(function(it) {
          return { key: it.CODE, title: it.CODE + (it.NAME ? ' - ' + it.NAME : '') };
        });
        this.csharpOptions = rows.filter(function(it) { return it.ASSETTYPE === 'csharp'; }).map(function(it) {
          return { key: it.CODE, title: it.CODE + (it.NAME ? ' - ' + it.NAME : '') };
        });
      } catch (e) { /* 静默 */ }
    },
    async loadList() {
      this.loading = true;
      try {
        var QQRY = this.storeObj.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 100); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: MC + '/query' });
        var st = this.$store.state[MC];
        this.list = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
        this.$emit('count', { key: 'scenes', n: this.list.length });
      } finally {
        this.loading = false;
      }
    },
    async loadModels() {
      try {
        var QQRY = this.m14Store.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 100); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: M14 + '/query' });
      } catch (e) { /* 静默 */ }
    },
    async loadPrompts() {
      try {
        var QQRY = this.m16Store.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 200); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: M16 + '/query' });
        var st = this.$store.state[M16];
        this.promptList = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
      } catch (e) { /* 静默 */ }
    },
    async loadTools() {
      try {
        var QQRY = this.m24Store.storeHelper.getTable('QQRY');
        if (QQRY) { QQRY.setValue('PageSize', 200); QQRY.setValue('PageIndex', 1) }
        await this.$callAction({ action: M24 + '/query' });
        var st = this.$store.state[M24];
        this.toolList = (st && st.dt && st.dt.QRY && st.dt.QRY.data) || [];
      } catch (e) { /* 静默 */ }
    },
    async selectScene(it) {
      this.currentId = it.ID;
      this.isNew = false;
      this.form = Object.assign(this.emptyForm(), it);
      this.detailLoading = true;
      try {
        // 并行加载场景详情 + 提示词 + 工具
        await Promise.all([
          this.$callAction({ action: MC + '/open', param: { ID: it.ID } }),
          this.loadPrompts(),
          this.loadTools()
        ]);
      } finally {
        this.detailLoading = false;
      }
    },
    addNew() {
      this.currentId = '__new__';
      this.isNew = true;
      this.form = this.emptyForm();
      this.promptList = [];
      this.toolList = [];
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: {} });
    },
    async save() {
      if (!this.form.SCENECODE) { this.$Message('场景编码不能为空'); return }
      this.saving = true;
      try {
        var MAIN = this.storeObj.storeHelper.getTable('MAIN');
        var keys = ['SCENECODE', 'SCENENAME', 'TRANSPORT', 'ENDPOINT', 'TOOLSET', 'PROMPTKEY', 'MODELID', 'FALLBACKID', 'PARAMS', 'DAILYQUOTA', 'FRONTENDTOOLS', 'CONTEXTSOURCE', 'ENABLED', 'SORTNO', 'REMARK'];
        var self = this;
        keys.forEach(function(k) { MAIN.setValue(k, self.form[k]); });
        await this.$callAction({
          action: MC + '/save',
          successText: '保存成功',
          successCall: function() {
            self.isNew = false;
            self.loadList();
          }
        });
      } finally {
        this.saving = false;
      }
    },
    async del() {
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: { ID: this.currentId } });
      await this.$callAction({
        action: MC + '/delete',
        successText: '删除成功',
        successCall: () => {
          this.currentId = '';
          this.form = this.emptyForm();
          this.loadList();
        }
      });
    }
  }
};
</script>

<style lang="less" scoped>
.scenes-part {
  flex: 1;
  display: flex;
  min-height: 0;
}
.sp-left {
  width: 220px;
  border-right: 1px solid #e8e8e8;
  background: #fff;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}
.sp-filter {
  padding: 8px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.sp-search {
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 5px 8px;
  font-size: 12px;
  outline: none;
  &:focus { border-color: #2F54EB; }
}
.sp-list {
  flex: 1;
  overflow-y: auto;
  padding: 4px;
}
.sp-item {
  padding: 8px 10px;
  border-radius: 4px;
  cursor: pointer;
  margin-bottom: 2px;
  border: 1px solid transparent;
  &:hover { background: #f5f7fa; }
  &.active { background: #e6f7ff; border-color: #91d5ff; }
}
.sp-item-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.sp-item-name { font-size: 13px; font-weight: 600; }
.sp-item-enabled {
  font-size: 10px; padding: 0 5px; border-radius: 3px;
  &.on { background: #f6ffed; color: #52c41a; }
  &.off { background: #f5f5f5; color: #999; }
}
.sp-item-code {
  font-size: 11px; color: #2F54EB; font-family: Consolas, monospace; margin: 2px 0;
}
.sp-item-meta { display: flex; gap: 4px; flex-wrap: wrap; }
.sp-tag {
  font-size: 10px; background: #f0f5ff; color: #2F54EB;
  padding: 0 5px; border-radius: 2px;
  &.ft { background: #f6ffed; color: #52c41a; }
}
.sp-empty { text-align: center; color: #bbb; padding: 30px 0; }

/* 右侧详情面板 */
.sp-right {
  flex: 1;
  min-width: 0;
  background: #f5f6f8;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.sp-detail {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}
.sp-detail-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.sp-detail-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
}
.sp-detail-enabled {
  font-size: 11px; padding: 0 6px; border-radius: 3px;
  &.on { background: #f6ffed; color: #52c41a; }
  &.off { background: #f5f5f5; color: #999; }
}
.sp-detail-actions { display: flex; gap: 6px; }
.sp-detail-body {
  flex: 1;
  overflow-y: auto;
  padding: 12px 16px;
}

/* 折叠区块 */
.sp-section {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
  margin-bottom: 5px;
}
.sp-section-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  flex-shrink: 0;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.sp-section-title { font-size: 13px; font-weight: 600; }
.sp-section-badge {
  font-size: 10px; background: #fff7e6; color: #fa8c16;
  padding: 0 5px; border-radius: 3px;
  &.quota { background: #fff1f0; color: #f5222d; }
}
.sp-section-body {
  padding: 12px;
}

/* 表单字段 */
.sp-field {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  &.full { align-items: flex-start; }
  label { font-size: 12px; color: #666; width: 80px; flex-shrink: 0; text-align: right; }
  input, textarea {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 5px 8px;
    font-size: 12px;
    outline: none;
    min-width: 0;
    &:focus { border-color: #2F54EB; }
    &:disabled { background: #f5f5f5; color: #999; }
  }
  textarea { resize: vertical; }
}
.sp-hint { font-size: 11px; color: #bbb; margin-left: 88px; }

/* 占位 */
.sp-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
}
.sp-ph-inner {
  text-align: center;
  color: #999;
  .sp-ph-tip { font-size: 12px; color: #bbb; margin-top: 8px; }
}
</style>
