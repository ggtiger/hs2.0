import store from '@/store';
import AiClient from '@/utils/ai/AiClient';

// 助理全局 Vuex 模块（namespaced，静态注册到 store/index.js）。
// 一个入口、三个智能体：currentAgent='assistant'(通用) | 'form'(填报) | 'sfc'(开发)。
// 三个智能体各自维护会话，切换不互清；form 模块变清 formMessages，sfc 离开编辑器清 sfcMessages。
// form 表单上下文从 formContext 取，sfc 编辑器上下文从 sfcContext 取。

// 通用助理 onBlock
function onAssistantBlock(b) {
  if (!b || !b.type) return;
  switch (b.type) {
    case 'conversation':
      store.commit('assistant/SET_CONVERSATION', b.conversationId);
      break;
    case 'tool_call':
      store.commit('assistant/TOOL_CALL', b);
      break;
    case 'tool_result':
      store.commit('assistant/TOOL_RESULT', b);
      break;
    default:
      store.commit('assistant/APPEND_BLOCK', b);
  }
}

// AI 填报 onBlock：fill/subtable 调当前表单的 applyFill/onSubTable
function onFormBlock(b) {
  if (!b || !b.type) return;
  var rsFormEdit = store.state.formContext.rsFormEdit;
  switch (b.type) {
    case 'fill':
      if (rsFormEdit && rsFormEdit.applyFill) rsFormEdit.applyFill(b.fields);
      store.commit('assistant/APPEND_BLOCK', { type: 'text', text: '✓ 已填充字段' });
      break;
    case 'subtable':
      if (rsFormEdit && rsFormEdit.onSubTable) rsFormEdit.onSubTable({ path: b.path, rows: b.rows });
      store.commit('assistant/APPEND_BLOCK', { type: 'text', text: '✓ 已添加子表行' });
      break;
    case 'tool_call':
      store.commit('assistant/TOOL_CALL', b);
      break;
    case 'tool_result':
      store.commit('assistant/TOOL_RESULT', b);
      break;
    default:
      store.commit('assistant/APPEND_BLOCK', b);
  }
}

// SFC 开发 onBlock：text/tool_call/tool_result 按 type commit
function onSfcBlock(b) {
  if (!b || !b.type) return;
  switch (b.type) {
    case 'tool_call':
      store.commit('assistant/TOOL_CALL', b);
      break;
    case 'tool_result':
      store.commit('assistant/TOOL_RESULT', b);
      break;
    default:
      store.commit('assistant/APPEND_BLOCK', b);
  }
}

// SFC done：提取 SEARCH/REPLACE 为独立 search_replace block（搬自 ai-chat-panel）
function onSfcDone() {
  var msgs = store.state.assistant.sfcMessages;
  if (msgs.length > 0) {
    extractSearchReplace(msgs[msgs.length - 1]);
  }
  store.commit('assistant/SET_LOADING', false);
}

function parseSearchReplace(text) {
  var normalized = String(text || '').replace(/```(?:vue|js|javascript)?\s*\n(<<<<<<< SEARCH[\s\S]*?>>>>>>> REPLACE)\n```/g, '$1');
  var srRe = /<<<<<<< SEARCH\s*\n([\s\S]*?)\n=======\s*\n([\s\S]*?)\n>>>>>>> REPLACE/g;
  var matches = [];
  var m;
  while ((m = srRe.exec(normalized)) !== null) {
    matches.push({ search: m[1], replace: m[2] });
  }
  return matches;
}

function extractSearchReplace(msg) {
  if (!msg || !msg.blocks) return;
  for (var i = msg.blocks.length - 1; i >= 0; i--) {
    var blk = msg.blocks[i];
    if (blk.type === 'text') {
      var srMatches = parseSearchReplace(blk.text);
      if (srMatches.length > 0) {
        blk.text = blk.text
          .replace(/```(?:vue|js|javascript)?\s*\n<<<<<<< SEARCH\s*\n[\s\S]*?\n>>>>>>> REPLACE\s*\n```/g, '')
          .replace(/<<<<<<< SEARCH\s*\n[\s\S]*?>>>>>>> REPLACE/g, '')
          .trim();
        msg.blocks.push({
          type: 'search_replace',
          searchReplace: srMatches,
          code: '',
          language: 'vue',
          fileName: ''
        });
      }
      break;
    }
  }
}

function onError(msg) {
  store.commit('assistant/APPEND_BLOCK', { type: 'text', text: '⚠️ ' + (msg || '错误') });
}

// 三个 AiClient 实例（assistant/form 共享 SignalR，sfc 走 SSE）
var aiClientAssistant = new AiClient({
  scene: 'assistant',
  onBlock: onAssistantBlock,
  onError: onError,
  onDone: function() { store.commit('assistant/SET_LOADING', false) }
});

var aiClientForm = new AiClient({
  scene: 'form',
  onBlock: onFormBlock,
  onError: onError,
  onDone: function() { store.commit('assistant/SET_LOADING', false) },
  getFrontendToolExtra: function() {
    var fc = store.state.formContext;
    return { moduleCode: fc.moduleCode, storeName: fc.storeName, formEdit: fc.rsFormEdit };
  }
});

