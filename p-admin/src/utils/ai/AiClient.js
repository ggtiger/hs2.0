/**
 * AiClient - 统一 AI 客户端（传输 + 事件分发层）
 *
 * 按 scene 选择传输方式：
 *   assistant / form / optimize -> SignalR (/assistantHub)
 *   aidev / wizard / sfc        -> SSE（复用 api/aidev.js、api/sfc-ai.js）
 *
 * 设计原则：AiClient 只负责传输与事件分发，通过 onBlock/onItem/onValidate/
 * onStep/onError/onDone 回调通知调用方，不维护 messages（各入口的消息模型不同）。
 * 调用方收到回调后自行更新 UI 状态。
 */

import * as signalR from '@aspnet/signalr';
import store from '@/store';
import db from '@/api/db';
import { aiAgentProxy } from '@/utils/aiAgentProxy';
import * as aidev from '@/api/aidev';
import * as sfcAi from '@/api/sfc-ai';
import { loadSceneConfig, getSceneSync } from './sceneConfig';

// ==================== SignalR 共享连接 ====================

// assistant/form/optimize 场景共享一个 SignalR 连接（与 api/assistant.js 一致）
let connection = null;
let startingPromise = null;
let ready = false;
// 每个 scene 注册自己的 block 回调（assistant -> block 事件，form -> formblock 事件）
const blockCallbacks = {};
// 前端工具调用的额外上下文提供器（由 form 场景返回 {formEdit}，其他返回 {}）
let frontendToolExtraProvider = null;

function build() {
  var c = new signalR.HubConnectionBuilder()
    .withUrl(db.getUrl('url') + '/assistantHub', {
      // 后端 AssistantHub 已加 [Authorize]，JWT 通过 query string 传递
      accessTokenFactory: function() {
        return (store.state['user'] && store.state['user'].access_token) || '';
      }
    })
    .configureLogging(signalR.LogLevel.Warning)
    .build();
  c.on('block', function(b) {
    var cb = blockCallbacks.assistant;
    if (cb) cb(b);
  });
  c.on('formblock', function(b) {
    var cb = blockCallbacks.form;
    if (cb) cb(b);
  });
  c.on('frontend_tool_call', function(callId, toolName, argsJson) {
    handleFrontendToolCall(callId, toolName, argsJson);
  });
  c.onclose(function() {
    connection = null;
    startingPromise = null;
    ready = false;
  });
  return c;
}

// 处理前端工具调用（收口 api/assistant.js handleFrontendToolCall）
async function handleFrontendToolCall(callId, toolName, argsJson) {
  var args = {};
  try { args = argsJson ? JSON.parse(argsJson) : {} } catch (e) { /* ignore */ }
  var extra = frontendToolExtraProvider ? frontendToolExtraProvider() : {};
  try {
    var result = await aiAgentProxy.execute(toolName, args, extra);
    await postToolResult(callId, result);
  } catch (e) {
    await postToolResult(callId, { success: false, error: e.message || '执行失败' });
  }
}

// 结果回传：HTTP POST 优先（绕过 SignalR 单向半开问题），失败降级 SignalR send
async function postToolResult(callId, result) {
  try {
    await db.postJson('/api/assistant/tool-result', {
      CallId: callId,
      ResultJson: JSON.stringify(result)
    });
  } catch (e) {
    if (connection) {
      try { connection.send('FrontendToolResult', callId, JSON.stringify(result)) } catch (e2) { /* ignore */ }
    }
  }
}

// ensureConnected 防重入（提取自 api/assistant.js startingPromise 模式）
async function ensureConnected(scene) {
  // 先确保场景配置已加载（tss_ai_scene → scene-config，失败回落内置默认值）
  await loadSceneConfig();
  if (ready && connection) return connection;
  if (!startingPromise) {
    if (!connection) connection = build();
    startingPromise = connection
      .start()
      .then(async function() {
        // 按 scene 注册前端工具子集（assistant 全 19，form 表单子集，其他空）
        var defs = aiAgentProxy.registerForScene(scene);
        if (defs && defs.length > 0) {
          try { await connection.invoke('RegisterFrontendTools', JSON.stringify(defs)) } catch (e) { console.warn('[AiClient] 注册前端工具失败:', e) }
        }
        ready = true;
      })
      .catch(function(e) {
        connection = null;
        startingPromise = null;
        ready = false;
        throw e;
      });
  }
  await startingPromise;
  startingPromise = null;
  return connection;
}

