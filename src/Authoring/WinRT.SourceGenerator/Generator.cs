﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private static readonly string ArrayAttributes = @"
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [global::System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class ReadOnlyArrayAttribute : global::System.Attribute
    {
    }

    [global::System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class WriteOnlyArrayAttribute : global::System.Attribute
    {
    }
}";

        private string _tempFolder;

        private static string GetAssemblyName(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
            return assemblyName;
        }

        private static string GetAssemblyVersion(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyVersion", out var assemblyVersion);
            return assemblyVersion;
        }

        internal static string GetGeneratedFilesDir(GeneratorExecutionContext context)
        {
            // TODO: determine correct location to write to.
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GeneratedFilesDir", out var generatedFilesDir);
            Directory.CreateDirectory(generatedFilesDir);
            return generatedFilesDir;
        }

        private static bool IsCsWinRTComponent(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTComponent", out var isCsWinRTComponentStr))
            {
                return bool.TryParse(isCsWinRTComponentStr, out var isCsWinRTComponent) && isCsWinRTComponent;
            }

            return false;
        }

        private static string GetCsWinRTExe(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTExe", out var cswinrtExe);
            return cswinrtExe;
        }

        private static bool GetKeepGeneratedSources(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTKeepGeneratedSources", out var keepGeneratedSourcesStr);
            return keepGeneratedSourcesStr != null && bool.TryParse(keepGeneratedSourcesStr, out var keepGeneratedSources) && keepGeneratedSources;
        }

        private static string GetCsWinRTWindowsMetadata(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTWindowsMetadata", out var cswinrtWindowsMetadata);
            return cswinrtWindowsMetadata;
        }

        private static string GetCsWinRTDependentMetadata(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTAuthoringInputs", out var winmds);
            return winmds;
        }

        private string GetTempFolder(bool clearSourceFilesFromFolder = false)
        {
            if(_tempFolder == null || !File.Exists(_tempFolder))
            {
                string outputDir = Path.Combine(Path.GetTempPath(), "CsWinRT", Path.GetRandomFileName()).TrimEnd('\\');
                Directory.CreateDirectory(outputDir);
                _tempFolder = outputDir;
                Logger.Log("Created temp folder: " + _tempFolder);
            }

            if (clearSourceFilesFromFolder)
            {
                foreach (var file in Directory.GetFiles(_tempFolder, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    Logger.Log("Clearing " + file);
                    File.Delete(file);
                }
            }

            return _tempFolder;
        }

        private void GenerateSources(GeneratorExecutionContext context)
        {
            string cswinrtExe = GetCsWinRTExe(context);
            string assemblyName = GetAssemblyName(context);
            string winmdFile = GetWinmdOutputFile(context);
            string outputDir = GetTempFolder(true);
            string windowsMetadata = GetCsWinRTWindowsMetadata(context);
            string winmds = GetCsWinRTDependentMetadata(context);

            string arguments = string.Format(
                "-component -input \"{0}\" -input {1} -include {2} -output \"{3}\" -input {4} -verbose",
                winmdFile,
                windowsMetadata,
                assemblyName,
                outputDir,
                winmds);
            Logger.Log("Running " + cswinrtExe + " " + arguments);

            var processInfo = new ProcessStartInfo
            {
                FileName = cswinrtExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            try
            {
                using var cswinrtProcess = Process.Start(processInfo);
                Logger.Log(cswinrtProcess.StandardOutput.ReadToEnd());
                Logger.Log(cswinrtProcess.StandardError.ReadToEnd());
                cswinrtProcess.WaitForExit();

                if (cswinrtProcess.ExitCode != 0)
                {
                    throw new Win32Exception(cswinrtProcess.ExitCode);
                }

                foreach (var file in Directory.GetFiles(outputDir, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    Logger.Log("Adding " + file);
                    context.AddSource(Path.GetFileNameWithoutExtension(file), SourceText.From(File.ReadAllText(file), Encoding.UTF8));
                }
            }
            finally
            {
                if (!GetKeepGeneratedSources(context))
                {
                    Directory.Delete(outputDir, true);
                }
            }
        }

        private string GetWinmdOutputFile(GeneratorExecutionContext context)
        {
            return Path.Combine(GetGeneratedFilesDir(context), GetAssemblyName(context) + ".winmd");
        }

        private void GenerateWinMD(MetadataBuilder metadataBuilder, string outputFile)
        {
            Logger.Log("Writing " + outputFile);
            var managedPeBuilder = new ManagedPEBuilder(
                new PEHeaderBuilder(
                    machine: Machine.I386,
                    imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll | Characteristics.Bit32Machine),
                new MetadataRootBuilder(metadataBuilder, "WindowsRuntime 1.4"),
                new BlobBuilder(),
                flags: CorFlags.ILOnly);

            var peBlob = new BlobBuilder();
            managedPeBuilder.Serialize(peBlob);

            using var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fs);
        }

        private bool CatchWinRTDiagnostics(GeneratorExecutionContext context)
        {
            // "DiagnosticTests" is a workaround, GetAssemblyName returns null when used by unit tests 
            // shouldn't need workaround once we can pass AnalyzerConfigOptionsProvider in DiagnosticTests.Helpers.cs
            string assemblyName = GetAssemblyName(context) ?? "DiagnosticTests";
            WinRTComponentScanner winrtScanner = new WinRTComponentScanner(context, assemblyName);
            winrtScanner.FindDiagnostics();
            return winrtScanner.Found();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!IsCsWinRTComponent(context))
            {
                return;
            }
            
            Logger.Initialize(context);
            
            
            if (CatchWinRTDiagnostics(context))
            {
                Logger.Log("Exiting early -- found errors in authored runtime component.");
                Logger.Close();
                return;
            }

            try
            {
                context.AddSource("System.Runtime.InteropServices.WindowsRuntime", SourceText.From(ArrayAttributes, Encoding.UTF8));
                string assembly = GetAssemblyName(context);
                string version = GetAssemblyVersion(context);
                MetadataBuilder metadataBuilder = new MetadataBuilder();

                var writer = new WinRTTypeWriter(
                    assembly,
                    version,
                    metadataBuilder);
                foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
                {
                    writer.Model = context.Compilation.GetSemanticModel(tree);
                    writer.Visit(tree.GetRoot());
                }
                writer.FinalizeGeneration();

                string winmdFile = GetWinmdOutputFile(context);

                GenerateWinMD(metadataBuilder, winmdFile);
                GenerateSources(context);
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
                if(e.InnerException != null)
                {
                    Logger.Log(e.InnerException.ToString());
                }
                Logger.Close();
                throw;
            }

            Logger.Log("Done");
            Logger.Close();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
