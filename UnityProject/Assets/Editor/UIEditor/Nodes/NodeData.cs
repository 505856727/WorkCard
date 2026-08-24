using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace WorkCard.Editor
{
    public class NodeInfo
    {
        public string Id;
        public string Name;
        public string Pkg;
        public NodeData Data;

        public NodeInfo(XmlAttributeCollection attribs, NodeData data)
        {
            Id = attribs["id"].Value;
            Name = attribs["name"].Value;
            Pkg = attribs["pkg"]?.Value;
            Data = data;
        }
    }

    public class NodeData
    {
        static readonly StringBuilder StringBuilder = new StringBuilder();

        public string Type = NodeType.Unknown;
        public PackageData Package;
        public List<NodeInfo> Children = new List<NodeInfo>();

        public NodeData(PackageData packageData, XmlNode data)
        {
            Package = packageData;
            if (data == null)
            {
                return;
            }

            var attribs = data.Attributes;
            Type = attribs["name"].Value;
            var packageId = attribs["pkg"]?.Value;
            if (packageId != null)
            {
                Package = packageData.Reader.GetPackageById(packageId);
            }
        }

        public int GetChildIndex(string id)
        {
            for (int i = 0, l = Children.Count; i < l; ++i)
            {
                if (Children[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public List<int> GetChildIndices(string idPath)
        {
            if (string.IsNullOrEmpty(idPath))
            {
                return null;
            }

            var ids = idPath.Split('/');
            var indices = new List<int>();
            var root = this;
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var childIndex = root.GetChildIndex(id);
                if (childIndex == -1)
                {
                    return null;
                }

                indices.Add(childIndex);
                root = root.Children[childIndex].Data;
            }

            return indices;
        }

        public NodeData FindChildById(string idPath)
        {
            if (string.IsNullOrEmpty(idPath))
            {
                return null;
            }

            var root = this;
            foreach (var id in idPath.Split('/'))
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var child = root.GetChildById(id)?.Data;
                if (child == null)
                {
                    return null;
                }

                root = child;
            }

            return root;
        }

        public NodeInfo GetChildByName(string name)
        {
            foreach (var child in Children)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }

            return null;
        }

        public NodeInfo GetChildById(string id)
        {
            foreach (var child in Children)
            {
                if (child.Id == id)
                {
                    return child;
                }
            }

            return null;
        }

        public string GetChildNamePathByIdPath(string idPath)
        {
            if (string.IsNullOrEmpty(idPath))
            {
                return null;
            }

            StringBuilder.Clear();
            var root = this;
            foreach (var id in idPath.Split('/'))
            {
                if (id.StartsWith("$") || id.StartsWith("@"))
                {
                    StringBuilder.Append(StringBuilder.Length > 0 ? "/" + id : id);
                    break;
                }

                var child = root.GetChildById(id);
                if (child == null)
                {
                    return null;
                }

                StringBuilder.Append(StringBuilder.Length > 0 ? "/" + child.Name : child.Name);
                root = child.Data;
            }

            return StringBuilder.ToString();
        }

        public virtual string GetIdPathByNamePath(string namePath) => "";
    }
}
