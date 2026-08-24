import Vue from 'vue';
import { BaseStore, Constants as SConstants } from './BaseStore';
import { DataTable } from 'rs-vcore/store/DataTable';
import db from '@/api/db';
import Store from './index';
const Constants = Object.assign({}, SConstants, {});

// 日期格式化辅助函数：将 Date 对象转为 yyyy-MM-dd 字符串，避免 MySQL str_to_date 解析失败
function formatDateValue(vvv) {
  if (vvv instanceof Date) {
    var y = vvv.getFullYear();
    var m = (vvv.getMonth() + 1);
    var d = vvv.getDate();
    return y + '-' + (m < 10 ? '0' + m : m) + '-' + (d < 10 ? '0' + d : d);
  }
  return vvv;
}

function formatFilterValue(vvv) {
  if (Object.prototype.toString.call(vvv) === '[object Object]') {
    var ret = {};
    Object.keys(vvv).forEach(function(k) {
      ret[k] = formatDateValue(vvv[k]);
    });
    return ret;
  }
  if (Array.isArray(vvv)) {
    return vvv.map(formatDateValue).join();
  }
  return formatDateValue(vvv);
}

class Moudle {
  constructor(MODULECODE, data) {
    this.MODULECODE = MODULECODE;
    this.MODPATH = data.MODPATH;
    this.MODPATHREF = data.MODPATHREF;
    this.MODAPI = data.MODAPI;
    this.MOD = data.MOD;
    this.MODPAGE = data.MODPAGE || [];
    this.MODBUTTON = data.MODBUTTON || [];
  }
  getApi(actionCode, APICODE) {
    actionCode = actionCode || APICODE;
    return this.MODAPI.find(item => item.ACTIONCODE === actionCode) || this.MODAPI.find(item => item.APICODE === actionCode);
  }

  getPaths() {
    let paths = {};
    this.MODPATH.map(item => { paths[item.PATHNAME] = item.RESOURCENAME });
    return paths;
  }

  getModCode() {
    return this.MODULECODE;
  }

  // 获取模块流程编码(FLOWCODE): MOD 是数组，取第一行
  getFlowCode() {
    if (Array.isArray(this.MOD) && this.MOD.length > 0) return this.MOD[0].FLOWCODE || '';
    if (this.MOD && this.MOD.FLOWCODE) return this.MOD.FLOWCODE;
    return '';
  }

  // 获取模块的所有页面配置
  getPages() {
    return this.MODPAGE.filter(p => (p.ISDELETED || 0) === 0);
  }

  // 获取指定页面编码的页面配置
  getPage(pageCode) {
    return this.MODPAGE.find(p => p.PAGECODE === pageCode && (p.ISDELETED || 0) === 0);
  }

  // 获取指定页面的按钮配置
  getButtons(pageId) {
    return this.MODBUTTON.filter(b => b.PAGEID === pageId && (b.ISDELETED || 0) === 0)
      .sort((a, b) => (a.SORTNO || 0) - (b.SORTNO || 0));
  }

  // 获取指定页面编码的按钮配置
  getButtonsByPageCode(pageCode) {
    var page = this.getPage(pageCode);
    if (!page) return [];
    return this.getButtons(page.ID);
  }

  // 获取指定类型的页面列表
  getPagesByType(pageType) {
    return this.MODPAGE.filter(p => p.PAGETYPE === pageType && (p.ISDELETED || 0) === 0)
      .sort((a, b) => (a.SORTNO || 0) - (b.SORTNO || 0));
  }

  // 获取指定页面的子页面
  getSubPages(parentId) {
    return this.MODPAGE.filter(p => p.PARENTID === parentId && (p.ISDELETED || 0) === 0)
      .sort((a, b) => (a.SORTNO || 0) - (b.SORTNO || 0));
  }

  // 获取指定路径的所有子表路径（递归）
  getSubPaths(pathName) {
    if (!this.MODPATHREF || this.MODPATHREF.length === 0) return [];
    const result = [];
    const visited = new Set();
    const findSubs = (parentPath) => {
      this.MODPATHREF.forEach(ref => {
        if (ref.PATHNAMEA === parentPath && !visited.has(ref.PATHNAMEB)) {
          visited.add(ref.PATHNAMEB);
          result.push(ref.PATHNAMEB);
          // 递归查找子表的子表
          findSubs(ref.PATHNAMEB);
        }
      });
    };
    findSubs(pathName);
    return result;
  }
}

