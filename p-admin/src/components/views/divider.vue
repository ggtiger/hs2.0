<template>
  <div
    class="rr-divider"
    :class="['rr-divider-'+type,'rr-divider-'+orientation,{'is-dashed': dashed,}]"
  >
    <span class="rr-divider-title" :style="{fontSize:size+'px'}">
      <slot></slot>
    </span>
  </div>
</template>
<script>
export default {
  name: 'Divider',
  props: {
    type: {
      type: String,
      default: 'horizontal',
      validator(value) {
        return ['horizontal'].indexOf(value) > -1;
      },
    },
    orientation: {
      type: String,
      default: 'left',
      validator(value) {
        return ['left', 'right', 'center'].indexOf(value) > -1;
      },
    },
    dashed: {
      type: Boolean,
      default: false,
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
.rr-divider {
  margin: 10px 0;
  position: relative;
  background: #fff;
  padding: 0 10px;
  &-title {
    padding: 0 10px;
    background: #fff;
    position: relative;
    z-index: 9;
  }
  &-horizontal::before {
    position: absolute;
    height: 1px;
    width: 100%;
    background: #eee;
    top: 50%;
    left: 0;
    content: '';
  }
  &-left {
    text-align: left;
  }
  &-center {
    text-align: center;
  }
  &-right {
    text-align: right;
  }
}
</style>
