<template>
  <div class="generic-selector-wrap">
    <!-- generic-module select 页面自带 toolbar，其他页面类型需要外部 toolbar -->
    <div v-if="!isSelectPage" class="generic-selector-toolbar">
      <span class="generic-selector-title">{{ title || '选择数据' }}</span>
      <div>
        <Button size="s" @click="handleCancel">取消</Button>
        <Button size="s" color="primary" v-if="selectMode === 'multiple'" @click="handleConfirm">确定</Button>
      </div>
    </div>
    <div :class="isSelectPage ? '' : 'generic-selector-body'">
      <generic-module
        ref="selList"
        :moduleCode="moduleCode"
        :pageCode="pageCode"
        :filterParams="filterParams"
        :selectMode="selectMode"
        @list-select="onListSelect"
        @list-click-row="onRowClick"
        @selector-selected="onSelectorSelected"
        @selector-cancel="handleCancel"
      ></generic-module>
    </div>
  </div>
</template>
<script>
// 避免循环引用: generic-module → generic-selector → generic-module
// 使用延迟导入，确保 generic-module.vue 已完成初始化
var GenericModule = null;

export default {
  name: 'generic-selector',
  components: {
    GenericModule: function() { return GenericModule || (GenericModule = require('./generic-module.vue').default) }
  },
  props: {
    moduleCode: { type: String, required: true },
    pageCode: { type: String, default: '' },
    selectMode: { type: String, default: 'single' }, // 'single' | 'multiple'
    title: { type: String, default: '选择数据' },
    filterParams: { type: Object, default: null }
  },
  data() {
    return {
      selectedRows: [],
      currentRow: null,
      isSelectPage: false
    };
  },
  watch: {
    moduleCode: {
      handler() { this.checkPageType() },
      immediate: true
    },
    pageCode: {
      handler() { this.checkPageType() }
    }
  },
  methods: {
    checkPageType() {
      var modCode = this.moduleCode;
      var pgCode = this.pageCode || 'main';
      var modData = this.$store.state.app && this.$store.state.app.modules[modCode];
      if (modData && modData.MODPAGE) {
        var page = modData.MODPAGE.find(function(p) {
          return p.PAGECODE === pgCode && (p.ISDELETED || 0) === 0;
        });
        this.isSelectPage = page && page.PAGETYPE === 'select';
      } else {
        this.isSelectPage = false;
      }
    },
    onListSelect(checks) {
      this.selectedRows = checks || [];
    },
    onRowClick(row) {
      this.currentRow = row;
      // 单选模式：点击行直接确认
      if (this.selectMode === 'single') {
        this.$emit('selected', [row]);
      }
    },
    onSelectorSelected(data) {
      // 多选模式确认按钮（来自 select 页面的 selector-selected 事件）
      this.$emit('selected', data.rows);
    },
    handleConfirm() {
      // 非 select 页面的多选确认
      if (this.selectMode === 'multiple') {
        if (this.selectedRows.length === 0) {
          this.$Message('请至少选择一条记录');
          return;
        }
        this.$emit('selected', this.selectedRows);
      } else {
        if (!this.currentRow) {
          this.$Message('请选择一条记录');
          return;
        }
        this.$emit('selected', [this.currentRow]);
      }
    },
    handleCancel() {
      this.$emit('cancel');
    }
  }
};
</script>
<style lang="less" scoped>
.generic-selector-wrap {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.generic-selector-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  border-bottom: 1px solid #e8e8e8;
  flex-shrink: 0;
}
.generic-selector-title {
  font-size: 14px;
  font-weight: bold;
}
.generic-selector-body {
  flex: 1;
  overflow: auto;
  min-height: 300px;
}
</style>
