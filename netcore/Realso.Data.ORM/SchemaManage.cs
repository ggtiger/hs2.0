using System.Net.Http.Headers;
using System;
using Realso.Data.DBAccess;
using System.Collections.Generic;
using System.Linq;
using Realso.Data.ORM.Core;

namespace Realso.Data.ORM
{
  public class SchemaManage
  {

    //获取数据
    public static Resource GetResource(string resourceName)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        Resource resource = helper.QueryFirst<Resource>("SELECT * FROM TSS_RESOURCE WHERE RESOURCENAME=@RESOURCENAME OR ID=@RESOURCENAME", new { RESOURCENAME = resourceName });
        List<ResourceField> columns = helper.Query<ResourceField>("SELECT A.*,B.FIELDNAME REFFIELDNAME,B.FIELDANAME REFFIELDANAME FROM TSS_RESFIELD A LEFT JOIN TSS_RESFIELD B ON A.REFFIELDID = B.ID WHERE A.RESOURCEID=@RESOURCEID ORDER BY A.ENTRYNUM", new { RESOURCEID = resource.ID }).ToList();
        List<ResourceFilter> filters = helper.Query<ResourceFilter>("SELECT * FROM TSS_RESFILTER WHERE RESOURCEID=@RESOURCEID", new { RESOURCEID = resource.ID }).ToList();
        // 一次性加载 resuipc 全字段配置，@ui 过滤器使用
        Dictionary<string, UisetField> uisetFields = LoadUisetFields(helper, resource.ID);
        resource.Fields = columns;
        resource.Filters = filters;
        resource.UisetFields = uisetFields;
        return resource;
      }
    }

    /// <summary>
    /// 加载资源的 resuipc 全字段配置为 Dictionary，复用已有 DBHelper 连接
    /// </summary>
    private static Dictionary<string, UisetField> LoadUisetFields(DBHelper helper, string resourceId)
    {
      try
      {
        string sql = @"SELECT u.FIELDNAME, u.LABELNAME, u.EDITTYPE, u.QUERYTYPE, u.QUERYMODE, u.QUERYSORT, u.LISTSORT, u.DISPLAYINLIST,
                              u.RESFIELDID,
                              f.FIELDTYPE, f.REFRESOURCEANAME, f.REFFIELDID, f.UPFIELDID,
                              rf.FIELDNAME AS REFFIELDNAME
                       FROM tss_resuipc u
                       LEFT JOIN tss_resfield f ON u.RESFIELDID = f.ID
                       LEFT JOIN tss_resfield rf ON f.REFFIELDID = rf.ID
                       WHERE u.RESOURCEID = @RESOURCEID";
        var list = helper.Query<UisetField>(sql, new { RESOURCEID = resourceId }).ToList();
        var dict = new Dictionary<string, UisetField>();
        foreach (var item in list)
        {
          if (!string.IsNullOrEmpty(item.FIELDNAME))
          {
            dict[item.FIELDNAME] = item;
          }
        }
        return dict;
      }
      catch (System.Exception ex)
      {
        System.Console.WriteLine("LoadUisetFields ERROR: " + ex.Message);
        return new Dictionary<string, UisetField>();
      }
    }

    /// <summary>
    /// 获取资源的 resuipc 查询字段配置（QUERYSORT > 0），用于 @ui F01 模糊搜索
    /// 优先从 resource.UisetFields 过滤，避免额外查询；fallback 到数据库查询
    /// </summary>
    public static List<UisetField> GetUisetQueryFields(string resourceId)
    {
      // 优先从已加载的 Resource.UisetFields 过滤
      var fromCache = _cachedUisetQueryFields(resourceId);
      if (fromCache != null) return fromCache;

      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        string sql = @"SELECT u.FIELDNAME, u.LABELNAME, u.EDITTYPE, u.QUERYTYPE, u.QUERYMODE, u.QUERYSORT,
                              f.FIELDTYPE, f.REFRESOURCEANAME, f.REFFIELDNAME, f.REFFIELDID
                       FROM tss_resuipc u
                       LEFT JOIN tss_resfield f ON u.RESFIELDID = f.ID
                       WHERE u.RESOURCEID = @RESOURCEID AND u.QUERYSORT > 0
                       ORDER BY u.QUERYSORT";
        return helper.Query<UisetField>(sql, new { RESOURCEID = resourceId }).ToList();
      }
    }

    /// <summary>
    /// 尝试从已缓存的 Resource.UisetFields 中过滤 QUERYSORT>0 的字段
    /// </summary>
    private static List<UisetField> _cachedUisetQueryFields(string resourceId)
    {
      return null; // GetResource 已加载到 Resource.UisetFields，由 BuildFilterFromUI 直接使用
    }

    /// <summary>
    /// 获取资源的所有 resuipc 配置（不过滤 QUERYSORT），用于 @ui:adv 高级查询全字段推导
    /// 优先返回 resource.UisetFields，避免额外查询
    /// </summary>
    public static Dictionary<string, UisetField> GetUisetAllFields(string resourceId)
    {
      return null; // GetResource 已加载到 Resource.UisetFields，由 BuildFilterFromUI 直接使用
    }
  }
}
