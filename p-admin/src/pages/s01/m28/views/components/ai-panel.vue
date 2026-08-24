<template>
  <div class="dc-ai">
    <!-- 顶部模式切换 -->
    <div class="dc-ai-header">
      <div class="dc-ai-tabs">
        <span :class="['dc-ai-tab', { active: mode === 'chat' }]" @click="mode = 'chat'">
          <i class="h-icon-bubble"></i> 对话
        </span>
        <span :class="['dc-ai-tab', { active: mode === 'wizard' }]" @click="mode = 'wizard'">
          <i class="h-icon-magic"></i> 向导
        </span>
      </div>
      <button class="dc-ai-icon-btn" @click="$emit('collapse')" title="折叠面板">
        <i class="h-icon-arrow-right"></i>
      </button>
    </div>

    <!-- 上下文焦点 -->
    <div class="dc-ai-context" v-if="mode === 'chat'">
      <div class="dc-ai-context-label">当前上下文</div>
      <div v-if="!moduleCode && focusList.length === 0" class="dc-ai-context-empty">
        从左侧选择模块后将自动注入
      </div>
      <div class="dc-ai-context-list">
        <span v-if="moduleCode" class="dc-ai-chip primary">
          <i class="h-icon-cube"></i>
          {{ moduleCode }}
          <em v-if="moduleName">{{ moduleName }}</em>
        </span>
        <span
          v-for="f in focusList"
          :key="f.key"
          class="dc-ai-chip"
        >
          <i :class="f.icon"></i>
          {{ f.label }}
          <button class="dc-ai-chip-close" @click="removeFocus(f.key)">×</button>
        </span>
      </div>
    </div>

    <!-- 对话模式 -->
    <template v-if="mode === 'chat'">
      <!-- 消息列表 -->
      <div class="dc-ai-messages" ref="msgBox">
        <div v-if="messages.length === 0" class="dc-ai-welcome">
          <i class="h-icon-bubble"></i>
          <p>AI 助理已就绪</p>
          <p class="dc-ai-welcome-sub">
            {{ moduleCode ? '当前模块：' + moduleCode : '请先从左侧选择一个模块' }}
          </p>
          <div class="dc-ai-suggestions" v-if="moduleCode">
            <button @click="sendSuggestion('列出该模块的所有资源和字段')">
              列出资源与字段
            </button>
            <button @click="sendSuggestion('这个模块的页面配置有什么可以优化的？')">
              页面优化建议
            </button>
            <button @click="sendSuggestion('为该模块新增一个查询接口')">
              新增查询接口
            </button>
          </div>
        </div>
        <AiMessageList v-else :messages="messages" scene="aidev" />
      </div>

      <!-- 输入框 -->
      <div class="dc-ai-input-wrap">
        <AiInput :loading="chatLoading" @send="onSend" />
      </div>
    </template>

    <!-- 向导模式 -->
    <template v-else>
      <div class="dc-ai-wizard">
        <div class="dc-ai-wiz-hero">
          <i class="h-icon-magic"></i>
          <h3>AI 模块向导</h3>
          <p>6 步生成完整模块：基本信息 / 数据模型 / 视图查询 / 接口页面 / UI / 菜单</p>
          <button class="dc-ai-wiz-launch" @click="$emit('open-wizard')">
            <i class="h-icon-play"></i> 启动向导
          </button>
        </div>
        <div class="dc-ai-wiz-features">
          <div class="dc-ai-wiz-feature">
            <i class="h-icon-edit"></i>
            <div>
              <strong>分步生成</strong>
              <p>每步 AI 生成 → 人工确认 → 执行变更项</p>
            </div>
          </div>
          <div class="dc-ai-wiz-feature">
            <i class="h-icon-flash"></i>
            <div>
              <strong>一键生成全部</strong>
              <p>一句话描述需求，AI 自动跑完 6 步</p>
            </div>
          </div>
          <div class="dc-ai-wiz-feature">
            <i class="h-icon-folder"></i>
            <div>
              <strong>从模板开始</strong>
              <p>复用模板市场的现有模块结构</p>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script>
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';

const SCENE = 'aidev';

export default {
  name: 'DcAiPanel',
  components: { AiMessageList, AiInput },
  props: {
    selectedModule: { type: Object, default: null },
    collapsed: { type: Boolean, default: false }
  },
  data() {
    return {
      mode: 'chat',
      messages: [],
      chatLoading: false,
      conversationId: '',
      // 额外焦点（用户手动添加的资源/页面/脚本，Phase 4+ 接入交互）
      focusList: []
    };
  },
  computed: {
    moduleCode() {
      return (this.selectedModule && this.selectedModule.MODULECODE) || '';
    },
    moduleName() {
      return (this.selectedModule && this.selectedModule.MODULENAME) || '';
    }
  },
  watch: {
    moduleCode() {
      // 模块切换时不强制清空对话，但清空额外焦点
      this.focusList = [];
    }
  },
  methods: {
    // 暴露给父组件：从 Tab 内点击"问 AI"时调用
    addFocus(focus) {
      if (!focus || !focus.key) return;
      var exists = this.focusList.find(f => f.key === focus.key);
      if (exists) return;
      this.focusList.push(focus);
    },
    removeFocus(key) {
      this.focusList = this.focusList.filter(f => f.key !== key);
    },
    buildContextText(text) {
      // 将当前上下文注入到用户输入前面，方便 LLM 感知
      if (!this.moduleCode) return text;
      var ctx = '[当前模块] ' + this.moduleCode;
      if (this.moduleName) ctx += ' (' + this.moduleName + ')';
      if (this.focusList.length > 0) {
        ctx += '\n[焦点] ' + this.focusList.map(f => f.label).join(' / ');
      }
      return ctx + '\n\n' + text;
    },
    sendSuggestion(text) {
      this.onSend(text);
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
      this.$nextTick(this.scrollBottom);
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
      if (!text || this.chatLoading) return;
      var finalText = this.buildContextText(text);
      this.messages.push({ role: 'user', blocks: [{ type: 'text', text: text }] });
      this.messages.push({ role: 'assistant', blocks: [] });
      this.chatLoading = true;
      this.$nextTick(this.scrollBottom);
      var self = this;
      try {
        var client = new AiClient({
          scene: SCENE,
          onBlock: function(b) { self.handleBlock(b) },
          onError: function(msg) {
            self.appendBlock({ type: 'text', text: '⚠️ ' + (msg || '错误') });
            self.chatLoading = false;
          },
          onDone: function() {
            self.chatLoading = false;
          }
        });
        await client.sendScene(SCENE, this.conversationId, finalText);
      } catch (e) {
        this.appendBlock({ type: 'text', text: '⚠️ ' + (e && e.message ? e.message : '请求失败') });
        this.chatLoading = false;
      }
    },
    scrollBottom() {
      var box = this.$refs.msgBox;
      if (box) box.scrollTop = box.scrollHeight;
    }
  }
};
</script>

