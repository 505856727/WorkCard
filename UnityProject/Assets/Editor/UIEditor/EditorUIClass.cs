using System.Collections.Generic;
using WorkCard.UI;

namespace WorkCard.Editor
{
    public class EditorUIClass
    {
        public string Name;
        public string Package;
        public string Component;
        public bool IsWindow;
        public List<UIPropInfo> Props = new List<UIPropInfo>();
        public List<string> Functions = new List<string>();
        public ComponentData ComponentData;
        public UIPropConfig PropConfig;
    }
}
