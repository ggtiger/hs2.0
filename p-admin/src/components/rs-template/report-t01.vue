<template>
  <div class="h-panel h-panel-no-border rr-flex-col">
    <slot></slot>
    <div class="h-panel-bar">
      <span class="h-panel-title">
        <Breadcrumb :datas="bcDatas"></Breadcrumb>
      </span>
      <div class="h-panel-right">
        <slot name="query"></slot>
      </div>
    </div>

    <div class="h-panel-body rr-flex-1">
      <div class="rr-report-table">
        <Table border stripe :datas="datas" :columns="columns">
          <div slot="empty">自定义提醒：暂时无数据</div>
        </Table>
      </div>
      <div class="rr-report-chart">
        <chart ref="charts" width="100%" height="300px" :options="options" :initOption="initOption"></chart>
      </div>
    </div>
  </div>
</template>
<script>
// import heyui from 'heyui';
// import db from '@/api/db';
import chart from '@/components/echarts/chart';
export default {
  name: 'report-t01',
  components: { chart },
  props: {
    bcDatas: {
      Type: [Object, Array],
    },
    datas: {
      Type: [Object, Array],
    },
    columns: {
      Type: [Object, Array],
    },
    options: {
      Type: [Object, Array],
    },
    initOption: {
      Type: [Object, Array],
    },
  },
  data() {
    return {
      serchStarDate: '',
      serchEndDate: '',
    };
  },
  computed: {},
  methods: {
    query() {
      let param = {
        serchStarDate: this.serchStarDate,
        serchEndDate: this.serchEndDate,
      };
      this.$emit('query', param);
      this.$refs.charts.init();
    },
  },
  watch: {},
  beforeCreate() {},
  mounted() {
    this.$nextTick(() => {
      this.query();
    });
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
/deep/ .h-table {
  max-height: calc(100% - 10px);
  height: calc(100% - 10px);
  /deep/ .h-table-container {
    max-height: calc(100% - 40px);
    overflow-y: auto;
    height: calc(100% - 10px);
  }
}
/deep/ .h-panel-body {
  padding: 10px 20px;
}
/deep/ .h-page {
  height: 32px;
}
/deep/ .h-panel-bar {
  background: @table-header-bg;
  border-bottom: 1px solid #e8e8e8;
}
</style>
