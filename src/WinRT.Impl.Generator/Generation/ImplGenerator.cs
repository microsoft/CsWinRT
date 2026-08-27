// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.PE;
using AsmResolver.PE.Builder;
using AsmResolver.PE.DotNet.StrongName;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Extensions;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.Generator.References;
using WindowsRuntime.ImplGenerator.Errors;
using WindowsRuntime.ImplGenerator.Writers;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT interop .dll generator.
/// </summary>
internal static partial class ImplGenerator
{
    /// <summary>
    /// An IID used to produce MVID values for new implementation assemblies.
    /// </summary>
    private static readonly Guid ImplGeneratorMvidSalt = new("5A79C752-B558-4FFC-9465-25029F160117");

    /// <summary>
    /// The set of well known attribute types to copy over to the generated assemblies.
    /// </summary>
    private static readonly FrozenSet<string> WellKnownAttributeTypes =
    [
        typeof(CompilationRelaxationsAttribute).FullName!,
        typeof(RuntimeCompatibilityAttribute).FullName!,
        typeof(DebuggableAttribute).FullName!,
        typeof(AssemblyMetadataAttribute).FullName!,
        typeof(AssemblyCompanyAttribute).FullName!,
        typeof(AssemblyConfigurationAttribute).FullName!,
        typeof(AssemblyFileVersionAttribute).FullName!,
        typeof(AssemblyInformationalVersionAttribute).FullName!,
        typeof(AssemblyProductAttribute).FullName!,
        typeof(AssemblyTitleAttribute).FullName!,
        typeof(TargetFrameworkAttribute).FullName!,
        typeof(SupportedOSPlatformAttribute).FullName!,
        typeof(NeutralResourcesLanguageAttribute).FullName!,
        typeof(DisableRuntimeMarshallingAttribute).FullName!,
#pragma warning disable SYSLIB0003 // Type or member is obsolete
        typeof(SecurityPermissionAttribute).FullName!,
#pragma warning restore SYSLIB0003
        typeof(AssemblyVersionAttribute).FullName!,
        typeof(UnverifiableCodeAttribute).FullName!
    ];

    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file or debug repro to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        GeneratorPhaseRunner<ImplGeneratorArgs> runner = GeneratorHost.CreateRunner(
            inputFilePath: inputFilePath,
            toolName: "cswinrtimplgen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: ResponseFileParser.Parse<ImplGeneratorArgs, WellKnownImplExceptions>,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledImplException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        // Initialize the assembly resolver and load the output module
        (RuntimeContext runtimeContext, ModuleDefinition outputModule) = runner.RunPhase(
            phaseName: "loading",
            body: LoadOutputModule);

        // Define the impl module to emit
        ModuleDefinition implModule = runner.RunPhase(
            phaseName: "loading",
            body: _ => DefineImplModule(runtimeContext, outputModule));

        // Emit all necessary IL code in the impl module
        runner.RunPhase(phaseName: "generation", body: _ =>
        {
            EmitAssemblyAttributes(outputModule, implModule);
            EmitTypeForwards(outputModule, implModule);
        });

        // Write the module to disk with all the generated contents
        runner.RunPhase(
            phaseName: "emit",
            body: args => WriteImplModuleToDisk(args, outputModule, implModule));

        // Signs the module on disk, if needed
        runner.RunPhase(
            phaseName: "sign",
            body: args => SignImplModuleOnDisk(args, outputModule));

        // Notify the user that generation was successful
        ConsoleApp.Log($"Impl code generated -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, implModule.Name!)}");
    }

