# 标准开发模板（AI 代码生成参考）

> 本文档用于指导 AI 和开发者快速生成符合项目规范的 generic-module + SFC 代码。
> 所有代码资产通过 m17 在线开发创建，存入 `tss_code_asset`（MODULEPATH = `@/modules/{moduleCode}/`）。

---

## 模式决策树

```
需求 → 什么类型？
├─ 基础 CRUD（增删改查）        → 模式 A: 标准CRUD
├─ CRUD + 审批流（提交/审核/审批）→ 模式 B: 审批流CRUD
├─ CRUD + 自定义Controller       → 模式 C: 自定义路由CRUD
├─ 纯统计图表                    → 模式 D: 报表页
├─ 选择器弹窗（选入数据）         → 模式 E: 选择页
├─ 复杂交互（树形/分屏/拖拽）     → 模式 F: 整页SFC
└─ 系统配置/元数据管理           → 保持现状（不适合 generic-module）
```

---

## 模式 A: 标准CRUD（最常用）

**适用**: b01/m01-m012, s01/m05/m06/m08/m11/m13/m14/m16 等基础数据管理

### 创建文件清单

```
@/modules/{MC}/
  store.js    ← Store扩展（可选，无自定义接口时不需要）
  main.js     ← 列表页扩展（页面编码=main 时）
  add.js      ← 表单页扩展（页面编码=add 时，文件名=页面编码）
```

> **命名规则**: 扩展JS文件名 = 页面编码（PAGECODE）。列表页通常叫 `main.js`，表单页通常叫 `add.js`。如果页面编码是 `edit`，文件就是 `edit.js`。

> **最小化原则**: 如果只是标准增删改查，不需要任何 SFC 扩展。只配 m18 即可。

### m18 配置

**页面**:

| PAGECODE | PAGENAME | PAGETYPE |
|----------|----------|----------|
| main | {模块名} | list |
| add | {模块名}编辑 | form |

**PAGECONFIG**:
```json
// main
{ "QRYPATH": "QRY", "QQRYSPATH": "QQRY", "defaultFormPageCode": "add" }
// add
{ "MAINPATH": "MAIN", "FORMLAYOUT": "twocolumn" }
```

**按钮**:

| BTNNAME | BTNCODE | BTNAREA | APICODE |
|---------|---------|---------|---------|
| 添加 | add | footer | A04 |
| 删除 | delete | footer | A07 |

### 完整 main.js 模板

```javascript
/**
 * {模块名} - 列表页扩展
 * MODULEPATH: @/modules/{MC}/main.js
 *
 * 仅在需要以下功能时才创建此文件：
 * - 自定义按钮显隐逻辑
 * - 批量操作
 * - 自定义查询/打印
 */
export default {
  computed: {
    // 按钮显隐：选中行时显示
    ISSELECTED() {
      return (this.selectedRows || []).length > 0;
    },
  },

  methods: {
    // 刷新列表
    refreshList() {
      if (this.$refs.list) this.$refs.list.query(1);
    },

    // 批量操作模板
    async batchOp(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) { this.$error('请先勾选记录'); return false; }
      var confirmed = await this.$confirm('确认对 ' + rows.length + ' 条记录执行「' + btn.BTNNAME + '」？');
      if (!confirmed) return false;

      this.$busy();
      try {
        await this.$callAction({
          action: this.moduleCode + '/batchOp',
          param: { items: rows.map(function(r) { return r.ID; }) },
          successText: '操作成功',
        });
        this.refreshList();
      } finally {
        this.$free();
      }
    },
  },
};
```

### 完整 add.js 模板（表单页扩展）

> 文件名 = 页面编码。如果 PAGECODE 是 `add`，文件就是 `add.js`；如果是 `edit`，就是 `edit.js`。

```javascript
/**
 * {模块名} - 表单页扩展
 * MODULEPATH: @/modules/{MC}/{PAGECODE}.js  (如 add.js)
 *
 * 仅在需要以下功能时才创建此文件：
 * - 字段联动计算
 * - onShow 初始化
 * - 自定义保存前校验
 */
export default {
  watch: {
    // 字段联动计算模板
    AMOUNT() { this._calcTotal(); },
    QTY() { this._calcTotal(); },
  },

  methods: {
    // 联动计算
    _calcTotal() {
      var qty = +this.QTY || 0;
      var price = +this.PRICE || 0;
      this.$MAIN.setValue('TOTAL', (qty * price).toFixed(2));
    },

    // onShow 初始化（新建时生成单据号等）
    async onShow() {
      if (this.ID) {
        await this.$store.dispatch(this.storeName + '/open', { FilterParams: { ID: this.ID } });
      } else {
        this.$store.dispatch(this.storeName + '/add');
        // 生成单据号（如有需要）
        // var ret = await this.$callAction({ action: this.moduleCode + '/getBillCode' });
        // this.$MAIN.setValue('BILLCODE', ret.Data.BILLCODE);
      }
    },
  },
};
```

### 完整 store.js 模板

```javascript
/**
 * {模块名} - Store 扩展
 * MODULEPATH: @/modules/{MC}/store.js
 *
 * Store03 已内置: query/open/add/save/delete/submit/check/verify/batch/call/flowSave
 * 仅在此添加 Store03 没有的自定义接口。
 */
export default {
  actions: {
    // 自定义查询接口
    async customQuery({ dispatch }, params) {
      return await dispatch('call', {
        APICODE: 'A51',
        params: params,
      });
    },

    // 获取单据号
    async getBillCode({ dispatch }) {
      return await dispatch('call', {
        APICODE: 'A44',
        params: { TCODE: 'XX|%Y%m%d|' },
      });
    },

    // 批量操作
    async batchOp({ dispatch }, { items }) {
      return await dispatch('call', {
        APICODE: 'A52',
        params: { items: items },
      });
    },
  },

  mutations: {
    // 自定义 mutation
    SET_CUSTOM(state, payload) {
      state.customData = payload;
    },
  },
};
```

---

## 模式 B: 审批流CRUD

**适用**: r01/m01, r01/m06 等需要提交/审核/审批的模块

### 按钮配置

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND |
|---------|---------|---------|---------|----------|
| 暂存 | save | footer | A04 | `ISSHOWSAVE` |
| 删除 | delete | footer | A07 | `ISSHOWDELETE` |
| 提交 | submit | footer | A08 | `ISSHOWSUBMIT` |
| 撤销提交 | reSubmit | footer | A09 | `ISSHOWRESUBMIT` |
| 审核 | check | footer | A12 | `ISSHOWCHECK` |
| 撤销审核 | reCheck | footer | A13 | `ISSHOWRECHECK` |
| 审批 | verify | footer | A14 | `ISSHOWVERIFY` |
| 撤销审批 | reVerify | footer | A15 | `ISSHOWREVERIFY` |

