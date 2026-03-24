// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AsmResolver.DotNet;
using ConsoleAppFramework;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Models;
using WindowsRuntime.WinMDGenerator.Resolvers;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT WinMD generator.
/// </summary>
internal static class WinMDGenerator
{
    /// <summary>
    /// Runs the WinMD generator to produce a <c>.winmd</c> file from a compiled assembly.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        WinMDGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = WinMDGeneratorArgs.ParseFromResponseFile(inputFilePath, token);
        }
        catch (Exception e)
        {
            ConsoleApp.Log($"Error parsing response file: {e.Message}");
            throw;
        }

        token.ThrowIfCancellationRequested();

        ConsoleApp.Log($"Generating WinMD for assembly: {Path.GetFileName(args.InputAssemblyPath)}");
        ConsoleApp.Log($"Output: {args.OutputWinmdPath}");

        // Phase 1: Load the input assembly using AsmResolver
        ModuleDefinition inputModule;

        try
        {
            string[] allReferencePaths = [args.InputAssemblyPath, .. args.ReferenceAssemblyPaths];
            PathAssemblyResolver assemblyResolver = new(allReferencePaths);
            inputModule = ModuleDefinition.FromFile(args.InputAssemblyPath, assemblyResolver.ReaderParameters);
        }
        catch (Exception e)
        {
            ConsoleApp.Log($"Error loading input assembly: {e.Message}");
            throw WellKnownWinMDExceptions.WinMDGenerationError(e);
        }

        token.ThrowIfCancellationRequested();

        // Phase 2: Discover public types
        AssemblyAnalyzer analyzer = new(inputModule);
        IReadOnlyList<TypeDefinition> publicTypes = analyzer.DiscoverPublicTypes();

        ConsoleApp.Log($"Found {publicTypes.Count} public types");

        token.ThrowIfCancellationRequested();

        // Phase 3: Generate the WinMD
        try
        {
            string assemblyName = analyzer.AssemblyName;
            TypeMapper mapper = new(args.UseWindowsUIXamlProjections);

            WinmdWriter writer = new(
                assemblyName,
                args.AssemblyVersion,
                mapper,
                inputModule);

            foreach (TypeDefinition type in publicTypes)
            {
                writer.ProcessType(type);
            }

            writer.FinalizeGeneration();

            // Ensure output directory exists
            string? outputDir = Path.GetDirectoryName(args.OutputWinmdPath);
            if (outputDir != null)
            {
                _ = Directory.CreateDirectory(outputDir);
            }

            writer.Write(args.OutputWinmdPath);

            ConsoleApp.Log($"WinMD generated successfully: {args.OutputWinmdPath}");
        }
        catch (Exception e)
        {
            ConsoleApp.Log($"Error generating WinMD: {e.Message}");
            throw WellKnownWinMDExceptions.WinMDGenerationError(e);
        }
    }
}
