// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.ProjectionWriter;
using WindowsRuntime.ProjectionWriter.Helpers;
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
        (ReferenceProjectionGeneratorArgs args, GeneratorPhaseRunner runner) = GeneratorHost.Prepare<ReferenceProjectionGeneratorArgs>(
            inputFilePath: inputFilePath,
            toolName: "cswinrtprojectionrefgen",
            unpackDebugRepro: UnpackDebugRepro,
            parseFromResponseFile: ResponseFileParser.Parse<ReferenceProjectionGeneratorArgs, WellKnownReferenceProjectionGeneratorExceptions>,
            saveDebugRepro: SaveDebugRepro,
            wrapUnhandled: static (phase, e) => new UnhandledReferenceProjectionGeneratorException(phase, e),
            log: ConsoleApp.Log,
            token: token);

        // Validate the target framework. CsWinRT 3.0 requires .NET 10 or later.
        if (!string.IsNullOrEmpty(args.TargetFramework) && !args.TargetFramework.StartsWith("net10.0", StringComparison.Ordinal))
        {
            throw WellKnownReferenceProjectionGeneratorExceptions.UnsupportedTargetFramework(args.TargetFramework);
        }

        // Build the writer options from the parsed arguments
        ProjectionWriterOptions options = runner.RunPhase(
            phaseName: "processing",
            body: () => BuildWriterOptions(args));

        args.Token.ThrowIfCancellationRequested();

        // Invoke the projection writer (in-process) to generate the projection sources. We can't
        // route this through the shared 'runner.RunPhase' helper because we wrap the exception
        // into a well-known 'CsWinRTProcessError' rather than the per-tool 'Unhandled' factory.
        try
        {
            ConsoleApp.Log($"Generating reference projection sources -> {options.OutputFolder}");

            global::WindowsRuntime.ProjectionWriter.ProjectionWriter.Run(options);
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
        // Each input may be a literal file or directory path, or a special token like 'local',
        // 'sdk', 'sdk+', or '10.0.X.Y' which expands to a set of WinMD paths. Expand each input
        // through WindowsMetadataExpander so the writer always receives concrete paths.
        List<string> inputPaths = [];

        foreach (string input in args.InputPaths)
        {
            inputPaths.AddRange(WindowsMetadataExpander.Expand(input));
        }

        // Make sure the output directory exists. ProjectionWriter.Run will also create it but creating
        // it here matches the OLD target's '<MakeDir Directories="..."/>' step.
        _ = Directory.CreateDirectory(args.OutputDirectory);

        return new ProjectionWriterOptions
        {
            InputPaths = inputPaths,
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
