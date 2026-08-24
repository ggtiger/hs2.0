# r01/m025 委托审核（LI_M02）→ generic-module + 整页 SFC 迁移方案

## 迁移思路

m025 是**委托审核工作台**，其 review.vue（1968 行）是典型的复杂业务页面：
- 三栏分屏（记录列表 | OnlyOffice 证书预览 | rs-edit-item 原始记录）
- 可拖拽分隔条（横向宽度 + 纵向高度双轴拖动）
- 审核检查清单（7 分组 14 项 + AI 自动检查 + 失败原因）
- 跨 store 调用（r01/m025 + r01/m026 共用同一 review.vue）

这类页面无法拆分成 slot 粒度，采用**整页 SFC 重写**方案：
1. **数据库配置** - tss_module_page 配 main 列表 + review 整页（PAGETYPE=form + SFCMODULEPATH）
2. **PAGECONFIG** - review 页通过 SFCMODULEPATH 指向整页 SFC（不走 generic-form 渲染）
3. **Store 扩展** - 保留原 store.js 全部自定义 actions（query/advQuery/check/verify/loadWtList/detectAnomalies/loadChangeLogs/loadCertFileId/empSel1）
4. **main.js 扩展** - 上下双表联动 + 前端分页分组 + 手动单选 checkbox + 全屏审核弹窗
5. **review.vue 整页 SFC** - 1968 行整体迁移为 SFC 资产（template + script + style 全量保留）

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | COMPONENTTYPE | SFCMODULEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|---------------|---------------|--------------|--------------|
| main | 委托审核 | list | /g/LI_M025/main | generic-module | - | A53 | - | - |
| review | 审核工作台 | form | /g/LI_M025/review | sfc | `@/modules/LI_M025/review.vue` | - | - | - |

**关键点**：
- `main` 走标准 generic-module 列表渲染（rs-table-list + rs-query-panel）
- `review` 的 COMPONENTTYPE=`sfc` 表示 generic-form 完全跳过自身渲染，直接加载 SFCMODULEPATH 指定的 SFC 作为整页（review.vue 不再是子组件，而是独立页面）
- review 不需要 OPEN_APICODE（数据加载由 review.vue 内部按需调 store action）

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "EXTENDJS": "@/modules/LI_M025/main.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M025/query-panel.vue",
    "body-query": "@/modules/LI_M025/query-panel.vue"
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

**说明**：review 页的 SFCMODULEPATH 在 generic-form.vue 的 `loadSlotComponents()` 中被识别为整页 SFC，直接 `loadCompiledSFC(SFCMODULEPATH)` 渲染到根节点，跳过 FORMLAYOUT/字段渲染逻辑。EXTENDJS 提供 review 页的 mixin 扩展（可选）。

### 1.4 按钮配置 (tss_module_button)

**main 页按钮**（底部 footer 区域）：

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND | EXTPARAM |
|---------|---------|---------|---------|----------|----------|
| 委托审核 | custom | footer | - | `ISSHOWCHECK` | `{"action":"startReview","param":{"mode":"check"}}` |
| 委托审批 | custom | footer | - | `ISSHOWVERIFY` | `{"action":"startReview","param":{"mode":"verify"}}` |

**review 页按钮**（不配置，整页 SFC 内部自渲染底部操作栏）：

review.vue 的底部操作栏（审核通过/驳回/撤销/导出/变更记录）由 SFC 内部直接渲染，不进 tss_module_button。原因是这些按钮的状态依赖 `activeItem.STATE + checkList.every(checked) + mode`，配置化不划算。

---

## 二、Store 扩展（保留原 store.js 全部自定义 actions）

路径：`@/modules/LI_M025/store.js`