### 按钮显隐 computed 模板

```javascript
// add.js
export default {
  computed: {
    // 暂存：新建或待提交
    ISSHOWSAVE() {
      return !this.ID || this.STATE === '1' || this.STATE === 1;
    },
    // 删除：有ID且待提交
    ISSHOWDELETE() {
      return this.ID && (this.STATE === '1' || this.STATE === 1);
    },
    // 提交：待提交
    ISSHOWSUBMIT() {
      return this.STATE === '1' || this.STATE === 1;
    },
    // 撤销提交：已提交
    ISSHOWRESUBMIT() {
      return this.STATE === '2' || this.STATE === 2;
    },
    // 审核：待审核
    ISSHOWCHECK() {
      return this.STATE === '2' || this.STATE === 2;
    },
    // 审批：待审批
    ISSHOWVERIFY() {
      return this.STATE === '5' || this.STATE === 5;
    },
    // 表单禁用：非待提交状态
    disabled() {
      return this.ID && this.STATE !== '1' && this.STATE !== 1;
    },
  },
};
```

### 列表页批量审批模板

```javascript
// main.js
export default {
  computed: {
    // 选中行全部为指定状态
    ISSHOWBATCHCHECK() {
      return this._allState('2');
    },
    ISSHOWBATCHVERIFY() {
      return this._allState('5');
    },
  },

  methods: {
    _allState(state) {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE == state; }); // eslint-disable-line eqeqeq
    },

    // 批量审核（beforeAction 确认）
    async confirmBatch(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) { this.$error('请先勾选记录'); return false; }
      return await this.$confirm('确认对 ' + rows.length + ' 条记录执行「' + btn.BTNNAME + '」？');
    },

    // 批量审核
    async batchCheck(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchCheck',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '审核成功',
      });
      this.refreshList();
    },
  },
};
```

---

## 模式 C: 自定义Controller

**适用**: r01/m03(/api/rm13/call), r01/m05(/api/rm15/call), r01/m06(/api/rm16/call)

### store.js 配置 apiPath

```javascript
export default {
  actions: {
    // 自定义接口走专用 Controller
    async customAction({ dispatch }, params) {
      return await dispatch('call', {
        APICODE: 'A51',
        params: params,
        apiPath: '/api/rm13/call',  // ← 专用 Controller
      });
    },

    // 获取单据号走标准接口
    async getBillCode({ dispatch }) {
      return await dispatch('call', {
        APICODE: 'A44',
        apiPath: '/api/rm11/call',
        params: { TCODE: 'XX|%Y%m%d|' },
      });
    },
  },
};
```

---

## 模式 D: 报表页

**适用**: r02/m01/m02/m03

### PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "REPORT": {
    "APICODE": "A20",
    "PAGEMAX": 100,
    "CHART": {
      "type": "bar",
      "xField": "DEPTNAME",
      "yFields": ["CNT"],
      "initOption": {
        "title": { "text": "统计表" },
        "legend": {},
        "xAxis": { "axisLabel": { "rotate": 45 } }
      }
    }
  },
  "EXTENDJS": "@/modules/{MC}/main.js"
}
```

### 图表类型速查

| type | 用途 | yFields | 特有配置 |
|------|------|---------|---------|
| `bar` | 柱状图 | 数值字段数组 | `xAxis.axisLabel.rotate` |
| `line` | 折线图 | 数值字段数组 | `smooth: true` |
| `pie` | 饼图 | 单字段 | `radius: ['40%', '70%']` |

---

## 模式 E: 选择页

**适用**: 弹窗选入数据

### PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "SELECTMODE": "single"
}
```

> multiple 改为 `"multiple"`

---

## 模式 F: 整页SFC

**适用**: r01/m02, r01/m025/m026, r01/m031 等复杂交互

### PAGECONFIG

```json
{
  "PAGETYPE": "form",
  "SFCMODULEPATH": "@/modules/{MC}/add.vue"
}
```

### 整页 SFC 组件模板

```html
<template>
  <view-dialog :title="title" class="d-width">
    <template slot="body">
      <!-- 完全自定义内容 -->
    </template>
    <template slot="footer">
      <Button @click="closePage">取消</Button>
      <Button color="primary" @click="save">保存</Button>
    </template>
  </view-dialog>
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true }
  },
  computed: {
    title() { return this.host.ID ? '编辑' : '新增'; },
  },
  methods: {
    save() {
      this.host.save();
    },
    closePage() {
      this.host.closePage();
    },
  },
};
</script>
```

---

## 常用代码块库

### 1. 选择器字段配置（UI 设置）

| 场景 | EDITTYPE | SELECTDATA | UPDATEFIELDS |
|------|----------|-----------|--------------|
| 部门 | autocomplete | `{"selType":"dept"}` | `DEPTID,ID;DEPTNAME,DEPTNAME` |
| 员工 | autocomplete | `{"selType":"emp"}` | `EMPID,ID;EMPNAME,EMPNAME` |
| 客户 | autocomplete | `{"selType":"cust"}` | `CUSTID,ID;CUSTNAME,CUSTNAME` |
| 测量标准 | autocomplete | `{"selType":"tstdd"}` | `STDDID,ID;STDDNAME,STDDNAME` |
| 委托单 | autocomplete | `{"selType":"accept"}` | `ACCEPTID,ID;ACCEPTCODE,BILLCODE` |
| 部门树 | treepicker | `{"selType":"dept-tree"}` | `DEPTID,ID;DEPTNAME,DEPTNAME` |
| 自定义接口 | autocomplete | `{"module":"RS_M00","apiCode":"A05","keyName":"ID","titleName":"DEPTNAME"}` | 同上 |
| 带联动过滤 | autocomplete | `{"selType":"emp","paramMappings":"DEPTID,DEPTID"}` | 同上 |
| 带默认参数 | autocomplete | `{"selType":"emp","defaultParams":{"STATUS":"1"}}` | 同上 |

### 2. 下拉字段配置

| 场景 | EDITTYPE | SELECTDATA |
|------|----------|-----------|
| 字典 | select | `D0701`（字典名） |
| 固定选项 | select | `[{"key":"1","title":"启用"},{"key":"0","title":"停用"}]` |
| 文本格式 | select | `1:启用,0:停用` |
| 字典+筛选 | select | 字典名 + override `{dict:'D0701', items:['1','2']}` |

