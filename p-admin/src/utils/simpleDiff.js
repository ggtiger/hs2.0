// 轻量差异对比工具（无第三方依赖）
// lineDiff: LCS 行级文本对比，输出 [{type:'same'|'add'|'del', text}]
// fieldDiff: JSON 字段级对比，输出 [{field, before, after, changed}]
// splitSnapshot: 把版本快照 JSON 拆成 文本大字段(走行级 diff) + 普通字段(走字段对比)

// LCS 行级 diff。大文件（m*n > 400万）降级为整体替换，避免内存/耗时爆炸
export function lineDiff(oldText, newText) {
  const a = (oldText == null ? '' : String(oldText)).split('\n');
  const b = (newText == null ? '' : String(newText)).split('\n');
  const m = a.length;
  const n = b.length;
  if (m * n > 4000000) {
    const ops = [];
    a.forEach(function(l) { ops.push({ type: 'del', text: l }) });
    b.forEach(function(l) { ops.push({ type: 'add', text: l }) });
    return ops;
  }
  const dp = [];
  for (let i = 0; i <= m; i++) dp.push(new Array(n + 1).fill(0));
  for (let i = m - 1; i >= 0; i--) {
    for (let j = n - 1; j >= 0; j--) {
      dp[i][j] = a[i] === b[j] ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]);
    }
  }
  const ops = [];
  let i = 0;
  let j = 0;
  while (i < m && j < n) {
    if (a[i] === b[j]) { ops.push({ type: 'same', text: a[i] }); i++; j++ } else if (dp[i + 1][j] >= dp[i][j + 1]) { ops.push({ type: 'del', text: a[i] }); i++ } else { ops.push({ type: 'add', text: b[j] }); j++ }
  }
  while (i < m) { ops.push({ type: 'del', text: a[i] }); i++ }
  while (j < n) { ops.push({ type: 'add', text: b[j] }); j++ }
  return ops;
}

function fmtVal(v) {
  if (v === undefined || v === null) return '';
  if (typeof v === 'object') return JSON.stringify(v);
  return String(v);
}

// 显示/比较前归一：ISO 与 MySQL 日期格式对齐（2026-07-18T01:59:26 → 2026-07-18 01:59:26）
// 版本前镜像来自 Dapper 序列化(ISO)，后镜像来自前端回传(MySQL 格式)，不归一会产生假变化
function normVal(v) {
  var s = fmtVal(v);
  if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(s)) s = s.replace('T', ' ');
  return s;
}

// JSON 字段级对比（before/after 为 JSON 字符串或对象）
export function fieldDiff(beforeJson, afterJson) {
  let b = {};
  let a = {};
  try { b = typeof beforeJson === 'string' ? (beforeJson ? JSON.parse(beforeJson) : {}) : (beforeJson || {}) } catch (e) {}
  try { a = typeof afterJson === 'string' ? (afterJson ? JSON.parse(afterJson) : {}) : (afterJson || {}) } catch (e) {}
  const keys = [];
  Object.keys(b).concat(Object.keys(a)).forEach(function(k) {
    if (keys.indexOf(k) < 0) keys.push(k);
  });
  return keys.map(function(k) {
    const bv = normVal(b[k]);
    const av = normVal(a[k]);
    return { field: k, before: bv, after: av, changed: bv !== av };
  });
}

// 把快照 JSON 拆成两部分：
// textFields  — 值较长的文本字段（如 SOURCECODE/SQLTXT，>120字符或含换行），走行级 diff
//               无论有无变化都生成（代码始终可见；无变化时 +0/-0 全量展示，有变化时差异高亮）
//               排序：有变化的排前面
// fieldRows   — 其余普通字段，走字段对比表
// 返回 { textFields: [{name, before, after, changed}], fieldRows: [{field, before, after, changed}] }
export function splitSnapshot(beforeJson, afterJson) {
  const rows = fieldDiff(beforeJson, afterJson);
  const textFields = [];
  const fieldRows = [];
  rows.forEach(function(r) {
    const isLong = (r.before && r.before.length > 120) || (r.after && r.after.length > 120) ||
      (r.before && r.before.indexOf('\n') >= 0) || (r.after && r.after.indexOf('\n') >= 0);
    // COMPILEDCODE 是编译产物（随 SOURCECODE 联动），不进文本 diff，避免噪音 tab
    if (isLong && r.field !== 'COMPILEDCODE') {
      textFields.push({ name: r.field, before: r.before, after: r.after, changed: r.changed });
    } else {
      fieldRows.push(r);
    }
  });
  textFields.sort(function(x, y) { return (y.changed ? 1 : 0) - (x.changed ? 1 : 0) });
  return { textFields, fieldRows };
}

// 统计行级 diff 的增删行数
export function diffStat(ops) {
  let add = 0;
  let del = 0;
  ops.forEach(function(o) {
    if (o.type === 'add') add++;
    if (o.type === 'del') del++;
  });
  return { add, del };
}
