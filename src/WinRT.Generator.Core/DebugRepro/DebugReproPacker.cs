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
using WindowsRuntime.GeneratorCli.Helpers;

namespace WindowsRuntime.GeneratorCli.DebugRepro;

/// <summary>
/// Leaf helpers shared across the CsWinRT CLI generators for packaging and unpacking debug repros.
/// </summary>
/// <remarks>
/// Each generator's <c>SaveDebugRepro</c>/<c>UnpackDebugRepro</c> still owns the high-level orchestration
/// (which input categories exist, which subfolders are used, how the response file is re-stitched). This
/// type only owns the small, identical-across-tools building blocks:
/// <list type="bullet">
///   <item>Hashing the original file path into a stable, collision-free file name.</item>
///   <item>Copying files (or a single file) into a destination directory using the hashed names.</item>
///   <item>Serializing / deserializing the "hashed name → original path" mapping as JSON inside the repro archive.</item>
/// </list>
/// </remarks>
internal static class DebugReproPacker
{
    /// <summary>
    /// Generates a hashed filename by appending a Shake128 hash of the original file path.
    /// </summary>
    /// <param name="filePath">The original file path.</param>
    /// <returns>The hashed filename in the form <c>{name}_{HEX}{ext}</c>.</returns>
    public static string GetHashedFileName(string filePath)
    {
        string fileName = Path.GetFileName(Path.Normalize(filePath));
        byte[] utf8Data = Encoding.UTF8.GetBytes(filePath);
        byte[] hashData = Shake128.HashData(utf8Data, outputLength: 16);
        string hash = Convert.ToHexString(hashData);

        return $"{Path.GetFileNameWithoutExtension(fileName)}_{hash}{Path.GetExtension(fileName)}";
    }

    /// <summary>
    /// Copies all specified files to a target folder using hashed file names, and returns the list of updated names.
    /// </summary>
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

            string hashedName = GetHashedFileName(filePath);
            string destinationPath = Path.Combine(destinationDirectory, hashedName);

            File.Copy(filePath, destinationPath, overwrite: true);

            updatedFileNames.Add(hashedName);
            originalPaths.Add(hashedName, filePath);
        }

        return updatedFileNames;
    }

    /// <summary>
    /// Copies a single specified file to a target folder using a hashed file name.
    /// </summary>
    /// <remarks>
    /// This is the simple variant used by the impl, projection, projection-ref, and WinMD generators.
    /// The interop generator keeps its own variant locally (which adds reserved-DLL dedupe and throws
    /// on path mismatches against the shared reference set).
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

        string hashedName = GetHashedFileName(filePath);
        string destinationPath = Path.Combine(destinationDirectory, hashedName);

        File.Copy(filePath, destinationPath, overwrite: true);

        token.ThrowIfCancellationRequested();

        originalPaths.Add(hashedName, filePath);

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
}
