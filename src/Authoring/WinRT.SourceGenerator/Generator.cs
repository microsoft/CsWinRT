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
        private Logger Logger { get; }
        private readonly GeneratorExecutionContext context;
        private string tempFolder;
        private readonly TypeMapper mapper;

        public ComponentGenerator(GeneratorExecutionContext context)
        {
            this.context = context;
            Logger = new Logger(context);
            mapper = new(context.AnalyzerConfigOptions.GlobalOptions.GetUiXamlMode());
            // TODO-WuxMux: output a module initializer that validates the MUX/WUX projection mode to ensure that things don't get out of sync.
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
            string assemblyName = context.GetAssemblyName();
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
                Environment.ExitCode = -1;
                return;
            }

            try
            {
                string assembly = context.GetAssemblyName();
                string version = context.GetAssemblyVersion();
                MetadataBuilder metadataBuilder = new MetadataBuilder();

                var writer = new WinRTTypeWriter(
                    assembly,
                    version,
                    metadataBuilder,
                    Logger,
                    mapper);

                WinRTSyntaxReceiver syntaxReceiver = (WinRTSyntaxReceiver)context.SyntaxReceiver;
                Logger.Log("Found " + syntaxReceiver.Declarations.Count + " types");
                foreach (var declaration in syntaxReceiver.Declarations)
                {
                    writer.Model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                    writer.Visit(declaration);
                }
                writer.FinalizeGeneration();

                GenerateWinMD(metadataBuilder);
                if (!context.ShouldGenerateWinMDOnly())
                {
                    GenerateSources();
                    writer.GenerateWinRTExposedClassAttributes(context);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                if (e.InnerException != null)
                {
                    Logger.Log(e.InnerException.ToString());
                }
                Logger.Close();
                Environment.ExitCode = -2;
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
            if (!context.IsCsWinRTComponent() && !context.ShouldGenerateWinMDOnly())
            {
                System.Diagnostics.Debug.WriteLine($"Skipping component {context.GetAssemblyName()}");
                return;
            }

            ComponentGenerator generator = new(context);
            generator.Generate();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new WinRTSyntaxReceiver());
        }
    }

    class WinRTSyntaxReceiver : ISyntaxReceiver
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

            if (syntaxNode is not MemberDeclarationSyntax declaration || !IsPublic(declaration))
            {
                return;
            }

            if (syntaxNode is ClassDeclarationSyntax ||
                syntaxNode is InterfaceDeclarationSyntax ||
                syntaxNode is EnumDeclarationSyntax ||
                syntaxNode is DelegateDeclarationSyntax ||
                syntaxNode is StructDeclarationSyntax)
            {
                Declarations.Add(declaration);
            }
        }

        private bool IsPublic(MemberDeclarationSyntax member)
        {
            // We detect whether partial types are public using symbol information later.
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.PartialKeyword));
        }
    }
}
