<template>
  <div class="form-ai-panel" v-show="visible" :style="panelStyle" ref="panel">
    <!-- 拖动标题栏 -->
    <div class="form-ai-header" @mousedown="startDrag">
      <span class="form-ai-title">✨ AI 填报</span>
      <div class="form-ai-header-btns">
        <span class="form-ai-resize-btn" @click="toggleSize" title="调整大小">⤢</span>
        <span class="form-ai-close" @click="close">✕</span>
      </div>
    </div>
    <!-- 消息区域 -->
    <AiMessageList :messages="messages" scene="form" theme="light" />
    <!-- 输入区域 -->
    <AiInput
      :loading="analyzingImage"
      :placeholder="pastePlaceholder"
      @send="send"
      @paste="onPaste"
    />
    <!-- 调整大小的手柄（四边） -->
    <div class="form-ai-resize-handle form-ai-resize-handle-n" @mousedown="startResize($event, 'n')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-e" @mousedown="startResize($event, 'e')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-s" @mousedown="startResize($event, 's')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-w" @mousedown="startResize($event, 'w')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-ne" @mousedown="startResize($event, 'ne')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-nw" @mousedown="startResize($event, 'nw')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-se" @mousedown="startResize($event, 'se')"></div>
    <div class="form-ai-resize-handle form-ai-resize-handle-sw" @mousedown="startResize($event, 'sw')"></div>
  </div>
</template>

<script>
import AiClient from '@/utils/ai/AiClient';
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';

