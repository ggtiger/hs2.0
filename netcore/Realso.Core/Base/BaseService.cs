/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/
using Realso.Data.ORM;
using System.Collections;
using System.Collections.Generic;
using Realso.Core.Models;
using Realso.Data.ORM.Core;
namespace Realso.Core.Base
{
  /// <summary>
  /// 服务对象基类
  /// </summary>
  public class BaseService
  {
    private IViewOperate viewOperate;
    //获取主键
    public object GetNewID(string resourceName, int inc = 1)
    {
      Resource resource = viewOperate.GetResource(resourceName);
      IList<string> list = viewOperate.GetNewID(resource, inc);
      if (inc == 1)
      {
        return list[0];
      }
      return list;
    }
    //获取日期
    //获取日期时间
    //查询数据
    public object Open(QueryParam queryParam)
    {
      Resource resource = viewOperate.GetResource(queryParam.ResourceName);
      return viewOperate.Open(resource, queryParam.QueryInfo);
    }
    //执行数据
    public object Save(SaveParam saveParam)
    {
      DataView view = new DataView();
      viewOperate.FillData(view, saveParam.StrXML);
      return null;
    }
  }

}
