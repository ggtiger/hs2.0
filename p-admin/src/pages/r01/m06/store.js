import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LI_M06', paths: oSelStore.mixPaths(), apiPath: '/api/rm16/call' },
  storeName: 'r01/m06',
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
    },
    SET_DTSDEFAULT(state) {
      let MAIN = storeHelper.getTable('MAIN');
      let DTS = storeHelper.getTable('DTS');
      DTS.setValue('SLINKER', MAIN.getValue('MOBILE'));
      DTS.setValue('SENDNAME', MAIN.getValue('LINKER'));
      DTS.setValue('WCUSTNAME', MAIN.getValue('CUSTNAME'));
      DTS.setValue('SENDDATE', MAIN.getValue('BILLDATE'));
    },
    IMPORT_DTS(state, {items, columns}) {
      let DTS = storeHelper.getTable('DTS');
      let MAIN = storeHelper.getTable('MAIN');
      items.map(item => {
        let row = {};
        columns.map(column => {
          if (item[column.title]) {
            row[column.key] = item[column.title];
          }
        });
        row['SLINKER'] = MAIN.getValue('MOBILE');
        row['SENDNAME'] = MAIN.getValue('LINKER');
        row['WCUSTNAME'] = MAIN.getValue('CUSTNAME');
        row['SENDDATE'] = MAIN.getValue('BILLDATE').split(' ')[0];
        DTS.add(row);
      });
    }
  },
  actions: {
    add({
      commit
    }) {
      commit('INIT', { paths: ['MAIN', 'DTS'] });
      commit('ADD', { path: 'MAIN', item: { STATE: 1, VERSION: 1 } });
    },
    import({
      commit, dispatch
    }, { items }) {
      commit('IMPORT_DTS', { items });
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
    async aprint({
      commit, dispatch
    }, { items }) {
      return await dispatch('batch', { APICODE: 'A10', items });
    },
    ...oSelStore.mixActions(),
    async check({ commit, dispatch }) {
      debugger;
      let _this = storeHelper;
      let row = _this.moudle.getApi('check');
      let modeCode = _this.moudle.getModCode();
      let { APIPARAM, APICODE, PATHNAME } = row;
      let paths = APIPARAM.split(',');
      let params = {};
      paths.forEach(path => {
        if (path !== PATHNAME) {
          commit('SET_ENTRYNUM', { path });
        }
        params[path] = _this.getTable(path).getXML();
      });
      let ret = await db.postData({
        api: `/api/rm16/call/${modeCode}/${APICODE}/`,
        params
      });
      commit(Constants.M_BATCHSETDATA, {
        data: ret,
      });
      // await dispatch('flowSave', { ID, ACTIONCODE: 'submit' });
    },
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
