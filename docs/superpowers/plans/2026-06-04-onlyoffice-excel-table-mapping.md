# OnlyOffice Excel 编辑表格映射 - 实施计划

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 s01/m07 模板编辑器中集成 OnlyOffice Excel，让用户通过 Excel 弹窗编辑表格布局、字段占位符和公式，一键回写到富文本控件。

**Architecture:** 前端使用 SheetJS（已安装 `xlsx@0.18.5`）完成 HTML 表格 ↔ Excel 双向转换。OnlyOffice Document Server（已部署 `localhost:8088`）作为 Excel 编辑器弹窗嵌入。后端新增 `ExcelEditorController` 提供临时文件上传/下载/回调接口。现有 `rs-onlyoffice-preview` 组件的 API 加载逻辑可复用。

**Tech Stack:** Vue 2.5 + SheetJS (xlsx@0.18.5) + OnlyOffice Document Editor API + .NET Core 2.2 (后端)

---

## Chunk 1: 后端 — ExcelEditorController

### Task 1: 创建 ExcelEditorController（临时文件上传/下载/回调）

**Files:**
- Create: `netcore/Realso.WebAPI/Controllers/ExcelEditorController.cs`

**背景**：OnlyOffice 编辑模式需要：①前端上传 .xlsx 文件到后端 ②OnlyOffice 通过 URL 下载文件 ③OnlyOffice 保存时回调后端通知文件更新。后端已有文件上传基础设施（`FileController` + `Upload:ROOT` 配置 + `Upload:临时` 路径），可直接复用。

