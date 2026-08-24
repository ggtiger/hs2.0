# r02/m02 人员效能表（LIR_M02）→ generic-module + SFC 扩展方案

## 迁移思路

人员效能表是 **"查询 + 表格（部门 rowspan）+ ECharts 饼图"** 报表。饼图的 series 数据由 `部门+检验员` 拼接成 name、`效能系数(XNXS)` 作为 value，与 generic-module 内置 `reportChartOptions` 的 pie 分支天然契合（`xField` 为 name 来源、`yFields[0]` 为 value）。迁移后：

1. **m18 配置化** —— 表格列由 scm `LISTSORT` 自动生成；饼图由 `PAGECONFIG.REPORT.CHART.type=pie` 描述。
2. **EXTENDJS 处理 2 类定制** —— ① 部门 `rowspan` 合并（DEPTNAME）② 饼图 name 拼接 `部门+空格+检验员`。
3. **Store 扩展** —— 空，Store03 默认 `advQuery` 走 A01。

> 与 r02/m01 的核心差异：饼图的 `name` 字段需要拼接两个字段（DEPTNAME+EMPNAME），而 generic-module 内置 pie 分支只取 `xField` 单字段。需要扩展 JS 重写 `reportChartOptions` computed，或在 `afterLoadReport` 中预生成 `CHART_NAME` 派生字段供 xField 引用。本文采用**派生字段方案**（零侵入）。

---

## 一、数据库配置（m18 可视化配置）

### 1.1 模块配置 (tss_module_page)

| PAGECODE | PAGENAME | PAGETYPE | COMPONENTTYPE | ROUTEPATH | QUERY_APICODE |
|----------|----------|----------|---------------|-----------|---------------|
| main | 人员效能表 | report | - | /g/LIR_M02/main | A01 |

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
    "ROWSPAN_FIELDS": ["DEPTNAME"],
    "DERIVED_FIELDS": {
      "CHART_NAME": "DEPTNAME + ' ' + EMPNAME"
    },
    "CHART": {
      "type": "pie",
      "xField": "CHART_NAME",
      "yFields": ["XNXS"],
      "initOption": {
        "color": ["#77a2dc", "#3b9a9c", "#4bc2c5", "#78fee0"],
        "title": {
          "text": "效能系数",
          "subtext": "（及时率-错误率）*检毕/平均检测时长",
          "left": "center"
        },
        "tooltip": { "trigger": "item", "formatter": "{a} <br/>{b} : {c} ({d}%)" },
        "legend": { "orient": "vertical", "left": "left", "data": [] },
        "series": [{
          "name": "访问来源",
          "type": "pie",
          "radius": "55%",
          "center": ["50%", "60%"],
          "emphasis": {
            "itemStyle": {
              "shadowBlur": 10,
              "shadowOffsetX": 0,
              "shadowColor": "rgba(0, 0, 0, 0.5)"
            }
          }
        }]
      }
    }
  },
  "EXTENDJS": "@/modules/LIR_M02/main.js"
}
```

#### 配置字段说明

| 字段 | 作用 |
|------|------|
| `APICODE` | 报表数据接口 A01 |
| `PAGEMAX` | PageSize 默认 500（拉全量） |
| `ROWSPAN_FIELDS` | 需要按部门合并的列：仅 DEPTNAME（EMPNAME 不合并） |
| `DERIVED_FIELDS` | 派生字段映射：`CHART_NAME = DEPTNAME + ' ' + EMPNAME`，供饼图 xField 引用 |
| `CHART.type=pie` | 饼图，generic-module `reportChartOptions` 走 pie 分支 |
| `CHART.xField=CHART_NAME` | 饼图每扇区的 name 来源（派生字段） |
| `CHART.yFields=[XNXS]` | 饼图每扇区的 value 来源（效能系数） |
| `title` | 饼图标题 "效能系数" + 副标题公式说明 |
| `series.radius/center/emphasis` | 饼图半径/位置/高亮样式（原 main.vue 配置） |

> 注意：scm 中不注册 `CHART_NAME` 字段（派生字段不入库），避免后端 SELECT 找不到列。

### 1.3 按钮配置 (tss_module_button)

| BTNNAME | BTNCODE | BTNAREA | INTERACTTYPE | SHOWCOND |
|---------|---------|---------|--------------|----------|
| 搜索 | search | header | none | - |

---

## 二、SFC 在线资产（`@/modules/LIR_M02/`）

### 2.1 main.js — 报表扩展

```javascript
/**
 * LIR_M02 人员效能表 报表扩展
 *
 * 职责：
 *   1) rowspan 合并：DEPTNAME 按部门分组
 *   2) 派生 CHART_NAME 字段：DEPTNAME + ' ' + EMPNAME（供饼图 xField 引用）
 *   3) 默认日期：本月1号 ~ 今天
 *
 * this 上下文 (generic-module):
 *   this.reportRows         - 当前表格数据
 *   this.reportConfig       - PAGECONFIG.REPORT
 *   this.pageConfigJson     - 整个 PAGECONFIG
 *   this.storeObj.storeHelper.getTable('QQRY') - 查询条件 DataTable
 */
