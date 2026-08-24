<template>
  <div class="md-editor" :style="editorStyle">
    <div class="md-toolbar">
      <span class="md-title">{{ title }}</span>
      <div class="md-mode-switch">
        <span :class="{ active: mode === 'edit' }" @click="mode = 'edit'">编辑</span>
        <span :class="{ active: mode === 'split' }" @click="mode = 'split'">双栏</span>
        <span :class="{ active: mode === 'preview' }" @click="mode = 'preview'">预览</span>
      </div>
    </div>
    <div class="md-body" :class="'mode-' + mode">
      <textarea
        v-show="mode !== 'preview'"
        class="md-input"
        :value="value"
        :placeholder="placeholder"
        @input="$emit('input', $event.target.value)"
        @keydown.tab.prevent="insertTab"
        ref="ta"
      ></textarea>
      <div v-show="mode !== 'edit'" class="md-preview markdown-body" v-html="html"></div>
    </div>
  </div>
</template>

<script>
import { marked } from 'marked';

export default {
  name: 'MdEditor',
  props: {
    value: { type: String, default: '' },
    title: { type: String, default: '' },
    placeholder: { type: String, default: '支持 Markdown 语法：# 标题、**粗体**、- 列表、```代码块' },
    // 不传(0)时 flex 自适应父容器高度; 传数字则固定高度(px)
    height: { type: Number, default: 0 }
  },
  data() {
    return { mode: 'split' };
  },
  computed: {
    editorStyle() {
      return this.height > 0 ? { height: this.height + 'px' } : { flex: '1', 'min-height': '0' };
    },
    html() {
      if (!this.value) return '<div class="md-empty">（暂无内容）</div>';
      try {
        return marked.parse(this.value);
      } catch (e) {
        return '<pre>' + this.value.replace(/</g, '&lt;') + '</pre>';
      }
    }
  },
  methods: {
    insertTab(e) {
      var ta = this.$refs.ta;
      var start = ta.selectionStart;
      var end = ta.selectionEnd;
      var v = ta.value;
      var nv = v.substring(0, start) + '  ' + v.substring(end);
      this.$emit('input', nv);
      this.$nextTick(() => { ta.selectionStart = ta.selectionEnd = start + 2 });
    }
  }
};
</script>

<style lang="less" scoped>
.md-editor {
  display: flex;
  flex-direction: column;
  border: 1px solid #e8e8e8;
  border-radius: 4px;
  background: #fff;
  overflow: hidden;
}
.md-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 10px;
  border-bottom: 1px solid #f0f0f0;
  background: #fafafa;
  flex-shrink: 0;
}
.md-title { font-size: 12px; color: #666; font-weight: 600; }
.md-mode-switch {
  display: flex;
  border: 1px solid #d9d9d9;
  border-radius: 3px;
  overflow: hidden;
  span {
    padding: 2px 10px;
    font-size: 12px;
    cursor: pointer;
    color: #666;
    background: #fff;
    & + span { border-left: 1px solid #d9d9d9; }
    &.active { background: #1890ff; color: #fff; }
    &:hover:not(.active) { color: #1890ff; }
  }
}
.md-body {
  flex: 1;
  display: flex;
  min-height: 0;
  &.mode-edit .md-input { flex: 1; }
  &.mode-preview .md-preview { flex: 1; }
  &.mode-split {
    .md-input { width: 50%; border-right: 1px solid #f0f0f0; }
    .md-preview { width: 50%; }
  }
}
.md-input {
  border: none;
  outline: none;
  resize: none;
  padding: 12px;
  font-family: 'SF Mono', Consolas, 'Courier New', monospace;
  font-size: 13px;
  line-height: 1.7;
  color: #333;
  background: #fdfdfd;
}
.md-preview {
  padding: 12px 16px;
  overflow-y: auto;
  font-size: 13px;
  line-height: 1.7;
}
.md-empty { color: #bbb; }
</style>

<style lang="less">
/* markdown 预览样式（非 scoped，v-html 内容需要） */
.markdown-body {
  h1, h2, h3, h4 { margin: 12px 0 6px; font-weight: 600; line-height: 1.4; }
  h1 { font-size: 18px; border-bottom: 1px solid #eee; padding-bottom: 4px; }
  h2 { font-size: 16px; border-bottom: 1px solid #f0f0f0; padding-bottom: 3px; }
  h3 { font-size: 14px; }
  h4 { font-size: 13px; }
  p { margin: 6px 0; }
  ul, ol { padding-left: 22px; margin: 6px 0; }
  li { margin: 2px 0; }
  code {
    background: #f2f4f6;
    padding: 1px 5px;
    border-radius: 3px;
    font-family: Consolas, monospace;
    font-size: 12px;
    color: #c7254e;
  }
  pre {
    background: #f7f7f7;
    border: 1px solid #eee;
    border-radius: 4px;
    padding: 10px;
    overflow-x: auto;
    margin: 8px 0;
    code { background: none; padding: 0; color: #333; }
  }
  blockquote {
    border-left: 3px solid #dfe2e5;
    padding-left: 12px;
    color: #6a737d;
    margin: 8px 0;
  }
  table { border-collapse: collapse; margin: 8px 0; }
  th, td { border: 1px solid #dfe2e5; padding: 4px 10px; }
  th { background: #f6f8fa; }
  hr { border: none; border-top: 1px solid #eee; margin: 12px 0; }
  strong { color: #24292e; }
}
</style>
