<template>
  <div class="res-add-adapter">
    <div class="res-add-toolbar">
      <span class="res-add-toolbar-title">{{ title }}</span>
      <div class="res-add-toolbar-actions">
        <Button size="s" color="red" v-per="'RS_M01/A07'" @click="handleDel">删除</Button>
        <Button size="s" color="primary" v-per="'RS_M01/A03'" @click="handleSave">保存</Button>
        <Button size="s" @click="askAI"><i class="h-icon-bubble"></i> 问AI</Button>
      </div>
    </div>
    <div class="res-add-body">
      <rs-res-add :DID="did" ref="resAdd"></rs-res-add>
    </div>
  </div>
</template>

<script>
import RsResAdd from '@/pages/s01/m01/views/add.vue';

export default {
  name: 'ResAddAdapter',
  components: { RsResAdd: RsResAdd },
  props: {
    did: { type: String, default: '' }
  },
  data() {
    return {
      isOpened: false
    };
  },
  computed: {
    title() {
      return this.did ? '编辑资源 - ' + this.did : '新增资源';
    },
    // 监听 m01 store 的 MAIN 数据
    mainData() {
      var st = this.$store.state['s01/m01'];
      return (st && st.dt && st.dt.MAIN && st.dt.MAIN.data) || [];
    }
  },
  watch: {
    mainData: {
      handler(v) {
        if (!v || v.length === 0) return;
        var main = v[0];
        var related = [];
        // DATAVIEW：追加来源表
        if (main.RESOURCETYPE === 'DATAVIEW' && main.TABLENAME) {
          related.push({
            ID: 'tbl_' + main.TABLENAME,
            RESOURCEID: 'tbl_' + main.TABLENAME,
            RESOURCENAME: main.TABLENAME,
            TABLENAME: main.TABLENAME,
            RESOURCETYPE: 'TABLE',
            _sourceType: 'table',
            PATHNAME: ''
          });
        }
        if (related.length > 0) {
          this.$emit('related', { parentDid: this.did, items: related });
        }
      },
      deep: true
    }
  },
  mounted() {
    var self = this;
    setTimeout(function() {
      self.isOpened = true;
    }, 150);
  },
  methods: {
    setvalue(val) {
      if (!val) {
        this.$emit('saved');
      }
    },
    handleSave() {
      if (this.$refs.resAdd && typeof this.$refs.resAdd.save === 'function') {
        this.$refs.resAdd.save();
      }
    },
    handleDel() {
      if (this.$refs.resAdd && typeof this.$refs.resAdd.del === 'function') {
        this.$refs.resAdd.del();
      }
    },
    askAI() {
      this.$emit('ask-ai', {
        key: 'resource_' + this.did,
        label: '资源: ' + this.did,
        icon: 'h-icon-link',
        type: 'resource',
        name: this.did
      });
    }
  }
};
</script>

<style lang="less" scoped>
@import '../studio-common.less';

.res-add-adapter {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.res-add-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 @st-space-md;
  height: 36px;
  border-bottom: 1px solid @st-border-light;
  flex-shrink: 0;
  background: @st-bg-white;
}

.res-add-toolbar-title {
  font-size: 12px;
  font-weight: 600;
  color: @st-text-sec;
}

.res-add-toolbar-actions {
  display: flex;
  gap: @st-space-sm;
}

.res-add-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  // view-dialog 样式适配内嵌
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
