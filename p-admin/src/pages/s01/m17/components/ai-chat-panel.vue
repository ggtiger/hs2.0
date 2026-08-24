<template>
  <div class="ai-chat-panel">
    <!-- 顶部标题栏 -->
    <div class="acp-header">
      <span class="acp-title">AI 助手</span>
      <span class="acp-actions">
        <span class="acp-clear-btn" @click="clearMessages" title="清空对话">清空</span>
      </span>
    </div>

    <!-- 消息区 -->
    <div class="acp-body">
      <div v-if="messages.length === 0" class="acp-empty">
        <div class="acp-empty-icon">AI</div>
        <p>告诉我你的需求，我来帮你写代码</p>
        <p class="acp-empty-hint" v-html="emptyHint"></p>
      </div>
      <AiMessageList
        v-else
        ref="msgList"
        :messages="messages"
        scene="sfc"
        theme="dark"
        @confirm-sql="onConfirmSql"
        @apply-code="onApplyCode"
      ></AiMessageList>
    </div>

    <!-- 输入区 -->
    <AiInput
      ref="inputRef"
      :loading="loading"
      theme="dark"
      placeholder="输入消息... (Enter 发送，Shift+Enter 换行)"
      @send="sendMessage"
    ></AiInput>
  </div>
</template>
<script>
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';
import { executeMetadataSql } from '@/api/sfc-ai';

