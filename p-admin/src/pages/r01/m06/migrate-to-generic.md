# r01/m06 委托管理（LI_M06）→ generic-module + SFC 扩展迁移方案

## 迁移思路

将传统"四件套"（router.js / store.js / main.vue / add.vue）拆解为：

1. **数据库配置** —— tss_module_page + tss_module_button + tss_resuipc（m18 可视化配置）
2. **SFC 扩展 JS** —— 列表批量操作、表单 Excel 导入、客户联动等业务逻辑
3. **Store 扩展** —— 跨模块调用（LI_M01 模板查询）+ 自定义 mutations（IMPORT_DTS / SET_DTSDEFAULT）
4. **SFC Slot** —— 客户选择器、Excel 导入按钮、查询面板

### 源代码核心特征

| 特征 | 实现位置 | 复杂度 |
|------|---------|--------|
| moduleCode=LI_M06, apiPath=/api/rm16/call | store.js | 基础 |
| 主表 + 子表（委托项目 DTS） | add.vue | 主子表 |
| Excel 导入子表（xlsx 解析） | add.vue + store.js IMPORT_DTS | **核心特色** |
| 子表行 getProps 动态列选择器 | add.vue getProps() | **核心特色** |
| 客户选择联动填充 8 个字段 | add.vue TCUST setter | 中等 |
| addDts 新增行带入 MAIN 默认值 | add.vue + SET_DTSDEFAULT | 中等 |
| 批量操作（折扣/费用/退费） | main.vue + store.js | 中等 |
| 审批流标准（提交/审核/撤销） | add.vue Add01 mixin | 标准 |

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置（tss_module_page）

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | COMPONENTTYPE | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|---------------|--------------|--------------|
| main | 委托管理列表 | list | /g/LI_M06/main | generic-module | A01 | - | - |
| add | 委托管理表单 | form | /g/LI_M06/add | generic-form | - | A02 | A04 |

字段说明：
- MODULECODE = `LI_M06`（两页相同）
- COMPONENTTYPE 分别为 `generic-module` / `generic-form`
- 列表页 defaultFormPageCode = `add`（点击行/新增打开表单页）

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "checkbox": true,
  "EXTENDJS": "@/modules/LI_M06/main.js",
  "SLOTS": {
    "body-query": "@/modules/LI_M06/query-panel.vue",
    "footer-action": "@/modules/LI_M06/footer-actions.vue"
  }
}
```

要点：
- `checkbox: true` 启用列表勾选，供批量折扣/批量费用使用
- `body-query` slot 承载原有的 6 字段高级查询面板（受理日期/委托单号/客户/联系人/签名状态/状态）
- `footer-action` 承载新增按钮

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "DTSPATHS": ["DTS"],
  "FORMLAYOUT": "twocolumn",
  "LABELWIDTH": 100,
  "EXTENDJS": "@/modules/LI_M06/form.js",
  "SLOTS": {
    "field:CUSTNAME": "@/modules/LI_M06/field-cust.vue",
    "dts-toolbar:DTS": "@/modules/LI_M06/dts-toolbar.vue",
    "dts-getProps:DTS": "@/modules/LI_M06/dts-getProps.js"
  }
}
```

要点：
- `DTSPATHS: ["DTS"]` 声明子表数据源（Store03.open 会自动带出）
- `field:CUSTNAME` slot 替换客户选择器，支持联动填充
- `dts-toolbar:DTS` slot 承载"导入 / 新增 / 移除"按钮组
- `dts-getProps:DTS` 是一个 JS 函数模块（非 Vue），动态返回每列的 AutoComplete option

### 1.4 按钮配置（tss_module_button）

**main 页（列表）—— footer 区域**

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND | ICON | COLOR |
|---------|---------|---------|---------|----------|----------|------|-------|
| 新增 | add | footer | - | `{"action":"openAdd"}` | - | h-icon-add | primary |
| 批量折扣 | custom | footer | A12 | `{"action":"batchDiscount","beforeAction":"promptDiscount"}` | `ISDISCOUNT` | h-icon-tag | blue |
| 批量费用 | custom | footer | A13 | `{"action":"batchFee"}` | `ISFEE` | h-iconwallet | green |
| 批量退费 | custom | footer | A14 | `{"action":"batchReFee"}` | `ISREFEE` | h-iconundo | red |

**add 页（表单）—— footer 区域**

| BTNNAME | BTNCODE | BTNAREA | APICODE | SHOWCOND | PERMCODE |
|---------|---------|---------|---------|----------|----------|
| 取消 | cancel | footer | - | - | - |
| 暂存 | save | footer | A04 | `ISSHOWSAVE` | LI_M06/A03 |
| 删除 | delete | footer | A07 | `ISSHOWDELETE` | LIB_M04/A07 |
| 提交 | submit | footer | A05 | `ISSHOWSUBMIT` | LI_M06/A05 |
| 撤销提交 | reSubmit | footer | A06 | `ISSHOWRESUBMIT` | LI_M06/A06 |
| 审核 | check | footer | - | `ISSHOWCHECK` | LI_M06/A08 |
| 撤销审核 | reCheck | footer | - | `ISSHOWRECHECK` | LI_M06/A09 |

说明：
- 审核/撤销审核走自定义 `check` action（store.js 中重写，处理多路径 APIPARAM）
- 批量折扣的 `promptDiscount` beforeAction 弹出折扣输入框（见 main.js）

---

## 二、SFC 在线资产 —— store.js（Store 扩展）

