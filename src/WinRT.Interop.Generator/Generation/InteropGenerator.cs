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
            if (Path.GetExtension(inputFilePath) == ".zip")
            {
                ConsoleApp.Log("Unpacking input 'cswinrtgen' debug repro");

                isUsingDebugRepro = true;

                // If we unpacked a debug repro, we'll also replace the input file
                // path with the extracted response file from the input repro.
                responseFilePath = UnpackDebugRepro(inputFilePath, token);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("unpack-debug-repro", e);
        }

        token.ThrowIfCancellationRequested();

        InteropGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = InteropGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("parsing", e);
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
                ConsoleApp.Log("Saving 'cswinrtgen' debug repro");

                SaveDebugRepro(args);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("save-debug-repro", e);
        }

        args.Token.ThrowIfCancellationRequested();

        InteropGeneratorDiscoveryState discoveryState;

        // Wrap the actual logic, to ensure that we're only ever throwing an exception that will result
        // in either graceful cancellation, or a well formatted error message. The 'ConsoleApp' code is
        // taking care of passing the exception 'ToString()' result to the output buffer, so we want all
        // exceptions that can reach that path to have our custom formatting implementation there.
        try
        {
            ConsoleApp.Log($"Processing {args.ReferenceAssemblyPaths.Length + 1} modules");

            discoveryState = Discover(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("discovery", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Same thing for the emit phase
        try
        {
            ConsoleApp.Log("Generating interop code");

            Emit(args, discoveryState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledInteropException("emit", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Interop code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.InteropDllName)}");
    }
}