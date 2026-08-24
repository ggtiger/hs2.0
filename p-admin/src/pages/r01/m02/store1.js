import createStore from '@/store/createStore';
import getBase from './baseStore';
let base = getBase();
base.storeHelper.getApiRow = function(actionCode, APICODE) {
  if (actionCode == 'query') {
    return base.storeHelper.moudle.getApi('', 'A34');
  }
  if (actionCode == 'advQuery') {
    return base.storeHelper.moudle.getApi('', 'A35');
  }
  return base.storeHelper.moudle.getApi(actionCode, APICODE);
};
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: base.config,
  storeHelper: base.storeHelper,
  mutations: base.mutations,
  actions: {
    ...base.actions
    ,
  },
  storeName: 'r01/m021',
});
export { mapState, mapGetters, mapDateTable, Constants };
