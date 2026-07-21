// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="ModuleDefinition"/>.
/// </summary>
internal static class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets a lookup of source <c>.winmd</c> module names ("stems") for projected Windows Runtime types in a given
    /// module. The lookup is built from the <c>[WindowsRuntimeMetadata]</c> attributes on the
    /// <c>WindowsRuntimeMetadataTypes</c> type in the <c>ABI</c> namespace (only present in implementation
    /// projections). The resulting dictionary maps projected types (by namespace and name) to their source
    /// <c>.winmd</c> stem.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting metadata-types lookup.</returns>
    public static IReadOnlyDictionary<(string? Namespace, string? Name), string> GetWindowsRuntimeMetadataTypesLookup(this ModuleDefinition module)
    {
        return WindowsRuntimeMetadataTypesLookupCache.Instance.GetOrAdd(
            key: module,
            valueFactory: static module =>
            {
                TypeDefinition? windowsRuntimeMetadataTypesType = null;

                foreach (TypeDefinition type in module.TopLevelTypes)
                {
                    if (type.Namespace?.Value == "ABI" && type.Name?.Value == "WindowsRuntimeMetadataTypes")
                    {
                        windowsRuntimeMetadataTypesType = type;

                        break;
                    }
                }

                if (windowsRuntimeMetadataTypesType is null)
                {
                    return FrozenDictionary<(string?, string?), string>.Empty;
                }

                Dictionary<(string?, string?), string> builder = [];

                // Enumerate all attributes on the lookup type and extract projected type to .winmd stem pairs
                foreach (CustomAttribute attribute in windowsRuntimeMetadataTypesType.CustomAttributes)
                {
                    // Match '[WindowsRuntimeMetadata(typeof(<TYPE>), "<STEM>")]'
                    if (attribute.Signature is { FixedArguments: [{ Element: TypeSignature projectedType }, { Element: { } stem }] })
                    {
                        builder[(projectedType.Namespace, projectedType.Name)] = stem.ToString()!;
                    }
                }

                return builder.ToFrozenDictionary();
            });
    }
}

/// <summary>
/// Contains a shared cache of metadata-types lookups, to speed up search operations.
/// </summary>
file static class WindowsRuntimeMetadataTypesLookupCache
{
    /// <summary>
    /// The singleton metadata-types lookups map.
    /// </summary>
    public static readonly ConditionalWeakTable<ModuleDefinition, FrozenDictionary<(string? Namespace, string? Name), string>> Instance = [];
}