    /// <summary>
    /// Loads the output assembly being produced.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The <see cref="RuntimeContext"/> instance in use and the loaded <see cref="ModuleDefinition"/> for the output assembly.</returns>
    private static (RuntimeContext RuntimeContext, ModuleDefinition OutputModule) LoadOutputModule(ImplGeneratorArgs args)
    {
        PEImage outputAssemblyImage;

        // Load the output assembly as a PE image first, so we can probe the .NET version
        try
        {
            outputAssemblyImage = PEImage.FromFile(args.OutputAssemblyPath);

        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.OutputAssemblyFileReadError(Path.GetFileName(args.OutputAssemblyPath), e);
        }

        // Probe the .NET runtime version for the output .dll
        if (!TargetRuntimeProber.TryGetLikelyTargetRuntime(outputAssemblyImage, out DotNetRuntimeInfo targetRuntime))
        {
            throw WellKnownImplExceptions.OutputAssemblyRuntimeVersionNotFound(args.OutputAssemblyPath);
        }

        // Initialize the assembly resolver (this will be held internally by the runtime context)
        PathAssemblyResolver assemblyResolver = new(args.ReferenceAssemblyPaths);

        // Initialize the runtime context (this will be reused to allow caching)
        RuntimeContext runtimeContext = new(targetRuntime, assemblyResolver);

        // Try to load the .dll at the current path
        try
        {
            return (runtimeContext, runtimeContext.LoadModule(outputAssemblyImage));
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.OutputAssemblyFileReadError(Path.GetFileName(args.OutputAssemblyPath), e);
        }
    }

    /// <summary>
    /// Defines the impl module to emit.
    /// </summary>
    /// <param name="runtimeContext">The <see cref="RuntimeContext"/> instance in use.</param>
    /// <param name="outputModule">The loaded <see cref="ModuleDefinition"/> for the output assembly.</param>
    /// <returns>The impl module to populate and emit.</returns>
    private static ModuleDefinition DefineImplModule(RuntimeContext runtimeContext, ModuleDefinition outputModule)
    {
        try
        {
            // Create the impl module, with a deterministic MVID
            ModuleDefinition implModule = new(outputModule.Name, runtimeContext.RuntimeCorLib!.ToAssemblyReference())
            {
                Mvid = MvidGenerator.CreateMvid(outputModule.Mvid, ImplGeneratorMvidSalt)
            };

            // Create its containing assembly as well and add the module to it
            AssemblyDefinition implAssembly = new(outputModule.Assembly?.Name, outputModule.Assembly?.Version ?? new Version(0, 0, 0, 0))
            {
                Modules = { implModule }
            };

            return implModule;
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownImplExceptions.DefineImplAssemblyError(e);
        }
    }

    /// <summary>
    /// Emits the assembly attributes for the impl module.
    /// </summary>
    /// <param name="inputModule">The input module.</param>
    /// <param name="implModule">The impl module being generated.</param>
    private static void EmitAssemblyAttributes(ModuleDefinition inputModule, ModuleDefinition implModule)
    {
        try
        {
            // Copy over all assembly attributes
            foreach (CustomAttribute assemblyAttribute in inputModule.Assembly!.CustomAttributes)
            {
                if (!WellKnownAttributeTypes.Contains(assemblyAttribute.Constructor?.DeclaringType?.FullName ?? ""))
                {
                    continue;
                }

                implModule.Assembly!.CustomAttributes.Add(new CustomAttribute(
                    constructor: assemblyAttribute.Constructor!,
                    signature: assemblyAttribute.Signature));
            }

            // Copy over all module attributes
            foreach (CustomAttribute moduleAttribute in inputModule.CustomAttributes)
            {
                if (!WellKnownAttributeTypes.Contains(moduleAttribute.Constructor?.DeclaringType?.FullName ?? ""))
                {
                    continue;
                }

                implModule.CustomAttributes.Add(new CustomAttribute(
                    constructor: moduleAttribute.Constructor!,
                    signature: moduleAttribute.Signature));
            }
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.EmitAssemblyAttributes(e);
        }
    }

