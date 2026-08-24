import db from "@/api/db";
import createStore from "@/store/createStore";
import Store from "@/store";
import { dateToString } from "rs-vcore/utils/Date";

const STORE_NAME = 'r02/m07';
const MODULE_CODE = 'R02_M07';

let _storeResult = null;

function getStoreResult() {
  if (_storeResult) return _storeResult;

  // 如果模块已注册，先注销再重新注册
  if (Store.state[STORE_NAME]) {
    Store.unregisterModule(STORE_NAME.split('/'));
  }

  _storeResult = createStore.getStore({
    config: { moduleCode: MODULE_CODE },
    storeName: STORE_NAME,
    mutations: {},
    actions: {
      add({ commit }) {
        commit('INIT', { paths: ["MAIN", "DTSA", "DTS"] });
        commit('ADD', { path: 'MAIN', item: {} });
      },
      // 编辑回显：标准 open 不返回 DTSA, 单独拉取后由调用方直写 DTSA
      // (multiautocomplete 字段会据此显示已关联受理单)
      async loadAcceptRefs(ctx, { id }) {
        return db.postData({
          api: '/api/data/call/R02_M07/A02/',
          params: { ID: id },
        });
      },
    }
  });
  return _storeResult;
}

// 延迟导出：在首次访问时才初始化 store
let mapState = function() { return getStoreResult().mapState.apply(this, arguments); };
let mapGetters = function() { return getStoreResult().mapGetters.apply(this, arguments); };
let mapDateTable = function() { return getStoreResult().mapDateTable.apply(this, arguments); };
let Constants = { STORE_NAME, MODULE_CODE };

export { mapState, mapGetters, mapDateTable, Constants };
