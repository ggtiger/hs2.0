using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Services.Agent
{
  /// <summary>
  /// IToolRegistry 默认实现（单例）。
  /// 按 setName 聚合多个 IToolExecutor（同一 set 可挂多个执行器，先注册优先），
  /// 提供按场景取定义/合并/找执行器的能力。
  /// 启动时由 Startup 注册各执行器：
  ///   registry.Register("assistant", assistantToolExecutor);  // Backend 工具
  ///   registry.Register("formfill", assistantToolExecutor);   // 填报子集（同执行器，过滤 Kind）
  ///   registry.Register("dev", assistantToolExecutor);        // 开发工具子集
  ///   registry.Register("sfc", sfcAiToolExecutor);            // ReadOnly 工具
  ///   registry.Register("assistant"/"formfill"/"dev"/"sfc", declarativeSqlToolExecutor); // 声明式工具(tss_ai_tool)
  /// </summary>
  public class ToolRegistry : IToolRegistry
  {
    private readonly Dictionary<string, List<IToolExecutor>> _sets = new Dictionary<string, List<IToolExecutor>>();

    public void Register(string setName, IToolExecutor executor)
    {
      if (string.IsNullOrEmpty(setName) || executor == null) return;
      if (!_sets.TryGetValue(setName, out var list))
      {
        list = new List<IToolExecutor>();
        _sets[setName] = list;
      }
      // 同执行器在同 set 只挂一次（builtin 先注册，声明式后注册 → 同名内置优先）
      if (!list.Contains(executor)) list.Add(executor);
    }

    /// <summary>取某工具集的定义（合并 set 内所有执行器，同名先注册优先=内置优先）</summary>
    public List<object> GetDefinitions(string setName, ToolKind? filter = null)
    {
      var merged = new List<object>();
      var names = new HashSet<string>();
      if (string.IsNullOrEmpty(setName) || !_sets.TryGetValue(setName, out var list))
        return merged;
      foreach (var exec in list)
      {
        foreach (var def in exec.GetDefinitionsBySet(setName, filter))
        {
          string name = ExtractName(def);
          if (name != null)
          {
            if (names.Add(name)) merged.Add(def is JObject j ? j : TryToJObject(def));
          }
          else
          {
            merged.Add(def);
          }
        }
      }
      return merged;
    }

    public List<object> GetMergedDefinitions(string[] setNames, ToolKind? filter = null)
    {
      var merged = new List<object>();
      var names = new HashSet<string>();
      if (setNames == null) return merged;
      foreach (var setName in setNames)
      {
        if (string.IsNullOrEmpty(setName) || !_sets.TryGetValue(setName, out var list)) continue;
        foreach (var exec in list)
        {
          foreach (var def in exec.GetDefinitionsBySet(setName, filter))
          {
            string name = ExtractName(def);
            if (name != null)
            {
              if (names.Add(name)) merged.Add(def is JObject j ? j : TryToJObject(def));
            }
            else
            {
              merged.Add(def);
            }
          }
        }
      }
      return merged;
    }

    public bool IsFrontendTool(string toolName)
    {
      if (string.IsNullOrEmpty(toolName)) return false;
      foreach (var list in _sets.Values)
        foreach (var exec in list)
          if (exec.IsFrontendTool(toolName)) return true;
      return false;
    }

    /// <summary>取某工具的执行器（按注册顺序找，builtin 先注册 → 同名内置优先）</summary>
    public IToolExecutor GetExecutor(string toolName)
    {
      if (string.IsNullOrEmpty(toolName)) return null;
      foreach (var list in _sets.Values)
        foreach (var exec in list)
          if (exec.GetToolNames().Contains(toolName)) return exec;
      return null;
    }

    private static string ExtractName(object def)
    {
      try
      {
        var jo = def as JObject;
        if (jo == null) jo = JObject.FromObject(def);
        return jo["function"]?["name"]?.ToString();
      }
      catch { return null; }
    }

    private static JObject TryToJObject(object def)
    {
      try { return JObject.FromObject(def); }
      catch { return new JObject(); }
    }
  }
}
