# r01/m031 费用汇总（LI_M031）→ generic-module + SFC 扩展方案

## 迁移思路

费用汇总页面包含两套视图：**费用明细**（树形表格 + 双查询合并 + 级联选中 + 批量收费/折扣/撤销/打印）与 **按项目汇总**（独立查询 + 统计行）。页面状态复杂，但拆解后绝大部分可纳入 generic-module 体系：

1. **列表整页 SFC** —— 由于双 Tab、树形级联选中、半选中态等是 HeyUI Table 的强交互特性，generic-module 的 list-t01 无法直接承载。改用 `COMPONENTTYPE=sfc`，把整页 `main.vue` 当作 SFC 在线资产加载，内部仍使用 `RsTableList`/原生 Table 渲染，但 **数据通道统一走 generic-store**（`LI_M031`）。
2. **表单页（add.vue）配置化** —— 收费表单字段简单、只有 AMT 联动计算，完全可走 generic-form + EXTENDJS。
3. **Store 扩展** —— 保留 `query/advQuery/queryDetail/queryProjectSum/batchFee/batchReFee/batchDiscount/aprint` 等自定义 action，其余 INIT/ADD/save 走 Store03 默认。

> 关键判断：**不强行配置化列表页**。强行把 25 列冻结+树形+级联+Tab 塞进 list-t01 的 schema 配置会得不偿失；SFC 整页方案保留原交互体验，同时获得热更新、版本管理、统一路由 `/g/LI_M031/main`。

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | COMPONENTTYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|---------------|-----------|---------------|--------------|--------------|
| main | 费用汇总 | list | sfc | /g/LI_M031/main | A01 | - | - |
| add | 费用管理（收费表单） | form | - | /g/LI_M031/add | - | A02 | A04 |

> `COMPONENTTYPE=sfc` + `SFCMODULEPATH` 指向 SFC 资产，generic-module 走 `isSfcPage` 分支整体加载；`PAGETYPE=list` 只是为了让 storeReady 判定走通用分支，实际不渲染 list-t01。

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "PAGETYPE": "list",
  "COMPONENTTYPE": "sfc",
  "SFCMODULEPATH": "@/modules/LI_M031/main.vue",
  "EXTENDJS": "@/modules/LI_M031/main.js",
  "defaultFormPageCode": "add",
  "SLOTS": {}
}
```

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "FORMLAYOUT": "twocolumn",
  "EXTENDJS": "@/modules/LI_M031/form.js"
}
```

### 1.4 按钮配置 (tss_module_button)

main 页 SFC 内部按钮自管（`v-per` + `SHOWCOND`），不强制入 tss_module_button。若要配合权限中心统一管理，可配：

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|----------|----------|----------|
| 折扣 | discount | footer | A12 | `{"action":"batchDiscount","beforeAction":"confirmDiscount"}` | ISDISCOUNT | LI_M031/A12 |
| 收费 | fee | footer | A13 | `{"action":"batchFee"}` | ISFEE | LI_M031/A13 |
| 撤销收费 | refee | footer | A14 | `{"action":"batchReFee","beforeAction":"confirmBatch"}` | ISREFEE | LI_M031/A14 |
| 打印 | aprint | footer | A16 | `{"action":"aprint"}` | ISSHOWAPRINT | LI_M031/A16 |
| 查询项目汇总 | querySum | header | A20 | `{"action":"querySum"}` | - | - |

add 页按钮：

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND |
|---------|---------|---------|---------|----------|
| 修改 | save | footer | A04 | `!CHARGEID` |
| 收费 | mySave | footer | A04 | - |

---

## 二、main 页 SFC 在线资产（`@/modules/LI_M031/main.vue`）

整页 SFC，保留原 main.vue 全部交互（树形、级联选中、半选、Tab、统计行）。关键改动：

1. **去掉 createStore**：通过 `host.storeObj` / `host.$callAction` 复用 generic-store
2. **`mapDateTable` 改为从 host 注入**
3. **datas/bcDatas/options 仍在本组件 data**
4. **calcTableHeight 保留**（列表区有自定义高度逻辑）

