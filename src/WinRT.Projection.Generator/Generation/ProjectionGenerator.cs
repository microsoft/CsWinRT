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
        GeneratorPhaseRunner<ProjectionGeneratorArgs> runner = GeneratorHost.CreateRunner(
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

        // Nothing to project and nothing to merge: skip the remaining phases entirely (no .dll will be
        // produced at all). An assembly implementing classes declared in existing metadata contributes no
        // types to project, but its activation entry point still has to be merged, so it is not "nothing".
        if (!processingState.HasTypesToProject && processingState.ComponentAssemblyNames.Count == 0)
        {
            return;
        }

        // Invoke the projection writer (in-process) to generate the projection sources
        if (processingState.HasTypesToProject)
        {
            runner.RunPhase(
                phaseName: "source-generation",
                logMessage: "Generating projection code",
                body: _ => GenerateSources(processingState));
        }

        // In component mode (i.e. producing 'WinRT.Component.dll'), emit the supporting source files
        // alongside the projection writer's output so the merged '.dll' plays the entry-assembly and
        // merged-activation roles (interop type map union, 'SetEntryAssembly' module init, merged
        // 'ABI.WinRT.Component.ManagedExports.GetActivationFactory', and AOT native export).
        runner.RunPhase(
            phaseName: "winrt-component-sources",
            body: args => EmitWinRTComponentSources(args, processingState));

        // Invoke Roslyn to compile the generated sources into 'WinRT.Projection.dll'
        runner.RunPhase(
            phaseName: "emit",
            logMessage: "Compiling projection code",
            body: args => Emit(args, processingState));

        // Notify the user that generation was successful
        ConsoleApp.Log($"Projection code generated -> {Path.Combine(runner.Args.GeneratedAssemblyDirectory, runner.Args.AssemblyName)}.dll");
    }
}