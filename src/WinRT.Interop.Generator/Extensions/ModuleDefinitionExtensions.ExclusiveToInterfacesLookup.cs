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
    /// Gets a lookup of '[exclusiveto]' interfaces for projected Windows Runtime class types in a given module. The lookup is
    /// built from the <c>[WindowsRuntimeExclusiveToInterface]</c> attributes on the <c>WindowsRuntimeExclusiveToInterfaces</c>
    /// type in the <c>ABI</c> namespace. The resulting dictionary maps class types (by namespace and name) to
    /// a list of their exclusive interface <see cref="TypeSignature"/> values.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting '[exclusiveto]' interfaces lookup.</returns>
    public static IReadOnlyDictionary<(Utf8String? Namespace, Utf8String? Name), FrozenSet<TypeSignature>> GetExclusiveToInterfacesLookup(this ModuleDefinition module)
    {
        return ExclusiveToInterfacesLookupCache.Instance.GetOrAdd(
            key: module,
            valueFactory: static module =>
            {
                TypeDefinition? windowsRuntimeExclusiveToInterfacesType = null;

                // Find the 'WindowsRuntimeExclusiveToInterfaces' lookup type in the ABI namespace
                foreach (TypeDefinition type in module.TopLevelTypes)
                {
                    // Rather than using the lookup, which we don't really need here since we're only
                    // doing a single find operation, we just scan the types to find the one we need.
                    if (type.Namespace is Utf8String ns && ns.AsSpan().SequenceEqual("ABI"u8) &&
                        type.Name is Utf8String name && name.AsSpan().SequenceEqual("WindowsRuntimeExclusiveToInterfaces"u8))
                    {
                        windowsRuntimeExclusiveToInterfacesType = type;

                        break;
                    }
                }

                // We didn't find the target type, so this module is likely invalid. We don't need
                // to do anything here, lookups would just fail and report the correct diagnostics.
                if (windowsRuntimeExclusiveToInterfacesType is null)
                {
                    return FrozenDictionary<(Utf8String?, Utf8String?), FrozenSet<TypeSignature>>.Empty;
                }

                Dictionary<(Utf8String?, Utf8String?), HashSet<TypeSignature>> builder = [];

                // Enumerate all attributes on the lookup type and extract runtime class to exclusive interface pairs
                foreach (CustomAttribute attribute in windowsRuntimeExclusiveToInterfacesType.CustomAttributes)
                {
                    // Match '[WindowsRuntimeExclusiveToInterface(typeof(<CLASS_TYPE>), typeof(<INTERFACE_TYPE>))]'
                    if (attribute.Signature is not { FixedArguments: [{ Element: TypeSignature classType }, { Element: TypeSignature interfaceType }] })
                    {
                        continue;
                    }

                    // Add the current pair to the map we're building (a class can have multiple exclusive interfaces)
                    (Utf8String? Namespace, Utf8String? Name) key = (classType.Namespace, classType.Name);

                    if (!builder.TryGetValue(key, out HashSet<TypeSignature>? set))
                    {
                        set = new(SignatureComparer.Default);
                        builder[key] = set;
                    }

                    _ = set.Add(interfaceType);
                }

                return builder.ToFrozenDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => kvp.Value.ToFrozenSet(SignatureComparer.Default));
            });
    }
}

/// <summary>
/// Contains a shared cache of '[exclusiveto]' interfaces lookups, to speed up search operations.
/// </summary>
file static class ExclusiveToInterfacesLookupCache
{
    /// <summary>
    /// The singleton '[exclusiveto]' interfaces lookups map.
    /// </summary>
    public static readonly ConditionalWeakTable<ModuleDefinition, FrozenDictionary<(Utf8String? Namespace, Utf8String? Name), FrozenSet<TypeSignature>>> Instance = [];
}
