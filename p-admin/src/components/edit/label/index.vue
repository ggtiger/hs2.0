<template>
  <span
    :class="['rr-text-'+align,'rr-f'+size,{ 'rr-weight': weight }]"
    style="display: inline-block;"
    :style="{'width': width,'height': height}"
  >
    <label @dblclick="dblclick" v-if="!isEdit||!inLayout">{{label}}</label>
    <input
      v-model="label"
      :class="['rr-text-'+align,'rr-f'+size,{ 'rr-weight': weight }]"
      v-if="(isEdit||label==='')&&inLayout"
      @blur="success()"
    />
  </span>
</template>
<script>
export default {
  name: 'label',
  props: {
    label: {
      type: String,
      default: '标签',
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
      default: '100%',
    },
    width: {
      type: String,
      default: 'auto',
    },
    inLayout: {
      type: Boolean,
      default: true,
    },
  },
  data() {
    return {
      isEdit: false,
    };
  },
  computed: {
    itemStyle() {
      const style = {
        height: this.height,
        width: this.width,
      };
      return style;
    },
  },
  mounted() {},
  methods: {
    dblclick() {
      this.isEdit = true;
    },
    success() {
      this.isEdit = false;
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/font.less';
input {
  height: 100%;
  line-height: 100%;
  width: 100%;
  background: none;
  border: none;
  outline: none;
  padding: 0;
}
</style>
