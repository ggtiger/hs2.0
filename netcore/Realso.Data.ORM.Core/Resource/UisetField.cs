using System;

namespace Realso.Data.ORM.Core
{
  /// <summary>
  /// tss_resuipc 查询字段配置，用于 @ui 过滤器自动生成
  /// </summary>
  public class UisetField
  {
    public string FIELDNAME { get; set; }
    public string LABELNAME { get; set; }
    public string EDITTYPE { get; set; }
    public string QUERYTYPE { get; set; }
    public string QUERYMODE { get; set; }
    public int? QUERYSORT { get; set; }
    public int? LISTSORT { get; set; }
    public byte? DISPLAYINLIST { get; set; }
    public string FIELDTYPE { get; set; }
    public string REFRESOURCEANAME { get; set; }
    public string REFFIELDNAME { get; set; }
    public string REFFIELDID { get; set; }
    public string UPFIELDID { get; set; }
    public string RESFIELDID { get; set; }
  }
}