路径：`@/modules/LI_M06/store.js`

保留原 store.js 的自定义 actions / mutations，Store03 默认 CRUD 不重复定义。

```javascript
/**
 * LI_M06 委托管理 Store 扩展
 *
 * apiPath 已在 createStore 中配置为 /api/rm16/call
 * Store03 默认 query/open/add/save/delete/submit/reSubmit/reCheck/verify 已内置
 */
import { dateToString } from 'rs-vcore/utils/Date';

export default {
  actions: {
    /**
     * 新建主表 + 空子表
     * 原始 store.js add() 逻辑：INIT + ADD MAIN（STATE=1, VERSION=1）
     */
    add({ commit }) {
      commit('INIT', { paths: ['MAIN', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: { STATE: 1, VERSION: 1 } });
    },

    /**
     * Excel 导入子表入口（由 form.js 的 onExcelChange 触发）
     */
    import({ commit }, { items, columns }) {
      commit('IMPORT_DTS', { items, columns });
    },

    /**
     * 启用/禁用（A07）
     * 原 endisable action —— 将 ISUSE 反转后 call A07
     */
    async endisable({ commit, dispatch, rootState }, { item }) {
      commit('SET_ENDISABLE', { item });
      let ret = await dispatch('call', {
        APICODE: 'A07',
        params: { UPDATE: rootState.storeHelper.getTable('UPDATE').getXML() },
      });
      if (ret.length > 0) {
        for (let a in ret[0]) {
          item[a] = ret[0][a];
        }
      }
    },

    /**
     * 更新 TPMDATA（A08）
     */
    async updateTPMDATA({ commit, dispatch, rootState }, { item }) {
      commit('SETTPMDATA', { item });
      let ret = await dispatch('call', {
        APICODE: 'A08',
        params: { UPDATE: rootState.storeHelper.getTable('UPDATE').getXML() },
      });
      if (ret.length > 0) {
        for (let a in ret[0]) {
          item[a] = ret[0][a];
        }
      }
    },

    /**
     * 跨模块：查询 LI_M01/A06 记录模板（querySel）
     * 原始 store.js 中的 querySel action，走 /api/data/call/LI_M01/A06/
     */
    async querySel({ state, commit }, { INPUT }) {
      let ret = await db.postData({
        api: '/api/data/call/LI_M01/A06/',
        params: {
          PageSize: 20,
          PageIndex: 1,
          FilterParams: { INPUT },
        },
      });
      commit(Constants.M_INITDATA, {
        path: 'SEL',
        data: ret.Items || [],
      });
    },

    /**
     * 批量折扣（A12）—— 更新 AMT + DISCOUNT 字段
     */
    async batchDiscount({ dispatch }, { items, DISCOUNT }) {
      await dispatch('batch', {
        APICODE: 'A12',
        items,
        updateFields: ['AMT', 'DISCOUNT'],
        params: { DISCOUNT },
      });
    },

    /**
     * 批量费用（A13）—— 更新 RAMT + CHARGEID + CHARGER + CHARGETIME
     */
    async batchFee({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A13',
        items,
        updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'],
        params: {},
      });
    },

    /**
     * 批量退费（A14）—— 同 batchFee 的 APCODE
     */
    async batchReFee({ dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A14',
        items,
        updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'],
        params: {},
      });
    },

    /**
     * 受理打印（A10）—— 不更新字段，仅返回打印数据
     */
    async aprint({ dispatch }, { items }) {
      return await dispatch('batch', { APICODE: 'A10', items });
    },

    /**
     * 审核（重写 Store03.check）—— 原始 LI_M06 check 走多路径提交
     * APIPARAM 配置的路径（如 'MAIN,DTS'）会被拆分，每个路径生成独立 XML
     */
    async check({ commit, dispatch, rootState }) {
      let storeHelper = rootState.storeHelper;
      let row = storeHelper.moudle.getApi('check');
      let modeCode = storeHelper.moudle.getModCode();
      let { APIPARAM, APICODE, PATHNAME } = row;
      let paths = APIPARAM.split(',');
      let params = {};
      paths.forEach((path) => {
        if (path !== PATHNAME) {
          commit('SET_ENTRYNUM', { path });
        }
        params[path] = storeHelper.getTable(path).getXML();
      });
      let ret = await db.postData({
        api: `/api/rm16/call/${modeCode}/${APICODE}/`,
        params,
      });
      commit(Constants.M_BATCHSETDATA, { data: ret });
    },
  },

  mutations: {
    /**
     * 启用/禁用切换 —— 写入 UPDATE 临时 DataTable
     */
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },

    /**
     * TPMDATA 字段更新
     */
    SETTPMDATA(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('TPMDATA', item.TPMDATA);
      UPDATE.setValue('ID', item.ID);
    },

    /**
     * 设置经办人信息（CHARGEID/CHARGER/CHARGETIME）
     * 表单保存前由 form.js mySave() 触发
     */
    SET_CHARGEDATA(state, { userInfo }) {
      let MAIN = storeHelper.getTable('MAIN');
      MAIN.setValue('CHARGEID', userInfo.ID);
      MAIN.setValue('CHARGER', userInfo.NICKNAME);
      MAIN.setValue('CHARGETIME', dateToString(new Date(), 'yyyy-MM-dd hh:mm:ss'));
    },

    /**
     * 将主表 4 个字段同步到子表"新增行"的默认值
     * addDts('DTS') 触发，配合 form.js 的 addDts 方法
     */
    SET_DTSDEFAULT(state) {
      let MAIN = storeHelper.getTable('MAIN');
      let DTS = storeHelper.getTable('DTS');
      DTS.setValue('SLINKER', MAIN.getValue('MOBILE'));
      DTS.setValue('SENDNAME', MAIN.getValue('LINKER'));
      DTS.setValue('WCUSTNAME', MAIN.getValue('CUSTNAME'));
      DTS.setValue('SENDDATE', MAIN.getValue('BILLDATE'));
    },

    /**
     * Excel 导入子表（核心特色）
     *
     * 数据流：
     *   add.vue onChange(file)
     *     → xlsxRead → sheet_to_json
     *     → commit IMPORT_DTS { items, columns }
     *
     * columns 是 rs-tableEdit.columns（包含 key + title）
     * 按 title 匹配 Excel 表头，写入 key 字段
     * 同时带入主表 4 个默认字段（SLINKER/SENDNAME/WCUSTNAME/SENDDATE）
     */
    IMPORT_DTS(state, { items, columns }) {
      let DTS = storeHelper.getTable('DTS');
      let MAIN = storeHelper.getTable('MAIN');
      items.forEach((item) => {
        let row = {};
        columns.forEach((column) => {
          if (item[column.title] != null) {
            row[column.key] = item[column.title];
          }
        });
        // 主表字段带入（与 addDts 默认值一致）
        row['SLINKER'] = MAIN.getValue('MOBILE');
        row['SENDNAME'] = MAIN.getValue('LINKER');
        row['WCUSTNAME'] = MAIN.getValue('CUSTNAME');
        // SENDDATE 只取日期部分（原代码 .split(' ')[0]）
        let billDate = MAIN.getValue('BILLDATE') || '';
        row['SENDDATE'] = billDate.split(' ')[0];
        DTS.add(row);
      });
    },
  },
};
```

