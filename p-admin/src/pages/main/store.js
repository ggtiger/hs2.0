/// //////////
// 非公共保存模板
/// //////////
import Store from '@/store/index';
import { createNamespacedHelpers } from 'vuex';
import { Store02, Constants as SConstants } from 'rs-vcore/store/Store02';
import db from '@/api/db';
import { dateToString } from 'rs-vcore/utils/Date';
const Constants = Object.assign({}, SConstants, {
  STORE_NAME: 'c02',
});

const { mapState, mapGetters } = createNamespacedHelpers(Constants.STORE_NAME);
const storeHelper = new Store02({
  paths: {
    QRY1: 'VBS_NOTICE',
    QRY11: 'VBS_NOTICE',
    QQRY1: 'VBS_NOTICE',
    QRY2: 'VSS_FUNC_LOG_C',
    QRY3: 'VSS_USER_NEED_APPR_C',
    QRY4: 'VRP_ORECORD_SUM01',
    QRY5: 'VRP_ORECORD_SUM02',
    MAINNOTICE: 'VBS_NOTICE',
    DTSNOTICE: 'VBS_BUSFILES',
  },
});

const state = {
  ...storeHelper.mixState(),
};

const getters = {};
const mutations = {
  ...storeHelper.mixMutations(),
};
const actions = {
  ...storeHelper.mixActions(),
  async query1({ state, commit }) {
    let QQRY1 =storeHelper.getTable("QQRY1");
    let ret = await db.postData({
      api: '/api/data/call/C02/A01/',
      params: {
        FilterParams: {
          INPUT: '',
        },
        PageSize: QQRY1.getValue("PageSize")||10,
        PageIndex: QQRY1.getValue("PageIndex")||1
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY1',
      data: ret.Items || [],
    });
    QQRY1.setValue("TotalCount",ret.TotalCount);
  },
  async query11({ state, commit }) {
    let QQRY1 =storeHelper.getTable("QQRY1");
    let ret = await db.postData({
      api: '/api/data/call/C02/A01/',
      params: {
        FilterParams: {
          INPUT: '',
        },
        PageSize: QQRY1.getValue("PageSize")||10,
        PageIndex: QQRY1.getValue("PageIndex")||1
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY11',
      data: ret.Items || [],
    });
    QQRY1.setValue("TotalCount",ret.TotalCount);
  },
  async query2({ state, commit }) {
    let ret = await db.postData({
      api: '/api/data/call/C02/A02/',
      params: {
        FilterParams: {
          INPUT: '',
        },
        PageSize:10
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY2',
      data: ret.Items || [],
    });
  },
  async query3({ state, commit }) {
    let ret = await db.postData({
      api: '/api/data/call/C02/A03/',
      params: {
        FilterParams: {
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY3',
      data: ret.Items || [],
    });
    return ret;
  },
  async query4({ state, commit }) {
    let date = new Date();
    date.setDate(1);
    let SDATE = dateToString(date);
    let EDATE = dateToString(new Date());
    let ret = await db.postData({
      api: '/api/data/call/LIR_M01/A01/',
      params: {
        FilterParams: {
          SDATE, EDATE
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY4',
      data: ret.Items || [],
    });
    return ret;
  },
  async query5({ state, commit }) {
    let date = new Date();
    date.setDate(1);
    let SDATE = dateToString(date);
    let EDATE = dateToString(new Date());
    let ret = await db.postData({
      api: '/api/data/call/LIR_M02/A01/',
      params: {
        FilterParams: {
          SDATE, EDATE
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'QRY5',
      data: ret.Items || [],
    });
    return ret;
  },
  async openNotice({ state, commit }, { ID }) {
    let ret = await db.postData({
      api: '/api/data/call/RS_M08/A02/',
      params: {
        FilterParams: {
          ID
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'MAINNOTICE',
      data: ret['MAIN'] || [],
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSNOTICE',
      data: ret['DTS'] || [],
    });
  },
};
Store.registerModule(Constants.STORE_NAME, {
  namespaced: true,
  state,
  getters,
  mutations,
  actions,
});

const mapDateTable = function(path, aFields, itemProp) {
  return storeHelper.mapGetters(path, aFields, itemProp);
};

export { mapState, mapGetters, mapDateTable, Constants };
