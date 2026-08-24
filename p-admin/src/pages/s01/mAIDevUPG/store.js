import createStore from '@/store/createStore';
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_MAIDEVUPG' },
  storeName: 's01/mAIDevUPG',
  mutations: {},
  actions: {
    // 升级记录的 add 不需要初始化 MAIN(由导入接口 A05 创建)
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: { STATUS: 'PENDING', ISDELETED: 0 } });
    },
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
