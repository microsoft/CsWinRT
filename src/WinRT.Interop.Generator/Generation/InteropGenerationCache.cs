// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Threading;
using ConsoleAppFramework;
using WindowsRuntime.InteropGenerator.References;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Stores a verified emission fingerprint beside the generated assembly.
/// </summary>
internal static class InteropGenerationCache
{
    /// <summary>The name of the cache record, relative to the generated assembly directory.</summary>
    private const string CacheFileName = "WinRT.Interop.cache";

    /// <summary>The fixed size of a versioned record (magic, key, output digest, length, MVID offset).</summary>
    private const int RecordSize = 8 + SHA256.HashSizeInBytes + SHA256.HashSizeInBytes + sizeof(long) + sizeof(long);

    /// <summary>
    /// Reuses a verified output, refreshing its content-derived MVID without emitting any IL or metadata.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="fingerprint">The current emission fingerprint.</param>
    /// <param name="mvid">The same MVID a full generation would use.</param>
    /// <returns>Whether the cached assembly was reused.</returns>
    public static bool TryReuse(InteropGeneratorArgs args, ReadOnlySpan<byte> fingerprint, Guid mvid)
    {
        string cachePath = Path.Combine(args.GeneratedAssemblyDirectory, CacheFileName);
        string assemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName);

        try
        {
            using FileStream cache = File.OpenRead(cachePath);
            Span<byte> record = stackalloc byte[RecordSize];

            if (cache.Length != RecordSize)
            {
                ConsoleApp.Log("Ignoring interop cache: invalid record length.");

                return false;
            }

            cache.ReadExactly(record);

            if (!record[..8].SequenceEqual("CWRTIC01"u8) ||
                !record.Slice(8, SHA256.HashSizeInBytes).SequenceEqual(fingerprint))
            {
                return false;
            }

            long length = BinaryPrimitives.ReadInt64LittleEndian(record[72..]);
            long mvidOffset = BinaryPrimitives.ReadInt64LittleEndian(record[80..]);

            using FileStream assembly = new(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

            if (length != assembly.Length || mvidOffset < 0 || mvidOffset > length - 16)
            {
                ConsoleApp.Log("Ignoring interop cache: the generated assembly has changed.");

                return false;
            }

            Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];

            HashAssembly(assembly, mvidOffset, digest, args.Token);

            if (!digest.SequenceEqual(record.Slice(40, SHA256.HashSizeInBytes)))
            {
                ConsoleApp.Log("Ignoring interop cache: the generated assembly checksum does not match.");

                return false;
            }

            args.Token.ThrowIfCancellationRequested();

            // The existing deterministic MVID hashes every input byte, including method bodies.
            // Updating only its GUID heap entry preserves exact fresh-generation output on a hit.
            Span<byte> bytes = stackalloc byte[16];

            _ = mvid.TryWriteBytes(bytes);
            assembly.Position = mvidOffset;
            assembly.Write(bytes);
            assembly.Flush();
            File.SetLastWriteTimeUtc(assembly.SafeFileHandle, DateTime.UtcNow);

            return true;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ConsoleApp.Log($"Ignoring interop cache: {e.Message}");

            return false;
        }
    }

    /// <summary>
    /// Publishes a newly emitted assembly and then its cache record.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="module">The generated module.</param>
    /// <param name="fingerprint">The emission fingerprint associated with this output.</param>
    public static void Write(InteropGeneratorArgs args, ModuleDefinition module, ReadOnlySpan<byte> fingerprint)
    {
        string assemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName);
        string cachePath = Path.Combine(args.GeneratedAssemblyDirectory, CacheFileName);
        string temporaryCachePath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using FileStream assembly = new(assemblyPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            module.Write(assembly);
            args.Token.ThrowIfCancellationRequested();

            byte[]? record = null;

            try
            {
                record = CreateRecord(assembly, fingerprint, args.Token);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                ConsoleApp.Log($"Could not create interop cache: {e.Message}");
            }

            if (record is not null)
            {
                try
                {
                    // Keep the DLL exclusively open until its record is published, so a concurrent
                    // invocation cannot associate this key with somebody else's output.
                    File.WriteAllBytes(temporaryCachePath, record);
                    File.Move(temporaryCachePath, cachePath, overwrite: true);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    ConsoleApp.Log($"Could not save interop cache: {e.Message}");
                }
            }
        }
        finally
        {
            DeleteTemporaryFile(temporaryCachePath);
        }
    }

    /// <summary>
    /// Cleans up an owned temporary file without hiding the original generation failure.
    /// </summary>
    private static void DeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ConsoleApp.Log($"Could not remove temporary interop file '{path}': {e.Message}");
        }
    }

    /// <summary>
    /// Creates the record from an exclusively owned, successfully emitted output.
    /// </summary>
    private static byte[] CreateRecord(FileStream stream, ReadOnlySpan<byte> fingerprint, CancellationToken token)
    {
        stream.Position = 0;
        long mvidOffset;

        using (PEReader reader = new(stream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata))
        {
            MetadataReader metadata = reader.GetMetadataReader();
            GuidHandle mvid = metadata.GetModuleDefinition().Mvid;

            if (mvid.IsNil)
            {
                throw new BadImageFormatException("The generated assembly has no MVID.");
            }

            mvidOffset = reader.PEHeaders.MetadataStartOffset +
                metadata.GetHeapMetadataOffset(HeapIndex.Guid) +
                (16L * (MetadataTokens.GetHeapOffset(mvid) - 1));
        }

        byte[] record = new byte[RecordSize];

        "CWRTIC01"u8.CopyTo(record);
        fingerprint.CopyTo(record.AsSpan(8));
        HashAssembly(stream, mvidOffset, record.AsSpan(40, SHA256.HashSizeInBytes), token);
        BinaryPrimitives.WriteInt64LittleEndian(record.AsSpan(72), stream.Length);
        BinaryPrimitives.WriteInt64LittleEndian(record.AsSpan(80), mvidOffset);

        return record;
    }

    /// <summary>
    /// Hashes the complete PE, treating the MVID entry as zero so it can be refreshed independently.
    /// </summary>
    private static void HashAssembly(FileStream stream, long mvidOffset, Span<byte> destination, CancellationToken token)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);

        stream.Position = 0;
        long offset = 0;
        int read;

        while ((read = stream.Read(buffer)) > 0)
        {
            token.ThrowIfCancellationRequested();

            long start = Math.Max(offset, mvidOffset);
            long end = Math.Min(offset + read, mvidOffset + 16);

            if (end > start)
            {
                buffer.AsSpan((int)(start - offset), (int)(end - start)).Clear();
            }

            hash.AppendData(buffer, 0, read);
            offset += read;
        }

        _ = hash.GetHashAndReset(destination);

        ArrayPool<byte>.Shared.Return(buffer);
    }
}
