using System;
using System.Collections.Generic;
using System.Reflection;
using FairyGUI;

namespace WorkCard.UI
{
    public class UIResInfo
    {
        public Type Type;
        public string Group;
        public string Package;
        public string Component;
        public bool IsWindow;
        public List<UIPropInfo> Props = new List<UIPropInfo>();
        public List<string> Functions = new List<string>();
    }

    public class UIPropInfo
    {
        public string Name;
        public UIPropType Type;
        public FieldInfo Field;
    }

    public static class UIRegistry
    {
        public static readonly Dictionary<Type, UIResInfo> ByType = new Dictionary<Type, UIResInfo>();
        public static readonly Dictionary<string, UIResInfo> ByClassName = new Dictionary<string, UIResInfo>();

        public static void Collect()
        {
            ByType.Clear();
            ByClassName.Clear();
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

                    var windowAttr = type.GetCustomAttribute<UIClassAttribute>();
                    var comAttr = type.GetCustomAttribute<UIComAttribute>();
                    if (windowAttr == null && comAttr == null)
                    {
                        continue;
                    }

                    var info = new UIResInfo { Type = type, IsWindow = windowAttr != null };
                    if (windowAttr != null)
                    {
                        info.Group = windowAttr.Group;
                        info.Package = windowAttr.Package;
                        info.Component = windowAttr.Component;
                    }
                    else
                    {
                        info.Package = comAttr.Package;
                        info.Component = comAttr.Component;
                    }

                    CollectMembers(type, info);
                    ByType[type] = info;
                    ByClassName[type.Name] = info;
                }
            }
        }

        static void CollectMembers(Type type, UIResInfo info)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(flags))
                {
                    var attr = field.GetCustomAttribute<UIPropAttribute>();
                    if (attr == null || attr.Type == UIPropType.Ignore)
                    {
                        continue;
                    }

                    info.Props.Add(new UIPropInfo
                    {
                        Name = field.Name,
                        Type = attr.Type,
                        Field = field,
                    });
                }

                foreach (var method in t.GetMethods(flags))
                {
                    if (method.GetParameters().Length != 0 || method.IsSpecialName)
                    {
                        continue;
                    }

                    info.Functions.Add(method.Name);
                }
            }
        }

        public static UIResInfo Get(Type type)
        {
            ByType.TryGetValue(type, out var info);
            return info;
        }
    }
}
