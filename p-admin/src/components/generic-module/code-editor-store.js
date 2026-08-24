/**
 * code-editor-popup 的 store
 *
 * 按 Vuex 四层标准实现（详见 docs/frontend-store-convention.md「state/getters/mutations/actions 分层」）：
 * - state: 服务端返回的原始数据原样存
 * - getters: 派生数据（filter/map/group 等纯函数）
 * - mutations: 同步修改 state 的唯一入口
 * - actions: 异步请求 + commit（链路 RPC 类除外）
 *
 * 两种列表模式：
 * - 模块上下文（moduleCode 非空）: listModuleAssets → RS_M18/A06，行带 KIND=1/2/3
 * - 全局模式（moduleCode 为空）: listAllAssets → RS_M17/A01，行带 ASSETTYPE=csharp/sql/js
 *
 * 选入面板：listAssetsByType → RS_M17/A01 + ASSETTYPE 过滤
 *
 * RPC 类 action（findAssetsByPath/findAssetsByCode/linkAsset/unlinkAsset）属于命令式调用，
 * 调用方需要拿具体返回值（apiCode/ID/message），不做状态化，原样返回 ret。
 */
import db from '@/api/db';
import createStore from '@/store/createStore';

const STORE_NAME = 'cep'; // code-editor-popup

// 把后端 raw 行映射为前端统一的列表项结构（filter/map 收口到 getter，不放 .vue）
function toModuleItem(r) {
  return { rid: r.RID, code: r.CODE, name: r.NAME, path: r.MODULEPATH, apiCode: r.APICODE };
}
function toAllAssetItem(r) {
  return { rid: r.ID, code: r.CODE, name: r.NAME, path: r.MODULEPATH, apiCode: null };
}
// 选入面板：排除已在当前组的（rid 去重），映射为 {rid, code, name, path, _checked:false}
// currentGroup 是已派生的 groupXxx（每项含 rid 字段）
function filterSelectorItems(rows, currentGroup) {
  var arr = Array.isArray(rows) ? rows : [];
  var group = Array.isArray(currentGroup) ? currentGroup : [];
  var existingRids = group.map(function(i) { return i.rid });
  return arr
    .filter(function(r) { return existingRids.indexOf(r.ID) < 0 })
    .map(function(r) {
      return { rid: r.ID, code: r.CODE, name: r.NAME, path: r.MODULEPATH, _checked: false };
    });
}