要点说明：
- `IMPORT_DTS` 是 m06 的核心 mutation，接收 Excel 二维数组 + 列定义，按列标题映射字段名
- `SET_DTSDEFAULT` 与 `addDts` 配合：手动新增单行时带入主表默认值
- `check` action 重写 Store03 默认实现，支持多路径 XML 提交（原始 add.vue 中的 check 逻辑）
- 所有 actions 走 apiPath `/api/rm16/call`（在 m18 模块配置中统一设置）

---

## 三、SFC 在线资产 —— main.js（列表扩展）

路径：`@/modules/LI_M06/main.js`

```javascript
/**
 * 委托管理列表页扩展
 *
 * this 上下文（generic-module 实例）：
 *   this.moduleCode === 'LI_M06'
 *   this.storeName === 'r01/m06' （或 createStore 自动生成的命名空间）
 *   this.$refs.list               —— RsTableList 组件
 *   this.selectedRows             —— 当前勾选的行
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
 *
 * 原始 main.vue 中的逻辑全部迁移到这里。
 */
export default {
  computed: {
    /**
     * 是否可批量费用：所有选中行都没有 CHARGEID（未收费）
     * 原 main.vue ISFEE
     */
    ISFEE() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function (r) { return !r.CHARGEID; });
    },

    /**
     * 是否可批量退费：所有选中行都有 CHARGEID（已收费）
     * 原 main.vue ISREFEE
     */
    ISREFEE() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function (r) { return !!r.CHARGEID; });
    },

    /**
     * 是否可批量折扣：只要有选中行
     * 原 main.vue ISDISCOUNT
     */
    ISDISCOUNT() {
      return (this.selectedRows || []).length > 0;
    },
  },

  methods: {
    /**
     * 打开新增表单弹窗
     * 原 main.vue add() 方法
     */
    openAdd() {
      // generic-module 内置打开 defaultFormPageCode 表单页（无 ID）
      this.openFormPage();
    },

    /**
     * 批量折扣 —— 弹出折扣输入框
     * beforeAction 钩子，返回 false 中断主流程
     */
    async promptDiscount(btn) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选记录');
        return false;
      }
      // 简单实现：用 window.prompt（生产建议改为 Modal 弹窗）
      var input = window.prompt('请输入折扣率（0~1）', '1');
      if (input == null) return false;
      var discount = parseFloat(input);
      if (isNaN(discount) || discount <= 0 || discount > 1) {
        this.$error('折扣率必须是 0~1 之间的数字');
        return false;
      }
      btn._extParams = { DISCOUNT: discount };
      return true;
    },

    async batchDiscount(btn) {
      var rows = this.selectedRows || [];
      var DISCOUNT = (btn._extParams && btn._extParams.DISCOUNT) || 1;
      await this.$callAction({
        action: this.moduleCode + '/batchDiscount',
        param: { items: rows, DISCOUNT: DISCOUNT },
        successText: '操作成功',
      });
      this.$refs.list.query(1);
    },

    async batchFee(btn) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchFee',
        param: { items: rows },
        successText: '操作成功',
      });
      this.$refs.list.query(1);
    },

    async batchReFee(btn) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReFee',
        param: { items: rows },
        successText: '操作成功',
      });
      this.$refs.list.query(1);
    },
  },
};
```

---

## 四、SFC 在线资产 —— form.js（表单扩展）

路径：`@/modules/LI_M06/form.js`