var aiClientSfc = new AiClient({
  scene: 'sfc',
  onBlock: onSfcBlock,
  onError: onError,
  onDone: onSfcDone
});

const state = {
  visible: false,
  currentAgent: 'assistant', // 'assistant' | 'form' | 'sfc'
  conversationId: '',
  assistantMessages: [],
  formMessages: [],
  sfcMessages: [],
  isLoading: false
};

function currentMsgs(s) {
  if (s.currentAgent === 'form') return s.formMessages;
  if (s.currentAgent === 'sfc') return s.sfcMessages;
  return s.assistantMessages;
}

const mutations = {
  SET_VISIBLE(s, v) {
    s.visible = v;
  },
  SET_AGENT(s, agent) {
    s.currentAgent = agent;
  },
  SET_CONVERSATION(s, id) {
    s.conversationId = id;
  },
  PUSH_MESSAGE(s, m) {
    currentMsgs(s).push(m);
  },
  APPEND_BLOCK(s, block) {
    var arr = currentMsgs(s);
    var last = arr[arr.length - 1];
    if (!last) return;
    if (
      block.type === 'text' &&
      last.blocks.length &&
      last.blocks[last.blocks.length - 1].type === 'text'
    ) {
      last.blocks[last.blocks.length - 1].text += block.text;
    } else {
      last.blocks.push(block);
    }
  },
  TOOL_CALL(s, b) {
    var arr = currentMsgs(s);
    var last = arr[arr.length - 1];
    if (!last) return;
    last.blocks.push({ type: 'tool_call', tool: b.tool, args: b.args, summary: '' });
  },
  TOOL_RESULT(s, b) {
    var arr = currentMsgs(s);
    var last = arr[arr.length - 1];
    if (!last) return;
    for (var i = last.blocks.length - 1; i >= 0; i--) {
      if (last.blocks[i].type === 'tool_call' && last.blocks[i].tool === b.tool) {
        last.blocks[i].summary = b.summary;
        break;
      }
    }
  },
  UPDATE_LAST_TEXT(s, text) {
    var arr = currentMsgs(s);
    var last = arr[arr.length - 1];
    if (last && last.role === 'assistant' && last.blocks.length && last.blocks[last.blocks.length - 1].type === 'text') {
      last.blocks[last.blocks.length - 1].text = text;
    }
  },
  RESET_ASSISTANT(s) {
    s.assistantMessages = [];
    s.conversationId = '';
  },
  RESET_FORM(s) {
    s.formMessages = [];
  },
  RESET_SFC(s) {
    s.sfcMessages = [];
  },
  SET_LOADING(s, v) {
    s.isLoading = v;
  }
};

const actions = {
  toggle({ commit, state, dispatch }) {
    var willShow = !state.visible;
    commit('SET_VISIBLE', willShow);
    if (willShow && state.currentAgent === 'assistant' && state.assistantMessages.length === 0) {
      setTimeout(function() { dispatch('send', '你能做什么？') }, 100);
    }
  },
  setAgent({ commit }, agent) {
    commit('SET_AGENT', agent);
  },
  openWithAgent({ commit, dispatch }, agent) {
    commit('SET_VISIBLE', true);
    dispatch('setAgent', agent);
  },
  async analyzeImage({ state }, { base64, mime }) {
    var aiClient = state.currentAgent === 'form' ? aiClientForm : aiClientAssistant;
    return aiClient.analyzeImage(base64, mime);
  },
  async send({ commit, state }, text) {
    commit('SET_LOADING', true);
    commit('PUSH_MESSAGE', { role: 'user', blocks: [{ type: 'text', text: text }] });
    commit('PUSH_MESSAGE', { role: 'assistant', blocks: [] });
    try {
      if (state.currentAgent === 'form') {
        var fc = store.state.formContext;
        if (!fc.rsFormEdit) {
          commit('APPEND_BLOCK', { type: 'text', text: '⚠️ 当前不在表单页面，无法使用 AI 填报' });
          return;
        }
        var formData = fc.rsFormEdit.allFormData || {};
        await aiClientForm.sendForm(fc.moduleCode, text, formData);
      } else if (state.currentAgent === 'sfc') {
        var sc = store.state.sfcContext;
        if (!sc.editorRef) {
          commit('APPEND_BLOCK', { type: 'text', text: '⚠️ 当前不在 SFC 编辑页面' });
          return;
        }
        var ctx = {
          currentFile: sc.editorRef.getAiCurrentFile ? sc.editorRef.getAiCurrentFile() : null,
          siblingFiles: sc.siblingFiles,
          moduleCode: sc.moduleCode,
          // 资产类型（csharp/sql/js），后端按它选专业提示词；空=默认 SFC 提示词
          editTarget: sc.editTarget || ''
        };
        await aiClientSfc.sendSfc(text, ctx);
      } else {
        await aiClientAssistant.send(state.conversationId, text);
      }
    } catch (e) {
      console.error('[assistant] send 失败:', e);
      commit('APPEND_BLOCK', { type: 'text', text: '⚠️ ' + (e && e.message ? e.message : '网络异常，请重试') });
    } finally {
      commit('SET_LOADING', false);
    }
  }
};

export default {
  namespaced: true,
  state,
  mutations,
  actions
};