```html
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
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">业务类型</label><Select class="rr-flex-1" v-model="BUSTYPEID" :datas="param1"></Select></div></Cell>
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">委托编号</label><input type="text" class="rr-flex-1" v-model="WTCODE" /></div></Cell>
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">客户</label><input type="text" class="rr-flex-1" v-model="CUSTNAME" /></div></Cell>
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">联系人</label><input type="text" class="rr-flex-1" v-model="LINKER" /></div></Cell>
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">状态</label><Select class="rr-flex-1" v-model="STATE" :datas="param"></Select></div></Cell>
            <Cell width="6"><div style="width:100%;text-align:right;padding-right:10px"><Button class="ml5" @click="advQuery">查询</Button><Button class="ml5" @click="resetQuery">重置</Button></div></Cell>
          </Row>
        </div>
        <div class="m031-table-wrap" ref="tableWrap">
          <Table border :height="tableHeight" ref="selection" :datas="QRY" :checkbox="true" @select="onSelect" @trdblclick="clickRow">
            <TableItem title="#" prop="$serial" :width="40" fixed="left"></TableItem>
            <TableItem title="业务类型" prop="BUSTYPENAME" :width="80" fixed="left"></TableItem>
            <TableItem title="委托单号" prop="WTCODE" :width="150" fixed="left" treeOpener></TableItem>
            <TableItem title="客户名称" prop="CUSTNAME" :width="250" fixed="left"></TableItem>
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
          <template>
            <Tooltip theme="white" trigger="click" editable ref="checkTip" v-if="ISFEE">
              <Button class="ml5" color="primary">折扣</Button>
              <div slot="content">
                <div v-padding="15">输入折扣(范围:0.00~1.00)<input type="number" v-model="DISCOUNT" style="width: 200px;" /></div>
                <div v-padding="10" class="text-center"><Button color="primary" @click.native="batchDiscount">确定</Button></div>
              </div>
            </Tooltip>
            <Button color="primary" v-per="'LI_M031/A13'" icon="h-icon-task" v-if="ISFEE" @click="batchFee">收费</Button>
            <Poptip content="确定撤销收费？" v-per="'LI_M031/A14'" v-if="ISREFEE" @confirm="batchReFee"><Button color="red" icon="h-icon-close">撤销收费</Button></Poptip>
            <Button color="primary" class="f13" icon="rr-font rr-font-dayin" @click="aprint" v-if="ISSHOWAPRINT">打印</Button>
          </template>
        </table-tool-bar>
      </template>
      <!-- 汇总视图 -->
      <template v-if="activeTab === 'summary'">
        <div style="padding:10px 0px;">
          <Row :space="9">
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">项目名称</label><input type="text" class="rr-flex-1" v-model="sumPTEMPLATENAME" placeholder="项目名称" /></div></Cell>
            <Cell width="6"><div class="rr-flex-row"><label class="rr-justify" style="width:60px">受理部门</label><input type="text" class="rr-flex-1" v-model="sumADEPTNAME" placeholder="受理部门" /></div></Cell>
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
    <rs-modal ref="madd"><rsAdd :storeName="host.storeName" title="费用管理" :ID="CDID"></rsAdd></rs-modal>
    <rs-modal ref="mpdf"><rs-print-pdf :src="pdfSrc"></rs-print-pdf></rs-modal>
  </div>
</template>

<script>
import rsAdd from './add.vue'; // 表单 SFC，可继续用，或改成打开 /g/LI_M031/add
export default {
  name: 'r01-m031-main',
  components: { rsAdd },
  props: { host: { type: Object, required: true } },
  data() {
    return {
      CDID: '', showQuery: false, modal1: false, modal2: false, loading: true,
      datas: [{ title: '检验管理' }, { title: '费用管理' }],
      param1: [{ title: '委外', key: 1 }, { title: '自检', key: 2 }],
      param: [{ title: '待收费', key: 1 }, { title: '已折扣', key: 3 }, { title: '已收费', key: 2 }],
      checks: [], _isUpdatedSelection: false, _prevChecks: [],
      DISCOUNT: 1, pdfSrc: '', tableHeight: 300,
      activeTab: 'detail',
      tabDatas: [{ title: '费用明细', key: 'detail' }, { title: '按项目汇总', key: 'summary' }],
      sumPTEMPLATENAME: '', sumADEPTNAME: '',
      QRY: [], QQRY: {}, SUM: [],
      INPUT: '', WTCODE: '', CUSTNAME: '', LINKER: '', STATE: '', BUSTYPEID: '',
      PageIndex: 1, PageSize: 20, TotalCount: 0,
    };
  },
  computed: {
    ISFEE() { var cs = this.checks; return cs.length > 0 && cs.every(c => !c.CHARGEID); },
    ISREFEE() { var cs = this.checks; return cs.length > 0 && cs.every(c => !!c.CHARGEID); },
    ISDISCOUNT() { return this.checks.length > 0; },
    ISSHOWAPRINT() {
      if (this.checks.length === 0) return false;
      var first = this.checks[0];
      return this.checks.every(c => c.CUSTNAME === first.CUSTNAME);
    },
    sumTotalAMT() { return this.SUM.reduce((s, r) => s + (parseFloat(r.TOTALAMT) || 0), 0).toFixed(2); },
    sumTotalRAMT() { return this.SUM.reduce((s, r) => s + (parseFloat(r.TOTALRAMT) || 0), 0).toFixed(2); },
    sumTotalUncharged() { return this.SUM.reduce((s, r) => s + (parseFloat(r.UNCHARGEDAMT) || 0), 0).toFixed(2); },
    pageInfo: {
      get() { return { page: this.PageIndex, size: this.PageSize, total: this.TotalCount, pagerSize: 1 }; },
      set(v) { this.PageIndex = v.page; this.PageSize = v.size; },
    },
  },
  methods: {
    calcTableHeight() {
      this.$nextTick(() => {
        var wrap = this.$refs.tableWrap;
        if (!wrap) return;
        var headerEl = wrap.querySelector('.h-table-header');
        var headerH = headerEl ? headerEl.offsetHeight : 40;
        this.tableHeight = wrap.clientHeight - headerH;
      });
    },
    onSelect(selection) {
      if (this._isUpdatedSelection) return;
      var newSelection = [].concat(selection);
      var prev = this._prevChecks || [];
      this.QRY.forEach(parent => {
        if (!parent.children || !parent.children.length) return;
        var wasChecked = prev.indexOf(parent) >= 0;
        var isChecked = newSelection.indexOf(parent) >= 0;
        if (isChecked && !wasChecked) {
          parent.children.forEach(c => { if (newSelection.indexOf(c) < 0) newSelection.push(c); });
        } else if (!isChecked && wasChecked) {
          newSelection = newSelection.filter(item => parent.children.indexOf(item) < 0);
        }
      });
      this.QRY.forEach(parent => {
        if (!parent.children || !parent.children.length) return;
        var all = parent.children.every(c => newSelection.indexOf(c) >= 0);
        var none = !parent.children.some(c => newSelection.indexOf(c) >= 0);
        if (all && newSelection.indexOf(parent) < 0) newSelection.push(parent);
        else if (none && newSelection.indexOf(parent) >= 0) newSelection = newSelection.filter(i => i !== parent);
      });
      this._prevChecks = [].concat(newSelection);
      this.checks = newSelection;
      var tableSel = this.$refs.selection.getSelection();
      if (newSelection.length !== tableSel.length) {
        this._isUpdatedSelection = true;
        this.$refs.selection.setSelection(newSelection);
      }
      this.$nextTick(() => { this._isUpdatedSelection = false; this.setIndeterminate(); });
    },
    setIndeterminate() {
      this.$nextTick(() => {
        var table = this.$refs.selection; if (!table) return;
        var flatDatas = table.tableDatas || [];
        var mainBody = table.$el.querySelector('.h-table-body tbody'); if (!mainBody) return;
        var rows = mainBody.querySelectorAll('tr');
        rows.forEach((tr, idx) => {
          if (idx >= flatDatas.length) return;
          var data = flatDatas[idx];
          if (!data.children || !data.children.length) return;
          var selectedCount = data.children.filter(c => this.checks.indexOf(c) >= 0).length;
          var isIndeterminate = selectedCount > 0 && selectedCount < data.children.length;
          var checkboxEl = tr.querySelector('.h-checkbox');
          if (checkboxEl) {
            if (isIndeterminate) { checkboxEl.classList.add('h-checkbox-indeterminate'); checkboxEl.classList.remove('h-checkbox-checked'); }
            else { checkboxEl.classList.remove('h-checkbox-indeterminate'); }
          }
          var input = tr.querySelector('input.h-checkbox-native');
          if (input) input.indeterminate = isIndeterminate;
        });
      });
    },
    clickRow(row, $event) {
      if ($event.srcElement.querySelector('.h-table-tree-icon') || this._hasClass($event.srcElement, 'h-table-tree-icon')) return;
      if (row.ID) { this.CDID = row.ID; this.$refs.madd.show(); }
    },
    _hasClass(obj, cls) {
      if (!cls || !cls.replace(/\s/g, '').length) return false;
      return new RegExp(' ' + cls + '').test(' ' + obj.className);
    },
    async query(param) {
      if (param !== 1) this.PageIndex = 1;
      else param = {};
      var self = this;
      await this.host.$callAction({
        action: this.host.storeName + '/query',
        param: Object.assign({ INPUT: this.INPUT }, param),
        successCall: function() {
          self.QRY = self.host.storeObj.storeHelper.getTable('QRY').data || [];
          self.TotalCount = self.host.storeObj.storeHelper.getTable('QQRY').getValue('TotalCount') || 0;
          self.calcTableHeight();
        },
      });
    },
    async advQuery(param) {
      if (param !== 1) this.PageIndex = 1;
      else param = {};
      var self = this;
      await this.host.$callAction({
        action: this.host.storeName + '/advQuery',
        param: Object.assign({ INPUT: this.INPUT }, param),
        successCall: function() {
          self.QRY = self.host.storeObj.storeHelper.getTable('QRY').data || [];
          self.TotalCount = self.host.storeObj.storeHelper.getTable('QQRY').getValue('TotalCount') || 0;
          self.calcTableHeight();
        },
      });
    },
    resetQuery() {
      ['WTCODE','CUSTNAME','LINKER','STATE','BUSTYPEID'].forEach(k => { this[k] = ''; });
      this.advQuery();
    },
    changePage(pageInfo) {
      this.PageIndex = pageInfo.page;
      this.PageSize = pageInfo.size;
      if (this.showQuery) this.advQuery(1); else this.query(1);
    },
    batchDiscount() {
      var self = this;
      this.host.$callAction({
        action: this.host.storeName + '/batchDiscount',
        param: { items: this.checks, DISCOUNT: this.DISCOUNT },
        successText: '操作成功',
        successCall: function() { self.$refs.checkTip.hide(); self.query(1); },
      });
    },
    batchFee() { var self = this; this.host.$callAction({ action: this.host.storeName + '/batchFee', param: { items: this.checks }, successText: '操作成功', successCall: function() { self.query(1); } }); },
    batchReFee() { var self = this; this.host.$callAction({ action: this.host.storeName + '/batchReFee', param: { items: this.checks }, successText: '操作成功', successCall: function() { self.query(1); } }); },
    aprint() {
      var item = this.checks[0]; if (!item) { this.host.$error('请选择可打印受理单！'); return; }
      var self = this;
      this.host.$callAction({
        action: this.host.storeName + '/aprint',
        param: { items: this.checks.filter(r => r.ID) },
        successCall: function(ret) {
          self.pdfSrc = require('@/api/urls').getUrl('pdf') + ret;
          self.$refs.mpdf.show();
        },
      });
    },
    onTabChange() { if (this.activeTab === 'summary') this.querySum(); },
    querySum() {
      var self = this;
      this.host.$callAction({
        action: this.host.storeName + '/queryProjectSum',
        param: { PTEMPLATENAME: this.sumPTEMPLATENAME, ADEPTNAME: this.sumADEPTNAME },
        successCall: function() { self.SUM = self.host.storeObj.storeHelper.getTable('SUM').data || []; },
      });
    },
  },
  async mounted() {
    var self = this;
    await this.host.$store.dispatch('app/initScms', [this.host.storeObj.storeHelper.moudle.RESOURCENAME]);
    this.PageSize = 20;
    this.query();
    this.calcTableHeight();
    this._resizeHandler = function() { self.calcTableHeight(); };
    window.addEventListener('resize', this._resizeHandler);
  },
  beforeDestroy() { window.removeEventListener('resize', this._resizeHandler); },
};
</script>
<style lang="less" scoped>
@import '~@/theme/modern.less';
.m031-wrap { height: 100%; display: flex; flex-direction: column; }
/deep/ .h-panel-bar { background: #fff; border-bottom: 1px solid #f0f0f0; padding: 10px 20px; display: flex; align-items: center; }
.m031-body { flex: 1; display: flex; flex-direction: column; overflow: hidden; padding: 10px 20px; }
.m031-table-wrap { flex: 1; overflow: hidden; }
.m031-tabs { display: inline-block; margin-right: 12px; }
</style>
```

