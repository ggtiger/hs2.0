<template>
  <div class="sfc-code-editor-wrap">
    <div ref="editor" class="sfc-code-editor-body"></div>
  </div>
</template>
<script>
import CodeMirror from 'codemirror';
import 'codemirror/lib/codemirror.css';
import 'codemirror/theme/idea.css';
import 'codemirror/mode/vue/vue.js';
import 'codemirror/mode/javascript/javascript.js';
import 'codemirror/mode/htmlmixed/htmlmixed.js';
import 'codemirror/mode/css/css.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/sql/sql.js';
import 'codemirror/addon/edit/closetag.js';
import 'codemirror/addon/edit/matchbrackets.js';
import 'codemirror/addon/edit/closebrackets.js';
import 'codemirror/addon/selection/active-line.js';
import 'codemirror/addon/hint/show-hint.js';
import 'codemirror/addon/hint/show-hint.css';
import 'codemirror/addon/hint/javascript-hint.js';
import 'codemirror/addon/hint/html-hint.js';

// fileType → CodeMirror mode (VUE/JS 是 SFC 组件, CSHARP/SQL 是 API 脚本/SQL 模板)
function modeOf(fileType) {
  if (fileType === 'JS') return 'javascript';
  if (fileType === 'CSHARP') return 'text/x-csharp';
  if (fileType === 'SQL') return 'text/x-sql';
  return 'vue';
}

