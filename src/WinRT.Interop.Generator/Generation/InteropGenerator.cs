// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
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
    /// <param name="referencePath">The input .dll paths.</param>
    /// <param name="assemblyPath">The path of the assembly that was built.</param>
    /// <param name="outputDirectory">The output path for the resulting assembly.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run(
        string[] referencePath,
        string assemblyPath,
        string outputDirectory,
        CancellationToken token)
    {
        RunCore(new InteropGeneratorArgs
        {
            ReferencePath = referencePath,
            AssemblyPath = assemblyPath,
            OutputDirectory = outputDirectory,
            Token = token
        });
    }

    /// <inheritdoc cref="Run"/>
    /// <param name="args">The arguments for this invocation.</param>
    private static void RunCore(InteropGeneratorArgs args)
    {
        InteropGeneratorState state;

        // Wrap the actual logic, to ensure that we're only ever throwing an exception that will result
        // in either graceful cancellation, or a well formatted error message. The 'ConsoleApp' code is
        // taking care of passing the exception 'ToString()' result to the output buffer, so we want all
        // exceptions that can reach that path to have our custom formatting implementation there.
        try
        {
            ConsoleApp.Log($"Processing {args.ReferencePath.Length + 1} modules...");

            state = Discover(args);
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw new UnhandledInteropException("discovery", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Same thing for the emit phase
        try
        {
            ConsoleApp.Log("Generating interop code...");

            Emit(args, state);
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw new UnhandledInteropException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Interop code generated -> {Path.Combine(args.OutputDirectory, WellKnownInteropNames.InteropDllName)}");
    }
}
