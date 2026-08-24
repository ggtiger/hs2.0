import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIB_M03', paths: oSelStore.mixPaths() },
  storeName: 'b01/m03',
  mutations: {
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },
    SETFILEDATA(state, { files }) {
      let DTS = storeHelper.getTable('DTS');
      DTS.clear();
      files.map(f => {
        DTS.add({ FILEID: f.id, FILENAME: f.name });
      });
    },
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: { ISON: 1 } });
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
