import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M16' },
  storeName: 's01/m16',
  mutations: {},
  actions: {
    add({ commit }) {
      commit('INIT', { paths: ['MAIN'] });
      commit('ADD', { path: 'MAIN', item: {} });
    },
  }
});
export { mapState, mapGetters, mapDateTable, Constants };
