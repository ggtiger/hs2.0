import db from "@/api/db"
import createStore from "@/store/createStore";
let { mapState, mapGetters, mapDateTable, Constants, storeHelper } = createStore.getStore({
  config: { moduleCode: 'OUT_M01',apiPath:'/api/outer/call' },
  storeName: 'out/m01',
  mutations: {
  },
  actions: {
  }
});

export { mapState, mapGetters, mapDateTable, Constants };
