import db from './db';
const login = async function(userInfo) {
  userInfo.api = '/api/user/login';
  return db.postData(userInfo, 'user');
};

const loginout = async function(userInfo) {
  userInfo.api = '/api/user/loginout';
  return db.postData(userInfo, 'user');
};

const resetPass = async function(params) {
  await db.postData({
    'api': '/api/sm15/call/RS_M05/A13/',
    params
  });
};

const loadMenu = async function() {
  let para = {};
  para['tp'] = 'loadmenu';
  return db.postData(para);
};

export default { login, loginout, loadMenu, resetPass };
