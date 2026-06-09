// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.GeneratorCli;
using WindowsRuntime.GeneratorCli.Errors;
using WindowsRuntime.WinMDGenerator.Errors;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT WinMD generator.
/// </summary>
/// <remarks>
/// <para>
/// This generator converts a compiled .NET assembly into a Windows Runtime metadata (<c>.winmd</c>) file.
/// It analyzes the public API surface of the assembly and emits the equivalent Windows Runtime type definitions,
/// handling all necessary type mappings (e.g., .NET collection interfaces → Windows Runtime collection interfaces),
/// synthesized interfaces, custom attributes, and Windows Runtime naming conventions.
/// </para>
/// <para>
/// The generation process runs in three phases:
/// </para>
/// <list type="number">
///   <item><strong>Parse</strong>: Read arguments from the response file via <see cref="WinMDGeneratorArgs.ParseFromResponseFile(string, CancellationToken)"/>.</item>
///   <item><strong>Discover</strong>: Load the input assembly and discover public types via <see cref="Discover"/>.</item>
///   <item><strong>Generate</strong>: Transform discovered types and write the WinMD file via <see cref="Generate"/>.</item>
/// </list>
/// </remarks>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// Runs the WinMD generator to produce a <c>.winmd</c> file from a compiled assembly.
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
                ConsoleApp.Log("Unpacking input 'cswinrtwinmdgen' debug repro");

                isUsingDebugRepro = true;

                // If we unpacked a debug repro, we'll also replace the input file
                // path with the extracted response file from the input repro.
                responseFilePath = UnpackDebugRepro(inputFilePath, token);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("unpack-debug-repro", e);
        }

        token.ThrowIfCancellationRequested();

        WinMDGeneratorArgs args;

        // Parse the arguments from the response file
        try
        {
            args = WinMDGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("parsing", e);
        }

        token.ThrowIfCancellationRequested();

        // Save a debug repro, if needed
        try
        {
            // If no debug repro directory was provided, we have nothing to do.
            // This is fully expected, it just means no debug repro is needed.
            // We also skip this if we're currently processing an input debug
            // repro, as there would be no point in creating a new one from that.
            if (args.DebugReproDirectory is not null && !isUsingDebugRepro)
            {
                ConsoleApp.Log("Saving 'cswinrtwinmdgen' debug repro");

                SaveDebugRepro(args);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("save-debug-repro", e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Discover the types to process
        WinMDGeneratorDiscoveryState discoveryState;

        try
        {
            ConsoleApp.Log($"Processing assembly: '{System.IO.Path.GetFileName(args.InputAssemblyPath)}'");

            discoveryState = Discover(args);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("discovery", e);
        }

        token.ThrowIfCancellationRequested();

        // Generate and write the .winmd file
        try
        {
            ConsoleApp.Log($"Defining {discoveryState.PublicTypes.Count} authored type(s)");

            Generate(args, discoveryState);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledWinMDException("generation", e);
        }

        ConsoleApp.Log($"Windows Runtime assembly (.winmd) generated -> {args.OutputWinmdPath}");
    }
}