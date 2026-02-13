// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        ZipArchiveEntry[] dllEntries = [.. archive.Entries.Where(entry => Path.GetExtension(Path.Normalize(entry.Name)) == ".dll")];

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
        List<string> implementationPaths = [];
        string? outputAssemblyPath = null;
        string? winRTProjectionAssemblyHashedName = null;
        string? winRTComponentAssemblyHashedName = null;

        // Create another subdirectory for all the input assembly paths. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting .dll easier, without having to scroll past hundreds of folders.
        string assembliesDirectory = Path.Combine(tempDirectory, "in");

        // Extract all .dll-s, one per directory, so we can ensure there's no name conflicts
        foreach ((int index, ZipArchiveEntry dllEntry) in dllEntries.Index())
        {
            string destinationFolder = Path.Combine(assembliesDirectory, index.ToString("000"));

            _ = Directory.CreateDirectory(destinationFolder);

            // Construct the path in the temporary subfolder with the original .dll name
            string originalPath = originalPaths[dllEntry.Name];
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the .dll to the new destination path
            dllEntry.ExtractToFile(destinationPath, overwrite: true);

            // Track all extracted reference paths, as well as the output assembly path.
            // Note that the debug repro only uses filenames, not full paths, for .dll-s.
            // We also split reference paths and implementation paths in different folders.
            if (dllEntry.Name == args.OutputAssemblyPath)
            {
                outputAssemblyPath = destinationPath;
            }
            else if (Path.IsWithinDirectoryName(dllEntry.FullName, "references"))
            {
                referencePaths.Add(destinationPath);
            }
            else
            {
                implementationPaths.Add(destinationPath);
            }

            // Also track the private implementation detail .dll-s (these are also in the set of references)
            if (dllEntry.Name == args.WinRTProjectionAssemblyPath)
            {
                winRTProjectionAssemblyHashedName = destinationPath;
            }
            else if (args.WinRTComponentAssemblyPath is not null && dllEntry.Name == args.WinRTComponentAssemblyPath)
            {
                winRTComponentAssemblyHashedName = destinationPath;
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. referencePaths],
            ImplementationAssemblyPaths = [.. implementationPaths],
            OutputAssemblyPath = outputAssemblyPath!,
            WinRTProjectionAssemblyPath = winRTProjectionAssemblyHashedName!,
            WinRTComponentAssemblyPath = winRTComponentAssemblyHashedName,
            GeneratedAssemblyDirectory = tempDirectory,
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
        string referencesDirectory = Path.Combine(tempDirectory, "references");
        string implementationDirectory = Path.Combine(tempDirectory, "implementation");

        _ = Directory.CreateDirectory(tempDirectory);
        _ = Directory.CreateDirectory(referencesDirectory);
        _ = Directory.CreateDirectory(implementationDirectory);

        // Map with all the original paths
        Dictionary<string, string> originalPaths = new(args.ReferenceAssemblyPaths.Length + 1);

        // Add all reference and implementation paths with hashed names to the respective subdirectories under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceDllNames = CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referencesDirectory, originalPaths, args.Token);
        List<string> updatedImplementationDllNames = CopyHashedFilesToDirectory(args.ImplementationAssemblyPaths, implementationDirectory, originalPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the well known assemblies we use as input
        string outputAssemblyHashedName = CopyHashedFileToDirectory(args.OutputAssemblyPath, tempDirectory, originalPaths, args.Token);
        string winRTProjectionAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTProjectionAssemblyPath, tempDirectory, originalPaths, args.Token);
        string? winRTComponentAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTComponentAssemblyPath, tempDirectory, originalPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. updatedReferenceDllNames],
            ImplementationAssemblyPaths = [.. updatedImplementationDllNames],
            OutputAssemblyPath = outputAssemblyHashedName,
            WinRTProjectionAssemblyPath = winRTProjectionAssemblyHashedName,
            WinRTComponentAssemblyPath = winRTComponentAssemblyHashedName,
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
        string fileName = Path.GetFileName(Path.Normalize(filePath));
        byte[] utf8Data = Encoding.UTF8.GetBytes(filePath);
        byte[] hashData = Shake128.HashData(utf8Data, outputLength: 16);
        string hash = Convert.ToHexString(hashData);

        return $"{Path.GetFileNameWithoutExtension(fileName)}_{hash}{Path.GetExtension(fileName)}";
    }

    /// <summary>
    /// Copies all specified assemblies to a target folder, and returns the list of updated hashed filenames.
    /// </summary>
    /// <param name="assemblyPaths">The input assembly paths.</param>
    /// <param name="destinationDirectory">The target directory to copy the assemblies to.</param>
    /// <param name="originalPaths">A dictionary to store the original paths of the copied assemblies.</param>
    /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The list of updated hashed filenames.</returns>
    private static List<string> CopyHashedFilesToDirectory(
        string[] assemblyPaths,
        string destinationDirectory,
        Dictionary<string, string> originalPaths,
        CancellationToken token)
    {
        List<string> updatedDllNames = [];

        foreach (string assemblyPath in assemblyPaths)
        {
            token.ThrowIfCancellationRequested();

            string hashedName = GetHashedFileName(assemblyPath);
            string destinationPath = Path.Combine(destinationDirectory, hashedName);

            File.Copy(assemblyPath, destinationPath, overwrite: true);

            updatedDllNames.Add(hashedName);
            originalPaths.Add(hashedName, assemblyPath);
        }

        return updatedDllNames;
    }

    /// <summary>
    /// Copies a specified assembly to a target folder.
    /// </summary>
    /// <param name="assemblyPath">The input assembly paths.</param>
    /// <param name="destinationDirectory">The target directory to copy the assembly to.</param>
    /// <param name="originalPaths">A dictionary to store the original paths of the copied assemblies.</param>
    /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The hashed filename.</returns>
    [return: NotNullIfNotNull(nameof(assemblyPath))]
    private static string? CopyHashedFileToDirectory(
        string? assemblyPath,
        string destinationDirectory,
        Dictionary<string, string> originalPaths,
        CancellationToken token)
    {
        if (assemblyPath is null)
        {
            return null;
        }

        string hashedName = GetHashedFileName(assemblyPath);
        string destinationPath = Path.Combine(destinationDirectory, hashedName);

        File.Copy(assemblyPath, destinationPath, overwrite: true);

        token.ThrowIfCancellationRequested();

        // Special case for private implementation detail assemblies (e.g. 'WinRT.Projection.dll') that are
        // both passed via the reference set, but also explicitly as separate properties. In that case, we
        // expect that those should already be in the original paths at this point. So we validate that
        // the path actually matches, and simply do nothing if that's the case, as this is intended.
        if (originalPaths.TryGetValue(hashedName, out string? originalPath) && originalPath == assemblyPath)
        {
            return hashedName;
        }

        // If we get to this point, it means that either a private implementation assembly was passed with a
        // different path than the one provided to the reference set, which should never happen (it's invalid).
        if (originalPaths.ContainsKey(hashedName))
        {
            string fileName = Path.GetFileName(Path.Normalize(assemblyPath));

            throw WellKnownInteropExceptions.ReservedDllOriginalPathMismatchFromDebugRepro(fileName);
        }

        originalPaths.Add(hashedName, assemblyPath);

        return hashedName;
    }
}