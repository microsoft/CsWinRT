// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using WindowsRuntime.GeneratorCli;
using WindowsRuntime.GeneratorCli.DebugRepro;
using WindowsRuntime.InteropGenerator.Errors;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// The file name for the original names of the reference .dll-s.
    /// </summary>
    private const string ReferencePathMapFileName = "original-reference-paths.json";

    /// <summary>
    /// The file name for the original names of the implementation .dll-s.
    /// </summary>
    private const string ImplementationPathMapFileName = "original-implementation-paths.json";

    /// <summary>
    /// Runs the debug repro unpack logic for the generator.
    /// </summary>
    /// <param name="path">The path to the debug repro file to unpack.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The path to the resulting response file to use.</returns>
    private static string UnpackDebugRepro(string path, CancellationToken token)
    {
        // Create a temporary directory to extract the files from the debug repro
        string tempFolderName = $"cswinrtinteropgen-debug-repro-unpack-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);

        _ = Directory.CreateDirectory(tempDirectory);

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtinteropgen.rsp");
        ZipArchiveEntry originalReferenceDllPathsEntry = archive.Entries.Single(entry => entry.Name == ReferencePathMapFileName);
        ZipArchiveEntry originalImplementationDllPathsEntry = archive.Entries.Single(entry => entry.Name == ImplementationPathMapFileName);
        ZipArchiveEntry[] dllEntries = [.. archive.Entries.Where(entry => Path.GetExtension(Path.Normalize(entry.Name)) == ".dll")];

        token.ThrowIfCancellationRequested();

        InteropGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = InteropGeneratorArgs.ParseFromResponseFile(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths for both reference and implementation .dll-s
        Dictionary<string, string> originalReferenceDllPaths = DebugReproPacker.ExtractPathMap(originalReferenceDllPathsEntry);
        Dictionary<string, string> originalImplementationDllPaths = DebugReproPacker.ExtractPathMap(originalImplementationDllPathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> referencePaths = [];
        List<string> implementationPaths = [];
        string? outputAssemblyPath = null;
        string? winRTSdkProjectionAssemblyPath = null;
        string? winRTSdkXamlProjectionAssemblyPath = null;
        string? winRTProjectionAssemblyPath = null;
        string? winRTComponentAssemblyPath = null;

        // Define two subdirectories for all the input assembly paths. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting .dll easier, without having to scroll past hundreds of folders.
        // Also, this makes it possible to directly inspect the two sets of input assembly paths.
        string referenceDllDirectory = Path.Combine(tempDirectory, "reference");
        string implementationDllDirectory = Path.Combine(tempDirectory, "implementation");

        // Create the directories too in advance, so that we can directly extract the .dll-s there
        _ = Directory.CreateDirectory(referenceDllDirectory);
        _ = Directory.CreateDirectory(implementationDllDirectory);

        // Extract all .dll-s, one per directory, so we can ensure there's no name conflicts
        foreach (ZipArchiveEntry dllEntry in dllEntries)
        {
            bool isReferenceDll = Path.IsWithinDirectoryName(dllEntry.FullName, "reference");
            bool isImplementationDll = Path.IsWithinDirectoryName(dllEntry.FullName, "implementation");

            // Select the right mapping (we have two, as we might have .dll-s with the same name in both sets)
            Dictionary<string, string> originalDllPaths = isReferenceDll ? originalReferenceDllPaths : originalImplementationDllPaths;

            // Also select the right destination folder (the output .dll will just go directly in the temporary directory)
            string destinationFolder = isReferenceDll
                ? referenceDllDirectory
                : (isImplementationDll
                    ? implementationDllDirectory
                    : tempDirectory);

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalDllPaths.TryGetValue(dllEntry.Name, out string? originalPath))
            {
                throw WellKnownInteropExceptions.DebugReproMissingFileEntryMapping(dllEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original .dll name
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
            else if (isReferenceDll)
            {
                referencePaths.Add(destinationPath);
            }
            else if (isImplementationDll)
            {
                implementationPaths.Add(destinationPath);
            }
            else
            {
                // We should never hit this case, so throw to validate that the debug repro is valid. Entries
                // should always be either reference .dll-s, implementation .dll-s, or the output assembly.
                throw WellKnownInteropExceptions.DebugReproUnrecognizedFileEntry(dllEntry.FullName);
            }

            // Also track the private implementation detail .dll-s (these are also in the set of references)
            if (dllEntry.Name == args.WinRTSdkProjectionAssemblyPath)
            {
                winRTSdkProjectionAssemblyPath = destinationPath;
            }
            else if (args.WinRTSdkXamlProjectionAssemblyPath is not null && dllEntry.Name == args.WinRTSdkXamlProjectionAssemblyPath)
            {
                winRTSdkXamlProjectionAssemblyPath = destinationPath;
            }
            else if (args.WinRTProjectionAssemblyPath is not null && dllEntry.Name == args.WinRTProjectionAssemblyPath)
            {
                winRTProjectionAssemblyPath = destinationPath;
            }
            else if (args.WinRTComponentAssemblyPath is not null && dllEntry.Name == args.WinRTComponentAssemblyPath)
            {
                winRTComponentAssemblyPath = destinationPath;
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. referencePaths],
            ImplementationAssemblyPaths = [.. implementationPaths],
            OutputAssemblyPath = outputAssemblyPath!,
            WinRTSdkProjectionAssemblyPath = winRTSdkProjectionAssemblyPath!,
            WinRTSdkXamlProjectionAssemblyPath = winRTSdkXamlProjectionAssemblyPath,
            WinRTProjectionAssemblyPath = winRTProjectionAssemblyPath,
            WinRTComponentAssemblyPath = winRTComponentAssemblyPath,
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
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtinteropgen.rsp");

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
        string zipPath = Path.Combine(args.DebugReproDirectory, "interop-debug-repro.zip");

        // Create a temporary directory to stage files for the ZIP
        string tempFolderName = $"cswinrtinteropgen-debug-repro-{Guid.NewGuid().ToString().ToUpperInvariant()}";
        string tempDirectory = Path.Combine(Path.GetTempPath(), tempFolderName);
        string referenceDirectory = Path.Combine(tempDirectory, "reference");
        string implementationDirectory = Path.Combine(tempDirectory, "implementation");

        _ = Directory.CreateDirectory(tempDirectory);
        _ = Directory.CreateDirectory(referenceDirectory);
        _ = Directory.CreateDirectory(implementationDirectory);

        // Maps with all the original paths
        Dictionary<string, string> originalReferenceDllPaths = new(args.ReferenceAssemblyPaths.Length + 1);
        Dictionary<string, string> originalImplementationDllPaths = new(args.ImplementationAssemblyPaths.Length + 1);

        // Add all reference and implementation paths with hashed names to the respective subdirectories under the
        // temporary directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedReferenceDllNames = DebugReproPacker.CopyHashedFilesToDirectory(args.ReferenceAssemblyPaths, referenceDirectory, originalReferenceDllPaths, args.Token);
        List<string> updatedImplementationDllNames = DebugReproPacker.CopyHashedFilesToDirectory(args.ImplementationAssemblyPaths, implementationDirectory, originalImplementationDllPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Hash and copy the well known assemblies we use as input
        string outputAssemblyHashedName = CopyHashedFileToDirectory(args.OutputAssemblyPath, tempDirectory, originalImplementationDllPaths, args.Token);
        string winRTSdkProjectionAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTSdkProjectionAssemblyPath, implementationDirectory, originalImplementationDllPaths, args.Token);
        string? winRTSdkXamlProjectionAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTSdkXamlProjectionAssemblyPath, implementationDirectory, originalImplementationDllPaths, args.Token);
        string? winRTProjectionAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTProjectionAssemblyPath, implementationDirectory, originalImplementationDllPaths, args.Token);
        string? winRTComponentAssemblyHashedName = CopyHashedFileToDirectory(args.WinRTComponentAssemblyPath, implementationDirectory, originalImplementationDllPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = new InteropGeneratorArgs
        {
            ReferenceAssemblyPaths = [.. updatedReferenceDllNames],
            ImplementationAssemblyPaths = [.. updatedImplementationDllNames],
            OutputAssemblyPath = outputAssemblyHashedName,
            WinRTSdkProjectionAssemblyPath = winRTSdkProjectionAssemblyHashedName,
            WinRTSdkXamlProjectionAssemblyPath = winRTSdkXamlProjectionAssemblyHashedName,
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
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtinteropgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the reference path map
        DebugReproPacker.CopyPathMapToDirectory(originalReferenceDllPaths, tempDirectory, ReferencePathMapFileName);

        args.Token.ThrowIfCancellationRequested();

        // Do the same for the implementation path map
        DebugReproPacker.CopyPathMapToDirectory(originalImplementationDllPaths, tempDirectory, ImplementationPathMapFileName);

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
    /// Copies a single specified file to a target folder, with reserved-DLL dedupe.
    /// </summary>
    /// <remarks>
    /// The interop generator keeps a local variant (instead of using the shared
    /// <see cref="DebugReproPacker.CopyHashedFileToDirectory(string?, string, Dictionary{string, string}, CancellationToken)"/>)
    /// to handle the private implementation-detail assemblies (<c>WinRT.Sdk.Projection.dll</c>, <c>WinRT.Sdk.Xaml.Projection.dll</c>,
    /// <c>WinRT.Projection.dll</c>, <c>WinRT.Component.dll</c>) that are passed both via the reference set and
    /// explicitly via dedicated properties. When the same path is already in the map (under the same hashed name),
    /// the copy is skipped; if the hashed name collides but the original paths differ, that's a real bug and we
    /// throw <see cref="WellKnownInteropExceptions.ReservedDllOriginalPathMismatchFromDebugRepro(string)"/>.
    /// </remarks>
    /// <param name="filePath">The input file path, or <see langword="null"/> to skip.</param>
    /// <param name="destinationDirectory">The target directory to copy the file to.</param>
    /// <param name="originalPaths">A dictionary to store the original path of the copied file (keyed by hashed file name).</param>
    /// <param name="token">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>The hashed filename, or <see langword="null"/> if <paramref name="filePath"/> was <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(filePath))]
    private static string? CopyHashedFileToDirectory(
        string? filePath,
        string destinationDirectory,
        Dictionary<string, string> originalPaths,
        CancellationToken token)
    {
        if (filePath is null)
        {
            return null;
        }

        string hashedName = DebugReproPacker.GetHashedFileName(filePath);

        // Special case for private implementation detail assemblies (e.g. 'WinRT.Projection.dll') that are
        // both passed via the reference set, but also explicitly as separate properties. In that case, we
        // expect that those should already be in the original paths at this point. So we validate that
        // the path actually matches, and simply do nothing if that's the case, as this is intended.
        if (originalPaths.TryGetValue(hashedName, out string? originalPath) && originalPath == filePath)
        {
            return hashedName;
        }

        // If we get to this point, it means that either a private implementation assembly was passed with a
        // different path than the one provided to the reference set, which should never happen (it's invalid).
        if (originalPaths.ContainsKey(hashedName))
        {
            string fileName = Path.GetFileName(Path.Normalize(filePath));

            throw WellKnownInteropExceptions.ReservedDllOriginalPathMismatchFromDebugRepro(fileName);
        }

        string destinationPath = Path.Combine(destinationDirectory, hashedName);

        // After validating that the file is unique and should be copied, we can safely do that. We move
        // this operation to ensure we don't accidentally end up with duplicated .dll-s in the debug repro.
        File.Copy(filePath, destinationPath, overwrite: true);

        token.ThrowIfCancellationRequested();

        originalPaths.Add(hashedName, filePath);

        return hashedName;
    }
}