```javascript
/**
 * 委托管理表单页扩展
 *
 * this 上下文（generic-form 实例）：
 *   this.ID / this.STATE / this.CUSTNAME / this.MOBILE ...    —— 主表字段（mapDateTable 自动映射）
 *   this.$MAIN / this.MAIN                                    —— 主表 DataTable / 数据数组
 *   this.$DTS / this.DTS                                      —— 子表 DataTable / 数据数组
 *   this.moduleCode / this.storeName
 *   this.save() / this.closePage()
 *   this.$callAction / this.$alert / this.$error / this.$confirm
 *
 * 原 add.vue 中的 ISSHOWXXX / 联动 / Excel 导入 / addDts 全部迁移到这里。
 */
import { read as xlsxRead, utils as xlsxUtils } from 'xlsx';

export default {
  computed: {
    /**
     * 是否新建（用于控制 Excel 导入按钮显隐）
     */
    ISNEW() {
      return !this.ID;
    },

    // ====== 按钮 ISSHOWXXX（与 Add01 mixin 等价，由 generic-form 框架消费）======
    ISSHOWSAVE() {
      return !this.STATE || this.STATE === 1 || this.STATE === '1';
    },
    ISSHOWDELETE() {
      return this.ID && (this.STATE === 1 || this.STATE === '1');
    },
    ISSHOWSUBMIT() {
      return !this.STATE || this.STATE === 1 || this.STATE === '1';
    },
    ISSHOWRESUBMIT() {
      return this.ID && (this.STATE === 2 || this.STATE === '2');
    },
    ISSHOWCHECK() {
      return this.ID && (this.STATE === 2 || this.STATE === '2');
    },
    ISSHOWRECHECK() {
      return this.ID && [3, 5, 19].indexOf(Number(this.STATE)) >= 0;
    },

    /**
     * 表单禁用：非待提交状态
     */
    disabled() {
      return this.ID && Number(this.STATE) !== 1;
    },

    /**
     * 客户对象（用于 CUSTNAME slot 内 AutoComplete 的 v-model）
     * 由 field-cust.vue 通过 host.TCUST 读取
     */
    TCUST: {
      get() {
        if (!this.CUSTID) return null;
        return { ID: this.CUSTID, CUSTNAME: this.CUSTNAME };
      },
      set(v) {
        v = v || {};
        // 写入主表 CUSTID / CUSTNAME
        this.$MAIN.setValue('CUSTID', v.ID);
        this.$MAIN.setValue('CUSTNAME', v.CUSTNAME);
        // 联动填充主表其他字段
        this.$MAIN.setValue('LINKER', v.LINKER);
        this.$MAIN.setValue('MOBILE', v.MOBILE);
        this.$MAIN.setValue('ADDR', v.ADDR);
        // 同步到子表默认值（立即生效到新增的 DTS 行）
        this.$MAIN.setValue('SLINKER', v.MOBILE);
        // 触发 SET_DTSDEFAULT 等效逻辑（form.js 直接写主表）
      },
    },
  },

  methods: {
    /**
     * 表单保存前注入经办人信息
     * 原 add.vue mySave() 方法
     */
    mySave() {
      this.$store.commit(this.storeName + '/SET_CHARGEDATA', {
        userInfo: this.$store.state.user.userInfo,
      });
      this.save();
    },

    // ====== Excel 导入子表（核心特色）======

    /**
     * Excel 文件选择回调
     * 原始 add.vue onChange(file) 方法
     *
     * 步骤：
     *   1. FileReader 读取二进制
     *   2. xlsx.read 解析工作簿
     *   3. sheet_to_json 转换为对象数组
     *   4. commit IMPORT_DTS 写入 DTS DataTable
     *
     * @param {File} file —— input[type=file].files[0]
     */
    async onExcelChange(fileEvent) {
      var file = fileEvent.target.files[0];
      if (!file) return;
      var dataBinary = await this._readFile(file);
      var workBook = xlsxRead(dataBinary, { type: 'binary', cellDates: true });
      var workSheet = workBook.Sheets[workBook.SheetNames[0]];
      var rows = xlsxUtils.sheet_to_json(workSheet);
      // 列定义从 rs-tableEdit 组件的 columns 取（包含 key + title）
      var columns = this.$refs.DTS.columns;
      this.$store.commit(this.storeName + '/IMPORT_DTS', {
        items: rows,
        columns: columns,
      });
      this.$alert('成功导入 ' + rows.length + ' 行');
    },

    /**
     * FileReader 包装 —— 读取为 binary string
     * 原始 add.vue readFile(file) 方法
     */
    _readFile(file) {
      return new Promise(function (resolve) {
        var reader = new FileReader();
        reader.readAsBinaryString(file);
        reader.onload = function (ev) {
          resolve(ev.target.result);
        };
      });
    },

    // ====== addDts 新增行带入默认值（核心特色）======

    /**
     * 子表新增行，带入主表 4 个字段
     * 原始 add.vue addDts(path) 方法
     *
     * 覆盖 generic-form 的默认 addDts 行为：
     *   - 默认：仅触发 ADD mutation（空行）
     *   - 这里：在 item 中预设 SLINKER/SENDNAME/WCUSTNAME/SENDDATE
     *
     * 同步逻辑与 IMPORT_DTS 的带入字段保持一致
     */
    addDts(path) {
      var billDate = this.BILLDATE || '';
      var item = {
        SLINKER: this.MOBILE,
        SENDNAME: this.LINKER,
        WCUSTNAME: this.CUSTNAME,
        SENDDATE: billDate.split(' ')[0],
      };
      this.$store.commit(this.storeName + '/ADD', { path: path, item: item });
    },

    /**
     * 子表移除行
     * 原 Add01 mixin removeDts —— 保持不变
     */
    removeDts(path, table) {
      if (!table.currentRow) return;
      this.$store.commit(this.storeName + '/DEL', {
        path: path,
        item: table.currentRow,
      });
    },

    /**
     * 记录当前选中行索引（供 getProps 使用）
     * 原始 add.vue onRowClick(row, rowIndex)
     */
    onRowClick(row, rowIndex) {
      this._dtsRowIndex = rowIndex;
      this._dtsCurrentRow = row;
    },
  },
};
```

