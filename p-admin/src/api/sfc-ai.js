import store from '../store';
import db from './db';
import { streamSSE } from '@/utils/ai/streamSSE';

/**
 * SFC AI 代码助手 - SSE 流式生成
 * POST /api/RMSfcAi/generate-code
 *
 * 使用统一 streamSSE 解析（替代历史 indexOf + 多行 data: 拼接实现）。
 *
 * @param {string} message - 用户消息
 * @param {object} context - { currentFile, siblingFiles, moduleCode, moduleSchema }
 * @param {function} onEvent - 回调 ({type, text?, error?, usage?})
 * @returns {Promise<void>}
 */
export async function generateCodeStream(message, context, onEvent) {
  var baseUrl = db.getUrl('url');
  var url = baseUrl + '/api/RMSfcAi/generate-code';

  var userInfo = store.state['user'] ? store.state['user'].userInfo : null;
  var accessToken = store.state['user'] ? store.state['user'].access_token : '';

  // FormData 表单格式提交
  var formData = new FormData();
  formData.append('message', message);
  formData.append('context', JSON.stringify(context || {}));
  if (userInfo) {
    formData.append('_userInfo_', JSON.stringify(userInfo));
  }

  var resp = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': 'Bearer ' + accessToken,
    },
    body: formData,
  });

  if (!resp.ok) {
    throw new Error('请求失败: ' + resp.status + ' ' + resp.statusText);
  }

  if (!resp.body) {
    throw new Error('浏览器不支持流式响应');
  }

  // 统一 SSE 解析：streamSSE 合并多行 data: 前缀 + tail 处理
  await streamSSE(resp, function(evt) {
    if (onEvent) onEvent(evt);
  });
}

/**
 * 获取模块元数据 schema - 从后端 tss_moudle/tss_resuipc 读取模块的 API/字段/子表/过滤器
 * POST /api/RMSfcAi/get-module-schema
 *
 * @param {string} moduleCode - 模块编码 (如 LIB_M07)
 * @returns {Promise<object>} - { moduleCode, moduleName, tableName, apis, fields, refFields, subTables, queryFilterParams }
 */
export async function getModuleSchema(moduleCode) {
  var baseUrl = db.getUrl('url');
  var url = baseUrl + '/api/RMSfcAi/get-module-schema';

  var userInfo = store.state['user'] ? store.state['user'].userInfo : null;
  var accessToken = store.state['user'] ? store.state['user'].access_token : '';

  var formData = new FormData();
  formData.append('moduleCode', moduleCode);
  if (userInfo) {
    formData.append('_userInfo', JSON.stringify(userInfo));
  }

  var resp = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': 'Bearer ' + accessToken,
    },
    body: formData,
  });

  if (!resp.ok) {
    throw new Error('请求失败: ' + resp.status + ' ' + resp.statusText);
  }

  var json = await resp.json();
  if (json.Code !== 200) {
    throw new Error(json.Message || '获取模块元数据失败');
  }
  return json.Data;
}

/**
 * 执行用户确认的元数据 SQL
 * POST /api/RMSfcAi/execute-metadata-sql
 *
 * @param {string} sql - 元数据 SQL (仅 INSERT/UPDATE/DELETE)
 * @returns {Promise<object>} - { affectedRows }
 */
export async function executeMetadataSql(sql) {
  var baseUrl = db.getUrl('url');
  var url = baseUrl + '/api/RMSfcAi/execute-metadata-sql';

  var userInfo = store.state['user'] ? store.state['user'].userInfo : null;
  var accessToken = store.state['user'] ? store.state['user'].access_token : '';

  var formData = new FormData();
  formData.append('sql', sql);
  if (userInfo) {
    formData.append('_userInfo', JSON.stringify(userInfo));
  }

  var resp = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': 'Bearer ' + accessToken,
    },
    body: formData,
  });

  if (!resp.ok) {
    throw new Error('请求失败: ' + resp.status + ' ' + resp.statusText);
  }

  var json = await resp.json();
  if (json.Code !== 200) {
    throw new Error(json.Message || 'SQL 执行失败');
  }
  return json.Data;
}
