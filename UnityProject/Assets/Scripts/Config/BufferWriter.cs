using System;
using System.Collections.Generic;
using System.Text;

namespace WorkCard.Config
{
    public class BufferWriter
    {
        readonly List<byte> _buf = new List<byte>(1024);
        readonly List<string> _strings = new List<string>();
        readonly Dictionary<string, int> _stringIndex = new Dictionary<string, int>();
        readonly bool _littleEndian;

        public BufferWriter(bool littleEndian = false)
        {
            _littleEndian = littleEndian;
        }

        public IReadOnlyList<string> Strings => _strings;

        public void Clear()
        {
            _buf.Clear();
            _strings.Clear();
            _stringIndex.Clear();
        }

        public void SetStrings(IEnumerable<string> strings)
        {
            _strings.Clear();
            _stringIndex.Clear();
            if (strings == null)
            {
                return;
            }

            foreach (var s in strings)
            {
                AddString(s ?? "");
            }
        }

        public int AddString(string value)
        {
            value ??= "";
            if (_stringIndex.TryGetValue(value, out var index))
            {
                return index;
            }

            index = _strings.Count;
            _strings.Add(value);
            _stringIndex[value] = index;
            return index;
        }

        public void WriteStringList()
        {
            if (_strings.Count == 0)
            {
                WriteUByte(0);
                return;
            }

            WriteUByte(1);
            WriteUInt16((ushort)_strings.Count);
            foreach (var s in _strings)
            {
                WriteString(s);
            }
        }

        public void WriteSByte(int value) => _buf.Add((byte)(sbyte)value);

        public void WriteUByte(int value) => _buf.Add((byte)value);

        public void WriteBool(bool value) => _buf.Add(value ? (byte)1 : (byte)0);

        public void WriteInt16(int value) => WriteRaw(BitConverter.GetBytes((short)value), 2);

        public void WriteUInt16(int value) => WriteRaw(BitConverter.GetBytes((ushort)value), 2);

        public void WriteInt32(int value) => WriteRaw(BitConverter.GetBytes(value), 4);

        public void WriteUInt32(uint value) => WriteRaw(BitConverter.GetBytes(value), 4);

        public void WriteFloat(float value) => WriteRaw(BitConverter.GetBytes(value), 4);

        public void WriteDouble(double value) => WriteRaw(BitConverter.GetBytes(value), 8);

        public void WriteString(string value)
        {
            var data = Encoding.UTF8.GetBytes(value ?? "");
            WriteUInt16(data.Length);
            _buf.AddRange(data);
        }

        public void WriteIndexString(string value) => WriteUInt16(AddString(value ?? ""));

        public void WriteBuffer(object value)
        {
            if (value is not byte[] data)
            {
                WriteUInt16(0);
                return;
            }

            WriteUInt16(data.Length);
            _buf.AddRange(data);
        }

        public void WriteValue(ValueType type, object value, ValueType elemType = ValueType.Null)
        {
            switch (type)
            {
                case ValueType.Bool:
                    WriteBool(ToBool(value));
                    break;
                case ValueType.SByte:
                    WriteSByte(ToInt(value));
                    break;
                case ValueType.UByte:
                    WriteUByte(ToInt(value));
                    break;
                case ValueType.Int16:
                    WriteInt16(ToInt(value));
                    break;
                case ValueType.UInt16:
                    WriteUInt16(ToInt(value));
                    break;
                case ValueType.Int:
                    WriteInt32(ToInt(value));
                    break;
                case ValueType.UInt:
                    WriteUInt32((uint)ToInt(value));
                    break;
                case ValueType.Float:
                    WriteFloat(ToFloat(value));
                    break;
                case ValueType.Double:
                    WriteDouble(ToFloat(value));
                    break;
                case ValueType.String:
                case ValueType.Text:
                case ValueType.Asset:
                    WriteString(value?.ToString() ?? "");
                    break;
                case ValueType.IndexString:
                case ValueType.IndexText:
                    WriteIndexString(value?.ToString() ?? "");
                    break;
                case ValueType.Time:
                    WriteUInt16(ToInt(value));
                    break;
                case ValueType.Buffer:
                    WriteBuffer(value);
                    break;
                case ValueType.Array1:
                    WriteArray1(value, elemType);
                    break;
                case ValueType.Array2:
                    WriteArray2(value, elemType);
                    break;
                case ValueType.Vec2:
                case ValueType.Size:
                    WriteVec2(value);
                    break;
                case ValueType.Vec3:
                    WriteVec3(value);
                    break;
                case ValueType.Color:
                    WriteUInt32((uint)ToInt(value));
                    break;
                default:
                    throw new Exception("不支持导出的配置类型：" + type);
            }
        }

        public void Prepend(byte[] prefix)
        {
            _buf.InsertRange(0, prefix);
        }

        public byte[] ToArray() => _buf.ToArray();

        void WriteVec2(object value)
        {
            if (value is UnityEngine.Vector2 v2)
            {
                WriteFloat(v2.x);
                WriteFloat(v2.y);
                return;
            }

            WriteFloat(0);
            WriteFloat(0);
        }

        void WriteVec3(object value)
        {
            if (value is UnityEngine.Vector3 v3)
            {
                WriteFloat(v3.x);
                WriteFloat(v3.y);
                WriteFloat(v3.z);
                return;
            }

            WriteFloat(0);
            WriteFloat(0);
            WriteFloat(0);
        }

        void WriteArray1(object value, ValueType elemType)
        {
            if (value is not Array array)
            {
                WriteUByte(0);
                return;
            }

            WriteUByte(array.Length);
            foreach (var item in array)
            {
                WriteValue(elemType, item);
            }
        }

        void WriteArray2(object value, ValueType elemType)
        {
            if (value is not Array array)
            {
                WriteUByte(0);
                return;
            }

            WriteUByte(array.Length);
            foreach (var item in array)
            {
                WriteValue(ValueType.Array1, item, elemType);
            }
        }

        void WriteRaw(byte[] data, int size)
        {
            if (BitConverter.IsLittleEndian == _littleEndian)
            {
                for (var i = 0; i < size; i++)
                {
                    _buf.Add(data[i]);
                }

                return;
            }

            for (var i = size - 1; i >= 0; i--)
            {
                _buf.Add(data[i]);
            }
        }

        static bool ToBool(object value)
        {
            if (value is bool b)
            {
                return b;
            }

            var text = value?.ToString()?.Trim();
            return text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        }

        static int ToInt(object value)
        {
            if (value is int i)
            {
                return i;
            }

            if (value is float f)
            {
                return (int)f;
            }

            return int.TryParse(value?.ToString(), out var n) ? n : 0;
        }

        static float ToFloat(object value)
        {
            if (value is float f)
            {
                return f;
            }

            if (value is double d)
            {
                return (float)d;
            }

            return float.TryParse(value?.ToString(), out var n) ? n : 0f;
        }
    }
}