    /// <summary>
    /// Emits the type forwards for all types in the input module.
    /// </summary>
    /// <param name="inputModule">The input module.</param>
    /// <param name="implModule">The impl module being generated.</param>
    private static void EmitTypeForwards(ModuleDefinition inputModule, ModuleDefinition implModule)
    {
        try
        {
            // We need an assembly reference for the precompiled projection .dll for the Windows SDK.
            // The version doesn't matter here (as long as it's not '255.255.255.255'). The real .dll
            // will always have a version number equal or higher than this, so it will load correctly.
            AssemblyReference sdkProjectionAssembly = new("WinRT.Sdk.Projection"u8, new Version(0, 0, 0, 0))
            {
                PublicKeyOrToken = WellKnownPublicKeys.WindowsSdkProjection,
                HasPublicKey = true
            };

            // Similar as above, but for the precompiled XAML projection .dll for Windows SDK XAML types.
            // This is only used when the option to use Windows UI Xaml projections is enabled.
            AssemblyReference sdkXamlProjectionAssembly = new("WinRT.Sdk.Xaml.Projection"u8, new Version(0, 0, 0, 0))
            {
                PublicKeyOrToken = WellKnownPublicKeys.WindowsSdkProjection,
                HasPublicKey = true
            };

            // Similar as above, but for the merged projection .dll for all other Windows Runtime types.
            // Unlike the implementation .dll for the Windows SDK however, this .dll is created on the fly.
            AssemblyReference projectionAssembly = new("WinRT.Projection"u8, new Version(0, 0, 0, 0))
            {
                PublicKeyOrToken = WellKnownPublicKeys.WindowsSdkProjection,
                HasPublicKey = true
            };

            // Check if the input module is either of the Windows SDK reference assemblies. Types
            // from the XAML assembly belong to the XAML projection .dll, while types from the SDK
            // assembly belong to the standard SDK projection .dll. All other types are forwarded
            // to the merged projection .dll, which is generated at final build time.
            bool isSdkModule = inputModule.Assembly?.Name is Utf8String sdkName && sdkName.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET"u8);
            bool isXamlModule = inputModule.Assembly?.Name is Utf8String xamlName && xamlName.AsSpan().SequenceEqual("Microsoft.Windows.UI.Xaml"u8);

            foreach (TypeDefinition exportedType in inputModule.TopLevelTypes)
            {
                // We only need to forward public types
                if (!exportedType.IsPublic)
                {
                    continue;
                }

                // Also make sure the type has a valid namespace, otherwise we can't handle it
                if (exportedType.Namespace is null)
                {
                    continue;
                }

                // Determine the target assembly based on the declaring assembly of the current type.
                // This matches the logic in 'cswinrtinteropgen' to figure out the right one.
                AssemblyReference implementationAssembly = isXamlModule
                    ? sdkXamlProjectionAssembly
                    : isSdkModule
                        ? sdkProjectionAssembly
                        : projectionAssembly;

                // Emit the type forwards for all public (projected) types
                implModule.ExportedTypes.Add(new ExportedType(
                    implementation: implementationAssembly,
                    ns: exportedType.Namespace,
                    name: exportedType.Name)
                {
                    Attributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes.Forwarder
                });
            }
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.EmitTypeForwards(e);
        }
    }

    /// <summary>
    /// Writes the impl module to disk.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="inputModule">The input module.</param>
    /// <param name="implModule">The impl module to write to disk.</param>
    private static void WriteImplModuleToDisk(ImplGeneratorArgs args, ModuleDefinition inputModule, ModuleDefinition implModule)
    {
        // If the input assembly is strongly named, we need to copy over the public key and the hash
        // method. These steps are required so that we can sign the generated .dll correctly later.
        // E.g. setting the hash algorithm allows reserving the right signature space in the file.
        if (inputModule.Assembly!.HasPublicKey)
        {
            implModule.Assembly!.PublicKey = inputModule.Assembly.PublicKey;
            implModule.Assembly!.HasPublicKey = true;
            implModule.Assembly!.HashAlgorithm = inputModule.Assembly.HashAlgorithm;
        }

        string implAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, implModule.Name!);

