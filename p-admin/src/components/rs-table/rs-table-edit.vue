<template>
  <div>
    <Table
      ref="table"
      :loading="loading"
      :border="border"
      :height="height"
      :stripe="stripe"
      :datas="datas"
      @trclick="onRowClick"
      :checkbox="checkbox || hasMultiSelect"
      :radio="radio || hasSingleSelect"
      :selectRow="selectRow"
      :selectWhenClickTr="selectWhenClickTr"
      :getTrClass="getTrClass"
    >
      <TableItem
        v-for="(column,index) in renderColumns"
        v-if="isColumnVisible(column)"
        :key="index"
        :title="column.title"
        :prop="_editColumn(column.type)?'':column.key"
        :align="column.align"
        :width="column.width"
        :fixed="column.fiexed"
        :dict="column.dict"
      >
        <template slot-scope="{data}" v-if="_editColumn(column.type)">
          <rs-table-cell
            v-bind="column.props"
            v-on="column.on"
            :value="data[column.key]"
            :data="data"
          />
        </template>
      </TableItem>
      <slot/>
    </Table>
  </div>
</template>
<script>
import Gen from '@/utils/gen';
import EditTableMixins from './mixins/EditTable';
import RsTableCell from './rs-table-cell';
import { evalVisibility } from '@/utils/visibility';
export default {
  name: 'rs-table-edit',
  inject: { visibilityHost: { default: null } },
  props: {
    // 修改状态
    edit: { Type: Boolean, default: true },
    // 数据源
    path: { Type: Object },
    showFields: { Type: Array },
    getProps: { Type: Function },
  },
  mixins: [EditTableMixins],
  components: { RsTableCell },
  data() {
    return {
      editInfo: {
        editIndex: -1,
        edit: true,
      },
      api: a => {
        console.log(a);
        return this;
      },
      columns: [],
    };
  },
  watch: {
    edit: {
      handler(v) {
        this.editInfo.edit = v;
      },
      immediate: true,
    },
  },
  computed: {
    // uiset 配置的多选列 -> 开启 HeyUI checkbox
    hasMultiSelect() {
      return (this.columns || []).some(c => c.type === 'multiselect');
    },
    // uiset 配置的单选列 -> 开启 HeyUI radio
    hasSingleSelect() {
      return (this.columns || []).some(c => c.type === 'singleselect');
    },
    // 排除选择列后的渲染列（选择列由 HeyUI checkbox/radio 自动渲染）
    renderColumns() {
      return (this.columns || []).filter(c => c.type !== 'multiselect' && c.type !== 'singleselect');
    },
  },
  methods: {
    // 列显隐：未配 visibleIf 时默认 ISSHOW+列 key；method 传入 { key, path }
    isColumnVisible(column) {
      if (!this.visibilityHost) return true;
      let visIf = column && column.visibleIf;
      const key = column && (column.key || column.prop || '');
      if (!visIf) visIf = 'ISSHOW' + key;
      return evalVisibility(this.visibilityHost, visIf, { key, path: this.path && (this.path._path_ || this.path.path) });
    },
    // 选择后设置当前编辑行
    onSelect(row, index) {
      debugger;
      this.editInfo.editIndex = index;
    },
    // 触发按钮事件
    onAction(actioncode) {
      this.$emit('on-action-click', actioncode);
    },
    onRowClick(row, event, rowIndex) {
      this.currentRow = this.datas[rowIndex];
      this.editInfo.editIndex = rowIndex;
      this.$emit('on-row-click', row, rowIndex);
    },
    onCellClick(row, rowIndex) {

    },
    // AI 填报：把 {字段名:值} 批量写入指定子表行。
    // 支持字段类型转换：checkbox→1/0, number→parseFloat, select→查字典
    applyFill(fields, rowIndex) {
      if (!fields || !this.path) return;
      const row = this.datas[rowIndex];
      if (!row) return;
      const converted = this._convertFields(fields);
      Object.keys(converted).forEach(key => {
        const v = converted[key];
        this.$set(row, key, v);
        this.path.setValue(key, v, row);
      });
    },
    // 内部：根据字段类型做值转换
    _convertFields(fields) {
      const result = {};
      if (!fields) return result;
      Object.keys(fields).forEach(k => {
        const key = (k || '').toUpperCase();
        let v = fields[k];
        // 根据字段类型做值转换
        const column = (this.columns || []).find(c => c.key === key);
        if (column) {
          const type = column.type;
          const dict = column.dict;
          if (type === 'checkbox') {
            v = v === true || v === 'true' || v === 1 || v === '1' ? 1 : 0;
          } else if (type === 'number') {
            const n = parseFloat(v);
            v = isNaN(n) ? v : n;
          } else if (type === 'select') {
            // select：尝试用 dict 解析
            if (dict) {
              v = this._resolveSelectValue(v, dict);
            }
          }
        }
        result[key] = v;
      });
      return result;
    },
    // select 字典值解析：如果值是字符串，尝试匹配字典 title→key
    _resolveSelectValue(v, dict) {
      if (v == null || v === '') return v;
      const strV = String(v);
      // dict 可能是字典名（如 '$CUSTTYPE'）或已注册的字典对象
      let dictData = dict;
      if (typeof dict === 'string') {
        try {
          dictData = JSON.parse(dict);
        } catch (e) {
          // 是字典名，从 heyui 取
          try {
            const heyui = require('heyui').default;
            dictData = heyui.getDict(dict);
          } catch (e2) {}
        }
      }
      if (!dictData) return v;
      if (Array.isArray(dictData)) {
        const found = dictData.find(d => d.title === strV || d.key === strV);
        if (found) return found.key;
      } else if (typeof dictData === 'object') {
        for (const dk in dictData) {
          if (dictData[dk] === strV) return dk;
        }
        if (dictData[strV] !== undefined) return strV;
      }
      return v;
    },
    // 触发修改事件
    onApplyEdit({ item, index }) {
      const row = this.datas[index];
      // 使用 $set 确保新增属性也是响应式的（Vue 2 无法检测属性新增）
      Object.keys(item).forEach(key => {
        this.$set(row, key, item[key]);
      });
      this.path.setValues(item, row);
      // 通知父组件数据已变化，以便重新计算动态下拉选项
      this.$emit('data-change', { path: this.path.scm, item, index });
    },
    _editColumn(type) {
      return ['text', 'select', 'number', 'autocomplete', 'textarea', 'checkbox', 'file', 'fileupload', 'imageupload', 'datepicker', 'code'].indexOf(type) !== -1;
    },
    setColumns(columns) {
      let { editInfo, getProps } = this;
      this.columns =
        columns ||
        Gen.getTableColumns(this.$store.state.app.scms[this.path.scm], {
          editInfo,
          'on-cell-click': this.onCellClick,
          'on-apply-edit': this.onApplyEdit,
          getProps,
        });
    },
  },
  async mounted() {
    this.loading = true;
    // eslint-disable-next-line no-restricted-syntax
    await this.$store.dispatch('app/initScms', [this.path.scm]);
    this.setColumns();
    this.loading = false;
    window.$currentTable = this;
  },
};
</script>
