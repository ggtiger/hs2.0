<template>
  <div :style="{'width': width,height: height,}" style="display: inline-block;">
    <div class="rr-flex-row">
      <itemLabel slot="label" v-if="fieldType!=='editor'" v-bind="labelProps" :inLayout="inLayout"></itemLabel>
      <div class="rr-flex-1">
        <textarea
          class="inputNoborder"
          v-if="fieldType==='text'&&textMore"
          type="text"
          rows="1"
          v-model="currentValue"
          :placeholder="placeholder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          v-on="cellOn||{}"
        ></textarea>
        <input
          class="inputNoborder"
          v-if="fieldType==='text'&&!textMore&&this.isInInput"
          type="text"
          v-model="currentValue"
          :placeholder="placeholder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          v-on="cellOn||{}"
          @blur="doInputBlur"
        />
        <AutoComplete
          type="object"
          v-else-if="fieldType==='autocomplete'"
          v-model="currentValue"
          :placeholder="placeholder"
          v-bind="fieldProps||{}"
          class="inputNoborder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          @input="setValue($event)"
          v-on="cellOn||{}"
        ></AutoComplete>
        <Select
          v-else-if="fieldType==='select'&&path"
          v-model="currentValue"
          :placeholder="placeholder"
          v-bind="fieldProps||{}"
          class="inputNoborder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          @input="setValue($event)"
          v-on="cellOn||{}"
          :dict="path"
        ></Select>
        <label
          v-else-if="fieldType==='select'&&!path"
          class="inputNoborder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;vertical-align: top;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
        >{{currentValue}}</label>

        <DatePicker
          v-else-if="fieldType==='date'"
          class="inputNoborder"
          v-bind="fieldProps||{}"
          v-model="currentValue"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;vertical-align: top;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          :no-border="true"
          :placeholder="placeholder"
          :format="format"
        ></DatePicker>
        <rs-editor
          v-else-if="fieldType==='editor'"
          class="inputEditorBorder"
          v-bind="fieldProps||{}"
          v-model="currentValue"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;vertical-align: top;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
          :no-border="true"
          :placeholder="placeholder"
          :format="format"
          :menus="[]"
        ></rs-editor>
        <label
          @click="doInput"
          v-if="fieldType==='text'&&!textMore&&!this.isInInput"
          class="inputNoborder"
          :class="['rr-text-'+fieldProps.align,'rr-f'+fieldProps.size,{ 'rr-weight': fieldProps.weight }]"
          style="display: inline-block;vertical-align: top;"
          :style="{'width': fieldProps.width,height: fieldProps.height}"
        >{{currentValue}}</label>
      </div>
    </div>
  </div>
</template>
<script>
import itemLabel from '../label/index.vue';
import RsEditor from './editor2.vue';
export default {
  name: 'field',
  components: {
    itemLabel,
    RsEditor,
  },
  props: {
    value: {},
    textMore: {
      type: Boolean,
      default: false,
    },
    fieldType: {
      type: String,
      default: 'text',
    },
    label: {
      type: String,
      default: '标签',
    },
    labelProps: Object,
    fieldProps: Object,
    placeholder: {
      type: String,
      default: '',
    },
    cellOn: {
      Type: Object,
    },
    width: {
      type: String,
      default: '100%',
    },
    data: {},
    path: {
      type: String,
      default: '',
    },
    height: {
      type: String,
      default: '100%',
    },
    inLayout: {
      type: Boolean,
      default: true,
    },
  },
  computed: {
    itemStyle() {
      const style = {
        height: this.height,
        width: this.width,
        lineHeight: this.height,
      };
      return style;
    },
    fieldStyle() {
      const style = {
        height: this.fieldProps.height,
        width: this.fieldProps.width,
        lineHeight: this.fieldProps.height,
      };
      return style;
    },
  },
  data() {
    return {
      currentValue: this.value,
      format: 'YYYY-MM-DD',
      isInInput: false,
    };
  },
  watch: {
    value(v) {
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
    doInput() {
      if (!this.isInInput) {
        this.isInInput = true;
      }
    },
    doInputBlur() {
      debugger;
      this.isInInput = false;
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
input.inputNoborder,
.inputNoborder,
.inputNoborder.h-datetime {
  background: none;
  border: none;
  border-bottom: 1px solid #333;
  border-radius: 0;
  padding: 0;
  line-height: 1;
}
.inputNoborder.h-autocomplete {
  border-bottom: none !important;
}
.inputNoborder.h-autocomplete /deep/ .h-autocomplete-show {
  min-height: 12px;
  height: 100%;
  overflow: hidden;
  border: none;
  border-bottom: 1px solid #333;
  border-radius: 0;
  font-size: inherit;
  line-height: 1;
  padding: 0;
  .h-input {
    height: 100%;
    padding: 0;
    font-size: inherit;
  }
}

.inputNoborder.h-select {
  border-bottom: none !important;
}
.inputNoborder.h-select /deep/ .h-select-show {
  min-height: 12px;
  height: 100%;
  overflow: hidden;
  border: none;
  border-bottom: 1px solid #333;
  border-radius: 0;
  font-size: inherit;
  line-height: 1;
  padding: 0;
  .h-input {
    height: 100%;
    padding: 0;
    font-size: inherit;
  }
}
.inputNoborder.h-datetime {
  line-height: 1;
  vertical-align: middle !important;
  /deep/ .h-datetime-show {
    height: 100%;
  }
}
.h-form .h-form-item {
  padding: 0;
}
.h-form-item-label {
  padding: 0 !important;
}

.inputEditorBorder{
  border: 1px solid #333;
}
</style>
