# r01/m05 迁移到 generic-module + SFC 扩展方案

## 迁移思路

将传统"四件套"(router.js/store.js/main.vue/add.vue) 拆解为：
1. **数据库配置** - tss_module_page + tss_module_button + tss_resuipc（可视化配置）
2. **SFC 扩展JS** - 复杂业务逻辑（按钮显隐/批量操作/联动计算）
3. **Store 扩展** - 跨模块调用 + 自定义查询
4. **SFC Slot** - 查询面板/表单字段替换/按钮区

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|--------------|
| main | 受理单列表 | list | /g/LI_M00/main | A01 | - | - |
| add | 受理单表单 | form | /g/LI_M00/add | - | A02 | A04 |

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/LI_M00/main.js",
  "SLOTS": {
    "simple-query": "@/modules/LI_M00/query-panel.vue",
    "header-action": "@/modules/LI_M00/header-actions.vue",
    "footer-action": "@/modules/LI_M00/footer-actions.vue"
  }
}
```

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "FORMLAYOUT": "twocolumn",
  "EXTENDJS": "@/modules/LI_M00/form.js",
  "SLOTS": {
    "form-top": "@/modules/LI_M00/form-top.vue",
    "field:AEMPNAME": "@/modules/LI_M00/field-emp.vue",
    "field:CUSTNAME": "@/modules/LI_M00/field-cust.vue",
    "field:PTEMPLATENAME": "@/modules/LI_M00/field-ptmp.vue",
    "field:ADEPTNAME": "@/modules/LI_M00/field-dept.vue",
    "field:FILES": "@/modules/LI_M00/field-files.vue"
  }
}
```

### 1.4 按钮配置 (tss_module_button)

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND |
|---------|---------|---------|---------|----------|----------|
| 添加 | add | footer | A04 | - | - |
| 添加物流 | custom | footer | - | `{"action":"addLogistics"}` | `ISSHOWLOGISTICS` |
| 确定退样 | custom | footer | A51 | `{"action":"batchReturn","beforeAction":"confirmBatch"}` | `ISSHOWRETURN` |
| 提交 | submit | footer | A08 | - | `ISSHOWSUBMIT` |
| 撤销提交 | reSubmit | footer | A09 | `{"beforeAction":"confirmBatch"}` | `ISSHOWRESUBMIT` |
| 完成 | custom | footer | A23 | `{"action":"batchComplete"}` | `ISSHOWCOMPLETE` |
| 撤销完成 | custom | footer | A24 | `{"action":"batchReComplete","beforeAction":"confirmBatch"}` | `ISSHOWRECOMPLETE` |
| 受理 | custom | footer | A14 | `{"action":"batchAccept"}` | `ISSHOWACCEPT` |
| 撤销受理 | custom | footer | A15 | `{"action":"batchReAccept","beforeAction":"confirmBatch"}` | `ISSHOWREACCEPT` |
| 受理打印 | custom | footer | A21 | `{"action":"aprint"}` | `ISSHOWAPRINT` |
| 受理便签打印 | custom | footer | A22 | `{"action":"pprint"}` | - |
| 证书打印 | custom | footer | A17 | `{"action":"print"}` | `ISSHOWPRINT` |
| 证书下载 | custom | footer | A20 | `{"action":"download"}` | `ISSHOWPDOWNLOAD` |

表单页按钮：

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND |
|---------|---------|---------|---------|----------|
| 暂存 | save | footer | A04 | `ISSHOWSAVE` |
| 删除 | delete | footer | A07 | `ISSHOWDELETE` |
| 提交 | submit | footer | A08 | `ISSHOWSUBMIT` |
| 撤销提交 | reSubmit | footer | A09 | `ISSHOWRESUBMIT` |

---

## 二、main 页 SFC 扩展JS

路径：`@/modules/LI_M00/main.js`

