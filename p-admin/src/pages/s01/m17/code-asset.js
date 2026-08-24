/**
 * 代码资产（API 脚本 C# / SQL 模板 / JS 模块）统一访问层
 * edit.vue（在线 IDE）与 code-editor-popup（模块脚本弹窗编辑器）共用，避免逻辑重复。
 * 原则：读写一律经数据源（getStoreResult → Store03 DataTable），不手拼 XML。
 * 通道：四类资产统一走 s01/m17 页面 store（同一 Store03 实例/同一 DataTable），
 *      不再使用 getGenericStore('RS_M17') 独立实例（双通道曾导致首次点击 open 未注册即 dispatch）。
 */
import Store from '@/store';
import db from '@/api/db';
import { getStoreResult } from './store';
import { compileSFC } from '@/sfc-loader';
import { ASSET_META as _ASSET_META, DEFAULT_CSHARP_TEMPLATE, DEFAULT_SQL_TEMPLATE, DEFAULT_JS_TEMPLATE, DEFAULT_VUE_TEMPLATE } from '@/constants';

// store 命名空间（vuex 模块名，非模块编码）
export const STORE_NS = 's01/m17';

// 常量 re-export（保持消费方 import 兼容）
export const ASSET_META = _ASSET_META;
export const defaultCsharpTemplate = DEFAULT_CSHARP_TEMPLATE;
export const defaultSqlTemplate = DEFAULT_SQL_TEMPLATE;
export const defaultJsTemplate = DEFAULT_JS_TEMPLATE;
export const defaultVueTemplate = DEFAULT_VUE_TEMPLATE;

export function assetStore(kind) {
  // 同步保证 vuex 模块已注册（任何 dispatch 之前必须先调用）
  return getStoreResult();
}

export function assetTable(kind) {
  return assetStore(kind).storeHelper.getTable('MAIN');
}

// ====== 目录推导（代码资产按模块组织：SC_/SS_ + 模块编码 前缀 → 模块目录） ======

var _moduleCodesCache = null;
var _moduleCodesLoading = null;

// 加载全部模块编码（RS_M02/A01 查 VSS_MOUDLE，模块级缓存）
export function loadModuleCodes() {
  if (_moduleCodesCache) return Promise.resolve(_moduleCodesCache);
  if (_moduleCodesLoading) return _moduleCodesLoading;
  _moduleCodesLoading = db.postData({
    api: '/api/data/call/RS_M02/A01/',
    params: { FilterParams: {}, PageSize: 500, PageIndex: 1 },
  }).then(function(ret) {
    var items = (ret && ret.Items) || [];
    _moduleCodesCache = items.map(function(m) { return m.MODULECODE }).filter(Boolean);
    return _moduleCodesCache;
  }).catch(function() {
    _moduleCodesCache = [];
    return _moduleCodesCache;
  });
  return _moduleCodesLoading;
}

// 从资产编码推导所属模块目录：
//   SC_R02_M07_BACK → R02_M07（最长匹配已知模块编码）
//   SC_SCRIPT_CHECK → 公共（无模块前缀）
// 返回值用于目录分组显示（IDE 文件树）与归属判断（自有/外链）
export function deriveAssetDir(code, kind, moduleCodes) {
  var prefix = kind === 'sql' ? 'SS_' : 'SC_';
  if (!code || code.indexOf(prefix) !== 0) return '公共';
  var rest = code.substring(prefix.length);
  var best = '';
  (moduleCodes || []).forEach(function(mc) {
    if (rest === mc || rest.indexOf(mc + '_') === 0) {
      if (mc.length > best.length) best = mc;
    }
  });
  return best || '公共';
}

// 重组编码前缀（目录编辑：SC_OLD_MOD_X → SC_NEW_MOD_X）
// dir 为 '公共' 时去掉模块段
export function recomposeAssetCode(code, kind, newDir, moduleCodes) {
  var prefix = kind === 'sql' ? 'SS_' : 'SC_';
  var rest = (code || '').indexOf(prefix) === 0 ? code.substring(prefix.length) : (code || '');
  var oldDir = deriveAssetDir(code, kind, moduleCodes);
  var name = rest;
  if (oldDir !== '公共' && rest.indexOf(oldDir + '_') === 0) {
    name = rest.substring(oldDir.length + 1);
  } else if (rest === oldDir) {
    name = '';
  }
  if (!newDir || newDir === '公共') return prefix + name;
  return prefix + newDir + '_' + name;
}

