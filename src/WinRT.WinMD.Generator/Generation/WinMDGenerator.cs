// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.WinMDGenerator.Errors;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT WinMD generator.
/// </summary>
/// <remarks>
/// <para>
/// This generator converts a compiled .NET assembly into a Windows Runtime metadata (<c>.winmd</c>) file.
/// It analyzes the public API surface of the assembly and emits the equivalent Windows Runtime type definitions,
/// handling all necessary type mappings (e.g., .NET collection interfaces → Windows Runtime collection interfaces),
/// synthesized interfaces, custom attributes, and Windows Runtime naming conventions.
/// </para>
/// <para>
/// The generation process runs in three phases:
/// </para>
/// <list type="number">
///   <item><strong>Parse</strong>: Read arguments from the response file via <see cref="WinMDGeneratorArgs.ParseFromResponseFile(string, CancellationToken)"/>.</item>
///   <item><strong>Discover</strong>: Load the input assembly and discover public types via <see cref="Discover"/>.</item>
///   <item><strong>Generate</strong>: Transform discovered types and write the WinMD file via <see cref="Generate"/>.</item>
/// </list>
/// </remarks>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// Runs the WinMD generator to produce a <c>.winmd</c> file from a compiled assembly.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        WinMDGeneratorArgs args;

        // Phase 1: Parse the actual arguments from the response file
        try
        {
            args = WinMDGeneratorArgs.ParseFromResponseFile(inputFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("parsing", e);
        }

        token.ThrowIfCancellationRequested();

        ConsoleApp.Log($"Generating WinMD for assembly: {System.IO.Path.GetFileName(args.InputAssemblyPath)}");
        ConsoleApp.Log($"Output: {args.OutputWinmdPath}");

        // Phase 2: Load and discover
        WinMDGeneratorDiscoveryState discoveryState;

        try
        {
            discoveryState = Discover(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("discovery", e);
        }

        token.ThrowIfCancellationRequested();

        ConsoleApp.Log($"Found {discoveryState.PublicTypes.Count} public types");

        // Phase 3: Generate and write the WinMD
        try
        {
            Generate(args, discoveryState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("generation", e);
        }

        ConsoleApp.Log($"WinMD generated successfully: {args.OutputWinmdPath}");
    }
}