// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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
}