### 3. 文件上传配置

| 场景 | EDITTYPE | SELECTDATA |
|------|----------|-----------|
| 单文件 | fileupload | `{}` |
| 多文件(逗号id) | fileupload | `{"multifile":true}` |
| 子表模式 | fileupload | `{"mode":"subtable","subtable":"DTS","subMappings":"FILEID,id;FILENAME,name"}` |
| 图片上传 | imageupload | 同上 |
| 模板上传 | fileuploadtpl | `{"templateType":"YSJL","moduleCode":"LI_M01"}` |

### 4. 日期配置

| 场景 | EDITTYPE | cellProps |
|------|----------|-----------|
| 日期 | datepicker | `{format: 'YYYY-MM-DD'}` |
| 日期时间 | datepicker | `{format: 'YYYY-MM-DD HH:mm:ss'}` |
| 日期范围 | daterange | - |

### 5. 按钮显隐常用模式

```javascript
// 选中行时显示
ISSELECTED() { return (this.selectedRows || []).length > 0; }

// 仅选1行
ISSINGLE() { return (this.selectedRows || []).length === 1; }

// 选中行全部为某状态
_allState(state) {
  var rows = this.selectedRows || [];
  return rows.length > 0 && rows.every(function(r) { return r.STATE == state; });
}

// 选中行全部在某状态集合中
_allInStates(states) {
  var rows = this.selectedRows || [];
  return rows.length > 0 && rows.every(function(r) { return states.indexOf(+r.STATE) >= 0; });
}

// 表单：新建时可编辑
ISSHOWSAVE() { return !this.ID || this.STATE == '1'; }

// 表单：禁用
disabled() { return this.ID && this.STATE != '1'; }
```

### 6. 审批流状态码速查

| 状态码 | 含义 | 可执行操作 |
|--------|------|-----------|
| 1 | 待提交 | 保存/删除/提交 |
| 2 | 待审核 | 撤销提交/审核 |
| 5 | 待审批 | 撤销审核/审批 |
| 6 | 已审批 | 撤销审批 |
| 10 | 已签发 | - |
| 12 | 已驳回 | - |

### 7. 联动计算模板

```javascript
// 金额 = 数量 × 单价
watch: {
  QTY() { this._calc(); },
  PRICE() { this._calc(); },
},
methods: {
  _calc() {
    var total = (+this.QTY || 0) * (+this.PRICE || 0);
    this.$MAIN.setValue('TOTAL', total.toFixed(2));
  },
}

// 金额 = (基数 + 附加) × 折扣 + 其他
_calc() {
  var v = ((+this.CAMT || 0) + (+this.OAMT || 0)) * (+this.DISCOUNT || 1) + (+this.BAMT || 0);
  this.$MAIN.setValue('AMT', v.toFixed(2));
}

// 含数量
_calc() {
  var v = (+this.CNT || 0) * (+this.CAMT || 0) * (+this.DISCOUNT || 1) + (+this.OAMT || 0) + (+this.BAMT || 0);
  this.$MAIN.setValue('AMT', v.toFixed(2));
}
```

### 8. 调用后端接口模板

```javascript
// 标准查询
var ret = await this.$callAction({
  action: this.moduleCode + '/call',
  param: { APICODE: 'A51', params: { ID: row.ID } },
});

// 带成功提示
await this.$callAction({
  action: this.moduleCode + '/save',
  successText: '保存成功',
  successCall: function() { this.refreshList(); }.bind(this),
});

// 跨模块调用
await this.$callAction({
  action: 'R02_M07/call',
  param: { APICODE: 'A08', params: { ACCEPTID: id } },
});

// 带确认对话框
var confirmed = await this.$confirm('确认删除？');
if (!confirmed) return;
```

### 9. 打开页面/选入弹窗模板

```javascript
// 打开表单页
this.openPage({
  pageCode: 'add',
  mode: 'add',           // 'add' | 'edit'
  id: row.ID,            // 编辑时传 ID
  row: row,              // 可选：预填数据
  title: '编辑记录',
});

// 打开选入弹窗
this.openSelector({
  moduleCode: 'RS_M00',
  pageCode: 'select',
  target: 'DTSA',       // 写入哪个子表
  fieldMap: 'CUSTID,ID;CUSTNAME,CUSTNAME',  // 字段映射
  title: '选择客户',
  width: 800,
  filterParams: { STATUS: '1' },  // 过滤条件
  onSelected: function(rows) {
    console.log('选入完成', rows);
  },
});
```

### 10. list-t01 columnOverrides

```html
<list-t01
  :column-overrides="{
    STATE: { dict: 'D0701', width: 100, align: 'center' },
    AMOUNT: { title: '金额', align: 'right' },
    ACTION: { fixed: 'right', width: 120 }
  }"
/>
```

### 11. rs-form-edit overrides

```html
<rs-form-edit
  :path="$MAIN"
  :overrides="{
    CUSTNAME: { readonly: !isNew },
    AMOUNT: { type: 'number', cellProps: { precision: 2, min: 0 } },
    STATE: { type: 'select', dict: 'D0701', items: ['1', '2'] }
  }"
/>
```

---

## AI 代码生成提示词模板

开发新模块时，给 AI 以下提示词：

```
创建一个 {模块名} 模块（moduleCode: {MC}），包含：
- 列表页：{查询字段描述}，{列表列描述}
- 表单页：{表单字段描述}，{联动逻辑}
- 按钮：{按钮列表}
- 自定义接口：{如有}

请生成：
1. m18 配置（页面+按钮+字段）
2. store.js（自定义 actions）
3. main.js（列表扩展）
4. add.js（表单扩展，文件名=页面编码）
```

### 示例提示词

```
创建一个"设备校准记录"模块（moduleCode: LIB_M08），包含：
- 列表页：按设备名称/校准日期/状态查询，显示设备名/校准日期/有效期/状态
- 表单页：设备名(text)、校准日期(datepicker)、有效期(datepicker)、校准机构(autocomplete,selType:cust)、校准结果(select,字典D0801)、备注(textarea)
- 按钮：添加/删除/提交/审核
- 联动：选校准机构后回填机构ID

用标准CRUD模式（模式A+审批流）。
```

---

## 补充模式 A+: 主子表CRUD

**适用**: b01/m03(标准器+检定项), b01/m04(标准+标准器), r01/m06(委托+子表), r02/m07(物流+受理单)

### m18 字段配置

