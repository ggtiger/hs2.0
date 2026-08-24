<template>
  <list-t01
    :title="$route.meta.title"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="LIB_M05/A04"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" :title="$route.meta.title" :ID="CDID"></rsAdd>
    </rs-modal>
    <TableItem
      title="操作"
      :width="150"
      align="center"
      v-if="false"
      fixed="right"
      slot="table-action"
    >
      <template slot-scope="{data}">
        <button
          v-per="'LIB_M05/A07'"
          v-if="(data.ISUSE+'')!=='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
        >启用</button>
        <button
          v-per="'LIB_M05/A07'"
          v-if="(data.ISUSE+'')==='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
        >停用</button>
      </template>
    </TableItem>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'b01-m05-main',
  components: {
    rsAdd,
  },
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
  },
};
</script>