const { mapState, mapGetters, Constants } = createStore.getStore({
  config: { moduleCode: 'CEP' },
  storeName: STORE_NAME,
  state: {
    // 模块上下文原始行（RS_M18/A06）
    moduleAssets: [],
    // 全局原始行（RS_M17/A01 全类型）
    allAssets: [],
    // 当前是否处于模块上下文（决定 getters 从哪个源派生）
    moduleMode: false,
    // 选入面板原始行，按 kind 分桶 { csharp: [], sql: [], js: [] }
    selectorAssets: { csharp: [], sql: [], js: [], vue: [] },
  },
  getters: {
    // ====== 左侧三组列表（按 mode 自动派生，组件直接 mapGetters 使用） ======
    // 模块模式按 KIND=1 过滤；全局模式按 ASSETTYPE=csharp 过滤
    groupCsharp: function(s) {
      if (s.moduleMode) {
        return s.moduleAssets.filter(function(r) { return r.KIND === 1 }).map(toModuleItem);
      }
      return s.allAssets
        .filter(function(r) { return (r.ASSETTYPE || '') === 'csharp' })
        .map(toAllAssetItem);
    },
    groupSql: function(s) {
      if (s.moduleMode) {
        return s.moduleAssets.filter(function(r) { return r.KIND === 2 }).map(toModuleItem);
      }
      return s.allAssets
        .filter(function(r) { return (r.ASSETTYPE || '') === 'sql' })
        .map(toAllAssetItem);
    },
    groupJs: function(s) {
      if (s.moduleMode) {
        // JS 模块 APICODE 段存的是 FILETYPE，前端用 null 与脚本类区分
        return s.moduleAssets
          .filter(function(r) { return r.KIND === 3 })
          .map(function(r) {
            var it = toModuleItem(r);
            it.apiCode = null;
            return it;
          });
      }
      return s.allAssets
        .filter(function(r) { return (r.ASSETTYPE || '') === 'js' })
        .map(toAllAssetItem);
    },
    groupVue: function(s) {
      if (s.moduleMode) {
        return s.moduleAssets
          .filter(function(r) { return r.KIND === 4 })
          .map(function(r) {
            var it = toModuleItem(r);
            it.apiCode = null;
            return it;
          });
      }
      return s.allAssets
        .filter(function(r) { return (r.ASSETTYPE || '') === 'vue' })
        .map(toAllAssetItem);
    },
    // ====== 选入面板列表（按 kind 取，组件额外做"已在当前组"过滤） ======
    // 三个 getter 完成全部派生：排除已在当前组的 + 映射为 {rid, code, name, path, _checked}
    // 组件 openSelector() 拷贝一份给本地 data（_checked 在本地翻转）
    selectorItemsCsharp: function(s, g) {
      return filterSelectorItems(s.selectorAssets.csharp, g.groupCsharp);
    },
    selectorItemsSql: function(s, g) {
      return filterSelectorItems(s.selectorAssets.sql, g.groupSql);
    },
    selectorItemsJs: function(s, g) {
      return filterSelectorItems(s.selectorAssets.js, g.groupJs);
    },
    selectorItemsVue: function(s, g) {
      return filterSelectorItems(s.selectorAssets.vue, g.groupVue);
    },
  },
  mutations: {
    SET_MODULE_MODE(s, isModule) { s.moduleMode = !!isModule },
    SET_MODULE_ASSETS(s, rows) { s.moduleAssets = Array.isArray(rows) ? rows : [] },
    SET_ALL_ASSETS(s, rows) { s.allAssets = Array.isArray(rows) ? rows : [] },
    SET_SELECTOR_ASSETS(s, payload) {
      if (!payload || !payload.kind) return;
      s.selectorAssets = Object.assign({}, s.selectorAssets);
      s.selectorAssets[payload.kind] = Array.isArray(payload.rows) ? payload.rows : [];
    },
    CLEAR_SELECTOR_ASSETS(s) {
      s.selectorAssets = { csharp: [], sql: [], js: [] };
    },
  },
  actions: {
    // ====== 列表加载（fetch + commit，不返回 raw 给组件） ======
    // 模块上下文加载（RS_M18/A06 SS_MOD_CODEFILES）
    async loadModuleAssets({ commit }, { moduleCode } = {}) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M18/A06/',
        params: { FilterParams: { MODULECODE: moduleCode } },
      });
      commit('SET_MODULE_MODE', true);
      commit('SET_MODULE_ASSETS', ret);
    },
    // 全量加载（RS_M17/A01 F01 全类型）
    async loadAllAssets({ commit }, { pageSize, pageIndex } = {}) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M17/A01/',
        params: { FilterParams: {}, PageSize: pageSize || 500, PageIndex: pageIndex || 1 },
      });
      commit('SET_MODULE_MODE', false);
      commit('SET_ALL_ASSETS', (ret && ret.Items) || []);
    },
    // 选入面板按类型加载（RS_M17/A01 + ASSETTYPE 过滤）
    async loadSelectorAssets({ commit }, { kind, pageSize, pageIndex } = {}) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M17/A01/',
        params: {
          FilterParams: { ASSETTYPE: kind },
          PageSize: pageSize || 500,
          PageIndex: pageIndex || 1,
        },
      });
      commit('SET_SELECTOR_ASSETS', { kind: kind, rows: (ret && ret.Items) || [] });
    },

    // ====== RPC 类（命令式调用，返回具体结果给调用方） ======
    // 全量加载代码资产（RS_M17/A01 F01 全类型），直接返回 raw 给调用方按需过滤
    listAllAssets(ctx, { pageSize, pageIndex } = {}) {
      return db.postData({
        api: '/api/data/call/RS_M17/A01/',
        params: { FilterParams: {}, PageSize: pageSize || 500, PageIndex: pageIndex || 1 },
      });
    },
    // 按 MODULEPATH 查代码资产（RS_M17/A06，JS 模块定位用）
    findAssetsByPath(ctx, { modulePath }) {
      return db.postData({
        api: '/api/data/call/RS_M17/A06/',
        params: { FilterParams: { MODULEPATH: modulePath } },
      });
    },
    // 按 ASSETTYPE + CODE 查代码资产（csharp/sql 定位用，CODE 精确匹配）
    findAssetsByCode(ctx, { assetType, code }) {
      return db.postData({
        api: '/api/data/call/RS_M17/A01/',
        params: { FilterParams: { ASSETTYPE: assetType, CODE: code }, PageSize: 5, PageIndex: 1 },
      });
    },
    // 关联模块接口（RS_M18/A07 SC_M18_LINK_API，幂等 + 自动分配 APICODE）
    // 返回 { apiCode, message } 或 { message }
    linkAsset(ctx, { moduleCode, kind, code, apiName }) {
      return db.postData({
        api: '/api/data/call/RS_M18/A07/',
        params: { MODULECODE: moduleCode, KIND: kind, CODE: code, APINAME: apiName },
      });
    },
    // 解除模块接口关联（RS_M18/A08 SC_M18_UNLINK_API，外链资产用，不删文件）
    unlinkAsset(ctx, { moduleCode, kind, code }) {
      return db.postData({
        api: '/api/data/call/RS_M18/A08/',
        params: { MODULECODE: moduleCode, KIND: kind, CODE: code },
      });
    },
  },
});

export { mapState, mapGetters, Constants };
