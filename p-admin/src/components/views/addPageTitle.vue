<template>
  <div class="rr-title">
    <Row :space="10">
      <Cell width="8" v-if="data.danjuhao">单据号：{{data.danjuhao}}</Cell>
      <Cell width="8" v-if="data.tijiaoren">提交人：{{data.tijiaoren}}</Cell>
      <Cell width="8" v-if="data.tijiaoshijian">提交时间：{{data.tijiaoshijian}}</Cell>
    </Row>
    <h2>{{title}}</h2>
  </div>
</template>
<script>
export default {
  name: 'add-page-title',
  props: {
    title: {
      type: String,
      default: 'wodezhuye',
    },
    data: {
      type: Object,
    },
  },
  data() {
    return {};
  },
  computed: {},
  mounted() {},
  methods: {},
};
</script>

<style lang="less" scoped>
@import '~heyui/themes/index.less';
.rr-title {
  text-align: center;
  h2 {
    margin: 30px 0;
  }
}
</style>
