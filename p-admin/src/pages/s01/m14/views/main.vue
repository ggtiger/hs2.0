<template>
  <list-t01
    title="LLM配置"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M14/A04"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="LLM配置" :ID="CDID"></rsAdd>
    </rs-modal>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';

export default {
  name: 's01-m14-main',
  components: { rsAdd },
  data() {
    return {
      CDID: '',
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [{ title: '系统管理' }, { title: 'LLM配置' }]
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
      if (action === 'add') this.add(param);
    }
  }
};
</script>
