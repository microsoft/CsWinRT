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
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ReferenceProjectionGenerator.Errors;

#pragma warning disable IDE0008

namespace WindowsRuntime.ReferenceProjectionGenerator.Generation;

/// <inheritdoc cref="ReferenceProjectionGenerator"/>
internal static partial class ReferenceProjectionGenerator
{
    /// <summary>
    /// The file name for the original names of the input .winmd files.
    /// </summary>
    private const string InputPathMapFileName = "original-input-paths.json";

    /// <summary>
    /// Runs the debug repro unpack logic for the generator.
    /// </summary>
    /// <param name="path">The path to the debug repro file to unpack.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The path to the resulting response file to use.</returns>
    private static string UnpackDebugRepro(string path, CancellationToken token)
    {
        string tempDirectory = DebugReproPacker.CreateUnpackTempDirectory("cswinrtprojectionrefgen");

        token.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        // Get all entries of interest
        ZipArchiveEntry responseFileEntry = archive.Entries.Single(entry => entry.Name == "cswinrtprojectionrefgen.rsp");
        ZipArchiveEntry originalInputPathsEntry = archive.Entries.Single(entry => entry.Name == InputPathMapFileName);
        ZipArchiveEntry[] winmdEntries = [.. archive.Entries.Where(entry => Path.GetExtension(Path.Normalize(entry.Name)) == ".winmd")];

        token.ThrowIfCancellationRequested();

        ReferenceProjectionGeneratorArgs args;

        // Parse the debug repro .rsp file
        using (Stream stream = responseFileEntry.Open())
        {
            args = ResponseFileParser.Parse<ReferenceProjectionGeneratorArgs, WellKnownReferenceProjectionGeneratorExceptions>(stream, token);
        }

        token.ThrowIfCancellationRequested();

        // Load the mappings with all the original file paths for the input .winmd files
        Dictionary<string, string> originalInputPaths = DebugReproPacker.ExtractPathMap(originalInputPathsEntry);

        token.ThrowIfCancellationRequested();

        List<string> inputPaths = [];

        // Define a subdirectory for all the input .winmd paths. We don't put these in the top level
        // temporary folder so that the number of files there remains very small. The reason is just to
        // make inspecting the resulting files easier, without having to scroll past hundreds of folders.
        string inputDirectory = Path.Combine(tempDirectory, "input");

        // Create the directory in advance, so that we can directly extract the .winmd files there
        _ = Directory.CreateDirectory(inputDirectory);

        // Extract all .winmd files, restoring their original filenames
        foreach (ZipArchiveEntry winmdEntry in winmdEntries)
        {
            bool isInputWinMD = Path.IsWithinDirectoryName(winmdEntry.FullName, "input");

            // Make sure the debug repro is well-formed and contains the mapping for this entry
            if (!originalInputPaths.TryGetValue(winmdEntry.Name, out string? originalPath))
            {
                throw WellKnownReferenceProjectionGeneratorExceptions.DebugReproMissingFileEntryMapping(winmdEntry.FullName);
            }

            // Construct the path in the temporary subfolder with the original .winmd name
            string originalName = Path.GetFileName(Path.Normalize(originalPath));
            string destinationPath = Path.Combine(inputDirectory, originalName);

            // Extract the .winmd to the new destination path
            winmdEntry.ExtractToFile(destinationPath, overwrite: true);

            if (isInputWinMD)
            {
                inputPaths.Add(destinationPath);
            }
            else
            {
                // We should never hit this case, so throw to validate that the debug repro is valid.
                // Entries should always be input .winmd files under the "input" folder.
                throw WellKnownReferenceProjectionGeneratorExceptions.DebugReproUnrecognizedFileEntry(winmdEntry.FullName);
            }
        }

        token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = ResponseFileBuilder.Format(new ReferenceProjectionGeneratorArgs
        {
            InputPaths = [.. inputPaths],
            OutputDirectory = tempDirectory,
            TargetFramework = args.TargetFramework,
            IncludeNamespaces = args.IncludeNamespaces,
            ExcludeNamespaces = args.ExcludeNamespaces,
            AdditionExcludeNamespaces = args.AdditionExcludeNamespaces,
            Verbose = args.Verbose,
            Component = args.Component,
            PublicExclusiveTo = args.PublicExclusiveTo,
            IdicExclusiveTo = args.IdicExclusiveTo,
            ReferenceProjection = args.ReferenceProjection,
            DebugReproDirectory = null,
            Token = CancellationToken.None
        });

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtprojectionrefgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        // Return the resulting .rsp file so it can be used to replay the debug repro
        return rspFilePath;
    }

    /// <summary>
    /// Runs the debug repro save logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void SaveDebugRepro(ReferenceProjectionGeneratorArgs args)
    {
        // We expect callers to have already performed this check, but just in case
        if (args.DebugReproDirectory is null)
        {
            return;
        }

        (string tempDirectory, string zipPath) = DebugReproPacker.BeginSave<WellKnownReferenceProjectionGeneratorExceptions>(
            args.DebugReproDirectory,
            toolName: "cswinrtprojectionrefgen",
            archiveFileName: "ref-projection-debug-repro.zip");

        string inputDirectory = Path.Combine(tempDirectory, "input");

        _ = Directory.CreateDirectory(inputDirectory);

        // Expand all input paths (which may be file paths, directories to recursively scan, or
        // special tokens like 'local', 'sdk', 'sdk+', or a version like '10.0.26100.0') into the
        // concrete set of .winmd files the writer would actually consume. This ensures the debug
        // repro is fully self-contained and can be replayed without needing the Windows SDK installed.
        List<string> expandedInputPaths = [];

        foreach (string inputPath in args.InputPaths)
        {
            expandedInputPaths.AddRange(WindowsMetadataExpander.Expand(inputPath));
        }

        args.Token.ThrowIfCancellationRequested();

        // Map with all the original paths
        Dictionary<string, string> originalInputPaths = [];

        // Add all input paths with hashed names to the input subdirectory under the temporary
        // directory, and store them with the updated names in a list to use to build the .rsp file.
        List<string> updatedInputNames = DebugReproPacker.CopyHashedFilesToDirectory([.. expandedInputPaths], inputDirectory, originalInputPaths, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Prepare the .rsp file with all updated arguments
        string rspText = ResponseFileBuilder.Format(new ReferenceProjectionGeneratorArgs
        {
            InputPaths = [.. updatedInputNames],
            OutputDirectory = args.OutputDirectory,
            TargetFramework = args.TargetFramework,
            IncludeNamespaces = args.IncludeNamespaces,
            ExcludeNamespaces = args.ExcludeNamespaces,
            AdditionExcludeNamespaces = args.AdditionExcludeNamespaces,
            Verbose = args.Verbose,
            Component = args.Component,
            PublicExclusiveTo = args.PublicExclusiveTo,
            IdicExclusiveTo = args.IdicExclusiveTo,
            ReferenceProjection = args.ReferenceProjection,
            DebugReproDirectory = args.DebugReproDirectory,
            Token = CancellationToken.None
        });

        // Create the actual .rsp file
        string rspFilePath = Path.Combine(tempDirectory, "cswinrtprojectionrefgen.rsp");

        File.WriteAllText(rspFilePath, rspText);

        args.Token.ThrowIfCancellationRequested();

        // Create the .json file with the input path map
        DebugReproPacker.CopyPathMapToDirectory(originalInputPaths, tempDirectory, InputPathMapFileName);

        args.Token.ThrowIfCancellationRequested();

        DebugReproPacker.FinalizeSave(tempDirectory, zipPath);
    }
}
