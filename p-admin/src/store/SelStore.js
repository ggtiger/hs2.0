// 查询接口store
import { Constants as SConstants } from './BaseStore';
import db from '@/api/db';
import Store from './index';
const Constants = Object.assign({}, SConstants, {});

class Moudle2 {
  constructor(MODULECODE, data) {
    this.MODULECODE = MODULECODE;
    this.MODPATH = data.MODPATH;
    this.MODAPI = data.MODAPI;
    this.MOD = data.MOD;
  }
  getApi(actionCode, APICODE) {
    actionCode = actionCode || APICODE;
    return this.MODAPI.find(item => item.ACTIONCODE === actionCode);
  }

  getPaths() {
    let paths = {};
    this.MODPATH.map(item => { paths[item.PATHNAME] = item.RESOURCENAME });
    return paths;
  }

  getModCode() {
    return this.MODULECODE;
  }
}

class SelStore {
  constructor() {
    this.moudle = new Moudle2('RS_M00', Store.state['app'].modules['RS_M00']);
  }
  mixPaths() {
    return {
      ...this.moudle.getPaths()
    };
  }
  mixActions() {
    return {
      // 上级部门选择
      async updeptSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A04/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'UPDEPT',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 部门选择
      async deptSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A05/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'DEPT',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 员工选择
      async empSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A06/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'EMP',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      async empSel1({ commit }, {
        INPUT, ID, DEPTID, FUNCID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A13/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT,
              DEPTID,
              FUNCID
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'EMPUSER',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      async empSel2({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A14/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'EMPUSER',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 标准选择
      async tstddSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A07/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'TSTDD',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 客户选择
      async custSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A08/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'CUST',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 原始记录模版选择
      async ptmpSel({ commit }, {
        INPUT, ID, DEPTID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A09/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT,
              DEPTID: DEPTID || '-1'

            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'PTMP',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 原始记录模版选择
      async acceptSel({ commit }, {
        INPUT, ID, STATE
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A10/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT,
              STATE
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'ACCEPT',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 标准器选择
      async ardSel({ commit }, {
        INPUT, ID, TSTANDARDID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A11/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT,
              TSTANDARDID: TSTANDARDID || '-1'
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'ARD',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 规程制度选择
      async reguitemSel({ commit }, {
        INPUT, ID
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A12/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              ID: ID || '-1',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'REGUITEM',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
      // 行政区划选择
      async regSel({ commit }, {
        INPUT, PCODE
      }) {
        let ret = await db.postData({
          'api': '/api/data/call/RS_M00/A15/',
          'params': {
            PageSize: 1,
            PageIndex: 1,
            FilterParams: {
              PCODE: PCODE || '100000',
              INPUT
            }
          }
        });
        commit(Constants.M_INITDATA, {
          path: 'REG',
          data: (ret.Items || [])
        });
        return (ret.Items || []);
      },
    };
  }
}
export { SelStore, Constants };
