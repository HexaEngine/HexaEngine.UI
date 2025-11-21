#nullable enable

namespace HexaEngine.UI.XamlGen
{
    public struct ElementContext
    {
        public string VariableName;
        public string TypeName;
        public string XmlPrefix;
        public bool IsRoot;
        public bool IsPropertyElement;
        public string PropertyName;
        public bool IsDefinition;
    }
}