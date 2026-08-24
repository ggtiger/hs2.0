import db from '@/api/db';
import store from '@/store';

// AI 开发助理 API 封装
// 标准 ORM 接口(A01/A02) 走 DataController: /api/data/call/RS_MAIDEV/
// 自定义接口(A05+) 走 RMAIDevController: /api/RMAIDev/call/RS_MAIDEV/ (路由 api/[controller], 继承 DataController.Call, this.doMyApi 才能调到子类)
// 流式接口 generate-stream 走 RMAIDevController 独立 action (SSE, 不走 Call 框架)
const MODULE = 'RS_MAIDEV';
const CUSTOM_BASE = `/api/RMAIDev/call/${MODULE}`; // 自定义接口(走 RMAIDevController)

// A05 生成: 发送对话消息, AI 产出变更项
export async function generate(sessionId, message) {
  return db.postData({
    api: `${CUSTOM_BASE}/A05/`,
    params: { sessionId, message },
  });
}

// A05 流式生成 (SSE): 逐字推送 AI 文本 + 工具调用/变更项/校验事件
// onEvent(evt) 回调收事件块，evt.type: text/tool_call/tool_result/item/validate/error/done/heartbeat
// 后端 SseWriter.Frame 输出格式: "data: {json}\n\n"，前端按此解析
export async function generateStream(sessionId, message, onEvent) {
  const url = db.getUrl('url') + '/api/RMAIDev/generate-stream';
  const formData = new FormData();
  formData.append('sessionId', sessionId);
  formData.append('message', message);
  formData.append('_userInfo_', JSON.stringify(store.state['user'].userInfo));
  const res = await fetch(url, {
    method: 'POST',
    body: formData,
    headers: { Authorization: 'Bearer ' + store.state['user'].access_token },
  });
  if (!res.ok && !res.body) {
    throw new Error('流式请求失败: HTTP ' + res.status);
  }
  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  // 按 SseWriter.Frame 格式 "data: {json}\n\n" 拆事件
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split('\n\n');
    buffer = events.pop(); // 保留最后不完整的片段
    for (const evt of events) {
      const trimmed = evt.trim();
      if (!trimmed.startsWith('data: ')) continue;
      const json = trimmed.slice(6).trim();
      if (!json) continue;
      try {
        onEvent(JSON.parse(json));
      } catch (e) {
        // 解析失败的块跳过，不中断流
      }
    }
  }
  // 处理 buffer 中残留的最后一块
  const tail = buffer.trim();
  if (tail.startsWith('data: ')) {
    const json = tail.slice(6).trim();
    if (json) {
      try { onEvent(JSON.parse(json)) } catch (e) { /* ignore */ }
    }
  }
}

// A06 校验变更包
export async function validateChangeSet(changesetId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A06/`,
    params: { changesetId },
  });
}

// A07 导出升级包(.aidev.sql)
export async function exportScript(sessionId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A07/`,
    params: { sessionId },
  });
}

// A09 确认变更项 DRAFT->CONFIRMED
export async function confirmItem(itemId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A09/`,
    params: { itemId },
  });
}

// A10 拒绝变更项 DRAFT->REJECTED
export async function rejectItem(itemId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A10/`,
    params: { itemId },
  });
}

// A11 撤销确认 CONFIRMED->DRAFT
export async function unconfirmItem(itemId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A11/`,
    params: { itemId },
  });
}

// A14 去重清理变更项（同 changeset 内重复项只保留一条）
export async function dedupItems(sessionId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A14/`,
    params: { sessionId },
  });
}

// A17 执行已确认脚本（开发环境直接落库，调试用）
export async function executeConfirmed(sessionId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A17/`,
    params: { sessionId },
  });
}

// A18 加载历史对话（重新打开工作区显示之前的对话）
export async function getConversation(sessionId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A18/`,
    params: { sessionId },
  });
}

// A15 合并所有 DRAFT 为一条统一变更项（按会话合并为一条）
export async function mergeItems(sessionId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A15/`,
    params: { sessionId },
  });
}

// A12 获取已确认脚本
export async function getConfirmedScript(changesetId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A12/`,
    params: { changesetId },
  });
}

// 查询变更项列表(走标准 A01, 用 F03 按 changesetId 过滤)
export async function listChangeItems(changesetId) {
  return db.postData({
    api: `${CUSTOM_BASE}/A16/`,
    params: { changesetId },
  });
}

// A19 创建模块向导会话（session + changeset，6 步共享）
// 返回 {sessionId, changesetId, sessionCode}
export async function openWizardSession() {
  return db.postData({
    api: `${CUSTOM_BASE}/A19/`,
    params: {},
  });
}

