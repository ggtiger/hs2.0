<template>
  <div class="tools-part">
    <div class="tp-toolbar">
      <span class="tp-title">AI 工具（{{ list.length }}）</span>
      <Button size="s" color="primary" @click="addNew">+ 新增工具</Button>
    </div>
    <div class="tp-body" v-loading="loading">
      <!-- 按 EXECUTORTYPE 分组 -->
      <div v-for="group in groups" :key="group.key" class="tp-group">
        <div class="tp-group-header" @click="toggleGroup(group.key)">
          <i :class="collapsedGroups[group.key] ? 'h-icon-right' : 'h-icon-down'"></i>
          <span :class="['tp-group-tag', group.key]">{{ group.label }}</span>
          <span class="tp-group-count">{{ group.items.length }}</span>
        </div>
        <div class="tp-group-body" v-if="!collapsedGroups[group.key]">
          <div v-for="it in group.items" :key="it.ID" class="tp-row">
            <div class="tp-row-head" @click="toggleExpand(it)">
              <i :class="expandedId === it.ID ? 'h-icon-down' : 'h-icon-right'"></i>
              <span class="tp-name">{{ it.TOOLNAME }}</span>
              <span class="tp-tag">{{ it.TOOLSET }}</span>
              <span :class="['tp-enabled', it.ENABLED === 1 ? 'on' : 'off']">{{ it.ENABLED === 1 ? '启用' : '停用' }}</span>
              <span class="tp-desc">{{ it.DESCRIPTION }}</span>
            </div>
            <!-- builtin 内置工具: 描述和参数可编辑(运行时生效), 工具名/类型不可改 -->
            <div class="tp-row-edit" v-if="expandedId === it.ID && it.EXECUTORTYPE === 'builtin'">
              <div class="tp-builtin-tip">内置工具（C# 执行器），工具名/类型不可改。描述和参数可在配置中心在线修改，修改后即时生效（下次 AI 对话使用新描述）。</div>
              <div class="tp-form-grid">
                <div class="tp-field">
                  <label>工具名</label>
                  <input :value="it.TOOLNAME" disabled />
                </div>
                <div class="tp-field">
                  <label>工具集</label>
                  <input :value="it.TOOLSET" disabled />
                </div>
                <div class="tp-field">
                  <label>启用</label>
                  <h-switch :value="it._edit.ENABLED===1" @input="it._edit.ENABLED=$event?1:0" />
                </div>
                <div class="tp-field full">
                  <label>描述(给 AI 看的)</label>
                  <textarea v-model="it._edit.DESCRIPTION" rows="2"></textarea>
                </div>
                <div class="tp-field full">
                  <label>参数(JSON Schema)</label>
                  <textarea v-model="it._edit.PARAMS" rows="4" class="mono"></textarea>
                </div>
                <div class="tp-field full">
                  <label>备注</label>
                  <input v-model="it._edit.REMARK" />
                </div>
              </div>
              <div class="tp-edit-actions">
                <Button size="s" color="primary" @click="save(it)" :loading="saving">保存</Button>
              </div>
            </div>
            <!-- frontend 前端工具: 只读展示 -->
            <div class="tp-row-edit" v-if="expandedId === it.ID && it.EXECUTORTYPE === 'frontend'">
              <div class="tp-builtin-tip">前端工具（JS 执行器），由前端 aiAgentProxy 注册，描述可在配置中心查看。前端工具的启用/停用由场景的 FRONTENDTOOLS 字段控制。</div>
              <div class="tp-form-grid">
                <div class="tp-field">
                  <label>工具名</label>
                  <input :value="it.TOOLNAME" disabled />
                </div>
                <div class="tp-field">
                  <label>工具集</label>
                  <input :value="it.TOOLSET" disabled />
                </div>
                <div class="tp-field">
                  <label>启用</label>
                  <h-switch :value="it._edit.ENABLED===1" @input="it._edit.ENABLED=$event?1:0" />
                </div>
                <div class="tp-field full">
                  <label>描述(给 AI 看的)</label>
                  <textarea v-model="it._edit.DESCRIPTION" rows="2"></textarea>
                </div>
                <div class="tp-field full">
                  <label>备注</label>
                  <input v-model="it._edit.REMARK" />
                </div>
              </div>
              <div class="tp-edit-actions">
                <Button size="s" color="primary" @click="save(it)" :loading="saving">保存</Button>
              </div>
            </div>
            <!-- sql/csharp/static 声明式工具: 可编辑 -->
            <div class="tp-row-edit" v-if="expandedId === it.ID && it.EXECUTORTYPE !== 'builtin' && it.EXECUTORTYPE !== 'frontend'">
              <div class="tp-form-grid">
                <div class="tp-field">
                  <label>工具名</label>
                  <input v-model="it._edit.TOOLNAME" disabled />
                </div>
                <div class="tp-field">
                  <label>工具集</label>
                  <input v-model="it._edit.TOOLSET" />
                </div>
                <div class="tp-field">
                  <label>执行类型</label>
                  <Select v-model="it._edit.EXECUTORTYPE" :datas="execOptions" />
                </div>
                <div class="tp-field">
                  <label>{{ it._edit.EXECUTORTYPE === 'csharp' ? '脚本编码' : 'SQL 模板' }}</label>
                  <Select v-model="it._edit.SQLCODE" :datas="codeOptions(it._edit.EXECUTORTYPE, it._edit.SQLCODE)" filterable :placeholder="it._edit.EXECUTORTYPE === 'csharp' ? '输入或选择脚本编码' : '输入或选择 SQL 模板'" />
                </div>
                <div class="tp-field">
                  <label>最大行数</label>
                  <NumberInput v-model="it._edit.MAXROWS" />
                </div>
                <div class="tp-field">
                  <label>启用</label>
                  <h-switch :value="it._edit.ENABLED===1" @input="it._edit.ENABLED=$event?1:0" />
                </div>
                <div class="tp-field full">
                  <label>描述(给 AI 看的)</label>
                  <textarea v-model="it._edit.DESCRIPTION" rows="2"></textarea>
                </div>
                <div class="tp-field full">
                  <label>参数(JSON Schema)</label>
                  <textarea v-model="it._edit.PARAMS" rows="4" class="mono"></textarea>
                </div>
                <div class="tp-field full">
                  <label>备注</label>
                  <input v-model="it._edit.REMARK" />
                </div>
              </div>
              <div class="tp-edit-actions">
                <Button size="s" color="primary" @click="save(it)" :loading="saving">保存</Button>
                <Poptip content="确定删除该工具？" @confirm="del(it)">
                  <Button size="s" color="red">删除</Button>
                </Poptip>
              </div>
            </div>
          </div>
          <div v-if="group.items.length === 0" class="tp-empty-sm">暂无</div>
        </div>
      </div>
      <div v-if="!loading && list.length === 0" class="tp-empty">暂无工具</div>
    </div>

    <!-- 新增工具弹窗 -->
    <Modal v-model="addVisible">
      <view-dialog title="新增工具">
        <div class="tp-add-form" slot="body">
          <div class="tp-field">
            <label>工具名</label>
            <input v-model="addName" placeholder="如 search_customer" ref="addNameInput" @keydown.enter="confirmAdd" />
          </div>
          <div class="tp-field">
            <label>执行类型</label>
            <Select v-model="addExecType" :datas="execOptions" />
          </div>
        </div>
        <div slot="footer">
          <Button @click="addVisible = false">取消</Button>
          <Button color="primary" @click="confirmAdd">确定</Button>
        </div>
      </view-dialog>
    </Modal>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';
