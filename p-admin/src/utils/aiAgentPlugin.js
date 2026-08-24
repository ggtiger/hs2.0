/**
 * AI Agent Vue Plugin - Vue插件封装
 *
 * 使用方式：
 * import AIAgentPlugin from '@/utils/aiAgentPlugin';
 * Vue.use(AIAgentPlugin, { store, router });
 *
 * 在组件中：
 * this.$aiAgent.execute('navigate', { path: '/r01/m05' });
 * this.$aiAgent.createClient({ moduleCode: 'LI_M02' });
 */

import Vue from 'vue';
import { aiAgentProxy, ITool } from './aiAgentProxy';
import AiClient from './ai/AiClient';

/**
 * AI Agent Vue Plugin
 */
const AIAgentPlugin = {
  install(Vue, options = {}) {
    // 初始化代理层
    aiAgentProxy.init({
      store: options.store,
      router: options.router
    });

    // 在Vue原型上添加$aiAgent
    Vue.prototype.$aiAgent = {
      /**
       * 执行前端工具
       * @param {string} name - 工具名
       * @param {Object} args - 参数
       * @param {Object} extra - 额外上下文（如 formEdit）
       */
      execute: async (name, args, extra) => {
        return await aiAgentProxy.execute(name, args, extra);
      },

      /**
       * 获取所有工具定义（用于LLM）
       */
      getToolDefinitions: () => {
        return aiAgentProxy.getToolDefinitions();
      },

      /**
       * 获取前端工具定义
       */
      getFrontendToolDefinitions: () => {
        return aiAgentProxy.getFrontendToolDefinitions();
      },

      /**
       * 注册自定义工具
       * @param {string} name - 工具名
       * @param {ITool} tool - 工具实例
       */
      registerTool: (name, tool) => {
        aiAgentProxy.registerTool(name, tool);
      },

      /**
       * 判断是否前端工具
       */
      isFrontendTool: (name) => {
        return aiAgentProxy.isFrontendTool(name);
      },

      /**
       * 创建统一 AI 客户端（按 scene 选传输：assistant/form/optimize 用 SignalR，aidev/wizard/sfc 用 SSE）
       * @param {Object} opts - { scene, onBlock, onItem, onValidate, onStep, onError, onDone, getFrontendToolExtra }
       * @returns {AiClient}
       */
      createClient: (opts) => {
        return new AiClient(opts);
      },

      /**
       * 代理层实例
       */
      proxy: aiAgentProxy
    };

    console.log('[AIAgentPlugin] Vue插件已安装');
  }
};

export default AIAgentPlugin;

// 导出核心类和工具基类，供自定义工具继承
export { aiAgentProxy, AiClient, ITool };
