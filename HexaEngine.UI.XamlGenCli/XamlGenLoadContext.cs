namespace HexaEngine.UI.XamlGen
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Loader;

    public class XamlGenLoadContext : AssemblyLoadContext
    {
        private readonly Dictionary<string, Assembly> loadedAssemblies = [];
        public XamlGenLoadContext() : base(isCollectible: false)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            Logger.LogInfo($"Resolving assembly: {assemblyName.Name}");
            if (loadedAssemblies.TryGetValue(assemblyName.FullName, out var assembly))
            {
                Logger.LogInfo($"Assembly cache hit");
                return assembly;
            }

            if (AssemblyCache.TryGetAssemblyPath(assemblyName.Name!, out var path))
            {
                Logger.LogInfo($"Loading assembly: {path}");
                assembly = LoadFromAssemblyPath(path);
                loadedAssemblies[assemblyName.FullName] = assembly;
                return assembly;
            }

            return null!;
        }
    }
}