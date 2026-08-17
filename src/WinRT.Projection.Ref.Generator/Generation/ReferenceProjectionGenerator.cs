// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.ProjectionWriter;
using WindowsRuntime.ReferenceProjectionGenerator.Errors;

namespace WindowsRuntime.ReferenceProjectionGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT reference projection source generator. Invoked by the
/// <c>CsWinRTGenerateProjection</c> MSBuild target to produce <c>.cs</c> files that get compiled
/// into the user's library/component <c>.dll</c>.
/// </summary>
internal static partial class ReferenceProjectionGenerator
{
    /// <summary>
    /// Runs the reference projection source generator.
    /// </summary>
    /// <param name="inputFilePath">The path to the response file or debug repro to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string inputFilePath, CancellationToken token)
    {
        GeneratorPhaseRunner<ReferenceProjectionGeneratorArgs> runner = GeneratorHost.CreateRunner(
            inputFilePath: inputFilePath,
            toolName: "cswinrtprojectionrefgen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: ResponseFileParser.Parse<ReferenceProjectionGeneratorArgs, WellKnownReferenceProjectionGeneratorExceptions>,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledReferenceProjectionGeneratorException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        // Validate the target framework. CsWinRT 3.0 requires .NET 10 or later
        if (!string.IsNullOrEmpty(runner.Args.TargetFramework) && !runner.Args.TargetFramework.StartsWith("net10.0", StringComparison.Ordinal))
        {
            throw WellKnownReferenceProjectionGeneratorExceptions.UnsupportedTargetFramework(runner.Args.TargetFramework);
        }

        // Build the writer options from the parsed arguments
        ProjectionWriterOptions options = runner.RunPhase(
            phaseName: "processing",
            body: BuildWriterOptions);

        // Invoke the projection writer (in-process) to generate the projection sources. We can't
        // route this through the shared 'runner.RunPhase' helper because we wrap the exception
        // into a well-known 'CsWinRTProcessError' rather than the per-tool 'Unhandled' factory.
        try
        {
            ConsoleApp.Log($"Generating reference projection sources -> {options.OutputFolder}");

            ProjectionWriter.ProjectionWriter.Run(options);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownReferenceProjectionGeneratorExceptions.CsWinRTProcessError(e);
        }
    }

    /// <summary>
    /// Builds the <see cref="ProjectionWriterOptions"/> from the parsed args.
    /// </summary>
    /// <param name="args">The parsed args.</param>
    /// <returns>The resulting <see cref="ProjectionWriterOptions"/>.</returns>
    private static ProjectionWriterOptions BuildWriterOptions(ReferenceProjectionGeneratorArgs args)
    {
        // Make sure the output directory exists. ProjectionWriter.Run will also create it but creating
        // it here matches the OLD target's '<MakeDir Directories="..."/>' step.
        _ = Directory.CreateDirectory(args.OutputDirectory);

        return new ProjectionWriterOptions
        {
            InputPaths = args.InputPaths,
            OutputFolder = args.OutputDirectory,
            Include = args.IncludeNamespaces,
            Exclude = args.ExcludeNamespaces,
            AdditionExclude = args.AdditionExcludeNamespaces,
            Verbose = args.Verbose,
            Component = args.Component,
            PublicExclusiveTo = args.PublicExclusiveTo,
            IdicExclusiveTo = args.IdicExclusiveTo,
            ReferenceProjection = args.ReferenceProjection,
            CancellationToken = args.Token,
        };
    }
}
