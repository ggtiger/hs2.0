# r02/m03 客户统计（LIR_M03）→ generic-module + SFC 扩展方案

## 迁移思路

客户统计表是 **"查询 + 表格 + ECharts 柱状图（当年/去年/前年三系列同比）"** 报表。与 r02/m01 的核心差异有三点：

1. **SHOWNUM 字段控制 PageSize** —— 用户输入"显示条数"，查询前同步到 `PageSize`，查询后恢复 SHOWNUM 原值。
2. **额外的 CUSTNAME 查询条件** —— 比 m01/m02 多一个客户名称模糊搜索。
3. **无 rowspan** —— 客户统计不合并单元格，`ROWSPAN_FIELDS` 留空。

迁移后：

1. **m18 配置化** —— 表格列由 scm `LISTSORT` 生成；柱状图由 `PAGECONFIG.REPORT.CHART` 描述（type=bar，3 个 yField）。
2. **EXTENDJS 处理 SHOWNUM 双向绑定** —— 监听 `shownum` data 字段，同步到 QQRY DataTable 的 `PageSize`；查询完成后恢复 shownum。
3. **Store 扩展** —— 空，Store03 默认 `advQuery` 走 A01。

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | COMPONENTTYPE | ROUTEPATH | QUERY_APICODE |
|----------|----------|----------|---------------|-----------|---------------|
| main | 客户统计 | report | - | /g/LIR_M03/main | A01 |

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "PAGETYPE": "report",
  "REPORT": {
    "APICODE": "A01",
    "PAGEMAX": 20,
    "PAGESIZE_FIELD": "SHOWNUM",
    "PAGESIZE_SYNC": true,
    "CHART": {
      "type": "bar",
      "xField": "CUSTNAME",
      "yFields": ["RAMT1", "RAMT2", "RAMT3"],
      "legendLabels": ["当年", "去年", "前年"],
      "initOption": {
        "color": ["#77a2dc", "#3b9a9c", "#4bc2c5", "#78fee0"],
        "tooltip": { "trigger": "axis", "axisPointer": { "type": "shadow" } },
        "legend": {
          "right": 0,
          "orient": "vertical",
          "formatter": "__LEGEND_FORMATTER__",
          "data": []
        },
        "grid": { "right": 200, "bottom": 100, "top": 20 },
        "xAxis": [{
          "type": "category",
          "axisTick": { "alignWithLabel": true },
          "axisLabel": { "interval": 0, "rotate": 45 }
        }],
        "yAxis": [{ "type": "value" }],
        "series": []
      }
    }
  },
  "EXTENDJS": "@/modules/LIR_M03/main.js"
}
```

#### 配置字段说明

| 字段 | 作用 |
|------|------|
| `APICODE` | 报表数据接口 A01 |
| `PAGEMAX` | PageSize 默认值 20（客户统计有 TOP N 语义，默认显示前 20） |
| `PAGESIZE_FIELD` | 指定 SHOWNUM 字段为 PageSize 来源，generic-module `loadReportData` 时读取此字段覆盖 PageSize |
| `PAGESIZE_SYNC` | true 表示查询完成后把 PageSize 恢复为 SHOWNUM 原值（原 main.vue `this.SHOWNUM = t` 行为） |
| `CHART.type=bar` | 柱状图 |
| `CHART.xField=CUSTNAME` | X 轴为客户名称 |
| `CHART.yFields=[RAMT1,RAMT2,RAMT3]` | 三个系列：当年实收/去年实收/前年实收 |
| `legendLabels` | legend 显示名（与 yFields 一一对应） |
| `__LEGEND_FORMATTER__` | 占位符，EXTENDJS 替换为逗号换行 formatter |

> 注意：`legendLabels` 与 `yFields` 长度必须一致；generic-module 生成 series 时把 legendLabels 映射到 series.name。

### 1.3 按钮配置 (tss_module_button)

| BTNNAME | BTNCODE | BTNAREA | INTERACTTYPE | SHOWCOND |
|---------|---------|---------|--------------|----------|
| 搜索 | search | header | none | - |

---

## 二、SFC 在线资产（`@/modules/LIR_M03/`）

### 2.1 main.js — 报表扩展

```javascript
/**
 * LIR_M03 客户统计 报表扩展
 *
 * 职责：
 *   1) SHOWNUM ↔ PageSize 双向同步（PAGESIZE_FIELD + PAGESIZE_SYNC）
 *   2) legend formatter 按逗号换行（原代码如此，当年/去年/前年虽无逗号但保留统一逻辑）
 *   3) 默认日期：本月1号 ~ 今天
 *   4) 默认 SHOWNUM = 20
 *
 * this 上下文 (generic-module):
 *   this.reportRows         - 当前表格数据
 *   this.reportConfig       - PAGECONFIG.REPORT
 *   this.storeObj.storeHelper.getTable('QQRY') - 查询条件 DataTable
 *   this.loadReportData()   - 重新查询（内置）
 */
