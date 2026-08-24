import db from "@/api/db"
import createStore from "@/store/createStore";
import { dateToString } from "rs-vcore/utils/Date";
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'LIR_M02' },
  storeName: 'r02/m02',
  mutations: {
  },
  actions: {

  }
});

export { mapState, mapGetters, mapDateTable, Constants };
