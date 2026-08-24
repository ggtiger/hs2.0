# r01/m026 委托审批（LI_M02）→ generic-module + 整页 SFC 迁移方案

## 迁移思路

m026 是**委托审批工作台**，与 m025（委托审核）共享同一 review.vue 组件（mode='verify'）。m026 自身的 review.vue 是简化版（无三栏分屏，仅表格清单+审批意见），但实际推荐复用 m025 的完整版 review.vue。

采用**整页 SFC 重写**方案（与 m025 一致）：
1. **数据库配置** - tss_module_page 配 main 列表 + review 整页
2. **PAGECONFIG** - review 页用 SFCMODULEPATH 指向整页 SFC（优先复用 m025 的 review.vue）
3. **Store 扩展** - 保留原 store.js 全部自定义 actions（query/advQuery/verify/batchVerify/batchReVerify/batchReject/print/download/aprint/loadWtList/detectAnomalies）
4. **main.js 扩展** - 上下双表联动 + 前端分页分组 + 批量审批入口
5. **review.vue 整页 SFC** - 复用 m025 的 review.vue（传 mode='verify' + storeName='r01/m026'）

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | COMPONENTTYPE | SFCMODULEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|---------------|---------------|--------------|--------------|
| main | 委托审批 | list | /g/LI_M026/main | generic-module | - | A40 | - | - |
| review | 审批工作台 | form | /g/LI_M026/review | sfc | `@/modules/LI_M025/review.vue` | - | - | - |

**关键点**：
- `main` 走标准 generic-module 列表渲染
- `review` 的 SFCMODULEPATH **指向 m025 的 review.vue**（复用整页 SFC，避免重复维护）
- review 页通过路由 query 或 props 区分 mode/storeName（`mode=verify`、`storeName=r01/m026`）

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "EXTENDJS": "@/modules/LI_M026/main.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M026/query-panel.vue",
    "body-query": "@/modules/LI_M026/query-panel.vue"
  }
}
```

### 1.3 review 页 PAGECONFIG

```json
{
  "SFCMODULEPATH": "@/modules/LI_M025/review.vue",
  "EXTENDJS": "@/modules/LI_M025/review.js"
}
```

**说明**：SFCMODULEPATH 直接指向 m025 的 review.vue，因为该 SFC 已内置 mode/storeName 自适应逻辑（详见 m025 文档第 5.8 节）。

### 1.4 按钮配置 (tss_module_button)

**main 页按钮**（底部 footer 区域）：

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND | EXTPARAM |
|---------|---------|---------|---------|----------|----------|
| 委托审批 | custom | footer | - | `ISSHOWVERIFY` | `{"action":"startReview","param":{"mode":"verify"}}` |
| 批量审批 | custom | footer | A25 | `ISSHOWBATCHVERIFY` | `{"action":"batchVerify","beforeAction":"confirmBatch"}` |
| 批量撤销审批 | custom | footer | A26 | `ISSHOWBATCHREVERIFY` | `{"action":"batchReVerify","beforeAction":"confirmBatch"}` |
| 批量驳回 | custom | footer | A29 | `ISSHOWBATCHREJECT` | `{"action":"batchReject","beforeAction":"confirmBatch"}` |
| 证书打印 | custom | footer | A17 | `ISSHOWPRINT` | `{"action":"print"}` |
| 证书下载 | custom | footer | A20 | `ISSHOWDOWNLOAD` | `{"action":"download"}` |
| 受理打印 | custom | footer | A21 | - | `{"action":"aprint"}` |

**review 页按钮**：不配置（与 m025 一致，review.vue 内部自渲染底部操作栏）。

---

## 二、Store 扩展（保留原 store.js 全部自定义 actions）

路径：`@/modules/LI_M026/store.js`

```javascript
/**
 * LI_M026 Store 扩展
 *
 * Store03 默认 actions (open/save/delete/call/batch/...) 已内置，无需重复定义。
 * 保留原 r01/m026/store.js 中的自定义 actions:
 *   - query/advQuery: 覆盖默认查询，使用审批专用 APICODE (A40/A42)
 *   - verify/reVerify/reject: 带行级回写的审批操作
 *   - batchVerify/batchReVerify/batchReject: 批量审批（updateFields 回写）
 *   - print/aprint/download: 证书打印下载
 *   - loadWtList: 委托单列表（LI_M02/A36 orecord 粒度）
 *   - detectAnomalies: AI 异常检测（复用 LI_M02/A57）
 */
