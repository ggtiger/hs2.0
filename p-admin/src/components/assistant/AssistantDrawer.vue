<template>
  <div>
    <!-- 可拖动按钮（全局入口） -->
    <button
      class="asst-fab"
      ref="fabBtn"
      :style="fabStyle"
      @click="toggle"
      title="智能助理"
    >
      <svg viewBox="0 0 24 24" width="20" height="20" fill="currentColor">
        <path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z"/>
        <path d="M7 9h10v2H7zm0-3h7v2H7z"/>
      </svg>
    </button>
    <!-- 浮动面板（可拖动标题栏，不占全高，避免遮挡内容） -->
    <div v-if="visible" class="asst-panel" :style="panelStyle" ref="panel">
      <div class="asst-header" @mousedown="startDrag">
        <span class="asst-title">🤖 智能助理</span>
        <span class="asst-close" @click="toggle">✕</span>
      </div>
      <div class="asst-agents">
        <div
          class="asst-agent"
          :class="{ active: currentAgent === 'assistant' }"
          @click="setAgent('assistant')"
          title="通用助理"
        >
          <span class="asst-agent-icon">🤖</span>
          <span class="asst-agent-name">助理</span>
        </div>
        <div
          class="asst-agent"
          :class="{ active: currentAgent === 'form', disabled: !formActive }"
          :title="formActive ? 'AI 填报：操作当前表单' : '请在表单页使用'"
          @click="formActive && setAgent('form')"
        >
          <span class="asst-agent-icon">📝</span>
          <span class="asst-agent-name">填报</span>
        </div>
        <div
          class="asst-agent"
          :class="{ active: currentAgent === 'sfc', disabled: !sfcActive }"
          :title="sfcActive ? '开发：SFC 代码生成' : '请在 SFC 编辑页使用'"
          @click="sfcActive && setAgent('sfc')"
        >
          <span class="asst-agent-icon">💻</span>
          <span class="asst-agent-name">开发</span>
        </div>
      </div>
      <AiMessageList
        ref="msgList"
        :messages="messages"
        :scene="currentAgent === 'sfc' ? 'sfc' : currentAgent === 'form' ? 'form' : 'assistant'"
        :theme="currentAgent === 'sfc' ? 'dark' : 'light'"
        @apply-code="onApplyCode"
        @confirm-sql="onConfirmSql"
      />
      <AiInput ref="aiInput" :loading="isLoading" :placeholder="placeholder" @send="onSend" @paste="onPaste" />
      <div class="asst-resize asst-resize-n" @mousedown="startResize('n', $event)"></div>
      <div class="asst-resize asst-resize-s" @mousedown="startResize('s', $event)"></div>
      <div class="asst-resize asst-resize-w" @mousedown="startResize('w', $event)"></div>
      <div class="asst-resize asst-resize-e" @mousedown="startResize('e', $event)"></div>
    </div>
  </div>
</template>

<script>
import AiMessageList from '@/components/ai/AiMessageList.vue';
import AiInput from '@/components/ai/AiInput.vue';
import { executeMetadataSql } from '@/api/sfc-ai';