主表配置一个 `tableblock` 类型字段，自动渲染子表区块（ToolBar 标题 + 增删移按钮 + rs-table-edit）：

| 字段名 | EDITTYPE | SELECTDATA | 说明 |
|--------|----------|-----------|------|
| DTSDETAIL | tableblock | `{"subtable":"DTSA"}` | 子表区块，subtable=子表路径名 |

子表字段在子表资源（如 VCK_XXX_DTSA）的 uiSetFull 中配置。

### Store 多 path INIT

```javascript
// store.js
export default {
  actions: {
    add({ commit }) {
      // 多 path 初始化（MAIN + 子表）
      commit('INIT', { paths: ['MAIN', 'DTSA', 'DTSB'] });
      commit('ADD', { path: 'MAIN' });
    },
  },
};
```

### 表单扩展：子表行默认值

```javascript
// add.js — 新增子表行时带入主表字段
export default {
  methods: {
    // Add01 mixin 的 addDts 钩子：新增子表行后调用
    addDts(path) {
      if (path !== 'DTSA') return;
      var dts = this.$DTSA;
      if (!dts || !dts.data.length) return;
      var newRow = dts.data[dts.data.length - 1];
      // 带入主表默认值
      newRow.DEPTID = this.DEPTID || '';
      newRow.DEPTNAME = this.DEPTNAME || '';
    },

    // 批量选入写子表（去重）
    async onSelectItems(btn, context) {
      this.openSelector({
        moduleCode: 'RS_M00',
        pageCode: 'select',
        target: 'DTSA',
        fieldMap: 'ITEMID,ID;ITEMNAME,ITEMNAME',
        onSelected: function(rows) {
          var dts = this.$DTSA;
          var existIds = (dts.data || []).map(function(r) { return r.ITEMID; });
          rows.forEach(function(r) {
            if (existIds.indexOf(r.ID) < 0) {
              dts.add({ ITEMID: r.ID, ITEMNAME: r.ITEMNAME });
            }
          });
        }.bind(this),
      });
    },
  },
};
```

---

## 补充模式 G: 弹窗内嵌表单

**适用**: r02/m07(物流弹窗), 列表页内弹窗新增/编辑

### 父组件（列表页）控制弹窗

```javascript
// main.js — 列表页打开弹窗
export default {
  methods: {
    add(btn, context) {
      // 标准 generic-module 的 openPage
      this.openPage({ pageCode: 'add', mode: 'add' });
    },
    // 或打开独立组件弹窗
    openCustomModal(btn, context) {
      var row = context.row;
      this.modalDefaultValues = { CUSTID: row.ID, CUSTNAME: row.CUSTNAME };
      this.$refs.madd.show();
    },
  },
  data: { modalDefaultValues: {} },
};
```

### 弹窗组件模板

```html
<!-- custom-modal.vue -->
<template>
  <rs-modal v-model="opened" title="快速编辑" :width="600">
    <rs-meta-form
      v-if="opened"
      :path="dt"
      module-code="{MC}"
      :default-values="defaultValues"
      mode="twocolumn"
      ref="form"
    />
    <template slot="footer">
      <Button @click="opened = false">取消</Button>
      <Button color="primary" @click="save">保存</Button>
    </template>
  </rs-modal>
</template>
```

---

## VISIBLEIF / 字段条件显隐

### 机制

字段的 `VISIBLEIF` 配置一个方法名，扩展JS中定义该 computed 控制显隐。

未配置 VISIBLEIF 时，默认查找 `ISSHOW` + 字段名（如字段 `REFTYPE` → 查找 `ISSHOWREFTYPE`）。

### m18 配置

| 字段名 | VISIBLEIF | 说明 |
|--------|-----------|------|
| CERTNO | `ISSHOWCERTNO` | 显示条件方法名 |
| REFTYPE | （留空） | 自动查找 ISSHOWREFTYPE |

### 扩展JS

```javascript
// add.js
export default {
  computed: {
    // TYPE=1 时显示证书编号字段
    ISSHOWCERTNO() {
      return this.TYPE === '1' || this.TYPE === 1;
    },
    // STATE=6 时显示签发日期字段
    ISSHOWSIGNDATE() {
      return this.STATE === '6' || this.STATE === 6;
    },
  },
};
```

---

## EDITTYPE 完整清单

| EDITTYPE | 用途 | SELECTDATA 格式 | 备注 |
|---------|------|----------------|------|
| `text` | 文本输入 | - | 默认类型 |
| `textarea` | 多行文本 | - | - |
| `number` | 数字输入 | - | REFFIELDPREC 控制精度 |
| `select` | 下拉选择 | 字典名 / JSON数组 / k:v格式 | - |
| `datepicker` | 日期选择 | - | - |
| `daterange` | 日期范围 | - | 值为 {start, end} |
| `checkbox` | 勾选框 | - | 值 1/0 |
| `autocomplete` | 自动完成 | `{"selType":"dept"}` | 见选择器配置表 |
| `treepicker` | 树选择器 | `{"selType":"dept-tree"}` | - |
| `multiautocomplete` | 多选自动完成 | `{"mode":"subtable","subtable":"DTSA","subMappings":"ID,id"}` | 见下方 |
| `fileupload` | 文件上传 | `{}` / `{"multifile":true}` / 子表模式 | - |
| `imageupload` | 图片上传 | 同 fileupload | - |
| `fileuploadtpl` | 模板上传 | `{"templateType":"YSJL","moduleCode":"LI_M01"}` | - |
| `code` | 代码编辑器 | `{"language":"sql"}` | 支持 sql/javascript/clike |
| `editor` | 富文本 | - | UEditor |
| `image` | 图片预览 | - | 列表只读 |
| `tableblock` | 子表区块 | `{"subtable":"DTSA"}` | 自动渲染 ToolBar+增删移+rs-table-edit |
| `toolbar` | 分组标题 | - | 纯显示占整行，不参与校验 |
| `slot` | 自定义插槽 | - | 配合 field:xxx SFC slot |
| `pageaction` | 列表页按钮 | ACTIONCODE 格式 | 不生成列 |
| `action` | 行操作按钮 | ACTIONCODE 格式 | 列表行内按钮 |
| `index` | 序号列 | - | 自动 $serial |

---

## multiautocomplete 多选自动完成

### 两种模式

**模式1: subtable 子表模式**（选中项映射为子表行）

```json
{
  "selType": "accept",
  "mode": "subtable",
  "subtable": "DTSA",
  "subMappings": "ACCEPTID,ID;ACCEPTCODE,BILLCODE"
}
```