export default {
  data: function() {
    return { shownum: 20 };
  },

  watch: {
    // SHOWNUM 变化时同步到 QQRY.PageSize
    shownum: function(v) {
      var sh = this.storeObj && this.storeObj.storeHelper;
      if (!sh) return;
      var qqry = sh.getTable('QQRY');
      if (qqry) qqry.setValue('PageSize', +v || 20);
    },
  },

  methods: {
    // loadReportData 前置钩子：把 SHOWNUM 写入 PageSize
    beforeLoadReport() {
      var sh = this.storeObj && this.storeObj.storeHelper;
      if (!sh) return;
      var qqry = sh.getTable('QQRY');
      if (qqry) qqry.setValue('PageSize', +this.shownum || 20);
    },

    // loadReportData 后置钩子：恢复 SHOWNUM 显示值（PageSize 可能被后端响应覆盖）
    afterLoadReport(rows) {
      var sh = this.storeObj && this.storeObj.storeHelper;
      if (sh && this.reportConfig.PAGESIZE_SYNC) {
        this.shownum = +this.shownum || 20;
      }
      return rows;
    },

    // legend formatter：按逗号拆成两行（与 r02/m01 一致）
    buildLegendFormatter() {
      return function(name) {
        var arr = String(name).split(',');
        return arr.length > 1 ? arr[0] + '\n' + arr[1] : arr[0];
      };
    },
  },

  mounted() {
    // 替换 legend.formatter 占位符
    var cfg = this.reportConfig || {};
    var initOpt = (cfg.CHART && cfg.CHART.initOption) || {};
    if (initOpt.legend && initOpt.legend.formatter === '__LEGEND_FORMATTER__') {
      initOpt.legend.formatter = this.buildLegendFormatter();
    }

    // 默认日期：本月1号 ~ 今天
    var sh = this.storeObj && this.storeObj.storeHelper;
    if (sh) {
      var qqry = sh.getTable('QQRY');
      var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
      var d1 = new Date(); d1.setDate(1);
      var firstDay = d1.getFullYear() + '-' + pad(d1.getMonth() + 1) + '-' + pad(d1.getDate());
      var t = new Date();
      var today = t.getFullYear() + '-' + pad(t.getMonth() + 1) + '-' + pad(t.getDate());
      qqry.setValue('SDATE', firstDay);
      qqry.setValue('EDATE', today);
      qqry.setValue('SHOWNUM', 20);
      qqry.setValue('PageSize', 20);
      this.shownum = 20;
    }
  },
};
```

### 2.2 SHOWNUM 输入框 SFC Slot

generic-module 的 simple-query slot 默认从 scm 生成查询字段。SHOWNUM 需要 `type=number`，可通过 scm `LISTCONFIG` 指定：

```json
{ "type": "number", "placeholder": "显示条数" }
```

或通过 SFC slot 自定义（如需更复杂的交互）：

路径：`@/modules/LIR_M03/query-panel.vue`

```html
<template>
  <div class="m03-query-slot">
    <input type="text" class="rr-flex-1" placeholder="客户名称" v-model="custname" />
    <input type="number" class="rr-flex-1" placeholder="显示条数" v-model="shownum" min="1" />
    <DatePicker v-model="sdate" placeholder="开始日期" :option="{end:edate}"></DatePicker>
    <span>-</span>
    <DatePicker v-model="edate" placeholder="结束日期" :option="{start:sdate}"></DatePicker>
  </div>
</template>

