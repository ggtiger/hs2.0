/**
 * OnlyOffice 公共模块
 * 提供脚本加载、HTTP 工具函数，供 rs-onlyoffice-preview、word-template-editor、excel-editor 共享
 */
// OnlyOffice Document Server 地址：
// 优先使用构建时环境变量；否则动态推导：浏览器当前主机 + (前端端口 + 8)
//   外网代理: 6080 -> 6088；内网直连: 8080 -> 8088
// 无端口(默认80)时回退 6088（外网代理场景）
var ONLYOFFICE_URL = process.env.ONLYOFFICE_DOC_SERVER ||
  (typeof window !== 'undefined' && window.location
    ? window.location.protocol + '//' + window.location.hostname + ':'
        + (window.location.port ? (Number(window.location.port) + 8) : '6088')
    : 'http://localhost:8088');

// OnlyOffice API 脚本加载状态（全局单例，确保只加载一次）
var scriptLoaded = false;
var scriptLoading = false;
var scriptCallbacks = [];

/**
 * 加载 OnlyOffice API 脚本（全局单例模式）
 * 多个组件同时调用时，只加载一次脚本，所有调用者共享同一个 Promise
 */
function loadOnlyOfficeScript() {
  return new Promise(function(resolve, reject) {
    if (scriptLoaded && window.DocsAPI) {
      resolve();
      return;
    }
    if (scriptLoading) {
      scriptCallbacks.push({ resolve: resolve, reject: reject });
      return;
    }
    scriptLoading = true;
    scriptCallbacks.push({ resolve: resolve, reject: reject });

    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = ONLYOFFICE_URL + '/web-apps/apps/api/documents/api.js';
    script.onload = function() {
      scriptLoaded = true;
      scriptLoading = false;
      scriptCallbacks.forEach(function(cb) { cb.resolve(); });
      scriptCallbacks = [];
    };
    script.onerror = function() {
      scriptLoading = false;
      scriptCallbacks.forEach(function(cb) { cb.reject(new Error('OnlyOffice API 加载失败')); });
      scriptCallbacks = [];
    };
    document.head.appendChild(script);
  });
}

/**
 * 通用 HTTP GET 请求
 */
function httpGet(url, token) {
  return new Promise(function(resolve, reject) {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', url);
    if (token) xhr.setRequestHeader('Authorization', 'Bearer ' + token);
    xhr.onload = function() {
      if (xhr.status === 200) {
        resolve(JSON.parse(xhr.responseText));
      } else {
        reject(new Error('请求失败: ' + xhr.status));
      }
    };
    xhr.onerror = function() { reject(new Error('网络错误')); };
    xhr.send();
  });
}

/**
 * 通用 HTTP POST 请求
 */
function httpPost(url, data, token) {
  return new Promise(function(resolve, reject) {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', 'application/json');
    if (token) xhr.setRequestHeader('Authorization', 'Bearer ' + token);
    xhr.onload = function() {
      if (xhr.status === 200) {
        resolve(JSON.parse(xhr.responseText));
      } else {
        reject(new Error('请求失败: ' + xhr.status));
      }
    };
    xhr.onerror = function() { reject(new Error('网络错误')); };
    xhr.send(JSON.stringify(data));
  });
}

export { loadOnlyOfficeScript, httpGet, httpPost, ONLYOFFICE_URL };
