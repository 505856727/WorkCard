using FairyGUI;
using UnityEngine;

namespace WorkCard.UI
{
    public class WindowBase : Window
    {
        protected override void OnInit()
        {
            var info = UIRegistry.Get(GetType());
            if (info == null)
            {
                Debug.LogError($"UI类（{GetType().Name}）未标记 [UIClass]，请检查配置");
                return;
            }

            var created = CreateContent(info);
            if (created == null)
            {
                Debug.LogError($"UI类（{GetType().Name}）关联FGUI组件（{info.Component}）不存在，请检查配置");
                return;
            }

            contentPane = created.asCom;
            UIClassHelper.InitProperties(contentPane, this);
            Center();

            var close = contentPane.GetChild("btn_close") as GButton;
            if (close != null)
            {
                close.onClick.Add(Hide);
            }
            else
            {
                var closeObj = contentPane.GetChild("btn_close");
                if (closeObj != null)
                {
                    closeObj.onClick.Add(Hide);
                }
            }
        }

        static GObject CreateContent(UIResInfo info)
        {
            var pkg = UIPackage.GetByName(info.Package);
            if (pkg != null && pkg.GetItemByName(info.Component) != null)
            {
                return UIPackage.CreateObject(info.Package, info.Component);
            }

            var fromXml = FGUIXmlBuilder.Create(info.Package, info.Component);
            if (fromXml != null)
            {
                Debug.Log($"[{info.Package}/{info.Component}] 尚未发布 *_fui.bytes，已从 FGUI 工程 XML 构建窗口");
            }

            return fromXml;
        }
    }
}
