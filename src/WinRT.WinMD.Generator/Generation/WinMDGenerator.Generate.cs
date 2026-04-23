// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGenerator"/>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// Generates and writes the WinMD file from the discovered types.
    /// </summary>
    private static void Generate(WinMDGeneratorArgs args, WinMDGeneratorDiscoveryState state)
    {
        TypeMapper mapper = new(args.UseWindowsUIXamlProjections);

        WinMDWriter writer = new(
            state.AssemblyName,
            args.AssemblyVersion,
            mapper,
            state.InputModule);

        foreach (TypeDefinition type in state.PublicTypes)
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
    }
}