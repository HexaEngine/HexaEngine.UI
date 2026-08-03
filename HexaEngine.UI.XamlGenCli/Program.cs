// See https://aka.ms/new-console-template for more information

using Hexa.NET.Logging;
using HexaEngine.UI.XamlGen;

ILogger Logger = LoggerFactory.General;

string? className = null;
string? defaultNamespace = null;
string? input = null;
string? outputFile = null;

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    switch (arg)
    {
        case "-r":
        case "--references":
            {
                ++i;
                ReadOnlySpan<char> references = args[i];
                while (!references.IsEmpty)
                {
                    int separator = references.IndexOf(';');
                    ReadOnlySpan<char> reference = separator < 0
                        ? references
                        : references[..separator];
                    reference = reference.Trim();

                    if (!reference.IsEmpty)
                    {
                        // The cache retains the path, so materialize only the final slices.
                        AssemblyCache.RegisterAssemblyPath(reference.ToString());
                    }

                    if (separator < 0)
                    {
                        break;
                    }

                    references = references[(separator + 1)..];
                }

                break;
            }

        case "-rf":
        case "--reference-files":
            {
                ++i;
                foreach (var line in File.ReadAllLines(args[i]))
                {
                    AssemblyCache.RegisterAssemblyPath(line);
                }
            }
            break;

        case "-i":
        case "--input":
            input = args[++i];
            break;
        case "-c":
        case "--class-name":
            className = args[++i];
            break;
        case "-n":
        case "--namespace":
            defaultNamespace = args[++i];
            break;
        case "-f":
        case "--file":
            input = File.ReadAllText(args[++i]);
            className ??= Path.GetFileNameWithoutExtension(args[i]);
            break;
        case "-o":
        case "--output":
            outputFile = args[++i];
            break;
        default:
            throw new Exception($"Invalid argument {arg}");
    }
}

Logger.Info($"Output File: {outputFile}");
Logger.Info($"Namespace: {defaultNamespace}");
Logger.Info($"Class Name: {className}");


if (input == null)
{
    Console.WriteLine("No input file specified.");
    return;
}

if (className == null)
{
    Console.WriteLine("No class name specified.");
    return;
}

if (defaultNamespace == null)
{
    Console.WriteLine("No default namespace specified.");
    return;
}

AssemblyCache.Init();

Logger.Info("Starting generation...");

string output;
try
{
    XamlCodeGenerator generator = new();
    output = generator.GenerateCode(className, input, defaultNamespace);
}
catch (Exception ex)
{
    Logger.Error("Generation failed.");
    Logger.Log(ex);
    throw;
}


Logger.Info("Generation complete.");
Logger.Info(output);
if (outputFile != null)
{
    Logger.Info("Writing to output file...");
    File.WriteAllText(outputFile, output);
}