<style lang="less" scoped>
.dc-ai {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #fff;
}
.dc-ai-header {
  display: flex;
  align-items: center;
  padding: 8px 10px;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}
.dc-ai-tabs {
  flex: 1;
  display: flex;
  gap: 4px;
}
.dc-ai-tab {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
  color: #666;
  &.active {
    background: #e6f7ff;
    color: #2F54EB;
    font-weight: 600;
  }
  &.disabled {
    color: #ccc;
    cursor: not-allowed;
  }
  i { font-size: 12px; }
}
.dc-ai-icon-btn {
  width: 24px;
  height: 24px;
  border: none;
  background: transparent;
  cursor: pointer;
  color: #999;
  border-radius: 3px;
  &:hover { color: #2F54EB; background: #f0f5ff; }
}
.dc-ai-context {
  padding: 6px 10px;
  background: #fafbfc;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}
.dc-ai-context-label {
  font-size: 10px;
  color: #999;
  margin-bottom: 4px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.dc-ai-context-empty {
  font-size: 11px;
  color: #ccc;
  font-style: italic;
}
.dc-ai-context-list {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
.dc-ai-chip {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 6px;
  border-radius: 3px;
  font-size: 11px;
  background: #f5f5f5;
  color: #666;
  i { font-size: 10px; }
  em {
    font-style: normal;
    color: #999;
    margin-left: 2px;
  }
  &.primary {
    background: #e6f7ff;
    color: #2F54EB;
    font-weight: 600;
    em { color: #69b1ff; font-weight: 400; }
  }
}
.dc-ai-chip-close {
  border: none;
  background: transparent;
  color: #999;
  cursor: pointer;
  padding: 0 2px;
  font-size: 12px;
  line-height: 1;
  &:hover { color: #ff4d4f; }
}
.dc-ai-messages {
  flex: 1;
  overflow-y: auto;
  min-height: 0;
}
.dc-ai-welcome {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 20px;
  text-align: center;
  color: #999;
  i { font-size: 40px; color: #d6e4ff; margin-bottom: 8px; }
  p { margin: 4px 0; font-size: 13px; }
  .dc-ai-welcome-sub { font-size: 11px; color: #ccc; }
}
.dc-ai-suggestions {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
  button {
    padding: 6px 10px;
    background: #fff;
    border: 1px solid #d6e4ff;
    color: #2F54EB;
    border-radius: 4px;
    font-size: 12px;
    cursor: pointer;
    &:hover { background: #e6f7ff; border-color: #2F54EB; }
  }
}
.dc-ai-input-wrap {
  flex-shrink: 0;
  border-top: 1px solid #f0f0f0;
}
.dc-ai-wizard {
  flex: 1;
  overflow-y: auto;
  padding: 14px;
  background: #fafbfc;
}
.dc-ai-wiz-hero {
  background: linear-gradient(135deg, #e6f7ff 0%, #f0f5ff 100%);
  border-radius: 6px;
  padding: 16px 12px;
  text-align: center;
  margin-bottom: 14px;
  i {
    font-size: 28px;
    color: #2F54EB;
  }
  h3 {
    margin: 8px 0 4px;
    font-size: 14px;
    color: #1f1f1f;
  }
  p {
    margin: 0 0 12px;
    font-size: 11px;
    color: #666;
    line-height: 1.5;
  }
}
.dc-ai-wiz-launch {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 6px 16px;
  background: #2F54EB;
  color: #fff;
  border: none;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  &:hover { background: #1d39c4; }
  i { font-size: 12px; }
}
.dc-ai-wiz-features {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.dc-ai-wiz-feature {
  display: flex;
  gap: 8px;
  padding: 10px;
  background: #fff;
  border-radius: 4px;
  border: 1px solid #f0f0f0;
  i {
    font-size: 16px;
    color: #2F54EB;
    flex-shrink: 0;
    margin-top: 2px;
  }
  strong {
    display: block;
    font-size: 12px;
    color: #1f1f1f;
    margin-bottom: 2px;
  }
  p {
    margin: 0;
    font-size: 11px;
    color: #999;
    line-height: 1.5;
  }
}
</style>
