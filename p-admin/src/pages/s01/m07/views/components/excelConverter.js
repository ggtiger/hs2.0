'use strict';

var XLSX = require('xlsx-js-style');

// =============================================
// Excel 函数 ↔ 模板公式 映射表
// =============================================

var EXCEL_TO_TEMPLATE = {
  'SUM': '$t',
  'AVERAGE': '$avg',
  'ABS': '$abs',
  'SQRT': '$sqrt',
  'LN': '$log',
  'ROUND': '$fixed',
  'STDEV.S': '$stdev',
};

var TEMPLATE_TO_EXCEL = {
  '$t': 'SUM',
  '$avg': 'AVERAGE',
  '$abs': 'ABS',
  '$sqrt': 'SQRT',
  '$log': 'LN',
  '$fixed': 'ROUND',
  '$stdev': 'STDEV.S',
  '$round': 'ROUND',
};

var CUSTOM_FUNCTIONS = [
  '$indError', '$std', '$maxStd', '$maxAbs', '$minAbs',
  '$avgStd', '$maxmin', '$maxminStd', '$sqrtpow', '$abAbs', '$round'
];

var FIELD_PATTERN = /\$\{([^\}]+)\}/g;
var CELL_REF_PATTERN = /\b([A-Z]+)(\d+)\b/g;

function colLetterToNum(letter) {
  var num = 0;
  for (var i = 0; i < letter.length; i++) {
    num = num * 26 + (letter.charCodeAt(i) - 64);
  }
  return num - 1;
}

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
// 样式转换工具
// =============================================

/**
 * 解析 CSS 样式字符串为键值对象
 */
function parseCssStyle(styleStr) {
  var style = {};
  if (!styleStr) return style;
  styleStr.split(';').forEach(function(part) {
    var kv = part.split(':');
    if (kv.length === 2) {
      style[kv[0].trim()] = kv[1].trim();
    }
  });
  return style;
}

/**
 * 从 HTML td 元素提取 Excel 单元格样式
 */
function extractCellStyle(td) {
  var css = parseCssStyle(td.getAttribute('style') || '');
  var s = {};

  // 字体
  var font = {};
  var fontSize = css['font-size'] || css['fontSize'];
  if (fontSize) {
    var pt = parseInt(fontSize);
    if (pt) font.sz = pt;
  }
  var fontWeight = css['font-weight'] || css['fontWeight'];
  if (fontWeight === 'bold' || parseInt(fontWeight) >= 700) {
    font.bold = true;
  }
  var fontStyle = css['font-style'] || css['fontStyle'];
  if (fontStyle === 'italic') {
    font.italic = true;
  }
  if (Object.keys(font).length > 0) s.font = font;

  // 对齐
  var alignment = {};
  var textAlign = css['text-align'] || css['textAlign'];
  if (textAlign === 'center') alignment.horizontal = 'center';
  else if (textAlign === 'right') alignment.horizontal = 'right';
  else if (textAlign === 'left') alignment.horizontal = 'left';
  var verticalAlign = css['vertical-align'] || css['verticalAlign'];
  if (verticalAlign === 'middle' || verticalAlign === 'center') alignment.vertical = 'center';
  else if (verticalAlign === 'top') alignment.vertical = 'top';
  else if (verticalAlign === 'bottom') alignment.vertical = 'bottom';
  if (Object.keys(alignment).length > 0) s.alignment = alignment;

  // 背景色
  var bgColor = css['background-color'] || css['backgroundColor'];
  if (bgColor && bgColor !== 'transparent' && bgColor !== 'none') {
    var rgb = parseColor(bgColor);
    if (rgb) {
      s.fill = { fgColor: { rgb: rgb } };
    }
  }

  // 边框 — 处理简写 border 和方向性 border-top/bottom/left/right
  var hasBorder = false;
  // 简写形式: border:1px solid #000
  var borderShort = css['border'];
  if (borderShort && borderShort !== 'none' && borderShort !== '0') {
    hasBorder = true;
  }
  // 方向性形式: border-top/bottom/left/right
  if (css['border-top'] || css['border-bottom'] || css['border-left'] || css['border-right']) {
    hasBorder = true;
  }
  // 默认给所有单元格加细边框
  s.border = {
    top: { style: 'thin', color: { rgb: '000000' } },
    bottom: { style: 'thin', color: { rgb: '000000' } },
    left: { style: 'thin', color: { rgb: '000000' } },
    right: { style: 'thin', color: { rgb: '000000' } }
  };

  return s;
}