---

## 五、SFC Slot —— 客户选择器（联动填充）

路径：`@/modules/LI_M06/field-cust.vue`

```html
<template>
  <AutoComplete
    :value="displayValue"
    @input="onInput"
    type="object"
    :option="option"
    :disabled="host.disabled"
  >
    <template slot="item" slot-scope="{ item }">
      <div>{{ item.value.CUSTNAME }}</div>
    </template>
  </AutoComplete>
</template>

<script>
import { buildAutoCompleteOption } from '@/utils/selRegistry';

export default {
  name: 'li-m06-field-cust',
  props: {
    host: { type: Object, required: true },
    value: { type: [String, Number, Object], default: '' },
  },
  data() {
    return {
      option: buildAutoCompleteOption({
        selType: 'cust',
        titleName: 'CUSTNAME',
        keyName: 'ID',
      }),
    };
  },
  computed: {
    /**
     * 显示值：主表 CUSTID + CUSTNAME 组合对象
     * 原 add.vue TCUST get()
     */
    displayValue() {
      if (!this.host.CUSTID) return null;
      return { ID: this.host.CUSTID, CUSTNAME: this.host.CUSTNAME };
    },
  },
  methods: {
    /**
     * 选中后联动填充 8 个字段
     * 原 add.vue TCUST set(v)
     *
     * 联动字段：
     *   主表：CUSTID / CUSTNAME / LINKER / MOBILE / ADDR / SLINKER
     *   子表新增默认值：SENDNAME=LINKER, WCUSTNAME=CUSTNAME, SLINKER=MOBILE
     */
    onInput(obj) {
      obj = obj || {};
      // 写回当前 slot 绑定的 CUSTNAME 字段
      this.$emit('input', obj.CUSTNAME || '');

      var main = this.host.$MAIN;
      main.setValue('CUSTID', obj.ID || '');
      main.setValue('CUSTNAME', obj.CUSTNAME || '');
      main.setValue('LINKER', obj.LINKER || '');
      main.setValue('SLINKER', obj.MOBILE || '');   // 发送人电话 = 客户手机
      main.setValue('ADDR', obj.ADDR || '');
      main.setValue('MOBILE', obj.MOBILE || '');
      // EMAIL 也带入（原 set 中有）
      if (obj.EMAIL != null) {
        main.setValue('EMAIL', obj.EMAIL);
      }
    },
  },
};
</script>
```

---

## 六、SFC Slot —— 子表工具栏（Excel 导入按钮）

路径：`@/modules/LI_M06/dts-toolbar.vue`

```html
<template>
  <div class="dts-toolbar">
    <label class="excel-upload-wrap">
      <input
        class="excel-upload-input"
        type="file"
        @change="onExcelChange"
        accept=".csv,.xlsx,.xls"
      />
      <Button color="primary" icon="h-icon-plus" size="s">导入</Button>
    </label>
    <Button color="primary" icon="h-icon-plus" size="s" @click="onAdd">
      新增
    </Button>
    <Button color="primary" icon="h-icon-minus" size="s" @click="onRemove">
      移除
    </Button>
  </div>
</template>

<script>
export default {
  name: 'li-m06-dts-toolbar',
  props: {
    host: { type: Object, required: true },   // generic-form 实例
    path: { type: String, default: 'DTS' },
    tableRef: { type: String, default: 'DTS' },
  },
  methods: {
    onExcelChange(fileEvent) {
      // 调用 form.js 的 onExcelChange
      this.host.onExcelChange(fileEvent);
    },
    onAdd() {
      // 调用 form.js 的 addDts（已覆盖默认行为，带入主表默认值）
      this.host.addDts(this.path);
    },
    onRemove() {
      var tableComp = this.host.$refs[this.tableRef];
      this.host.removeDts(this.path, tableComp);
    },
  },
};
</script>

<style scoped>
.excel-upload-wrap {
  position: relative;
  display: inline-block;
  overflow: hidden;
}
.excel-upload-input {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  cursor: pointer;
  z-index: 5;
}
</style>
```

---

## 七、SFC Slot —— 子表 getProps 动态配置（核心特色）

路径：`@/modules/LI_M06/dts-getProps.js`

**非 Vue 组件**，而是一个普通 JS 函数模块，由 generic-form 在渲染 rs-table-edit 时调用。

原始 add.vue 的 `getProps(key)` 实现：根据列名返回不同的 AutoComplete option。

