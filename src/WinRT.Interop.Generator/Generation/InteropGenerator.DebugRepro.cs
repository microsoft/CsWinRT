// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WindowsRuntime.Generator.Generation;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the debug repro logic for the generator.
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
            string hashedName = GetHashedFileName(referenceAssemblyPath);
            string destinationPath = Path.Combine(tempDirectory, hashedName);

            File.Copy(referenceAssemblyPath, destinationPath, overwrite: true);

            updatedDllNames.Add(hashedName);
            originalPaths.Add(hashedName, referenceAssemblyPath);
        }

        // Add the output assembly to the temp directory with a hashed name
        string outputAssemblyHashedName = GetHashedFileName(args.OutputAssemblyPath);
        string outputAssemblyDestination = Path.Combine(tempDirectory, outputAssemblyHashedName);

        File.Copy(args.OutputAssemblyPath, outputAssemblyDestination, overwrite: true);

        originalPaths.Add(outputAssemblyHashedName, args.OutputAssemblyPath);

        // Prepare the .rsp file with all updated arguments
        StringBuilder builder = new();

        _ = builder.Append(GeneratorArgs.GetCommandLineArgumentName<InteropGeneratorArgs>(nameof(InteropGeneratorArgs.ReferenceAssemblyPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', updatedDllNames));

        _ = builder.Append(GeneratorArgs.GetCommandLineArgumentName<InteropGeneratorArgs>(nameof(InteropGeneratorArgs.OutputAssemblyPath)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(outputAssemblyDestination);

        _ = builder.Append(GeneratorArgs.GetCommandLineArgumentName<InteropGeneratorArgs>(nameof(InteropGeneratorArgs.UseWindowsUIXamlProjections)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(args.UseWindowsUIXamlProjections.ToString());

        _ = builder.Append(GeneratorArgs.GetCommandLineArgumentName<InteropGeneratorArgs>(nameof(InteropGeneratorArgs.MaxDegreesOfParallelism)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(args.MaxDegreesOfParallelism.ToString());

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtgen.rsp");

        File.WriteAllText(rspFilePath, builder.ToString());

        // Create the .json file with the original paths
        string jsonFilePath = Path.Combine(tempDirectory, "original-paths.json");

        // Serialize the original paths
        using (Stream jsonStream = File.Create(jsonFilePath))
        {
            var jsonTypeInfo = (JsonTypeInfo<Dictionary<string, string>>)InteropGeneratorJsonSerializerContext.Default.GetTypeInfo(typeof(Dictionary<string, string>))!;

            JsonSerializer.Serialize(jsonStream, originalPaths, jsonTypeInfo);
        }

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
