// 全局表单上下文：rs-form-edit mounted 时写入当前表单实例 + moduleCode + storeName，
// beforeDestroy 清空。供全局抽屉的 AI 填报 agent 通过 getFrontendToolExtra 读取，
// 让前端工具（fill_form/set_form_field/save_form 等）能操作当前表单。
// 解决全局抽屉（App 根）拿不到深层 rs-form-edit 实例的问题（provide/inject 跨不过去）。
const state = {
  rsFormEdit: null,
  moduleCode: null,
  storeName: null,
  active: false
};

const mutations = {
  SET(s, payload) {
    s.rsFormEdit = payload.rsFormEdit || null;
    s.moduleCode = payload.moduleCode || null;
    s.storeName = payload.storeName || null;
    s.active = true;
  },
  CLEAR(s) {
    s.rsFormEdit = null;
    s.moduleCode = null;
    s.storeName = null;
    s.active = false;
  }
};

const getters = {
  // 当前是否有活动的表单（AI 填报 Tab 据此启用/禁用）
  isActive: function(s) {
    return !!s.rsFormEdit && s.active;
  }
};

export default {
  namespaced: true,
  state,
  mutations,
  getters
};
