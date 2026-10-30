// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using AsmResolver.DotNet;
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
        GeneratorPhaseRunner<InteropGeneratorArgs> runner = GeneratorHost.CreateRunner(
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
            logMessage: $"Processing {runner.Args.ReferenceAssemblyPaths.Length + runner.Args.ImplementationAssemblyPaths.Length + 1} module(s)",
            body: Discover);

        // Prepare the output module (normalizing references first, so the fingerprint sees the same metadata as emit)
        (ModuleDefinition module, ModuleDefinition windowsRuntimeModule) = runner.RunPhase(
            phaseName: "prepare emission",
            state: discoveryState,
            body: PrepareEmission);

        byte[]? fingerprint = null;

        // The optional report includes emitted type and IL counts, so it requires fresh emission
        if (runner.Args.EnableIncrementalGeneration && runner.Args.LogDirectory is null)
        {
            // Compute the fingerprint of the emission inputs
            fingerprint = runner.RunPhase(
                phaseName: "fingerprint",
                logMessage: "Checking interop generation cache",
                body: args => InteropGenerationFingerprint.Create(args, discoveryState));

            // Reuse the previously generated interop assembly, if its emission inputs are unchanged
            if (runner.RunPhase("cache lookup", args => InteropGenerationCache.TryReuse(args, fingerprint, module.Mvid)))
            {
                ConsoleApp.Log($"Cached interop code -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)}");

                return;
            }
        }

        // Emit the resulting interop assembly
        runner.RunPhase(
            phaseName: "emit",
            logMessage: "Generating interop code",
            body: args => Emit(args, discoveryState, module, windowsRuntimeModule, fingerprint));

        // Notify the user that generation was successful
        ConsoleApp.Log($"Interop code generated -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)}");
    }
}