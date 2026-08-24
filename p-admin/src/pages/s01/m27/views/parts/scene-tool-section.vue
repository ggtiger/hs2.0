<template>
  <div class="scene-tool-section sp-section">
    <div class="sts-header" @click="toggleSection">
      <i :class="sectionExpanded ? 'h-icon-down' : 'h-icon-right'"></i>
      <span class="sts-title">后端工具</span>
      <span class="sts-count" v-if="filteredTools.length">{{ filteredTools.length }} 项</span>
      <span class="sts-toolset" v-if="toolset">工具集: {{ toolset }}</span>
    </div>
    <div class="sts-body" v-if="sectionExpanded">
      <div v-if="!toolset" class="sts-empty">此场景未指定工具集</div>
      <template v-else>
        <!-- builtin 组 -->
        <div v-if="builtinTools.length" class="sts-group">
          <div class="sts-group-title">内置工具 (builtin)</div>
          <div v-for="it in builtinTools" :key="it.ID" class="sts-tool-row">
            <h-switch :value="it.ENABLED === 1" @input="toggleEnabled(it, $event)" />
            <span class="sts-tool-name">{{ it.TOOLNAME }}</span>
            <span class="sts-tool-desc" :title="it.DESCRIPTION">{{ it.DESCRIPTION || '(无描述)' }}</span>
            <span class="sts-readonly-badge">只读</span>
          </div>
        </div>
        <!-- sql 组 -->
        <div v-if="sqlTools.length" class="sts-group">
          <div class="sts-group-title">SQL 查询 (sql)</div>
          <div v-for="it in sqlTools" :key="it.ID" class="sts-tool-row">
            <h-switch :value="it.ENABLED === 1" @input="toggleEnabled(it, $event)" />
            <span class="sts-tool-name">{{ it.TOOLNAME }}</span>
            <span class="sts-tool-desc" :title="it.DESCRIPTION">{{ it.DESCRIPTION || '(无描述)' }}</span>
            <span class="sts-edit-link" @click.stop="editTool(it)">编辑</span>
          </div>
        </div>
        <!-- csharp 组 -->
        <div v-if="csharpTools.length" class="sts-group">
          <div class="sts-group-title">API 脚本 (csharp)</div>
          <div v-for="it in csharpTools" :key="it.ID" class="sts-tool-row">
            <h-switch :value="it.ENABLED === 1" @input="toggleEnabled(it, $event)" />
            <span class="sts-tool-name">{{ it.TOOLNAME }}</span>
            <span class="sts-tool-desc" :title="it.DESCRIPTION">{{ it.DESCRIPTION || '(无描述)' }}</span>
            <span class="sts-edit-link" @click.stop="editTool(it)">编辑</span>
          </div>
        </div>
        <!-- static 组 -->
        <div v-if="staticTools.length" class="sts-group">
          <div class="sts-group-title">静态合并 (static)</div>
          <div v-for="it in staticTools" :key="it.ID" class="sts-tool-row">
            <h-switch :value="it.ENABLED === 1" @input="toggleEnabled(it, $event)" />
            <span class="sts-tool-name">{{ it.TOOLNAME }}</span>
            <span class="sts-tool-desc" :title="it.DESCRIPTION">{{ it.DESCRIPTION || '(无描述)' }}</span>
            <span class="sts-edit-link" @click.stop="editTool(it)">编辑</span>
          </div>
        </div>
        <!-- 无匹配 -->
        <div v-if="filteredTools.length === 0 && toolset" class="sts-empty">工具集 "{{ toolset }}" 下暂无工具</div>
      </template>
      <!-- 操作按钮 -->
      <div class="sts-add" v-if="toolset">
        <Button size="s" @click="showSelectDialog">选入已有工具</Button>
        <Button size="s" @click="showAddDialog">+ 新建声明式工具</Button>
      </div>
    </div>

    <!-- 选入已有工具弹窗 -->
    <Modal v-model="selectVisible">
      <view-dialog title="选入已有工具到当前工具集" class="d-width">
        <div class="sts-select-list" slot="body">
          <div v-if="selectableTools.length === 0" class="sts-empty">没有可选入的工具</div>
          <div v-for="it in selectableTools" :key="it.ID" class="sts-select-row" @click="selectTool(it)">
            <span class="sts-tool-name">{{ it.TOOLNAME }}</span>
            <span class="sts-select-from">{{ it.TOOLSET }}</span>
            <span class="sts-tool-desc" :title="it.DESCRIPTION">{{ it.DESCRIPTION || '(无描述)' }}</span>
            <span class="sts-select-type">{{ it.EXECUTORTYPE }}</span>
            <span class="sts-select-link">选入</span>
          </div>
        </div>
        <div slot="footer">
          <Button @click="selectVisible = false">关闭</Button>
        </div>
      </view-dialog>
    </Modal>

    <!-- 新建工具弹窗 -->
    <Modal v-model="addVisible">
      <view-dialog title="新建工具">
        <div class="sts-edit-form" slot="body">
          <div class="sts-field">
            <label>工具名</label>
            <input v-model="addForm.TOOLNAME" placeholder="如 search_customer" ref="addNameInput" @keydown.enter="confirmAdd" />
          </div>
          <div class="sts-field">
            <label>执行类型</label>
            <Select v-model="addForm.EXECUTORTYPE" :datas="execTypeOptions" />
          </div>
          <div class="sts-field">
            <label>描述</label>
            <input v-model="addForm.DESCRIPTION" placeholder="工具功能描述(给 AI 看的)" />
          </div>
          <div class="sts-field">
            <label>{{ addForm.EXECUTORTYPE === 'csharp' ? '脚本编码' : 'SQL 模板' }}</label>
            <Select v-model="addForm.SQLCODE" :datas="codeOptions(addForm.EXECUTORTYPE, addForm.SQLCODE)" filterable :placeholder="addForm.EXECUTORTYPE === 'csharp' ? '输入或选择脚本编码' : '输入或选择 SQL 模板'" />
          </div>
          <div class="sts-field">
            <label>最大行数</label>
            <NumberInput v-model="addForm.MAXROWS" />
          </div>
          <div class="sts-field full">
            <label>参数(JSON Schema)</label>
            <textarea v-model="addForm.PARAMS" rows="4" class="mono" placeholder='{"type":"object","properties":{...}}'></textarea>
          </div>
        </div>
        <div slot="footer">
          <Button @click="addVisible = false">取消</Button>
          <Button color="primary" @click="confirmAdd" :loading="saving">确定</Button>
        </div>
      </view-dialog>
    </Modal>

    <!-- 编辑弹窗 -->
    <Modal v-model="editVisible">
      <view-dialog :title="editForm.TOOLNAME || '编辑工具'">
        <div class="sts-edit-form" slot="body">
          <div class="sts-field">
            <label>工具名</label>
            <input v-model="editForm.TOOLNAME" disabled />
          </div>
          <div class="sts-field">
            <label>工具集</label>
            <input v-model="editForm.TOOLSET" />
          </div>
          <div class="sts-field">
            <label>执行类型</label>
            <Select v-model="editForm.EXECUTORTYPE" :datas="execTypeOptions" />
          </div>
          <div class="sts-field">
            <label>{{ editForm.EXECUTORTYPE === 'csharp' ? '脚本编码' : 'SQL 模板' }}</label>
            <Select v-model="editForm.SQLCODE" :datas="codeOptions(editForm.EXECUTORTYPE, editForm.SQLCODE)" filterable :placeholder="editForm.EXECUTORTYPE === 'csharp' ? '输入或选择脚本编码' : '输入或选择 SQL 模板'" />
          </div>
          <div class="sts-field">
            <label>最大行数</label>
            <NumberInput v-model="editForm.MAXROWS" />
          </div>
          <div class="sts-field full">
            <label>描述(给 AI 看的)</label>
            <textarea v-model="editForm.DESCRIPTION" rows="2"></textarea>
          </div>
          <div class="sts-field full">
            <label>参数(JSON Schema)</label>
            <textarea v-model="editForm.PARAMS" rows="4" class="mono"></textarea>
          </div>
          <div class="sts-field full">
            <label>备注</label>
            <input v-model="editForm.REMARK" />
          </div>
        </div>
        <div slot="footer">
          <Button @click="editVisible = false">取消</Button>
          <Button color="primary" @click="saveEdit" :loading="saving">保存</Button>
        </div>
      </view-dialog>
    </Modal>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import db from '@/api/db'; // 仅用于 invalidate-tool-cache 缓存失效

