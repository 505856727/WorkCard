using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WorkCard.Editor.Config
{
    public static class CsvTableIO
    {
        public static TableSheet Read(string file)
        {
            var sheet = new TableSheet
            {
                Name = Path.GetFileNameWithoutExtension(file),
                File = file,
            };
            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {
                if (line.Length > 0 && line[0] == '\uFEFF')
                {
                    sheet.Rows.Add(ParseLine(line.Substring(1)));
                }
                else
                {
                    sheet.Rows.Add(ParseLine(line));
                }
            }

            return sheet;
        }

        public static void Write(string file, TableSheet sheet)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();
            foreach (var row in sheet.Rows)
            {
                for (var i = 0; i < row.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Escape(row[i]));
                }

                sb.AppendLine();
            }

            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
        }

        static List<string> ParseLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    cells.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(c);
                }
            }

            cells.Add(sb.ToString());
            return cells;
        }

        static string Escape(string value)
        {
            value ??= "";
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