- `subMappings` 格式：`子表字段,远程字段;子表字段,远程字段`
- 选中委托单后，`ID` 写入子表 DTSA 的 `ACCEPTID` 列

**模式2: field 字段模式**（选中项 key 拼成逗号 id）

```json
{
  "selType": "emp",
  "mode": "field",
  "field": "EMPIDS"
}
```

- 存储结果如 `"id1,id2,id3"`

---

## 审批流弹窗（审核人选择 + 备注）

### 提交时选审核人

```javascript
// add.js — 提交时弹出选审核人
export default {
  data() {
    return {
      checkEmpVisible: false,   // 审核人弹窗
      checkRemark: '',           // 审核备注
    };
  },
  methods: {
    // beforeAction 钩子：提交前选审核人
    async beforeSubmit(btn, context) {
      // 弹出 Tooltip/AutoComplete 选审核人
      this.checkEmpVisible = true;
      // 返回 false 阻止自动提交，等用户选完
      return false;
    },

    // 选完审核人后执行提交
    async doSubmit(empId, empName) {
      this.$busy();
      try {
        await this.$callAction({
          action: this.storeName + '/flowSave',
          param: {
            ID: this.ID,
            ACTIONCODE: 'submit',
            CHECKID: empId,
            CHECKNAME: empName,
          },
          successText: '提交成功',
        });
        this.closePage();
      } finally {
        this.$free();
      }
    },

    // 驳回
    async reject(btn, context) {
      var remark = await this.$prompt('请输入驳回原因');
      if (!remark) return false;
      await this.$callAction({
        action: this.storeName + '/flowSave',
        param: { ID: this.ID, ACTIONCODE: 'reject', REMARK: remark },
      });
    },
  },
};
```

---

## EDITGROUP 表单分组

### 机制

字段的 `EDITGROUP` 配置分组名，generic-form 自动按分组分 Tab 渲染。

### m18 配置

| 字段名 | EDITGROUP | 说明 |
|--------|-----------|------|
| CUSTNAME | 基本信息 | 分组1 |
| LINKER | 基本信息 | 分组1 |
| ITEMID | 标准器 | 分组2 |
| ITEMNAME | 标准器 | 分组2 |

单分组或全部无 EDITGROUP 时不分 Tab，直接渲染。

---

## PAGETYPE 完整清单

| PAGETYPE | 用途 | 关键 PAGECONFIG |
|----------|------|----------------|
| `list` | 列表页 | QRYPATH / QQRYSPATH / defaultFormPageCode |
| `form` | 表单页 | MAINPATH / FORMLAYOUT / SFCMODULEPATH |
| `select` | 选择页 | SELECTMODE (single/multiple) |
| `report` | 报表页 | REPORT (APICODE/CHART) |
| `sfc` | 整页SFC | COMPONENTTYPE=sfc + SFCMODULEPATH |

---

## SFC Slot 完整清单

### 列表页 Slot

| Slot 名 | 位置 | 接收 props | 典型用途 |
|---------|------|-----------|---------|
| `simple-query` | 查询面板 | `host` | rs-meta-query-panel 查询条件 |
| `body-query` | 正文查询区 | `host` | 高级查询（dynamicQuery=false 时用） |
| `header-action` | 头部按钮区 | `host`, `buttons` | 自定义头部按钮 |
| `footer-action` | 底部按钮区 | `host`, `buttons` | 批量操作按钮 |
| `table-action` | 行操作列 | `host`, `buttons`, `row` | 行内操作按钮 |

### 表单页 Slot

| Slot 名 | 位置 | 接收 props | 典型用途 |
|---------|------|-----------|---------|
| `form-top` | 表单顶部 | `host` | 信息栏/状态提示 |
| `form-bottom` | 表单底部 | `host` | 附件列表/审批记录 |
| `field:{字段名}` | 替换字段控件 | `host`, `value` | 自定义控件（需 EDITTYPE=slot） |

---

## 调试技巧

### 查看 DataTable 数据

```javascript
// 浏览器 Console 中执行
$vm0.$store.state['LI_M07'].dt.MAIN       // 主表 DataTable
$vm0.$store.state['LI_M07'].dt.MAIN.data   // 数据行数组
$vm0.$store.state.app.scms['VCK_XXX']     // 字段配置
$vm0.$store.state.app.modules['LI_M07']   // 模块配置
```

### 扩展JS调试

```javascript
// main.js / add.js 中加日志
export default {
  methods: {
    beforeSave(btn, context) {
      console.log('[beforeSave] btn:', btn, 'context:', context);
      console.log('[beforeSave] MAIN:', this.$MAIN.data[0]);
      console.log('[beforeSave] DTSA:', this.$DTSA ? this.$DTSA.data : '无子表');
    },
  },
};
```

### 热更新

- m17 保存 SFC 后自动清 `moduleCache`
- `activated` 钩子重新加载扩展JS
- 浏览器 Ctrl+Shift+R 强刷可清 keep-alive 缓存

---

## 常见错误排查

| 错误 | 原因 | 解决 |
|------|------|------|
| `Cannot read properties of undefined (reading 'MODPATH')` | Store 模块未初始化 | 检查路由是否走 `/g/{MC}/main`，或 `app/initModule` 是否调用 |
| `The given key 'X' was not present` | 字段未在 tss_resfield 注册 | 在 m01 资源管理中为资源注册字段 |
| `mode` undefined (FormItem) | rs-form-cell 缺少父级 Form | rs-meta-field 设 `:wrap-form="true"` |
| 修改扩展JS不生效 | moduleCache 缓存 | m17 保存自动清缓存，或退出登录清全量 |
| Tab 切换后表格滚动条错乱 | keep-alive 缓存 | 组件 name 必须遵循 `{业务码}-{模块码}-main` |
| `removeProp is not a function` | Form 销毁时序 | rs-meta-field 已 provide 兜底 |

---

## 按钮配置字段说明

| 字段 | 说明 | 示例 |
|------|------|------|
| `BTNNAME` | 按钮名称 | `'添加'` |
| `BTNCODE` | 预设动作 | `add` / `edit` / `delete` / `save` / `submit` / `reSubmit` / `check` / `reCheck` / `verify` / `reVerify` / `custom` |
| `BTNAREA` | 按钮区域 | `header` / `footer` / `row` |
| `APICODE` | 后端接口编码 | `A04`(save) / `A07`(delete) / `A08`(submit) |
| `INTERACTTYPE` | 交互方式 | `poptip`(确认框) / 空(直接点击) |
| `SHOWCOND` | 显隐条件 | `ISSHOWSAVE` (扩展JS中的 computed 名) |
| `PERMCODE` | 权限码 | `M01_ADD` |
| `EXTPARAM` | JSON 扩展参数 | `{"action":"batchOp","beforeAction":"confirmBatch"}` |

