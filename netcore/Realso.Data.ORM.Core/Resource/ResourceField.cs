using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM.Core
{
  public class ResourceField
  {
    public string ID { get; set; }
    public string RESOURCEID { get; set; }
    public string FIELDNAME { get; set; }
    public string FIELDANAME { get; set; }
    public string FIELDTYPE { get; set; }
    public int PREC { get; set; }
    public int NULLABLE { get; set; }
    public int FIELDLENGTH { get; set; }
    public string COMMENTS { get; set; }
    public string REFFIELDID { get; set; }
    public string REFFIELDNAME { get; set; }
    public string REFFIELDANAME { get; set; }
    public string REFRESOURCEID { get; set; }
    public string REFRESOURCEANAME { get; set; }
    public string REFRELATION { get; set; }
    public string UPFIELDID { get; set; }
    public string VFORMAT { get; set; }
    public string ISVIRTUAL { get; set; }
    public string ISVO { get; set; }
    public string ISDO { get; set; }
    public string DEFAULTVALUE { get; set; }
    public string ISKEY { get; set; }
    public string KEYGENTYPE { get; set; }
    public string ENTRYNUM { get; set; }
  }
}
