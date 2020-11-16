using Microsoft.CodeAnalysis;
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

            // Directory.Delete(outputDir, true);
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

        private static string diagnosticsLink = "https://docs.microsoft.com/en-us/previous-versions/hh977010(v=vs.110)";

        private static DiagnosticDescriptor AsyncRule = MakeRule(
            "WME1084",
            "Async Interfaces Rule",
            "Runtime component class {0} cannot implement async interface {1}; use AsyncInfo class methods instead of async interfaces");

        private static DiagnosticDescriptor ClassConstructorRule = MakeRule(
            "WME1099",
            "Class Constructor Rule",
            "Runtime component class {0} cannot have multiple constructors of the same arity {1}");

        private static DiagnosticDescriptor ParameterNamedValueRule = MakeRule(
            "WME1092",
            "Parameter Named Value Rule",
            ("The method {0} has a parameter named {1} which is the same as the default return value name. " 
            + "Consider using another name for the parameter or use the System.Runtime.InteropServices.WindowsRuntime.ReturnValueNameAttribute " 
            + "to explicitly specify the name of the return value."));

        private static DiagnosticDescriptor StructHasPropertyRule = MakeRule(
            "WME1060(a)",
            "Property in a Struct",
            "The structure {0} has a property {1}. In Windows Runtime, structures can only contain public fields of basic types, enums and structures.");
       
        private static DiagnosticDescriptor StructHasPrivateFieldRule = MakeRule(
            "WME1060(b)",
            "Private field in struct",
            ("Structure {0} has private field. All fields must be public for Windows Runtime structures."));

        private static DiagnosticDescriptor StructHasInvalidFieldRule = MakeRule(
            "WME1060",
            "Invalid field in struct",
            ("Structure {0} has field {1} of type {2}. {2} is not a valid Windows Runtime field type. Each field " +
             "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure.") ); 


        private static DiagnosticDescriptor MakeRule(string id, string title, string messageFormat) 
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: "Usage",
                /* Warnings dont fail command line build; winmd generation is prevented regardless of severity.
                 * Make this error when making final touches on this deliverable. */
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: diagnosticsLink);
        }

        static private string[] ProhibitedAsyncInterfaces = {
                "Windows.Foundation.IAsyncAction", 
                "Windows.Foundation.IAsyncActionWithProgress`1",
                "Windows.Foundation.IAsyncOperation`1",
                "Windows.Foundation.IAsyncOperationWithProgress`2"
        };

        /* SameAsyncInterface uses the proper ISymbol equality check on the OriginalDefinition of the given symbols */
        private bool SameAsyncInterface(INamedTypeSymbol interfaceA, INamedTypeSymbol interfaceB)
        {   
            /* Using OriginalDefinition b/c the generic field of the metadata type has the template name, e.g. `TProgress`
             * and the actual interface will have a concrete type, e.g. `int` */
            return SymbolEqualityComparer.Default.Equals(interfaceA.OriginalDefinition, interfaceB.OriginalDefinition);
        }

        /* ClassImplementsAsyncInterface returns true if the class represented by the symbol
           implements any of the interfaces defined in ProhibitedAsyncInterfaces */
        private bool ImplementsAsyncInterface(GeneratorExecutionContext context, INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
        { 
            foreach (string prohibitedInterface in ProhibitedAsyncInterfaces)
            {
                INamedTypeSymbol asyncInterface = context.Compilation.GetTypeByMetadataName(prohibitedInterface);
                foreach (INamedTypeSymbol interfaceImplemented in classSymbol.AllInterfaces)
                {
                    if (SameAsyncInterface(interfaceImplemented, asyncInterface))
                    { 
                        context.ReportDiagnostic(Diagnostic.Create(AsyncRule, classDeclaration.GetLocation(), classDeclaration.Identifier, interfaceImplemented));
                        return true; 
                        /* By exiting early, we only report diagnostic for first prohibited interface we see. 
                        If a class implemented 2 (or more) such interfaces, then we would only report diagnostic error for the first one. 
                        could thread `found` variable from CatchWinRTDiagnostics here as well, if we want more diagnostics reported */
                    }
                }
            }
            return false;
        }

        /* HasMultipleConstructorsOfSameArity keeps track of the arity of all constructors seen, 
         * and reports the diagnostic (and exits) as soon as a duplicate is seen. */
        private bool HasMultipleConstructorsOfSameArity(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>();
            
            /* more performant data structure? or use a Set, in order to not have to call Contains()? */
            IList<int> aritiesSeenSoFar = new List<int>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;

                if (aritiesSeenSoFar.Contains(arity))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity));
                    return true;
                }
                else
                {
                    aritiesSeenSoFar.Add(arity);
                }
            }
            return false;
        }

        /* HasParameterNamedValue */
        private bool HasReturnValueNameConflict(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<MethodDeclarationSyntax> methods = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>();

            foreach (MethodDeclarationSyntax method in methods)
            {
                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    if (parameter.Identifier.Value.Equals("__retval"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ParameterNamedValueRule, parameter.GetLocation(), method.Identifier, parameter.Identifier));
                        return true;
                    }
                }
            }
            return false;
        }

        private bool StructHasProperty(GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
        { 
            var propertyDeclarations = structDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            if (propertyDeclarations.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasPropertyRule, structDeclaration.GetLocation(), structDeclaration.Identifier, propertyDeclarations.First().Identifier));
                return true;
            }
            return false;
        }

        private bool StructHasInvalidFields(GeneratorExecutionContext context, FieldDeclarationSyntax field, StructDeclarationSyntax structDeclaration, List<string> classNames)
        {
            bool found = false; 

            if (field.GetFirstToken().ToString().Equals("private")) // hmm
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasPrivateFieldRule, field.GetLocation(), structDeclaration.Identifier));
                found |= true;
            } 
            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var type = variable.Type;
                var typeStr = type.ToString();

                List<string> invalidTypes = new List<string> { "object", "byte", "dynamic" };
                invalidTypes.AddRange(classNames);
                
                /*
                if (invalidTypes.Contains(typeStr))
                { 
                    context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                            variable.GetLocation(),
                            structDeclaration.Identifier,
                            field.ToString(),
                            typeStr));
                }
                */

                if (type.IsKind(SyntaxKind.PredefinedType))
                {
                    if (typeStr.Equals("object") || typeStr.Equals("byte"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                            variable.GetLocation(),
                            structDeclaration.Identifier,
                            field.ToString(),
                            typeStr));
                        found |= true;
                    }
                }
                
                if (type.IsKind(SyntaxKind.IdentifierName))
                {
                    // Assuming the only other IdentifierNames would be enums or structures ...
                    if (typeStr.Equals("dynamic") || classNames.Contains(type.GetFirstToken().ToString()))
                    { 
                        context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                            variable.GetLocation(),
                            structDeclaration.Identifier,
                            field.ToString(), // would be better if it just had the field name
                            typeStr));
                        found |= true;
                    }
                }
            }
            return found;
        }

        private bool CatchWinRTDiagnostics(GeneratorExecutionContext context)
        {
            bool found = false; 
            
            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                var nodes = tree.GetRoot().DescendantNodes();

                var classes = nodes.OfType<ClassDeclarationSyntax>();
                var enums   = nodes.OfType<EnumDeclarationSyntax>();
                var structs = nodes.OfType<StructDeclarationSyntax>();

                List<string> classNames = new List<string>();
                List<SyntaxToken> enumNames = new List<SyntaxToken>();

                /* look for... */
                foreach (ClassDeclarationSyntax classDeclaration in classes)
                {
                    classNames.Add(classDeclaration.Identifier.ToString());

                    /* parameters named __retval*/
                    found |= HasReturnValueNameConflict(context, classDeclaration);

                    /* multiple constructors of the same arity */
                    found |= HasMultipleConstructorsOfSameArity(context, classDeclaration);
                    
                    /* implementing async interfaces */
                    var classSymbol = model.GetDeclaredSymbol(classDeclaration);
                    found |= ImplementsAsyncInterface(context, classSymbol, classDeclaration);
                }

                // don't need this loop if the only other variable identifier possible is for a class or struct 
                foreach (EnumDeclarationSyntax enumDeclaration in enums)
                { 
                    enumNames.Add(enumDeclaration.Identifier);
                }

                foreach (StructDeclarationSyntax structDeclaration in structs)
                {
                    found |= StructHasProperty(context, structDeclaration);
                    /*var propertyDeclarations = structDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                    if (propertyDeclarations.Any())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(StructHasPropertyRule, structDeclaration.GetLocation(), structDeclaration.Identifier, propertyDeclarations.First().Identifier));
                        found |= true;
                    }*/



                    var fields = structDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
                    foreach (var field in fields)
                    {

                        found |= StructHasInvalidFields(context, field, structDeclaration, classNames);
                       
                        // check if field is private
                        /*
                        if (field.GetFirstToken().ToString().Equals("private")) // hmm
                        {
                            context.ReportDiagnostic(Diagnostic.Create(StructHasPrivateFieldRule, field.GetLocation(), structDeclaration.Identifier));
                            found |= true;
                        }

                        foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
                        {
                            var type = variable.Type;
                            var typeStr = type.ToString();

                            if (type.IsKind(SyntaxKind.PredefinedType))
                            {
                                if (typeStr.Equals("object") || typeStr.Equals("byte"))
                                { 
                                    context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                                        variable.GetLocation(),
                                        structDeclaration.Identifier,
                                        field.ToString(),
                                        typeStr));
                                    found |= true;
                                }
                            }

                            if (type.IsKind(SyntaxKind.IdentifierName))
                            {
                                // Assuming the only other IdentifierNames would be enums or structures ...
                                if (classNames.Contains(type.GetFirstToken().ToString()))
                                { 
                                    context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                                        variable.GetLocation(),
                                        structDeclaration.Identifier,
                                        field.ToString(), // would be better if it just had the field name
                                        typeStr));
                                    found |= true;
                                }
                            }
                        }
                        */
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
