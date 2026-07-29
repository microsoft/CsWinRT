// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Helpers;

namespace WindowsRuntime.Generator.DebugRepro;

/// <summary>
/// Leaf helpers shared across the CsWinRT CLI generators for packaging and unpacking debug repros.
/// </summary>
/// <remarks>
/// Each generator still owns the high-level orchestration (which input categories exist, which subfolders are
/// used, how the response file is re-stitched). This type only owns identical helpers that can be reused.
/// </remarks>
internal static class DebugReproPacker
{
    /// <summary>
    /// Generates a hashed filename by appending a hash of the original file path.
    /// </summary>
    /// <param name="filePath">The original file path.</param>
    /// <returns>The hashed filename in the form <c>{name}_{HEX}{ext}</c>.</returns>
    public static string GetHashedFileName(string filePath)
    {
        string fileName = Path.GetFileName(Path.Normalize(filePath));
        byte[] utf8Data = Encoding.UTF8.GetBytes(filePath);
        byte[] hashData = SHA256.HashData(utf8Data);
        string hash = Convert.ToHexString(hashData.AsSpan(0, 16));

        return $"{Path.GetFileNameWithoutExtension(fileName)}_{hash}{Path.GetExtension(fileName)}";
    }

    /// <summary>
    /// Copies all specified files to a target folder using hashed file names, and returns the list of updated names.
    /// </summary>
    /// <remarks>
    /// The same path can legitimately appear more than once in <paramref name="filePaths"/>, as MSBuild item lists
    /// are not deduplicated and several targets can contribute to the same reference set. Repeated entries are
    /// still reported in the returned list (so the rebuilt response file matches the original invocation), but
    /// they are only staged and recorded once.
    /// </remarks>
    /// <param name="filePaths">The input file paths.</param>
    /// <param name="destinationDirectory">The target directory to copy the files to.</param>
    /// <param name="originalPaths">A dictionary to store the original paths of the copied files (keyed by hashed file name).</param>
    /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The list of updated hashed filenames, in the same order as <paramref name="filePaths"/>.</returns>
    public static List<string> CopyHashedFilesToDirectory(
        string[] filePaths,
        string destinationDirectory,
        Dictionary<string, string> originalPaths,
        CancellationToken token)
    {
        List<string> updatedFileNames = [];

        foreach (string filePath in filePaths)
        {
            token.ThrowIfCancellationRequested();

            updatedFileNames.Add(CopyHashedFile(filePath, destinationDirectory, originalPaths));
        }

        return updatedFileNames;
    }

