// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;
using WindowsRuntime.InteropGenerator.Errors;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>, specifically for the emit phase.
/// </summary>
internal sealed class InteropGeneratorEmitState
{
    /// <summary>
    /// A map to provide fast lookup for generated types that need to be referenced in different parts of the emit phase.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<TypeSignature, TypeDefinition>> _typeDefinitionLookup = [];

    /// <summary>
    /// A map to allow reusing vtable types for applicable <c>IMapView&lt;K, V&gt;</c> interfaces.
    /// </summary>
    private readonly ConcurrentDictionary<TypeSignature, TypeDefinition> _mapViewVftblTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// A map to allow reusing vtable types for applicable <c>IMap&lt;K, V&gt;</c> interfaces.
    /// </summary>
    private readonly ConcurrentDictionary<(TypeSignature Key, TypeSignature Value), TypeDefinition> _mapVftblTypes = new(SignatureComparer.IgnoreVersion.MakeValueTupleComparer());

    /// <summary>
    /// Tracks a new type generation for a given signature and key, for fast lookup.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to track.</param>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> associated with <paramref name="typeDefinition"/>.</param>
    /// <param name="key">The key for <paramref name="typeDefinition"/>.</param>
    public void TrackTypeDefinition(TypeDefinition typeDefinition, TypeSignature typeSignature, string key)
    {
        ConcurrentDictionary<TypeSignature, TypeDefinition> innerLookup = _typeDefinitionLookup.GetOrAdd(
            key: key,
            valueFactory: static _ => new ConcurrentDictionary<TypeSignature, TypeDefinition>(SignatureComparer.IgnoreVersion));

        if (!innerLookup.TryAdd(typeSignature, typeDefinition))
        {
            throw WellKnownInteropExceptions.AddingDuplicateTrackedTypeDefinition(typeSignature, key);
        }
    }

    /// <summary>
    /// Looks up a type definition previously registered with <see cref="TrackTypeDefinition"/>.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> to use for lookup.</param>
    /// <param name="key">The key to use for lookup.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public TypeDefinition LookupTypeDefinition(TypeSignature typeSignature, string key)
    {
        if (_typeDefinitionLookup.TryGetValue(key, out ConcurrentDictionary<TypeSignature, TypeDefinition>? innerLookup) &&
            innerLookup.TryGetValue(typeSignature, out TypeDefinition? typeDefinition))
        {
            return typeDefinition;
        }

        throw WellKnownInteropExceptions.TrackedTypeDefinitionLookupError(typeSignature, key);
    }

    /// <summary>
    /// Tries to get a previously registered vtable type for an <c>IMapView&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="vftblType">The resulting vtable type, if present.</param>
    /// <returns>Whether <paramref name="vftblType"/> was successfully retrieved.</returns>
    public bool TryGetIMapView2VftblType(TypeSignature keyType, [NotNullWhen(true)] out TypeDefinition? vftblType)
    {
        return _mapViewVftblTypes.TryGetValue(keyType, out vftblType);
    }

    /// <summary>
    /// Gets or adds a vtable type for an <c>IMapView&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="vftblType">The created vtable type for <paramref name="keyType"/>.</param>
    /// <returns>The vtable type that should be used.</returns>
    public TypeDefinition GetOrAddIMapView2VftblType(TypeSignature keyType, TypeDefinition vftblType)
    {
        return _mapViewVftblTypes.GetOrAdd(keyType, vftblType);
    }

    /// <summary>
    /// Tries to get a previously registered vtable type for an <c>IMap&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="valueType">The value type.</param>
    /// <param name="vftblType">The resulting vtable type, if present.</param>
    /// <returns>Whether <paramref name="vftblType"/> was successfully retrieved.</returns>
    public bool TryGetIMap2VftblType(TypeSignature keyType, TypeSignature valueType, [NotNullWhen(true)] out TypeDefinition? vftblType)
    {
        return _mapVftblTypes.TryGetValue((keyType, valueType), out vftblType);
    }

    /// <summary>
    /// Gets or adds a vtable type for an <c>IMap&lt;K, V&gt;</c> interface.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="valueType">The value type.</param>
    /// <param name="vftblType">The created vtable type for <paramref name="keyType"/>.</param>
    /// <returns>The vtable type that should be used.</returns>
    public TypeDefinition GetOrAddIMap2VftblType(TypeSignature keyType, TypeSignature valueType, TypeDefinition vftblType)
    {
        return _mapVftblTypes.GetOrAdd((keyType, valueType), vftblType);
    }
}
