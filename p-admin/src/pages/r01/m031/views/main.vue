<template>
  <div class="h-panel h-panel-no-border m031-wrap">
    <div class="h-panel-bar rr-flex-row">
      <span class="h-panel-title">
        <Breadcrumb :datas="datas"></Breadcrumb>
      </span>
      <div class="h-panel-right">
        <Tabs class="m031-tabs" :datas="tabDatas" v-model="activeTab" @change="onTabChange"></Tabs>
        <Search placeholder="请输入关键字" v-model="INPUT" style="width:300px;" @search="query" v-if="activeTab === 'detail'" />
        <Button class="ml5" @click="showQuery=!showQuery; calcTableHeight()" v-if="activeTab === 'detail'">高级查询</Button>
        <Button class="ml5" color="primary" @click="querySum" v-if="activeTab === 'summary'">查询</Button>
      </div>
    </div>
    <div class="h-panel-body m031-body">
      <!-- 明细视图 -->
      <template v-if="activeTab === 'detail'">
      <div style="padding:10px 0px;" v-if="showQuery">
        <Row :space="9">
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">业务类型</label>
            <Select class="rr-flex-1" v-model="BUSTYPEID" :datas="param1"></Select>
          </div>
        </Cell>
        <Cell width="6">
          <div class="rr-flex-row">
            <label class="rr-justify" style="width:60px">委托编号</label>
            <input type="text" class="rr-flex-1" v-model="WTCODE" />
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
      </div>
      <div class="m031-table-wrap" ref="tableWrap">
        <Table border :height="tableHeight" ref="selection" :datas="QRY"
        :checkbox="true"
        @select="onSelect"
        @trdblclick="clickRow"
        >
          <TableItem title="#" prop="$serial" :width="40" fixed="left"></TableItem>
          <TableItem title="业务类型" prop="BUSTYPENAME" :width="80" fixed="left"></TableItem>
          <TableItem title="委托单号" prop="WTCODE" :width="150" fixed="left" treeOpener></TableItem>
          <TableItem title="客户名称" prop="CUSTNAME" :width="250"  fixed="left">
          </TableItem>
          <TableItem title="受理日期" prop="BILLDATE" :width="80"></TableItem>
          <TableItem title="联系人" prop="LINKER" :width="70"></TableItem>
          <TableItem title="联系方式" prop="MOBILE" :width="100"></TableItem>
          <TableItem title="受理单号" prop="BILLCODE" :width="120"></TableItem>
          <TableItem title="仪器名称" prop="MNAME" :width="150"></TableItem>
          <TableItem title="规格" prop="SIZETYPE" :width="150"></TableItem>
          <TableItem title="数量" prop="CNT" :width="150"></TableItem>
          <TableItem title="出厂编号" prop="OPCODE" :width="150"></TableItem>
          <TableItem title="制造单位" prop="MANUFACTURER" :width="150"></TableItem>
          <TableItem title="受理部门" prop="DEPTNAME" :width="150"></TableItem>
          <TableItem title="受理人" prop="EMPNAME" :width="150"></TableItem>
          <TableItem title="委托单位" prop="WCUSTNAME" :width="150"></TableItem>
          <TableItem title="强检免费" prop="ISFFREE" :width="150"></TableItem>
          <TableItem title="检测费" prop="CAMT" :width="150"></TableItem>
          <TableItem title="新购/维修费用" prop="BAMT" :width="150"></TableItem>
          <TableItem title="其他费用" prop="OAMT" :width="150"></TableItem>
          <TableItem title="费用说明" prop="OREMARK" :width="150"></TableItem>
          <TableItem title="折扣" prop="DISCOUNT" :width="150"></TableItem>
          <TableItem title="应收费用" prop="AMT" :width="150"></TableItem>
          <TableItem title="实收费用" prop="RAMT" :width="150"></TableItem>
          <TableItem title="收费人" prop="CHARGER" :width="150"></TableItem>
          <TableItem title="收费时间" prop="CHARGETIME" :width="150"></TableItem>
        </Table>
      </div>
        <table-tool-bar v-model="pageInfo" @change="changePage">
          <template >
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
            <Button color="primary"  v-per="'LI_M031/A13'" icon="h-icon-task" v-if="ISFEE" @click="batchFee">收费</Button>
            <Poptip content="确定撤销收费？" v-per="'LI_M031/A14'" v-if="ISREFEE" @confirm="batchReFee">
              <Button color="red" icon="h-icon-close">撤销收费</Button>
            </Poptip>
            <Button
            color="primary"
            class="f13"
            icon="rr-font rr-font-dayin"
            @click="aprint"
            v-if="ISSHOWAPRINT"
          >打印</Button>
          </template>
        </table-tool-bar>
      </template>
      <!-- 汇总视图 -->
      <template v-if="activeTab === 'summary'">
        <div style="padding:10px 0px;">
          <Row :space="9">
            <Cell width="6">
              <div class="rr-flex-row">
                <label class="rr-justify" style="width:60px">项目名称</label>
                <input type="text" class="rr-flex-1" v-model="sumPTEMPLATENAME" placeholder="项目名称" />
              </div>
            </Cell>
            <Cell width="6">
              <div class="rr-flex-row">
                <label class="rr-justify" style="width:60px">受理部门</label>
                <input type="text" class="rr-flex-1" v-model="sumADEPTNAME" placeholder="受理部门" />
              </div>
            </Cell>
          </Row>
        </div>
        <div class="m031-table-wrap" ref="sumTableWrap">
          <Table border :height="tableHeight" ref="sumTable" :datas="SUM">
            <TableItem title="#" prop="$serial" :width="40"></TableItem>
            <TableItem title="项目名称" prop="PTEMPLATENAME" :width="250"></TableItem>
            <TableItem title="受理部门" prop="ADEPTNAME" :width="120"></TableItem>
            <TableItem title="受理单数" prop="TOTALCNT" :width="80"></TableItem>
            <TableItem title="检测费合计" prop="TOTALCAMT" :width="100"></TableItem>
            <TableItem title="其他费合计" prop="TOTALOAMT" :width="100"></TableItem>
            <TableItem title="加急费合计" prop="TOTALBAMT" :width="100"></TableItem>
            <TableItem title="应收合计" prop="TOTALAMT" :width="100"></TableItem>
            <TableItem title="实收合计" prop="TOTALRAMT" :width="100"></TableItem>
            <TableItem title="已收费" prop="CHARGEDCNT" :width="80"></TableItem>
            <TableItem title="未收费金额" prop="UNCHARGEDAMT" :width="100"></TableItem>
          </Table>
        </div>
        <div style="padding:10px 0; text-align:right; color:#999;" v-if="SUM.length > 0">
          共 {{ SUM.length }} 个项目，应收合计 {{ sumTotalAMT }}，实收合计 {{ sumTotalRAMT }}，未收费 {{ sumTotalUncharged }}
        </div>
      </template>
    </div>
    <Modal
      v-model="modal1"
      title="重置"
      :styles="{top: '20px'}"
      width="80%"
      :loading="loading"
      :mask-closable="false"
      @on-cancel="close(false)"
    >
      <div></div>
    </Modal>

    <rs-modal ref="madd">
      <rsAdd :storeName="store.Constants.STORE_NAME" title="费用管理" :ID="CDID"></rsAdd>
    </rs-modal>
    <rs-modal ref="mpdf">
      <rs-print-pdf :src="pdfSrc"></rs-print-pdf>
    </rs-modal>
  </div>
