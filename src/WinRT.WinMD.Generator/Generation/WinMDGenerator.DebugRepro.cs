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
using WindowsRuntime.Generator.Helpers;
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
    /// The file name for the original names of the input .winmd files.
    /// </summary>
    private const string WinMDPathMapFileName = "original-winmd-paths.json";

    /// <summary>
    /// The file name for the original names of the Windows metadata .winmd files.
    /// </summary>
    private const string WindowsMetadataPathMapFileName = "original-windows-metadata-paths.json";

    /// <summary>
    /// The subfolder name (relative to the debug repro root) where reference .dll-s are stored.
    /// </summary>
    private const string ReferenceSubfolder = "reference";

    /// <summary>
    /// The subfolder name (relative to the debug repro root) where input .winmd files are stored.
    /// </summary>
    private const string WinMDSubfolder = "winmd";

    /// <summary>
    /// The subfolder name (relative to the debug repro root) where the expanded Windows metadata
    /// .winmd files are stored. The replay run sets <see cref="WinMDGeneratorArgs.WindowsMetadata"/>
    /// to the absolute path of this folder so it is picked up via recursive scan.
    /// </summary>
    private const string WindowsMetadataSubfolder = "windows-metadata";

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
        ZipArchiveEntry originalWinMDPathsEntry = archive.Entries.Single(entry => entry.Name == WinMDPathMapFileName);
        ZipArchiveEntry originalWindowsMetadataPathsEntry = archive.Entries.Single(entry => entry.Name == WindowsMetadataPathMapFileName);
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

        // Load the mappings with all the original file paths for each category
        Dictionary<string, string> originalReferencePaths = DebugReproPacker.ExtractPathMap(originalReferencePathsEntry);
        Dictionary<string, string> originalWinMDPaths = DebugReproPacker.ExtractPathMap(originalWinMDPathsEntry);
        Dictionary<string, string> originalWindowsMetadataPaths = DebugReproPacker.ExtractPathMap(originalWindowsMetadataPathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        List<string> winmdPaths = [];
        string? inputAssemblyPath = null;

        // Define subdirectories for each category of input. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting files easier, without having to scroll past hundreds of folders.
        string referenceDirectory = Path.Combine(tempDirectory, ReferenceSubfolder);
        string winmdDirectory = Path.Combine(tempDirectory, WinMDSubfolder);
        string windowsMetadataDirectory = Path.Combine(tempDirectory, WindowsMetadataSubfolder);

        // Create the directories in advance, so that we can directly extract the files there
        _ = Directory.CreateDirectory(referenceDirectory);
        _ = Directory.CreateDirectory(winmdDirectory);
        _ = Directory.CreateDirectory(windowsMetadataDirectory);

        // Extract all .dll/.winmd files, restoring their original filenames. The input assembly lives at
        // the top level, while reference/winmd/windows-metadata inputs live inside their own subfolders.
        foreach (ZipArchiveEntry assemblyEntry in assemblyEntries)
        {
            bool isReferenceAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, ReferenceSubfolder);
            bool isWinMDAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, WinMDSubfolder);
            bool isWindowsMetadataAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, WindowsMetadataSubfolder);

            // Select the right mapping based on the entry's category (the input assembly, at the top
            // level, is tracked in the reference-paths map alongside the reference assemblies).
            Dictionary<string, string> originalPaths = isWinMDAssembly
                ? originalWinMDPaths
                : isWindowsMetadataAssembly
                    ? originalWindowsMetadataPaths
                    : originalReferencePaths;

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalPaths.TryGetValue(assemblyEntry.Name, out string? originalPath))
            {
                throw WellKnownWinMDExceptions.DebugReproMissingFileEntryMapping(assemblyEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original assembly name
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationFolder = isReferenceAssembly
                ? referenceDirectory
                : isWinMDAssembly
                    ? winmdDirectory
                    : isWindowsMetadataAssembly
                        ? windowsMetadataDirectory
                        : tempDirectory;
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the file to the new destination path
            assemblyEntry.ExtractToFile(destinationPath, overwrite: true);

            // Track the extracted paths per category
            if (assemblyEntry.Name == args.InputAssemblyPath)
            {
                inputAssemblyPath = destinationPath;
            }
            else if (isReferenceAssembly)
            {
                referencePaths.Add(destinationPath);
            }
            else if (isWinMDAssembly)
            {
                winmdPaths.Add(destinationPath);
            }
            else if (!isWindowsMetadataAssembly)
            {
                // We should never hit this case, so throw to validate that the debug repro is valid.
                throw WellKnownWinMDExceptions.DebugReproUnrecognizedFileEntry(assemblyEntry.FullName);
            }
        }

        token.ThrowIfCancellationRequested();

        // Place the output .winmd into the temporary directory, using the original name
        string originalOutputName = Path.GetFileName(Path.Normalize(args.OutputWinmdPath));
        string outputWinmdPath = Path.Combine(tempDirectory, originalOutputName);

        // Prepare the .rsp file with all updated arguments. The 'WindowsMetadata' value points at the
        // bundled folder, which is scanned recursively to pick up all the .winmd files it contains.
        string rspText = ResponseFileBuilder.Format(new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyPath!,
            ReferenceAssemblyPaths = [.. referencePaths],
            WinMDPaths = [.. winmdPaths],
            WindowsMetadata = windowsMetadataDirectory,
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

        string referenceDirectory = Path.Combine(tempDirectory, ReferenceSubfolder);
        string winmdDirectory = Path.Combine(tempDirectory, WinMDSubfolder);
        string windowsMetadataDirectory = Path.Combine(tempDirectory, WindowsMetadataSubfolder);

        _ = Directory.CreateDirectory(referenceDirectory);
        _ = Directory.CreateDirectory(winmdDirectory);
        _ = Directory.CreateDirectory(windowsMetadataDirectory);

        // Maps with all the original paths, per category
        Dictionary<string, string> originalReferencePaths = [];
        Dictionary<string, string> originalWinMDPaths = [];
        Dictionary<string, string> originalWindowsMetadataPaths = [];

        // Add all reference and .winmd paths with hashed names to the respective subdirectories under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceNames = DebugReproPacker.CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferencePaths, args.Token);
        List<string> updatedWinMDNames = DebugReproPacker.CopyHashedFilesToDirectory(args.WinMDPaths, winmdDirectory, originalWinMDPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the input assembly to the top level temporary directory (tracked in the reference map)
        string inputAssemblyHashedName = DebugReproPacker.CopyHashedFileToDirectory(args.InputAssemblyPath, tempDirectory, originalReferencePaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Expand the Windows metadata token (a literal path, directory, 'local', 'sdk', 'sdk+', or a
        // version like '10.0.26100.0') to the concrete set of .winmd files it resolves to. This makes
        // the debug repro fully self-contained, even when the original token depended on the host
        // environment (e.g. a registered SDK installation).
        List<string> expandedWindowsMetadataPaths = [];

        foreach (string expanded in WindowsMetadataExpander.Expand<WellKnownWinMDExceptions>(args.WindowsMetadata))
        {
            // The expander may return either individual files or directories; we want individual
            // files in the bundled repro so the layout is fully self-describing.
            if (File.Exists(expanded))
            {
                expandedWindowsMetadataPaths.Add(expanded);
            }
            else if (Directory.Exists(expanded))
            {
                expandedWindowsMetadataPaths.AddRange(Directory.EnumerateFiles(expanded, "*.winmd", SearchOption.AllDirectories));
            }
        }

        args.Token.ThrowIfCancellationRequested();

        // Bundle the expanded Windows metadata files into the windows-metadata subdirectory
        _ = DebugReproPacker.CopyHashedFilesToDirectory([.. expandedWindowsMetadataPaths], windowsMetadataDirectory, originalWindowsMetadataPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments. The 'WindowsMetadata' value is just the
        // subfolder name (relative path); the replay run resolves it to an absolute path inside its
        // own temporary unpack directory, since the original 'DebugReproDirectory' may not exist there.
        string rspText = ResponseFileBuilder.Format(new WinMDGeneratorArgs
        {
            InputAssemblyPath = inputAssemblyHashedName,
            ReferenceAssemblyPaths = [.. updatedReferenceNames],
            WinMDPaths = [.. updatedWinMDNames],
            WindowsMetadata = WindowsMetadataSubfolder,
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

        // Create the .json files with the reference path maps for each category
        DebugReproPacker.CopyPathMapToDirectory(originalReferencePaths, tempDirectory, ReferencePathMapFileName);
        DebugReproPacker.CopyPathMapToDirectory(originalWinMDPaths, tempDirectory, WinMDPathMapFileName);
        DebugReproPacker.CopyPathMapToDirectory(originalWindowsMetadataPaths, tempDirectory, WindowsMetadataPathMapFileName);

        args.Token.ThrowIfCancellationRequested();

        DebugReproPacker.FinalizeSave(tempDirectory, zipPath);
    }
}
