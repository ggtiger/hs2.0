<template>
  <list-t01
    title="原始记录"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    :checkbox="true"
    @list-select="selectRow"
    ref="list"
  >
    <rs-modal ref="madd" :fullScreen="true">
      <rsAdd1 :storeName="store.Constants.STORE_NAME" :citem="citem" title="原始记录" :ID="CDID"></rsAdd1>
    </rs-modal>
    <rs-modal ref="mpdf">
      <rs-print-pdf :src="pdfSrc"></rs-print-pdf>
    </rs-modal>
    <rs-modal ref="ppdf">
      <rs-print-pdf :src="pdfSrc" type="preview"></rs-print-pdf>
    </rs-modal>
    <rs-modal ref="mloglist">
      <rsLogList :storeName="store.Constants.STORE_NAME" :ID="CDID"></rsLogList>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">检校日期</label>
            <DateRangePicker class="rr-flex-1" v-model="BILLDATE"></DateRangePicker>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">委托单号</label>
            <input type="text" class="rr-flex-1" v-model="WTCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">受理编号</label>
            <input type="text" class="rr-flex-1" v-model="REFBILLCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">送校单位</label>
            <input type="text" class="rr-flex-1" v-model="CUSTNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">设备名称</label>
            <input type="text" class="rr-flex-1" v-model="MNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">出厂编号</label>
            <input type="text" class="rr-flex-1" v-model="OPCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">型号规格</label>
            <input type="text" class="rr-flex-1" v-model="SIZETYPE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">生产厂家</label>
            <input type="text" class="rr-flex-1" v-model="MANUFACTURER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">检校依据</label>
            <input type="text" class="rr-flex-1" v-model="TSTANDARDNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">文档标题</label>
            <input type="text" class="rr-flex-1" v-model="DOCTITLE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">证书编号</label>
            <input type="text" class="rr-flex-1" v-model="CERTCODE" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">证书有效期</label>
            <DateRangePicker class="rr-flex-1" v-model="EXPDATE"></DateRangePicker>
          </div>
        </Cell>

        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">检校员</label>
            <input type="text" class="rr-flex-1" v-model="CREATER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">核验员</label>
            <input type="text" class="rr-flex-1" v-model="CHECKER" />
          </div>
        </Cell>

        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">审批员</label>
            <input type="text" class="rr-flex-1" v-model="VERIFIER" />
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
      <Button color="primary" icon="h-icon-list" v-if="ISSHOWLOGLIST" @click="showLogList">查看变更记录</Button>
      <Tooltip
        theme="white"
        v-per="'LI_M02/A12'"
        trigger="click"
        editable
        v-if="ISSHOWCHECK"
        ref="checkTip"
      >
        <Button class="ml5" icon="h-icon-check" color="primary">审核</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" placeholder="输入审核说明" v-model="REMARK" style="width: 200px;"></textarea>
            <AutoComplete
              placeholder="请选择下一审批人"
              :option="empParam1"
              v-model="VERIFYID"
              @change="v=>this.VERIFYER = v.title"
            ></AutoComplete>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchCheck(ID);$refs.checkTip.hide();">通过</Button>
            <Button
              class="ml5"
              color="red"
              @click.native="batchCheckReject(ID);$refs.checkTip.hide();"
            >驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审核？" v-per="'LI_M02/A13'" v-if="ISSHOWRECHECK" @confirm="batchReCheck">
        <Button color="red" icon="h-icon-close">撤销审核</Button>
      </Poptip>
       <Button
        color="primary"
        class="f13"
        v-per="'LI_M02/A49'"
        icon="rr-font rr-font-dayin"
        @click="printPreview"
        v-if="ISSHOWPRINTPREVIEW"
        >证书预览</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd1 from './add1.vue';
import rsLogList from './logList.vue';
import List01 from '@/mixins/list01';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { mapState, mapGetters, mapDateTable, Constants } from '../store1';
export default {
  name: 'r01-m021-main',
  components: {
    rsAdd1,
    rsLogList,
  },
  computed: {
    ...mapDateTable('QQRY', [
      'BILLDATE',
      'REFBILLCODE',
      'CUSTNAME',
      'MNAME',
      'ISONSITE',
      'CERTCODE',
      'EXPDATE',
      'CREATER',
      'CHECKER',
      'VERIFIER',
      'STATE',
      'OPCODE',
      'SIZETYPE',
      'MANUFACTURER',
      'TSTANDARDNAME',
      'DOCTITLE',
      'WTCODE'
    ]),
    ...mapDateTable('EMPUSER', []),
    ISSHOWGENCERT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 6;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWLOGLIST() {
      return this.checks.length === 1;
    },
    ISSHOWPRINTPREVIEW() {
      return this.checks.length === 1;
    },
  },
  mixins: [List01],
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
          title: this.$route.meta.title,
        },
      ],
      param: [
        { title: '待审核', key: 2 },
        { title: '已审核', key: 3 },
      ],
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      checks: [],
      REMARK: '',
      pdfSrc: '',
    };
  },
  methods: {
    add() {
      this.CDID = '';

      this.$refs.madd.show();
    },
    clickRow(row) {
      this.CDID = row.ID;
      this.citem = row;
      this.$refs.madd.show();
    },
    showLogList() {
      if (this.checks.length == 1) {
        this.CDID = this.checks[0].ID;
      }
      this.$refs.mloglist.show();
    },
    selectRow(checks) {
      this.checks = checks || [];
    },
    advQuery(param) {
      this.$refs.list.advQuery();
    },
    batchGenCert() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchGenCert`,
        param: { items: this.checks },
        successText: '操作成功',
      });
    },
    async empSel1(INPUT, callback) {
      if (this.TEMP1 === INPUT) {
        INPUT = '';
      }
      await this.$callAction({ action: `${Constants.STORE_NAME}/empSel1`,
        param: {
          INPUT,
          FUNCID: '3be11623d4114bc68a8e63551e861ced',
          DEPTID: this.checks[0].ADEPTID,
        },
        isBusy: false });
      callback(this.EMPUSER);
    },
    printPreview() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择！');
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/printPreview`,
        param: { ID: item.ID },
        successCall: ret => {
          this.pdfSrc = db.getUrl('pdf') + ret;
          this.$refs.ppdf.show();
        },
      });
    },
  },
};
</script>