export default {
  name: 'sfc-code-editor',
  props: {
    value: {
      type: String,
      default: '',
    },
    fileType: {
      type: String,
      default: 'VUE',
    },
  },
  data() {
    return {
      editor: null,
      internalCode: this.value || '',
    };
  },
  created() {
    // 非响应式标志: 程序化 setValue 时为 true, 抑制 change 事件
    this.silentFlag = false;
  },
  watch: {
    value(v) {
      if (this.editor && v !== this.internalCode) {
        this._setValueSilent(v || '');
      }
    },
    fileType(v) {
      if (this.editor) {
        this.editor.setOption('mode', modeOf(v));
      }
    },
  },
  mounted() {
    this.initEditor();
  },
  beforeDestroy() {
    if (this.editor) {
      this.editor = null;
    }
  },
  methods: {
    initEditor() {
      var mode = modeOf(this.fileType);
      this.editor = CodeMirror(this.$refs.editor, {
        value: this.internalCode,
        mode: mode,
        lineNumbers: true,
        lineWrapping: true,
        theme: 'idea',
        tabSize: 2,
        indentUnit: 2,
        autoCloseTags: true,
        autoCloseBrackets: true,
        matchBrackets: true,
        styleActiveLine: true,
        extraKeys: {
          'Ctrl-Space': 'autocomplete',
          '.': function(cm) {
            cm.replaceRange('.', cm.getCursor());
            cm.showHint({ hint: CodeMirror.hint.auto });
          },
        },
      });
      // 内容变化防抖通知
      var debounceTimer = null;
      var self = this;
      this.editor.on('change', function() {
        // 程序化 setValue 期间不触发 change 事件, 避免父组件误判为"用户修改"
        if (self.silentFlag) return;
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(function() {
          self.internalCode = self.editor.getValue();
          self.$emit('input', self.internalCode);
          self.$emit('change', self.internalCode);
        }, 500);
      });
      // 输入时自动触发代码提示
      this.editor.on('inputRead', function(cm, change) {
        if (self.silentFlag) return;
        if (cm.state.completionActive) return;
        var ch = change.text && change.text[0];
        if (!ch) return;
        // 输入字母/下划线/$ → 标识符提示
        // 输入 . → 对象属性提示
        // 输入 < → HTML 标签提示
        if (/^[a-zA-Z_$]$/.test(ch) || ch === '.') {
          cm.showHint({ hint: CodeMirror.hint.auto, completeSingle: false });
        }
      });
      // 延迟刷新 (容器尺寸可能未就绪)
      setTimeout(function() {
        if (self.editor) self.editor.refresh();
      }, 100);
    },
    getValue() {
      return this.editor ? this.editor.getValue() : this.internalCode;
    },
    setValue(val) {
      this._setValueSilent(val || '');
    },
    // 内部: 静默 setValue (不触发 change 事件)
    _setValueSilent(val) {
      if (this.editor) {
        this.silentFlag = true;
        this.editor.setValue(val || '');
        this.silentFlag = false;
      }
      this.internalCode = val || '';
    },
    refresh() {
      // 供外部 (如全屏切换后) 强制刷新 CodeMirror 尺寸
      if (this.editor) this.editor.refresh();
    },
    /**
     * 在当前光标位置插入代码
     * @param {string} code - 要插入的代码
     */
    insertAtCursor(code) {
      if (!this.editor || !code) return;
      var cursor = this.editor.getCursor();
      this.editor.replaceRange(code, cursor);
      this.internalCode = this.editor.getValue();
      this.$emit('input', this.internalCode);
      this.$emit('change', this.internalCode);
    },
    /**
     * 获取当前光标位置
     * @returns {{line: number, ch: number}}
     */
    getCursor() {
      if (!this.editor) return { line: 0, ch: 0 };
      return this.editor.getCursor();
    },
    /**
     * 应用 SEARCH/REPLACE 块 — 精准替换
     * @param {{search: string, replace: string}[]} blocks
     * @returns {{applied: number, failed: Array}}
     */
    applySearchReplace(blocks) {
      var result = { applied: 0, failed: [] };
      if (!this.editor || !blocks || blocks.length === 0) return result;
      var source = this.editor.getValue();
      for (var i = 0; i < blocks.length; i++) {
        var search = blocks[i].search;
        var replace = blocks[i].replace;
        if (source.indexOf(search) !== -1) {
          // 只替换第一处匹配
          source = source.replace(search, replace);
          result.applied++;
        } else {
          result.failed.push({ search: search, index: i });
        }
      }
      // 静默更新编辑器内容
      this._setValueSilent(source);
      // 通知父组件内容已变更
      this.$emit('input', this.internalCode);
      this.$emit('change', this.internalCode);
      return result;
    },
  },
};
</script>
<style lang="less" scoped>
.sfc-code-editor-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
}
.sfc-code-editor-body {
  flex: 1;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  overflow: hidden;
  /deep/ .CodeMirror {
    height: 100%;
    font-family: 'Courier New', Consolas, Monaco, monospace;
    font-size: 13px;
  }
  /deep/ .cm-s-idea span.cm-variable { color: #6a8759; }
  /deep/ .cm-s-idea span.cm-variable-2 { color: #cc7832; }
  /deep/ .cm-s-idea span.cm-property { color: #9876aa; }
  /deep/ .cm-s-idea span.cm-def { color: #ffc66d; }
  /deep/ .cm-s-idea span.cm-operator { color: #cc7832; }
  /deep/ .cm-s-idea span.cm-string { color: #629755; }
  /deep/ .cm-s-idea span.cm-comment { color: #808080; font-style: italic; }
  /deep/ .cm-s-idea span.cm-keyword { color: #cc7832; font-weight: bold; }
  /deep/ .cm-s-idea span.cm-number { color: #6897bb; }
  /deep/ .cm-s-idea span.cm-atom { color: #cc7832; font-weight: bold; }
  /deep/ .cm-s-idea span.cm-builtin { color: #9876aa; }
}
</style>
<!-- 代码提示弹出框：CodeMirror 将其创建到 <body> 下，必须用非 scoped 样式 -->
<style>
.CodeMirror-hints {
  z-index: 99999 !important;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  font-family: Consolas, Monaco, monospace;
  font-size: 12px;
  max-height: 200px;
}
.CodeMirror-hint {
  padding: 3px 10px;
  line-height: 1.5;
}
li.CodeMirror-hint-active {
  background: #0a84ff;
  color: #fff;
}
</style>
