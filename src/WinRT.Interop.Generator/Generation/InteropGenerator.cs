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

        (ModuleDefinition module, ModuleDefinition windowsRuntimeModule) = runner.RunPhase(
            phaseName: "prepare emission",
            body: args =>
            {
                NormalizeAssemblyReferences(discoveryState);

                ModuleDefinition module = DefineInteropModule(args, discoveryState, out ModuleDefinition runtime, out _);

                return (module, runtime);
            });

        byte[]? fingerprint = runner.Args.EnableIncrementalGeneration
            ? runner.RunPhase(
                phaseName: "fingerprint",
                logMessage: "Checking interop generation cache",
                body: args => InteropGenerationFingerprint.TryCreate(args, discoveryState))
            : null;

        if (fingerprint is not null &&
            runner.RunPhase("cache lookup", args => InteropGenerationCache.TryReuse(args, fingerprint, module.Mvid)))
        {
            ConsoleApp.Log($"Reusing cached interop code -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)}");

            return;
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