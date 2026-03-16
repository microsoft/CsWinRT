// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ImplGenerator;

/// <summary>
/// Extensions for <see cref="SignatureComparer"/>.
/// </summary>
internal static class SignatureComparerExtensions
{
#pragma warning disable IDE0052 // TODO: remove this once Roslyn bug is fixed
    /// <summary>
    /// Backing field for <see cref="get_IgnoreVersion"/>.
    /// </summary>
    private static readonly SignatureComparer IgnoreVersion = new(SignatureComparisonFlags.VersionAgnostic);
#pragma warning restore IDE0052

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