export default {
  name: 'FormAssistantPanel',
  components: { AiMessageList, AiInput },
  // inject 当前表单的store模块名（add01.js provide，用于子表/主表操作工具）
  inject: {
    aiFormStoreName: { default: null }
  },
  props: {
    moduleCode: { type: String, required: true },
    visible: { type: Boolean, default: false },
    formData: { type: Object, default: () => ({}) }
  },
  data() {
    return {
      messages: [],
      aiClient: null,
      analyzingImage: false,
      // 面板位置和大小
      x: 0,
      y: 100,
      width: 380,
      height: 500,
      isExpanded: true,
      // 拖动状态
      isDragging: false,
      dragStartX: 0,
      dragStartY: 0,
      dragStartPanelX: 0,
      dragStartPanelY: 0,
      // 调整大小状态
      isResizing: false,
      resizeDir: '', // n,e,s,w,ne,nw,se,sw
      resizeStartX: 0,
      resizeStartY: 0,
      resizeStartWidth: 0,
      resizeStartHeight: 0,
      resizeStartPanelX: 0,
      resizeStartPanelY: 0
    };
  },
  computed: {
    panelStyle() {
      return {
        position: 'fixed',
        top: this.y + 'px',
        left: this.x + 'px',
        width: this.width + 'px',
        height: this.height + 'px'
      };
    },
    pastePlaceholder() {
      return this.analyzingImage ?
        '正在识别图片内容…' :
        '描述要填的内容…（如：客户ABC公司，器具万用表，日期今天。可Ctrl+V粘贴图片自动识别）';
    },
    // 当前store模块名（inject优先，用于传递给代理层操作主表/子表）
    currentStoreName() {
      return this.aiFormStoreName;
    }
  },
  mounted() {
    // 把面板移动到 body 下，避免被父容器的 transform 影响 fixed 定位
    if (this.$refs.panel && this.$refs.panel.parentNode !== document.body) {
      document.body.appendChild(this.$refs.panel);
    }
    // 全局鼠标事件
    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('mouseup', this.onMouseUp);
    window.addEventListener('resize', this.onWindowResize);
  },
  beforeDestroy() {
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
    window.removeEventListener('resize', this.onWindowResize);
    if (this.aiClient) {
      this.aiClient.disconnect();
      this.aiClient = null;
    }
    // 从 body 移除面板
    if (this.$refs.panel && this.$refs.panel.parentNode === document.body) {
      document.body.removeChild(this.$refs.panel);
    }
  },
  watch: {
    visible(v) {
      if (v) {
        // 第一次打开时初始化位置（如果还没设置过）
        if (this.x === 0) {
          this.x = Math.max(0, window.innerWidth - this.width - 20);
          this.y = Math.max(0, 100);
        }
        if (!this.aiClient) {
          this.initAiClient();
        } else {
          this.send('请帮我重新填写这个表单。');
        }
      } else {
        this.messages = [];
        if (this.aiClient) {
          this.aiClient.disconnect();
          this.aiClient = null;
        }
      }
    }
  },
  methods: {
    // 创建 AiClient（form 场景），首次打开时调用，自动发送开场白
    initAiClient() {
      this.aiClient = new AiClient({
        scene: 'form',
        // 前端工具执行上下文：moduleCode/storeName 供子表/主表工具定位 store 模块
        getFrontendToolExtra: () => ({
          moduleCode: this.moduleCode,
          storeName: this.currentStoreName,
          formEdit: this.$parent
        }),
        onBlock: (b) => this.onFormBlock(b),
        onError: (msg) => this.appendBlock({ type: 'text', text: '⚠️ ' + (msg || '错误') })
      });
      this.send('请帮我填写这个表单。');
    },
    // 处理 form 场景的 block 事件
    onFormBlock(b) {
      if (!b || !b.type) return;
      switch (b.type) {
        case 'fill':
          this.$emit('fill', b.fields);
          this.appendBlock({ type: 'text', text: '✅ 已填入 ' + Object.keys(b.fields || {}).length + ' 个字段，请在表单复核' });
          break;
        case 'subtable':
          this.$emit('subtable', { path: b.path, rows: b.rows });
          this.appendBlock({ type: 'text', text: '✅ 已添加 ' + (b.rows || []).length + ' 行到子表 ' + b.path + '，请在表单复核' });
          break;
        case 'tool_call':
          // 工具调用通知（仅展示，实际执行由 frontend_tool_call 事件触发代理层）
          this.appendBlock({ type: 'tool_call', tool: b.tool, args: b.args, summary: '执行中…' });
          break;
        case 'tool_result':
          // 工具结果：更新对应的 tool_call 块的 summary
          this.updateLastToolCallSummary(b.tool, b.summary);
          break;
        case 'conversation':
        case 'done':
        case 'heartbeat':
          // 忽略
          break;
        case 'error':
          this.appendBlock({ type: 'text', text: '⚠️ ' + (b.text || '错误') });
          break;
        default:
          // text/thinking/navigate 等
          this.appendBlock(b);
      }
    },
    // 拖动
    startDrag(e) {
      if (e.target.classList.contains('form-ai-close') ||
          e.target.classList.contains('form-ai-resize-btn')) return;
      this.isDragging = true;
      this.dragStartX = e.clientX;
      this.dragStartY = e.clientY;
      this.dragStartPanelX = this.x;
      this.dragStartPanelY = this.y;
    },
    // 调整大小
    startResize(e, dir) {
      this.isResizing = true;
      this.resizeDir = dir;
      this.resizeStartX = e.clientX;
      this.resizeStartY = e.clientY;
      this.resizeStartWidth = this.width;
      this.resizeStartHeight = this.height;
      this.resizeStartPanelX = this.x;
      this.resizeStartPanelY = this.y;
      e.preventDefault();
      e.stopPropagation();
    },
    onMouseMove(e) {
      if (this.isDragging) {
        const dx = e.clientX - this.dragStartX;
        const dy = e.clientY - this.dragStartY;
        this.x = Math.max(0, Math.min(window.innerWidth - this.width, this.dragStartPanelX + dx));
        this.y = Math.max(0, Math.min(window.innerHeight - this.height, this.dragStartPanelY + dy));
      }
      if (this.isResizing) {
        const dx = e.clientX - this.resizeStartX;
        const dy = e.clientY - this.resizeStartY;
        const dir = this.resizeDir;

        // 处理宽度变化（东/西方向）
        if (dir.includes('e')) {
          this.width = Math.max(300, Math.min(window.innerWidth - this.x, this.resizeStartWidth + dx));
        } else if (dir.includes('w')) {
          const newWidth = Math.max(300, this.resizeStartWidth - dx);
          const maxWidth = this.resizeStartPanelX + this.resizeStartWidth;
          if (newWidth <= maxWidth) {
            this.width = newWidth;
            this.x = this.resizeStartPanelX + this.resizeStartWidth - newWidth;
          }
        }

        // 处理高度变化（南/北方向）
        if (dir.includes('s')) {
          this.height = Math.max(200, Math.min(window.innerHeight - this.y, this.resizeStartHeight + dy));
        } else if (dir.includes('n')) {
          const newHeight = Math.max(200, this.resizeStartHeight - dy);
          const maxHeight = this.resizeStartPanelY + this.resizeStartHeight;
          if (newHeight <= maxHeight) {
            this.height = newHeight;
            this.y = this.resizeStartPanelY + this.resizeStartHeight - newHeight;
          }
        }
      }
    },
    onMouseUp() {
      this.isDragging = false;
      this.isResizing = false;
    },
    onWindowResize() {
      // 确保面板不超出屏幕
      this.x = Math.max(0, Math.min(this.x, window.innerWidth - this.width));
      this.y = Math.max(0, Math.min(this.y, window.innerHeight - this.height));
    },
    // 切换大小（恢复默认/最小化）
    toggleSize() {
      if (this.isExpanded) {
        // 最小化
        this._prevWidth = this.width;
        this._prevHeight = this.height;
        this.width = 300;
        this.height = 400;
        this.isExpanded = false;
      } else {
        // 恢复
        this.width = this._prevWidth || 380;
        this.height = this._prevHeight || 500;
        this.isExpanded = true;
      }
    },
    appendBlock(block) {
      const last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') {
        this.messages.push({ role: 'assistant', blocks: [] });
      }
      const cur = this.messages[this.messages.length - 1];
      if (block.type === 'text' && cur.blocks.length && cur.blocks[cur.blocks.length - 1].type === 'text') {
        cur.blocks[cur.blocks.length - 1].text += block.text;
      } else {
        cur.blocks.push(block);
      }
      // AiMessageList 内部 watch messages 自动滚动到底部，无需手动滚动
    },
    // 更新最后一个对应工具的 tool_call 块 summary（用 $set 保证响应式触发）
    updateLastToolCallSummary(tool, summary) {
      const last = this.messages[this.messages.length - 1];
      if (!last || last.role !== 'assistant') return;
      for (let i = last.blocks.length - 1; i >= 0; i--) {
        if (last.blocks[i].type === 'tool_call' && last.blocks[i].tool === tool) {
          this.$set(last.blocks[i], 'summary', summary);
          return;
        }
      }
    },
    send(text) {
      const t = (text || '').trim();
      if (!t) return;
      if (!this.aiClient) return;
      this.messages.push({ role: 'user', blocks: [{ type: 'text', text: t }] });
      this.messages.push({ role: 'assistant', blocks: [] });
      this.aiClient.sendForm(this.moduleCode, t, this.formData)
        .catch(e => this.appendBlock({ type: 'text', text: '⚠️ 发送失败：' + (e.message || e) }));
    },
    // 粘贴图片：AiInput 已读取图片转 base64 并 emit paste，这里调 AnalyzeImage 识别，
    // 识别结果走 onFormBlock 回调（text/fill/subtable 等）
    async onPaste(pasteData) {
      this.analyzingImage = true;
      this.appendBlock({ type: 'text', text: '🖼️ 正在识别图片内容…' });
      try {
        await this.aiClient.analyzeImage(pasteData.base64, pasteData.mime);
      } catch (err) {
        this.appendBlock({ type: 'text', text: '⚠️ 图片识别失败：' + (err.message || err) });
      } finally {
        this.analyzingImage = false;
      }
    },
    close() {
      this.$emit('close');
    }
  }
};
</script>

