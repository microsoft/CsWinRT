// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Parsing;
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
        (InteropGeneratorArgs args, GeneratorPhaseRunner runner) = GeneratorHost.Prepare<InteropGeneratorArgs>(
            inputFilePath: inputFilePath,
            toolName: "cswinrtinteropgen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: ResponseFileParser.Parse<InteropGeneratorArgs, WellKnownInteropExceptions>,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledInteropException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        // Discover the types to process
        InteropGeneratorDiscoveryState discoveryState = runner.RunPhase(
            phaseName: "discovery",
            logMessage: $"Processing {args.ReferenceAssemblyPaths.Length + args.ImplementationAssemblyPaths.Length + 1} module(s)",
            body: () => Discover(args));

        args.Token.ThrowIfCancellationRequested();

        // Emit the resulting interop assembly
        runner.RunPhase(
            phaseName: "emit",
            logMessage: "Generating interop code",
            body: () => Emit(args, discoveryState));

        // Notify the user that generation was successful
        ConsoleApp.Log($"Interop code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)}");
    }
}