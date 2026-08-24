import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIB_M04', paths: oSelStore.mixPaths() },
  storeName: 'b01/m04',
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
    SET_DTSAKEY(state, { KEYS }) {
      let DTSA = storeHelper.getTable('DTSA');
      DTSA.data.map(d => {
        if (!d.ID) {
          DTSA.setValue('ID', KEYS.shift(), d);
        }
      });
    },
    SET_DTSA(state, { items }) {
      let SELFDTS = items;
      let DTSA = storeHelper.getTable('DTSA');
      SELFDTS.map(t => {
        let item = {};
        item['ARDID'] = t.ID;
        item['ARDCODE'] = t.ARDCODE;
        item['ARDNAME'] = t.ARDNAME;
        item['SIZETYPE'] = t.SIZETYPE;
        item['OMCODE'] = t.OMCODE;
        item['MRANGE'] = t.MRANGE;
        item['DEGREE'] = t.DEGREE;
        item['MANUFACTURER'] = t.MANUFACTURER;
        item['CDATE'] = t.CDATE;
        item['NCDATE'] = t.NCDATE;
        DTSA.add(item);
      });
    },
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN', 'DTS', 'DTSA'] });
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
    async setDtsA({ state, commit }, { TYPE, items }) {
      commit('SET_DTSA', {
        TYPE, items
      });
      let DTSA = storeHelper.getTable('DTSA');
      let CNT = DTSA.data.filter(t => {
        return !t.ID;
      }).length;
      if (CNT > 0) {
        let ret = await db.postData({
          api: '/api/data/call/C00/A01/',
          params: {
            CNT,
          },
        });
        commit('SET_DTSAKEY', {
          KEYS: ret,
        });
      }
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
