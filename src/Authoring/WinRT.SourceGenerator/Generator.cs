using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Generator
{
    public class ComponentGenerator
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

        private Logger Logger { get; }
        private readonly GeneratorExecutionContext context;
        private string tempFolder;

        public ComponentGenerator(GeneratorExecutionContext context)
        {
            this.context = context;
            Logger = new Logger(context);
        }

        private string GetTempFolder(bool clearSourceFilesFromFolder = false)
        {
            if (string.IsNullOrEmpty(tempFolder) || !File.Exists(tempFolder))
            {
                string outputDir = Path.Combine(Path.GetTempPath(), "CsWinRT", Path.GetRandomFileName()).TrimEnd('\\');
                Directory.CreateDirectory(outputDir);
                tempFolder = outputDir;
                Logger.Log("Created temp folder: " + tempFolder);
            }

            if (clearSourceFilesFromFolder)
            {
                foreach (var file in Directory.GetFiles(tempFolder, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    Logger.Log("Clearing " + file);
                    File.Delete(file);
                }
            }

            return tempFolder;
        }

        private void GenerateSources()
        {
            string cswinrtExe = context.GetCsWinRTExe();
            string assemblyName = context.GetAssemblyName();
            string winmdFile = context.GetWinmdOutputFile();
            string outputDir = GetTempFolder(true);
            string windowsMetadata = context.GetCsWinRTWindowsMetadata();
            string winmds = context.GetCsWinRTDependentMetadata();

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
                if (!context.GetKeepGeneratedSources())
                {
                    Directory.Delete(outputDir, true);
                }
            }
        }

        private void GenerateWinMD(MetadataBuilder metadataBuilder)
        {
            string outputFile = context.GetWinmdOutputFile();
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

        private bool CatchWinRTDiagnostics()
        {
            // "DiagnosticTests" is a workaround, GetAssemblyName returns null when used by unit tests 
            // shouldn't need workaround once we can pass AnalyzerConfigOptionsProvider in DiagnosticTests.Helpers.cs
            string assemblyName = context.GetAssemblyName() ?? "DiagnosticTests";
            WinRTComponentScanner winrtScanner = new(context, assemblyName);
            winrtScanner.FindDiagnostics();
            return winrtScanner.Found();
        }

        public void Generate()
        {
            if (CatchWinRTDiagnostics())
            {
                Logger.Log("Exiting early -- found errors in authored runtime component.");
                Logger.Close();
                return;
            }

            try
            {
                context.AddSource("System.Runtime.InteropServices.WindowsRuntime", SourceText.From(ArrayAttributes, Encoding.UTF8));
                string assembly = context.GetAssemblyName();
                string version = context.GetAssemblyVersion();
                MetadataBuilder metadataBuilder = new MetadataBuilder();

                var writer = new WinRTTypeWriter(
                    assembly,
                    version,
                    metadataBuilder,
                    Logger);

                WinRTSyntaxReciever syntaxReciever = (WinRTSyntaxReciever)context.SyntaxReceiver;
                Logger.Log("Found " + syntaxReciever.Declarations.Count + " types");
                foreach (var declaration in syntaxReciever.Declarations)
                {
                    writer.Model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                    writer.Visit(declaration);
                }
                writer.FinalizeGeneration();

                GenerateWinMD(metadataBuilder);
                GenerateSources();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                if (e.InnerException != null)
                {
                    Logger.Log(e.InnerException.ToString());
                }
                Logger.Close();
                throw;
            }

            Logger.Log("Done");
            Logger.Close();
        }
    }

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var isTest = string.CompareOrdinal(Process.GetCurrentProcess().ProcessName, "testhost") == 0;
            if (!isTest && !context.IsCsWinRTComponent())
            {
                return;
            }

            ComponentGenerator generator = new ComponentGenerator(context);
            generator.Generate();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new WinRTSyntaxReciever());
        }
    }

    class WinRTSyntaxReciever : ISyntaxReceiver
    {
        public List<MemberDeclarationSyntax> Declarations = new();
        public List<NamespaceDeclarationSyntax> Namespaces = new();

        private bool HasSomePublicTypes(SyntaxNode syntaxNode)
        {
            return syntaxNode.ChildNodes().OfType<MemberDeclarationSyntax>().Any(IsPublic);
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Store namespaces separately as we only need to look at them for diagnostics
            // If we did store them in declarations, we would get duplicate entries in the WinMD,
            //   once from the namespace declaration and once from the member's declaration
            if (syntaxNode is NamespaceDeclarationSyntax @namespace)
            {
                if (HasSomePublicTypes(syntaxNode))
                {
                    Namespaces.Add(@namespace);
                }
            
                // Subsequent checks will fail, small performance boost to return now. 
                return;
            }

            if (syntaxNode is not MemberDeclarationSyntax decaralation || !IsPublic(decaralation))
            {
                return;
            }

            if (syntaxNode is ClassDeclarationSyntax ||
                syntaxNode is InterfaceDeclarationSyntax ||
                syntaxNode is EnumDeclarationSyntax ||
                syntaxNode is DelegateDeclarationSyntax ||
                syntaxNode is StructDeclarationSyntax)
            {
                Declarations.Add(decaralation);
            }
        }

        private bool IsPublic(MemberDeclarationSyntax member)
        {
            // We detect whether partial types are public using symbol information later.
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.PartialKeyword));
        }
    }
}
