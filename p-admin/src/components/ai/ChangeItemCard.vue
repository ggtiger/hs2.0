<template>
  <div class="ai-change-card">
    <div class="acc-head">
      <span class="acc-cat">{{ categoryLabel }}</span>
      <span class="acc-action" :class="'acc-action-' + (item.ACTION || '').toLowerCase()">{{ actionLabel }}</span>
      <span class="acc-target">{{ item.TARGET || '-' }}</span>
    </div>
    <div class="acc-summary" v-if="summary && summary.length > 0">
      <div v-for="(line, i) in summary" :key="i" class="acc-line">
        <span class="acc-line-k">{{ line.k }}</span>
        <span class="acc-line-v">{{ line.v }}</span>
      </div>
    </div>
    <div class="acc-sql" v-if="sqlText">
      <div class="acc-sql-toggle" @click="sqlOpen = !sqlOpen">
        {{ sqlOpen ? '收起' : '查看' }} SQL
      </div>
      <pre v-show="sqlOpen" class="acc-sql-code">{{ sqlText }}</pre>
    </div>
    <div class="acc-actions" v-if="showActions">
      <button class="acc-btn acc-btn-primary" @click="$emit('confirm', item)">确认</button>
      <button class="acc-btn" @click="$emit('reject', item)">拒绝</button>
    </div>
  </div>
</template>

