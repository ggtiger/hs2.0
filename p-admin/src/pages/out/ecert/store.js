import db from '@/api/db';

// 外部对接页（电子证书查询），走 /api/outer/call/LI_ECERT 专用通道
// 不走标准 createStore.getStore（外部用户无完整 Vuex 上下文，无 DataTable 双向绑定需求）

const STORE_NAME = 'out/ecert';

// 查证书（A02）
async function queryCert({ id, certNo }) {
  var params = {};
  if (id) {
    params.ID = id;
  } else {
    params.CERTNO = (certNo || '').trim();
  }
  return db.postData({
    api: '/api/outer/call/LI_ECERT/A02/',
    params: params,
  });
}

// 看证书（A03，带密码）
async function viewCert({ id, pwd }) {
  var params = { ID: id };
  if (pwd) params.PWD = pwd;
  return db.postData({
    api: '/api/outer/call/LI_ECERT/A03/',
    params: params,
  });
}

const Constants = { STORE_NAME };

function getUrl(type) {
  return db.getUrl(type);
}

export { Constants, queryCert, viewCert, getUrl };