```javascript
/**
 * 受理单列表页扩展
 *
 * this 上下文:
 *   this.moduleCode / this.storeName / this.storeObj
 *   this.$refs.list  - 列表组件
 *   this.selectedRows - 选中的行
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
 */
export default {
  computed: {
    // ====== 按钮显隐逻辑（基于选中行的 STATE 判断）======

    // 退样：全部 STATE===1
    ISSHOWRETURN() {
      return this._allSelectedState(1);
    },
    // 提交：全部 STATE===1
    ISSHOWSUBMIT() {
      return this._allSelectedState(1);
    },
    // 撤销提交：全部 STATE===7或8
    ISSHOWRESUBMIT() {
      return this._allSelectedIn([7, 8]);
    },
    // 完成：全部 STATE===7
    ISSHOWCOMPLETE() {
      return this._allSelectedState(7);
    },
    // 撤销完成：全部 STATE===15
    ISSHOWRECOMPLETE() {
      return this._allSelectedState(15);
    },
    // 受理：全部 STATE===7
    ISSHOWACCEPT() {
      return this._allSelectedState(7);
    },
    // 撤销受理：全部 STATE===8 且当前用户是 AEMPID
    ISSHOWREACCEPT() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      var empid = this.$store.state.user.userInfo.EMPID;
      return rows.every(function(r) {
        return r.STATE === 8 && r.AEMPID === empid;
      });
    },
    // 证书打印：仅1行且 STATE===10/11/14
    ISSHOWPRINT() {
      var rows = this.selectedRows || [];
      if (rows.length !== 1) return false;
      return [10, 11, 14].indexOf(rows[0].STATE) >= 0;
    },
    // 证书下载：全部 STATE===10/11/14
    ISSHOWPDOWNLOAD() {
      return this._allSelectedIn([10, 11, 14]);
    },
    // 受理打印：选中行 BILLDATE/CUSTNAME/SENDNAME 相同
    ISSHOWAPRINT() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      var first = rows[0];
      return rows.every(function(r) {
        return r.BILLDATE === first.BILLDATE
          && r.CUSTNAME === first.CUSTNAME
          && r.SENDNAME === first.SENDNAME;
      });
    },
    // 添加物流：有选中行
    ISSHOWLOGISTICS() {
      return (this.selectedRows || []).length > 0;
    },
  },

  methods: {
    // ====== 辅助方法 ======

    // 选中行是否全部为指定 STATE
    _allSelectedState(state) {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return r.STATE === state; });
    },
    // 选中行是否全部在指定 STATE 列表中
    _allSelectedIn(states) {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return states.indexOf(r.STATE) >= 0; });
    },

    // 批量操作确认（beforeAction 钩子）
    async confirmBatch(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选记录');
        return false;
      }
      return await this.$confirm('确认对 ' + rows.length + ' 条记录执行「' + btn.BTNNAME + '」操作？');
    },

    // ====== 批量操作 ======

    async batchReturn(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReturn',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '退样成功',
      });
      this.$refs.list.query(1);
    },

    async batchComplete(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchComplete',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '完成成功',
      });
      this.$refs.list.query(1);
    },

    async batchReComplete(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReComplete',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '撤销完成成功',
      });
      this.$refs.list.query(1);
    },

    async batchAccept(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchAccept',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '受理成功',
      });
      this.$refs.list.query(1);
    },

    async batchReAccept(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReAccept',
        param: { items: rows.map(function(r) { return r.ID; }) },
        successText: '撤销受理成功',
      });
      this.$refs.list.query(1);
    },

    // ====== 打印/下载 ======

    async print(btn, context) {
      var row = (this.selectedRows || [])[0];
      if (!row) return;
      this.$busy();
      try {
        var ret = await this.$callAction({
          action: this.moduleCode + '/call',
          param: { APICODE: 'A17', params: { ID: row.ID } },
        });
        // 展示 PDF
        this.$refs.mpdf.show(ret.Data.url);
      } finally {
        this.$free();
      }
    },

    async aprint(btn, context) {
      var rows = this.selectedRows || [];
      var ids = rows.map(function(r) { return r.ID; }).join(',');
      await this.$callAction({
        action: this.moduleCode + '/call',
        param: { APICODE: 'A21', params: { ID: ids } },
        successCall: function(ret) {
          if (ret.Data && ret.Data.url) {
            this.$refs.mpdf.show(ret.Data.url);
          }
        }.bind(this),
      });
    },

    pprint(btn, context) {
      var rows = this.selectedRows || [];
      var ids = rows.map(function(r) { return r.ID; }).join(',');
      this.$callAction({
        action: this.moduleCode + '/call',
        param: { APICODE: 'A22', params: { ID: ids } },
        successCall: function(ret) {
          if (ret.Data && ret.Data.url) {
            this.$refs.mpdf.show(ret.Data.url);
          }
        }.bind(this),
      });
    },

    download(btn, context) {
      var rows = this.selectedRows || [];
      var ids = rows.map(function(r) { return r.ID; }).join(',');
      this.$callAction({
        action: this.moduleCode + '/call',
        param: { APICODE: 'A20', params: { ID: ids } },
        successCall: function(ret) {
          if (ret.Data && ret.Data.url) {
            window.open(ret.Data.url);
          }
        }.bind(this),
      });
    },

    // ====== 添加物流 ======

    async addLogistics(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选受理单');
        return;
      }

      // 检查是否已有物流记录
      this.$busy();
      try {
        var existIds = [];
        for (var i = 0; i < rows.length; i++) {
          var ret = await this.$callAction({
            action: this.moduleCode + '/checkLogisticsExists',
            param: { ACCEPTID: rows[i].ID },
          });
          if (ret.Data && ret.Data.exists) {
            existIds.push(rows[i].BILLCODE || rows[i].ID);
          }
        }
        if (existIds.length > 0) {
          var confirmed = await this.$confirm(
            '以下受理单已有物流记录：' + existIds.join(', ') + '，是否继续？'
          );
          if (!confirmed) return;
        }

        // 构建物流表单默认值
        var acceptItems = rows.map(function(r) {
          return { ACCEPTID: r.ID, ACCEPTCODE: r.BILLCODE };
        });

        // 确保物流模块已加载
        await this.$store.dispatch('app/initModule', 'R02_M07');

        // 打开物流弹窗
        this.logisticsAcceptItems = acceptItems;
        this.$refs.mlogistics.show();
      } finally {
        this.$free();
      }
    },

    onLogisticsSaved() {
      this.$refs.mlogistics.close();
      this.$alert('物流添加成功');
    },
  },

  // init: 初始化物流弹窗数据
  init() {
    this.logisticsAcceptItems = [];
  },
};
```