/**
 * 解析颜色字符串为 RGB 十六进制
 */
function parseColor(colorStr) {
  if (!colorStr) return null;
  // #rrggbb
  var hexMatch = colorStr.match(/^#([0-9a-fA-F]{6})$/);
  if (hexMatch) return hexMatch[1].toUpperCase();
  // rgb(r,g,b)
  var rgbMatch = colorStr.match(/^rgb\((\d+),\s*(\d+),\s*(\d+)\)$/);
  if (rgbMatch) {
    var r = parseInt(rgbMatch[1]).toString(16).padStart(2, '0');
    var g = parseInt(rgbMatch[2]).toString(16).padStart(2, '0');
    var b = parseInt(rgbMatch[3]).toString(16).padStart(2, '0');
    return (r + g + b).toUpperCase();
  }
  // 常见颜色名
  var namedColors = {
    'black': '000000',
    'white': 'FFFFFF',
    'red': 'FF0000',
    'green': '00FF00',
    'blue': '0000FF',
    'yellow': 'FFFF00',
    'gray': '808080',
    'grey': '808080'
  };
  return namedColors[colorStr.toLowerCase()] || null;
}

/**
 * 从 Excel 单元格样式生成 HTML style 属性
 */
function excelStyleToHtml(cellStyle) {
  if (!cellStyle) return '';
  var parts = [];

  // 字体
  if (cellStyle.font) {
    if (cellStyle.font.sz) parts.push('font-size:' + cellStyle.font.sz + 'px');
    if (cellStyle.font.bold) parts.push('font-weight:bold');
    if (cellStyle.font.italic) parts.push('font-style:italic');
  }

  // 对齐
  if (cellStyle.alignment) {
    if (cellStyle.alignment.horizontal) parts.push('text-align:' + cellStyle.alignment.horizontal);
    if (cellStyle.alignment.vertical === 'center') parts.push('vertical-align:middle');
    else if (cellStyle.alignment.vertical) parts.push('vertical-align:' + cellStyle.alignment.vertical);
  }

  // 背景色
  if (cellStyle.fill && cellStyle.fill.fgColor && cellStyle.fill.fgColor.rgb) {
    var rgb = cellStyle.fill.fgColor.rgb;
    if (rgb !== 'FFFFFF' && rgb !== 'ffffff') {
      parts.push('background-color:#' + rgb);
    }
  }

  // 边框 — 从 Excel 样式提取边框信息
  if (cellStyle.border) {
    var hasBorder = false;
    var borderParts = [];
    if (cellStyle.border.top && cellStyle.border.top.style) {
      hasBorder = true;
      var color = cellStyle.border.top.color;
      var borderColor = (color && color.rgb) ? '#' + color.rgb : '#000';
      borderParts.push('border-top:1px solid ' + borderColor);
    }
    if (cellStyle.border.bottom && cellStyle.border.bottom.style) {
      hasBorder = true;
      var color2 = cellStyle.border.bottom.color;
      var borderColor2 = (color2 && color2.rgb) ? '#' + color2.rgb : '#000';
      borderParts.push('border-bottom:1px solid ' + borderColor2);
    }
    if (cellStyle.border.left && cellStyle.border.left.style) {
      hasBorder = true;
      var color3 = cellStyle.border.left.color;
      var borderColor3 = (color3 && color3.rgb) ? '#' + color3.rgb : '#000';
      borderParts.push('border-left:1px solid ' + borderColor3);
    }
    if (cellStyle.border.right && cellStyle.border.right.style) {
      hasBorder = true;
      var color4 = cellStyle.border.right.color;
      var borderColor4 = (color4 && color4.rgb) ? '#' + color4.rgb : '#000';
      borderParts.push('border-right:1px solid ' + borderColor4);
    }
    if (hasBorder) {
      parts = borderParts.concat(parts);
    }
  }

  return parts.join(';');
}

// =============================================
// 导出: itemEditor → Excel
// =============================================

/**
 * 解析 HTML 表格，提取单元格数据、样式和合并信息
 */
function parseHtmlTable(html) {
  var parser = new DOMParser();
  var doc = parser.parseFromString(html, 'text/html');
  var table = doc.querySelector('table');
  if (!table) {
    return { rows: [], merges: [], cellFieldMap: {}, cellStyles: [], colWidthsPx: [], rowHeights: [] };
  }

  var trs = table.querySelectorAll('tr');
  var rows = [];
  var merges = [];
  var cellFieldMap = {};
  var cellStyles = []; // [row][col] = Excel 样式对象
  var colWidthsPx = []; // 每列宽度（像素），从 HTML 中提取
  var colMaxTextLen = []; // 每列最大文本长度（用于宽度估算）
  var rowHeights = []; // 每行高度（px）

  // 解析 <colgroup>/<col> 中的列宽信息（最可靠的宽度来源）
  var cols = table.querySelectorAll('col');
  if (cols.length > 0) {
    for (var ci = 0; ci < cols.length; ci++) {
      var colWidthAttr = cols[ci].getAttribute('width');
      if (colWidthAttr) {
        // <col width="xxx"> 通常是像素值
        var pxVal = parseInt(colWidthAttr);
        if (pxVal > 0) colWidthsPx[ci] = pxVal;
      }
      // 也检查 style 中的 width
      var colStyle = parseCssStyle(cols[ci].getAttribute('style') || '');
      var colStyleWidth = colStyle['width'];
      if (colStyleWidth) {
        var pxMatch = colStyleWidth.match(/^(\d+(?:\.\d+)?)(px|pt|em|rem|%)?$/);
        if (pxMatch) {
          var val = parseFloat(pxMatch[1]);
          var unit = pxMatch[2] || 'px';
          if (unit === 'px') colWidthsPx[ci] = Math.max(colWidthsPx[ci] || 0, val);
          else if (unit === 'pt') colWidthsPx[ci] = Math.max(colWidthsPx[ci] || 0, val * 1.33);
          else if (unit === 'em' || unit === 'rem') colWidthsPx[ci] = Math.max(colWidthsPx[ci] || 0, val * 16);
        }
      }
    }
  }

  // 解析 <colgroup> 样式中的 width（某些 UEditor 格式）
  var colgroups = table.querySelectorAll('colgroup');
  for (var cgi = 0; cgi < colgroups.length; cgi++) {
    var cgCols = colgroups[cgi].querySelectorAll('col');
    for (var cgi2 = 0; cgi2 < cgCols.length; cgi2++) {
      var cgColWidth = cgCols[cgi2].getAttribute('width');
      if (cgColWidth) {
        var cgPx = parseInt(cgColWidth);
        if (cgPx > 0) colWidthsPx[cgi2] = Math.max(colWidthsPx[cgi2] || 0, cgPx);
      }
    }
  }

  // 第一遍：构建网格，处理合并单元格
  var grid = [];
  var styleGrid = [];
  for (var ri = 0; ri < trs.length; ri++) {
    if (!grid[ri]) grid[ri] = [];
    if (!styleGrid[ri]) styleGrid[ri] = [];

    // 行高
    var trStyle = parseCssStyle(trs[ri].getAttribute('style') || '');
    var trHeight = trStyle['height'];
    if (trHeight) {
      var h = parseInt(trHeight);
      if (h) rowHeights[ri] = Math.max(rowHeights[ri] || 0, h);
    }

    var tds = trs[ri].querySelectorAll('td');
    var colIdx = 0;
    for (var di = 0; di < tds.length; di++) {
      var td = tds[di];
      while (grid[ri][colIdx] !== undefined) colIdx++;

      var text = td.textContent.trim();
      var colspan = parseInt(td.getAttribute('colspan')) || 1;
      var rowspan = parseInt(td.getAttribute('rowspan')) || 1;

      // 提取单元格样式
      var cellStyle = extractCellStyle(td);

      if (colspan > 1 || rowspan > 1) {
        merges.push({
          s: { r: ri, c: colIdx },
          e: { r: ri + rowspan - 1, c: colIdx + colspan - 1 }
        });
      }

      for (var rs = 0; rs < rowspan; rs++) {
        if (!grid[ri + rs]) grid[ri + rs] = [];
        if (!styleGrid[ri + rs]) styleGrid[ri + rs] = [];
        for (var cs = 0; cs < colspan; cs++) {
          grid[ri + rs][colIdx + cs] = (rs === 0 && cs === 0) ? text : null;
          styleGrid[ri + rs][colIdx + cs] = (rs === 0 && cs === 0) ? cellStyle : null;
        }
      }

      // 识别 ${字段名} 占位符
      var fieldMatch = text.match(FIELD_PATTERN);
      if (fieldMatch) {
        var cellRef = colNumToLetter(colIdx) + (ri + 1);
        var fieldName = fieldMatch[0].substring(2, fieldMatch[0].length - 1);
        cellFieldMap[cellRef] = fieldName;
      }

      // 估算列宽（像素）— 记录每列最大文本长度，后续统一计算
      var textLen = text.length;
      var displayLen = textLen;
      var fieldContentMatch = text.match(/^\$\{([^\}]+)\}$/);
      if (fieldContentMatch) {
        displayLen = fieldContentMatch[1].length + 2; // 字段名 + 2
      }
      // 仅记录最大文本长度，不直接设宽度（后面统一按比例分配）
      if (displayLen > 0 && (!colMaxTextLen[colIdx] || displayLen > colMaxTextLen[colIdx])) {
        colMaxTextLen[colIdx] = displayLen;
      }

      // td 的 width 属性（通常是像素值）
      var tdWidth = td.getAttribute('width');
      if (tdWidth) {
        var tdWidthPx = parseInt(tdWidth);
        if (tdWidthPx > 0) {
          colWidthsPx[colIdx] = Math.max(colWidthsPx[colIdx] || 0, tdWidthPx);
        }
      }

      // td 的 CSS width 样式
      var tdCss = parseCssStyle(td.getAttribute('style') || '');
      var tdCssWidth = tdCss['width'];
      if (tdCssWidth) {
        var pxMatch2 = tdCssWidth.match(/^(\d+(?:\.\d+)?)(px|pt|em|rem|%)?$/);
        if (pxMatch2) {
          var val2 = parseFloat(pxMatch2[1]);
          var unit2 = pxMatch2[2] || 'px';
          var convertedPx = val2;
          if (unit2 === 'pt') convertedPx = val2 * 1.33;
          else if (unit2 === 'em' || unit2 === 'rem') convertedPx = val2 * 16;
          else if (unit2 === '%') convertedPx = 0; // 百分比无法直接转像素，跳过
          if (convertedPx > 0) {
            colWidthsPx[colIdx] = Math.max(colWidthsPx[colIdx] || 0, convertedPx);
          }
        }
      }

      colIdx += colspan;
    }
  }

  // 第二遍：构建行数据
  for (var rIdx = 0; rIdx < grid.length; rIdx++) {
    var row = [];
    var styleRow = [];
    if (grid[rIdx]) {
      for (var cIdx = 0; cIdx < grid[rIdx].length; cIdx++) {
        row.push(grid[rIdx][cIdx] !== undefined ? grid[rIdx][cIdx] : '');
        styleRow.push(styleGrid[rIdx] && styleGrid[rIdx][cIdx] ? styleGrid[rIdx][cIdx] : {});
      }
    }
    rows.push(row);
    cellStyles.push(styleRow);
  }

  // 统一计算列宽：如果没有 <colgroup> 提供宽度，用文本长度按比例分配 A4 宽度
  // A4 纵向 ≈ 495px（按 7.5px/wch × 66wch）
  var hasColWidthInfo = false;
  for (var hci = 0; hci < colWidthsPx.length; hci++) {
    if (colWidthsPx[hci] && colWidthsPx[hci] > 0) {
      hasColWidthInfo = true;
      break;
    }
  }
  if (!hasColWidthInfo && colMaxTextLen.length > 0) {
    var A4_TOTAL_PX = 495;
    var totalTextLen = 0;
    var maxCol = rows.length > 0 ? Math.max.apply(null, rows.map(function(r) { return r.length })) : colMaxTextLen.length;
    for (var tci = 0; tci < maxCol; tci++) {
      totalTextLen += (colMaxTextLen[tci] || 6);
    }
    // 按文本长度比例分配 A4 总宽度
    for (var tci2 = 0; tci2 < maxCol; tci2++) {
      var textRatio = (colMaxTextLen[tci2] || 6) / totalTextLen;
      colWidthsPx[tci2] = Math.max(30, Math.round(textRatio * A4_TOTAL_PX));
    }
  }

  return {
    rows: rows,
    merges: merges,
    cellFieldMap: cellFieldMap,
    cellStyles: cellStyles,
    colWidthsPx: colWidthsPx,
    rowHeights: rowHeights
  };
}

