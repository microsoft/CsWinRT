// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.WinMDGenerator.Helpers;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGenerator"/>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// Generates and writes the WinMD file from the discovered types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates a <see cref="TypeMapper"/> configured for the target XAML framework, then uses
    /// <see cref="WinMDWriter"/> to process each discovered public type into WinMD metadata.
    /// After all types are processed, <see cref="WinMDWriter.FinalizeGeneration"/> is called
    /// to add <c>MethodImpl</c> fixups, version attributes, custom attributes, and overload attributes.
    /// </para>
    /// <para>
    /// The output directory is created if it does not exist, and the WinMD file is written to
    /// the path specified in <paramref name="args"/>.
    /// </para>
    /// </remarks>
    /// <param name="args">The parsed generator arguments.</param>
    /// <param name="state">The discovery state containing the loaded module and public types.</param>
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