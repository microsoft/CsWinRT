using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;

namespace Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private string _tempFolder;

        private string GetAssemblyName(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
            return assemblyName;
        }

        private string GetAssemblyVersion(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyVersion", out var assemblyVersion);
            return assemblyVersion;
        }

        public static string GetGeneratedFilesDir(GeneratorExecutionContext context)
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
            // TODO: make it a property with a list of WinMDs
            string additionalWinMds = "10.0.18362.0";

            string arguments = string.Format("-component -input \"{0}\" -input {1} -include {2} -output \"{3}\" -verbose", winmdFile, additionalWinMds, assemblyName, outputDir);
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

            using var cswinrtProcess = Process.Start(processInfo);
            Logger.Log(cswinrtProcess.StandardOutput.ReadToEnd());
            Logger.Log(cswinrtProcess.StandardError.ReadToEnd());
            cswinrtProcess.WaitForExit();

            foreach(var file in Directory.GetFiles(outputDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                Logger.Log("Adding " + file);
                context.AddSource(Path.GetFileNameWithoutExtension(file), SourceText.From(File.ReadAllText(file), Encoding.UTF8));
            }

            Directory.Delete(outputDir, true);
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

        private static DiagnosticDescriptor AsyncRule = new DiagnosticDescriptor("WME1084",
                "WinRT doesn't support implementing Async interfaces",
                "Runtime components can't implement Async interfaces, use AsyncInfo class methods instead (see: WME Error 1084)",
                "Usage",
                DiagnosticSeverity.Error,
                true, 
                "Longer description about not implementing async interfaces.");

        private bool CatchWinRTDiagnostics(GeneratorExecutionContext context)
        {
            Logger.Log("Starting CatchWinRTDiagnostics");
            bool found = false;
            INamedTypeSymbol asyncInterfaceType = context.Compilation.GetTypeByMetadataName("Windows.Foundation.IAsyncAction");
            string[] DontImplementTheseInterfaces = { "Windows.Foundation.IAsyncAction", 
                "Windows.Foundation.IAsyncActionWithProgress", 
                "Windows.Foundation.IAsyncOperation", 
                "Windows.Foundation.IAsyncOperationWithProgress" };

            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (ClassDeclarationSyntax classDeclaration in classes)
                {
                    var classSymbol = model.GetDeclaredSymbol(classDeclaration);
                    foreach (string interfaceName in DontImplementTheseInterfaces)
                    {
                        INamedTypeSymbol interfaceTypeSymbol = context.Compilation.GetTypeByMetadataName(interfaceName);
                        Logger.Log("interfacename: " + interfaceTypeSymbol.ToString());
                        foreach (var thing in classSymbol.AllInterfaces)
                        {
                            Logger.Log("!!! thing is" + thing.ToString());
                        }
                        Logger.Log("did we see it?");
                        if (classSymbol.AllInterfaces.Contains(interfaceTypeSymbol))
                        { 
                            context.ReportDiagnostic(Diagnostic.Create(AsyncRule, classDeclaration.GetLocation()));
                            found |= true;
                        }
                    }
                }
            }

            return found;
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
                Logger.Log("Exiting early runtime component errors found");
                Logger.Close();
                return;
            }

            /*
            foreach (Diagnostic d in context.Compilation.GetMethodBodyDiagnostics()) { Logger.Log("MethodBody diagnostic: " + d.ToString()); }
            foreach (Diagnostic d in context.Compilation.GetParseDiagnostics()) { Logger.Log("Parse diagnostic: " + d.ToString()); }
            foreach (Diagnostic d in context.Compilation.GetDiagnostics()) { Logger.Log("Plain ol diagnostic: " + d.ToString()); }
            foreach (Diagnostic d in context.Compilation.GetDeclarationDiagnostics()) { Logger.Log("Declaration diagnostic: " + d.ToString()); }
            */

            try
            {
                string assembly = GetAssemblyName(context);
                string version = GetAssemblyVersion(context);
                MetadataBuilder metadataBuilder = new MetadataBuilder();

                var writer = new WinRTTypeWriter(
                    assembly,
                    version,
                    metadataBuilder);

                foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
                {
                    var model = context.Compilation.GetSemanticModel(tree);
                    /*
                    foreach (Diagnostic d in model.GetSyntaxDiagnostics()) { Logger.Log("[later] MethodBody diagnostic: " + d.ToString()); }
                    foreach (Diagnostic d in model.GetMethodBodyDiagnostics()) { Logger.Log("[later] MethodBody diagnostic: " + d.ToString()); }
                    foreach (Diagnostic d in model.GetDiagnostics()) { Logger.Log("[later] Plain ol diagnostic: " + d.ToString()); }
                    foreach (Diagnostic d in model.GetDeclarationDiagnostics()) { Logger.Log("[later] Declaration diagnostic: " + d.ToString()); }
                    */
                    writer.Model = model;
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
