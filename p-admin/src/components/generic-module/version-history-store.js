/**
 * 通用版本历史弹窗的 store
 *
 * 按 Vuex 四层标准实现（详见 docs/frontend-store-convention.md）：
 * - state: 服务端返回的版本列表/详情/当前态原始数据
 * - getters: 列表显示用派生（操作类型 label、tag 高亮等）
 * - mutations: 唯一同步入口
 * - actions: fetch + commit；回滚/标记属命令式 RPC（调用方需 message），直接 return
 *
 * 接口分布：
 * - RS_M22/A01 版本列表（标准 ORM query，走 /api/data/call）
 * - RS_M22/A02 版本详情（标准 ORM open，走 Store03 open action → MAIN DataTable）
 * - RS_M22/A05 回滚、A06 当前态、A07 标记（自定义接口，走 /api/RDevVersion/call）
 *
 * 数据源：MAIN=VSS_DEV_VERSION（当前选中版本行），标记表单(TAG/PINNED)经 mapDateTable 绑定
 */
import db from '@/api/db';
import createStore from '@/store/createStore';

const STORE_NAME = 'vhp'; // version-history-popup

const storeResult = createStore.getStore({
  config: {
    moduleCode: 'RS_M22',
    // 直接指定 paths，不依赖 ensureModule 从 ORM 元数据加载
    paths: { 'MAIN': 'VSS_DEV_VERSION' },
  },
  storeName: STORE_NAME,
  state: {
    // 列表原始行（RS_M22/A01 Items）
    versions: [],
    // 当前选中版本的完整详情（RS_M22/A02 MAIN[0]）
    currentDetail: null,
    // 当前态原始内容（RS_M22/A06 current，用于"与现在对比"模式）
    currentContent: '',
    currentExists: false,
    // 弹窗当前上下文（用于 getter 与外部读）
    objType: '',
    objId: '',
    objCode: '',
  },
  getters: {
    // 列表已按时间倒序，无需额外派生；getter 留作未来扩展（如分组/高亮）
    versionCount: function(s) { return s.versions.length },
    hasCurrent: function(s) { return !!s.currentDetail },
  },
  mutations: {
    SET_CONTEXT(s, payload) {
      s.objType = (payload && payload.objType) || '';
      s.objId = (payload && payload.objId) || '';
      s.objCode = (payload && payload.objCode) || '';
    },
    SET_VERSIONS(s, rows) { s.versions = Array.isArray(rows) ? rows : [] },
    SET_CURRENT_DETAIL(s, row) { s.currentDetail = row || null },
    SET_CURRENT_CONTENT(s, payload) {
      s.currentContent = (payload && payload.content) || '';
      s.currentExists = !!(payload && payload.exists);
    },
    CLEAR_CURRENT(s) {
      s.currentDetail = null;
      s.currentContent = '';
      s.currentExists = false;
    },
    // 标记成功后同步版本行（避免重新拉接口）
    APPLY_MARK(s, { id, tag, pinned }) {
      if (s.currentDetail && s.currentDetail.ID === id) {
        s.currentDetail = Object.assign({}, s.currentDetail, {
          TAG: tag || null,
          PINNED: pinned ? 1 : 0,
        });
      }
      s.versions = s.versions.map(function(v) {
        if (v.ID !== id) return v;
        return Object.assign({}, v, { TAG: tag || null, PINNED: pinned ? 1 : 0 });
      });
    },
  },
  actions: {
    // 设置弹窗上下文（show 时调用）
    setContext({ commit }, opts) { commit('SET_CONTEXT', opts) },

    // 版本列表（RS_M22/A01）
    async loadVersions({ commit }, { objType, objId }) {
      var ret = await db.postData({
        api: '/api/data/call/RS_M22/A01/',
        params: { FilterParams: { OBJTYPE: objType, OBJID: objId }, PageSize: 200, PageIndex: 1 },
      });
      commit('SET_VERSIONS', (ret && ret.Items) || []);
    },

    // 版本详情（RS_M22/A02，含 BEFORE/AFTER 大字段）
    // 走 Store03 标准 open → MAIN DataTable（标记表单 TAG/PINNED 经 mapDateTable 绑同一行）
    async loadDetail({ commit, dispatch }, { id, fallback }) {
      if (!id) {
        commit('SET_CURRENT_DETAIL', fallback || null);
        return;
      }
      await dispatch('open', { ID: id });
      var dt = storeResult.storeHelper.getTable('MAIN');
      var row = (dt && dt.data && dt.data[0]) || fallback || null;
      commit('SET_CURRENT_DETAIL', row);
    },

    // 当前态内容（RS_M22/A06，走 RDevVersion 自定义路由）
    async loadCurrentState({ commit }, { id }) {
      // id 为空时仅重置（用于切换版本/回滚后清缓存，下次 setMode('current') 重新加载）
      if (!id) {
        commit('SET_CURRENT_CONTENT', { content: '', exists: false });
        return;
      }
      var ret = await db.postData({
        api: '/api/RDevVersion/call/RS_M22/A06/',
        params: { ID: id },
      });
      commit('SET_CURRENT_CONTENT', {
        content: (ret && ret.current) || '',
        exists: !!(ret && ret.exists),
      });
    },

    // 回滚（RPC，返回 { message } 给调用方弹 Message）
    rollback(ctx, { id }) {
      return db.postData({
        api: '/api/RDevVersion/call/RS_M22/A05/',
        params: { ID: id },
      });
    },

    // 标记版本（RPC，commit APPLY_MARK 同步本地行避免重拉）
    async markVersion({ commit }, { id, tag, pinned }) {
      await db.postData({
        api: '/api/RDevVersion/call/RS_M22/A07/',
        params: { ID: id, TAG: tag || '', PINNED: pinned ? '1' : '0' },
      });
      commit('APPLY_MARK', { id: id, tag: tag, pinned: pinned });
    },
  },
});

const { mapState, mapGetters, mapDateTable, Constants } = storeResult;

export { mapState, mapGetters, mapDateTable, Constants };
