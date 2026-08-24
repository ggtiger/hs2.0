using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using Realso.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.SS.Converter;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  public class ExcelEditorController : BaseControl
  {
    private readonly IHostingEnvironment _hostingEnvironment;
    private static readonly Dictionary<string, TempFileInfo> _tempFiles = new Dictionary<string, TempFileInfo>();

    public ExcelEditorController(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }

    /// <summary>
    /// 创建空白 xlsx 文件供 OnlyOffice 编辑
    /// POST /api/exceleditor/create-blank
    /// </summary>
    [HttpPost("create-blank")]
    [EnableCors("AllowHeaders")]
    public IActionResult CreateBlank([FromBody] JObject body)
    {
      try
      {
        string fileName = body?["fileName"]?.ToString() ?? "template.xlsx";

        string key = Guid.NewGuid().ToString("N");
        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        // 生成最简单的空白 xlsx 文件
        string xlsxFilePath = Path.Combine(tempDir, key + "_template.xlsx");
        byte[] blankXlsx = CreateBlankXlsx();
        System.IO.File.WriteAllBytes(xlsxFilePath, blankXlsx);

        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = xlsxFilePath,
          FileName = fileName,
          CreateTime = DateTime.Now
        };

        CleanupOldFiles();

        return Ok(new { key });
      }
      catch (Exception ex)
      {
        Logger.Info("CreateBlank 异常: " + ex.Message);
        return BadRequest(new { Message = "创建失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 上传 xlsx 文件（前端将 HTML 转 xlsx 后上传）
    /// POST /api/exceleditor/upload-xlsx
    /// </summary>
    [HttpPost("upload-xlsx")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> UploadXlsx()
    {
      try
      {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
          body = await reader.ReadToEndAsync();
        }

        var jobj = JObject.Parse(body);
        string base64Data = jobj["data"]?.ToString();
        string fileName = jobj["fileName"]?.ToString() ?? "template.xlsx";

        if (string.IsNullOrEmpty(base64Data))
        {
          return BadRequest(new { Message = "文件数据为空" });
        }

        // base64 数据可能包含 data:xxx;base64, 前缀
        if (base64Data.Contains(","))
        {
          base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
        }

        byte[] fileBytes = Convert.FromBase64String(base64Data);

        string key = Guid.NewGuid().ToString("N");
        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        string xlsxFilePath = Path.Combine(tempDir, key + "_template.xlsx");
        System.IO.File.WriteAllBytes(xlsxFilePath, fileBytes);

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = xlsxFilePath,
          FileName = fileName,
          CreateTime = DateTime.Now
        };

        CleanupOldFiles();

        return Ok(new { key });
      }
      catch (Exception ex)
      {
        Logger.Info("UploadXlsx 异常: " + ex.Message);
        return BadRequest(new { Message = "上传失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 将 HTML 表格转为 xlsx 文件供 OnlyOffice 编辑（后端用 NPOI 转换，保留完整样式）
    /// POST /api/exceleditor/html-to-xlsx
    /// </summary>
    [HttpPost("html-to-xlsx")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> HtmlToXlsx()
    {
      try
      {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
          body = await reader.ReadToEndAsync();
        }

        var jobj = JObject.Parse(body);
        string htmlContent = jobj["html"]?.ToString();
        string fileName = jobj["fileName"]?.ToString() ?? "template.xlsx";

        // 提取 fields 中的公式定义：field名 → formula
        var fieldFormulas = new Dictionary<string, string>();
        var fieldsArr = jobj["fields"] as JArray;
        if (fieldsArr != null)
        {
          foreach (var f in fieldsArr)
          {
            var fieldName = f["field"]?.ToString();
            var formula = f["formula"]?.ToString();
            if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(formula))
            {
              fieldFormulas[fieldName] = formula;
            }
          }
        }

        if (string.IsNullOrEmpty(htmlContent))
        {
          return BadRequest(new { Message = "HTML 内容为空" });
        }

        // 调试：保存原始 HTML 到临时文件
        try
        {
          string rootPath2 = ConfigHelper.GetConfig("Upload:ROOT");
          string tempDir2 = Path.Combine(rootPath2, "临时");
          if (!Directory.Exists(tempDir2)) Directory.CreateDirectory(tempDir2);
          string debugHtmlPath = Path.Combine(tempDir2, "debug_input_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html");
          var debugHtml = new StringBuilder();
          debugHtml.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
          debugHtml.AppendLine("<style>table { border-collapse: collapse; } td, th { border: 1px solid #999; padding: 2px 5px; }</style>");
          debugHtml.AppendLine("</head><body>");
          debugHtml.AppendLine(htmlContent);
          debugHtml.AppendLine("</body></html>");
          System.IO.File.WriteAllText(debugHtmlPath, debugHtml.ToString(), Encoding.UTF8);
          Logger.Info("HtmlToXlsx 调试: HTML 已保存到 " + debugHtmlPath);
        }
        catch (Exception debugEx)
        {
          Logger.Info("HtmlToXlsx 调试保存失败: " + debugEx.Message);
        }

        // 使用 NPOI 将 HTML 转为 xlsx
        byte[] xlsxBytes = HtmlToXlsx(htmlContent, fieldFormulas);

        string key = Guid.NewGuid().ToString("N");
        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        string xlsxFilePath = Path.Combine(tempDir, key + "_template.xlsx");
        System.IO.File.WriteAllBytes(xlsxFilePath, xlsxBytes);

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = xlsxFilePath,
          FileName = fileName,
          CreateTime = DateTime.Now
        };

        CleanupOldFiles();

        return Ok(new { key });
      }
      catch (Exception ex)
      {
        Logger.Info("HtmlToXlsx 异常: " + ex.Message);
        return BadRequest(new { Message = "HTML转Excel失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 上传 xlsx 文件（直接上传文件）
    /// POST /api/exceleditor/upload
    /// </summary>
    [HttpPost("upload")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> Upload()
    {
      var files = Request.Form.Files;
      if (files.Count == 0)
      {
        return BadRequest(new { Message = "未提供文件" });
      }

      var formFile = files[0];
      if (formFile.Length > 0)
      {
        string key = Guid.NewGuid().ToString("N");
        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        string fileName = key + "_" + formFile.FileName;
        string filePath = Path.Combine(tempDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
          await formFile.CopyToAsync(stream);
        }

        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
        string downloadUrl = apiUrl + "/api/exceleditor/download?key=" + key;

        _tempFiles[key] = new TempFileInfo
        {
          FilePath = filePath,
          FileName = formFile.FileName,
          CreateTime = DateTime.Now
        };

        CleanupOldFiles();

        return Ok(new { key, downloadUrl, fileName = formFile.FileName });
      }

      return BadRequest(new { Message = "文件为空" });
    }

    /// <summary>
    /// 下载临时文件（供 OnlyOffice Document Server 调用）
    /// GET /api/exceleditor/download?key=xxx
    /// </summary>
    [HttpGet("download")]
    [EnableCors("AllowHeaders")]
    public IActionResult Download(string key)
    {
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      var info = _tempFiles[key];

      if (!System.IO.File.Exists(info.FilePath))
      {
        return NotFound(new { Message = "文件不存在" });
      }

      var stream = System.IO.File.OpenRead(info.FilePath);
      return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", info.FileName);
    }

    /// <summary>
    /// OnlyOffice 保存回调
    /// POST /api/exceleditor/callback
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

        Logger.Info("ExcelEditor Callback: status=" + status + ", key=" + (key ?? "null") + ", url=" + (url ?? "null"));

        // OnlyOffice callback 的 key 可能带时间戳后缀（如 abc123_639164311000284920），
        // 而 _tempFiles 中存储的是不带后缀的原始 key（abc123），需要提取
        var lookupKey = key;
        if (!string.IsNullOrEmpty(key) && !_tempFiles.ContainsKey(key))
        {
          var underscoreIdx = key.LastIndexOf('_');
          if (underscoreIdx > 0)
          {
            var baseKey = key.Substring(0, underscoreIdx);
            if (_tempFiles.ContainsKey(baseKey))
            {
              lookupKey = baseKey;
              Logger.Info("ExcelEditor Callback: matched baseKey=" + baseKey);
            }
          }
        }

        if (string.IsNullOrEmpty(lookupKey) || !_tempFiles.ContainsKey(lookupKey))
        {
          Logger.Info("ExcelEditor Callback: key not found in _tempFiles, keys=" + string.Join(",", _tempFiles.Keys));
          return Ok(new { error = 0 });
        }

        var info = _tempFiles[lookupKey];

        // status 2: 文档关闭保存, status 6: 强制保存, status 4: 文档关闭无修改
        if (status == 2 || status == 6)
        {
          if (!string.IsNullOrEmpty(url))
          {
            // OnlyOffice callback 中的 url 可能是容器内部地址（如 http://onlyoffice-ds/...），
            // 后端在宿主机上无法访问，需要替换为宿主机地址
            var downloadUrl = url;
            var docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl") ?? "http://localhost:8088";
            // 替换容器内部域名为宿主机地址
            try
            {
              var uri = new Uri(url);
              if (uri.Host != "localhost" && uri.Host != "127.0.0.1" && uri.Host != "host.docker.internal")
              {
                var newUri = new UriBuilder(url) { Host = new Uri(docServerUrl).Host, Port = new Uri(docServerUrl).Port }.Uri;
                downloadUrl = newUri.ToString();
                Logger.Info("ExcelEditor Callback: rewritten url from " + url + " to " + downloadUrl);
              }
            }
            catch { /* url 解析失败就用原始 url */ }

            Logger.Info("ExcelEditor Callback: downloading from " + downloadUrl);
            using (var client = new HttpClient())
            {
              client.Timeout = TimeSpan.FromSeconds(30);
              var response = await client.GetAsync(downloadUrl);
              if (response.IsSuccessStatusCode)
              {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.IO.File.WriteAllBytes(info.FilePath, bytes);
                info.SavedByCallback = true;
                Logger.Info("ExcelEditor Callback: saved " + bytes.Length + " bytes to " + info.FilePath);
              }
              else
              {
                Logger.Info("ExcelEditor Callback: download failed, status=" + response.StatusCode);
              }
            }
          }
          else
          {
            Logger.Info("ExcelEditor Callback: status=" + status + " but no url");
          }
        }
        else if (status == 4)
        {
          // status=4: 文档关闭但无修改，直接使用原始文件
          info.CallbackReceived = true;
          Logger.Info("ExcelEditor Callback: status=4, no changes, using original file");
        }

        // status=2/6 成功保存后也标记 CallbackReceived
        if (status == 2 || status == 6)
        {
          info.CallbackReceived = true;
        }

        return Ok(new { error = 0 });
      }
      catch (Exception ex)
      {
        Logger.Info($"ExcelEditor Callback 异常: {ex.Message}");
        return Ok(new { error = 0 });
      }
    }

    /// <summary>
    /// 获取 OnlyOffice 编辑器配置（编辑模式）
    /// GET /api/exceleditor/editor-config?key=xxx
    /// </summary>
    [HttpGet("editor-config")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetEditorConfig(string key)
    {
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      var info = _tempFiles[key];
      string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
      string fileUrl = apiUrl + "/api/exceleditor/download?key=" + key;
      string callbackUrl = apiUrl + "/api/exceleditor/callback";
      // docKey 加时间戳确保 OnlyOffice 每次加载最新版本
      string docKey = key + "_" + DateTime.Now.Ticks;
      info.DocKey = docKey;

      var config = new JObject();
      var document = new JObject();
      document["fileType"] = "xlsx";
      document["key"] = docKey;
      document["title"] = info.FileName;
      document["url"] = fileUrl;
      var permissions = new JObject();
      permissions["edit"] = true;
      permissions["download"] = true;
      permissions["print"] = false;
      document["permissions"] = permissions;
      config["document"] = document;

      config["documentType"] = "cell";

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

      config["editorConfig"] = editorConfig;

      return Ok(config);
    }

    /// <summary>
    /// 导出编辑后的 xlsx 内容为 HTML
    /// 后端直接解析 xlsx XML 生成带完整样式的 HTML
    /// GET /api/exceleditor/export-html?key=xxx
    /// </summary>
    [HttpGet("export-html")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> ExportHtml(string key)
    {
      Logger.Info("ExportHtml: key=" + (key ?? "null"));
      if (string.IsNullOrEmpty(key) || !_tempFiles.ContainsKey(key))
      {
        return NotFound(new { Message = "文件不存在或已过期" });
      }

      var info = _tempFiles[key];
      if (!System.IO.File.Exists(info.FilePath))
      {
        return NotFound(new { Message = "文件不存在" });
      }

      // 如果 callback 还没下载新文件，先调用 OnlyOffice Command API forcesave
      if (!info.SavedByCallback)
      {
        var forceSaveOk = await ForceSaveFromDocServer(key);
        if (!forceSaveOk)
        {
          // forcesave 失败或超时，使用原始文件
          Logger.Info("ExportHtml: forcesave failed, using original file");
        }
      }

      try
      {
        var bytes = System.IO.File.ReadAllBytes(info.FilePath);
        var html = XlsxToHtml(bytes, out var fieldFormulas);
        return Ok(new { data = html, fileName = info.FileName, formulas = fieldFormulas });
      }
      catch (Exception ex)
      {
        Logger.Info("ExportHtml 异常: " + ex.Message);
        return BadRequest(new { Message = "导出失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 调用 OnlyOffice Document Server Command API 强制保存文档
    /// </summary>
    private async Task<bool> ForceSaveFromDocServer(string key)
    {
      try
      {
        var info = _tempFiles[key];
        string docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl") ?? "http://localhost:8088";

        // 构造 docKey（和 editor-config 中一致，带时间戳后缀）
        // 需要找到最近一次 editor-config 生成的 docKey
        // 简单方案：遍历 _tempFiles 中的 key，用 key + 时间戳尝试
        // 更好的方案：在 TempFileInfo 中记录 docKey
        var docKey = info.DocKey;
        if (string.IsNullOrEmpty(docKey))
        {
          Logger.Info("ForceSaveFromDocServer: no docKey found for key=" + key);
          return false;
        }

        var commandUrl = docServerUrl + "/command";
        var body = new JObject();
        body["c"] = "forcesave";
        body["key"] = docKey;

        using (var client = new HttpClient())
        {
          client.Timeout = TimeSpan.FromSeconds(10);
          var content = new StringContent(body.ToString(), System.Text.Encoding.UTF8, "application/json");
          var response = await client.PostAsync(commandUrl, content);
          var result = await response.Content.ReadAsStringAsync();
          Logger.Info("ForceSaveFromDocServer: key=" + key + ", docKey=" + docKey + ", result=" + result);

          var resultObj = JObject.Parse(result);
          var error = resultObj["error"]?.Value<int>() ?? -1;
          if (error == 0)
          {
            // forcesave 成功，等待 callback 下载文件
            // 最多等 5 秒
            for (int i = 0; i < 10; i++)
            {
              await Task.Delay(500);
              if (info.SavedByCallback)
              {
                Logger.Info("ForceSaveFromDocServer: callback received after " + (i + 1) * 500 + "ms");
                return true;
              }
            }
            Logger.Info("ForceSaveFromDocServer: callback not received after 5s");
            return false;
          }
          else
          {
            Logger.Info("ForceSaveFromDocServer: error=" + error);
            return false;
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("ForceSaveFromDocServer 异常: " + ex.Message);
        return false;
      }
    }

    /// <summary>
    /// 将 xlsx 字节解析为 HTML 表格（使用 NPOI 官方 ExcelToHtmlConverter，保留完整样式）
    /// ExcelToHtmlConverter 对 XSSFColor 背景色有 bug（IndexedColor index: 0），
    /// 先将 XSSF 背景色移除，转换后再手动补回背景色
    /// </summary>
    private string XlsxToHtml(byte[] xlsxBytes, out Dictionary<string, string> fieldFormulas)
    {
      fieldFormulas = new Dictionary<string, string>();
      Console.WriteLine("[XlsxToHtml] start, bytes=" + xlsxBytes.Length);
      using (var ms = new MemoryStream(xlsxBytes))
      {
        IWorkbook workbook = WorkbookFactory.Create(ms);
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null) return "<table></table>";

        // 1. 收集所有单元格的 XSSF 背景色信息（转换前保存）
        var bgColorMap = new Dictionary<string, string>(); // "r_c" → "#RRGGBB"
        if (workbook is XSSFWorkbook)
        {
          for (var r = 0; r <= sheet.LastRowNum; r++)
          {
            var row = sheet.GetRow(r);
            if (row == null) continue;
            for (var c = 0; c < row.LastCellNum; c++)
            {
              var cell = row.GetCell(c);
              if (cell == null) continue;
              var cellStyle = cell.CellStyle as XSSFCellStyle;
              if (cellStyle == null) continue;

              try
              {
                if (cellStyle.FillPattern == FillPattern.SolidForeground)
                {
                  var fill = cellStyle.FillForegroundXSSFColor;
                  if (fill != null)
                  {
                    var hex = XssfColorToHex(fill);
                    if (!string.IsNullOrEmpty(hex))
                    {
                      bgColorMap[r + "_" + c] = hex;
                    }
                  }
                }
              }
              catch { }
            }
          }
        }

        // 2. 直接使用 GenerateHtmlManually 生成 HTML（支持公式占位符转换）
        // ExcelToHtmlConverter 不支持公式占位符转换，且对 XSSFColor 有 bug
        var resultHtml = GenerateHtmlManually(workbook, sheet, bgColorMap, fieldFormulas);

        return resultHtml;
      }
    }

    /// <summary>
    /// 手动从 workbook 生成 HTML 表格（当 ExcelToHtmlConverter 失败时的回退方案）
    /// 保留基本样式：对齐、边框、背景色、粗体、斜体、下标、上标、字体、字号、合并单元格
    /// </summary>
    private string GenerateHtmlManually(IWorkbook workbook, ISheet sheet, Dictionary<string, string> bgColorMap, Dictionary<string, string> fieldFormulas)
    {
      var sb = new StringBuilder();
      sb.Append("<table style=\"border-collapse:collapse;\">");

      // 收集合并区域
      var mergedRegions = new HashSet<string>();
      for (var i = 0; i < sheet.NumMergedRegions; i++)
      {
        var region = sheet.GetMergedRegion(i);
        mergedRegions.Add(region.FirstRow + "_" + region.FirstColumn);
      }

      // 计算最大列数
      int maxCol = 0;
      for (var r = 0; r <= sheet.LastRowNum; r++)
      {
        var row = sheet.GetRow(r);
        if (row != null && row.LastCellNum > maxCol) maxCol = row.LastCellNum;
      }

      // 用 colgroup 设置列宽
      if (maxCol > 0)
      {
        sb.Append("<colgroup>");
        for (int c = 0; c < maxCol; c++)
        {
          var w = sheet.GetColumnWidth(c);
          // 默认宽度 2048 ≈ 64px，超过默认的才设置
          if (w > 0 && w != 2048)
          {
            var px = Math.Max((int)(w / 36.6), 20);
            sb.Append("<col style=\"width:" + px + "px\">");
          }
          else
          {
            sb.Append("<col>");
          }
        }
        sb.Append("</colgroup>");
      }

      // 收集图片信息
      var images = ExtractImagesFromSheet(workbook, sheet);

      for (var r = 0; r <= sheet.LastRowNum; r++)
      {
        var row = sheet.GetRow(r);
        if (row == null)
        {
          sb.Append("<tr><td></td></tr>");
          continue;
        }

        // 行高
        var rowHeight = row.Height;
        var rowStyle = (-1 != row.Height && row.Height != 300) ? " style=\"height:" + (row.Height / 15) + "pt\"" : "";

        sb.Append("<tr" + rowStyle + ">");
        for (var c = 0; c < row.LastCellNum; c++)
        {
          var cell = row.GetCell(c);
          if (cell == null) continue;

          // 检查是否是合并区域的子单元格（跳过）
          var isMergedChild = false;
          for (var i = 0; i < sheet.NumMergedRegions; i++)
          {
            var region = sheet.GetMergedRegion(i);
            if (r >= region.FirstRow && r <= region.LastRow && c >= region.FirstColumn && c <= region.LastColumn
              && !(r == region.FirstRow && c == region.FirstColumn))
            {
              isMergedChild = true;
              break;
            }
          }
          if (isMergedChild) continue;

          var style = cell.CellStyle;
          var styles = new List<string>();

          // 对齐
          if (style != null)
          {
            if (style.Alignment == HorizontalAlignment.Center) styles.Add("text-align:center");
            else if (style.Alignment == HorizontalAlignment.Right) styles.Add("text-align:right");
            else if (style.Alignment == HorizontalAlignment.Left) styles.Add("text-align:left");

            if (style.VerticalAlignment == VerticalAlignment.Center) styles.Add("vertical-align:middle");
            else if (style.VerticalAlignment == VerticalAlignment.Top) styles.Add("vertical-align:top");
            else if (style.VerticalAlignment == VerticalAlignment.Bottom) styles.Add("vertical-align:bottom");

            // 边框
            if (style.BorderTop == BorderStyle.Thin) styles.Add("border-top:1px solid #000");
            if (style.BorderBottom == BorderStyle.Thin) styles.Add("border-bottom:1px solid #000");
            if (style.BorderLeft == BorderStyle.Thin) styles.Add("border-left:1px solid #000");
            if (style.BorderRight == BorderStyle.Thin) styles.Add("border-right:1px solid #000");

            // 背景色
            var bgColorKey = r + "_" + c;
            if (bgColorMap.ContainsKey(bgColorKey))
            {
              styles.Add("background-color:" + bgColorMap[bgColorKey]);
            }
          }

          // colspan/rowspan
          var colspan = 1;
          var rowspan = 1;
          for (var i = 0; i < sheet.NumMergedRegions; i++)
          {
            var region = sheet.GetMergedRegion(i);
            if (region.FirstRow == r && region.FirstColumn == c)
            {
              colspan = region.LastColumn - region.FirstColumn + 1;
              rowspan = region.LastRow - region.FirstRow + 1;
              break;
            }
          }

          // 内容（处理 RichText 和普通文本）
          var content = "";
          // 调试：记录公式单元格信息
          if (cell.CellType == CellType.Formula || (r < 15 && c < 10))
          {
            Console.WriteLine("[CellDebug] [" + r + "," + c + "] CellType=" + cell.CellType + " val=" + (cell.CellType == CellType.String ? cell.StringCellValue : cell.CellType == CellType.Formula ? cell.CellFormula : ""));
          }
          if (cell.CellType == CellType.String)
          {
            content = GetCellHtmlContent(workbook, cell);
          }
          else if (cell.CellType == CellType.Numeric)
          {
            content = System.Net.WebUtility.HtmlEncode(cell.NumericCellValue.ToString());
          }
          else if (cell.CellType == CellType.Boolean)
          {
            content = cell.BooleanCellValue ? "true" : "false";
          }
          else if (cell.CellType == CellType.Formula)
          {
            // 公式反向转换
            var formula = cell.CellFormula;
            // 简单单元格引用公式 (C1) → ${FC1}
            var formulaRefMatch = Regex.Match(formula, @"^([A-Z]+)(\d+)$");
            if (formulaRefMatch.Success)
            {
              content = "${F" + formulaRefMatch.Groups[1].Value + formulaRefMatch.Groups[2].Value + "}";
            }
            // 字符串拼接公式：以单元格引用+&开头，且后面有字符串字面量
            // 如 G2&"FUNC("&C2&","&D2&","&E2&")"
            else
            {
              var currentColLetter = GetColumnLetter(c);
              var currentRef = currentColLetter + (r + 1);
              // 检查公式开头是否是 "引用&" 模式
              var concatStartMatch = Regex.Match(formula, @"^[A-Z]+\d+&");
              if (concatStartMatch.Success)
              {
                content = "${F" + currentColLetter + (r + 1) + "}";
                var parseRef = concatStartMatch.Groups[0].Value.Replace("&", "");
                var formulaDef = ParseConcatFormula(formula, parseRef);
                Console.WriteLine("[FormulaDebug] concat currentRef=" + currentRef + " parseRef=" + parseRef + " formula=" + formula + " parsedDef=" + formulaDef);
                if (!string.IsNullOrEmpty(formulaDef))
                {
                  fieldFormulas["F" + currentColLetter + (r + 1)] = formulaDef;
                }
              }
              else
              {
                try { content = System.Net.WebUtility.HtmlEncode(cell.StringCellValue); }
                catch { content = System.Net.WebUtility.HtmlEncode(formula); }
              }
            }
          }

          // 检查该单元格位置是否有图片
          var imgKey = r + "_" + c;
          if (images.ContainsKey(imgKey))
          {
            content = images[imgKey] + content;
          }

          var styleAttr = styles.Count > 0 ? " style=\"" + string.Join(";", styles) + "\"" : "";
          var colspanAttr = colspan > 1 ? " colspan=\"" + colspan + "\"" : "";
          var rowspanAttr = rowspan > 1 ? " rowspan=\"" + rowspan + "\"" : "";

          sb.Append("<td" + styleAttr + colspanAttr + rowspanAttr + ">" + content + "</td>");
        }
        sb.Append("</tr>");
      }
      sb.Append("</table>");
      return sb.ToString();
    }

    /// <summary>
    /// 从 xlsx sheet 中提取图片，返回行列位置到 HTML img 标签的映射
    /// 不依赖 GDI+，直接操作 xlsx 的 Open XML 结构
    /// </summary>
    private Dictionary<string, string> ExtractImagesFromSheet(IWorkbook workbook, ISheet sheet)
    {
      var result = new Dictionary<string, string>();
      try
      {
        var xssfSheet = sheet as XSSFSheet;
        if (xssfSheet == null) return result;

        var drawingPatriarch = xssfSheet.GetDrawingPatriarch() as NPOI.XSSF.UserModel.XSSFDrawing;
        if (drawingPatriarch == null) return result;

        // 通过 GetShapes 获取所有 shape，XSSFPicture 不需要 GDI+
        var shapes = drawingPatriarch.GetShapes();
        foreach (var shape in shapes)
        {
          try
          {
            var picture = shape as NPOI.XSSF.UserModel.XSSFPicture;
            if (picture == null) continue;

            // 获取锚点信息（行列位置）
            var anchor = picture.GetAnchor();
            int row = 0, col = 0;
            if (anchor is NPOI.XSSF.UserModel.XSSFClientAnchor clientAnchor)
            {
              row = clientAnchor.Row1;
              col = clientAnchor.Col1;
            }
            else
            {
              // 尝试通过反射获取
              var prop = anchor.GetType().GetProperty("Row1");
              if (prop != null) row = (int)prop.GetValue(anchor);
              prop = anchor.GetType().GetProperty("Col1");
              if (prop != null) col = (int)prop.GetValue(anchor);
            }

            // 获取图片数据
            var pictureData = picture.PictureData;
            if (pictureData == null) continue;

            var mimeType = pictureData.MimeType;
            var base64 = Convert.ToBase64String(pictureData.Data);
            var imgTag = "<img src=\"data:" + mimeType + ";base64," + base64 + "\" style=\"max-width:100%;\">";

            var key = row + "_" + col;
            result[key] = imgTag;
            Logger.Info("ExtractImagesFromSheet: found image at row=" + row + ", col=" + col + ", size=" + pictureData.Data.Length + ", mime=" + mimeType);
          }
          catch (Exception shapeEx)
          {
            Logger.Info("ExtractImagesFromSheet: shape error: " + shapeEx.Message);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Info("ExtractImagesFromSheet 异常: " + ex.Message);
      }
      return result;
    }

    /// <summary>
    /// 从单元格获取 HTML 内容，保留 RichText 格式（粗体、斜体、下标、上标、字体、字号）
    /// </summary>
    private string GetCellHtmlContent(IWorkbook workbook, ICell cell)
    {
      var richText = cell.RichStringCellValue as XSSFRichTextString;
      var style = cell.CellStyle;
      var text = System.Net.WebUtility.HtmlEncode(cell.StringCellValue);
      var wrapText = style != null && style.WrapText;

      // 当 RichText 存在且至少有 1 个 formatting run 时，优先使用 RichText 的字体信息
      // 因为 RichText 的 <rPr> 才是实际格式，CellStyle 的 font 只是默认值
      if (richText != null && richText.NumFormattingRuns >= 1)
      {
        // 单片段或多片段：都从 RichText 的 formatting runs 获取格式
        if (richText.NumFormattingRuns == 1)
        {
          // 单片段：从 Run 0 的字体获取格式（覆盖 CellStyle font）
          var runFont = richText.GetFontOfFormattingRun(0);
          if (runFont != null)
          {
            var runParts = new List<string>();
            if (runFont.IsBold) runParts.Add("font-weight:bold");
            if (runFont.IsItalic) runParts.Add("font-style:italic");
            if (runFont.TypeOffset == FontSuperScript.Sub) runParts.Add("vertical-align:sub;font-size:smaller");
            if (runFont.TypeOffset == FontSuperScript.Super) runParts.Add("vertical-align:super;font-size:smaller");
            var xf = runFont as XSSFFont;
            var fontName = xf != null ? xf.FontName : "";
            fontName = fontName.Replace("'", "").Replace("\"", "").Trim();
            if (!string.IsNullOrEmpty(fontName) && fontName != "Calibri")
              runParts.Add("font-family:'" + fontName + "'");
            if (runFont.FontHeightInPoints > 0 && runFont.FontHeightInPoints != 11)
              runParts.Add("font-size:" + runFont.FontHeightInPoints + "pt");

            if (runParts.Count > 0)
            {
              text = "<span style=\"" + string.Join(";", runParts) + "\">" + text + "</span>";
            }
          }

          if (wrapText) text = text.Replace("\n", "<br>");
          return text;
        }

        // 多格式片段：逐片段生成 HTML
        var sb = new StringBuilder();
        var numRuns = richText.NumFormattingRuns;
        for (var i = 0; i < numRuns; i++)
        {
          var start = richText.GetIndexOfFormattingRun(i);
          var end = (i + 1 < numRuns) ? richText.GetIndexOfFormattingRun(i + 1) : richText.Length;
          var runText = cell.StringCellValue.Substring(start, Math.Min(end - start, cell.StringCellValue.Length - start));

          var runFont2 = richText.GetFontOfFormattingRun(i);
          var multiParts = new List<string>();

          if (runFont2 != null)
          {
            if (runFont2.IsBold) multiParts.Add("font-weight:bold");
            if (runFont2.IsItalic) multiParts.Add("font-style:italic");
            if (runFont2.TypeOffset == FontSuperScript.Sub) multiParts.Add("vertical-align:sub;font-size:smaller");
            if (runFont2.TypeOffset == FontSuperScript.Super) multiParts.Add("vertical-align:super;font-size:smaller");
            var xf2 = runFont2 as XSSFFont;
            var fontName3 = xf2 != null ? xf2.FontName : "";
            fontName3 = fontName3.Replace("'", "").Replace("\"", "").Trim();
            if (!string.IsNullOrEmpty(fontName3) && fontName3 != "Calibri")
              multiParts.Add("font-family:'" + fontName3 + "'");
            if (runFont2.FontHeightInPoints > 0 && runFont2.FontHeightInPoints != 11)
              multiParts.Add("font-size:" + runFont2.FontHeightInPoints + "pt");
          }

          var encodedText = System.Net.WebUtility.HtmlEncode(runText);
          if (multiParts.Count > 0)
          {
            sb.Append("<span style=\"" + string.Join(";", multiParts) + "\">" + encodedText + "</span>");
          }
          else
          {
            sb.Append(encodedText);
          }
        }

        var result = sb.ToString();
        if (wrapText) result = result.Replace("\n", "<br>");
        return result;
      }

      // 没有 RichText，用 CellStyle 的字体
      if (style == null) return text;

      var fontIdx = style.FontIndex;
      var font = workbook.GetFontAt(fontIdx);

      var parts = new List<string>();
      if (font.IsBold) parts.Add("font-weight:bold");
      if (font.IsItalic) parts.Add("font-style:italic");
      if (font.TypeOffset == FontSuperScript.Sub) parts.Add("vertical-align:sub;font-size:smaller");
      if (font.TypeOffset == FontSuperScript.Super) parts.Add("vertical-align:super;font-size:smaller");
      if (!string.IsNullOrEmpty(font.FontName) && font.FontName != "Calibri")
        parts.Add("font-family:'" + font.FontName + "'");
      if (font.FontHeightInPoints > 0 && font.FontHeightInPoints != 11)
        parts.Add("font-size:" + font.FontHeightInPoints + "pt");

      if (parts.Count > 0)
      {
        text = "<span style=\"" + string.Join(";", parts) + "\">" + text + "</span>";
      }

      if (wrapText) text = text.Replace("\n", "<br>");
      return text;
    }

    /// <summary>
    /// 将 CSS class 引用的 style 转为 inline style，同时补回 XSSF 背景色
    /// </summary>
    private string ConvertCssClassToInlineStyle(string styleSection, string tableSection,
      Dictionary<string, string> bgColorMap, IWorkbook workbook, ISheet sheet)
    {
      // 解析 CSS class 定义
      var cssClasses = new Dictionary<string, string>(); // class名 → style内容
      var cssRegex = new Regex(@"(?:td|tr|table)\.(\w+)\s*\{([^}]*)\}");
      foreach (Match m in cssRegex.Matches(styleSection))
      {
        var className = m.Groups[1].Value;
        var styleContent = m.Groups[2].Value.Trim();
        cssClasses[className] = styleContent;
      }

      // 解析 table 中的 HTML，将 class 引用替换为 inline style
      // 同时按行列位置补回背景色
      var rowColTracker = new RowColTracker(sheet);
      var tdRegex = new Regex(@"<td([^>]*?)class=""(\w+)""([^>]*?)>(.*?)</td>", RegexOptions.Singleline);
      var result = tdRegex.Replace(tableSection, delegate (Match m)
      {
        var beforeClass = m.Groups[1].Value;
        var className = m.Groups[2].Value;
        var afterClass = m.Groups[3].Value;
        var content = m.Groups[4].Value;

        // 获取 CSS class 的 style
        var cssStyle = cssClasses.ContainsKey(className) ? cssClasses[className] : "";

        // 获取 colspan/rowspan
        var colspan = 1;
        var rowspan = 1;
        var colspanMatch = Regex.Match(beforeClass + afterClass, @"colspan=""(\d+)""");
        if (colspanMatch.Success) colspan = int.Parse(colspanMatch.Groups[1].Value);
        var rowspanMatch = Regex.Match(beforeClass + afterClass, @"rowspan=""(\d+)""");
        if (rowspanMatch.Success) rowspan = int.Parse(rowspanMatch.Groups[1].Value);

        // 获取当前行列位置
        var pos = rowColTracker.Next(colspan, rowspan);

        // 补回背景色
        var bgColorKey = pos.Row + "_" + pos.Col;
        if (bgColorMap.ContainsKey(bgColorKey))
        {
          cssStyle += ";background-color:" + bgColorMap[bgColorKey];
        }

        // 构建新的 td 标签（inline style 替代 class）
        var newAttrs = beforeClass + afterClass;
        // 移除 class 属性
        newAttrs = Regex.Replace(newAttrs, @"\s*class=""\w+""", "");
        // 添加 inline style
        if (!string.IsNullOrEmpty(cssStyle))
        {
          newAttrs += " style=\"" + cssStyle + "\"";
        }

        return "<td" + newAttrs + ">" + content + "</td>";
      });

      // 处理没有 class 的 td 标签
      var tdNoClassRegex = new Regex(@"<td([^>]*?)>(.*?)</td>", RegexOptions.Singleline);
      // 只处理之前没被匹配过的 td（没有 class 属性的）

      // 添加 border-collapse 和全局样式到 table
      result = Regex.Replace(result, @"<table([^>]*?)>", delegate (Match m)
      {
        var attrs = m.Groups[1].Value;
        // 移除 class
        attrs = Regex.Replace(attrs, @"\s*class=""\w+""", "");
        return "<table" + attrs + " style=\"border-collapse:collapse;\">";
      });

      // 处理 colgroup 中的 col 标签（保留宽度）
      // colgroup 已经在 ExcelToHtmlConverter 生成的 HTML 中了

      return result;
    }

    /// <summary>
    /// 行列位置追踪器，用于将 HTML td 标签映射回 Excel 的行列位置
    /// </summary>
    private class RowColTracker
    {
      private int _row = 0;
      private int _col = 0;
      private readonly HashSet<string> _mergedCells = new HashSet<string>();

      public RowColTracker(ISheet sheet)
      {
        // 预计算所有被合并占用的单元格
        for (var mi = 0; mi < sheet.NumMergedRegions; mi++)
        {
          var region = sheet.GetMergedRegion(mi);
          for (var r = region.FirstRow; r <= region.LastRow; r++)
          {
            for (var c = region.FirstColumn; c <= region.LastColumn; c++)
            {
              if (r != region.FirstRow || c != region.FirstColumn)
              {
                _mergedCells.Add(r + "_" + c);
              }
            }
          }
        }
      }

      public Position Next(int colspan, int rowspan)
      {
        // 跳过被合并占用的列
        while (_mergedCells.Contains(_row + "_" + _col)) _col++;

        var pos = new Position { Row = _row, Col = _col };

        // 如果是合并单元格，标记占用的区域
        if (colspan > 1 || rowspan > 1)
        {
          for (var r = _row; r < _row + rowspan; r++)
          {
            for (var c = _col; c < _col + colspan; c++)
            {
              if (r != _row || c != _col)
              {
                _mergedCells.Add(r + "_" + c);
              }
            }
          }
        }

        _col += colspan;
        return pos;
      }

      public void NextRow()
      {
        _row++;
        _col = 0;
      }
    }

    private class Position
    {
      public int Row { get; set; }
      public int Col { get; set; }
    }

    /// <summary>
    /// 将 NPOI 颜色索引/XSSFColor 转为 HTML 颜色字符串
    /// </summary>
    private string GetColorHtml(IWorkbook workbook, ICellStyle cellStyle)
    {
      try
      {
        if (workbook is XSSFWorkbook)
        {
          var xssfStyle = cellStyle as XSSFCellStyle;
          if (xssfStyle != null)
          {
            var fill = xssfStyle.FillForegroundXSSFColor;
            if (fill != null)
            {
              return XssfColorToHex(fill);
            }
          }
          return "";
        }
        else if (workbook is HSSFWorkbook hssfWb)
        {
          var palette = hssfWb.GetCustomPalette();
          var colorIndex = cellStyle.FillForegroundColor;
          var color = palette.GetColor(colorIndex);
          if (color != null)
          {
            var hex = color.GetHexString();
            if (!string.IsNullOrEmpty(hex)) return "#" + hex;
          }
        }
      }
      catch { }
      return "";
    }

    /// <summary>
    /// 将 XSSFColor 转为 #RRGGBB 格式的 HTML 颜色字符串
    /// </summary>
    private string XssfColorToHex(XSSFColor color)
    {
      if (color == null) return "";
      var argb = color.ARGB;
      if (argb != null && argb.Length >= 4)
      {
        // ARGB byte[]: [A, R, G, B]
        return "#" + argb[1].ToString("X2") + argb[2].ToString("X2") + argb[3].ToString("X2");
      }
      return "";
    }

    /// <summary>
    /// 获取单元格边框颜色的 HTML 表示
    /// </summary>
    private string GetBorderColorHtml(IWorkbook workbook, ICellStyle cellStyle, string side)
    {
      try
      {
        if (workbook is XSSFWorkbook)
        {
          var xssfStyle = cellStyle as XSSFCellStyle;
          if (xssfStyle != null)
          {
            XSSFColor color = null;
            switch (side)
            {
              case "top": color = xssfStyle.TopBorderXSSFColor; break;
              case "bottom": color = xssfStyle.BottomBorderXSSFColor; break;
              case "left": color = xssfStyle.LeftBorderXSSFColor; break;
              case "right": color = xssfStyle.RightBorderXSSFColor; break;
            }
            if (color != null)
            {
              var hex = XssfColorToHex(color);
              if (!string.IsNullOrEmpty(hex)) return hex;
            }
          }
        }
        else if (workbook is HSSFWorkbook hssfWb)
        {
          var palette = hssfWb.GetCustomPalette();
          short colorIndex = 8; // 默认黑色
          switch (side)
          {
            case "top": colorIndex = cellStyle.TopBorderColor; break;
            case "bottom": colorIndex = cellStyle.BottomBorderColor; break;
            case "left": colorIndex = cellStyle.LeftBorderColor; break;
            case "right": colorIndex = cellStyle.RightBorderColor; break;
          }
          var color = palette.GetColor(colorIndex);
          if (color != null)
          {
            var hex = color.GetHexString();
            if (!string.IsNullOrEmpty(hex)) return "#" + hex;
          }
        }
      }
      catch { }
      return "#000";
    }

    /// <summary>
    /// 将 HTML 表格字符串转为 xlsx 字节（使用 NPOI，保留完整样式）
    /// 使用正则表达式解析 HTML，不依赖 HtmlAgilityPack
    /// </summary>
    private byte[] HtmlToXlsx(string html, Dictionary<string, string> fieldFormulas = null)
    {
      // 检查是否有 <table>
      if (string.IsNullOrEmpty(html) || html.IndexOf("<table", StringComparison.OrdinalIgnoreCase) < 0)
      {
        return CreateBlankXlsx();
      }

      var workbook = new XSSFWorkbook();
      var sheet = workbook.CreateSheet("Sheet1");

      // 解析 <col> 获取列宽
      var colWidths = new Dictionary<int, int>();
      var colMatches = System.Text.RegularExpressions.Regex.Matches(html, @"<col\s+[^>]*?>", RegexOptions.IgnoreCase);
      var colIdx = 0;
      foreach (Match m in colMatches)
      {
        var colTag = m.Value;
        var widthAttr = GetHtmlAttr(colTag, "width");
        if (!string.IsNullOrEmpty(widthAttr) && int.TryParse(widthAttr, out var w) && w > 0)
        {
          colWidths[colIdx] = w;
        }
        var styleAttr = GetHtmlAttr(colTag, "style");
        if (!string.IsNullOrEmpty(styleAttr))
        {
          var cssMap = ParseCssStyle(styleAttr);
          if (cssMap.ContainsKey("width"))
          {
            var pxVal = ParsePxValue(cssMap["width"]);
            if (pxVal > 0) colWidths[colIdx] = Math.Max(colWidths.ContainsKey(colIdx) ? colWidths[colIdx] : 0, pxVal);
          }
        }
        colIdx++;
      }

      // 设置列宽
      foreach (var kv in colWidths)
      {
        sheet.SetColumnWidth(kv.Key, (int)(kv.Value / 8.0 * 256));
      }

      // 创建单元格样式缓存
      var styleCache = new Dictionary<string, ICellStyle>();

      // 解析 <tr> 行
      var trMatches = System.Text.RegularExpressions.Regex.Matches(html, @"<tr\s*[^>]*?>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
      if (trMatches.Count == 0)
      {
        return WriteWorkbookToBytes(workbook);
      }

      // 网格化处理合并单元格
      var grid = new HashSet<string>();
      var mergeRegions = new List<CellRangeAddress>();

      var rowIdx = 0;
      foreach (Match trMatch in trMatches)
      {
        var trContent = trMatch.Groups[1].Value;
        var colIdx2 = 0;

        // 解析 <td> 和 <th>
        var tdMatches = System.Text.RegularExpressions.Regex.Matches(trContent, @"<(td|th)\s*([^>]*?)>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (tdMatches.Count == 0) { rowIdx++; continue; }

        foreach (Match tdMatch in tdMatches)
        {
          var attrs = tdMatch.Groups[2].Value;
          var innerHtml = tdMatch.Groups[3].Value;

          // 跳过已被合并占用的列
          while (grid.Contains(rowIdx + "_" + colIdx2)) colIdx2++;

          var colspan = 1;
          var colspanAttr = GetHtmlAttr(attrs, "colspan");
          if (!string.IsNullOrEmpty(colspanAttr)) int.TryParse(colspanAttr, out colspan);
          if (colspan < 1) colspan = 1;

          var rowspan = 1;
          var rowspanAttr = GetHtmlAttr(attrs, "rowspan");
          if (!string.IsNullOrEmpty(rowspanAttr)) int.TryParse(rowspanAttr, out rowspan);
          if (rowspan < 1) rowspan = 1;

          // 记录到网格
          grid.Add(rowIdx + "_" + colIdx2);

          // 合并单元格
          if (colspan > 1 || rowspan > 1)
          {
            var merge = new CellRangeAddress(rowIdx, rowIdx + rowspan - 1, colIdx2, colIdx2 + colspan - 1);
            mergeRegions.Add(merge);
            for (var r = rowIdx; r < rowIdx + rowspan; r++)
            {
              for (var c = colIdx2; c < colIdx2 + colspan; c++)
              {
                grid.Add(r + "_" + c);
              }
            }
          }

          // 创建单元格
          var row = sheet.GetRow(rowIdx) ?? sheet.CreateRow(rowIdx);
          var cell = row.CreateCell(colIdx2);

          // 判断是否是 <th>（默认居中+粗体）
          var isTh = tdMatch.Groups[1].Value.Equals("th", StringComparison.OrdinalIgnoreCase);

          // 从 innerHtml 中提取 <p> 的 text-align 和 <strong>/<b> 的粗体信息
          // WangEditor 生成: <td><p style="text-align:center"><strong>文字</strong></p></td>
          var innerAlign = "";
          var pAlignMatch = System.Text.RegularExpressions.Regex.Match(innerHtml, @"<p\s[^>]*?style=""[^""]*?text-align:\s*([^;""]+)", RegexOptions.IgnoreCase);
          if (pAlignMatch.Success) innerAlign = pAlignMatch.Groups[1].Value.Trim();
          var innerBold = System.Text.RegularExpressions.Regex.IsMatch(innerHtml, @"<(strong|b)\b", RegexOptions.IgnoreCase);
          var innerItalic = System.Text.RegularExpressions.Regex.IsMatch(innerHtml, @"<(em|i)\b", RegexOptions.IgnoreCase);

          // 从 <span> 中提取 font-family 和 font-size
          // WangEditor: <span style="font-family: 宋体;font-size: 14px">
          var spanFontFamily = "";
          var spanFontSize = "";
          var spanMatch = System.Text.RegularExpressions.Regex.Match(innerHtml, @"<span\s[^>]*?style=""([^""]*)""", RegexOptions.IgnoreCase);
          if (spanMatch.Success)
          {
            var spanStyle = spanMatch.Groups[1].Value;
            var spanCss = ParseCssStyle(spanStyle);
            if (spanCss.ContainsKey("font-family")) spanFontFamily = spanCss["font-family"].Trim('\'', '"');
            if (spanCss.ContainsKey("font-size")) spanFontSize = spanCss["font-size"];
          }

          // 提取纯文本（去掉所有 HTML 标签）
          var text = System.Text.RegularExpressions.Regex.Replace(innerHtml, @"<[^>]+>", "").Trim();
          // HTML 实体解码
          text = System.Net.WebUtility.HtmlDecode(text);

          // 合并 td style + 内部标签样式
          var tdStyle = GetHtmlAttr(attrs, "style");
          // 如果 <p> 有 text-align 且 td style 没有，追加到 tdStyle
          if (!string.IsNullOrEmpty(innerAlign) && (string.IsNullOrEmpty(tdStyle) || !tdStyle.Contains("text-align")))
          {
            tdStyle = (string.IsNullOrEmpty(tdStyle) ? "" : tdStyle + "; ") + "text-align: " + innerAlign;
          }
          // 如果有 <strong>/<b> 且 td style 没有 font-weight，追加 bold
          if (innerBold && (string.IsNullOrEmpty(tdStyle) || !tdStyle.Contains("font-weight")))
          {
            tdStyle = (string.IsNullOrEmpty(tdStyle) ? "" : tdStyle + "; ") + "font-weight: bold";
          }
          // 如果有 <em>/<i> 且 td style 没有 font-style，追加 italic
          if (innerItalic && (string.IsNullOrEmpty(tdStyle) || !tdStyle.Contains("font-style")))
          {
            tdStyle = (string.IsNullOrEmpty(tdStyle) ? "" : tdStyle + "; ") + "font-style: italic";
          }
          // 如果 <span> 有 font-family 且 td style 没有，追加
          if (!string.IsNullOrEmpty(spanFontFamily) && (string.IsNullOrEmpty(tdStyle) || !tdStyle.Contains("font-family")))
          {
            tdStyle = (string.IsNullOrEmpty(tdStyle) ? "" : tdStyle + "; ") + "font-family: " + spanFontFamily;
          }
          // 如果 <span> 有 font-size 且 td style 没有，追加
          if (!string.IsNullOrEmpty(spanFontSize) && (string.IsNullOrEmpty(tdStyle) || !tdStyle.Contains("font-size")))
          {
            tdStyle = (string.IsNullOrEmpty(tdStyle) ? "" : tdStyle + "; ") + "font-size: " + spanFontSize;
          }

          // 检查 innerHtml 中是否有 <img> 标签（图片）
          var imgMatch = System.Text.RegularExpressions.Regex.Match(innerHtml, @"<img[^>]+src=""([^""]+)""[^>]*>", RegexOptions.IgnoreCase);
          var hasImage = imgMatch.Success;

          // 设置单元格内容
          var usedRichText = false;
          if (hasImage)
          {
            // 图片处理：尝试将 base64 图片嵌入 Excel
            var imgSrc = imgMatch.Groups[1].Value;
            var imgStyle = GetHtmlAttr(imgMatch.Value, "style");
            var imgWidth = GetHtmlAttr(imgMatch.Value, "width");
            var imgHeight = GetHtmlAttr(imgMatch.Value, "height");
            InsertImageToCell(workbook, sheet, rowIdx, colIdx2, imgSrc, imgStyle, imgWidth, imgHeight);
            // 如果图片旁边还有文本，也设置到单元格
            if (!string.IsNullOrEmpty(text))
            {
              cell.SetCellValue(text);
            }
          }
          else
          {
            // 尝试解析为数字（只有纯数字文本时才设为数字）
            if (double.TryParse(text, out var numVal) && !innerHtml.Contains("<span"))
            {
              cell.SetCellValue(numVal);
            }
            else
            {
              // 检查文本是否是公式占位符 ${F列字母行数字}
              // ${FC1} → 公式 =C1（默认就是引用对应列行）
              // 如果有 formula 定义且非简单引用，用字符串拼接公式
              var formulaMatch = Regex.Match(text, @"^\$\{F([A-Z])(\d+)\}$");
              if (formulaMatch.Success)
              {
                var colLetter = formulaMatch.Groups[1].Value;
                var rowNum = formulaMatch.Groups[2].Value;
                var fieldKey = "F" + colLetter + rowNum;

                // 检查是否有复杂 formula 定义
                string formulaDef = null;
                if (fieldFormulas != null && fieldFormulas.TryGetValue(fieldKey, out formulaDef))
                {
                  var excelFormula = Regex.Replace(formulaDef, @"\$\{F([A-Z])(\d+)\}", m => m.Groups[1].Value + m.Groups[2].Value);
                  // 如果 formula 替换后是简单单元格引用（如 C1），直接设公式（和默认一样）
                  if (Regex.IsMatch(excelFormula, @"^[A-Z]\d+$"))
                  {
                    cell.SetCellFormula(excelFormula);
                  }
                  else
                  {
                    // 复杂公式定义，生成字符串拼接公式
                    // 例：FUNC(C2,D2,E2) → G2&"FUNC("&C2&","&D2&","&E2&")"
                    // 公式效果：单元格显示 FUNC(C2值,D2值,E2值)
                    var formulaParts = new StringBuilder();
                    formulaParts.Append(colLetter + rowNum); // 当前单元格引用
                    var fullText = excelFormula;
                    var refMatch = Regex.Match(fullText, @"([A-Z]\d+)");
                    int lastEnd = 0;
                    while (refMatch.Success)
                    {
                      var beforeText = fullText.Substring(lastEnd, refMatch.Index - lastEnd);
                      if (beforeText.Length > 0)
                      {
                        if (formulaParts.Length > 0) formulaParts.Append("&");
                        formulaParts.Append("\"" + beforeText.Replace("\"", "\"\"") + "\"");
                      }
                      if (formulaParts.Length > 0) formulaParts.Append("&");
                      formulaParts.Append(refMatch.Value);
                      lastEnd = refMatch.Index + refMatch.Length;
                      refMatch = refMatch.NextMatch();
                    }
                    // 尾部文本
                    if (lastEnd < fullText.Length)
                    {
                      var tailText = fullText.Substring(lastEnd);
                      if (formulaParts.Length > 0) formulaParts.Append("&");
                      formulaParts.Append("\"" + tailText.Replace("\"", "\"\"") + "\"");
                    }
                    cell.SetCellFormula(formulaParts.ToString());
                  }
                }
                else
                {
                  // 无 formula 定义，默认转为简单引用公式 ${FC1} → =C1
                  cell.SetCellFormula(colLetter + rowNum);
                }
              }
              else
              {
                // 使用 RichText 支持混合格式（如 <em>斜体</em>、<sub>下标</sub>、不同字体等）
                usedRichText = SetCellRichText(workbook, cell, innerHtml);
              }
            }
          }

          // 设置样式（合并 td style 和 th 默认样式）
          // 重要：当使用了 RichText 时，CellStyle 不应设置字体属性（bold/italic/font-family/font-size/sub/super），
          // 否则 NPOI 2.5.6 会丢失 RichText 中各片段的字体信息（<rPr> 不会被写入 XML）
          var cellStyle = CreateCellStyleFromCss(workbook, tdStyle, styleCache, isTh, skipFont: usedRichText);
          if (cellStyle == null)
          {
            // 无 style 属性时创建默认样式
            cellStyle = workbook.CreateCellStyle();
            cellStyle.VerticalAlignment = VerticalAlignment.Center; // HTML 表格默认垂直居中
            if (isTh)
            {
              // <th> 默认居中（粗体由 RichText 处理，不设到 CellStyle）
              cellStyle.Alignment = HorizontalAlignment.Center;
              if (!usedRichText)
              {
                var thFont = workbook.CreateFont();
                thFont.IsBold = true;
                cellStyle.SetFont(thFont);
              }
            }
            cellStyle.BorderTop = BorderStyle.Thin;
            cellStyle.BorderBottom = BorderStyle.Thin;
            cellStyle.BorderLeft = BorderStyle.Thin;
            cellStyle.BorderRight = BorderStyle.Thin;
          }
          cell.CellStyle = cellStyle;

          // 为合并区域内的子单元格也创建并设置边框样式
          // NPOI 中合并单元格的边框由区域内每个 cell 的边框决定
          if (colspan > 1 || rowspan > 1)
          {
            var borderStyle = CreateBorderStyleForMerge(workbook, cellStyle, styleCache);
            for (var mr = rowIdx; mr < rowIdx + rowspan; mr++)
            {
              var mergeRow = sheet.GetRow(mr) ?? sheet.CreateRow(mr);
              for (var mc = colIdx2; mc < colIdx2 + colspan; mc++)
              {
                if (mr == rowIdx && mc == colIdx2) continue; // 跳过左上角（已有样式）
                var mergeCell = mergeRow.GetCell(mc) ?? mergeRow.CreateCell(mc);
                mergeCell.CellStyle = borderStyle;
              }
            }
          }

          colIdx2 += colspan;
        }
        rowIdx++;
      }

      // 应用合并区域
      foreach (var merge in mergeRegions)
      {
        sheet.AddMergedRegion(merge);
      }

      return WriteWorkbookToBytes(workbook);
    }

    /// <summary>
    /// 从 HTML 标签属性字符串中提取指定属性值
    /// </summary>
    private string GetHtmlAttr(string tagOrAttrs, string attrName)
    {
      var match = System.Text.RegularExpressions.Regex.Match(tagOrAttrs, attrName + @"=""([^""]*?)""", RegexOptions.IgnoreCase);
      if (match.Success) return match.Groups[1].Value;
      // 单引号
      match = System.Text.RegularExpressions.Regex.Match(tagOrAttrs, attrName + @"='([^']*?)'", RegexOptions.IgnoreCase);
      if (match.Success) return match.Groups[1].Value;
      // 无引号
      match = System.Text.RegularExpressions.Regex.Match(tagOrAttrs, attrName + @"=(\S+)", RegexOptions.IgnoreCase);
      if (match.Success) return match.Groups[1].Value;
      return "";
    }

    /// <summary>
    /// 为合并区域内的子单元格创建边框样式
    /// 复制主单元格的边框、对齐方式，确保合并区域边线完整
    /// </summary>
    private ICellStyle CreateBorderStyleForMerge(IWorkbook workbook, ICellStyle mainStyle, Dictionary<string, ICellStyle> cache)
    {
      var borderStyle = workbook.CreateCellStyle();
      // 复制边框
      if (mainStyle != null)
      {
        borderStyle.BorderTop = mainStyle.BorderTop;
        borderStyle.BorderBottom = mainStyle.BorderBottom;
        borderStyle.BorderLeft = mainStyle.BorderLeft;
        borderStyle.BorderRight = mainStyle.BorderRight;
        borderStyle.TopBorderColor = mainStyle.TopBorderColor;
        borderStyle.BottomBorderColor = mainStyle.BottomBorderColor;
        borderStyle.LeftBorderColor = mainStyle.LeftBorderColor;
        borderStyle.RightBorderColor = mainStyle.RightBorderColor;
        // 复制对齐方式（合并区域内子单元格也需保持对齐一致）
        borderStyle.Alignment = mainStyle.Alignment;
        borderStyle.VerticalAlignment = mainStyle.VerticalAlignment;
        // 复制背景色
        if (mainStyle.FillPattern == FillPattern.SolidForeground)
        {
          borderStyle.FillPattern = FillPattern.SolidForeground;
          if (workbook is XSSFWorkbook && mainStyle is XSSFCellStyle xssfMain)
          {
            ((XSSFCellStyle)borderStyle).FillForegroundXSSFColor = xssfMain.FillForegroundXSSFColor;
          }
          else
          {
            borderStyle.FillForegroundColor = mainStyle.FillForegroundColor;
          }
        }
        // XSSF 需要单独设置边框颜色
        if (workbook is XSSFWorkbook && mainStyle is XSSFCellStyle xssfSrc)
        {
          var xssfDest = (XSSFCellStyle)borderStyle;
          try { xssfDest.SetTopBorderColor(xssfSrc.TopBorderXSSFColor); } catch { }
          try { xssfDest.SetBottomBorderColor(xssfSrc.BottomBorderXSSFColor); } catch { }
          try { xssfDest.SetLeftBorderColor(xssfSrc.LeftBorderXSSFColor); } catch { }
          try { xssfDest.SetRightBorderColor(xssfSrc.RightBorderXSSFColor); } catch { }
        }
      }
      else
      {
        // 主单元格无样式时，给子单元格设置默认细边框
        borderStyle.BorderTop = BorderStyle.Thin;
        borderStyle.BorderBottom = BorderStyle.Thin;
        borderStyle.BorderLeft = BorderStyle.Thin;
        borderStyle.BorderRight = BorderStyle.Thin;
      }
      return borderStyle;
    }

    /// <summary>
    /// 从 CSS style 字符串创建 NPOI ICellStyle
    /// </summary>
    private ICellStyle CreateCellStyleFromCss(IWorkbook workbook, string styleStr, Dictionary<string, ICellStyle> cache, bool isTh = false, bool skipFont = false)
    {
      if (string.IsNullOrEmpty(styleStr) && !isTh) return null;

      // 有 style 或是 th 标签时都需要创建样式
      // skipFont 时缓存 key 需区分，因为同样的 styleStr 可能产生不同的 CellStyle
      var cacheKey = styleStr + (isTh ? "|th" : "") + (skipFont ? "|sf" : "");
      if (cache.ContainsKey(cacheKey)) return cache[cacheKey];

      var css = ParseCssStyle(styleStr ?? "");
      var cellStyle = workbook.CreateCellStyle();

      // 字体（当 skipFont=true 时不设置字体属性，避免 NPOI 丢失 RichText 的字体信息）
      if (!skipFont)
      {
        var font = workbook.CreateFont();
        var fontChanged = false;

        // <th> 默认粗体
        if (isTh)
        {
          font.IsBold = true;
          fontChanged = true;
        }

        if (css.ContainsKey("font-size"))
        {
          var pt = ParseFontPtValue(css["font-size"]);
          if (pt > 0) { font.FontHeightInPoints = pt; fontChanged = true; }
        }

        if (css.ContainsKey("font-weight"))
        {
          var fw = css["font-weight"];
          if (fw == "bold" || fw == "bolder" || (int.TryParse(fw, out var fwVal) && fwVal >= 700))
          {
            font.IsBold = true; fontChanged = true;
          }
        }

        if (css.ContainsKey("font-style") && css["font-style"] == "italic")
        {
          font.IsItalic = true; fontChanged = true;
        }

        if (css.ContainsKey("font-family"))
        {
          var fontFamily = System.Net.WebUtility.HtmlDecode(css["font-family"]).Trim('\'', '"');
          if (!string.IsNullOrEmpty(fontFamily))
          {
            font.FontName = fontFamily;
            fontChanged = true;
          }
        }

        // 处理复合 font 属性: font: bold 14px/1.5 Arial
        if (css.ContainsKey("font"))
        {
          var fontVal = css["font"];
          if (fontVal.Contains("bold")) { font.IsBold = true; fontChanged = true; }
          if (fontVal.Contains("italic")) { font.IsItalic = true; fontChanged = true; }
          var fontSizeMatch = System.Text.RegularExpressions.Regex.Match(fontVal, @"(\d+(?:\.\d+)?)px");
          if (fontSizeMatch.Success)
          {
            var pt = ParseFontPtValue(fontSizeMatch.Groups[1].Value + "px");
            if (pt > 0) { font.FontHeightInPoints = pt; fontChanged = true; }
          }
        }

        if (fontChanged) cellStyle.SetFont(font);
      }

      // 对齐
      // <th> 默认居中
      if (isTh) cellStyle.Alignment = HorizontalAlignment.Center;

      if (css.ContainsKey("text-align"))
      {
        var ta = css["text-align"];
        if (ta == "center") cellStyle.Alignment = HorizontalAlignment.Center;
        else if (ta == "right") cellStyle.Alignment = HorizontalAlignment.Right;
        else if (ta == "left") cellStyle.Alignment = HorizontalAlignment.Left;
      }

      // HTML 表格默认垂直居中，Excel 默认底部对齐，需要覆盖
      cellStyle.VerticalAlignment = VerticalAlignment.Center;

      if (css.ContainsKey("vertical-align"))
      {
        var va = css["vertical-align"];
        if (va == "top" || va == "text-top") cellStyle.VerticalAlignment = VerticalAlignment.Top;
        else if (va == "bottom" || va == "text-bottom") cellStyle.VerticalAlignment = VerticalAlignment.Bottom;
        // middle/center 保持默认的 Center
      }

      // 背景色
      if (css.ContainsKey("background-color"))
      {
        var bgColor = css["background-color"];
        if (bgColor != "transparent" && bgColor != "none")
        {
          var rgb = ParseColorToRgb(bgColor);
          if (rgb != null)
          {
            var colorObj = GetColorObj(workbook, rgb);
            if (colorObj != null)
            {
              if (workbook is XSSFWorkbook && colorObj is XSSFColor xssfColor)
              {
                ((XSSFCellStyle)cellStyle).FillForegroundXSSFColor = xssfColor;
              }
              else if (colorObj is HSSFColor hssfColor)
              {
                cellStyle.FillForegroundColor = hssfColor.Indexed;
              }
              cellStyle.FillPattern = FillPattern.SolidForeground;
            }
          }
        }
      }

      // 边框 — 始终设置默认细边框（HTML 表格单元格默认有边线）
      var hasBorder = false;
      if (css.ContainsKey("border"))
      {
        var b = css["border"];
        if (b != "none" && b != "0") hasBorder = true;
      }
      if (css.ContainsKey("border-top") || css.ContainsKey("border-bottom") || css.ContainsKey("border-left") || css.ContainsKey("border-right"))
        hasBorder = true;

      if (hasBorder)
      {
        cellStyle.BorderTop = BorderStyle.Thin;
        cellStyle.BorderBottom = BorderStyle.Thin;
        cellStyle.BorderLeft = BorderStyle.Thin;
        cellStyle.BorderRight = BorderStyle.Thin;
        var borderColor = GetColorObj(workbook, "000000");
        if (borderColor != null)
        {
          if (workbook is XSSFWorkbook && borderColor is XSSFColor xssfBorderColor)
          {
            var xssfCellStyle = (XSSFCellStyle)cellStyle;
            xssfCellStyle.SetTopBorderColor(xssfBorderColor);
            xssfCellStyle.SetBottomBorderColor(xssfBorderColor);
            xssfCellStyle.SetLeftBorderColor(xssfBorderColor);
            xssfCellStyle.SetRightBorderColor(xssfBorderColor);
          }
          else if (borderColor is HSSFColor hssfBorderColor)
          {
            cellStyle.TopBorderColor = hssfBorderColor.Indexed;
            cellStyle.BottomBorderColor = hssfBorderColor.Indexed;
            cellStyle.LeftBorderColor = hssfBorderColor.Indexed;
            cellStyle.RightBorderColor = hssfBorderColor.Indexed;
          }
        }
      }
      else
      {
        // 无显式 border 属性时也设置默认细边框，确保合并单元格边线完整
        cellStyle.BorderTop = BorderStyle.Thin;
        cellStyle.BorderBottom = BorderStyle.Thin;
        cellStyle.BorderLeft = BorderStyle.Thin;
        cellStyle.BorderRight = BorderStyle.Thin;
      }

      cellStyle.WrapText = true;

      cache[cacheKey] = cellStyle;
      return cellStyle;
    }

    /// <summary>
    /// 解析 CSS 样式字符串为 Dictionary
    /// </summary>
    private Dictionary<string, string> ParseCssStyle(string styleStr)
    {
      var result = new Dictionary<string, string>();
      if (string.IsNullOrEmpty(styleStr)) return result;
      // 先将 HTML 实体中的 &quot; 替换为单引号，避免 ; 被误当作 CSS 分隔符
      // 例如：font-family: &quot;Times New Roman&quot;;font-size: 14px
      // &quot; 的 ; 会被 split(';') 截断，导致解析错误
      styleStr = styleStr.Replace("&quot;", "'").Replace("&apos;", "'");
      foreach (var part in styleStr.Split(';'))
      {
        var kv = part.Split(new[] { ':' }, 2);
        if (kv.Length == 2)
        {
          result[kv[0].Trim()] = kv[1].Trim();
        }
      }
      return result;
    }

    /// <summary>
    /// 解析 CSS 像素值，返回像素数（pt 转换为 px: pt * 1.33）
    /// </summary>
    private int ParsePxValue(string val)
    {
      if (string.IsNullOrEmpty(val)) return 0;
      var match = System.Text.RegularExpressions.Regex.Match(val, @"^(\d+(?:\.\d+)?)(px|pt|em|rem)?");
      if (!match.Success) return 0;
      var num = double.Parse(match.Groups[1].Value);
      var unit = match.Groups[2].Success ? match.Groups[2].Value : "px";
      if (unit == "pt") return (int)(num * 1.33);
      if (unit == "em" || unit == "rem") return (int)(num * 16);
      return (int)num;
    }

    /// <summary>
    /// 解析 CSS 字体大小为 pt 值（NPOI FontHeightInPoints 使用 pt 单位）
    /// </summary>
    private short ParseFontPtValue(string val)
    {
      if (string.IsNullOrEmpty(val)) return 0;
      var match = System.Text.RegularExpressions.Regex.Match(val, @"^(\d+(?:\.\d+)?)(px|pt|em|rem)?");
      if (!match.Success) return 0;
      var num = double.Parse(match.Groups[1].Value);
      var unit = match.Groups[2].Success ? match.Groups[2].Value : "px";
      if (unit == "pt") return (short)num;
      if (unit == "px") return (short)(num * 0.75); // px → pt
      if (unit == "em" || unit == "rem") return (short)(num * 12); // em → pt (假设基准 12pt)
      return (short)(num * 0.75); // 默认当 px 处理
    }

    /// <summary>
    /// 解析颜色字符串为 RGB 十六进制（不含 #）
    /// </summary>
    private string ParseColorToRgb(string colorStr)
    {
      if (string.IsNullOrEmpty(colorStr)) return null;
      // #rrggbb
      var hexMatch = System.Text.RegularExpressions.Regex.Match(colorStr, @"^#([0-9a-fA-F]{6})$");
      if (hexMatch.Success) return hexMatch.Groups[1].Value.ToUpper();
      // rgb(r,g,b)
      var rgbMatch = System.Text.RegularExpressions.Regex.Match(colorStr, @"^rgb\((\d+),\s*(\d+),\s*(\d+)\)$");
      if (rgbMatch.Success)
      {
        var r = int.Parse(rgbMatch.Groups[1].Value).ToString("X2");
        var g = int.Parse(rgbMatch.Groups[2].Value).ToString("X2");
        var b = int.Parse(rgbMatch.Groups[3].Value).ToString("X2");
        return (r + g + b).ToUpper();
      }
      // 常见颜色名
      var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {
        { "black", "000000" }, { "white", "FFFFFF" }, { "red", "FF0000" },
        { "green", "00FF00" }, { "blue", "0000FF" }, { "yellow", "FFFF00" },
        { "gray", "808080" }, { "grey", "808080" }
      };
      return named.ContainsKey(colorStr) ? named[colorStr] : null;
    }

    /// <summary>
    /// 获取 NPOI 颜色值（XSSF 使用 XSSFColor，HSSF 使用调色板索引）
    /// 返回值直接赋给 CellStyle 的颜色属性
    /// </summary>
    private IColor GetColorObj(IWorkbook workbook, string rgb)
    {
      if (string.IsNullOrEmpty(rgb) || rgb.Length != 6) return null;

      var r = Convert.ToByte(rgb.Substring(0, 2), 16);
      var g = Convert.ToByte(rgb.Substring(2, 2), 16);
      var b = Convert.ToByte(rgb.Substring(4, 2), 16);

      if (workbook is XSSFWorkbook)
      {
        // XSSF: 直接使用 XSSFColor，支持任意 RGB
        return new XSSFColor(new byte[] { r, g, b });
      }
      else if (workbook is HSSFWorkbook hssfWb)
      {
        // HSSF: 使用调色板查找最接近的颜色
        var palette = hssfWb.GetCustomPalette();
        var color = palette.FindSimilarColor(r, g, b);
        return color;
      }

      return null;
    }

    /// <summary>
    /// 使用 RichText 设置单元格内容，支持混合格式（粗体、斜体、不同字体/字号、上下标）
    /// 解析 innerHtml 中的 <span>/<strong>/<em>/<sub>/<sup> 等标签，
    /// 每个片段生成独立的 IFont 并应用到 XSSFRichTextString
    /// 返回 true 表示使用了 RichText（调用方不应在 CellStyle 中设置字体属性，否则 NPOI 会丢失 RichText 的字体信息）
    /// </summary>
    private bool SetCellRichText(IWorkbook workbook, ICell cell, string innerHtml)
    {
      try
      {
        // 解析 innerHtml 为文本片段列表
        var fragments = ParseHtmlToFragments(innerHtml);
        if (fragments.Count == 0) return false;

        // 调试：输出 innerHtml 和 fragments
        if (innerHtml.Contains("font-weight") || innerHtml.Contains("bold"))
        {
          Logger.Info("SetCellRichText innerHtml contains bold: " + innerHtml.Substring(0, Math.Min(innerHtml.Length, 200)));
        }
        foreach (var f in fragments)
        {
          if (f.Bold)
          {
            Logger.Info("SetCellRichText BOLD fragment: text=\"" + (f.Text.Length > 20 ? f.Text.Substring(0, 20) + "..." : f.Text) + "\" bold=" + f.Bold);
          }
        }

        // 合并所有片段的文本
        var fullText = new StringBuilder();
        foreach (var frag in fragments)
        {
          fullText.Append(frag.Text);
        }
        var text = fullText.ToString();

        if (string.IsNullOrEmpty(text.Trim()))
        {
          cell.SetCellValue(text);
          return false;
        }

        // 检查是否有任何片段包含特殊格式 → 需要 RichText
        var hasSpecialFormat = false;
        foreach (var frag in fragments)
        {
          if (frag.Bold || frag.Italic || frag.Subscript || frag.Superscript
            || !string.IsNullOrEmpty(frag.FontFamily) || frag.FontSizePt > 0
            || !string.IsNullOrEmpty(frag.Color))
          {
            hasSpecialFormat = true;
            break;
          }
        }

        // 多片段且格式不同 → 需要 RichText
        var needRichText = false;
        if (fragments.Count > 1)
        {
          for (var i = 1; i < fragments.Count; i++)
          {
            if (!FragmentsHaveSameFormat(fragments[0], fragments[i]))
            {
              needRichText = true;
              break;
            }
          }
        }

        // 即使所有片段格式相同，只要有 bold/italic/自定义字体等特殊格式，就必须用 RichText
        // 因为纯文本 cell.SetCellValue 不会保留任何字体格式
        if (!needRichText && hasSpecialFormat)
        {
          needRichText = true;
        }

        if (!needRichText)
        {
          // 格式统一且无特殊格式，直接设纯文本
          cell.SetCellValue(System.Net.WebUtility.HtmlDecode(text));
          return false;
        }

        // 使用 XSSFRichTextString
        var richText = new XSSFRichTextString(System.Net.WebUtility.HtmlDecode(text));

        // 预计算每个片段的起止索引
        var fragRanges = new List<int[]>(); // [startIdx, endIdx]
        var sIdx = 0;
        for (var i = 0; i < fragments.Count; i++)
        {
          var fragText = System.Net.WebUtility.HtmlDecode(fragments[i].Text);
          var eIdx = sIdx + fragText.Length;
          fragRanges.Add(new int[] { sIdx, eIdx });
          sIdx = eIdx;
        }

        // 为每个片段设置字体
        // 重要：NPOI 2.5.6 的 XSSFRichTextString.ApplyFont 有 bug，
        // 从前往后调用会导致前面的 <rPr> 被覆盖丢失，
        // 必须从后往前调用 ApplyFont（先设置最后一个片段，再设置前面的）
        for (var i = fragments.Count - 1; i >= 0; i--)
        {
          var frag = fragments[i];
          var startIdx = fragRanges[i][0];
          var endIdx = fragRanges[i][1];

          if (endIdx > startIdx && !string.IsNullOrEmpty(System.Net.WebUtility.HtmlDecode(frag.Text).Trim()))
          {
            var font = CreateFontFromFragment(workbook, frag);
            if (font != null)
            {
              richText.ApplyFont(startIdx, endIdx, font);
            }
          }
        }

        cell.SetCellValue(richText);
        return true;
      }
      catch (Exception ex)
      {
        // 降级：纯文本
        var plainText = System.Text.RegularExpressions.Regex.Replace(innerHtml, @"<[^>]+>", "").Trim();
        cell.SetCellValue(System.Net.WebUtility.HtmlDecode(plainText));
        Logger.Info("SetCellRichText 降级为纯文本: " + ex.Message);
        return false;
      }
    }

    /// <summary>
    /// HTML 文本片段
    /// </summary>
    private class HtmlFragment
    {
      public string Text { get; set; }
      public bool Bold { get; set; }
      public bool Italic { get; set; }
      public bool Subscript { get; set; }
      public bool Superscript { get; set; }
      public string FontFamily { get; set; }
      public double FontSizePt { get; set; }
      public string Color { get; set; }
    }

    /// <summary>
    /// 解析 innerHtml 为文本片段列表，保留每个片段的格式信息
    /// </summary>
    private List<HtmlFragment> ParseHtmlToFragments(string html)
    {
      var fragments = new List<HtmlFragment>();
      // 逐字符解析，跟踪当前格式上下文
      var boldStack = 0;
      var italicStack = 0;
      var subStack = 0;
      var supStack = 0;
      // 用栈管理 <span> 嵌套的样式（外层 span 的样式在关闭内层 span 后应恢复）
      var spanStyleStack = new Stack<Dictionary<string, string>>();
      var currentFontFamily = "";
      var currentFontSize = 0.0;
      var currentColor = "";

      var currentText = new StringBuilder();

      // 用正则逐标签解析
      var tagRegex = new Regex(@"<(/?)(strong|b|em|i|sub|sup|span|p|br)\s*([^>]*?)(?:\s*/?)>", RegexOptions.IgnoreCase);
      var pos = 0;

      // 提取文本节点（标签之间的内容）
      void FlushText()
      {
        if (currentText.Length > 0)
        {
          fragments.Add(new HtmlFragment
          {
            Text = currentText.ToString(),
            Bold = boldStack > 0,
            Italic = italicStack > 0,
            Subscript = subStack > 0,
            Superscript = supStack > 0,
            FontFamily = currentFontFamily,
            FontSizePt = currentFontSize,
            Color = currentColor
          });
          currentText.Clear();
        }
      }

      while (pos < html.Length)
      {
        var match = tagRegex.Match(html, pos);
        if (match.Success && match.Index == pos)
        {
          // 先保存标签前的文本
          FlushText();

          var isClosing = match.Groups[1].Value == "/";
          var tagName = match.Groups[2].Value.ToLower();
          var tagAttrs = match.Groups[3].Value;

          switch (tagName)
          {
            case "strong":
            case "b":
              if (isClosing) boldStack = Math.Max(0, boldStack - 1);
              else boldStack++;
              break;
            case "em":
            case "i":
              if (isClosing) italicStack = Math.Max(0, italicStack - 1);
              else italicStack++;
              break;
            case "sub":
              if (isClosing) subStack = Math.Max(0, subStack - 1);
              else subStack++;
              break;
            case "sup":
              if (isClosing) supStack = Math.Max(0, supStack - 1);
              else supStack++;
              break;
            case "br":
              // <br> 换行 → 在文本中插入换行符
              currentText.Append("\n");
              break;
            case "p":
              // <p> 忽略（text-align 已在外层处理），<br> 在 p 间换行
              // 闭合 </p> 后如果有新 <p>，Excel 中用换行分隔
              if (isClosing && pos + match.Length < html.Length)
              {
                // 检查后面是否还有 <p>（段落间换行）
                var rest = html.Substring(pos + match.Length).TrimStart();
                if (rest.StartsWith("<p", StringComparison.OrdinalIgnoreCase))
                {
                  currentText.Append("\n");
                }
              }
              break;
            case "span":
              if (!isClosing)
              {
                // 保存当前样式到栈（用于嵌套 span 恢复）
                spanStyleStack.Push(new Dictionary<string, string>
                {
                  { "fontFamily", currentFontFamily },
                  { "fontSize", currentFontSize.ToString() },
                  { "color", currentColor },
                  { "boldAdded", "0" },
                  { "italicAdded", "0" }
                });
                // 从 <span style="..."> 中提取 font-family, font-size, color, font-weight, font-style
                var styleAttr = GetHtmlAttr(match.Value, "style");
                if (!string.IsNullOrEmpty(styleAttr))
                {
                  var css = ParseCssStyle(styleAttr);
                  if (css.ContainsKey("font-family"))
                  {
                    currentFontFamily = css["font-family"].Trim('\'', '"');
                  }
                  if (css.ContainsKey("font-size"))
                  {
                    currentFontSize = ParseFontPtValue(css["font-size"]);
                  }
                  if (css.ContainsKey("color"))
                  {
                    currentColor = css["color"];
                  }
                  // 处理 font-weight:bold / font-weight:700
                  if (css.ContainsKey("font-weight"))
                  {
                    var fw = css["font-weight"].Trim();
                    if (fw == "bold" || fw == "bolder" || fw == "700" || fw == "600" || fw == "800" || fw == "900")
                    {
                      boldStack++;
                      // 标记这个 bold 是由 span 的 font-weight 贡献的，关闭 span 时需要恢复
                      if (spanStyleStack.Count > 0)
                      {
                        var top = spanStyleStack.Peek();
                        top["boldAdded"] = "1";
                      }
                    }
                  }
                  // 处理 font-style:italic
                  if (css.ContainsKey("font-style"))
                  {
                    var fs = css["font-style"].Trim();
                    if (fs == "italic" || fs == "oblique")
                    {
                      italicStack++;
                      if (spanStyleStack.Count > 0)
                      {
                        var top = spanStyleStack.Peek();
                        top["italicAdded"] = "1";
                      }
                    }
                  }
                  // 处理 font shorthand: font: bold 14px/1.5 Arial
                  if (css.ContainsKey("font"))
                  {
                    var fontVal = css["font"];
                    if (fontVal.Contains("bold"))
                    {
                      boldStack++;
                      if (spanStyleStack.Count > 0)
                      {
                        var top = spanStyleStack.Peek();
                        top["boldAdded"] = "1";
                      }
                    }
                    if (fontVal.Contains("italic"))
                    {
                      italicStack++;
                      if (spanStyleStack.Count > 0)
                      {
                        var top = spanStyleStack.Peek();
                        top["italicAdded"] = "1";
                      }
                    }
                    var ffMatch = Regex.Match(fontVal, @"(?:\d+(?:\.\d+)?px\s+)?([A-Za-z\u4e00-\u9fff\s]+)$");
                    if (ffMatch.Success) currentFontFamily = ffMatch.Groups[1].Value.Trim();
                    var fsMatch = Regex.Match(fontVal, @"(\d+(?:\.\d+)?)px");
                    if (fsMatch.Success) currentFontSize = ParseFontPtValue(fsMatch.Groups[1].Value + "px");
                  }
                }
              }
              else
              {
                // </span> 关闭时从栈中恢复外层样式
                if (spanStyleStack.Count > 0)
                {
                  var prevStyle = spanStyleStack.Pop();
                  // 恢复由本 span 贡献的 bold/italic
                  if (prevStyle.ContainsKey("boldAdded") && prevStyle["boldAdded"] == "1")
                    boldStack = Math.Max(0, boldStack - 1);
                  if (prevStyle.ContainsKey("italicAdded") && prevStyle["italicAdded"] == "1")
                    italicStack = Math.Max(0, italicStack - 1);
                  // 恢复其他样式
                  currentFontFamily = prevStyle.ContainsKey("fontFamily") ? prevStyle["fontFamily"] : "";
                  currentFontSize = prevStyle.ContainsKey("fontSize") && double.TryParse(prevStyle["fontSize"], out var fs) ? fs : 0;
                  currentColor = prevStyle.ContainsKey("color") ? prevStyle["color"] : "";
                }
                else
                {
                  currentFontFamily = "";
                  currentFontSize = 0;
                  currentColor = "";
                }
              }
              break;
          }

          pos = match.Index + match.Length;
        }
        else if (match.Success && match.Index > pos)
        {
          // 标签之前有文本
          currentText.Append(html.Substring(pos, match.Index - pos));
          pos = match.Index;
        }
        else
        {
          // 没有更多标签，剩余都是文本
          currentText.Append(html.Substring(pos));
          break;
        }
      }

      FlushText();
      return fragments;
    }

    /// <summary>
    /// 判断两个片段是否有相同格式
    /// </summary>
    private bool FragmentsHaveSameFormat(HtmlFragment a, HtmlFragment b)
    {
      return a.Bold == b.Bold
        && a.Italic == b.Italic
        && a.Subscript == b.Subscript
        && a.Superscript == b.Superscript
        && a.FontFamily == b.FontFamily
        && Math.Abs(a.FontSizePt - b.FontSizePt) < 0.1
        && a.Color == b.Color;
    }

    /// <summary>
    /// 根据 HtmlFragment 创建 NPOI IFont
    /// </summary>
    private IFont CreateFontFromFragment(IWorkbook workbook, HtmlFragment frag)
    {
      var font = workbook.CreateFont();
      var changed = false;

      if (frag.Bold) { font.IsBold = true; changed = true; }
      if (frag.Italic) { font.IsItalic = true; changed = true; }
      if (frag.Subscript) { font.TypeOffset = FontSuperScript.Sub; changed = true; }
      if (frag.Superscript) { font.TypeOffset = FontSuperScript.Super; changed = true; }
      if (!string.IsNullOrEmpty(frag.FontFamily)) { font.FontName = System.Net.WebUtility.HtmlDecode(frag.FontFamily); changed = true; }
      if (frag.FontSizePt > 0) { font.FontHeightInPoints = frag.FontSizePt; changed = true; }
      if (!string.IsNullOrEmpty(frag.Color))
      {
        var rgb = ParseColorToRgb(frag.Color);
        if (rgb != null)
        {
          try
          {
            var bytes = new byte[] { 255, Convert.ToByte(rgb.Substring(0, 2), 16), Convert.ToByte(rgb.Substring(2, 2), 16), Convert.ToByte(rgb.Substring(4, 2), 16) };
            font.Color = new XSSFColor(bytes).Indexed;
          }
          catch { }
          changed = true;
        }
      }

      return changed ? font : null;
    }

    /// <summary>
    /// 将 HTML 中的图片（base64 或 URL）插入到 Excel 单元格
    /// </summary>
    private void InsertImageToCell(IWorkbook workbook, ISheet sheet, int rowIdx, int colIdx, string imgSrc, string imgStyle, string imgWidthAttr, string imgHeightAttr)
    {
      try
      {
        byte[] imgBytes = null;
        var pictureType = PictureType.PNG;

        // 解析 base64 图片
        if (imgSrc.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
          // 格式: data:image/png;base64,xxxxx
          var base64Part = imgSrc;
          if (base64Part.Contains(","))
          {
            base64Part = base64Part.Substring(base64Part.IndexOf(",") + 1);
          }
          imgBytes = Convert.FromBase64String(base64Part);

          // 解析图片格式
          if (imgSrc.Contains("image/png")) pictureType = PictureType.PNG;
          else if (imgSrc.Contains("image/jpeg") || imgSrc.Contains("image/jpg")) pictureType = PictureType.JPEG;
          else if (imgSrc.Contains("image/gif")) pictureType = PictureType.GIF;
          else if (imgSrc.Contains("image/bmp")) pictureType = PictureType.BMP;
        }

        if (imgBytes == null || imgBytes.Length == 0) return;

        // 添加图片到 workbook
        var pictureIdx = workbook.AddPicture(imgBytes, pictureType);

        // 创建绘图对象
        var drawing = sheet.CreateDrawingPatriarch();

        // 设置锚点（图片位置和大小）
        var helper = sheet.Workbook.GetCreationHelper();
        var anchor = helper.CreateClientAnchor();
        anchor.Col1 = colIdx;
        anchor.Row1 = rowIdx;
        anchor.Col2 = colIdx + 1;
        anchor.Row2 = rowIdx + 1;

        // 计算图片尺寸
        var imgWidthPx = 120; // 默认宽度
        var imgHeightPx = 60; // 默认高度

        // 从 width 属性获取
        if (!string.IsNullOrEmpty(imgWidthAttr))
        {
          var w = ParsePxValue(imgWidthAttr);
          if (w > 0) imgWidthPx = w;
        }

        // 从 style 中获取尺寸
        if (!string.IsNullOrEmpty(imgStyle))
        {
          var cssMap = ParseCssStyle(imgStyle);
          if (cssMap.ContainsKey("width"))
          {
            var w = ParsePxValue(cssMap["width"]);
            if (w > 0) imgWidthPx = w;
          }
          if (cssMap.ContainsKey("height"))
          {
            var h = ParsePxValue(cssMap["height"]);
            if (h > 0) imgHeightPx = h;
          }
        }

        // 设置行高以容纳图片（1pt ≈ 1.33px）
        var row = sheet.GetRow(rowIdx);
        if (row != null)
        {
          var heightPt = (short)(imgHeightPx / 1.33);
          if (heightPt > row.Height || row.Height == 0)
          {
            row.Height = (short)Math.Max((int)heightPt, 30);
          }
        }

        // 设置列宽以容纳图片
        var currentWidth = sheet.GetColumnWidth(colIdx);
        var needWidth = (int)(imgWidthPx / 8.0 * 256);
        if (needWidth > currentWidth)
        {
          sheet.SetColumnWidth(colIdx, needWidth);
        }

        // anchor.AnchorType 在 NPOI 2.5.6 中不可用，跳过

        var picture = drawing.CreatePicture(anchor, pictureIdx);
        // 自动调整图片大小（基于单元格）
        picture.Resize();
      }
      catch (Exception ex)
      {
        Logger.Info("InsertImageToCell 异常: " + ex.Message);
      }
    }

    /// <summary>
    /// 将 IWorkbook 写入字节数组
    /// </summary>
    private byte[] WriteWorkbookToBytes(IWorkbook workbook)
    {
      using (var ms = new MemoryStream())
      {
        workbook.Write(ms);
        return ms.ToArray();
      }
    }

    /// <summary>
    /// 使用 NPOI 创建空白 xlsx 文件
    /// </summary>
    private byte[] CreateBlankXlsx()
    {
      IWorkbook workbook = new XSSFWorkbook();
      var sheet = workbook.CreateSheet("Sheet1");
      // 创建一行一列，确保 OnlyOffice 可以正常打开
      var row = sheet.CreateRow(0);
      row.CreateCell(0);
      return WriteWorkbookToBytes(workbook);
    }

    /// <summary>
    /// 调试用：保存 HTML 内容到临时文件，返回可访问的 URL
    /// POST /api/exceleditor/debug-save-html
    /// </summary>
    [HttpPost("debug-save-html")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> DebugSaveHtml()
    {
      try
      {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
          body = await reader.ReadToEndAsync();
        }

        var jobj = JObject.Parse(body);
        string htmlContent = jobj["html"]?.ToString();
        string label = jobj["label"]?.ToString() ?? "debug";

        if (string.IsNullOrEmpty(htmlContent))
        {
          return BadRequest(new { Message = "HTML 内容为空" });
        }

        string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
        string tempDir = Path.Combine(rootPath, "临时");

        if (!Directory.Exists(tempDir))
        {
          Directory.CreateDirectory(tempDir);
        }

        string fileName = "debug_" + label + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
        string filePath = Path.Combine(tempDir, fileName);

        // 包裹为完整 HTML 文档
        var fullHtml = new StringBuilder();
        fullHtml.AppendLine("<!DOCTYPE html>");
        fullHtml.AppendLine("<html><head><meta charset=\"utf-8\">");
        fullHtml.AppendLine("<style>table { border-collapse: collapse; } td, th { border: 1px solid #999; padding: 2px 5px; }</style>");
        fullHtml.AppendLine("</head><body>");
        fullHtml.AppendLine(htmlContent);
        fullHtml.AppendLine("</body></html>");

        System.IO.File.WriteAllText(filePath, fullHtml.ToString(), Encoding.UTF8);

        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
        string url = apiUrl + "/api/exceleditor/debug-view-html?file=" + fileName;

        return Ok(new { url, fileName, filePath });
      }
      catch (Exception ex)
      {
        Logger.Info("DebugSaveHtml 异常: " + ex.Message);
        return BadRequest(new { Message = "保存失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 调试用：查看保存的 HTML 文件
    /// GET /api/exceleditor/debug-view-html?file=xxx.html
    /// </summary>
    [HttpGet("debug-view-html")]
    [EnableCors("AllowHeaders")]
    public IActionResult DebugViewHtml(string file)
    {
      if (string.IsNullOrEmpty(file) || file.Contains(".."))
      {
        return BadRequest(new { Message = "参数无效" });
      }

      string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
      string tempDir = Path.Combine(rootPath, "临时");
      string filePath = Path.Combine(tempDir, file);

      if (!System.IO.File.Exists(filePath))
      {
        return NotFound(new { Message = "文件不存在" });
      }

      var stream = System.IO.File.OpenRead(filePath);
      return File(stream, "text/html", file);
    }

    private void CleanupOldFiles()
    {
      var expired = new List<string>();
      foreach (var kv in _tempFiles)
      {
        if (DateTime.Now - kv.Value.CreateTime > TimeSpan.FromHours(24))
        {
          try
          {
            if (System.IO.File.Exists(kv.Value.FilePath))
            {
              System.IO.File.Delete(kv.Value.FilePath);
            }
            if (!string.IsNullOrEmpty(kv.Value.HtmlFilePath) && System.IO.File.Exists(kv.Value.HtmlFilePath))
            {
              System.IO.File.Delete(kv.Value.HtmlFilePath);
            }
          }
          catch { }
          expired.Add(kv.Key);
        }
      }
      foreach (var k in expired)
      {
        _tempFiles.Remove(k);
      }
    }

    private class TempFileInfo
    {
      public string FilePath { get; set; }
      public string HtmlFilePath { get; set; }
      public string FileName { get; set; }
      public DateTime CreateTime { get; set; }
      /// <summary>OnlyOffice callback 已收到（status=2/4/6 都算）</summary>
      public bool CallbackReceived { get; set; }
      /// <summary>OnlyOffice 已通过 callback 保存了编辑后的文件（status=2/6 且有 url）</summary>
      public bool SavedByCallback { get; set; }
      /// <summary>OnlyOffice 编辑器使用的 docKey（带时间戳后缀），用于 Command API</summary>
      public string DocKey { get; set; }
    }

    /// <summary>
    /// 调试用：端到端测试 HTML→xlsx→HTML
    /// GET /api/exceleditor/debug-test?saveFiles=true
    /// </summary>
    [HttpGet("debug-test")]
    [EnableCors("AllowHeaders")]
    public IActionResult DebugTest(bool saveFiles = true)
    {
      try
      {
        // 使用实际的 HTML 文件进行测试
        string testHtml;
        try
        {
          string rootPath3 = ConfigHelper.GetConfig("Upload:ROOT");
          string tempDir3 = Path.Combine(rootPath3, "临时");
          var htmlFiles = System.IO.Directory.GetFiles(tempDir3, "debug_input_*.html")
            .OrderByDescending(f => f).ToArray();
          if (htmlFiles.Length > 0)
          {
            var rawHtml = System.IO.File.ReadAllText(htmlFiles[0], Encoding.UTF8);
            // 从完整 HTML 中提取 <table> 部分
            var tableMatch = System.Text.RegularExpressions.Regex.Match(rawHtml, @"<table[^>]*>.*?</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            testHtml = tableMatch.Success ? tableMatch.Value : rawHtml;
            Logger.Info("DebugTest 使用文件: " + htmlFiles[0]);
          }
          else
          {
            testHtml = "<table><tr><td>no test file</td></tr></table>";
          }
        }
        catch (Exception ex2)
        {
          testHtml = "<table><tr><td>error: " + ex2.Message + "</td></tr></table>";
        }

        Logger.Info("=== DebugTest 开始 ===");
        Logger.Info("输入HTML:\n" + testHtml);

        // HTML → xlsx
        var xlsxBytes = HtmlToXlsx(testHtml);

        // xlsx → HTML（验证往返）
        var resultHtml = XlsxToHtml(xlsxBytes, out _);

        Logger.Info("输出HTML:\n" + resultHtml);
        Logger.Info("=== DebugTest 结束 ===");

        if (saveFiles)
        {
          string rootPath = ConfigHelper.GetConfig("Upload:ROOT");
          string tempDir = Path.Combine(rootPath, "临时");
          if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

          var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
          var xlsxPath = Path.Combine(tempDir, "debug_test_" + timestamp + ".xlsx");
          System.IO.File.WriteAllBytes(xlsxPath, xlsxBytes);

          var htmlPath = Path.Combine(tempDir, "debug_test_result_" + timestamp + ".html");
          var fullHtml = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>" + resultHtml + "</body></html>";
          System.IO.File.WriteAllText(htmlPath, fullHtml, Encoding.UTF8);

          return Ok(new { inputHtml = testHtml, outputHtml = resultHtml, xlsxPath, htmlPath });
        }

        return Ok(new { inputHtml = testHtml, outputHtml = resultHtml });
      }
      catch (Exception ex)
      {
        Logger.Info("DebugTest 异常: " + ex.Message + "\n" + ex.StackTrace);
        return BadRequest(new { Message = "测试失败: " + ex.Message });
      }
    }

    /// <summary>
    /// 将列索引(0-based)转为 Excel 列字母(A, B, ..., Z, AA, AB, ...)
    /// </summary>
    private static string GetColumnLetter(int colIndex)
    {
      var result = new StringBuilder();
      int col = colIndex;
      while (col >= 0)
      {
        result.Insert(0, (char)('A' + (col % 26)));
        col = col / 26 - 1;
      }
      return result.ToString();
    }

    /// <summary>
    /// 解析字符串拼接公式，提取 formula 定义
    /// 例：G2&amp;"=$FUNC("&amp;C2&amp;","&amp;D2&amp;","&amp;E2&amp;")"
    /// → FUNC(${FC2},${FD2},${FE2})
    /// </summary>
    private string ParseConcatFormula(string formula, string currentRef)
    {
      try
      {
        // 去掉开头的 "当前引用&"
        var rest = formula.Substring(currentRef.Length + 1); // 跳过 "G2&"

        // 用正则解析 & 分隔的片段：字符串字面量 或 单元格引用
        var parts = new List<string>();
        var tokenRegex = new Regex(@"""((?:[^""]|"""")*)""|([A-Z]+\d+)");
        var m = tokenRegex.Match(rest);
        while (m.Success)
        {
          if (m.Groups[1].Success)
          {
            // 字符串字面量（去掉引号，处理转义 "" → "）
            var text = m.Groups[1].Value.Replace("\"\"", "\"");
            parts.Add(text);
          }
          else if (m.Groups[2].Success)
          {
            // 单元格引用（如 C2）→ ${FC2}
            var refText = m.Groups[2].Value;
            var refMatch2 = Regex.Match(refText, @"^([A-Z]+)(\d+)$");
            parts.Add("${F" + refMatch2.Groups[1].Value + refMatch2.Groups[2].Value + "}");
          }
          m = m.NextMatch();
        }

        // 拼接所有片段
        var result = string.Join("", parts);
        return result;
      }
      catch
      {
        return "";
      }
    }
  }
}
