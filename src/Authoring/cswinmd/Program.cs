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
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
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
        /// <summary>
        /// CSWinMD - Compile C# definitions to WinMD
        /// </summary>
        /// <param name="i">Input WinMD path</param>
        /// <param name="o">Output directory</param>
        /// <param name="sdkVersion">Optional sdk version</param>
        /// Uses System.CommandLine.Dragonfruit
        public static void Main(string[] i, string o, string? sdkVersion)
        {
            if (i.Length == 0)
            {
                Console.Error.WriteLine("No C# source files specified");
                return;
            }

            string inputFile = i[0];

            Console.Write($"Compiling {inputFile}");

            string inputText = File.ReadAllText(inputFile);

            string componentName = Path.GetFileNameWithoutExtension(inputFile);

            var assemblyName = componentName;

            var windows_winmd = GetWindowsWinMdPath(sdkVersion);
            var compilation = CSharpCompilation.Create(
                assemblyName: componentName,
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(inputText, new CSharpParseOptions(LanguageVersion.Preview), inputFile) },
                references: new[] { MetadataReference.CreateFromFile(windows_winmd),
                MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location)
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

            var g = new SourceGenerator();
            var outFolder = string.IsNullOrEmpty(o) ? Environment.GetEnvironmentVariable("TEMP") : o;
            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }
            try
            {
                var cp = new ConfigProvider();
                var config = cp.GlobalOptions as ConfigOptions;
                config.Values["build_property.AssemblyName"] = assemblyName;
                config.Values["build_property.AssemblyVersion"] = "0.0.0.1";
                config.Values["build_property.CsWinRTGeneratedFilesDir"] = outFolder;
                config.Values["build_property.CsWinRTComponent"] = "true";
                config.Values["build_property.CsWinRTEnableLogging"] = "true";
                config.Values["build_property.CSWinMD"] = "true";

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
                    foreach (var v in res.Diagnostics)
                    {
                        Console.WriteLine(v.GetMessage());
                        Console.WriteLine($"\t\tIn {v.Location.GetLineSpan()}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(File.ReadAllText(Path.Join(outFolder, "log.txt")));
            }
        }

        private static bool IsVersion(string v)
        {
            return Version.TryParse(Path.GetFileName(v), out var _);
        }
        private static string GetWindowsWinMdPath(string? sdkVersion)
        {
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (var roots = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots"))
                {
                    var kitsRoot10 = (string)roots.GetValue("KitsRoot10");
                    var unionMetadata = Path.Combine(kitsRoot10, "UnionMetadata");
                    if (sdkVersion == null) {
                        var dirs = Directory.EnumerateDirectories(unionMetadata);
                        sdkVersion = Path.GetFileName(dirs.Where(IsVersion).Last());
                    }
                    var path = Path.Combine(kitsRoot10, "UnionMetadata", sdkVersion, "Windows.winmd");
                    return path;
                }
            }
            throw new ArgumentException("Could not determine Windows.winmd path in the Windows SDK");
        }
    }
}