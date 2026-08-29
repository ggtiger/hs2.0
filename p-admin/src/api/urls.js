/**
 * 纯 URL 配置模块 — 不引入 axios、不依赖 store/token
 *
 * 用途：让 .vue 组件需要拼装下载/预览 URL 时可以从这里 import，
 *      避免为了拿一个 URL 前缀而 `import db from '@/api/db'`（违反前端数据流规范）。
 *
 * 网络请求层 `@/api/db` 内部也复用本模块的 getUrl。
 *
 * 详见 docs/frontend-store-convention.md 「例外白名单」
 *
 * 生产部署说明：API 地址使用同源相对路径，由部署端 nginx (deploy/nginx.conf) 反代：
 *   /api/   -> webapi:5001  （业务接口、文件上传/PDF）
 *   /auth/  -> auth:5000    （登录/登出）
 *   /hub/   -> webapi:5001  （SignalR WebSocket）
 * 本地联调时如需直连后端，可临时改回绝对地址（如 http://127.0.0.1:5001）。
 */

const getUrl = function(type) {
  var url = '';
  switch (type) {
    case 'upload':
      // 本地联调: url = 'http://127.0.0.1:5001/api/file/';
      url = '/api/file/';
      break;
    case 'url':
      // 本地联调: url = 'http://127.0.0.1:5001';
      url = '';
      break;
    case 'pdf':
      // 本地联调: url = 'http://127.0.0.1:5001/api/file/pdf/';
      url = '/api/file/pdf/';
      break;
    case 'pdfsy':
      // 本地联调: url = 'http://127.0.0.1:5001/api/file/pdfsy/';
      url = '/api/file/pdfsy/';
      break;
    case 'user':
      // 本地联调: url = 'http://127.0.0.1:5000';
      url = '/auth';
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
