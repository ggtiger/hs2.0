using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realso.Data.DBAccess;
using Realso.Utils;

namespace Realso.WebAPI.Services
{
  /// <summary>
  /// 内置工具清单同步：把 C# 静态注册的 Tool(...) 定义同步到 tss_ai_tool（EXECUTORTYPE='builtin'），
  /// 同时同步前端工具定义（EXECUTORTYPE='frontend'）。
  /// 目的：配置中心「工具」分区能看到完整工具清单（名称/描述/参数 schema），
  /// builtin/frontend 行只读展示（执行器是 C#/JS 代码，不可配置化）；声明式工具(EXECUTORTYPE=sql/static)仍可自定义。
  /// 启动时调用 SyncAll()，幂等：已存在的 builtin/frontend 行只更新 DESCRIPTION/PARAMS（不覆盖 REMARK/ENABLED）。
  /// TOOLSET 按实际场景写入(assistant/formfill/dev/sfc/frontend)，供 GetBuiltinOverrides 按场景过滤。
  /// </summary>
  public static class BuiltinToolSync
  {
    // 前端工具定义清单（与 aiAgentProxy.js 中注册的工具保持一致）
    private static readonly (string name, string desc, string toolset)[] FrontendTools = new[]
    {
      ("navigate", "跳转到指定页面路由", "assistant"),
      ("fill_form", "批量填充表单字段", "formfill"),
      ("fill_subtable", "批量添加子表行", "formfill"),
      ("get_current_page", "获取当前页面路由和模块信息", "assistant"),
      ("get_form_data", "获取当前表单数据(主表+子表)", "formfill"),
      ("get_user_info", "获取当前登录用户信息", "assistant"),
      ("get_menus", "获取用户菜单树", "assistant"),
      ("show_message", "显示消息提示", "assistant"),
      ("close_dialog", "关闭 AI 助手面板", "assistant"),
      ("open_form", "按 ID 加载记录到表单", "formfill"),
      ("set_form_field", "设置主表字段值", "formfill"),
      ("get_form_field", "获取主表字段值", "formfill"),
      ("save_form", "保存当前表单", "formfill"),
      ("add_subtable_row", "新增一行子表数据", "formfill"),
      ("delete_subtable_row", "删除指定子表行", "formfill"),
      ("update_subtable_row", "修改指定子表行", "formfill"),
      ("clear_subtable", "清空子表所有行", "formfill"),
      ("get_subtable_data", "获取子表数据", "formfill"),
      ("list_subtables", "列出所有子表路径", "formfill")
    };
    public static void SyncAll()
    {
      try
      {
        var allDefs = new System.Collections.Generic.List<(string toolset, object def)>();
        foreach (var d in AssistantToolExecutor.GetToolDefinitions()) allDefs.Add(("assistant", d));
        foreach (var d in AssistantToolExecutor.GetDevToolDefinitions()) allDefs.Add(("dev", d));
        foreach (var d in SfcAiToolExecutor.GetToolDefinitions()) allDefs.Add(("sfc", d));

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          foreach (var item in allDefs)
          {
            try
            {
              // 工具定义是匿名对象 {type:"function", function:{name, description, parameters}}
              var jo = JObject.FromObject(item.def);
              var fn = jo["function"];
              if (fn == null) continue;
              string name = fn["name"]?.ToString();
              string desc = fn["description"]?.ToString() ?? "";
              string paramsJson = fn["parameters"] != null ? fn["parameters"].ToString(Formatting.None) : null;
              if (string.IsNullOrEmpty(name)) continue;

              var exist = helper.QueryFirstOrDefault<dynamic>(
                "SELECT ID, EXECUTORTYPE FROM tss_ai_tool WHERE TOOLNAME=@n AND TOOLSET=@ts LIMIT 1", new { n = name, ts = item.toolset });
              if (exist == null)
              {
                helper.Execute(
                  @"INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, MAXROWS, ENABLED, REMARK, ISDELETED)
                    VALUES (@id, @n, @ts, @d, @p, 'builtin', 200, 1, '内置工具(C#执行器, 定义与代码同步, 只读)', 0)",
                  new { id = "bt_" + name, n = name, ts = item.toolset, d = desc, p = paramsJson });
              }
              else if ((string)exist.EXECUTORTYPE == "builtin")
              {
                // 已存在的 builtin 行同步描述/参数/工具集(代码升级为权威, 不动 REMARK/ENABLED)
                helper.Execute(
                  "UPDATE tss_ai_tool SET DESCRIPTION=@d, PARAMS=@p, TOOLSET=@ts WHERE ID=@id",
                  new { id = (string)exist.ID, d = desc, p = paramsJson, ts = item.toolset });
              }
              // 同名但是用户自定义的(sql/static)不动——用户覆盖优先
            }
            catch { /* 单个工具同步失败不阻塞其他 */ }
          }

          // 同步前端工具定义
          foreach (var ft in FrontendTools)
          {
            try
            {
              var exist = helper.QueryFirstOrDefault<dynamic>(
                "SELECT ID, EXECUTORTYPE FROM tss_ai_tool WHERE TOOLNAME=@n AND TOOLSET=@ts LIMIT 1", new { n = ft.name, ts = ft.toolset });
              if (exist == null)
              {
                helper.Execute(
                  @"INSERT INTO tss_ai_tool (ID, TOOLNAME, TOOLSET, DESCRIPTION, PARAMS, EXECUTORTYPE, MAXROWS, ENABLED, REMARK, ISDELETED)
                    VALUES (@id, @n, @ts, @d, NULL, 'frontend', 0, 1, '前端工具(JS执行器, 前端注册)', 0)",
                  new { id = "ft_" + ft.name, n = ft.name, ts = ft.toolset, d = ft.desc });
              }
              else if ((string)exist.EXECUTORTYPE == "frontend")
              {
                helper.Execute(
                  "UPDATE tss_ai_tool SET DESCRIPTION=@d, TOOLSET=@ts WHERE ID=@id",
                  new { id = (string)exist.ID, d = ft.desc, ts = ft.toolset });
              }
            }
            catch { /* 单个工具同步失败不阻塞其他 */ }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("BuiltinToolSync 同步失败（已跳过）: " + ex.Message);
      }
    }
  }
}
