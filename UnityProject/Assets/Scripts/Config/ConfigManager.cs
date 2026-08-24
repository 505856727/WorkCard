using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WorkCard.Config
{
    public interface IConfigTable
    {
        string Name { get; }
        void Load(byte[] data);
        void Unload();
    }

    public static class ConfigManager
    {
        static readonly Dictionary<Type, IConfigTable> ByType = new Dictionary<Type, IConfigTable>();
        static readonly Dictionary<string, IConfigTable> ByName = new Dictionary<string, IConfigTable>();

        public static void LoadAll(string directory = null)
        {
            UnloadAll();
            foreach (var (type, attr) in Collect())
            {
                var table = CreateTable(type, attr);
                var data = ReadBytes(attr.Name, directory);
                if (data == null)
                {
                    Debug.LogError($"未找到配置表（{attr.Name}.bytes），请先在配置编辑器中导出");
                    continue;
                }

                table.Load(data);
                ByType[type] = table;
                ByName[attr.Name] = table;
            }
        }

        public static void UnloadAll()
        {
            foreach (var table in ByType.Values)
            {
                table.Unload();
            }

            ByType.Clear();
            ByName.Clear();
        }

        public static TTable Get<TItem, TTable>()
            where TItem : IConfigItem, new()
            where TTable : class, IConfigTable
        {
            return ByType.TryGetValue(typeof(TItem), out var table) ? table as TTable : null;
        }

        public static ConfigMap<T> GetMap<T>() where T : IConfigItem, new() => Get<T, ConfigMap<T>>();

        public static ConfigList<T> GetList<T>() where T : IConfigItem, new() => Get<T, ConfigList<T>>();

        public static ConfigGroup<T> GetGroup<T>() where T : IConfigItem, new() => Get<T, ConfigGroup<T>>();

        public static ConfigGroupList<T> GetGroupList<T>() where T : IConfigItem, new() => Get<T, ConfigGroupList<T>>();

        public static ConfigGroupMap<T> GetGroupMap<T>() where T : IConfigItem, new() => Get<T, ConfigGroupMap<T>>();

        public static ConfigMapList<T> GetMapList<T>() where T : IConfigItem, new() => Get<T, ConfigMapList<T>>();

        public static IConfigTable Get(string name)
        {
            ByName.TryGetValue(name, out var table);
            return table;
        }

        static IConfigTable CreateTable(Type type, ConfigAttribute attr)
        {
            if (typeof(IConfigTable).IsAssignableFrom(type))
            {
                return (IConfigTable)Activator.CreateInstance(type);
            }

            switch (attr.Kind)
            {
                case ConfigKind.List:
                    return Create(typeof(ConfigList<>), type, attr.Name, attr.IndexFromOne);
                case ConfigKind.Group:
                    return Create(typeof(ConfigGroup<>), type, attr.Name, attr.GroupKey ?? "");
                case ConfigKind.MapList:
                    return Create(typeof(ConfigMapList<>), type, attr.Name, attr.IndexFromOne);
                case ConfigKind.GroupList:
                    return Create(typeof(ConfigGroupList<>), type, attr.Name, attr.GroupKey ?? "", attr.IndexFromOne);
                case ConfigKind.GroupMap:
                    return Create(typeof(ConfigGroupMap<>), type, attr.Name, attr.GroupKey ?? "");
                default:
                    return Create(typeof(ConfigMap<>), type, attr.Name);
            }
        }

        static IConfigTable Create(Type generic, Type itemType, params object[] args)
        {
            var tableType = generic.MakeGenericType(itemType);
            return (IConfigTable)Activator.CreateInstance(tableType, args);
        }

        static byte[] ReadBytes(string name, string directory)
        {
            if (!string.IsNullOrEmpty(directory))
            {
                var file = Path.Combine(directory, name + ".bytes");
                if (File.Exists(file))
                {
                    return File.ReadAllBytes(file);
                }
            }

            var fromResources = Resources.Load<TextAsset>("Config/" + name);
            if (fromResources != null)
            {
                return fromResources.bytes;
            }

            var fromAssets = Path.Combine(Application.dataPath, "Config", name + ".bytes");
            return File.Exists(fromAssets) ? File.ReadAllBytes(fromAssets) : null;
        }

        public static List<(Type type, ConfigAttribute attr)> Collect()
        {
            var result = new List<(Type, ConfigAttribute)>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name.StartsWith("Unity") || name.StartsWith("System") || name.StartsWith("mscorlib")
                    || name.StartsWith("FairyGUI") || name == "netstandard")
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract)
                    {
                        continue;
                    }

                    var attr = type.GetCustomAttribute<ConfigAttribute>();
                    if (attr != null)
                    {
                        result.Add((type, attr));
                    }
                }
            }

            return result;
        }
    }
}
