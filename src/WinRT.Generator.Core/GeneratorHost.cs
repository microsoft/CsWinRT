// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.Generator;

/// <summary>
/// Shared <c>Run</c> entry-point scaffold for the CsWinRT CLI generators.
/// </summary>
/// <remarks>
/// Each generator's <c>Run</c> method historically opened with an identical preamble:
/// <list type="number">
///   <item>If the input file path looks like a <c>.zip</c>, unpack a debug repro and re-route to the extracted <c>.rsp</c>.</item>
///   <item>Parse the response file into a per-tool args record.</item>
///   <item>If <c>DebugReproDirectory</c> is set and we are not already replaying, save a debug repro of the current invocation.</item>
/// </list>
/// <see cref="Prepare{TArgs}"/> encapsulates that preamble. Each generator's <c>Run</c> now starts with a
/// single call to it; the per-tool unpack / save / parse logic is supplied via delegates so behavior
/// stays identical (same log messages, same exception phases, same per-tool unhandled exception type).
/// </remarks>
internal static class GeneratorHost
{
    /// <summary>
    /// Runs the shared unpack → parse → save preamble for a CsWinRT CLI generator.
    /// </summary>
    /// <typeparam name="TArgs">The per-tool args record (must implement <see cref="IGeneratorArgs"/>).</typeparam>
    /// <param name="inputFilePath">The input file path (response file or debug-repro <c>.zip</c>).</param>
    /// <param name="toolName">The tool name used in <paramref name="log"/> messages (e.g. <c>"cswinrtimplgen"</c>).</param>
    /// <param name="unpackDebugRepro">Extracts a debug-repro <c>.zip</c> to a temp folder and returns the rewritten response file path.</param>
    /// <param name="parseFromResponseFile">Parses a response file at the given path into <typeparamref name="TArgs"/>.</param>
    /// <param name="saveDebugRepro">Saves a debug repro of the current invocation (called only when <see cref="IGeneratorArgs.DebugReproDirectory"/> is set and we are not already replaying).</param>
    /// <param name="wrapUnhandled">Wraps an unexpected exception into the per-tool <c>Unhandled*Exception</c> with the given phase name.</param>
    /// <param name="log">Logs a progress message to the user (typically <c>ConsoleApp.Log</c> from ConsoleAppFramework).</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The parsed <typeparamref name="TArgs"/> instance.</returns>
    public static TArgs Prepare<TArgs>(
        string inputFilePath,
        string toolName,
        Func<string, CancellationToken, string> unpackDebugRepro,
        Func<string, CancellationToken, TArgs> parseFromResponseFile,
        Action<TArgs> saveDebugRepro,
        Func<string, Exception, Exception> wrapUnhandled,
        Action<string> log,
        CancellationToken token)
        where TArgs : IGeneratorArgs
    {
        string responseFilePath = inputFilePath;
        bool isUsingDebugRepro = false;

        // Load the debug repro to investigate with, if we have one
        try
        {
            // If the input file path is a .zip, treat it as a debug repro and unpack
            // it. Any other extension is treated as a regular response file.
            if (Path.GetExtension(Path.Normalize(inputFilePath)) == ".zip")
            {
                log($"Unpacking input '{toolName}' debug repro");

                isUsingDebugRepro = true;

                // If we unpacked a debug repro, we'll also replace the input file
                // path with the extracted response file from the input repro.
                responseFilePath = unpackDebugRepro(inputFilePath, token);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled("unpack-debug-repro", e);
        }

        token.ThrowIfCancellationRequested();

        TArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = parseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled("parsing", e);
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
                log($"Saving '{toolName}' debug repro");

                saveDebugRepro(args);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw wrapUnhandled("save-debug-repro", e);
        }

        args.Token.ThrowIfCancellationRequested();

        return args;
    }
}

