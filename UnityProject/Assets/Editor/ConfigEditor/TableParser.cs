using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using WorkCard.Config;
using ValueType = WorkCard.Config.ValueType;

namespace WorkCard.Editor.Config
{
    public class ParsedHeader
    {
        public string Key;
        public string RawKey;
        public string Desc;
        public string TypeString;
        public ValueType Type;
        public ValueType ElemType;
        public int Col;
        public bool Fitted;
    }

    public class ParsedTable
    {
        public string Name;
        public string File;
        public readonly List<ParsedHeader> Headers = new List<ParsedHeader>();
        public readonly List<object[]> Rows = new List<object[]>();
        public readonly List<string> Errors = new List<string>();
    }

    public static class TableParser
    {
        public const string ExportSuffix = "Config";

        static readonly Regex CamelSplit = new Regex(@"[_.\-\s]+(\w|$)", RegexOptions.Compiled);

        public static bool IsConfigName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.EndsWith(ExportSuffix, StringComparison.Ordinal);
        }

        public static string Pascalize(string value)
        {
            var camel = Camelize(value);
            return string.IsNullOrEmpty(camel) ? camel : char.ToUpperInvariant(camel[0]) + camel.Substring(1);
        }

        public static string Camelize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            var camel = CamelSplit.Replace(value, match =>
            {
                var ch = match.Groups[1].Value;
                return string.IsNullOrEmpty(ch) ? "" : ch.ToUpperInvariant();
            });
            return char.ToLowerInvariant(camel[0]) + camel.Substring(1);
        }

        public static ParsedTable Parse(TableSheet sheet, int descRow = 0, int keyRow = 2, int typeRow = 1, int dataRow = 3)
        {
            var table = new ParsedTable
            {
                Name = Pascalize(sheet.Name),
                File = sheet.File,
            };
            if (sheet.RowCount < 3)
            {
                table.Errors.Add($"（{sheet.Name}）配置表格式错误，字段定义少于3行");
                return table;
            }

            var headerRows = ResolveHeaderRows(sheet, descRow, keyRow, typeRow, dataRow);
            var startCol = headerRows.newFormat ? 1 : 0;
            var keys = new HashSet<string>();
            for (var col = startCol; col < sheet.ColCount; col++)
            {
                var typeCell = sheet.Cell(headerRows.type, col).Trim();
                if (string.IsNullOrEmpty(typeCell))
                {
                    continue;
                }

                var typeString = typeCell.ToLowerInvariant().Split('.')[0];
                if (!TryParseType(typeString, out var type, out var elemType))
                {
                    table.Errors.Add($"无法识别类型: {typeString}");
                    continue;
                }

                var rawKey = sheet.Cell(headerRows.key, col).Trim();
                var key = string.IsNullOrEmpty(rawKey) ? "" : Camelize(rawKey);
                if (!string.IsNullOrEmpty(key) && !keys.Add(key))
                {
                    table.Errors.Add($"字段名（{key}）重复定义");
                }

                table.Headers.Add(new ParsedHeader
                {
                    Key = key,
                    RawKey = rawKey,
                    Desc = sheet.Cell(headerRows.desc, col),
                    TypeString = typeString,
                    Type = type,
                    ElemType = elemType,
                    Col = col,
                });
            }

            if (table.Headers.Count == 0)
            {
                table.Errors.Add($"（{sheet.Name}）没有可导出的字段");
                return table;
            }

            for (var row = headerRows.data; row < sheet.RowCount; row++)
            {
                if (headerRows.newFormat)
                {
                    var tag = sheet.Cell(row, 0).Trim();
                    if (tag == "##")
                    {
                        continue;
                    }
                }
                else if (string.IsNullOrEmpty(sheet.Cell(row, table.Headers[0].Col)))
                {
                    break;
                }

                if (IsEmptyRow(sheet, row, table.Headers))
                {
                    continue;
                }

                var values = new object[table.Headers.Count];
                for (var i = 0; i < table.Headers.Count; i++)
                {
                    var header = table.Headers[i];
                    var cell = sheet.Cell(row, header.Col);
                    try
                    {
                        values[i] = ReadField(header, cell);
                    }
                    catch (Exception e)
                    {
                        table.Errors.Add($"{sheet.Name} 第{row + 1}行 {header.Key}：{e.Message}");
                        values[i] = DefaultValue(header);
                    }
                }

                table.Rows.Add(values);
            }

            return table;
        }

        static (bool newFormat, int desc, int key, int type, int data) ResolveHeaderRows(
            TableSheet sheet, int descRow, int keyRow, int typeRow, int dataRow)
        {
            var first = sheet.Cell(0, 0);
            if (string.IsNullOrEmpty(first) || !first.StartsWith("##", StringComparison.Ordinal))
            {
                return (false, descRow, keyRow, typeRow, dataRow);
            }

            var desc = -1;
            var key = -1;
            var type = -1;
            var data = -1;
            for (var row = 0; row < 4 && row < sheet.RowCount; row++)
            {
                var tag = sheet.Cell(row, 0);
                if (string.IsNullOrEmpty(tag) || !tag.StartsWith("##", StringComparison.Ordinal))
                {
                    data = row + 1;
                    continue;
                }

                switch (tag)
                {
                    case "##var":
                        key = row;
                        data = row + 1;
                        break;
                    case "##type":
                        type = row;
                        data = row + 1;
                        break;
                    case "##desc":
                        desc = row;
                        data = row + 1;
                        break;
                    case "##group":
                        data = row + 1;
                        break;
                }
            }

            return (true, desc < 0 ? descRow : desc, key < 0 ? keyRow : key, type < 0 ? typeRow : type, data < 0 ? dataRow : data);
        }

        static bool TryParseType(string typeString, out ValueType type, out ValueType elemType) =>
            ValueTypeUtil.TryParse(typeString, out type, out elemType);

        static bool IsEmptyRow(TableSheet sheet, int row, List<ParsedHeader> headers)
        {
            foreach (var header in headers)
            {
                if (!string.IsNullOrEmpty(sheet.Cell(row, header.Col)))
                {
                    return false;
                }
            }

            return true;
        }

        static object ReadField(ParsedHeader header, string cell)
        {
            if (string.IsNullOrEmpty(cell))
            {
                return DefaultValue(header);
            }

            return ReadValue(header, header.Type, cell);
        }

        static object DefaultValue(ParsedHeader header)
        {
            switch (header.Type)
            {
                case ValueType.Bool: return false;
                case ValueType.String:
                case ValueType.IndexString:
                case ValueType.Text:
                case ValueType.IndexText:
                case ValueType.Asset: return "";
                case ValueType.Time: return 0;
                case ValueType.Buffer: return Array.Empty<byte>();
                case ValueType.Expression:
                case ValueType.Object: return null;
                case ValueType.Array1: return CreateArray(header.ElemType, 0);
                case ValueType.Array2: return Array.CreateInstance(ArrayElemSystemType(header.ElemType), 0);
                case ValueType.Vec2:
                case ValueType.Size: return UnityEngine.Vector2.zero;
                case ValueType.Vec3: return UnityEngine.Vector3.zero;
                case ValueType.Float:
                case ValueType.Double: return 0f;
                default: return 0;
            }
        }

        static object ReadValue(ParsedHeader header, ValueType type, string cell)
        {
            switch (type)
            {
                case ValueType.Bool:
                    return string.Equals(cell.Trim(), "true", StringComparison.OrdinalIgnoreCase) || cell.Trim() == "1";
                case ValueType.String:
                case ValueType.IndexString:
                case ValueType.Text:
                case ValueType.IndexText:
                case ValueType.Asset:
                    return cell;
                case ValueType.Time:
                    return ReadTime(cell);
                case ValueType.Buffer:
                    return ReadBuffer(cell);
                case ValueType.Array1:
                    return ReadArray1(header, header.ElemType, cell);
                case ValueType.Array2:
                    return ReadArray2(header, cell);
                case ValueType.Vec2:
                case ValueType.Size:
                    return ReadVec2(cell);
                case ValueType.Vec3:
                    return ReadVec3(cell);
                case ValueType.Color:
                    return ReadColor(cell);
                default:
                    if (IsNumber(type))
                    {
                        var number = ParseNumber(cell);
                        FitNumber(header, type, number, false);
                        return IsIntNumber(type) && number == Math.Truncate(number) ? (object)(int)number : (float)number;
                    }

                    throw new Exception("不支持的数据类型（" + header.TypeString + "）");
            }
        }

        static Array ReadArray1(ParsedHeader header, ValueType elemType, string cell)
        {
            if (ValueTypeUtil.IsString(elemType))
            {
                var texts = ParseStringArray(cell);
                return texts.ToArray();
            }

            var parts = Regex.Split(cell, @"[|,]");
            var values = new List<object>();
            foreach (var part in parts)
            {
                if (IsNumber(elemType))
                {
                    var number = ParseNumber(part);
                    FitNumber(header, elemType, number, true);
                    values.Add(IsIntNumber(elemType) && number == Math.Truncate(number) ? (object)(int)number : (float)number);
                }
                else
                {
                    values.Add(ReadValue(header, elemType, part));
                }
            }

            var array = CreateArray(elemType, values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        static Array ReadArray2(ParsedHeader header, string cell)
        {
            if (ValueTypeUtil.IsString(header.ElemType))
            {
                var rows = ParseStringArray2D(cell);
                var array = Array.CreateInstance(typeof(string[]), rows.Count);
                for (var i = 0; i < rows.Count; i++)
                {
                    array.SetValue(rows[i].ToArray(), i);
                }

                return array;
            }

            var lines = cell.Split('|');
            var result = Array.CreateInstance(ArrayElemSystemType(header.ElemType), lines.Length);
            for (var i = 0; i < lines.Length; i++)
            {
                result.SetValue(ReadArray1(header, header.ElemType, lines[i]), i);
            }

            return result;
        }

        static List<string> ParseStringArray(string value)
        {
            var list = new List<string>();
            ParseStringArrayCore(value, list, null);
            return list;
        }

        static List<List<string>> ParseStringArray2D(string value)
        {
            var rows = new List<List<string>>();
            var current = new List<string>();
            ParseStringArrayCore(value, current, rows);
            if (current.Count > 0 || rows.Count == 0)
            {
                rows.Add(current);
            }

            return rows;
        }

        static void ParseStringArrayCore(string value, List<string> current, List<List<string>> rows)
        {
            var i = 0;
            var afterItem = false;
            while (i < value.Length)
            {
                if (afterItem)
                {
                    var ch = value[i++];
                    if (ch == ' ')
                    {
                        continue;
                    }

                    if (rows != null && ch == '|')
                    {
                        rows.Add(new List<string>(current));
                        current.Clear();
                        afterItem = false;
                        continue;
                    }

                    if (ch != ',')
                    {
                        throw new Exception("字符串输入错误，应为逗号分隔");
                    }

                    afterItem = false;
                    continue;
                }

                if (value[i] == ' ')
                {
                    i++;
                    continue;
                }

                if (value[i] != '`')
                {
                    throw new Exception("字符串数组请使用反引号：`a`, `b`");
                }

                current.Add(ReadQuoted(value, ref i));
                afterItem = true;
            }

            if (rows != null && current.Count > 0)
            {
                rows.Add(new List<string>(current));
                current.Clear();
            }
        }

        static string ReadQuoted(string value, ref int index)
        {
            var quote = value[index++];
            var sb = new System.Text.StringBuilder();
            while (index < value.Length)
            {
                var ch = value[index++];
                if (ch == quote)
                {
                    return sb.ToString();
                }

                if (ch == '\\' && index < value.Length)
                {
                    var next = value[index++];
                    sb.Append(next switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => next,
                    });
                }
                else
                {
                    sb.Append(ch);
                }
            }

            throw new Exception("字符串缺少结尾 `");
        }

        static UnityEngine.Vector2 ReadVec2(string cell)
        {
            var xy = cell.Split(':');
            if (xy.Length != 2)
            {
                throw new Exception("坐标格式错误，请使用 x:y");
            }

            return new UnityEngine.Vector2(ParseFloat(xy[0]), ParseFloat(xy[1]));
        }

        static UnityEngine.Vector3 ReadVec3(string cell)
        {
            var xyz = cell.Split(':');
            if (xyz.Length != 3)
            {
                throw new Exception("坐标格式错误，请使用 x:y:z");
            }

            return new UnityEngine.Vector3(ParseFloat(xyz[0]), ParseFloat(xyz[1]), ParseFloat(xyz[2]));
        }

        static uint ReadColor(string cell)
        {
            var color = cell.StartsWith("#", StringComparison.Ordinal) ? cell.Substring(1) : cell;
            if (color.Length != 6 && color.Length != 8)
            {
                throw new Exception("颜色格式错误，请使用 #RRGGBB 或 #RRGGBBAA");
            }

            var r = Convert.ToInt32(color.Substring(0, 2), 16);
            var g = Convert.ToInt32(color.Substring(2, 2), 16);
            var b = Convert.ToInt32(color.Substring(4, 2), 16);
            var a = color.Length == 8 ? Convert.ToInt32(color.Substring(6, 2), 16) : 255;
            return (uint)((a << 24) | (b << 16) | (g << 8) | r);
        }

        static int ReadTime(string cell)
        {
            var text = (cell ?? "").Trim();
            if (text.Contains(":"))
            {
                var hm = text.Split(':');
                return int.Parse(hm[0], CultureInfo.InvariantCulture) * 60
                       + int.Parse(hm[1], CultureInfo.InvariantCulture);
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
            {
                return (int)Math.Ceiling(serial * 24 * 60);
            }

            throw new Exception("时间数据类型不对，请使用 HH:MM");
        }

        static byte[] ReadBuffer(string cell)
        {
            var text = (cell ?? "").Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(2);
            }

            if (text.Length == 0 || (text.Length % 2) != 0)
            {
                return Array.Empty<byte>();
            }

            var data = new byte[text.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }

            return data;
        }

        static double ParseNumber(string cell)
        {
            var text = (cell ?? "").Trim();
            if (text.Length == 0)
            {
                return 0;
            }

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                && long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
            {
                return hex;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new Exception("数值（" + cell + "）错误");
        }

        static float ParseFloat(string cell) => (float)ParseNumber(cell);

        static void FitNumber(ParsedHeader header, ValueType declared, double value, bool arrayElem)
        {
            var fitted = declared == ValueType.Int ? AdaptNumber(ValueType.SByte, value) : AdaptNumber(declared, value);
            if (!header.Fitted)
            {
                if (arrayElem)
                {
                    header.ElemType = fitted;
                }
                else
                {
                    header.Type = fitted;
                }

                header.Fitted = true;
                return;
            }

            var selected = SelectNumberType(arrayElem ? header.ElemType : header.Type, fitted);
            if (arrayElem)
            {
                header.ElemType = selected;
            }
            else
            {
                header.Type = selected;
            }
        }

        static ValueType AdaptNumber(ValueType type, double value)
        {
            switch (type)
            {
                case ValueType.SByte:
                    if (value < -128) return AdaptNumber(ValueType.Int16, value);
                    if (value > 127) return AdaptNumber(ValueType.UByte, value);
                    break;
                case ValueType.UByte:
                    if (value < 0) return AdaptNumber(ValueType.SByte, value);
                    if (value > 255) return AdaptNumber(ValueType.Int16, value);
                    break;
                case ValueType.Int16:
                    if (value < -32768) return AdaptNumber(ValueType.Int, value);
                    if (value > 32767) return AdaptNumber(ValueType.UInt16, value);
                    break;
                case ValueType.UInt16:
                    if (value < 0) return AdaptNumber(ValueType.Int16, value);
                    if (value > 65536) return AdaptNumber(ValueType.Int, value);
                    break;
                case ValueType.Int:
                    if (value > 2147483647) return AdaptNumber(ValueType.UInt, value);
                    break;
                case ValueType.UInt:
                    if (value < 0) return AdaptNumber(ValueType.Int, value);
                    break;
                case ValueType.Float:
                    if (value < -3.4e38 || value > 3.4e38) return AdaptNumber(ValueType.Double, value);
                    break;
            }

            return type;
        }

        static ValueType SelectNumberType(ValueType oldType, ValueType newType)
        {
            if (oldType == newType)
            {
                return oldType;
            }

            var oldFloat = IsFloat(oldType);
            var newFloat = IsFloat(newType);
            var oldUnsigned = IsUnsigned(oldType);
            var newUnsigned = IsUnsigned(newType);
            if (oldFloat == newFloat)
            {
                if (oldFloat || oldUnsigned == newUnsigned)
                {
                    return newType > oldType ? newType : oldType;
                }

                if (oldUnsigned)
                {
                    return oldType < newType ? newType : oldType + 1;
                }

                return newType < oldType ? oldType : newType + 1;
            }

            return oldFloat ? oldType : newType;
        }

        static bool IsNumber(ValueType type) =>
            type is ValueType.SByte or ValueType.UByte or ValueType.Int16 or ValueType.UInt16
                or ValueType.Int or ValueType.UInt or ValueType.Float or ValueType.Double;

        static bool IsIntNumber(ValueType type) =>
            type is ValueType.SByte or ValueType.UByte or ValueType.Int16 or ValueType.UInt16
                or ValueType.Int or ValueType.UInt;

        static bool IsFloat(ValueType type) => type is ValueType.Float or ValueType.Double;

        static bool IsUnsigned(ValueType type) => type is ValueType.UByte or ValueType.UInt16 or ValueType.UInt;

        static Array CreateArray(ValueType elemType, int length)
        {
            switch (elemType)
            {
                case ValueType.Bool: return new bool[length];
                case ValueType.Float:
                case ValueType.Double: return new float[length];
                case ValueType.String:
                case ValueType.IndexString:
                case ValueType.Text:
                case ValueType.IndexText:
                case ValueType.Asset: return new string[length];
                default: return new int[length];
            }
        }

        static Type ArrayElemSystemType(ValueType elemType)
        {
            switch (elemType)
            {
                case ValueType.Bool: return typeof(bool[]);
                case ValueType.Float:
                case ValueType.Double: return typeof(float[]);
                case ValueType.String:
                case ValueType.IndexString:
                case ValueType.Text:
                case ValueType.IndexText:
                case ValueType.Asset: return typeof(string[]);
                default: return typeof(int[]);
            }
        }
    }
}
