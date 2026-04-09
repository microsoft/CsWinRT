// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;

namespace WindowsRuntime.ImplGenerator;

/// <summary>
/// A generator for MVIDs for .NET modules.
/// </summary>
internal static class MvidGenerator
{
    /// <summary>
    /// Generates a deterministic MVID based on two input IIDs.
    /// </summary>
    /// <param name="left">The first IID to combine.</param>
    /// <param name="right">The second IID to combine.</param>
    /// <returns>The resulting MVID.</returns>
    public static Guid CreateMvid(Guid left, Guid right)
    {
        Span<byte> input = stackalloc byte[32];

        // Write the two IIDs in sequence
        _ = left.TryWriteBytes(input, bigEndian: true, out _);
        _ = right.TryWriteBytes(input[16..], bigEndian: true, out _);

        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Hash the two IIDs together (the order matters)
        _ = SHA1.HashData(input, hash);

        // Take first 16 bytes and set UUID v5 version and variant bits
        Span<byte> result = hash[..16];
        result[6] = (byte)((result[6] & 0x0F) | 0x50);
        result[8] = (byte)((result[8] & 0x3F) | 0x80);

        // Construct the resulting IID from the hashed data
        return new(result, bigEndian: true);
    }
}