class Store03 extends BaseStore {
  constructor(config) {
    super(config);
    this.moduleCode = config.moduleCode;
    this._moudle = null;
    this._modulePromise = null;
    this.myGetParams = config.myGetParams;
    this.apiPath = config.apiPath || '/api/data/call';
    this.paths = config.paths || {};
    // 构造时尝试初始化 (若 app store 已有配置则立即完成, 否则触发异步加载)
    this.ensureModule();
  }

  /**
   * 确保模块配置已加载
   * - app store 有配置: 立即创建 Moudle, 同步完成
   * - app store 无配置: 异步 dispatch initModule 从数据库加载, 加载完后创建 Moudle
   * 所有依赖 this.moudle 的 action 调用前应 await 此方法
   */
  ensureModule() {
    if (this._moudle) return Promise.resolve(this._moudle);
    if (this._modulePromise) return this._modulePromise;
    var modData = Store.state['app'].modules[this.moduleCode];
    if (modData) {
      this._initMoudle(modData);
      return Promise.resolve(this._moudle);
    }
    // 异步从数据库加载模块配置
    var self = this;
    this._modulePromise = Store.dispatch('app/initModule', this.moduleCode).then(function() {
      var data = Store.state['app'].modules[self.moduleCode];
      if (!data) {
        throw new Error('模块 ' + self.moduleCode + ' 数据库中未找到配置 (RS_M00/A03 未返回数据)');
      }
      self._initMoudle(data);
      self._modulePromise = null;
      return self._moudle;
    }).catch(function(err) {
      self._modulePromise = null;
      throw err;
    });
    return this._modulePromise;
  }

  _initMoudle(data) {
    this._moudle = new Moudle(this.moduleCode, data);
    this.paths = { ...this._moudle.getPaths(), ...this.paths };
    // 补创建 mixState 遗漏的 DataTable
    // 场景: Store03 构造时 ensureModule 异步加载模块数据,
    // mixState 已在构造期间同步执行 (dt 为空), paths 加载后需补建
    this._ensureDataTables();
  }

  /**
   * 确保 dt 中所有 path 对应的 DataTable 已创建
   * mixState 在构造期间同步执行, 若此时 paths 为空(模块数据异步加载中),
   * dt 会是空对象 {}; paths 加载后调用此方法补建缺失的 DataTable
   * 使用 Vue.set 确保新增属性是响应式的 (dt 在 Vuex state 中)
   */
  _ensureDataTables() {
    if (!this.paths || !this.dt) return;
    var self = this;
    Object.keys(this.paths).forEach(function(path) {
      if (!self.dt[path]) {
        var val = self.paths[path];
        var dt = (typeof val === 'object') ? val : new DataTable(path, val);
        Vue.set(self.dt, path, dt);
      }
    });
  }

  get moudle() {
    if (!this._moudle) {
      // 同步访问兜底: 若 app store 已有数据则即时初始化, 否则抛清晰错误
      var modData = Store.state['app'].modules[this.moduleCode];
      if (modData) {
        this._initMoudle(modData);
      } else {
        throw new Error('模块 ' + this.moduleCode + ' 配置未加载, 请先 await store.ensureModule()');
      }
    }
    return this._moudle;
  }

  getApiRow(actionCode, APICODE) {
    return this.moudle.getApi(actionCode, APICODE);
  }

