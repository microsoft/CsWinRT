// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Loads all reference assemblies, so they are read only once and shared by all phases that inspect them.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The loaded reference assemblies.</returns>
    private static ReferenceAssemblySet LoadReferenceAssemblies(ProjectionGeneratorArgs args)
    {
        // Filter out .winmd files from the resolver paths (they are metadata, not assemblies)
        string[] resolverPaths = [.. args.ReferenceAssemblyPaths
            .Where(static path => !path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))];

        PathAssemblyResolver resolver = new(resolverPaths);

        List<ReferenceAssembly> assemblies = [];

        // Load each distinct reference assembly once, keeping the original order
        foreach (string path in resolverPaths.Distinct(StringComparer.Ordinal))
        {
            args.Token.ThrowIfCancellationRequested();

            ModuleDefinition module = ModuleDefinition.FromFile(path, resolver.ReaderParameters);

            assemblies.Add(new(path, module, module.Assembly?.Name?.Value, IsComponentAssembly(module)));
        }

        return new(resolver, assemblies);
    }

    /// <summary>
    /// Counts the distinct <c>.winmd</c> files that the current invocation processes. All invocations are given the
    /// same list of <c>.winmd</c> files, but each one only handles the subset that is relevant to it.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="references">The loaded reference assemblies.</param>
    /// <param name="isComponentMode">Whether this invocation is producing <c>WinRT.Component.dll</c>.</param>
    /// <returns>The number of <c>.winmd</c> files processed by this invocation.</returns>
    private static int CountProcessedWinMDs(ProjectionGeneratorArgs args, ReferenceAssemblySet references, bool isComponentMode)
    {
        HashSet<string> processedWinMDPaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (string winmdPath in args.WinMDPaths)
        {
            bool isProcessed = isComponentMode
                ? references.IsComponentWinMD(winmdPath)
                : IsThirdPartyWinMD(winmdPath, references);

            if (isProcessed)
            {
                _ = processedWinMDPaths.Add(Path.GetFullPath(winmdPath));
            }
        }

        return processedWinMDPaths.Count;
    }

    /// <summary>
    /// Checks whether a <c>.winmd</c> file is third-party metadata, i.e. not the Windows SDK, the internal
    /// interop metadata, or an authored component (those are all handled separately).
    /// </summary>
    /// <param name="winmdPath">The path to the <c>.winmd</c> file.</param>
    /// <param name="references">The loaded reference assemblies.</param>
    /// <returns>Whether <paramref name="winmdPath"/> is third-party metadata.</returns>
    private static bool IsThirdPartyWinMD(string winmdPath, ReferenceAssemblySet references)
    {
        string metadataName = Path.GetFileNameWithoutExtension(winmdPath);

        return !IsWindowsSdkMetadataName(metadataName)
            && metadataName != "WindowsRuntime.Internal"
            && !references.IsComponentWinMD(winmdPath);
    }
}