function tryMakeRange(cellRefs) {
  if (cellRefs.length < 2) return null;
  var first = cellRefs[0].match(/^([A-Z]+)(\d+)$/);
  var last = cellRefs[cellRefs.length - 1].match(/^([A-Z]+)(\d+)$/);
  if (!first || !last) return null;
  if (first[1] === last[1]) {
    var startRow = parseInt(first[2]);
    var endRow = parseInt(last[2]);
    if (endRow - startRow === cellRefs.length - 1) {
      return first[1] + startRow + ':' + first[1] + endRow;
    }
  }
  return null;
}

function convertTemplateFormulaToExcel(formula, fieldCellMap) {
  if (!formula) return '';

  for (var i = 0; i < CUSTOM_FUNCTIONS.length; i++) {
    if (formula.indexOf(CUSTOM_FUNCTIONS[i]) === 0) {
      var result = formula.replace(FIELD_PATTERN, function(match, fieldName) {
        var cellRef = fieldCellMap[fieldName];
        return cellRef || match;
      });
      return '=' + result;
    }
  }

  var templateFnMatch = formula.match(/^\$(\w+)\(\[([^\]]*)\](?:\s*,\s*(.*))?\)$/);
  if (templateFnMatch) {
    var fnName = '$' + templateFnMatch[1];
    var fieldsStr = templateFnMatch[2];
    var extraArgs = templateFnMatch[3];

    var fields = [];
    var m;
    var localPattern = /\$\{([^\}]+)\}/g;
    while ((m = localPattern.exec(fieldsStr)) !== null) {
      fields.push(m[1]);
    }

    var cellRefs = fields.map(function(f) {
      return fieldCellMap[f] || '${' + f + '}';
    });

    var excelFn = TEMPLATE_TO_EXCEL[fnName];
    if (excelFn) {
      var rangeRef = tryMakeRange(cellRefs);
      if (rangeRef) {
        return '=' + excelFn + '(' + rangeRef + (extraArgs ? ',' + extraArgs : '') + ')';
      }
      return '=' + excelFn + '(' + cellRefs.join(',') + (extraArgs ? ',' + extraArgs : '') + ')';
    }

    if (fnName === '$pow2' && fields.length === 1) {
      return '=' + cellRefs[0] + '^2';
    }
  }

  var result = formula.replace(FIELD_PATTERN, function(match, fieldName) {
    var cellRef = fieldCellMap[fieldName];
    return cellRef || match;
  });

  return '=' + result;
}

