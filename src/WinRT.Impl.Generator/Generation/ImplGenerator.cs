// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.ImplGenerator.Errors;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <summary>
/// The implementation of the CsWinRT interop .dll generator.
/// </summary>
internal static partial class ImplGenerator
{
    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="responseFilePath">The path to the response file to use.</param>
    /// <param name="token">The token for the operation.</param>
    public static void Run([Argument] string responseFilePath, CancellationToken token)
    {
        ImplGeneratorArgs args;

        // Parse the actual arguments from the response file
        try
        {
            args = ImplGeneratorArgs.ParseFromResponseFile(responseFilePath, token);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledImplException("parsing", e);
        }

        // Notify the user that generation was successful
        ConsoleApp.Log($"Impl code generated -> {Path.Combine(args.GeneratedAssemblyDirectory, "test")}");
    }
}
