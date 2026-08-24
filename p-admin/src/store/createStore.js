
import { Store03, Constants as SConstants } from './Store03';
import Store from './index';
import { createNamespacedHelpers } from 'vuex';
export default {
  getStore({ config, storeHelper, storeName, state, getters, actions, mutations }) {
    const Constants = Object.assign({}, SConstants, {
      STORE_NAME: storeName,
    });
    const { mapState, mapGetters } = createNamespacedHelpers(storeName);
    storeHelper = storeHelper || new Store03(config);
    const _state = {
      MODULECODE: config.moduleCode,
      ...storeHelper.mixState(),
      ...state
    };
    // 支持模块自定义 getters（用于派生数据：filter/map 等纯计算）
    // 详见 docs/frontend-store-convention.md 「state/getters/mutations/actions 分层」
    const _getters = { ...getters };
    const _mutations = {
      ...storeHelper.mixMutations(),
      ...mutations
    };
    const _actions = {
      ...storeHelper.mixActions(),
      ...actions
    };
    // Vuex 的 registerModule 对同名模块的 actions/mutations 是追加而非替换
    // (源码: _actions[type] = _actions[type] || []; entry.push(handler))
    // 若不先 unregister, 多次调用 createStore.getStore (典型场景: SFC 在线编辑器实时预览
    // 反复执行 store.js) 会导致 dispatch 时 N 个 handler 全部触发, 出现"查询被调用 N 次"
    // 因此先卸载已存在的同名模块, 保证 actions/mutations 数组始终只有一份
    if (Store._modules.root._children[storeName]) {
      Store.unregisterModule(storeName);
    }
    Store.registerModule(storeName, {
      namespaced: true,
      state: _state,
      getters: _getters,
      mutations: _mutations,
      actions: _actions,
    });
    const mapDateTable = function(path, aFields, itemProp) {
      return storeHelper.mapGetters(path, aFields, itemProp);
    };
    return { mapState, mapGetters, mapDateTable, Constants, storeHelper };
  }
};
