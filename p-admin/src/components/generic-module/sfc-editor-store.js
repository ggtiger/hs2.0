/**
 * sfc-editor-popup 的 store（SFC 组件 / 扩展JS / Store扩展 弹窗编辑器）
 *
 * 按 Vuex 四层标准实现：
 * - state: 由 BaseStore.mixState 创建 MAIN DataTable（承载当前编辑的资产行）
 * - actions: 覆盖 add（RS_M17 没有 A02 add API，本地 INIT+ADD 空行）；
 *   open/save 走 Store03 标准 action（/api/data/call/RS_M17/A02|A04）
 *
 * 说明：与 s01/m17 页面 store 同模块配置（RS_M17 / VSS_CODE_ASSET），
 * 但用独立 storeName 隔离 DataTable，避免弹窗编辑污染 IDE 当前行。
 */
import Store from '@/store';
import createStore from '@/store/createStore';

const STORE_NAME = '_sfc_popup_RS_M17';
const MODULE_CODE = 'RS_M17';

let _storeResult = null;

function getStoreResult() {
  if (_storeResult) return _storeResult;

  if (Store.state[STORE_NAME]) {
    Store.unregisterModule(STORE_NAME.split('/'));
  }

  _storeResult = createStore.getStore({
    config: { moduleCode: MODULE_CODE },
    storeName: STORE_NAME,
    actions: {
      // 新增空行 (本地初始化, 不调 API；与 s01/m17 store 的 add 一致)
      // 必须先 await ensureModule()：mixState 同步执行时 paths 为空(dt={}),
      // 模块配置异步加载完后 _ensureDataTables 才补建 DataTable
      async add({ commit }) {
        await _storeResult.storeHelper.ensureModule();
        commit('INIT', { paths: ['MAIN'] });
        commit('ADD', { path: 'MAIN', item: {} });
      },
    },
  });
  return _storeResult;
}

let mapState = function() { return getStoreResult().mapState.apply(this, arguments) };
let mapGetters = function() { return getStoreResult().mapGetters.apply(this, arguments) };
let mapDateTable = function() { return getStoreResult().mapDateTable.apply(this, arguments) };
let Constants = { STORE_NAME, MODULE_CODE };

export { mapState, mapGetters, mapDateTable, Constants, getStoreResult };