/**
 * 将 itemEditor 的 HTML 和字段定义导出为 Excel（保留样式）
 */
function exportToExcel(htmlValue, fields) {
  var parsed = parseHtmlTable(htmlValue);
  var rows = parsed.rows;
  var merges = parsed.merges;
  var cellFieldMap = parsed.cellFieldMap;
  var cellStyles = parsed.cellStyles;
  var colWidthsPx = parsed.colWidthsPx;
  var rowHeights = parsed.rowHeights;

  // 反转映射: 字段名 → 单元格坐标
  var fieldCellMap = {};
  for (var cellRef in cellFieldMap) {
    fieldCellMap[cellFieldMap[cellRef]] = cellRef;
  }

  // 先用 aoa_to_sheet 创建基础 worksheet（只包含值）
  var aoaData = [];
  for (var rIdx = 0; rIdx < rows.length; rIdx++) {
    var aoaRow = [];
    for (var cIdx = 0; cIdx < rows[rIdx].length; cIdx++) {
      var cellValue = rows[rIdx][cIdx];
      var cellRef = colNumToLetter(cIdx) + (rIdx + 1);
      var fieldName = cellFieldMap[cellRef];

      if (fieldName && fields) {
        var field = fields.find(function(f) { return f.field === fieldName });
        if (field && field.formula) {
          aoaRow.push(null); // 公式单元格，值留空，后面单独设置
        } else {
          aoaRow.push(cellValue);
        }
      } else {
        aoaRow.push(cellValue);
      }
    }
    aoaData.push(aoaRow);
  }

  var ws = XLSX.utils.aoa_to_sheet(aoaData);

  // 逐个单元格设置样式和公式
  // 先构建合并单元格查找表：找出哪些单元格是合并区域内部单元格，应继承起始单元格样式
  var mergeStartStyle = {}; // "R_C" → style对象（合并起始单元格的样式）
  for (var mi = 0; mi < merges.length; mi++) {
    var merge = merges[mi];
    var startStyle = (cellStyles[merge.s.r] && cellStyles[merge.s.r][merge.s.c]) ? cellStyles[merge.s.r][merge.s.c] : {};
    if (startStyle && Object.keys(startStyle).length > 0) {
      // 将起始单元格的样式应用到合并区域的所有单元格
      for (var mr = merge.s.r; mr <= merge.e.r; mr++) {
        for (var mc = merge.s.c; mc <= merge.e.c; mc++) {
          mergeStartStyle[mr + '_' + mc] = startStyle;
        }
      }
    }
  }

  for (var rIdx2 = 0; rIdx2 < rows.length; rIdx2++) {
    for (var cIdx2 = 0; cIdx2 < rows[rIdx2].length; cIdx2++) {
      var addr = colNumToLetter(cIdx2) + (rIdx2 + 1);
      var cell = ws[addr];
      if (!cell) continue;

      // 设置样式 — 优先使用合并起始单元格的样式
      var style = mergeStartStyle[rIdx2 + '_' + cIdx2] ||
        ((cellStyles[rIdx2] && cellStyles[rIdx2][cIdx2]) ? cellStyles[rIdx2][cIdx2] : {});
      if (style && Object.keys(style).length > 0) {
        cell.s = style;
      }

      // 设置公式
      var fieldName2 = cellFieldMap[addr];
      if (fieldName2 && fields) {
        var field2 = fields.find(function(f) { return f.field === fieldName2 });
        if (field2 && field2.formula) {
          var excelFormula = convertTemplateFormulaToExcel(field2.formula, fieldCellMap);
          if (excelFormula) {
            cell.f = excelFormula;
            // 公式单元格：清空缓存值，让 Excel 自动计算
            delete cell.v;
            delete cell.t;
          }
        }
      }
    }
  }

  // 合并单元格
  if (merges.length > 0) {
    ws['!merges'] = merges;
  }

  // 列宽 — 像素转 wch，并约束 A4 纸张宽度
  // A4 纵向 ≈ 66 wch（Excel 默认列宽单位），A4 横向 ≈ 102 wch
  var A4_MAX_WCH = 66;
  var pxPerWch = 7.5; // 1 wch ≈ 7.5px（11pt Calibri）
  var colCount = rows.length > 0 ? Math.max.apply(null, rows.map(function(r) { return r.length })) : 1;
  var colWchList = [];
  var totalWch = 0;
  for (var ci = 0; ci < colCount; ci++) {
    var pxVal = colWidthsPx[ci] || 0;
    var wchVal = pxVal > 0 ? Math.round(pxVal / pxPerWch) : 10; // 默认 10 wch
    if (wchVal < 4) wchVal = 4;
    if (wchVal > 50) wchVal = 50;
    colWchList.push(wchVal);
    totalWch += wchVal;
  }
  // 如果总宽度超过 A4，按比例缩小
  if (totalWch > A4_MAX_WCH) {
    var scale = A4_MAX_WCH / totalWch;
    for (var ci2 = 0; ci2 < colWchList.length; ci2++) {
      colWchList[ci2] = Math.max(4, Math.round(colWchList[ci2] * scale));
    }
  }
  ws['!cols'] = [];
  for (var ci3 = 0; ci3 < colCount; ci3++) {
    // 同时设置 wch 和 width，确保 OnlyOffice 正确读取列宽
    ws['!cols'][ci3] = { wch: colWchList[ci3], width: colWchList[ci3] };
  }

  // 行高
  ws['!rows'] = [];
  for (var ri = 0; ri < rows.length; ri++) {
    ws['!rows'][ri] = { hpt: rowHeights[ri] || 20 };
  }

  var wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');

  var wbout = XLSX.write(wb, { bookType: 'xlsx', type: 'array', cellStyles: true });
  return wbout;
}

