<template>
  <div class="rr-flex-row rr-ToolBar">
    <span class="rr-ToolBar-left"></span>
    <span :style="{fontSize:size+'px'}">{{title}}</span>
    <div class="rr-flex-1 rr-text-right">
      <slot name="right"></slot>
    </div>
  </div>
</template>
<script>
export default {
  name: 'ToolBar',
  props: {
    label: {
      type: String,
      default: '',
    },
    size: {
      type: Number,
      default: 14,
    },
  },
  data() {
    return {
      title: this.label,
    };
  },
  computed: {},
  mounted() {},
};
</script>

<style lang="less" scoped>
@import '~@/theme/index.less';
.rr-ToolBar {
  margin: 5px 0 5px;
  position: relative;
  padding-left: 10px;
  line-height: 31px;
  .rr-ToolBar-left {
    position: absolute;
    height: 16px;
    width: 3px;
    background: @primary-color;
    left: 0;
    top: 50%;
    margin-top: -8px;
  }
}
</style>