<script>
export default {
  props: { host: { type: Object, required: true } },
  data() {
    return { custname: '', shownum: 20, sdate: '', edate: '' };
  },
  watch: {
    custname(v) { this._sync('CUSTNAME', v); },
    shownum(v)  { this._sync('SHOWNUM', +v || 20); this._sync('PageSize', +v || 20); },
    sdate(v)    { this._sync('SDATE', v); },
    edate(v)    { this._sync('EDATE', v); },
  },
  methods: {
    _sync(field, val) {
      var sh = this.host.storeObj && this.host.storeObj.storeHelper;
      if (!sh) return;
      var qqry = sh.getTable('QQRY');
      if (qqry) qqry.setValue(field, val);
    },
  },
};
</script>
```

PAGECONFIG.SLOTS 配置：

```json
{
  "SLOTS": {
    "simple-query": "@/modules/LIR_M03/query-panel.vue"
  }
}
```

> 两种方式二选一。推荐用 scm LISTCONFIG 配置（零 SFC），复杂交互时才用 SFC slot。

---

## 三、查询字段配置（m18 uiSetFull）

### QQRY 查询条件字段（resuipc QUERYSORT>0）

| FIELDNAME | LABELNAME | EDITTYPE | QUERYMODE | QUERYSORT | DEFAULTVALUE | LISTCONFIG |
|-----------|-----------|----------|-----------|-----------|--------------|------------|
| CUSTNAME | 客户名称 | text | like | 10 | - | - |
| SHOWNUM | 显示条数 | number | eq | 20 | 20 | `{"placeholder":"显示条数","min":1}` |
| SDATE | 开始日期 | date | range | 30 | 本月1号 | - |
| EDATE | 结束日期 | date | range | 40 | 今天 | - |

> 注意：SHOWNUM 是虚拟查询字段，后端 A01 接口的 F02 过滤器需忽略此字段（只用于设置 PageSize）。

### QRY 报表字段（resuipc LISTSORT>0）

| FIELDNAME | LABELNAME | LISTSORT |
|-----------|-----------|----------|
| CUSTNAME | 客户名称 | 10 |
| LINKER | 联系人 | 20 |
| MOBILE | 联系方式 | 30 |
| RAMT1 | 实收费用 | 40 |
| F11 | 台件数 | 50 |
| RAMT2 | 收费同比去年 | 60 |
| RAMT3 | 收费同比前年 | 70 |
| F12 | 台件数同比去年 | 80 |
| F13 | 台件数同比前年 | 90 |

---

## 四、Store 扩展（`@/modules/LIR_M03/store.js`）

```javascript
/**
 * LIR_M03 Store 扩展
 * 报表数据查询走 Store03 默认 advQuery（A01 APICODE），空挂载。
 * SHOWNUM → PageSize 的同步由 EXTENDJS watch + beforeLoadReport 处理。
 */