### EXTPARAM 常用配置

```json
// 自定义 action（BTNCODE=custom 时）
{"action": "myCustomMethod"}

// beforeAction 钩子（执行前校验/确认）
{"beforeAction": "confirmBatch"}

// afterAction 钩子（执行后回调）
{"afterAction": "refreshList"}

// 指定表单页
{"formPageCode": "add"}

// 指定选入页
{"selectPageCode": "select", "selectModule": "RS_M00"}

// 组合使用
{"action": "batchApprove", "beforeAction": "confirmBatch", "afterAction": "refreshList"}
```

---

## BeforeAction vs 重写 save 选择指南

| 场景 | 方案 | 示例 |
|------|------|------|
| 保存前校验 | `beforeAction` 钩子 | `{"beforeAction":"validateForm"}` 返回 false 阻止 |
| 保存前确认 | `beforeAction` + `$confirm` | 同上 |
| 修改保存数据结构 | 重写 `save` 方法 | 组装特殊 XML 后调 `$callAction` |
| 完全自定义按钮 | `BTNCODE=custom` + `EXTPARAM.action` | 不走标准 CRUD 流程 |
| 审批流操作 | `BTNCODE=submit/check/verify` | Store03 内置 `flowSave` |

---

## pageConfigJson 访问

扩展JS中可直接访问 PAGECONFIG 解析后的 JSON：

```javascript
// main.js / add.js
export default {
  computed: {
    myConfig() {
      // 读取 PAGECONFIG 中的自定义配置
      return this.pageConfigJson.MY_CUSTOM_FIELD || {};
    },
  },
  mounted() {
    console.log('PAGECONFIG:', this.pageConfigJson);
    // { QRYPATH, QQRYSPATH, EXTENDJS, SLOTS, REPORT, FORMLAYOUT, ... }
  },
};
```

---

## SUBPAGES 跨模块页面引用

### PAGECONFIG 配置

```json
{
  "SUBPAGES": [
    { "REFMODULECODE": "RS_M00", "REFPAGECODE": "select-cust" }
  ]
}
```

`doOpenSelector` 自动查找 SUBPAGES 引用，可在按钮配置中直接使用其他模块的选择页。

---

## 数据流说明（INIT → ADD → open → save）

```
新建流程:
  dispatch('add')
    → commit('INIT', { paths: ['MAIN', 'DTSA'] })   // 初始化空 DataTable
    → commit('ADD', { path: 'MAIN' })               // 添加空行
    → 用户编辑 (setValue)
    → dispatch('save')                               // getXML() → POST 后端 A04

编辑流程:
  dispatch('open', { FilterParams: { ID: xxx } })
    → 后端 A02 返回数据
    → 回填 MAIN + DTSA + DTSB
    → 用户编辑 (setValue)
    → dispatch('save')                               // getXML() → POST 后端 A04

删除流程:
  dispatch('delete')                                 // getXML() → POST 后端 A07
```

---

## Store03 标准 Actions 完整对照表

| Action | 参数 | 返回值 | APITYPE | 说明 |
|--------|------|--------|---------|------|
| `query` | `{ isExport, columns, sumFields }` | 写入 QRY | query | 列表查询 |
| `advQuery` | `{ isExport, columns, APICODE }` | 写入 QRY | query | 高级查询（可覆盖 APICODE） |
| `open` | `{ FilterParams: {ID}, extraFilterParams }` | 写入 MAIN+子表 | open | 打开单条 |
| `add` | 无 | INIT+ADD | - | 新增空行 |
| `save` | `{ CHANGENOTE, SKIPVERSION }` 可选 | 回写 ID | save | 保存（含版本管理） |
| `delete` | 无 | - | delete | 删除（先 INIT+ADD 待删行） |
| `call` | `{ APICODE, params, apiPath }` | `ret` | 任意 | 通用调用 |
| `batch` | `{ APICODE, items, updateFields }` | `ret` | 批量 | 批量操作 |
| `flowSave` | `{ ID, ACTIONCODE, CHECKID, CHECKNAME, VERIFYID, VERIFYER, REMARK }` | - | flow | 审批流操作 |
| `submit` | 无（内部封存 ID） | - | submit | 提交 |
| `reSubmit` | 无 | - | reSubmit | 撤销提交 |
| `check` | 无（传 citem） | - | check | 审核 |
| `reCheck` | 无 | - | reCheck | 撤销审核 |
| `verify` | 无 | - | verify | 审批 |
| `reVerify` | 无 | - | reVerify | 撤销审批 |
| `invalid` | 无 | - | invalid | **作废**（STATE=6 时可用） |

### 标准 APICODE 对照表

| APICODE | 用途 |
|---------|------|
| A01 | 列表查询 |
| A02 | 打开单条 |
| A03 | 加载模块配置 |
| A04 | 保存 |
| A05 | 通用选择器查询 |
| A07 | 删除 |
| A08 | 提交 |
| A09 | 导出 |
| A12 | 审核 |
| A13 | 撤销审核 |
| A14 | 审批 |
| A15 | 撤销审批 |
| A16 | 驳回 |
| A17 | 提交（别名） |
| A20 | 报表查询 |
| A44 | 获取单据号 |
| A51+ | 自定义接口 |

### FILTERCODE 编号规范

| FILTERCODE | 用途 |
|-----------|------|
| F00 | 单条查询（`A.ID=@ID`） |
| F01 | 列表模糊搜索 |
| F02 | 高级查询 |
| F011 | 审核列表（CHECKID=当前用户） |
| F012 | 审批列表（VERIFYID=当前用户） |
| `@ui` | 自动生成（替代手写 NVelocity） |

### APITYPE 对照表

| APITYPE | 用途 | 标准 APICODE |
|---------|------|-------------|
| query | 列表查询 | A01 |
| open | 打开单条 | A02 |
| save | 保存 | A04 |
| delete | 删除 | A07 |
| submit/reSubmit | 提交/撤销 | A08/A13 |
| check/reCheck | 审核/撤销 | A12/A13 |
| verify/reVerify | 审批/撤销 | A14/A15 |
| getbillcode | 单据号 | A44 |
| batch* | 批量操作 | A51+ |
| NULL | 自定义 | 必须配 Controller |

---

## Add01 / List01 / Sel01 Mixin 内置方法

### Add01 mixin（表单页，generic-form 已内置）

