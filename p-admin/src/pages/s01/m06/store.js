import db from "@/api/db"
import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M06' },
  storeName: 's01/m06',
  mutations: {
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue("ISUSE", item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue("ID", item.ID);
    }
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ["MAIN", 'DTSA'] });
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
    },
    async endisable({
      commit, dispatch
    }, { item }) {
      commit("SET_ENDISABLE", { item });
      let ret = await dispatch('call', {
        APICODE: 'A07', params: {
          "UPDATE": storeHelper.getTable("UPDATE").getXML()
        }
      })
      if (ret.length > 0) {
        for (let a in ret[0]) {
          item[a] = ret[0][a];
        }
      }
    },
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
