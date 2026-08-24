using System;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Realso.Data.ORM.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO.Compression;
using System.IO;
using System.Drawing;

namespace Realso.Utils
{
  public class FileInfo
  {
    public FileInfo(string FileName, string FilePath)
    {
      this.FileName = FileName;
      this.FilePath = FilePath;
    }
    public string FileName { get; set; }
    public string FilePath { get; set; }
  }
  public class FileHelper
  {
    public static void Zip(IList<FileInfo> filesPath, string zipPath)
    {
      using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
      {
        for (int i = 0; i < filesPath.Count; i++)
        {
          archive.CreateEntryFromFile(filesPath[i].FilePath, filesPath[i].FileName, CompressionLevel.Fastest);
        }
      }
    }

    public static FileStream FromBitmap2FileStream(string fileName, Bitmap bitmap)
    {
      MemoryStream ms = null;
      try
      {
        ms = new MemoryStream();
        bitmap.Save(ms, bitmap.RawFormat);
        byte[] byteImage = new Byte[ms.Length];
        byteImage = ms.ToArray();
        FileStream fs = new FileStream(fileName, FileMode.Create);
        BinaryWriter bw = new BinaryWriter(fs);
        bw.Write(byteImage);
        bw.Close();
        return fs;
      }
      catch (ArgumentNullException ex)
      {
        throw ex;
      }
      finally
      {
        ms.Close();
      }
    }

    public static void SaveBitmap(string fileName, Bitmap bitmap){
      bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
    }
  }
}
