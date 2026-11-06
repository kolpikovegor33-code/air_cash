using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using AirlineTicketOffice.Models;

namespace AirlineTicketOffice.Services;

public static class ExcelExportService
{
    public static void ExportToExcel(
        string filePath,
        string reportTitle,
        string periodText,
        string filterText,
        KpiMetrics kpi,
        List<string> headers,
        List<List<object?>> rows)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        AddZipEntry(archive, "[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>");

        AddZipEntry(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

        AddZipEntry(archive, "xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");

        AddZipEntry(archive, "xl/workbook.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Отчёт"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");

        AddZipEntry(archive, "xl/styles.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""4"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
    <font><b/><sz val=""14""/><color rgb=""FF0F172A""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FF0F172A""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""5"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF0284C7""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF8FAFC""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFF1F5F9""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/></border>
    <border>
      <left style=""thin""><color rgb=""FFCBD5E1""/></left>
      <right style=""thin""><color rgb=""FFCBD5E1""/></right>
      <top style=""thin""><color rgb=""FFCBD5E1""/></top>
      <bottom style=""thin""><color rgb=""FFCBD5E1""/></bottom>
    </border>
  </borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""6"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""><alignment horizontal=""center"" vertical=""center"" wrapText=""1""/></xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""><alignment vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""><alignment horizontal=""right"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""><alignment vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""3"" fillId=""4"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""><alignment vertical=""center""/></xf>
  </cellXfs>
</styleSheet>");

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
        sb.AppendLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

        sb.AppendLine("  <cols>");
        int colCount = Math.Max(headers.Count, 4);
        for (int c = 1; c <= colCount; c++)
        {
            double width = c == 1 ? 30 : (c == 2 ? 26 : 20);
            sb.AppendLine($"    <col min=\"{c}\" max=\"{c}\" width=\"{width.ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        }
        sb.AppendLine("  </cols>");

        sb.AppendLine("  <sheetData>");

        int r = 1;

        sb.AppendLine($"    <row r=\"{r}\" ht=\"26\" customHeight=\"1\">");
        sb.AppendLine($"      <c r=\"A{r}\" t=\"inlineStr\" s=\"4\"><is><t>{EscapeXml("СИСТЕМА УЧЁТА ПРОДАЖ АВИАБИЛЕТОВ — " + reportTitle)}</t></is></c>");
        sb.AppendLine("    </row>");
        r++;

        sb.AppendLine($"    <row r=\"{r}\" ht=\"18\" customHeight=\"1\">");
        sb.AppendLine($"      <c r=\"A{r}\" t=\"inlineStr\"><is><t>{EscapeXml($"Период: {periodText} | Фильтр авиакомпании: {filterText} | Дата выгрузки: {DateTime.Now:dd.MM.yyyy HH:mm:ss}")}</t></is></c>");
        sb.AppendLine("    </row>");
        r++;

        r++;

        sb.AppendLine($"    <row r=\"{r}\" ht=\"24\" customHeight=\"1\">");
        sb.AppendLine($"      <c r=\"A{r}\" t=\"inlineStr\" s=\"5\"><is><t>{EscapeXml($"ПРОДАНО БИЛЕТОВ: {kpi.TotalSoldTickets} шт.")}</t></is></c>");
        sb.AppendLine($"      <c r=\"B{r}\" t=\"inlineStr\" s=\"5\"><is><t>{EscapeXml($"СУММА ВЫРУЧКИ: {kpi.TotalRevenue:N0} ₽")}</t></is></c>");
        sb.AppendLine($"      <c r=\"C{r}\" t=\"inlineStr\" s=\"5\"><is><t>{EscapeXml($"СРЕДНЯЯ ЗАГРУЗКА: {kpi.AverageLoadFactorPercent:0.0}%")}</t></is></c>");
        sb.AppendLine($"      <c r=\"D{r}\" t=\"inlineStr\" s=\"5\"><is><t>{EscapeXml($"СУММА ВОЗВРАТОВ: {kpi.TotalRefunds:N0} ₽")}</t></is></c>");
        sb.AppendLine("    </row>");
        r++;

        r++;

        sb.AppendLine($"    <row r=\"{r}\" ht=\"28\" customHeight=\"1\">");
        for (int c = 0; c < headers.Count; c++)
        {
            string colLetter = GetColumnLetter(c + 1);
            sb.AppendLine($"      <c r=\"{colLetter}{r}\" t=\"inlineStr\" s=\"1\"><is><t>{EscapeXml(headers[c])}</t></is></c>");
        }
        sb.AppendLine("    </row>");
        r++;

        foreach (var row in rows)
        {
            sb.AppendLine($"    <row r=\"{r}\" ht=\"20\" customHeight=\"1\">");
            for (int c = 0; c < row.Count; c++)
            {
                string colLetter = GetColumnLetter(c + 1);
                var val = row[c];
                if (val == null)
                {
                    sb.AppendLine($"      <c r=\"{colLetter}{r}\" s=\"2\"/>");
                }
                else if (val is int || val is long || val is short)
                {
                    sb.AppendLine($"      <c r=\"{colLetter}{r}\" s=\"3\"><v>{val}</v></c>");
                }
                else if (val is decimal decVal)
                {
                    sb.AppendLine($"      <c r=\"{colLetter}{r}\" s=\"3\"><v>{decVal.ToString(CultureInfo.InvariantCulture)}</v></c>");
                }
                else if (val is double dblVal)
                {
                    sb.AppendLine($"      <c r=\"{colLetter}{r}\" s=\"3\"><v>{dblVal.ToString(CultureInfo.InvariantCulture)}</v></c>");
                }
                else
                {
                    string strVal = val.ToString() ?? "";
                    sb.AppendLine($"      <c r=\"{colLetter}{r}\" t=\"inlineStr\" s=\"2\"><is><t>{EscapeXml(strVal)}</t></is></c>");
                }
            }
            sb.AppendLine("    </row>");
            r++;
        }

        sb.AppendLine("  </sheetData>");
        sb.AppendLine("</worksheet>");

        AddZipEntry(archive, "xl/worksheets/sheet1.xml", sb.ToString());
    }

    private static void AddZipEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string GetColumnLetter(int colIndex)
    {
        string div = "";
        while (colIndex > 0)
        {
            int mod = (colIndex - 1) % 26;
            div = (char)(65 + mod) + div;
            colIndex = (colIndex - mod) / 26;
        }
        return div;
    }

    private static string EscapeXml(string text)
    {
        return SecurityElement.Escape(text) ?? text;
    }
}
