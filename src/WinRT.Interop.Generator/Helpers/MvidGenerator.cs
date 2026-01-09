// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for MVIDs for .NET modules.
/// </summary>
internal static class MvidGenerator
{
    /// <summary>
    /// Generates a deterministic MVID based on a set of input assemblies.
    /// </summary>
    /// <param name="assemblyPaths">The input paths of all assemblies being processed.</param>
    /// <returns>The resulting MVID.</returns>
    public static Guid CreateMvid(params IEnumerable<string> assemblyPaths)
    {
        using IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

        // Process all input assemblies to compute the MVID
        foreach (string assemblyPath in assemblyPaths.Order())
        {
            using FileStream stream = File.OpenRead(assemblyPath);

            hasher.AppendData(stream);
        }

        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Write the final combined hash
        _ = hasher.GetCurrentHash(hash);

        // Create the final MVID from the first 16 bytes of the hash
        return new(hash[..16]);
    }
}
