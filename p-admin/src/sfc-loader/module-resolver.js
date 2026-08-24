/**
 * 模块解析器 — __sfc_require__ + 路径解析 + 预加载
 *
 * 四类模块来源:
 *   类型A — webpack 桥梁模块: @/api/db, @/store 等 (由 module-bridge 暴露到 window.__SFC_MODULES__)
 *   类型A2 — webpack 动态 require: @/components/xxx 等桥梁未注册的 @/ 路径 (通过 webpack require 按需加载)
 *   类型B — 数据库模块: ./store, ./add.vue 等相对路径 (从后端 API 加载 COMPILEDCODE)
 *   类型C — 全局库: vue, heyui, vuex 等 (直接取 window 全局变量)
 */

import db from '@/api/db';
import { executeCompiled } from './sfc-compiler';

// 模块缓存
var moduleCache = {};

// 正在加载中的模块 (防止循环依赖)
var resolvingQueue = {};

// ORM API 路径
var API_PATH = '/api/data/call/RS_M17/A06/';

/**
 * 全局库映射表 (类型C)
 * 返回对象必须带 __esModule: true, 否则 Babel _interopRequireDefault 会再包一层
 */
var globalLibs = {
  'vue': function() { return { __esModule: true, default: require('vue') } },
  'heyui': function() { return { __esModule: true, default: require('heyui') } },
  'vuex': function() { return { __esModule: true, default: require('vuex') } },
  'axios': function() { return { __esModule: true, default: require('axios') } },
};

/**
 * 尝试通过 webpack require 加载 @/ 路径模块 (类型A2)
 * 桥梁未注册的 @/ 路径 (如 @/components/xxx, @/utils/xxx),
 * 通过 webpack 的 require.context 预扫描 src/ 目录实现按需加载
 * 无需手动在 module-bridge.js 中逐个注册
 */
var webpackReqCtx = require.context('@/', true, /\.(vue|js)$/);
var webpackReqMap = {};
webpackReqCtx.keys().forEach(function(key) {
  // key 格式: ./components/rs-table/rs-table-list.vue → @/components/rs-table/rs-table-list.vue
  webpackReqMap['@/' + key.substring(2)] = key;
});

/**
 * 通过 webpack require 加载 @/ 路径模块
 * @param {String} resolvedPath - 如 @/components/rs-table/rs-table-list.vue
 * @returns {Object|null} 模块 exports，未找到返回 null
 */
function tryWebpackRequire(resolvedPath) {
  var extPaths = tryExtensions(resolvedPath);
  for (var i = 0; i < extPaths.length; i++) {
    var wpKey = webpackReqMap[extPaths[i]];
    if (wpKey) {
      var mod = webpackReqCtx(wpKey);
      // 缓存
      moduleCache[extPaths[i]] = mod;
      return mod;
    }
  }
  return null;
}

/**
 * 路径解析: 将相对路径基于 callerPath 解析为绝对路径
 * @param {String} modulePath - 模块路径 (如 ./store, ../utils, @/api/db, vue)
 * @param {String} callerPath - 调用方路径 (如 @/pages/r02/m07/views/main.vue)
 * @returns {String} 解析后的路径
 */
export function resolvePath(modulePath, callerPath) {
  // 类型C: 全局库
  if (globalLibs[modulePath]) {
    return modulePath;
  }

  // 类型A: @/ 开头的 webpack 模块
  if (modulePath.startsWith('@/')) {
    return modulePath;
  }

  // 类型B: 相对路径, 基于 callerPath 解析
  if (modulePath.startsWith('./') || modulePath.startsWith('../')) {
    if (!callerPath) {
      throw new Error('无法解析相对路径 ' + modulePath + ': 缺少 callerPath');
    }
    // 取 callerPath 的目录部分
    var dir = callerPath.substring(0, callerPath.lastIndexOf('/'));
    var parts = dir.split('/');
    var relParts = modulePath.split('/');

    for (var i = 0; i < relParts.length; i++) {
      var part = relParts[i];
      if (part === '.') continue;
      if (part === '..') {
        parts.pop();
      } else {
        parts.push(part);
      }
    }
    return parts.join('/');
  }

  // 其他: 当作全局库名处理
  return modulePath;
}

