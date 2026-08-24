<template>
  <div class="sfc-preview-wrap">
    <div v-if="error" class="sfc-preview-error">
      <h4>编译错误</h4>
      <pre>{{ error }}</pre>
    </div>
    <div v-else-if="compiling" class="sfc-preview-loading">
      <span class="sfc-preview-spinner"></span>
      <p>编译中...</p>
    </div>
    <div v-else ref="previewContainer" class="sfc-preview-content"></div>
  </div>
</template>
<script>
import Vue from 'vue';
// eslint-disable-next-line camelcase
import { compileSFC, executeCompiled, __sfc_require__, preloadDeps, invalidateCacheByPrefix } from '@/sfc-loader';

export default {
  name: 'sfc-preview',
  props: {
    source: {
      type: String,
      default: '',
    },
    modulePath: {
      type: String,
      default: '@/preview/sfc-preview.vue',
    },
  },
  data() {
    return {
      error: '',
      compiling: false,
      componentInstance: null,
      debounceTimer: null,
    };
  },
  watch: {
    source() {
      this.schedulePreview();
    },
    modulePath() {
      this.schedulePreview();
    },
  },
  mounted() {
    this.schedulePreview();
  },
  beforeDestroy() {
    this.destroyPreview();
  },
  methods: {
    schedulePreview() {
      if (this.debounceTimer) clearTimeout(this.debounceTimer);
      var self = this;
      this.debounceTimer = setTimeout(function() {
        self.doPreview();
      }, 800);
    },
    async doPreview() {
      var self = this;
      this.destroyPreview();
      if (!this.source || !this.source.trim()) {
        return;
      }
      this.compiling = true;
      this.error = '';
      var options = null;
      try {
        // 先失效当前编辑文件所属模块目录下的所有缓存, 让相对路径依赖(store.js/add.vue 等)重新从 DB 拉最新
        // 这样实时预览和已部署页面互不干扰: 预览始终用最新代码, 已部署页面不受预览缓存污染
        if (this.modulePath) {
          var dirPath = this.modulePath.substring(0, this.modulePath.lastIndexOf('/'));
          var moduleDirPrefix = dirPath.substring(0, dirPath.lastIndexOf('/') + 1);
          invalidateCacheByPrefix(moduleDirPrefix);
        }
        var result = await compileSFC(this.source, this.modulePath);
        // 预加载所有数据库依赖 (相对路径模块需异步从 DB 加载到缓存)
        // executeCompiled 是同步的, __sfc_require__ 同步查缓存, 故必须先预加载
        await preloadDeps(result.deps, self.modulePath);
        // preloadDeps 触发 SFC 的 store.js 执行 → Store03.ensureModule → app/initModule
        // 此刻模块配置已写入 state.app.modules, 但 scm 尚未加载
        // 扫描所有已加载模块的 MODPATH, 把用到的 RESOURCENAME 通过 initScms 拉到本地
        // 否则被预览组件用到的 <rs-form-edit>/<rs-table-*> 等读不到 scm 会报错
        await self._preloadScms();
        var requireFn = function(depPath) {
          return __sfc_require__(depPath, self.modulePath);
        };
        options = executeCompiled(result.compiledCode, requireFn);
        // JS 文件没有 render 函数, 不挂载为 Vue 组件
        if (!options.render && !options.template && !options.component) {
          this.error = 'JS 模块无预览 (没有 render/template)';
          return;
        }
      } catch (e) {
        this.error = e.message || String(e);
        console.error('[SfcPreview] 编译失败:', e);
        return;
      } finally {
        this.compiling = false;
      }
      // compiling=false 后模板才会渲染 previewContainer, 再 $nextTick 挂载
      // 用离线 $mount() + appendChild, 避免 $mount(el) 替换掉容器 div 导致二次挂载失败
      var ComponentClass = Vue.extend(options);
      // 独立 new 出的实例无 parent, Vuex/VueRouter 通过 beforeCreate mixin 从 parent 继承
      // 这里显式注入 store/router/原型方法, 保证被预览组件内的全局组件 (如 rs-form-edit) 能正常访问 $store
      this.componentInstance = new ComponentClass({
        store: this.$store,
        router: this.$router,
      });
      this.componentInstance.$mount();
      this.$nextTick(function() {
        var container = self.$refs.previewContainer;
        if (container && self.componentInstance && self.componentInstance.$el) {
          container.appendChild(self.componentInstance.$el);
        }
      });
    },
    async _preloadScms() {
      try {
        var store = this.$store;
        var modules = (store.state.app && store.state.app.modules) || {};
        var resNames = {};
        Object.keys(modules).forEach(function(code) {
          var modData = modules[code];
          var paths = (modData && modData.MODPATH) || [];
          paths.forEach(function(p) {
            if (p && p.RESOURCENAME) resNames[p.RESOURCENAME] = 1;
          });
        });
        var names = Object.keys(resNames);
        if (names.length > 0) {
          await store.dispatch('app/initScms', names);
        }
      } catch (e) {
        console.warn('[SfcPreview] preload scms failed:', e);
      }
    },
    destroyPreview() {
      if (this.componentInstance) {
        try {
          this.componentInstance.$destroy();
        } catch (e) {
          // ignore
        }
        if (this.componentInstance.$el && this.componentInstance.$el.parentNode) {
          this.componentInstance.$el.parentNode.removeChild(this.componentInstance.$el);
        }
        this.componentInstance = null;
      }
    },
  },
};
</script>
<style lang="less" scoped>
.sfc-preview-wrap {
  /* 父级 .sfc-ide-preview 是 flex column, 这里用 flex:1 撑满标题栏以外的剩余高度
     height:100% 在 flex 子项中可能被解析为 auto (取决于父高度是否"definite"), 导致
     内部的 RsTableList 拿不到确定高度, 表格高度自适应内容而不固定 */
  flex: 1;
  min-height: 0;
  height: 100%;
  overflow: auto;
  background: #fff;
}
.sfc-preview-content {
  /* 撑满父容器, 让被预览组件的 flex 高度链 (RsTableList 等) 能拿到正确高度
     padding 用 box-sizing 吸收, 避免撑破 100% 高度 */
  height: 100%;
  box-sizing: border-box;
  padding: 10px;
}
.sfc-preview-error {
  padding: 16px;
  h4 {
    color: #ed4014;
    margin-bottom: 8px;
  }
  pre {
    background: #fef0f0;
    border-radius: 4px;
    padding: 10px;
    color: #ed4014;
    font-size: 12px;
    white-space: pre-wrap;
    word-break: break-all;
  }
}
.sfc-preview-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px;
  color: #999;
  p {
    margin-top: 10px;
  }
}
.sfc-preview-spinner {
  display: inline-block;
  width: 24px;
  height: 24px;
  border: 3px solid #ddd;
  border-top-color: #0a84ff;
  border-radius: 50%;
  animation: sfc-preview-spin 0.6s linear infinite;
}
@keyframes sfc-preview-spin {
  to { transform: rotate(360deg); }
}
</style>
