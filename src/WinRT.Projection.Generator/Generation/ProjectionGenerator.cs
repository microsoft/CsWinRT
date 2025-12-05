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

        try
        {
            Emit(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionGeneratorException("emit", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Notify the user that generation was successful
        ConsoleApp.Log($"Projection code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, ProjectionAssemblyName)}");
    }
}