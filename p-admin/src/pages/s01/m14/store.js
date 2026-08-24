import createStore from '@/store/createStore';

let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M14' },
  storeName: 's01/m14',
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: { ENABLED: 1 } });
    }
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
