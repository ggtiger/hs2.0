<template>
  <div class="remote-page-container">
    <component v-if="remoteComponent" :is="remoteComponent"></component>
    <div v-else-if="loading" class="remote-loading">
      <span class="remote-spinner"></span>
      <p>正在加载在线模块...</p>
    </div>
    <div v-else-if="error" class="remote-error">
      <h3>模块加载失败</h3>
      <pre>{{ error }}</pre>
      <Button color="primary" @click="reload">重新加载</Button>
    </div>
  </div>
</template>
<script>
import { loadCompiledSFC } from '@/sfc-loader';

export default {
  name: 'remote-route',
  props: {
    modulePath: {
      type: String,
      required: true,
    },
  },
  data() {
    return {
      remoteComponent: null,
      loading: false,
      error: '',
    };
  },
  watch: {
    modulePath() {
      this.loadModule();
    },
  },
  created() {
    this.loadModule();
  },
  methods: {
    async loadModule() {
      this.loading = true;
      this.error = '';
      this.remoteComponent = null;
      try {
        var options = await loadCompiledSFC(this.modulePath);
        await this._preloadScms();
        if (options && (options.render || options.template || options.component)) {
          this.remoteComponent = options;
        } else {
          this.error = '模块 ' + this.modulePath + ' 不是有效的 Vue 组件';
        }
      } catch (e) {
        this.error = e.message || String(e);
        console.error('[RemoteRoute] 加载失败:', e);
      } finally {
        this.loading = false;
      }
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
        console.warn('[RemoteRoute] preload scms failed:', e);
      }
    },
    reload() {
      this.loadModule();
    },
  },
};
</script>
<style lang="less" scoped>
.remote-page-container {
  /* 与本地页面 router-view 直接渲染的效果对齐: 让子组件的高度自适应撑满父容器
     不能用 min-height 固定值, 否则 list-t01 内部的 flex 布局会拿不到正确高度 */
  height: 100%;
}
.remote-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #999;
  p {
    margin-top: 12px;
  }
}
.remote-spinner {
  display: inline-block;
  width: 28px;
  height: 28px;
  border: 3px solid #ddd;
  border-top-color: #0a84ff;
  border-radius: 50%;
  animation: remote-spin 0.6s linear infinite;
}
@keyframes remote-spin {
  to { transform: rotate(360deg); }
}
.remote-error {
  padding: 30px;
  text-align: center;
  h3 {
    color: #ed4014;
    margin-bottom: 12px;
  }
  pre {
    background: #f5f5f5;
    border-radius: 4px;
    padding: 12px;
    text-align: left;
    color: #ed4014;
    font-size: 13px;
    margin: 12px 0;
    max-height: 300px;
    overflow: auto;
  }
}
</style>