<script>
// 变更项卡片：从 workspace.vue 提取 buildItemSummary（解析 METADATA 生成人类可读摘要）
export default {
  name: 'ChangeItemCard',
  props: {
    item: { type: Object, required: true },
    // 是否显示确认/拒绝按钮（已确认/已拒绝项不显示）
    showActions: { type: Boolean, default: true }
  },
  data() {
    return { sqlOpen: false };
  },
  computed: {
    categoryLabel() {
      var map = {
        physical_table: '物理表',
        dataview: '视图',
        field: '字段',
        ui: 'UI配置',
        dict: '字典',
        filter: '过滤器',
        module: '模块',
        api: '接口',
        page: '页面',
        button: '按钮',
        menu: '菜单',
        permission: '权限',
        billflow: '审批流'
      };
      return map[this.item.CATEGORY] || this.item.CATEGORY || '-';
    },
    actionLabel() {
      var map = { CREATE: '新增', UPDATE: '修改', DELETE: '删除' };
      return map[this.item.ACTION] || this.item.ACTION || '-';
    },
    summary() {
      return this.buildItemSummary(this.item);
    },
    // 兼容 SQLCONTENT(DB 字段大写) 与 SQL 两种字段名
    sqlText() {
      return this.item.SQLCONTENT || this.item.SQL || '';
    }
  },
  methods: {
    // 解析变更项 METADATA 生成人类可读摘要 [{k,v}]（提取自 workspace.vue L273-355）
    buildItemSummary(it) {
      if (!it.METADATA) return null;
      var meta;
      try {
        meta = typeof it.METADATA === 'string' ? JSON.parse(it.METADATA) : it.METADATA;
      } catch (e) { return null }
      if (!meta) return null;
      var lines = [];
      var cat = it.CATEGORY;
      if (cat === 'physical_table') {
        var r = meta.resource || {};
        lines.push({ k: '资源', v: r.RESOURCENAME || r.TABLENAME || it.TARGET });
        lines.push({ k: '类型', v: r.RESOURCETYPE || 'TABLE' });
        var fields = meta.resfields || [];
        lines.push({ k: '字段', v: fields.length + ' 个' });
        lines.push({ k: '字段列表', v: fields.map(function(f) { return f.FIELDNAME + '(' + (f.FIELDTYPE || '') + (f.FIELDLENGTH ? '(' + f.FIELDLENGTH + ')' : '') + (f.ISKEY ? '/PK' : '') + ')' }).join(', ') });
      } else if (cat === 'dataview') {
        var r2 = meta.resource || {};
        lines.push({ k: '视图', v: r2.RESOURCENAME || it.TARGET });
        lines.push({ k: '关联表', v: r2.TABLERESOURCEID || '-' });
        var fields2 = meta.resfields || [];
        lines.push({ k: '字段', v: fields2.length + ' 个 (REFFIELDID 已链向 TBS)' });
        lines.push({ k: '字段列表', v: fields2.map(function(f) { return f.FIELDNAME }).join(', ') });
      } else if (cat === 'field') {
        var f = meta.resfield || {};
        lines.push({ k: '字段名', v: f.FIELDNAME || it.TARGET });
        lines.push({ k: '类型', v: (f.FIELDTYPE || '') + (f.FIELDLENGTH ? '(' + f.FIELDLENGTH + ')' : '') });
        lines.push({ k: '可空', v: f.NULLABLE ? '是' : '否' });
        if (f.ISKEY) lines.push({ k: '主键', v: '是 (GUID)' });
        if (f.REFRESOURCEID) lines.push({ k: '引用', v: '关联其他表 (REFRESOURCEID=' + (f.REFRESOURCEID || '').substring(0, 8) + '...)' });
      } else if (cat === 'ui') {
        var u = meta.resuipc || {};
        lines.push({ k: '控件', v: u.EDITTYPE || '-' });
        if (u.SELECTDATA) lines.push({ k: '数据源', v: u.SELECTDATA });
        if (u.LABELNAME) lines.push({ k: '标签', v: u.LABELNAME });
        if (u.LISTSORT) lines.push({ k: '列序', v: u.LISTSORT });
        if (u.EDITSORT) lines.push({ k: '表单序', v: u.EDITSORT });
        if (u.UPDATEFIELDS) lines.push({ k: '联动', v: u.UPDATEFIELDS });
      } else if (cat === 'dict') {
        var d = meta.dict || {};
        lines.push({ k: '字典名', v: d.DICTNAME || it.TARGET });
        var items = d.items || [];
        lines.push({ k: '字典项', v: items.length + ' 个' });
        lines.push({ k: '项列表', v: items.map(function(i) { return i.value + ':' + i.name }).join(', ') });
      } else if (cat === 'filter') {
        var f2 = meta.filter || {};
        lines.push({ k: '编码', v: f2.FILTERCODE || it.TARGET });
        if (f2.ORDERBY) lines.push({ k: '排序', v: f2.ORDERBY });
        lines.push({ k: '模板', v: 'NVelocity (已校验三条铁律)' });
      } else if (cat === 'module') {
        if (meta.sfc) {
          var s = meta.sfc;
          lines.push({ k: '类型', v: 'SFC 在线模块 (' + (s.FILETYPE || 'VUE') + ')' });
          lines.push({ k: '编码', v: s.TEMPLATECODE || it.TARGET });
          lines.push({ k: '名称', v: s.TEMPLATENAME || '-' });
          lines.push({ k: '路径', v: s.MODULEPATH || '-' });
          if (s.SOURCECODE_LEN) lines.push({ k: '源码', v: s.SOURCECODE_LEN + ' 字符' });
          if (s.DEPS) lines.push({ k: '依赖', v: s.DEPS });
        } else {
          var m = meta.module || {};
          lines.push({ k: '编码', v: m.MODULECODE || it.TARGET });
          lines.push({ k: '名称', v: m.MODULENAME || '-' });
        }
      } else if (cat === 'api') {
        var a = meta.moudleapi || meta.api || {};
        lines.push({ k: '编码', v: a.APICODE || it.TARGET });
        lines.push({ k: '类型', v: a.APITYPE || '-' });
        if (a.PATHNAME) lines.push({ k: '路径', v: a.PATHNAME });
        if (a.ACTIONCODE) lines.push({ k: '动作', v: a.ACTIONCODE });
        if (a.FILTERCODE) lines.push({ k: '过滤器', v: a.FILTERCODE });
      } else if (cat === 'page') {
        var pg = meta.page || {};
        lines.push({ k: '页面', v: (pg.MODULECODE || '') + '/' + (pg.PAGECODE || it.TARGET) });
        lines.push({ k: '名称', v: pg.PAGENAME || '-' });
        lines.push({ k: '类型', v: (pg.PAGETYPE || '-') + (pg.COMPONENTTYPE === 'sfc' ? ' (SFC)' : ' (通用模板)') });
        if (pg.QUERYAPICODE) lines.push({ k: '查询接口', v: pg.QUERYAPICODE });
        if (pg.OPENAPICODE) lines.push({ k: '打开接口', v: pg.OPENAPICODE });
        if (pg.SAVEAPICODE) lines.push({ k: '保存接口', v: pg.SAVEAPICODE });
        if (pg.SFCMODULEPATH) lines.push({ k: 'SFC路径', v: pg.SFCMODULEPATH });
      } else if (cat === 'button') {
        var b = meta.button || {};
        lines.push({ k: '按钮', v: b.BTNNAME || it.TARGET });
        lines.push({ k: '页面', v: (b.MODULECODE || '') + '/' + (b.PAGECODE || '-') });
        lines.push({ k: '区域', v: (b.BTNAREA || '-') + ' / ' + (b.BTNCODE || 'custom') });
        if (b.APICODE) lines.push({ k: '接口', v: b.APICODE });
        if (b.PERMCODE) lines.push({ k: '权限', v: b.PERMCODE });
        if (b.SHOWCOND) lines.push({ k: '显隐', v: b.SHOWCOND });
      } else if (cat === 'menu') {
        var f3 = meta.func || meta.menu || {};
        lines.push({ k: '编码', v: f3.FUNCCODE || it.TARGET });
        lines.push({ k: '名称', v: f3.FUNCNAME || '-' });
        if (f3.OUTERURL) lines.push({ k: '路由', v: f3.OUTERURL });
      } else if (cat === 'permission') {
        var fps = meta.funcpoints || [];
        lines.push({ k: '权限点', v: fps.length + ' 个' });
        lines.push({ k: '列表', v: fps.map(function(p) { return p.FUNCPOINTCODE || p.code }).join(', ') });
      } else if (cat === 'billflow') {
        lines.push({ k: '说明', v: '审批流配置 (STATE 字段 + A12/A14/A16 等 API)' });
      }
      return lines.length ? lines : null;
    }
  }
};
</script>

