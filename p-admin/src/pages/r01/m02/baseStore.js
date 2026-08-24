import db from '@/api/db';
import { SelStore } from '@/store/SelStore';
import { dateToString } from 'rs-vcore/utils/Date';
import { Store03, Constants } from '@/store/Store03';
import store from '@/store';
let oSelStore = new SelStore();
let dealTreeData = function(node, MAIN) {
  node.map(n => {
    if (n.field) {
      if (n.field == 'Modifications') {
      }
      n.value = MAIN.getValue(n.field) || n.dvalue;
      n.label = n.value || n.label;
    }
    if (n.children && n.children.length > 0) {
      dealTreeData(n.children, MAIN);
    }
  });
  return node;
};
let getBase = () => {
  const config = { moduleCode: 'LI_M02',
    paths: oSelStore.mixPaths(),
    apiPath: '/api/rm11/call',
  };
  const storeHelper = new Store03(config);
  return {
    config,
    storeHelper,
    mutations: {
      SETTPMDATA(state, { item, aitem }) {
        let MAIN = storeHelper.getTable('MAIN');
        let ACCEPT = storeHelper.getTable('ACCEPT');
        // MAIN.setValue("REFTPMDATA", JSON.parse(item.REFTPMDATA));
        /* DOCCODE			文档受控号
        ORGNAME			机构名称
        DOCTITLE			文档标题
        CERTCODE			证书编号
        MNAME			设备名称
        CUSTNAME			送校单位
        ADDR			委托方地址
        SIZETYPE			型号规格
        OMCODE			出厂编号
        MANUFACTURER			生产厂家 */
        MAIN.setValue('DOCCODE', item.DOCCODE);
        MAIN.setValue('DOCTITLE', '');
        MAIN.setValue('CERTCODE', item.CERTCODE);
        MAIN.setValue('ADEPTID', ACCEPT.getValue('ADEPTID', aitem));
        MAIN.setValue('MNAME', ACCEPT.getValue('MNAME', aitem));
        MAIN.setValue('ADDR', ACCEPT.getValue('ADDR', aitem));
        MAIN.setValue('MOBILE', ACCEPT.getValue('MOBILE', aitem));
        MAIN.setValue('LINKER', ACCEPT.getValue('LINKER', aitem));
        MAIN.setValue('SIZETYPE', ACCEPT.getValue('SIZETYPE', aitem));
        MAIN.setValue('ORGNAME', item.ORGNAME);
        MAIN.setValue('OPCODE', ACCEPT.getValue('OPCODE', aitem));
        MAIN.setValue('MANUFACTURER', ACCEPT.getValue('MANUFACTURER', aitem));
        MAIN.setValue('TSTANDARDID', item.TSTANDARDID);
        MAIN.setValue('TSTANDARDNAME', `${item.STDDCODE}《${item.STDDNAME}》`);
        MAIN.setValue('REGUITEMID', item.REGUITEMID);
        MAIN.setValue('REGUITEMNAME', `${item.REGUITEMCODE} ${item.REGUITEMNAME}`);
        /*
        CUSTID ,CUSTNAME客户
        REFBILLID，REFBILLCODE 引用
        TSTANDARDID，TSTANDARDNAME
        */
        MAIN.setValue('CUSTID', ACCEPT.getValue('CUSTID', aitem));
        MAIN.setValue('CUSTNAME', ACCEPT.getValue('CUSTNAME', aitem));
        MAIN.setValue('REFBILLID', ACCEPT.getValue('ID', aitem));
        MAIN.setValue('REFBILLCODE', ACCEPT.getValue('BILLCODE', aitem));

        MAIN.setValue('REFBILLCODE', ACCEPT.getValue('BILLCODE', aitem));
        MAIN.setValue('BILLDATE', dateToString(new Date()));
        MAIN.setValue('CREATER', store.state['user'].userInfo.NICKNAME);
        MAIN.setValue('CHUMIDITY', '');
        MAIN.setValue('CTEMPERATURE', '');
        MAIN.setValue('ISONSITE', 0);
        MAIN.setValue('CADDR', ACCEPT.getValue('ADDR', aitem));
        MAIN.setValue('CHECKER', '');
        MAIN.setValue('PTEMPLATEID', item.ID);
        MAIN.setValue('EXPDATE', '');
        MAIN.setValue('VER', 1);
        MAIN.setValue('MANUFACTDATE', '');
        MAIN.setValue('BEFOREUSE', '');
        MAIN.setValue('AFTERUSE', '');
        MAIN.setValue('ATMOS', '');
        MAIN.setValue('OTHER', '');
      },
      SETSAVEDATA(state, { inputObj, editorObj, tableObj }) {
        let MAIN = storeHelper.getTable('MAIN');
        let DTSA = storeHelper.getTable('DTSA');
        let DTSB = storeHelper.getTable('DTSB');
        let keys = Object.keys(inputObj);
        let fields = MAIN.getFields();
        let okeys = {};
        let eMsg = [];

        Object.keys(inputObj).map(k => {
          if (inputObj[k].isnotnull && !inputObj[k].value) {
            eMsg.push(inputObj[k].content);
          }
        });
        if (eMsg.length > 0) { throw new Error(eMsg.join(',') + '，不可空！') }
        // 主编辑
        keys.map(k => {
          let v = inputObj[k].value;
          if (inputObj[k].type == 'itemLabel' && !v) {
            v = inputObj[k].label;
          }
          if (fields.indexOf(k) != -1) {
            if (k === 'REGUITEMNAME' && v.ID) {
              MAIN.setValue('REGUITEMID', v.ID);
              MAIN.setValue(k, `${v.ITEMCODE} ${v.ITEMNAME}`);
            } else {
              if (k != 'CHECKER') { MAIN.setValue(k, v) }
            }
          } else {
            okeys[k] = { field: k, name: inputObj[k].content, value: v };
          }
        });
        // ueditor编辑
        editorObj.map(p => {
          let tfields = p.fields || [];
          tfields.map(f => {
            okeys[f.field] = { field: f.field, name: f.name, value: f.value };
          });
        });
        if (DTSB.data.length == 0) {
          for (let key in okeys) {
            DTSB.add({ FIELDNAME: okeys[key].field, FIELDREMARK: okeys[key].name, FIELDVALUE: okeys[key].value });
          }
        } else {
          let tokeys = Object.keys(okeys).filter(t => {
            return !DTSB.data.find(tt => { return tt.FIELDNAME == t });
          });
          tokeys.forEach(key => {
            DTSB.add({ FIELDNAME: okeys[key].field, FIELDREMARK: okeys[key].name, FIELDVALUE: okeys[key].value });
          });

          DTSB.data.forEach(d => {
            if (okeys[d.FIELDNAME]) { DTSB.setValue('FIELDVALUE', okeys[d.FIELDNAME].value, d) } else {
              console.log(d.FIELDNAME);
            }
          });
        }
        DTSA.clear();
        let isError = false;
        Object.values(tableObj).map(t => {
          let v = t.value || [];
          v.map(item => {
            if (DTSA.data.find(dd => {
              return dd.ARDID === item.ID;
            })) {
              isError = true;
            }
            DTSA.add({ ARDID: item.ID, ARDNAME: item.ARDNAME, SIZETYPE: item.SIZETYPE, OMCODE: item.OMCODE, DEGREE: item.DEGREE, EXPDATE: item.EXPDATE, CERTCODE: item.CERTCODE, CORGNAME: item.CORGNAME });
          });
        });
        if (isError) {
          throw new Error('标准器重复！');
        }
        let MANUFACTDATE = MAIN.getValue('MANUFACTDATE');
        let BILLDATE = MAIN.getValue('BILLDATE');
        let SIGNDATE = MAIN.getValue('SIGNDATE');

        if (MANUFACTDATE && BILLDATE) {
          if (MANUFACTDATE > BILLDATE) {
            throw new Error('样品接收日期不能大于校准日期！');
          }
        }
        if (SIGNDATE && BILLDATE) {
          if (BILLDATE > SIGNDATE) {
            throw new Error('签发日期需大于校准日期！');
          }
        }
      },
      SETSHOWTPMDATA(state, { inputObj, editorObj, tableObj }) {
        // TODO:放到其他位置
        let MAIN = storeHelper.getTable('MAIN');
        let DTSA = storeHelper.getTable('DTSA');
        let DTSB = storeHelper.getTable('DTSB');
        let ARD = storeHelper.getTable('ARD');
        let keys = Object.keys(inputObj);
        let fields = MAIN.getFields();
        keys.map(k => {
          if (fields.indexOf(k) != -1) {
            if (k === 'TSTANDARDNAME') {
              // MAIN.setValue(k, `${v.STDDCODE}《${v.STDDNAME}》`);
              inputObj[k].value = MAIN.getValue(k);
            } else {
              inputObj[k].value = MAIN.getValue(k);
            }

          } else {
            let tt = DTSB.data.find(d => d.FIELDNAME === k);
            if (tt) {
              inputObj[k].value = tt.FIELDVALUE;
              inputObj[k].name = tt.FIELDREMARK;
              inputObj[k].field = tt.FIELDNAME;
            }
          }
        });
        editorObj.map(p => {
          let tfields = p.fields || [];
          tfields.map(f => {
            let tt = DTSB.data.find(d => d.FIELDNAME === f.field);
            if (tt) {
              f.value = tt.FIELDVALUE;
              f.name = tt.FIELDREMARK;
              f.field = tt.FIELDNAME;
            }
          });
        });
        Object.values(tableObj).map(t => {
          let v = t.value || [];
          if (DTSA.count() == 0) {
            ARD.data.map(item => { v.push(item) });
            ARD.initData([]);
          }
          DTSA.data.map(item => {
            v.push({ ID: item.ARDID, ARDNAME: item.ARDNAME, SIZETYPE: item.SIZETYPE, OMCODE: item.OMCODE, DEGREE: item.DEGREE, CERTCODE: item.CERTCODE, EXPDATE: item.EXPDATE, CORGNAME: item.CORGNAME });
          });
        });
      },
      SETLOGDATA(state) {
        let LOG = storeHelper.getTable('LOG');
        let OPLOGS = storeHelper.getTable('OPLOGS');
        let items = [];
        LOG.data.map(p => {
          let d = JSON.parse(p.LOGDATA);
          d.map(pp => {
            items.push({ ...pp, 操作人: p.CREATER, 操作时间: p.CREATEDATE });
          });
        });
        OPLOGS.setData(items);
      },
      SETFILEDATA(state, { files }) {
        let DTS = storeHelper.getTable('DTSD');
        DTS.clear();
        files.map(f => {
          DTS.add({ FILEID: f.id, FILENAME: f.name });
        });
      }
    },
    actions: {
      add({
        dispatch, commit
      }) {
        commit('INIT', { paths: ['MAIN', 'DTSA', 'DTSB', 'DTSC', 'ARD', 'DTSD'] });

        commit('ADD', { path: 'MAIN', item: { ISUSE: 1 } });
      },
      async printPreview({
        commit
      }, { ID }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A49/',
          params: {
            ID
          },
        });
        return ret;
      },
      async genCert({
        commit
      }, { ID }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A21/',
          params: {
            TYPE: 'GENCERT',
            ID
          },
        });
      },
      async getBillCode({
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
      async invalid({
        commit
      }, { ID }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A22/',
          params: {
            ID
          },
        });
        return ret;
      },
      async openPTEMP({ state, commit, dispatch }, { ID, ISEDIT, item }) {
        sessionStorage.setItem('hlims_ueditor_fmFields', JSON.stringify({}));
        // 查询表资源
        let ret = await db.postData({
          api: '/api/data/call/LI_M02/A08/',
          params: {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID,
            },
          },
        });
        commit(Constants.M_INITDATA, {
          path: 'PTEMPSEL',
          data: ret.Items || [],
        });
        let PTEMPSEL = storeHelper.getTable('PTEMPSEL');
        let MAIN = storeHelper.getTable('MAIN');
        let REFTPMDATA = MAIN.getValue('TPMDATA');
        if (!REFTPMDATA) {
          MAIN.setValue('TPMDATA', PTEMPSEL.getValue('REFTPMDATA'));
          MAIN.setValue('REFTPMDATA', JSON.parse(PTEMPSEL.getValue('REFTPMDATA')));
        } else {
          MAIN.setValue('REFTPMDATA', JSON.parse(REFTPMDATA));
        }
        if (ISEDIT !== true) {

          if (ret.Items) {
            await dispatch('ardSel', {
              INPUT: '-------------',
              TSTANDARDID: ret.Items[0]['TSTANDARDID']
            });
            let bcode = await dispatch('getBillCode', {
              TCODE: ret.Items[0]['CERTCODE']
            });
            if (bcode) {
              ret.Items[0]['CERTCODE'] = bcode;
            }
            commit('SETTPMDATA', { item: ret.Items[0], aitem: item });
          } else {
            commit('SETTPMDATA', { aitem: item });
          }
          // 复制附件
          // 查询表资源
          let files = await db.postData({
            api: '/api/data/call/LI_M02/A51/',
            params: {
              PageSize: 1,
              PageIndex: 1,
              FilterParams: {
                ID: storeHelper.getTable('ACCEPT').getValue('ID', item),
              },
            },
          });
          files = files.Items.map(f => {
            return {id: f.FILEID, name: f.FILENAME};
          });
          commit('SETFILEDATA', { files });
        }

        dealTreeData(MAIN.getValue('REFTPMDATA') || [], MAIN);
      },
      async queryLog({ state, commit }, { ID }) {
        // 查询表资源
        let ret = await db.postData({
          api: '/api/data/call/LI_M02/A31/',
          params: {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID,
            },
          },
        });
        commit(Constants.M_INITDATA, {
          path: 'LOG',
          data: ret.Items || [],
        });
        commit('INIT', { paths: ['OPLOGS'] });
        commit('SETLOGDATA', {});
      },
      async doMySave({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
        commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
        let MAIN = storeHelper.getTable('MAIN');
        ['CHECKER', 'CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME'].map(f => {
          MAIN.setValue(f, '');
        });
        await dispatch('save', {});
        commit('INIT', { paths: ['ARD'] });
      },
      async doMySubmit({ state, commit, dispatch }, { inputObj, editorObj, tableObj }) {
        commit('SETSAVEDATA', { inputObj, editorObj, tableObj });
        let MAIN = storeHelper.getTable('MAIN');
        ['CHECKREMARK', 'CHECKTIME', 'VERIFIER', 'VERIFYREMARK', 'VERIFYTIME'].map(f => {
          MAIN.setValue(f, '');
        });
        await dispatch('submit', {});
        commit('INIT', { paths: ['ARD'] });
      },
      async accept({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A11', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async reAccept({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A30', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async callUpdate({
        commit, dispatch
      }, { APICODE, params, item, updateFields }) {
        let ret = await dispatch('call', { APICODE, params });
        updateFields.map(f => {
          item[f] = ret[0][f];
        });
      },
      async check({
        commit, dispatch
      }, { REMARK, ID, item, VERIFYID, VERIFYER }) {
        return await dispatch('callUpdate', { APICODE: 'A12', params: { REMARK, ID, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async reCheck({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A13', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async verify({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A14', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async reVerify({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A15', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async reject({
        commit, dispatch
      }, { REMARK, ID, item }) {
        return await dispatch('callUpdate', { APICODE: 'A16', params: { REMARK, ID }, item, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchCheck({
        commit, dispatch
      }, { items, REMARK, VERIFYID, VERIFYER }) {
        await dispatch('batch', { APICODE: 'A23', items, params: { REMARK, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME'] });
      },
      async batchCheckReject({
        commit, dispatch
      }, { items, REMARK }) {
        await dispatch('batch', { APICODE: 'A28', items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME'] });
      },
      async batchReCheck({
        commit, dispatch
      }, { items, REMARK }) {
        await dispatch('batch', { APICODE: 'A24', items, updateFields: ['STATE', 'CHECKER', 'CHECKTIME'] });
      },
      async batchVerify({
        commit, dispatch
      }, { items, REMARK }) {
        await dispatch('batch', { APICODE: 'A25', items, params: { REMARK }, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchVerifyReject({
        commit, dispatch
      }, { items, REMARK }) {
        await dispatch('batch', { APICODE: 'A29', items, params: { REMARK }, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchReVerify({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A26', items, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchGenCert({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A27', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async batchReGenCert({
        commit, dispatch
      }, { items }) {
        await dispatch('batch', { APICODE: 'A50', items, updateFields: ['STATE', 'AEMPID', 'AEMPNAME'] });
      },
      async eCertSign({
        commit, dispatch
      }, { ID, ECERTPWD }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A55/',
          params: { ID, ECERTPWD },
        });
        return ret;
      },
      async batchECertSign({
        commit, dispatch
      }, { items, ECERTPWD }) {
        await dispatch('batch', { APICODE: 'A55', items, params: { ECERTPWD }, updateFields: ['ECERTSIGN', 'ECERTSIGNDATE', 'ECERTSIGNER', 'ECERTPWD'] });
      },
      async updateECertPwd({
        commit, dispatch
      }, { ID, ECERTPWD }) {
        let ret = await db.postData({
          api: '/api/rm11/call/LI_M02/A58/',
          params: { ID, ECERTPWD },
        });
        return ret;
      },
      async batchUpdateTemplate({
        commit, dispatch
      }, { items, REMARK, CHECKID, CHECKER }) {
        await dispatch('batch', { APICODE: 'A45', items, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async download({
        commit, dispatch
      }, { items }) {
        return await dispatch('batch', { APICODE: 'A39', items, updateFields: ['STATE'] });
      },
      async batchUpdateECertPwd({
        commit, dispatch
      }, { items, ECERTPWD }) {
        await dispatch('batch', { APICODE: 'A58', items, params: { ECERTPWD }, updateFields: ['ECERTPWD'] });
      },
      ...oSelStore.mixActions()
    }
  };
};
export default getBase;
