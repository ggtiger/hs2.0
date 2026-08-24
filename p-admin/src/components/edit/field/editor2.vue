<template>
  <div class="editor">
    <Toolbar
            style="border-bottom: 1px solid #ccc"
            :editor="editor"
            :defaultConfig="toolbarConfig"
            :mode="mode"
        />
        <Editor
            v-model="html"
            :defaultConfig="editorConfig"
            @onChange="onChange"
            :mode="mode"
            @onCreated="onCreated"
        />
  </div>
</template>

<script>
import { Editor, Toolbar } from '@wangeditor/editor-for-vue';
export default {
  components: { Editor, Toolbar },
  name: 'rs-editor2',
  data() {
    return {
      editor: null,
      html: '',
      toolbarConfig: { },
      editorConfig: { placeholder: '',
        uploadImgShowBase64: true,
        MENU_CONF: {
        } },
      mode: 'default', // or 'simple'
    };
  },
  model: {
    prop: 'value',
    event: 'change',
  },
  props: {
    value: {
      type: String,
      default: '',
    },
    isClear: {
      type: Boolean,
      default: false,
    },
    menus: {
      type: Array,
      default: () => {
        return [
        ];
      },
    },
  },
  watch: {
    isClear(val) {
      // 触发清除文本域内容
      if (val) {
        this.editor.txt.clear();
        this.info_ = null;
      }
    },
    value: function(value) {
      this.info_ = value;
      if (value !== this.html) {
        this.html = this.info_;
      }
    },
    // value为编辑框输入的内容，这里我监听了一下值，当父组件调用得时候，如果给value赋值了，子组件将会显示父组件赋给的值
  },
  methods: {
    onCreated(editor) {
      this.editor = Object.seal(editor); // 一定要用 Object.seal() ，否则会报错
      // this.toolbarConfig.toolbarKeys = ['bold', 'underline', 'italic', 'through', 'code', 'sub', 'sup', 'clearStyle', 'color', 'bgColor', 'fontSize', 'fontFamily', 'indent', 'delIndent', 'justifyLeft', 'justifyRight', 'justifyCenter', 'justifyJustify', 'lineHeight', 'insertImage', 'divider', 'codeBlock', 'blockquote', 'headerSelect', 'todo', 'redo', 'undo', 'fullScreen', 'enter', 'bulletedList', 'numberedList', 'insertTable', 'uploadImage'];
      console.log('this.editor.getAllMenuKeys()', this.editor.getAllMenuKeys());

    },
    onChange() {
      this.$emit('input', this.html);
      this.$emit('change', this.html, this.value);
    }
  },
  mounted() {
  },
  beforeDestroy() {
    const editor = this.editor;
    if (editor == null) return;
    editor.destroy(); // 组件销毁时，及时销毁编辑器
  }
};
</script>

<style lang="less" scoped>
/deep/ table {
  border-spacing: 0;
}
.toolbar {
  border: 1px solid #ccc;
}
.text {
  border: 1px solid black;
  border-bottom: 0px solid #fff;
  height: auto;
  min-height: 250px;
}

.ueditor {
  margin-top: 10px;
  margin-bottom: 10px;
  /deep/ table {
    border-top: 1px solid windowtext;
    border-left: 1px solid windowtext;
  }
  /deep/ table td,
  table th {
    border-bottom: 1px solid #ccc;
    border-right: 1px solid #ccc;
  }
  /deep/ p {
    margin: 2px 0;
  }
}

/deep/.w-e-text {
  overflow-y: auto;
  overflow-x: hidden;
  padding: 0;
  min-height: 250px;
}
/deep/.w-e-text p {
  margin: 2px 0;
}
/deep/ .w-e-text table {
  border-top: 1px solid windowtext;
  border-left: 1px solid windowtext;
}
</style>