---

## 三、main 页扩展 JS（`@/modules/LI_M031/main.js`）

`SFCMODULEPATH` 已指向整页 SFC，扩展 JS 仅承担 generic-module 框架钩子（无需重复实现按钮显隐）：

```javascript
/**
 * LI_M031 列表页扩展（SFC 整页方案）
 * 因 main.vue 已是 SFC 整页，本文件保留空挂载，仅用于：
 *   1) 给 tss_module_button 的 SHOWCOND 求值兜底
 *   2) 后续若把按钮外置到 m18 配置时提供 computed
 */
export default {
  computed: {
    ISFEE() { return false; },
    ISREFEE() { return false; },
    ISDISCOUNT() { return false; },
    ISSHOWAPRINT() { return false; },
  },
};
```

---

## 四、add 页 SFC 扩展 JS（`@/modules/LI_M031/form.js`）

表单页完全配置化，仅保留 AMT 联动 + 收费时间写入：

```javascript
/**
 * LI_M031 收费表单扩展
 * this 上下文 (generic-form):
 *   this.ID / this.STATE / this.CAMT / this.CNT / this.BAMT / this.OAMT / this.AMT / this.DISCOUNT / this.CHARGEID
 *   this.$MAIN (DataTable) / this.save() / this.closePage()
 */
export default {
  computed: {
    ISSHOWSAVE() { return !this.CHARGEID; },
  },
  watch: {
    CNT()      { this._calcAMT(); },
    CAMT()     { this._calcAMT(); },
    DISCOUNT() { this._calcAMT(); },
    OAMT()     { this._calcAMT(); },
    BAMT()     { this._calcAMT(); },
  },
  methods: {
    _calcAMT() {
      var v = (+this.CNT || 0) * (+this.CAMT || 0) * (+this.DISCOUNT || 1)
            + (+this.OAMT || 0) + (+this.BAMT || 0);
      this.$MAIN.setValue('AMT', v.toFixed(2));
    },
    // 覆盖 A04 save：把收费人/收费时间写入 MAIN
    async mySave() {
      var userInfo = this.$store.state.user.userInfo;
      this.$MAIN.setValue('CHARGEID', userInfo.ID);
      this.$MAIN.setValue('CHARGER', userInfo.NICKNAME);
      this.$MAIN.setValue('CHARGETIME', this._now());
      await this.save();
    },
    _now() {
      var d = new Date();
      var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
      return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
           + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    },
  },
};
```