import db from '@/api/db';
import { GROUP_DEFS, EXEC_TYPE_OPTIONS } from '@/constants';

const MC = 'RS_M24';

export default {
  name: 'ToolsPart',
  data() {
    return {
      storeName: MC,
      loading: false,
      saving: false,
      list: [],
      expandedId: '',
      collapsedGroups: { builtin: true, frontend: true },
      addVisible: false,
      addName: '',
      addExecType: 'sql',
      execOptions: EXEC_TYPE_OPTIONS,
      sqlOptions: [],
      csharpOptions: []
    };
  },
  computed: {
    groups() {
      var list = this.list;
      return GROUP_DEFS.map(function(g) {
        return {
          key: g.key,
          label: g.label,
          sort: g.sort,
          items: list.filter(function(it) { return it.EXECUTORTYPE === g.key; })
        };
      }).filter(function(g) { return g.items.length > 0; });
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
    this.m17Store = getGenericStore('RS_M17');
  },
  mounted() {
    this.loadList();
    this.loadCodeOptions();
  },
  methods: {
    toggleGroup(key) {
      this.$set(this.collapsedGroups, key, !this.collapsedGroups[key]);
    },
    async loadCodeOptions() {
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
        this.$emit('count', { key: 'tools', n: this.list.length });
      } finally {
        this.loading = false;
      }
    },
    async toggleExpand(it) {
      if (this.expandedId === it.ID) {
        this.expandedId = '';
        return;
      }
      this.expandedId = it.ID;
      this.$set(it, '_edit', Object.assign({}, it));
      await this.$callAction({ action: MC + '/open', param: { ID: it.ID } });
      // open 完成后用 store 完整数据更新 _edit，确保 SQLCODE 等字段不丢失
      var st = this.$store.state[MC];
      var mainData = (st && st.dt && st.dt.MAIN && st.dt.MAIN.data) || [];
      var row = mainData[0];
      if (row) {
        var edit = it._edit;
        var keys = ['TOOLNAME', 'TOOLSET', 'DESCRIPTION', 'PARAMS', 'EXECUTORTYPE', 'SQLCODE', 'MAXROWS', 'ENABLED', 'REMARK'];
        keys.forEach(function(k) {
          if (row[k] !== undefined && row[k] !== null) {
            edit[k] = row[k];
          }
        });
      }
    },
    formatParams(p) {
      if (!p) return '(无参数)';
      try { return JSON.stringify(JSON.parse(p), null, 2) } catch (e) { return p }
    },
    // 确保 SQLCODE 当前值在 options 中可见（HeyUI Select 找不到 key 会显示空白）
    codeOptions(execType, currentValue) {
      var base = execType === 'csharp' ? this.csharpOptions : this.sqlOptions;
      if (!currentValue) return base;
      var has = base.some(function(o) { return o.key === currentValue; });
      if (has) return base;
      return base.concat([{ key: currentValue, title: currentValue }]);
    },
    addNew() {
      this.addName = '';
      this.addExecType = 'sql';
      this.addVisible = true;
      this.$nextTick(() => {
        var el = this.$refs.addNameInput;
        if (el) el.focus();
      });
    },
    confirmAdd() {
      var name = (this.addName || '').trim();
      if (!name) { this.$Message('工具名不能为空'); return }
      this.addVisible = false;
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: {} });
      var MAIN = this.storeObj.storeHelper.getTable('MAIN');
      MAIN.setValue('TOOLNAME', name);
      MAIN.setValue('TOOLSET', 'readonly');
      MAIN.setValue('EXECUTORTYPE', this.addExecType);
      MAIN.setValue('DESCRIPTION', '');
      MAIN.setValue('ENABLED', 1);
      MAIN.setValue('ISDELETED', 0);
      this.$callAction({
        action: MC + '/save',
        successText: '已创建, 展开编辑详情',
        successCall: () => this.loadList()
      });
    },
    async save(it) {
      this.saving = true;
      try {
        var MAIN = this.storeObj.storeHelper.getTable('MAIN');
        var keys = ['TOOLNAME', 'TOOLSET', 'DESCRIPTION', 'PARAMS', 'EXECUTORTYPE', 'SQLCODE', 'MAXROWS', 'ENABLED', 'REMARK'];
        keys.forEach(k => MAIN.setValue(k, it._edit[k]));
        await this.$callAction({
          action: MC + '/save',
          successText: '保存成功',
          successCall: () => {
            this.expandedId = '';
            this.loadList();
            // 刷新后端工具定义缓存
            db.postData({ api: '/api/RMAIDev/invalidate-tool-cache', params: {} }).catch(function() {});
          }
        });
      } finally {
        this.saving = false;
      }
    },
    async del(it) {
      this.$store.commit(MC + '/INIT', { paths: ['MAIN'] });
      this.$store.commit(MC + '/ADD', { path: 'MAIN', item: { ID: it.ID } });
      await this.$callAction({
        action: MC + '/delete',
        successText: '删除成功',
        successCall: () => { this.expandedId = ''; this.loadList() }
      });
    }
  }
};
</script>