const MC24 = 'RS_M24';

export default {
  name: 'SceneToolSection',
  props: {
    toolset: { type: String, default: '' },
    toolList: { type: Array, default: function() { return [] } },
    sqlOptions: { type: Array, default: function() { return [] } },
    csharpOptions: { type: Array, default: function() { return [] } }
  },
  data() {
    return {
      sectionExpanded: true,
      editVisible: false,
      addVisible: false,
      selectVisible: false,
      addForm: { TOOLNAME: '', EXECUTORTYPE: 'sql', DESCRIPTION: '', SQLCODE: '' },
      editForm: {},
      saving: false,
      execTypeOptions: [
        { key: 'sql', title: 'sql (SQL 查询, 只读)' },
        { key: 'csharp', title: 'csharp (API 脚本, 可读写)' },
        { key: 'static', title: 'static (静态合并)' }
      ]
    };
  },
  computed: {
    filteredTools() {
      var ts = this.toolset;
      if (!ts) return [];
      return this.toolList.filter(function(it) {
        return it.TOOLSET === ts && it.EXECUTORTYPE !== 'frontend';
      });
    },
    builtinTools() {
      return this.filteredTools.filter(function(it) { return it.EXECUTORTYPE === 'builtin'; });
    },
    sqlTools() {
      return this.filteredTools.filter(function(it) { return it.EXECUTORTYPE === 'sql'; });
    },
    csharpTools() {
      return this.filteredTools.filter(function(it) { return it.EXECUTORTYPE === 'csharp'; });
    },
    staticTools() {
      return this.filteredTools.filter(function(it) { return it.EXECUTORTYPE === 'static'; });
    },
    // 不在当前工具集的可选工具（sql/static 类型，排除 builtin/frontend，按 TOOLNAME 去重）
    selectableTools() {
      var ts = this.toolset;
      if (!ts) return [];
      // 当前工具集已有的工具名（用于去重）
      var currentNames = {};
      this.filteredTools.forEach(function(it) { currentNames[it.TOOLNAME] = true; });
      return this.toolList.filter(function(it) {
        return it.EXECUTORTYPE !== 'builtin'
          && it.EXECUTORTYPE !== 'frontend'
          && !currentNames[it.TOOLNAME];
      });
    }
  },
  created() {
    this.m24Store = getGenericStore(MC24);
  },
  methods: {
    toggleSection() {
      this.sectionExpanded = !this.sectionExpanded;
    },
    // 确保 SQLCODE 当前值在 options 中可见（HeyUI Select 找不到 key 会显示空白）
    codeOptions(execType, currentValue) {
      var base = execType === 'csharp' ? this.csharpOptions : this.sqlOptions;
      if (!currentValue) return base;
      var has = base.some(function(o) { return o.key === currentValue; });
      if (has) return base;
      return base.concat([{ key: currentValue, title: currentValue }]);
    },
    async toggleEnabled(it, val) {
      var newEnabled = val ? 1 : 0;
      await this.$callAction({ action: MC24 + '/open', param: { ID: it.ID } });
      var MAIN = this.m24Store.storeHelper.getTable('MAIN');
      if (MAIN) {
        MAIN.setValue('ENABLED', newEnabled);
        await this.$callAction({
          action: MC24 + '/save',
          successText: newEnabled === 1 ? '已启用' : '已停用',
          successCall: () => this.$emit('refresh-tools')
        });
      }
    },
    // 选入已有工具：复制一份到当前工具集
    async selectTool(it) {
      try {
        this.$store.commit(MC24 + '/INIT', { paths: ['MAIN'] });
        this.$store.commit(MC24 + '/ADD', { path: 'MAIN', item: {} });
        var MAIN = this.m24Store.storeHelper.getTable('MAIN');
        MAIN.setValue('TOOLNAME', it.TOOLNAME);
        MAIN.setValue('TOOLSET', this.toolset);
        MAIN.setValue('EXECUTORTYPE', it.EXECUTORTYPE || 'sql');
        MAIN.setValue('DESCRIPTION', it.DESCRIPTION || it.TOOLNAME);
        MAIN.setValue('SQLCODE', it.SQLCODE || '');
        MAIN.setValue('MAXROWS', it.MAXROWS || 10);
        MAIN.setValue('PARAMS', it.PARAMS || '');
        MAIN.setValue('ENABLED', 1);
        MAIN.setValue('ISDELETED', 0);
        MAIN.setValue('REMARK', it.REMARK || '');
        await this.$callAction({
          action: MC24 + '/save',
          successText: '已选入 ' + it.TOOLNAME,
          successCall: () => {
            this.selectVisible = false;
            this.$emit('refresh-tools');
          }
        });
      } catch (e) {
        this.$Message('选入失败: ' + (e && e.message ? e.message : e));
      }
    },
    showSelectDialog() {
      this.selectVisible = true;
    },
    async editTool(it) {
      await this.$callAction({ action: MC24 + '/open', param: { ID: it.ID } });
      var st = this.$store.state[MC24];
      var mainData = (st && st.dt && st.dt.MAIN && st.dt.MAIN.data) || [];
      var row = mainData[0] || it;
      this.editForm = {
        ID: it.ID,
        TOOLNAME: row.TOOLNAME || it.TOOLNAME,
        TOOLSET: row.TOOLSET || it.TOOLSET,
        DESCRIPTION: row.DESCRIPTION || it.DESCRIPTION,
        PARAMS: row.PARAMS || it.PARAMS,
        EXECUTORTYPE: row.EXECUTORTYPE || it.EXECUTORTYPE,
        SQLCODE: row.SQLCODE || it.SQLCODE,
        MAXROWS: row.MAXROWS || it.MAXROWS,
        ENABLED: row.ENABLED !== undefined ? row.ENABLED : it.ENABLED,
        REMARK: row.REMARK || it.REMARK
      };
      this.editVisible = true;
    },
    async saveEdit() {
      this.saving = true;
      try {
        var MAIN = this.m24Store.storeHelper.getTable('MAIN');
        var keys = ['TOOLNAME', 'TOOLSET', 'DESCRIPTION', 'PARAMS', 'EXECUTORTYPE', 'SQLCODE', 'MAXROWS', 'ENABLED', 'REMARK'];
        var self = this;
        keys.forEach(function(k) { MAIN.setValue(k, self.editForm[k]); });
        await this.$callAction({
          action: MC24 + '/save',
          successText: '保存成功',
          successCall: () => {
            this.editVisible = false;
            this.$emit('refresh-tools');
            db.postData({ api: '/api/RMAIDev/invalidate-tool-cache', params: {} }).catch(function() {});
          }
        });
      } finally {
        this.saving = false;
      }
    },
    showAddDialog() {
      this.addForm = { TOOLNAME: '', EXECUTORTYPE: 'sql', DESCRIPTION: '', SQLCODE: '', MAXROWS: 10, PARAMS: '' };
      this.addVisible = true;
      this.$nextTick(() => {
        var el = this.$refs.addNameInput;
        if (el) el.focus();
      });
    },
    confirmAdd() {
      var name = (this.addForm.TOOLNAME || '').trim();
      if (!name) { this.$Message('工具名不能为空'); return }
      var desc = (this.addForm.DESCRIPTION || '').trim();
      if (!desc) { this.$Message('描述不能为空'); return }
      this.saving = true;
      this.$store.commit(MC24 + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC24 + '/ADD', { path: 'MAIN', item: {} });
      var MAIN = this.m24Store.storeHelper.getTable('MAIN');
      MAIN.setValue('TOOLNAME', name);
      MAIN.setValue('TOOLSET', this.toolset);
      MAIN.setValue('EXECUTORTYPE', this.addForm.EXECUTORTYPE || 'sql');
      MAIN.setValue('DESCRIPTION', desc);
      MAIN.setValue('SQLCODE', (this.addForm.SQLCODE || '').trim());
      MAIN.setValue('MAXROWS', this.addForm.MAXROWS || 10);
      MAIN.setValue('PARAMS', (this.addForm.PARAMS || '').trim());
      MAIN.setValue('ENABLED', 1);
      MAIN.setValue('ISDELETED', 0);
      this.$callAction({
        action: MC24 + '/save',
        successText: '已创建',
        successCall: () => {
          this.addVisible = false;
          this.$emit('refresh-tools');
        }
      });
      this.saving = false;
    }
  }
};
</script>

