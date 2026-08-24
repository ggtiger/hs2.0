// AI 场景配置加载器（tss_ai_scene → /api/assistant/scene-config）
// 替代 AiClient.isSignalRScene / aiAgentProxy.registerForScene 的硬编码。
// 一次性加载 + 内存缓存；加载失败回落内置默认值（与历史硬编码行为一致）。
import db from '@/api/db';

let _scenes = null;
let _loading = null;

// 内置默认值：与后端 SceneConfigService.Defaults 一致（配置拉取失败时兜底）
function defaults() {
  return {
    assistant: { SCENECODE: 'assistant', TRANSPORT: 'signalr', ENDPOINT: 'Ask', TOOLSET: 'assistant', FRONTENDTOOLS: 'all', CONTEXTSOURCE: 'none' },
    form: { SCENECODE: 'form', TRANSPORT: 'signalr', ENDPOINT: 'AskForm', TOOLSET: 'formfill', FRONTENDTOOLS: 'fill_form,fill_subtable,get_form_data,get_form_field,set_form_field,save_form,add_subtable_row,delete_subtable_row,update_subtable_row,clear_subtable,get_subtable_data,list_subtables', CONTEXTSOURCE: 'formContext' },
    optimize: { SCENECODE: 'optimize', TRANSPORT: 'signalr', ENDPOINT: 'OptimizePrompt', TOOLSET: null, FRONTENDTOOLS: 'none', CONTEXTSOURCE: 'none' },
    aidev: { SCENECODE: 'aidev', TRANSPORT: 'sse', ENDPOINT: '/api/RMAIDev/generate-stream', TOOLSET: 'dev', FRONTENDTOOLS: 'none', CONTEXTSOURCE: 'none' },
    wizard: { SCENECODE: 'wizard', TRANSPORT: 'sse', ENDPOINT: '/api/RMAIDev/generate-step-stream', TOOLSET: 'dev', FRONTENDTOOLS: 'none', CONTEXTSOURCE: 'none' },
    sfc: { SCENECODE: 'sfc', TRANSPORT: 'sse', ENDPOINT: '/api/RMSfcAi/generate-code', TOOLSET: 'sfc', FRONTENDTOOLS: 'none', CONTEXTSOURCE: 'sfcContext' }
  };
}

// 加载场景配置（幂等；失败回落默认值）
export function loadSceneConfig() {
  if (_scenes) return Promise.resolve(_scenes);
  if (_loading) return _loading;
  _loading = db.postData({ api: '/api/assistant/scene-config', params: {} })
    .then(function(ret) {
      var map = {};
      (ret || []).forEach(function(s) {
        if (s && s.SCENECODE) map[s.SCENECODE] = s;
      });
      _scenes = Object.keys(map).length > 0 ? map : defaults();
      return _scenes;
    })
    .catch(function() {
      _scenes = defaults();
      return _scenes;
    });
  return _loading;
}

// 同步取场景配置（未加载完成返回 null，调用方需有硬编码回落）
export function getSceneSync(sceneCode) {
  return _scenes && sceneCode ? (_scenes[sceneCode] || null) : null;
}

// 手动失效（场景管理页保存后调用，下次使用重新拉取）
export function invalidateSceneConfig() {
  _scenes = null;
  _loading = null;
}
