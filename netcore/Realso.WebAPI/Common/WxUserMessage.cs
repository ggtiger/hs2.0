using System.Collections;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Realso.WebAPI.Common
{
  public class WxUserMessage
  {
    public static void addMessage(String mobile, String userId, String msgType, Object msgParams, String sendType = "mp")
    {
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Url:公众号接口");

      try
      {
        Task.Run(() =>
        {
          Hashtable Params = new Hashtable();
          Params["mobile"] = mobile;
          Params["userId"] = userId;
          Params["msgType"] = msgType;
          Params["msgParams"] =JsonConvert.SerializeObject(msgParams);
          Params["sendType"] = sendType;
          HttpClientHepler.PostResponse(rPath + "wxmp/message/add", JsonConvert.SerializeObject (Params));
        });
      }
      catch (Exception e)
      {
      }
    }
  }
}