</template>

<script>
import { mapState, mapGetters, mapDateTable, Constants } from '../store';
import Gen from '@/utils/gen';
import rsAdd from './add.vue';
// eslint-disable-next-line no-restricted-imports
import db from '@/api/db';
export default {
  name: 'r01-m031-main',
  components: {
    rsAdd,
  },
  computed: {
    ...mapDateTable('QRY', []),
    ...mapDateTable('MAIN', []),
    ...mapDateTable('QRY1', []),
    ...mapDateTable('SUM', []),
    ...mapDateTable('QQRY', ['WTCODE', 'CUSTNAME', 'LINKER', 'STATE', 'BUSTYPEID', 'INPUT', 'TotalCount', 'PageSize', 'PageIndex']),
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
    ISSHOWAPRINT(state, getters, rootState, rootGetters) {
      if (this.checks.length == 0) return false;
      let check = this.checks[0];
      let fchecks = this.checks.filter(item => {
        return (
          check['CUSTNAME'] == item['CUSTNAME']
        );
      });
      // 打印
      return this.checks.length > 0 && fchecks.length === this.checks.length;
    },
    TREEQRY() {
      let ret = [];
      this.QRY.map(r => {
        ret.push(r);
        r.children = this.QRY1.filter(q => {
          return q.WTCODE == r.WTCODE && r.CUSTID == q.CUSTID && r.BILLDATE == q.BILLDATE && r.BUSTYPEID == q.BUSTYPEID;
        });
      });
      console.log('TREEQRY', ret);
      return ret;
    },
    sumTotalAMT() {
      return this.SUM.reduce((s, r) => s + (parseFloat(r.TOTALAMT) || 0), 0).toFixed(2);
    },
    sumTotalRAMT() {
      return this.SUM.reduce((s, r) => s + (parseFloat(r.TOTALRAMT) || 0), 0).toFixed(2);
    },
    sumTotalUncharged() {
      return this.SUM.reduce((s, r) => s + (parseFloat(r.UNCHARGEDAMT) || 0), 0).toFixed(2);
    },
    pageInfo: {
      get() {
        return {
          page: this.PageIndex,
          size: this.PageSize,
          total: this.TotalCount,
          pagerSize: 1,
        };
      },
      set(v) {
        this.PageIndex = v.page;
        this.PageSize = v.size;
      },
    },
  },
  data() {
    return {
      CDID: '',
      citem: {},
      showQuery: false,
      store: { mapState, mapGetters, mapDateTable, Constants },
      modal1: false,
      modal2: false,
      loading: true,
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
      _isUpdatingSelection: false,
      _prevChecks: [],
      DISCOUNT: 1,
      pdfSrc: '',
      tableHeight: 300,
      activeTab: 'detail',
      tabDatas: [{ title: '费用明细', key: 'detail' }, { title: '按项目汇总', key: 'summary' }],
      sumPTEMPLATENAME: '',
      sumADEPTNAME: '',
    };
  },
  methods: {
    calcTableHeight() {
      this.$nextTick(() => {
        let wrap = this.$refs.tableWrap;
        if (wrap) {
          // HeyUI 的 height prop 是设给 body 的 maxHeight，不含表头
          // 所以 tableHeight 应 = wrap总高度 - 表头高度，让 body 完整填充剩余空间
          let headerEl = wrap.querySelector('.h-table-header');
          let headerH = headerEl ? headerEl.offsetHeight : 40;
          this.tableHeight = wrap.clientHeight - headerH;
        }
      });
    },
    getTreeData(datas, up) {
      let aobj = [];
      aobj = datas.filter(item => (item.UPFUNCID || '') === up);
      aobj.forEach(element => {
        let tobj = this.getTreeData(datas, element.ID);
        if (tobj.length > 0) {
          element.children = tobj;
        }
      });
      console.log('aobj', aobj);
      return aobj;
    },
    onSelect(selection) {
      if (this._isUpdatingSelection) return;

      let newSelection = [...selection];
      const prev = this._prevChecks || [];

      // 父节点选中/取消 → 级联子节点
      this.QRY.forEach(parent => {
        if (!parent.children || !parent.children.length) return;
        const wasChecked = prev.includes(parent);
        const isChecked = newSelection.includes(parent);

        if (isChecked && !wasChecked) {
          // 选中父 → 选中所有子
          parent.children.forEach(c => {
            if (!newSelection.includes(c)) newSelection.push(c);
          });
        } else if (!isChecked && wasChecked) {
          // 取消父 → 取消所有子
          newSelection = newSelection.filter(item => !parent.children.includes(item));
        }
      });

      // 子节点变化 → 同步父节点选中状态
      this.QRY.forEach(parent => {
        if (!parent.children || !parent.children.length) return;
        const all = parent.children.every(c => newSelection.includes(c));
        const none = !parent.children.some(c => newSelection.includes(c));
        if (all && !newSelection.includes(parent)) {
          newSelection.push(parent);
        } else if (none && newSelection.includes(parent)) {
          newSelection = newSelection.filter(i => i !== parent);
        }
      });

      this._prevChecks = [...newSelection];
      this.checks = newSelection;

      const tableSel = this.$refs.selection.getSelection();
      if (newSelection.length !== tableSel.length) {
        this._isUpdatingSelection = true;
        this.$refs.selection.setSelection(newSelection);
      }

      this.$nextTick(() => {
        this._isUpdatingSelection = false;
        this.setIndeterminate();
      });
    },
    setIndeterminate() {
      this.$nextTick(() => {
        const table = this.$refs.selection;
        if (!table) return;

        const flatDatas = table.tableDatas || [];
        const mainBody = table.$el.querySelector('.h-table-body tbody');
        if (!mainBody) return;

        const rows = mainBody.querySelectorAll('tr');
        rows.forEach((tr, idx) => {
          if (idx >= flatDatas.length) return;
          const data = flatDatas[idx];
          if (!data.children || !data.children.length) return;

          const selectedCount = data.children.filter(c => this.checks.includes(c)).length;
          const isIndeterminate = selectedCount > 0 && selectedCount < data.children.length;

          const checkboxEl = tr.querySelector('.h-checkbox');
          if (checkboxEl) {
            if (isIndeterminate) {
              checkboxEl.classList.add('h-checkbox-indeterminate');
              checkboxEl.classList.remove('h-checkbox-checked');
            } else {
              checkboxEl.classList.remove('h-checkbox-indeterminate');
            }
          }

          const input = tr.querySelector('input.h-checkbox-native');
          if (input) {
            input.indeterminate = isIndeterminate;
          }
        });
      });
    },
    hasClass(obj, cls) {
      var cls = cls || '';
      if (cls.replace(/\s/g, '').length == 0) {
        return false; // 当cls没有参数时,返回false;
      } else {
        return new RegExp(' ' + cls + '').test(' ' + obj.className);
      }
    },
    add() {
      // this.$store.dispatch(`${Constants.STORE_NAME}/add`);
      this.$callAction({ action: `${Constants.STORE_NAME}/add`, timeOut: 0 });
      this.modal2 = true;
    },
    aprint() {
      let item = this.checks[0];
      if (!item) {
        this.$error('请选择可打印受理单！');
        return;
      }
      this.$callAction({
        action: `${Constants.STORE_NAME}/aprint`,
        param: { items: this.checks.filter(r => r.ID) },
        successCall: ret => {
          this.pdfSrc = db.getUrl('pdf') + ret;
          this.$refs.mpdf.show();
        },
      });
    },
    query(param) {
      if (param !== 1) {
        this.PageIndex = 1;
      } else {
        param = {};
      }
      param = { ...param};
      this.$callAction({
        action: `${Constants.STORE_NAME}/query`,
        param: param,
        timeOut: 0,
        successCall: () => {
          this.calcTableHeight();
        },
      });
    },
    advQuery(param) {
      if (param !== 1) {
        this.PageIndex = 1;
      } else {
        param = {};
      }
      param = { ...param };
      param.sumFields = this.sumFields;
      this.$callAction({
        action: `${this.store.Constants.STORE_NAME}/advQuery`,
        param: param,
        timeOut: 0,
        successCall: () => {
          this.calcTableHeight();
        },
      });
    },
    clickRow(row, $event) {
      console.log(
        '$event.srcElement.querySelector(".h-table-tree-icon")',
        $event.srcElement.querySelector('.h-table-tree-icon')
      );
      if (
        $event.srcElement.querySelector('.h-table-tree-icon') ||
        this.hasClass($event.srcElement, 'h-table-tree-icon')
      ) {
        return;
      }
      if (row.ID) {
        this.CDID = row.ID;
        this.$refs.madd.show();
      }
    },
    renderFuncName(data) {
      return `<i class="${data.FUNCICON}"></i>${data.FUNCNAME}`;
    },
    changePage(pageInfo) {
      this.PageIndex = pageInfo.page;
      this.PageSize = pageInfo.size;
      if (this.showQuery) {
        this.advQuery(1);
      } else {
        this.query(1);
      }
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
    onTabChange() {
      if (this.activeTab === 'summary') {
        this.querySum();
      }
    },
    querySum() {
      this.$callAction({
        action: `${Constants.STORE_NAME}/queryProjectSum`,
        param: { PTEMPLATENAME: this.sumPTEMPLATENAME, ADEPTNAME: this.sumADEPTNAME },
      });
    },
  },
  async mounted() {
    // eslint-disable-next-line no-restricted-syntax
    await this.$store.dispatch('app/initScms', [this.$MAIN.scm]);
    this.columns4 = Gen.getTableColumns(this.$store.state.app.scms[this.$MAIN.scm], {});
    this.PageSize = 20;
    this.query();
    this.calcTableHeight();
    this._resizeHandler = () => this.calcTableHeight();
    window.addEventListener('resize', this._resizeHandler);
  },
  beforeDestroy() {
    window.removeEventListener('resize', this._resizeHandler);
  },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
.m031-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
}
/deep/ .h-panel-bar {
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
  padding: 10px 20px;
  display: flex;
  align-items: center;
}
/deep/ .h-breadcrumb a {
  color: @primary-color;
}
/deep/ .h-btn-primary {
  background-color: @primary-color;
}
.m031-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 10px 20px;
}
.m031-table-wrap {
  flex: 1;
  overflow: hidden;
}
/deep/ .h-table {
  max-height: calc(100% - 10px);
  height: calc(100% - 10px);
}
/deep/ .h-table-container {
  max-height: calc(100% - 40px);
  overflow-y: auto;
  height: calc(100% - 10px);
}
/deep/ .h-table-body {
  overflow-y: auto;
}
/deep/ .h-page {
  height: 32px;
}
/deep/ .rr-table-toolBar {
  flex-shrink: 0;
}
.m031-tabs {
  display: inline-block;
  margin-right: 12px;
}
</style>
