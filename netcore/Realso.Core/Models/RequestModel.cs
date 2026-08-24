/******************************************
 * AUTHOR:          ggtiger
 * CREATEDON:       2019-01-30
 ******************************************/

namespace Realso.Core.Models
{
    /// <summary>
    /// 请求实体
    /// </summary>
    public class RequestModel
    {
        /// <summary>
        /// 请求响应实体类
        /// </summary>
        public RequestModel()
        {
            Code = 200;
            Message = "操作成功";
        }
        /// <summary>
        /// 响应代码
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// 响应消息内容
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 返回的响应数据
        /// </summary>
    }
}
