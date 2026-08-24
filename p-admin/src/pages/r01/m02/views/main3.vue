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
      <rsAdd :storeName="store.Constants.STORE_NAME" :citem="citem" title="原始记录" :ID="CDID"></rsAdd>
    </rs-modal>
    <rs-modal ref="mpdf">
      <rs-print-pdf :src="pdfSrc"></rs-print-pdf>
    </rs-modal>
    <rs-modal ref="ppdf">
      <rs-print-pdf :src="pdfSrc" type="preview"></rs-print-pdf>
    </rs-modal>
    <rs-modal ref="mECertPwd" :hasClose="true" autoWidth>
      <div style="padding: 20px;">
        <h4 style="margin-bottom: 15px;">电子签发 - 设置查看密码</h4>
        <p style="color: #999; font-size: 13px; margin-bottom: 15px;">如需对电子证书设置查看密码，请输入密码；留空则不设密码保护</p>
        <div class="rr-flex-row">
          <label class="rr-justify" style="width: 80px">查看密码</label>
          <input type="password" class="rr-flex-1" v-model="ecertPwd" placeholder="留空则不设密码" />
        </div>
        <div style="text-align: right; margin-top: 20px;">
          <Button @click="$refs.mECertPwd.hide()">取消</Button>
          <Button color="primary" @click="confirmECertSign">确认签发</Button>
        </div>
      </div>
    </rs-modal>
    <rs-modal ref="mResetPwd" :hasClose="true" autoWidth>
      <div style="padding: 20px;">
        <h4 style="margin-bottom: 15px;">重置电子证书查看密码</h4>
        <p style="color: #999; font-size: 13px; margin-bottom: 15px;">输入新密码覆盖旧密码；留空则清除密码保护</p>
        <div class="rr-flex-row">
          <label class="rr-justify" style="width: 80px">新密码</label>
          <input type="password" class="rr-flex-1" v-model="resetPwd" placeholder="留空则清除密码" />
        </div>
        <div style="text-align: right; margin-top: 20px;">
          <Button @click="$refs.mResetPwd.hide()">取消</Button>
          <Button color="primary" @click="confirmResetPwd">确认</Button>
        </div>
      </div>
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
      <!--
      <Button color="primary" icon="h-icon-list" v-if="ISSHOWLOGLIST" @click="showLogList">查看变更记录</Button>
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
      -->
      <Button
        color="primary"
        v-per="'LI_M02/A27'"
        icon="h-icon-check"
        @click="batchGenCert"
        v-show="ISSHOWGENCERT"
      >证书签发</Button>

      <Poptip content="确定撤销审批？" v-per="'LI_M02/A50'" v-show="ISSHOWREGENCERT" @confirm="batchReGenCert">
        <Button color="red" icon="h-icon-close">撤销签发</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'LI_M02/A55'"
        icon="h-icon-check"
        @click="batchECertSign"
        v-show="ISSHOWECERTSIGN"
      >电子签发</Button>
      <Button
        color="primary"
        v-per="'LI_M02/A58'"
        icon="h-icon-edit"
        @click="batchResetPwd"
        v-show="ISSHOWRESETPWD"
      >重置密码</Button>
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
import rsAdd from './add3.vue';
import rsLogList from './logList.vue';
import List01 from '@/mixins/list01';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { mapState, mapGetters, mapDateTable, Constants } from '../store3';
export default {
  name: 'r01-m023-main',
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

    ISSHOWGENCERT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 6;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWREGENCERT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWECERTSIGN(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10 && item.ECERTSIGN !== 1;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWRESETPWD(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.ECERTSIGN === 1;
      });
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
        { title: '待签发', key: 6 },
        { title: '已签发', key: 10 },
      ],
      checks: [],
      REMARK: '',
      pdfSrc: '',
      ecertPwd: '',
      ecertSignType: '', // 'batch' or 'single'
      resetPwd: '',
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
    batchReGenCert() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReGenCert`,
        param: { items: this.checks },
        successText: '操作成功',
      });
    },
    batchECertSign() {
      this.ecertSignType = 'batch';
      this.ecertPwd = '';
      this.$refs.mECertPwd.show();
    },
    confirmECertSign() {
      this.$refs.mECertPwd.hide();
      if (this.ecertSignType === 'batch') {
        this.$callAction({
          action: `${Constants.STORE_NAME}/batchECertSign`,
          param: { items: this.checks, ECERTPWD: this.ecertPwd },
          successText: '电子签发成功',
        });
      }
    },
    batchResetPwd() {
      this.resetPwd = '';
      this.$refs.mResetPwd.show();
    },
    confirmResetPwd() {
      this.$refs.mResetPwd.hide();
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchUpdateECertPwd`,
        param: { items: this.checks, ECERTPWD: this.resetPwd },
        successText: this.resetPwd ? '密码修改成功' : '密码已清除',
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
  },
};
</script>
