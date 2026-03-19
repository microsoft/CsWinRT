// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Gets a lookup of default interfaces for projected Windows Runtime class types in a given module. The lookup is
    /// built from the <c>[WindowsRuntimeDefaultInterface]</c> attributes on the <c>WindowsRuntimeDefaultInterfaces</c>
    /// type in the <c>ABI</c> namespace. The resulting dictionary maps class types (by namespace and name) to
    /// their default interface <see cref="TypeSignature"/> values.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting default interfaces lookup.</returns>
    public static IReadOnlyDictionary<(Utf8String? Namespace, Utf8String? Name), TypeSignature> GetDefaultInterfacesLookup(this ModuleDefinition module)
    {
        return DefaultInterfacesLookupCache.Instance.GetOrAdd(
            key: module,
            valueFactory: static module =>
            {
                // Find the 'WindowsRuntimeDefaultInterfaces' lookup type in the ABI namespace
                if (!module.GetTopLevelTypesLookup().TryGetValue(
                    ("ABI"u8, "WindowsRuntimeDefaultInterfaces"u8),
                    out TypeDefinition? lookupType))
                {
                    return FrozenDictionary<(Utf8String?, Utf8String?), TypeSignature>.Empty;
                }

                Dictionary<(Utf8String?, Utf8String?), TypeSignature> builder = [];

                // Enumerate all attributes on the lookup type and extract runtime class / default interface pairs
                foreach (CustomAttribute attribute in lookupType.CustomAttributes)
                {
                    // Match '[WindowsRuntimeDefaultInterface(typeof(Class), typeof(Interface))]'
                    if (attribute.Signature is not { FixedArguments: [{ Element: TypeSignature classType }, { Element: TypeSignature interfaceType }] })
                    {
                        continue;
                    }

                    // Resolve the class type to get its namespace and name for the lookup key
                    if (classType.Resolve() is TypeDefinition resolvedClassType)
                    {
                        builder[(resolvedClassType.Namespace, resolvedClassType.Name)] = interfaceType;
                    }
                }

                return builder.ToFrozenDictionary();
            });
    }
}

/// <summary>
/// Contains a shared cache of default interfaces lookups, to speed up search operations.
/// </summary>
file static class DefaultInterfacesLookupCache
{
    /// <summary>
    /// The singleton default interfaces lookups map.
    /// </summary>
    public static readonly ConditionalWeakTable<ModuleDefinition, FrozenDictionary<(Utf8String? Namespace, Utf8String? Name), TypeSignature>> Instance = [];
}
