import user from '@/api/user';
import { clearModuleCache } from '@/sfc-loader';
import { clearStoreCache } from '@/components/generic-module/generic-store';
console.log('login', user);
const state = {
  userInfo: {},
  form_email: '',
  form_password: '',
  form_recordPass: true,
  access_token: '',
};
const mutations = {
  'INIT_DATA': function(state) {
    state.userInfo = {};
  },
  'SET_DATA': function(state, data) {
    state.userInfo = data.userInfo;
    if (data && data.token) { state.access_token = data.token.access_token }
  },
  'SET_LOGINSTATUS': function(state) {
    let userInfo = state.userInfo || {};
    let ERRMESSAGE = '';
    if (userInfo && userInfo.status === '2') {
      state.ISLOGIN = true;
    } else {
      if (userInfo['ISUSE'] === '0') {
        ERRMESSAGE = '用户已停用！';
      } else if (userInfo['status'] === 3) {
        ERRMESSAGE = '用户名不存在！';
      } else {
        ERRMESSAGE = '用户名密码不匹配！';
      }
    }
    state.ERRMESSAGE = ERRMESSAGE;
  },
};
const getters = {
  isLogin(state) {
    let userInfo = state.userInfo || {};
    return userInfo.status === 2;
  }
};
const actions = {
  async login({ commit }, userInfo) {
    let res = await user.login(userInfo);
    commit('SET_DATA', res);
    commit('SET_LOGINSTATUS');
    return res;
  },
  async loginOut({ commit, state }) {
    await user.loginout(state.userInfo);
    // commit('SET_DATA', { token: {} });
    commit('app/INIT', { items: [] }, { root: true });
    // 清除 SFC 模块缓存和通用 store 缓存
    clearModuleCache();
    clearStoreCache();
  },
  async resetPass({ commit, state }, { params }) {
    await user.resetPass(params);
  }
};
export default {
  namespaced: true,
  state,
  mutations,
  getters,
  actions
};
