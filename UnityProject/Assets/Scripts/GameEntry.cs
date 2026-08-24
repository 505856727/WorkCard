using System.Collections;
using FairyGUI;
using Game;
using UnityEngine;
using WorkCard.Config;
using WorkCard.UI;
using WorkCard.UI.Example;

namespace WorkCard
{
    public class GameEntry : MonoBehaviour
    {
        IEnumerator Start()
        {
            StageCamera.CheckMainCamera();
            GRoot.inst.SetContentScaleFactor(
                DesignResolution.Width,
                DesignResolution.Height,
                UIContentScaler.ScreenMatchMode.MatchWidthOrHeight);
            var loaded = false;
            yield return AddressableUILoader.LoadAll(success => loaded = success);
            if (!loaded)
            {
                Debug.LogError("UI 初始化失败，游戏无法启动");
                yield break;
            }

            UIBinder.BindAll(false);
            ConfigManager.LoadAll();
            var events = ConfigManager.GetMap<EventCardConfigItem>();
            if (events != null)
            {
                Debug.Log($"已加载 EventCardConfig：{events.listItems.Count} 条");
            }

            var window = new ExampleWindow();
            window.Show();
        }
    }
}
