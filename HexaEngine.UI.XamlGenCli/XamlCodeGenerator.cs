#nullable enable


using HexaEngine.UI.Markup;

namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Hexa.NET.Logging;
    using HexaEngine.UI.XamlGenCli;

    public class XamlCodeGenerator
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(XamlCodeGenerator));
        private static void ParseXmlnsDeclaration(string prefix, string uri)
        {
            if (AssemblyCache.IsNamespaceRegistered(prefix))
            {
                Logger.Info($"Namespace prefix '{prefix}' already registered, skipping");
                return;
            }

            // Parse clr-namespace and assembly from URI
            // Format: "clr-namespace:HexaEngine.UI.Controls;assembly=HexaEngine.UI"
            if (uri.StartsWith("clr-namespace:"))
            {
                string? assemblyName = null;

                int clrStart = "clr-namespace:".Length;
                int semicolonIndex = uri.IndexOf(';', clrStart);

                string? clrNamespace;
                if (semicolonIndex > 0)
                {
                    clrNamespace = uri.Substring(clrStart, semicolonIndex - clrStart);

                    int assemblyStart = uri.IndexOf("assembly=", semicolonIndex);
                    if (assemblyStart > 0)
                    {
                        assemblyName = uri.Substring(assemblyStart + "assembly=".Length);
                    }
                }
                else
                {
                    clrNamespace = uri.Substring(clrStart);
                }

                if (assemblyName == null)
                {
                    Logger.Error($"Assembly name missing in xmlns URI: '{uri}'");
                    throw new NotSupportedException($"Assembly name missing in xmlns URI: '{uri}'");
                }

                Logger.Error($"Registering CLR namespace: prefix='{prefix}', namespace='{clrNamespace}', assembly='{assemblyName}'");
                AssemblyCache.RegisterNamespace(prefix, clrNamespace, assemblyName);
            }
            else if (uri.StartsWith("http://hexaengine.com/ui/v0/xaml"))
            {
                Logger.Info($"Registering default HexaEngine.UI namespace for prefix '{prefix}'");
                AssemblyCache.RegisterNamespace(prefix, "*", "HexaEngine.UI");
            }
            else if (uri == "http://schemas.microsoft.com/winfx/2006/xaml")
            {
                // XAML language directives such as x:Key are handled by the generator.
            }
            else
            {
                Logger.Error($"Unsupported xmlns URI: '{uri}'");
                throw new NotSupportedException($"Unsupported xmlns URI: '{uri}'");
            }
        }

        public string GenerateCode(string className, string inputFileContents, string defaultNamespace)
        {
            Logger.Info($"Generating code for class: {className} in namespace: {defaultNamespace}");

            // Clear namespace map for this generation
            AssemblyCache.Clear();

            // First pass: parse xmlns declarations
            using (StringReader stringReader = new(inputFileContents))
            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name == "xmlns")
                                {
                                    Logger.Info($"Registering default xmlns: {reader.Value}");
                                    ParseXmlnsDeclaration("", reader.Value);
                                }
                                else if (reader.Name.StartsWith("xmlns:"))
                                {
                                    string prefix = reader.Name.Substring(6);
                                    Logger.Info($"Registering xmlns prefix '{prefix}': {reader.Value}");
                                    ParseXmlnsDeclaration(prefix, reader.Value);
                                }
                            }
                            reader.MoveToElement();
                        }
                        break; // Only need to process the root element for xmlns
                    }
                }
            }

            StringBuilder sb = new();
            CodeWriter writer = new(sb, defaultNamespace,
                "System",
                "HexaEngine.UI",
                "HexaEngine.UI.Controls",
                "HexaEngine.UI.Graphics",
                "HexaEngine.UI.Graphics.Text",
                "Hexa.NET.Mathematics");

            List<NamedElement> namedElements = [];
            string? rootTypeName = null;

            // Parse XAML to find named elements
            using (StringReader stringReader = new(inputFileContents))
            using (var reader = XmlReader.Create(stringReader))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        rootTypeName ??= ParseTypeName(reader.Name);

                        string nameValue = reader.GetAttribute("Name");
                        if (!string.IsNullOrEmpty(nameValue))
                        {
                            string typeName = ParseTypeName(reader.Name);
                            namedElements.Add(new NamedElement { TypeName = typeName, Name = nameValue });
                            Logger.Info($"Found named element: {nameValue} of type {typeName}");
                        }
                    }
                }
            }

            Logger.Info($"Root type: {rootTypeName}, Named elements: {namedElements.Count}");

            using (writer.PushBlock($"public partial class {className} : {rootTypeName}"))
            {
                // Generate fields for named elements
                foreach (var element in namedElements)
                {
                    writer.WriteLine($"private {element.TypeName} {element.Name};");
                }

                if (namedElements.Count > 0)
                {
                    writer.WriteLine();
                }

                // Generate InitializeComponent method
                using (writer.PushBlock($"public override void InitializeComponent()"))
                {
                    ParseInner(inputFileContents, writer, rootTypeName);
                }
            }

            writer.Dispose(); // VERY IMPORTANT: Dispose the writer to end the namespace block without it the last '}' would be missing.

            Logger.Info($"Code generation completed successfully for {className}");
            return writer.ToString();
        }

        private void ParseInner(string inputFileContents, CodeWriter writer, string? rootTypeName)
        {
            int elementIndex = 0;
            Stack<ElementContext> stack = new();

            XamlCodeGenContext ctx = new();
            ctx.CurrentElement = new()
            {
                VariableName = "this",
                IsRoot = true,
                TypeName = new(rootTypeName),
                ResourceLookupVariable = "this"
            };
            StringReader stringReader = new(inputFileContents);
            var reader = XmlReader.Create(stringReader);

            Queue<string> eventHandlerQueue = new();

            ref var currentContext = ref ctx.CurrentElement;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        string elementName = reader.Name;

                        // Check if it's a property element (contains '.')
                        if (elementName.Contains('.'))
                        {
                            // Property element like Grid.RowDefinitions
                            if (!reader.IsEmptyElement)
                            {
                                string propertyName = elementName[(elementName.IndexOf('.') + 1)..];
                                ElementContext ownerContext = currentContext;
                                if (propertyName == "Resources")
                                {
                                    ownerContext.ResourceLookupVariable = ownerContext.VariableName;
                                }

                                stack.Push(ownerContext);
                                currentContext = new()
                                {
                                    VariableName = ownerContext.VariableName,
                                    TypeName = ownerContext.TypeName,
                                    IsPropertyElement = true,
                                    PropertyName = propertyName,
                                    ResourceLookupVariable = ownerContext.ResourceLookupVariable
                                };
                            }
                            continue;
                        }

                        XamlTypeName typeName = new(elementName);
                        string nameValue = reader.GetAttribute("Name");
                        string variableName = null;

                        if (currentContext.IsRoot)
                        {
                            variableName = "this";
                        }
                        else if (!string.IsNullOrEmpty(nameValue))
                        {
                            variableName = nameValue;
                            writer.WriteLine($"{variableName} = new {typeName.Name}();");
                        }
                        else if (currentContext.IsPropertyElement)
                        {
                            variableName = $"element{elementIndex++}";
                            writer.WriteLine($"{typeName} {variableName} = new();");

                            TypeInfo typeInfo = AssemblyCache.GetType(typeName)
                                ?? throw new InvalidOperationException($"Type '{typeName}' could not be resolved.");
                            string? dictionaryKey = null;
                            Type? styleTargetType = null;

                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "xmlns" || reader.Name.StartsWith("xmlns:"))
                                    {
                                        continue;
                                    }

                                    if (IsDictionaryKeyAttribute(reader))
                                    {
                                        dictionaryKey = QuoteString(reader.Value);
                                        continue;
                                    }

                                    string propertyName = reader.Name;
                                    string propertyValue = reader.Value;
                                    if (!typeInfo.TryGetProperty(propertyName, out XamlPropertyInfo propertyInfo))
                                    {
                                        throw new InvalidOperationException($"Property '{propertyName}' was not found on '{typeName}'.");
                                    }

                                    string convertedValue = ConvertValue(propertyValue, propertyInfo.PropertyType, propertyName, currentContext.ResourceLookupVariable);
                                    writer.WriteLine($"{variableName}.{propertyName} = {convertedValue};");

                                    if (dictionaryKey == null && propertyName == typeInfo.DictionaryKeyProperty)
                                    {
                                        dictionaryKey = convertedValue;
                                    }

                                    if (propertyName == typeInfo.DictionaryKeyProperty && propertyInfo.PropertyType == typeof(Type))
                                    {
                                        styleTargetType = AssemblyCache.GetType(new XamlTypeName(propertyValue))?.Type;
                                    }
                                }
                                reader.MoveToElement();
                            }

                            ElementContext valueContext = new()
                            {
                                VariableName = variableName,
                                TypeName = typeName,
                                DictionaryKey = dictionaryKey,
                                StyleTargetType = styleTargetType,
                                ResourceLookupVariable = currentContext.ResourceLookupVariable
                            };

                            if (reader.IsEmptyElement)
                            {
                                WritePropertyValue(writer, currentContext, valueContext);
                                continue;
                            }

                            stack.Push(currentContext);
                            currentContext = valueContext;
                            continue;
                        }
                        else
                        {
                            variableName = $"element{elementIndex++}";
                            writer.WriteLine($"{typeName} {variableName} = new();");
                        }

                        // Set properties from attributes (for non-definition elements or non-property-collection contexts)
                        if (reader.HasAttributes && variableName != null)
                        {
                            Type? setterValueType = null;
                            XamlPropertyInfo? setterTargetProperty = null;
                            TypeInfo? elementTypeInfo = AssemblyCache.GetType(typeName);
                            if (elementTypeInfo?.Type.FullName == "HexaEngine.UI.Setter" && currentContext.StyleTargetType != null)
                            {
                                string? targetPropertyName = reader.GetAttribute("Property");
                                if (!string.IsNullOrEmpty(targetPropertyName))
                                {
                                    TypeInfo targetTypeInfo = new(currentContext.StyleTargetType);
                                    setterTargetProperty = targetTypeInfo.GetProperty(targetPropertyName);
                                    setterValueType = setterTargetProperty.Value.PropertyType;
                                    if (setterTargetProperty.Value.Field == null)
                                    {
                                        throw new InvalidOperationException($"Style setter property '{targetPropertyName}' on '{currentContext.StyleTargetType}' is not a dependency property.");
                                    }

                                    writer.WriteLine($"{variableName}.TargetProperty = {GetTypeReference(setterTargetProperty.Value.Field.DeclaringType!)}.{setterTargetProperty.Value.Field.Name};");
                                }
                            }

                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name == "xmlns" || reader.Name.StartsWith("xmlns:") || reader.Name == "Name")
                                {
                                    continue;
                                }


                                string propertyName = reader.Name;
                                string propertyValue = reader.Value;

                                var idx = propertyName.IndexOf('.');
                                if (idx != -1)
                                {
                                    var ownerType = propertyName.AsSpan(0, idx);
                                    var propName = propertyName.AsSpan(idx + 1);
                                    TypeInfo typeInfo = AssemblyCache.GetType(new XamlTypeName(ownerType.ToString()))
                                        ?? throw new InvalidOperationException($"Attached-property owner '{ownerType}' could not be resolved.");
                                    if (typeInfo.TryGetProperty(propName, out var propInfo))
                                    {
                                        writer.WriteLine($"{variableName}.SetValue({ownerType}.{propInfo.Field!.Name}, {ConvertValue(propertyValue, propInfo.PropertyType, propName, currentContext.ResourceLookupVariable)});");
                                    }
                                    else if (typeInfo.TryGetEvent(propertyName, out var eventInfo))
                                    {
                                        eventHandlerQueue.Enqueue($"{variableName}.AddHandler({typeName}.{eventInfo.Field!.Name}, {propertyValue});");
                                    }
                                }
                                else
                                {
                                    TypeInfo typeInfo = AssemblyCache.GetType(typeName)
                                        ?? throw new InvalidOperationException($"Type '{typeName}' could not be resolved.");
                                    if (typeInfo.TryGetProperty(propertyName, out var propInfo))
                                    {
                                        Type propertyType = propertyName == "Value" && setterValueType != null
                                            ? setterValueType
                                            : propInfo.PropertyType;
                                        writer.WriteLine($"{variableName}.{propertyName} = {ConvertValue(propertyValue, propertyType, propertyName, currentContext.ResourceLookupVariable)};");
                                    }
                                    else if (typeInfo.TryGetEvent(propertyName, out var _))
                                    {
                                        eventHandlerQueue.Enqueue($"{variableName}.{propertyName} += {propertyValue};");
                                    }
                                }
                            }
                            reader.MoveToElement();
                        }

                        if (variableName != null)
                        {
                            ElementContext valueContext = new()
                            {
                                VariableName = variableName,
                                TypeName = typeName,
                                ResourceLookupVariable = currentContext.ResourceLookupVariable
                            };
                            if (reader.IsEmptyElement)
                            {
                                WriteChildValue(writer, currentContext, valueContext);
                            }
                            else
                            {
                                stack.Push(currentContext);
                                currentContext = valueContext;
                            }
                        }

                        break;

                    case XmlNodeType.Text:
                        // Handle text content for elements like Label
                        if (!string.IsNullOrWhiteSpace(reader.Value) && currentContext.VariableName != null)
                        {
                            string textValue = reader.Value.Trim();

                            // Get the content property name dynamically
                            string contentProperty = AssemblyCache.GetContentPropertyName(currentContext.TypeName) ?? throw new InvalidOperationException();
                            writer.WriteLine($"{currentContext.VariableName}.{contentProperty} = \"{textValue}\";");
                        }
                        break;

                    case XmlNodeType.EndElement:
                        string endElementName = reader.Name;

                        // Skip property elements
                        if (endElementName.Contains('.'))
                        {
                            if (stack.Count > 0)
                            {
                                currentContext = stack.Pop();
                            }
                            continue;
                        }

                        // Add element to parent before popping (but not if current is root or if current is a definition)
                        if (stack.Count > 0 && !currentContext.IsPropertyElement && currentContext.VariableName != null)
                        {
                            ElementContext parentContext = stack.Peek();
                            WriteChildValue(writer, parentContext, currentContext);
                        }

                        if (stack.Count > 0)
                        {
                            currentContext = stack.Pop();
                        }
                        break;
                }
            }

            while (eventHandlerQueue.TryDequeue(out var handlerLine))
            {
                writer.WriteLine(handlerLine);
            }
        }

        private static void WriteChildValue(CodeWriter writer, in ElementContext parentContext, in ElementContext valueContext)
        {
            if (parentContext.IsPropertyElement)
            {
                WritePropertyValue(writer, parentContext, valueContext);
                return;
            }

            if (parentContext.IsRoot)
            {
                return;
            }

            TypeInfo? parentType = AssemblyCache.GetType(parentContext.TypeName);
            if (parentType?.ContentProperty == null)
            {
                return;
            }

            XamlPropertyInfo property = parentType.GetProperty(parentType.ContentProperty);
            if (property.PropertyType.IsAssignableTo(typeof(System.Collections.IList)))
            {
                writer.WriteLine($"{parentContext.VariableName}.{parentType.ContentProperty}.Add({valueContext.VariableName});");
            }
            else
            {
                writer.WriteLine($"{parentContext.VariableName}.{parentType.ContentProperty} = {valueContext.VariableName};");
            }
        }

        private static void WritePropertyValue(CodeWriter writer, in ElementContext propertyContext, in ElementContext valueContext)
        {
            TypeInfo ownerType = AssemblyCache.GetType(propertyContext.TypeName)
                ?? throw new InvalidOperationException($"Type '{propertyContext.TypeName}' could not be resolved.");
            XamlPropertyInfo property = ownerType.GetProperty(propertyContext.PropertyName);

            if (property.PropertyType.IsAssignableTo(typeof(System.Collections.IDictionary)))
            {
                if (valueContext.DictionaryKey == null)
                {
                    throw new InvalidOperationException($"Resource '{valueContext.TypeName}' requires x:Key or DictionaryKeyPropertyAttribute.");
                }

                writer.WriteLine($"{propertyContext.VariableName}.{propertyContext.PropertyName}.Add({valueContext.DictionaryKey}, {valueContext.VariableName});");
                return;
            }

            if (property.PropertyType.IsAssignableTo(typeof(System.Collections.IList)))
            {
                writer.WriteLine($"{propertyContext.VariableName}.{propertyContext.PropertyName}.Add({valueContext.VariableName});");
                return;
            }

            writer.WriteLine($"{propertyContext.VariableName}.{propertyContext.PropertyName} = {valueContext.VariableName};");
        }

        private static string ConvertValue(string value, Type propertyType, ReadOnlySpan<char> propertyName, string? resourceLookupVariable)
        {
            ReadOnlySpan<char> expression = value.AsSpan().Trim();
            if (expression.IsEmpty || expression[0] != '{')
            {
                return ValueConverter.Convert(value, propertyType, propertyName);
            }

            if (expression.Length < 2 || expression[^1] != '}')
            {
                throw new FormatException($"Invalid markup extension '{value}'.");
            }

            expression = expression[1..^1].Trim();
            const string staticResource = "StaticResource";
            if (!expression.StartsWith(staticResource, StringComparison.Ordinal) ||
                (expression.Length > staticResource.Length && !char.IsWhiteSpace(expression[staticResource.Length])))
            {
                throw new NotSupportedException($"Markup extension '{{{expression.ToString()}}}' is not supported.");
            }

            ReadOnlySpan<char> key = expression[staticResource.Length..].Trim();
            if (key.IsEmpty)
            {
                throw new FormatException("StaticResource requires a resource key.");
            }

            string lookupVariable = resourceLookupVariable ?? "this";
            return $"({GetTypeReference(propertyType)}){lookupVariable}.FindResource({QuoteString(key.ToString())})!";
        }

        private static string GetTypeReference(Type type)
        {
            if (type.IsGenericType)
            {
                throw new NotSupportedException($"Generic type '{type}' is not supported in generated resource expressions.");
            }

            string? fullName = type.FullName;
            if (fullName == null)
            {
                throw new InvalidOperationException($"Type '{type}' has no C# type name.");
            }

            return $"global::{fullName.Replace('+', '.')}";
        }

        private static bool IsDictionaryKeyAttribute(XmlReader reader)
        {
            return reader.LocalName == "Key" && reader.NamespaceURI == "http://schemas.microsoft.com/winfx/2006/xaml";
        }

        private static string QuoteString(string value)
        {
            return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n")}\"";
        }

        private string ParseTypeName(string xmlName)
        {
            // Remove namespace prefix if present (e.g., "ui:Button" -> "Button")
            int colonIndex = xmlName.IndexOf(':');
            if (colonIndex >= 0)
            {
                return xmlName.Substring(colonIndex + 1);
            }
            return xmlName;
        }

        private string GetXmlPrefix(string xmlName)
        {
            // Extract namespace prefix (e.g., "ui:Button" -> "ui", "Button" -> "")
            int colonIndex = xmlName.IndexOf(':');
            if (colonIndex >= 0)
            {
                return xmlName.Substring(0, colonIndex);
            }
            return "";
        }
    }
}
