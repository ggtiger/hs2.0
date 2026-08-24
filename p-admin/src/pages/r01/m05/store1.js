import createStore from "@/store/createStore";
import getBase from "./baseStore";
let base = getBase();
base.storeHelper.getApiRow = function(actionCode, APICODE) {
  if ('query' == actionCode) {
    return base.storeHelper.moudle.getApi("", "A011");
  }
  return base.storeHelper.moudle.getApi(actionCode, APICODE);
}
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: base.config,
  storeHelper: base.storeHelper,
  mutations: base.mutations,
  actions: {
    ...base.actions
    ,
  },
  storeName: 'r01/m051',
});
export { mapState, mapGetters, mapDateTable, Constants };
