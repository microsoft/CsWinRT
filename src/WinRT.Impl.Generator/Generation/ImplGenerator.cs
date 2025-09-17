// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using AsmResolver.DotNet;
using ConsoleAppFramework;
using WindowsRuntime.ImplGenerator.Errors;
using WindowsRuntime.ImplGenerator.Resolvers;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT interop .dll generator.
/// </summary>
internal static partial class ImplGenerator
{
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
        typeof(TargetPlatformAttribute).FullName!,
        typeof(SupportedOSPlatformAttribute).FullName!,
#pragma warning disable SYSLIB0003 // Type or member is obsolete
        typeof(SecurityPermissionAttribute).FullName!,
#pragma warning restore SYSLIB0003
        typeof(AssemblyVersionAttribute).FullName!,
        typeof(UnverifiableCodeAttribute).FullName!
    ];

    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="responseFilePath">The path to the response file to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string responseFilePath, CancellationToken token)
    {
        ImplGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = ImplGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("parsing", e);
        }

        args.Token.ThrowIfCancellationRequested();

        PathAssemblyResolver assemblyResolver;
        ModuleDefinition outputModule;

        // Initialize the assembly resolver and load the output module
        try
        {
            LoadOutputModule(args, out assemblyResolver, out outputModule);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("loading", e);
        }

        args.Token.ThrowIfCancellationRequested();

        ModuleDefinition implModule;

        // Define the impl module to emit
        try
        {
            implModule = DefineImplModule(assemblyResolver, outputModule);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("loading", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Emit all necessary IL code in the impl module
        try
        {
            EmitAssemblyAttributes(outputModule, implModule);
            EmitTypeForwards(outputModule, implModule);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("generation", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Write the module to disk with all the generated contents
        try
        {
            WriteImplModuleToDisk(args, implModule);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Impl code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, implModule.Name!)}");
    }

    /// <summary>
    /// Loads the output assembly being produced.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="assemblyResolver">The <see cref="IAssemblyResolver"/> instance in use.</param>
    /// <param name="outputModule">The loaded <see cref="ModuleDefinition"/> for the output assembly.</param>
    private static void LoadOutputModule(
        ImplGeneratorArgs args,
        out PathAssemblyResolver assemblyResolver,
        out ModuleDefinition outputModule)
    {
        // Initialize the assembly resolver (we need to reuse this to allow caching)
        assemblyResolver = new(args.ReferenceAssemblyPaths);

        // Try to load the .dll at the current path
        try
        {
            outputModule = ModuleDefinition.FromFile(args.OutputAssemblyPath, assemblyResolver.ReaderParameters);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownImplExceptions.OutputAssemblyFileReadError(Path.GetFileName(args.OutputAssemblyPath), e);
        }
    }

    /// <summary>
    /// Defines the impl module to emit.
    /// </summary>
    /// <param name="assemblyResolver">The <see cref="IAssemblyResolver"/> instance in use.</param>
    /// <param name="outputModule">The loaded <see cref="ModuleDefinition"/> for the output assembly.</param>
    /// <returns>The impl module to populate and emit.</returns>
    private static ModuleDefinition DefineImplModule(PathAssemblyResolver assemblyResolver, ModuleDefinition outputModule)
    {
        try
        {
            // Create the impl module and its containing assembly
            AssemblyDefinition implAssembly = new(outputModule.Assembly?.Name, outputModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
            ModuleDefinition implModule = new(outputModule.Name, outputModule.OriginalTargetRuntime.GetDefaultCorLib())
            {
                MetadataResolver = new DefaultMetadataResolver(assemblyResolver)
            };

            // Add the module to the parent assembly
            implAssembly.Modules.Add(implModule);

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
            // Copy over all module attributes
            foreach (CustomAttribute moduleAttribute in inputModule.CustomAttributes)
            {
                if (!WellKnownAttributeTypes.Contains(moduleAttribute.Constructor?.DeclaringType?.FullName ?? ""))
                {
                    continue;
                }

                implModule.CustomAttributes.Add(new CustomAttribute(
                    constructor: (ICustomAttributeType)moduleAttribute.Constructor!.ImportWith(implModule.DefaultImporter),
                    signature: moduleAttribute.Signature));
            }

            // Copy over all assembly attributes
            foreach (CustomAttribute assemblyAttribute in inputModule.Assembly!.CustomAttributes)
            {
                if (!WellKnownAttributeTypes.Contains(assemblyAttribute.Constructor?.DeclaringType?.FullName ?? ""))
                {
                    continue;
                }

                implModule.Assembly!.CustomAttributes.Add(new CustomAttribute(
                    constructor: (ICustomAttributeType)assemblyAttribute.Constructor!.ImportWith(implModule.DefaultImporter),
                    signature: assemblyAttribute.Signature));
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
            // We need an assembly reference for the merged projection .dll that will be generated.
            // The version doesn't matter here (as long as it's not '255.255.255.255'). The real .dll
            // will always have a version number equal or higher than this, so it will load correctly.
            AssemblyReference projectionAssembly = new("WinRT.Projection.dll"u8, new Version(0, 0, 0, 0));

            foreach (TypeDefinition exportedType in inputModule.TopLevelTypes)
            {
                // We only need to forward public types
                if (!exportedType.IsPublic)
                {
                    continue;
                }

                // Emit the type forwards for all public (projected) types
                implModule.ExportedTypes.Add(new ExportedType(
                    implementation: projectionAssembly.ImportWith(implModule.DefaultImporter),
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
    /// <param name="module">The module to write to disk.</param>
    private static void WriteImplModuleToDisk(ImplGeneratorArgs args, ModuleDefinition module)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, module.Name!);

        try
        {
            module.Write(winRTInteropAssemblyPath);
        }
        catch (Exception e)
        {
            throw WellKnownImplExceptions.EmitDllError(e);
        }
    }
}
