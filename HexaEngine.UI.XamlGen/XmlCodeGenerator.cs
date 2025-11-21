#nullable enable

namespace HexaEngine.UI.XamlGen
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Design.Serialization;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using VSLangProj80;
    using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

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
            Logger.LogInfo($"Starting code generation for file: {wszInputFilePath}");

            try
            {
                string generatedCode = GenerateCode(wszInputFilePath, bstrInputFileContents, wszDefaultNamespace);

                byte[] outputBytes = Encoding.UTF8.GetBytes(generatedCode);
                pcbOutput = (uint)outputBytes.Length;

                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputBytes.Length);
                Marshal.Copy(outputBytes, 0, rgbOutputFileContents[0], outputBytes.Length);

                Logger.LogInfo($"Successfully generated {outputBytes.Length} bytes of code for {wszInputFilePath}");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Code generation failed for {wszInputFilePath}", ex);
                pGenerateProgress?.GeneratorError(0, 0, ex.Message, 0, 0);
                pcbOutput = 0;
                return 1;
            }
        }

        private string GenerateCode(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace)
        {
            var tempFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();
            File.WriteAllLines(tempFile, GetReferences());
            ProcessStartInfo psi = new("C:\\Users\\junam\\source\\repos\\HexaEngine.UI\\HexaEngine.UI.XamlGenCli\\bin\\Publish\\HexaXamlGenCli.exe")
            {
                UseShellExecute = false,
                Arguments = $"-f \"{wszInputFilePath}\" -n \"{wszDefaultNamespace}\" -rf \"{tempFile}\" -o \"{outputFile}\"",
            };

            var process = Process.Start(psi);
            process.WaitForExit();
            var result = File.ReadAllText(outputFile);
            File.Delete(tempFile);
            File.Delete(outputFile);
            return result;
        }

        private static readonly HashSet<string> ignoredProjects = ["HexaEngine.UI.XamlGen", "HexaEngine.UI.XamlGenCli"];

        private static IEnumerable<string> GetReferences()
        {
            var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetRequiredService<SComponentModel>();
            var workspace = componentModel.GetService<Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace>();

            HashSet<Guid> visited = [];
            Stack<Project> projectStack = new();
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                if (ignoredProjects.Contains(project.Name)) continue;
                projectStack.Push(project);
            }

            while (projectStack.Count > 0)
            {
                var project = projectStack.Pop();
                if (!visited.Add(project.Id.Id))
                    continue;

                if (!string.IsNullOrEmpty(project.OutputFilePath))
                {
                    yield return project.OutputFilePath!;
                }

                foreach (var reference in project.MetadataReferences)
                {
                    if (reference is PortableExecutableReference pe && !string.IsNullOrEmpty(pe.FilePath))
                    {
                        yield return pe.FilePath!;
                    }
                }

                foreach (var projectRef in project.ProjectReferences)
                {
                    if (visited.Contains(projectRef.ProjectId.Id))
                        continue;

                    var referencedProject = workspace.CurrentSolution.GetProject(projectRef.ProjectId);
                    if (referencedProject != null)
                    {
                        projectStack.Push(referencedProject);
                    }
                }
            }
        }
    }
}