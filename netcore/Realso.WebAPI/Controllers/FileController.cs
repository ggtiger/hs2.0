using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Core.Base;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Realso.WebAPI.Models;
using Realso.Data.ORM.Core;
using Realso.Data.DBAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Realso.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.WebAPI.Controllers
{
  [Route("api/[controller]")]
  //[ApiController]
  public class FileController : BaseControl
  {
    private readonly IHostingEnvironment _hostingEnvironment;

    public FileController(IHostingEnvironment hostingEnvironment)
    {
      this._hostingEnvironment = hostingEnvironment;
    }

    [HttpPost]
    [EnableCors("AllowHeaders")]
   public async Task<IActionResult> FileSave()
    {
      var data = Request;
      var files = Request.Form.Files;
      var uploadType = data.Form["uploadType"];
      var id = "";
      long size = 0;
      int chunks = int.Parse(data.Form["chunks"] + "");
      int chunk = int.Parse(data.Form["chunk"] + "");
      foreach (var formFile in files)
      {
        if (formFile.Length > 0)
        {
          FILE fileModel = new FILE(this.operate01);
          DataView logView = fileModel.GetView();
          Hashtable fileInfo = new Hashtable();
          fileInfo["CREATER"] = this.userInfo["NICKNAME"] + "";
          fileInfo["UPLOADTYPE"] = data.Form["uploadType"];
          fileInfo["FILENAME"] = formFile.FileName;
          if (data.Form["fileName"] + "" != "")
          {
            fileInfo["FILENAME"] = data.Form["fileName"];
          }
          fileInfo["UPLOADFILE"] = formFile;
          if (chunks > 1)
          {
            fileInfo["name"] = data.Form["name"];
            fileInfo["chunks"] = data.Form["chunks"];
            fileInfo["chunk"] = data.Form["chunk"];
            await fileModel.SaveChunk(fileInfo);
            if (chunk != chunks - 1)
            {
              return Ok(new { });
            }
          }
          else
          {
            await fileModel.Save(fileInfo);
          }
          this.operate01.FillKey(logView);
          ArrayList saveList2 = new ArrayList();
          saveList2.Add(logView);
          this.operate01.Save(saveList2);
          id += "," + fileModel.GetValue("ID");
          size += formFile.Length;
        }
      }
      return Ok(new { count = files.Count, size, id = id.Substring(1) });
    }

    [HttpGet("{id}")]
    [EnableCors("AllowHeaders")]
    public IActionResult DownLoad(string Id, string pdf, string token)
    {
      Hashtable Params = new Hashtable();
      Params["FILTERCODE"] = "F00";
      Hashtable FilterParams = new Hashtable();
      FilterParams["ID"] = Id;
      Params["FilterParams"] = FilterParams;
      BaseModel MAIN = GetModel("", "VSS_FILES");
      MAIN.Open(GetQueryInfo(Params));
      string webRootPath = _hostingEnvironment.WebRootPath;
      string contentRootPath = _hostingEnvironment.ContentRootPath;
      if (MAIN.GetView().Count > 0)
      {
        ViewRow row = MAIN.GetView()[0];

        // Token校验：如果该文件是密码保护的电子证书PDF，必须带有效token
        if (!string.IsNullOrEmpty(token))
        {
          if (!PasswordHelper.VerifyAccessToken(Id, token))
          {
            return StatusCode(403, new { Message = "访问Token无效或已过期" });
          }
        }
        else
        {
          // 无token时检查是否为密码保护的电子证书
          if (IsEcertWithPassword(Id))
          {
            return StatusCode(403, new { Message = "该证书需要密码才能访问" });
          }
        }
        string FILENAME = row.GetString("FILENAME");

        string fileExt = Path.GetExtension(FILENAME);
        string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string FilePath = rootPath + row.GetString("FILEPATH");
        if(row.GetString("FILEPATH").IndexOf("http")==0){
          return Redirect(row.GetString("FILEPATH"));
        }
        var stream = System.IO.File.OpenRead(FilePath);
        //获取文件的ContentType
        var provider = new FileExtensionContentTypeProvider();
        var memi = provider.Mappings[fileExt];

        if (fileExt == ".pdf")
        {
          return new PhysicalFileResult(FilePath, "application/pdf");
        }
        else if (fileExt == ".jpg")
        {
          return new PhysicalFileResult(FilePath, "image/jpeg");
        }else if (fileExt == ".png")
        {
          return new PhysicalFileResult(FilePath, "image/png");
        }
        else
        {
          return File(stream, memi, FILENAME);
        }
      }
      return null;
    }

    [HttpGet("pdf/{id}")]
    [EnableCors("AllowHeaders")]
    public IActionResult DownLoadPdf(string Id)
    {
      Hashtable Params = new Hashtable();
      Params["FILTERCODE"] = "F00";
      Hashtable FilterParams = new Hashtable();
      FilterParams["ID"] = Id;
      Params["FilterParams"] = FilterParams;
      BaseModel MAIN = GetModel("", "VSS_FILES");
      MAIN.Open(GetQueryInfo(Params));
      if (MAIN.GetView().Count > 0)
      {
        ViewRow row = MAIN.GetView()[0];
        string FILENAME = row.GetString("FILENAME");
        string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string FilePath = rootPath + row.GetString("FILEPATH");
        string pdfPath = FilePath.Replace(".docx", ".pdf");

        // 优先使用已缓存的PDF
        if (System.IO.File.Exists(pdfPath))
        {
          return new PhysicalFileResult(pdfPath, "application/pdf");
        }

        // PDF不存在，触发转换
        if (System.IO.File.Exists(FilePath) && Path.GetExtension(FILENAME) == ".docx")
        {
          // 使用 OnlyOffice Document Server 转换，传入 fileId 构建 HTTP URL
          OnlyOfficeConverter.ConvertToPdf(FilePath, pdfPath, Id);
        }

        if (System.IO.File.Exists(pdfPath))
        {
          return new PhysicalFileResult(pdfPath, "application/pdf");
        }

        return NotFound(new { Message = "PDF文件生成中，请稍后重试" });
      }
      return NotFound();
    }

    [HttpGet("test/{id}")]
    public IActionResult test(string Id)
    {
        return Ok(Id);
    }

    [HttpGet("pdfsy/{id}")]
    [EnableCors("AllowHeaders")]
    public IActionResult DownLoadSyPdf(string Id, string token)
    {
      // Token校验：如果该文件是密码保护的电子证书PDF，必须带有效token
      if (!string.IsNullOrEmpty(token))
      {
        if (!PasswordHelper.VerifyAccessToken(Id, token))
        {
          return StatusCode(403, new { Message = "访问Token无效或已过期" });
        }
      }
      else
      {
        if (IsEcertWithPassword(Id))
        {
          return StatusCode(403, new { Message = "该证书需要密码才能访问" });
        }
      }

      Hashtable Params = new Hashtable();
      Params["FILTERCODE"] = "F00";
      Hashtable FilterParams = new Hashtable();
      FilterParams["ID"] = Id;
      Params["FilterParams"] = FilterParams;
      BaseModel MAIN = GetModel("", "VSS_FILES");
      MAIN.Open(GetQueryInfo(Params));
      string webRootPath = _hostingEnvironment.WebRootPath;
      string contentRootPath = _hostingEnvironment.ContentRootPath;
      try{
      if (MAIN.GetView().Count > 0)
      {
        ViewRow row = MAIN.GetView()[0];
        string FILENAME = row.GetString("FILENAME");

        string fileExt = Path.GetExtension(FILENAME);
        string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
        string FilePath = rootPath + row.GetString("FILEPATH");
        Logger.Info(FilePath);
        if(!System.IO.File.Exists(FilePath.Replace(".docx", ".pdf"))){
            OnlyOfficeConverter.ConvertToPdf(FilePath, FilePath.Replace(".docx", ".pdf"), Id);
        }
        if(!System.IO.File.Exists(FilePath.Replace(".docx", "-sy.pdf"))){
            Realso.Utils.MySocket.Send("127.0.0.1", 5555, FilePath.Replace(".docx", ".pdf"));
        }
        if(System.IO.File.Exists(FilePath.Replace(".docx", "-sy.pdf"))){
            FilePath = FilePath.Replace(".docx", "-sy.pdf");
        }else{
            FilePath = FilePath.Replace(".docx", ".pdf");
        }
        return new PhysicalFileResult(FilePath, "application/pdf");
      }
      }catch(Exception ex){
        Logger.Info(ex.StackTrace+ex.Message);
      }

      return null;
    }

    /// <summary>
    /// 获取 OnlyOffice Document Editor 配置（只读预览模式）
    /// GET /api/file/editor-config/{id}
    /// </summary>
    [HttpGet("editor-config/{id}")]
    [EnableCors("AllowHeaders")]
    public IActionResult GetEditorConfig(string id)
    {
      try
      {
        // 查询文件信息
        Hashtable Params = new Hashtable();
        Params["FILTERCODE"] = "F00";
        Hashtable FilterParams = new Hashtable();
        FilterParams["ID"] = id;
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
        string FilePath = rootPath + row.GetString("FILEPATH");

        if (!System.IO.File.Exists(FilePath))
        {
          return NotFound(new { Message = "文件不存在于磁盘" });
        }

        string fileExt = Path.GetExtension(FILENAME).TrimStart('.');
        string docKey = id + "_" + row.GetString("VER") + "_" + System.IO.File.GetLastWriteTime(FilePath).Ticks;

        // OnlyOffice Document Server 地址
        string docServerUrl = ConfigHelper.GetConfig("OnlyOffice:DocServerUrl") ?? "http://localhost:8088";

        // 构建文件 URL（OnlyOffice Document Server 需要能访问此 URL 下载文件）
        // 使用 HTTP URL，让 Document Server 通过后端接口下载文件
        string apiUrl = ConfigHelper.GetConfig("OnlyOffice:ApiUrl") ?? "http://127.0.0.1:5001";
        string fileUrl = apiUrl + "/api/file/" + id;

        // 判断文档类型
        string documentType = "word";
        string[] cellExts = { "xls", "xlsx", "xlsm", "xlt", "xltx", "csv", "ods" };
        string[] slideExts = { "ppt", "pptx", "pptm", "pot", "potx", "odp" };
        if (Array.IndexOf(cellExts, fileExt) >= 0)
        {
          documentType = "cell";
        }
        else if (Array.IndexOf(slideExts, fileExt) >= 0)
        {
          documentType = "slide";
        }

        // 构建编辑器配置
        var config = new
        {
          document = new
          {
            fileType = fileExt,
            key = docKey,
            title = FILENAME,
            url = fileUrl,
            permissions = new
            {
              edit = false,
              download = true,
              print = true
            }
          },
          documentType,
          editorConfig = new
          {
            mode = "view",
            lang = "zh-CN",
            user = new
            {
              id = this.userInfo != null ? this.userInfo["ID"] + "" : "guest",
              name = this.userInfo != null ? this.userInfo["NICKNAME"] + "" : "访客"
            },
            customization = new
            {
              autosave = false,
              chat = false,
              comments = false,
              compactHeader = true,
              compactToolbar = true,
              feedback = false,
              forcesave = false,
              help = false,
              hideRightMenu = true,
              hideRulers = true,
              reviewDisplay = "markup",
              showReviewChanges = false,
              spellcheck = false,
              toolbarNoTabs = true,
              unit = "cm",
              zoom = 100
            }
          }
        };

        return Ok(config);
      }
      catch (Exception ex)
      {
        Logger.Info($"GetEditorConfig 异常: {ex.Message}\n{ex.StackTrace}");
        return StatusCode(500, new { Message = "获取编辑器配置失败" });
      }
    }

    /// <summary>
    /// 检查文件ID是否为设置了密码的电子证书
    /// 通过查询 tck_orecord 表中 CERTID=文件ID 且 ECERTPWD 不为空的记录
    /// </summary>
    private bool IsEcertWithPassword(string fileId)
    {
      if (string.IsNullOrEmpty(fileId)) return false;
      try
      {
        var dbHelper = DB.GetDBHelper();
        string sql = "SELECT COUNT(1) FROM tck_orecord WHERE CERTID=@FILEID AND ECERTPWD IS NOT NULL AND ECERTPWD<>'' AND ECERTSIGN=1";
        var param = new { FILEID = fileId };
        int count = Convert.ToInt32(dbHelper.ExecuteScalar(sql, param));
        return count > 0;
      }
      catch (Exception ex)
      {
        Logger.Info($"IsEcertWithPassword 异常: {ex.Message}");
        return false;
      }
    }
  }
}
