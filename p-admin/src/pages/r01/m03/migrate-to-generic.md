# r01/m03 费用管理（LI_M03）→ generic-module + SFC 扩展迁移方案

## 迁移思路

将传统"四件套"（router.js / store.js / main.vue / add.vue）拆解为：

1. **数据库配置** — tss_module_page + tss_module_button + tss_resuipc（m18 可视化配置）
2. **SFC 扩展 JS** — 按钮显隐、批量收费/撤销/折扣、AMT 联动计算
3. **Store 扩展** — 批量操作 + 启停用 + TPM 数据更新
4. **list-t01 columnOverrides** — 列表列覆盖（金额格式化等）

**模块标识**：
- moduleCode: `LI_M03`
- apiPath: `/api/rm13/call`（RM13Controller，对应 tss_moudle MODULEID）

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | ROUTEPATH | QUERY_APICODE | OPEN_APICODE | SAVE_APICODE |
|----------|----------|----------|-----------|---------------|--------------|--------------|
| main | 费用管理 | list | /g/LI_M03/main | A01 | - | - |
| add | 费用编辑 | form | /g/LI_M03/add | - | A02 | A04 |

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "defaultFormPageCode": "add",
  "EXTENDJS": "@/modules/LI_M03/main.js",
  "SUMFIELDS": "AMT,RAMT"
}
```

字段说明：
- `QRYPATH` / `QQRYSPATH`：列表 / 高级查询 DataTable 路径名
- `defaultFormPageCode`：双击行打开的默认表单页
- `EXTENDJS`：列表扩展 SFC 资产路径
- `SUMFIELDS`：底部合计行字段（原 main.vue 中 `sumFields='AMT,RAMT'`）

### 1.3 add 页 PAGECONFIG

```json
{
  "MAINPATH": "MAIN",
  "FORMLAYOUT": "twocolumn",
  "EXTENDJS": "@/modules/LI_M03/form.js"
}
```

字段说明：
- `FORMLAYOUT: "twocolumn"`：双列表单（对应原 add.vue 的 `mode="twocolumn"`）
- `EXTENDJS`：表单扩展 SFC 资产路径（AMT 自动计算 + 收费注入）

### 1.4 按钮配置 (tss_module_button)

main 页按钮（BTNAREA=footer）：

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND | 说明 |
|---------|---------|---------|---------|----------|----------|------|
| 批量折扣 | custom | footer | A12 | `{"action":"batchDiscount","beforeAction":"confirmDiscount"}` | `ISDISCOUNT` | 选中任意行时可见 |
| 批量收费 | custom | footer | A13 | `{"action":"batchFee"}` | `ISFEE` | 全部未收费时可见 |
| 撤销收费 | custom | footer | A14 | `{"action":"batchReFee","beforeAction":"confirmBatch"}` | `ISREFEE` | 全部已收费时可见 |

add 页按钮（BTNAREA=footer）：

| BTNNAME | BTNCODE | BTNAREA | APICODE | EXTPARAM | SHOWCOND | 说明 |
|---------|---------|---------|---------|----------|----------|------|
| 修改 | save | footer | A04 | - | `ISSHOWSAVE` | 未收费时可见 |
| 收费 | custom | footer | A04 | `{"action":"chargeSave","beforeAction":"confirmCharge"}` | `ISSHOWCHARGE` | 未收费时可见 |

> 说明：原 add.vue 有"修改"与"收费"两个保存按钮，逻辑都走 A04 save，区别是"收费"会先 commit `SET_CHARGEDATA` 注入收费人 / 收费时间，再 save。

### 1.5 字段配置（走 m18 uiSetFull，不写 field slot）

查询面板字段（QQRY，8 个）：

| FIELDNAME | LABELNAME | EDITTYPE | QUERYMODE | SELECTDATA | QUERYSORT |
|-----------|-----------|----------|-----------|------------|-----------|
| BUSTYPEID | 业务类型 | select | eq | D03_BUSTYPE | 10 |
| BILLDATE | 受理日期 | daterange | range | - | 20 |
| BILLCODE | 受理编号 | text | like | - | 30 |
| CUSTNAME | 客户 | text | like | - | 40 |
| LINKER | 联系人 | text | like | - | 50 |
| MNAME | 仪器名称 | text | like | - | 60 |
| EMPNAME | 受理人 | text | like | - | 70 |
| STATE | 状态 | select | eq | D03_STATE | 80 |

> 字典：`D03_BUSTYPE`（委外=1/自检=2）、`D03_STATE`（待收费=1/已折扣=3/已收费=2）

列表字段（QRY，走 @ui 过滤器自动生成）：

| FIELDNAME | LABELNAME | LISTSORT |
|-----------|-----------|----------|
| BILLCODE | 受理编号 | 10 |
| CUSTNAME | 客户名称 | 20 |
| LINKER | 联系人 | 30 |
| MNAME | 仪器名称 | 40 |
| CAMT | 检测费 | 50 |
| OAMT | 其他费 | 60 |
| BAMT | 加急费 | 70 |
| AMT | 应收金额 | 80 |
| DISCOUNT | 折扣 | 90 |
| RAMT | 实收金额 | 100 |
| EMPNAME | 受理人 | 110 |
| CHARGER | 收费人 | 120 |
| CHARGETIME | 收费时间 | 130 |
| STATE | 状态 | 140 |

表单字段（MAIN，EDITSORT>0 显示）：

| FIELDNAME | LABELNAME | EDITTYPE | SELECTDATA | EDITSORT |
|-----------|-----------|----------|------------|----------|
| BILLCODE | 受理编号 | text | - | 10 |
| CUSTNAME | 客户 | text | - | 20 |
| LINKER | 联系人 | text | - | 30 |
| MNAME | 仪器名称 | text | - | 40 |
| CAMT | 检测费 | number | - | 50 |
| OAMT | 其他费 | number | - | 60 |
| BAMT | 加急费 | number | - | 70 |
| AMT | 应收金额 | number | - | 80 |
| DISCOUNT | 折扣 | number | - | 90 |
| RAMT | 实收金额 | number | - | 100 |
| DEPTID | 部门ID | text | - | 0（隐藏） |
| DEPTNAME | 部门名称 | text | - | 110 |
| CHARGEID | 收费人ID | text | - | 0（隐藏） |
| CHARGER | 收费人 | text | - | 120 |
| CHARGETIME | 收费时间 | datetime | - | 130 |
| STATE | 状态 | select | D03_STATE | 140 |
| ISUSE | 启用 | switch | - | 150 |

---

## 二、SFC 在线资产

### 2.1 store.js — Store 扩展

路径：`@/modules/LI_M03/store.js`

```javascript
/**
 * LI_M03 Store 扩展
 *
 * 保留原 store.js 中的自定义 actions 和 mutations。
 * Store03 默认 actions（query/open/add/save/delete/call/...）已内置，无需重复定义。
 *
 * 原 store.js 自定义 actions 对照：
 *   - add           → Store03 内置 add（INIT+ADD），删除
 *   - endisable     → 保留（A07 启停用）
 *   - updateTPMDATA → 保留（A08 更新 TPM 数据）
 *   - batchDiscount → 保留（A12 批量折扣）
 *   - batchFee      → 保留（A13 批量收费）
 *   - batchReFee    → 保留（A14 撤销收费）
 *   - querySel      → 删除（LI_M01/A06 跨模块查询，迁移后走选择器配置）
 *
 * 原 mutations：
 *   - SET_ENDISABLE   → 保留
 *   - SETTPMDATA      → 保留
 *   - SET_CHARGEDATA  → 保留（收费时注入 CHARGEID/CHARGER/CHARGETIME）
 */