export default {
  name: 'AssistantDrawer',
  components: { AiMessageList, AiInput },
  data() {
    return {
      // FAB 按钮位置
      fabX: window.innerWidth - 80,
      fabY: 800,
      fabIsDragging: false,
      fabDragStartX: 0,
      fabDragStartY: 0,
      fabDragStartFabX: 0,
      fabDragStartFabY: 0,
      // 浮动面板位置（初始右上，不贴边不遮内容）
      panelX: window.innerWidth - 450,
      panelY: 80,
      panelW: 430,
      panelH: 600,
      panelIsDragging: false,
      panelDragStartX: 0,
      panelDragStartY: 0,
      panelDragStartPX: 0,
      panelDragStartPY: 0
    };
  },
  computed: {
    visible() {
      return this.$store.state.assistant.visible;
    },
    currentAgent() {
      return this.$store.state.assistant.currentAgent;
    },
    messages() {
      var s = this.$store.state.assistant;
      if (s.currentAgent === 'form') return s.formMessages;
      if (s.currentAgent === 'sfc') return s.sfcMessages;
      return s.assistantMessages;
    },
    isLoading() {
      return this.$store.state.assistant.isLoading;
    },
    formActive() {
      return this.$store.getters['formContext/isActive'];
    },
    formModuleCode() {
      return this.$store.state.formContext.moduleCode;
    },
    sfcActive() {
      return this.$store.getters['sfcContext/isActive'];
    },
    placeholder() {
      if (this.currentAgent === 'form') return '描述要填的内容，如：客户名ABC公司...';
      if (this.currentAgent === 'sfc') return '描述要生成的代码，如：写单表CRUD列表页...';
      return '问点什么…';
    },
    fabStyle() {
      return {
        position: 'fixed',
        left: this.fabX + 'px',
        top: this.fabY + 'px',
        zIndex: 2000
      };
    },
    panelStyle() {
      return {
        position: 'fixed',
        left: this.panelX + 'px',
        top: this.panelY + 'px',
        width: this.panelW + 'px',
        height: this.panelH + 'px',
        zIndex: 2000
      };
    }
  },
  watch: {
    // 填报不可用（离开表单页）时，自动切回通用助理
    formActive(v) {
      if (!v && this.currentAgent === 'form') {
        this.setAgent('assistant');
      }
    },
    // 模块变化时清空填报会话（同模块切换不清，保留会话）
    formModuleCode(newCode, oldCode) {
      if (oldCode && newCode !== oldCode) {
        this.$store.commit('assistant/RESET_FORM');
      }
    },
    // 离开 SFC 编辑页时，自动切回通用助理
    sfcActive(v) {
      if (!v && this.currentAgent === 'sfc') {
        this.setAgent('assistant');
      }
    }
  },
  mounted() {
    this.initFabDrag();
  },
  beforeDestroy() {
    this.cleanupFabDrag();
    this.cleanupPanelDrag();
  },
  methods: {
    toggle() {
      if (this.fabIsDragging) return;
      this.$store.dispatch('assistant/toggle');
    },
    setAgent(agent) {
      this.$store.dispatch('assistant/setAgent', agent);
    },
    onSend(text) {
      this.$store.dispatch('assistant/send', text);
    },
    // SFC 代码块应用：归一化 payload（AiMessageList 的 {block,mode} -> edit.vue 期望的 {code,mode,searchReplace}），转发到 sfcContext.editorRef.onApplyCode
    onApplyCode(event) {
      if (this.currentAgent !== 'sfc') return;
      var block = event.block || {};
      var payload = {
        code: event.code || block.code || '',
        mode: event.mode
      };
      if (block.searchReplace && block.searchReplace.length > 0) {
        payload.searchReplace = block.searchReplace;
      }
      var sc = this.$store.state.sfcContext;
      if (sc.editorRef && sc.editorRef.onApplyCode) {
        sc.editorRef.onApplyCode(payload);
      }
    },
    // metadata-sql 确认执行：executeMetadataSql + setSqlStatus + 结果回传 AI
    async onConfirmSql(code) {
      try {
        var result = await executeMetadataSql(code);
        if (this.$refs.msgList) {
          this.$refs.msgList.setSqlStatus(code, 'success', '执行成功，影响 ' + (result.affectedRows || 0) + ' 行');
        }
        var followUp = '元数据 SQL 已执行成功，影响 ' + (result.affectedRows || 0) + ' 行。请基于更新后的元数据继续。';
        this.$store.dispatch('assistant/send', followUp);
      } catch (e) {
        if (this.$refs.msgList) {
          this.$refs.msgList.setSqlStatus(code, 'failed', '执行失败: ' + (e.message || e));
        }
      }
    },
    async onPaste(pasteData) {
      // 消息区显示图片 + "识别中"提示，识别完填输入框
      this.$store.commit('assistant/PUSH_MESSAGE', { role: 'user', blocks: [{ type: 'image', dataUrl: pasteData.dataUrl }] });
      this.$store.commit('assistant/PUSH_MESSAGE', { role: 'assistant', blocks: [{ type: 'text', text: '识别中...' }] });
      this.$store.commit('assistant/SET_LOADING', true);
      try {
        var text = await this.$store.dispatch('assistant/analyzeImage', {
          base64: pasteData.base64,
          mime: pasteData.mime
        });
        this.$store.commit('assistant/UPDATE_LAST_TEXT', '识别完成，已填入输入框，请确认后发送');
        if (text && this.$refs.aiInput) {
          this.$refs.aiInput.setText(text);
        }
      } catch (e) {
        this.$store.commit('assistant/UPDATE_LAST_TEXT', '⚠️ 识别失败：' + (e && e.message ? e.message : ''));
      } finally {
        this.$store.commit('assistant/SET_LOADING', false);
      }
    },
    // 四方向调整面板高宽（n/s 调高，w/e 调宽）
    startResize(dir, e) {
      if (e.button !== 0) return;
      e.preventDefault();
      e.stopPropagation();
      var startX = e.clientX;
      var startY = e.clientY;
      var startPX = this.panelX;
      var startPY = this.panelY;
      var startPW = this.panelW;
      var startPH = this.panelH;
      var minW = 320;
      var minH = 300;
      var self = this;
      var onMouseMove = function(ev) {
        var dx = ev.clientX - startX;
        var dy = ev.clientY - startY;
        if (dir === 'e') {
          self.panelW = Math.max(minW, startPW + dx);
        } else if (dir === 'w') {
          self.panelW = Math.max(minW, startPW - dx);
          self.panelX = startPX + (startPW - self.panelW);
        } else if (dir === 's') {
          self.panelH = Math.max(minH, startPH + dy);
        } else if (dir === 'n') {
          self.panelH = Math.max(minH, startPH - dy);
          self.panelY = startPY + (startPH - self.panelH);
        }
      };
      var onMouseUp = function() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
      };
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
    },
    // ===== FAB 按钮拖动 =====
    initFabDrag() {
      var btn = this.$refs.fabBtn;
      if (!btn) return;
      var self = this;
      var onMouseDown = function(e) {
        if (e.button !== 0) return;
        self.fabIsDragging = false;
        self.fabDragStartX = e.clientX;
        self.fabDragStartY = e.clientY;
        self.fabDragStartFabX = self.fabX;
        self.fabDragStartFabY = self.fabY;
        var onMouseMove = function(e) {
          var dx = e.clientX - self.fabDragStartX;
          var dy = e.clientY - self.fabDragStartY;
          if (!self.fabIsDragging && (Math.abs(dx) > 3 || Math.abs(dy) > 3)) {
            self.fabIsDragging = true;
          }
          if (self.fabIsDragging) {
            self.fabX = Math.max(0, Math.min(window.innerWidth - 56, self.fabDragStartFabX + dx));
            self.fabY = Math.max(0, Math.min(window.innerHeight - 56, self.fabDragStartFabY + dy));
          }
        };
        var onMouseUp = function() {
          document.removeEventListener('mousemove', onMouseMove);
          document.removeEventListener('mouseup', onMouseUp);
          setTimeout(function() { self.fabIsDragging = false }, 100);
        };
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
      };
      btn.addEventListener('mousedown', onMouseDown);
      this._fabDragCleanup = function() { btn.removeEventListener('mousedown', onMouseDown) };
    },
    cleanupFabDrag() {
      if (this._fabDragCleanup) {
        this._fabDragCleanup();
        this._fabDragCleanup = null;
      }
    },
    // ===== 浮动面板拖动（标题栏） =====
    startDrag(e) {
      // 点关闭按钮不拖动
      if (e.target.classList.contains('asst-close')) return;
      if (e.button !== 0) return;
      this.panelIsDragging = false;
      this.panelDragStartX = e.clientX;
      this.panelDragStartY = e.clientY;
      this.panelDragStartPX = this.panelX;
      this.panelDragStartPY = this.panelY;
      var self = this;
      var onMouseMove = function(e) {
        var dx = e.clientX - self.panelDragStartX;
        var dy = e.clientY - self.panelDragStartY;
        if (!self.panelIsDragging && (Math.abs(dx) > 3 || Math.abs(dy) > 3)) {
          self.panelIsDragging = true;
        }
        if (self.panelIsDragging) {
          self.panelX = Math.max(0, Math.min(window.innerWidth - self.panelW, self.panelDragStartPX + dx));
          self.panelY = Math.max(0, Math.min(window.innerHeight - 60, self.panelDragStartPY + dy));
        }
      };
      var onMouseUp = function() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        self.panelIsDragging = false;
      };
      document.addEventListener('mousemove', onMouseMove);
      document.addEventListener('mouseup', onMouseUp);
      this._panelDragCleanup = function() {
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
      };
    },
    cleanupPanelDrag() {
      if (this._panelDragCleanup) {
        this._panelDragCleanup();
        this._panelDragCleanup = null;
      }
    }
  }
};
</script>

