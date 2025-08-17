// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// An immutable, equatable set of <see cref="TypeSignature"/> values.
/// </summary>
internal sealed partial class TypeSignatureEquatableSet : IEquatable<TypeSignatureEquatableSet>, IEnumerable<TypeSignature>
{
    /// <summary>
    /// The comparer for the <see cref="TypeSignature"/> set.
    /// </summary>
    private static readonly IEqualityComparer<HashSet<TypeSignature>> SetComparer = HashSet<TypeSignature>.CreateSetComparer();

    /// <summary>
    /// The underlying <see cref="TypeSignature"/> set.
    /// </summary>
    private readonly HashSet<TypeSignature> _set;

    /// <summary>
    /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance.
    /// </summary>
    /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
    public TypeSignatureEquatableSet(params ReadOnlySpan<TypeSignature> typeSignatures)
    {
        HashSet<TypeSignature> set = new(typeSignatures.Length, SignatureComparer.IgnoreVersion);

        foreach (TypeSignature typeSignature in typeSignatures)
        {
            _ = set.Add(typeSignature);
        }

        _set = set;
    }

    /// <summary>
    /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance.
    /// </summary>
    /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
    public TypeSignatureEquatableSet(params IEnumerable<TypeSignature> typeSignatures)
    {
        _set = new HashSet<TypeSignature>(typeSignatures, SignatureComparer.IgnoreVersion);
    }

    /// <summary>
    /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance.
    /// </summary>
    /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
    private TypeSignatureEquatableSet(HashSet<TypeSignature> typeSignatures)
    {
        _set = typeSignatures;
    }

    /// <summary>
    /// Gets a value indicating whether the current set is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set.Count == 0;
    }

    /// <inheritdoc/>
    public bool Equals(TypeSignatureEquatableSet? other)
    {
        return other is not null && SetComparer.Equals(_set, other._set);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TypeSignatureEquatableSet other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return SetComparer.GetHashCode(_set);
    }

    /// <inheritdoc/>
    public IEnumerator<TypeSignature> GetEnumerator()
    {
        return _set.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