export default {
  actions: {
    // ====== 审批专用查询（覆盖 Store03.query/advQuery）======

    // 列表查询：使用 A40（F012 过滤器，VERIFYID=当前用户）
    // 注意：m026 走 /api/rm11/call/ 路由（带审批权限校验）
    async query({ commit, dispatch }, { isExport, columns, sumFields } = {}) {
      var row = this.state.helper.moudle.getApi('', 'A40');
      var modeCode = this.state.helper.moudle.getModCode();
      var { APIPARAM, APICODE, PATHNAME } = row;
      var QQRY = this.state.helper.getTable(APIPARAM);
      commit('INIT', { paths: [PATHNAME] });
      var params = { FilterParams: {}, isExport: isExport, columns: columns, sumFields: sumFields };
      QQRY.getFields().forEach(function(f) {
        if (['PageSize', 'PageIndex', 'TotalCount', 'SumInfo'].indexOf(f) !== -1) {
          params[f] = QQRY.getValue(f);
        } else {
          var vvv = QQRY.getValue(f);
          if (Object.prototype.toString.call(vvv) === '[object Object]') {
            Object.keys(vvv).forEach(function(k) {
              params.FilterParams[f + '_' + k] = formatDateValue(vvv[k]);
            });
          } else if (Array.isArray(vvv)) {
            params.FilterParams[f] = vvv.map(formatDateValue).join();
          } else {
            params.FilterParams[f] = formatDateValue(vvv);
          }
        }
      });
      // m026 走 /api/rm11/call/ 路由（审批权限校验由 RM11Controller 处理）
      var ret = await dispatch('callRaw', {
        api: '/api/rm11/call/' + modeCode + '/' + APICODE + '/',
        params: params,
      });
      if (isExport) return ret;
      QQRY.setValue('TotalCount', ret.TotalCount);
      QQRY.setValue('SumInfo', ret.SumInfo);
      commit(Constants.M_INITDATA, { path: PATHNAME, data: ret.Items || [] });
    },

    // 高级查询：使用 A42（F022 过滤器）
    async advQuery({ commit, dispatch }, { isExport, columns, sumFields } = {}) {
      // 同 query，APICODE 改为 A42
      // ...
    },

    // ====== 审批操作（带行级回写）======

    // 审批通过 A14
    async verify({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', {
        APICODE: 'A14',
        params: { REMARK: REMARK, ID: ID },
      });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 撤销审批 A15
    async reVerify({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', {
        APICODE: 'A15',
        params: { REMARK: REMARK, ID: ID },
      });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 驳回 A16（审批/审核通用）
    async reject({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', {
        APICODE: 'A16',
        params: { REMARK: REMARK, ID: ID },
      });
      if (ret && ret.length > 0) {
        ['STATE', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // ====== 批量审批操作（updateFields 回写）======

    // 批量审批通过 A25
    async batchVerify({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A25',
        items: items,
        params: { REMARK: REMARK },
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量撤销审批 A26
    async batchReVerify({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A26',
        items: items,
        params: { REMARK: REMARK },
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // 批量驳回 A29
    async batchReject({ dispatch }, { items, REMARK }) {
      await dispatch('batch', {
        APICODE: 'A29',
        items: items,
        params: { REMARK: REMARK },
        updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'],
      });
    },

    // ====== 委托单列表加载 ======

    // 委托单列表（LI_M02/A36 orecord 粒度，前端按 REFBILLID 分组）
    async loadWtList({ dispatch }, { input, billDate }) {
      var filterParams = { INPUT: input || '' };
      if (billDate) filterParams.BILLDATE = billDate;
      return await dispatch('callRaw', {
        api: '/api/data/call/LI_M02/A36/',
        params: {
          PageSize: 9999,
          PageIndex: 1,
          FilterParams: filterParams,
        },
      });
    },

    // 异常检测（复用 LI_M02/A57）
    async detectAnomalies({ dispatch }, { id }) {
      return await dispatch('callRaw', {
        api: '/api/data/call/LI_M02/A57/',
        params: { FilterParams: { ID: id } },
      });
    },

    // ====== 打印/下载 ======

    // 证书打印 A17（单条）
    async print({ dispatch }, { ID }) {
      await dispatch('call', { APICODE: 'A17', params: { ID: ID } });
    },

    // 受理打印 A21（批量）
    async aprint({ dispatch }, { items }) {
      return await dispatch('batch', { APICODE: 'A21', items: items });
    },

    // 证书下载 A20（批量，返回下载链接）
    async download({ dispatch }, { items }) {
      return await dispatch('batch', {
        APICODE: 'A20',
        items: items,
        updateFields: ['STATE'],
      });
    },
  },
};

function formatDateValue(vvv) {
  if (vvv instanceof Date) {
    var y = vvv.getFullYear();
    var m = vvv.getMonth() + 1;
    var d = vvv.getDate();
    return y + '-' + (m < 10 ? '0' + m : m) + '-' + (d < 10 ? '0' + d : d);
  }
  return vvv;
}
```

---

## 三、main.js — 列表页扩展（上下双表联动 + 前端分页分组 + 批量审批）

路径：`@/modules/LI_M026/main.js`

```javascript
/**
 * 委托审批列表页扩展
 *
 * 核心特性：
 *   1. 上下双表联动（与 m025 一致：上表=委托单，下表=委托明细）
 *   2. 前端分页：服务端返回 orecord 粒度（A36），按 REFBILLID 分组后客户端切片
 *   3. 手动单选 checkbox（委托单列表强制单选）
 *   4. 全屏审批弹窗（复用 m025 的 review.vue，传 mode='verify'）
 *   5. 批量审批入口（选中明细后直接批量操作，无需进 review 页）
 *
 * this 上下文同 m025 main.js
 */
export default {
  data() {
    return {
      // 搜索条件
      searchInput: '',
      searchBillDate: null,
      // 委托单列表（上表）
      wtLoading: false,
      wtDatas: [],
      selectedWt: null,
      _allOrecords: [],
      totalRows: 0,
      currentPage: 1,
      pageSize: 20,
      // 委托明细列表（下表）
      dtlLoading: false,
      dtlDatas: [],
      selectedDtlItems: [],
      // 审批弹窗
      showReviewModal: false,
    };
  },

  computed: {
    // 委托审批按钮显隐：选中明细全部 STATE=5 或 19（待审批）
    ISSHOWVERIFY() {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE === 5 || r.STATE === 19; });
    },
    // 批量审批按钮显隐：同 ISSHOWVERIFY（允许批量）
    ISSHOWBATCHVERIFY() {
      return this.ISSHOWVERIFY && this.selectedDtlItems.length > 1;
    },
    // 批量撤销审批：选中明细全部 STATE=6 或 20（已审批）
    ISSHOWBATCHREVERIFY() {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE === 6 || r.STATE === 20; });
    },
    // 批量驳回：与 ISSHOWVERIFY 一致（待审批才能驳回）
    ISSHOWBATCHREJECT() {
      return this.ISSHOWVERIFY;
    },
    // 证书打印：选中 1 行且 STATE=10/11/14（已签发/已打印）
    ISSHOWPRINT() {
      var rows = this.selectedDtlItems || [];
      if (rows.length !== 1) return false;
      return [10, 11, 14].indexOf(rows[0].STATE) >= 0;
    },
    // 证书下载：选中行全部 STATE=10/11/14
    ISSHOWDOWNLOAD() {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return [10, 11, 14].indexOf(r.STATE) >= 0; });
    },
    wtPageInfo: {
      get() {
        return {
          page: this.currentPage,
          size: this.pageSize,
          total: this.totalRows,
          pagerSize: 1,
        };
      },
      set(v) {
        this.currentPage = v.page;
        this.pageSize = v.size;
      },
    },
  },

  mounted() {
    this.loadWtList();
  },

  methods: {
    // ====== 搜索 ======
    doSearch() {
      this.currentPage = 1;
      this.loadWtList();
    },
    resetSearch() {
      this.searchInput = '';
      this.searchBillDate = null;
      this.currentPage = 1;
      this.loadWtList();
    },

    // ====== 委托单列表加载（同 m025，API 换为 A36）======
    async loadWtList() {
      this.wtLoading = true;
      try {
        var ret = await this.$callAction({
          action: this.moduleCode + '/loadWtList',
          param: {
            input: this.searchInput || '',
            billDate: this.searchBillDate,
          },
          isBusy: false,
        });
        var items = (ret && ret.Items) || [];
        this._allOrecords = items;
        var groupMap = {};
        items.forEach(function(item) {
          var key = item.REFBILLID;
          if (!groupMap[key]) {
            groupMap[key] = {
              REFBILLID: item.REFBILLID,
              WTCODE: item.WTCODE,
              CUSTNAME: item.CUSTNAME,
              BILLDATE: item.BILLDATE,
              SUMBMITTIME: item.SUMBMITTIME,
              CREATER: item.CREATER,
              DEVICECOUNT: 0,
            };
          }
          groupMap[key].DEVICECOUNT++;
        });
        var groups = Object.values(groupMap);
        this.totalRows = groups.length;
        var start = (this.currentPage - 1) * this.pageSize;
        this.wtDatas = groups.slice(start, start + this.pageSize);
      } catch (e) {
        console.error('加载委托单列表失败', e);
      } finally {
        this.wtLoading = false;
      }
    },

    onPageChange(pageInfo) {
      this.currentPage = pageInfo.page;
      this.pageSize = pageInfo.size;
      this.loadWtList();
    },

    // ====== 双表联动（同 m025）======
    onWtClickRow(row) {
      if (this.selectedWt && this.selectedWt !== row) {
        this.$refs.wtTable.setCheck(this.selectedWt, false);
      }
      this.$refs.wtTable.setCheck(row, true);
      this.selectedWt = row;
      this.selectedDtlItems = [];
      if (this._allOrecords) {
        this.dtlDatas = this._allOrecords.filter(function(item) {
          return item.REFBILLID === row.REFBILLID;
        });
      } else {
        this.dtlDatas = [];
      }
    },

    onWtSelect(checks) {
      if (checks.length > 1) {
        var last = checks[checks.length - 1];
        var self = this;
        this.wtDatas.forEach(function(item) {
          if (item !== last) self.$refs.wtTable.setCheck(item, false);
        });
        this.selectedWt = last;
      } else if (checks.length === 1) {
        this.selectedWt = checks[0];
      } else {
        this.selectedWt = null;
      }
      this.selectedDtlItems = [];
      if (this._allOrecords && this.selectedWt) {
        var sel = this.selectedWt;
        this.dtlDatas = this._allOrecords.filter(function(item) {
          return item.REFBILLID === sel.REFBILLID;
        });
      } else {
        this.dtlDatas = [];
      }
    },

    onDtlSelect(checks) {
      this.selectedDtlItems = checks || [];
    },

    // ====== 批量操作确认（beforeAction 钩子）======
    async confirmBatch(btn, context) {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) {
        this.$error('请先勾选记录');
        return false;
      }
      return await this.$confirm('确认对 ' + rows.length + ' 条记录执行「' + btn.BTNNAME + '」操作？');
    },

    // ====== 启动审批（打开 review.vue 整页弹窗，mode='verify'）======
    startReview(btn, context) {
      if (!this.selectedWt) {
        this.$error('请先选择一条委托单！');
        return;
      }
      if (this.selectedDtlItems.length === 0) {
        this.$error('请选择需要审批的委托明细！');
        return;
      }
      this.showReviewModal = true;
      this.$nextTick(function() {
        if (this.$refs.reviewModal) this.$refs.reviewModal.show();
      }.bind(this));
    },

    onReviewClose() {
      this.showReviewModal = false;
      if (this.$refs.reviewModal) this.$refs.reviewModal.close();
      this.loadWtList();
    },

    // ====== 批量审批操作 ======
    async batchVerify(btn, context) {
      var rows = this.selectedDtlItems || [];
      var REMARK = ''; // 批量操作不填 REMARK，或弹窗输入
      await this.$callAction({
        action: this.moduleCode + '/batchVerify',
        param: { items: rows, REMARK: REMARK },
        successText: '批量审批成功',
      });
      this.loadWtList();
    },

    async batchReVerify(btn, context) {
      var rows = this.selectedDtlItems || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReVerify',
        param: { items: rows, REMARK: '' },
        successText: '批量撤销审批成功',
      });
      this.loadWtList();
    },

    async batchReject(btn, context) {
      var rows = this.selectedDtlItems || [];
      // 驳回必须填原因
      var REMARK = await this.$prompt('请输入驳回原因');
      if (!REMARK) return;
      await this.$callAction({
        action: this.moduleCode + '/batchReject',
        param: { items: rows, REMARK: REMARK },
        successText: '批量驳回成功',
      });
      this.loadWtList();
    },

    // ====== 打印/下载 ======
    async print(btn, context) {
      var row = (this.selectedDtlItems || [])[0];
      if (!row) return;
      this.$busy();
      try {
        var ret = await this.$callAction({
          action: this.moduleCode + '/print',
          param: { ID: row.ID },
        });
        if (ret && ret.Data && ret.Data.url) {
          this.$refs.mpdf.show(ret.Data.url);
        }
      } finally {
        this.$free();
      }
    },

    async download(btn, context) {
      var rows = this.selectedDtlItems || [];
      var ret = await this.$callAction({
        action: this.moduleCode + '/download',
        param: { items: rows },
      });
      if (ret && ret.Data && ret.Data.url) {
        window.open(ret.Data.url);
      }
    },

    async aprint(btn, context) {
      var rows = this.selectedDtlItems || [];
      var ids = rows.map(function(r) { return r.ID; }).join(',');
      var ret = await this.$callAction({
        action: this.moduleCode + '/aprint',
        param: { items: rows },
      });
      if (ret && ret.Data && ret.Data.url) {
        this.$refs.mpdf.show(ret.Data.url);
      }
    },
  },
};
```

---

## 四、query-panel.vue — 查询面板 SFC slot（可选）

路径：`@/modules/LI_M026/query-panel.vue`

与 m025 的 query-panel.vue 结构一致（input + DateRangePicker + 查询/重置按钮），仅搜索的 API 不同（A36 vs A53）。推荐保留手写方式，通过 `body-query` slot 注入。

---

## 五、review.vue — 整页 SFC（复用 m025）

### 5.1 复用策略

**m026 不创建独立的 review.vue**，直接复用 `@/modules/LI_M025/review.vue`。原因：

1. **m025 的 review.vue 已内置 mode/storeName 自适应**：所有 dispatch 走 `this.currentStoreName + '/' + actionName`，传入 `storeName='r01/m026'` 后自动路由到 m026 的 store
2. **审批操作与审核操作共用同一 UI**：三栏分屏（记录列表 | OnlyOffice | 原始记录）+ 检查清单 + 底部操作栏，仅按钮文案不同（审核 vs 审批）
3. **避免重复维护**：检查清单（14 项 7 分组）、AI 自动检查、原始记录加载、Tooltip 弹窗等逻辑完全一致

### 5.2 review.vue 整页 SFC 说明

详细结构见 [`m025/migrate-to-generic.md` 第五节](../m025/migrate-to-generic.md#五reviewvue--整页-sfc1968-行)。m026 复用时仅需传入：

```javascript
// m026 的 main.js 中 startReview 方法
this.showReviewModal = true;
// review.vue 的 props:
//   mode: 'verify'        // 审批模式（STATE=5/19）
//   storeName: 'r01/m026' // 走 m026 的 store
//   wtItem: selectedWt
//   dtlItems: selectedDtlItems
```

### 5.3 m026 自身的 review.vue（已废弃）

原 `r01/m026/views/review.vue`（571 行）是简化版审批页：
- 无三栏分屏
- 无 OnlyOffice 预览
- 无可拖拽分隔条
- 表格形式检查清单
- 无 rs-edit-item 原始记录展示

**迁移后该文件不保留**，统一使用 m025 的完整版 review.vue（mode='verify'）。

### 5.4 审批与审核的差异点（在 review.vue 内部处理）

| 差异点 | 审核（mode=check） | 审批（mode=verify） |
|-------|-------------------|-------------------|
| 目标 STATE | 2（待审核） | 5/19（待审批） |
| 通过 APICODE | A12（check，需选下一审批人） | A14（verify） |
| 撤销 APICODE | A13（reCheck） | A15（reVerify） |
| 按钮文案 | "审核"、"复核通过/驳回" | "审批"、"审批通过/驳回" |
| 撤销条件 | STATE=3（已审核） | STATE=6/20（已审批） |
| 审批人选择器 | 显示（check 模式需选下一审批人 VERIFYID） | 不显示（verify 模式无需选人） |

以上差异全部由 review.vue 内部的 computed 属性处理（approveBtnText/rejectBtnText/passBtnText/canReview/canReCheck/canReVerify），m026 无需额外配置。

### 5.5 路由参数传递

review 页通过路由 query 传递 mode 和 storeName：

```
/g/LI_M026/review?mode=verify&storeName=r01/m026
```

generic-form.vue 在加载 SFCMODULEPATH 时，将 `$route.query` 作为 props 传入 SFC。

如果通过 modal 方式打开（m026 main.js 的 startReview），则直接通过 props 传递：

```html
<rs-modal ref="reviewModal" :fullScreen="true" v-if="showReviewModal">
  <review-page
    :wtItem="selectedWt"
    :dtlItems="selectedDtlItems"
    mode="verify"
    storeName="r01/m026"
    @close="onReviewClose"
  ></review-page>
