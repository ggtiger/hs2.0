<template>
  <div class="page-list-panel">
    <!-- 页面列表区 -->
    <div class="page-list-section">
      <div class="page-list-header">
        <span class="page-list-title">页面配置</span>
        <button class="page-list-add-btn" @click="$emit('add')">
          <i class="h-icon-plus"></i> 新增页面
        </button>
      </div>
      <div class="page-list-items">
        <div
          v-for="page in pages"
          :key="page.ID"
          class="page-list-item"
          :class="{ 'page-list-item-active': page.ID === selectedPageId }"
          @click="$emit('select', page.ID)"
        >
          <span class="page-list-item-icon" :class="'page-type-' + page.PAGETYPE">
            {{ pageTypeIcon(page.PAGETYPE) }}
          </span>
          <span class="page-list-item-name">{{ page.PAGENAME || page.PAGECODE }}</span>
          <span class="page-list-item-type">{{ page.PAGETYPE }}</span>
          <button class="page-list-item-del" @click.stop="$emit('delete', page)" title="删除">
            <i class="h-icon-trash"></i>
          </button>
        </div>
        <div v-if="pages.length === 0" class="page-list-empty">
          暂无页面，点击上方按钮新增
        </div>
      </div>
    </div>

    <!-- 属性编辑区 -->
    <div class="page-props-section" v-if="selectedPage">
      <div class="page-list-header">
        <span class="page-list-title">页面属性</span>
      </div>
      <div class="page-props-form">
        <div class="page-props-row">
          <label>页面编码</label>
          <input v-model="editPage.PAGECODE" @input="syncField('PAGECODE', $event.target.value)" class="page-props-input" placeholder="如 main" />
        </div>
        <div class="page-props-row">
          <label>页面名称</label>
          <input v-model="editPage.PAGENAME" @input="syncField('PAGENAME', $event.target.value)" class="page-props-input" placeholder="如 列表页" />
        </div>
        <div class="page-props-row">
          <label>页面类型</label>
          <select v-model="editPage.PAGETYPE" @change="syncField('PAGETYPE', editPage.PAGETYPE)" class="page-props-select">
            <option value="list">list (列表)</option>
            <option value="form">form (表单)</option>
            <option value="review">review (审核)</option>
            <option value="report">report (报表)</option>
          </select>
        </div>
        <div class="page-props-row">
          <label>路由路径</label>
          <input v-model="editPage.ROUTEPATH" @input="syncField('ROUTEPATH', $event.target.value)" class="page-props-input" placeholder="如 /b01/m01/main" />
        </div>
        <div class="page-props-row">
          <label>组件类型</label>
          <select v-model="editPage.COMPONENTTYPE" @change="syncField('COMPONENTTYPE', editPage.COMPONENTTYPE)" class="page-props-select">
            <option value="standard">standard (标准)</option>
            <option value="sfc">sfc (在线组件)</option>
          </select>
        </div>
        <div class="page-props-row" v-if="editPage.COMPONENTTYPE === 'sfc'">
          <label>SFC路径</label>
          <input v-model="editPage.SFCMODULEPATH" @input="syncField('SFCMODULEPATH', $event.target.value)" class="page-props-input" placeholder="SFC组件路径" />
        </div>
        <div class="page-props-row">
          <label>查询接口</label>
          <input v-model="editPage.QUERYAPICODE" @input="syncField('QUERYAPICODE', $event.target.value)" class="page-props-input" placeholder="如 A01" />
        </div>
        <div class="page-props-row">
          <label>打开接口</label>
          <input v-model="editPage.OPENAPICODE" @input="syncField('OPENAPICODE', $event.target.value)" class="page-props-input" placeholder="如 A02" />
        </div>
        <div class="page-props-row">
          <label>保存接口</label>
          <input v-model="editPage.SAVEAPICODE" @input="syncField('SAVEAPICODE', $event.target.value)" class="page-props-input" placeholder="如 A04" />
        </div>
        <div class="page-props-row">
          <label>排序号</label>
          <input v-model.number="editPage.SORTNO" @input="syncField('SORTNO', editPage.SORTNO)" class="page-props-input" type="number" />
        </div>
      </div>
    </div>

    <!-- 按钮配置区 -->
    <div class="page-buttons-section" v-if="selectedPage">
      <div class="page-list-header">
        <span class="page-list-title">按钮配置</span>
        <button class="page-list-add-btn page-list-add-btn-sm" @click="addButton">
          <i class="h-icon-plus"></i> 新增
        </button>
      </div>
      <div class="page-buttons-table">
        <table class="btn-config-table">
          <thead>
            <tr>
              <th>名称</th>
              <th>类型</th>
              <th>区域</th>
              <th>接口</th>
              <th>子表</th>
              <th style="width:40px"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="btn in pageButtons" :key="btn.ID">
              <td>
                <input v-model="btn.BTNNAME" @input="syncBtnField(btn, 'BTNNAME', btn.BTNNAME)" class="btn-table-input" />
              </td>
              <td>
                <select v-model="btn.BTNTYPE" @change="syncBtnField(btn, 'BTNTYPE', btn.BTNTYPE)" class="btn-table-select">
                  <option value="crud">crud</option>
                  <option value="flow">flow</option>
                  <option value="custom">custom</option>
                  <option value="batch">batch</option>
                </select>
              </td>
              <td>
                <select v-model="btn.BTNAREA" @change="syncBtnField(btn, 'BTNAREA', btn.BTNAREA)" class="btn-table-select">
                  <option value="header">header</option>
                  <option value="footer">footer</option>
                  <option value="row">row</option>
                  <option value="subtable">subtable</option>
                </select>
              </td>
              <td>
                <input v-model="btn.APICODE" @input="syncBtnField(btn, 'APICODE', btn.APICODE)" class="btn-table-input" />
              </td>
              <td>
                <select v-if="btn.BTNAREA === 'subtable'" :value="btnSubtable(btn)" @change="syncBtnSubtable(btn, $event.target.value)" class="btn-table-select">
                  <option value="">请选择</option>
                  <option v-for="p in subPaths" :key="p" :value="p">{{p}}</option>
                </select>
              </td>
              <td>
                <button class="btn-table-del" @click="deleteButton(btn)">
                  <i class="h-icon-trash"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="pageButtons.length === 0" class="page-list-empty">
          暂无按钮
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapDateTable, Constants } from '../../store';

