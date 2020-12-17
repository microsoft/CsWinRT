﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using WinRT.SourceGenerator;

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
            if (_tempFolder == null || !File.Exists(_tempFolder))
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

            foreach (var file in Directory.GetFiles(outputDir, "*.cs", SearchOption.TopDirectoryOnly))
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

        private bool CatchWinRTDiagnostics(ref GeneratorExecutionContext context)
        {
            bool found = false;
            WinRTRules winrtRules = new WinRTRules();
            // classes and interfaces defined, should not appear as struct fields!
            HashSet<INamedTypeSymbol> userCreatedTypes = new HashSet<INamedTypeSymbol>();
            // populate the above set 
            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(winrtRules.IsPublic);
                var interfaces = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Where(winrtRules.IsPublic);
                foreach (var @class in classes) 
                {
                    userCreatedTypes.Add(model.GetDeclaredSymbol(@class));
                }
                foreach (var @interface in interfaces)
                {
                    userCreatedTypes.Add(model.GetDeclaredSymbol(@interface));
                }
            }

            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                var nodes = tree.GetRoot().DescendantNodes();

                var classes = nodes.OfType<ClassDeclarationSyntax>().Where(winrtRules.IsPublic);
                var interfaces = nodes.OfType<InterfaceDeclarationSyntax>().Where(winrtRules.IsPublic);
                var structs = nodes.OfType<StructDeclarationSyntax>();
                
                /* Check all classes */
                foreach (ClassDeclarationSyntax classDeclaration in classes)
                {
            
                    /* exposes an operator overload  */
                    found |= winrtRules.OverloadsOperator(ref context, classDeclaration);

                    /* multiple constructors of the same arity */
                    found |= winrtRules.HasMultipleConstructorsOfSameArity(ref context, classDeclaration);

                    // the below three can be lniked with interface invalid methods I think...
                    found |= winrtRules.ImplementsAsyncInterface(ref context, model.GetDeclaredSymbol(classDeclaration), classDeclaration);
                    found |= winrtRules.CheckPropertiesForArrayTypes(ref context, classDeclaration);

                    found |= winrtRules.ClassHasInvalidMethods(ref context, classDeclaration);
                    // var publicMethods = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>().Where(winrtRules.IsPublic);
                    // found |= winrtRules.HasInvalidMethods<ClassDeclarationSyntax>(ref context, publicMethods, classDeclaration.Identifier);

                }

                foreach (InterfaceDeclarationSyntax interfaceDeclaration in interfaces)
                {
                    // var methods = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    // found |= winrtRules.HasInvalidMethods<ClassDeclarationSyntax>(ref context, methods, interfaceDeclaration.Identifier);
                    found |= winrtRules.InterfaceHasInvalidMethods(ref context, interfaceDeclaration);
                }

                /* Check all structs */
                foreach (StructDeclarationSyntax structDeclaration in structs)
                {
                    found |= winrtRules.StructHasFieldOfType<ConstructorDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<DelegateDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<EventFieldDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<IndexerDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<MethodDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<OperatorDeclarationSyntax>(ref context, structDeclaration);
                    found |= winrtRules.StructHasFieldOfType<PropertyDeclarationSyntax>(ref context, structDeclaration);

                    var fields = structDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
                    foreach (var field in fields) 
                    { 
                        found |= winrtRules.CheckFieldValidity(ref context, field, structDeclaration.Identifier, userCreatedTypes, model); 
                    }
                    if (!fields.Any())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.StructWithNoFieldsRule, structDeclaration.GetLocation(), model.GetDeclaredSymbol(structDeclaration)));
                    }
                }
            }
            return found;
        }

        private void AddArrayAttributes(ref GeneratorExecutionContext context)
        { 
            context.AddSource("System.Runtime.InteropServices.WindowsRuntime", SourceText.From(@"
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
}", Encoding.UTF8));


        }

        public void Execute(GeneratorExecutionContext context)
        {
            /* Temporary workaround needed when unit testing -- need to specify 
             * this property on the unit test's source code's anlayzer config options 
             * *****
            if (!IsCsWinRTComponent(context))
            {
                return;
            }
            */

            Logger.Initialize(context);

            AddArrayAttributes(ref context);

            if (CatchWinRTDiagnostics(ref context))
            {
                Logger.Log("Exiting early -- found errors in authored runtime component.");
                Logger.Close();
                return;
            }

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