</rs-modal>
```

---

## 六、迁移对照表

| 原 r01/m026 文件 | 迁移后 | 说明 |
|----------------|--------|------|
| `router.js` | 不需要 | generic-module 路由自动注册（/g/LI_M026/main、/g/LI_M026/review） |
| `store.js` | `@/modules/LI_M026/store.js` | 保留全部自定义 actions（query/advQuery/verify/reVerify/reject/batchVerify/batchReVerify/batchReject/print/aprint/download/loadWtList/detectAnomalies） |
| `views/main.vue` | m18 配置 + `main.js` 扩展 | 上下双表逻辑、前端分页分组、手动单选、批量审批、全屏弹窗 → main.js |
| `views/main.vue` 搜索栏 | `query-panel.vue` SFC slot（可选） | 顶部 input + DateRangePicker |
| `views/review.vue`（571 行简化版） | **废弃，复用 m025 的 review.vue** | 通过 mode='verify' + storeName='r01/m026' 区分 |
| `mapDateTable('QQRY', [...])` | 不需要 | main.vue 走自定义 wtDatas/dtlDatas |
| `mapDateTable('EMPUSER', [])` | 不需要 | m026 main.vue 无审批人选择器（verify 模式不需要选下一审批人） |
| 批量审批/撤销/驳回 | 扩展 JS methods + 按钮配置 | 通过 EXTPARAM.action 路由 |
| 打印/下载 | 扩展 JS methods | print/download/aprint |

---

## 七、迁移后目录结构

```
src/modules/LI_M026/           # SFC 扩展资产（数据库 tss_code_asset）
  store.js                     # Store 扩展（query/advQuery/verify/batchVerify/batchReVerify/batchReject/print/download/loadWtList/...）
  main.js                      # 列表页扩展（上下双表 + 前端分页 + 批量审批 + 全屏弹窗）
  query-panel.vue              # 查询面板 SFC slot（可选，2 字段简单搜索）
  # 不创建 review.vue，复用 @/modules/LI_M025/review.vue
