// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.DebugRepro;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.WinMDGenerator.Errors;

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
        string tempDirectory = DebugReproPacker.CreateUnpackTempDirectory("cswinrtwinmdgen");

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
            args = ResponseFileParser.Parse<WinMDGeneratorArgs, WellKnownWinMDExceptions>(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths for reference assemblies
        Dictionary<string, string> originalReferencePaths = DebugReproPacker.ExtractPathMap(originalReferencePathsEntry);

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
        string rspText = ResponseFileBuilder.Format(new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyPath!,
            ReferenceAssemblyPaths = [.. referencePaths],
            OutputWinmdPath = outputWinmdPath,
            AssemblyVersion = args.AssemblyVersion,
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        });

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

        (string tempDirectory, string zipPath) = DebugReproPacker.BeginSave<WellKnownWinMDExceptions>(
            args.DebugReproDirectory,
            toolName: "cswinrtwinmdgen",
            archiveFileName: "winmd-debug-repro.zip");

        string referenceDirectory = Path.Combine(tempDirectory, "reference");

        _ = Directory.CreateDirectory(referenceDirectory);

        // Map with all the original paths
        Dictionary<string, string> originalReferencePaths = [];

        // Add all reference paths with hashed names to the reference subdirectory under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceNames = DebugReproPacker.CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferencePaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the input assembly to the top level temporary directory
        string inputAssemblyHashedName = DebugReproPacker.CopyHashedFileToDirectory(args.InputAssemblyPath, tempDirectory, originalReferencePaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = ResponseFileBuilder.Format(new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyHashedName,
            ReferenceAssemblyPaths = [.. updatedReferenceNames],
            OutputWinmdPath = args.OutputWinmdPath,
            AssemblyVersion = args.AssemblyVersion,
            UseWindowsUIXamlProjections = args.UseWindowsUIXamlProjections,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        });

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtwinmdgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the reference path map
        DebugReproPacker.CopyPathMapToDirectory(originalReferencePaths, tempDirectory, ReferencePathMapFileName);

        args.Token.ThrowIfCancellationRequested();

        DebugReproPacker.FinalizeSave(tempDirectory, zipPath);
    }
}
