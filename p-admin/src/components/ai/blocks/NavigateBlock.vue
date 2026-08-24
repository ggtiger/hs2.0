<template>
  <div class="asst-navigate">
    <button class="asst-nav-btn" @click="go">
      <span class="asst-nav-icon">📍</span>
      <span class="asst-nav-text">打开{{ moduleName || path }}</span>
    </button>
  </div>
</template>

<script>
export default {
  name: 'NavigateBlock',
  props: {
    path: { type: String, required: true },
    query: { type: Object, default: null },
    moduleCode: { type: String, default: '' },
    moduleName: { type: String, default: '' }
  },
  methods: {
    async go() {
      // 跳转前先加载模块配置（和菜单跳转main.vue.initModule一致）
      try {
        const appModules = this.$store.state.app.modules || {};
        if (!appModules['RS_M00']) {
          await this.$store.dispatch('app/initModule', 'RS_M00');
        }
        if (this.moduleCode && !appModules[this.moduleCode]) {
          await this.$store.dispatch('app/initModule', this.moduleCode);
        }
      } catch (e) {
        console.warn('[NavigateBlock] 加载模块配置失败:', e.message || e);
      }
      this.$router
        .push({ path: this.path, query: this.query || {} })
        .catch(() => {}); // 忽略重复导航错误
    }
  }
};
</script>

<style scoped>
.asst-navigate {
  margin: 6px 0;
}
.asst-nav-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border: 1px solid #2d8cf0;
  border-radius: 4px;
  background: #ecf5ff;
  color: #2d8cf0;
  cursor: pointer;
  font-size: 13px;
}
.asst-nav-btn:hover {
  background: #2d8cf0;
  color: #fff;
}
</style>
