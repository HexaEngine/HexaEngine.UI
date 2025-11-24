namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Reflection;

    public struct XamlPropertyInfo
    {
        public PropertyInfo? Property;
        public FieldInfo? Field;
        public DependencyProperty? Dependency;

        public XamlPropertyInfo(PropertyInfo? property, FieldInfo? field, DependencyProperty? dependency)
        {
            Property = property;
            Field = field;
            Dependency = dependency;
        }

        public readonly Type PropertyType => Dependency?.PropertyType ?? Property!.PropertyType;
    }

}