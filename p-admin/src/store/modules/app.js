import Vue from 'vue';
import db from '@/api/db';
import { registerGenericRoute } from '@/router/index';

const state = {
  scms: {},
  dicts: {},
  menus: [],
  omenus: [],
  modules: {},
};
const getters = {};
const mutations = {
  'INIT'(state) {
    state.scms = {};
    state.dicts = {};
    state.menus = [];
    state.omenus = [];
    state.modules = {};
  },
  'SET_SCMS'(state, { items }) {
    let mname = '';
    let titems = [];
    items.map(item => {
      if (item['RESOURCENAME'] !== mname) {
        if (titems.length > 0) {
          Vue.set(state.scms, mname, titems);
          titems = [];
        }
        mname = item['RESOURCENAME'];
      }
      titems.push(item);
    });
    if (titems.length > 0) {
      Vue.set(state.scms, mname, titems);
    }
  },
  'SET_DICTS'(state, { items }) {
    items.map(item => {
      let { DICTNAME, ITEMNAME, ITEMVALUE } = item;
      if (state.dicts[DICTNAME]) {
        state.dicts[DICTNAME][ITEMVALUE || ITEMNAME] = ITEMNAME;
      } else {
        state.dicts[DICTNAME] = {};
        state.dicts[DICTNAME][ITEMVALUE || ITEMNAME] = ITEMNAME;
      }
    });

  },
  'SET_MENUS'(state, { items, pitems }) {
    let getTreeData = function(datas, up) {
      let aobj = [];
      aobj = datas.filter(item => (item.UPFUNCID || '') === up && item.ISHIDE !== 1);
      // 按 SORTCODE 优先，FUNCCODE 次之排序
      aobj.sort(function(a, b) {
        let sortA = a.SORTCODE || 0;
        let sortB = b.SORTCODE || 0;
        if (sortA !== sortB) return sortA - sortB;
        let codeA = (a.FUNCCODE || '') + '';
        let codeB = (b.FUNCCODE || '') + '';
        if (codeA < codeB) return -1;
        if (codeA > codeB) return 1;
        return 0;
      });
      aobj.forEach(element => {
        let tobj = getTreeData(datas, element.ID);
        if (tobj.length > 0) {
          element.children = tobj;
        }
      });
      aobj = aobj.map(item => { return { FUNCTYPE: item.FUNCTYPE, key: item.OUTERURL || item.FUNCCODE, title: item.FUNCNAME, children: item.children, icon: item.FUNCICON, moduleCode: item.FUNCCODE } });
      return aobj;
    };
    let obj = getTreeData(items, '');
    obj = obj.filter(m => {
      return !(m.FUNCTYPE === 1 && (!m.children || m.children.every(c => c.FUNCTYPE === 1)));
    });
    state.menus = obj;
    state.omenus = items;
    let fpoints = {};
    pitems.map(p => {
      fpoints[p.FUNCCODE + '/' + p.FUNCPOINTCODE] = 1;
    });
    state.fpoints = fpoints;
    state.ofpoints = pitems;
  },
  'SET_MODULE'(state, { moduleCode, data }) {
    // Vue 2 响应式: 新增属性必须用 Vue.set, 否则依赖 modules[moduleCode] 的 computed 不会重算
    Vue.set(state.modules, moduleCode, data);
  }
};

// 并发控制：缓存正在进行的 initScms 请求，避免多个组件同时 dispatch 导致重复 HTTP 请求被 axios removePending cancel
let _initScmsPromise = null;

const actions = {
  async initScms({ state, commit }, names) {
    let fnames = names.filter(t => {
      return !state.scms[t];
    });
    if (fnames.length === 0) return;
    // 如果已有进行中的请求，等待它完成后再检查（此时 state.scms 可能已更新）
    if (_initScmsPromise) {
      await _initScmsPromise;
      fnames = fnames.filter(t => !state.scms[t]);
      if (fnames.length === 0) return;
    }
    _initScmsPromise = db.postData({
      'api': '/api/outer/call/C00/A02/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          RESOURCENAMES: fnames
        },
        OrderBy: 'RESOURCEID,ENTRYNUM'
      }
    }).then(ret => {
      commit('SET_SCMS', { items: ret.Items });
    }).finally(() => {
      _initScmsPromise = null;
    });
    await _initScmsPromise;
  },
  async initDict({ state, commit }, names) {
    let ret = await db.postData({
      'api': '/api/outer/call/C00/A03/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
        },
      }
    });
    commit('SET_DICTS', { items: ret.Items });
  },
  async initMenu({ state, commit }, USERID) {
    let ret = await db.postData({
      'api': '/api/outer/call/C00/A04/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          ID: USERID
        },
      }
    });
    let ret2 = await db.postData({
      'api': '/api/outer/call/C00/A06/',
      'params': {
        PageSize: 1,
        PageIndex: 1,
        FilterParams: {
          ID: USERID
        },
      }
    });
    commit('SET_MENUS', { items: ret.Items, pitems: ret2.Items });
  },
  async initModule({ state, commit }, moduleCode) {
    let ret = await db.postData({
      'api': '/api/outer/call/RS_M00/A03',
      'params': {
        FilterParams: {
          MODULECODE: moduleCode
        },
      }
    });
    commit('SET_MODULE', { moduleCode, data: ret });
    // 如果模块有页面配置，自动注册通用路由
    if (ret && ret.MODPAGE && ret.MODPAGE.length > 0) {
      registerGenericRoute(moduleCode, ret);
    }
  }
};

export default {
  namespaced: true,
  state,
  mutations,
  getters,
  actions
};