| 方法 | 说明 |
|------|------|
| `save()` | 保存（校验 + callAction save） |
| `submit(ID)` / `reSubmit(ID)` | 提交/撤销提交 |
| `check(ID)` / `reCheck(ID)` | 审核/撤销审核 |
| `verify(ID)` / `reVerify(ID)` | 审批/撤销审批 |
| `invalid(ID)` | **作废** |
| `del()` | 删除 |
| `addDts(path)` | 新增子表行（**可覆盖**，带入默认值） |
| `removeDts(path, table)` | 删除子表行 |
| `moveUp(path, table)` / `moveDown(path, table)` | 子表行上下移 |
| `onShow()` | 表单显示时加载（**可覆盖**） |
| `close()` / `closeW()` | 关闭弹窗 |

### List01 mixin（列表页，generic-module 已内置批量操作）

| ISSHOWxxx | 实际判断（基于 selectedRows 的 STATE） |
|-----------|----------------------------------------|
| ISSHOWSUBMIT | 全部 STATE===1 |
| ISSHOWRESUBMIT | 全部 STATE===2 |
| ISSHOWCHECK | 全部 STATE===2 |
| ISSHOWRECHECK | 全部 STATE===5 或 19 |
| ISSHOWVERIFY | 全部 STATE===5 或 19 |
| ISSHOWREVERIFY | 全部 STATE===6 或 20 |

> **注意**: 状态码 3(已审核)/19/20 是扩展状态，模板中的简化判断需修正。

### Add01 ISSHOWxxx 实际逻辑（含创建人校验）

```javascript
// ISSHOWSAVE: 新建 或 (未启用自改权限) 或 (创建人本人)
(!STATE || STATE===1) && (isModifyBySelf===false || CREATEID=='' || CREATEID==userInfo.ID)
// ISSHOWDELETE: 有 ID 且同上
// ISSHOWRESUBMIT: STATE===2 且创建人校验
// ISSHOWCHECK: STATE===2（无创建人校验）
// ISSHOWRECHECK: STATE===3 或 5 或 19
// ISSHOWVERIFY: STATE===3 或 5 或 19
// ISSHOWREVERIFY: STATE===6 或 20
// ISSHOWINVALID: STATE===6
```

### Sel01 mixin（选择器参数，generic-form 已内置）

| 参数 | 用途 | keyName | titleName |
|------|------|---------|-----------|
| `this.deptParam` | 部门 | ID | DEPTNAME |
| `this.empParam` | 员工 | ID | EMPNAME |
| `this.custParam` | 客户 | ID | CUSTNAME |
| `this.provinceParam` | 省 | REGION_CODE | REGION_NAME |
| `this.cityParam` | 市（联动 PROVINCEID） | REGION_CODE | REGION_NAME |

---

## 前端数据流强制规范

### 四条强制规则

```javascript
// ❌ 禁止：扩展 JS 中 import db
import db from '@/api/db';  // ESLint 报错 no-restricted-imports

// ✅ 正确：通过 $callAction
await this.$callAction({ action: this.moduleCode + '/save' });

// ❌ 禁止：this.$store.dispatch
this.$store.dispatch('LI_M07/customAction');  // ESLint 报错 no-restricted-syntax

// ✅ 正确：$callAction
await this.$callAction({ action: this.moduleCode + '/customAction' });
```

### 铁律：不手拼 XML

```javascript
// ❌ 禁止：手动拼接 XML
var xml = '<MAIN><a><r c0="' + id + '"/></a></MAIN>';

// ✅ 正确：通过 setValue 让 Store03 自动调 getXML()
this.$MAIN.setValue('CUSTNAME', '张三');
// 保存时 Store03 自动生成:
// <MAIN l="u" c="ID,CUSTNAME" t="varchar,varchar">
//   <m><r c0="guid" oc0="guid" c1="张三" oc1="旧值"/></m>
// </MAIN>
```

### useDetailPage 不适用说明

generic-module 体系**不需要** `useDetailPage` / `closeTabAndBack` / `saveAndClose`。
- Tab 缓存由 keep-alive 的 `cachedViews` 自动管理
- 关闭页面用 `this.closePage()`（generic-form 内置）

---

## $callAction 完整参数

```javascript
await this.$callAction({
  action: 'LI_M07/save',          // moduleCode/actionName
  param: { CHANGENOTE: 'v2' },    // 传给 action 的参数
  successText: '保存成功',         // 成功提示
  errorText: '保存失败',           // 错误提示（覆盖默认）
  successCall: (ret) => { ... },   // 成功回调
  errorCall: () => { ... },        // 失败回调
  isBusy: true,                    // 显示加载中（默认 true）
  isSuccessBack: true,             // 成功后关闭弹窗
  timeOut: 500,                    // 延迟执行
});
```

### Vue 原型方法清单

| 方法 | 用途 |
|------|------|
| `this.$alert(msg)` | 成功提示 |
| `this.$error(msg)` | 错误提示 |
| `this.$confirm(content, title)` | 确认对话框（返回 Promise<boolean>） |
| `this.$busy(content?)` | 显示加载（返回 handle） |
| `this.$free(handle)` | 关闭加载 |
| `this.$callAction(opts)` | 调用 store action |
| `this.$Message(msg)` | 轻量提示 |

---

## 路由规则

| 场景 | URL | 说明 |
|------|-----|------|
| generic-module 列表 | `/g/{MC}/main` | 菜单 OUTERURL 配此格式 |
| generic-module 表单 | `/g/{MC}/{PAGECODE}` | 如 `/g/LIB_M07/add` |
| 传统四件套 | `/b01/m05/main` | 走 router.js 注册 |

### keep-alive 组件 name 规则

```javascript
// 组件 name 必须等于路由 name 的 '/' 替换为 '-'
export default {
  name: 'b01-m05-main',  // ✅ 路由 name = 'b01/m05/main'
  // name: 'CustManage',  // ❌ 不匹配，不会被 keep-alive 缓存
}
```

---

## 字段配置补充

### SHOWLENGTH 格式

| 格式 | 用途 |
|------|------|
| `"200"` | 列表宽度 200px |
| `">200"` | 列表最小宽度 200px |
| `"<300"` | 列表最大宽度 300px |
| `"0"` | 列表不显示 |
| 空 | 表单 `single=true`（独占整行） |

### COLSPAN

| COLSPAN | 效果 |
|---------|------|
| 1 或空 | 默认两列布局 |
| >=2 | 独占整行（single=true） |

### QUERYMODE（查询匹配方式）

