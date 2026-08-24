<template>
  <table cellspacing="0" cellpadding="0">
    <tr>
      <td
        v-for="(column,i) in this.columns"
        :key="i"
        :width="100/columns.length+'%'"
        align="center"
      >{{column.title}}</td>
    </tr>
    <tr v-for="(v,j) in (this.value)||[]" :key="j">
      <td
        v-for="(column,i) in (columns||[])"
        :width="100/columns.length+'%'"
        align="center"
        :key="i"
      >
        <a v-if="i==0" style="float:left;color:red;" @click="doDelete(j)">删</a>
        <label v-html="v[column.key]"></label>
      </td>
    </tr>
    <tr v-if="!inLayout">
      <td
        v-for="(column,i) in this.columns"
        :width="100/columns.length+'%'"
        align="center"
        :key="i"
      >
        <a v-if="i==0" style="float:left;" @click="doAdd()">添加</a>
      </td>
    </tr>
    <tr v-else>
      <td
        v-for="(column,i) in this.columns"
        :width="100/columns.length+'%'"
        align="center"
        :key="i"
      >
        <label>{{column.key}}</label>
      </td>
    </tr>
  </table>
</template>
<script>
import Gen from '@/utils/gen';
export default {
  name: 'itemTable',
  props: {
    label: {
      type: String,
      default: '',
    },
    value: {
      type: Array,
    },
    cell: {
      type: Number,
      default: 3,
    },
    row: {
      type: Number,
      default: 3,
    },
    align: {
      type: String,
      default: 'left',
    },
    size: {
      type: Number,
      default: 12,
    },
    weight: {
      type: Boolean,
      default: false, // false为细，true为粗
    },
    sourceName: {
      type: String,
    },
    inLayout: {
      type: Boolean,
      default: true,
    },
  },
  data() {
    return {
      columns: [],
    };
  },
  computed: {},
  async mounted() {
    let v = this.sourceName;
    if (!v) {
      this.columns = [];
    } else {
      await this.$store.dispatch('app/initScms', [v]);
      this.columns = Gen.getTableColumns(this.$store.state.app.scms[v]);
    }
  },
  methods: {
    doAdd() {
      this.$emit('clickAtion', 'ardClick');
    },
    doDelete(j) {
      this.value.splice(j, 1);
      this.$forceUpdate();
    },
  },
  watch: {
    async sourceName(v) {
      if (!v) {
        this.columns = [];
      } else {
        await this.$store.dispatch('app/initScms', [v]);
        this.columns = Gen.getTableColumns(this.$store.state.app.scms[v]);
      }
    },
  },
};
</script>

<style lang="less" scoped>
@import '~@/theme/font.less';
table {
  width: 100%;
  border: 1px solid black;
  border-right: none;
  border-bottom: none;
  td {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: normal;
    word-break: break-all;
    box-sizing: border-box;
    min-width: 0;
    vertical-align: middle;
    min-height: 30px;
    border-bottom: 1px solid black;
    border-right: 1px solid black;
  }
}
input.inputNoborder {
  background: none;
  border: none;
  border-radius: 0;
  min-height: 30px;
  width: 100%;
}
</style>
