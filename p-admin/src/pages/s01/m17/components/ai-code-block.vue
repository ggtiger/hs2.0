<template>
  <div class="ai-code-block">
    <!-- 文件标题栏 -->
    <div class="acb-header">
      <span class="acb-title">
        <i class="h-icon-code"></i>
        {{ fileName || 'AI 生成代码' }}
      </span>
      <span class="acb-mode" v-if="mode === 'search-replace'">精准修改</span>
      <span class="acb-mode acb-mode-replace" v-else>完整代码</span>
    </div>

    <!-- SEARCH/REPLACE diff 预览 -->
    <div class="acb-diff" v-if="mode === 'search-replace' && searchReplace && searchReplace.length > 0">
      <div class="acb-diff-block" v-for="(block, idx) in searchReplace" :key="idx">
        <div class="acb-diff-label">修改 {{ idx + 1 }}</div>
        <!-- 旧代码 -->
        <div class="acb-diff-section acb-diff-old">
          <div class="acb-diff-section-label">删除 (-)</div>
          <pre><code v-for="(line, i) in splitLines(block.search)" :key="'s' + i" class="acb-line acb-line-del">- {{ line }}</code></pre>
        </div>
        <!-- 新代码 -->
        <div class="acb-diff-section acb-diff-new">
          <div class="acb-diff-section-label">新增 (+)</div>
          <pre><code v-for="(line, i) in splitLines(block.replace)" :key="'r' + i" class="acb-line acb-line-add">+ {{ line }}</code></pre>
        </div>
      </div>
    </div>

    <!-- 完整代码展示 -->
    <div class="acb-code" v-else>
      <pre><code>{{ code }}</code></pre>
    </div>

    <!-- 按钮区 -->
    <div class="acb-actions">
      <template v-if="mode === 'search-replace'">
        <Button size="s" color="primary" @click="$emit('apply', { mode: 'search-replace' })">应用修改</Button>
        <Button size="s" @click="$emit('apply', { mode: 'replace' })">替换全部</Button>
      </template>
      <template v-else>
        <Button size="s" color="primary" @click="$emit('apply', { mode: 'replace' })">替换全部</Button>
        <Button size="s" @click="$emit('apply', { mode: 'insert' })">插入到光标</Button>
        <Button size="s" @click="$emit('apply', { mode: 'newfile' })">新建文件</Button>
      </template>
    </div>
  </div>
</template>
<script>
export default {
  name: 'ai-code-block',
  props: {
    code: { type: String, default: '' },
    language: { type: String, default: 'vue' },
    searchReplace: { type: Array, default: function() { return [] } },
    mode: { type: String, default: 'replace' }, // 'replace' | 'search-replace' | 'insert' | 'newfile'
    fileName: { type: String, default: '' },
  },
  methods: {
    splitLines(text) {
      if (!text) return [];
      return text.split('\n');
    },
  },
};
</script>
<style lang="less" scoped>
.ai-code-block {
  border: 1px solid #3c3c3c;
  border-radius: 6px;
  overflow: hidden;
  margin: 4px 0;
}
.acb-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  background: #2d2d2d;
  border-bottom: 1px solid #3c3c3c;
}
.acb-title {
  color: #ddd;
  font-size: 12px;
  display: flex;
  align-items: center;
  gap: 6px;
  i {
    color: #0a84ff;
  }
}
.acb-mode {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 3px;
  background: #1a5276;
  color: #5dade2;
  &.acb-mode-replace {
    background: #1e3a2e;
    color: #58d68d;
  }
}
.acb-diff {
  max-height: 400px;
  overflow: auto;
  background: #1e1e1e;
}
.acb-diff-block {
  border-bottom: 1px solid #2d2d2d;
  &:last-child {
    border-bottom: none;
  }
}
.acb-diff-label {
  padding: 4px 12px;
  font-size: 11px;
  color: #888;
  background: #252526;
}
.acb-diff-section {
  padding: 4px 0;
}
.acb-diff-section-label {
  padding: 2px 12px;
  font-size: 11px;
  font-weight: bold;
}
.acb-diff-old .acb-diff-section-label {
  color: #f48771;
}
.acb-diff-new .acb-diff-section-label {
  color: #58d68d;
}
.acb-diff pre {
  margin: 0;
  padding: 0 12px;
  background: transparent;
  font-family: 'Courier New', Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.5;
}
.acb-line {
  display: block;
  white-space: pre-wrap;
  word-break: break-all;
}
.acb-line-del {
  background: rgba(244, 135, 113, 0.15);
  color: #f48771;
}
.acb-line-add {
  background: rgba(88, 214, 141, 0.15);
  color: #58d68d;
}
.acb-code {
  max-height: 300px;
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
.acb-actions {
  display: flex;
  gap: 8px;
  padding: 6px 12px;
  background: #2d2d2d;
  border-top: 1px solid #3c3c3c;
}
</style>
