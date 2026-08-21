using System;

namespace WorkCard.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UIComAttribute : Attribute
    {
        public string Package { get; }
        public string Component { get; }

        public UIComAttribute(string package, string component)
        {
            Package = package;
            Component = component;
        }
    }
}
