# r02/m01 检测情况统计表（LIR_M01）→ generic-module + SFC 扩展方案

## 迁移思路

检测情况统计表是标准 **"查询 + 表格 + ECharts 柱状图"** 报表，是 generic-module 内置 `PAGETYPE=report` 模板的最佳适用对象。迁移后：

1. **m18 配置化** —— 表格列由 scm `LISTSORT` 自动生成；图表由 `PAGECONFIG.REPORT.CHART` 描述（type/xField/yFields）。
2. **EXTENDJS 处理 3 类定制** —— ① 部门名称 `rowspan` 合并 ② 部门汇总行单独画图（剔除明细行）③ 双行 legend formatter。
3. **Store 扩展** —— 几乎不需要自定义 action，Store03 默认 `query` 走 A01 即可（后端已返回扁平数据，前端做 rowspan）。

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | COMPONENTTYPE | ROUTEPATH | QUERY_APICODE |
|----------|----------|----------|---------------|-----------|---------------|
| main | 检测情况统计表 | report | - | /g/LIR_M01/main | A01 |

> PAGETYPE=report 走 generic-module 内置 `report-t01` 模板，自动渲染表格+图表。

### 1.2 main 页 PAGECONFIG

```json
{
  "QRYPATH": "QRY",
  "QQRYSPATH": "QQRY",
  "PAGETYPE": "report",
  "REPORT": {
    "APICODE": "A01",
    "PAGEMAX": 500,
    "ROWSPAN_FIELD": "DEPTNAME",
    "ROWSPAN_FIELDS": ["DEPTNAME", "S2", "S3"],
    "LEGEND_SPLITTER": ",",
    "CHART_FILTER": "DEPTNAME=部门汇总",
    "CHART": {
      "type": "bar",
      "xField": "STDDNAME",
      "yFields": ["F1", "CN2", "RAMT", "S3"],
      "legendLabels": ["收件,该标准受理单台件数", "未检数,尚未办结的台件数", "实收收入", "项目累计,该项目今年1月1日来累计收入"],
      "initOption": {
        "color": ["#77a2dc", "#3b9a9c", "#4bc2c5", "#78fee0"],
        "tooltip": { "trigger": "axis", "axisPointer": { "type": "shadow" } },
        "legend": {
          "right": 0,
          "orient": "vertical",
          "formatter": "__LEGEND_FORMATTER__"
        },
        "grid": { "right": 200, "bottom": 100, "top": 20 },
        "xAxis": [{
          "type": "category",
          "axisTick": { "alignWithLabel": true },
          "axisLabel": { "interval": 0, "rotate": 45 }
        }],
        "yAxis": [{ "type": "value" }]
      }
    }
  },
  "EXTENDJS": "@/modules/LIR_M01/main.js"
}
```

#### 配置字段说明

| 字段 | 作用 |
|------|------|
| `APICODE` | 报表数据接口，generic-module `loadReportData` 自动 dispatch advQuery |
| `PAGEMAX` | PageSize 默认 500（拉全量），原 report-t01 一次性拉取 |
| `ROWSPAN_FIELDS` | 需要按部门合并的列，扩展 JS 读取后写入 `namePrevIndex` 字段驱动 attrs.rowspan |
| `CHART_FILTER` | 图表数据筛选条件：只画 "部门汇总" 行 |
| `legendLabels` | 与 yFields 一一对应的多行 legend 名称（用逗号分隔表示换行） |
| `__LEGEND_FORMATTER__` | 占位符，扩展 JS 在 mounted 时替换为按 `,` 拆分换行的 formatter 函数 |

### 1.3 按钮配置 (tss_module_button)

| BTNNAME | BTNCODE | BTNAREA | INTERACTTYPE | SHOWCOND |
|---------|---------|---------|--------------|----------|
| 搜索 | search | header | none | - |

> 查询按钮的 click 由 generic-module `loadReportData` 自动触发。

---

## 二、SFC 在线资产（`@/modules/LIR_M01/`）

### 2.1 main.js — 报表扩展

