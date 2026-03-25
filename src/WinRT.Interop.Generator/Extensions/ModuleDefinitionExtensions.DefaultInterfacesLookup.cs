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
                TypeDefinition? windowsRuntimeDefaultInterfacesType = null;

                // Find the 'WindowsRuntimeDefaultInterfaces' lookup type in the ABI namespace
                foreach (TypeDefinition type in module.TopLevelTypes)
                {
                    // Rather than using the lookup, which we don't really need here since we're only
                    // doing a single find operation, we just scan the types to find the one we need.
                    if (type.Namespace is Utf8String ns && ns.AsSpan().SequenceEqual("ABI"u8) &&
                        type.Name is Utf8String name && name.AsSpan().SequenceEqual("WindowsRuntimeDefaultInterfaces"u8))
                    {
                        windowsRuntimeDefaultInterfacesType = type;

                        break;
                    }
                }

                // We didn't find the target type, so this module is likely invalid. We don't need
                // to do anything here, lookups would just fail and report the correct diagnostics.
                if (windowsRuntimeDefaultInterfacesType is null)
                {
                    return FrozenDictionary<(Utf8String?, Utf8String?), TypeSignature>.Empty;
                }

                Dictionary<(Utf8String?, Utf8String?), TypeSignature> builder = [];

                // Enumerate all attributes on the lookup type and extract runtime class to default interface pairs
                foreach (CustomAttribute attribute in windowsRuntimeDefaultInterfacesType.CustomAttributes)
                {
                    // Match '[WindowsRuntimeDefaultInterface(typeof(<CLASS_TYPE>), typeof(<INTERFACE_TYPE>))]'
                    if (attribute.Signature is not { FixedArguments: [{ Element: TypeSignature classType }, { Element: TypeSignature interfaceType }] })
                    {
                        continue;
                    }

                    // Add the current pair to the map we're building
                    builder[(classType.Namespace, classType.Name)] = interfaceType;
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
