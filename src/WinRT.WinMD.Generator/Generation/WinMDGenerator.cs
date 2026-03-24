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