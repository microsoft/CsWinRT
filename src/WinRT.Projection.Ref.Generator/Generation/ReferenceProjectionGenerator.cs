// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.GeneratorCli;
using WindowsRuntime.GeneratorCli.Errors;
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
        string responseFilePath = inputFilePath;
        bool isUsingDebugRepro = false;

        // Load the debug repro to investigate with, if we have one
        try
        {
            // If no debug repro directory was provided, we have nothing to do.
            // This is fully expected, it just means no debug repro is needed.
            if (Path.GetExtension(Path.Normalize(inputFilePath)) == ".zip")
            {
                ConsoleApp.Log("Unpacking input 'cswinrtprojectionrefgen' debug repro");

                isUsingDebugRepro = true;

                // If we unpacked a debug repro, we'll also replace the input file
                // path with the extracted response file from the input repro.
                responseFilePath = UnpackDebugRepro(inputFilePath, token);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledReferenceProjectionGeneratorException("unpack-debug-repro", e);
        }

        token.ThrowIfCancellationRequested();

        ReferenceProjectionGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = ReferenceProjectionGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledReferenceProjectionGeneratorException("parsing", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Save a debug repro, if needed
        try
        {
            // If no debug repro directory was provided, we have nothing to do.
            // This is fully expected, it just means no debug repro is needed.
            // We also skip this if we're currently processing an input debug
            // repro, as there would be no point in creating a new one from that.
            if (args.DebugReproDirectory is not null && !isUsingDebugRepro)
            {
                ConsoleApp.Log("Saving 'cswinrtprojectionrefgen' debug repro");

                SaveDebugRepro(args);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledReferenceProjectionGeneratorException("save-debug-repro", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Validate the target framework. CsWinRT 3.0 requires .NET 10 or later.
        if (!string.IsNullOrEmpty(args.TargetFramework) && !args.TargetFramework.StartsWith("net10.0", StringComparison.Ordinal))
        {
            throw WellKnownReferenceProjectionGeneratorExceptions.UnsupportedTargetFramework(args.TargetFramework);
        }

        // Build the writer options from the parsed arguments
        ProjectionWriterOptions options;

        try
        {
            options = BuildWriterOptions(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledReferenceProjectionGeneratorException("processing", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Invoke the projection writer (in-process) to generate the projection sources
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
