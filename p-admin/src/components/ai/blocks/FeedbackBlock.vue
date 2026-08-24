<template>
  <div class="ai-feedback-block" v-if="visible">
    <div class="ai-feedback-row" v-if="!submitted">
      <span class="ai-feedback-label">这条回答：</span>
      <button
        class="ai-feedback-btn"
        :class="{ active: chosen === 'up' }"
        title="👍 准确，作为好示例沉淀"
        @click="choose('up')"
      >👍</button>
      <button
        class="ai-feedback-btn"
        :class="{ active: chosen === 'down' }"
        title="👎 不准确，需要改进"
        @click="choose('down')"
      >👎</button>
      <button
        v-if="chosen === 'down'"
        class="ai-feedback-submit"
        @click="submit"
        :disabled="loading"
      >{{ loading ? '提交中…' : '提交反馈' }}</button>
    </div>

    <!-- 👎 展开后的问题标签选择 -->
    <div class="ai-feedback-tags" v-if="chosen === 'down' && !submitted">
      <span class="ai-feedback-tag-label">问题类型(可多选)：</span>
      <label
        v-for="tag in issueTagOptions"
        :key="tag.value"
        class="ai-feedback-tag"
        :class="{ active: selectedTags.includes(tag.value) }"
      >
        <input type="checkbox" :value="tag.value" v-model="selectedTags" />
        {{ tag.label }}
      </label>
    </div>

    <!-- 👍 展开后的"提升为示例"选项 -->
    <div class="ai-feedback-promote" v-if="chosen === 'up' && !submitted">
      <label class="ai-feedback-checkbox">
        <input type="checkbox" v-model="promoteAsExample" />
        同时沉淀为"参考示例"(后续 AI 同类问题会检索采用)
      </label>
      <div class="ai-feedback-score" v-if="promoteAsExample">
        <span>质量评分：</span>
        <select v-model.number="qualityScore">
          <option :value="3">3 - 还可以</option>
          <option :value="4">4 - 不错</option>
          <option :value="5">5 - 极好</option>
        </select>
      </div>
    </div>

    <!-- 可选备注 -->
    <div class="ai-feedback-comment" v-if="chosen && !submitted">
      <textarea
        v-model="comment"
        placeholder="补充说明(可选，帮助系统改进)"
        rows="2"
        class="ai-feedback-textarea"
      ></textarea>
    </div>

    <!-- 提交完成提示 -->
    <div class="ai-feedback-done" v-if="submitted">
      <span class="ai-feedback-thanks">✓ 感谢反馈，已记录到记忆库</span>
      <button
        v-if="lastFeedbackId && chosen === 'up'"
        class="ai-feedback-promote-btn"
        @click="$emit('promote', lastFeedbackId)"
      >提升为示例</button>
    </div>
  </div>
</template>

<script>
import { submitFeedback } from '@/api/aidev';

