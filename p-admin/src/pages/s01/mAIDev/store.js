import createStore from '@/store/createStore';
import db from '@/api/db';

let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_MAIDEV' },
  storeName: 's01/mAIDev',
  mutations: {},
  actions: {
    // 新建会话: 初始化 MAIN 表, 默认状态 DRAFT
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: { STATUS: 'DRAFT', SESSIONTYPE: 'NEW', ISDELETED: 0 } });
    },
    // 打开会话进入工作区(Chunk 3 接 AI 生成)
    async openSession({ dispatch }, { id }) {
      // 预留: 加载会话 + changeset + changeitem 子表
      return id;
    },
    // 加载会话详情（RS_MAIDEV/A02 open，返回 { MAIN: [{...}] }，组件需要单行对象）
    async loadSessionDetail(ctx, { id }) {
      var r = await db.postData({
        api: '/api/data/call/RS_MAIDEV/A02/',
        params: { FilterParams: { ID: id } },
      });
      // A02 返回 {MAIN:[{...}]}（直接对象，非数组包数组）
      // 兼容两种结构：{MAIN:[...]} 或 [{MAIN:[...]}]
      var mainArr = (r && r.MAIN) || (r && r[0] && r[0].MAIN) || [];
      return mainArr[0] || {};
    },
    // 把当前会话存为模板（RPC，调用 RModuleTpl 控制器 RS_M25/A08）
    saveSessionAsTemplate(ctx, { sessionId, templateCode, templateName, category, description }) {
      return db.postData({
        api: '/api/RModuleTpl/call/RS_M25/A08/',
        params: {
          sessionId: sessionId,
          templateCode: templateCode,
          templateName: templateName,
          category: category,
          description: description,
        },
      });
    },
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
