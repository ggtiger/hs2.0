import db from '@/api/db';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';
import { Store03, Constants } from '@/store/Store03';
import store from '@/store';
import heyui from 'heyui';
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
  const config = { moduleCode: 'RS_M10', paths: oSelStore.mixPaths(), apiPath: '/api/sm110/call' };
  const storeHelper = new Store03(config);
  return {
    config,
    storeHelper,
    mutations: {
      SETSAVEDATA(state, { inputObj, editorObj, tableObj }) {
        let MAIN = storeHelper.getTable('MAIN');
        let DTSA = storeHelper.getTable('DTSA');
        let keys = Object.keys(inputObj);
        let fields = MAIN.getFields();
        let okeys = {};
        let eMsg = [];
        let MANUFACTDATE = MAIN.getValue('MANUFACTDATE');
        let BILLDATE = MAIN.getValue('BILLDATE');
        debugger;
        if (MANUFACTDATE && BILLDATE) {
          if (MANUFACTDATE > BILLDATE) {
            throw new Error('样品接收日期不能大于校准日期！');
          }
        }
        Object.keys(inputObj).map(k => {
          if (inputObj[k].isnotnull && !inputObj[k].value) {
            eMsg.push(inputObj[k].content);
          }
        });
        if (eMsg.length > 0) { throw new Error(eMsg.join(',') + '，不可空！') }
        // 主编辑
        keys.map(k => {
          let v = inputObj[k].value;
          if (fields.indexOf(k) != -1) {
            if (k === 'DEPTNAME' && v && v.ID) {
              MAIN.setValue('DEPTID', v.ID);
              MAIN.setValue('DEPTNAME', v.DEPTNAME);
            } else {
              if (k != 'CHECKER') { MAIN.setValue(k, v) }
            }
          } else {
            okeys[k] = { field: k, name: inputObj[k].content, value: v, isnotnull: inputObj[k].isnotnull };
          }
        });
        // ueditor编辑
        editorObj.map(p => {
          let tfields = p.fields || [];
          tfields.map(f => {
            okeys[f.field] = { field: f.field, name: f.name, value: f.value, isnotnull: f.isnotnull };
          });
        });
        if (DTSA.data.length == 0) {
          for (let key in okeys) {
            DTSA.add({ FIELDNAME: okeys[key].field, FIELDREMARK: okeys[key].name, FIELDVALUE: okeys[key].value });
          }
        } else {
          DTSA.data.forEach(d => {
            if (okeys[d.FIELDNAME]) { DTSA.setValue('FIELDVALUE', okeys[d.FIELDNAME].value, d) } else {
              console.log(d.FIELDNAME);
            }
          });
        }
      },
      SETSHOWTPMDATA(state, { inputObj, editorObj, tableObj }) {
        let MAIN = storeHelper.getTable('MAIN');
        let DTSA = storeHelper.getTable('DTSA');
        debugger;
        let keys = Object.keys(inputObj);
        let fields = MAIN.getFields();
        keys.map(k => {
          let n = inputObj[k];
          if (fields.indexOf(k) != -1) {
            debugger;
            if (k === 'DEPTNAME') {
              // MAIN.setValue(k, `${v.STDDCODE}《${v.STDDNAME}》`);
              n.value = MAIN.getValue('DEPTNAME');
            } else {
              n.value = MAIN.getValue(k);
              if (n.type === 'itemLabel') {
                n.label = n.value || n.label;
                n.label = heyui.dictMapping(n.label, '单据状态') || n.label;
              }
            }
          } else {
            let tt = DTSA.data.find(d => d.FIELDNAME === k);
            if (tt) {
              n.value = tt.FIELDVALUE;
              n.name = tt.FIELDREMARK;
              n.field = tt.FIELDNAME;
              if (n.type === 'itemLabel') {
                n.label = heyui.dictMapping(n.label, '单据状态') || n.label;
              }
            }
          }
        });
        editorObj.map(p => {
          let tfields = p.fields || [];
          tfields.map(f => {
            let tt = DTSA.data.find(d => d.FIELDNAME === f.field);
            if (tt) {
              f.value = tt.FIELDVALUE;
              f.name = tt.FIELDREMARK;
              f.field = tt.FIELDNAME;
            }
          });
        });
      },
      SETFILEDATA(state, { files }) {
        let DTS = storeHelper.getTable('DTSB');
        DTS.clear();
        files.map(f => {
          DTS.add({ FILEID: f.id, FILENAME: f.name });
        });
      }
    },
    actions: {
      add({
        commit
      }, { item }) {
        commit('INIT', { paths: ['MAIN', 'DTSA'] });
        item = item || {};
        commit('ADD', {
          path: 'MAIN',
          item: {
            DOCNAME: '',
            DOCCODE: '',
            DOCSORT: '',
            DOCTYPEID: '',
            DEPTID: '',
            BILLDATE: '',
            FIELD1: '',
            FIELD2: '',
            FIELD3: '',
            FIELD4: '',
            CREATEREMARK: '',
            CREATER: '',
            CREATETIME: '',
            CHECKER: '',
            CHECKREMARK: '',
            CHECKTIME: '',
            VERIFIER: '',
            VERIFYREMARK: '',
            VERIFYTIME: '',
            PTEMPLATEID: item.ID
          }
        });
      },
      copyAdd({
        commit
      }) {
        let MAIN = storeHelper.getTable('MAIN');
        let DTSA = storeHelper.getTable('DTSA');
        let mainItem = {...MAIN.getRawItem()};
        mainItem.ID = '';
        mainItem.CREATEID = '';
        mainItem.CREATER = '';
        mainItem.CREATEREMARK = '';
        mainItem.CREATETIME = '';
        mainItem.SUBMITID = '';
        mainItem.SUBMITER = '';
        mainItem.SUMBMITTIME = '';
        mainItem.MODIFYID = '';
        mainItem.MODIFER = '';
        mainItem.MODIFYTIME = '';
        mainItem.CHECKID = '';
        mainItem.CHECKER = '';
        mainItem.CHECKREMARK = '';
        mainItem.CHECKTIME = '';
        mainItem.VERIFYID = '';
        mainItem.VERIFIER = '';
        mainItem.VERIFYREMARK = '';
        mainItem.VERIFYTIME = '';
        mainItem.STATE = '';
        let dtsItems = [];
        DTSA.data.map(r => {
          let dtsItem = {...DTSA.getRawItem(r)};
          dtsItem.ID = '';
          dtsItem.REFID = '';
          dtsItems.push(dtsItem);
        });
        commit('INIT', { paths: ['MAIN', 'DTSA'] });
        commit('ADD', {path: 'MAIN', item: mainItem});
        dtsItems.map(r => {
          commit('ADD', {path: 'DTSA', item: r});
        });
      },
      async openTMP({ state, commit }, { INPUT, ID, ISEDIT }) {
        // 查询表资源
        let ret = await db.postData({
          api: '/api/data/call/RS_M10/A14/',
          params: {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              INPUT: INPUT || '',
              ID: ID || -1
            },
          },
        });
        commit(Constants.M_INITDATA, {
          path: 'TPM',
          data: ret.Items || [],
        });
        if (ISEDIT && ret.Items.length > 0) {
          let MAIN = storeHelper.getTable('MAIN');
          MAIN.setValue('REFTPMDATA', JSON.parse(ret.Items[0]['TPMDATA']));
        }
      },
      async setTmpData({ state, commit }, { item }) {
        let MAIN = storeHelper.getTable('MAIN');
        MAIN.setValue('REFTPMDATA', JSON.parse(item['TPMDATA']));
        MAIN.setValue('PTEMPLATEID', item['ID']);
        dealTreeData(MAIN.getValue('REFTPMDATA') || [], MAIN);
      },
      async doMySave({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
        commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
        let MAIN = storeHelper.getTable('MAIN');
        ['CHECKER', 'CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME'].map(f => {
          MAIN.setValue(f, '');
        });
        await dispatch('save', {});
      },
      async doMySubmit({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
        commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
        let MAIN = storeHelper.getTable('MAIN');
        ['CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME'].map(f => {
          MAIN.setValue(f, '');
        });
        await dispatch('submit', {});
      },
      async callUpdate({
        commit, dispatch
      }, { APICODE, params, item, updateFields }) {
        let ret = await dispatch('call', { APICODE, params });
        updateFields.map(f => {
          item[f] = ret[0][f];
        });
      },
      async print({
        commit, dispatch
      }, { ID }) {
        return await dispatch('call', { APICODE: 'A27', params: { ID } });
      },
      async check({
        commit, dispatch
      }, { REMARK, ID, item, VERIFYID, VERIFYER }) {
        return await dispatch('callUpdate', { APICODE: 'A10', params: { REMARK, ID, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async reCheck({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A11', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async verify({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A12', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });

      },
      async reVerify({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A13', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });

      },
      async reject({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A14', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      ...oSelStore.mixActions()
    }
  };
};
export default getBase;
