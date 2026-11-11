// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.Generator.References;

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
    /// Selects the references needed to compile this projection, excluding unrelated application assemblies.
    /// </summary>
    /// <remarks>
    /// Roslyn embeds every compilation reference and its MVID in the PDB, even when no emitted IL uses it.
    /// SDK projections only depend on the framework and CsWinRT infrastructure. Other projections can name
    /// authored component types, so their references conservatively include the dependencies of those components,
    /// even when a particular dependency is not used by the generated source. Changes within that dependency
    /// graph can therefore still change the projection's bytes and invalidate downstream caches.
    /// </remarks>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="references">The loaded reference assemblies.</param>
    /// <param name="projectionReferenceAssemblies">Reference projections replaced by the generated sources.</param>
    /// <returns>The compilation reference paths, ordered by assembly name and then path.</returns>
    private static string[] GetCompilationReferences(
        ProjectionGeneratorArgs args,
        ReferenceAssemblySet references,
        HashSet<string> projectionReferenceAssemblies)
    {
        bool isComponentMode = args.AssemblyName == "WinRT.Component";
        bool isWindowsSdkMode = IsWindowsSdkMode(args);

        ILookup<string, ReferenceAssembly> availableReferences = references.Assemblies
            .Where(reference => !projectionReferenceAssemblies.Contains(reference.Path))
            .ToLookup(static reference => reference.Name ?? string.Empty, StringComparer.Ordinal);
        HashSet<string> paths = new(StringComparer.Ordinal);
        Queue<ReferenceAssembly> pendingReferences = new();

        // Select the roots to compile against (framework, CsWinRT infrastructure, and for non-SDK projections
        // also the WinRT reference assemblies and components). Their dependencies are added in a second pass.
        foreach (ReferenceAssembly reference in references.Assemblies)
        {
            if (projectionReferenceAssemblies.Contains(reference.Path))
            {
                continue;
            }

            bool include = IsFrameworkReference(reference) || reference.Name == "WinRT.Runtime";

            // The SDK projections only depend on the framework and CsWinRT infrastructure (plus the SDK projection
            // for the XAML one), so no other reference is needed. The remaining projections can also name types from
            // other projections and components, and need those references to compile their generated sources.
            if (isWindowsSdkMode)
            {
                include |= args.WindowsUIXamlProjection && reference.Name == "WinRT.Sdk.Projection";
            }
            else
            {
                include |=
                    reference.Name is "WinRT.Sdk.Projection" or "WinRT.Sdk.Xaml.Projection" ||
                    (isComponentMode && reference.Name == "WinRT.Projection") ||
                    reference.IsComponent ||
                    IsWindowsRuntimeReferenceAssembly(reference.Module) ||
                    (isComponentMode && IsActivationFactoryAssembly(reference.Module));
            }

            if (include && paths.Add(reference.Path) && !isWindowsSdkMode)
            {
                pendingReferences.Enqueue(reference);
            }
        }

        // Components can inherit managed base classes or use forwarders in their public signatures.
        // Preserve their dependency closure rather than assuming every required type is a WinRT type.
        while (pendingReferences.TryDequeue(out ReferenceAssembly reference))
        {
            args.Token.ThrowIfCancellationRequested();

            foreach (AssemblyReference dependency in reference.Module.AssemblyReferences)
            {
                foreach (ReferenceAssembly candidate in availableReferences[dependency.Name?.Value ?? string.Empty])
                {
                    if (paths.Add(candidate.Path))
                    {
                        pendingReferences.Enqueue(candidate);
                    }
                }
            }
        }

        // Keep only the selected references, in a stable order so the output doesn't depend on the input order
#pragma warning disable IDE0305
        return references.Assemblies
            .Where(reference => paths.Contains(reference.Path))
            .OrderBy(static reference => reference.Name, StringComparer.Ordinal)
            .ThenBy(static reference => reference.Path, StringComparer.Ordinal)
            .Select(static reference => reference.Path)
            .ToArray();
#pragma warning restore IDE0305
    }

    /// <summary>
    /// Checks whether a reference is a .NET framework assembly used by generated projections.
    /// </summary>
    /// <param name="reference">The reference assembly to check.</param>
    /// <returns>Whether the reference belongs to the framework.</returns>
    private static bool IsFrameworkReference(ReferenceAssembly reference)
    {
        // Microsoft signs non-framework libraries with some of these keys too, so the token alone isn't enough
        if (!BaseClassLibraryIdentity.IsBaseClassLibraryPublicKeyToken(reference.Module.Assembly?.GetPublicKeyToken()))
        {
            return false;
        }

        // The name families mirror the Microsoft.NETCore.App reference pack; revisit them if it adds new ones
        return
            reference.Name is "mscorlib" or "netstandard" or "System" or "Microsoft.CSharp" or "Microsoft.VisualBasic" or "Microsoft.VisualBasic.Core" ||
            reference.Name?.StartsWith("System.", StringComparison.Ordinal) == true ||
            reference.Name?.StartsWith("Microsoft.Win32.", StringComparison.Ordinal) == true;
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