```javascript
/**
 * LIR_M01 检测情况统计表 报表扩展
 *
 * 职责：
 *   1) rowspan 合并：DEPTNAME / S2(部门小计) / S3(部门累计) 按部门分组
 *   2) 图表只画 "部门汇总" 行（generic-module 的 reportChartOptions 已支持 CHART_FILTER，
 *      但若需更复杂条件，可在此重写 this.reportRows / this.reportChartOptions）
 *   3) legend formatter 按逗号换行（mounted 时注入到 initOption.legend.formatter）
 *
 * this 上下文 (generic-module):
 *   this.reportRows         - 当前表格数据（数组）
 *   this.reportConfig       - PAGECONFIG.REPORT 对象
 *   this.pageConfigJson     - 整个 PAGECONFIG
 *   this.$refs.report       - report-t01 组件实例（含 $refs.charts）
 *   this.loadReportData()   - 重新查询（已内置）
 */
export default {
  methods: {
    // 在 loadReportData 完成后调用：计算 rowspan
    afterLoadReport(rows) {
      var rsFields = (this.reportConfig.ROWSPAN_FIELDS) || ['DEPTNAME'];
      var nameField = (this.reportConfig.ROWSPAN_FIELD) || 'DEPTNAME';
      // 对每个 rowspan 字段，按 nameField 连续段计算 rowspan
      rsFields.forEach(function(field) {
        var segStart = 0;
        for (var i = 1; i <= rows.length; i++) {
          var prev = rows[i - 1];
          var curr = rows[i];
          if (!prev) continue;
          if (!curr || prev[nameField] !== curr[nameField]) {
            // 一段结束
            var span = i - segStart;
            rows[segStart][field + '_rowspan'] = span;
            for (var k = segStart + 1; k < i; k++) {
              rows[k][field + '_rowspan'] = 0;
            }
            segStart = i;
          }
        }
      });
      return rows;
    },

    // legend formatter：按逗号拆成两行
    buildLegendFormatter() {
      return function(name) {
        var arr = String(name).split(',');
        return arr.length > 1 ? arr[0] + '\n' + arr[1] : arr[0];
      };
    },
  },

  // mounted 后替换 legend.formatter 占位符，并触发首次查询
  mounted() {
    var cfg = this.reportConfig || {};
    var initOpt = (cfg.CHART && cfg.CHART.initOption) || {};
    if (initOpt.legend && initOpt.legend.formatter === '__LEGEND_FORMATTER__') {
      initOpt.legend.formatter = this.buildLegendFormatter();
    }
    // 默认查询日期：本月1号 ~ 今天
    var sh = this.storeObj && this.storeObj.storeHelper;
    if (sh) {
      var qqry = sh.getTable('QQRY');
      var d = new Date(); d.setDate(1);
      var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
      var firstDay = d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
      var today = (function() {
        var t = new Date();
        return t.getFullYear() + '-' + pad(t.getMonth() + 1) + '-' + pad(t.getDate());
      })();
      qqry.setValue('SDATE', firstDay);
      qqry.setValue('EDATE', today);
    }
  },
};
```

### 2.2 表格列 rowspan 注入（通过 scm `LISTCONFIG`）

generic-module 的 `reportColumns` 从 scm 生成 column 定义。为让 DEPTNAME/S2/S3 列带上 `attrs(data, index)` rowspan，有两种方式：

**方式 A（推荐）：scm 配置 `LISTCONFIG` JSON**

在 m18 的列设置弹窗里，对 DEPTNAME/S2/S3 三列的 `LISTCONFIG` 字段填入：

```json
{
  "attrs": "rowspan:DEPTNAME_rowspan"
}
```

generic-module 解析后生成：

```javascript
{ title: '部门名称', key: 'DEPTNAME', attrs: function(data) { return { rowspan: data.DEPTNAME_rowspan }; } }
```

**方式 B：扩展 JS 覆盖 `reportColumns` computed**

```javascript
computed: {
  reportColumns() {
    var cols = this._defaultColumns; // generic-module 默认 scm 生成结果
    var rsMap = { DEPTNAME: 'DEPTNAME_rowspan', S2: 'S2_rowspan', S3: 'S3_rowspan' };
    return cols.map(function(c) {
      var key = c.key || c.prop;
      if (rsMap[key]) {
        return Object.assign({}, c, {
          attrs: function(data) { return { rowspan: data[rsMap[key]] || 1 }; }
        });
      }
      return c;
    });
  },
}
```

> 因 Vue 2 在 mounted 后添加 computed 不自动建立 watcher，generic-module 已提供 `_extendKeys` 机制允许覆盖内置 computed。建议优先用方式 A。

---

## 三、查询字段配置（m18 uiSetFull）

### QQRY 查询条件字段（resuipc QUERYSORT>0）

| FIELDNAME | LABELNAME | EDITTYPE | QUERYMODE | QUERYSORT | DEFAULTVALUE |
|-----------|-----------|----------|-----------|-----------|--------------|
| SDATE | 开始日期 | date | range | 10 | 本月1号 |
| EDATE | 结束日期 | date | range | 20 | 今天 |

### QRY 报表字段（resuipc LISTSORT>0）

