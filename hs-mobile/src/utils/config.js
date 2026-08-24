/**
 * 应用配置
 *
 * 端口说明（对齐桌面端 p-admin getUrl）：
 * - 业务数据 / 文件 / PDF：WebAPI 端口 5001（桌面端 getUrl('url') = http://127.0.0.1:5001）
 * - 登录认证：Auth 端口 5000（桌面端 getUrl('user') = http://127.0.0.1:5000）
 *
 * 多端策略：
 * - H5：用相对路径 '/api'，由 vite.config.js 的 proxy 分流（/api/user→5000，其余 /api→5001）
 * - 小程序 / App：直连完整 URL，通过 .env 的 VITE_DATA_BASE / VITE_AUTH_BASE 配置
 *   （微信开发者工具需勾选「不校验合法域名」，上线时在小程序后台配置 request 合法域名）
 */

// H5 走 vite proxy：base 留空，由 api path 的 /api 前缀触发代理路由
//   '/api/user/*' → 5000，'/api/*' → 5001
// 小程序/App 直连完整 URL
let DATA_BASE_URL
let AUTH_BASE_URL
// #ifdef H5
DATA_BASE_URL = ''
AUTH_BASE_URL = ''
// #endif
// #ifndef H5
DATA_BASE_URL = import.meta.env.VITE_DATA_BASE || 'http://localhost:5001'
AUTH_BASE_URL = import.meta.env.VITE_AUTH_BASE || 'http://localhost:5000'
// #endif

export { DATA_BASE_URL, AUTH_BASE_URL }

// 文件/PDF 域名（证书预览、电子证书下载）
export const FILE_BASE_URL = DATA_BASE_URL

/**
 * 构造业务数据接口 URL（WebAPI 5001）
 * @param {string} moduleCode 模块编码，如 LI_M02
 * @param {string} apiCode 接口编码，如 A14
 * @returns {string} /api/data/call/LI_M02/A14/
 */
export function dataApiPath(moduleCode, apiCode) {
  return `/api/data/call/${moduleCode}/${apiCode}/`
}

/**
 * 构造外部接口 URL（WebAPI 5001，免认证）
 */
export function outerApiPath(moduleCode, apiCode) {
  return `/api/outer/call/${moduleCode}/${apiCode}/`
}
