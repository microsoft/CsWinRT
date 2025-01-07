using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Generator
{
    class ConfigOptions : AnalyzerConfigOptions
    {
        public Dictionary<string, string> Values { get; set; } = new();

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return Values.TryGetValue(key, out value);
        }
    }

    class ConfigProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new ConfigOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            var i = new List<string>();
            string? o = null;
            string? sdkVersion = null;
            bool? verbose = false;
            bool? nologo = false;
            string? a = null;

            for (int idx = 0; idx < args.Length; idx++)
            {
                if (args[idx] == "-i")
                {
                    idx++;
                    i.AddRange(args[idx].Split(';'));
                }
                else if (args[idx] == "-o")
                {
                    idx++;
                    o = args[idx]; 
                }
                else if (args[idx] == "-a")
                {
                    idx++;
                    a = args[idx];
                }
                else if (args[idx] == "-sdkVersion")
                {
                    idx++;
                    sdkVersion = args[idx];
                }
                else if (args[idx] == "-verbose")
                {
                    verbose = true;
                }
                else if (args[idx] == "-nologo")
                {
                    nologo = true;
                }
            }

            DoMain(i.ToArray(), o!, a!, sdkVersion, verbose, nologo);
        }

        /// <summary>
        /// CSWinMD - Compile C# definitions to WinMD
        /// </summary>
        /// <param name="i">Input WinMD path</param>
        /// <param name="o">Output directory</param>
        /// <param name="a">Assembly name</param>
        /// <param name="sdkVersion">Optional sdk version</param>
        /// <param name="verbose">Verbose logging</param>
        /// <param name="nologo">Don't print logo</param>
        /// Uses System.CommandLine.Dragonfruit
        public static void DoMain(string[] i, string o, string a, string? sdkVersion, bool? verbose, bool? nologo)
        {
            if (!nologo.HasValue || !nologo.Value)
            {
                Console.WriteLine($"CSWinMD {Assembly.GetExecutingAssembly().GetName().Version}");
            }

            var outFolder = string.IsNullOrEmpty(o) ? Environment.GetEnvironmentVariable("TEMP")! : o!;

            try
            {
                if (i.Length == 0)
                {
                    Console.Error.WriteLine("No C# source files specified");

                    return;
                }
                else if (i.Length > 1)
                {
                    throw new NotImplementedException("Compiling more than one file is not yet implemented");
                }

                string inputFile = i[0];

                Console.Write($"Compiling {inputFile}");

                string inputText = File.ReadAllText(inputFile);

                string componentName = Path.GetFileNameWithoutExtension(inputFile);

                var assemblyName = a;

                var windows_winmd = GetWindowsWinMdPath(sdkVersion);
                var compilation = CSharpCompilation.Create(
                    assemblyName: assemblyName,
                    syntaxTrees: new[] { CSharpSyntaxTree.ParseText(inputText, new CSharpParseOptions(LanguageVersion.Preview), inputFile) },
                    references: new[] { 
                        MetadataReference.CreateFromFile(windows_winmd),
                        MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location)
                    },
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    );

                var g = new SourceGenerator();
                
                if (!Directory.Exists(outFolder))
                {
                    Directory.CreateDirectory(outFolder);
                }
                var cp = new ConfigProvider();
                var config = (cp.GlobalOptions as ConfigOptions)!;
                config.Values["build_property.AssemblyName"] = assemblyName;
                config.Values["build_property.CsWinRTWinMDOutputFile"] = componentName;
                config.Values["build_property.AssemblyVersion"] = "0.0.0.1";
                config.Values["build_property.CsWinRTGeneratedFilesDir"] = outFolder;
                config.Values["build_property.CsWinRTEnableLogging"] = "true";
                config.Values["build_property.CsWinRTGenerateWinMDOnly"] = "true";
                config.Values["build_property.CsWinRTComponentWinMD"] = "true";

                var driver = CSharpGeneratorDriver.Create(
                    generators: ImmutableArray.Create(g),
                    additionalTexts: ImmutableArray<AdditionalText>.Empty,
                    parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
                    optionsProvider: cp
                    );

                var d = driver.RunGenerators(compilation);
                var res = d.GetRunResult();

                if (!res.Diagnostics.IsEmpty)
                {
                    Console.WriteLine();
                    foreach (var v in res.Diagnostics)
                    {
                        Console.WriteLine(v.GetMessage());
                        Console.WriteLine($"\t\tIn {v.Location.GetLineSpan()}");
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($" => {outFolder}\\{componentName}.winmd");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.Error.WriteLine(e);

                if (verbose.HasValue && verbose.Value)
                {
                    var log_txt = Path.Join(outFolder, "log.txt");

                    try
                    {
                        Console.Error.WriteLine(File.ReadAllText(log_txt));
                    }
                    catch
                    {
                    }
                }
            }
        }


        private static bool IsVersion(string v)
        {
            return Version.TryParse(Path.GetFileName(v), out var _);
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "CSWinMD only runs on Windows")]
        private static string GetWindowsWinMdPath(string? sdkVersion)
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var roots = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots");
            var kitsRoot10 = (string)roots!.GetValue("KitsRoot10")!;
            var unionMetadata = Path.Combine(kitsRoot10, "UnionMetadata");

            if (sdkVersion == null)
            {
                var dirs = Directory.EnumerateDirectories(unionMetadata);
                var versions = dirs.Where(IsVersion).ToList();
                versions.Sort();
                sdkVersion = Path.GetFileName(versions.Last());
            }

            var path = Path.Combine(kitsRoot10, "UnionMetadata", sdkVersion, "Windows.winmd");

            return path;
        }
    }
}