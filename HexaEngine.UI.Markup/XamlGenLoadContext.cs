namespace HexaEngine.UI.XamlGen
{
    using Hexa.NET.Logging;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Loader;

    public class XamlGenLoadContext : AssemblyLoadContext
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(XamlGenLoadContext));
        private readonly Dictionary<string, Assembly> loadedAssemblies = [];
        public XamlGenLoadContext() : base(isCollectible: false)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            Logger.Info($"Resolving assembly: {assemblyName.Name}");
            if (loadedAssemblies.TryGetValue(assemblyName.FullName, out var assembly))
            {
                Logger.Info($"Assembly cache hit");
                return assembly;
            }

            if (AssemblyCache.TryGetAssemblyPath(assemblyName.Name!, out var path))
            {
                Logger.Info($"Loading assembly: {path}");
                assembly = LoadFromAssemblyPath(path);
                loadedAssemblies[assemblyName.FullName] = assembly;
                return assembly;
            }

            return null!;
        }
    }
}