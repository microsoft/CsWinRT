// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// Orders byte sequences lexicographically without formatting them as hexadecimal strings.
/// </summary>
internal sealed class ByteSequenceComparer : IComparer<ReadOnlyMemory<byte>>
{
    /// <summary>Gets the shared stateless comparer.</summary>
    public static ByteSequenceComparer Instance { get; } = new();

    /// <inheritdoc/>
    public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
    {
        return x.Span.SequenceCompareTo(y.Span);
    }
}
