// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Generates Windows Runtime parameterized GUIDs (PIIDs) by combining the WinRT-defined
/// namespace GUID (<c>d57af411-737b-c042-abae-878b1e16adee</c>) with the type's signature
/// hashed via SHA-1 (per the WinRT type-system spec).
/// </summary>
internal static class GuidGenerator
{
    /// <summary>
    /// The PIID for the Windows Runtime namespace, used for generating IIDs of generic instantiations.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#guid-generation-for-parameterized-types"/>
    private static readonly Guid s_windowsRuntimePIIDNamespace = new(0xD57AF411, 0x737B, 0xC042, 0xAB, 0xAE, 0x87, 0x8B, 0x1E, 0x16, 0xAD, 0xEE);

    /// <summary>
    /// Generates a GUID for the given Windows Runtime parameterized type signature.
    /// </summary>
    /// <param name="signature">The parameterized signature (e.g., <c>"pinterface({...};Boolean)"</c>).</param>
    /// <returns>The resulting GUID.</returns>
    public static Guid Generate(ReadOnlySpan<char> signature)
    {
        // Stack-allocate the UTF-8 buffer (namespace GUID prefix + signature bytes) when possible;
        // fall back to ArrayPool for very long signatures. The 512-byte threshold matches interop's
        // GuidGenerator and is large enough for virtually every realistic signature.
        int maxUtf8ByteCount = Encoding.UTF8.GetMaxByteCount(signature.Length);
        int minimumPooledLength = 16 + maxUtf8ByteCount;
        byte[]? utf8BytesFromPool = null;
        Span<byte> utf8Bytes = minimumPooledLength <= 512
            ? stackalloc byte[512]
            : (utf8BytesFromPool = ArrayPool<byte>.Shared.Rent(minimumPooledLength));

        _ = s_windowsRuntimePIIDNamespace.TryWriteBytes(utf8Bytes);

        int encodedUtf8BytesWritten = Encoding.UTF8.GetBytes(signature, utf8Bytes[16..]);
        Span<byte> sha1Bytes = stackalloc byte[SHA1.HashSizeInBytes];

        _ = SHA1.HashData(utf8Bytes[..(16 + encodedUtf8BytesWritten)], sha1Bytes);

        Guid iid = EncodeGuid(sha1Bytes);

        if (utf8BytesFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(utf8BytesFromPool);
        }

        return iid;
    }

    /// <summary>
    /// Encodes a <see cref="Guid"/> from the first 16 bytes of <paramref name="data"/> following
    /// RFC 4122 rules: applies the little-endian byte-order swaps for the <c>int</c> and two
    /// <c>short</c> fields, sets the version (5) and variant (RFC 4122) bits.
    /// </summary>
    /// <param name="data">The input span (must be at least 16 bytes).</param>
    /// <returns>The resulting GUID.</returns>
    private static Guid EncodeGuid(ReadOnlySpan<byte> data)
    {
        Debug.Assert(data.Length >= 16);

        Span<byte> buffer = stackalloc byte[16];

        data[..16].CopyTo(buffer);

        if (BitConverter.IsLittleEndian)
        {
            // Swap bytes of 'int a'
            (buffer[3], buffer[0]) = (buffer[0], buffer[3]);
            (buffer[2], buffer[1]) = (buffer[1], buffer[2]);

            // Swap bytes of 'short b'
            (buffer[5], buffer[4]) = (buffer[4], buffer[5]);

            // Swap bytes of 'short c' and encode the RFC time/version field
            (buffer[7], buffer[6]) = ((byte)((buffer[6] & 0x0F) | (5 << 4)), buffer[7]);

            // Encode the RFC clock/reserved field
            buffer[8] = (byte)((buffer[8] & 0x3F) | 0x80);
        }

        return new(buffer);
    }
}