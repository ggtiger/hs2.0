/**
 * script-flow-editor 的 store（编排接口编辑器）
 *
 * 按 Vuex 四层标准实现：
 * - state: RS_M18/A06 返回的模块关联资产原始行（KIND=2 即 SQL 模板）
 * - getters: SQL 模板下拉选项派生（filter+map 收口到 store）
 * - actions: loadSqlTemplates(fetch+commit) + 三个 RPC action(createApi/removeApi/saveSteps)
 *
 * 接口分布：
 * - RS_M18/A06 模块关联资产（与 code-editor-store 同源，但只消费 KIND=2）
 * - RS_M18/A13 创建编排接口（SC_M18_CREATE_SCRIPTFLOW）
 * - RS_M18/A08 解除接口关联（SC_M18_UNLINK_API）
 * - RS_M18/A12 保存编排步骤（SC_M18_SAVE_SCRIPTFLOW）
 */
import db from '@/api/db';
import createStore from '@/store/createStore';

const STORE_NAME = 'sfe'; // script-flow-editor

const storeResult = createStore.getStore({
  config: {
    moduleCode: 'RS_M18',
    // 直接指定 paths：MAIN=VSS_MOUDLEAPI 承载当前编辑的编排接口行（APINAME/ACTIONCODE 经 mapDateTable 绑定）
    paths: { 'MAIN': 'VSS_MOUDLEAPI' },
  },
  storeName: STORE_NAME,
  state: {
    // RS_M18/A06 原始行（含 KIND/ASSETTYPE 等）
    moduleAssets: [],
    // 当前弹窗绑定的模块编码
    moduleCode: '',
    // 当前选中编辑的编排接口行（app store MODAPI 中的原始行）
    activeApi: null,
  },
  getters: {
    // SQL 模板下拉项（KIND===2，前端下拉 {key,title}）
    sqlTemplateOptions: function(s) {
      return s.moduleAssets
        .filter(function(r) { return r.KIND === 2 })
        .map(function(r) { return { key: r.CODE, title: r.CODE + ' - ' + r.NAME } });
    },
    // APITYPE=script 的编排接口列表（从 app store MODAPI 派生，附 _stepCount）
    scriptApis: function(s, g, rootState) {
      var modData = rootState.app && rootState.app.modules && rootState.app.modules[s.moduleCode];
      var rows = (modData && modData.MODAPI) || [];
      return rows
        .filter(function(a) { return a.APITYPE === 'script' })
        .map(function(a) {
          var steps = [];
          try { steps = JSON.parse(a.APIPARAM || '[]') } catch (e) { /* ignore */ }
          a._stepCount = Array.isArray(steps) ? steps.length : 0;
          return a;
        });
    },
    // 模块已有的查询接口下拉（APITYPE=query/advQuery）
    queryApiOptions: function(s, g, rootState) {
      var modData = rootState.app && rootState.app.modules && rootState.app.modules[s.moduleCode];
      var rows = (modData && modData.MODAPI) || [];
      return rows
        .filter(function(a) { return a.APITYPE === 'query' || a.APITYPE === 'advQuery' })
        .map(function(a) { return { key: a.APICODE, title: a.APICODE + ' - ' + (a.APINAME || '') } });
    },
  },
  mutations: {
    SET_MODULE_CODE(s, code) { s.moduleCode = code || '' },
    SET_MODULE_ASSETS(s, rows) { s.moduleAssets = Array.isArray(rows) ? rows : [] },
    SET_ACTIVE_API(s, api) { s.activeApi = api || null },
    // 保存编排成功后同步 activeApi 行（步骤 JSON + 名称/动作 + 计数，避免重拉模块配置）
    APPLY_STEPS(s, { stepsJson, apiName, actionCode, stepCount }) {
      if (!s.activeApi) return;
      s.activeApi.APIPARAM = stepsJson;
      s.activeApi.APINAME = apiName;
      s.activeApi.ACTIONCODE = actionCode;
      s.activeApi._stepCount = stepCount;
    },
  },
  actions: {
    setModuleCode({ commit }, moduleCode) { commit('SET_MODULE_CODE', moduleCode) },

    // 选中编排接口：state.activeApi + MAIN DataTable 加载该行（APINAME/ACTIONCODE 绑定源）
    // api=null 时清空（关闭弹窗/移除当前接口）
    selectApi({ commit }, api) {
      commit('SET_ACTIVE_API', api);
      commit('INIT', { paths: ['MAIN'] });
      if (api) {
        storeResult.storeHelper.getTable('MAIN').initData([api]);
      }
    },

    // 模块关联资产（fetch + commit）
    async loadModuleAssets({ commit }, { moduleCode }) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M18/A06/',
        params: { FilterParams: { MODULECODE: moduleCode } },
      });
      commit('SET_MODULE_ASSETS', Array.isArray(ret) ? ret : []);
    },

    // 创建编排接口（RPC，返回 { apiCode, ... } 给调用方选中新行）
    createApi(ctx, { moduleCode, apiCode, apiName, actionCode }) {
      return db.postData({
        api: '/api/data/call/RS_M18/A13/',
        params: {
          MODULECODE: moduleCode,
          APICODE: apiCode,
          APINAME: apiName,
          ACTIONCODE: actionCode,
        },
      });
    },

    // 解除编排接口关联（RPC，外链资产不删文件）
    removeApi(ctx, { moduleCode, apiCode }) {
      return db.postData({
        api: '/api/data/call/RS_M18/A08/',
        params: { MODULECODE: moduleCode, KIND: 1, CODE: apiCode },
      });
    },

    // 保存编排步骤（RPC + commit APPLY_STEPS 同步 activeApi 行，避免重拉模块配置）
    async saveSteps({ commit }, { apiId, stepsJson, apiName, actionCode, stepCount }) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M18/A12/',
        params: {
          APIID: apiId,
          STEPS_JSON: stepsJson,
          APINAME: apiName,
          ACTIONCODE: actionCode,
        },
      });
      commit('APPLY_STEPS', { stepsJson: stepsJson, apiName: apiName, actionCode: actionCode, stepCount: stepCount });
      return ret;
    },
  },
});

const { mapState, mapGetters, mapDateTable, Constants } = storeResult;

export { mapState, mapGetters, mapDateTable, Constants };
