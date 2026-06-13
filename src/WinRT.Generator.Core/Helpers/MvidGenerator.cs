// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using WindowsRuntime.Generator.Extensions;

namespace WindowsRuntime.Generator.Helpers;

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

        // CodeQL [SM02196] We'll fill the entire buffer during hashing (see below)
        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Hash the two IIDs together (the order matters).
        // CodeQL [SM02196] This hash is only used as MVID for the assembly, not for authentication.
        _ = SHA1.HashData(input, hash);

        // Create the final MVID from the first 16 bytes of the hash
        return new(hash[..16]);
    }

    /// <summary>
    /// Generates a deterministic MVID based on a set of input assemblies.
    /// </summary>
    /// <param name="assemblyPaths">The input paths of all assemblies being processed.</param>
    /// <returns>The resulting MVID.</returns>
    public static Guid CreateMvid(params IEnumerable<string> assemblyPaths)
    {
        // CodeQL [SM02196] This hash is only used as MVID for the assembly, not for authentication.
        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

        // Process all input assemblies to compute the MVID
        foreach (string assemblyPath in assemblyPaths.Order())
        {
            using FileStream stream = File.OpenRead(assemblyPath);

            hasher.AppendData(stream);
        }

        // CodeQL [SM02196] We'll fill the entire buffer during hashing (see below)
        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Write the final combined hash.
        _ = hasher.GetCurrentHash(hash);

        // Create the final MVID from the first 16 bytes of the hash
        return new(hash[..16]);
    }
}
