import createStore from '@/store/createStore';

let { mapState, mapGetters, mapDateTable, Constants } = createStore.getStore({
  config: { moduleCode: 'RS_M15' },
  storeName: 's01/m15'
});

export { mapState, mapGetters, mapDateTable, Constants };
