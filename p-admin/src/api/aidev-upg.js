import db from '@/api/db';

// 升级管理 API 封装
// 标准 ORM 接口(A01/A02) 走 DataController: /api/data/call/RS_MAIDEVUPG/
// 自定义接口(A05-A08) 走 RMAIDevUpgController: /api/RMAIDevUpg/call/RS_MAIDEVUPG/
const MODULE = 'RS_MAIDEVUPG';
const STD_BASE = `/api/data/call/${MODULE}`; // 标准接口(A01/A02)
const CUSTOM_BASE = `/api/RMAIDevUpg/call/${MODULE}`; // 自定义接口

// A05 导入升级脚本
export async function importScript(scriptContent) {
  return db.postData({
    api: `${CUSTOM_BASE}/A05/`,
    params: { scriptContent },
  });
}

// A06 执行升级
export async function execute(upgradeId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A06/`,
    params: { upgradeId },
  });
}

// A07 回滚
export async function rollback(upgradeId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A07/`,
    params: { upgradeId },
  });
}

// A08 预览变更项
export async function preview(upgradeId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A08/`,
    params: { upgradeId },
  });
}

// 查询升级记录列表(标准 A01)
export async function listUpgrades(filterParams) {
  return db.postData({
    api: `${STD_BASE}/A01/`,
    params: {
      FilterParams: filterParams || {},
      PageSize: 50,
      PageIndex: 1,
    },
  });
}

// 打开单条升级记录(标准 A02, 含 log 子表)
export async function openUpgrade(id) {
  return db.postData({
    api: `${STD_BASE}/A02/`,
    params: { FilterParams: { ID: id } },
  });
}

export default {
  importScript,
  execute,
  rollback,
  preview,
  listUpgrades,
  openUpgrade,
};
