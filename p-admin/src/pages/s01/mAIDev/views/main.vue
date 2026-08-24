<template>
  <list-t01
    title="AI开发助理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_MAIDEV/A03"
  >
    <rs-modal ref="madd" :width="600">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="新建开发会话" :ID="CDID"></rsAdd>
    </rs-modal>
    <TableItem title="操作" :width="180" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="enterWorkspace(data)"
        >进入工作区</button>
        <button
          class="h-btn h-btn-s h-btn-red"
          v-per="'RS_MAIDEV/A04'"
          @click.stop="del(data)"
        >删除</button>
      </template>
    </TableItem>
    <rsWorkspace ref="workspace"></rsWorkspace>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import rsWorkspace from './workspace.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 's01-mAIDev-main',
  components: {
    rsAdd,
    rsWorkspace,
  },
  computed: {},
  data() {
    return {
      CDID: '',
      citem: {},
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        { title: '系统管理' },
        { title: 'AI开发助理' },
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
        default:
          break;
      }
    },
    enterWorkspace(row) {
      this.$refs.workspace.open(row.ID);
    },
    del(row) {
      this.$confirm('确认删除该会话？').then(() => {
        this.$callAction({
          action: `${Constants.STORE_NAME}/delete`,
          param: { item: row },
          successText: '删除成功',
        });
      });
    },
  },
};
</script>
