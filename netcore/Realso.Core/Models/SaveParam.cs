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
    public class SaveParam
    {

        public SaveParam(string path,string resourceName,string StrXML){
           this.Path = path; 
           this.ResourceName = resourceName;
           this.StrXML = StrXML;
        }
        
        public string Path { get; set; }

        public string ResourceName { get; set; }

        public string StrXML { get; set; }
    }
}
