using System;
using System.Collections.Generic;
using System.IO;
using WorkCard.LitJson;
using WorkCard.UI;
using UnityEngine;

namespace WorkCard.Editor
{
    [Serializable]
    public class UIPropEntry
    {
        public string Name;
        public int Type;
        public string NamePath;
        public string IdPath;
        public string Click;
        public List<UIPropEntry> Elements;
    }

    [Serializable]
    public class UIPropConfigFile
    {
        public string Version = "0.0.3";
        public List<UIPropEntry> Props = new List<UIPropEntry>();
    }

    public class UIPropConfig
    {
        public const string ConfigVersion = "0.0.3";
        public string File;
        public UIPropConfigFile Data = new UIPropConfigFile();
        public readonly Dictionary<string, UIPropEntry> Props = new Dictionary<string, UIPropEntry>();

        List<UIExportPropNode> _propsNodes;
        List<UIExportPropNode> _propsArrayNodes;

        public static UIPropConfig Load(string file)
        {
            var config = new UIPropConfig { File = file };
            if (System.IO.File.Exists(file))
            {
                try
                {
                    config.Data = JsonMapper.ToObject<UIPropConfigFile>(System.IO.File.ReadAllText(file))
                                  ?? new UIPropConfigFile();
                }
                catch
                {
                    config.Data = new UIPropConfigFile();
                }
            }

            foreach (var entry in config.Data.Props)
            {
                config.Props[entry.Name] = entry;
            }

            return config;
        }

        public void Save()
        {
            Data.Version = ConfigVersion;
            Data.Props = new List<UIPropEntry>(Props.Values);
            var dir = Path.GetDirectoryName(File);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var writer = new JsonWriter { PrettyPrint = true };
            JsonMapper.ToJson(Data, writer);
            System.IO.File.WriteAllText(File, writer.ToString());
        }

        public UIPropEntry GetOrCreate(string name, UIPropType type)
        {
            if (!Props.TryGetValue(name, out var entry) || entry.Type != (int)type)
            {
                entry = new UIPropEntry { Name = name, Type = (int)type };
                Props[name] = entry;
            }

            return entry;
        }

        public void Export(JsonData uiConfigs, ComponentData componentData, List<UIPropInfo> props)
        {
            _propsNodes = new List<UIExportPropNode>();
            _propsArrayNodes = new List<UIExportPropNode>();
            var exportInfos = new JsonData();
            exportInfos.SetJsonType(JsonType.Array);
            var byType = new Dictionary<int, JsonData>();
            var hasProps = false;

            foreach (var prop in props)
            {
                if (!Props.TryGetValue(prop.Name, out var config))
                {
                    continue;
                }

                hasProps = true;
                var propType = config.Type;
                if (propType == (int)UIPropType.Node)
                {
                    if ((config.IdPath ?? "").Contains("$"))
                    {
                        propType = (int)UIPropType.Transition;
                    }
                    else if ((config.IdPath ?? "").Contains("@"))
                    {
                        propType = (int)UIPropType.Controller;
                    }
                }

                if (!byType.TryGetValue(propType, out var infos))
                {
                    infos = new JsonData();
                    infos.SetJsonType(JsonType.Array);
                    byType[propType] = infos;
                }

                switch ((UIPropType)propType)
                {
                    case UIPropType.Node:
                        GenNode(componentData, infos, config, prop);
                        break;
                    case UIPropType.ArrayNode:
                        GenArrayNode(componentData, infos, config, prop);
                        break;
                    case UIPropType.Controller:
                        GenController(componentData, infos, config, prop);
                        break;
                    case UIPropType.Transition:
                        GenTransition(componentData, infos, config, prop);
                        break;
                    default:
                        break;
                }
            }

            var types = new List<int>(byType.Keys);
            types.Sort();
            foreach (var type in types)
            {
                exportInfos.Add(type);
                exportInfos.Add(byType[type]);
            }

            if (!hasProps)
            {
                return;
            }

            AddClicks(exportInfos);

            JsonData packageConfigs;
            if (uiConfigs.ContainsKey(componentData.Package.Name))
            {
                packageConfigs = uiConfigs[componentData.Package.Name];
            }
            else
            {
                packageConfigs = new JsonData();
                packageConfigs.SetJsonType(JsonType.Object);
                uiConfigs[componentData.Package.Name] = packageConfigs;
            }

            packageConfigs[componentData.FileName] = exportInfos;
        }

