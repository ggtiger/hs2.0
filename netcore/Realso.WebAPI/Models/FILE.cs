using System.Collections;
using System;
using Realso.Core.Base;
using Realso.Data.ORM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Realso.Data.ORM.Core;

namespace Realso.WebAPI.Models
{
  public class FILE : BaseModel
  {
    public FILE(IViewOperate operate) : base(operate, "VSS_FILES")
    {
    }

    public string GetFilePath(string ID)
    {
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      this.OpenByID(ID);
      if (this.GetView().Count == 1)
      {
        // 兼容历史 Windows 生成的含 \ 路径：统一为正斜杠(.NET IO 双平台兼容)
        string rawPath = rootPath + (this.GetValue("FILEPATH") + "");
        string convertedPath = rawPath.Replace('\\', '/');
        // 本服务器历史文件以字面反斜杠为目录名存储，转换路径不存在时回退原始路径
        if (!System.IO.File.Exists(convertedPath) && System.IO.File.Exists(rawPath))
        {
          return rawPath;
        }
        return convertedPath;
      }
      else
      {
        return "";
      }
    }

    public async Task Save(Hashtable fileInfo)
    {
      //fileInfo[]
      //CREATER:
      string CREATER = fileInfo["CREATER"] + "";
      //FILENAME:
      string FILENAME = fileInfo["FILENAME"] + "";
      //UPLOADTYPE:
      string UPLOADTYPE = fileInfo["UPLOADTYPE"] + "";
      //UPLOADFILE:
      IFormFile UPLOADFILE = fileInfo["UPLOADFILE"] as IFormFile;
      //UPLOADFILEPATH:
      string UPLOADFILEPATH = fileInfo["UPLOADFILEPATH"] + "";
      if (UPLOADTYPE == "")
      {
        UPLOADTYPE = "其他";
      }
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:{UPLOADTYPE}:Path");
      string createType = Realso.Utils.ConfigHelper.GetConfig($"Upload:{UPLOADTYPE}:CreateType");
      DataView view = this.GetView();
      view.Clear();
      view.Inserted.Clear();
      view.Updated.Clear();
      view.Deleted.Clear();
      ViewRow row = new ViewRow(view);
      row["CREATER"] = fileInfo["NICKNAME"] + "";
      row["CREATEDATE"] = System.DateTime.Now;
      view.AddRow(row);
      this.FillKey();
      String FilePath = rPath;
      // 目录分隔统一用 /：Windows/Linux 的 .NET IO 均兼容正斜杠，
      // 避免 Linux 下 "其他\2026-08\" 被当成单个目录名
      if (createType == "YEAR")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy") + "/";
      }
      else if (createType == "MONTH")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy-MM") + "/";
      }
      else if (createType == "DAY")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy-MM-dd") + "/";
      }
      string FileName = Path.GetFileNameWithoutExtension(FILENAME) + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(FILENAME);
      DirectoryInfo di = new DirectoryInfo(rootPath + FilePath);
      if (!di.Exists)
      {
        di.Create();
      }
      row["FILEPATH"] = FilePath + FileName;
      row["FILENAME"] = FILENAME;
      if (UPLOADFILEPATH != "")
      {
        File.Move(UPLOADFILEPATH, rootPath + FilePath + FileName);
        using (FileStream stream = System.IO.File.Open(rootPath + FilePath + FileName, FileMode.Open))
        {
          row["FILESIZE"] = stream.Length;
        }
        if (Path.GetExtension(FileName) == ".docx")
        {
          Realso.Utils.MySocket.Send("127.0.0.1", 5555, rootPath + FilePath + FileName);
        }
        row["FILENAME"] = Path.GetFileName(FILENAME);
      }
      else
      {
        row["FILESIZE"] = UPLOADFILE.Length;
        using (FileStream file = System.IO.File.Create(rootPath + FilePath + FileName))
        {
          await UPLOADFILE.CopyToAsync(file);
        }
      }

    }

    public async Task SaveChunk(Hashtable fileInfo)
    {
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:临时:Path");
      string name = fileInfo["name"] + "";
      int chunk = int.Parse(fileInfo["chunk"] + "");
      int chunks = int.Parse(fileInfo["chunks"] + "");
      IFormFile UPLOADFILE = fileInfo["UPLOADFILE"] as IFormFile;
      string FILENAME = fileInfo["FILENAME"] + "";
      DirectoryInfo di = new DirectoryInfo(rootPath + rPath + name);
      if (!di.Exists)
      {
        di.Create();
      }
      using (FileStream file = System.IO.File.Create(rootPath + rPath + name + "/" + chunk + ".temp"))
      {
        await UPLOADFILE.CopyToAsync(file);
      }
      if (chunk == chunks - 1)
      {
        using (FileStream fs = new FileStream(rootPath + rPath + FILENAME, FileMode.Create))
        {//从临时文件夹，读取文件块，合并到同一个文件
          for (int i = 0; i < chunks; i++)
          {
            string file2 = rootPath + rPath + name + "/" + i + ".temp";
            var tempBytes = System.IO.File.ReadAllBytes(file2);
            fs.Write(tempBytes, 0, tempBytes.Length);
          }
          fs.Close();
        }
        Directory.Delete(rootPath + rPath + name, true);
        fileInfo["UPLOADFILEPATH"] = rootPath + rPath + FILENAME;
        await Save(fileInfo);
      }
    }

    public void SaveFile(Hashtable fileInfo)
    {
      //fileInfo[]
      //CREATER:
      string CREATER = fileInfo["CREATER"] + "";
      //FILENAME:
      string FILENAME = fileInfo["FILENAME"] + "";
      //UPLOADTYPE:
      string UPLOADTYPE = fileInfo["UPLOADTYPE"] + "";
      //UPLOADFILE:
      IFormFile UPLOADFILE = fileInfo["UPLOADFILE"] as IFormFile;
      //UPLOADFILEPATH:
      string UPLOADFILEPATH = fileInfo["UPLOADFILEPATH"] + "";
      if (UPLOADTYPE == "")
      {
        UPLOADTYPE = "其他";
      }
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:{UPLOADTYPE}:Path");
      string createType = Realso.Utils.ConfigHelper.GetConfig($"Upload:{UPLOADTYPE}:CreateType");
      DataView view = this.GetView();
      view.Clear();
      view.Inserted.Clear();
      view.Updated.Clear();
      view.Deleted.Clear();
      ViewRow row = new ViewRow(view);
      row["CREATER"] = fileInfo["NICKNAME"] + "";
      row["CREATEDATE"] = System.DateTime.Now;
      view.AddRow(row);
      this.FillKey();
      String FilePath = rPath;
      // 目录分隔统一用 /（同 Save 方法，跨平台兼容）
      if (createType == "YEAR")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy") + "/";
      }
      else if (createType == "MONTH")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy-MM") + "/";
      }
      else if (createType == "DAY")
      {
        FilePath += "/" + DateTime.Now.ToString("yyyy-MM-dd") + "/";
      }
      string FileName = Path.GetFileNameWithoutExtension(FILENAME) + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(FILENAME);
      DirectoryInfo di = new DirectoryInfo(rootPath + FilePath);
      if (!di.Exists)
      {
        di.Create();
      }
      row["FILEPATH"] = FilePath + FileName;
      row["FILENAME"] = FILENAME;
      if (UPLOADFILEPATH != "")
      {

        File.Move(UPLOADFILEPATH, rootPath + FilePath + FileName);
        using (FileStream stream = System.IO.File.Open(rootPath + FilePath + FileName, FileMode.Open))
        {
          row["FILESIZE"] = stream.Length;
        }
        if (Path.GetExtension(FileName) == ".docx")
        {
          // Realso.Utils.MySocket.Send("127.0.0.1", 5555, rootPath + FilePath + FileName);
          if(fileInfo["SYNC"]+""=="1"){
             SendAsync(rootPath + FilePath + FileName);
          }else{
            Realso.Utils.MySocket.Send("127.0.0.1", 5555, rootPath + FilePath + FileName);
          }
        }
        row["FILENAME"] = Path.GetFileName(FILENAME);
      }
      else
      {
        row["FILESIZE"] = UPLOADFILE.Length;
        using (FileStream file = System.IO.File.Create(rootPath + FilePath + FileName))
        {
          UPLOADFILE.CopyTo(file);
        }
      }
    }

    public async Task SendAsync(String path){
        await Task.Run(()=>{
             Realso.Utils.MySocket.Send("127.0.0.1", 5555,path);
        });
    }
  }
}
