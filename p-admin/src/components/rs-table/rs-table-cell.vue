<template>
  <div :class="{ 'h-form-item-valid-error': this.formItemProps.required && this.canEdit && !currentValue }">
    <div v-if="type == 'text'">
      <label v-if="canEdit" style="width: 100%">
        <input style="width: 100%" type="text" v-model="currentValue" @blur="applyEdit" />
      </label>
      <label v-else>{{ currentValue }}</label>
    </div>

    <div v-else-if="type == 'textarea'">
      <label v-if="canEdit" style="width: 100%">
        <textarea style="width: 100%" v-model="currentValue" @blur="applyEdit" />
      </label>
      <label v-else>{{ currentValue }}</label>
    </div>
    <div v-else-if="type == 'file'">
      <label v-if="canEdit" style="width: 100%">
        <RsUploader type="file" data-type="file" v-model="fileValue" @blur="applyEdit"></RsUploader>
      </label>
      <label v-else>
        <a v-if="currentValue" class="link" :href="fileDownloadUrl" target="_blank">{{ fileDisplayName }}</a>
      </label>
    </div>
    <div v-else-if="type == 'fileupload'">
      <label v-if="canEdit" style="width: 100%">
        <RsUploader type="file" data-type="file" :options="(cellProps||{}).uploaderOptions" v-model="fileValue"></RsUploader>
      </label>
      <label v-else>
        <a v-if="currentValue" class="link" :href="fileDownloadUrl" target="_blank">{{ fileDisplayName }}</a>
      </label>
    </div>
    <div v-else-if="type == 'imageupload'">
      <label v-if="canEdit" style="width: 100%">
        <RsUploader type="image" data-type="file" :options="(cellProps||{}).uploaderOptions" v-model="fileValue"></RsUploader>
      </label>
      <label v-else>
        <a v-if="currentValue" class="link" :href="fileDownloadUrl" target="_blank">{{ fileDisplayName }}</a>
      </label>
    </div>
    <div v-else-if="type == 'fileuploadtpl'">
      <!-- 编辑态：上传 + 选入模版；非编辑态：只显示附件名称链接 -->
      <label v-if="canEdit" style="width: 100%">
        <RsUploaderTemplate
          style="width: 100%"
          :template-type="((cellProps||{}).uploaderTplConfig||{}).templateType || ''"
          :module-code="((cellProps||{}).uploaderTplConfig||{}).moduleCode || ''"
          :show-select="((cellProps||{}).uploaderTplConfig||{}).showSelect !== false"
          v-model="fileValue"
        ></RsUploaderTemplate>
      </label>
      <label v-else>
        <a v-if="currentValue" class="link" :href="fileDownloadUrl" target="_blank">{{ tplFileName || fileDisplayName }}</a>
      </label>
    </div>
    <div v-else-if="type == 'number'">
      <label v-if="canEdit" style="width: 100%">
        <input type="number" style="width: 100%" v-model="currentValue" @blur="applyEdit" />
      </label>
      <label v-else>{{ currentValue }}</label>
    </div>
    <div v-else-if="type == 'select'">
      <label v-if="canEdit" style="width: 100%">
        <Select v-model="currentValue" v-bind="selectCellProps" @change="applyEdit"></Select>
      </label>
      <label v-else>{{ selectDisplayTitle || currentValue }}</label>
    </div>
    <div v-else-if="type == 'datepicker'">
      <label v-if="canEdit" style="width: 100%">
        <DatePicker
          v-model="currentValue"
          v-bind="cellProps || {}"
          @change="applyEdit"
          v-on="cellOn || {}"
        ></DatePicker>
      </label>
      <label v-else>{{ currentValue }}</label>
    </div>
    <div v-else-if="type == 'autocomplete'">
      <label v-if="canEdit" style="width: 100%">
        <AutoComplete
          v-model="currentObject"
          type="object"
          v-bind="autoCompleteCellProps"
          v-on="cellOn"
          @change="applyEdit()"
        ></AutoComplete>
      </label>
      <label v-else>{{ currentValue }}</label>
    </div>
    <div v-else-if="type == 'checkbox'">
      <label v-if="canEdit" style="width: 100%">
        <Checkbox v-model="currentValue" @change="applyCheckboxEdit" :trueValue="1" :falseValue="0"></Checkbox>
      </label>
      <label v-else>{{ currentValue === 1 ? '√' : '' }}</label>
    </div>
    <div v-else-if="type == 'code'">
      <div
        class="rs-table-code-cell"
        :class="{ editing: canEdit }"
        @click="canEdit && openCodeEditor()"
      >
        <pre>{{ currentValue || (canEdit ? '点击编辑' : '') }}</pre>
      </div>
      <rs-code-editor
        v-model="codeEditorVisible"
        :code="currentValue"
        :title="formItemProps && formItemProps.label || '代码编辑'"
        :language="(cellProps||{}).language || 'sql'"
        @confirm="onCodeConfirm"
      />
    </div>
    <div v-else>{{ currentValue }}</div>
  </div>
