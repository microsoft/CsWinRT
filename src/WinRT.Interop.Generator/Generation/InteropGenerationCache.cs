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
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Writers;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Stores a verified emission fingerprint beside the generated assembly.
/// </summary>
internal static class InteropGenerationCache
{
    /// <summary>The name of the cache record, relative to the generated assembly directory.</summary>
    private const string CacheFileName = "WinRT.Interop.cache";

    /// <summary>The size of the MVID entry in the GUID heap of the generated assembly.</summary>
    private const int MvidSize = 16;

    /// <summary>The offset of the fingerprint in a cache record (after the magic and format version).</summary>
    private const int FingerprintOffset = 8;

    /// <summary>The offset of the digest of the generated assembly in a cache record.</summary>
    private const int DigestOffset = FingerprintOffset + SHA256.HashSizeInBytes;

    /// <summary>The offset of the length of the generated assembly in a cache record.</summary>
    private const int LengthOffset = DigestOffset + SHA256.HashSizeInBytes;

    /// <summary>The offset of the MVID entry of the generated assembly in a cache record.</summary>
    private const int MvidOffset = LengthOffset + sizeof(long);

    /// <summary>The fixed size of a cache record.</summary>
    private const int RecordSize = MvidOffset + sizeof(long);

    /// <summary>Gets the magic and format version that a cache record starts with.</summary>
    private static ReadOnlySpan<byte> RecordMagic => "CWRTIC01"u8;

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

            // A different length indicates a truncated, damaged, or incompatible cache record
            if (cache.Length != RecordSize)
            {
                return false;
            }

            Span<byte> record = stackalloc byte[RecordSize];

            cache.ReadExactly(record);

            // A different fingerprint is the expected outcome after any change in emission inputs
            if (!record[..FingerprintOffset].SequenceEqual(RecordMagic) ||
                !record.Slice(FingerprintOffset, SHA256.HashSizeInBytes).SequenceEqual(fingerprint))
            {
                return false;
            }

            long length = BinaryPrimitives.ReadInt64LittleEndian(record[LengthOffset..]);
            long mvidOffset = BinaryPrimitives.ReadInt64LittleEndian(record[MvidOffset..]);

            // Other readers are allowed while validating and refreshing the output, but no other writers. This
            // is more permissive than the exclusive access needed to regenerate the assembly in case of a miss.
            using FileStream assembly = new(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

            // A size mismatch or out-of-bounds MVID offset means the record cannot safely describe this assembly
            if (length != assembly.Length || mvidOffset < 0 || mvidOffset > length - MvidSize)
            {
                return false;
            }

            Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];

            HashAssembly(assembly, mvidOffset, digest, args.Token);

            // The digest also covers the recorded MVID offset, so a different offset cannot match it
            if (!digest.SequenceEqual(record.Slice(DigestOffset, SHA256.HashSizeInBytes)))
            {
                return false;
            }

            args.Token.ThrowIfCancellationRequested();

            // The existing deterministic MVID hashes every input byte, including method bodies.
            // Updating only its GUID heap entry preserves exact fresh-generation output on a hit.
            Span<byte> mvidBytes = stackalloc byte[MvidSize];

            _ = mvid.TryWriteBytes(mvidBytes);

            assembly.Position = mvidOffset;
            assembly.Write(mvidBytes);
            assembly.Flush();

            // Refresh the timestamp so MSBuild considers the reused output up to date
            File.SetLastWriteTimeUtc(assembly.SafeFileHandle, DateTime.UtcNow);

            return true;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
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

            InteropModuleWriter.Write(module, assembly);
            args.Token.ThrowIfCancellationRequested();

            Span<byte> record = stackalloc byte[RecordSize];

            WriteRecord(assembly, fingerprint, record, args.Token);

