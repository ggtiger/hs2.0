<template>
  <div class="dc-detail" v-if="item">
    <!-- 元信息头 -->
    <div class="dc-detail-meta">
      <div class="dc-detail-title">
        <i class="h-icon-folder"></i>
        <span class="dc-detail-code">{{ item.TEMPLATECODE }}</span>
        <span v-if="item.CATEGORY" :class="['dc-detail-type', 'cat-' + item.CATEGORY]">{{ item.CATEGORY }}</span>
        <span v-if="item.VERSION" class="dc-detail-version">v{{ item.VERSION }}</span>
        <span v-if="item.ENABLED == 0" class="dc-detail-disabled">禁用</span>
      </div>
      <div class="dc-detail-sub" v-if="item.TEMPLATENAME">
        <span><i class="h-icon-document"></i> {{ item.TEMPLATENAME }}</span>
        <span v-if="item.SOURCEINFO" class="dc-detail-table">
          <i class="h-icon-link"></i> 源 {{ item.SOURCEINFO }}
        </span>
      </div>
      <div class="dc-detail-ops">
        <button class="dc-detail-btn primary" @click="openMarket">
          <i class="h-icon-folder"></i> 模板市场
        </button>
        <button class="dc-detail-btn" @click="askAI">
          <i class="h-icon-bubble"></i> 问 AI
        </button>
      </div>
    </div>

    <div class="dc-detail-body">
      <!-- 元数据 -->
      <section class="dc-detail-section">
        <header class="dc-detail-section-head">
          <span class="dc-detail-section-title">
            <i class="h-icon-info"></i> 模板元数据
          </span>
        </header>
        <div class="dc-prop-grid">
          <div class="dc-prop-item" v-if="item.CREATER">
            <span class="dc-prop-label">创建人</span>
            <span class="dc-prop-value">{{ item.CREATER }}</span>
          </div>
          <div class="dc-prop-item" v-if="item.CREATETIME">
            <span class="dc-prop-label">创建时间</span>
            <span class="dc-prop-value">{{ item.CREATETIME }}</span>
          </div>
        </div>
      </section>

      <!-- 描述 -->
      <section class="dc-detail-section" v-if="item.DESCRIPTION">
        <header class="dc-detail-section-head">
          <span class="dc-detail-section-title">
            <i class="h-icon-text"></i> 模板描述
          </span>
        </header>
        <div class="dc-description">{{ item.DESCRIPTION }}</div>
      </section>

      <!-- 应用提示 -->
      <section class="dc-detail-section">
        <div class="dc-hint">
          <i class="h-icon-info"></i>
          <span>应用模板请在模板市场完成（AI 向导第 0 步可基于模板创建模块）</span>
        </div>
      </section>
    </div>
  </div>

  <div v-else class="dc-detail-empty">
    <i class="h-icon-info"></i>
    <p>从中间列表选择一个模板查看详情</p>
  </div>
</template>

<script>
export default {
  name: 'DcTemplateDetail',
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  methods: {
    openMarket() {
      this.$emit('open-editor', { type: 'template-market' });
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'tpl_' + (this.item && this.item.ID),
        label: '模板 ' + (this.item && this.item.TEMPLATECODE),
        icon: 'h-icon-folder'
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import './detail-common.less';
.dc-detail-type {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  font-weight: 600;
  text-transform: uppercase;
  background: #e6f7ff;
  color: #1890ff;
}
.dc-detail-version {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  background: #f9f0ff;
  color: #722ed1;
  font-weight: 600;
}
.dc-detail-disabled {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  background: #f5f5f5;
  color: #999;
  font-weight: 600;
}
.dc-prop-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px 16px;
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  padding: 10px 12px;
}
.dc-prop-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  .dc-prop-label {
    font-size: 10px;
    color: #999;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  .dc-prop-value {
    font-size: 12px;
    color: #333;
  }
}
.dc-description {
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  padding: 10px 12px;
  font-size: 12px;
  color: #333;
  line-height: 1.6;
  white-space: pre-wrap;
}
.dc-hint {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 12px;
  background: #fffbe6;
  border: 1px solid #ffe58f;
  border-radius: 4px;
  font-size: 11px;
  color: #ad6800;
  i { color: #fa8c16; }
}
</style>
