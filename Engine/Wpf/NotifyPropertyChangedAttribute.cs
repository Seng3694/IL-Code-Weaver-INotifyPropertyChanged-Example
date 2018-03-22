using System;

namespace Engine.Wpf
{
    public class NotifyPropertyChangedAttribute : Attribute
    {
        public Type ComparerType { get; }

        public NotifyPropertyChangedAttribute(Type comparerType = null)
        {
            ComparerType = comparerType;
        }
    }
}