            // Keep the DLL exclusively open until its record is published, so a concurrent
            // invocation cannot associate this key with somebody else's output.
            File.WriteAllBytes(temporaryCachePath, record);
            File.Move(temporaryCachePath, cachePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryCachePath);
        }
    }

    /// <summary>
    /// Writes the record for an exclusively owned, successfully emitted output.
    /// </summary>
    /// <param name="stream">The stream for the generated assembly.</param>
    /// <param name="fingerprint">The emission fingerprint associated with the generated assembly.</param>
    /// <param name="record">The target buffer for the cache record.</param>
    /// <param name="token">The token for the operation.</param>
    private static void WriteRecord(FileStream stream, ReadOnlySpan<byte> fingerprint, Span<byte> record, CancellationToken token)
    {
        long mvidOffset;

        stream.Position = 0;

        // Keep the caller-owned stream open for hashing below and retain its exclusive lock until cache publication
        using (PEReader reader = new(stream, PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata))
        {
            MetadataReader metadata = reader.GetMetadataReader();
            GuidHandle mvid = metadata.GetModuleDefinition().Mvid;

            if (mvid.IsNil)
            {
                throw new BadImageFormatException("The generated assembly has no MVID.");
            }

            // GUID heap indices are 1-based
            mvidOffset = reader.PEHeaders.MetadataStartOffset +
                metadata.GetHeapMetadataOffset(HeapIndex.Guid) +
                ((long)MvidSize * (MetadataTokens.GetHeapOffset(mvid) - 1));
        }

        RecordMagic.CopyTo(record);
        fingerprint.CopyTo(record[FingerprintOffset..]);

        HashAssembly(stream, mvidOffset, record.Slice(DigestOffset, SHA256.HashSizeInBytes), token);

        BinaryPrimitives.WriteInt64LittleEndian(record[LengthOffset..], stream.Length);
        BinaryPrimitives.WriteInt64LittleEndian(record[MvidOffset..], mvidOffset);
    }

    /// <summary>
    /// Computes an integrity digest for the cached assembly and its recorded MVID location.
    /// </summary>
    /// <remarks>
    /// The generated MVID depends on every input assembly byte, so a method-body edit can change it without
    /// changing the required interop code. On a cache hit, <see cref="TryReuse"/> replaces that GUID to match
    /// fresh output. Hashing zeros in its place keeps the digest valid across this update while checking
    /// every other DLL byte. Hashing the recorded offset too prevents a damaged record from redirecting
    /// the GUID write to a different location.
    /// </remarks>
    /// <param name="stream">The stream for the generated assembly.</param>
    /// <param name="mvidOffset">The offset of the MVID entry in the generated assembly.</param>
    /// <param name="destination">The target buffer for the resulting digest.</param>
    /// <param name="token">The token for the operation.</param>
    private static void HashAssembly(FileStream stream, long mvidOffset, Span<byte> destination, CancellationToken token)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        Span<byte> mvidOffsetBytes = stackalloc byte[sizeof(long)];

        // Clearing the MVID entry alone does not identify its offset: if the bytes around it are zero, a nearby offset
        // would clear the same bytes. Hashing the offset too ensures the record can only patch the original MVID entry.
        BinaryPrimitives.WriteInt64LittleEndian(mvidOffsetBytes, mvidOffset);

        hash.AppendData(mvidOffsetBytes);

        // Use 64 KiB chunks to amortize read/hash calls while keeping the pool bucket below the LOH threshold
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        long offset = 0;
        int read;

        stream.Position = 0;

        while ((read = stream.Read(buffer.AsSpan())) > 0)
        {
            token.ThrowIfCancellationRequested();

            // Clear the part of the MVID entry within the current chunk, if any
            long start = Math.Max(offset, mvidOffset);
            long end = Math.Min(offset + read, mvidOffset + MvidSize);

            if (end > start)
            {
                buffer.AsSpan((int)(start - offset), (int)(end - start)).Clear();
            }

            hash.AppendData(buffer.AsSpan(0, read));

            offset += read;
        }

        _ = hash.GetHashAndReset(destination);

        ArrayPool<byte>.Shared.Return(buffer);
    }
}