export default {
  actions: {
    // 启停用（原 A07）
    async endisable({ commit, dispatch }, { item }) {
      commit('SET_ENDISABLE', { item });
      var ret = await dispatch('call', {
        APICODE: 'A07',
        params: {
          UPDATE: this.storeHelper.getTable('UPDATE').getXML()
        }
      });
      if (ret && ret.length > 0) {
        for (var key in ret[0]) {
          item[key] = ret[0][key];
        }
      }
    },

    // 更新 TPM 数据（原 A08）
    async updateTPMDATA({ commit, dispatch }, { item }) {
      commit('SETTPMDATA', { item });
      var ret = await dispatch('call', {
        APICODE: 'A08',
        params: {
          UPDATE: this.storeHelper.getTable('UPDATE').getXML()
        }
      });
      if (ret && ret.length > 0) {
        for (var key in ret[0]) {
          item[key] = ret[0][key];
        }
      }
    },

    // 批量折扣（原 A12）
    async batchDiscount({ commit, dispatch }, { items, DISCOUNT }) {
      await dispatch('batch', {
        APICODE: 'A12',
        items: items,
        updateFields: ['AMT', 'DISCOUNT'],
        params: { DISCOUNT: DISCOUNT }
      });
    },

    // 批量收费（原 A13）
    async batchFee({ commit, dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A13',
        items: items,
        updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'],
        params: {}
      });
    },

    // 撤销收费（原 A14）
    async batchReFee({ commit, dispatch }, { items }) {
      await dispatch('batch', {
        APICODE: 'A14',
        items: items,
        updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'],
        params: {}
      });
    }
  },

  mutations: {
    // 启停用：写 UPDATE DataTable
    SET_ENDISABLE(state, { item }) {
      var UPDATE = state.storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },

    // TPM 数据更新
    SETTPMDATA(state, { item }) {
      var UPDATE = state.storeHelper.getTable('UPDATE');
      UPDATE.setValue('TPMDATA', item.TPMDATA);
      UPDATE.setValue('ID', item.ID);
    },

    // 收费注入：写入 MAIN DataTable
    SET_CHARGEDATA(state, { userInfo }) {
      var MAIN = state.storeHelper.getTable('MAIN');
      MAIN.setValue('CHARGEID', userInfo.ID);
      MAIN.setValue('CHARGER', userInfo.NICKNAME);
      MAIN.setValue('CHARGETIME', _now());
    }
  }
};

