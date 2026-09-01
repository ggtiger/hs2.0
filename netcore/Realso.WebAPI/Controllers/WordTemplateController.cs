using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using Realso.Utils;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace Realso.WebAPI.Controllers
{
  [Route("api/word-template")]
  public class WordTemplateController : BaseControl
  {
    private readonly IHostingEnvironment _hostingEnvironment;
    // 临时文件索引：ConcurrentDictionary 保证线程安全
    private static readonly ConcurrentDictionary<string, TempFileInfo> _tempFiles = new ConcurrentDictionary<string, TempFileInfo>();
    // 字段插入队列：按 tempKey 隔离，避免多用户同时编辑时字段交叉错乱
    private static readonly ConcurrentDictionary<string, List<FieldInsertCommand>> _fieldQueueByDoc = new ConcurrentDictionary<string, List<FieldInsertCommand>>();
    private static readonly object _fieldQueueLock = new object();
    // 插件上报的当前内容控件 Tag（按 docKey 隔离），用于前端高亮映射
    private static readonly ConcurrentDictionary<string, string> _currentSelection = new ConcurrentDictionary<string, string>();

    public WordTemplateController(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }

    #region 临时文件管理

    private class TempFileInfo
    {
      public string FilePath;
      public string OriginalFileId;
      public string FileName;
      public DateTime CreateTime;
      public bool CallbackReceived;
      public bool SavedByCallback;
      public string DocKey;
    }

    private void CleanupOldFiles()
    {
      var expired = _tempFiles.Where(kv => kv.Value.CreateTime < DateTime.Now.AddHours(-2)).ToList();
      foreach (var kv in expired)
      {
        try
        {
          if (System.IO.File.Exists(kv.Value.FilePath))
          {
            System.IO.File.Delete(kv.Value.FilePath);
          }
        }
        catch { }
        TempFileInfo removed;
        _tempFiles.TryRemove(kv.Key, out removed);
        // 同步清理该 tempKey 对应的选中状态
        string selRemoved;
        _currentSelection.TryRemove(kv.Key, out selRemoved);
      }
    }

    #endregion

    #region 配置接口

    /// <summary>
    /// 获取 OnlyOffice 相关配置（供前端运行时获取）
    /// GET /api/word-template/config
    /// </summary>
    [HttpGet("config")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetConfig()
    {
      try
      {
        string docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl") ?? "";
        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "";
        return Ok(new { docServerUrl, apiUrl });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetConfig 异常: " + ex.Message);
        return Ok(new { docServerUrl = "", apiUrl = "" });
      }
    }

    #endregion

    #region OnlyOffice 编辑器

    /// <summary>
    /// 打开 Word 模版进行 OnlyOffice 在线编辑
    /// GET /api/word-template/editor-config/{fileId}
    /// </summary>
    [HttpGet("editor-config/{fileId}")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetEditorConfig(string fileId)
    {
      try
      {
        // 查询文件信息
        Hashtable Params = new Hashtable();
        Params["FILTERCODE"] = "F00";
        Hashtable FilterParams = new Hashtable();
        FilterParams["ID"] = fileId;
        Params["FilterParams"] = FilterParams;
        BaseModel MAIN = GetModel("", "VSS_FILES");
        MAIN.Open(GetQueryInfo(Params));

        if (MAIN.GetView().Count == 0)
        {
          return NotFound(new { Message = "文件不存在" });
        }

        ViewRow row = MAIN.GetView()[0];
        string FILENAME = row.GetString("FILENAME");
        string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
        // 兼容两种路径形态：Windows 分隔符数据（\ 转 /）与 Linux 下以字面反斜杠为目录名的历史文件
        string rawPath = rootPath + row.GetString("FILEPATH");
        string FilePath = rootPath + row.GetString("FILEPATH").Replace('\\', '/');
        if (!System.IO.File.Exists(FilePath) && System.IO.File.Exists(rawPath))
        {
          FilePath = rawPath;
        }

        if (!System.IO.File.Exists(FilePath))
        {
          return NotFound(new { Message = "文件不存在于磁盘" });
        }

        // 复制到临时目录
        string key = Guid.NewGuid().ToString("N");
        string tempDir = Path.Combine(rootPath, "临时");
        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }
        string tempFilePath = Path.Combine(tempDir, key + "_" + FILENAME);
        System.IO.File.Copy(FilePath, tempFilePath);

        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl");
        if (string.IsNullOrEmpty(apiUrl))
        {
          return StatusCode(500, new { Message = "OnlyOffice:ApiUrl 未配置，请在 appsettings.json 中设置" });
        }
        string fileUrl = apiUrl + "/api/word-template/download?key=" + key;
        string callbackUrl = apiUrl + "/api/word-template/callback";
        string docKey = key + "_" + DateTime.Now.Ticks;

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = tempFilePath,
          OriginalFileId = fileId,
          FileName = FILENAME,
          CreateTime = DateTime.Now,
          DocKey = docKey
        };

        CleanupOldFiles();

        // 解析模版中已有的字段
        var existingFields = ParseContentControls(tempFilePath);

        var config = new JObject();
        var document = new JObject();
        document["fileType"] = "docx";
        document["key"] = docKey;
        document["title"] = FILENAME;
        document["url"] = fileUrl;
        var permissions = new JObject();
        permissions["edit"] = true;
        permissions["download"] = true;
        permissions["print"] = true;
        document["permissions"] = permissions;
        config["document"] = document;

        config["documentType"] = "word";

        var editorConfig = new JObject();
        editorConfig["mode"] = "edit";
        editorConfig["callbackUrl"] = callbackUrl;
        editorConfig["lang"] = "zh-CN";
        var user = new JObject();
        user["id"] = this.userInfo != null ? this.userInfo["ID"] + "" : "guest";
        user["name"] = this.userInfo != null ? this.userInfo["NICKNAME"] + "" : "访客";
        editorConfig["user"] = user;

        var customization = new JObject();
        customization["autosave"] = true;
        customization["comments"] = true;
        customization["forcesave"] = true;
        customization["help"] = false;
        customization["hideRightMenu"] = false;
        customization["compactHeader"] = true;
        customization["compactToolbar"] = true;
        customization["feedback"] = false;
        customization["toolbarNoTabs"] = false;
        editorConfig["customization"] = customization;

        // 配置字段插入插件，将 tempKey 传递给插件以实现文档隔离
        // 插件配置：仅用 autostart 从 Docker 本地加载插件（sdkjs-plugins/fieldinserter）
        // 注意：不要用 pluginsData 指向外部 URL，否则插件 HTML 的相对路径脚本（../v1/plugins.js）
        // 会解析到错误的域名导致加载失败
        var plugins = new JObject();
        plugins["autostart"] = new JArray { PluginGuid };
        plugins["pluginsData"] = new JArray();
        editorConfig["plugins"] = plugins;

        config["editorConfig"] = editorConfig;

        // 附加已有字段信息
        config["_existingFields"] = JArray.FromObject(existingFields);
        // 附加临时文件 GUID key（供前端调用 force-save/save 时回传）
        config["_tempKey"] = key;

        return Ok(config);
      }
      catch (Exception ex)
      {
        Logger.Info($"WordTemplate GetEditorConfig 异常: {ex.Message}\n{ex.StackTrace}");
        return StatusCode(500, new { Message = "获取编辑器配置失败" });
      }
    }

    /// <summary>
    /// 下载临时文件（供 OnlyOffice Document Server 调用）
    /// GET /api/word-template/download?key=xxx
    /// </summary>
    [HttpGet("download")]
    [EnableCors("AllowHeaders")]
    public IActionResult Download(string key)
    {
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      TempFileInfo info;
      if (!_tempFiles.TryGetValue(key, out info))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      if (!System.IO.File.Exists(info.FilePath))
      {
        return NotFound(new { Message = "文件不存在" });
      }

      var stream = System.IO.File.OpenRead(info.FilePath);
      string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
      return File(stream, contentType, info.FileName);
    }

    /// <summary>
    /// OnlyOffice 保存回调
    /// POST /api/word-template/callback
    /// </summary>
    [HttpPost("callback")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> Callback([FromBody] JObject body)
    {
      try
      {
        int status = body["status"]?.Value<int>() ?? 0;
        string key = body["key"]?.ToString();
        string url = body["url"]?.ToString();

        Logger.Info("WordTemplate Callback: status=" + status + ", key=" + (key ?? "null") + ", url=" + (url ?? "null"));

        var lookupKey = key;
        TempFileInfo tempInfo = null;
        if (!string.IsNullOrEmpty(key) && !_tempFiles.TryGetValue(key, out tempInfo))
        {
          var underscoreIdx = key.LastIndexOf('_');
          if (underscoreIdx > 0)
          {
            var baseKey = key.Substring(0, underscoreIdx);
            if (_tempFiles.TryGetValue(baseKey, out tempInfo))
            {
              lookupKey = baseKey;
              Logger.Info("WordTemplate Callback: matched baseKey=" + baseKey);
            }
          }
        }

        if (string.IsNullOrEmpty(lookupKey) || tempInfo == null)
        {
          Logger.Info("WordTemplate Callback: key not found in _tempFiles, keys=" + string.Join(",", _tempFiles.Keys));
          return Ok(new { error = 0 });
        }

        var info = tempInfo;

        if (status == 2 || status == 6)
        {
          if (!string.IsNullOrEmpty(url))
          {
            var downloadUrl = url;
            var docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl");
            if (!string.IsNullOrEmpty(docServerUrl))
            {
              try
              {
                var uri = new Uri(url);
                if (uri.Host != "localhost" && uri.Host != "127.0.0.1" && uri.Host != "host.docker.internal")
                {
                  var newUri = new UriBuilder(url) { Host = new Uri(docServerUrl).Host, Port = new Uri(docServerUrl).Port }.Uri;
                  downloadUrl = newUri.ToString();
                  Logger.Info("WordTemplate Callback: rewritten url from " + url + " to " + downloadUrl);
                }
              }
              catch { }
            }

            Logger.Info("WordTemplate Callback: downloading from " + downloadUrl);
            using (var client = new HttpClient())
            {
              client.Timeout = TimeSpan.FromSeconds(30);
              var response = await client.GetAsync(downloadUrl);
              if (response.IsSuccessStatusCode)
              {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.IO.File.WriteAllBytes(info.FilePath, bytes);
                info.SavedByCallback = true;
                Logger.Info("WordTemplate Callback: saved " + bytes.Length + " bytes to " + info.FilePath);
              }
            }
          }
        }
        else if (status == 4)
        {
          info.CallbackReceived = true;
          Logger.Info("WordTemplate Callback: status=4, no changes");
        }

        if (status == 2 || status == 6)
        {
          info.CallbackReceived = true;
          // 文档关闭/编辑完成时清理该 key 对应的选中状态和字段队列，防止内存泄漏
          string selRemoved;
          _currentSelection.TryRemove(lookupKey, out selRemoved);
          List<FieldInsertCommand> queueRemoved;
          _fieldQueueByDoc.TryRemove(lookupKey, out queueRemoved);
        }

        return Ok(new { error = 0 });
      }
      catch (Exception ex)
      {
        Logger.Info($"WordTemplate Callback 异常: {ex.Message}");
        return Ok(new { error = 0 });
      }
    }

    /// <summary>
    /// 保存编辑后的模版文件（回写到原始位置）
    /// POST /api/word-template/save
    /// </summary>
    [HttpPost("save")]
    [EnableCors("AllowHeaders")]
    public IActionResult SaveTemplate([FromBody] JObject body)
    {
      try
      {
        string key = body?["key"]?.ToString();
        string fileId = body?["fileId"]?.ToString();

        TempFileInfo info;
        if (string.IsNullOrEmpty(key) || !_tempFiles.TryGetValue(key, out info))
        {
          return BadRequest(new { Message = "临时文件不存在" });
        }

        // 不再强制要求回调完成：文档未修改时 OnlyOffice 不会触发回调，
        // 临时文件本身就是有效的（初始拷贝或回调覆盖版本），直接使用即可。
        if (!System.IO.File.Exists(info.FilePath))
        {
          return BadRequest(new { Message = "临时文件不存在于磁盘" });
        }

        // 如果有原始 fileId，回写到原始位置
        if (!string.IsNullOrEmpty(fileId))
        {
          Hashtable Params = new Hashtable();
          Params["FILTERCODE"] = "F00";
          Hashtable FilterParams = new Hashtable();
          FilterParams["ID"] = fileId;
          Params["FilterParams"] = FilterParams;
          BaseModel MAIN = GetModel("", "VSS_FILES");
          MAIN.Open(GetQueryInfo(Params));

          if (MAIN.GetView().Count > 0)
          {
            ViewRow row = MAIN.GetView()[0];
            string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
            string originalPath = rootPath + row.GetString("FILEPATH").Replace('\\', '/');
            System.IO.File.Copy(info.FilePath, originalPath, true);
            Logger.Info("WordTemplate Save: 已覆盖原始文件 " + originalPath);
          }
        }

        // 解析保存后的模版字段
        var fields = ParseContentControls(info.FilePath);

        return Ok(new { success = true, fields });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate Save 异常: " + ex.Message);
        return StatusCode(500, new { Message = "保存失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 强制保存（Command API）
    /// POST /api/word-template/force-save
    /// </summary>
    [HttpPost("force-save")]
    [EnableCors("AllowHeaders")]
    public IActionResult ForceSave([FromBody] JObject body)
    {
      try
      {
        string key = body?["key"]?.ToString();
        TempFileInfo info;
        if (string.IsNullOrEmpty(key) || !_tempFiles.TryGetValue(key, out info))
        {
          return BadRequest(new { Message = "文件不存在" });
        }
        if (string.IsNullOrEmpty(info.DocKey))
        {
          return BadRequest(new { Message = "DocKey 不存在" });
        }

        string docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl");
        if (string.IsNullOrEmpty(docServerUrl))
        {
          return StatusCode(500, new { Message = "OnlyOffice:DocServerUrl 未配置，请在 appsettings.json 中设置" });
        }
        string commandUrl = docServerUrl + "/command";

        var commandBody = new { c = "forcesave", key = info.DocKey };
        string jsonBody = JsonConvert.SerializeObject(commandBody);

        using (var client = new HttpClient())
        {
          client.Timeout = TimeSpan.FromSeconds(10);
          var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
          var response = client.PostAsync(commandUrl, content).Result;
          var result = response.Content.ReadAsStringAsync().Result;
          Logger.Info("WordTemplate ForceSave: " + result);
        }

        // 等待回调保存
        for (int i = 0; i < 10; i++)
        {
          System.Threading.Thread.Sleep(500);
          if (info.SavedByCallback)
          {
            return Ok(new { success = true });
          }
        }

        return Ok(new { success = false, message = "等待保存超时" });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate ForceSave 异常: " + ex.Message);
        return StatusCode(500, new { Message = "强制保存失败" });
      }
    }

    #endregion

    #region 模拟数据预览

    /// <summary>
    /// 用模拟数据填充模版并生成预览文件
    /// POST /api/word-template/preview
    /// body: { "key": "tempKey" }
    /// </summary>
    [HttpPost("preview")]
    [EnableCors("AllowHeaders")]
    public IActionResult PreviewWithMockData([FromBody] JObject body)
    {
      try
      {
        string key = body?["key"]?.ToString();
        if (string.IsNullOrEmpty(key))
        {
          return BadRequest(new { Message = "key 不能为空" });
        }

        TempFileInfo info;
        if (!_tempFiles.TryGetValue(key, out info))
        {
          return BadRequest(new { Message = "临时文件不存在，请重新打开编辑器" });
        }

        if (!System.IO.File.Exists(info.FilePath))
        {
          return BadRequest(new { Message = "临时文件不存在于磁盘" });
        }

        // 1. 解析模版中所有字段（SDT + Bookmark）
        var sdtFields = ParseContentControls(info.FilePath);
        var bmFields = ParseBookmarks(info.FilePath);

        // 诊断：输出解析到的字段和 _TABLE SDT 结构
        Logger.Info("=== PreviewWithMockData 诊断 key=" + key + " ===");
        Logger.Info("SDT字段数: " + sdtFields.Count + ", Bookmark字段数: " + bmFields.Count);
        foreach (var f in sdtFields)
        {
          Logger.Info("  SDT字段: Key=" + f.Key + ", Label=" + f.Label + ", Type=" + f.BaseType + ", Children=" + f.Children.Count);
        }
        DiagnoseTableSdtStructure(info.FilePath);

        // 2. 为每个字段生成模拟数据
        var mockData = new Dictionary<string, object>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in sdtFields)
        {
          GenerateMockData(f, mockData, seenKeys);
        }
        foreach (var f in bmFields)
        {
          GenerateMockData(f, mockData, seenKeys);
        }

        // 诊断：输出 mockData 内容
        foreach (var kv in mockData)
        {
          if (kv.Value is List<Dictionary<string, object>> rows)
          {
            Logger.Info("  mockData[" + kv.Key + "] = 表格 " + rows.Count + " 行");
          }
          else
          {
            Logger.Info("  mockData[" + kv.Key + "] = " + (kv.Value + ""));
          }
        }

        // 3. 复制模版到新文件，执行替换
        string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
        string previewDir = Path.Combine(rootPath, "预览");
        if (!Directory.Exists(previewDir))
        {
          Directory.CreateDirectory(previewDir);
        }
        string previewPath = Path.Combine(previewDir, "preview_" + key + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx");

        // CopyWord 会打开源文件并 SaveAs 到目标，然后 ReplaceFromTemplate 处理目标文件
        var copyDoc = WordHelper.CopyWord(info.FilePath, previewPath);
        WordHelper.ReplaceFromTemplate(copyDoc, mockData);

        // 4. 保存到 VSS_FILES，返回 fileId
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          string previewFileId = Guid.NewGuid().ToString("N");
          string relativePath = "预览/" + Path.GetFileName(previewPath);
          helper.Execute(
            "INSERT INTO TSS_FILES (ID, FILENAME, FILEPATH, FILESIZE, CREATEDATE) VALUES (@ID, @NAME, @PATH, @SIZE, NOW())",
            new { ID = previewFileId, NAME = info.FileName, PATH = relativePath, SIZE = new System.IO.FileInfo(previewPath).Length });

          return Ok(new { success = true, fileId = previewFileId });
        }
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate PreviewWithMockData 异常: " + ex.Message + "\n" + ex.StackTrace);
        return StatusCode(500, new { Message = "预览生成失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 诊断：检查 _TABLE 类型 SDT 的结构（是否包含 TableRow）
    /// </summary>
    private void DiagnoseTableSdtStructure(string docxPath)
    {
      try
      {
        using (var doc = WordprocessingDocument.Open(docxPath, false))
        {
          var sdts = doc.MainDocumentPart.Document.Body.Descendants<SdtElement>().ToList();
          foreach (var sdt in sdts)
          {
            var tagEl = sdt.SdtProperties?.GetFirstChild<Tag>();
            string tag = tagEl?.Val?.Value;
            if (string.IsNullOrEmpty(tag) || !tag.EndsWith("_TABLE")) continue;

            bool hasTableRow = sdt.Descendants<TableRow>().Any();
            bool hasTable = sdt.Descendants<Table>().Any();
            int rowCount = sdt.Descendants<TableRow>().Count();
            int childSdtCount = sdt.Descendants<SdtElement>().Count(e => e != sdt);
            // SDT 的直接内容类型
            string sdtType = sdt.GetType().Name;
            Logger.Info("  [TABLE诊断] Tag=" + tag + ", SDT类型=" + sdtType +
              ", 含TableRow=" + hasTableRow + " (" + rowCount + "行)" +
              ", 含Table=" + hasTable + ", 子SDT数=" + childSdtCount);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("DiagnoseTableSdtStructure 异常: " + ex.Message);
      }
    }

    // 占位图片缓存（避免每次预览都重新生成）
    private static readonly Dictionary<string, string> _placeholderCache = new Dictionary<string, string>();

    /// <summary>
    /// 生成或获取占位图片（纯色矩形，颜色由 label 决定，便于区分不同图片字段）。
    /// 用纯 C# 生成 PNG，不依赖 System.Drawing/libgdiplus，跨平台可靠。
    /// </summary>
    private string EnsurePlaceholderImage(string label)
    {
      try
      {
        string cacheKey = label ?? "";
        if (_placeholderCache.ContainsKey(cacheKey))
        {
          string cached = _placeholderCache[cacheKey];
          if (System.IO.File.Exists(cached)) return cached;
        }

        string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
        string dir = Path.Combine(rootPath, "预览");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string safeName = (label ?? "img").GetHashCode().ToString("X");
        string imgPath = Path.Combine(dir, "placeholder_" + safeName + ".png");

        if (!System.IO.File.Exists(imgPath))
        {
          // 颜色由 label hash 决定，不同字段显示不同颜色
          int hash = (label ?? "").GetHashCode();
          byte r = (byte)((hash >> 16) & 0x7F | 0x40);
          byte g = (byte)((hash >> 8) & 0x7F | 0x40);
          byte b = (byte)(hash & 0x7F | 0x40);
          byte[] png = CreateSolidColorPng(160, 50, r, g, b);
          System.IO.File.WriteAllBytes(imgPath, png);
        }

        _placeholderCache[cacheKey] = imgPath;
        return imgPath;
      }
      catch (Exception ex)
      {
        Logger.Info("EnsurePlaceholderImage 生成失败: " + ex.Message);
        return "";
      }
    }

    // 以下为纯 C# PNG 生成（不依赖图形库）====================================

    /// <summary>生成纯色 RGB PNG 字节数组</summary>
    private static byte[] CreateSolidColorPng(int width, int height, byte r, byte g, byte b)
    {
      using (var ms = new MemoryStream())
      {
        // PNG 签名
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);

        // IHDR
        byte[] ihdr = new byte[13];
        WriteBE32(ihdr, 0, width);
        WriteBE32(ihdr, 4, height);
        ihdr[8] = 8;  // 位深度
        ihdr[9] = 2;  // 颜色类型：RGB
        WriteChunk(ms, "IHDR", ihdr);

        // IDAT（每行 = 1字节filter + width*3字节像素）
        byte[] raw = new byte[height * (1 + width * 3)];
        for (int y = 0; y < height; y++)
        {
          int rowStart = y * (1 + width * 3);
          raw[rowStart] = 0; // filter: none
          for (int x = 0; x < width; x++)
          {
            int idx = rowStart + 1 + x * 3;
            raw[idx] = r;
            raw[idx + 1] = g;
            raw[idx + 2] = b;
          }
        }
        WriteChunk(ms, "IDAT", ZlibCompress(raw));

        // IEND
        WriteChunk(ms, "IEND", new byte[0]);

        return ms.ToArray();
      }
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
      byte[] len = new byte[4];
      WriteBE32(len, 0, data.Length);
      s.Write(len, 0, 4);

      byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
      s.Write(typeBytes, 0, 4);
      if (data.Length > 0) s.Write(data, 0, data.Length);

      // CRC32 覆盖 type + data
      using (var crcInput = new MemoryStream())
      {
        crcInput.Write(typeBytes, 0, 4);
        if (data.Length > 0) crcInput.Write(data, 0, data.Length);
        byte[] crc = new byte[4];
        WriteBE32(crc, 0, (int)Crc32(crcInput.ToArray()));
        s.Write(crc, 0, 4);
      }
    }

    private static uint[] _crcTable;
    private static uint Crc32(byte[] data)
    {
      if (_crcTable == null)
      {
        _crcTable = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
          uint c = n;
          for (int k = 0; k < 8; k++)
            c = (c & 1) == 1 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
          _crcTable[n] = c;
        }
      }
      uint crc = 0xFFFFFFFF;
      foreach (byte b in data)
        crc = _crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
      return crc ^ 0xFFFFFFFF;
    }

    // zlib = 2字节头 + Deflate 数据 + 4字节 Adler32
    private static byte[] ZlibCompress(byte[] data)
    {
      using (var ms = new MemoryStream())
      {
        ms.WriteByte(0x78);
        ms.WriteByte(0x01);
        using (var ds = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionLevel.Optimal, true))
        {
          ds.Write(data, 0, data.Length);
        }
        byte[] adler = new byte[4];
        WriteBE32(adler, 0, (int)Adler32(data));
        ms.Write(adler, 0, 4);
        return ms.ToArray();
      }
    }

    private static uint Adler32(byte[] data)
    {
      uint a = 1, b = 0;
      foreach (byte c in data)
      {
        a = (a + c) % 65521;
        b = (b + a) % 65521;
      }
      return (b << 16) | a;
    }

    private static void WriteBE32(byte[] buf, int offset, int value)
    {
      buf[offset] = (byte)(value >> 24);
      buf[offset + 1] = (byte)(value >> 16);
      buf[offset + 2] = (byte)(value >> 8);
      buf[offset + 3] = (byte)value;
    }

    /// <summary>
    /// 根据字段定义生成贴合实际的模拟数据
    /// </summary>
    private void GenerateMockData(FieldDefinition field, Dictionary<string, object> mockData, HashSet<string> seenKeys)
    {
      string key = field.Key;
      string baseName = key;
      string suffix = "";

      // 解析后缀
      var parts = key.Split('_');
      if (parts.Length > 1)
      {
        string lastPart = parts[parts.Length - 1];
        if (lastPart == "YY" || lastPart == "MM" || lastPart == "DD" ||
            lastPart == "IMG" || lastPart == "IMG2" || lastPart == "HTML" || lastPart == "TABLE")
        {
          baseName = string.Join("_", parts.Take(parts.Length - 1));
          suffix = lastPart;
        }
      }

      // 跳过已处理的 key
      if (seenKeys.Contains(key)) return;
      seenKeys.Add(key);

      // 图片字段：生成占位图片路径（用字段 label 作为图片文字，便于辨认）
      if (suffix == "IMG" || suffix == "IMG2")
      {
        if (!mockData.ContainsKey(key))
        {
          string label = string.IsNullOrEmpty(field.Label) ? baseName : field.Label;
          mockData[key] = EnsurePlaceholderImage(label);
        }
        return;
      }

      // 表格字段：根据子字段生成多行贴合实际的模拟数据
      if (suffix == "TABLE")
      {
        if (!mockData.ContainsKey(baseName))
        {
          var rows = new List<Dictionary<string, object>>();
          // 有子字段时，按子字段生成 3 行数据；无子字段时生成 2 行占位数据
          if (field.Children.Count > 0)
          {
            for (int i = 1; i <= 3; i++)
            {
              var row = new Dictionary<string, object>();
              foreach (var child in field.Children)
              {
                string childKey = child.Key;
                // 解析子字段后缀，得到 baseName（替换引擎用 baseName 查找）
                string cBase = childKey;
                string cSuffix = "";
                var cParts = childKey.Split('_');
                if (cParts.Length > 1)
                {
                  string cLast = cParts[cParts.Length - 1];
                  if (cLast == "YY" || cLast == "MM" || cLast == "DD" ||
                      cLast == "IMG" || cLast == "IMG2" || cLast == "HTML")
                  {
                    cBase = string.Join("_", cParts.Take(cParts.Length - 1));
                    cSuffix = cLast;
                  }
                }

                // 图片子字段：跳过（无法生成图片）
                if (cSuffix == "IMG" || cSuffix == "IMG2") continue;

                string val;
                if (cSuffix == "HTML")
                {
                  // HTML 富文本子字段：生成富文本内容
                  string htmlLabel = string.IsNullOrEmpty(child.Label) ? cBase : child.Label;
                  val = "<p style=\"font-size:14px;line-height:1.6\"><strong>" + htmlLabel + "</strong>：<span style=\"color:#1890ff\">模拟" + i + "</span>，支持<em>富文本</em>格式</p>";
                }
                else
                {
                  val = GenerateSmartMockValue(cBase, child.Label, i);
                }

                // 用 baseName 作为 key（替换引擎用 baseName 查找）；Bookmark 兼容加 T 前缀
                row[cBase] = val;
                row["T" + cBase] = val;
              }
              rows.Add(row);
            }
          }
          else
          {
            for (int i = 1; i <= 2; i++)
            {
              var row = new Dictionary<string, object>();
              row["TDUMMY"] = "模拟行" + i;
              rows.Add(row);
            }
          }
          mockData[baseName] = rows;
        }
        return;
      }

      // HTML 富文本字段：替换引擎用 baseName 查找 HTML 内容
      if (suffix == "HTML")
      {
        if (!mockData.ContainsKey(baseName))
        {
          // 生成贴合字段含义的富文本模拟数据
          string htmlLabel = string.IsNullOrEmpty(field.Label) ? baseName : field.Label;
          mockData[baseName] = "<p style=\"font-size:14px;line-height:1.6\"><strong>" + htmlLabel + "</strong>：<span style=\"color:#1890ff\">模拟富文本内容</span>，支持<em>斜体</em>、<u>下划线</u>、<strong>加粗</strong>等格式。</p>";
        }
        return;
      }

      // 日期后缀字段：先确保 baseName 有完整日期值
      if (suffix == "YY" || suffix == "MM" || suffix == "DD")
      {
        if (!mockData.ContainsKey(baseName))
        {
          mockData[baseName] = "2024-06-19";
        }
        return;
      }

      // 普通文本/日期字段：根据字段名关键词智能生成贴合实际的模拟值
      string mockValue = GenerateSmartMockValue(key, field.Label);
      if (!mockData.ContainsKey(key))
      {
        mockData[key] = mockValue;
      }
    }

    /// <summary>
    /// 根据字段名关键词智能生成贴合实际的模拟值
    /// </summary>
    /// <summary>
    /// 生成模拟值（支持表格行号差异化）
    /// </summary>
    private string GenerateSmartMockValue(string key, string label, int rowIndex = 0)
    {
      string baseValue = GenerateSmartMockValueCore(key, label);
      if (rowIndex <= 0) return baseValue;

      // 表格行：每行模拟数据需有差异
      string upper = key.ToUpper();
      // 编号类：末尾加三位序号（JD2024-0619001）
      if (upper.Contains("CODE") || upper.Contains("OPCODE") || upper.Contains("BILLNO"))
        return baseValue + rowIndex.ToString("D3");
      // 设备/参数名：用 A/B/C 区分（数显游标卡尺A）
      if (upper == "MNAME" || upper == "ARDNAME" || upper == "NAME" || upper.Contains("ITEMNAME"))
        return baseValue + (char)('A' + rowIndex - 1);
      // 规格/精度/日期：保持不变（同一委托下规格相同是合理的）
      if (upper.Contains("SIZETYPE") || upper.Contains("SPEC") || upper.Contains("DEGREE") ||
          upper.Contains("DATE") || upper.Contains("ACCURACY"))
        return baseValue;
      // 数值类：递增让差异可见
      if (upper.Contains("VALUE") || upper.Contains("DATA") || upper.Contains("MEASURE"))
      {
        // 尝试在数字基础上递增
        return baseValue + "-" + rowIndex;
      }
      // 其他：加序号后缀
      return baseValue + rowIndex;
    }

    private string GenerateSmartMockValueCore(string key, string label)
    {
      string upper = key.ToUpper();
      string upperLabel = (label ?? "").ToUpper();

      // 编号类
      if (upper.Contains("CODE") || upper.Contains("BILLNO") || upper.Contains("BILLCODE") || upper.Contains("CERTCODE") || upper.Contains("OPCODE"))
        return "JD2024-0619";
      if (upper.Contains("WTCODE"))
        return "WT2024-001";
      if (upper.Contains("DOCNUMBER") || upper.Contains("DOCCODE"))
        return "DOC2024-0619-001";

      // 名称类
      if (upper == "CUSTNAME" || upper.Contains("CUSTOMER") || upper.Contains("CLIENT"))
        return "XX科技有限公司";
      if (upper == "MNAME" || upper == "NAME" || upper == "ARDNAME")
        return "数显游标卡尺";
      if (upper.Contains("SIZETYPE") || upper.Contains("SPEC") || upper.Contains("MODEL"))
        return "0-150mm";
      if (upper == "MANUFACTURER")
        return "XX精密仪器有限公司";
      if (upper == "CORGNAME" || upper.Contains("ORGNAME"))
        return "XX计量检测有限公司";

      // 日期类
      if (upper.Contains("DATE") || upper.Contains("TIME"))
        return "2024-06-19";
      if (upper == "SIGNDATE")
        return "2024-06-19";
      if (upper == "EXPDATE")
        return "2025-06-19";

      // 人员类
      if (upper == "CREATER" || upper.Contains("SUBMITER") || upper.Contains("SUBMIT"))
        return "张三";
      if (upper == "CHECKER" || upper.Contains("CHECKNAME"))
        return "李四";
      if (upper == "VERIFIER" || upper.Contains("VERIFYNAME"))
        return "王五";
      if (upper.Contains("PERSON") || upper.Contains("OPERATOR") || upper.Contains("WORKER"))
        return "赵六";

      // 地址/联系方式
      if (upper.Contains("ADDRESS") || upper.Contains("ADDR"))
        return "江苏省常州市金坛区XX路XX号";
      if (upper.Contains("PHONE") || upper.Contains("TEL") || upper.Contains("MOBILE"))
        return "0519-8288XXXX";
      if (upper.Contains("EMAIL") || upper.Contains("MAIL"))
        return "test@example.com";

      // 精度/偏差类
      if (upper == "DEGREE" || upper.Contains("ACCURACY") || upper.Contains("PRECISION"))
        return "±0.02mm";
      if (upper.Contains("TOLERANCE"))
        return "±0.05";

      // 金额/数量
      if (upper.Contains("AMOUNT") || upper.Contains("FEE") || upper.Contains("PRICE") || upper.Contains("COST"))
        return "1500.00";
      if (upper.Contains("QTY") || upper.Contains("COUNT") || upper.Contains("NUM"))
        return "3";

      // 结果/结论
      if (upper.Contains("RESULT") || upper.Contains("CONCLUSION"))
        return "合格";
      if (upper.Contains("REMARK") || upper.Contains("NOTE") || upper.Contains("MEMO"))
        return "设备状态良好";

      // ID 类
      if (upper == "ID" || upper.EndsWith("ID"))
        return Guid.NewGuid().ToString("N").Substring(0, 8);

      // 兜底：加「模拟」前缀区分，避免和占位文本混淆
      if (!string.IsNullOrEmpty(label) && label != key)
        return "模拟" + label;

      return "模拟" + key;
    }

    #endregion

    #region OnlyOffice 字段插入插件

    private static readonly string PluginGuid = "asc.{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}";

    /// <summary>
    /// 获取字段插入插件配置
    /// GET /api/word-template/plugin/config?key=xxx
    /// </summary>
    [HttpGet("plugin/config")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetPluginConfig(string key)
    {
      string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl");
      if (string.IsNullOrEmpty(apiUrl))
      {
        Logger.Info("WordTemplate GetPluginConfig: OnlyOffice:ApiUrl 未配置");
        apiUrl = "http://127.0.0.1:5001";
      }

      string pluginCodeUrl = apiUrl + "/api/word-template/plugin/code";
      if (!string.IsNullOrEmpty(key))
      {
        pluginCodeUrl += "?key=" + Uri.EscapeDataString(key);
      }

      var config = new JObject();
      config["name"] = "Field Inserter";
      config["guid"] = PluginGuid;
      config["variations"] = new JArray {
        new JObject {
          { "description", "Insert template fields as content controls" },
          { "url", pluginCodeUrl },
          { "isViewer", false },
          { "EditorsSupport", new JArray { "word" } },
          { "isVisual", false },
          { "isModal", false },
          { "isInsideMode", false },
          { "initDataType", "none" }
        }
      };
      return Ok(config);
    }

    /// <summary>
    /// 获取字段插入插件代码（动态注入 API 地址和 docKey）
    /// GET /api/word-template/plugin/code?key=xxx
    /// </summary>
    [HttpGet("plugin/code")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetPluginCode(string key)
    {
      string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl");
      if (string.IsNullOrEmpty(apiUrl))
      {
        Logger.Info("WordTemplate GetPluginCode: OnlyOffice:ApiUrl 未配置");
        apiUrl = "http://127.0.0.1:5001";
      }

      string fieldQueueUrl = apiUrl + "/api/word-template/field-queue";
      string selectionUrl = apiUrl + "/api/word-template/current-selection";
      // docKey 使用 tempKey（即前端打开编辑器时返回的 _tempKey）
      string docKeyJs = string.IsNullOrEmpty(key) ? "''" : "'" + key + "'";

      string html = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8""></head><body>
<script>
window._WORD_TEMPLATE_API_URL_ = '" + fieldQueueUrl + @"';
window._WORD_TEMPLATE_SEL_URL_ = '" + selectionUrl + @"';
window._WORD_TEMPLATE_DOC_KEY_ = " + docKeyJs + @";
</script>
<script src=""../v1/plugins.js""></script>
<script src=""index.js""></script>
</body></html>";

      return Content(html, "text/html", System.Text.Encoding.UTF8);
    }

    #endregion

    #region 字段插入队列（前端→插件通信桥梁）

    /// <summary>
    /// 前端写入待插入字段（按 docKey 隔离，避免多用户同时编辑时字段交叉）
    /// POST /api/word-template/field-queue
    /// body: { "docKey": "tempKey", "field": { "key": "xxx", "label": "xxx", "type": "text" } }
    /// </summary>
    [HttpPost("field-queue")]
    [EnableCors("AllowHeaders")]
    public IActionResult EnqueueField([FromBody] JObject body)
    {
      try
      {
        string docKey = body?["docKey"]?.ToString();
        string fieldKey = body?["field"]?["key"]?.ToString();
        string fieldLabel = body?["field"]?["label"]?.ToString();
        string fieldType = body?["field"]?["type"]?.ToString();

        if (string.IsNullOrEmpty(fieldKey))
        {
          return BadRequest(new { Message = "field.key 不能为空" });
        }

        // 兼容过渡期：无 docKey 时归入 "default"
        string queueKey = string.IsNullOrEmpty(docKey) ? "default" : docKey;

        var cmd = new FieldInsertCommand
        {
          FieldKey = fieldKey,
          FieldLabel = string.IsNullOrEmpty(fieldLabel) ? fieldKey : fieldLabel,
          FieldType = string.IsNullOrEmpty(fieldType) ? "text" : fieldType
        };

        lock (_fieldQueueLock)
        {
          List<FieldInsertCommand> queue;
          if (!_fieldQueueByDoc.TryGetValue(queueKey, out queue))
          {
            queue = new List<FieldInsertCommand>();
            _fieldQueueByDoc[queueKey] = queue;
          }
          queue.Add(cmd);
        }

        Logger.Info($"WordTemplate EnqueueField: docKey={queueKey}, field={fieldKey}");
        return Ok(new { success = true });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate EnqueueField 异常: " + ex.Message);
        return StatusCode(500, new { Message = "插入字段失败" });
      }
    }

    /// <summary>
    /// 插件轮询获取待插入字段（按 key 隔离，读取后清空）
    /// GET /api/word-template/field-queue?key=xxx
    /// </summary>
    [HttpGet("field-queue")]
    [EnableCors("AllowHeaders")]
    public IActionResult DequeueFields(string key)
    {
      try
      {
        JArray result = new JArray();

        lock (_fieldQueueLock)
        {
          if (!string.IsNullOrEmpty(key))
          {
            // 按 docKey 精确返回（新版插件，支持多用户隔离）
            List<FieldInsertCommand> queue;
            if (_fieldQueueByDoc.TryGetValue(key, out queue) && queue.Count > 0)
            {
              foreach (var cmd in queue)
              {
                result.Add(new JObject(
                  new JProperty("key", cmd.FieldKey),
                  new JProperty("label", cmd.FieldLabel),
                  new JProperty("type", cmd.FieldType)
                ));
              }
              queue.Clear();
            }
          }
          else
          {
            // 无 key 时（Docker 本地加载的插件无法获知 docKey），返回并清空所有队列
            // 注意：多用户同时编辑时此处无隔离，但能保证插件正常工作
            foreach (var kv in _fieldQueueByDoc)
            {
              foreach (var cmd in kv.Value)
              {
                result.Add(new JObject(
                  new JProperty("key", cmd.FieldKey),
                  new JProperty("label", cmd.FieldLabel),
                  new JProperty("type", cmd.FieldType)
                ));
              }
              kv.Value.Clear();
            }
          }
        }

        return Ok(new { fields = result });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate DequeueFields 异常: " + ex.Message);
        return Ok(new { fields = new JArray() });
      }
    }

    #endregion

    #region 当前内容控件选中（插件→后端→前端 高亮映射桥梁）

    /// <summary>
    /// 插件上报当前光标所在内容控件的 Tag
    /// POST /api/word-template/current-selection
    /// body: { "key": "docKey", "tag": "FIELDKEY" }
    /// </summary>
    [HttpPost("current-selection")]
    [EnableCors("AllowHeaders")]
    public IActionResult ReportCurrentSelection([FromBody] JObject body)
    {
      try
      {
        string docKey = body?["key"]?.ToString();
        string tag = body?["tag"]?.ToString();
        if (string.IsNullOrEmpty(docKey))
        {
          return Ok(new { success = false });
        }
        if (string.IsNullOrEmpty(tag))
        {
          string removed;
          _currentSelection.TryRemove(docKey, out removed);
        }
        else
        {
          _currentSelection[docKey] = tag;
        }
        return Ok(new { success = true });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate ReportCurrentSelection 异常: " + ex.Message);
        return Ok(new { success = false });
      }
    }

    /// <summary>
    /// 前端轮询当前选中内容控件的 Tag
    /// GET /api/word-template/current-selection?key=docKey
    /// </summary>
    [HttpGet("current-selection")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetCurrentSelection(string key)
    {
      try
      {
        if (string.IsNullOrEmpty(key))
        {
          return Ok(new { tag = "" });
        }
        string tag = "";
        _currentSelection.TryGetValue(key, out tag);
        return Ok(new { tag = tag ?? "" });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetCurrentSelection 异常: " + ex.Message);
        return Ok(new { tag = "" });
      }
    }

    /// <summary>
    /// 前端关闭编辑器时主动清理选中状态
    /// DELETE /api/word-template/current-selection?key=xxx
    /// </summary>
    [HttpDelete("current-selection")]
    [EnableCors("AllowHeaders")]
    public IActionResult ClearCurrentSelection(string key)
    {
      try
      {
        if (!string.IsNullOrEmpty(key))
        {
          string removed;
          _currentSelection.TryRemove(key, out removed);
          List<FieldInsertCommand> queueRemoved;
          _fieldQueueByDoc.TryRemove(key, out queueRemoved);
        }
        return Ok(new { success = true });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate ClearCurrentSelection 异常: " + ex.Message);
        return Ok(new { success = false });
      }
    }

    #endregion

    #region 字段来源配置（持久化到 tbs_word_template）

    /// <summary>
    /// 加载字段来源配置
    /// GET /api/word-template/field-config?fileId=xxx
    /// </summary>
    [HttpGet("field-config")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetFieldConfig(string fileId)
    {
      try
      {
        if (string.IsNullOrEmpty(fileId))
        {
          return Ok(new { moduleCode = "", templateId = "", manualFields = new JArray() });
        }

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var row = helper.QueryFirstOrDefault(
            "SELECT MODULECODE, TEMPLATEID, FIELDBINDINGS FROM tbs_word_template WHERE FILEID=@FILEID AND ISDELETED=0 LIMIT 1",
            new { FILEID = fileId });

          string moduleCode = "";
          string templateId = "";
          JArray manualFields = new JArray();

          if (row != null)
          {
            moduleCode = (row.MODULECODE ?? "") + "";
            templateId = (row.TEMPLATEID ?? "") + "";
            string bindings = (row.FIELDBINDINGS ?? "") + "";
            if (!string.IsNullOrEmpty(bindings))
            {
              try
              {
                var bindObj = JObject.Parse(bindings);
                var mf = bindObj["manualFields"] as JArray;
                if (mf != null) manualFields = mf;
              }
              catch { }
            }
          }

          return Ok(new
          {
            moduleCode = moduleCode,
            templateId = templateId,
            manualFields = manualFields
          });
        }
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetFieldConfig 异常: " + ex.Message);
        return Ok(new { moduleCode = "", templateId = "", manualFields = new JArray() });
      }
    }

    /// <summary>
    /// 保存字段来源配置
    /// POST /api/word-template/field-config
    /// body: { "fileId": "xxx", "moduleCode": "xxx", "templateId": "xxx", "manualFields": [...] }
    /// </summary>
    [HttpPost("field-config")]
    [EnableCors("AllowHeaders")]
    public IActionResult SaveFieldConfig([FromBody] JObject body)
    {
      try
      {
        string fileId = body?["fileId"]?.ToString();
        string moduleCode = body?["moduleCode"]?.ToString();
        string templateId = body?["templateId"]?.ToString();
        var manualFields = body?["manualFields"] as JArray;

        if (string.IsNullOrEmpty(fileId))
        {
          return BadRequest(new { Message = "fileId 不能为空" });
        }

        var bindObj = new JObject();
        bindObj["manualFields"] = manualFields ?? new JArray();
        string fieldBindings = bindObj.ToString(Formatting.None);

        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 检查是否已有记录
          var existing = helper.QueryFirstOrDefault(
            "SELECT ID FROM tbs_word_template WHERE FILEID=@FILEID AND ISDELETED=0 LIMIT 1",
            new { FILEID = fileId });

          if (existing != null)
          {
            string existId = existing.ID + "";
            helper.Execute(
              "UPDATE tbs_word_template SET MODULECODE=@MODULECODE, TEMPLATEID=@TEMPLATEID, FIELDBINDINGS=@FIELDBINDINGS, UPDATEDATE=NOW() WHERE ID=@ID",
              new { MODULECODE = moduleCode ?? "", TEMPLATEID = templateId ?? "", FIELDBINDINGS = fieldBindings, ID = existId });
          }
          else
          {
            // 没有记录则插入
            string newId = Guid.NewGuid().ToString("N");
            helper.Execute(
              "INSERT INTO tbs_word_template (ID, TEMPLATENAME, MODULECODE, TEMPLATEID, FILEID, FIELDBINDINGS, ISUSE, ISDELETED, CREATEDATE) " +
              "VALUES (@ID, @NAME, @MODULECODE, @TEMPLATEID, @FILEID, @FIELDBINDINGS, 1, 0, NOW())",
              new { ID = newId, NAME = "模版编辑", MODULECODE = moduleCode ?? "", TEMPLATEID = templateId ?? "", FILEID = fileId, FIELDBINDINGS = fieldBindings });
          }
        }

        return Ok(new { success = true });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate SaveFieldConfig 异常: " + ex.Message);
        return StatusCode(500, new { Message = "保存字段配置失败" });
      }
    }

    #endregion

    #region 字段获取接口

    /// <summary>
    /// 合并获取所有来源字段
    /// GET /api/word-template/fields?moduleCode=xxx&amp;templateId=xxx&amp;type=cert
    /// </summary>
    [HttpGet("fields")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetAllFields(string moduleCode, string templateId, string type)
    {
      try
      {
        var groups = new JArray();

        // 来源 A: ORM 元数据 — 主表字段和子表分开展示
        if (!string.IsNullOrEmpty(moduleCode))
        {
          var ormFields = GetOrmFields(moduleCode);
          var mainFields = new JArray(ormFields.Where(f => (string)f["type"] != "table"));
          var subTables = new JArray(ormFields.Where(f => (string)f["type"] == "table"));

          if (mainFields.Count > 0)
          {
            groups.Add(new JObject(
              new JProperty("name", "业务字段(主表)"),
              new JProperty("source", "orm"),
              new JProperty("fields", mainFields)
            ));
          }

          if (subTables.Count > 0)
          {
            groups.Add(new JObject(
              new JProperty("name", "业务字段(子表)"),
              new JProperty("source", "orm-sub"),
              new JProperty("fields", subTables)
            ));
          }
        }

        // 来源 B: 模版管理字段 — 主表字段和子表分开展示
        if (!string.IsNullOrEmpty(templateId))
        {
          var templateFields = GetTemplateFields(templateId);
          var tplMainFields = new JArray(templateFields.Where(f => (string)f["type"] != "table"));
          var tplSubTables = new JArray(templateFields.Where(f => (string)f["type"] == "table"));

          if (tplMainFields.Count > 0)
          {
            groups.Add(new JObject(
              new JProperty("name", "模版字段(主表)"),
              new JProperty("source", "template"),
              new JProperty("fields", tplMainFields)
            ));
          }

          if (tplSubTables.Count > 0)
          {
            groups.Add(new JObject(
              new JProperty("name", "模版字段(子表)"),
              new JProperty("source", "template-sub"),
              new JProperty("fields", tplSubTables)
            ));
          }
        }

        // 系统内置字段
        var systemFields = GetSystemFields(type);
        if (systemFields.Count > 0)
        {
          groups.Add(new JObject(
            new JProperty("name", "系统字段"),
            new JProperty("source", "system"),
            new JProperty("fields", systemFields)
          ));
        }

        return Ok(new { groups });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetAllFields 异常: " + ex.Message);
        return StatusCode(500, new { Message = "获取字段失败" });
      }
    }

    /// <summary>
    /// 获取 ORM 元数据字段
    /// GET /api/word-template/fields/orm?moduleCode=xxx
    /// </summary>
    [HttpGet("fields/orm")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetOrmFieldsApi(string moduleCode)
    {
      try
      {
        var fields = GetOrmFields(moduleCode);
        return Ok(new { fields });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetOrmFields 异常: " + ex.Message);
        return StatusCode(500, new { Message = "获取字段失败" });
      }
    }

    /// <summary>
    /// 获取模版管理字段
    /// GET /api/word-template/fields/template?templateId=xxx
    /// </summary>
    [HttpGet("fields/template")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetTemplateFieldsApi(string templateId)
    {
      try
      {
        var fields = GetTemplateFields(templateId);
        return Ok(new { fields });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetTemplateFields 异常: " + ex.Message);
        return StatusCode(500, new { Message = "获取字段失败" });
      }
    }

    /// <summary>
    /// 获取系统内置字段
    /// GET /api/word-template/fields/system?type=cert
    /// </summary>
    [HttpGet("fields/system")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetSystemFieldsApi(string type)
    {
      try
      {
        var fields = GetSystemFields(type);
        return Ok(new { fields });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetSystemFields 异常: " + ex.Message);
        return StatusCode(500, new { Message = "获取字段失败" });
      }
    }

    /// <summary>
    /// 解析已有模版文件中的字段（SDT + Bookmark）
    /// GET /api/word-template/parse-fields/{fileId}
    /// </summary>
    [HttpGet("parse-fields/{fileId}")]
    [EnableCors("AllowHeaders")]
    public IActionResult ParseFields(string fileId)
    {
      try
      {
        Hashtable Params = new Hashtable();
        Params["FILTERCODE"] = "F00";
        Hashtable FilterParams = new Hashtable();
        FilterParams["ID"] = fileId;
        Params["FilterParams"] = FilterParams;
        BaseModel MAIN = GetModel("", "VSS_FILES");
        MAIN.Open(GetQueryInfo(Params));

        if (MAIN.GetView().Count == 0)
        {
          return NotFound(new { Message = "文件不存在" });
        }

        ViewRow row = MAIN.GetView()[0];
        string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
        string FilePath = rootPath + row.GetString("FILEPATH").Replace('\\', '/');

        if (!System.IO.File.Exists(FilePath))
        {
          return NotFound(new { Message = "文件不存在于磁盘" });
        }

        var fields = ParseContentControls(FilePath);
        var bookmarks = ParseBookmarks(FilePath);

        return Ok(new { contentControls = fields, bookmarks });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate ParseFields 异常: " + ex.Message);
        return StatusCode(500, new { Message = "解析字段失败" });
      }
    }

    #endregion

    #region 私有方法 - 字段获取

    /// <summary>
    /// 获取业务模块列表
    /// GET /api/word-template/modules
    /// </summary>
    [HttpGet("modules")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetModuleList()
    {
      try
      {
        var list = new JArray();
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var rows = helper.Query("SELECT MODULECODE, MODULENAME FROM TSS_MOUDLE ORDER BY MODULECODE");
          foreach (var row in rows)
          {
            list.Add(new JObject(
              new JProperty("key", row.MODULECODE + ""),
              new JProperty("title", row.MODULENAME + "")
            ));
          }
        }
        return Ok(new { modules = list });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetModuleList 异常: " + ex.Message + "\n" + ex.StackTrace);
        return StatusCode(500, new { Message = "获取模块列表失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 获取模版列表
    /// GET /api/word-template/templates
    /// </summary>
    [HttpGet("templates")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetTemplateList()
    {
      try
      {
        var list = new JArray();
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var rows = helper.Query("SELECT ID, TPMNAME FROM TSS_TEMPLATE WHERE ISUSE=1 ORDER BY TPMNAME");
          foreach (var row in rows)
          {
            list.Add(new JObject(
              new JProperty("key", row.ID + ""),
              new JProperty("title", row.TPMNAME + "")
            ));
          }
        }
        return Ok(new { templates = list });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetTemplateList 异常: " + ex.Message);
        return StatusCode(500, new { Message = "获取模版列表失败" });
      }
    }

    /// <summary>
    /// 查询 Word 模版定义列表（供选入弹窗使用）
    /// GET /api/word-template/list?keyword=xxx&templateType=xxx&moduleCode=xxx
    /// </summary>
    [HttpGet("list")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetWordTemplateList(string keyword, string templateType, string moduleCode)
    {
      try
      {
        var list = new JArray();
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          string sql = @"SELECT ID, TEMPLATENAME, TEMPLATETYPE, MODULECODE,
                                TEMPLATEID, FILEID, FILENAME, ISUSE, REMARK
                         FROM tbs_word_template
                         WHERE ISDELETED = 0 AND ISUSE = 1";
          if (!string.IsNullOrEmpty(keyword))
          {
            sql += " AND TEMPLATENAME LIKE CONCAT('%',@KEYWORD,'%')";
          }
          if (!string.IsNullOrEmpty(templateType))
          {
            sql += " AND TEMPLATETYPE = @TEMPLATETYPE";
          }
          if (!string.IsNullOrEmpty(moduleCode))
          {
            sql += " AND MODULECODE = @MODULECODE";
          }
          sql += " ORDER BY CREATEDATE DESC";

          var rows = helper.Query(sql, new
          {
            KEYWORD = keyword ?? "",
            TEMPLATETYPE = templateType ?? "",
            MODULECODE = moduleCode ?? ""
          });
          foreach (var row in rows)
          {
            list.Add(new JObject(
              new JProperty("ID", row.ID + ""),
              new JProperty("TEMPLATENAME", row.TEMPLATENAME + ""),
              new JProperty("TEMPLATETYPE", row.TEMPLATETYPE + ""),
              new JProperty("MODULECODE", row.MODULECODE + ""),
              new JProperty("TEMPLATEID", row.TEMPLATEID + ""),
              new JProperty("FILEID", row.FILEID + ""),
              new JProperty("FILENAME", row.FILENAME + ""),
              new JProperty("ISUSE", row.ISUSE + ""),
              new JProperty("REMARK", row.REMARK + "")
            ));
          }
        }
        return Ok(new { success = true, data = list });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate GetWordTemplateList 异常: " + ex.Message);
        return Ok(new { success = false, data = new JArray() });
      }
    }

    // 系统字段（不在模版中暴露）：只排除纯技术字段（主键/外键/附件/逻辑删除）
    // 制单人/审核人/时间/状态等审计字段放开——证书/报告模版中制单人、签章日期、状态很常用
    private static readonly HashSet<string> _systemFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "ID", "REFID", "REFBILLID", "REFBILLCODE", "ORECORDID", "REFRESID",
      "DOCCODE", "FILEID", "FILENAME", "FILESIZE", "FILEPATH", "ISDELETED"
    };

    private bool IsSystemField(string fieldName)
    {
      if (string.IsNullOrEmpty(fieldName)) return true;
      return _systemFieldNames.Contains(fieldName);
    }

    /// <summary>
    /// 查询某个资源的字段列表（resfield LEFT JOIN resuipc 获取 label）
    /// </summary>
    private List<ResourceFieldInfo> QueryResourceFields(DBHelper helper, string resourceId)
    {
      var list = new List<ResourceFieldInfo>();
      string sql = @"SELECT f.FIELDNAME AS FIELDNAME, f.FIELDTYPE AS FIELDTYPE,
                            u.LABELNAME AS LABELNAME, u.EDITSORT AS EDITSORT, u.LISTSORT AS LISTSORT
                     FROM tss_resfield f
                     LEFT JOIN tss_resuipc u ON u.RESFIELDID = f.ID
                     WHERE f.RESOURCEID = @rid
                     ORDER BY u.EDITSORT ASC, u.LISTSORT ASC, f.FIELDNAME ASC";
      var rows = helper.Query(sql, new { rid = resourceId });
      foreach (var row in rows)
      {
        string fieldName = (row.FIELDNAME ?? "") + "";
        if (IsSystemField(fieldName)) continue;
        string fieldType = (row.FIELDTYPE ?? "") + "";
        string label = row.LABELNAME == null ? "" : (row.LABELNAME + "");
        list.Add(new ResourceFieldInfo
        {
          FieldName = fieldName,
          FieldType = fieldType,
          Label = label
        });
      }
      return list;
    }

    private class ResourceFieldInfo
    {
      public string FieldName { get; set; }
      public string FieldType { get; set; }
      public string Label { get; set; }
    }

    private class ModulePathInfo
    {
      public string PathName { get; set; }
      public string ResourceId { get; set; }
      public string ResourceName { get; set; }
      public string Remark { get; set; }
    }

    private JArray GetOrmFields(string moduleCode)
    {
      var fields = new JArray();
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 1. 查询模块的所有主表/子表数据源
          string pathSql = @"SELECT MP.PATHNAME AS PATHNAME, MP.RESOURCEID AS RESOURCEID,
                                    MP.REMARK AS REMARK, R.RESOURCENAME AS RESOURCENAME
                             FROM tss_moudlepath MP
                             LEFT JOIN tss_resource R ON R.ID = MP.RESOURCEID
                             WHERE MP.MODULEID = (SELECT ID FROM tss_moudle WHERE MODULECODE = @mc)
                             AND MP.PATHNAME IN ('MAIN', 'DTSA', 'DTSB', 'DTSC', 'DTSD', 'DTSE', 'DTSF')
                             ORDER BY MP.PATHNAME";
          var pathRows = helper.Query(pathSql, new { mc = moduleCode });
          var paths = new List<ModulePathInfo>();
          foreach (var row in pathRows)
          {
            paths.Add(new ModulePathInfo
            {
              PathName = (row.PATHNAME ?? "") + "",
              ResourceId = (row.RESOURCEID ?? "") + "",
              ResourceName = row.RESOURCENAME == null ? "" : (row.RESOURCENAME + ""),
              Remark = row.REMARK == null ? "" : (row.REMARK + "")
            });
          }

          var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

          // 2. 主表字段（MAIN）
          var mainPath = paths.FirstOrDefault(p => p.PathName == "MAIN");
          if (mainPath != null)
          {
            var mainFields = QueryResourceFields(helper, mainPath.ResourceId);
            foreach (var f in mainFields)
            {
              if (seenKeys.Contains(f.FieldName)) continue;
              seenKeys.Add(f.FieldName);
              fields.Add(new JObject(
                new JProperty("key", f.FieldName),
                new JProperty("label", string.IsNullOrEmpty(f.Label) ? f.FieldName : f.Label),
                new JProperty("type", MapFieldType(f.FieldType)),
                new JProperty("source", "orm")
              ));
            }
          }

          // 3. 子表字段（DTSA/B/C/D/E/F）— 作为 table 类型字段
          var subPaths = paths.Where(p => p.PathName.StartsWith("DTS")).OrderBy(p => p.PathName);
          foreach (var subPath in subPaths)
          {
            var subFields = QueryResourceFields(helper, subPath.ResourceId);
            if (subFields.Count == 0) continue;

            var children = new JArray();
            foreach (var f in subFields)
            {
              children.Add(new JObject(
                new JProperty("key", f.FieldName),
                new JProperty("label", string.IsNullOrEmpty(f.Label) ? f.FieldName : f.Label),
                new JProperty("type", MapFieldType(f.FieldType))
              ));
            }

            // 标签优先使用 REMARK，其次 RESOURCENAME，最后 PATHNAME
            string tableLabel = !string.IsNullOrEmpty(subPath.Remark) ? subPath.Remark
                              : !string.IsNullOrEmpty(subPath.ResourceName) ? subPath.ResourceName
                              : subPath.PathName;
            // table key 使用 PATHNAME（如 DTSA_TABLE），与替换引擎 _TABLE 后缀约定一致
            string tableKey = subPath.PathName + "_TABLE";

            fields.Add(new JObject(
              new JProperty("key", tableKey),
              new JProperty("label", tableLabel + " (子表)"),
              new JProperty("type", "table"),
              new JProperty("source", "orm"),
              new JProperty("children", children)
            ));
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("GetOrmFields 异常: " + ex.Message + "\n" + ex.StackTrace);
      }
      return fields;
    }

    /// <summary>
    /// 按资源名（如 VBS_ARD_4TPL）查询子表字段
    /// </summary>
    private JArray QuerySubTableFieldsByResourceName(string resourceName)
    {
      var childFields = new JArray();
      try
      {
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          // 查资源 ID
          string resSql = "SELECT ID FROM tss_resource WHERE RESOURCENAME = @rn LIMIT 1";
          var resRow = helper.QueryFirstOrDefault(resSql, new { rn = resourceName });
          if (resRow == null) return childFields;
          string resourceId = resRow.ID + "";

          var fieldList = QueryResourceFields(helper, resourceId);
          foreach (var f in fieldList)
          {
            childFields.Add(new JObject(
              new JProperty("key", f.FieldName),
              new JProperty("label", string.IsNullOrEmpty(f.Label) ? f.FieldName : f.Label),
              new JProperty("type", MapFieldType(f.FieldType))
            ));
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("QuerySubTableFieldsByResourceName 异常: " + ex.Message);
      }
      return childFields;
    }

    private JArray GetTemplateFields(string templateId)
    {
      var fields = new JArray();
      try
      {
        BaseModel MAIN = GetModel("", "VSS_TEMPLATE");
        Hashtable Params = new Hashtable();
        Params["FILTERCODE"] = "F00";
        Hashtable FilterParams = new Hashtable();
        FilterParams["ID"] = templateId;
        Params["FilterParams"] = FilterParams;
        MAIN.Open(GetQueryInfo(Params));

        if (MAIN.GetView().Count > 0)
        {
          string tpmData = MAIN.GetView()[0].GetString("TPMDATA");
          if (!string.IsNullOrEmpty(tpmData))
          {
            var items = JArray.Parse(tpmData);
            ExtractFieldsFromTemplate(items, fields);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("GetTemplateFields 异常: " + ex.Message);
      }
      return fields;
    }

    private void ExtractFieldsFromTemplate(JArray items, JArray result)
    {
      foreach (var item in items)
      {
        string type = item["type"]?.ToString();

        if (type == "itemField")
        {
          string field = item["field"]?.ToString();
          string label = item["labelProps"]?["label"]?.ToString();
          string fieldType = item["fieldType"]?.ToString() ?? "text";
          if (!string.IsNullOrEmpty(field) && field != "NAME")
          {
            result.Add(new JObject(
              new JProperty("key", field),
              new JProperty("label", string.IsNullOrEmpty(label) ? field : label),
              new JProperty("type", MapFieldType(fieldType)),
              new JProperty("source", "template")
            ));
          }
        }
        else if (type == "itemEditor")
        {
          var editorFields = item["fields"] as JArray;
          if (editorFields != null)
          {
            foreach (var f in editorFields)
            {
              string field = f["field"]?.ToString();
              string name = f["name"]?.ToString();
              if (!string.IsNullOrEmpty(field))
              {
                result.Add(new JObject(
                  new JProperty("key", field),
                  new JProperty("label", string.IsNullOrEmpty(name) ? field : name),
                  new JProperty("type", "text"),
                  new JProperty("source", "template")
                ));
              }
            }
          }
        }
        else if (type == "itemTable")
        {
          string sourceName = item["sourceName"]?.ToString();
          if (!string.IsNullOrEmpty(sourceName))
          {
            var childFields = new JArray();

            // 优先使用 TPMDATA 中已有的 children（部分模版直接配置了子字段）
            var inlineChildren = item["children"] as JArray;
            if (inlineChildren != null && inlineChildren.Count > 0)
            {
              foreach (var child in inlineChildren)
              {
                string cField = child["field"]?.ToString();
                string cLabel = child["labelProps"]?["label"]?.ToString() ?? child["name"]?.ToString();
                if (!string.IsNullOrEmpty(cField) && cField != "NAME")
                {
                  childFields.Add(new JObject(
                    new JProperty("key", cField),
                    new JProperty("label", string.IsNullOrEmpty(cLabel) ? cField : cLabel),
                    new JProperty("type", "text")
                  ));
                }
              }
            }

            // 若 TPMDATA 无 children，则按 sourceName 查询数据库资源字段
            if (childFields.Count == 0)
            {
              childFields = QuerySubTableFieldsByResourceName(sourceName);
            }

            string tableTitle = item["title"]?.ToString();
            string tableLabel = !string.IsNullOrEmpty(tableTitle) && tableTitle != "表格"
                              ? tableTitle
                              : sourceName;

            result.Add(new JObject(
              new JProperty("key", sourceName + "_TABLE"),
              new JProperty("label", tableLabel + " (子表)"),
              new JProperty("type", "table"),
              new JProperty("source", "template"),
              new JProperty("sourceName", sourceName),
              new JProperty("children", childFields)
            ));
          }
        }
        else if (type == "itemCheckBox")
        {
          string field = item["field"]?.ToString();
          if (!string.IsNullOrEmpty(field) && field != "NAME")
          {
            result.Add(new JObject(
              new JProperty("key", field),
              new JProperty("label", field),
              new JProperty("type", "text"),
              new JProperty("source", "template")
            ));
          }
        }

        // 递归处理子元素
        var subChildren = item["children"] as JArray;
        if (subChildren != null && type != "itemTable")
        {
          ExtractFieldsFromTemplate(subChildren, result);
        }
      }
    }

    private JArray GetSystemFields(string type)
    {
      var fields = new JArray();

      // 签名图片字段
      fields.Add(new JObject(
        new JProperty("key", "CREATER_IMG"),
        new JProperty("label", "提交人签名"),
        new JProperty("type", "image"),
        new JProperty("source", "system")
      ));
      fields.Add(new JObject(
        new JProperty("key", "CHECKER_IMG"),
        new JProperty("label", "审核人签名"),
        new JProperty("type", "image"),
        new JProperty("source", "system")
      ));
      fields.Add(new JObject(
        new JProperty("key", "VERIFIER_IMG"),
        new JProperty("label", "审批人签名"),
        new JProperty("type", "image"),
        new JProperty("source", "system")
      ));

      // 二维码
      fields.Add(new JObject(
        new JProperty("key", "CHECKQR_IMG2"),
        new JProperty("label", "验证二维码"),
        new JProperty("type", "image"),
        new JProperty("source", "system")
      ));

      // 日期拆分（通用）
      fields.Add(new JObject(
        new JProperty("key", "_YY"),
        new JProperty("label", "日期后缀-年（如 SIGNDATE_YY）"),
        new JProperty("type", "suffix"),
        new JProperty("source", "system")
      ));
      fields.Add(new JObject(
        new JProperty("key", "_MM"),
        new JProperty("label", "日期后缀-月（如 SIGNDATE_MM）"),
        new JProperty("type", "suffix"),
        new JProperty("source", "system")
      ));
      fields.Add(new JObject(
        new JProperty("key", "_DD"),
        new JProperty("label", "日期后缀-日（如 SIGNDATE_DD）"),
        new JProperty("type", "suffix"),
        new JProperty("source", "system")
      ));

      return fields;
    }

    private string MapFieldType(string fieldType)
    {
      if (string.IsNullOrEmpty(fieldType)) return "text";
      switch (fieldType.ToLower())
      {
        case "date":
        case "datetime":
          return "date";
        default:
          return "text";
      }
    }

    #endregion

    #region 私有方法 - 模版解析

    /// <summary>
    /// 解析 docx 中的 Content Control (SDT) 字段
    /// </summary>
    private List<FieldDefinition> ParseContentControls(string docxPath)
    {
      var fields = new List<FieldDefinition>();
      try
      {
        using (var doc = WordprocessingDocument.Open(docxPath, false))
        {
          var sdts = doc.MainDocumentPart.Document.Body.Descendants<SdtElement>();
          foreach (var sdt in sdts)
          {
            // v2.9.1 兼容：用 GetFirstChild<Tag>() 代替 GetTag()
            var tagElement = sdt.SdtProperties?.GetFirstChild<Tag>();
            var aliasElement = sdt.SdtProperties?.GetFirstChild<SdtAlias>();
            string tag = tagElement?.Val?.Value;
            string title = aliasElement?.Val?.Value;

            if (!string.IsNullOrEmpty(tag))
            {
              string baseName = tag;
              string suffix = "";
              var parts = tag.Split('_');
              if (parts.Length > 1)
              {
                string lastPart = parts[parts.Length - 1];
                if (lastPart == "YY" || lastPart == "MM" || lastPart == "DD" ||
                    lastPart == "IMG" || lastPart == "IMG2" || lastPart == "HTML" || lastPart == "TABLE")
                {
                  baseName = string.Join("_", parts.Take(parts.Length - 1));
                  suffix = lastPart;
                }
              }

              var field = new FieldDefinition
              {
                Key = tag,
                Label = title ?? tag,
                BaseType = DetermineFieldTypeFromSuffix(suffix, sdt)
              };

              // 提取 _TABLE 类型 SDT 的子字段
              if (suffix == "TABLE")
              {
                // 优先 SDT 内部的子 SDT（SDT 包裹整行的情况）
                var childSdts = sdt.Descendants<SdtElement>().Where(e => e != sdt).ToList();

                // 若 SDT 内部无子 SDT（OnlyOffice 的 Block SDT 在单元格内，是段落级），
                // 向上找到表格行，收集同行所有单元格的子字段 SDT
                if (childSdts.Count == 0)
                {
                  var ancestorRow = sdt.Ancestors<TableRow>().FirstOrDefault();
                  if (ancestorRow != null)
                  {
                    childSdts = ancestorRow.Descendants<SdtElement>()
                      .Where(e => e != sdt)
                      .Where(e =>
                      {
                        // 排除其他 _TABLE 标记 SDT，只保留普通子字段
                        var t = e.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                        return !string.IsNullOrEmpty(t) && !t.EndsWith("_TABLE");
                      })
                      .ToList();
                  }
                }

                foreach (var childSdt in childSdts)
                {
                  var childTagEl = childSdt.SdtProperties?.GetFirstChild<Tag>();
                  var childAliasEl = childSdt.SdtProperties?.GetFirstChild<SdtAlias>();
                  string childTag = childTagEl?.Val?.Value;
                  string childTitle = childAliasEl?.Val?.Value;
                  if (!string.IsNullOrEmpty(childTag))
                  {
                    string childBaseName = childTag;
                    string childSuffix = "";
                    var childParts = childTag.Split('_');
                    if (childParts.Length > 1)
                    {
                      string childLast = childParts[childParts.Length - 1];
                      if (childLast == "YY" || childLast == "MM" || childLast == "DD" ||
                          childLast == "IMG" || childLast == "IMG2" || childLast == "HTML")
                      {
                        childBaseName = string.Join("_", childParts.Take(childParts.Length - 1));
                        childSuffix = childLast;
                      }
                    }
                    field.Children.Add(new FieldDefinition
                    {
                      Key = childTag,
                      Label = childTitle ?? childTag,
                      BaseType = DetermineFieldTypeFromSuffix(childSuffix, childSdt)
                    });
                  }
                }
              }

              fields.Add(field);
            }
          }

          // 同时检查 Header 中的 SDT
          foreach (var header in doc.MainDocumentPart.HeaderParts)
          {
            var headerSdts = header.Header.Descendants<SdtElement>();
            foreach (var sdt in headerSdts)
            {
              var tagElement2 = sdt.SdtProperties?.GetFirstChild<Tag>();
              var aliasElement2 = sdt.SdtProperties?.GetFirstChild<SdtAlias>();
              string tag = tagElement2?.Val?.Value;
              string title = aliasElement2?.Val?.Value;
              if (!string.IsNullOrEmpty(tag))
              {
                fields.Add(new FieldDefinition
                {
                  Key = tag,
                  Label = title ?? tag,
                  BaseType = "text"
                });
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("ParseContentControls 异常: " + ex.Message);
      }
      return fields;
    }

    /// <summary>
    /// 解析 docx 中的 Bookmark 字段（兼容旧模版）
    /// </summary>
    private List<FieldDefinition> ParseBookmarks(string docxPath)
    {
      var fields = new List<FieldDefinition>();
      try
      {
        using (var doc = WordprocessingDocument.Open(docxPath, false))
        {
          var bookmarks = doc.MainDocumentPart.Document.Body.Descendants<BookmarkStart>();
          foreach (var bm in bookmarks)
          {
            string name = bm.Name?.Value;
            if (string.IsNullOrEmpty(name) || name.StartsWith("_")) continue;

            string baseName = name;
            string suffix = "";
            var parts = name.Split('_');
            if (parts.Length > 1)
            {
              string lastPart = parts[parts.Length - 1];
              if (lastPart == "YY" || lastPart == "MM" || lastPart == "DD" ||
                  lastPart == "IMG" || lastPart == "IMG2" || lastPart == "HTML" || lastPart == "TABLE")
              {
                baseName = string.Join("_", parts.Take(parts.Length - 1));
                suffix = lastPart;
              }
            }

            fields.Add(new FieldDefinition
            {
              Key = name,
              Label = name,
              BaseType = DetermineFieldTypeFromSuffix(suffix, null)
            });
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("ParseBookmarks 异常: " + ex.Message);
      }
      return fields;
    }

    // v2.9.1 兼容的字段类型判断（使用 SdtContentPicture 属性代替 SdtPicture 类型）
    private string DetermineFieldTypeFromSuffix(string suffix, SdtElement sdt)
    {
      if (suffix == "IMG" || suffix == "IMG2") return "image";
      if (suffix == "HTML") return "html";
      if (suffix == "TABLE") return "table";
      if (suffix == "YY" || suffix == "MM" || suffix == "DD") return "date";
      // v2.9.1: 检查 SdtContentPicture 属性元素是否存在
      if (sdt.SdtProperties?.GetFirstChild<SdtContentPicture>() != null) return "image";
      return "text";
    }

    #endregion

    #region Bookmark → SDT 迁移

    /// <summary>
    /// 将 Bookmark 模版迁移为 Content Control (SDT) 模版
    /// POST /api/word-template/migrate-bookmarks/{fileId}
    /// </summary>
    [HttpPost("migrate-bookmarks/{fileId}")]
    [EnableCors("AllowHeaders")]
    public IActionResult MigrateBookmarks(string fileId)
    {
      try
      {
        // 查询文件信息
        Hashtable Params = new Hashtable();
        Params["FILTERCODE"] = "F00";
        Hashtable FilterParams = new Hashtable();
        FilterParams["ID"] = fileId;
        Params["FilterParams"] = FilterParams;
        BaseModel MAIN = GetModel("", "VSS_FILES");
        MAIN.Open(GetQueryInfo(Params));

        if (MAIN.GetView().Count == 0)
        {
          return NotFound(new { Message = "文件不存在" });
        }

        ViewRow row = MAIN.GetView()[0];
        string FILENAME = row.GetString("FILENAME");
        string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
        string FilePath = rootPath + row.GetString("FILEPATH").Replace('\\', '/');

        if (!System.IO.File.Exists(FilePath))
        {
          return NotFound(new { Message = "文件不存在于磁盘" });
        }

        // 迁移前先解析 Bookmark 数量
        var bookmarksBefore = ParseBookmarks(FilePath);
        var sdtsBefore = ParseContentControls(FilePath);

        // 创建备份文件（原文件名 + .bak）
        string backupPath = FilePath + ".bak";
        try
        {
          System.IO.File.Copy(FilePath, backupPath, true);
        }
        catch (Exception ex)
        {
          Logger.Info("WordTemplate MigrateBookmarks: 创建备份失败 - " + ex.Message);
        }

        // 执行迁移
        int migratedCount = 0;
        int skippedCount = 0;
        var migratedFields = new JArray();

        using (var doc = WordprocessingDocument.Open(FilePath, true))
        {
          var body = doc.MainDocumentPart.Document.Body;

          // 收集所有 BookmarkStart 和对应的 BookmarkEnd
          var bookmarkPairs = new List<BookmarkPairInfo>();
          var bookmarkStarts = body.Descendants<BookmarkStart>().ToList();
          var bookmarkEnds = body.Descendants<BookmarkEnd>().ToList();

          foreach (var bmStart in bookmarkStarts)
          {
            string name = bmStart.Name?.Value;
            if (string.IsNullOrEmpty(name) || name.StartsWith("_")) continue;

            // 查找对应的 BookmarkEnd
            var bmEnd = bookmarkEnds.FirstOrDefault(e => e.Id.Value == bmStart.Id.Value);

            bookmarkPairs.Add(new BookmarkPairInfo
            {
              Start = bmStart,
              End = bmEnd,
              Name = name
            });
          }

          // 对每个 Bookmark 创建对应的 SDT
          foreach (var pair in bookmarkPairs)
          {
            try
            {
              // 解析后缀确定 SDT 类型
              string suffix = "";
              var parts = pair.Name.Split('_');
              if (parts.Length > 1)
              {
                string lastPart = parts[parts.Length - 1];
                if (lastPart == "YY" || lastPart == "MM" || lastPart == "DD" ||
                    lastPart == "IMG" || lastPart == "IMG2" || lastPart == "HTML" || lastPart == "TABLE")
                {
                  suffix = lastPart;
                }
              }

              // 获取 Bookmark 的父元素
              var parent = pair.Start.Parent;
              if (parent == null)
              {
                skippedCount++;
                continue;
              }

              // 收集 Bookmark 范围内的所有元素
              var elementsInRange = new List<OpenXmlElement>();
              bool inRange = false;
              foreach (var child in parent.Elements())
              {
                if (child == pair.Start || (child.Descendants<BookmarkStart>().Any(b => b.Id.Value == pair.Start.Id.Value)))
                {
                  inRange = true;
                }
                if (inRange)
                {
                  elementsInRange.Add(child);
                }
                if (child == pair.End || (pair.End != null && child.Descendants<BookmarkEnd>().Any(b => b.Id.Value == pair.End.Id.Value)))
                {
                  break;
                }
              }

              if (elementsInRange.Count == 0)
              {
                skippedCount++;
                continue;
              }

              // 创建 SDT
              SdtElement sdt;
              OpenXmlElement sdtContent;

              if (suffix == "TABLE")
              {
                // 表格类型：Block 级 SDT
                var blockSdt = new SdtBlock();
                var blockContent = new SdtContentBlock();
                blockSdt.SdtContentBlock = blockContent;
                sdt = blockSdt;
                sdtContent = blockContent;
              }
              else if (suffix == "IMG" || suffix == "IMG2")
              {
                // 图片类型：Inline 级 SDT（带 SdtContentPicture 标记）
                var runSdt = new SdtRun();
                var runContent = new SdtContentRun();
                runSdt.SdtContentRun = runContent;
                // 添加 SdtContentPicture 属性标记
                runSdt.SdtProperties.AppendChild(new SdtContentPicture());
                sdt = runSdt;
                sdtContent = runContent;
              }
              else
              {
                // 文本/日期/富文本：Inline 级 SDT
                var runSdt = new SdtRun();
                var runContent = new SdtContentRun();
                runSdt.SdtContentRun = runContent;
                sdt = runSdt;
                sdtContent = runContent;
              }

              // 设置 Tag 和 Alias
              sdt.SdtProperties.AppendChild(new Tag { Val = pair.Name });
              sdt.SdtProperties.AppendChild(new SdtAlias { Val = pair.Name });

              // 将 Bookmark 范围内的内容（排除 BookmarkStart/End 本身）移入 SDT
              foreach (var elem in elementsInRange)
              {
                if (elem is BookmarkStart || elem is BookmarkEnd) continue;
                // 克隆元素并添加到 SDT 内容
                var clone = elem.CloneNode(true);
                // 移除克隆中的 BookmarkStart/End
                foreach (var bm in clone.Descendants<BookmarkStart>().ToList())
                {
                  bm.Remove();
                }
                foreach (var bm in clone.Descendants<BookmarkEnd>().ToList())
                {
                  bm.Remove();
                }
                if (sdtContent is SdtContentBlock blockC)
                {
                  if (clone is Paragraph p)
                  {
                    blockC.AppendChild(p);
                  }
                  else if (clone is TableRow tr)
                  {
                    // 表格行需要包裹在 Table 中
                    blockC.AppendChild(clone);
                  }
                  else
                  {
                    blockC.AppendChild(clone);
                  }
                }
                else if (sdtContent is SdtContentRun runC)
                {
                  if (clone is Run r)
                  {
                    runC.AppendChild(r);
                  }
                  else if (clone is Paragraph para)
                  {
                    // 如果是段落，取其中的 Run
                    foreach (var run in para.Elements<Run>())
                    {
                      runC.AppendChild((Run)run.CloneNode(true));
                    }
                  }
                }
              }

              // 在 Bookmark 起始位置前插入 SDT
              pair.Start.InsertBeforeSelf(sdt);

              // 删除原 Bookmark 范围内的元素
              foreach (var elem in elementsInRange)
              {
                elem.Remove();
              }

              migratedCount++;
              migratedFields.Add(new JObject(
                new JProperty("key", pair.Name),
                new JProperty("type", DetermineFieldTypeFromSuffix(suffix, sdt)),
                new JProperty("suffix", suffix)
              ));
            }
            catch (Exception ex)
            {
              Logger.Info("WordTemplate MigrateBookmarks: 迁移 Bookmark '" + pair.Name + "' 失败 - " + ex.Message);
              skippedCount++;
            }
          }

          // 同时处理 Header 中的 Bookmark
          foreach (var header in doc.MainDocumentPart.HeaderParts)
          {
            var headerBookmarks = header.Header.Descendants<BookmarkStart>().ToList();
            var headerBookmarkEnds = header.Header.Descendants<BookmarkEnd>().ToList();

            foreach (var bmStart in headerBookmarks)
            {
              string name = bmStart.Name?.Value;
              if (string.IsNullOrEmpty(name) || name.StartsWith("_")) continue;

              try
              {
                var bmEnd = headerBookmarkEnds.FirstOrDefault(e => e.Id.Value == bmStart.Id.Value);
                var parent = bmStart.Parent;
                if (parent == null) continue;

                // Header 中创建简单的 Run 级 SDT
                var runSdt = new SdtRun();
                var runContent = new SdtContentRun();
                runSdt.SdtContentRun = runContent;
                runSdt.SdtProperties.AppendChild(new Tag { Val = name });
                runSdt.SdtProperties.AppendChild(new SdtAlias { Val = name });

                // 添加占位文本
                var placeholderRun = new Run(new Text("[" + name + "]"));
                runContent.AppendChild(placeholderRun);

                bmStart.InsertBeforeSelf(runSdt);

                // 删除 Bookmark 范围
                var elementsInRange = new List<OpenXmlElement>();
                bool inRange = false;
                foreach (var child in parent.Elements())
                {
                  if (child == bmStart) inRange = true;
                  if (inRange) elementsInRange.Add(child);
                  if (child == bmEnd) break;
                }
                foreach (var elem in elementsInRange)
                {
                  elem.Remove();
                }

                migratedCount++;
              }
              catch (Exception ex)
              {
                Logger.Info("WordTemplate MigrateBookmarks: Header Bookmark '" + name + "' 迁移失败 - " + ex.Message);
                skippedCount++;
              }
            }
          }

          doc.MainDocumentPart.Document.Save();
        }

        // 迁移后解析验证
        var sdtsAfter = ParseContentControls(FilePath);
        var bookmarksAfter = ParseBookmarks(FilePath);

        return Ok(new
        {
          success = true,
          migratedCount,
          skippedCount,
          migratedFields,
          before = new { bookmarks = bookmarksBefore.Count, contentControls = sdtsBefore.Count },
          after = new { bookmarks = bookmarksAfter.Count, contentControls = sdtsAfter.Count },
          backupPath
        });
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate MigrateBookmarks 异常: " + ex.Message + "\n" + ex.StackTrace);
        return StatusCode(500, new { Message = "迁移失败: " + ex.Message });
      }
    }

    private class BookmarkPairInfo
    {
      public BookmarkStart Start { get; set; }
      public BookmarkEnd End { get; set; }
      public string Name { get; set; }
    }

    #endregion

    #region 模版复制

    /// <summary>
    /// 复制 Word 模版定义（模版记录与模版文件均生成独立副本）
    /// POST /api/word-template/copy/{templateId}
    /// </summary>
    [HttpPost("copy/{templateId}")]
    [EnableCors("AllowHeaders")]
    public IActionResult CopyTemplate(string templateId)
    {
      try
      {
        // 1. 查询模版记录
        DBHelper helper = DB.GetDBHelper();
        using (helper)
        {
          var tpl = helper.QueryFirstOrDefault(
            "SELECT ID, TEMPLATENAME, TEMPLATETYPE, MODULECODE, TEMPLATEID, FILEID, FILENAME, FIELDBINDINGS, REMARK, ISUSE " +
            "FROM tbs_word_template WHERE ID=@ID AND ISDELETED=0 LIMIT 1",
            new { ID = templateId });
          if (tpl == null)
          {
            return NotFound(new { success = false, Message = "模版不存在" });
          }
          string srcFileId = tpl.FILEID + "";
          if (string.IsNullOrEmpty(srcFileId))
          {
            return Ok(new { success = false, Message = "该模版未上传文件，无法复制" });
          }

          // 2. 查询源文件记录
          var srcFile = helper.QueryFirstOrDefault(
            "SELECT ID, FILENAME, FILEPATH, FILESIZE FROM tss_files WHERE ID=@ID LIMIT 1",
            new { ID = srcFileId });
          if (srcFile == null)
          {
            return Ok(new { success = false, Message = "模版文件记录不存在" });
          }
          string rootPath = Realso.Utils.ConfigHelper.GetConfig("Upload:ROOT");
          // 兼容历史 Windows 生成的含 \ 路径
          string srcRelPath = (srcFile.FILEPATH + "").Replace('\\', '/');
          string srcPath = rootPath + srcRelPath;
          if (!System.IO.File.Exists(srcPath))
          {
            return Ok(new { success = false, Message = "模版文件不存在于磁盘" });
          }

          // 3. 复制物理文件到同目录，文件名加时间戳避免冲突
          string srcDir = Path.GetDirectoryName(srcPath);
          string srcExt = Path.GetExtension(srcPath);
          string srcNameNoExt = Path.GetFileNameWithoutExtension(srcPath);
          string copyNameNoExt = srcNameNoExt + "_copy_" + DateTime.Now.ToString("yyyyMMddHHmmss");
          string copyFileName = copyNameNoExt + srcExt;
          string copyPath = Path.Combine(srcDir, copyFileName);
          System.IO.File.Copy(srcPath, copyPath, false);

          // 4. 写入 tss_files 文件记录
          string newFileId = Guid.NewGuid().ToString("N");
          string copyRelativePath = srcRelPath.Replace(Path.GetFileName(srcRelPath), copyFileName);
          helper.Execute(
            "INSERT INTO tss_files (ID, FILENAME, FILEPATH, FILESIZE, CREATEDATE) VALUES (@ID, @NAME, @PATH, @SIZE, NOW())",
            new { ID = newFileId, NAME = copyFileName, PATH = copyRelativePath, SIZE = new System.IO.FileInfo(copyPath).Length });

          // 5. 写入 tbs_word_template 模版记录（名称追加"副本"）
          string newTplId = Guid.NewGuid().ToString("N");
          string newTplName = (tpl.TEMPLATENAME + "") + "（副本）";
          helper.Execute(
            "INSERT INTO tbs_word_template (ID, TEMPLATENAME, TEMPLATETYPE, MODULECODE, TEMPLATEID, FILEID, FILENAME, FIELDBINDINGS, REMARK, ISUSE, ISDELETED, CREATEDATE) " +
            "VALUES (@ID, @NAME, @TYPE, @MODULECODE, @TEMPLATEID, @FILEID, @FILENAME, @FIELDBINDINGS, @REMARK, @ISUSE, 0, NOW())",
            new
            {
              ID = newTplId,
              NAME = newTplName,
              TYPE = tpl.TEMPLATETYPE + "",
              MODULECODE = tpl.MODULECODE + "",
              TEMPLATEID = tpl.TEMPLATEID + "",
              FILEID = newFileId,
              FILENAME = copyFileName,
              FIELDBINDINGS = tpl.FIELDBINDINGS == null ? "" : (tpl.FIELDBINDINGS + ""),
              REMARK = tpl.REMARK + "",
              ISUSE = Convert.ToInt32(tpl.ISUSE)
            });

          return Ok(new { success = true, ID = newTplId, FILEID = newFileId });
        }
      }
      catch (Exception ex)
      {
        Logger.Info("WordTemplate CopyTemplate 异常: " + ex.Message + "\n" + ex.StackTrace);
        return StatusCode(500, new { success = false, Message = "复制失败: " + ex.Message });
      }
    }

    #endregion

    #region 数据模型

    public class FieldDefinition
    {
      public string Key { get; set; }
      public string Label { get; set; }
      public string BaseType { get; set; }
      public List<FieldDefinition> Children { get; set; } = new List<FieldDefinition>();
    }

    public class FieldInsertCommand
    {
      public string FieldKey { get; set; }
      public string FieldLabel { get; set; }
      public string FieldType { get; set; }
    }

    #endregion
  }
}
