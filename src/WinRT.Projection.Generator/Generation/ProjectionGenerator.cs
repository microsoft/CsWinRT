// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
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
    /// <param name="responseFilePath">The path to the response file to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string responseFilePath, CancellationToken token)
    {
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

        ProjectionGeneratorProcessingState processingState;

        // Process all .winmd references and create the .rsp file for 'cswinrt.exe'
        try
        {
            ConsoleApp.Log($"Processing {args.WinMDPaths.Length + 1} .winmd references");

            processingState = ProcessReferences(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("processing", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Invoke 'cswinrt.exe' to generate the projection sources
        try
        {
            ConsoleApp.Log("Generating merged projection code");

            GenerateSources(args, processingState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("source-generation", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Invoke Roslyn to compile the generated sources into 'WinRT.Projection.dll'
        try
        {
            ConsoleApp.Log("Compiling merged projection");

            Emit(args, processingState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Projection code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, ProjectionAssemblyName)}.dll");
    }
}