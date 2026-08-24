<template>
  <list-t01
    title="原始记录"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    :showQuery="showQuery"
    :checkbox="true"
    @list-select="selectRow"
    ref="list"
  >
    <rs-modal ref="madd" :fullScreen="true">
      <rsAdd :storeName="store.Constants.STORE_NAME" :citem="citem" title="原始记录" :ID="CDID"></rsAdd>
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
            <label class="rr-justify" style="width:60px">审核员</label>
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
      <Button color="primary" v-per="'LI_M02/A04'" icon="h-icon-plus" @click="add">添加</Button>
      <Button color="primary" icon="h-icon-list" v-if="ISSHOWLOGLIST" @click="showLogList">查看变更记录</Button>
      <Tooltip
        theme="white"
        v-per="'LI_M02/A17'"
        trigger="click"
        editable
        v-if="ISSHOWSUBMIT"
        ref="submitTip"
      >
        <Button class="ml5" icon="h-icon-check" color="primary">提交</Button>
        <div slot="content">
          <div v-padding="10">
            <AutoComplete
              placeholder="请选择审核人"
              :option="empParam1"
              v-model="CHECKID"
              @change="v=>this.CHECKER = v.title"
            ></AutoComplete>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchSubmit">确定提交</Button>
          </div>
        </div>

      </Tooltip>
      <Poptip content="确定撤销提交？" v-per="'LI_M02/A18'" v-if="ISSHOWRESUBMIT" @confirm="batchReSubmit">
        <Button color="red" icon="h-icon-close">撤销提交</Button>
      </Poptip>
      <Poptip content="确定更新模版？" v-per="'LI_M02/A45'" v-if="ISSHOWSUBMIT||ISSHOWLOGLIST" @confirm="batchUpdateTemplate">
        <Button color="blue" icon="h-icon-close">更新模版</Button>
      </Poptip>
         <Button
        color="primary"
        class="f13"
        v-per="'LI_M02/A49'"
        icon="rr-font rr-font-dayin"
        @click="printPreview"
        v-if="ISSHOWPRINTPREVIEW"
        >证书预览</Button>
       <!--

      <Button
        color="primary"
        class="f13"
        v-per="'LI_M02/A38'"
        icon="rr-font rr-font-dayin"
        @click="print"
        v-if="ISSHOWPRINT"
      >记录打印</Button>
      <Button
        color="primary"
        class="f13"
        icon="rr-font rr-font-dayin"
        v-per="'LI_M02/A39'"
        @click="download"
        v-if="ISSHOWPDOWNLOAD"
      >记录下载</Button>

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
            <textarea dict="simple" v-model="REMARK" style="width: 200px;"></textarea>
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
      <Tooltip
        theme="white"
        v-per="'LI_M02/A14'"
        trigger="click"
        editable
        v-if="ISSHOWRECHECK"
        ref="verifyTip"
      >
        <Button class="ml5" icon="h-icon-check" color="primary">审批</Button>
        <div slot="content">
          <div v-padding="10">
            <textarea dict="simple" v-model="REMARK" style="width: 200px;"></textarea>
          </div>
          <div v-padding="10" class="text-center">
            <Button color="primary" @click.native="batchVerify(ID);$refs.verifyTip.hide();">通过</Button>
            <Button
              class="ml5"
              color="red"
              @click.native="batchVerifyReject(ID);$refs.verifyTip.hide();"
            >驳回</Button>
          </div>
        </div>
      </Tooltip>
      <Poptip content="确定撤销审批？" v-per="'LI_M02/A15'" v-if="ISSHOWREVERIFY" @confirm="batchReVerify">
        <Button color="red" icon="h-icon-close">撤销审批</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'LI_M02/A21'"
        icon="h-icon-check"
        @click="batchGenCert"
        v-if="ISSHOWGENCERT"
      >证书生成</Button>
      -->
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import rsLogList from './logList.vue';
import List01 from '@/mixins/list01';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'r01-m02-main',
  components: {
    rsAdd,
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
    ISSHOWSUBMIT(state, getters, rootState, rootGetters) {
      // 提交
      let fchecks = this.checks.filter(item => {
        return item.STATE === 1 || item.STATE === 12;
      });
      if (this.checks.length > 0) {
        let ADEPTID = this.checks[0].ADEPTID;
        if (
          this.checks.filter(item => {
            return item.ADEPTID === ADEPTID && (item.CREATEID == this.$store.state.user.userInfo.ID);
          }).length != this.checks.length
        ) {
          return false;
        }
      }
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWPRINT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10;
      });
      // 打印
      return this.checks.length === 1 && fchecks.length === this.checks.length;
    },
    ISSHOWPDOWNLOAD(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10;
      });
      // 打印
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
  },
  mixins: [List01],
  data() {
    return {
      CDID: '',
      citem: {},
      showQuery: false,
      store: { mapState, mapGetters, mapDateTable, Constants },
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
      datas: [
        {
          title: '检验管理',
        },
        {
          title: this.$route.meta.title,
        },
      ],
      param: [
        { title: '全部', key: '' },
        { title: '待提交', key: 1 },
        { title: '待审核', key: 2 },
        { title: '待审批', key: 5 },
        { title: '已驳回', key: 12 },
        { title: '已审批', key: 6 },
        { title: '已签发', key: 10 },
        { title: '已作废', key: 4 },
      ],
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
    batchGenCert() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchGenCert`,
        param: { items: this.checks },
        successText: '操作成功',
      });
    },
    batchUpdateTemplate() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchUpdateTemplate`,
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
          FUNCID: 'a94920a95a6946fca61bcb3421d16ff4',
          DEPTID: this.checks[0].ADEPTID,
        },
        isBusy: false });
      callback(this.EMPUSER);
    },
    print() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择可打印原始记录！');
        return;
      }
      if (!item.EXPFILEID) {
        this.$error('原始记录未生成！');
        return;
      }
      this.pdfSrc = db.getUrl('pdf') + item.EXPFILEID;
      this.$nextTick(() => {
        this.$refs.mpdf.show();
      });
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
    download() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/download`,
        param: { items: this.checks },
        successCall: ret => {
          window.open(`${db.getUrl('upload')}${ret.ID}`, '_black');
        },
      });
    },
    advQuery(param) {
      this.$refs.list.advQuery();
    },
  },
};
</script>
