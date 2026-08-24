import createStore from '@/store/createStore';
import getBase from './baseStore';
let base = getBase();
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: base.config,
  storeHelper: base.storeHelper,
  mutations: base.mutations,
  actions: base.actions,
  storeName: 'r01/m02',
});
export { mapState, mapGetters, mapDateTable, Constants };