/**
 * 尝试补全文件扩展名
 * 模块路径可能没有 .vue / .js 后缀, 尝试补全
 */
function tryExtensions(modulePath) {
  // 如果已有扩展名, 直接返回
  if (/\.(vue|js|json)$/.test(modulePath)) {
    return [modulePath];
  }
  // 尝试: 无扩展 → .vue → .js
  return [modulePath + '.vue', modulePath + '.js', modulePath];
}

/**
 * 从后端加载模块 (类型B)
 * 调用 ORM API: POST /api/data/call/RS_M17/A06/
 * 参数: { FilterParams: { MODULEPATH: xxx } }
 */
function fetchModuleFromDB(modulePath) {
  // 先检查缓存
  if (moduleCache[modulePath]) {
    return Promise.resolve(moduleCache[modulePath]);
  }

  // 检查是否正在加载 (循环依赖保护)
  if (resolvingQueue[modulePath]) {
    return resolvingQueue[modulePath];
  }

  var promise = db.postData({
    api: API_PATH,
    params: {
      FilterParams: { MODULEPATH: modulePath },
      PageSize: 1,
      PageIndex: 1,
    },
  }).then(function(ret) {
    var items = (ret && ret.Items) || [];
    if (items.length === 0) {
      throw new Error('数据库中未找到模块: ' + modulePath);
    }
    var record = items[0];
    var compiledCode = record.COMPILEDCODE;
    var deps = [];
    try {
      deps = record.DEPS ? JSON.parse(record.DEPS) : [];
    } catch (e) {
      // DEPS 解析失败, 不影响加载
    }

    if (!compiledCode) {
      throw new Error('模块 ' + modulePath + ' 没有编译后代码, 请先在编辑器中保存');
    }

    // 预加载所有依赖
    return preloadDeps(deps, modulePath).then(function() {
      // 执行编译后代码
      var requireFn = function(depPath) {
        return __sfc_require__(depPath, modulePath);
      };
      var exports = executeCompiled(compiledCode, requireFn);
      moduleCache[modulePath] = exports;
      delete resolvingQueue[modulePath];
      return exports;
    });
  }).catch(function(err) {
    delete resolvingQueue[modulePath];
    throw err;
  });

  resolvingQueue[modulePath] = promise;
  return promise;
}

/**
 * 预加载所有数据库依赖
 * 递归加载 DEPS 中的所有模块到缓存
 * 导出供预览场景调用 (预览时 executeCompiled 是同步的, 需先把所有数据库依赖加载到缓存)
 */
export function preloadDeps(deps, callerPath) {
  console.debug('[preloadDeps] deps:', deps, 'callerPath:', callerPath);
  if (!deps || deps.length === 0) {
    return Promise.resolve();
  }

  var promises = deps.map(function(dep) {
    var resolvedPath = resolvePath(dep, callerPath);
    console.debug('[preloadDeps] dep:', dep, '→ resolvedPath:', resolvedPath);

    // 类型C: 全局库, 直接返回
    if (globalLibs[resolvedPath]) {
      if (!moduleCache[resolvedPath]) {
        moduleCache[resolvedPath] = globalLibs[resolvedPath]();
      }
      return Promise.resolve();
    }

    // 类型A: @/ 开头的 webpack 模块
    if (resolvedPath.startsWith('@/')) {
      var bridgeModules = window.__SFC_MODULES__ || {};
      if (bridgeModules[resolvedPath]) {
        moduleCache[resolvedPath] = bridgeModules[resolvedPath];
        return Promise.resolve();
      }
      // 缓存
      var extPathsA = tryExtensions(resolvedPath);
      for (var j = 0; j < extPathsA.length; j++) {
        if (moduleCache[extPathsA[j]]) {
          return Promise.resolve();
        }
      }
      // webpack require.context 动态加载 (类型A2)
      var wpMod = tryWebpackRequire(resolvedPath);
      if (wpMod) return Promise.resolve();
      // 桥梁/缓存/webpack 均未命中 → 当作数据库模块加载
    }

    // 类型B: 数据库模块 (相对路径解析后的绝对路径, 或未注册的 @/ 路径)
    var extPaths = tryExtensions(resolvedPath);
    // 检查缓存
    for (var i = 0; i < extPaths.length; i++) {
      if (moduleCache[extPaths[i]]) {
        return Promise.resolve();
      }
    }
    // 尝试从数据库加载
    return tryLoadFromDB(extPaths);
  });

  return Promise.all(promises);
}