export default {
  name: 'page-list',
  computed: {
    ...mapDateTable('MODPAGE', []),
    ...mapDateTable('MODBUTTON', []),
    pages() {
      var dt = this.$MODPAGE;
      if (!dt || !dt.data) return [];
      return dt.data.filter(function(p) {
        return (p.ISDELETED || 0) === 0;
      }).sort(function(a, b) {
        return (a.SORTNO || 0) - (b.SORTNO || 0);
      });
    },
    selectedPage() {
      var self = this;
      var found = null;
      this.pages.forEach(function(p) {
        if (!found && p.ID === self.selectedPageId) found = p;
      });
      return found;
    },
    editPage() {
      return this.selectedPage || {};
    },
    pageButtons() {
      var self = this;
      var dt = this.$MODBUTTON;
      if (!dt || !dt.data) return [];
      return dt.data.filter(function(b) {
        return b.PAGEID === self.selectedPageId && (b.ISDELETED || 0) === 0;
      }).sort(function(a, b) {
        return (a.SORTNO || 0) - (b.SORTNO || 0);
      });
    },
    // 子表路径选项(从模块 MODPATHREF 取), 用于 subtable 按钮选择归属子表
    subPaths() {
      var moduleCode = this.$store.state[Constants.STORE_NAME].configModuleCode;
      var modData = this.$store.state.app && this.$store.state.app.modules && this.$store.state.app.modules[moduleCode];
      if (!modData || !modData.MODPATHREF) return [];
      var seen = {};
      var paths = [];
      modData.MODPATHREF.forEach(function(ref) {
        if (!seen[ref.PATHNAMEB]) { seen[ref.PATHNAMEB] = true; paths.push(ref.PATHNAMEB) }
      });
      return paths;
    }
  },
  props: {
    selectedPageId: {
      type: String,
      default: ''
    }
  },
  methods: {
    pageTypeIcon(type) {
      var icons = { list: '☰', form: '☐', review: '✓', report: '📊' };
      return icons[type] || '☐';
    },
    syncField(field, value) {
      var dt = this.$MODPAGE;
      if (!dt || !this.selectedPage) return;
      dt.setValue(field, value, this.selectedPage);
    },
    syncBtnField(btn, field, value) {
      var dt = this.$MODBUTTON;
      if (!dt) return;
      dt.setValue(field, value, btn);
    },
    // 解析按钮 EXTPARAM.subtable
    btnSubtable(btn) {
      if (!btn.EXTPARAM) return '';
      try {
        var ext = typeof btn.EXTPARAM === 'string' ? JSON.parse(btn.EXTPARAM) : btn.EXTPARAM;
        return (ext && ext.subtable) || '';
      } catch (e) { return '' }
    },
    // 同步 EXTPARAM.subtable(合并已有 EXTPARAM)
    syncBtnSubtable(btn, sub) {
      var ext = {};
      try { ext = typeof btn.EXTPARAM === 'string' ? JSON.parse(btn.EXTPARAM || '{}') : (btn.EXTPARAM || {}) } catch (e) {}
      ext.subtable = sub;
      this.syncBtnField(btn, 'EXTPARAM', JSON.stringify(ext));
    },
    addButton() {
      var moduleCode = this.$store.state[Constants.STORE_NAME].configModuleCode;
      this.$store.commit(Constants.STORE_NAME + '/ADD', {
        path: 'MODBUTTON',
        item: {
          ID: 'tmp_' + Date.now() + '_' + Math.floor(Math.random() * 1000),
          PAGEID: this.selectedPageId,
          MODULECODE: moduleCode,
          BTNNAME: '新按钮',
          BTNTYPE: 'custom',
          BTNAREA: 'header',
          INTERACTTYPE: 'direct',
          ISDELETED: 0,
          SORTNO: this.pageButtons.length + 1
        }
      });
    },
    deleteButton(btn) {
      var dt = this.$MODBUTTON;
      if (dt) {
        dt.setValue('ISDELETED', 1, btn);
      }
    }
  }
};
</script>