// ==================== AiClient 主类 ====================

export default class AiClient {
  /**
   * @param {Object} opts
   * @param {string} opts.scene - 'assistant'|'form'|'optimize'|'aidev'|'wizard'|'sfc'
   * @param {function(Object):void} [opts.onBlock] - block 事件回调（text/thinking/tool_call/tool_result/navigate/fill/subtable 等）
   * @param {function(Object):void} [opts.onItem] - 变更项事件回调
   * @param {function(Object):void} [opts.onValidate] - 校验报告回调
   * @param {function(string,string,string,Object):void} [opts.onStep] - 步骤回调 (stepKey, status, toolName, block)
   * @param {function(string):void} [opts.onError] - 错误回调
   * @param {function(Object):void} [opts.onDone] - 完成回调 (完整 done block, 含 usage/changeSetId/warnings 等)
   * @param {function(Object):Object} [opts.getFrontendToolExtra] - 前端工具执行额外上下文提供器
   */
  constructor(opts) {
    opts = opts || {};
    this.scene = opts.scene || 'assistant';
    this.onBlock = opts.onBlock || function() {};
    this.onItem = opts.onItem || function() {};
    this.onValidate = opts.onValidate || function() {};
    this.onStep = opts.onStep || function() {};
    this.onError = opts.onError || function() {};
    this.onDone = opts.onDone || function() {};
    this.getFrontendToolExtra = opts.getFrontendToolExtra || function() { return {} };
    // SignalR 场景：设置共享 extra provider（前端工具回传时使用）
    if (this.isSignalRScene()) {
      frontendToolExtraProvider = this.getFrontendToolExtra;
    }
  }

  isSignalRScene() {
    // 场景配置（tss_ai_scene）优先；配置未加载时回落历史硬编码名单
    var cfg = getSceneSync(this.scene);
    if (cfg && cfg.TRANSPORT) return cfg.TRANSPORT === 'signalr';
    return this.scene === 'assistant' || this.scene === 'form' || this.scene === 'optimize';
  }

  /**
   * 处理收到的原始 block 事件（统一分发，收口 4 份 block 模型逻辑）
   * 参考 store/modules/assistant.js 的 APPEND_BLOCK/TOOL_RESULT 与 workspace 的 appendAssistantText/pushToolCall
   * @param {Object} b - 原始 block 事件
   */
  handleBlock(b) {
    if (!b || !b.type) return;
    switch (b.type) {
      case 'text':
        // 累加到最后一个 text block（打字效果）由调用方处理，这里透传
        this.onBlock({ type: 'text', text: b.text || '' });
        break;
      case 'thinking':
        this.onBlock({ type: 'thinking', text: b.text || '' });
        break;
      case 'tool_call':
        this.onBlock({ type: 'tool_call', tool: b.tool, args: b.args, summary: '' });
        break;
      case 'tool_result':
        // 反向查找最后一个同名 tool_call 更新 summary 由调用方处理，这里透传
        this.onBlock({ type: 'tool_result', tool: b.tool, summary: b.summary });
        break;
      case 'item':
        this.onItem(b.item || b);
        break;
      case 'validate':
        this.onValidate(b.report || b);
        break;
      case 'step':
      case 'step_start':
        // 第 4 个参数透传完整 block，供 wizard 场景取 label/newItemCount 等扩展字段
        this.onStep(b.step || b.stepKey, b.status || (b.type === 'step_start' ? 'start' : 'done'), b.toolName || b.tool, b);
        break;
      case 'navigate':
      case 'fill':
      case 'subtable':
        // 透传给调用方处理
        this.onBlock(b);
        break;
      case 'error':
        this.onError(b.text || b.message || '错误');
        break;
      case 'done':
        // 透传完整 block，供调用方取 changeSetId/warnings/newItemCount 等扩展字段
        this.onDone(b);
        break;
      case 'conversation':
        // 透传给调用方（store 需要从中取 conversationId 维持会话）
        this.onBlock(b);
        break;
      case 'heartbeat':
        // 忽略
        break;
      default:
        this.onBlock(b);
    }
  }

