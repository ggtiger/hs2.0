import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIB_M06', paths: oSelStore.mixPaths() },
  storeName: 'b01/m06',
  mutations: {
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    }
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN', 'DTSA', 'DTSB', 'DTSC', 'DTSD', 'DTSE'] });
      commit('ADD', { path: 'MAIN', item: { ISON: 1 } });
    },
    addDts({ commit }, { path }) {
      commit('ADD', { path, item: {} });
    },
    removeDts({ commit }, { path, rows }) {
      commit('DEL', { path, rows });
    },
    async endisable({
      commit, dispatch
    }, { item }) {
      commit('SET_ENDISABLE', { item });
      let ret = await dispatch('call', {
        APICODE: 'A07',
        params: {
          'UPDATE': storeHelper.getTable('UPDATE').getXML()
        }
      });
      if (ret.length > 0) {
        for (let a in ret[0]) {
          item[a] = ret[0][a];
        }
      }
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
