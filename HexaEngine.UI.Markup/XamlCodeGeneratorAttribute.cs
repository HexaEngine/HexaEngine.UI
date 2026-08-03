namespace HexaEngine.UI.Markup
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public class XamlCodeGeneratorAttribute : Attribute
    {
        public XamlCodeGeneratorAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