  // ==================== 发送方法 ====================

  /**
   * 场景测试对话（AI 配置中心用）：SignalR AskScene，按 scene 编号加载场景配置
   */
  async sendScene(scene, conversationId, text) {
    blockCallbacks.assistant = (b) => this.handleBlock(b);
    var conn = await ensureConnected('assistant');
    await conn.invoke('AskScene', scene, conversationId || '', text, JSON.stringify(store.state.user.userInfo || {}));
  }

  /**
   * assistant 场景：发送消息（SignalR Ask）
   */
  async send(conversationId, text) {
    blockCallbacks.assistant = (b) => this.handleBlock(b);
    var conn = await ensureConnected('assistant');
    await conn.invoke('Ask', conversationId || '', text, JSON.stringify(store.state.user.userInfo || {}));
  }

  /**
   * form 场景：表单填报（SignalR AskForm）
   */
  async sendForm(moduleCode, text, formData) {
    blockCallbacks.form = (b) => this.handleBlock(b);
    var conn = await ensureConnected('form');
    await conn.invoke('AskForm', moduleCode, text, JSON.stringify(store.state.user.userInfo || {}), JSON.stringify(formData || {}));
  }

  /**
   * aidev 场景：开发助理（SSE，复用 api/aidev.js generateStream）
   */
  async sendDev(sessionId, text) {
    await aidev.generateStream(sessionId, text, (evt) => this.handleBlock(evt));
  }

  /**
   * wizard 场景：分步生成（SSE，复用 api/aidev.js generateStepStream）
   */
  async sendWizardStep(sessionId, step, ctx, text) {
    await aidev.generateStepStream(sessionId, step, ctx, text, (evt) => this.handleBlock(evt));
  }

  /**
   * wizard 场景：一键生成全部步骤（SSE，复用 api/aidev.js generateAllStream）
   */
  async sendWizardAll(sessionId, ctx, text) {
    await aidev.generateAllStream(sessionId, ctx, text, (evt) => this.handleBlock(evt));
  }

  /**
   * sfc 场景：SFC 代码生成（SSE，复用 api/sfc-ai.js generateCodeStream）
   */
  async sendSfc(text, context) {
    await sfcAi.generateCodeStream(text, context || {}, (evt) => this.handleBlock(evt));
  }

  /**
   * optimize 场景：优化提示词（SignalR OptimizePrompt RPC，无 block 监听）
   * @returns {Promise<string>} 优化后的提示词
   */
  async optimizePrompt(content) {
    var conn = await ensureConnected('optimize');
    return conn.invoke('OptimizePrompt', content, JSON.stringify(store.state.user.userInfo || {}));
  }

  /**
   * assistant 场景：图片分析（HTTP POST，绕过 SignalR 32KB 单消息限制）
   * 后端 /api/assistant/analyze-image 返回 {success, text}，识别文本作为 text block 回调
   */
  async analyzeImage(base64, mime) {
    var resp = await db.postJson('/api/assistant/analyze-image', {
      base64Image: base64,
      mimeType: mime || 'image/png'
    });
    var data = resp || {};
    if (data.success) {
      return data.text || '';
    }
    this.onError(data.error || '图片识别失败');
    return '';
  }

  /**
   * 注册前端工具定义（SignalR 场景补充注册，自定义工具）
   */
  registerFrontendTools(defs) {
    if (this.isSignalRScene() && connection && ready) {
      connection.invoke('RegisterFrontendTools', JSON.stringify(defs)).catch(function() {});
    }
  }

  /**
   * 断开连接：仅清理当前 scene 的 block 回调，SignalR 连接保留共享
   */
  async disconnect() {
    if (!this.isSignalRScene()) return;
    if (this.scene === 'assistant') delete blockCallbacks.assistant;
    if (this.scene === 'form') delete blockCallbacks.form;
  }
}
