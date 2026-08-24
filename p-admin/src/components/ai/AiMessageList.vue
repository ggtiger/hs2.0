<template>
  <div class="ai-msg-list" ref="msgList" :class="{ 'ai-msg-list-dark': theme === 'dark' }">
    <div
      v-for="(msg, i) in messages"
      :key="i"
      :class="['ai-msg', 'ai-msg-' + msg.role]"
    >
      <template v-for="(b, j) in (msg.blocks || [])">
          <!-- 填表/子表块：内联信息卡（无独立组件） -->
          <div v-if="b.type === 'fill'" class="ai-fill-block" :key="'f' + j">
            <span class="ai-fill-icon">📝</span>
            <span class="ai-fill-text">填充表单: {{ formatFields(b.fields) }}</span>
          </div>
          <div v-else-if="b.type === 'subtable'" class="ai-fill-block" :key="'s' + j">
            <span class="ai-fill-icon">📋</span>
            <span class="ai-fill-text">子表 {{ b.path || '' }}: {{ formatFields(b.rows) }}</span>
          </div>
          <!-- SEARCH/REPLACE 代码块：diff 预览 + 应用按钮（emit apply-code 透传父组件） -->
          <div v-else-if="b.type === 'search_replace'" class="ai-sr-block" :key="'sr' + j">
            <div class="ai-sr-header">
              <span class="ai-sr-title">{{ b.fileName || 'AI 生成代码' }}</span>
              <span class="ai-sr-badge">精准修改</span>
            </div>
            <div class="ai-sr-diff">
              <div class="ai-sr-diff-item" v-for="(blk, k) in (b.searchReplace || [])" :key="k">
                <div class="ai-sr-diff-label">修改 {{ k + 1 }}</div>
                <pre class="ai-sr-diff-old"><code v-for="(line, li) in splitLines(blk.search)" :key="'d' + li" class="ai-sr-line ai-sr-line-del">- {{ line }}</code></pre>
                <pre class="ai-sr-diff-new"><code v-for="(line, li) in splitLines(blk.replace)" :key="'r' + li" class="ai-sr-line ai-sr-line-add">+ {{ line }}</code></pre>
              </div>
            </div>
            <div class="ai-sr-actions">
              <button class="ai-sr-btn ai-sr-btn-primary" @click="$emit('apply-code', { block: b, mode: 'search-replace' })">应用修改</button>
              <button class="ai-sr-btn" @click="$emit('apply-code', { block: b, mode: 'replace' })">替换全部</button>
            </div>
          </div>
          <!-- 图片块（粘贴的图片） -->
          <div v-else-if="b.type === 'image'" class="ai-msg-image-wrap" :key="'img' + j">
            <img :src="b.dataUrl" class="ai-msg-image" />
          </div>
          <!-- 其余块：组件分发 -->
          <component
            v-else
            :key="j"
            :is="blockComp(b.type)"
            :ref="'blk_' + i + '_' + j"
            v-bind="blockProps(b)"
            @confirm-sql="$emit('confirm-sql', $event)"
            @apply-code="$emit('apply-code', $event)"
          />
        </template>
    </div>
  </div>
</template>

<script>
import RichTextBlock from './blocks/RichTextBlock.vue';
import ThinkingBlock from './blocks/ThinkingBlock.vue';
import ToolCallBlock from './blocks/ToolCallBlock.vue';
import NavigateBlock from './blocks/NavigateBlock.vue';
import CodeBlock from './blocks/CodeBlock.vue';
import MetadataSqlBlock from './blocks/MetadataSqlBlock.vue';

export default {
  name: 'AiMessageList',
  components: { RichTextBlock, ThinkingBlock, ToolCallBlock, NavigateBlock, CodeBlock, MetadataSqlBlock },
  props: {
    // messages: [{ role: 'user'|'assistant', blocks: [{ type, text?, ... }] }]
    messages: { type: Array, default: function() { return [] } },
    scene: { type: String, default: 'assistant' },
    theme: { type: String, default: 'light' } // 'light' | 'dark'
  },
  watch: {
    // 消息变化时自动滚动到底部
    messages: {
      handler() {
        this.$nextTick(() => this.scrollToBottom());
      },
      deep: true
    }
  },
  methods: {
    userText(msg) {
      if (msg.blocks && msg.blocks[0]) return msg.blocks[0].text;
      return msg.content || '';
    },
    blockComp(type) {
      var map = {
        text: 'RichTextBlock',
        thinking: 'ThinkingBlock',
        tool_call: 'ToolCallBlock',
        navigate: 'NavigateBlock',
        code: 'CodeBlock',
        metadata_sql: 'MetadataSqlBlock'
      };
      return map[type] || null;
    },
    blockProps(b) {
      if (b.type === 'text' || b.type === 'thinking') return { text: b.text || '', applyable: this.scene === 'sfc', theme: this.theme };
      if (b.type === 'tool_call') return { tool: b.tool, args: b.args, summary: b.summary };
      if (b.type === 'navigate') return { path: b.path, query: b.query, moduleCode: b.moduleCode, moduleName: b.moduleName };
      if (b.type === 'code') return { code: b.code || '', language: b.language || b.lang || '' };
      if (b.type === 'metadata_sql') return { code: b.code || '', initialStatus: b.execStatus || 'pending' };
      return b;
    },
    formatFields(fields) {
      if (!fields) return '';
      try {
        return JSON.stringify(fields);
      } catch (e) {
        return String(fields);
      }
    },
    splitLines(text) {
      if (!text) return [];
      return text.split('\n');
    },
    // 供父组件按 code 定位 MetadataSqlBlock（嵌套在 RichTextBlock 内）并更新执行状态
    // 遍历所有块组件 ref，委托给含 setSqlStatus 的组件（RichTextBlock）
    setSqlStatus(code, status, result) {
      var refs = this.$refs;
      var keys = Object.keys(refs);
      for (var i = 0; i < keys.length; i++) {
        var comp = refs[keys[i]];
        if (comp && typeof comp.setSqlStatus === 'function') {
          if (comp.setSqlStatus(code, status, result)) return true;
        }
      }
      return false;
    },
    scrollToBottom() {
      var el = this.$refs.msgList;
      if (el) el.scrollTop = el.scrollHeight;
    }
  }
};
</script>