```javascript
/**
 * 子表动态列选择器配置
 *
 * 原始 add.vue getProps(key) 方法的等价迁移
 *
 * 使用方式（generic-form 中）：
 *   <rs-table-edit :getProps="(key) => dtsGetProps(key, host)" />
 *
 * @param {String} key           —— 列字段名（DEPTNAME/AEMPNAME/PTEMPLATENAME/...）
 * @param {Object} host          —— generic-form 实例（提供 deptParam/empParam/ptmpSel 等）
 * @param {Number} rowIndex      —— 当前行索引（用于 PTEMPLATENAME 联动 ADEPTID）
 * @returns {Object} cellProps 配置
 */
export default function getProps(key, host, rowIndex) {
  if (key === 'DEPTNAME') {
    // 部门选择器 —— 使用 SelStore 的 deptParam
    return {
      cellProps: { option: host.deptParam },
    };
  }
  if (key === 'AEMPNAME') {
    // 员工选择器 —— 使用 SelStore 的 empParam
    return {
      cellProps: { option: host.empParam },
    };
  }
  if (key === 'PTEMPLATENAME') {
    // 记录模板选择器 —— 动态查询，按当前行 ADEPTID 过滤
    return {
      cellProps: {
        option: {
          loadData: function (INPUT, callback) {
            host.ptmpSel(INPUT, callback, rowIndex);
          },
          keyName: 'ID',
          titleName: 'DOCTITLE',
        },
      },
    };
  }
  // 其他列：默认无特殊配置
  return {};
}
```

### ptmpSel 的实现位置

`ptmpSel` 是 PTEMPLATENAME 列的动态数据加载函数，需要访问当前行的 `ADEPTID`。建议放在 form.js 中：

```javascript
// form.js 中补充
methods: {
  /**
   * 记录模板动态查询
   * 原始 add.vue ptmpSel(INPUT, callback)
   *
   * 特殊逻辑：如果当前输入值 === 已选 PTEMPLATENAME，则清空 INPUT（防止只查自身）
   * 查询参数 DEPTID 来自当前行（this.DTS[this._dtsRowIndex]['ADEPTID']）
   */
  async ptmpSel(INPUT, callback, rowIndex) {
    var currentRow = (this.DTS || [])[rowIndex] || {};
    if (currentRow.PTEMPLATENAME === INPUT) {
      INPUT = '';
    }
    var ret = await this.$callAction({
      action: this.moduleCode + '/ptmpSel',
      param: {
        INPUT: INPUT,
        DEPTID: currentRow.ADEPTID,
      },
      isBusy: false,
    });
    callback(ret);
  },
}
```

### generic-form 中如何接入 getProps

generic-form.vue 源码（框架已有，无需修改）：

```html
<rs-table-edit
  border
  ref="DTS"
  :path="$DTS"
  :datas="DTS"
  :getProps="(key) => dtsGetProps(key)"
  @on-row-click="onDtsRowClick"
></rs-table-edit>
```

```javascript
// generic-form 的 computed
computed: {
  dtsGetProps() {
    // 从 SLOTS['dts-getProps:DTS'] 加载的函数模块
    if (this._loadedDtsGetProps) return this._loadedDtsGetProps;
    return (key) => {
      const fn = this.slotComponents['dts-getProps:DTS'];
      if (typeof fn === 'function') {
        return fn(key, this, this._dtsRowIndex);
      }
      return {};
    };
  },
}
```

> **说明**：`dts-getProps:DTS` 是约定的 slot 类型，值是 JS 模块路径（非 Vue 组件）。generic-form 在加载时通过 `loadExtendMixin` 识别 `dts-getProps:` 前缀，用 `require()` 加载并缓存为函数引用。

---

## 八、SFC Slot —— 查询面板

路径：`@/modules/LI_M06/query-panel.vue`

```html
<template>
  <div class="query-panel" v-if="qqryDt">
    <Row :space="9">
      <Cell width="6">
        <div class="rr-flex-row">
          <label class="rr-justify" style="width:60px">受理日期</label>
          <DateRangePicker class="rr-flex-1" v-model="BILLDATE" />
        </div>
      </Cell>
      <Cell width="6">
        <div class="rr-flex-row">
          <label class="rr-justify" style="width:60px">委托单号</label>
          <input type="text" class="rr-flex-1" v-model="BILLCODE" />
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
          <label class="rr-justify" style="width:60px">签名状态</label>
          <Select class="rr-flex-1" v-model="SIGNSTATE" :datas="signStates" />
        </div>
      </Cell>
      <Cell width="6">
        <div class="rr-flex-row">
          <label class="rr-justify" style="width:60px">状态</label>
          <Select class="rr-flex-1" v-model="STATE" :datas="billStates" />
        </div>
      </Cell>
      <Cell width="6">
        <div style="text-align:right;padding-right:10px">
          <Button class="ml5" color="primary" @click="doSearch">查询</Button>
          <Button class="ml5" @click="doReset">重置</Button>
        </div>
      </Cell>
    </Row>
  </div>
</template>

<script>
export default {
  name: 'li-m06-query-panel',
  props: {
    host: { type: Object, required: true },
  },
  data() {
    return {
      // 原 main.vue 的 param2
      signStates: [
        { title: '待签名', key: 'dqm' },
        { title: '已签名', key: 'yqm' },
      ],
      // 原 main.vue 的 param
      billStates: [
        { title: '待提交', key: 1 },
        { title: '待审批', key: 2 },
        { title: '已审批', key: 3 },
      ],
    };
  },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    },
    // v-model 代理：读写 QQRY DataTable
    BILLDATE: this._proxyField('BILLDATE'),
    BILLCODE: this._proxyField('BILLCODE'),
    CUSTNAME: this._proxyField('CUSTNAME'),
    LINKER: this._proxyField('LINKER'),
    SIGNSTATE: this._proxyField('SIGNSTATE'),
    STATE: this._proxyField('STATE'),
  },
  methods: {
    _proxyField(key) {
      return {
        get() {
          var dt = this.qqryDt;
          return (dt && dt.data[0] && dt.data[0][key]) || '';
        },
        set(v) {
          var dt = this.qqryDt;
          if (dt && dt.data[0]) dt.setValue(key, v);
        },
      };
    },
    doSearch() {
      this.host.$refs.list.query(1);
    },
    doReset() {
      var dt = this.qqryDt;
      if (dt && dt.data[0]) {
        ['BILLDATE', 'BILLCODE', 'CUSTNAME', 'LINKER', 'SIGNSTATE', 'STATE'].forEach((k) => {
          dt.setValue(k, '');
        });
      }
      this.host.$refs.list.query(1);
    },
  },
};
</script>
```

