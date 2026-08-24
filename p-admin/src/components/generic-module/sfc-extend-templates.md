# SFC 扩展开发代码模板

结合 `rs-meta-form` / `rs-meta-field` / `rs-meta-query-panel` / `rs-meta-query-panel-field` 和 generic-module 扩展机制的实用代码模板。

## 目录

1. [扩展JS基本骨架](#1-扩展js基本骨架)
2. [Store扩展骨架](#2-store扩展骨架)
3. [查询面板 SFC Slot](#3-查询面板-sfc-slot)
4. [表单字段 SFC Slot](#4-表单字段-sfc-slot)
5. [按钮钩子](#5-按钮钩子)
6. [常用场景](#6-常用场景)

---

## 1. 扩展JS基本骨架

路径：`@/modules/{moduleCode}/{pageCode}.js`

```javascript
/**
 * {模块名称} - {页面名称} 扩展
 *
 * 可用的 this 属性:
 *   this.moduleCode      - 模块编码
 *   this.storeName       - Vuex 命名空间
 *   this.pageConfig      - 页面配置 (tss_module_page 行)
 *   this.pageConfigJson  - PAGECONFIG 解析后的 JSON
 *   this.storeObj        - store 辅助对象 (storeHelper/mapDateTable 等)
 *   this.$store / this.$router / this.$route
 *   this.$callAction({action, param, successText, successCall})
 *   this.$alert(msg) / this.$error(msg) / this.$confirm(msg)
 *   this.$busy() / this.$free()
 *
 * 列表页 (generic-module) 额外可用:
 *   this.$refs.list      - 列表组件 (this.$refs.list.query(1) 刷新)
 *   this.selectedRows    - 选中的行
 *   this.citem           - 当前点击行
 *   this.openPage({pageCode, mode, id, row, title})
 *   this.openSelector({moduleCode, pageCode, target, onSelected})
 *
 * 表单页 (generic-form) 额外可用:
 *   this.ID / this.STATE / this.CUSTNAME ... - 主表字段直接读写
 *   this.$MAIN / this.MAIN                  - 主表 DataTable / 数据
 *   this.$DTSA / this.DTSA                  - 子表 DataTable / 数据
 *   this.save() / this.closePage()
 */
export default {
  // ====== data ======
  data: {
    customLoading: false,
    extraData: {},
  },

  // ====== computed ======
  computed: {
    // 计算属性，模板中可直接用
    isEditable() {
      return this.ID && this.STATE === '1';
    },
    totalAmount() {
      if (!this.DTSA) return 0;
      return this.DTSA.reduce(function(sum, row) {
        return sum + (parseFloat(row.AMOUNT) || 0);
      }, 0);
    },
  },

  // ====== methods ======
  methods: {
    // 自定义方法，模板和按钮钩子中可用
    async loadData() {
      this.customLoading = true;
      try {
        var ret = await this.$callAction({
          action: this.moduleCode + '/customQuery',
          param: { FILTER: this.keyword },
        });
        this.extraData = ret.Data || {};
      } finally {
        this.customLoading = false;
      }
    },

    // 刷新列表
    refreshList() {
      if (this.$refs.list) this.$refs.list.query(1);
    },
  },

  // ====== 生命周期 ======

  // init: 早于 created，适合设置初始状态
  init() {
    this.extraData = { initialized: true };
  },

  // mounted: 已挂载，可访问 DOM
  mounted() {
    console.log('[' + this.moduleCode + '] 页面已挂载');
  },
};
```

---

## 2. Store扩展骨架

路径：`@/modules/{moduleCode}/store.js`

```javascript
/**
 * {模块名称} Store 扩展
 *
 * 合并到 {moduleCode} 的 Vuex 模块中，可扩展 actions/mutations。
 * Store03 默认 actions (可直接 dispatch 调用):
 *   query / open / add / save / delete / submit / reSubmit
 *   check / reCheck / verify / reVerify / batch / call / flowSave
 */
export default {
  actions: {
    // 自定义查询接口
    async customQuery({ commit, dispatch }, params) {
      var ret = await dispatch('call', {
        APICODE: 'A51',
        params: params,
      });
      return ret;
    },

    // 批量操作
    async batchApprove({ commit, dispatch, state }, { ids }) {
      var ret = await dispatch('call', {
        APICODE: 'A52',
        params: { ID: ids.join(','), ACTION: 'approve' },
      });
      // 重新查询刷新列表
      dispatch('query');
      return ret;
    },

    // 联动查询：根据主表数据查子表
    async loadSubTable({ commit, dispatch }, parentId) {
      var ret = await dispatch('call', {
        APICODE: 'A53',
        params: { PARENTID: parentId },
      });
      return ret;
    },
  },

  mutations: {
    // 自定义 mutation
    SET_EXTRA(state, payload) {
      state.extraData = payload;
    },
    CLEAR_EXTRA(state) {
      state.extraData = null;
    },
  },
};
```

---

## 3. 查询面板 SFC Slot

### 3.1 使用 rs-meta-query-panel 自动渲染

路径：`@/modules/{moduleCode}/query-panel.vue`

```html
<template>
  <div slot="simple-query">
    <rs-meta-query-panel
      v-if="qqryDt"
      :path="qqryDt"
      :module-code="host.moduleCode"
      :overrides="fieldOverrides"
      :cell-width="6"
      @query="onQuery"
      @reset="onReset"
    />
  </div>
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true }
  },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    },
    // 字段级覆盖
    fieldOverrides() {
      return {
        BUSTYPEID: { type: 'select', dict: 'D0701' },
        BILLDATE: { type: 'daterange' },
        CUSTID: { type: 'autocomplete', selType: 'cust', titleName: 'CUSTNAME' },
        STATUS: { type: 'select', dict: 'D0701', items: ['1', '2'] },
      };
    },
  },
  methods: {
    onQuery(queryValues) {
      // 查询已自动同步到 QQRY DataTable，直接触发列表查询
      this.host.$refs.list.query(1);
    },
    onReset() {
      this.host.$refs.list.query(1);
    },
  },
};
</script>
```

### 3.2 手动布局查询面板

路径：`@/modules/{moduleCode}/query-panel.vue`

```html
<template>
  <div slot="simple-query" v-if="qqryDt">
    <Row :space="9">
      <Cell width="6">
        <rs-meta-query-panel-field
          :path="qqryDt"
          field-name="BUSTYPEID"
          :module-code="host.moduleCode"
          :override="{ type: 'select', dict: 'D0701' }"
          :label-width="80"
        />
      </Cell>
      <Cell width="6">
        <rs-meta-query-panel-field
          :path="qqryDt"
          field-name="BILLDATE"
          :override="{ type: 'daterange' }"
          :label-width="80"
        />
      </Cell>
      <Cell width="6">
        <rs-meta-query-panel-field
          :path="qqryDt"
          field-name="CUSTID"
          :override="{ type: 'autocomplete', selType: 'cust', titleName: 'CUSTNAME' }"
          :label-width="80"
        />
      </Cell>
      <Cell width="6" style="text-align:right;">
        <Button color="primary" @click="doSearch">查询</Button>
        <Button class="ml5" @click="doReset">重置</Button>
      </Cell>
    </Row>
  </div>
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true }
  },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    }
  },
  methods: {
    doSearch() {
      if (!this.qqryDt) return;
      this.host.$refs.list.query(1);
    },
    doReset() {
      if (!this.qqryDt) return;
      // 清空查询条件
      var row = this.qqryDt.data[0];
      if (row) {
        Object.keys(row).forEach(function(k) {
          if (k.indexOf('_') !== 0) row[k] = '';
        });
      }
      this.host.$refs.list.query(1);
    },
  },
};
</script>
```

### 3.3 联动查询（选部门后过滤员工）

```html
<template>
  <div slot="simple-query" v-if="qqryDt">
    <Row :space="9">
      <Cell width="6">
        <rs-meta-query-panel-field
          :path="qqryDt"
          field-name="DEPTID"
          :override="{
            type: 'autocomplete',
            selType: 'dept',
            titleName: 'DEPTNAME',
            updateFields: 'DEPTID,ID;DEPTNAME,DEPTNAME'
          }"
          :label-width="80"
          @change="onDeptChange"
        />
      </Cell>
      <Cell width="6">
        <rs-meta-query-panel-field
          :path="qqryDt"
          field-name="EMPID"
          :override="{
            type: 'autocomplete',
            selType: 'emp',
            titleName: 'EMPNAME',
            paramMappings: 'DEPTID,DEPTID'
          }"
          :label-width="80"
        />
      </Cell>
    </Row>
  </div>
</template>

<script>
export default {
  props: { host: { type: Object, required: true } },
  computed: {
    qqryDt() {
      if (!this.host || !this.host.storeObj) return null;
      return this.host.storeObj.storeHelper.getTable('QQRY');
    }
  },
  methods: {
    onDeptChange({ field, value }) {
      // 部门变更后清空员工
      if (this.qqryDt) {
        this.qqryDt.setValue('EMPID', '');
      }
    },
  },
};
</script>
```

---

## 4. 表单字段 SFC Slot

### 替换机制说明

1. **位置**：由 `tss_resuipc.EDITSORT` 决定（m18 uiSetFull 配置字段排序）
2. **替换条件**：字段的 `EDITTYPE` 必须设为 `slot`（rs-form-cell 只在 `type==='slot'` 时渲染 slot 内容）
3. **数据流**：generic-form 传入 `:value`（字段当前值）+ 监听 `@input`（写回 DataTable），SFC 通过 props/emit 读写
4. **不要用 rs-meta-field**：会嵌套 FormItem（rs-form-cell 已含 FormItem + label），SFC 只渲染控件部分
5. **联动写入**：通过 `host.$MAIN.setValue('其他字段', 值)` 直接写 DataTable

### 4.1 AutoComplete 选择器字段

**前置条件**：在 m18 uiSetFull 中将字段 EDITTYPE 改为 `slot`

路径：`@/modules/{moduleCode}/field-cust.vue`

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
    return {
      option: buildAutoCompleteOption({ selType: 'cust', titleName: 'CUSTNAME' }),
    };
  },
  methods: {
    onInput(obj) {
      // emit input 写回当前字段
      this.$emit('input', obj ? obj.CUSTNAME : '');

      // 联动写入关联字段
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
        main.setValue('PHONE', d.PHONE || '');
      }
    },
  },
};
</script>
```

**常用选择器配置**：

| 字段 | selType | titleName | 联动 updateFields |
|------|---------|-----------|------------------|
| 客户 | `cust` | `CUSTNAME` | `CUSTID,ID;CUSTNAME,CUSTNAME` |
| 员工 | `emp` | `EMPNAME` | `EMPID,ID;EMPNAME,EMPNAME` |
| 部门 | `dept` | `DEPTNAME` | `DEPTID,ID;DEPTNAME,DEPTNAME` |
| 客户(树) | `dept-tree` | `DEPTNAME` | 同上 |
| 测量标准 | `tstdd` | `STDDNAME` | `STDDID,ID;STDDNAME,STDDNAME` |
| 委托单 | `accept` | `ACCEPTNAME` | `ACCEPTID,ID;ACCEPTCODE,BILLCODE` |

### 4.2 文件上传字段

**前置条件**：FILES 字段 EDITTYPE 改为 `slot`

路径：`@/modules/{moduleCode}/field-files.vue`

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
      var dts = this.host.$DTSA;
      if (!dts || !dts.data || !dts.data.length) return '';
      return dts.data.map(function(r) { return r.FILEID; }).filter(Boolean).join(',');
    },
  },
  methods: {
    onFileChange(files) {
      // 触发 mutation 重写 DTS 子表
      this.host.$store.commit(this.host.storeName + '/SETFILEDATA', files);
      this.$emit('input', files.map(function(f) { return f.id; }).join(','));
    },
  },
};
</script>
```

### 4.3 自定义输入控件

**前置条件**：字段 EDITTYPE 改为 `slot`

路径：`@/modules/{moduleCode}/field-amount.vue`

```html
<template>
  <NumberInput
    :value="value"
    @input="$emit('input', $event)"
    :precision="2"
    :min="0"
    :disabled="host.disabled"
    placeholder="请输入金额"
  />
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true },
    value: { type: [String, Number], default: '' },
  },
};
</script>
```

### 4.4 表单顶部/底部自定义内容

路径：`@/modules/{moduleCode}/form-top.vue`

```html
<template>
  <div class="form-top-area" v-if="host.ID">
    <div class="info-bar">
      <span>单号: {{ host.BILLCODE || '新建' }}</span>
      <span>状态: {{ statusText }}</span>
      <span>创建人: {{ host.CREATER }}</span>
    </div>
  </div>
</template>

<script>
export default {
  props: { host: { type: Object, required: true } },
  computed: {
    statusText() {
      var map = { '1': '待提交', '2': '待审核', '6': '已审批' };
      return map[this.host.STATE] || '未知';
    },
  },
};
</script>

<style scoped>
.info-bar {
  display: flex;
  gap: 20px;
  padding: 8px 12px;
  background: #f5f7fa;
  border-radius: 4px;
  margin-bottom: 8px;
  font-size: 13px;
  color: #606266;
}
</style>
```

---

## 5. 按钮钩子

### 5.1 保存前校验（异步）

扩展JS中定义：

```javascript
export default {
  methods: {
    // 按钮配置 EXTPARAM: { "beforeAction": "beforeSave" }
    async beforeSave(btn, context) {
      var row = context.row;

      // 校验必填
      if (!row.CUSTNAME) {
        this.$error('请填写客户名称');
        return false;
      }

      // 异步校验：检查客户编码是否重复
      var ret = await this.$callAction({
        action: this.moduleCode + '/checkCustCode',
        param: { CUSTCODE: row.CUSTCODE, ID: row.ID },
      });
      if (ret.Data && ret.Data.exists) {
        this.$error('客户编码已存在');
        return false;
      }

      // 确认对话框
      if (row.STATE === '6') {
        var confirmed = await this.$confirm('已审批的单据修改后需重新审批，确认修改？');
        if (!confirmed) return false;
      }
    },

    // 按钮配置 EXTPARAM: { "afterAction": "afterSave" }
    async afterSave(btn, context) {
      this.$alert('保存成功');
      // 刷新列表
      if (this.$refs.list) this.$refs.list.query(1);
    },
  },
};
```

### 5.2 删除前确认 + 批量操作

```javascript
export default {
  methods: {
    // 按钮配置 EXTPARAM: { "beforeAction": "beforeDelete" }
    async beforeDelete(btn, context) {
      var row = context.row;
      if (!row) {
        this.$error('请先选择记录');
        return false;
      }
      if (row.STATE === '6') {
        this.$error('已审批的单据不可删除');
        return false;
      }
    },

    // 批量审批按钮
    async batchApprove(btn, context) {
      var rows = this.selectedRows || [];
      if (!rows.length) {
        this.$error('请先勾选要审批的记录');
        return false;
      }
      var confirmed = await this.$confirm('确认审批选中的 ' + rows.length + ' 条记录？');
      if (!confirmed) return false;

      this.$busy();
      try {
        var ids = rows.map(function(r) { return r.ID; });
        await this.$callAction({
          action: this.moduleCode + '/batchApprove',
          param: { ids: ids },
          successText: '批量审批成功',
        });
        this.refreshList();
      } finally {
        this.$free();
      }
    },
  },
};
```

---

## 6. 常用场景

### 6.1 打开表单页并传参

```javascript
// 扩展JS methods 中
methods: {
  // 自定义"复制新建"按钮
  copyNew(btn, context) {
    var row = context.row;
    if (!row) {
      this.$error('请先选择要复制的记录');
      return false;
    }
    // 打开新增表单，传入源数据
    this.openPage({
      pageCode: 'add',
      mode: 'add',
      row: Object.assign({}, row, { ID: '', BILLCODE: '' }),
      title: '复制新建',
    });
  },
}
```

### 6.2 打开选入弹窗

```javascript
methods: {
  // 自定义"选入客户"按钮
  selectCustomer(btn, context) {
    this.openSelector({
      moduleCode: 'RS_M00',
      pageCode: 'select',
      target: 'MAIN',          // 写入主表
      fieldMap: 'CUSTID,ID;CUSTNAME,CUSTNAME;CUSTPHONE,PHONE',
      title: '选择客户',
      width: 800,
      filterParams: { STATUS: '1' },
      onSelected: function(rows) {
        console.log('选入完成', rows);
      },
    });
  },
}
```

### 6.3 子表操作（增删行 + 计算）

```javascript
// 表单页扩展JS
methods: {
  // 添加子表行时设默认值
  afterAddDetail(btn, context) {
    var dtsa = this.$DTSA;
    if (!dtsa) return;
    var newRow = dtsa.data[dtsa.data.length - 1];
    if (newRow) {
      newRow.UNIT = '个';
      newRow.PRICE = 0;
      newRow.QTY = 1;
    }
  },

  // 子表数量变更后计算金额
  onQtyChange(field, value) {
    if (field !== 'QTY') return;
    var row = this.$DTSA.data[0];
    if (row) {
      var qty = parseFloat(value) || 0;
      var price = parseFloat(row.PRICE) || 0;
      this.$DTSA.setValue('AMOUNT', (qty * price).toFixed(2));
    }
  },
}
```

### 6.4 调用后端自定义接口

```javascript
// 扩展JS methods
methods: {
  // 预览证书
  async previewCert(btn, context) {
    var row = context.row;
    if (!row || !row.ID) {
      this.$error('请先保存单据');
      return false;
    }
    this.$busy();
    try {
      var ret = await this.$callAction({
        action: this.moduleCode + '/call',
        param: {
          APICODE: 'A51',
          params: { ID: row.ID },
        },
      });
      if (ret.Data && ret.Data.url) {
        window.open(ret.Data.url);
      }
    } finally {
      this.$free();
    }
  },
}
```

### 6.5 列表行内操作（table-action slot）

路径：`@/modules/{moduleCode}/row-actions.vue`

```html
<template>
  <div class="row-actions">
    <Button
      v-for="btn in buttons"
      :key="btn.ID"
      size="s"
      :color="btn.COLOR || 'primary'"
      @click="host.handleBtnAction(btn, row)"
    >
      {{ btn.BTNNAME }}
    </Button>
    <!-- 自定义行内按钮 -->
    <Button size="s" @click="copyRow">复制</Button>
  </div>
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true },
    buttons: { type: Array, default: () => [] },
    row: { type: Object, default: null },
  },
  methods: {
    copyRow() {
      this.host.openPage({
        pageCode: 'add',
        mode: 'add',
        row: Object.assign({}, this.row, { ID: '', BILLCODE: '' }),
        title: '复制新建',
      });
    },
  },
};
</script>
```

### 6.6 头部按钮区（header-action slot）

路径：`@/modules/{moduleCode}/header-actions.vue`

```html
<template>
  <div class="header-actions">
    <!-- 配置的标准按钮 -->
    <Button
      v-for="btn in buttons"
      :key="btn.ID"
      :color="btn.COLOR || 'primary'"
      @click="host.handleBtnAction(btn)"
    >
      {{ btn.BTNNAME }}
    </Button>
    <!-- 自定义按钮 -->
    <Button @click="exportData">导出</Button>
    <Button @click="importData">导入</Button>
  </div>
</template>

<script>
export default {
  props: {
    host: { type: Object, required: true },
    buttons: { type: Array, default: () => [] },
  },
  methods: {
    exportData() {
      var rows = this.host.selectedRows || [];
      if (!rows.length) {
        this.host.$error('请先勾选要导出的记录');
        return;
      }
      this.host.$callAction({
        action: this.host.moduleCode + '/exportData',
        param: { ids: rows.map(function(r) { return r.ID; }) },
        successText: '导出成功',
      });
    },
    importData() {
      // 打开导入弹窗
      this.host.$router.push({ name: 'import-page', query: { module: this.host.moduleCode } });
    },
  },
};
</script>
```

---

## PAGECONFIG 配置

在 m18 配置页面中，PAGECONFIG JSON 配置 SLOTS 和 EXTENDJS：

```json
{
  "EXTENDJS": "@/modules/LIB_M07/main.js",
  "SLOTS": {
    "simple-query": "@/modules/LIB_M07/query-panel.vue",
    "header-action": "@/modules/LIB_M07/header-actions.vue",
    "table-action": "@/modules/LIB_M07/row-actions.vue"
  }
}
```

表单页：

```json
{
  "EXTENDJS": "@/modules/LIB_M07/add.js",
  "SLOTS": {
    "form-top": "@/modules/LIB_M07/form-top.vue",
    "field:CUSTNAME": "@/modules/LIB_M07/form-cust-field.vue"
  }
}
```
