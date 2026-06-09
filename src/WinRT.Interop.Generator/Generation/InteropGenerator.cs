// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT interop .dll generator.
/// </summary>
internal static partial class InteropGenerator
{
    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file or debug repro to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        InteropGeneratorArgs args = GeneratorHost.Prepare<InteropGeneratorArgs>(
            inputFilePath: inputFilePath,
            toolName: "cswinrtinteropgen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: InteropGeneratorArgs.ParseFromResponseFile,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledInteropException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        InteropGeneratorDiscoveryState discoveryState;

        // Wrap the actual logic, to ensure that we're only ever throwing an exception that will result
        // in either graceful cancellation, or a well formatted error message. The 'ConsoleApp' code is
        // taking care of passing the exception 'ToString()' result to the output buffer, so we want all
        // exceptions that can reach that path to have our custom formatting implementation there.
        try
        {
            ConsoleApp.Log($"Processing {args.ReferenceAssemblyPaths.Length + args.ImplementationAssemblyPaths.Length + 1} module(s)");

            discoveryState = Discover(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("discovery", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Same thing for the emit phase
        try
        {
            ConsoleApp.Log("Generating interop code");

            Emit(args, discoveryState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Interop code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)}");
    }
}