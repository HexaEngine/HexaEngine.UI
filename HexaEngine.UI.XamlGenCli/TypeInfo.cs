namespace HexaEngine.UI.XamlGen
{
    using HexaEngine.UI.XamlGenCli;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class TypeInfo
    {
        public Type Type;
        public string? ContentProperty;
        public Dictionary<string, XamlPropertyInfo>? Properties;
        public Dictionary<string, XamlEventInfo>? Events;

        public TypeInfo(Type type)
        {
            Type = type;
            var attr = type.GetCustomAttribute(AssemblyCache.ContentPropertyAttributeType, true);
            if (attr != null)
            {
                ContentProperty = (string)AssemblyCache.ContentPropertyAttributeType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public)!.GetValue(attr)!;
            }

            if (Type.IsAssignableTo(AssemblyCache.DependencyObjectType))
            {
                RuntimeHelpers.RunClassConstructor(Type.TypeHandle);
            }
        }

        private void Reflect()
        {
            if (Properties == null)
            {
                Properties = [];
                foreach (var prop in Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    Properties[prop.Name] = new(prop, null, null);
                }

                if (Type.IsAssignableTo(AssemblyCache.DependencyObjectType))
                {
                    var dpFields = Type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                       .Where(f => f.FieldType.IsAssignableTo(AssemblyCache.DependencyPropertyType));

                    foreach (var field in dpFields)
                    {
                        var dp = new DependencyProperty(field.GetValue(null)!);
                        XamlPropertyInfo info = new(null, field, dp);
                        Properties[dp.Name] = info;
                    }
                }
            }

            if (Events == null)
            {
                Events = []; 
                foreach (var even in Type.GetEvents(BindingFlags.Instance | BindingFlags.Public))
                {
                    Events[even.Name] = new(even, null, null);
                }

                if (Type.IsAssignableTo(AssemblyCache.DependencyObjectType))
                {
                    var reFields = Type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                        .Where(x => x.FieldType.IsAssignableTo(AssemblyCache.RoutedEventType));

                    foreach (var field in reFields)
                    {
                        var e = new RoutedEvent(field.GetValue(null)!);
                        XamlEventInfo eventInfo = new(null, field, e);
                        Events[e.Name] = eventInfo;
                    }
                }
            }
        }

        public bool TryGetProperty(ReadOnlySpan<char> name, [NotNullWhen(true)] out XamlPropertyInfo propertyInfo)
        {
            Reflect();
            return Properties!.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out propertyInfo);
        }

        public bool TryGetEvent(ReadOnlySpan<char> name, [NotNullWhen(true)] out XamlEventInfo eventInfo)
        {
            Reflect();
            return Events!.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out eventInfo);
        }

        public XamlPropertyInfo GetProperty(ReadOnlySpan<char> name)
        {
            if (TryGetProperty(name, out var property))
            {
                return property;
            }
            throw new InvalidOperationException($"Property with name '{name}' not found in type '{Type}'");
        }

        public XamlEventInfo GetEvent(ReadOnlySpan<char> name)
        { 
            if (TryGetEvent(name, out var eventInfo))
            {
                return eventInfo;
            }
            throw new InvalidOperationException($"Event with name '{name}' not found in type '{Type}'");
        }
    }

}