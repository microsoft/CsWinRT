// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Helpers;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for external IID type map entries, independent of individual API call sites.
/// </summary>
internal static class WindowsRuntimeIidTypeMapBuilder
{
    /// <summary>
    /// Creates conditional IID providers for Windows Runtime types and discovered constructed types.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="inputs">The canonicalized discovery inputs.</param>
    /// <param name="interopDefinitions">The projection definitions.</param>
    /// <param name="interopReferences">The references for this invocation.</param>
    /// <param name="module">The interop module being built.</param>
    public static void Build(
        InteropGeneratorArgs args,
        InteropGeneratorEmitInputs inputs,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        HashSet<TypeSignature> types = new(interopReferences.SignatureComparer);

        // Fixed projection and runtime IIDs are precompiled in their owning assemblies
        foreach (TypeSignature type in inputs.EnumerateIidTypes())
        {
            // IID generation requires resolvable metadata and supported closed signatures
            if (!type.IsFullyResolvable(interopReferences.RuntimeContext, out TypeDefinition? definition) ||
                !type.IsConstructedType(definition))
            {
                continue;
            }

            // 'Nullable<T>' entries are synthesized below from valid value types, excluding nullable key/value pairs
            if (type.IsConstructedNullableValueType(interopReferences))
            {
                continue;
            }

            // Ordinary managed types can appear in discovery without having a Windows Runtime identity
            if (type.IsWindowsRuntimeType(interopReferences))
            {
                // Value metadata contributes its explicit 'IReference<T>' projection, not an IID for 'T' itself
                TypeSignature iidType = type is not (GenericInstanceTypeSignature or SzArrayTypeSignature) && definition.IsValueType
                    ? interopReferences.Nullable1.MakeGenericValueType([type])
                    : type;

                _ = types.Add(iidType);
            }
        }

        Dictionary<string, (TypeSignature Type, Guid Iid)> entries = new(StringComparer.Ordinal);

        // Compute the IID and structural key for each eligible type
        foreach (TypeSignature type in types)
        {
            args.Token.ThrowIfCancellationRequested();

            // A valid metadata name does not necessarily represent a native interface
            if (!GuidGenerator.TryCreateIID(type, interopDefinitions, interopReferences, args.UseWindowsUIXamlProjections, out Guid iid))
            {
                continue;
            }

            TypeSignature importedType = module.DefaultImporter.ImportTypeSignature(type);
            string key = WindowsRuntimeIidTypeNameFactory.GetName(importedType, interopDefinitions, interopReferences);

            // Conflicting projections must not bind the same key to different IIDs
            if (!entries.TryAdd(key, (importedType, iid)))
            {
                (TypeSignature previousType, Guid previousIid) = entries[key];

                if (previousIid != iid)
                {
                    throw WellKnownInteropExceptions.ConflictingIidTypeNames(key, previousIid, iid);
                }

                // Distinct reference/implementation spellings can share a key and IID but have different trim targets
                if (TypeDescriptorComparer.Default.Compare(importedType, previousType) < 0)
                {
                    entries[key] = (importedType, iid);
                }
            }
        }

        Dictionary<Guid, TypeDefinition> providers = [];

        // Provider names depend only on their IID, not discovery or emission order
        foreach (KeyValuePair<string, (TypeSignature Type, Guid Iid)> entry in entries)
        {
            args.Token.ThrowIfCancellationRequested();

            if (!providers.TryGetValue(entry.Value.Iid, out TypeDefinition? providerType))
            {
                // Providers need only a '[Guid]' attribute, with no fields or executable code
                providerType = new TypeDefinition(
                    ns: "ABI"u8,
                    name: $"<{entry.Value.Iid:N}>IidProvider",
                    attributes: TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    baseType: interopReferences.Object.ToTypeDefOrRef())
                {
                    CustomAttributes = { InteropCustomAttributeFactory.Guid(entry.Value.Iid, interopReferences) }
                };

                module.TopLevelTypes.Add(providerType);
                providers.Add(entry.Value.Iid, providerType);
            }

            // Map the structural key to its IID provider, conditional on the selected trim target being retained
            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeIidTypeMapGroup(
                value: entry.Key,
                target: providerType.ToTypeSignature(),
                trimTarget: GetTrimTarget(entry.Value.Type, interopReferences),
                interopReferences: interopReferences));
        }
    }

    /// <summary>
    /// Selects a non-generic trim target whose retention keeps the external type map entry, without requiring allocation.
    /// </summary>
    /// <param name="type">The type represented by the IID entry.</param>
    /// <param name="interopReferences">The references for identifying fundamental types.</param>
    /// <returns>The type controlling the entry's retention.</returns>
    private static TypeSignature GetTrimTarget(TypeSignature type, InteropReferences interopReferences)
    {
        // Array types can be constructed at runtime, so use the element's dependency to retain their entries
        if (type is SzArrayTypeSignature array)
        {
            return GetTrimTarget(array.BaseType, interopReferences);
        }

        // A non-generic leaf already provides the concrete type dependency we need
        if (type is not GenericInstanceTypeSignature generic)
        {
            return type;
        }

        // Native AOT can construct closed generics from shared-code templates at runtime, without marking
        // their exact types during compilation. Their non-generic argument types must still be available.
        TypeSignature target = GetTrimTarget(generic.TypeArguments[0], interopReferences);

        foreach (TypeSignature argument in generic.TypeArguments.Skip(1))
        {
            // Prefer a more selective dependency over ubiquitous types such as 'object' or 'string'
            if (!target.IsFundamentalWindowsRuntimeType(interopReferences))
            {
                break;
            }

            target = GetTrimTarget(argument, interopReferences);
        }

        return target;
    }
}
