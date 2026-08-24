using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace WorkCard.Editor.Config
{
    public static class XlsxTableIO
    {
        static readonly Regex CellRef = new Regex(@"^([A-Z]+)(\d+)$", RegexOptions.Compiled);

        public static List<TableSheet> Read(string file)
        {
            var result = new List<TableSheet>();
            using var zip = ZipFile.OpenRead(file);
            var strings = ReadSharedStrings(zip);
            var sheets = ReadWorkbookSheets(zip);
            foreach (var sheet in sheets)
            {
                var entry = zip.GetEntry("xl/" + sheet.Path.TrimStart('/'));
                if (entry == null && sheet.Path.StartsWith("xl/"))
                {
                    entry = zip.GetEntry(sheet.Path);
                }

                if (entry == null)
                {
                    continue;
                }

                var table = new TableSheet { Name = sheet.Name, File = file };
                FillSheet(entry, strings, table);
                result.Add(table);
            }

            return result;
        }

        static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return list;
            }

            var doc = LoadXml(entry);
            var ns = CreateNs(doc);
            var siNodes = doc.SelectNodes("//x:si", ns);
            if (siNodes == null)
            {
                return list;
            }

            foreach (XmlNode node in siNodes)
            {
                var texts = node.SelectNodes(".//x:t", ns);
                var sb = new StringBuilder();
                if (texts != null)
                {
                    foreach (XmlNode t in texts)
                    {
                        sb.Append(t.InnerText);
                    }
                }

                list.Add(sb.ToString());
            }

            return list;
        }

        static List<(string Name, string Path)> ReadWorkbookSheets(ZipArchive zip)
        {
            var sheets = new List<(string, string)>();
            var workbook = zip.GetEntry("xl/workbook.xml");
            var rels = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (workbook == null || rels == null)
            {
                return sheets;
            }

            var relMap = new Dictionary<string, string>();
            var relDoc = LoadXml(rels);
            foreach (XmlNode rel in relDoc.DocumentElement)
            {
                var id = rel.Attributes?["Id"]?.Value;
                var target = rel.Attributes?["Target"]?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                {
                    relMap[id] = target.Replace("\\", "/");
                }
            }

            var wbDoc = LoadXml(workbook);
            var ns = CreateNs(wbDoc);
            var sheetNodes = wbDoc.SelectNodes("//x:sheet", ns);
            if (sheetNodes == null)
            {
                return sheets;
            }

            foreach (XmlNode sheet in sheetNodes)
            {
                var name = sheet.Attributes?["name"]?.Value;
                var rid = sheet.Attributes?["r:id"]?.Value
                          ?? sheet.Attributes?.GetNamedItem("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")?.Value
                          ?? sheet.Attributes?["id"]?.Value;
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rid) || !relMap.TryGetValue(rid, out var target))
                {
                    continue;
                }

                if (!target.StartsWith("xl/"))
                {
                    target = "xl/" + target.TrimStart('/');
                }

                sheets.Add((name, target));
            }

            return sheets;
        }

        static void FillSheet(ZipArchiveEntry entry, List<string> strings, TableSheet table)
        {
            var doc = LoadXml(entry);
            var ns = CreateNs(doc);
            var cellNodes = doc.SelectNodes("//x:c", ns);
            if (cellNodes == null)
            {
                return;
            }

            foreach (XmlNode cell in cellNodes)
            {
                var refer = cell.Attributes?["r"]?.Value;
                if (string.IsNullOrEmpty(refer) || !TryParseCell(refer, out var col, out var row))
                {
                    continue;
                }

                EnsureCell(table, row, col);
                table.Rows[row][col] = ReadCell(cell, strings, ns);
            }
        }

        static string ReadCell(XmlNode cell, List<string> strings, XmlNamespaceManager ns)
        {
            var type = cell.Attributes?["t"]?.Value;
            var value = cell.SelectSingleNode("x:v", ns)?.InnerText ?? "";
            if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < strings.Count)
            {
                return strings[index];
            }

            if (type == "inlineStr")
            {
                var texts = cell.SelectNodes(".//x:t", ns);
                var sb = new StringBuilder();
                if (texts != null)
                {
                    foreach (XmlNode t in texts)
                    {
                        sb.Append(t.InnerText);
                    }
                }

                return sb.ToString();
            }

            return value;
        }

        static void EnsureCell(TableSheet table, int row, int col)
        {
            while (table.Rows.Count <= row)
            {
                table.Rows.Add(new List<string>());
            }

            while (table.Rows[row].Count <= col)
            {
                table.Rows[row].Add("");
            }
        }

        static bool TryParseCell(string refer, out int col, out int row)
        {
            col = 0;
            row = 0;
            var match = CellRef.Match(refer);
            if (!match.Success)
            {
                return false;
            }

            var letters = match.Groups[1].Value;
            foreach (var c in letters)
            {
                col = col * 26 + (c - 'A' + 1);
            }

            col -= 1;
            row = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) - 1;
            return col >= 0 && row >= 0;
        }

        static XmlDocument LoadXml(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            var doc = new XmlDocument();
            doc.Load(stream);
            return doc;
        }

        static XmlNamespaceManager CreateNs(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            return ns;
        }
    }
}