// 当前时间字符串（yyyy-MM-dd hh:mm:ss）
function _now() {
  var d = new Date();
  var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
  return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
    + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
}
```

### 2.2 main.js — 列表页扩展

路径：`@/modules/LI_M03/main.js`

```javascript
/**
 * 费用管理列表页扩展
 *
 * this 上下文（generic-module 实例）：
 *   this.moduleCode / this.storeName / this.storeObj
 *   this.$refs.list                - ListT01 列表组件
 *   this.selectedRows / this.checks - 选中的行
 *   this.$callAction / this.$alert / this.$error / this.$confirm / this.$busy / this.$free
 *
 * 按钮显隐逻辑对照（原 main.vue computed）：
 *   ISFEE      - 全部未收费（有选中 && 全部 CHARGEID 为空）
 *   ISREFEE    - 全部已收费（有选中 && 全部 CHARGEID 非空）
 *   ISDISCOUNT - 任意选中（仅用于折扣按钮）
 *
 * 批量操作对照（原 main.vue methods）：
 *   batchFee      - A13 批量收费
 *   batchReFee    - A14 撤销收费
 *   batchDiscount - A12 批量折扣（带 DISCOUNT 参数）
 */
export default {
  data() {
    return {
      // 折扣弹层输入值（原 main.vue data.DISCOUNT = 1）
      DISCOUNT: 1
    };
  },

  computed: {
    // ====== 按钮显隐 ======

    // 批量折扣：有任意选中行
    ISDISCOUNT() {
      return (this.selectedRows || []).length > 0;
    },

    // 批量收费：有选中且全部未收费
    ISFEE() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return !r.CHARGEID; });
    },

    // 撤销收费：有选中且全部已收费
    ISREFEE() {
      var rows = this.selectedRows || [];
      if (!rows.length) return false;
      return rows.every(function(r) { return !!r.CHARGEID; });
    }
  },

  methods: {
    // ====== 批量操作确认（beforeAction 钩子）======

    // 批量操作通用确认
    async confirmBatch(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选记录');
        return false;
      }
      return await this.$confirm(
        '确认对 ' + rows.length + ' 条记录执行「' + btn.BTNNAME + '」操作？'
      );
    },

    // 折扣前确认（输入折扣值）
    async confirmDiscount(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选记录');
        return false;
      }
      var v = parseFloat(this.DISCOUNT);
      if (isNaN(v) || v < 0 || v > 1) {
        this.$error('折扣范围必须为 0.00 ~ 1.00');
        return false;
      }
      return await this.$confirm(
        '确认对 ' + rows.length + ' 条记录应用折扣 ' + v.toFixed(2) + '？'
      );
    },

    // ====== 批量操作 ======

    // 批量折扣（A12）— 原 main.vue batchDiscount
    async batchDiscount(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchDiscount',
        param: {
          items: rows,
          DISCOUNT: this.DISCOUNT
        },
        successText: '操作成功'
      });
      this.$refs.list.query(1);
    },

    // 批量收费（A13）— 原 main.vue batchFee
    async batchFee(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchFee',
        param: { items: rows },
        successText: '操作成功'
      });
      this.$refs.list.query(1);
    },

    // 撤销收费（A14）— 原 main.vue batchReFee
    async batchReFee(btn, context) {
      var rows = this.selectedRows || [];
      await this.$callAction({
        action: this.moduleCode + '/batchReFee',
        param: { items: rows },
        successText: '操作成功'
      });
      this.$refs.list.query(1);
    }
  },

  // 初始化折扣默认值
  init() {
    this.DISCOUNT = 1;
  }
};
```

### 2.3 form.js — 表单页扩展

路径：`@/modules/LI_M03/form.js`

```javascript
/**
 * 费用编辑表单页扩展
 *
 * this 上下文（generic-form 实例）：
 *   this.ID / this.STATE / this.CAMT / this.OAMT / this.BAMT / this.AMT ...
 *   this.$MAIN                       - 主表 DataTable
 *   this.save() / this.closePage()
 *   this.$callAction / this.$alert / this.$error / this.$confirm
 *   this.$store.state.user.userInfo  - 当前登录用户
 *
 * 核心逻辑（原 add.vue）：
 *   1. AMT 联动计算：AMT = (CAMT + OAMT) * DISCOUNT + BAMT
 *   2. 收费按钮：先 commit SET_CHARGEDATA 注入 CHARGEID/CHARGER/CHARGETIME，再 save
 *   3. 修改按钮：直接 save（不注入收费数据）
 */
