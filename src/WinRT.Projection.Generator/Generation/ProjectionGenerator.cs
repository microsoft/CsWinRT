// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.GeneratorCli;
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
        string responseFilePath = inputFilePath;
        bool isUsingDebugRepro = false;

        // Load the debug repro to investigate with, if we have one
        try
        {
            // If no debug repro directory was provided, we have nothing to do.
            // This is fully expected, it just means no debug repro is needed.
            if (Path.GetExtension(Path.Normalize(inputFilePath)) == ".zip")
            {
                ConsoleApp.Log("Unpacking input 'cswinrtprojectiongen' debug repro");

                isUsingDebugRepro = true;

                // If we unpacked a debug repro, we'll also replace the input file
                // path with the extracted response file from the input repro.
                responseFilePath = UnpackDebugRepro(inputFilePath, token);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("unpack-debug-repro", e);
        }

        token.ThrowIfCancellationRequested();

        ProjectionGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = ProjectionGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("parsing", e);
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
                ConsoleApp.Log("Saving 'cswinrtprojectiongen' debug repro");

                SaveDebugRepro(args);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("save-debug-repro", e);
        }

        args.Token.ThrowIfCancellationRequested();

        ProjectionGeneratorProcessingState processingState;

        // Process all .winmd references
        try
        {
            // Show the appropriate message to inform users of what this generator is doing,
            // based on the input arguments. If we don't have precompiled projections, this
            // tool might run up to 3 times during builds, so this helps make things clearer.
            ConsoleApp.Log(args switch
            {
                { WindowsSdkOnly: true, WindowsUIXamlProjection: false } => "Processing Windows SDK .winmd references",
                { WindowsSdkOnly: true, WindowsUIXamlProjection: true } => "Processing 'Windows.UI.Xaml' .winmd references",
                _ => $"Processing {args.WinMDPaths.Length} .winmd reference(s)"
            });

            processingState = ProcessReferences(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("processing", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // If no types were found to project (e.g., component mode with no component references),
        // skip the source generation and emit phases entirely (no .dll will be produced at all).
        if (!processingState.HasTypesToProject)
        {
            return;
        }

        // Invoke the projection writer (in-process) to generate the projection sources
        try
        {
            ConsoleApp.Log("Generating projection code");

            GenerateSources(processingState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("source-generation", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Invoke Roslyn to compile the generated sources into 'WinRT.Projection.dll'
        try
        {
            ConsoleApp.Log("Compiling projection code");

            Emit(args, processingState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Projection code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, args.AssemblyName)}.dll");
    }
}