// 从 MODULEPATH 推导 TEMPLATECODE（新 JS 模块行用，TEMPLATECODE 非空约束）
// 格式 {模块编码}_{页面编码}：目录段(模块编码)大写 + 文件名原样
// '@/modules/LIB_M01/add.js' → 'LIB_M01_add'；'@/pages/demo.js' → 'DEMO'
export function deriveTplCode(modulePath) {
  var parts = (modulePath || '').split('/').filter(function(p) { return !!p });
  var file = parts.length > 0 ? parts[parts.length - 1].replace(/\.(js|vue)$/i, '') : 'unnamed';
  var dir = parts.length > 1 ? parts[parts.length - 2] : '';
  return (dir ? dir.toUpperCase() + '_' : '') + file;
}

// 推导脚本路径（csharp/sql 的路径约定: @/scripts/{模块}/{编码}.{ext}; 公共 → @/scripts/{编码}.{ext}）
export async function deriveScriptPath(kind, code, moduleCodes) {
  var dir = deriveAssetDir(code, kind, moduleCodes || (await loadModuleCodes()));
  var ext = kind === 'csharp' ? '.cs' : '.sql';
  if (dir === '公共') return '@/scripts/' + code + ext;
  return '@/scripts/' + dir + '/' + code + ext;
}

// 打开资产到 DataTable（F00 按 ID；sql 的 F00 是 A.SQLID=@ID，传 SQLID 即可）
export async function openAsset(kind, idValue) {
  assetStore(kind);
  await Store.dispatch(STORE_NS + '/open', { ID: idValue });
  return assetTable(kind);
}

// ====== AI 多文件联动（接口 → store → 页面 JS 全链路一次落地） ======