- [ ] **Step 1: 创建 ExcelEditorController.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Realso.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  public class ExcelEditorController : BaseControl
  {
    private readonly IHostingEnvironment _hostingEnvironment;
    // 临时文件存储（key -> 文件路径）
    private static readonly Dictionary<string, TempFileInfo> _tempFiles = new Dictionary<string, TempFileInfo>();

    public ExcelEditorController(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }

    /// <summary>
    /// 上传临时 Excel 文件，返回 key 和下载 URL
    /// POST /api/exceleditor/upload
    /// </summary>
    [HttpPost("upload")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> Upload()
    {
      var files = Request.Form.Files;
      if (files.Count == 0)
      {
        return BadRequest(new { Message = "未提供文件" });
      }

      var formFile = files[0];
      if (formFile.Length > 0)
      {
        string key = Guid.NewGuid().ToString("N");
        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        string fileName = key + "_" + formFile.FileName;
        string filePath = Path.Combine(tempDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await formFile.CopyToAsync(stream);
        }

        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
        string downloadUrl = apiUrl + "/api/exceleditor/download?key=" + key;

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = filePath,
          FileName = formFile.FileName,
          CreateTime = DateTime.Now
        };

        // 清理超过 24 小时的临时文件
        CleanupOldFiles();

        return Ok(new { key, downloadUrl, fileName = formFile.FileName });
      }

      return BadRequest(new { Message = "文件为空" });
    }

    /// <summary>
    /// 下载临时 Excel 文件（供 OnlyOffice Document Server 调用）
    /// GET /api/exceleditor/download?key=xxx
    /// </summary>
    [HttpGet("download")]
    [EnableCors("AllowHeaders")]
    public IActionResult Download(string key)
    {
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      var info = _tempFiles[key];
      if (!System.IO.File.Exists(info.FilePath))
      {
        return NotFound(new { Message = "文件不存在" });
      }

      var stream = System.IO.File.OpenRead(info.FilePath);
      return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", info.FileName);
    }

    /// <summary>
    /// OnlyOffice 保存回调
    /// POST /api/exceleditor/callback
    /// OnlyOffice 文档编辑完成或强制保存时回调此接口
    /// status=2: 文档已关闭需保存, status=6: 强制保存中
    /// </summary>
    [HttpPost("callback")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> Callback([FromBody] JObject body)
    {
      try
      {
        int status = body["status"]?.Value<int>() ?? 0;
        string key = body["key"]?.ToString();

        if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
        {
          return Ok(new { error = 0 });
        }

        var info = _tempFiles[key];

        // status=2: 文档关闭，下载最新版本
        // status=6: 正在编辑中的强制保存
        if (status == 2 || status == 6)
        {
          string url = body["url"]?.ToString();
          if (!string.IsNullOrEmpty(url))
          {
            using (var client = new HttpClient())
            {
              client.Timeout = TimeSpan.FromSeconds(30);
              var response = await client.GetAsync(url);
              if (response.IsSuccessStatusCode)
              {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.IO.File.WriteAllBytes(info.FilePath, bytes);
              }
            }
          }
        }

        // OnlyOffice 要求回调返回 {"error":0}
        return Ok(new { error = 0 });
      }
      catch (Exception ex)
      {
        Logger.Info($"ExcelEditor Callback 异常: {ex.Message}");
        return Ok(new { error = 0 });
      }
    }

    /// <summary>
    /// 获取 OnlyOffice 编辑器配置（编辑模式）
    /// GET /api/exceleditor/editor-config?key=xxx
    /// </summary>
    [HttpGet("editor-config")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetEditorConfig(string key)
    {
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      var info = _tempFiles[key];
      string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
      string fileUrl = apiUrl + "/api/exceleditor/download?key=" + key;
      string callbackUrl = apiUrl + "/api/exceleditor/callback";
      string docKey = key + "_" + DateTime.Now.Ticks;

      var config = new
      {
        document = new
        {
          fileType = "xlsx",
          key = docKey,
          title = info.FileName,
          url = fileUrl,
          permissions = new
          {
            edit = true,
            download = true,
            print = false
          }
        },
        documentType = "cell",
        editorConfig = new
        {
          mode = "edit",
          callbackUrl = callbackUrl,
          lang = "zh-CN",
          user = new
          {
            id = this.userInfo != null ? this.userInfo["ID"] + "" : "guest",
            name = this.userInfo != null ? this.userInfo["NICKNAME"] + "" : "访客"
          },
          customization = new
          {
            autosave = false,
            chat = false,
            comments = true,
            forcesave = true,
            help = false,
            hideRightMenu = false,
            compactHeader = true,
            compactToolbar = true,
            feedback = false,
            toolbarNoTabs = false
          }
        }
      };

      return Ok(config);
    }

    private void CleanupOldFiles()
    {
      var expired = new List<string>();
      foreach (var kv in _tempFiles)
      {
        if (DateTime.Now - kv.Value.CreateTime > TimeSpan.FromHours(24))
        {
          try
          {
            if (File.Exists(kv.Value.FilePath))
            {
              File.Delete(kv.Value.FilePath);
            }
          }
          catch { }
          expired.Add(kv.Key);
        }
      }
      foreach (var k in expired)
      {
        _tempFiles.Remove(k);
      }
    }

    private class TempFileInfo
    {
      public string FilePath { get; set; }
      public string FileName { get; set; }
      public DateTime CreateTime { get; set; }
    }
  }
}
```

- [ ] **Step 2: 构建后端验证编译通过**

Run: `cd /Users/wanghu/work/project/hs2.0/netcore && dotnet build Realso.WebAPI`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add netcore/Realso.WebAPI/Controllers/ExcelEditorController.cs
git commit -m "feat: add ExcelEditorController for OnlyOffice Excel editing"
```

---

## Chunk 2: 前端核心 — excelConverter.js 双向转换引擎

### Task 2: 创建 excelConverter.js — HTML 表格解析与字段提取

**Files:**
- Create: `p-admin/src/pages/s01/m07/views/components/excelConverter.js`

**背景**：`itemEditor` 的 `value` 字段包含 HTML（内有 `<table>`），`fields` 字段是数组 `[{field, name, value, width, height, fieldType, formula, minv, maxv, ...}]`。转换器需将 HTML 表格 → Excel，Excel → HTML + fields。

- [ ] **Step 1: 创建 excelConverter.js 基础结构和 HTML → Excel 导出**

```javascript
'use strict';

import XLSX from 'xlsx';

// =============================================
// Excel 函数 ↔ 模板公式 映射表
// =============================================

// Excel 函数 → 模板函数
var EXCEL_TO_TEMPLATE = {
  'SUM': '$t',
  'AVERAGE': '$avg',
  'ABS': '$abs',
  'SQRT': '$sqrt',
  'LN': '$log',
  'ROUND': '$fixed',
  'STDEV.S': '$stdev',
  'MAX': null,   // 特殊处理
  'MIN': null,   // 特殊处理
  'SUMSQ': null, // 特殊处理
};

// 模板函数 → Excel 函数
var TEMPLATE_TO_EXCEL = {
  '$t': 'SUM',
  '$avg': 'AVERAGE',
  '$abs': 'ABS',
  '$sqrt': 'SQRT',
  '$log': 'LN',
  '$fixed': 'ROUND',
  '$stdev': 'STDEV.S',
  '$pow2': null,       // 特殊: val^2
  '$indError': null,   // 计量专用，直接保留 $ 前缀
  '$std': null,
  '$maxStd': null,
  '$maxAbs': null,
  '$minAbs': null,
  '$avgStd': null,
  '$maxmin': null,
  '$maxminStd': null,
  '$sqrtpow': null,
  '$abAbs': null,
  '$round': 'ROUND',
};

// 计量专用函数列表（在 Excel 中以 =$xxx() 形式标记）
var CUSTOM_FUNCTIONS = [
  '$indError', '$std', '$maxStd', '$maxAbs', '$minAbs',
  '$avgStd', '$maxmin', '$maxminStd', '$sqrtpow', '$abAbs', '$round'
];

// 匹配 ${字段名} 占位符
var FIELD_PATTERN = /\$\{([^\}]+)\}/g;
// 匹配 Excel 单元格引用 (如 A1, B2, AA10)
var CELL_REF_PATTERN = /\b([A-Z]+)(\d+)\b/g;

/**
 * 将列字母转换为列号 (A=0, B=1, ..., Z=25, AA=26, ...)
 */
function colLetterToNum(letter) {
  var num = 0;
  for (var i = 0; i < letter.length; i++) {
    num = num * 26 + (letter.charCodeAt(i) - 64);
  }
  return num - 1;
}

/**
 * 将列号转换为列字母 (0=A, 1=B, ...)
 */
function colNumToLetter(num) {
  var letter = '';
  num++;
  while (num > 0) {
    num--;
    letter = String.fromCharCode(65 + (num % 26)) + letter;
    num = Math.floor(num / 26);
  }
  return letter;
}

// =============================================
// 导出: itemEditor → Excel
// =============================================

/**
 * 解析 HTML 表格，提取单元格数据和合并信息
 * @param {string} html - 包含 <table> 的 HTML 字符串
 * @returns {{ rows: Array, merges: Array, cellFieldMap: Object }}
 */
function parseHtmlTable(html) {
  var parser = new DOMParser();
  var doc = parser.parseFromString(html, 'text/html');
  var table = doc.querySelector('table');
  if (!table) {
    return { rows: [], merges: [], cellFieldMap: {} };
  }

  var trs = table.querySelectorAll('tr');
  var rows = [];
  var merges = [];
  var cellFieldMap = {};  // 单元格坐标 → 字段名 (如 "B2" → "TEMPERATURE")

  // 第一遍：构建网格，处理合并单元格
  var grid = [];
  for (var ri = 0; ri < trs.length; ri++) {
    if (!grid[ri]) grid[ri] = [];
    var tds = trs[ri].querySelectorAll('td');
    var ci = 0;
    for (var di = 0; di < tds.length; di++) {
      var td = tds[di];
      // 跳过已被合并占用的格子
      while (grid[ri][ci] !== undefined) ci++;

      var text = td.textContent.trim();
      var colspan = parseInt(td.getAttribute('colspan')) || 1;
      var rowspan = parseInt(td.getAttribute('rowspan')) || 1;

      // 记录合并区域
      if (colspan > 1 || rowspan > 1) {
        merges.push({
          s: { r: ri, c: ci },
          e: { r: ri + rowspan - 1, c: ci + colspan - 1 }
        });
      }

      // 填充网格
      for (var rs = 0; rs < rowspan; rs++) {
        for (var cs = 0; cs < colspan; cs++) {
          grid[ri + rs][ci + cs] = (rs === 0 && cs === 0) ? text : null;
        }
      }

      // 识别 ${字段名} 占位符
      var fieldMatch = text.match(FIELD_PATTERN);
      if (fieldMatch) {
        var cellRef = colNumToLetter(ci) + (ri + 1);
        // 取第一个字段名（一个单元格通常只有一个占位符）
        var fieldName = fieldMatch[0].substring(2, fieldMatch[0].length - 1);
        cellFieldMap[cellRef] = fieldName;
      }

      ci += colspan;
    }
  }

  // 第二遍：构建行数据
  for (var ri = 0; ri < grid.length; ri++) {
    var row = [];
    if (grid[ri]) {
      for (var ci = 0; ci < grid[ri].length; ci++) {
        row.push(grid[ri][ci] !== undefined ? grid[ri][ci] : '');
      }
    }
    rows.push(row);
  }

  return { rows: rows, merges: merges, cellFieldMap: cellFieldMap };
}

/**
 * 将模板公式转换为 Excel 公式
 * @param {string} formula - 模板公式，如 "$avg([${A},${B}],2)" 或 "${A}+${B}"
 * @param {Object} fieldCellMap - 字段名 → 单元格坐标映射 (如 "TEMPERATURE" → "B2")
 * @returns {string} Excel 公式，如 "=AVERAGE(B2,C2)" 或 "=B2+C2"
 */
function convertTemplateFormulaToExcel(formula, fieldCellMap) {
  if (!formula) return '';

  // 检查是否是计量专用函数（直接保留 $ 前缀）
  for (var i = 0; i < CUSTOM_FUNCTIONS.length; i++) {
    if (formula.indexOf(CUSTOM_FUNCTIONS[i]) === 0) {
      // 替换 ${FIELD} 为单元格引用
      var result = formula.replace(FIELD_PATTERN, function(match, fieldName) {
        var cellRef = fieldCellMap[fieldName];
        return cellRef || match;
      });
      return '=' + result;
    }
  }

  // 标准模板函数转换
  // 处理 $t([...]) → =SUM(...)
  var templateFnMatch = formula.match(/^\$(\w+)\(\[([^\]]*)\](?:\s*,\s*(.*))?\)$/);
  if (templateFnMatch) {
    var fnName = '$' + templateFnMatch[1];
    var fieldsStr = templateFnMatch[2];
    var extraArgs = templateFnMatch[3];

    // 提取字段列表
    var fields = [];
    var m;
    var localPattern = /\$\{([^\}]+)\}/g;
    while ((m = localPattern.exec(fieldsStr)) !== null) {
      fields.push(m[1]);
    }

    // 转换为单元格引用
    var cellRefs = fields.map(function(f) {
      return fieldCellMap[f] || '${' + f + '}';
    });

    var excelFn = TEMPLATE_TO_EXCEL[fnName];
    if (excelFn) {
      // 如果字段连续，使用范围引用 (如 A2:A5)
      var rangeRef = tryMakeRange(cellRefs);
      if (rangeRef) {
        return '=' + excelFn + '(' + rangeRef + (extraArgs ? ',' + extraArgs : '') + ')';
      }
      return '=' + excelFn + '(' + cellRefs.join(',') + (extraArgs ? ',' + extraArgs : '') + ')';
    }

    // 特殊处理
    if (fnName === '$pow2' && fields.length === 1) {
      return '=' + cellRefs[0] + '^2';
    }
  }

  // 简单算术表达式：${A}+${B} → =B2+C2
  var result = formula.replace(FIELD_PATTERN, function(match, fieldName) {
    var cellRef = fieldCellMap[fieldName];
    return cellRef || match;
  });

  return '=' + result;
}

/**
 * 尝试将连续的单元格引用转换为范围引用
 * 如 ["A2","A3","A4","A5"] → "A2:A5"
 */
function tryMakeRange(cellRefs) {
  if (cellRefs.length < 2) return null;

  var first = cellRefs[0].match(/^([A-Z]+)(\d+)$/);
  var last = cellRefs[cellRefs.length - 1].match(/^([A-Z]+)(\d+)$/);
  if (!first || !last) return null;

  // 同一列且行号连续
  if (first[1] === last[1]) {
    var startRow = parseInt(first[2]);
    var endRow = parseInt(last[2]);
    if (endRow - startRow === cellRefs.length - 1) {
      return first[1] + startRow + ':' + first[1] + endRow;
    }
  }

  return null;
}

/**
 * 将 itemEditor 的 HTML 和字段定义导出为 Excel
 * @param {string} htmlValue - 富文本 HTML（含 <table>）
 * @param {Array} fields - 字段定义数组
 * @returns {ArrayBuffer} .xlsx 文件数据
 */
function exportToExcel(htmlValue, fields) {
  var parsed = parseHtmlTable(htmlValue);
  var rows = parsed.rows;
  var merges = parsed.merges;
  var cellFieldMap = parsed.cellFieldMap; // 单元格坐标 → 字段名

  // 反转映射: 字段名 → 单元格坐标
  var fieldCellMap = {};
  for (var cellRef in cellFieldMap) {
    fieldCellMap[cellFieldMap[cellRef]] = cellRef;
  }

  // 构建 worksheet 数据 (AOA: Array of Arrays)
  var wsData = [];
  for (var ri = 0; ri < rows.length; ri++) {
    var wsRow = [];
    for (var ci = 0; ci < rows[ri].length; ci++) {
      var cellValue = rows[ri][ci];

      // 如果这个单元格是字段占位符，检查该字段是否有公式
      var cellRef = colNumToLetter(ci) + (ri + 1);
      var fieldName = cellFieldMap[cellRef];
      if (fieldName && fields) {
        var field = fields.find(function(f) { return f.field === fieldName; });
        if (field && field.formula) {
          // 有公式的字段：在 Excel 中写入公式
          var excelFormula = convertTemplateFormulaToExcel(field.formula, fieldCellMap);
          // 公式作为单元格公式写入
          wsRow.push({ f: excelFormula });
          continue;
        }
      }

      wsRow.push(cellValue);
    }
    wsData.push(wsRow);
  }

  // 创建 worksheet
  var ws = XLSX.utils.aoa_to_sheet(wsData);

  // 设置合并单元格
  if (merges.length > 0) {
    ws['!merges'] = merges;
  }

  // 设置列宽
  var colCount = rows.length > 0 ? Math.max.apply(null, rows.map(function(r) { return r.length; })) : 1;
  ws['!cols'] = [];
  for (var i = 0; i < colCount; i++) {
    ws['!cols'].push({ wch: 15 });
  }

  // 创建 workbook
  var wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');

  // 导出为 ArrayBuffer
  var wbout = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
  return wbout;
}

// =============================================
// 导入: Excel → itemEditor
// =============================================

/**
 * 将 Excel 公式转换为模板公式
 * @param {string} excelFormula - Excel 公式，如 "=SUM(A2:A5)" 或 "=A2+B2"
 * @param {Object} cellFieldMap - 单元格坐标 → 字段名映射
 * @returns {string} 模板公式，如 "$t([${F1},...],2)" 或 "${F1}+${F2}"
 */
function convertExcelFormulaToTemplate(excelFormula, cellFieldMap) {
  if (!excelFormula) return '';

  // 去掉前导 =
  var formula = excelFormula;
  if (formula.charAt(0) === '=') {
    formula = formula.substring(1);
  }

  // 检查是否是计量专用函数（$ 前缀）
  for (var i = 0; i < CUSTOM_FUNCTIONS.length; i++) {
    if (formula.indexOf(CUSTOM_FUNCTIONS[i]) === 0) {
      // 替换单元格引用为 ${字段名}
      var result = formula.replace(CELL_REF_PATTERN, function(match, col, row) {
        var fieldName = cellFieldMap[match];
        return fieldName ? '${' + fieldName + '}' : match;
      });
      return result;
    }
  }

  // 标准Excel函数转换
  // 处理 SUM(A2:A5) → $t([${F1},${F2},...])
  var fnMatch = formula.match(/^(\w+(?:\.\w+)?)\(([^)]+)\)$/);
  if (fnMatch) {
    var excelFn = fnMatch[1];
    var argsStr = fnMatch[2];
    var templateFn = EXCEL_TO_TEMPLATE[excelFn];

    if (templateFn) {
      // 解析参数
      var args = parseFunctionArgs(argsStr, cellFieldMap);

      if (args.fields.length > 0) {
        var fieldsArray = args.fields.map(function(f) { return '${' + f + '}'; });
        return templateFn + '([' + fieldsArray.join(',') + ']' + (args.extra ? ',' + args.extra : '') + ')';
      }
    }

    // 特殊处理: MAX(A2:A5)-MIN(A2:A5) → $maxmin([...])
    // 这个在上层调用中处理（因为包含运算符）
  }

  // 处理 A^2 → $pow2(${FIELD})
  var powMatch = formula.match(/^([A-Z]+\d+)\^2$/);
  if (powMatch) {
    var fieldName = cellFieldMap[powMatch[1]];
    if (fieldName) {
      return '$pow2(${' + fieldName + '})';
    }
  }

  // 处理 ABS(A2-B2) → $abAbs(${F1},${F2})
  var abAbsMatch = formula.match(/^ABS\(([A-Z]+\d+)-([A-Z]+\d+)\)$/);
  if (abAbsMatch) {
    var f1 = cellFieldMap[abAbsMatch[1]];
    var f2 = cellFieldMap[abAbsMatch[2]];
    if (f1 && f2) {
      return '$abAbs(${' + f1 + '},${' + f2 + '})';
    }
  }

  // 处理 SQRT(SUMSQ(A2:A5)) → $sqrtpow([...])
  var sqrtSumsqMatch = formula.match(/^SQRT\(SUMSQ\(([^)]+)\)\)$/);
  if (sqrtSumsqMatch) {
    var args = parseRangeOrRefs(sqrtSumsqMatch[1], cellFieldMap);
    if (args.length > 0) {
      var fieldsArray = args.map(function(f) { return '${' + f + '}'; });
      return '$sqrtpow([' + fieldsArray.join(',') + '])';
    }
  }

  // 处理 MAX(A2:A5)-MIN(A2:A5) → $maxmin([...])
  var maxMinMatch = formula.match(/^MAX\(([^)]+)\)-MIN\(([^)]+)\)$/);
  if (maxMinMatch) {
    var maxArgs = parseRangeOrRefs(maxMinMatch[1], cellFieldMap);
    var minArgs = parseRangeOrRefs(maxMinMatch[2], cellFieldMap);
    // 合并去重
    var allFields = maxArgs.concat(minArgs.filter(function(f) { return maxArgs.indexOf(f) < 0; }));
    if (allFields.length > 0) {
      var fieldsArray = allFields.map(function(f) { return '${' + f + '}'; });
      return '$maxmin([' + fieldsArray.join(',') + '])';
    }
  }

  // 简单表达式：替换单元格引用为 ${字段名}
  var result = formula.replace(CELL_REF_PATTERN, function(match, col, row) {
    var fieldName = cellFieldMap[match];
    return fieldName ? '${' + fieldName + '}' : match;
  });

  return result;
}

/**
 * 解析函数参数，分离字段引用和额外参数
 */
function parseFunctionArgs(argsStr, cellFieldMap) {
  var parts = argsStr.split(',');
  var fields = [];
  var extra = [];

  for (var i = 0; i < parts.length; i++) {
    var part = parts[i].trim();
    // 检查是否是范围引用 (A2:A5)
    var rangeMatch = part.match(/^([A-Z]+)(\d+):([A-Z]+)(\d+)$/);
    if (rangeMatch) {
      var startCol = colLetterToNum(rangeMatch[1]);
      var startRow = parseInt(rangeMatch[2]);
      var endCol = colLetterToNum(rangeMatch[3]);
      var endRow = parseInt(rangeMatch[4]);

      if (startCol === endCol) {
        // 同一列，展开行
        for (var r = startRow; r <= endRow; r++) {
          var ref = rangeMatch[1] + r;
          var fn = cellFieldMap[ref];
          if (fn) fields.push(fn);
        }
      }
      continue;
    }

    // 检查是否是单元格引用
    var refMatch = part.match(/^([A-Z]+\d+)$/);
    if (refMatch) {
      var fn = cellFieldMap[part];
      if (fn) {
        fields.push(fn);
      }
      continue;
    }

    // 其他参数（数字、字符串等）
    extra.push(part);
  }

  return { fields: fields, extra: extra.join(',') };
}

/**
 * 解析范围引用或逗号分隔的引用列表
 */
function parseRangeOrRefs(str, cellFieldMap) {
  var fields = [];
  var parts = str.split(',');
  for (var i = 0; i < parts.length; i++) {
    var part = parts[i].trim();
    var rangeMatch = part.match(/^([A-Z]+)(\d+):([A-Z]+)(\d+)$/);
    if (rangeMatch) {
      var startCol = colLetterToNum(rangeMatch[1]);
      var startRow = parseInt(rangeMatch[2]);
      var endCol = colLetterToNum(rangeMatch[3]);
      var endRow = parseInt(rangeMatch[4]);
      if (startCol === endCol) {
        for (var r = startRow; r <= endRow; r++) {
          var ref = rangeMatch[1] + r;
          var fn = cellFieldMap[ref];
          if (fn) fields.push(fn);
        }
      }
    } else {
      var refMatch = part.match(/^([A-Z]+\d+)$/);
      if (refMatch) {
        var fn = cellFieldMap[part];
        if (fn) fields.push(fn);
      }
    }
  }
  return fields;
}

/**
 * 将 Excel 工作簿导入为 itemEditor 的 HTML 和字段定义
 * @param {ArrayBuffer} xlsxData - .xlsx 文件数据
 * @param {Array} existingFields - 已有字段定义（用于属性合并）
 * @returns {{ value: string, fields: Array }}
 */
function importFromExcel(xlsxData, existingFields) {
  var wb = XLSX.read(xlsxData, { type: 'array' });
  var wsName = wb.SheetNames[0];
  var ws = wb.Sheets[wsName];

  // 读取为 HTML
  var html = XLSX.utils.sheet_to_html(ws, { id: 'table1' });

  // 读取为 JSON 获取单元格详细信息
  var range = XLSX.utils.decode_range(ws['!ref'] || 'A1');

  // 建立单元格坐标 → 字段名映射
  var cellFieldMap = {};
  var newFields = [];

  for (var ri = range.s.r; ri <= range.e.r; ri++) {
    for (var ci = range.s.c; ci <= range.e.c; ci++) {
      var cellRef = XLSX.utils.encode_cell({ r: ri, c: ci });
      var cell = ws[cellRef];
      if (!cell) continue;

      var text = (cell.v || '') + '';

      // 识别 ${字段名}
      var fieldMatch = text.match(FIELD_PATTERN);
      if (fieldMatch) {
        var fieldName = fieldMatch[0].substring(2, fieldMatch[0].length - 1);
        var excelCellRef = colNumToLetter(ci) + (ri + 1);
        cellFieldMap[excelCellRef] = fieldName;

        // 创建/合并字段定义
        var existingField = null;
        if (existingFields) {
          existingField = existingFields.find(function(f) { return f.field === fieldName; });
        }

        if (existingField) {
          // 保留已有属性，仅更新 formula
          newFields.push(Object.assign({}, existingField, { value: '' }));
        } else {
          // 创建默认字段定义
          newFields.push({
            field: fieldName,
            name: '',
            value: '',
            width: '100%',
            height: '100%',
            fieldType: 'text',
            textMore: false,
            readonly: false,
            isnotnull: false,
            dvalue: '',
            formula: '',
            minv: '',
            maxv: '',
            data: '',
            helpInfo: ''
          });
        }
      }
    }
  }

  // 第二遍：处理公式
  for (var ri = range.s.r; ri <= range.e.r; ri++) {
    for (var ci = range.s.c; ci <= range.e.c; ci++) {
      var cellRef = XLSX.utils.encode_cell({ r: ri, c: ci });
      var cell = ws[cellRef];
      if (!cell || !cell.f) continue;  // 没有公式的跳过

      var excelCellRef = colNumToLetter(ci) + (ri + 1);
      var fieldName = cellFieldMap[excelCellRef];
      if (fieldName) {
        var templateFormula = convertExcelFormulaToTemplate(cell.f, cellFieldMap);
        var field = newFields.find(function(f) { return f.field === fieldName; });
        if (field) {
          field.formula = templateFormula;
        }
      }
    }
  }

  // 清理 HTML：XLSX.utils.sheet_to_html 生成的 HTML 需要调整样式
  // 确保表格边框样式与现有模板一致
  html = html.replace(/<table>/, '<table style="border-top: 1px solid #333; border-left: 1px solid #333; border-spacing: 0;">');
  html = html.replace(/<td>/g, '<td style="border-bottom: 1px solid #333; border-right: 1px solid #333; padding: 0;">');
  html = html.replace(/<td /g, '<td style="border-bottom: 1px solid #333; border-right: 1px solid #333; padding: 0;" ');

  return {
    value: html,
    fields: newFields
  };
}

// =============================================
// 导出
// =============================================

module.exports = {
  exportToExcel: exportToExcel,
  importFromExcel: importFromExcel,
  parseHtmlTable: parseHtmlTable,
  convertTemplateFormulaToExcel: convertTemplateFormulaToExcel,
  convertExcelFormulaToTemplate: convertExcelFormulaToTemplate,
};
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/s01/m07/views/components/excelConverter.js
git commit -m "feat: add excelConverter.js for HTML table ↔ Excel conversion"
```

---

## Chunk 3: 前端 — OnlyOffice 弹窗编辑器组件

### Task 3: 创建 excel-editor.vue 弹窗组件

**Files:**
- Create: `p-admin/src/pages/s01/m07/views/components/excel-editor.vue`

**背景**：复用现有 `rs-onlyoffice-preview` 组件的 OnlyOffice API 加载逻辑（单例模式，`DocsAPI` 脚本只加载一次）。使用 `rs-modal` 弹窗容器，内部挂载 OnlyOffice Document Editor（cell 类型，编辑模式）。

- [ ] **Step 1: 创建 excel-editor.vue**

```vue
<template>
  <rs-modal ref="modal" :width="1200" style="z-index:9999">
    <div class="excel-editor-wrapper">
      <div v-if="loading" class="excel-editor-loading">
        <i class="h-icon-loading" style="font-size: 32px;"></i>
        <p>正在加载编辑器...</p>
      </div>
      <div v-if="error" class="excel-editor-error">
        <i class="h-icon-error" style="font-size: 32px; color: #ed4014;"></i>
        <p>{{ error }}</p>
        <Button @click.native="retry">重试</Button>
      </div>
      <div :id="editorId" class="excel-editor-container"></div>
    </div>
    <template slot="footer">
      <Button @click.native="cancel">取消</Button>
      <Button color="primary" @click.native="saveAndClose">保存并返回</Button>
    </template>
  </rs-modal>
</template>

<script>
import db from '@/api/db';
import excelConverter from './excelConverter';

// OnlyOffice API 脚本加载状态（全局单例）
var scriptLoaded = false;
var scriptLoading = false;
var scriptCallbacks = [];

var ONLYOFFICE_URL = 'http://localhost:8088';

function loadOnlyOfficeScript() {
  return new Promise(function(resolve, reject) {
    if (scriptLoaded && window.DocsAPI) {
      resolve();
      return;
    }
    if (scriptLoading) {
      scriptCallbacks.push({ resolve: resolve, reject: reject });
      return;
    }
    scriptLoading = true;
    scriptCallbacks.push({ resolve: resolve, reject: reject });

    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = ONLYOFFICE_URL + '/web-apps/apps/api/documents/api.js';
    script.onload = function() {
      scriptLoaded = true;
      scriptLoading = false;
      scriptCallbacks.forEach(function(cb) { cb.resolve(); });
      scriptCallbacks = [];
    };
    script.onerror = function() {
      scriptLoading = false;
      scriptCallbacks.forEach(function(cb) { cb.reject(new Error('OnlyOffice API 加载失败')); });
      scriptCallbacks = [];
    };
    document.head.appendChild(script);
  });
}

export default {
  name: 'ExcelEditor',
  data: function() {
    return {
      editorId: 'excel-editor-' + Math.random().toString(36).substr(2, 9),
      loading: false,
      error: '',
      docEditor: null,
      fileKey: '',
      itemEditorData: null,  // 存储传入的 itemEditor 数据
    };
  },
  beforeDestroy: function() {
    this.destroyEditor();
  },
  methods: {
    /**
     * 打开 Excel 编辑器
     * @param {Object} data - { value: html, fields: [...] }
     */
    open: function(data) {
      var self = this;
      self.itemEditorData = data;
      self.loading = true;
      self.error = '';
      self.$refs.modal.show();

      self.$nextTick(function() {
        self.initEditor();
      });
    },

    initEditor: async function() {
      var self = this;
      try {
        // 1. 将 HTML 表格转换为 Excel
        var xlsxData = excelConverter.exportToExcel(
          self.itemEditorData.value,
          self.itemEditorData.fields
        );

        // 2. 上传到后端
        var blob = new Blob([xlsxData], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        var formData = new FormData();
        formData.append('file', blob, 'template.xlsx');

        var uploadUrl = db.getUrl('upload');
        var apiUrl = db.getUrl('url');
        var uploadResponse = await new Promise(function(resolve, reject) {
          var xhr = new XMLHttpRequest();
          xhr.open('POST', apiUrl + '/api/exceleditor/upload');
          xhr.setRequestHeader('Authorization', 'Bearer ' + self.$store.state['user'].access_token);
          xhr.onload = function() {
            if (xhr.status === 200) {
              resolve(JSON.parse(xhr.responseText));
            } else {
              reject(new Error('上传失败: ' + xhr.status));
            }
          };
          xhr.onerror = function() { reject(new Error('网络错误')); };
          xhr.send(formData);
        });

        if (!uploadResponse.key) {
          throw new Error('上传返回无效');
        }

        self.fileKey = uploadResponse.key;

        // 3. 获取 OnlyOffice 编辑器配置
        var configResponse = await new Promise(function(resolve, reject) {
          var xhr = new XMLHttpRequest();
          xhr.open('GET', apiUrl + '/api/exceleditor/editor-config?key=' + self.fileKey);
          xhr.setRequestHeader('Authorization', 'Bearer ' + self.$store.state['user'].access_token);
          xhr.onload = function() {
            if (xhr.status === 200) {
              resolve(JSON.parse(xhr.responseText));
            } else {
              reject(new Error('获取配置失败: ' + xhr.status));
            }
          };
          xhr.onerror = function() { reject(new Error('网络错误')); };
          xhr.send();
        });

        // 4. 加载 OnlyOffice API 脚本
        await loadOnlyOfficeScript();

        if (!window.DocsAPI) {
          throw new Error('OnlyOffice API 不可用');
        }

        // 5. 初始化编辑器
        self.destroyEditor();

        var config = Object.assign({}, configResponse, {
          events: {
            onDocumentReady: function() {
              self.loading = false;
            },
            onError: function(event) {
              self.error = '编辑器错误: ' + (event.data.errorDescription || '未知错误');
              self.loading = false;
            }
          }
        });

        self.docEditor = new window.DocsAPI.DocEditor(self.editorId, config);
      } catch (e) {
        console.error('Excel 编辑器初始化失败', e);
        self.error = e.message || '编辑器加载失败';
        self.loading = false;
      }
    },

    /**
     * 保存并返回
     */
    saveAndClose: async function() {
      var self = this;
      if (!self.docEditor) {
        self.$error('编辑器未就绪');
        return;
      }

      var busy = self.$busy('正在保存...');

      try {
        // 触发 OnlyOffice 强制保存
        self.docEditor.triggerSave();

        // 等待保存完成（给 OnlyOffice 回调处理时间）
        await new Promise(function(resolve) { setTimeout(resolve, 2000); });

        // 从后端下载保存后的 Excel
        var apiUrl = db.getUrl('url');
        var xlsxData = await new Promise(function(resolve, reject) {
          var xhr = new XMLHttpRequest();
          xhr.open('GET', apiUrl + '/api/exceleditor/download?key=' + self.fileKey);
          xhr.setRequestHeader('Authorization', 'Bearer ' + self.$store.state['user'].access_token);
          xhr.responseType = 'arraybuffer';
          xhr.onload = function() {
            if (xhr.status === 200) {
              resolve(xhr.response);
            } else {
              reject(new Error('下载失败: ' + xhr.status));
            }
          };
          xhr.onerror = function() { reject(new Error('网络错误')); };
          xhr.send();
        });

        // 转换为 HTML + fields
        var result = excelConverter.importFromExcel(xlsxData, self.itemEditorData.fields);

        // 通知父组件
        self.$emit('save', result);

        self.$free(busy);
        self.$alert('保存成功');
        self.close();
      } catch (e) {
        self.$free(busy);
        self.$error('保存失败: ' + e.message);
      }
    },

    cancel: function() {
      this.close();
    },

    close: function() {
      this.destroyEditor();
      this.$refs.modal.hide();
    },

    retry: function() {
      this.error = '';
      this.loading = true;
      this.initEditor();
    },

    destroyEditor: function() {
      if (this.docEditor) {
        try {
          this.docEditor.destroyEditor();
        } catch (e) {
          // 忽略销毁错误
        }
        this.docEditor = null;
      }
    }
  }
};
</script>

<style lang="less" scoped>
.excel-editor-wrapper {
  width: 100%;
  height: calc(100vh - 250px);
  min-height: 500px;
  position: relative;
}
.excel-editor-loading,
.excel-editor-error {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 100%;
  color: #999;
  font-size: 14px;
  p {
    margin-top: 10px;
  }
}
.excel-editor-container {
  width: 100%;
  height: 100%;
  overflow: hidden;
}
</style>
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/pages/s01/m07/views/components/excel-editor.vue
git commit -m "feat: add excel-editor.vue OnlyOffice popup component"
```

---

## Chunk 4: 前端 — 集成到模板编辑器

### Task 4: 在 rs-set-attr.vue 中集成"Excel编辑"按钮

**Files:**
- Modify: `p-admin/src/pages/s01/m07/views/components/rs-set-attr.vue`

**背景**：`rs-set-attr.vue` 是右侧属性面板，当 `attr.type==='itemEditor'` 时显示字段列表。需要在字段列表上方增加"Excel编辑"按钮，点击后打开 `excel-editor.vue` 弹窗。

- [ ] **Step 1: 修改 rs-set-attr.vue — 引入组件和添加按钮**

在 `<script>` 的 `components` 中注册 `excelEditor`：

```javascript
// 在 import styleAttr 后面添加
import excelEditor from './excel-editor.vue';
```

```javascript
components: {
  styleAttr,
  setEditor,
  excelEditor,  // 新增
},
```

在 `data()` 中添加：

```javascript
data() {
  return {
    // ... 现有字段
    excelEditorVisible: false,  // 新增
  };
},
```

在 `methods` 中添加：

```javascript
openExcelEditor() {
  this.$refs.excelEditor.open({
    value: this.attr.value,
    fields: this.attr.fields,
  });
},
onExcelSave({ value, fields }) {
  this.attr.value = value;
  this.attr.fields = fields;
},
```

在 `<template>` 的 `itemEditor` 区域（约第 40-82 行之间），在 `<textarea>` 后面、字段列表前面，添加按钮和弹窗组件：

```html
<!-- 在 <textarea v-model="SFIELDS" ...></textarea> 后面添加 -->
<div class="list-item">
  <span></span>
  <div class="list-right">
    <Button color="primary" size="s" @click.native="openExcelEditor">Excel编辑</Button>
  </div>
</div>
<excel-editor ref="excelEditor" @save="onExcelSave" />
```

- [ ] **Step 2: 验证页面可正常加载**

Run: `cd /Users/wanghu/work/project/hs2.0/p-admin && npm run dev`
Expected: 开发服务器正常启动，访问 s01/m07 页面不报错

- [ ] **Step 3: Commit**

```bash
git add p-admin/src/pages/s01/m07/views/components/rs-set-attr.vue
git commit -m "feat: integrate Excel editor button into itemEditor attribute panel"
```

---

### Task 5: 在 ueditor/index2.vue 编辑模式下添加"Excel编辑"快捷入口

**Files:**
- Modify: `p-admin/src/components/edit/ueditor/index2.vue`

**背景**：`ueditor/index2.vue` 在 `inLayout=true` 时显示 wangEditor 工具栏和编辑区。在工具栏区域增加一个"Excel编辑"按钮，让用户在编辑富文本内容时也能快速打开 Excel 编辑器。这个按钮需要向上查找到父组件 `itemEditor` 的数据。

- [ ] **Step 1: 修改 ueditor/index2.vue — 添加 Excel 编辑按钮**

在模板 `<div class="toolbar" ref="toolbar">` 前面添加：

```html
<div class="excel-edit-btn" v-if="inLayout" @click="openExcelEditor" title="使用Excel编辑表格">
  <span class="h-icon-edit" style="margin-right:4px;"></span>Excel编辑
</div>
```

在 `<script>` 的 `methods` 中添加：

```javascript
openExcelEditor() {
  // 向上查找 editTemplate 页面中的 excelEditor 组件
  // 通过事件冒泡让 rs-set-attr 处理
  this.$emit('excel-edit');
},
```

在 `<style>` 中添加：

```css
.excel-edit-btn {
  display: inline-block;
  padding: 4px 10px;
  margin-bottom: 2px;
  background: #4b9efd;
  color: #fff;
  border-radius: 3px;
  cursor: pointer;
  font-size: 12px;
  vertical-align: middle;
}
.excel-edit-btn:hover {
  background: #3a8eec;
}
```

- [ ] **Step 2: Commit**

```bash
git add p-admin/src/components/edit/ueditor/index2.vue
git commit -m "feat: add Excel edit shortcut button in ueditor toolbar"
```

---

## Chunk 5: 集成测试与完善

### Task 6: 端到端测试与修复

**Files:**
- Possibly modify: `excelConverter.js`, `excel-editor.vue`, `ExcelEditorController.cs`

- [ ] **Step 1: 启动后端和前端服务**

```bash
# 终端1: 启动后端
cd /Users/wanghu/work/project/hs2.0/netcore && dotnet run --project Realso.WebAPI

# 终端2: 启动前端
cd /Users/wanghu/work/project/hs2.0/p-admin && npm run dev
```

- [ ] **Step 2: 测试完整流程**

1. 访问 s01/m07 列表页面
2. 选择一个待提交状态的模板，点击"编辑模板"
3. 添加一个"富文本"控件
4. 在富文本控件中粘贴一个含表格的 HTML
5. 选中富文本控件，在右侧属性面板点击"Excel编辑"按钮
6. 验证 OnlyOffice 弹窗打开，Excel 中显示表格内容
7. 在 Excel 中修改内容（添加 `${TEST_FIELD}` 占位符、修改公式）
8. 点击"保存并返回"
9. 验证富文本内容已更新，字段列表已自动生成
10. 在字段列表中验证公式已正确转换

- [ ] **Step 3: 修复发现的问题**

根据测试结果修复代码。

- [ ] **Step 4: 最终 Commit**

```bash
git add -A
git commit -m "fix: fix issues found during integration testing"
```

---

## 文件结构总览

```
p-admin/src/pages/s01/m07/views/components/
  excel-editor.vue       ← 新增: OnlyOffice 弹窗编辑器组件
  excelConverter.js      ← 新增: HTML ↔ Excel 双向转换引擎
  rs-set-attr.vue        ← 修改: 添加"Excel编辑"按钮
  set-editor.vue         ← 不变
  style-attr.vue         ← 不变
  rs-add-field.vue       ← 不变
  fieldTem.js            ← 不变

p-admin/src/components/edit/ueditor/
  index2.vue             ← 修改: 工具栏添加"Excel编辑"快捷按钮

netcore/Realso.WebAPI/Controllers/
  ExcelEditorController.cs  ← 新增: 临时文件上传/下载/回调 API
  FileController.cs         ← 不变
```