/**
 * 尝试从数据库加载 (尝试多个扩展名)
 */
function tryLoadFromDB(extPaths) {
  console.debug('[tryLoadFromDB] ', extPaths);
  function tryNext(index) {
    console.debug('[tryLoadFromDB] index=' + index + ' path=' + extPaths[index]);
    if (index >= extPaths.length) {
      return Promise.reject(new Error('数据库中未找到模块: ' + extPaths[0]));
    }
    var path = extPaths[index];
    if (moduleCache[path]) {
      console.debug('[tryLoadFromDB] 缓存命中:', path);
      return Promise.resolve(moduleCache[path]);
    }
    return fetchModuleFromDB(path).then(function(mod) {
      console.debug('[tryLoadFromDB] 加载成功:', path);
      return mod;
    }).catch(function(err) {
      console.debug('[tryLoadFromDB] 加载失败, err:', err.message, '→ 尝试下一个');
      // 尝试下一个扩展名
      if (index < extPaths.length - 1) {
        return tryNext(index + 1);
      }
      throw err;
    });
  }
  return tryNext(0);
}

/**
 * 核心函数: __sfc_require__
 * 被编译后的代码调用, 用于加载依赖模块
 *
 * @param {String} modulePath - 模块路径
 * @param {String} callerPath - 调用方路径 (自动传入)
 * @returns {Object} 模块的 exports
 */
// eslint-disable-next-line camelcase
export function __sfc_require__(modulePath, callerPath) {
  var resolvedPath = resolvePath(modulePath, callerPath);

  // 类型C: 全局库
  if (globalLibs[resolvedPath]) {
    if (!moduleCache[resolvedPath]) {
      moduleCache[resolvedPath] = globalLibs[resolvedPath]();
    }
    return moduleCache[resolvedPath];
  }

  // 类型A: @/ 开头的 webpack 模块
  if (resolvedPath.startsWith('@/')) {
    // 1) 桥梁模块 (优先)
    var bridgeModules = window.__SFC_MODULES__ || {};
    if (bridgeModules[resolvedPath]) {
      return bridgeModules[resolvedPath];
    }
    // 2) 缓存
    var extPathsA = tryExtensions(resolvedPath);
    for (var j = 0; j < extPathsA.length; j++) {
      if (moduleCache[extPathsA[j]]) {
        return moduleCache[extPathsA[j]];
      }
    }
    // 3) webpack require.context 动态加载 (类型A2)
    var wpMod = tryWebpackRequire(resolvedPath);
    if (wpMod) return wpMod;
    throw new Error('模块未找到: ' + resolvedPath + ' (桥梁/缓存/webpack均未命中)');
  }

  // 类型B: 相对路径 → 数据库模块
  var extPaths = tryExtensions(resolvedPath);
  for (var i = 0; i < extPaths.length; i++) {
    if (moduleCache[extPaths[i]]) {
      return moduleCache[extPaths[i]];
    }
  }
  // 同步缓存未命中
  throw new Error('模块未预加载: ' + modulePath + ' → ' + resolvedPath + ' (请确保 DEPS 中包含此路径)');
}

/**
 * 运行时加载 SFC 模块 (异步入口)
 * 1. 从后端获取 COMPILEDCODE + DEPS
 * 2. 递归预加载所有数据库依赖
 * 3. 执行编译后代码
 * 4. 返回 Vue 组件 options
 *
 * @param {String} modulePath - 模块路径 如 @/pages/r02/m07/views/main.vue
 * @returns {Promise<Object>} Vue 组件 options
 */
