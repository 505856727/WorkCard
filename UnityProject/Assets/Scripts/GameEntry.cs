using System.IO;
using FairyGUI;
using UnityEngine;
using WorkCard.UI;
using WorkCard.UI.Example;

namespace WorkCard
{
    public class GameEntry : MonoBehaviour
    {
        void Start()
        {
            StageCamera.CheckMainCamera();
            GRoot.inst.SetContentScaleFactor(
                DesignResolution.Width,
                DesignResolution.Height,
                UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);
            LoadUIPackages();
            UIBinder.BindAll();

            var window = new ExampleWindow();
            window.Show();
        }

        static void LoadUIPackages()
        {
            var uiDir = Path.Combine(Application.dataPath, "UI");
            if (!Directory.Exists(uiDir))
            {
                Debug.LogError("未找到 Assets/UI，请先发布 FGUI 包（菜单 WorkCard/发布FGUI）");
                return;
            }

            var files = Directory.GetFiles(uiDir, "*_fui.bytes", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Debug.LogWarning("未找到已发布的 FGUI 包（*_fui.bytes）。将从 FGUIProject XML 构建 ExampleWindow。可在 FairyGUI 中发布，或使用菜单 WorkCard/发布FGUI。");
                return;
            }

            foreach (var file in files)
            {
                var assetPath = "Assets" + file.Substring(Application.dataPath.Length);
                assetPath = assetPath.Replace("\\", "/");
                if (assetPath.EndsWith("_fui.bytes"))
                {
                    assetPath = assetPath.Substring(0, assetPath.Length - "_fui.bytes".Length);
                }

                UIPackage.AddPackage(assetPath);
            }
        }
    }
}
