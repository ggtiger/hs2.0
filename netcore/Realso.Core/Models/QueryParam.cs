/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/
using System.Collections;
using Realso.Data.ORM;
namespace Realso.Core.Models
{
    /// <summary>
    /// 查询实体
    /// </summary>
    public class QueryParam
    {

        public QueryParam(string path,string resourceName,QueryInfo queryInfo){
           this.Path = path; 
           this.ResourceName = resourceName;
           this.QueryInfo = queryInfo;
        }
        
        public string Path { get; set; }

        public string ResourceName { get; set; }

        public QueryInfo QueryInfo { get; set; }
    }
}
