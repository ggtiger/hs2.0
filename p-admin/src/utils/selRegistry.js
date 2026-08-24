// 通用选择器注册表
// 配合 rs-form-cell 的 autocomplete / treepicker EDITTYPE 使用。
// 支持两种配置方式：
//   1. 预设（selType）：从 SEL_TYPES 快速选常用选择器
//   2. 自定义（module + apiCode）：任意模块的任意查询接口
import db from '@/api/db';

// 预设选择器（快捷选项，底层也是 module + apiCode）
export const SEL_TYPES = [
  { key: 'dept', title: '部门', module: 'RS_M00', apiCode: 'A05', keyName: 'ID', titleName: 'DEPTNAME' },
  { key: 'updept', title: '部门(含上级)', module: 'RS_M00', apiCode: 'A04', keyName: 'ID', titleName: 'DEPTNAME' },
  { key: 'emp', title: '员工', module: 'RS_M00', apiCode: 'A06', keyName: 'ID', titleName: 'EMPNAME' },
  { key: 'emp-user', title: '员工(按部门/功能点)', module: 'RS_M00', apiCode: 'A13', keyName: 'ID', titleName: 'EMPNAME' },
  { key: 'tstdd', title: '测量标准', module: 'RS_M00', apiCode: 'A07', keyName: 'ID', titleName: 'STDDNAME' },
  { key: 'cust', title: '客户', module: 'RS_M00', apiCode: 'A08', keyName: 'ID', titleName: 'CUSTNAME' },
  { key: 'ptmp', title: '原始记录模板', module: 'RS_M00', apiCode: 'A09', keyName: 'ID', titleName: 'PTMPNAME' },
  { key: 'accept', title: '委托单', module: 'RS_M00', apiCode: 'A10', keyName: 'ID', titleName: 'ACCEPTNAME' },
  { key: 'ard', title: '标准器', module: 'RS_M00', apiCode: 'A11', keyName: 'ID', titleName: 'ARDNAME' },
  { key: 'reguitem', title: '规程制度', module: 'RS_M00', apiCode: 'A12', keyName: 'ID', titleName: 'REGUITEMNAME' },
  { key: 'reg', title: '行政区划', module: 'RS_M00', apiCode: 'A15', keyName: 'REGION_CODE', titleName: 'REGION_NAME' },
];

// 树形选择器（用于 TreePicker，需要 parentName 字段）
export const TREE_SEL_TYPES = [
  { key: 'dept-tree', title: '部门树', module: 'RS_M00', apiCode: 'A04', keyName: 'ID', titleName: 'DEPTNAME', parentName: 'UPDEPTID' },
];

// 所有类型（含树形）
export const ALL_SEL_TYPES = [...SEL_TYPES, ...TREE_SEL_TYPES];

// 查找预设
export function getSelType(selType, includeTree = false) {
  const arr = includeTree ? ALL_SEL_TYPES : SEL_TYPES;
  return arr.find(t => t.key === selType);
}

// 通用调用：/api/data/call/{module}/{apiCode}/
async function callModuleApi(module, apiCode, filterParams) {
  const ret = await db.postData({
    api: `/api/data/call/${module}/${apiCode}/`,
    params: {
      PageSize: 1,
      PageIndex: 1,
      FilterParams: filterParams || {},
    },
  });
  return ret.Items || [];
}

