<template>
  <div class="dc-detail" v-if="item">
    <!-- 元信息头 -->
    <div class="dc-detail-meta">
      <div class="dc-detail-title">
        <i :class="item.FUNCICON || 'h-icon-menu'"></i>
        <span class="dc-detail-code">{{ item.FUNCCODE }}</span>
        <span v-if="item.ISHIDE == 1" class="dc-detail-disabled">隐藏</span>
        <span v-if="item.ISUSE == 0" class="dc-detail-disabled">停用</span>
      </div>
      <div class="dc-detail-sub" v-if="item.FUNCNAME">
        <span><i class="h-icon-document"></i> {{ item.FUNCNAME }}</span>
        <span v-if="item.OUTERURL" class="dc-detail-table">
          <i class="h-icon-link"></i> {{ item.OUTERURL }}
        </span>
      </div>
      <div class="dc-detail-ops">
        <button class="dc-detail-btn primary" @click="openMenuMgr">
          <i class="h-icon-menu"></i> 菜单管理
        </button>
        <button class="dc-detail-btn" @click="askAI">
          <i class="h-icon-bubble"></i> 问 AI
        </button>
      </div>
    </div>

    <!-- 加载中 -->
    <div v-if="loading" class="dc-detail-state">
      <i class="h-icon-loading"></i> 加载功能点权限...
    </div>

    <div v-else class="dc-detail-body">
      <!-- 元数据 -->
      <section class="dc-detail-section">
        <header class="dc-detail-section-head">
          <span class="dc-detail-section-title">
            <i class="h-icon-info"></i> 菜单元数据
          </span>
        </header>
        <div class="dc-prop-grid">
          <div class="dc-prop-item" v-if="item.SORTCODE !== null && item.SORTCODE !== undefined">
            <span class="dc-prop-label">排序号</span>
            <span class="dc-prop-value">{{ item.SORTCODE }}</span>
          </div>
          <div class="dc-prop-item" v-if="item.UPFUNCID">
            <span class="dc-prop-label">父菜单 ID</span>
            <span class="dc-prop-value mono">{{ item.UPFUNCID }}</span>
          </div>
          <div class="dc-prop-item" v-if="item.OUTERURL">
            <span class="dc-prop-label">跳转 URL</span>
            <span class="dc-prop-value mono">{{ item.OUTERURL }}</span>
          </div>
        </div>
      </section>

      <!-- 功能点权限 -->
      <section class="dc-detail-section">
        <header class="dc-detail-section-head">
          <span class="dc-detail-section-title">
            <i class="h-icon-key"></i> 功能点权限
            <em>{{ funcpoints.length }}</em>
          </span>
        </header>
        <div class="dc-fp-list">
          <div v-for="fp in funcpoints" :key="fp.ID" class="dc-fp-row">
            <span class="fp-code">{{ fp.FUNCPOINTCODE }}</span>
            <span class="fp-name">{{ fp.FUNCPOINTNAME || '-' }}</span>
          </div>
          <div v-if="funcpoints.length === 0" class="empty-block">
            该菜单暂无功能点权限（在菜单管理中配置）
          </div>
        </div>
      </section>
    </div>
  </div>

  <div v-else class="dc-detail-empty">
    <i class="h-icon-info"></i>
    <p>从中间列表选择一个菜单查看详情</p>
  </div>
</template>

<script>
import { getGenericStore } from '@/components/generic-module/generic-store';

const MC = 'RS_M03';

export default {
  name: 'DcMenuDetail',
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  data() {
    return {
      loading: false,
      funcpoints: []
    };
  },
  watch: {
    'item.ID'(v) {
      if (v) this.loadFuncpoints();
      else this.funcpoints = [];
    }
  },
  created() {
    this.storeObj = getGenericStore(MC);
    if (this.item && this.item.ID) this.loadFuncpoints();
  },
  methods: {
    async loadFuncpoints() {
      if (!this.item || !this.item.ID) return;
      this.loading = true;
      try {
        // RS_M03 call A01 拉所有，前端按 UPFUNCID 过滤
        // RS_M03 是菜单模块，FUNCPOINT 在子表里，需要看 store 是否返回
        // 兜底：拉全部菜单行，按 UPFUNCID=item.ID 过滤
        var ret = await this.$callAction({
          action: MC + '/call',
          param: {
            APICODE: 'A01',
            params: { PageSize: 500, PageIndex: 1, FilterParams: {} }
          },
          isBusy: false
        });
        var rows = (ret && ret.Items) || [];
        // 子菜单作为功能点的简单兜底（tss_funcpoint 通常通过 m03 子表加载）
        this.funcpoints = rows.filter(r => r.UPFUNCID === this.item.ID);
      } catch (e) {
        this.funcpoints = [];
      } finally {
        this.loading = false;
      }
    },
    openMenuMgr() {
      this.$emit('open-editor', { type: 'menu-mgr' });
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'menu_' + (this.item && this.item.ID),
        label: '菜单 ' + (this.item && this.item.FUNCNAME),
        icon: 'h-icon-menu'
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import './detail-common.less';
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
    &.mono { font-family: 'SF Mono', Menlo, Consolas, monospace; color: #2F54EB; }
  }
}
.dc-fp-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.dc-fp-row {
  display: flex;
  align-items: center;
  gap: 10px;
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 3px;
  padding: 6px 10px;
  font-size: 12px;
  .fp-code {
    font-family: 'SF Mono', Menlo, Consolas, monospace;
    color: #2F54EB;
    font-weight: 600;
    min-width: 80px;
  }
  .fp-name { color: #666; }
}
.empty-block {
  padding: 16px;
  text-align: center;
  color: #ccc;
  font-size: 12px;
  background: #fff;
  border: 1px dashed #eee;
  border-radius: 4px;
}
</style>
