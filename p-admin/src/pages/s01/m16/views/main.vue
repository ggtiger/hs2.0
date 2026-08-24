<template>
  <list-t01
    title="提示词"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M16/A04"
  >
    <TableItem title="提示词键" prop="PROMPTKEY" :width="200"/>
    <TableItem title="说明" prop="DESCRIPTION"/>
    <TableItem title="更新时间" prop="UPDATETIME" :width="160"/>
    <rs-modal ref="madd" :width="800">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="提示词" :ID="CDID"></rsAdd>
    </rs-modal>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-m16-main',
  components: { rsAdd },
  data() {
    return {
      CDID: '',
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: '系统管理' }, { title: '提示词管理' }],
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
        case 'add': this.add(param); break;
        default: break;
      }
    },
  },
};
</script>
