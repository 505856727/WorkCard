using System;

namespace WorkCard.Config
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : Attribute
    {
        public string Name { get; }
        public ConfigKind Kind { get; }
        public bool IndexFromOne { get; set; }
        public string GroupKey { get; set; }

        public ConfigAttribute(string name, ConfigKind kind = ConfigKind.Map)
        {
            Name = name;
            Kind = kind;
            GroupKey = "";
        }
    }
}
