// See https://aka.ms/new-console-template for more information
using HexaEngine.UI.XamlGen;

Logger.Init();

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
                foreach (string reference in args[i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    AssemblyCache.RegisterAssemblyPath(reference);
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

Logger.LogInfo($"Output File: {outputFile}");
Logger.LogInfo($"Namespace: {defaultNamespace}");
Logger.LogInfo($"Class Name: {className}");


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

Logger.LogInfo("Starting generation...");

string output;
try
{
    XamlCodeGenerator generator = new();
    output = generator.GenerateCode(className, input, defaultNamespace);
}
catch (Exception ex)
{
    Logger.LogError("Generation failed.", ex);
    throw;
}


Logger.LogInfo("Generation complete.");
Logger.LogInfo(output);
if (outputFile != null)
{
    Logger.LogInfo("Writing to output file...");
    File.WriteAllText(outputFile, output);
}

Logger.Shutdown();