---

## 五、查询字段配置（m18 uiSetFull）

### 5.1 QQRY 查询条件字段（resuipc QUERYSORT>0）

| FIELDNAME | LABELNAME | EDITTYPE | QUERYMODE | QUERYSORT | SELECTDATA |
|-----------|-----------|----------|-----------|-----------|------------|
| INPUT | 关键字 | text | like | 10 | - |
| WTCODE | 委托编号 | text | like | 20 | - |
| CUSTNAME | 客户名称 | text | like | 30 | - |
| LINKER | 联系人 | text | like | 40 | - |
| STATE | 状态 | select | eq | 50 | `D03_STATE_FEE`（字典：1待收费/3已折扣/2已收费） |
| BUSTYPEID | 业务类型 | select | eq | 60 | `1:委外;2:自检`（inline） |
| SDATE | 开始日期 | date | range | 70 | - |
| EDATE | 结束日期 | date | range | 80 | - |

### 5.2 QRY 列表字段（resuipc LISTSORT>0）

按原 main.vue 列顺序配置 LISTSORT=10/20/30...，fixed 列在 LISTCONFIG 标记 `fixed:'left'`。

### 5.3 SUM 汇总字段（resuipc LISTSORT>0）

PTEMPLATENAME/ADEPTNAME/TOTALCNT/TOTALCAMT/TOTALOAMT/TOTALBAMT/TOTALAMT/TOTALRAMT/CHARGEDCNT/UNCHARGEDAMT。