export default {
  computed: {
    // 修改按钮：未收费（无 CHARGEID 或为空）
    ISSHOWSAVE() {
      return !this.CHARGEID;
    },
    // 收费按钮：未收费
    ISSHOWCHARGE() {
      return !this.CHARGEID;
    }
  },

  watch: {
    // ====== AMT 联动计算 ======
    // 公式：AMT = (CAMT + OAMT) * DISCOUNT + BAMT
    // 原逻辑：watch CAMT/OAMT/DISCOUNT/BAMT 任一变化即重算 AMT
    // 注意：原 add.vue watch 里漏了 CAMT（实际应一并监听）
    CAMT() { this._calcAMT(); },
    OAMT() { this._calcAMT(); },
    DISCOUNT() { this._calcAMT(); },
    BAMT() { this._calcAMT(); }
  },

  methods: {
    // 计算 AMT（原 add.vue 内联逻辑）
    _calcAMT() {
      var camt = parseFloat(this.CAMT) || 0;
      var oamt = parseFloat(this.OAMT) || 0;
      var discount = parseFloat(this.DISCOUNT) || 1;
      var bamt = parseFloat(this.BAMT) || 0;
      var amt = (camt + oamt) * discount + bamt;
      this.$MAIN.setValue('AMT', amt.toFixed(2));
    },

    // 收费保存：注入收费人信息后保存
    // 对应按钮 EXTPARAM.action = "chargeSave"
    async chargeSave(btn, context) {
      // 注入收费人/收费时间（原 add.vue mySave 方法）
      this.$store.commit(this.storeName + '/SET_CHARGEDATA', {
        userInfo: this.$store.state.user.userInfo
      });
      // 调用标准 save action
      await this.save();
    },

    // 表单打开时初始化（可选）
    async onShow() {
      if (this.ID) {
        // 编辑模式：加载数据
        await this.$store.dispatch(this.storeName + '/open', {
          FilterParams: { ID: this.ID }
        });
      } else {
        // 新增模式：初始化空行 + 默认值（原 store.js add action 设 ISUSE=1）
        await this.$store.dispatch(this.storeName + '/add');
        this.$MAIN.setValue('ISUSE', 1);
      }
    }
  }
};
```

---

## 三、list-t01 columnOverrides 配置示例

在 `@/modules/LI_M03/main.js` 的 `data()` 或通过 PAGECONFIG 注入列覆盖配置（金额字段格式化）：

```javascript
// 方式一：通过 PAGECONFIG.COLUMN_OVERRIDS 注入（推荐）
// PAGECONFIG JSON 增加：
// "COLUMN_OVERRIDS": {
//   "AMT": { "format": "number:2" },
//   "RAMT": { "format": "number:2" },
//   "CAMT": { "format": "number:2" },
//   "OAMT": { "format": "number:2" },
//   "DISCOUNT": { "format": "number:2" }
// }

