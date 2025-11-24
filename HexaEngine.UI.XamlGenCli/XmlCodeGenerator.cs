#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;

    public class XmlCodeGenerator
    {
        private static void ParseXmlnsDeclaration(string prefix, string uri)
        {
            if (AssemblyCache.IsNamespaceRegistered(prefix))
            {
                Logger.LogInfo($"Namespace prefix '{prefix}' already registered, skipping");
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
                    Logger.LogError($"Assembly name missing in xmlns URI: '{uri}'");
                    throw new NotSupportedException($"Assembly name missing in xmlns URI: '{uri}'");
                }

                Logger.LogInfo($"Registering CLR namespace: prefix='{prefix}', namespace='{clrNamespace}', assembly='{assemblyName}'");
                AssemblyCache.RegisterNamespace(prefix, clrNamespace, assemblyName);
            }
            else if (uri.StartsWith("http://hexaengine.com/ui/v0/xaml"))
            {
                Logger.LogInfo($"Registering default HexaEngine.UI namespace for prefix '{prefix}'");
                AssemblyCache.RegisterNamespace(prefix, "*", "HexaEngine.UI");
            }
            else
            {
                Logger.LogError($"Unsupported xmlns URI: '{uri}'");
                throw new NotSupportedException($"Unsupported xmlns URI: '{uri}'");
            }
        }

        public string GenerateCode(string className, string inputFileContents, string defaultNamespace)
        {
            Logger.LogInfo($"Generating code for class: {className} in namespace: {defaultNamespace}");

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
                                    Logger.LogInfo($"Registering default xmlns: {reader.Value}");
                                    ParseXmlnsDeclaration("", reader.Value);
                                }
                                else if (reader.Name.StartsWith("xmlns:"))
                                {
                                    string prefix = reader.Name.Substring(6);
                                    Logger.LogInfo($"Registering xmlns prefix '{prefix}': {reader.Value}");
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
                            Logger.LogInfo($"Found named element: {nameValue} of type {typeName}");
                        }
                    }
                }
            }

            Logger.LogInfo($"Root type: {rootTypeName}, Named elements: {namedElements.Count}");

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

            Logger.LogInfo($"Code generation completed successfully for {className}");
            return writer.ToString();
        }

        private void ParseInner(string inputFileContents, CodeWriter writer, string? rootTypeName)
        {
            int elementIndex = 0;
            Stack<ElementContext> stack = new();
            ElementContext currentContext = new() { VariableName = "this", IsRoot = true, TypeName = rootTypeName, XmlPrefix = "" };
            StringReader stringReader = new(inputFileContents);
            var reader = XmlReader.Create(stringReader);

            Queue<string> eventHandlerQueue = new();

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
                                ElementContext propertyContext = new()
                                {
                                    VariableName = currentContext.VariableName,
                                    TypeName = currentContext.TypeName,
                                    XmlPrefix = currentContext.XmlPrefix,
                                    IsPropertyElement = true,
                                    PropertyName = elementName[(elementName.IndexOf('.') + 1)..]
                                };
                                stack.Push(currentContext);
                                currentContext = propertyContext;
                            }
                            continue;
                        }

                        string typeName = ParseTypeName(elementName);
                        string xmlPrefix = GetXmlPrefix(elementName);
                        string nameValue = reader.GetAttribute("Name");
                        string variableName = null;

                        if (currentContext.IsRoot)
                        {
                            variableName = "this";
                        }
                        else if (!string.IsNullOrEmpty(nameValue))
                        {
                            variableName = nameValue;
                            writer.WriteLine($"{variableName} = new {typeName}();");
                        }
                        else if (currentContext.IsPropertyElement)
                        {
                            writer.Write($"{currentContext.VariableName}.{currentContext.PropertyName}.Add(new {typeName}()");

                            // Add properties inline if any
                            bool hasProperties = false;
                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "xmlns" || reader.Name.StartsWith("xmlns:"))
                                    {
                                        continue;
                                    }

                                    if (!hasProperties)
                                    {
                                        writer.BeginBlock();
                                        hasProperties = true;
                                    }

                                    string propertyName = reader.Name;
                                    string propertyValue = reader.Value;
                                    writer.WriteLine($"{propertyName} = {ValueConverter.Convert(propertyValue, propertyName, typeName, xmlPrefix)},");
                                }
                                reader.MoveToElement();
                            }

                            if (hasProperties)
                            {
                                writer.EndBlock("});");
                            }
                            else
                            {
                                writer.WriteLine(");");
                            }

                            // Don't push context for empty definitions
                            if (reader.IsEmptyElement)
                            {
                                continue;
                            }

                            stack.Push(currentContext);
                            currentContext = new() { VariableName = null!, TypeName = typeName, XmlPrefix = xmlPrefix, IsDefinition = true };
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
                                    var typeInfo = AssemblyCache.GetType(xmlPrefix, ownerType)!;
                                    if (typeInfo.TryGetProperty(propName, out var propInfo))
                                    {
                                        writer.WriteLine($"{variableName}.SetValue({ownerType}.{propInfo.Field!.Name}, {ValueConverter.Convert(propertyValue, propInfo.PropertyType, propName)});");
                                    }
                                    else if (typeInfo.TryGetEvent(propertyName, out var eventInfo))
                                    {
                                        eventHandlerQueue.Enqueue($"{variableName}.AddHandler({typeName}.{eventInfo.Field!.Name}, {propertyValue});");
                                    }
                                }
                                else
                                {
                                    var typeInfo = AssemblyCache.GetType(xmlPrefix, typeName)!;
                                    if (typeInfo.TryGetProperty(propertyName, out var propInfo))
                                    {
                                        writer.WriteLine($"{variableName}.{propertyName} = {ValueConverter.Convert(propertyValue, propInfo.PropertyType, propertyName)};");
                                    }
                                    else if (typeInfo.TryGetEvent(propertyName, out var _))
                                    {
                                        eventHandlerQueue.Enqueue($"{variableName}.{propertyName} += {propertyValue};");
                                    }
                                }
                            }
                            reader.MoveToElement();
                        }

                        if (!reader.IsEmptyElement && variableName != null)
                        {
                            stack.Push(currentContext);
                            currentContext = new() { VariableName = variableName, TypeName = typeName, XmlPrefix = xmlPrefix };
                        }

                        break;

                    case XmlNodeType.Text:
                        // Handle text content for elements like Label
                        if (!string.IsNullOrWhiteSpace(reader.Value) && currentContext.VariableName != null)
                        {
                            string textValue = reader.Value.Trim();

                            // Get the content property name dynamically
                            string contentProperty = AssemblyCache.GetContentPropertyName(currentContext.TypeName, currentContext.XmlPrefix) ?? throw new InvalidOperationException();
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
                            if (!parentContext.IsRoot)
                            {
                                var typeInfo = AssemblyCache.GetType(parentContext.XmlPrefix, parentContext.TypeName);

                                if (typeInfo.ContentProperty != null)
                                {
                                    var prop = typeInfo.GetProperty(typeInfo.ContentProperty);
                                    if (prop.PropertyType.IsAssignableTo(typeof(System.Collections.IList)))
                                    {
                                        writer.WriteLine($"{parentContext.VariableName}.{typeInfo.ContentProperty}.Add({currentContext.VariableName});");

                                    }
                                    else
                                    {
                                        writer.WriteLine($"{parentContext.VariableName}.{typeInfo.ContentProperty} = {currentContext.VariableName};");
                                    }
                                }
                            }
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