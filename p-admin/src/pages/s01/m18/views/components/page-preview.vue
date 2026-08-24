<template>
  <div class="page-preview-panel">
    <div class="page-preview-header">
      <span class="page-preview-title">页面预览</span>
      <span class="page-preview-badge" v-if="pageConfig">{{ pageConfig.PAGETYPE }}</span>
      <span class="page-preview-badge" v-if="pageConfig" style="margin-left:4px">{{ pageConfig.PAGECODE }}</span>
    </div>
    <div class="page-preview-body" v-if="pageConfig" ref="previewBody" @mousemove="onBodyMouseMove" @mouseleave="hoverArea = ''" @click="onBodyClick">
      <generic-module
        :key="previewKey"
        :moduleCode="moduleCode"
        :pageCode="pageConfig.PAGECODE"
      ></generic-module>
      <!-- 搜索栏边框指示 (仅 list 页面, pointer-events:none 不遮挡交互) -->
      <div
        v-if="isListPage && queryOverlayHeight > 0"
        class="preview-overlay preview-overlay-query"
        :class="{ 'preview-overlay-hover': hoverArea === 'query' }"
        :style="queryOverlayStyle"
      >
        <div class="preview-overlay-setting" @click.stop="openUiSet('query')" title="设置查询字段">
          <i class="h-icon-setting"></i>
        </div>
      </div>
      <!-- 表格区域边框指示 (仅 list 页面) -->
      <div
        v-if="isListPage && tableOverlayHeight > 0"
        class="preview-overlay preview-overlay-table"
        :class="{ 'preview-overlay-hover': hoverArea === 'table' }"
        :style="tableOverlayStyle"
      >
        <div class="preview-overlay-setting" @click.stop="openUiSet('list')" title="设置列表字段">
          <i class="h-icon-setting"></i>
        </div>
      </div>
      <!-- 表单区域边框指示 (仅 form 页面) -->
      <div
        v-if="isFormPage && formOverlayHeight > 0"
        class="preview-overlay preview-overlay-form"
        :class="{ 'preview-overlay-hover': hoverArea === 'form' }"
        :style="formOverlayStyle"
      >
        <div class="preview-overlay-setting" @click.stop="openUiSet('form')" title="设置表单字段">
          <i class="h-icon-setting"></i>
        </div>
      </div>
      <!-- 子表(tableBlock)区域边框指示 (仅 form 页面, 每个子表一个) -->
      <div
        v-for="(sub, si) in subOverlays"
        :key="'subOvl' + si"
        v-if="isFormPage"
        class="preview-overlay preview-overlay-subtable"
        :class="{ 'preview-overlay-hover': hoverArea === 'sub_' + si }"
        :style="{ top: sub.top + 'px', height: sub.height + 'px' }"
      >
        <div class="preview-overlay-setting" @click.stop="openSubUiSet(sub.subtable)" :title="'设置子表字段：' + sub.subtable">
          <i class="h-icon-setting"></i>
        </div>
      </div>
    </div>
    <div v-else class="page-preview-empty">
      <div class="page-preview-empty-icon">☐</div>
      <p>选择左侧页面查看预览</p>
    </div>
  </div>
</template>

<script>
import GenericModule from '@/components/generic-module/generic-module.vue';

