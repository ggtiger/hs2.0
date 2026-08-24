<template>
  <div class="ai-code">
    <div class="ai-code-header">
      <span class="ai-code-lang">
        <i class="h-icon-code"></i>
        {{ langLabel }}
      </span>
      <button class="ai-code-copy" @click="copy">
        <span v-if="copied">✓ 已复制</span>
        <span v-else>复制</span>
      </button>
    </div>
    <div class="ai-code-body">
      <pre><code>{{ code }}</code></pre>
    </div>
    <div class="ai-code-actions" v-if="applyable">
      <Button size="s" color="primary" @click="apply('replace')">替换全部</Button>
      <Button size="s" @click="apply('insert')">插入到光标</Button>
      <Button size="s" @click="apply('newfile')">新建文件</Button>
    </div>
  </div>
</template>

<script>
// 通用代码块：header(语言+复制) + 代码区 + 底部 apply 按钮区（applyable 时，HeyUI Button）
// 风格对齐原 ai-code-block.vue 的"完整代码"模式。applyable 由父组件按 scene 控制（SFC 场景需合并到编辑器）。
export default {
  name: 'CodeBlock',
  props: {
    code: { type: String, default: '' },
    language: { type: String, default: '' },
    applyable: { type: Boolean, default: false }
  },
  data() {
    return { copied: false };
  },
  computed: {
    langLabel() {
      var map = { vue: 'Vue', js: 'JavaScript', css: 'CSS', less: 'Less', html: 'HTML', csharp: 'C#', sql: 'SQL' };
      return map[this.language] || (this.language || 'Code').toUpperCase();
    }
  },
  methods: {
    apply(mode) {
      this.$emit('apply', { code: this.code, mode: mode });
    },
    copy() {
      try {
        var text = this.code;
        if (navigator.clipboard) {
          navigator.clipboard.writeText(text);
        } else {
          var ta = document.createElement('textarea');
          ta.value = text;
          document.body.appendChild(ta);
          ta.select();
          document.execCommand('copy');
          document.body.removeChild(ta);
        }
        this.copied = true;
        var self = this;
        setTimeout(function() { self.copied = false }, 1500);
      } catch (e) {
        console.warn('[CodeBlock] 复制失败:', e);
      }
    }
  }
};
</script>

<style lang="less" scoped>
.ai-code {
  border: 1px solid #3c3c3c;
  border-radius: 6px;
  overflow: hidden;
  margin: 6px 0;
}
.ai-code-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  background: #2d2d2d;
  border-bottom: 1px solid #3c3c3c;
}
.ai-code-lang {
  color: #ddd;
  font-size: 12px;
  display: flex;
  align-items: center;
  gap: 6px;
  i {
    color: #0a84ff;
  }
}
.ai-code-copy {
  background: transparent;
  border: 1px solid #3c3c3c;
  border-radius: 3px;
  color: #aaa;
  font-size: 11px;
  padding: 2px 8px;
  cursor: pointer;
  &:hover {
    color: #fff;
    border-color: #0a84ff;
  }
}
.ai-code-body {
  max-height: 360px;
  overflow: auto;
  background: #1e1e1e;
  pre {
    margin: 0;
    padding: 8px 12px;
    font-family: 'Courier New', Consolas, Monaco, monospace;
    font-size: 12px;
    line-height: 1.5;
    color: #d4d4d4;
    white-space: pre-wrap;
    word-break: break-all;
  }
}
.ai-code-actions {
  display: flex;
  gap: 8px;
  padding: 6px 12px;
  background: #2d2d2d;
  border-top: 1px solid #3c3c3c;
}
</style>
