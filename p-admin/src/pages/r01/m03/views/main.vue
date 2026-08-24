<template>
  <list-t01
    title="费用管理"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    @list-select="selectRow"
    :sumFields="'AMT,RAMT'"
    ref="list"
    :checkbox="true"
  >
    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="费用管理" :ID="CDID"></rsAdd>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">业务类型</label>
            <Select class="rr-flex-1" v-model="BUSTYPEID" :datas="param1"></Select>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">受理日期</label>
            <DateRangePicker class="rr-flex-1" v-model="BILLDATE"></DateRangePicker>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">受理编号</label>
            <input type="text" class="rr-flex-1" v-model="BILLCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">客户</label>
            <input type="text" class="rr-flex-1" v-model="CUSTNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">联系人</label>
            <input type="text" class="rr-flex-1" v-model="LINKER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">仪器名称</label>
            <input type="text" class="rr-flex-1" v-model="MNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">受理人</label>
            <input type="text" class="rr-flex-1" v-model="EMPNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">状态</label>
            <Select class="rr-flex-1" v-model="STATE" :datas="param"></Select>
          </div>
        </Cell>
        <Cell width="6">
          <div style="width:100%;text-align:right;padding-right:10px">
            <Button class="ml5" @click="advQuery">查询</Button>
            <Button class="ml5">重置</Button>
          </div>
        </Cell>
      </Row>
    </template>
    <template slot="header-action">
      <Button class="ml5" @click="showQuery=!showQuery">高级查询</Button>
    </template>
    <template slot="footer-action">
      <Tooltip theme="white" trigger="click" editable ref="checkTip" v-if="ISFEE">
        <Button class="ml5" color="primary">折扣</Button>
        <div slot="content">
          <div v-padding="15">
            输入折扣(范围:0.00~1.00)
            <input type="number" v-model="DISCOUNT" style="width: 200px;" />
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchDiscount">确定</Button>
          </div>
        </div>
      </Tooltip>
      <Button color="primary" icon="h-icon-task" v-if="ISFEE" @click="batchFee">收费</Button>
      <Poptip content="确定撤销收费？" v-if="ISREFEE" @confirm="batchReFee">
        <Button color="red" icon="h-icon-close">撤销收费</Button>
      </Poptip>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'r01-m03-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', ['BILLDATE', 'BILLCODE', 'CUSTNAME', 'LINKER', 'MNAME', 'EMPNAME', 'STATE', 'BUSTYPEID']),
    ISFEE(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return !item.CHARGEID;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISREFEE(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return !!item.CHARGEID;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISDISCOUNT(state, getters, rootState, rootGetters) {
      return this.checks.length > 0;
    },
  },
  data() {
    return {
      CDID: '',
      citem: {},
      showQuery: false,
      store: { mapState, mapGetters, mapDateTable, Constants },
      datas: [
        {
          title: '检验管理',
        },
        {
          title: '费用管理',
        },
      ],
      param1: [
        { title: '委外', key: 1 },
        { title: '自检', key: 2 }
      ],
      param: [
        { title: '待收费', key: 1 },
        { title: '已折扣', key: 3 },
        { title: '已收费', key: 2 },
      ],
      checks: [],
      DISCOUNT: 1,
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
    selectRow(checks) {
      this.checks = checks;
    },
    advQuery() {
      this.$refs.list.advQuery();
    },
    batchFee() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchFee`,
        param: { items: this.checks },
        successText: '操作成功',
      });
    },
    batchReFee() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReFee`,
        param: { items: this.checks },
        successText: '操作成功',
      });
    },
    batchDiscount() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchDiscount`,
        param: { items: this.checks, DISCOUNT: this.DISCOUNT },
        successText: '操作成功',
        successCall: () => {
          this.$refs.checkTip.hide();
        },
      });
    },
  },
};
</script>