// generate-step-stream (SSE 分步生成): 按向导当前 step 只生成该步相关工具的变更项
// onEvent(evt) 回调收事件块，evt.type: text/tool_call/tool_result/item/validate/error/done/heartbeat
// 完全复用 generateStream 的 SSE 解析逻辑，只改 url + formData
export async function generateStepStream(sessionId, step, wizardContext, message, onEvent) {
  const url = db.getUrl('url') + '/api/RMAIDev/generate-step-stream';
  const formData = new FormData();
  formData.append('sessionId', sessionId);
  formData.append('step', step);
  formData.append('wizardContext', wizardContext || '');
  formData.append('message', message);
  formData.append('_userInfo_', JSON.stringify(store.state['user'].userInfo));
  const res = await fetch(url, {
    method: 'POST',
    body: formData,
    headers: { Authorization: 'Bearer ' + store.state['user'].access_token },
  });
  if (!res.ok && !res.body) {
    throw new Error('流式请求失败: HTTP ' + res.status);
  }
  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  // 按 SseWriter.Frame 格式 "data: {json}\n\n" 拆事件
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split('\n\n');
    buffer = events.pop(); // 保留最后不完整的片段
    for (const evt of events) {
      const trimmed = evt.trim();
      if (!trimmed.startsWith('data: ')) continue;
      const json = trimmed.slice(6).trim();
      if (!json) continue;
      try {
        onEvent(JSON.parse(json));
      } catch (e) {
        // 解析失败的块跳过，不中断流
      }
    }
  }
  // 处理 buffer 中残留的最后一块
  const tail = buffer.trim();
  if (tail.startsWith('data: ')) {
    const json = tail.slice(6).trim();
    if (json) {
      try { onEvent(JSON.parse(json)) } catch (e) { /* ignore */ }
    }
  }
}

// generate-all-stream (SSE 一键生成全部6步): 用户描述一次需求，后端连续生成6步
// onEvent(evt) 回调，evt.type: step_start/text/tool_call/tool_result/item/validate/error/done/heartbeat
// 比 generateStepStream 多 step_start 事件(推进步骤条)，text/tool_call/tool_result 带 step 字段
export async function generateAllStream(sessionId, wizardContext, message, onEvent) {
  const url = db.getUrl('url') + '/api/RMAIDev/generate-all-stream';
  const formData = new FormData();
  formData.append('sessionId', sessionId);
  formData.append('wizardContext', wizardContext || '');
  formData.append('message', message);
  formData.append('_userInfo_', JSON.stringify(store.state['user'].userInfo));
  const res = await fetch(url, {
    method: 'POST',
    body: formData,
    headers: { Authorization: 'Bearer ' + store.state['user'].access_token },
  });
  if (!res.ok && !res.body) {
    throw new Error('流式请求失败: HTTP ' + res.status);
  }
  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split('\n\n');
    buffer = events.pop();
    for (const evt of events) {
      const trimmed = evt.trim();
      if (!trimmed.startsWith('data: ')) continue;
      const json = trimmed.slice(6).trim();
      if (!json) continue;
      try { onEvent(JSON.parse(json)) } catch (e) { /* ignore */ }
    }
  }
  const tail = buffer.trim();
  if (tail.startsWith('data: ')) {
    const json = tail.slice(6).trim();
    if (json) { try { onEvent(JSON.parse(json)) } catch (e) { /* ignore */ } }
  }
}

// ============================================================
// AI 记忆中枢 - 反馈回流(2026-07-19)
// 不走 ORM DataTable, 直接 JSON POST 到 RMAIDevController 独立端点
// ============================================================

// 提交用户反馈(👍/👎/修正后/采纳)到 tss_ai_feedback
// payload: {sessionId, sceneCode, assetType, feedbackType, userRequest, originalOutput, finalOutput, diffText, issueTags, qualityScore, comment}
export async function submitFeedback(payload) {
  const url = db.getUrl('url') + '/api/RMAIDev/feedback';
  const res = await fetch(url, {
    method: 'POST',
    body: JSON.stringify(payload || {}),
    headers: {
      'Content-Type': 'application/json',
      Authorization: 'Bearer ' + store.state['user'].access_token,
    },
  });
  if (!res.ok) throw new Error('提交反馈失败: HTTP ' + res.status);
  return res.json();
}

// 把指定反馈提升为 example(后续 AI 调用会检索使用)
// payload: {feedbackId}
export async function promoteExample(feedbackId) {
  const url = db.getUrl('url') + '/api/RMAIDev/promote-example';
  const res = await fetch(url, {
    method: 'POST',
    body: JSON.stringify({ feedbackId: feedbackId }),
    headers: {
      'Content-Type': 'application/json',
      Authorization: 'Bearer ' + store.state['user'].access_token,
    },
  });
  if (!res.ok) throw new Error('提升示例失败: HTTP ' + res.status);
  return res.json();
}

// 手动失效记忆缓存(管理页保存后调用)
export async function invalidateMemory() {
  const url = db.getUrl('url') + '/api/RMAIDev/invalidate-memory';
  const res = await fetch(url, {
    method: 'POST',
    headers: { Authorization: 'Bearer ' + store.state['user'].access_token },
  });
  if (!res.ok) throw new Error('刷新缓存失败: HTTP ' + res.status);
  return res.json();
}

// 测试 LLM 连接(AI 配置中心用): 用启用配置发 ping, 返回耗时/模型/结果
export async function testLlm() {
  const url = db.getUrl('url') + '/api/RMAIDev/test-llm';
  const res = await fetch(url, {
    method: 'POST',
    headers: { Authorization: 'Bearer ' + store.state['user'].access_token },
  });
  if (!res.ok) throw new Error('测试失败: HTTP ' + res.status);
  return res.json();
}

export default {
  generate,
  generateStream,
  validateChangeSet,
  exportScript,
  confirmItem,
  rejectItem,
  unconfirmItem,
  dedupItems,
  executeConfirmed,
  getConversation,
  mergeItems,
  getConfirmedScript,
  listChangeItems,
  openWizardSession,
  generateStepStream,
  generateAllStream,
  submitFeedback,
  promoteExample,
  invalidateMemory,
  testLlm,
};
