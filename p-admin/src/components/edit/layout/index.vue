<template>
  <Row :style="itemStyle" :class="['rr-text-'+align,'rr-f'+size,{ 'rr-weight': weight }]">
    <Cell
      v-for="(item,index) in cols||currentCols"
      :key="index"
      :width="item"
      :style="{height:height}"
    >
      <slot :name="'cell'+index"></slot>
    </Cell>
  </Row>
</template>
<script>
export default {
  name: 'layout',
  props: {
    value: {},
    cell: {},
    cols: {
      type: [Object, Array],
    },
    align: {
      type: String,
      default: 'left',
    },
    size: {
      type: Number,
      default: 12,
    },
    weight: {
      type: Boolean,
      default: false, // false为细，true为粗
    },
    height: {
      type: String,
      default: '50%',
    },
    width: {
      type: String,
      default: '100%',
    },
  },
  data() {
    return {
      currentValue: this.value,
    };
  },
  computed: {
    cellWidth() {
      return 24 / this.cell;
    },
    currentCols() {
      let cols = this.cols || {};
      if (Object.keys(cols).length !== this.cell) {
        let v = parseInt(24 / this.cell, 10);
        let tc = {};
        for (let i = 0; i < this.cell; i++) {
          tc[i] = v;
        }
        return Object.values(tc);
      } else {
        return Object.values(this.cols);
      }
    },
    itemStyle(value) {
      var styles = {
        height: this.height,
        width: this.width,
      };
      return styles;
    },
  },
  watch: {
    value(v) {
      this.currentValue = v;
    },
    currentValue(v) {
      this.$emit('input', v);
    },
  },
  mounted() {},
  methods: {
    addListCom(v) {
      this.$emit('addListCom', this);
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/font.less';
.add {
  padding: 0 20px;
}
</style>
