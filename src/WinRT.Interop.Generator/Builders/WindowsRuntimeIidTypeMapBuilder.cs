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
using WindowsRuntime.InteropGenerator.Visitors;

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

        void AddType(TypeSignature type)
        {
            if (!type.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) ||
                !type.IsFullyResolvable(interopReferences.RuntimeContext, out TypeDefinition? definition))
            {
                return;
            }

            if (type is not (CorLibTypeSignature or TypeDefOrRefSignature or GenericInstanceTypeSignature or SzArrayTypeSignature) ||
                (definition.HasGenericParameters && type is not (GenericInstanceTypeSignature or SzArrayTypeSignature)) ||
                type.IsConstructedNullableValueType(interopReferences))
            {
                return;
            }

            // ABI value types and metadata proxies are not public projections. Exclusive interfaces are
            // genuine projected types, even when their generated declarations live in an ABI namespace.
            if ((type.Namespace == "ABI" || type.Namespace?.ToString().StartsWith("ABI.", StringComparison.Ordinal) is true) &&
                (!definition.IsInterface || !definition.IsProjectedWindowsRuntimeType))
            {
                return;
            }

            if (type.IsWindowsRuntimeType(interopReferences))
            {
                _ = types.Add(type);
            }
        }

        foreach (TypeSignature type in inputs.EnumerateIidTypes())
        {
            AddType(type);
        }

        Dictionary<string, (TypeSignature Type, Guid Iid)> entries = new(StringComparer.Ordinal);

        foreach (TypeSignature type in types.OrderByFullyQualifiedTypeName(interopReferences.RuntimeContext))
        {
            args.Token.ThrowIfCancellationRequested();

            if (!GuidGenerator.TryCreateIID(type, interopDefinitions, interopReferences, args.UseWindowsUIXamlProjections, out Guid iid))
            {
                continue;
            }

            TypeSignature importedType = module.DefaultImporter.ImportTypeSignature(type);
            string key = WindowsRuntimeIidTypeNameFactory.GetName(importedType, interopDefinitions, interopReferences);

            if (!entries.TryAdd(key, (importedType, iid)) && entries[key].Iid != iid)
            {
                throw WellKnownInteropExceptions.ConflictingIidTypeNames(key, entries[key].Iid, iid);
            }
        }

        Dictionary<Guid, TypeDefinition> providers = [];

        foreach (KeyValuePair<string, (TypeSignature Type, Guid Iid)> entry in entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            args.Token.ThrowIfCancellationRequested();

            if (!providers.TryGetValue(entry.Value.Iid, out TypeDefinition? providerType))
            {
                providerType = new(
                    ns: "ABI"u8,
                    name: $"<{providers.Count}>IidProvider",
                    attributes: TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    baseType: interopReferences.Object.ToTypeDefOrRef());

                providerType.CustomAttributes.Add(InteropCustomAttributeFactory.Guid(entry.Value.Iid, interopReferences));
                module.TopLevelTypes.Add(providerType);
                providers.Add(entry.Value.Iid, providerType);
            }

            module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeIidTypeMapGroup(
                value: entry.Key,
                target: providerType.ToTypeSignature(),
                trimTarget: GetTrimTarget(entry.Value.Type, interopReferences),
                interopReferences: interopReferences));
        }
    }

    /// <summary>
    /// Selects a non-parameterized dependency that is retained whenever an entry's type can be constructed.
    /// </summary>
    /// <param name="type">The type represented by the IID entry.</param>
    /// <param name="interopReferences">The references for identifying fundamental types.</param>
    /// <returns>The type controlling the entry's retention.</returns>
    private static TypeSignature GetTrimTarget(TypeSignature type, InteropReferences interopReferences)
    {
        if (type is SzArrayTypeSignature array)
        {
            return GetTrimTarget(array.BaseType, interopReferences);
        }

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
