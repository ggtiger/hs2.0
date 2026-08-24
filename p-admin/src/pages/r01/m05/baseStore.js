import db from '@/api/db';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';
import { Store03, Constants } from '@/store/Store03';
import store from '@/store';
let oSelStore = new SelStore();
let dealTreeData = function(node, MAIN) {
  node.map(n => {
    if (n.field) {
      n.value = MAIN.getValue(n.field);
      n.label = n.value || n.label;
    }
    if (n.children && n.children.length > 0) {
      dealTreeData(n.children, MAIN);
    }
  });
  return node;
};
let getBase = () => {
  const config = { moduleCode: 'LI_M00',
    paths: oSelStore.mixPaths(),
    apiPath: '/api/rm15/call',
  };
  const storeHelper = new Store03(config);
  return {
    config,
    storeHelper,
    mutations: {
      SET_ENDISABLE(state, { item }) {
        let UPDATE = storeHelper.getTable('UPDATE');
        UPDATE.setValue('ISUSE', item.ISUSE === 1 ? 0 : 1);
        UPDATE.setValue('ID', item.ID);
      },
      SETTPMDATA(state, { item }) {
        let UPDATE = storeHelper.getTable('UPDATE');
        UPDATE.setValue('TPMDATA', item.TPMDATA);
        UPDATE.setValue('ID', item.ID);
      },
      SETFILEDATA(state, { files }) {
        let DTS = storeHelper.getTable('DTS');
        DTS.clear();
        files.map(f => {
          DTS.add({ FILEID: f.id, FILENAME: f.name });
        });
      },
    },
    actions: {
      async getBillCode2({
        commit
      }, { TCODE }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A44/',
          params: {
            TCODE
          },
        });
        return ret;
      },
      async add({
        commit, dispatch
      }, { item }) {
        commit('INIT', { paths: ['MAIN', 'DTS'] });
        console.log('add', 1);
        if (!dispatch) return;
        let {SENDNAME, SENDDATE, GETDATE, AGREEDATE, BILLDATE, WTCODE, WCUSTNAME, SLINKER, CUSTID, CUSTNAME, LINKER, MOBILE, ADDR, EMAIL, MNAME, SIZETYPE, CNT, OPCODE, MANUFACTURER, PTEMPLATEID, PTEMPLATENAME, CAMT, OAMT, ADEPTID, ADEPTNAME, AEMPID, AEMPNAME } = item;
        commit('ADD', { path: 'MAIN', item: { WTCODE, SENDNAME, SENDDATE, GETDATE, AGREEDATE, WCUSTNAME, SLINKER, BILLDATE: BILLDATE || dateToString(new Date()), CUSTID, CUSTNAME, LINKER, MOBILE, ADDR, EMAIL, MNAME, SIZETYPE, CNT, OPCODE, MANUFACTURER, PTEMPLATEID, PTEMPLATENAME, CAMT, OAMT, ADEPTID, ADEPTNAME, AEMPID, AEMPNAME } });
      },
      async endisable({
        commit, dispatch
      }, { item }) {
        commit('SET_ENDISABLE', { item });
        let ret = await dispatch('call', {
          APICODE: 'A07',
          params: {
            'UPDATE': storeHelper.getTable('UPDATE').getXML()
          }
        });
        if (ret.length > 0) {
          for (let a in ret[0]) {
            item[a] = ret[0][a];
          }
        }
      },
      async updateTPMDATA({
        commit, dispatch
      }, { item }) {
        commit('SETTPMDATA', { item });
        let ret = await dispatch('call', {
          APICODE: 'A08',
          params: {
            'UPDATE': storeHelper.getTable('UPDATE').getXML()
          }
        });
        if (ret.length > 0) {
          for (let a in ret[0]) {
            item[a] = ret[0][a];
          }
        }
      },
      async querySel({ state, commit }, { INPUT }) {
        // 查询表资源
        let ret = await db.postData({
          api: '/api/data/call/LI_M05/A06/',
          params: {
            PageSize: 20,
            PageIndex: 1,
            FilterParams: {
              INPUT,
            },
          },
        });
        commit(Constants.M_INITDATA, {
          path: 'SEL',
          data: ret.Items || [],
        });
      },
      async batchSubmit({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A12', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async batchReSubmit({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A13', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async batchComplete({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A23', items, updateFields: ['STATE'] });
      },
      async batchReturn({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A51', items, updateFields: ['STATE'] });
      },
      async batchReComplete({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A24', items, updateFields: ['STATE'] });
      },
      async batchAccept({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A14', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async batchReAccept({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A15', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async print({
        commit, dispatch
      }, { ID }) {
        await dispatch('call', { APICODE: 'A17', params: { ID } });
      },
      async download({
        commit, dispatch
      }, { items }) {
        return await dispatch('batch', { APICODE: 'A20', items, updateFields: ['STATE'] });
      },
      async aprint({
        commit, dispatch
      }, { items }) {
        return await dispatch('batch', { APICODE: 'A21', items });
      },
      async pprint({
        commit, dispatch
      }, { items }) {
        return await dispatch('batch', { APICODE: 'A22', items });
      },
      // 检查受理单是否已有物流记录（R02_M07/A08 查询）
      async checkLogisticsExists(ctx, { acceptId }) {
        let ret = await db.postData({
          api: '/api/data/call/R02_M07/A08/',
          params: {
            FilterParams: { ACCEPTID: acceptId },
            PageSize: 1,
            PageIndex: 1,
          },
        });
        return (ret && ret.Items) || [];
      },
      // 加载项目费用（LI_PROJECT_FEE/A01 查询，返回首条）
      async loadProjectFee(ctx, { templateId }) {
        let ret = await db.postData({
          api: '/api/data/call/LI_PROJECT_FEE/A01/',
          params: {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: { TEMPLATE_ID: templateId },
          },
        });
        let items = (ret && ret.Items) || [];
        return items[0] || null;
      },
      ...oSelStore.mixActions()
    }
  };
};
export default getBase;
