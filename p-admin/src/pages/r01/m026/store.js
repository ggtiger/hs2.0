import db from '@/api/db';
import createStore from '@/store/createStore';
import getBase from '../m02/baseStore';

// 常量定义（不依赖模块初始化）
// 注意：M_INITDATA 等值必须与 BaseStore.js 中的 Constants 保持一致
import { Constants as SConstants } from '@/store/BaseStore';

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
export const Constants = Object.assign({}, SConstants, {
  STORE_NAME: 'r01/m026',
});

let _storeResult = null;

// 延迟获取 store，仅在需要时初始化
// 调用前必须确保 LI_M02 模块已通过 initModule 初始化
function getStore() {
  if (!_storeResult) {
    let base = getBase();
    _storeResult = createStore.getStore({
      config: base.config,
      storeHelper: base.storeHelper,
      mutations: base.mutations,
      actions: {
        ...base.actions,
        // 覆盖 query，使用审批专用的 APICODE (A40)
        async query({ commit }, { isExport, columns, sumFields } = {}) {
          let row = _storeResult.storeHelper.moudle.getApi('', 'A40');
          let modeCode = _storeResult.storeHelper.moudle.getModCode();
          let { APIPARAM, APICODE, PATHNAME } = row;
          let QQRY = _storeResult.storeHelper.getTable(APIPARAM);
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
            api: `/api/rm11/call/${modeCode}/${APICODE}/`,
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
        // 覆盖 advQuery，使用审批专用的 APICODE (A42)
        async advQuery({ commit }, { isExport, columns, sumFields } = {}) {
          let row = _storeResult.storeHelper.moudle.getApi('', 'A42');
          let modeCode = _storeResult.storeHelper.moudle.getModCode();
          let { APIPARAM, APICODE, PATHNAME } = row;
          let QQRY = _storeResult.storeHelper.getTable(APIPARAM);
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
            api: `/api/rm11/call/${modeCode}/${APICODE}/`,
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
        async verify({ commit, dispatch }, { REMARK, ID, item }) {
          let ret = await dispatch('call', { APICODE: 'A14', params: { REMARK, ID } });
          if (ret && ret.length > 0) {
            ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].map(f => { item[f] = ret[0][f] });
          }
          return ret;
        },
        async reVerify({ commit, dispatch }, { REMARK, ID, item }) {
          let ret = await dispatch('call', { APICODE: 'A15', params: { REMARK, ID } });
          if (ret && ret.length > 0) {
            ['STATE', 'CHECKER', 'CHECKTIME', 'VERIFIER', 'VERIFYTIME'].map(f => { item[f] = ret[0][f] });
          }
          return ret;
        },
        async reject({ commit, dispatch }, { REMARK, ID, item }) {
          let ret = await dispatch('call', { APICODE: 'A16', params: { REMARK, ID } });
          if (ret && ret.length > 0) {
            ['STATE', 'VERIFIER', 'VERIFYTIME'].map(f => { item[f] = ret[0][f] });
          }
          return ret;
        },
        async batchVerify({ commit, dispatch }, { items, REMARK }) {
          await dispatch('batch', { APICODE: 'A25', items, params: { REMARK }, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
        },
        async batchReVerify({ commit, dispatch }, { items, REMARK }) {
          await dispatch('batch', { APICODE: 'A26', items, params: { REMARK }, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
        },
        async batchReject({ commit, dispatch }, { items, REMARK }) {
          await dispatch('batch', { APICODE: 'A29', items, params: { REMARK }, updateFields: ['STATE', 'VERIFIER', 'VERIFYTIME'] });
        },
        async print({ commit, dispatch }, { ID }) {
          await dispatch('call', { APICODE: 'A17', params: { ID } });
        },
        async download({ commit, dispatch }, { items }) {
          return await dispatch('batch', { APICODE: 'A20', items, updateFields: ['STATE'] });
        },
        async aprint({ commit, dispatch }, { items }) {
          return await dispatch('batch', { APICODE: 'A21', items });
        },
        // 委托单列表（LI_M02/A36 orecord 粒度；main.vue 在前端按 REFBILLID 分组）
        async loadWtList(ctx, { input, billDate }) {
          let filterParams = { INPUT: input || '' };
          if (billDate) filterParams.BILLDATE = billDate;
          return db.postData({
            api: '/api/data/call/LI_M02/A36/',
            params: {
              PageSize: 9999,
              PageIndex: 1,
              FilterParams: filterParams,
            },
          });
        },
        // 异常检测（LI_M02/A57；返回 { Code, Data: anomalies[] }）
        async detectAnomalies(ctx, { id }) {
          return db.postData({
            api: '/api/data/call/LI_M02/A57/',
            params: { FilterParams: { ID: id } },
          });
        },
      },
      storeName: 'r01/m026',
    });
  }
  return _storeResult;
}

// 导出包装函数，保持与原 mapState/mapGetters/mapDateTable 相同的调用方式
// 这些函数在组件 created 钩子调用 getStore() 后才能正常工作
const mapState = function() {
  return getStore().mapState.apply(this, arguments);
};

const mapGetters = function() {
  return getStore().mapGetters.apply(this, arguments);
};

const mapDateTable = function(path, aFields, itemProp) {
  return getStore().mapDateTable(path, aFields, itemProp);
};

export { getStore, mapState, mapGetters, mapDateTable };
