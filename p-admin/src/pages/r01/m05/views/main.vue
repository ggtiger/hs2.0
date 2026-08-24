<template>
  <list-t01
    title="受理单"
    :bcDatas="datas"
    :store="store"
    @list-click-row="clickRow"
    @list-action="listAction"
    :showQuery="showQuery"
    :checkbox="true"
    @list-select="selectRow"
    ref="list"
  >
  <template slot="simple-query">
      <Row :space="9" >
         <Cell width="2">
         </Cell>
         <Cell width="11">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">业务类型</label>
            <Select class="rr-flex-1" v-model="BUSTYPEID" :datas="param1"></Select>
          </div>
        </Cell>
        <Cell width="11">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">状态</label>
            <Select class="rr-flex-1" v-model="STATE" :datas="param"></Select>
          </div>
        </Cell>
      </Row>
   </template>
    <rs-modal ref="madd">
      <rsAdd
        :storeName="store.Constants.STORE_NAME"
        :showQuery="showQuery"
        :item="citem"
        title="受理单"
        :ID="CDID"
      ></rsAdd>
    </rs-modal>
    <rs-modal ref="mpdf">
      <rs-print-pdf :src="pdfSrc"></rs-print-pdf>
    </rs-modal>
    <rs-modal ref="mlogistics" :width="700">
      <rsLogisticsAdd :storeName="logisticsStoreName" title="添加物流" :default-values="logisticsDefaultValues" @saved="onLogisticsSaved"></rsLogisticsAdd>
    </rs-modal>
    <template slot="body-query">
      <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">受理日期</label>
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
            <label class="rr-justify" style="width:60px">业务类型</label>
            <Select class="rr-flex-1" v-model="BUSTYPEID" :datas="param1"></Select>
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
            <label class="rr-justify" style="width:60px">检验部门</label>
            <input type="text" class="rr-flex-1" v-model="DEPTNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">检验员</label>
            <input type="text" class="rr-flex-1" v-model="EMPNAME" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">协议时间</label>
            <DateRangePicker class="rr-flex-1" v-model="AGREEDATE"></DateRangePicker>
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
            <label class="rr-justify" style="width:60px">型号规格</label>
            <input type="text" class="rr-flex-1" v-model="SIZETYPE" />
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
            <label class="rr-justify" style="width:60px">制造单位</label>
            <input type="text" class="rr-flex-1" v-model="MANUFACTURER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">记录模板</label>
            <input type="text" class="rr-flex-1" v-model="PTEMPLATENAME" />
          </div>
        </Cell>
         <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">创建人</label>
            <input type="text" class="rr-flex-1" v-model="CREATER" />
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">状态</label>
            <Select class="rr-flex-1" v-model="STATE" :datas="param"></Select>
          </div>
        </Cell>
         <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">委托方要求</label>
            <input type="text" class="rr-flex-1" v-model="REMARK2" />
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
      <Button color="primary" v-per="'LI_M00/A04'" icon="h-icon-plus" @click="add">添加</Button>
      <Button color="primary" v-per="'R02_M07/A04'" icon="h-icon-search" @click="addLogistics" v-if="ISSHOWLOGISTICS">添加物流</Button>
      <Poptip content="确定退样？" v-per="'LI_M00/A51'" v-if="ISSHOWRETURN" @confirm="doBatchReturn">
        <Button color="red" icon="h-icon-close">确定退样</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'LI_M00/A08'"
        icon="h-icon-check"
        @click="batchSubmit"
        v-if="ISSHOWSUBMIT"
      >提交</Button>
      <Poptip content="确定撤销提交？" v-per="'LI_M00/A23'" v-if="ISSHOWRESUBMIT" @confirm="batchReSubmit">
        <Button color="red" icon="h-icon-close">撤销提交</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'LI_M00/A23'"
        icon="h-icon-check"
        @click="batchComplete"
        v-if="ISSHOWCOMPLETE"
      >完成</Button>
      <Poptip content="确定撤销提交？" v-per="'LI_M00/A24'" v-if="ISSHOWRECOMPLETE" @confirm="batchReComplete">
        <Button color="red" icon="h-icon-close">撤销完成</Button>
      </Poptip>
      <Button
        color="primary"
        v-per="'LI_M00/A14'"
        icon="h-icon-task"
        @click="batchAccept"
        v-if="ISSHOWACCEPT"
      >受理</Button>
      <Poptip content="确定撤销受理？" v-per="'LI_M00/A15'" v-if="ISSHOWREACCEPT" @confirm="batchReAccept">
        <Button color="red" icon="h-icon-close">撤销受理</Button>
      </Poptip>
      <Button
        color="primary"
        class="f13"
        v-per="'LI_M00/A16'"
        icon="rr-font rr-font-dayin"
        @click="aprint"
        v-if="ISSHOWAPRINT"
      >受理打印</Button>
      <Button
        color="primary"
        class="f13"
        v-per="'LI_M00/A16'"
        icon="rr-font rr-font-dayin"
        @click="pprint"
      >受理便签打印</Button>
      <Button
        color="primary"
        class="f13"
        v-per="'LI_M00/A16'"
        icon="rr-font rr-font-dayin"
        @click="print"
        v-if="ISSHOWPRINT"
      >证书打印</Button>
      <Button
        color="primary"
        class="f13"
        icon="rr-font rr-font-dayin"
        v-per="'LI_M00/A20'"
        @click="download"
        v-if="ISSHOWPDOWNLOAD"
      >证书下载</Button>
    </template>
  </list-t01>
