<template>
  <list-t01
    title="日志管理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M09/A04"
  ></list-t01>
</template>
<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-m09-main',
  components: {},
  computed: {},
  data() {
    return {
      CDID: '',
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '系统管理',
        },
        {
          title: '日志管理',
        },
      ],
    };
  },

  methods: {
    add() {
      this.CDID = '';
      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction(action, param) {
      switch (action) {
        case 'add':
          this.add(param);
          break;
        case 'uiset':
          this.clickUiSet(param);
          break;
        default:
          break;
      }
    },
    endisable(row, $event) {
      this.$callAction({
        action: `${Constants.STORE_NAME}/endisable`,
        param: { item: row },
        successText: '操作成功',
      });
    },
    edittemplate(row, $event) {
      let { ID, TPMDATA } = row;
      alert('弹出编辑页面');
    },
  },
};
</script>
