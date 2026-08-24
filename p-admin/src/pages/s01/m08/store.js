import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'RS_M08', paths: oSelStore.mixPaths() },
  storeName: 's01/m08',
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
    },
    SETFILEDATA(state, { files }) {
      let DTS = storeHelper.getTable('DTS');
      DTS.clear();
      files.map(f => {
        DTS.add({ FILEID: f.id, FILENAME: f.name });
      });
    }
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
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
    async querySel({ state, commit }, { INPUT }) {
      // 查询表资源
      let ret = await db.postData({
        api: '/api/data/call/RS_M08/A06/',
        params: {
          PageSize: 20,
          PageIndex: 1,
          FilterParams: {
            INPUT,
          },
        },
      });
      commit(Constants.M_INITDATA, {
        path: 'SEL',
        data: ret.Items || [],
      });
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
