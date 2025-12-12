// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
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

        byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

        // Process all input assemblies to compute the MVID
        foreach (string assemblyPath in assemblyPaths.Order())
        {
            using FileStream stream = File.OpenRead(assemblyPath);

            int read;

            // Hash each assembly's contents. There's no way to use 'IncrementalHash'
            // to append data from multiple streams, so here we're manually reading the
            // contents from each stream and appending it in chunks to the final hash.
            while ((read = stream.Read(buffer)) > 0)
            {
                hasher.AppendData(buffer, 0, read);
            }
        }

        // Always return the pooled array (no need to clear it, the data is not sensitive)
        ArrayPool<byte>.Shared.Return(buffer);

        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Write the final combined hash
        _ = hasher.GetCurrentHash(hash);

        // Create the final MVID from the first 16 bytes of the hash
        return new(hash[..16]);
    }
}