---

## 六、Store 扩展（`@/modules/LI_M031/store.js`）

```javascript
/**
 * LI_M031 Store 扩展
 * INIT/ADD/save/delete 走 Store03 默认，仅保留：
 *   1) 双查询合并（A01+A09，前端 children 组装）
 *   2) 项目汇总查询（A20）
 *   3) 批量折扣/收费/撤销收费/打印
 */
export default {
  actions: {
    // 模糊查询 + 明细子表合并（树形展开）
    async query({ state, dispatch }, { INPUT } = {}) {
      var sh = this.storeHelper; // generic-store 注入
      var qqry = sh.getTable('QQRY');
      var params = { PageSize: qqry.getValue('PageSize') || 20, PageIndex: qqry.getValue('PageIndex') || 1, FilterParams: { INPUT: INPUT } };
      qqry.getFields().forEach(function(f) {
        if (['PageSize','PageIndex','TotalCount','SumInfo'].indexOf(f) >= 0) params[f] = qqry.getValue(f);
        else params.FilterParams[f] = qqry.getValue(f);
      });
      params.FilterParams.STATE = '1';
      var ret = await dispatch('call', { APICODE: 'A01', params: params });
      var ret2 = await dispatch('call', {
        APICODE: 'A09',
        params: { PageSize: 1, PageIndex: 1, FilterParams: { INPUT: INPUT, rows: ret.Items } },
      });
      ret.Items.forEach(function(r) {
        r.children = ret2.Items.filter(function(q) {
          return q.WTCODE === r.WTCODE && r.CUSTID === q.CUSTID && r.BILLDATE === q.BILLDATE && r.BUSTYPEID === q.BUSTYPEID;
        });
      });
      qqry.setValue('TotalCount', ret.TotalCount);
      return ret.Items;
    },
    // 高级查询（同 query，不带 INPUT，按 QQRY 各字段精确过滤）
    async advQuery({ dispatch }, payload) {
      var sh = this.storeHelper;
      var qqry = sh.getTable('QQRY');
      var params = { PageSize: qqry.getValue('PageSize') || 20, PageIndex: qqry.getValue('PageIndex') || 1, FilterParams: {} };
      qqry.getFields().forEach(function(f) {
        if (['PageSize','PageIndex','TotalCount','SumInfo'].indexOf(f) >= 0) params[f] = qqry.getValue(f);
        else params.FilterParams[f] = qqry.getValue(f);
      });
      var ret = await dispatch('call', { APICODE: 'A01', params: params });
      var ret2 = await dispatch('call', {
        APICODE: 'A09', params: { PageSize: 1, PageIndex: 1, FilterParams: { rows: ret.Items } },
      });
      ret.Items.forEach(function(r) {
        r.children = ret2.Items.filter(function(q) {
          return q.WTCODE === r.WTCODE && r.CUSTID === q.CUSTID && r.BILLDATE === q.BILLDATE && r.BUSTYPEID === q.BUSTYPEID;
        });
      });
      qqry.setValue('TotalCount', ret.TotalCount);
      return ret.Items;
    },
    async queryProjectSum({ dispatch }, { PTEMPLATENAME, ADEPTNAME } = {}) {
      var ret = await dispatch('call', {
        APICODE: 'A20',
        params: { PageSize: 200, PageIndex: 1, FilterParams: { PTEMPLATENAME: PTEMPLATENAME, ADEPTNAME: ADEPTNAME } },
      });
      return ret.Items || [];
    },
    async batchDiscount({ dispatch }, { items, DISCOUNT }) {
      return await dispatch('batch', { APICODE: 'A12', items: items, updateFields: ['AMT','DISCOUNT'], params: { DISCOUNT: DISCOUNT } });
    },
    async batchFee({ dispatch }, { items }) {
      return await dispatch('batch', { APICODE: 'A13', items: items, updateFields: ['RAMT','CHARGEID','CHARGER','CHARGETIME'], params: {} });
    },
    async batchReFee({ dispatch }, { items }) {
      return await dispatch('batch', { APICODE: 'A14', items: items, updateFields: ['RAMT','CHARGEID','CHARGER','CHARGETIME'], params: {} });
    },
    async aprint({ dispatch }, { items }) {
      return await dispatch('batch', { APICODE: 'A16', items: items });
    },
  },
};
```

