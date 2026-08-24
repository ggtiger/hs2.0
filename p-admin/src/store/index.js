import Vue from 'vue';
import Vuex from 'vuex';
import app from './modules/app';
import user from './modules/user';
import assistant from './modules/assistant';
import formContext from './modules/formContext';
import sfcContext from './modules/sfcContext';
import createPersistedState from 'vuex-persistedstate';
Vue.use(Vuex);
const store = new Vuex.Store({
  state: {},
  modules: {
    app, user, assistant, formContext, sfcContext
  },
  getters: {},
  actions: {},
  plugins: [
    createPersistedState({ paths: ['user', 'app'], storage: window.sessionStorage })
  ]

});
export default store;
