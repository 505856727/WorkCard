using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using WorkCard.LitJson;
using UnityEngine;

namespace WorkCard.UI
{
    public static class UIConfig
    {
        public const int Version = 1;
        public static JsonData PropertyConfig;

        public static void Load(string json)
        {
            PropertyConfig = JsonMapper.ToObject(json);
        }

        public static void LoadFromResources(string path = "UI/ui_config")
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset != null)
            {
                Load(asset.text);
                return;
            }

            LoadFromFile();
        }

        public static void LoadFromFile()
        {
            var file = System.IO.Path.Combine(Application.dataPath, "UI/UI/ui_config.json");
            if (!System.IO.File.Exists(file))
            {
                Debug.LogWarning("[UIConfig] 未找到 ui_config.json，请先在 UI 编辑器中导出");
                return;
            }

            Load(System.IO.File.ReadAllText(file));
        }
    }

    public static class UIClassHelper
    {
        static readonly List<GObject> PropNodes = new List<GObject>();

        public static void InitProperties(GComponent root, object target)
        {
            if (root == null || target == null || UIConfig.PropertyConfig == null)
            {
                return;
            }

            var info = UIRegistry.Get(target.GetType());
            if (info == null)
            {
                return;
            }

            var config = UIConfig.PropertyConfig;
            if (config.ContainsKey("version") && (int)config["version"] != UIConfig.Version)
            {
                Debug.LogError("ui_config 版本不匹配，请重新导出");
                return;
            }

            if (!config.ContainsKey(info.Package))
            {
                return;
            }

            var pkg = config[info.Package];
            if (!pkg.ContainsKey(info.Component))
            {
                return;
            }

            var properties = pkg[info.Component];
            PropNodes.Clear();
            for (int i = 0, l = properties.Count; i < l; i += 2)
            {
                var propType = (UIPropType)(int)properties[i];
                var props = properties[i + 1];
                switch (propType)
                {
                    case UIPropType.Node:
                        SetNode(root, target, props);
                        break;
                    case UIPropType.Controller:
                        SetController(root, target, props);
                        break;
                    case UIPropType.Transition:
                        SetTransition(root, target, props);
                        break;
                    case UIPropType.ArrayNode:
                        SetArrayNode(root, target, props);
                        break;
                    case UIPropType.OnClick:
                        SetOnClick(target, props);
                        break;
                    case UIPropType.ArrayController:
                        SetArrayCtrlOrTrans(root, target, props, true);
                        break;
                    case UIPropType.ArrayTransition:
                        SetArrayCtrlOrTrans(root, target, props, false);
                        break;
                }
            }

            PropNodes.Clear();
        }

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        static GObject Walk(GComponent root, JsonData props, ref int index, int endIndex, string propName, object target)
        {
            GObject node = root;
            while (++index <= endIndex)
            {
                var parent = node as GComponent;
                if (parent == null)
                {
                    Debug.LogWarning($"无法对 UI 类（{target.GetType().Name}）属性（{propName}）赋值，请检查节点配置");
                    return null;
                }

                node = parent.GetChildAt((int)props[index]);
                if (node == null)
                {
                    Debug.LogWarning($"无法对 UI 类（{target.GetType().Name}）属性（{propName}）赋值，请检查节点配置");
                    return null;
                }
            }

            return node;
        }

        static void SetNode(GComponent root, object target, JsonData props)
        {
            int index = 0;
            int count = props.Count;
            while (index < count)
            {
                var propName = (string)props[index++];
                var endIndex = index + (int)props[index];
                var node = Walk(root, props, ref index, endIndex, propName, target);
                PropNodes.Add(node);
                SetField(target, propName, node);
            }
        }

        static void SetArrayNode(GComponent root, object target, JsonData props)
        {
            int index = 0;
            int count = props.Count;
            while (index < count)
            {
                var propName = (string)props[index];
                var arrayLength = (int)props[index + 1];
                index += 2;
                var field = target.GetType().GetField(propName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var elemType = field?.FieldType.GetElementType() ?? typeof(GObject);
                var array = System.Array.CreateInstance(elemType, arrayLength);
                for (int i = 0; i < arrayLength; ++i)
                {
                    var endIndex = index + (int)props[index];
                    var node = Walk(root, props, ref index, endIndex, propName, target);
                    PropNodes.Add(node);
                    array.SetValue(node, i);
                }

                if (field != null)
                {
                    field.SetValue(target, array);
                }
            }
        }

        static void SetArrayCtrlOrTrans(GComponent root, object target, JsonData props, bool controller)
        {
            int index = 0;
            int count = props.Count;
            while (index < count)
            {
                var propName = (string)props[index];
                var arrayLength = (int)props[index + 1];
                index += 2;
                var field = target.GetType().GetField(propName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var elemType = field?.FieldType.GetElementType() ?? typeof(object);
                var array = System.Array.CreateInstance(elemType, arrayLength);
                for (int i = 0; i < arrayLength; ++i)
                {
                    var endIndex = index + (int)props[index];
                    var node = Walk(root, props, ref index, endIndex, propName, target) as GComponent;
                    object value = null;
                    if (node != null)
                    {
                        var at = (int)props[index++];
                        value = controller ? (object)node.GetControllerAt(at) : node.GetTransitionAt(at);
                    }

                    PropNodes.Add(node);
                    array.SetValue(value, i);
                }

                if (field != null)
                {
                    field.SetValue(target, array);
                }
            }
        }

        static void SetController(GComponent root, object target, JsonData props)
        {
            int index = 0;
            int count = props.Count;
            while (index < count)
            {
                var propName = (string)props[index++];
                var endIndex = index + (int)props[index];
                var node = Walk(root, props, ref index, endIndex, propName, target) as GComponent;
                if (node != null)
                {
                    SetField(target, propName, node.GetControllerAt((int)props[index++]));
                }
            }
        }

        static void SetTransition(GComponent root, object target, JsonData props)
        {
            int index = 0;
            int count = props.Count;
            while (index < count)
            {
                var propName = (string)props[index++];
                var endIndex = index + (int)props[index];
                var node = Walk(root, props, ref index, endIndex, propName, target) as GComponent;
                if (node != null)
                {
                    SetField(target, propName, node.GetTransitionAt((int)props[index++]));
                }
            }
        }

        static void SetOnClick(object target, JsonData props)
        {
            for (int i = 0; i < props.Count; i += 2)
            {
                var nodeIndex = (int)props[i];
                var funcName = (string)props[i + 1];
                if (nodeIndex < 0 || nodeIndex >= PropNodes.Count)
                {
                    continue;
                }

                var node = PropNodes[nodeIndex];
                var method = target.GetType().GetMethod(funcName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (node == null || method == null)
                {
                    continue;
                }

                node.onClick.Add(() => method.Invoke(target, null));
            }
        }
    }

    public class UIComponent : GComponent
    {
        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            UIClassHelper.InitProperties(this, this);
            OnInit();
        }

        protected virtual void OnInit()
        {
        }
    }
}
