import db from '@/api/db';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';

// 日期格式化辅助函数：将 Date 对象转为 yyyy-MM-dd 字符串，避免 MySQL str_to_date 解析失败
function formatDateValue(vvv) {
  if (vvv instanceof Date) {
    var y = vvv.getFullYear();
    var m = (vvv.getMonth() + 1);
    var d = vvv.getDate();
    return y + '-' + (m < 10 ? '0' + m : m) + '-' + (d < 10 ? '0' + d : d);
  }
  return vvv;
}
let oSelStore = new SelStore();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LI_M031', paths: oSelStore.mixPaths(), apiPath: '/api/rm13/call' },
  storeName: 'r01/m031',
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
    async querySel({ state, commit }, { INPUT } = {}) {
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
    async query({ state, commit }, { INPUT, rows } = {}) {
      let QQRY = storeHelper.getTable('QQRY');
      let params = {
        PageSize: 20,
        PageIndex: 1,
        FilterParams: {
          INPUT, rows
        },
      };
      QQRY.getFields().forEach(f => {
        if (['PageSize', 'PageIndex', 'TotalCount', 'SumInfo'].indexOf(f) !== -1) {
          params[f] = QQRY.getValue(f);
        } else {
          let vvv = QQRY.getValue(f);
          if (Object.prototype.toString.call(vvv) === '[object Object]') {
            Object.keys(vvv).map(k => {
              params.FilterParams[f + '_' + k] = formatDateValue(vvv[k]);
            });
          } else {
            if (Array.isArray(vvv)) {
              params.FilterParams[f] = vvv.map(formatDateValue).join();
            } else {
              params.FilterParams[f] = formatDateValue(vvv);
            }
          }
        }
      });
      params.FilterParams['STATE'] = '1';
      // 查询表资源
      let ret = await db.postData({
        api: '/api/data/call/LI_M031/A01/',
        params
      });
      let ret2 = await db.postData({
        api: '/api/data/call/LI_M031/A09/',
        params: {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
            INPUT, rows: ret.Items
          },
        },
      });
      ret.Items.map(r => {
        r.children = ret2.Items.filter(q => {
          return q.WTCODE == r.WTCODE && r.CUSTID == q.CUSTID && r.BILLDATE == q.BILLDATE && r.BUSTYPEID == q.BUSTYPEID;
        });
      });
      QQRY.setValue('TotalCount', ret.TotalCount);
      QQRY.setValue('SumInfo', ret.SumInfo);
      commit(Constants.M_INITDATA, {
        path: 'QRY',
        data: ret.Items || [],
      });
    },
    async advQuery({ state, commit }, { }) {
      let QQRY = storeHelper.getTable('QQRY');
      let params = {
        PageSize: 20,
        PageIndex: 1,
        FilterParams: {
        },
      };
      QQRY.getFields().forEach(f => {
        if (['PageSize', 'PageIndex', 'TotalCount', 'SumInfo'].indexOf(f) !== -1) {
          params[f] = QQRY.getValue(f);
        } else {
          let vvv = QQRY.getValue(f);
          if (Object.prototype.toString.call(vvv) === '[object Object]') {
            Object.keys(vvv).map(k => {
              params.FilterParams[f + '_' + k] = formatDateValue(vvv[k]);
            });
          } else {
            if (Array.isArray(vvv)) {
              params.FilterParams[f] = vvv.map(formatDateValue).join();
            } else {
              params.FilterParams[f] = formatDateValue(vvv);
            }
          }
        }
      });
      // 查询表资源
      let ret = await db.postData({
        api: '/api/data/call/LI_M031/A01/',
        params
      });
      let ret2 = await db.postData({
        api: '/api/data/call/LI_M031/A09/',
        params: {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
            rows: ret.Items
          },
        },
      });
      ret.Items.map(r => {
        r.children = ret2.Items.filter(q => {
          return q.WTCODE == r.WTCODE && r.CUSTID == q.CUSTID && r.BILLDATE == q.BILLDATE && r.BUSTYPEID == q.BUSTYPEID;
        });
      });
      QQRY.setValue('TotalCount', ret.TotalCount);
      QQRY.setValue('SumInfo', ret.SumInfo);
      commit(Constants.M_INITDATA, {
        path: 'QRY',
        data: ret.Items || [],
      });
    },
    async queryDetail({ state, commit }, { INPUT, rows } = {}) {
      // 查询表资源
      let ret = await db.postData({
        api: '/api/data/call/LI_M031/A09/',
        params: {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
            INPUT, rows
          },
        },
      });
      commit(Constants.M_INITDATA, {
        path: 'QRY1',
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
    async queryProjectSum({ state, commit }, { PTEMPLATENAME, ADEPTNAME } = {}) {
      let ret = await db.postData({
        api: '/api/data/call/LI_M031/A20/',
        params: {
          PageSize: 200,
          PageIndex: 1,
          FilterParams: { PTEMPLATENAME, ADEPTNAME },
        },
      });
      commit(Constants.M_INITDATA, {
        path: 'SUM',
        data: ret.Items || [],
      });
    },
    async aprint({
      commit, dispatch
    }, { items }) {
      return await dispatch('batch', { APICODE: 'A16', items });
    },
    ...oSelStore.mixActions()
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
