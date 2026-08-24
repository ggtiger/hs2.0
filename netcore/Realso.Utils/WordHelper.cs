using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
namespace Realso.Utils
{
  public class WordHelper
  {
    //获取word
    public static WordprocessingDocument GetWord(string wordSrc)
    {
      return WordprocessingDocument.CreateFromTemplate(wordSrc);
    }
    //复制word
    public static WordprocessingDocument CopyWord(string fromSrc, string toSrc)
    {
      WordprocessingDocument fromDoc = WordprocessingDocument.CreateFromTemplate(fromSrc);
      using (WordprocessingDocument wordprocessingDocument =
          WordprocessingDocument.Open(fromSrc, true))
      {
        WordprocessingDocument toDoc = (WordprocessingDocument)wordprocessingDocument.SaveAs(toSrc);
        return toDoc;
      }
    }
    //创建图片对象
    public static Drawing GetImageDrawing(WordprocessingDocument doc, string imgSrc)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;
      ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
      using (FileStream stream = new FileStream(imgSrc, FileMode.Open))
      {
        imagePart.FeedData(stream);
      }
      return WordHelper.getPic(mainPart.GetIdOfPart(imagePart));
    }

    //创建图片对象
    public static Drawing GetImage2Drawing(WordprocessingDocument doc, string imgSrc)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;
      ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
      using (FileStream stream = new FileStream(imgSrc, FileMode.Open))
      {
        imagePart.FeedData(stream);
      }
      return WordHelper.getPic(mainPart.GetIdOfPart(imagePart),714400L,714400L);
    }

    //创建富文本对象
    public static AltChunk GetEditorChunk(WordprocessingDocument doc, string html)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;
      ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
      string altChunkId = "N" + Guid.NewGuid().ToString("N");
      string htmlEncodedString = "<html><head></head><body><font style='FONT-SIZE: 10.5pt'>" + html.Replace("<em>","<i>").Replace("</em>","</i>") + "</font></body></html>";
      MemoryStream ms = new MemoryStream(new System.Text.UTF8Encoding(true).GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(htmlEncodedString)).ToArray());
      //MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlEncodedString));
      AlternativeFormatImportPart formatImportPart = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Html, altChunkId);
      formatImportPart.FeedData(ms);
      AltChunk altChunk = new AltChunk();
      altChunk.Id = altChunkId;
      return altChunk;
    }
    //table复制一行

    //替换模版对象（兼容 Bookmark 和 Content Control）
    public static void ReplaceFromTemplate(WordprocessingDocument doc, Dictionary<string, Object> markInfo)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;
      var headers = mainPart.HeaderParts.ToList();
      using (doc)
      {
        // 1. 优先处理 Content Control (SDT)
        replaceFromContentControl(doc, markInfo);

        // 2. 兼容处理 Bookmark（旧模版）
        var bookmarks = from bm in mainPart.Document.Body.Descendants<BookmarkStart>()
                        select bm;
        replaceFromBookMark(doc, bookmarks, markInfo, false);
        foreach (var header in headers)
        {
          var bks = from bmm in header.Header.Descendants<BookmarkStart>()
                    select bmm;
          replaceFromBookMark(doc, bks, markInfo, false);
        }
      }
    }

    #region Content Control (SDT) 替换

    //获取 SDT 的 Tag 值（v2.9.1 兼容）
    private static string GetSdtTag(SdtElement sdt)
    {
      var tagElement = sdt.SdtProperties?.GetFirstChild<Tag>();
      return tagElement?.Val?.Value;
    }

    //获取 SDT 的 Title/Alias 值（v2.9.1 兼容）
    private static string GetSdtTitle(SdtElement sdt)
    {
      var aliasElement = sdt.SdtProperties?.GetFirstChild<SdtAlias>();
      return aliasElement?.Val?.Value;
    }

    //获取 SDT 的内容元素（v2.9.1 兼容：SdtElement 基类没有内容属性，需转型）
    private static OpenXmlElement GetSdtContent(SdtElement sdt)
    {
      if (sdt is SdtBlock block) return block.SdtContentBlock;
      if (sdt is SdtRun run) return run.SdtContentRun;
      if (sdt is SdtCell cell) return cell.SdtContentCell;
      if (sdt is SdtRow row) return row.SdtContentRow;
      return null;
    }

    /// <summary>
    /// 移除 SDT 的占位文本标记（w:showingPlcHdr + w:placeholder），
    /// 确保替换内容后 Word/OnlyOffice 不再显示占位文本
    /// </summary>
    private static void RemovePlcHdr(SdtElement sdt)
    {
      var sdtPr = sdt.GetFirstChild<SdtProperties>();
      if (sdtPr == null) return;
      var toRemove = sdtPr.Elements()
        .Where(e => e.LocalName == "showingPlcHdr" || e.LocalName == "placeholder")
        .ToList();
      foreach (var elem in toRemove)
      {
        elem.Remove();
      }
    }

    //从 Content Control 替换字段
    private static void replaceFromContentControl(WordprocessingDocument doc, Dictionary<string, Object> markInfo)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;

      // 处理 Body 中的 SDT
      var sdts = mainPart.Document.Body.Descendants<SdtElement>().ToList();
      processSdtElements(doc, sdts, markInfo, false);

      // 处理 Header 中的 SDT
      foreach (var header in mainPart.HeaderParts)
      {
        var headerSdts = header.Header.Descendants<SdtElement>().ToList();
        processSdtElements(doc, headerSdts, markInfo, false);
      }

      // 扁平化已替换的 SDT：将内容提取到父元素中，移除 SDT 外壳
      // 需要重新获取 SDT 列表（表格行克隆可能产生新的 SDT）
      FlattenSdts(mainPart.Document.Body);
      foreach (var header in mainPart.HeaderParts)
      {
        FlattenSdts(header.Header);
      }
    }

    /// <summary>
    /// 扁平化 SDT：将非表格类型的 SDT 内容提取到父元素中，移除 SDT 外壳。
    /// 这样替换后的文档不会显示 Content Control 灰色标签。
    /// </summary>
    private static void FlattenSdts(OpenXmlElement root)
    {
      // 反向遍历（从内到外），避免嵌套 SDT 处理顺序问题
      var allSdts = root.Descendants<SdtElement>().Reverse().ToList();
      foreach (var sdt in allSdts)
      {
        string tag = GetSdtTag(sdt);
        if (string.IsNullOrEmpty(tag)) continue;

        string suffix = GetSuffix(tag);

        // _TABLE 类型的 SDT 保留外壳（重复节需要保留结构）
        if (suffix == "TABLE") continue;

        var content = GetSdtContent(sdt);
        if (content == null) continue;

        var parent = sdt.Parent;
        if (parent == null) continue;

        // 将 SDT 内容的子元素克隆后插入到 SDT 的父元素中（在 SDT 之前）
        // 必须克隆，因为元素仍属于 SDT 内容树，不能直接插入到另一棵树
        var contentChildren = content.Elements().ToList();
        foreach (var child in contentChildren)
        {
          sdt.InsertBeforeSelf(child.CloneNode(true));
        }

        // 移除 SDT 外壳
        sdt.Remove();
      }
    }

    private static void processSdtElements(WordprocessingDocument doc, List<SdtElement> sdts, Dictionary<string, Object> markInfo, bool inTable)
    {
      foreach (var sdt in sdts)
      {
        string fullTag = GetSdtTag(sdt);
        if (string.IsNullOrEmpty(fullTag)) continue;

        // 统一移除 SDT 的占位文本标记，确保替换后不显示占位文本
        RemovePlcHdr(sdt);

        string baseName = GetBaseName(fullTag);
        string suffix = GetSuffix(fullTag);

        // 处理表格类型（重复节）
        if (suffix == "TABLE")
        {
          if (!inTable)
          {
            // 顶层 _TABLE SDT：克隆行
            if (markInfo.ContainsKey(baseName))
            {
              addTableToContentControl(doc, sdt, markInfo[baseName] as List<Dictionary<string, Object>>);
            }
          }
          else
          {
            // 克隆行内的 _TABLE 标记 SDT：清空占位文本（避免显示 List 类型名）
            addTextToContentControl(sdt, "");
          }
          continue;
        }

        // 图片字段：用完整 Tag 查找图片路径（CREATER_IMG），不依赖 baseName 文本字段
        if (suffix == "IMG" || suffix == "IMG2")
        {
          string imgPath = "";
          if (markInfo.ContainsKey(fullTag))
          {
            imgPath = markInfo[fullTag] + "";
          }
          else if (markInfo.ContainsKey(baseName))
          {
            imgPath = markInfo[baseName] + "";
          }
          if (!string.IsNullOrEmpty(imgPath))
          {
            if (suffix == "IMG2")
              addImg2ToContentControl(doc, sdt, imgPath);
            else
              addImgToContentControl(doc, sdt, imgPath);
          }
          continue;
        }

        if (!markInfo.ContainsKey(baseName))
        {
          // 数据中没有提供该字段的值，清空占位文本显示为空
          addTextToContentControl(sdt, "");
          continue;
        }

        string value = markInfo[baseName] + "";
        if (value == "")
        {
          addTextToContentControl(sdt, "");
          continue;
        }

        if (suffix == "")
        {
          addTextToContentControl(sdt, value);
        }
        else if (suffix == "YY")
        {
          if (value.Split('-').Length == 3)
            addTextToContentControl(sdt, value.Split('-')[0]);
        }
        else if (suffix == "MM")
        {
          if (value.Split('-').Length == 3)
            addTextToContentControl(sdt, value.Split('-')[1]);
        }
        else if (suffix == "DD")
        {
          if (value.Split('-').Length == 3)
            addTextToContentControl(sdt, value.Split('-')[2]);
        }
        else if (suffix == "HTML")
        {
          addHtmlToContentControl(doc, sdt, value);
        }
        else
        {
          addTextToContentControl(sdt, value);
        }
      }
    }

    //SDT 文本替换（v2.9.1 兼容）
    private static void addTextToContentControl(SdtElement sdt, string v)
    {
      var content = GetSdtContent(sdt);
      if (content == null) return;

      // 从 SDT 内容的第一个 Run 提取样式（字体、字号、颜色等）
      // 插件插入 SDT 时已通过 SetTextPr 将光标样式应用到 SDT，因此此处直接读取即可
      RunProperties styleProps = null;
      var firstRun = content.Descendants<Run>().FirstOrDefault();
      if (firstRun?.RunProperties != null)
      {
        styleProps = firstRun.RunProperties.CloneNode(true) as RunProperties;
      }

      // 清空段落/内容中所有 Run，避免残留占位文本
      var oldRuns = content.Descendants<Run>().ToList();
      foreach (var r in oldRuns)
      {
        r.Remove();
      }

      // 写入新 Run，继承提取的样式
      // 空字符串不渲染，Word 会回退显示占位文本，所以用零宽空格(​)替代
      var textValue = string.IsNullOrEmpty(v) ? "​" : v;
      var newRun = new Run(new Text(textValue) { Space = SpaceProcessingModeValues.Preserve });
      if (styleProps != null)
      {
        newRun.RunProperties = styleProps;
      }

      var paragraph = content.GetFirstChild<Paragraph>();
      if (paragraph != null)
      {
        paragraph.Append(newRun);
      }
      else
      {
        content.Append(newRun);
      }
    }

    //SDT 图片替换（v2.9.1 兼容）
    private static void addImgToContentControl(WordprocessingDocument doc, SdtElement sdt, string imgPath)
    {
      addImageToContentControl(doc, sdt, imgPath, false);
    }

    //SDT 图片替换-固定尺寸（v2.9.1 兼容）
    private static void addImg2ToContentControl(WordprocessingDocument doc, SdtElement sdt, string imgPath)
    {
      addImageToContentControl(doc, sdt, imgPath, true);
    }

    /// <summary>
    /// 图片插入到 SDT：区分 Block / Inline。
    /// Inline SDT（图片控件默认，SdtRun）的 SdtContentRun 只能含 Run，不能含块级 Paragraph。
    /// </summary>
    private static void addImageToContentControl(WordprocessingDocument doc, SdtElement sdt, string imgPath, bool fixedSize)
    {
      try
      {
        var content = GetSdtContent(sdt);
        if (content == null) return;

        var drawing = fixedSize ? WordHelper.GetImage2Drawing(doc, imgPath) : WordHelper.GetImageDrawing(doc, imgPath);

        if (sdt is SdtBlock)
        {
          // Block SDT：图片放 Paragraph
          var paragraph = content.GetFirstChild<Paragraph>();
          if (paragraph == null)
          {
            paragraph = new Paragraph();
            content.Append(paragraph);
          }
          var oldRuns = paragraph.Elements<Run>().ToList();
          foreach (var r in oldRuns) r.Remove();
          var run = new Run(new RunProperties());
          run.Append(drawing);
          paragraph.Append(run);
        }
        else
        {
          // Inline SDT（SdtRun）：图片直接放 Run，不加 Paragraph（SdtContentRun 只能含 Run）
          var oldRuns = content.Elements<Run>().ToList();
          foreach (var r in oldRuns) r.Remove();
          var run = new Run(new RunProperties());
          run.Append(drawing);
          content.Append(run);
        }
      }
      catch (Exception ex) { }
    }

    //SDT HTML 富文本替换（v2.9.1 兼容）
    private static void addHtmlToContentControl(WordprocessingDocument doc, SdtElement sdt, string html)
    {
      var content = GetSdtContent(sdt);
      if (content == null) return;

      // 清空现有内容
      var paragraphs = content.Elements<Paragraph>().ToList();
      foreach (var p in paragraphs)
      {
        p.Remove();
      }

      // 插入 HTML 块
      content.Append(WordHelper.GetEditorChunk(doc, html));
    }

    //SDT 表格行替换（重复节，v2.9.1 兼容）
    private static void addTableToContentControl(WordprocessingDocument doc, SdtElement sdt, List<Dictionary<string, Object>> rows)
    {
      if (rows == null || rows.Count == 0) return;

      // 查找表格行：
      // 1. 优先 SDT 内部（SDT 包裹整行的情况）
      // 2. 回退向上找包含 SDT 的行（OnlyOffice 的 Block SDT 实际在单元格内，是段落级）
      var tableRow = sdt.Descendants<TableRow>().FirstOrDefault();
      if (tableRow == null)
      {
        tableRow = sdt.Ancestors<TableRow>().FirstOrDefault();
      }
      if (tableRow == null || tableRow.Parent == null) return;

      TableRow templateRow = tableRow;
      TableRow lastRow = templateRow;

      foreach (var rowData in rows)
      {
        TableRow newRow = (TableRow)templateRow.CloneNode(true);
        lastRow.InsertAfterSelf(newRow);
        lastRow = newRow;

        // 替换新行中的 SDT 字段
        var rowSdts = newRow.Descendants<SdtElement>().ToList();
        processSdtElements(doc, rowSdts, rowData, true);

        // 兼容：替换新行中的 Bookmark
        var rowBookmarks = newRow.Descendants<BookmarkStart>();
        replaceFromBookMark(doc, rowBookmarks, rowData, true);
      }

      // 移除模版行
      templateRow.Parent.RemoveChild(templateRow);
    }

    //解析 Tag 获取基础名（去掉类型后缀）
    private static string GetBaseName(string tag)
    {
      string[] suffixes = { "_YY", "_MM", "_DD", "_IMG", "_IMG2", "_HTML", "_TABLE" };
      foreach (var suffix in suffixes)
      {
        if (tag.EndsWith(suffix))
        {
          return tag.Substring(0, tag.Length - suffix.Length);
        }
      }
      return tag;
    }

    //解析 Tag 获取类型后缀
    private static string GetSuffix(string tag)
    {
      string[] suffixes = { "_YY", "_MM", "_DD", "_IMG", "_IMG2", "_HTML", "_TABLE" };
      foreach (var suffix in suffixes)
      {
        if (tag.EndsWith(suffix))
        {
          return suffix.Substring(1); // 去掉前面的下划线
        }
      }
      return "";
    }

    #endregion

    private static void replaceFromBookMark(WordprocessingDocument doc, IEnumerable<BookmarkStart> bookmarks, Dictionary<string, Object> markInfo, bool inTable)
    {
      BookmarkStart[] abookmarks = bookmarks.ToArray();
      foreach (var bookmark in abookmarks)
      {
        string[] ainfo = bookmark.Name.ToString().Split('_');
        string name = ainfo[0];
        if (markInfo.ContainsKey(name))
        {
          string value = markInfo[name] + "";
          if (value == "")
          {
            addTextToBookMark(bookmark, "");
            continue;
          }
          string tinfo = "";
          if (ainfo.Length > 1)
          {
            tinfo = ainfo[1];
          }
          //直接插入
          if (tinfo == "")
          {
            addTextToBookMark(bookmark, value);
          }
          //日期分隔-年
          else if (tinfo == "YY")
          {
            if (value.Split('-').Length == 3)
              addTextToBookMark(bookmark, value.Split('-')[0]);
          }
          //日期分隔-月
          else if (tinfo == "MM")
          {
            if (value.Split('-').Length == 3)
              addTextToBookMark(bookmark, value.Split('-')[1]);
          }
          //日期分隔-日
          else if (tinfo == "DD")
          {
            if (value.Split('-').Length == 3)
              addTextToBookMark(bookmark, value.Split('-')[2]);
          }
          else if (tinfo == "IMG")
          {
            if (markInfo.ContainsKey(bookmark.Name.ToString()))
            {
              value = markInfo[bookmark.Name.ToString()] + "";
              addImgToBookMark(doc, bookmark, value);
            }
          }
          else if (tinfo == "IMG2")
          {
            if (markInfo.ContainsKey(bookmark.Name.ToString()))
            {
              value = markInfo[bookmark.Name.ToString()] + "";
              addImg2ToBookMark(doc, bookmark, value);
            }
          }
          else if (tinfo == "HTML")
          {
            addHtmlToBookMark(doc, bookmark, value);
          }
          else if (tinfo == "TABLE" && inTable == false)
          {
            addTableToBookMark(doc, bookmark, markInfo[name] as List<Dictionary<string, Object>>);
          }
          else
          {
            addTextToBookMark(bookmark, value);
          }
        }
        else
        {
          addTextToBookMark(bookmark, "");
        }
      }
    }

    private static void addTableToBookMark(WordprocessingDocument doc, BookmarkStart bookmark, List<Dictionary<string, Object>> v)
    {
      MainDocumentPart mainPart = doc.MainDocumentPart;
      var p = bookmark.Parent;
      while (p != null && !(p is TableRow))
      {
        p = p.Parent;
      }
      if (p is TableRow && p.Parent != null)
      {
        var tmarks = from bm in p.Parent.Descendants<TableRow>()
                     select bm;

        TableRow r = p as TableRow;
        TableRow ir = r;
        foreach (var tv in v)
        {
          TableRow tr = (TableRow)r.CloneNode(true);
          ir.InsertAfterSelf(tr);
          ir = tr;
          var bookmarks = from bm in tr.Descendants<BookmarkStart>()
                          select bm;
          foreach (var bk in bookmarks)
          {
            bk.Name = bk.Name.ToString().Replace("_TABLE", "");
          }
          replaceFromBookMark(doc, bookmarks, tv, true);
        }
        r.Parent.RemoveChild(r);
      }
    }

    private static void addTextToBookMark(BookmarkStart bookmark, string v)
    {
      Run bookmarkText = bookmark.NextSibling<Run>();
      if (bookmarkText != null && bookmarkText.GetFirstChild<Text>() != null)
      {
        string vv = bookmarkText.GetFirstChild<Text>().Text;
        if (vv == "")
        {
          vv = v;
        }
        bookmarkText.GetFirstChild<Text>().Text = vv.Replace("?", v).Replace("？", v);
      }
      else
      {
        var parent = bookmark.Parent;
        Text text = new Text(v);
        Run run = new Run(new RunProperties());
        run.Append(text);
        parent.Append(run);
      }
    }
    private static void addImgToBookMark(WordprocessingDocument doc, BookmarkStart bookmark, string v)
    {
      try
      {
        Run bookmarkText = bookmark.NextSibling<Run>();
        if (bookmarkText != null)
        {
          bookmarkText.GetFirstChild<Text>().Text = "";
          bookmarkText.Append(WordHelper.GetImageDrawing(doc, v));
        }
        else
        {
          var parent = bookmark.Parent;
          Run run = new Run(new RunProperties());
          run.Append(WordHelper.GetImageDrawing(doc, v));
          parent.Append(run);
        }
      }
      catch (Exception ex) { }
    }

    private static void addImg2ToBookMark(WordprocessingDocument doc, BookmarkStart bookmark, string v)
    {
      try
      {
        Run bookmarkText = bookmark.NextSibling<Run>();
        if (bookmarkText != null)
        {
          bookmarkText.GetFirstChild<Text>().Text = "";
          bookmarkText.Append(WordHelper.GetImage2Drawing(doc, v));
        }
        else
        {
          var parent = bookmark.Parent;
          Run run = new Run(new RunProperties());
          run.Append(WordHelper.GetImage2Drawing(doc, v));
          parent.Append(run);
        }
      }
      catch (Exception ex) { }
    }

    private static void addHtmlToBookMark(WordprocessingDocument doc, BookmarkStart bookmark, string v)
    {
      var p = bookmark.Parent;
      while (p != null && !(p is TableCell))
      {
        p = p.Parent;
      }
      if (p is TableCell && p.Parent != null)
      {
        var ps = from bm in p.Descendants<Paragraph>()
                 select bm;
        foreach (var pp in ps)
        {
          pp.Remove();
        }
        p.Append(WordHelper.GetEditorChunk(doc, v));
        return;
      }

      Run bookmarkText = bookmark.NextSibling<Run>();
      if (bookmarkText != null)
      {
        //bookmarkText.RemoveChild(bookmarkText.GetFirstChild<Text>());
        var parent = bookmarkText.Parent;
        //bookmarkText.RemoveAllChildren();
        parent.Append(WordHelper.GetEditorChunk(doc, v));
      }
      else
      {
        var parent = bookmark.Parent;
        Run run = new Run(new RunProperties());
        run.Append(WordHelper.GetEditorChunk(doc, v));
        parent.Append(run);
      }
    }

    private static Drawing getPic(string relationshipId,long widthEmus,long heightEmus)
    {
      //const int widthEmus = 914400;
      //const int heightEmus = 360000;
      //var maxWidthCm = 16.51;
      // Define the reference of the image.
      var element =
           new Drawing(
               new DW.Inline(
                   new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                   new DW.EffectExtent()
                   {
                     LeftEdge = 0L,
                     TopEdge = 0L,
                     RightEdge = 0L,
                     BottomEdge = 0L
                   },
                   new DW.DocProperties()
                   {
                     Id = (UInt32Value)1U,
                     Name = "Picture 1"
                   },
                   new DW.NonVisualGraphicFrameDrawingProperties(
                       new A.GraphicFrameLocks() { NoChangeAspect = true }),
                   new A.Graphic(
                       new A.GraphicData(
                           new PIC.Picture(
                               new PIC.NonVisualPictureProperties(
                                   new PIC.NonVisualDrawingProperties()
                                   {
                                     Id = (UInt32Value)0U,
                                     Name = "New Bitmap Image.jpg"
                                   },
                                   new PIC.NonVisualPictureDrawingProperties()),
                               new PIC.BlipFill(
                                   new A.Blip(
                                       new A.BlipExtensionList(
                                           new A.BlipExtension()
                                           {
                                             Uri =
                                                 "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                           })
                                   )
                                   {
                                     Embed = relationshipId,
                                     CompressionState = A.BlipCompressionValues.Print
                                   },
                                   new A.Stretch(
                                       new A.FillRectangle())),
                               new PIC.ShapeProperties(
                                   new A.Transform2D(
                                       new A.Offset() { X = 0L, Y = 0L },
                                       new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                   new A.PresetGeometry(
                                       new A.AdjustValueList()
                                   )
                                   { Preset = A.ShapeTypeValues.Rectangle }))
                       )
                       { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
               )
               {
                 DistanceFromTop = (UInt32Value)0U,
                 DistanceFromBottom = (UInt32Value)0U,
                 DistanceFromLeft = (UInt32Value)0U,
                 DistanceFromRight = (UInt32Value)0U,
                 EditId = "50D07946"
               });
      return element;
    }

    private static Drawing getPic(string relationshipId)
    {
      return WordHelper.getPic(relationshipId,990000L,392000L);
    }
  }
}