---

## 三、main 页查询面板 SFC Slot

路径：`@/modules/LI_M00/query-panel.vue`

```html
<template>
  <div slot="simple-query" v-if="qqryDt">
    <rs-meta-query-panel
      :path="qqryDt"
      module-code="LI_M00"
      :overrides="fieldOverrides"
      :cell-width="6"
      :show-buttons="false"
      @query="onQuery"
    />
    <div style="text-align:right; padding:4px 0;">
      <Button color="primary" @click="doSearch">查询</Button>
      <Button class="ml5" @click="doReset">重置</Button>
    </div>
  </div>
</template>

<script>
export default {
  props: { host: { type: Object, required: true } },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    },
    fieldOverrides() {
      return {
        BUSTYPEID: {
          type: 'select',
          datas: [
            { key: '1', title: '委外' },
            { key: '2', title: '自检' },
            { key: '3', title: '期间核查' },
            { key: '4', title: '其他' },
          ],
        },
        STATE: {
          type: 'select',
          datas: [
            { key: '1', title: '待提交' },
            { key: '2', title: '待接收' },
            { key: '7', title: '待检验' },
            { key: '10', title: '待签发' },
            { key: '11', title: '已签发' },
            { key: '14', title: '已打印' },
            { key: '15', title: '已下载' },
            { key: '20', title: '已完成' },
            { key: '12', title: '已退样' },
          ],
        },
        BILLDATE: { type: 'daterange' },
        AGREEDATE: { type: 'daterange' },
      };
    },
  },
  methods: {
    doSearch() {
      // rs-meta-query-panel 已同步到 QQRY DataTable
      this.host.$refs.list.query(1);
    },
    doReset() {
      var dt = this.qqryDt;
      if (dt && dt.data[0]) {
        Object.keys(dt.data[0]).forEach(function(k) {
          if (k.indexOf('_') !== 0) dt.data[0][k] = '';
        });
      }
      this.host.$refs.list.query(1);
    },
  },
};
</script>
```

---

## 四、add 页 SFC 扩展JS

路径：`@/modules/LI_M00/form.js`

