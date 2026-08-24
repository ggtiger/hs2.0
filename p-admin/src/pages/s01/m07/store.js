import db from '@/api/db';
import createStore from '@/store/createStore';
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M07' },
  storeName: 's01/m07',
  mutations: {
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },
    SETTPMDATA(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('TPMDATA', item.TPMDATA);
      UPDATE.setValue('ID', item.ID);
    }
  },
  actions: {
    add({
      commit
    }, { item }) {
      commit('INIT', { paths: ['MAIN'] });
      let { TPMTYPE, TPMNAME, TPMDATA } = item;
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1, TPMTYPE, TPMNAME: TPMNAME + '(复制)', TPMDATA, STATE: '1' } });
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
    async updateTPMDATA({
      commit, dispatch
    }, { item }) {
      commit('SETTPMDATA', { item });
      let ret = await dispatch('call', {
        APICODE: 'A08',
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
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
