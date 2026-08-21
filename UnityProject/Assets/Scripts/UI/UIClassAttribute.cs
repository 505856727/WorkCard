using System;

namespace WorkCard.UI
{
    public static class WindowGroup
    {
        public const string Window = "Window";
        public const string Pop = "Pop";
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UIClassAttribute : Attribute
    {
        public string Group { get; }
        public string Package { get; }
        public string Component { get; }

        public UIClassAttribute(string group, string package, string component)
        {
            Group = group;
            Package = package;
            Component = component;
        }
    }
}
