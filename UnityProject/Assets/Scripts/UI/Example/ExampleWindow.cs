using FairyGUI;

namespace WorkCard.UI.Example
{
    /// <summary>
    /// 对应 FGUI 包 Example / 组件 ExampleWindow。
    /// 在 UI 编辑器里绑定节点后导出 ui_config.json，运行时按配置赋值。
    /// </summary>
    [UIClass(WindowGroup.Pop, "Example", "ExampleWindow")]
    public class ExampleWindow : WindowBase
    {
        [UIProp]
        public GTextField txtTitle;

        [UIProp]
        public GButton btnOk;

        [UIProp]
        public Controller ctrlState;

        [UIProp(UIPropType.ArrayNode)]
        public GButton[] btnTabs;

        protected override void OnShown()
        {
            if (txtTitle != null)
            {
                txtTitle.text = "Hello WorkCard";
            }
        }

        private void _OnBtnOk()
        {
            if (ctrlState != null)
            {
                ctrlState.selectedIndex = 1;
            }

            Hide();
        }
    }
}
