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
using System.Text.Json.Serialization;
using System.Threading;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// Debug repro support for the projection generator.
/// </summary>
internal static partial class ProjectionGenerator
{
    /// <summary>
    /// Unpacks a debug repro ZIP file and returns the path to the extracted response file.
    /// </summary>
    /// <param name="zipPath">The path to the debug repro ZIP file.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The path to the extracted response file.</returns>
    private static string UnpackDebugRepro(string zipPath, CancellationToken token)
    {
        if (!File.Exists(zipPath))
        {
            throw WellKnownProjectionExceptions.DebugReproDirectoryDoesNotExist(zipPath);
        }

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"cswinrtprojgen-debug-repro-unpack-{Guid.NewGuid():N}");

        try
        {
            _ = Directory.CreateDirectory(tempDirectory);

            string inputDirectory = Path.Combine(tempDirectory, "input");
            _ = Directory.CreateDirectory(inputDirectory);

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                // Extract all files
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    token.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    string destinationPath = Path.Combine(tempDirectory, entry.FullName);
                    string? destinationDir = Path.GetDirectoryName(destinationPath);

                    if (destinationDir is not null)
                    {
                        _ = Directory.CreateDirectory(destinationDir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            // Look for response file
            string responseFilePath = Path.Combine(tempDirectory, "response.rsp");

            if (!File.Exists(responseFilePath))
            {
                throw WellKnownProjectionExceptions.DebugReproUnpackFailed(zipPath, new FileNotFoundException("Response file not found in debug repro archive."));
            }

            // Load path mappings if present
            string pathMapFile = Path.Combine(tempDirectory, "original-input-paths.json");

            if (File.Exists(pathMapFile))
            {
                Dictionary<string, string>? pathMap = JsonSerializer.Deserialize(
                    File.ReadAllText(pathMapFile),
                    DebugReproJsonContext.Default.DictionaryStringString);

                if (pathMap is not null)
                {
                    // Rewrite response file with updated paths
                    string[] lines = File.ReadAllLines(responseFilePath);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();

                        if (line.StartsWith("--input-file-paths", StringComparison.Ordinal))
                        {
                            // Remap paths
                            string value = line["--input-file-paths".Length..].Trim();
                            string[] paths = value.Split(',');

                            for (int j = 0; j < paths.Length; j++)
                            {
                                string originalName = Path.GetFileName(paths[j].Trim());

                                // Look up the hashed file in the input directory
                                foreach (string hashedFile in Directory.GetFiles(inputDirectory))
                                {
                                    if (Path.GetFileName(hashedFile).Contains(originalName.Replace(".winmd", ""), StringComparison.OrdinalIgnoreCase))
                                    {
                                        paths[j] = hashedFile;
                                        break;
                                    }
                                }
                            }

                            lines[i] = $"--input-file-paths {string.Join(',', paths)}";
                        }
                    }

                    File.WriteAllLines(responseFilePath, lines);
                }
            }

            return responseFilePath;
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw WellKnownProjectionExceptions.DebugReproUnpackFailed(zipPath, e);
        }
    }

    /// <summary>
    /// Saves a debug repro ZIP file for the current generator invocation.
    /// </summary>
    /// <param name="args">The generator arguments to save.</param>
    private static void SaveDebugRepro(ProjectionGeneratorArgs args)
    {
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        string debugReproDirectory = args.DebugReproDirectory;

        if (!Directory.Exists(debugReproDirectory))
        {
            throw WellKnownProjectionExceptions.DebugReproDirectoryDoesNotExist(debugReproDirectory);
        }

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"cswinrtprojgen-debug-repro-save-{Guid.NewGuid():N}");

        try
        {
            _ = Directory.CreateDirectory(tempDirectory);

            string inputDirectory = Path.Combine(tempDirectory, "input");
            _ = Directory.CreateDirectory(inputDirectory);

            // Copy input fileswith hashed names to avoid collisions
            Dictionary<string, string> originalPaths = new(StringComparer.Ordinal);
            List<string> hashedInputPaths = [];

            foreach (string inputPath in args.InputFilePaths)
            {
                string hashedFileName = GetHashedFileName(inputPath);
                string destinationPath = Path.Combine(inputDirectory, hashedFileName);

                File.Copy(inputPath, destinationPath, overwrite: true);
                originalPaths[hashedFileName] = inputPath;
                hashedInputPaths.Add(hashedFileName);
            }

            // Write path map
            string pathMapJson = JsonSerializer.Serialize(
                originalPaths,
                DebugReproJsonContext.Default.DictionaryStringString);

            File.WriteAllText(
                Path.Combine(tempDirectory, "original-input-paths.json"),
                pathMapJson);

            // Write response file
            string responseFilePath = Path.Combine(tempDirectory, "response.rsp");

            using (StreamWriter rspWriter = new(responseFilePath))
            {
                rspWriter.WriteLine($"--input-file-paths {string.Join(',', hashedInputPaths.Select(p => Path.Combine("input", p)))}");
                rspWriter.WriteLine($"--output-directory {args.OutputDirectory}");

                if (args.IncludeNamespaces is { Length: > 0 })
                {
                    rspWriter.WriteLine($"--include-namespaces {string.Join(',', args.IncludeNamespaces)}");
                }

                if (args.ExcludeNamespaces is { Length: > 0 })
                {
                    rspWriter.WriteLine($"--exclude-namespaces {string.Join(',', args.ExcludeNamespaces)}");
                }

                if (args.IsComponent)
                {
                    rspWriter.WriteLine("--component");
                }

                if (args.IsInternal)
                {
                    rspWriter.WriteLine("--internal");
                }

                if (args.IsEmbedded)
                {
                    rspWriter.WriteLine("--embedded");
                }

                if (args.IsReferenceProjection)
                {
                    rspWriter.WriteLine("--reference-projection");
                }
            }

            // Create ZIP
            string zipPath = Path.Combine(debugReproDirectory, "debug-repro.zip");

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(tempDirectory, zipPath);
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw WellKnownProjectionExceptions.DebugReproSaveFailed(debugReproDirectory, e);
        }
        finally
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    /// <summary>
    /// Gets a hashed file name to avoid collisions when copying files.
    /// The hash is computed from the full file path using SHAKE128.
    /// </summary>
    /// <param name="filePath">The original file path.</param>
    /// <returns>A file name with format <c>{name}_{hash}{ext}</c>.</returns>
    private static string GetHashedFileName(string filePath)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);
        string ext = Path.GetExtension(filePath);

        // Use SHA256 for the hash (SHAKE128 is not available in all frameworks)
        byte[] pathBytes = Encoding.UTF8.GetBytes(filePath);
        byte[] hashBytes = SHA256.HashData(pathBytes);

        // Take first 16 bytes for the hash suffix
        string hashSuffix = Convert.ToHexStringLower(hashBytes.AsSpan(0, 16));

        return $"{name}_{hashSuffix}{ext}";
    }
}

/// <summary>
/// JSON serialization context for debug repro path maps.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class DebugReproJsonContext : JsonSerializerContext;
