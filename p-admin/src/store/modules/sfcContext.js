// 全局 SFC 编辑器上下文：edit.vue mounted 时写入编辑器实例 + moduleCode + siblingFiles，
// beforeDestroy 清空。供全局抽屉的开发 agent 使用（sendSfc 取 context + apply-code 桥接）。
// currentFile 不存（send 时现取 editorRef.getAiCurrentFile()，保证最新代码）。
// editTarget: sfc(默认,IDE的vue/js) / csharp / sql / js —— 后端 RMSfcAi 按它选专业提示词
const state = {
  editorRef: null,
  moduleCode: '',
  siblingFiles: [],
  editTarget: '',
  active: false
};

const mutations = {
  SET(s, payload) {
    s.editorRef = payload.editorRef || null;
    s.moduleCode = payload.moduleCode || '';
    s.siblingFiles = payload.siblingFiles || [];
    s.editTarget = payload.editTarget || '';
    s.active = true;
  },
  // 增量更新 moduleCode/siblingFiles/editTarget（不碰 editorRef）
  UPDATE(s, payload) {
    if (payload.moduleCode !== undefined) s.moduleCode = payload.moduleCode;
    if (payload.siblingFiles !== undefined) s.siblingFiles = payload.siblingFiles;
    if (payload.editTarget !== undefined) s.editTarget = payload.editTarget;
  },
  CLEAR(s) {
    s.editorRef = null;
    s.moduleCode = '';
    s.siblingFiles = [];
    s.editTarget = '';
    s.active = false;
  }
};

const getters = {
  // 当前是否有活动的 SFC 编辑器（开发 agent Tab 据此启用/禁用）
  isActive: function(s) {
    return !!s.editorRef && s.active;
  }
};

export default {
  namespaced: true,
  state,
  mutations,
  getters
};