export default {
  actions: {},
};
```

---

## 五、SHOWNUM → PageSize 同步机制说明

原 main.vue 的 `query()` 方法有一段特殊逻辑：

```javascript
query() {
  this.PageSize = this.SHOWNUM;   // 1) 查询前把 SHOWNUM 写入 PageSize
  let t = this.SHOWNUM;           // 2) 缓存 SHOWNUM 原值
  this.$callAction({
    action: '...',
    successCall: () => {
      this.initData();
      this.SHOWNUM = t;           // 3) 查询后恢复 SHOWNUM（因为后端响应可能改了 PageSize）
    },
  });
}
```

这是因为：
- **PageSize 是后端分页参数**，控制返回行数
- **SHOWNUM 是前端显示参数**，用户可随意修改
- 两者解耦：查询时同步，查询后恢复，避免 SHOWNUM 被 PageSize 响应值覆盖

迁移后通过 `PAGESIZE_FIELD` + `PAGESIZE_SYNC` 两个 PAGECONFIG 配置项声明此行为，generic-module `loadReportData` 内部处理：

```javascript
// generic-module.loadReportData（伪代码，已内置）
var cfg = this.reportConfig;
var qqry = this.storeObj.storeHelper.getTable(this.listQqryPath);
if (cfg.PAGESIZE_FIELD) {
  var shownum = qqry.getValue(cfg.PAGESIZE_FIELD);
  if (shownum) qqry.setValue('PageSize', shownum);  // 同步
}
var prevShownum = qqry.getValue(cfg.PAGESIZE_FIELD);
await this.$store.dispatch(this.storeName + '/advQuery', { APICODE: cfg.APICODE, qryPath: this.listQryPath });
if (cfg.PAGESIZE_SYNC && prevShownum) {
  qqry.setValue(cfg.PAGESIZE_FIELD, prevShownum);   // 恢复
}
```

---

## 六、迁移对照表

| 原 r02/m03 文件 | 迁移后 | 说明 |
|-----------------|--------|------|
| `router.js` | 不需要 | 路由自动注册 `/g/LIR_M03/main` |
| `store.js`（含 createStore） | `@/modules/LIR_M03/store.js` | 空挂载 |
| `views/main.vue` | m18 配置 + `@/modules/LIR_M03/main.js` | report-t01 内置渲染 |
| `report-t01` 组件引用 | generic-module 内置 | PAGETYPE=report 自动加载 |
| `chart` 组件引用 | report-t01 内置 | 无需 import |
| `columns` 数组 | scm LISTSORT 配置 | m18 uiSetFull 维护 |
| `datas` 数组 | `this.reportRows` | generic-module `loadReportData` 填充 |
| `options` 对象 | PAGECONFIG.REPORT.CHART.initOption | 静态部分配置化 |
| `SHOWNUM` 输入框 | scm LISTCONFIG `type:number` 或 SFC slot | m18 配置 |
| `this.PageSize = this.SHOWNUM` | `PAGESIZE_FIELD: "SHOWNUM"` | generic-module loadReportData 同步 |
| `this.SHOWNUM = t`（恢复） | `PAGESIZE_SYNC: true` | generic-module loadReportData 恢复 |
| `watch.SHOWNUM → PageSize` | EXTENDJS `watch.shownum` | SFC data 字段同步 |
| `initData` 中 legend/series 构建 | generic-module bar 分支 + legendLabels 映射 | xField=CUSTNAME, yFields=[RAMT1,RAMT2,RAMT3] |
| `namePrevIndex` rowspan 计算 | 删除（客户统计不合并单元格） | 原代码保留了 m01 的 rowspan 逻辑但实际无合并效果 |
| legend formatter 换行 | EXTENDJS `buildLegendFormatter` | 占位符替换 |
| 日期默认值 | EXTENDJS mounted + DEFAULTVALUE 兜底 | 本月1号~今天 |
| SHOWNUM 默认值 20 | EXTENDJS mounted + DEFAULTVALUE | mounted 中 `this.shownum = 20` |
| `query()` method | `loadReportData()` | generic-module 自动调用 |

---

## 七、迁移后目录结构

```
src/modules/LIR_M03/              # SFC 扩展资产（tss_code_asset）
  main.js                         # 报表扩展（SHOWNUM 同步 + legend formatter + 默认值）
  store.js                        # Store 扩展（空挂载）
  query-panel.vue                 # 可选：查询面板 SFC slot（复杂交互时使用）
```

原 `src/pages/r02/m03/` 目录可删除，菜单 `tss_func.OUTERURL = /g/LIR_M03/main` 自动注册。

---

## 八、关键风险与对策

| 风险 | 对策 |
|------|------|
| SHOWNUM 是虚拟字段，后端 F02 过滤器不识别 | A01 的 FILTERSQL 用 `@ui:adv` 自动生成时，SHOWNUM 无 resuipc QUERYMODE 配置则被跳过；或手动在 FILTERSQL 中不引用此字段 |
| SHOWNUM 同步到 PageSize 后，后端响应的 PageSize 可能与请求不一致 | `PAGESIZE_SYNC: true` 在查询后恢复 SHOWNUM 原值，避免 UI 显示混乱 |
| SHOWNUM 输入非数字会导致 PageSize 为 NaN | EXTENDJS watch 中 `+v \|\| 20` 兜底为默认值 20 |
| legend formatter 是 function 无法直接 JSON 配置 | 用占位符 `__LEGEND_FORMATTER__`，EXTENDJS mounted 时替换 |
| 三系列柱状图（当年/去年/前年）若某客户去年/前年无数据 | ECharts 自动跳过 null/undefined 数据点，不影响渲染 |
| `namePrevIndex` rowspan 在原代码中保留但客户统计实际不合并（DEPTNAME 列不存在） | 迁移时直接删除 rowspan 相关逻辑，`ROWSPAN_FIELDS` 留空 |
| CUSTNAME 模糊查询若客户名含特殊字符 | 后端 F02 用 `LIKE CONCAT('%',@CUSTNAME,'%')` 已处理；@ui:adv 的 like 模式同效 |
| SHOWNUM=0 或负数会导致 PageSize 异常 | LISTCONFIG 配置 `min:1`，EXTENDJS watch 兜底 `+v \|\| 20` |
| 客户名重复时柱状图 X 轴会重叠 | `axisLabel.rotate: 45` + `interval: 0` 强制全部显示（已配置） |
