using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using WorkCard.UI;

namespace WorkCard.Editor
{
    public static class AddressablesUISetup
    {
        const string GroupName = "WorkCard UI";
        const string UiRoot = "Assets/UI";

        [MenuItem("WorkCard/Addressables/同步 UI 资源")]
        public static void SyncUIAssets()
        {
            var settings = GetOrCreateSettings();
            var group = GetOrCreateGroup(settings);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            settings.AddLabel(AddressableUILoader.AssetLabel);

            var syncedGuids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { UiRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath) || Path.GetFileName(assetPath) == ".gitkeep")
                {
                    continue;
                }

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = BuildAddress(assetPath);
                entry.SetLabel(AddressableUILoader.AssetLabel, true, true, false);
                syncedGuids.Add(guid);
            }

            foreach (var entry in new List<AddressableAssetEntry>(group.entries))
            {
                if (!syncedGuids.Contains(entry.guid))
                {
                    settings.RemoveAssetEntry(entry.guid, false);
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Addressables UI] 已同步 {syncedGuids.Count} 个资源到 {GroupName}");
        }

        [MenuItem("WorkCard/Addressables/构建 UI Addressables")]
        public static void SetupAndBuild()
        {
            SyncUIAssets();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception("Addressables UI 构建失败：" + result.Error);
            }

            Debug.Log($"[Addressables UI] 构建完成：{result.OutputPath}");
        }

        static AddressableAssetSettings GetOrCreateSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                return settings;
            }

            settings = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                true,
                true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            return settings;
        }

        static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    GroupName,
                    false,
                    false,
                    false,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            return group;
        }

        static string BuildAddress(string assetPath)
        {
            var relative = assetPath.Substring(UiRoot.Length).TrimStart('/');
            return "WorkCard/UI/" + relative;
        }
    }
}