  getParams(actionCode, { commit, DID } = {}) {
    let row = this.moudle.getApi('query');
    let { APIPARAM, PATHNAME } = row;
    let params = {};
    switch (actionCode) {
      case 'add':
        let paths = APIPARAM.split(',');
        paths.forEach(path => {
          params[path] = this.getTable(path).add({});
        });
        break;
      case 'save':
        paths.forEach(path => {
          if (path !== PATHNAME) {
            commit('SET_ENTRYNUM', { path });
          }
          params[path] = this.getTable(path).getXML();
        });
        break;
      case 'delete':
        paths.forEach(path => {
          this.getTable(path).clear();
          params[path] = this.getTable(path).getXML();
        });
        break;
      case 'query':
        let QQRY = this.getTable(APIPARAM);
        params = { FilterParams: {} };
        QQRY.getFields().forEach(f => {
          if (['PageSize', 'PageIndex', 'TotalCount'].indexOf(f) !== -1) {
            params[f] = QQRY.getValue(f);
          } else {
            params.FilterParams[f] = formatFilterValue(QQRY.getValue(f));
          }
        });
        break;
      case 'open':
        params = { FilterParams: {} };
        params.FilterParams[APIPARAM || 'ID'] = DID;
        break;
      default:
        break;

    }
    if (this.myGetParams) {
      params = { ...params, ...this.myGetParams(actionCode) };
    }
    return params;
  }