<style scoped>
.asst-fab {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: none;
  background: linear-gradient(135deg, #21AB6E 0%, #157A4E 100%);
  color: #fff;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(33, 171, 110, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s, box-shadow 0.2s;
  user-select: none;
}
.asst-fab:hover {
  transform: scale(1.1);
  box-shadow: 0 6px 20px rgba(33, 171, 110, 0.5);
}
.asst-fab:active {
  transform: scale(0.95);
}
/* 浮动面板 */
.asst-panel {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 6px 24px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #e8e8e8;
}
.asst-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  border-bottom: 1px solid #F0F0F0;
  background: linear-gradient(135deg, #21AB6E 0%, #157A4E 100%);
  color: #fff;
  cursor: move;
  user-select: none;
}
.asst-title {
  font-weight: 600;
  font-size: 14px;
}
.asst-close {
  cursor: pointer;
  font-size: 16px;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: background 0.2s;
}
.asst-close:hover {
  background: rgba(255, 255, 255, 0.2);
}
.asst-agents {
  display: flex;
  gap: 8px;
  padding: 8px 12px;
  border-bottom: 1px solid #F0F0F0;
  background: #FAFAFA;
}
.asst-agent {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 13px;
  color: #595959;
  cursor: pointer;
  transition: all 0.2s;
  user-select: none;
}
.asst-agent:hover {
  background: #E9F8F0;
  color: #21AB6E;
}
.asst-agent.active {
  background: linear-gradient(135deg, #21AB6E 0%, #157A4E 100%);
  color: #fff;
}
.asst-agent-icon {
  font-size: 16px;
  line-height: 1;
}
.asst-agent.disabled {
  color: #C0C4CC;
  cursor: not-allowed;
}
.asst-agent.disabled:hover {
  background: transparent;
  color: #C0C4CC;
}
/* 四方向调整大小 handle */
.asst-resize {
  position: absolute;
  z-index: 10;
}
.asst-resize-n {
  top: 0;
  left: 8px;
  right: 8px;
  height: 4px;
  cursor: ns-resize;
}
.asst-resize-s {
  bottom: 0;
  left: 8px;
  right: 8px;
  height: 4px;
  cursor: ns-resize;
}
.asst-resize-w {
  top: 8px;
  bottom: 8px;
  left: 0;
  width: 4px;
  cursor: ew-resize;
}
.asst-resize-e {
  top: 8px;
  bottom: 8px;
  right: 0;
  width: 4px;
  cursor: ew-resize;
}
</style>
