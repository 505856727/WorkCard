using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace WorkCard.Editor
{
    public class PackageData
    {
        public class DepInfo
        {
            public List<string> Packages = new List<string>();
            public Dictionary<string, List<string>> ComponentPackages = new Dictionary<string, List<string>>();
        }

        public string Id;
        public string PackageFile;
        public string Name;
        public readonly Dictionary<string, ComponentData> ComponentByName = new Dictionary<string, ComponentData>();
        public readonly Dictionary<string, ComponentData> ComponentById = new Dictionary<string, ComponentData>();
        public FGUIReader Reader;

        public PackageData(FGUIReader reader, string packageFile)
        {
            Reader = reader;
            PackageFile = packageFile;
            var fileFullPath = Path.GetDirectoryName(packageFile);
            var fileParentFullPath = Path.GetDirectoryName(fileFullPath);
            var packagePathName = fileFullPath.Substring(fileParentFullPath.Length + 1);
            var doc = XMLHelper.Load(packageFile);
            var packageDescription = doc.SelectSingleNode("packageDescription");
            Id = packageDescription.Attributes["id"].Value;
            var publishPackageName = packageDescription.SelectSingleNode("publish")?.Attributes?["name"].Value;
            Name = string.IsNullOrEmpty(publishPackageName) ? packagePathName : publishPackageName;

            var components = packageDescription.SelectNodes("resources/component");
            InitComponents(fileFullPath, components);
        }

        public void LoadComponents()
        {
            foreach (var compData in ComponentByName.Values)
            {
                compData.Parse();
            }
        }

        public ComponentData GetComponentById(string componentId)
        {
            ComponentById.TryGetValue(componentId, out var ret);
            return ret;
        }

        public ComponentData GetComponentByName(string componentName)
        {
            ComponentByName.TryGetValue(componentName, out var ret);
            return ret;
        }

        public DepInfo GetDepPackages()
        {
            var depInfo = new DepInfo();
            var deps = new HashSet<string>();
            foreach (var componentData in ComponentById.Values)
            {
                if (componentData.Packages.Count > 0)
                {
                    foreach (var pkg in componentData.Packages)
                    {
                        deps.Add(pkg);
                    }

                    depInfo.ComponentPackages[componentData.FileName] = componentData.Packages;
                }
            }

            depInfo.Packages.AddRange(deps);
            return depInfo;
        }

        void InitComponents(string packagePath, XmlNodeList components)
        {
            foreach (XmlNode comp in components)
            {
                var attribs = comp.Attributes;
                var exported = attribs["exported"];
                var compData = new ComponentData(
                    packagePath + attribs["path"].Value + attribs["name"].Value,
                    attribs["id"].Value,
                    exported != null && bool.Parse(exported.Value),
                    this);
                ComponentByName[compData.FileName] = compData;
                ComponentById[compData.FileId] = compData;
            }
        }
    }
}
