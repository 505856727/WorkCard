using System.Collections.Generic;
using System.IO;
using System.Text;
using WorkCard.LitJson;
using WorkCard.UI;
using UnityEditor;
using UnityEngine;

namespace WorkCard.Editor
{
    public class FGUIReader
    {
        public static readonly int UIConfigVersion = 1;
        public readonly Dictionary<string, PackageData> PackageByName = new Dictionary<string, PackageData>();
        public readonly Dictionary<string, PackageData> PackageById = new Dictionary<string, PackageData>();
        public readonly Dictionary<string, EditorUIClass> UIClasses = new Dictionary<string, EditorUIClass>();

        static readonly string ExportResultFile = "Library/ui_export.json";

        public static System.DateTime LatestExportTime
        {
            get
            {
                var result = IOHelper.ReadJson(ExportResultFile);
                return result == null ? System.DateTime.MinValue : new System.DateTime(long.Parse(result["time"].ToString()));
            }
        }

        public bool LoadFGUIProject(string fguiProjectPath)
        {
            if (!Directory.Exists(fguiProjectPath))
            {
                return false;
            }

            PackageByName.Clear();
            PackageById.Clear();
            foreach (var file in IOHelper.GetFiles(fguiProjectPath + "/assets"))
            {
                if (file.EndsWith("package.xml"))
                {
                    var packageData = new PackageData(this, file);
                    PackageByName[packageData.Name] = packageData;
                    PackageById[packageData.Id] = packageData;
                }
            }

            foreach (var packageData in PackageByName.Values)
            {
                packageData.LoadComponents();
            }

            return true;
        }

        public void RefreshUIClasses()
        {
            UIRegistry.Collect();
            UIClasses.Clear();
            foreach (var info in UIRegistry.ByClassName.Values)
            {
                UIClasses[info.Type.Name] = new EditorUIClass
                {
                    Name = info.Type.Name,
                    Package = info.Package,
                    Component = info.Component,
                    IsWindow = info.IsWindow,
                    Props = info.Props,
                    Functions = info.Functions,
                };
            }
        }

        public PackageData GetPackageById(string packageId)
        {
            PackageById.TryGetValue(packageId, out var ret);
            return ret;
        }

        public PackageData GetPackageByName(string packageName)
        {
            PackageByName.TryGetValue(packageName, out var ret);
            return ret;
        }

        public ComponentData GetPackageComponent(string packageName, string componentName)
        {
            return GetPackageByName(packageName)?.GetComponentByName(componentName);
        }

        public void LoadClassConfig(EditorUIClass uiClass, string configPath)
        {
            uiClass.ComponentData = GetPackageComponent(uiClass.Package, uiClass.Component);
            if (uiClass.ComponentData == null)
            {
                return;
            }

            uiClass.PropConfig = UIPropConfig.Load($"{configPath}/{uiClass.Package}/{uiClass.Component}.json");
            foreach (var prop in uiClass.Props)
            {
                var entry = uiClass.PropConfig.GetOrCreate(prop.Name, prop.Type);
                if (uiClass.ComponentData != null)
                {
                    entry.NamePath = uiClass.ComponentData.GetChildNamePathByIdPath(entry.IdPath);
                }
            }
        }

        public bool ExportAll(string fguiProjectPath, List<string> excludeDepPackages, string configPath, string exportFile)
        {
            if (!IOHelper.PathExists(fguiProjectPath))
            {
                Debug.LogError($"【UIEditor】FGUI项目路径不存在（{fguiProjectPath}）");
                return false;
            }

            if (!IOHelper.FilePathExists(exportFile))
            {
                Debug.LogError($"【UIEditor】导出路径不存在（{exportFile}）");
                return false;
            }

            LoadFGUIProject(fguiProjectPath);
            RefreshUIClasses();

            var uiConfigs = IOHelper.ReadJson(exportFile) ?? new JsonData();
            uiConfigs.SetJsonType(JsonType.Object);
            foreach (var uiClass in UIClasses.Values)
            {
                LoadClassConfig(uiClass, configPath);
                if (uiClass.ComponentData != null)
                {
                    uiClass.PropConfig?.Export(uiConfigs, uiClass.ComponentData, uiClass.Props);
                }
            }

            var pkgs = new JsonData();
            pkgs.SetJsonType(JsonType.Array);
            var pkgList = new List<string>();
            foreach (var pkgName in uiConfigs.Keys)
            {
                if (pkgName == "version" || pkgName == "packages")
                {
                    continue;
                }

                pkgs.Add(pkgName);
                pkgList.Add(pkgName);
            }

            var sbAll = new StringBuilder();
            foreach (var pkgName in pkgList.ToArray())
            {
                if (!PackageByName.TryGetValue(pkgName, out var packageData))
                {
                    continue;
                }

                var pkgIndicesJson = new JsonData();
                pkgIndicesJson.SetJsonType(JsonType.Array);
                var depInfo = packageData.GetDepPackages();
                var sb = new StringBuilder();
                foreach (var dep in depInfo.Packages)
                {
                    if (!PackageById.TryGetValue(dep, out var depPackageData))
                    {
                        continue;
                    }

                    if (excludeDepPackages.Contains(depPackageData.Name) || pkgName == depPackageData.Name)
                    {
                        continue;
                    }

                    if (pkgList.IndexOf(depPackageData.Name) == -1)
                    {
                        pkgs.Add(depPackageData.Name);
                        pkgList.Add(depPackageData.Name);
                        Debug.LogWarning($"【{pkgName}】依赖包【{depPackageData.Name}】未在代码中使用");
                    }

                    pkgIndicesJson.Add(pkgList.IndexOf(depPackageData.Name));
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(depPackageData.Name);
                }

                if (pkgIndicesJson.Count > 0)
                {
                    uiConfigs[pkgName]["deps"] = pkgIndicesJson;
                }

                if (sb.Length > 0)
                {
                    sbAll.AppendLine($"【{pkgName}】依赖包：{sb}");
                }
            }

            uiConfigs["version"] = UIConfigVersion;
            uiConfigs["packages"] = pkgs;
            IOHelper.WriteJson(exportFile, uiConfigs, true);
            var assetPath = "Assets/" + IOHelper.RelativeProjectPath(exportFile);
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log("【UIEditor】包依赖信息\n" + sbAll);
            Debug.Log($"<color=green>【UIEditor】UI配置成功导出到： {assetPath}</color>");

            var jsonData = new JsonData();
            jsonData.SetJsonType(JsonType.Object);
            jsonData["time"] = System.DateTime.Now.Ticks + "";
            IOHelper.WriteJson(ExportResultFile, jsonData);
            return true;
        }
    }
}
