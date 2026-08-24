<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <slot></slot>
    <div class="h-panel-bar">
      <span class="h-panel-title">
        <Breadcrumb :datas="bcDatas"></Breadcrumb>
      </span>
    </div>

    <div class="h-panel-body rr-flex-1">
      <div class="rr-flex-col">
        <div style="height:auto;padding:10px 0px;" >
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
            ref="table"
            :checkbox="checkbox"
            @select="listSelect"
          >
            <slot name="table-action"></slot>
          </rs-table-list>
        </div>
      </div>
    </div>
  </div>
</template>
<script>
export default {
  name: 'list-t02',
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
  },
  data() {
    return {
      sumLabel: '',
      isQuery: 0,
    };
  },
  computed: {
    pageInfo: {
      get() {
        return {
          page: this.PageIndex,
          size: 1000,
          total: this.TotalCount,
          pagerSize: 1,
        };
      },
      set(v) {
        this.PageIndex = v.page;
        this.PageSize = v.size;
      },
    },
  },
  methods: {
    advQuery(param) {
      if (param !== 1) {
        this.PageIndex = 1;
      } else {
        param = {};
      }
      param = { ...param };
      param.sumFields = this.sumFields;
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
  watch: {},
  beforeCreate() {
    let { store } = this.$options.propsData;
    this.$options.computed = Object.assign(this.$options.computed, {
      ...store.mapDateTable('QRY', []),
      ...store.mapDateTable('QQRY', ['INPUT', 'TotalCount', 'PageSize', 'PageIndex', 'SumInfo', 'QSTATE']),
    });
    this.$store.commit(`${store.Constants.STORE_NAME}/initData`, {
      path: 'QQRY',
      data: [{ PageSize: 20, PageIndex: 1, INPUT: '' }],
    });
  },
  mounted() {
    this.$nextTick(() => {
      if (this.showQuery) {
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
}
</style>
