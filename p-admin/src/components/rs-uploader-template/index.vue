<template>
  <div class="rs-uploader-template">
    <div class="rs-uploader-template-row">
      <RsUploader
        ref="uploader"
        type="file"
        data-type="file"
        :options="mergedOptions"
        :readonly="readonly"
        v-model="innerValue"
        @input="onUploaderInput"
      ></RsUploader>
      <Button
        v-if="showSelect && !readonly"
        @click.native="openSelector"
        style="margin-left:8px;flex-shrink:0;"
      >选入模版</Button>
    </div>

    <!-- 选入弹窗 -->
    <rs-modal ref="selectorModal" :width="800">
      <template-selector
        ref="selector"
        :template-type="templateType"
        :module-code="moduleCode"
        @on-select="onTemplateSelect"
      ></template-selector>
    </rs-modal>
  </div>
</template>
<script>
import RsUploader from '@/components/rs-uploader';
import TemplateSelector from './template-selector.vue';
import db from '@/api/db';

export default {
  name: 'rs-uploader-template',
  props: {
    value: {
      type: [Object, String, Array],
      default: function() { return null; },
    },
    options: {
      type: Object,
      default: function() { return {}; },
    },
    readonly: {
      type: Boolean,
      default: false,
    },
    templateType: {
      type: String,
      default: '',
    },
    moduleCode: {
      type: String,
      default: '',
    },
    showSelect: {
      type: Boolean,
      default: true,
    },
  },
  data() {
    return {
      innerValue: this.value,
    };
  },
  computed: {
    mergedOptions() {
      return Object.assign({ max_file_size: '10mb' }, this.options);
    },
  },
  watch: {
    value: {
      handler: function(val) {
        this.innerValue = val;
      },
      deep: true,
    },
  },
  methods: {
    onUploaderInput(val) {
      this.innerValue = val;
      this.$emit('input', val);
    },
    openSelector() {
      this.$refs.selectorModal.show();
    },
    onTemplateSelect(template) {
      var result = {
        id: template.FILEID,
        name: template.TEMPLATENAME,
      };
      if (template.FILEID) {
        result.url = db.getUrl('upload') + template.FILEID;
      }
      this.innerValue = result;
      this.$emit('input', result);
      this.$emit('select', Object.assign({}, template, { id: template.FILEID, name: template.TEMPLATENAME }));
    },
  },
  components: { RsUploader, TemplateSelector },
};
</script>
<style lang="less" scoped>
.rs-uploader-template-row {
  display: flex;
  align-items: center;
  width: 100%;

  /deep/ .h-uploader {
    flex: 1;
    min-width: 0;
  }
}
</style>