// 标准化配置：把各种格式统一成 { module, apiCode, keyName, titleName, parentName }
// 支持格式：
//   字符串："dept"（预设名）
//   JSON {selType:"dept"}：预设
//   JSON {module:"RS_M00",apiCode:"A05",keyName:"ID",titleName:"DEPTNAME"}：自定义
export function normalizeSelConfig(raw, includeTree = false) {
  if (!raw) return null;
  let cfg = raw;
  if (typeof raw === 'string') {
    try {
      cfg = JSON.parse(raw);
    } catch (e) {
      cfg = { selType: raw };
    }
  }
  if (!cfg || typeof cfg !== 'object') return null;
  // 自定义格式
  if (cfg.module && cfg.apiCode) {
    return {
      module: cfg.module,
      apiCode: cfg.apiCode,
      keyName: cfg.keyName || 'ID',
      titleName: cfg.titleName || '',
      parentName: cfg.parentName || '',
      paramMappings: cfg.paramMappings || '',
      defaultParams: cfg.defaultParams || null,
    };
  }
  // 预设格式
  if (cfg.selType) {
    const preset = getSelType(cfg.selType, true);
    if (preset) {
      return {
        module: preset.module,
        apiCode: preset.apiCode,
        keyName: cfg.keyName || preset.keyName,
        titleName: cfg.titleName || preset.titleName,
        parentName: cfg.parentName || preset.parentName || '',
        paramMappings: cfg.paramMappings || '',
        defaultParams: cfg.defaultParams || null,
      };
    }
  }
  return null;
}

// 生成 AutoComplete 的 option 配置
// rawConfig 可以是字符串、JSON 字符串或对象
// loadData(INPUT, callback, extraParams)：extraParams 由 rs-form-edit 注入（表单字段值）
export function buildAutoCompleteOption(rawConfig) {
  const cfg = normalizeSelConfig(rawConfig, true);
  if (!cfg) {
    return { keyName: 'ID', titleName: '', paramMappings: '', loadData() {} };
  }
  const { module, apiCode, keyName, titleName, paramMappings, defaultParams } = cfg;
  // 默认参数：静态固定过滤条件（如 {TYPE:'1'}），优先级低于动态 extraParams
  const baseParams = defaultParams && typeof defaultParams === 'object' ? defaultParams : {};
  return {
    keyName,
    titleName,
    paramMappings: paramMappings || '',
    loadData(INPUT, callback, extraParams) {
      const keyword = INPUT === titleName ? '' : INPUT;
      const filterParams = Object.assign({}, baseParams, { INPUT: keyword, ID: '-1' }, extraParams || {});
      callModuleApi(module, apiCode, filterParams).then(items => callback(items));
    },
  };
}

// 生成 TreePicker 的 option 配置
export function buildTreePickerOption(rawConfig) {
  const cfg = normalizeSelConfig(rawConfig, true);
  if (!cfg) {
    return { keyName: 'ID', titleName: '', parentName: 'PARENTID', dataMode: 'list', getTotalDatas(cb) { cb([]); } };
  }
  const { module, apiCode, keyName, titleName, parentName } = cfg;
  return {
    keyName,
    titleName,
    parentName: parentName || 'PARENTID',
    dataMode: 'list',
    getTotalDatas(callback) {
      callModuleApi(module, apiCode, { ID: '-1' }).then(items => callback(items));
    },
  };
}

// 解析上传配置（fileupload/imageupload 的 SELECTDATA）
export function parseUploaderConfig(selectData) {
  if (!selectData) return {};
  if (typeof selectData === 'object') return selectData;
  try {
    const parsed = JSON.parse(selectData);
    if (parsed && typeof parsed === 'object') return parsed;
  } catch (e) {}
  return {};
}

// 查询所有业务模块（用于 uiSetFull 选择器配置下拉）
export async function queryModules() {
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M02/A01/',
      params: {
        PageSize: 999,
        PageIndex: 1,
        FilterParams: { INPUT: '' },
      },
    });
    return ret.Items || [];
  } catch (e) {
    console.error('[selRegistry] queryModules failed:', e);
    return [];
  }
}

// 查询指定模块下的接口（用于 uiSetFull 选择器配置下拉）
// RS_M02 的 A02 open 返回 DTSC = VSS_MOUDLEAPI 子表
export async function queryApis(moduleCode) {
  if (!moduleCode) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M02/A02/',
      params: {
        FilterParams: { MODULECODE: moduleCode },
      },
    });
    return ret.DTSC || [];
  } catch (e) {
    return [];
  }
}