export default {
  name: 'ai-chat-panel',
  components: { AiMessageList, AiInput },
  props: {
    currentFile: { type: Object, default: function() { return null } },
    siblingFiles: { type: Array, default: function() { return [] } },
    moduleCode: { type: String, default: '' },
    editTarget: { type: String, default: '' },
    pageCode: { type: String, default: '' },
    // 空态示例提示（HTML，按资产类型定制）
    emptyHint: { type: String, default: '例如：帮我写一个单表CRUD列表页<br />把编码列宽度改为150<br />在 methods 里加一个 reset 方法' }
  },
  data() {
    return {
      // 统一 blocks 模型：[{role:'user'|'assistant', blocks:[{type, ...}], streaming?}]
      messages: [],
      loading: false
    };
  },
  created() {
    // 创建 AiClient（sfc 场景走 SSE），绑定回调
    this.aiClient = new AiClient({
      scene: 'sfc',
      onBlock: this.onBlock.bind(this),
      onError: this.onError.bind(this),
      onDone: this.onDone.bind(this)
    });
    // 当前流式 AI 消息引用（非响应式，指向 messages 内对象）
    this.currentAiMsg = null;
  },
  beforeDestroy() {
    if (this.aiClient) this.aiClient.disconnect();
  },
  methods: {
    /**
     * 发送消息（用户输入或 metadata-sql 执行后自动续发）
     */
    async sendMessage(text) {
      if (!text || this.loading) return;

      // 用户消息
      this.messages.push({ role: 'user', blocks: [{ type: 'text', text: text }] });
      this.loading = true;

      // AI 消息占位（流式更新）
      var aiMsg = { role: 'assistant', blocks: [], streaming: true };
      this.messages.push(aiMsg);
      this.currentAiMsg = aiMsg;

      try {
        await this.aiClient.sendSfc(text, this.buildContext());
      } catch (e) {
        this.appendError(e.message || String(e));
      } finally {
        this.finalizeStream();
      }
    },

    /**
     * AiClient onBlock 回调：分发 text/tool_call/tool_result
     */
    onBlock(b) {
      if (!b || !b.type) return;
      var msg = this.currentAiMsg;
      if (!msg) return;
      if (b.type === 'text') {
        // 累加到最后一个 text block（打字效果）
        this.appendAssistantText(b.text || '');
      } else if (b.type === 'tool_call') {
        // 追加 tool_call block
        msg.blocks.push({ type: 'tool_call', tool: b.tool, args: b.args, summary: '', status: 'running' });
      } else if (b.type === 'tool_result') {
        // 反向查找最后一个 tool_call 更新 summary/status
        for (var i = msg.blocks.length - 1; i >= 0; i--) {
          if (msg.blocks[i].type === 'tool_call') {
            msg.blocks[i].status = 'done';
            msg.blocks[i].summary = b.summary;
            break;
          }
        }
      }
    },

    /**
     * AiClient onError 回调：错误追加为文本
     */
    onError(msg) {
      this.appendError(msg);
    },

    /**
     * AiClient onDone 回调：结束流式 + 提取 SEARCH/REPLACE
     */
    onDone() {
      var msg = this.currentAiMsg;
      if (msg) {
        msg.streaming = false;
        this.extractSearchReplace(msg);
      }
    },

    /**
     * 流结束兜底（异常/正常均在 finally 调用）
     */
    finalizeStream() {
      var msg = this.currentAiMsg;
      if (msg && msg.streaming) {
        msg.streaming = false;
        this.extractSearchReplace(msg);
      }
      this.currentAiMsg = null;
      this.loading = false;
    },

    /**
     * 累加文本到最后一个 text block，无则新建
     */
    appendAssistantText(text) {
      var msg = this.currentAiMsg;
      if (!msg) return;
      var blocks = msg.blocks;
      var last = blocks.length > 0 ? blocks[blocks.length - 1] : null;
      if (last && last.type === 'text') {
        last.text += text;
      } else {
        blocks.push({ type: 'text', text: text });
      }
    },

    /**
     * 追加错误文本
     */
    appendError(msg) {
      this.appendAssistantText('\n\n**错误:** ' + msg);
    },

    /**
     * 从最后一个 text block 提取 SEARCH/REPLACE 块
     * RichTextBlock 不支持 SEARCH/REPLACE，这里提取为独立 search_replace block
     * 普通 ```vue/js``` 代码块仍由 RichTextBlock 实时解析
     */
    extractSearchReplace(msg) {
      for (var i = msg.blocks.length - 1; i >= 0; i--) {
        var blk = msg.blocks[i];
        if (blk.type === 'text') {
          var srMatches = this.parseSearchReplace(blk.text);
          if (srMatches.length > 0) {
            // 从 text 中移除 SEARCH/REPLACE 部分：先移除 ```vue 围栏包裹的整块（避免留空 vue 代码块壳），再移除裸的
            blk.text = blk.text
              .replace(/```(?:vue|js|javascript)?\s*\n<<<<<<< SEARCH\s*\n[\s\S]*?\n>>>>>>> REPLACE\s*\n```/g, '')
              .replace(/<<<<<<< SEARCH\s*\n[\s\S]*?>>>>>>> REPLACE/g, '')
              .trim();
            // 追加 search_replace block（AiMessageList 渲染 diff + 应用按钮）
            msg.blocks.push({
              type: 'search_replace',
              searchReplace: srMatches,
              code: '',
              language: 'vue',
              fileName: this.currentFile ? this.currentFile.path : ''
            });
          }
          break;
        }
      }
    },

    /**
     * 解析 SEARCH/REPLACE 格式
     */
    parseSearchReplace(text) {
      // AI 可能把 SEARCH/REPLACE 放在 ```vue/js 围栏里输出，先去掉围栏再匹配
      var normalized = String(text || '').replace(/```(?:vue|js|javascript)?\s*\n(<<<<<<< SEARCH[\s\S]*?>>>>>>> REPLACE)\n```/g, '$1');
      var srRe = /<<<<<<< SEARCH\s*\n([\s\S]*?)\n=======\s*\n([\s\S]*?)\n>>>>>>> REPLACE/g;
      var matches = [];
      var m;
      while ((m = srRe.exec(normalized)) !== null) {
        matches.push({ search: m[1], replace: m[2] });
      }
      return matches;
    },

    /**
     * 确认执行元数据 SQL（MetadataSqlBlock emit confirm -> AiMessageList emit confirm-sql）
     * 执行后更新状态，成功则结果作为新 user 消息续发 AI
     */
    async onConfirmSql(code) {
      // MetadataSqlBlock 已自行设置 'running'
      try {
        var result = await executeMetadataSql(code);
        this.$refs.msgList.setSqlStatus(code, 'success', '执行成功，影响 ' + (result.affectedRows || 0) + ' 行');
        // 结果作为新 user 消息追加，触发 AI 继续
        var followUp = '元数据 SQL 已执行成功，影响 ' + (result.affectedRows || 0) + ' 行。请基于更新后的元数据继续。';
        this.sendMessage(followUp);
      } catch (e) {
        this.$refs.msgList.setSqlStatus(code, 'failed', '执行失败: ' + (e.message || e));
      }
    },

    /**
     * 应用代码 - 通知父组件（AiMessageList search_replace block emit apply-code）
     */
    onApplyCode(event) {
      var block = event.block || {};
      var payload = {
        code: event.code || block.code || '',
        mode: event.mode
      };
      if (block.searchReplace && block.searchReplace.length > 0) {
        payload.searchReplace = block.searchReplace;
      }
      this.$emit('apply-code', payload);
    },

    /**
     * 构建 SFC 上下文
     */
    buildContext() {
      return {
        currentFile: this.currentFile,
        siblingFiles: this.siblingFiles,
        moduleCode: this.moduleCode,
        editTarget: this.editTarget,
        pageCode: this.pageCode
      };
    },

    clearMessages() {
      this.messages = [];
      this.currentAiMsg = null;
    },

    /**
     * 暴露给父组件: 聚焦输入框
     */
    focusInput() {
      if (this.$refs.inputRef) this.$refs.inputRef.focus();
    }
  }
};
</script>
<style lang="less" scoped>
.ai-chat-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #1e1e1e;
}
.acp-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  background: #2d2d2d;
  border-bottom: 1px solid #3c3c3c;
  flex-shrink: 0;
}
.acp-title {
  color: #ddd;
  font-size: 13px;
  font-weight: bold;
}
.acp-clear-btn {
  color: #888;
  font-size: 12px;
  cursor: pointer;
  padding: 2px 8px;
  border-radius: 3px;
  &:hover {
    color: #fff;
    background: #3c3c3c;
  }
}
.acp-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}
.acp-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #555;
  p {
    margin: 8px 0;
    font-size: 13px;
  }
  .acp-empty-hint {
    font-size: 11px;
    color: #444;
    text-align: center;
    line-height: 1.8;
  }
}
.acp-empty-icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #2d8cf0;
  color: #fff;
  font-size: 18px;
  font-weight: bold;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
}
</style>
