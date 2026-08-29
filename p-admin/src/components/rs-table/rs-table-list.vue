<template>
  <div class="rs-table-list-root">
    <div v-if="pageActions.length" class="rs-table-page-actions">
      <Button
        v-for="(act, i) in pageActions"
        :key="'pa' + i"
        color="primary"
        size="s"
        v-if="isActionVisible(act)"
        v-per="act.perCode || act.per"
        @click="onPageAction(act)"
      >{{act.label}}</Button>
    </div>
    <Table
      ref="table"
      :loading="loading"
      :border="border"
      :height="height"
      :stripe="stripe"
      :datas="datas"
      @trclick="onRowClick"
      @trdblclick="onDBRowClick"
      @select="onSelect"
      :checkbox="checkbox || hasMultiSelect"
      :radio="radio || hasSingleSelect"
      :selectRow="selectRow"
      :selectWhenClickTr="selectWhenClickTr"
      :getTrClass="getTrClass"
    >
      <TableItem
        v-for="(column,index) in tableColumns"
        :key="index"
        :title="column.title"
        :prop="_htmlFormat(column)?'':column.prop"
        :align="column.align"
        :width="column.width"
        :fixed="column.fixed"
        :dict="column.dict"
      >
        <template slot-scope="{data}">
          <div v-if="column.actions">
            <Button
              v-for="(obj,ai) in column.actions"
              color="primary"
              size="s"
              v-if="isActionVisible(obj)"
              @click.stop="listAction(obj.code,data)"
              :key="ai"
              v-per="obj.perCode || obj.per"
            >{{obj.label}}</Button>
          </div>
          <span v-if="_htmlFormat(column)" v-html="column.render?column.render(data[column.prop]):data[column.prop]"></span>
        </template>
      </TableItem>
      <slot></slot>
    </Table>
  </div>
</template>
<script>
import Gen from '@/utils/gen';
import EditTableMixins from './mixins/EditTable';
import { evalVisibility } from '@/utils/visibility';
export default {
  name: 'rs-table-list',
  inject: { visibilityHost: { default: null } },
  props: {
    path: { Type: Object },
    showFields: { Type: Array },
    getProps: { Type: Function },
    // 可选：直接传入列配置（优先于 path.scm 读取），用于预览等场景
    columnConfig: { Type: Array },
  },
  mixins: [EditTableMixins],
  components: {},
  data() {
    return {
      columns: [],
    };
  },
  watch: {
    // 列配置变化时重新生成（预览场景）
    columnConfig() {
      this.setColumns();
    },
  },
  computed: {
    // 排除 pageaction / 选择列 后的真实表格列
    tableColumns() {
      return (this.columns || []).filter(c => c.type !== 'pageaction' && c.type !== 'multiselect' && c.type !== 'singleselect');
    },
    // uiset 配置的多选列 -> 开启 HeyUI checkbox
    hasMultiSelect() {
      return (this.columns || []).some(c => c.type === 'multiselect');
    },
    // uiset 配置的单选列 -> 开启 HeyUI radio
    hasSingleSelect() {
      return (this.columns || []).some(c => c.type === 'singleselect');
    },
    // 收集 pageaction 配置为顶部按钮组
    pageActions() {
      const list = [];
      (this.columns || []).forEach(c => {
        if (c.type === 'pageaction' && c.pageActions) {
          c.pageActions.forEach(a => list.push(a));
        }
      });
      return list;
    },
  },
  methods: {
    // 代理内部 HeyUI Table 的选区方法（弹窗组件如 tmpSel/ardSel 通过 $refs.selection 调用）
    getSelection() {
      return this.$refs.table ? this.$refs.table.getSelection() : [];
    },
    setSelection(rows) {
      if (this.$refs.table) this.$refs.table.setSelection(rows);
    },
    setRowSelect(row, value) {
      if (this.$refs.table) this.$refs.table.setRowSelect(row, value);
    },
    // 按钮显隐：未配 visibleIf 时默认 ISSHOW+按钮 code；method 传入 { key, path }
    isActionVisible(act) {
      if (!this.visibilityHost) return true;
      let visIf = act.visibleIf;
      const key = act.code || '';
      if (!visIf) visIf = 'ISSHOW' + key;
      return evalVisibility(this.visibilityHost, visIf, { key, path: this.path && (this.path._path_ || this.path.path) });
    },
    onPageAction(act) {
      this.$emit('list-action', act.code, null);
    },
    onRowClick(row, $event) {
      this.$emit('trclick', row, $event);
    },
    onDBRowClick(row, $event) {
      this.$emit('trdblclick', row, $event);
    },
    onSelect(checks, $event) {
      this.$emit('select', checks, $event);
    },
    setColumns(columns) {
      let { getProps } = this;
      if (columns) {
        this.columns = columns;
      } else if (this.columnConfig) {
        // 优先使用外部传入的列配置（预览场景）
        this.columns = this.columnConfig;
      } else if (this.path && this.path.scm) {
        this.columns = Gen.getTableColumns(this.$store.state.app.scms[this.path.scm], { getProps });
      }
      if (this.columns && this.columns.length > 0 && this.checkbox) {
        // 仅对真实列设 fixed
        const first = this.tableColumns[0];
        if (first) first.fixed = 'left';
      }
    },
    listAction(type, param) {
      this.$emit('list-action', type, param);
    },
    _htmlFormat(column) {
      if (column.dict || (column.prop + '').indexOf('$') === 0) {
        return false;
      }
      return true;
    },
  },
  async mounted() {
    this.loading = true;
    if (this.path && this.path.scm) {
      // eslint-disable-next-line no-restricted-syntax
      await this.$store.dispatch('app/initScms', [this.path.scm]);
    }
    this.setColumns();
    this.loading = false;
  },
};
</script>
<style scoped>
/* 根容器撑满父级并作为 flex 列容器，保证 Table 拿到高度触发固定头部 */
.rs-table-list-root {
  height: 100%;
  display: flex;
  flex-direction: column;
}
.rs-table-list-root > /deep/ .h-table {
  flex: 1;
  min-height: 0;
}
.rs-table-page-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 8px;
}
</style>