// 查询接口返回数据的字段名列表（用于 uiSetFull 值字段/显示字段下拉）
// 通过调用一次接口，取首条记录的 keys 作为字段选项，返回 [{key, title}] 对象数组
// 不同接口过滤器参数名不同（有的用 INPUT，有的用 REMARK/ID 等），这里依次降级尝试，
// 只要任一方式拿到数据即取其字段名。
export async function queryApiFields(module, apiCode) {
  if (!module || !apiCode) return [];
  // 降级尝试的过滤参数组合：空 → INPUT → ID:'-1' → PCODE（级联接口如行政区划查根级）
  const attempts = [
    {},
    { INPUT: '' },
    { ID: '-1' },
    { INPUT: '', ID: '-1' },
    { PCODE: '' },
    { PCODE: '0' },
  ];
  for (const filterParams of attempts) {
    try {
      const ret = await db.postData({
        api: `/api/data/call/${module}/${apiCode}/`,
        params: {
          PageSize: 1,
          PageIndex: 1,
          FilterParams: filterParams,
        },
      });
      const items = ret.Items || [];
      if (items.length > 0) {
        return Object.keys(items[0]).map(k => ({ key: k, title: k }));
      }
    } catch (e) {
      // 该参数组合报错则试下一个
    }
  }
  return [];
}

// 根据 RESOURCEID 查询字段名列表
// 查询资源的字段列表（用于 uiSetFull 值字段/显示字段下拉）
// 复用 RS_M01/A02 open 接口：F00 同时匹配 ID 和 RESOURCENAME，返回 MAIN(资源) + DTSA(字段)
// 传 GUID 或编码均可，直接从 DTSA 取字段名。
export async function queryFieldsByResourceId(resourceIdOrName) {
  if (!resourceIdOrName) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M01/A02/',
      params: { FilterParams: { RESOURCENAME: resourceIdOrName } },
    });
    const items = (ret && ret.DTSA) || [];
    debugger
    return items.map(item => ({
      key: item.FIELDNAME,
      title: item.FIELDNAME + (item.COMMENTS ? '(' + item.COMMENTS + ')' : ''),
    }));
  } catch (e) {
    console.error('[selRegistry] queryFieldsByResourceId failed:', e);
    return [];
  }
}

// 根据 RESOURCEID 查询过滤器列表
// 利用 RS_M01/A02 open 接口，F00 过滤器: A.ID=@RESOURCENAME OR A.RESOURCENAME=@RESOURCENAME
// 传入 resourceId 作为 RESOURCENAME 参数（F00 同时匹配 ID 和 RESOURCENAME）
// 返回的 DTSB 子表就是过滤器
export async function queryFiltersByResourceId(resourceId) {
  if (!resourceId) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M01/A02/',
      params: { FilterParams: { RESOURCENAME: resourceId } },
    });
    return (ret.DTSB || []).map(item => ({
      key: item.FILTERCODE,
      title: item.FILTERCODE + (item.REMARK ? '(' + item.REMARK + ')' : ''),
    }));
  } catch (e) {
    console.error('[selRegistry] queryFiltersByResourceId failed:', e);
    return [];
  }
}

// 查询 SQL 模板列表（用于模块接口 SQLID 选择）
// RS_M02/A08 → VSS_SQL, F02: (REMARK LIKE '%INPUT%' OR SQLCODE LIKE '%INPUT%')
export async function querySqlTemplates(input) {
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M02/A08/',
      params: {
        PageSize: 500,
        PageIndex: 1,
        FilterParams: { INPUT: input || '' },
      },
    });
    return (ret.Items || []).map(item => ({
      key: item.SQLCODE,
      title: item.SQLCODE + (item.REMARK ? '(' + item.REMARK + ')' : ''),
    }));
  } catch (e) {
    console.error('[selRegistry] querySqlTemplates failed:', e);
    return [];
  }
}

// 搜索资源列表（用于页面配置"从其他字段复制配置"弹窗）
// RS_M01/A06 → VSS_RESOURCE, F01: INPUT 模糊搜索
export async function searchResources(input) {
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M01/A01/',
      params: {
        PageSize: 20,
        PageIndex: 1,
        FilterParams: { INPUT: input || '' },
      },
    });
    return ret.Items || [];
  } catch (e) {
    console.error('[selRegistry] searchResources failed:', e);
    return [];
  }
}

