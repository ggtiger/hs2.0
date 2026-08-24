import axios from 'axios';
import store from '../store';
import { getUrl } from '@/api/urls';
// import pako from 'pako';
// 设置axios为form-data
// axios.defaults.headers.post['Content-Type'] = 'application/x-www-form-urlencoded';
// axios.defaults.headers.get['Content-Type'] = 'application/x-www-form-urlencoded';
const tran = function(data) {
  if (typeof data === 'string') {
    return data;
  }
  let ret = '';
  for (let it in data) {
    ret +=
      encodeURIComponent(it) +
      '=' +
      encodeURIComponent(JSON.stringify(data[it])) +
      '&';
  }
  return ret;
};
axios.defaults.transformRequest = [tran];
let pending = []; // 声明一个数组用于存储每个ajax请求的取消函数和ajax标识
let removePending = config => {
  for (let p in pending) {
    if (pending[p].u === `${config.url}${config.method}${tran(config.data)}`) {
      // 当当前请求在数组中存在时执行函数体
      pending[p].f('您的手速太快了'); // 执行取消操作
      pending.splice(p, 1); // 把这条记录从数组中移除
    }
  }
  // console.log(config);
};

let isReatCalling = config => {
  let ret = false;
  if (config.data.ISCHECKREPEAT === true) {
    let t = pending.find(p => {
      return p.u === `${config.url}${config.method}${tran(config.data)}`;
    });
    ret = !!t;
  }
  return ret;
};

// 添加请求拦截器
axios.interceptors.request.use(
  config => {
    if (isReatCalling(config)) {
      throw new Error('请求已提交,无须重复提交！');
    }
    removePending(config);
    let CancelToken = axios.CancelToken;
    config.cancelToken = new CancelToken(c => {
      pending.push({
        u: `${config.url}${config.method}${tran(config.data)}`,
        f: c
      });
    });
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// 添加响应拦截器
axios.interceptors.response.use(
  response => {
    removePending(response.config); // 在一个ajax响应后再执行一下取消操作，把已经完成的请求从pending中移除
    return response;
  },
  (error, response) => {
    if (error && error.response && error.response.status === 401) {
      Promise.reject(error);
    }
    // return this.config.cancelData
    return Promise.reject(error);
  }
);

const postData = async function(param, type) {
  type = type || 'url';
  let tpara = { ...param };
  tpara['_userInfo_'] = store.state['user'].userInfo;
  return new Promise(function(resolve, reject) {
    axios
      .post(getUrl(type) + param['api'], tpara, {
        headers: { Authorization: 'Bearer ' + store.state['user'].access_token }
      })
      .then(function(res) {
        if (!res) {
          reject(new Error('请求异常！'));
        }
        if (res.code === '401') {
          reject(new Error('登陆超时！'));
        }
        if (res.data.Code === '501') {
          reject(new Error('登陆超时！'));
        }
        if (res.data.Code === 500) {
          reject(new Error(res.data.Message || '内部错误！'));
        } else {
          resolve(res.data.Data);
        }
      })
      .catch(function(e) {
        if (e.response && e.response.status === 401) {
          reject(new Error('登陆超时！'));
        } else {
          if (e.message === 'Network Error') {
            reject(new Error('网络异常！'));
          } else {
            reject(e);
          }
        }
      });
  });
};

const open = function(params) {
  let param = {};
  if (params['sqlId']) {
    param['tp'] = 'query6';
    param['json'] = encodeURIComponent(JSON.stringify({ params }));
  } else {
    params['tp'] = 'query1';
    param = params;
  }
  return postData(param);
};

const _getQueryInfo = function(para) {
  var queryInfo = {
    where: '',
    orderBy: '',
    pageSize: 20,
    pageIndex: 1,
    groupBy: '',
    egg: '',
    having: ''
  };
  queryInfo = Object.assign(queryInfo, para);
  return queryInfo;
};

const openTables = function(paths) {
  let param = {};
  let postPaths = {};
  paths.forEach(p => {
    let path = p['path'];
    let para = p['para'];
    para = _getQueryInfo(para);
    p['para'] = para;
    para['scmName'] = para['scmName'] || para['modalName'];
    postPaths[path] = p;
  });
  param['tp'] = 'query4';
  param['json'] = encodeURIComponent(JSON.stringify(postPaths));
  return postData(param);
};

const getNewID = function(scmName, inc) {
  var param = { tp: 'getid', modalName: scmName, col: inc };
  return postData(param);
};

const call = function(para) {
  let tpara = { ...para };
  tpara['_userInfo_'] = store.state['user'].userInfo;
  let param = {
    tp: 'call',
    ISCHECKREPEAT: para.ISCHECKREPEAT,
    json: encodeURIComponent(JSON.stringify({ para: tpara }))
  };
  return postData(param);
};

// 发送纯JSON请求（覆盖全局tran的form编码），用于后端[FromBody]端点
const postJson = async function(api, data) {
  return new Promise(function(resolve, reject) {
    axios
      .post(getUrl('url') + api, data, {
        headers: {
          Authorization: 'Bearer ' + store.state['user'].access_token,
          'Content-Type': 'application/json'
        },
        transformRequest: [function(d) {
          return JSON.stringify(d);
        }]
      })
      .then(function(res) {
        if (res.data && res.data.Code === 200) {
          resolve(res.data.Data);
        } else {
          resolve(res.data);
        }
      })
      .catch(function(e) {
        reject(e);
      });
  });
};

export default {
  getUrl,
  postData,
  postJson,
  open,
  openTables,
  getNewID,
  call
};
