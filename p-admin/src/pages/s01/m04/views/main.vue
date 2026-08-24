<template>
  <list-t01
    title="角色"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
  >
    <rs-modal title="新增角色" ref="madd">
      <view-add-t01 :ID="CID" title="角色" :path="$MAIN" :storeName="storeName"></view-add-t01>
    </rs-modal>
    <rs-modal title="设置角色" ref="mpowerset">
      <powerSet :params="params"></powerSet>
    </rs-modal>
    <TableItem title="操作" :width="150" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M04/A07'"
          @click.stop="setPower(data)"
        >设置角色</button>
        <button
          v-if="(data.ISUSE+'')!=='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M04/A08'"
        >启用</button>
        <button
          v-if="(data.ISUSE+'')==='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M04/A08'"
        >停用</button>
      </template>
    </TableItem>
  </list-t01>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import rsAdd from './add.vue';
import powerSet from './powerSet.vue';
export default {
  name: 's01-m04-main',
  components: {
    rsAdd,
    powerSet,
  },
  computed: {
    ...mapDateTable('MAIN', []),
  },
  data() {
    return {
      datas: [
        {
          title: '系统管理',
        },
        {
          title: '角色管理',
        },
      ],
      params: {},
      CID: '',
      storeName: Constants.STORE_NAME,
      store: { mapState, mapGetters, mapDateTable, Constants },
    };
  },
  methods: {
    add() {
      this.CID = '';
      this.$refs.madd.show();
    },
    clickRow(row, $event) {
      this.CID = row.ID;
      this.$refs.madd.show();
    },
    endisable(row, $event) {
      this.$callAction({
        action: `${Constants.STORE_NAME}/endisable`,
        param: { item: row },
        successText: '操作成功',
      });
    },
    setPower(data) {
      this.params = data;
      this.$refs.mpowerset.show();
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
  },
};
</script>
