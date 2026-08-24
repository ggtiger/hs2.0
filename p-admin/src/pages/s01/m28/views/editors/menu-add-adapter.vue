<template>
  <div class="menu-add-adapter">
    <div class="menu-add-toolbar">
      <span class="menu-add-toolbar-title">{{ title }}</span>
      <div class="menu-add-toolbar-actions">
        <Button size="s" color="red" v-per="'RS_M03/A07'" @click="handleDel">删除</Button>
        <Button size="s" color="primary" v-per="'RS_M03/A04'" @click="handleSave">保存</Button>
        <Button size="s" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
      </div>
    </div>
    <div class="menu-add-body">
      <rs-menu-add ref="menuAdd"></rs-menu-add>
    </div>
  </div>
</template>

<script>
import RsMenuAdd from '@/pages/s01/m03/views/add.vue';

export default {
  name: 'MenuAddAdapter',
  components: { RsMenuAdd: RsMenuAdd },
  props: {
    itemId: { type: String, default: '' }
  },
  data() {
    return {
      // 模拟 rs-modal 的 isOpened，让 view-dialog watch $parent.$parent.isOpened 触发 on-show
      isOpened: false
    };
  },
  computed: {
    title() {
      return this.itemId ? '编辑菜单' : '新增菜单';
    }
  },
  mounted() {
    // 先加载菜单数据，再触发 isOpened 让 add.vue 感知
    var self = this;
    if (this.itemId) {
      this.$callAction({
        action: 's01/m03/open',
        param: { ID: this.itemId },
        isBusy: false
      }).then(function() {
        setTimeout(function() {
          self.isOpened = true;
        }, 150);
      }).catch(function(e) {
        // eslint-disable-next-line no-console
        console.warn('菜单加载失败:', e);
      });
    } else {
      // 新增：延迟触发
      setTimeout(function() {
        self.isOpened = true;
      }, 150);
    }
  },
  methods: {
    setvalue(val) {
      if (!val) {
        this.$emit('saved');
      }
    },
    handleSave() {
      if (this.$refs.menuAdd && typeof this.$refs.menuAdd.save === 'function') {
        this.$refs.menuAdd.save();
      }
    },
    handleDel() {
      if (this.$refs.menuAdd && typeof this.$refs.menuAdd.del === 'function') {
        this.$refs.menuAdd.del();
      }
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'menu_' + this.itemId,
        label: '菜单编辑',
        icon: 'h-icon-menu'
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.menu-add-adapter {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.menu-add-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 @st-space-md;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
  background: @st-bg-white;
}

.menu-add-toolbar-title {
  font-size: 12px;
  font-weight: 600;
  color: @st-text-sec;
}

.menu-add-toolbar-actions {
  display: flex;
  gap: @st-space-sm;
}

.menu-add-body {
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