```javascript
/**
 * LI_M025 Store 扩展
 *
 * Store03 默认 actions (open/save/delete/call/batch/...) 已内置，无需重复定义。
 * 仅保留原 r01/m025/store.js 中的自定义 actions:
 *   - query/advQuery: 覆盖默认查询，使用审核专用 APICODE (A53/A54)
 *   - check/reCheck/reject/verify/reVerify: 带行级回写的审核流操作
 *   - loadWtList: 委托单列表（LI_M02/A53 orecord 粒度，前端按 REFBILLID 分组）
 *   - loadCertFileId: OnlyOffice 预览用证书文件 ID
 *   - detectAnomalies: AI 异常检测（标准器冲突/人员冲突/委托超期）
 *   - loadChangeLogs: 变更记录（LI_M02/A31）
 *   - empSel1: 审批人选择器（按部门+功能点过滤）
 */
export default {
  actions: {
    // ====== 审核专用查询（覆盖 Store03.query/advQuery）======

    // 列表查询：使用 A53（F011 过滤器，CHECKID=当前用户）
    async query({ commit, dispatch }, { isExport, columns, sumFields } = {}) {
      var row = this.state.helper.moudle.getApi('', 'A53');
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
      var ret = await dispatch('callRaw', {
        api: '/api/data/call/' + modeCode + '/' + APICODE + '/',
        params: params,
      });
      if (isExport) return ret;
      QQRY.setValue('TotalCount', ret.TotalCount);
      QQRY.setValue('SumInfo', ret.SumInfo);
      commit(Constants.M_INITDATA, { path: PATHNAME, data: ret.Items || [] });
    },

    // 高级查询：使用 A54（F021 过滤器）
    async advQuery({ commit, dispatch }, { isExport, columns, sumFields } = {}) {
      // 同 query，APICODE 改为 A54
      // ...
    },

    // ====== 审核流操作（带行级回写）======

    // 复核通过 A12（审核模式专用，需选下一审批人 VERIFYID/VERIFYER）
    async check({ dispatch }, { REMARK, ID, item, VERIFYID, VERIFYER }) {
      var ret = await dispatch('call', {
        APICODE: 'A12',
        params: { REMARK: REMARK, ID: ID, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER },
      });
      // 行级回写：同步 item 的 STATE/CHECKER/CHECKTIME/VERIFIER/VERIFYTIME
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 撤销审核 A13
    async reCheck({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', { APICODE: 'A13', params: { REMARK: REMARK, ID: ID } });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 驳回 A16（审核/审批通用）
    async reject({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', { APICODE: 'A16', params: { REMARK: REMARK, ID: ID } });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 审批通过 A14
    async verify({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', { APICODE: 'A14', params: { REMARK: REMARK, ID: ID } });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // 撤销审批 A15
    async reVerify({ dispatch }, { REMARK, ID, item }) {
      var ret = await dispatch('call', { APICODE: 'A15', params: { REMARK: REMARK, ID: ID } });
      if (ret && ret.length > 0) {
        ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].forEach(function(f) {
          item[f] = ret[0][f];
        });
      }
      return ret;
    },

    // ====== 列表数据加载（非 Store03.query，专用接口）======

    // 委托单列表（LI_M02/A53 orecord 粒度；前端按 REFBILLID 分组为委托单级别）
    async loadWtList({ dispatch }, { input, billDate }) {
      var filterParams = { INPUT: input || '' };
      if (billDate) filterParams.BILLDATE = billDate;
      return await dispatch('callRaw', {
        api: '/api/data/call/LI_M02/A53/',
        params: {
          PageSize: 9999,
          PageIndex: 1,
          FilterParams: filterParams,
        },
      });
    },

    // 加载证书文件 ID（OnlyOffice 预览；LI_M02/A49 返回 fileId 字符串）
    async loadCertFileId({ dispatch }, { id }) {
      return await dispatch('callRaw', {
        api: '/api/rm11/call/LI_M02/A49/',
        params: { ID: id },
      });
    },

    // 异常检测（LI_M02/A57；返回 { Code, Data: anomalies[] }）
    async detectAnomalies({ dispatch }, { id }) {
      return await dispatch('callRaw', {
        api: '/api/data/call/LI_M02/A57/',
        params: { FilterParams: { ID: id } },
      });
    },

    // 变更记录（LI_M02/A31；返回 { Items: [...] }）
    async loadChangeLogs({ dispatch }, { id }) {
      return await dispatch('callRaw', {
        api: '/api/data/call/LI_M02/A31/',
        params: {
          PageSize: 100,
          PageIndex: 1,
          FilterParams: { ID: id },
        },
      });
    },

    // ====== 下拉选择器 ======

    // 员工选择器（审批人；按部门+功能点过滤）
    async empSel1({ commit }, { INPUT, ID, DEPTID, FUNCID }) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M00/A13/',
        params: {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
            ID: ID || '-1',
            INPUT: INPUT,
            DEPTID: DEPTID,
            FUNCID: FUNCID,
          },
        },
      });
      commit(Constants.M_INITDATA, { path: 'EMPUSER', data: ret.Items || [] });
      return ret.Items || [];
    },

    // ====== 批量下载 ======

    async download({ dispatch }, { items }) {
      return await dispatch('batch', {
        APICODE: 'A39',
        items: items,
        updateFields: ['STATE'],
      });
    },
  },
};

// 日期格式化辅助：Date → yyyy-MM-dd（避免 MySQL str_to_date 解析失败）
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

## 三、main.js — 列表页扩展（上下双表联动 + 前端分页分组）

路径：`@/modules/LI_M025/main.js`

```javascript
/**
 * 委托审核列表页扩展
 *
 * 核心特性（无法配置化，必须走整页扩展）：
 *   1. 上下双表联动（上表=委托单，下表=委托明细）
 *   2. 前端分页：服务端返回 orecord 粒度（9999条），按 REFBILLID 分组后在客户端切片
 *   3. 手动单选 checkbox：委托单列表只能选一行（ checkbox 多选→强制保留最后一条）
 *   4. 全屏审核弹窗（review.vue 作为 modal 内容）
 *
 * this 上下文:
 *   this.moduleCode / this.storeName / this.storeObj
 *   this.$refs.list       - generic-module 列表组件
 *   this.selectedRows     - 选中的明细行
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
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
      _allOrecords: [], // 服务端返回的全量 orecord（不响应式）
      totalRows: 0,
      currentPage: 1,
      pageSize: 20,
      // 委托明细列表（下表）
      dtlLoading: false,
      dtlDatas: [],
      selectedDtlItems: [],
      // 审核弹窗
      showReviewModal: false,
      reviewMode: 'check', // 'check' | 'verify'
      // 审批人选择器
      VERIFYID: '',
      VERIFYER: '',
      empParam1: {
        loadData: this.empSel1,
        keyName: 'ID',
        titleName: 'EMPNAME',
      },
    };
  },

  computed: {
    // 委托审核按钮显隐：选中明细全部 STATE=2（待审核）
    ISSHOWCHECK() {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE === 2; });
    },
    // 委托审批按钮显隐：选中明细全部 STATE=5 或 19（待审批）
    ISSHOWVERIFY() {
      var rows = this.selectedDtlItems || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE === 5 || r.STATE === 19; });
    },
    // 分页对象（HeyUI table-tool-bar v-model）
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

  // 在 generic-module 的 mounted 钩子之后执行
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

    // ====== 委托单列表加载 ======
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
        // A53 返回 orecord 粒度，前端按 REFBILLID 聚合为委托单
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
        // 前端分页切片
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

    // ====== 双表联动（点击委托单→下表显示对应明细）======
    onWtClickRow(row) {
      // 手动实现单选：清除其他选中，只选中当前行
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

    // checkbox 选中（强制单选：保留最后一条）
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

    // ====== 启动审核/审批（打开 review.vue 整页弹窗）======
    startReview(btn, context) {
      var mode = (btn.EXTPARAM && btn.EXTPARAM.param && btn.EXTPARAM.param.mode) || 'check';
      if (!this.selectedWt) {
        this.$error('请先选择一条委托单！');
        return;
      }
      if (this.selectedDtlItems.length === 0) {
        this.$error(mode === 'verify' ? '请选择需要审批的委托明细！' : '请选择需要审核的委托明细！');
        return;
      }
      this.reviewMode = mode;
      this.showReviewModal = true;
      this.$nextTick(function() {
        if (this.$refs.reviewModal) this.$refs.reviewModal.show();
      }.bind(this));
    },

    onReviewClose() {
      this.showReviewModal = false;
      if (this.$refs.reviewModal) this.$refs.reviewModal.close();
      this.loadWtList(); // 重新加载
    },

    // ====== 审批人选择器（AutoComplete loadData 回调）======
    async empSel1(INPUT, callback) {
      if (this.TEMP1 === INPUT) INPUT = '';
      await this.$callAction({
        action: this.moduleCode + '/empSel1',
        param: {
          INPUT: INPUT,
          FUNCID: '3be11623d4114bc68a8e63551e861ced',
          DEPTID: this.selectedWt ? this.selectedWt.ADEPTID : '',
        },
        isBusy: false,
      });
      callback(this.EMPUSER);
    },
  },
};
```

---

## 四、query-panel.vue — 查询面板 SFC slot（可选）

路径：`@/modules/LI_M025/query-panel.vue`

> main.vue 原本使用顶部手写搜索栏（input + DateRangePicker + 查询/重置按钮），不走 QQRY DataTable。可选两种方案：
>
> **方案 A（保留原样）**：手写搜索栏直接放在 main.js 扩展的模板覆盖中（通过 `body-query` slot 注入）
> **方案 B（配置化）**：改用 rs-meta-query-panel，QQRY 配置关键字/日期范围字段
>
> 推荐方案 A，因为只有 2 个字段，配置化收益不大。query-panel.vue 内容直接复用原 main.vue 的 `<div class="h-panel-bar">` 区块。

---

## 五、review.vue — 整页 SFC（1968 行）

路径：`@/modules/LI_M025/review.vue`（数据库资产 tss_code_asset，MODULEPATH=`@/modules/LI_M025/review.vue`）

### 5.1 SFC 整体结构

```
<template>
  审核工作台（整页）
  ├── 顶部信息条（委托单号/委托时间/超期标签/审核进度）
  ├── 三栏分屏容器（.review-container）
  │   ├── 左栏：记录列表（可折叠，点击切换 activeItem）
  │   ├── 中栏：OnlyOffice 证书预览（rs-onlyoffice-preview）
  │   ├── 可拖拽分隔条（mousedown → mousemove → mouseup，含遮罩层防 iframe 捕获）
  │   └── 右栏：原始记录（rs-edit-item，复用 openPTEMP 逻辑）
  ├── 审核检查清单（可拖拽调整高度，7 分组 14 项 + AI 自动检查）
  ├── 底部操作栏（Tooltip 弹窗式审核通过/驳回/撤销/导出/变更记录）
  ├── 帮助文档弹窗（rs-modal）
  └── 变更记录弹窗（rs-modal）
</template>
```

### 5.2 三栏分屏布局（flex + 拖拽）

```html
<div class="review-container">
  <!-- 左栏：220px 固定宽度，可折叠到 40px -->
  <div class="review-left" :class="{ 'review-left--collapsed': leftCollapsed }">
    <div class="review-left-title">
      <span>{{ leftCollapsed ? '列表' : '记录列表' }}</span>
      <i :class="leftCollapsed ? 'h-icon-right' : 'h-icon-left'" @click="leftCollapsed = !leftCollapsed"></i>
    </div>
    <div class="review-left-list" v-show="!leftCollapsed">
      <div v-for="(item, index) in dtlItems" :key="item.ID || index"
           :class="{ 'review-left-item--active': activeItem && activeItem.ID === item.ID,
                     'review-left-item--done': item.STATE === 6 || item.STATE === 20,
                     'review-left-item--rejected': item.STATE === 12 }"
           @click="selectItem(item)">
        <!-- 序号圆圈 + 设备名/编号 + 状态标签 -->
      </div>
    </div>
  </div>

  <!-- 中栏：证书预览（OnlyOffice），宽度由 centerWidth 控制 -->
  <div class="review-center" :style="{ width: centerWidth + 'px', flex: 'none' }">
    <rs-onlyoffice-preview v-if="certFileId" :file-id="certFileId" :title="certFileName" :file-type="certFileType" />
  </div>

  <!-- 可拖拽分隔条 -->
  <div class="review-resizer" :class="{ 'review-resizer--active': isResizing }" @mousedown="startResize">
    <div class="review-resizer-line"></div>
  </div>

  <!-- 右栏：原始记录（rs-edit-item）-->
  <div class="review-right">
    <rs-edit-item v-if="refPmData && refPmData.length > 0" :layouts="refPmData" :select="{}" :parent="-1" :inLayout="false" />
  </div>
</div>
```

### 5.3 可拖拽分隔条实现（横向宽度拖动）

核心：`startResize(e)` → 创建全屏遮罩层（防 OnlyOffice iframe 捕获鼠标事件）→ `document.addEventListener('mousemove')` 实时更新 `centerWidth` → `mouseup` 移除遮罩。

```javascript
startResize: function(e) {
  var self = this;
  e.preventDefault();
  self.isResizing = true;
  var startX = e.clientX;
  var startCenterWidth = self.centerWidth;
  // 全屏遮罩层（z-index:9999，cursor:col-resize）
  var mask = document.createElement('div');
  mask.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9999;cursor:col-resize;';
  document.body.appendChild(mask);

  function onMouseMove(ev) {
    var diff = ev.clientX - startX;
    var newCenter = startCenterWidth + diff;
    if (newCenter < 300) newCenter = 300; // 最小 300px
    self.centerWidth = newCenter;
  }
  function onMouseUp() {
    self.isResizing = false;
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
    document.body.removeChild(mask);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }
  document.body.style.cursor = 'col-resize';
  document.body.style.userSelect = 'none';
  document.addEventListener('mousemove', onMouseMove);
  document.addEventListener('mouseup', onMouseUp);
}
```

审核清单区域纵向高度拖动（`startChecklistResize`）逻辑相同，仅光标改为 `row-resize`，边界限制 `60 ≤ height ≤ 400`。

### 5.4 审核检查清单（14 项 7 分组）

```javascript
checkList: [
  // basic - 基础信息完整性
  { key: 'basic_info', label: '设备名称、送校单位、委托方地址等基础信息完整', checked: false, autoResult: null, category: 'basic', failReason: '' },
  { key: 'env_condition', label: '环境条件（温湿度/气压）已填写且在合理范围', checked: false, autoResult: null, category: 'basic', failReason: '' },
  { key: 'standard_ref', label: '依据标准/规程已填写', checked: false, autoResult: null, category: 'basic', failReason: '' },
  // data - 数据/日期/环境条件复核
  { key: 'date_valid', label: '检校日期合理，无录入错误', checked: false, autoResult: null, category: 'data', failReason: '' },
  { key: 'data_no_error', label: '测量数据无超差项', checked: false, autoResult: null, category: 'data', failReason: '' },
  { key: 'uncertainty_range', label: '不确定度/测量结果在规定范围内', checked: false, autoResult: null, category: 'data', failReason: '' },
  // format - 格式规范
  { key: 'format_complete', label: '报告模板、编号、页码、栏目无缺失', checked: false, autoResult: null, category: 'format', failReason: '' },
  // compliance - 方法合规性
  { key: 'method_valid', label: '所用检定规程/校准规范/产品标准现行有效', checked: false, autoResult: null, category: 'compliance', failReason: '' },
  { key: 'deviation_approved', label: '方法偏离有审批和说明', checked: false, autoResult: null, category: 'compliance', failReason: '' },
  // standard - 标准器/人员核查
  { key: 'standard_expiry', label: '标准器在有效期内', checked: false, autoResult: null, category: 'standard', failReason: '' },
  { key: 'no_conflict', label: '标准器/人员无时间地域冲突', checked: false, autoResult: null, category: 'standard', failReason: '' },
  // ai - 数据真实性（AI 预留）
  { key: 'data_authenticity', label: '数据真实性与照片示值一致（AI）', checked: false, autoResult: null, category: 'ai', failReason: '' },
  // record - 原始记录完整性
  { key: 'record_complete', label: '原始记录完整', checked: false, autoResult: null, category: 'record', failReason: '' },
  { key: 'conclusion_correct', label: '结论正确', checked: false, autoResult: null, category: 'record', failReason: '' },
]

categoryLabels: {
  basic: '基础信息完整性',
  data: '数据/日期/环境条件复核',
  format: '格式规范检查',
  compliance: '方法合规性',
  standard: '标准器/人员核查',
  ai: '数据真实性（AI）',
  record: '原始记录完整性',
}
```

### 5.5 AI 自动检查逻辑（autoCheckAll）

点击"自动检查"按钮触发，按 category 依次检查 14 项，设置每项的 `autoResult: 'pass'|'fail'|'warn'`、`checked`、`failReason`。

**字段对照表**（VCK_ORECORD 业务字段）：

| 业务含义 | 字段名 | 检查规则 |
|---------|--------|---------|
| 设备名称 | MNAME | 必填，空则 fail |
| 型号规格 | SIZETYPE | 必填，空则 fail |
| 出厂编号 | OPCODE | 必填，空则 fail |
| 送校单位 | CUSTNAME | 必填，空则 fail |
| 委托方地址 | ADDR | 必填，空则 warn |
| 检校温度 | CTEMPERATURE | 数值范围 [15, 35]℃ |
| 检校湿度 | CHUMIDITY | 数值范围 [20, 80]% |
| 检校大气压 | ATMOS | 有值即 pass |
| 检校依据 | TSTANDARDNAME | 必填，与 REGUITEMNAME 二选一 |
| 依据规程 | REGUITEMNAME | 必填，与 TSTANDARDNAME 二选一 |
| 检校日期 | BILLDATE | 距今天数 [-1, 365] 合理 |
| 证书编号 | CERTCODE | 必填，空则 fail |
| 原始记录模板 | PTEMPLATEID | 必填，空则 warn |
| 不确定度 | STTDEGREE | 有值即 pass |
| 使用前状态 | BEFOREUSE | - |
| 使用后状态 | AFTERUSE | 结论正确性判断依据 |
| 其他条件 | OTHER | 方法偏离检测 |
| 不确定评论 | OTHERCONTENT | 方法偏离说明 |

**AI 异常检测**（`detectWarnings`）：优先调后端 A57 接口，失败则降级为前端逻辑：
- 标准器冲突：同一标准器（TSTANDARDID/TSTANDARDNAME）在多个 BILLDATE 出现
- 人员冲突：同一 CHECKER/CREATER 在同一 BILLDATE 超过 5 条记录

### 5.6 原始记录加载（rs-edit-item 数据填充）

核心方法 `loadRecordTemplate(item)`，6 步骤：

1. 调 `store.open({ ID: item.ID })` 加载 orecord 到 MAIN/DTSA/DTSB
2. 调 `store.openPTEMP({ ID: item.PTEMPLATEID, ISEDIT: true, item: item })` 加载模板配置
3. 从 MAIN.DataTable 获取 REFTPMDATA（JSON，dealTreeData 已填充 field.value）
4. `dealConfigSelect(tpmData, self)` 递归构建 inputObj/tableObj/editorObj 索引
5. 补充 DTSB/DTSA 数据到 inputObj/editorObj/tableObj（不走 SETSHOWTPMDATA，避免 ARD 不存在崩溃）
6. `this.$set(this, 'refPmData', tpmData)` 触发 rs-edit-item 重新渲染

### 5.7 底部操作栏（Tooltip 弹窗式审核）

```html
<Tooltip theme="white" trigger="click" editable ref="reviewTip">
  <Button color="primary" :loading="isReviewing" :disabled="!canApprove">{{ approveBtnText }}</Button>
  <div slot="content">
    <textarea v-model="reviewRemark" :placeholder="'请输入' + approveBtnText + '说明'"></textarea>
    <!-- 审核模式（check）需要选择下一审批人 -->
    <template v-if="mode === 'check'">
      <AutoComplete :option="empParam1" v-model="VERIFYID" @change="onVerifyChange" />
    </template>
    <Button color="primary" @click.native="handleApprove">{{ passBtnText }}</Button>
    <Button color="red" @click.native="handleReject">{{ rejectBtnText }}</Button>
  </div>
</Tooltip>
```

**按钮显隐逻辑**（基于 activeItem.STATE + mode）：

| 条件 | 显示按钮 |
|------|---------|
| mode=check & STATE=2 | 复核通过/复核驳回（Tooltip 弹窗） |
| mode=verify & STATE=5/19 | 审批通过/审批驳回（Tooltip 弹窗） |
| mode=check & STATE=3 | 撤销审核（Poptip 确认） |
| mode=verify & STATE=6/20 | 撤销审批（Poptip 确认） |
| STATE=12 | "已驳回"文字（不可撤销） |
| 其他 | 状态文字（stateLabel） |

**关键 bug 修复**：dispatch 前必须 `closeReviewTip()` 关闭 Tooltip 并清理 body 上残留的 `.h-tooltip-popper` DOM，避免 STATE 变化触发 v-if 移除 Tooltip 后 popper 残留。

### 5.8 模式自适应（mode + storeName）

review.vue 同时服务 m025（审核）和 m026（审批）两个模块，通过 props 切换：

```javascript
props: {
  mode: { type: String, default: 'check' },     // 'check' = 审核(STATE=2), 'verify' = 审批(STATE=5/19)
  storeName: { type: String, default: 'r01/m025' },
},
computed: {
  currentStoreName: function() {
    return this.storeName || Constants.STORE_NAME;
  },
  approveBtnText: function() {
    return this.mode === 'verify' ? '审批' : '审核';
  },
  // passBtnText / rejectBtnText 同理
}
```

所有 `dispatch` 调用都走 `this.currentStoreName + '/' + actionName`，实现单组件复用。

### 5.9 跨 store 引用

review.vue 同时 import 了两个 store：

```javascript
import { getStore as getStore025 } from '../store';       // r01/m025（自身）
import { getStore as getStore026 } from '../../m026/store'; // r01/m026（审批复用时使用）

getCurrentStore: function() {
  if (this.currentStoreName === 'r01/m026') return getStore026();
  return getStore025();
}
```

迁移为 SFC 后，跨 store 引用通过 `@/modules/LI_M026/store.js` 导入。

---

## 六、迁移对照表

| 原 r01/m025 文件 | 迁移后 | 说明 |
|----------------|--------|------|
| `router.js` | 不需要 | generic-module 路由自动注册（/g/LI_M025/main、/g/LI_M025/review） |
| `store.js` | `@/modules/LI_M025/store.js` | 保留全部自定义 actions（query/advQuery/check/verify/loadWtList/loadCertFileId/detectAnomalies/loadChangeLogs/empSel1/download） |
| `views/main.vue` | m18 配置 + `main.js` 扩展 | 上下双表逻辑、前端分页分组、手动单选、全屏弹窗 → main.js |
| `views/main.vue` 搜索栏 | `query-panel.vue` SFC slot（可选） | 顶部 input + DateRangePicker，可保留手写或配置化 |
| `views/review.vue`（1968 行） | `@/modules/LI_M025/review.vue` SFC 资产 | **整页迁移**：三栏分屏 + OnlyOffice + rs-edit-item + 检查清单 + 拖拽 + Tooltip 审核 |
| `mapDateTable('QQRY', [...])` | 不需要 | main.vue 走自定义 wtDatas/dtlDatas，不用 QQRY DataTable |
| `mapDateTable('EMPUSER', [])` | 扩展 JS data | EMPUSER 改为本地 data 属性（由 empSel1 action 直接填充） |
| `mapDateTable('MAIN', ['REFTPMDATA'])` | 不需要 | review.vue 整页直接读 storeHelper.getTable('MAIN') |
| 审核流按钮显隐 | 扩展 JS computed | ISSHOWCHECK / ISSHOWVERIFY（基于 selectedDtlItems 的 STATE 判断） |
| 批量审核/撤销/驳回 | 扩展 JS methods | 通过 EXTPARAM.action 路由到 startReview |
| Tooltip popper 清理 | 保留在 review.vue 内 | closeReviewTip() + body 残留 popper 兜底清理 |
| 跨 store 引用（m026） | SFC import | `import { getStore as getStore026 } from '@/modules/LI_M026/store'` |

---

## 七、迁移后目录结构

```
src/modules/LI_M025/           # SFC 扩展资产（数据库 tss_code_asset）
  store.js                     # Store 扩展（query/advQuery/check/verify/loadWtList/...）
  main.js                      # 列表页扩展（上下双表 + 前端分页 + 手动单选 + 全屏弹窗）
  review.vue                   # 审核工作台整页 SFC（1968 行，三栏分屏 + OnlyOffice + AI 检测）
  query-panel.vue              # 查询面板 SFC slot（可选，2 字段简单搜索）
```

原 `src/pages/r01/m025/` 目录可删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M025/main` 自动注册。

review 页通过 `this.$router.push({ path: '/g/LI_M025/review', query: { mode: 'check', storeName: 'r01/m025' }})` 跳转，或在 main.js 中作为全屏 modal 内嵌。

---

## 八、迁移注意事项

### 8.1 SFCMODULEPATH 整页渲染机制

generic-form.vue 的 `loadSlotComponents()` 检测到 PAGECONFIG.SFCMODULEPATH 后，跳过 FORMLAYOUT/字段渲染，直接将 SFC 渲染到根节点。SFC 内部通过 `this.$store.state[this.storeName]` 访问 store 数据。

### 8.2 双 store 共享 review.vue

m026（委托审批）的 main 页打开 review.vue 时传入 `mode='verify'` 和 `storeName='r01/m026'`，review.vue 内部所有 dispatch 走 r01/m026 store。迁移后：
- `@/modules/LI_M025/review.vue` 保持不变
- `@/modules/LI_M026/main.js` 中 startReview 传 `storeName='r01/m026'`
- `@/modules/LI_M026/review.vue` 可以不创建，直接复用 `@/modules/LI_M025/review.vue`

### 8.3 rs-edit-item 异步加载

rs-edit-item 组件需懒加载（避免 review.vue 同步 import 导致 webpack code-split 失败）：

```javascript
components: {
  'rs-edit-item': function() { return import('@/components/edit/rs-edit-item.vue'); },
}
```

迁移为 SFC 后同样保留此懒加载方式。

### 8.4 OnlyOffice iframe 鼠标事件捕获

拖拽分隔条时必须创建全屏遮罩层（`z-index: 9999`），否则 OnlyOffice 的 iframe 会捕获 mousemove 事件导致拖拽失效。原代码已实现，迁移时务必保留。

### 8.5 empSel1 的 DEPTID 联动

审批人选择器的过滤条件依赖 `selectedWt.ADEPTID`（检验部门），main.js 中的 empSel1 方法需保留此联动。

### 8.6 tss_func 菜单配置

```
OUTERURL = /g/LI_M025/main
FUNCNAME = 委托审核
FUNCPOINTCODE = LI_M02/A12  （审核权限点）
```

review 页不需要单独的菜单项（通过 main 页的按钮打开）。
