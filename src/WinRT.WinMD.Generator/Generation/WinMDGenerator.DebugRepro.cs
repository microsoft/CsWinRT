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
using WindowsRuntime.GeneratorCli;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.GeneratorCli.Helpers;

#pragma warning disable IDE0008

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGenerator"/>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// The file name for the original names of the reference assemblies.
    /// </summary>
    private const string ReferencePathMapFileName = "original-reference-paths.json";

    /// <summary>
    /// Runs the debug repro unpack logic for the generator.
    /// </summary>
    /// <param name="path">The path to the debug repro file to unpack.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The path to the resulting response file to use.</returns>
    private static string UnpackDebugRepro(string path, CancellationToken token)
    {
        // Create a temporary directory to extract the files from the debug repro
        string tempFolderName = $"cswinrtwinmdgen-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtwinmdgen.rsp");
        ZipArchiveEntry originalReferencePathsEntry = archive.Entries.Single(entry => entry.Name == ReferencePathMapFileName);
        ZipArchiveEntry[] assemblyEntries =
        [
            .. archive.Entries.Where(entry =>
            {
                string extension = Path.GetExtension(Path.Normalize(entry.Name));

                return extension is ".dll" or ".winmd";
            })
        ];

        token.ThrowIfCancellationRequested();

        WinMDGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = WinMDGeneratorArgs.ParseFromResponseFile(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths for reference assemblies
        Dictionary<string, string> originalReferencePaths = ExtractPathMap(originalReferencePathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        string? inputAssemblyPath = null;

        // Define a subdirectory for all the reference assembly paths. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting files easier, without having to scroll past hundreds of folders.
        string referenceDirectory = Path.Combine(tempDirectory, "reference");

        // Create the directory in advance, so that we can directly extract the files there
        _ = Directory.CreateDirectory(referenceDirectory);

        // Extract all .dll/.winmd files, restoring their original filenames
        foreach (ZipArchiveEntry assemblyEntry in assemblyEntries)
        {
            bool isReferenceAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, "reference");

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalReferencePaths.TryGetValue(assemblyEntry.Name, out string? originalPath))
            {
                throw WellKnownWinMDExceptions.DebugReproMissingFileEntryMapping(assemblyEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original assembly name
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationFolder = isReferenceAssembly ? referenceDirectory : tempDirectory;
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the file to the new destination path
            assemblyEntry.ExtractToFile(destinationPath, overwrite: true);

            // Track the extracted paths. The input assembly lives at the top level, while
            // all reference assemblies live inside the "reference" subfolder.
            if (assemblyEntry.Name == args.InputAssemblyPath)
            {
                inputAssemblyPath = destinationPath;
            }
            else if (isReferenceAssembly)
            {
                referencePaths.Add(destinationPath);
            }
            else
            {
                // We should never hit this case, so throw to validate that the debug repro is valid.
                // Entries should always be either reference assemblies or the input assembly.
                throw WellKnownWinMDExceptions.DebugReproUnrecognizedFileEntry(assemblyEntry.FullName);
            }
        }

        token.ThrowIfCancellationRequested();

        // Place the output .winmd into the temporary directory, using the original name
        string originalOutputName = Path.GetFileName(Path.Normalize(args.OutputWinmdPath));
        string outputWinmdPath = Path.Combine(tempDirectory, originalOutputName);

        // Prepare the .rsp file with all updated arguments
        string rspText = new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyPath!,
            ReferenceAssemblyPaths = [.. referencePaths],
            OutputWinmdPath = outputWinmdPath,
            AssemblyVersion = args.AssemblyVersion,
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtwinmdgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        // Return the resulting .rsp file so it can be used to replay the debug repro
        return rspFilePath;
    }

    /// <summary>
    /// Runs the debug repro save logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void SaveDebugRepro(WinMDGeneratorArgs args)
    {
        // We expect callers to have already performed this check, but just in case
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        // The target folder must exist
        if (!Directory.Exists(args.DebugReproDirectory))
        {
            throw WellKnownWinMDExceptions.DebugReproDirectoryDoesNotExist(args.DebugReproDirectory);
        }

        // Path for the ZIP archive
        string zipPath = Path.Combine(args.DebugReproDirectory, "winmd-debug-repro.zip");

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"cswinrtwinmdgen-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);
        string referenceDirectory = Path.Combine(tempDirectory, "reference");

        _ = Directory.CreateDirectory(tempDirectory);
        _ = Directory.CreateDirectory(referenceDirectory);

        // Map with all the original paths
        Dictionary<string, string> originalReferencePaths = [];

        // Add all reference paths with hashed names to the reference subdirectory under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceNames = CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferencePaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the input assembly to the top level temporary directory
        string inputAssemblyHashedName = CopyHashedFileToDirectory(args.InputAssemblyPath, tempDirectory, originalReferencePaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyHashedName,
            ReferenceAssemblyPaths = [.. updatedReferenceNames],
            OutputWinmdPath = args.OutputWinmdPath,
            AssemblyVersion = args.AssemblyVersion,
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtwinmdgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the reference path map
        CopyPathMapToDirectory(originalReferencePaths, tempDirectory, ReferencePathMapFileName);

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
        List<string> updatedNames = [];

        foreach (string assemblyPath in assemblyPaths)
        {
            token.ThrowIfCancellationRequested();

            string hashedName = GetHashedFileName(assemblyPath);
            string destinationPath = Path.Combine(destinationDirectory, hashedName);

            File.Copy(assemblyPath, destinationPath, overwrite: true);

            updatedNames.Add(hashedName);
            originalPaths.Add(hashedName, assemblyPath);
        }

        return updatedNames;
    }

    /// <summary>
    /// Copies a specified assembly to a target folder.
    /// </summary>
    /// <param name="assemblyPath">The input assembly path.</param>
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

        originalPaths.Add(hashedName, assemblyPath);

        return hashedName;
    }

    /// <summary>
    /// Copies an input path map to a target directory, as a serialized JSON file.
    /// </summary>
    /// <param name="pathMap">The input path map.</param>
    /// <param name="destinationDirectory">The target directory to copy the assemblies to.</param>
    /// <param name="fileName">The name to use for the file with the serialized path map.</param>
    private static void CopyPathMapToDirectory(
        Dictionary<string, string> pathMap,
        string destinationDirectory,
        string fileName)
    {
        // Create the .json file with the input path map
        string jsonFilePath = Path.Combine(destinationDirectory, fileName);

        using Stream jsonStream = File.Create(jsonFilePath);

        // Serialize the path map to the target file
        JsonSerializer.Serialize(jsonStream, pathMap, GeneratorJsonSerializerContext.Default.DictionaryStringString);
    }

    /// <summary>
    /// Extracts an input path from a .zip archive entry.
    /// </summary>
    /// <param name="pathMapEntry">The input path map entry.</param>
    /// <remarks>
    /// The <paramref name="pathMapEntry"/> value is expected to have the content produced by calls to <see cref="CopyPathMapToDirectory"/>.
    /// </remarks>
    private static Dictionary<string, string> ExtractPathMap(ZipArchiveEntry pathMapEntry)
    {
        using Stream stream = pathMapEntry.Open();

        // Load the mapping with all the original file paths for the included assemblies
        return JsonSerializer.Deserialize(stream, GeneratorJsonSerializerContext.Default.DictionaryStringString)!;
    }
}
