using FairyGUI;

namespace WorkCard.UI
{
    public static class UIBinder
    {
        public static void BindAll(bool loadConfigFromFile = true)
        {
            UIRegistry.Collect();
            if (loadConfigFromFile)
            {
                UIConfig.LoadFromFile();
            }

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
