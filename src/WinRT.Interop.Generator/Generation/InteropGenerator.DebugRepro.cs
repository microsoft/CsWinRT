// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the debug repro unpack logic for the generator.
    /// </summary>
    /// <param name="path">The path to the debug repro file to unpack.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The path to the resulting response file to use.</returns>
    private static string UnpackDebugRepro(string path, CancellationToken token)
    {
        // Create a temporary directory to extract the files from the debug repro
        string tempFolderName = $"cswinrtgen-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtgen.rsp");
        ZipArchiveEntry originalPathsEntry = archive.Entries.Single(entry => entry.Name == "original-paths.json");
        ZipArchiveEntry[] dllEntries = [.. archive.Entries.Where(entry => Path.GetExtension(entry.Name) == ".dll")];

        token.ThrowIfCancellationRequested();

        InteropGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = InteropGeneratorArgs.ParseFromResponseFile(stream, token);
        }

        token.ThrowIfCancellationRequested();

        Dictionary<string, string> originalPaths;

        // Load the mapping with all the original file paths for the included .dll-s
        using (Stream stream = originalPathsEntry.Open())
        {
            originalPaths = JsonSerializer.Deserialize(stream, InteropGeneratorJsonSerializerContext.Default.DictionaryStringString)!;
        }

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        string? outputAssemblyPath = null;

        // Extract all .dll-s, one per directory, so we can ensure there's no name conflicts
        foreach ((int index, ZipArchiveEntry dllEntry) in dllEntries.Index())
        {
            string destinationFolder = Path.Combine(tempDirectory, index.ToString("000"));

            _ = Directory.CreateDirectory(destinationFolder);

            // Construct the path in the temporary subfolder with the original .dll name
            string originalPath = originalPaths[dllEntry.Name];
            string originalName = Path.GetFileName(originalPath);
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the .dll to the new destination path
            dllEntry.ExtractToFile(destinationPath, overwrite: true);

            // Track all extracted reference paths, as well as the output assembly path
            if (originalPath == args.OutputAssemblyPath)
            {
                outputAssemblyPath = destinationPath;
            }
            else
            {
                referencePaths.Add(destinationPath);
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. referencePaths],
            OutputAssemblyPath = outputAssemblyPath!,
            GeneratedAssemblyDirectory = Path.Combine(tempDirectory, "bin"),
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            ValidateWinRTRuntimeAssemblyVersion = args.ValidateWinRTRuntimeAssemblyVersion,
            ValidateWinRTRuntimeDllVersion2References = args.ValidateWinRTRuntimeDllVersion2References,
            EnableIncrementalGeneration = args.EnableIncrementalGeneration,
            TreatWarningsAsErrors = args.TreatWarningsAsErrors,
            MaxDegreesOfParallelism = args.MaxDegreesOfParallelism,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        // Return the resulting .rsp file so it can be used to replay the debug repro
        return rspFilePath;
    }

    /// <summary>
    /// Runs the debug repro save logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void SaveDebugRepro(InteropGeneratorArgs args)
    {
        // We expect callers to have already performed this check, but just in case
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        // The target folder must exist
        if (!Directory.Exists(args.DebugReproDirectory))
        {
            throw WellKnownInteropExceptions.DebugReproDirectoryDoesNotExist(args.DebugReproDirectory);
        }

        // Path for the ZIP archive
        string zipPath = Path.Combine(args.DebugReproDirectory, "debug-repro.zip");

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"cswinrtgen-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        // List to store the updated DLL names for the .rsp file
        List<string> updatedDllNames = [];

        // Map with all the original paths
        Dictionary<string, string> originalPaths = new(args.ReferenceAssemblyPaths.Length + 1);

        // Add all reference assemblies to the temp directory with hashed names
        foreach (string referenceAssemblyPath in args.ReferenceAssemblyPaths)
        {
            args.Token.ThrowIfCancellationRequested();

            string hashedName = GetHashedFileName(referenceAssemblyPath);
            string destinationPath = Path.Combine(tempDirectory, hashedName);

            File.Copy(referenceAssemblyPath, destinationPath, overwrite: true);

            updatedDllNames.Add(hashedName);
            originalPaths.Add(hashedName, referenceAssemblyPath);
        }

        args.Token.ThrowIfCancellationRequested();

        // Add the output assembly to the temp directory with a hashed name
        string outputAssemblyHashedName = GetHashedFileName(args.OutputAssemblyPath);
        string outputAssemblyDestination = Path.Combine(tempDirectory, outputAssemblyHashedName);

        File.Copy(args.OutputAssemblyPath, outputAssemblyDestination, overwrite: true);

        args.Token.ThrowIfCancellationRequested();

        originalPaths.Add(outputAssemblyHashedName, args.OutputAssemblyPath);

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. updatedDllNames],
            OutputAssemblyPath = outputAssemblyHashedName,
            GeneratedAssemblyDirectory = args.GeneratedAssemblyDirectory,
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            ValidateWinRTRuntimeAssemblyVersion = args.ValidateWinRTRuntimeAssemblyVersion,
            ValidateWinRTRuntimeDllVersion2References = args.ValidateWinRTRuntimeDllVersion2References,
            EnableIncrementalGeneration = args.EnableIncrementalGeneration,
            TreatWarningsAsErrors = args.TreatWarningsAsErrors,
            MaxDegreesOfParallelism = args.MaxDegreesOfParallelism,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the original paths
        string jsonFilePath = Path.Combine(tempDirectory, "original-paths.json");

        // Serialize the original paths
        using (Stream jsonStream = File.Create(jsonFilePath))
        {
            JsonSerializer.Serialize(jsonStream, originalPaths, InteropGeneratorJsonSerializerContext.Default.DictionaryStringString);
        }

        args.Token.ThrowIfCancellationRequested();

        // Delete the previous file, if it exists
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        // Create the actual .zip file in the target directory
        ZipFile.CreateFromDirectory(tempDirectory, zipPath);

        // Clean up the temporary directory
        Directory.Delete(tempDirectory, recursive: true);
    }

    /// <summary>
    /// Generates a hashed filename by appending a hash of the original filename.
    /// </summary>
    /// <param name="filePath">The original file path.</param>
    /// <returns>The hashed filename.</returns>
    private static string GetHashedFileName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        byte[] utf8Data = Encoding.UTF8.GetBytes(filePath);
        byte[] hashData = Shake128.HashData(utf8Data, outputLength: 16);
        string hash = Convert.ToHexString(hashData);

        return $"{Path.GetFileNameWithoutExtension(fileName)}_{hash}{Path.GetExtension(fileName)}";
    }
}