// 方式二：在 main.js 通过 columnOverrides 选项注入
export default {
  data() {
    return {
      DISCOUNT: 1,
      // 列覆盖配置
      columnOverrides: {
        // 金额格式化（保留两位小数）
        AMT: function(value, row) {
          return parseFloat(value || 0).toFixed(2);
        },
        RAMT: function(value, row) {
          return parseFloat(value || 0).toFixed(2);
        },
        CAMT: function(value, row) {
          return parseFloat(value || 0).toFixed(2);
        },
        OAMT: function(value, row) {
          return parseFloat(value || 0).toFixed(2);
        },
        // 状态映射
        STATE: function(value, row) {
          var map = { '1': '待收费', '2': '已收费', '3': '已折扣' };
          return map[value] || value;
        },
        // 折扣百分比显示
        DISCOUNT: function(value, row) {
          var v = parseFloat(value || 1);
          return (v * 100).toFixed(0) + '%';
        }
      }
    };
  },
  // ... computed / methods 同上
};
```

---

## 四、关键逻辑对照说明

### 4.1 AMT 自动计算（原 add.vue watch）

**原逻辑**（`add.vue` 第 39-49 行）：
```javascript
watch: {
  OAMT()    { this.AMT = parseFloat((1*this.CAMT + 1*this.OAMT) * this.DISCOUNT + this.BAMT, 2); },
  DISCOUNT(){ this.AMT = parseFloat((1*this.CAMT + 1*this.OAMT) * this.DISCOUNT + this.BAMT, 2); },
  BAMT()    { this.AMT = parseFloat((1*this.CAMT + 1*this.OAMT) * this.DISCOUNT + this.BAMT, 2); }
}
```

**迁移后**（`form.js`）：统一抽出 `_calcAMT()` 方法，并补充 CAMT 的监听（原代码漏了）。详见 §2.3。

### 4.2 批量折扣 / 收费 / 撤销收费

**原逻辑**（`main.vue` methods）：通过 `this.$callAction` 调用 store.js 中的 actions：
- `batchFee` → A13，updateFields=['RAMT','CHARGEID','CHARGER','CHARGETIME']
- `batchReFee` → A14，updateFields 同上
- `batchDiscount` → A12，带 DISCOUNT 参数，updateFields=['AMT','DISCOUNT']

**迁移后**：
- Store actions 完整保留（见 §2.1）
- main.js 中封装为独立方法（见 §2.2），便于按钮 `EXTPARAM.action` 路由
- 折扣按钮额外校验 DISCOUNT 取值范围（0~1），原代码无校验

### 4.3 ISFEE / ISREFEE / ISDISCOUNT 按钮显隐

**原逻辑**（`main.vue` computed，第 108-123 行）：
- `ISFEE`：选中行全部 `!CHARGEID`（未收费）
- `ISREFEE`：选中行全部 `!!CHARGEID`（已收费）
- `ISDISCOUNT`：有任意选中行

**迁移后**（见 §2.2）：保留原语义，使用 `Array.every` 简化判断，逻辑等价。

### 4.4 收费按钮注入逻辑（原 add.vue mySave）

**原逻辑**（`add.vue` 第 52-56 行）：
```javascript
mySave() {
  this.$store.commit(`${Constants.STORE_NAME}/SET_CHARGEDATA`, {
    userInfo: this.$store.state.user.userInfo
  });
  this.save();
}
```

**迁移后**：`SET_CHARGEDATA` mutation 保留在 store.js（见 §2.1），表单按钮通过 `EXTPARAM.action="chargeSave"` 路由到 `form.js` 的 `chargeSave` 方法（见 §2.3）。

---

## 五、迁移对照表

| 原 r01/m03 文件 | 迁移后 | 说明 |
|----------------|--------|------|
| `router.js` | 删除 | generic-module 路由通过 `tss_func.OUTERURL=/g/LI_M03/main` 自动注册 |
| `index.js` | 删除 | 入口由 generic-module 统一接管 |
| `store.js` | `@/modules/LI_M03/store.js` | 仅保留自定义 actions（batchFee/ReFee/Discount + endisable/updateTPMDATA）和 mutations（SET_ENDISABLE/SETTPMDATA/SET_CHARGEDATA） |
| `views/main.vue` | m18 配置 + `main.js` | 模板配置化（list-t01 + 查询面板），按钮逻辑进扩展 JS |
| `views/add.vue` | m18 配置 + `form.js` | 表单配置化（rs-form-edit + twocolumn），AMT 联动进扩展 JS |
| `mapDateTable('QQRY', [...])` | m18 uiSetFull 自动处理 | 8 个查询字段全部走 `@ui` 过滤器 |
| `mapDateTable('MAIN', [...])` | generic-form 自动映射 | 字段直接通过 `this.CAMT` 等访问 |
| `ISFEE/ISREFEE/ISDISCOUNT` computed | 扩展 JS computed | 放入 `main.js` |
| `batchFee/batchReFee/batchDiscount` methods | 扩展 JS methods | 放入 `main.js`，按钮通过 `EXTPARAM.action` 路由 |
| `mySave()` + `SET_CHARGEDATA` | 扩展 JS `chargeSave` + Store mutation | 放入 `form.js` + `store.js` |
| `watch CAMT/OAMT/DISCOUNT/BAMT` | 扩展 JS watch | 放入 `form.js`，统一抽 `_calcAMT()` |
| `sumFields='AMT,RAMT'` | PAGECONFIG.SUMFIELDS | 列表合计行配置 |
| 面包屑 `bcDatas` | list-t01 内置（自动从菜单生成） | 不需手写 |
| `param1` (业务类型选项) | 字典 D03_BUSTYPE | m18 SELECTDATA 配置 |
| `param` (状态选项) | 字典 D03_STATE | m18 SELECTDATA 配置 |

---

## 六、迁移后目录结构

```
src/modules/LI_M03/              # SFC 扩展资产（数据库 tss_code_asset）
  store.js                       # Store 扩展（批量操作 + 启停用 + SET_CHARGEDATA）
  main.js                        # 列表页扩展（按钮显隐 + 批量折扣/收费/撤销）
  form.js                        # 表单页扩展（AMT 联动计算 + 收费注入）