        try
        {
            // We can't just use 'implModule.Write(path)' here, as that gives us no chance to populate
            // the debug directory of the resulting .dll. Go through the PE image and the file builder
            // explicitly instead, which is exactly what 'Write' does anyway.
            PEImage implImage = implModule.ToPEImage();

            EmitDebugDirectory(implModule, implImage);

            implImage.ToPEFile(new ManagedPEFileBuilder()).Write(implAssemblyPath);
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.EmitDllError(e);
        }
    }

    /// <summary>
    /// Emits the debug directory (and the portable PDB it embeds) for the impl image.
    /// </summary>
    /// <param name="implModule">The impl module being generated.</param>
    /// <param name="implImage">The <see cref="PEImage"/> for the impl module being generated.</param>
    /// <remarks>
    /// <para>
    /// The impl assembly is built from scratch, so it starts with an empty debug directory. That would make
    /// it ship with no symbols at all: no Source Link, no compiler flags, and no way for tooling to tell it
    /// was built deterministically. It is also the assembly that ends up in <c>lib/&lt;tfm&gt;</c> of the
    /// resulting NuGet package, so that gap makes the whole package report as having no symbols.
    /// </para>
    /// <para>
    /// There is no PDB to carry over either: the input assembly is compiled as a reference assembly (see
    /// 'Microsoft.Windows.CsWinRT.BeforeMicrosoftNetSdk.targets'), and a reference-only compilation emits no
    /// debug information at all. Even if it did, that PDB would describe method bodies the impl assembly does
    /// not have. The debug information is therefore synthesized here, describing the impl assembly itself.
    /// </para>
    /// </remarks>
    private static void EmitDebugDirectory(ModuleDefinition implModule, PEImage implImage)
    {
        // The document name has to be deterministic (and is, as it only depends on the assembly name).
        // The '/_' prefix is the same marker the .NET SDK uses for path mapped, deterministic builds.
        string assemblyName = Path.GetFileNameWithoutExtension(implModule.Name!);
        string documentName = $"/_/{assemblyName}.TypeForwards.g.cs";

        PortablePdb pdb = PortablePdbWriter.Write(
            documentName: documentName,
            documentText: TypeForwardsDocumentWriter.Write(implModule),
            references: TypeForwardsDocumentWriter.GetReferences(implModule),
            compilationOptions: GetCompilationOptions());

        DebugDirectoryWriter.Write(implImage, pdb, $"{assemblyName}.pdb");
    }

    /// <summary>
    /// Gets the compilation options to record in the portable PDB of the impl assembly.
    /// </summary>
    /// <returns>The compilation options, as key/value pairs.</returns>
    /// <remarks>
    /// The <c>version</c> entry is the version of this metadata format (not of any tool), and tooling relies
    /// on it to decide whether the remaining information can be trusted. The rest describes how the impl
    /// assembly was produced, which is by this generator rather than by a C# compiler.
    /// </remarks>
    private static List<KeyValuePair<string, string>> GetCompilationOptions()
    {
        return
        [
            new("version", "2"),
            new("compiler-version", typeof(ImplGenerator).Assembly.GetName().Version?.ToString() ?? "0.0.0.0"),
            new("name", "cswinrtimplgen"),
            new("language", "C#"),
            new("source-file-count", "1"),
            new("output-kind", "DynamicallyLinkedLibrary")
        ];
    }

    /// <summary>
    /// Signs the impl module on disk, if needed.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="inputModule">The input module.</param>
    private static void SignImplModuleOnDisk(ImplGeneratorArgs args, ModuleDefinition inputModule)
    {
        // If there is no assembly originator key file, then we don't sign the impl assembly
        if (args.AssemblyOriginatorKeyFile is null or "")
        {
            return;
        }

        StrongNamePrivateKey snk;

        // Try to load the key file (we don't know that the path is actually valid)
        try
        {
            snk = StrongNamePrivateKey.FromFile(args.AssemblyOriginatorKeyFile);
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.SnkLoadError(e);
        }

        string implAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, inputModule.Name!);

        try
        {
            StrongNameSigner signer = new(snk);

            // We're doing full signing with the private key, so we must overwrite the file on disk
            using FileStream assemblyStream = File.Open(implAssemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            // Sign the file with the hashing algorithm that was used in the original module
            signer.SignImage(assemblyStream, inputModule.Assembly!.HashAlgorithm);
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.SignDllError(e);
        }
    }
}