```javascript
/**
 * 受理单表单页扩展
 *
 * this 上下文 (generic-form):
 *   this.ID / this.STATE / this.CUSTNAME ... - 主表字段直接读写
 *   this.$MAIN / this.MAIN                  - 主表 DataTable / 数据
 *   this.$DTSA / this.DTSA                  - 子表 DataTable / 数据
 *   this.save() / this.closePage()
 *   this.$callAction / this.$alert / this.$error / this.$confirm
 */
export default {
  computed: {
    // 暂存：STATE===1 或新建
    ISSHOWSAVE() {
      return !this.ID || this.STATE === '1' || this.STATE === 1;
    },
    // 删除：有 ID 且 STATE===1
    ISSHOWDELETE() {
      return this.ID && (this.STATE === '1' || this.STATE === 1);
    },
    // 提交：STATE===1
    ISSHOWSUBMIT() {
      return this.STATE === '1' || this.STATE === 1;
    },
    // 撤销提交：STATE===8或7
    ISSHOWRESUBMIT() {
      return this.STATE === '8' || this.STATE === '7'
        || this.STATE === 8 || this.STATE === 7;
    },
    // 表单禁用：STATE 非 1 时
    disabled() {
      return this.ID && this.STATE !== '1' && this.STATE !== 1;
    },
  },

  watch: {
    // 联动计算: CAMT = PTEMPLATECAMT * CNT + OAMT + BAMT
    CNT() { this._calcCAMT(); },
    OAMT() { this._calcCAMT(); },
    BAMT() { this._calcCAMT(); },
    PTEMPLATECAMT() { this._calcCAMT(); },
  },

  methods: {
    _calcCAMT() {
      var cnt = parseFloat(this.CNT) || 0;
      var oamt = parseFloat(this.OAMT) || 0;
      var bamt = parseFloat(this.BAMT) || 0;
      var ptplCamt = parseFloat(this.PTEMPLATECAMT) || 0;
      this.$MAIN.setValue('CAMT', (ptplCamt * cnt + oamt + bamt).toFixed(2));
    },

    // 选模板后查询项目费用
    async loadProjectFee(templateId) {
      if (!templateId) return;
      try {
        var ret = await this.$callAction({
          action: this.moduleCode + '/loadProjectFee',
          param: { TEMPLATEID: templateId },
        });
        if (ret.Data) {
          this.$MAIN.setValue('CAMT', ret.Data.CAMT || this.PTEMPLATECAMT);
          this.$MAIN.setValue('OAMT', ret.Data.OAMT || 0);
          this.$MAIN.setValue('BAMT', ret.Data.BAMT || 0);
          this._calcCAMT();
        }
      } catch (e) {
        // 降级：直接用模板 CAMT
        this.$MAIN.setValue('CAMT', this.PTEMPLATECAMT);
      }
    },

    // 表单打开时初始化
    async onShow() {
      if (this.ID) {
        // 编辑：打开数据
        this.$store.dispatch(this.storeName + '/open', {
          FilterParams: { ID: this.ID }
        });
      } else {
        // 新增：生成单据号
        this.$store.dispatch(this.storeName + '/add');
        var ret = await this.$callAction({
          action: this.moduleCode + '/getBillCode2',
        });
        if (ret.Data && ret.Data.BILLCODE) {
          this.$MAIN.setValue('BILLCODE', ret.Data.BILLCODE);
        }
      }
    },
  },
};
```

---

## 五、add 页表单字段 SFC Slot

### 替换机制说明

1. **位置**：由 `tss_resuipc.EDITSORT` 决定（m18 uiSetFull 配置字段排序）
2. **替换条件**：字段的 `EDITTYPE` 必须设为 `slot`（rs-form-cell 只在 `type==='slot'` 时渲染 slot 内容）
3. **数据流**：generic-form 传入 `:value`（字段当前值）+ 监听 `@input`（写回 DataTable），SFC 通过 props/emit 读写
4. **不要用 rs-meta-field**：会嵌套 FormItem（rs-form-cell 已含 FormItem），SFC 只渲染控件部分

### 5.1 客户选择器

**前置条件**：在 m18 uiSetFull 中将 CUSTNAME 字段的 EDITTYPE 改为 `slot`

路径：`@/modules/LI_M00/field-cust.vue`

