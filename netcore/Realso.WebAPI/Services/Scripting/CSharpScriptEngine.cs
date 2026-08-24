using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using Realso.Data.DBAccess;
using Realso.Data.ORM;

namespace Realso.WebAPI.Services.Scripting
{
  /// <summary>
  /// C# 脚本引擎（Roslyn Scripting，netcoreapp2.2 兼容的 2.8 版）。
  /// 源码存 tss_api_script，运行时编译为内存程序集并缓存；
  /// 每次调用先查 VERSION（索引轻查询），版本变化才重新编译 —— 实现"在线编辑、保存即生效"。
  /// 与前端 sfc-loader 同一模式（缓存 + 失效重编译）。
  /// </summary>
  public static class CSharpScriptEngine
  {
    private class CacheEntry
    {
      public string SourceHash;  // 源码 MD5（热更新检测：不依赖 VERSION 人工维护，任何保存都生效）
      public Script<object> Script;
    }

    private static readonly ConcurrentDictionary<string, CacheEntry> _cache =
      new ConcurrentDictionary<string, CacheEntry>();

    private static List<MetadataReference> _references;

    /// <summary>
    /// 引用清单：当前进程已加载的全部程序集（脚本可用 app 内一切类型：Dapper/MySql/Newtonsoft/Realso.*）。
    /// 进程内执行无法真沙箱——脚本按"管理员级可信代码"对待（编辑权限点 + AI 变更包确认双重把关）。
    /// </summary>
    private static List<MetadataReference> GetReferences()
    {
      if (_references != null) return _references;
      // 先强制加载核心程序集（.NET 程序集懒加载：未触碰的类型所在程序集不在 GetAssemblies 里，
      // 冒烟测试曾因此报 CS0234 'Realso.Data' does not exist）
      ForceLoadAssemblies();
      var list = new List<MetadataReference>();
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
        try { list.Add(MetadataReference.CreateFromFile(asm.Location)); }
        catch { /* 个别程序集加载失败可忽略 */ }
      }
      _references = list;
      return _references;
    }

