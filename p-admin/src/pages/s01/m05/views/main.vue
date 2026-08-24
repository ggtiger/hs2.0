<template>
  <list-t01
    title="用户"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M05/A03"
  >
    <rs-modal title="新增用户" ref="madd">
      <rsAdd :ID="CID" title="用户" :path="$MAIN" :storeName="storeName"></rsAdd>
    </rs-modal>
    <rs-modal title="设置角色" ref="mroleset" autoWidth>
      <roleSet :params="params"></roleSet>
    </rs-modal>
    <rs-modal title="设置部门" ref="mdeptset" autoWidth>
      <deptSet :params="params"></deptSet>
    </rs-modal>
    <TableItem title="操作" :width="150" align="left" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M05/A08'"
          @click.stop="setPower(data)"
        >设置角色</button>
        <button
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M05/A10'"
          @click.stop="setDept(data)"
        >设置部门</button>
        <br />
        <button
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M05/A03'"
          @click.stop="setPass(data)"
        >重置密码</button>
        <button
          v-if="(data.ISUSE+'')!=='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M05/A07'"
        >启用</button>
        <button
          v-if="(data.ISUSE+'')==='1'"
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M05/A07'"
          @click.stop="endisable(data)"
        >停用</button>
      </template>
    </TableItem>
  </list-t01>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import rsAdd from './add.vue';
import roleSet from './roleSet.vue';
import deptSet from './deptSet.vue';
export default {
  name: 's01-m05-main',
  components: {
    rsAdd,
    roleSet,
    deptSet,
  },
  computed: {
    ...mapDateTable('MAIN', []),
  },
  data() {
    return {
      modal1: false,
      modal2: false,
      datas: [
        {
          title: '系统管理',
        },
        {
          title: '用户管理',
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
    setPass(row) {
      this.$callAction({
        action: `${Constants.STORE_NAME}/resetPass`,
        param: { ID: row.ID },
        successText: '操作成功',
      });
    },
    setPower(data) {
      this.params = data;
      this.$refs.mroleset.show();
    },
    setDept(data) {
      this.params = data;
      this.$refs.mdeptset.show();
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