<style lang="less" scoped>
.tools-part { flex: 1; display: flex; flex-direction: column; min-height: 0; }
.tp-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
}
.tp-title { font-size: 13px; font-weight: 600; }
.tp-body { flex: 1; overflow-y: auto; padding: 8px 16px; }

/* 分组 */
.tp-group { margin-bottom: 12px; }
.tp-group-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: #fafafa;
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  cursor: pointer;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.tp-group-tag {
  font-size: 13px; font-weight: 600;
  padding: 1px 8px; border-radius: 3px;
  &.builtin { background: #f5f5f5; color: #666; }
  &.frontend { background: #fff7e6; color: #fa8c16; }
  &.sql { background: #f6ffed; color: #52c41a; }
  &.csharp { background: #f9f0ff; color: #722ed1; }
  &.static { background: #f0f5ff; color: #2F54EB; }
}
.tp-group-count {
  font-size: 11px; color: #999; background: #f0f0f0;
  padding: 0 6px; border-radius: 8px;
}
.tp-group-body { padding: 4px 0 0 16px; border-left: 2px solid #e8e8e8; margin-left: 8px; margin-top: 4px; }

.tp-row {
  background: #fff;
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  margin-bottom: 6px;
  overflow: hidden;
}
.tp-row-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  cursor: pointer;
  &:hover { background: #f8f9fa; }
  i { color: #999; }
}
.tp-name { font-size: 13px; font-weight: 600; font-family: Consolas, monospace; }
.tp-tag {
  font-size: 11px; background: #f0f5ff; color: #2F54EB;
  padding: 1px 8px; border-radius: 3px;
}
.tp-builtin-tip {
  font-size: 12px;
  color: #999;
  background: #fafafa;
  border: 1px dashed #e8e8e8;
  border-radius: 4px;
  padding: 8px 12px;
  margin-bottom: 10px;
}
.tp-enabled {
  font-size: 11px; padding: 1px 6px; border-radius: 3px;
  &.on { background: #f6ffed; color: #52c41a; }
  &.off { background: #f5f5f5; color: #999; }
}
.tp-desc {
  font-size: 12px; color: #999;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  flex: 1; text-align: right;
}
.tp-row-edit { border-top: 1px solid #f0f0f0; padding: 12px; background: #fafafa; }
.tp-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr;
  gap: 10px 16px;
}
.tp-field {
  display: flex;
  align-items: center;
  gap: 6px;
  &.full { grid-column: span 3; align-items: flex-start; }
  label { font-size: 12px; color: #666; width: 100px; flex-shrink: 0; text-align: right; }
  input, textarea {
    flex: 1;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 5px 8px;
    font-size: 12px;
    outline: none;
    min-width: 0;
    &:focus { border-color: #2F54EB; }
    &:disabled { background: #f0f0f0; color: #999; }
  }
  textarea { resize: vertical; }
  .mono { font-family: Consolas, monospace; }
}
.tp-edit-actions { display: flex; gap: 8px; margin-top: 10px; }
.tp-empty { text-align: center; color: #bbb; padding: 40px; }
.tp-empty-sm { text-align: center; color: #ccc; padding: 12px; font-size: 12px; }
.tp-add-form { padding: 8px 0; }
</style>
