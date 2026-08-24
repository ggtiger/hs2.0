<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <slot></slot>
    <div class="h-panel-bar rr-flex-row">
      <span class="h-panel-title" width="400px">
        <Breadcrumb :datas="bcDatas"></Breadcrumb>
      </span>
       <Row :space="9" class="rr-flex-1">
        <Cell width="12">
        <slot name="simple-query">
        </slot>
        </Cell>
       <Cell width="12" style="text-align: right;">
          <Search placeholder="请输入关键字" v-model="INPUT" style="width:300px" @search="query" />
          <Button v-if="dynamicQuery && dynamicQueryFields.length > 0" class="ml5" @click="$parent.showQuery = !$parent.showQuery">高级查询</Button>
          <slot name="header-action">
            <Button v-per="addper" color="primary" class="ml5 rr-flex-1" @click="listAction('add','')">新增{{title}}</Button>
          </slot>
       </Cell>
       </Row>
    </div>

    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <!-- 动态高级查询面板：rs-query-panel 从 scm 读 QUERYSORT>0 字段自动渲染 -->
        <!-- 显示条件与原 body-query 一致：showQuery 控制（默认隐藏，业务页按钮切换） -->
        <rs-query-panel
          v-if="dynamicQuery && showQuery && dynamicQueryFields.length > 0"
          :scm="dynamicScmName"
          :qqry-path="$QQRY"
          @query="doDynamicAdvQuery"
          @reset="onDynamicQueryReset"
        >
          <!-- 透传业务页按字段名的具名 slot（QUERYTYPE=slot 的字段由业务页覆盖） -->
          <template v-for="(_, name) in $slots" :slot="name">
            <slot :name="name"></slot>
          </template>
        </rs-query-panel>
        <!-- 保留原 body-query slot：dynamicQuery=false 时使用 -->
        <div style="height:auto;padding:10px 0px;" v-if="showQuery && !dynamicQuery">
          <slot name="body-query"></slot>
        </div>
        <div class="rr-flex-1">
          <rs-table-list
            :datas="QRY"
            :path="$QRY"
            @trdblclick="clickRow"
            @list-action="listAction"
            border
            :getProps="getTableProps"
            :column-config="resolvedColumns"
            ref="table"
            :checkbox="checkbox"
            @select="listSelect"
          >
            <slot name="table-action"></slot>
          </rs-table-list>
        </div>
        <table-tool-bar v-model="pageInfo" @change="changePage">
          <label v-if="TotalCount>0">{{sumLabel}}</label>
           <slot name="footer-action"></slot>
          <Button
            class="ml5"
            v-if="QRY.length>0 && expper!==false"
            v-per="expper"
            color="primary"
            @click.native="exportExcel"
          >导出</Button>
        </table-tool-bar>
      </div>
    </div>
  </div>
