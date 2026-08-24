using System;
using System.Collections.Generic;
using Realso.Data.DBAccess;
using Realso.Utils;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// AI 场景配置服务（tss_ai_scene）：前端 AiClient/aiAgentProxy 的场景定义来源，
  /// 替代三处硬编码镜像（AiClient.isSignalRScene / registerForScene / 后端 setName）。
  /// 60s 缓存；表不存在或为空时回落代码内置默认值（与历史硬编码行为一致，保证未迁移环境可用）。
  /// </summary>
  public static class SceneConfigService
  {
    public class AiScene
    {
      public string SCENECODE;
      public string SCENENAME;
      public string TRANSPORT;      // signalr / sse
      public string ENDPOINT;       // SSE: 完整路由; signalr: Hub 方法名
      public string TOOLSET;        // assistant/formfill/dev/sfc
      public string PROMPTKEY;
      public string MODELID;        // 指定 LLM 模型 ID (TBS_LLM_CONFIG.ID)，NULL=用全局默认
      public string PARAMS;         // Agent 参数 JSON (maxSteps/temperature 等)，NULL=用代码默认值
      public int DAILYQUOTA;       // 每日 Token 上限（0=不限）
      public string FRONTENDTOOLS;  // all/none 或逗号分隔工具名
      public string CONTEXTSOURCE;  // none/formContext/sfcContext
      public int SORTNO;
    }

    private static List<AiScene> _cache;
    private static DateTime _loadedAt = DateTime.MinValue;
    private static readonly object _lock = new object();

    /// <summary>全部启用场景（前端 scene-config 端点用）</summary>
    public static List<AiScene> GetAll()
    {
      if (_cache != null && (DateTime.Now - _loadedAt).TotalSeconds < 60) return _cache;
      lock (_lock)
      {
        if (_cache != null && (DateTime.Now - _loadedAt).TotalSeconds < 60) return _cache;
        List<AiScene> list = null;
        try
        {
          using (var helper = DB.GetDBHelper())
          {
            var rows = helper.Query<dynamic>(
              "SELECT SCENECODE, SCENENAME, TRANSPORT, ENDPOINT, TOOLSET, PROMPTKEY, MODELID, PARAMS, DAILYQUOTA, FRONTENDTOOLS, CONTEXTSOURCE, SORTNO FROM tss_ai_scene WHERE ENABLED=1 AND ISDELETED=0 ORDER BY SORTNO, SCENECODE");
            list = new List<AiScene>();
            foreach (var r in rows)
            {
              list.Add(new AiScene
              {
                SCENECODE = (string)r.SCENECODE,
                SCENENAME = (string)r.SCENENAME,
                TRANSPORT = (string)r.TRANSPORT,
                ENDPOINT = (string)r.ENDPOINT,
                TOOLSET = (string)r.TOOLSET,
                PROMPTKEY = (string)r.PROMPTKEY,
                MODELID = r.MODELID == null ? null : (string)r.MODELID,
                PARAMS = r.PARAMS == null ? null : (string)r.PARAMS,
                DAILYQUOTA = r.DAILYQUOTA == null ? 0 : (int)r.DAILYQUOTA,
                FRONTENDTOOLS = (string)r.FRONTENDTOOLS,
                CONTEXTSOURCE = (string)r.CONTEXTSOURCE,
                SORTNO = r.SORTNO == null ? 0 : (int)r.SORTNO
              });
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Warn("SceneConfigService 读取 tss_ai_scene 失败（回落内置默认值）: " + ex.Message);
          list = null;
        }
        if (list == null || list.Count == 0) list = Defaults();
        _cache = list;
        _loadedAt = DateTime.Now;
        return _cache;
      }
    }

    /// <summary>按场景编码取配置（取不到返回 null）</summary>
    public static AiScene GetScene(string sceneCode)
    {
      if (string.IsNullOrEmpty(sceneCode)) return null;
      return GetAll().Find(s => s.SCENECODE == sceneCode);
    }

    /// <summary>手动失效缓存（场景管理页保存后调用）</summary>
    public static void Invalidate()
    {
      lock (_lock) { _cache = null; }
    }

    /// <summary>场景每日配额检查：超限返回错误消息，未超限返回 null</summary>
    public static string CheckDailyQuota(AiScene scene, string operationType)
    {
      if (scene == null || scene.DAILYQUOTA <= 0) return null;
      try
      {
        using (var helper = DB.GetDBHelper())
        {
          var today = DateTime.Now.ToString("yyyy-MM-dd");
          var used = helper.QueryFirstOrDefault<long>(
            "SELECT COALESCE(SUM(TOTALTOKENS),0) FROM TBS_LLM_USAGE WHERE OPERATIONTYPE=@opt AND REQUESTTIME>=@today AND ISDELETED=0",
            new { opt = operationType, today = today });
          if (used >= scene.DAILYQUOTA)
            return $"场景 [{scene.SCENECODE}] 今日已用 {used} tokens，超过配额 {scene.DAILYQUOTA}，请明天再试或联系管理员调整配额";
        }
      }
      catch { /* 配额查询失败不阻塞请求 */ }
      return null;
    }

    /// <summary>内置默认值：与历史硬编码行为一一对应（未迁移环境兜底）</summary>
    private static List<AiScene> Defaults()
    {
      return new List<AiScene>
      {
        new AiScene { SCENECODE = "assistant", SCENENAME = "通用助理", TRANSPORT = "signalr", ENDPOINT = "Ask", TOOLSET = "assistant", FRONTENDTOOLS = "all", CONTEXTSOURCE = "none", SORTNO = 1 },
        new AiScene { SCENECODE = "form", SCENENAME = "表单填报", TRANSPORT = "signalr", ENDPOINT = "AskForm", TOOLSET = "formfill", FRONTENDTOOLS = "fill_form,fill_subtable,get_form_data,get_form_field,set_form_field,save_form,add_subtable_row,delete_subtable_row,update_subtable_row,clear_subtable,get_subtable_data,list_subtables", CONTEXTSOURCE = "formContext", SORTNO = 2 },
        new AiScene { SCENECODE = "optimize", SCENENAME = "提示词优化", TRANSPORT = "signalr", ENDPOINT = "OptimizePrompt", TOOLSET = null, FRONTENDTOOLS = "none", CONTEXTSOURCE = "none", SORTNO = 3 },
        new AiScene { SCENECODE = "aidev", SCENENAME = "AI开发助理", TRANSPORT = "sse", ENDPOINT = "/api/RMAIDev/generate-stream", TOOLSET = "dev", FRONTENDTOOLS = "none", CONTEXTSOURCE = "none", SORTNO = 4 },
        new AiScene { SCENECODE = "wizard", SCENENAME = "模块向导", TRANSPORT = "sse", ENDPOINT = "/api/RMAIDev/generate-step-stream", TOOLSET = "dev", FRONTENDTOOLS = "none", CONTEXTSOURCE = "none", SORTNO = 5 },
        new AiScene { SCENECODE = "sfc", SCENENAME = "SFC代码助手", TRANSPORT = "sse", ENDPOINT = "/api/RMSfcAi/generate-code", TOOLSET = "sfc", FRONTENDTOOLS = "none", CONTEXTSOURCE = "sfcContext", SORTNO = 6 }
      };
    }
  }
}
