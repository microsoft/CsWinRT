// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator;

/// <inheritdoc cref="ModuleDefinitionExtensions"/>
internal partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets a lookup of top level types for a given module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting top level types lookup.</returns>
    public static IReadOnlyDictionary<(Utf8String? Namespace, Utf8String? Name), TypeDefinition> GetTopLevelTypesLookup(this ModuleDefinition module)
    {
        return TopLevelTypesLookupCache.Instance.GetOrAdd(
            key: module,
            valueFactory: static module => module.TopLevelTypes.ToFrozenDictionary(static type => (type.Namespace, type.Name)));
    }
}

/// <summary>
/// Contains a shared cache of top level types lookups, to speed up search operations.
/// </summary>
file static class TopLevelTypesLookupCache
{
    /// <summary>
    /// The singleton top level types lookups map.
    /// </summary>
    public static readonly ConditionalWeakTable<ModuleDefinition, FrozenDictionary<(Utf8String? ns, Utf8String? name), TypeDefinition>> Instance = [];
}