export default {
  methods: {
    // loadReportData 完成后调用
    afterLoadReport(rows) {
      // 1) 派生 CHART_NAME 字段
      rows.forEach(function(r) {
        r.CHART_NAME = (r.DEPTNAME || '') + ' ' + (r.EMPNAME || '');
      });
      // 2) 计算 DEPTNAME rowspan
      var segStart = 0;
      for (var i = 1; i <= rows.length; i++) {
        var prev = rows[i - 1];
        var curr = rows[i];
        if (!prev) continue;
        if (!curr || prev.DEPTNAME !== curr.DEPTNAME) {
          var span = i - segStart;
          rows[segStart].DEPTNAME_rowspan = span;
          for (var k = segStart + 1; k < i; k++) {
            rows[k].DEPTNAME_rowspan = 0;
          }
          segStart = i;
        }
      }
      return rows;
    },
  },

  mounted() {
    // 默认日期：本月1号 ~ 今天
    var sh = this.storeObj && this.storeObj.storeHelper;
    if (!sh) return;
    var qqry = sh.getTable('QQRY');
    var pad = function(n) { return n < 10 ? '0' + n : '' + n; };
    var d1 = new Date(); d1.setDate(1);
    var firstDay = d1.getFullYear() + '-' + pad(d1.getMonth() + 1) + '-' + pad(d1.getDate());
    var t = new Date();
    var today = t.getFullYear() + '-' + pad(t.getMonth() + 1) + '-' + pad(t.getDate());
    qqry.setValue('SDATE', firstDay);
    qqry.setValue('EDATE', today);
  },
};
```

### 2.2 表格列 rowspan 注入

DEPTNAME 列的 scm `LISTCONFIG` 配置：

```json
{
  "rowspan": "DEPTNAME_rowspan"
}
```

generic-module 的 `reportColumns` 生成时解析为：

```javascript
{
  title: '部门',
  key: 'DEPTNAME',
  attrs: function(data) { return { rowspan: data.DEPTNAME_rowspan }; }
}
```

> 注意：原 main.vue 中 `namePrevIndex` 字段同时被 DEPTNAME 列使用，这里改名为 `DEPTNAME_rowspan` 避免与表格内置 `$index` 等字段冲突，并保持与 r02/m01 命名一致。

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
| DEPTNAME | 部门 | 10 | `{"rowspan":"DEPTNAME_rowspan"}` |
| EMPNAME | 检验员 | 20 | `{"width":200}` |
| F1 | 收件 | 30 | - |
| CN1 | 检毕 | 40 | - |
| CN2 | 未检数 | 50 | - |
| CN3 | 积压数 | 60 | - |
| WCL | 完成率 | 70 | - |
| JSL | 及时率 | 80 | - |
| CN4 | 重做数 | 90 | - |
| TN1 | 平均检测时长(天) | 100 | - |
| CWL | 错误率 | 110 | - |
| XNXS | 效能系数 | 120 | - |

> 注意：**不要**在 scm 中注册 `CHART_NAME` 字段，它是前端派生字段，后端 SELECT 不会有此列。

---

## 四、Store 扩展（`@/modules/LIR_M02/store.js`）

```javascript
/**
 * LIR_M02 Store 扩展
 * 报表数据查询走 Store03 默认 advQuery（A01 APICODE），空挂载。
 */
