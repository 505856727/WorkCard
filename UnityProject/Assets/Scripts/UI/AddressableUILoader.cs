using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace WorkCard.UI
{
    /// <summary>
    /// Keeps all FairyGUI assets alive through Addressables and exposes them to FairyGUI by asset name.
    /// </summary>
    public static class AddressableUILoader
    {
        public const string AssetLabel = "WorkCard.UI";

        static readonly Dictionary<string, List<Object>> AssetsByName =
            new Dictionary<string, List<Object>>(StringComparer.Ordinal);

        static AsyncOperationHandle<IList<Object>> _assetsHandle;

        public static bool IsLoaded { get; private set; }

        public static IEnumerator LoadAll(Action<bool> completed)
        {
            if (IsLoaded)
            {
                completed?.Invoke(true);
                yield break;
            }

            var initializeHandle = Addressables.InitializeAsync(false);
            yield return initializeHandle;
            if (initializeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Addressables UI] 初始化失败：" + initializeHandle.OperationException);
                Addressables.Release(initializeHandle);
                completed?.Invoke(false);
                yield break;
            }

            Addressables.Release(initializeHandle);

            _assetsHandle = Addressables.LoadAssetsAsync<Object>(AssetLabel, null);
            yield return _assetsHandle;
            if (_assetsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[Addressables UI] 无法加载标签 {AssetLabel}：{_assetsHandle.OperationException}");
                completed?.Invoke(false);
                yield break;
            }

            AssetsByName.Clear();
            foreach (var asset in _assetsHandle.Result)
            {
                if (asset == null)
                {
                    continue;
                }

                if (!AssetsByName.TryGetValue(asset.name, out var assets))
                {
                    assets = new List<Object>();
                    AssetsByName.Add(asset.name, assets);
                }

                assets.Add(asset);
            }

            if (!TryGetAsset("ui_config", typeof(TextAsset), out var configAsset))
            {
                Debug.LogError("[Addressables UI] 未找到 ui_config，请执行 WorkCard/Addressables/构建 UI Addressables");
                completed?.Invoke(false);
                yield break;
            }

            UIConfig.Load(((TextAsset)configAsset).text);

            var descriptors = _assetsHandle.Result
                .OfType<TextAsset>()
                .Where(asset => asset.name.EndsWith("_fui", StringComparison.Ordinal))
                .OrderBy(asset => asset.name, StringComparer.Ordinal)
                .ToArray();

            if (descriptors.Length == 0)
            {
                Debug.LogError("[Addressables UI] 未找到 FairyGUI 的 *_fui.bytes 包");
                completed?.Invoke(false);
                yield break;
            }

            foreach (var descriptor in descriptors)
            {
                var packagePrefix = descriptor.name.Substring(0, descriptor.name.Length - "_fui".Length);
                if (UIPackage.GetByName(packagePrefix) == null)
                {
                    UIPackage.AddPackage(descriptor.bytes, packagePrefix, LoadResource);
                }
            }

            IsLoaded = true;
            Debug.Log($"[Addressables UI] 已加载 {descriptors.Length} 个 FairyGUI 包");
            completed?.Invoke(true);
        }

        static object LoadResource(string name, string extension, Type type, out DestroyMethod destroyMethod)
        {
            destroyMethod = DestroyMethod.None;
            if (TryGetAsset(name, type, out var asset))
            {
                return asset;
            }

            Debug.LogWarning($"[Addressables UI] 未找到资源：{name}{extension} ({type.Name})");
            return null;
        }

        static bool TryGetAsset(string name, Type type, out Object result)
        {
            result = null;
            if (!AssetsByName.TryGetValue(name, out var assets))
            {
                return false;
            }

            foreach (var asset in assets)
            {
                if (type.IsInstanceOfType(asset))
                {
                    result = asset;
                    return true;
                }
            }

            return false;
        }
    }
}
