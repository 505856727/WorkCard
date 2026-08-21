using FairyGUI;

namespace WorkCard.UI
{
    public static class UIBinder
    {
        public static void BindAll()
        {
            UIRegistry.Collect();
            UIConfig.LoadFromFile();

            foreach (var info in UIRegistry.ByType.Values)
            {
                if (info.IsWindow)
                {
                    continue;
                }

                UIObjectFactory.SetPackageItemExtension(
                    "ui://" + info.Package + "/" + info.Component,
                    info.Type);
            }
        }
    }
}