```

原 `src/pages/r01/m03/` 目录可删除，路由通过菜单 `tss_func.OUTERURL = /g/LI_M03/main` 自动注册。

---

## 七、实施步骤

1. **m18 配置页面**（s01/m18）
   - 新建模块 LI_M03，配置 main/add 两个页面
   - 配置 PAGECONFIG（参考 §1.2 / §1.3）
   - 配置按钮（参考 §1.4）
   - 字段配置（参考 §1.5，走 uiSetFull）

2. **数据字典**
   - 新建 D03_BUSTYPE（1=委外, 2=自检）
   - 新建 D03_STATE（1=待收费, 2=已收费, 3=已折扣）

3. **SFC 资产**（s01/m17 代码在线开发）
   - 新建 `@/modules/LI_M03/store.js`（§2.1）
   - 新建 `@/modules/LI_M03/main.js`（§2.2）
   - 新建 `@/modules/LI_M03/form.js`（§2.3）

4. **菜单配置**
   - `tss_func.OUTERURL` 改为 `/g/LI_M03/main`

5. **验证**
   - 列表查询（8 个查询字段）
   - 合计行（AMT/RAMT）
   - 批量折扣（含范围校验）
   - 批量收费 / 撤销收费（按钮显隐）
   - 表单 AMT 联动计算
   - 收费按钮注入收费人信息

6. **清理**
   - 删除 `src/pages/r01/m03/` 目录
