// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <inheritdoc cref="ModuleDefinitionExtensions"/>
internal partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets a lookup of source <c>.winmd</c> module names ("stems") for projected Windows Runtime types in a given
    /// module. The lookup is built from the <c>[WindowsRuntimeMetadata]</c> attributes on the
    /// <c>WindowsRuntimeMetadataTypes</c> type in the <c>ABI</c> namespace. The resulting dictionary maps projected
    /// types (by namespace and name) to their source <c>.winmd</c> stem.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting metadata-types lookup.</returns>
    public static IReadOnlyDictionary<(Utf8String? Namespace, Utf8String? Name), Utf8String> GetWindowsRuntimeMetadataTypesLookup(this ModuleDefinition module)
    {
        return WindowsRuntimeMetadataTypesLookupCache.Instance.GetOrAdd(
            key: module,
            valueFactory: static module =>
            {
                TypeDefinition? windowsRuntimeMetadataTypesType = null;

                // Find the 'WindowsRuntimeMetadataTypes' lookup type in the ABI namespace
                foreach (TypeDefinition type in module.TopLevelTypes)
                {
                    // Rather than using the lookup, which we don't really need here since we're only
                    // doing a single find operation, we just scan the types to find the one we need.
                    if (type.Namespace is Utf8String ns && ns.AsSpan().SequenceEqual("ABI"u8) &&
                        type.Name is Utf8String name && name.AsSpan().SequenceEqual("WindowsRuntimeMetadataTypes"u8))
                    {
                        windowsRuntimeMetadataTypesType = type;

                        break;
                    }
                }

                // We didn't find the target type, so this module is likely invalid. We don't need
                // to do anything here, lookups would just fail and report the correct diagnostics.
                if (windowsRuntimeMetadataTypesType is null)
                {
                    return FrozenDictionary<(Utf8String?, Utf8String?), Utf8String>.Empty;
                }

                Dictionary<(Utf8String?, Utf8String?), Utf8String> builder = [];

                // Enumerate all attributes on the lookup type and extract projected type to .winmd stem pairs
                foreach (CustomAttribute attribute in windowsRuntimeMetadataTypesType.CustomAttributes)
                {
                    // Match '[WindowsRuntimeMetadata(typeof(<TYPE>), "<STEM>")]'
                    if (attribute.Signature is not { FixedArguments: [{ Element: TypeSignature projectedType }, { Element: Utf8String stem }] })
                    {
                        continue;
                    }

                    // Add the current pair to the map we're building
                    builder[(projectedType.Namespace, projectedType.Name)] = stem;
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
    public static readonly ConditionalWeakTable<ModuleDefinition, FrozenDictionary<(Utf8String? Namespace, Utf8String? Name), Utf8String>> Instance = [];
}