        void AddClicks(JsonData exportInfos)
        {
            var clicks = new JsonData();
            clicks.SetJsonType(JsonType.Array);
            var has = false;
            for (var i = 0; i < _propsNodes.Count; i++)
            {
                var n = _propsNodes[i];
                if (n == null || string.IsNullOrEmpty(n.Click))
                {
                    continue;
                }

                has = true;
                clicks.Add(i);
                clicks.Add(n.Click);
            }

            for (var i = 0; i < _propsArrayNodes.Count; i++)
            {
                var n = _propsArrayNodes[i];
                if (n == null || string.IsNullOrEmpty(n.Click))
                {
                    continue;
                }

                has = true;
                clicks.Add(i + _propsNodes.Count);
                clicks.Add(n.Click);
            }

            if (!has)
            {
                return;
            }

            exportInfos.Add((int)UIPropType.OnClick);
            exportInfos.Add(clicks);
        }

        void GenNode(ComponentData componentData, JsonData infos, UIPropEntry config, UIPropInfo prop)
        {
            var indices = componentData.GetChildIndices(config.IdPath);
            if (indices == null)
            {
                Debug.LogWarning($"（{componentData.Package.Name}: {componentData.FileName}）未配置属性（{prop.Name}），建议去掉 [UIProp]");
                return;
            }

            infos.Add(prop.Name);
            infos.Add(indices.Count);
            foreach (var index in indices)
            {
                infos.Add(index);
            }

            _propsNodes.Add(string.IsNullOrEmpty(config.Click)
                ? null
                : new UIExportPropNode { Index = _propsNodes.Count, Click = config.Click });
        }

        void GenArrayNode(ComponentData componentData, JsonData infos, UIPropEntry config, UIPropInfo prop)
        {
            var elements = config.Elements ?? new List<UIPropEntry>();
            infos.Add(prop.Name);
            infos.Add(elements.Count);
            foreach (var elem in elements)
            {
                var indices = componentData.GetChildIndices(elem.IdPath);
                if (indices == null)
                {
                    infos.Add(0);
                    _propsArrayNodes.Add(null);
                    continue;
                }

                infos.Add(indices.Count);
                foreach (var index in indices)
                {
                    infos.Add(index);
                }

                _propsArrayNodes.Add(string.IsNullOrEmpty(elem.Click)
                    ? null
                    : new UIExportPropNode { Index = _propsArrayNodes.Count, Click = elem.Click });
            }
        }

        static void GenController(ComponentData componentData, JsonData infos, UIPropEntry config, UIPropInfo prop)
        {
            GenCtrlOrTrans(componentData, infos, config, prop, '@',
                (data, name) => data.GetControllerIndex(name), "Controller");
        }

        static void GenTransition(ComponentData componentData, JsonData infos, UIPropEntry config, UIPropInfo prop)
        {
            GenCtrlOrTrans(componentData, infos, config, prop, '$',
                (data, name) => data.GetTransitionIndex(name), "Transition");
        }

        static void GenCtrlOrTrans(ComponentData componentData, JsonData infos, UIPropEntry config, UIPropInfo prop,
            char sep, Func<ComponentData, string, int> getIndex, string kind)
        {
            var indices = new List<int>();
            var index = -1;
            var idPath = config.IdPath ?? "";
            var paths = idPath.Split(sep);
            if (paths.Length == 2)
            {
                if (paths[0] == "")
                {
                    index = getIndex(componentData, paths[1]);
                }
                else
                {
                    indices = componentData.GetChildIndices(paths[0]) ?? new List<int>();
                    var node = componentData.FindChildById(paths[0]) as ComponentData;
                    index = node != null ? getIndex(node, paths[1]) : -1;
                }
            }

            if (index == -1)
            {
                Debug.LogWarning($"（{componentData.Package.Name}: {componentData.FileName}）属性（{prop.Name}: {kind}）未找到，{idPath}");
                return;
            }

            infos.Add(prop.Name);
            infos.Add(indices.Count);
            foreach (var idx in indices)
            {
                infos.Add(idx);
            }

            infos.Add(index);
        }

        class UIExportPropNode
        {
            public int Index;
            public string Click;
        }
    }
}