> **简化建议**：也可用 `<rs-meta-query-panel>` 替代手写查询面板，配合 `tss_resuipc.QUERYMODE` 配置（range 用于日期，eq 用于下拉）。此处保留原样是为了完全对齐原始 UI。

---

## 九、字段配置（UI 设置 tss_resuipc）

### 9.1 主表字段（MAIN）

| FIELDNAME | LABELNAME | EDITTYPE | SELECTDATA | LISTSORT | EDITSORT |
|-----------|-----------|----------|------------|----------|----------|
| BILLCODE | 委托单号 | text | - | 1 | 1 |
| BILLDATE | 受理日期 | date | - | 2 | 2 |
| CUSTNAME | 客户 | slot | - | 3 | 3 |
| LINKER | 联系人 | text | - | 4 | 4 |
| MOBILE | 手机 | text | - | 5 | 5 |
| ADDR | 地址 | text | - | - | 6 |
| DEPTID | 部门ID | hidden | - | - | - |
| DEPTNAME | 部门 | text | - | 6 | 7 |
| SIGNSTATE | 签名状态 | select | `D06_SIGNSTATE` | 7 | - |
| SIGNIMG | 签名图片 | image | - | - | - |
| STATE | 状态 | select | `D06_STATE` | 8 | - |
| CHARGEID | 经办人ID | hidden | - | - | - |
| CHARGER | 经办人 | text | - | 9 | - |
| CHARGETIME | 经办时间 | datetime | - | 10 | - |
| VERSION | 版本 | hidden | - | - | - |

### 9.2 子表字段（DTS —— 委托项目）

| FIELDNAME | LABELNAME | EDITTYPE | SELECTDATA | EDITSORT |
|-----------|-----------|----------|------------|----------|
| DEPTNAME | 部门 | autocomplete | `{"selType":"dept","keyName":"ID","titleName":"DEPTNAME"}` | 1 |
| AEMPNAME | 检验员 | autocomplete | `{"selType":"emp","keyName":"ID","titleName":"EMPNAME"}` | 2 |
| PTEMPLATENAME | 记录模板 | autocomplete | `{"selType":"ptmp","keyName":"ID","titleName":"DOCTITLE"}` | 3 |
| SLINKER | 联系电话 | text | - | 4 |
| SENDNAME | 联系人 | text | - | 5 |
| WCUSTNAME | 委托方 | text | - | 6 |
| SENDDATE | 委托日期 | date | - | 7 |

**重点**：子表 3 个 autocomplete 列的 SELECTDATA 只声明元数据，**实际 option 在运行时由 getProps 提供**（包含动态联动逻辑，如 PTEMPLATENAME 按 ADEPTID 过滤）。

### 9.3 查询字段（QQRY）

| FIELDNAME | LABELNAME | QUERYMODE | QUERYSORT |
|-----------|-----------|-----------|-----------|
| BILLDATE | 受理日期 | range | 1 |
| BILLCODE | 委托单号 | like | 2 |
| CUSTNAME | 客户 | like | 3 |
| LINKER | 联系人 | like | 4 |
| SIGNSTATE | 签名状态 | eq | 5 |
| STATE | 状态 | eq | 6 |

---

## 十、迁移对照表

