// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Helpers;

namespace WindowsRuntime.WinMDGenerator.Helpers;

/// <summary>
/// Builds a lookup from a projected Windows Runtime type (by namespace and name) to the source
/// <c>.winmd</c> module name (its "stem", i.e. the file name without extension) that defines it.
/// </summary>
/// <remarks>
/// <para>
/// The WinMD generator runs during a component authoring (library) build. There, the projection types
/// it references come from <em>reference</em> projections, which do not carry the centralized
/// <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type (that only exists in implementation projections
/// produced at app build time). To emit correct type references (e.g. <c>Microsoft.UI.Xaml</c> rather
/// than the <c>Microsoft.WinUI</c> projection assembly), the contract assembly name is instead resolved
/// by finding the referenced type in the actual input <c>.winmd</c> files and using the <c>.winmd</c>
/// file name it comes from.
/// </para>
/// </remarks>
internal static class WindowsRuntimeMetadataNameResolver
{
    /// <summary>
    /// Builds the <c>(namespace, name)</c> to source <c>.winmd</c> stem lookup from the given inputs.
    /// </summary>
    /// <param name="winmdPaths">The input <c>.winmd</c> file or directory paths (third party components and internal metadata).</param>
    /// <param name="windowsMetadata">The Windows metadata token (path, directory, <c>"local"</c>, <c>"sdk"</c>, <c>"sdk+"</c>, or a version).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting metadata-name lookup.</returns>
    public static FrozenDictionary<(string? Namespace, string? Name), string> Build(
        IEnumerable<string> winMDPaths,
        string windowsMetadata,
        CancellationToken token)
    {
        Dictionary<(string?, string?), string> builder = [];

        // Add all explicit .winmd inputs (third party components and internal metadata)
        foreach (string winmdPath in winmdPaths)
        {
            token.ThrowIfCancellationRequested();

            AddPath(builder, winmdPath);
        }

        // Expand the Windows metadata token (path | directory | "local" | "sdk[+]" | version[+]) into
        // actual .winmd file paths (or directories to scan), the same way the projection generators do.
        foreach (string path in WindowsMetadataExpander.Expand(windowsMetadata))
        {
            token.ThrowIfCancellationRequested();

            AddPath(builder, path);
        }

        return builder.ToFrozenDictionary();
    }

    /// <summary>
    /// Adds all Windows Runtime types from a <c>.winmd</c> file, or from every <c>.winmd</c> file under
    /// a directory (recursively), to the lookup.
    /// </summary>
    /// <param name="builder">The lookup being populated.</param>
    /// <param name="path">The <c>.winmd</c> file or directory path.</param>
    private static void AddPath(Dictionary<(string?, string?), string> builder, string path)
    {
        if (File.Exists(path))
        {
            if (path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
            {
                AddWinMD(builder, path);
            }

            return;
        }

        if (Directory.Exists(path))
        {
            foreach (string winmd in Directory.EnumerateFiles(path, "*.winmd", SearchOption.AllDirectories))
            {
                AddWinMD(builder, winmd);
            }
        }
    }

    /// <summary>
    /// Adds all public top-level types from a single <c>.winmd</c> file to the lookup, keyed by their
    /// namespace and name, mapping each to that <c>.winmd</c> file's stem.
    /// </summary>
    /// <param name="builder">The lookup being populated.</param>
    /// <param name="winmdPath">The <c>.winmd</c> file path.</param>
    private static void AddWinMD(Dictionary<(string?, string?), string> builder, string winmdPath)
    {
        ModuleDefinition module;

        // Loading a .winmd purely to enumerate its type names does not require any type resolution,
        // so failures to load a single input are non-fatal and just skip that file.
        try
        {
            module = ModuleDefinition.FromFile(winmdPath);
        }
        catch (Exception)
        {
            return;
        }

        string stem = Path.GetFileNameWithoutExtension(winmdPath);

        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            // Skip the '<Module>' pseudo-type and any non-public types (Windows Runtime types are public)
            if (!type.IsPublic || type.Name?.Value is not { } name || name.StartsWith('<'))
            {
                continue;
            }

            // First .winmd defining a given (namespace, name) wins; duplicate contract definitions
            // across metadata files are not expected in practice.
            _ = builder.TryAdd((type.Namespace?.Value, name), stem);
        }
    }
}
