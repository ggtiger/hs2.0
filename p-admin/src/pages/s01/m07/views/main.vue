<template>
  <list-t01
    title="模板"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    addper="RS_M07/A07"
     :checkbox="true"
       @list-select="selectRow"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="模板" :item="this.citem" :ID="CDID"></rsAdd>
    </rs-modal>
    <rs-modal ref="edittemplate" :fullScreen="true">
      <editTemplate title="编辑模板" :storeName="store.Constants.STORE_NAME" :ID="TDID"></editTemplate>
    </rs-modal>
    <TableItem title="操作" :width="150" align="center" fixed="right" slot="table-action">
      <template slot-scope="{data}">
        <button
          v-if="(data.ISUSE+'')!=='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M07/A07'"
        >启用</button>
        <button
          v-if="(data.ISUSE+'')==='1'"
          class="h-btn h-btn-s h-btn-blue"
          @click.stop="endisable(data)"
          v-per="'RS_M07/A07'"
        >停用</button>
        <button
          class="h-btn h-btn-s h-btn-blue"
          v-per="'RS_M07/A08'"
          @click.stop="edittemplate(data)"
           v-if="data.STATE==1"
        >编辑模板</button>
      </template>
    </TableItem>
     <template slot="footer-action">
      <Button color="primary" v-per="'RS_M07/A04'" icon="h-icon-plus" @click="add">添加</Button>
      <Button
        color="primary"
        v-per="'RS_M07/A09'"
        icon="h-icon-check"
        @click="batchSubmit(true)"
        v-if="ISSHOWSUBMIT"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'RS_M07/A10'" v-if="ISSHOWRESUBMIT" @confirm="batchReSubmit">
        <Button color="red" icon="h-icon-close">撤销提交</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'RS_M07/A11'"
        icon="h-icon-check"
        @click="batchComCheck(true)"
        v-if="ISSHOWCHECK"
      >审核</Button>
      <Poptip content="确定撤销提交？" v-per="'RS_M07/A12'" v-if="ISSHOWRECHECK" @confirm="batchComReCheck">
        <Button color="red" icon="h-icon-close">撤销审核</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'RS_M07/A13'"
        icon="h-icon-check"
        @click="batchComVerify"
        v-if="ISSHOWVERIFY"
      >发布</Button>
      <Poptip content="确定撤销提交？" v-per="'RS_M07/A14'" v-if="ISSHOWREVERIFY" @confirm="batchComReVerify">
        <Button color="red" icon="h-icon-close">撤销发布</Button>
      </Poptip>
     </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import editTemplate from './editTemplate.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import List01 from '@/mixins/list01';
export default {
  name: 's01-m07-main',
  components: {
    rsAdd,
    editTemplate,
  },

  computed: {},
  data() {
    return {
      CDID: '',
      TDID: '',
      citem: {},
      checks: [],
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '系统管理',
        },
        {
          title: '模板管理',
        },
      ],
    };
  },
  mixins: [List01],
  methods: {
    add() {
      this.CDID = '';
      debugger;
      this.citem = this.checks[0];
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
      this.TDID = ID;
      this.$refs.edittemplate.show();
    },
    selectRow(checks) {
      this.checks = checks;
    },
  },
};
</script>
