// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// An immutable, equatable set of <see cref="TypeSignature"/> values.
/// </summary>
internal sealed partial class TypeSignatureEquatableSet :
    IReadOnlySet<TypeSignature>,
    IEquatable<TypeSignatureEquatableSet>,
    IComparable<TypeSignatureEquatableSet>
{
    /// <summary>
    /// The underlying <see cref="TypeSignature"/> set.
    /// </summary>
    private readonly HashSet<TypeSignature> _set;

    /// <summary>
    /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance.
    /// </summary>
    /// <param name="signatureComparer">The comparer for this invocation.</param>
    /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
    public TypeSignatureEquatableSet(SignatureComparer signatureComparer, params ReadOnlySpan<TypeSignature> typeSignatures)
    {
        HashSet<TypeSignature> set = new(typeSignatures.Length, signatureComparer);

        foreach (TypeSignature typeSignature in typeSignatures)
        {
            _ = set.Add(typeSignature);
        }

        _set = set;
    }

    /// <summary>
    /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance.
    /// </summary>
    /// <param name="signatureComparer">The comparer for this invocation.</param>
    /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
    public TypeSignatureEquatableSet(SignatureComparer signatureComparer, params IEnumerable<TypeSignature> typeSignatures)
    {
        _set = new HashSet<TypeSignature>(typeSignatures, signatureComparer);
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

    /// <summary>
    /// Gets the number of <see cref="TypeSignature"/>-s in the current set.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set.Count;
    }

    /// <inheritdoc/>
    public bool Equals(TypeSignatureEquatableSet? other)
    {
        return other is not null && _set.SetEquals(other._set);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TypeSignatureEquatableSet other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Equal sets can retain different forwarded spellings, with different lexical sort orders.
        // Combine the actual element comparer hashes without depending on that order.
        int hashCode = 0;

        foreach (TypeSignature typeSignature in _set)
        {
            hashCode ^= _set.Comparer.GetHashCode(typeSignature);
        }

        return hashCode;
    }

    /// <inheritdoc/>
    public bool Contains(TypeSignature item)
    {
        return _set.Contains(item);
    }

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<TypeSignature> other)
    {
        return _set.IsProperSubsetOf(other);
    }

    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<TypeSignature> other)
    {
        return _set.IsProperSupersetOf(other);
    }

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<TypeSignature> other)
    {
        return _set.IsSubsetOf(other);
    }

    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<TypeSignature> other)
    {
        return _set.IsSupersetOf(other);
    }

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<TypeSignature> other)
    {
        return _set.Overlaps(other);
    }

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<TypeSignature> other)
    {
        return _set.SetEquals(other);
    }

    /// <inheritdoc/>
    public int CompareTo(TypeSignatureEquatableSet? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, other) || Equals(other))
        {
            return 0;
        }

        // Fast-path if both sets are just empty
        if (_set.Count == 0 && other._set.Count == 0)
        {
            return 0;
        }

        TypeDescriptorComparer comparer = new(((SignatureComparer)_set.Comparer).RuntimeContext);
        using IEnumerator<TypeSignature> left = _set.Order<TypeSignature>(comparer).GetEnumerator();
        using IEnumerator<TypeSignature> right = other.Order<TypeSignature>(comparer).GetEnumerator();

        // We want to enumerate pairs of items from both sets, one at a time
        while (true)
        {
            bool leftMoveNext = left.MoveNext();
            bool rightMoveNext = right.MoveNext();

            // If both sets have no remaining items, they are equal.
            // This is because all previous items up to now matched.
            if (!leftMoveNext && !rightMoveNext)
            {
                return 0;
            }

            // If the left sequence is over, then that set comes first
            if (!leftMoveNext)
            {
                return -1;
            }

            // If the other sequence is over, then that set comes after this one
            if (!rightMoveNext)
            {
                return 1;
            }

            int result = comparer.Compare(left.Current, right.Current);

            // If the items are not equal, just return that result. That is,
            // the first pair of items that is not equal determines the set.
            if (result != 0)
            {
                return result;
            }
        }
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
