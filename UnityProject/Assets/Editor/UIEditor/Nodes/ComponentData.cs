using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace WorkCard.Editor
{
    public class ComponentData : NodeData
    {
        public string ComponentFile;
        public string FileId;
        public string FileName;
        public bool Exported;
        public List<string> Transitions;
        public List<string> Controllers;

        List<string> _nodeNamePaths;
        List<string> _nodeIdPaths;

        public List<string> NodeNamePaths
        {
            get
            {
                if (_nodeNamePaths == null)
                {
                    _nodeNamePaths = new List<string>();
                    _nodeIdPaths = new List<string>();
                    foreach (var child in Children)
                    {
                        _nodeNamePaths.Add(child.Name);
                        _nodeIdPaths.Add(child.Id);
                        if (child.Data is ComponentData data)
                        {
                            _nodeNamePaths.AddRange(data.NodeNamePaths.Select(t => child.Name + "/" + t));
                            _nodeIdPaths.AddRange(data._nodeIdPaths.Select(t => child.Id + "/" + t));
                        }
                    }

                    foreach (var controller in Controllers)
                    {
                        _nodeNamePaths.Add("@" + controller);
                        _nodeIdPaths.Add("@" + controller);
                    }

                    foreach (var transition in Transitions)
                    {
                        _nodeNamePaths.Add("$" + transition);
                        _nodeIdPaths.Add("$" + transition);
                    }
                }

                return _nodeNamePaths;
            }
        }

        public List<string> Packages
        {
            get
            {
                var packages = new List<string>();
                foreach (var nodeInfo in Children)
                {
                    if (!string.IsNullOrEmpty(nodeInfo.Pkg) && !packages.Contains(nodeInfo.Pkg))
                    {
                        packages.Add(nodeInfo.Pkg);
                    }
                }

                return packages;
            }
        }

        public ComponentData(string componentFile, string fileId, bool exported, PackageData packageData) : base(packageData, null)
        {
            Type = NodeType.Component;
            Exported = exported;
            ComponentFile = componentFile;
            FileId = fileId;
            FileName = Path.GetFileNameWithoutExtension(componentFile);
        }

        public override string GetIdPathByNamePath(string namePath)
        {
            var index = NodeNamePaths.IndexOf(namePath);
            return index > -1 ? _nodeIdPaths[index] : "";
        }

        public int GetTransitionIndex(string name) => Transitions.FindIndex(value => name == value);
        public int GetControllerIndex(string name) => Controllers.FindIndex(value => name == value);

        public void Parse()
        {
            if (!File.Exists(ComponentFile))
            {
                Debug.LogWarning($"包（{Package.Name}）内组件（{FileName}）文件（{ComponentFile}）未找到");
                return;
            }

            var doc = XMLHelper.Load(ComponentFile);
            var displayList = doc.SelectSingleNode("component/displayList");
            if (displayList != null)
            {
                foreach (XmlNode displayItem in displayList)
                {
                    var nodeInfo = CreateNode(displayItem);
                    if (nodeInfo != null)
                    {
                        Children.Add(nodeInfo);
                    }
                }
            }

            Transitions = new List<string>();
            var transitions = doc.SelectNodes("component/transition");
            if (transitions != null)
            {
                foreach (XmlNode item in transitions)
                {
                    Transitions.Add(item.Attributes["name"].Value);
                }
            }

            Controllers = new List<string>();
            var controllers = doc.SelectNodes("component/controller");
            if (controllers != null)
            {
                foreach (XmlNode item in controllers)
                {
                    Controllers.Add(item.Attributes["name"].Value);
                }
            }
        }

        NodeInfo CreateNode(XmlNode node)
        {
            var nodeData = NodeFactory.Create(node.Name, Package, node);
            return nodeData == null ? null : new NodeInfo(node.Attributes, nodeData);
        }
    }
}
