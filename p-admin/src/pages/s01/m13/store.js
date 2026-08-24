import createStore from '@/store/createStore';
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M13' },
  storeName: 's01/m13',
  mutations: {},
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: { SQLTYPE: 'mysql' } });
    },
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
