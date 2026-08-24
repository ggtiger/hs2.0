<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    :dynamicQuery="true"
    addper="LIB_M01/A04"
    expper="LIB_M01/A09"
    ref="list"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID"></rsAdd>
    </rs-modal>
    <!-- 按字段名插槽：uiset 里 CUSTTYPE 配 QUERYTYPE=slot，此处覆盖为字典下拉 -->
    <template slot="header-action">
      <Button color="primary" v-per="'LIB_M01/A04'" icon="h-icon-plus" @click="add">添加</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'b01-m01-main',
  components: {
    rsAdd,
  },
  computed: {
  },
  data() {
    return {
      CDID: '',
      showQuery: false,
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '系统管理',
        },
        {
          title: this.$route.meta.title,
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
    advQuery(param) {
      this.$refs.list.advQuery();
    },
  },
};
</script>