---

## 七、迁移对照表

| 原 r01/m031 文件 | 迁移后 | 说明 |
|-----------------|--------|------|
| `router.js` | 不需要 | generic-module 路由自动注册 `/g/LI_M031/main`、`/g/LI_M031/add` |
| `store.js`（含 SelStore/getStore） | `@/modules/LI_M031/store.js` | 去掉 createStore/SelStore，仅保留自定义 action（query/advQuery/queryProjectSum/batch×4/aprint） |
| `views/main.vue` | `@/modules/LI_M031/main.vue`（SFC 整页资产） | 保留树形/级联/双Tab/统计行，host 注入替代 mapDateTable |
| `views/add.vue` | m18 配置 + `@/modules/LI_M031/form.js` | rs-form-edit 配置化，仅留 AMT 联动 + 收费时间写入 |
| `mapDateTable('QQRY',[...])` | SFC 内 host.$callAction + storeObj.storeHelper.getTable | QQRY 仍走 generic-store DataTable |
| `mapDateTable('MAIN',[...])` | generic-form 自动映射 | this.CAMT/this.AMT 直接可用 |
| `mapDateTable('SUM',[...])` | SFC 内本地 data SUM + queryProjectSum 填充 | SUM 路径不在 moudlepath，改本地数组 |
| `ISFEE/ISREFEE/ISDISCOUNT/ISSHOWAPRINT` | SFC computed | 保留原逻辑（基于 this.checks） |
| 树形级联选中 onSelect/setIndeterminate | SFC methods | 完整保留 |
| A09 明细查询合并 | store.query/advQuery 内 dispatch('call', {APICODE:'A09'}) | 双 await 合并 children |
| A20 项目汇总 | store.queryProjectSum | 同原逻辑 |
| 批量 A12/A13/A14/A16 | store.batch×4 + aprint | dispatch('batch', ...) 走 Store03 |
| 日期格式化 formatDateValue | 删除 | 后端 F02 过滤器 str_to_date 已兼容；如确实需要，移入 store action |
| `_resizeHandler` | SFC mounted/beforeDestroy | 保留 |
| debugger/console.log | 删除 | 旧调试残留 |

