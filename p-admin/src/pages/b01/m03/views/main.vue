<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="LIB_M03/A04"
  >
   <template slot="simple-query">
      <Row :space="9" >
         <Cell width="12">
         </Cell>
        <Cell width="12">
       <div class="rr-flex-row">
          <label class="rr-justify" style="width:60px">预警状态</label>
          <Select class="rr-flex-1" v-model="QSTATE" :datas="param"></Select>
        </div>
        </Cell>
      </Row>

   </template>
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID"></rsAdd>
    </rs-modal>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'b01-m03-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', ['QSTATE'])
  },
  data() {
    return {
      CDID: '',
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '基础管理',
        },
        {
          title: this.$route.meta.title,
        },
      ],
      param: [
        { title: '标准器过期', key: 1 },
        { title: '应溯源设备', key: 2 },
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
  },
};
</script>
