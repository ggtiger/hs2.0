using System;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Realso.Data.ORM.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Realso.Utils
{
  public class ExcelHelper
  {
    public static void CreateExcel(string excelFilePath, DataView[] tables, string[] sheetNames, ArrayList[] columns)
    {
      var spreadsheetDocument = SpreadsheetDocument.Create(excelFilePath, SpreadsheetDocumentType.Workbook);
      using (spreadsheetDocument)
      {
        var workbookpart = spreadsheetDocument.AddWorkbookPart();
        workbookpart.Workbook = new Workbook();

        Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
        for (int i = 0; i < tables.Length; i++)
        {
          WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
          worksheetPart.Worksheet = new Worksheet(CreatSheetData(tables[i], columns[i]));
          Sheet sheet = new Sheet()
          {
            Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
            SheetId = new UInt32Value((uint)(i + 1)),
            Name = (sheetNames != null ? sheetNames[i] : i.ToString())
          };
          sheets.Append(sheet);
        }
        workbookpart.Workbook.Save();
      }
    }

    public static SheetData CreatSheetData(DataView table, ArrayList fields)
    {
      var sheetData = new SheetData();
      Row head = new Row();
      Dictionary<string, ViewColumn> dFields = new Dictionary<string, ViewColumn>();
      for (int i = 0; i < fields.Count; i++)
      {
        ViewColumn column = table.Columns.Find((ViewColumn vc) =>
        {
          return vc.Name == (fields[i] as Hashtable)["key"] + "";
        });
        if (column != null)
        {
          dFields.Add(column.Name, column);
        }
      }
      for (int i = 0; i < fields.Count; i++)
      {
        string field = (fields[i] as Hashtable)["key"] + "";
        if (dFields.Keys.Contains(field))
        {
          Cell dataCell = new Cell();
          dataCell.CellValue = new CellValue((fields[i] as Hashtable)["title"] + "");
          dataCell.DataType = CellValues.String;
          head.AppendChild(dataCell);
        }
      }
      sheetData.Append(head);
      for (int r = 0; r < table.Count; r++)
      {
        Row row = new Row();
        for (int c = 0; c < fields.Count; c++)
        {
          Hashtable hField = (fields[c] as Hashtable);
          string field = hField["key"] + "";
          if (!dFields.Keys.Contains(field))
          {
            continue;
          }
          Cell dataCell = new Cell();
          if (dFields[field].Type == "float")
            dataCell.DataType = new EnumValue<CellValues>(CellValues.Number);
          else
            dataCell.DataType = new EnumValue<CellValues>(CellValues.String);
          try
          {
            string val = table[r].GetString(hField["key"] + "");
            if (hField["dict"] + "" != "")
            {
              val = (hField["dictData"] as Hashtable)[val] + "";
            }
            dataCell.CellValue = new CellValue(val);
          }
          catch (Exception ex)
          {
            Logger.Error(ex.Message + fields);
          }

          row.AppendChild(dataCell);
        }
        sheetData.Append(row);
      }
      return sheetData;
    }
  }
}
