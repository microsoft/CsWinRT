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
using WindowsRuntime.ProjectionGenerator.Errors;
using WindowsRuntime.ProjectionWriter.Helpers;

#pragma warning disable IDE0008

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal static partial class ProjectionGenerator
{
    /// <summary>
    /// The file name for the original names of the reference .dll-s.
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
    /// .winmd files are stored. The replay run sets <see cref="ProjectionGeneratorArgs.WindowsMetadata"/>
    /// to the absolute path of this folder so the writer picks up its contents via recursive scan.
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
        // Create a temporary directory to extract the files from the debug repro
        string tempFolderName = $"cswinrtprojectiongen-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtprojectiongen.rsp");
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

        ProjectionGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = ResponseFileParser.Parse<ProjectionGeneratorArgs, WellKnownProjectionGeneratorExceptions>(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths
        Dictionary<string, string> originalReferencePaths = DebugReproPacker.ExtractPathMap(originalReferencePathsEntry);
        Dictionary<string, string> originalWinMDPaths = DebugReproPacker.ExtractPathMap(originalWinMDPathsEntry);
        Dictionary<string, string> originalWindowsMetadataPaths = DebugReproPacker.ExtractPathMap(originalWindowsMetadataPathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        List<string> winmdPaths = [];

        // Define subdirectories for each category of input. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just
        // to make inspecting the resulting files easier, without having to scroll past many folders.
        // It also makes it possible to directly inspect the different sets of input files.
        string referenceDirectory = Path.Combine(tempDirectory, ReferenceSubfolder);
        string winmdDirectory = Path.Combine(tempDirectory, WinMDSubfolder);
        string windowsMetadataDirectory = Path.Combine(tempDirectory, WindowsMetadataSubfolder);

        // Create the directories in advance, so that we can directly extract the files there
        _ = Directory.CreateDirectory(referenceDirectory);
        _ = Directory.CreateDirectory(winmdDirectory);
        _ = Directory.CreateDirectory(windowsMetadataDirectory);

        // Extract all .dll/.winmd files, one per directory based on category, so we can ensure
        // there's no name conflicts between the different sets of input files.
        foreach (ZipArchiveEntry assemblyEntry in assemblyEntries)
        {
            bool isReferenceAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, ReferenceSubfolder);
            bool isWinMDAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, WinMDSubfolder);
            bool isWindowsMetadataAssembly = Path.IsWithinDirectoryName(assemblyEntry.FullName, WindowsMetadataSubfolder);

            // Select the right mapping based on the entry's category
            Dictionary<string, string> originalPaths = isReferenceAssembly
                ? originalReferencePaths
                : isWinMDAssembly
                    ? originalWinMDPaths
                    : originalWindowsMetadataPaths;

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalPaths.TryGetValue(assemblyEntry.Name, out string? originalPath))
            {
                throw WellKnownProjectionGeneratorExceptions.DebugReproMissingFileEntryMapping(assemblyEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original assembly name
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationFolder = isReferenceAssembly
                ? referenceDirectory
                : isWinMDAssembly
                    ? winmdDirectory
                    : windowsMetadataDirectory;
            string destinationPath = Path.Combine(destinationFolder, originalName);

            // Extract the file to the new destination path
            assemblyEntry.ExtractToFile(destinationPath, overwrite: true);

            if (isReferenceAssembly)
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
                // Entries should always be either reference, winmd, or windows-metadata assemblies.
                throw WellKnownProjectionGeneratorExceptions.DebugReproUnrecognizedFileEntry(assemblyEntry.FullName);
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments. The 'WindowsMetadata' value points at the
        // bundled folder, which the writer scans recursively to pick up all the .winmd files it contains.
        string rspText = ResponseFileBuilder.Format(new ProjectionGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. referencePaths],
            GeneratedAssemblyDirectory = tempDirectory,
            WinMDPaths = [.. winmdPaths],
            TargetFramework = args.TargetFramework,
            WindowsMetadata = windowsMetadataDirectory,
            AssemblyName = args.AssemblyName,
            WindowsSdkOnly = args.WindowsSdkOnly,
            WindowsUIXamlProjection = args.WindowsUIXamlProjection,
            MaxDegreesOfParallelism = args.MaxDegreesOfParallelism,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        });

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtprojectiongen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        // Return the resulting .rsp file so it can be used to replay the debug repro
        return rspFilePath;
    }

    /// <summary>
    /// Runs the debug repro save logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void SaveDebugRepro(ProjectionGeneratorArgs args)
    {
        // We expect callers to have already performed this check, but just in case
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        // The target folder must exist
        if (!Directory.Exists(args.DebugReproDirectory))
        {
            throw WellKnownProjectionGeneratorExceptions.DebugReproDirectoryDoesNotExist(args.DebugReproDirectory);
        }

        // Path for the ZIP archive
        string zipPath = Path.Combine(args.DebugReproDirectory, "projection-debug-repro.zip");

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"cswinrtprojectiongen-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);
        string referenceDirectory = Path.Combine(tempDirectory, ReferenceSubfolder);
        string winmdDirectory = Path.Combine(tempDirectory, WinMDSubfolder);
        string windowsMetadataDirectory = Path.Combine(tempDirectory, WindowsMetadataSubfolder);

        _ = Directory.CreateDirectory(tempDirectory);
        _ = Directory.CreateDirectory(referenceDirectory);
        _ = Directory.CreateDirectory(winmdDirectory);
        _ = Directory.CreateDirectory(windowsMetadataDirectory);

        // Maps with all the original paths
        Dictionary<string, string> originalReferencePaths = [];
        Dictionary<string, string> originalWinMDPaths = [];
        Dictionary<string, string> originalWindowsMetadataPaths = [];

        // Add all reference and .winmd paths with hashed names to the respective subdirectories under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceNames = DebugReproPacker.CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferencePaths, args.Token);
        List<string> updatedWinMDNames = DebugReproPacker.CopyHashedFilesToDirectory(args.WinMDPaths, winmdDirectory, originalWinMDPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Expand the Windows metadata token (a literal path, 'local', 'sdk', 'sdk+', or a version like
        // '10.0.26100.0') to the concrete set of .winmd files the writer would actually consume. This
        // makes the debug repro fully self-contained, even when the original Windows metadata token was
        // a special value that depends on the host environment (e.g. a registered SDK installation).
        List<string> expandedWindowsMetadataPaths = [];

        foreach (string expanded in WindowsMetadataExpander.Expand(args.WindowsMetadata))
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
        string rspText = ResponseFileBuilder.Format(new ProjectionGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. updatedReferenceNames],
            GeneratedAssemblyDirectory = args.GeneratedAssemblyDirectory,
            WinMDPaths = [.. updatedWinMDNames],
            TargetFramework = args.TargetFramework,
            WindowsMetadata = WindowsMetadataSubfolder,
            AssemblyName = args.AssemblyName,
            WindowsSdkOnly = args.WindowsSdkOnly,
            WindowsUIXamlProjection = args.WindowsUIXamlProjection,
            MaxDegreesOfParallelism = args.MaxDegreesOfParallelism,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        });

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtprojectiongen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json files with the path maps for each category
        DebugReproPacker.CopyPathMapToDirectory(originalReferencePaths, tempDirectory, ReferencePathMapFileName);
        DebugReproPacker.CopyPathMapToDirectory(originalWinMDPaths, tempDirectory, WinMDPathMapFileName);
        DebugReproPacker.CopyPathMapToDirectory(originalWindowsMetadataPaths, tempDirectory, WindowsMetadataPathMapFileName);

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
}