| QUERYMODE | 含义 | 控件 |
|-----------|------|------|
| `like` | 模糊 | 文本输入 |
| `eq` | 精确 | 文本/下拉 |
| `in` | 多值 | 多选下拉 |
| `range` | 范围 | 双输入 |
| 空 | 自动推导 | QUERYTYPE > EDITTYPE > FIELDTYPE |

### 审计字段标准命名

```
CREATEID(varchar 64) / CREATER(varchar 16) / CREATETIME(datetime)
MODIFYID(varchar 64) / MODIFER(varchar 16) / MODIFYTIME(datetime)
ISDELETED(tinyint, 默认 0)
```

> **禁止下划线**: `CREATED_BY` / `CREATED_TIME` 是错误写法。

### 数据视图命名规则

- 物理表 `TBS_xxx` → 基础视图 `VBS_xxx`
- 业务表 `TCK_xxx` → 业务视图 `VCK_xxx`
- DATAVIEW.TABLENAME 直接填物理表名（不必 CREATE VIEW）

### ORDERBY 铁律

```
-- ✅ 正确：不带表别名
ORDERBY = 'SORTNO, PAGECODE'

-- ❌ 错误：带别名前缀
ORDERBY = 'A.SORTNO, A.PAGECODE'
-- 原因：BuildSQL01 包子查询 SELECT * FROM (...) T ORDER BY {orderBy}
-- 带前缀且列不在 SELECT 输出列时报 Unknown column
```

---

## 导出 Excel

```javascript
// 按钮配置：BTNCODE=export, APICODE=A09, BTNAREA=header
// list-t01 内置 exportExcel 方法，自动收集列和字典翻译

this.$callAction({
  action: this.storeName + '/query',
  param: { isExport: 1, columns: this.$refs.table.columns },
  successCall: (ret) => {
    window.open(`${this.$store.state.app.uploadUrl}/${ret}`);
  },
});
```

---

## 字典注册

```javascript
// 启动时 app store initDict 全量加载 tss_dict → heyui.addDict 全局注册
// 扩展 JS 中读取：
var dictMap = this.$store.state.app.dicts['D0701'];  // {key: title}
// 或
var dict = heyui.getDict('D0701');  // [{key, title}]

// 配置：tss_resuipc.SELECTDATA 写字典名（D0701）
// 字典编码规范：D07xx 业务字典，D06xx 系统字典
```

---

## 权限控制（v-per 指令）

```html
<!-- 按钮级权限 -->
<Button v-per="'M01_ADD'" @click="add">添加</Button>
<!-- 无权限时自动 display:none -->

<!-- 字段级权限 -->
<input v-per="'M01_VIEW_AMOUNT'" :value="row.AMOUNT" />
```

- `PERCODE` 对应 `tss_funcpoint.FUNCPOINTCODE`
- 权限存入 `store.state.app.fpoints`
- `v-per` 指令检查 `fpoints[permCode]`，无权限则隐藏

---

## 扩展 JS 生命周期

| 钩子 | 触发时机 | 可定义 |
|------|---------|--------|
| `init()` | mixin 注入后（created 阶段） | ✅ 异步初始化 |
| `mounted()` | Vue mounted | ✅ DOM 操作 |
| `activated()` | keep-alive 恢复 | ✅ 重新加载扩展（框架自动调） |

> **注意**: 扩展 JS 没有 `beforeDestroy`/`destroyed`。扩展 JS 的 `data` 是浅复制到实例，**不是响应式**。

### watch 陷阱

```javascript
// ✅ 正确：字段名直接作为 key
watch: { AMOUNT() { this._calc(); } }

// ❌ 错误：箭头函数丢失 this
watch: { AMOUNT: () => { this._calc(); } }

// ⚠️ 联动计算初值：watch 注册时字段可能未映射完成
// 需在 onShow 中手动触发一次
async onShow() {
  if (this.ID) await this.$store.dispatch(this.storeName + '/open', { FilterParams: { ID: this.ID } });
  this._calc();  // 手动触发一次联动计算
}
```

---

## 全局组件清单

| 组件 | 用途 |
|------|------|
| `RsTableList` | 只读表格（列表页） |
| `RsTableEdit` | 可编辑表格（子表） |
| `RsFormEdit` | 表单编辑器 |
| `RsFormCell` | 表单字段单元 |
| `RsModal` | 弹窗 |
| `ListT01` | 列表页模板 |
| `ReportT01` | 报表页模板 |
| `RsMetaForm` | 元数据表单（独立表单） |
| `RsMetaField` | 元数据单字段 |
| `RsMetaQueryPanel` | 查询面板 |
| `RsMetaQueryPanelField` | 查询面板单字段 |
| `RsUploader` | 文件上传 |
| `RsPrintPdf` | PDF 预览 |
| `RsCodeEditor` | 代码编辑器 |
| `RsOnlyofficePreview` | OnlyOffice 预览 |

---

## 自定义 Controller 路由规则

| 路由 | 后端 | 前端调用 |
|------|------|---------|
| `/api/data/call/{MC}/{APICODE}` | DataController | 标准 store action |
| `/api/rm11/call/{MC}/{APICODE}` | RM11Controller | `apiPath: '/api/rm11/call'` |
| `/api/rm13/call/{MC}/{APICODE}` | RM13Controller | `apiPath: '/api/rm13/call'` |
| `/api/rm15/call/{MC}/{APICODE}` | RM15Controller | `apiPath: '/api/rm15/call'` |

> 自定义 Controller 必须走 `/api/{Name}/call`，不能走 `/api/data/call`（到不了子类）。

---

## 开发检查清单

创建新模块时确认：

- [ ] m18 配置了页面（main/add）
- [ ] m18 配置了按钮（BTNCODE + SHOWCOND）
- [ ] m18 uiSetFull 配置了字段（EDITTYPE/SELECTDATA/QUERYSORT/LISTSORT/EDITSORT）
- [ ] tss_func 菜单 OUTERURL 设为 `/g/{MC}/main`
- [ ] SFC 资产创建了 store.js/main.js/add.js（文件名=页面编码）（仅自定义部分）
- [ ] 页面组件 name 遵循 `{业务码}-{模块码}-main` 规则
- [ ] 按钮钩子方法名与 EXTPARAM 配置一致
- [ ] 批量操作有确认对话框
- [ ] 联动计算覆盖所有相关字段的 watch
- [ ] 选择器 UPDATEFIELDS 配置正确
- [ ] 列表 columnOverrides 格式正确
- [ ] 表单 overrides 属性映射正确
