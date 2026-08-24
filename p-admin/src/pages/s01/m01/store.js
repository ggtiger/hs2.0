/// //////////
// 非公共保存模板
/// //////////
import Store from '@/store/index';
import { createNamespacedHelpers } from 'vuex';
import { Store02, Constants as SConstants } from 'rs-vcore/store/Store02';
import db from '@/api/db';

const Constants = Object.assign({}, SConstants, {
  STORE_NAME: 's01/m01',
});

const { mapState, mapGetters } = createNamespacedHelpers(Constants.STORE_NAME);
const storeHelper = new Store02({
  paths: {
    QRY: 'VSS_RESOURCE',
    QQRY: 'VSS_RESOURCE',
    MAIN: 'VSS_RESOURCE',
    DTSA: 'VSS_RESFIELD',
    DTSB: 'VSS_RESFILTER',
    DTSC: 'VSS_RESUIPC',
    SEL: 'SEL',
    SELF: 'SELF',
    SELFDTS: 'SELFDTS',
  },
});

const state = {
  ...storeHelper.mixState(),
  QueryInfo: {
    PageSize: 20,
    PageIndex: 1,
    TotalCount: 0,
    FilterParams: {
      INPUT: '',
    },
  },
  TTDTS: [
    {
      FIELDNAME: 'AAA',
    },
    {
      FIELDNAME: 'BBB',
    },
  ],
};

let item = {
  PageSize: 20,
  PageIndex: 1,
  TotalCount: 0,
  INPUT: '',
};
storeHelper.getTable('QQRY').add(item);

