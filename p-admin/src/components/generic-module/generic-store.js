/**
 * GenericModule 的 store 工厂
 * 通过 moduleCode 动态创建 Vuex 模块，替代各业务模块独立的 store.js
 */
import createStore from '@/store/createStore';
import Store from '@/store/index';
import { loadCompiledSFC } from '@/sfc-loader';

// 缓存已创建的 store 辅助对象
var storeCache = {};

/**
 * 尝试加载模块级 SFC store 扩展
 * 约定路径: @/modules/{moduleCode}/store.js
 * 扩展 JS 导出 { actions, mutations } 来扩展 Store03
 * @param {string} moduleCode
 * @returns {Promise<{actions, mutations}|null>}
 */
async function loadModuleStoreExtend(moduleCode) {
  var jsPath = '@/modules/' + moduleCode + '/store.js';
  try {
    var mod = await loadCompiledSFC(jsPath);
    var extendObj = mod && mod.default ? mod.default : mod;
    if (extendObj && typeof extendObj === 'object' && (extendObj.actions || extendObj.mutations)) {
      return extendObj;
    }
  } catch (e) {
    // 约定路径不存在是正常情况，静默忽略
  }
  return null;
}

/**
 * 创建或获取通用模块的 store 辅助对象（同步，基础版本）
 * @param {string} moduleCode - 模块编码如 'LIB_M07'
 * @returns {{mapState, mapGetters, mapDateTable, Constants, storeHelper}}
 */
function getGenericStore(moduleCode) {
  var storeName = moduleCode.replace(/\//g, '_');

  if (storeCache[moduleCode]) {
    return storeCache[moduleCode];
  }

  var result = createStore.getStore({
    config: { moduleCode: moduleCode },
    storeName: storeName,
    actions: {
      add({
        commit
      }) {
        // 对齐业务模块标准 add：先 INIT 清空旧数据，再 ADD 新行。
        // 响应式问题由 rs-form-edit 的 model watcher 通过 $set 预初始化字段解决
        let row = result.storeHelper.getApiRow('save');
        let { APIPARAM, PATHNAME } = row;
        let paths = APIPARAM.split(',');
        commit('INIT', { paths });
        commit('ADD', { path: PATHNAME });
      }
    }
  });

  storeCache[moduleCode] = result;
  return result;
}

/**
 * 异步加载模块级 SFC store 扩展并合并到已注册的 Vuex 模块
 * 约定路径: @/modules/{moduleCode}/store.js
 * 扩展 JS 可导出 actions/mutations 来扩展 Store03
 * @param {string} moduleCode
 */
async function applyStoreExtend(moduleCode) {
  var extend = await loadModuleStoreExtend(moduleCode);
  if (!extend) return;

  var storeName = moduleCode.replace(/\//g, '_');
  var targetModule = Store._modules.root._children[storeName];
  if (!targetModule) return;

  // 合并扩展的 actions（允许覆盖已有，支持热更新）
  if (extend.actions) {
    Object.keys(extend.actions).forEach(function(key) {
      targetModule._rawModule.actions[key] = extend.actions[key];
      // 运行时 _actions 同步更新
      Store._actions[storeName + '/' + key] = [extend.actions[key]];
    });
  }

  // 合并扩展的 mutations（允许覆盖已有，支持热更新）
  if (extend.mutations) {
    Object.keys(extend.mutations).forEach(function(key) {
      targetModule._rawModule.mutations[key] = extend.mutations[key];
      // 运行时 _mutations 同步更新
      Store._mutations[storeName + '/' + key] = [extend.mutations[key]];
    });
  }
}

/**
 * 注销通用模块的 Vuex store (页面销毁时调用)
 */
function unregisterGenericStore(moduleCode) {
  var storeName = moduleCode.replace(/\//g, '_');
  if (Store._modules.root._children[storeName]) {
    Store.unregisterModule(storeName);
  }
  delete storeCache[moduleCode];
}

/**
 * 清空所有通用 store 缓存（退出登录时调用）
 */
function clearStoreCache() {
  storeCache = {};
}

export { getGenericStore, applyStoreExtend, unregisterGenericStore, clearStoreCache };
