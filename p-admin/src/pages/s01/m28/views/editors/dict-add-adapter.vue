<template>
  <div class="dict-add-adapter">
    <div class="dict-add-toolbar">
      <span class="dict-add-toolbar-title">{{ title }}</span>
      <div class="dict-add-toolbar-actions">
        <Button size="s" color="red" v-per="'RS_M06/A04'" @click="handleDel">删除</Button>
        <Button size="s" color="primary" v-per="'RS_M06/A03'" @click="handleSave">保存</Button>
        <Button size="s" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
      </div>
    </div>
    <div class="dict-add-body" v-if="scmReady">
      <rs-dict-add :ID="itemId" :storeName="storeName" ref="dictAdd"></rs-dict-add>
    </div>
    <div v-else class="dict-add-loading"><i class="h-icon-loading"></i></div>
  </div>
</template>

<script>
import RsDictAdd from '@/pages/s01/m06/views/add.vue';

export default {
  name: 'DictAddAdapter',
  components: { RsDictAdd: RsDictAdd },
  props: {
    itemId: { type: String, default: '' }
  },
  data() {
    return {
      isOpened: false,
      storeName: 's01/m06',
      scmReady: false
    };
  },
  computed: {
    title() {
      return this.itemId ? '编辑字典' : '新增字典';
    }
  },
  mounted() {
    var self = this;
    // 先确保模块配置（scm）已加载，再渲染 add.vue
    this.ensureScm().then(function() {
      self.scmReady = true;
      setTimeout(function() {
        self.isOpened = true;
        if (self.itemId) self.loadDict(self.itemId);
      }, 50);
    });
  },
  watch: {
    itemId(v) {
      if (v && this.isOpened) this.loadDict(v);
    }
  },
  methods: {
    async ensureScm() {
      // rs-form-edit 在 created 时读 app.scms[path.scm] 获取字段配置
      // 1. 确保模块配置已加载
      var modData = this.$store.state.app.modules['RS_M06'];
      if (!modData) {
        try {
          await this.$callAction({ action: 'app/initModule', param: { moduleCode: 'RS_M06' }, isBusy: false });
          modData = this.$store.state.app.modules['RS_M06'];
        } catch (e) {}
      }
      // 2. 从模块配置的 MODPATH 中找 MAIN 路径的资源名，调 initScms 加载字段 UI 配置
      if (modData && modData.MODPATH) {
        var mainPath = modData.MODPATH.find(function(p) { return p.PATHNAME === 'MAIN' });
        if (mainPath && mainPath.RESOURCENAME) {
          if (!this.$store.state.app.scms[mainPath.RESOURCENAME]) {
            try {
              await this.$callAction({ action: 'app/initScms', param: [mainPath.RESOURCENAME], isBusy: false });
            } catch (e) {}
          }
        }
      }
    },
    loadDict(id) {
      this.$callAction({ action: 's01/m06/open', param: { ID: id }, isBusy: false });
    },
    // 模拟 rs-modal 的 setvalue，add.vue closeW 调 this.$parent.setvalue(false)
    setvalue(val) {
      if (!val) {
        this.$emit('saved');
      }
    },
    handleSave() {
      if (this.$refs.dictAdd && typeof this.$refs.dictAdd.save === 'function') {
        this.$refs.dictAdd.save();
      }
    },
    handleDel() {
      if (this.$refs.dictAdd && typeof this.$refs.dictAdd.del === 'function') {
        this.$refs.dictAdd.del();
      }
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'dict_' + this.itemId,
        label: '字典编辑',
        icon: 'h-icon-inbox',
        type: 'dict',
        name: this.itemId
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.dict-add-adapter {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.dict-add-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 @st-space-md;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
  background: @st-bg-white;
}

.dict-add-toolbar-title {
  font-size: 12px;
  font-weight: 600;
  color: @st-text-sec;
}

.dict-add-toolbar-actions {
  display: flex;
  gap: @st-space-sm;
}

.dict-add-loading {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: @st-primary;
}

.dict-add-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  & /deep/ .h-panel {
    border: none;
    box-shadow: none;
  }
  & /deep/ .h-panel-bar {
    display: none;
  }
  & /deep/ .h-panel-footer {
    display: none;
  }
  & /deep/ .maxModalH {
    max-height: none;
    overflow: visible;
  }
}
</style>
