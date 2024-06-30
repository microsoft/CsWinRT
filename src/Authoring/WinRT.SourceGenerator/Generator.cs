using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            mapper = new(context.AnalyzerConfigOptions.GlobalOptions.GetUIXamlProjectionsMode());
            // TODO-WuxMux: output a module initializer that validates the MUX/WUX projection mode to ensure that things don't get out of sync.
        }

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035", Justification = "We need to do file IO to invoke the 'cswinrt' tool.")]
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

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035", Justification = "We need to do file IO to invoke the 'cswinrt' tool.")]
        private void GenerateSources()
        {
            string cswinrtExe = context.GetCsWinRTExe();
            string assemblyName = context.GetAssemblyName();
            string winmdFile = context.GetWinmdOutputFile();
            string outputDir = GetTempFolder(true);
            string windowsMetadata = context.GetCsWinRTWindowsMetadata();
            string winmds = context.GetCsWinRTDependentMetadata();
            string csWinRTExeTFM = context.GetCsWinRTExeTFM();

            string arguments = string.Format(
                "-component -input \"{0}\" -input {1} -include {2} -output \"{3}\" -input {4} -target {5} -verbose",
                winmdFile,
                windowsMetadata,
                assemblyName,
                outputDir,
                winmds,
                csWinRTExeTFM);
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

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035", Justification = "This method is only setting the exit code, not doing actual file IO.")]
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

        /// <summary>
        /// Generates the native exports for a WinRT component.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorExecutionContext"/> value to use to produce source files.</param>
        public static void GenerateWinRTNativeExports(GeneratorExecutionContext context)
        {
            context.AddSource("NativeExports.g.cs", """
                // <auto-generated/>
                #pragma warning disable
                
                namespace WinRT
                {
                    using global::System;
                    using global::System.Runtime.CompilerServices;
                    using global::System.Runtime.InteropServices;

                    /// <inheritdoc cref="Module"/>
                    unsafe partial class Module
                    {
                        /// <summary>
                        /// Retrieves the activation factory from a DLL that contains activatable Windows Runtime classes.
                        /// </summary>
                        /// <param name="activatableClassId">The class identifier that is associated with an activatable runtime class.</param>
                        /// <param name="factory">A pointer to the activation factory that corresponds with the class specified by <paramref name="activatableClassId"/>.</param>
                        /// <returns>The <c>HRESULT</c> for the operation.</returns>
                        /// <seealso href="https://learn.microsoft.com/en-us/previous-versions/br205771(v=vs.85)"/>
                        [UnmanagedCallersOnly(EntryPoint = nameof(DllGetActivationFactory), CallConvs = new[] { typeof(CallConvStdcall) })]
                        public static int DllGetActivationFactory(void* activatableClassId, void** factory)
                        {
                            const int E_INVALIDARG = unchecked((int)0x80070057);
                            const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)(0x80040111));
                            const int S_OK = 0;

                            if (activatableClassId is null || factory is null)
                            {
                                return E_INVALIDARG;
                            }

                            try
                            {
                                scoped ReadOnlySpan<char> fullyQualifiedTypeName = MarshalString.FromAbiUnsafe((IntPtr)activatableClassId);

                                IntPtr obj = GetActivationFactory(fullyQualifiedTypeName);

                                if ((void*)obj is null)
                                {
                                    obj = TryGetDependentActivationFactory(fullyQualifiedTypeName);
                                }

                                if ((void*)obj is null)
                                {
                                    *factory = null;

                                    return CLASS_E_CLASSNOTAVAILABLE;
                                }

                                *factory = (void*)obj;

                                return S_OK;
                            }
                            catch (Exception e)
                            {
                                ExceptionHelpers.SetErrorInfo(e);

                                return ExceptionHelpers.GetHRForException(e);
                            }
                        }

                        /// <summary>
                        /// Determines whether the DLL that implements this function is in use. If not, the caller can unload the DLL from memory.
                        /// </summary>
                        /// <returns>This method always returns <c>S_FALSE</c>.</returns>
                        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/combaseapi/nf-combaseapi-dllcanunloadnow"/>
                        [UnmanagedCallersOnly(EntryPoint = nameof(DllCanUnloadNow), CallConvs = new[] { typeof(CallConvStdcall) })]
                        public static int DllCanUnloadNow()
                        {
                            const int S_FALSE = 1;

                            return S_FALSE;
                        }
                    }
                }
                """);
        }

        /// <summary>
        /// Generates the native exports for a WinRT component.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorExecutionContext"/> value to use to produce source files.</param>
        public static void GenerateWinRTExportsType(GeneratorExecutionContext context)
        {
            if (context.Compilation.AssemblyName is not { Length: > 0 } assemblyName)
            {
                return;
            }

            // Make sure to escape invalid characters for namespace names.
            // See ECMA 335, II.6.2 and II.5.2/3.
            if (assemblyName.AsSpan().IndexOfAny("$@`?".AsSpan()) != -1)
            {
                char[] buffer = new char[assemblyName.Length];

                for (int i = 0; i < assemblyName.Length; i++)
                {
                    buffer[i] = assemblyName[i] is '$' or '@' or '`' or '?'
                        ? '_'
                        : assemblyName[i];
                }

                assemblyName = new string(buffer);
            }

            context.AddSource("ExportsType.g.cs", $$"""
                // <auto-generated/>
                #pragma warning disable

                [assembly: global::WinRT.WinRTAssemblyExportsType(typeof(ABI.Exports.{{assemblyName}}.Module))]
                
                namespace ABI.Exports.{{assemblyName}}
                {
                    using global::System;

                    /// <inheritdoc cref="global::WinRT.Module"/>
                    public static partial class Module
                    {
                        /// <inheritdoc cref="global::WinRT.Module.GetActivationFactory(ReadOnlySpan{char})"/>
                        public static IntPtr GetActivationFactory(ReadOnlySpan<char> runtimeClassId)
                        {
                            return global::WinRT.Module.GetActivationFactory(runtimeClassId);
                        }
                    }
                }
                """);
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

            // Emit the native exports for NAOT compiled WinRT components, if needed
            if (context.IsCsWinRTComponent() && context.ShouldGenerateWinRTNativeExports())
            {
                ComponentGenerator.GenerateWinRTNativeExports(context);
            }

            // Also emit the unique exported 'Module' type to avoid ambiguities in some scenarios (eg. merging)
            if (context.IsCsWinRTComponent())
            {
                ComponentGenerator.GenerateWinRTExportsType(context);
            }
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
                // We only include the namespace if it has a public type as otherwise it won't
                // be projected.  For partial types, there would be one instance that we encounter
                // which declares the accessibility and we will use that to determine the accessibility
                // of the type for the purpose of determining whether to include the namespace.
                if (HasSomePublicTypes(syntaxNode))
                {
                    Namespaces.Add(@namespace);
                }
            
                // Subsequent checks will fail, small performance boost to return now. 
                return;
            }

            if (syntaxNode is not MemberDeclarationSyntax declaration || !IsPublicOrPartial(declaration))
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
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        }

        private bool IsPublicOrPartial(MemberDeclarationSyntax member)
        {
            // We detect whether partial types are public using symbol information later.
            return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.PartialKeyword));
        }
    }
}
