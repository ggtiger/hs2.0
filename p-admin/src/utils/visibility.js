// 统一显隐条件求值
//
// 显隐条件（visibleIf）在业务页（visibilityHost）上定义，支持两种形态：
//   1) computed：ISSHOWCUSTTYPE() { return this.xxx; }  —— 取其值
//   2) method：  ISSHOWCUSTTYPE(ctx) { return ctx.row.STATE === 1; } —— 调用并传入标准化 ctx
//
// 统一 ctx 形状：{ row, key, path }
//   row  —— 当前数据行（表单 model；列表/按钮场景无则 undefined，method 内可直接读 host 上的 this.checks 等）
//   key  —— 字段/按钮/列的 key
//   path —— DataTable 路径名（MAIN/QRY/DTSA 等），用于字段名重复时消歧
//
// 规则：
//   - 无 host 或条件名 → 恒显
//   - host 上未定义该 computed/method → 恒显（向后兼容）
//   - 值为 function → method(ctx)，取真值
//   - 否则当作 computed，取真值
export function evalVisibility(host, visIf, ctx) {
  if (!host || !visIf) return true;
  const target = host[visIf];
  if (target === undefined) return true;
  if (typeof target === 'function') return !!target.call(host, ctx);
  return !!target;
}
