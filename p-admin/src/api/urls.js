/**
 * 纯 URL 配置模块 — 不引入 axios、不依赖 store/token
 *
 * 用途：让 .vue 组件需要拼装下载/预览 URL 时可以从这里 import，
 *      避免为了拿一个 URL 前缀而 `import db from '@/api/db'`（违反前端数据流规范）。
 *
 * 网络请求层 `@/api/db` 内部也复用本模块的 getUrl。
 *
 * 详见 docs/frontend-store-convention.md 「例外白名单」
 */

const getUrl = function(type) {
  var url = '';
  switch (type) {
    case 'upload':
      // url = 'http://192.168.137.1:5001/api/file/';
      url = 'http://127.0.0.1:5001/api/file/';
      break;
    case 'url':
      // url = "http://10.100.129.111:5001";
      url = 'http://127.0.0.1:5001';
      // url = 'http://192.168.56.1:5001';
      // url = 'http://192.168.137.1:5001';
      break;
    case 'pdf':
      // url = "http://10.100.129.111:5001";
      url = 'http://127.0.0.1:5001/api/file/pdf/';
      // url = 'http://192.168.56.1:5001';
      // url = 'http://192.168.137.1:5001';
      break;
    case 'pdfsy':
      // url = "http://10.100.129.111:5001";
      url = 'http://127.0.0.1:5001/api/file/pdfsy/';
      // url = 'http://192.168.56.1:5001';
      // url = 'http://192.168.137.1:5001';
      break;
    case 'user':
      // url = "http://10.100.129.111:5001";
      // url = 'http://192.168.1.5:5000';
      url = 'http://127.0.0.1:5000';
      break;
    case 'socket':
      url = 'ws://ydzl.gujing.net:9091';
      break;
    default:
      alert('类型：' + type + '不存在！');
  }
  return url;
};

export { getUrl };
export default { getUrl };