</template>
<script>
import rsAdd from './add.vue';
import rsLogisticsAdd from '@/pages/r02/m07/views/add.vue';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
export default {
  name: 'r01-m05-main',
  components: {
    rsAdd,
    rsLogisticsAdd,
  },
  computed: {
    ...mapDateTable('QQRY', [
      'BILLDATE',
      'BILLCODE',
      'CUSTNAME',
      'LINKER',
      'MNAME',
      'EMPNAME',
      'STATE',
      'BUSTYPEID',
      'SIZETYPE',
      'OPCODE',
      'MANUFACTURER',
      'DEPTNAME',
      'AGREEDATE',
      'PTEMPLATENAME',
      'CREATER',
      'WTCODE',
      'REMARK2'
    ]),
    // 把选中受理单转成物流表单 ACCEPTS 字段的默认值（multiautocomplete 远程对象形状）
    // add.vue 的 applyDefaultValues 会按 subMappings 映射成 DTSA 子表行
    logisticsDefaultValues() {
      return {
        ACCEPTS: (this.logisticsAcceptItems || []).map(a => ({ ID: a.ACCEPTID, BILLCODE: a.ACCEPTCODE })),
      };
    },
    ISSHOWRETURN() {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 1;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWSUBMIT(state, getters, rootState, rootGetters) {
      // 提交
      let fchecks = this.checks.filter(item => {
        return item.STATE === 1;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWRESUBMIT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 7 || item.STATE === 8;
      });
      // 撤销提交
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWCOMPLETE(state, getters, rootState, rootGetters) {
      // 提交
      let fchecks = this.checks.filter(item => {
        return item.STATE === 7;
      });
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWRECOMPLETE(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 15;
      });
      // 撤销提交
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWACCEPT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 7;
      });
      // 受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWREACCEPT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 8 && this.$store.state['user'].userInfo.EMPID === item.AEMPID;
      });
      // 撤销受理
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWPRINT(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10 || item.STATE === 11 || item.STATE === 14;
      });
      // 打印
      return this.checks.length === 1 && fchecks.length === this.checks.length;
    },
    ISSHOWPDOWNLOAD(state, getters, rootState, rootGetters) {
      let fchecks = this.checks.filter(item => {
        return item.STATE === 10 || item.STATE === 11 || item.STATE === 14;
      });
      // 打印
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWAPRINT(state, getters, rootState, rootGetters) {
      if (this.checks.length == 0) return false;
      let check = this.checks[0];
      let fchecks = this.checks.filter(item => {
        return (
          check['BILLDATE'] == item['BILLDATE'] &&
          check['CUSTNAME'] == item['CUSTNAME'] &&
          (check['SENDNAME'] == item['SENDNAME'] || (!check['SENDNAME'] && !item['SENDNAME']))
        );
      });
      // 打印
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    ISSHOWLOGISTICS() {
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
          title: '受理单',
        },
      ],
      param: [
        { title: '全部', key: '' },
        { title: '待提交', key: 1 },
        { title: '待接收', key: 7 },
        { title: '待检验', key: 8 },
        { title: '待签发', key: 9 },
        { title: '已签发', key: 10 },
        { title: '已打印', key: 11 },
        { title: '已下载', key: 14 },
        { title: '已完成', key: 15 },
        { title: '已退样', key: 21 },
      ],
      param1: [
        { title: '委外', key: 1 },
        { title: '自检', key: 2},
        { title: '期间核查', key: 3},
        { title: '其他', key: 4},
      ],
      checks: [],
      pdfSrc: '',
      citem: {},
      logisticsAcceptItems: [],
      logisticsStoreName: 'r02/m07',
    };
  },
  methods: {
    add() {
      this.citem = this.checks[0];
      this.CDID = '';
      this.$refs.madd.show();
    },
    print() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择可打印受理单！');
        return;
      }
      if (!item.CERTID) {
        this.$error('证书未生成！');
        return;
      }
      if (item['STATE'] === 10) {
        this.$callAction({
          action: `${Constants.STORE_NAME}/print`,
          param: { ID: item.ID },
        });
      }
      this.pdfSrc = db.getUrl('pdf') + item.CERTID;
      this.$refs.mpdf.show();
    },
    aprint() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择可打印受理单！');
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/aprint`,
        param: { items: this.checks },
        successCall: ret => {
          this.pdfSrc = db.getUrl('pdf') + ret;
          this.$refs.mpdf.show();
        },
      });
    },
    pprint() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择可打印受理单！');
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/pprint`,
        param: { items: this.checks },
        successCall: ret => {
          this.pdfSrc = db.getUrl('pdf') + ret;
          this.$refs.mpdf.show();
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
    clickRow(row) {
      this.CDID = row.ID;
      this.$refs.madd.show();
    },
    listAction() {},
    selectRow(checks) {
      this.checks = checks;
    },
    advQuery(param) {
      this.$refs.list.advQuery();
    },
    query() {
      this.$refs.list.query();
    },
    async addLogistics() {
      if (!this.checks || this.checks.length === 0) {
        this.$alert('请先选择受理单');
        return;
      }
      // 检查每个受理单是否已有物流记录
      let existCodes = [];
      for (let i = 0; i < this.checks.length; i++) {
        let item = this.checks[i];
        try {
          let items = await this.$callAction({
            action: Constants.STORE_NAME + '/checkLogisticsExists',
            param: { acceptId: item.ID },
            isBusy: false,
          });
          if (items && items.length > 0) {
            existCodes.push(item.BILLCODE);
          }
        } catch (e) {
          // 查询失败不阻断流程
        }
      }
      if (existCodes.length > 0) {
        let confirmed = await new Promise(resolve => {
          this.$Confirm(`以下受理单已有物流记录：${existCodes.join('、')}，是否继续添加？`, '提示', () => resolve(true)).catch(() => resolve(false));
        });
        if (!confirmed) return;
      }
      this.logisticsAcceptItems = this.checks.map(item => ({
        ACCEPTID: item.ID,
        ACCEPTCODE: item.BILLCODE,
      }));
      // 确保 R02_M07 模块配置已加载（store 初始化依赖）
      if (!this.$store.state['app'].modules['R02_M07']) {
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initModule', 'R02_M07');
      }
      // 确保 scm（字段配置）已加载，否则 rs-form-edit 的 created 钩子会报错
      let modData = this.$store.state['app'].modules['R02_M07'];
      if (modData && modData.MODPATH) {
        let resNames = [...new Set(modData.MODPATH.map(p => p.RESOURCENAME))];
        // eslint-disable-next-line no-restricted-syntax
        await this.$store.dispatch('app/initScms', resNames);
      }
      this.$refs.mlogistics.show();
    },
    onLogisticsSaved() {
      this.$refs.mlogistics.setvalue(false);
    },
    doBatchReturn() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReturn`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchSubmit() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchSubmit`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchReSubmit() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReSubmit`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchComplete() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchComplete`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchReComplete() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReComplete`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchAccept() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchAccept`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
    batchReAccept() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/batchReAccept`,
        param: { items: this.checks },
        successText: '操作成功',
        successCall: () => {
          if (this.showQuery) {
            this.advQuery();
          } else {
            this.query();
          }
        },
      });
    },
  },
};
</script>
<style scoped>
.f13 {
  font-size: 13px;
}
</style>
