<template>
  <div
    :style="{'width': width,height: height,lineHeight: height,fontSize:size+'px' }"
    style="display: inline-block;"
  >
    <template v-if="datas.length===1">
      <Checkbox
        v-if="fieldType==='checkBox'"
        :trueValue="1"
        :falseValue="0"
        v-model="currentValue"
      >{{datas[0].title}}</Checkbox>
      <Radio v-else v-model="currentValue" :datas="datas" @change="change">{{datas[0].title}}</Radio>
    </template>
    <template v-if="datas.length>1">
      <Checkbox v-if="fieldType==='checkBox'" v-model="currentValue" :trueValue="1" :falseValue="0"></Checkbox>
      <Radio v-else v-model="currentValue" @change="change"></Radio>
    </template>
  </div>
</template>
<script>
import itemLabel from '../label/index.vue';
export default {
  name: 'checkBox',
  components: {
    itemLabel,
  },
  props: {
    value: {},
    fieldType: {
      type: String,
      default: 'checkbox',
    },
    datas: Array,
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
    width: {
      type: String,
      default: '100%',
    },
    height: {
      type: String,
      default: '100%',
    },
  },
  computed: {},
  data() {
    return {
      currentValue: this.value,
    };
  },
  watch: {
    value(v) {
      debugger;
      this.currentValue = v;
    },
    currentValue(v) {
      this.$emit('input', v);
    },
  },
  methods: {
    getValue(field) {
      return this.model[field];
    },
    setValue(v) {
      this.$emit('input', v);
    },
    change() {
      console.log('change');
    },
  },
  mounted() {},
};
</script>
<style lang="less" scoped>
@import '~@/theme/font.less';
@import '~@/theme/index.less';
.rr-active {
  border-color: @primary-color;
  color: @primary-color;
}
textarea.inputNoborder,
.inputNoborder,
.inputNoborder.h-datetime,
.inputNoborder.h-select .h-select-show {
  background: none;
  border: none;
  border-bottom: 1px solid #333;
  border-radius: 0;
  padding: 0;
  line-height: 1;
  vertical-align: middle;
}
.inputNoborder.h-select /deep/ .h-select-show {
  min-height: 12px;
  line-height: 1;
  border: none;
  border-radius: 0;
  padding: 0;
  .h-select-placeholder {
    line-height: 1;
    height: 100%;
    margin-bottom: 0;
  }
}
.inputNoborder.h-datetime {
  line-height: 1;
  vertical-align: middle !important;
}
.h-form .h-form-item {
  padding: 0;
}
.h-form-item-label {
  padding: 0 !important;
}
</style>
