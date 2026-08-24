<template>
  <div class="rr-table-toolBar">
    <Pagination v-model="pagination" align="left" @change="change"></Pagination>
    <div class="rr-left">
      <slot name="left"></slot>
    </div>
    <div class="rr-right">
      <slot></slot>
    </div>
  </div>
</template>
<script>
export default {
  name: 'table-tool-bar',
  props: {
    value: {},
  },
  data() {
    return {
      pagination: {},
    };
  },
  watch: {
    value: {
      handler(val) {
        this.pagination = val;
      },
      immediate: true,
      deep: true,
    },
    pagination: {
      handler(val) {
        this.$emit('input', val);
      },
      immediate: true,
      deep: true,
    },
  },
  computed: {},
  mounted() {},
  methods: {
    change(value) {
      this.$emit('change', value);
    },
  },
};
</script>

<style lang="less" scoped>
.rr-title {
  text-align: center;
  h2 {
    margin: 30px 0;
  }
}
</style>