```html
<template>
  <AutoComplete
    :value="value"
    @input="onInput"
    type="object"
    :option="option"
    :disabled="host.disabled"
  >
    <template slot="item" slot-scope="{item}">
      <div>{{ item.value.CUSTNAME }}</div>
    </template>
  </AutoComplete>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  props: {
    host: { type: Object, required: true },
    value: { type: [String, Number, Object], default: '' },
  },
  data() {
    return { option: buildAutoCompleteOption({ selType: 'cust', titleName: 'CUSTNAME' }) };
  },
  methods: {
    onInput(obj) {
      // emit input 写回 CUSTNAME 字段
      this.$emit('input', obj ? obj.CUSTNAME : '');

      // 联动写入 CUSTID
      if (obj) {
        this.host.$MAIN.setValue('CUSTID', obj.ID);
        this.loadCustDetail(obj.ID);
      } else {
        this.host.$MAIN.setValue('CUSTID', '');
      }
    },
    async loadCustDetail(custId) {
      var ret = await this.host.$callAction({
        action: this.host.moduleCode + '/call',
        param: { APICODE: 'A05', params: { ID: custId } },
      });
      if (ret.Data) {
        var d = ret.Data;
        var main = this.host.$MAIN;
        main.setValue('LINKER', d.LINKER || '');
        main.setValue('SENDNAME', d.SENDNAME || d.CUSTNAME || '');
        main.setValue('WCUSTNAME', d.WCUSTNAME || d.CUSTNAME || '');
        main.setValue('SLINKER', d.SLINKER || d.LINKER || '');
        main.setValue('ADDR', d.ADDR || '');
        main.setValue('MOBILE', d.MOBILE || d.PHONE || '');
      }
    },
  },
};
</script>
```

### 5.2 记录模板选择器

**前置条件**：PTEMPLATENAME 字段 EDITTYPE 改为 `slot`

路径：`@/modules/LI_M00/field-ptmp.vue`

```html
<template>
  <AutoComplete
    :value="value"
    @input="onInput"
    type="object"
    :option="option"
    :disabled="host.disabled"
  >
    <template slot="item" slot-scope="{item}">
      <div>{{ item.value.PTMPNAME }}</div>
    </template>
  </AutoComplete>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  props: {
    host: { type: Object, required: true },
    value: { type: [String, Number, Object], default: '' },
  },
  data() {
    return { option: buildAutoCompleteOption({ selType: 'ptmp', titleName: 'PTMPNAME' }) };
  },
  methods: {
    onInput(obj) {
      this.$emit('input', obj ? obj.PTMPNAME : '');
      if (obj) {
        // 联动写入 PTEMPLATEID
        this.host.$MAIN.setValue('PTEMPLATEID', obj.ID);
        // 调用扩展JS的 loadProjectFee
        this.host.loadProjectFee(obj.ID);
      } else {
        this.host.$MAIN.setValue('PTEMPLATEID', '');
      }
    },
  },
};
</script>
```

### 5.4 检验员/部门选择器（同理）

检验员（AEMPNAME）和检验部门（ADEPTNAME）的 SFC slot 结构与客户选择器一致，只需改 `selType` 和 `titleName`：

| 字段 | selType | titleName | updateFields 联动 |
|------|---------|-----------|------------------|
| AEMPNAME | `emp` | `EMPNAME` | `AEMPID,ID;AEMPNAME,EMPNAME` |
| ADEPTNAME | `dept` | `DEPTNAME` | `ADEPTID,ID;ADEPTNAME,DEPTNAME` |

路径：`@/modules/LI_M00/field-files.vue`

```html
<template>
  <RsUploader
    type="file"
    data-type="file"
    :options="{ multifile: false }"
    :readonly="host.disabled"
    :value="fileValue"
    @input="onFileChange"
  />
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true },
    value: { type: [String, Number], default: '' },
  },
  computed: {
    fileValue() {
      // 从 DTS 子表转换
      var dts = this.host.$DTSA;
      if (!dts || !dts.data || !dts.data.length) return '';
      return dts.data.map(function(r) { return r.FILEID; }).filter(Boolean).join(',');
    },
  },
  methods: {
    onFileChange(files) {
      // 触发 SETFILEDATA mutation 重写 DTS
      this.host.$store.commit(this.host.storeName + '/SETFILEDATA', files);
    },
  },
};
</script>
```

---

## 六、Store 扩展

路径：`@/modules/LI_M00/store.js`

