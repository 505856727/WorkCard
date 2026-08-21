using System;
using System.Collections.Generic;
using System.Xml;

namespace WorkCard.Editor
{
    public static class NodeFactory
    {
        public static NodeData Create(string nodeType, PackageData packageData, XmlNode node)
        {
            if (nodeType == NodeType.Component)
            {
                var pkg = node.Attributes["pkg"];
                if (pkg != null)
                {
                    packageData = packageData.Reader.GetPackageById(pkg.Value);
                }

                if (packageData == null)
                {
                    throw new Exception($"包不存在: {pkg?.Value}");
                }

                return packageData.GetComponentById(node.Attributes["src"].Value);
            }

            if (nodeType == NodeType.Group)
            {
                if (!bool.TryParse(node.Attributes["advanced"]?.Value, out _))
                {
                    return null;
                }
            }

            return new NodeData(packageData, node);
        }
    }
}