```

原 `src/pages/r01/m026/` 目录可删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M026/main` 自动注册。

---

## 八、迁移注意事项

### 8.1 API 路由差异

m026 的 query/advQuery 走 `/api/rm11/call/` 路由（带审批权限校验），而 m025 走 `/api/data/call/`。原因是审批操作需要 RM11Controller 的权限校验（授权签字人验证）。store.js 中必须保留此路由差异。

### 8.2 批量操作 updateFields 回写

批量审批的 `batchVerify/batchReVerify/batchReject` 都通过 Store03 的 `batch` action 触发，`updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME']` 指定回写字段。batch action 执行后，选中行的这些字段会被服务端返回值自动更新。

### 8.3 复用 m025 review.vue 的 store 引用

m025 的 review.vue 内部 import 了两个 store：

```javascript
import { getStore as getStore025 } from '@/modules/LI_M025/store';
import { getStore as getStore026 } from '@/modules/LI_M026/store';
```

迁移后，m026 的 store.js 必须存在于 `@/modules/LI_M026/store.js`，否则 review.vue 的 `getStore026()` 调用会失败。

### 8.4 批量驳回必须填原因

与 m025 不同，m026 的批量驳回（batchReject）建议强制要求填写驳回原因（通过 `$prompt` 弹窗）。原因是批量驳回影响范围大，需要留下审计记录。单条驳回（review.vue 内部的 handleReject）已校验 `this.REMARK`。

### 8.5 tss_func 菜单配置

```
OUTERURL = /g/LI_M026/main
FUNCNAME = 委托审批
FUNCPOINTCODE = LI_M02/A14  （审批权限点）
```

review 页不需要单独的菜单项（通过 main 页的按钮打开）。

### 8.6 审批人权限校验

m026 的 review.vue（复用自 m025）在审批模式下（mode='verify'）**不显示审批人选择器**（AutoComplete），因为审批操作（A14）不需要指定下一审批人。此逻辑由 review.vue 内部的 `v-if="mode === 'check'"` 控制，m026 无需额外处理。

但后端 RM11Controller 会校验当前用户是否为该项目的授权签字人（通过 VERIFYID 字段比对），权限不足时返回错误。前端在 review.vue 的 `hasPermission` 判断中预留了扩展点（目前默认 true，TODO 对接后端权限接口）。
