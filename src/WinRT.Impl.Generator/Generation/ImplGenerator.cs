// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
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

        // Initialize the assembly resolver (we need to reuse this to allow caching)
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferenceAssemblyPaths);

        ModuleDefinition module;

        // Try to load the .dll at the current path
        try
        {
            module = ModuleDefinition.FromFile(args.OutputAssemblyPath, pathAssemblyResolver.ReaderParameters);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownImplExceptions.OutputAssemblyFileReadError(Path.GetFileName(args.OutputAssemblyPath), e);
        }

        try
        {
            // Create the impl module and its containing assembly
            AssemblyDefinition implAssembly = new(module.Assembly?.Name, module.Assembly?.Version ?? new Version(0, 0, 0, 0));
            ModuleDefinition implModule = new(module.Name, module.OriginalTargetRuntime.GetDefaultCorLib())
            {
                MetadataResolver = new DefaultMetadataResolver(pathAssemblyResolver)
            };

            // Add the module to the parent assembly
            implAssembly.Modules.Add(implModule);

            WriteImplModuleToDisk(args, implModule);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownImplExceptions.DefineImplAssemblyError(e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Impl code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, "test")}");
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
