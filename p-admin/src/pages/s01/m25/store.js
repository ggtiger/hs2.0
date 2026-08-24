import Store from '@/store';
import createStore from '@/store/createStore';
import db from '@/api/db';

const STORE_NAME = 's01/m25';
const MODULE_CODE = 'RS_M25';

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
      // 预览模板脚本（RS_M25/A02 open，一次性展示数据直接 return 不进 state）
      async loadPreviewScript(ctx, { id }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M25/A02/',
          params: { FilterParams: { ID: id } },
        });
        var main = ret && ret.MAIN;
        return (main && main[0] && main[0].SCRIPT) || '（无脚本内容）';
      },
      // 物理删除模板（INIT+ADD 键值行再走 Store03 delete → <d> 段只带键值）
      // 不用 INIT+ADD+save: ADD 行被当 INSERT, 会撞 NOT NULL
      async deleteTemplate({ commit, dispatch }, { id }) {
        commit('INIT', { paths: ['MAIN'] });
        commit('ADD', { path: 'MAIN', item: { ID: id } });
        await dispatch('delete');
      },
      // 安装模板（RPC，调用 RModuleTpl 控制器；返回 { Data } 给调用方处理）
      installTemplate(ctx, { templateId, variables }) {
        return db.postData({
          api: '/api/RModuleTpl/call/RS_M25/A06/',
          params: { templateId: templateId, variables: JSON.stringify(variables) },
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