```javascript
/**
 * LI_M00 Store 扩展
 *
 * 保留原 baseStore.js 中的跨模块调用和自定义查询。
 * Store03 默认 actions (query/open/add/save/delete/...) 已内置，无需重复定义。
 */
export default {
  actions: {
    // 获取单据号
    async getBillCode2({ dispatch }) {
      return await dispatch('call', {
        APICODE: 'A44',
        params: { TCODE: 'WT|%Y%m%d|' },
        apiPath: '/api/rm11/call',
      });
    },

    // 检查受理单是否已有物流记录（跨模块 R02_M07）
    async checkLogisticsExists({ dispatch }, { ACCEPTID }) {
      return await dispatch('call', {
        APICODE: 'A08',
        params: { ACCEPTID: ACCEPTID },
        apiPath: '/api/data/call/R02_M07',
      });
    },

    // 加载项目费用（跨模块 LI_PROJECT_FEE）
    async loadProjectFee({ dispatch }, { TEMPLATEID }) {
      return await dispatch('call', {
        APICODE: 'A01',
        params: { TEMPLATEID: TEMPLATEID },
        apiPath: '/api/data/call/LI_PROJECT_FEE',
      });
    },

    // 批量退样
    async batchReturn({ dispatch }, { items }) {
      return await dispatch('call', {
        APICODE: 'A51',
        params: { items: items },
      });
    },

    // 批量完成
    async batchComplete({ dispatch }, { items }) {
      return await dispatch('call', {
        APICODE: 'A23',
        params: { items: items },
      });
    },

    // 批量受理
    async batchAccept({ dispatch }, { items }) {
      return await dispatch('call', {
        APICODE: 'A14',
        params: { items: items },
      });
    },
  },

  mutations: {
    // 文件列表写入 DTS 子表
    SETFILEDATA(state, files) {
      var dts = state.dt.DTS;
      if (!dts) return;
      // 清空旧数据
      dts.data = [];
      // 填充新数据
      if (files && files.length) {
        files.forEach(function(f) {
          dts.data.push({ FILEID: f.id, FILENAME: f.name });
        });
      }
    },
  },
};
```

---

## 七、迁移对照表

| 原 r01/m05 文件 | 迁移后 | 说明 |
|----------------|--------|------|
| `router.js` | 不需要 | generic-module 路由自动注册 |
| `store.js` + `baseStore.js` | `@/modules/LI_M00/store.js` | 只保留跨模块 action + 自定义 mutation |
| `views/main.vue` | m18 配置 + `main.js` + `query-panel.vue` | 模板配置化，逻辑进扩展JS |
| `views/add.vue` | m18 配置 + `form.js` + `field-*.vue` | 表单配置化，字段替换进 SFC slot |
| `views/logistics-add.vue` | 保留为独立 SFC slot | 独立 store 组件，不迁移 |
| `mapDateTable('QQRY', [...])` | rs-meta-query-panel 自动处理 | 不需手写 |
| `mapDateTable('MAIN', [...])` | generic-form 自动映射 | this.ID/this.STATE 直接可用 |
| `ISSHOWXXX` computed | 扩展JS computed | 放入 `main.js` / `form.js` |
| 批量操作 methods | 扩展JS methods | 放入 `main.js` |
| 打印/下载 | 扩展JS methods | 放入 `main.js` |
| 联动计算 watch | 扩展JS watch | 放入 `form.js` |
| AutoComplete 选择器 | SFC field slot + rs-meta-field | 4 个 field-*.vue |
| 查询面板 | rs-meta-query-panel | 配置 + overrides |

---

## 八、迁移后目录结构

```
src/modules/LI_M00/          # SFC 扩展资产（数据库 tss_code_asset）
  main.js                    # 列表页扩展（按钮显隐 + 批量操作 + 打印下载）
  form.js                    # 表单页扩展（联动计算 + onShow + loadProjectFee）
  store.js                   # Store 扩展（跨模块 action + SETFILEDATA mutation）
  query-panel.vue            # 查询面板 SFC slot
  header-actions.vue         # 头部按钮区 SFC slot（可选）
  footer-actions.vue         # 底部按钮区 SFC slot（可选）
  form-top.vue               # 表单顶部信息栏 SFC slot
  field-cust.vue             # 客户选择器 SFC slot
  field-emp.vue              # 检验员选择器 SFC slot
  field-ptmp.vue             # 记录模板选择器 SFC slot
  field-dept.vue             # 检验部门选择器 SFC slot
  field-files.vue            # 文件上传 SFC slot
```

原 `src/pages/r01/m05/` 目录可删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M00/main` 自动注册。
