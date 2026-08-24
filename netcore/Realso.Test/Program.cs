using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Data;
using System.Collections.Generic;
using System.Collections;
using System;
using Dapper;
using System.Data.OracleClient;
using Realso.Data.ORM;
using System.Linq;
using System.IO;
using Realso.Utils;
using EasyOffice;
using EasyOffice.Extensions;
using EasyOffice.Extensions.Interfaces;
using Aspose.Words;
using Spire.Doc;
using Spire.Doc.Documents;
using System.IO.Compression;
namespace Realso.Test
{
  class DEPT
  {
    string DEPTNAME { get; set; }
  }
  class Program
  {
    static void Main(string[] args)
    {
      // Finds(document, copyDoc);
      // Convert();
      // Convert2();
       Convert3();
      // Convert4();
      // DynamicParameters parameters = new DynamicParameters();//建立一个parem对象
      // parameters.Add("@TCODE", "AC|%Y%m|3");
      // parameters.Add("@OCODE", "", System.Data.DbType.String, System.Data.ParameterDirection.Output);
      // Realso.Data.DBAccess.DB.GetDBHelper().Execute("call PSS_GENCODE(@TCODE,@OCODE);", parameters, null, null, CommandType.StoredProcedure);
      // string document = @"D:\1.docx";
      // string copyDoc = document + DateTime.Now.ToString("yyyyMMddHHmmss") + ".docx";
    }


    private static string getQRFilePath(int size,string id){
      string rPath = Realso.Utils.ConfigHelper.GetConfig($"Url:验证二维码");
      string rootPath = Realso.Utils.ConfigHelper.GetConfig($"Upload:ROOT");
      string filePath =  Realso.Utils.ConfigHelper.GetConfig($"Upload:临时:Path");
      string fileName= "二维码"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".png" ;
      DirectoryInfo di = new DirectoryInfo(rootPath + filePath);
      if (!di.Exists)
      {
        di.Create();
      }
      QRHelper.SaveQR(rootPath+filePath+fileName, size, rPath+"&id="+id);
      return rootPath+filePath+fileName;
    }