<style lang="less" scoped>
.ai-change-card {
  border: 1px solid #e4e7ed;
  border-radius: 6px;
  background: #fff;
  padding: 8px 12px;
  margin: 6px 0;
  font-size: 12px;
}
.acc-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
}
.acc-cat {
  background: #ecf5ff;
  color: #2d8cf0;
  padding: 1px 8px;
  border-radius: 3px;
  font-weight: 600;
}
.acc-action {
  padding: 1px 8px;
  border-radius: 3px;
  color: #fff;
  font-weight: 600;
}
.acc-action-create {
  background: #19be6b;
}
.acc-action-update {
  background: #ff9900;
}
.acc-action-delete {
  background: #ed4014;
}
.acc-target {
  color: #888;
  font-family: monospace;
}
.acc-summary {
  margin: 4px 0;
}
.acc-line {
  display: flex;
  gap: 8px;
  padding: 2px 0;
  line-height: 1.5;
}
.acc-line-k {
  color: #909399;
  flex-shrink: 0;
  width: 60px;
}
.acc-line-v {
  color: #333;
  word-break: break-all;
}
.acc-sql-toggle {
  color: #2d8cf0;
  cursor: pointer;
  user-select: none;
  margin: 4px 0;
  font-size: 11px;
}
.acc-sql-code {
  margin: 4px 0;
  padding: 8px;
  background: #1e1e1e;
  color: #d4d4d4;
  font-family: 'Monaco', 'Consolas', monospace;
  font-size: 11px;
  line-height: 1.5;
  max-height: 200px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
  border-radius: 4px;
}
.acc-actions {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}
.acc-btn {
  padding: 3px 12px;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  background: #fff;
  color: #606266;
  font-size: 12px;
  cursor: pointer;
  &:hover {
    border-color: #409eff;
    color: #409eff;
  }
}
.acc-btn-primary {
  background: #19be6b;
  border-color: #19be6b;
  color: #fff;
  &:hover {
    background: #47cb89;
    border-color: #47cb89;
    color: #fff;
  }
}
</style>
