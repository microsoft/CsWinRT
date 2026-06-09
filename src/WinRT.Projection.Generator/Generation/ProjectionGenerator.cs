// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT projection .dll generator.
/// </summary>
internal static partial class ProjectionGenerator
{
    /// <summary>
    /// Runs the projection generator to produce the resulting <c>WinRT.Projection.dll</c> assembly.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file or debug repro to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        GeneratorPhaseRunner<ProjectionGeneratorArgs> runner = GeneratorHost.CreateRunner<ProjectionGeneratorArgs>(
            inputFilePath: inputFilePath,
            toolName: "cswinrtprojectiongen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: ResponseFileParser.Parse<ProjectionGeneratorArgs, WellKnownProjectionGeneratorExceptions>,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledProjectionGeneratorException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        // Process all .winmd references. Show the appropriate message to inform users of what this
        // generator is doing, based on the input arguments. If we don't have precompiled projections,
        // this tool might run up to 3 times during builds, so this helps make things clearer.
        ProjectionGeneratorProcessingState processingState = runner.RunPhase(
            phaseName: "processing",
            logMessage: runner.Args switch
            {
                { WindowsSdkOnly: true, WindowsUIXamlProjection: false } => "Processing Windows SDK .winmd references",
                { WindowsSdkOnly: true, WindowsUIXamlProjection: true } => "Processing 'Windows.UI.Xaml' .winmd references",
                _ => $"Processing {runner.Args.WinMDPaths.Length} .winmd reference(s)"
            },
            body: ProcessReferences);

        runner.Args.Token.ThrowIfCancellationRequested();

        // If no types were found to project (e.g., component mode with no component references),
        // skip the source generation and emit phases entirely (no .dll will be produced at all).
        if (!processingState.HasTypesToProject)
        {
            return;
        }

        // Invoke the projection writer (in-process) to generate the projection sources
        runner.RunPhase(
            phaseName: "source-generation",
            logMessage: "Generating projection code",
            body: _ => GenerateSources(processingState));

        runner.Args.Token.ThrowIfCancellationRequested();

        // Invoke Roslyn to compile the generated sources into 'WinRT.Projection.dll'
        runner.RunPhase(
            phaseName: "emit",
            logMessage: "Compiling projection code",
            body: args => Emit(args, processingState));

        // Notify the user that generation was successful
        ConsoleApp.Log($"Projection code generated -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, runner.Args.AssemblyName)}.dll");
    }
}