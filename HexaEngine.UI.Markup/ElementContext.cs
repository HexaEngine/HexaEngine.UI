#nullable enable

using HexaEngine.UI.XamlGenCli;

namespace HexaEngine.UI.XamlGen
{
    public struct ElementContext
    {
        public string VariableName;
        public XamlTypeName TypeName;
        public bool IsRoot;
        public bool IsPropertyElement;
        public string PropertyName;
        public bool IsDefinition;
    }
}