export default {
  name: 'FeedbackBlock',
  props: {
    // 是否显示反馈 UI(默认显示)
    visible: { type: Boolean, default: true },
    // 当前场景(assistant/aidev/wizard/sfc/...)
    sceneCode: { type: String, default: 'assistant' },
    // 资产类型(sfc/sql/csharp/metadata/wizard/general)
    assetType: { type: String, default: 'general' },
    // 当前会话 ID(可空)
    sessionId: { type: String, default: '' },
    // 用户原始请求
    userRequest: { type: String, default: '' },
    // AI 原始输出
    originalOutput: { type: String, default: '' },
    // 用户最终采用的版本(若用户编辑过)
    finalOutput: { type: String, default: '' },
    // 差异文本(用户编辑过时自动计算)
    diffText: { type: String, default: '' }
  },
  data() {
    return {
      chosen: '',                // 'up' / 'down' / ''
      selectedTags: [],          // 👎 的问题标签
      promoteAsExample: false,   // 👍 是否沉淀为示例
      qualityScore: 4,           // 👍 评分
      comment: '',               // 备注
      submitted: false,
      loading: false,
      lastFeedbackId: null,
      issueTagOptions: [
        { value: 'naming', label: '命名问题' },
        { value: 'syntax', label: '语法错误' },
        { value: 'logic', label: '逻辑不对' },
        { value: 'missing_field', label: '漏字段' },
        { value: 'permission', label: '权限/越权' },
        { value: 'style', label: '风格不符' },
        { value: 'redundant', label: '冗余/重复' },
        { value: 'other', label: '其他' }
      ]
    };
  },
  methods: {
    choose(type) {
      if (this.chosen === type) {
        // 重复点击同一按钮: 取消选择
        this.chosen = '';
        return;
      }
      this.chosen = type;
      // 👍 默认勾选"沉淀为示例", 评分 4
      if (type === 'up') {
        this.promoteAsExample = true;
        this.qualityScore = 4;
      }
      // 直接提交(简单反馈不打字也行)
      if (type === 'up' && !this.comment) {
        // 给用户 0.5s 看到展开内容再提交
        // 不自动提交, 让用户决定
      }
    },
    async submit() {
      if (this.loading) return;
      this.loading = true;
      try {
        const payload = {
          sessionId: this.sessionId,
          sceneCode: this.sceneCode,
          assetType: this.assetType,
          feedbackType: this.chosen === 'up' ? 'thumbs_up' : 'thumbs_down',
          userRequest: this.userRequest,
          originalOutput: this.originalOutput,
          finalOutput: this.finalOutput || this.originalOutput,
          diffText: this.diffText,
          issueTags: this.chosen === 'down' ? this.selectedTags.join(',') : '',
          qualityScore: this.chosen === 'up' ? this.qualityScore : null,
          comment: this.comment
        };
        const res = await submitFeedback(payload);
        if (res && res.Code === 200) {
          this.submitted = true;
          this.$emit('submitted', { chosen: this.chosen, payload });
        } else {
          this.$emit('error', (res && res.Message) || '提交失败');
        }
      } catch (e) {
        // 静默失败不影响主流程
        console.warn('submitFeedback failed:', e);
        this.submitted = true; // 失败也标记完成, 不打扰用户
      } finally {
        this.loading = false;
      }
    }
  }
};
</script>

<style scoped>
.ai-feedback-block {
  margin-top: 6px;
  padding: 6px 10px;
  border-top: 1px dashed #e0e0e0;
  font-size: 12px;
  color: #666;
}
.ai-feedback-row {
  display: flex;
  align-items: center;
  gap: 6px;
}
.ai-feedback-label {
  color: #888;
  margin-right: 2px;
}
.ai-feedback-btn {
  border: 1px solid #e0e0e0;
  background: #fff;
  border-radius: 4px;
  padding: 2px 8px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.15s;
}
.ai-feedback-btn:hover {
  background: #f5f5f5;
  border-color: #d0d0d0;
}
.ai-feedback-btn.active {
  background: #e6f7ff;
  border-color: #1890ff;
  color: #1890ff;
}
.ai-feedback-submit {
  margin-left: auto;
  border: none;
  background: #1890ff;
  color: #fff;
  border-radius: 4px;
  padding: 4px 12px;
  cursor: pointer;
}
.ai-feedback-submit:disabled {
  background: #aaa;
  cursor: not-allowed;
}
.ai-feedback-tags,
.ai-feedback-promote,
.ai-feedback-comment {
  margin-top: 6px;
}
.ai-feedback-tag-label {
  color: #888;
  margin-right: 4px;
}
.ai-feedback-tag {
  display: inline-block;
  margin-right: 8px;
  padding: 2px 8px;
  border: 1px solid #e0e0e0;
  border-radius: 12px;
  cursor: pointer;
  font-size: 11px;
}
.ai-feedback-tag.active {
  background: #fff7e6;
  border-color: #fa8c16;
  color: #fa8c16;
}
.ai-feedback-tag input {
  display: none;
}
.ai-feedback-checkbox {
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
}
.ai-feedback-score {
  margin-top: 4px;
}
.ai-feedback-score select {
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  padding: 2px 4px;
}
.ai-feedback-textarea {
  width: 100%;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
  padding: 4px 6px;
  font-family: inherit;
  font-size: 12px;
  resize: vertical;
}
.ai-feedback-done {
  display: flex;
  align-items: center;
  gap: 10px;
  color: #52c41a;
}
.ai-feedback-promote-btn {
  margin-left: auto;
  border: 1px solid #52c41a;
  background: transparent;
  color: #52c41a;
  border-radius: 4px;
  padding: 2px 8px;
  cursor: pointer;
}
.ai-feedback-promote-btn:hover {
  background: #f6ffed;
}
</style>
