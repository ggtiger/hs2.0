import db from '@/api/db';
import createStore from '@/store/createStore';
import Store from '@/store';
import { queryApis } from '@/utils/selRegistry';
import { unregisterGenericStore } from '@/components/generic-module/generic-store';

const STORE_NAME = 's01/m18';
const MODULE_CODE = 'RS_M18';

let _storeResult = null;

function getStoreResult() {
  if (_storeResult) return _storeResult;

  if (Store.state[STORE_NAME]) {
    Store.unregisterModule(STORE_NAME.split('/'));
  }

  _storeResult = createStore.getStore({
    config: {
      moduleCode: MODULE_CODE,
      // 直接指定 paths，不依赖 ensureModule 从 ORM 元数据加载
      // FUNC/FUNCPOINT 仅用于 publish action，复用 RS_M03 标准 save 接口
      paths: {
        'MAIN': 'VSS_MOUDLE',
        'MODPAGE': 'VCK_MODULE_PAGE',
        'MODBUTTON': 'VCK_MODULE_BUTTON',
        'FUNC': 'VSS_FUNC',
        'FUNCPOINT': 'VSS_FUNCPOINT'
      }
    },
    storeName: STORE_NAME,
    state: {
      configModuleCode: '',
      moduleApis: [],
      // 发布目标菜单列表（RS_M03/A01，目录+模块，供"发布到目录"树选择）
      publishMenuList: [],
      // 模块向导用原始数据（loadMenus/loadResources/searchTemplates 写入）
      wizardMenus: [],
      wizardResources: [],
      wizardTemplates: []
    },
    mutations: {
      SET_MODULECODE(state, { moduleCode }) {
        state.configModuleCode = moduleCode;
      },
      SET_MODULE_APIS(state, { apis }) {
        state.moduleApis = apis || [];
      },
      SET_PUBLISH_MENU_LIST(state, rows) {
        state.publishMenuList = Array.isArray(rows) ? rows : [];
      },
      SET_WIZARD_MENUS(state, rows) { state.wizardMenus = Array.isArray(rows) ? rows : [] },
      SET_WIZARD_RESOURCES(state, rows) { state.wizardResources = Array.isArray(rows) ? rows : [] },
      SET_WIZARD_TEMPLATES(state, rows) { state.wizardTemplates = Array.isArray(rows) ? rows : [] },
    },
    getters: {
      // 向导菜单下拉（仅含 UPFUNCID 非空的项 = 子菜单）
      wizardMenuOptions: function(s) {
        return s.wizardMenus
          .filter(function(m) { return m.UPFUNCID })
          .map(function(m) {
            return { key: m.ID, title: m.FUNCNAME + ' (' + m.FUNCCODE + ')' };
          });
      },
      // 向导资源下拉
      wizardResourceOptions: function(s) {
        return s.wizardResources.map(function(r) {
          return { key: r.ID, title: r.RESOURCENAME + ' (' + r.TABLENAME + ')' };
        });
      },
      // 向导模板下拉
      wizardTemplateOptions: function(s) {
        return s.wizardTemplates.map(function(t) {
          return {
            key: t.TEMPLATECODE,
            title: t.TEMPLATENAME + ' (' + (t.CATEGORY || '') + ')',
            desc: t.DESCRIPTION,
          };
        });
      },
    },
    actions: {
      async openConfig({ commit }, { MODULECODE }) {
        commit('SET_MODULECODE', { moduleCode: MODULECODE });
        commit('SET_MODULE_APIS', { apis: [] });
        commit('INIT', { paths: ['MAIN', 'MODPAGE', 'MODBUTTON'] });

        // 先加载 app 模块配置(含 MODPATHREF 子表关系), 保证 subPaths/btnAreas 首次渲染有值
        await Store.dispatch('app/initModule', MODULECODE);
        // 使通用模块 store 缓存失效, 确保预览用最新 MODPATH 重建 DataTable/scm
        unregisterGenericStore(MODULECODE);

        var apis = await queryApis(MODULECODE);
        commit('SET_MODULE_APIS', { apis: apis || [] });

        var ret = await db.postData({
          api: '/api/data/call/RS_M18/A02/',
          params: { FilterParams: { MODULECODE: MODULECODE } }
        });

        commit('batchSetData', { data: ret });
      },
      async saveConfig({ commit, state, dispatch }) {
        var sh = getStoreResult().storeHelper;
        var paths = ['MAIN', 'MODPAGE', 'MODBUTTON'];
        var params = {};
        paths.forEach(function(path) {
          params[path] = sh.getTable(path).getXML();
        });

        await db.postData({
          api: '/api/data/call/RS_M18/A04/',
          params: params
        });

        // 保存后重新 open 加载数据（A04 返回含 ISDELETED 行，A02 会过滤）
        var moduleCode = state.configModuleCode;
        if (moduleCode) {
          await dispatch('openConfig', { MODULECODE: moduleCode });
        } else {
          commit('batchSetData', { data: {} });
        }
      },
      // 发布模块到菜单: new=新增菜单到指定目录, replace=替换已有菜单的 URL/编码
      // 复用 RS_M03 标准 save 接口 (MAIN=VSS_FUNC, DTSA=VSS_FUNCPOINT)
      // buttons: [{APICODE, BTNNAME}] → 生成功能点 FUNCPOINTCODE/FUNCPOINTNAME
      async publish({ commit, state }, payload) {
        var sh = getStoreResult().storeHelper;
        var mode = payload.mode;
        var targetFuncId = payload.targetFuncId;
        var upFuncId = payload.upFuncId;
        var funcName = payload.funcName;
        var moduleCode = payload.moduleCode;
        var outerUrl = '/g/' + moduleCode + '/main';
        var buttons = payload.buttons || [];

        // 清空 FUNC/FUNCPOINT DataTable, 避免上次操作残留
        commit('initByPath', { paths: ['FUNC', 'FUNCPOINT'] });
        var FUNC = sh.getTable('FUNC');
        var FUNCPOINT = sh.getTable('FUNCPOINT');

        if (mode === 'replace') {
          // 通过 RS_M03 标准 open 接口加载已有菜单 + 功能点
          var ret = await db.postData({
            api: '/api/data/call/RS_M03/A02/',
            params: { FilterParams: { ID: targetFuncId } }
          });
          FUNC.initData(ret.MAIN || []);
          FUNCPOINT.initData(ret.DTSA || []);

          // 替换 FUNCCODE/FUNCNAME/OUTERURL
          var mainRow = FUNC.data[0];
          if (mainRow) {
            FUNC.setValue('FUNCCODE', moduleCode, mainRow);
            FUNC.setValue('FUNCNAME', funcName, mainRow);
            FUNC.setValue('OUTERURL', outerUrl, mainRow);
          }
          // 清空旧功能点（逐个 del 标记删除, 后端 ORM 据此生成 DELETE SQL）
          FUNCPOINT.data.slice().forEach(function(row) {
            FUNCPOINT.del(row);
          });
        } else {
          // 新增模式: 在指定上级目录下创建菜单
          FUNC.add({
            FUNCTYPE: 2,
            FUNCCODE: moduleCode,
            FUNCNAME: funcName,
            OUTERURL: outerUrl,
            UPFUNCID: upFuncId || '',
            ISOUTERURL: 0,
            ISHIDE: 0,
            ISUSE: 1,
            LEVEL: 2
          });
        }

        // 按钮转功能点
        buttons.forEach(function(btn) {
          if (!btn.APICODE) return;
          FUNCPOINT.add({
            FUNCPOINTCODE: btn.APICODE,
            FUNCPOINTNAME: btn.BTNNAME || btn.APICODE
          });
        });

        // 调用 RS_M03 标准保存接口（后端按 APIPARAM=MAIN,DTSA 处理）
        var saveRet = await db.postData({
          api: '/api/data/call/RS_M03/A04/',
          params: {
            MAIN: FUNC.getXML(),
            DTSA: FUNCPOINT.getXML()
          }
        });

        // 发布后刷新菜单, 让新菜单立即在侧边栏可见
        await Store.dispatch('app/initMenu', Store.state.user.userInfo.ID);

        return saveRet;
      },
      // 加载发布目标菜单列表（RS_M03/A01，全部菜单含目录与模块）
      // 给 HeyUI Tree 的 getTotalDatas 回调用，结果存 state.publishMenuList
      async loadPublishMenus({ commit }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M03/A01/',
          params: { PageSize: 999, PageIndex: 1, FilterParams: { INPUT: '' } },
        });
        commit('SET_PUBLISH_MENU_LIST', (ret && ret.Items) || []);
        return (ret && ret.Items) || [];
      },
      // 导出模块为模板（RS_M25/A05，RPC；调用方拿 message 弹提示）
      exportTemplate(ctx, payload) {
        return db.postData({
          api: '/api/RModuleTpl/call/RS_M25/A05/',
          params: payload,
        });
      },

      // ====== 模块向导（module-wizard.vue）======
      // 加载菜单列表（RS_M03/A01，含目录与模块）
      async loadWizardMenus({ commit }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M03/A01/',
          params: { FilterParams: { INPUT: '' }, PageSize: 999 },
        });
        var rows = (ret && ret.Items) || [];
        commit('SET_WIZARD_MENUS', rows);
      },
      // 加载资源列表（RS_M01/A01）
      async loadWizardResources({ commit }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M01/A01/',
          params: { FilterParams: { INPUT: '' }, PageSize: 999 },
        });
        var rows = (ret && ret.Items) || [];
        commit('SET_WIZARD_RESOURCES', rows);
      },
      // 搜索模块模板（RS_M25/A01）
      async searchWizardTemplates({ commit }, { keyword }) {
        var ret = await db.postData({
          api: '/api/data/call/RS_M25/A01/',
          params: { FilterParams: { INPUT: keyword || '' }, PageSize: 20 },
        });
        var rows = (ret && ret.Items) || [];
        commit('SET_WIZARD_TEMPLATES', rows);
        return rows;
      },
      // 向导"手动落库"兜底：创建模块（MAIN=VSS_MOUDLE DataTable 生成 XML，严禁手拼）
      async createModuleBare({ commit }, { moduleCode, moduleName }) {
        var sh = getStoreResult().storeHelper;
        commit('INIT', { paths: ['MAIN'] });
        var MAIN = sh.getTable('MAIN');
        MAIN.add({ MODULECODE: moduleCode, MODULENAME: moduleName });
        return db.postData({
          api: '/api/data/call/RS_M02/A04/',
          params: { VSS_MOUDLE: MAIN.getXML() },
        });
      },
      // 向导"手动落库"兜底：保存页面+按钮（MODPAGE/MODBUTTON DataTable 生成 XML）
      // pages/buttons 为字段对象数组（ID/MODULECODE/PAGECODE/...），由 DataTable.add 承载
      async saveModulePagesBare({ commit }, { pages, buttons }) {
        var sh = getStoreResult().storeHelper;
        commit('INIT', { paths: ['MODPAGE', 'MODBUTTON'] });
        var MODPAGE = sh.getTable('MODPAGE');
        var MODBUTTON = sh.getTable('MODBUTTON');
        (pages || []).forEach(function(p) { MODPAGE.add(p) });
        (buttons || []).forEach(function(b) { MODBUTTON.add(b) });
        return db.postData({
          api: '/api/data/call/RS_M18/A04/',
          params: { VCK_MODULE_PAGE: MODPAGE.getXML(), VCK_MODULE_BUTTON: MODBUTTON.getXML() },
        });
      },
      // 向导"手动落库"兜底：创建菜单（FUNC=VSS_FUNC DataTable 生成 XML）
      async createMenuBare({ commit }, { funcCode, funcName, parentFuncId, moduleCode }) {
        var sh = getStoreResult().storeHelper;
        commit('INIT', { paths: ['FUNC'] });
        var FUNC = sh.getTable('FUNC');
        FUNC.add({
          FUNCCODE: funcCode,
          FUNCNAME: funcName,
          UPFUNCID: parentFuncId,
          OUTERURL: moduleCode,
          ISDELETED: '0',
        });
        return db.postData({
          api: '/api/data/call/S01_M03/A04/',
          params: { VSS_FUNC: FUNC.getXML() },
        });
      }
    }
  });
  return _storeResult;
}

var mapState = function() { return getStoreResult().mapState.apply(this, arguments) };
var mapGetters = function() { return getStoreResult().mapGetters.apply(this, arguments) };
var mapDateTable = function() { return getStoreResult().mapDateTable.apply(this, arguments) };
var Constants = { STORE_NAME: STORE_NAME, MODULE_CODE: MODULE_CODE };

export { mapState, mapGetters, mapDateTable, Constants };