export default {
  actions: {},
};
```

---

## 五、迁移对照表

| 原 r02/m02 文件 | 迁移后 | 说明 |
|-----------------|--------|------|
| `router.js` | 不需要 | 路由自动注册 `/g/LIR_M02/main` |
| `store.js`（含 createStore） | `@/modules/LIR_M02/store.js` | 空挂载 |
| `views/main.vue` | m18 配置 + `@/modules/LIR_M02/main.js` | report-t01 内置渲染 |
| `report-t01` 组件引用 | generic-module 内置 | PAGETYPE=report 自动加载 |
| `chart` 组件引用 | report-t01 内置 | 无需 import |
| `columns` 数组 | scm LISTSORT 配置 | m18 uiSetFull 维护 |
| `datas` 数组 | `this.reportRows` | generic-module `loadReportData` 填充 |
| `options.title`（效能系数+公式） | PAGECONFIG.REPORT.CHART.initOption.title | 静态配置化 |
| `options.tooltip.formatter` | initOption.tooltip.formatter | ECharts 字符串模板原生支持 |
| `options.series[0]`（pie 配置） | initOption.series[0] | type/radius/center/emphasis 原样保留 |
| `namePrevIndex` + `attrs(data).rowspan` | `ROWSPAN_FIELDS: ["DEPTNAME"]` + `afterLoadReport` | 自动合并部门 |
| `initData` 中 `legend.data` + `series[0].data` 构建 | generic-module pie 分支 + `CHART_NAME` 派生字段 | xField=CHART_NAME, yFields=[XNXS] |
| 饼图 name = `部门+空格+检验员` | `DERIVED_FIELDS.CHART_NAME` | EXTENDJS `afterLoadReport` 预生成 |
| `query()` method | `loadReportData()` | generic-module 自动调用 |
| 日期默认值 | EXTENDJS mounted + DEFAULTVALUE 兜底 | 本月1号~今天 |

---

## 六、迁移后目录结构

```
src/modules/LIR_M02/              # SFC 扩展资产（tss_code_asset）
  main.js                         # 报表扩展（CHART_NAME 派生 + DEPTNAME rowspan + 默认日期）
  store.js                        # Store 扩展（空挂载）
```

原 `src/pages/r02/m02/` 目录可删除，菜单 `tss_func.OUTERURL = /g/LIR_M02/main` 自动注册。

---

## 七、关键风险与对策

| 风险 | 对策 |
|------|------|
| 饼图 name 需拼接两字段，generic-module pie 分支只取单字段 xField | 引入 `DERIVED_FIELDS` 配置，EXTENDJS `afterLoadReport` 预生成 `CHART_NAME` 字段；xField 指向派生字段 |
| 派生字段若误入 scm 会导致后端 SELECT 报错 | scm 中**不注册** CHART_NAME；EXTENDJS 只在前端 rows 上挂属性 |
| `tooltip.formatter` 是字符串模板 | ECharts 原生支持，直接写入 initOption.tooltip.formatter |
| `emphasis.itemStyle` 阴影样式 | 原样保留到 initOption.series[0].emphasis |
| 饼图 series.name="访问来源" 是原代码遗留（语义不符） | 可改为 `"人员效能"` 或保留（影响 tooltip `{a}` 显示，无功能问题） |
| 部门分组时若 EMPNAME 为空会导致 name 末尾多一个空格 | `afterLoadReport` 中 trim：`r.CHART_NAME = (r.DEPTNAME + ' ' + (r.EMPNAME||'')).trim()` |
| 平均检测时长（TN1）/ 错误率（CWL）列单位 | 放在 LABELNAME 中（已是如此） |
| 效能系数 XNXS 若为 null/undefined，饼图 value 会异常 | `afterLoadReport` 中补 0：`r.XNXS = +r.XNXS || 0` |
