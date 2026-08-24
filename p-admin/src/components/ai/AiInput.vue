<template>
  <div class="ai-input" :class="{ 'ai-input-dark': theme === 'dark' }">
    <textarea
      ref="ta"
      v-model="text"
      class="ai-input-area"
      :placeholder="placeholder"
      :disabled="disabled"
      rows="3"
      @keydown="onKeyDown"
      @paste="onPaste"
    ></textarea>
    <button
      class="ai-input-send"
      :disabled="!text.trim() || loading || disabled"
      @click="send"
    >
      {{ loading ? '生成中...' : '发送' }}
    </button>
  </div>
</template>

<script>
export default {
  name: 'AiInput',
  props: {
    loading: { type: Boolean, default: false },
    placeholder: { type: String, default: '输入消息... (Enter 发送，Shift+Enter 换行)' },
    disabled: { type: Boolean, default: false },
    theme: { type: String, default: 'light' } // 'light' | 'dark'
  },
  data() {
    return { text: '' };
  },
  methods: {
    onKeyDown(e) {
      // IME 合成中（中文输入法拼音选词的回车）不触发发送，交给输入法
      if (e.isComposing || e.keyCode === 229) return;
      // Enter 发送，Shift+Enter 换行（通用习惯）
      if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.altKey && !e.metaKey) {
        e.preventDefault();
        this.send();
      }
    },
    onPaste(e) {
      var items = e.clipboardData && e.clipboardData.items;
      if (!items) return;
      for (var i = 0; i < items.length; i++) {
        var item = items[i];
        if (item.type && item.type.indexOf('image/') === 0) {
          var file = item.getAsFile();
          if (file) {
            e.preventDefault();
            this.readImage(file);
            return;
          }
        }
      }
    },
    readImage(file) {
      var reader = new FileReader();
      var self = this;
      reader.onload = function() {
        var base64 = reader.result;
        // reader.result 形如 "data:image/png;base64,xxxx"
        var mime = file.type || 'image/png';
        var base64Data = base64;
        var commaIdx = base64.indexOf(',');
        if (commaIdx !== -1) base64Data = base64.substring(commaIdx + 1);
        self.$emit('paste', { base64: base64Data, mime: mime, dataUrl: base64 });
      };
      reader.readAsDataURL(file);
    },
    send() {
      var t = this.text.trim();
      if (!t || this.loading || this.disabled) return;
      this.$emit('send', t);
      this.text = '';
    },
    setText(t) {
      this.text = t || '';
      this.focus();
    },
    focus() {
      if (this.$refs.ta) this.$refs.ta.focus();
    }
  }
};
</script>

<style lang="less" scoped>
.ai-input {
  display: flex;
  gap: 8px;
  padding: 8px;
  border-top: 1px solid #eee;
  flex-shrink: 0;
}
.ai-input-dark {
  background: #2d2d2d;
  border-top-color: #3c3c3c;
}
.ai-input-area {
  flex: 1;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 6px 10px;
  font-size: 13px;
  font-family: inherit;
  resize: none;
  outline: none;
  background: #fff;
  color: #333;
  &:focus {
    border-color: #409eff;
  }
}
.ai-input-dark .ai-input-area {
  background: #1e1e1e;
  border-color: #3c3c3c;
  color: #ddd;
  &:focus {
    border-color: #0a84ff;
  }
}
.ai-input-send {
  align-self: flex-end;
  padding: 6px 16px;
  background: #409eff;
  color: #fff;
  border: none;
  border-radius: 4px;
  font-size: 13px;
  cursor: pointer;
  flex-shrink: 0;
  &:hover:not(:disabled) {
    background: #66b1ff;
  }
  &:disabled {
    background: #c0c4cc;
    color: #fff;
    cursor: not-allowed;
  }
}
.ai-input-dark .ai-input-send {
  background: #0a84ff;
  &:hover:not(:disabled) {
    background: #0978e0;
  }
  &:disabled {
    background: #3c3c3c;
    color: #666;
  }
}
</style>