<style lang="less" scoped>
.page-list-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #fff;
}
.page-list-section {
  flex-shrink: 0;
  border-bottom: 1px solid #e0e0e0;
}
.page-list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
}
.page-list-title {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
}
.page-list-add-btn {
  background: none;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 2px 10px;
  cursor: pointer;
  font-size: 12px;
  color: #606266;
  display: flex;
  align-items: center;
  gap: 4px;
  &:hover {
    color: #0a84ff;
    border-color: #0a84ff;
  }
}
.page-list-add-btn-sm {
  font-size: 11px;
  padding: 1px 8px;
}
.page-list-items {
  max-height: 200px;
  overflow-y: auto;
}
.page-list-item {
  display: flex;
  align-items: center;
  padding: 6px 12px;
  cursor: pointer;
  border-bottom: 1px solid #f0f0f0;
  gap: 8px;
  &:hover {
    background: #f5f7fa;
  }
}
.page-list-item-active {
  background: #ecf5ff;
  border-left: 3px solid #0a84ff;
}
.page-list-item-icon {
  width: 20px;
  text-align: center;
  font-size: 14px;
  flex-shrink: 0;
}
.page-type-list { color: #409eff; }
.page-type-form { color: #67c23a; }
.page-type-review { color: #e6a23c; }
.page-type-report { color: #909399; }
.page-list-item-name {
  flex: 1;
  font-size: 13px;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.page-list-item-type {
  font-size: 11px;
  color: #909399;
  background: #f4f4f5;
  padding: 1px 6px;
  border-radius: 3px;
  flex-shrink: 0;
}
.page-list-item-del {
  background: none;
  border: none;
  cursor: pointer;
  color: #c0c4cc;
  padding: 2px;
  flex-shrink: 0;
  &:hover {
    color: #f56c6c;
  }
}
.page-list-empty {
  padding: 20px;
  text-align: center;
  color: #c0c4cc;
  font-size: 13px;
}
.page-props-section {
  flex-shrink: 0;
  border-bottom: 1px solid #e0e0e0;
}
.page-props-form {
  padding: 8px 12px;
}
.page-props-row {
  display: flex;
  align-items: center;
  margin-bottom: 6px;
  label {
    width: 70px;
    text-align: right;
    padding-right: 8px;
    font-size: 12px;
    color: #606266;
    flex-shrink: 0;
  }
}
.page-props-input,
.page-props-select {
  flex: 1;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 4px 8px;
  font-size: 12px;
  outline: none;
  &:focus {
    border-color: #0a84ff;
  }
}
.page-buttons-section {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.page-buttons-table {
  flex: 1;
  overflow-y: auto;
  padding: 0 4px;
}
.btn-config-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  th {
    background: #fafafa;
    padding: 4px 6px;
    text-align: left;
    color: #909399;
    font-weight: 500;
    border-bottom: 1px solid #e0e0e0;
  }
  td {
    padding: 2px 4px;
    border-bottom: 1px solid #f0f0f0;
  }
}
.btn-table-input,
.btn-table-select {
  width: 100%;
  border: 1px solid transparent;
  border-radius: 3px;
  padding: 2px 4px;
  font-size: 12px;
  outline: none;
  &:focus {
    border-color: #0a84ff;
  }
}
.btn-table-del {
  background: none;
  border: none;
  cursor: pointer;
  color: #c0c4cc;
  padding: 2px;
  &:hover {
    color: #f56c6c;
  }
}
</style>