    public static void
    Convert4()
    {
      MySocket.Send("127.0.0.1", 5555, @"D:\realso\文档迁移点\韩总\原始记录采集报价系统\证书2(1).docx");
    }
    public static void
    Convert3()
    {
      Spire.Doc.Document document = new Spire.Doc.Document();
      document.LoadFromFile(@"C:\Users\ggtiger\Desktop\202403\13.docx");

      //Convert Word to PDF
      document.SaveToFile(@"C:\Users\ggtiger\Desktop\202403\13.pdf", FileFormat.PDF);
    }
    public static void Convert2()
    {
      Aspose.Words.Document doc = new Aspose.Words.Document(@"D:\realso\文档迁移点\韩总\原始记录采集报价系统\证书2(1).docx");
      bool isOk = false;
      if (doc != null)
      {
        doc.Save(@"D:\realso\文档迁移点\韩总\原始记录采集报价系统\证书2(1)-002.pdf", SaveFormat.Pdf);
      }
    }
    public class WordCarTemplateDTO
    {
      //默认占位符为{PropertyName}
      public string OwnerName { get; set; }
    }
    public static async Task Convert()
    {
      try
      {


        EasyOffice.Extensions.Interfaces.IWordConverter converter = new EasyOffice.Extension.Converter.Converters.WordConverter();
        //IWordExportProvider
        EasyOffice.Services.WordExportService wordService = new EasyOffice.Services.WordExportService(new EasyOffice.Providers.NPOI.WordExportProvider());
        var word = await wordService.CreateFromTemplateAsync(@"C:\Users\ggtiger\Desktop\证书模版.docx", new WordCarTemplateDTO() { OwnerName = "abc" });
        var pdfBytes = converter.ConvertToPDF(word.WordBytes, "text");
        File.WriteAllBytes(@"D:\realso\文档迁移点\韩总\原始记录采集报价系统\证书2(1)-003.pdf", pdfBytes);
      }
      catch (Exception ex)
      {
        ex = ex;
      }
    }
    /*
        public static void Finds(string document, string copyDoc)
        {

          WordprocessingDocument doc =

           WordprocessingDocument.CreateFromTemplate(document);

          using (WordprocessingDocument wordprocessingDocument =
              WordprocessingDocument.Open(document, true))
          {
            WordprocessingDocument knwe = (WordprocessingDocument)wordprocessingDocument.SaveAs(copyDoc);
            using (knwe)
            {

              MainDocumentPart mainPart = knwe.MainDocumentPart;
              List<Paragraph> allBookmarkStart = mainPart.RootElement.Descendants<Paragraph>().ToList();
              foreach (var element in allBookmarkStart)
              {
                Console.WriteLine(element.InnerText);
              }
            }
          }

          using (WordprocessingDocument wordprocessingDocument = WordHelper.CopyWord(document, copyDoc))
          {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("${检定机构}", "<span style=\"letter-spacing:0px; font-size:50pt;\"></span>（650~1500）kg/m<sup>3 </sup><em>U</em>=0.08kg/m<sup>3</sup> (<em>k</em>=2)；（1500~2000）kg/m<sup>3 </sup><em>U</em>=0.20kg/m<sup>3</sup> (<em>k</em>=2)<span style=\"letter-spacing: 0px; font-size: 11px;\"></span>");
            dic.Add("${证书编号}", "E<sub>2</sub>T201-2-0001");
            dic.Add("${计量检定机构授权证书号}", "E<sub>2</sub>（国）法计（2017）01023号");
            dic.Add("${送检单位}", "E<sub>2</sub>等级");
            Dictionary<string, string> pic = new Dictionary<string, string>();
            pic.Add("${批准人}", @"C:\Users\ggtiger\Pictures\图片2.png");
            pic.Add("${核验员}", @"C:\Users\ggtiger\Pictures\图片3.png");
            pic.Add("${检定员}", @"C:\Users\ggtiger\Pictures\图片4.png");
            MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;
            List<Paragraph> allBookmarkStart = mainPart.RootElement.Descendants<Paragraph>().ToList();
            foreach (var ele in allBookmarkStart)
            {
              var rn = ele.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().FirstOrDefault(g => dic.ContainsKey(g.InnerText.Trim()));
              if (rn != null)
              {
                var text = rn?.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().FirstOrDefault(g => dic.ContainsKey(g.InnerText.Trim()));
                text.Parent.Parent.AppendChild(WordHelper.GetEditorChunk(wordprocessingDocument, dic[text.InnerText]));
                break;
              }
              else
              {
                if (dic.ContainsKey(ele.InnerText.Trim()))
                {
                  var list = ele.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().ToList();
                  foreach (var aa in list)
                  {
                    if (list.IndexOf(aa) > 0)
                    {
                      aa.Parent.RemoveChild(aa);
                    }
                    else
                    {
                      var text = aa?.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().FirstOrDefault();
                      text.Text = dic[ele.InnerText];
                    }
                  }
                }
              }
              var rn2 = ele.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().FirstOrDefault(g => pic.ContainsKey(g.InnerText.Trim()));
              if (rn2 != null)
              {
                var text = rn2?.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().FirstOrDefault(g => pic.ContainsKey(g.InnerText.Trim()));
                rn2.Append(WordHelper.GetImageDrawing(wordprocessingDocument, pic[text.InnerText]));
                text.Text = "";
              }
              else
              {
                if (pic.ContainsKey(ele.InnerText.Trim()))
                {
                  var list = ele.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>().ToList();
                  foreach (var aa in list)
                  {
                    if (list.IndexOf(aa) > 0)
                    {
                      aa.Parent.RemoveChild(aa);
                    }
                    else
                    {
                      var text = aa?.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>().FirstOrDefault();
                      aa.Append(WordHelper.GetImageDrawing(wordprocessingDocument, pic[text.InnerText]));
                      text.Text = "";
                    }
                  }
                }
              }
            }
              }
}
            */

  }
}