export default {
  name: 'page-preview',
  components: { GenericModule },
  props: {
    pageConfig: {
      type: Object,
      default: null
    },
    moduleCode: {
      type: String,
      default: ''
    }
  },
  data() {
    return {
      hoverArea: '',
      refreshTick: 0,
      queryOverlayTop: 0,
      queryOverlayHeight: 0,
      tableOverlayTop: 0,
      tableOverlayHeight: 0,
      formOverlayTop: 0,
      formOverlayHeight: 0,
      // 子表(tableBlock)覆盖层信息: [{ subtable, top, height }]
      subOverlays: []
    };
  },
  computed: {
    isListPage() {
      return this.pageConfig && (this.pageConfig.PAGETYPE === 'list' || this.pageConfig.PAGETYPE === 'select');
    },
    isFormPage() {
      return this.pageConfig && this.pageConfig.PAGETYPE === 'form';
    },
    moduleData() {
      var appState = this.$store.state.app;
      if (appState && appState.modules) {
        return appState.modules[this.moduleCode];
      }
      return null;
    },
    pageConfigJson() {
      if (!this.pageConfig || !this.pageConfig.PAGECONFIG) return {};
      try {
        return JSON.parse(this.pageConfig.PAGECONFIG);
      } catch (e) {
        return {};
      }
    },
    qryResourceName() {
      // PAGECONFIG.QRYPATH 存的是 PATHNAME，通过 MODPATH 找 RESOURCENAME；默认 QRY
      var pathname = this.pageConfigJson.QRYPATH || 'QRY';
      if (!this.moduleData || !this.moduleData.MODPATH) return '';
      var item = this.moduleData.MODPATH.find(function(p) { return p.PATHNAME === pathname });
      return item ? item.RESOURCENAME : '';
    },
    qqryResourceName() {
      var pathname = this.pageConfigJson.QQRYSPATH || 'QQRY';
      if (!this.moduleData || !this.moduleData.MODPATH) return '';
      var item = this.moduleData.MODPATH.find(function(p) { return p.PATHNAME === pathname });
      return item ? item.RESOURCENAME : '';
    },
    mainResourceName() {
      var pathname = this.pageConfigJson.MAINPATH || 'MAIN';
      if (!this.moduleData || !this.moduleData.MODPATH) return '';
      var item = this.moduleData.MODPATH.find(function(p) { return p.PATHNAME === pathname });
      return item ? item.RESOURCENAME : '';
    },
    previewKey() {
      if (!this.pageConfig) return '';
      return this.moduleCode + '_' + this.pageConfig.PAGECODE + '_' + this.pageConfig._idx_ + '_' + this.refreshTick;
    },
    queryOverlayStyle() {
      return {
        top: this.queryOverlayTop + 'px',
        height: this.queryOverlayHeight + 'px'
      };
    },
    tableOverlayStyle() {
      return {
        top: this.tableOverlayTop + 'px',
        height: this.tableOverlayHeight + 'px'
      };
    },
    formOverlayStyle() {
      return {
        top: this.formOverlayTop + 'px',
        height: this.formOverlayHeight + 'px'
      };
    }
  },
  watch: {
    pageConfig: {
      handler() {
        this.hoverArea = '';
        this.queryOverlayHeight = 0;
        this.tableOverlayHeight = 0;
        this.formOverlayHeight = 0;
        this.subOverlays = [];
        this.$nextTick(this.scheduleUpdatePositions);
      },
      deep: true
    }
  },
  mounted() {
    this._resizeHandler = this.scheduleUpdatePositions.bind(this);
    window.addEventListener('resize', this._resizeHandler);
  },
  beforeDestroy() {
    window.removeEventListener('resize', this._resizeHandler);
    if (this._updateTimer) clearTimeout(this._updateTimer);
  },
  methods: {
    onBodyClick(e) {
      // 检测 tab 切换点击：HeyUI Tabs 的 .h-tabs-item 元素
      var tabItem = e.target.closest && e.target.closest('.h-tabs-item');
      if (tabItem) {
        // tab 切换后 v-show 变化，延迟重新计算 overlay 位置
        var self = this;
        setTimeout(function() { self.updateOverlayPositions() }, 100);
        setTimeout(function() { self.updateOverlayPositions() }, 400);
      }
    },
    onBodyMouseMove(e) {
      if (!this.isListPage && !this.isFormPage) {
        this.hoverArea = '';
        return;
      }
      var body = this.$refs.previewBody;
      if (!body) return;
      var rect = body.getBoundingClientRect();
      var y = e.clientY - rect.top + body.scrollTop;
      if (this.isListPage) {
        if (y >= this.queryOverlayTop && y < this.queryOverlayTop + this.queryOverlayHeight) {
          this.hoverArea = 'query';
        } else if (y >= this.tableOverlayTop && y < this.tableOverlayTop + this.tableOverlayHeight) {
          this.hoverArea = 'table';
        } else {
          this.hoverArea = '';
        }
      } else if (this.isFormPage) {
        // 优先判断是否在某个子表覆盖层内
        var hitSub = -1;
        for (var i = 0; i < this.subOverlays.length; i++) {
          var so = this.subOverlays[i];
          if (y >= so.top && y < so.top + so.height) { hitSub = i; break }
        }
        if (hitSub >= 0) {
          this.hoverArea = 'sub_' + hitSub;
        } else if (y >= this.formOverlayTop && y < this.formOverlayTop + this.formOverlayHeight) {
          this.hoverArea = 'form';
        } else {
          this.hoverArea = '';
        }
      }
    },
    scheduleUpdatePositions() {
      if (this._updateTimer) clearTimeout(this._updateTimer);
      // generic-module 异步渲染，需要多次尝试
      this._updateTimer = setTimeout(this.updateOverlayPositions, 300);
      setTimeout(this.updateOverlayPositions, 800);
      setTimeout(this.updateOverlayPositions, 1500);
    },
    updateOverlayPositions() {
      var body = this.$refs.previewBody;
      if (!body) return;
      var bodyRect = body.getBoundingClientRect();

      if (this.isListPage) {
        // 搜索栏
        var panelBar = body.querySelector('.h-panel-bar');
        if (panelBar) {
          var r = panelBar.getBoundingClientRect();
          this.queryOverlayTop = r.top - bodyRect.top + body.scrollTop;
          this.queryOverlayHeight = r.height;
        }

        // 表格区域
        var table = body.querySelector('.h-table-container');
        if (table) {
          var r2 = table.getBoundingClientRect();
          this.tableOverlayTop = r2.top - bodyRect.top + body.scrollTop;
          this.tableOverlayHeight = r2.height;
        }
      } else if (this.isFormPage) {
        // 表单区域: generic-form-page 容器
        var formEl = body.querySelector('.generic-form-page');
        if (formEl) {
          var r3 = formEl.getBoundingClientRect();
          this.formOverlayTop = r3.top - bodyRect.top + body.scrollTop;
          this.formOverlayHeight = r3.height;
        }
        // 子表(tableBlock)区域: 扫描所有 .rs-form-tableblock[data-subtable]
        // tab 分组模式下，非活动 tab 的子表 display:none，高度为 0，需过滤
        var subEls = body.querySelectorAll('.rs-form-tableblock[data-subtable]');
        var newSubs = [];
        subEls.forEach(function(el) {
          var subtable = el.getAttribute('data-subtable');
          if (!subtable) return;
          var r4 = el.getBoundingClientRect();
          if (r4.height < 1) return; // 跳过隐藏的 tab 中的子表
          newSubs.push({
            subtable: subtable,
            top: r4.top - bodyRect.top + body.scrollTop,
            height: r4.height
          });
        });
        this.subOverlays = newSubs;
      }
    },
    async getResourceId(resourceName) {
      if (!resourceName) return '';
      var scms = this.$store.state.app.scms;
      if (scms && scms[resourceName] && scms[resourceName].length > 0) {
        return scms[resourceName][0].RESOURCEID || '';
      }
      // scm 未加载，先 initScms
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initScms', [resourceName]);
      scms = this.$store.state.app.scms;
      if (scms && scms[resourceName] && scms[resourceName].length > 0) {
        return scms[resourceName][0].RESOURCEID || '';
      }
      return '';
    },
    async openUiSet(type) {
      var resourceName;
      if (type === 'query') {
        resourceName = this.qqryResourceName;
      } else if (type === 'form') {
        resourceName = this.mainResourceName;
      } else {
        resourceName = this.qryResourceName;
      }
      var resourceId = await this.getResourceId(resourceName);
      if (!resourceId) {
        this.$error('未找到资源配置，请检查模块的 MODPATH 配置');
        return;
      }
      this.$emit('open-ui-set', { type: type, resourceId: resourceId, resourceName: resourceName });
    },
    // 打开子表(tableBlock)字段设置: 由 PATHNAME(如 DTSA) 查 MODPATH 找 RESOURCENAME
    async openSubUiSet(subtable) {
      if (!subtable || !this.moduleData || !this.moduleData.MODPATH) {
        this.$error('未找到子表配置');
        return;
      }
      var mpItem = this.moduleData.MODPATH.find(function(p) { return p.PATHNAME === subtable });
      if (!mpItem || !mpItem.RESOURCENAME) {
        this.$error('子表 ' + subtable + ' 未配置 RESOURCENAME');
        return;
      }
      var resourceName = mpItem.RESOURCENAME;
      var resourceId = await this.getResourceId(resourceName);
      if (!resourceId) {
        this.$error('未找到资源配置，请检查模块的 MODPATH 配置');
        return;
      }
      // 子表用 list tab(子表以表格形式呈现)
      this.$emit('open-ui-set', { type: 'list', resourceId: resourceId, resourceName: resourceName });
    },
    refresh() {
      this.refreshTick++;
    }
  }
};
</script>

