using System;
using System.Collections.Generic;
using System.Reflection;

namespace WorkCard.Config
{
    static class ConfigItemAccess
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Dictionary<(Type type, string name), MemberInfo> Cache = new Dictionary<(Type, string), MemberInfo>();

        public static int GetId<T>(T item)
        {
            if (item is IConfigItemWithId withId)
            {
                return withId.id;
            }

            return GetInt(item, "id");
        }

        public static int GetGroupId<T>(T item, string groupKey)
        {
            if (!string.IsNullOrEmpty(groupKey))
            {
                return GetInt(item, groupKey);
            }

            if (item is IConfigGroupItem groupItem)
            {
                return groupItem.groupId;
            }

            throw new Exception($"配置类（{typeof(T).Name}）未设置 GroupKey，也没有 groupId");
        }

        public static int GetInt(object item, string name)
        {
            if (item == null || string.IsNullOrEmpty(name))
            {
                throw new Exception("无法读取配置字段：" + name);
            }

            var type = item.GetType();
            var key = (type, name);
            if (!Cache.TryGetValue(key, out var member))
            {
                member = (MemberInfo)type.GetField(name, Flags) ?? type.GetProperty(name, Flags);
                Cache[key] = member;
            }

            if (member is FieldInfo field)
            {
                return Convert.ToInt32(field.GetValue(item));
            }

            if (member is PropertyInfo prop)
            {
                return Convert.ToInt32(prop.GetValue(item));
            }

            throw new Exception($"配置类（{type.Name}）需要字段（{name}）");
        }
    }
}