| 原 r01/m06 文件/逻辑 | 迁移后位置 | 类型 | 说明 |
|---------------------|-----------|------|------|
| `router.js` | 删除 | - | generic-module 路由自动注册 |
| `index.js` | 删除 | - | 不再需要入口文件 |
| `store.js` createStore 主体 | m18 模块配置 | 数据库 | moduleCode/apiPath/paths 配置化 |
| `store.js` 自定义 actions | `@/modules/LI_M06/store.js` | SFC JS | add/import/endisable/updateTPMDATA/querySel/batchDiscount/batchFee/batchReFee/aprint/check |
| `store.js` mutations | `@/modules/LI_M06/store.js` | SFC JS | SET_ENDISABLE / SETTPMDATA / SET_CHARGEDATA / SET_DTSDEFAULT / IMPORT_DTS |
| `views/main.vue` 模板 | m18 PAGECONFIG + footer-action slot | 数据库 + SFC | 模板配置化 |
| `views/main.vue` datas（面包屑） | m18 PAGENAME | 数据库 | 自动生成 |
| `views/main.vue` param/param2（状态下拉） | tss_resuipc SELECTDATA + 字典 | 数据库 | 走数据字典 D06_STATE / D06_SIGNSTATE |
| `views/main.vue` ISFEE/ISREFEE/ISDISCOUNT | `main.js` computed | SFC JS | 按钮显隐逻辑 |
| `views/main.vue` batchFee/batchReFee/batchDiscount | `main.js` methods | SFC JS | 批量操作 |
| `views/main.vue` advQuery | generic-module 内置 | 框架 | 无需迁移 |
| `views/main.vue` showQuery 切换 | generic-module 内置 | 框架 | body-query slot 自动折叠 |
| `views/add.vue` rs-form-edit | generic-form 自动渲染 | 框架 | 按 EDITSORT 排序 |
| `views/add.vue` TCUST computed（客户联动） | `field-cust.vue` + `form.js` TCUST | SFC Vue + JS | 拆为 slot + 扩展 |
| `views/add.vue` Excel 导入 onChange | `form.js` onExcelChange | SFC JS | xlsx 解析 + IMPORT_DTS |
| `views/add.vue` readFile | `form.js` _readFile | SFC JS | FileReader 封装 |
| `views/add.vue` addDts（带默认值） | `form.js` addDts | SFC JS | SLINKER/SENDNAME/WCUSTNAME/SENDDATE |
| `views/add.vue` getProps（动态列选择器） | `dts-getProps.js` | SFC JS | DEPTNAME/AEMPNAME/PTEMPLATENAME 三列 |
| `views/add.vue` ptmpSel（模板动态查询） | `form.js` ptmpSel | SFC JS | 按当前行 ADEPTID 过滤 |
| `views/add.vue` onRowClick | `form.js` onRowClick | SFC JS | 记录 rowIndex 供 getProps 使用 |
| `views/add.vue` mySave（注入经办人） | `form.js` mySave | SFC JS | SET_CHARGEDATA + save |
| `views/add.vue` mixins: [Add01] | generic-form 内置 + `form.js` | 框架 | ISSHOWXXX / save / submit / check |
| `views/add.vue` mixins: [Sel01] | SelStore 全局注册 | 框架 | deptParam/empParam/custParam 自动可用 |
| `Add01.js` 全部按钮逻辑 | generic-form + tss_module_button | 框架 + 数据库 | 配置化 |
| Excel 导入按钮（upload label） | `dts-toolbar.vue` | SFC Vue | 子表工具栏 slot |
| 子表新增/移除按钮 | `dts-toolbar.vue` | SFC Vue | 同上 |
| 权限点 `v-per="'LI_M06/AXX'"` | tss_module_button.PERMCODE | 数据库 | 按钮级权限 |

---

## 十一、迁移后目录结构

```
src/modules/LI_M06/                # SFC 扩展资产（数据库 tss_code_asset, MODULEPATH 形式）
  store.js                         # Store 扩展（自定义 actions + 5 个 mutations）
  main.js                          # 列表页扩展（ISFEE/ISREFEE/ISDISCOUNT + 3 个批量 action）
  form.js                          # 表单页扩展（ISSHOWXXX + Excel 导入 + addDts + ptmpSel）
  field-cust.vue                   # 客户选择器 SFC slot（联动填充 8 字段）
  dts-toolbar.vue                  # 子表工具栏 SFC slot（导入/新增/移除）
  dts-getProps.js                  # 子表动态列选择器配置（非 Vue 组件，纯函数模块）
  query-panel.vue                  # 高级查询面板 SFC slot（6 字段）
```

原 `src/pages/r01/m06/` 目录可删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M06/main` 自动注册。

---

## 十二、关键迁移风险与注意事项

### 12.1 Excel 导入的 xlsx 依赖

- 原代码使用 `import { read as xlsxRead, utils as xlsxUtils } from 'xlsx'`
- SFC 在线资产环境（`window.__SFC_MODULES__`）需要预暴露 `xlsx`
- **方案**：在 `module-bridge.js` 中新增 `window.__SFC_MODULES__['xlsx'] = require('xlsx')`
- 或在 form.js 中改用动态 require：`var XLSX = require('xlsx')`（需 bridge 支持）

### 12.2 子表 getProps 的接入点

- 原始 rs-table-edit 通过 props 接收 `getProps` 函数
- generic-form 需新增 `dts-getProps:{PATH}` slot 类型识别
- **框架改动点**：`generic-form.vue` 的 `loadSlotComponents()` 需识别 `dts-getProps:` 前缀，缓存为函数而非组件

### 12.3 SelStore 的全局注入

- 原 add.vue 通过 `mixins: [Sel01]` 获取 deptParam/empParam/custParam
- 迁移后 form.js 需要访问这些 param 对象
- **方案**：generic-form 已注入全局 SelStore（通过 `app` 模块），form.js 中直接 `this.$store.state.app.selStore` 获取
- 或者在 form.js 中重新构建：

```javascript
data() {
  return {
    deptParam: { loadData: this._deptSel, keyName: 'ID', titleName: 'DEPTNAME' },
    empParam: { loadData: this._empSel, keyName: 'ID', titleName: 'EMPNAME' },
  };
}
```

### 12.4 check action 的多路径提交

- 原 store.js 的 check action 读取 `APIPARAM`（如 `'MAIN,DTS'`），拆分后每个路径生成独立 XML
- Store03 默认的 check 实现**不支持**这种模式
- **必须保留**在 store.js 扩展中重写 check action

### 12.5 debugger 语句清理

- 原 store.js 第 135 行有 `debugger;` 语句，迁移时删除

### 12.6 子表行索引追踪

- getProps 中 PTEMPLATENAME 列需要访问"当前行 ADEPTID"
- 原 add.vue 通过 `onRowClick` 记录 `this.rowIndex`
- 迁移后 form.js 需保留 `onRowClick` + `_dtsRowIndex` 私有属性