---

## 八、迁移后目录结构

```
src/modules/LI_M031/              # SFC 扩展资产（tss_code_asset）
  main.vue                        # 列表整页 SFC（双 Tab + 树形 + 级联 + 统计行）
  main.js                         # 列表扩展占位（按钮显隐兜底）
  form.js                         # 表单扩展（AMT 联动 + mySave 收费时间）
  store.js                        # Store 扩展（query/advQuery/queryProjectSum/batch×4/aprint）
```

原 `src/pages/r01/m031/` 目录可删除，菜单 `tss_func.OUTERURL = /g/LI_M031/main` 自动注册。

---

## 九、关键风险与对策

| 风险 | 对策 |
|------|------|
| SFC 整页内部使用 `this.$refs.selection.setSelection` 等 HeyUI 私有 API | 保留原写法；SFC 运行时与原 .vue 一致 |
| A01 + A09 双查询合并依赖前端 children 组装 | 保留在 store.query action 内完成，前端只读 QRY.data |
| 树形 `treeOpener` + 25 列冻结 + 自定义 tableHeight | SFC 整页方案完全保留样式与高度计算逻辑 |
| 半选中态依赖 DOM 操作 `.h-checkbox-indeterminate` | 保留 setIndeterminate 方法，已验证在 SFC 模式下可用 |
| SUM 不在 moudlepath | 改为本组件 data 数组，由 queryProjectSum action 填充，不走 DataTable |
| 菜单切换 keep-alive 命名 | 原 `name: 'r01-m031-main'` 必须改为 `r01-m031-main`（与路由 name 一致）以保证 Tab 缓存 |
