import createStore from '@/store/createStore';
import getBase from './baseStore';
let base = getBase();
base.storeHelper.getApiRow = function(actionCode, APICODE) {
  if (actionCode == 'query') {
    return base.storeHelper.moudle.getApi('', 'A36');
  }
  if (actionCode == 'advQuery') {
    return base.storeHelper.moudle.getApi('', 'A37');
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
  storeName: 'r01/m022',
});
export { mapState, mapGetters, mapDateTable, Constants };
