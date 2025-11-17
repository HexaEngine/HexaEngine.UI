#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class AssemblyCache
    {
        private static readonly Dictionary<string, AssemblyCacheEntry> assemblyCache = [];
        private static readonly Dictionary<string, NamespaceInfo> namespaceMap = [];

        public static void Clear()
        {
            namespaceMap.Clear();
        }

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
                Types.TryGetValue(clrTypeName, out var type);
                return type;
            }
        }

        public struct TypeInfo
        {
            public Type Type;
            public string? ContentProperty;

            public TypeInfo(Type type)
            {
                Type = type;
                foreach (var attr in type.GetCustomAttributes(true))
                {
                    if (attr.GetType().Name == "ContentPropertyAttribute")
                    {
                        var nameProperty = attr.GetType().GetProperty("Name");
                        if (nameProperty != null)
                        {
                            string propertyName = (string)nameProperty.GetValue(attr);
                            ContentProperty = propertyName;
                        }
                    }
                }
            }
        }

        private struct NamespaceInfo
        {
            public string ClrNamespace;
            public string AssemblyName;
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

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == assemblyName)
                {
                    cachedAssembly = new(asm);
                    assemblyCache[assemblyName] = cachedAssembly;
                    return cachedAssembly;
                }
            }

            Assembly assembly = Assembly.Load(assemblyName);
            cachedAssembly = new(assembly);
            assemblyCache[assemblyName] = cachedAssembly;
            return cachedAssembly;
        }

        public static TypeInfo? GetType(string xmlPrefix, string typeName)
        {
            if (!namespaceMap.TryGetValue(xmlPrefix, out NamespaceInfo nsInfo))
            {
                return null;
            }
            var assembly = LoadAssembly(nsInfo.AssemblyName);
            if (assembly == null)
            {
                return null;
            }

            return assembly.Value.GetType(new(typeName, nsInfo.ClrNamespace));
        }

        public static string? GetContentPropertyName(string typeName, string xmlPrefix)
        {
            var info = GetType(xmlPrefix, typeName);
            if (info.HasValue)
            {
                return info.Value.ContentProperty;
            }
            return null;
        }

        public static Type? GetPropertyType(string typeName, string propertyName, string xmlPrefix)
        {
            var type = GetType(xmlPrefix, typeName);
            if (type == null)
            {
                return null;
            }

            PropertyInfo propertyInfo = type.Value.Type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return propertyInfo?.PropertyType;
        }
    }
}