<style scoped>
.ai-msg-list {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
}
.ai-msg-list-dark {
  background: #1e1e1e;
  color: #d4d4d4;
}
.ai-msg-image-wrap {
  margin: 4px 0;
}
.ai-msg-image {
  max-width: 220px;
  max-height: 220px;
  border-radius: 6px;
  border: 1px solid #eee;
}
.ai-msg {
  margin-bottom: 12px;
}
.ai-msg-user {
  text-align: right;
}
.ai-msg-user::before {
  content: '我';
  display: inline-block;
  width: 24px;
  height: 24px;
  line-height: 24px;
  text-align: center;
  border-radius: 50%;
  background: #19be6b;
  color: #fff;
  font-size: 12px;
  margin-right: 6px;
}
.ai-msg-assistant::before {
  content: 'AI';
  display: inline-block;
  width: 24px;
  height: 24px;
  line-height: 24px;
  text-align: center;
  border-radius: 50%;
  background: #2d8cf0;
  color: #fff;
  font-size: 12px;
  margin-right: 6px;
}
.ai-fill-block {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin: 4px 0;
  padding: 4px 8px;
  background: #f6f8fa;
  border-left: 3px solid #19be6b;
  border-radius: 0 4px 4px 0;
  font-size: 12px;
  color: #555;
  word-break: break-all;
}
.ai-msg-list-dark .ai-fill-block {
  background: #2d2d2d;
  color: #ddd;
}
/* SEARCH/REPLACE diff 块（深色主题） */
.ai-sr-block {
  margin: 6px 0;
  border: 1px solid #3c3c3c;
  border-radius: 6px;
  overflow: hidden;
}
.ai-sr-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  background: #2d2d2d;
  border-bottom: 1px solid #3c3c3c;
}
.ai-sr-title {
  color: #ddd;
  font-size: 12px;
}
.ai-sr-badge {
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 3px;
  background: #1a5276;
  color: #5dade2;
}
.ai-sr-diff {
  max-height: 400px;
  overflow: auto;
  background: #1e1e1e;
}
.ai-sr-diff-item {
  border-bottom: 1px solid #2d2d2d;
  &:last-child { border-bottom: none; }
}
.ai-sr-diff-label {
  padding: 4px 12px;
  font-size: 11px;
  color: #888;
  background: #252526;
}
.ai-sr-diff-old,
.ai-sr-diff-new {
  margin: 0;
  padding: 0 12px;
  background: transparent;
  font-family: 'Courier New', Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.5;
}
.ai-sr-line {
  display: block;
  white-space: pre-wrap;
  word-break: break-all;
  font-family: 'Courier New', Consolas, Monaco, monospace;
  font-size: 12px;
  line-height: 1.5;
}
.ai-sr-line-del {
  background: rgba(244, 135, 113, 0.15);
  color: #f48771;
}
.ai-sr-line-add {
  background: rgba(88, 214, 141, 0.15);
  color: #58d68d;
}
.ai-sr-actions {
  display: flex;
  gap: 8px;
  padding: 6px 12px;
  background: #2d2d2d;
  border-top: 1px solid #3c3c3c;
}
.ai-sr-btn {
  padding: 4px 14px;
  border: 1px solid #3c3c3c;
  border-radius: 4px;
  background: transparent;
  color: #aaa;
  font-size: 12px;
  cursor: pointer;
  &:hover {
    color: #fff;
    border-color: #0a84ff;
  }
}
.ai-sr-btn-primary {
  background: #0a84ff;
  border-color: #0a84ff;
  color: #fff;
  &:hover {
    background: #0978e0;
    border-color: #0978e0;
    color: #fff;
  }
}
</style>
