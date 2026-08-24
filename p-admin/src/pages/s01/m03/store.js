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
  STORE_NAME: 's01/m03'
});

const {
  mapState,
  mapGetters
} = createNamespacedHelpers(Constants.STORE_NAME);
const storeHelper = new Store02({
  paths: {
    'QRY': 'QRY',
    'QQRY': 'QQRY',
    'MAIN': 'VSS_FUNC',
    'DTSA': 'VSS_FUNCPOINT',
    'SEL': 'SEL',
    'SELFDTS': 'VSS_MOUDLEAPI',
  }
});

const state = {
  ...storeHelper.mixState(),
};

let item = {
  PageSize: 1,
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
      PageSize: 1,
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
  SET_SELECTAPI(state, { items }) {
    let DTS = storeHelper.getTable('DTSA');
    items.map((d, index) => {
      DTS.add({ FUNCPOINTCODE: d.APICODE, FUNCPOINTNAME: d.APINAME });
    });
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
      'api': '/api/data/call/RS_M03/A01/',
      'params': {
        PageSize,
        PageIndex,
        FilterParams: {
          INPUT
        }
      }
    });
    QQRY.setValue('TotalCount', ret.TotalCount);

    commit(Constants.M_INITDATA, {
      path: 'QRY',
      data: (ret.Items || [])
    });
  },
  async open({
    state,
    commit
  }, {
    ID
  }) {
    let ret = await db.postData({ 'api': '/api/data/call/RS_M03/A02/', 'params': { FilterParams: { ID } } });
    commit(Constants.M_INITBYPATH, { paths: ['MAIN'] });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
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
      'api': '/api/data/call/RS_M03/A08/',
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
  async querySelsDts({
    state,
    commit
  }, {
    INPUT
  }) {
    let MAIN = storeHelper.getTable('MAIN');
    let ret = await db.postData({
      'api': '/api/data/call/RS_M03/A10/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          MODULECODE: MAIN.getValue('FUNCCODE') || '-1'
        }
      }
    });
    commit(Constants.M_INITDATA, {
      path: 'SELFDTS',
      data: (ret.Items || [])
    });
  },
  add({
    commit
  }) {
    commit(Constants.M_INITBYPATH, {
      paths: ['MAIN', 'DTSA']
    });
    commit('ADD', { path: 'MAIN', item: { ISHIDE: 0, FUNCTYPE: 1 } });
  },
  async save({
    commit
  }) {
    let ret = await db.postData({
      'api': '/api/data/call/RS_M03/A04/',
      'params': {
        'MAIN': storeHelper.getTable('MAIN').getXML(),
        'DTSA': storeHelper.getTable('DTSA').getXML(),
      }
    });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
    commit(Constants.M_INITDATA, { path: 'DTSA', data: (ret.DTSA || []) });
  },
  async delete() {
    storeHelper.getTable('MAIN').clear();
    storeHelper.getTable('DTSA').clear();
    let ret = await db.postData({
      'api': '/api/data/call/RS_M03/A07/',
      'params': {
        'MAIN': storeHelper.getTable('MAIN').getXML(),
        'DTSA': storeHelper.getTable('DTSA').getXML(),
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
