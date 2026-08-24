import createStore from '@/store/createStore';
import getBase from './baseStore';
let base = getBase();
base.storeHelper.getApiRow = function(actionCode, APICODE) {
  if (actionCode == 'query') {
    return base.storeHelper.moudle.getApi('', 'A25');
  }
  if (actionCode == 'advQuery') {
    return base.storeHelper.moudle.getApi('', 'A26');
  }
  return base.storeHelper.moudle.getApi(actionCode, APICODE);
};
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: base.config,
  storeHelper: base.storeHelper,
  mutations: base.mutations,
  actions: base.actions,
  storeName: 's01/m102',
});
export { mapState, mapGetters, mapDateTable, Constants };