</template>
<script>
import heyui from 'heyui';
import { getUrl } from '@/api/urls';
import RsQueryPanel from '@/components/rs-query-panel';
export default {
  name: 'list-t01',
  components: { RsQueryPanel },
  provide() {
    // 把业务页（list-t01 的父）提供给 rs-table-list，用于读取 ISxxx 显隐 computed
    return { visibilityHost: this.$parent };
  },
  props: {
    bcDatas: {
      Type: [Object, Array],
    },
    title: {
      Type: String,
    },
    addper: {
      Type: String,
    },
    expper: {
      Type: String,
    },
    path: {
      Type: Object,
    },
    store: {
      Type: Object,
    },
    showQuery: {
      Type: Boolean,
      default: false,
    },
    sumFields: {
      Type: String,
      default: '',
    },
    checkbox: {
      Type: Boolean,
      default: false,
    },
    getTableProps: { Type: Function },
    queryAction: {
      Type: String,
      default: 'query',
    },
    advQueryAction: {
      Type: String,
      default: 'advQuery',
    },
    // 高级查询 APICODE：若指定，dispatch advQuery action 时附带 APICODE 参数，让 Store03 按 APICODE 查找 API 行
    advQueryAPICODE: {
      Type: String,
      default: '',
    },
    // 是否启用动态高级查询面板（从 scm 读 QUERYSORT>0 字段自动渲染）
    dynamicQuery: {
      Type: Boolean,
      default: false,
    },
    // 可选：显式指定动态查询字段来自哪个 scm 资源名；不传则用 QRY/QQRY 的 scm
    dynamicQueryScm: {
      Type: String,
      default: '',
    },
    // 可配置 PATH 名称，默认 QRY/QQRY
    qryPath: {
      Type: String,
      default: 'QRY',
    },
    qqryPath: {
      Type: String,
      default: 'QQRY',
    },
    // 可选：直接传入列配置（优先于 path.scm 读取）
    columnConfig: { Type: Array },
    // 列级覆盖: { CUSTNAME: { title: '客户', width: 200, dict: 'D0701' } }
    columnOverrides: { Type: Object, default: () => ({}) },
  },
  data() {
    return {
      sumLabel: '',
      isQuery: 0,
      // 从 scm 读出的查询字段配置
      dynamicQueryFields: [],
      // 响应式缓存，{ FIELD_NAME: value }
      queryValues: {},
    };
  },
  computed: {
    pageInfo: {
      get() {
        return {
          page: this.PageIndex,
          size: this.PageSize,
          total: this.TotalCount,
          pagerSize: 1,
        };
      },
      set(v) {
        this.PageIndex = v.page;
        this.PageSize = v.size;
      },
    },
    // 动态查询使用的 scm 资源名：优先 dynamicQueryScm，否则从 QRY DataTable 取
    dynamicScmName() {
      if (this.dynamicQueryScm) return this.dynamicQueryScm;
      const qry = this.$QRY;
      if (qry && qry.scm) return qry.scm;
      return '';
    },
    dynamicScmData() {
      if (!this.dynamicScmName) return [];
      return this.$store.state.app.scms[this.dynamicScmName] || [];
    },
    // 解析最终列配置：columnConfig 优先，否则从 scm 生成，再应用 overrides
    resolvedColumns() {
      var cols = this.columnConfig;
      if (!cols) {
        // 未传 columnConfig 时返回 undefined，让 rs-table-list 自己从 scm 生成
        if (!this.columnOverrides || !Object.keys(this.columnOverrides).length) {
          return undefined;
        }
        // 有 overrides 但没 columnConfig：从 rs-table-list 已生成的 columns 取
        if (this.$refs.table && this.$refs.table.columns) {
          cols = this.$refs.table.columns;
        }
      }
      if (!cols) return undefined;
      return this._applyColumnOverrides(cols);
    },
  },
  methods: {
    // 应用列级覆盖
    // override 属性: title/width/minWidth/maxWidth/dict/datas/visibleIf/perCode/type/align/fixed
    _applyColumnOverrides(columns) {
      if (!this.columnOverrides || !Object.keys(this.columnOverrides).length) return columns;
      var ov = this.columnOverrides;
      return columns.map(function(col) {
        var key = col.key || col.prop;
        var o = ov[key];
        if (!o) return col;
        var merged = Object.assign({}, col);
        if (o.title !== undefined) merged.title = o.title;
        if (o.width !== undefined) merged.width = o.width;
        if (o.minWidth !== undefined) merged.minWidth = o.minWidth;
        if (o.maxWidth !== undefined) merged.maxWidth = o.maxWidth;
        if (o.dict !== undefined) merged.dict = o.dict;
        if (o.datas !== undefined) merged.datas = o.datas;
        if (o.visibleIf !== undefined) merged.visibleIf = o.visibleIf;
        if (o.perCode !== undefined) merged.perCode = o.perCode;
        if (o.type !== undefined) merged.type = o.type;
        if (o.align !== undefined) merged.align = o.align;
        if (o.fixed !== undefined) merged.fixed = o.fixed;
        if (o.updateFields !== undefined) merged.updateFields = o.updateFields;
        if (o.selectData !== undefined) merged.selectData = o.selectData;
        // dict + items: 字典筛选项
        if (o.dict && o.items) {
          // 需要 store 访问，跳过（在调用方用 datas 代替）
        }
        // 任意其他属性
        Object.keys(o).forEach(function(k) {
          if (['title', 'width', 'minWidth', 'maxWidth', 'dict', 'datas', 'visibleIf', 'perCode', 'type', 'align', 'fixed', 'updateFields', 'selectData', 'items'].indexOf(k) < 0) {
            merged[k] = o[k];
          }
        });
        return merged;
      });
    },

    query(param) {
      this.$parent.showQuery = false;
      if (param !== 1) {
        this.PageIndex = 1;
      } else {
        param = {};
      }
      let params = this.$route.meta.params || {};
      if (params.QSTATE) {
        this.QSTATE = params.QSTATE;
      }
      param = { ...param, ...params };
      param.sumFields = this.sumFields;
      // 传入 qryPath 让 Store03 query action 写入正确的路径
      if (this.qryPath && this.qryPath !== 'QRY') {
        param.qryPath = this.qryPath;
      }
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/${this.queryAction}`,
        param: param,
        timeOut: 0,
        successCall: () => {
          this.getSumLabel();
          this.$route.meta.params = {};
          // this.QSTATE = '';
        },
      });
    },
    advQuery(param) {
      if (param !== 1) {
        this.PageIndex = 1;
      } else {
        param = {};
      }
      param = { ...param };
      param.sumFields = this.sumFields;
      // 传入 qryPath 让 Store03 advQuery action 写入正确的路径
      if (this.qryPath && this.qryPath !== 'QRY') {
        param.qryPath = this.qryPath;
      }
      // 传入 advQueryAPICODE，让 Store03 按 APICODE 查找 API 行（而非 ACTIONCODE）
      if (this.advQueryAPICODE) {
        param.APICODE = this.advQueryAPICODE;
      }
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/${this.advQueryAction}`,
        param: param,
        timeOut: 0,
        successCall: () => {
          this.getSumLabel();
        },
      });
    },
    getSumLabel() {
      let label = '';
      let columns = this.$refs.table.columns;
      if (this.sumFields) {
        this.sumFields.split(',').map(f => {
          let cc = columns.find(c => {
            return c.key === f;
          });
          if (cc) {
            label += `${cc.title}合计:${this.SumInfo[f] || 0} `;
          }
        });
      }
      this.sumLabel = label;
    },
    changePage(pageInfo) {
      this.PageIndex = pageInfo.page;
      this.PageSize = pageInfo.size;
      if (this.dynamicQuery && this.showQuery) {
        this.advQuery(1);
      } else {
        this.query(1);
      }
    },
    // 解析 scm 字段的 SELECTDATA 为 [{key,title}] 数组
    parseQuerySelectDatas(raw) {
      if (!raw) return [];
      try {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) return parsed;
      } catch (e) {}
      if (typeof raw === 'string' && raw.indexOf(':') > 0) {
        return raw.split(',').map(seg => {
          const [k, title] = seg.split(':');
          return { key: (k || '').trim(), title: (title || k || '').trim() };
        });
      }
      return [];
    },
    // rs-query-panel 已在 emit query 前把值同步到 QQRY DataTable，这里直接触发查询
    doDynamicAdvQuery() {
      this.advQuery();
    },
    resetDynamicQuery() {
      this.dynamicQueryFields.forEach(f => {
        const k = f.RESFIELDNAME || f.FIELDNAME;
        if (!k) return;
        const mode = f.QUERYMODE || ((f.QUERYTYPE || f.EDITTYPE) === 'daterange' ? 'range' : '');
        let def = '';
        if (mode === 'range' || (f.QUERYTYPE || f.EDITTYPE) === 'daterange') def = { start: '', end: '' };
        else if (mode === 'in') def = [];
        this.$set(this.queryValues, k, def);
      });
    },
    // rs-query-panel 重置后：同步清空 QQRY（组件内部已重置 queryValues）
    onDynamicQueryReset() {
      if (this.$QQRY) {
        this.dynamicQueryFields.forEach(f => {
          const k = f.RESFIELDNAME || f.FIELDNAME;
          if (k) this.$QQRY.setValue(k, '');
        });
      }
    },
    exportExcel() {
      let columns = this.$refs.table.columns;
      columns.map(c => {
        if (c.dict) {
          c.dictData = heyui.getDict(c.dict);
        }
      });
      // 导出走查询 action 的 isExport=1 分支，返回文件路径后用 upload URL 拼装下载链接
      const actionName = `${this.store.Constants.STORE_NAME}/${this.showQuery ? this.advQueryAction : this.queryAction}`;
      this.$callAction({
        action: actionName,
        param: { isExport: 1, columns },
        successCall: (ret) => {
          window.open(`${getUrl('upload')}${ret}`, '_black');
        },
      });
    },
    clickRow(row) {
      this.$emit('list-click-row', row);
    },
    listAction(type, param) {
      this.$emit('list-action', type, param);
    },
    listSelect(checks) {
      this.$emit('list-select', checks);
    },
  },
  watch: {
    // dynamicScmName 依赖 $QRY.scm, 首次打开时 $QRY.scm 可能未就绪, 变化后补加载 scm
    dynamicScmName: {
      immediate: true,
      async handler(name, oldName) {
        if (this.dynamicQuery && name && name !== oldName && !this.$store.state.app.scms[name]) {
          // eslint-disable-next-line no-restricted-syntax
          await this.$store.dispatch('app/initScms', [name]);
        }
      },
    },
    // scm 异步加载，加载完成后初始化查询字段与 queryValues
    dynamicScmData: {
      immediate: true,
      handler(scm) {
        if (!this.dynamicQuery || !scm || !scm.length) return;
        const fields = scm
          .filter(f => +f.QUERYSORT > 0)
          .sort((a, b) => +a.QUERYSORT - +b.QUERYSORT);
        this.dynamicQueryFields = fields;
        fields.forEach(f => {
          const k = f.RESFIELDNAME || f.FIELDNAME;
          if (!k) return;
          if (this.queryValues[k] === undefined) {
            const mode = f.QUERYMODE || ((f.QUERYTYPE || f.EDITTYPE) === 'daterange' ? 'range' : '');
            let def = '';
            if (mode === 'range' || (f.QUERYTYPE || f.EDITTYPE) === 'daterange') def = { start: '', end: '' };
            else if (mode === 'in') def = [];
            this.$set(this.queryValues, k, def);
          }
        });
      },
    },
  },
  beforeCreate() {
    let { store, qryPath, qqryPath } = this.$options.propsData;
    qryPath = qryPath || 'QRY';
    qqryPath = qqryPath || 'QQRY';
    // 动态注册 mapDateTable（computed 名由 path 决定）
    var computedMap = Object.assign({},
      store.mapDateTable(qryPath, []),
      store.mapDateTable(qqryPath, ['INPUT', 'TotalCount', 'PageSize', 'PageIndex', 'SumInfo', 'QSTATE']),
    );
    // path 非默认时，添加固定名称代理让模板中 QRY/$QRY/$QQRY 不受影响
    if (qryPath !== 'QRY') {
      computedMap.QRY = function() { return this[qryPath] };
      computedMap['$QRY'] = function() { return this['$' + qryPath] };
    }
    if (qqryPath !== 'QQRY') {
      computedMap['$QQRY'] = function() { return this['$' + qqryPath] };
    }
    this.$options.computed = Object.assign(this.$options.computed, computedMap);
    this.$store.commit(`${store.Constants.STORE_NAME}/initData`, {
      path: qqryPath,
      data: [{ PageSize: 20, PageIndex: 1, INPUT: '' }],
    });
  },
  async mounted() {
    // dynamicQuery 模式下确保 scm 已加载（watch immediate 在初次为空时会自动重试）
    if (this.dynamicQuery && this.dynamicScmName) {
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initScms', [this.dynamicScmName]);
    }
    this.$nextTick(() => {
      console.log('list-t01 mounted', this.dynamicQuery, this.showQuery);
      if (this.dynamicQuery && this.showQuery) {
        this.advQuery();
      } else {
        this.$route.meta.params = this.$route.params;
        this.query();
      }
    });
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
/deep/ .h-table {
  max-height: calc(100% - 10px);
  height: calc(100% - 10px);
}
/deep/ .h-table-container {
  max-height: calc(100% - 40px);
  overflow-y: auto;
  height: calc(100% - 10px);
}
/deep/ .h-table-body {
  overflow-y: auto;
}
/deep/ .h-panel-body {
  padding: 10px 20px;
}
/deep/ .h-page {
  height: 32px;
}
/deep/ .h-panel-bar {
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
  padding: 10px 20px;
  display: flex;
  align-items: center;
}
/deep/ .h-breadcrumb a {
  color: @primary-color;
}
/deep/ .h-btn-primary {
  background-color: @primary-color;
}
</style>