| FIELDNAME | LABELNAME | LISTSORT | LISTCONFIG |
|-----------|-----------|----------|------------|
| DEPTNAME | 部门名称 | 10 | `{"rowspan":"DEPTNAME_rowspan"}` |
| STDDNAME | 项目 | 20 | `{"width":200,"tooltip":{"placement":"top-start","content":"项目说明"}}` |
| F1 | 收件 | 30 | - |
| CN1 | 检毕 | 40 | - |
| CN2 | 未检数 | 50 | - |
| CN3 | 积压数 | 60 | - |
| WCL | 完成率 | 70 | - |
| JSL | 及时率 | 80 | - |
| TN1 | 平均检测时长(天) | 90 | - |
| AMT | 应收收入 | 100 | - |
| RAMT | 实收收入 | 110 | - |
| S1 | 项目累计 | 120 | - |
| S2 | 部门小计 | 130 | `{"rowspan":"S2_rowspan"}` |
| S3 | 部门累计 | 140 | `{"rowspan":"S3_rowspan"}` |

---

## 四、Store 扩展（`@/modules/LIR_M01/store.js`）

```javascript
/**
 * LIR_M01 Store 扩展
 * 报表数据查询走 Store03 默认 advQuery（A01 APICODE），
 * 此处仅保留空挂载，未来如需后处理可在 afterQuery 钩子扩展。
 */
export default {
  actions: {},
};
```

---

## 五、迁移对照表

| 原 r02/m01 文件 | 迁移后 | 说明 |
|-----------------|--------|------|
| `router.js` | 不需要 | 路由自动注册 `/g/LIR_M01/main` |
| `store.js`（含 createStore） | `@/modules/LIR_M01/store.js` | 空挂载（A01 query 走 Store03 默认） |
| `views/main.vue` | m18 配置 + `@/modules/LIR_M01/main.js` | report-t01 由 generic-module 内置渲染 |
| `report-t01` 组件引用 | generic-module 内置 | PAGETYPE=report 自动加载 |
| `chart` 组件引用 | report-t01 内置 | 无需 import |
| `columns` 数组 | scm LISTSORT 配置 | m18 uiSetFull 维护 |
| `datas` 数组 | `this.reportRows` | generic-module `loadReportData` 填充 |
| `options` 对象 | PAGECONFIG.REPORT.CHART.initOption | 静态部分配置化，formatter 进 EXTENDJS |
| `namePrevIndex` + `attrs(data).rowspan` | `ROWSPAN_FIELDS` 配置 + `afterLoadReport` 计算 | 自动合并 DEPTNAME/S2/S3 |
| `initData` 后处理 | EXTENDJS `afterLoadReport` 钩子 | 计算 rowspan、分离汇总行画图 |
| 部门汇总筛选画图 | `CHART_FILTER: "DEPTNAME=部门汇总"` | generic-module 过滤后再画图 |
| legend formatter 换行 | EXTENDJS `buildLegendFormatter` | 占位符 `__LEGEND_FORMATTER__` 替换 |
| 日期默认值 | EXTENDJS mounted + DEFAULTVALUE 兜底 | 本月1号~今天 |
| `query()` method | `loadReportData()` | generic-module 自动调用 |

---

## 六、迁移后目录结构

```
src/modules/LIR_M01/              # SFC 扩展资产（tss_code_asset）
  main.js                         # 报表扩展（rowspan + legend formatter + 默认日期）
  store.js                        # Store 扩展（空挂载）
```

原 `src/pages/r02/m01/` 目录可删除，菜单 `tss_func.OUTERURL = /g/LIR_M01/main` 自动注册。

---

## 七、关键风险与对策

| 风险 | 对策 |
|------|------|
| `attrs(data).rowspan` 是 HeyUI Table 的列属性，scm `LISTCONFIG` 需 generic-module 支持 JSON 解析为 function | 若不支持，降级为 EXTENDJS 覆盖 `reportColumns` computed |
| 部门汇总行（DEPTNAME=部门汇总）不能进表格显示 | 保持表格显示全部行（含汇总），图表用 CHART_FILTER 剔除；若表格也要剔除，扩展 afterLoadReport 返回前 filter |
| legend formatter 是 function 无法直接 JSON 配置 | 用占位符 `__LEGEND_FORMATTER__`，EXTENDJS mounted 时替换 |
| tooltip `formatter: '{a} <br/>{b} : {c} ({d}%)'` 是字符串模板 | ECharts 原生支持字符串 formatter，直接写入 initOption 即可 |
| 平均检测时长（TN1）单位"天"放在表头 | LISTCONFIG.title 设为 `'平均检测时长(天)'`（已是如此） |
| `namePrevIndex` 字段污染原始数据 | 改用 `{FIELD}_rowspan` 命名，避免与后端字段冲突 |
