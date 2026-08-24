<template>
  <div class="scene-test-chat sp-section">
    <div class="stc-header" @click="toggleSection">
      <i :class="sectionExpanded ? 'h-icon-down' : 'h-icon-right'"></i>
      <span class="stc-title">测试对话</span>
      <span class="stc-scene" v-if="sceneCode">{{ sceneCode }}</span>
      <span class="stc-status" v-if="chatLoading">生成中...</span>
      <Button size="s" @click.stop="clearChat" v-if="messages.length > 0" style="margin-left:auto">清空</Button>
    </div>
    <div class="stc-body" v-if="sectionExpanded">
      <div v-if="!sceneCode" class="stc-empty">请先选择场景</div>
      <div v-else-if="!form.ENABLED" class="stc-empty">场景未启用，无法测试</div>
      <template v-else>
        <div class="stc-tip">按场景配置（模型/提示词/工具集/参数）发起对话，验证配置是否生效</div>
        <AiMessageList :messages="messages" scene="assistant" class="stc-messages" />
        <AiInput :loading="chatLoading" @send="onSend" class="stc-input" />
      </template>
    </div>
  </div>
</template>

<script>
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';

export default {
  name: 'SceneTestChat',
  components: { AiMessageList, AiInput },
  props: {
    sceneCode: { type: String, default: '' },
    form: { type: Object, default: function() { return {} } }
  },
  data() {
    return {
      sectionExpanded: false,
      messages: [],
      chatLoading: false,
      conversationId: ''
    };
  },
  watch: {
    sceneCode() {
      this.clearChat();
    }
  },
  methods: {
    toggleSection() {
      this.sectionExpanded = !this.sectionExpanded;
    },
    clearChat() {
      this.messages = [];
      this.conversationId = '';
    },
    handleBlock(b) {
      if (!b || !b.type) return;
      switch (b.type) {
        case 'conversation':
          this.conversationId = b.conversationId;
          break;
        case 'tool_call':
          this.appendBlock({ type: 'tool_call', tool: b.tool, args: b.args, summary: '' });
          break;
        case 'tool_result':
          this.updateToolResult(b.tool, b.summary);
          break;
        default:
          this.appendBlock(b);
      }
    },
    appendBlock(block) {
      var msgs = this.messages;
      var last = msgs[msgs.length - 1];
      if (!last || last.role !== 'assistant') return;
      if (block.type === 'text' && last.blocks.length && last.blocks[last.blocks.length - 1].type === 'text') {
        last.blocks[last.blocks.length - 1].text += block.text;
      } else {
        last.blocks.push(block);
      }
    },
    updateToolResult(tool, summary) {
      var msgs = this.messages;
      var last = msgs[msgs.length - 1];
      if (!last) return;
      for (var i = last.blocks.length - 1; i >= 0; i--) {
        if (last.blocks[i].type === 'tool_call' && last.blocks[i].tool === tool) {
          last.blocks[i].summary = summary;
          break;
        }
      }
    },
    async onSend(text) {
      this.messages.push({ role: 'user', blocks: [{ type: 'text', text: text }] });
      this.messages.push({ role: 'assistant', blocks: [] });
      this.chatLoading = true;
      var self = this;
      try {
        var client = new AiClient({
          scene: 'assistant',
          onBlock: function(b) { self.handleBlock(b) },
          onError: function(msg) {
            self.appendBlock({ type: 'text', text: '⚠️ ' + (msg || '错误') });
            self.chatLoading = false;
          },
          onDone: function() {
            self.chatLoading = false;
          }
        });
        // 走 AskScene Hub 方法，按场景配置运行
        await client.sendScene(this.sceneCode, this.conversationId, text);
      } catch (e) {
        this.appendBlock({ type: 'text', text: '⚠️ ' + (e && e.message ? e.message : '请求失败') });
        this.chatLoading = false;
      }
    }
  }
};
</script>

<style lang="less" scoped>
.scene-test-chat {
  border: 1px solid #e8e8e8;
  border-radius: 6px;
  background: #fff;
  overflow: hidden;
}
.stc-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  background: #fafafa;
  border-bottom: 1px solid #f0f0f0;
  cursor: pointer;
  flex-shrink: 0;
  &:hover { background: #f5f7fa; }
  i { color: #999; font-size: 12px; }
}
.stc-title { font-size: 13px; font-weight: 600; }
.stc-scene {
  font-size: 11px; color: #2F54EB; font-family: Consolas, monospace;
  background: #f0f5ff; padding: 0 5px; border-radius: 3px;
}
.stc-status {
  font-size: 11px; color: #fa8c16; background: #fff7e6;
  padding: 0 5px; border-radius: 3px;
}
.stc-body {
  display: flex;
  flex-direction: column;
  height: 400px;
}
.stc-tip {
  font-size: 11px; color: #999; padding: 6px 12px;
  background: #fafafa; border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}
.stc-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 12px;
  color: #999;
}
.stc-messages {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  border-bottom: 1px solid #f0f0f0;
}
.stc-input {
  flex-shrink: 0;
}
</style>