    /// <summary>
    /// Copies a single specified file to a target folder using a hashed file name.
    /// </summary>
    /// <remarks>
    /// This is the simple variant used by the impl, projection, projection-ref, and WinMD generators.
    /// The interop generator keeps its own variant locally (which additionally validates that a reserved
    /// .dll was passed with the same path in the shared reference set).
    /// </remarks>
    /// <param name="filePath">The input file path, or <see langword="null"/> to skip.</param>
    /// <param name="destinationDirectory">The target directory to copy the file to.</param>
    /// <param name="originalPaths">A dictionary to store the original path of the copied file (keyed by hashed file name).</param>
    /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The hashed filename, or <see langword="null"/> if <paramref name="filePath"/> was <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(filePath))]
    public static string? CopyHashedFileToDirectory(
        string? filePath,
        string destinationDirectory,
        Dictionary<string, string> originalPaths,
        CancellationToken token)
    {
        if (filePath is null)
        {
            return null;
        }

        token.ThrowIfCancellationRequested();

        return CopyHashedFile(filePath, destinationDirectory, originalPaths);
    }

    /// <summary>
    /// Stages a single input file into a target folder using its hashed file name, and records its original path.
    /// </summary>
    /// <param name="filePath">The input file path.</param>
    /// <param name="destinationDirectory">The target directory to copy the file to.</param>
    /// <param name="originalPaths">A dictionary to store the original path of the copied file (keyed by hashed file name).</param>
    /// <returns>The hashed filename of the staged file.</returns>
    private static string CopyHashedFile(
        string filePath,
        string destinationDirectory,
        Dictionary<string, string> originalPaths)
    {
        string hashedName = GetHashedFileName(filePath);
        string destinationPath = Path.Combine(destinationDirectory, hashedName);

        // Hashed names are derived from the full original path, so a file already staged at this exact
        // destination was necessarily copied from this exact source, and staging it again is a no-op.
        // The check is per destination folder, as the same file can legitimately also be staged under
        // another folder of the same repro (e.g. as both the input assembly and a reference assembly).
        if (!File.Exists(destinationPath))
        {
            File.Copy(filePath, destinationPath);

            // Clear the read-only attribute, which 'File.Copy' propagates from the source file. Inputs can
            // be read-only (e.g. in enlistment-style builds), and leaving the staged copies read-only would
            // make it impossible to delete the staging directory once the archive has been created.
            File.SetAttributes(destinationPath, File.GetAttributes(destinationPath) & ~FileAttributes.ReadOnly);
        }

        _ = originalPaths.TryAdd(hashedName, filePath);

        return hashedName;
    }

    /// <summary>
    /// Serializes an input path map to a target directory as a JSON file.
    /// </summary>
    /// <param name="pathMap">The input path map (hashed file name → original file path).</param>
    /// <param name="destinationDirectory">The target directory.</param>
    /// <param name="fileName">The name to use for the file with the serialized path map.</param>
    public static void CopyPathMapToDirectory(
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
    /// Extracts an input path map from a .zip archive entry.
    /// </summary>
    /// <param name="pathMapEntry">The input path map entry.</param>
    /// <returns>The deserialized path map (hashed file name → original file path).</returns>
    /// <remarks>
    /// The <paramref name="pathMapEntry"/> value is expected to have the content produced by calls to <see cref="CopyPathMapToDirectory"/>.
    /// </remarks>
    public static Dictionary<string, string> ExtractPathMap(ZipArchiveEntry pathMapEntry)
    {
        using Stream stream = pathMapEntry.Open();

        // Load the mapping with all the original file paths for the included files
        return JsonSerializer.Deserialize(stream, GeneratorJsonSerializerContext.Default.DictionaryStringString)!;
    }

    /// <summary>
    /// Prepares the staging directory and target archive path for a debug repro save operation.
    /// </summary>
    /// <typeparam name="TError">The per-tool error factory used to throw if <paramref name="debugReproDirectory"/> does not exist.</typeparam>
    /// <param name="debugReproDirectory">The user-provided directory where the resulting <c>.zip</c> archive will be written. Must already exist.</param>
    /// <param name="toolName">The CLI tool name (e.g. <c>"cswinrtimplgen"</c>), used as the prefix of the staging directory.</param>
    /// <param name="archiveFileName">The file name of the resulting <c>.zip</c> archive (e.g. <c>"impl-debug-repro.zip"</c>).</param>
    /// <returns>A pair containing the freshly-created staging directory and the absolute path of the target archive.</returns>
    /// <exception cref="Exception">Thrown via <typeparamref name="TError"/> if <paramref name="debugReproDirectory"/> does not exist.</exception>
    public static (string TempDirectory, string ZipPath) BeginSave<TError>(
        string debugReproDirectory,
        string toolName,
        string archiveFileName)
        where TError : IGeneratorErrorFactory
    {
        // The target folder must exist
        if (!Directory.Exists(debugReproDirectory))
        {
            throw TError.DebugReproDirectoryDoesNotExist(debugReproDirectory);
        }

        // Path for the ZIP archive
        string zipPath = Path.Combine(debugReproDirectory, archiveFileName);

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"{toolName}-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        return (tempDirectory, zipPath);
    }

    /// <summary>
    /// Finalizes a debug repro save by zipping the staging directory into the target archive and deleting the staging directory.
    /// </summary>
    /// <param name="tempDirectory">The staging directory previously returned by <see cref="BeginSave{TError}(string, string, string)"/>.</param>
    /// <param name="zipPath">The absolute path of the target <c>.zip</c> archive, previously returned by <see cref="BeginSave{TError}(string, string, string)"/>.</param>
    /// <remarks>
    /// If a file already exists at <paramref name="zipPath"/>, it is deleted before the new archive is created.
    /// </remarks>
    public static void FinalizeSave(string tempDirectory, string zipPath)
    {
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
    /// Creates a freshly-named temporary directory for unpacking a debug repro <c>.zip</c> archive.
    /// </summary>
    /// <param name="toolName">The CLI tool name (e.g. <c>"cswinrtimplgen"</c>), used as the prefix of the directory.</param>
    /// <returns>The absolute path of the created directory.</returns>
    public static string CreateUnpackTempDirectory(string toolName)
    {
        // Create a temporary directory to extract the files from the debug repro
        string tempFolderName = $"{toolName}-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        return tempDirectory;
    }
}
