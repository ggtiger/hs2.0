/// //////////
// 非公共保存模板
// 重构: 走 createStore.getStore 统一注册入口 (保留 Store02 + 所有 actions/mutations 不变)
// 详见 docs/frontend-store-convention.md
/// //////////
import db from '@/api/db';
import {
  Store02,
  Constants as SConstants
} from 'rs-vcore/store/Store02';
import createStore from '@/store/createStore';
import { SelStore } from '@/store/SelStore';
let oSelStore = new SelStore();

const storeHelper = new Store02({
  paths: {
    'QRY': 'VSS_USER',
    'QQRY': 'VSS_USER',
    'MAIN': 'VSS_USER',
    'UPDATE': 'VSS_USER',
    'DTSA': 'VSS_USERROLE',
    'DTSB': 'VSS_USERDEPT',
    'ROLE': 'VSS_ROLE',
    'DEPT': 'VSS_DEPT',
    ...oSelStore.mixPaths()
  }
});


const { mapState, mapGetters, mapDateTable, Constants } = createStore.getStore({
  config: { moduleCode: 'RS_M05' },
  storeHelper,
  storeName: 's01/m05',
  mutations: {
    'INIT_QQRY'(state) {
      let item = {
        PageSize: 20,
        PageIndex: 1,
        TotalCount: 0,
        INPUT: ''
      };
      storeHelper.getTable('QQRY').add(item);
    },
    'ADD'(state, { path, item }) {
      item = item || {};
      storeHelper.getTable(path).add(item);
    },
    'DEL'(state, { path,
      item
    }) {
      storeHelper.getTable(path).del(item);
    },
    'SET_DTSAKEY'(state, { path,
      KEYS
    }) {
      let DTSA = storeHelper.getTable(path);
      DTSA.data.map(d => {
        if (!d.ID) {
          DTSA.setValue('ID', KEYS.shift(), d);
        }
      });
    },
    SET_ENTRYNUM(state, { path }) {
      let DTS = storeHelper.getTable(path);
      DTS.data.map((d, index) => {
        DTS.setValue('ENTRYNUM', index + 1, d);
      });
    },
    SET_ENDISABLE(state, { item }) {
      let UPDATE = storeHelper.getTable('UPDATE');
      UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
      UPDATE.setValue('ID', item.ID);
    },
    'SET_DTSA'(state, { USERID, data
    }) {
      let DTSA = storeHelper.getTable('DTSA');
      let MAIN = storeHelper.getTable('MAIN');
      let keyValue = {};
      DTSA.data.forEach(item => {
        keyValue[item.ROLEID] = item;
        if (data.indexOf(item.ROLEID) === -1) {
          DTSA.del(item);
        }
      });
      data.forEach(ID => {
        if (!keyValue[ID]) {
          DTSA.add({ ROLEID: ID, USERID });
        }
      });
    },
    'SET_DTSB'(state, { USERID, data
    }) {
      let DTSA = storeHelper.getTable('DTSB');
      let MAIN = storeHelper.getTable('MAIN');
      let keyValue = {};
      DTSA.data.forEach(item => {
        keyValue[item.DEPTID] = item;
        if (data.indexOf(item.DEPTID) === -1) {
          DTSA.del(item);
        }
      });
      data.forEach(ID => {
        if (!keyValue[ID]) {
          DTSA.add({ DEPTID: ID, USERID });
        }
      });
    },
  },
  actions: {
    async query({
      state,
      commit
    }) {
      let QQRY = storeHelper.getTable('QQRY');
      let PageSize = QQRY.getValue('PageSize');
      let PageIndex = QQRY.getValue('PageIndex');
      let INPUT = QQRY.getValue('INPUT');
      let ret = await db.postData({
        'api': '/api/sm15/call/RS_M05/A01/',
        'params': {
          PageSize,
          PageIndex,
          FilterParams: {
            INPUT
          }
        }
      });
      QQRY.setValue('TotalCount', ret.TotalCount);
      commit(Constants.M_INITDATA, { path: 'QRY', data: (ret.Items || []) });
    },
    async open({
      state,
      commit
    }, {
      ID
    }) {
      let ret = await db.postData({ 'api': '/api/sm15/call/RS_M05/A02/', 'params': { FilterParams: { ID } } });
      commit(Constants.M_INITBYPATH, { paths: ['MAIN'] });
      commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
    },
    async openDts({
      state,
      commit
    }, {
      ID
    }) {
      let ret = await db.postData({ 'api': '/api/sm15/call/RS_M05/A06/', 'params': { FilterParams: { USERID: ID } } });
      commit(Constants.M_INITBYPATH, { paths: ['DTSA'] });
      commit(Constants.M_INITDATA, { path: 'DTSA', data: (ret.Items || []) });
    },
    async openDtsB({
      state,
      commit
    }, {
      ID
    }) {
      let ret = await db.postData({ 'api': '/api/sm15/call/RS_M05/A09/', 'params': { FilterParams: { USERID: ID } } });
      commit(Constants.M_INITBYPATH, { paths: ['DTSB'] });
      commit(Constants.M_INITDATA, { path: 'DTSB', data: (ret.Items || []) });
    },
    async openSel({
      state,
      commit
    }, {
      ID
    }) {
      let ret = await db.postData({
        'api': '/api/sm15/call/RS_M05/A05/',
        'params': {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
          }
        }
      });
      commit(Constants.M_INITDATA, {
        path: 'ROLE',
        data: (ret.Items || [])
      });
    },
    async openDeptSel({
      state,
      commit
    }, {
      ID
    }) {
      let ret = await db.postData({
        'api': '/api/sm15/call/RS_M05/A11/',
        'params': {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: {
            INPUT: ''
          }
        }
      });
      commit(Constants.M_INITDATA, {
        path: 'DEPT',
        data: (ret.Items || [])
      });
    },
    add({
      commit
    }) {
      commit(Constants.M_INITDATA, { path: 'MAIN', data: ([]) });
      commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
    },
    async save({
      commit
    }) {
      let ret = await db.postData({
        'api': '/api/sm15/call/RS_M05/A03/',
        'params': {
          'MAIN': storeHelper.getTable('MAIN').getXML(),
        }
      });
      commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
    },
    async saveRole({
      commit
    }, { }) {
      await db.postData({
        'api': '/api/sm15/call/RS_M05/A08/',
        'params': {
          'DTSA': storeHelper.getTable('DTSA').getXML(),
        }
      });
    },
    async resetPass({
      commit
    }, { ID }) {
      await db.postData({
        'api': '/api/sm15/call/RS_M05/A12/',
        'params': {
          ID
        }
      });
    },
    async saveDept({
      commit
    }, { }) {
      await db.postData({
        'api': '/api/sm15/call/RS_M05/A10/',
        'params': {
          'DTSB': storeHelper.getTable('DTSB').getXML(),
        }
      });
    },
    async endisable({
      commit
    }, { item }) {
      commit('SET_ENDISABLE', { item });
      let ret = await db.postData({
        'api': '/api/sm15/call/RS_M05/A07/',
        'params': {
          'UPDATE': storeHelper.getTable('UPDATE').getXML(),
        }
      });
      if (ret.length > 0) {
        for (let a in ret[0]) {
          item[a] = ret[0][a];
        }
      }
    },
    async delete() {
      storeHelper.getTable('MAIN').clear();
      let ret = await db.postData({
        api: '/api/sm15/call/RS_M05/A04/',
        params: {
          MAIN: storeHelper.getTable('MAIN').getXML(),
        },
      });
      console.log(ret);
    },
    ...oSelStore.mixActions()
  }
});

// createStore.getStore 注册后 DataTable 才可用，在此初始化 QQRY 默认行
storeHelper.getTable('QQRY').add({
  PageSize: 20,
  PageIndex: 1,
  TotalCount: 0,
  INPUT: ''
});

export {
  mapState,
  mapGetters,
  mapDateTable,
  Constants
};
