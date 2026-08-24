using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.Utils
{
  /// <summary>
  /// OnlyOffice Document Server PDF 转换工具
  /// 调用 OnlyOffice ConvertService API 将 docx 转换为 pdf
  /// </summary>
  public class OnlyOfficeConverter
  {
    private static readonly string DocServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl") ?? "http://localhost:8088";
    private static readonly string ConvertApi = ConfigHelper.GetConfig("OnlyOffice:ConvertApi") ?? "/ConvertService.ashx";

    /// <summary>
    /// 将 docx 文件转换为 pdf
    /// </summary>
    /// <param name="docxPath">docx 文件绝对路径</param>
    /// <param name="pdfPath">pdf 输出绝对路径</param>
    /// <param name="fileId">文件ID（可选），用于构建HTTP URL供Document Server下载</param>
    /// <returns>是否转换成功</returns>
    public static bool ConvertToPdf(string docxPath, string pdfPath, string fileId = "")
    {
      try
      {
        if (!File.Exists(docxPath))
        {
          Logger.Info($"OnlyOfficeConverter: 源文件不存在 {docxPath}");
          return false;
        }

        // 如果 pdf 已存在，跳过转换
        if (File.Exists(pdfPath))
        {
          return true;
        }

        string fileUrl = BuildFileUrl(docxPath, fileId);
        if (string.IsNullOrEmpty(fileUrl))
        {
          Logger.Info("OnlyOfficeConverter: 无法构建文件URL，请确保文件路径在Web可访问目录下");
          return false;
        }

        string convertUrl = DocServerUrl + ConvertApi;
        string requestBody = BuildConvertRequest(fileUrl, "docx", "pdf");

        string responseJson = PostConvertRequest(convertUrl, requestBody);
        if (string.IsNullOrEmpty(responseJson))
        {
          Logger.Info("OnlyOfficeConverter: 转换请求无响应");
          return false;
        }

        // 解析响应，获取转换后的文件URL
        string resultFileUrl = ParseConvertResponse(responseJson);
        if (string.IsNullOrEmpty(resultFileUrl))
        {
          Logger.Info($"OnlyOfficeConverter: 转换响应解析失败 {responseJson}");
          return false;
        }

        // 下载转换后的 PDF
        return DownloadPdf(resultFileUrl, pdfPath);
      }
      catch (Exception ex)
      {
        Logger.Info($"OnlyOfficeConverter: 转换异常 {ex.Message}\n{ex.StackTrace}");
        return false;
      }
    }

    /// <summary>
    /// 构建文件的可访问URL
    /// 优先使用 HTTP URL（通过后端API下载），Document Server 可直接访问
    /// 无 fileId 时降级为 file:// 协议（仅限同机部署且 Document Server 可访问宿主机文件系统）
    /// </summary>
    private static string BuildFileUrl(string filePath, string fileId = "")
    {
      // 优先使用 HTTP URL，让 Document Server 通过后端 API 下载文件
      if (!string.IsNullOrEmpty(fileId))
      {
        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
        return apiUrl + "/api/file/" + fileId;
      }
      // 降级：file:// 协议（仅限同机部署）
      string normalized = filePath.Replace("\\", "/");
      if (!normalized.StartsWith("/"))
      {
        normalized = "/" + normalized;
      }
      return "file://" + normalized;
    }

    /// <summary>
    /// 构建转换请求 JSON
    /// </summary>
    private static string BuildConvertRequest(string fileUrl, string fromType, string toType)
    {
      var request = new
      {
        async = false,
        filetype = fromType,
        key = Guid.NewGuid().ToString("N"),
        outputtype = toType,
        title = $"convert_{fromType}_to_{toType}",
        url = fileUrl
      };
      return JsonConvert.SerializeObject(request);
    }

    /// <summary>
    /// 发送转换请求
    /// </summary>
    private static string PostConvertRequest(string url, string body)
    {
      try
      {
        if (url.StartsWith("https"))
          ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

        using (var client = new HttpClient())
        {
          client.Timeout = TimeSpan.FromSeconds(60);
          var content = new StringContent(body, Encoding.UTF8, "application/json");
          var response = client.PostAsync(url, content).Result;
          if (response.IsSuccessStatusCode)
          {
            return response.Content.ReadAsStringAsync().Result;
          }
          Logger.Info($"OnlyOfficeConverter: 请求失败 StatusCode={response.StatusCode}");
          return null;
        }
      }
      catch (Exception ex)
      {
        Logger.Info($"OnlyOfficeConverter: 请求异常 {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// 解析转换响应，获取结果文件URL
    /// OnlyOffice 可能返回 JSON 或 XML 格式
    /// </summary>
    private static string ParseConvertResponse(string response)
    {
      try
      {
        // 尝试 XML 解析（Document Server 默认返回 XML）
        if (response.TrimStart().StartsWith("<"))
        {
          var doc = new XmlDocument();
          doc.LoadXml(response);
          var fileUrlNode = doc.SelectSingleNode("//FileUrl");
          if (fileUrlNode != null && !string.IsNullOrEmpty(fileUrlNode.InnerText))
          {
            return fileUrlNode.InnerText;
          }
          return null;
        }
        // 降级 JSON 解析
        var json = JObject.Parse(response);
        return json["fileUrl"]?.ToString();
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// 下载转换后的 PDF 文件
    /// </summary>
    private static bool DownloadPdf(string pdfUrl, string savePath)
    {
      try
      {
        if (pdfUrl.StartsWith("https"))
          ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

        using (var client = new WebClient())
        {
          client.DownloadFile(pdfUrl, savePath);
        }
        return File.Exists(savePath);
      }
      catch (Exception ex)
      {
        Logger.Info($"OnlyOfficeConverter: 下载PDF失败 {ex.Message}");
        return false;
      }
    }
  }
}