<style lang="less" scoped>
.scene-tool-section {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.sts-header {
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
.sts-title { font-size: 13px; font-weight: 600; }
.sts-count {
  font-size: 11px; background: #f0f5ff; color: #2F54EB;
  padding: 0 6px; border-radius: 8px;
}
.sts-toolset {
  font-size: 11px; color: #999; margin-left: auto;
  font-family: Consolas, monospace;
}
.sts-body { padding: 12px; }
.sts-empty { font-size: 12px; color: #999; padding: 8px 0; }
.sts-group { margin-bottom: 10px; }
.sts-group-title {
  font-size: 12px; font-weight: 600; color: #666;
  margin-bottom: 6px; padding-bottom: 4px;
  border-bottom: 1px dashed #e8e8e8;
}
.sts-tool-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 0;
  font-size: 12px;
}
.sts-tool-name {
  font-family: Consolas, monospace;
  color: #333; font-weight: 600;
  min-width: 140px;
}
.sts-tool-desc {
  flex: 1; color: #999;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.sts-readonly-badge {
  font-size: 10px; background: #f5f5f5; color: #999;
  padding: 0 5px; border-radius: 3px;
}
.sts-edit-link {
  font-size: 11px; color: #2F54EB; cursor: pointer;
  &:hover { text-decoration: underline; }
}
.sts-add { margin-top: 8px; display: flex; gap: 6px; }
/* 选入弹窗 */
.sts-select-list {
  max-height: 400px;
  overflow-y: auto;
}
.sts-select-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 0;
  font-size: 12px;
  border-bottom: 1px solid #f5f5f5;
  cursor: pointer;
  &:hover { background: #f5f7fa; }
}
.sts-select-from {
  font-size: 11px; color: #999; background: #f5f5f5;
  padding: 0 5px; border-radius: 3px;
  font-family: Consolas, monospace;
}
.sts-select-type {
  font-size: 10px; background: #f6ffed; color: #52c41a;
  padding: 0 5px; border-radius: 3px;
}
.sts-select-link {
  margin-left: auto;
  font-size: 11px; color: #2F54EB;
  &:hover { text-decoration: underline; }
}
/* 编辑/新增表单 */
.sts-edit-form {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.sts-field {
  display: flex;
  align-items: center;
  gap: 6px;
  &.full { align-items: flex-start; }
  label { font-size: 12px; color: #666; width: 90px; flex-shrink: 0; text-align: right; }
  input, textarea {
    flex: 1; border: 1px solid #d9d9d9; border-radius: 4px;
    padding: 5px 8px; font-size: 12px; outline: none; min-width: 0;
    &:focus { border-color: #2F54EB; }
    &:disabled { background: #f0f0f0; color: #999; }
  }
  textarea { resize: vertical; }
  .mono { font-family: Consolas, monospace; }
}
</style>
