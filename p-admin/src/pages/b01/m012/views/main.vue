<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="LIB_M012/A04"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID" />
    </rs-modal>
  </list-t01>
</template>

<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';

export default {
  name: 'b01-m012-main',
  components: { rsAdd },
  data () {
    return {
      CDID: '',
      datas: [
        { title: '基础数据' },
        { title: '人员监督' }
      ],
      store: { mapState, mapGetters, mapDateTable, Constants }
    };
  },
  methods: {
    clickRow (data) {
      this.CDID = data.ID;
      this.$refs.madd.show();
    },
    listAction (type, data) {
      if (type === 'add') {
        this.CDID = '';
        this.$refs.madd.show();
      }
    }
  }
};
</script>
