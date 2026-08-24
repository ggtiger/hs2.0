<template>
  <FormItem v-bind="formItemProps||{}" v-per="perCode">
    <input
      v-if="type==='text'"
      type="text"
      v-model="currentValue"
      v-bind="cellProps||{}"
      v-on="cellOn||{}"
    />
    <textarea
      v-else-if="type==='textarea'"
      type="textarea"
      v-model="currentValue"
      v-bind="cellProps||{}"
      v-on="cellOn||{}"
    />
    <NumberInput
      v-else-if="type==='number'"
      type="text"
      v-model="currentValue"
      v-bind="cellProps||{}"
      v-on="cellOn||{}"
    />
    <Select
      v-else-if="type==='select'"
      :value="currentValue"
      v-bind="cellProps||{}"
      @input="setValue($event)"
      v-on="cellOn||{}"
    ></Select>
    <DatePicker
      v-else-if="type==='datepicker'"
      :value="currentValue"
      v-bind="cellProps||{}"
      @input="setValue($event)"
      v-on="cellOn||{}"
    ></DatePicker>
    <h-switch
      v-else-if="type==='checkbox'"
      v-model.lazy="currentValue"
      :trueValue="1"
      :falseValue="0"
      v-bind="cellProps||{}"
    >
      <span slot="open">是</span>
      <span slot="close">否</span>
    </h-switch>
    <rs-editor
      v-else-if="type==='editor'"
      v-bind="cellProps||{}"
      :value="currentValue"
      @input="setValue($event)"
      v-on="cellOn||{}"
    ></rs-editor>
    <img v-else-if="type==='image'" :src="currentValue" style="width:200px"/>
    <!-- AutoComplete：通过 cellProps.option 渲染，updateFields 联动多字段 -->
    <AutoComplete
      v-else-if="type==='autocomplete'"
      v-model="currentObject"
      type="object"
      :option="(cellProps||{}).option"
      :disabled="(cellProps||{}).disabled"
      @change="emitObjectUpdate"
    >
      <template slot="item" slot-scope="{item}">
        <div>{{ item.value[titleNameOf] }}</div>
      </template>
    </AutoComplete>
    <!-- multiautocomplete：多选自动完成。subtable 模式绑定子表；field 模式存逗号id -->
    <AutoComplete
      v-else-if="type==='multiautocomplete'"
      v-model="multiSelectValue"
      :multiple="true"
      type="object"
      :option="(cellProps||{}).option"
      :disabled="(cellProps||{}).disabled"
      :placeholder="(cellProps||{}).placeholder"
    >
      <template slot="item" slot-scope="{item}">
        <div>{{ item.value[titleNameOf] }}</div>
      </template>
    </AutoComplete>
    <!-- TreePicker：树形选择，updateFields 联动多字段 -->
    <TreePicker
      v-else-if="type==='treepicker'"
      v-model="currentObject"
      type="object"
      :option="(cellProps||{}).option"
      :disabled="(cellProps||{}).disabled"
      @change="emitObjectUpdate"
    ></TreePicker>
    <!-- 文件上传：multifile 模式 v-model 为 [{id}] 单字段逗号id；否则单文件 updateFields 联动 -->
    <RsUploader
      v-else-if="type==='fileupload'"
      :type="multiFile ? 'files' : 'file'"
      data-type="file"
      :options="(cellProps||{}).uploaderOptions"
      :readonly="(cellProps||{}).disabled"
      v-model="fileValue"
    ></RsUploader>
    <!-- 图片上传：multifile 模式 v-model 为 [{id}] 单字段逗号id；否则单图片 updateFields 联动 -->
    <RsUploader
      v-else-if="type==='imageupload'"
      :type="multiFile ? 'images' : 'image'"
      data-type="file"
      :options="(cellProps||{}).uploaderOptions"
      :readonly="(cellProps||{}).disabled"
      v-model="fileValue"
    ></RsUploader>
    <!-- 文件上传+模板选择：rs-uploader-template -->
    <RsUploaderTemplate
      v-else-if="type==='fileuploadtpl'"
      :options="uploaderTplOptions"
      :template-type="uploaderTplConfig.templateType || ''"
      :module-code="uploaderTplConfig.moduleCode || ''"
      :show-select="uploaderTplConfig.showSelect !== false"
      :readonly="(cellProps||{}).disabled"
      v-model="fileValue"
    ></RsUploaderTemplate>
    <!-- code：点击弹出代码编辑器 -->
    <div v-else-if="type==='code'" class="rs-code-cell">
      <div
        class="rs-code-preview"
        :class="{ disabled: (cellProps||{}).disabled }"
        @click="openCodeEditor"
      >
        <pre>{{ currentValue || (cellProps||{}).placeholder || '点击编辑代码' }}</pre>
      </div>
      <rs-code-editor
        v-model="codeEditorVisible"
        :code="currentValue"
        :title="formItemProps && formItemProps.label || '代码编辑'"
        :language="(cellProps||{}).language || 'sql'"
        @confirm="onCodeConfirm"
      />
    </div>
    <template v-else-if="type==='slot'">
      <slot></slot>
      <span v-if="!hasSlotContent" class="rs-slot-placeholder">[自定义插槽]</span>
    </template>
    <label class="item-label" v-else>{{getValue(value)}}</label>
    <!-- 设计器工具条插槽：绝对定位，不影响表单布局 -->
    <slot name="designer-tools"></slot>
  </FormItem>
