using System;

namespace WorkCard.UI
{
    [AttributeUsage(AttributeTargets.Field)]
    public class UIPropAttribute : Attribute
    {
        public UIPropType Type { get; }

        public UIPropAttribute(UIPropType type = UIPropType.Node)
        {
            Type = type;
        }
    }
}
