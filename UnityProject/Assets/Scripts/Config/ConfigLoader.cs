using System;
using System.Reflection;
using UnityEngine;

namespace WorkCard.Config
{
    public class ConfigLoader
    {
        public const int Version = 3;

        protected readonly BufferReader Reader = new BufferReader();
        protected ConfigHeader[] Headers = Array.Empty<ConfigHeader>();
        int _version;
        int _itemCount;

        public int version => _version;
        public int itemCount => _itemCount;

        public bool Initialize(byte[] data)
        {
            Reader.SetBuffer(data);
            _version = Reader.ReadSByte();
            if (_version != Version)
            {
                throw new Exception($"当前版本号：{Version} 与配置表版本号（{_version}）不一致，请重新导出");
            }

            Reader.ReadStringList();
            var objectHeaderCount = Reader.ReadSByte();
            if (objectHeaderCount > 0)
            {
                throw new Exception("暂不支持 object 类型配置，请把嵌套对象拆成独立字段");
            }

            Headers = ReadHeaders();
            _itemCount = Reader.ReadInt32();
            return true;
        }

        public void Reset()
        {
            Headers = Array.Empty<ConfigHeader>();
            Reader.SetBuffer(null);
        }

        public void LoadItem(object configItem)
        {
            var type = configItem.GetType();
            foreach (var header in Headers)
            {
                var value = Reader.ReadValue(header.Type, header.ElemType);
                SetMember(type, configItem, header.Name, value);
            }

            if (configItem is IConfigItem item)
            {
                item.OnLoad();
            }
        }

        ConfigHeader[] ReadHeaders()
        {
            var count = Reader.ReadUByte();
            var headers = new ConfigHeader[count];
            for (var i = 0; i < count; i++)
            {
                headers[i] = new ConfigHeader(Reader);
            }

            return headers;
        }

        static void SetMember(Type type, object target, string name, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(name, flags);
            if (field != null)
            {
                field.SetValue(target, ConvertValue(value, field.FieldType));
                return;
            }

            var prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, ConvertValue(value, prop.PropertyType));
                return;
            }

            Debug.LogWarning($"配置类（{type.Name}）没有字段（{name}）");
        }

        static object ConvertValue(object value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType == typeof(int) || targetType == typeof(short) || targetType == typeof(sbyte))
            {
                return Convert.ChangeType(Convert.ToInt32(value), targetType);
            }

            if (targetType == typeof(uint) || targetType == typeof(ushort) || targetType == typeof(byte))
            {
                return Convert.ChangeType(Convert.ToUInt32(value), targetType);
            }

            if (targetType == typeof(float) || targetType == typeof(double))
            {
                return Convert.ChangeType(Convert.ToSingle(value), targetType);
            }

            if (targetType == typeof(bool))
            {
                return Convert.ToBoolean(value);
            }

            if (targetType == typeof(string))
            {
                return value.ToString();
            }

            return value;
        }
    }
}
