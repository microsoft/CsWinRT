// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="SignatureComparer"/>.
/// </summary>
internal static class SignatureComparerExtensions
{
    /// <summary>
    /// Backing field for <see cref="get_IgnoreVersion"/>.
    /// </summary>
    private static readonly SignatureComparer IgnoreVersion = new(SignatureComparisonFlags.VersionAgnostic);

    extension(SignatureComparer comparer)
    {
        /// <summary>
        /// An immutable default instance of <see cref="SignatureComparer"/>, with <see cref="SignatureComparisonFlags.VersionAgnostic"/>.
        /// </summary>
        public static SignatureComparer IgnoreVersion => IgnoreVersion;

        /// <summary>
        /// Creates an <see cref="IEqualityComparer{T}"/> instance for a pair of <see cref="TypeSignature"/> values.
        /// </summary>
        /// <returns>The resulting <see cref="IEqualityComparer{T}"/> instance.</returns>
        public IEqualityComparer<(TypeSignature, TypeSignature)> MakeValueTupleComparer()
        {
            return new SignatureValueTupleComparer(comparer);
        }

        /// <summary>
        /// Creates an <see cref="IEqualityComparer{T}"/> instance for a <see cref="TypeSignature"/> value in a tuple, on the left.
        /// </summary>
        /// <typeparam name="TRight">The type of the right item in the tuple.</typeparam>
        /// <returns>The resulting <see cref="IEqualityComparer{T}"/> instance.</returns>
        public IEqualityComparer<(TypeSignature, TRight)> MakeValueTupleLeftComparer<TRight>()
        {
            return new SignatureValueTupleLeftComparer<TRight>(comparer);
        }
    }
}

/// <summary>
/// An <see cref="IEqualityComparer{T}"/> for a pair of <see cref="TypeSignature"/> values.
/// </summary>
file sealed class SignatureValueTupleComparer : IEqualityComparer<(TypeSignature, TypeSignature)>
{
    /// <summary>
    /// The wrapped <see cref="SignatureComparer"/> instance used for comparison.
    /// </summary>
    private readonly SignatureComparer _comparer;

    /// <summary>
    /// Creates a new <see cref="SignatureValueTupleComparer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="comparer">The <see cref="SignatureComparer"/> instance to wrap.</param>
    public SignatureValueTupleComparer(SignatureComparer comparer)
    {
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public bool Equals((TypeSignature, TypeSignature) x, (TypeSignature, TypeSignature) y)
    {
        return _comparer.Equals(x.Item1, y.Item1) && _comparer.Equals(x.Item2, y.Item2);
    }

    /// <inheritdoc/>
    public int GetHashCode((TypeSignature, TypeSignature) obj)
    {
        return HashCode.Combine(_comparer.GetHashCode(obj.Item1), _comparer.GetHashCode(obj.Item2));
    }
}

/// <summary>
/// An <see cref="IEqualityComparer{T}"/> for a <see cref="TypeSignature"/> value in a tuple, on the left.
/// </summary>
/// <typeparam name="TRight">The type of the right item in the tuple.</typeparam>
file sealed class SignatureValueTupleLeftComparer<TRight> : IEqualityComparer<(TypeSignature, TRight)>
{
    /// <summary>
    /// The wrapped <see cref="SignatureComparer"/> instance used for comparison.
    /// </summary>
    private readonly SignatureComparer _comparer;

    /// <summary>
    /// Creates a new <see cref="SignatureValueTupleLeftComparer{TRight}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="comparer">The <see cref="SignatureComparer"/> instance to wrap.</param>
    public SignatureValueTupleLeftComparer(SignatureComparer comparer)
    {
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public bool Equals((TypeSignature, TRight) x, (TypeSignature, TRight) y)
    {
        return _comparer.Equals(x.Item1, y.Item1) && EqualityComparer<TRight>.Default.Equals(x.Item2, y.Item2);
    }

    /// <inheritdoc/>
    public int GetHashCode((TypeSignature, TRight) obj)
    {
        return HashCode.Combine(
            value1: _comparer.GetHashCode(obj.Item1),
            value2: obj.Item2 is null ? 0 : EqualityComparer<TRight>.Default.GetHashCode(obj.Item2));
    }
}