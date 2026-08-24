import db from "@/api/db";
import createStore from "@/store/createStore";
import { SelStore } from '@/store/SelStore';

let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIB_M08', paths: oSelStore.mixPaths() },
  storeName: 'b01/m08',
  mutations: {
    SET_ENDISABLE (state, obj) {
      let dt = storeHelper.getDT(state, 'MAIN');
      let row = dt.rows.find(r => r.ID === obj.ID);
      if (row) {
        row.setValue('ISON', obj.ISON === 1 ? 0 : 1);
        dt.setRowState(row, 'UPDATE');
      }
    }
  },
  actions: {
    add ({ commit }) {
      commit('INIT', { paths: ["MAIN"] });
      commit('ADD', { path: 'MAIN', item: {} });
    },
    async endisable ({ commit, dispatch }, obj) {
      commit('SET_ENDISABLE', obj);
      let ret = await dispatch('call', {
        APICODE: 'A07', params: {
          "UPDATE": storeHelper.getTable("UPDATE").getXML()
        }
      });
      if (ret.length > 0) {
        for (let a in ret[0]) {
          obj[a] = ret[0][a];
        }
      }
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
