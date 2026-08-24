/// //////////
// 非公共保存模板
/// //////////
import Store from '@/store/index';
import {
  createNamespacedHelpers
} from 'vuex';
import {
  Store02,
  Constants as SConstants
} from 'rs-vcore/store/Store02';
import db from '@/api/db';

const Constants = Object.assign({}, SConstants, {
  STORE_NAME: 's01/m04'
});

const {
  mapState,
  mapGetters
} = createNamespacedHelpers(Constants.STORE_NAME);
const storeHelper = new Store02({
  paths: {
    'QRY': 'VSS_ROLE',
    'QQRY': 'QQRY',
    'MAIN': 'VSS_ROLE',
    'UPDATE': 'VSS_ROLE',
    'DTSA': 'VSS_ROLEFUNC',
    'SEL': 'SEL',
    'SELDTS': 'SELDTS',
  }
});

const state = {
  ...storeHelper.mixState(),
};

let item = {
  PageSize: 20,
  PageIndex: 1,
  TotalCount: 0,
  INPUT: ''
};
storeHelper.getTable('QQRY').add(item);
const getters = {};
const mutations = {
  ...storeHelper.mixMutations(),
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
  'SET_DTSA'(state, { path,
    ROLEID,
    treeData
  }) {
    let DTSA = storeHelper.getTable('DTSA');
    let keyValue = {};
    DTSA.data.forEach(item => keyValue[item['FUNCID']] = item);
    let dealTreeData = (items) => {
      items.forEach(t => {
        if (keyValue[t.ID]) {
          if (!t.ISCHECK) {
            debugger;
            DTSA.del(keyValue[t.ID]);
          }
        } else {
          if (t.ISCHECK && t.FUNCTYPE != '1') {
            DTSA.add({ ROLEID, FUNCID: t.ID });
          }
        }
        if (t.children) {
          dealTreeData(t.children);
        }
        if (t.point) {
          dealTreeData(t.point);
        }
      });
    };
    dealTreeData(treeData);
  },
};
const actions = {
  ...storeHelper.mixActions(),
  async query({
    state,
    commit
  }) {
    let QQRY = storeHelper.getTable('QQRY');
    let PageSize = QQRY.getValue('PageSize');
    let PageIndex = QQRY.getValue('PageIndex');
    let INPUT = QQRY.getValue('INPUT');
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A01/',
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
    let ret = await db.postData({ 'api': '/api/data/call/RS_M04/A02/', 'params': { FilterParams: { ID } } });
    commit(Constants.M_INITBYPATH, { paths: ['MAIN'] });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
  },
  async openSel({
    state,
    commit
  }, {
    ID
  }) {
    let ret = await db.postData({ 'api': '/api/data/call/RS_M04/A06/', 'params': { FilterParams: { ID } } });
    commit(Constants.M_INITBYPATH, { paths: ['SEL', 'SELDTS'] });
    commit(Constants.M_INITDATA, { path: 'SEL', data: (ret.SEL || []) });
    commit(Constants.M_INITDATA, { path: 'SELDTS', data: (ret.SELDTS || []) });
  },
  async openDts({
    state,
    commit
  }, {
    ID
  }) {
    let ret = await db.postData({ 'api': '/api/data/call/RS_M04/A05/', 'params': { FilterParams: { ID } } });
    commit(Constants.M_INITBYPATH, { paths: ['DTSA'] });
    commit(Constants.M_INITDATA, { path: 'DTSA', data: (ret.DTSA || []) });
  },
  async querySel({
    state,
    commit
  }, {
    INPUT
  }) {
    let MAIN = storeHelper.getTable('MAIN');
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A08/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          ID: MAIN.getValue('ID') || '-1'
        }
      }
    });
    commit(Constants.M_INITDATA, {
      path: 'SEL',
      data: (ret.Items || [])
    });
  },
  add({
    commit
  }) {
    commit(Constants.M_INITBYPATH, {
      paths: ['MAIN']
    });
    commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
  },
  async save({
    commit
  }) {
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A03/',
      'params': {
        'MAIN': storeHelper.getTable('MAIN').getXML(),
      }
    });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
  },
  async savePower({
    commit
  }, { ROLEID, treeData }) {
    commit('SET_DTSA', { ROLEID, treeData });
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A07/',
      'params': {
        'DTSA': storeHelper.getTable('DTSA').getXML(),
      }
    });
  },
  async endisable({
    commit
  }, { item }) {
    commit('SET_ENDISABLE', { item });
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A08/',
      'params': {
        'UPDATE': storeHelper.getTable('UPDATE').getXML(),
      }
    });
    if (ret.length > 0) {
      for (let a in ret[0]) {
        item[a] = ret[0][a];
      }
    }
    debugger;
  },
  async delete() {
    let ret = await db.postData({
      'api': '/api/data/call/RS_M04/A04/',
      'params': {
        'ID': storeHelper.getTable('MAIN').getValue('ID'),
      }
    });
  }
};
Store.registerModule(Constants.STORE_NAME, {
  namespaced: true,
  state,
  getters,
  mutations,
  actions
});

const mapDateTable = function(path, aFields, itemProp) {
  return storeHelper.mapGetters(path, aFields, itemProp);
};

export {
  mapState,
  mapGetters,
  mapDateTable,
  Constants
};
