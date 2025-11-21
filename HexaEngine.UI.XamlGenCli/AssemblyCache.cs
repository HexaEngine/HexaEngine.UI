namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Xml.Linq;

    internal class AssemblyCache
    {
        private static readonly Dictionary<string, AssemblyCacheEntry> assemblyCache = [];
        private static readonly Dictionary<string, NamespaceInfo> namespaceMap = [];
        private static readonly Dictionary<string, string> assemblyPaths = [];
        private static readonly XamlGenLoadContext loadContext = new();

        public static Assembly CoreAssembly = null!;
        public static Assembly UIAssembly = null!;
        public static Type DependencyObjectType = null!;
        public static Type DependencyPropertyType = null!;
        public static Type RoutedEventType = null!;
        public static Type ContentPropertyAttributeType = null!;

        public static void Init()
        {
            CoreAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("HexaEngine.Core"));
            RoutedEventType = CoreAssembly.GetType("HexaEngine.Core.Windows.Events.RoutedEvent", true)!;

            UIAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("HexaEngine.UI"));
            DependencyObjectType = UIAssembly.GetType("HexaEngine.UI.DependencyObject", true)!;
            DependencyPropertyType = UIAssembly.GetType("HexaEngine.UI.DependencyProperty", true)!;
            ContentPropertyAttributeType = UIAssembly.GetType("HexaEngine.UI.Markup.ContentPropertyAttribute", true)!;
        }

        public static void Clear()
        {
            namespaceMap.Clear();
        }

        public static void RegisterAssemblyPath(string path)
        {
            if (path.StartsWith("C:\\Program Files\\dotnet\\packs\\Microsoft.NETCore.App.Ref\\")) return;
            assemblyPaths[Path.GetFileNameWithoutExtension(path)] = path;
        }

        public static bool TryGetAssemblyPath(string assemblyName, out string path)
        {
            return assemblyPaths.TryGetValue(assemblyName, out path);
        }
        public static void RegisterNamespace(string xmlPrefix, string clrNamespace, string assemblyName)
        {
            namespaceMap[xmlPrefix] = new()
            {
                ClrNamespace = clrNamespace,
                AssemblyName = assemblyName
            };
        }

        public static bool IsNamespaceRegistered(string xmlPrefix)
        {
            return namespaceMap.ContainsKey(xmlPrefix);
        }

        public static AssemblyCacheEntry? LoadAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return null;

            if (assemblyCache.TryGetValue(assemblyName, out var cachedAssembly))
                return cachedAssembly;

            Logger.LogInfo($"Loading assembly: {assemblyName}");
            Assembly assembly = loadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));

            cachedAssembly = new(assembly);
            assemblyCache[assemblyName] = cachedAssembly;
            return cachedAssembly;
        }

        public static TypeInfo? GetType(ReadOnlySpan<char> xmlPrefix, ReadOnlySpan<char> typeName)
        {
            if (!namespaceMap.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(xmlPrefix, out NamespaceInfo nsInfo))
            {
                return null;
            }
            var assembly = LoadAssembly(nsInfo.AssemblyName);
            if (assembly == null)
            {
                return null;
            }

            return assembly.Value.GetType(new(typeName.ToString(), nsInfo.ClrNamespace));
        }

        public static string? GetContentPropertyName(ReadOnlySpan<char> typeName, ReadOnlySpan<char> xmlPrefix)
        {
            var info = GetType(xmlPrefix, typeName);
            return info?.ContentProperty;
        }

        public static Type? GetPropertyType(ReadOnlySpan<char> typeName, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> xmlPrefix)
        {
            var type = GetType(xmlPrefix, typeName);
            return type?.GetProperty(propertyName).PropertyType;
        }

        public static XamlPropertyInfo? GetPropertyInfo(ReadOnlySpan<char> typeName, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> xmlPrefix)
        {
            var type = GetType(xmlPrefix, typeName);
            return type?.GetProperty(propertyName);
        }
    }
}