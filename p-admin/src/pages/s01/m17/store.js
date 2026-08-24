import Store from '@/store';
import createStore from '@/store/createStore';
import db from '@/api/db';

const STORE_NAME = 's01/m17';
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
    mutations: {},
    actions: {
      // 新增空行 (本地初始化, 不调 API)
      add({ commit }) {
        commit('INIT', { paths: ['MAIN'] });
        commit('ADD', { path: 'MAIN', item: {} });
      },
      // 列出代码资产 (RS_M17/A01)。assetType 可选: csharp / sql / js / vue / undefined(全部)
      // 收口原 .vue 直接 db.postData 的查询，返回原始 ret 供调用方处理
      async listAssets(ctx, { assetType, pageSize, pageIndex } = {}) {
        var params = {
          FilterParams: assetType ? { ASSETTYPE: assetType } : {},
          PageSize: pageSize || 500,
          PageIndex: pageIndex || 1,
        };
        return db.postData({
          api: '/api/data/call/RS_M17/A01/',
          params: params,
        });
      },
    }
  });
  return _storeResult;
}

let mapState = function() { return getStoreResult().mapState.apply(this, arguments) };
let mapGetters = function() { return getStoreResult().mapGetters.apply(this, arguments) };
let mapDateTable = function() { return getStoreResult().mapDateTable.apply(this, arguments) };
let Constants = { STORE_NAME, MODULE_CODE };

export { mapState, mapGetters, mapDateTable, Constants, getStoreResult };