const getters = {};
const mutations = {
  ...storeHelper.mixMutations(),
  INIT_QQRY(state) {
    let item = {
      PageSize: 10,
      PageIndex: 1,
      TotalCount: 0,
      INPUT: '',
    };
    storeHelper.getTable('QQRY').add(item);
  },
  SET_SELF(state, { TYPE, item }) {
    let SELF = storeHelper.getTable('SELF');
    // let SELFDTS = storeHelper.getTable('SELFDTS');
    let MAIN = storeHelper.getTable('MAIN');
    // let DTSA = storeHelper.getTable('DTSA');
    if (TYPE === 'MAIN') {
      SELF.initData();
      SELF.add({
        REFRESOURCEID: MAIN.getValue('TABLERESOURCEID'),
        REFRESOURCENAME: MAIN.getValue('TABLERESOURCENAME'),
      });
    } else {
      SELF.initData();
      SELF.add(item);
    }
  },
  SET_SELFDTS(state, { TYPE, item }) {
    let SELF = storeHelper.getTable('SELF');
    let SELFDTS = storeHelper.getTable('SELFDTS');
    let DTSA = storeHelper.getTable('DTSA');
    if (TYPE === 'MAIN') {
      SELFDTS.data.map(d => {
        if (
          DTSA.data.find(t => {
            return !t.UPFIELDID && t.REFFIELDID === d.ID;
          })
        ) {
          SELFDTS.setValue('ISREF', true, d);
        }
      });
    } else {
      let items = DTSA.data.filter(t => {
        return t.UPFIELDID === SELF.getValue('ID');
      });
      SELFDTS.data.map(d => {
        if (
          items.find(t => {
            return t.REFFIELDID === d.ID;
          })
        ) {
          SELFDTS.setValue('ISREF', true, d);
        }
      });
    }
  },
  SET_DTSA(state, { TYPE, items }) {
    let SELFDTS = storeHelper.getTable('SELFDTS');
    let DTSA = storeHelper.getTable('DTSA');
    if (TYPE === 'MAIN') {
      let titems = DTSA.data.filter(t => {
        return !t.UPFIELDID && t.REFFIELDID;
      });
      SELFDTS.data.map(t => {
        let titem = titems.find(tt => {
          return tt.REFFIELDID === t.ID;
        });
        if (items.indexOf(t) > -1) {
          if (!titem) {
            let item = {};
            item['REFFIELDID'] = t.ID;
            item['REFFIELDNAME'] = t.FIELDNAME;
            item['FIELDNAME'] = t.FIELDNAME;
            item['FIELDTYPE'] = t.FIELDTYPE;
            item['PREC'] = t.PREC;
            item['NULLABLE'] = t.NULLABLE;
            item['FIELDLENGTH'] = t.FIELDLENGTH;
            item['COMMENTS'] = t.COMMENTS;
            item['VFORMAT'] = t.VFORMAT;
            item['DEFAULTVALUE'] = t.DEFAULTVALUE;
            item['ISKEY'] = t.ISKEY;
            item['KEYGENTYPE'] = t.KEYGENTYPE;
            DTSA.add(item);
          }
        } else {
          if (titem) {
            DTSA.del(titem);
          }
        }
      });
    } else {
      let SELF = storeHelper.getTable('SELF');
      let nitem = DTSA.data.find(t => {
        return t.ID === SELF.getValue('ID');
      });
      DTSA.setValue('REFRESOURCEID', SELF.getValue('REFRESOURCEID'), nitem);
      DTSA.setValue('REFRESOURCENAME', SELF.getValue('REFRESOURCENAME'), nitem);
      DTSA.setValue('REFRESOURCEANAME', SELF.getValue('REFRESOURCEANAME'), nitem);
      DTSA.setValue('REFRELATION', SELF.getValue('REFRELATION'), nitem);
      let titems = DTSA.data.filter(t => {
        return t.UPFIELDID === SELF.getValue('ID');
      });
      SELFDTS.data.map(t => {
        let titem = titems.find(tt => {
          return tt.REFFIELDID === t.ID;
        });
        if (items.indexOf(t) > -1) {
          if (!titem) {
            let item = {};
            item['REFFIELDID'] = t.ID;
            item['REFFIELDNAME'] = t.FIELDNAME;
            item['FIELDNAME'] = t.FIELDNAME;
            item['FIELDTYPE'] = t.FIELDTYPE;
            item['PRECISION'] = t.PRECISION;
            item['NULLABLE'] = t.NULLABLE;
            item['FIELDLENGTH'] = t.FIELDLENGTH;
            item['COMMENTS'] = t.COMMENTS;
            item['VFORMAT'] = t.VFORMAT;
            item['DEFAULTVALUE'] = t.DEFAULTVALUE;
            item['ISKEY'] = t.ISKEY;
            item['KEYGENTYPE'] = t.KEYGENTYPE;
            item['UPFIELDID'] = SELF.getValue('ID');
            DTSA.add(item);
          }
        } else {
          if (titem) {
            DTSA.del(titem);
          }
        }
      });
    }
  },
  ADD_DTSA(state) {
    storeHelper.getTable('DTSA').add({});
  },
  ADD_DTSB(state) {
    storeHelper.getTable('DTSB').add({});
  },
  DEL_DTSB(state, { item }) {
    storeHelper.getTable('DTSB').del(item);
  },
  DEL_DTSC(state, { item }) {
    storeHelper.getTable('DTSC').del(item);
  },
  DEL_DTSA(state, { item }) {
    let DTSA = storeHelper.getTable('DTSA');
    let items = DTSA.data.filter(t => {
      return t.UPFIELDID === item.ID && item.ID && t.UPFIELDID;
    });
    items.map(t => DTSA.del(t));
    DTSA.del(item);
  },
  ADD(state, { path }) {
    storeHelper.getTable(path).add({});
  },
  DEL(state, { path, item }) {
    storeHelper.getTable(path).del(item);
  },
  SET_DTSAKEY(state, { KEYS }) {
    let DTSA = storeHelper.getTable('DTSA');
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
  SET_DTSC(state, { items }) {
    let DTSC = storeHelper.getTable('DTSC');
    items.map(item => {
      let t = {};
      t['RESOURCEID'] = item['RESOURCEID'];
      t['RESFIELDID'] = item['ID'];
      t['RESFIELDNAME'] = item['FIELDNAME'];
      t['FIELDNAME'] = item['FIELDNAME'];
      t['LABELNAME'] = item['COMMENTS'];
      t['MAXLENGTH'] = item['FIELDLENGTH'];
      DTSC.add(t);
    });
  },
  ADD_DTSC(state, { RESOURCEID, defaults }) {
    let DTSC = storeHelper.getTable('DTSC');
    const row = Object.assign({ RESOURCEID }, defaults || {});
    DTSC.add(row);
  },
};
const actions = {
  ...storeHelper.mixActions(),
  async query({ state, commit }) {
    let QQRY = storeHelper.getTable('QQRY');
    let PageSize = QQRY.getValue('PageSize');
    let PageIndex = QQRY.getValue('PageIndex');
    let INPUT = QQRY.getValue('INPUT');
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A01/',
      params: {
        PageSize,
        PageIndex,
        FilterParams: {
          INPUT,
        },
      },
    });
    QQRY.setValue('TotalCount', ret.TotalCount);

    commit(Constants.M_INITDATA, {
      path: 'QRY',
      data: ret.Items || [],
    });
  },
  async open({ state, commit }, { DID }) {
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A02/',
      params: {
        FilterParams: {
          RESOURCENAME: DID,
        },
      },
    });
    commit(Constants.M_INITBYPATH, {
      paths: ['MAIN'],
    });
    commit(Constants.M_INITDATA, {
      path: 'MAIN',
      data: ret.MAIN || [],
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSA',
      data: ret.DTSA || [],
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSB',
      data: ret.DTSB || [],
    });
  },
  async querySel({ state, commit }, { INPUT }) {
    // 查询资源
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A04/',
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
  async querySelT({ state, commit }, { INPUT }) {
    // 查询表资源
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A06/',
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
  async querySelF({ state, commit, dispatch }, { RESOURCEID, TYPE, item }) {
    // 查询表资源
    if (item) {
      RESOURCEID = item['REFRESOURCEID'];
    }
    commit('SET_SELF', {
      TYPE,
      item,
    });
    await dispatch('querySelsDts', {
      TYPE,
      RESOURCEID,
    });
  },
  async querySelsDts({ state, commit }, { TYPE, RESOURCEID }) {
    commit(Constants.M_INITBYPATH, {
      paths: ['SELFDTS'],
    });
    if (!RESOURCEID) {
      return;
    }
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A05/',
      params: {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          RESOURCEID,
        },
      },
    });

    ret.Items.map(item => (item['ISREF'] = false));
    commit(Constants.M_INITDATA, {
      path: 'SELFDTS',
      data: ret.Items || [],
    });
    commit('SET_SELFDTS', {
      TYPE,
      item,
    });
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
  init({ commit }) {
    commit(Constants.M_INITBYPATH, {
      paths: ['MAIN', 'DTSA', 'DTSB'],
    });
  },
  add({ commit }) {
    commit(Constants.M_INITBYPATH, {
      paths: ['MAIN', 'DTSA', 'DTSB'],
    });
  },
  async save({ commit }) {
    commit('SET_ENTRYNUM', { path: 'DTSA', });
    commit('SET_ENTRYNUM', { path: 'DTSB', });
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A03/',
      params: {
        MAIN: storeHelper.getTable('MAIN').getXML(),
        DTSA: storeHelper.getTable('DTSA').getXML(),
        DTSB: storeHelper.getTable('DTSB').getXML(),
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'MAIN',
      data: ret.MAIN || [],
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSA',
      data: ret.DTSA || [],
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSB',
      data: ret.DTSB || [],
    });
  },
  async delete() {
    storeHelper.getTable('MAIN').clear();
    storeHelper.getTable('DTSA').clear();
    storeHelper.getTable('DTSB').clear();
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A07/',
      params: {
        MAIN: storeHelper.getTable('MAIN').getXML(),
        DTSA: storeHelper.getTable('DTSA').getXML(),
        DTSB: storeHelper.getTable('DTSB').getXML(),
      },
    });
    console.log(ret);
  },
  async queryDTSC({ state, commit }, { RESOURCEID }) {
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A08/',
      params: {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          RESOURCEID,
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSC',
      data: ret.Items || [],
    });
  },
  async saveDTSC({ state, commit, rootState }) {
    // 保存前取 RESOURCEID，用于清除 scm 缓存
    const DTSC = storeHelper.getTable('DTSC');
    const firstRow = DTSC.data && DTSC.data[0];
    const resourceId = firstRow && firstRow.RESOURCEID;
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A09/',
      params: {
        DTSC: DTSC.getXML(),
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'DTSC',
      data: ret.DTSC || [],
    });
    // 清除 scm 缓存，使下次打开页面时重新加载最新的 UI 配置
    if (resourceId && rootState.app && rootState.app.scms) {
      const scms = rootState.app.scms;
      // 找到该 RESOURCEID 对应的 scm key 并删除
      Object.keys(scms).forEach(key => {
        if (scms[key] && scms[key].some && scms[key].some(item => item.RESOURCEID === resourceId)) {
          delete scms[key];
        }
      });
    }
  },
  async queryFIELDSEL({ state, commit }, { RESOURCEID }) {
    if (!RESOURCEID) {
      return;
    }
    let ret = await db.postData({
      api: '/api/data/call/RS_M01/A05/',
      params: {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          RESOURCEID,
        },
      },
    });
    commit(Constants.M_INITDATA, {
      path: 'SELFDTS',
      data: ret.Items || [],
    });
  },
  // 对比物理表结构与元数据字段
  async compareTable({ commit }, { TABLENAME, RESOURCEID }) {
    let ret = await db.postData({
      api: '/api/S01M01/compare',
      params: { TABLENAME, RESOURCEID },
    });
    return ret;
  },
  // 同步元数据字段到物理表
  async syncTable({ commit }, { TABLENAME, FIELDS }) {
    let ret = await db.postData({
      api: '/api/S01M01/sync',
      params: { TABLENAME, FIELDS: JSON.stringify(FIELDS) },
    });
    return ret;
  },
  // 刷新元数据：根据物理表列更新字段定义
  async refreshTable({ commit }, { TABLENAME, RESOURCEID }) {
    let ret = await db.postData({
      api: '/api/S01M01/refresh',
      params: { TABLENAME, RESOURCEID },
    });
    return ret;
  },
  // 查询未注册的物理表
  async queryUnregistered({ commit }, { INPUT }) {
    let ret = await db.postData({
      api: '/api/S01M01/unregistered',
      params: { INPUT: INPUT || '' },
    });
    return ret;
  },
  // 批量生成TABLE类型资源
  async batchCreateResources({ commit }, { TABLES }) {
    let ret = await db.postData({
      api: '/api/S01M01/batchCreate',
      params: { TABLES: JSON.stringify(TABLES) },
    });
    return ret;
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