<style lang="less" scoped>
.page-preview-panel {
  background: #fff;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  height: 100%;
}
.page-preview-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
  flex-shrink: 0;
}
.page-preview-title {
  font-size: 13px;
  font-weight: 600;
  color: #303133;
}
.page-preview-badge {
  font-size: 11px;
  color: #2F54EB;
  background: #F0F5FF;
  padding: 1px 8px;
  border-radius: 3px;
}
.page-preview-body {
  flex: 1;
  overflow: hidden;
  position: relative;
}
.page-preview-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #BFBFBF;
  p {
    margin-top: 8px;
    font-size: 13px;
  }
}
.page-preview-empty-icon {
  font-size: 48px;
}

/* 覆盖层: pointer-events:none 不遮挡下方交互 */
.preview-overlay {
  position: absolute;
  left: 0;
  right: 0;
  z-index: 10;
  pointer-events: none;
  border: 2px dashed transparent;
  transition: border-color 0.2s;
}
.preview-overlay-hover {
  border-color: #2F54EB;
}
/* 设置按钮: pointer-events:auto 可点击 */
.preview-overlay-setting {
  position: absolute;
  top: 4px;
  right: 4px;
  pointer-events: auto;
  cursor: pointer;
  background: #2F54EB;
  color: #fff;
  width: 24px;
  height: 24px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  opacity: 0;
  transition: opacity 0.2s;
  box-shadow: 0 1px 4px rgba(47, 84, 235, 0.3);
  &:hover {
    background: #1D3FBF;
  }
}
.preview-overlay-hover .preview-overlay-setting {
  opacity: 1;
}
</style>
