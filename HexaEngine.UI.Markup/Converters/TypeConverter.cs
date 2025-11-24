using HexaEngine.UI.XamlGen;

namespace HexaEngine.UI.XamlGenCli.Converters
{
    public class TypeConverter : IValueConverter
    {
        public bool TryConvert(string value, out string code)
        {
            XamlTypeName typeName = new(value);
            if (typeName.HasNamespace && AssemblyCache.TryGetNamespaceInfo(typeName.Namespace, out var info))
            {
                code = $"typeof({info.ClrNamespace}.{typeName.Name})";
            }
            else
            {
                code = $"typeof({typeName.Name})";
            }
            return true;
        }
    }
}