// 解析 AI 多文件输出：###FILE: {路径} 段 → [{path, code}]；无标记返回 null
export function parseAiFileBlocks(text) {
  if (!text) return null;
  var re = /^###FILE:\s*(\S+)\s*$/gm;
  var marks = [];
  var m;
  while ((m = re.exec(text)) !== null) {
    marks.push({ path: m[1], index: m.index, end: re.lastIndex });
  }
  if (marks.length === 0) return null;
  var ops = [];
  for (var i = 0; i < marks.length; i++) {
    var start = marks[i].end;
    var end = i + 1 < marks.length ? marks[i + 1].index : text.length;
    var code = text.substring(start, end).replace(/^\n+/, '').replace(/\s+$/, '');
    // 去掉尾部残留的 markdown 围栏
    code = code.replace(/\n```\s*$/, '');
    if (code) ops.push({ path: marks[i].path, code: code });
  }
  return ops.length > 0 ? ops : null;
}

// 按路径后缀判断资产类型（AI 文件块路由用）
export function kindOfPath(path) {
  var p = (path || '').toLowerCase();
  if (p.slice(-3) === '.cs') return 'csharp';
  if (p.slice(-4) === '.sql') return 'sql';
  if (p.slice(-3) === '.js') return 'js';
  if (p.slice(-4) === '.vue') return 'vue';
  return '';
}

function fileNameOf(path) {
  var n = path.substring(path.lastIndexOf('/') + 1);
  return n.replace(/\.(cs|sql|js|vue)$/i, '');
}

// 打开资产: js/vue 按 MODULEPATH(A06), csharp/sql 按 CODE(A01)；找不到返回 null
async function openAssetByPathOrCode(kind, key) {
  if (kind === 'js' || kind === 'vue') {
    var ret = await db.postData({ api: '/api/data/call/RS_M17/A06/', params: { FilterParams: { MODULEPATH: key } } });
    var rows = (ret && ret.Items) || [];
    return rows.length > 0 ? openAsset('js', rows[0].ID) : null;
  }
  var r2 = await db.postData({
    api: '/api/data/call/RS_M17/A01/',
    params: { FilterParams: { ASSETTYPE: kind, CODE: key }, PageSize: 5, PageIndex: 1 },
  });
  var items = (r2 && r2.Items) || [];
  var found = null;
  items.forEach(function(x) { if (x.CODE === key) found = x; });
  return found ? openAsset(kind, found.ID) : null;
}

// VUE 资产保存（编译 + 全字段 setValue + save；与 IDE handleSave 的 sfc 流程一致）
async function saveVueAsset(path, source, name, changeNote) {
  var result = await compileSFC(source, path, 'VUE');
  var dt = await openAssetByPathOrCode('js', path); // js/vue 都按 MODULEPATH 定位
  if (!dt) await addAsset('js');
  dt = assetTable('js');
  if (!dt.getValue('CODE')) dt.setValue('CODE', deriveTplCode(path));
  dt.setValue('NAME', name);
  dt.setValue('MODULEPATH', path);
  dt.setValue('FILETYPE', 'VUE');
  dt.setValue('SOURCECODE', source);
  dt.setValue('COMPILEDCODE', result.compiledCode);
  dt.setValue('DEPS', JSON.stringify(result.deps));
  dt.setValue('ASSETTYPE', 'vue');
  dt.setValue('ISDELETED', '0');
  var now = new Date().toISOString().replace('T', ' ').substring(0, 19);
  dt.setValue('MODIFYTIME', now);
  if (!dt.getValue('CREATETIME')) dt.setValue('CREATETIME', now);
  await Store.dispatch(STORE_NS + '/save', { CHANGENOTE: changeNote || '' });
  return { passed: true, id: dt.getValue('ID'), deps: result.deps };
}

// 执行 AI 多文件落库：脚本类先保存并自动关联模块接口(按序分配接口码 A51+)，js/vue 随后
// ctx: { moduleCode, currentPath, applyCurrent(path, code) }
// 返回 { saved: [], linked: [], skipped: [], errors: [] }
export async function applyAiFileOps(ops, ctx) {
  var result = { saved: [], linked: [], skipped: [], errors: [] };
  var orderMap = { csharp: 0, sql: 1, js: 2, vue: 3 };
  var ordered = ops.slice().sort(function(a, b) {
    return (orderMap[kindOfPath(a.path)] !== undefined ? orderMap[kindOfPath(a.path)] : 9) -
           (orderMap[kindOfPath(b.path)] !== undefined ? orderMap[kindOfPath(b.path)] : 9);
  });
  for (var i = 0; i < ordered.length; i++) {
    var op = ordered[i];
    var kind = kindOfPath(op.path);
    try {
      if (ctx.currentPath && op.path === ctx.currentPath) {
        ctx.applyCurrent(op.path, op.code);
        result.saved.push(op.path + '(当前文件)');
        continue;
      }
      if (!kind) {
        result.skipped.push(op.path + '(仅支持 cs/sql/js/vue)');
        continue;
      }
      var code = fileNameOf(op.path);
      // VUE 组件: 编译后落库（不走 saveAsset——其校验/SQL 规则不适用）
      if (kind === 'vue') {
        await saveVueAsset(op.path, op.code, code, 'AI 多文件生成');
        result.saved.push(op.path);
        continue;
      }
      var dt = await openAssetByPathOrCode(kind, kind === 'js' ? op.path : code);
      if (!dt) await addAsset(kind);
      var ret = await saveAsset(kind, {
        code: kind === 'js' ? deriveTplCode(op.path) : code,
        name: code,
        source: op.code,
        path: op.path,
        changeNote: 'AI 多文件生成',
      });
      if (!ret.passed) {
        result.errors.push(op.path + ': ' + ret.message);
        continue;
      }
      result.saved.push(op.path);
      // 脚本类自动关联模块接口（幂等，按序分配接口码，与提示词推导规则一致）
      if (ctx.moduleCode && (kind === 'csharp' || kind === 'sql')) {
        try {
          var link = await db.postData({
            api: '/api/data/call/RS_M18/A07/',
            params: { MODULECODE: ctx.moduleCode, KIND: kind, CODE: code, APINAME: code },
          });
          if (link && link.apiCode) result.linked.push(code + ' → ' + link.apiCode);
        } catch (le) {
          result.errors.push(code + ' 关联失败: ' + (le.message || le));
        }
      }
    } catch (e) {
      result.errors.push(op.path + ': ' + (e.message || e));
    }
  }
  return result;
}

// 新建资产空行（INIT+ADD）
export async function addAsset(kind) {
  assetStore(kind);
  await Store.dispatch(STORE_NS + '/add');
  return assetTable(kind);
}

// 编译/规则校验：csharp → RS_M21 A05 Roslyn 检查；sql → 前端铁律校验；js → compileSFC
// 返回 { passed, message, errors?, deps?, compiledCode? }
export async function checkAsset(kind, sourceCode, modulePath) {
  if (kind === 'csharp') {
    var ret = await db.postData({
      api: '/api/data/call/RS_M21/A05/',
      params: { SOURCECODE: sourceCode },
    });
    if (ret && ret.passed) return { passed: true, message: '编译通过' };
    return {
      passed: false,
      message: '编译失败: ' + (((ret && ret.errors) || []).join('；') || (ret && ret.message) || '未知错误'),
      errors: (ret && ret.errors) || [],
    };
  }
  if (kind === 'js') {
    try {
      var result = await compileSFC(sourceCode, modulePath || '@/modules/未命名.js', 'JS');
      return {
        passed: true,
        message: '编译成功, 依赖: ' + (result.deps.length > 0 ? result.deps.join(', ') : '无'),
        deps: result.deps,
        compiledCode: result.compiledCode,
      };
    } catch (e) {
      return { passed: false, message: '编译失败: ' + (e.message || e) };
    }
  }
  if (kind === 'vue') {
    try {
      var result2 = await compileSFC(sourceCode, modulePath || '@/pages/未命名.vue', 'VUE');
      return {
        passed: true,
        message: '编译成功, 依赖: ' + (result2.deps.length > 0 ? result2.deps.join(', ') : '无'),
        deps: result2.deps,
        compiledCode: result2.compiledCode,
      };
    } catch (e) {
      return { passed: false, message: '编译失败: ' + (e.message || e) };
    }
  }
  // sql: NVelocity 铁律 + DDL 黑名单（前端校验）
  var src = (sourceCode || '').trim();
  if (!src) return { passed: false, message: 'SQL 内容为空' };
  if (src.indexOf('\'') >= 0) {
    return { passed: false, message: '含单引号：NVelocity 解析会失败，请改用 @参数 或 CHAR(39)' };
  }
  // 头部关键字/DDL 判断前先剥注释（-- 行注释与 /* 块注释 不执行，不应参与判断）
  // 单引号检查仍针对全文（NVelocity 解析的是整个模板文本，注释里的单引号同样会炸）
  var noComments = src
    .replace(/\/\*[\s\S]*?\*\//g, ' ')
    .replace(/--[^\n]*/g, ' ')
    .trim();
  if (!noComments) return { passed: false, message: 'SQL 内容为空（只有注释）' };
  var head = noComments.substring(0, 12).toUpperCase();
  if (!/^(SELECT|WITH|SHOW|INSERT|UPDATE|DELETE)/.test(head)) {
    return { passed: false, message: '必须以 SELECT/WITH/SHOW/INSERT/UPDATE/DELETE 开头' };
  }
  var ddl = noComments.match(/\b(DROP|ALTER|TRUNCATE|CREATE|GRANT|REVOKE)\s/i);
  if (ddl) return { passed: false, message: '含 DDL 关键字 ' + ddl[1] + '：模板禁止 DDL' };
  return { passed: true, message: '规则校验通过（无单引号 / 无 DDL）' };
}

// 脚本类路径守卫: 提供的路径文件名主干与编码不一致(改编码后路径未跟进) → 按编码重新推导
async function guardScriptPath(kind, code, path) {
  if (!path) return deriveScriptPath(kind, code);
  var ext = kind === 'csharp' ? '.cs' : '.sql';
  var file = path.substring(path.lastIndexOf('/') + 1);
  if (file !== code + ext) return deriveScriptPath(kind, code);
  return path;
}

// 保存资产：先校验，再 DataTable setValue → Store03 save（getXML 自动生成，后端回写 ID）
// values: { code, name, source, remark, version, changeNote(提交时填的版本说明), skipVersion(true=快速保存不留版本) }
// js 的 code = MODULEPATH（路径即身份），保存时重算 COMPILEDCODE/DEPS
// 返回 { passed, message, id, version, deps? }
export async function saveAsset(kind, values) {
  var check = await checkAsset(kind, values.source, values.code);
  if (!check.passed) return check;
  var meta = ASSET_META[kind];
  var dt = assetTable(kind);
  if (!dt) throw new Error('DataTable 未初始化');
  dt.setValue(meta.codeField, values.code);
  dt.setValue(meta.nameField, values.name);
  if (kind === 'csharp') {
    dt.setValue(meta.sourceField, values.source);
    dt.setValue('VERSION', values.version);
    dt.setValue(meta.remarkField, values.remark || '');
    dt.setValue('MODULEPATH', await guardScriptPath('csharp', values.code, values.path));
    dt.setValue('ASSETTYPE', 'csharp');
    dt.setValue('ISDELETED', '0');
  } else if (kind === 'js') {
    // JS 模块: CODE 非空(从路径推导 {目录}_{文件名大写}) + MODULEPATH + 源码 + 编译产物 + 依赖 + 时间戳
    if (!dt.getValue('CODE')) {
      dt.setValue('CODE', deriveTplCode(values.path || values.code));
    }
    dt.setValue('MODULEPATH', values.path || values.code);
    dt.setValue(meta.sourceField, values.source);
    dt.setValue('COMPILEDCODE', check.compiledCode || '');
    dt.setValue('DEPS', JSON.stringify(check.deps || []));
    dt.setValue('FILETYPE', 'JS');
    dt.setValue('ASSETTYPE', 'js');
    dt.setValue(meta.remarkField, values.remark || '');
    dt.setValue('ISDELETED', '0');
    var now = new Date().toISOString().replace('T', ' ').substring(0, 19);
    dt.setValue('MODIFYTIME', now);
    if (!dt.getValue('CREATETIME')) {
      dt.setValue('CREATETIME', now);
    }
  } else if (kind === 'vue') {
    // Vue 组件: 与 JS 类似，但 FILETYPE=VUE, ASSETTYPE=vue
    if (!dt.getValue('CODE')) {
      dt.setValue('CODE', deriveTplCode(values.path || values.code));
    }
    dt.setValue('MODULEPATH', values.path || values.code);
    dt.setValue(meta.sourceField, values.source);
    dt.setValue('COMPILEDCODE', check.compiledCode || '');
    dt.setValue('DEPS', JSON.stringify(check.deps || []));
    dt.setValue('FILETYPE', 'VUE');
    dt.setValue('ASSETTYPE', 'vue');
    dt.setValue(meta.remarkField, values.remark || '');
    dt.setValue('ISDELETED', '0');
    var now2 = new Date().toISOString().replace('T', ' ').substring(0, 19);
    dt.setValue('MODIFYTIME', now2);
    if (!dt.getValue('CREATETIME')) {
      dt.setValue('CREATETIME', now2);
    }
  } else {
    dt.setValue('SQLTYPE', 'mysql');
    dt.setValue(meta.sourceField, values.source);
    dt.setValue('MODULEPATH', await guardScriptPath('sql', values.code, values.path));
    dt.setValue('ASSETTYPE', 'sql');
    dt.setValue('ISDELETED', '0');
    dt.setValue(meta.remarkField, values.remark || '');
  }
  await Store.dispatch(STORE_NS + '/save', { CHANGENOTE: values.changeNote || '', SKIPVERSION: values.skipVersion ? '1' : null });
  return {
    passed: true,
    message: '保存成功',
    id: dt.getValue(meta.idField),
    version: kind === 'csharp' ? +(dt.getValue('VERSION') || values.version) : 0,
    deps: check.deps || [],
  };
}
