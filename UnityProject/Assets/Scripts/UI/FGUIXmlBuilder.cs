using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using FairyGUI;
using UnityEngine;

namespace WorkCard.UI
{
    /// <summary>
    /// 在尚未发布 *_fui.bytes 时，直接从 FGUI 工程 XML 构建组件，便于示例在 Editor 里跑通。
    /// 发布后 WindowBase 会优先走 UIPackage.CreateObject。
    /// </summary>
    public static class FGUIXmlBuilder
    {
        static string _projectPath;

        public static GObject Create(string packageName, string componentName)
        {
            var projectPath = GetFGUIProjectPath();
            if (string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            var packageDir = Path.Combine(projectPath, "assets", packageName);
            var packageXml = Path.Combine(packageDir, "package.xml");
            if (!File.Exists(packageXml))
            {
                return null;
            }

            var doc = LoadXml(packageXml);
            var resources = doc.SelectNodes("/packageDescription/resources/component");
            if (resources == null)
            {
                return null;
            }

            var byId = new Dictionary<string, XmlElement>();
            var byName = new Dictionary<string, XmlElement>();
            foreach (XmlElement node in resources)
            {
                var fileName = Path.GetFileNameWithoutExtension(node.GetAttribute("name"));
                byId[node.GetAttribute("id")] = node;
                byName[fileName] = node;
            }

            if (!byName.TryGetValue(componentName, out var item))
            {
                return null;
            }

            return BuildComponent(packageDir, item, byId, null);
        }

        static GComponent BuildComponent(string packageDir, XmlElement item, Dictionary<string, XmlElement> byId, XmlElement instance)
        {
            var relPath = (item.GetAttribute("path") + item.GetAttribute("name")).Replace('\\', '/');
            var xmlPath = Path.Combine(packageDir, relPath.TrimStart('/'));
            if (!File.Exists(xmlPath))
            {
                return null;
            }

            var doc = LoadXml(xmlPath);
            var root = doc.DocumentElement;
            var isButton = string.Equals(root.GetAttribute("extention"), "Button", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(root.GetAttribute("extension"), "Button", StringComparison.OrdinalIgnoreCase);
            GComponent com = isButton ? new GButton() : new GComponent();
            com.gameObjectName = Path.GetFileNameWithoutExtension(item.GetAttribute("name"));

            var size = ParseVec2(root.GetAttribute("size"), new Vector2(100, 100));
            com.SetSize(size.x, size.y);
            ApplyPivot(com, root.GetAttribute("pivot"));

            foreach (XmlNode ctrlNode in root.SelectNodes("controller"))
            {
                var ctrl = ctrlNode as XmlElement;
                if (ctrl == null)
                {
                    continue;
                }

                var controller = BuildController(ctrl);
                com.AddController(controller);
                var selected = ParseInt(ctrl.GetAttribute("selected"), 0);
                if (selected >= 0 && selected < controller.pageCount)
                {
                    controller.selectedIndex = selected;
                }
            }

            var children = new List<GObject>();
            var byXmlId = new Dictionary<string, GObject>();
            var display = root.SelectSingleNode("displayList");
            if (display != null)
            {
                foreach (XmlNode childNode in display.ChildNodes)
                {
                    var child = childNode as XmlElement;
                    if (child == null)
                    {
                        continue;
                    }

                    var obj = BuildChild(packageDir, child, byId);
                    if (obj == null)
                    {
                        continue;
                    }

                    com.AddChild(obj);
                    children.Add(obj);
                    var xmlId = child.GetAttribute("id");
                    if (!string.IsNullOrEmpty(xmlId))
                    {
                        byXmlId[xmlId] = obj;
                    }
                }

                var index = 0;
                foreach (XmlNode childNode in display.ChildNodes)
                {
                    var child = childNode as XmlElement;
                    if (child == null)
                    {
                        continue;
                    }

                    if (index < children.Count)
                    {
                        ApplyRelations(children[index], com, byXmlId, child.SelectSingleNode("relation") as XmlElement);
                    }

                    index++;
                }
            }

            if (instance != null)
            {
                ApplyInstance(com, instance);
            }

            return com;
        }

        static GObject BuildChild(string packageDir, XmlElement node, Dictionary<string, XmlElement> byId)
        {
            switch (node.Name)
            {
                case "graph":
                    return BuildGraph(node);
                case "text":
                    return BuildText(node);
                case "component":
                    return BuildNested(packageDir, node, byId);
                default:
                    return null;
            }
        }

        static GGraph BuildGraph(XmlElement node)
        {
            var graph = new GGraph();
            ApplyObject(graph, node);
            var size = graph.size;
            var fill = ParseColor(node.GetAttribute("fillColor"), Color.white);
            var lineSize = ParseInt(node.GetAttribute("lineSize"), 0);
            graph.DrawRect(size.x, size.y, lineSize, Color.clear, fill);
            return graph;
        }

        static GTextField BuildText(XmlElement node)
        {
            var text = new GTextField();
            ApplyObject(text, node);
            var format = text.textFormat;
            format.size = ParseInt(node.GetAttribute("fontSize"), 24);
            format.color = ParseColor(node.GetAttribute("color"), Color.black);
            text.textFormat = format;
            text.align = ParseAlign(node.GetAttribute("align"));
            text.verticalAlign = ParseVAlign(node.GetAttribute("vAlign"));
            if (node.GetAttribute("autoSize") == "none")
            {
                text.autoSize = AutoSizeType.None;
            }

            text.text = node.GetAttribute("text") ?? "";
            return text;
        }

        static GObject BuildNested(string packageDir, XmlElement node, Dictionary<string, XmlElement> byId)
        {
            var src = node.GetAttribute("src");
            if (string.IsNullOrEmpty(src) || !byId.TryGetValue(src, out var item))
            {
                return null;
            }

            var com = BuildComponent(packageDir, item, byId, node);
            if (com == null)
            {
                return null;
            }

            ApplyObject(com, node);
            return com;
        }

        static void ApplyInstance(GComponent com, XmlElement instance)
        {
            var button = instance.SelectSingleNode("Button") as XmlElement;
            if (button == null)
            {
                return;
            }

            var title = button.GetAttribute("title");
            if (string.IsNullOrEmpty(title))
            {
                return;
            }

            if (com is GButton gButton)
            {
                var titleObj = com.GetChild("title");
                if (titleObj != null)
                {
                    titleObj.text = title;
                }

                gButton.title = title;
            }
        }

        static void ApplyObject(GObject obj, XmlElement node)
        {
            obj.name = node.GetAttribute("name");
            var xy = ParseVec2(node.GetAttribute("xy"), Vector2.zero);
            obj.SetXY(xy.x, xy.y);
            if (!string.IsNullOrEmpty(node.GetAttribute("size")))
            {
                var size = ParseVec2(node.GetAttribute("size"), obj.size);
                obj.SetSize(size.x, size.y);
            }

            ApplyPivot(obj, node.GetAttribute("pivot"));
        }

        static void ApplyPivot(GObject obj, string pivot)
        {
            if (string.IsNullOrEmpty(pivot))
            {
                return;
            }

            var v = ParseVec2(pivot, new Vector2(0, 0));
            obj.SetPivot(v.x, v.y, true);
        }

        static void ApplyRelations(GObject obj, GComponent parent, Dictionary<string, GObject> byXmlId, XmlElement relation)
        {
            if (relation == null)
            {
                return;
            }

            var targetAttr = relation.GetAttribute("target");
            GObject target = parent;
            if (!string.IsNullOrEmpty(targetAttr) && byXmlId.TryGetValue(targetAttr, out var found))
            {
                target = found;
            }

            var pairs = (relation.GetAttribute("sidePair") ?? "").Split(',');
            foreach (var pair in pairs)
            {
                var type = ParseRelation(pair.Trim());
                if (type.HasValue)
                {
                    obj.AddRelation(target, type.Value);
                }
            }
        }

        static RelationType? ParseRelation(string pair)
        {
            return pair switch
            {
                "width-width" => RelationType.Width,
                "height-height" => RelationType.Height,
                "left-left" => RelationType.Left_Left,
                "right-right" => RelationType.Right_Right,
                "center-center" => RelationType.Center_Center,
                "top-top" => RelationType.Top_Top,
                "bottom-bottom" => RelationType.Bottom_Bottom,
                "middle-middle" => RelationType.Middle_Middle,
                "leftext-left" => RelationType.LeftExt_Left,
                "rightext-right" => RelationType.RightExt_Right,
                _ => null
            };
        }

        static Controller BuildController(XmlElement node)
        {
            var controller = new Controller { name = node.GetAttribute("name") };
            var pages = (node.GetAttribute("pages") ?? "").Split(',');
            for (var i = 0; i + 1 < pages.Length; i += 2)
            {
                controller.AddPage(pages[i + 1]);
            }

            return controller;
        }

        static XmlDocument LoadXml(string path)
        {
            var xml = new XmlDocument();
            xml.Load(path);
            return xml;
        }

        static string GetFGUIProjectPath()
        {
            if (!string.IsNullOrEmpty(_projectPath) && Directory.Exists(_projectPath))
            {
                return _projectPath;
            }

            var fromAssets = Path.GetFullPath(Path.Combine(Application.dataPath, "../../FGUIProject"));
            if (Directory.Exists(fromAssets))
            {
                _projectPath = fromAssets;
                return _projectPath;
            }

            return null;
        }

        static Vector2 ParseVec2(string value, Vector2 fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            var parts = value.Split(',');
            if (parts.Length < 2)
            {
                return fallback;
            }

            return new Vector2(ParseFloat(parts[0], fallback.x), ParseFloat(parts[1], fallback.y));
        }

        static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value) || value[0] != '#' || (value.Length != 7 && value.Length != 9))
            {
                return fallback;
            }

            var hex = value.Substring(1);
            if (hex.Length == 6)
            {
                hex = "FF" + hex;
            }

            var a = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            var r = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            var g = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            var b = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;

        static float ParseFloat(string value, float fallback) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;

        static AlignType ParseAlign(string value) => value switch
        {
            "center" => AlignType.Center,
            "right" => AlignType.Right,
            _ => AlignType.Left
        };

        static VertAlignType ParseVAlign(string value) => value switch
        {
            "middle" => VertAlignType.Middle,
            "bottom" => VertAlignType.Bottom,
            _ => VertAlignType.Top
        };
    }
}
