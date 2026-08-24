<template>
  <Modal v-model="visible" :title="title" :width="700">
    <div class="rs-code-editor">
      <div class="rs-code-toolbar">
        <Select v-model="mode" :datas="modeOptions" style="width:120px" @change="onModeChange"></Select>
        <Button size="s" icon="h-icon-refresh" @click="formatCode">格式化</Button>
      </div>
      <div ref="editor" class="rs-code-editor-body"></div>
    </div>
    <template slot="footer">
      <Button @click="visible = false">取消</Button>
      <Button color="primary" @click="onConfirm">确定</Button>
    </template>
  </Modal>
</template>
<script>
import CodeMirror from 'codemirror';
import 'codemirror/lib/codemirror.css';
import 'codemirror/mode/sql/sql.js';
import 'codemirror/mode/javascript/javascript.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/theme/idea.css';

// 语言名归一化：配置里写 csharp → CodeMirror clike 模式的 text/x-csharp
function normalizeMode(v) {
  if (v === 'csharp' || v === 'c#' || v === 'cs') return 'text/x-csharp';
  return v || 'sql';
}

export default {
  name: 'rs-code-editor',
  props: {
    value: {
      type: Boolean,
      default: false,
    },
    code: {
      type: String,
      default: '',
    },
    title: {
      type: String,
      default: '代码编辑',
    },
    language: {
      type: String,
      default: 'sql',
    },
  },
  data() {
    return {
      visible: this.value,
      mode: normalizeMode(this.language),
      modeOptions: [
        { key: 'sql', title: 'SQL' },
        { key: 'javascript', title: 'JavaScript' },
        { key: 'text/x-csharp', title: 'C#' },
        { key: 'text/plain', title: '纯文本' },
      ],
      editor: null,
      internalCode: this.code || '',
    };
  },
  computed: {},
  watch: {
    value(v) {
      this.visible = v;
      if (v) {
        // Modal 打开时同步最新 code 值
        this.internalCode = this.code || '';
        this.mode = normalizeMode(this.language);
        this.$nextTick(() => {
          this.initEditor();
        });
      }
    },
    visible(v) {
      this.$emit('input', v);
      if (!v && this.editor) {
        // Modal 关闭时销毁编辑器实例，避免再次打开时 DOM 脱离
        const wrapper = this.editor.getWrapperElement();
        if (wrapper && wrapper.parentNode) {
          wrapper.parentNode.removeChild(wrapper);
        }
        this.editor = null;
      }
    },
    code(v) {
      this.internalCode = v || '';
      if (this.editor) {
        this.editor.setValue(this.internalCode);
      }
    },
    language(v) {
      this.mode = normalizeMode(v);
      if (this.editor) {
        this.editor.setOption('mode', this.mode);
      }
    },
  },
  methods: {
    initEditor() {
      // 每次打开都重新创建（关闭时已销毁旧实例）
      this.editor = CodeMirror(this.$refs.editor, {
        value: this.internalCode,
        mode: this.mode,
        lineNumbers: true,
        lineWrapping: true,
        theme: 'idea',
        tabSize: 2,
      });
      // Modal 动画可能导致编辑器尺寸计算不准，延迟刷新
      setTimeout(() => {
        if (this.editor) this.editor.refresh();
      }, 100);
    },
    onModeChange(val) {
      this.mode = val;
      if (this.editor) {
        this.editor.setOption('mode', val);
      }
    },
    formatCode() {
      if (!this.editor) return;
      const code = this.editor.getValue();
      if (!code.trim()) return;
      let formatted = code;
      if (this.mode === 'sql') {
        formatted = this.formatSQL(code);
      } else if (this.mode === 'javascript') {
        formatted = this.formatJS(code);
      }
      // 纯文本不做格式化
      if (formatted !== code) {
        this.editor.setValue(formatted);
      }
    },
    formatSQL(sql) {
      const keywords = [
        'SELECT', 'FROM', 'WHERE', 'AND', 'OR', 'NOT', 'IN', 'ON',
        'JOIN', 'LEFT JOIN', 'RIGHT JOIN', 'INNER JOIN', 'OUTER JOIN',
        'CROSS JOIN', 'FULL JOIN', 'GROUP BY', 'ORDER BY', 'HAVING',
        'INSERT INTO', 'UPDATE', 'DELETE', 'SET', 'VALUES', 'INTO',
        'CREATE', 'ALTER', 'DROP', 'TABLE', 'INDEX', 'VIEW', 'AS',
        'DISTINCT', 'BETWEEN', 'LIKE', 'IS', 'NULL', 'EXISTS',
        'UNION', 'UNION ALL', 'INTERSECT', 'EXCEPT', 'LIMIT', 'OFFSET',
        'CASE', 'WHEN', 'THEN', 'ELSE', 'END',
      ];
      const upper = sql.trim();
      let result = upper;
      // 先在主关键字前加换行
      keywords.forEach(kw => {
        // 匹配关键字前面不是换行的位置，插入换行
        const re = new RegExp('\\b(' + kw.replace(/ /g, '\\s+') + ')\\b', 'gi');
        result = result.replace(re, '\n' + kw);
      });
      // 清理多余空行和行首空格
      const lines = result.split('\n')
        .map(l => l.trim())
        .filter(l => l);
      // 缩进：主关键字顶格，其余缩进2格
      const mainKeywords = new Set([
        'SELECT', 'FROM', 'WHERE', 'GROUP BY', 'ORDER BY', 'HAVING',
        'INSERT INTO', 'UPDATE', 'DELETE', 'CREATE', 'ALTER', 'DROP',
        'UNION', 'UNION ALL', 'LEFT JOIN', 'RIGHT JOIN', 'INNER JOIN',
        'CROSS JOIN', 'FULL JOIN', 'JOIN', 'ON', 'LIMIT',
      ]);
      const formatted = lines.map(line => {
        const isMain = mainKeywords.has(line.split(/\s+/).slice(0, 2).join(' ').toUpperCase());
        return isMain ? line : '  ' + line;
      });
      return formatted.join('\n');
    },
    formatJS(code) {
      // 简单的 JS 格式化：基于花括号缩进
      let indent = 0;
      const lines = code.split('\n');
      const result = [];
      lines.forEach(line => {
        const trimmed = line.trim();
        if (!trimmed) return;
        // 闭合括号先减缩进
        if (trimmed.startsWith('}') || trimmed.startsWith(']') || trimmed.startsWith(')')) {
          indent = Math.max(0, indent - 1);
        }
        result.push('  '.repeat(indent) + trimmed);
        // 开启括号后加缩进
        if (trimmed.endsWith('{') || trimmed.endsWith('[') || trimmed.endsWith('(')) {
          indent++;
        }
      });
      return result.join('\n');
    },
    onConfirm() {
      const code = this.editor ? this.editor.getValue() : this.internalCode;
      this.$emit('confirm', code);
      this.visible = false;
    },
  },
  beforeDestroy() {
    if (this.editor) {
      this.editor = null;
    }
  },
};
</script>
<style lang="less" scoped>
.rs-code-editor {
  position: relative;
}
.rs-code-toolbar {
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.rs-code-editor-body {
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  width: 660px;
  /deep/ .CodeMirror {
    height: 400px;
    width: 660px;
    font-family: 'Courier New', Courier, monospace;
    font-size: 13px;
  }
}
</style>