// =============================================
// 导入: Excel → itemEditor
// =============================================

function parseFunctionArgs(argsStr, cellFieldMap) {
  var parts = argsStr.split(',');
  var fields = [];
  var extra = [];

  for (var i = 0; i < parts.length; i++) {
    var part = parts[i].trim();
    var rangeMatch = part.match(/^([A-Z]+)(\d+):([A-Z]+)(\d+)$/);
    if (rangeMatch) {
      var startRow = parseInt(rangeMatch[2]);
      var endRow = parseInt(rangeMatch[4]);
      if (rangeMatch[1] === rangeMatch[3]) {
        for (var r = startRow; r <= endRow; r++) {
          var ref = rangeMatch[1] + r;
          var fn = cellFieldMap[ref];
          if (fn) fields.push(fn);
        }
      }
      continue;
    }

    var refMatch = part.match(/^([A-Z]+\d+)$/);
    if (refMatch) {
      var fn = cellFieldMap[part];
      if (fn) {
        fields.push(fn);
      }
      continue;
    }

    extra.push(part);
  }

  return { fields: fields, extra: extra.join(',') };
}

function parseRangeOrRefs(str, cellFieldMap) {
  var fields = [];
  var parts = str.split(',');
  for (var i = 0; i < parts.length; i++) {
    var part = parts[i].trim();
    var rangeMatch = part.match(/^([A-Z]+)(\d+):([A-Z]+)(\d+)$/);
    if (rangeMatch) {
      var startRow = parseInt(rangeMatch[2]);
      var endRow = parseInt(rangeMatch[4]);
      if (rangeMatch[1] === rangeMatch[3]) {
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

function convertExcelFormulaToTemplate(excelFormula, cellFieldMap) {
  if (!excelFormula) return '';

  var formula = excelFormula;
  if (formula.charAt(0) === '=') {
    formula = formula.substring(1);
  }

  for (var i = 0; i < CUSTOM_FUNCTIONS.length; i++) {
    if (formula.indexOf(CUSTOM_FUNCTIONS[i]) === 0) {
      var result = formula.replace(CELL_REF_PATTERN, function(match, col, row) {
        var fieldName = cellFieldMap[match];
        return fieldName ? '${' + fieldName + '}' : match;
      });
      return result;
    }
  }

  var fnMatch = formula.match(/^(\w+(?:\.\w+)?)\(([^)]+)\)$/);
  if (fnMatch) {
    var excelFn = fnMatch[1];
    var argsStr = fnMatch[2];
    var templateFn = EXCEL_TO_TEMPLATE[excelFn];

    if (templateFn) {
      var args = parseFunctionArgs(argsStr, cellFieldMap);
      if (args.fields.length > 0) {
        var fieldsArray = args.fields.map(function(f) { return '${' + f + '}' });
        return templateFn + '([' + fieldsArray.join(',') + ']' + (args.extra ? ',' + args.extra : '') + ')';
      }
    }
  }

  var powMatch = formula.match(/^([A-Z]+\d+)\^2$/);
  if (powMatch) {
    var fieldName = cellFieldMap[powMatch[1]];
    if (fieldName) {
      return '$pow2(${' + fieldName + '})';
    }
  }

  var abAbsMatch = formula.match(/^ABS\(([A-Z]+\d+)-([A-Z]+\d+)\)$/);
  if (abAbsMatch) {
    var f1 = cellFieldMap[abAbsMatch[1]];
    var f2 = cellFieldMap[abAbsMatch[2]];
    if (f1 && f2) {
      return '$abAbs(${' + f1 + '},${' + f2 + '})';
    }
  }

  var sqrtSumsqMatch = formula.match(/^SQRT\(SUMSQ\(([^)]+)\)\)$/);
  if (sqrtSumsqMatch) {
    var args = parseRangeOrRefs(sqrtSumsqMatch[1], cellFieldMap);
    if (args.length > 0) {
      var fieldsArray = args.map(function(f) { return '${' + f + '}' });
      return '$sqrtpow([' + fieldsArray.join(',') + '])';
    }
  }

  var maxMinMatch = formula.match(/^MAX\(([^)]+)\)-MIN\(([^)]+)\)$/);
  if (maxMinMatch) {
    var maxArgs = parseRangeOrRefs(maxMinMatch[1], cellFieldMap);
    var minArgs = parseRangeOrRefs(maxMinMatch[2], cellFieldMap);
    var allFields = maxArgs.concat(minArgs.filter(function(f) { return maxArgs.indexOf(f) < 0 }));
    if (allFields.length > 0) {
      var fieldsArray = allFields.map(function(f) { return '${' + f + '}' });
      return '$maxmin([' + fieldsArray.join(',') + '])';
    }
  }

  var result = formula.replace(CELL_REF_PATTERN, function(match, col, row) {
    var fieldName = cellFieldMap[match];
    return fieldName ? '${' + fieldName + '}' : match;
  });

  return result;
}

// =============================================
// 导出
// =============================================

export {
  exportToExcel,
  parseHtmlTable,
  convertTemplateFormulaToExcel,
  convertExcelFormulaToTemplate,
};
