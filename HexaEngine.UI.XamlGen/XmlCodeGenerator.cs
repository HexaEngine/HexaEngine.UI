#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using Microsoft.VisualStudio.Shell.Design.Serialization;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using VSLangProj80;

    [ProvideGenerator(typeof(XmlCodeGenerator), "HexaEngine.UI XAML Gen", "", vsContextGuids.vsContextGuidVCSProject, true)]
    [Guid(GeneratorGuidString)]
    public class XmlCodeGenerator : IVsSingleFileGenerator
    {
        public const string GeneratorGuidString = "49a7add4-0024-4919-a7f1-082964553e61";

        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".g.cs";
            return 0;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            try
            {
                string generatedCode = GenerateCode(wszInputFilePath, bstrInputFileContents, wszDefaultNamespace);

                byte[] outputBytes = Encoding.UTF8.GetBytes(generatedCode);
                pcbOutput = (uint)outputBytes.Length;

                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputBytes.Length);
                Marshal.Copy(outputBytes, 0, rgbOutputFileContents[0], outputBytes.Length);

                return 0;
            }
            catch (Exception ex)
            {
                pGenerateProgress?.GeneratorError(0, 0, ex.Message, 0, 0);
                pcbOutput = 0;
                return 1;
            }
        }

        private static void ParseXmlnsDeclaration(string prefix, string uri)
        {
            if (AssemblyCache.IsNamespaceRegistered(prefix))
                return;

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
                    throw new NotSupportedException($"Assembly name missing in xmlns URI: '{uri}'");
                }

                AssemblyCache.RegisterNamespace(prefix, clrNamespace, assemblyName);
            }
            else if (uri.StartsWith("http://hexaengine.com/ui/v0/xaml"))
            {
                AssemblyCache.RegisterNamespace(prefix, "HexaEngine.UI.Controls", "HexaEngine.UI");
            }
            else
            {
                throw new NotSupportedException($"Unsupported xmlns URI: '{uri}'");
            }
        }

        private string GenerateCode(string inputFilePath, string inputFileContents, string defaultNamespace)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            string className = fileName;

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
                                    ParseXmlnsDeclaration("", reader.Value);
                                }
                                else if (reader.Name.StartsWith("xmlns:"))
                                {
                                    string prefix = reader.Name.Substring(6);
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
            using (XmlReader reader = XmlReader.Create(stringReader))
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
                        }
                    }
                }
            }

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

            return writer.ToString();
        }

        private void ParseInner(string inputFileContents, CodeWriter writer, string? rootTypeName)
        {
            int elementIndex = 0;
            Stack<ElementContext> stack = new();
            ElementContext currentContext = new() { VariableName = "this", IsRoot = true, TypeName = rootTypeName, XmlPrefix = "" };
            StringReader stringReader = new(inputFileContents);
            var reader = XmlReader.Create(stringReader);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        string elementName = reader.Name;

                        // Check if it's a property element (contains '.')
                        if (elementName.Contains("."))
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
                                    PropertyName = elementName.Substring(elementName.IndexOf('.') + 1)
                                };
                                stack.Push(currentContext);
                                currentContext = propertyContext;
                            }
                            continue;
                        }

                        string typeName = ParseTypeName(elementName);
                        string xmlPrefix = GetXmlPrefix(elementName);
                        string nameValue = reader.GetAttribute("Name");
                        string variableName;

                        // Skip creating RowDefinition/ColumnDefinition variables
                        bool isDefinition = typeName == "RowDefinition" || typeName == "ColumnDefinition";

                        if (!string.IsNullOrEmpty(nameValue))
                        {
                            variableName = nameValue;
                            writer.WriteLine($"{variableName} = new {typeName}();");
                        }
                        else if (currentContext.IsRoot)
                        {
                            variableName = "this";
                        }
                        else if (isDefinition)
                        {
                            // Create inline for definitions
                            variableName = null;
                        }
                        else
                        {
                            variableName = $"element{elementIndex++}";
                            writer.WriteLine($"{typeName} {variableName} = new();");
                        }

                        // For property collections, write the Add statement with inline creation
                        if (currentContext.IsPropertyElement && isDefinition)
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
                                        writer.WriteLine();
                                        writer.WriteLine("{");
                                        writer.Indent(1);
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
                                writer.Unindent(1);
                                writer.WriteLine("});");
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
                            currentContext = new() { VariableName = variableName, TypeName = typeName, XmlPrefix = xmlPrefix, IsDefinition = true };
                            continue;
                        }

                        // Set properties from attributes (for non-definition elements or non-property-collection contexts)
                        if (reader.HasAttributes && variableName != null && !isDefinition)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                if (reader.Name == "xmlns" || reader.Name.StartsWith("xmlns:") || reader.Name == "Name")
                                {
                                    continue;
                                }

                                string propertyName = reader.Name;
                                string propertyValue = reader.Value;

                                // Check for attached properties (contains '.')
                                if (propertyName.Contains("."))
                                {
                                    string[] parts = propertyName.Split('.');
                                    string ownerType = parts[0];
                                    string propName = parts[1];

                                    writer.WriteLine($"{ownerType}.Set{propName}({variableName}, {ValueConverter.Convert(propertyValue, propName, ownerType, xmlPrefix)});");
                                }
                                else
                                {
                                    writer.WriteLine($"{variableName}.{propertyName} = {ValueConverter.Convert(propertyValue, propertyName, typeName, xmlPrefix)};");
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
                            string contentProperty = AssemblyCache.GetContentPropertyName(currentContext.TypeName, currentContext.XmlPrefix) ?? "Content";
                            writer.WriteLine($"{currentContext.VariableName}.{contentProperty} = \"{textValue}\";");
                        }
                        break;

                    case XmlNodeType.EndElement:
                        string endElementName = reader.Name;

                        // Skip property elements
                        if (endElementName.Contains("."))
                        {
                            if (stack.Count > 0)
                            {
                                currentContext = stack.Pop();
                            }
                            continue;
                        }

                        // Add element to parent before popping (but not if current is root or if current is a definition)
                        if (stack.Count > 0 && !currentContext.IsRoot && !currentContext.IsPropertyElement && !currentContext.IsDefinition && currentContext.VariableName != null)
                        {
                            ElementContext parentContext = stack.Peek();

                            // Don't add root element to itself
                            if (!parentContext.IsRoot)
                            {
                                // Check if parent has a ContentProperty attribute
                                var contentProperty = AssemblyCache.GetContentPropertyName(parentContext.TypeName, parentContext.XmlPrefix);

                                if (contentProperty != null)
                                {
                                    writer.WriteLine($"{parentContext.VariableName}.{contentProperty} = {currentContext.VariableName};");
                                }
                                else
                                {
                                    writer.WriteLine($"{parentContext.VariableName}.Children.Add({currentContext.VariableName});");
                                }
                            }
                            else
                            {
                                // Parent is root, use appropriate property
                                var contentProperty = AssemblyCache.GetContentPropertyName(parentContext.TypeName, parentContext.XmlPrefix);

                                if (contentProperty != null)
                                {
                                    writer.WriteLine($"{parentContext.VariableName}.{contentProperty} = {currentContext.VariableName};");
                                }
                                else
                                {
                                    writer.WriteLine($"{parentContext.VariableName}.Children.Add({currentContext.VariableName});");
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