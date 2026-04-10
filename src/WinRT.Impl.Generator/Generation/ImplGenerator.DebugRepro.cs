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
using WindowsRuntime.ImplGenerator.Errors;
using WindowsRuntime.ImplGenerator.Helpers;
using WindowsRuntime.InteropGenerator;

#pragma warning disable IDE0008

namespace WindowsRuntime.ImplGenerator.Generation;

/// <inheritdoc cref="ImplGenerator"/>
internal static partial class ImplGenerator
{
    /// <summary>
    /// The file name for the original names of the reference .dll-s.
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
        string tempFolderName = $"cswinrtimplgen-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtimplgen.rsp");
        ZipArchiveEntry originalReferenceDllPathsEntry = archive.Entries.Single(entry => entry.Name == ReferencePathMapFileName);
        ZipArchiveEntry[] dllEntries = [.. archive.Entries.Where(entry => Path.GetExtension(Path.Normalize(entry.Name)) == ".dll")];

        token.ThrowIfCancellationRequested();

        ImplGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = ImplGeneratorArgs.ParseFromResponseFile(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths for reference .dll-s
        Dictionary<string, string> originalReferenceDllPaths = ExtractPathMap(originalReferenceDllPathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        string? outputAssemblyPath = null;

        // Define a subdirectory for all the input assembly paths. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting .dll easier, without having to scroll past hundreds of folders.
        string referenceDllDirectory = Path.Combine(tempDirectory, "reference");

        // Create the directory in advance, so that we can directly extract the .dll-s there
        _ = Directory.CreateDirectory(referenceDllDirectory);

        // Extract all .dll-s, one per directory, so we can ensure there's no name conflicts
        foreach (ZipArchiveEntry dllEntry in dllEntries)
        {
            bool isReferenceDll = Path.IsWithinDirectoryName(dllEntry.FullName, "reference");

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalReferenceDllPaths.TryGetValue(dllEntry.Name, out string? originalPath))
            {
                throw WellKnownImplExceptions.DebugReproMissingFileEntryMapping(dllEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original .dll name
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationFolder = isReferenceDll ? referenceDllDirectory : tempDirectory;
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the .dll to the new destination path
            dllEntry.ExtractToFile(destinationPath, overwrite: true);

            // Track all extracted reference paths, as well as the output assembly path.
            // Note that the debug repro only uses filenames, not full paths, for .dll-s.
            if (dllEntry.Name == args.OutputAssemblyPath)
            {
                outputAssemblyPath = destinationPath;
            }
            else if (isReferenceDll)
            {
                referencePaths.Add(destinationPath);
            }
            else
            {
                // We should never hit this case, so throw to validate that the debug repro is valid.
                // Entries should always be either reference .dll-s or the output assembly.
                throw WellKnownImplExceptions.DebugReproUnrecognizedFileEntry(dllEntry.FullName);
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new ImplGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. referencePaths],
            OutputAssemblyPath = outputAssemblyPath!,
            GeneratedAssemblyDirectory = tempDirectory,
            TreatWarningsAsErrors = args.TreatWarningsAsErrors,
            AssemblyOriginatorKeyFile = args.AssemblyOriginatorKeyFile,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtimplgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        // Return the resulting .rsp file so it can be used to replay the debug repro
        return rspFilePath;
    }

    /// <summary>
    /// Runs the debug repro save logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void SaveDebugRepro(ImplGeneratorArgs args)
    {
        // We expect callers to have already performed this check, but just in case
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        // The target folder must exist
        if (!Directory.Exists(args.DebugReproDirectory))
        {
            throw WellKnownImplExceptions.DebugReproDirectoryDoesNotExist(args.DebugReproDirectory);
        }

        // Path for the ZIP archive
        string zipPath = Path.Combine(args.DebugReproDirectory, "impl-debug-repro.zip");

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"cswinrtimplgen-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);
        string referenceDirectory = Path.Combine(tempDirectory, "reference");

        _ = Directory.CreateDirectory(tempDirectory);
        _ = Directory.CreateDirectory(referenceDirectory);

        // Map with all the original paths
        Dictionary<string, string> originalReferenceDllPaths = new(args.ReferenceAssemblyPaths.Length + 1);

        // Add all reference paths with hashed names to the reference subdirectory under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceDllNames = CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferenceDllPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the output assembly
        string outputAssemblyHashedName = CopyHashedFileToDirectory(args.OutputAssemblyPath, tempDirectory, originalReferenceDllPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new ImplGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. updatedReferenceDllNames],
            OutputAssemblyPath = outputAssemblyHashedName,
            GeneratedAssemblyDirectory = args.GeneratedAssemblyDirectory,
            TreatWarningsAsErrors = args.TreatWarningsAsErrors,
            AssemblyOriginatorKeyFile = args.AssemblyOriginatorKeyFile,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        }.FormatToResponseFile();

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtimplgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the reference path map
        CopyPathMapToDirectory(originalReferenceDllPaths, tempDirectory, ReferencePathMapFileName);

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
        JsonSerializer.Serialize(jsonStream, pathMap, ImplGeneratorJsonSerializerContext.Default.DictionaryStringString);
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

        // Load the mapping with all the original file paths for the included .dll-s
        return JsonSerializer.Deserialize(stream, ImplGeneratorJsonSerializerContext.Default.DictionaryStringString)!;
    }
}