  mixState() {
    return super.mixState();
  }
  mixActions() {
    let _this = this;
    return {
      // 新增
      async add({ commit }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('save');
        let { APIPARAM } = row;
        let paths = APIPARAM.split(',');
        paths.forEach(path => {
          commit('ADD', { path });
        });
      },
      // 保存
      async save({ commit }, payload) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('save');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE, PATHNAME } = row;
        let paths = APIPARAM.split(',');
        let params = {};
        paths.forEach(path => {
          if (path !== PATHNAME) {
            commit('SET_ENTRYNUM', { path });
          }
          params[path] = _this.getTable(path).getXML();
        });
        // 保留参数: CHANGENOTE=版本变更说明, SKIPVERSION=1 快速保存不留版本(后端不作为数据路径处理)
        if (payload && payload.CHANGENOTE != null) {
          params.CHANGENOTE = payload.CHANGENOTE;
        }
        if (payload && payload.SKIPVERSION != null) {
          params.SKIPVERSION = payload.SKIPVERSION;
        }
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        commit(Constants.M_BATCHSETDATA, {
          data: ret,
        });
      },
      // 删除
      async delete() {
        await _this.ensureModule();
        let row = _this.moudle.getApi('delete');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE } = row;
        let paths = APIPARAM.split(',');
        let params = {};
        paths.forEach(path => {
          _this.getTable(path).clear();
          params[path] = _this.getTable(path).getXML();
        });
        await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
      },
      // 获取单据号
      async getBillCode({ commit }, {TCODE}) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('getbillcode');
        let modeCode = _this.moudle.getModCode();
        let { APICODE } = row;
        let params = {TCODE};
        await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
      },
      // 查询
      async query({ commit }, { isExport, columns, sumFields, qryPath } = {}) {
        await _this.ensureModule();
        // 获取api行
        console.log('query');
        let row = _this.getApiRow('query');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE, PATHNAME } = row;
        // 支持自定义 qryPath（如 select 页面配了 SEL 数据源）
        if (qryPath) PATHNAME = qryPath;
        let QQRY = _this.getTable(APIPARAM);
        if (!QQRY) {
          QQRY = _this.getTable('QQRY');
        }
        commit('INIT', { paths: [PATHNAME] });
        let params = { FilterParams: {}, isExport, columns, sumFields };
        QQRY.getFields().forEach(f => {
          if (['PageSize', 'PageIndex', 'TotalCount', 'SumInfo'].indexOf(f) !== -1) {
            params[f] = QQRY.getValue(f);
          } else {
            let vvv = QQRY.getValue(f);
            if (Object.prototype.toString.call(vvv) === '[object Object]') {
              Object.keys(vvv).map(k => {
                params.FilterParams[f + '_' + k] = formatDateValue(vvv[k]);
              });
            } else {
              if (Array.isArray(vvv)) {
                params.FilterParams[f] = vvv.map(formatDateValue).join();
              } else {
                params.FilterParams[f] = formatDateValue(vvv);
              }

            }
          }
        });
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        if (isExport) {
          return ret;
        }
        QQRY.setValue('TotalCount', ret.TotalCount);
        QQRY.setValue('SumInfo', ret.SumInfo);
        commit(Constants.M_INITDATA, {
          path: PATHNAME,
          data: ret.Items || [],
        });
      },
      // 高级查询
      async advQuery({ commit }, { isExport, columns, sumFields, qryPath, APICODE: overrideAPICODE } = {}) {
        await _this.ensureModule();
        // 获取api行：优先用传入的 APICODE，否则按 ACTIONCODE='advQuery' 查找
        let row = overrideAPICODE ? _this.getApiRow(null, overrideAPICODE) : _this.getApiRow('advQuery');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE, PATHNAME } = row || {};
        // 支持自定义 qryPath（如 select 页面配了 SEL 数据源）
        if (qryPath) PATHNAME = qryPath;
        let QQRY = _this.getTable(APIPARAM);
        let params = { FilterParams: {}, isExport, columns, sumFields };
        QQRY.getFields().forEach(f => {
          if (['PageSize', 'PageIndex', 'TotalCount', 'SumInfo'].indexOf(f) !== -1) {
            params[f] = QQRY.getValue(f);
          } else {
            let vvv = QQRY.getValue(f);
            if (Object.prototype.toString.call(vvv) === '[object Object]') {
              Object.keys(vvv).map(k => {
                params.FilterParams[f + '_' + k] = formatDateValue(vvv[k]);
              });
            } else {
              if (Array.isArray(vvv)) {
                params.FilterParams[f] = vvv.map(formatDateValue).join();
              } else {
                params.FilterParams[f] = formatDateValue(vvv);
              }
            }
          }
        });
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        if (isExport) {
          return ret;
        }
        QQRY.setValue('TotalCount', ret.TotalCount);
        QQRY.setValue('SumInfo', ret.SumInfo);
        commit(Constants.M_INITDATA, {
          path: PATHNAME,
          data: ret.Items || [],
        });
      },
      // 打开一行
      async open({ commit }, { ID, extraFilterParams }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('open');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE } = row;
        let params = { FilterParams: {} };
        params.FilterParams[APIPARAM || 'ID'] = ID;
        // 合并额外参数
        if (extraFilterParams) {
          Object.keys(extraFilterParams).forEach(function(k) {
            params.FilterParams[k] = extraFilterParams[k];
          });
        }
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        commit(Constants.M_BATCHSETDATA, {
          data: ret,
        });
      },
      async flowSave({ commit }, { ID, ACTIONCODE }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi(ACTIONCODE);
        let modeCode = _this.moudle.getModCode();
        let { APICODE, PATHNAME } = row;
        let params = { ID };
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        commit(Constants.M_INITDATA, {
          path: PATHNAME,
          data: ret || [],
        });
      },
      async submit({ commit, dispatch }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('submit');
        let modeCode = _this.moudle.getModCode();
        let { APIPARAM, APICODE, PATHNAME } = row;
        let paths = APIPARAM.split(',');
        let params = {};
        paths.forEach(path => {
          if (path !== PATHNAME) {
            commit('SET_ENTRYNUM', { path });
          }
          params[path] = _this.getTable(path).getXML();
        });
        let ret = await db.postData({
          api: `${_this.apiPath}/${modeCode}/${APICODE}/`,
          params
        });
        commit(Constants.M_BATCHSETDATA, {
          data: ret,
        });
        // await dispatch('flowSave', { ID, ACTIONCODE: 'submit' });
      },
      async reSubmit({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'reSubmit' });
      },
      async check({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'check' });
      },
      async reCheck({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'reCheck' });
      },
      async verify({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'verify' });
      },
      async reVerify({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'reVerify' });
      },
      async invalid({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'invalid' });
      },
      async reInvalid({ commit, dispatch }, { ID }) {
        await dispatch('flowSave', { ID, ACTIONCODE: 'reInvalid' });
      },
      // 打开一行
      async call({ commit }, { APICODE, moduleCode, params }) {
        await _this.ensureModule();
        // let row = _this.moudle.getApi(APICODE);
        moduleCode = moduleCode || _this.moudle.getModCode();
        let ret = await db.postData({
          api: `${_this.apiPath}/${moduleCode}/${APICODE}/`,
          params
        });
        return ret;
      },
      async batch({
        commit, dispatch
      }, { APICODE, items, updateFields, params }) {
        let ID = [];
        items.map(item => {
          ID.push(item.ID);
        });
        let ret = await dispatch('call', {
          APICODE,
          params: {
            FilterParams: { ID },
            ID: ID.join(','),
            ...params
          }
        });
        let dd = ret.Items || ret;
        if (dd.length > 0 && dd.map) {
          dd.map(r => {
            let rr = items.find(item => item.ID === r.ID);
            if (rr && updateFields) {
              updateFields.map(f => {
                rr[f] = r[f];
              });
            }
          });
        }
        return ret;
      },
      async batchSubmit({
        commit, dispatch
      }, { items, REMARK, CHECKID, CHECKER }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchSubmit');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK, NEXTAPRID: CHECKID, NEXTAPRER: CHECKER }, updateFields: ['STATE', 'SUBMITER', 'SUMBMITTIME', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchReSubmit({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchReSubmit');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, updateFields: ['STATE', 'SUBMITER', 'SUMBMITTIME', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchCheck({
        commit, dispatch
      }, { items, REMARK, VERIFYID, VERIFYER }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchCheck');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchComCheck({
        commit, dispatch
      }, { items, REMARK, VERIFYID, VERIFYER }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('check');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK, NEXTAPRID: VERIFYID, NEXTAPRER: VERIFYER }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchComReCheck({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('reCheck');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchCheckReject({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchCheckReject');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchReCheck({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchReCheck');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchVerify({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchVerify');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchVerifyReject({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchVerifyReject');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchComVerify({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('verify');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchComReVerify({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('reVerify');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchReVerify({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchReVerify');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'] });
      },
      async batchComplete({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchComplete');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, params: { REMARK }, updateFields: ['STATE', 'COMPLETER', 'COMPLETETIME'] });
      },
      async batchReComplete({
        commit, dispatch
      }, { items, REMARK }) {
        await _this.ensureModule();
        let row = _this.moudle.getApi('batchReComplete');
        let { APICODE } = row;
        await dispatch('batch', { APICODE, items, updateFields: ['STATE', 'COMPLETER', 'COMPLETETIME'] });
      },
      ...super.mixActions()
    };
  }
  mixMutations() {
    let _this = this;
    return {
      ...super.mixMutations(),
      INIT(state, { paths }) {
        paths.forEach(path => {
          _this.getTable(path).initData([]);
        });
      },
      ADD(state, { path, item }) {
        _this.getTable(path).add(item || {});
      },
      DEL(state, { path, item }) {
        _this.getTable(path).del(item);
      },
      // 强制刷新子表视图：替换数组引用+每行为新对象，确保heyui Table重新读取属性渲染
      REFRESH_SUBTABLE(state, { path }) {
        let dt = _this.getTable(path);
        console.log('[Store03] REFRESH_SUBTABLE path=', path, 'dt=', !!dt, 'dataLen=', dt && dt.data ? dt.data.length : 0);
        if (dt && dt.data && dt.data.length > 0) {
          // splice替换所有元素为新对象，触发数组dep（heyui Table响应数组内部方法变化，和push同机制）
          const newData = dt.data.map(r => Object.assign({}, r));
          dt.data.splice(0, dt.data.length, ...newData);
        }
      },
      SET_DTSAKEY(state, { path, KEYS }) {
        let DTSA = _this.getTable(path);
        DTSA.data.map(d => {
          if (!d.ID) {
            DTSA.setValue('ID', KEYS.shift(), d);
          }
        });
      },
      SET_ENTRYNUM(state, { path }) {
        let DTS = _this.getTable(path);
        DTS.data.map((d, index) => {
          DTS.setValue('ENTRYNUM', index + 1, d);
        });
      },
      [Constants.M_BATCHSETDATA]: function(state, { data }) {
        Object.keys(data).forEach(key => {
          if (_this.getTable(key)) {
            _this.getTable(key).setData(data[key]);
          } else {
            state[key] = data[key];
          }
        });
      }
    };
  }
}
export { Store03, Constants };
