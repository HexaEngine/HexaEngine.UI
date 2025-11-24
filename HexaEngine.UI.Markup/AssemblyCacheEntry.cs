namespace HexaEngine.UI.XamlGen
{
    using System.Collections.Generic;
    using System.Reflection;

    public readonly struct AssemblyCacheEntry
    {
        public readonly Assembly Assembly;
        public readonly Dictionary<ClrTypeName, TypeInfo> Types;

        public AssemblyCacheEntry(Assembly assembly)
        {
            Assembly = assembly;
            Types = [];
            foreach (var type in assembly.GetTypes())
            {
                var clrName = type.GetClrTypeName();
                Types[clrName] = new(type);
            }
        }

        public readonly TypeInfo? GetType(ClrTypeName clrTypeName)
        {
            if (clrTypeName.Namespace == "*")
            {
                foreach (var t in Types)
                {
                    if (t.Key.Name == clrTypeName.Name)
                    {
                        return t.Value;
                    }
                }
            }
            Types.TryGetValue(clrTypeName, out var type);
            return type.Type == null ? null : type;
        }
    }

}