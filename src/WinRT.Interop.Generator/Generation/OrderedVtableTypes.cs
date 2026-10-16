// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// An emit-only, ordered view of a canonical set of interface signatures.
/// </summary>
internal sealed class OrderedVtableTypes : IComparable<OrderedVtableTypes>
{
    private readonly TypeDescriptorComparer _comparer;
    private readonly string[] _keys;

    /// <summary>
    /// Creates an ordered view after the interface signatures have been imported for emission.
    /// </summary>
    /// <param name="set">The canonical interface set.</param>
    /// <param name="comparer">The comparer shared by this invocation's user-defined type preparation.</param>
    public OrderedVtableTypes(TypeSignatureEquatableSet set, TypeDescriptorComparer comparer)
    {
        Set = set;
        _comparer = comparer;

        IEnumerable<(TypeSignature Type, string Key)> ordered = set
            .Select(type => (Type: type, Key: _comparer.GetOrderKey(type)))
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Type, _comparer);

        TypeSignature[] types = new TypeSignature[set.Count];
        string[] keys = new string[set.Count];
        int index = 0;

        foreach ((TypeSignature type, string key) in ordered)
        {
            types[index] = type;
            keys[index] = key;
            index++;
        }

        Types = types;
        _keys = keys;
    }

    /// <summary>
    /// Gets the canonical set whose equality semantics govern grouping.
    /// </summary>
    public TypeSignatureEquatableSet Set { get; }

    /// <summary>
    /// Gets the signatures in the same order used for interface entries.
    /// </summary>
    public IReadOnlyList<TypeSignature> Types { get; }

    /// <inheritdoc/>
    public int CompareTo(OrderedVtableTypes? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(Set, other.Set) || Set.Equals(other.Set))
        {
            return 0;
        }

        int count = Math.Min(_keys.Length, other._keys.Length);

        for (int i = 0; i < count; i++)
        {
            int result = string.CompareOrdinal(_keys[i], other._keys[i]);

            if (result == 0)
            {
                result = _comparer.CompareEqualOrderKeys(Types[i], other.Types[i]);
            }

            if (result != 0)
            {
                return result;
            }
        }

        return _keys.Length.CompareTo(other._keys.Length);
    }
}
