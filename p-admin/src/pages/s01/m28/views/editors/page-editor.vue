<template>
  <div class="st-ed-page" v-if="moduleCode">
    <div class="st-ed-page-bar">
      <span class="st-ed-page-bar-title">页面配置 {{ moduleCode }}</span>
      <span class="st-ed-page-bar-flex"></span>
      <Button size="s" icon="h-icon-setting" @click="callConfig('openModuleStore')">Store扩展</Button>
      <Button size="s" icon="h-icon-link" @click="callConfig('openScriptFlowEditor')">编排接口</Button>
      <Button size="s" icon="h-icon-export" v-per="'RS_M25/A05'" @click="callConfig('openExportTpl')">导出模板</Button>
      <Button size="s" color="cyan" icon="h-icon-share" @click="callConfig('openPublishModal')">发布</Button>
      <Button size="s" color="primary" @click="callConfig('handleSave')">保存</Button>
    </div>
    <mod-config
      ref="modConfig"
      :moduleCodeProp="moduleCode"
      :hideToolbar="true"
      @saved="onSaved"
      @save-error="onSaveError"
    />
  </div>
</template>

<script>
import ModConfig from '@/pages/s01/m18/views/config.vue';

export default {
  name: 'PageEditor',
  components: { ModConfig: ModConfig },
  props: {
    item: { type: Object, default: null },
    moduleCode: { type: String, default: '' }
  },
  methods: {
    callConfig(method) {
      var mc = this.$refs.modConfig;
      if (mc && typeof mc[method] === 'function') {
        mc[method]();
      }
    },
    onSaved() {
      this.$emit('saved', { section: 'page', id: this.item && this.item.ID });
    },
    onSaveError() {
      // config.vue 内部已处理错误提示
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.st-ed-page {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  background: @st-bg-white;
  overflow: hidden;
}

.st-ed-page-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 10px;
  height: 34px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
}

.st-ed-page-bar-title {
  font-size: 12px;
  font-weight: 600;
  color: @st-text-sec;
}

.st-ed-page-bar-flex {
  flex: 1;
}

.st-ed-page > :nth-child(2) {
  flex: 1;
  min-height: 0;
  overflow: auto;
}
</style>