export function loadCompiledSFC(modulePath) {
  return loadModuleRecursive(modulePath, null);
}

/**
 * 递归加载模块 (用于运行时入口)
 */
function loadModuleRecursive(modulePath, callerPath) {
  var resolvedPath = resolvePath(modulePath, callerPath);

  // 类型C: 全局库
  if (globalLibs[resolvedPath]) {
    if (!moduleCache[resolvedPath]) {
      moduleCache[resolvedPath] = globalLibs[resolvedPath]();
    }
    return Promise.resolve(moduleCache[resolvedPath]);
  }

  // 类型A: @/ 开头的 webpack 模块
  if (resolvedPath.startsWith('@/')) {
    // 1) 桥梁模块 (优先)
    var bridgeModules = window.__SFC_MODULES__ || {};
    if (bridgeModules[resolvedPath]) {
      moduleCache[resolvedPath] = bridgeModules[resolvedPath];
      return Promise.resolve(bridgeModules[resolvedPath]);
    }
    // 2) 缓存
    var extPathsA = tryExtensions(resolvedPath);
    for (var j = 0; j < extPathsA.length; j++) {
      if (moduleCache[extPathsA[j]]) {
        return Promise.resolve(moduleCache[extPathsA[j]]);
      }
    }
    // 3) webpack require.context 动态加载 (类型A2)
    var wpMod = tryWebpackRequire(resolvedPath);
    if (wpMod) return Promise.resolve(wpMod);
    // 4) 都没命中 → 当作数据库模块加载
  }

  // 类型B: 数据库模块
  var extPaths = tryExtensions(resolvedPath);

  // 检查缓存
  for (var i = 0; i < extPaths.length; i++) {
    if (moduleCache[extPaths[i]]) {
      return Promise.resolve(moduleCache[extPaths[i]]);
    }
  }

  // 从数据库加载
  return tryLoadFromDB(extPaths).then(function(mod) {
    return mod;
  });
}

/**
 * 清除模块缓存 (开发调试用)
 */
export function clearModuleCache() {
  moduleCache = {};
  resolvingQueue = {};
}

/**
 * 按前缀清除模块缓存 (精准失效)
 * 用于: 编辑器保存某文件后, 让相关缓存失效, 下次加载从 DB 拉最新代码
 * 同时也清掉桥梁模块之外的全局库以外的内容, 但保留全局库缓存 (vue/heyui 等没必要清)
 *
 * @param {String|Array} prefix - 单个前缀或前缀数组 (如 '@/pages/s01/m16/')
 *                                 不传则清掉所有非全局库的数据库模块缓存
 */
export function invalidateCacheByPrefix(prefix) {
  var prefixes = Array.isArray(prefix) ? prefix : [prefix];
  // 全局库 (vue/heyui/vuex/axios) 不清, 清了也只是重新 require 一次没必要
  var globalLibKeys = { 'vue': 1, 'heyui': 1, 'vuex': 1, 'axios': 1 };
  Object.keys(moduleCache).forEach(function(key) {
    if (globalLibKeys[key]) return;
    // 无 prefix 参数 → 清掉所有非全局库
    if (!prefix || prefixes.length === 0) {
      delete moduleCache[key];
      return;
    }
    // 命中任一前缀则清除
    for (var i = 0; i < prefixes.length; i++) {
      if (key.indexOf(prefixes[i]) === 0) {
        delete moduleCache[key];
        return;
      }
    }
  });
  // resolvingQueue 也按同样规则清, 避免卡在加载中的状态
  Object.keys(resolvingQueue).forEach(function(key) {
    if (globalLibKeys[key]) return;
    if (!prefix || prefixes.length === 0) {
      delete resolvingQueue[key];
      return;
    }
    for (var i = 0; i < prefixes.length; i++) {
      if (key.indexOf(prefixes[i]) === 0) {
        delete resolvingQueue[key];
        return;
      }
    }
  });
}
