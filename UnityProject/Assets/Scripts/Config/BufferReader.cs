using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorkCard.Config
{
    public class BufferReader
    {
        byte[] _bytes;
        int _length;
        int _pos;
        bool _littleEndian;
        string[] _strings = Array.Empty<string>();

        public int Pos
        {
            get => _pos;
            set => _pos = value;
        }

        public void SetBuffer(byte[] buffer, bool littleEndian = false)
        {
            _bytes = buffer;
            _length = buffer != null ? buffer.Length : 0;
            _pos = 0;
            _littleEndian = littleEndian;
            _strings = Array.Empty<string>();
        }

        public ValueType ReadType() => (ValueType)ReadUByte();

        public bool ReadBool()
        {
            Validate(1);
            return _bytes[_pos++] != 0;
        }

        public sbyte ReadSByte()
        {
            Validate(1);
            return (sbyte)_bytes[_pos++];
        }

        public byte ReadUByte()
        {
            Validate(1);
            return _bytes[_pos++];
        }

        public short ReadInt16()
        {
            Validate(2);
            var value = _littleEndian
                ? (short)(_bytes[_pos] | (_bytes[_pos + 1] << 8))
                : (short)((_bytes[_pos] << 8) | _bytes[_pos + 1]);
            _pos += 2;
            return value;
        }

        public ushort ReadUInt16() => (ushort)ReadInt16();

        public int ReadInt32()
        {
            Validate(4);
            int value;
            if (_littleEndian)
            {
                value = _bytes[_pos] | (_bytes[_pos + 1] << 8) | (_bytes[_pos + 2] << 16) | (_bytes[_pos + 3] << 24);
            }
            else
            {
                value = (_bytes[_pos] << 24) | (_bytes[_pos + 1] << 16) | (_bytes[_pos + 2] << 8) | _bytes[_pos + 3];
            }

            _pos += 4;
            return value;
        }

        public uint ReadUInt32() => (uint)ReadInt32();

        public float ReadFloat()
        {
            Validate(4);
            var raw = new byte[4];
            if (_littleEndian)
            {
                Array.Copy(_bytes, _pos, raw, 0, 4);
            }
            else
            {
                raw[0] = _bytes[_pos + 3];
                raw[1] = _bytes[_pos + 2];
                raw[2] = _bytes[_pos + 1];
                raw[3] = _bytes[_pos];
            }

            _pos += 4;
            return BitConverter.ToSingle(raw, 0);
        }

        public double ReadDouble()
        {
            Validate(8);
            var raw = new byte[8];
            if (_littleEndian)
            {
                Array.Copy(_bytes, _pos, raw, 0, 8);
            }
            else
            {
                for (var i = 0; i < 8; i++)
                {
                    raw[i] = _bytes[_pos + 7 - i];
                }
            }

            _pos += 8;
            return BitConverter.ToDouble(raw, 0);
        }

        public string ReadString()
        {
            var byteLen = ReadUInt16();
            Validate(byteLen);
            var value = Encoding.UTF8.GetString(_bytes, _pos, byteLen);
            _pos += byteLen;
            return value;
        }

        public string ReadIndexString()
        {
            var index = ReadUInt16();
            return index < _strings.Length ? _strings[index] : "";
        }

        public void ReadStringList()
        {
            if (ReadUByte() == 0)
            {
                _strings = Array.Empty<string>();
                return;
            }

            var count = ReadUInt16();
            _strings = new string[count];
            for (var i = 0; i < count; i++)
            {
                _strings[i] = ReadString();
            }
        }

        public Array ReadArray1(ValueType elemType)
        {
            var len = ReadUByte();
            var array = CreateArray(elemType, len);
            for (var i = 0; i < len; i++)
            {
                array.SetValue(ReadValue(elemType), i);
            }

            return array;
        }

        public Array ReadArray2(ValueType elemType)
        {
            var len = ReadUByte();
            var array = Array.CreateInstance(GetArrayType(elemType), len);
            for (var i = 0; i < len; i++)
            {
                array.SetValue(ReadArray1(elemType), i);
            }

            return array;
        }

        public byte[] ReadBuffer()
        {
            var len = ReadUInt16();
            Validate(len);
            var data = new byte[len];
            Array.Copy(_bytes, _pos, data, 0, len);
            _pos += len;
            return data;
        }

        public Vector2 ReadVec2() => new Vector2(ReadFloat(), ReadFloat());

        public Vector3 ReadVec3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());

        public object ReadValue(ValueType type, ValueType elemType = ValueType.Null)
        {
            switch (type)
            {
                case ValueType.Bool: return ReadBool();
                case ValueType.SByte: return (int)ReadSByte();
                case ValueType.UByte: return (int)ReadUByte();
                case ValueType.Int16: return (int)ReadInt16();
                case ValueType.UInt16: return (int)ReadUInt16();
                case ValueType.Int: return ReadInt32();
                case ValueType.UInt: return (int)ReadUInt32();
                case ValueType.Float: return ReadFloat();
                case ValueType.Double: return (float)ReadDouble();
                case ValueType.String:
                case ValueType.Text:
                case ValueType.Asset: return ReadString();
                case ValueType.IndexString:
                case ValueType.IndexText: return ReadIndexString();
                case ValueType.Array1: return ReadArray1(elemType);
                case ValueType.Array2: return ReadArray2(elemType);
                case ValueType.Vec2: return ReadVec2();
                case ValueType.Vec3: return ReadVec3();
                case ValueType.Size: return ReadVec2();
                case ValueType.Color: return ReadUInt32();
                case ValueType.Time: return (int)ReadUInt16();
                case ValueType.Buffer: return ReadBuffer();
                default: return null;
            }
        }

        static Array CreateArray(ValueType elemType, int len)
        {
            switch (elemType)
            {
                case ValueType.Bool: return new bool[len];
                case ValueType.Float:
                case ValueType.Double: return new float[len];
                case ValueType.String:
                case ValueType.IndexString:
                case ValueType.Text:
                case ValueType.IndexText:
                case ValueType.Asset: return new string[len];
                default: return new int[len];
            }
        }

        static Type GetArrayType(ValueType elemType)
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

        void Validate(int size)
        {
            if (_bytes == null || _pos + size > _length)
            {
                throw new Exception($"配置读取越界：{_pos + size} > {_length}");
            }
        }
    }
}
