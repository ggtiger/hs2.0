import Store from '@/store';
import createStore from '@/store/createStore';
import db from '@/api/db';

const STORE_NAME = 's01/m22';
const MODULE_CODE = 'RS_M22';

let _storeResult = null;

function getStoreResult() {
  if (_storeResult) return _storeResult;

  if (Store.state[STORE_NAME]) {
    Store.unregisterModule(STORE_NAME.split('/'));
  }

  _storeResult = createStore.getStore({
    config: { moduleCode: MODULE_CODE },
    storeName: STORE_NAME,
    state: {
      // 发布包列表（RDevVersion A11 原始行）
      releases: [],
    },
    mutations: {
      SET_RELEASES(state, rows) {
        state.releases = Array.isArray(rows) ? rows : [];
      },
    },
    actions: {
      // 回滚版本（RPC，返回 { message } 给调用方弹提示；版本详情/历史走 vhp store）
      rollback(ctx, { id }) {
        return db.postData({
          api: '/api/RDevVersion/call/RS_M22/A05/',
          params: { ID: id },
        });
      },
      // 发布包列表（fetch + commit）
      async loadReleases({ commit }) {
        var ret = await db.postData({
          api: '/api/RDevVersion/call/RS_M22/A11/',
          params: {},
        });
        commit('SET_RELEASES', (ret && ret.Data) || ret || []);
      },
      // 批量打标（RPC，返回 { Data: { affected } } 给调用方弹提示）
      batchMark(ctx, { objType, objCode, tag, pinned }) {
        return db.postData({
          api: '/api/RDevVersion/call/RS_M22/A08/',
          params: { OBJTYPE: objType, OBJCODE: objCode, TAG: tag, PINNED: pinned },
        });
      },
      // 创建发布包（RPC，返回 { Data: { objCount } } 给调用方弹提示）
      createRelease(ctx, { tag, releaseCode, releaseName, remark }) {
        return db.postData({
          api: '/api/RDevVersion/call/RS_M22/A09/',
          params: { TAG: tag, RELEASECODE: releaseCode, RELEASENAME: releaseName, REMARK: remark },
        });
      },
      // 部署发布包（RPC，导入到升级中心）
      deployRelease(ctx, { releaseId }) {
        return db.postData({
          api: '/api/RDevVersion/call/RS_M22/A10/',
          params: { RELEASEID: releaseId },
        });
      },
      // 读取发布包脚本内容（RS_M22/A02 open，一次性展示数据直接 return 不进 state）
      async loadReleaseScript(ctx, { id }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M22/A02/',
          params: { FilterParams: { ID: id } },
        });
        var data = ret && ret.Data;
        if (data && data[0] && data[0].Rows && data[0].Rows[0]) {
          return data[0].Rows[0].SCRIPTCONTENT || '（无脚本内容）';
        }
        return '（无法加载脚本内容）';
      },
    }
  });
  return _storeResult;
}

let mapState = function() { return getStoreResult().mapState.apply(this, arguments) };
let mapGetters = function() { return getStoreResult().mapGetters.apply(this, arguments) };
let mapDateTable = function() { return getStoreResult().mapDateTable.apply(this, arguments) };
let Constants = { STORE_NAME, MODULE_CODE };

export { mapState, mapGetters, mapDateTable, Constants };
