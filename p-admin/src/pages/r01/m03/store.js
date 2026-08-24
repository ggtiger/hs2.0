import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LI_M03', paths: oSelStore.mixPaths(), apiPath: '/api/rm13/call' },
  storeName: 'r01/m03',
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
    SET_CHARGEDATA(state, { userInfo }) {
      let MAIN = storeHelper.getTable('MAIN');
      MAIN.setValue('CHARGEID', userInfo.ID);
      MAIN.setValue('CHARGER', userInfo.NICKNAME);
      MAIN.setValue('CHARGETIME', dateToString(new Date(), 'yyyy-MM-dd hh:mm:ss'));
    }
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN'] });
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
        api: '/api/data/call/LI_M01/A06/',
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
    async batchDiscount({
      commit, dispatch
    }, { items, DISCOUNT }) {
      await dispatch('batch', { APICODE: 'A12', items, updateFields: ['AMT', 'DISCOUNT'], params: { DISCOUNT } });
    },
    async batchFee({
      commit, dispatch
    }, { items }) {
      await dispatch('batch', { APICODE: 'A13', items, updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'], params: {} });
    },
    async batchReFee({
      commit, dispatch
    }, { items }) {
      await dispatch('batch', { APICODE: 'A14', items, updateFields: ['RAMT', 'CHARGEID', 'CHARGER', 'CHARGETIME'], params: {} });
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
