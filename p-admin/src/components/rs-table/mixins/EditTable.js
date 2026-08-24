// 表格通用属性
export default {
  props: {
    border: { Type: Boolean, default: false },
    stripe: { Type: Boolean, default: false },
    datas: { Type: Array },
    height: { Type: Number },
    checkbox: { Type: Boolean, default: false },
    radio: { Type: Boolean, default: false },
    edit: { Type: Boolean, default: true },
    selectRow: { Type: Boolean, default: true },
    selectWhenClickTr: { Type: Boolean },
    getTrClass: { Type: Function },
  },
  data() {
    return {
      loading: false
    };
  },
  watch: {
  },
  methods: {
    setSelection(row) {
      this.$refs.table.setSelection(row);
    },
    getSelection() {
      return this.$refs.table.getSelection();
    },
    clearSort() {
      return this.$refs.table.clearSort();
    },
    clearSelection() {
      return this.$refs.table.clearSelection();
    },
    invereSelection() {
      return this.$refs.table.invereSelection();
    },
    triggerSort(triggerType) {
      return this.$refs.table.triggerSort(triggerType);
    },
  },
};