    /// <summary>触碰核心类型强制其程序集加载进 AppDomain</summary>
    private static void ForceLoadAssemblies()
    {
      var forceTypes = new[]
      {
        typeof(DBHelper),                          // Realso.Data.DBAccess
        typeof(SQLManage),                         // Realso.Data.ORM
        typeof(Realso.Data.ORM.Core.DataView),     // Realso.Data.ORM.Core
        typeof(Realso.Utils.VelocityHelper),       // Realso.Utils
        typeof(Realso.Core.Models.ResponseModel),  // Realso.Core
        typeof(ScriptGlobals),                     // Realso.WebAPI
        typeof(Dapper.SqlMapper),                  // Dapper
        typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo), // Microsoft.CSharp(动态绑定, row.FIELD 取值需要)
        typeof(MySql.Data.MySqlClient.MySqlConnection), // MySql.Data
        typeof(Newtonsoft.Json.JsonConvert),       // Newtonsoft.Json
        typeof(System.Data.IDbConnection)          // System.Data
      };
      foreach (var t in forceTypes)
      {
        try { var _ = t.Assembly.Location; } catch { }
      }
    }

    private static ScriptOptions BuildOptions()
    {
      return ScriptOptions.Default
        .WithReferences(GetReferences())
        .WithImports(
          "System", "System.Linq", "System.Collections", "System.Collections.Generic",
          "System.Data", "System.Text",
          "Realso.Data.DBAccess", "Realso.Data.ORM", "Realso.Utils",
          "Realso.WebAPI.Services.Scripting");
    }

    /// <summary>仅编译检查（编辑页"编译检查"按钮用）：返回诊断列表，无错误返回空列表</summary>
    public static List<string> CheckSyntax(string source)
    {
      var errors = new List<string>();
      try
      {
        var script = CSharpScript.Create<object>(source ?? "", BuildOptions(), typeof(ScriptGlobals));
        var diags = script.Compile();
        foreach (var d in diags)
        {
          if (d.Severity == DiagnosticSeverity.Error) errors.Add(d.ToString());
        }
      }
      catch (Exception ex)
      {
        errors.Add(ex.Message);
      }
      return errors;
    }

    /// <summary>
    /// 执行脚本。返回 true=成功（结果由脚本经 Response 设置），false=失败（error 已填）。
    /// </summary>
    public static bool Execute(string scriptCode, ScriptGlobals globals, out string error)
    {
      error = null;
      // 1. 查源码（轻量查询，热更新检测依据：源码哈希，保存即生效）
      // 统一代码资产表 tss_code_asset（ASSETTYPE='csharp'，原 tss_api_script 已并入）
      string source;
      using (var helper = DB.GetDBHelper())
      {
        source = helper.QueryFirstOrDefault<string>(
          "SELECT SOURCECODE FROM tss_code_asset WHERE ASSETTYPE='csharp' AND CODE=@sc AND ISDELETED=0 LIMIT 1",
          new { sc = scriptCode });
        if (source == null)
        {
          error = "脚本 " + scriptCode + " 不存在（tss_code_asset 查无 ASSETTYPE=csharp CODE=" + scriptCode + "）";
          return false;
        }
      }
      if (string.IsNullOrEmpty(source))
      {
        error = "脚本 " + scriptCode + " 的 SOURCECODE 为空";
        return false;
      }
      return CompileAndRun(scriptCode, source, globals, out error);
    }

    /// <summary>
    /// 执行指定源码（接口测试用：编辑器未保存的内容也可直接运行）。
    /// 缓存键用源码哈希（改动即重编译），不依赖 tss_code_asset 行。
    /// </summary>
    public static bool ExecuteSource(string source, ScriptGlobals globals, out string error)
    {
      error = null;
      if (string.IsNullOrWhiteSpace(source))
      {
        error = "脚本源码为空";
        return false;
      }
      return CompileAndRun("src_" + ComputeHash(source), source, globals, out error);
    }

    /// <summary>编译（带缓存）并执行脚本</summary>
    private static bool CompileAndRun(string cacheKey, string source, ScriptGlobals globals, out string error)
    {
      error = null;
      // 2. 缓存命中检查，源码哈希不一致才重编译
      string sourceHash = ComputeHash(source);
      CacheEntry entry;
      if (!_cache.TryGetValue(cacheKey, out entry) || entry.SourceHash != sourceHash)
      {
        try
        {
          var script = CSharpScript.Create<object>(source, BuildOptions(), typeof(ScriptGlobals));
          var diags = script.Compile();
          var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).Take(3).ToList();
          if (errors.Count > 0)
          {
            error = "脚本 " + cacheKey + " 编译失败: " + string.Join("；", errors.Select(e => e.ToString()));
            return false;
          }
          entry = new CacheEntry { SourceHash = sourceHash, Script = script };
          _cache[cacheKey] = entry;
        }
        catch (Exception ex)
        {
          error = "脚本 " + cacheKey + " 编译异常: " + ex.Message;
          return false;
        }
      }

      // 3. 执行（脚本异常经 InnerException 透出）
      try
      {
        entry.Script.RunAsync(globals).GetAwaiter().GetResult();
        return true;
      }
      catch (CompilationErrorException cex)
      {
        error = "脚本 " + cacheKey + " 编译错误: " + cex.Message;
        return false;
      }
      catch (Exception ex)
      {
        error = "脚本 " + cacheKey + " 执行异常: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
        return false;
      }
    }

    /// <summary>失效指定脚本的编译缓存（编辑保存后调用；源码哈希变化也会自动重编译，双保险）</summary>
    public static void Invalidate(string scriptCode)
    {
      CacheEntry removed;
      _cache.TryRemove(scriptCode, out removed);
    }

    private static string ComputeHash(string s)
    {
      using (var md5 = System.Security.Cryptography.MD5.Create())
      {
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        var sb = new System.Text.StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
      }
    }
  }
}
