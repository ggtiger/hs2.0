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
  STORE_NAME: 's01/m02'
});

const {
  mapState,
  mapGetters
} = createNamespacedHelpers(Constants.STORE_NAME);
const storeHelper = new Store02({
  paths: {
    'QRY': 'VSS_MOUDLE',
    'QQRY': 'VSS_MOUDLE',
    'MAIN': 'VSS_MOUDLE',
    'DTSA': 'VSS_MOUDLEPATH',
    'DTSB': 'VSS_MOUDLEPATHREL',
    'DTSC': 'VSS_MOUDLEAPI',
    'SEL': 'SEL',
    'SELF': 'SELF',
    'SELFDTS': 'SELFDTS'
  }
});

const state = {
  ...storeHelper.mixState(),
  QueryInfo: {
    PageSize: 20,
    PageIndex: 1,
    TotalCount: 0,
    FilterParams: {
      INPUT: ''
    }
  }
};

let item = {
  PageSize: 20,
  PageIndex: 1,
  TotalCount: 0,
  INPUT: ''
};
storeHelper.getTable('QQRY').add(item);
storeHelper.getTable('SEL').add({});
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
  'ADD'(state, { path }) {
    storeHelper.getTable(path).add({});
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
};
const actions = {
  ...storeHelper.mixActions(),
  async query({
    state,
    commit
  }) {
    debugger;
    let QQRY = storeHelper.getTable('QQRY');
    let PageSize = QQRY.getValue('PageSize');
    let PageIndex = QQRY.getValue('PageIndex');
    let INPUT = QQRY.getValue('INPUT');
    let ret = await db.postData({
      'api': '/api/data/call/RS_M02/A01/',
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
    DID
  }) {
    let ret = await db.postData({ 'api': '/api/data/call/RS_M02/A02/', 'params': { FilterParams: { MODULECODE: DID } } });
    commit(Constants.M_INITBYPATH, { paths: ['MAIN'] });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
    commit(Constants.M_INITDATA, { path: 'DTSA', data: (ret.DTSA || []) });
    commit(Constants.M_INITDATA, { path: 'DTSB', data: (ret.DTSB || []) });
    commit(Constants.M_INITDATA, { path: 'DTSC', data: (ret.DTSC || []) });
    commit(Constants.M_INITDATA, { path: 'SEL', data: ([]) });
  },
  async querySel({
    state,
    commit
  }, {
    INPUT
  }) {
    // 查询资源
    let ret = await db.postData({
      'api': '/api/data/call/RS_M02/A03/',
      'params': {
        PageSize: 20,
        PageIndex: 1,
        FilterParams: {
          INPUT
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
      paths: ['MAIN', 'DTSA', 'DTSB', 'DTSC']
    });
  },
  async save({
    commit
  }) {
    commit('SET_ENTRYNUM', { path: 'DTSA', });
    commit('SET_ENTRYNUM', { path: 'DTSB', });
    commit('SET_ENTRYNUM', { path: 'DTSC', });
    let ret = await db.postData({
      'api': '/api/data/call/RS_M02/A04/',
      'params': {
        'MAIN': storeHelper.getTable('MAIN').getXML(),
        'DTSA': storeHelper.getTable('DTSA').getXML(),
        'DTSB': storeHelper.getTable('DTSB').getXML(),
        'DTSC': storeHelper.getTable('DTSC').getXML(),
      }
    });
    commit(Constants.M_INITDATA, { path: 'MAIN', data: (ret.MAIN || []) });
    commit(Constants.M_INITDATA, { path: 'DTSA', data: (ret.DTSA || []) });
    commit(Constants.M_INITDATA, { path: 'DTSB', data: (ret.DTSB || []) });
    commit(Constants.M_INITDATA, { path: 'DTSC', data: (ret.DTSC || []) });
  },
  async delete() {
    storeHelper.getTable('MAIN').clear();
    storeHelper.getTable('DTSA').clear();
    storeHelper.getTable('DTSB').clear();
    let ret = await db.postData({
      'api': '/api/data/call/RS_M02/A07/',
      'params': {
        'MAIN': storeHelper.getTable('MAIN').getXML(),
        'DTSA': storeHelper.getTable('DTSA').getXML(),
        'DTSB': storeHelper.getTable('DTSB').getXML(),
        'DTSC': storeHelper.getTable('DTSC').getXML()
      }
    });
  },
  async querySelT({ state, commit }, { INPUT }) {
    // 查询表资源
    let ret = await db.postData({
      api: '/api/data/call/RS_M02/A03/',
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
