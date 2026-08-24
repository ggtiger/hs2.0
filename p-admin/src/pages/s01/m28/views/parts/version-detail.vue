<template>
  <div class="dc-detail" v-if="item">
    <!-- 元信息头 -->
    <div class="dc-detail-meta">
      <div class="dc-detail-title">
        <i class="h-icon-clock"></i>
        <span class="dc-detail-code">{{ item.OBJCODE || '-' }}</span>
        <span :class="['dc-detail-type', optypeClass]">{{ item.OPTYPE || '?' }}</span>
        <span v-if="item.VERSION" class="dc-detail-version">v{{ item.VERSION }}</span>
        <span v-if="item.PINNED == 1" class="dc-detail-pin" title="已置顶">★</span>
      </div>
      <div class="dc-detail-sub" v-if="item.OBJNAME || item.OBJTYPE">
        <span v-if="item.OBJNAME"><i class="h-icon-document"></i> {{ item.OBJNAME }}</span>
        <span v-if="item.OBJTYPE" class="dc-detail-table">
          <i class="h-icon-folder"></i> {{ item.OBJTYPE }}
        </span>
        <span v-if="item.SRCTABLE" class="dc-detail-table">
          <i class="h-icon-database"></i> {{ item.SRCTABLE }}
        </span>
      </div>
      <div class="dc-detail-ops">
        <button class="dc-detail-btn primary" @click="openVersionCenter">
          <i class="h-icon-history"></i> 版本中心
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
            <i class="h-icon-info"></i> 版本元数据
          </span>
        </header>
        <div class="dc-prop-grid">
          <div class="dc-prop-item" v-if="item.TAG">
            <span class="dc-prop-label">标签</span>
            <span class="dc-prop-value mono tag">{{ item.TAG }}</span>
          </div>
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

      <!-- 变更说明 -->
      <section class="dc-detail-section" v-if="item.CHANGENOTE">
        <header class="dc-detail-section-head">
          <span class="dc-detail-section-title">
            <i class="h-icon-edit"></i> 变更说明
          </span>
        </header>
        <div class="dc-changenote">{{ item.CHANGENOTE }}</div>
      </section>

      <!-- 操作提示 -->
      <section class="dc-detail-section">
        <div class="dc-hint">
          <i class="h-icon-info"></i>
          <span>回滚/对比/打标操作请到版本中心完成</span>
        </div>
      </section>
    </div>
  </div>

  <div v-else class="dc-detail-empty">
    <i class="h-icon-info"></i>
    <p>从中间列表选择一个版本查看详情</p>
  </div>
</template>

<script>
export default {
  name: 'DcVersionDetail',
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  computed: {
    optypeClass() {
      var t = (this.item && this.item.OPTYPE) || '';
      if (t === 'create') return 'op-create';
      if (t === 'update') return 'op-update';
      if (t === 'delete') return 'op-delete';
      return 'op-other';
    }
  },
  methods: {
    openVersionCenter() {
      this.$emit('open-editor', { type: 'version-center' });
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'ver_' + (this.item && this.item.ID),
        label: '版本 ' + (this.item && this.item.OBJCODE) + ' v' + (this.item && this.item.VERSION),
        icon: 'h-icon-clock'
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
  &.op-create { background: #f6ffed; color: #52c41a; }
  &.op-update { background: #e6f7ff; color: #1890ff; }
  &.op-delete { background: #fff1f0; color: #ff4d4f; }
  &.op-other { background: #f5f5f5; color: #999; }
}
.dc-detail-version {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 3px;
  background: #f9f0ff;
  color: #722ed1;
  font-weight: 600;
}
.dc-detail-pin {
  color: #fa8c16;
  font-size: 16px;
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
    &.mono { font-family: 'SF Mono', Menlo, Consolas, monospace; }
    &.tag { color: #722ed1; font-weight: 600; }
  }
}
.dc-changenote {
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