// 查询指定资源的 UI 配置字段（resuipc，含 SELECTDATA/UPDATEFIELDS 等配置值）
// RS_M01/A05 → VSS_RESUIPC, F01: RESOURCEID 过滤
export async function queryUisetFields(resourceId) {
  if (!resourceId) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M01/A08/',
      params: {
        PageSize: 500,
        PageIndex: 1,
        FilterParams: { RESOURCEID: resourceId },
      },
    });
    return (ret.Items || []).map(item => ({
      ID: item.ID,
      FIELDNAME: item.FIELDNAME || item.RESFIELDNAME || '',
      LABELNAME: item.LABELNAME || '',
      EDITTYPE: item.EDITTYPE || '',
      SELECTDATA: item.SELECTDATA || '',
      UPDATEFIELDS: item.UPDATEFIELDS || '',
      QUERYTYPE: item.QUERYTYPE || '',
      QUERYMODE: item.QUERYMODE || '',
      EDITABLE: item.EDITABLE,
      NULLABLE: item.NULLABLE,
      PLACEHOLDER: item.PLACEHOLDER || '',
      MAXLENGTH: item.MAXLENGTH || '',
      COLSPAN: item.COLSPAN || '',
      EDITGROUP: item.EDITGROUP || '',
    }));
  } catch (e) {
    console.error('[selRegistry] queryUisetFields failed:', e);
    return [];
  }
}

// 查询指定模块的按钮配置（用于"从其他模块复制按钮"）
// RS_M18/A02 open -> 返回 MODBUTTON 子表
export async function queryModuleButtons(moduleCode) {
  if (!moduleCode) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M18/A02/',
      params: { FilterParams: { MODULECODE: moduleCode } },
    });
    var modBtn = ret && ret.MODBUTTON;
    if (Array.isArray(modBtn)) return modBtn;
    if (modBtn && Array.isArray(modBtn.items)) return modBtn.items;
    return [];
  } catch (e) {
    console.error('[selRegistry] queryModuleButtons failed:', e);
    return [];
  }
}

// 查询指定模块的页面配置（用于"从其他模块复制按钮"时展示页面名称）
// RS_M18/A02 open -> 返回 MODPAGE 子表
export async function queryModulePages(moduleCode) {
  if (!moduleCode) return [];
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M18/A02/',
      params: { FilterParams: { MODULECODE: moduleCode } },
    });
    var modPage = ret && ret.MODPAGE;
    if (Array.isArray(modPage)) return modPage;
    if (modPage && Array.isArray(modPage.items)) return modPage.items;
    return [];
  } catch (e) {
    console.error('[selRegistry] queryModulePages failed:', e);
    return [];
  }
}

// 一次查询模块的按钮+页面（避免重复调 A02）
export async function queryModuleButtonsAndPages(moduleCode) {
  if (!moduleCode) return { buttons: [], pages: [] };
  try {
    const ret = await db.postData({
      api: '/api/data/call/RS_M18/A02/',
      params: { FilterParams: { MODULECODE: moduleCode } },
    });
    var modBtn = ret && ret.MODBUTTON;
    var modPage = ret && ret.MODPAGE;
    var buttons = Array.isArray(modBtn) ? modBtn : (modBtn && Array.isArray(modBtn.items) ? modBtn.items : []);
    var pages = Array.isArray(modPage) ? modPage : (modPage && Array.isArray(modPage.items) ? modPage.items : []);
    buttons = buttons.filter(function(b) { return (+b.ISDELETED || 0) !== 1; });
    pages = pages.filter(function(p) { return (+p.ISDELETED || 0) !== 1; });
    return { buttons: buttons, pages: pages };
  } catch (e) {
    console.error('[selRegistry] queryModuleButtonsAndPages failed:', e);
    return { buttons: [], pages: [] };
  }
}