</template>
<script>
import RsUploader from '@/components/rs-uploader';
import RsUploaderTemplate from '@/components/rs-uploader-template';
import RsCodeEditor from '@/components/rs-code-editor';
import heyui from 'heyui';
import { getUrl } from '@/api/urls';
import { httpGet } from '@/components/rs-onlyoffice-shared';

// Word 模版文件名缓存（FILEID → 模板名），非编辑态显示用
const tplNameCache = {};
let tplNamesLoading = false;
const tplNameWaiters = [];
function loadTplNames(vm) {
  if (tplNamesLoading) {
    return new Promise(resolve => tplNameWaiters.push(resolve));
  }
  tplNamesLoading = true;
  const token = (vm.$store && vm.$store.state['user'] && vm.$store.state['user'].access_token) || '';
  return httpGet(getUrl('url') + '/api/word-template/list', token).then(r => {
    (r.data || []).forEach(it => { tplNameCache[it.FILEID] = it.TEMPLATENAME || it.FILENAME || ''; });
  }).catch(() => {}).then(() => {
    tplNamesLoading = false;
    tplNameWaiters.splice(0).forEach(cb => cb());
  });
}
export default {
  name: 'rs-table-cell',
  props: {
    field: { Type: String },
    updateFields: { Type: String },
    data: { Type: Object },
    options: { Type: Array },
    rowIndex: { Type: Number },
    editInfo: { Type: Object },
    edit: { Type: Boolean, default: true },
    type: { Type: String, default: 'label' },
    cellProps: { Type: Object },
    cellOn: { Type: Object },
    value: { Type: Object },
    selectInfo: { Type: Object },
    formItemProps: { Type: Object },
  },
  data() {
    return {
      currentValue: this.value,
      selectValue: {},
      updateItem: null,
      codeEditorVisible: false,
      tplFileName: '',
    };
  },
  components: { RsUploader, RsCodeEditor, RsUploaderTemplate },
  computed: {
    canEdit() {
      return this.editInfo.editIndex === this.$parent.index && this.edit && this.editInfo.edit;
    },
    selectCellProps() {
      if (!this.cellProps) return {};
      if (typeof this.cellProps.getDatas === 'function') {
        const { getDatas, ...rest } = this.cellProps;
        try {
          return { ...rest, datas: getDatas(this.data) || [] };
        } catch (e) {
          return { ...rest, datas: [] };
        }
      }
      return this.cellProps;
    },
    selectDisplayTitle() {
      if (!this.currentValue) return '';
      // 优先从 datas 匹配
      const datas = this.selectCellProps.datas;
      if (datas && datas.length) {
        const matched = datas.find(d => d.key === this.currentValue);
        if (matched) return matched.title;
      }
      // dict 方式：用 HeyUI dictMapping 解析
      const dict = this.cellProps && this.cellProps.dict;
      if (dict) {
        const title = heyui.dictMapping(this.currentValue, dict);
        if (title) return title;
      }
      return '';
    },
    autoCompleteKeyName() {
      return (this.cellProps && this.cellProps.option && this.cellProps.option.keyName) || 'key';
    },
    autoCompleteTitleName() {
      return (this.cellProps && this.cellProps.option && this.cellProps.option.titleName) || 'title';
    },
    autoCompleteCellProps() {
      if (!this.cellProps) return {};
      const { option, ...rest } = this.cellProps;
      if (!option) return this.cellProps;
      const rowData = this.data;
      const wrappedOption = typeof option.loadData === 'function' ?
        { ...option, loadData: (text, callback) => option.loadData(text, callback, rowData) } :
        option;
      return { ...rest, option: wrappedOption };
    },
    fileDownloadUrl() {
      if (!this.currentValue) return '';
      return getUrl('upload') + this.currentValue;
    },
    fileDisplayName() {
      return this.data[this.field + '_NAME'] || this.currentValue || '';
    },
    currentObject: {
      get() {
        const titleName = this.autoCompleteTitleName;
        if (!this.updateFields) {
          const key = this.autoCompleteKeyName;
          return { [key]: this.currentValue, [titleName]: this.currentValue };
        }
        let aa = this.updateFields.split(';');
        let obj = {};
        let hasAny = false;
        let refObj = this.updateItem || this.data;
        aa.forEach((a) => {
          if (a) {
            const parts = a.split(',');
            const local = parts[0];
            const remote = parts[1];
            const v = refObj[local];
            obj[remote] = v;
            if (v !== '' && v != null) hasAny = true;
          }
        });
        // 当前绑定字段的值以 titleName 为 key 补入，确保 AutoComplete 显示 label
        if (titleName && this.currentValue != null && this.currentValue !== '') {
          if (!obj[titleName]) {
            obj[titleName] = this.currentValue;
          }
          hasAny = true;
        }
        return hasAny ? obj : null;
      },
      set(obj) {
        if (!obj) {
          // 清空：把 updateFields 映射的所有本地字段置空
          if (this.updateFields) {
            this.updateItem = {};
            let aa = this.updateFields.split(';');
            aa.forEach((a) => {
              if (a) this.updateItem[a.split(',')[0]] = '';
            });
          }
          this.currentValue = '';
          return;
        }
        if (!this.updateFields) {
          const key = this.autoCompleteKeyName;
          this.currentValue = obj[key];
          let updateItem = { [this.field]: obj[key] };
          this.$emit('on-apply-edit', { item: updateItem, index: this.$parent.index });
          return;
        }
        let aa = this.updateFields.split(';');
        this.updateItem = {};
        aa.forEach((a) => {
          if (a) {
            const parts = a.split(',');
            const local = parts[0];
            const remote = parts[1];
            this.updateItem[local] = obj[remote];
          }
        });
        this.currentValue = obj[this.autoCompleteTitleName] || '';
      },
    },
    fileValue: {
      get() {
        if (!this.currentValue) return null;
        const name = this.fileDisplayName || '';
        return { id: this.currentValue, name: name };
      },
      set(file) {
        if (!file) {
          if (this.updateFields) {
            this.updateItem = {};
            this.updateFields.split(';').forEach((a) => {
              if (a) this.updateItem[a.split(',')[0]] = '';
            });
          } else {
            this.$set(this.data, this.field + '_NAME', '');
          }
          this.currentValue = '';
          this.applyEdit('');
          return;
        }
        if (this.updateFields) {
          this.updateItem = {};
          this.updateFields.split(';').forEach((a) => {
            if (a) {
              const parts = a.split(',');
              const local = parts[0];
              const remote = parts[1];
              this.updateItem[local] = file[remote];
            }
          });
          // UPDATEFIELDS 模式：currentValue 是引用字段（如 CERTIFICATE），上传后设为文件名供显示
          this.currentValue = file.name;
        } else {
          this.$set(this.data, this.field + '_NAME', file.name);
          this.currentValue = file.id;
        }
        this.applyEdit(file.id);
      },
    },
  },
  watch: {
    // value prop变化时同步到currentValue（AI填报$set row后，:value="data[column.key]"传新值，需同步显示）
    value(v) {
      this.currentValue = v;
      this.tryLoadTplName(v);
    },
  },
  mounted() {
    this.tryLoadTplName(this.currentValue);
  },
  methods: {
    // fileuploadtpl 非编辑态显示：按 FILEID 反查 Word 模板名（模块级缓存）
    tryLoadTplName(id) {
      if (this.type !== 'fileuploadtpl' || !id) return;
      if (tplNameCache[id] !== undefined) {
        this.tplFileName = tplNameCache[id];
        return;
      }
      loadTplNames(this).then(() => {
        this.tplFileName = tplNameCache[id] || '';
      });
    },
    getFileValue() {
      return this.fileValue;
    },
    applyEdit(v) {
      let { updateItem } = this;
      if (!updateItem) {
        updateItem = { [this.field]: this.currentValue };
      }
      this.$emit('on-apply-edit', { item: updateItem, index: this.$parent.index });
    },
    applyCheckboxEdit(v) {
      let { updateItem } = this;
      if (!updateItem) {
        updateItem = { [this.field]: v };
      }
      this.$emit('on-apply-edit', { item: updateItem, index: this.$parent.index });
    },
    openCodeEditor() {
      this.codeEditorVisible = true;
    },
    onCodeConfirm(code) {
      this.currentValue = code;
      this.codeEditorVisible = false;
      let updateItem = { [this.field]: code };
      this.$emit('on-apply-edit', { item: updateItem, index: this.$parent.index });
    },
  },
};
</script>
<style lang="less" scoped="true">
.h-form.h-form-twocolumn .h-form-item {
  width: 100%;
}
.rs-table-code-cell {
  cursor: default;
  pre {
    margin: 0;
    font-family: 'Courier New', Courier, monospace;
    font-size: 12px;
    white-space: pre-wrap;
    word-break: break-all;
    max-height: 60px;
    overflow-y: auto;
  }
  &.editing {
    cursor: pointer;
    border: 1px solid #d9d9d9;
    border-radius: 3px;
    padding: 2px 6px;
    background: #fafafa;
    &:hover {
      border-color: #1d39c4;
    }
  }
}
</style>