<style scoped>
.form-ai-panel {
  position: fixed;
  background: #fff;
  display: flex;
  flex-direction: column;
  border: 1px solid #E8E8E8;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.10), 0 4px 8px rgba(0, 0, 0, 0.06);
  z-index: 3000;
  overflow: hidden;
  user-select: none;
}
.form-ai-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 9px 16px;
  border-bottom: 1px solid #F0F0F0;
  background: #FAFAFA;
  color: #262626;
  flex-shrink: 0;
  cursor: move;
}
.form-ai-header-btns {
  display: flex;
  gap: 8px;
  align-items: center;
}
.form-ai-title {
  font-weight: 600;
  font-size: 14px;
}
.form-ai-close {
  cursor: pointer;
  font-size: 16px;
  color: #8C8C8C;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.2s;
}
.form-ai-close:hover {
  color: #262626;
  background: #F0F0F0;
}
.form-ai-resize-btn {
  cursor: pointer;
  font-size: 14px;
  color: #8C8C8C;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.2s;
}
.form-ai-resize-btn:hover {
  color: #262626;
  background: #F0F0F0;
}
/* AiMessageList 的 .ai-msg-list 自带 flex:1/overflow，补 min-height:0 防 flex 溢出导致滚动失效 */
.form-ai-panel /deep/ .ai-msg-list {
  min-height: 0;
}
/* 调整大小的手柄 */
.form-ai-resize-handle {
  position: absolute;
  z-index: 10;
}
/* 四边 */
.form-ai-resize-handle-n {
  top: 0;
  left: 8px;
  right: 8px;
  height: 4px;
  cursor: n-resize;
}
.form-ai-resize-handle-e {
  top: 8px;
  right: 0;
  bottom: 8px;
  width: 4px;
  cursor: e-resize;
}
.form-ai-resize-handle-s {
  bottom: 0;
  left: 8px;
  right: 8px;
  height: 4px;
  cursor: s-resize;
}
.form-ai-resize-handle-w {
  top: 8px;
  left: 0;
  bottom: 8px;
  width: 4px;
  cursor: w-resize;
}
/* 四角 */
.form-ai-resize-handle-ne {
  top: 0;
  right: 0;
  width: 8px;
  height: 8px;
  cursor: ne-resize;
}
.form-ai-resize-handle-nw {
  top: 0;
  left: 0;
  width: 8px;
  height: 8px;
  cursor: nw-resize;
}
.form-ai-resize-handle-se {
  bottom: 0;
  right: 0;
  width: 8px;
  height: 8px;
  cursor: se-resize;
}
.form-ai-resize-handle-sw {
  bottom: 0;
  left: 0;
  width: 8px;
  height: 8px;
  cursor: sw-resize;
}
/* hover 效果 */
.form-ai-resize-handle:hover {
  background: rgba(47, 84, 235, 0.3);
}
</style>