</template>
<script>
import heyui from 'heyui';
import RsEditor from './rs-editor';
import RsUploader from '@/components/rs-uploader';
import RsUploaderTemplate from '@/components/rs-uploader-template';
import RsCodeEditor from '@/components/rs-code-editor';
export default {
  name: 'rs-form-cell',
  props: {
    type: {
      type: String,
      default: 'text',
    },
    dict: {
      type: String,
    },
    formItemProps: { type: Object },
    cellProps: {
      type: Object,
    },
    cellOn: {
      type: Object,
    },
    value: {
      type: [String, Number],
    },
    // updateFields 格式：本地字段,远程字段;本地字段,远程字段
    // 用于 autocomplete/treepicker/fileupload/imageupload 联动写入多个字段
    updateFields: { type: String },
    // 整行数据（由 rs-form-edit 注入），用于构建 currentObject
    rowData: { type: Object },
  },
  components: { RsEditor, RsUploader, RsUploaderTemplate, RsCodeEditor },
  data() {
    return {
      currentValue: this.value,
      codeEditorVisible: false,
    };
  },
  watch: {
    value(v) {
      this.currentValue = v;
    },
    currentValue(v) {
      this.$emit('input', v);
    },
    // multiautocomplete 子表模式：值实际在子表，主表虚拟字段为空会导致必填校验误报。
    // 这里把选中项的 key 拼成逗号串同步到主表虚拟字段供校验（后台不保存该虚拟字段）。
    // 覆盖"用户选择"和"打开回填"两种时机。
    multiSelectValue: {
      handler(arr) {
        if (!this.isMultiSubtable) return;
        const ids = (arr || [])
          .map(o => o && o[this.multiSelKeyName])
          .filter(v => v !== undefined && v !== null && v !== '')
          .join(',');
        if (ids !== (this.currentValue || '')) {
          this.setValue(ids);
        }
      },
      immediate: true,
    },
  },
  computed: {
    hasSlotContent() {
      return !!(this.$slots.default && this.$slots.default.length);
    },
    // 权限码（来自 cellProps.perCode，空值 v-per 不隐藏）
    perCode() {
      return (this.cellProps || {}).perCode || '';
    },
    // 解析 updateFields 为 [{local, remote}] 数组
    fieldMappings() {
      if (!this.updateFields) return [];
      return this.updateFields.split(';')
        .filter(seg => seg && seg.indexOf(',') >= 0)
        .map(seg => {
          const [local, remote] = seg.split(',');
          return { local: (local || '').trim(), remote: (remote || '').trim() };
        });
    },
    // AutoComplete/TreePicker 显示字段（用于 item slot 渲染）
    titleNameOf() {
      const cp = this.cellProps || {};
      return cp.titleName || (cp.option && cp.option.titleName) || '';
    },
    // AutoComplete/TreePicker 双向绑定对象
    // get: 从 rowData 按映射构建 {远程字段: 本地字段值}
    // set: 拆出每个字段值，通过 update-fields 事件通知父组件写回
    currentObject: {
      get() {
        const mappings = this.fieldMappings;
        if (!mappings.length || !this.rowData) return null;
        const obj = {};
        let hasAny = false;
        mappings.forEach(m => {
          const v = this.rowData[m.local];
          obj[m.remote] = v;
          if (v !== '' && v != null) hasAny = true;
        });
        // 当前绑定字段的值以 titleName 为 key 补入，确保 AutoComplete 显示 label
        // 如绑定 EMPNAME，titleName=EMPNAME，补入 obj['EMPNAME'] = '王虎'
        const tn = this.titleNameOf;
        if (tn && this.currentValue != null && this.currentValue !== '') {
          if (!obj[tn]) {
            obj[tn] = this.currentValue;
          }
          hasAny = true;
        }
        return hasAny ? obj : null;
      },
      set(obj) {
        // 实际写回由 emitObjectUpdate 处理（@change 触发）
        this._pendingObject = obj;
      },
    },
    // 是否多文件模式：multifile=true 或 subtable 模式时用复数 type（files/images）
    multiFile() {
      const cp = this.cellProps || {};
      return !!(cp.uploaderOptions && cp.uploaderOptions.multifile) || !!cp.uploadSubtableConfig;
    },
    // fileuploadtpl 的 options：从 config 构造上传参数
    uploaderTplOptions() {
      const cfg = this.uploaderTplConfig;
      const opt = {};
      if (cfg.maxFileSize) opt.max_file_size = cfg.maxFileSize;
      if (cfg.multifile) opt.multifile = true;
      return opt;
    },
    // fileuploadtpl 配置对象（安全访问）
    uploaderTplConfig() {
      const cp = this.cellProps || {};
      return cp.uploaderTplConfig || {};
    },
    // 是否上传子表模式：uploaderOptions.mode=subtable 时每文件=子表行
    isFileSubtable() {
      const cp = this.cellProps || {};
      return (this.type === 'fileupload' || this.type === 'imageupload' || this.type === 'fileuploadtpl') &&
        cp.uploadSubtableConfig && !!cp.subtableAccessor;
    },
    // 文件/图片上传 v-model
    fileValue: {
      get() {
        // 子表模式：从子表行反向映射成文件对象数组
        if (this.isFileSubtable) {
          const acc = this.cellProps.subtableAccessor;
          return acc.getData().map(row => {
            const obj = {};
            acc.mappings.forEach(m => { obj[m.remote] = row[m.sub] });
            return obj;
          });
        }
        // 多文件模式：当前字段值（逗号id）→ [{id}]
        if (this.multiFile) {
          const ids = String(this.currentValue == null ? '' : this.currentValue).split(',').map(s => s.trim()).filter(Boolean);
          return ids.map(id => ({ id, name: '' }));
        }
        const mappings = this.fieldMappings;
        if (!mappings.length || !this.rowData) return null;
        // 约定：第一个映射是 id，第二个是 name
        const idField = mappings[0].local;
        const nameField = mappings[1] ? mappings[1].local : null;
        const id = this.rowData[idField];
        if (!id) return null;
        return { id, name: nameField ? this.rowData[nameField] : '' };
      },
      set(file) {
        // 子表模式：rebuild 子表行
        if (this.isFileSubtable) {
          this.cellProps.subtableAccessor.rebuild(file || []);
          return;
        }
        // 多文件模式：拼逗号id 写回当前字段
        if (this.multiFile) {
          const ids = (file || []).map(f => (f && (f.id || f)) || '').filter(Boolean);
          this.setValue(ids.join(','));
          return;
        }
        // RsUploader 在删除时会 emit input null
        const payload = {};
        if (!file) {
          const mappings = this.fieldMappings;
          mappings.forEach(m => { payload[m.local] = '' });
        } else {
          const mappings = this.fieldMappings;
          if (mappings[0]) payload[mappings[0].local] = file.id;
          if (mappings[1]) payload[mappings[1].local] = file.name;
        }
        this.$emit('update-fields', payload);
      },
    },
    // multiautocomplete 双向绑定（对象数组）
    // subtable 模式：get 从子表行反向映射成远程对象；set 重建子表
    // field 模式：get 由当前字段逗号id 构建对象；set 拼逗号id 写回
    isMultiSubtable() {
      const cp = this.cellProps || {};
      return this.type === 'multiautocomplete' &&
        cp.multSelConfig && cp.multSelConfig.mode === 'subtable' &&
        !!cp.subtableAccessor;
    },
    multiSelKeyName() {
      const cp = this.cellProps || {};
      return cp.keyName || 'ID';
    },
    multiSelectValue: {
      get() {
        const cp = this.cellProps || {};
        const cfg = cp.multSelConfig || {};
        const keyName = cp.keyName || 'ID';
        const titleName = cp.titleName || '';
        if (cfg.mode === 'subtable' && cp.subtableAccessor) {
          const acc = cp.subtableAccessor;
          return acc.getData().map(row => {
            const obj = {};
            acc.mappings.forEach(m => { obj[m.remote] = row[m.sub] });
            return obj;
          });
        }
        const ids = String(this.currentValue == null ? '' : this.currentValue).split(',').map(s => s.trim()).filter(Boolean);
        return ids.map(id => {
          const o = {};
          o[keyName] = id;
          if (titleName && titleName !== keyName) o[titleName] = id;
          return o;
        });
      },
      set(arr) {
        const cp = this.cellProps || {};
        const cfg = cp.multSelConfig || {};
        const keyName = cp.keyName || 'ID';
        if (cfg.mode === 'subtable' && cp.subtableAccessor) {
          cp.subtableAccessor.rebuild(arr || []);
          return;
        }
        const ids = (arr || []).map(o => o && o[keyName]).filter(v => v !== undefined && v !== null && v !== '');
        this.setValue(ids.join(','));
      },
    },
  },
  methods: {
    setValue(v) {
      this.currentValue = v;
      this.$emit('input', v);
    },
    // AutoComplete/TreePicker 选中后，把对象的远程字段值映射回本地字段
    // 清空时 obj 为 null，需要把所有映射的本地字段也清空
    emitObjectUpdate() {
      const obj = this._pendingObject;
      this._pendingObject = null;
      if (!obj) {
        // 清空：把 updateFields 映射的所有本地字段置空
        const payload = {};
        this.fieldMappings.forEach(m => {
          payload[m.local] = '';
        });
        this.$emit('update-fields', payload);
        return;
      }
      const payload = {};
      this.fieldMappings.forEach(m => {
        if (obj[m.remote] !== undefined) {
          payload[m.local] = obj[m.remote];
        }
      });
      if (Object.keys(payload).length) {
        this.$emit('update-fields', payload);
      }
    },
    getValue(v) {
      if (!this.dict) {
        return v;
      }
      return heyui.dictMapping(v, this.dict) || v;
    },
    openCodeEditor() {
      if ((this.cellProps || {}).disabled) return;
      this.codeEditorVisible = true;
    },
    onCodeConfirm(code) {
      this.setValue(code);
      this.codeEditorVisible = false;
    },
  },
  mounted() {},
};
</script>
<style lang="postcss" scoped>
.item-label {
  display: inline-block;
  text-align: right;
  font-size: 14px;
  line-height: 1;
  padding: 8.5px 15px 8.5px 0;
  -webkit-box-sizing: border-box;
  box-sizing: border-box;
}
.rs-slot-placeholder {
  display: inline-block;
  padding: 4px 10px;
  font-size: 12px;
  color: #999;
  background: #fafafa;
  border: 1px dashed #d9d9d9;
  border-radius: 3px;
}
.rs-code-cell {
  width: 100%;
}
.rs-code-preview {
  min-height: 32px;
  padding: 4px 8px;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  cursor: pointer;
  background: #fafafa;
  transition: border-color .2s;
  &:hover { border-color: #1d39c4; }
  &.disabled { cursor: not-allowed; background: #f5f5f5; color: #999; }
  pre {
    margin: 0;
    font-family: 'Courier New', Courier, monospace;
    font-size: 13px;
    white-space: pre-wrap;
    word-break: break-all;
    max-height: 120px;
    overflow-y: auto;
  }
}
</style>
