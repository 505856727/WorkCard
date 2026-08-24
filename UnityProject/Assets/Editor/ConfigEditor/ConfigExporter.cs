using System;
using System.Collections.Generic;
using System.IO;
using WorkCard.Config;
using ValueType = WorkCard.Config.ValueType;

namespace WorkCard.Editor.Config
{
    public class ExportResult
    {
        public string Name;
        public string BytesPath;
        public string CsvPath;
        public int RowCount;
        public readonly List<string> Errors = new List<string>();
        public bool Ok => Errors.Count == 0;
    }

    public static class ConfigExporter
    {
        public const string Extension = ".bytes";

        public static List<TableSheet> LoadSourceFile(string file)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".csv")
            {
                return new List<TableSheet> { CsvTableIO.Read(file) };
            }

            if (ext == ".xlsx" || ext == ".xls")
            {
                return XlsxTableIO.Read(file);
            }

            return new List<TableSheet>();
        }

        public static List<string> ScanSourceFiles(string excelDir)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(excelDir) || !Directory.Exists(excelDir))
            {
                return files;
            }

            foreach (var file in Directory.GetFiles(excelDir, "*.*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("~$"))
                {
                    continue;
                }

                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".csv" || ext == ".xlsx" || ext == ".xls")
                {
                    files.Add(file);
                }
            }

            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        public static ExportResult Export(
            TableSheet sheet,
            string bytesDir,
            string csvDir,
            int descRow = 0,
            int keyRow = 1,
            int typeRow = 2)
        {
            var dataRow = Math.Max(descRow, Math.Max(keyRow, typeRow)) + 1;
            var parsed = TableParser.Parse(sheet, descRow, keyRow, typeRow, dataRow);
            var result = new ExportResult
            {
                Name = parsed.Name,
                RowCount = parsed.Rows.Count,
            };
            result.Errors.AddRange(parsed.Errors);
            if (!result.Ok)
            {
                return result;
            }

            try
            {
                result.BytesPath = WriteBytes(parsed, bytesDir);
                if (!string.IsNullOrEmpty(csvDir))
                {
                    result.CsvPath = Path.Combine(csvDir, parsed.Name + ".csv");
                    CsvTableIO.Write(result.CsvPath, sheet);
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(parsed.Name + " 导出失败：" + e.Message);
            }

            return result;
        }

        static string WriteBytes(ParsedTable table, string bytesDir)
        {
            if (string.IsNullOrEmpty(bytesDir))
            {
                throw new Exception("未设置二进制导出路径");
            }

            Directory.CreateDirectory(bytesDir);
            var writer = new BufferWriter();
            writer.WriteUByte(0);
            writer.WriteUByte(table.Headers.Count);
            foreach (var header in table.Headers)
            {
                var type = ValueTypeUtil.GetExportType(header.Type);
                var elemType = ValueTypeUtil.GetExportType(header.ElemType);
                writer.WriteUByte((int)type);
                if (type == ValueType.Array1 || type == ValueType.Array2)
                {
                    writer.WriteUByte((int)elemType);
                }

                writer.WriteString(header.Key);
            }

            writer.WriteInt32(table.Rows.Count);
            foreach (var row in table.Rows)
            {
                for (var i = 0; i < table.Headers.Count; i++)
                {
                    var header = table.Headers[i];
                    writer.WriteValue(
                        ValueTypeUtil.GetExportType(header.Type),
                        row[i],
                        ValueTypeUtil.GetExportType(header.ElemType));
                }
            }

            var head = new BufferWriter();
            head.WriteSByte(ConfigLoader.Version);
            head.SetStrings(writer.Strings);
            head.WriteStringList();
            writer.Prepend(head.ToArray());

            var path = Path.Combine(bytesDir, table.Name + Extension);
            File.WriteAllBytes(path, writer.ToArray());
            return path;
        }
    }
}
